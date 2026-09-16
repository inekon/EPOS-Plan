using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die zweistufige Geraetesuche der Waermepumpen-Verwaltung (iU9-W7.0e) —
    /// woertlich aus <c>Form_WPAuswahl.GeraetedatenFuellen</c> (Z. 95-120, Aenderung Ä22).
    ///
    /// <para><b>Warum zweistufig.</b> Eine Zeile der Projektliste verweist ueber
    /// <c>ID_WP</c> entweder auf die PROJEKTKOPIE (<c>Tab_WP</c>) oder — solange sie noch
    /// nicht gespeichert ist — auf den STAMMKATALOG (<c>Tab_WP_STAMM</c>). Der Neu-Fluss
    /// materialisiert die Kopie erst beim OK der Verwaltung; bis dahin traegt der frische
    /// Eintrag die Stamm-Id. Eine einstufige Suche allein in <c>Tab_WP</c> lief deshalb
    /// in „Datensatz nicht gefunden" (Nutzerbefund „Datensatz ID 67 nicht gefunden").</para>
    ///
    /// <para><b>Eigene Klasse statt einer Methode an <see cref="WPCtrl"/>.</b> Der Weg
    /// braucht BEIDE Controller und gehoert damit keinem von beiden. Er steht hier, weil
    /// ihn nach Welle 7 drei Huellen rufen: die Waermepumpen-Verwaltung, der Anlagendialog
    /// und der Simulation-Detailweg.</para>
    /// </summary>
    internal static class WaermepumpeGeraeteCtrl
    {
        /// <summary>
        /// Uebertraegt die STAMMFELDER eines Geraets in eine Anlagenzeile — Projektkopie
        /// vor Stammkatalog.
        /// </summary>
        /// <param name="ziel">Die Anlagenzeile; sie wird an Ort und Stelle ergaenzt.</param>
        /// <param name="idWp">Die Geraete-Id aus <c>Tab_WP</c> ODER <c>Tab_WP_STAMM</c>.</param>
        /// <returns>
        /// <c>false</c>, wenn das Geraet in KEINER der beiden Tabellen steht — dann zeigt
        /// der Aufrufer die praezisierte Meldung, wie der Vorlaeufer es tat.
        /// </returns>
        internal static bool GeraetedatenFuellen(WErzeugerModel ziel, int idWp)
        {
            if (ziel == null || idWp <= 0) return false;

            WPModel quelle = null;

            WPCtrl projekt = new WPCtrl();
            projekt.ReadAll("ID=" + idWp);
            if (projekt.items.Count > 0) quelle = projekt.items[0];
            else
            {
                WPStammCtrl stamm = new WPStammCtrl();
                stamm.ReadAll("ID=" + idWp);
                if (stamm.rows > 0) quelle = stamm.items[0];
            }
            if (quelle == null) return false;

            ziel.Regelung = quelle.Regelung;
            ziel.Nennleistung = quelle.Nennleistung;
            ziel.Modulkosten = quelle.Modulkosten;
            ziel.Baujahr = quelle.Baujahr;
            ziel.Beschreibung = quelle.Beschreibung;
            ziel.Firma = quelle.Firma;
            ziel.Typ = quelle.Typ;

            // 16.09.2026: Die Aufstellungsart wird MITGEZOGEN. Sie steht seit dem
            // Anwenderentscheid im Feldsatz des Anlagendialogs (die Stammfelder
            // bearbeiten die Projektkopie), und ohne diese Zeile stuende sie dort leer
            // - NachModell schriebe sie so zurueck, genau die Falle aus Ä22/Ä23.
            ziel.Aufstellung = quelle.Aufstellung;
            ziel.Heizung = quelle.Heizung;
            return true;
        }

        // =================================================================================
        // Der Schreibweg der STAMMFELDER in die Projektkopie (Anwenderentscheid 16.09.2026)
        // =================================================================================

        /// <summary>
        /// Traegt die Stammfelder EINER Anlagenzeile in ihre PROJEKTKOPIE nach —
        /// <b>der Schritt NACH dem WizardCtrl-Weg</b>.
        /// </summary>
        /// <remarks>
        /// <para><b>Warum es diesen Schritt gibt.</b> Die Felder Hersteller, Beschreibung,
        /// Typ, Regelung, Aufstellung, Baujahr, Nennleistung und Heizstableistung standen
        /// im Waermepumpen-Anlagendialog bis zum 16.09.2026 fuer den KATALOGSATZ. Sie
        /// gehoeren der Anlage dieses Projekts — und der Rechenweg liest genau die Zeile
        /// in <c>Tab_WP</c> (<c>SimulationWaermepumpe.ModuleAufbauen</c>). Der
        /// Del+Add-Weg der Erzeuger schreibt aber nur <c>Tab_Energieanlagen</c>; die
        /// Gerätekopie ruehrt er nicht an (<c>CopyFromStamm</c> gibt bei vorhandener Kopie
        /// deren Id zurueck, ohne zu ueberschreiben). Ohne diesen Nachzug fiele jede
        /// Aenderung an den Stammfeldern still unter den Tisch.</para>
        ///
        /// <para><b>Warum NACH dem Add.</b> Eine frische Anlagenzeile traegt in
        /// <c>ID_WP</c> die KATALOG-Id, bis <c>Add_WP_Waermeerzeuger</c> die Kopie ueber
        /// <c>WPCtrl.CopyFromStamm</c> anlegt und <c>item.ID_WP</c> auf deren Id setzt.
        /// Davor geschrieben, traefe der Weg den falschen Satz — der Kern lehnt das
        /// benannt ab (<c>WP_PROJ_MSG_NICHT_GESPEICHERT</c>).</para>
        ///
        /// <para><b>Warum hier und nicht in einer Huelle.</b> Die drei Speicherwege der
        /// Anlage liegen in ZWEI Projekten: Startseite und Simulationskonfiguration in der
        /// Windows-Schale, der Simulationsreiter plattformfrei in <c>EPOS.UI.Daten</c>.
        /// Eine gemeinsame Stelle, die alle drei sehen, kann nur der Kern sein — und diese
        /// Klasse ist bereits die, die Stammfelder und Anlagenzeile zusammenbringt.</para>
        /// </remarks>
        /// <param name="quelle">Die Anlagenzeile; ihre Felder sind die Vorlage.</param>
        /// <param name="idProjekt">Das Projekt (<c>Tab_WP.ID_Projekt</c>).</param>
        /// <returns><c>null</c> = geschrieben oder nicht zustaendig; sonst der Grund im Klartext.</returns>
        internal static string ProjektgeraetNachziehen(WErzeugerModel quelle, int idProjekt)
        {
            if (quelle == null || idProjekt <= 0) return null;
            if (quelle.ID_Type != WizardItemClass.WP_TYP &&
                quelle.ID_Type != WizardItemClass.REF_WP_TYP) return null;
            if (quelle.ID_WP <= 0) return null;

            WPCtrl.SpeicherErgebnis ergebnis = WPCtrl.ProjektgeraetSchreiben(
                quelle.ID_WP, idProjekt,
                new WPCtrl.ProjektgeraetFelder(
                    Firma: quelle.Firma ?? "",
                    Beschreibung: quelle.Beschreibung ?? "",
                    Typ: quelle.Typ ?? "",
                    Regelung: quelle.Regelung ?? "",
                    Aufstellung: quelle.Aufstellung ?? "",
                    Baujahr: quelle.Baujahr,
                    Nennleistung: quelle.Nennleistung,
                    Heizung: (int)Math.Round(quelle.Heizung)));

            return ergebnis.Ok ? null : ergebnis.Meldung;
        }

        /// <summary>
        /// Dieselbe Nachfuehrung fuer die LISTE, die die Erzeugerwege am Stueck schreiben.
        /// Zeilen anderer Anlagenart werden uebergangen.
        /// </summary>
        /// <returns><c>null</c> = alles geschrieben; sonst der ERSTE Ablehnungsgrund.</returns>
        internal static string ProjektgeraeteNachziehen(
            System.Collections.Generic.IEnumerable<WErzeugerModel> modelle, int idProjekt)
        {
            if (modelle == null) return null;

            string erster = null;
            foreach (WErzeugerModel m in modelle)
            {
                string grund = ProjektgeraetNachziehen(m, idProjekt);
                if (grund != null && erster == null) erster = grund;
            }
            return erster;
        }

        /// <summary>
        /// Die Stammdaten EINES Geraets, ohne eine Anlagenzeile zu beruehren — dieselbe
        /// zweistufige Suche. Der Anlagendialog fuellt damit seine Anzeigefelder auf,
        /// nachdem der Stammdialog darueber gestanden hat (<c>Wizard_WPItem.btn_WP_Click</c>,
        /// Z. 594-616).
        /// </summary>
        /// <returns><c>null</c>, wenn es das Geraet nicht (mehr) gibt — der Vorlaeufer
        /// liess die Anzeige dann unveraendert stehen (Befund vom 26.08.2026).</returns>
        internal static WPModel Geraetedaten(int idWp)
        {
            if (idWp <= 0) return null;

            WPCtrl projekt = new WPCtrl();
            projekt.ReadAll("ID=" + idWp);
            if (projekt.items.Count > 0) return projekt.items[0];

            WPStammCtrl stamm = new WPStammCtrl();
            stamm.ReadAll("ID=" + idWp);
            return stamm.rows > 0 ? stamm.items[0] : null;
        }
    }
}
