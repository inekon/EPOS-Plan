using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Projekt;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das WINDOWS-FENSTER von „Projekt Speichern unter" (iU9-W15a.4) — seit Auftrag
    /// KI‑F8 nur noch der Adapter.
    ///
    /// <para><b>Die Datenseite liegt in <c>EPOS.UI.Daten</c></b>
    /// (<see cref="ProjektKopieHuelle"/>): Namensliste, Vorprüfung, Kopierlauf und die
    /// drei Verwaltungsfelder. Auf iOS zeigt die <c>AppWurzel</c> dieselbe Komponente
    /// als Ansicht.</para>
    /// </summary>
    internal static class ProjektKopieFenster
    {
        /// <summary>
        /// Gewünschtes Innenmaß: das Inhaltsmaß der Projektdialoge
        /// (<see cref="EPOS.UI.Dienste.Fenstermass.Projektdialog"/>), in CSS-Pixeln.
        /// </summary>
        private static readonly Size MASS = new Size(
            EPOS.UI.Dienste.Fenstermass.Projektdialog.Breite,
            EPOS.UI.Dienste.Fenstermass.Projektdialog.Hoehe);

        /// <summary>
        /// Öffnet den Dialog. Rückgabe <c>true</c>, wenn dupliziert wurde —
        /// <c>WinFormsNavigation</c> reicht das als Ergebnis weiter (der Vorläufer
        /// wertete ebenfalls nur das <c>DialogResult</c> aus).
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<ProjektKopieDialog> dlg = null;

            var werte = new Dictionary<string, object>(ProjektKopieHuelle.Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<ProjektKopieDialog>(
                ProjektKopieHuelle.Titel(), MASS, werte, EPOS.UI.Dienste.Dialogart.Inhaltsmass);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }
    }
}
