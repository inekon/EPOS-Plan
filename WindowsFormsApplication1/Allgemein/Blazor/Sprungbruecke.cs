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

                    // --- iU9-W6.0d: die vier Katalogverwaltungen der Erzeugerdialoge ----
                    // Alle vier bleiben bis Welle 14 WinForms-Masken; nach der Rueckkehr
                    // laedt die Huelle die Katalogliste neu. Sie sind DIESELBEN Masken,
                    // die Dienste.Navigation fuer Masken.HeizkesselAdmin,
                    // Masken.PvAdmin und Masken.PufferSpAdmin zeigt - die Vorlaeufer
                    // riefen zwei davon ueber MenueCtrl.PV() bzw. MenueCtrl.PufferSp().
                    // Der Stromspeicher hatte kein Maskenkuerzel und oeffnete
                    // Form_AdminStromspeicher direkt.
                    case Sprungziel.HeizkesselAdmin:
                        using (Form_Heizkessel_Admin f = new Form_Heizkessel_Admin())
                            return MitOk(f, besitzer);

                    case Sprungziel.StromspeicherAdmin:
                        using (Form_AdminStromspeicher f = new Form_AdminStromspeicher())
                            return MitOk(f, besitzer);

                    case Sprungziel.PvAdmin:
                        using (Form_AdminPV f = new Form_AdminPV())
                            return MitOk(f, besitzer);

                    case Sprungziel.PufferSpAdmin:
                        using (Form_PufferSp_Admin f = new Form_PufferSp_Admin())
                            return MitOk(f, besitzer);

                    // --- iU9-W10a.0c: derselbe Katalog, aber NUR ZUM ANSEHEN -----------
                    // Der Knopf "Katalog ansehen" der Pufferspeicher-Verwaltung auf
                    // Projektebene (Form_PufferSp_Projekt.btnKatalog_Click:1596) setzte
                    // m_bReadOnly = true, bevor er die Maske zeigte. Ohne dieses
                    // Kennzeichen waere aus dem Nachschlagen das Bearbeiten des
                    // Auslieferungskatalogs geworden (Befund W10-B28) - deshalb ein
                    // eigener Zweig und nicht der Schluessel darueber.
                    case Sprungziel.PufferSpAdminNurLesen:
                        using (Form_PufferSp_Admin f = new Form_PufferSp_Admin())
                        {
                            f.m_bReadOnly = true;
                            return MitOk(f, besitzer);
                        }

                    // --- iU9-W7.0f: die Stammdaten der Solarthermieganglinien ----------
                    // Dieselbe Maske, die Dienste.Navigation fuer Masken.SolarganglinieAdmin
                    // zeigt; der Vorlaeufer rief sie ueber MenueCtrl.Solarganglinie(). Sie
                    // bleibt bis Welle 14b WinForms. Nach der Rueckkehr laedt der Dialog
                    // seine Katalogliste neu - der Anwender kann dort etwas geaendert und
                    // mit Abbrechen geschlossen haben (A-19 aus Welle 6).
                    case Sprungziel.SolarganglinieAdmin:
                        using (Form_Solarganglinie_Admin f = new Form_Solarganglinie_Admin())
                            return MitOk(f, besitzer);

                    // --- iU9-W9.0f: die Verwaltung der externen Waermebedarfsganglinien -
                    // Dieselbe Maske, die Dienste.Navigation fuer
                    // Masken.WaermebedarfExternAdmin zeigt; der Vorlaeufer
                    // (Form_Waermebedarf.btn_Bearbeiten_Click:257) rief sie unmittelbar
                    // samt SetControls(). Sie bleibt bis Welle 13 WinForms. Nach der
                    // Rueckkehr laedt der Dialog seine Katalogliste neu.
                    case Sprungziel.WaermebedarfExternAdmin:
                        using (Form_AdminWaermeeinlesen f = new Form_AdminWaermeeinlesen())
                        {
                            f.SetControls();
                            return MitOk(f, besitzer);
                        }

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
