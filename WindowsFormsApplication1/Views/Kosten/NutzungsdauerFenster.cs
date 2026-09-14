using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-ADAPTER der Nutzungsdauern (AfA) — mehr steuert Windows zu
    /// diesem Dialog nicht bei.
    ///
    /// <para><b>Die Hülle selbst ist plattformfrei</b> und liegt in
    /// <see cref="NutzungsdauerHuelle"/> (<c>EPOS.UI.Daten</c>): Sie lädt und
    /// schreibt über <c>NutzungsdauerCtrl</c> und kennt keine Plattform. Hier
    /// steht nur das Fenster — eine <see cref="BlazorDialogForm{TKomponente}"/> um
    /// <see cref="NutzungsdauerDialog"/>, gebaut aus dem Parametersatz der Hülle.
    /// Wortgleich zu <see cref="EnergietraegerFenster"/>; auf iOS zeigt dieselbe
    /// Hülle dieselbe Komponente ohne diesen Adapter.</para>
    /// </summary>
    internal static class NutzungsdauerFenster
    {
        /// <summary>
        /// Zeigt die Nutzungsdauerverwaltung als modales Fenster.
        /// </summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <returns>true, wenn der Dialog etwas geschrieben hat.</returns>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            // Die Hüllen-INSTANZ hält den Bearbeitungsstand; sie lebt über die
            // Rückrufe ihres Parametersatzes so lange wie das Fenster.
            var huelle = new NutzungsdauerHuelle();

            BlazorDialogForm<NutzungsdauerDialog> dlg = null;

            var werte = new Dictionary<string, object>(huelle.Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<NutzungsdauerErgebnis?>(
                    new object(), erg =>
                    {
                        if (dlg != null) dlg.Schliessen(erg.HasValue);
                    })
            };

            dlg = new BlazorDialogForm<NutzungsdauerDialog>(
                NutzungsdauerHuelle.Titel(),
                new Size(NutzungsdauerHuelle.FENSTER_BREITE, NutzungsdauerHuelle.FENSTER_HOEHE),
                werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }

            return huelle.Geaendert;
        }
    }
}
