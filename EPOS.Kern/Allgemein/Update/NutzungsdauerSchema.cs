using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // Die NUTZUNGSDAUERTABELLE - Migrationsschritt 75, Stufe S1 des Konzepts
    // "Nutzungsdauer je Technik und Positionsart aus einer AfA-Tabelle"
    // (Dokumentation/ueberholt/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md, Anwenderentscheid
    // vom 14.09.2026: ND-Q1 bis ND-Q8 nach Empfehlung).
    //
    // WOZU. Die Nutzungsdauer ist heute ein freies Feld je Kostenposition; die
    // Auslieferungsvorlagen lassen es leer, und ein leeres Feld rechnet im
    // KapitalwertRechner still "wie Betrachtungszeitraum" - ohne Ersatzbeschaffung und
    // ohne Restwert. Mit diesem Schritt bekommt das Haus EINE editierbare Tabelle je
    // Technik und Positionsart, gesaet mit Richtwerten und mit Quellenangabe je Zeile.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // WechselrichterSchema (65), AnlageStrangSchema (66) und SpeicherAuslegungStrict
    // (74): Die Anweisungen brauchen DREI Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs (Access-Zweig, deshalb
    // nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema und den Nachweis in
    // EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden sie abschreiben -
    // drei Wahrheiten ueber dieselbe Tabelle.
    //
    // STRICT VON ANFANG AN. Jede Fachtabelle des SQLite-Zielschemas ist STRICT; die
    // iOS-CI zaehlt die STRICT-Tabellen der Seed-Datenbank als Startgate (ios.yml).
    // Schritt 74 hat gezeigt, was ein Nachziehen kostet - ein vollstaendiger
    // Tabellenneubau -, also traegt der CREATE-Text das Wort von der ersten Zeile an.
    //
    // ZWEI EINDEUTIGKEITEN, UND DAS IST KEIN VERSEHEN. Die Tabellenbedingung
    // UNIQUE (KomponentenID, Positionsart) steht so im Konzept (2.2). Sie greift aber
    // NICHT fuer die zwei technikuebergreifenden Zeilen: SQLite haelt NULL nicht gegen
    // NULL, zwei Zeilen mit KomponentenID NULL und gleicher Positionsart waeren also
    // erlaubt. Der zusaetzliche eindeutige Index ueber COALESCE(KomponentenID, 0)
    // schliesst genau diese Luecke.
    //
    // KEIN DDL-DEFAULT AUF FACHWERTEN (Hausregel, PV-Ertragsmodell N2.2): NULL ist der
    // Vorgabewert, und der Vorgabewert ist der, der nichts aendert. Die zwei Ausnahmen
    // sind die Wahrheitswerte IstStandard und ReadOnly - fuer sie gilt die Regel aus
    // BETRIEB_SQLITE.md 6.3 (INTEGER NOT NULL DEFAULT 0 CHECK (... IN (0,1))).
    //
    // WAS NULL IN Nutzungsdauer_a HEISST. "Wie die Standardzeile der Technik der
    // Position" (Konzept 2.6, letzter Absatz): Montage- und Planungspositionen tragen
    // keinen eigenen Ersatz, sie werden mit der Hauptposition ersetzt. Die Aufloesung
    // steht im Controller (NutzungsdauerCtrl.Vorgabe), nicht in der Datenbank.
    //
    // ERGEBNISNEUTRAL. Der Schritt legt eine Tabelle an, saet sie, haengt zwei nullbare
    // Verweisspalten an und setzt den Verweis der AUSLIEFERUNGSPOSITIONEN
    // (Tab_KostenVorlagePosition). Tab_ProjektWerte wird NICHT angefasst: Kein
    // gespeicherter Wert eines Bestandsprojekts aendert sich, kein Rechenweg liest die
    // neue Tabelle, und der Referenzlauf bleibt byte-gleich - das ist die Abnahme
    // (Konzept 2.7).
    // ====================================================================================

    /// <summary>
    /// Eine Zeile der Auslieferungssaat (Konzept 2.6). <c>KomponentenId</c> <c>null</c>
    /// heisst technikuebergreifend, <c>Nutzungsdauer</c> <c>null</c> heisst "wie die
    /// Standardzeile der Technik der Position".
    /// </summary>
    public sealed class NutzungsdauerSaat
    {
        public NutzungsdauerSaat(int id, int? komponentenId, string positionsart,
                                 bool istStandard, double? nutzungsdauer, double? afa,
                                 string quelle, int sortierung)
        {
            Id = id;
            KomponentenId = komponentenId;
            Positionsart = positionsart;
            IstStandard = istStandard;
            Nutzungsdauer = nutzungsdauer;
            AfaSteuerlich = afa;
            Quelle = quelle;
            Sortierung = sortierung;
        }

        /// <summary>Feste Saat-Id — sie bleibt ueber alle Auslieferungen gleich.</summary>
        public int Id { get; }

        /// <summary>Technik (<c>Tab_KostenKomponente.ID</c>); <c>null</c> = technikuebergreifend.</summary>
        public int? KomponentenId { get; }

        /// <summary>Positionsart, deutsch und eingefroren — sie ist Teil des Schluessels.</summary>
        public string Positionsart { get; }

        /// <summary>Standardzeile ihrer Technik — der Rueckfall ohne Positionsart.</summary>
        public bool IstStandard { get; }

        /// <summary>Rechnerische Nutzungsdauer [a]; <c>null</c> = wie die Standardzeile.</summary>
        public double? Nutzungsdauer { get; }

        /// <summary>Steuerliche Nutzungsdauer [a], nur Anzeige (ND-Q1); <c>null</c> = die
        /// AfA-Tabelle kennt die Position nicht.</summary>
        public double? AfaSteuerlich { get; }

        /// <summary>Woher der Wert stammt — er steht als Text in der Zeile.</summary>
        public string Quelle { get; }

        /// <summary>Reihenfolge INNERHALB der Technik (Zehnerschritte).</summary>
        public int Sortierung { get; }
    }

    /// <summary>
    /// Eine Zeile der Saat-Zuordnung: welche Auslieferungsposition welche Positionsart
    /// traegt (Konzept 2.3). Zugeordnet wird ueber den POSITIONSNAMEN je Technik; ein
    /// Name, der hier nicht steht, bleibt <c>NULL</c> und rechnet damit ueber den
    /// Technik-Standard.
    /// </summary>
    public sealed class NutzungsdauerZuordnung
    {
        public NutzungsdauerZuordnung(int vorlagenKomponentenId, string bezeichnung,
                                      int? zielKomponentenId, string positionsart)
        {
            VorlagenKomponentenId = vorlagenKomponentenId;
            Bezeichnung = bezeichnung;
            ZielKomponentenId = zielKomponentenId;
            Positionsart = positionsart;
        }

        /// <summary>Technik der VORLAGE, in der die Position steht.</summary>
        public int VorlagenKomponentenId { get; }

        /// <summary>Name der Position, buchstabengetreu wie in der Saat.</summary>
        public string Bezeichnung { get; }

        /// <summary>Technik der ZIELZEILE; <c>null</c> = eine der zwei technikuebergreifenden.</summary>
        public int? ZielKomponentenId { get; }

        /// <summary>Positionsart der Zielzeile.</summary>
        public string Positionsart { get; }
    }

    /// <summary>
    /// <b>Die DDL, die Saat und die Saat-Zuordnung der Nutzungsdauertabelle</b> —
    /// Migrationsschritt 75. Anlass, Rezept und Ergebnisneutralitaet stehen im
    /// Klassenkopf oben.
    /// </summary>
    public static class NutzungsdauerSchema
    {
        // =================================================================
        //  Namen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>Die Tabelle, die der Schritt anlegt.</summary>
        public const string TABELLE = "Tab_Nutzungsdauer";

        /// <summary>Technik (FK <c>Tab_KostenKomponente.ID</c>); NULL = technikuebergreifend.</summary>
        public const string SPALTE_KOMPONENTENID = "KomponentenID";

        /// <summary>Positionsart — Waermeerzeuger, Abgasanlage, MSR, Montage, …</summary>
        public const string SPALTE_POSITIONSART = "Positionsart";

        /// <summary>Standardzeile der Technik (genau eine je Technik).</summary>
        public const string SPALTE_IST_STANDARD = "IstStandard";

        /// <summary>Rechnerische Nutzungsdauer [a]; NULL = wie die Standardzeile.</summary>
        public const string SPALTE_NUTZUNGSDAUER = "Nutzungsdauer_a";

        /// <summary>Steuerliche Nutzungsdauer [a] — nur Anzeige (ND-Q1).</summary>
        public const string SPALTE_AFA = "AfA_steuerlich_a";

        /// <summary>Instandsetzungssatz [%] nach VDI 2067 Blatt 1, Tabelle A2 — Stufe S3
        /// (ND-Q6: die SPALTE entsteht mit S1, die Anzeige kommt mit S3).</summary>
        public const string SPALTE_INSTANDSETZUNG = "Instandsetzung_Prozent";

        /// <summary>Wartungssatz [%] nach VDI 2067 Blatt 1, Tabelle A2 — Stufe S3.</summary>
        public const string SPALTE_WARTUNG = "Wartung_Prozent";

        /// <summary>Quelle des Werts, als Text in der Zeile.</summary>
        public const string SPALTE_QUELLE = "Quelle";

        /// <summary>Auslieferungszeile: Wert editierbar, Zeile nicht loeschbar (ND-Q5).</summary>
        public const string SPALTE_READONLY = "ReadOnly";

        /// <summary>Reihenfolge innerhalb der Technik.</summary>
        public const string SPALTE_SORTIERUNG = "Sortierung";

        /// <summary>
        /// Der VERWEIS einer Position auf ihre Zeile — dieselbe Spalte in
        /// <c>Tab_KostenVorlagePosition</c> und <c>Tab_ProjektWerte</c> (Konzept 2.3).
        /// NULL heisst "keine Positionsart gepflegt" und faellt auf den Technik-Standard.
        /// </summary>
        public const string SPALTE_VERWEIS = "NutzungsdauerID";

        /// <summary>Der eindeutige Index ueber die technikuebergreifenden Zeilen
        /// (Klassenkopf, "zwei Eindeutigkeiten").</summary>
        public const string INDEX = "idx_Nutzungsdauer_Art";

        // =================================================================
        //  Die Quellentexte der Saat
        // =================================================================

        /// <summary>Quelle der rechnerischen Nutzungsdauer (Konzept 2.6).</summary>
        public const string QUELLE_VDI = "VDI 2067 Blatt 1, Tab. A2 (Richtwert)";

        /// <summary>Quelle der steuerlichen Spalte — sie steht ZUSAETZLICH in der Zeile,
        /// weil gerechnet wird mit der VDI-Zahl (ND-Q1).</summary>
        public const string QUELLE_AFA = "AfA-Tabelle AV (Richtwert)";

        /// <summary>Beide Quellen in EINER Zeile — dort, wo die AfA-Tabelle die Position
        /// kennt.</summary>
        public const string QUELLE_VDI_UND_AFA = QUELLE_VDI + "; " + QUELLE_AFA;

        /// <summary>Die zwei technikuebergreifenden Zeilen tragen keinen eigenen Wert:
        /// Ihre Quelle IST die Standardzeile der Technik.</summary>
        public const string QUELLE_WIE_STANDARD = "wie Standardzeile der Technik";

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Nutzungsdauer</c> — elf Spalten nach
        /// Konzept 2.2, <b>STRICT</b>, mit der Eindeutigkeit und dem Fremdschluessel auf
        /// <c>Tab_KostenKomponente</c>.
        /// </summary>
        public const string SQL_CREATE =
            "CREATE TABLE IF NOT EXISTS \"Tab_Nutzungsdauer\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY,\n" +
            "    \"KomponentenID\" INTEGER,\n" +
            "    \"Positionsart\" TEXT NOT NULL,\n" +
            "    \"IstStandard\" INTEGER NOT NULL DEFAULT 0 CHECK (\"IstStandard\" IN (0,1)),\n" +
            "    \"Nutzungsdauer_a\" REAL,\n" +
            "    \"AfA_steuerlich_a\" REAL,\n" +
            "    \"Instandsetzung_Prozent\" REAL,\n" +
            "    \"Wartung_Prozent\" REAL,\n" +
            "    \"Quelle\" TEXT,\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)),\n" +
            "    \"Sortierung\" INTEGER,\n" +
            "    UNIQUE (\"KomponentenID\", \"Positionsart\"),\n" +
            "    FOREIGN KEY (\"KomponentenID\") REFERENCES \"Tab_KostenKomponente\" (\"ID\")\n" +
            ") STRICT";

        /// <summary>
        /// Der eindeutige Index, der die zwei technikuebergreifenden Zeilen schuetzt:
        /// SQLite haelt NULL nicht gegen NULL, die Tabellenbedingung greift dort also
        /// nicht (Klassenkopf).
        /// </summary>
        public const string SQL_INDEX =
            "CREATE UNIQUE INDEX IF NOT EXISTS \"idx_Nutzungsdauer_Art\" ON \"Tab_Nutzungsdauer\" " +
            "(COALESCE(\"KomponentenID\", 0), \"Positionsart\")";

        /// <summary>
        /// Alles hinter dem Spaltennamen eines <c>ADD COLUMN</c> der Verweisspalte.
        ///
        /// <para><b>Mit <c>REFERENCES</c>, ohne <c>DEFAULT</c>.</b> SQLite laesst ein
        /// nachtraegliches <c>ADD COLUMN</c> mit Fremdschluessel genau dann zu, wenn der
        /// Vorgabewert NULL ist — und NULL ist hier ohnehin die Aussage "keine
        /// Positionsart gepflegt".</para>
        /// </summary>
        public const string TYP_VERWEIS =
            "INTEGER REFERENCES \"Tab_Nutzungsdauer\" (\"ID\")";

        /// <summary>Beide DDL-Anweisungen in Anlegereihenfolge — so, wie Migration und
        /// Werkzeug sie abarbeiten.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TABELLE, SQL_CREATE);
                yield return new KeyValuePair<string, string>("Index " + INDEX, SQL_INDEX);
            }
        }

        /// <summary>
        /// Die zwei Verweisspalten in Anlegereihenfolge: der Tabellenname als
        /// Schluessel, die Typdefinition als Wert. Der Spaltenname ist in beiden
        /// Faellen <see cref="SPALTE_VERWEIS"/>.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Verweisspalten
        {
            get
            {
                yield return new KeyValuePair<string, string>(
                    SchemaKatalog.TAB_KOSTENVORLAGEPOSITION, TYP_VERWEIS);
                yield return new KeyValuePair<string, string>(
                    SchemaKatalog.TAB_PROJEKTWERTE, TYP_VERWEIS);
            }
        }

        // =================================================================
        //  Die Saat (Konzept 2.6)
        // =================================================================

        /// <summary>
        /// Die 28 Auslieferungszeilen — Richtwerte in Jahren, gerundet, je mit ihrer
        /// Quelle (Anwenderentscheid ND-Q8 vom 14.09.2026: Die Werte der Tabelle 2.6
        /// werden als Saat mit Quellenangabe „Richtwert" ausgeliefert, sind im Dialog
        /// editierbar und lassen sich mit „Auslieferungswerte wiederherstellen"
        /// zuruecksetzen).
        ///
        /// <para><b>Genau eine Standardzeile je Technik</b> — zehn Techniken, zehn
        /// Standardzeilen. Die zwei technikuebergreifenden Zeilen tragen weder eine
        /// Technik noch einen eigenen Wert.</para>
        /// </summary>
        public static readonly NutzungsdauerSaat[] Saat =
        {
            // ---- Heizkessel (2) --------------------------------------------------
            new NutzungsdauerSaat( 1,    2, "Wärmeerzeuger",                        true,  20, null, QUELLE_VDI, 10),
            new NutzungsdauerSaat( 2,    2, "Brenner",                              false, 12, null, QUELLE_VDI, 20),
            new NutzungsdauerSaat( 3,    2, "Abgasanlage / Schornstein",            false, 25, null, QUELLE_VDI, 30),
            new NutzungsdauerSaat( 4,    2, "MSR / Automation",                     false, 12, null, QUELLE_VDI, 40),
            new NutzungsdauerSaat( 5,    2, "Hydraulik / Zubehör",                  false, 20, null, QUELLE_VDI, 50),

            // ---- BHKW (7) --------------------------------------------------------
            new NutzungsdauerSaat( 6,    7, "Modul",                                true,  15,   10, QUELLE_VDI_UND_AFA, 10),
            new NutzungsdauerSaat( 7,    7, "Abgasanlage",                          false, 20, null, QUELLE_VDI, 20),
            new NutzungsdauerSaat( 8,    7, "Hydraulik",                            false, 20, null, QUELLE_VDI, 30),
            new NutzungsdauerSaat( 9,    7, "MSR",                                  false, 12, null, QUELLE_VDI, 40),

            // ---- Wärmepumpe (1) --------------------------------------------------
            new NutzungsdauerSaat(10,    1, "Gerät",                                true,  18, null, QUELLE_VDI, 10),
            new NutzungsdauerSaat(11,    1, "Erdsonden / Erdkollektor",             false, 40, null, QUELLE_VDI, 20),
            new NutzungsdauerSaat(12,    1, "MSR",                                  false, 12, null, QUELLE_VDI, 30),
            new NutzungsdauerSaat(13,    1, "Hydraulik",                            false, 20, null, QUELLE_VDI, 40),

            // ---- Solarthermie (4) ------------------------------------------------
            new NutzungsdauerSaat(14,    4, "Kollektoren",                          true,  20,   10, QUELLE_VDI_UND_AFA, 10),
            new NutzungsdauerSaat(15,    4, "Speicher",                             false, 20, null, QUELLE_VDI, 20),
            new NutzungsdauerSaat(16,    4, "Hydraulik",                            false, 20, null, QUELLE_VDI, 30),

            // ---- Photovoltaik (3) ------------------------------------------------
            new NutzungsdauerSaat(17,    3, "Module",                               true,  25,   20, QUELLE_VDI_UND_AFA, 10),
            new NutzungsdauerSaat(18,    3, "Wechselrichter",                       false, 12, null, QUELLE_VDI, 20),
            new NutzungsdauerSaat(19,    3, "Unterkonstruktion",                    false, 25, null, QUELLE_VDI, 30),

            // ---- Stromspeicher (5) -----------------------------------------------
            new NutzungsdauerSaat(20,    5, "Batterie",                             true,  10,   10, QUELLE_VDI_UND_AFA, 10),
            new NutzungsdauerSaat(21,    5, "Wechselrichter / Leistungselektronik", false, 12, null, QUELLE_VDI, 20),

            // ---- Pufferspeicher (6) ----------------------------------------------
            new NutzungsdauerSaat(22,    6, "Speicher",                             true,  20, null, QUELLE_VDI, 10),

            // ---- Wärmezentrale (8) -----------------------------------------------
            new NutzungsdauerSaat(23,    8, "Rohrleitungen",                        true,  40, null, QUELLE_VDI, 10),
            new NutzungsdauerSaat(24,    8, "Pumpen / Armaturen",                   false, 15, null, QUELLE_VDI, 20),

            // ---- Stromeinspeisung (10) -------------------------------------------
            new NutzungsdauerSaat(25,   10, "Netzanschluss",                        true,  30, null, QUELLE_VDI, 10),

            // ---- Bauliche Anlagen (9) --------------------------------------------
            new NutzungsdauerSaat(26,    9, "Bauliche Anlagen",                     true,  40, null, QUELLE_VDI, 10),

            // ---- technikuebergreifend (KomponentenID NULL) ------------------------
            // Nutzungsdauer_a NULL heisst "wie die Standardzeile der Technik der
            // Position": Montage- und Planungskosten tragen keinen eigenen Ersatz, sie
            // werden mit der Hauptposition ersetzt (Konzept 2.6, letzter Absatz).
            new NutzungsdauerSaat(27, null, "Montage",                              false, null, null, QUELLE_WIE_STANDARD, 10),
            new NutzungsdauerSaat(28, null, "Planung / Baunebenkosten",             false, null, null, QUELLE_WIE_STANDARD, 20),
        };

        /// <summary>
        /// Die Saat-Zuordnung der Auslieferungspositionen (Konzept 2.3) — 31 der 53
        /// Investitionspositionen. Die uebrigen 22 (und alle 67 Betriebspositionen)
        /// bleiben <c>NULL</c>: Wo der Name keine Positionsart DIESER Technik nennt —
        /// „Batteriespeicher" in der PV-Vorlage, „Bauliche Anlagen (Gerüst etc.)",
        /// „Sonstiges" —, wird nichts erfunden; NULL rechnet ueber den Technik-Standard.
        /// </summary>
        public static readonly NutzungsdauerZuordnung[] Zuordnungen =
        {
            // ---- Wärmepumpe (1) --------------------------------------------------
            new NutzungsdauerZuordnung(1, "Wärmepumpe (Aggregat)",                   1, "Gerät"),
            new NutzungsdauerZuordnung(1, "Erschließung (Sonden/Kollektor/Luft)",    1, "Erdsonden / Erdkollektor"),
            new NutzungsdauerZuordnung(1, "Zubehör",                                 1, "Hydraulik"),
            new NutzungsdauerZuordnung(1, "MSR-Technik / Automation",                1, "MSR"),
            new NutzungsdauerZuordnung(1, "Montage, Installation & Kältetechnik", null, "Montage"),
            new NutzungsdauerZuordnung(1, "Planung / Baunebenkosten",             null, "Planung / Baunebenkosten"),

            // ---- Heizkessel (2) --------------------------------------------------
            new NutzungsdauerZuordnung(2, "Wärmeerzeuger (Kessel)",                  2, "Wärmeerzeuger"),
            new NutzungsdauerZuordnung(2, "Zubehör",                                 2, "Hydraulik / Zubehör"),
            new NutzungsdauerZuordnung(2, "MSR-Technik / Automation",                2, "MSR / Automation"),
            new NutzungsdauerZuordnung(2, "Abgasanlage / Schornstein",               2, "Abgasanlage / Schornstein"),
            new NutzungsdauerZuordnung(2, "Montage und Installation",             null, "Montage"),
            new NutzungsdauerZuordnung(2, "Planung / Baunebenkosten",             null, "Planung / Baunebenkosten"),

            // ---- Photovoltaik (3) ------------------------------------------------
            new NutzungsdauerZuordnung(3, "PV-Module",                               3, "Module"),
            new NutzungsdauerZuordnung(3, "Wechselrichter",                          3, "Wechselrichter"),
            new NutzungsdauerZuordnung(3, "Montagesystem / Unterkonstruktion",       3, "Unterkonstruktion"),
            new NutzungsdauerZuordnung(3, "Montage und Installation",             null, "Montage"),
            new NutzungsdauerZuordnung(3, "Planung / Baunebenkosten",             null, "Planung / Baunebenkosten"),

            // ---- Solarthermie (4) ------------------------------------------------
            new NutzungsdauerZuordnung(4, "Sonnenkollektoren",                       4, "Kollektoren"),
            new NutzungsdauerZuordnung(4, "Zubehör (Montagesystem/Solarstation)",    4, "Hydraulik"),
            new NutzungsdauerZuordnung(4, "Wärmespeicher (Solarspeicher)",           4, "Speicher"),
            new NutzungsdauerZuordnung(4, "Montage und Verrohrung",               null, "Montage"),
            new NutzungsdauerZuordnung(4, "Planung / Baunebenkosten",             null, "Planung / Baunebenkosten"),

            // ---- Stromspeicher (5) und Pufferspeicher (6) ------------------------
            // Beide Vorlagen fuehren eine Position "Speicher"; sie trifft je Technik
            // eine ANDERE Zeile - die Batterie mit 10 a, den Pufferspeicher mit 20 a.
            new NutzungsdauerZuordnung(5, "Speicher",                                5, "Batterie"),
            new NutzungsdauerZuordnung(6, "Speicher",                                6, "Speicher"),

            // ---- BHKW (7) --------------------------------------------------------
            new NutzungsdauerZuordnung(7, "BHKW-Modul (Kompaktaggregat)",            7, "Modul"),
            new NutzungsdauerZuordnung(7, "MSR-Technik / Schaltanlage",              7, "MSR"),
            new NutzungsdauerZuordnung(7, "Abgasanlage / Schalldämpfer",             7, "Abgasanlage"),
            new NutzungsdauerZuordnung(7, "Montage und Einbringung",              null, "Montage"),
            new NutzungsdauerZuordnung(7, "Planung / Baunebenkosten",             null, "Planung / Baunebenkosten"),

            // ---- Bauliche Anlagen (9) und Stromeinspeisung (10) ------------------
            new NutzungsdauerZuordnung(9,  "Bauliche Maßnahmen",                     9, "Bauliche Anlagen"),
            new NutzungsdauerZuordnung(10, "Stromeinspeisung",                      10, "Netzanschluss"),
        };

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>Wie viele Zeilen traegt die Tabelle?</summary>
        public static string Zaehlung()
        {
            return "SELECT COUNT(*) FROM \"" + TABELLE + "\"";
        }

        /// <summary>Wie viele Auslieferungspositionen tragen bereits einen Verweis?</summary>
        public static string ZaehlungZuordnung()
        {
            return "SELECT COUNT(*) FROM \"" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION +
                   "\" WHERE \"" + SPALTE_VERWEIS + "\" IS NOT NULL";
        }

        /// <summary>
        /// Gibt es die Tabelle OHNE <c>STRICT</c>? Dieselbe Schreibweise wie in
        /// <see cref="SpeicherAuslegungStrict.Zaehlung"/> — das Wort am Ende des
        /// <c>CREATE</c>-Textes, nicht ein <c>LIKE '%STRICT%'</c>.
        /// </summary>
        public static string ZaehlungOhneStrict()
        {
            return "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '" +
                   TABELLE + "' AND upper(trim(substr(sql, length(sql) - 6))) <> 'STRICT'";
        }

        // =================================================================
        //  Saat und Zuordnung schreiben
        // =================================================================

        /// <summary>
        /// Schreibt die fehlenden Auslieferungszeilen. <b>Wiederholbar:</b> Eine Zeile,
        /// deren Id ODER deren Schluessel (Technik, Positionsart) schon dasteht, wird
        /// uebergangen — ein zweiter Lauf aendert nichts.
        /// </summary>
        /// <returns>Zahl der angelegten Zeilen.</returns>
        public static int SaatSchreiben()
        {
            int angelegt = 0;
            foreach (NutzungsdauerSaat s in Saat)
            {
                if (IdBelegt(s.Id) || ZeileZu(s.KomponentenId, s.Positionsart) > 0) continue;

                int n = DataRepository.ExecuteNonQuery(
                    "INSERT INTO \"" + TABELLE + "\" (\"ID\", \"" + SPALTE_KOMPONENTENID +
                    "\", \"" + SPALTE_POSITIONSART + "\", \"" + SPALTE_IST_STANDARD +
                    "\", \"" + SPALTE_NUTZUNGSDAUER + "\", \"" + SPALTE_AFA +
                    "\", \"" + SPALTE_QUELLE + "\", \"" + SPALTE_READONLY +
                    "\", \"" + SPALTE_SORTIERUNG + "\") VALUES (?, ?, ?, ?, ?, ?, ?, 1, ?)",
                    new DbParam("@id", s.Id),
                    Ganz("@kid", s.KomponentenId),
                    new DbParam("@art", s.Positionsart),
                    new DbParam("@std", s.IstStandard ? 1 : 0),
                    Wert("@nd", s.Nutzungsdauer),
                    Wert("@afa", s.AfaSteuerlich),
                    new DbParam("@q", s.Quelle),
                    new DbParam("@so", s.Sortierung));
                if (n == 1) angelegt++;
            }
            return angelegt;
        }

        /// <summary>
        /// Setzt den Verweis der Auslieferungspositionen (Konzept 2.3).
        /// <b>Wiederholbar</b> und <b>nie ueberschreibend</b>: Geschrieben wird nur, wo
        /// der Verweis <c>NULL</c> ist — eine vom Anwender gepflegte Zuordnung bleibt.
        /// Angefasst werden ausschliesslich INVESTITIONSvorlagen (Kategorie 1); die
        /// Betriebspositionen kennen keinen Ersatz.
        /// </summary>
        /// <returns>Zahl der gesetzten Verweise.</returns>
        public static int ZuordnungSchreiben()
        {
            int gesetzt = 0;
            foreach (NutzungsdauerZuordnung z in Zuordnungen)
            {
                int zielId = ZeileZu(z.ZielKomponentenId, z.Positionsart);
                if (zielId <= 0) continue;

                gesetzt += DataRepository.ExecuteNonQuery(
                    "UPDATE \"" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "\" SET \"" +
                    SPALTE_VERWEIS + "\" = ? WHERE \"" + SPALTE_VERWEIS + "\" IS NULL AND \"" +
                    SchemaKatalog.SPALTE_KVP_BEZEICHNUNG + "\" = ? AND \"" +
                    SchemaKatalog.SPALTE_KVP_VORLAGEID + "\" IN (SELECT \"ID\" FROM \"" +
                    SchemaKatalog.TAB_KOSTENVORLAGE + "\" WHERE \"" +
                    SchemaKatalog.SPALTE_KV_KOMPONENTENID + "\" = ? AND \"" +
                    SchemaKatalog.SPALTE_KV_KATEGORIEID + "\" = ?)",
                    new DbParam("@ndid", zielId),
                    new DbParam("@bez", z.Bezeichnung),
                    new DbParam("@kid", z.VorlagenKomponentenId),
                    new DbParam("@kat", DbWerte.KOSTEN_KATEGORIE_INVESTITION));
            }
            return gesetzt;
        }

        /// <summary>
        /// Die Id der Zeile zu (Technik, Positionsart); 0, wenn es sie nicht gibt.
        /// <para><b>Zwei Abfragen statt einer.</b> <c>KomponentenID = NULL</c> ist in
        /// SQL nie wahr — die technikuebergreifenden Zeilen brauchen deshalb
        /// <c>IS NULL</c>.</para>
        /// </summary>
        public static int ZeileZu(int? komponentenId, string positionsart)
        {
            if (string.IsNullOrEmpty(positionsart)) return 0;

            object o = komponentenId.HasValue
                ? DataRepository.ExecuteScalar(
                    "SELECT \"ID\" FROM \"" + TABELLE + "\" WHERE \"" + SPALTE_KOMPONENTENID +
                    "\" = ? AND \"" + SPALTE_POSITIONSART + "\" = ?",
                    new DbParam("@kid", komponentenId.Value),
                    new DbParam("@art", positionsart))
                : DataRepository.ExecuteScalar(
                    "SELECT \"ID\" FROM \"" + TABELLE + "\" WHERE \"" + SPALTE_KOMPONENTENID +
                    "\" IS NULL AND \"" + SPALTE_POSITIONSART + "\" = ?",
                    new DbParam("@art", positionsart));

            return (o == null || o == DBNull.Value)
                ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Die Saatzeile zu einer Id; <c>null</c>, wenn die Id keine Saatzeile
        /// ist (eine vom Anwender angelegte Zeile).</summary>
        public static NutzungsdauerSaat SaatZu(int id)
        {
            foreach (NutzungsdauerSaat s in Saat) if (s.Id == id) return s;
            return null;
        }

        // =================================================================
        //  intern
        // =================================================================

        private static bool IdBelegt(int id)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + TABELLE + "\" WHERE \"ID\" = ?",
                new DbParam("@id", id));
            return o != null && o != DBNull.Value &&
                   Convert.ToInt64(o, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>Nullbarer DOUBLE-Parameter mit ausdruecklichem Typ (Muster
        /// <c>KostenVorlagenCtrl</c>).</summary>
        internal static DbParam Wert(string name, double? wert)
        {
            var p = new DbParam(name, DbParamTyp.Double);
            p.Wert = wert.HasValue ? (object)wert.Value : DBNull.Value;
            return p;
        }

        /// <summary>Nullbarer LONG-Parameter.</summary>
        internal static DbParam Ganz(string name, int? wert)
        {
            var p = new DbParam(name, DbParamTyp.Integer);
            p.Wert = wert.HasValue ? (object)wert.Value : DBNull.Value;
            return p;
        }
    }
}
