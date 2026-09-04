using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Allgemein;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die SPRUNGBRÜCKE (iU9-W2.2) — die Windows-Seite von
    /// <see cref="Sprungziel"/>: Sie ordnet einem sprachneutralen Schlüssel eine
    /// WinForms-Maske zu und zeigt sie modal aus dem Rückruf einer
    /// Razor-Komponente heraus.
    ///
    /// <para><b>Wozu.</b> Ein Blazor-Dialog, der weiterführt, konnte bis hierher
    /// nur NACHGELAGERT springen: Die Komponente meldete den Wunsch im Ergebnis,
    /// die Hülle schloss den Dialog, öffnete das Ziel und brachte den Dialog
    /// danach zurück (Muster
    /// <see cref="BhkwWirtschaftlichkeitHuelle"/><c>.TarifOeffnen</c>, Etappe
    /// B5b, Befund O1). Das ist umständlich und für den Anwender sichtbar — das
    /// Fenster verschwindet und kommt wieder. Für ein <b>WinForms</b>-Ziel geht
    /// es einfacher.</para>
    ///
    /// <para><b>Warum das geht.</b> Der Blazor-Verteiler der
    /// <c>BlazorWebView</c> läuft im WinForms-Oberflächenfaden. Ein
    /// <c>ShowDialog()</c> aus einem Komponentenrückruf heraus öffnet dort
    /// dieselbe verschachtelte Nachrichtenschleife wie ein
    /// <c>OpenFileDialog</c> in einem <c>Click</c>-Ereignis: Der Blazor-Dialog
    /// bleibt stehen und pumpt weiter, das Zielfenster liegt modal darüber.
    /// Sicherheitshalber wird der Faden geprüft und notfalls über
    /// <see cref="Control.Invoke(Delegate)"/> gewechselt — die Zusicherung
    /// „Rückruf im Oberflächenfaden" steht nirgends geschrieben.</para>
    ///
    /// <para><b>Grenze (Risiko R1 des Wellenplans iU9).</b> Ziele, die selbst
    /// eine <c>BlazorDialogForm</c> sind, gehören NICHT hierher: Zwei WebViews
    /// übereinander kosten Speicher und Aufbauzeit und verwirren die
    /// Fokusreihenfolge (Risiko R2). Für sie bleibt der nachgelagerte Sprung,
    /// bis Welle 4 den Baustein <c>Ueberlagerung</c> bringt. Diese Klasse führt
    /// deshalb ausschließlich WinForms-Masken. <b>Am Gerät zu prüfen</b>
    /// (Abnahmepunkt W2‑7 im Protokoll): Öffnet sich der Katalog wirklich über
    /// dem Dialog, bleibt er modal, und steht der Dialog danach unverändert da?
    /// Fällt das durch, ist der Rückweg der nachgelagerte Sprung — er ist eine
    /// Zeile in der Hülle, nicht ein Umbau der Komponente.</para>
    ///
    /// <para><b>Muster.</b> Aufbau wie <c>Dienste.Navigation</c>
    /// (<see cref="WinFormsNavigation.OeffneMaske"/>) und <c>Masken</c>: ein
    /// <c>switch</c> über Schlüssel, eine Maske je Zweig, ein unbekannter
    /// Schlüssel tut nichts und liefert <c>false</c>.</para>
    /// </summary>
    internal static class Sprungbruecke
    {
        /// <summary>
        /// Der Delegat für den Parameter <c>Sprung</c> einer Razor-Komponente.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem das Ziel erscheint — in der
        /// Regel die <c>BlazorDialogForm</c> selbst. <c>null</c> ist erlaubt.</param>
        /// <returns>Ein Delegat, der zu einem Schlüssel aus <see cref="Sprungziel"/>
        /// das Fenster zeigt und meldet, ob es mit OK geschlossen wurde.</returns>
        /// <param name="lauf">
        /// Der gerechnete Simulationslauf — nur <see cref="Sprungziel.SpeicherOptimierung"/>
        /// braucht ihn (iU9-W11b.0). Ohne ihn bleibt dieser Zweig wirkungslos.
        /// </param>
        /// <param name="idProjekt">Das Projekt zum <paramref name="lauf"/>.</param>
        internal static Func<string, Task<bool>> Fuer(IWin32Window besitzer,
                                                      SimulationControl lauf = null,
                                                      int idProjekt = 0)
        {
            Control anker = besitzer as Control;

            return schluessel =>
            {
                if (anker != null && !anker.IsDisposed && anker.InvokeRequired)
                {
                    // Nicht im Oberflaechenfaden: hinueberwechseln. Invoke wartet,
                    // die Aufgabe ist danach schon fertig.
                    object antwort = anker.Invoke(
                        new Func<bool>(() => Zeigen(besitzer, schluessel, lauf, idProjekt)));
                    return Task.FromResult(antwort is bool b && b);
                }

                return Task.FromResult(Zeigen(besitzer, schluessel, lauf, idProjekt));
            };
        }

        /// <summary>
        /// Schlüssel → Maske. Ein unbekannter Schlüssel ist kein Fehler; er tut
        /// nichts (derselbe Umgang wie <see cref="WinFormsNavigation.OeffneMaske"/>).
        /// </summary>
        private static bool Zeigen(IWin32Window besitzer, string schluessel,
                                   SimulationControl lauf = null, int idProjekt = 0)
        {
            if (string.IsNullOrEmpty(schluessel)) return false;

            try
            {
                switch (schluessel)
                {
                    case Sprungziel.GesetzesparameterCo2:
                        using (Form_Gesetzesparameter f = new Form_Gesetzesparameter())
                        {
                            f.GewaehlteKlasse = DbWerte.GESETZ_KLASSE_CO2_PREIS;
                            return MitOk(f, besitzer);
                        }

                    case Sprungziel.Gesetzesparameter:
                        using (Form_Gesetzesparameter f = new Form_Gesetzesparameter())
                            return MitOk(f, besitzer);

                    // --- iU9-W14a: die FUENF Katalogverwaltungen sind WEG --------------
                    // Bis W14a standen hier fuenf Zweige: HeizkesselAdmin,
                    // StromspeicherAdmin, PvAdmin, PufferSpAdmin und - seit W10a.0c -
                    // PufferSpAdminNurLesen. Ihre Ziele sind jetzt
                    // selbst Blazor: Aus jedem Sprung ist eine UEBERLAGERUNG im
                    // selben Fenster geworden (Muster W4/W10a, Risiko R2), und die
                    // Aufrufer bekommen den Parametersatz der Verwaltung als
                    // VerwaltungGaben statt eines Sprungschluessels.

                    // --- iU9-W11b.0: die Auslegungsoptimierung des Stromspeichers ------
                    // Sie bleibt WinForms (iF22) - der einzige Ort des Programms, an dem
                    // ScottPlot laeuft (Heatmap und Schnittkurve der Rastersuche). Sie
                    // braucht als einzige Bruecke einen PARAMETER: den gerechneten Lauf,
                    // auf dem die Rastersuche arbeitet.
                    //
                    // ANTWORT ist hier NICHT "mit OK geschlossen", sondern
                    // AuslegungUebernommen - die Maske hat kein DialogResult, sondern
                    // eine fachliche Rueckgabe (woertlich wie
                    // Form_Simulation_Detail.SpOptimierung_Click:5992). Bei true liest
                    // die Ergebnisseite die Speichervariante neu.
                    case Sprungziel.SpeicherOptimierung:
                        if (lauf == null || idProjekt <= 0) return false;
                        using (Form_SpeicherOptimierung f = new Form_SpeicherOptimierung(lauf, idProjekt))
                        {
                            if (besitzer != null) f.ShowDialog(besitzer); else f.ShowDialog();
                            return f.AuslegungUebernommen;
                        }

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                // Ein gescheiterter Sprung darf den Dialog dahinter nicht mitreissen.
                MessageBox.Show(besitzer, ex.Message, Application.ProductName,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static bool MitOk(Form frm, IWin32Window besitzer)
        {
            return (besitzer != null ? frm.ShowDialog(besitzer) : frm.ShowDialog())
                   == DialogResult.OK;
        }
    }
}
