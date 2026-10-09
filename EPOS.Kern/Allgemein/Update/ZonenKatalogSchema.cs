using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // ZK - ZONEN IM GEBAEUDEKATALOG (Schemaschritt 204, Anwenderwunsch 08.10.2026).
    //
    // WAS. Ein Katalogsatz (Tab_Gebaeude_STAMM) traegt Zonen wie ein Projektgebaeude: drei
    // Katalogzwillinge der Projekttabellen mit denselben Fachspalten - Tab_Zone_STAMM (an
    // Tab_Gebaeude_STAMM, ON DELETE CASCADE), Tab_Bauteil_STAMM (an der Katalogzone, Nachbarzone und
    // Aufbau im Katalog) und Tab_Zonenluftstrom_STAMM (zwischen zwei Katalogzonen) - und an
    // Tab_Konditionierungskalender und Tab_Konditionierungsvorgabe die Eigentuemerspalte ID_Zone_Stamm
    // (an Tab_Zone_STAMM, ON DELETE CASCADE). Die Periodentabelle haengt am Kalender und braucht keine.
    //
    // WARUM ZWILLINGE. Die Projekttabellen sind STRICT und haengen mit NOT NULL an Tab_Gebaeude; ein
    // zweiter Eigentuemer dort hiesse jede der drei Tabellen neu zu bauen. Die Zwillinge folgen dem
    // Muster der uebrigen Kataloge (Tab_X_STAMM neben Tab_X) und lassen den Rechenweg unberuehrt: Der
    // Lauf liest nur Tab_Zone.
    //
    // WARUM EINE SPALTE AN DER KONDITIONIERUNG. Der Eigentuemermechanismus kennt ID_Zone nur mit
    // Fremdschluessel auf Tab_Zone und der Pruefklausel "ID_Zone IS NULL OR ID_Gebaeude IS NOT NULL" -
    // eine Katalogzone kann er nicht tragen. Die neue Spalte ist das Gegenstueck: eine Zeile einer
    // Katalogzone traegt ID_Gebaeude_Stamm UND ID_Zone_Stamm (wie die Projektzone ID_Gebaeude UND
    // ID_Zone), die Pruefklausel der Spalte verlangt den Katalogbau daneben. Die beiden Teilindizes
    // des Katalogbaus nehmen deshalb "ID_Zone_Stamm IS NULL" auf (gleicher Name, neue Bedingung), zwei
    // neue halten die Eindeutigkeit je Katalogzone.
    //
    // KEIN ReadOnly an den Zwillingen. Gesperrt ist der Kopf (Tab_Gebaeude_STAMM.ReadOnly); die Zonen
    // haengen an ihm wie die Schichten am Bauteilaufbau. Die Auslieferungsvorlage fuehrt sie deshalb
    // als Kindkataloge ohne ReadOnly - sonst nahme die ReadOnly-Regel die Zonen eines gesperrten Satzes
    // mit.
    //
    // ERGEBNISNEUTRAL. Reines DDL, keine Zeile entsteht; keine gesaete Gebaeude- oder Zonenzeile
    // aendert sich, der Rechenweg liest die Zwillinge nicht.
    //
    // NUMMER. 204 = KaeltemaschinenTypkennfelderSchema.SCHRITT + 1. Eingetragen in
    // SchemaStand.Zielversion, im Register der Paketanhebung (Art Katalog), in der SchemaMigration der
    // Schale, in Werkzeuge/Testdatenbankschema und in EPOS.Kern.Tests/TestDatenbank.
    // ====================================================================================

    /// <summary>
    /// <b>ZK</b> — Zonen im Gebäudekatalog: die drei Katalogzwillinge der Zonentabellen und die
    /// Eigentümerspalte <see cref="SPALTE_ID_ZONE_STAMM"/> an der Konditionierung. Anlass und Bauform stehen
    /// im Kopf der Datei.
    /// </summary>
    public static class ZonenKatalogSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> (Schritt 204) — die EINE Stelle, an der sie steht: der Schritt
        /// hinter <see cref="KaeltemaschinenTypkennfelderSchema"/> (Schritt 203).
        /// </summary>
        public const int SCHRITT = KaeltemaschinenTypkennfelderSchema.SCHRITT + 1;

        /// <summary>Die Katalogzonen (Zwilling von <c>Tab_Zone</c>).</summary>
        public const string TAB_ZONE = SchemaKatalog.TAB_ZONE_STAMM;

        /// <summary>Die Bauteile der Katalogzonen (Zwilling von <c>Tab_Bauteil</c>).</summary>
        public const string TAB_BAUTEIL = SchemaKatalog.TAB_BAUTEIL_STAMM;

        /// <summary>Die Luftströme zwischen Katalogzonen (Zwilling von <c>Tab_Zonenluftstrom</c>).</summary>
        public const string TAB_LUFTSTROM = SchemaKatalog.TAB_ZONENLUFTSTROM_STAMM;

        /// <summary>Die Eigentümerspalte einer Katalogzone an Kalender und Vorgabe der Konditionierung.</summary>
        public const string SPALTE_ID_ZONE_STAMM = "ID_Zone_Stamm";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Zone_STAMM</c> — dieselben Fachspalten wie <c>Tab_Zone</c>.</summary>
        public const string SQL_CREATE_ZONE =
            "CREATE TABLE IF NOT EXISTS \"Tab_Zone_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Gebaeude\" INTEGER NOT NULL,\n" +
            "    \"Rang\" INTEGER NOT NULL,\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") <= 80),\n" +
            "    \"Nutzflaeche\" REAL,\n" +
            "    \"Raumhoehe\" REAL,\n" +
            "    \"Volumen\" REAL,\n" +
            "    \"IstBeheizt\" INTEGER NOT NULL DEFAULT 1 CHECK (\"IstBeheizt\" IN (0,1)),\n" +
            "    \"Raumsolltemperatur_Tag\" REAL,\n" +
            "    \"Raumsolltemperatur_Nachtabsenkung\" REAL,\n" +
            "    \"Raumsolltemperatur_Wochenende\" REAL,\n" +
            "    \"Raumsolltemperatur_Ferien\" REAL,\n" +
            "    \"Maximaleraumtemperatur\" REAL,\n" +
            "    \"Heizung_Strahlungsanteil\" REAL,\n" +
            "    \"Heizleistung_Max\" REAL,\n" +
            "    \"Luftwechsel_Infiltration\" REAL,\n" +
            "    \"Luftwechsel_Nutzer\" REAL,\n" +
            "    \"Interne_Waermegewinne\" REAL,\n" +
            "    \"Bewohner\" REAL,\n" +
            "    \"Kuehl_Sollwert\" REAL,\n" +
            "    \"Kuehlleistung_Max\" REAL,\n" +
            "    \"Kuehlung_Aktiv\" INTEGER CHECK (\"Kuehlung_Aktiv\" IN (0,1)),\n" +
            "    \"Kuehl_Sollwert_Nacht\" REAL,\n" +
            "    \"Uebergabe_Art\" TEXT CHECK (\"Uebergabe_Art\" IN ('IDEAL','RADIATOR','FLAECHE','KONVEKTOR')),\n" +
            "    \"Uebergabe_Exponent\" REAL,\n" +
            "    \"Uebergabe_Leistung_Nenn\" REAL,\n" +
            "    \"Herkunft\" TEXT CHECK (\"Herkunft\" IN ('MANUELL','KATALOG','IFC','GBXML','VORGABE','SQPROJ')),\n" +
            "    \"Quellkennung\" TEXT CHECK (length(\"Quellkennung\") <= 64),\n" +
            "    \"Kuehl_Uebergabe_Art\" TEXT CHECK (\"Kuehl_Uebergabe_Art\" IN ('IDEAL','KUEHLDECKE','FLAECHENKUEHLUNG','GEBLAESEKONVEKTOR')),\n" +
            "    \"Kuehl_Uebergabe_Exponent\" REAL,\n" +
            "    \"Kuehl_Uebergabe_Leistung_Nenn\" REAL,\n" +
            "    \"Auslegung_Vorlauf\" REAL,\n" +
            "    \"Auslegung_Ruecklauf\" REAL,\n" +
            "    \"Auslegung_Raumtemperatur\" REAL,\n" +
            "    \"Regler_Proportionalband\" REAL CHECK (\"Regler_Proportionalband\" IS NULL OR \"Regler_Proportionalband\" >= 0),\n" +
            "    \"Nutzungsprofil\" TEXT CHECK (\"Nutzungsprofil\" IS NULL OR length(\"Nutzungsprofil\") BETWEEN 1 AND 120),\n" +
            "    FOREIGN KEY (\"ID_Gebaeude\") REFERENCES \"Tab_Gebaeude_STAMM\" (\"ID\") ON DELETE CASCADE\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Bauteil_STAMM</c> — dieselben Fachspalten wie <c>Tab_Bauteil</c>; der
        /// Aufbau zeigt in den Aufbaukatalog (fällt er, bleibt das Bauteil mit seinem U-Wert), die Nachbarzone
        /// auf eine Katalogzone.
        /// </summary>
        public const string SQL_CREATE_BAUTEIL =
            "CREATE TABLE IF NOT EXISTS \"Tab_Bauteil_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Zone\" INTEGER NOT NULL,\n" +
            "    \"Rang\" INTEGER NOT NULL,\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") <= 80),\n" +
            "    \"Bauteilart\" TEXT NOT NULL CHECK (\"Bauteilart\" IN ('AUSSENWAND','DACH','BODENPLATTE','FENSTER','TUER','INNENWAND','DECKE','VORHANGFASSADE','SONSTIGES')),\n" +
            "    \"ID_Aufbau\" INTEGER,\n" +
            "    \"Flaeche\" REAL NOT NULL,\n" +
            "    \"U_Wert\" REAL,\n" +
            "    \"g_Wert\" REAL,\n" +
            "    \"Rahmenanteil\" REAL,\n" +
            "    \"Verschattungsfaktor\" REAL,\n" +
            "    \"Neigung\" REAL,\n" +
            "    \"Azimut\" REAL,\n" +
            "    \"Randbedingung\" TEXT CHECK (\"Randbedingung\" IN ('AUSSENLUFT','ERDREICH','ZONE','UNBEHEIZT')),\n" +
            "    \"Psi_L\" REAL,\n" +
            "    \"Herkunft\" TEXT CHECK (\"Herkunft\" IN ('MANUELL','KATALOG','IFC','GBXML','VORGABE','SQPROJ')),\n" +
            "    \"Quellkennung\" TEXT CHECK (length(\"Quellkennung\") <= 64),\n" +
            "    \"ID_Nachbarzone\" INTEGER REFERENCES \"Tab_Zone_STAMM\" (\"ID\"),\n" +
            "    \"Trennflaeche_Zuordnung\" TEXT CHECK (\"Trennflaeche_Zuordnung\" IN ('IW','AW')),\n" +
            "    \"Flaechenherkunft\" TEXT CHECK (\"Flaechenherkunft\" IS NULL OR \"Flaechenherkunft\" IN ('MENGENSATZ','RAUMGRENZE','KOERPER','SCHEMATISCH')),\n" +
            "    FOREIGN KEY (\"ID_Zone\") REFERENCES \"Tab_Zone_STAMM\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_Aufbau\") REFERENCES \"Tab_Bauteilaufbau_STAMM\" (\"ID\") ON DELETE SET NULL\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Zonenluftstrom_STAMM</c> — wie <c>Tab_Zonenluftstrom</c>.</summary>
        public const string SQL_CREATE_LUFTSTROM =
            "CREATE TABLE IF NOT EXISTS \"Tab_Zonenluftstrom_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_ZoneA\" INTEGER NOT NULL,\n" +
            "    \"ID_ZoneB\" INTEGER NOT NULL,\n" +
            "    \"Volumenstrom\" REAL NOT NULL CHECK (\"Volumenstrom\" > 0),\n" +
            "    CHECK (\"ID_ZoneA\" < \"ID_ZoneB\"),\n" +
            "    FOREIGN KEY (\"ID_ZoneA\") REFERENCES \"Tab_Zone_STAMM\" (\"ID\") ON DELETE CASCADE,\n" +
            "    FOREIGN KEY (\"ID_ZoneB\") REFERENCES \"Tab_Zone_STAMM\" (\"ID\") ON DELETE CASCADE\n" +
            ") STRICT";

        /// <summary>Die Spaltendefinition von <see cref="SPALTE_ID_ZONE_STAMM"/> für <c>ALTER TABLE … ADD COLUMN</c>.</summary>
        public const string DEFINITION_ID_ZONE_STAMM =
            "INTEGER REFERENCES \"Tab_Zone_STAMM\" (\"ID\") ON DELETE CASCADE " +
            "CHECK (\"ID_Zone_Stamm\" IS NULL OR \"ID_Gebaeude_Stamm\" IS NOT NULL)";

        /// <summary>Die Tabellenanweisungen in Anlegereihenfolge (Zonen vor Bauteilen und Luftströmen).</summary>
        public static IEnumerable<KeyValuePair<string, string>> Tabellenanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_ZONE, SQL_CREATE_ZONE);
                yield return new KeyValuePair<string, string>(TAB_BAUTEIL, SQL_CREATE_BAUTEIL);
                yield return new KeyValuePair<string, string>(TAB_LUFTSTROM, SQL_CREATE_LUFTSTROM);
            }
        }

        /// <summary>Die beiden Tabellen der Konditionierung, die die Eigentümerspalte bekommen.</summary>
        public static IReadOnlyList<string> Konditionierungstabellen { get; } =
            new[] { KonditionierungSchema.TAB_KALENDER, KonditionierungSchema.TAB_VORGABE };

        /// <summary>Die einfachen Indizes der Zwillinge und der neuen Eigentümerspalte (Name → Anweisung).</summary>
        public static IEnumerable<KeyValuePair<string, string>> Indexanweisungen
        {
            get
            {
                yield return Index("idx_ZoneStamm_Gebaeude",
                    "CREATE INDEX IF NOT EXISTS \"idx_ZoneStamm_Gebaeude\" ON \"Tab_Zone_STAMM\" (\"ID_Gebaeude\", \"Rang\")");
                yield return Index("idx_BauteilStamm_Zone",
                    "CREATE INDEX IF NOT EXISTS \"idx_BauteilStamm_Zone\" ON \"Tab_Bauteil_STAMM\" (\"ID_Zone\", \"Rang\")");
                yield return Index("idx_BauteilStamm_Nachbarzone",
                    "CREATE INDEX IF NOT EXISTS \"idx_BauteilStamm_Nachbarzone\" ON \"Tab_Bauteil_STAMM\" (\"ID_Nachbarzone\")");
                yield return Index("idx_ZonenluftstromStamm",
                    "CREATE UNIQUE INDEX IF NOT EXISTS \"idx_ZonenluftstromStamm\" ON \"Tab_Zonenluftstrom_STAMM\" (\"ID_ZoneA\", \"ID_ZoneB\")");
                yield return Index("idx_ZonenluftstromStamm_ZoneB",
                    "CREATE INDEX IF NOT EXISTS \"idx_ZonenluftstromStamm_ZoneB\" ON \"Tab_Zonenluftstrom_STAMM\" (\"ID_ZoneB\")");
                yield return Index("idx_KondKalender_ZoneStamm",
                    "CREATE INDEX IF NOT EXISTS \"idx_KondKalender_ZoneStamm\" ON \"Tab_Konditionierungskalender\" (\"ID_Zone_Stamm\")");
                yield return Index("idx_KondVorgabe_ZoneStamm",
                    "CREATE INDEX IF NOT EXISTS \"idx_KondVorgabe_ZoneStamm\" ON \"Tab_Konditionierungsvorgabe\" (\"ID_Zone_Stamm\")");
                yield return Index("idx_KondKalender_EindeutigZoneStamm",
                    "CREATE UNIQUE INDEX IF NOT EXISTS \"idx_KondKalender_EindeutigZoneStamm\" ON \"Tab_Konditionierungskalender\" " +
                    "(\"ID_Zone_Stamm\", \"Groesse\") WHERE \"ID_Zone_Stamm\" IS NOT NULL");
                yield return Index("idx_KondVorgabe_EindeutigZoneStamm",
                    "CREATE UNIQUE INDEX IF NOT EXISTS \"idx_KondVorgabe_EindeutigZoneStamm\" ON \"Tab_Konditionierungsvorgabe\" " +
                    "(\"ID_Zone_Stamm\", \"Groesse\", \"Zeile\") WHERE \"ID_Zone_Stamm\" IS NOT NULL");
            }
        }

        /// <summary>
        /// Die beiden Teilindizes des Katalogbaus in ihrer neuen Form: dieselben Namen wie in
        /// <see cref="KonditionierungVorlagenSchema.Teilindizes"/>, die Bedingung schließt die Zeilen einer
        /// Katalogzone aus (sie tragen den Katalogbau daneben).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Katalogbauindizes
        {
            get
            {
                yield return Index("idx_KondKalender_EindeutigGebaeudeStamm",
                    "CREATE UNIQUE INDEX \"idx_KondKalender_EindeutigGebaeudeStamm\" ON \"Tab_Konditionierungskalender\" " +
                    "(\"ID_Gebaeude_Stamm\", \"Groesse\") WHERE \"ID_Gebaeude_Stamm\" IS NOT NULL AND \"ID_Zone_Stamm\" IS NULL");
                yield return Index("idx_KondVorgabe_EindeutigGebaeudeStamm",
                    "CREATE UNIQUE INDEX \"idx_KondVorgabe_EindeutigGebaeudeStamm\" ON \"Tab_Konditionierungsvorgabe\" " +
                    "(\"ID_Gebaeude_Stamm\", \"Groesse\", \"Zeile\") WHERE \"ID_Gebaeude_Stamm\" IS NOT NULL AND \"ID_Zone_Stamm\" IS NULL");
            }
        }

        private static KeyValuePair<string, string> Index(string name, string sql) => new KeyValuePair<string, string>(name, sql);

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return GebaeudeStammCtrl.TABLE;
            yield return SchemaKatalog.TAB_ZONE;
            yield return SchemaKatalog.TAB_BAUTEIL;
            yield return SchemaKatalog.TAB_ZONENLUFTSTROM;
            yield return SchemaKatalog.TAB_BAUTEILAUFBAU_STAMM;
            yield return KonditionierungSchema.TAB_KALENDER;
            yield return KonditionierungSchema.TAB_VORGABE;
        }

        // =================================================================
        //  Stand und Ausführung
        // =================================================================

        /// <summary>
        /// Kann der Datenweg die Katalogzonen nutzen? Die drei Zwillinge und die Eigentümerspalte an beiden
        /// Konditionierungstabellen — die Probe der Kopierwege und des Katalogdialogs.
        /// </summary>
        public static bool Lesbar()
        {
            foreach (KeyValuePair<string, string> a in Tabellenanweisungen)
                if (!DataRepository.TabelleVorhanden(a.Key)) return false;
            foreach (string t in Konditionierungstabellen)
                if (!DataRepository.SpalteVorhanden(t, SPALTE_ID_ZONE_STAMM)) return false;
            return true;
        }

        /// <summary>
        /// Steht der Schritt? <see cref="Lesbar"/>, alle Indizes, und die Teilindizes des Katalogbaus
        /// schließen die Katalogzonen aus.
        /// </summary>
        public static bool Vollstaendig()
        {
            if (!Lesbar()) return false;
            foreach (KeyValuePair<string, string> a in Indexanweisungen.Concat(Katalogbauindizes))
            {
                object sql = DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = ?", new DbParam("@i", a.Key));
                if (sql == null || sql == DBNull.Value) return false;
                if (Katalogbauindizes.Any(k => k.Key == a.Key) &&
                    Convert.ToString(sql, CultureInfo.InvariantCulture).IndexOf(SPALTE_ID_ZONE_STAMM, StringComparison.Ordinal) < 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus — für die Migration der Schale, <c>Werkzeuge/Testdatenbankschema</c>
        /// und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar</b> (vorhandene Tabelle oder Spalte = nichts zu tun), <b>kein
        /// DML</b>. Fehlt eine Voraussetzung, bricht er benannt ab.
        /// </summary>
        /// <param name="bericht">Nimmt Zeilen auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Tabellen und Spalten (höchstens fünf).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + SCHRITT.ToString(CultureInfo.InvariantCulture) +
                                                        ": Die Tabelle " + t + " fehlt; ein frueherer Schritt ist nicht gelaufen.");

            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            List<string> tabellen = Tabellenanweisungen.Where(a => !DataRepository.TabelleVorhanden(a.Key)).Select(a => a.Key).ToList();
            List<string> spalten = Konditionierungstabellen.Where(t => !DataRepository.SpalteVorhanden(t, SPALTE_ID_ZONE_STAMM)).ToList();
            bool indizesNeu = !Vollstaendig();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (KeyValuePair<string, string> a in Tabellenanweisungen) v.Ausfuehren(a.Value);
                    foreach (string t in spalten)
                        v.Ausfuehren("ALTER TABLE \"" + t + "\" ADD COLUMN \"" + SPALTE_ID_ZONE_STAMM + "\" " + DEFINITION_ID_ZONE_STAMM);
                    foreach (KeyValuePair<string, string> a in Indexanweisungen) v.Ausfuehren(a.Value);
                    if (indizesNeu)
                        foreach (KeyValuePair<string, string> a in Katalogbauindizes)
                        {
                            v.Ausfuehren("DROP INDEX IF EXISTS \"" + a.Key + "\"");
                            v.Ausfuehren(a.Value);
                        }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }

            bericht?.Add(tabellen.Count.ToString(CultureInfo.InvariantCulture) + " von 3 Tabelle(n) angelegt (" +
                         TAB_ZONE + ", " + TAB_BAUTEIL + ", " + TAB_LUFTSTROM + "), " +
                         spalten.Count.ToString(CultureInfo.InvariantCulture) + " von 2 Spalte(n) " + SPALTE_ID_ZONE_STAMM +
                         " an der Konditionierung, Teilindizes des Katalogbaus " + (indizesNeu ? "neu gebaut" : "standen bereits"));
            return tabellen.Count + spalten.Count;
        }
    }
}
