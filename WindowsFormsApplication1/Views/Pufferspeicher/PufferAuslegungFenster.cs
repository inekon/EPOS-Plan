using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Seiten.Pufferspeicher;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-WEG der Naht <see cref="Pufferauslegungswege.Fenster"/> (Konzept
    /// Pufferspeicher-Auslegung, Stufe P2): zeigt die Ansicht <c>PufferAuslegungSeite</c> modal in
    /// einem eigenen Fenster. Gebraucht wird er, wo der Einstieg selbst in einem Fenster steht —
    /// „An Speicherauslegung übergeben…" im Zapfprofil, das im Bedarfsprofil-Fenster liegt. Die
    /// Konfiguration und die Kachel nehmen die freie Ansicht des Hauptfensters.
    /// </summary>
    internal static class PufferAuslegungFenster
    {
        /// <summary>Gewünschtes Innenmaß — eine Fachmaske, die mitwächst.</summary>
        private static readonly Size MASS = new Size(1180, 820);

        /// <summary>Hängt den Weg in die Naht ein (<c>Program.Main</c>).</summary>
        internal static void Einhaengen()
        {
            Pufferauslegungswege.Fenster = Oeffnen;
        }

        /// <summary>Zeigt den Parametersatz modal; „← zurück" und das Kreuz schließen das Fenster.</summary>
        internal static Task Oeffnen(IReadOnlyDictionary<string, object> gaben)
        {
            if (gaben == null) return Task.CompletedTask;
            BlazorDialogForm<PufferAuslegungSeite> dlg = null;
            var werte = new Dictionary<string, object>(gaben)
            {
                ["Geschlossen"] = EventCallback.Factory.Create(new object(), () =>
                {
                    if (dlg != null) dlg.Schliessen(true);
                })
            };
            dlg = new BlazorDialogForm<PufferAuslegungSeite>(MyResource.Resource.PAUS_TITEL, MASS, werte);
            using (dlg)
            {
                Form besitzer = Form.ActiveForm;
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return Task.CompletedTask;
        }
    }
}
