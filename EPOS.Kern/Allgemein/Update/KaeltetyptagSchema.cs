using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KBP-4a - SCHEMASCHRITT B DER KAELTEBEDARFSPROFILE: TYPTAGE UND ZUORDNUNGSMATRIX
    // (Konzept Kaeltebedarfsprofile 3.4, 8.2; Entscheid OE-1).
    //
    // WAS.
    //   Tab_Kaeltetyptag         ID, ID_Projekt (FK Tab_Projekt, CASCADE), ID_Kaeltebedarf (FK auf die Projektkopie
    //                            Tab_Kaeltebedarf, CASCADE), Nr 1 ... 6 (eindeutig je Projektkopie), Bezeichner,
    //                            "1" ... "24" (Stundenwerte, relative Form, >= 0), Tagesmenge (relatives Gewicht, >= 0,
    //                            Vorgabe 1). STRICT.
    //   Tab_Kaeltetyptag_Matrix  ID, ID_Kaeltebedarf (FK, CASCADE), Monat 1 ... 12, Tagesart werktag/samstag/sonntag,
    //                            ID_Kaeltetyptag (FK, CASCADE); eindeutig je (ID_Kaeltebedarf, Monat, Tagesart). STRICT.
    //   Kein _STAMM: Vorlagen bleiben Monate x Woche, Typtage sind eine Projekteingabe.
    //
    // ERGEBNISNEUTRAL. Zwei neue, leere Tabellen; kein UPDATE, keine Saat. Gerechnet wird nur fuer Zuordnungen mit
    // Eingabeart 'typtag' (Typtagschicht in ProfilBedarf) - keine Bestandszuordnung traegt sie, der Referenzlauf bleibt
    // byte-gleich. Wiederholbar.
    //
    // DREI LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema und die Testvorrichtung in EPOS.Kern.Tests;
    // dazu KaeltetyptagCtrl und ProfilBedarf nach Vorhandensein (TabellenVorhanden).
    // ====================================================================================

    /// <summary>
    /// <b>KBP-4a</b> — die Typtage der Kältebedarfszuordnung und ihre Zuordnungsmatrix 12 Monate × 3 Tagesarten
    /// (Konzept Kältebedarfsprofile 3.4 und 8.2). EINE Quelle für Migration, Werkzeug, Testkopie und Nachweis
    /// (ADR-001 Option C).
    /// </summary>
    public static class KaeltetyptagSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht.</summary>
        // KETTE: geparkt auf 236 - 234 BedarfsprofilEingabeartSchema (KBP-5a, selbst geparkt) und 235
        // WasserpreisTraegerkarteSchema (Sitzung Kostenbasis, KB-7 Wasserkosten) liegen noch nicht in der Kette. Sobald 235
        // auf origin liegt, umhaengen auf "WasserpreisTraegerkarteSchema.SCHRITT + 1" und im selben Schritt eintragen:
        // SchemaMigration.SCHRITTE_SQLITE, Paketanhebung.STUFEN, SchemaStand.Zielversion; danach die Testdatenbank heben.
        public const int SCHRITT = 236;

        /// <summary>Die Typtage (Projekteingabe an der Projektkopie des Kältebedarfs).</summary>
        public const string TAB_TYPTAG = "Tab_Kaeltetyptag";

        /// <summary>Die Zuordnungsmatrix Monat × Tagesart → Typtag.</summary>
        public const string TAB_MATRIX = "Tab_Kaeltetyptag_Matrix";

        /// <summary>Die Projektkopie des Kältekopfs, an der beide Tabellen hängen.</summary>
        public const string TAB_KOPF = KaeltebedarfSchema.TAB_KOPF;

        /// <summary>Der Verweis auf die Projektkopie.</summary>
        public const string SPALTE_ID_KAELTEBEDARF = KaeltebedarfSchema.SPALTE_ID_KAELTEBEDARF;

        /// <summary>Der Verweis der Matrixzelle auf ihren Typtag.</summary>
        public const string SPALTE_ID_TYPTAG = "ID_Kaeltetyptag";

        /// <summary>Die Nummer des Typtags (1 … 6) — sein Name in der Matrix des Editors.</summary>
        public const string SPALTE_NR = "Nr";

        /// <summary>Die Tagesmenge (relatives Gewicht des Typtags).</summary>
        public const string SPALTE_TAGESMENGE = "Tagesmenge";

        /// <summary>Der Monat der Matrixzelle (1 … 12).</summary>
        public const string SPALTE_MONAT = "Monat";

        /// <summary>Die Tagesart der Matrixzelle.</summary>
        public const string SPALTE_TAGESART = "Tagesart";

        /// <summary>Größte Zahl der Typtage je Projektkopie.</summary>
        public const int NR_MAX = 6;

        /// <summary>Stunden eines Typtags.</summary>
        public const int STUNDEN = 24;

        /// <summary>Monate der Matrix.</summary>
        public const int MONATE = 12;

        /// <summary>Tagesart Werktag (Montag bis Freitag; Ferientage rechnen als Werktag mit Ferienfaktor).</summary>
        public const string TAGESART_WERKTAG = "werktag";

        /// <summary>Tagesart Samstag.</summary>
        public const string TAGESART_SAMSTAG = "samstag";

        /// <summary>Tagesart Sonntag und Feiertag.</summary>
        public const string TAGESART_SONNTAG = "sonntag";

        /// <summary>Die drei Tagesarten in Matrixfolge (Index 0 … 2).</summary>
        public static readonly IReadOnlyList<string> TAGESARTEN = new[] { TAGESART_WERKTAG, TAGESART_SAMSTAG, TAGESART_SONNTAG };

        private static string Q(string s) => "\"" + s + "\"";

        /// <summary>Der Spaltenname der Stunde <paramref name="h"/> (1 … 24).</summary>
        public static string Stunde(int h) => h.ToString(CultureInfo.InvariantCulture);

        private const string PROJEKT_FK =
            "    \"ID_Projekt\" INTEGER NOT NULL REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n";

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Kaeltetyptag</c>.</summary>
        public static string SqlTyptag()
        {
            var sb = new StringBuilder();
            sb.Append("CREATE TABLE IF NOT EXISTS " + Q(TAB_TYPTAG) + " (\n");
            sb.Append("    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n");
            sb.Append(PROJEKT_FK);
            sb.Append("    " + Q(SPALTE_ID_KAELTEBEDARF) + " INTEGER NOT NULL REFERENCES " + Q(TAB_KOPF) +
                      " (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n");
            sb.Append("    " + Q(SPALTE_NR) + " INTEGER NOT NULL CHECK (" + Q(SPALTE_NR) + " >= 1 AND " + Q(SPALTE_NR) + " <= " +
                      NR_MAX.ToString(CultureInfo.InvariantCulture) + "),\n");
            sb.Append("    \"Bezeichner\" TEXT NOT NULL,\n");
            for (int h = 1; h <= STUNDEN; h++)
                sb.Append("    " + Q(Stunde(h)) + " REAL NOT NULL DEFAULT 0 CHECK (" + Q(Stunde(h)) + " >= 0),\n");
            sb.Append("    " + Q(SPALTE_TAGESMENGE) + " REAL NOT NULL DEFAULT 1 CHECK (" + Q(SPALTE_TAGESMENGE) + " >= 0),\n");
            sb.Append("    UNIQUE (" + Q(SPALTE_ID_KAELTEBEDARF) + ", " + Q(SPALTE_NR) + ")\n");
            sb.Append(") STRICT");
            return sb.ToString();
        }

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Kaeltetyptag_Matrix</c>.</summary>
        public static string SqlMatrix() =>
            "CREATE TABLE IF NOT EXISTS " + Q(TAB_MATRIX) + " (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    " + Q(SPALTE_ID_KAELTEBEDARF) + " INTEGER NOT NULL REFERENCES " + Q(TAB_KOPF) +
            " (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n" +
            "    " + Q(SPALTE_MONAT) + " INTEGER NOT NULL CHECK (" + Q(SPALTE_MONAT) + " >= 1 AND " + Q(SPALTE_MONAT) + " <= 12),\n" +
            "    " + Q(SPALTE_TAGESART) + " TEXT NOT NULL CHECK (" + Q(SPALTE_TAGESART) + " IN ('" + TAGESART_WERKTAG + "','" +
            TAGESART_SAMSTAG + "','" + TAGESART_SONNTAG + "')),\n" +
            "    " + Q(SPALTE_ID_TYPTAG) + " INTEGER NOT NULL REFERENCES " + Q(TAB_TYPTAG) +
            " (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE,\n" +
            "    UNIQUE (" + Q(SPALTE_ID_KAELTEBEDARF) + ", " + Q(SPALTE_MONAT) + ", " + Q(SPALTE_TAGESART) + ")\n" +
            ") STRICT";

        /// <summary>Die zwei Tabellen in Anlagefolge (Name, Anweisung).</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Tabellen() => new[]
        {
            new KeyValuePair<string, string>(TAB_TYPTAG, SqlTyptag()),
            new KeyValuePair<string, string>(TAB_MATRIX, SqlMatrix()),
        };

        /// <summary>Die Indizes der Fremdschlüssel (Name, Anweisung; <c>IF NOT EXISTS</c>).</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> Indizes() => new[]
        {
            Index("IX_" + TAB_TYPTAG + "_ID_Projekt", TAB_TYPTAG, "\"ID_Projekt\""),
            Index("IX_" + TAB_MATRIX + "_" + SPALTE_ID_TYPTAG, TAB_MATRIX, Q(SPALTE_ID_TYPTAG)),
        };

        private static KeyValuePair<string, string> Index(string name, string tabelle, string spalten) =>
            new KeyValuePair<string, string>(name, "CREATE INDEX IF NOT EXISTS " + Q(name) + " ON " + Q(tabelle) + " (" + spalten + ")");

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { "Tab_Projekt", TAB_KOPF };

        /// <summary>Stehen beide Tabellen? (Leserwächter für Controller und Rechenweg.)</summary>
        public static bool TabellenVorhanden() =>
            DataRepository.TabelleVorhanden(TAB_TYPTAG) && DataRepository.TabelleVorhanden(TAB_MATRIX);

        /// <summary>Stehen Tabellen und Indizes?</summary>
        public static bool SchemaVollstaendig() => TabellenVorhanden() && Indizes().All(i => IndexVorhanden(i.Key));

        /// <summary>Der Schritt ist vollständig, wenn sein Schema steht (keine Saat).</summary>
        public static bool Vollstaendig() => SchemaVollstaendig();

        private static bool IndexVorhanden(string name)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = ?", new DbParam("@n", name));
            return o != null && o != DBNull.Value && Convert.ToInt32(o, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>Der Matrixindex (0 … 2) einer Tagesart; −1 bei unbekanntem Wert.</summary>
        public static int TagesartIndex(string tagesart)
        {
            for (int i = 0; i < TAGESARTEN.Count; i++)
                if (string.Equals(TAGESARTEN[i], tagesart, StringComparison.Ordinal)) return i;
            return -1;
        }

        /// <summary>
        /// Führt den Schritt aus (wiederholbar): Tabellen und Indizes in EINEM Vorgang. Rückgabe = Zahl der
        /// Schemaanweisungen. Fehlt eine Voraussetzung, wirft er benannt.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");

            // Die Auskunft VOR dem Vorgang - TabelleVorhanden arbeitet auf einer eigenen Verbindung.
            var anweisungen = new List<KeyValuePair<string, string>>();
            foreach (KeyValuePair<string, string> t in Tabellen())
                if (!DataRepository.TabelleVorhanden(t.Key))
                    anweisungen.Add(new KeyValuePair<string, string>(t.Key + " angelegt", t.Value));
            foreach (KeyValuePair<string, string> i in Indizes())
                if (!IndexVorhanden(i.Key))
                    anweisungen.Add(new KeyValuePair<string, string>("Index " + i.Key + " angelegt", i.Value));

            if (anweisungen.Count > 0)
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        foreach (KeyValuePair<string, string> a in anweisungen) v.Ausfuehren(a.Value);
                        v.Commit();
                    }
                    catch
                    {
                        v.Rollback();
                        throw;
                    }
                }
                if (bericht != null)
                    foreach (KeyValuePair<string, string> a in anweisungen) bericht.Add(a.Key);
            }
            else
            {
                bericht?.Add("Typtage und Matrix stehen bereits");
            }
            bericht?.Add("KEIN DML; nur Zuordnungen mit Eingabeart '" + KaeltebedarfsprofilVorlagenSchema.EINGABE_TYPTAG +
                         "' lesen die Typtage, der Referenzlauf bleibt byte-gleich");
            return anweisungen.Count;
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
