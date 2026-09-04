using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="IDateiDienst"/> — die gewohnten
    /// Systemdialoge und der Start mit der Standardanwendung.
    ///
    /// <para><c>RestoreDirectory = true</c> steht an beiden Dateidialogen, weil beide
    /// Fundstellen des Bestands es setzen: Ohne das merkt sich der Prozess den zuletzt
    /// benutzten Ordner und ignoriert den vorgeschlagenen Startordner beim nächsten
    /// Aufruf.</para>
    /// </summary>
    public sealed class WindowsDateiDienst : IDateiDienst
    {
        /// <inheritdoc/>
        public string DateiOeffnen(string titel, string filter, string startOrdner)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                if (!string.IsNullOrEmpty(titel)) dlg.Title = titel;
                if (!string.IsNullOrEmpty(filter)) { dlg.Filter = filter; dlg.FilterIndex = 1; }
                if (!string.IsNullOrEmpty(startOrdner)) dlg.InitialDirectory = startOrdner;
                dlg.RestoreDirectory = true;

                return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : "";
            }
        }

        /// <inheritdoc/>
        public string DateiSpeichern(string titel, string filter, string vorschlag)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                if (!string.IsNullOrEmpty(titel)) dlg.Title = titel;
                if (!string.IsNullOrEmpty(filter)) { dlg.Filter = filter; dlg.FilterIndex = 1; }
                dlg.RestoreDirectory = true;

                // Der Ordner steht MIT im Dateinamen. InitialDirectory allein wird von
                // Windows ignoriert, sobald sich das System für die Anwendung bereits
                // einen zuletzt benutzten Ordner gemerkt hat - der Hinweis stammt aus
                // dem CSV-Export und gilt für jeden Speichern-Dialog.
                if (!string.IsNullOrEmpty(vorschlag))
                {
                    dlg.FileName = vorschlag;
                    try
                    {
                        string ordner = System.IO.Path.GetDirectoryName(vorschlag);
                        if (!string.IsNullOrEmpty(ordner)) dlg.InitialDirectory = ordner;
                    }
                    catch { }
                }

                return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : "";
            }
        }

        /// <inheritdoc/>
        public string OrdnerWaehlen(string titel, string startOrdner)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(titel)) dlg.Description = titel;
                if (!string.IsNullOrEmpty(startOrdner)) dlg.SelectedPath = startOrdner;

                return dlg.ShowDialog() == DialogResult.OK ? dlg.SelectedPath : "";
            }
        }

        /// <inheritdoc/>
        public bool MitSystemOeffnen(string pfad)
        {
            if (string.IsNullOrEmpty(pfad) || !System.IO.File.Exists(pfad)) return false;

            try
            {
                // UseShellExecute = true: erst damit sucht Windows die zur Endung
                // hinterlegte Anwendung. Ohne das versucht .NET die Datei selbst als
                // Programm zu starten.
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pfad)
                {
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Öffnen der Datei: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Öffnet eine Adresse im Standardbrowser (iU9-W16c.3).
        ///
        /// <para>Wörtlich der Rumpf von <c>Hauptfensterrahmen.MenuItem_Dokumentation_Click</c>
        /// (<c>:826</c>) — bis dahin die letzte unmittelbare
        /// <c>Process.Start</c>-Zeile des Hauptfensters. Ein Fehlschlag bleibt
        /// folgenlos: Der Vorläufer schrieb ihn nach <c>Debug.WriteLine</c> und
        /// meldete nichts.</para>
        /// </summary>
        public bool AdresseOeffnen(string adresse)
        {
            if (string.IsNullOrWhiteSpace(adresse)) return false;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = adresse,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Fehler beim Öffnen des Links: " + ex.Message);
                return false;
            }
        }
    }
}
