using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Admin;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-ADAPTER von „Katalog aktualisieren…" (Entscheidungsvorlage Modellgrenzen KU1
    /// Stufe 1) — mehr steuert Windows zu diesem Dialog nicht bei.
    ///
    /// <para><b>Die Hülle selbst ist plattformfrei</b> und liegt in
    /// <see cref="KatalogabgleichHuelle"/> (<c>EPOS.UI.Daten</c>): Sie ruft den Katalogabgleich des
    /// Kerns. Hier steht nur das Fenster — eine <see cref="BlazorDialogForm{TKomponente}"/> um
    /// <see cref="KatalogabgleichDialog"/>. Muster <see cref="NutzungsdauerFenster"/>.</para>
    /// </summary>
    internal static class KatalogabgleichFenster
    {
        /// <summary>Zeigt „Katalog aktualisieren…" als modales Fenster.</summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <returns>true, wenn der Dialog etwas geschrieben hat.</returns>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            var huelle = new KatalogabgleichHuelle();
            BlazorDialogForm<KatalogabgleichDialog> dlg = null;

            var werte = new Dictionary<string, object>(huelle.Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<KatalogabgleichDialog>(
                KatalogabgleichHuelle.Titel(),
                new Size(KatalogabgleichHuelle.FENSTER_BREITE, KatalogabgleichHuelle.FENSTER_HOEHE),
                werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return huelle.Geaendert;
        }
    }
}
