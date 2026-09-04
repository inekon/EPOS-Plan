using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Zwischenstand des CEC-Abrufs (iU9-W13.0j) — ein SCHLUESSEL und seine
    /// Platzhalterwerte, kein fertiger Satz. Der Kern kennt keine Anzeigetexte;
    /// der Wirt uebersetzt.
    /// </summary>
    public readonly struct CecFortschritt
    {
        public CecFortschritt(string schluessel, params string[] werte)
        {
            Schluessel = schluessel ?? "";
            Werte = werte ?? Array.Empty<string>();
        }

        /// <summary>Sprachneutraler Schluessel, z. B. <c>CEC_PROT_VERBINDEN</c>.</summary>
        public string Schluessel { get; }

        /// <summary>Platzhalterwerte in der Reihenfolge <c>{0}</c>, <c>{1}</c>, …</summary>
        public string[] Werte { get; }
    }

    public class CECDataService
    {
        private const string Url1 = "https://raw.githubusercontent.com/NREL/SAM/refs/heads/develop/deploy/libraries/CEC%20Modules.csv";
        private const string Url2 = "https://raw.githubusercontent.com/NREL/SAM/develop/deploy/libraries/CEC%20Modules.csv";
        private const string Url3 = "https://raw.githubusercontent.com/pvlib/pvlib-python/main/pvlib/data/sam-library-cec-modules-2019-03-05.csv";

        private readonly string _localCachePath;
        private List<PVModule> _allModules = new List<PVModule>();

        public IReadOnlyList<PVModule> AllModules => _allModules;

        public CECDataService()
        {
            // BenutzerLokalBasis und nicht BenutzerLokal: Dieser Zwischenspeicher liegt
            // seit jeher unter LocalApplicationData\CECModuleImporter und NICHT unter
            // dem Anwendungsordner. Ein Zusammenlegen wuerde den Bestandsspeicher jedes
            // Rechners entwerten.
            _localCachePath = Dienste.Pfade.Verbinde(
                Dienste.Pfade.BenutzerLokalBasis, "CECModuleImporter", "cec_modules.csv");
        }

        /// <summary>
        /// Holt die CEC-Modulliste — aus dem Zwischenspeicher, wenn er juenger als
        /// 30 Tage ist, sonst ueber HTTP von einer der drei Quellen.
        ///
        /// <para><b>Der Abbruch ist neu</b> (iU9-W13.0j, Risiko R-W13-3): Drei URLs
        /// mit je 45 Sekunden Zeitgrenze sind im schlechtesten Fall mehr als zwei
        /// Minuten, in denen der Anwender nichts tun konnte. Der Melder war schon
        /// da — die Maske uebergab ihn nur nicht (Befund W13-B38).</para>
        ///
        /// <para><b>Die Fortschrittstexte sind SCHLUESSEL</b> und keine deutschen
        /// Saetze mehr: Der Kern kennt keine Anzeigetexte. Der Wirt uebersetzt sie
        /// (<c>Texte.Zu</c>); der Hostname der gerade versuchten Quelle reist als
        /// Platzhalterwert mit.</para>
        /// </summary>
        public async Task<(bool success, CecFortschritt meldung)> LoadDataAsync(
            IProgress<CecFortschritt> progress = null,
            System.Threading.CancellationToken abbruch = default)
        {
            progress?.Report(new CecFortschritt("CEC_PROT_CACHE_SUCHEN"));

            if (File.Exists(_localCachePath))
            {
                var age = DateTime.Now - File.GetLastWriteTime(_localCachePath);
                if (age.TotalDays < 30)
                {
                    progress?.Report(new CecFortschritt("CEC_PROT_CACHE_LADEN"));
                    return ParseCsv(_localCachePath);
                }
            }

            progress?.Report(new CecFortschritt("CEC_PROT_VERBINDEN"));

            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(45);
                http.DefaultRequestHeaders.Add("User-Agent", "CECModuleImporter/1.0");

                foreach (var url in new[] { Url1, Url2, Url3 })
                {
                    abbruch.ThrowIfCancellationRequested();
                    try
                    {
                        progress?.Report(new CecFortschritt("CEC_PROT_VERSUCHE", new Uri(url).Host));
                        var response = await http.GetAsync(url, abbruch).ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode) continue;

                        var csv = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (csv.Length < 1000) continue;

                        var dir = Path.GetDirectoryName(_localCachePath);
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                        // .NET 4.8 kompatibles Schreiben
                        using (var sw = new StreamWriter(_localCachePath, false, Encoding.UTF8))
                        {
                            await sw.WriteAsync(csv).ConfigureAwait(false);
                        }

                        progress?.Report(new CecFortschritt("CEC_PROT_GESPEICHERT"));
                        return ParseCsv(_localCachePath);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        progress?.Report(new CecFortschritt("CEC_PROT_FEHLER", ex.Message));
                    }
                }
            }

            if (File.Exists(_localCachePath))
            {
                progress?.Report(new CecFortschritt("CEC_PROT_CACHE_ALT"));
                return ParseCsv(_localCachePath);
            }

            return (false, new CecFortschritt("CEC_MSG_KEINE_QUELLE"));
        }

        public (bool success, CecFortschritt meldung) LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return (false, new CecFortschritt("CEC_MSG_DATEI_FEHLT"));
            return ParseCsv(filePath);
        }

        private (bool success, CecFortschritt meldung) ParseCsv(string path)
        {
            try
            {
                var allLines = File.ReadAllLines(path);
                var dataLines = new List<string>();
                bool headerFound = false;

                foreach (var line in allLines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                    if (trimmed.IndexOf("Source", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (trimmed.IndexOf("[0]", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                   
                    if (!headerFound)
                    {
                        dataLines.Add(line);
                        headerFound = true;
                        continue;
                    }

                    if (dataLines.Count == 1 && (trimmed.IndexOf("Units", StringComparison.OrdinalIgnoreCase) >= 0 || IsUnitsRow(trimmed)))
                        continue;
 
                    
                    dataLines.Add(line);
                }

                if (dataLines.Count <= 1) return (false, new CecFortschritt("CEC_MSG_LEER"));

                var headers = SplitCsvLine(dataLines[0]);
                var colIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Length; i++)
                    colIndex[headers[i].Trim().ToLowerInvariant().Replace(" ", "_")] = i;

                // Hilfsfunktion lokal (C# 7.3 erlaubt lokale Funktionen)
                int GetCol(string name) => colIndex.TryGetValue(name, out int idx) ? idx : -1;

                _allModules = new List<PVModule>();

                for (int li = 1; li < dataLines.Count; li++)
                {
                    var fields = SplitCsvLine(dataLines[li]);
                    if (fields.Length < 3) continue;

                    string GetF(int idx) => (idx >= 0 && idx < fields.Length) ? fields[idx].Trim() : "";

                    // BEFUND W13-B48, BEHOBEN: Der Vorlaeufer griff hier auf den FESTEN
                    // Spaltenindex 26 zu, obwohl JEDES andere Feld ueber die Kopfzeile
                    // aufgeloest wird - eine geaenderte Spaltenfolge des NREL-Katalogs
                    // haette den ganzen Import geworfen, nicht nur das Datum. Jetzt
                    // kommt die Spalte aus derselben Kopfzeilenzuordnung, und ein
                    // unlesbares Datum kostet die Zeile ihr Jahr, nicht die Datei
                    // ihren Import.
                    int jahr = 0;
                    {
                        string datumstext = GetF(GetCol("date"));
                        DateTime datum;
                        if (DateTime.TryParse(datumstext, CultureInfo.InvariantCulture,
                                              DateTimeStyles.None, out datum))
                            jahr = datum.Year;
                    }
                    var mod = new PVModule
                    {
                        Database = "CEC",  
                        Name = GetF(GetCol("name")),
                        Manufacturer = GetF(GetCol("manufacturer")),
                        Technology = GetF(GetCol("technology")),
                        Bifacial = GetF(GetCol("bifacial")),
                        STC = SafeD(GetF(GetCol("stc"))),
                        PTC = SafeD(GetF(GetCol("ptc"))),
                        A_c = SafeD(GetF(GetCol("a_c"))),
                        Length = SafeD(GetF(GetCol("length"))),
                        Width = SafeD(GetF(GetCol("width"))),
                        N_s = SafeI(GetF(GetCol("n_s"))),
                        I_sc_ref = SafeD(GetF(GetCol("i_sc_ref"))),
                        V_oc_ref = SafeD(GetF(GetCol("v_oc_ref"))),
                        I_mp_ref = SafeD(GetF(GetCol("i_mp_ref"))),
                        V_mp_ref = SafeD(GetF(GetCol("v_mp_ref"))),
                        alpha_sc = SafeD(GetF(GetCol("alpha_sc"))),
                        beta_oc = SafeD(GetF(GetCol("beta_oc"))),
                        gamma_pmp = SafeD(GetF(GetCol("gamma_pmp"))),
                        T_NOCT = SafeD(GetF(GetCol("t_noct"))),
                        Date = jahr,
                        // ... füge hier weitere Felder nach Bedarf hinzu
                    };
                    _allModules.Add(mod);
                }

                return (true, new CecFortschritt("CEC_MSG_GELADEN",
                    _allModules.Count.ToString(CultureInfo.InvariantCulture)));
            }
            catch (Exception ex)
            {
                return (false, new CecFortschritt("CEC_MSG_FEHLER", ex.Message));
            }
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuote = false;
            var cur = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') inQuote = !inQuote;
                else if (c == ',' && !inQuote) { result.Add(cur.ToString()); cur.Clear(); }
                else cur.Append(c);
            }
            result.Add(cur.ToString());
            return result.ToArray();
        }

        private static double SafeD(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s == "N/A" || s == "-") return 0.0;
            return double.TryParse(s.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ? d : 0.0;
        }

        private static int SafeI(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s == "N/A" || s == "-") return 0;
            return int.TryParse(s.Trim(), out int i) ? i : 0;
        }

        private static bool IsUnitsRow(string line)
        {
            var markers = new[] { "m2", ",m,", ",A,", ",V,", ",K,", "Ohm" };
            return markers.Count(u => line.IndexOf(u, StringComparison.OrdinalIgnoreCase) >= 0) >= 2;
        }

        // ── Filter ───────────────────────────────────────────────────

        public IEnumerable<string> GetManufacturers() =>
            _allModules.Select(m => m.Manufacturer).Distinct().OrderBy(x => x);

        public IEnumerable<int> GetYears() =>
            _allModules.Select(m => m.Date).Where(y => y > 1990).Distinct().OrderBy(x => x);

        public IEnumerable<string> GetTechnologies() =>
            _allModules.Select(m => m.Technology).Distinct().OrderBy(x => x);


        // iU9-W13.0j: Filter(...) und BuildWildcardMatcher sind GELOESCHT
        // (Befund W13-B41). Sie waren die DRITTE Platzhaltersuche des Bestands -
        // neben Form_CECImport.GetFilterRegex und EPOS.Kern/Allgemein/Suchmuster,
        // das es seit W9 gibt. Beide hatte nie ein Aufrufer; die Maske filterte
        // selbst. Der Nachfolger PvModulImportDialog nimmt Suchmuster.
        //
        // Mit ihnen faellt auch der Steuerwert "(alle)" aus dem Kern: Er war dort
        // ein deutscher ANZEIGETEXT, gegen den verglichen wurde (Befund W13-B39).

    }
}