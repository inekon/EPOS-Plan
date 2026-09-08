using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="IDateiDienst"/> — die gewohnten
    /// Systemdialoge und der Start mit der Standardanwendung.
    ///
    /// <para><b>Der zuletzt benutzte Ordner wird GEMERKT</b> (Windows-Abnahme 08.09.2026,
    /// Befund W13‑B‑2: „der zuvor ausgewählte Pfad wird nicht gemerkt"). Bis dahin stand
    /// <c>RestoreDirectory = true</c> allein da: Der Prozess vergaß den Ordner, und jeder
    /// Aufruf begann wieder beim vorgeschlagenen Startordner — beim Katalogimport dem
    /// Herstellerdatenpfad. Jetzt merkt sich der Dienst je DATEIART (die erste Endung des
    /// Filters, z. B. <c>ond</c>, <c>pan</c>, <c>csv</c>) den Ordner der zuletzt gewählten
    /// Datei in den Einstellungen (<see cref="Dienste.Einstellungen"/>, unter Windows
    /// <c>HKCU\Software\wp-plan</c>, Wert <c>DateiOrdner.&lt;endung&gt;</c>). Gibt es einen
    /// gemerkten Ordner und existiert er noch, gewinnt er; sonst gilt der vorgeschlagene
    /// Startordner. <c>RestoreDirectory</c> bleibt gesetzt, damit das Arbeitsverzeichnis
    /// des Prozesses unberührt bleibt.</para>
    ///
    /// <para><b>Mehrere Dateien auf einmal</b> (W13‑B‑3): <see cref="DateienOeffnen"/> ist
    /// derselbe Dialog mit <c>Multiselect</c>; gemerkt wird der Ordner der ersten Datei.</para>
    /// </summary>
    public sealed class WindowsDateiDienst : IDateiDienst
    {
        private const string ORDNER_SCHLUESSEL = "DateiOrdner.";
        private static readonly Regex ERSTE_ENDUNG =
            new Regex(@"\*\.([A-Za-z0-9]+)", RegexOptions.Compiled);

        public string DateiOeffnen(string titel, string filter, string startOrdner)
        {
            using (OpenFileDialog dlg = Oeffner(titel, filter, startOrdner, false))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return "";
                OrdnerMerken(filter, dlg.FileName);
                return dlg.FileName;
            }
        }

        public string[] DateienOeffnen(string titel, string filter, string startOrdner)
        {
            using (OpenFileDialog dlg = Oeffner(titel, filter, startOrdner, true))
            {
                if (dlg.ShowDialog() != DialogResult.OK) return Array.Empty<string>();
                string[] pfade = dlg.FileNames ?? Array.Empty<string>();
                if (pfade.Length > 0) OrdnerMerken(filter, pfade[0]);
                return pfade;
            }
        }

        private static OpenFileDialog Oeffner(string titel, string filter, string startOrdner,
                                              bool mehrere)
        {
            var dlg = new OpenFileDialog();
            if (!string.IsNullOrEmpty(titel)) dlg.Title = titel;
            if (!string.IsNullOrEmpty(filter)) { dlg.Filter = filter; dlg.FilterIndex = 1; }
            string ordner = Startordner(filter, startOrdner);
            if (!string.IsNullOrEmpty(ordner)) dlg.InitialDirectory = ordner;
            dlg.RestoreDirectory = true;
            dlg.Multiselect = mehrere;
            return dlg;
        }

        public string DateiSpeichern(string titel, string filter, string vorschlag)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                if (!string.IsNullOrEmpty(titel)) dlg.Title = titel;
                if (!string.IsNullOrEmpty(filter)) { dlg.Filter = filter; dlg.FilterIndex = 1; }
                dlg.RestoreDirectory = true;

                // Der Ordner steht MIT im Dateinamen. InitialDirectory allein wird von
                // Windows ignoriert, sobald FileName einen vollen Pfad traegt.
                string vorschlagsordner = "";
                if (!string.IsNullOrEmpty(vorschlag))
                {
                    dlg.FileName = vorschlag;
                    try { vorschlagsordner = System.IO.Path.GetDirectoryName(vorschlag) ?? ""; }
                    catch { }
                }

                // Ein Vorschlag MIT Ordner gilt (der Aufrufer weiss, wohin); ohne Ordner
                // der gemerkte - wie beim Oeffnen.
                string ordner = vorschlagsordner.Length > 0 ? vorschlagsordner : Startordner(filter, "");
                if (!string.IsNullOrEmpty(ordner)) dlg.InitialDirectory = ordner;

                if (dlg.ShowDialog() != DialogResult.OK) return "";
                OrdnerMerken(filter, dlg.FileName);
                return dlg.FileName;
            }
        }

        public string OrdnerWaehlen(string titel, string startOrdner)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(titel)) dlg.Description = titel;
                if (!string.IsNullOrEmpty(startOrdner)) dlg.SelectedPath = startOrdner;
                return dlg.ShowDialog() == DialogResult.OK ? dlg.SelectedPath : "";
            }
        }

        public bool MitSystemOeffnen(string pfad)
        {
            if (string.IsNullOrEmpty(pfad) || !System.IO.File.Exists(pfad)) return false;
            try
            {
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

        // ------------------------------------------------------------------
        //  Das Ordnergedaechtnis (W13-B-2)
        // ------------------------------------------------------------------

        /// <summary>Der Einstellungsschluessel zur Dateiart: die erste Endung des Filters, sonst "alle".</summary>
        internal static string Ordnerschluessel(string filter)
        {
            Match m = string.IsNullOrEmpty(filter) ? Match.Empty : ERSTE_ENDUNG.Match(filter);
            string endung = m.Success ? m.Groups[1].Value.ToLowerInvariant() : "alle";
            return ORDNER_SCHLUESSEL + endung;
        }

        /// <summary>Der gemerkte Ordner der Dateiart, wenn es ihn noch gibt - sonst der Vorschlag.</summary>
        private static string Startordner(string filter, string vorschlag)
        {
            try
            {
                string gemerkt = Dienste.Einstellungen.Lies(Ordnerschluessel(filter), "");
                if (!string.IsNullOrEmpty(gemerkt) && System.IO.Directory.Exists(gemerkt)) return gemerkt;
            }
            catch { }
            return vorschlag ?? "";
        }

        private static void OrdnerMerken(string filter, string pfad)
        {
            try
            {
                string ordner = System.IO.Path.GetDirectoryName(pfad);
                if (!string.IsNullOrEmpty(ordner))
                    Dienste.Einstellungen.Schreib(Ordnerschluessel(filter), ordner);
            }
            catch { }
        }

        // ------------------------------------------------------------------
        //  Die asynchronen Zwillinge (Befund W13-B-1): das Fenster faehrt HINTER
        //  dem Blazor-Ereignis hoch, eine gepostete Nachricht spaeter.
        //
        //  Der Weg im Bestand:
        //           Blazor-Ereignis (WebMessageReceived-Rueckruf der WebView2)
        //           -> Huelle: await Dienste.Datei.DateiOeffnenAsync(...)
        //           -> Blazornachlauf.Nachgelagert: BeginInvoke auf den UI-Faden
        //           -> OpenFileDialog.ShowDialog()
        // ------------------------------------------------------------------

        public System.Threading.Tasks.Task<string> DateiOeffnenAsync(
            string titel, string filter, string startOrdner)
            => Blazornachlauf.Nachgelagert(() => DateiOeffnen(titel, filter, startOrdner));

        public System.Threading.Tasks.Task<string[]> DateienOeffnenAsync(
            string titel, string filter, string startOrdner)
            => Blazornachlauf.Nachgelagert(() => DateienOeffnen(titel, filter, startOrdner));

        public System.Threading.Tasks.Task<string> DateiSpeichernAsync(
            string titel, string filter, string vorschlag)
            => Blazornachlauf.Nachgelagert(() => DateiSpeichern(titel, filter, vorschlag));

        public System.Threading.Tasks.Task<string> OrdnerWaehlenAsync(
            string titel, string startOrdner)
            => Blazornachlauf.Nachgelagert(() => OrdnerWaehlen(titel, startOrdner));
    }
}
