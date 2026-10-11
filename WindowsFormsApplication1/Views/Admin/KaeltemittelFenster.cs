using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Admin;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-ADAPTER der Verwaltung „Kältemittel" (KM4-E3-c) — mehr steuert Windows zu diesem Dialog nicht bei.
    ///
    /// <para><b>Die Hülle selbst ist plattformfrei</b> und liegt in <see cref="KaeltemittelHuelle"/>
    /// (<c>EPOS.UI.Daten</c>). Hier steht nur das Fenster — eine <see cref="BlazorDialogForm{TKomponente}"/> um
    /// <see cref="KaeltemittelDialog"/>. Muster <see cref="NutzungsdauerFenster"/>.</para>
    /// </summary>
    internal static class KaeltemittelFenster
    {
        /// <summary>Zeigt die Verwaltung „Kältemittel" als modales Fenster.</summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <returns>true, wenn der Dialog etwas geschrieben hat.</returns>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            var huelle = new KaeltemittelHuelle();
            BlazorDialogForm<KaeltemittelDialog> dlg = null;

            var werte = new Dictionary<string, object>(huelle.Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<KaeltemittelDialog>(
                KaeltemittelHuelle.Titel(),
                new Size(KaeltemittelHuelle.FENSTER_BREITE, KaeltemittelHuelle.FENSTER_HOEHE),
                werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return huelle.Geaendert;
        }
    }
}
