using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Klimadaten;
using Microsoft.AspNetCore.Components;
using SkiaSharp;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Klimadaten (iU9-W14c.7).
    ///
    /// <para><b>Sie heisst wieder KLIMADATEN</b> (Entscheid E-3, Anwender 04.09.2026:
    /// „Klimaregion ist eigentlich für Deutschland gedacht, Klimadaten für den
    /// Download weltweit mit TMY-Daten."). Der Menütext heisst „Klimadaten", die
    /// Komponente ebenso. <b>KLIMAREGION</b> meint dagegen die DEUTSCHEN
    /// Klimaregionen (Klimazonenkarte, Projektbezug über <c>ID_Klimaregion</c>) —
    /// <see cref="KlimaregionStammCtrl"/>, <c>Tab_Klimaregion_STAMM</c> und
    /// <c>KlimazonenkarteDialog</c> behalten ihren Namen.</para>
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
    /// <para><b>Der Netzzugriff des Programms</b> (Risiko R-W14c-5) hängt an den
    /// Delegaten <c>ITmyQuelle</c>, <c>IOrtsQuelle</c> und — seit KL1-B —
    /// <c>INetzbereich</c>; hier sind es <c>PVGIS_EPW_Downloader.GetTMY</c>,
    /// <c>GetCoordinatesAsync</c> und <see cref="Bereich"/>, in der Probe eingefrorene
    /// Dateien. Der Kern kennt weder <c>HttpClient</c> noch eine Adresse.</para>
    ///
    /// <para><b>Drei Klimaquellen</b> (Auftrag KL1-B): PVGIS-TMY wie bisher, eine
    /// DWD-TRY-Datei vom Rechner des Anwenders (ganz ohne Netz) und die offenen
    /// TRY-Regionaldaten — über Bereichsabrufe auf <c>data.zip</c> oder aus einer
    /// lokalen Kopie dieses Pakets.</para>
    ///
    /// <para><b>Die Ortsliste ist eine VORSCHLAGSLISTE, kein Startbedingung</b>
    /// (Befund W14c-B15, Entscheid E-7): <c>Form_Klimadaten_Load</c> las
    /// <c>&lt;BenutzerLokal&gt;\Ortsliste\Ortsnamen.txt</c> ohne <c>File.Exists</c>
    /// und ohne <c>try</c> — die Datei liegt weder im Repo noch im Setup, und auf
    /// einer frischen Installation öffnete die Maske deshalb NICHT. Fehlt sie, bleibt
    /// die Liste leer; das Feld erlaubt ohnehin freie Eingabe.</para>
    /// </summary>
    internal static class KlimadatenHuelle
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
        /// Zeigt die Klimadaten als eigenes Fenster — der Weg von
        /// <c>Hauptfensterrahmen.MenuItem_Klimadaten_Click</c>.
        ///
        /// <para><b>Mit Besitzer und in einem <c>using</c></b> (Befund W14c-B34).</para>
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<KlimadatenDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<KlimadatenDialog>(
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
                ["Regionen"] = new Func<Task<List<KlimadatenDialog.Regionszeile>>>(RegionenLesen),
                ["Ansicht"] = new Func<string, Task<KlimadatenDialog.Regionsansicht>>(Ansicht),
                ["Importieren"] = new Func<KlimaImportAuftrag, IProgress<ImportFortschritt>,
                                           Task<KlimaImportErgebnis>>(Importieren),
                ["Abbrechen"] = new Action(() => { try { _abbruch?.Cancel(); } catch { } }),
                ["RegionErmitteln"] = new Func<KlimaImportAuftrag,
                                               Task<KlimaVorschauErgebnis>>(RegionErmitteln),
                ["Loeschen"] = new Func<string, Task<bool>>(Loeschen),
                ["Ortsvorschlaege"] = Ortsvorschlaege(),
                ["DateiWaehlen"] = new Func<string, Task<string>>(DateiWaehlen)
            };
        }

        // =====================================================================
        // Dateiwahl der TRY-Quellen (Auftrag KL1-B)
        // =====================================================================

        /// <summary>
        /// Der Dateiwähler für die TRY-Datei und das Regionalpaket. <b>Die ASYNCHRONE
        /// Fassung</b> (Befund W13-B-1): <c>OpenFileDialog.ShowDialog()</c> öffnete
        /// seine verschachtelte Nachrichtenschleife INNERHALB des
        /// WebView2-Rückrufs; <c>DateiOeffnenAsync</c> fährt das Fenster hinter dem
        /// Blazor-Ereignis hoch.
        /// </summary>
        private static async Task<string> DateiWaehlen(string filter)
        {
            string pfad = await Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.KLIMA_TITEL, filter ?? "", "").ConfigureAwait(true);
            return pfad ?? "";
        }

        // =====================================================================
        // Liste und Ansicht
        // =====================================================================

        private static Task<List<KlimadatenDialog.Regionszeile>> RegionenLesen()
        {
            var ctrl = new KlimaregionStammCtrl();
            ctrl.ReadAll();

            var liste = new List<KlimadatenDialog.Regionszeile>(ctrl.rows);
            for (int i = 0; i < ctrl.rows; i++)
                liste.Add(new KlimadatenDialog.Regionszeile(ctrl.items[i].m_szName,
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
        private static Task<KlimadatenDialog.Regionsansicht> Ansicht(string name)
        {
            var region = new KlimaregionStammCtrl();
            region.ReadByName(name ?? "");

            if (region.m_ID_Klimaregion <= 0)
                return Task.FromResult(new KlimadatenDialog.Regionsansicht(
                    "", null, null, null, null, MyResource.Resource.KLIMA_MSG_KEINE_DATEN));

            var solar = new SolardatenCtrl();
            solar.ReadAllStamm(region.m_ID_Klimaregion);

            if (solar.list_Temperatur == null || solar.list_Temperatur.Count == 0)
                return Task.FromResult(new KlimadatenDialog.Regionsansicht(
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

            return Task.FromResult(new KlimadatenDialog.Regionsansicht(
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
                marke,
                Bereich));
        }

        // =====================================================================
        // Die REGIONSVORSCHAU (Auftrag KL-3)
        // =====================================================================

        /// <summary>
        /// Sagt vor dem Einlesen, welche TRY-Region der Standort trifft — über
        /// DIESELBEN Nahtstellen wie der Import (Ortsauflösung und Bereichsabruf).
        ///
        /// <para><b>Auch sie läuft in <c>Task.Run</c></b>: Sie holt das
        /// Zentralverzeichnis des Pakets über das Netz, und in einer WebView ist der
        /// Renderfaden derselbe Faden. Eine eigene Abbruchmarke braucht sie nicht —
        /// die Vorschau dauert einen Bruchteil des Imports.</para>
        /// </summary>
        private static async Task<KlimaVorschauErgebnis> RegionErmitteln(KlimaImportAuftrag auftrag)
        {
            return await Task.Run(() => KlimaImportAblauf.RegionErmittelnAsync(
                auftrag,
                ort => PVGIS_EPW_Downloader.GetCoordinatesAsync(ort),
                CancellationToken.None,
                Bereich));
        }

        // =====================================================================
        // Der BEREICHSABRUF der TRY-Regionaldaten (Auftrag KL1-B)
        // =====================================================================

        /// <summary>Ein eigener Client: Bereichsabrufe brauchen keine Zeitgrenze von 100 s.</summary>
        private static readonly HttpClient _bereichsClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        /// <summary>
        /// Holt einen BEREICH einer Adresse (<c>HTTP Range</c>) — die zweite Naht des
        /// Netzzugriffs neben <c>ITmyQuelle</c>.
        ///
        /// <para><b>Absolute Grenzen.</b> <c>RangeHeaderValue(von, bis)</c> bekommt
        /// <c>bis = von + laenge − 1</c>; eine Länge kleiner 0 heißt „bis zum Ende" und
        /// lässt die Obergrenze weg. Gelesen wird mit
        /// <c>HttpCompletionOption.ResponseHeadersRead</c> — der Rumpf darf nie als
        /// Ganzes in den Speicher laufen.</para>
        ///
        /// <para><b>Die Gesamtlänge kommt aus <c>Content-Range</c></b>
        /// (<c>bytes von-bis/gesamt</c>). Fehlt sie, meldet der Kern „Die Adresse
        /// erlaubt keine Teilabrufe" und bricht ab — statt 892 MB zu ziehen.</para>
        /// </summary>
        private static async Task<(byte[] Daten, long Gesamtlaenge)> Bereich(
            string adresse, long von, long laenge, CancellationToken abbruch)
        {
            using (var anfrage = new HttpRequestMessage(HttpMethod.Get, adresse))
            {
                anfrage.Headers.Range = laenge < 0
                    ? new RangeHeaderValue(von, null)
                    : new RangeHeaderValue(von, von + laenge - 1);

                using (HttpResponseMessage antwort = await _bereichsClient.SendAsync(
                           anfrage, HttpCompletionOption.ResponseHeadersRead, abbruch)
                       .ConfigureAwait(false))
                {
                    antwort.EnsureSuccessStatusCode();

                    byte[] daten = await antwort.Content.ReadAsByteArrayAsync(abbruch)
                                                        .ConfigureAwait(false);

                    long gesamt = 0;
                    ContentRangeHeaderValue bereich = antwort.Content.Headers.ContentRange;
                    if (bereich != null && bereich.Length.HasValue) gesamt = bereich.Length.Value;

                    return (daten, gesamt);
                }
            }
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
