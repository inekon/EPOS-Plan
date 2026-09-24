using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die DDL des Zapfprofilgenerators</b> — Schemaschritt T1 „Katalog, Zonen, Projekt"
    /// (<c>Dokumentation/aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md</c>,
    /// Abschnitte 3.1 und 3.2, Stufe Z0) und Schemaschritt T2 „Zapfkategorien" (Stufe Z3,
    /// Schritt 115, <see cref="AnweisungenT2"/>).
    ///
    /// <para><b>Eine Quelle für Migration und Testdatenbank.</b> Dieselben zehn Tabellen
    /// legen <c>SchemaMigration</c> beim Programmstart und <c>Werkzeuge/Testdatenbankschema</c>
    /// beim Nachziehen der Messlatte <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> an — Muster
    /// <see cref="WechselrichterSchema"/>. Zwei abgeschriebene <c>CREATE TABLE</c> wären zwei
    /// Schemata.</para>
    ///
    /// <para><b>STRICT, <c>IF NOT EXISTS</c>, Beziehungen über IDs.</b> Jede Beziehung ist ein
    /// deklarierter Fremdschlüssel auf die Spalte <c>ID</c> der Zieltabelle; Wahrheitswerte
    /// sind <c>INTEGER NOT NULL DEFAULT … CHECK (… IN (0,1))</c>, Aufzählungen INTEGER mit
    /// CHECK der Wertemenge, und bei jeder Überschreibung heißt NULL „Vorgabe". Die
    /// Idempotenz trägt <c>IF NOT EXISTS</c>.</para>
    ///
    /// <para><b>Stabile Kennungen.</b> Jede <c>ID</c> ist <c>INTEGER PRIMARY KEY
    /// AUTOINCREMENT</c> wie bei <see cref="WechselrichterSchema"/>: Die ID einer gelöschten
    /// Zeile wird nie wieder vergeben — auch nicht die höchste, etwa eine entfernte
    /// <c>IMPORT</c>-Zeile. Die unveränderlichen Katalogversionen (Konzept 3.2) setzen das
    /// voraus.</para>
    ///
    /// <para><b>Indizes auf den Kindspalten.</b> <see cref="Indizes"/> legt nach den Tabellen
    /// je einen Index auf die Fremdschlüsselspalten, über die ein Löschen der Elternzeile
    /// die Kinder sucht (CASCADE bzw. Sperre). Wer <see cref="Anweisungen"/> abarbeitet,
    /// arbeitet danach <see cref="Indizes"/> ab.</para>
    ///
    /// <para><b>Provenienzgruppen.</b> Die Kataloge tragen je Wertgruppe vier Spalten
    /// <c>Quelle</c>, <c>Ausgabe</c>, <c>Version</c> und <c>Herkunftsart</c> — in
    /// <c>Tab_TwwNutzungsart_STAMM</c> mit dem Präfix der Gruppe (<c>Bedarf_</c>,
    /// <c>Jahresgang_</c>, <c>Wochengang_</c>), in den übrigen Katalogen ohne Präfix.
    /// <c>Quelle</c> nennt nur Norm, Verfahren oder Eigenkonstruktion samt Ausgabe, nie einen
    /// Hersteller; eine Sekundärquelle steht in der internen Spalte <c>Beleg</c>, die
    /// Oberfläche, Bericht und KiSicht nie zeigen (Konzept Kapitel 6 (e)).</para>
    ///
    /// <para><b>Keine Werte.</b> Die Klasse enthält nur Struktur: keine Auslieferungszeile,
    /// keine Normzahl, keinen Kennwert (Konzept Kapitel 6 (a)). Die Auslieferungswerte kommen
    /// aus einem Katalogpaket außerhalb des Repositoriums; die Testdatenbank trägt nur einen
    /// fiktiven Katalog (Kapitel 6 (b)).</para>
    ///
    /// <para><b>Ergebnisneutral.</b> Alle Anweisungen sind reines DDL ohne DML: Nach dem
    /// Schritt sind die zehn Tabellen leer, kein Projekt steht auf dem Generator, und kein
    /// Rechenweg liest sie. Der Referenzlauf bleibt byte-gleich.</para>
    /// </summary>
    public static class TwwSchema
    {
        // =================================================================
        //  Die Tabellennamen
        // =================================================================

        /// <summary>Der Tagesgangsatz — ein Satz von vier Tagesgängen, eigenständig geschlüsselt, damit eine Zone einen anderen Satz als den ihrer Nutzungsart wählen kann.</summary>
        public const string TAB_TWW_TAGESGANGSATZ_STAMM = "Tab_TwwTagesgangsatz_STAMM";

        /// <summary>Die Tagesgänge — Tagesgangsatz × Tagtyp × 24 Stundenanteile, Provenienz je Tagtyp.</summary>
        public const string TAB_TWW_TAGESGANG_STAMM = "Tab_TwwTagesgang_STAMM";

        /// <summary>Der Katalog der Nutzungsarten (S0) — Auslieferung und Anwenderkopien in einer Tabelle, Provenienz je Wertgruppe (Bedarf, Jahresgang, Wochengang).</summary>
        public const string TAB_TWW_NUTZUNGSART_STAMM = "Tab_TwwNutzungsart_STAMM";

        /// <summary>Der Kopf eines Bedarfstags der Auslegung (Konzept 4.5).</summary>
        public const string TAB_TWW_BEDARFSTAG_STAMM = "Tab_TwwBedarfstag_STAMM";

        /// <summary>Die Ereignisse eines Bedarfstags — eine Minutenreihe ist eine Ereignisliste mit Dauer 1; eine 1440-Spalten-Tabelle gibt es nicht.</summary>
        public const string TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM = "Tab_TwwBedarfstagEreignis_STAMM";

        /// <summary>Die gekapselten Normkonstanten, Regelwerksgrenzen und INEKON-Setzungen — der Schlüssel steht im Code, der Wert nur hier.</summary>
        public const string TAB_TWW_PARAMETER_STAMM = "Tab_TwwParameter_STAMM";

        /// <summary>Die Katalogwerte der DIN-4708-Kennzahl (Belegung je Raumzahl, Ausstattungsklassen) — nie abgedruckt.</summary>
        public const string TAB_TWW_DIN4708_WERT_STAMM = "Tab_TwwDin4708Wert_STAMM";

        /// <summary>Die Zonen eines Projekts — Verweis auf eine unveränderliche Katalogversion der Nutzungsart, nullbare Überschreibungen (NULL = Vorgabe).</summary>
        public const string TAB_TWW_ZONE = "Tab_TwwZone";

        /// <summary>Die Wohnungstabelle einer Zone — Grundlage der DIN-4708-Kennzahl und des Mengengerüsts Wohnen.</summary>
        public const string TAB_TWW_WOHNUNGSTYP = "Tab_TwwWohnungstyp";

        /// <summary>Weiche und gebäudeweite Größen, höchstens eine Zeile je Projekt.</summary>
        public const string TAB_TWW_PROJEKT = "Tab_TwwProjekt";

        /// <summary>
        /// Die Zapfkategorien je Nutzungsart (Schemaschritt T2, Konzept 3.1 und 4.4) — Volumenstrom,
        /// Streuung, Dauer, Anteil und obere Kappung der stochastischen Ziehung.
        /// </summary>
        public const string TAB_TWW_ZAPFKATEGORIE_STAMM = "Tab_TwwZapfkategorie_STAMM";

        // =================================================================
        //  Die Wertemengen der Textspalten mit CHECK
        // =================================================================

        /// <summary><c>Status</c>: Zeile der Auslieferung (kommt nur aus dem Katalogpaket, nie aus dem Repositorium).</summary>
        public const string STATUS_AUSLIEFERUNG = "AUSLIEFERUNG";

        /// <summary><c>Status</c>: vom Anwender angelegte oder kopierte Zeile; auch der fiktive Testkatalog.</summary>
        public const string STATUS_EIGEN = "EIGEN";

        /// <summary><c>Status</c>: aus einer Datei eingespielte Zeile; die Auslieferungsvorlage entfernt sie.</summary>
        public const string STATUS_IMPORT = "IMPORT";

        /// <summary><c>Herkunftsart</c> einer Wertgruppe: aus einem Verfahren gerechnet.</summary>
        public const string HERKUNFT_VERFAHREN = "VERFAHREN";

        /// <summary><c>Herkunftsart</c>: Eigenkonstruktion.</summary>
        public const string HERKUNFT_EIGENKONSTRUKTION = "EIGENKONSTRUKTION";

        /// <summary><c>Herkunftsart</c>: frei verfügbare Quelle.</summary>
        public const string HERKUNFT_FREI = "FREI";

        /// <summary><c>Herkunftsart</c>: vom Anwender eingespielt.</summary>
        public const string HERKUNFT_IMPORT = "IMPORT";

        /// <summary><c>Herkunftsart</c>: erfundener Wert (Testkatalog).</summary>
        public const string HERKUNFT_FIKTIV = "FIKTIV";

        /// <summary><c>Tab_TwwProjekt.Weg</c>: der Brauchwasserkanal rechnet wie im Bestand (Vorgabe).</summary>
        public const string WEG_BESTAND = "BESTAND";

        /// <summary><c>Tab_TwwProjekt.Weg</c>: der Brauchwasserkanal kommt aus dem Zapfprofilgenerator.</summary>
        public const string WEG_GENERATOR = "GENERATOR";

        /// <summary><c>Tab_TwwDin4708Wert_STAMM.Art</c>: Belegung je Raumzahl.</summary>
        public const string DIN4708_ART_BELEGUNG = "BELEGUNG";

        /// <summary><c>Tab_TwwDin4708Wert_STAMM.Art</c>: Ausstattungsklasse.</summary>
        public const string DIN4708_ART_AUSSTATTUNG = "AUSSTATTUNG";

        /// <summary>
        /// Wertemenge von <c>Tab_TwwProjekt.Perzentil</c> — EINE Quelle für die CHECK-Klausel der
        /// DDL und die Prüfung des Schreibwegs (<see cref="Perzentile"/>).
        /// </summary>
        public const string PERZENTIL_WERTE = "95,99";

        /// <summary>Untergrenze von <c>Realisierungen</c> und <c>Realisierungen_Auslegung</c> — DDL und Schreibweg.</summary>
        public const string REALISIERUNGEN_MINDESTENS = "1";

        /// <summary><see cref="PERZENTIL_WERTE"/> als Zahlen.</summary>
        public static readonly IReadOnlyList<int> Perzentile =
            System.Array.ConvertAll(PERZENTIL_WERTE.Split(','), s => int.Parse(s, System.Globalization.CultureInfo.InvariantCulture));

        /// <summary><see cref="REALISIERUNGEN_MINDESTENS"/> als Zahl.</summary>
        public static readonly int RealisierungenMindestens =
            int.Parse(REALISIERUNGEN_MINDESTENS, System.Globalization.CultureInfo.InvariantCulture);

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_TwwTagesgangsatz_STAMM</c> — 6 Spalten (Konzept 3.1).</summary>
        public const string SQL_CREATE_TAGESGANGSATZ =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwTagesgangsatz_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Bezeichner\" TEXT NOT NULL,\n" +
            "    \"Katalogversion\" TEXT NOT NULL,\n" +
            "    \"Status\" TEXT NOT NULL CHECK (\"Status\" IN ('AUSLIEFERUNG','EIGEN','IMPORT')),\n" +
            "    \"Beleg\" TEXT,\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)),\n" +
            "    UNIQUE (\"Bezeichner\", \"Katalogversion\")\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_TwwTagesgang_STAMM</c> — 31 Spalten (Konzept 3.1).</summary>
        public const string SQL_CREATE_TAGESGANG =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwTagesgang_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Tagesgangsatz\" INTEGER NOT NULL REFERENCES \"Tab_TwwTagesgangsatz_STAMM\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Tagtyp\" INTEGER NOT NULL CHECK (\"Tagtyp\" IN (1,2,3,4)),\n" +
            "    \"Anteil_01\" REAL NOT NULL,\n" +
            "    \"Anteil_02\" REAL NOT NULL,\n" +
            "    \"Anteil_03\" REAL NOT NULL,\n" +
            "    \"Anteil_04\" REAL NOT NULL,\n" +
            "    \"Anteil_05\" REAL NOT NULL,\n" +
            "    \"Anteil_06\" REAL NOT NULL,\n" +
            "    \"Anteil_07\" REAL NOT NULL,\n" +
            "    \"Anteil_08\" REAL NOT NULL,\n" +
            "    \"Anteil_09\" REAL NOT NULL,\n" +
            "    \"Anteil_10\" REAL NOT NULL,\n" +
            "    \"Anteil_11\" REAL NOT NULL,\n" +
            "    \"Anteil_12\" REAL NOT NULL,\n" +
            "    \"Anteil_13\" REAL NOT NULL,\n" +
            "    \"Anteil_14\" REAL NOT NULL,\n" +
            "    \"Anteil_15\" REAL NOT NULL,\n" +
            "    \"Anteil_16\" REAL NOT NULL,\n" +
            "    \"Anteil_17\" REAL NOT NULL,\n" +
            "    \"Anteil_18\" REAL NOT NULL,\n" +
            "    \"Anteil_19\" REAL NOT NULL,\n" +
            "    \"Anteil_20\" REAL NOT NULL,\n" +
            "    \"Anteil_21\" REAL NOT NULL,\n" +
            "    \"Anteil_22\" REAL NOT NULL,\n" +
            "    \"Anteil_23\" REAL NOT NULL,\n" +
            "    \"Anteil_24\" REAL NOT NULL,\n" +
            "    \"Quelle\" TEXT NOT NULL,\n" +
            "    \"Ausgabe\" TEXT,\n" +
            "    \"Version\" TEXT NOT NULL,\n" +
            "    \"Herkunftsart\" TEXT NOT NULL CHECK (\"Herkunftsart\" IN ('VERFAHREN','EIGENKONSTRUKTION','FREI','IMPORT','FIKTIV')),\n" +
            "    UNIQUE (\"ID_Tagesgangsatz\", \"Tagtyp\")\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_TwwNutzungsart_STAMM</c> — 55 Spalten (Konzept 3.1).</summary>
        public const string SQL_CREATE_NUTZUNGSART =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwNutzungsart_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Bezeichner\" TEXT NOT NULL,\n" +
            "    \"Katalogversion\" TEXT NOT NULL,\n" +
            "    \"Bezugsart\" INTEGER NOT NULL CHECK (\"Bezugsart\" IN (1,2,3,4,5,6,7)),\n" +
            "    \"Bedarf_Niedrig\" REAL NOT NULL,\n" +
            "    \"Bedarf_Mittel\" REAL NOT NULL,\n" +
            "    \"Bedarf_Hoch\" REAL NOT NULL,\n" +
            "    \"Bedarf_Niedrig_Min\" REAL,\n" +
            "    \"Bedarf_Niedrig_Max\" REAL,\n" +
            "    \"Bedarf_Mittel_Min\" REAL,\n" +
            "    \"Bedarf_Mittel_Max\" REAL,\n" +
            "    \"Bedarf_Hoch_Min\" REAL,\n" +
            "    \"Bedarf_Hoch_Max\" REAL,\n" +
            "    \"Bedarf_Quelle\" TEXT NOT NULL,\n" +
            "    \"Bedarf_Ausgabe\" TEXT,\n" +
            "    \"Bedarf_Version\" TEXT NOT NULL,\n" +
            "    \"Bedarf_Herkunftsart\" TEXT NOT NULL CHECK (\"Bedarf_Herkunftsart\" IN ('VERFAHREN','EIGENKONSTRUKTION','FREI','IMPORT','FIKTIV')),\n" +
            "    \"Bezug_Zapftemperatur\" REAL NOT NULL,\n" +
            "    \"Bezug_Kaltwasser\" REAL NOT NULL,\n" +
            "    \"Bilanzgrenze\" INTEGER NOT NULL CHECK (\"Bilanzgrenze\" IN (1,2,3)),\n" +
            "    \"Kalenderart\" INTEGER NOT NULL CHECK (\"Kalenderart\" IN (1,2,3,4,5)),\n" +
            "    \"Ferienfaktor\" REAL,\n" +
            "    \"Monat_1\" REAL NOT NULL,\n" +
            "    \"Monat_2\" REAL NOT NULL,\n" +
            "    \"Monat_3\" REAL NOT NULL,\n" +
            "    \"Monat_4\" REAL NOT NULL,\n" +
            "    \"Monat_5\" REAL NOT NULL,\n" +
            "    \"Monat_6\" REAL NOT NULL,\n" +
            "    \"Monat_7\" REAL NOT NULL,\n" +
            "    \"Monat_8\" REAL NOT NULL,\n" +
            "    \"Monat_9\" REAL NOT NULL,\n" +
            "    \"Monat_10\" REAL NOT NULL,\n" +
            "    \"Monat_11\" REAL NOT NULL,\n" +
            "    \"Monat_12\" REAL NOT NULL,\n" +
            "    \"Jahresgang_Quelle\" TEXT NOT NULL,\n" +
            "    \"Jahresgang_Ausgabe\" TEXT,\n" +
            "    \"Jahresgang_Version\" TEXT NOT NULL,\n" +
            "    \"Jahresgang_Herkunftsart\" TEXT NOT NULL CHECK (\"Jahresgang_Herkunftsart\" IN ('VERFAHREN','EIGENKONSTRUKTION','FREI','IMPORT','FIKTIV')),\n" +
            "    \"Woche_1\" REAL NOT NULL,\n" +
            "    \"Woche_2\" REAL NOT NULL,\n" +
            "    \"Woche_3\" REAL NOT NULL,\n" +
            "    \"Woche_4\" REAL NOT NULL,\n" +
            "    \"Woche_5\" REAL NOT NULL,\n" +
            "    \"Woche_6\" REAL NOT NULL,\n" +
            "    \"Woche_7\" REAL NOT NULL,\n" +
            "    \"Wochengang_Quelle\" TEXT NOT NULL,\n" +
            "    \"Wochengang_Ausgabe\" TEXT,\n" +
            "    \"Wochengang_Version\" TEXT NOT NULL,\n" +
            "    \"Wochengang_Herkunftsart\" TEXT NOT NULL CHECK (\"Wochengang_Herkunftsart\" IN ('VERFAHREN','EIGENKONSTRUKTION','FREI','IMPORT','FIKTIV')),\n" +
            "    \"ID_Tagesgangsatz\" INTEGER NOT NULL REFERENCES \"Tab_TwwTagesgangsatz_STAMM\" (\"ID\"),\n" +
            "    \"ID_Vorlage\" INTEGER REFERENCES \"Tab_TwwNutzungsart_STAMM\" (\"ID\") ON DELETE SET NULL,\n" +
            "    \"Status\" TEXT NOT NULL CHECK (\"Status\" IN ('AUSLIEFERUNG','EIGEN','IMPORT')),\n" +
            "    \"Beleg\" TEXT,\n" +
            "    \"Freigabe\" TEXT,\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)),\n" +
            "    UNIQUE (\"Bezeichner\", \"Katalogversion\")\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_TwwBedarfstag_STAMM</c> — 12 Spalten (Konzept 3.1).</summary>
        public const string SQL_CREATE_BEDARFSTAG =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwBedarfstag_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Bezeichner\" TEXT NOT NULL,\n" +
            "    \"Katalogversion\" TEXT NOT NULL,\n" +
            "    \"Quelle_Art\" INTEGER NOT NULL CHECK (\"Quelle_Art\" IN (2,3,4,5)),\n" +
            "    \"Bezugsmenge\" REAL,\n" +
            "    \"Quelle\" TEXT NOT NULL,\n" +
            "    \"Ausgabe\" TEXT,\n" +
            "    \"Version\" TEXT NOT NULL,\n" +
            "    \"Herkunftsart\" TEXT NOT NULL CHECK (\"Herkunftsart\" IN ('VERFAHREN','EIGENKONSTRUKTION','FREI','IMPORT','FIKTIV')),\n" +
            "    \"Status\" TEXT NOT NULL CHECK (\"Status\" IN ('AUSLIEFERUNG','EIGEN','IMPORT')),\n" +
            "    \"Beleg\" TEXT,\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)),\n" +
            "    UNIQUE (\"Bezeichner\", \"Katalogversion\")\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_TwwBedarfstagEreignis_STAMM</c> — 6 Spalten (Konzept 3.1).</summary>
        public const string SQL_CREATE_BEDARFSTAG_EREIGNIS =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwBedarfstagEreignis_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Bedarfstag\" INTEGER NOT NULL REFERENCES \"Tab_TwwBedarfstag_STAMM\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Minute_Beginn\" INTEGER NOT NULL CHECK (\"Minute_Beginn\" BETWEEN 0 AND 1439),\n" +
            "    \"Dauer_min\" INTEGER NOT NULL CHECK (\"Dauer_min\" >= 1),\n" +
            "    \"Energie_Kwh\" REAL NOT NULL,\n" +
            "    \"Reihenfolge\" INTEGER NOT NULL\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_TwwParameter_STAMM</c> — 12 Spalten (Konzept 3.1).</summary>
        public const string SQL_CREATE_PARAMETER =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwParameter_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Schluessel\" TEXT NOT NULL,\n" +
            "    \"Wert\" REAL NOT NULL,\n" +
            "    \"Einheit\" TEXT,\n" +
            "    \"Katalogversion\" TEXT NOT NULL,\n" +
            "    \"Quelle\" TEXT NOT NULL,\n" +
            "    \"Ausgabe\" TEXT,\n" +
            "    \"Version\" TEXT NOT NULL,\n" +
            "    \"Herkunftsart\" TEXT NOT NULL CHECK (\"Herkunftsart\" IN ('VERFAHREN','EIGENKONSTRUKTION','FREI','IMPORT','FIKTIV')),\n" +
            "    \"Status\" TEXT NOT NULL CHECK (\"Status\" IN ('AUSLIEFERUNG','EIGEN','IMPORT')),\n" +
            "    \"Beleg\" TEXT,\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)),\n" +
            "    UNIQUE (\"Schluessel\", \"Katalogversion\")\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_TwwDin4708Wert_STAMM</c> — 12 Spalten (Konzept 3.1).</summary>
        public const string SQL_CREATE_DIN4708_WERT =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwDin4708Wert_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Art\" TEXT NOT NULL CHECK (\"Art\" IN ('BELEGUNG','AUSSTATTUNG')),\n" +
            "    \"Schluessel\" TEXT NOT NULL,\n" +
            "    \"Wert\" REAL NOT NULL,\n" +
            "    \"Katalogversion\" TEXT NOT NULL,\n" +
            "    \"Quelle\" TEXT NOT NULL,\n" +
            "    \"Ausgabe\" TEXT,\n" +
            "    \"Version\" TEXT NOT NULL,\n" +
            "    \"Herkunftsart\" TEXT NOT NULL CHECK (\"Herkunftsart\" IN ('VERFAHREN','EIGENKONSTRUKTION','FREI','IMPORT','FIKTIV')),\n" +
            "    \"Status\" TEXT NOT NULL CHECK (\"Status\" IN ('AUSLIEFERUNG','EIGEN','IMPORT')),\n" +
            "    \"Beleg\" TEXT,\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)),\n" +
            "    UNIQUE (\"Art\", \"Schluessel\", \"Katalogversion\")\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_TwwZone</c> — 45 Spalten (Konzept 3.1).</summary>
        public const string SQL_CREATE_ZONE =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwZone\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Nutzungsart\" INTEGER NOT NULL REFERENCES \"Tab_TwwNutzungsart_STAMM\" (\"ID\"),\n" +
            "    \"ID_Tagesgangsatz\" INTEGER REFERENCES \"Tab_TwwTagesgangsatz_STAMM\" (\"ID\"),\n" +
            "    \"ID_Gebaeude\" INTEGER REFERENCES \"Tab_Gebaeude\" (\"ID\") ON DELETE SET NULL,\n" +
            "    \"Reihenfolge\" INTEGER NOT NULL,\n" +
            "    \"Name\" TEXT NOT NULL,\n" +
            "    \"Bezugsmenge\" REAL NOT NULL CHECK (\"Bezugsmenge\" > 0),\n" +
            "    \"Niveau\" INTEGER NOT NULL DEFAULT 2 CHECK (\"Niveau\" IN (1,2,3)),\n" +
            "    \"Personen_je_WE\" REAL,\n" +
            "    \"Wohnflaeche_je_WE\" REAL,\n" +
            "    \"Topologie\" INTEGER NOT NULL DEFAULT 1 CHECK (\"Topologie\" IN (1,2,3,4)),\n" +
            "    \"Zirkulation\" INTEGER NOT NULL DEFAULT 1 CHECK (\"Zirkulation\" IN (0,1)),\n" +
            "    \"Ferienbeginn_1\" INTEGER CHECK (\"Ferienbeginn_1\" BETWEEN 0 AND 366),\n" +
            "    \"Ferienende_1\" INTEGER CHECK (\"Ferienende_1\" BETWEEN 0 AND 366),\n" +
            "    \"Ferienbeginn_2\" INTEGER CHECK (\"Ferienbeginn_2\" BETWEEN 0 AND 366),\n" +
            "    \"Ferienende_2\" INTEGER CHECK (\"Ferienende_2\" BETWEEN 0 AND 366),\n" +
            "    \"Ferienbeginn_3\" INTEGER CHECK (\"Ferienbeginn_3\" BETWEEN 0 AND 366),\n" +
            "    \"Ferienende_3\" INTEGER CHECK (\"Ferienende_3\" BETWEEN 0 AND 366),\n" +
            "    \"Ferienbeginn_4\" INTEGER CHECK (\"Ferienbeginn_4\" BETWEEN 0 AND 366),\n" +
            "    \"Ferienende_4\" INTEGER CHECK (\"Ferienende_4\" BETWEEN 0 AND 366),\n" +
            "    \"Jahresmesswert\" REAL,\n" +
            "    \"Jahresmesswert_Einheit\" INTEGER CHECK (\"Jahresmesswert_Einheit\" IN (1,2)),\n" +
            "    \"Jahresmesswert_Bilanzgrenze\" INTEGER CHECK (\"Jahresmesswert_Bilanzgrenze\" IN (1,2,3)),\n" +
            "    \"Jahresmesswert_Quelle\" TEXT,\n" +
            "    \"Jahresmesswert_Zeitraum\" TEXT,\n" +
            "    \"Speicherverlust_Kwh_a\" REAL,\n" +
            "    \"Tagesbedarf_Auto\" INTEGER NOT NULL DEFAULT 1 CHECK (\"Tagesbedarf_Auto\" IN (0,1)),\n" +
            "    \"Tagesbedarf_Manuell_Kwh\" REAL,\n" +
            "    \"Bedarf_Spez\" REAL,\n" +
            "    \"Zapftemperatur\" REAL,\n" +
            "    \"Kaltwasser_Mittel\" REAL,\n" +
            "    \"Kaltwasser_Amplitude\" REAL,\n" +
            "    \"Auslastung_01\" REAL,\n" +
            "    \"Auslastung_02\" REAL,\n" +
            "    \"Auslastung_03\" REAL,\n" +
            "    \"Auslastung_04\" REAL,\n" +
            "    \"Auslastung_05\" REAL,\n" +
            "    \"Auslastung_06\" REAL,\n" +
            "    \"Auslastung_07\" REAL,\n" +
            "    \"Auslastung_08\" REAL,\n" +
            "    \"Auslastung_09\" REAL,\n" +
            "    \"Auslastung_10\" REAL,\n" +
            "    \"Auslastung_11\" REAL,\n" +
            "    \"Auslastung_12\" REAL\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_TwwWohnungstyp</c> — 7 Spalten (Konzept 3.1).</summary>
        public const string SQL_CREATE_WOHNUNGSTYP =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwWohnungstyp\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Zone\" INTEGER NOT NULL REFERENCES \"Tab_TwwZone\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Anzahl\" INTEGER NOT NULL CHECK (\"Anzahl\" > 0),\n" +
            "    \"Raumzahl\" REAL,\n" +
            "    \"Personen\" REAL,\n" +
            "    \"ID_Ausstattung\" INTEGER REFERENCES \"Tab_TwwDin4708Wert_STAMM\" (\"ID\"),\n" +
            "    \"Reihenfolge\" INTEGER NOT NULL\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_TwwProjekt</c> — 40 Spalten (Konzept 3.1).</summary>
        public const string SQL_CREATE_PROJEKT =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwProjekt\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL UNIQUE REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Weg\" TEXT NOT NULL DEFAULT 'BESTAND' CHECK (\"Weg\" IN ('BESTAND','GENERATOR')),\n" +
            "    \"Jahresreihe_Stochastisch\" INTEGER NOT NULL DEFAULT 0 CHECK (\"Jahresreihe_Stochastisch\" IN (0,1)),\n" +
            "    \"Seed\" INTEGER NOT NULL DEFAULT 1,\n" +
            "    \"Realisierungen\" INTEGER NOT NULL DEFAULT 10 CHECK (\"Realisierungen\" >= " + REALISIERUNGEN_MINDESTENS + "),\n" +
            "    \"Realisierungen_Auslegung\" INTEGER CHECK (\"Realisierungen_Auslegung\" >= " + REALISIERUNGEN_MINDESTENS + "),\n" +
            "    \"Perzentil\" INTEGER NOT NULL DEFAULT 99 CHECK (\"Perzentil\" IN (" + PERZENTIL_WERTE + ")),\n" +
            "    \"Zirk_Auto\" INTEGER NOT NULL DEFAULT 1 CHECK (\"Zirk_Auto\" IN (0,1)),\n" +
            "    \"Zirk_Methode\" INTEGER NOT NULL DEFAULT 3 CHECK (\"Zirk_Methode\" IN (1,2,3)),\n" +
            "    \"Zirk_Lage\" INTEGER CHECK (\"Zirk_Lage\" IN (1,2)),\n" +
            "    \"Zirk_Laenge_m\" REAL,\n" +
            "    \"Zirk_Verlust_W_m\" REAL,\n" +
            "    \"Zirk_Anteil\" REAL,\n" +
            "    \"Zirk_Kennwert\" REAL,\n" +
            "    \"Zirk_Flaeche_m2\" REAL,\n" +
            "    \"Zirk_Laufzeit_h\" REAL,\n" +
            "    \"Zirk_Manuell_Kw\" REAL,\n" +
            "    \"Leitungsinhalt_l\" REAL,\n" +
            "    \"Lade_Auto\" INTEGER NOT NULL DEFAULT 1 CHECK (\"Lade_Auto\" IN (0,1)),\n" +
            "    \"Ladefenster_h\" REAL,\n" +
            "    \"Ladefenster_Beginn_h\" REAL,\n" +
            "    \"Lade_Manuell_Kw\" REAL,\n" +
            "    \"Speicher_C\" REAL,\n" +
            "    \"Kaltwasser_Auslegung_C\" REAL,\n" +
            "    \"Erzeuger_Kw\" REAL,\n" +
            "    \"Uebertrager_Kw\" REAL,\n" +
            "    \"Uebertrager_UA_W_K\" REAL,\n" +
            "    \"Uebertrager_Flaeche_m2\" REAL,\n" +
            "    \"Speicherart\" INTEGER NOT NULL DEFAULT 1 CHECK (\"Speicherart\" IN (1,2)),\n" +
            "    \"Sensorhoehe_Anteil\" REAL,\n" +
            "    \"Nachweis_Volumen_l\" REAL,\n" +
            "    \"Speicherverlust_W\" REAL,\n" +
            "    \"Nutzanteil\" REAL,\n" +
            "    \"Zuschlag\" REAL,\n" +
            "    \"Bedarfstag_Quelle\" INTEGER CHECK (\"Bedarfstag_Quelle\" IN (1,2,3,4,5)),\n" +
            "    \"ID_Bedarfstag\" INTEGER REFERENCES \"Tab_TwwBedarfstag_STAMM\" (\"ID\") ON DELETE SET NULL,\n" +
            "    \"Auslegung_Volumen_l\" REAL,\n" +
            "    \"Auslegung_Leistung_Kw\" REAL,\n" +
            "    \"Aenderungsdatum\" TEXT\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_TwwZapfkategorie_STAMM</c> — 16 Spalten (Schemaschritt
        /// T2, Konzept 3.1 und 4.4; Spaltennamen wie die Klasse <c>Zapfkategorie</c> und der
        /// Skriptblock <c>ZAPFKATEGORIEN</c> des Testkatalogs).
        ///
        /// <para><b>Eine Kategorie gehört zu genau einer Nutzungsart</b> (<c>ID_Nutzungsart</c>,
        /// <c>ON DELETE CASCADE</c>): Sie ist Teil der unveränderlichen Katalogversion ihrer
        /// Nutzungsart und trägt deshalb keine eigene Katalogversion; der natürliche Schlüssel ist
        /// (<c>ID_Nutzungsart</c>, <c>Kategorie</c>). Dessen Index trägt zugleich die Suche der
        /// Kaskade. <c>Reihenfolge</c> ordnet die Kategorien wie im Katalog (4.4).</para>
        ///
        /// <para><b>Werte:</b> <c>Volumenstrom_l_min</c> μ und <c>Sigma</c> σ [l/min] nicht
        /// negativ, <c>Dauer_min</c> 1 … 1440, <c>Anteil</c> an der Tagesmenge nicht negativ,
        /// <c>Kappung_l_min</c> die obere Kappung des Volumenstroms (<c>Zapfkategorie.KappungLJeMin</c>;
        /// NULL = keine obere Kappung, nur die bei 0). Die Provenienz steht ohne Präfix (<c>Quelle</c>,
        /// <c>Ausgabe</c>, <c>Version</c>, <c>Herkunftsart</c>); dazu <c>Status</c>, der interne
        /// <c>Beleg</c> und <c>ReadOnly</c> wie bei den übrigen Katalogen — eine Kategorie ist
        /// als Katalogkopie eigens pflegbar (Konzept 4.4, Stufe Z4).</para>
        /// </summary>
        public const string SQL_CREATE_ZAPFKATEGORIE =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwZapfkategorie_STAMM\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Nutzungsart\" INTEGER NOT NULL REFERENCES \"Tab_TwwNutzungsart_STAMM\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Kategorie\" TEXT NOT NULL,\n" +
            "    \"Reihenfolge\" INTEGER NOT NULL,\n" +
            "    \"Volumenstrom_l_min\" REAL NOT NULL CHECK (\"Volumenstrom_l_min\" >= 0),\n" +
            "    \"Dauer_min\" INTEGER NOT NULL CHECK (\"Dauer_min\" BETWEEN 1 AND 1440),\n" +
            "    \"Anteil\" REAL NOT NULL CHECK (\"Anteil\" >= 0),\n" +
            "    \"Sigma\" REAL NOT NULL CHECK (\"Sigma\" >= 0),\n" +
            "    \"Kappung_l_min\" REAL CHECK (\"Kappung_l_min\" > 0),\n" +
            "    \"Quelle\" TEXT NOT NULL,\n" +
            "    \"Ausgabe\" TEXT,\n" +
            "    \"Version\" TEXT NOT NULL,\n" +
            "    \"Herkunftsart\" TEXT NOT NULL CHECK (\"Herkunftsart\" IN ('VERFAHREN','EIGENKONSTRUKTION','FREI','IMPORT','FIKTIV')),\n" +
            "    \"Status\" TEXT NOT NULL CHECK (\"Status\" IN ('AUSLIEFERUNG','EIGEN','IMPORT')),\n" +
            "    \"Beleg\" TEXT,\n" +
            "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1)),\n" +
            "    UNIQUE (\"ID_Nutzungsart\", \"Kategorie\")\n" +
            ") STRICT";

        /// <summary>
        /// Alle zehn Anweisungen in Anlegereihenfolge, je Tabellenname — so, wie Migration
        /// und Werkzeug sie abarbeiten. Die Reihenfolge folgt den Fremdschlüsseln: erst die
        /// Tabelle, auf die verwiesen wird, dann die verweisende (Tagesgangsatz vor
        /// Nutzungsart, Nutzungsart vor Zone, Zone vor Wohnungstyp, Bedarfstag vor
        /// Ereignis und Projekt).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_TWW_TAGESGANGSATZ_STAMM, SQL_CREATE_TAGESGANGSATZ);
                yield return new KeyValuePair<string, string>(TAB_TWW_TAGESGANG_STAMM, SQL_CREATE_TAGESGANG);
                yield return new KeyValuePair<string, string>(TAB_TWW_NUTZUNGSART_STAMM, SQL_CREATE_NUTZUNGSART);
                yield return new KeyValuePair<string, string>(TAB_TWW_BEDARFSTAG_STAMM, SQL_CREATE_BEDARFSTAG);
                yield return new KeyValuePair<string, string>(TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM, SQL_CREATE_BEDARFSTAG_EREIGNIS);
                yield return new KeyValuePair<string, string>(TAB_TWW_PARAMETER_STAMM, SQL_CREATE_PARAMETER);
                yield return new KeyValuePair<string, string>(TAB_TWW_DIN4708_WERT_STAMM, SQL_CREATE_DIN4708_WERT);
                yield return new KeyValuePair<string, string>(TAB_TWW_ZONE, SQL_CREATE_ZONE);
                yield return new KeyValuePair<string, string>(TAB_TWW_WOHNUNGSTYP, SQL_CREATE_WOHNUNGSTYP);
                yield return new KeyValuePair<string, string>(TAB_TWW_PROJEKT, SQL_CREATE_PROJEKT);
            }
        }

        /// <summary>
        /// Die Anweisungen des Schemaschritts T2 (Schritt 115, Stufe Z3): die Zapfkategorien.
        /// Sie verweisen auf <see cref="TAB_TWW_NUTZUNGSART_STAMM"/> und laufen deshalb NACH
        /// <see cref="Anweisungen"/>; ein eigener Index entfällt, weil der natürliche Schlüssel
        /// mit <c>ID_Nutzungsart</c> beginnt. Reines DDL, wiederholbar über <c>IF NOT EXISTS</c>.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> AnweisungenT2
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_TWW_ZAPFKATEGORIE_STAMM, SQL_CREATE_ZAPFKATEGORIE);
            }
        }

        /// <summary>Alle Tww-Tabellen der Schritte T1 und T2 in Anlegereihenfolge.</summary>
        public static IEnumerable<KeyValuePair<string, string>> AlleAnweisungen
        {
            get
            {
                foreach (KeyValuePair<string, string> a in Anweisungen) yield return a;
                foreach (KeyValuePair<string, string> a in AnweisungenT2) yield return a;
            }
        }

        /// <summary>
        /// Die Indizes auf den Kindspalten, je Indexname — nach <see cref="Anweisungen"/>
        /// abzuarbeiten, denn sie brauchen ihre Tabelle. Wiederholbar über
        /// <c>IF NOT EXISTS</c>. Sie tragen die Suche, die SQLite beim Löschen einer
        /// Elternzeile nach ihren Kindern anstellt: Projekt → Zone, Nutzungsart → Zone,
        /// Zone → Wohnungstyp, Bedarfstag → Ereignis.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Indizes
        {
            get
            {
                yield return Index(TAB_TWW_ZONE, "ID_Projekt");
                yield return Index(TAB_TWW_ZONE, "ID_Nutzungsart");
                yield return Index(TAB_TWW_WOHNUNGSTYP, "ID_Zone");
                yield return Index(TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM, "ID_Bedarfstag");
            }
        }

        /// <summary>
        /// Ein Index auf einer Spalte, benannt <c>Tabelle_Spalte</c>. Beide Namen sind
        /// Konstanten dieser Klasse bzw. Spalten aus der DDL oben; <c>TwwSchemaTests</c>
        /// legt jeden Index an und prüft, dass seine Spalte ein Fremdschlüssel ist.
        /// </summary>
        private static KeyValuePair<string, string> Index(string tabelle, string spalte)
        {
            string name = tabelle + "_" + spalte;
            return new KeyValuePair<string, string>(name,
                "CREATE INDEX IF NOT EXISTS \"" + name + "\" ON \"" + tabelle + "\" (\"" + spalte + "\")");
        }
    }
}
