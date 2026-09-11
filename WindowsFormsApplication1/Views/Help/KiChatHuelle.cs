using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Hilfe;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des KI-Chatfensters (iU9-W15b.7) — Ersatz für
    /// <c>Views/Help/Form_KiChat.cs</c> (1 704 Z., ohne Designer).
    ///
    /// <para><b>Nicht-modal mit Besitzer</b> (Entscheid E-6, Befund W15b-B29). Der
    /// Chat war die einzige Maske des Bestands, die mit <c>Show(besitzer)</c> geöffnet
    /// wurde — und das bleibt so: Wer den Assistenten fragt, will nebenher in der
    /// Maske weiterarbeiten, über die er fragt. Ein modaler Chat wäre ein
    /// Rückschritt.</para>
    ///
    /// <para><b>Ein Fenster, nicht zwei.</b> Ein zweites Öffnen holt das offene
    /// Fenster nach vorn, statt ein zweites anzulegen — die 25 Zeilen dafür standen
    /// bis W14a in <c>KiAufrufKnopf.Aufrufen</c> (<c>:223-247</c>) und liegen jetzt
    /// hier, wo sie hingehören. Das beseitigt zugleich den latenten Fehler des
    /// Bestands (Befund W15b-B28): Zwei Chatfenster setzten
    /// <c>KiChatService.Bestaetigungsweg</c> bedingungslos, und das Schließen des
    /// zweiten ließ ihn auf <c>null</c> — das erste konnte danach keine Schreibaktion
    /// mehr bestätigen.</para>
    ///
    /// <para><b>Was die Hülle hält und die Komponente nicht kennt:</b> den Dienst
    /// (<see cref="KiChatService"/>), den Ausführer, den Prompt-Verlauf (H8, die
    /// ZWEITE Liste), die Platzhaltertabelle der Sitzung, die Verfallsuhr der
    /// Bestätigung und den Weg auf den Oberflächenfaden.</para>
    ///
    /// <para><b>Keine DPI-Insel.</b> <c>BlazorDialogForm.ShowDialog</c> stellt den
    /// Faden für den modalen Lauf auf <c>PER_MONITOR_AWARE_V2</c>; ein
    /// nicht-modales <c>Show</c> hat keinen umschließenden Lauf, in dem das ginge.
    /// Der Chatinhalt wird ab 125 % also bitmapskaliert — derselbe Schönheitsfehler
    /// wie bei <c>BlazorSeite</c> (offener Entscheid iF21), kein Fehlschlag.</para>
    /// </summary>
    internal sealed partial class KiChatHuelle : IDisposable
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 720 × 580, Mindestmaß 620 × 460).</summary>
        private static readonly Size MASS = new Size(820, 660);

        /// <summary>Takt der Verfallsanzeige — wie im Bestand (<c>_verfallUhr</c>, 500 ms).</summary>
        private const int VERFALL_TAKT = 500;

        /// <summary>Das eine offene Chatfenster; <c>null</c> = keines.</summary>
        private static KiChatHuelle _offene;

        private readonly BlazorDialogForm<KiChatDialog> _fenster;

        /// <summary>Die ZWEITE Liste (H8): der Prompt-Verlauf, platzgehalten.</summary>
        private readonly List<string> _verlauf = new List<string>();

        /// <summary>Die Bezeichnertabelle der Sitzung.</summary>
        private readonly KiPlatzhalter _platzhalter = new KiPlatzhalter();

        private readonly bool _hilfeBetrieb;

        private KiChatSteuerung _steuerung;
        private KiBestaetigungsfrage _bestaetigungsweg;
        private KiFreigabe _offeneFreigabe;
        private System.Windows.Forms.Timer _verfallUhr;

        /// <summary>
        /// Der AUFRUFKONTEXT dieses Chatfensters (Auftrag #199): Bereich, Dialogname,
        /// Kennung und die vorbelegte Frage. Nie <c>null</c> — der Menüweg baut einen
        /// aus dem aktiven Bereich.
        /// </summary>
        private KiAufrufkontext _aufruf = new KiAufrufkontext();

        private KiChatHuelle(IWin32Window besitzer, KiAufrufkontext aufruf)
        {
            // MELDEN, auch wenn nichts mitkam: Ein null loescht einen alten Aufruf,
            // sonst zeigte der Menueweg auf den Dialog von vorhin. Der Weg aus einem
            // Dialog hat hier schon gemeldet (KiAssistentWeg) - dasselbe Objekt noch
            // einmal zu melden ist folgenlos.
            KiChatKontext.AufrufMelden(aufruf);
            _aufruf = aufruf ?? Menuekontext();

            // Der Abschalter wird bei JEDEM Oeffnen neu gelesen: Die Verwaltung kann
            // ihn im laufenden Programm umlegen (Bestand :494-499).
            _hilfeBetrieb = KiEinwilligung.Abgeschaltet;

            _fenster = new BlazorDialogForm<KiChatDialog>(
                MyResource.Resource.KI_CHAT_TITEL, MASS, Gaben());

            _fenster.FormClosed += (s, e) => Aufraeumen();
            Einhaengen();

            // WAS EINGEHAENGT IST, WIRD BEI EINEM FEHLSCHLAG WIEDER AUSGEHAENGT
            // (Befund KI-D-B-3): Einhaengen() belegt KiChatService.Bestaetigungsweg
            // und den Weg auf den Oberflaechenfaden. Bricht der Aufbau danach ab,
            // zeigten beide auf eine Huelle ohne Fenster - und der naechste
            // Bestaetigungslauf haette auf ein Fenster gewartet, das es nie gab.
            try
            {
                if (besitzer is Form wirt && !wirt.IsDisposed) _fenster.Show(wirt);
                else _fenster.Show();

                // DIE TASTATUR (Befund KI-D-B-1). Ein Show(besitzer) holt sie nicht von
                // selbst - anders als ein ShowDialog, das seine eigene Nachrichtenschleife
                // mitbringt. Ohne diese Zeile blieb die Eingabe bei dem Fenster, aus dessen
                // WebView2-Rueckruf der Klick kam, und das Chatfenster nahm kein Zeichen an.
                _fenster.TastaturUebergeben();
            }
            catch
            {
                Aufraeumen();
                if (!_fenster.IsDisposed) _fenster.Dispose();
                throw;
            }
        }

        // ==================================================================
        //  Einstieg
        // ==================================================================

        /// <summary>
        /// Öffnet den Assistenten mit dem aktuell erkannten Bedienkontext — oder holt
        /// ein bereits offenes Fenster nach vorn.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Auch bei abgeschalteter KI öffnet das Fenster.</b> Seit Paket F5 gilt der
        /// Hilfe-Betrieb (Fachkonzept 11.9): Der Chat geht auf und arbeitet als reine
        /// Hilfesuche — die Hilfe liegt lokal vor, kostet nichts und ist gerade dann
        /// nützlich, wenn der Dienst nicht zur Verfügung steht.
        /// </para>
        /// <para>
        /// <b>Keine Schutzwirkung geht verloren.</b> Dass ohne Einwilligung und bei
        /// gesetztem Abschalter nichts hinausgeht, trägt <c>KiEinwilligung</c> und der
        /// Einwilligungsriegel in <c>KiChatService</c> — nicht das geschlossene
        /// Fenster.
        /// </para>
        /// </remarks>
        public static void Oeffnen(IWin32Window besitzer = null, KiAufrufkontext aufruf = null)
        {
            // Die 25 Zeilen aus KiAufrufKnopf.Aufrufen (:223-247). Ein minimiertes
            // Fenster wird zuvor wiederhergestellt, sonst blinkt es nur in der
            // Taskleiste und der Klick sieht wirkungslos aus.
            KiChatHuelle offen = _offene;
            if (offen != null && offen.Steht)
            {
                if (offen._fenster.WindowState == FormWindowState.Minimized)
                    offen._fenster.WindowState = FormWindowState.Normal;

                // Nach vorn holen UND die Tastatur mitgeben (Befund KI-D-B-1): Ein
                // blosses Activate() stellt das Fenster vor die anderen, laesst den
                // Tastaturzeiger aber dort, wo er war - also in der WebView2, aus deren
                // Rueckruf der zweite Klick kam.
                offen._fenster.TastaturUebergeben();

                // EIN Fenster, aber ein NEUER Kontext (Auftrag #199): Wer aus einem
                // zweiten Dialog fragt, bekommt dessen Bereich und dessen vorbelegte
                // Frage - sonst zeigte der offene Chat weiter auf die Maske von
                // vorhin. Der Gespraechsverlauf bleibt dabei stehen; er gehoert der
                // Sitzung, nicht dem Dialog.
                offen.KontextSetzen(aufruf);
                return;
            }

            // EIN HALB GEBAUTES FENSTER DARF NICHT SPERREN (Befund KI-D-B-3,
            // Auftrag #228). Das Feld wurde bis dahin in Einhaengen() gesetzt,
            // also VOR Show() und TastaturUebergeben(): Warf der Aufbau danach,
            // blieb _offene auf einer Huelle stehen, deren Fenster nie erschien
            // und deshalb nie ein FormClosed meldete - jedes weitere Oeffnen
            // "holte es nach vorn" und tat sichtbar nichts. Jetzt traegt das Feld
            // nur, was fertig gebaut ist, und ein Fehlschlag ist zu sehen statt
            // zu schweigen.
            _offene = null;
            try
            {
                _offene = new KiChatHuelle(besitzer, aufruf);
            }
            catch (Exception ex)
            {
                _offene = null;
                Protokoll("Das Chatfenster liess sich nicht aufbauen: " + ex);
                try { Dienste.Dialog.Meldung(ex.Message); } catch { }
            }
        }

        /// <summary>
        /// <c>true</c>, solange das Fenster dieser Hülle wirklich steht — gebaut,
        /// gezeigt und nicht entsorgt.
        /// </summary>
        /// <remarks>
        /// <c>IsHandleCreated</c> gehört dazu (Befund KI‑D‑B‑3): Eine Hülle, deren
        /// <c>Show</c> nie gelaufen ist, hat ein Fenster, das weder entsorgt noch
        /// sichtbar ist — „nicht entsorgt" allein wäre also kein Beleg dafür, dass
        /// der Anwender den Assistenten vor sich hat.
        /// </remarks>
        private bool Steht
            => _fenster != null && !_fenster.IsDisposed && _fenster.IsHandleCreated;

        private static void Protokoll(string satz)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[KiChat] " + satz);
                System.Diagnostics.Trace.WriteLine("[KiChat] " + satz);
            }
            catch { }
        }

        /// <summary>
        /// Der Aufrufkontext des MENÜWEGS: kein Dialog, keine Meldung — der Bereich,
        /// in dem der Anwender gerade arbeitet (<c>HilfeKontext</c> über
        /// <c>KiChatKontext.AktuellerBereich</c>).
        /// </summary>
        /// <remarks>
        /// Der Menüweg bleibt damit, was er war, und trägt seinen Kontext trotzdem
        /// ausdrücklich — statt ihn, wie bis #199, erst im Augenblick jeder Frage aus
        /// dem gerade aktiven Fenster zu erraten.
        /// </remarks>
        private static KiAufrufkontext Menuekontext()
        {
            return new KiAufrufkontext { Bereich = KiChatKontext.AktuellerBereich() };
        }

        /// <summary>
        /// Stellt ein bereits offenes Fenster auf einen neuen Aufrufkontext ein
        /// (Auftrag #199).
        /// </summary>
        private void KontextSetzen(KiAufrufkontext aufruf)
        {
            _aufruf = aufruf ?? Menuekontext();
            KiChatKontext.AufrufMelden(aufruf);

            KiChatSteuerung s = _steuerung;
            if (s == null) return;

            // Ein Parametersatz laesst sich nach dem ersten Zeichnen nicht mehr
            // austauschen (RootComponents.Add nimmt ihn EINMAL entgegen). Die
            // Komponente hat sich deshalb mit einem Setzweg angemeldet - dieselbe
            // Bauart wie bei der Bestaetigungsschicht (W15b-B28).
            try { s.Kontext(Kontextangabe()); } catch (Exception) { }
        }

        /// <summary>Die Kontextangaben für die Komponente (Auftrag #199, seit #227 fünf).</summary>
        private KiKontextangabe Kontextangabe()
        {
            return new KiKontextangabe(HilfeKontext.Beschreibung(),
                                       _aufruf.Dialogname ?? "",
                                       _aufruf.Kennung ?? "",
                                       _aufruf.Frage ?? "",
                                       _aufruf.Hilfeschluessel ?? "");
        }

        // ==================================================================
        //  Ein- und Aushaengen
        // ==================================================================

        private void Einhaengen()
        {
            // HIER STAND "_offene = this" (Befund KI-D-B-3, Auftrag #228). Das war
            // zu frueh: Einhaengen() laeuft VOR Show(), und ein Fehlschlag danach
            // liess das Feld auf einer Huelle stehen, deren Fenster nie erschien.
            // Gesetzt wird es jetzt in Oeffnen(), wenn der Bau gelungen ist.

            // Der Ausfuehrer marshallt jeden Datenbankzugriff ueber dieses Fenster auf
            // den Oberflaechenfaden (Fachkonzept 3.4; seit W15b.0c ein Delegat statt
            // eines Control, Entscheid E-8).
            KiAusfuehrungWindows.Aktuell.AufOberflaeche = ArbeitAufDemFenster;

            _bestaetigungsweg = BestaetigungFragen;
            KiChatService.Bestaetigungsweg = _bestaetigungsweg;

            _verfallUhr = new System.Windows.Forms.Timer { Interval = VERFALL_TAKT };
            _verfallUhr.Tick += (s, e) => VerfallAktualisieren();
        }

        private void Aufraeumen()
        {
            if (ReferenceEquals(_offene, this)) _offene = null;

            if (_verfallUhr != null) { _verfallUhr.Stop(); _verfallUhr.Dispose(); _verfallUhr = null; }

            // Eine offene Vorschau darf das Fenster nicht ueberleben: Wer schliesst,
            // hat nicht bestaetigt (Bestand OnFormClosed, :131-143).
            _offeneFreigabe = null;
            if (_steuerung != null) _steuerung.Beenden(false);

            if (ReferenceEquals(KiChatService.Bestaetigungsweg, _bestaetigungsweg))
                KiChatService.Bestaetigungsweg = null;

            if (ReferenceEquals(KiAusfuehrungWindows.Aktuell.AufOberflaeche,
                                (Func<Func<Task>, Task>)ArbeitAufDemFenster))
                KiAusfuehrungWindows.Aktuell.AufOberflaeche = null;

            KiAusfuehrungWindows.Aktuell.Ueberlagerung = null;

            // Die Fortschrittssenke gehoert der Komponente (Auftrag #214); mit dem
            // Fenster faellt sie. Ein Melder auf eine Komponente, die es nicht mehr
            // gibt, waere schlechter als gar keiner.
            if (_steuerung != null &&
                ReferenceEquals(KiAusfuehrungWindows.Aktuell.Fortschritt, _steuerung.Fortschritt))
                KiAusfuehrungWindows.Aktuell.Fortschritt = null;

            // Der Aufrufkontext gilt fuer das FENSTER (Auftrag #199): Ist es zu,
            // beantwortet wieder die Fensterermittlung, in welchem Bereich der
            // Anwender arbeitet.
            KiChatKontext.AufrufMelden(null);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Aufraeumen();
            if (_fenster != null && !_fenster.IsDisposed) _fenster.Dispose();
        }

        // ==================================================================
        //  Der Weg auf den Oberflaechenfaden (W15b.0c)
        // ==================================================================

        private Task ArbeitAufDemFenster(Func<Task> arbeit)
        {
            if (arbeit == null) return Task.CompletedTask;
            if (_fenster == null || _fenster.IsDisposed || !_fenster.IsHandleCreated
                || !_fenster.InvokeRequired)
                return arbeit();

            var quelle = new TaskCompletionSource<bool>();
            _fenster.BeginInvoke((MethodInvoker)delegate
            {
                try
                {
                    arbeit();
                    quelle.SetResult(true);
                }
                catch (Exception ex) { quelle.SetException(ex); }
            });
            return quelle.Task;
        }

        // ==================================================================
        //  Die Bestaetigungsschicht (Fachkonzept 3.5)
        // ==================================================================

        /// <summary>
        /// Der Weg, über den <see cref="KiChatService"/> die Bestätigung einholt.
        /// </summary>
        /// <remarks>
        /// Liefert eine Aufgabe, die erst mit dem Klick des Anwenders erfüllt wird —
        /// das ist die Stelle, an der die Rundenschleife des Dienstes stehen bleibt,
        /// ohne einen Thread zu belegen. Ist das Fenster nicht (mehr) da, kommt sofort
        /// eine Ablehnung zurück; ein wartender Dienst darf nicht auf ein geschlossenes
        /// Fenster hoffen.
        /// </remarks>
        private async Task<KiEntscheidung> BestaetigungFragen(KiFreigabe freigabe,
                                                              CancellationToken abbruch)
        {
            if (freigabe == null || _steuerung == null
                || _fenster == null || _fenster.IsDisposed)
                return KiEntscheidung.Abgelehnt;

            _offeneFreigabe = freigabe;

            // Der Verfall wird HIER mitgezaehlt und nicht der Oberflaeche ueberlassen:
            // Ein Fenster, dessen Uhr steht, duerfte sonst beliebig lange bestaetigen.
            using (abbruch.Register(() => { if (_steuerung != null) _steuerung.Beenden(false); }))
            {
                await ArbeitAufDemFenster(() =>
                {
                    if (_verfallUhr != null) _verfallUhr.Start();
                    return Task.CompletedTask;
                }).ConfigureAwait(true);

                bool erteilt = await _steuerung.Zeigen(freigabe.Text, Verfallstext(freigabe))
                                               .ConfigureAwait(true);

                _offeneFreigabe = null;
                if (_verfallUhr != null) _verfallUhr.Stop();

                if (erteilt) return KiEntscheidung.Erteilt;
                if (freigabe.IstVerfallen()) return KiEntscheidung.Verfallen;
                return abbruch.IsCancellationRequested
                    ? KiEntscheidung.Abgebrochen
                    : KiEntscheidung.Abgelehnt;
            }
        }

        /// <summary>Zählt die Frist herunter und beendet die Vorschau beim Verfall.</summary>
        private void VerfallAktualisieren()
        {
            KiFreigabe f = _offeneFreigabe;
            if (f == null || _steuerung == null) { if (_verfallUhr != null) _verfallUhr.Stop(); return; }

            if (f.Restzeit() <= TimeSpan.Zero)
            {
                if (_verfallUhr != null) _verfallUhr.Stop();
                f.AlsVerfallenMarkieren();
                _steuerung.Beenden(false);
                return;
            }

            _steuerung.Verfall(Verfallstext(f));
        }

        private static string Verfallstext(KiFreigabe freigabe)
        {
            return string.Format(MyResource.Resource.KI_AKT_BESTAETIGUNG_VERFALL,
                                 (int)Math.Ceiling(freigabe.Restzeit().TotalSeconds));
        }
    }
}
