using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KONDITIONIERUNGSPROFILE - Schemaschritt KP-S1, Stufe KP1 (Konzept
    // Konditionierungsprofile 5.1, 5.4 und 5.6; Anwenderentscheide E52 = P1..P8 und
    // E53 = P9..P13 samt Heizperiode, 26.09.2026).
    //
    // WOZU. Heute stehen Heizsollwerte, Kuehlsollwert, Luftwechsel und innere Gewinne als
    // EINZELWERTE am Gebaeude; eine Zeitstruktur gibt es nur als vier feste Sollwerte, eine
    // Nachtzeit und vier Ferienzeitraeume. Das Konzept fuehrt je Groesse einen KALENDER ein:
    // Grundangabe, Standardwoche als H8-Text und Perioden mit Datum oder Feiertagsregel. Die
    // Zellen der Vorgabe-Matrix, die keine Bestandsspalte haben, bekommen nach P10 (b) eine
    // eigene Tabelle je Eigentuemer, Groesse und Zeile.
    //
    // DREI TABELLEN (Konzept 5.1 und 5.6), die Testdatenbank wird nur einmal angefasst:
    //   - Tab_Konditionierungskalender STRICT: ein Kalender je Eigentuemer und Groesse.
    //     EIGENTUEMERREGEL als CHECK: genau EIN Gebaeude (mit oder ohne Zone), EIN Katalogbau
    //     oder EINE Vorlage; am Zonenkalender ist ID_Gebaeude das Gebaeude der Zone (die
    //     Konsistenz Zone -> Gebaeude haelt der Controller, ein Datenbankfall prueft sie -
    //     SQLite kennt keinen CHECK ueber eine zweite Tabelle). Genau EINE Angabe aus Wert,
    //     Aus = 1 und Woche.
    //   - Tab_Konditionierungsperiode STRICT: die Perioden darunter, Rang eindeutig je
    //     Kalender, Tage 1..365, Feiertagsregel aus der festen Liste; genau EINE Angabe aus
    //     Wert, Aus = 1, Woche und WieWochentag; entweder Datum ohne Regel oder Regel ohne
    //     Datum mit Art = FEIERTAG.
    //   - Tab_Konditionierungsvorgabe STRICT: die neuen Zellen der Matrix, dieselbe
    //     Eigentuemerregel, Zeile aus sechs Kennwoertern; Von/Bis sind Stunde 0..23 in der
    //     Zeile NACHT, Tag 1..365 in der Zeile SAISON und sonst NULL (CHECK je Zeile).
    //
    // ID_VORLAGE OHNE FREMDSCHLUESSEL (Festlegung der Umsetzung). Die Vorlagentabelle
    // Tab_Konditionierungsvorlage_STAMM samt Saat kommt mit der zweiten Haelfte (KP1b). Die
    // Spalte ID_Vorlage steht aber schon JETZT in der Eigentuemerregel, damit KP1b keine
    // Tabelle neu bauen muss (SQLite kann einem CHECK keine Spalte nachtragen); sie traegt
    // deshalb noch keine REFERENCES-Klausel - ein Fremdschluessel auf eine fehlende Tabelle
    // waere bei eingeschaltetem foreign_keys ein Laufzeitfehler. In KP1a schreibt kein
    // Controller die Spalte; KP1b legt die Tabelle an und traegt die Bindung im Controller
    // nach (dasselbe Muster wie die Teilindizes, Konzept 5.1, R4).
    //
    // ERGEBNISNEUTRAL. Reines DDL, KEIN DML (F5): Die Tabellen entstehen LEER, kein
    // Referenzprojekt traegt eine Zeile, und ohne angelegten Kalender nimmt der Eingang
    // woertlich den Bestandszweig (Konzept 6, „Bauvorschrift der Byte-Gleichheit"). Der
    // Referenzlauf bleibt byte-gleich, es entsteht keine neue Basis.
    // ====================================================================================

    /// <summary>
    /// <b>Die DDL der Konditionierungsprofile</b> — Schemaschritt KP-S1 (Nummer
    /// <see cref="SCHRITT"/>). EINE Quelle für Migrationsschritt,
    /// <c>Werkzeuge/Testdatenbankschema</c>, Testvorrichtung, Kopierweg und Nachweis
    /// (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class KonditionierungSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht; Migration,
        /// Werkzeug, Zielstand und Tests verweisen hierher. Vergeben unmittelbar vor dem
        /// Schemacommit gegen <c>origin</c> (Regel „lückenlos", n = Zielversion 150 + 1). Wird er
        /// umnummeriert, ändert sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = SolarkollektorTemperaturen.SCHRITT + 1;

        // =================================================================
        //  Namen — sprachneutral und EINMAL
        // =================================================================

        /// <summary>Die Kalendertabelle (Konzept 5.1).</summary>
        public const string TAB_KALENDER = SchemaKatalog.TAB_KONDITIONIERUNGSKALENDER;

        /// <summary>Die Periodentabelle (Konzept 5.1).</summary>
        public const string TAB_PERIODE = SchemaKatalog.TAB_KONDITIONIERUNGSPERIODE;

        /// <summary>Die Vorgabetabelle (Konzept 5.6, P10 (b)).</summary>
        public const string TAB_VORGABE = SchemaKatalog.TAB_KONDITIONIERUNGSVORGABE;

        /// <summary>Das Gebäude eines Kalenders oder einer Vorgabezeile (→ <c>Tab_Gebaeude.ID</c>, Kaskade).</summary>
        public const string SPALTE_ID_GEBAEUDE = "ID_Gebaeude";

        /// <summary>Die Zone (→ <c>Tab_Zone.ID</c>, Kaskade); <b>NULL heißt „kein Zonenkalender"</b>.</summary>
        public const string SPALTE_ID_ZONE = "ID_Zone";

        /// <summary>Der Katalogbau (→ <c>Tab_Gebaeude_STAMM.ID</c>, Kaskade; Entscheid P3 (b)).</summary>
        public const string SPALTE_ID_GEBAEUDE_STAMM = "ID_Gebaeude_Stamm";

        /// <summary>
        /// Die Vorlage (Entscheid P11) — <b>ohne Fremdschlüssel in KP1a</b>, weil
        /// <c>Tab_Konditionierungsvorlage_STAMM</c> erst mit KP1b entsteht; der Grund steht im
        /// Kopf der Datei. Den Fremdschlüssel setzt der Folgeschritt per Tabellenneubau
        /// (<see cref="KonditionierungVorlagenSchema"/>).
        /// </summary>
        public const string SPALTE_ID_VORLAGE = "ID_Vorlage";

        /// <summary>Die Größe eines Kalenders oder einer Vorgabezeile (<see cref="DbWerte.KOND_GROESSEN"/>).</summary>
        public const string SPALTE_GROESSE = "Groesse";

        /// <summary>Der Kalender einer Periode (→ <c>Tab_Konditionierungskalender.ID</c>, Kaskade).</summary>
        public const string SPALTE_ID_KALENDER = "ID_Kalender";

        /// <summary>Die Zeile der Matrix (<see cref="DbWerte.KOND_ZEILEN"/>).</summary>
        public const string SPALTE_ZEILE = "Zeile";

        /// <summary>Index auf dem Gebäudeverweis der Kalender — trägt die Kaskade und das Lesen je Gebäude.</summary>
        public const string INDEX_KALENDER_GEBAEUDE = "idx_KondKalender_Gebaeude";

        /// <summary>Index auf dem Zonenverweis der Kalender — trägt die Kaskade der Zone.</summary>
        public const string INDEX_KALENDER_ZONE = "idx_KondKalender_Zone";

        /// <summary>Index auf dem Katalogverweis der Kalender (P3 (b)) — trägt die Kaskade des Katalogbaus.</summary>
        public const string INDEX_KALENDER_STAMM = "idx_KondKalender_GebaeudeStamm";

        /// <summary>Index auf dem Vorlagenverweis der Kalender — das Lesen einer Vorlage (KP1b).</summary>
        public const string INDEX_KALENDER_VORLAGE = "idx_KondKalender_Vorlage";

        /// <summary>Index auf dem Kalenderverweis der Perioden — trägt die Kaskade und das Lesen in Rangfolge.</summary>
        public const string INDEX_PERIODE_KALENDER = "idx_KondPeriode_Kalender";

        /// <summary>Index auf dem Gebäudeverweis der Vorgabezeilen.</summary>
        public const string INDEX_VORGABE_GEBAEUDE = "idx_KondVorgabe_Gebaeude";

        /// <summary>Index auf dem Zonenverweis der Vorgabezeilen.</summary>
        public const string INDEX_VORGABE_ZONE = "idx_KondVorgabe_Zone";

        /// <summary>Index auf dem Katalogverweis der Vorgabezeilen.</summary>
        public const string INDEX_VORGABE_STAMM = "idx_KondVorgabe_GebaeudeStamm";

        /// <summary>Index auf dem Vorlagenverweis der Vorgabezeilen (KP1b).</summary>
        public const string INDEX_VORGABE_VORLAGE = "idx_KondVorgabe_Vorlage";

        /// <summary>Die Höchstzahl der Zeichen einer Standardwoche (Konzept 3.6, 5.2).</summary>
        public const int WOCHE_MAX_ZEICHEN = 1400;

        /// <summary>Die Höchstzahl der Zeichen einer Bemerkung (Konzept 5.1).</summary>
        public const int BEMERKUNG_MAX_ZEICHEN = 200;

        /// <summary>Die Höchstzahl der Zeichen eines Periodenbezeichners (Konzept 5.1).</summary>
        public const int BEZEICHNER_MAX_ZEICHEN = 80;

        // =================================================================
        //  Die Wertlisten als SQL-Literale — Quelle der CHECKs
        // =================================================================

        /// <summary>Die fünf Größen als SQL-Literal.</summary>
        public static readonly string WERTE_GROESSE = Liste(DbWerte.KOND_GROESSEN);

        /// <summary>Die vier Periodenarten als SQL-Literal.</summary>
        public static readonly string WERTE_ART = Liste(DbWerte.KOND_ARTEN);

        /// <summary>Die neun Feiertagsregeln als SQL-Literal (F11).</summary>
        public static readonly string WERTE_FEIERTAG = Liste(DbWerte.KOND_FEIERTAGE);

        /// <summary>Die sechs Zeilen der Matrix als SQL-Literal.</summary>
        public static readonly string WERTE_ZEILE = Liste(DbWerte.KOND_ZEILEN);

        private static string Liste(IReadOnlyList<string> werte)
            => string.Join(",", werte.Select(w => "'" + w + "'"));

        // =================================================================
        //  Die DDL
        // =================================================================

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Konditionierungskalender</c> — elf Spalten, STRICT.
        ///
        /// <para><b>Eigentümerregel:</b> genau ein Gebäude (mit oder ohne Zone), ein Katalogbau
        /// oder eine Vorlage; eine Zone nur mit Gebäude. <b>Genau eine Angabe</b> aus
        /// <c>Wert</c>, <c>Aus = 1</c> und <c>Woche</c> — ein Kalender ohne Angabe wäre eine
        /// Leerstelle, zwei Angaben eine zweite Wahrheit.</para>
        /// </summary>
        public static readonly string SQL_CREATE_KALENDER =
            "CREATE TABLE IF NOT EXISTS \"" + TAB_KALENDER + "\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Gebaeude\" INTEGER REFERENCES \"Tab_Gebaeude\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Zone\" INTEGER REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Gebaeude_Stamm\" INTEGER REFERENCES \"Tab_Gebaeude_STAMM\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Vorlage\" INTEGER,\n" +
            "    \"Groesse\" TEXT NOT NULL CHECK (\"Groesse\" IN (" + WERTE_GROESSE + ")),\n" +
            "    \"Wert\" REAL,\n" +
            "    \"Aus\" INTEGER NOT NULL DEFAULT 0 CHECK (\"Aus\" IN (0,1)),\n" +
            "    \"Woche\" TEXT CHECK (\"Woche\" IS NULL OR length(\"Woche\") <= " +
                WOCHE_MAX_ZEICHEN.ToString(CultureInfo.InvariantCulture) + "),\n" +
            "    \"Nennwert\" REAL CHECK (\"Nennwert\" IS NULL OR (\"Groesse\" IN ('" +
                DbWerte.KOND_GROESSE_GERAETE + "','" + DbWerte.KOND_GROESSE_PERSONEN +
                "') AND \"Nennwert\" >= 0)),\n" +
            "    \"Bemerkung\" TEXT CHECK (\"Bemerkung\" IS NULL OR length(\"Bemerkung\") <= " +
                BEMERKUNG_MAX_ZEICHEN.ToString(CultureInfo.InvariantCulture) + "),\n" +
            "    CHECK ((\"ID_Gebaeude\" IS NOT NULL) + (\"ID_Gebaeude_Stamm\" IS NOT NULL) + (\"ID_Vorlage\" IS NOT NULL) = 1),\n" +
            "    CHECK (\"ID_Zone\" IS NULL OR \"ID_Gebaeude\" IS NOT NULL),\n" +
            "    CHECK ((\"Wert\" IS NOT NULL) + (\"Aus\") + (\"Woche\" IS NOT NULL) = 1)\n" +
            ") STRICT";

        /// <summary>Spaltenzahl von <c>Tab_Konditionierungskalender</c> (Nachweis in den Tests).</summary>
        public const int SPALTENZAHL_KALENDER = 11;

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Konditionierungsperiode</c> — elf Spalten, STRICT.
        ///
        /// <para><b>Genau eine Angabe</b> aus <c>Wert</c>, <c>Aus = 1</c>, <c>Woche</c> und
        /// <c>WieWochentag</c>; <b>entweder Datum oder Regel</b> — <c>Beginn</c> und <c>Ende</c>
        /// in 1 … 365 ohne Feiertagsregel oder eine Regel ohne Tage mit
        /// <c>Art = 'FEIERTAG'</c>. Der Rang ist je Kalender eindeutig (<c>UNIQUE</c>).</para>
        /// </summary>
        public static readonly string SQL_CREATE_PERIODE =
            "CREATE TABLE IF NOT EXISTS \"" + TAB_PERIODE + "\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Kalender\" INTEGER NOT NULL REFERENCES \"" + TAB_KALENDER + "\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Rang\" INTEGER NOT NULL CHECK (\"Rang\" BETWEEN 1 AND 999),\n" +
            "    \"Art\" TEXT NOT NULL CHECK (\"Art\" IN (" + WERTE_ART + ")),\n" +
            "    \"Bezeichner\" TEXT NOT NULL CHECK (length(\"Bezeichner\") BETWEEN 1 AND " +
                BEZEICHNER_MAX_ZEICHEN.ToString(CultureInfo.InvariantCulture) + "),\n" +
            "    \"Beginn\" INTEGER CHECK (\"Beginn\" IS NULL OR \"Beginn\" BETWEEN 1 AND 365),\n" +
            "    \"Ende\" INTEGER CHECK (\"Ende\" IS NULL OR \"Ende\" BETWEEN 1 AND 365),\n" +
            "    \"Feiertagsregel\" TEXT CHECK (\"Feiertagsregel\" IS NULL OR \"Feiertagsregel\" IN (" +
                WERTE_FEIERTAG + ")),\n" +
            "    \"Wert\" REAL,\n" +
            "    \"Aus\" INTEGER NOT NULL DEFAULT 0 CHECK (\"Aus\" IN (0,1)),\n" +
            "    \"Woche\" TEXT CHECK (\"Woche\" IS NULL OR length(\"Woche\") <= " +
                WOCHE_MAX_ZEICHEN.ToString(CultureInfo.InvariantCulture) + "),\n" +
            "    \"WieWochentag\" INTEGER CHECK (\"WieWochentag\" IS NULL OR \"WieWochentag\" BETWEEN 1 AND 7),\n" +
            "    CHECK ((\"Wert\" IS NOT NULL) + (\"Aus\") + (\"Woche\" IS NOT NULL) + (\"WieWochentag\" IS NOT NULL) = 1),\n" +
            "    CHECK ((\"Feiertagsregel\" IS NULL AND \"Beginn\" IS NOT NULL AND \"Ende\" IS NOT NULL)\n" +
            "        OR (\"Feiertagsregel\" IS NOT NULL AND \"Beginn\" IS NULL AND \"Ende\" IS NULL AND \"Art\" = '" +
                DbWerte.KOND_ART_FEIERTAG + "')),\n" +
            "    UNIQUE (\"ID_Kalender\", \"Rang\")\n" +
            ") STRICT";

        /// <summary>Spaltenzahl von <c>Tab_Konditionierungsperiode</c> (Nachweis in den Tests).</summary>
        public const int SPALTENZAHL_PERIODE = 12;

        /// <summary>
        /// <c>CREATE TABLE IF NOT EXISTS Tab_Konditionierungsvorgabe</c> — zehn Spalten, STRICT
        /// (Konzept 5.6, P10 (b)).
        ///
        /// <para><b>Von und Bis sind zeilenabhängig:</b> Stunde 0 … 23 in der Zeile
        /// <c>NACHT</c>, Tag 1 … 365 in der Zeile <c>SAISON</c> — dort beide oder keiner (E53) —,
        /// sonst NULL. <c>Bedingt_K</c> trägt allein die Lüftung in der Nachtzeile (ΔT der
        /// Nachtauskühlung, P9); NULL heißt 2 K. <b>Jedes Feld darf leer sein</b> und heißt dann
        /// „wie die Ebene darüber" (Konzept 3.4).</para>
        /// </summary>
        public static readonly string SQL_CREATE_VORGABE =
            "CREATE TABLE IF NOT EXISTS \"" + TAB_VORGABE + "\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Gebaeude\" INTEGER REFERENCES \"Tab_Gebaeude\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Zone\" INTEGER REFERENCES \"Tab_Zone\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Gebaeude_Stamm\" INTEGER REFERENCES \"Tab_Gebaeude_STAMM\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"ID_Vorlage\" INTEGER,\n" +
            "    \"Groesse\" TEXT NOT NULL CHECK (\"Groesse\" IN (" + WERTE_GROESSE + ")),\n" +
            "    \"Zeile\" TEXT NOT NULL CHECK (\"Zeile\" IN (" + WERTE_ZEILE + ")),\n" +
            "    \"Wert\" REAL,\n" +
            "    \"Aus\" INTEGER NOT NULL DEFAULT 0 CHECK (\"Aus\" IN (0,1)),\n" +
            "    \"Von\" INTEGER,\n" +
            "    \"Bis\" INTEGER,\n" +
            "    \"Bedingt_K\" REAL CHECK (\"Bedingt_K\" IS NULL OR (\"Bedingt_K\" BETWEEN 0 AND 5\n" +
            "        AND \"Groesse\" = '" + DbWerte.KOND_GROESSE_LUEFTUNG + "' AND \"Zeile\" = '" +
                DbWerte.KOND_ZEILE_NACHT + "')),\n" +
            "    CHECK ((\"ID_Gebaeude\" IS NOT NULL) + (\"ID_Gebaeude_Stamm\" IS NOT NULL) + (\"ID_Vorlage\" IS NOT NULL) = 1),\n" +
            "    CHECK (\"ID_Zone\" IS NULL OR \"ID_Gebaeude\" IS NOT NULL),\n" +
            "    CHECK (CASE \"Zeile\"\n" +
            "        WHEN '" + DbWerte.KOND_ZEILE_NACHT + "' THEN (\"Von\" IS NULL OR \"Von\" BETWEEN 0 AND 23)\n" +
            "                                    AND (\"Bis\" IS NULL OR \"Bis\" BETWEEN 0 AND 23)\n" +
            // Der Saisonzweig ist ausdruecklich NULL-FREI gebaut: Einen CHECK, dessen Ergebnis NULL
            // ist, LAESST SQLite durch - ohne das zweite "IS NOT NULL" kaeme ein Saisonstart ohne
            // Ende hindurch, und E53 verlangt beide oder keinen.
            "        WHEN '" + DbWerte.KOND_ZEILE_SAISON + "' THEN ((\"Von\" IS NULL AND \"Bis\" IS NULL)\n" +
            "                                    OR (\"Von\" IS NOT NULL AND \"Bis\" IS NOT NULL\n" +
            "                                        AND \"Von\" BETWEEN 1 AND 365 AND \"Bis\" BETWEEN 1 AND 365))\n" +
            "        ELSE \"Von\" IS NULL AND \"Bis\" IS NULL END)\n" +
            ") STRICT";

        /// <summary>Spaltenzahl von <c>Tab_Konditionierungsvorgabe</c> (Nachweis in den Tests).</summary>
        public const int SPALTENZAHL_VORGABE = 12;

        /// <summary>Die drei Tabellen des Schritts in Anlegereihenfolge, je Tabellenname.</summary>
        public static IEnumerable<KeyValuePair<string, string>> Tabellenanweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(TAB_KALENDER, SQL_CREATE_KALENDER);
                yield return new KeyValuePair<string, string>(TAB_PERIODE, SQL_CREATE_PERIODE);
                yield return new KeyValuePair<string, string>(TAB_VORGABE, SQL_CREATE_VORGABE);
            }
        }

        /// <summary>
        /// Die neun Indizes des Schritts, je Indexname — nach den Tabellen anzulegen. Es sind die
        /// Verweise, an denen die Löschkaskaden hängen und über die Controller und Lauf lesen;
        /// die <b>Teilindizes der Eindeutigkeit</b> (ein Kalender je Eigentümerart und Größe)
        /// kommen erst nach der Werkzeugprobe (Konzept 5.1, R4) mit dem Folgeschritt
        /// (<see cref="KonditionierungVorlagenSchema.Teilindizes"/>).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Indexanweisungen
        {
            get
            {
                yield return Index(INDEX_KALENDER_GEBAEUDE, TAB_KALENDER, SPALTE_ID_GEBAEUDE);
                yield return Index(INDEX_KALENDER_ZONE, TAB_KALENDER, SPALTE_ID_ZONE);
                yield return Index(INDEX_KALENDER_STAMM, TAB_KALENDER, SPALTE_ID_GEBAEUDE_STAMM);
                yield return Index(INDEX_KALENDER_VORLAGE, TAB_KALENDER, SPALTE_ID_VORLAGE);
                yield return Index(INDEX_PERIODE_KALENDER, TAB_PERIODE, SPALTE_ID_KALENDER);
                yield return Index(INDEX_VORGABE_GEBAEUDE, TAB_VORGABE, SPALTE_ID_GEBAEUDE);
                yield return Index(INDEX_VORGABE_ZONE, TAB_VORGABE, SPALTE_ID_ZONE);
                yield return Index(INDEX_VORGABE_STAMM, TAB_VORGABE, SPALTE_ID_GEBAEUDE_STAMM);
                yield return Index(INDEX_VORGABE_VORLAGE, TAB_VORGABE, SPALTE_ID_VORLAGE);
            }
        }

        private static KeyValuePair<string, string> Index(string name, string tabelle, string spalte)
            => new KeyValuePair<string, string>(name,
                   "CREATE INDEX IF NOT EXISTS \"" + name + "\" ON \"" + tabelle + "\" (\"" + spalte + "\")");

        // =================================================================
        //  Stand und Ausführung
        // =================================================================

        /// <summary>
        /// Kann der Datenweg den Schritt nutzen? Alle drei Tabellen stehen — die Probe der
        /// Controller und des Eingangs; die Indizes ändern kein Lesen und Schreiben.
        /// </summary>
        public static bool Lesbar()
        {
            foreach (KeyValuePair<string, string> a in Tabellenanweisungen)
                if (!DataRepository.TabelleVorhanden(a.Key)) return false;
            return true;
        }

        /// <summary>Steht der Schritt? Alle drei Tabellen und alle neun Indizes.</summary>
        public static bool Vollstaendig()
        {
            if (!Lesbar()) return false;
            foreach (KeyValuePair<string, string> a in Indexanweisungen)
            {
                object n = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?", new DbParam("@i", a.Key));
                if (n == null || System.Convert.ToInt64(n, CultureInfo.InvariantCulture) == 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang aus — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen
        /// Helfer. <b>Wiederholbar</b> (<c>IF NOT EXISTS</c>), <b>kein DML</b>. Ohne
        /// <c>Tab_Gebaeude</c>, <c>Tab_Gebaeude_STAMM</c> oder <c>Tab_Zone</c> tut er nichts und
        /// sagt es.
        /// </summary>
        /// <param name="bericht">Nimmt Zeilen auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Tabellen (höchstens drei).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            // Die Auskunft VOR dem Vorgang - TabelleVorhanden arbeitet auf einer eigenen
            // Verbindung und saehe die offene Transaktion nicht.
            foreach (string t in new[] { "Tab_Gebaeude", "Tab_Gebaeude_STAMM", SchemaKatalog.TAB_ZONE })
                if (!DataRepository.TabelleVorhanden(t))
                {
                    bericht?.Add("Die Tabelle " + t + " fehlt - nichts angelegt");
                    return 0;
                }

            List<KeyValuePair<string, string>> fehlend = Tabellenanweisungen
                .Where(a => !DataRepository.TabelleVorhanden(a.Key)).ToList();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (KeyValuePair<string, string> a in Tabellenanweisungen) v.Ausfuehren(a.Value);
                    foreach (KeyValuePair<string, string> a in Indexanweisungen) v.Ausfuehren(a.Value);
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }

            bericht?.Add(fehlend.Count.ToString(CultureInfo.InvariantCulture) + " von 3 Tabelle(n) angelegt (" +
                         TAB_KALENDER + ", " + TAB_PERIODE + ", " + TAB_VORGABE + "), neun Indizes. KEIN DML - " +
                         "die Tabellen entstehen leer, der Referenzlauf bleibt byte-gleich");
            return fehlend.Count;
        }
    }
}
