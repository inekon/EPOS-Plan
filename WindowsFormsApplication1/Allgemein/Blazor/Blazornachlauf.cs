using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der NACHLAUF für ein modales Systemfenster aus einem Blazor-Ereignis —
    /// Befund <b>W13‑B‑1</b> der Windows-Abnahme vom 05.09.2026
    /// („VDI-3805-Datei-Import: Absturz bei Datei laden, teilweise Absturz auch
    /// bei Dateiauswahl-Dialog").
    ///
    /// <para><b>Das Verhältnis zu <see cref="Blazorsprung"/>.</b> Beide lösen
    /// dieselbe Lage — etwas Modales soll aus dem
    /// <c>WebMessageReceived</c>-Rückruf einer WebView2 heraus aufgehen —, aber
    /// an zwei verschiedenen Stellen und mit zwei verschiedenen Verpflichtungen:</para>
    ///
    /// <list type="bullet">
    ///   <item><description><see cref="Blazorsprung"/> steht an den zwei
    ///   VERTEILERN (Kachelweg, Menüweg). Er liefert nichts zurück; der Aufrufer
    ///   erfährt das Ergebnis über den Projektkontext bzw. den
    ///   <c>SeitenZustand</c>. Deshalb genügt dort ein <c>BeginInvoke</c> und ein
    ///   Riegel gegen zwei Sprünge zugleich.</description></item>
    ///   <item><description>Diese Klasse steht in einem DIENST
    ///   (<c>WindowsDateiDienst</c>, <c>WindowsDialogDienst</c>). Ein Dateiwähler
    ///   MUSS einen Pfad zurückgeben — das Ereignis kann also nicht einfach
    ///   weiterlaufen und die Antwort vergessen. Der Nachlauf liefert deshalb
    ///   einen <see cref="Task{TResult}"/>: Die Razor-Komponente <c>await</c>et
    ///   ihn, Blazor schließt sein Ereignis ab, der WebView2-Rückruf kehrt zurück
    ///   — und ERST DANN, eine gepostete Nachricht später, geht das Fenster
    ///   auf.</description></item>
    /// </list>
    ///
    /// <para><b>Kein Riegel.</b> Anders als beim Sprung darf hier mehreres
    /// nebeneinander warten: Zwei Dateiwähler zugleich gibt es ohnehin nicht (das
    /// erste modale Fenster sperrt die WebView darunter), und ein Riegel würde
    /// einen Aufrufer mit einem leeren Ergebnis abspeisen, obwohl er auf eine
    /// Antwort wartet.</para>
    ///
    /// <para><b>Wenn es nichts zu verzögern gibt</b> — kein Fenster, kein
    /// Fensterhandle, ein Fehlschlag beim Einreihen —, läuft die Arbeit
    /// unmittelbar. Schlechter als vorher wird es dadurch nicht; das war der
    /// Zustand bis zu diesem Befund.</para>
    ///
    /// <para><b>Was der Aufrufer nicht tun darf:</b> auf den Rückgabewert
    /// BLOCKIEREND warten (<c>.Result</c>, <c>.Wait()</c>, <c>GetAwaiter().GetResult()</c>).
    /// Der Nachlauf braucht die Nachrichtenschleife des Bedienfadens, um zum Zug
    /// zu kommen — wer sie anhält, wartet auf sich selbst. Deshalb tragen die
    /// wartbaren Zwillinge in <c>IDateiDienst</c>/<c>IDialogDienst</c> das
    /// <c>Async</c> im Namen und werden nur <c>await</c>et.</para>
    /// </summary>
    internal static class Blazornachlauf
    {
        /// <summary>
        /// Führt <paramref name="arbeit"/> aus, sobald das laufende Ereignis zu
        /// Ende ist, und liefert ihr Ergebnis.
        /// </summary>
        /// <typeparam name="T">Was die Arbeit liefert (ein Pfad, eine Antwort).</typeparam>
        /// <param name="arbeit">Der Aufruf, der ein modales Fenster hochfährt.</param>
        internal static Task<T> Nachgelagert<T>(Func<T> arbeit)
        {
            if (arbeit == null) return Task.FromResult(default(T));

            Control wirt = Wirt();
            if (wirt == null) return Task.FromResult(arbeit());

            var quelle = new TaskCompletionSource<T>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                wirt.BeginInvoke(new Action(() =>
                {
                    try { quelle.TrySetResult(arbeit()); }
                    catch (Exception ex) { quelle.TrySetException(ex); }
                }));
            }
            catch (Exception ex)
            {
                // Konnte nicht gepostet werden (Fenster im Abbau): dann eben
                // unmittelbar - dieselbe Weiche wie in Blazorsprung.Verzoegert.
                Protokoll("Der nachgelagerte Aufruf liess sich nicht einreihen: " + ex.Message);
                return Task.FromResult(arbeit());
            }

            return quelle.Task;
        }

        /// <summary>
        /// Dieselbe Verzögerung für eine Arbeit ohne Ergebnis.
        /// </summary>
        internal static Task Nachgelagert(Action arbeit)
        {
            if (arbeit == null) return Task.CompletedTask;
            return Nachgelagert(() => { arbeit(); return true; });
        }

        /// <summary>
        /// Das Fenster, über dessen Nachrichtenschlange der Nachlauf läuft:
        /// das AKTIVE, sonst das erste offene mit einem Handle.
        ///
        /// <para>Das aktive ist in der Regel das modale Blazor-Fenster, aus dem
        /// der Aufruf kommt — das ist auch der richtige Besitzer für den
        /// Dateiwähler. Ohne aktives Fenster (der besitzerlose Lauf aus
        /// <c>Program.Main</c>, W15c.6) taugt jedes offene; ohne jedes Fenster
        /// gibt es nichts zu verzögern.</para>
        /// </summary>
        private static Control Wirt()
        {
            try
            {
                Form aktiv = Form.ActiveForm;
                if (Taugt(aktiv)) return aktiv;

                foreach (Form offen in Application.OpenForms)
                    if (Taugt(offen)) return offen;
            }
            catch (Exception ex)
            {
                Protokoll("Kein Wirt fuer den Nachlauf gefunden: " + ex.Message);
            }

            return null;
        }

        private static bool Taugt(Form fenster)
            => fenster != null && !fenster.IsDisposed && fenster.IsHandleCreated;

        private static void Protokoll(string satz)
        {
            try
            {
                Debug.WriteLine("[Blazornachlauf] " + satz);
                Trace.WriteLine("[Blazornachlauf] " + satz);
            }
            catch { /* ein Protokoll darf nie der Grund eines Fehlers sein */ }
        }
    }
}
