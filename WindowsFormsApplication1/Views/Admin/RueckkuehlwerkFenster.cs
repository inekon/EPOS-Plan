using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-ADAPTER der Verwaltung „Rückkühlwerke" (K-F5a) — mehr steuert Windows zu diesem Dialog nicht bei.
    ///
    /// <para><b>Die Hülle selbst ist plattformfrei</b> und liegt in <see cref="RueckkuehlwerkHuelle"/>
    /// (<c>EPOS.UI.Daten</c>). Hier steht nur das Fenster — eine <see cref="BlazorDialogForm{TKomponente}"/> um
    /// <see cref="RueckkuehlwerkDialog"/>. Muster <see cref="KaeltemittelFenster"/>.</para>
    /// </summary>
    internal static class RueckkuehlwerkFenster
    {
        /// <summary>Zeigt die Verwaltung „Rückkühlwerke" als modales Fenster.</summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <returns>true, wenn der Dialog etwas geschrieben hat.</returns>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            var huelle = new RueckkuehlwerkHuelle();
            BlazorDialogForm<RueckkuehlwerkDialog> dlg = null;

            var werte = new Dictionary<string, object>(huelle.Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<RueckkuehlwerkDialog>(
                RueckkuehlwerkHuelle.Titel(),
                new Size(RueckkuehlwerkHuelle.FENSTER_BREITE, RueckkuehlwerkHuelle.FENSTER_HOEHE),
                werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return huelle.Geaendert;
        }
    }
}
