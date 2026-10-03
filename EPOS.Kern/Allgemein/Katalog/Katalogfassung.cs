using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KATALOGFASSUNG: SCHLÜSSEL UND PRÜFSUMME JE AUSGELIEFERTEM SATZ - Entscheidungsvorlage
    // Modellgrenzen KU1, Stufe 1 (Welle M6 „Katalog-Update").
    //
    // WAS. Jeder ausgelieferte Satz (ReadOnly = 1) eines laufend gepflegten Katalogs trägt einen
    // stabilen SCHLÜSSEL (über Programmfassungen gleich) und die PRÜFSUMME seiner ausgelieferten
    // Fachwerte. Ein Update erkennt daran, ob der Anwender den Satz angefasst hat: Prüfsumme der
    // Zeile = gespeicherte Prüfsumme heißt „wie ausgeliefert", also darf der neue Stand darüber.
    //
    // DIE STUFE 1 sind die Kataloge mit laufender Pflege: Wärmepumpen samt Kennlinien und
    // Kühlkennlinien, Heizkessel, BHKW, PV-Module, Brauchwasser- und Prozessprofile mit ihren
    // Wochenprofilen (Typen). Die übrigen Kataloge folgen in Stufe 2.
    //
    // DIE PRÜFSUMME ist SHA-256 über die Fachspalten in der festen Folge von
    // Katalogtabelle.Fachspalten, je Spalte „Name=Wert" (Zahlen invariant und rundlauffest, Text
    // unverändert, LEER trägt nichts bei - eine neue, leer angelegte Spalte verschiebt keine
    // Prüfsumme). Die Kindzeilen (Kennlinien) gehen sortiert hinter dem Kopf ein. Die Reihenfolge,
    // in der ein Leser die Spalten liefert, spielt keine Rolle.
    //
    // DER SCHLÜSSEL ist Tabellenkürzel und bereinigter Bezeichner („WP:LW_12_A"); belegt ein
    // anderer Satz derselben Tabelle den Schlüssel schon, kommt ein Zähler dazu („_2", „_3").
    // Ein Anwendersatz (ReadOnly = 0) bekommt keinen.
    // ====================================================================================

    /// <summary>Eine Kindtabelle eines Katalogsatzes (etwa die Kennlinie der Wärmepumpe).</summary>
    public sealed class Katalogkind
    {
        internal Katalogkind(string tabelle, string fremdschluessel, string[] fachspalten, string projektspalte = null)
        {
            Tabelle = tabelle;
            Fremdschluessel = fremdschluessel;
            Fachspalten = fachspalten;
            Projektspalte = projektspalte;
        }

        /// <summary>Die Kindtabelle.</summary>
        public string Tabelle { get; }

        /// <summary>Die Spalte, die auf den Kopfsatz zeigt.</summary>
        public string Fremdschluessel { get; }

        /// <summary>Die Fachspalten in fester Folge.</summary>
        public IReadOnlyList<string> Fachspalten { get; }

        /// <summary>
        /// Führt die Kindtabelle eine Projektspalte, gehören nur Zeilen mit leerem oder 0
        /// dazu — eine Zeile mit Projekt ist eine Projektkopie und wird nie angefasst.
        /// </summary>
        public string Projektspalte { get; }

        /// <summary>Die Bedingung „gehört zum Katalog" (leer ohne Projektspalte).</summary>
        internal string Katalogbedingung =>
            Projektspalte == null ? "" : " AND COALESCE(\"" + Projektspalte + "\", 0) = 0";
    }

    /// <summary>Eine Katalogtabelle der Stufe 1.</summary>
    public sealed class Katalogtabelle
    {
        internal Katalogtabelle(string tabelle, string kuerzel, string[] fachspalten, params Katalogkind[] kinder)
        {
            Tabelle = tabelle;
            Kuerzel = kuerzel;
            Fachspalten = fachspalten;
            Kinder = kinder ?? new Katalogkind[0];
        }

        /// <summary>Die Katalogtabelle (<c>…_STAMM</c>).</summary>
        public string Tabelle { get; }

        /// <summary>Das Kürzel vor dem Schlüssel.</summary>
        public string Kuerzel { get; }

        /// <summary>Die Fachspalten in fester Folge — alle Spalten außer ID, ReadOnly und den Katalogspalten.</summary>
        public IReadOnlyList<string> Fachspalten { get; }

        /// <summary>Die Kindtabellen, deren Zeilen zum Satz gehören.</summary>
        public IReadOnlyList<Katalogkind> Kinder { get; }

        public override string ToString() => Tabelle;
    }

    /// <summary>
    /// <b>Die Katalogfassung</b> — Tabellenliste der Stufe 1, Schlüsselbildung und Prüfsumme.
    /// Regeln im Kopf der Datei.
    /// </summary>
    public static class Katalogfassung
    {
        /// <summary>Die stabile Textkennung eines ausgelieferten Satzes.</summary>
        public const string SPALTE_SCHLUESSEL = "Katalog_Schluessel";

        /// <summary>Die Prüfsumme des ausgelieferten Stands (SHA-256, 64 Hexzeichen).</summary>
        public const string SPALTE_PRUEFSUMME = "Katalog_Pruefsumme";

        /// <summary>1 = der Satz ist in einer späteren Auslieferung entfallen; er bleibt stehen.</summary>
        public const string SPALTE_AUSGELAUFEN = "Katalog_Ausgelaufen";

        /// <summary>Die Spalte der Programmfassung an <c>Tab_Applikation</c>; leer = noch nie abgeglichen.</summary>
        public const string SPALTE_FASSUNG = "Katalogfassung";

        /// <summary>Die Statustabelle mit der Katalogfassung.</summary>
        public const string TAB_APPLIKATION = "Tab_Applikation";

        /// <summary>Die Spalte des Bezeichners — in allen Tabellen der Stufe 1 eindeutig.</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>Das Auslieferungskennzeichen.</summary>
        public const string SPALTE_READONLY = "ReadOnly";

        /// <summary>Höchstlänge des Schlüssels (Prüfklausel der Spalte).</summary>
        public const int SCHLUESSEL_MAX = 120;

        /// <summary>
        /// Die Spalten, die KEIN Fachwert sind: Kennung, Auslieferungskennzeichen und die drei
        /// Katalogspalten. Eine Katalogkopie, die Dublettenprüfung und die Prüfsumme lassen sie aus.
        /// </summary>
        public static readonly IReadOnlyCollection<string> Metaspalten =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ID", SPALTE_READONLY, SPALTE_SCHLUESSEL, SPALTE_PRUEFSUMME, SPALTE_AUSGELAUFEN
            };

        /// <summary>Ist <paramref name="spalte"/> eine der drei Katalogspalten?</summary>
        public static bool IstKatalogspalte(string spalte) =>
            string.Equals(spalte, SPALTE_SCHLUESSEL, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(spalte, SPALTE_PRUEFSUMME, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(spalte, SPALTE_AUSGELAUFEN, StringComparison.OrdinalIgnoreCase);

        private static string[] Stunden168()
        {
            var s = new string[168];
            for (int i = 0; i < 168; i++) s[i] = (i + 1).ToString(CultureInfo.InvariantCulture);
            return s;
        }

        private static string[] Monate12()
        {
            var s = new string[12];
            for (int i = 0; i < 12; i++) s[i] = "Monat_" + (i + 1).ToString(CultureInfo.InvariantCulture);
            return s;
        }

        private static string[] Verbinden(params string[][] teile) => teile.SelectMany(t => t).ToArray();

        private static readonly Katalogtabelle[] STUFE1 =
        {
            new Katalogtabelle("Tab_WP_STAMM", "WP",
                new[]
                {
                    "Bezeichner", "Firma", "Beschreibung", "Typ", "Baujahr", "Aufstellung", "Nennleistung",
                    "maxPtherm", "Heizung", "Regelung", "Modulkosten", "Laenge", "Breite", "Hoehe", "Gewicht",
                    "Raum", "Kuehlleistung", "Bauart", "Kuehlbetrieb", "Kuehl_Vorlauf", "Kuehl_Hilfsstromanteil",
                    "Mindestleistung_kW", "Taktverlustfaktor_Cd"
                },
                new Katalogkind("Tab_Kenndaten_STAMM", "ID_WP", new[] { "Vorlauf", "Temperatur", "COP", "Ptherm" }),
                new Katalogkind("Tab_Kenndaten_Kuehlung_STAMM", "ID_WP",
                                new[] { "Vorlauf", "Temperatur", "COP", "Pkuehl", "Last" }, "ID_Projekt")),
            new Katalogtabelle("Tab_Heizkessel_STAMM", "KES",
                new[]
                {
                    "Bezeichner", "Firma", "Beschreibung", "Ptherm", "Brennstoff", "Wirkungsgrad_Gas",
                    "Wirkungsgrad_Öl", "Investitionskosten", "Raumbedarf", "Wartungskosten", "Nutzungsdauer",
                    "CO2", "SO2", "NOx", "CO", "Staub", "Betriebsbereitschaftverlust", "Brennwert", "Vorlauf",
                    "Ruecklauf", "Wartungskosten_Einheit", "Wirkungsgrad_Teillast30", "Kennlinie_Brennwert",
                    "Mindestleistung", "Anfahrverlust_kWh", "Mindestlaufzeit_min", "Bereitschaft_Einheit"
                }),
            new Katalogtabelle("Tab_BHKW_STAMM", "BHKW",
                new[]
                {
                    "Bezeichner", "Firma", "Beschreibung", "Ptherm", "Pel", "Brennstoff", "Wirkungsgrad",
                    "Investition_kwel", "Raumbedarf", "Wartungskosten_kwhel", "Nutzungsdauer", "NOX", "SO2", "CO",
                    "CO2", "Staub", "Motortyp", "Grenzleistung", "Kosten_Modul", "Kosten_Montage",
                    "Kosten_Lieferung", "Kosten_Schallschutzhaube", "Kosten_Abgasreinigung", "Vorlauf",
                    "Ruecklauf", "Wirkungsgrad_el", "Wirkungsgrad_th", "Wirkungsgrad_el_Teillast50",
                    "Wirkungsgrad_th_Teillast50", "Anfahrverlust_kWh", "Mindestlaufzeit_min"
                }),
            new Katalogtabelle("Tab_PV_STAMM", "PV",
                new[]
                {
                    "Bezeichner", "Firma", "Beschreibung", "Leistung", "Wirkungsgrad", "U_Mpp", "U_Leerlauf",
                    "I_Mpp", "I_Kurzschluss", "alpha_SC", "beta_OC", "gamma_PMP", "T_NOCT", "Laenge", "Breite",
                    "Modulkosten", "Technologie"
                }),
            new Katalogtabelle("Tab_Brauchwasser_STAMM", "BW",
                Verbinden(new[] { "Bezeichner", "Typ", "Beschreibung" }, Monate12())),
            new Katalogtabelle("Tab_Brauchwassertyp_STAMM", "BWT",
                Verbinden(new[] { "Bezeichner", "Beschreibung" }, Stunden168())),
            new Katalogtabelle("Tab_Prozesswaerme_STAMM", "PW",
                Verbinden(new[] { "Bezeichner", "Typ", "Beschreibung" }, Monate12(), new[] { "Vorlauf", "Ruecklauf" })),
            new Katalogtabelle("Tab_Prozesstyp_STAMM", "PWT",
                Verbinden(new[] { "Bezeichner", "Beschreibung" }, Stunden168())),
        };

        /// <summary>Die Katalogtabellen der Stufe 1 in fester Folge.</summary>
        public static IReadOnlyList<Katalogtabelle> Stufe1 => STUFE1;

        /// <summary>Die Tabelle der Stufe 1 mit diesem Namen oder <c>null</c>.</summary>
        public static Katalogtabelle Tabelle(string name) =>
            STUFE1.FirstOrDefault(t => string.Equals(t.Tabelle, name, StringComparison.OrdinalIgnoreCase));

        // =================================================================================
        // Prüfsumme
        // =================================================================================

        /// <summary>
        /// Ein Wert in seiner Prüfsummenform: Zahlen als „n:" mit invarianter, rundlauffester
        /// Darstellung (ganzzahlige Gleitkommazahlen wie Ganzzahlen, Wahrheitswerte als 1/0), Text
        /// als „s:" unverändert. <c>null</c> = leer (trägt nichts bei).
        /// </summary>
        public static string Normiert(object wert)
        {
            if (wert == null || wert == DBNull.Value) return null;
            switch (wert)
            {
                case string s: return "s:" + s;
                case bool b: return "n:" + (b ? "1" : "0");
                case byte or sbyte or short or ushort or int or uint or long:
                    return "n:" + Convert.ToInt64(wert, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                case ulong u: return "n:" + u.ToString(CultureInfo.InvariantCulture);
                case float f: return Zahl(f);
                case double d: return Zahl(d);
                case decimal m: return Zahl((double)m);
                case DateTime t: return "s:" + t.ToString("o", CultureInfo.InvariantCulture);
                case byte[] bytes: return "b:" + Convert.ToBase64String(bytes);
                default: return "s:" + Convert.ToString(wert, CultureInfo.InvariantCulture);
            }
        }

        private static string Zahl(double d)
        {
            if (!double.IsNaN(d) && !double.IsInfinity(d) && Math.Floor(d) == d && Math.Abs(d) < 9.0e15)
                return "n:" + ((long)d).ToString(CultureInfo.InvariantCulture);
            return "n:" + d.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>Die Zeile „Name=Wert" je belegter Spalte, in der Folge von <paramref name="spalten"/>.</summary>
        private static void Zeilen(StringBuilder sb, IEnumerable<string> spalten, IReadOnlyDictionary<string, object> werte)
        {
            foreach (string s in spalten)
            {
                if (werte == null || !werte.TryGetValue(s, out object w)) continue;
                string n = Normiert(w);
                if (n == null) continue;
                sb.Append(s).Append('=').Append(n.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r")).Append('\n');
            }
        }

        /// <summary>Eine Kindzeile als eine Textzeile (Spalten mit „;" getrennt).</summary>
        private static string Kindzeile(Katalogkind k, IReadOnlyDictionary<string, object> werte)
        {
            var sb = new StringBuilder();
            Zeilen(sb, k.Fachspalten, werte);
            return sb.ToString().Replace("\n", ";");
        }

        /// <summary>
        /// <b>Die Prüfsumme eines Satzes</b> — SHA-256 (Hex, klein) über die Fachwerte in der festen
        /// Spaltenfolge und die sortierten Kindzeilen. Unabhängig von der Reihenfolge, in der
        /// <paramref name="werte"/> und die Kindzeilen geliefert werden.
        /// </summary>
        /// <param name="t">Die Katalogtabelle.</param>
        /// <param name="werte">Spaltenname → Wert (Groß-/Kleinschreibung wie im Schema).</param>
        /// <param name="kinder">Kindtabelle → Zeilen; fehlt eine Kindtabelle, zählt sie als leer.</param>
        public static string Pruefsumme(Katalogtabelle t, IReadOnlyDictionary<string, object> werte,
                                        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>> kinder = null)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            var sb = new StringBuilder();
            sb.Append("#").Append(t.Tabelle).Append('\n');
            Zeilen(sb, t.Fachspalten, werte);
            foreach (Katalogkind k in t.Kinder)
            {
                sb.Append("#").Append(k.Tabelle).Append('\n');
                IReadOnlyList<IReadOnlyDictionary<string, object>> zeilen = null;
                kinder?.TryGetValue(k.Tabelle, out zeilen);
                var texte = (zeilen ?? new IReadOnlyDictionary<string, object>[0]).Select(z => Kindzeile(k, z)).ToList();
                texte.Sort(StringComparer.Ordinal);
                foreach (string z in texte) sb.Append(z).Append('\n');
            }
            return Hex(sb.ToString());
        }

        private static string Hex(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(new UTF8Encoding(false).GetBytes(text));
                var sb = new StringBuilder(64);
                foreach (byte b in h) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        // =================================================================================
        // Schlüssel
        // =================================================================================

        /// <summary>
        /// Der Schlüsselstamm aus Kürzel und bereinigtem Bezeichner: Umlaute ausgeschrieben, alles
        /// außer Buchstaben A–Z und Ziffern als „_", Folgen zusammengezogen, groß geschrieben, höchstens
        /// <see cref="SCHLUESSEL_MAX"/> − 8 Zeichen (Platz für den Zähler).
        /// </summary>
        public static string Schluesselstamm(Katalogtabelle t, string bezeichner)
        {
            string b = (bezeichner ?? "").Trim()
                .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue")
                .Replace("Ä", "Ae").Replace("Ö", "Oe").Replace("Ü", "Ue").Replace("ß", "ss")
                .ToUpperInvariant();
            var sb = new StringBuilder();
            bool strich = false;
            foreach (char c in b)
            {
                bool gut = (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
                if (gut) { sb.Append(c); strich = false; }
                else if (!strich && sb.Length > 0) { sb.Append('_'); strich = true; }
            }
            string rumpf = sb.ToString().TrimEnd('_');
            if (rumpf.Length == 0) rumpf = "SATZ";
            string stamm = t.Kuerzel + ":" + rumpf;
            int max = SCHLUESSEL_MAX - 8;
            return stamm.Length > max ? stamm.Substring(0, max).TrimEnd('_') : stamm;
        }

        /// <summary>
        /// Der Schlüssel eines Satzes: der Stamm, ist er belegt, mit Zähler „_2", „_3" … — der erste
        /// freie nach <paramref name="belegt"/>.
        /// </summary>
        public static string Schluessel(Katalogtabelle t, string bezeichner, Func<string, bool> belegt)
        {
            string stamm = Schluesselstamm(t, bezeichner);
            if (belegt == null || !belegt(stamm)) return stamm;
            for (int i = 2; ; i++)
            {
                string k = stamm + "_" + i.ToString(CultureInfo.InvariantCulture);
                if (!belegt(k)) return k;
            }
        }

        // =================================================================================
        // Lesen aus der Datenbank
        // =================================================================================

        /// <summary>Sind die drei Katalogspalten an <paramref name="tabelle"/> angelegt?</summary>
        public static bool SpaltenVorhanden(string tabelle) =>
            DataRepository.SpalteVorhanden(tabelle, SPALTE_SCHLUESSEL) &&
            DataRepository.SpalteVorhanden(tabelle, SPALTE_PRUEFSUMME) &&
            DataRepository.SpalteVorhanden(tabelle, SPALTE_AUSGELAUFEN);

        /// <summary>Die Fachspalten von <paramref name="t"/>, die die geöffnete Datenbank führt.</summary>
        internal static List<string> VorhandeneFachspalten(Katalogtabelle t) =>
            t.Fachspalten.Where(s => DataRepository.SpalteVorhanden(t.Tabelle, s)).ToList();

        /// <summary>Die Fachwerte einer gelesenen Zeile.</summary>
        internal static Dictionary<string, object> Werte(IEnumerable<string> spalten, DataRow r)
        {
            var d = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (string s in spalten)
            {
                if (!r.Table.Columns.Contains(s)) continue;
                object w = r[s];
                d[s] = w == DBNull.Value ? null : w;
            }
            return d;
        }

        /// <summary>Das SELECT der Spalten (Namen in Anführungszeichen, auch mit Umlaut).</summary>
        internal static string Spaltentext(IEnumerable<string> spalten) =>
            string.Join(", ", spalten.Select(s => "\"" + s + "\""));

        /// <summary>
        /// Die Kindzeilen eines Satzes je Kindtabelle — nur Katalogzeilen, nie Projektkopien. Eine
        /// Kindtabelle, die die Datenbank nicht führt, fehlt im Ergebnis.
        /// </summary>
        internal static Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>> Kinder(
            Katalogtabelle t, long id, Func<string, DbParam[], DataTable> lese)
        {
            var d = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>>(StringComparer.Ordinal);
            foreach (Katalogkind k in t.Kinder)
            {
                if (!DataRepository.TabelleVorhanden(k.Tabelle)) continue;
                List<string> spalten = k.Fachspalten.Where(s => DataRepository.SpalteVorhanden(k.Tabelle, s)).ToList();
                if (spalten.Count == 0) continue;
                DataTable dt = lese("SELECT " + Spaltentext(spalten) + " FROM \"" + k.Tabelle + "\" WHERE \"" +
                                    k.Fremdschluessel + "\" = ?" + k.Katalogbedingung + " ORDER BY ID",
                                    new[] { new DbParam("@id", id) });
                var zeilen = new List<IReadOnlyDictionary<string, object>>();
                if (dt != null) foreach (DataRow r in dt.Rows) zeilen.Add(Werte(spalten, r));
                d[k.Tabelle] = zeilen;
            }
            return d;
        }

        /// <summary>Die Prüfsumme eines gelesenen Kopfsatzes samt seiner Kindzeilen.</summary>
        internal static string PruefsummeDerZeile(Katalogtabelle t, List<string> spalten, DataRow r,
                                                  Func<string, DbParam[], DataTable> lese)
        {
            long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
            return Pruefsumme(t, Werte(spalten, r), Kinder(t, id, lese));
        }

        /// <summary>Lesen über die Zugriffsschicht ohne Vorgang.</summary>
        internal static DataTable LeseOhneVorgang(string sql, DbParam[] p) => DataRepository.GetDataTable(sql, p);
    }
}
