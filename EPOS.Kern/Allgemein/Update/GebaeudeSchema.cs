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
    // ====================================================================================

    /// <summary>
    /// Der Gebaeudespalten-Schritt M3 (Schemaschritt 101): Umbenennung
    /// <c>Wohnflaeche</c> → <c>Nutzflaeche</c>, fuenfzehn neue Spalten je Gebaeudetabelle
    /// und der Neubau der Sicht <c>Abfrage_Projektgebaeude</c> - EINE Quelle fuer
    /// Migration, Werkzeug und Nachweis.
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
        /// Die Sichtdefinition ab Schritt 101 - die einzige Quelle; <c>sql/schema/002_views.sql</c>
        /// bleibt der eingefrorene Stand 61. Wortgleich mit der Definition von dort, bis auf
        /// <c>Nutzflaeche</c> statt <c>Wohnflaeche</c> und die fuenfzehn neuen Spalten hinter
        /// <c>Tab_Gebaeude.ID</c>.
        /// </summary>
        public static readonly string SQL_VIEW_NEU =
            "CREATE VIEW [" + VIEW + "] AS\n" +
            "SELECT " +
            string.Join(", ",
                SICHT_ZUORDNUNG.Select(s => "Z_ProjektGebaeude." + s)
                    .Concat(SICHT_GEBAEUDE.Select(s => "Tab_Gebaeude." + s))
                    .Concat(NEUE_SPALTEN.Select(s => "Tab_Gebaeude." + s.Key))) +
            "\nFROM Z_ProjektGebaeude INNER JOIN Tab_Gebaeude ON Z_ProjektGebaeude.ID = Tab_Gebaeude.ID_ProjektGebaeude";

        /// <summary>Die Umbenennung einer Tabelle (E19).</summary>
        public static string UmbenennungSql(string tabelle)
            => "ALTER TABLE \"" + tabelle + "\" RENAME COLUMN \"" + SPALTE_WOHNFLAECHE_ALT +
               "\" TO \"" + SPALTE_NUTZFLAECHE + "\"";

        // ---- Auskunft und Ausfuehrung (Werkzeug und Nachweis) ---------------------------

        /// <summary>
        /// Steht der Schritt vollstaendig? Beide Tabellen fuehren <c>Nutzflaeche</c> und
        /// keine <c>Wohnflaeche</c> mehr, alle 30 Spalten stehen, und die Sicht liefert
        /// genau <see cref="SICHT_ALLE"/> in dieser Reihenfolge.
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
            return SichtSpalten().SequenceEqual(SICHT_ALLE, StringComparer.Ordinal);
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
    }
}
