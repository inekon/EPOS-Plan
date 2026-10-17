using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // K1 - KAELTEBEDARF OHNE GEBAEUDEMODELL (Schritt 213; Konzept Kaeltebedarf, Abschnitt 5, Entscheide E-K1 bis E-K3).
    //
    // WOZU. Kaeltebedarf wird wie Prozesswaerme als Profil (Typkatalog mit 168 Wochenstunden, Kopf mit zwoelf
    // Monatswerten und Jahressumme an der Zuordnung) gepflegt; jede Zuordnungszeile - Profil wie Kuehllastgang -
    // traegt ihre Deckungsart: zentral (Kaeltefolge) oder dezentral (Split) mit EER, Kuehltraeger und Zaehler.
    //
    //   Tab_Kaeltebedarf_STAMM   Kopfkatalog (ReadOnly, Katalogspalten der Fassung)
    //   Tab_Kaeltetyp_STAMM      Typkatalog, "1".."168"
    //   Tab_Kaeltebedarf         Projektkopie des Kopfs, ID_Stamm -> Katalog (ON DELETE SET NULL)
    //   Tab_Kaeltetyp            Projektkopie des Typs, an der Kopfkopie (CASCADE)
    //   Z_Projekt_Kaeltebedarf   Zuordnung: Summe [MWh], Betriebskalender, Deckungsspalten
    //   Z_ProjektWaermebedarf    + Deckungsspalten (wirksam nur fuer Kanal Kuehlung; Vorgabe 'zentral')
    //   Tab_ErgebnisEnergiebedarf + fuenf Ergebnisspalten, leer
    //
    // Vorlauf/Ruecklauf sind eine ANGABE ohne Wirkung auf die Erzeuger (E-K3): -40 ... 100 Grad C, beide oder
    // keiner, Vorlauf <= Ruecklauf (Kaelte: der Vorlauf ist die kalte Seite).
    //
    // KEIN DML an Bestandszeilen ausser der Saat (KaeltetypSaat) und den Katalogschluesseln ihrer Saetze.
    // Die neuen Tabellen sind in den Referenzprojekten leer, die Deckungsspalten an Z_ProjektWaermebedarf
    // stehen auf 'zentral'; der Referenzlauf bleibt byte-gleich. Alles DDL in EINEM Vorgang; wiederholbar.
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// <b>K1</b> — Kältebedarfsprofile, Typkatalog, Zuordnung mit Deckungsart. EINE Quelle für Migration, Werkzeug,
    /// Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class KaeltebedarfSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 213).</summary>
        public const int SCHRITT = KaelteRangSchema.SCHRITT + 1;

        /// <summary>Der Kopfkatalog.</summary>
        public const string TAB_KOPF_STAMM = "Tab_Kaeltebedarf_STAMM";

        /// <summary>Der Typkatalog der Wochenprofile.</summary>
        public const string TAB_TYP_STAMM = "Tab_Kaeltetyp_STAMM";

        /// <summary>Die Projektkopie des Kopfs.</summary>
        public const string TAB_KOPF = "Tab_Kaeltebedarf";

        /// <summary>Die Projektkopie des Typs.</summary>
        public const string TAB_TYP = "Tab_Kaeltetyp";

        /// <summary>Die Zuordnung Projekt ↔ Kopfkopie.</summary>
        public const string TAB_ZUORDNUNG = "Z_Projekt_Kaeltebedarf";

        /// <summary>Die Zuordnung der externen Lastgänge (Kanal Kühlung trägt die Deckungsart).</summary>
        public const string TAB_LASTGANG = "Z_ProjektWaermebedarf";

        /// <summary>Die Ergebnistabelle des Energiebedarfs.</summary>
        public const string TAB_ERGEBNIS = "Tab_ErgebnisEnergiebedarf";

        /// <summary>Der Verweis der Zuordnung und der Typkopie auf die Kopfkopie.</summary>
        public const string SPALTE_ID_KAELTEBEDARF = "ID_Kaeltebedarf";

        /// <summary>Der Verweis der Projektkopie auf ihren Katalogsatz.</summary>
        public const string SPALTE_ID_STAMM = "ID_Stamm";

        /// <summary>Vorlauf [°C] — Angabe ohne Wirkung.</summary>
        public const string SPALTE_VORLAUF = "Vorlauf";

        /// <summary>Rücklauf [°C] — Angabe ohne Wirkung.</summary>
        public const string SPALTE_RUECKLAUF = "Ruecklauf";

        /// <summary>Deckungsart: <see cref="DECKUNG_ZENTRAL"/> oder <see cref="DECKUNG_SPLIT"/>.</summary>
        public const string SPALTE_DECKUNG = "Deckung";

        /// <summary>EER-Weg bei Split: <see cref="EER_FEST"/> oder <see cref="EER_LINEAR"/>; leer bei zentral.</summary>
        public const string SPALTE_EER_WEG = "Split_EER_Weg";

        /// <summary>Fester EER bzw. EER am Punkt 1.</summary>
        public const string SPALTE_EER_1 = "Split_EER_1";

        /// <summary>Außentemperatur am Punkt 1 [°C].</summary>
        public const string SPALTE_TAUSSEN_1 = "Split_Taussen_1";

        /// <summary>EER am Punkt 2 (nur linear).</summary>
        public const string SPALTE_EER_2 = "Split_EER_2";

        /// <summary>Außentemperatur am Punkt 2 [°C] (nur linear).</summary>
        public const string SPALTE_TAUSSEN_2 = "Split_Taussen_2";

        /// <summary>Kühlträger des Split-Stroms; leer = Stromträger des Projekts.</summary>
        public const string SPALTE_KUEHL_ID_CARRIER = KuehlungSchema.SPALTE_KUEHL_ID_CARRIER;

        /// <summary>Eigener Zähler des Split-Stroms (0/1).</summary>
        public const string SPALTE_KUEHL_EIGENER_ZAEHLER = KuehlungSchema.SPALTE_KUEHL_EIGENER_ZAEHLER;

        /// <summary>Deckungsart „zentral (Kältefolge)“ — die Vorgabe.</summary>
        public const string DECKUNG_ZENTRAL = "zentral";

        /// <summary>Deckungsart „dezentral (Split)“.</summary>
        public const string DECKUNG_SPLIT = "split";

        /// <summary>EER-Weg „fest“.</summary>
        public const string EER_FEST = "fest";

        /// <summary>EER-Weg „linear über der Außentemperatur“.</summary>
        public const string EER_LINEAR = "linear";

        /// <summary>Untere Grenze der Außentemperatur eines EER-Punkts [°C].</summary>
        public const double TAUSSEN_MIN = -20;

        /// <summary>Obere Grenze der Außentemperatur eines EER-Punkts [°C].</summary>
        public const double TAUSSEN_MAX = 50;

        /// <summary>Untere Grenze des Temperaturpaars [°C].</summary>
        public const double TEMPERATUR_MIN = -40;

        /// <summary>Obere Grenze des Temperaturpaars [°C].</summary>
        public const double TEMPERATUR_MAX = 100;

        /// <summary>Die fünf Ergebnisspalten an <see cref="TAB_ERGEBNIS"/> (nullbar, leer bis K3).</summary>
        public static readonly IReadOnlyList<string> ERGEBNISSPALTEN = new[]
        {
            "Kaeltebedarf_Profil_MWh", "Kaeltebedarf_Dezentral_MWh", "Kaelte_Dezentral_Spitze_kW",
            "Strom_Dezentral_MWh", "Jahres_EER_Dezentral"
        };

        private static string Q(string s) => "\"" + s + "\"";

        private static string Zahl(double d) => d.ToString(CultureInfo.InvariantCulture);

        /// <summary>Die acht Deckungsspalten: Name und Typ samt Klausel (gleich an beiden Zuordnungen).</summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> DECKUNGSSPALTEN = new[]
        {
            (SPALTE_DECKUNG, "TEXT NOT NULL DEFAULT '" + DECKUNG_ZENTRAL + "' CHECK (" + Q(SPALTE_DECKUNG) + " IN ('" +
                             DECKUNG_ZENTRAL + "','" + DECKUNG_SPLIT + "'))"),
            (SPALTE_EER_WEG, "TEXT CHECK (" + Q(SPALTE_EER_WEG) + " IS NULL OR " + Q(SPALTE_EER_WEG) + " IN ('" +
                             EER_FEST + "','" + EER_LINEAR + "'))"),
            (SPALTE_EER_1, "REAL CHECK (" + Q(SPALTE_EER_1) + " IS NULL OR " + Q(SPALTE_EER_1) + " > 0)"),
            (SPALTE_TAUSSEN_1, "REAL CHECK (" + Q(SPALTE_TAUSSEN_1) + " IS NULL OR (" + Q(SPALTE_TAUSSEN_1) + " >= " +
                               Zahl(TAUSSEN_MIN) + " AND " + Q(SPALTE_TAUSSEN_1) + " <= " + Zahl(TAUSSEN_MAX) + "))"),
            (SPALTE_EER_2, "REAL CHECK (" + Q(SPALTE_EER_2) + " IS NULL OR " + Q(SPALTE_EER_2) + " > 0)"),
            (SPALTE_TAUSSEN_2, "REAL CHECK (" + Q(SPALTE_TAUSSEN_2) + " IS NULL OR (" + Q(SPALTE_TAUSSEN_2) + " >= " +
                               Zahl(TAUSSEN_MIN) + " AND " + Q(SPALTE_TAUSSEN_2) + " <= " + Zahl(TAUSSEN_MAX) + "))"),
            (SPALTE_KUEHL_ID_CARRIER, KuehlungSchema.TYP_KUEHL_ID_CARRIER),
            (SPALTE_KUEHL_EIGENER_ZAEHLER, "INTEGER NOT NULL DEFAULT 0 CHECK (" + Q(SPALTE_KUEHL_EIGENER_ZAEHLER) + " IN (0,1))"),
        };

        private static string Monate()
        {
            var sb = new StringBuilder();
            for (int m = 1; m <= 12; m++) sb.Append("    \"Monat_" + m.ToString(CultureInfo.InvariantCulture) + "\" REAL,\n");
            return sb.ToString();
        }

        private static string Stunden()
        {
            var sb = new StringBuilder();
            for (int h = 1; h <= 168; h++) sb.Append("    \"" + h.ToString(CultureInfo.InvariantCulture) + "\" REAL,\n");
            return sb.ToString();
        }

        private static string Temperatur(string spalte) =>
            "    " + Q(spalte) + " REAL CHECK (" + Q(spalte) + " IS NULL OR (" + Q(spalte) + " >= " + Zahl(TEMPERATUR_MIN) +
            " AND " + Q(spalte) + " <= " + Zahl(TEMPERATUR_MAX) + ")),\n";

        private static string Paarklauseln() =>
            "    CHECK ((" + Q(SPALTE_VORLAUF) + " IS NULL) = (" + Q(SPALTE_RUECKLAUF) + " IS NULL)),\n" +
            "    CHECK (" + Q(SPALTE_VORLAUF) + " IS NULL OR " + Q(SPALTE_VORLAUF) + " <= " + Q(SPALTE_RUECKLAUF) + ")";

        private static string Katalogspalten()
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, string> s in KatalogfassungSchema.Katalogspalten())
                sb.Append("    " + Q(s.Key) + " " + s.Value + ",\n");
            return sb.ToString();
        }

        private const string READONLY = "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)),\n";

        private const string PROJEKT_FK =
            "    \"ID_Projekt\" INTEGER NOT NULL REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Kaeltebedarf_STAMM</c>.</summary>
        public static string SqlKopfStamm() =>
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_KOPF_STAMM) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Bezeichner\" TEXT NOT NULL,\n" +
            "    \"Typ\" TEXT,\n" +
            "    \"Beschreibung\" TEXT,\n" +
            Monate() + READONLY + Temperatur(SPALTE_VORLAUF) + Temperatur(SPALTE_RUECKLAUF) + Katalogspalten() +
            Paarklauseln() + "\n) STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Kaeltetyp_STAMM</c>.</summary>
        public static string SqlTypStamm()
        {
            string k = Katalogspalten();
            return "CREATE TABLE IF NOT EXISTS " + Q(TAB_TYP_STAMM) + " (\n" +
                   "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                   "    \"Bezeichner\" TEXT NOT NULL,\n" +
                   "    \"Beschreibung\" TEXT,\n" +
                   Stunden() + READONLY + k.Substring(0, k.Length - 2) + "\n) STRICT";
        }

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Kaeltebedarf</c> — die Projektkopie des Kopfs.</summary>
        public static string SqlKopf() =>
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_KOPF) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            PROJEKT_FK +
            "    \"Bezeichner\" TEXT NOT NULL,\n" +
            "    \"Typ\" TEXT,\n" +
            "    \"Beschreibung\" TEXT,\n" +
            Monate() + READONLY + Temperatur(SPALTE_VORLAUF) + Temperatur(SPALTE_RUECKLAUF) +
            "    " + Q(SPALTE_ID_STAMM) + " INTEGER REFERENCES " + Q(TAB_KOPF_STAMM) + " (\"ID\") ON DELETE SET NULL,\n" +
            Paarklauseln() + "\n) STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Kaeltetyp</c> — die Projektkopie des Typs.</summary>
        public static string SqlTyp()
        {
            string s = Stunden();
            return "CREATE TABLE IF NOT EXISTS " + Q(TAB_TYP) + " (\n" +
                   "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                   "    " + Q(SPALTE_ID_KAELTEBEDARF) + " INTEGER NOT NULL REFERENCES " + Q(TAB_KOPF) +
                   " (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n" +
                   PROJEKT_FK +
                   "    \"Typname\" TEXT NOT NULL,\n" +
                   "    \"Beschreibung\" TEXT,\n" +
                   READONLY + s.Substring(0, s.Length - 2) + "\n) STRICT";
        }

        /// <summary><c>CREATE TABLE IF NOT EXISTS Z_Projekt_Kaeltebedarf</c> samt Deckungsspalten.</summary>
        public static string SqlZuordnung()
        {
            var sb = new StringBuilder();
            sb.Append("CREATE TABLE IF NOT EXISTS " + Q(TAB_ZUORDNUNG) + " (\n");
            sb.Append("    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n");
            sb.Append(PROJEKT_FK);
            sb.Append("    " + Q(SPALTE_ID_KAELTEBEDARF) + " INTEGER NOT NULL REFERENCES " + Q(TAB_KOPF) +
                      " (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n");
            sb.Append("    \"Bezeichner\" TEXT,\n");
            sb.Append("    \"Summe\" REAL DEFAULT 0,\n");
            sb.Append("    " + Q(BedarfNetzKalenderSchema.SPALTE_ID_KALENDER) +
                      " INTEGER REFERENCES \"Tab_Betriebskalender\" (\"ID\") ON DELETE SET NULL");
            foreach ((string Spalte, string Typ) d in DECKUNGSSPALTEN)
                sb.Append(",\n    " + Q(d.Spalte) + " " + d.Typ);
            sb.Append("\n) STRICT");
            return sb.ToString();
        }

        /// <summary>Die Tabellen des Schritts in Anlagefolge (Verweisziele zuerst): Name und Anweisung.</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Tabellen() => new[]
        {
            new KeyValuePair<string, string>(TAB_KOPF_STAMM, SqlKopfStamm()),
            new KeyValuePair<string, string>(TAB_TYP_STAMM, SqlTypStamm()),
            new KeyValuePair<string, string>(TAB_KOPF, SqlKopf()),
            new KeyValuePair<string, string>(TAB_TYP, SqlTyp()),
            new KeyValuePair<string, string>(TAB_ZUORDNUNG, SqlZuordnung()),
        };

        /// <summary>Die Indizes nach dem Muster der Prozesswärme: Name und Anweisung (<c>IF NOT EXISTS</c>).</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Indizes() => new[]
        {
            Index("UX_" + TAB_KOPF_STAMM + "_Bezeichner", TAB_KOPF_STAMM, "\"Bezeichner\"", true),
            Index("IX_" + TAB_KOPF_STAMM + "_Typ", TAB_KOPF_STAMM, "\"Typ\"", false),
            Index("UX_" + TAB_TYP_STAMM + "_Bezeichner", TAB_TYP_STAMM, "\"Bezeichner\"", true),
            Teilindex(TAB_KOPF_STAMM),
            Teilindex(TAB_TYP_STAMM),
            Index("IX_" + TAB_KOPF + "_ID_Projekt", TAB_KOPF, "\"ID_Projekt\"", false),
            Index("IX_" + TAB_KOPF + "_Bezeichner", TAB_KOPF, "\"Bezeichner\"", false),
            Index("IX_" + TAB_KOPF + "_ID_Stamm", TAB_KOPF, Q(SPALTE_ID_STAMM), false),
            Index("IX_" + TAB_TYP + "_ID_Projekt", TAB_TYP, "\"ID_Projekt\"", false),
            Index("IX_" + TAB_TYP + "_ID_Kaeltebedarf", TAB_TYP, Q(SPALTE_ID_KAELTEBEDARF), false),
            Index("IX_" + TAB_TYP + "_Typname", TAB_TYP, "\"Typname\"", false),
            Index("IX_" + TAB_ZUORDNUNG + "_ID_Projekt", TAB_ZUORDNUNG, "\"ID_Projekt\"", false),
            Index("IX_" + TAB_ZUORDNUNG + "_ID_Kaeltebedarf", TAB_ZUORDNUNG, Q(SPALTE_ID_KAELTEBEDARF), false),
        };

        private static KeyValuePair<string, string> Index(string name, string tabelle, string spalten, bool eindeutig) =>
            new KeyValuePair<string, string>(name, "CREATE " + (eindeutig ? "UNIQUE " : "") + "INDEX IF NOT EXISTS " +
                                                   Q(name) + " ON " + Q(tabelle) + " (" + spalten + ")");

        private static KeyValuePair<string, string> Teilindex(string tabelle)
        {
            string name = KatalogfassungSchema.Indexname(tabelle);
            return new KeyValuePair<string, string>(name,
                "CREATE UNIQUE INDEX IF NOT EXISTS " + Q(name) + " ON " + Q(tabelle) + " (" +
                Q(Katalogfassung.SPALTE_SCHLUESSEL) + ") WHERE " + Q(Katalogfassung.SPALTE_SCHLUESSEL) + " IS NOT NULL");
        }

        /// <summary>Die Spalten, die per <c>ALTER TABLE ADD COLUMN</c> an bestehende Tabellen kommen.</summary>
        public static IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN { get; } =
            DECKUNGSSPALTEN.Select(d => (TAB_LASTGANG, d.Spalte, d.Typ))
                           .Concat(ERGEBNISSPALTEN.Select(e => (TAB_ERGEBNIS, e, "REAL")))
                           .ToArray();

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[]
        {
            "Tab_Projekt", "Tab_Betriebskalender", "energy_carrier", TAB_LASTGANG, TAB_ERGEBNIS
        };

        /// <summary>Die Anweisung, die eine Spalte anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE " + Q(s.Tabelle) + " ADD COLUMN " + Q(s.Spalte) + " " + s.Typ;

        /// <summary>Stehen Tabellen, Spalten und Indizes? (Wächter der Leser, etwa für einen älteren Stand auf iOS.)</summary>
        public static bool SchemaVollstaendig()
        {
            foreach (KeyValuePair<string, string> t in Tabellen())
                if (!DataRepository.TabelleVorhanden(t.Key)) return false;
            if (!SPALTEN.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte))) return false;
            return Indizes().All(i => IndexVorhanden(i.Key));
        }

        /// <summary>Schema vollständig, Saat vollständig, jeder gesäte Satz mit Schlüssel und Prüfsumme?</summary>
        public static bool Vollstaendig() =>
            SchemaVollstaendig() && KaeltetypSaat.Vollstaendig() &&
            KatalogSchluesselSaat.OffeneSaetze(Katalogfassung.Stufe4) == 0;

        /// <summary>Ist die Tabellenkette des Kältebedarfs da? (Leserwächter der Controller.)</summary>
        public static bool TabellenVorhanden() =>
            DataRepository.TabelleVorhanden(TAB_KOPF_STAMM) && DataRepository.TabelleVorhanden(TAB_ZUORDNUNG);

        private static bool IndexVorhanden(string name)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?", new DbParam("@n", name));
            return o != null && o != DBNull.Value && Convert.ToInt32(o, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// Prüft ein Temperaturpaar der Kälte (Angabe): beide oder keiner, −40 … 100 °C, Vorlauf ≤ Rücklauf.
        /// <c>null</c> = zulässig, sonst der Grund — dieselben Grenzen wie die Prüfklauseln des Schemas.
        /// </summary>
        public static string Paarpruefung(double? vorlauf, double? ruecklauf)
        {
            if (vorlauf == null && ruecklauf == null) return null;
            if (vorlauf == null || ruecklauf == null) return "Vorlauf und Ruecklauf nur gemeinsam";
            if (vorlauf < TEMPERATUR_MIN || vorlauf > TEMPERATUR_MAX || ruecklauf < TEMPERATUR_MIN || ruecklauf > TEMPERATUR_MAX)
                return "Temperatur ausserhalb " + Zahl(TEMPERATUR_MIN) + " ... " + Zahl(TEMPERATUR_MAX) + " Grad C";
            if (vorlauf > ruecklauf) return "Vorlauf ueber Ruecklauf";
            return null;
        }

        /// <summary>
        /// Führt den Schritt aus (wiederholbar): Tabellen, Indizes und Spalten in EINEM Vorgang, danach Saat und
        /// Katalogschlüssel. Rückgabe = Zahl der Schemaanweisungen. Fehlt eine Voraussetzung, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");

            var anweisungen = new List<KeyValuePair<string, string>>();
            foreach (KeyValuePair<string, string> t in Tabellen())
                if (!DataRepository.TabelleVorhanden(t.Key))
                    anweisungen.Add(new KeyValuePair<string, string>(t.Key + " angelegt", t.Value));
            foreach ((string Tabelle, string Spalte, string Typ) s in SPALTEN)
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte))
                    anweisungen.Add(new KeyValuePair<string, string>(s.Tabelle + "." + s.Spalte + " angelegt", Anlegen(s)));
            foreach (KeyValuePair<string, string> i in Indizes())
                if (!IndexVorhanden(i.Key))
                    anweisungen.Add(new KeyValuePair<string, string>("Index " + i.Key + " angelegt", i.Value));

            if (anweisungen.Count > 0)
            {
                using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
                {
                    try
                    {
                        foreach (KeyValuePair<string, string> a in anweisungen) v.Ausfuehren(a.Value);
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                if (bericht != null)
                    foreach (KeyValuePair<string, string> a in anweisungen) bericht.Add(a.Key);
            }
            else
            {
                bericht?.Add("Tabellen, Spalten und Indizes des Kaeltebedarfs stehen bereits");
            }

            KaeltetypSaat.Ausfuehren(bericht);
            KatalogSchluesselSaat.Ausfuehren(bericht, Katalogfassung.Stufe4);
            bericht?.Add("KEIN DML an Bestandsdaten ausser der Saat; Deckung der Bestandslastgaenge = '" + DECKUNG_ZENTRAL +
                         "', der Referenzlauf bleibt byte-gleich");
            return anweisungen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
