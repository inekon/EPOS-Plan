using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Strom;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das WINDOWS-FENSTER der Stromganglinien-Verwaltung (iU9-W12.4) — seit Auftrag
    /// KI‑F8 nur noch der Adapter.
    ///
    /// <para><b>Die Datenseite liegt in <c>EPOS.UI.Daten</c></b>
    /// (<see cref="StromganglinieAdminHuelle"/>): Katalog, Löschen, Dateiwahl und die
    /// Importkette. Auf iOS zeigt die <c>AppWurzel</c> dieselbe Komponente als
    /// Ansicht.</para>
    /// </summary>
    internal static class StromganglinieAdminFenster
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 664 × 316).</summary>
        private static readonly Size MASS = new Size(880, 620);

        /// <summary>
        /// Zeigt die Verwaltung als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> (<c>Masken.StromganglinieAdmin</c>) und von
        /// <c>MenueCtrl.Stromganglinie</c>.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<StromganglinieAdminDialog> dlg = null;

            var werte = new Dictionary<string, object>(StromganglinieAdminHuelle.Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<StromganglinieAdminDialog>(
                StromganglinieAdminHuelle.Titel(), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }
    }
}
