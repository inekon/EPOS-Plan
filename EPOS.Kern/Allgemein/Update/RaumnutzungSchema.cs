using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // NP-S1 - DER KATALOG DER NUTZUNGSPROFILE (Schritt 189; Entscheide E90, E91; Konzept
    // Nutzungsprofile Kapitel 4 und 5).
    //
    // WAS DER SCHRITT ANLEGT (alle STRICT, Beziehungen ueber IDs, Boolean 0/1 mit CHECK):
    //
    //   Tab_Raumnutzungskatalog   die Kategorie: Bezeichner (eindeutig ohne Unterschied der Schreibung),
    //                             Art (EPOS_MUSTER, DIN_V_18599_10, SIA_2024, VDI_2078, EIGEN),
    //                             Beschreibung, Quellenhinweis, ReadOnly, Reihenfolge
    //   Tab_Raumnutzungsprofil    das Profil: ID_Katalog -> Kategorie (CASCADE), Nummer (Text, eindeutig je
    //                             Kategorie), Bezeichner (eindeutig je Kategorie), Beschreibung, ReadOnly
    //                             und 25 Kennwerte, jeder nullbar (NP-F6)
    //   Tab_Raumnutzungszeile     das Zeilenbild (NP-F7): Spalten und Pruefklauseln von
    //                             Tab_Konditionierungsvorgabe ohne NENNWERT und SAISON, eindeutig je
    //                             (Profil, Groesse, Zeile)
    //   Tab_Raumnutzungsstunden   die Stundenprofile (NP-F9): je (Profil, Groesse, Tagesart) 24 Werte
    //   Tab_Raumnutzungszuordnung die Zuordnung (NP-F12): Art, Schluessel, ID_Profil (SET NULL = "keine"),
    //                             ReadOnly; eindeutig je (Art, Schluessel ohne Unterschied der Schreibung)
    //
    // WAS ER AN BESTANDSTABELLEN AENDERT:
    //
    //   Tab_Konditionierungskalender.Nutzung und Tab_Konditionierungsvorlage_STAMM.Nutzung: der CHECK auf
    //   die vier Kennungen wird eine Laengenpruefung (1..120, NP-F15). TABELLENNEUBAU nach dem Rezept der
    //   Schritte 96 und 152 (KonditionierungVorlagenSchema): Zieltext = GELTENDER sqlite_master.sql mit
    //   genau EINER ersetzten Stelle, Umbenennen unter legacy_alter_table (beide Tabellen sind
    //   ELTERNtabellen: Periode -> Kalender, Kalender und Vorgabe -> Vorlage), Zeilen namentlich mit IDs,
    //   AUTOINCREMENT-Stand, Indizes und Trigger des Bestands Wort fuer Wort. Kein Wert aendert sich:
    //   WOHNEN/BUERO/SCHULE/SONSTIGE bleiben, wie sie sind.
    //
    //   Tab_Zone.Nutzungsprofil   TEXT nullbar, 1..120 Zeichen - der Profilname als Kopie (Q41), nie eine ID.
    //
    // DIE SAAT (RaumnutzungSaat, INSERT ... WHERE NOT EXISTS / OR IGNORE - wiederholbar): vier Kategorien,
    // neun EPOS-Muster samt Zeilenbild, die 43 Nutzungen der DIN/TS 18599-10:2025-10 ohne Werte (E96), die
    // Zuordnung und in Z_Nutzungsprofil die Musternamen Buero und Schule unter Quelle KONDITIONIERUNG
    // (NP-F15). Steht noch die Kategorie DIN der Saat 2018, stellt RaumnutzungDinTsSchema.AlteFassungUmbauen
    // sie vor der Saat auf die Ausgabe 2025 (Schritt 190) - sonst entstuende eine zweite Kategorie -, und
    // RaumnutzungDinTsSchema.ZuordnungUmstellen die Zuordnung DIN_NUMMER auf die Zaehlung 2025 - sonst saete
    // die Saat 30, 33, 37, 43 neben 28, 29, 35, 41.
    //
    // ALLES IN EINEM VORGANG ohne Fremdschluessel (DROP einer Elterntabelle loest sonst die Kaskade aus);
    // vor dem Commit: foreign_key_check leer und die Kindtabellen verweisen namentlich auf die
    // umgebauten Tabellen. Scheitert ein Teil, bleibt die Datei, wie sie war.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest die neuen Tabellen oder die Zonenspalte; die Kalender
    // behalten Zeilen, IDs und Werte. Der Referenzlauf bleibt byte-gleich (NP-F24).
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl: ein Paket fuehrt keinen Katalog; die Zonenspalte
    // kommt leer an).
    // ====================================================================================

    /// <summary>
    /// <b>NP-S1</b> — Katalog der Nutzungsprofile (fünf Tabellen), freie Nutzung an Kalender und Vorlage,
    /// Profilname an der Zone, Saat. EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis
    /// (ADR-001 Option C). Anlass, Rezept und Saat stehen im Kopf der Datei.
    /// </summary>
    public static class RaumnutzungSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht (angemeldet als 189).
        /// </summary>
        public const int SCHRITT = VorlaufwahlSchema.SCHRITT + 1;

        // =================================================================
        //  Namen
        // =================================================================

        /// <summary>Die Kategorien des Katalogs.</summary>
        public const string TAB_KATALOG = "Tab_Raumnutzungskatalog";

        /// <summary>Die Profile.</summary>
        public const string TAB_PROFIL = "Tab_Raumnutzungsprofil";

        /// <summary>Das Zeilenbild der Profile.</summary>
        public const string TAB_ZEILE = "Tab_Raumnutzungszeile";

        /// <summary>Die Stundenprofile.</summary>
        public const string TAB_STUNDEN = "Tab_Raumnutzungsstunden";

        /// <summary>Die Zuordnung DIN-Nummer / IFC-Klasse / HottCAD-Raumtyp → Profil.</summary>
        public const string TAB_ZUORDNUNG = "Tab_Raumnutzungszuordnung";

        /// <summary>Die fünf neuen Tabellen in Anlagereihenfolge (Eltern vor Kindern).</summary>
        public static readonly IReadOnlyList<string> TABELLEN = new[] { TAB_KATALOG, TAB_PROFIL, TAB_ZEILE, TAB_STUNDEN, TAB_ZUORDNUNG };

        /// <summary>Die Zonentabelle.</summary>
        public const string TAB_ZONE = "Tab_Zone";

        /// <summary>Die Spalte des Profilnamens an der Zone (Q41).</summary>
        public const string SPALTE_ZONE_NUTZUNGSPROFIL = "Nutzungsprofil";

        /// <summary>Die Spalte der Nutzung an Kalender und Vorlage.</summary>
        public const string SPALTE_NUTZUNG = KonditionierungVorlagenSchema.SPALTE_NUTZUNG;

        /// <summary>Die zwei Bestandstabellen, deren Nutzung freier Text wird — in Umbaureihenfolge.</summary>
        public static readonly IReadOnlyList<string> TABELLEN_NUTZUNG = new[]
        {
            KonditionierungVorlagenSchema.TAB_VORLAGE, KonditionierungSchema.TAB_KALENDER
        };

        /// <summary>Höchstlänge eines Nutzungs- bzw. Profilnamens an Kalender, Vorlage und Zone.</summary>
        public const int NUTZUNG_MAX_ZEICHEN = 120;

        /// <summary>Höchstlänge eines Bezeichners (Kategorie, Profil, Schlüssel).</summary>
        public const int BEZEICHNER_MAX_ZEICHEN = 80;

        /// <summary>Höchstlänge von Beschreibung und Quellenhinweis.</summary>
        public const int BESCHREIBUNG_MAX_ZEICHEN = 400;

        /// <summary>Höchstlänge der Nummer in der Quelle.</summary>
        public const int NUMMER_MAX_ZEICHEN = 20;

        /// <summary>Höchstlänge des Textes eines Stundenprofils (24 Zahlen mit Semikolon).</summary>
        public const int STUNDENWERTE_MAX_ZEICHEN = 600;

        /// <summary>Art der Kategorie: EPOS-Muster.</summary>
        public const string ART_EPOS_MUSTER = "EPOS_MUSTER";
        /// <summary>Art der Kategorie: DIN 18599-10 — die Kennung nennt die Ausgabe 2018, die Kategorie führt seit Schritt
        /// <see cref="RaumnutzungDinTsSchema.SCHRITT"/> die DIN/TS 18599-10:2025-10.</summary>
        public const string ART_DIN_V_18599_10 = "DIN_V_18599_10";
        /// <summary>Art der Kategorie: SIA 2024.</summary>
        public const string ART_SIA_2024 = "SIA_2024";
        /// <summary>Art der Kategorie: VDI 2078.</summary>
        public const string ART_VDI_2078 = "VDI_2078";
        /// <summary>Art der Kategorie: eigene des Anwenders.</summary>
        public const string ART_EIGEN = "EIGEN";

        /// <summary>Die fünf Arten der Kategorie (NP-F4).</summary>
        public static readonly IReadOnlyList<string> ARTEN_KATALOG = new[]
        {
            ART_EPOS_MUSTER, ART_DIN_V_18599_10, ART_SIA_2024, ART_VDI_2078, ART_EIGEN
        };

        /// <summary>Die Arten der Normkategorien — ausgeliefert ohne Kennwert (NP-F21).</summary>
        public static readonly IReadOnlyList<string> ARTEN_NORM = new[] { ART_DIN_V_18599_10, ART_SIA_2024, ART_VDI_2078 };

        /// <summary>Art der Zuordnung: DIN-V-18599-10-Profilnummer.</summary>
        public const string ZUORDNUNG_DIN = "DIN_NUMMER";
        /// <summary>Art der Zuordnung: IFC-Nutzungsklasse.</summary>
        public const string ZUORDNUNG_IFC = "IFC_KLASSE";
        /// <summary>Art der Zuordnung: HottCAD-Raumtyp.</summary>
        public const string ZUORDNUNG_HOTTCAD = "HOTTCAD_RAUMTYP";

        /// <summary>Die drei Arten der Zuordnung (NP-F12).</summary>
        public static readonly IReadOnlyList<string> ARTEN_ZUORDNUNG = new[] { ZUORDNUNG_DIN, ZUORDNUNG_IFC, ZUORDNUNG_HOTTCAD };

        /// <summary>Einheit der Außenluft: Luftwechsel je Stunde.</summary>
        public const string EINHEIT_JE_STUNDE = "1/h";
        /// <summary>Einheit der Außenluft: m³ je Stunde und m² (NP-F10).</summary>
        public const string EINHEIT_JE_FLAECHE = "m3/hm2";

        /// <summary>Tagesart eines Stundenprofils: Werktag.</summary>
        public const string TAGESART_WERKTAG = "WERKTAG";
        /// <summary>Tagesart eines Stundenprofils: nutzungsfreier Tag.</summary>
        public const string TAGESART_FREI = "FREI";

        /// <summary>Die vier Zeilen des Zeilenbilds — die der Vorgabe ohne NENNWERT und SAISON.</summary>
        public static readonly IReadOnlyList<string> ZEILEN = new[]
        {
            DbWerte.KOND_ZEILE_TAG, DbWerte.KOND_ZEILE_NACHT, DbWerte.KOND_ZEILE_WOCHENENDE, DbWerte.KOND_ZEILE_FERIEN
        };

        /// <summary>Die Namensregel der Kategorie.</summary>
        public const string INDEX_KATALOG_NAME = "idx_Raumnutzungskatalog_Name";
        /// <summary>Die Namensregel des Profils je Kategorie.</summary>
        public const string INDEX_PROFIL_NAME = "idx_Raumnutzungsprofil_Name";
        /// <summary>Die Nummernregel des Profils je Kategorie (nur gesetzte Nummern).</summary>
        public const string INDEX_PROFIL_NUMMER = "idx_Raumnutzungsprofil_Nummer";
        /// <summary>Die Eindeutigkeit der Zuordnung je (Art, Schlüssel).</summary>
        public const string INDEX_ZUORDNUNG_SCHLUESSEL = "idx_Raumnutzungszuordnung_Schluessel";
        /// <summary>Der Index des Fremdschlüssels der Zuordnung.</summary>
        public const string INDEX_ZUORDNUNG_PROFIL = "idx_Raumnutzungszuordnung_Profil";

        // =================================================================
        //  Die DDL
        // =================================================================

        private static string Q(string s) => "\"" + s + "\"";
        private static string Zahl(double d) => d.ToString("R", CultureInfo.InvariantCulture);
        private static string Zahl(int i) => i.ToString(CultureInfo.InvariantCulture);

        private static string Liste(IEnumerable<string> werte) => string.Join(",", werte.Select(w => "'" + w + "'"));

        private static string Bezeichner(string s, int max)
            => Q(s) + " TEXT NOT NULL CHECK (length(" + Q(s) + ") BETWEEN 1 AND " + Zahl(max) + " AND " + Q(s) + " = trim(" + Q(s) + "))";

        private static string TextHoechstens(string s, int max)
            => Q(s) + " TEXT CHECK (" + Q(s) + " IS NULL OR length(" + Q(s) + ") <= " + Zahl(max) + ")";

        private static string ReadOnly() => "\"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1))";

        private static string Stunde(string s) => "INTEGER CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " BETWEEN 0 AND 24)";
        private static string Bit(string s) => "INTEGER CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " IN (0,1))";
        private static string Bereich(string s, double min, double max)
            => "REAL CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " BETWEEN " + Zahl(min) + " AND " + Zahl(max) + ")";
        private static string NichtNegativ(string s) => "REAL CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " >= 0)";
        private static string Positiv(string s) => "REAL CHECK (" + Q(s) + " IS NULL OR " + Q(s) + " > 0)";

        /// <summary>
        /// <b>Die 25 Kennwerte des Profils</b> (Konzept 4.1) in Schemareihenfolge: Spalte und Typ samt Prüfklausel; jeder
        /// nullbar (NP-F6). Grenzen der Sollwerte aus <see cref="Konditionierungsgroessen"/> (NP-F11).
        /// </summary>
        public static readonly IReadOnlyList<(string Spalte, string Typ)> SPALTEN_KENNWERTE = new (string, string)[]
        {
            ("Nutzung_Von", Stunde("Nutzung_Von")),
            ("Nutzung_Bis", Stunde("Nutzung_Bis")),
            ("Betrieb_Von", Stunde("Betrieb_Von")),
            ("Betrieb_Bis", Stunde("Betrieb_Bis")),
            ("Nutzungstage_Woche", "TEXT CHECK (\"Nutzungstage_Woche\" IS NULL OR (length(\"Nutzungstage_Woche\") = 7 AND " +
                                   "NOT \"Nutzungstage_Woche\" GLOB '*[^01]*'))"),
            ("Nutzungstage_Jahr", "INTEGER CHECK (\"Nutzungstage_Jahr\" IS NULL OR \"Nutzungstage_Jahr\" BETWEEN 1 AND 365)"),
            ("Feiertage_Wie_Sonntag", Bit("Feiertage_Wie_Sonntag")),
            ("Heiz_Soll", Bereich("Heiz_Soll", Konditionierungsgroessen.Min(Konditionierungsgroesse.Heizsoll),
                                  Konditionierungsgroessen.Max(Konditionierungsgroesse.Heizsoll))),
            ("Heiz_Soll_Ausserhalb", Bereich("Heiz_Soll_Ausserhalb", Konditionierungsgroessen.Min(Konditionierungsgroesse.Heizsoll),
                                             Konditionierungsgroessen.Max(Konditionierungsgroesse.Heizsoll))),
            ("Heiz_Aus_Ausserhalb", Bit("Heiz_Aus_Ausserhalb")),
            ("Kuehl_Soll", Bereich("Kuehl_Soll", Konditionierungsgroessen.Min(Konditionierungsgroesse.Kuehlsoll),
                                   Konditionierungsgroessen.Max(Konditionierungsgroesse.Kuehlsoll))),
            ("Kuehl_Soll_Ausserhalb", Bereich("Kuehl_Soll_Ausserhalb", Konditionierungsgroessen.Min(Konditionierungsgroesse.Kuehlsoll),
                                              Konditionierungsgroessen.Max(Konditionierungsgroesse.Kuehlsoll))),
            ("Kuehl_Aus_Ausserhalb", Bit("Kuehl_Aus_Ausserhalb")),
            ("Aussenluft", NichtNegativ("Aussenluft")),
            ("Aussenluft_Einheit", "TEXT CHECK (\"Aussenluft_Einheit\" IS NULL OR \"Aussenluft_Einheit\" IN ('" +
                                   EINHEIT_JE_STUNDE + "','" + EINHEIT_JE_FLAECHE + "'))"),
            ("Aussenluft_Ausserhalb", NichtNegativ("Aussenluft_Ausserhalb")),
            ("Personen_Flaeche", Positiv("Personen_Flaeche")),
            ("Personen_Waerme", NichtNegativ("Personen_Waerme")),
            ("Personen_Anteil", Bereich("Personen_Anteil", 0.0, 1.0)),
            ("Personen_Anteil_Ausserhalb", Bereich("Personen_Anteil_Ausserhalb", 0.0, 1.0)),
            ("Geraete_Leistung", NichtNegativ("Geraete_Leistung")),
            ("Geraete_Anteil", Bereich("Geraete_Anteil", 0.0, 1.0)),
            ("Geraete_Anteil_Ausserhalb", Bereich("Geraete_Anteil_Ausserhalb", 0.0, 1.0)),
            ("Beleuchtung_Leistung", NichtNegativ("Beleuchtung_Leistung")),
            ("Beleuchtung_Anteil", Bereich("Beleuchtung_Anteil", 0.0, 1.0)),
        };

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Raumnutzungskatalog</c> — STRICT.</summary>
        public static readonly string SQL_CREATE_KATALOG =
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_KATALOG) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    " + Bezeichner("Bezeichner", BEZEICHNER_MAX_ZEICHEN) + ",\n" +
            "    \"Art\" TEXT NOT NULL CHECK (\"Art\" IN (" + Liste(ARTEN_KATALOG) + ")),\n" +
            "    " + TextHoechstens("Beschreibung", BESCHREIBUNG_MAX_ZEICHEN) + ",\n" +
            "    " + TextHoechstens("Quellenhinweis", BESCHREIBUNG_MAX_ZEICHEN) + ",\n" +
            "    " + ReadOnly() + ",\n" +
            "    \"Reihenfolge\" INTEGER\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Raumnutzungsprofil</c> — STRICT, 6 + 25 Spalten.</summary>
        public static readonly string SQL_CREATE_PROFIL =
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_PROFIL) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Katalog\" INTEGER NOT NULL REFERENCES " + Q(TAB_KATALOG) + " (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Nummer\" TEXT CHECK (\"Nummer\" IS NULL OR (length(\"Nummer\") BETWEEN 1 AND " + Zahl(NUMMER_MAX_ZEICHEN) +
                " AND \"Nummer\" = trim(\"Nummer\"))),\n" +
            "    " + Bezeichner("Bezeichner", BEZEICHNER_MAX_ZEICHEN) + ",\n" +
            "    " + TextHoechstens("Beschreibung", BESCHREIBUNG_MAX_ZEICHEN) + ",\n" +
            "    " + ReadOnly() + ",\n" +
            string.Join(",\n", SPALTEN_KENNWERTE.Select(s => "    " + Q(s.Spalte) + " " + s.Typ)) + ",\n" +
            "    CHECK ((\"Aussenluft\" IS NULL AND \"Aussenluft_Ausserhalb\" IS NULL) OR \"Aussenluft_Einheit\" IS NOT NULL)\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Raumnutzungszeile</c> — dieselben Spalten und Prüfklauseln wie
        /// <c>Tab_Konditionierungsvorgabe</c>, ohne die Zeilen NENNWERT und SAISON; eindeutig je (Profil, Größe, Zeile).
        /// </summary>
        public static readonly string SQL_CREATE_ZEILE =
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_ZEILE) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Profil\" INTEGER NOT NULL REFERENCES " + Q(TAB_PROFIL) + " (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Groesse\" TEXT NOT NULL CHECK (\"Groesse\" IN (" + KonditionierungSchema.WERTE_GROESSE + ")),\n" +
            "    \"Zeile\" TEXT NOT NULL CHECK (\"Zeile\" IN (" + Liste(ZEILEN) + ")),\n" +
            "    \"Wert\" REAL,\n" +
            "    \"Aus\" INTEGER NOT NULL DEFAULT 0 CHECK (\"Aus\" IN (0,1)),\n" +
            "    \"Von\" INTEGER,\n" +
            "    \"Bis\" INTEGER,\n" +
            "    \"Bedingt_K\" REAL CHECK (\"Bedingt_K\" IS NULL OR (\"Bedingt_K\" BETWEEN 0 AND 5\n" +
            "        AND \"Groesse\" = 'LUEFTUNG' AND \"Zeile\" = 'NACHT')),\n" +
            "    CHECK (CASE \"Zeile\"\n" +
            "        WHEN 'NACHT' THEN (\"Von\" IS NULL OR \"Von\" BETWEEN 0 AND 23)\n" +
            "                                    AND (\"Bis\" IS NULL OR \"Bis\" BETWEEN 0 AND 23)\n" +
            "        ELSE \"Von\" IS NULL AND \"Bis\" IS NULL END),\n" +
            "    UNIQUE (\"ID_Profil\", \"Groesse\", \"Zeile\")\n" +
            ") STRICT";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Raumnutzungsstunden</c> — STRICT; eindeutig je (Profil, Größe, Tagesart).</summary>
        public static readonly string SQL_CREATE_STUNDEN =
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_STUNDEN) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Profil\" INTEGER NOT NULL REFERENCES " + Q(TAB_PROFIL) + " (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Groesse\" TEXT NOT NULL CHECK (\"Groesse\" IN (" + KonditionierungSchema.WERTE_GROESSE + ")),\n" +
            "    \"Tagesart\" TEXT NOT NULL CHECK (\"Tagesart\" IN ('" + TAGESART_WERKTAG + "','" + TAGESART_FREI + "')),\n" +
            "    \"Werte\" TEXT NOT NULL CHECK (length(\"Werte\") BETWEEN 1 AND " + Zahl(STUNDENWERTE_MAX_ZEICHEN) + "),\n" +
            "    UNIQUE (\"ID_Profil\", \"Groesse\", \"Tagesart\")\n" +
            ") STRICT";

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Raumnutzungszuordnung</c> — STRICT; <c>ID_Profil</c> NULL = „keine“, das
        /// Löschen eines Profils setzt die Zuordnung auf „keine“ (NP-F19).
        /// </summary>
        public static readonly string SQL_CREATE_ZUORDNUNG =
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_ZUORDNUNG) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"Art\" TEXT NOT NULL CHECK (\"Art\" IN (" + Liste(ARTEN_ZUORDNUNG) + ")),\n" +
            "    " + Bezeichner("Schluessel", BEZEICHNER_MAX_ZEICHEN) + ",\n" +
            "    \"ID_Profil\" INTEGER REFERENCES " + Q(TAB_PROFIL) + " (\"ID\") ON DELETE SET NULL,\n" +
            "    " + ReadOnly() + "\n" +
            ") STRICT";

        /// <summary>Die Indizes des Katalogs: Name, Anweisung.</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> INDIZES = new[]
        {
            new KeyValuePair<string, string>(INDEX_KATALOG_NAME,
                "CREATE UNIQUE INDEX IF NOT EXISTS " + Q(INDEX_KATALOG_NAME) + " ON " + Q(TAB_KATALOG) +
                " (\"Bezeichner\" COLLATE NOCASE)"),
            new KeyValuePair<string, string>(INDEX_PROFIL_NAME,
                "CREATE UNIQUE INDEX IF NOT EXISTS " + Q(INDEX_PROFIL_NAME) + " ON " + Q(TAB_PROFIL) +
                " (\"ID_Katalog\", \"Bezeichner\" COLLATE NOCASE)"),
            new KeyValuePair<string, string>(INDEX_PROFIL_NUMMER,
                "CREATE UNIQUE INDEX IF NOT EXISTS " + Q(INDEX_PROFIL_NUMMER) + " ON " + Q(TAB_PROFIL) +
                " (\"ID_Katalog\", \"Nummer\" COLLATE NOCASE) WHERE \"Nummer\" IS NOT NULL"),
            new KeyValuePair<string, string>(INDEX_ZUORDNUNG_SCHLUESSEL,
                "CREATE UNIQUE INDEX IF NOT EXISTS " + Q(INDEX_ZUORDNUNG_SCHLUESSEL) + " ON " + Q(TAB_ZUORDNUNG) +
                " (\"Art\", \"Schluessel\" COLLATE NOCASE)"),
            new KeyValuePair<string, string>(INDEX_ZUORDNUNG_PROFIL,
                "CREATE INDEX IF NOT EXISTS " + Q(INDEX_ZUORDNUNG_PROFIL) + " ON " + Q(TAB_ZUORDNUNG) + " (\"ID_Profil\")"),
        };

        /// <summary>Die fünf Tabellen: Name, Anweisung — Eltern vor Kindern.</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Tabellen() => new[]
        {
            new KeyValuePair<string, string>(TAB_KATALOG, SQL_CREATE_KATALOG),
            new KeyValuePair<string, string>(TAB_PROFIL, SQL_CREATE_PROFIL),
            new KeyValuePair<string, string>(TAB_ZEILE, SQL_CREATE_ZEILE),
            new KeyValuePair<string, string>(TAB_STUNDEN, SQL_CREATE_STUNDEN),
            new KeyValuePair<string, string>(TAB_ZUORDNUNG, SQL_CREATE_ZUORDNUNG),
        };

        /// <summary>Die Prüfklausel der Nutzung VOR dem Schritt — die vier Kennungen (Schritte 152 und 176).</summary>
        public static readonly string NUTZUNG_ALT =
            "\"" + SPALTE_NUTZUNG + "\" TEXT CHECK (\"" + SPALTE_NUTZUNG + "\" IS NULL OR \"" + SPALTE_NUTZUNG + "\" IN (" +
            KonditionierungVorlagenSchema.WERTE_NUTZUNG + "))";

        /// <summary>Die Prüfklausel der Nutzung NACH dem Schritt — freier Text, 1 … 120 Zeichen (NP-F15).</summary>
        public static readonly string NUTZUNG_NEU =
            "\"" + SPALTE_NUTZUNG + "\" TEXT CHECK (\"" + SPALTE_NUTZUNG + "\" IS NULL OR length(\"" + SPALTE_NUTZUNG +
            "\") BETWEEN 1 AND " + Zahl(NUTZUNG_MAX_ZEICHEN) + ")";

        /// <summary>Typ samt Prüfklausel der Zonenspalte.</summary>
        public static readonly string TYP_ZONE_NUTZUNGSPROFIL =
            "TEXT CHECK (\"" + SPALTE_ZONE_NUTZUNGSPROFIL + "\" IS NULL OR length(\"" + SPALTE_ZONE_NUTZUNGSPROFIL +
            "\") BETWEEN 1 AND " + Zahl(NUTZUNG_MAX_ZEICHEN) + ")";

        /// <summary>Die Anweisung, die die Zonenspalte anlegt.</summary>
        public static readonly string SQL_ZONE_SPALTE =
            "ALTER TABLE " + Q(TAB_ZONE) + " ADD COLUMN " + Q(SPALTE_ZONE_NUTZUNGSPROFIL) + " " + TYP_ZONE_NUTZUNGSPROFIL;

        /// <summary>Der Zusatz, unter dem eine Tabelle für die Dauer ihres Neubaus ausweicht.</summary>
        public const string HILFSZUSATZ = "_np_alt";

        // =================================================================
        //  Die Saat
        // =================================================================

        internal const string SQL_SAAT_KATALOG =
            "INSERT INTO \"" + TAB_KATALOG + "\" (\"Bezeichner\", \"Art\", \"Beschreibung\", \"Quellenhinweis\", \"ReadOnly\", " +
            "\"Reihenfolge\") SELECT ?, ?, ?, ?, 1, ? WHERE NOT EXISTS (SELECT 1 FROM \"" + TAB_KATALOG +
            "\" WHERE \"Bezeichner\" = ? COLLATE NOCASE)";

        /// <summary>Die Saat eines Profils: Kopf und Kennwerte; zehn plus 25 Parameter.</summary>
        internal static readonly string SQL_SAAT_PROFIL =
            "INSERT INTO \"" + TAB_PROFIL + "\" (\"ID_Katalog\", \"Nummer\", \"Bezeichner\", \"Beschreibung\", \"ReadOnly\", " +
            string.Join(", ", SPALTEN_KENNWERTE.Select(s => Q(s.Spalte))) + ") SELECT k.\"ID\", ?, ?, ?, 1, " +
            string.Join(", ", SPALTEN_KENNWERTE.Select(_ => "?")) + " FROM \"" + TAB_KATALOG +
            "\" k WHERE k.\"Bezeichner\" = ? COLLATE NOCASE AND NOT EXISTS (SELECT 1 FROM \"" + TAB_PROFIL +
            "\" p WHERE p.\"ID_Katalog\" = k.\"ID\" AND p.\"Bezeichner\" = ? COLLATE NOCASE)";

        private const string SQL_PROFIL_ID =
            "(SELECT p.\"ID\" FROM \"" + TAB_PROFIL + "\" p JOIN \"" + TAB_KATALOG + "\" k ON k.\"ID\" = p.\"ID_Katalog\" " +
            "WHERE k.\"Bezeichner\" = ? COLLATE NOCASE AND p.\"Bezeichner\" = ? COLLATE NOCASE)";

        // Ohne INSERT OR IGNORE: SQLite zählt den AUTOINCREMENT-Stand auch bei ignoriertem Einfügen hoch; die Saaten
        // der Zeilen, der Zuordnung und der Pufferzuordnung fügen deshalb nur über NOT EXISTS auf ihren UNIQUE-Schlüssel
        // ein — Zeile: (Profil, Größe, Zeile); Zuordnung: (Art, Schlüssel ohne Unterschied der Schreibung);
        // Pufferzuordnung in Z_Nutzungsprofil: (Quelle, Schlüssel).

        /// <summary>Die Saat einer Zeile; vierzehn Parameter: Kategorie, Profil, sechs Werte, Kategorie, Profil
        /// (Profil besteht), Kategorie, Profil, Größe, Zeile (Zeile fehlt).</summary>
        internal const string SQL_SAAT_ZEILE =
            "INSERT INTO \"" + TAB_ZEILE + "\" (\"ID_Profil\", \"Groesse\", \"Zeile\", \"Wert\", \"Aus\", \"Von\", \"Bis\") " +
            "SELECT " + SQL_PROFIL_ID + ", ?, ?, ?, ?, ?, ? WHERE " + SQL_PROFIL_ID + " IS NOT NULL " +
            "AND NOT EXISTS (SELECT 1 FROM \"" + TAB_ZEILE + "\" WHERE \"ID_Profil\" = " + SQL_PROFIL_ID +
            " AND \"Groesse\" = ? AND \"Zeile\" = ?)";

        /// <summary>Die Saat einer Zuordnung; sechs Parameter: Art, Schlüssel, Kategorie, Profil, Art, Schlüssel.</summary>
        internal const string SQL_SAAT_ZUORDNUNG =
            "INSERT INTO \"" + TAB_ZUORDNUNG + "\" (\"Art\", \"Schluessel\", \"ID_Profil\", \"ReadOnly\") " +
            "SELECT ?, ?, " + SQL_PROFIL_ID + ", 1 WHERE NOT EXISTS (SELECT 1 FROM \"" + TAB_ZUORDNUNG +
            "\" WHERE \"Art\" = ? AND \"Schluessel\" = ? COLLATE NOCASE)";

        internal const string SQL_SAAT_PUFFER =
            "INSERT INTO \"" + ProzessNutzungSchema.TAB_ZUORDNUNG + "\" (\"ID_Nutzungsprofil\", \"Quelle\", \"Schluessel\") " +
            "SELECT \"ID\", ?, ? FROM \"" + ProzessNutzungSchema.TAB_PROFIL + "\" WHERE \"Kennung\" = ? " +
            "AND NOT EXISTS (SELECT 1 FROM \"" + ProzessNutzungSchema.TAB_ZUORDNUNG + "\" WHERE \"Quelle\" = ? AND \"Schluessel\" = ?)";

        /// <summary>Die Zahl der ausgelieferten Kategorien, die stehen.</summary>
        internal const string SQL_ZAHL_KATALOG_GESAAT =
            "SELECT COUNT(*) FROM \"" + TAB_KATALOG + "\" WHERE \"ReadOnly\" = 1 AND \"Bezeichner\" = ? COLLATE NOCASE";

        /// <summary>Steht das Profil (Kategorie, Name)?</summary>
        internal const string SQL_ZAHL_PROFIL_GESAAT =
            "SELECT COUNT(*) FROM \"" + TAB_PROFIL + "\" p JOIN \"" + TAB_KATALOG + "\" k ON k.\"ID\" = p.\"ID_Katalog\" " +
            "WHERE k.\"Bezeichner\" = ? COLLATE NOCASE AND p.\"Bezeichner\" = ? COLLATE NOCASE";

        /// <summary>Steht die Zuordnung (Art, Schlüssel)?</summary>
        internal const string SQL_ZAHL_ZUORDNUNG_GESAAT =
            "SELECT COUNT(*) FROM \"" + TAB_ZUORDNUNG + "\" WHERE \"Art\" = ? AND \"Schluessel\" = ? COLLATE NOCASE";

        /// <summary>Steht die Pufferzuordnung (Quelle KONDITIONIERUNG, Schlüssel)?</summary>
        internal const string SQL_ZAHL_PUFFER_GESAAT =
            "SELECT COUNT(*) FROM \"" + ProzessNutzungSchema.TAB_ZUORDNUNG + "\" WHERE \"Quelle\" = ? AND \"Schluessel\" = ?";

        // =================================================================
        //  Stand
        // =================================================================

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[]
        {
            KonditionierungVorlagenSchema.TAB_VORLAGE, KonditionierungSchema.TAB_KALENDER, KonditionierungSchema.TAB_PERIODE,
            KonditionierungSchema.TAB_VORGABE, TAB_ZONE, ProzessNutzungSchema.TAB_ZUORDNUNG, ProzessNutzungSchema.TAB_PROFIL,
        };

        /// <summary>Stehen die fünf Tabellen, ihre Indizes und die Zonenspalte, und ist die Nutzung an beiden Tabellen freier Text?</summary>
        public static bool SchemaVollstaendig()
        {
            if (!TABELLEN.All(DataRepository.TabelleVorhanden)) return false;
            if (!DataRepository.SpalteVorhanden(TAB_ZONE, SPALTE_ZONE_NUTZUNGSPROFIL)) return false;
            foreach (KeyValuePair<string, string> i in INDIZES)
                if (Zaehle(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?",
                                                        new DbParam("@n", i.Key))) == 0) return false;
            return TABELLEN_NUTZUNG.All(NutzungFrei);
        }

        /// <summary>Trägt die Tabelle die freie Nutzung (Prüfklausel <see cref="NUTZUNG_NEU"/>)?</summary>
        public static bool NutzungFrei(string tabelle)
        {
            string sql = Text(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                           new DbParam("@t", tabelle)));
            return sql != null && sql.Contains(NUTZUNG_NEU, StringComparison.Ordinal);
        }

        /// <summary>Trägt <c>Tab_Zone</c> die Spalte <c>Nutzungsprofil</c>? (Wächter der Zonenwege, etwa für einen älteren Stand auf iOS.)</summary>
        public static bool ZonenspalteVorhanden() => DataRepository.SpalteVorhanden(TAB_ZONE, SPALTE_ZONE_NUTZUNGSPROFIL);

        /// <summary>Wie viele Zeilen der Saat noch fehlen (Kategorien, Profile, Zeilenbild, Zuordnung, Pufferzuordnung).</summary>
        public static int OffeneSaat()
        {
            if (!TABELLEN.All(DataRepository.TabelleVorhanden)) return -1;
            int offen = 0;
            foreach (RaumnutzungSaatkategorie k in RaumnutzungSaat.Kategorien)
                if (Zaehle(DataRepository.ExecuteScalar(SQL_ZAHL_KATALOG_GESAAT, new DbParam("@b", k.Bezeichner))) == 0) offen++;
            foreach (RaumnutzungSaatprofil p in RaumnutzungSaat.Profile)
                if (Zaehle(DataRepository.ExecuteScalar(SQL_ZAHL_PROFIL_GESAAT,
                                                        new DbParam("@k", p.Kategorie), new DbParam("@p", p.Bezeichner))) == 0) offen++;
            long zeilen = Zaehle(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + TAB_ZEILE + "\" z JOIN \"" + TAB_PROFIL + "\" p ON p.\"ID\" = z.\"ID_Profil\" " +
                "WHERE p.\"ReadOnly\" = 1"));
            int sollZeilen = RaumnutzungSaat.Profile.Sum(p => p.Zeilen.Count);
            if (zeilen < sollZeilen) offen += (int)(sollZeilen - zeilen);
            foreach (RaumnutzungSaatzuordnung z in RaumnutzungSaat.Zuordnungen)
                if (Zaehle(DataRepository.ExecuteScalar(SQL_ZAHL_ZUORDNUNG_GESAAT,
                                                        new DbParam("@a", z.Art), new DbParam("@s", z.Schluessel))) == 0) offen++;
            foreach ((string Schluessel, PufferNutzungsprofil Profil) z in RaumnutzungSaat.Pufferzuordnungen)
                if (Zaehle(DataRepository.ExecuteScalar(SQL_ZAHL_PUFFER_GESAAT,
                                                        new DbParam("@q", NutzungsprofilQuelle.KONDITIONIERUNG),
                                                        new DbParam("@s", z.Schluessel))) == 0) offen++;
            return offen;
        }

        /// <summary>Schema vollständig und Saat ohne Lücke?</summary>
        public static bool Vollstaendig() => SchemaVollstaendig() && OffeneSaat() == 0;

        // =================================================================
        //  Der Zieltext des Neubaus
        // =================================================================

        /// <summary>
        /// Der Zieltext einer Tabelle aus <see cref="TABELLEN_NUTZUNG"/>: ihr GELTENDER <c>sqlite_master.sql</c> mit der
        /// EINEN Stelle <see cref="NUTZUNG_ALT"/> durch <see cref="NUTZUNG_NEU"/> ersetzt — sonst Zeichen für Zeichen der
        /// Bestand. Keine STRICT-Tabelle, ein anderer Kopf, die Stelle nicht genau einmal: benannter Abbruch.
        /// </summary>
        public static string Zieltext(string tabelle, string bestand)
        {
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException("Zu " + tabelle + " gibt es keinen CREATE-Text in sqlite_master.");
            string text = bestand.TrimEnd();
            if (!text.EndsWith(KonditionierungVorlagenSchema.ENDE, StringComparison.Ordinal))
                throw new InvalidOperationException("Die Tabelle " + tabelle + " endet nicht auf \"" +
                                                    KonditionierungVorlagenSchema.ENDE + "\" - Schemaschritt " + Nr +
                                                    " baut sie deshalb NICHT um.");
            string kopf = "CREATE TABLE \"" + tabelle + "\" (";
            if (!text.StartsWith(kopf, StringComparison.Ordinal))
                throw new InvalidOperationException("Der CREATE-Text von " + tabelle + " hat eine andere Bauform als erwartet " +
                                                    "(erwartet wurde der Beginn " + kopf + ").");
            int erste = text.IndexOf(NUTZUNG_ALT, StringComparison.Ordinal);
            int zweite = erste < 0 ? -1 : text.IndexOf(NUTZUNG_ALT, erste + NUTZUNG_ALT.Length, StringComparison.Ordinal);
            if (erste < 0 || zweite >= 0)
                throw new InvalidOperationException("Im CREATE-Text von " + tabelle + " steht die Pruefklausel der Nutzung " +
                                                    (erste < 0 ? "nicht" : "mehr als einmal") + " in der Form der Schritte 152/176 - " +
                                                    "Schemaschritt " + Nr + " baut die Tabelle NICHT um.");
            return text.Substring(0, erste) + NUTZUNG_NEU + text.Substring(erste + NUTZUNG_ALT.Length);
        }

        // =================================================================
        //  Ausführung
        // =================================================================

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang ohne Fremdschlüssel aus — Tabellen, Indizes, Neubau der Nutzung,
        /// Zonenspalte, Saat — für Migration der Schale, <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>.
        /// <b>Wiederholbar.</b> Fehlt eine Voraussetzung, wirft er benannt; ein nicht leerer <c>foreign_key_check</c> oder
        /// ein umgeschriebener Verweis nimmt alles zurück.
        /// </summary>
        /// <param name="bericht">Nimmt Zeilen auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der Änderungen am Schema (Tabellen, Indizes, Neubauten, Spalte); 0, wenn alles stand.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            foreach (string t in TABELLEN_NUTZUNG)
                if (!DataRepository.SpalteVorhanden(t, SPALTE_NUTZUNG))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": " + t + "." + SPALTE_NUTZUNG +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");

            if (Vollstaendig())
            {
                bericht?.Add("steht bereits - fuenf Tabellen des Katalogs, freie Nutzung an Kalender und Vorlage, " +
                             "Tab_Zone.Nutzungsprofil und die Saat; nichts zu tun");
                return 0;
            }

            int aenderungen = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    // ---- 1. Die fuenf Tabellen und ihre Indizes.
                    foreach (KeyValuePair<string, string> t in Tabellen())
                    {
                        bool neu = !ObjektSteht(v, "table", t.Key);
                        v.Ausfuehren(t.Value);
                        if (neu) { aenderungen++; zeilen.Add(t.Key + " angelegt"); }
                    }
                    foreach (KeyValuePair<string, string> i in INDIZES)
                    {
                        bool neu = !ObjektSteht(v, "index", i.Key);
                        v.Ausfuehren(i.Value);
                        if (neu) { aenderungen++; zeilen.Add("Index " + i.Key + " angelegt"); }
                    }

                    // ---- 2. Die freie Nutzung an Vorlage und Kalender (Tabellenneubau).
                    foreach (string t in TABELLEN_NUTZUNG)
                    {
                        string sql = Text(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                   new DbParam("@t", t)));
                        if (sql != null && sql.Contains(NUTZUNG_NEU, StringComparison.Ordinal))
                        {
                            zeilen.Add(t + "." + SPALTE_NUTZUNG + " ist bereits freier Text");
                            continue;
                        }
                        zeilen.Add(Neubau(v, t, sql));
                        aenderungen++;
                    }

                    // ---- 3. Die Zonenspalte.
                    if (!SpalteImVorgang(v, TAB_ZONE, SPALTE_ZONE_NUTZUNGSPROFIL))
                    {
                        v.Ausfuehren(SQL_ZONE_SPALTE);
                        aenderungen++;
                        zeilen.Add(TAB_ZONE + "." + SPALTE_ZONE_NUTZUNGSPROFIL + " angelegt (leer)");
                    }

                    // ---- 4. Die Saat - vorher eine Kategorie DIN der Saat 2018 auf die Ausgabe 2025 und die Zuordnung
                    //      DIN_NUMMER auf die Zaehlung 2025 (Schritt 190), sonst legte die Saat neben ihnen zweite an.
                    int umbau = 0;
                    zeilen.AddRange(RaumnutzungDinTsSchema.AlteFassungUmbauen(v, ref umbau));
                    zeilen.AddRange(RaumnutzungDinTsSchema.ZuordnungUmstellen(v, ref umbau));
                    zeilen.AddRange(Saat(v));

                    // ---- Die Zeugen, noch INNERHALB der Transaktion.
                    Nachpruefen(v);
                    v.Commit();
                }
                finally
                {
                    // Der Legacy-Modus darf die Verbindung nicht ueberleben (Muster der Schritte 96 und 152).
                    try { v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* nach Commit abgeschlossen - dann stand er schon aus */ }
                }
            }
            GebaeudeZonenanschluss.ProbeVerwerfen();

            if (bericht != null)
                foreach (string z in zeilen) bericht.Add(z);
            bericht?.Add("foreign_key_check leer, Verweise namentlich; KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            return aenderungen;
        }

        /// <summary>Die Saat im laufenden Vorgang; liefert die Berichtszeilen.</summary>
        private static List<string> Saat(DbVorgang v)
        {
            int kategorien = 0, profile = 0, zeilen = 0, zuordnungen = 0, puffer = 0;
            foreach (RaumnutzungSaatkategorie k in RaumnutzungSaat.Kategorien)
                kategorien += v.Ausfuehren(SQL_SAAT_KATALOG,
                    new DbParam("@b", k.Bezeichner), new DbParam("@a", k.Art), new DbParam("@d", k.Beschreibung),
                    new DbParam("@q", k.Quellenhinweis), new DbParam("@r", k.Reihenfolge), new DbParam("@b2", k.Bezeichner));

            foreach (RaumnutzungSaatprofil p in RaumnutzungSaat.Profile)
            {
                var par = new List<DbParam>
                {
                    new DbParam("@n", p.Nummer), new DbParam("@b", p.Bezeichner), new DbParam("@d", p.Beschreibung),
                };
                int i = 0;
                foreach (object w in p.Kennwerte()) par.Add(new DbParam("@w" + (i++).ToString(CultureInfo.InvariantCulture), w));
                par.Add(new DbParam("@k", p.Kategorie));
                par.Add(new DbParam("@b2", p.Bezeichner));
                profile += v.Ausfuehren(SQL_SAAT_PROFIL, par.ToArray());

                foreach (RaumnutzungSaatzeile z in p.Zeilen)
                    zeilen += v.Ausfuehren(SQL_SAAT_ZEILE,
                        new DbParam("@k", p.Kategorie), new DbParam("@p", p.Bezeichner),
                        new DbParam("@g", z.Groesse), new DbParam("@z", z.Zeile), new DbParam("@w", z.Wert),
                        new DbParam("@a", z.Aus ? 1 : 0), new DbParam("@v", z.Von), new DbParam("@bis", z.Bis),
                        new DbParam("@k2", p.Kategorie), new DbParam("@p2", p.Bezeichner),
                        new DbParam("@k3", p.Kategorie), new DbParam("@p3", p.Bezeichner),
                        new DbParam("@g2", z.Groesse), new DbParam("@z2", z.Zeile));
            }

            foreach (RaumnutzungSaatzuordnung z in RaumnutzungSaat.Zuordnungen)
                zuordnungen += v.Ausfuehren(SQL_SAAT_ZUORDNUNG,
                    new DbParam("@a", z.Art), new DbParam("@s", z.Schluessel),
                    new DbParam("@k", z.Kategorie), new DbParam("@p", z.Profil),
                    new DbParam("@a2", z.Art), new DbParam("@s2", z.Schluessel));

            foreach ((string Schluessel, PufferNutzungsprofil Profil) z in RaumnutzungSaat.Pufferzuordnungen)
                puffer += v.Ausfuehren(SQL_SAAT_PUFFER,
                    new DbParam("@q", NutzungsprofilQuelle.KONDITIONIERUNG), new DbParam("@s", z.Schluessel),
                    new DbParam("@k", z.Profil.ToString()),
                    new DbParam("@q2", NutzungsprofilQuelle.KONDITIONIERUNG), new DbParam("@s2", z.Schluessel));

            return new List<string>
            {
                "Saat: " + T(kategorien) + " Kategorie(n), " + T(profile) + " Profil(e), " + T(zeilen) + " Zeile(n) des Zeilenbilds, " +
                T(zuordnungen) + " Zuordnung(en), " + T(puffer) + " Pufferzuordnung(en) der Musternamen angelegt",
            };
        }

        /// <summary>
        /// Das Tabellenneubau-Rezept der Schritte 96 und 152: alte Tabelle unter <c>legacy_alter_table = ON</c> auf den
        /// Hilfsnamen, neue unter dem echten Namen, Zeilen namentlich mit IDs, AUTOINCREMENT-Stand, alte fällt
        /// (Fremdschlüssel aus — keine Kaskade), Indizes und Trigger des Bestands Wort für Wort.
        /// </summary>
        private static string Neubau(DbVorgang v, string tabelle, string bestand)
        {
            string alt = tabelle + HILFSZUSATZ;
            string ziel = Zieltext(tabelle, bestand);

            var namen = new List<string>();
            DataTable info = v.Lese("SELECT name FROM pragma_table_info(?)", new DbParam("@t", tabelle));
            foreach (DataRow zeile in info.Rows)
                namen.Add("\"" + Convert.ToString(zeile["name"], CultureInfo.InvariantCulture) + "\"");
            string spaltenliste = string.Join(", ", namen);

            var objekte = new List<(bool Index, string Sql)>();
            DataTable idx = v.Lese(
                "SELECT type, sql FROM sqlite_master WHERE type IN ('index', 'trigger') AND tbl_name = ? AND sql IS NOT NULL " +
                "ORDER BY type, name", new DbParam("@t", tabelle));
            foreach (DataRow zeile in idx.Rows)
                objekte.Add((string.Equals(Convert.ToString(zeile["type"], CultureInfo.InvariantCulture), "index", StringComparison.Ordinal),
                             Convert.ToString(zeile["sql"], CultureInfo.InvariantCulture)));

            object standWert = v.Skalar("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@t", tabelle));
            long zeilenVorher = Zaehle(v.Skalar("SELECT COUNT(*) FROM \"" + tabelle + "\""));
            string pruefVorher = Text(v.Skalar("SELECT group_concat(quote(\"ID\") || ':' || quote(\"" + SPALTE_NUTZUNG +
                                               "\"), ',') FROM (SELECT \"ID\", \"" + SPALTE_NUTZUNG + "\" FROM \"" + tabelle +
                                               "\" ORDER BY \"ID\")"));

            v.Ausfuehren("DROP TABLE IF EXISTS \"" + alt + "\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = ON");
            v.Ausfuehren("ALTER TABLE \"" + tabelle + "\" RENAME TO \"" + alt + "\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = OFF");

            v.Ausfuehren(ziel);
            v.Ausfuehren("INSERT INTO \"" + tabelle + "\" (" + spaltenliste + ") SELECT " + spaltenliste + " FROM \"" + alt + "\"");
            v.Ausfuehren("DROP TABLE \"" + alt + "\"");

            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("@t", alt));
            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("@t", tabelle));
            if (standWert != null && standWert != DBNull.Value)
                v.Ausfuehren("INSERT INTO sqlite_sequence (name, seq) VALUES (?, ?)",
                             new DbParam("@t", tabelle), new DbParam("@s", Convert.ToInt64(standWert, CultureInfo.InvariantCulture)));

            // Indizes mit IF NOT EXISTS (Muster der Schritte 96 und 152), Trigger Wort fuer Wort - sie fielen mit der Hilfstabelle.
            foreach ((bool Index, string Sql) o in objekte)
                v.Ausfuehren(o.Index ? ProjektFremdschluessel.MitIfNotExists(o.Sql) : o.Sql);

            long zeilenNachher = Zaehle(v.Skalar("SELECT COUNT(*) FROM \"" + tabelle + "\""));
            string pruefNachher = Text(v.Skalar("SELECT group_concat(quote(\"ID\") || ':' || quote(\"" + SPALTE_NUTZUNG +
                                                "\"), ',') FROM (SELECT \"ID\", \"" + SPALTE_NUTZUNG + "\" FROM \"" + tabelle +
                                                "\" ORDER BY \"ID\")"));
            if (zeilenNachher != zeilenVorher || !string.Equals(pruefVorher, pruefNachher, StringComparison.Ordinal))
                throw new InvalidOperationException("Der Neubau von " + tabelle + " traegt " + T(zeilenNachher) + " statt " +
                                                    T(zeilenVorher) + " Zeilen oder andere Nutzungen - Schemaschritt " + Nr +
                                                    " nimmt ihn zurueck.");

            return tabelle + "." + SPALTE_NUTZUNG + ": Pruefklausel per Neubau auf freien Text (1.." + Zahl(NUTZUNG_MAX_ZEICHEN) +
                   ") gesetzt; Zeilen " + T(zeilenVorher) + " -> " + T(zeilenNachher) + ", IDs, Nutzungen und Zaehlerstand erhalten, " +
                   T(objekte.Count) + " Index(e)/Trigger des Bestands wieder angelegt";
        }

        /// <summary>
        /// Die Zeugen vor dem Commit: <c>foreign_key_check</c> leer für die umgebauten und die neuen Tabellen samt Kindern,
        /// und jedes Kind verweist NAMENTLICH auf seine Elterntabelle — nicht auf den Hilfsnamen.
        /// </summary>
        private static void Nachpruefen(DbVorgang v)
        {
            var pruefen = new List<string>(TABELLEN)
            {
                KonditionierungSchema.TAB_KALENDER, KonditionierungSchema.TAB_PERIODE, KonditionierungSchema.TAB_VORGABE,
                KonditionierungVorlagenSchema.TAB_VORLAGE, ProzessNutzungSchema.TAB_ZUORDNUNG,
            };
            foreach (string t in pruefen)
            {
                long verletzt = Zaehle(v.Skalar("SELECT COUNT(*) FROM pragma_foreign_key_check(?)", new DbParam("@t", t)));
                if (verletzt > 0)
                    throw new InvalidOperationException("Nach dem Umbau meldet foreign_key_check fuer " + t + " " + T(verletzt) +
                                                        " verletzte Zeile(n) - Schemaschritt " + Nr + " nimmt alles zurueck.");
            }
            foreach ((string Kind, string Eltern, string Spalte) f in new[]
                     {
                         (KonditionierungSchema.TAB_PERIODE, KonditionierungSchema.TAB_KALENDER, KonditionierungSchema.SPALTE_ID_KALENDER),
                         (KonditionierungSchema.TAB_KALENDER, KonditionierungVorlagenSchema.TAB_VORLAGE, KonditionierungVorlagenSchema.SPALTE_ID_VORLAGE),
                         (KonditionierungSchema.TAB_VORGABE, KonditionierungVorlagenSchema.TAB_VORLAGE, KonditionierungVorlagenSchema.SPALTE_ID_VORLAGE),
                     })
            {
                long namentlich = Zaehle(v.Skalar(
                    "SELECT COUNT(*) FROM pragma_foreign_key_list(?) WHERE \"table\" = ? AND \"from\" = ?",
                    new DbParam("@t", f.Kind), new DbParam("@z", f.Eltern), new DbParam("@s", f.Spalte)));
                string text = Text(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                            new DbParam("@t", f.Kind))) ?? "";
                if (namentlich != 1 || text.Contains(HILFSZUSATZ + "\"", StringComparison.Ordinal))
                    throw new InvalidOperationException(f.Kind + " verweist nach dem Neubau nicht mehr namentlich auf " + f.Eltern +
                                                        " (legacy_alter_table) - Schemaschritt " + Nr + " nimmt alles zurueck.");
            }
        }

        // =================================================================
        //  Kleinkram
        // =================================================================

        private static bool ObjektSteht(DbVorgang v, string art, string name)
            => Zaehle(v.Skalar("SELECT COUNT(*) FROM sqlite_master WHERE type = ? AND name = ?",
                               new DbParam("@a", art), new DbParam("@n", name))) > 0;

        private static bool SpalteImVorgang(DbVorgang v, string tabelle, string spalte)
            => Zaehle(v.Skalar("SELECT COUNT(*) FROM pragma_table_info(?) WHERE name = ?",
                               new DbParam("@t", tabelle), new DbParam("@s", spalte))) > 0;

        private static long Zaehle(object wert)
            => wert == null || wert == DBNull.Value ? 0 : Convert.ToInt64(wert, CultureInfo.InvariantCulture);

        private static string Text(object wert)
            => wert == null || wert == DBNull.Value ? null : Convert.ToString(wert, CultureInfo.InvariantCulture);

        private static string T(long zahl) => zahl.ToString(CultureInfo.InvariantCulture);

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
