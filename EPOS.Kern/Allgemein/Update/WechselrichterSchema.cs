using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die DDL des Wechselrichterkatalogs</b> — Migrationsschritt 65
    /// (Konzept <c>Konzept_Wechselrichter_EPOS-Plan.md</c> 3.1/3.2 und 3.6,
    /// Anwenderentscheid <b>W6‑E‑2</b> vom 06.09.2026, Stufe S1).
    ///
    /// <para><b>Warum eine eigene Klasse und kein Text in der Migration.</b> Zwei
    /// Stellen legen dieselben zwei Tabellen an: <c>SchemaMigration</c> beim
    /// Programmstart und <c>Werkzeuge/Testdatenbankschema</c> beim Nachziehen der
    /// Messlatte <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>. Zwei abgeschriebene
    /// <c>CREATE TABLE</c> wären zwei Schemata, die beim ersten Nachtrag
    /// auseinanderlaufen — dieselbe Begründung, mit der <c>KlimaWaisenBereinigung</c>
    /// die zwei <c>DELETE</c> des Schritts 62 trägt.</para>
    ///
    /// <para><b>Nur SQLite, kein Access-Zweig.</b> Der Anwender hat am 06.09.2026
    /// festgehalten, dass die Access-Datenbank nicht mehr relevant ist; die zwei
    /// Tabellen entstehen deshalb ausschließlich im SQLite-Zweig (Schritte ab 62) und
    /// nicht im eingefrorenen Access-Zweig (bis 61). Aus demselben Grund bleibt
    /// <c>sql/schema/001_grundschema.sql</c> unberührt: Diese Datei ist der
    /// eingefrorene Access-Zielstand 61 („NICHT VON HAND AENDERN"), eingebettete
    /// Ressource des <c>EposSqliteMigrator</c> und über <c>inventar.json</c> auf
    /// 114 Tabellen gezählt — genau wie schon bei den Schritten 62 bis 64.</para>
    ///
    /// <para><b>STRICT und <c>IF NOT EXISTS</c>.</b> Alle Tabellen des Zielschemas sind
    /// <c>STRICT</c>; erlaubt sind dort nur INT/INTEGER/REAL/TEXT/BLOB/ANY. Die
    /// Idempotenz trägt <c>IF NOT EXISTS</c> — SQLite kann das selbst, und der
    /// SQLite-Zweig der Migration deutet bewusst keine Fehlertexte
    /// (<c>SchemaMigration.SqliteDdl</c>).</para>
    ///
    /// <para><b>Kein DDL-DEFAULT auf Fachwerten</b> (Hausregel, PV-Ertragsmodell N2.2):
    /// NULL ist der Vorgabewert, und der Vorgabewert ist der, der nichts ändert. Die
    /// einzige Ausnahme ist <c>ReadOnly</c> — ein Wahrheitswert, und für den gilt die
    /// Regel aus <c>BETRIEB_SQLITE.md</c> 6.3
    /// (<c>INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))</c>).</para>
    ///
    /// <para><b>Kein Fremdschlüssel auf <c>ID_Projekt</c></b> — spaltengleich zum
    /// Zwilling <c>Tab_PV</c>, der ebenfalls keinen führt. Ein Fremdschlüssel wäre hier
    /// eine stille Verhaltensänderung am Löschweg eines Projekts
    /// (<c>ProjektCtrl.LoeschenMitVorarbeiten</c>), und S1 ändert kein Verhalten.</para>
    ///
    /// <para><b>Ergebnisneutral.</b> Beide Anweisungen sind reines DDL ohne DML: Nach
    /// der Migration ist der Katalog leer und kein Projekt führt eine Kopie. Der
    /// Referenzlauf bleibt byte-gleich.</para>
    /// </summary>
    public static class WechselrichterSchema
    {
        // =================================================================
        //  Die Spaltennamen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>AC-Nennwirkleistung [kW] (CEC <c>Paco</c>, OND <c>PNomConv</c>). Pflichtfeld.</summary>
        public const string SPALTE_P_AC_NENN = "P_AC_Nenn";

        /// <summary>Maximale AC-Scheinleistung [kVA]; NULL = wie <see cref="SPALTE_P_AC_NENN"/>.</summary>
        public const string SPALTE_S_AC_MAX = "S_AC_Max";

        /// <summary>Maximale DC-Eingangsleistung [kW] (OND <c>PNomDC</c>); NULL = keine Grenze.</summary>
        public const string SPALTE_P_DC_MAX = "P_DC_Max";

        /// <summary>Untere Grenze des MPP-Fensters [V] (CEC <c>Mppt_low</c>); NULL = keine Prüfung.</summary>
        public const string SPALTE_U_MPP_MIN = "U_Mpp_Min";

        /// <summary>Obere Grenze des MPP-Fensters [V] (CEC <c>Mppt_high</c>); NULL = keine Prüfung.</summary>
        public const string SPALTE_U_MPP_MAX = "U_Mpp_Max";

        /// <summary>Maximale DC-Eingangsspannung [V] (CEC <c>Vdcmax</c>); NULL = keine Prüfung.</summary>
        public const string SPALTE_U_DC_MAX = "U_Dc_Max";

        /// <summary>Einschaltspannung [V]; NULL = keine Prüfung.</summary>
        public const string SPALTE_U_START = "U_Start";

        /// <summary>
        /// Maximaler DC-Strom <b>je MPPT</b> [A] (CEC <c>Idcmax</c>).
        /// <para><b>Je MPPT, nicht je Gerät</b> — so führt es die CEC-Liste, und so
        /// braucht es die Auslegungsprüfung P4 (Konzept 4.2). Der Hinweis gehört
        /// hierher, sonst wird die Spalte beim Handpflegen falsch gefüllt.</para>
        /// </summary>
        public const string SPALTE_I_DC_MAX = "I_Dc_Max";

        /// <summary>Zahl der MPP-Tracker; NULL = 1.</summary>
        public const string SPALTE_ANZAHL_MPPT = "Anzahl_Mppt";

        /// <summary>Zulässige Stränge je MPPT; NULL = keine Prüfung.</summary>
        public const string SPALTE_STRAENGE_JE_MPPT = "Straenge_Je_Mppt";

        /// <summary>Wirkungsgrad bei 5 % der AC-Nennleistung (0…1); NULL = Stützstelle unbekannt.</summary>
        public const string SPALTE_ETA05 = "Eta05";

        /// <summary>Wirkungsgrad bei 10 % (0…1) — deckungsgleich mit <c>Tab_Energieanlagen.PV_WrEta10</c>.</summary>
        public const string SPALTE_ETA10 = "Eta10";

        /// <summary>Wirkungsgrad bei 20 % (0…1).</summary>
        public const string SPALTE_ETA20 = "Eta20";

        /// <summary>Wirkungsgrad bei 30 % (0…1).</summary>
        public const string SPALTE_ETA30 = "Eta30";

        /// <summary>Wirkungsgrad bei 50 % (0…1) — deckungsgleich mit <c>PV_WrEta50</c>.</summary>
        public const string SPALTE_ETA50 = "Eta50";

        /// <summary>Wirkungsgrad bei 100 % (0…1) — deckungsgleich mit <c>PV_WrEta100</c>.</summary>
        public const string SPALTE_ETA100 = "Eta100";

        /// <summary>Europäischer Wirkungsgrad (0…1), Ausweis; NULL = aus den Stützstellen zu rechnen.</summary>
        public const string SPALTE_ETA_EURO = "Eta_Euro";

        /// <summary>Maximalwirkungsgrad (0…1), nur Ausweis.</summary>
        public const string SPALTE_ETA_MAX = "Eta_Max";

        /// <summary>Einschaltschwelle / Eigenverbrauch [W] (CEC <c>Pso</c>); NULL = 0.</summary>
        public const string SPALTE_P_STANDBY = "P_Standby";

        /// <summary>Nachtverbrauch [W] (CEC <c>Pnt</c>); NULL = 0.</summary>
        public const string SPALTE_P_NACHT = "P_Nacht";

        /// <summary>Gerätepreis [€] — Anwenderfeld wie <c>Tab_PV_STAMM.Modulkosten</c>.</summary>
        public const string SPALTE_KOSTEN = "Kosten";

        /// <summary>Sandia: DC-Leistung bei AC-Nennleistung [W] (CEC <c>Pdco</c>).</summary>
        public const string SPALTE_SANDIA_PDCO = "Sandia_Pdco";

        /// <summary>Sandia: Bezugsspannung [V] (CEC <c>Vdco</c>).</summary>
        public const string SPALTE_SANDIA_VDCO = "Sandia_Vdco";

        /// <summary>Sandia: Einschaltschwelle [W] (CEC <c>Pso</c>).</summary>
        public const string SPALTE_SANDIA_PSO = "Sandia_Pso";

        /// <summary>Sandia-Koeffizient C0 [1/W].</summary>
        public const string SPALTE_SANDIA_C0 = "Sandia_C0";

        /// <summary>Sandia-Koeffizient C1 [1/V].</summary>
        public const string SPALTE_SANDIA_C1 = "Sandia_C1";

        /// <summary>Sandia-Koeffizient C2 [1/V].</summary>
        public const string SPALTE_SANDIA_C2 = "Sandia_C2";

        /// <summary>Sandia-Koeffizient C3 [1/V].</summary>
        public const string SPALTE_SANDIA_C3 = "Sandia_C3";

        /// <summary>
        /// Woher der Satz stammt — <see cref="DbWerte.WR_HERKUNFT_CEC"/>,
        /// <see cref="DbWerte.WR_HERKUNFT_OND"/> oder
        /// <see cref="DbWerte.WR_HERKUNFT_HAND"/>; NULL = unbekannt.
        /// </summary>
        public const string SPALTE_HERKUNFT = "Herkunft";

        /// <summary>„Gehört zur Auslieferung" — nur in der Stammtabelle.</summary>
        public const string SPALTE_READONLY = "ReadOnly";

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Wechselrichter_STAMM</c> — 34 Spalten
        /// (Konzept 3.1).
        /// </summary>
        public const string SQL_CREATE_STAMM =
            "CREATE TABLE IF NOT EXISTS \"Tab_Wechselrichter_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Bezeichner\" TEXT NOT NULL,\n" +
            "    \"Firma\" TEXT,\n" +
            "    \"Beschreibung\" TEXT,\n" +
            "    \"P_AC_Nenn\" REAL,\n" +
            "    \"S_AC_Max\" REAL,\n" +
            "    \"P_DC_Max\" REAL,\n" +
            "    \"U_Mpp_Min\" REAL,\n" +
            "    \"U_Mpp_Max\" REAL,\n" +
            "    \"U_Dc_Max\" REAL,\n" +
            "    \"U_Start\" REAL,\n" +
            "    \"I_Dc_Max\" REAL,\n" +
            "    \"Anzahl_Mppt\" INTEGER,\n" +
            "    \"Straenge_Je_Mppt\" INTEGER,\n" +
            "    \"Eta05\" REAL,\n" +
            "    \"Eta10\" REAL,\n" +
            "    \"Eta20\" REAL,\n" +
            "    \"Eta30\" REAL,\n" +
            "    \"Eta50\" REAL,\n" +
            "    \"Eta100\" REAL,\n" +
            "    \"Eta_Euro\" REAL,\n" +
            "    \"Eta_Max\" REAL,\n" +
            "    \"P_Standby\" REAL,\n" +
            "    \"P_Nacht\" REAL,\n" +
            "    \"Kosten\" REAL,\n" +
            "    \"Sandia_Pdco\" REAL,\n" +
            "    \"Sandia_Vdco\" REAL,\n" +
            "    \"Sandia_Pso\" REAL,\n" +
            "    \"Sandia_C0\" REAL,\n" +
            "    \"Sandia_C1\" REAL,\n" +
            "    \"Sandia_C2\" REAL,\n" +
            "    \"Sandia_C3\" REAL,\n" +
            "    \"Herkunft\" TEXT,\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1))\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Wechselrichter</c> — die Projektkopie,
        /// spaltengleich, zusätzlich <c>ID_Projekt</c>, ohne <c>ReadOnly</c>
        /// (Konzept 3.2).
        /// </summary>
        public const string SQL_CREATE_PROJEKT =
            "CREATE TABLE IF NOT EXISTS \"Tab_Wechselrichter\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL,\n" +
            "    \"Bezeichner\" TEXT NOT NULL,\n" +
            "    \"Firma\" TEXT,\n" +
            "    \"Beschreibung\" TEXT,\n" +
            "    \"P_AC_Nenn\" REAL,\n" +
            "    \"S_AC_Max\" REAL,\n" +
            "    \"P_DC_Max\" REAL,\n" +
            "    \"U_Mpp_Min\" REAL,\n" +
            "    \"U_Mpp_Max\" REAL,\n" +
            "    \"U_Dc_Max\" REAL,\n" +
            "    \"U_Start\" REAL,\n" +
            "    \"I_Dc_Max\" REAL,\n" +
            "    \"Anzahl_Mppt\" INTEGER,\n" +
            "    \"Straenge_Je_Mppt\" INTEGER,\n" +
            "    \"Eta05\" REAL,\n" +
            "    \"Eta10\" REAL,\n" +
            "    \"Eta20\" REAL,\n" +
            "    \"Eta30\" REAL,\n" +
            "    \"Eta50\" REAL,\n" +
            "    \"Eta100\" REAL,\n" +
            "    \"Eta_Euro\" REAL,\n" +
            "    \"Eta_Max\" REAL,\n" +
            "    \"P_Standby\" REAL,\n" +
            "    \"P_Nacht\" REAL,\n" +
            "    \"Kosten\" REAL,\n" +
            "    \"Sandia_Pdco\" REAL,\n" +
            "    \"Sandia_Vdco\" REAL,\n" +
            "    \"Sandia_Pso\" REAL,\n" +
            "    \"Sandia_C0\" REAL,\n" +
            "    \"Sandia_C1\" REAL,\n" +
            "    \"Sandia_C2\" REAL,\n" +
            "    \"Sandia_C3\" REAL,\n" +
            "    \"Herkunft\" TEXT\n" +
            ") STRICT";

        /// <summary>
        /// Beide Anweisungen in Anlegereihenfolge, je Tabellenname — so, wie
        /// Migration und Werkzeug sie abarbeiten.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(
                    SchemaKatalog.TAB_WECHSELRICHTER_STAMM, SQL_CREATE_STAMM);
                yield return new KeyValuePair<string, string>(
                    SchemaKatalog.TAB_WECHSELRICHTER, SQL_CREATE_PROJEKT);
            }
        }

        /// <summary>
        /// Die FACHSPALTEN beider Tabellen in Schemareihenfolge — ohne <c>ID</c>,
        /// <c>ID_Projekt</c>, <c>Bezeichner</c>, <c>Firma</c>, <c>Beschreibung</c> und
        /// <c>ReadOnly</c>.
        ///
        /// <para>Sie ist die EINE Liste, an der <c>WechselrichterCtrl.CopyFromStamm</c>,
        /// die Schreibwege des Katalogs und der Nachweis hängen: „Katalog und
        /// Projektkopie im selben Schritt, Spalte für Spalte gleich — eine Spalte nur
        /// auf einer Seite ist beim <c>CopyFromStamm</c> sofort ein Datenverlust"
        /// (Konzept 3, Hausregeln).</para>
        /// </summary>
        public static readonly string[] Fachspalten =
        {
            SPALTE_P_AC_NENN, SPALTE_S_AC_MAX, SPALTE_P_DC_MAX,
            SPALTE_U_MPP_MIN, SPALTE_U_MPP_MAX, SPALTE_U_DC_MAX, SPALTE_U_START,
            SPALTE_I_DC_MAX, SPALTE_ANZAHL_MPPT, SPALTE_STRAENGE_JE_MPPT,
            SPALTE_ETA05, SPALTE_ETA10, SPALTE_ETA20, SPALTE_ETA30, SPALTE_ETA50,
            SPALTE_ETA100, SPALTE_ETA_EURO, SPALTE_ETA_MAX,
            SPALTE_P_STANDBY, SPALTE_P_NACHT, SPALTE_KOSTEN,
            SPALTE_SANDIA_PDCO, SPALTE_SANDIA_VDCO, SPALTE_SANDIA_PSO,
            SPALTE_SANDIA_C0, SPALTE_SANDIA_C1, SPALTE_SANDIA_C2, SPALTE_SANDIA_C3,
            SPALTE_HERKUNFT
        };
    }
}
