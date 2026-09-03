using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Dialogs „Tarifstruktur Strom" (iU9-W2.3).
    ///
    /// <para>Der Dialog lebt als Razor-Komponente
    /// <see cref="TarifstrukturDialog"/> in <c>EPOS.UI</c>; die WinForms-Fassung
    /// <c>Form_Tarifstruktur</c> ist mit demselben Schritt GELÖSCHT (Regel M1).
    /// Vorbild dieser Klasse ist <see cref="BhkwWirtschaftlichkeitHuelle"/>:
    /// Datenseite hier, Anzeige dort.</para>
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Geladen wird mit
    /// <c>WirtschaftlichkeitCtrl.LadeTarif(idStamm)</c> — demselben Aufruf, den
    /// der Konstruktor der Maske machte —, geschrieben mit
    /// <c>SpeichereTarif</c>. Die Komponente kennt keine Datenbank
    /// (Hausregel <c>EPOS.UI/CLAUDE.md</c>).</para>
    ///
    /// <para><b>Die Sicht bleibt Ä18.</b> Es gilt EIN Tarifsatz je Stamm; die
    /// <see cref="TarifSicht"/> bestimmt nur, welche Blöcke der Dialog baut und
    /// damit auch überschreibt. Die Aufzählung ist mit dem Port nach
    /// <c>EPOS.UI</c> gewandert (sie stand im Quelltext der Maske) und heißt
    /// unverändert.</para>
    /// </summary>
    internal static class TarifstrukturHuelle
    {
        /// <summary>Gewünschtes Innenmaß. Die Maske maß 620 px breit; die Hülle
        /// klemmt auf den Bildschirm, gescrollt wird in der Komponente.</summary>
        private const int FENSTER_BREITE = 760;

        /// <summary>
        /// Zeigt den Dialog in der vollen Sicht.
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer, int idStamm)
        {
            return Oeffnen(besitzer, idStamm, TarifSicht.Komplett);
        }

        /// <summary>
        /// Zeigt den Dialog in einer Komponentensicht (Ä18).
        /// </summary>
        /// <returns><c>true</c>, wenn gespeichert wurde — dann rechnet der
        /// Aufrufer neu (Bestandsverhalten von <c>Form_Tarifstruktur.Gespeichert</c>).</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int idStamm, TarifSicht sicht)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            TarifParameter tarif = ctrl.LadeTarif(idStamm);

            bool gespeichert = false;
            BlazorDialogForm<TarifstrukturDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["Tarif"] = tarif,
                ["Sicht"] = sicht,

                // Der Schreibweg. Die Komponente hat den Bildschirmzustand
                // unmittelbar davor in denselben Satz uebernommen.
                ["Speichern"] = new Func<bool>(() =>
                {
                    try { return ctrl.SpeichereTarif(tarif); }
                    catch { return false; }
                }),

                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), ok =>
                {
                    gespeichert = ok;
                    if (dlg != null) dlg.Schliessen(ok);
                })
            };

            int hoehe = Math.Max(420, Screen.PrimaryScreen.WorkingArea.Height - 90);
            dlg = new BlazorDialogForm<TarifstrukturDialog>(
                Titel(sicht), new Size(FENSTER_BREITE, hoehe), werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return gespeichert;
        }

        /// <summary>Fenstertitel — derselbe Text wie in der Komponente.</summary>
        private static string Titel(TarifSicht sicht)
        {
            return new TarifstrukturTexte().Titel(sicht);
        }
    }
}
