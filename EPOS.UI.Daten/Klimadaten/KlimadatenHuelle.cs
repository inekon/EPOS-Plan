using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Klimadaten;
using SpeicherEngine;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Klimadaten (Auftrag KI‑F8, Anwenderentscheid KI‑D‑Q8) — sie
    /// lag bis hierher vollständig in <c>WindowsFormsApplication1/Views/Admin</c> und
    /// war damit auf dem iPad unerreichbar, obwohl sie keine einzige Windows-Zeile
    /// führt.
    ///
    /// <para><b>Was hier steht.</b> Die Datenbank-, Netz- und Rechenseite: die
    /// Regionsliste aus <see cref="KlimaregionStammCtrl"/>, die Stundenwerte aus
    /// <see cref="SolardatenCtrl"/>, die zwei Zeichenmodelle aus
    /// <c>ChartRenderer.JahresgangModell</c>, der Import als
    /// <see cref="KlimaImportAblauf"/> und der Bereichsabruf der TRY-Regionaldaten.
    /// Die Plattform steuert nur zwei Dinge bei, und beide kommen über die
    /// Kern-Dienste herein: die Dateiwahl (<c>Dienste.Datei</c>) und die Ablagewurzel
    /// der Ortsliste (<c>Dienste.Pfade</c>).</para>
    ///
    /// <para><b>Was NICHT hier steht:</b> das Fenster. Unter Windows zeigt
    /// <c>Views/Admin/KlimadatenFenster</c> dieselbe Komponente modal und zieht
    /// danach die Startseite nach; auf iOS zeigt die <c>AppWurzel</c> sie als
    /// Ansicht. Muster <see cref="NutzungsdauerHuelle"/>.</para>
    ///
    /// <para><b>Sie heisst KLIMADATEN</b> (Entscheid E-3, Anwender 04.09.2026:
    /// „Klimaregion ist eigentlich für Deutschland gedacht, Klimadaten für den
    /// Download weltweit mit TMY-Daten."). <b>KLIMAREGION</b> meint dagegen die
    /// DEUTSCHEN Klimaregionen (Klimazonenkarte, Projektbezug über
    /// <c>ID_Klimaregion</c>) — <see cref="KlimaregionStammCtrl"/>,
    /// <c>Tab_Klimaregion_STAMM</c> und <c>KlimazonenkarteDialog</c> behalten ihren
    /// Namen.</para>
    ///
    /// <para><b>Der Import läuft nebenher und lässt sich abbrechen</b>
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
    /// DWD-TRY-Datei vom Gerät des Anwenders (ganz ohne Netz) und die offenen
    /// TRY-Regionaldaten — über Bereichsabrufe auf <c>data.zip</c> oder aus einer
    /// lokalen Kopie dieses Pakets.</para>
    ///
    /// <para><b>Die Ortsliste ist eine VORSCHLAGSLISTE, keine Startbedingung</b>
    /// (Befund W14c-B15, Entscheid E-7): <c>Form_Klimadaten_Load</c> las
    /// <c>&lt;BenutzerLokal&gt;\Ortsliste\Ortsnamen.txt</c> ohne <c>File.Exists</c>
    /// und ohne <c>try</c> — die Datei liegt weder im Repo noch im Setup, und auf
    /// einer frischen Installation öffnete die Maske deshalb NICHT. Fehlt sie, bleibt
    /// die Liste leer; das Feld erlaubt ohnehin freie Eingabe.</para>
    /// </summary>
    internal static class KlimadatenHuelle
    {
        /// <summary>Der Ordner der Ortsliste unterhalb von <c>Dienste.Pfade.BenutzerLokal</c>.</summary>
        private const string ORDNER_ORTSLISTE = "Ortsliste";

        /// <summary>Die Datei mit den Ortsvorschlägen.</summary>
        private const string DATEI_ORTSLISTE = "Ortsnamen.txt";

        /// <summary>Die Abbruchmarke des laufenden Imports (A-4).</summary>
        private static CancellationTokenSource _abbruch;

        /// <summary>Der Fenstertitel und zugleich die Dialogüberschrift.</summary>
        internal static string Titel() => MyResource.Resource.KLIMA_TITEL;

        /// <summary>Das Wunschmaß der Klimadatenverwaltung selbst (Vorläufer: 757 × 641).</summary>
        internal const int FENSTER_BREITE = 1180;

        /// <summary>Siehe <see cref="FENSTER_BREITE"/>.</summary>
        internal const int FENSTER_HOEHE = 780;

        /// <summary>
        /// Das Wunschmaß der Überlagerung „Klimadaten einlesen“ als eigenes Fenster (KD-1):
        /// so breit wie eine Überlagerung (900 px), so hoch, dass sie VOR der Suche (gemessen
        /// 900 px samt Höhenfeld und Suchzeile, KD-3) in 90 % der Fensterhöhe steht, ohne zu
        /// rollen. Die Vorschaukarte (rund 750 px) rollt dann unter der haftenden Leiste — ein
        /// Fenster, das auch sie ganz zeigte, gäbe kein Schirm her. Reicht der Schirm nicht,
        /// rollt der Inhalt, und die Schlussleiste
        /// haftet am Boden der Überlagerung (<c>epos-ui.css</c>, „Die Schlussleiste des
        /// Einlesens haftet genauso“).
        /// </summary>
        internal const int EINLESEN_BREITE = 900;

        /// <summary>Siehe <see cref="EINLESEN_BREITE"/>.</summary>
        internal const int EINLESEN_HOEHE = 1000;

        /// <summary>
        /// Das Wunschmaß des Fensters: mindestens das der getragenen Überlagerung
        /// (<see cref="EPOS.UI.Dienste.Fenstermass.MitUeberlagerung"/>), wie bei jedem Fenster,
        /// das einen Import als Überlagerung trägt.
        /// </summary>
        internal static (int Breite, int Hoehe) Wunschmass()
            => EPOS.UI.Dienste.Fenstermass.MitUeberlagerung(
                FENSTER_BREITE, FENSTER_HOEHE, EINLESEN_BREITE, EINLESEN_HOEHE);

        /// <summary>Der PARAMETERSATZ der Komponente — ohne <c>Geschlossen</c>.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["CsvSpeichern"] = Diagrammexportnaht.Fuer(Dienste.Projekt.Id),
                ["Regionen"] = new Func<Task<IReadOnlyList<Katalogfilterzeile>>>(RegionenLesen),
                ["Ansicht"] = new Func<string, Task<KlimadatenDialog.Regionsansicht>>(Ansicht),
                ["FarbeSetzen"] = new Func<Farbrolle, Farbe, Task>(FarbeSetzen),
                ["FarbeZuruecksetzen"] = new Func<Farbrolle, Task>(FarbeZuruecksetzen),
                ["Importieren"] = new Func<KlimaImportAuftrag, IProgress<ImportFortschritt>,
                                           Task<KlimaImportErgebnis>>(Importieren),
                ["Abbrechen"] = new Action(() => { try { _abbruch?.Cancel(); } catch { } }),
                ["RegionErmitteln"] = new Func<KlimaImportAuftrag,
                                               Task<KlimaVorschauErgebnis>>(RegionErmitteln),
                ["Loeschen"] = new Func<string, Task<bool>>(Loeschen),
                // AD-Q15: das Schloss laesst sich nach Rueckfrage umschalten.
                ["Schloss"] = Schlosswege.Aus(KlimaregionStammCtrl.SchlossSetzen),
                ["Ortsvorschlaege"] = Ortsvorschlaege(),
                ["DateiWaehlen"] = new Func<string, Task<string>>(DateiWaehlen),
                // KD-5: der CSV-Export einer Region (Handlung „Exportieren…" der Auswahlleiste).
                ["ExportLesen"] = new Func<string, Task<KlimaExportDaten>>(ExportLesen),
                ["ExportSpeichern"] = new Func<string, string, Task<EPOS.UI.Dialoge.Export.KlimaExportErgebnis>>(
                    (inhalt, vorschlag) => ExportSpeichern(inhalt, vorschlag, OperatingSystem.IsIOS()))
            };
        }

        // =====================================================================
        // CSV-Export einer Region (Auftrag KD-5)
        // =====================================================================

        /// <summary>Liest die Region abseits des Oberflächenfadens (<see cref="KlimaExportCtrl.Lesen(string)"/>).</summary>
        private static Task<KlimaExportDaten> ExportLesen(string region)
            => Kulturweitergabe.Starten(() => KlimaExportCtrl.Lesen(region));

        /// <summary>
        /// Der Speicherweg des Exports über die vorhandene Naht <see cref="CsvExportClass.DateiWaehlenFuerExportAsync"/>
        /// (Dateiwahl über <c>Dienste.Datei</c>, gemerkter Ordner): Schreiben als UTF-8 mit BOM — ganz oder gar nicht —,
        /// auf iOS danach das Teilen. Eine abgebrochene Dateiwahl meldet <see cref="EPOS.UI.Dialoge.Export.KlimaExportErgebnis.Abgebrochen"/>.
        /// </summary>
        internal static async Task<EPOS.UI.Dialoge.Export.KlimaExportErgebnis> ExportSpeichern(string inhalt, string vorschlag, bool ios)
        {
            string pfad = await CsvExportClass.DateiWaehlenFuerExportAsync(vorschlag).ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(pfad)) return EPOS.UI.Dialoge.Export.KlimaExportErgebnis.Abgebrochen;
            try
            {
                await Kulturweitergabe.Starten(() =>
                {
                    File.WriteAllText(pfad, inhalt ?? "", new System.Text.UTF8Encoding(true));
                    return true;
                });
            }
            catch (Exception ex)
            {
                return new EPOS.UI.Dialoge.Export.KlimaExportErgebnis(false, false,
                    string.Format(System.Globalization.CultureInfo.CurrentCulture, MyResource.Resource.KEXP_MSG_FEHLER, ex.Message));
            }
            string text = string.Format(System.Globalization.CultureInfo.CurrentCulture, MyResource.Resource.KEXP_MSG_GESPEICHERT, pfad);
            if (ios)
            {
                bool geteilt;
                try { geteilt = Dienste.Datei.MitSystemOeffnen(pfad); }
                catch (Exception) { geteilt = false; }
                if (!geteilt)
                    text = string.Format(System.Globalization.CultureInfo.CurrentCulture, MyResource.Resource.KEXP_MSG_TEILEN_FEHLER, pfad);
            }
            return new EPOS.UI.Dialoge.Export.KlimaExportErgebnis(true, false, text);
        }

        // =====================================================================
        // Dateiwahl der TRY-Quellen (Auftrag KL1-B)
        // =====================================================================

        /// <summary>
        /// Der Dateiwähler der Plattform für die TRY-Datei und das Regionalpaket.
        /// <b>Die ASYNCHRONE Fassung</b> (Befund W13-B-1): <c>OpenFileDialog.ShowDialog()</c>
        /// öffnete seine verschachtelte Nachrichtenschleife INNERHALB des
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

        /// <summary>
        /// Die Zeilen der Regionsliste (Auftrag KL-4) — sieben Spalten aus
        /// <see cref="KlimaregionStammCtrl.Katalogfilterzeilen"/>, unverändert
        /// durchgereicht.
        ///
        /// <para><b>Den Satzbau der Spalte „Quelle" macht der KERN</b> (Auftrag KL-6,
        /// <c>KlimaAnzeige.Quellenzeile</c>): Sie reiht Quelle, Bezugsjahr und Szenario
        /// zu „TRY-Regionaldaten (Deutschland) · 2045 · sommerwarm". Bis KL-6 stand
        /// hier eine eigene Übersetzung des Quellenschlüssels; aus drei Angaben EINEN
        /// Satz zu bauen ist aber kein Übersetzen mehr, und derselbe Satz steht auf der
        /// Startseite — er gehört deshalb an eine Stelle und nicht an zwei.</para>
        ///
        /// <para>Ein Altbestand ohne Quelle (NULL, vor Schemaschritt 95) bleibt LEER;
        /// sein Halbgeviertstrich kommt aus <c>Katalogwert.AusText</c>.</para>
        /// </summary>
        private static Task<IReadOnlyList<Katalogfilterzeile>> RegionenLesen()
        {
            return Task.FromResult(KlimaregionStammCtrl.Katalogfilterzeilen());
        }

        /// <summary>
        /// Details, Koordinaten und die zwei ZEICHENMODELLE einer Region.
        ///
        /// <para><b>Ohne Stundenwerte gibt es eine Meldung, keine Ausnahme</b> (Befund
        /// W14c-B19): <c>yAxis.ToArray().Max()</c> warf mit „Sequence contains no
        /// elements", sobald eine Region keine Zeilen in <c>Tab_Solar_STAMM</c> hatte —
        /// etwa nach einem abgebrochenen Import.</para>
        ///
        /// <para><b>Ein Modell statt Bildbytes</b> (Konzept Diagramme, Etappe E2): Die
        /// Oberfläche zeichnet es als SVG, und der Zeitausschnitt liegt seither im
        /// Browser — in der <c>viewBox</c> des inneren <c>&lt;svg&gt;</c>. Deshalb gibt
        /// es hier kein Achsenfenster mehr und keinen zweiten Delegaten: Ein Zoom
        /// zeichnet nichts neu. Die PNG-Fassung <c>ChartRenderer.Jahresgang</c> bleibt
        /// für den Bericht, sie geht durch dasselbe Modell.</para>
        /// </summary>
        /// <param name="name">Der Bezeichner der Region.</param>
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

            double[] werteTemperatur = solar.list_Temperatur.ToArray();
            double[] werteSonnenwinkel = solar.list_Sonnenwinkel.ToArray();

            Zeichenmodell temperatur = ChartRenderer.JahresgangModell(
                MyResource.Resource.KLIMA_DIA_TEMPERATUR,
                new[]
                {
                    new ChartRenderer.Reihe(MyResource.Resource.KLIMA_REIHE_TEMPERATUR,
                                            werteTemperatur,
                                            Farbrolle.AUSSENTEMPERATUR)
                },
                MyResource.Resource.KLIMA_ACHSE_X,
                MyResource.Resource.KLIMA_ACHSE_TEMPERATUR);

            // A-3/E-4: Die Sonnenwinkel-Achse beginnt bei 0 - wie YMinValue = 0 des
            // Vorlaeufers (W14c.0j).
            Zeichenmodell winkel = ChartRenderer.JahresgangModell(
                MyResource.Resource.KLIMA_DIA_SONNENWINKEL,
                new[]
                {
                    new ChartRenderer.Reihe(MyResource.Resource.KLIMA_REIHE_SONNENWINKEL,
                                            werteSonnenwinkel,
                                            Farbrolle.SONNENWINKEL)
                },
                MyResource.Resource.KLIMA_ACHSE_X,
                MyResource.Resource.KLIMA_ACHSE_SONNENWINKEL,
                true);

            // DIE DREI KENNZAHLEN IM KOPF DES STAMMBLATTS (Neuordnung Stufe 4):
            // Jahresmittel, Tiefst- und Hoechstwert der Aussentemperatur - aus
            // DENSELBEN Stundenwerten wie das Bild, einmal je Wahl.
            return Task.FromResult(new KlimadatenDialog.Regionsansicht(
                region.Details ?? "", region.Longitude, region.Latitude, temperatur, winkel, "")
            {
                Jahresmittel = werteTemperatur.Average(),
                Tiefstwert = werteTemperatur.Min(),
                Hoechstwert = werteTemperatur.Max(),
                // KD-8 Teil A: die gespeicherten Rechenspalten für „Weitere Reihen“ —
                // dieselben Zeilen (SolardatenCtrl.ReadAllStamm, ?-Parameter).
                Reihenbestand = KlimaReihenkatalog.AusRechenspalten(solar.items)
            });
        }

        // =====================================================================
        // Die Farbe einer Reihe (Farbrollen, Bedienung Teil 2)
        // =====================================================================

        /// <summary>
        /// Der Klick auf das Farbfeld eines Legendeneintrags landet hier: Die Rolle
        /// bekommt anwendungsweit diese Farbe (<c>Diagrammfarben.Setze</c> schreibt
        /// die Einstellung und speist <c>Farbpalette.Aktuell</c>). Danach trägt sie
        /// jedes Diagramm und jeder Bericht — beide malen über dieselbe Palette.
        /// </summary>
        private static Task FarbeSetzen(Farbrolle rolle, Farbe farbe)
        {
            Diagrammfarben.Setze(rolle, farbe);
            return Task.CompletedTask;
        }

        /// <summary>„Hausfarbe": Der Eintrag fällt aus der Einstellung.</summary>
        private static Task FarbeZuruecksetzen(Farbrolle rolle)
        {
            Diagrammfarben.Zuruecksetzen(rolle);
            return Task.CompletedTask;
        }

        // =====================================================================
        // Import (A-4: nebenher mit Abbruch)
        // =====================================================================

        private static async Task<KlimaImportErgebnis> Importieren(
            KlimaImportAuftrag auftrag, IProgress<ImportFortschritt> melder)
        {
            try { _abbruch?.Dispose(); } catch { }
            _abbruch = new CancellationTokenSource();

            CancellationToken marke = _abbruch.Token;

            // Der Arbeitsfaden bekommt die Kultur des Aufrufers (Auftrag #232): Ein
            // Faden ohne eigene Kultur laese den veraenderlichen prozessweiten
            // Vorgabewert und koennte mitten im Import die Sprache wechseln.
            // Eine Ausnahme, die der Ablauf nicht selbst benennt, wird ueber die Fangstelle der
            // Importe zu einem Eingabefehler mit Grund (IM-1).
            string datei = string.IsNullOrEmpty(auftrag?.TryPfad) ? auftrag?.TryPaketPfad ?? "" : auftrag.TryPfad;
            return await Importfang.StartenAsync(datei, () => KlimaImportAblauf.Laufen(
                auftrag,
                (lon, lat, azimut) => PVGIS_EPW_Downloader.GetTMY(lon, lat, azimut),
                ort => PVGIS_EPW_Downloader.GetCoordinatesAsync(ort),
                melder,
                marke,
                Bereich,
                _vorschau),
                a => new KlimaImportErgebnis { Ausgang = KlimaImportAusgang.Eingabefehler, Meldung = a.Text });
        }

        /// <summary>
        /// Die zuletzt gelesene VORSCHAU (Auftrag KD-2, Entscheid KD-Q3). Der Import
        /// übernimmt ihren Satz ohne zweiten Abruf, solange sie zum Auftrag passt
        /// (<see cref="KlimaVorschauErgebnis.PasstZu"/>); ändert der Anwender Ort, Quelle,
        /// Jahr oder Szenario, ist sie verfallen und der Import liest selbst.
        /// </summary>
        private static KlimaVorschauErgebnis _vorschau;

        // =====================================================================
        // Die REGIONSVORSCHAU (Auftrag KL-3)
        // =====================================================================

        /// <summary>
        /// Sucht und LIEST vor dem Einlesen den Datensatz des Standorts (Auftrag KD-2,
        /// alle drei Quellen) — über DIESELBEN Nahtstellen wie der Import (Ortsauflösung,
        /// Bereichsabruf, PVGIS-Abruf samt Kopf). Das Ergebnis bleibt für den Import liegen.
        ///
        /// <para><b>Auch sie läuft nebenher</b>: Sie holt das
        /// Zentralverzeichnis des Pakets über das Netz, und in einer WebView ist der
        /// Renderfaden derselbe Faden. Eine eigene Abbruchmarke braucht sie nicht —
        /// die Vorschau dauert einen Bruchteil des Imports.</para>
        /// </summary>
        private static async Task<KlimaVorschauErgebnis> RegionErmitteln(KlimaImportAuftrag auftrag)
        {
            _vorschau = null;
            KlimaVorschauErgebnis erg = await Kulturweitergabe.StartenAsync(() => KlimaImportAblauf.RegionErmittelnAsync(
                auftrag,
                ort => PVGIS_EPW_Downloader.GetCoordinatesAsync(ort),
                CancellationToken.None,
                Bereich,
                (lon, lat, azimut) => PVGIS_EPW_Downloader.GetTmyAbruf(lon, lat, azimut)));
            _vorschau = erg;
            return erg;
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
