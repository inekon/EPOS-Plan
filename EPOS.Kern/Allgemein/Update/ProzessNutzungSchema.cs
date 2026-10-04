using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // NUTZUNGSPROFIL ÜBER IDs UND NUTZUNGSARTEN BÜRO, SCHULE, GEWERBE - V31/V32 der Recherche
    // Pufferoptimierung (Anwenderauftrag 03.10.2026, Entscheid E-P25).
    //
    // WAS DER SCHRITT ANLEGT
    //   (1) Tab_Nutzungsprofil_STAMM (STRICT): die fünf Nutzungsprofile der Pufferauslegung mit der
    //       Kennung (WOHNEN, BEHERBERGUNG, PFLEGE, BUERO_SCHULE, GEWERBE), ReadOnly 1.
    //   (2) Z_Nutzungsprofil (STRICT): Schlüssel einer Quelle (ZAPF = Bezeichner der Zapf-Nutzungsart,
    //       KONDITIONIERUNG = Nutzung der Konditionierungsvorlage, GEBAEUDEART = Gebäudeart) → ID des
    //       Profils; eindeutig je (Quelle, Schluessel). Die Pufferauslegung leitet ihr Profil darüber ab
    //       (NutzungsprofilZuordnung.Lesen), nicht mehr über Teiltexte.
    //
    // DIE SAAT (INSERT OR IGNORE, wiederholbar): die Profile und die Zuordnung aus
    // NutzungsprofilZuordnung.VORGABE - der EINEN Quelle im Code -, dazu die drei Zapf-Nutzungsarten
    // Büro, Schule, Gewerbe des freien Paketteils in einen Katalog, der schon eine Katalogversion führt
    // (TwwPaketteilCtrl.NutzungsartenNachtragen; das Nachladen lädt nur ohne Katalogversion). Eine
    // vorhandene Zeile bleibt; keine Zeile eines Projekts und keine bestehende Katalogzeile ändert sich.
    //
    // ERGEBNISNEUTRAL. Die Simulation liest keine der Tabellen; neue Nutzungsarten benutzt kein
    // Referenzprojekt. Der Referenzlauf bleibt byte-gleich.
    //
    // VIER LESER: die Schalenmigration, Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl).
    // ====================================================================================

    /// <summary>
    /// Der Schemaschritt Nutzungsprofil-Zuordnung und Zapf-Nutzungsarten Büro/Schule/Gewerbe — EINE Quelle
    /// für Migration, Werkzeug, Testkopie und Nachweis. Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ProzessNutzungSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht.</summary>
        public const int SCHRITT = WaermepumpeSperrprofilSchema.SCHRITT + 1;

        /// <summary>Der Katalog der Nutzungsprofile.</summary>
        public const string TAB_PROFIL = "Tab_Nutzungsprofil_STAMM";

        /// <summary>Die Zuordnung Schlüssel → Nutzungsprofil.</summary>
        public const string TAB_ZUORDNUNG = "Z_Nutzungsprofil";

        /// <summary>Die Zapf-Nutzungsarten, die der Schritt in einen versionierten Katalog nachträgt (V31).</summary>
        public static readonly IReadOnlyList<string> NUTZUNGSARTEN = new[]
        {
            "Büro (Setzung)", "Schule (Setzung)", "Gewerbe Schichtbetrieb (Setzung)"
        };

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return "Tab_Projekt";
        }

        /// <summary>Das CREATE der Profiltabelle.</summary>
        public static string SqlCreateProfil()
        {
            string kennungen = string.Join(",", System.Enum.GetNames(typeof(PufferNutzungsprofil)).Select(n => "'" + n + "'"));
            return "CREATE TABLE IF NOT EXISTS \"" + TAB_PROFIL + "\" (" +
                   "\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
                   "\"Kennung\" TEXT NOT NULL UNIQUE CHECK (\"Kennung\" IN (" + kennungen + ")), " +
                   "\"ReadOnly\" INTEGER NOT NULL DEFAULT 0 CHECK (\"ReadOnly\" IN (0,1))" +
                   ") STRICT";
        }

        /// <summary>Das CREATE der Zuordnungstabelle.</summary>
        public static string SqlCreateZuordnung()
        {
            string quellen = string.Join(",", NutzungsprofilQuelle.ALLE.Select(q => "'" + q + "'"));
            return "CREATE TABLE IF NOT EXISTS \"" + TAB_ZUORDNUNG + "\" (" +
                   "\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
                   "\"ID_Nutzungsprofil\" INTEGER NOT NULL REFERENCES \"" + TAB_PROFIL + "\" (\"ID\") ON DELETE CASCADE ON UPDATE CASCADE, " +
                   "\"Quelle\" TEXT NOT NULL CHECK (\"Quelle\" IN (" + quellen + ")), " +
                   "\"Schluessel\" TEXT NOT NULL CHECK (length(\"Schluessel\") BETWEEN 1 AND 120), " +
                   "UNIQUE (\"Quelle\", \"Schluessel\")" +
                   ") STRICT";
        }

        /// <summary>Stehen beide Tabellen?</summary>
        public static bool SchemaVollstaendig() =>
            DataRepository.TabelleVorhanden(TAB_PROFIL) && DataRepository.TabelleVorhanden(TAB_ZUORDNUNG);

        /// <summary>Schema vollständig, jedes Profil und jede Zuordnung der Vorgabe gesät?</summary>
        public static bool Vollstaendig()
        {
            if (!SchemaVollstaendig()) return false;
            long profile = System.Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + TAB_PROFIL), CultureInfo.InvariantCulture);
            long zuordnungen = System.Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + TAB_ZUORDNUNG), CultureInfo.InvariantCulture);
            return profile >= System.Enum.GetNames(typeof(PufferNutzungsprofil)).Length && zuordnungen >= NutzungsprofilZuordnung.VORGABE.Count;
        }

        /// <summary>Die DDL-Anweisungen — je fehlende Tabelle eine; leer, wenn beide stehen (<b>wiederholbar</b>).</summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                if (!DataRepository.TabelleVorhanden(TAB_PROFIL))
                    yield return new KeyValuePair<string, string>(TAB_PROFIL + " anlegen", SqlCreateProfil());
                if (!DataRepository.TabelleVorhanden(TAB_ZUORDNUNG))
                    yield return new KeyValuePair<string, string>(TAB_ZUORDNUNG + " anlegen", SqlCreateZuordnung());
            }
        }

        internal const string SQL_PROFIL_SAAT =
            "INSERT OR IGNORE INTO " + TAB_PROFIL + " (Kennung, ReadOnly) VALUES (?, 1)";
        internal const string SQL_ZUORDNUNG_SAAT =
            "INSERT OR IGNORE INTO " + TAB_ZUORDNUNG + " (ID_Nutzungsprofil, Quelle, Schluessel) " +
            "SELECT ID, ?, ? FROM " + TAB_PROFIL + " WHERE Kennung = ?";

        /// <summary>
        /// Die Saat: Profile und Zuordnung (INSERT OR IGNORE), dann die Nutzungsarten Büro, Schule, Gewerbe in
        /// einen versionierten Zapfkatalog. Wiederholbar.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Zeilen (Profile, Zuordnungen, Nutzungsarten).</returns>
        public static int Saat(IList<string> bericht)
        {
            int p = 0, z = 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                foreach (string k in System.Enum.GetNames(typeof(PufferNutzungsprofil)))
                    p += v.Ausfuehren(SQL_PROFIL_SAAT, new DbParam("?", k));
                foreach (var e in NutzungsprofilZuordnung.VORGABE)
                    z += v.Ausfuehren(SQL_ZUORDNUNG_SAAT,
                                      new DbParam("?", e.Quelle),
                                      new DbParam("?", e.Schluessel),
                                      new DbParam("?", e.Profil.ToString()));
                v.Commit();
            }
            bericht?.Add(TAB_PROFIL + ": " + p.ToString(CultureInfo.InvariantCulture) + " Profil(e) angelegt");
            bericht?.Add(TAB_ZUORDNUNG + ": " + z.ToString(CultureInfo.InvariantCulture) + " Zuordnung(en) angelegt");
            int n = TwwPaketteilCtrl.NutzungsartenNachtragen(NUTZUNGSARTEN, null, out string fehler);
            if (n < 0) throw new System.InvalidOperationException("Nutzungsarten Büro/Schule/Gewerbe: " + fehler);
            bericht?.Add("Tab_TwwNutzungsart_STAMM: " + n.ToString(CultureInfo.InvariantCulture) + " Nutzungsart(en) nachgetragen");
            return p + z + n;
        }

        /// <summary>
        /// Führt den Schritt aus — DDL, dann die Saat — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen Helfer.
        /// </summary>
        /// <returns>Die Zahl der DDL-Handgriffe.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            int n = 0;
            foreach (KeyValuePair<string, string> a in Anweisungen.ToList())
            {
                DataRepository.ExecuteNonQuery(a.Value);
                n++;
                bericht?.Add(a.Key);
            }
            if (n == 0) bericht?.Add(TAB_PROFIL + " und " + TAB_ZUORDNUNG + " vorhanden");
            Saat(bericht);
            return n;
        }
    }
}
