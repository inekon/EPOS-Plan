using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
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

        /// <summary>
        /// Der Slug der Seite, wie WordPress ihn meldet.
        /// </summary>
        /// <remarks>
        /// Seit F7 ist der Link-Pfad der Schluessel. Der Slug ist nur noch die
        /// bequeme Kurzform - und liesse sich fast immer aus dem letzten
        /// Pfadabschnitt ableiten. Fast: Die Startseite traegt den Slug
        /// "epos-plan" bei Pfad "/". Wer den Slug mitfuehrt, statt ihn zu raten,
        /// verliert diesen Fall nicht. Aeltere Sicherungen ohne dieses Feld
        /// fallen auf den letzten Pfadabschnitt zurueck.
        /// </remarks>
        public string Slug { get; set; } = "";
    }

    public class WordPressHelpCatalog
    {
        private readonly HttpClient _http = new();

        // -------------------------------------------------------------------
        // F7 - Schluessel ist der Pfad, nicht der Slug
        //
        // Am 22.08.2026 gegen den Livebestand gemessen: 116 Seiten teilen sich nur
        // 108 eindeutige Slugs. Ein slug-geschluesselter Katalog verwirft acht
        // Seiten wortlos - darunter die DEUTSCHEN Seiten zu "installation" und
        // "update", die dadurch durch keine Zuordnung mehr erreichbar waeren.
        // Der Link-Pfad dagegen ist eindeutig: 116 Pfade zu 116 Seiten.
        // -------------------------------------------------------------------

        /// <summary>Alle Seiten, geschluesselt ueber den normalisierten Link-Pfad.</summary>
        private readonly Dictionary<string, HelpEntry> _nachPfad =
            new Dictionary<string, HelpEntry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Kurzform Slug -> Pfad. Der Slug bleibt zulaessig, solange er eindeutig
        /// ist; bei Mehrdeutigkeit gewinnt der erste Eintrag der REST-Antwort und
        /// der Zugriff protokolliert eine Warnung.
        /// </summary>
        private readonly Dictionary<string, string> _slugAufPfad =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Alle Kandidatenpfade je mehrdeutigem Slug - Grundlage der Warnung.</summary>
        private readonly Dictionary<string, List<string>> _slugMehrdeutig =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        // Jede Warnung genau einmal je Programmlauf: Control_MouseEnter feuert sonst
        // bei jedem Ueberfahren dieselbe Zeile ins Ausgabefenster.
        private readonly HashSet<string> _gewarnt =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Dateiname der lokalen Sicherung UND des mitgelieferten Startbestandes (F6).</summary>
        private const string StartbestandDateiName = "help_cache.json";

        private string _baseUrl;

        public WordPressHelpCatalog(string baseUrl) => _baseUrl = baseUrl;

        /// <summary>Anzahl der bekannten Hilfeseiten.</summary>
        public int SeitenAnzahl => _nachPfad.Count;

        // Meldet, wann der Ladelauf durch ist. MDIMainForm_Load ruft LoadAllAsync
        // bewusst ohne await auf; Formulare, die frueher oeffnen, saehen sonst einen
        // leeren Katalog und wuerden ihre Infobuttons voreilig abschalten (F3).
        private readonly TaskCompletionSource<bool> _geladen =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Ist der Ladelauf abgeschlossen? Auch der Rueckfall auf die lokale
        /// Sicherung zaehlt als abgeschlossen - danach aendert sich nichts mehr.
        /// </summary>
        public bool IsLoaded => _geladen.Task.IsCompleted;

        /// <summary>
        /// Wird abgeschlossen, sobald <see cref="LoadAllAsync"/> durch ist.
        /// </summary>
        public Task Loaded => _geladen.Task;

        // -------------------------------------------------------------------
        // Zugriff (F7): Slug ODER Pfad
        // -------------------------------------------------------------------

        /// <summary>
        /// Kennt der Katalog diese Adresse? Zulaessig sind beide Schreibweisen:
        /// die Kurzform als Slug ("klimadaten") und der eindeutige Link-Pfad
        /// ("/epos-plan/epos-plan-grundlagen/klimadaten/").
        /// </summary>
        public bool Contains(string adresse) => Aufloesen(adresse) != null;

        /// <summary>
        /// Katalogeintrag zu einem Slug oder einem Link-Pfad. Liefert nie
        /// <c>null</c>; eine unbekannte Adresse ergibt einen leeren Eintrag.
        /// </summary>
        public HelpEntry Get(string adresse) => Aufloesen(adresse) ?? new HelpEntry();

        /// <summary>Alle bekannten Kurzformen (Slugs).</summary>
        public ICollection<string> GetAllCachedSlugs() => _slugAufPfad.Keys;

        /// <summary>Alle bekannten Link-Pfade - je Seite genau einer.</summary>
        public ICollection<string> GetAllCachedPaths() => _nachPfad.Keys;

        /// <summary>
        /// Loest eine Adresse gegen den Katalog auf. Eine Adresse mit '/' gilt als
        /// Pfad, alles andere als Slug.
        /// </summary>
        private HelpEntry Aufloesen(string adresse)
        {
            if (string.IsNullOrWhiteSpace(adresse)) return null;

            string schluessel = adresse.Trim();
            return schluessel.IndexOf('/') >= 0 ? UeberPfad(schluessel) : UeberSlug(schluessel);
        }

        private HelpEntry UeberPfad(string angabe)
        {
            string pfad = PfadNormalisieren(angabe);
            if (pfad.Length == 0) return null;

            if (_nachPfad.TryGetValue(pfad, out HelpEntry treffer)) return treffer;

            // Bequemlichkeit: ein eindeutiges Pfadende genuegt. Ein mehrdeutiges
            // Ende wird nicht geraten, sondern gemeldet.
            HelpEntry einziger = null;
            int anzahl = 0;
            foreach (var kvp in _nachPfad)
            {
                if (!kvp.Key.EndsWith(pfad, StringComparison.OrdinalIgnoreCase)) continue;
                einziger = kvp.Value;
                if (++anzahl > 1) break;
            }

            if (anzahl == 1) return einziger;
            if (anzahl > 1)
            {
                Warnen("pfad:" + pfad,
                    $"[Help] WARNUNG: Das Pfadende '{pfad}' passt auf mehrere Seiten - " +
                    "bitte den vollstaendigen Pfad in help_mapping.txt eintragen.");
            }

            return null;
        }

        private HelpEntry UeberSlug(string slug)
        {
            // F7, Punkt 3: Mehrdeutigkeit faellt auf, statt still falsch zu sein.
            if (_slugMehrdeutig.TryGetValue(slug, out List<string> kandidaten) && kandidaten.Count > 1)
            {
                Warnen("slug:" + slug,
                    $"[Help] WARNUNG: Der Slug '{slug}' ist mehrdeutig ({kandidaten.Count} Seiten: " +
                    $"{string.Join(", ", kandidaten)}). Es gewinnt '{kandidaten[0]}'. " +
                    "In help_mapping.txt bitte den Pfad statt des Slugs eintragen (F7).");
            }

            if (_slugAufPfad.TryGetValue(slug, out string pfad) &&
                _nachPfad.TryGetValue(pfad, out HelpEntry treffer)) return treffer;

            return null;
        }

        private void Warnen(string kennung, string text)
        {
            if (!_gewarnt.Add(kennung)) return;
            System.Diagnostics.Debug.WriteLine(text);
        }

        /// <summary>
        /// Bringt eine volle URL oder einen Pfadschnipsel auf die Normalform
        /// <c>/abschnitt/unterabschnitt/</c> - klein geschrieben, mit fuehrendem
        /// und abschliessendem Schraegstrich.
        /// </summary>
        public static string PfadNormalisieren(string linkOderPfad)
        {
            if (string.IsNullOrWhiteSpace(linkOderPfad)) return "";

            string pfad = linkOderPfad.Trim();

            if (Uri.TryCreate(pfad, UriKind.Absolute, out Uri absolut)) pfad = absolut.AbsolutePath;

            // Abfrage und Anker abschneiden, falls jemand eine volle Adresse eintraegt.
            int schnitt = pfad.IndexOfAny(new[] { '?', '#' });
            if (schnitt >= 0) pfad = pfad.Substring(0, schnitt);

            try { pfad = Uri.UnescapeDataString(pfad); }
            catch (Exception) { /* die Rohform genuegt */ }

            pfad = pfad.Replace('\\', '/').Trim();
            if (pfad.Length == 0) return "";
            if (!pfad.StartsWith("/")) pfad = "/" + pfad;
            if (!pfad.EndsWith("/")) pfad += "/";

            return pfad.ToLowerInvariant();
        }

        private static string SlugAusPfad(string pfad)
        {
            if (string.IsNullOrEmpty(pfad)) return "";

            string kern = pfad.Trim('/');
            if (kern.Length == 0) return "";

            int letzter = kern.LastIndexOf('/');
            return letzter < 0 ? kern : kern.Substring(letzter + 1);
        }

        /// <summary>
        /// Baut Pfad- und Slug-Register neu auf. Der Schluessel der uebergebenen
        /// Sammlung ist dabei gleichgueltig - massgeblich ist der Link im Eintrag.
        /// Nur so liest sich auch eine aeltere, slug-geschluesselte Sicherung
        /// verlustfrei ein.
        /// </summary>
        private void IndizesAufbauen(IEnumerable<KeyValuePair<string, HelpEntry>> eintraege)
        {
            _nachPfad.Clear();
            _slugAufPfad.Clear();
            _slugMehrdeutig.Clear();
            _gewarnt.Clear();

            if (eintraege == null) return;

            foreach (var kvp in eintraege) EintragAufnehmen(kvp.Key, kvp.Value);

            if (_slugMehrdeutig.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Help] Katalog: {_nachPfad.Count} Seiten, {_slugAufPfad.Count} eindeutige Slugs, " +
                    $"{_slugMehrdeutig.Count} mehrdeutige Slugs ({string.Join(", ", _slugMehrdeutig.Keys)}) - " +
                    "diese bitte in help_mapping.txt ueber den Pfad ansprechen (F7).");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Help] Katalog: {_nachPfad.Count} Seiten.");
            }
        }

        private void EintragAufnehmen(string schluessel, HelpEntry eintrag)
        {
            if (eintrag == null) return;

            string pfad = PfadNormalisieren(eintrag.Url);
            if (pfad.Length == 0) pfad = PfadNormalisieren(schluessel);   // Notnagel ohne Link
            if (pfad.Length == 0) return;

            if (!_nachPfad.ContainsKey(pfad)) _nachPfad[pfad] = eintrag;

            // Der mitgefuehrte Slug hat Vorrang; nur eine aeltere Sicherung ohne
            // dieses Feld laesst ihn aus dem letzten Pfadabschnitt ableiten.
            string slug = string.IsNullOrWhiteSpace(eintrag.Slug)
                ? SlugAusPfad(pfad)
                : eintrag.Slug.Trim();
            if (slug.Length == 0) return;

            if (_slugAufPfad.TryGetValue(slug, out string bisher))
            {
                if (string.Equals(bisher, pfad, StringComparison.OrdinalIgnoreCase)) return;

                if (!_slugMehrdeutig.TryGetValue(slug, out List<string> liste))
                {
                    liste = new List<string> { bisher };
                    _slugMehrdeutig[slug] = liste;
                }
                if (!liste.Contains(pfad)) liste.Add(pfad);

                // Der erste bleibt Sieger - die Warnung beim Zugriff macht es sichtbar.
                return;
            }

            _slugAufPfad[slug] = pfad;
        }

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
 
    /// <summary>
    /// Laedt den Katalog und meldet anschliessend "geladen" - auch dann, wenn
    /// der Abruf gescheitert ist. Ohne diese Meldung koennte niemand zwischen
    /// "kein Treffer" und "noch nicht geladen" unterscheiden.
    /// </summary>
    public async Task LoadAllAsync()
    {
        try
        {
            await LoadAllCoreAsync();
        }
        catch (Exception ex)
        {
            // Der Ladelauf wird bewusst ohne await angestossen. Eine Ausnahme, die
            // hier entkaeme, waere eine unbeobachtete Task-Ausnahme - sichtbar erst
            // beim Aufraeumen durch den Speicherbereiniger, also praktisch nie.
            System.Diagnostics.Debug.WriteLine("[Help] FEHLER beim Laden des Katalogs: " + ex);
        }
        finally
        {
            _geladen.TrySetResult(true);
        }
    }

    private async Task LoadAllCoreAsync()
    {
        // Pfad für die lokale Sicherung im AppData-Verzeichnis
        string localBackupPath = SicherungsPfad();
        string appDataFolder = Path.GetDirectoryName(localBackupPath);

        int currentPage = 1;
        bool hasMorePages = true;
        bool onlineLoadSuccessful = false;
        int previousCacheCount = 0; // SICHERHEITS-CHECK FÜR LOKALEN SERVER

        // Temporärer Cache, um bei Fehlern den alten Cache nicht unvollständig zu überschreiben
        var tempCache = new System.Collections.Generic.Dictionary<string, HelpEntry>();

        while (hasMorePages)
        {

            // Plaintext, Wordpress Link Aufbau
            //
            //    http://deine-domain.de/wp-json/wp/v2/help
            //    └-─────────┬──────────┘└───┬───┘  └─┬┘└─┬┘
            //        Website - URL  API Präfix  Version REST-Base(Custom Post Type)
            
            var url = $"{_baseUrl}/wp-json/wp/v2/{Properties.Settings.Default.WordPressPrefix}?per_page=100&page={currentPage}&_fields=slug,link,title";
            // per_page auf 100 erhöht für maximale Effizienz, page= dynamisch angehängt

            // 1. wp-json (Das API - Präfix): Das sagt WordPress: „Achtung, jetzt kommt keine normale HTML-Webseite für den Browser,
            //                                sondern eine Daten-Anfrage im JSON-Format.“
            //
            // 2. wp/v2 (Der Namensraum / Namespace): wp steht für die WordPress-Kernfunktionen, v2 für die Version 2 der API.
            //
            // 3. help (Die REST-Base / Der Post-Type): Das ist der entscheidende Teil.In WordPress gibt es standardmäßig zwei
            //                                          eingebaute Inhaltstypen: posts (Beiträge) und pages (Seiten).
            //                                          Wenn du also Standard-Seiten abfragst, heißt die URL.../wp/v2/pages.
            //                                          Da aber ein eigenes Hilfesystem aufgebaut wird, einen Custom Post Type nutzen.

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
                    if (currentPage > 1) onlineLoadSuccessful = true; //  auf vorherigen Seiten Daten erhalten
                }
                else
                {
                    foreach (var page in array.EnumerateArray())
                    {
                        // F7: Geschluesselt wird ueber den Link-Pfad. Der frueher
                        // benutzte Slug liess acht der 116 Seiten unter den Tisch
                        // fallen, weil sieben Slugs doppelt vergeben sind.
                        var slug = page.GetProperty("slug").GetString() ?? "";
                        var link = page.GetProperty("link").GetString() ?? "";

                        string schluessel = PfadNormalisieren(link);
                        if (schluessel.Length == 0) schluessel = slug;      // Seite ohne Link
                        if (string.IsNullOrEmpty(schluessel)) continue;
                        if (tempCache.ContainsKey(schluessel)) continue;    // echte Dublette

                        tempCache[schluessel] = new HelpEntry
                        {
                            Tooltip = StripHtml(page.GetProperty("title").GetProperty("rendered").GetString() ?? ""),
                            Url = link,
                            Slug = slug
                        };
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

        // ---------------------------------------------------------------
        // Rangfolge (F6): Online > lokale Sicherung > mitgelieferter Startbestand
        // ---------------------------------------------------------------

        // FALL 1: Online-Abruf war erfolgreich -> Register aufbauen und lokal sichern
        if (onlineLoadSuccessful && tempCache.Count > 0)
        {
            IndizesAufbauen(tempCache);

            // Als lokale Sicherung für den nächsten Offline-Start wegschreiben.
            // Geschluesselt wird ueber den Pfad, damit die Sicherung genauso
            // verlustfrei ist wie der Katalog selbst (F7).
            try
            {
                if (!string.IsNullOrEmpty(appDataFolder) && !Directory.Exists(appDataFolder))
                    Directory.CreateDirectory(appDataFolder);

                string jsonCache = JsonSerializer.Serialize(tempCache);
                File.WriteAllText(localBackupPath, jsonCache);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Help] Fehler beim Schreiben der Sicherung: {ex.Message}");
            }

            return;
        }

        // FALL 2: Offline oder Serverfehler -> lokale Sicherung.
        if (LokaleSicherungLaden(localBackupPath)) return;

        // FALL 3: Auch die fehlt -> mitgelieferter Startbestand (F6).
        MitgelieferterStartbestandLaden();
    }

    /// <summary>
    /// Belegt den Katalog VOR dem Onlineabruf, damit er nie leer ist.
    /// </summary>
    /// <remarks>
    /// Entschaerft den Startwettlauf: <c>MDIMainForm_Load</c> stoesst
    /// <see cref="LoadAllAsync"/> bewusst ohne <c>await</c> an, damit der Start
    /// nicht blockiert. Formulare, die frueher oeffnen, sahen bisher einen
    /// leeren Katalog. Nach diesem Aufruf sehen sie den Startbestand; der
    /// Onlineabruf ersetzt ihn spaeter vollstaendig.
    /// Rangfolge wie beim Ladelauf: lokale Sicherung vor mitgeliefertem Bestand.
    /// </remarks>
    public void StartbestandLaden()
    {
        if (_nachPfad.Count > 0) return;   // schon belegt - nichts zu tun

        try
        {
            if (LokaleSicherungLaden(SicherungsPfad())) return;
            MitgelieferterStartbestandLaden();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[Help] FEHLER beim Vorbelegen des Katalogs: " + ex);
        }
    }

    /// <summary>Ablageort der lokalen Sicherung im AppData-Verzeichnis.</summary>
    private static string SicherungsPfad()
    {
        string ordner = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Application.ProductName ?? "WP-Plan");

        return Path.Combine(ordner, StartbestandDateiName);
    }

    private bool LokaleSicherungLaden(string pfad)
    {
        if (string.IsNullOrEmpty(pfad) || !File.Exists(pfad)) return false;

        try
        {
            var gesichert = JsonSerializer.Deserialize<Dictionary<string, HelpEntry>>(File.ReadAllText(pfad));
            if (gesichert == null || gesichert.Count == 0) return false;

            IndizesAufbauen(gesichert);
            System.Diagnostics.Debug.WriteLine(
                $"[Help] Katalog aus lokaler Sicherung: {pfad} ({_nachPfad.Count} Seiten).");

            return _nachPfad.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Help] WARNUNG: Lokale Sicherung nicht lesbar: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// F6 - Offline-Erstlauf. Der gepflegte Startbestand steckt als eingebettete
    /// Ressource in der Assembly; ohne ihn waere die Hilfe beim allerersten Start
    /// ohne Netz und ohne vorherigen Onlinelauf vollstaendig leer.
    /// </summary>
    private bool MitgelieferterStartbestandLaden()
    {
        try
        {
            using (Stream stream = typeof(WordPressHelpCatalog).Assembly
                       .GetManifestResourceStream(StartbestandDateiName))
            {
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Help] FEHLER: Eingebetteter Startbestand '{StartbestandDateiName}' fehlt. " +
                        "Ist er in der .csproj als EmbeddedResource mit passendem LogicalName eingetragen? " +
                        "Ohne Netz bleibt die Hilfe sonst leer.");
                    return false;
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    var startbestand = JsonSerializer.Deserialize<Dictionary<string, HelpEntry>>(reader.ReadToEnd());
                    if (startbestand == null || startbestand.Count == 0) return false;

                    IndizesAufbauen(startbestand);
                    System.Diagnostics.Debug.WriteLine(
                        $"[Help] Katalog aus mitgeliefertem Startbestand ({_nachPfad.Count} Seiten).");

                    return _nachPfad.Count > 0;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[Help] FEHLER beim Lesen des Startbestandes: " + ex);
            return false;
        }
    }

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
        /// <summary>
        /// Dateiname der Zuordnung - identisch neben der EXE und als eingebettete Ressource.
        /// </summary>
        private const string MappingDateiName = "help_mapping.txt";

        /// <summary>
        /// Logischer Name der eingebetteten Fassung. Er ist in der .csproj ueber
        /// &lt;LogicalName&gt; festgeschrieben, damit er nicht am Ordnerpfad haengt.
        /// </summary>
        private const string MappingRessourceName = "help_mapping.txt";

        /// <summary>
        /// Namenspraefix, an dem ein Infobutton erkannt wird. Alle vorhandenen
        /// Schaltflaechen heissen so; neue halten sich an dieselbe Konvention.
        /// </summary>
        private const string InfobuttonPraefix = "btn_Help";

        // Einmal gelesene Zuordnungszeilen. Der Ladeweg - Datei neben der EXE
        // schlaegt eingebettete Fassung (F2) - wird pro Programmlauf genau einmal
        // gegangen; eine geaenderte Datei wirkt nach dem naechsten Start.
        private static string[] _mappingZeilen;
        private static readonly object _mappingSperre = new object();

        // Speichert, welches Control welchen HelpKey zugewiesen bekommen hat
        private readonly Dictionary<Control, string> _keys = new();

        // F5: Dieser Extender ist anwendungsweit und lebt so lange wie das Programm.
        // Was hier haengen bleibt, wird nie wieder freigegeben - deshalb raeumt
        // Control_Disposed jeden Eintrag beim Schliessen eines Formulars wieder ab.
        //
        // Von UNS abgeschaltete Infobuttons (F3) samt ihrem urspruenglichen Zeiger.
        // Nur diese duerfen spaeter wieder eingeschaltet werden; ein Button, den die
        // Fachlogik gesperrt hat, bleibt gesperrt.
        private readonly Dictionary<Control, Cursor> _abgeschaltet = new();

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

            // Alle Bindungen erst loesen, dann setzen: die zentrale Registrierung
            // (F5) darf denselben Baum beliebig oft erfassen, ohne dass ein Klick
            // mehrfach ausgeloest wird.
            c.MouseEnter -= Control_MouseEnter;
            c.MouseEnter += Control_MouseEnter;
            c.MouseLeave -= Control_MouseLeave;
            c.MouseLeave += Control_MouseLeave;

            // F1: Ein Steuerelement, das wie eine Schaltflaeche aussieht, muss auf
            // den Klick reagieren. Der Klick heftet das Popup an, das Ueberfahren
            // behaelt sein fluechtiges Verhalten.
            c.Click -= Control_Click;
            c.Click += Control_Click;

            // Der anwendungsweite Extender ueberlebt jedes Formular - ohne dieses
            // Aufraeumen sammelte er Verweise auf laengst geschlossene Fenster.
            c.Disposed -= Control_Disposed;
            c.Disposed += Control_Disposed;
        }

        private void Control_Disposed(object sender, EventArgs e)
        {
            Control c = sender as Control;
            if (c == null) return;

            _keys.Remove(c);
            _abgeschaltet.Remove(c);
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

            var registrierte = new List<Control>();
            ZuordnungenAnwenden(rootContainer, prefixName, registrierte);

            // F3, erster Teil: Ein Infobutton ohne jede Zeile in der Zuordnung kann
            // nie etwas anzeigen. Dafuer braucht es den Katalog nicht - sofort abschalten.
            InfobuttonsOhneZuordnungAbschalten(rootContainer, prefixName);

            // F3, zweiter Teil: Ob hinter einem Slug auch Inhalt steht, laesst sich
            // erst sagen, wenn der Katalog geladen ist.
            NachKatalogAuswerten(rootContainer, registrierte);
        }

        /// <summary>
        /// F5 - Einstiegspunkt der zentralen Registrierung: erfasst einen ganzen
        /// Steuerelementbaum auf einmal.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Der Unterschied zu <see cref="RegisterControl"/>: Neben dem Praefix der
        /// Wurzel werden auch die Praefixe aller eingebetteten Formulare und
        /// UserControls angewandt. <c>MDIMainForm</c> traegt <c>Form_Start</c> als
        /// eingebettetes Formular (<c>TopLevel=false</c>), und <c>Form_Kosten</c>
        /// haengt <c>ucFuelSettings</c> zur Laufzeit ein - beide sollen ihre eigenen
        /// Zeilen aus <c>help_mapping.txt</c> bekommen, ohne dass jemand dafuer Code
        /// in ihr Formular schreiben muss.
        /// </para>
        /// <para>
        /// <b>Reihenfolge ist hier entscheidend.</b> Erst werden ALLE Praefixe des
        /// Baumes angewandt, danach erst wird abgeschaltet. Andernfalls loeschte der
        /// Durchgang fuer <c>MDIMainForm</c> die Infobuttons von <c>Form_Start</c>,
        /// bevor deren eigene Zeilen ueberhaupt an der Reihe waeren.
        /// </para>
        /// <para>
        /// Der Aufruf ist beliebig oft wiederholbar (idempotent): <see cref="SetHelpKey"/>
        /// loest jede Ereignisbindung vor dem Setzen wieder, und der Schluessel wird
        /// ueberschrieben statt ergaenzt.
        /// </para>
        /// </remarks>
        public void RegisterBaum(Control wurzel, string prefixName)
        {
            if (this.DesignMode || wurzel == null || wurzel.IsDisposed) return;

            var registrierte = new List<Control>();

            ZuordnungenAnwenden(wurzel, prefixName, registrierte);
            UnterPraefixeAnwenden(wurzel, registrierte);

            InfobuttonsOhneZuordnungAbschalten(wurzel, prefixName);
            NachKatalogAuswerten(wurzel, registrierte);
        }

        /// <summary>
        /// Wendet die Zeilen jedes eingebetteten Formulars und UserControls unter
        /// seinem EIGENEN Namen an - genau so, wie es ein handgeschriebenes
        /// <c>RegisterControl(uc, "ucFuelSettings")</c> taete.
        /// </summary>
        private void UnterPraefixeAnwenden(Control behaelter, List<Control> registrierte)
        {
            if (behaelter == null || behaelter.IsDisposed) return;

            foreach (Control kind in behaelter.Controls)
            {
                if (kind == null || kind.IsDisposed) continue;

                if ((kind is Form || kind is UserControl) && !string.IsNullOrEmpty(kind.Name))
                {
                    ZuordnungenAnwenden(kind, kind.Name, registrierte);
                }

                UnterPraefixeAnwenden(kind, registrierte);
            }
        }

        /// <summary>
        /// Wendet alle Zuordnungszeilen an, deren Praefix zu <paramref name="prefixName"/>
        /// passt. Schaltet NICHTS ab - das ist Sache des Aufrufers, damit bei einem
        /// Baum mit mehreren Praefixen erst nach dem letzten Durchgang geurteilt wird.
        /// </summary>
        private void ZuordnungenAnwenden(Control rootContainer, string prefixName, List<Control> registrierte)
        {
            if (rootContainer == null || rootContainer.IsDisposed || string.IsNullOrEmpty(prefixName)) return;

            try
            {
                foreach (string rohzeile in ZuordnungsZeilen())
                {
                    // Ein BOM-Rest wuerde die Kommentarerkennung der ersten Zeile aushebeln.
                    string line = rohzeile == null ? "" : rohzeile.Trim('\uFEFF', ' ', '\t');

                    // Leerzeilen und Kommentare überspringen
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    // Trennen bei '=' (Key und Slug-Paar trennen)
                    int gleich = line.IndexOf('=');
                    if (gleich <= 0) continue; // Ungültige Zeile einfach überspringen!

                    string fullPath = line.Substring(0, gleich).Trim();
                    string wpSlug = line.Substring(gleich + 1).Trim();
                    if (fullPath.Length == 0 || wpSlug.Length == 0) continue;

                    // Ersten Punkt suchen, um den Präfix zu isolieren
                    int firstDot = fullPath.IndexOf('.');
                    if (firstDot <= 0) continue; // Kein Punkt da? Zeile überspringen!

                    string configPrefix = fullPath.Substring(0, firstDot).Trim();
                    string controlPath = fullPath.Substring(firstDot + 1).Trim();

                    // WICHTIG: Nur verarbeiten, wenn diese Zeile GENAU zu dem aktuell gescannten Container gehört!
                    if (!string.Equals(configPrefix, prefixName, StringComparison.OrdinalIgnoreCase)) continue;

                    // Rekursive Suche starten
                    Control targetControl = FindControlRecursive(rootContainer, controlPath);

                    if (targetControl != null)
                    {
                        // Registrieren (überschreibt nichts anderes, fügt nur hinzu)
                        this.SetHelpKey(targetControl, wpSlug);
                        registrierte.Add(targetControl);
                        System.Diagnostics.Debug.WriteLine($"[Help] Erfolgreich registriert: {prefixName}.{controlPath} -> {wpSlug}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Help] WARNUNG: Control '{controlPath}' wurde in '{prefixName}' nicht gefunden.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Falls doch was schiefgeht, Meldung im Debug-Fenster
                System.Diagnostics.Debug.WriteLine("[Help] KRITISCHER FEHLER beim Mapping: " + ex.ToString());
            }
        }

        // -------------------------------------------------------------------
        // Zuordnung laden (F2): Datei neben der EXE uebersteuert die eingebettete
        // Fassung. Fehlt beides, wird das laut protokolliert statt still verschluckt.
        // -------------------------------------------------------------------

        private static string[] ZuordnungsZeilen()
        {
            if (_mappingZeilen != null) return _mappingZeilen;

            lock (_mappingSperre)
            {
                if (_mappingZeilen == null) _mappingZeilen = ZuordnungLaden();
                return _mappingZeilen;
            }
        }

        private static string[] ZuordnungLaden()
        {
            // 1. Fassung neben der EXE hat Vorrang - so lassen sich Zuordnungen
            //    ohne Neubau korrigieren.
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MappingDateiName);
                if (File.Exists(filePath))
                {
                    // Encoding ausdruecklich: sonst entschiede die Systemcodepage
                    // ueber die Umlaute in den Kommentaren.
                    string[] zeilen = File.ReadAllLines(filePath, Encoding.UTF8);
                    System.Diagnostics.Debug.WriteLine($"[Help] Zuordnung aus Datei neben der EXE: {filePath} ({zeilen.Length} Zeilen).");
                    return zeilen;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Help] WARNUNG: Zuordnungsdatei neben der EXE nicht lesbar, weiche auf die eingebettete Fassung aus: " + ex.Message);
            }

            // 2. Eingebettete Fassung - sie wird immer mitgeliefert.
            try
            {
                var assembly = typeof(HelpExtender).Assembly;
                using (Stream stream = assembly.GetManifestResourceStream(MappingRessourceName))
                {
                    if (stream == null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[Help] FEHLER: Eingebettete Zuordnung '{MappingRessourceName}' fehlt. " +
                            "Ist sie in der .csproj als EmbeddedResource mit passendem LogicalName eingetragen? " +
                            "Ohne Zuordnung bleibt jeder Infobutton abgeschaltet.");
                        return Array.Empty<string>();
                    }

                    using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    {
                        var zeilen = new List<string>();
                        string zeile;
                        while ((zeile = reader.ReadLine()) != null) zeilen.Add(zeile);

                        System.Diagnostics.Debug.WriteLine($"[Help] Zuordnung aus eingebetteter Ressource ({zeilen.Count} Zeilen).");
                        return zeilen.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Help] FEHLER beim Lesen der eingebetteten Zuordnung: " + ex.ToString());
                return Array.Empty<string>();
            }
        }

        // -------------------------------------------------------------------
        // F3 - ehrliche Buttons
        // -------------------------------------------------------------------

        /// <summary>
        /// Schaltet jeden Infobutton im Container ab, zu dem keine einzige Zeile
        /// in der Zuordnung passt. Ein grauer Button ist ehrlicher als ein toter.
        /// </summary>
        private void InfobuttonsOhneZuordnungAbschalten(Control container, string prefixName)
        {
            var infobuttons = new List<Control>();
            InfobuttonsSammeln(container, infobuttons);

            foreach (Control ctrl in infobuttons)
            {
                if (_keys.ContainsKey(ctrl)) continue;

                SteuerelementAbschalten(ctrl);
                System.Diagnostics.Debug.WriteLine(
                    $"[Help] WARNUNG: Infobutton '{prefixName}.{ctrl.Name}' hat keine Zeile in {MappingDateiName} - abgeschaltet.");
            }
        }

        private static void InfobuttonsSammeln(Control container, List<Control> treffer)
        {
            if (container == null) return;

            if (!string.IsNullOrEmpty(container.Name) &&
                container.Name.StartsWith(InfobuttonPraefix, StringComparison.OrdinalIgnoreCase))
            {
                treffer.Add(container);
            }

            foreach (Control kind in container.Controls) InfobuttonsSammeln(kind, treffer);
        }

        /// <summary>
        /// Wertet die Katalogtreffer aus - aber erst, wenn der Katalog fertig ist.
        /// MDIMainForm_Load startet LoadAllAsync bewusst ohne await; wer vorher
        /// urteilt, faerbt nach dem Start saemtliche Infobuttons grau.
        /// </summary>
        private void NachKatalogAuswerten(Control rootContainer, List<Control> registrierte)
        {
            if (registrierte.Count == 0) return;

            if (_catalog == null)
            {
                foreach (Control ctrl in registrierte) SteuerelementAbschalten(ctrl);
                return;
            }

            if (_catalog.IsLoaded)
            {
                ZuordnungenPruefen(registrierte);
                return;
            }

            // Keine dauerhafte Ereignisbindung: die Fortsetzung laeuft genau einmal
            // und gibt den Verweis danach frei.
            _catalog.Loaded.ContinueWith(
                _ => AufOberflaeche(rootContainer, () => ZuordnungenPruefen(registrierte)),
                TaskScheduler.Default);
        }

        private void ZuordnungenPruefen(List<Control> kandidaten)
        {
            foreach (Control ctrl in kandidaten)
            {
                if (ctrl == null || ctrl.IsDisposed) continue;
                if (!_keys.TryGetValue(ctrl, out string schluessel)) continue;

                if (string.IsNullOrEmpty(ZielAufloesen(schluessel)))
                {
                    SteuerelementAbschalten(ctrl);
                    System.Diagnostics.Debug.WriteLine(
                        $"[Help] WARNUNG: Zur Zuordnung '{schluessel}' von '{ctrl.Name}' liefert der Katalog nichts - abgeschaltet.");
                }
                else
                {
                    // Der Katalog kennt das Ziel doch - ein voreiliges Abschalten
                    // (leerer Katalog waehrend des Ladelaufs) wird zurueckgenommen.
                    SteuerelementWiederEinschalten(ctrl);
                }
            }
        }

        private void SteuerelementAbschalten(Control ctrl)
        {
            if (ctrl == null || ctrl.IsDisposed) return;
            if (_abgeschaltet.ContainsKey(ctrl)) return;   // schon von uns abgeschaltet

            _abgeschaltet[ctrl] = ctrl.Cursor;

            ctrl.Enabled = false;
            // Der Handzeiger verspraeche weiterhin eine Reaktion.
            ctrl.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Nimmt ein Abschalten zurueck - aber nur ein Abschalten, das VON UNS
        /// stammt. Was die Fachlogik gesperrt hat, bleibt gesperrt.
        /// </summary>
        private void SteuerelementWiederEinschalten(Control ctrl)
        {
            if (ctrl == null || ctrl.IsDisposed) return;
            if (!_abgeschaltet.TryGetValue(ctrl, out Cursor vorher)) return;

            _abgeschaltet.Remove(ctrl);

            ctrl.Enabled = true;
            if (vorher != null) ctrl.Cursor = vorher;
        }

        private static void AufOberflaeche(Control ctrl, Action aktion)
        {
            if (ctrl == null || ctrl.IsDisposed) return;

            try
            {
                // InvokeRequired ist nur dann true, wenn es ueberhaupt ein Fensterhandle
                // gibt und der Aufruf von einem fremden Faden kommt.
                if (ctrl.InvokeRequired)
                {
                    if (ctrl.IsHandleCreated) ctrl.BeginInvoke(aktion);
                }
                else
                {
                    aktion();
                }
            }
            catch (ObjectDisposedException) { /* Formular ist zwischenzeitlich zu */ }
            catch (InvalidOperationException) { /* Handle ist zwischenzeitlich weg */ }
        }

        // -------------------------------------------------------------------
        // F4 - Sprache ueber Slug-Paare, F7 - Slug ODER Pfad
        // -------------------------------------------------------------------

        /// <summary>
        /// Loest ein Zielpaar "de | en" gegen die eingestellte Oberflaechensprache
        /// auf. Fehlt das Ziel der aktiven Sprache im Katalog, greift das andere.
        /// Liefert "", wenn der geladene Katalog keines davon kennt.
        /// </summary>
        /// <remarks>
        /// Jede Haelfte darf ein Slug ("klimadaten") ODER ein Link-Pfad
        /// ("/epos-plan/epos-plan-grundlagen/klimadaten/") sein (F7). Der
        /// Trennstrich '|' kommt in keinem Pfad vor, die Zerlegung bleibt also
        /// unveraendert.
        /// </remarks>
        private string ZielAufloesen(string schluessel)
        {
            if (string.IsNullOrWhiteSpace(schluessel)) return "";

            string[] teile = schluessel.Split('|');
            string zielDe = teile[0].Trim();
            string zielEn = teile.Length > 1 ? teile[1].Trim() : "";

            bool englisch = Program.nLanguage != 0;
            string bevorzugt = englisch ? zielEn : zielDe;
            string ersatz = englisch ? zielDe : zielEn;

            // Ein einzeln angegebenes Ziel gilt fuer beide Sprachen.
            if (string.IsNullOrEmpty(bevorzugt)) bevorzugt = ersatz;

            // Solange der Katalog laedt, wird nichts verworfen.
            if (_catalog == null || !_catalog.IsLoaded) return bevorzugt;

            if (!string.IsNullOrEmpty(bevorzugt) && _catalog.Contains(bevorzugt)) return bevorzugt;
            if (!string.IsNullOrEmpty(ersatz) && _catalog.Contains(ersatz)) return ersatz;

            return "";
        }

        private Control FindControlRecursive(Control container, string remainingPath)
        {
            if (string.IsNullOrEmpty(remainingPath) || container == null) return null;

            int nextDot = remainingPath.IndexOf('.');

            // FALL A: das finale Control (z.B. "btn_Carrier") suchen
            if (nextDot == -1)
            {
                return FindControlByNameDeep(container, remainingPath);
            }

            // FALL B: existiert eine Zwischen-Container? usercontrol? (z.B. "ucCarrierPanel")
            string currentTargetName = remainingPath.Substring(0, nextDot).Trim();
            string tail = remainingPath.Substring(nextDot + 1).Trim();

            Control nextContainer = FindControlByNameDeep(container, currentTargetName);
            if (nextContainer != null)
            {
                // Tiefer graben: im gefundenen Container nach dem Rest des Pfades ("tail") suchen
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

            // Durchsuchen: alle Kinder und Kindeskinder rekursiv
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

        // -------------------------------------------------------------------
        // Anzeige
        // -------------------------------------------------------------------

        /// <summary>
        /// Holt den Katalogeintrag zu einem Steuerelement. Liefert der geladene
        /// Katalog nichts, wird der Button hier nachtraeglich abgeschaltet (F3).
        /// </summary>
        private HelpEntry EintragHolen(Control ctrl, string schluessel)
        {
            if (_catalog == null) return null;

            string ziel = ZielAufloesen(schluessel);
            if (!string.IsNullOrEmpty(ziel))
            {
                HelpEntry entry = _catalog.Get(ziel);
                if (!string.IsNullOrEmpty(entry.Tooltip)) return entry;
            }

            if (_catalog.IsLoaded)
            {
                SteuerelementAbschalten(ctrl);
                System.Diagnostics.Debug.WriteLine(
                    $"[Help] WARNUNG: Zur Zuordnung '{schluessel}' von '{ctrl.Name}' liefert der Katalog nichts - abgeschaltet.");
            }

            return null;
        }

        private void PopupBereitstellen()
        {
            if (_popup == null || _popup.IsDisposed) _popup = new Form_HelpPopup();
        }

        /// <summary>
        /// F1: Der Klick fuehrt. Das Popup bleibt angeheftet stehen, bis der
        /// Anwender es schliesst (Esc) oder woanders hinklickt.
        /// </summary>
        private void Control_Click(object sender, EventArgs e)
        {
            if (this.DesignMode) return;

            Control ctrl = sender as Control;
            if (ctrl == null || !_keys.TryGetValue(ctrl, out string schluessel)) return;

            HelpEntry entry = EintragHolen(ctrl, schluessel);
            if (entry == null) return;

            PopupBereitstellen();
            _popup.ShowHelpAngeheftet(entry.Tooltip, entry.Url, Cursor.Position);
        }

        private void Control_MouseEnter(object sender, EventArgs e)
        {
            if (this.DesignMode) return;

            Control ctrl = sender as Control;
            if (ctrl == null || !_keys.TryGetValue(ctrl, out string schluessel)) return;

            // Ein angeheftetes Popup gehoert dem Anwender - Ueberfahren nimmt es ihm nicht weg.
            if (_popup != null && !_popup.IsDisposed && _popup.IstAngeheftet) return;

            HelpEntry entry = EintragHolen(ctrl, schluessel);
            if (entry == null) return;

            PopupBereitstellen();
            _popup.ShowHelp(entry.Tooltip, entry.Url, Cursor.Position);
        }

        private void Control_MouseLeave(object sender, EventArgs e)
        {
            if (this.DesignMode || _popup == null || _popup.IsDisposed) return;

            // Angeheftet bleibt stehen (F1).
            if (_popup.IstAngeheftet) return;

            Timer delayTimer = new Timer { Interval = 500 };
            delayTimer.Tick += (s, ev) =>
            {
                delayTimer.Stop();
                delayTimer.Dispose();

                if (_popup != null && !_popup.IsDisposed && !_popup.IstAngeheftet
                    && !_popup.Bounds.Contains(Cursor.Position)) _popup.Hide();
            };
            delayTimer.Start();
        }
    }

}
