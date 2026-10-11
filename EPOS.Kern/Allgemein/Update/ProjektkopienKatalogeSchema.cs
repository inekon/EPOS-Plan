using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // PROJEKTKOPIEN DER BRENNSTOFFE, KONDITIONIERUNGSVORLAGEN UND PUFFERAUSLEGUNGS-VORGABEN -
    // Anwenderentscheid 03.10.2026 („Die drei Kataloge sollten ebenfalls eine Projektkopie
    // besitzen"), Konzept Simulationsablauf Abschnitt 22.
    //
    // WARUM. Der Katalogabgleich (KU1) aktualisiert unveränderte Auslieferungssätze. Ein Projekt,
    // das einen Satz ohne Projektkopie liest, rechnete nach einem Update anders. Mit der Kopie
    // fasst der Abgleich nur den Stamm an; das Projekt rechnet wie vorher.
    //
    // WAS DER SCHRITT ANLEGT
    //   (1) Tab_Brennstoff (STRICT): je Projekt und Brennstoffart (ID_Brennstoff = die ID des
    //       Stammsatzes, die Brennstoffart, die Geräte, Träger und Referenzkessel führen) die
    //       Fachwerte des Stamms samt Emissionsfaktoren, Heizwerten, Kategorie und Preisvorgaben,
    //       dazu Katalogfassung_Herkunft (die Fassung zur Zeit der Kopie). Eindeutig je
    //       (ID_Projekt, ID_Brennstoff). Die Verweise der Projekttabellen bleiben, wie sie sind:
    //       Sie nennen die Brennstoffart, die Werte liest der Kern über ProjektBrennstoffe.Sicht
    //       aus der Kopie des Projekts.
    //   (2) Tab_PufferAuslegungParameter (STRICT): je Projekt mit Pufferauslegung die Vorgaben der
    //       Auslegung (Schluessel, Wert, Einheit, Quelle, Herkunftsart) samt Katalogfassung_Herkunft.
    //       Eindeutig je (ID_Projekt, Schluessel).
    //   (3) Konditionierungsvorlagen: KEINE neue Tabelle. „Vorlage übernehmen" kopiert schon heute
    //       den Inhalt in die Matrix und den Kalender des Projektgebäudes bzw. der Zone (Eigentümer
    //       ID_Gebaeude, ID_Zone); die Herkunft steht nur als Text in Bemerkung, nie als ID. Der Lauf
    //       liest ausschließlich diese Projektzeilen (Konditionierungdatenweg) - die Projektkopie
    //       besteht.
    //
    // DIE SAAT kopiert WERTGLEICH: je Projekt jeden Stammsatz der Brennstoffe (die Brennstoffart
    // eines Projekts ist über Rückfallketten - Stromträger, Referenzkessel, Gerätebrennstoff ohne
    // Träger - nicht abschließend bestimmbar; die vollständige Kopie hält die Zusage), je Projekt mit
    // Pufferauslegung jede Vorgabe. Wiederholbar: Eine stehende Kopie bleibt, wie sie ist.
    //
    // ERGEBNISNEUTRAL. Die Kopien tragen dieselben Werte wie der Stamm; der Referenzlauf bleibt
    // byte-gleich.
    //
    // VIER LESER: die Schalenmigration, Werkzeuge/Testdatenbankschema, die Testvorrichtung in
    // EPOS.Kern.Tests und die Paketanhebung (Art Ddl: ein älteres Paket bringt keine Kopien mit; das
    // Projekt liest den Katalog des Ziels, bis der Abgleich oder der Projektdialog die Kopie anlegt).
    // ====================================================================================

    /// <summary>
    /// Der Schemaschritt der Projektkopien für Brennstoffe und Pufferauslegungs-Vorgaben — EINE Quelle
    /// für Migration, Werkzeug, Testkopie und Nachweis. Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class ProjektkopienKatalogeSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht: der nächste Schritt
        /// hinter Aufschlag und manueller Aufheizzeit (<see cref="AufheizManuellSchema"/>, 174), also 175.
        /// </summary>
        public const int SCHRITT = AufheizManuellSchema.SCHRITT + 1;

        /// <summary>Die Projektkopie der Brennstoffe.</summary>
        public const string TAB_BRENNSTOFF = "Tab_Brennstoff";

        /// <summary>Die Projektkopie der Vorgaben der Pufferauslegung.</summary>
        public const string TAB_PUFFERPARAMETER = "Tab_PufferAuslegungParameter";

        /// <summary>Die Spalte mit der Katalogfassung zur Zeit der Kopie (leer = nie abgeglichen).</summary>
        public const string SPALTE_HERKUNFT = "Katalogfassung_Herkunft";

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IEnumerable<string> Voraussetzungen()
        {
            yield return "Tab_Projekt";
            yield return ProjektBrennstoffe.TAB_STAMM;
            yield return PufferAuslegungSchema.TAB;
            yield return PufferAuslegungSchema.TAB_PARAMETER;
        }

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_Brennstoff</c> — STRICT.</summary>
        public static string SqlCreateBrennstoff()
        {
            return "CREATE TABLE IF NOT EXISTS \"" + TAB_BRENNSTOFF + "\" (\n" +
                   "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                   "    \"ID_Projekt\" INTEGER NOT NULL REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE,\n" +
                   "    \"ID_Brennstoff\" INTEGER NOT NULL CHECK (\"ID_Brennstoff\" > 0),\n" +
                   "    \"ID_Kategorie\" INTEGER NOT NULL,\n" +
                   "    \"Bezeichner\" TEXT NOT NULL,\n" +
                   "    \"Einheit\" TEXT,\n" +
                   "    \"PreisEinheit\" TEXT,\n" +
                   "    \"Hi\" REAL DEFAULT 0,\n" +
                   "    \"Hs\" REAL DEFAULT 0,\n" +
                   "    \"CO2\" REAL DEFAULT 0,\n" +
                   "    \"SO2\" REAL DEFAULT 0,\n" +
                   "    \"NOx\" REAL DEFAULT 0,\n" +
                   "    \"Staub\" REAL DEFAULT 0,\n" +
                   "    \"PE_Faktor\" REAL DEFAULT 0,\n" +
                   "    \"Standard_Grundpreis\" REAL DEFAULT 0,\n" +
                   "    \"Standard_Arbeitspreis\" REAL DEFAULT 0,\n" +
                   "    \"Standard_Leistungspreis\" REAL DEFAULT 0,\n" +
                   "    \"" + SPALTE_HERKUNFT + "\" INTEGER CHECK (\"" + SPALTE_HERKUNFT + "\" IS NULL OR \"" +
                   SPALTE_HERKUNFT + "\" >= 0),\n" +
                   "    UNIQUE (\"ID_Projekt\", \"ID_Brennstoff\")\n" +
                   ") STRICT";
        }

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_PufferAuslegungParameter</c> — STRICT.</summary>
        public static string SqlCreatePufferparameter()
        {
            return "CREATE TABLE IF NOT EXISTS \"" + TAB_PUFFERPARAMETER + "\" (\n" +
                   "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                   "    \"ID_Projekt\" INTEGER NOT NULL REFERENCES \"Tab_Projekt\" (\"ID\") ON DELETE CASCADE,\n" +
                   "    \"Schluessel\" TEXT NOT NULL,\n" +
                   "    \"Wert\" REAL NOT NULL,\n" +
                   "    \"Einheit\" TEXT,\n" +
                   "    \"Quelle\" TEXT NOT NULL,\n" +
                   "    \"Herkunftsart\" TEXT CHECK (\"Herkunftsart\" IS NULL OR \"Herkunftsart\" IN (" +
                   string.Join(",", PufferHerkunftsart.ALLE.Select(a => "'" + a + "'")) + ")),\n" +
                   "    \"" + SPALTE_HERKUNFT + "\" INTEGER CHECK (\"" + SPALTE_HERKUNFT + "\" IS NULL OR \"" +
                   SPALTE_HERKUNFT + "\" >= 0),\n" +
                   "    UNIQUE (\"ID_Projekt\", \"Schluessel\")\n" +
                   ") STRICT";
        }

        /// <summary>Steht das Schema des Schritts (beide Tabellen, ohne die Saat)?</summary>
        public static bool SchemaVollstaendig() =>
            DataRepository.TabelleVorhanden(TAB_BRENNSTOFF) && DataRepository.TabelleVorhanden(TAB_PUFFERPARAMETER);

        /// <summary>Schema vollständig und jede Projektkopie gesät?</summary>
        public static bool Vollstaendig() =>
            SchemaVollstaendig() && ProjektBrennstoffe.OffeneKopien() == 0 && ProjektPufferparameter.OffeneKopien() == 0;

        /// <summary>
        /// Die DDL-Anweisungen des Schritts — je fehlende Tabelle eine; leer, wenn beide stehen
        /// (<b>wiederholbar</b>). Die Saat folgt in <see cref="Ausfuehren"/>.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                if (!DataRepository.TabelleVorhanden(TAB_BRENNSTOFF))
                    yield return new KeyValuePair<string, string>(TAB_BRENNSTOFF + " anlegen", SqlCreateBrennstoff());
                if (!DataRepository.TabelleVorhanden(TAB_PUFFERPARAMETER))
                    yield return new KeyValuePair<string, string>(TAB_PUFFERPARAMETER + " anlegen", SqlCreatePufferparameter());
            }
        }

        /// <summary>
        /// Die Saat des Schritts: die wertgleichen Projektkopien aller Projekte (Brennstoffe je Projekt,
        /// Vorgaben je Projekt mit Pufferauslegung). Wiederholbar.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Kopien.</returns>
        public static int Saat(IList<string> bericht)
        {
            int b, p;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                b = ProjektBrennstoffe.Sichern(v, null);
                p = ProjektPufferparameter.Sichern(v, null);
                v.Commit();
            }
            bericht?.Add(TAB_BRENNSTOFF + ": " + b.ToString(CultureInfo.InvariantCulture) + " Projektkopie(n) angelegt");
            bericht?.Add(TAB_PUFFERPARAMETER + ": " + p.ToString(CultureInfo.InvariantCulture) + " Projektkopie(n) angelegt");
            return b + p;
        }

        /// <summary>
        /// Führt den Schritt aus — DDL, dann die Saat — für <c>Werkzeuge/Testdatenbankschema</c> und
        /// <c>EPOS.Kern.Tests</c>; die Migration der Schale geht denselben Weg über ihre eigenen Helfer.
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
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
            if (n == 0) bericht?.Add(TAB_BRENNSTOFF + " und " + TAB_PUFFERPARAMETER + " vorhanden");
            Saat(bericht);
            return n;
        }
    }
}
