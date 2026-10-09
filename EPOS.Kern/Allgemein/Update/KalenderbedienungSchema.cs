using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // K2 - KALENDERBEDIENUNG STUFE 2 (Konzept Konditionierungsprofile 7.8 „Die zwei Stufen", E110).
    //
    // WAS. Fuenf Teile in EINEM Schritt:
    //   a. GEMEINSAMER KALENDER „alle Groessen": Tab_Konditionierungskalender kennt neben den fuenf
    //      Groessen die Pseudogroesse ALLE - je Eigentuemer (Gebaeude, Zone, Katalogbau, Katalogzone,
    //      Vorlage) hoechstens einer, ueber dieselben Teilindizes wie die Groessen. Er traegt keine
    //      Angabe (CHECK), seine Perioden tragen die Groessenmaske Gilt_Fuer. Eine Periode dort wirkt in
    //      jeder Groesse der Maske, deren Kalender angelegt ist, so, als stuende sie als Kopie in deren
    //      Kalender (gleicher Rang, gleiche Angabe); hat der Groessenkalender am selben Rang eine eigene
    //      Periode, gilt die eigene.
    //   b. BENANNTE WOCHEN: Tab_Konditionierungswoche (Eigentuemer wie die Kalender, Name, Groesse, die
    //      168 Werte im Wochenformat), nullbarer Verweis ID_Woche an der Periode; der Verweis geht der
    //      eingebetteten Woche vor. Er ist eine Angabe wie Woche; beide duerfen zugleich stehen.
    //   c. WOCHENENDE AM GEBAEUDE: Tab_Gebaeude(_STAMM).Wochenendtage, eine Wochenmaske Mo = Bit 0 …
    //      So = Bit 6 (die Zaehlung des Wochenrasters, Kalenderwoche.Stelle: Montag = Tag 0), Vorgabe 96 =
    //      Sa + So. Eine Zahl statt Text, weil der Ortszeit-Kalender intern je Tag einen Merker fuehrt und
    //      der Generator den Wochentag als 0 … 6 zaehlt - die Maske ist dann ein Bittest, der CHECK ein
    //      Bereich. NULL heisst Vorgabe (wie die WQ_*-Felder): Die Pruefsumme eines Katalogsatzes ueberspringt
    //      leere Fachwerte und bleibt so unveraendert.
    //   d. LAENDERFEIERTAGE: der CHECK der Feiertagsregel kennt die acht Laenderregeln
    //      (DbWerte.KOND_FEIERTAGE_LAENDER), Tab_Gebaeude(_STAMM).Feiertagsland das Land (ISO-Kuerzel,
    //      NULL = nur bundeseinheitlich). Die neun bundeseinheitlichen Regeln bleiben in Bedeutung und Rang.
    //      Weggelassen: das Augsburger Friedensfest (gilt nur in der Stadt Augsburg, keinem Land
    //      zuzuordnen), Oster- und Pfingstsonntag (BB, HE; fallen stets auf einen Sonntag).
    //   e. FERIENLISTE: Ferienzeitraeume sind Perioden der Art FERIEN im gemeinsamen Kalender des
    //      Gebaeudes OHNE Angabe (die Angabe stellt je Groesse die Ferienzeile der Matrix). Die ersten vier
    //      (Rang 200 … 203) SPIEGELN die Spalten Ferienbeginn/-ende_1…4: Ein Trigger je Gebaeudetabelle
    //      schreibt sie nach jeder Aenderung einer der acht Spalten neu. Die Spalten bleiben die Quelle der
    //      Leser (Generator, Tagesbilanz-Weg, Zapfkalender) - die Ergebnisse aendern sich nicht.
    //
    // WARUM DIE MASKE ALS BITMASKE (eine Spalte statt fuenf 0/1-Spalten). Ein CHECK (1 … 31) haelt die
    // ganze Regel „mindestens eine Groesse"; fuenf Spalten braeuchten fuenf CHECKs und einen sechsten fuer
    // „nicht alle 0". Der Leser prueft ein Bit (Maskenbit) statt einer Spalte je Groesse. Bit k ist die
    // k-te Groesse in DbWerte.KOND_GROESSEN (HEIZSOLL = 1 … PERSONEN = 16).
    //
    // PRUEFREGEL DES EIGENTUEMERS. Kalender: ALLE ohne Angabe (CHECK im Neubau). Periode: Gilt_Fuer
    // steht GENAU in den Perioden des ALLE-Kalenders, eine Periode ohne Angabe nur dort und nur als FERIEN -
    // ein CHECK sieht den Kalender nicht, deshalb zwei Trigger (BEFORE INSERT, BEFORE UPDATE).
    //
    // NEUBAU STATT ALTER. SQLite kann einem CHECK nichts nachtragen; Kalender und Periode werden nach dem
    // Rezept aus Schritt 96/151 neu gebaut (KonditionierungVorlagenSchema: Zieltext aus dem GELTENDEN
    // sqlite_master.sql, jede Stelle genau einmal, sonst benannter Abbruch; legacy_alter_table, Zaehler,
    // Indizes; ein Vorgang ohne Fremdschluessel).
    //
    // MIGRATION (deterministisch, ergebnisneutral). (1) Die Ferienliste entsteht aus den Spalten aller
    // Gebaeude und Katalogbauten (dieselbe Regel wie der Trigger). (2) Gekoppelte Kopien - Perioden
    // ausser FERIEN, die in mindestens zwei Groessenkalendern EINES Eigentuemers mit gleichem Rang, gleicher
    // Art, gleichem Bezeichner, gleichen Tagen bzw. gleicher Regel und gleicher Angabe stehen - werden EINE
    // Periode des gemeinsamen Kalenders mit der Maske ihrer Groessen (Kalendergemeinschaft.Zusammenfuehren).
    // Ist der Rang im gemeinsamen Kalender schon belegt, bleiben die Kopien stehen. Weil die
    // Gemeinschaftsperiode beim Lesen in genau diese Kalender mit genau dieser Angabe zurueckkehrt, aendert
    // sich kein Rechenergebnis.
    //
    // NUMMER. PvGanglinieSchema.SCHRITT + 1 (endgueltig 207). Eingetragen in SchemaStand.Zielversion, im
    // Register der Paketanhebung, in der SchemaMigration der Schale, in Werkzeuge/Testdatenbankschema und
    // in EPOS.Kern.Tests/TestDatenbank.
    // ====================================================================================

    /// <summary>
    /// <b>K2</b> — Kalenderbedienung Stufe 2: gemeinsamer Kalender „alle Größen" mit Größenmaske, benannte
    /// Wochen, Wochenende und Feiertagsland am Gebäude, Länderfeiertage als Regeln, Ferienliste als Spiegel
    /// der vier Ferienspalten. Anlass, Bauform und Migrationsregel stehen im Kopf der Datei.
    /// </summary>
    public static class KalenderbedienungSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der Schritt hinter
        /// <see cref="PvGanglinieSchema"/>.
        /// </summary>
        public const int SCHRITT = PvGanglinieSchema.SCHRITT + 1;

        /// <summary>Die Tabelle der benannten Wochen.</summary>
        public const string TAB_WOCHE = "Tab_Konditionierungswoche";

        /// <summary>Spaltenzahl von <c>Tab_Konditionierungsperiode</c> nach dem Schritt (Maske und Wochenverweis dazu).</summary>
        public const int SPALTENZAHL_PERIODE = KonditionierungSchema.SPALTENZAHL_PERIODE + 2;

        /// <summary>Die Größenmaske einer Periode des gemeinsamen Kalenders (1 … 31).</summary>
        public const string SPALTE_GILT_FUER = "Gilt_Fuer";

        /// <summary>Der Verweis einer Periode auf eine benannte Woche.</summary>
        public const string SPALTE_ID_WOCHE = "ID_Woche";

        /// <summary>
        /// Die Wochenmaske des Wochenendes am Gebäude (Mo = Bit 0 … So = Bit 6); <b>leer heißt Vorgabe</b>
        /// (<see cref="WOCHENENDE_VORGABE"/>) — so bleiben die Prüfsummen der Katalogsätze unberührt.
        /// </summary>
        public const string SPALTE_WOCHENENDTAGE = "Wochenendtage";

        /// <summary>Das Land der Feiertage am Gebäude (ISO-Kürzel; NULL = nur bundeseinheitlich).</summary>
        public const string SPALTE_FEIERTAGSLAND = "Feiertagsland";

        /// <summary>Die Vorgabe des Wochenendes: Samstag (Bit 5) und Sonntag (Bit 6) = 96.</summary>
        public const int WOCHENENDE_VORGABE = 96;

        /// <summary>Die Maske „alle fünf Größen".</summary>
        public const int MASKE_ALLE = 31;

        /// <summary>Der Rang des ersten gespiegelten Ferienzeitraums (Ferien k auf Rang 199 + k).</summary>
        public const int RANG_FERIEN_ERSTER = 200;

        /// <summary>Die Zahl der gespiegelten Ferienspalten.</summary>
        public const int FERIENSPALTEN = 4;

        /// <summary>Die beiden Gebäudetabellen, die Wochenende, Feiertagsland und den Ferienspiegel bekommen.</summary>
        public static IReadOnlyList<string> Gebaeudetabellen { get; } = new[] { "Tab_Gebaeude", "Tab_Gebaeude_STAMM" };

        /// <summary>Das Bit einer Größe in der Maske; 0 für ein unbekanntes Kennwort (auch <c>ALLE</c>).</summary>
        public static int Maskenbit(string groesse)
        {
            for (int i = 0; i < DbWerte.KOND_GROESSEN.Count; i++)
                if (string.Equals(DbWerte.KOND_GROESSEN[i], groesse, StringComparison.Ordinal)) return 1 << i;
            return 0;
        }

        /// <summary>Gehört <paramref name="wochentag"/> (0 = Montag … 6 = Sonntag) zum Wochenende der Maske?</summary>
        public static bool IstWochenendtag(int maske, int wochentag)
            => wochentag >= 0 && wochentag < 7 && (maske & (1 << wochentag)) != 0;

        private static string Liste(IEnumerable<string> werte) => string.Join(",", werte.Select(w => "'" + w + "'"));

        private const string ALLE = DbWerte.KOND_GROESSE_ALLE;

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Konditionierungswoche</c> — die benannten Wochen, STRICT. Eigentümer
        /// wie die Kalender (genau ein Gebäude, Katalogbau oder eine Vorlage; Zone nur mit Gebäude, Katalogzone nur
        /// mit Katalogbau), alle mit Kaskade.
        /// </summary>
        public static readonly string SQL_CREATE_WOCHE =
            "CREATE TABLE IF NOT EXISTS \"" + TAB_WOCHE + "\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Gebaeude\" INTEGER REFERENCES \"Tab_Gebaeude\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Zone\" INTEGER REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Gebaeude_Stamm\" INTEGER REFERENCES \"Tab_Gebaeude_STAMM\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Zone_Stamm\" INTEGER REFERENCES \"Tab_Zone_STAMM\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Vorlage\" INTEGER REFERENCES \"Tab_Konditionierungsvorlage_STAMM\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Groesse\" TEXT NOT NULL CHECK (\"Groesse\" IN (" + KonditionierungSchema.WERTE_GROESSE + ")),\n" +
            "    \"Name\" TEXT NOT NULL CHECK (length(\"Name\") BETWEEN 1 AND " +
                KonditionierungSchema.BEZEICHNER_MAX_ZEICHEN.ToString(CultureInfo.InvariantCulture) + "),\n" +
            "    \"Woche\" TEXT NOT NULL CHECK (length(\"Woche\") BETWEEN 1 AND " +
                KonditionierungSchema.WOCHE_MAX_ZEICHEN.ToString(CultureInfo.InvariantCulture) + "),\n" +
            "    CHECK ((\"ID_Gebaeude\" IS NOT NULL) + (\"ID_Gebaeude_Stamm\" IS NOT NULL) + (\"ID_Vorlage\" IS NOT NULL) = 1),\n" +
            "    CHECK (\"ID_Zone\" IS NULL OR \"ID_Gebaeude\" IS NOT NULL),\n" +
            "    CHECK (\"ID_Zone_Stamm\" IS NULL OR \"ID_Gebaeude_Stamm\" IS NOT NULL)\n" +
            ") STRICT";

        /// <summary>Die Spaltendefinition von <see cref="SPALTE_WOCHENENDTAGE"/> für <c>ALTER TABLE … ADD COLUMN</c>.</summary>
        public static readonly string DEFINITION_WOCHENENDTAGE =
            "INTEGER CHECK (\"" + SPALTE_WOCHENENDTAGE + "\" IS NULL OR \"" + SPALTE_WOCHENENDTAGE + "\" BETWEEN 0 AND 127)";

        /// <summary>Die Spaltendefinition von <see cref="SPALTE_FEIERTAGSLAND"/> für <c>ALTER TABLE … ADD COLUMN</c>.</summary>
        public static readonly string DEFINITION_FEIERTAGSLAND =
            "TEXT CHECK (\"" + SPALTE_FEIERTAGSLAND + "\" IS NULL OR \"" + SPALTE_FEIERTAGSLAND + "\" IN (" +
            Liste(Landesfeiertage.BUNDESLAENDER) + "))";

        /// <summary>Die einfachen Indizes (Name → Anweisung): Eigentümer und Name der Wochen, Verweis der Periode.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Indexanweisungen
        {
            get
            {
                foreach (string s in new[] { "ID_Gebaeude", "ID_Zone", "ID_Gebaeude_Stamm", "ID_Zone_Stamm", "ID_Vorlage" })
                    yield return Index("idx_KondWoche_" + s.Replace("ID_", "").Replace("_", ""),
                        "CREATE INDEX IF NOT EXISTS \"idx_KondWoche_" + s.Replace("ID_", "").Replace("_", "") +
                        "\" ON \"" + TAB_WOCHE + "\" (\"" + s + "\")");
                yield return Index("idx_KondWoche_EindeutigName",
                    "CREATE UNIQUE INDEX IF NOT EXISTS \"idx_KondWoche_EindeutigName\" ON \"" + TAB_WOCHE + "\" " +
                    "(IFNULL(\"ID_Gebaeude\", 0), IFNULL(\"ID_Zone\", 0), IFNULL(\"ID_Gebaeude_Stamm\", 0), " +
                    "IFNULL(\"ID_Zone_Stamm\", 0), IFNULL(\"ID_Vorlage\", 0), \"Groesse\", \"Name\")");
                yield return Index("idx_KondPeriode_Woche",
                    "CREATE INDEX IF NOT EXISTS \"idx_KondPeriode_Woche\" ON \"" + KonditionierungSchema.TAB_PERIODE +
                    "\" (\"" + SPALTE_ID_WOCHE + "\")");
            }
        }

        private static KeyValuePair<string, string> Index(string name, string sql) => new KeyValuePair<string, string>(name, sql);

        // ---- Die Pruefregel der Periode (zwei Trigger) ----

        private static readonly string BEDINGUNG_PRUEFREGEL =
            "((SELECT \"Groesse\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID\" = NEW.\"ID_Kalender\") = '" + ALLE +
            "') <> (NEW.\"" + SPALTE_GILT_FUER + "\" IS NOT NULL)\n" +
            "     OR (IFNULL((SELECT \"Groesse\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID\" = NEW.\"ID_Kalender\"), '') <> '" +
            ALLE + "'\n         AND NEW.\"Wert\" IS NULL AND NEW.\"Aus\" = 0 AND NEW.\"Woche\" IS NULL AND NEW.\"" + SPALTE_ID_WOCHE +
            "\" IS NULL AND NEW.\"WieWochentag\" IS NULL)";

        private const string MELDUNG_PRUEFREGEL =
            "Tab_Konditionierungsperiode: Gilt_Fuer steht genau in den Perioden des gemeinsamen Kalenders (ALLE); " +
            "eine Periode ohne Angabe nur dort";

        /// <summary>Die Trigger (Name → Anweisung): Prüfregel der Periode und Ferienspiegel beider Gebäudetabellen.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Triggeranweisungen
        {
            get
            {
                foreach (string ereignis in new[] { "INSERT", "UPDATE" })
                {
                    string name = "trg_KondPeriode_Gemeinsam_" + (ereignis == "INSERT" ? "Einfuegen" : "Aendern");
                    yield return Index(name,
                        "CREATE TRIGGER IF NOT EXISTS \"" + name + "\" BEFORE " + ereignis + " ON \"" +
                        KonditionierungSchema.TAB_PERIODE + "\"\nBEGIN\n" +
                        "    SELECT RAISE(ABORT, '" + MELDUNG_PRUEFREGEL + "')\n    WHERE " + BEDINGUNG_PRUEFREGEL + ";\nEND");
                }
                foreach (string tabelle in Gebaeudetabellen)
                {
                    string name = "trg_" + tabelle.Replace("Tab_", "") + "_Ferienspiegel";
                    yield return Index(name, Ferienspiegeltrigger(name, tabelle));
                }
            }
        }

        /// <summary>Eigentümerspalte und Zonenspalte des gemeinsamen Kalenders einer Gebäudetabelle.</summary>
        private static void Eigentuemerspalten(string tabelle, out string eigner, out string zone)
        {
            bool stamm = tabelle.EndsWith("_STAMM", StringComparison.Ordinal);
            eigner = stamm ? KonditionierungSchema.SPALTE_ID_GEBAEUDE_STAMM : KonditionierungSchema.SPALTE_ID_GEBAEUDE;
            zone = stamm ? ZonenKatalogSchema.SPALTE_ID_ZONE_STAMM : KonditionierungSchema.SPALTE_ID_ZONE;
        }

        /// <summary>Ist das Ferienpaar k (1 … 4) der Zeile <paramref name="q"/> (Präfix) ein gültiger Zeitraum?</summary>
        private static string Gueltig(string q, int k)
        {
            string b = q + "\"Ferienbeginn_" + k.ToString(CultureInfo.InvariantCulture) + "\"";
            string e = q + "\"Ferienende_" + k.ToString(CultureInfo.InvariantCulture) + "\"";
            return "(" + b + " BETWEEN 1 AND 365 AND " + e + " BETWEEN 1 AND 365 AND " +
                   b + " = CAST(" + b + " AS INTEGER) AND " + e + " = CAST(" + e + " AS INTEGER))";
        }

        private static string IrgendeinGueltiges(string q)
            => "(" + string.Join(" OR ", Enumerable.Range(1, FERIENSPALTEN).Select(k => Gueltig(q, k))) + ")";

        private static string OhneAngabe(string p)
            => p + "\"Art\" = '" + DbWerte.KOND_ART_FERIEN + "' AND " + p + "\"Wert\" IS NULL AND " + p + "\"Aus\" = 0 AND " +
               p + "\"Woche\" IS NULL AND " + p + "\"" + SPALTE_ID_WOCHE + "\" IS NULL AND " + p + "\"WieWochentag\" IS NULL";

        /// <summary>
        /// Die Anweisungen, die den Ferienspiegel einer Gebäudetabelle schreiben — im Trigger mit
        /// <paramref name="neu"/> = <c>NEW.</c>, in der Migration über alle Zeilen (<paramref name="neu"/> = <c>null</c>).
        /// </summary>
        public static IReadOnlyList<string> Ferienspiegelanweisungen(string tabelle, string neu)
        {
            Eigentuemerspalten(tabelle, out string eigner, out string zone);
            string kal = KonditionierungSchema.TAB_KALENDER, per = KonditionierungSchema.TAB_PERIODE;
            bool trigger = neu != null;
            string q = trigger ? neu : "g.";
            string quelle = trigger ? "" : " FROM \"" + tabelle + "\" g";
            string id = q + "\"ID\"";
            var liste = new List<string>();

            // 1. Der gespiegelte Teil der Liste faellt (nur Perioden ohne Angabe auf Rang 200 ... 203).
            liste.Add("DELETE FROM \"" + per + "\" WHERE \"Rang\" BETWEEN " + RANG_FERIEN_ERSTER.ToString(CultureInfo.InvariantCulture) +
                      " AND " + (RANG_FERIEN_ERSTER + FERIENSPALTEN - 1).ToString(CultureInfo.InvariantCulture) + " AND " + OhneAngabe("") +
                      " AND \"ID_Kalender\" IN (SELECT k.\"ID\" FROM \"" + kal + "\" k" + (trigger ? "" : " JOIN \"" + tabelle +
                      "\" g ON g.\"ID\" = k.\"" + eigner + "\"") + " WHERE " + (trigger ? "k.\"" + eigner + "\" = " + id + " AND " : "") +
                      "k.\"" + zone + "\" IS NULL AND k.\"Groesse\" = '" + ALLE + "')");

            // 2. Der gemeinsame Kalender entsteht, wenn ein Zeitraum gilt, er fehlt und das Gebaeude schon einen
            //    Groessenkalender fuehrt (ein Gebaeude ohne Kalender rechnet auf dem Bestandszweig aus den Spalten).
            liste.Add("INSERT INTO \"" + kal + "\" (\"" + eigner + "\", \"Groesse\", \"Aus\") SELECT " + id + ", '" + ALLE + "', 0" +
                      quelle + " WHERE " + IrgendeinGueltiges(q) + " AND NOT EXISTS (SELECT 1 FROM \"" + kal + "\" k WHERE k.\"" +
                      eigner + "\" = " + id + " AND k.\"" + zone + "\" IS NULL AND k.\"Groesse\" = '" + ALLE + "')" +
                      " AND EXISTS (SELECT 1 FROM \"" + kal + "\" k WHERE k.\"" + eigner + "\" = " + id + " AND k.\"" + zone +
                      "\" IS NULL AND k.\"Groesse\" <> '" + ALLE + "')" + (trigger ? "" : " ORDER BY g.\"ID\""));

            // 3. Je gueltigem Paar eine Periode „Ferien k" auf Rang 199 + k, Maske alle, ohne Angabe.
            for (int k = 1; k <= FERIENSPALTEN; k++)
            {
                string nr = k.ToString(CultureInfo.InvariantCulture);
                string rang = (RANG_FERIEN_ERSTER + k - 1).ToString(CultureInfo.InvariantCulture);
                liste.Add("INSERT INTO \"" + per + "\" (\"ID_Kalender\", \"Rang\", \"Art\", \"Bezeichner\", \"Beginn\", \"Ende\", \"Aus\", \"" +
                          SPALTE_GILT_FUER + "\") SELECT k.\"ID\", " + rang + ", '" + DbWerte.KOND_ART_FERIEN + "', 'Ferien " + nr +
                          "', CAST(" + q + "\"Ferienbeginn_" + nr + "\" AS INTEGER), CAST(" + q + "\"Ferienende_" + nr + "\" AS INTEGER), 0, " +
                          MASKE_ALLE.ToString(CultureInfo.InvariantCulture) + " FROM \"" + kal + "\" k" +
                          (trigger ? "" : " JOIN \"" + tabelle + "\" g ON g.\"ID\" = k.\"" + eigner + "\"") +
                          " WHERE k.\"" + eigner + "\" = " + id + " AND k.\"" + zone + "\" IS NULL AND k.\"Groesse\" = '" + ALLE + "' AND " +
                          Gueltig(q, k) + " AND NOT EXISTS (SELECT 1 FROM \"" + per + "\" p WHERE p.\"ID_Kalender\" = k.\"ID\" AND p.\"Rang\" = " +
                          rang + ")" + (trigger ? "" : " ORDER BY k.\"ID\""));
            }
            return liste;
        }

        private static string Ferienspiegeltrigger(string name, string tabelle)
        {
            var spalten = new List<string>();
            for (int k = 1; k <= FERIENSPALTEN; k++)
            {
                spalten.Add("\"Ferienbeginn_" + k.ToString(CultureInfo.InvariantCulture) + "\"");
                spalten.Add("\"Ferienende_" + k.ToString(CultureInfo.InvariantCulture) + "\"");
            }
            string wenn = string.Join(" OR ", spalten.Select(s => "OLD." + s + " IS NOT NEW." + s));
            return "CREATE TRIGGER IF NOT EXISTS \"" + name + "\" AFTER UPDATE OF " + string.Join(", ", spalten) + " ON \"" + tabelle +
                   "\"\nWHEN " + wenn + "\nBEGIN\n    " + string.Join(";\n    ", Ferienspiegelanweisungen(tabelle, "NEW.")) + ";\nEND";
        }

        // =================================================================
        //  Die Zieltexte der beiden Neubauten
        // =================================================================

        private static readonly string KALENDER_GROESSE_ALT = "CHECK (\"Groesse\" IN (" + KonditionierungSchema.WERTE_GROESSE + "))";
        private static readonly string KALENDER_GROESSE_NEU =
            "CHECK (\"Groesse\" IN (" + KonditionierungSchema.WERTE_GROESSE + ",'" + ALLE + "'))";
        private const string KALENDER_ANGABE_ALT = "CHECK ((\"Wert\" IS NOT NULL) + (\"Aus\") + (\"Woche\" IS NOT NULL) = 1)";
        private static readonly string KALENDER_ANGABE_NEU =
            "CHECK (CASE WHEN \"Groesse\" = '" + ALLE + "' THEN \"Wert\" IS NULL AND \"Aus\" = 0 AND \"Woche\" IS NULL AND \"Nennwert\" IS NULL\n" +
            "        ELSE (\"Wert\" IS NOT NULL) + (\"Aus\") + (\"Woche\" IS NOT NULL) = 1 END)";

        private static readonly string PERIODE_REGEL_ALT = "\"Feiertagsregel\" IN (" + KonditionierungSchema.WERTE_FEIERTAG + ")";
        private static readonly string PERIODE_REGEL_NEU = "\"Feiertagsregel\" IN (" + Liste(DbWerte.KOND_FEIERTAGE_ALLE) + ")";
        private const string PERIODE_ANGABE_ALT =
            "CHECK ((\"Wert\" IS NOT NULL) + (\"Aus\") + (\"Woche\" IS NOT NULL) + (\"WieWochentag\" IS NOT NULL) = 1)";
        private static readonly string PERIODE_ANGABE_NEU =
            "CHECK ((\"Wert\" IS NOT NULL) + (\"Aus\") + (\"Woche\" IS NOT NULL OR \"" + SPALTE_ID_WOCHE + "\" IS NOT NULL) + " +
            "(\"WieWochentag\" IS NOT NULL) = 1\n        OR (" + OhneAngabe("") + "))";
        private const string PERIODE_SPALTE_ALT =
            "\"WieWochentag\" INTEGER CHECK (\"WieWochentag\" IS NULL OR \"WieWochentag\" BETWEEN 1 AND 7),";
        private static readonly string PERIODE_SPALTE_NEU = PERIODE_SPALTE_ALT + "\n" +
            "    \"" + SPALTE_GILT_FUER + "\" INTEGER CHECK (\"" + SPALTE_GILT_FUER + "\" IS NULL OR \"" + SPALTE_GILT_FUER +
            "\" BETWEEN 1 AND " + MASKE_ALLE.ToString(CultureInfo.InvariantCulture) + "),\n" +
            "    \"" + SPALTE_ID_WOCHE + "\" INTEGER REFERENCES \"" + TAB_WOCHE + "\" (\"ID\"),";

        /// <summary>Der Zieltext der Kalendertabelle: Größe <c>ALLE</c> erlaubt, ohne Angabe.</summary>
        public static string ZieltextKalender(string bestand)
            => Ersetzen(KonditionierungSchema.TAB_KALENDER, bestand,
                        (KALENDER_GROESSE_ALT, KALENDER_GROESSE_NEU), (KALENDER_ANGABE_ALT, KALENDER_ANGABE_NEU));

        /// <summary>Der Zieltext der Periodentabelle: Länderregeln, Maske, Wochenverweis, Ferien ohne Angabe.</summary>
        public static string ZieltextPeriode(string bestand)
            => Ersetzen(KonditionierungSchema.TAB_PERIODE, bestand,
                        (PERIODE_REGEL_ALT, PERIODE_REGEL_NEU), (PERIODE_ANGABE_ALT, PERIODE_ANGABE_NEU),
                        (PERIODE_SPALTE_ALT, PERIODE_SPALTE_NEU));

        private static string Ersetzen(string tabelle, string bestand, params (string Alt, string Neu)[] stellen)
        {
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException("Zu " + tabelle + " gibt es keinen CREATE-Text in sqlite_master.");
            string text = bestand.TrimEnd();
            if (!text.EndsWith(KonditionierungVorlagenSchema.ENDE, StringComparison.Ordinal) ||
                !text.StartsWith("CREATE TABLE \"" + tabelle + "\" (", StringComparison.Ordinal))
                throw new InvalidOperationException("Der CREATE-Text von " + tabelle + " hat eine andere Bauform als erwartet - " +
                                                    "Schemaschritt " + Nr + " baut die Tabelle NICHT um.");
            foreach ((string alt, string neu) in stellen)
            {
                int erste = text.IndexOf(alt, StringComparison.Ordinal);
                int zweite = erste < 0 ? -1 : text.IndexOf(alt, erste + alt.Length, StringComparison.Ordinal);
                if (erste < 0 || zweite >= 0)
                    throw new InvalidOperationException("Im CREATE-Text von " + tabelle + " steht die Stelle " + alt + " " +
                                                        (erste < 0 ? "nicht" : "mehr als einmal") + " - Schemaschritt " + Nr +
                                                        " baut die Tabelle NICHT um.");
                text = text.Substring(0, erste) + neu + text.Substring(erste + alt.Length);
            }
            return text;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);

        // =================================================================
        //  Stand
        // =================================================================

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return KonditionierungSchema.TAB_KALENDER;
            yield return KonditionierungSchema.TAB_PERIODE;
            yield return KonditionierungVorlagenSchema.TAB_VORLAGE;
            yield return SchemaKatalog.TAB_ZONE_STAMM;
            foreach (string t in Gebaeudetabellen) yield return t;
        }

        /// <summary>Können die Leser die neuen Tabellen und Spalten nutzen? (Wochentabelle, Spalten an Periode und Gebäude.)</summary>
        public static bool Lesbar()
        {
            if (!DataRepository.TabelleVorhanden(TAB_WOCHE)) return false;
            if (!DataRepository.SpalteVorhanden(KonditionierungSchema.TAB_PERIODE, SPALTE_GILT_FUER)) return false;
            foreach (string t in Gebaeudetabellen)
                if (!DataRepository.SpalteVorhanden(t, SPALTE_WOCHENENDTAGE) || !DataRepository.SpalteVorhanden(t, SPALTE_FEIERTAGSLAND))
                    return false;
            return true;
        }

        /// <summary>
        /// Beginnt die Spaltenfolge der Sicht <c>Abfrage_Projektgebaeude</c> mit
        /// <see cref="GebaeudeSchema.SICHT_KALENDERBEDIENUNG"/> (der zwoelfte Sichtneubau)?
        /// </summary>
        public static bool SichtSteht()
        {
            List<string> ist = GebaeudeSchema.SichtSpalten();
            string[] soll = GebaeudeSchema.SICHT_KALENDERBEDIENUNG;
            return ist.Count >= soll.Length && ist.Take(soll.Length).SequenceEqual(soll, StringComparer.Ordinal);
        }

        /// <summary>Steht der Schritt? <see cref="Lesbar"/>, die Sicht, beide Neubauten, alle Indizes und Trigger.</summary>
        public static bool Vollstaendig() => SichtSteht() && TabellenStehen();

        /// <summary>Stehen Tabellen, Spalten, Neubauten, Indizes und Trigger (alles außer der Sicht)?</summary>
        private static bool TabellenStehen()
        {
            if (!Lesbar()) return false;
            string kal = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", KonditionierungSchema.TAB_KALENDER)),
                CultureInfo.InvariantCulture) ?? "";
            string per = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", KonditionierungSchema.TAB_PERIODE)),
                CultureInfo.InvariantCulture) ?? "";
            if (!kal.Contains(KALENDER_GROESSE_NEU, StringComparison.Ordinal) || !per.Contains(PERIODE_REGEL_NEU, StringComparison.Ordinal))
                return false;
            foreach (KeyValuePair<string, string> a in Indexanweisungen.Concat(Triggeranweisungen))
            {
                object n = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type IN ('index', 'trigger') AND name = ?", new DbParam("@n", a.Key));
                if (Convert.ToInt64(n, CultureInfo.InvariantCulture) == 0) return false;
            }
            return true;
        }

        // =================================================================
        //  Ausführung
        // =================================================================

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang ohne Fremdschlüssel aus — für die Migration der Schale,
        /// <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar</b>: Steht alles, öffnet er
        /// keinen Vorgang. Benannter Abbruch bei fehlender Voraussetzung, einer Stelle des Zieltexts, die nicht genau
        /// einmal dasteht, oder einem nicht leeren <c>foreign_key_check</c>.
        /// </summary>
        /// <param name="bericht">Nimmt Zeilen auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der Änderungen am Schema (Tabelle, Neubauten, Spalten); 0, wenn alles stand.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            if (Vollstaendig())
            {
                bericht?.Add("steht bereits - nichts zu tun");
                return 0;
            }
            if (TabellenStehen())
            {
                // Nur die Sicht fehlt (ein frueherer Sichtneubau hat sie in seiner Form gebaut): die Migration der
                // Bestandsdaten laeuft NICHT ein zweites Mal - der Ferienspiegel vergaebe sonst neue Ids.
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    v.Ausfuehren(GebaeudeSchema.SQL_VIEW_DROP);
                    v.Ausfuehren(GebaeudeSchema.SQL_VIEW_KALENDERBEDIENUNG);
                    v.Commit();
                }
                bericht?.Add("Sicht " + GebaeudeSchema.VIEW + " neu gebaut (" +
                             GebaeudeSchema.SICHT_KALENDERBEDIENUNG.Length.ToString(CultureInfo.InvariantCulture) + " Spalten)");
                return 1;
            }

            int aenderungen = 0;
            var zeilen = new List<string>();
            bool sicht = !SichtSteht();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    // ---- 1. Die Wochentabelle (vor dem Neubau der Periode, die auf sie verweist).
                    if (!ObjektSteht(v, "table", TAB_WOCHE)) { aenderungen++; zeilen.Add(TAB_WOCHE + " angelegt (leer)"); }
                    v.Ausfuehren(SQL_CREATE_WOCHE);

                    // ---- 2. Die beiden Neubauten.
                    string kal = SqlVon(v, KonditionierungSchema.TAB_KALENDER);
                    if (!kal.Contains(KALENDER_GROESSE_NEU, StringComparison.Ordinal))
                    {
                        zeilen.Add(Neubau(v, KonditionierungSchema.TAB_KALENDER, ZieltextKalender(kal)) + ", Groesse " + ALLE + " erlaubt");
                        aenderungen++;
                    }
                    string per = SqlVon(v, KonditionierungSchema.TAB_PERIODE);
                    if (!per.Contains(PERIODE_REGEL_NEU, StringComparison.Ordinal))
                    {
                        zeilen.Add(Neubau(v, KonditionierungSchema.TAB_PERIODE, ZieltextPeriode(per)) + ", Laenderregeln, " +
                                   SPALTE_GILT_FUER + " und " + SPALTE_ID_WOCHE + " angelegt");
                        aenderungen++;
                    }
                    foreach (KeyValuePair<string, string> a in KonditionierungSchema.Indexanweisungen) v.Ausfuehren(a.Value);
                    foreach (KeyValuePair<string, string> a in Indexanweisungen) v.Ausfuehren(a.Value);

                    // ---- 3. Wochenende und Feiertagsland an beiden Gebaeudetabellen.
                    int spalten = 0;
                    foreach (string t in Gebaeudetabellen)
                    {
                        if (!SpalteImVorgang(v, t, SPALTE_WOCHENENDTAGE))
                        {
                            v.Ausfuehren("ALTER TABLE \"" + t + "\" ADD COLUMN \"" + SPALTE_WOCHENENDTAGE + "\" " + DEFINITION_WOCHENENDTAGE);
                            spalten++;
                        }
                        if (!SpalteImVorgang(v, t, SPALTE_FEIERTAGSLAND))
                        {
                            v.Ausfuehren("ALTER TABLE \"" + t + "\" ADD COLUMN \"" + SPALTE_FEIERTAGSLAND + "\" " + DEFINITION_FEIERTAGSLAND);
                            spalten++;
                        }
                    }
                    aenderungen += spalten;
                    zeilen.Add(spalten.ToString(CultureInfo.InvariantCulture) + " von 4 Spalte(n) " + SPALTE_WOCHENENDTAGE + "/" +
                               SPALTE_FEIERTAGSLAND + " an " + string.Join(", ", Gebaeudetabellen) + " angelegt");

                    // ---- 3a. Der zwoelfte Sichtneubau: Der Lauf liest das Gebaeude ueber Abfrage_Projektgebaeude; die
                    //          beiden Spalten stehen dort hinter der Kuehlkurve (GebaeudeSchema.SQL_VIEW_KALENDERBEDIENUNG).
                    if (sicht)
                    {
                        v.Ausfuehren(GebaeudeSchema.SQL_VIEW_DROP);
                        v.Ausfuehren(GebaeudeSchema.SQL_VIEW_KALENDERBEDIENUNG);
                        aenderungen++;
                        zeilen.Add("Sicht " + GebaeudeSchema.VIEW + " neu gebaut (" +
                                   GebaeudeSchema.SICHT_KALENDERBEDIENUNG.Length.ToString(CultureInfo.InvariantCulture) + " Spalten)");
                    }

                    // ---- 4. Die Migration: Ferienliste aus den Spalten, gekoppelte Kopien in den gemeinsamen Kalender.
                    zeilen.AddRange(Bestandsform(v));

                    // ---- 5. Die Trigger zuletzt (die Migration schreibt selbst).
                    foreach (KeyValuePair<string, string> a in Triggeranweisungen) v.Ausfuehren(a.Value);

                    Nachpruefen(v);
                    v.Commit();
                }
                finally
                {
                    try { v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* nach Commit abgeschlossen */ }
                }
            }
            if (bericht != null) foreach (string z in zeilen) bericht.Add(z);
            bericht?.Add("foreign_key_check leer; die Ferienspalten bleiben Quelle der Leser, der Referenzlauf bleibt byte-gleich");
            return aenderungen;
        }

        /// <summary>
        /// <b>Die Migration der Bestandsdaten</b> im laufenden Vorgang: die Ferienliste aus den Ferienspalten jedes
        /// Gebäudes und Katalogbaus, das einen Größenkalender führt, danach die gekoppelten Kopien aller Eigentümer in
        /// den gemeinsamen Kalender (<see cref="Kalendergemeinschaft.AlleZusammenfuehren"/>). Deterministisch und
        /// wiederholbar (ein zweiter Lauf ändert nichts); der Schritt ruft sie einmal, die Wache des Referenzprojekts
        /// 1051 ruft sie nach dem Nachbau aus dem Bauplan.
        /// </summary>
        /// <returns>Die Berichtszeilen.</returns>
        public static IReadOnlyList<string> Bestandsform(DbVorgang v)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            int ferien = 0;
            foreach (string t in Gebaeudetabellen)
            {
                // Die ersten beiden Anweisungen loeschen bzw. legen den Kalender an, die uebrigen je eine Periode.
                IReadOnlyList<string> anweisungen = Ferienspiegelanweisungen(t, null);
                for (int i = 0; i < anweisungen.Count; i++)
                {
                    int n = v.Ausfuehren(anweisungen[i]);
                    if (i >= 2) ferien += n;
                }
            }
            int gruppen = Kalendergemeinschaft.AlleZusammenfuehren(v, out int kopien);
            return new[]
            {
                ferien.ToString(CultureInfo.InvariantCulture) + " Ferienzeitraum/-raeume als Ferienliste gespiegelt",
                gruppen.ToString(CultureInfo.InvariantCulture) + " gekoppelte Periode(n) aus " +
                kopien.ToString(CultureInfo.InvariantCulture) + " Kopien in den gemeinsamen Kalender ueberfuehrt",
            };
        }

        /// <summary>
        /// Entfernt die Trigger des Schritts — für Proben, die die Tabellen der Konditionierung auf einen früheren Stand
        /// zurückbauen (SQLite prüft beim Umbau jeden Trigger gegen das Schema).
        /// </summary>
        public static void TriggerEntfernen(DbVorgang v)
        {
            foreach (KeyValuePair<string, string> a in Triggeranweisungen) v.Ausfuehren("DROP TRIGGER IF EXISTS \"" + a.Key + "\"");
        }
        /// <summary>Das Neubau-Rezept aus Schritt 96/151 (siehe <see cref="KonditionierungVorlagenSchema"/>).</summary>
        private static string Neubau(DbVorgang v, string tabelle, string ziel)
        {
            string alt = tabelle + KonditionierungVorlagenSchema.HILFSZUSATZ;
            var namen = new List<string>();
            foreach (DataRow zeile in v.Lese("SELECT name FROM pragma_table_info(?)", new DbParam("@t", tabelle)).Rows)
                namen.Add("\"" + Convert.ToString(zeile["name"], CultureInfo.InvariantCulture) + "\"");
            string spaltenliste = string.Join(", ", namen);
            var indizes = new List<string>();
            foreach (DataRow zeile in v.Lese("SELECT sql FROM sqlite_master WHERE type = 'index' AND tbl_name = ? AND sql IS NOT NULL",
                                             new DbParam("@t", tabelle)).Rows)
                indizes.Add(Convert.ToString(zeile["sql"], CultureInfo.InvariantCulture));
            object stand = v.Skalar("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@t", tabelle));
            long vorher = Zahl(v.Skalar("SELECT COUNT(*) FROM \"" + tabelle + "\""));

            v.Ausfuehren("DROP TABLE IF EXISTS \"" + alt + "\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = ON");
            v.Ausfuehren("ALTER TABLE \"" + tabelle + "\" RENAME TO \"" + alt + "\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = OFF");
            v.Ausfuehren(ziel);
            v.Ausfuehren("INSERT INTO \"" + tabelle + "\" (" + spaltenliste + ") SELECT " + spaltenliste + " FROM \"" + alt + "\"");
            v.Ausfuehren("DROP TABLE \"" + alt + "\"");
            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("@t", alt));
            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("@t", tabelle));
            if (stand != null && stand != DBNull.Value)
                v.Ausfuehren("INSERT INTO sqlite_sequence (name, seq) VALUES (?, ?)", new DbParam("@t", tabelle),
                             new DbParam("@s", Convert.ToInt64(stand, CultureInfo.InvariantCulture)));
            foreach (string anweisung in indizes) v.Ausfuehren(ProjektFremdschluessel.MitIfNotExists(anweisung));
            long nachher = Zahl(v.Skalar("SELECT COUNT(*) FROM \"" + tabelle + "\""));
            if (nachher != vorher)
                throw new InvalidOperationException("Der Neubau von " + tabelle + " traegt " + nachher.ToString(CultureInfo.InvariantCulture) +
                                                    " statt " + vorher.ToString(CultureInfo.InvariantCulture) + " Zeilen - Schemaschritt " +
                                                    Nr + " nimmt ihn zurueck.");
            return tabelle + " neu gebaut (Zeilen " + vorher.ToString(CultureInfo.InvariantCulture) + ", IDs und Zaehlerstand erhalten, " +
                   indizes.Count.ToString(CultureInfo.InvariantCulture) + " Index(e) des Bestands wieder angelegt)";
        }

        private static void Nachpruefen(DbVorgang v)
        {
            foreach (string t in new[] { KonditionierungSchema.TAB_KALENDER, KonditionierungSchema.TAB_PERIODE, TAB_WOCHE })
            {
                long verletzt = Zahl(v.Skalar("SELECT COUNT(*) FROM pragma_foreign_key_check(?)", new DbParam("@t", t)));
                if (verletzt > 0)
                    throw new InvalidOperationException("Nach dem Umbau meldet foreign_key_check fuer " + t + " " +
                                                        verletzt.ToString(CultureInfo.InvariantCulture) + " verletzte Zeile(n) - Schemaschritt " +
                                                        Nr + " nimmt alles zurueck.");
            }
            long namentlich = Zahl(v.Skalar(
                "SELECT COUNT(*) FROM pragma_foreign_key_list(?) WHERE \"table\" = ? AND \"from\" = ?",
                new DbParam("@t", KonditionierungSchema.TAB_PERIODE), new DbParam("@z", KonditionierungSchema.TAB_KALENDER),
                new DbParam("@s", KonditionierungSchema.SPALTE_ID_KALENDER)));
            if (namentlich != 1 || SqlVon(v, KonditionierungSchema.TAB_PERIODE).Contains(KonditionierungVorlagenSchema.HILFSZUSATZ + "\"",
                                                                                     StringComparison.Ordinal))
                throw new InvalidOperationException(KonditionierungSchema.TAB_PERIODE + " verweist nach dem Neubau nicht mehr namentlich auf " +
                                                    KonditionierungSchema.TAB_KALENDER + " - Schemaschritt " + Nr + " nimmt alles zurueck.");
        }

        private static string SqlVon(DbVorgang v, string tabelle)
            => Convert.ToString(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", tabelle)),
                                CultureInfo.InvariantCulture) ?? "";

        private static bool ObjektSteht(DbVorgang v, string art, string name)
            => Zahl(v.Skalar("SELECT COUNT(*) FROM sqlite_master WHERE type = ? AND name = ?",
                             new DbParam("@a", art), new DbParam("@n", name))) > 0;

        private static bool SpalteImVorgang(DbVorgang v, string tabelle, string spalte)
            => Zahl(v.Skalar("SELECT COUNT(*) FROM pragma_table_info(?) WHERE name = ?",
                             new DbParam("@t", tabelle), new DbParam("@s", spalte))) > 0;

        private static long Zahl(object wert)
            => wert == null || wert == DBNull.Value ? 0 : Convert.ToInt64(wert, CultureInfo.InvariantCulture);
    }
}
