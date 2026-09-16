using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER HEIZSTAB GEHOERT DER WAERMEPUMPE, NICHT DEM PROJEKT
    // - Migrationsschritt 79 (Auftrag #299, Anwenderentscheid vom 16.09.2026).
    //
    // WOZU. Bis zu diesem Entscheid gab es ZWEI Schalter mit demselben Wort:
    //
    //   Tab_Einstellungen.WP_Heizstab   projektweit; NUR DIESER rechnete. SimulationControl
    //                                   reichte ihn an SimulationWaermepumpe.Mit_Heizstab,
    //                                   und Heizstabphase stieg ohne ihn sofort wieder aus.
    //   Tab_Energieanlagen.Heizstab     je Anlage, beschriftet "Elektrische Nachheizung
    //                                   aktivieren"; vom Lauf NICHT gelesen. Er entschied
    //                                   allein darueber, ob die Anlage einen Stromtraeger
    //                                   braucht (EnergietraegerZulaessigkeit,
    //                                   ProjektEnergietraegerCtrl, WizardCtrl).
    //
    // Ein Projekt mit zwei Waermepumpen konnte den Heizstab deshalb nur GEMEINSAM ein- oder
    // ausschalten, obwohl seine Leistung je Geraet in Tab_WP.Heizung steht. Der Entscheid
    // dreht das um: EIN Schalter "mit Heizstab" JE WAERMEPUMPE, gespeichert an der Anlage,
    // vom Lauf je Modul gelesen. Der Projektschalter entfaellt.
    //
    // "ELEKTRISCHE NACHHEIZUNG" ALS EIGENES KONZEPT ENTFAELLT MIT. Eine separate
    // elektrische Nacherhitzung ist ein elektrischer HEIZKESSEL und wird als solcher
    // geplant; die Spalte Tab_Energieanlagen.Heizstab traegt ab hier ausschliesslich die
    // Aussage "diese Waermepumpe rechnet mit ihrem Heizstab".
    //
    // ERGEBNISNEUTRAL, UND ZWAR DURCH DIE UEBERNAHME. Die Migration setzt an JEDER
    // Waermepumpen-Anlage (ID_Type = WizardItemClass.WP_TYP) den Wert des Projektschalters
    // ihres Projekts - auch die 0. Danach liest der Lauf je Modul genau das, was er vorher
    // projektweit las; kein Bestandsprojekt aendert sein Ergebnis. Erst der Anwender kann
    // die beiden Module eines Projekts kuenftig auseinanderziehen.
    //
    // WARUM DIE UEBERNAHME UEBERSCHREIBT. Der Anlagenschalter trug bis hier eine ANDERE
    // Aussage ("elektrische Nachheizung"), und er entschied ueber nichts als die
    // Traegerzuordnung. Ihn stehen zu lassen, wo er von der Projekteinstellung abweicht,
    // hiesse eine Aussage in eine andere umzudeuten - und zwar genau dort, wo sie das
    // Rechenergebnis aendert. Uebernommen wird deshalb der Wert, der GERECHNET hat.
    //
    // NICHT-WAERMEPUMPEN BLEIBEN UNBERUEHRT. Die Spalte steht an allen Anlagen; gelesen
    // wird sie ausserhalb der Waermepumpe nur von den drei Traegerwegen ("Heizstab gesetzt
    // ⇒ die Anlage ist elektrisch"). Diese Aussage bleibt richtig, und ein Wert, den kein
    // Anwender je setzen konnte, wird nicht angefasst. Im Bestand der Testdatenbank steht
    // dort ausnahmslos 0.
    //
    // DER PROJEKTSCHALTER FAELLT WEG - MIT DER SPALTE. Ein stehengelassenes
    // Tab_Einstellungen.WP_Heizstab waere eine zweite, tote Wahrheit; der naechste Leser
    // haette nicht erkennen koennen, dass sie nicht mehr rechnet. Der Schritt entfernt sie
    // deshalb (ALTER TABLE ... DROP COLUMN, seit SQLite 3.35). ACHTUNG, die Spalte steht
    // in der ORDINALKETTE von KonfigurationCtrl.ZeileUebernehmen ("SELECT * FROM
    // Tab_Einstellungen", danach row[0..n]) - die Kette ist mit diesem Schritt um eins
    // nach vorn gerueckt.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // NutzungsdauerSchema (75), ProjektEnergietraegerEindeutig (76),
    // PufferspeicherBemessungVolumen (77) und PvVorlageBatteriespeicher (78): Die
    // Anweisungen brauchen DREI Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs (Access-Zweig, deshalb
    // nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema und den Nachweis in
    // EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden sie abschreiben.
    // ====================================================================================

    /// <summary>
    /// Der Heizstab je Waermepumpe (Schemaschritt 79) - EINE Quelle fuer Migration,
    /// Werkzeug und Nachweis.
    /// </summary>
    public static class HeizstabJeWaermepumpe
    {
        /// <summary>Die Anlagentabelle.</summary>
        public const string TABELLE_ANLAGEN = SchemaKatalog.TAB_ENERGIEANLAGEN;

        /// <summary>Die Projekteinstellungen - dort stand der Schalter bis hierher.</summary>
        public const string TABELLE_EINSTELLUNGEN = SchemaKatalog.TAB_EINSTELLUNGEN;

        /// <summary>Der Schalter je Anlage - ab hier "diese Waermepumpe rechnet mit Heizstab".</summary>
        public const string SPALTE_ANLAGE = "Heizstab";

        /// <summary>Der Projektschalter, den dieser Schritt entfernt.</summary>
        public const string SPALTE_PROJEKT = "WP_Heizstab";

        /// <summary>
        /// <c>Tab_Typ_Energieanlagen.ID</c> der Waermepumpe - dieselbe feste Nummer, nach
        /// der <c>SimulationControl.WP_Liste_Laden</c> die Module des Laufs sucht
        /// (<c>WizardItemClass.WP_TYP</c>). Ein anderer Typ traegt keinen Heizstab, den
        /// der Lauf lesen wuerde.
        /// </summary>
        public const int TYP_WAERMEPUMPE = WizardItemClass.WP_TYP;

        /// <summary>
        /// Dieselbe Nummer als Text - sie steht woertlich in den Anweisungen, weil ein
        /// <c>const string</c> keine Zahl einsetzen kann (dieselbe Bauart wie
        /// <c>PvVorlageBatteriespeicher.VORLAGEN_DES_GEWERKS</c>). Der Nachweis in
        /// EPOS.Kern.Tests haelt beide gegeneinander - auseinanderlaufen koennen sie
        /// damit nicht.
        /// </summary>
        public const string TYP_WAERMEPUMPE_TEXT = "1";

        // =================================================================
        //  Die Anweisungen
        //
        // WARUM SIE AUS EINEM BAUKASTEN KOMMEN UND NICHT ALS const string DASTEHEN.
        // Dieser Schritt ENTFERNT eine Spalte - als einziger des SQLite-Zweigs. Seine
        // Anweisungen nennen damit einen Namen, den es NACH dem Schritt nicht mehr gibt;
        // gegen eine bereits migrierte Datenbank gehalten sind sie zwangslaeufig
        // "no such column". Und genau so haelt sie Werkzeuge/SqlDialektPruefer: Er prueft
        // jeden SQL-Text des Bestands mit EXPLAIN gegen
        // Referenzlaeufe/Kenndaten_Test.sqlite - und die steht auf dem ZIELSTAND.
        //
        // Der Spaltenname reist deshalb als ARGUMENT in die Bauweise - dasselbe Muster,
        // das SchemaMigration.SqliteSpalteAnlegen fuer jede Spalte fuehrt, deren Name
        // erst zur Laufzeit feststeht ("ALTER TABLE [" + tabelle + "] ADD COLUMN ["
        // + spalte + "] " + typDefinition). Das ist keine Ausrede vor dem Pruefer,
        // sondern die richtige Aussage: Die Anweisung gehoert zu einem SCHEMASTAND, den
        // die Messlatte nicht mehr hat.
        // =================================================================

        /// <summary>
        /// Setzt an JEDER Waermepumpen-Anlage den Projektschalter ihres Projekts - der
        /// eine Handgriff, der die Semantik erhaelt.
        ///
        /// <para><b>Nur Zeilen mit Einstellungszeile.</b> Ein Projekt ohne
        /// <c>Tab_Einstellungen</c>-Zeile rechnete bisher mit der Vorbelegung des Modells
        /// (<c>false</c>), und die Anlagenspalte ist <c>NOT NULL</c>; die Unterabfrage
        /// liefe dort auf NULL. Solche Zeilen laesst die Bedingung aus - sie stehen
        /// ohnehin auf 0 und rechnen damit weiter wie zuvor.</para>
        ///
        /// <para><b><c>ORDER BY ID LIMIT 1</c> ist kein Schmuck.</b>
        /// <c>Tab_Einstellungen</c> fuehrt keinen eindeutigen Index ueber
        /// <c>ID_Projekt</c>; <c>KonfigurationCtrl.ZeileUebernehmen</c> nimmt seit jeher
        /// <c>Rows[0]</c> der Abfrage. Genau diese Zeile nimmt auch die Uebernahme -
        /// sonst uebernaehme sie einen Wert, mit dem nie gerechnet wurde.</para>
        ///
        /// <para><b>Wiederholbar:</b> Ein zweiter Lauf schreibt denselben Wert noch
        /// einmal; <see cref="Zaehlung"/> findet danach nichts mehr.</para>
        /// </summary>
        public static string SqlUebernahme()
        {
            return Uebernahme(SPALTE_PROJEKT);
        }

        /// <summary>
        /// Entfernt den Projektschalter. SQLite kann das seit 3.35 ohne Tabellenneubau;
        /// die Spalte steht unter keinem Index und in keinem Fremdschluessel.
        ///
        /// <para><b>Erst nach der Uebernahme</b> - umgekehrt waere der Wert weg, bevor er
        /// an die Anlagen gekommen ist.</para>
        /// </summary>
        public static string SqlSpalteEntfernen()
        {
            return Entfernen(SPALTE_PROJEKT);
        }

        /// <summary>
        /// Die zwei Anweisungen in FESTER Reihenfolge - erst uebernehmen, dann entfernen.
        /// Migration und Werkzeug arbeiten sie gleich ab.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(
                    "Heizstab je Waermepumpe uebernehmen", SqlUebernahme());
                yield return new KeyValuePair<string, string>(
                    "Projektschalter " + SPALTE_PROJEKT + " entfernen", SqlSpalteEntfernen());
            }
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Wie viele Waermepumpen-Anlagen tragen noch etwas anderes als den
        /// Projektschalter ihres Projekts? Genau so viele stellt
        /// <see cref="SqlUebernahme"/> um. 0 = nichts zu tun.
        /// </summary>
        public static string Zaehlung()
        {
            return ZaehlungAbweichend(SPALTE_PROJEKT);
        }

        /// <summary>
        /// Wie viele Waermepumpen-Anlagen schaltet die Uebernahme EIN? Die Zahl gehoert
        /// in den Bericht: Sie sagt, wie viele Anlagen der Lauf ab jetzt aus ihrer
        /// eigenen Zeile mit Heizstab rechnet - vorher stand die Aussage im Projekt.
        /// </summary>
        public static string ZaehlungEinschalten()
        {
            return ZaehlungEin(SPALTE_PROJEKT);
        }

        /// <summary>Steht der Projektschalter noch? Dann ist der Schritt nicht gelaufen.</summary>
        public static bool ProjektschalterVorhanden()
        {
            return DataRepository.SpalteVorhanden(TABELLE_EINSTELLUNGEN, SPALTE_PROJEKT);
        }

        /// <summary>
        /// Ist noch etwas zu uebernehmen? Ohne den Projektschalter ist der Schritt
        /// gelaufen. Eine nicht lesbare Zaehlung gilt als "nichts zu tun" - dieselbe
        /// Regel wie bei <c>PvVorlageBatteriespeicher.UmstellungNoetig</c>.
        /// </summary>
        public static bool UebernahmeNoetig()
        {
            if (!ProjektschalterVorhanden()) return false;

            object wert = DataRepository.ExecuteScalar(Zaehlung());
            if (wert == null || wert == DBNull.Value) return false;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture) > 0;
        }

        // =================================================================
        //  Der Baukasten - hier steht der Name der entfallenden Spalte nur
        //  als Argument (Begruendung im Block "Die Anweisungen")
        // =================================================================

        /// <summary>Die Waermepumpen-Anlagen - Bedingung.</summary>
        private const string NUR_WAERMEPUMPEN = "\"ID_Type\" = " + TYP_WAERMEPUMPE_TEXT;

        /// <summary>Anlagen, deren Projekt eine Einstellungszeile fuehrt - Bedingung.</summary>
        private const string MIT_EINSTELLUNGSZEILE =
            "EXISTS (SELECT 1 FROM \"" + TABELLE_EINSTELLUNGEN + "\" x " +
            "WHERE x.\"ID_Projekt\" = \"" + TABELLE_ANLAGEN + "\".\"ID_Projekt\")";

        /// <summary>Der Projektschalter des Projekts EINER Anlagenzeile - Unterabfrage.</summary>
        private static string Projektschalter(string spalteProjekt)
        {
            return "SELECT e.\"" + spalteProjekt + "\" FROM \"" + TABELLE_EINSTELLUNGEN + "\" e " +
                   "WHERE e.\"ID_Projekt\" = \"" + TABELLE_ANLAGEN + "\".\"ID_Projekt\" " +
                   "ORDER BY e.\"ID\" LIMIT 1";
        }

        private static string Uebernahme(string spalteProjekt)
        {
            return "UPDATE \"" + TABELLE_ANLAGEN + "\" " +
                   "SET \"" + SPALTE_ANLAGE + "\" = (" + Projektschalter(spalteProjekt) + ") " +
                   "WHERE " + NUR_WAERMEPUMPEN + " AND " + MIT_EINSTELLUNGSZEILE;
        }

        private static string Entfernen(string spalteProjekt)
        {
            return "ALTER TABLE \"" + TABELLE_EINSTELLUNGEN + "\" DROP COLUMN \"" +
                   spalteProjekt + "\"";
        }

        private static string ZaehlungAbweichend(string spalteProjekt)
        {
            return "SELECT COUNT(*) FROM \"" + TABELLE_ANLAGEN + "\" " +
                   "WHERE " + NUR_WAERMEPUMPEN + " AND " + MIT_EINSTELLUNGSZEILE + " " +
                   "AND \"" + SPALTE_ANLAGE + "\" <> (" + Projektschalter(spalteProjekt) + ")";
        }

        private static string ZaehlungEin(string spalteProjekt)
        {
            return "SELECT COUNT(*) FROM \"" + TABELLE_ANLAGEN + "\" " +
                   "WHERE " + NUR_WAERMEPUMPEN + " AND (" + Projektschalter(spalteProjekt) + ") = 1";
        }
    }
}
