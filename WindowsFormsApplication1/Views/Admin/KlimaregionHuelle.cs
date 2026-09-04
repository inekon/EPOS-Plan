using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Klimadaten;
using Microsoft.AspNetCore.Components;
using SkiaSharp;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Klimaregionen (iU9-W14c.7).
    ///
    /// <para><b>Die Datenbank-, Netz- und Rechenseite steht hier</b>, nicht in der
    /// Komponente: die Regionsliste aus <see cref="KlimaregionStammCtrl"/> (seit
    /// W14c.0d im Kern), die Stundenwerte aus <see cref="SolardatenCtrl"/>, die zwei
    /// Bilder aus <c>ChartRenderer.Jahresgang</c> und der Import als
    /// <see cref="KlimaImportAblauf"/> (W14c.0e).</para>
    ///
    /// <para><b>Der Import läuft in <c>Task.Run</c> und lässt sich abbrechen</b>
    /// (A-4): Er holt eine PVGIS-Antwort über das Netz, rechnet 8 760 Sonnenstände
    /// und schreibt 9 125 Zeilen in einer Transaktion. In einer WebView ist der
    /// Renderfaden derselbe Faden.</para>
    ///
    /// <para><b>Der einzige Netzzugriff des Programms</b> (Risiko R-W14c-5) hängt an
    /// den zwei Delegaten <c>ITmyQuelle</c> und <c>IOrtsQuelle</c>; hier sind es
    /// <c>PVGIS_EPW_Downloader.GetTMY</c> und <c>GetCoordinatesAsync</c>, in der
    /// Probe eine eingefrorene Datei.</para>
    ///
    /// <para><b>Die Ortsliste ist eine VORSCHLAGSLISTE, kein Startbedingung</b>
    /// (Befund W14c-B15, Entscheid E-7): <c>Form_Klimadaten_Load</c> las
    /// <c>&lt;BenutzerLokal&gt;\Ortsliste\Ortsnamen.txt</c> ohne <c>File.Exists</c>
    /// und ohne <c>try</c> — die Datei liegt weder im Repo noch im Setup, und auf
    /// einer frischen Installation öffnete die Maske deshalb NICHT. Fehlt sie, bleibt
    /// die Liste leer; das Feld erlaubt ohnehin freie Eingabe.</para>
    /// </summary>
    internal static class KlimaregionHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 757 × 641).</summary>
        private static readonly Size MASS = new Size(1180, 780);

        /// <summary>Der Ordner der Ortsliste unterhalb von <c>Dienste.Pfade.BenutzerLokal</c>.</summary>
        private const string ORDNER_ORTSLISTE = "Ortsliste";

        /// <summary>Die Datei mit den Ortsvorschlägen.</summary>
        private const string DATEI_ORTSLISTE = "Ortsnamen.txt";

        /// <summary>Die Abbruchmarke des laufenden Imports (A-4).</summary>
        private static CancellationTokenSource _abbruch;

        /// <summary>
        /// Zeigt die Klimaregionen als eigenes Fenster — der Weg von
        /// <c>MDIMainForm.MenuItem_Klimadaten_Click</c>.
        ///
        /// <para><b>Mit Besitzer und in einem <c>using</c></b> (Befund W14c-B34).</para>
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<KlimaregionDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<KlimaregionDialog>(
                MyResource.Resource.KLIMA_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Der PARAMETERSATZ der Komponente.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Regionen"] = new Func<Task<List<KlimaregionDialog.Regionszeile>>>(RegionenLesen),
                ["Ansicht"] = new Func<string, Task<KlimaregionDialog.Regionsansicht>>(Ansicht),
                ["Importieren"] = new Func<KlimaImportAuftrag, IProgress<ImportFortschritt>,
                                           Task<KlimaImportErgebnis>>(Importieren),
                ["Abbrechen"] = new Action(() => { try { _abbruch?.Cancel(); } catch { } }),
                ["Loeschen"] = new Func<string, Task<bool>>(Loeschen),
                ["Ortsvorschlaege"] = Ortsvorschlaege()
            };
        }

        // =====================================================================
        // Liste und Ansicht
        // =====================================================================

        private static Task<List<KlimaregionDialog.Regionszeile>> RegionenLesen()
        {
            var ctrl = new KlimaregionStammCtrl();
            ctrl.ReadAll();

            var liste = new List<KlimaregionDialog.Regionszeile>(ctrl.rows);
            for (int i = 0; i < ctrl.rows; i++)
                liste.Add(new KlimaregionDialog.Regionszeile(ctrl.items[i].m_szName,
                                                             ctrl.items[i].m_bReadOnly));
            return Task.FromResult(liste);
        }

        /// <summary>
        /// Details, Koordinaten und die zwei Bilder einer Region (<c>CreateChart</c>).
        ///
        /// <para><b>Ohne Stundenwerte gibt es eine Meldung, keine Ausnahme</b> (Befund
        /// W14c-B19): <c>yAxis.ToArray().Max()</c> warf mit „Sequence contains no
        /// elements", sobald eine Region keine Zeilen in <c>Tab_Solar_STAMM</c> hatte —
        /// etwa nach einem abgebrochenen Import.</para>
        /// </summary>
        private static Task<KlimaregionDialog.Regionsansicht> Ansicht(string name)
        {
            var region = new KlimaregionStammCtrl();
            region.ReadByName(name ?? "");

            if (region.m_ID_Klimaregion <= 0)
                return Task.FromResult(new KlimaregionDialog.Regionsansicht(
                    "", null, null, null, null, MyResource.Resource.KLIMA_MSG_KEINE_DATEN));

            var solar = new SolardatenCtrl();
            solar.ReadAllStamm(region.m_ID_Klimaregion);

            if (solar.list_Temperatur == null || solar.list_Temperatur.Count == 0)
                return Task.FromResult(new KlimaregionDialog.Regionsansicht(
                    region.Details ?? "", region.Longitude, region.Latitude, null, null,
                    MyResource.Resource.KLIMA_MSG_KEINE_DATEN));

            byte[] temperatur = ChartRenderer.Jahresgang(
                MyResource.Resource.KLIMA_DIA_TEMPERATUR,
                new[]
                {
                    new ChartRenderer.Reihe(MyResource.Resource.KLIMA_REIHE_TEMPERATUR,
                                            solar.list_Temperatur.ToArray(),
                                            ChartRenderer.C_AUSSENTEMPERATUR)
                },
                MyResource.Resource.KLIMA_ACHSE_X,
                MyResource.Resource.KLIMA_ACHSE_TEMPERATUR);

            // A-3/E-4: Die Sonnenwinkel-Achse beginnt bei 0 - wie YMinValue = 0 des
            // Vorlaeufers (W14c.0j).
            byte[] winkel = ChartRenderer.Jahresgang(
                MyResource.Resource.KLIMA_DIA_SONNENWINKEL,
                new[]
                {
                    new ChartRenderer.Reihe(MyResource.Resource.KLIMA_REIHE_SONNENWINKEL,
                                            solar.list_Sonnenwinkel.ToArray(),
                                            SKColors.Orange)
                },
                MyResource.Resource.KLIMA_ACHSE_X,
                MyResource.Resource.KLIMA_ACHSE_SONNENWINKEL,
                minimumNull: true);

            return Task.FromResult(new KlimaregionDialog.Regionsansicht(
                region.Details ?? "", region.Longitude, region.Latitude, temperatur, winkel, ""));
        }

        // =====================================================================
        // Import (A-4: Task.Run mit Abbruch)
        // =====================================================================

        private static async Task<KlimaImportErgebnis> Importieren(
            KlimaImportAuftrag auftrag, IProgress<ImportFortschritt> melder)
        {
            try { _abbruch?.Dispose(); } catch { }
            _abbruch = new CancellationTokenSource();

            CancellationToken marke = _abbruch.Token;

            return await Task.Run(() => KlimaImportAblauf.Laufen(
                auftrag,
                (lon, lat, azimut) => PVGIS_EPW_Downloader.GetTMY(lon, lat, azimut),
                ort => PVGIS_EPW_Downloader.GetCoordinatesAsync(ort),
                melder,
                marke));
        }

        // =====================================================================
        // Loeschen (A-7/A-8)
        // =====================================================================

        private static Task<bool> Loeschen(string name)
        {
            return Task.FromResult(new KlimaregionStammCtrl().Delete(name ?? ""));
        }

        // =====================================================================
        // Ortsvorschlaege (Befund W14c-B15, Entscheid E-7)
        // =====================================================================

        /// <summary>
        /// Die Vorschläge des Ortsfeldes. <b>Fehlt die Datei, ist die Liste leer</b> —
        /// der Dialog öffnet trotzdem.
        /// </summary>
        private static IReadOnlyList<string> Ortsvorschlaege()
        {
            try
            {
                string datei = Dienste.Pfade.Verbinde(Dienste.Pfade.BenutzerLokal,
                                                      ORDNER_ORTSLISTE, DATEI_ORTSLISTE);
                if (!File.Exists(datei)) return Array.Empty<string>();

                return File.ReadAllLines(datei)
                           .Select(z => (z ?? "").Trim())
                           .Where(z => z.Length > 0)
                           .ToList();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
