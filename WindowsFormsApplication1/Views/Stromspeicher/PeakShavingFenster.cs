using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Strom;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das WINDOWS-FENSTER der Lastspitzenkappung (iU9-W12.6) — seit Auftrag KI‑F8
    /// nur noch der Adapter.
    ///
    /// <para><b>Die Datenseite liegt in <c>EPOS.UI.Daten</c></b>
    /// (<see cref="PeakShavingHuelle"/>): Ganglinien, Import, die zwei Rechenläufe,
    /// das Zeichenmodell und der CSV-Auszug. Auf iOS zeigt die <c>AppWurzel</c>
    /// dieselbe Komponente als Ansicht.</para>
    ///
    /// <para><b>Der Rückgabewert ist immer <c>false</c></b> — Befund W12-B24: Der
    /// einzige Fußknopf des Vorläufers trug <c>DialogResult.Cancel</c>, und
    /// <c>MitOk(frm)</c> in <c>WinFormsNavigation</c> lieferte deshalb nie
    /// <c>true</c>. Das bleibt so; niemand wertet es aus.</para>
    /// </summary>
    internal static class PeakShavingFenster
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 1 060 × 830).</summary>
        private static readonly Size MASS = new Size(1100, 860);

        /// <summary>
        /// Zeigt die Lastspitzenkappung als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> (<c>Masken.PeakShaving</c>).
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="projektId">Das Projekt; 0 = ohne Projekt.</param>
        /// <returns>Immer <c>false</c> (Befund W12-B24).</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId)
        {
            BlazorDialogForm<PeakShavingDialog> dlg = null;

            var werte = new Dictionary<string, object>(new PeakShavingHuelle(projektId).Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<PeakShavingDialog>(
                PeakShavingHuelle.Titel(), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return false;
        }
    }
}
