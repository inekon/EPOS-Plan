using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung der Naht <see cref="Berichtsvorlagenwege"/> (Etappe BV-E1, Konzept
    /// Berichtsvorlagen 10.3): „Im Ordner zeigen" über den Explorer, „In Word öffnen" und
    /// „Schreibgeschützt öffnen" über Word selbst. <c>Program.Main</c> hängt sie ein.
    ///
    /// <para><b>Word, nicht „das Programm für .docx".</b> Eine <c>.dotx</c> öffnet die Shell mit dem
    /// Verb „Neu" — heraus käme ein neues Dokument AUS der Vorlage, und was der Anwender darin ändert,
    /// landet nie in der Vorlage. Word bekommt deshalb den Pfad als Argument: So öffnet es die Datei
    /// selbst, gleich welcher Endung. Gefunden wird Word über seinen Eintrag unter
    /// <c>App Paths</c>; fehlt er, liefert der Weg <c>false</c>, und die Hülle sagt es („Word wurde
    /// auf diesem Rechner nicht gefunden").</para>
    ///
    /// <para><b>Schreibgeschützt ist eine Kopie.</b> Einen Schalter „nur lesen" kennt die
    /// Befehlszeile von Word nicht verlässlich; die mitgelieferte Vorlage wird deshalb in den
    /// Temp-Ordner kopiert, dort schreibgeschützt markiert und geöffnet. Word zeigt sie als
    /// schreibgeschützt, und das Original im Programmordner bleibt unberührt.</para>
    /// </summary>
    internal static class WindowsBerichtsvorlagenwege
    {
        /// <summary>Der Registereintrag, unter dem Word seinen Programmpfad hinterlegt.</summary>
        private const string APP_PATHS_WORD = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Winword.exe";

        /// <summary>Die Wege dieser Schale.</summary>
        internal static Berichtsvorlagenwege Erzeugen()
        {
            return new Berichtsvorlagenwege
            {
                ImOrdnerZeigen = ImOrdnerZeigen,
                InWordOeffnen = InWordOeffnen,
                SchreibgeschuetztOeffnen = SchreibgeschuetztOeffnen,
                OrdnerWaehlbar = true
            };
        }

        /// <summary>
        /// Der Explorer mit der Datei markiert (<c>/select,</c>); fehlt die Datei, der Ordner selbst.
        /// </summary>
        internal static bool ImOrdnerZeigen(string pfad)
        {
            if (string.IsNullOrWhiteSpace(pfad)) return false;
            try
            {
                if (File.Exists(pfad))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + pfad + "\"")
                    {
                        UseShellExecute = true
                    });
                    return true;
                }

                string ordner = Directory.Exists(pfad) ? pfad : Path.GetDirectoryName(pfad);
                if (string.IsNullOrEmpty(ordner) || !Directory.Exists(ordner)) return false;
                Process.Start(new ProcessStartInfo(ordner) { UseShellExecute = true });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Öffnet die Vorlage selbst in Word; <c>false</c>, wenn Word fehlt oder nicht startet.</summary>
        internal static bool InWordOeffnen(string pfad)
        {
            if (string.IsNullOrWhiteSpace(pfad) || !File.Exists(pfad)) return false;
            string word = WordPfad();
            return word != null && Starte(word, pfad);
        }

        /// <summary>
        /// Öffnet eine schreibgeschützte Kopie in Word; <c>false</c>, wenn Word fehlt, die Kopie nicht
        /// entsteht oder Word nicht startet.
        /// </summary>
        internal static bool SchreibgeschuetztOeffnen(string pfad)
        {
            if (string.IsNullOrWhiteSpace(pfad) || !File.Exists(pfad)) return false;
            string word = WordPfad();
            if (word == null) return false;
            string kopie = Lesekopie(pfad);
            return kopie != null && Starte(word, kopie);
        }

        /// <summary>
        /// Der Programmpfad von Word aus <c>App Paths</c> — beide Registeransichten der Maschine,
        /// dann der Anwender; <c>null</c>, wenn keiner auf eine vorhandene Datei zeigt.
        /// </summary>
        internal static string WordPfad()
        {
            foreach (RegistryHive wurzel in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
                foreach (RegistryView ansicht in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    try
                    {
                        using (RegistryKey basis = RegistryKey.OpenBaseKey(wurzel, ansicht))
                        using (RegistryKey schluessel = basis.OpenSubKey(APP_PATHS_WORD))
                        {
                            string wert = schluessel?.GetValue(null) as string;
                            if (string.IsNullOrWhiteSpace(wert)) continue;
                            string pfad = Environment.ExpandEnvironmentVariables(wert.Trim().Trim('"'));
                            if (File.Exists(pfad)) return pfad;
                        }
                    }
                    catch (Exception)
                    {
                        // Ein nicht lesbarer Zweig ist kein Grund, die übrigen nicht zu fragen.
                    }
                }
            return null;
        }

        /// <summary>Startet Word mit der Datei als einzigem Argument.</summary>
        private static bool Starte(string word, string datei)
        {
            try
            {
                var start = new ProcessStartInfo(word) { UseShellExecute = false };
                start.ArgumentList.Add(datei);
                return Process.Start(start) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Die schreibgeschützte Kopie unter <c>%TEMP%\EPOS-Plan\Vorlagen (nur lesen)</c>. Eine ältere
        /// Kopie desselben Namens wird ersetzt; hält Word sie noch offen, bekommt die neue einen
        /// freien Namen mit „ (2)" usw. <c>null</c>, wenn keine Kopie entsteht.
        /// </summary>
        private static string Lesekopie(string pfad)
        {
            try
            {
                string ordner = Path.Combine(Path.GetTempPath(), "EPOS-Plan", "Vorlagen (nur lesen)");
                Directory.CreateDirectory(ordner);
                string stamm = Path.GetFileNameWithoutExtension(pfad);
                string endung = Path.GetExtension(pfad);
                for (int n = 1; n < 100; n++)
                {
                    string ziel = Path.Combine(ordner, n == 1 ? stamm + endung : stamm + " (" + n + ")" + endung);
                    if (File.Exists(ziel) && !Wegraeumen(ziel)) continue;
                    File.Copy(pfad, ziel, false);
                    File.SetAttributes(ziel, File.GetAttributes(ziel) | FileAttributes.ReadOnly);
                    return ziel;
                }
            }
            catch (Exception)
            {
                // benannt über den Rückgabewert
            }
            return null;
        }

        /// <summary>Löscht eine frühere Lesekopie; <c>false</c>, wenn Word sie noch offen hält.</summary>
        private static bool Wegraeumen(string datei)
        {
            try
            {
                File.SetAttributes(datei, FileAttributes.Normal);
                File.Delete(datei);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
