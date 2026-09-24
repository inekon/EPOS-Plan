using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die DDL des Zapfprofilgenerators</b> — Schemaschritt T1 „Katalog, Zonen, Projekt"
    /// (<c>Dokumentation/aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md</c>,
    /// Abschnitte 3.1 und 3.2, Stufe Z0), Schemaschritt T2 „Zapfkategorien" (Stufe Z3,
    /// Schritt 115, <see cref="AnweisungenT2"/>) und Schemaschritt T3 „Laufangaben der Auslegung
    /// und Bezugsart am Bedarfstag" (Stufe Z4, Schritt 124, <see cref="SpaltenT3"/>) und
    /// Schemaschritt T3 „Typtage" (Stufe Z4b, Schritt 131,
    /// <see cref="AnweisungenT3Typtage"/>): Den Papiernamen T3 des Konzepts 3.2 trägt im
    /// Bestand schon Schritt 124 — gemeint ist dort die Spaltenerweiterung, hier die Tabelle
    /// der Typtage (Nachtrag N14).
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

        /// <summary>
        /// Die eingespielten Typtage des lizenzierten Anwenders (Schemaschritt T3 „Typtage",
        /// Konzept 3.1 und 3.2, Stufe Z4b) — <b>anwenderlokal</b>: Sie kommen allein aus einem
        /// Normformvektorpaket des Anwenders (<c>Normformvektorleser</c>), tragen kein
        /// <c>ReadOnly</c> und keinen <c>Status</c>, wandern nicht in eine Projektkopie oder ein
        /// <c>.wpx</c>-Paket und werden von der Auslieferungsvorlage geleert (Konzept 3.2, 6).
        /// </summary>
        public const string TAB_TWW_TYPTAG_IMPORT = "Tab_TwwTyptag_IMPORT";

        /// <summary>
        /// Die eingespielten Messreihen EINES Projekts (Schemaschritt T4 „Messreihen", Konzept 4.8
        /// und Kapitel 7 Zeile Z5) — <b>anwenderlokal und Bestandteil des Projekts</b>: Sie kommen
        /// allein aus einer CSV-Datei des Anwenders (<c>Messreihenleser</c>), tragen kein
        /// <c>ReadOnly</c> und keinen <c>Status</c>, wandern über <c>ID_Projekt</c> mit einer
        /// Projektkopie und einem <c>.wpx</c>-Paket und werden von der Auslieferungsvorlage
        /// geleert (Kapitel 9 K5: Messdaten gehören dem Objekt, nie der Auslieferung).
        /// </summary>
        public const string TAB_TWW_MESSREIHE = "Tab_TwwMessreihe";

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

        /// <summary>
        /// Wertemengen der Spalten des Schemaschritts T3 (Schritt 124) — EINE Quelle für die
        /// CHECK-Klausel der DDL und die Prüfung des Schreibwegs, wie <see cref="PERZENTIL_WERTE"/>:
        /// Erzeugerart 1 Kessel, 2 Wärmepumpe; Werkstoff des Übertragers 1 Stahl, 2 Edelstahl;
        /// Bezug des Füllstands 1 Nenninhalt des Punkts, 2 Punkt, 3 Nenninhalt des Bands, 4 V_max;
        /// Bezugsart eines Bedarfstags 1 … 7 wie die Bezugsart der Nutzungsart.
        /// </summary>
        public const string ERZEUGERART_WERTE = "1,2";

        /// <summary>Wertemenge von <c>Tab_TwwProjekt.Uebertrager_Werkstoff</c> (siehe <see cref="ERZEUGERART_WERTE"/>).</summary>
        public const string WERKSTOFF_WERTE = "1,2";

        /// <summary>Wertemenge von <c>Tab_TwwProjekt.Fuellstand_Bezug</c> (siehe <see cref="ERZEUGERART_WERTE"/>).</summary>
        public const string FUELLSTAND_BEZUG_WERTE = "1,2,3,4";

        /// <summary>Wertemenge von <c>Tab_TwwBedarfstag_STAMM.Bezugsart</c> (siehe <see cref="ERZEUGERART_WERTE"/>).</summary>
        public const string BEZUGSART_WERTE = "1,2,3,4,5,6,7";

        // -----------------------------------------------------------------
        //  Die Satzarten der eingespielten Typtage (T3 „Typtage", Z4b)
        // -----------------------------------------------------------------

        /// <summary>
        /// <c>Art</c> = Kategoriezeile: <c>Typtag</c> ist der Code der Typtagkategorie,
        /// <c>Zeilenindex</c> 0 die Jahreszeit, 1 die Tagart, 2 die Bewölkung und <c>Wert</c>
        /// die Zahl der jeweiligen Aufzählung (<c>Typtagjahreszeit</c>,
        /// <c>Typtagart</c>, <c>Typtagbewoelkung</c>). Drei Zeilen je Kategorie;
        /// <c>Klimazone</c> = 0 und <c>Gebaeudeart</c> = "" — die Systematik gilt für jede
        /// Zone und Gebäudeart.
        /// </summary>
        public const string TYPTAG_ART_KATEGORIE = "KATEGORIE";

        /// <summary>
        /// <c>Art</c> = Zahl der Kalendertage einer Typtagkategorie je Klimazone und Gebäudeart
        /// (<c>Zeilenindex</c> 0, <c>Wert</c> ganzzahlig ≥ 0, Summe je Zone und Gebäudeart 365).
        /// </summary>
        public const string TYPTAG_ART_ANZAHL = "ANZAHL";

        /// <summary>
        /// <c>Art</c> = Faktor der Tagesenergie je Klimazone, Gebäudeart und Typtag
        /// (<c>Zeilenindex</c> 0). Er ist eine Schwankung um den Jahresmittelwert und darf
        /// negativ sein; positiv bleibt allein die Tagesmenge (Klemmung, Konzept 4.2).
        /// </summary>
        public const string TYPTAG_ART_FAKTOR = "FAKTOR";

        /// <summary>
        /// <c>Art</c> = normierter Tagesgang einer Typtagkategorie: <c>Aufloesung_min</c> das
        /// Zeitraster, <c>Zeilenindex</c> 0 … 1440/<c>Aufloesung_min</c> − 1 der Zeitabschnitt,
        /// <c>Wert</c> sein Anteil (Σ = 1, jeder ≥ 0); <c>Klimazone</c> = 0 (zonenunabhängig).
        /// Wahlfrei — ohne diese Zeilen trägt der Tagesgangsatz der Zone die Tagesform.
        /// </summary>
        public const string TYPTAG_ART_GANG = "GANG";

        /// <summary>
        /// <c>Art</c> = Kennwert des Verfahrens: <c>Typtag</c> trägt den Schlüssel (etwa die
        /// Heizgrenze einer Gebäudeart), <c>Wert</c> seinen Zahlenwert; <c>Klimazone</c> = 0 und
        /// <c>Zeilenindex</c> = 0. Die Werte stehen NIE im Quelltext — sie kommen aus dem Paket
        /// des Anwenders (Konzept Kapitel 6 (a)).
        /// </summary>
        public const string TYPTAG_ART_KENNWERT = "KENNWERT";

        /// <summary>Wertemenge von <c>Tab_TwwTyptag_IMPORT.Art</c> als SQL-Liste für den CHECK.</summary>
        public const string TYPTAG_ART_WERTE = "'KATEGORIE','ANZAHL','FAKTOR','GANG','KENNWERT'";

        /// <summary><c>Groesse</c> einer Messreihe: Energie je Zeitschritt [kWh].</summary>
        public const string MESSGROESSE_ENERGIE = "ENERGIE";

        /// <summary><c>Groesse</c> einer Messreihe: Volumen je Zeitschritt [m³].</summary>
        public const string MESSGROESSE_VOLUMEN = "VOLUMEN";

        /// <summary><c>Groesse</c> einer Messreihe: mittlere Leistung im Zeitschritt [kW].</summary>
        public const string MESSGROESSE_LEISTUNG = "LEISTUNG";

        /// <summary>Die Wertemenge der Spalte <c>Groesse</c> von <see cref="TAB_TWW_MESSREIHE"/>.</summary>
        public const string MESSGROESSE_WERTE = "'ENERGIE','VOLUMEN','LEISTUNG'";

        /// <summary>Die drei Messgrößen in der Reihenfolge der Wertemenge.</summary>
        public static readonly IReadOnlyList<string> Messgroessen = new[]
        {
            MESSGROESSE_ENERGIE, MESSGROESSE_VOLUMEN, MESSGROESSE_LEISTUNG
        };

        /// <summary>Die Satzarten der eingespielten Typtage in der Reihenfolge von <see cref="TYPTAG_ART_WERTE"/>.</summary>
        public static readonly IReadOnlyList<string> TyptagArten = new[]
        {
            TYPTAG_ART_KATEGORIE, TYPTAG_ART_ANZAHL, TYPTAG_ART_FAKTOR, TYPTAG_ART_GANG, TYPTAG_ART_KENNWERT
        };

        /// <summary>Eine Wertemenge als Zahlen — für die Prüfung des Schreibwegs.</summary>
        public static IReadOnlyList<int> Werte(string wertemenge)
            => System.Array.ConvertAll(wertemenge.Split(','), s => int.Parse(s, CultureInfo.InvariantCulture));

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

        // =================================================================
        //  Schemaschritt T3 „Typtage" (Schritt 131, Stufe Z4b): die
        //  eingespielten Typtage des lizenzierten Anwenders
        // =================================================================

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_TwwTyptag_IMPORT</c> — 11 Spalten (Konzept 3.1,
        /// Schemaschritt T3 „Typtage", Stufe Z4b).
        ///
        /// <para><b>Eine Zeile je Wert</b> (Konzept 3.1 „Werte als Zeilen"): <c>Art</c> sagt,
        /// was die Zeile trägt (<see cref="TYPTAG_ART_KATEGORIE"/>,
        /// <see cref="TYPTAG_ART_ANZAHL"/>, <see cref="TYPTAG_ART_FAKTOR"/>,
        /// <see cref="TYPTAG_ART_GANG"/>, <see cref="TYPTAG_ART_KENNWERT"/>), und (<c>Art</c>,
        /// <c>Klimazone</c>, <c>Gebaeudeart</c>, <c>Typtag</c>, <c>Zeilenindex</c>) ist ihr
        /// natürlicher Schlüssel. <c>Klimazone</c> = 0 heißt „für jede Zone",
        /// <c>Gebaeudeart</c> = "" „für jede Gebäudeart".</para>
        ///
        /// <para><b>Kein <c>Status</c>, kein <c>ReadOnly</c>, keine Provenienzgruppe</b> (Konzept
        /// 3.1): Jede Zeile ist eingespielt — <c>Quelle</c> und <c>Ausgabe</c> nennen die
        /// Richtlinie des Anwenders, <c>Datum_Import</c> den Tag des Einspielens. Die Tabelle
        /// gehört nie zur Auslieferung: <c>Werkzeuge/Auslieferungsvorlage</c> leert sie, und der
        /// Projekttransfer trägt sie nicht (sie führt kein <c>ID_Projekt</c> und endet nicht auf
        /// <c>_STAMM</c>).</para>
        ///
        /// <para><b>Keine Werte im Quelltext.</b> Die DDL beschreibt allein die Struktur; jeder
        /// Wert kommt aus dem Paket des Anwenders (Konzept Kapitel 6).</para>
        /// </summary>
        public const string SQL_CREATE_TYPTAG_IMPORT =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwTyptag_IMPORT\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Art\" TEXT NOT NULL CHECK (\"Art\" IN (" + TYPTAG_ART_WERTE + ")),\n" +
            "    \"Klimazone\" INTEGER NOT NULL CHECK (\"Klimazone\" >= 0),\n" +
            "    \"Gebaeudeart\" TEXT NOT NULL,\n" +
            "    \"Typtag\" TEXT NOT NULL,\n" +
            "    \"Aufloesung_min\" INTEGER CHECK (\"Aufloesung_min\" BETWEEN 1 AND 1440),\n" +
            "    \"Zeilenindex\" INTEGER NOT NULL CHECK (\"Zeilenindex\" >= 0),\n" +
            "    \"Wert\" REAL NOT NULL,\n" +
            "    \"Quelle\" TEXT NOT NULL,\n" +
            "    \"Ausgabe\" TEXT,\n" +
            "    \"Datum_Import\" TEXT NOT NULL,\n" +
            "    UNIQUE (\"Art\", \"Klimazone\", \"Gebaeudeart\", \"Typtag\", \"Zeilenindex\")\n" +
            ") STRICT";

        /// <summary>
        /// Die Anweisungen des Schemaschritts T3 „Typtage" (Schritt 131, Stufe Z4b): die
        /// eingespielten Typtage. Die Tabelle steht für sich — kein Fremdschlüssel, kein Verweis
        /// auf einen Katalog —, sie darf deshalb vor oder nach den übrigen entstehen. Reines DDL,
        /// wiederholbar über <c>IF NOT EXISTS</c>; nach dem Schritt ist sie leer, und kein
        /// Rechenweg findet Typtage (der Weg ist benannt nicht verfügbar).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> AnweisungenT3Typtage
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_TWW_TYPTAG_IMPORT, SQL_CREATE_TYPTAG_IMPORT);
            }
        }

        // =================================================================
        //  Schemaschritt T3 „Typtage" (Schritt 131, Stufe Z4b): die WAHL des
        //  Typtagwegs je Projekt
        // =================================================================

        /// <summary>
        /// <c>Tab_TwwProjekt.Typtage_Aktiv</c> — rechnet der Jahresgang des Projekts über die
        /// eingespielten Typtage (1) oder wie im Bestand über den Formvektor (0)? Vorgabe 0: Ohne
        /// die Wahl des Anwenders rechnet das Projekt genau wie vor dem Schritt.
        /// </summary>
        public const string SPALTE_TYPTAGE_AKTIV = "Typtage_Aktiv";

        /// <summary>
        /// <c>Tab_TwwProjekt.Typtage_Klimazone</c> — die gewählte Klimazone des eingespielten
        /// Pakets (&gt; 0); NULL = keine Wahl. Die Nummer ist die des Pakets, nicht eine Kennung der
        /// Datenbank — <c>Tab_Klimaregion</c> führt keine TRY-Zone (N14 (d)).
        /// </summary>
        public const string SPALTE_TYPTAGE_KLIMAZONE = "Typtage_Klimazone";

        /// <summary>
        /// <c>Tab_TwwProjekt.Typtage_Gebaeudeart</c> — die gewählte Gebäudeart des eingespielten
        /// Pakets; NULL oder leer = keine Wahl. Der Text ist der des Pakets (Konzept Kapitel 6: die
        /// Namen kommen aus dem Paket des Anwenders, nicht aus dem Quelltext).
        /// </summary>
        public const string SPALTE_TYPTAGE_GEBAEUDEART = "Typtage_Gebaeudeart";

        /// <summary>
        /// <b>Die drei Spalten der Wahl des Typtagwegs</b> (Schritt 131, Stufe Z4b, Gruppe 2;
        /// Umsetzungskonzept Zapfprofilgenerator N14 (i), Folge (b)): Sie stehen im SELBEN Schritt
        /// wie <see cref="AnweisungenT3Typtage"/> — die Tabelle der eingespielten Werte und die
        /// Wahl, die sie benutzt, gehören zusammen, und der Schritt war noch nicht ausgerollt.
        ///
        /// <para><b>Gespeichert wird die WAHL, nie ein Wert.</b> Die Zeilen der Typtage stehen in
        /// <c>Tab_TwwTyptag_IMPORT</c> und bleiben anwenderlokal; die drei Spalten nennen nur, ob
        /// das Projekt sie benutzt und mit welcher Zone und Gebäudeart. Ein Projekttransfer trägt
        /// deshalb die Wahl mit — die Daten nicht (Konzept Kapitel 6).</para>
        ///
        /// <para><b>Reines DDL, ergebnisneutral:</b> Nach dem Schritt steht <c>Typtage_Aktiv</c> auf
        /// 0 und beide Angaben auf NULL — so, wie der Kern ohne Spalte rechnete (Formvektor wie im
        /// Bestand). Je Spalte die SQLite-Definition hinter <c>ADD COLUMN</c> (STRICT-Typen);
        /// EINE Quelle für Migration, <c>Werkzeuge/Testdatenbankschema</c>, die Testhelfer und den
        /// Nachweis.</para>
        /// </summary>
        public static readonly IReadOnlyList<TwwSpalte> SpaltenT3Typtage = new[]
        {
            new TwwSpalte(TAB_TWW_PROJEKT, SPALTE_TYPTAGE_AKTIV,
                "INTEGER NOT NULL DEFAULT 0 CHECK (\"" + SPALTE_TYPTAGE_AKTIV + "\" IN (0,1))"),
            new TwwSpalte(TAB_TWW_PROJEKT, SPALTE_TYPTAGE_KLIMAZONE,
                "INTEGER CHECK (\"" + SPALTE_TYPTAGE_KLIMAZONE + "\" > 0)"),
            new TwwSpalte(TAB_TWW_PROJEKT, SPALTE_TYPTAGE_GEBAEUDEART, "TEXT"),
        };

        /// <summary>Stehen alle Spalten von <see cref="SpaltenT3Typtage"/> (die Wahl des Typtagwegs, Schritt 131)?</summary>
        public static bool T3TyptageVollstaendig()
            => SpaltenT3Typtage.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Name));

        /// <summary>
        /// Legt die Spalten von <see cref="SpaltenT3Typtage"/> in EINEM Vorgang an — für
        /// <c>Werkzeuge/Testdatenbankschema</c> und die Testhelfer; die Migration der Schale geht
        /// denselben Weg über ihre eigenen Helfer, aus denselben Definitionen. <b>Wiederholbar:</b>
        /// Eine vorhandene Spalte wird übergangen, eine fehlende Tabelle (Stand vor 103) ebenso.
        /// <b>Kein DML.</b>
        /// </summary>
        /// <param name="bericht">Nimmt eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (höchstens drei).</returns>
        public static int T3TyptageAlle(IList<string> bericht)
        {
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var fehlend = SpaltenT3Typtage.Where(s => DataRepository.TabelleVorhanden(s.Tabelle)
                                                      && !DataRepository.SpalteVorhanden(s.Tabelle, s.Name)).ToList();
            int angelegt = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (TwwSpalte s in fehlend)
                    {
                        v.Ausfuehren(SpalteAnlegen(s));
                        angelegt++;
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                         SpaltenT3Typtage.Count.ToString(CultureInfo.InvariantCulture) +
                         " Spalte(n) angelegt (Wahl des Typtagwegs an " + TAB_TWW_PROJEKT + ")");
            return angelegt;
        }

        // =================================================================
        //  Schemaschritt T4 „Messreihen" (Schritt 135, Stufe Z5): die
        //  eingespielten Messreihen eines Projekts
        // =================================================================

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_TwwMessreihe</c> — 10 Spalten (Konzept 4.8,
        /// Schemaschritt T4 „Messreihen", Stufe Z5).
        ///
        /// <para><b>Eine Zeile je Wert</b> (Muster von <see cref="SQL_CREATE_TYPTAG_IMPORT"/>):
        /// <c>Zeilenindex</c> zählt die Zeitschritte ab <c>Beginn</c> bei 0, <c>Wert</c> trägt den
        /// gemessenen Wert in der Einheit der <c>Groesse</c>
        /// (<see cref="MESSGROESSE_ENERGIE"/> kWh, <see cref="MESSGROESSE_VOLUMEN"/> m³,
        /// <see cref="MESSGROESSE_LEISTUNG"/> kW), und
        /// (<c>ID_Projekt</c>, <c>Bezeichnung</c>, <c>Zeilenindex</c>) ist ihr natürlicher
        /// Schlüssel. Die Kopfangaben — <c>Groesse</c>, <c>Aufloesung_min</c>, <c>Beginn</c>,
        /// <c>Quelle</c>, <c>Datum_Import</c> — stehen an jeder Zeile: Die Reihe ist EIN Vorgang,
        /// und der Schreibweg schreibt sie geschlossen (<c>TwwMessreihenCtrl</c>).</para>
        ///
        /// <para><b>Bestandteil des Projekts</b> (Kapitel 9 K5): <c>ID_Projekt</c> mit
        /// <c>ON DELETE CASCADE</c> — die Reihe gehört dem Projekt, reist mit seiner Kopie und
        /// seinem Paket und verschwindet mit ihm. <b>Kein <c>Status</c>, kein <c>ReadOnly</c>,
        /// keine Provenienzgruppe:</b> Jede Zeile ist gemessen, <c>Quelle</c> nennt die Herkunft
        /// beim Anwender, <c>Datum_Import</c> den Tag des Einspielens.</para>
        ///
        /// <para><b>Keine Werte im Quelltext.</b> Die DDL beschreibt allein die Struktur; jeder
        /// Wert kommt aus einer Datei des Anwenders. <c>Werkzeuge/Auslieferungsvorlage</c> leert
        /// die Tabelle — auch die Beispielprojekte führen keine Messreihe.</para>
        ///
        /// <para><b>Beginn</b> ist ein Datum <c>JJJJ-MM-TT</c> (ISO, invariant) samt Uhrzeit des
        /// ersten Zeitschritts als <c>JJJJ-MM-TTThh:mm</c>; die Reihe braucht keinen Jahresbezug
        /// des Rechenkerns (der rechnet 365 Tage ohne Schaltjahr, die Messung kennt ihren echten
        /// Kalender).</para>
        /// </summary>
        public const string SQL_CREATE_MESSREIHE =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwMessreihe\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Projekt\" INTEGER NOT NULL REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Bezeichnung\" TEXT NOT NULL,\n" +
            "    \"Groesse\" TEXT NOT NULL CHECK (\"Groesse\" IN (" + MESSGROESSE_WERTE + ")),\n" +
            "    \"Aufloesung_min\" INTEGER NOT NULL CHECK (\"Aufloesung_min\" BETWEEN 1 AND 1440),\n" +
            "    \"Beginn\" TEXT NOT NULL,\n" +
            "    \"Zeilenindex\" INTEGER NOT NULL CHECK (\"Zeilenindex\" >= 0),\n" +
            "    \"Wert\" REAL NOT NULL CHECK (\"Wert\" >= 0),\n" +
            "    \"Quelle\" TEXT NOT NULL,\n" +
            "    \"Datum_Import\" TEXT NOT NULL,\n" +
            "    UNIQUE (\"ID_Projekt\", \"Bezeichnung\", \"Zeilenindex\")\n" +
            ") STRICT";

        /// <summary>
        /// Die Anweisungen des Schemaschritts T4 „Messreihen" (Schritt 135, Stufe Z5): die Tabelle
        /// der eingespielten Messreihen. Sie hängt allein an <c>Tab_Projekt</c> und darf deshalb
        /// nach den übrigen Tww-Tabellen entstehen. Reines DDL, wiederholbar über
        /// <c>IF NOT EXISTS</c>; nach dem Schritt ist sie leer, und kein Rechenweg findet eine
        /// Messreihe (der Vergleich ist benannt nicht verfügbar).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> AnweisungenT4Messreihen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_TWW_MESSREIHE, SQL_CREATE_MESSREIHE);
            }
        }

        /// <summary>
        /// Der Index des Schemaschritts T4 auf <c>Tab_TwwMessreihe.ID_Projekt</c> — der Suchweg
        /// jedes Zugriffs (die Reihen EINES Projekts) und die Suche, die SQLite beim Löschen eines
        /// Projekts nach seinen Kindern anstellt. Nach <see cref="AnweisungenT4Messreihen"/>
        /// abzuarbeiten; wiederholbar über <c>IF NOT EXISTS</c>.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> IndizesT4Messreihen
        {
            get { yield return Index(TAB_TWW_MESSREIHE, "ID_Projekt"); }
        }

        // =================================================================
        //  Schemaschritt T3 (Schritt 124, Stufe Z4): Laufangaben der Auslegung
        //  und Bezugsart am Bedarfstag
        // =================================================================

        /// <summary><c>Tab_TwwProjekt.Erzeugerart</c> — die gewählte Erzeugerart am Speicher; NULL = keine Angabe (Vorschlag des Anlagenbestands).</summary>
        public const string SPALTE_ERZEUGERART = "Erzeugerart";

        /// <summary><c>Tab_TwwProjekt.Uebertrager_Werkstoff</c> — der Werkstoff des Übertragers; NULL = keine Angabe.</summary>
        public const string SPALTE_UEBERTRAGER_WERKSTOFF = "Uebertrager_Werkstoff";

        /// <summary><c>Tab_TwwProjekt.Personen_Auto</c> — Personen des Verfahrensvergleichs aus dem Mengengerüst (1) oder manuell (0).</summary>
        public const string SPALTE_PERSONEN_AUTO = "Personen_Auto";

        /// <summary><c>Tab_TwwProjekt.Personen_Manuell</c> — der manuelle Wert der Personen; wirkt nur bei <c>Personen_Auto</c> = 0.</summary>
        public const string SPALTE_PERSONEN_MANUELL = "Personen_Manuell";

        /// <summary><c>Tab_TwwProjekt.Fuellstand_Bezug</c> — der Bezug des Füllstands; NULL = Vorgabe.</summary>
        public const string SPALTE_FUELLSTAND_BEZUG = "Fuellstand_Bezug";

        /// <summary><c>Tab_TwwBedarfstag_STAMM.Bezugsart</c> — die Bezugsart der Bezugsmenge des Tags; NULL = ohne Angabe.</summary>
        public const string SPALTE_BEZUGSART = "Bezugsart";

        /// <summary>
        /// <b>Die Spalten des Schemaschritts T3</b> (Schritt 124; Umsetzungskonzept
        /// Zapfprofilgenerator N10 (i)/(j), N11 (d)/(i)/(j)): an <c>Tab_TwwProjekt</c> die
        /// Laufangaben der Auslegung — Erzeugerart und Werkstoff des Übertragers (nullbar, CHECK
        /// der Wertemenge), Personen auto/manuell (0/1, <c>NOT NULL DEFAULT 1</c>, CHECK) samt
        /// manuellem Wert (≥ 0) und der Bezug des Füllstands (nullbar, CHECK) —, an
        /// <c>Tab_TwwBedarfstag_STAMM</c> die Bezugsart der Bezugsmenge (nullbar, CHECK). Je
        /// Spalte die SQLite-Definition hinter <c>ADD COLUMN</c> (STRICT-Typen <c>INTEGER</c> und
        /// <c>REAL</c>); EINE Quelle für Migration, <c>Werkzeuge/Testdatenbankschema</c>, die
        /// Testhelfer und den Nachweis. <b>Reines DDL, ergebnisneutral:</b> Nach dem Schritt stehen
        /// alle Werte auf NULL bzw. <c>Personen_Auto</c> = 1 — so, wie der Kern ohne Spalte rechnete.
        /// </summary>
        public static readonly IReadOnlyList<TwwSpalte> SpaltenT3 = new[]
        {
            new TwwSpalte(TAB_TWW_PROJEKT, SPALTE_ERZEUGERART,
                "INTEGER CHECK (\"" + SPALTE_ERZEUGERART + "\" IN (" + ERZEUGERART_WERTE + "))"),
            new TwwSpalte(TAB_TWW_PROJEKT, SPALTE_UEBERTRAGER_WERKSTOFF,
                "INTEGER CHECK (\"" + SPALTE_UEBERTRAGER_WERKSTOFF + "\" IN (" + WERKSTOFF_WERTE + "))"),
            new TwwSpalte(TAB_TWW_PROJEKT, SPALTE_PERSONEN_AUTO,
                "INTEGER NOT NULL DEFAULT 1 CHECK (\"" + SPALTE_PERSONEN_AUTO + "\" IN (0,1))"),
            new TwwSpalte(TAB_TWW_PROJEKT, SPALTE_PERSONEN_MANUELL,
                "REAL CHECK (\"" + SPALTE_PERSONEN_MANUELL + "\" >= 0)"),
            new TwwSpalte(TAB_TWW_PROJEKT, SPALTE_FUELLSTAND_BEZUG,
                "INTEGER CHECK (\"" + SPALTE_FUELLSTAND_BEZUG + "\" IN (" + FUELLSTAND_BEZUG_WERTE + "))"),
            new TwwSpalte(TAB_TWW_BEDARFSTAG_STAMM, SPALTE_BEZUGSART,
                "INTEGER CHECK (\"" + SPALTE_BEZUGSART + "\" IN (" + BEZUGSART_WERTE + "))"),
        };

        /// <summary>Die Anweisung, die eine Spalte von <see cref="SpaltenT3"/> anlegt (<c>ALTER TABLE … ADD COLUMN</c>).</summary>
        public static string SpalteAnlegen(TwwSpalte s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Name + "\" " + s.Definition;

        /// <summary>Steht Schritt 124? Alle Spalten von <see cref="SpaltenT3"/> stehen.</summary>
        public static bool T3Vollstaendig()
            => SpaltenT3.All(s => DataRepository.SpalteVorhanden(s.Tabelle, s.Name));

        /// <summary>
        /// Führt Schritt 124 in EINEM Vorgang aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// die Testhelfer; die Migration der Schale geht denselben Weg über ihre eigenen Helfer, aus
        /// denselben Definitionen. <b>Wiederholbar:</b> Eine vorhandene Spalte wird übergangen;
        /// eine fehlende Tabelle (Stand vor 103) ebenso. <b>Kein DML.</b>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (höchstens sechs).</returns>
        public static int T3Alle(IList<string> bericht)
        {
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung.
            var fehlend = SpaltenT3.Where(s => DataRepository.TabelleVorhanden(s.Tabelle)
                                               && !DataRepository.SpalteVorhanden(s.Tabelle, s.Name)).ToList();
            int angelegt = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (TwwSpalte s in fehlend)
                    {
                        v.Ausfuehren(SpalteAnlegen(s));
                        angelegt++;
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                         SpaltenT3.Count.ToString(CultureInfo.InvariantCulture) +
                         " Spalte(n) angelegt (Laufangaben der Auslegung an Tab_TwwProjekt, Bezugsart an " +
                         "Tab_TwwBedarfstag_STAMM)");
            return angelegt;
        }

        /// <summary>Alle Tww-Tabellen der Schritte T1, T2, T3 „Typtage" und T4 „Messreihen" in Anlegereihenfolge.</summary>
        public static IEnumerable<KeyValuePair<string, string>> AlleAnweisungen
        {
            get
            {
                foreach (KeyValuePair<string, string> a in Anweisungen) yield return a;
                foreach (KeyValuePair<string, string> a in AnweisungenT2) yield return a;
                foreach (KeyValuePair<string, string> a in AnweisungenT3Typtage) yield return a;
                foreach (KeyValuePair<string, string> a in AnweisungenT4Messreihen) yield return a;
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

    /// <summary>Eine Spalte eines Tww-Schemaschritts: Tabelle, Name und die SQLite-Definition hinter <c>ADD COLUMN</c>.</summary>
    public sealed record TwwSpalte(string Tabelle, string Name, string Definition);
}
