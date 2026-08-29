using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Paket H4 (Konzept Hilfesystem, B1-B4): die Online-Dokumentation als
    /// Wissensquelle des Hilfe-Assistenten - Wiki-Suche, Klartext-Auszuege,
    /// Tagescache.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Was hinausgeht.</b> An das Wiki geht eine kurze STICHWORTLISTE, nie die
    /// Rohfrage (<see cref="Stichwortliste"/>). Die Ableitung ist dieselbe wie in
    /// <see cref="HilfeWissen"/>: Woerter ab vier Zeichen, kleingeschrieben,
    /// ohne Wiederholung. Empfaenger ist der eigene Server
    /// <c>wiki.epos-plan.de</c> ueber TLS und ohne Anmeldung - ein ZWEITER
    /// Datenfluss neben dem Modellanbieter, der nach Entscheid 7.4 auch im
    /// Betrieb ohne KI stattfindet und deshalb im Rechtshinweis benannt ist
    /// (Entscheid 7.5, Fassung der Einwilligung bleibt unveraendert).
    /// </para>
    /// <para>
    /// <b>Reihenfolge je Seite:</b> frischer Cache -> Online -> abgelaufener
    /// Cache -> nichts. Der Cache liegt als eine Datei je Seite unter
    /// <c>%APPDATA%\wp-plan\wiki-wissen\</c> und gilt 24 Stunden; die
    /// TREFFERLISTEN der Suche werden bewusst nicht gecacht - sie haengen an der
    /// Frage und waeren beim naechsten Mal ohnehin andere.
    /// </para>
    /// <para>
    /// <b>Das Chatfenster darf nie am Wiki haengen.</b> Jeder Aufruf hat eine
    /// eigene Zeitgrenze von vier Sekunden, alles laeuft asynchron, und JEDER
    /// Fehler endet still (<see cref="Debug.WriteLine"/>) mit einem leeren
    /// Ergebnis. Ohne Netz faellt der Assistent damit auf das eingebaute Wissen
    /// zurueck, genau wie vor H4.
    /// </para>
    /// <para>
    /// <b>Wiki-Inhalte sind Daten, keine Anweisungen.</b> Die Auszuege werden im
    /// Prompt unveraendert als "Hilfeabschnitte" gefuehrt - hinter den
    /// Grundregeln, die <see cref="KiChatService"/> davorstellt.
    /// </para>
    /// </remarks>
    public static class WikiWissen
    {
        /// <summary>Bereichsangabe, unter der Wiki-Auszuege im Prompt erscheinen.</summary>
        public const string BEREICH = "Wiki";

        /// <summary>Titelpraefix der Rubrik mit den Dialoghilfen (H1/H2, A1).</summary>
        public const string RUBRIK = "Programm Dokumentation/";

        /// <summary>Hoechstens so viele Wiki-Seiten gehen in eine Antwort ein.</summary>
        public const int MAX_SEITEN = 3;

        /// <summary>Hoechstlaenge eines Seitenauszugs (Konzept B1.3).</summary>
        public const int MAX_ZEICHEN = 6000;

        /// <summary>So viele Treffer holt die Volltextsuche.</summary>
        private const int SUCH_TREFFER = 5;

        /// <summary>Mehr Stichwoerter verbessern nichts und verlaengern nur die Adresse.</summary>
        private const int MAX_STICHWOERTER = 8;

        /// <summary>Gueltigkeit einer Cache-Datei.</summary>
        private static readonly TimeSpan Gueltigkeit = TimeSpan.FromHours(24);

        /// <summary>Zeitgrenze je HTTP-Aufruf - kurz, damit die Oberflaeche nie wartet.</summary>
        private static readonly TimeSpan Zeitgrenze = TimeSpan.FromSeconds(4);

        /// <summary>
        /// EIN statischer Client fuer alle Aufrufe (Hausmuster wie
        /// <see cref="KiChatService"/> und <c>WikiHelpCatalog</c>).
        /// </summary>
        private static readonly HttpClient _http = new HttpClient { Timeout = Zeitgrenze };

        /// <summary>
        /// Die zuletzt gebaute Such-Adresse - fuer die Selbstpruefung, dass dort
        /// niemals die Rohfrage steht.
        /// </summary>
        public static string LetzteSuchAdresse { get; private set; } = "";

        // ==================================================================
        //  Basis-URL
        // ==================================================================

        /// <summary>
        /// Basis-URL der Dokumentation - derselbe Einstellwert, den auch der
        /// Hilfe-Katalog und der Menuepunkt Dokumentation benutzen (A2).
        /// </summary>
        public static string Basis()
        {
            string wert = null;
            try { wert = Properties.Settings.Default.WordPressUrl; }
            catch (Exception ex) { Debug.WriteLine("[Wiki] Einstellwert nicht lesbar: " + ex.Message); }

            if (string.IsNullOrWhiteSpace(wert)) wert = Program.WIKI_STANDARD;
            return wert.Trim().TrimEnd('/');
        }

        // ==================================================================
        //  Stichwoerter (B1.1) - die Rohfrage verlaesst den Rechner NICHT
        // ==================================================================

        /// <summary>Trennzeichen wie in <c>HilfeWissen.Zerlegen</c>.</summary>
        private static readonly char[] TRENNER =
            { ' ', '\t', '\r', '\n', ',', ';', '.', '?', '!', ':', '(', ')', '"', '\'', '/', '-' };

        /// <summary>
        /// Die Stichwoerter einer Frage: Woerter ab vier Zeichen, kleingeschrieben,
        /// ohne Wiederholung - dieselbe Regel, nach der <see cref="HilfeWissen"/>
        /// lokal bewertet (dort <c>if (w.Length &lt; 4) continue;</c>).
        /// </summary>
        public static string[] Stichwoerter(string frage)
        {
            if (string.IsNullOrWhiteSpace(frage)) return new string[0];

            return frage.ToLowerInvariant()
                        .Split(TRENNER, StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => w.Length >= 4)
                        .Distinct()
                        .Take(MAX_STICHWOERTER)
                        .ToArray();
        }

        /// <summary>Die Stichwoerter als eine Zeichenkette - genau das, was gesendet wird.</summary>
        public static string Stichwortliste(string frage)
        {
            return string.Join(" ", Stichwoerter(frage));
        }

        // ==================================================================
        //  Adressen
        // ==================================================================

        /// <summary>Adresse der Volltextsuche (REST, mit Abschnitts-Ankern).</summary>
        public static string SuchAdresse(string basis, string frage)
        {
            return basis + "/rest.php/v1/search/page?q=" +
                   Uri.EscapeDataString(Stichwortliste(frage)) + "&limit=" + SUCH_TREFFER;
        }

        /// <summary>Adresse der Klartext-Auszuege; die Titel werden mit %7C verbunden.</summary>
        public static string AuszugAdresse(string basis, IEnumerable<string> titel)
        {
            string liste = string.Join("%7C", titel.Select(Uri.EscapeDataString));

            // exlimit: MediaWiki liefert VOLLE Artikelauszuege sonst nur fuer die
            // erste Seite einer Sammelanfrage (am 29.08.2026 an wiki.epos-plan.de
            // nachgemessen: die Antwort meldet "exlimit was too large ... lowered
            // to 1"). Die Angabe schadet nicht und wirkt, sobald die Einstellung
            // des Wikis es zulaesst; solange sie nicht wirkt, holt
            // AuszugEinzelnAsync die uebrigen Seiten nach.
            return basis + "/api.php?action=query&prop=extracts&titles=" + liste +
                   "&explaintext=1&exlimit=max&format=json&redirects=1";
        }

        /// <summary>
        /// Die Adresse einer Wiki-Seite: Leerzeichen werden VOR der Kodierung zu
        /// Unterstrichen, der Schraegstrich der Unterseite bleibt stehen -
        /// dieselbe Vorschrift wie im Hilfe-Katalog (H1, A1).
        /// </summary>
        public static string SeitenUrl(string basis, string titel, string anker)
        {
            if (string.IsNullOrWhiteSpace(titel)) return "";

            string pfad = string.Join("/",
                titel.Trim().Replace(' ', '_').Split('/').Select(Uri.EscapeDataString));

            string url = basis + "/wiki/" + pfad;
            if (!string.IsNullOrWhiteSpace(anker)) url += "#" + anker.Trim().TrimStart('#');
            return url;
        }

        // ==================================================================
        //  Kontextseite (B1.4)
        // ==================================================================

        /// <summary>
        /// Bereichsbezeichnung aus <see cref="HilfeKontext"/> -&gt; Kurzname der
        /// Rubrik-Unterseite. Aufgestellt gegen die dortige Positivliste, nicht
        /// geraten; Bereiche ohne sinnvolle Seite fehlen bewusst.
        /// </summary>
        /// <remarks>
        /// Ohne Eintrag bleiben: <c>Unbekannter Bereich</c>, <c>Bericht</c> und
        /// <c>Lizenz</c> - fuer sie gibt es in der Rubrik keine Unterseite
        /// (Inventar A3). <c>Wärmequelle Erdreich (...)</c> zeigt auf
        /// <c>Wärmepumpe</c>: der Dialog legt das Quellsystem EINER Waermepumpe
        /// fest, und eine eigene Rubrikseite dafuer gibt es nicht.
        /// <c>Ergebnis</c> steht nicht in der Positivliste, ist aber die
        /// Bereichsangabe mehrerer eingebauter Abschnitte - der Eintrag schadet
        /// nicht und trifft, falls der Bereich einmal gesetzt wird.
        /// </remarks>
        private static readonly Dictionary<string, string> SEITE_JE_BEREICH =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Administration",           "Einstellungen" },
            { "Assistent (Wizard)",       "Kurzanleitung" },
            { "BHKW",                     "BHKW" },
            { "Brauchwasser",             "Brauchwasser" },
            { "Detaillierte Simulation",  "Simulation" },
            { "Ergebnis",                 "Simulation" },
            { "Gebäude",                  "Gebäude" },
            { "Hauptfenster",             "Programmablauf" },
            { "Heizkessel",               "Heizkessel" },
            { "Hilfe",                    "Hilfe-Assistent" },
            { "Klimadaten",               "Klimadaten" },
            { "Kosten und Preise",        "Kosten" },
            { "Photovoltaik",             "Photovoltaik" },
            { "Projektverwaltung",        "Projektverwaltung" },
            { "Prozesswärme",             "Prozesswärme" },
            { "Pufferspeicher",           "Pufferspeicher" },
            { "Simulation",               "Simulation" },
            { "Simulation Konfiguration", "Simulation" },
            { "Simulation Konfiguration (Erzeuger definieren, Pufferspeicher zuordnen)", "Simulation" },
            { "Solarthermie",             "Solarthermie" },
            { "Stromspeicher",            "Stromspeicher" },
            { "Stromverbraucher",         "Stromverbraucher" },
            { "Varianten",                "Varianten" },
            { "Wärmebedarf",              "Wärmebedarf" },
            { "Wärmepumpe",               "Wärmepumpe" },
            { "Wärmequelle Erdreich (Quellsystem, Bodentyp, Auslegungsprüfung VDI 4640)", "Wärmepumpe" },
            { "Wirtschaftlichkeit",       "Wirtschaftlichkeit" }
        };

        /// <summary>Kennung, mit der <c>HilfeKontext.Beschreibung()</c> den Bereich einleitet.</summary>
        private const string BEREICH_VORSATZ = "Bereich: ";

        /// <summary>
        /// Der Bereichstext aus der Kontextzeile ("Bereich: X | Registerkarte: Y"
        /// -&gt; "X"). Leer, wenn die Zeile keinen Bereich fuehrt.
        /// </summary>
        public static string BereichAus(string kontext)
        {
            if (string.IsNullOrWhiteSpace(kontext)) return "";

            string rest = kontext;
            int p = rest.IndexOf(BEREICH_VORSATZ, StringComparison.OrdinalIgnoreCase);
            if (p >= 0) rest = rest.Substring(p + BEREICH_VORSATZ.Length);

            int ende = rest.IndexOf('|');
            if (ende >= 0) rest = rest.Substring(0, ende);

            return rest.Trim();
        }

        /// <summary>
        /// Kurzname der Rubrik-Unterseite zum aktuellen Bereich; leer, wenn es
        /// keine gibt. Ein Bereich, der laenger ist als sein Tabelleneintrag
        /// (die Masken setzen bewusst feinere Bezeichnungen), trifft ueber den
        /// laengsten passenden Anfang.
        /// </summary>
        public static string KontextSeite(string kontext)
        {
            string bereich = BereichAus(kontext);
            if (bereich.Length == 0) return "";

            string kurzname;
            if (SEITE_JE_BEREICH.TryGetValue(bereich, out kurzname)) return kurzname;

            string besterSchluessel = null;
            foreach (KeyValuePair<string, string> paar in SEITE_JE_BEREICH)
            {
                if (!bereich.StartsWith(paar.Key, StringComparison.OrdinalIgnoreCase)) continue;
                if (besterSchluessel == null || paar.Key.Length > besterSchluessel.Length)
                {
                    besterSchluessel = paar.Key;
                    kurzname = paar.Value;
                }
            }

            return besterSchluessel != null ? kurzname : "";
        }

        /// <summary>Vollstaendiger Wiki-Titel einer Rubrik-Unterseite.</summary>
        public static string RubrikTitel(string kurzname)
        {
            return string.IsNullOrWhiteSpace(kurzname) ? "" : RUBRIK + kurzname.Trim();
        }

        // ==================================================================
        //  Der eine Einstiegspunkt
        // ==================================================================

        /// <summary>
        /// Sucht Abschnitte der Online-Dokumentation zu einer Frage. Ergebnis ist
        /// dieselbe Abschnittsform wie beim eingebauten Wissen, zusaetzlich mit
        /// <see cref="WissensAbschnitt.QuellUrl"/> fuer die Quellenangabe im Chat.
        /// Bei jedem Fehler bleibt die Liste leer.
        /// </summary>
        public static Task<List<WissensAbschnitt>> SucheAsync(string frage, string kontext,
                                                              CancellationToken abbruch = default)
        {
            return SucheAsync(Basis(), frage, kontext, abbruch);
        }

        /// <summary>
        /// Dieselbe Suche gegen eine ausdruecklich angegebene Basis-URL - der Weg
        /// des Pruefharnischs (auch fuer die Offline-Probe mit falscher Adresse).
        /// </summary>
        internal static async Task<List<WissensAbschnitt>> SucheAsync(string basis, string frage,
                                                                      string kontext,
                                                                      CancellationToken abbruch)
        {
            List<WissensAbschnitt> ergebnis = new List<WissensAbschnitt>();
            if (string.IsNullOrWhiteSpace(basis) || string.IsNullOrWhiteSpace(frage)) return ergebnis;

            try
            {
                // ---- 1) Treffer sammeln: Kontextseite immer zuerst, dann die Suche.
                List<Suchtreffer> treffer = new List<Suchtreffer>();

                string kontextTitel = RubrikTitel(KontextSeite(kontext));
                if (kontextTitel.Length > 0)
                    treffer.Add(new Suchtreffer { Titel = kontextTitel });

                foreach (Suchtreffer t in await SuchtrefferAsync(basis, frage, abbruch).ConfigureAwait(false))
                {
                    if (treffer.Any(v => string.Equals(v.Titel, t.Titel, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    treffer.Add(t);
                }

                if (treffer.Count == 0) return ergebnis;

                // Rubrik-Treffer nach vorn (OrderBy ist stabil, die Kontextseite
                // bleibt damit an erster Stelle).
                List<Suchtreffer> gereiht = treffer
                    .OrderBy(t => t.Titel.StartsWith(RUBRIK, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .Take(MAX_SEITEN)
                    .ToList();

                // ---- 2) Auszuege: frischer Cache, sonst online, sonst alter Cache.
                Dictionary<string, string> texte =
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                List<string> fehlend = new List<string>();

                foreach (Suchtreffer t in gereiht)
                {
                    string ausCache = CacheLesen(basis, t.Titel, Gueltigkeit);
                    if (ausCache != null) texte[t.Titel] = ausCache;
                    else fehlend.Add(t.Titel);
                }

                if (fehlend.Count > 0)
                {
                    Dictionary<string, string> geholt =
                        await AuszuegeAsync(basis, fehlend, abbruch).ConfigureAwait(false);

                    foreach (string titel in fehlend)
                    {
                        string text;
                        if (!geholt.TryGetValue(titel, out text) || string.IsNullOrWhiteSpace(text))
                        {
                            // Die Sammelanfrage liefert je nach Wiki-Einstellung nur
                            // fuer die erste Seite einen vollen Auszug - der Rest
                            // wird einzeln nachgeholt.
                            text = await AuszugEinzelnAsync(basis, titel, abbruch).ConfigureAwait(false);
                        }

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            texte[titel] = text;
                            CacheSchreiben(basis, titel, text);
                        }
                        else
                        {
                            // Letzte Stufe: ein abgelaufener Cache ist besser als nichts.
                            string alt = CacheLesen(basis, titel, TimeSpan.MaxValue);
                            if (alt != null) texte[titel] = alt;
                        }
                    }
                }

                // ---- 3) Abschnitte bauen, Reihenfolge der Treffer beibehalten.
                foreach (Suchtreffer t in gereiht)
                {
                    string text;
                    if (!texte.TryGetValue(t.Titel, out text) || string.IsNullOrWhiteSpace(text)) continue;

                    ergebnis.Add(new WissensAbschnitt(t.Titel, BEREICH, Kappen(text),
                                                      SeitenUrl(basis, t.Titel, t.Anker)));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Wiki] Suche fehlgeschlagen: " + ex.Message);
                return new List<WissensAbschnitt>();
            }

            return ergebnis;
        }

        /// <summary>Kappt einen Auszug auf <see cref="MAX_ZEICHEN"/> - Marke eingerechnet.</summary>
        internal static string Kappen(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= MAX_ZEICHEN) return text ?? "";
            return text.Substring(0, MAX_ZEICHEN - 3) + "...";
        }

        // ==================================================================
        //  Volltextsuche (B1.2)
        // ==================================================================

        /// <summary>Ein Suchtreffer des Wikis.</summary>
        internal sealed class Suchtreffer
        {
            public string Titel = "";
            public string Anker = "";
            public string Beschreibung = "";
        }

        private static async Task<List<Suchtreffer>> SuchtrefferAsync(string basis, string frage,
                                                                      CancellationToken abbruch)
        {
            List<Suchtreffer> liste = new List<Suchtreffer>();

            string stichwoerter = Stichwortliste(frage);
            if (stichwoerter.Length == 0) return liste;      // nichts Brauchbares zu senden

            string adresse = SuchAdresse(basis, frage);
            LetzteSuchAdresse = adresse;

            string rumpf = await HolenAsync(adresse, abbruch).ConfigureAwait(false);
            if (rumpf == null) return liste;

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(rumpf))
                {
                    JsonElement seiten;
                    if (!doc.RootElement.TryGetProperty("pages", out seiten) ||
                        seiten.ValueKind != JsonValueKind.Array) return liste;

                    foreach (JsonElement s in seiten.EnumerateArray())
                    {
                        string titel = Zeichenkette(s, "title");
                        if (string.IsNullOrWhiteSpace(titel)) continue;

                        liste.Add(new Suchtreffer
                        {
                            Titel = titel,
                            Anker = Zeichenkette(s, "anchor"),
                            Beschreibung = Zeichenkette(s, "description")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Wiki] Trefferliste nicht lesbar: " + ex.Message);
            }

            return liste;
        }

        // ==================================================================
        //  Klartext-Auszuege (B1.3)
        // ==================================================================

        private static async Task<Dictionary<string, string>> AuszuegeAsync(
            string basis, List<string> titel, CancellationToken abbruch)
        {
            Dictionary<string, string> ergebnis =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (titel == null || titel.Count == 0) return ergebnis;

            string rumpf = await HolenAsync(AuszugAdresse(basis, titel), abbruch).ConfigureAwait(false);
            AuszuegeLesen(rumpf, ergebnis);
            return ergebnis;
        }

        private static async Task<string> AuszugEinzelnAsync(string basis, string titel,
                                                             CancellationToken abbruch)
        {
            Dictionary<string, string> eine =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string rumpf = await HolenAsync(AuszugAdresse(basis, new[] { titel }), abbruch)
                                 .ConfigureAwait(false);
            AuszuegeLesen(rumpf, eine);

            string text;
            if (eine.TryGetValue(titel, out text)) return text;

            // Bei einer Weiterleitung traegt die Antwort den ZIELtitel; bei genau
            // einer angefragten Seite ist der einzige Auszug zwangslaeufig ihrer.
            return eine.Count == 1 ? eine.Values.First() : "";
        }

        /// <summary>Liest <c>query.pages[*].extract</c> in die Tabelle, Schluessel ist der Seitentitel.</summary>
        private static void AuszuegeLesen(string rumpf, Dictionary<string, string> ziel)
        {
            if (string.IsNullOrWhiteSpace(rumpf)) return;

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(rumpf))
                {
                    JsonElement abfrage, seiten;
                    if (!doc.RootElement.TryGetProperty("query", out abfrage)) return;
                    if (!abfrage.TryGetProperty("pages", out seiten) ||
                        seiten.ValueKind != JsonValueKind.Object) return;

                    foreach (JsonProperty seite in seiten.EnumerateObject())
                    {
                        string titel = Zeichenkette(seite.Value, "title");
                        string text = Zeichenkette(seite.Value, "extract");
                        if (string.IsNullOrWhiteSpace(titel) || string.IsNullOrWhiteSpace(text)) continue;

                        ziel[titel] = text.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Wiki] Auszug nicht lesbar: " + ex.Message);
            }
        }

        // ==================================================================
        //  HTTP - still, kurz, abbrechbar
        // ==================================================================

        /// <summary>
        /// Holt eine Adresse; <c>null</c> bei jedem Fehler. Die Antwort wird
        /// ausdruecklich als UTF-8 gelesen: die REST-Schnittstelle des Wikis
        /// meldet <c>application/json</c> OHNE Zeichensatz.
        /// </summary>
        private static async Task<string> HolenAsync(string adresse, CancellationToken abbruch)
        {
            try
            {
                using (CancellationTokenSource uhr =
                           CancellationTokenSource.CreateLinkedTokenSource(abbruch))
                {
                    uhr.CancelAfter(Zeitgrenze);

                    using (HttpRequestMessage nachricht =
                               new HttpRequestMessage(HttpMethod.Get, adresse))
                    using (HttpResponseMessage antwort =
                               await _http.SendAsync(nachricht, uhr.Token).ConfigureAwait(false))
                    {
                        if (!antwort.IsSuccessStatusCode)
                        {
                            Debug.WriteLine("[Wiki] HTTP " + (int)antwort.StatusCode + " fuer " + adresse);
                            return null;
                        }

                        byte[] rohdaten = await antwort.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        return Encoding.UTF8.GetString(rohdaten);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Wiki] Abruf fehlgeschlagen (" + adresse + "): " + ex.Message);
                return null;
            }
        }

        private static string Zeichenkette(JsonElement knoten, string name)
        {
            JsonElement wert;
            if (!knoten.TryGetProperty(name, out wert)) return "";
            return wert.ValueKind == JsonValueKind.String ? (wert.GetString() ?? "") : "";
        }

        // ==================================================================
        //  Cache: je Seite eine Datei, 24 Stunden gueltig
        // ==================================================================

        /// <summary>Ablageordner der Seitenauszuege.</summary>
        public static string CacheOrdner()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "wp-plan", "wiki-wissen");
        }

        /// <summary>
        /// Dateiname einer Seite: Streuwert ueber Basis-URL UND Titel. Die
        /// Basis-URL gehoert hinein, sonst laege der Auszug eines anderen Wikis
        /// unter demselben Namen.
        /// </summary>
        internal static string CacheDatei(string basis, string titel)
        {
            using (SHA256 hasch = SHA256.Create())
            {
                byte[] streu = hasch.ComputeHash(
                    Encoding.UTF8.GetBytes((basis ?? "") + "|" + (titel ?? "")));

                StringBuilder sb = new StringBuilder(32);
                for (int i = 0; i < 16; i++) sb.Append(streu[i].ToString("x2", CultureInfo.InvariantCulture));
                return Path.Combine(CacheOrdner(), sb.ToString() + ".txt");
            }
        }

        private const string CACHE_TITEL = "Titel: ";
        private const string CACHE_ZEIT = "Abgerufen: ";
        private const string CACHE_QUELLE = "Quelle: ";

        /// <summary>
        /// Liest einen zwischengespeicherten Auszug; <c>null</c>, wenn es keinen
        /// gibt oder er aelter ist als <paramref name="hoechstalter"/>.
        /// </summary>
        internal static string CacheLesen(string basis, string titel, TimeSpan hoechstalter)
        {
            try
            {
                string datei = CacheDatei(basis, titel);
                if (!File.Exists(datei)) return null;

                string[] zeilen = File.ReadAllLines(datei, Encoding.UTF8);
                if (zeilen.Length < 4) return null;

                DateTime abgerufen;
                if (!DateTime.TryParse(zeilen[1].StartsWith(CACHE_ZEIT, StringComparison.Ordinal)
                                           ? zeilen[1].Substring(CACHE_ZEIT.Length) : zeilen[1],
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                       out abgerufen))
                    return null;

                if (hoechstalter != TimeSpan.MaxValue &&
                    DateTime.UtcNow - abgerufen > hoechstalter) return null;

                // Zeile 0 Titel, 1 Zeitpunkt, 2 Quelle, 3 Leerzeile, ab 4 der Text.
                return string.Join("\n", zeilen.Skip(4)).Trim();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Wiki] Cache nicht lesbar: " + ex.Message);
                return null;
            }
        }

        /// <summary>Legt einen Auszug ab. Scheitert das, faellt nichts aus - es wird nur nicht gecacht.</summary>
        internal static void CacheSchreiben(string basis, string titel, string text)
        {
            try
            {
                Directory.CreateDirectory(CacheOrdner());

                StringBuilder sb = new StringBuilder();
                sb.Append(CACHE_TITEL).Append(titel).Append('\n');
                sb.Append(CACHE_ZEIT)
                  .Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))
                  .Append('\n');
                sb.Append(CACHE_QUELLE).Append(basis).Append('\n');
                sb.Append('\n');
                sb.Append(text ?? "");

                File.WriteAllText(CacheDatei(basis, titel), sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Wiki] Cache nicht schreibbar: " + ex.Message);
            }
        }
    }
}
