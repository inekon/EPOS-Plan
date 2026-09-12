using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using EPOS.UI.Bausteine;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Seiten.Strom;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Ansicht „Stromspeicher-Auslegung" (Paket P3, Auftrag #192)
    /// — die Plattformseite von <c>EPOS.UI/Seiten/Strom/StromspeicherAuslegungSeite.razor</c>.
    ///
    /// <para><b>Was sie tut und was nicht.</b> Sie legt genau das um den Kern-Controller
    /// <see cref="StromspeicherAuslegungCtrl"/>, was die PLATTFORM beisteuert: den
    /// Hintergrundfaden (<c>Task.Run</c>), die Abbruchmarke, den Dateiwähler und den
    /// Dateischreiber. <b>Keine Fachrechnung, kein SQL</b> — beides liegt im Kern
    /// (Hausregel: Datenbankseite in den Kern).</para>
    ///
    /// <para><b>Woher der Simulationslauf kommt.</b> Jede EPOS-Zeitreihe stammt aus
    /// einem abgeschlossenen Lauf, und den hält unter Windows die ERGEBNISSEITE
    /// (<c>SimulationErgebnisHuelle.sim</c>). Deren Knopf „Speicherflotte &amp; Auslegung
    /// öffnen" MELDET deshalb zuerst ihren Arbeitsgang an (<see cref="Anmelden"/>) und
    /// wechselt erst dann die Ansicht. Kommt jemand ohne Anmeldung hierher — später über
    /// einen Menüpunkt oder eine Kachel —, entsteht ein Arbeitsgang für das offene
    /// Projekt OHNE Lauf, und die Seite bietet an, ihn zu rechnen
    /// (<see cref="Simulationslauf"/>).</para>
    ///
    /// <para><b>Muster:</b> <c>AssistentHuelle.AnsichtGaben</c> (#62b) — ein DELEGAT je
    /// Betreten statt eines stehenden Wörterbuchs, weil der Arbeitsgang beim Betreten
    /// beginnt.</para>
    /// </summary>
    internal sealed class StromspeicherAuslegungHuelle
    {
        /// <summary>Der angemeldete Arbeitsgang; <c>null</c> = es hat sich keiner angemeldet.</summary>
        private static StromspeicherAuslegungHuelle _angemeldet;

        private readonly StromspeicherAuslegungCtrl _ctrl;
        private readonly Action _nachzug;
        private readonly SimulationWaermebedarf _waerme = new SimulationWaermebedarf();
        private readonly SimulationStrombedarf _strom = new SimulationStrombedarf();

        /// <summary>Die Abbruchmarke des laufenden Hintergrundlaufs; <c>null</c> = keiner läuft.</summary>
        private CancellationTokenSource _abbruch;

        private StromspeicherAuslegungHuelle(StromspeicherAuslegungCtrl ctrl, Action nachzug)
        {
            _ctrl = ctrl ?? throw new ArgumentNullException(nameof(ctrl));
            _nachzug = nachzug;
        }

        /// <summary>
        /// Meldet den Arbeitsgang an, den das nächste Betreten der Ansicht übernimmt.
        /// </summary>
        /// <param name="ctrl">Der Controller samt seinem Simulationslauf.</param>
        /// <param name="nachzug">
        /// Wird gerufen, sobald an der Projektflotte geschrieben wurde — die
        /// Ergebnisseite zieht daran ihr gespeichertes Ergebnis nach. <c>null</c> = kein
        /// Nachzug.
        /// </param>
        internal static void Anmelden(StromspeicherAuslegungCtrl ctrl, Action nachzug)
        {
            _angemeldet = new StromspeicherAuslegungHuelle(ctrl, nachzug);
        }

        /// <summary>
        /// Der Parametersatz der Ansicht. Ohne angemeldeten Arbeitsgang entsteht einer
        /// für das offene Projekt — dann ohne Simulationslauf.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> AnsichtGaben()
        {
            StromspeicherAuslegungHuelle huelle = _angemeldet;
            if (huelle == null)
            {
                int id = Dienste.Projekt != null ? Dienste.Projekt.Id : 0;
                if (id <= 0) return null;
                huelle = new StromspeicherAuslegungHuelle(new StromspeicherAuslegungCtrl(id), null);
            }

            // Der angemeldete Arbeitsgang gilt fuer GENAU EIN Betreten: Wer die Ansicht
            // spaeter ueber einen anderen Weg oeffnet, bekommt einen frischen Stand.
            _angemeldet = null;
            return huelle.Gaben();
        }

        private IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Dienste"] = DiensteSatz(),
                ["ProjektText"] = Projektzeile()
            };
        }

        /// <summary>„Projekt „…"" — die Kopfzeile der Ansicht; leer ohne offenes Projekt.</summary>
        private string Projektzeile()
        {
            string name = Dienste.Projekt != null ? Dienste.Projekt.Name : "";
            return string.IsNullOrWhiteSpace(name)
                ? ""
                : string.Format(CultureInfo.CurrentCulture, MyResource.Resource.FLOTTE_SEITE_PROJEKT, name);
        }

        private StromspeicherAuslegungDienste DiensteSatz()
        {
            return new StromspeicherAuslegungDienste
            {
                Vorgaben = _ctrl.Vorgaben,
                LaufVorhanden = () => _ctrl.LaufVorhanden,
                Simulationslauf = Simulationslauf,

                FlotteRechnen = FlotteRechnen,
                Abbrechen = Abbrechen,

                EinstellungenSpeichern = EinstellungenSpeichern,
                ProfilSpeichern = ProfilSpeichern,
                DateiWaehlen = DateiWaehlen,
                Csv = Csv,

                AuslegungUebernehmen = AuslegungUebernehmen,
                LeistungspreisSchreiben = LeistungspreisSchreiben,

                ProjektflotteAktiv = _ctrl.ProjektflotteAktiv,
                ProjektflotteAktivieren = ProjektflotteAktivieren,
                ProjektflotteDeaktivieren = ProjektflotteDeaktivieren,

                Vorpruefen = eingaben => _ctrl.Vorpruefen(eingaben),
                PeakZielVorschlag = eingaben => _ctrl.PeakZielVorschlag(eingaben),
                PeakZielBestimmen = PeakZielBestimmen,

                // „Speicher hinzufuegen" aus einer Projektanlage oder aus dem Katalog
                // (Auftrag #239). Alle vier Wege sind reine DURCHREICHEN in den Kern —
                // die Abbildung Katalog -> FlottenEinheit steht an EINER Stelle
                // (SpeicherFlottenStudieCtrl), nicht hier.
                Projektanlagen = _ctrl.Projektanlagen,
                EinheitAusProjektanlage = _ctrl.EinheitAusProjektanlage,
                Katalogzeilen = StromspeicherStammCtrl.Katalogfilterzeilen,
                Katalogprofil = Katalogfilterprofil.MitVerwendung(Anlagenart.Stromspeicher, Text_),
                EinheitAusKatalog = SpeicherFlottenStudieCtrl.EinheitAusKatalog
            };
        }

        /// <summary>
        /// Ein Ressourcentext mit Rückfall auf den Schlüssel — der Übersetzer des
        /// Katalogfilterprofils (Muster <c>StromspeicherHuelle.Text_</c>; der Kern kennt
        /// keine Anzeigetexte, <c>Katalogfilterprofil.Finde</c> nimmt sie entgegen).
        /// </summary>
        private static string Text_(string schluessel)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { /* ein fehlender Schluessel darf keine Spalte kosten */ }
            return string.IsNullOrEmpty(t) ? schluessel : t;
        }

        // =================================================================
        //  Der Simulationslauf (Muster iU9-W11a)
        // =================================================================

        /// <summary>
        /// Rechnet den fehlenden Simulationslauf nach: Vorprüfung, Bedarf und Bestückung
        /// auf dem Bedienfaden, die Rechnung in <c>Task.Run</c>.
        /// </summary>
        private async Task<Rueckmeldung> Simulationslauf(Action<double?, string> melder)
        {
            if (_abbruch != null)
                return new Rueckmeldung(false, MyResource.Resource.FLOTTE_DLG_MSG_LAEUFT);

            SimulationControl neuerLauf;
            string fehler = _ctrl.SimulationslaufVorbereiten(_waerme, _strom, out neuerLauf);
            if (fehler != null) return new Rueckmeldung(false, fehler);

            _abbruch = new CancellationTokenSource();
            CancellationToken marke = _abbruch.Token;
            IProgress<LaufFortschritt> fortschritt = new Progress<LaufFortschritt>(
                f => melder(f != null ? f.Anteil : (double?)null, f != null ? (f.Text ?? "") : ""));

            try
            {
                string abbruch = await Kulturweitergabe.Starten(
                    () => _ctrl.SimulationslaufRechnen(neuerLauf, fortschritt, marke), marke);
                return abbruch == null ? Rueckmeldung.Still : new Rueckmeldung(false, abbruch);
            }
            catch (OperationCanceledException)
            {
                return new Rueckmeldung(false, "");
            }
            catch (Exception ex)
            {
                return new Rueckmeldung(false, ex.Message);
            }
            finally
            {
                Aufraeumen();
            }
        }

        // =================================================================
        //  Die zwei Rechenwege
        // =================================================================

        /// <summary>
        /// Die Flottenstudie: Quellen und Kosten auf dem Bedienfaden, die Rechnung in
        /// <c>Task.Run</c>. Der Fortschritt ist auf höchstens eine Meldung je 150 ms
        /// gedrosselt — der Optimierer meldet je Kandidat.
        /// </summary>
        private async Task<SpeicherFlottenErgebnis> FlotteRechnen(
            SpeicherOptimierungEingaben eingaben, Action<double?, string> melder)
        {
            if (_abbruch != null)
                return new SpeicherFlottenErgebnis { Meldung = MyResource.Resource.FLOTTE_DLG_MSG_LAEUFT };

            string meldung;
            StromspeicherOptimierungVorbereitung vorbereitung = _ctrl.FlotteVorbereiten(eingaben, out meldung);
            if (vorbereitung == null) return new SpeicherFlottenErgebnis { Meldung = meldung };
            Nachziehen();

            _abbruch = new CancellationTokenSource();
            CancellationToken marke = _abbruch.Token;
            DateTime letztes = DateTime.MinValue;
            IProgress<FlottenFortschritt> fortschritt = new Progress<FlottenFortschritt>(p =>
            {
                if (p.Abgeschlossen != p.Gesamt && (DateTime.UtcNow - letztes).TotalMilliseconds < 150) return;
                letztes = DateTime.UtcNow;
                melder(p.Gesamt > 0 ? (double)p.Abgeschlossen / p.Gesamt : (double?)null,
                    string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.FLOTTE_DLG_STATUS_VARIANTE, p.Abgeschlossen, p.Gesamt));
            });

            try
            {
                return await Kulturweitergabe.Starten(
                    () => _ctrl.FlotteRechnen(vorbereitung, fortschritt, marke), marke);
            }
            catch (OperationCanceledException)
            {
                return new SpeicherFlottenErgebnis
                { Abgebrochen = true, Meldung = MyResource.Resource.FLOTTE_DLG_MSG_ABGEBROCHEN };
            }
            catch (Exception ex)
            {
                return new SpeicherFlottenErgebnis { Meldung = ex.Message };
            }
            finally
            {
                Aufraeumen();
            }
        }

        /// <summary>
        /// Die Bisektion „Peak-Ziel bestimmen" — bis zu zwölf Jahresläufe in
        /// <c>Task.Run</c> (Anwenderentscheid SD‑Q3).
        /// </summary>
        private async Task<FlottenPeakZielErgebnis> PeakZielBestimmen(
            SpeicherOptimierungEingaben eingaben, Action<double?, string> melder)
        {
            string meldung;
            StromspeicherOptimierungVorbereitung vorbereitung = _ctrl.FlotteVorbereiten(eingaben, out meldung);
            if (vorbereitung == null) throw new InvalidOperationException(meldung);
            Nachziehen();

            _abbruch = new CancellationTokenSource();
            CancellationToken marke = _abbruch.Token;
            IProgress<FlottenPeakZielFortschritt> fortschritt =
                new Progress<FlottenPeakZielFortschritt>(p => melder(
                    p.HoechsteLaeufe > 0 ? (double)p.Lauf / p.HoechsteLaeufe : (double?)null,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.FLOTTE_PEAK_LAUF,
                        p.Lauf, p.HoechsteLaeufe,
                        p.PeakZielKw.ToString("0.#", CultureInfo.CurrentCulture),
                        p.ErreichteSpitzeKw.ToString("0.#", CultureInfo.CurrentCulture))));

            try
            {
                return await Kulturweitergabe.Starten(
                    () => _ctrl.PeakZielBestimmen(vorbereitung, fortschritt, marke), marke);
            }
            finally
            {
                Aufraeumen();
            }
        }

        /// <summary>Setzt die Abbruchmarke, falls ein Lauf läuft.</summary>
        /// <remarks>
        /// Die lokale Kopie und der Fang der <see cref="ObjectDisposedException"/> stehen
        /// hier aus demselben Grund wie in der abgelösten Maske: Zwischen dem Entsorgen
        /// im <c>finally</c> und dem Nullsetzen kann ein zweiter Aufruf hineinlaufen.
        /// </remarks>
        private void Abbrechen()
        {
            CancellationTokenSource quelle = _abbruch;
            if (quelle == null) return;
            try { quelle.Cancel(); }
            catch (ObjectDisposedException) { /* Lauf war ohnehin schon zu Ende */ }
        }

        private void Aufraeumen()
        {
            CancellationTokenSource quelle = _abbruch;
            _abbruch = null;
            if (quelle != null) quelle.Dispose();
        }

        private void Nachziehen()
        {
            if (_ctrl.ProjektflotteGeaendert && _nachzug != null) _nachzug();
        }

        // =================================================================
        //  Speichern, Dateien, Bilder
        // =================================================================

        private Task<string> EinstellungenSpeichern(SpeicherOptimierungEingaben eingaben)
        {
            string fehler = _ctrl.EinstellungenSpeichern(eingaben);
            Nachziehen();
            return Task.FromResult(fehler);
        }

        private Task<SpeicherOptimierungVorgaben> ProfilSpeichern(
            SpeicherOptimierungEingaben eingaben, string name)
        {
            return Task.FromResult(_ctrl.ProfilSpeichern(eingaben, name));
        }

        /// <summary>Wählt eine CSV-Datei und übergibt ihren Inhalt pfadfrei an Razor.</summary>
        private async Task<SpeicherImportDatei> DateiWaehlen()
        {
            string pfad = await Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.FLOTTE_DLG_DATEI_TITEL, "CSV-Zeitreihen (*.csv)|*.csv",
                Dienste.Pfade.Dokumente);
            if (string.IsNullOrEmpty(pfad)) return new SpeicherImportDatei();
            if (!string.Equals(Path.GetExtension(pfad), ".csv", StringComparison.OrdinalIgnoreCase))
                throw new IOException(MyResource.Resource.FLOTTE_DLG_DATEI_NUR_CSV);

            const long maximal = 20L * 1024L * 1024L;
            var info = new FileInfo(pfad);
            if (!info.Exists) throw new FileNotFoundException(MyResource.Resource.FLOTTE_DLG_DATEI_FEHLT, pfad);
            if (info.Length > maximal) throw new IOException(MyResource.Resource.FLOTTE_DLG_DATEI_ZU_GROSS);

            byte[] inhalt = await Kulturweitergabe.Starten(() => File.ReadAllBytes(pfad));
            if (inhalt.LongLength > maximal) throw new IOException(MyResource.Resource.FLOTTE_DLG_DATEI_ZU_GROSS);
            return new SpeicherImportDatei { Dateiname = Path.GetFileName(pfad), Inhalt = inhalt };
        }

        /// <summary>Schreibt einen CSV-Text als Datei; UTF-8 MIT BOM für deutsches Excel.</summary>
        private async Task<Rueckmeldung> Csv(string csv)
        {
            if (string.IsNullOrEmpty(csv))
                return new Rueckmeldung(false, MyResource.Resource.OPT_MSG_KEIN_ERGEBNIS);

            string vorschlag = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.OPT_DATEI, _ctrl.ProjektId);

            string pfad = await Dienste.Datei.DateiSpeichernAsync(
                MyResource.Resource.OPT_CSV_TITEL, "CSV (*.csv)|*.csv", vorschlag);
            if (string.IsNullOrEmpty(pfad)) return Rueckmeldung.Still;

            try
            {
                File.WriteAllText(pfad, csv, new System.Text.UTF8Encoding(true));
            }
            catch (Exception ex)
            {
                return new Rueckmeldung(false, string.Format(MyResource.Resource.OPT_CSV_FEHLER, ex.Message));
            }

            return new Rueckmeldung(true, string.Format(MyResource.Resource.OPT_CSV_GESCHRIEBEN, pfad));
        }

        /// <summary>
        /// Schreibt Kapazität und Leistung EINER Einheit in die Speicheranlage des
        /// Projekts (Schritt 5 der Ansicht). <b>Er bleibt mit SD‑E‑8</b>: Ohne
        /// aktivierte Projektflotte rechnet der Projektlauf die Einzelanlage (SD‑Q2).
        /// </summary>
        private Rueckmeldung AuslegungUebernehmen(double kwh, double kw)
        {
            (bool Erfolg, string Text) antwort = _ctrl.AuslegungUebernehmen(kwh, kw);
            return new Rueckmeldung(antwort.Erfolg, antwort.Text);
        }

        private void LeistungspreisSchreiben(double wert)
        {
            (bool Erfolg, string Text) antwort = _ctrl.LeistungspreisSchreiben(wert);
            // Die Seite hat fuer diesen Weg keine eigene Meldezeile; ein Fehlschlag geht
            // wenigstens in die Konsole und nicht ins Leere (Befund W11b-B-29).
            if (!antwort.Erfolg)
                Console.WriteLine("Der Leistungspreis konnte nicht geschrieben werden: " + antwort.Text);
        }

        private Task<string> ProjektflotteAktivieren(SpeicherFlottenErgebnis ergebnis)
        {
            string fehler = _ctrl.ProjektflotteAktivieren(ergebnis);
            Nachziehen();
            return Task.FromResult(fehler);
        }

        private Task<string> ProjektflotteDeaktivieren()
        {
            string fehler = _ctrl.ProjektflotteDeaktivieren();
            Nachziehen();
            return Task.FromResult(fehler);
        }
    }
}
