using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // SPERRPROFIL DER WAERMEPUMPE - Schemaschritt der Welle V14 (Anwenderentscheid 03.10.2026:
    // nur fuer die Waermepumpe, ohne Dimmung, ohne Rechtsbezug).
    //
    // EINE NEUE TABELLE, KEIN DML:
    //
    //   Tab_Sperrfenster (STRICT, AUTOINCREMENT): je Waermepumpen-Anlagenzeile beliebig viele
    //   Sperrfenster - Beginn Von_h [0 … 24], Dauer_h (0 … 24], Wochentage als Bitmaske
    //   (Mo = 1 … So = 64, Vorgabe 127 = jeden Tag), Heizstab_gesperrt (0/1, Vorgabe 1) und eine
    //   Reihenfolge. ID_Energieanlage mit ON DELETE CASCADE: geht die Anlage, gehen ihre Fenster.
    //
    // Das Altfenster Tab_Energieanlagen.Sperrung/Sperrzeit_von/Sperrzeit_bis bleibt gueltig und
    // wird vom Rechenweg weiter gelesen; die Tabelle ergaenzt es. Ueberfuehrt wird es nur durch
    // den Anwender (Speichern im Waermepumpen-Dialog), nie durch diesen Schritt.
    //
    // ERGEBNISNEUTRAL: Die Tabelle kommt leer an; ohne Zeilen rechnet der Lauf wie zuvor, der
    // Referenzlauf bleibt byte-gleich.
    //
    // VIER LESER: SchemaMigration (WindowsFormsApplication1/Allgemein/Update), das Werkzeug
    // Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und die Paketanhebung.
    // ====================================================================================

    /// <summary>
    /// Die Tabelle der Sperrfenster einer Wärmepumpe — EINE Quelle für Migration, Werkzeug,
    /// Testkopie und Nachweis (ADR-001 Option C). Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class WaermepumpeSperrprofilSchema
    {
        /// <summary><b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht.</summary>
        // Kette: beim Merge auf <Klasse 176>.SCHRITT + 1 umhängen
        public const int SCHRITT = ProjektkopienKatalogeSchema.SCHRITT + 2;

        /// <summary>Die Tabelle der Sperrfenster.</summary>
        public const string TAB = "Tab_Sperrfenster";

        /// <summary>Die Bitmaske „jeden Tag“ (Mo = 1 … So = 64).</summary>
        public const int ALLE_TAGE = 127;

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Sperrfenster</c> — STRICT, AUTOINCREMENT.</summary>
        public static string SqlCreate()
        {
            return "CREATE TABLE IF NOT EXISTS \"" + TAB + "\" (\n" +
                   "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                   "    \"ID_Energieanlage\" INTEGER NOT NULL REFERENCES \"" + SchemaKatalog.TAB_ENERGIEANLAGEN +
                   "\" (\"ID\") ON DELETE CASCADE,\n" +
                   "    \"Von_h\" REAL NOT NULL DEFAULT 0 CHECK (\"Von_h\" >= 0 AND \"Von_h\" <= 24),\n" +
                   "    \"Dauer_h\" REAL NOT NULL CHECK (\"Dauer_h\" > 0 AND \"Dauer_h\" <= 24),\n" +
                   "    \"Wochentage\" INTEGER NOT NULL DEFAULT " + ALLE_TAGE +
                   " CHECK (\"Wochentage\" >= 1 AND \"Wochentage\" <= " + ALLE_TAGE + "),\n" +
                   "    \"Heizstab_gesperrt\" INTEGER NOT NULL DEFAULT 1 CHECK (\"Heizstab_gesperrt\" IN (0,1)),\n" +
                   "    \"Reihenfolge\" INTEGER NOT NULL DEFAULT 0\n" +
                   ") STRICT";
        }

        /// <summary>Der Index auf die Anlage (Lesen je Wärmepumpe, Duplizieren, Löschen).</summary>
        public const string SQL_INDEX =
            "CREATE INDEX IF NOT EXISTS \"IX_Tab_Sperrfenster_Anlage\" ON \"" + TAB + "\" (\"ID_Energieanlage\")";

        /// <summary>Steht die Tabelle?</summary>
        public static bool Vollstaendig() => DataRepository.TabelleVorhanden(TAB);

        /// <summary>
        /// Die DDL-Anweisungen des Schritts — Beschreibung und SQL; leer, wenn die Tabelle steht
        /// (<b>wiederholbar</b>).
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                if (DataRepository.TabelleVorhanden(TAB)) yield break;
                yield return new KeyValuePair<string, string>(TAB + " anlegen", SqlCreate());
                yield return new KeyValuePair<string, string>(TAB + ": Index auf ID_Energieanlage", SQL_INDEX);
            }
        }

        /// <summary>
        /// Führt den Schritt aus: die Tabelle samt Index anlegen, wenn sie fehlt — in EINEM Vorgang.
        /// Für <c>SchemaMigration</c>, <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der Handgriffe (0, wenn die Tabelle stand).</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            List<KeyValuePair<string, string>> ddl = Anweisungen.ToList();
            if (ddl.Count == 0)
            {
                bericht?.Add(TAB + " vorhanden");
                return 0;
            }
            using (DbVorgang v = DataRepository.Vorgang())
            {
                foreach (KeyValuePair<string, string> a in ddl)
                {
                    v.Ausfuehren(a.Value);
                    bericht?.Add(a.Key);
                }
                v.Commit();
            }
            return ddl.Count;
        }
    }
}
