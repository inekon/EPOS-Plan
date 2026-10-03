using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DAS KATALOGPAKET JE PROGRAMMFASSUNG - Entscheidungsvorlage Modellgrenzen KU1 Stufe 1.
    //
    // WAS. Alle ausgelieferten Sätze (ReadOnly = 1) der Kataloge der Stufe 1 mit Schlüssel,
    // Prüfsumme und Fachwerten, die Kindzeilen (Kennlinien) eingeschlossen, dazu die Fassung.
    // Werkzeuge/Auslieferungsvorlage schreibt es neben die Vorlage; der Abgleich beim Start liest es.
    //
    // FORMAT. EINE JSON-Datei, UTF-8 ohne BOM, LF, zwei Leerzeichen Einzug:
    //   { "Format": "EPOS-Katalogpaket", "Formatversion": 1, "Katalogfassung": n,
    //     "Tabellen": [ { "Tabelle": "Tab_WP_STAMM", "Kuerzel": "WP",
    //                     "Saetze": [ { "Schluessel": "WP:…", "Pruefsumme": "…",
    //                                   "Werte": { "Bezeichner": "…", … },
    //                                   "Kinder": { "Tab_Kenndaten_STAMM": [ { … } ] } } ] } ] }
    // Warum nicht das Format des Katalogimports: Jener liest Herstellerdateien (VDI 3805, CEC,
    // Ganglinien-CSV) - Fremdformate ohne Schlüssel, ohne Prüfsumme und ohne die Spalten des
    // eigenen Schemas. Das Paket des Zapfprofilgenerators (CSV je Tabelle) kennt keine Typen: Eine
    // Zahl und ein gleichlautender Text ergäben dort dieselbe Zelle, die Prüfsumme aber nicht. JSON
    // trägt Zahl, Text und leer getrennt - wie das Projektpaket.
    //
    // KEIN SCHEMASTAND IM PAKET: Das Paket gehört zu dem Programm, mit dem es ausgeliefert wird; die
    // Nummer eines Schemaschritts kann sich beim Zusammenführen paralleler Zweige verschieben, ein
    // Paket soll davon nicht abhängen.
    //
    // DETERMINISTISCH. Tabellen in der Folge der Stufe 1, Sätze nach Schlüssel (ordinal), Werte in
    // der Folge der Fachspalten, Kindzeilen nach ihrer Prüfsummenzeile - dasselbe Paket ergibt
    // dieselben Bytes, auf jeder Plattform.
    // ====================================================================================

    /// <summary>Ein Satz des Katalogpakets.</summary>
    public sealed class Katalogpaketsatz
    {
        /// <summary>Der stabile Schlüssel.</summary>
        public string Schluessel { get; set; } = "";

        /// <summary>Die Prüfsumme des ausgelieferten Stands.</summary>
        public string Pruefsumme { get; set; } = "";

        /// <summary>Die Fachwerte (Spalte → Wert; <c>null</c> = leer).</summary>
        public Dictionary<string, object> Werte { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>Die Kindzeilen je Kindtabelle.</summary>
        public Dictionary<string, List<Dictionary<string, object>>> Kinder { get; set; } =
            new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.Ordinal);

        /// <summary>Der Bezeichner (Anzeige).</summary>
        public string Bezeichner =>
            Werte.TryGetValue(Katalogfassung.SPALTE_BEZEICHNER, out object b) && b != null
                ? Convert.ToString(b, CultureInfo.InvariantCulture) : "";

        /// <summary>Die Kindzeilen in der Form der Prüfsumme.</summary>
        internal IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>> KinderLesbar() =>
            Kinder.ToDictionary(k => k.Key,
                                k => (IReadOnlyList<IReadOnlyDictionary<string, object>>)k.Value
                                    .Select(z => (IReadOnlyDictionary<string, object>)z).ToList(),
                                StringComparer.Ordinal);

        /// <summary>Die Prüfsumme der Werte dieses Satzes, neu gerechnet.</summary>
        internal string PruefsummeNeu(Katalogtabelle t) => Katalogfassung.Pruefsumme(t, Werte, KinderLesbar());
    }

    /// <summary>Eine Tabelle des Katalogpakets.</summary>
    public sealed class Katalogpakettabelle
    {
        /// <summary>Die Katalogtabelle.</summary>
        public string Tabelle { get; set; } = "";

        /// <summary>Die Sätze, nach Schlüssel geordnet.</summary>
        public List<Katalogpaketsatz> Saetze { get; set; } = new List<Katalogpaketsatz>();
    }

    /// <summary>
    /// <b>Das Katalogpaket</b> — Format, Schreiben aus der geöffneten Datenbank, Lesen aus der Datei.
    /// Regeln im Kopf der Datei.
    /// </summary>
    public sealed class Katalogpaket
    {
        /// <summary>Die Kennung des Formats.</summary>
        public const string FORMAT = "EPOS-Katalogpaket";

        /// <summary>Die Fassung des Formats.</summary>
        public const int FORMATVERSION = 1;

        /// <summary>Der Dateiname neben <c>Vorlage/Kenndaten.sqlite</c>.</summary>
        public const string DATEINAME = "Katalogpaket.json";

        /// <summary>Die Katalogfassung dieses Pakets (die Programmfassung des Katalogs).</summary>
        public int Fassung { get; set; }

        /// <summary>Die Tabellen in der Folge der Stufe 1.</summary>
        public List<Katalogpakettabelle> Tabellen { get; set; } = new List<Katalogpakettabelle>();

        /// <summary>Zahl der Sätze über alle Tabellen.</summary>
        public int Satzzahl => Tabellen.Sum(t => t.Saetze.Count);

        /// <summary>
        /// Der Ort des Pakets in der Auslieferung: neben der Vorlagendatenbank
        /// (<c>Dienste.Pfade.Auslieferungsvorlage</c>), also <c>{app}\Vorlage\Katalogpaket.json</c>.
        /// </summary>
        public static string Pfad(string vorlagepfad)
        {
            if (string.IsNullOrWhiteSpace(vorlagepfad)) return "";
            string ordner = Path.GetDirectoryName(vorlagepfad);
            return string.IsNullOrEmpty(ordner) ? DATEINAME : Path.Combine(ordner, DATEINAME);
        }

        // =================================================================================
        // Aus der Datenbank
        // =================================================================================

        /// <summary>
        /// <b>Schreibt den Auslieferungsstand in der geöffneten Datenbank fest</b> — der Schritt des
        /// Werkzeugs Auslieferungsvorlage vor dem Paket: Schlüssel für jeden gesperrten Satz ohne
        /// Schlüssel (die Saat), die gespeicherte Prüfsumme jedes gesperrten Satzes mit Schlüssel auf
        /// seine heutigen Werte, das Auslaufkennzeichen zurück und <c>Tab_Applikation.Katalogfassung</c>
        /// auf <paramref name="fassung"/>. So trägt die Vorlage genau den Stand des Pakets, und eine
        /// Neuinstallation gleicht beim ersten Start nichts ab. Liefert das Paket.
        /// </summary>
        public static Katalogpaket Festschreiben(int fassung, IList<string> bericht = null)
        {
            if (fassung < 0) throw new ArgumentOutOfRangeException(nameof(fassung));
            KatalogSchluesselSaat.Ausfuehren(bericht);
            foreach (Katalogtabelle t in Katalogfassung.Stufe1)
            {
                if (!DataRepository.TabelleVorhanden(t.Tabelle) || !Katalogfassung.SpaltenVorhanden(t.Tabelle)) continue;
                List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
                int n = 0;
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        DataTable dt = v.Lese("SELECT ID, \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\", \"" +
                                              Katalogfassung.SPALTE_AUSGELAUFEN + "\", " + Katalogfassung.Spaltentext(spalten) +
                                              " FROM \"" + t.Tabelle + "\" WHERE \"ReadOnly\" = 1 AND \"" +
                                              Katalogfassung.SPALTE_SCHLUESSEL + "\" IS NOT NULL ORDER BY ID");
                        if (dt != null)
                            foreach (DataRow r in dt.Rows)
                            {
                                string summe = Katalogfassung.PruefsummeDerZeile(t, spalten, r, (sql, p) => v.Lese(sql, p));
                                bool ausgelaufen = r[Katalogfassung.SPALTE_AUSGELAUFEN] != DBNull.Value &&
                                                   Convert.ToInt64(r[Katalogfassung.SPALTE_AUSGELAUFEN], CultureInfo.InvariantCulture) != 0;
                                if (string.Equals(summe, r[Katalogfassung.SPALTE_PRUEFSUMME] as string, StringComparison.Ordinal) &&
                                    !ausgelaufen) continue;
                                v.Ausfuehren("UPDATE \"" + t.Tabelle + "\" SET \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\" = ?, \"" +
                                             Katalogfassung.SPALTE_AUSGELAUFEN + "\" = 0 WHERE ID = ?",
                                             new DbParam("@p", summe),
                                             new DbParam("@id", Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture)));
                                n++;
                            }
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                if (n > 0) bericht?.Add(t.Tabelle + ": " + n.ToString(CultureInfo.InvariantCulture) +
                                        " Pruefsumme(n) auf den heutigen Stand festgeschrieben");
            }
            if (DataRepository.SpalteVorhanden(Katalogfassung.TAB_APPLIKATION, Katalogfassung.SPALTE_FASSUNG))
                DataRepository.ExecuteNonQuery("UPDATE \"" + Katalogfassung.TAB_APPLIKATION + "\" SET \"" +
                                               Katalogfassung.SPALTE_FASSUNG + "\" = ?", new DbParam("@f", fassung));
            return AusDatenbank(fassung);
        }

        /// <summary>
        /// Baut das Paket aus der geöffneten Datenbank: je Tabelle der Stufe 1 alle Sätze mit
        /// <c>ReadOnly = 1</c> und Schlüssel. Setzt den Schemaschritt voraus (Saat gelaufen).
        /// </summary>
        public static Katalogpaket AusDatenbank(int fassung)
        {
            var paket = new Katalogpaket { Fassung = fassung };
            foreach (Katalogtabelle t in Katalogfassung.Stufe1)
            {
                if (!DataRepository.TabelleVorhanden(t.Tabelle) || !Katalogfassung.SpaltenVorhanden(t.Tabelle)) continue;
                List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, \"" + Katalogfassung.SPALTE_SCHLUESSEL + "\", " + Katalogfassung.Spaltentext(spalten) +
                    " FROM \"" + t.Tabelle + "\" WHERE \"ReadOnly\" = 1 AND \"" + Katalogfassung.SPALTE_SCHLUESSEL +
                    "\" IS NOT NULL ORDER BY ID");
                var pt = new Katalogpakettabelle { Tabelle = t.Tabelle };
                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        var satz = new Katalogpaketsatz
                        {
                            Schluessel = Convert.ToString(r[Katalogfassung.SPALTE_SCHLUESSEL], CultureInfo.InvariantCulture),
                            Werte = Glatt(Katalogfassung.Werte(spalten, r)),
                        };
                        long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                        foreach (var k in Katalogfassung.Kinder(t, id, Katalogfassung.LeseOhneVorgang))
                            satz.Kinder[k.Key] = k.Value.Select(z => Glatt(z)).ToList();
                        satz.Pruefsumme = satz.PruefsummeNeu(t);
                        pt.Saetze.Add(satz);
                    }
                }
                pt.Saetze.Sort((a, b) => string.CompareOrdinal(a.Schluessel, b.Schluessel));
                paket.Tabellen.Add(pt);
            }
            return paket;
        }

        /// <summary>Werte in die Form des Pakets: Wahrheitswerte als 1/0, Ganzzahlen als long, sonst double/Text.</summary>
        private static Dictionary<string, object> Glatt(IReadOnlyDictionary<string, object> werte)
        {
            var d = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var kv in werte) d[kv.Key] = Glattwert(kv.Value);
            return d;
        }

        internal static object Glattwert(object w)
        {
            switch (w)
            {
                case null: return null;
                case DBNull _: return null;
                case bool b: return b ? 1L : 0L;
                case byte or sbyte or short or ushort or int or uint or long:
                    return Convert.ToInt64(w, CultureInfo.InvariantCulture);
                case float f: return (double)f;
                case decimal m: return (double)m;
                case double d: return d;
                case string s: return s;
                case DateTime t: return t.ToString("o", CultureInfo.InvariantCulture);
                default: return Convert.ToString(w, CultureInfo.InvariantCulture);
            }
        }

        // =================================================================================
        // Schreiben
        // =================================================================================

        /// <summary>Das Paket als Bytes (UTF-8 ohne BOM, LF) — deterministisch.</summary>
        public byte[] Bytes()
        {
            using (var ms = new MemoryStream())
            {
                var opt = new JsonWriterOptions { Indented = true, NewLine = "\n" };
                using (var w = new Utf8JsonWriter(ms, opt))
                {
                    w.WriteStartObject();
                    w.WriteString("Format", FORMAT);
                    w.WriteNumber("Formatversion", FORMATVERSION);
                    w.WriteNumber("Katalogfassung", Fassung);
                    w.WriteStartArray("Tabellen");
                    foreach (Katalogtabelle t in Katalogfassung.Stufe1)
                    {
                        Katalogpakettabelle pt = Tabellen.FirstOrDefault(x => string.Equals(x.Tabelle, t.Tabelle, StringComparison.Ordinal));
                        if (pt == null) continue;
                        w.WriteStartObject();
                        w.WriteString("Tabelle", t.Tabelle);
                        w.WriteString("Kuerzel", t.Kuerzel);
                        w.WriteStartArray("Saetze");
                        foreach (Katalogpaketsatz s in pt.Saetze.OrderBy(x => x.Schluessel, StringComparer.Ordinal))
                        {
                            w.WriteStartObject();
                            w.WriteString("Schluessel", s.Schluessel);
                            w.WriteString("Pruefsumme", s.Pruefsumme);
                            w.WritePropertyName("Werte");
                            Zeile(w, t.Fachspalten, s.Werte);
                            if (t.Kinder.Count > 0)
                            {
                                w.WriteStartObject("Kinder");
                                foreach (Katalogkind k in t.Kinder)
                                {
                                    if (!s.Kinder.TryGetValue(k.Tabelle, out List<Dictionary<string, object>> zeilen)) continue;
                                    w.WriteStartArray(k.Tabelle);
                                    foreach (Dictionary<string, object> z in zeilen
                                                 .OrderBy(z => Kindschluessel(k, z), StringComparer.Ordinal))
                                        Zeile(w, k.Fachspalten, z);
                                    w.WriteEndArray();
                                }
                                w.WriteEndObject();
                            }
                            w.WriteEndObject();
                        }
                        w.WriteEndArray();
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                    w.WriteEndObject();
                }
                ms.WriteByte((byte)'\n');
                return ms.ToArray();
            }
        }

        private static string Kindschluessel(Katalogkind k, IReadOnlyDictionary<string, object> z) =>
            string.Join(";", k.Fachspalten.Select(s => s + "=" + (z.TryGetValue(s, out object w) ? Katalogfassung.Normiert(w) : null)));

        private static void Zeile(Utf8JsonWriter w, IEnumerable<string> spalten, IReadOnlyDictionary<string, object> werte)
        {
            w.WriteStartObject();
            foreach (string s in spalten)
            {
                if (!werte.TryGetValue(s, out object v)) continue;
                switch (Glattwert(v))
                {
                    case null: w.WriteNull(s); break;
                    case long l: w.WriteNumber(s, l); break;
                    case double d when !double.IsNaN(d) && !double.IsInfinity(d): w.WriteNumber(s, d); break;
                    case double _: w.WriteNull(s); break;
                    case string t: w.WriteString(s, t); break;
                }
            }
            w.WriteEndObject();
        }

        /// <summary>Schreibt das Paket nach <paramref name="pfad"/> (überschreibt).</summary>
        public void Speichern(string pfad)
        {
            string ordner = Path.GetDirectoryName(pfad);
            if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);
            File.WriteAllBytes(pfad, Bytes());
        }

        // =================================================================================
        // Lesen
        // =================================================================================

        /// <summary>
        /// Liest ein Paket. Wirft <see cref="InvalidDataException"/> mit Grund, wenn Format, Fassung,
        /// eine Tabelle oder eine Prüfsumme nicht stimmt — ein beschädigtes Paket gleicht nichts ab.
        /// </summary>
        public static Katalogpaket Lesen(string pfad)
        {
            byte[] bytes = File.ReadAllBytes(pfad);
            return AusBytes(bytes);
        }

        /// <summary>Wie <see cref="Lesen"/>, aus Bytes.</summary>
        public static Katalogpaket AusBytes(byte[] bytes)
        {
            using (JsonDocument doc = JsonDocument.Parse(bytes))
            {
                JsonElement wurzel = doc.RootElement;
                if (wurzel.ValueKind != JsonValueKind.Object ||
                    !wurzel.TryGetProperty("Format", out JsonElement f) || f.GetString() != FORMAT)
                    throw new InvalidDataException("Format „" + FORMAT + "“ erwartet.");
                if (!wurzel.TryGetProperty("Formatversion", out JsonElement fv) || fv.GetInt32() != FORMATVERSION)
                    throw new InvalidDataException("Formatversion " + FORMATVERSION.ToString(CultureInfo.InvariantCulture) + " erwartet.");
                var paket = new Katalogpaket
                {
                    Fassung = wurzel.GetProperty("Katalogfassung").GetInt32(),
                };
                if (paket.Fassung < 0) throw new InvalidDataException("Die Katalogfassung ist negativ.");
                foreach (JsonElement te in wurzel.GetProperty("Tabellen").EnumerateArray())
                {
                    string name = te.GetProperty("Tabelle").GetString();
                    Katalogtabelle t = Katalogfassung.Tabelle(name);
                    if (t == null) throw new InvalidDataException("Die Tabelle " + name + " gehört nicht zur Stufe 1.");
                    var pt = new Katalogpakettabelle { Tabelle = t.Tabelle };
                    foreach (JsonElement se in te.GetProperty("Saetze").EnumerateArray())
                    {
                        var s = new Katalogpaketsatz
                        {
                            Schluessel = se.GetProperty("Schluessel").GetString() ?? "",
                            Pruefsumme = se.GetProperty("Pruefsumme").GetString() ?? "",
                            Werte = Werte(se.GetProperty("Werte")),
                        };
                        if (se.TryGetProperty("Kinder", out JsonElement ke))
                            foreach (JsonProperty kp in ke.EnumerateObject())
                                s.Kinder[kp.Name] = kp.Value.EnumerateArray().Select(Werte).ToList();
                        if (s.Schluessel.Length == 0)
                            throw new InvalidDataException(t.Tabelle + ": ein Satz ohne Schlüssel.");
                        if (!string.Equals(s.PruefsummeNeu(t), s.Pruefsumme, StringComparison.Ordinal))
                            throw new InvalidDataException(t.Tabelle + ", " + s.Schluessel + ": die Prüfsumme passt nicht zu den Werten.");
                        pt.Saetze.Add(s);
                    }
                    if (pt.Saetze.Select(x => x.Schluessel).Distinct(StringComparer.Ordinal).Count() != pt.Saetze.Count)
                        throw new InvalidDataException(t.Tabelle + ": ein Schlüssel steht doppelt.");
                    paket.Tabellen.Add(pt);
                }
                return paket;
            }
        }

        private static Dictionary<string, object> Werte(JsonElement e)
        {
            var d = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (JsonProperty p in e.EnumerateObject())
            {
                switch (p.Value.ValueKind)
                {
                    case JsonValueKind.Null: d[p.Name] = null; break;
                    case JsonValueKind.String: d[p.Name] = p.Value.GetString(); break;
                    case JsonValueKind.Number:
                        d[p.Name] = p.Value.TryGetInt64(out long l) ? l : p.Value.GetDouble();
                        break;
                    case JsonValueKind.True: d[p.Name] = 1L; break;
                    case JsonValueKind.False: d[p.Name] = 0L; break;
                    default: throw new InvalidDataException("Spalte " + p.Name + ": kein einfacher Wert.");
                }
            }
            return d;
        }
    }
}
