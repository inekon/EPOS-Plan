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
            _localCachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CECModuleImporter", "cec_modules.csv");
        }

        public async Task<(bool success, string message)> LoadDataAsync(IProgress<string> progress = null)
        {
            progress?.Report("Suche lokalen Cache…");

            if (File.Exists(_localCachePath))
            {
                var age = DateTime.Now - File.GetLastWriteTime(_localCachePath);
                if (age.TotalDays < 30)
                {
                    progress?.Report("Lade aus lokalem Cache…");
                    return ParseCsv(_localCachePath);
                }
            }

            progress?.Report("Verbinde mit Quellen…");

            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(45);
                http.DefaultRequestHeaders.Add("User-Agent", "CECModuleImporter/1.0");

                foreach (var url in new[] { Url1, Url2, Url3 })
                {
                    try
                    {
                        progress?.Report($"Versuche: {new Uri(url).Host}…");
                        var response = await http.GetAsync(url).ConfigureAwait(false);

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

                        progress?.Report("Datei gespeichert, parse Daten…");
                        return ParseCsv(_localCachePath);
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"Fehler: {ex.Message}");
                    }
                }
            }

            if (File.Exists(_localCachePath))
            {
                progress?.Report("Verwende vorhandenen Cache…");
                return ParseCsv(_localCachePath);
            }

            return (false, "Keine CEC-Datenquelle erreichbar.");
        }

        public (bool success, string message) LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return (false, "Datei nicht gefunden.");
            return ParseCsv(filePath);
        }

        private (bool success, string message) ParseCsv(string path)
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

                if (dataLines.Count <= 1) return (false, "CSV leer.");

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

                    DateTime dateTime = DateTime.Parse(fields[26], CultureInfo.InvariantCulture);
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
                        Date = dateTime.Year,
                        // ... füge hier weitere Felder nach Bedarf hinzu
                    };
                    _allModules.Add(mod);
                }

                return (true, $"{_allModules.Count} Module geladen.");
            }
            catch (Exception ex)
            {
                return (false, "Fehler: " + ex.Message);
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


        public IEnumerable<PVModule> Filter(
           string manufacturer = null,
           string namePattern = null,
           string technology = null,
           double? minPower = null,
           double? maxPower = null,
           double? minEfficiency = null,
           double? maxEfficiency = null,
           bool? bifacial = null)
        {
            var q = _allModules.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(namePattern) && namePattern != "*")
            {
                var m = BuildWildcardMatcher(namePattern);
                q = q.Where(x => m(x.Name));
            }
            if (!string.IsNullOrWhiteSpace(manufacturer) && manufacturer != "(alle)")
            {
                if (manufacturer.Contains('*') || manufacturer.Contains('?'))
                {
                    var m = BuildWildcardMatcher(manufacturer);
                    q = q.Where(x => m(x.Manufacturer));
                }
                else
                {
                    q = q.Where(x => x.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase));
                }
            }
 
            if (!string.IsNullOrWhiteSpace(technology) && technology != "(alle)")
            {
                if (technology.Contains('*') || technology.Contains('?'))
                {
                    var m = BuildWildcardMatcher(technology);
                    q = q.Where(x => m(x.Technology));
                }
                else
                {
                    q = q.Where(x => x.Technology.Equals(technology, StringComparison.OrdinalIgnoreCase));
                }
            }
            if (minPower.HasValue && minPower.Value > 0) q = q.Where(x => x.STC >= minPower.Value);
            if (maxPower.HasValue && maxPower.Value < 99999) q = q.Where(x => x.STC <= maxPower.Value);
            if (minEfficiency.HasValue && minEfficiency.Value > 0) q = q.Where(x => x.Efficiency >= minEfficiency.Value);
            if (maxEfficiency.HasValue && maxEfficiency.Value < 30) q = q.Where(x => x.Efficiency <= maxEfficiency.Value);
            if (bifacial.HasValue)
                q = q.Where(x => bifacial.Value
                    ? x.Bifacial == "1" || x.Bifacial.Equals("true", StringComparison.OrdinalIgnoreCase)
                    : x.Bifacial != "1" && !x.Bifacial.Equals("true", StringComparison.OrdinalIgnoreCase));

            return q.OrderBy(x => x.Manufacturer).ThenBy(x => x.Name);
        }

        public static Func<string, bool> BuildWildcardMatcher(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern) || pattern == "*") return s => true;
            var rx = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            var reg = new Regex(rx, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return s => reg.IsMatch(s ?? string.Empty);
        }
    }
}