using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // EIN SATZ JE ENERGIETRAEGER UND PROJEKT - Migrationsschritt 76 (Auftrag #278,
    // Anwenderentscheid vom 15.09.2026, Ausgangslage aus Auftrag #268).
    //
    // WOZU. "Ein Preis und ein Emissionssatz je Traegertyp im Projekt" ist eine Regel des
    // Hauses, und bis hierher hielt sie allein die Anwendungslogik: Jeder der fuenf
    // Schreibwege in energy_project_settings zaehlt vorher nach, ob die Zuordnung schon
    // steht. Die Datenbank selbst liess die zweite Zeile zu. Was die Regel bricht, ist
    // deshalb nicht der einzelne Schreibweg, sondern jeder Weg, der ZEILEN KOPIERT -
    // Variantenanlage, Projektkopie, Projekttransfer -, wenn er zweimal laeuft.
    //
    // WARUM DAS NICHT NUR ORDNUNGSLIEBE IST. Jede Lesekette dieses Hauses fragt die
    // Tabelle mit WHERE ID_Projekt = ? AND ID_Energietraeger = ? und nimmt die ERSTE
    // gelieferte Zeile - EmissionsFaktorLader.AltwerteProjekt, StromPreisCtrl (Stufe 3
    // der Preiskette), KostenEmissionRechner, WirtschaftlichkeitCtrl. Keine dieser
    // Stellen sortiert. Bei zwei Zeilen entscheidet also die Speicherreihenfolge, welcher
    // Preis und welcher Emissionsfaktor gilt; die zweite Zeile ist unsichtbar und
    // dennoch da. Ein Anwender, der den Preis pflegt, pflegt dann vielleicht die Zeile,
    // die niemand liest.
    //
    // DIE SPALTENKOMBINATION IST GEMESSEN, NICHT GERATEN. Der Bestand der Messlatte
    // Referenzlaeufe/Kenndaten_Test.sqlite (28 Zeilen, 18 Projekte) fuehrt zu keinem
    // Paar (ID_Projekt, ID_Energietraeger) mehr als eine Zeile, und alle Lesewege
    // fragen genau dieses Paar. ID_Umrechnung gehoert NICHT dazu: Sie benennt die
    // Recheneinheit der Zeile, nicht ihre Identitaet - zwei Zeilen desselben Traegers mit
    // verschiedener Recheneinheit waeren genau der Fall, den dieser Schritt ausschliesst.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // WechselrichterSchema (65), AnlageStrangSchema (66), SpeicherAuslegungStrict (74)
    // und NutzungsdauerSchema (75): Die Anweisungen brauchen DREI Leser - den
    // Schemaschritt in WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs
    // (Access-Zweig, deshalb nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema
    // und den Nachweis in EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden
    // sie abschreiben - drei Wahrheiten ueber denselben Index.
    //
    // DER PLAIN-INDEX UND NICHT DER COALESCE-INDEX. NutzungsdauerSchema (75) legt seinen
    // eindeutigen Index ueber COALESCE(KomponentenID, 0), weil dort zwei Zeilen mit NULL
    // erlaubt und gemeint sind und SQLite NULL nicht gegen NULL haelt. Hier ist die Lage
    // anders herum: Die Spalte ID_Energietraeger ist zwar nullbar, aber eine Zeile OHNE
    // Traeger ist kein "Satz je Energietraeger" - sie ist von keinem Leseweg erreichbar
    // (alle fragen mit = ?) und von keinem Schreibweg erzeugbar (alle nennen den Traeger).
    // Der Index ueber die beiden ROHEN Spalten hat dafuer einen Vorzug, den ein
    // Ausdrucksindex nicht haette: Er ist zugleich der SUCHWEG genau dieser Lesekette,
    // die es bis hierher nur ueber den Projektindex allein gab.
    //
    // WAS DER SCHRITT MIT EINEM UNSAUBEREN BESTAND MACHT. Er raeumt ihn auf, statt
    // stehenzubleiben - ein gescheiterter Schemaschritt sperrt den Simulationsbereich
    // (ADR-001), und das waere die schlechtere Antwort auf eine Zeile, die ohnehin
    // niemand liest. Behalten wird je Paar die Zeile mit der KLEINSTEN ID: Genau sie
    // liefert SQLite heute als erste, und genau sie gilt deshalb schon jetzt in jeder
    // Lesekette. Der Schritt ist damit ERGEBNISNEUTRAL - er entfernt nur, was ohne
    // Wirkung war, und die Messlatte hat ohnehin nichts zu entfernen.
    // ====================================================================================

    /// <summary>
    /// Der eindeutige Index ueber <c>energy_project_settings</c> (Schemaschritt 76) samt
    /// der Entdoppelung, die ihm vorausgeht - EINE Quelle fuer Migration, Werkzeug und
    /// Nachweis.
    /// </summary>
    public static class ProjektEnergietraegerEindeutig
    {
        /// <summary>Die Tabelle, ueber der der Index steht.</summary>
        public const string TABELLE = "energy_project_settings";

        /// <summary>Erste Schluesselspalte: das Projekt.</summary>
        public const string SPALTE_PROJEKT = "ID_Projekt";

        /// <summary>
        /// Zweite Schluesselspalte: der Energietraeger. <b>Buchstabengetreu mit Umlaut</b>
        /// (BETRIEB_SQLITE.md 6.1) - SQLite vergleicht Bezeichner nur bei ASCII-Buchstaben
        /// ohne Ruecksicht auf Gross- und Kleinschreibung.
        /// </summary>
        public const string SPALTE_TRAEGER = "ID_Energieträger";

        /// <summary>Name des eindeutigen Index.</summary>
        public const string INDEX = "idx_EnergyProjectSettings_Traeger";

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        /// <summary>
        /// Entfernt je Paar (Projekt, Traeger) alles ausser der Zeile mit der kleinsten
        /// <c>ID</c> - der Zeile, die jede Lesekette heute schon nimmt (Klassenkopf).
        ///
        /// <para><b>Zeilen ohne Traeger bleiben unberuehrt.</b> Sie fallen nicht unter die
        /// Regel „ein Satz je Energietraeger", und der Index haelt sie ebenfalls nicht
        /// gegeneinander - SQLite zaehlt NULL in einem eindeutigen Index nie als
        /// Dublette.</para>
        ///
        /// <para><b>Wiederholbar:</b> Ein zweiter Lauf findet nichts mehr zu loeschen.</para>
        /// </summary>
        public const string SQL_ENTDOPPELN =
            "DELETE FROM \"" + TABELLE + "\" " +
            "WHERE \"" + SPALTE_TRAEGER + "\" IS NOT NULL " +
            "AND \"ID\" NOT IN (SELECT MIN(\"ID\") FROM \"" + TABELLE + "\" " +
            "WHERE \"" + SPALTE_TRAEGER + "\" IS NOT NULL " +
            "GROUP BY \"" + SPALTE_PROJEKT + "\", \"" + SPALTE_TRAEGER + "\")";

        /// <summary>
        /// Der eindeutige Index ueber die beiden rohen Schluesselspalten. <c>IF NOT
        /// EXISTS</c> macht den Schritt wiederholbar; die Anlage scheitert, solange noch
        /// eine Dublette steht - deshalb laeuft <see cref="SQL_ENTDOPPELN"/> vorher.
        /// </summary>
        public const string SQL_INDEX =
            "CREATE UNIQUE INDEX IF NOT EXISTS \"" + INDEX + "\" ON \"" + TABELLE + "\" " +
            "(\"" + SPALTE_PROJEKT + "\", \"" + SPALTE_TRAEGER + "\")";

        /// <summary>
        /// Beide Anweisungen in Ausfuehrungsreihenfolge - so, wie Migration und Werkzeug
        /// sie abarbeiten: erst entdoppeln, dann den Index anlegen. Die Reihenfolge ist
        /// NICHT beliebig.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(
                    "Entdoppelung " + TABELLE, SQL_ENTDOPPELN);
                yield return new KeyValuePair<string, string>(
                    "Index " + INDEX, SQL_INDEX);
            }
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Wie viele Zeilen zu viel stehen da? 0 = der Index laesst sich anlegen.
        ///
        /// <para>Gezaehlt wird die Summe der UEBERZAEHLIGEN Zeilen je Paar (also
        /// <c>n - 1</c>), nicht die Zahl der Paare: Genau so viele Zeilen entfernt
        /// <see cref="SQL_ENTDOPPELN"/>.</para>
        /// </summary>
        public static string Zaehlung()
        {
            return "SELECT COALESCE(SUM(\"n\" - 1), 0) FROM (" +
                   "SELECT COUNT(*) AS \"n\" FROM \"" + TABELLE + "\" " +
                   "WHERE \"" + SPALTE_TRAEGER + "\" IS NOT NULL " +
                   "GROUP BY \"" + SPALTE_PROJEKT + "\", \"" + SPALTE_TRAEGER + "\" " +
                   "HAVING COUNT(*) > 1) AS \"d\"";
        }

        /// <summary>
        /// Steht der eindeutige Index? 1 = ja. Gefragt wird <c>sqlite_master</c> nach dem
        /// Namen - dieselbe Frage, die Migrationsbericht, Werkzeug und Nachweis stellen.
        /// </summary>
        public static string ZaehlungIndex()
        {
            return "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = '" +
                   INDEX + "'";
        }

        /// <summary>
        /// Stehen noch Dubletten im Bestand? Die Auskunft geht ueber
        /// <see cref="Zaehlung"/>; eine nicht lesbare Zaehlung gilt als „nichts zu tun".
        /// </summary>
        public static bool EntdoppelungNoetig()
        {
            object wert = DataRepository.ExecuteScalar(Zaehlung());
            if (wert == null || wert == DBNull.Value) return false;
            return Convert.ToInt64(wert, System.Globalization.CultureInfo.InvariantCulture) > 0;
        }
    }
}
