using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Hilfe;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-ADAPTER von „Hilfe → Fernwartung…" (Konzept Administration und Support,
    /// Abschnitt 4.2). Die Hülle ist plattformfrei (<see cref="FernwartungHuelle"/>,
    /// <c>EPOS.UI.Daten</c>); hier stehen nur das Fenster und die drei Rückrufe der Plattform:
    /// Adresse öffnen über <c>Dienste.Datei.AdresseOeffnen</c>, Kopieren über die
    /// Zwischenablage und Quick Assist starten über <see cref="QuickAssistStart"/>. Muster
    /// <see cref="KatalogabgleichFenster"/>.
    /// </summary>
    internal static class FernwartungFenster
    {
        /// <summary>Zeigt „Fernwartung" als modales Fenster.</summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        internal static void Oeffnen(IWin32Window besitzer)
        {
            BlazorDialogForm<FernwartungDialog> dlg = null;

            // Die Zwischenablage verlangt den STA-Faden der Oberfläche; kommt der Klick auf einem
            // anderen Faden an, wird er auf den des Fensters gereicht.
            Action<string> kopieren = text =>
            {
                if (dlg != null && dlg.InvokeRequired) dlg.Invoke(new Action(() => Kopieren(text)));
                else Kopieren(text);
            };

            var werte = new Dictionary<string, object>(FernwartungHuelle.Gaben(
                Application.ProductVersion, AdresseOeffnen, kopieren, QuickAssistStart.Starten))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<FernwartungDialog>(
                FernwartungHuelle.Titel(),
                new Size(FernwartungHuelle.FENSTER_BREITE, FernwartungHuelle.FENSTER_HOEHE),
                werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }

        /// <summary>Öffnet die Adresse im Standardprogramm; eine Ausnahme heißt „misslungen".</summary>
        private static bool AdresseOeffnen(string adresse)
        {
            try { return Dienste.Datei.AdresseOeffnen(adresse); }
            catch { return false; }
        }

        /// <summary>
        /// Text in die Zwischenablage, auf dem STA-Faden der Oberfläche (wie
        /// <c>EntwicklungHuelle.DiagnosedatenKopieren</c>). Eine Ausnahme meldet der Dialog.
        /// </summary>
        private static void Kopieren(string text) => Clipboard.SetText(string.IsNullOrEmpty(text) ? " " : text);
    }
}
