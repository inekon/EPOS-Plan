using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das WINDOWS-FENSTER des Katalogdialogs „Brauchwasser-Nutzungsarten" (Umsetzungskonzept
    /// Zapfprofilgenerator 5.4, 5.5; Stufe Z4, Gruppe 3) — nur der Adapter.
    ///
    /// <para><b>Die Datenseite liegt in <c>EPOS.UI.Daten</c></b>
    /// (<see cref="ZapfprofilHuelle.KatalogGaben"/>): Katalog, Stammblatt, Editor, Löschen, die
    /// Editoren des Zapfprofils und der Katalogimport samt Dateiwahl über <c>Dienste.Datei</c>.
    /// <b>Auf iOS läuft derselbe Dialog als freie Ansicht der <c>AppWurzel</c></b>
    /// (Anwenderentscheid ZU26, N23): <c>IosProjektQuelle.NutzungsartKatalogGaben</c> holt DENSELBEN
    /// Parametersatz; dort nimmt der Import allein ein ZIP-Archiv, weil iOS keinen Ordnerdialog
    /// kennt. Es ist der einzige Katalog, der auf iOS aufgeht — die acht übrigen
    /// Katalogverwaltungen lehnt die Wurzel benannt ab (KI-D-Q10).</para>
    ///
    /// <para><b>Unter Windows bleibt es bei diesem Fenster</b>: <c>WinFormsNavigation</c> fängt
    /// <c>Masken.BrauchwasserNutzungsarten</c> ab, bevor der Menüweg die Wurzel erreicht.</para>
    /// </summary>
    internal static class TwwNutzungsartAdminHuelle
    {
        /// <summary>Gewünschtes Innenmaß — Liste, Stammblatt und die breiten Überlagerungen der Editoren.</summary>
        private static readonly Size MASS = new Size(1100, 700);

        /// <summary>
        /// Zeigt den Katalog als eigenes Fenster — der Weg von <c>WinFormsNavigation</c>
        /// (<c>Masken.BrauchwasserNutzungsarten</c>).
        /// </summary>
        /// <returns>Immer <c>true</c>: Der Katalog kennt nur „Beenden" — jede Aktion schreibt sofort.</returns>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<TwwNutzungsartAdminDialog> dlg = null;

            var werte = new Dictionary<string, object>(ZapfprofilHuelle.KatalogGaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<TwwNutzungsartAdminDialog>(
                ZapfprofilHuelle.KatalogTexte().Titel, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }
    }
}
