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
        private static bool _angefordert;

        /// <summary>
        /// Führt <paramref name="sprung"/> aus, sobald das laufende Ereignis
        /// zu Ende ist.
        /// </summary>
        /// <param name="wirt">Das Fenster, über dessen Nachrichtenschlange der
        /// Sprung läuft. Ohne Fenster (oder ohne Fensterhandle) gibt es nichts
        /// zu verzögern — dann läuft er unmittelbar.</param>
        /// <param name="sprung">Was zu tun ist.</param>
        internal static void Verzoegert(IWin32Window wirt, Action sprung)
        {
            if (sprung == null) return;

            Control fenster = wirt as Control;
            if (fenster == null || fenster.IsDisposed || !fenster.IsHandleCreated)
            {
                Ausfuehren(sprung);
                return;
            }

            if (_angefordert) return;
            _angefordert = true;

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

        private static void Ausfuehren(Action sprung)
        {
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
            finally
            {
                _angefordert = false;
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
