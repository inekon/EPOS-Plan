using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // NETZVERLUSTE JE KANAL, ZIRKULATION IM BESTANDSWEG, BETRIEBSKALENDER - Welle M3b der
    // Entscheidungsvorlage Modellgrenzen, Punkte BW4, PW2 und BW2 (Wellenplan vom Anwender
    // freigegeben). Regeln: Konzept Simulationsablauf, Abschnitt 17.
    //
    // DREI TEILE IN EINEM SCHRITT:
    //
    // (1) Tab_Einstellungen, acht nullbare Spalten:
    //       Netzverluste_Heizung / _Brauchwasser / _Prozess            REAL  >= 0
    //       Netzverluste_Heizung_Einheit / _Brauchwasser_ / _Prozess_   TEXT  '%' oder 'kWh/a'
    //         (ein Prozentwert hoechstens 100; Wert und Einheit beide leer oder beide gesetzt)
    //       Zirkulation_Leistung_kW                                    REAL  0 … 100
    //       Zirkulation_Laufzeit_h_d                                   REAL  0 … 24
    //     Alle drei Kanalwerte leer = der Projektwert Netzverluste/NetzverlusteEinheit mit seiner
    //     anteiligen Verteilung, Zeichen fuer Zeichen wie zuvor.
    //
    // (2) Tab_Betriebskalender (STRICT, AUTOINCREMENT): ein Kalender je Zeile, projektuebergreifend
    //     wie ein Katalog - Bezeichner, Bundesland (NULL = nur die neun bundeseinheitlichen
    //     Feiertage), vier Ferienzeitraeume als Jahrestag 1 … 365 (Beginn > Ende = ueber den
    //     Jahreswechsel), Ferienfaktor 0 … 1, Feiertag_wie_Sonntag 0/1, Ferien_kuerzen 0/1.
    //
    // (3) Je Zuordnungstabelle (Z_Projekt_Brauchwasser, Z_Projekt_Prozesswaerme,
    //     Z_Projekt_Stromverbraucher) die nullbare Spalte ID_Betriebskalender mit Fremdschluessel
    //     ON DELETE SET NULL. Leer = das Wochenprofil gilt fuer alle Wochen wie zuvor.
    //
    // KEIN DML AN BESTANDSZEILEN. Jede Bestandszeile bleibt leer, die neue Tabelle leer; der
    // Referenzlauf bleibt byte-gleich.
    //
    // DER NAME STEHT NUR ALS ARGUMENT (Muster AufheizvorgabeSchema): Die Anweisungen entstehen
    // aus Tabelle, Spalte und Typ; Werkzeuge/SqlDialektPruefer sieht keinen fertigen Text, der
    // gegen die bereits migrierte Testdatenbank „duplicate column" waere.
    //
    // VIER LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema, die Testvorrichtung samt
    // Nachweis in EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Netzverluste je Kanal, Zirkulation im Bestandsweg und Betriebskalender der Bedarfsprofile
    /// (Entscheidungsvorlage Modellgrenzen BW4, PW2, BW2) — EINE Quelle für Migration, Werkzeug,
    /// Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class BedarfNetzKalenderSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste
        /// Schritt hinter dem Temperaturpaar je Prozess (<see cref="ProzesswaermeTemperaturSchema"/>).
        /// Wird der Schritt beim Zusammenführen umnummeriert, ändert sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = ProzesswaermeTemperaturSchema.SCHRITT + 1;

        // =================================================================
        //  Teil 1 — Tab_Einstellungen
        // =================================================================

        /// <summary>Die Projekteinstellungen.</summary>
        public const string TAB_EINSTELLUNGEN = SchemaKatalog.TAB_EINSTELLUNGEN;

        /// <summary>Netzverlust des Heizkanals (Wert); NULL = kein Kanalwert.</summary>
        public const string SPALTE_NV_HEIZUNG = "Netzverluste_Heizung";

        /// <summary>Einheit des Heizkanalwerts (<see cref="EINHEIT_PROZENT"/> oder <see cref="EINHEIT_KWH"/>).</summary>
        public const string SPALTE_NV_HEIZUNG_EINHEIT = "Netzverluste_Heizung_Einheit";

        /// <summary>Netzverlust des Brauchwasserkanals (Wert); NULL = kein Kanalwert.</summary>
        public const string SPALTE_NV_BRAUCHWASSER = "Netzverluste_Brauchwasser";

        /// <summary>Einheit des Brauchwasserkanalwerts.</summary>
        public const string SPALTE_NV_BRAUCHWASSER_EINHEIT = "Netzverluste_Brauchwasser_Einheit";

        /// <summary>Netzverlust des Prozesskanals (Wert); NULL = kein Kanalwert.</summary>
        public const string SPALTE_NV_PROZESS = "Netzverluste_Prozess";

        /// <summary>Einheit des Prozesskanalwerts.</summary>
        public const string SPALTE_NV_PROZESS_EINHEIT = "Netzverluste_Prozess_Einheit";

        /// <summary>Zirkulationsleistung des Bestandswegs [kW], 0 … 100; NULL = keine Zirkulation.</summary>
        public const string SPALTE_ZIRK_LEISTUNG = "Zirkulation_Leistung_kW";

        /// <summary>Laufzeit der Zirkulation [h/d], 0 … 24; NULL = keine Zirkulation.</summary>
        public const string SPALTE_ZIRK_LAUFZEIT = "Zirkulation_Laufzeit_h_d";

        /// <summary>Einheit „Prozent des Jahresbedarfs des Kanals".</summary>
        public const string EINHEIT_PROZENT = "%";

        /// <summary>Einheit „feste Jahresmenge".</summary>
        public const string EINHEIT_KWH = "kWh/a";

        /// <summary>Obergrenze der Zirkulationsleistung [kW].</summary>
        public const double ZIRK_LEISTUNG_MAX_KW = 100;

        /// <summary>Obergrenze der Laufzeit [h/d].</summary>
        public const double ZIRK_LAUFZEIT_MAX_H = 24;

        /// <summary>Die Typdefinition eines Kanalwerts: nullbar, nicht negativ.</summary>
        private static string TypWert(string spalte) =>
            "REAL CHECK (\"" + spalte + "\" IS NULL OR \"" + spalte + "\" >= 0)";

        /// <summary>
        /// Die Typdefinition einer Kanaleinheit: nullbar, Wertliste, paarweise mit dem Wert, ein
        /// Prozentwert höchstens 100. Die Klauseln stehen an der ZWEITEN Spalte, weil ALTER TABLE
        /// die Spalten nacheinander anlegt.
        /// </summary>
        private static string TypEinheit(string wert, string einheit) =>
            "TEXT CHECK (\"" + einheit + "\" IS NULL OR \"" + einheit + "\" IN ('" + EINHEIT_PROZENT + "','" +
            EINHEIT_KWH + "')) " +
            "CHECK ((\"" + wert + "\" IS NULL) = (\"" + einheit + "\" IS NULL)) " +
            "CHECK (\"" + einheit + "\" IS NULL OR \"" + einheit + "\" <> '" + EINHEIT_PROZENT + "' OR \"" +
            wert + "\" <= 100)";

        /// <summary>Die acht Spalten an Tab_Einstellungen samt Typ, in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> SPALTEN_EINSTELLUNGEN = new[]
        {
            new KeyValuePair<string, string>(SPALTE_NV_HEIZUNG, TypWert(SPALTE_NV_HEIZUNG)),
            new KeyValuePair<string, string>(SPALTE_NV_HEIZUNG_EINHEIT, TypEinheit(SPALTE_NV_HEIZUNG, SPALTE_NV_HEIZUNG_EINHEIT)),
            new KeyValuePair<string, string>(SPALTE_NV_BRAUCHWASSER, TypWert(SPALTE_NV_BRAUCHWASSER)),
            new KeyValuePair<string, string>(SPALTE_NV_BRAUCHWASSER_EINHEIT, TypEinheit(SPALTE_NV_BRAUCHWASSER, SPALTE_NV_BRAUCHWASSER_EINHEIT)),
            new KeyValuePair<string, string>(SPALTE_NV_PROZESS, TypWert(SPALTE_NV_PROZESS)),
            new KeyValuePair<string, string>(SPALTE_NV_PROZESS_EINHEIT, TypEinheit(SPALTE_NV_PROZESS, SPALTE_NV_PROZESS_EINHEIT)),
            new KeyValuePair<string, string>(SPALTE_ZIRK_LEISTUNG,
                "REAL CHECK (\"" + SPALTE_ZIRK_LEISTUNG + "\" IS NULL OR (\"" + SPALTE_ZIRK_LEISTUNG + "\" >= 0 AND \"" +
                SPALTE_ZIRK_LEISTUNG + "\" <= 100))"),
            new KeyValuePair<string, string>(SPALTE_ZIRK_LAUFZEIT,
                "REAL CHECK (\"" + SPALTE_ZIRK_LAUFZEIT + "\" IS NULL OR (\"" + SPALTE_ZIRK_LAUFZEIT + "\" >= 0 AND \"" +
                SPALTE_ZIRK_LAUFZEIT + "\" <= 24))"),
        };

        // =================================================================
        //  Teil 2 — Tab_Betriebskalender
        // =================================================================

        /// <summary>Die Kalendertabelle.</summary>
        public const string TAB_KALENDER = "Tab_Betriebskalender";

        /// <summary>Bezeichner, NOT NULL, 1 … 100 Zeichen.</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>Länderkennung (<see cref="Landesfeiertage.BUNDESLAENDER"/>); NULL = nur bundeseinheitliche Feiertage.</summary>
        public const string SPALTE_BUNDESLAND = "Bundesland";

        /// <summary>Ferienfaktor f, 0 … 1, NOT NULL DEFAULT 0.</summary>
        public const string SPALTE_FERIENFAKTOR = "Ferienfaktor";

        /// <summary>Feiertag wie Sonntag, 0/1, NOT NULL DEFAULT 1.</summary>
        public const string SPALTE_FEIERTAG_WIE_SONNTAG = "Feiertag_wie_Sonntag";

        /// <summary>Ferien kürzen die Monatsmenge, 0/1, NOT NULL DEFAULT 0.</summary>
        public const string SPALTE_FERIEN_KUERZEN = "Ferien_kuerzen";

        /// <summary>Zahl der Ferienzeiträume je Kalender.</summary>
        public const int FERIEN_ANZAHL = 4;

        /// <summary>Beginn des Ferienzeitraums <paramref name="i"/> (1 … 4) als Jahrestag.</summary>
        public static string SpalteFerienVon(int i) => "Ferien" + i + "_Von";

        /// <summary>Ende des Ferienzeitraums <paramref name="i"/> (1 … 4) als Jahrestag.</summary>
        public static string SpalteFerienBis(int i) => "Ferien" + i + "_Bis";

        /// <summary>Die Wertliste der Bundesländer als SQL-Literal.</summary>
        private static string WerteBundesland() =>
            string.Join(",", Landesfeiertage.BUNDESLAENDER.Select(b => "'" + b + "'"));

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Betriebskalender</c> — STRICT, AUTOINCREMENT.</summary>
        public static string SqlCreateKalender()
        {
            var s = new System.Text.StringBuilder();
            s.Append("CREATE TABLE IF NOT EXISTS \"" + TAB_KALENDER + "\" (\n");
            s.Append("    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n");
            s.Append("    \"" + SPALTE_BEZEICHNER + "\" TEXT NOT NULL CHECK (length(\"" + SPALTE_BEZEICHNER +
                     "\") BETWEEN 1 AND 100),\n");
            s.Append("    \"" + SPALTE_BUNDESLAND + "\" TEXT CHECK (\"" + SPALTE_BUNDESLAND + "\" IS NULL OR \"" +
                     SPALTE_BUNDESLAND + "\" IN (" + WerteBundesland() + ")),\n");
            for (int i = 1; i <= FERIEN_ANZAHL; i++)
            {
                string von = SpalteFerienVon(i), bis = SpalteFerienBis(i);
                s.Append("    \"" + von + "\" INTEGER CHECK (\"" + von + "\" IS NULL OR \"" + von + "\" BETWEEN 1 AND 365),\n");
                s.Append("    \"" + bis + "\" INTEGER CHECK (\"" + bis + "\" IS NULL OR \"" + bis + "\" BETWEEN 1 AND 365),\n");
            }
            s.Append("    \"" + SPALTE_FERIENFAKTOR + "\" REAL NOT NULL DEFAULT 0 CHECK (\"" + SPALTE_FERIENFAKTOR +
                     "\" BETWEEN 0 AND 1),\n");
            s.Append("    \"" + SPALTE_FEIERTAG_WIE_SONNTAG + "\" INTEGER NOT NULL DEFAULT 1 CHECK (\"" +
                     SPALTE_FEIERTAG_WIE_SONNTAG + "\" IN (0,1)),\n");
            s.Append("    \"" + SPALTE_FERIEN_KUERZEN + "\" INTEGER NOT NULL DEFAULT 0 CHECK (\"" +
                     SPALTE_FERIEN_KUERZEN + "\" IN (0,1))");
            for (int i = 1; i <= FERIEN_ANZAHL; i++)
                s.Append(",\n    CHECK ((\"" + SpalteFerienVon(i) + "\" IS NULL) = (\"" + SpalteFerienBis(i) + "\" IS NULL))");
            s.Append("\n) STRICT");
            return s.ToString();
        }

        // =================================================================
        //  Teil 3 — die drei Zuordnungstabellen
        // =================================================================

        /// <summary>Die Spalte der Zuordnungszeile, die auf einen Betriebskalender zeigt.</summary>
        public const string SPALTE_ID_KALENDER = "ID_Betriebskalender";

        /// <summary>Die drei Zuordnungstabellen in dieser Reihenfolge: Brauchwasser, Prozesswärme, Strom.</summary>
        public static readonly IReadOnlyList<string> ZUORDNUNGEN = new[]
        {
            "Z_Projekt_Brauchwasser", "Z_Projekt_Prozesswaerme", "Z_Projekt_Stromverbraucher"
        };

        /// <summary>Die Typdefinition der Kalenderspalte: nullbar, Fremdschlüssel mit SET NULL.</summary>
        public const string TYP_ID_KALENDER =
            "INTEGER REFERENCES \"" + TAB_KALENDER + "\" (\"ID\") ON DELETE SET NULL";

        // =================================================================
        //  Stand und Anweisungen
        // =================================================================

        /// <summary>Steht alles — Tabelle, acht Spalten, drei Kalenderspalten?</summary>
        public static bool Vollstaendig()
        {
            if (!DataRepository.TabelleVorhanden(TAB_KALENDER)) return false;
            foreach (KeyValuePair<string, string> s in SPALTEN_EINSTELLUNGEN)
                if (!DataRepository.SpalteVorhanden(TAB_EINSTELLUNGEN, s.Key)) return false;
            foreach (string z in ZUORDNUNGEN)
                if (!DataRepository.SpalteVorhanden(z, SPALTE_ID_KALENDER)) return false;
            return true;
        }

        /// <summary>Stehen die Kalenderspalten an allen drei Zuordnungstabellen? (Leser im Rechenweg)</summary>
        public static bool KalenderspaltenVorhanden()
        {
            foreach (string z in ZUORDNUNGEN)
                if (!DataRepository.SpalteVorhanden(z, SPALTE_ID_KALENDER)) return false;
            return true;
        }

        /// <summary>
        /// Die Anweisungen des Schritts — Beschreibung und SQL, je fehlender Teil eine; leer, wenn
        /// alles steht (<b>wiederholbar</b>). Die Tabelle zuerst: Die Kalenderspalten zeigen auf sie.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                if (!DataRepository.TabelleVorhanden(TAB_KALENDER))
                    yield return new KeyValuePair<string, string>(TAB_KALENDER + " anlegen", SqlCreateKalender());

                foreach (KeyValuePair<string, string> s in SPALTEN_EINSTELLUNGEN)
                {
                    if (DataRepository.SpalteVorhanden(TAB_EINSTELLUNGEN, s.Key)) continue;
                    yield return new KeyValuePair<string, string>(
                        TAB_EINSTELLUNGEN + "." + s.Key + " anlegen",
                        "ALTER TABLE \"" + TAB_EINSTELLUNGEN + "\" ADD COLUMN \"" + s.Key + "\" " + s.Value);
                }

                foreach (string z in ZUORDNUNGEN)
                {
                    if (DataRepository.SpalteVorhanden(z, SPALTE_ID_KALENDER)) continue;
                    yield return new KeyValuePair<string, string>(
                        z + "." + SPALTE_ID_KALENDER + " anlegen",
                        "ALTER TABLE \"" + z + "\" ADD COLUMN \"" + SPALTE_ID_KALENDER + "\" " + TYP_ID_KALENDER);
                }
            }
        }

        /// <summary>
        /// Führt den Schritt aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen
        /// Helfer, aus derselben <see cref="Anweisungen"/>.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der Handgriffe (0, wenn alles stand).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            List<KeyValuePair<string, string>> offen = Anweisungen.ToList();
            if (offen.Count == 0)
            {
                bericht?.Add(TAB_KALENDER + ", Netzverluste je Kanal und Kalenderspalten vorhanden");
                return 0;
            }
            foreach (KeyValuePair<string, string> a in offen)
            {
                DataRepository.ExecuteNonQuery(a.Value);
                bericht?.Add(a.Key);
            }
            return offen.Count;
        }
    }
}
