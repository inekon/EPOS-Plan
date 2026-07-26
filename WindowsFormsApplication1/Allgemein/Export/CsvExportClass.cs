using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Wertspalte für den CSV-Export (Spaltenüberschrift inkl. Einheit + Ganglinie).
    /// Die Ganglinie kann Stundenwerte (8760) oder Viertelstundenwerte (35040) enthalten,
    /// die Umrechnung auf das Ziel-Zeitraster übernimmt CsvExportClass.
    /// </summary>
    public class CsvSpalte
    {
        public string Name;
        public float[] Werte;

        public CsvSpalte(string name, float[] werte)
        {
            Name = name;
            Werte = werte;
        }
    }

    /// <summary>
    /// Generischer CSV-Export für Simulations-Ganglinien.
    ///
    /// Aufbau der Datei:
    ///   Zeitstempel;Außentemperatur [°C];Spalte1;Spalte2;...
    ///
    /// - Trennzeichen: Semikolon, Dezimaltrennzeichen: Komma (de-DE) -> direkt in
    ///   deutschem Excel zu öffnen.
    /// - Zeitraster wahlweise Stundenwerte (8760 Zeilen) oder Viertelstundenwerte
    ///   (35040 Zeilen). Liegt eine Spalte im jeweils anderen Raster vor, wird sie
    ///   automatisch umgerechnet (Viertelstunden -> Stundenmittel bzw. Stundenwert
    ///   je Viertelstunde wiederholt).
    /// - Zeitstempel: 01.01. 00:00 bis 31.12. 23:00 eines Nicht-Schaltjahres
    ///   (Simulationsjahr mit 8760 Stunden).
    /// </summary>
    public static class CsvExportClass
    {
        private const int STUNDEN_JAHR = 8760;

        // Zuletzt verwendeter Export-Ordner wird in der Registry gemerkt
        // (gleicher Schlüssel wie die Sprach-Einstellung der Anwendung).
        private const string REG_SCHLUESSEL = @"Software\wp-plan";
        private const string REG_WERT = "CsvExportPfad";

        /// <summary>
        /// Liest den zuletzt verwendeten Export-Ordner aus der Registry (HKCU\Software\wp-plan).
        /// </summary>
        private static string LetztenPfadLesen()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REG_SCHLUESSEL))
                {
                    return key != null ? key.GetValue(REG_WERT) as string : null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Merkt sich den Ordner der gespeicherten Datei für den nächsten Export.
        /// </summary>
        private static void PfadMerken(string dateiname)
        {
            try
            {
                string ordner = Path.GetDirectoryName(dateiname);
                if (string.IsNullOrEmpty(ordner)) return;

                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(REG_SCHLUESSEL))
                {
                    if (key != null) key.SetValue(REG_WERT, ordner);
                }
            }
            catch
            {
                // Pfad merken ist optional - Fehler hier nicht an den Benutzer melden
            }
        }

        /// <summary>
        /// Zeigt einen Speichern-Dialog und schreibt die CSV-Datei.
        /// </summary>
        /// <param name="vorschlagDateiname">Vorbelegter Dateiname im Dialog</param>
        /// <param name="temperaturStuendlich">Außentemperatur als Stundenwerte (8760), darf null sein</param>
        /// <param name="spalten">Wertspalten (mindestens eine)</param>
        /// <param name="viertelstundenwerte">true = 35040 Zeilen (15-min-Raster), false = 8760 Zeilen (Stundenraster)</param>
        public static void Export(string vorschlagDateiname, float[] temperaturStuendlich, List<CsvSpalte> spalten, bool viertelstundenwerte = false)
        {
            if (spalten == null || spalten.Count == 0)
            {
                MessageBox.Show("Keine Datenreihe für den Export ausgewählt!", "CSV Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Zuletzt verwendeten Export-Ordner vorschlagen, sonst "Dokumente"
            string startOrdner = LetztenPfadLesen();
            if (string.IsNullOrEmpty(startOrdner) || !Directory.Exists(startOrdner))
                startOrdner = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "CSV Export";
            dlg.Filter = "CSV Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;
            dlg.InitialDirectory = startOrdner;
            // WICHTIG: Ordner direkt im Dateinamen mitgeben - InitialDirectory alleine
            // wird von Windows ignoriert, sobald sich das System für die Anwendung
            // bereits einen zuletzt verwendeten Ordner gemerkt hat.
            dlg.FileName = Path.Combine(startOrdner, vorschlagDateiname);

            if (dlg.ShowDialog() != DialogResult.OK) return;

            // Ordner für den nächsten Export merken
            PfadMerken(dlg.FileName);

            try
            {
                Schreiben(dlg.FileName, temperaturStuendlich, spalten, viertelstundenwerte);
                MessageBox.Show("CSV-Datei wurde erstellt:\n" + dlg.FileName, "CSV Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Schreiben der CSV-Datei:\n" + ex.Message, "CSV Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void Schreiben(string dateiname, float[] temperaturStuendlich, List<CsvSpalte> spalten, bool viertelstundenwerte)
        {
            CultureInfo kultur = new CultureInfo("de-DE");
            const string SEP = ";";

            int anzahlZeilen = viertelstundenwerte ? STUNDEN_JAHR * 4 : STUNDEN_JAHR;
            TimeSpan schritt = viertelstundenwerte ? TimeSpan.FromMinutes(15) : TimeSpan.FromHours(1);

            // Nicht-Schaltjahr verwenden, damit das 8760-h-Simulationsjahr sauber
            // auf den Kalender passt (kein 29. Februar).
            int jahr = DateTime.Now.Year;
            while (DateTime.IsLeapYear(jahr)) jahr++;
            DateTime zeit = new DateTime(jahr, 1, 1, 0, 0, 0);

            // UTF-8 mit BOM, damit Excel Umlaute korrekt anzeigt
            using (StreamWriter sw = new StreamWriter(dateiname, false, new UTF8Encoding(true)))
            {
                // Kopfzeile
                StringBuilder kopf = new StringBuilder();
                kopf.Append("Zeitstempel").Append(SEP).Append("Außentemperatur [°C]");
                foreach (CsvSpalte sp in spalten)
                    kopf.Append(SEP).Append(sp.Name);
                sw.WriteLine(kopf.ToString());

                // Datenzeilen
                StringBuilder zeile = new StringBuilder(256);
                for (int i = 0; i < anzahlZeilen; i++)
                {
                    zeile.Clear();
                    zeile.Append(zeit.ToString("dd.MM.yyyy HH:mm", kultur));

                    // Außentemperatur (Stundenwert, bei Viertelstundenraster je Stunde wiederholt)
                    int stundenIndex = viertelstundenwerte ? i / 4 : i;
                    zeile.Append(SEP);
                    if (temperaturStuendlich != null && stundenIndex < temperaturStuendlich.Length)
                        zeile.Append(temperaturStuendlich[stundenIndex].ToString("0.0##", kultur));

                    foreach (CsvSpalte sp in spalten)
                    {
                        zeile.Append(SEP);
                        zeile.Append(WertHolen(sp.Werte, i, viertelstundenwerte).ToString("0.0##", kultur));
                    }

                    sw.WriteLine(zeile.ToString());
                    zeit += schritt;
                }
            }
        }

        /// <summary>
        /// Liefert den Wert einer Ganglinie für die Zeile i im Ziel-Zeitraster und
        /// rechnet bei Bedarf zwischen Stunden- und Viertelstundenraster um.
        /// </summary>
        private static float WertHolen(float[] werte, int i, bool viertelstundenwerte)
        {
            if (werte == null || werte.Length == 0) return 0f;

            bool quelleViertelstunden = werte.Length >= STUNDEN_JAHR * 4;

            if (viertelstundenwerte)
            {
                // Ziel: Viertelstundenraster
                int index = quelleViertelstunden ? i : i / 4;
                return index < werte.Length ? werte[index] : 0f;
            }
            else
            {
                // Ziel: Stundenraster
                if (quelleViertelstunden)
                {
                    // Stundenmittel aus 4 Viertelstundenwerten (Leistungsmittelwert)
                    int basis = i * 4;
                    if (basis + 3 >= werte.Length) return 0f;
                    return (werte[basis] + werte[basis + 1] + werte[basis + 2] + werte[basis + 3]) / 4f;
                }
                return i < werte.Length ? werte[i] : 0f;
            }
        }
    }
}
