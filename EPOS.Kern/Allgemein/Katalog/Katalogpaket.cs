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
    // DAS KATALOGPAKET JE PROGRAMMFASSUNG - Entscheidungsvorlage Modellgrenzen KU1 Stufe 1 und 2.
    //
    // WAS. Alle ausgelieferten Sätze (ReadOnly = 1) der Kataloge des Registers (Katalogfassung.Alle)
    // mit Schlüssel, Prüfsumme und Fachwerten, die Kindzeilen (Kennlinien, Reihen, Konditionierung
    // samt Perioden) eingeschlossen, dazu die Fassung.
    // Werkzeuge/Auslieferungsvorlage schreibt es neben die Vorlage; der Abgleich beim Start liest es.
    //
    // FORMAT. EINE JSON-Datei, UTF-8 ohne BOM, LF, zwei Leerzeichen Einzug:
    //   { "Format": "EPOS-Katalogpaket", "Formatversion": 1, "Katalogfassung": n,
    //     "Tabellen": [ { "Tabelle": "Tab_WP_STAMM", "Kuerzel": "WP",
    //                     "Saetze": [ { "Schluessel": "WP:…", "Pruefsumme": "…",
    //                                   "Werte": { "Bezeichner": "…", … },
    //                                   "Kinder": { "Tab_Kenndaten_STAMM": [ { … } ] } } ] } ] }
    // Formatversion 2 (Stufe 2) liest auch Fassung 1 und kennt zwei Formen dazu: Eine REIHE
    // (einspaltig, geordnet - Ganglinien, Tagesverteilungen) steht als bloße Werteliste
    // ("Tab_WaermebedarfDaten_STAMM": [ 1.5, 2, … ]) in ihrer Folge; eine Kindzeile mit Enkeln trägt
    // sie unter "Kinder" ({ "Groesse": …, "Kinder": { "Tab_Konditionierungsperiode": [ … ] } }).
    // Ein Verweis steht mit dem Namen seines Ziels, nie mit dessen ID.
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
    // DETERMINISTISCH. Tabellen in der Folge des Registers, Sätze nach Schlüssel (ordinal), Werte in
    // der Folge der Fachspalten, Kindzeilen nach ihrer Prüfsummenzeile (eine Reihe in ihrer Folge) -
    // dasselbe Paket ergibt dieselben Bytes, auf jeder Plattform.
    //
    // GRÖSSE. Die Reihen machen den Hauptteil: eine Stundenreihe rund 0,2 MB, eine
    // Viertelstundenreihe rund 0,8 MB. Das Werkzeug Auslieferungsvorlage nennt die Größe im Bericht
    // und warnt ab GROESSE_WARNUNG.
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

        /// <summary>Die Katalogtabelle des Satzes (gesetzt beim Lesen und Bauen).</summary>
        internal Katalogtabelle Katalog { get; set; }

        /// <summary>Der Name (Anzeige) aus den Namensspalten der Tabelle.</summary>
        public string Bezeichner =>
            Katalog != null
                ? Katalogfassung.Name(Katalog, Werte)
                : Werte.TryGetValue(Katalogfassung.SPALTE_BEZEICHNER, out object b) && b != null
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

        /// <summary>Die Fassung des Formats, die geschrieben wird (2: Stufe 2 mit Reihen und Enkeln).</summary>
        public const int FORMATVERSION = 2;

        /// <summary>Die älteste Fassung des Formats, die sich noch liest.</summary>
        public const int FORMATVERSION_MIN = 1;

        /// <summary>Ab dieser Größe (Bytes) warnt das Werkzeug: Reihen wären dann benannt auszunehmen.</summary>
        public const long GROESSE_WARNUNG = 20L * 1024 * 1024;

        /// <summary>Der Dateiname neben <c>Vorlage/Kenndaten.sqlite</c>.</summary>
        public const string DATEINAME = "Katalogpaket.json";

        /// <summary>Die Katalogfassung dieses Pakets (die Programmfassung des Katalogs).</summary>
        public int Fassung { get; set; }

        /// <summary>Die Tabellen in der Folge des Registers.</summary>
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
            foreach (Katalogtabelle t in Katalogfassung.Alle)
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
        /// Baut das Paket aus der geöffneten Datenbank: je Tabelle des Registers alle Sätze mit
        /// <c>ReadOnly = 1</c> und Schlüssel. Setzt den Schemaschritt voraus (Saat gelaufen).
        /// </summary>
        public static Katalogpaket AusDatenbank(int fassung)
        {
            var paket = new Katalogpaket { Fassung = fassung };
            foreach (Katalogtabelle t in Katalogfassung.Alle)
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
                            Katalog = t,
                            Schluessel = Convert.ToString(r[Katalogfassung.SPALTE_SCHLUESSEL], CultureInfo.InvariantCulture),
                            Werte = Glatt(Katalogfassung.Fachwerte(t, spalten, r, Katalogfassung.LeseOhneVorgang)),
                        };
                        long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                        foreach (var k in Katalogfassung.Kinder(t, id, Katalogfassung.LeseOhneVorgang))
                        {
                            Katalogkind kind = t.Kinder.First(x => x.Tabelle == k.Key);
                            satz.Kinder[k.Key] = k.Value.Select(z => GlattKind(kind, z)).ToList();
                        }
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

        /// <summary>Eine Kindzeile in der Form des Pakets, ihre Enkel eingeschlossen.</summary>
        private static Dictionary<string, object> GlattKind(Katalogkind k, IReadOnlyDictionary<string, object> z)
        {
            var d = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var kv in z)
                if (!k.Enkel.Any(e => e.Tabelle == kv.Key)) d[kv.Key] = Glattwert(kv.Value);
            foreach (Katalogkind e in k.Enkel)
                if (z.ContainsKey(e.Tabelle))
                    d[e.Tabelle] = Katalogfassung.Enkelzeilen(z, e.Tabelle).Select(x => GlattKind(e, x)).ToList();
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
                    foreach (Katalogtabelle t in Katalogfassung.Alle)
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
                                    Kindzeilen(w, k, zeilen);
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

        /// <summary>
        /// Die Kindzeilen einer Kindtabelle als Feld: eine Reihe als bloße Werteliste in ihrer Folge,
        /// sonst je Zeile ein Objekt (sortiert, eine geordnete Tabelle in ihrer Folge), Enkel unter „Kinder".
        /// </summary>
        private static void Kindzeilen(Utf8JsonWriter w, Katalogkind k, IEnumerable<IReadOnlyDictionary<string, object>> zeilen)
        {
            w.WriteStartArray(k.Tabelle);
            if (k.Reihe)
            {
                string spalte = k.Fachspalten[0];
                foreach (IReadOnlyDictionary<string, object> z in zeilen)
                    Wert(w, z.TryGetValue(spalte, out object v) ? v : null);
            }
            else
            {
                IEnumerable<IReadOnlyDictionary<string, object>> folge = zeilen;
                if (!k.Geordnet)
                    folge = k.Enkel.Count == 0
                        ? zeilen.OrderBy(z => Kindschluessel(k, z), StringComparer.Ordinal)
                        : zeilen.OrderBy(z => Katalogfassung.Kindtexte(k, new[] { z })[0], StringComparer.Ordinal);
                foreach (IReadOnlyDictionary<string, object> z in folge)
                {
                    if (k.Enkel.Count == 0) { Zeile(w, k.Fachspalten, z); continue; }
                    w.WriteStartObject();
                    Felder(w, k.Fachspalten, z);
                    w.WriteStartObject("Kinder");
                    foreach (Katalogkind e in k.Enkel)
                        Kindzeilen(w, e, Katalogfassung.Enkelzeilen(z, e.Tabelle));
                    w.WriteEndObject();
                    w.WriteEndObject();
                }
            }
            w.WriteEndArray();
        }

        private static string Kindschluessel(Katalogkind k, IReadOnlyDictionary<string, object> z) =>
            string.Join(";", k.Fachspalten.Select(s => s + "=" + (z.TryGetValue(s, out object w) ? Katalogfassung.Normiert(w) : null)));

        private static void Zeile(Utf8JsonWriter w, IEnumerable<string> spalten, IReadOnlyDictionary<string, object> werte)
        {
            w.WriteStartObject();
            Felder(w, spalten, werte);
            w.WriteEndObject();
        }

        private static void Felder(Utf8JsonWriter w, IEnumerable<string> spalten, IReadOnlyDictionary<string, object> werte)
        {
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
        }

        /// <summary>Ein einzelner Wert eines Feldes (Reihe).</summary>
        private static void Wert(Utf8JsonWriter w, object v)
        {
            switch (Glattwert(v))
            {
                case null: w.WriteNullValue(); break;
                case long l: w.WriteNumberValue(l); break;
                case double d when !double.IsNaN(d) && !double.IsInfinity(d): w.WriteNumberValue(d); break;
                case double _: w.WriteNullValue(); break;
                case string t: w.WriteStringValue(t); break;
            }
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
                if (!wurzel.TryGetProperty("Formatversion", out JsonElement fv) || fv.ValueKind != JsonValueKind.Number ||
                    fv.GetInt32() < FORMATVERSION_MIN || fv.GetInt32() > FORMATVERSION)
                    throw new InvalidDataException("Formatversion " + FORMATVERSION_MIN.ToString(CultureInfo.InvariantCulture) +
                                                   " bis " + FORMATVERSION.ToString(CultureInfo.InvariantCulture) + " erwartet.");
                var paket = new Katalogpaket
                {
                    Fassung = wurzel.GetProperty("Katalogfassung").GetInt32(),
                };
                if (paket.Fassung < 0) throw new InvalidDataException("Die Katalogfassung ist negativ.");
                foreach (JsonElement te in wurzel.GetProperty("Tabellen").EnumerateArray())
                {
                    string name = te.GetProperty("Tabelle").GetString();
                    Katalogtabelle t = Katalogfassung.Tabelle(name);
                    if (t == null) throw new InvalidDataException("Die Tabelle " + name + " gehört nicht zum Katalogregister.");
                    var pt = new Katalogpakettabelle { Tabelle = t.Tabelle };
                    foreach (JsonElement se in te.GetProperty("Saetze").EnumerateArray())
                    {
                        var s = new Katalogpaketsatz
                        {
                            Katalog = t,
                            Schluessel = se.GetProperty("Schluessel").GetString() ?? "",
                            Pruefsumme = se.GetProperty("Pruefsumme").GetString() ?? "",
                            Werte = Werte(se.GetProperty("Werte")),
                        };
                        if (se.TryGetProperty("Kinder", out JsonElement ke))
                            foreach (JsonProperty kp in ke.EnumerateObject())
                                s.Kinder[kp.Name] = Kindzeilen(kp.Value, t.Kinder.FirstOrDefault(k => k.Tabelle == kp.Name));
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

        /// <summary>
        /// Die Kindzeilen eines Feldes: Objekte (Enkel unter „Kinder") oder, bei einer Reihe, bloße Werte.
        /// <paramref name="k"/> = <c>null</c> für eine Kindtabelle, die das Register nicht kennt.
        /// </summary>
        private static List<Dictionary<string, object>> Kindzeilen(JsonElement feld, Katalogkind k)
        {
            if (feld.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Kindzeilen: ein Feld erwartet.");
            var liste = new List<Dictionary<string, object>>();
            foreach (JsonElement z in feld.EnumerateArray())
            {
                if (z.ValueKind != JsonValueKind.Object)
                {
                    if (k == null || !k.Reihe)
                        throw new InvalidDataException((k?.Tabelle ?? "Kindtabelle") + ": ein bloßer Wert gehört nur in eine Reihe.");
                    liste.Add(new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        [k.Fachspalten[0]] = Einzelwert(k.Fachspalten[0], z)
                    });
                    continue;
                }
                bool mitEnkeln = k != null && k.Enkel.Count > 0;
                Dictionary<string, object> d = Werte(z, mitEnkeln);
                if (mitEnkeln && z.TryGetProperty("Kinder", out JsonElement ke))
                    foreach (JsonProperty ep in ke.EnumerateObject())
                        d[ep.Name] = Kindzeilen(ep.Value, k.Enkel.FirstOrDefault(e => e.Tabelle == ep.Name));
                liste.Add(d);
            }
            return liste;
        }

        private static Dictionary<string, object> Werte(JsonElement e, bool ohneKinder = false)
        {
            var d = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (JsonProperty p in e.EnumerateObject())
            {
                if (ohneKinder && p.Name == "Kinder") continue;
                d[p.Name] = Einzelwert(p.Name, p.Value);
            }
            return d;
        }

        private static object Einzelwert(string spalte, JsonElement v)
        {
            switch (v.ValueKind)
            {
                case JsonValueKind.Null: return null;
                case JsonValueKind.String: return v.GetString();
                case JsonValueKind.Number: return v.TryGetInt64(out long l) ? l : v.GetDouble();
                case JsonValueKind.True: return 1L;
                case JsonValueKind.False: return 0L;
                default: throw new InvalidDataException("Spalte " + spalte + ": kein einfacher Wert.");
            }
        }
    }
}
