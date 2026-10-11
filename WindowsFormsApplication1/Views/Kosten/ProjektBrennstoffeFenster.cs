using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-ADAPTER von „Brennstoffe des Projekts…" (Anwenderentscheid 03.10.2026:
    /// Projektkopie des Brennstoffkatalogs) — mehr steuert Windows zu diesem Dialog nicht bei.
    ///
    /// <para><b>Die Hülle selbst ist plattformfrei</b> und liegt in
    /// <see cref="ProjektBrennstoffeHuelle"/> (<c>EPOS.UI.Daten</c>): Sie ruft
    /// <see cref="ProjektBrennstoffe"/> des Kerns für das offene Projekt. Hier steht nur das Fenster —
    /// eine <see cref="BlazorDialogForm{TKomponente}"/> um <see cref="ProjektBrennstoffeDialog"/>.
    /// Muster <see cref="KatalogabgleichFenster"/>.</para>
    /// </summary>
    internal static class ProjektBrennstoffeFenster
    {
        /// <summary>Zeigt „Brennstoffe des Projekts…" als modales Fenster.</summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <returns>true, wenn der Dialog etwas geschrieben hat.</returns>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            var huelle = new ProjektBrennstoffeHuelle();
            BlazorDialogForm<ProjektBrennstoffeDialog> dlg = null;

            var werte = new Dictionary<string, object>(huelle.Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<ProjektBrennstoffeDialog>(
                ProjektBrennstoffeHuelle.Titel(),
                new Size(ProjektBrennstoffeHuelle.FENSTER_BREITE, ProjektBrennstoffeHuelle.FENSTER_HOEHE),
                werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return huelle.Geaendert;
        }
    }
}
