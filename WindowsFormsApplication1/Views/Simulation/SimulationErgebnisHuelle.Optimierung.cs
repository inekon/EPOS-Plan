using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die STROMSPEICHER-AUSLEGUNG in der Ergebnishülle — seit Paket P3 (Auftrag #192)
    /// nur noch das, was der Reiter „Stromspeicher" selbst braucht.
    ///
    /// <para><b>Was hier stand und wohin es gegangen ist.</b> Bis zur Windows-Abnahme V2
    /// genügte der Hülle EINE Zeile, die Sprungbrücke in <c>Form_SpeicherOptimierung</c>.
    /// Mit W11b‑B‑5 wurden daraus zwölf Delegaten — Vorbelegung lesen, Stand und Profil
    /// schreiben, Flotte rechnen, Raster rechnen, Betriebsbild nachzeichnen,
    /// Projektflotte aktivieren und deaktivieren, Bestpunkt übernehmen, Leistungspreis
    /// schreiben —, weil die zwei RAZOR-DIALOGE daran hingen. Beide Dialoge sind mit P3
    /// gefallen („Fenster in Fenster ist nicht gut", Anwenderrückmeldung 11.09.2026);
    /// ihre Datenseite liegt seither in
    /// <see cref="StromspeicherAuslegungCtrl"/> — Hausregel: Datenbankseite in den
    /// Kern.</para>
    ///
    /// <para><b>Geblieben sind vier Wege</b>, und alle vier gehören dem REITER
    /// „Stromspeicher": Er zeigt die Flotte samt Betriebseditor an
    /// (<see cref="OptimierungVorgaben"/>), speichert deren Optionen
    /// (<see cref="OptimierungEinstellungenSpeichern"/>), schreibt die CSV
    /// (<see cref="OptimierungCsv"/>) und fragt, ob es den Flotteneinstieg überhaupt
    /// gibt (<see cref="OptimierungFlottenRechnen"/>). Dazu kommt der
    /// ANSICHTSWECHSEL <see cref="AuslegungOeffnen"/>.</para>
    /// </summary>
    internal sealed partial class SimulationErgebnisHuelle
    {
        /// <summary>
        /// Der Auslegungscontroller dieses Projekts — er hält Lauf, Vorbereitung und
        /// das rohe Raster. Angelegt beim ersten Zugriff.
        /// </summary>
        private StromspeicherAuslegungCtrl _auslegung;

        /// <summary>An der Projektflotte wurde geschrieben, ohne dass neu gerechnet wurde.</summary>
        private bool _flotteProjektGeaendert;

        /// <summary>Die Abbruchmarke eines laufenden Hintergrundlaufs; <c>null</c> = keiner läuft.</summary>
        private CancellationTokenSource _auslegungAbbruch;

        /// <summary>
        /// Der Controller mit dem AKTUELLEN Simulationslauf. Der Lauf wird nur
        /// nachgereicht, wenn er sich geändert hat — sonst verlöre die Ansicht bei
        /// jedem Zeichnen ihr gemerktes Raster.
        /// </summary>
        private StromspeicherAuslegungCtrl Auslegung()
        {
            if (_auslegung == null) _auslegung = new StromspeicherAuslegungCtrl(m_ID_Projekt);
            if (!ReferenceEquals(_auslegung.Lauf, sim)) _auslegung.LaufUebernehmen(sim);
            return _auslegung;
        }

        /// <summary>
        /// Vorbelegung des Suchraums samt der aktuellen Auslegung — <b>Datenbankzugriff</b>,
        /// deshalb auf dem Bedienfaden.
        /// </summary>
        private SpeicherOptimierungVorgaben OptimierungVorgaben() => Auslegung().Vorgaben();

        /// <summary>Speichert den bearbeiteten Stand als projektgebundene Vorbelegung.</summary>
        private Task<string> OptimierungEinstellungenSpeichern(SpeicherOptimierungEingaben eingaben)
        {
            string fehler = Auslegung().EinstellungenSpeichern(eingaben);
            if (string.IsNullOrEmpty(fehler))
            {
                _flotteProjektGeaendert = true;
                _ergebnisGueltig = false;
            }
            return Task.FromResult(fehler);
        }

        /// <summary>
        /// Rechnet die Flottenstudie: Datenbank auf dem Bedienfaden, Rechnung in
        /// <c>Task.Run</c>.
        /// </summary>
        /// <remarks>
        /// Der Reiter „Stromspeicher" fragt diesen Weg nur auf <c>null</c> ab — er
        /// entscheidet damit, ob es den Flotteneinstieg gibt. Gerechnet wird in der
        /// Ansicht STROMSPEICHER_AUSLEGUNG; dass der Weg trotzdem VOLLSTÄNDIG ist, hält
        /// die Auskunft ehrlich.
        /// </remarks>
        private async Task<SpeicherFlottenErgebnis> OptimierungFlottenRechnen(
            SpeicherOptimierungEingaben eingaben, Action<double?, string> melder)
        {
            if (_auslegungAbbruch != null)
                return new SpeicherFlottenErgebnis { Meldung = MyResource.Resource.FLOTTE_DLG_MSG_LAEUFT };

            StromspeicherAuslegungCtrl ctrl = Auslegung();
            string meldung;
            StromspeicherOptimierungVorbereitung vorbereitung = ctrl.FlotteVorbereiten(eingaben, out meldung);
            if (vorbereitung == null) return new SpeicherFlottenErgebnis { Meldung = meldung };

            _flotteProjektGeaendert = true;
            _ergebnisGueltig = false;

            _auslegungAbbruch = new CancellationTokenSource();
            CancellationToken marke = _auslegungAbbruch.Token;
            IProgress<FlottenFortschritt> fortschritt =
                new Progress<FlottenFortschritt>(p => melder(
                    p.Gesamt > 0 ? (double)p.Abgeschlossen / p.Gesamt : (double?)null,
                    string.Format(MyResource.Resource.FLOTTE_DLG_STATUS_VARIANTE,
                                  p.Abgeschlossen, p.Gesamt)));
            try
            {
                return await Task.Run(() => ctrl.FlotteRechnen(vorbereitung, fortschritt, marke), marke);
            }
            catch (OperationCanceledException)
            {
                return new SpeicherFlottenErgebnis
                { Abgebrochen = true, Meldung = MyResource.Resource.FLOTTE_DLG_MSG_ABGEBROCHEN };
            }
            finally
            {
                CancellationTokenSource quelle = _auslegungAbbruch;
                _auslegungAbbruch = null;
                if (quelle != null) quelle.Dispose();
            }
        }

        /// <summary>
        /// Wechselt auf die Ansicht „Stromspeicher-Auslegung" (Muster W16c‑E‑3,
        /// „Ansicht wechseln statt Überlagerung").
        /// </summary>
        /// <remarks>
        /// Zuerst wird der ARBEITSGANG angemeldet — mit dem Projekt, dem gerechneten
        /// Simulationslauf und dem Nachzug für diese Seite —, dann meldet die Hülle den
        /// Seitenschlüssel an die Wurzel. Ohne angemeldete Wurzel (kein Blazor auf dem
        /// Bildschirm) geschieht nichts; das ist derselbe Ausgang wie bei jedem anderen
        /// Navigationsweg.
        /// </remarks>
        private void AuslegungOeffnen()
        {
            StromspeicherAuslegungHuelle.Anmelden(Auslegung(), () =>
            {
                _flotteProjektGeaendert = true;
                _ergebnisGueltig = false;
            });

            EPOS.UI.Dienste.Navigationsziel.Aktuell?.OeffneMaske(
                EPOS.UI.Seiten.Seitenschluessel.StromspeicherAuslegung);
        }

        /// <summary>
        /// Schreibt einen CSV-Text als Datei.
        /// </summary>
        /// <remarks>
        /// <b>Eigener Schreiber, nicht <c>CsvExportClass</c>.</b> Jene Klasse ist auf
        /// ZEITREIHEN zugeschnitten — sie stellt jeder Zeile einen Zeitstempel voran;
        /// eine Rastermatrix hat weder Zeitbezug noch Zeitraster. Hier kommt nur die
        /// Kodierung dazu: UTF-8 MIT BOM, damit deutsches Excel die Datei direkt richtig
        /// öffnet.
        /// <para>Der Dateiwähler wird <c>await</c>et — ein synchron geöffnetes
        /// Plattformfenster mitten im Blazor-Ereignis ist der Absturz aus Befund
        /// W13‑B‑1.</para>
        /// </remarks>
        private async Task<Rueckmeldung> OptimierungCsv(string csv)
        {
            if (string.IsNullOrEmpty(csv))
                return new Rueckmeldung(false, MyResource.Resource.OPT_MSG_KEIN_ERGEBNIS);

            string vorschlag = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                MyResource.Resource.OPT_DATEI, m_ID_Projekt);

            string pfad = await Dienste.Datei.DateiSpeichernAsync(
                MyResource.Resource.OPT_CSV_TITEL, "CSV (*.csv)|*.csv", vorschlag);

            if (string.IsNullOrEmpty(pfad)) return Rueckmeldung.Still;

            try
            {
                File.WriteAllText(pfad, csv, new UTF8Encoding(true));
            }
            catch (Exception ex)
            {
                return new Rueckmeldung(false,
                    string.Format(MyResource.Resource.OPT_CSV_FEHLER, ex.Message));
            }

            return new Rueckmeldung(true,
                string.Format(MyResource.Resource.OPT_CSV_GESCHRIEBEN, pfad));
        }
    }
}
