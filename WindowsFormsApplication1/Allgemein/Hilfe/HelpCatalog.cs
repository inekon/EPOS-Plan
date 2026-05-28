using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace WindowsFormsApplication1
{
    public class HelpEntry
    {
        public string Tooltip { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class WordPressHelpCatalog
    {
        private readonly HttpClient _http = new();
        private readonly Dictionary<string, HelpEntry> _cache = new();
        private string _baseUrl;

        public WordPressHelpCatalog(string baseUrl) => _baseUrl = baseUrl;

/*
        public async Task LoadAllAsync()
        {
            var url = $"{_baseUrl}/wp-json/wp/v2/help?per_page=10&_fields=slug,link,title";
            var json = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            
            foreach (var page in doc.RootElement.EnumerateArray())
            {
                var slug = page.GetProperty("slug").GetString() ?? "";
                
                _cache[slug] = new HelpEntry
                {
                    Tooltip = StripHtml(page.GetProperty("title").GetProperty("rendered").GetString()),
                    Url = page.GetProperty("link").GetString() ?? ""
                };
            }
        }
*/
 
// Falls Ihre Klasse ein internes Dictionary nutzt (z. B. private Dictionary<string, HelpEntry> _cache;)
public async Task LoadAllAsync()
    {
        // Pfad für die lokale Backup-Datei im AppData-Verzeichnis
        string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeineAnwendung");
        string localBackupPath = Path.Combine(appDataFolder, "help_cache.json");

        int currentPage = 1;
        bool hasMorePages = true;
        bool onlineLoadSuccessful = false;
        int previousCacheCount = 0; // SICHERHEITS-CHECK FÜR LOKALEN SERVER

        // Temporärer Cache, um bei Fehlern den alten Cache nicht unvollständig zu überschreiben
        var tempCache = new System.Collections.Generic.Dictionary<string, HelpEntry>();

        while (hasMorePages)
        {
            // per_page auf 100 erhöht für maximale Effizienz, page= dynamisch angehängt
            var url = $"{_baseUrl}/wp-json/wp/v2/help?per_page=100&page={currentPage}&_fields=slug,link,title";

            try
            {
                // Timeout schützt vor ewigem Hängen bei schlechter Verbindung
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _http.GetAsync(url, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    hasMorePages = false;
                    break;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var array = doc.RootElement;
                if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() == 0)
                {
                    hasMorePages = false;
                    if (currentPage > 1) onlineLoadSuccessful = true; // Wir haben auf vorherigen Seiten Daten erhalten
                }
                else
                {
                    foreach (var page in array.EnumerateArray())
                    {
                        var slug = page.GetProperty("slug").GetString() ?? "";

                        if (!string.IsNullOrEmpty(slug) && !tempCache.ContainsKey(slug))
                        {
                            tempCache[slug] = new HelpEntry
                            {
                                Tooltip = StripHtml(page.GetProperty("title").GetProperty("rendered").GetString() ?? ""),
                                Url = page.GetProperty("link").GetString() ?? ""
                            };
                        }
                    }
                    // SICHERHEIT 2: Wenn nach dem Durchlauf keine NEUEN Elemente hinzugekommen sind,
                    // liefert der Testserver vermutlich nur Duplikate. -> Abbrechen!
                    if (tempCache.Count == previousCacheCount)
                    {
                        hasMorePages = false;
                        onlineLoadSuccessful = true;
                        break;
                    }

                    previousCacheCount = tempCache.Count; // Zähler aktualisieren
                    onlineLoadSuccessful = true;
                    currentPage++;
                }
            }
            catch (Exception)
            {
                // Netzwerkfehler oder Server-Timeout -> Schleife abbrechen und Fallback nutzen
                hasMorePages = false;
                onlineLoadSuccessful = false;
            }
        }

        // FALL 1: Online-Abruf war erfolgreich -> Hauptcache befüllen und lokal sichern
        if (onlineLoadSuccessful && tempCache.Count > 0)
        {
            // Lokalen Speicher (_cache) aktualisieren
            _cache.Clear();
            foreach (var kvp in tempCache)
            {
                _cache[kvp.Key] = kvp.Value;
            }

            // Als lokales JSON-Backup für den nächsten Offline-Start wegsichern
            try
            {
                if (!Directory.Exists(appDataFolder)) Directory.CreateDirectory(appDataFolder);

                string jsonCache = JsonSerializer.Serialize(_cache);
                File.WriteAllText(localBackupPath, jsonCache);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Schreiben des Backups: {ex.Message}");
            }

            return;
        }

        // FALL 2: Offline oder Serverfehler -> Aus lokaler Backup-Datei laden
        if (File.Exists(localBackupPath))
        {
            try
            {
                string localJson = File.ReadAllText(localBackupPath);
                var savedCache = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, HelpEntry>>(localJson);

                if (savedCache != null)
                {
                    _cache.Clear();
                    foreach (var kvp in savedCache)
                    {
                        _cache[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Lesen der Backup-Datei: {ex.Message}");
            }
        }
    }



    // Hilfsmethode, um die Keys im Testprogramm auszulesen
    public ICollection<string> GetAllCachedSlugs() => _cache.Keys;

        public HelpEntry Get(string slug) => _cache.TryGetValue(slug, out var e) ? e : new HelpEntry();

        private static string StripHtml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";

            // HTML-Tags entfernen
            string clean = Regex.Replace(s, "<.*?>", "").Trim();

            // HTML-Entities (wie &amp; oder &quot;) in normalen Text umwandeln
            return System.Net.WebUtility.HtmlDecode(clean);
        }
    }

    [ProvideProperty("HelpKey", typeof(Control))]
    public class HelpExtender : System.ComponentModel.Component, System.ComponentModel.IExtenderProvider
    {
        // Speichert, welches Control welchen HelpKey zugewiesen bekommen hat
        private readonly Dictionary<Control, string> _keys = new();
        
        private readonly WordPressHelpCatalog _catalog;
        
        private Form_HelpPopup _popup;

        public HelpExtender(WordPressHelpCatalog catalog) => _catalog = catalog;

        // Pflichtmethode für IExtenderProvider: Wer darf diese Eigenschaft nutzen?
        public bool CanExtend(object o) => o is Control;

        // Die Get-Methode für das Framework
        public string GetHelpKey(Control c) => _keys.TryGetValue(c, out var k) ? k : "";

        // Die Set-Methode für das Framework
        public void SetHelpKey(Control c, string key)
        {
            if (c == null) return;
            _keys[c] = key;

            c.MouseEnter -= Control_MouseEnter;
            c.MouseEnter += Control_MouseEnter;
            c.MouseLeave -= Control_MouseLeave;
            c.MouseLeave += Control_MouseLeave;
        }

        /// <summary>
        /// Scannt ein gesamtes Formular ab (für Controls, die von Anfang an existieren)
        /// </summary>
        public void RegisterForm(Form form)
        {
            if (this.DesignMode || form == null) return;
            RegisterControl(form, form.Name);
        }

        /// <summary>
        /// Scannt ein spezifisches Control/UserControl ab (auch für dynamisch nachgeladene UIs)
        /// </summary>
        public void RegisterControl(Control rootContainer, string prefixName)
        {
            if (this.DesignMode || rootContainer == null) return;

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "help_mapping.txt");
            if (!File.Exists(filePath)) return;

            try
            {
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    // Leerzeilen und Kommentare überspringen
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    // Splitten bei '=' (Key und Slug trennen)
                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue; // Ungültige Zeile einfach überspringen!

                    string fullPath = parts[0].Trim();
                    string wpSlug = parts[1].Trim();

                    // Ersten Punkt suchen, um den Präfix zu isolieren
                    int firstDot = fullPath.IndexOf('.');
                    if (firstDot <= 0) continue; // Kein Punkt da? Zeile überspringen!

                    string configPrefix = fullPath.Substring(0, firstDot).Trim();
                    string controlPath = fullPath.Substring(firstDot + 1).Trim();

                    // WICHTIG: Nur verarbeiten, wenn diese Zeile GENAU zu dem aktuell gescannten Container gehört!
                    if (string.Equals(configPrefix, prefixName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Rekursive Suche starten
                        Control targetControl = FindControlRecursive(rootContainer, controlPath);

                        if (targetControl != null)
                        {
                            // Registrieren (überschreibt nichts anderes, fügt nur hinzu)
                            this.SetHelpKey(targetControl, wpSlug);
                            System.Diagnostics.Debug.WriteLine($"[Help] Erfolgreich registriert: {prefixName}.{controlPath} -> {wpSlug}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[Help] WARNUNG: Control '{controlPath}' wurde in '{prefixName}' nicht gefunden.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Falls doch was schiefgeht, Meldung im Debug-Fenster
                System.Diagnostics.Debug.WriteLine("KRITISCHER FEHLER beim Mapping: " + ex.ToString());
            }
        }

        private Control FindControlRecursive(Control container, string remainingPath)
        {
            if (string.IsNullOrEmpty(remainingPath) || container == null) return null;

            int nextDot = remainingPath.IndexOf('.');

            // FALL A: Wir suchen das finale Control (z.B. "btn_Carrier")
            if (nextDot == -1)
            {
                return FindControlByNameDeep(container, remainingPath);
            }

            // FALL B: Wir müssen zuerst einen Zwischen-Container finden (z.B. "ucCarrierPanel")
            string currentTargetName = remainingPath.Substring(0, nextDot).Trim();
            string tail = remainingPath.Substring(nextDot + 1).Trim();

            Control nextContainer = FindControlByNameDeep(container, currentTargetName);
            if (nextContainer != null)
            {
                // Tiefer graben: Wir suchen im gefundenen Container nach dem Rest des Pfades ("tail")
                return FindControlRecursive(nextContainer, tail);
            }

            return null;
        }

        /// <summary>
        /// Eine echte, unfehlbare Tiefensuche über alle Control-Ebenen hinweg.
        /// </summary>
        private Control FindControlByNameDeep(Control root, string name)
        {
            if (root == null) return null;

            // Stimmt der Name direkt?
            if (string.Equals(root.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            // Durchsuche alle Kinder und Kindeskinder rekursiv
            foreach (Control child in root.Controls)
            {
                Control found = FindControlByNameDeep(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        // --- Die bekannten Mouse-Events (bleiben unverändert) ---
        private void Control_MouseEnter(object sender, EventArgs e)
        {
            if (this.DesignMode) return;
            Control ctrl = sender as Control;
            if (ctrl != null && _keys.TryGetValue(ctrl, out string key))
            {
                var entry = _catalog.Get(key);
                if (!string.IsNullOrEmpty(entry.Tooltip))
                {
                    if (_popup == null || _popup.IsDisposed) _popup = new Form_HelpPopup();
                    _popup.ShowHelp(entry.Tooltip, entry.Url, Cursor.Position);
                }
            }
        }

        private void Control_MouseLeave(object sender, EventArgs e)
        {
            if (this.DesignMode || _popup == null) return;
            Timer delayTimer = new Timer { Interval = 500 };
            delayTimer.Tick += (s, ev) =>
            {
                delayTimer.Stop();
                delayTimer.Dispose();
                if (_popup != null && !_popup.Bounds.Contains(Cursor.Position)) _popup.Hide();
            };
            delayTimer.Start();
        }
    }

}
