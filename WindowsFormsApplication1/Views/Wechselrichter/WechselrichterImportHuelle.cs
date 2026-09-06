using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Photovoltaik;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des CEC-Wechselrichterimports (Anwenderentscheid
    /// <b>W6‑E‑2</b> vom 06.09.2026, Stufe S1.5 des
    /// <c>Konzept_Wechselrichter_EPOS-Plan.md</c>).
    ///
    /// <para><b>Zwilling zu <see cref="PvModulImportHuelle"/>, Zeile für Zeile.</b> Die
    /// Netzseite kommt aus <see cref="CecWechselrichterDienst"/>, der Schreibweg aus
    /// <see cref="WechselrichterStammCtrl"/>, die Vorprüfung aus derselben
    /// <see cref="DublettenPruefung"/> wie bei den vier VDI-Importen und beim
    /// Modulimport.</para>
    ///
    /// <para><b>Der Netzabruf läuft in <c>Task.Run</c></b> (dieselbe Auflage wie beim
    /// Modulimport, Risiko R‑W13‑3): Zwei URLs mit je 45 Sekunden Zeitgrenze sind im
    /// schlechtesten Fall anderthalb Minuten, in denen der Anwender nichts tun könnte.
    /// Der Fortschrittsmelder samt Abbruch hängt am Baustein <c>Fortschritt</c>.</para>
    ///
    /// <para><b>Der Dienst lebt so lange wie der Dialog</b> — er entsteht in
    /// <see cref="Gaben"/> und nicht als statisches Feld (Lehre aus Befund W13‑B46:
    /// Die PAN-Sitzungsliste überlebte das Schließen der Maske und den
    /// Projektwechsel).</para>
    /// </summary>
    internal static class WechselrichterImportHuelle
    {
        /// <summary>Gewünschtes Innenmaß — wie beim Modulimport.</summary>
        private static readonly Size MASS = new Size(1240, 800);

        /// <summary>
        /// Zeigt den Import als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> (<c>Masken.WechselrichterImport</c>).
        /// </summary>
        /// <returns><c>true</c>, wenn etwas geschrieben wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<WechselrichterImportDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<WechselrichterImportDialog>(
                MyResource.Resource.WRK_IMP_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Der PARAMETERSATZ der Komponente.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            var dienst = new CecWechselrichterDienst();

            return new Dictionary<string, object>
            {
                ["Laden"] = new Func<IProgress<CecFortschritt>, CancellationToken,
                                     Task<WrLeseErgebnis>>(
                    (melder, abbruch) => Laden(dienst, melder, abbruch)),
                ["Vorpruefen"] = new Func<CecWechselrichter, Task<PvVorpruefung>>(Vorpruefen),
                ["Anlegen"] = new Func<CecWechselrichter, string, Task<bool>>(Anlegen),
                ["Ueberschreiben"] = new Func<CecWechselrichter, int, Task<bool>>(Ueberschreiben),
                ["Meldungstext"] = new Func<CecFortschritt, string>(PvModulImportHuelle.Meldungstext)
            };
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        private static Task<WrLeseErgebnis> Laden(CecWechselrichterDienst dienst,
                                                  IProgress<CecFortschritt> melder,
                                                  CancellationToken abbruch)
        {
            return Task.Run(async () =>
            {
                var r = await dienst.LadenAsync(melder, abbruch).ConfigureAwait(false);
                return r.Erfolg
                    ? new WrLeseErgebnis(true, dienst.AlleGeraete, r.Meldung)
                    : new WrLeseErgebnis(false, null, r.Meldung);
            }, abbruch);
        }

        /// <summary>
        /// Die Vorprüfung des gewählten Geräts gegen <c>Tab_Wechselrichter_STAMM</c> —
        /// dieselbe <see cref="DublettenPruefung"/> wie beim Modulimport, mit genau
        /// EINEM Kandidaten.
        /// </summary>
        private static Task<PvVorpruefung> Vorpruefen(CecWechselrichter geraet)
        {
            return Task.Run(() =>
            {
                KatalogDefinition katalog = KatalogRegistry.Finde("WECHSELRICHTER");

                var kandidat = new ImportKandidat { Name = geraet.Name, Tag = null };
                foreach (KeyValuePair<string, object> paar in geraet.Vergleichswerte(geraet.Name))
                    kandidat.Werte[paar.Key] = paar.Value;

                List<ImportPruefung> pruefungen = DublettenPruefung.PruefeKandidaten(
                    katalog, new List<ImportKandidat> { kandidat });

                WechselrichterPlausibilitaet.Befund plausi =
                    WechselrichterPlausibilitaet.Pruefe(geraet.NachModell());

                return new PvVorpruefung(
                    pruefungen.Count > 0 ? pruefungen[0].Befund : ImportBefund.Neu,
                    pruefungen,
                    DublettenPruefung.VergebeneNamen(katalog),
                    plausi.Ok && plausi.Warnungen.Count == 0
                        ? "" : WechselrichterPlausibilitaet.Meldung(plausi),
                    !plausi.Ok);
            });
        }

        /// <summary>Legt das Gerät als neuen Katalogsatz an.</summary>
        private static Task<bool> Anlegen(CecWechselrichter geraet, string name)
        {
            return Task.Run(() =>
            {
                try
                {
                    WechselrichterModel m = geraet.NachModell();
                    if (!string.IsNullOrEmpty(name)) m.m_szName = name;
                    return new WechselrichterStammCtrl().InsertFrom(m);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Speichern des Wechselrichters: " + ex.Message);
                    return false;
                }
            });
        }

        /// <summary>
        /// Aktualisiert genau die Importfelder des Bestandssatzes — Id, Bezeichner,
        /// Beschreibung und die Anwenderkosten bleiben stehen (Dublettenkonzept 4.2).
        /// </summary>
        private static Task<bool> Ueberschreiben(CecWechselrichter geraet, int bestandsId)
        {
            return Task.Run(() =>
            {
                try
                {
                    var ctrl = new WechselrichterStammCtrl();
                    geraet.NachModell(ctrl);
                    return ctrl.UpdateImport(bestandsId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Aktualisieren des Wechselrichters: " + ex.Message);
                    return false;
                }
            });
        }
    }
}
