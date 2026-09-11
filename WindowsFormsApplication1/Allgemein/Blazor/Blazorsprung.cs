using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der SPRUNG AUS EINEM BLAZOR-EREIGNIS in ein modales Fenster — Befund
    /// <b>W16b‑B‑1</b> der Windows-Abnahme vom 05.09.2026.
    ///
    /// <para><b>Die Lage.</b> Seit W16b/W16c sind Startseite UND Hauptfenster
    /// Razor. Jeder Kachelklick und jeder Menüpunkt läuft damit als
    /// Blazor-Ereignis der EINEN <c>BlazorWebView</c> des Fensters — und öffnet
    /// von dort aus ein modales <see cref="BlazorDialogForm{T}"/> mit einer
    /// ZWEITEN WebView. Der Blazor-Verteiler ist der Bedienfaden
    /// (<c>WindowsFormsDispatcher</c> ruft auf ihm synchron), das Ereignis kommt
    /// aber aus dem <c>WebMessageReceived</c>-Rückruf der ersten WebView2:
    /// <c>ShowDialog</c> öffnet seine verschachtelte Nachrichtenschleife also
    /// INNERHALB eines laufenden WebView2-Rückrufs, und die zweite WebView2 soll
    /// sich darin aufbauen.</para>
    ///
    /// <para><b>Warum das neu ist.</b> Bis W16b war das ausdrücklich vermieden:
    /// <c>Sprungbruecke</c> (iU9‑W2.2) führt aus einem Razor-Rückruf
    /// <b>ausschließlich WinForms-Ziele</b> — „Ziele, die selbst eine
    /// <c>BlazorDialogForm</c> sind, gehören NICHT hierher: Zwei WebViews
    /// übereinander … (Risiko R2)"; für Blazor-Ziele nimmt das Haus seit W4 den
    /// Baustein <c>Ueberlagerung</c> im selben Fenster. Mit der Razor-Startseite
    /// und dem Razor-Menüband ist genau dieser Weg für 21 Kacheln und 55
    /// Menüpunkte zur Regel geworden, ohne dass jemand die Regel geändert
    /// hätte.</para>
    ///
    /// <para><b>Was diese Klasse tut.</b> Sie lässt das laufende Ereignis ZU
    /// ENDE laufen und führt den Sprung erst danach aus — eine gepostete
    /// Nachricht später, aus der gewöhnlichen Schleife von
    /// <c>Application.Run</c> heraus statt aus dem WebView2-Rückruf. Für den
    /// Anwender ändert sich nichts (er sieht keinen Unterschied zwischen „jetzt"
    /// und „in einer Nachricht"), für die zweite WebView2 ändert sich die
    /// Ausgangslage.</para>
    ///
    /// <para><b>Der synchrone Rückgabewert bleibt heil</b>, weil er hier gar
    /// nicht anfällt: Die zwei Verteiler, die diese Klasse benutzen
    /// (<c>StartseiteHuelle.Kachelweg</c> und <c>HauptfensterHuelle.Weg</c>),
    /// werten das <c>DialogResult</c> INNERHALB des Sprungs aus und schreiben
    /// ihr Ergebnis in den Projektkontext bzw. in den
    /// <c>SeitenZustand</c> — die Razor-Seite erfährt es über
    /// <c>Auffrischen</c>, nicht über einen Rückgabewert. <c>Weg</c> beantwortet
    /// seine EINE Frage („behandle ich diesen Schlüssel?") weiterhin sofort und
    /// aus der Schlüsseltabelle, nicht aus dem Ausgang des Fensters.</para>
    ///
    /// <para><b>Der Rückweg.</b> Wer den Sprung wieder unmittelbar haben will,
    /// ruft in den zwei Verteilern statt <see cref="Verzoegert"/> den Rumpf
    /// direkt — mehr ist nicht zu ändern. Ob die Verzögerung die leere Fläche
    /// wirklich behebt, sagt die <see cref="WebViewWache"/> am Gerät.</para>
    /// </summary>
    internal static class Blazorsprung
    {
        /// <summary>
        /// Ein Sprung zur Zeit. Zwischen Klick und gepostetem Sprung liegt
        /// zwar nur eine Nachricht, aber die WebView ist in dieser Zeit
        /// bedienbar — ohne den Riegel könnten zwei Kachelklicks zwei modale
        /// Fenster in die Schlange stellen. Der Riegel gilt nur bis zum
        /// <b>Beginn</b> des Sprungs; danach hält ihn das modale Fenster selbst.
        /// </summary>
        /// <remarks>
        /// <para><b>Befund KI‑D‑B‑3</b> (Anwender, 11.09.2026: „der Hilfe-Assistent
        /// lässt sich nicht mehr aus dem Hauptmenü aufrufen, auch mit F1 nicht").
        /// Bis dahin fiel der Riegel erst im <c>finally</c> von
        /// <see cref="Ausfuehren"/>, also am ENDE des Sprungs — der Absatz darüber
        /// versprach den Beginn, der Programmtext hielt das Ende. Für einen
        /// modalen Sprung blieb das folgenlos; seit Auftrag #219 gibt es aber
        /// einen SPRUNG IM SPRUNG: <c>HauptfensterHuelle.Weg</c> verzögert jeden
        /// der 25 Maskenschlüssel (und <c>Masken.KiAssistent</c> ist einer davon),
        /// und <c>WinFormsNavigation</c> verzögert in genau diesem Fall noch
        /// einmal. Der innere Ruf traf den Riegel des äußeren und kehrte bei
        /// <c>if (_angefordert) return;</c> stumm zurück — der Menüpunkt
        /// „KI-Assistent" tat seither nichts.</para>
        /// <para>Seither fällt der Riegel als ERSTES in <see cref="Ausfuehren"/>.
        /// Ein innerer Sprung reiht sich damit regulär ein (er läuft eine
        /// Nachricht später) statt verschluckt zu werden — und er läuft
        /// ausdrücklich NICHT unmittelbar: Der äußere Sprung kann inzwischen in
        /// einer verschachtelten Nachrichtenschleife stehen (<c>ShowDialog</c>),
        /// und dann käme der innere Ruf wieder aus einem WebView2-Rückruf.</para>
        /// </remarks>
        private static bool _angefordert;

        /// <summary>Wann der Riegel gesetzt wurde — für <see cref="RiegelSteht"/>.</summary>
        private static DateTime _riegelSeit;

        /// <summary>
        /// Höchstalter eines Riegels. Eine mit <c>BeginInvoke</c> eingereihte
        /// Nachricht kann VERFALLEN: Wird das Wirtsfenster abgebaut, bevor die
        /// Schleife sie abarbeitet, läuft <see cref="Ausfuehren"/> nie — und ohne
        /// diese Frist bliebe der Riegel für die restliche Sitzung stehen. Menü,
        /// Kacheln und Hilfe-Pillen wären auf einen Schlag stumm, ohne dass
        /// irgendetwas sichtbar fehlschlägt. Fünf Sekunden sind weit jenseits der
        /// einen Nachricht, um die es geht.
        /// </summary>
        private static readonly TimeSpan RIEGELFRIST = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Führt <paramref name="sprung"/> aus, sobald das laufende Ereignis
        /// zu Ende ist.
        /// </summary>
        /// <param name="wirt">Das Fenster, über dessen Nachrichtenschlange der
        /// Sprung läuft. Taugt es nicht, sucht <see cref="Wirtsfenster"/> ein
        /// anderes; ohne jedes Fenster gibt es nichts zu verzögern — dann läuft
        /// der Sprung unmittelbar.</param>
        /// <param name="sprung">Was zu tun ist.</param>
        internal static void Verzoegert(IWin32Window wirt, Action sprung)
        {
            if (sprung == null) return;

            // RUECKFALL AUF DAS HAUPTFENSTER (Befund KI-D-B-3): Die Aufrufer
            // reichen "Form.ActiveForm" bzw. "_besitzer?.Invoke()" herein, und
            // beides kann null sein - etwa, wenn der Klick aus einem gerade
            // schliessenden Fenster kommt. Bis dahin hiess null "dann eben
            // unmittelbar", und genau das ist die Lage, die diese Klasse
            // verhindern soll: eine zweite WebView2 im Rueckruf der ersten.
            Control gereicht = wirt as Control;
            Control fenster = Taugt(gereicht) ? gereicht : Wirtsfenster(null);
            if (fenster == null)
            {
                Protokoll("Kein Wirtsfenster - der Sprung laeuft unmittelbar.");
                Ausfuehren(sprung);
                return;
            }

            if (RiegelSteht()) return;

            _angefordert = true;
            _riegelSeit = DateTime.UtcNow;

            try
            {
                fenster.BeginInvoke(new Action(() => Ausfuehren(sprung)));
            }
            catch (Exception ex)
            {
                // Konnte nicht gepostet werden (Fenster im Abbau): dann eben
                // unmittelbar - schlechter als vorher wird es dadurch nicht.
                _angefordert = false;
                Protokoll("Der verzoegerte Sprung liess sich nicht einreihen: " + ex.Message);
                Ausfuehren(sprung);
            }
        }

        /// <summary>
        /// Das Fenster, über dessen Nachrichtenschlange ein Sprung laufen kann:
        /// das <paramref name="bevorzugt"/>e, sonst das aktive, sonst das
        /// HAUPTFENSTER, sonst das erste offene mit Handle.
        /// </summary>
        /// <remarks>
        /// Derselbe Gedanke wie <c>Blazornachlauf.Wirt</c>, nur mit einem
        /// ausdrücklichen Rückfall auf den <c>Hauptfensterrahmen</c>: Er trägt
        /// <c>Application.Run</c> und ist damit das einzige Fenster, dessen
        /// Schlange verlässlich abgearbeitet wird. <c>null</c> heißt „es gibt
        /// keins" — dann gibt es auch nichts zu verzögern.
        /// </remarks>
        internal static Form Wirtsfenster(IWin32Window bevorzugt)
        {
            try
            {
                if (bevorzugt is Form gewuenscht && Taugt(gewuenscht)) return gewuenscht;

                Form aktiv = Form.ActiveForm;
                if (Taugt(aktiv)) return aktiv;

                if (Taugt(Program.rahmen)) return Program.rahmen;

                foreach (Form offen in Application.OpenForms)
                    if (Taugt(offen)) return offen;
            }
            catch (Exception ex)
            {
                Protokoll("Kein Wirtsfenster zu ermitteln: " + ex.Message);
            }

            return null;
        }

        private static bool Taugt(Control fenster)
            => fenster != null && !fenster.IsDisposed && fenster.IsHandleCreated;

        /// <summary>
        /// Steht ein Riegel, der diesen Sprung abweist? Ein VERWAISTER Riegel
        /// (die gepostete Nachricht wurde nie abgearbeitet) fällt dabei.
        /// </summary>
        /// <remarks>
        /// <b>Kein stummes Abweisen mehr</b> (Befund KI‑D‑B‑3): Wer hier
        /// verworfen wird, steht wenigstens im Protokoll. Bis dahin war die
        /// einzige Spur eines geschluckten Sprungs ein Menüpunkt, der nichts tat.
        /// </remarks>
        private static bool RiegelSteht()
        {
            if (!_angefordert) return false;

            TimeSpan alter = DateTime.UtcNow - _riegelSeit;
            if (alter < RIEGELFRIST)
            {
                Protokoll("Ein Sprung ist bereits eingereiht (vor " +
                          (int)alter.TotalMilliseconds + " ms) - dieser wird verworfen.");
                return true;
            }

            Protokoll("Verwaister Riegel nach " + (int)alter.TotalSeconds +
                      " s zurueckgesetzt - die gepostete Nachricht kam nie an.");
            _angefordert = false;
            return false;
        }

        private static void Ausfuehren(Action sprung)
        {
            // DER RIEGEL FAELLT ZUERST (Befund KI-D-B-3) und nicht im finally:
            // Ab hier laeuft der Sprung aus der gewoehnlichen Nachrichtenschleife
            // heraus und nicht mehr aus dem WebView2-Rueckruf. Ein Sprung, der
            // WAEHREND dieses Sprungs angefordert wird, ist deshalb kein zweiter
            // Klick, den es abzuwehren gaelte, sondern dessen Fortsetzung - er
            // darf sich regulaer einreihen. Die Bedienung haelt danach das modale
            // Fenster selbst an, genau wie der Klassenkopf es seit W16b sagt.
            _angefordert = false;

            try
            {
                sprung();
            }
            catch (Exception ex)
            {
                // Bis W16b lief der Sprung im Blazor-Ereignis; eine Ausnahme
                // riss dort die Anwendung mit. Aus einer geposteten Nachricht
                // heraus taete sie dasselbe - deshalb dieselbe Behandlung wie
                // in Sprungbruecke.Zeigen: melden, nicht mitreissen.
                Protokoll("Sprung gescheitert: " + ex);
                try { Dienste.Dialog.Meldung(ex.Message); } catch { }
            }
        }

        private static void Protokoll(string satz)
        {
            try
            {
                Debug.WriteLine("[Blazorsprung] " + satz);
                Trace.WriteLine("[Blazorsprung] " + satz);
            }
            catch { }
        }
    }
}
