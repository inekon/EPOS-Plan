using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER GEBAEUDESPALTEN-SCHRITT M3 - Schemaschritt 101 (Stufe G1 der Gebaeudesimulation,
    // Umsetzungskonzept Gebaeudesimulation 1.6 und 1.7; Entscheide E19, E27/U5, F-S1, F-S2).
    //
    // WAS ER TUT, IN DIESER REIHENFOLGE (Konzept N1.24):
    //   0. die Sicht Abfrage_Projektgebaeude verwerfen - sie nennt Wohnflaeche namentlich,
    //      und SQLite kennt kein ALTER VIEW;
    //   1. in Tab_Gebaeude und Tab_Gebaeude_STAMM die Spalte Wohnflaeche in Nutzflaeche
    //      umbenennen (E19: Umbenennung mit Wertuebernahme, keine zweite Spalte);
    //   2. fuenfzehn neue Spalten je Tabelle anlegen - die zwoelf der Stufe G1 und die drei
    //      der Stufe G2 (U5: EIN Schritt, EIN Sichtneubau);
    //   3. die Sicht neu bauen, aus SQL_VIEW_NEU - der einzigen Quelle ihrer Definition.
    //
    // ERGEBNISNEUTRAL. Alle neuen Spalten bleiben NULL (die zwei Schalter 0), und kein
    // Rechenweg liest sie; die Umbenennung traegt die Werte 1:1 hinueber. Der Referenzlauf
    // bleibt byte-gleich - eine Abweichung waere ein Fehler der Migration.
    //
    // DIE NEUEN SPALTEN STEHEN IN DER SICHT HINTER Tab_Gebaeude.ID. Die 58 Bestandsspalten
    // behalten ihre Stellen 0..57; ProjektGebaeudeCtrl liest ab diesem Schritt zwar nach
    // Namen, aber jeder fremde Indexleser ueberlebt den Schritt dadurch unveraendert.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // ProjektEnergietraegerEindeutig (76) oder FremdschluesselVorgabe (100): drei Leser -
    // SchemaMigration (Schale), Werkzeuge/Testdatenbankschema und EPOS.Kern.Tests.
    // Stuenden die Definitionen dort, muessten die anderen beiden sie abschreiben.
    //
    // DIE ACHT NICHT-ASCII-BEZEICHNER (k_Wert_Außenwand, Flaeche_Außenwand,
    // WBVK_Anschluß_*, Abmessung_Anschluß_*) stehen BUCHSTABENGETREU, wie im Schema
    // (BETRIEB_SQLITE.md 6.1): SQLite vergleicht Bezeichner nur bei ASCII-Buchstaben ohne
    // Ruecksicht auf Gross- und Kleinschreibung.
    //
    // DER ZWEITE DURCHGANG: KU-S1 (Schemaschritt 108, Kuehlkonzept 7.1). Die Klasse wird
    // mehrfach angefasst (Softwarearchitektur Gebaeudesimulation 2.4): Jeder Durchgang, der
    // Gebaeudespalten anlegt, ist ein eigener Sichtneubau. KU-S1 legt die vier
    // Kuehleingaben an beide Gebaeudetabellen und baut die Sicht mit ihnen HINTER den
    // fuenfzehn Spalten von M3 neu. Die Bauvorschrift der Sicht steht EINMAL (SichtSql);
    // jeder Durchgang nennt nur seine Zusatzspalten. Tab_Zone gibt es noch nicht - den
    // Block „Spalten aus KU-S1" legt der Zonenschritt S-C mit an (Mehrzonenkonzept 4.2).
    //
    // DER DRITTE DURCHGANG: AK-S1 (Schemaschritt 122, Anlagenkopplung 8.1). Dreizehn
    // Spalten der Waermeuebergabe je Gebaeudetabelle, die Sicht mit ihnen HINTER den
    // Kuehlspalten neu. Die Projektspalte Tab_Einstellungen.Anlagenkopplung desselben
    // Schritts steht bei AnlagenkopplungSchema, das beide Teile zu EINEM Schritt fuegt.
    // Die drei Uebergabespalten der Zone legt ebenfalls S-C mit an.
    //
    // DER VIERTE DURCHGANG: KAK-S1 (Entscheid E37, Anlagenkopplung 8.1; Nummer bei
    // KuehluebergabeSchema.SCHRITT). Acht Spalten der Kuehluebergabe je Gebaeudetabelle -
    // der Schalter Kuehluebergabe_Aktiv zuerst (A1) -, die Sicht mit ihnen HINTER den
    // Uebergabespalten neu (98 Spalten). Die drei Zonenspalten der Kuehluebergabe bringt ein
    // eigener Schritt nach S-C.
    //
    // DER FUENFTE DURCHGANG: das Baujahr (Stufe G4a, Umsetzungskonzept 3.4; Nummer bei
    // BaujahrSchema.SCHRITT). Eine Spalte Baujahr (INTEGER, CHECK 1500 bis 2100) je
    // Gebaeudetabelle, die Sicht mit ihr HINTER den Spalten der Kuehluebergabe neu (99
    // Spalten).
    //
    // DER SECHSTE DURCHGANG: die Nachtzeit (Entscheid E43, Konzept-Nachtrag N1.48; Nummer bei
    // NachtzeitSchema.SCHRITT). Zwei Spalten Nachtabsenkung_Beginn und Nachtabsenkung_Ende
    // (INTEGER, Stunde des Tages 0 bis 23, NULL = Vorgabe) je Gebaeudetabelle, die Sicht mit
    // ihnen HINTER dem Baujahr neu (101 Spalten).
    //
    // DER SIEBTE DURCHGANG: der Energiestandard (Entscheid E47, Konzept Baualtersklassen 3.2;
    // Nummer bei BaualtersklassenSchema.SCHRITT). Eine Spalte Energiestandard (TEXT, CHECK auf die
    // elf Codes, NULL = keiner) je Gebaeudetabelle, die Sicht mit ihr HINTER der Nachtzeit neu (102
    // Spalten); dazu die einmalige Umschluesselung der Baualtersklassen (BaualtersklassenSchema). Er
    // laeuft in Migration, Werkzeug und Testkopie ZULETZT, damit kein aelterer Durchgang die Spalte
    // wieder aus der Sicht schneidet.
    // ====================================================================================

    /// <summary>
    /// Der Gebaeudespalten-Schritt M3 (Schemaschritt 101): Umbenennung
    /// <c>Wohnflaeche</c> → <c>Nutzflaeche</c>, fuenfzehn neue Spalten je Gebaeudetabelle
    /// und der Neubau der Sicht <c>Abfrage_Projektgebaeude</c> - EINE Quelle fuer
    /// Migration, Werkzeug und Nachweis. Dazu der zweite Durchgang KU-S1 (Schemaschritt
    /// 108, Kuehlkonzept 7.1): vier Kuehleingaben je Gebaeudetabelle und der zweite
    /// Sichtneubau; und der dritte Durchgang AK-S1 (Schemaschritt 122, Anlagenkopplung
    /// 8.1): dreizehn Spalten der Waermeuebergabe je Gebaeudetabelle und der dritte
    /// Sichtneubau; und der vierte Durchgang KAK-S1 (<see cref="KuehluebergabeSchema.SCHRITT"/>,
    /// E37): acht Spalten der Kuehluebergabe je Gebaeudetabelle und der vierte Sichtneubau;
    /// und der fuenfte Durchgang (<see cref="BaujahrSchema.SCHRITT"/>, G4a): das Baujahr je
    /// Gebaeudetabelle und der fuenfte Sichtneubau; und der sechste Durchgang
    /// (<see cref="NachtzeitSchema.SCHRITT"/>, E43): Beginn und Ende der Nachtabsenkung je
    /// Gebaeudetabelle und der sechste Sichtneubau; und der siebte Durchgang
    /// (<see cref="BaualtersklassenSchema.SCHRITT"/>, E47): der Energiestandard je Gebaeudetabelle und
    /// der siebte Sichtneubau.
    /// </summary>
    public static class GebaeudeSchema
    {
        /// <summary>Die Projekttabelle der Gebaeude.</summary>
        public const string TAB_GEBAEUDE = "Tab_Gebaeude";

        /// <summary>Der Auslieferungskatalog der Gebaeude.</summary>
        public const string TAB_GEBAEUDE_STAMM = "Tab_Gebaeude_STAMM";

        /// <summary>Die beiden Tabellen, die der Schritt anfasst - Projekt zuerst.</summary>
        public static readonly string[] TABELLEN = { TAB_GEBAEUDE, TAB_GEBAEUDE_STAMM };

        /// <summary>Die Sicht, aus der <c>ProjektGebaeudeCtrl</c> die Gebaeude eines Projekts liest.</summary>
        public const string VIEW = "Abfrage_Projektgebaeude";

        /// <summary>Der alte Name der Bezugsflaeche (bis Schritt 100).</summary>
        public const string SPALTE_WOHNFLAECHE_ALT = "Wohnflaeche";

        /// <summary>
        /// Die Bezugsflaeche des Gebaeudes (E19, Konzept N1.24). <b>Nicht zu verwechseln</b>
        /// mit <c>Wohnflaeche_gesamt</c> und den Skalierungsspalten der Projektzuordnung
        /// (<c>Z_ProjektGebaeude.Wohnflaeche_Waermebedarf</c>) - die behalten Namen und
        /// Bedeutung.
        /// </summary>
        public const string SPALTE_NUTZFLAECHE = "Nutzflaeche";

        // ---- die fuenfzehn neuen Spalten (Umsetzungskonzept 1.6 Tabelle, 1.7) -----------

        /// <summary>Rechenweg des Gebaeudes; NULL = <see cref="DbWerte.GEBAEUDE_MODELL_VDI6007"/> (E1).</summary>
        public const string SPALTE_GEBAEUDE_MODELL = "Gebaeude_Modell";
        /// <summary>Fensterflaeche Ost [m²]; NULL = die Haelfte von <c>Fensterflaeche_Ost_West</c>.</summary>
        public const string SPALTE_FENSTERFLAECHE_OST = "Fensterflaeche_Ost";
        /// <summary>Fensterflaeche West [m²]; NULL = die Haelfte von <c>Fensterflaeche_Ost_West</c>.</summary>
        public const string SPALTE_FENSTERFLAECHE_WEST = "Fensterflaeche_West";
        /// <summary>Rahmenanteil der Fenster [-]; NULL = Vorgabewert.</summary>
        public const string SPALTE_RAHMENANTEIL = "Rahmenanteil";
        /// <summary>Verschattungsfaktor [-]; NULL = Vorgabewert.</summary>
        public const string SPALTE_VERSCHATTUNGSFAKTOR = "Verschattungsfaktor";
        /// <summary>Randbedingung der Grundflaeche; NULL = <see cref="DbWerte.GRUND_ERDREICH"/>.</summary>
        public const string SPALTE_GRUNDFLAECHE_RANDBEDINGUNG = "Grundflaeche_Randbedingung";
        /// <summary>Kellertemperatur [°C]; NULL = Vorgabewert.</summary>
        public const string SPALTE_KELLERTEMPERATUR = "Kellertemperatur";
        /// <summary>Anteil der Aussenbauteile an der Masse [-]; NULL = Vorgabewert.</summary>
        public const string SPALTE_MASSEANTEIL_AUSSEN = "Masseanteil_Aussen";
        /// <summary>Innenflaechenfaktor, bezogen auf die Nutzflaeche [-]; NULL = Vorgabewert.</summary>
        public const string SPALTE_INNENFLAECHENFAKTOR = "Innenflaechenfaktor";
        /// <summary>Strahlungsanteil der Heizung [-]; NULL = Vorgabewert.</summary>
        public const string SPALTE_HEIZUNG_STRAHLUNGSANTEIL = "Heizung_Strahlungsanteil";
        /// <summary>Leistungsgrenze der idealen Heizung [kW]; NULL = unbegrenzt.</summary>
        public const string SPALTE_HEIZLEISTUNG_MAX = "Heizleistung_Max";
        /// <summary>Schalter (0/1, NOT NULL DEFAULT 0): Absorption und Abstrahlung der Aussenbauteile.</summary>
        public const string SPALTE_AUSSENBAUTEILE_STRAHLUNG = "Aussenbauteile_Strahlung";
        /// <summary>Infiltrationsluftwechsel [1/h]; NULL = Vorgabewert (Stufe G2).</summary>
        public const string SPALTE_LUFTWECHSEL_INFILTRATION = "Luftwechsel_Infiltration";
        /// <summary>Nutzerluftwechsel [1/h]; NULL = Vorgabewert (Stufe G2).</summary>
        public const string SPALTE_LUFTWECHSEL_NUTZER = "Luftwechsel_Nutzer";
        /// <summary>Schalter (0/1, NOT NULL DEFAULT 0): Sommerlueftung (Stufe G2).</summary>
        public const string SPALTE_SOMMERLUEFTUNG = "Sommerlueftung";

        /// <summary>
        /// Die fuenfzehn neuen Spalten JE Tabelle, in der Reihenfolge des Anlegens, mit der
        /// Typangabe in <b>Access</b>-Schreibweise - uebersetzt wird beim Anlegen
        /// (<c>StilleDb.SqliteSpaltenTyp</c>): <c>DOUBLE</c> → <c>REAL</c>,
        /// <c>TEXT(20)</c> → <c>TEXT</c>, <c>YESNO</c> →
        /// <c>INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))</c>. Kein DDL-DEFAULT auf einem
        /// Fachwert: NULL ist die Vorgabe.
        /// </summary>
        public static readonly KeyValuePair<string, string>[] NEUE_SPALTEN =
        {
            new KeyValuePair<string, string>(SPALTE_GEBAEUDE_MODELL,            "TEXT(20)"),
            new KeyValuePair<string, string>(SPALTE_FENSTERFLAECHE_OST,         "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_FENSTERFLAECHE_WEST,        "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_RAHMENANTEIL,               "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_VERSCHATTUNGSFAKTOR,        "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_GRUNDFLAECHE_RANDBEDINGUNG, "TEXT(20)"),
            new KeyValuePair<string, string>(SPALTE_KELLERTEMPERATUR,           "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_MASSEANTEIL_AUSSEN,         "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_INNENFLAECHENFAKTOR,        "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_HEIZUNG_STRAHLUNGSANTEIL,   "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_HEIZLEISTUNG_MAX,           "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_AUSSENBAUTEILE_STRAHLUNG,   "YESNO"),
            new KeyValuePair<string, string>(SPALTE_LUFTWECHSEL_INFILTRATION,   "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_LUFTWECHSEL_NUTZER,         "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_SOMMERLUEFTUNG,             "YESNO"),
        };

        /// <summary>Die zwei Schalter unter den neuen Spalten - NOT NULL DEFAULT 0, ohne NULL-Fall.</summary>
        public static readonly string[] SCHALTER = { SPALTE_AUSSENBAUTEILE_STRAHLUNG, SPALTE_SOMMERLUEFTUNG };

        /// <summary>Die zwei Textspalten unter den neuen Spalten (NULL = Vorgabe; leerer Text ebenso).</summary>
        public static readonly string[] TEXTSPALTEN = { SPALTE_GEBAEUDE_MODELL, SPALTE_GRUNDFLAECHE_RANDBEDINGUNG };

        /// <summary>
        /// Die 30 <see cref="SchemaSpalte"/>-Eintraege des Schritts - die fuenfzehn aus
        /// <see cref="NEUE_SPALTEN"/> fuer <c>Tab_Gebaeude</c>, dann fuer
        /// <c>Tab_Gebaeude_STAMM</c>. Der Name traegt keine Schrittnummer (F-S1).
        /// </summary>
        public static readonly SchemaSpalte[] Gebaeudespalten =
            TABELLEN.SelectMany(t => NEUE_SPALTEN.Select(s => new SchemaSpalte(t, s.Key, s.Value)))
                    .ToArray();

        // ---- KU-S1: die vier Kuehleingaben (Schemaschritt 108, Kuehlkonzept 7.1) -------

        /// <summary>
        /// Kuehlsollwert der Anlage [°C] — die Regelgroesse der Kuehlung; <b>NULL = Kuehlung
        /// aus</b>. Der Rueckfall auf <c>Maximaleraumtemperatur</c> ist eine ausdrueckliche
        /// Einstellung, kein stiller Wert (F-K1): <c>Maximaleraumtemperatur</c> bleibt die
        /// Ueberhitzungsgrenze des Stundenmodells, dieser Wert regelt die Anlage.
        /// </summary>
        public const string SPALTE_KUEHL_SOLLWERT = "Kuehl_Sollwert";
        /// <summary>Leistungsgrenze der idealen Kuehlung [kW]; NULL = unbegrenzt — das Gegenstueck zu <see cref="SPALTE_HEIZLEISTUNG_MAX"/>.</summary>
        public const string SPALTE_KUEHLLEISTUNG_MAX = "Kuehlleistung_Max";
        /// <summary>
        /// Schalter (0/1, NOT NULL DEFAULT 0): „dieses Gebaeude wird gekuehlt". Er traegt die
        /// ABSICHT, der Sollwert den WERT — wer die Kuehlung abschaltet, verliert den Sollwert
        /// nicht (Kuehlkonzept 7.1).
        /// </summary>
        public const string SPALTE_KUEHLUNG_AKTIV = "Kuehlung_Aktiv";
        /// <summary>
        /// Kuehlsollwert der Nacht [°C]; NULL = wie <see cref="SPALTE_KUEHL_SOLLWERT"/> (keine
        /// Nachtanhebung). Die Spalte gehoert zu KU-S1, gelesen und im Dialog angeboten wird
        /// sie erst mit der Stufe KU3 (K11, E27).
        /// </summary>
        public const string SPALTE_KUEHL_SOLLWERT_NACHT = "Kuehl_Sollwert_Nacht";

        /// <summary>
        /// Die vier Kuehlspalten JE Tabelle in der Reihenfolge von Kuehlkonzept 7.1, mit der
        /// Typangabe in <b>Access</b>-Schreibweise — uebersetzt beim Anlegen wie bei M3:
        /// <c>DOUBLE</c> → nullbares <c>REAL</c>, <c>YESNO</c> →
        /// <c>INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))</c>. Kein DDL-DEFAULT auf einem
        /// Fachwert: NULL ist die Vorgabe.
        /// </summary>
        public static readonly KeyValuePair<string, string>[] KUEHL_SPALTEN =
        {
            new KeyValuePair<string, string>(SPALTE_KUEHL_SOLLWERT,       "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_KUEHLLEISTUNG_MAX,    "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_KUEHLUNG_AKTIV,       "YESNO"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_SOLLWERT_NACHT, "DOUBLE"),
        };

        /// <summary>Der eine Schalter unter den Kuehlspalten - NOT NULL DEFAULT 0, ohne NULL-Fall.</summary>
        public static readonly string[] KUEHL_SCHALTER = { SPALTE_KUEHLUNG_AKTIV };

        /// <summary>
        /// Die acht <see cref="SchemaSpalte"/>-Eintraege von KU-S1 (Kuehlkonzept 7.1) - die
        /// vier aus <see cref="KUEHL_SPALTEN"/> fuer <c>Tab_Gebaeude</c>, dann fuer
        /// <c>Tab_Gebaeude_STAMM</c>. Der Name traegt keine Schrittnummer (F-S1).
        /// </summary>
        public static readonly SchemaSpalte[] Kuehlspalten =
            TABELLEN.SelectMany(t => KUEHL_SPALTEN.Select(s => new SchemaSpalte(t, s.Key, s.Value)))
                    .ToArray();

        // ---- AK-S1: die Waermeuebergabe (Schemaschritt 122, Anlagenkopplung 8.1) ------------
        //
        // Der dritte Durchgang. Dreizehn Spalten je Gebaeudetabelle beschreiben den Heizkreis
        // eines Gebaeudes: den Schalter, die Uebergabe (Art, Exponent, Nennleistung), den
        // Auslegungspunkt, die Heizkurve, das Proportionalband des Raumreglers (E25) und das
        // Sollwert-Zeitprogramm (H8). Die Gruppen und Bezeichner heissen hier "Uebergabe",
        // nicht "Heizkreis": SIM_HEIZKREIS benennt im Bestand die Waermesenke des
        // Anlagenschemas (H11). Die Spalte Heizkreis_Aktiv behaelt ihren Papiernamen.
        //
        // KEIN LESER IM RECHENWEG. Die Spalten reisen durch Namensleser, Katalogkopie und
        // Katalogschreibweg; die Rechnung liest sie erst mit der zweiten Welle von AK1.
        // Tab_Zone gibt es noch nicht - ihre drei Uebergabespalten legt der Zonenschritt S-C
        // mit an (Mehrzonenkonzept 4.2).

        /// <summary>
        /// Schalter (0/1, NOT NULL DEFAULT 0): „die Uebergabe dieses Gebaeudes wird gerechnet".
        /// Er traegt die ABSICHT, die Felder daneben die WERTE — wer ihn abschaltet, verliert
        /// seine Auslegungsdaten nicht (Anlagenkopplung 8.1).
        /// </summary>
        public const string SPALTE_HEIZKREIS_AKTIV = "Heizkreis_Aktiv";
        /// <summary>Uebergabeart (<see cref="DbWerte.UEBERGABE_RADIATOR"/> …); <b>NULL = ideal</b> — Kopplung aus, Bestandsweg.</summary>
        public const string SPALTE_UEBERGABE_ART = "Uebergabe_Art";
        /// <summary>Exponent der Uebergabegleichung [-]; NULL = Vorgabe der Uebergabeart (3.1).</summary>
        public const string SPALTE_UEBERGABE_EXPONENT = "Uebergabe_Exponent";
        /// <summary>Nennleistung der Uebergabe [kW]; NULL = aus Auslegungspunkt und gerechneter Auslegungsheizlast (8.4).</summary>
        public const string SPALTE_UEBERGABE_LEISTUNG_NENN = "Uebergabe_Leistung_Nenn";
        /// <summary>Auslegungsvorlauf [°C]; NULL = Vorgabe der Uebergabeart (3.1).</summary>
        public const string SPALTE_AUSLEGUNG_VORLAUF = "Auslegung_Vorlauf";
        /// <summary>Auslegungsruecklauf [°C]; NULL = Vorgabe der Uebergabeart (3.1).</summary>
        public const string SPALTE_AUSLEGUNG_RUECKLAUF = "Auslegung_Ruecklauf";
        /// <summary>Raumtemperatur im Auslegungspunkt [°C]; NULL = <c>Raumsolltemperatur_Tag</c>.</summary>
        public const string SPALTE_AUSLEGUNG_RAUMTEMPERATUR = "Auslegung_Raumtemperatur";
        /// <summary>Auslegungs-Aussentemperatur [°C]; NULL = kaeltestes Tagesmittel der Klimareihe, abgerundet (H10).</summary>
        public const string SPALTE_AUSLEGUNG_AUSSENTEMPERATUR = "Auslegung_Aussentemperatur";
        /// <summary>Schalter (0/1, NOT NULL DEFAULT 0): Vorlauf aus der Heizkurve statt fest aus <c>Tab_Energieanlagen.Vorlauf</c> (3.4).</summary>
        public const string SPALTE_HEIZKURVE_AKTIV = "Heizkurve_Aktiv";
        /// <summary>Niveau der Heizkurve [K]; NULL = 0.</summary>
        public const string SPALTE_HEIZKURVE_NIVEAU = "Heizkurve_Niveau";
        /// <summary>Steilheit der Heizkurve [-]; NULL = 1,0 — die Kurve durch den Auslegungspunkt.</summary>
        public const string SPALTE_HEIZKURVE_STEILHEIT = "Heizkurve_Steilheit";
        /// <summary>Proportionalband des Raumreglers [K]; NULL = 1,0 (EPOS-Vorgabe, H1); gewaehlt 0,5 / 1 / 2 K oder frei 0…5 K (E25).</summary>
        public const string SPALTE_REGLER_PROPORTIONALBAND = "Regler_Proportionalband";
        /// <summary>
        /// Sollwert-Zeitprogramm: 168 Raumsollwerte [°C], Montag 00:00 bis Sonntag 23:00,
        /// Trennzeichen <c>;</c> (4.3, H8). <b>NULL = die vier Bestandssollwerte und die
        /// Ferienmaske, unveraendert.</b> Format und strenger Leser:
        /// <see cref="AnlagenkopplungSchema.WochenprofilLesen"/>.
        /// </summary>
        public const string SPALTE_SOLLWERTPROFIL = "Sollwertprofil";

        /// <summary>
        /// Die dreizehn Uebergabespalten JE Tabelle in der Reihenfolge von Anlagenkopplung 8.1,
        /// mit der Typangabe in <b>Access</b>-Schreibweise — uebersetzt beim Anlegen wie bei M3:
        /// <c>DOUBLE</c> → nullbares <c>REAL</c>, <c>TEXT(n)</c> → <c>TEXT CHECK (length(…) &lt;= n)</c>,
        /// <c>YESNO</c> → <c>INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))</c>. Kein DDL-DEFAULT
        /// auf einem Fachwert: NULL ist die Vorgabe.
        /// </summary>
        public static readonly KeyValuePair<string, string>[] UEBERGABE_SPALTEN =
        {
            new KeyValuePair<string, string>(SPALTE_HEIZKREIS_AKTIV,            "YESNO"),
            new KeyValuePair<string, string>(SPALTE_UEBERGABE_ART,              "TEXT(20)"),
            new KeyValuePair<string, string>(SPALTE_UEBERGABE_EXPONENT,         "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_UEBERGABE_LEISTUNG_NENN,    "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_AUSLEGUNG_VORLAUF,          "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_AUSLEGUNG_RUECKLAUF,        "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_AUSLEGUNG_RAUMTEMPERATUR,   "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_AUSLEGUNG_AUSSENTEMPERATUR, "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_HEIZKURVE_AKTIV,            "YESNO"),
            new KeyValuePair<string, string>(SPALTE_HEIZKURVE_NIVEAU,           "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_HEIZKURVE_STEILHEIT,        "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_REGLER_PROPORTIONALBAND,    "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_SOLLWERTPROFIL,             "TEXT(1400)"),
        };

        /// <summary>Die zwei Schalter unter den Uebergabespalten - NOT NULL DEFAULT 0, ohne NULL-Fall.</summary>
        public static readonly string[] UEBERGABE_SCHALTER = { SPALTE_HEIZKREIS_AKTIV, SPALTE_HEIZKURVE_AKTIV };

        /// <summary>Die zwei Textspalten unter den Uebergabespalten (NULL = Vorgabe; leerer Text ebenso).</summary>
        public static readonly string[] UEBERGABE_TEXTSPALTEN = { SPALTE_UEBERGABE_ART, SPALTE_SOLLWERTPROFIL };

        /// <summary>
        /// Die 26 <see cref="SchemaSpalte"/>-Eintraege von AK-S1 an den Gebaeudetabellen
        /// (Anlagenkopplung 8.1) - die dreizehn aus <see cref="UEBERGABE_SPALTEN"/> fuer
        /// <c>Tab_Gebaeude</c>, dann fuer <c>Tab_Gebaeude_STAMM</c>. Der 27. Eintrag des
        /// Schritts ist die Projektspalte <see cref="AnlagenkopplungSchema.Projektspalte"/>.
        /// </summary>
        public static readonly SchemaSpalte[] Uebergabespalten =
            TABELLEN.SelectMany(t => UEBERGABE_SPALTEN.Select(s => new SchemaSpalte(t, s.Key, s.Value)))
                    .ToArray();

        // ---- KAK-S1, die Kuehluebergabe (vierter Durchgang, E37) ------------------------
        //
        // Acht Spalten je Gebaeudetabelle beschreiben die Kaelteseite der Kopplung
        // (Anlagenkopplung 8.1, Schritt K in 10.5). Der Schalter (A1) wie Heizkreis_Aktiv,
        // alle anderen nullbar - NULL ist die Vorgabe, die Bereiche prueft der Eingang.

        /// <summary>
        /// „Die Kuehluebergabe dieses Gebaeudes wird gerechnet" (A1): Schalter wie
        /// <see cref="SPALTE_HEIZKREIS_AKTIV"/>, <c>INTEGER NOT NULL DEFAULT 0 CHECK (… IN (0,1))</c>.
        /// Er traegt die ABSICHT, die Felder daneben die WERTE — wer ihn abschaltet, behaelt die Art.
        /// </summary>
        public const string SPALTE_KUEHLUEBERGABE_AKTIV = "Kuehluebergabe_Aktiv";
        /// <summary>Kuehluebergabeart (<c>DbWerte.KUEHLUEBERGABE_*</c>); NULL = ideal, Kaelteseite nicht gekoppelt.</summary>
        public const string SPALTE_KUEHL_UEBERGABE_ART = "Kuehl_Uebergabe_Art";
        /// <summary>Exponent der Kuehluebergabe [–]; NULL = Vorgabe der Art.</summary>
        public const string SPALTE_KUEHL_UEBERGABE_EXPONENT = "Kuehl_Uebergabe_Exponent";
        /// <summary>Nennleistung der Kuehluebergabe [kW], sensibel; NULL = aus dem Auslegungstag (A2).</summary>
        public const string SPALTE_KUEHL_UEBERGABE_LEISTUNG_NENN = "Kuehl_Uebergabe_Leistung_Nenn";
        /// <summary>Auslegungsvorlauf der Kuehluebergabe [°C]; NULL = Vorgabe der Art.</summary>
        public const string SPALTE_KUEHL_AUSLEGUNG_VORLAUF = "Kuehl_Auslegung_Vorlauf";
        /// <summary>Auslegungsruecklauf der Kuehluebergabe [°C]; NULL = Vorgabe der Art.</summary>
        public const string SPALTE_KUEHL_AUSLEGUNG_RUECKLAUF = "Kuehl_Auslegung_Ruecklauf";
        /// <summary>Raumtemperatur im Auslegungspunkt der Kuehluebergabe [°C]; NULL = <c>Kuehl_Sollwert</c>.</summary>
        public const string SPALTE_KUEHL_AUSLEGUNG_RAUMTEMPERATUR = "Kuehl_Auslegung_Raumtemperatur";
        /// <summary>
        /// Untere Grenze des Kaltwasser-Vorlaufs [°C] — eine Vorgabe statt einer Taupunktrechnung
        /// (7.2); NULL = Vorgabe der Art, beim Geblaesekonvektor keine Grenze.
        /// </summary>
        public const string SPALTE_KUEHL_VORLAUFGRENZE = "Kuehl_Vorlaufgrenze";

        /// <summary>
        /// Die acht Spalten der Kuehluebergabe JE Tabelle in der Reihenfolge von Anlagenkopplung
        /// 8.1 (KAK-S1), in <b>Access</b>-Schreibweise wie <see cref="UEBERGABE_SPALTEN"/>.
        /// </summary>
        public static readonly KeyValuePair<string, string>[] KUEHLUEBERGABE_SPALTEN =
        {
            new KeyValuePair<string, string>(SPALTE_KUEHLUEBERGABE_AKTIV,           "YESNO"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_UEBERGABE_ART,            "TEXT(20)"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_UEBERGABE_EXPONENT,       "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_UEBERGABE_LEISTUNG_NENN,  "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_AUSLEGUNG_VORLAUF,        "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_AUSLEGUNG_RUECKLAUF,      "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_AUSLEGUNG_RAUMTEMPERATUR, "DOUBLE"),
            new KeyValuePair<string, string>(SPALTE_KUEHL_VORLAUFGRENZE,            "DOUBLE"),
        };

        /// <summary>Der eine Schalter unter den Spalten der Kuehluebergabe - NOT NULL DEFAULT 0.</summary>
        public static readonly string[] KUEHLUEBERGABE_SCHALTER = { SPALTE_KUEHLUEBERGABE_AKTIV };

        /// <summary>
        /// Die 16 <see cref="SchemaSpalte"/>-Eintraege von KAK-S1 — die acht aus
        /// <see cref="KUEHLUEBERGABE_SPALTEN"/> fuer <c>Tab_Gebaeude</c>, dann fuer
        /// <c>Tab_Gebaeude_STAMM</c>. Die Nummer steht bei <see cref="KuehluebergabeSchema.SCHRITT"/>.
        /// </summary>
        public static readonly SchemaSpalte[] Kuehluebergabespalten =
            TABELLEN.SelectMany(t => KUEHLUEBERGABE_SPALTEN.Select(s => new SchemaSpalte(t, s.Key, s.Value)))
                    .ToArray();

        // ---- Das Baujahr (G4a; Schritt BaujahrSchema.SCHRITT, der fuenfte Sichtneubau) ---
        //
        // Eine Spalte je Gebaeudetabelle (Umsetzungskonzept Gebaeudesimulation 3.4, Zeile
        // "Baujahr (neue Spalte)"): die Jahreszahl, NULL = unbekannt. Sie steht neben der
        // Baualtersklasse, steuert aber nichts - kein Rechenweg und keine Vorgabe liest sie.

        /// <summary>
        /// Das Baujahr des Gebaeudes [a], ganzzahlig; NULL = unbekannt. Gefuellt von Hand oder
        /// vom IFC-Import (<c>Pset_BuildingCommon.YearOfConstruction</c>); kein Rechenweg liest es.
        /// </summary>
        public const string SPALTE_BAUJAHR = "Baujahr";

        /// <summary>Kleinstes zulaessiges Baujahr — dieselbe Grenze wie die Leseregel des Imports.</summary>
        public const int BAUJAHR_MIN = 1500;

        /// <summary>Groesstes zulaessiges Baujahr.</summary>
        public const int BAUJAHR_MAX = 2100;

        /// <summary>
        /// Die SQLite-Definition der Spalte: <c>INTEGER</c>, nullbar, mit Bereichspruefung
        /// <see cref="BAUJAHR_MIN"/> … <see cref="BAUJAHR_MAX"/>. Sie steht unmittelbar in
        /// SQLite-Schreibweise, weil die Typuebersetzung (<c>StilleDb.SqliteSpaltenTyp</c>) keine
        /// Bereichspruefung kennt.
        /// </summary>
        public static readonly string SQLITE_BAUJAHR =
            "INTEGER CHECK (" + SPALTE_BAUJAHR + " IS NULL OR " + SPALTE_BAUJAHR + " BETWEEN " +
            BAUJAHR_MIN.ToString(CultureInfo.InvariantCulture) + " AND " +
            BAUJAHR_MAX.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>Die Anweisung, die das Baujahr an einer Gebaeudetabelle anlegt (<c>ALTER TABLE … ADD COLUMN</c>).</summary>
        public static string BaujahrAnlegen(string tabelle)
            => "ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + SPALTE_BAUJAHR + "\" " + SQLITE_BAUJAHR;

        // ---- Die Nachtzeit (E43, N1.48; Schritt NachtzeitSchema.SCHRITT, der sechste Sichtneubau)
        //
        // Zwei Spalten je Gebaeudetabelle: Beginn und Ende der Nachtabsenkung als volle Stunde des
        // Tages (0 bis 23). Die Nacht ist [Beginn, Ende), zyklisch ueber Mitternacht. Beide NULL =
        // die Vorgabe des Stundenmodells (22 bis 6 Uhr, Nachtzeit.VORGABE_BEGINN/VORGABE_ENDE) -
        // bitgleich mit dem Fahrplan ohne die Spalten. Nur eine gesetzt oder Beginn = Ende ist ein
        // benannter Eingabefehler (Nachtzeit.Pruefen), kein CHECK: Die Tabelle haelt allein den
        // Bereich, das Paar pruefen Editor und Eingangsbauer mit derselben Regel.

        /// <summary>Beginn der Nachtabsenkung [Stunde des Tages 0 … 23]; NULL = Vorgabe (22 Uhr).</summary>
        public const string SPALTE_NACHTABSENKUNG_BEGINN = "Nachtabsenkung_Beginn";

        /// <summary>Ende der Nachtabsenkung [Stunde des Tages 0 … 23], ausschliesslich; NULL = Vorgabe (6 Uhr).</summary>
        public const string SPALTE_NACHTABSENKUNG_ENDE = "Nachtabsenkung_Ende";

        /// <summary>Kleinste zulaessige Stunde der Nachtzeit (0 Uhr).</summary>
        public const int NACHTSTUNDE_MIN = 0;

        /// <summary>Groesste zulaessige Stunde der Nachtzeit (23 Uhr).</summary>
        public const int NACHTSTUNDE_MAX = 23;

        /// <summary>Die zwei Spalten der Nachtzeit in ihrer Reihenfolge - Beginn, dann Ende.</summary>
        public static readonly string[] NACHTZEIT_SPALTEN = { SPALTE_NACHTABSENKUNG_BEGINN, SPALTE_NACHTABSENKUNG_ENDE };

        /// <summary>
        /// Die SQLite-Definition einer Nachtzeit-Spalte: <c>INTEGER</c>, nullbar, mit Bereichspruefung
        /// <see cref="NACHTSTUNDE_MIN"/> … <see cref="NACHTSTUNDE_MAX"/> - wie beim Baujahr unmittelbar in
        /// SQLite-Schreibweise.
        /// </summary>
        public static string SqliteNachtstunde(string spalte)
            => "INTEGER CHECK (" + spalte + " IS NULL OR " + spalte + " BETWEEN " +
               NACHTSTUNDE_MIN.ToString(CultureInfo.InvariantCulture) + " AND " +
               NACHTSTUNDE_MAX.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>Die Anweisung, die eine Nachtzeit-Spalte an einer Gebaeudetabelle anlegt (<c>ALTER TABLE … ADD COLUMN</c>).</summary>
        public static string NachtstundeAnlegen(string tabelle, string spalte)
            => "ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + spalte + "\" " + SqliteNachtstunde(spalte);

        // ---- Der Energiestandard (E47; Schritt BaualtersklassenSchema.SCHRITT, der siebte Sichtneubau)
        //
        // Eine Spalte je Gebaeudetabelle: der sprachneutrale Code (Energiestandard.CODES), NULL = keiner.
        // Die Tabelle haelt mit ihrem CHECK allein die Menge der Codes; dass Effizienzhaus 115/100 und 85
        // nur zu Wohngebaeuden passen, prueft der Editor (Energiestandard.PasstZu), nicht die Tabelle.

        /// <summary>Der Energiestandard als Code (<see cref="Energiestandard.CODES"/>); NULL = keiner.</summary>
        public const string SPALTE_ENERGIESTANDARD = "Energiestandard";

        /// <summary>
        /// Die SQLite-Definition der Spalte: <c>TEXT</c>, nullbar, <c>CHECK</c> auf die elf Codes - wie beim
        /// Baujahr unmittelbar in SQLite-Schreibweise; die Liste kommt aus <see cref="Energiestandard.CODES"/>.
        /// </summary>
        public static readonly string SQLITE_ENERGIESTANDARD =
            "TEXT CHECK (" + SPALTE_ENERGIESTANDARD + " IS NULL OR " + SPALTE_ENERGIESTANDARD + " IN (" +
            string.Join(", ", Energiestandard.CODES.Select(c => "'" + c + "'")) + "))";

        /// <summary>Die Anweisung, die den Energiestandard an einer Gebaeudetabelle anlegt (<c>ALTER TABLE … ADD COLUMN</c>).</summary>
        public static string EnergiestandardAnlegen(string tabelle)
            => "ALTER TABLE \"" + tabelle + "\" ADD COLUMN \"" + SPALTE_ENERGIESTANDARD + "\" " + SQLITE_ENERGIESTANDARD;

        // ---- die Sicht ----------------------------------------------------------------

        /// <summary>Verwirft die Sicht - wiederholbar (<c>IF EXISTS</c>).</summary>
        public const string SQL_VIEW_DROP = "DROP VIEW IF EXISTS \"" + VIEW + "\"";

        /// <summary>
        /// Die Spalten der Sicht, die aus <c>Z_ProjektGebaeude</c> kommen (Stellen 0..4) -
        /// die Skalierungsspalten nach E8 behalten ihre Namen.
        /// </summary>
        public static readonly string[] SICHT_ZUORDNUNG =
        {
            "ID_Projekt", "Wohnflaeche_Waermebedarf", "Einheit_Waermebedarf_Wohnflaeche",
            "Jahresnutzungsgrad", "dezWarmwasserbereitung",
        };

        /// <summary>
        /// Die Bestandsspalten der Sicht aus <c>Tab_Gebaeude</c> (Stellen 5..57), in der
        /// Reihenfolge von <c>sql/schema/002_views.sql</c> - mit <c>Nutzflaeche</c> an der
        /// Stelle, an der <c>Wohnflaeche</c> stand (32), und <c>ID</c> zuletzt (57).
        /// </summary>
        public static readonly string[] SICHT_GEBAEUDE =
        {
            "Gebaeudename", "Typ", "Beschreibung", "Wohnflaeche_gesamt", "Bewohner",
            "Flaeche_Nutzer", "Interne_Waermegewinne", "Bauweise", "Fensterflaeche_Sued",
            "Fensterflaeche_Ost_West", "Fensterflaeche_Nord", "Fensterdurchlassgrad",
            "Raumsolltemperatur_Nachtabsenkung", "Raumsolltemperatur_Tag",
            "Raumsolltemperatur_Wochenende", "Raumsolltemperatur_Ferien",
            "Maximaleraumtemperatur", "k_Wert_Außenwand", "k_Wert_Fenster",
            "k_Wert_Dachflaeche", "k_Wert_Grundflaeche", "k_Wert_Sonstiges",
            "Flaeche_Außenwand", "gesamte_Fensterflaeche", "Dachflaeche", "Grundflaeche",
            "Sonstige_Flaechen", SPALTE_NUTZFLAECHE, "Raumhoehe",
            "WBVK_Anschluß_Fenster_Wand", "WBVK_Anschluß_Wand_Dach",
            "WBVK_Anschluß_Außenwand_Kellerdecke", "Abmessung_Anschluß_Fenster_Wand",
            "Abmessung_Anschluß_Wand_Dach", "Abmessung_Anschluß_Außenwand_Kellerdecke",
            "Luftwechselrate", "Wochenende", "Ferien", "Ferienbeginn_1", "Ferienende_1",
            "Ferienbeginn_2", "Ferienende_2", "Ferienbeginn_3", "Ferienende_3",
            "Ferienbeginn_4", "Ferienende_4", "WW_Bedarf", "spez_Waermeverbrauch",
            "Waermebedarf", "Baualtersklasse", "Gebaeudeart",
            "Wohngebaeude_Nicht_Wohngebaeude", "ID",
        };

        /// <summary>
        /// Die 58 Bestandsspalten der Sicht in ihrer Reihenfolge (Stellen 0..57) - die
        /// Spaltennamen des Ergebnisses, nach denen <c>ProjektGebaeudeCtrl</c> liest.
        /// </summary>
        public static readonly string[] SICHT_BESTAND =
            SICHT_ZUORDNUNG.Concat(SICHT_GEBAEUDE).ToArray();

        /// <summary>
        /// Alle Spalten der neuen Sicht: die 58 Bestandsspalten, dahinter die fuenfzehn
        /// neuen (U5, E27) - <b>hinter</b> <c>Tab_Gebaeude.ID</c>.
        /// </summary>
        public static readonly string[] SICHT_ALLE =
            SICHT_BESTAND.Concat(NEUE_SPALTEN.Select(s => s.Key)).ToArray();

        /// <summary>
        /// DIE BAUVORSCHRIFT DER SICHT — fuer jeden Sichtneubau dieselbe: die 58
        /// Bestandsspalten an ihren Stellen, dahinter die Zusatzspalten der Durchgaenge in
        /// ihrer Reihenfolge (M3, KU-S1, AK-S1, KAK-S1, das Baujahr, dann die Nachtzeit). Ein Durchgang nennt
        /// nur, was er anhaengt.
        /// </summary>
        /// <param name="zusatzspalten">Die Spalten aus <c>Tab_Gebaeude</c> hinter <c>Tab_Gebaeude.ID</c>.</param>
        public static string SichtSql(IEnumerable<string> zusatzspalten)
        {
            return "CREATE VIEW [" + VIEW + "] AS\n" +
                   "SELECT " +
                   string.Join(", ",
                       SICHT_ZUORDNUNG.Select(s => "Z_ProjektGebaeude." + s)
                           .Concat(SICHT_GEBAEUDE.Select(s => "Tab_Gebaeude." + s))
                           .Concat(zusatzspalten.Select(s => "Tab_Gebaeude." + s))) +
                   "\nFROM Z_ProjektGebaeude INNER JOIN Tab_Gebaeude ON Z_ProjektGebaeude.ID = Tab_Gebaeude.ID_ProjektGebaeude";
        }

        /// <summary>
        /// Die Sichtdefinition des Schritts 101 (M3); <c>sql/schema/002_views.sql</c> bleibt
        /// der eingefrorene Stand 61. Wortgleich mit der Definition von dort, bis auf
        /// <c>Nutzflaeche</c> statt <c>Wohnflaeche</c> und die fuenfzehn neuen Spalten hinter
        /// <c>Tab_Gebaeude.ID</c>. Den GELTENDEN Stand nennt <see cref="SQL_VIEW_AKTUELL"/>.
        /// </summary>
        public static readonly string SQL_VIEW_NEU = SichtSql(NEUE_SPALTEN.Select(s => s.Key));

        /// <summary>
        /// Alle Spalten der Sicht ab Schritt 108 (KU-S1): die 73 aus <see cref="SICHT_ALLE"/>,
        /// dahinter die vier Kuehlspalten - an den Stellen 73..76.
        /// </summary>
        public static readonly string[] SICHT_KUEHLUNG =
            SICHT_ALLE.Concat(KUEHL_SPALTEN.Select(s => s.Key)).ToArray();

        /// <summary>Die Sichtdefinition des Schritts 108 (KU-S1): M3 und dahinter die vier Kuehlspalten.</summary>
        public static readonly string SQL_VIEW_KUEHLUNG =
            SichtSql(NEUE_SPALTEN.Select(s => s.Key).Concat(KUEHL_SPALTEN.Select(s => s.Key)));

        /// <summary>
        /// Alle Spalten der Sicht ab Schritt 122 (AK-S1): die 77 aus <see cref="SICHT_KUEHLUNG"/>,
        /// dahinter die dreizehn Uebergabespalten - an den Stellen 77..89.
        /// </summary>
        public static readonly string[] SICHT_UEBERGABE =
            SICHT_KUEHLUNG.Concat(UEBERGABE_SPALTEN.Select(s => s.Key)).ToArray();

        /// <summary>Die Sichtdefinition des Schritts 122 (AK-S1): M3, KU-S1 und dahinter die dreizehn Uebergabespalten.</summary>
        public static readonly string SQL_VIEW_UEBERGABE =
            SichtSql(NEUE_SPALTEN.Select(s => s.Key)
                                 .Concat(KUEHL_SPALTEN.Select(s => s.Key))
                                 .Concat(UEBERGABE_SPALTEN.Select(s => s.Key)));

        /// <summary>
        /// Alle Spalten der Sicht ab KAK-S1 (<see cref="KuehluebergabeSchema.SCHRITT"/>): die 90 aus
        /// <see cref="SICHT_UEBERGABE"/>, dahinter die acht Spalten der Kuehluebergabe - an den
        /// Stellen 90..97.
        /// </summary>
        public static readonly string[] SICHT_KUEHLUEBERGABE =
            SICHT_UEBERGABE.Concat(KUEHLUEBERGABE_SPALTEN.Select(s => s.Key)).ToArray();

        /// <summary>Die Sichtdefinition von KAK-S1: M3, KU-S1, AK-S1 und dahinter die acht Spalten der Kuehluebergabe.</summary>
        public static readonly string SQL_VIEW_KUEHLUEBERGABE =
            SichtSql(NEUE_SPALTEN.Select(s => s.Key)
                                 .Concat(KUEHL_SPALTEN.Select(s => s.Key))
                                 .Concat(UEBERGABE_SPALTEN.Select(s => s.Key))
                                 .Concat(KUEHLUEBERGABE_SPALTEN.Select(s => s.Key)));

        /// <summary>
        /// Alle Spalten der Sicht ab dem Schritt des Baujahrs (<see cref="BaujahrSchema.SCHRITT"/>,
        /// der fuenfte Durchgang): die 98 aus <see cref="SICHT_KUEHLUEBERGABE"/>, dahinter das
        /// Baujahr - an der Stelle 98.
        /// </summary>
        public static readonly string[] SICHT_BAUJAHR =
            SICHT_KUEHLUEBERGABE.Concat(new[] { SPALTE_BAUJAHR }).ToArray();

        /// <summary>Die Sichtdefinition des Baujahrs: M3, KU-S1, AK-S1, KAK-S1 und dahinter das Baujahr.</summary>
        public static readonly string SQL_VIEW_BAUJAHR =
            SichtSql(NEUE_SPALTEN.Select(s => s.Key)
                                 .Concat(KUEHL_SPALTEN.Select(s => s.Key))
                                 .Concat(UEBERGABE_SPALTEN.Select(s => s.Key))
                                 .Concat(KUEHLUEBERGABE_SPALTEN.Select(s => s.Key))
                                 .Concat(new[] { SPALTE_BAUJAHR }));

        /// <summary>
        /// Alle Spalten der Sicht ab dem Schritt der Nachtzeit (<see cref="NachtzeitSchema.SCHRITT"/>,
        /// der sechste Durchgang): die 99 aus <see cref="SICHT_BAUJAHR"/>, dahinter Beginn und Ende der
        /// Nachtabsenkung - an den Stellen 99 und 100.
        /// </summary>
        public static readonly string[] SICHT_NACHTZEIT =
            SICHT_BAUJAHR.Concat(NACHTZEIT_SPALTEN).ToArray();

        /// <summary>Die Sichtdefinition der Nachtzeit: M3, KU-S1, AK-S1, KAK-S1, das Baujahr und dahinter die zwei Nachtzeit-Spalten.</summary>
        public static readonly string SQL_VIEW_NACHTZEIT =
            SichtSql(NEUE_SPALTEN.Select(s => s.Key)
                                 .Concat(KUEHL_SPALTEN.Select(s => s.Key))
                                 .Concat(UEBERGABE_SPALTEN.Select(s => s.Key))
                                 .Concat(KUEHLUEBERGABE_SPALTEN.Select(s => s.Key))
                                 .Concat(new[] { SPALTE_BAUJAHR })
                                 .Concat(NACHTZEIT_SPALTEN));

        /// <summary>
        /// Alle Spalten der Sicht ab dem Schritt des Energiestandards (<see cref="BaualtersklassenSchema.SCHRITT"/>,
        /// der siebte Durchgang): die 101 aus <see cref="SICHT_NACHTZEIT"/>, dahinter der Energiestandard - an
        /// der Stelle 101.
        /// </summary>
        public static readonly string[] SICHT_ENERGIESTANDARD =
            SICHT_NACHTZEIT.Concat(new[] { SPALTE_ENERGIESTANDARD }).ToArray();

        /// <summary>Die Sichtdefinition des Energiestandards: M3, KU-S1, AK-S1, KAK-S1, das Baujahr, die Nachtzeit und dahinter der Energiestandard.</summary>
        public static readonly string SQL_VIEW_ENERGIESTANDARD =
            SichtSql(NEUE_SPALTEN.Select(s => s.Key)
                                 .Concat(KUEHL_SPALTEN.Select(s => s.Key))
                                 .Concat(UEBERGABE_SPALTEN.Select(s => s.Key))
                                 .Concat(KUEHLUEBERGABE_SPALTEN.Select(s => s.Key))
                                 .Concat(new[] { SPALTE_BAUJAHR })
                                 .Concat(NACHTZEIT_SPALTEN)
                                 .Concat(new[] { SPALTE_ENERGIESTANDARD }));

        /// <summary>
        /// Die Spalten der GELTENDEN Sicht - des letzten Sichtneubaus (derzeit der des Energiestandards).
        /// Wer die Sicht einer Datei gegen die Quelle haelt, nimmt diese Liste.
        /// </summary>
        public static string[] SICHT_AKTUELL => SICHT_ENERGIESTANDARD;

        /// <summary>Die GELTENDE Sichtdefinition - die des letzten Sichtneubaus (derzeit der des Energiestandards).</summary>
        public static string SQL_VIEW_AKTUELL => SQL_VIEW_ENERGIESTANDARD;

        /// <summary>Die Umbenennung einer Tabelle (E19).</summary>
        public static string UmbenennungSql(string tabelle)
            => "ALTER TABLE \"" + tabelle + "\" RENAME COLUMN \"" + SPALTE_WOHNFLAECHE_ALT +
               "\" TO \"" + SPALTE_NUTZFLAECHE + "\"";

        // ---- Auskunft und Ausfuehrung (Werkzeug und Nachweis) ---------------------------

        /// <summary>
        /// Steht der Schritt 101 vollstaendig? Beide Tabellen fuehren <c>Nutzflaeche</c> und
        /// keine <c>Wohnflaeche</c> mehr, alle 30 Spalten stehen, und die Sicht liefert
        /// <see cref="SICHT_ALLE"/> in dieser Reihenfolge an ihren Stellen 0..72. Was ein
        /// spaeterer Sichtneubau dahinter anhaengt (KU-S1), aendert daran nichts.
        /// </summary>
        public static bool Vollstaendig()
        {
            foreach (string t in TABELLEN)
            {
                if (!DataRepository.SpalteVorhanden(t, SPALTE_NUTZFLAECHE)) return false;
                if (DataRepository.SpalteVorhanden(t, SPALTE_WOHNFLAECHE_ALT)) return false;
            }
            foreach (SchemaSpalte s in Gebaeudespalten)
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) return false;
            return SichtBeginntMit(SICHT_ALLE);
        }

        /// <summary>
        /// Steht KU-S1 (Schritt 108) vollstaendig? Alle acht Kuehlspalten stehen, und die
        /// Sicht liefert <see cref="SICHT_KUEHLUNG"/> in dieser Reihenfolge an ihren Stellen
        /// 0..76.
        /// </summary>
        public static bool KuehlspaltenVollstaendig()
        {
            foreach (SchemaSpalte s in Kuehlspalten)
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) return false;
            return SichtBeginntMit(SICHT_KUEHLUNG);
        }

        /// <summary>
        /// Steht der Gebaeudeteil von AK-S1 (Schritt 122) vollstaendig? Alle 26
        /// Uebergabespalten stehen, und die Sicht liefert <see cref="SICHT_UEBERGABE"/> in
        /// dieser Reihenfolge an ihren Stellen 0..89. Die Projektspalte fragt
        /// <see cref="AnlagenkopplungSchema.UebergabeVollstaendig"/> dazu.
        /// </summary>
        public static bool UebergabespaltenVollstaendig()
        {
            foreach (SchemaSpalte s in Uebergabespalten)
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) return false;
            return SichtBeginntMit(SICHT_UEBERGABE);
        }

        /// <summary>
        /// Steht KAK-S1 (<see cref="KuehluebergabeSchema.SCHRITT"/>) vollstaendig? Alle 16 Spalten
        /// der Kuehluebergabe stehen, und die Sicht liefert <see cref="SICHT_KUEHLUEBERGABE"/> in
        /// dieser Reihenfolge an ihren Stellen 0..97.
        /// </summary>
        public static bool KuehluebergabespaltenVollstaendig()
        {
            foreach (SchemaSpalte s in Kuehluebergabespalten)
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) return false;
            return SichtBeginntMit(SICHT_KUEHLUEBERGABE);
        }

        /// <summary>
        /// Steht der Schritt des Baujahrs (<see cref="BaujahrSchema.SCHRITT"/>) vollstaendig? Beide
        /// Gebaeudetabellen fuehren <see cref="SPALTE_BAUJAHR"/>, und die Sicht liefert
        /// <see cref="SICHT_BAUJAHR"/> in dieser Reihenfolge an ihren Stellen 0..98.
        /// </summary>
        public static bool BaujahrVollstaendig()
        {
            foreach (string t in TABELLEN)
                if (!DataRepository.SpalteVorhanden(t, SPALTE_BAUJAHR)) return false;
            return SichtBeginntMit(SICHT_BAUJAHR);
        }

        /// <summary>
        /// Steht der Schritt der Nachtzeit (<see cref="NachtzeitSchema.SCHRITT"/>) vollstaendig? Beide
        /// Gebaeudetabellen fuehren <see cref="SPALTE_NACHTABSENKUNG_BEGINN"/> und
        /// <see cref="SPALTE_NACHTABSENKUNG_ENDE"/>, und die Sicht liefert <see cref="SICHT_NACHTZEIT"/> in
        /// dieser Reihenfolge an ihren Stellen 0..100.
        /// </summary>
        public static bool NachtzeitVollstaendig()
        {
            foreach (string t in TABELLEN)
                foreach (string s in NACHTZEIT_SPALTEN)
                    if (!DataRepository.SpalteVorhanden(t, s)) return false;
            return SichtBeginntMit(SICHT_NACHTZEIT);
        }

        /// <summary>
        /// Steht der Schritt des Energiestandards (<see cref="BaualtersklassenSchema.SCHRITT"/>) vollstaendig?
        /// Beide Gebaeudetabellen fuehren <see cref="SPALTE_ENERGIESTANDARD"/>, und die Sicht liefert
        /// <see cref="SICHT_ENERGIESTANDARD"/> in dieser Reihenfolge an ihren Stellen 0..101.
        /// </summary>
        public static bool EnergiestandardVollstaendig()
        {
            foreach (string t in TABELLEN)
                if (!DataRepository.SpalteVorhanden(t, SPALTE_ENERGIESTANDARD)) return false;
            return SichtBeginntMit(SICHT_ENERGIESTANDARD);
        }

        /// <summary>Beginnt die Spaltenfolge der Sicht mit <paramref name="soll"/>?</summary>
        private static bool SichtBeginntMit(string[] soll)
        {
            List<string> ist = SichtSpalten();
            return ist.Count >= soll.Length &&
                   ist.Take(soll.Length).SequenceEqual(soll, StringComparer.Ordinal);
        }

        /// <summary>Die Spaltennamen der Sicht, wie SQLite sie meldet (leer, wenn es sie nicht gibt).</summary>
        public static List<string> SichtSpalten()
        {
            var namen = new List<string>();
            DataTable dt = DataRepository.GetDataTable("SELECT name FROM pragma_table_info(?)",
                                                       new DbParam("?", VIEW));
            if (dt == null) return namen;
            foreach (DataRow r in dt.Rows)
                namen.Add(Convert.ToString(r["name"], CultureInfo.InvariantCulture));
            return namen;
        }

        /// <summary>
        /// Fuehrt den ganzen Schritt in EINEM Vorgang aus - fuer
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>; die Migration
        /// der Schale geht denselben Weg ueber ihre eigenen Helfer. Wiederholbar: Die
        /// Umbenennung laeuft nur, wo <c>Wohnflaeche</c> noch steht, eine vorhandene
        /// Spalte wird uebergangen, die Sicht wird immer neu gebaut.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten.</returns>
        public static int Alle(IList<string> bericht)
        {
            int angelegt = 0;
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            var umzubenennen = TABELLEN.Where(t => DataRepository.SpalteVorhanden(t, SPALTE_WOHNFLAECHE_ALT)).ToList();
            var fehlend = Gebaeudespalten.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Name)).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren(SQL_VIEW_DROP);
                    bericht?.Add("Sicht " + VIEW + " verworfen");
                    foreach (string t in umzubenennen)
                    {
                        v.Ausfuehren(UmbenennungSql(t));
                        bericht?.Add(t + ": " + SPALTE_WOHNFLAECHE_ALT + " -> " + SPALTE_NUTZFLAECHE);
                    }
                    foreach (SchemaSpalte s in fehlend)
                    {
                        v.Ausfuehren("ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " +
                                     StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
                        angelegt++;
                    }
                    bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                                 Gebaeudespalten.Length.ToString(CultureInfo.InvariantCulture) +
                                 " Spalte(n) angelegt");
                    v.Ausfuehren(SQL_VIEW_NEU);
                    bericht?.Add("Sicht " + VIEW + " neu gebaut (" +
                                 SICHT_ALLE.Length.ToString(CultureInfo.InvariantCulture) + " Spalten)");
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return angelegt;
        }

        /// <summary>
        /// Fuehrt KU-S1 (Schritt 108) in EINEM Vorgang aus - fuer
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>; die Migration
        /// der Schale geht denselben Weg ueber ihre eigenen Helfer. Sicht verwerfen, die
        /// fehlenden Kuehlspalten anlegen, Sicht aus <see cref="SQL_VIEW_KUEHLUNG"/> neu
        /// bauen. Setzt M3 voraus (<see cref="Alle"/> davor). Wiederholbar: Eine vorhandene
        /// Spalte wird uebergangen, die Sicht wird immer neu gebaut.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten.</returns>
        public static int KuehlspaltenAlle(IList<string> bericht)
        {
            int angelegt = 0;
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            var fehlend = Kuehlspalten.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Name)).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren(SQL_VIEW_DROP);
                    bericht?.Add("Sicht " + VIEW + " verworfen");
                    foreach (SchemaSpalte s in fehlend)
                    {
                        v.Ausfuehren("ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " +
                                     StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
                        angelegt++;
                    }
                    bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                                 Kuehlspalten.Length.ToString(CultureInfo.InvariantCulture) +
                                 " Kuehlspalte(n) angelegt");
                    v.Ausfuehren(SQL_VIEW_KUEHLUNG);
                    bericht?.Add("Sicht " + VIEW + " neu gebaut (" +
                                 SICHT_KUEHLUNG.Length.ToString(CultureInfo.InvariantCulture) + " Spalten)");
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return angelegt;
        }

        /// <summary>
        /// Fuehrt den Gebaeudeteil von AK-S1 (Schritt 122) in EINEM Vorgang aus - fuer
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c> ueber
        /// <see cref="AnlagenkopplungSchema.UebergabeAlle"/>; die Migration der Schale geht
        /// denselben Weg ueber ihre eigenen Helfer. Sicht verwerfen, die fehlenden
        /// Uebergabespalten anlegen, Sicht aus <see cref="SQL_VIEW_UEBERGABE"/> neu bauen. Setzt
        /// M3 und KU-S1 voraus. Wiederholbar: Eine vorhandene Spalte wird uebergangen, die
        /// Sicht wird immer neu gebaut. <b>Kein DML</b> - die Schalter stehen danach auf 0, die
        /// uebrigen Spalten auf NULL.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (hoechstens 26).</returns>
        public static int UebergabespaltenAlle(IList<string> bericht)
        {
            int angelegt = 0;
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            var fehlend = Uebergabespalten.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Name)).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren(SQL_VIEW_DROP);
                    bericht?.Add("Sicht " + VIEW + " verworfen");
                    foreach (SchemaSpalte s in fehlend)
                    {
                        v.Ausfuehren("ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " +
                                     StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
                        angelegt++;
                    }
                    bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                                 Uebergabespalten.Length.ToString(CultureInfo.InvariantCulture) +
                                 " Uebergabespalte(n) angelegt");
                    v.Ausfuehren(SQL_VIEW_UEBERGABE);
                    bericht?.Add("Sicht " + VIEW + " neu gebaut (" +
                                 SICHT_UEBERGABE.Length.ToString(CultureInfo.InvariantCulture) + " Spalten)");
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return angelegt;
        }

        /// <summary>
        /// Fuehrt KAK-S1 (<see cref="KuehluebergabeSchema.SCHRITT"/>) in EINEM Vorgang aus - fuer
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c> ueber
        /// <see cref="KuehluebergabeSchema.GebaeudeAlle"/>; die Migration der Schale geht denselben
        /// Weg ueber ihre eigenen Helfer. Sicht verwerfen, die fehlenden Spalten der Kuehluebergabe
        /// anlegen, Sicht aus <see cref="SQL_VIEW_KUEHLUEBERGABE"/> neu bauen. Setzt M3, KU-S1 und
        /// AK-S1 voraus; hinter ihm laeuft nur noch der Durchgang des Baujahrs
        /// (<see cref="BaujahrAlle"/>). Wiederholbar, <b>kein DML</b> - der Schalter steht danach
        /// auf 0, die uebrigen Spalten auf NULL.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (hoechstens 16).</returns>
        public static int KuehluebergabespaltenAlle(IList<string> bericht)
        {
            int angelegt = 0;
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            var fehlend = Kuehluebergabespalten.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Name)).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren(SQL_VIEW_DROP);
                    bericht?.Add("Sicht " + VIEW + " verworfen");
                    foreach (SchemaSpalte s in fehlend)
                    {
                        v.Ausfuehren("ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " +
                                     StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
                        angelegt++;
                    }
                    bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                                 Kuehluebergabespalten.Length.ToString(CultureInfo.InvariantCulture) +
                                 " Spalte(n) der Kuehluebergabe angelegt");
                    v.Ausfuehren(SQL_VIEW_KUEHLUEBERGABE);
                    bericht?.Add("Sicht " + VIEW + " neu gebaut (" +
                                 SICHT_KUEHLUEBERGABE.Length.ToString(CultureInfo.InvariantCulture) + " Spalten)");
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return angelegt;
        }

        /// <summary>
        /// Fuehrt den Schritt des Baujahrs (<see cref="BaujahrSchema.SCHRITT"/>) in EINEM Vorgang aus
        /// - fuer <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c> ueber
        /// <see cref="BaujahrSchema.Alle"/>; die Migration der Schale geht denselben Weg ueber ihre
        /// eigenen Helfer. Sicht verwerfen, die Spalte an beiden Gebaeudetabellen anlegen, wo sie
        /// fehlt, Sicht aus <see cref="SQL_VIEW_BAUJAHR"/> neu bauen. Setzt M3, KU-S1, AK-S1 und
        /// KAK-S1 voraus; hinter ihm laeuft nur noch der Durchgang der Nachtzeit
        /// (<see cref="NachtzeitAlle"/>). Wiederholbar, <b>kein DML</b> - die Spalte steht danach auf
        /// NULL.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (hoechstens zwei).</returns>
        public static int BaujahrAlle(IList<string> bericht)
        {
            int angelegt = 0;
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            var fehlend = TABELLEN.Where(t => !DataRepository.SpalteVorhanden(t, SPALTE_BAUJAHR)).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren(SQL_VIEW_DROP);
                    bericht?.Add("Sicht " + VIEW + " verworfen");
                    foreach (string t in fehlend)
                    {
                        v.Ausfuehren(BaujahrAnlegen(t));
                        angelegt++;
                    }
                    bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                                 TABELLEN.Length.ToString(CultureInfo.InvariantCulture) +
                                 " Spalte(n) Baujahr angelegt");
                    v.Ausfuehren(SQL_VIEW_BAUJAHR);
                    bericht?.Add("Sicht " + VIEW + " neu gebaut (" +
                                 SICHT_BAUJAHR.Length.ToString(CultureInfo.InvariantCulture) + " Spalten)");
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return angelegt;
        }

        /// <summary>
        /// Fuehrt den Schritt der Nachtzeit (<see cref="NachtzeitSchema.SCHRITT"/>) in EINEM Vorgang aus
        /// - fuer <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c> ueber
        /// <see cref="NachtzeitSchema.Alle"/>; die Migration der Schale geht denselben Weg ueber ihre
        /// eigenen Helfer. Sicht verwerfen, die zwei Spalten an beiden Gebaeudetabellen anlegen, wo sie
        /// fehlen, Sicht aus <see cref="SQL_VIEW_NACHTZEIT"/> neu bauen. Setzt M3, KU-S1, AK-S1, KAK-S1
        /// und das Baujahr voraus; hinter ihm laeuft nur noch der Durchgang des Energiestandards
        /// (<see cref="BaualtersklassenSchema.Ausfuehren"/>). Wiederholbar, <b>kein DML</b>
        /// - die Spalten stehen danach auf NULL (die Vorgabe 22 bis 6 Uhr).
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (hoechstens vier).</returns>
        public static int NachtzeitAlle(IList<string> bericht)
        {
            int angelegt = 0;
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            var fehlend = TABELLEN.SelectMany(t => NACHTZEIT_SPALTEN.Select(s => (Tabelle: t, Spalte: s)))
                                  .Where(p => !DataRepository.SpalteVorhanden(p.Tabelle, p.Spalte)).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren(SQL_VIEW_DROP);
                    bericht?.Add("Sicht " + VIEW + " verworfen");
                    foreach ((string tabelle, string spalte) in fehlend)
                    {
                        v.Ausfuehren(NachtstundeAnlegen(tabelle, spalte));
                        angelegt++;
                    }
                    bericht?.Add(angelegt.ToString(CultureInfo.InvariantCulture) + " von " +
                                 (TABELLEN.Length * NACHTZEIT_SPALTEN.Length).ToString(CultureInfo.InvariantCulture) +
                                 " Spalte(n) der Nachtzeit angelegt");
                    v.Ausfuehren(SQL_VIEW_NACHTZEIT);
                    bericht?.Add("Sicht " + VIEW + " neu gebaut (" +
                                 SICHT_NACHTZEIT.Length.ToString(CultureInfo.InvariantCulture) + " Spalten)");
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return angelegt;
        }
    }
}
