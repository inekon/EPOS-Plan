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
            ziel.Heizung = quelle.Heizung;
            return true;
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
