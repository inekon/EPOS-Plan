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

        // Zuletzt verwendeter Export-Ordner wird gemerkt - unter Windows im selben
        // Registry-Zweig wie die Sprach-Einstellung der Anwendung. Der Zweig steht seit
        // iU5 im Adapter (RegistryEinstellungen), hier nur noch der Wertname.
        private const string EINSTELLUNG_PFAD = "CsvExportPfad";

        /// <summary>Liest den zuletzt verwendeten Export-Ordner; <c>null</c>, wenn keiner gemerkt ist.</summary>
        private static string LetztenPfadLesen()
        {
            return Dienste.Einstellungen.Lies(EINSTELLUNG_PFAD, null);
        }

        /// <summary>
        /// Merkt sich den Ordner der gespeicherten Datei für den nächsten Export.
        /// Fehler bleiben still: Pfad merken ist eine Bequemlichkeit, kein Auftrag.
        /// </summary>
        private static void PfadMerken(string dateiname)
        {
            string ordner = Path.GetDirectoryName(dateiname);
            if (string.IsNullOrEmpty(ordner)) return;

            Dienste.Einstellungen.Schreib(EINSTELLUNG_PFAD, ordner);
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
                Dienste.Dialog.Meldung("Keine Datenreihe für den Export ausgewählt!", "CSV Export");
                return;
            }

            // Zuletzt verwendeten Export-Ordner vorschlagen, sonst "Dokumente"
            string startOrdner = LetztenPfadLesen();
            if (string.IsNullOrEmpty(startOrdner) || !Directory.Exists(startOrdner))
                startOrdner = Dienste.Pfade.Dokumente;

            // Der Ordner geht MIT im Dateinamen hinein: Ein Startordner allein wird von
            // Windows ignoriert, sobald sich das System für die Anwendung bereits einen
            // zuletzt verwendeten Ordner gemerkt hat. Der Adapter setzt beides.
            string dateiname = Dienste.Datei.DateiSpeichern(
                "CSV Export",
                "CSV Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
                Path.Combine(startOrdner, vorschlagDateiname));

            if (string.IsNullOrEmpty(dateiname)) return;

            // Ordner für den nächsten Export merken
            PfadMerken(dateiname);

            try
            {
                Schreiben(dateiname, temperaturStuendlich, spalten, viertelstundenwerte);
                Dienste.Dialog.Meldung("CSV-Datei wurde erstellt:\n" + dateiname, "CSV Export");
            }
            catch (Exception ex)
            {
                Dienste.Dialog.Fehler("Fehler beim Schreiben der CSV-Datei:\n" + ex.Message, "CSV Export");
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
                // Kopfzeile - Spaltennamen entschärft und eindeutig gemacht.
                StringBuilder kopf = new StringBuilder();
                kopf.Append("Zeitstempel").Append(SEP).Append("Außentemperatur [°C]");
                foreach (string name in Spaltenkoepfe(spalten, SEP))
                    kopf.Append(SEP).Append(name);
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
        /// Baut die Spaltenüberschriften der Kopfzeile auf.
        ///
        /// Zwei Dinge, die vorher fehlten und mit den Speicher-Spalten aus Paket 7
        /// erstmals real auftreten konnten - dort geht der Bezeichner eines Speichers
        /// ungefiltert in den Kopf:
        ///
        ///  - Das Trennzeichen im Namen (ein Speicher darf "600 l; Vitocell" heißen)
        ///    hätte die Kopfzeile um eine Spalte verschoben und die ganze Datei gegen
        ///    die Datenzeilen verrutschen lassen. Es wird durch ein Komma ersetzt,
        ///    ebenso Zeilenumbrüche.
        ///  - Zwei Speicher dürfen denselben Bezeichner tragen (Katalog und
        ///    Projektkopie). Gleichnamige Spalten macht Excel beim Auswerten
        ///    ununterscheidbar; die zweite bekommt deshalb "_2", die dritte "_3" usw.
        /// </summary>
        private static List<string> Spaltenkoepfe(List<CsvSpalte> spalten, string separator)
        {
            var ergebnis = new List<string>(spalten.Count);
            var zaehler = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (CsvSpalte sp in spalten)
            {
                string name = (sp.Name ?? "").Replace(separator, ",")
                                             .Replace("\r", " ").Replace("\n", " ").Trim();
                if (name.Length == 0) name = "Spalte";

                int n;
                if (zaehler.TryGetValue(name, out n))
                {
                    zaehler[name] = n + 1;
                    ergebnis.Add(name + "_" + (n + 1));
                }
                else
                {
                    zaehler[name] = 1;
                    ergebnis.Add(name);
                }
            }

            return ergebnis;
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
