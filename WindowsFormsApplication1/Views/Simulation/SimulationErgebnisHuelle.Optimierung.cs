using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die AUSLEGUNGSOPTIMIERUNG in der Ergebnishülle (W11b‑B‑5, Windows-Abnahme V2
    /// vom 07.09.2026).
    ///
    /// <para><b>Warum diese Datei entsteht.</b> Bis hierher genügte der Hülle EINE
    /// Zeile — <c>Sprung = schluessel =&gt; Sprungbruecke.Fuer(_fenster, sim,
    /// m_ID_Projekt)(schluessel)</c> —, weil das Ziel eine WinForms-Maske war und
    /// alles selbst tat: Felder lesen, Datenbank, <c>Task.Run</c>, ScottPlot,
    /// Übernahme, CSV. <c>Form_SpeicherOptimierung</c> ist mit dieser Welle gefallen
    /// (Befunde „Texte überschneiden sich" und „Dialog stürzt nach kurzer Zeit ab"),
    /// und was sie an Umgebung brauchte, steht jetzt hier: die Datenbankseite auf dem
    /// Bedienfaden, der Rechenlauf in <c>Task.Run</c>, die Abbruchmarke und der
    /// Dateischreiber.</para>
    ///
    /// <para><b>Die Aufteilung ist dieselbe wie beim Simulationslauf</b>
    /// (<see cref="SimulationErgebnisHuelle"/>): <c>BereiteOptimierungVor</c> LIEST die
    /// Datenbank und bleibt auf dem Bedienfaden — <c>DataRepository.EngineModus</c> ist
    /// prozessweit und nicht threadgebunden —, nur die reine Rechnung geht in
    /// <c>Task.Run</c>.</para>
    ///
    /// <para><b>Sie steht in einer EIGENEN Teildatei</b>, damit die Welle die drei
    /// gewachsenen Teildateien der Hülle nicht anfasst.</para>
    /// </summary>
    internal sealed partial class SimulationErgebnisHuelle
    {
        /// <summary>Die Abbruchmarke des laufenden Suchlaufs; <c>null</c> = keiner läuft.</summary>
        private CancellationTokenSource _optimierungAbbruch;

        /// <summary>
        /// Vorbelegung des Suchraums samt der aktuellen Auslegung — <b>Datenbankzugriff</b>,
        /// deshalb auf dem Bedienfaden.
        /// </summary>
        private SpeicherOptimierungVorgaben OptimierungVorgaben()
        {
            return SpeicherOptimierungCtrl.Vorbelegung(m_ID_Projekt, BezugsspitzeKw());
        }

        /// <summary>
        /// Die höchste Bezugsleistung des gerechneten Strombedarfs [kW]; 0, solange kein
        /// Lauf vorliegt (Anwenderentscheid W11b‑E‑3, 10.09.2026).
        /// </summary>
        /// <remarks>
        /// Sie entscheidet, welche STUFE der Leistungspreis-Staffel an der Spitze greift
        /// — und damit, welchen Wert die Tarifstruktur als Leistungspreis anbietet.
        /// Genommen wird der Strombedarf des Laufs und nicht der Netzbezug: Der Speicher
        /// soll die Spitze ja erst kappen, die Bezugsspitze OHNE ihn ist die
        /// Bezugsgröße.
        /// </remarks>
        private double BezugsspitzeKw()
        {
            double[] werte = sim != null && sim.simulation_Strombedarf != null
                ? sim.simulation_Strombedarf.Strombedarf_viertelStundenwerte
                : null;
            if (werte == null) return 0.0;

            double spitze = 0.0;
            for (int i = 0; i < werte.Length; i++)
                if (werte[i] > spitze) spitze = werte[i];
            return spitze;
        }

        /// <summary>
        /// Schreibt den Leistungspreis L_P SOFORT in die aktive Speichervariante
        /// (Anwenderentscheid W11b‑E‑3, 10.09.2026).
        /// </summary>
        /// <remarks>
        /// <b>Über denselben Weg wie der Reiter „Parameter"</b>
        /// (<c>SpeicherfeldSchreiben</c> mit <c>SpeicherFeld.Leistungspreis</c>) — L_P
        /// hat EINE Pflegestelle, und ein zweiter Schreiber daneben wäre genau die
        /// Doppelung, die zwei auseinanderlaufende Werte erzeugt. <c>VarianteLesen</c>
        /// steht davor, weil der Anwender die Optimierung öffnen kann, ohne den Reiter
        /// „Parameter" je gesehen zu haben; ohne den Aufruf wäre <c>_speicherVariante</c>
        /// dann <c>null</c> und der Schreibversuch stumm wirkungslos.
        /// </remarks>
        private void OptimierungLeistungspreis(double leistungspreisEurProKwA)
        {
            VarianteLesen();
            SpeicherfeldSchreiben(SpeicherFeld.Leistungspreis,
                leistungspreisEurProKwA.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Die Rastersuche: Datenbank auf dem Bedienfaden, Rechnung in
        /// <c>Task.Run</c>, Ergebnis (samt der zwei fertigen PNG) wieder hier.
        /// </summary>
        /// <remarks>
        /// <para><b>Sie wirft nicht.</b> Jeder Ausgang steht im Ergebnis — der
        /// Controller fängt Abbruch und Fehler selbst ab, und was hier noch schiefgehen
        /// kann (die Vorbereitung), wird an Ort und Stelle gefangen. Eine unbehandelte
        /// Ausnahme aus einem <c>Task.Run</c>, auf das niemand wartet, beendete unter
        /// dem WinForms-<c>BlazorWebView</c> den Prozess.</para>
        /// <para><b>Der Fortschritt ist im Controller gedrosselt</b>
        /// (<c>SpeicherOptimierungCtrl.Drossel</c>); hier kommt also schon wenig an.
        /// <c>Progress&lt;T&gt;</c> entsteht auf dem Bedienfaden und marshallt selbst
        /// dorthin zurück.</para>
        /// </remarks>
        private async Task<SpeicherOptimierungErgebnis> OptimierungRechnen(
            SpeicherOptimierungEingaben eingaben, Action<double?, string> melder)
        {
            if (sim == null || sim.simulation_Strombedarf == null)
                return new SpeicherOptimierungErgebnis
                {
                    Erfolg = false,
                    Meldung = MyResource.Resource.OPT_MSG_KEIN_LAUF
                };

            // ---- Datenbank, Bedienfaden -----------------------------------
            StromspeicherSimCtrl ctrl = new StromspeicherSimCtrl();
            StromspeicherOptimierungVorbereitung vorbereitung;
            try
            {
                vorbereitung = ctrl.BereiteOptimierungVor(sim, m_ID_Projekt);
            }
            catch (Exception ex)
            {
                return new SpeicherOptimierungErgebnis
                {
                    Erfolg = false,
                    Meldung = string.Format(MyResource.Resource.OPT_MSG_FEHLER, ex.Message)
                };
            }

            if (vorbereitung == null)
                return new SpeicherOptimierungErgebnis
                {
                    Erfolg = false,
                    Meldung = string.IsNullOrEmpty(ctrl.LetzterHinweis)
                        ? MyResource.Resource.SIMENG_SPEICHER_KEIN_SPEICHER
                        : ctrl.LetzterHinweis
                };

            // ---- Rechnung, Hintergrund-Task -------------------------------
            _optimierungAbbruch = new CancellationTokenSource();
            CancellationToken marke = _optimierungAbbruch.Token;

            IProgress<SpeicherOptimierungFortschritt> fortschritt =
                new Progress<SpeicherOptimierungFortschritt>(
                    f => melder(f != null ? f.Anteil : null, f != null ? f.Text : ""));

            try
            {
                return await Task.Run(
                    () => SpeicherOptimierungCtrl.Rechnen(vorbereitung, eingaben, fortschritt, marke));
            }
            catch (Exception ex)
            {
                return new SpeicherOptimierungErgebnis
                {
                    Erfolg = false,
                    Meldung = string.Format(MyResource.Resource.OPT_MSG_FEHLER, ex.Message)
                };
            }
            finally
            {
                CancellationTokenSource quelle = _optimierungAbbruch;
                _optimierungAbbruch = null;
                if (quelle != null) quelle.Dispose();
            }
        }

        /// <summary>
        /// Setzt die Abbruchmarke, falls ein Suchlauf läuft.
        /// </summary>
        /// <remarks>
        /// Die lokale Kopie und der Fang der <see cref="ObjectDisposedException"/> stehen
        /// hier aus demselben Grund wie in der abgelösten Maske: Zwischen dem Entsorgen
        /// im <c>finally</c> und dem Nullsetzen kann ein zweiter Aufruf hineinlaufen.
        /// </remarks>
        private void OptimierungAbbrechen()
        {
            CancellationTokenSource quelle = _optimierungAbbruch;
            if (quelle == null) return;

            try { quelle.Cancel(); }
            catch (ObjectDisposedException) { /* Lauf war ohnehin schon zu Ende */ }
        }

        /// <summary>
        /// Schreibt Kapazität und Leistung des Bestpunkts in die Gerätedaten.
        /// </summary>
        /// <remarks>
        /// <b>Kein automatisches Nachrechnen</b> — wörtlich wie im Vorläufer: Die
        /// Simulation ist danach nicht mehr aktuell, das steht in der Meldung, und der
        /// Anwender entscheidet selbst, wann er den Lauf wiederholt.
        /// </remarks>
        private Rueckmeldung OptimierungUebernehmen(double cNomKwh, double pKw)
        {
            StromspeicherSimCtrl ctrl = new StromspeicherSimCtrl();
            bool ok;
            try
            {
                ok = ctrl.UebernehmeAuslegung(m_ID_Projekt, cNomKwh, pKw);
            }
            catch (Exception ex)
            {
                return new Rueckmeldung(false, string.Format(MyResource.Resource.OPT_MSG_FEHLER, ex.Message));
            }

            if (!ok)
                return new Rueckmeldung(false, string.IsNullOrEmpty(ctrl.LetzterHinweis)
                    ? MyResource.Resource.OPT_MSG_UEBERNAHME_FEHLER
                    : ctrl.LetzterHinweis);

            return new Rueckmeldung(true, MyResource.Resource.OPT_MSG_UEBERNOMMEN);
        }

        /// <summary>
        /// Schreibt das Raster als CSV.
        /// </summary>
        /// <remarks>
        /// <b>Eigener Schreiber, nicht <c>CsvExportClass</c>.</b> Jene Klasse ist auf
        /// ZEITREIHEN zugeschnitten — sie stellt jeder Zeile einen Zeitstempel voran und
        /// rechnet zwischen 8 760 und 35 040 Werten um; eine Rastermatrix hat weder
        /// Zeitbezug noch Zeitraster. Den Text liefert
        /// <c>SpeicherOptimierungCtrl.RasterCsvText</c> (Semikolon, Dezimalkomma), hier
        /// kommt nur die Kodierung dazu: UTF-8 MIT BOM, damit deutsches Excel die Datei
        /// direkt richtig öffnet.
        ///
        /// <para>Der Dateiwähler wird <c>await</c>et
        /// (<c>Dienste.Datei.DateiSpeichernAsync</c>) — ein synchron geöffnetes
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
