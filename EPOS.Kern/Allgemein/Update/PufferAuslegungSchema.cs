using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // PUFFERSPEICHER-AUSLEGUNG - Schemaschritt der Stufe P1, Welle W1 (Konzept
    // Pufferspeicher-Auslegung, Abschnitt 4.1 und 4.2).
    //
    // ZWEI TABELLEN UND EINE SAAT IN EINEM SCHRITT:
    //
    // (1) Tab_PufferAuslegung (STRICT, AUTOINCREMENT): die Eingaben und das letzte Ergebnis
    //     einer Auslegung je Projektpuffer; ID_Projekt mit ON DELETE CASCADE, ID_Pufferspeicher
    //     mit ON DELETE SET NULL (NULL = "neu anlegen"). Jede Eingabespalte ist nullbar:
    //     NULL = Vorgabe. Boolean-Spalten 0/1 mit CHECK.
    //
    // (2) Tab_PufferAuslegungParameter_STAMM (STRICT): die Vorgabewerte (Schluessel UNIQUE,
    //     Wert, Einheit, Quelle, Herkunftsart, ReadOnly).
    //
    // (3) Die Saat: INSERT ... WHERE NOT EXISTS aller Zeilen aus PufferAuslegungVorgaben.EINTRAEGE - der
    //     EINEN Quelle der Zahlen, aus der auch der Rueckfall von PufferAuslegungParameter
    //     liest. Eine gepflegte Zeile bleibt, wie sie ist.
    //
    // ERGEBNISNEUTRAL: Die Auslegung rechnet nur auf Zuruf und schreibt nur auf Zuruf; keine
    // Bestandszeile wird angefasst, der Referenzlauf bleibt byte-gleich.
    //
    // VIER LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema, die Testvorrichtung samt
    // Nachweis in EPOS.Kern.Tests und die Paketanhebung (Art Ddl, die Saat laeuft im Schritt).
    // ====================================================================================

    /// <summary>
    /// Die Tabellen der Pufferspeicher-Auslegung und die Saat ihrer Vorgabewerte — EINE Quelle für
    /// Migration, Werkzeug, Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im
    /// Kopf der Datei.
    /// </summary>
    public static class PufferAuslegungSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste
        /// Schritt hinter der Einspeisegrenze und Selbstentladung (<see cref="StromViertelstundenSchema"/>).
        /// </summary>
        public const int SCHRITT = StromViertelstundenSchema.SCHRITT + 1;

        /// <summary>Die Auslegungstabelle (eine Zeile je Projektpuffer bzw. „neu anlegen“).</summary>
        public const string TAB = "Tab_PufferAuslegung";

        /// <summary>Die Vorgabetabelle.</summary>
        public const string TAB_PARAMETER = "Tab_PufferAuslegungParameter_STAMM";

        /// <summary>Die Werteliste der Nutzungsprofile.</summary>
        public static readonly IReadOnlyList<string> NUTZUNGSPROFILE = new[]
        {
            "WOHNEN", "BEHERBERGUNG", "PFLEGE", "BUERO_SCHULE", "GEWERBE"
        };

        /// <summary>Die Werteliste der Sperrprofile.</summary>
        public static readonly IReadOnlyList<string> SPERRPROFILE = new[]
        {
            "KEINE", "ZWEI_MAL_ZWEI", "DREI_MAL_ZWEI", "EIGEN"
        };

        /// <summary>Die Werteliste der Zirkulationswege.</summary>
        public static readonly IReadOnlyList<string> ZIRKULATIONSWEGE = new[]
        {
            "ZAPFPROFIL", "PROJEKT", "ANTEIL", "JE_WE"
        };

        /// <summary>Die Spalten der Auslegungstabelle hinter ID, ID_Projekt und ID_Pufferspeicher, samt Typ.</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> SPALTEN = Spalten();

        private static string Liste(IEnumerable<string> werte) => string.Join(",", werte.Select(w => "'" + w + "'"));

        private static string Bool(string s, bool pflicht) => pflicht
            ? "INTEGER NOT NULL DEFAULT 0 CHECK (\"" + s + "\" IN (0,1))"
            : "INTEGER CHECK (\"" + s + "\" IS NULL OR \"" + s + "\" IN (0,1))";

        private static string Real(string s, string bedingung) =>
            "REAL CHECK (\"" + s + "\" IS NULL OR (" + bedingung.Replace("x", "\"" + s + "\"") + "))";

        private static string Text(string s, IEnumerable<string> werte) =>
            "TEXT CHECK (\"" + s + "\" IS NULL OR \"" + s + "\" IN (" + Liste(werte) + "))";

        private static List<KeyValuePair<string, string>> Spalten()
        {
            var l = new List<KeyValuePair<string, string>>();
            void A(string n, string t) => l.Add(new KeyValuePair<string, string>(n, t));
            A("Klasse_Heizung", Bool("Klasse_Heizung", true));
            A("Klasse_Brauchwasser", Bool("Klasse_Brauchwasser", true));
            A("Klasse_Prozess", Bool("Klasse_Prozess", true));
            A("Nutzungsprofil", Text("Nutzungsprofil", NUTZUNGSPROFILE));
            A("Vorlage", Text("Vorlage", PufferAuslegungVorgaben.VORLAGEN));
            A("WP_Geregelt", Bool("WP_Geregelt", false));
            A("Zweiterzeuger_Frei", Bool("Zweiterzeuger_Frei", false));
            A("Mindestlaufzeit_min", Real("Mindestlaufzeit_min", "x > 0"));
            A("Mindestleistung_kW", Real("Mindestleistung_kW", "x >= 0"));
            A("Anlagenvolumen_l", Real("Anlagenvolumen_l", "x >= 0"));
            A("Sperrprofil", Text("Sperrprofil", SPERRPROFILE));
            A("Sperrdauer_h", Real("Sperrdauer_h", "x >= 0 AND x <= 24"));
            A("Sperrbeginn_h", Real("Sperrbeginn_h", "x >= 0 AND x <= 24"));
            A("Startziel_je_Tag", Real("Startziel_je_Tag", "x > 0"));
            A("Deckungsziel", Real("Deckungsziel", "x >= 0 AND x <= 1"));
            A("DeltaT_B_K", Real("DeltaT_B_K", "x > 0"));
            A("T_Puffer_Oben_C", "REAL");
            A("Zirkulation_Weg", Text("Zirkulation_Weg", ZIRKULATIONSWEGE));
            A("BHKW_Verschiebedauer_h", Real("BHKW_Verschiebedauer_h", "x >= 0"));
            A("Volumen_H_l", "REAL");
            A("Volumen_B_l", "REAL");
            A("Volumen_P_l", "REAL");
            A("Volumen_Empfehlung_l", "REAL");
            A("Bemessend", "TEXT");
            A("Berechnet_am", "TEXT");
            return l;
        }

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_PufferAuslegung</c> — STRICT, AUTOINCREMENT.</summary>
        public static string SqlCreateAuslegung()
        {
            var s = new System.Text.StringBuilder();
            s.Append("CREATE TABLE IF NOT EXISTS \"" + TAB + "\" (\n");
            s.Append("    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n");
            s.Append("    \"ID_Projekt\" INTEGER NOT NULL REFERENCES \"" + SchemaKatalog.TAB_PROJEKT +
                     "\" (\"ID\") ON DELETE CASCADE,\n");
            s.Append("    \"ID_Pufferspeicher\" INTEGER REFERENCES \"" + SchemaKatalog.TAB_PUFFERSPEICHER +
                     "\" (\"ID\") ON DELETE SET NULL");
            foreach (KeyValuePair<string, string> sp in SPALTEN)
                s.Append(",\n    \"" + sp.Key + "\" " + sp.Value);
            s.Append("\n) STRICT");
            return s.ToString();
        }

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_PufferAuslegungParameter_STAMM</c> — STRICT.</summary>
        public static string SqlCreateParameter()
        {
            return "CREATE TABLE IF NOT EXISTS \"" + TAB_PARAMETER + "\" (\n" +
                   "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                   "    \"Schluessel\" TEXT NOT NULL UNIQUE,\n" +
                   "    \"Wert\" REAL NOT NULL,\n" +
                   "    \"Einheit\" TEXT,\n" +
                   "    \"Quelle\" TEXT NOT NULL,\n" +
                   "    \"Herkunftsart\" TEXT CHECK (\"Herkunftsart\" IS NULL OR \"Herkunftsart\" IN (" +
                   Liste(PufferHerkunftsart.ALLE) + ")),\n" +
                   "    \"ReadOnly\" INTEGER NOT NULL DEFAULT 1 CHECK (\"ReadOnly\" IN (0,1))\n" +
                   ") STRICT";
        }

        /// <summary>
        /// Die Saatanweisung — je Vorgabe einmal, mit Parametern; eine stehende Zeile bleibt. Sechs Parameter:
        /// Schlüssel, Wert, Einheit, Quelle, Herkunftsart, dann noch einmal der Schlüssel für die Bedingung.
        /// </summary>
        // Ohne INSERT OR IGNORE: SQLite zählt den AUTOINCREMENT-Stand auch bei ignoriertem Einfügen hoch;
        // NOT EXISTS über den UNIQUE-Schlüssel (Schluessel) lässt ihn stehen.
        public const string SQL_SAAT =
            "INSERT INTO " + TAB_PARAMETER + " (Schluessel, Wert, Einheit, Quelle, Herkunftsart, ReadOnly) " +
            "SELECT ?, ?, ?, ?, ?, 1 WHERE NOT EXISTS (SELECT 1 FROM " + TAB_PARAMETER + " WHERE Schluessel = ?)";

        /// <summary>Die Schlüssel der Vorgabetabelle.</summary>
        public const string SQL_SCHLUESSEL = "SELECT Schluessel FROM " + TAB_PARAMETER;

        /// <summary>Die Schlüssel, die in der Vorgabetabelle stehen; leer, wenn sie fehlt.</summary>
        public static HashSet<string> GesaeteSchluessel()
        {
            var h = new HashSet<string>(StringComparer.Ordinal);
            if (!DataRepository.TabelleVorhanden(TAB_PARAMETER)) return h;
            DataTable t = DataRepository.GetDataTable(SQL_SCHLUESSEL);
            if (t == null) return h;
            foreach (DataRow r in t.Rows)
                if (r[0] is string s) h.Add(s);
            return h;
        }

        /// <summary>Wie viele Vorgaben fehlen in der Vorgabetabelle?</summary>
        public static int SaatOffen()
        {
            HashSet<string> da = GesaeteSchluessel();
            return PufferAuslegungVorgaben.EINTRAEGE.Count(v => !da.Contains(v.Schluessel));
        }

        /// <summary>Steht alles — beide Tabellen und jede Saatzeile?</summary>
        public static bool Vollstaendig()
        {
            if (!DataRepository.TabelleVorhanden(TAB)) return false;
            if (!DataRepository.TabelleVorhanden(TAB_PARAMETER)) return false;
            return SaatOffen() == 0;
        }

        /// <summary>
        /// Die DDL-Anweisungen des Schritts — Beschreibung und SQL, je fehlende Tabelle eine; leer,
        /// wenn beide stehen (<b>wiederholbar</b>). Die Saat folgt in <see cref="Ausfuehren"/>.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                if (!DataRepository.TabelleVorhanden(TAB))
                    yield return new KeyValuePair<string, string>(TAB + " anlegen", SqlCreateAuslegung());
                if (!DataRepository.TabelleVorhanden(TAB_PARAMETER))
                    yield return new KeyValuePair<string, string>(TAB_PARAMETER + " anlegen", SqlCreateParameter());
            }
        }

        /// <summary>
        /// Führt den Schritt aus: fehlende Tabellen anlegen, dann die Saat (INSERT … WHERE NOT EXISTS) in
        /// EINEM Vorgang. Für <c>SchemaMigration</c>, <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der Handgriffe: angelegte Tabellen plus eingefügte Saatzeilen (0, wenn alles stand).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            // Die Auskunft VOR dem Vorgang - TabelleVorhanden arbeitet auf einer eigenen Verbindung.
            List<KeyValuePair<string, string>> ddl = Anweisungen.ToList();
            HashSet<string> da = ddl.Count > 0 ? new HashSet<string>(StringComparer.Ordinal) : GesaeteSchluessel();
            List<PufferVorgabe> offen = PufferAuslegungVorgaben.EINTRAEGE.Where(v => !da.Contains(v.Schluessel)).ToList();
            if (ddl.Count == 0 && offen.Count == 0)
            {
                bericht?.Add(TAB + " und " + TAB_PARAMETER + " samt Saat vorhanden");
                return 0;
            }

            int eingefuegt = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                foreach (KeyValuePair<string, string> a in ddl)
                {
                    v.Ausfuehren(a.Value);
                    bericht?.Add(a.Key);
                }
                foreach (PufferVorgabe p in offen)
                    eingefuegt += v.Ausfuehren(SQL_SAAT,
                        new DbParam("@s", p.Schluessel),
                        new DbParam("@w", p.Wert),
                        new DbParam("@e", (object)p.Einheit ?? DBNull.Value),
                        new DbParam("@q", p.Quelle),
                        new DbParam("@h", p.Herkunftsart),
                        new DbParam("@s2", p.Schluessel));
                v.Commit();
            }
            if (eingefuegt > 0)
                bericht?.Add(TAB_PARAMETER + ": " + eingefuegt.ToString(CultureInfo.InvariantCulture) + " Vorgabe(n) gesät");
            return ddl.Count + eingefuegt;
        }
    }
}
