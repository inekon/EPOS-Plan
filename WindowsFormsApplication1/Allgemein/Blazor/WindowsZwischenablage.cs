using System;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dienste;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Windows-Adapter von <see cref="IZwischenablage"/> (Konzept Berichtsvorlagen 9.6, BV-E6): die
    /// Zwischenablage der Schale über <see cref="System.Windows.Forms.Clipboard"/> — derselbe Weg, den
    /// der Hilfe-Assistent beim Kopieren des Verlaufs nimmt (<c>KiChatHuelle.InZwischenablage</c>).
    ///
    /// <para><b>Faden.</b> Die Zwischenablage von Windows verlangt einen STA-Faden. Der Verteiler der
    /// <c>BlazorWebView</c> läuft auf dem Oberflächenfaden (STA) — dort wird unmittelbar geschrieben;
    /// kommt der Aufruf von anderswo, schreibt ein eigener kurzer STA-Faden.</para>
    ///
    /// <para><b>Scheitern ist benannt:</b> Hält ein anderes Programm die Zwischenablage, liefert der
    /// Adapter <c>false</c>, und die Marke zeigt den Text markiert.</para>
    /// </summary>
    internal sealed class WindowsZwischenablage : IZwischenablage
    {
        /// <inheritdoc />
        public Task<bool> TextSetzenAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return Task.FromResult(false);

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                return Task.FromResult(Schreiben(text));

            var fertig = new TaskCompletionSource<bool>();
            var faden = new Thread(() => fertig.TrySetResult(Schreiben(text))) { IsBackground = true };
            faden.SetApartmentState(ApartmentState.STA);
            faden.Start();
            return fertig.Task;
        }

        private static bool Schreiben(string text)
        {
            try
            {
                System.Windows.Forms.Clipboard.SetText(text);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Platzhalter] Kopieren: " + ex.Message);
                return false;
            }
        }
    }
}
