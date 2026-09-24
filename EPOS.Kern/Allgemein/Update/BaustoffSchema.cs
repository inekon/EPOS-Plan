using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER BAUSTOFFKATALOG - Schemaschritt S-A der Gebaeudesimulation, Stufe G3
    // (Softwarearchitektur Gebaeudesimulation 2.2 und 2.4; Mehrzonenkonzept 3.5 und 4.4).
    //
    // WOZU. Ein Bauteilaufbau besteht aus Schichten, und eine Schicht braucht die Stoffwerte
    // Lambda, Rho und cp. Bis hierher gab es keinen Ort dafuer. Der Schritt legt den Katalog
    // (Tab_Baustoff_STAMM) und seine Projektkopie (Tab_Baustoff) an und saet den Katalog mit
    // Norm- und Richtwerten nach DIN 4108-4 und DIN EN ISO 10456, je Zeile mit Quelle.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei NutzungsdauerSchema
    // (75) und ProjektWirkungSchema (127): VIER Leser - der Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema, die Testvorrichtung EPOS.Kern.Tests/TestDatenbank und der
    // Kopierweg BaustoffCtrl.CopyFromStamm (Fachspalten).
    //
    // SPALTENGLEICH (Regel WechselrichterSchema): Katalog und Projektkopie tragen dieselben
    // Fachspalten in derselben Reihenfolge; nur ReadOnly (Katalog) und ID_Projekt (Kopie)
    // unterscheiden sie. Eine Spalte nur auf einer Seite waere beim CopyFromStamm sofort ein
    // Datenverlust.
    //
    // HERSTELLER (Anwenderwunsch 24.09.2026): eine Spalte ueber 2.2 hinaus. NULL heisst
    // herstellerneutral - ein Norm- oder Richtwert. Die Kataloge der wichtigsten Hersteller
    // werden in einem spaeteren Schritt gesaet; die Spalte entsteht schon jetzt, damit dieser
    // Schritt kein DDL braucht.
    //
    // KEIN FREMDSCHLUESSEL AUF ID_PROJEKT (W16) - wie Tab_Wechselrichter und Tab_PV. Ein
    // Fremdschluessel aenderte den Loeschweg eines Projekts; die Projektkopie reist ueber
    // ihre Spalte ID_Projekt mit (ProjektDuplizierenCtrl, Projekttransfer).
    //
    // DIE SAAT. 65 Zeilen mit fester Id (1 bis 65), ReadOnly = 1, Herkunft = VORGABE und der
    // Quelle je Zeile; die Bemerkungen der Recherche stehen NICHT in der Datenbank. Die Ids
    // unter SAAT_ID_GRENZE sind der Auslieferungssaat vorbehalten: SaatSchreiben hebt die
    // AUTOINCREMENT-Folge des Katalogs auf die Grenze, damit eine vom Anwender angelegte Zeile
    // nie eine Id bekommt, die eine spaetere Saat (die Herstellerkataloge) fest vergibt.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest die Tabellen; der Referenzlauf bleibt byte-gleich.
    // ====================================================================================

    /// <summary>
    /// Eine Zeile der Baustoffsaat — ein Norm- oder Richtwert mit Quelle. Die Id ist fest und
    /// bleibt über alle Auslieferungen gleich (Muster <see cref="NutzungsdauerSaat"/>).
    /// </summary>
    public sealed class BaustoffSaat
    {
        public BaustoffSaat(int id, string gruppe, string bezeichner, double lambda, double rho,
                            double cp, string quelle)
        {
            Id = id;
            Gruppe = gruppe;
            Bezeichner = bezeichner;
            Lambda = lambda;
            Rho = rho;
            Cp = cp;
            Quelle = quelle;
        }

        /// <summary>Feste Saat-Id — sie bleibt über alle Auslieferungen gleich.</summary>
        public int Id { get; }

        /// <summary>Ordnungsgruppe (Mauerwerk, Beton, Dämmstoffe, …).</summary>
        public string Gruppe { get; }

        /// <summary>Name des Stoffes, herstellerneutral; die Klasse steht im Namen.</summary>
        public string Bezeichner { get; }

        /// <summary>Wärmeleitfähigkeit [W/(m·K)] — bei Dämmstoffen der Bemessungswert nach DIN 4108-4, Tab. 2.</summary>
        public double Lambda { get; }

        /// <summary>Rohdichte [kg/m³].</summary>
        public double Rho { get; }

        /// <summary>Spezifische Wärmekapazität [J/(kg·K)].</summary>
        public double Cp { get; }

        /// <summary>Regelwerk mit Tabelle und Zeile.</summary>
        public string Quelle { get; }
    }

    /// <summary>
    /// <b>Die DDL und die Saat des Baustoffkatalogs</b> — Schemaschritt S-A (Nummer
    /// <see cref="SCHRITT"/>). Anlass, Bauform und Ergebnisneutralität stehen im Kopf der Datei.
    /// </summary>
    public static class BaustoffSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: Migration,
        /// Werkzeug und Nachweis lesen sie hier. Vergeben am 24.09.2026 für die Welle G3-B;
        /// 129 ist einem parallelen Schritt zugesagt.
        /// </summary>
        public const int SCHRITT = 130;

        // =================================================================
        //  Namen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>Der Katalog (Auslieferung).</summary>
        public const string TAB_STAMM = SchemaKatalog.TAB_BAUSTOFF_STAMM;

        /// <summary>Die Projektkopie.</summary>
        public const string TAB_PROJEKT = SchemaKatalog.TAB_BAUSTOFF;

        /// <summary>Name des Stoffes, NOT NULL, höchstens <see cref="LAENGE_BEZEICHNER"/> Zeichen (W3).</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>Ordnungsgruppe; NULL = ohne Gruppe.</summary>
        public const string SPALTE_GRUPPE = "Gruppe";

        /// <summary>
        /// Hersteller des Produkts; <b>NULL = herstellerneutral</b> (Norm- oder Richtwert).
        /// Anwenderwunsch vom 24.09.2026: Herstellerkataloge kommen in einem späteren Schritt.
        /// </summary>
        public const string SPALTE_HERSTELLER = "Hersteller";

        /// <summary>Wärmeleitfähigkeit [W/(m·K)]; NULL = nicht angegeben.</summary>
        public const string SPALTE_LAMBDA = "Lambda";

        /// <summary>Rohdichte [kg/m³]; NULL = nicht angegeben.</summary>
        public const string SPALTE_RHO = "Rho";

        /// <summary>Spezifische Wärmekapazität [J/(kg·K)]; NULL = nicht angegeben.</summary>
        public const string SPALTE_CP = "cp";

        /// <summary>Regelwerk oder Dateiname des Imports; NULL = nicht angegeben.</summary>
        public const string SPALTE_QUELLE = "Quelle";

        /// <summary>Herkunft (<see cref="DbWerte.HERKUENFTE"/>); NULL = nicht angegeben (W9).</summary>
        public const string SPALTE_HERKUNFT = "Herkunft";

        /// <summary>Kennung der Quellentität eines Imports (IFC-GUID, gbXML-id); NULL = keine (W10).</summary>
        public const string SPALTE_QUELLKENNUNG = "Quellkennung";

        /// <summary>„Gehört zur Auslieferung" — nur im Katalog.</summary>
        public const string SPALTE_READONLY = "ReadOnly";

        /// <summary>Das Projekt der Kopie — nur in der Projektkopie, ohne Fremdschlüssel (W16).</summary>
        public const string SPALTE_ID_PROJEKT = "ID_Projekt";

        /// <summary>Höchstlänge des Bezeichners.</summary>
        public const int LAENGE_BEZEICHNER = 80;

        /// <summary>Höchstlänge der Gruppe.</summary>
        public const int LAENGE_GRUPPE = 40;

        /// <summary>Höchstlänge des Herstellers.</summary>
        public const int LAENGE_HERSTELLER = 80;

        /// <summary>Höchstlänge der Quelle.</summary>
        public const int LAENGE_QUELLE = 120;

        /// <summary>Höchstlänge der Quellkennung (W10) — gilt an allen vier Tabellenfamilien.</summary>
        public const int LAENGE_QUELLKENNUNG = 64;

        /// <summary>
        /// Die Wertliste der Herkunft als SQL-Literal — die EINE Quelle aller vier
        /// <c>CHECK</c>-Klauseln (Baustoff, Aufbau, Zone, Bauteil). Konstant zusammengesetzt aus
        /// <see cref="DbWerte"/>; der Nachweis hält sie gegen <see cref="DbWerte.HERKUENFTE"/>.
        /// </summary>
        public const string WERTE_HERKUNFT =
            "'" + DbWerte.HERKUNFT_MANUELL + "','" + DbWerte.HERKUNFT_KATALOG + "','" +
            DbWerte.HERKUNFT_IFC + "','" + DbWerte.HERKUNFT_GBXML + "','" + DbWerte.HERKUNFT_VORGABE + "'";

        /// <summary>
        /// <b>Die Ids unterhalb dieser Grenze gehören der Auslieferungssaat</b> — die Normsaat
        /// belegt 1 bis 65, eine spätere Herstellersaat bekommt ihre festen Ids darüber.
        /// <see cref="SaatSchreiben"/> hebt die AUTOINCREMENT-Folge des Katalogs auf
        /// <c>SAAT_ID_GRENZE − 1</c>; eine vom Anwender angelegte Zeile beginnt damit bei
        /// dieser Grenze und kann keine feste Saat-Id besetzen.
        /// </summary>
        public const int SAAT_ID_GRENZE = 10000;

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Baustoff_STAMM</c> — elf Spalten, <b>STRICT</b>.
        /// </summary>
        public const string SQL_CREATE_STAMM =
            "CREATE TABLE IF NOT EXISTS \"Tab_Baustoff_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") <= 80),\n" +
            "    \"Gruppe\" TEXT CHECK (length(\"Gruppe\") <= 40),\n" +
            "    \"Hersteller\" TEXT CHECK (length(\"Hersteller\") <= 80),\n" +
            "    \"Lambda\" REAL,\n" +
            "    \"Rho\" REAL,\n" +
            "    \"cp\" REAL,\n" +
            "    \"Quelle\" TEXT CHECK (length(\"Quelle\") <= 120),\n" +
            "    \"Herkunft\" TEXT CHECK (\"Herkunft\" IN (" + WERTE_HERKUNFT + ")),\n" +
            "    \"Quellkennung\" TEXT CHECK (length(\"Quellkennung\") <= 64),\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1))\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Baustoff</c> — die Projektkopie, spaltengleich,
        /// zusätzlich <c>ID_Projekt</c> (ohne Fremdschlüssel), ohne <c>ReadOnly</c>.
        /// </summary>
        public const string SQL_CREATE_PROJEKT =
            "CREATE TABLE IF NOT EXISTS \"Tab_Baustoff\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL,\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") <= 80),\n" +
            "    \"Gruppe\" TEXT CHECK (length(\"Gruppe\") <= 40),\n" +
            "    \"Hersteller\" TEXT CHECK (length(\"Hersteller\") <= 80),\n" +
            "    \"Lambda\" REAL,\n" +
            "    \"Rho\" REAL,\n" +
            "    \"cp\" REAL,\n" +
            "    \"Quelle\" TEXT CHECK (length(\"Quelle\") <= 120),\n" +
            "    \"Herkunft\" TEXT CHECK (\"Herkunft\" IN (" + WERTE_HERKUNFT + ")),\n" +
            "    \"Quellkennung\" TEXT CHECK (length(\"Quellkennung\") <= 64)\n" +
            ") STRICT";

        /// <summary>
        /// Beide Anweisungen in Anlegereihenfolge, je Tabellenname — so, wie Migration,
        /// Werkzeug und Testvorrichtung sie abarbeiten.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_STAMM, SQL_CREATE_STAMM);
                yield return new KeyValuePair<string, string>(TAB_PROJEKT, SQL_CREATE_PROJEKT);
            }
        }

        /// <summary>
        /// Die FACHSPALTEN beider Tabellen in Schemareihenfolge — ohne <c>ID</c>,
        /// <c>ID_Projekt</c>, <c>Bezeichner</c> und <c>ReadOnly</c>. Die EINE Liste, an der
        /// <c>BaustoffCtrl.CopyFromStamm</c>, die Schreibwege und der Nachweis hängen.
        /// </summary>
        public static readonly IReadOnlyList<string> Fachspalten = new[]
        {
            SPALTE_GRUPPE, SPALTE_HERSTELLER, SPALTE_LAMBDA, SPALTE_RHO, SPALTE_CP,
            SPALTE_QUELLE, SPALTE_HERKUNFT, SPALTE_QUELLKENNUNG
        };

        // =================================================================
        //  Die Saat (Normrecherche 24.09.2026, DIN 4108-4:2020-11 und DIN EN ISO 10456:2010-05)
        // =================================================================

        /// <summary>
        /// Die 65 Auslieferungszeilen — Norm- und Richtwerte, je mit ihrer Quelle. Bei den
        /// Dämmstoffen nennt der Bezeichner den Nennwert λD, die Spalte <c>Lambda</c> trägt den
        /// Bemessungswert nach DIN 4108-4, Tab. 2. Kupfer, Bronze, Messing und Blei fehlen,
        /// weil ihre Rohdichte über dem Band des Imports liegt (Mehrzonenkonzept 3.5).
        /// </summary>
        public static readonly IReadOnlyList<BaustoffSaat> Saat = new[]
        {
            // ---- Putze und Mörtel --------------------------------------------------
            new BaustoffSaat( 1, "Putze und Mörtel", "Kalkzementputz", 1.0, 1800.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.1.1; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat( 2, "Putze und Mörtel", "Gipsputz 1200", 0.43, 1200.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.1.2; DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat( 3, "Putze und Mörtel", "Leichtputz 1000", 0.38, 1000.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.1.4; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat( 4, "Putze und Mörtel", "Zementputz", 1.0, 1800.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Estriche ----------------------------------------------------------
            new BaustoffSaat( 5, "Estriche", "Zementestrich", 1.4, 2000.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.3.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat( 6, "Estriche", "Calciumsulfatestrich", 1.2, 2100.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.3.3; DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat( 7, "Estriche", "Calciumsulfat-Fließestrich", 1.4, 2100.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.3.4; DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat( 8, "Estriche", "Gussasphaltestrich", 0.9, 2300.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 1.3.1; DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Beton -------------------------------------------------------------
            new BaustoffSaat( 9, "Beton", "Normalbeton 2400", 2.0, 2400.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(10, "Beton", "Stahlbeton", 2.5, 2400.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(11, "Beton", "Stahlbeton 1 % Bewehrung", 2.3, 2300.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(12, "Beton", "Leichtbeton 1200", 0.62, 1200.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 2.2; DIN EN ISO 10456:2010-05, Tab. 4"),

            // ---- Mauerwerk ---------------------------------------------------------
            new BaustoffSaat(13, "Mauerwerk", "Vollziegel 1800", 0.81, 1800.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.1.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(14, "Mauerwerk", "Klinker 2000", 0.96, 2000.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.1.1; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(15, "Mauerwerk", "Hochlochziegel 1200", 0.5, 1200.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.1.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(16, "Mauerwerk", "Hochlochziegel HLzA/B 800", 0.39, 800.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.1.3; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(17, "Mauerwerk", "Hochlochziegel HLzW 700", 0.24, 700.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.1.4; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(18, "Mauerwerk", "Kalksandstein 1400", 0.7, 1400.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(19, "Mauerwerk", "Kalksandstein 1600", 0.79, 1600.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(20, "Mauerwerk", "Kalksandstein 1800", 0.99, 1800.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(21, "Mauerwerk", "Kalksandstein 2000", 1.1, 2000.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.2; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(22, "Mauerwerk", "Porenbeton 350", 0.11, 350.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.3; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(23, "Mauerwerk", "Porenbeton 400", 0.13, 400.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.3; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(24, "Mauerwerk", "Porenbeton 500", 0.16, 500.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.3; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(25, "Mauerwerk", "Porenbeton 600", 0.19, 600.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.3; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(26, "Mauerwerk", "Leichtbeton-Hohlblock 800", 0.35, 800.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 4.4.1; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(27, "Mauerwerk", "Sandstein", 2.3, 2600.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(28, "Mauerwerk", "Kalkstein mittelhart", 1.4, 2000.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Holz und Holzwerkstoffe -------------------------------------------
            new BaustoffSaat(29, "Holz und Holzwerkstoffe", "Nadelholz 500", 0.13, 500.0, 1600.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(30, "Holz und Holzwerkstoffe", "Laubholz 700", 0.18, 700.0, 1600.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(31, "Holz und Holzwerkstoffe", "Sperrholz 500", 0.13, 500.0, 1600.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(32, "Holz und Holzwerkstoffe", "OSB-Platte", 0.13, 650.0, 1700.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(33, "Holz und Holzwerkstoffe", "Spanplatte 600", 0.14, 600.0, 1700.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(34, "Holz und Holzwerkstoffe", "Holzfaserplatte MDF 800", 0.18, 800.0, 1700.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Dämmstoffe --------------------------------------------------------
            new BaustoffSaat(35, "Dämmstoffe", "Mineralwolle λD 0,032", 0.033, 40.0, 1030.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.1, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(36, "Dämmstoffe", "Mineralwolle λD 0,035", 0.036, 40.0, 1030.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.1, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(37, "Dämmstoffe", "Mineralwolle λD 0,040", 0.041, 40.0, 1030.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.1, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(38, "Dämmstoffe", "EPS-Hartschaum λD 0,032", 0.033, 20.0, 1450.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.2, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(39, "Dämmstoffe", "EPS-Hartschaum λD 0,035", 0.036, 20.0, 1450.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.2, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(40, "Dämmstoffe", "EPS-Hartschaum λD 0,040", 0.041, 20.0, 1450.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.2, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(41, "Dämmstoffe", "XPS-Hartschaum λD 0,035", 0.036, 35.0, 1450.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.3, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(42, "Dämmstoffe", "PUR-Hartschaum λD 0,023", 0.024, 30.0, 1400.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.4, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(43, "Dämmstoffe", "Holzfaserdämmstoff λD 0,040", 0.042, 140.0, 2000.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.10, Fn. b; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(44, "Dämmstoffe", "Zellulose λD 0,040", 0.041, 50.0, 1600.0,
                             "DIN 4108-4:2020-11, Tab. 5, Z. 2.1; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(45, "Dämmstoffe", "Schaumglas λD 0,040", 0.041, 120.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.6, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(46, "Dämmstoffe", "Perlite-Schüttung λD 0,050", 0.052, 90.0, 900.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.13, Fn. a; DIN EN ISO 10456:2010-05, Tab. 4"),
            new BaustoffSaat(47, "Dämmstoffe", "Holzwolle-Leichtbauplatte λD 0,090", 0.095, 400.0, 1470.0,
                             "DIN 4108-4:2020-11, Tab. 2, Z. 5.7.1, Fn. b; DIN EN ISO 10456:2010-05, Tab. 4"),

            // ---- Platten -----------------------------------------------------------
            new BaustoffSaat(48, "Platten", "Gipskartonplatte 700", 0.21, 700.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 3.4; DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(49, "Platten", "Gipsfaserplatte 1200", 0.43, 1200.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(50, "Platten", "Zementgebundene Spanplatte", 0.23, 1200.0, 1500.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Bodenbeläge -------------------------------------------------------
            new BaustoffSaat(51, "Bodenbeläge", "Keramikfliese", 1.3, 2300.0, 840.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(52, "Bodenbeläge", "Parkett", 0.18, 700.0, 1600.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(53, "Bodenbeläge", "PVC-Bodenbelag", 0.25, 1700.0, 1400.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(54, "Bodenbeläge", "Linoleum", 0.17, 1200.0, 1400.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(55, "Bodenbeläge", "Teppichboden", 0.06, 200.0, 1300.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Abdichtungen ------------------------------------------------------
            new BaustoffSaat(56, "Abdichtungen", "Bitumenbahn", 0.17, 1200.0, 1000.0,
                             "DIN 4108-4:2020-11, Tab. 1, Z. 7.3.1; DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(57, "Abdichtungen", "Kunststoffbahn PVC-P", 0.14, 1200.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(58, "Abdichtungen", "Elastomerbahn EPDM", 0.25, 1150.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(59, "Abdichtungen", "PE-Folie", 0.33, 920.0, 2200.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Metalle und Glas --------------------------------------------------
            new BaustoffSaat(60, "Metalle und Glas", "Stahl", 50.0, 7800.0, 450.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(61, "Metalle und Glas", "Aluminium", 160.0, 2800.0, 880.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(62, "Metalle und Glas", "Zink", 110.0, 7200.0, 380.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(63, "Metalle und Glas", "Floatglas", 1.0, 2500.0, 750.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),

            // ---- Erdreich ----------------------------------------------------------
            new BaustoffSaat(64, "Erdreich", "Erdreich Ton und Schluff", 1.5, 1500.0, 2000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
            new BaustoffSaat(65, "Erdreich", "Erdreich Sand und Kies", 2.0, 2000.0, 1000.0,
                             "DIN EN ISO 10456:2010-05, Tab. 3"),
        };

        // =================================================================
        //  Auskunft
        // =================================================================

        /// <summary>Was ein Lauf getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Zahl der in diesem Lauf angelegten Tabellen (0 bis 2).</summary>
            public int TabellenAngelegt;

            /// <summary>Zahl der geschriebenen Saatzeilen.</summary>
            public int Gesaet;

            /// <summary>Die Zeile für Protokoll und Werkzeug.</summary>
            public string Zeile()
            {
                return TabellenAngelegt.ToString(CultureInfo.InvariantCulture) + " von 2 Tabelle(n) angelegt (" +
                       TAB_STAMM + ", " + TAB_PROJEKT + "), " + Gesaet.ToString(CultureInfo.InvariantCulture) +
                       " von " + Saat.Count.ToString(CultureInfo.InvariantCulture) +
                       " Saatzeile(n) geschrieben (ReadOnly = 1, Herkunft " + DbWerte.HERKUNFT_VORGABE + ")";
            }
        }

        /// <summary>Stehen beide Tabellen?</summary>
        public static bool TabellenVorhanden()
            => DataRepository.TabelleVorhanden(TAB_STAMM) && DataRepository.TabelleVorhanden(TAB_PROJEKT);

        /// <summary>
        /// Steht der Zielstand — beide Tabellen da, jede Saatzeile unter ihrer festen Id, die
        /// Folge über der Saatgrenze?
        /// </summary>
        public static bool Vollstaendig()
        {
            if (!TabellenVorhanden()) return false;
            object n = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + TAB_STAMM + "\" WHERE \"ID\" BETWEEN 1 AND ? AND \"ReadOnly\" = 1",
                new DbParam("@max", SAAT_ID_GRENZE - 1));
            if (n == null || n == DBNull.Value || Convert.ToInt64(n, CultureInfo.InvariantCulture) < Saat.Count)
                return false;
            object seq = DataRepository.ExecuteScalar(
                "SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@t", TAB_STAMM));
            return seq != null && seq != DBNull.Value &&
                   Convert.ToInt64(seq, CultureInfo.InvariantCulture) >= SAAT_ID_GRENZE - 1;
        }

        // =================================================================
        //  Ausführen
        // =================================================================

        /// <summary>
        /// Legt die Tabellen an (wenn sie fehlen) und schreibt die Saat — für
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>; die Migration der
        /// Schale geht denselben Weg über ihre eigenen Helfer. <b>Wiederholbar.</b> Fehler
        /// werfen — der Aufrufer meldet sie.
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var b = new Bericht();
            foreach (KeyValuePair<string, string> a in Anweisungen)
            {
                bool vorher = DataRepository.TabelleVorhanden(a.Key);
                DataRepository.ExecuteNonQuery(a.Value);
                if (!vorher && DataRepository.TabelleVorhanden(a.Key)) b.TabellenAngelegt++;
            }
            b.Gesaet = SaatSchreiben();
            return b;
        }

        /// <summary>
        /// Schreibt die fehlenden Saatzeilen und hebt danach die AUTOINCREMENT-Folge des
        /// Katalogs auf die Saatgrenze. <b>Wiederholbar und nie überschreibend:</b> Eine Zeile,
        /// deren Id schon belegt ist ODER deren Bezeichner schon als herstellerneutraler Satz
        /// dasteht, wird übergangen — ein zweiter Lauf ändert nichts, und eine vom Anwender
        /// geänderte Saatzeile bleibt, wie sie ist.
        /// </summary>
        /// <returns>Zahl der angelegten Zeilen.</returns>
        public static int SaatSchreiben()
        {
            int angelegt = 0;
            foreach (BaustoffSaat s in Saat)
            {
                if (Anzahl("SELECT COUNT(*) FROM \"" + TAB_STAMM + "\" WHERE \"ID\" = ?",
                           new DbParam("@id", s.Id)) > 0) continue;
                if (Anzahl("SELECT COUNT(*) FROM \"" + TAB_STAMM + "\" WHERE \"Bezeichner\" = ? AND \"Hersteller\" IS NULL",
                           new DbParam("@bez", s.Bezeichner)) > 0) continue;

                int n = DataRepository.ExecuteNonQuery(
                    "INSERT INTO \"" + TAB_STAMM + "\" (\"ID\", \"Bezeichner\", \"Gruppe\", \"Hersteller\", " +
                    "\"Lambda\", \"Rho\", \"cp\", \"Quelle\", \"Herkunft\", \"Quellkennung\", \"ReadOnly\") " +
                    "VALUES (?, ?, ?, NULL, ?, ?, ?, ?, ?, NULL, 1)",
                    new DbParam("@id", s.Id),
                    new DbParam("@bez", s.Bezeichner),
                    new DbParam("@gr", s.Gruppe),
                    new DbParam("@l", DbParamTyp.Double) { Wert = s.Lambda },
                    new DbParam("@r", DbParamTyp.Double) { Wert = s.Rho },
                    new DbParam("@c", DbParamTyp.Double) { Wert = s.Cp },
                    new DbParam("@q", s.Quelle),
                    new DbParam("@h", DbWerte.HERKUNFT_VORGABE));
                if (n == 1) angelegt++;
            }

            FolgeReservieren();
            return angelegt;
        }

        /// <summary>
        /// Hebt die AUTOINCREMENT-Folge des Katalogs auf <c>SAAT_ID_GRENZE − 1</c> — nie nach
        /// unten. Nach dem ersten <c>INSERT</c> steht die Zeile in <c>sqlite_sequence</c>
        /// bereits; das zweite Statement legt sie nur für einen Katalog an, in den noch nie
        /// geschrieben wurde.
        /// </summary>
        private static void FolgeReservieren()
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE sqlite_sequence SET seq = ? WHERE name = ? AND seq < ?",
                new DbParam("@s", SAAT_ID_GRENZE - 1),
                new DbParam("@t", TAB_STAMM),
                new DbParam("@g", SAAT_ID_GRENZE - 1));
            DataRepository.ExecuteNonQuery(
                "INSERT INTO sqlite_sequence (name, seq) SELECT ?, ? " +
                "WHERE NOT EXISTS (SELECT 1 FROM sqlite_sequence WHERE name = ?)",
                new DbParam("@t", TAB_STAMM),
                new DbParam("@s", SAAT_ID_GRENZE - 1),
                new DbParam("@t2", TAB_STAMM));
        }

        /// <summary>Die Saatzeile zu einer Id; <c>null</c>, wenn die Id keine Saatzeile ist.</summary>
        public static BaustoffSaat SaatZu(int id)
        {
            foreach (BaustoffSaat s in Saat) if (s.Id == id) return s;
            return null;
        }

        private static long Anzahl(string sql, params DbParam[] p)
        {
            object o = DataRepository.ExecuteScalar(sql, p);
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
