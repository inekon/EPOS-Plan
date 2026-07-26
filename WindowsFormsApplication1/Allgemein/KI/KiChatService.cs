using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>Ergebnis einer KI-Anfrage.</summary>
    public class KiAntwort
    {
        public bool Erfolg;
        public string Text = "";
        public string Fehler = "";
        public List<string> Quellen = new List<string>();
        public bool AusCache;
        public int TokenGeschaetzt;
    }

    /// <summary>
    /// Anbindung an Google Gemini 2.5 Flash-Lite über die REST-Schnittstelle.
    ///
    /// Kostenbewusst ausgelegt:
    ///  - RAG: nur die wenigen passenden Hilfeabschnitte gehen als Kontext mit,
    ///    nicht die gesamte Dokumentation
    ///  - kleines, günstiges Modell (Flash-Lite)
    ///  - begrenzte Antwortlänge (maxOutputTokens)
    ///  - lokaler Antwort-Cache: gleiche Frage im gleichen Bereich kostet nichts
    ///  - Tageslimit je Arbeitsplatz gegen Ausreißer
    ///
    /// Datenschutz: Übertragen werden ausschließlich Hilfetexte, die Frage des
    /// Benutzers und eine grobe Kontextangabe (Maske/Registerkarte).
    /// Es werden keine Projekt-, Kunden- oder Simulationsdaten gesendet.
    /// </summary>
    public static class KiChatService
    {
        // Registry-Ablage (wie Sprache und CSV-Export-Pfad)
        private const string REG_SCHLUESSEL = @"Software\wp-plan";
        private const string REG_APIKEY = "GeminiApiKey";
        private const string REG_LIMIT = "KiTageslimit";
        private const string REG_ZAEHLER = "KiZaehler";
        private const string REG_ZAEHLER_TAG = "KiZaehlerTag";
        private const string REG_MODELL = "KiModell";

        /// <summary>
        /// Bevorzugte Modelle in absteigender Reihenfolge (günstig zuerst).
        /// Modellnamen ändern sich beim Anbieter regelmäßig - schlägt der Aufruf
        /// mit "nicht mehr verfügbar" fehl, ermittelt der Dienst automatisch ein
        /// passendes verfügbares Modell und merkt es sich.
        /// </summary>
        private static readonly string[] MODELL_KANDIDATEN =
        {
            "gemini-3.5-flash-lite",
            "gemini-2.5-flash-lite",
            "gemini-3.5-flash",
            "gemini-2.5-flash"
        };

        /// <summary>Tatsächlich verwendetes Modell (gemerkt in der Registry).</summary>
        public static string MODELL
        {
            get
            {
                string gemerkt = RegLesen(REG_MODELL);
                return !string.IsNullOrWhiteSpace(gemerkt) ? gemerkt : MODELL_KANDIDATEN[0];
            }
            set { RegSchreiben(REG_MODELL, value ?? ""); }
        }
        private const int MAX_ANTWORT_TOKEN = 400;
        private const int STANDARD_TAGESLIMIT = 50;

        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        // Lokaler Antwort-Cache (Frage + Kontext -> Antwort), spart Kosten
        private static readonly Dictionary<string, KiAntwort> _cache = new Dictionary<string, KiAntwort>();

        // ------------------------------------------------------------------
        // Konfiguration
        // ------------------------------------------------------------------

        public static string ApiKey
        {
            get { return RegLesen(REG_APIKEY) ?? ""; }
            set { RegSchreiben(REG_APIKEY, value ?? ""); }
        }

        public static int Tageslimit
        {
            get
            {
                string v = RegLesen(REG_LIMIT);
                int limit;
                return (v != null && int.TryParse(v, out limit) && limit > 0) ? limit : STANDARD_TAGESLIMIT;
            }
            set { RegSchreiben(REG_LIMIT, value.ToString()); }
        }

        public static bool IstEingerichtet
        {
            get { return !string.IsNullOrWhiteSpace(ApiKey); }
        }

        /// <summary>
        /// Verwirft ein gemerktes Modell, sodass beim nächsten Aufruf wieder mit
        /// der Vorgabe begonnen und bei Bedarf neu erkannt wird.
        /// </summary>
        public static void ModellZuruecksetzen()
        {
            RegSchreiben(REG_MODELL, "");
        }

        /// <summary>Verbrauchte Anfragen des heutigen Tages.</summary>
        public static int AnfragenHeute
        {
            get
            {
                string tag = RegLesen(REG_ZAEHLER_TAG);
                if (tag != DateTime.Today.ToString("yyyy-MM-dd")) return 0;
                int n;
                return int.TryParse(RegLesen(REG_ZAEHLER) ?? "0", out n) ? n : 0;
            }
        }

        private static void ZaehlerErhoehen()
        {
            int n = AnfragenHeute + 1;
            RegSchreiben(REG_ZAEHLER_TAG, DateTime.Today.ToString("yyyy-MM-dd"));
            RegSchreiben(REG_ZAEHLER, n.ToString());
        }

        // ------------------------------------------------------------------
        // Anfrage
        // ------------------------------------------------------------------

        /// <summary>
        /// Stellt eine Frage an den Assistenten. Der Kontext beschreibt die
        /// aktuelle Maske, der Verlauf die letzten Wortwechsel (bewusst kurz).
        /// </summary>
        public static async Task<KiAntwort> FrageAsync(string frage, string kontext, List<string> verlauf = null)
        {
            KiAntwort antwort = new KiAntwort();

            if (string.IsNullOrWhiteSpace(frage))
            {
                antwort.Fehler = "Keine Frage angegeben.";
                return antwort;
            }

            if (!IstEingerichtet)
            {
                antwort.Fehler = "Es ist kein API-Schlüssel hinterlegt. " +
                                 "Bitte im Chatfenster über 'Einstellungen...' eintragen.";
                return antwort;
            }

            // 1) Cache prüfen - gleiche Frage im gleichen Bereich kostet nichts
            string cacheKey = (kontext ?? "") + "||" + frage.Trim().ToLowerInvariant();
            if (_cache.ContainsKey(cacheKey))
            {
                KiAntwort treffer = _cache[cacheKey];
                return new KiAntwort
                {
                    Erfolg = treffer.Erfolg,
                    Text = treffer.Text,
                    Quellen = treffer.Quellen,
                    AusCache = true
                };
            }

            // 2) Tageslimit prüfen
            if (AnfragenHeute >= Tageslimit)
            {
                antwort.Fehler = "Das Tageslimit von " + Tageslimit + " Anfragen ist erreicht. " +
                                 "Die Suche in der Hilfe steht weiterhin zur Verfügung.";
                return antwort;
            }

            // 3) Passende Hilfeabschnitte suchen (lokal, kostenlos)
            List<WissensAbschnitt> treffer2 = HilfeWissen.Suchen(frage, kontext, 4);
            if (treffer2.Count == 0)
            {
                // Ohne Treffer trotzdem antworten lassen - mit klarer Ansage im Prompt
                treffer2 = new List<WissensAbschnitt>();
            }

            string prompt = PromptBauen(frage, kontext, treffer2, verlauf);
            antwort.TokenGeschaetzt = prompt.Length / 4;   // grobe Schätzung

            try
            {
                string text = await AufrufenAsync(prompt);
                antwort.Erfolg = true;
                antwort.Text = text;
                foreach (WissensAbschnitt a in treffer2) antwort.Quellen.Add(a.Titel);

                ZaehlerErhoehen();
                _cache[cacheKey] = antwort;
            }
            catch (Exception ex)
            {
                antwort.Fehler = "Die Anfrage konnte nicht beantwortet werden:\r\n" + ex.Message;
            }

            return antwort;
        }

        /// <summary>
        /// Baut den Prompt: knappe Rolle, Kontext, Hilfeabschnitte, Frage.
        /// Die strikte Bindung an die Abschnitte verhindert erfundene Antworten.
        /// </summary>
        private static string PromptBauen(string frage, string kontext,
                                          List<WissensAbschnitt> abschnitte, List<string> verlauf)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Du bist der Hilfe-Assistent der Energieplanungs-Software WP-Plan.");
            sb.AppendLine("Beantworte die Frage kurz, sachlich und auf Deutsch - höchstens 6 Sätze.");
            sb.AppendLine("Stütze dich AUSSCHLIESSLICH auf die unten stehenden Hilfeabschnitte.");
            sb.AppendLine("Steht die Antwort dort nicht, sage das klar und nenne, wo der Benutzer nachsehen kann.");
            sb.AppendLine("Erfinde keine Menüpunkte, Feldnamen oder Zahlenwerte.");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(kontext))
            {
                sb.AppendLine("Der Benutzer befindet sich gerade hier:");
                sb.AppendLine(kontext);
                sb.AppendLine("Beziehe dich bevorzugt auf diesen Bereich.");
                sb.AppendLine();
            }

            if (abschnitte != null && abschnitte.Count > 0)
            {
                sb.AppendLine("Hilfeabschnitte:");
                foreach (WissensAbschnitt a in abschnitte)
                {
                    sb.AppendLine("---");
                    sb.AppendLine("Titel: " + a.Titel);
                    if (!string.IsNullOrEmpty(a.Bereich)) sb.AppendLine("Bereich: " + a.Bereich);
                    sb.AppendLine(a.Inhalt);
                }
                sb.AppendLine("---");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("Hinweis: Zu dieser Frage wurden keine Hilfeabschnitte gefunden.");
                sb.AppendLine();
            }

            // Nur die letzten Wortwechsel mitgeben (hält die Token-Menge konstant)
            if (verlauf != null && verlauf.Count > 0)
            {
                sb.AppendLine("Bisheriger Verlauf (Auszug):");
                int start = Math.Max(0, verlauf.Count - 4);
                for (int i = start; i < verlauf.Count; i++) sb.AppendLine(verlauf[i]);
                sb.AppendLine();
            }

            sb.AppendLine("Frage: " + frage.Trim());
            return sb.ToString();
        }

        /// <summary>
        /// Ruft die Gemini-REST-Schnittstelle auf. Meldet der Dienst, dass das
        /// Modell nicht (mehr) verfügbar ist, wird automatisch ein passendes
        /// verfügbares Modell ermittelt, gemerkt und die Anfrage wiederholt.
        /// </summary>
        private static async Task<string> AufrufenAsync(string prompt)
        {
            try
            {
                return await AufrufenMitModellAsync(prompt, MODELL);
            }
            catch (Exception ex)
            {
                if (!IstModellFehler(ex.Message)) throw;

                string neuesModell = await ModellErmittelnAsync();
                if (string.IsNullOrEmpty(neuesModell))
                    throw new Exception("Das Modell '" + MODELL + "' ist nicht verfügbar und es konnte " +
                                        "kein Ersatzmodell ermittelt werden.\r\n\r\nUrsprüngliche Meldung: " + ex.Message);

                MODELL = neuesModell;   // für die nächsten Aufrufe merken
                return await AufrufenMitModellAsync(prompt, neuesModell);
            }
        }

        /// <summary>Erkennt Fehlermeldungen, die auf ein ungültiges Modell hindeuten.</summary>
        private static bool IstModellFehler(string meldung)
        {
            if (string.IsNullOrEmpty(meldung)) return false;
            string m = meldung.ToLowerInvariant();
            return m.Contains("404") || m.Contains("no longer available") ||
                   m.Contains("not found") || m.Contains("is not supported");
        }

        /// <summary>
        /// Fragt die Modell-Liste des Anbieters ab und wählt das erste passende
        /// Modell: bevorzugt die eigenen Kandidaten, sonst das erste verfügbare
        /// Modell mit "flash-lite" bzw. "flash" im Namen.
        /// </summary>
        private static async Task<string> ModellErmittelnAsync()
        {
            try
            {
                string url = "https://generativelanguage.googleapis.com/v1beta/models?key=" +
                             Uri.EscapeDataString(ApiKey);

                using (HttpResponseMessage antwort = await _http.GetAsync(url))
                {
                    string body = await antwort.Content.ReadAsStringAsync();
                    if (!antwort.IsSuccessStatusCode) return null;

                    List<string> verfuegbar = new List<string>();

                    using (JsonDocument doc = JsonDocument.Parse(body))
                    {
                        if (!doc.RootElement.TryGetProperty("models", out JsonElement modelle)) return null;

                        foreach (JsonElement m in modelle.EnumerateArray())
                        {
                            if (!m.TryGetProperty("name", out JsonElement nameEl)) continue;
                            string name = nameEl.GetString() ?? "";
                            if (name.StartsWith("models/")) name = name.Substring(7);

                            // Nur Modelle, die Textantworten erzeugen können
                            bool kannGenerieren = true;
                            if (m.TryGetProperty("supportedGenerationMethods", out JsonElement methoden))
                            {
                                kannGenerieren = false;
                                foreach (JsonElement me in methoden.EnumerateArray())
                                    if (me.GetString() == "generateContent") { kannGenerieren = true; break; }
                            }
                            if (kannGenerieren) verfuegbar.Add(name);
                        }
                    }

                    // 1) eigene Wunschliste
                    foreach (string kandidat in MODELL_KANDIDATEN)
                        if (verfuegbar.Contains(kandidat)) return kandidat;

                    // 2) sonst günstigste Klasse: flash-lite vor flash, Vorschauen zuletzt
                    string treffer = verfuegbar.FirstOrDefault(
                        n => n.Contains("flash-lite") && !n.Contains("preview"));
                    if (treffer != null) return treffer;

                    treffer = verfuegbar.FirstOrDefault(
                        n => n.Contains("flash") && !n.Contains("preview") &&
                             !n.Contains("audio") && !n.Contains("image") && !n.Contains("omni"));
                    if (treffer != null) return treffer;

                    return verfuegbar.FirstOrDefault(n => n.Contains("flash"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Modell-Liste konnte nicht gelesen werden: " + ex.Message);
                return null;
            }
        }

        /// <summary>Führt den eigentlichen Aufruf mit einem konkreten Modell aus.</summary>
        private static async Task<string> AufrufenMitModellAsync(string prompt, string modell)
        {
            string url = "https://generativelanguage.googleapis.com/v1beta/models/" +
                         modell + ":generateContent?key=" + Uri.EscapeDataString(ApiKey);

            var anfrage = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    temperature = 0.2,                    // sachlich, wenig Variation
                    maxOutputTokens = MAX_ANTWORT_TOKEN   // begrenzt die Kosten je Antwort
                }
            };

            string json = JsonSerializer.Serialize(anfrage);

            using (StringContent inhalt = new StringContent(json, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage antwort = await _http.PostAsync(url, inhalt))
            {
                string body = await antwort.Content.ReadAsStringAsync();

                if (!antwort.IsSuccessStatusCode)
                    throw new Exception("HTTP " + (int)antwort.StatusCode + " - " + KurzFehler(body));

                return TextAusJson(body);
            }
        }

        /// <summary>Liest den Antworttext aus der JSON-Antwort.</summary>
        private static string TextAusJson(string body)
        {
            using (JsonDocument doc = JsonDocument.Parse(body))
            {
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("candidates", out JsonElement kandidaten) &&
                    kandidaten.GetArrayLength() > 0)
                {
                    JsonElement erster = kandidaten[0];
                    if (erster.TryGetProperty("content", out JsonElement content) &&
                        content.TryGetProperty("parts", out JsonElement parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (JsonElement p in parts.EnumerateArray())
                        {
                            if (p.TryGetProperty("text", out JsonElement t))
                                sb.Append(t.GetString());
                        }
                        string text = sb.ToString().Trim();
                        if (text.Length > 0) return text;
                    }
                }

                throw new Exception("Die Antwort enthielt keinen Text.");
            }
        }

        private static string KurzFehler(string body)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(body))
                {
                    if (doc.RootElement.TryGetProperty("error", out JsonElement fehler) &&
                        fehler.TryGetProperty("message", out JsonElement msg))
                        return msg.GetString();
                }
            }
            catch { }

            if (body != null && body.Length > 200) return body.Substring(0, 200) + "...";
            return body ?? "";
        }

        // ------------------------------------------------------------------
        // Registry-Zugriff (still, ohne Fehlerdialoge)
        // ------------------------------------------------------------------

        private static string RegLesen(string wert)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                       Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REG_SCHLUESSEL))
                {
                    return key != null ? key.GetValue(wert) as string : null;
                }
            }
            catch { return null; }
        }

        private static void RegSchreiben(string wert, string inhalt)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                       Microsoft.Win32.Registry.CurrentUser.CreateSubKey(REG_SCHLUESSEL))
                {
                    if (key != null) key.SetValue(wert, inhalt ?? "");
                }
            }
            catch { }
        }
    }
}
