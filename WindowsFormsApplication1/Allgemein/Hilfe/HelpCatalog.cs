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
        /// Die Kurzform der Seite - seit H1 der Kurzname der Wiki-Unterseite
        /// (der Titelteil hinter "Programm Dokumentation/").
        /// </summary>
        /// <remarks>
        /// Seit F7 ist der Link-Pfad der Schluessel. Der Slug ist nur noch die
        /// bequeme Kurzform - und liesse sich fast immer aus dem letzten
        /// Pfadabschnitt ableiten. Fast: Die alte WordPress-Startseite trug den
        /// Slug "epos-plan" bei Pfad "/". Wer den Slug mitfuehrt, statt ihn zu
        /// raten, verliert diesen Fall nicht. Aeltere Sicherungen ohne dieses
        /// Feld fallen auf den letzten Pfadabschnitt zurueck.
        /// </remarks>
        public string Slug { get; set; } = "";

        /// <summary>
        /// H11 (Entscheid 7.6) - der Einleitungssatz der Wiki-Seite, hoechstens
        /// zwei Saetze, als Klartext ohne Auszeichnung. Das Popup zeigt ihn
        /// unter der Kapitelzeile.
        /// </summary>
        /// <remarks>
        /// <b>Leer ist zulaessig</b> und der Regelfall bei jeder Bezugsquelle
        /// ausser dem Onlineabruf: Der mitgelieferte Startbestand fuehrt das
        /// Feld nicht, aeltere Sicherungen kennen es nicht. Ein fehlendes Feld
        /// deserialisiert zur leeren Zeichenkette - das Popup verhaelt sich dann
        /// exakt wie vor H11. Beschafft wird die Beschreibung nachtraeglich und
        /// nur bei erfolgreichem Onlineabruf
        /// (<see cref="WikiHelpCatalog.BeschreibungenGeladen"/>).
        /// </remarks>
        public string Beschreibung { get; set; } = "";
    }

    /// <summary>
    /// Der Hilfekatalog. Quelle ist seit H1 das Wiki unter
    /// <c>wiki.epos-plan.de</c> - genauer die Rubrik "Programm Dokumentation"
    /// und ihre Unterseiten (A1 des Konzepts Hilfesystem/Wikidokumentation).
    /// </summary>
    /// <remarks>
    /// Hiess bis H1 <c>WordPressHelpCatalog</c>. Die Umbenennung ist rein
    /// namentlich - Aufloesung, Rangfolge der Bezugsquellen und Sicherung sind
    /// unveraendert; nur der Ladeweg ist von der WordPress-REST-Form auf die
    /// MediaWiki-Action-API gewechselt.
    /// </remarks>
    public class WikiHelpCatalog
    {
        /// <summary>
        /// Der eine Hilfekatalog des laufenden Programms.
        ///
        /// <para><b>Wozu.</b> Bis iU5 lag er als <c>Program.HelpCatalog</c> im
        /// WinForms-Einstiegspunkt; Kern-naher Programmtext, der einen Hilfetext
        /// nachschlagen wollte (<c>KiAktionenDialog</c>), kam nur ueber <c>Program</c>
        /// dorthin. Die Anmeldung hier ist dasselbe Hausmuster wie
        /// <c>WizardCtrl.Aktueller</c>: EIN statischer Halter, gesetzt von
        /// <c>Program.Main</c>; <c>Program.HelpCatalog</c> ist seither nur noch die
        /// Weiterleitung fuer die Masken.</para>
        ///
        /// <para><c>null</c> ist ein zulaessiger Zustand — im Aktionsharnisch und in
        /// Prueflaeufen gibt es keinen Katalog. Ein fehlender Hilfetext ist ein
        /// Schoenheitsfehler und kein Grund, eine Erklaerung scheitern zu lassen.</para>
        /// </summary>
        public static WikiHelpCatalog Aktueller { get; set; }

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

        /// <summary>
        /// A1 - Geltungsbereich des Katalogs: die Rubrik "Programm Dokumentation"
        /// und ausschliesslich ihre Unterseiten. Die Rubrikseite selbst traegt
        /// keinen Schraegstrich und bleibt damit automatisch aussen vor.
        /// </summary>
        private const string RubrikPraefix = "Programm Dokumentation/";

        private string _baseUrl;

        public WikiHelpCatalog(string baseUrl) => _baseUrl = (baseUrl ?? "").TrimEnd('/');

        /// <summary>Anzahl der bekannten Hilfeseiten.</summary>
        public int SeitenAnzahl => _nachPfad.Count;

        // Meldet, wann der Ladelauf durch ist. Hauptfensterrahmen.BeimLaden ruft LoadAllAsync
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
        // H11 (7.6) - Popup-Kurzbeschreibungen
        // -------------------------------------------------------------------

        /// <summary>
        /// Meldet, wann der NACHGELAGERTE Lauf fuer die Kurzbeschreibungen durch
        /// ist. Getrennt von <see cref="Loaded"/>, weil der Katalog schon vorher
        /// vollstaendig benutzbar ist - siehe <see cref="LoadAllCoreAsync"/>.
        /// </summary>
        private readonly TaskCompletionSource<bool> _beschreibungen =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Wurde der Nachladelauf ueberhaupt angestossen?</summary>
        private bool _beschreibungenGestartet;

        /// <summary>
        /// Wird abgeschlossen, sobald die Kurzbeschreibungen beschafft sind -
        /// auch dann, wenn es keine gibt (offline, Serverfehler, kein
        /// Onlineabruf). Wer darauf wartet, wartet also nie vergeblich.
        /// </summary>
        public Task BeschreibungenGeladen => _beschreibungen.Task;

        /// <summary>Wie viele Seiten des Katalogs eine Kurzbeschreibung tragen.</summary>
        public int BeschreibungenAnzahl
        {
            get
            {
                int anzahl = 0;
                foreach (HelpEntry eintrag in _nachPfad.Values)
                    if (!string.IsNullOrWhiteSpace(eintrag.Beschreibung)) anzahl++;

                return anzahl;
            }
        }

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

            // A3 - Anker gehoeren NICHT in die Aufloesung. Der Pfadweg schneidet
            // sie in PfadNormalisieren ohnehin ab; der Slug-Weg braucht denselben
            // Schnitt, sonst suchte "Pufferspeicher#ladung" als ganzer Slug.
            int raute = schluessel.IndexOf('#');
            if (raute >= 0) schluessel = schluessel.Substring(0, raute).Trim();
            if (schluessel.Length == 0) return null;

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

        private HelpEntry UeberSlug(string angabe)
        {
            // H1 - EIN Normalisierungsweg fuer beide Seiten: der Katalog legt den
            // Kurznamen so ab (EintragAufnehmen), das Mapping-Ziel wird hier
            // genauso behandelt. Nur so trifft "Wärmebedarf" aus help_mapping.txt
            // den Kurznamen der Wiki-Unterseite.
            string slug = SlugNormalisieren(angabe);
            if (slug.Length == 0) return null;

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

        /// <summary>
        /// Einheitliche Schreibform einer Kurzform (Slug bzw. Kurzname der
        /// Wiki-Unterseite). Katalogseite UND Mapping-Ziel laufen beide
        /// hierdurch - das ist die Zusage aus H1/H2: gleiche Normalisierung auf
        /// beiden Seiten des Abgleichs.
        /// </summary>
        /// <remarks>
        /// Bewusst dieselbe Kleinschreibung wie in <see cref="PfadNormalisieren"/>
        /// (<c>ToLowerInvariant</c>). Umlaute bleiben Umlaute: der Kurzname
        /// "Wärmebedarf" wird zu "wärmebedarf", nicht zu "waermebedarf" - die
        /// Zuordnungsdatei traegt ihn genauso.
        /// </remarks>
        public static string SlugNormalisieren(string slug)
        {
            return string.IsNullOrWhiteSpace(slug) ? "" : slug.Trim().ToLowerInvariant();
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
                ? SlugNormalisieren(SlugAusPfad(pfad))
                : SlugNormalisieren(eintrag.Slug);
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

            // H11: Lief der Nachladelauf der Kurzbeschreibungen gar nicht erst an
            // (offline, Serverfehler, leere Rubrik), meldet er sich auch nie
            // fertig. Dann wird hier abgemeldet - sonst wartet der Aufrufer von
            // BeschreibungenGeladen bis in alle Ewigkeit.
            if (!_beschreibungenGestartet) _beschreibungen.TrySetResult(true);
        }
    }

    private async Task LoadAllCoreAsync()
    {
        // Pfad für die lokale Sicherung im AppData-Verzeichnis
        string localBackupPath = SicherungsPfad();
        string appDataFolder = Path.GetDirectoryName(localBackupPath);

        bool onlineLoadSuccessful = false;
        string apcontinue = null;

        // Temporärer Cache, um bei Fehlern den alten Cache nicht unvollständig zu überschreiben
        var tempCache = new System.Collections.Generic.Dictionary<string, HelpEntry>();

        // -------------------------------------------------------------------
        // A1 (H1) - MediaWiki-Action-API statt WordPress-REST
        //
        //   {_baseUrl}/api.php?action=query&list=allpages
        //             &apprefix=Programm%20Dokumentation%2F&aplimit=500&format=json
        //
        //   └────┬────┘└──┬───┘└─────┬────┘└──────┬──────┘
        //   Wiki-Basis  Action-API  Seitenliste  Geltungsbereich = die Rubrik
        //
        // Der frueher benutzte WordPress-Weg (/rest.php/v1/{Prefix}?per_page=…)
        // beantwortet MediaWiki mit HTTP 404 - der Katalog fiel dadurch still auf
        // den eingebetteten Startbestand zurueck. Die Einstellung
        // "WordPressPrefix" wird seit H1 nirgends mehr gelesen (Entscheid 7.3).
        //
        // Antwortform: { "query": { "allpages": [ { "title": "…" }, … ] },
        //                "continue": { "apcontinue": "…" } }
        // Fortsetzung, solange ein apcontinue gemeldet wird.
        // -------------------------------------------------------------------
        while (true)
        {
            string url = $"{_baseUrl}/api.php?action=query&list=allpages" +
                         $"&apprefix={Uri.EscapeDataString(RubrikPraefix)}" +
                         "&aplimit=500&format=json";

            if (!string.IsNullOrEmpty(apcontinue))
                url += "&apcontinue=" + Uri.EscapeDataString(apcontinue);

            try
            {
                // Timeout schützt vor ewigem Hängen bei schlechter Verbindung
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));

                var response = await _http.GetAsync(url, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    onlineLoadSuccessful = false;
                    break;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                    !doc.RootElement.TryGetProperty("query", out JsonElement abfrage) ||
                    !abfrage.TryGetProperty("allpages", out JsonElement seiten) ||
                    seiten.ValueKind != JsonValueKind.Array)
                {
                    // Keine verwertbare Antwort - Rueckfall auf Sicherung/Beilage.
                    onlineLoadSuccessful = false;
                    break;
                }

                foreach (var seite in seiten.EnumerateArray())
                {
                    if (seite.ValueKind != JsonValueKind.Object) continue;
                    if (!seite.TryGetProperty("title", out JsonElement titelFeld)) continue;

                    HelpEntry eintrag = EintragAusTitel(titelFeld.GetString() ?? "");
                    if (eintrag == null) continue;

                    // F7: Geschluesselt wird ueber den Link-Pfad, nicht ueber den
                    // Kurznamen - der Pfad ist je Seite eindeutig.
                    string schluessel = PfadNormalisieren(eintrag.Url);
                    if (schluessel.Length == 0) continue;
                    if (tempCache.ContainsKey(schluessel)) continue;    // echte Dublette

                    tempCache[schluessel] = eintrag;
                }

                onlineLoadSuccessful = true;

                string weiter = null;
                if (doc.RootElement.TryGetProperty("continue", out JsonElement fortsetzung) &&
                    fortsetzung.ValueKind == JsonValueKind.Object &&
                    fortsetzung.TryGetProperty("apcontinue", out JsonElement apc))
                {
                    weiter = apc.GetString();
                }

                if (string.IsNullOrEmpty(weiter)) break;

                // Sicherheitsnetz gegen einen Server, der sich im Kreis dreht.
                if (string.Equals(weiter, apcontinue, StringComparison.Ordinal)) break;

                apcontinue = weiter;
            }
            catch (Exception)
            {
                // Netzwerkfehler oder Server-Timeout -> Schleife abbrechen und Fallback nutzen
                onlineLoadSuccessful = false;
                break;
            }
        }

        // ---------------------------------------------------------------
        // Rangfolge (F6): Online > lokale Sicherung > mitgelieferter Startbestand
        // ---------------------------------------------------------------

        if (onlineLoadSuccessful && tempCache.Count == 0)
        {
            // Reihenfolge-Zwang aus dem Konzept (Abschnitt 8/11): Die Rubrik ist
            // erreichbar, aber noch leer - die Hilfeseiten (H3) sind noch nicht
            // angelegt. Der Abruf gilt als gescheitert, damit Sicherung bzw.
            // Startbestand greifen; ohne diese Zeile bliebe das unsichtbar.
            System.Diagnostics.Debug.WriteLine(
                $"[Help] WARNUNG: Die Wiki-Rubrik '{RubrikPraefix}' antwortet, fuehrt aber KEINE " +
                "Unterseiten. Die Hilfeseiten sind im Wiki noch nicht angelegt (Paket H3). " +
                "Es greift die lokale Sicherung bzw. der mitgelieferte Startbestand.");
        }

        // FALL 1: Online-Abruf war erfolgreich -> Register aufbauen und lokal sichern
        if (onlineLoadSuccessful && tempCache.Count > 0)
        {
            // H11 (7.6): Die Beschreibungen des bisherigen Bestandes retten. Der
            // frische tempCache kennt nur Titel und Adresse; ohne diese Zeilen
            // waeren die Popup-Kurzbeschreibungen zwischen Katalogaufbau und
            // Ende des Nachladelaufs kurzzeitig wieder weg.
            foreach (var paar in tempCache)
            {
                if (_nachPfad.TryGetValue(paar.Key, out HelpEntry alt) &&
                    !string.IsNullOrEmpty(alt.Beschreibung))
                {
                    paar.Value.Beschreibung = alt.Beschreibung;
                }
            }

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

            // H11 (7.6) - Kurzbeschreibungen NACHTRAEGLICH und ohne await.
            //
            // Der Katalog ist an dieser Stelle vollstaendig und benutzbar; die
            // Beschreibungen sind Zierrat. Wuerde hier gewartet, verzoegerten
            // zwei weitere HTTP-Abrufe den Zeitpunkt, ab dem "IsLoaded" gilt -
            // und damit den Moment, in dem die Infobuttons ihr Ziel kennen (F3).
            // Wer auf die Beschreibungen warten muss (Pruefstand), nimmt
            // "BeschreibungenGeladen".
            _beschreibungenGestartet = true;
            _ = BeschreibungenNachladenAsync(tempCache, localBackupPath);

            return;
        }

        // FALL 2: Offline oder Serverfehler -> lokale Sicherung.
        if (LokaleSicherungLaden(localBackupPath)) return;

        // FALL 3: Auch die fehlt -> mitgelieferter Startbestand (F6).
        MitgelieferterStartbestandLaden();
    }

    // =======================================================================
    //  H11 (Entscheid 7.6) - Kurzbeschreibungen fuer das Popup
    // =======================================================================

    /// <summary>
    /// Wie viele Seitentitel hoechstens in EINE Auszugsanfrage gehen.
    /// </summary>
    /// <remarks>
    /// <b>Empirisch bestimmt am 29.08.2026 gegen wiki.epos-plan.de.</b> Der
    /// Auszugs-Dienst deckelt die Zahl der gelieferten Auszuege je Anfrage; der
    /// Deckel haengt an der Betriebsart:
    /// <list type="bullet">
    /// <item>Volltext (ohne <c>exintro</c>): 1 Auszug je Anfrage - der Grund,
    ///       warum <see cref="WikiWissen"/> seine Seiten einzeln holt.</item>
    /// <item>Einleitung (<c>exintro=1</c>): 20 Auszuege je Anfrage. Gemessen mit
    ///       29 Titeln: die Antwort trug 20 Auszuege und meldete
    ///       <c>"continue":{"excontinue":20}</c>.</item>
    /// </list>
    /// Deshalb wird gestueckelt statt fortgesetzt: Bei 32 Rubrikseiten sind das
    /// zwei Anfragen, und keine Antwort kann stillschweigend beschnitten sein.
    /// </remarks>
    private const int ExtraktStapel = 20;

    /// <summary>
    /// Holt je Rubrikseite den Einleitungssatz nach und schreibt ihn in die
    /// bereits im Katalog haengenden Eintraege. Bester Wille, kein Muss: Jeder
    /// Fehler laesst die Beschreibungen leer, und das Popup verhaelt sich dann
    /// exakt wie vor H11.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Abrufform (MediaWiki-Action-API):
    /// <c>api.php?action=query&amp;prop=extracts&amp;exintro=1&amp;explaintext=1
    /// &amp;exsentences=2&amp;titles=A|B|…&amp;format=json&amp;redirects=1</c>
    /// </para>
    /// <para>
    /// <b>Warum die Eintraege genuegen und kein neuer Index noetig ist:</b>
    /// <see cref="IndizesAufbauen"/> legt die uebergebenen
    /// <see cref="HelpEntry"/>-Objekte selbst ab, nicht Kopien davon. Wer hier
    /// <c>Beschreibung</c> setzt, aendert damit denselben Eintrag, den
    /// <see cref="Get"/> spaeter herausgibt.
    /// </para>
    /// <para>
    /// Den Wiki-Titel traegt der Eintrag nicht mit - er ist aber eindeutig
    /// rekonstruierbar: <see cref="RubrikPraefix"/> plus Kurzname, und der
    /// Kurzname steht im <c>Slug</c> (siehe <see cref="EintragAusTitel"/>).
    /// </para>
    /// </remarks>
    private async Task BeschreibungenNachladenAsync(
        Dictionary<string, HelpEntry> eintraege, string sicherungsPfad)
    {
        int getroffen = 0;

        try
        {
            // Titel je Eintrag bilden; Zuordnung ueber die Kleinschreibform,
            // weil das Wiki den Titel normalisiert zurueckgibt.
            var nachTitel = new Dictionary<string, HelpEntry>(StringComparer.OrdinalIgnoreCase);
            var titel = new List<string>();

            foreach (HelpEntry eintrag in eintraege.Values)
            {
                if (eintrag == null) continue;

                string kurzname = (eintrag.Slug ?? "").Trim();
                if (kurzname.Length == 0) continue;

                string voll = RubrikPraefix + kurzname;
                if (nachTitel.ContainsKey(voll)) continue;

                nachTitel[voll] = eintrag;
                titel.Add(voll);
            }

            if (titel.Count == 0) return;

            for (int start = 0; start < titel.Count; start += ExtraktStapel)
            {
                var stapel = titel.GetRange(start, Math.Min(ExtraktStapel, titel.Count - start));
                getroffen += await StapelBeschreibungenAsync(stapel, nachTitel);
            }

            if (getroffen == 0) return;

            // Die Sicherung wurde bereits ohne Beschreibungen geschrieben - jetzt
            // noch einmal, damit der naechste Start OHNE Netz die Saetze kennt.
            // Der Aufbau bleibt derselbe; ein alter Bestand ohne dieses Feld
            // liest sich unveraendert (fehlend = leer).
            try
            {
                File.WriteAllText(sicherungsPfad, JsonSerializer.Serialize(eintraege));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Help] Beschreibungen nicht gesichert: " + ex.Message);
            }

            System.Diagnostics.Debug.WriteLine(
                $"[Help] Kurzbeschreibungen: {getroffen} von {titel.Count} Seiten (7.6).");
        }
        catch (Exception ex)
        {
            // Ohne await gestartet - eine entkommene Ausnahme waere unbeobachtet.
            System.Diagnostics.Debug.WriteLine(
                "[Help] WARNUNG: Kurzbeschreibungen nicht ladbar: " + ex.Message);
        }
        finally
        {
            _beschreibungen.TrySetResult(true);
        }
    }

    /// <summary>
    /// Ein Stapel Titel in einer Anfrage. Liefert, wie viele Beschreibungen
    /// gesetzt wurden.
    /// </summary>
    private async Task<int> StapelBeschreibungenAsync(
        List<string> stapel, Dictionary<string, HelpEntry> nachTitel)
    {
        string url = $"{_baseUrl}/api.php?action=query&prop=extracts" +
                     "&exintro=1&explaintext=1&exsentences=2" +
                     $"&titles={Uri.EscapeDataString(string.Join("|", stapel))}" +
                     "&format=json&redirects=1";

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));

            var response = await _http.GetAsync(url, cts.Token);
            if (!response.IsSuccessStatusCode) return 0;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty("query", out JsonElement abfrage) ||
                !abfrage.TryGetProperty("pages", out JsonElement seiten) ||
                seiten.ValueKind != JsonValueKind.Object)
            {
                return 0;
            }

            int gesetzt = 0;

            // Die Seiten stehen als Objekt, geschluesselt ueber die Seitennummer;
            // unbekannte Titel tragen eine negative Nummer und ein "missing".
            foreach (JsonProperty seite in seiten.EnumerateObject())
            {
                if (seite.Value.ValueKind != JsonValueKind.Object) continue;
                if (!seite.Value.TryGetProperty("title", out JsonElement titelFeld)) continue;
                if (!seite.Value.TryGetProperty("extract", out JsonElement auszug)) continue;

                string satz = TextSaeubern(auszug.GetString());
                if (satz.Length == 0) continue;

                if (!nachTitel.TryGetValue(titelFeld.GetString() ?? "", out HelpEntry eintrag)) continue;

                eintrag.Beschreibung = satz;
                gesetzt++;
            }

            return gesetzt;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Help] Auszugsabruf gescheitert ({stapel.Count} Titel): {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Macht aus dem Auszug eine einzeilige Kurzbeschreibung: Zeilenumbrueche
    /// und Mehrfachleerzeichen werden zu einem Leerzeichen.
    /// </summary>
    /// <remarks>
    /// <c>explaintext=1</c> liefert bereits Klartext ohne Auszeichnung; was
    /// bleibt, sind Absatzumbrueche. Das Popup bricht selbst um (auf ~70
    /// Zeichen) - ein mitgelieferter Umbruch wuerde diese Rechnung stoeren.
    /// </remarks>
    private static string TextSaeubern(string roh)
    {
        if (string.IsNullOrWhiteSpace(roh)) return "";

        return Regex.Replace(roh, @"\s+", " ").Trim();
    }

    /// <summary>
    /// A1 - Katalogeintrag zu einem Wiki-Seitentitel der Rubrik. Liefert
    /// <c>null</c>, wenn der Titel nicht zur Rubrik gehoert oder keinen
    /// Kurznamen traegt (die Rubrikseite selbst).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Tooltip</c> und <c>Slug</c> sind der Kurzname - der Titelteil hinter
    /// "Programm Dokumentation/". Das Popup zeigt ihn als Kapitelnamen, die
    /// Zuordnungsdatei spricht ihn als Ziel an.
    /// </para>
    /// <para>
    /// <b>Beide Wege strikt getrennt halten</b> (Fallstrick "Titel-Kodierung"):
    /// Geoeffnet wird die URL-kodierte Originaladresse, abgeglichen wird ueber
    /// die dekodierte Kleinschreibform (<see cref="PfadNormalisieren"/> bzw.
    /// <see cref="SlugNormalisieren"/>).
    /// </para>
    /// </remarks>
    private HelpEntry EintragAusTitel(string titel)
    {
        if (string.IsNullOrWhiteSpace(titel)) return null;
        if (!titel.StartsWith(RubrikPraefix, StringComparison.OrdinalIgnoreCase)) return null;

        string kurzname = titel.Substring(RubrikPraefix.Length).Trim();
        if (kurzname.Length == 0) return null;

        return new HelpEntry
        {
            Tooltip = kurzname,
            Url = SeitenUrl(titel),
            Slug = kurzname
        };
    }

    /// <summary>
    /// Artikeladresse zu einem Wiki-Seitentitel. Leerzeichen werden VOR der
    /// Kodierung zu Unterstrichen, die Schraegstriche der Unterseiten bleiben
    /// stehen:
    /// <c>"Programm Dokumentation/Wärmebedarf"</c> ->
    /// <c>".../wiki/Programm_Dokumentation/W%C3%A4rmebedarf"</c>.
    /// </summary>
    private string SeitenUrl(string titel)
    {
        string[] teile = (titel ?? "").Replace(' ', '_').Split('/');
        for (int i = 0; i < teile.Length; i++) teile[i] = Uri.EscapeDataString(teile[i]);

        return $"{_baseUrl}/wiki/{string.Join("/", teile)}";
    }

    /// <summary>
    /// Belegt den Katalog VOR dem Onlineabruf, damit er nie leer ist.
    /// </summary>
    /// <remarks>
    /// Entschaerft den Startwettlauf: <c>Hauptfensterrahmen.BeimLaden</c> stoesst
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

    /// <summary>
    /// Ablageort der lokalen Sicherung im AppData-Verzeichnis.
    ///
    /// <para>Das ist <c>%APPDATA%\&lt;Produktname&gt;</c> und damit ein ANDERER Ordner
    /// als <c>%APPDATA%\wp-plan</c>, unter dem Lizenz und KI-Schluessel liegen. Der
    /// Unterschied ist gewachsen und bleibt: <c>Dienste.Pfade.Produktdaten</c> bildet
    /// zeichengleich, was bisher <c>Application.ProductName ?? "WP-Plan"</c> ergab.</para>
    /// </summary>
    private static string SicherungsPfad()
    {
        return Dienste.Pfade.Verbinde(Dienste.Pfade.Produktdaten, StartbestandDateiName);
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
            using (Stream stream = typeof(WikiHelpCatalog).Assembly
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

        private readonly WikiHelpCatalog _catalog;

        private Form_HelpPopup _popup;

        public HelpExtender(WikiHelpCatalog catalog) => _catalog = catalog;

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
        /// UserControls angewandt. <c>Hauptfensterrahmen</c> traegt <c>Form_Start</c> als
        /// eingebettetes Formular (<c>TopLevel=false</c>), und <c>Form_KostenKomponente</c>
        /// haengt <c>ucFuelSettings</c> zur Laufzeit ein - beide sollen ihre eigenen
        /// Zeilen aus <c>help_mapping.txt</c> bekommen, ohne dass jemand dafuer Code
        /// in ihr Formular schreiben muss.
        /// </para>
        /// <para>
        /// <b>Reihenfolge ist hier entscheidend.</b> Erst werden ALLE Praefixe des
        /// Baumes angewandt, danach erst wird abgeschaltet. Andernfalls loeschte der
        /// Durchgang fuer <c>Hauptfensterrahmen</c> die Infobuttons von <c>Form_Start</c>,
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

        /// <summary>
        /// Baut die Zuordnungszeilen: eingebettete Fassung als Grundlage, die
        /// Datei neben der EXE als Auflage darueber (F2).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Auflage statt Ersatz</b> (Befund 29.08.2026). Bis dahin ERSETZTE eine
        /// Datei neben der EXE die eingebettete Fassung vollstaendig. Eine
        /// unvollstaendige oder veraltete Datei loeschte damit stillschweigend alle
        /// Zuordnungen, die sie selbst nicht nennt - und
        /// <see cref="InfobuttonsOhneZuordnungAbschalten"/> faerbte die zugehoerigen
        /// Infobuttons grau. Genau das ist passiert: eine 464 Byte grosse Restdatei
        /// im Ausgabeordner (aus der Zeit, als die Zuordnung noch mitkopiert wurde)
        /// nannte 6 der 26 Zeilen; 24 von 26 Infobuttons waren daraufhin
        /// abgeschaltet.
        /// </para>
        /// <para>
        /// Die Absicht von F2 - "Zuordnungen ohne Neubau korrigieren" - bleibt
        /// vollstaendig erhalten: Die Zeilen der Datei stehen HINTER den
        /// eingebetteten, und <see cref="ZuordnungenAnwenden"/> wendet jede
        /// passende Zeile an. Da <see cref="SetHelpKey"/> den Schluessel
        /// ueberschreibt statt ihn zu ergaenzen, gewinnt die zuletzt gelesene
        /// Zeile - also die aus der Datei neben der EXE. Was die Datei NICHT
        /// nennt, bleibt jetzt aber in Kraft, statt zu verschwinden.
        /// </para>
        /// </remarks>
        private static string[] ZuordnungLaden()
        {
            string[] eingebettet = ZuordnungEingebettetLaden();
            string[] daneben = ZuordnungNebenExeLaden();

            if (daneben.Length == 0) return eingebettet;
            if (eingebettet.Length == 0) return daneben;

            var zusammen = new List<string>(eingebettet.Length + daneben.Length);
            zusammen.AddRange(eingebettet);
            zusammen.AddRange(daneben);   // spaeter gelesen = hat Vorrang

            System.Diagnostics.Debug.WriteLine(
                $"[Help] Zuordnung: {eingebettet.Length} eingebettete Zeilen, darueber " +
                $"{daneben.Length} Zeilen aus der Datei neben der EXE. Die Datei uebersteuert " +
                "die Zeilen, die sie nennt; alle uebrigen bleiben in Kraft.");

            return zusammen.ToArray();
        }

        /// <summary>
        /// Fassung neben der EXE - so lassen sich Zuordnungen ohne Neubau
        /// korrigieren. Fehlt sie (Regelfall), ist das kein Fehler.
        /// </summary>
        private static string[] ZuordnungNebenExeLaden()
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MappingDateiName);
                if (!File.Exists(filePath)) return Array.Empty<string>();

                // Encoding ausdruecklich: sonst entschiede die Systemcodepage
                // ueber die Umlaute in den Kommentaren.
                string[] zeilen = File.ReadAllLines(filePath, Encoding.UTF8);
                System.Diagnostics.Debug.WriteLine($"[Help] Zuordnung aus Datei neben der EXE: {filePath} ({zeilen.Length} Zeilen).");
                return zeilen;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Help] WARNUNG: Zuordnungsdatei neben der EXE nicht lesbar, es gilt allein die eingebettete Fassung: " + ex.Message);
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Eingebettete Fassung - sie wird immer mitgeliefert und ist seit dem
        /// Befund vom 29.08.2026 die Grundlage, die nie ganz wegfallen kann.
        /// </summary>
        private static string[] ZuordnungEingebettetLaden()
        {
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

            if (IstInfobutton(container)) treffer.Add(container);

            foreach (Control kind in container.Controls) InfobuttonsSammeln(kind, treffer);
        }

        /// <summary>
        /// Traegt das Steuerelement die Infobutton-Namenskonvention
        /// (<see cref="InfobuttonPraefix"/>)?
        /// </summary>
        /// <remarks>
        /// <b>H12 - die Trennlinie der Abschaltlogik.</b> Seit der feldgenauen
        /// Hilfe traegt <c>help_mapping.txt</c> auch Zeilen fuer EINGABEBEREICHE
        /// (GroupBox, Panel, Beschriftung). Diese Steuerelemente gehoeren der
        /// Fachlogik - das Hilfesystem darf sie unter keinen Umstaenden
        /// abschalten oder optisch veraendern. Nur der Infobutton ist ein
        /// Steuerelement DES HILFESYSTEMS; nur er wird grau, wenn er nichts
        /// anzuzeigen hat.
        /// </remarks>
        private static bool IstInfobutton(Control ctrl)
        {
            return ctrl != null
                && !string.IsNullOrEmpty(ctrl.Name)
                && ctrl.Name.StartsWith(InfobuttonPraefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Was ein nicht aufloesbares Ziel fuer dieses Steuerelement bedeutet -
        /// nur zur Protokollzeile.
        /// </summary>
        private static string Wirkung(Control ctrl)
        {
            return IstInfobutton(ctrl) ? "abgeschaltet" : "Feldhilfe bleibt still (H12)";
        }

        /// <summary>
        /// Wertet die Katalogtreffer aus - aber erst, wenn der Katalog fertig ist.
        /// Hauptfensterrahmen.BeimLaden startet LoadAllAsync bewusst ohne await; wer vorher
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
                        $"[Help] WARNUNG: Zur Zuordnung '{schluessel}' von '{ctrl.Name}' liefert der Katalog nichts - {Wirkung(ctrl)}.");
                }
                else
                {
                    // Der Katalog kennt das Ziel doch - ein voreiliges Abschalten
                    // (leerer Katalog waehrend des Ladelaufs) wird zurueckgenommen.
                    SteuerelementWiederEinschalten(ctrl);
                }
            }
        }

        /// <summary>
        /// Schaltet einen Infobutton ab, der nichts anzuzeigen hat (F3).
        /// </summary>
        /// <remarks>
        /// <b>H12 - die Sperre gilt AUSSCHLIESSLICH fuer Infobuttons.</b> Mit der
        /// feldgenauen Hilfe stehen in <c>help_mapping.txt</c> auch Zeilen fuer
        /// Eingabebereiche. Ohne diese Weiche traefe die Abschaltlogik ueber
        /// <see cref="ZuordnungenPruefen"/> und <see cref="EintragHolen"/> auch
        /// sie - und ein <c>GroupBox.Enabled = false</c> nimmt in WinForms JEDEM
        /// Steuerelement darin die Bedienbarkeit mit. Eine umbenannte oder noch
        /// fehlende Wiki-Seite legte so den halben Dialog lahm. Ein Feld, dessen
        /// Ziel der Katalog nicht kennt, bleibt deshalb einfach still: kein
        /// Popup, aber auch kein Eingriff in Bedienbarkeit oder Aussehen.
        /// </remarks>
        private void SteuerelementAbschalten(Control ctrl)
        {
            if (ctrl == null || ctrl.IsDisposed) return;

            if (!IstInfobutton(ctrl))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Help] '{ctrl.Name}' ist kein Infobutton, sondern ein zugeordneter " +
                    "Eingabebereich (H12) - er bleibt unveraendert bedienbar, die Feldhilfe bleibt still.");
                return;
            }

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
        private string ZielAufloesen(string schluessel) => ZielAufloesen(schluessel, out _);

        /// <summary>
        /// Wie <see cref="ZielAufloesen(string)"/>, meldet zusaetzlich den
        /// Sprungmarken-Anteil des gewaehlten Ziels (A3).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Jede Haelfte darf eine Kurzform ("Pufferspeicher") ODER ein Link-Pfad
        /// ("/wiki/Programm_Dokumentation/Pufferspeicher") sein (F7). Der
        /// Trennstrich '|' kommt in keinem Pfad vor, die Zerlegung bleibt also
        /// unveraendert. Seit H2 traegt die Zuordnungsdatei je Zeile nur noch
        /// EIN Ziel; die Zerlegung bleibt trotzdem stehen, damit aeltere
        /// Zuordnungsdateien neben der EXE weiterhin verstanden werden.
        /// </para>
        /// <para>
        /// <b>A3 - Anker-Durchlass.</b> Ein Ziel darf "Ziel#anker" lauten. Der
        /// Anker wird VOR der Katalogaufloesung abgetrennt (sonst suchte der
        /// Katalog nach einer Seite namens "Pufferspeicher#ladung") und beim
        /// Oeffnen wieder an die aufgeloeste Adresse gehaengt
        /// (<see cref="MitAnker"/>).
        /// </para>
        /// </remarks>
        private string ZielAufloesen(string schluessel, out string anker)
        {
            anker = "";
            if (string.IsNullOrWhiteSpace(schluessel)) return "";

            string[] teile = schluessel.Split('|');

            string zielDe = AnkerAbtrennen(teile[0], out string ankerDe);
            string zielEn = "";
            string ankerEn = "";
            if (teile.Length > 1) zielEn = AnkerAbtrennen(teile[1], out ankerEn);

            bool englisch = Dienste.Sprache.IstEnglisch;
            string bevorzugt = englisch ? zielEn : zielDe;
            string bevorzugtAnker = englisch ? ankerEn : ankerDe;
            string ersatz = englisch ? zielDe : zielEn;
            string ersatzAnker = englisch ? ankerDe : ankerEn;

            // Ein einzeln angegebenes Ziel gilt fuer beide Sprachen.
            if (string.IsNullOrEmpty(bevorzugt))
            {
                bevorzugt = ersatz;
                bevorzugtAnker = ersatzAnker;
            }

            // Solange der Katalog laedt, wird nichts verworfen.
            if (_catalog == null || !_catalog.IsLoaded)
            {
                anker = bevorzugtAnker;
                return bevorzugt;
            }

            if (!string.IsNullOrEmpty(bevorzugt) && _catalog.Contains(bevorzugt))
            {
                anker = bevorzugtAnker;
                return bevorzugt;
            }

            if (!string.IsNullOrEmpty(ersatz) && _catalog.Contains(ersatz))
            {
                anker = ersatzAnker;
                return ersatz;
            }

            return "";
        }

        /// <summary>
        /// Zerlegt "Ziel#anker". Ohne '#' bleibt der Anker leer; das Ziel ist
        /// dann unveraendert die getrimmte Angabe.
        /// </summary>
        private static string AnkerAbtrennen(string ziel, out string anker)
        {
            anker = "";
            if (ziel == null) return "";

            string wert = ziel.Trim();

            int raute = wert.IndexOf('#');
            if (raute < 0) return wert;

            anker = wert.Substring(raute + 1).Trim();
            return wert.Substring(0, raute).Trim();
        }

        /// <summary>
        /// Haengt den Anker aus der Zuordnungszeile an die aufgeloeste Adresse.
        /// Der Katalogeintrag selbst bleibt unberuehrt - er wird kopiert, damit
        /// der gemeinsam genutzte Katalog nicht je Formular verschmutzt wird.
        /// </summary>
        private static HelpEntry MitAnker(HelpEntry eintrag, string anker)
        {
            if (eintrag == null || string.IsNullOrEmpty(anker)) return eintrag;
            if (string.IsNullOrEmpty(eintrag.Url)) return eintrag;
            if (eintrag.Url.IndexOf('#') >= 0) return eintrag;   // die URL traegt schon einen

            return new HelpEntry
            {
                Tooltip = eintrag.Tooltip,
                Url = eintrag.Url + "#" + anker,
                Slug = eintrag.Slug,
                Beschreibung = eintrag.Beschreibung   // H11: die Kopie darf sie nicht verlieren
            };
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
        /// Tiefensuche über alle Ebenen - aber nur im EIGENEN Zuständigkeitsbereich
        /// der Maske. An einem eingebetteten <see cref="Form"/> oder
        /// <see cref="UserControl"/> endet sie.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>H11 - warum die Grenze nachgetragen wurde.</b> Ohne sie lief die Suche
        /// in eingebettete Masken hinein und lieferte deren Infoknopf. Beobachtet auf
        /// dem Reiter „Berichte &amp; Kosten": <c>UcBerichteKosten</c> hängt seine
        /// Inhaltsfläche <c>pnlInhalt</c> als ERSTES Kind ein (Index 0), die Kopfzeile
        /// <c>lblKopf</c> mit dem eigenen Infoknopf erst als zweites. Die Zeile
        /// <c>UcBerichteKosten.btn_Help</c> traf deshalb den Knopf der gerade
        /// eingeblendeten Unterseite (<c>UcBericht</c> &amp;c.) - der eigene Knopf blieb
        /// ohne Schlüssel und wurde von
        /// <see cref="InfobuttonsOhneZuordnungAbschalten"/> abgeschaltet. Für den
        /// Anwender: zwei Infoknöpfe fast übereinander, der obere ohne Wirkung.
        /// </para>
        /// <para>
        /// Nachgetragen wird damit genau die Grenze, die <c>InfoKnopf.Vorhandenen</c>
        /// schon immer zieht und die
        /// <see cref="UnterPraefixeAnwenden"/> beim Präfix zieht: <b>Der Infoknopf
        /// einer eingebetteten Maske gehört IHR</b> und wird über IHRE Zeile
        /// angesprochen, nicht über die des Behälters.
        /// </para>
        /// <para>
        /// Ein eingebettetes Control kann weiterhin als ZWISCHENSTUFE eines
        /// Punktpfades angesprochen werden (<c>Behaelter.UcSeite.btn_Help</c>): Die
        /// Grenze verhindert nur das Hineinsteigen, nicht den Treffer auf das
        /// eingebettete Control selbst - und <see cref="FindControlRecursive"/> setzt
        /// die Suche dann in ihm als neuer Wurzel fort.
        /// </para>
        /// </remarks>
        private Control FindControlByNameDeep(Control root, string name)
        {
            if (root == null) return null;

            // Stimmt der Name direkt? (Auch die Wurzel selbst zaehlt - nur so kann ein
            // Punktpfad ueber ein eingebettetes UserControl weitergefuehrt werden.)
            if (string.Equals(root.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            foreach (Control child in root.Controls)
            {
                if (child == null) continue;

                if (child is Form || child is UserControl)
                {
                    // Getroffen werden darf es, betreten nicht.
                    if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
                        return child;

                    continue;
                }

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

            string ziel = ZielAufloesen(schluessel, out string anker);
            if (!string.IsNullOrEmpty(ziel))
            {
                HelpEntry entry = _catalog.Get(ziel);
                if (!string.IsNullOrEmpty(entry.Tooltip)) return MitAnker(entry, anker);
            }

            if (_catalog.IsLoaded)
            {
                SteuerelementAbschalten(ctrl);
                System.Diagnostics.Debug.WriteLine(
                    $"[Help] WARNUNG: Zur Zuordnung '{schluessel}' von '{ctrl.Name}' liefert der Katalog nichts - {Wirkung(ctrl)}.");
            }

            return null;
        }

        /// <summary>
        /// Die BRUECKE fuer Oberflaechen ohne Steuerelemente (Umsetzungskonzept iOS,
        /// Paket iU8): Was steht im Hilfekatalog zu dieser Zuordnungszeile?
        /// </summary>
        /// <param name="schluessel">
        /// Die linke Seite einer Zeile aus <c>help_mapping.txt</c>, also
        /// <c>Praefix.Controlpfad</c> - zum Beispiel
        /// <c>Form_Kosten_Auswahl.btn_Help</c>.
        /// </param>
        /// <returns>
        /// Kurztext, Beschreibung und Adresse; <c>null</c>, wenn die Zuordnung
        /// fehlt, der Katalog das Ziel nicht kennt oder noch nichts geladen ist.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <b>Warum das noetig ist.</b> Der ganze uebrige Weg des Hilfesystems
        /// haengt an einem <see cref="Control"/>: <see cref="ZuordnungenAnwenden"/>
        /// SUCHT das Steuerelement zu einer Zeile und haengt Ereignisse daran.
        /// Ein Blazor-Dialog hat keine Steuerelemente - sein Infoknopf ist ein
        /// <c>&lt;button&gt;</c> in einer WebView2. Er kennt nur denselben
        /// Schluessel und fragt hier nach.
        /// </para>
        /// <para>
        /// Aufgeloest wird genau wie beim Klick auf einen Infobutton
        /// (<see cref="EintragHolen"/>): Zuordnungszeile -&gt; Ziel, Ziel gegen die
        /// Oberflaechensprache und den Katalog (<see cref="ZielAufloesen(string, out string)"/>),
        /// Anker wieder anhaengen (<see cref="MitAnker"/>). Nur das Abschalten des
        /// Steuerelements bei leerem Katalog entfaellt - es gibt keines.
        /// </para>
        /// <para>
        /// <b>Die letzte passende Zeile gewinnt</b>, wie in
        /// <see cref="ZuordnungenAnwenden"/>: Die Zeilen der Datei neben der EXE
        /// stehen hinter den eingebetteten und uebersteuern sie damit (F2).
        /// </para>
        /// </remarks>
        public EPOS.UI.Dienste.HilfeEintrag ZielFuer(string schluessel)
        {
            if (_catalog == null || string.IsNullOrWhiteSpace(schluessel)) return null;

            // Zuordnungszeile suchen: "Praefix.Controlpfad = Ziel".
            string zeilenziel = "";
            foreach (string rohzeile in ZuordnungsZeilen())
            {
                string line = rohzeile == null ? "" : rohzeile.Trim('\uFEFF', ' ', '\t');
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                int gleich = line.IndexOf('=');
                if (gleich <= 0) continue;

                string linkeSeite = line.Substring(0, gleich).Trim();
                if (!string.Equals(linkeSeite, schluessel.Trim(), StringComparison.OrdinalIgnoreCase)) continue;

                string ziel = line.Substring(gleich + 1).Trim();
                if (ziel.Length > 0) zeilenziel = ziel;   // spaetere Zeile schlaegt fruehere
            }

            if (zeilenziel.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Help] WARNUNG: help_mapping.txt kennt '{schluessel}' nicht - der Infoknopf bleibt wirkungslos.");
                return null;
            }

            string adresse = ZielAufloesen(zeilenziel, out string anker);
            if (string.IsNullOrEmpty(adresse)) return null;

            HelpEntry eintrag = MitAnker(_catalog.Get(adresse), anker);
            if (eintrag == null || string.IsNullOrEmpty(eintrag.Tooltip)) return null;

            return new EPOS.UI.Dienste.HilfeEintrag(
                eintrag.Tooltip,
                eintrag.Beschreibung,
                string.IsNullOrEmpty(eintrag.Url) ? null : eintrag.Url);
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
            _popup.ShowHelpAngeheftet(entry.Tooltip, entry.Beschreibung, entry.Url, Cursor.Position);
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

            // Nutzerregel 29.08.2026: Das fluechtige Hover-Popup erscheint nur,
            // wenn es zum Ziel einen EIGENEN Text gibt. Die Bereichshilfe (H12)
            // zielt auf Anker INNERHALB einer Seite; einen ankerspezifischen
            // Text kennt der Katalog nicht - ihr Popup truege fuer jede Flaeche
            // denselben Seitentext und stuende beim Ueberfahren des Dialogs
            // staendig im Weg. Sie oeffnet weiterhin per Klick (Control_Click).
            // Infobuttons behalten das Hover-Popup, solange die Kurzbeschreibung
            // ihrer Zielseite vorliegt (H11) - ohne Text kein Popup.
            if (!IstInfobutton(ctrl) && schluessel.IndexOf('#') >= 0) return;
            if (string.IsNullOrWhiteSpace(entry.Beschreibung)) return;

            PopupBereitstellen();
            _popup.ShowHelp(entry.Tooltip, entry.Beschreibung, entry.Url, Cursor.Position);
        }

        private void Control_MouseLeave(object sender, EventArgs e)
        {
            if (this.DesignMode || _popup == null || _popup.IsDisposed) return;

            // Angeheftet bleibt stehen (F1).
            if (_popup.IstAngeheftet) return;

            // Der Uebertritt in ein KIND-Steuerelement (Eingabefeld in einer
            // GroupBox) feuert in WinForms ebenfalls MouseLeave — die
            // Bereichshilfe (H12) verschwand dadurch, sobald die Maus ein Feld
            // beruehrte. Solange der Zeiger im Quellbereich oder im Popup
            // steht, bleibt das Popup deshalb offen.
            Control quelle = sender as Control;
            Timer delayTimer = new Timer { Interval = 500 };
            delayTimer.Tick += (s, ev) =>
            {
                delayTimer.Stop();
                delayTimer.Dispose();

                if (_popup == null || _popup.IsDisposed || _popup.IstAngeheftet) return;
                if (_popup.Bounds.Contains(Cursor.Position)) return;
                if (quelle != null && !quelle.IsDisposed && quelle.Visible
                    && quelle.RectangleToScreen(quelle.ClientRectangle).Contains(Cursor.Position)) return;
                _popup.Hide();
            };
            delayTimer.Start();
        }
    }

}
