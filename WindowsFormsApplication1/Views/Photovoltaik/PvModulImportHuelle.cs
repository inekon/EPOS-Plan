using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Photovoltaik;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des PV-Modulimports (iU9-W13.3).
    ///
    /// <para><b>Die Datenbank- und Netzseite steht hier, nicht in der Komponente.</b>
    /// Die CEC-Modulliste kommt aus <see cref="CECDataService"/>, die PAN-Dateien
    /// aus <see cref="PanDataService"/>, der Schreibweg aus
    /// <see cref="PhotovoltaikStammCtrl"/>.</para>
    ///
    /// <para><b>Der Netzabruf läuft in <c>Task.Run</c></b> (Risiko R‑W13‑3): Drei
    /// URLs mit je 45 Sekunden Zeitgrenze sind im schlechtesten Fall über zwei
    /// Minuten. Der Melder war schon da — die Maske übergab ihn nur nicht
    /// (Befund W13‑B38); jetzt hängt der Baustein <c>Fortschritt</c> daran, samt
    /// Abbrechen.</para>
    ///
    /// <para><b>Der PAN-Dienst lebt so lange wie der Dialog.</b> Seine Liste war
    /// statisch und überlebte damit das Schließen der Maske und den
    /// Projektwechsel (Befund W13‑B46). Das Sammeln mehrerer <c>.pan</c>-Dateien
    /// einer Sitzung bleibt Absicht; es ist die Lebensdauer, die falsch war.</para>
    /// </summary>
    internal static class PvModulImportHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 1 216 × 758).</summary>
        private static readonly Size MASS = new Size(1240, 800);

        /// <summary>
        /// Zeigt den Modulimport als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> (<c>Masken.PvImport</c>) mit dem Argument
        /// <c>"CEC"</c> oder <c>"PAN"</c>.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="quelle">Die Quelle, mit der der Dialog aufmacht.</param>
        /// <returns><c>true</c>, wenn etwas geschrieben wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, string quelle)
        {
            bool ok = false;
            BlazorDialogForm<PvModulImportDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(quelle))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<PvModulImportDialog>(
                MyResource.Resource.PVIMP_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ der Komponente. Die beiden Dienste entstehen HIER und
        /// leben damit so lange wie der Dialog.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(string quelle)
        {
            CECDataService cec = new CECDataService();
            PanDataService pan = new PanDataService();

            return new Dictionary<string, object>
            {
                ["Quelle"] = string.IsNullOrEmpty(quelle) ? "CEC" : quelle,
                ["CecLaden"] = new Func<IProgress<CecFortschritt>, CancellationToken,
                                        Task<PvLeseErgebnis>>(
                    (melder, abbruch) => CecLaden(cec, melder, abbruch)),
                ["PanWaehlen"] = new Func<Task<string>>(PanWaehlen),
                ["PanLaden"] = new Func<string, Task<PvLeseErgebnis>>(pfad => PanLaden(pan, pfad)),
                ["Vorpruefen"] = new Func<UnifiedModule, Task<PvVorpruefung>>(Vorpruefen),
                ["Anlegen"] = new Func<UnifiedModule, string, Task<bool>>(Anlegen),
                ["Ueberschreiben"] = new Func<UnifiedModule, int, Task<bool>>(Ueberschreiben),
                ["Meldungstext"] = new Func<CecFortschritt, string>(Meldungstext)
            };
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        /// <summary>
        /// Die CEC-Modulliste — aus dem Zwischenspeicher oder über HTTP, im
        /// Hintergrund und abbrechbar.
        /// </summary>
        private static Task<PvLeseErgebnis> CecLaden(
            CECDataService dienst, IProgress<CecFortschritt> melder, CancellationToken abbruch)
        {
            return Task.Run(async () =>
            {
                var r = await dienst.LoadDataAsync(melder, abbruch).ConfigureAwait(false);
                if (!r.success) return new PvLeseErgebnis(false, null, r.meldung);

                var module = dienst.AllModules.Select(UnifiedModule.FromPanCec).ToList();
                return new PvLeseErgebnis(true, module, r.meldung);
            }, abbruch);
        }

        /// <summary>
        /// Der Dateiwähler für eine <c>.pan</c>-Datei — HINTER dem Blazor-Ereignis
        /// (Befund W13‑B‑1, siehe <c>IDateiDienst</c>).
        /// </summary>
        private static Task<string> PanWaehlen()
        {
            string ordner = Path.Combine(Properties.Settings.Default.VDI3805Path ?? "", "PAN");
            return Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.PVIMP_TITEL, "(*.pan)|*.pan", ordner);
        }

        /// <summary>
        /// Liest eine <c>.pan</c>-Datei und nimmt sie in die Sitzungsliste auf.
        ///
        /// <para><b>ANSI (Windows-1252) ausdrücklich</b> — wörtlich wie der
        /// Vorläufer (<c>_btnPAN_Click</c> :622): PVsyst schreibt seine Dateien
        /// nicht in UTF-8, und ein Herstellername mit Umlaut würde sonst zu
        /// U+FFFD.</para>
        ///
        /// <para><b>Der Dateiname reist mit</b> (Befund W13‑B45): Der Vorläufer
        /// rief <c>ParsePan(inhalt)</c> ohne ihn und ließ <c>SourceFile</c>
        /// leer.</para>
        /// </summary>
        private static Task<PvLeseErgebnis> PanLaden(PanDataService dienst, string pfad)
        {
            return Task.Run(() =>
            {
                try
                {
                    string inhalt = File.ReadAllText(pfad, AnsiEncoding.Get());
                    dienst.Einlesen(inhalt, Path.GetFileName(pfad));

                    var module = dienst.AllModules.Select(UnifiedModule.FromPanCec).ToList();
                    return new PvLeseErgebnis(true, module,
                        new CecFortschritt("PAN_MSG_GELESEN",
                            module.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
                catch (Exception ex)
                {
                    return new PvLeseErgebnis(false, null,
                        new CecFortschritt("PAN_MSG_LESEFEHLER", ex.Message));
                }
            });
        }

        /// <summary>
        /// Die Vorprüfung des gewählten Moduls gegen <c>Tab_PV_STAMM</c> — dieselbe
        /// <see cref="DublettenPruefung"/> wie bei den vier VDI-Importen, nur mit
        /// genau EINEM Kandidaten: Der PV-Import ist der einzige der Welle ohne
        /// Mehrfachauswahl.
        /// </summary>
        private static Task<PvVorpruefung> Vorpruefen(UnifiedModule modul)
        {
            return Task.Run(() =>
            {
                KatalogDefinition katalog = KatalogRegistry.Finde("PV");

                var kandidat = new ImportKandidat { Name = modul.Name, Tag = null };
                foreach (var paar in modul.Vergleichswerte(modul.Name))
                    kandidat.Werte[paar.Key] = paar.Value;

                List<ImportPruefung> pruefungen = DublettenPruefung.PruefeKandidaten(
                    katalog, new List<ImportKandidat> { kandidat });

                return new PvVorpruefung(
                    pruefungen.Count > 0 ? pruefungen[0].Befund : ImportBefund.Neu,
                    pruefungen,
                    DublettenPruefung.VergebeneNamen(katalog));
            });
        }

        /// <summary>Legt das Modul als neuen Katalogsatz an.</summary>
        private static Task<bool> Anlegen(UnifiedModule modul, string name)
        {
            return Task.Run(() =>
            {
                try
                {
                    PhotovoltaikModel model = modul.NachModell();
                    if (!string.IsNullOrEmpty(name)) model.m_szName = name;
                    return new PhotovoltaikStammCtrl().InsertFrom(model);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Speichern des PV-Moduls: " + ex.Message);
                    return false;
                }
            });
        }

        /// <summary>
        /// Aktualisiert genau die Importfelder des Bestandssatzes — Id, Bezeichner
        /// und Anwenderfelder bleiben stehen (Dublettenkonzept 4.2).
        /// </summary>
        private static Task<bool> Ueberschreiben(UnifiedModule modul, int bestandsId)
        {
            return Task.Run(() =>
            {
                try
                {
                    PhotovoltaikStammCtrl ctrl = new PhotovoltaikStammCtrl();
                    modul.NachModell(ctrl);
                    return ctrl.UpdateImport(bestandsId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Aktualisieren des PV-Moduls: " + ex.Message);
                    return false;
                }
            });
        }

        /// <summary>Übersetzt einen Meldungsschlüssel des CEC- bzw. PAN-Dienstes.</summary>
        internal static string Meldungstext(CecFortschritt meldung)
        {
            string vorlage = MyResource.Resource.ResourceManager.GetString(meldung.Schluessel ?? "");
            if (string.IsNullOrEmpty(vorlage)) return meldung.Schluessel ?? "";

            return meldung.Werte.Length == 0
                ? vorlage
                : string.Format(System.Globalization.CultureInfo.CurrentCulture, vorlage, meldung.Werte);
        }
    }
}
