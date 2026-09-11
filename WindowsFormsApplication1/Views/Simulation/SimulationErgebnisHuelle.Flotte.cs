using System;
using System.Threading;
using System.Threading.Tasks;
using SpeicherEngine;

namespace WindowsFormsApplication1;

internal sealed partial class SimulationErgebnisHuelle
{
    private bool _flotteProjektGeaendert;
    private Task<string> FlottenProjektUebernehmen(SpeicherFlottenErgebnis ergebnis)
    {
        try
        {
            SpeicherFlottenProjektCtrl.Aktivieren(m_ID_Projekt, ergebnis);
            var aktuell = ergebnis.Eingaben.Kopie();
            aktuell.Auslegung.Flotte = SpeicherAuslegungKopie.Von(ergebnis.Konfiguration);
            aktuell.Auslegung.FlotteImProjektAktiv = false;
            aktuell.Auslegung.FlottenProjektbetriebDeaktiviert = false;
            SpeicherAuslegungCtrl.Speichern(m_ID_Projekt, SpeicherAuslegungCtrl.Anlage(m_ID_Projekt),
                SpeicherAuslegungCtrl.AktuellerStand, aktuell);
            _ergebnisGueltig = false;
            _flotteProjektGeaendert = true;
            return Task.FromResult("");
        }
        catch (Exception ex) { return Task.FromResult(ex.Message); }
    }
    private Task<string> FlottenProjektDeaktivieren()
    {
        try
        {
            var aktuell = OptimierungVorgaben().Eingaben.Kopie();
            aktuell.Auslegung.FlottenProjektbetriebDeaktiviert = true;
            SpeicherFlottenProjektCtrl.Deaktivieren(m_ID_Projekt);
            SpeicherAuslegungCtrl.Speichern(m_ID_Projekt, SpeicherAuslegungCtrl.Anlage(m_ID_Projekt),
                SpeicherAuslegungCtrl.AktuellerStand, aktuell);
            _ergebnisGueltig = false;
            _flotteProjektGeaendert = true;
            return Task.FromResult("");
        }
        catch (Exception ex) { return Task.FromResult(ex.Message); }
    }
    /// <summary>Beschafft Quellen einmal im Bedienfaden und rechnet die Flotte im Hintergrund.</summary>
    private async Task<SpeicherFlottenErgebnis> OptimierungFlottenRechnen(
        SpeicherOptimierungEingaben eingaben, Action<double?, string> melder)
    {
        if (_optimierungAbbruch != null)
            return new SpeicherFlottenErgebnis { Meldung = "Es läuft bereits eine Speicherberechnung." };
        try
        {
            var vorbereitet = SpeicherAuslegungCtrl.Vorbereiten(sim, m_ID_Projekt, eingaben.Kopie());
            if (vorbereitet == null) return new SpeicherFlottenErgebnis { Meldung = "Es fehlt eine Speicheranlage als Ausgangspunkt." };
            string fehler = await OptimierungEinstellungenSpeichern(vorbereitet.Eingaben);
            if (!string.IsNullOrEmpty(fehler)) return new SpeicherFlottenErgebnis { Meldung = fehler };
            _optimierungAbbruch = new CancellationTokenSource();
            var token = _optimierungAbbruch.Token;
            var letztes = DateTime.MinValue;
            var fortschritt = new Progress<FlottenFortschritt>(p =>
            {
                if (p.Abgeschlossen != p.Gesamt && (DateTime.UtcNow - letztes).TotalMilliseconds < 150) return;
                letztes = DateTime.UtcNow;
                melder(p.Gesamt > 0 ? (double)p.Abgeschlossen / p.Gesamt : null,
                    $"Flottenvariante {p.Abgeschlossen} von {p.Gesamt}");
            });
            return await Task.Run(() => SpeicherFlottenStudieCtrl.Rechnen(vorbereitet,
                new SpeicherPlanung.OrToolsFlottenPlaner(), fortschritt, token), token);
        }
        catch (OperationCanceledException)
        { return new SpeicherFlottenErgebnis { Abgebrochen = true, Meldung = "Berechnung abgebrochen." }; }
        catch (Exception ex)
        { return new SpeicherFlottenErgebnis { Meldung = ex.Message }; }
        finally
        {
            _optimierungAbbruch?.Dispose();
            _optimierungAbbruch = null;
        }
    }
}
