using System;
using System.Diagnostics;
using System.IO;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Startet QUICK ASSIST, das Hilfeprogramm von Windows — den Standardweg der Fernwartung
    /// (Konzept Administration und Support, Abschnitt 4). Steht in der Schale, nicht im Kern,
    /// weil beide Startwege an Windows gebunden sind.
    ///
    /// <para><b>Zwei Wege in fester Folge.</b> Zuerst die Store-App über ihren Shell-Namen
    /// (<see cref="STORE_APP"/>, Shell-Ausführung); misslingt das, die klassische
    /// <see cref="KLASSISCH"/> unter <c>%WINDIR%\System32</c>, wenn die Datei liegt. Ob die App
    /// installiert ist, wird nicht vorab (etwa in der Registry) geprüft: Der Start selbst ist die
    /// Probe.</para>
    ///
    /// <para><b>Ohne Fehlerfenster der Shell</b> (<c>ErrorDialog = false</c>): Der Aufruf kommt aus
    /// einem Rückruf der WebView2; ein modales Fenster der Shell darf ihn nicht anhalten. Den
    /// Misserfolg meldet der Dialog selbst.</para>
    /// </summary>
    internal static class QuickAssistStart
    {
        /// <summary>Der Shell-Name der Store-App Quick Assist.</summary>
        internal const string STORE_APP = @"shell:AppsFolder\MicrosoftCorporationII.QuickAssist_8wekyb3d8bbwe!App";

        /// <summary>Die klassische Quick-Assist-Datei, relativ zu <c>%WINDIR%</c>.</summary>
        internal const string KLASSISCH = @"%WINDIR%\System32\quickassist.exe";

        /// <summary>Startet Quick Assist; <c>false</c>, wenn keiner der beiden Wege trägt.</summary>
        internal static bool Starten()
        {
            if (Versuche(STORE_APP)) return true;
            string klassisch = Environment.ExpandEnvironmentVariables(KLASSISCH);
            return File.Exists(klassisch) && Versuche(klassisch);
        }

        /// <summary>
        /// Ein Startversuch über die Shell. <see cref="Process.Start(ProcessStartInfo)"/> darf
        /// <c>null</c> liefern (die Shell reicht an einen laufenden Prozess weiter) — das zählt als
        /// gestartet; nur eine Ausnahme heißt „misslungen".
        /// </summary>
        private static bool Versuche(string ziel)
        {
            try
            {
                using (Process.Start(new ProcessStartInfo(ziel) { UseShellExecute = true, ErrorDialog = false }))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
