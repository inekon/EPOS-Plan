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

        private KiChatHuelle(IWin32Window besitzer)
        {
            // Der Abschalter wird bei JEDEM Oeffnen neu gelesen: Die Verwaltung kann
            // ihn im laufenden Programm umlegen (Bestand :494-499).
            _hilfeBetrieb = KiEinwilligung.Abgeschaltet;

            _fenster = new BlazorDialogForm<KiChatDialog>(
                MyResource.Resource.KI_CHAT_TITEL, MASS, Gaben());

            _fenster.FormClosed += (s, e) => Aufraeumen();
            Einhaengen();

            if (besitzer is Form wirt && !wirt.IsDisposed) _fenster.Show(wirt);
            else _fenster.Show();
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
        public static void Oeffnen(IWin32Window besitzer = null)
        {
            // Die 25 Zeilen aus KiAufrufKnopf.Aufrufen (:223-247). Ein minimiertes
            // Fenster wird zuvor wiederhergestellt, sonst blinkt es nur in der
            // Taskleiste und der Klick sieht wirkungslos aus.
            KiChatHuelle offen = _offene;
            if (offen != null && offen._fenster != null && !offen._fenster.IsDisposed)
            {
                if (offen._fenster.WindowState == FormWindowState.Minimized)
                    offen._fenster.WindowState = FormWindowState.Normal;
                offen._fenster.Activate();
                return;
            }

            _offene = new KiChatHuelle(besitzer);
        }

        // ==================================================================
        //  Ein- und Aushaengen
        // ==================================================================

        private void Einhaengen()
        {
            _offene = this;

            // Der Ausfuehrer marshallt jeden Datenbankzugriff ueber dieses Fenster auf
            // den Oberflaechenfaden (Fachkonzept 3.4; seit W15b.0c ein Delegat statt
            // eines Control, Entscheid E-8).
            KiAusfuehrer.AufOberflaeche = ArbeitAufDemFenster;

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

            if (ReferenceEquals(KiAusfuehrer.AufOberflaeche,
                                (Func<Func<Task>, Task>)ArbeitAufDemFenster))
                KiAusfuehrer.AufOberflaeche = null;

            KiAusfuehrer.Ueberlagerung = null;
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
