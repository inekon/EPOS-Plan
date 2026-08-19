using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using KiKern;

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

        /// <summary>Die Aktionen dieser Äußerung, in der Reihenfolge der Runden (Etappe 2).</summary>
        public List<KiSchritt> Schritte = new List<KiSchritt>();

        /// <summary>Die Antwort entstand über den Rückfallweg B (Aktionsvorschlag als JSON im Text).</summary>
        public bool WegB;

        /// <summary>Verbrauchte Modellrunden; höchstens <see cref="KiWerkzeuge.Rundendeckel"/>.</summary>
        public int Runden;

        /// <summary>Der Rundendeckel hat den Lauf beendet.</summary>
        public bool Deckel;

        /// <summary>Hinweise, die NICHT vom Modell stammen (Wegwechsel, Einstellungen).</summary>
        public List<string> Hinweise = new List<string>();
    }

    /// <summary>
    /// Ein Aktionsschritt einer Werkzeugrunde - das, was der Chat als „ich habe X getan"
    /// anzeigt, samt der zugehörigen Protokollzeile.
    /// </summary>
    /// <remarks>
    /// Der Schritt entsteht IMMER, auch wenn nichts lief: ein abgewiesener Aufruf
    /// (Parameterfehler, geschlossener Riegel) ist für den Anwender genauso wichtig wie
    /// ein gelungener - sonst bliebe unklar, warum keine Zahl kam.
    /// </remarks>
    public class KiSchritt
    {
        /// <summary>Name der Aktion, wie ihn das Modell gerufen hat.</summary>
        public string Aktion = "";

        /// <summary>Aktion mit Angaben in Klartext (<see cref="KiBestaetigung.Kurzfassung"/>).</summary>
        public string Kurzfassung = "";

        /// <summary>Die Aktion lief und lieferte ein Ergebnis.</summary>
        public bool Ausgefuehrt;

        /// <summary>Klartextgrund, wenn nichts lief; leer, wenn alles gut ging.</summary>
        public string Grund = "";

        /// <summary>Das Ergebnis; <c>null</c>, wenn die Aktion gar nicht erst lief.</summary>
        public KiErgebnis Ergebnis;

        /// <summary>Die Protokollzeile dieses Versuchs (Protokoll liegt neben der Datenbank).</summary>
        public string Protokollzeile = "";

        /// <summary>Brauchte dieser Aufruf die ausdrückliche Bestätigung des Anwenders?</summary>
        public bool Bestaetigungspflichtig;

        /// <summary>
        /// Der Bestätigungstext, der dem Anwender gezeigt wurde — aus <c>KiBestaetigung</c>,
        /// nie aus Modelltext. Leer, wenn es zu keiner Vorschau kam.
        /// </summary>
        public string Bestaetigung = "";

        /// <summary>Wie der Anwender entschieden hat.</summary>
        public KiEntscheidung Entscheidung = KiEntscheidung.Offen;

        /// <summary>Pfad des Sicherungspunkts dieser Sitzung; leer, wenn keiner nötig war.</summary>
        public string Sicherungspunkt = "";
    }

    /// <summary>
    /// Der Weg, auf dem die ausdrückliche Bestätigung des Anwenders eingeholt wird
    /// (Fachkonzept 3.5, Punkt 3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Oberfläche hängt sich hier ein und zeigt den Vorschaublock mit
    /// „Ausführen"/„Abbrechen". Der Rückgabewert ist die ANTWORT DES ANWENDERS, nicht
    /// der Zustand der Freigabe: Was daraus für die Freigabe folgt, entscheidet
    /// <see cref="KiFreigabe"/> im Kern — eine Oberfläche kann eine verfallene Vorschau
    /// nicht durch ein spätes „Erteilt" wiederbeleben.
    /// </para>
    /// <para>
    /// <b>Ist kein Weg gesetzt, wird nicht geschrieben.</b> Das ist die Vorgabe: Ein
    /// Prozess ohne Chatfenster — Prüflauf, Konsole, Hintergrunddienst — kann den
    /// Anwender nicht fragen und darf deshalb auch nichts ändern.
    /// </para>
    /// </remarks>
    public delegate Task<KiEntscheidung> KiBestaetigungsfrage(KiFreigabe freigabe,
                                                              CancellationToken abbruch);

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
    ///
    /// Sicherheit:
    ///  - der API-Schlüssel liegt DPAPI-verschlüsselt in %APPDATA%\wp-plan
    ///    (gleiches Hausmuster wie die Lizenzablage), nicht mehr im Klartext
    ///    in der Registry; ein Altbestand wird einmalig übernommen und gelöscht
    ///  - der Schlüssel geht in der HTTP-Kopfzeile "x-goog-api-key" mit und
    ///    steht nie in der Adresse - Query-Parameter landen in Proxy- und
    ///    Serverprotokollen
    ///  - SendeVorschau() liefert jederzeit genau den Text, den der nächste
    ///    Aufruf senden würde (Selbstprüfung im Chatfenster)
    ///  - vor JEDER Anfrage steht Einwilligungsriegel(): ohne Einwilligung in den
    ///    Rechtshinweis und bei gesetztem Abschalter der Installation entsteht kein
    ///    einziger Modellaufruf (siehe KiEinwilligung)
    ///
    /// Werkzeugrunde (Etappe 2, Fachkonzept 3.3):
    ///  - TRANSPORT BLEIBT DER ROHE REST-AUFRUF. Das referenzierte Paket
    ///    Mscc.GenerativeAI wird an keiner Stelle des Projekts benutzt; ein
    ///    Umstieg wäre ein eigenes Paket mit eigener Abnahme (Transport, Fehler-
    ///    behandlung, Schlüsselablage, Modellwahl) und wurde hier bewusst NICHT
    ///    mitgemacht. Der Werkzeugkatalog geht deshalb als "tools"/"toolConfig"
    ///    in denselben JSON-Rumpf, den der Hilfefall schon sendet.
    ///  - AUTOMATIC FUNCTION CALLING IST AUS - und zwar nicht durch einen Schalter,
    ///    sondern weil es keinen Bequemweg gibt, der etwas ausführen könnte:
    ///    Tools.AddFunction(Delegate), Tools.Invoke(...) und
    ///    AutomaticFunctionCallingConfig kommen im Code nicht vor. Ausgeführt wird
    ///    ausschließlich über KiAusfuehrer, und erst, nachdem KiRiegel die
    ///    Schutzstufe freigegeben hat. Würde das SDK selbst rufen, wäre die
    ///    Bestätigungsschicht der Etappe 3 von vornherein umgangen.
    ///  - WERKZEUGFÜHRENDE ANFRAGEN GEHEN NIE ÜBER DEN ANTWORT-CACHE. Der Cache
    ///    kennt kein Verfallsdatum; eine gemerkte Antwort würde bei der nächsten
    ///    gleichlautenden Frage den Datenstand von vorhin zeigen.
    ///  - Weg B (Aktionsvorschlag als JSON im Antworttext) ist der Rückfall, wenn
    ///    kein werkzeugfähiges Modell zur Verfügung steht - oder von Hand erzwungen.
    /// </summary>
    public static class KiChatService
    {
        // Registry-Ablage (wie Sprache und CSV-Export-Pfad).
        // ACHTUNG: REG_APIKEY dient nur noch der einmaligen Übernahme des
        // früheren Klartextwerts - geschrieben wird dort nichts mehr.
        private const string REG_SCHLUESSEL = @"Software\wp-plan";
        private const string REG_APIKEY = "GeminiApiKey";
        private const string REG_LIMIT = "KiTageslimit";
        private const string REG_ZAEHLER = "KiZaehler";
        private const string REG_ZAEHLER_TAG = "KiZaehlerTag";
        private const string REG_MODELL = "KiModell";
        private const string REG_WEG_B = "KiWegB";

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

        /// <summary>
        /// Modellfamilien, die Werkzeugaufrufe beherrschen - die gepflegte Positivliste
        /// aus Fachkonzept 3.3, Punkt 6.
        /// </summary>
        /// <remarks>
        /// Die Modell-Liste des Anbieters führt je Modell nur METHODENNAMEN
        /// (<c>supportedGenerationMethods</c>: generateContent, countTokens …), KEIN
        /// Merkmal „werkzeugfähig". Die Annahme des Fachkonzepts löst sich damit
        /// zugunsten der dort vorgesehenen Rückfalllösung auf: dieser Liste. Sie steht
        /// bewusst im Code und nicht in der Registry - eine verstellte Einstellung würde
        /// sonst stillschweigend alle Aktionen abschalten.
        /// </remarks>
        private static readonly string[] WERKZEUG_POSITIV =
        {
            "gemini-1.5-", "gemini-2.0-flash", "gemini-2.5-", "gemini-3"
        };

        /// <summary>
        /// Namensbestandteile, die ein Modell trotz passender Familie ausschließen:
        /// Einbettungs-, Sprach- und Bildmodelle sowie Gemini 2.0 Flash-Lite, das
        /// Werkzeugaufrufe nicht unterstützt.
        /// </summary>
        private static readonly string[] WERKZEUG_NEGATIV =
        {
            "gemini-2.0-flash-lite", "embedding", "gemma", "aqa",
            "tts", "image", "vision", "live"
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

        /// <summary>
        /// Antwortlänge einer Werkzeugrunde. Etwas größer als im reinen Hilfefall:
        /// in einer Runde kann NEBEN dem Text auch ein Werkzeugaufruf stehen.
        /// </summary>
        private const int MAX_ANTWORT_TOKEN_AKTION = 600;
        private const int STANDARD_TAGESLIMIT = 50;

        /// <summary>Basisadresse der Generative-Language-Schnittstelle.</summary>
        private const string BASIS_URL = "https://generativelanguage.googleapis.com/v1beta/";

        /// <summary>
        /// Kopfzeile für den API-Schlüssel. Belegt durch die Anbieterdokumentation
        /// (ai.google.dev, "Using Gemini API keys": -H "x-goog-api-key: ...") und
        /// durch das NuGet-Paket Mscc.GenerativeAI 3.1.0, das dieselbe Kopfzeile setzt.
        /// </summary>
        private const string HEADER_APIKEY = "x-goog-api-key";

        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        // Lokaler Antwort-Cache (Frage + Kontext -> Antwort), spart Kosten
        private static readonly Dictionary<string, KiAntwort> _cache = new Dictionary<string, KiAntwort>();

        // ------------------------------------------------------------------
        // Konfiguration
        // ------------------------------------------------------------------

        /// <summary>
        /// API-Schlüssel des Anbieters. Ablage DPAPI-verschlüsselt im Dateisystem,
        /// siehe Abschnitt "Schlüsselablage" weiter unten.
        /// </summary>
        public static string ApiKey
        {
            get { return SchluesselLesen(); }
            set { SchluesselSchreiben(value ?? ""); }
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
        /// Der EINE Riegel vor jeder Übertragung: Abschalter der Installation und
        /// versionierte Einwilligung in den Rechtshinweis. Rückgabe <c>null</c> = es darf
        /// gesendet werden, sonst der Klartext für den Anwender.
        /// </summary>
        /// <remarks>
        /// Steht bewusst VOR allem anderen - vor Cache, Tageslimit, Schlüsselprüfung und
        /// auch vor dem eingespeisten <see cref="Modellkanal"/>. Nur so lässt sich ohne
        /// Netz nachweisen, dass ohne Einwilligung kein einziger Modellaufruf entsteht.
        /// </remarks>
        private static string Einwilligungsriegel()
        {
            if (KiEinwilligung.Abgeschaltet) return MyResource.Resource.KI_ABSCHALTER_MELDUNG;
            if (!KiEinwilligung.Sicherstellen()) return MyResource.Resource.KI_HINWEIS_ABGELEHNT;
            return null;
        }

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

            string riegel = Einwilligungsriegel();
            if (riegel != null)
            {
                antwort.Fehler = riegel;
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

            string prompt = PromptBauen(frage, kontext, treffer2, verlauf, false, null);
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
        /// Selbstprüfung (A5): liefert genau den Text, den der nächste Aufruf an
        /// den Anbieter senden würde — erzeugt mit demselben Prompt-Baukasten wie
        /// <see cref="FrageAsync"/>, damit Vorschau und Wirklichkeit nicht
        /// auseinanderlaufen können. Es wird nichts gesendet, nichts gezählt und
        /// nichts zwischengespeichert.
        /// </summary>
        public static string SendeVorschau(string frage, string kontext, List<string> verlauf = null,
                                           bool mitAktionen = false, KiRegister register = null)
        {
            string f = string.IsNullOrWhiteSpace(frage) ? "(noch keine Frage eingegeben)" : frage.Trim();
            List<WissensAbschnitt> treffer = HilfeWissen.Suchen(f, kontext, 4);

            if (!mitAktionen) return PromptBauen(f, kontext, treffer, verlauf, false, null);

            // Mit Aktionen wird der VOLLSTÄNDIGE Anfragerumpf gezeigt, nicht nur der
            // Prompt: der Werkzeugkatalog ist der größte Teil dessen, was hinausgeht,
            // und gehört deshalb in die Selbstprüfung.
            KiRegister reg = register ?? KiAusfuehrer.Register;
            bool wegB = WegBErzwingen || WerkzeugModell().Length == 0;

            var gespraech = new List<JsonObject>
            {
                KiWerkzeuge.VerlaufseintragKnoten(KiWerkzeuge.RolleAnwender,
                    KiWerkzeuge.TextteilKnoten(PromptBauen(f, kontext, treffer, verlauf, true,
                                                           wegB ? KiWerkzeuge.WegBAnweisung(reg) : null)))
            };

            var sb = new StringBuilder();
            sb.AppendLine("POST " + BASIS_URL + "models/" + (wegB ? MODELL : WerkzeugModell()) +
                          ":generateContent");
            sb.AppendLine("Kopfzeile " + HEADER_APIKEY + ": (Schlüssel, wird hier nicht gezeigt)");
            sb.AppendLine(wegB ? "Weg B - Aktionsvorschlag als JSON im Antworttext"
                               : "Weg A - Werkzeugkatalog als tools/toolConfig");
            sb.AppendLine();
            sb.Append(AnfrageRumpf(gespraech, reg, wegB, true));
            return sb.ToString();
        }

        /// <summary>
        /// Baut den Prompt: knappe Rolle, Kontext, Hilfeabschnitte, Frage.
        /// Die strikte Bindung an die Abschnitte verhindert erfundene Antworten.
        /// </summary>
        private static string PromptBauen(string frage, string kontext,
                                          List<WissensAbschnitt> abschnitte, List<string> verlauf,
                                          bool mitAktionen, string wegBAnweisung)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Du bist der Hilfe-Assistent der Energieplanungs-Software WP-Plan.");
            sb.AppendLine("Beantworte die Frage kurz, sachlich und auf Deutsch - höchstens 6 Sätze.");
            if (mitAktionen)
            {
                // Die strenge Bindung an die Hilfeabschnitte gilt im Aktionsbetrieb NICHT
                // mehr uneingeschränkt: Zahlen zum Datenbestand kommen dann aus einem
                // Aktionsergebnis. Verboten bleibt das Erfinden.
                sb.AppendLine("Bedienfragen beantwortest du aus den unten stehenden Hilfeabschnitten.");
                sb.AppendLine("Fragen zum Datenbestand beantwortest du über die verfügbaren Aktionen; " +
                              "höchstens EINE Aktion je Antwort und nur, wenn die Frage sie verlangt.");
                sb.AppendLine("Jede Zahl, die du nennst, stammt aus einem Aktionsergebnis oder aus einem " +
                              "Hilfeabschnitt. Erfinde weder Zahlen noch Bezeichner.");
                sb.AppendLine("Bezeichner erscheinen als Platzhalter („Name 1“); übernimm sie unverändert.");
            }
            else
            {
                sb.AppendLine("Stütze dich AUSSCHLIESSLICH auf die unten stehenden Hilfeabschnitte.");
                sb.AppendLine("Steht die Antwort dort nicht, sage das klar und nenne, wo der Benutzer nachsehen kann.");
            }
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

            if (!string.IsNullOrEmpty(wegBAnweisung))
            {
                sb.AppendLine(wegBAnweisung);
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
        private static async Task<string> ModellErmittelnAsync(bool nurWerkzeugfaehig = false)
        {
            try
            {
                // Schlüssel bewusst NICHT als Query-Parameter (A4)
                using (HttpRequestMessage nachricht = new HttpRequestMessage(HttpMethod.Get, BASIS_URL + "models"))
                using (HttpResponseMessage antwort = await SendenAsync(nachricht))
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

                    // 0) Kriterium „werkzeugfähig" (Fachkonzept 3.3, Punkt 6). Die
                    //    Anbieterliste weist es nicht aus - deshalb die Positivliste.
                    if (nurWerkzeugfaehig)
                        verfuegbar = verfuegbar.Where(IstWerkzeugfaehig).ToList();

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
            string url = BASIS_URL + "models/" + modell + ":generateContent";

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

            // Schlüssel bewusst NICHT als Query-Parameter (A4)
            using (HttpRequestMessage nachricht = new HttpRequestMessage(HttpMethod.Post, url))
            {
                nachricht.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpResponseMessage antwort = await SendenAsync(nachricht))
                {
                    string body = await antwort.Content.ReadAsStringAsync();

                    if (!antwort.IsSuccessStatusCode)
                        throw new Exception("HTTP " + (int)antwort.StatusCode + " - " + KurzFehler(body));

                    return TextAusJson(body);
                }
            }
        }

        /// <summary>
        /// Sendet die Anfrage und legt den API-Schlüssel dabei in die Kopfzeile
        /// <c>x-goog-api-key</c>. Er darf nicht in der Adresse stehen: Query-Parameter
        /// werden von Proxys, Gateways und Serverprotokollen mitgeschrieben.
        /// </summary>
        private static Task<HttpResponseMessage> SendenAsync(HttpRequestMessage nachricht)
        {
            nachricht.Headers.TryAddWithoutValidation(HEADER_APIKEY, ApiKey);
            return _http.SendAsync(nachricht);
        }

        /// <summary>Adresse, an die die Anfrage geht - ohne Schlüssel.</summary>
        public static string Endpunkt()
        {
            return BASIS_URL + "models/" + MODELL + ":generateContent";
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

        // ==================================================================
        // Werkzeugrunde (Etappe 2, Fachkonzept 3.3)
        // ==================================================================

        /// <summary>
        /// Prüfpfad OHNE NETZ: ist dieser Kanal gesetzt, liefert er den Antwortrumpf,
        /// den sonst der Anbieter liefern würde — Eingabe ist der vollständige
        /// Anfragerumpf und der Modellname.
        /// </summary>
        /// <remarks>
        /// Damit ist die Werkzeugrunde prüfbar, ohne dass ein Modell gefragt, ein
        /// Schlüssel hinterlegt oder eine Anfrage gezählt wird (Fachkonzept 8/Etappe 2:
        /// „Die Modellanbindung selbst wird NICHT automatisiert getestet"). Der Kanal
        /// bleibt im Betrieb leer; er ersetzt ausschließlich den Transport, nicht die
        /// Auswertung — Absichtserkennung, Riegel, Ausführung und Protokoll laufen im
        /// Prüflauf genau wie im Betrieb.
        /// </remarks>
        public static Func<string, string, CancellationToken, Task<string>> Modellkanal { get; set; }

        /// <summary>
        /// Der Weg zur Bestätigung des Anwenders (Fachkonzept 3.5). <c>null</c> heißt:
        /// Es gibt niemanden zu fragen — dann läuft KEINE Schreibaktion.
        /// </summary>
        /// <remarks>
        /// <c>Form_KiChat</c> setzt ihn beim Öffnen und räumt ihn beim Schließen wieder
        /// ab. Der Aktionsharnisch setzt ihn auf einen Prüfling, der „Ausführen",
        /// „Abbrechen" oder gar nichts antwortet — und weist damit alle Ausgänge nach,
        /// ohne dass ein Fenster nötig wäre.
        /// </remarks>
        public static KiBestaetigungsfrage Bestaetigungsweg { get; set; }

        /// <summary>
        /// Weg B von Hand erzwingen (Einstellung). Ohne diese Angabe entscheidet die
        /// Modellwahl, ob Weg A möglich ist.
        /// </summary>
        public static bool WegBErzwingen
        {
            get { return RegLesen(REG_WEG_B) == "1"; }
            set { RegSchreiben(REG_WEG_B, value ? "1" : "0"); }
        }

        /// <summary>
        /// Beherrscht dieses Modell Werkzeugaufrufe? Grundlage ist die gepflegte
        /// Positivliste, weil die Anbieterliste kein solches Merkmal führt.
        /// </summary>
        public static bool IstWerkzeugfaehig(string modell)
        {
            if (string.IsNullOrWhiteSpace(modell)) return false;
            string m = modell.ToLowerInvariant();

            foreach (string aus in WERKZEUG_NEGATIV)
                if (m.Contains(aus)) return false;

            foreach (string kandidat in MODELL_KANDIDATEN)
                if (string.Equals(m, kandidat, StringComparison.OrdinalIgnoreCase)) return true;

            foreach (string familie in WERKZEUG_POSITIV)
                if (m.StartsWith(familie, StringComparison.Ordinal)) return true;

            return false;
        }

        /// <summary>
        /// Das Modell für die Aktionsrunde: das gemerkte, wenn es werkzeugfähig ist,
        /// sonst der erste werkzeugfähige Kandidat. Leer bedeutet: es gibt keines —
        /// dann fällt der Assistent auf Weg B.
        /// </summary>
        public static string WerkzeugModell()
        {
            string gemerkt = MODELL;
            if (IstWerkzeugfaehig(gemerkt)) return gemerkt;

            foreach (string kandidat in MODELL_KANDIDATEN)
                if (IstWerkzeugfaehig(kandidat)) return kandidat;

            return "";
        }

        /// <summary>
        /// Eine Anwenderäußerung MIT Aktionen: Werkzeugkatalog hinaus, Werkzeugwunsch
        /// herein, Aktion ausführen, Ergebnis zurück — höchstens
        /// <see cref="KiWerkzeuge.Rundendeckel"/> Modellrunden.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Der Ablauf einer Runde: Anfrage bauen (Verlauf + tools + toolConfig AUTO) →
        /// senden → <see cref="KiModellantwort"/> lesen → <see cref="KiAbsicht"/> daraus
        /// einen geprüften <see cref="KiAufruf"/> machen → <see cref="KiRiegel"/> fragen →
        /// <see cref="KiAusfuehrer"/> ausführen → <see cref="KiRueckmeldung"/> als
        /// <c>functionResponse</c> in den Verlauf → nächste Runde. Redet das Modell nur,
        /// ist der Lauf zu Ende.
        /// </para>
        /// <para>
        /// Diese Anfrage geht NIE über den Antwort-Cache — weder lesend noch schreibend.
        /// Der Cache kennt kein Verfallsdatum und würde bei wiederholter Frage den
        /// Datenstand von vorhin zeigen.
        /// </para>
        /// <para>
        /// Seit Etappe 3 fragt der Riegel <c>KiRiegel.PruefeStufe</c> — also die EINE
        /// Grenze im Kern und keine hier eingetragene Stufe mehr. Alles darüber (heute:
        /// Stufe 3, die Rechenaktionen) wird abgewiesen; Stufe 2 geht durch die
        /// Bestätigungsschicht (<see cref="MitBestaetigungAsync"/>) und läuft nur nach
        /// einem Klick des Anwenders.
        /// </para>
        /// </remarks>
        /// <param name="frage">Die Äußerung des Anwenders.</param>
        /// <param name="kontext">Bereichsangabe aus <see cref="HilfeKontext"/>.</param>
        /// <param name="verlauf">Die letzten Wortwechsel (bewusst kurz).</param>
        /// <param name="platzhalter">Bezeichner-Tabelle der Sitzung; <c>null</c> = ohne Platzhalterung.</param>
        /// <param name="register">Register; <c>null</c> = das der Anwendung.</param>
        /// <param name="abbruch">Abbruchmarke.</param>
        public static async Task<KiAntwort> FrageMitAktionenAsync(string frage, string kontext,
                                                                  List<string> verlauf = null,
                                                                  KiPlatzhalter platzhalter = null,
                                                                  KiRegister register = null,
                                                                  CancellationToken abbruch = default)
        {
            KiAntwort antwort = new KiAntwort();

            if (string.IsNullOrWhiteSpace(frage))
            {
                antwort.Fehler = MyResource.Resource.KI_AKT_KEINE_FRAGE;
                return antwort;
            }

            // Der Aktionsbetrieb überträgt MEHR als der Hilfefall (Werkzeugkatalog,
            // Ergebnisse) - er verlangt aber dieselbe Einwilligung, weil der Hinweistext
            // beides ausdrücklich benennt.
            string sperre = Einwilligungsriegel();
            if (sperre != null)
            {
                antwort.Fehler = sperre;
                return antwort;
            }

            KiRegister reg = register ?? KiAusfuehrer.Register;
            bool eingespeist = Modellkanal != null;

            if (!eingespeist && !IstEingerichtet)
            {
                antwort.Fehler = MyResource.Resource.KI_AKT_KEIN_SCHLUESSEL;
                return antwort;
            }

            // ---- Weg wählen. Ohne werkzeugfähiges Modell bleibt nur Weg B.
            bool wegB = WegBErzwingen;
            string modell = MODELL;

            if (wegB)
            {
                antwort.Hinweise.Add(MyResource.Resource.KI_AKT_WEGB_ERZWUNGEN);
            }
            else
            {
                modell = WerkzeugModell();
                if (modell.Length == 0)
                {
                    wegB = true;
                    modell = MODELL;
                    antwort.Hinweise.Add(MyResource.Resource.KI_AKT_WEGB_OHNE_MODELL);
                }
            }

            // ---- Hilfeabschnitte wie im reinen Hilfefall (lokal, kostenlos).
            List<WissensAbschnitt> abschnitte = HilfeWissen.Suchen(frage, kontext, 4);
            foreach (WissensAbschnitt a in abschnitte) antwort.Quellen.Add(a.Titel);

            List<JsonObject> gespraech = new List<JsonObject>();
            gespraech.Add(ErsteRunde(frage, kontext, abschnitte, verlauf, reg, wegB));

            KiRunden runden = new KiRunden();          // Deckel 3 (Fachkonzept 3.3, Festlegung 5)
            string schlusstext = "";

            try
            {
                while (true)
                {
                    if (!runden.Beginne())
                    {
                        antwort.Deckel = true;
                        schlusstext = Anhaengen(schlusstext, runden.Abbruchtext());
                        break;
                    }

                    if (!eingespeist && AnfragenHeute >= Tageslimit)
                    {
                        antwort.Fehler = string.Format(MyResource.Resource.KI_AKT_TAGESLIMIT, Tageslimit);
                        antwort.Runden = runden.Verbraucht;
                        antwort.WegB = wegB;
                        return antwort;
                    }

                    string anfrage = AnfrageRumpf(gespraech, reg, wegB, false);
                    antwort.TokenGeschaetzt += anfrage.Length / 4;   // grobe Schätzung, wie im Hilfefall

                    string rumpf;
                    try
                    {
                        rumpf = await RundeSendenAsync(anfrage, modell, abbruch).ConfigureAwait(true);
                    }
                    catch (Exception ex) when (!wegB && IstModellFehler(ex.Message))
                    {
                        // Das gemerkte Modell gibt es nicht mehr. Erst ein anderes
                        // WERKZEUGFÄHIGES suchen; gibt es keines, wird sichtbar auf
                        // Weg B umgeschaltet — nicht still.
                        string ersatz = await ModellErmittelnAsync(true).ConfigureAwait(true);
                        if (!string.IsNullOrEmpty(ersatz))
                        {
                            MODELL = ersatz;
                            modell = ersatz;
                        }
                        else
                        {
                            wegB = true;
                            modell = MODELL;
                            antwort.Hinweise.Add(MyResource.Resource.KI_AKT_WEGB_OHNE_MODELL);
                            gespraech.Clear();
                            gespraech.Add(ErsteRunde(frage, kontext, abschnitte, verlauf, reg, true));
                        }
                        continue;   // dieselbe Frage, neue Runde
                    }

                    if (!eingespeist) ZaehlerErhoehen();

                    KiModellantwort modellantwort = KiModellantwort.Lesen(rumpf);
                    KiAbsichtBefund befund = wegB
                        ? KiAbsicht.AusText(reg, modellantwort.Text)
                        : KiAbsicht.AusWerkzeugantwort(reg, modellantwort);

                    if (!befund.HatAbsicht)
                    {
                        // Reine Auskunft — der Lauf ist zu Ende.
                        schlusstext = Anhaengen(schlusstext, befund.Text);
                        break;
                    }

                    // Die eigene Runde des Modells MUSS zurück in den Verlauf, sonst weiß
                    // es in der nächsten Runde nicht, worauf sich das Ergebnis bezieht.
                    gespraech.Add(ModellrundeKnoten(modellantwort, wegB));
                    if (befund.Text.Length > 0) schlusstext = Anhaengen(schlusstext, befund.Text);

                    KiSchritt schritt = new KiSchritt { Aktion = befund.Werkzeugname };
                    string rueckmeldung;

                    if (!befund.Gueltig)
                    {
                        // Unbekannte Aktion oder fehlerhafte Parameter: das Modell bekommt
                        // den Klartextgrund zurück und darf EINMAL nachbessern.
                        schritt.Grund = befund.FehlerText();
                        rueckmeldung = KiRueckmeldung.Abgelehnt(befund.Werkzeugname, schritt.Grund);
                    }
                    else
                    {
                        KiAufruf aufruf = befund.Aufruf;
                        schritt.Kurzfassung = KiBestaetigung.Kurzfassung(aufruf);

                        schritt.Bestaetigungspflichtig = KiRiegel.BrauchtBestaetigung(aufruf);

                        string riegel = KiRiegel.PruefeStufe(aufruf);
                        if (riegel != null)
                        {
                            schritt.Grund = riegel;
                            rueckmeldung = KiRueckmeldung.Abgelehnt(aufruf.Name, riegel);
                        }
                        else
                        {
                            KiErgebnis ergebnis = schritt.Bestaetigungspflichtig
                                ? await MitBestaetigungAsync(aufruf, schritt, abbruch).ConfigureAwait(true)
                                : await KiAusfuehrer.AusfuehrenAsync(aufruf, abbruch).ConfigureAwait(true);

                            schritt.Ergebnis = ergebnis;
                            schritt.Ausgefuehrt = ergebnis.Erfolg;
                            if (!ergebnis.Erfolg) schritt.Grund = ergebnis.Text;
                            schritt.Protokollzeile = KiAusfuehrer.LetzteProtokollzeile;

                            rueckmeldung = KiRueckmeldung.Erzeuge(aufruf, ergebnis, platzhalter);
                        }
                    }

                    antwort.Schritte.Add(schritt);
                    gespraech.Add(ErgebnisKnoten(schritt.Aktion, rueckmeldung, wegB));
                }
            }
            catch (OperationCanceledException)
            {
                antwort.Fehler = MyResource.Resource.KI_AKT_ABGEBROCHEN;
                antwort.Runden = runden.Verbraucht;
                antwort.WegB = wegB;
                return antwort;
            }
            catch (Exception ex)
            {
                antwort.Fehler = string.Format(MyResource.Resource.KI_AKT_FEHLER, ex.Message);
                antwort.Runden = runden.Verbraucht;
                antwort.WegB = wegB;
                return antwort;
            }

            antwort.Runden = runden.Verbraucht;
            antwort.WegB = wegB;
            antwort.Erfolg = true;

            // Erst hier zurück in Klarnamen: an das Modell ging die platzgehaltene
            // Fassung, im Chat steht der Klartext (Fachkonzept 4.2).
            string text = platzhalter != null ? platzhalter.Aufloesen(schlusstext) : schlusstext;
            antwort.Text = text.Trim().Length > 0 ? text.Trim() : KiTexte.AntwortLeer;
            return antwort;
        }

        // ------------------------------------------------------------------
        // Bestätigungsschicht (Etappe 3, Fachkonzept 3.5)
        // ------------------------------------------------------------------

        /// <summary>
        /// Vorschau erzeugen, den Anwender fragen, seine Entscheidung einlösen — die
        /// Bestätigungsschicht innerhalb EINER Runde.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Hier wird die durchgehend await-getriebene Runden-Schleife unterbrechbar.</b>
        /// Die Schleife selbst bleibt EINE asynchrone Methode; die Wartezeit auf den Klick
        /// ist schlicht ein weiteres <c>await</c> — auf die Aufgabe, die das Chatfenster
        /// erfüllt, sobald der Anwender eine der beiden Schaltflächen drückt. Drei Dinge
        /// bleiben dadurch unangetastet, die eine zweite Schleife oder ein zweiter Thread
        /// gefährdet hätte: Der Gesprächsverlauf bleibt in einer Hand, der Rundendeckel
        /// zählt unverändert weiter (das Warten liegt INNERHALB der Runde und verbraucht
        /// keine eigene), und die Abbruchmarke wirkt durchgehend.
        /// </para>
        /// <para>
        /// <b>Die Einläufigkeit des Ausführers bleibt gewahrt, ohne die Bedenkzeit zu
        /// sperren.</b> <c>KiAusfuehrer.VorbereitenAsync</c> nimmt die Laufsperre für
        /// Vorbedingung und Vorschau und gibt sie VOR der Bedenkzeit wieder frei;
        /// <c>AusfuehrenAsync</c> nimmt sie danach erneut. Andernfalls wäre der Assistent
        /// eine Minute lang für alles andere blockiert — und wer zwischendurch etwas
        /// fragt, bekäme statt einer Antwort „es läuft bereits etwas".
        /// </para>
        /// <para>
        /// <b>Jeder Ausgang geht durch den Ausführer.</b> Auch „abgelehnt" und „verfallen"
        /// werden dort eingelöst: Er prüft die Freigabe ein zweites Mal — Lizenz,
        /// Sicherungspunkt, Verfall, Laufmarke, Einmaligkeit — und schreibt die EINE
        /// Protokollzeile dieses Versuchs. So bleibt keine Entscheidung unprotokolliert,
        /// und eine Freigabe, die zwischen Klick und Lauf verfallen ist, schreibt trotzdem
        /// nichts.
        /// </para>
        /// </remarks>
        private static async Task<KiErgebnis> MitBestaetigungAsync(KiAufruf aufruf, KiSchritt schritt,
                                                                   CancellationToken abbruch)
        {
            KiBestaetigungsfrage weg = Bestaetigungsweg;
            if (weg == null)
            {
                // Kein Chatfenster, kein Anwender, keine Änderung. Die Vorbereitung wird
                // gar nicht erst angestoßen — so entsteht auch kein Sicherungspunkt für
                // etwas, das ohnehin nicht laufen kann.
                schritt.Entscheidung = KiEntscheidung.Abgelehnt;
                return KiAusfuehrer.AbweisenUndVermerken(aufruf,
                    string.Format(MyResource.Resource.KI_AKT_OHNE_BESTAETIGUNGSWEG, aufruf.Name));
            }

            KiVorbereitung vorbereitung = await KiAusfuehrer.VorbereitenAsync(aufruf, abbruch)
                                                            .ConfigureAwait(true);
            if (!vorbereitung.Bereit) return vorbereitung.Ablehnung;

            KiFreigabe freigabe = vorbereitung.Freigabe;
            schritt.Bestaetigung = freigabe.Text;
            schritt.Sicherungspunkt = KiAusfuehrer.SicherungPfad;

            schritt.Entscheidung = await EntscheidungAbwartenAsync(freigabe, weg, abbruch)
                                         .ConfigureAwait(true);

            return await KiAusfuehrer.AusfuehrenAsync(aufruf, freigabe, abbruch).ConfigureAwait(true);
        }

        /// <summary>
        /// Wartet auf die Entscheidung des Anwenders — höchstens bis zum Verfall der
        /// Vorschau (Fachkonzept 3.5, Punkt 5).
        /// </summary>
        /// <remarks>
        /// Der Verfall wird HIER mitgezählt und nicht der Oberfläche überlassen: Ein
        /// Fenster, dessen Uhr steht, dürfte sonst beliebig lange bestätigen. Antwortet
        /// die Oberfläche zu spät, gewinnt die Frist; antwortet sie gar nicht, endet das
        /// Warten trotzdem.
        /// </remarks>
        private static async Task<KiEntscheidung> EntscheidungAbwartenAsync(
            KiFreigabe freigabe, KiBestaetigungsfrage weg, CancellationToken abbruch)
        {
            TimeSpan rest = freigabe.Restzeit();
            if (rest <= TimeSpan.Zero)
            {
                freigabe.AlsVerfallenMarkieren();
                return freigabe.Stand;
            }

            Task<KiEntscheidung> frage;
            try
            {
                frage = weg(freigabe, abbruch);
            }
            catch (Exception)
            {
                freigabe.Ablehnen();
                return freigabe.Stand;
            }

            if (frage == null)
            {
                freigabe.Ablehnen();
                return freigabe.Stand;
            }

            using (var uhrAus = new CancellationTokenSource())
            {
                // Etwas Nachlauf, damit eine Oberfläche, die ihren eigenen Ablauf meldet,
                // vor der Frist hier ankommt — sonst hätte derselbe Vorgang zwei Namen.
                Task ablauf = Task.Delay(rest + TimeSpan.FromMilliseconds(100), uhrAus.Token);
                Task fertig = await Task.WhenAny(frage, ablauf).ConfigureAwait(true);
                uhrAus.Cancel();

                if (!ReferenceEquals(fertig, frage))
                {
                    freigabe.AlsVerfallenMarkieren();
                    return freigabe.Stand;
                }
            }

            KiEntscheidung antwort;
            try
            {
                antwort = await frage.ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                freigabe.Abbrechen();
                return freigabe.Stand;
            }
            catch (Exception)
            {
                freigabe.Ablehnen();
                return freigabe.Stand;
            }

            // Die Antwort des Anwenders wird auf die Freigabe ANGEWENDET, nicht übernommen:
            // Über Verfall und Einmaligkeit entscheidet der Kern, nicht die Oberfläche.
            switch (antwort)
            {
                case KiEntscheidung.Erteilt: freigabe.Erteilen(); break;
                case KiEntscheidung.Abgebrochen: freigabe.Abbrechen(); break;
                case KiEntscheidung.Verfallen: freigabe.AlsVerfallenMarkieren(); break;
                default: freigabe.Ablehnen(); break;
            }
            return freigabe.Stand;
        }

        // ------------------------------------------------------------------
        // Bausteine der Werkzeugrunde
        // ------------------------------------------------------------------

        /// <summary>Der erste Verlaufseintrag einer Äußerung.</summary>
        private static JsonObject ErsteRunde(string frage, string kontext,
                                             List<WissensAbschnitt> abschnitte, List<string> verlauf,
                                             KiRegister register, bool wegB)
        {
            string prompt = PromptBauen(frage, kontext, abschnitte, verlauf, true,
                                        wegB ? KiWerkzeuge.WegBAnweisung(register) : null);
            return KiWerkzeuge.VerlaufseintragKnoten(KiWerkzeuge.RolleAnwender,
                                                     KiWerkzeuge.TextteilKnoten(prompt));
        }

        /// <summary>
        /// Der vollständige Anfragerumpf einer Runde. Weg A führt zusätzlich
        /// <c>tools</c> und <c>toolConfig</c> (Modus AUTO, niemals ANY).
        /// </summary>
        private static string AnfrageRumpf(List<JsonObject> gespraech, KiRegister register,
                                           bool wegB, bool eingerueckt)
        {
            JsonArray inhalte = new JsonArray();
            foreach (JsonObject eintrag in gespraech) inhalte.Add(eintrag.DeepClone());

            JsonObject wurzel = new JsonObject
            {
                ["contents"] = inhalte,
                ["generationConfig"] = new JsonObject
                {
                    ["temperature"] = 0.2,
                    ["maxOutputTokens"] = MAX_ANTWORT_TOKEN_AKTION
                }
            };

            if (!wegB)
            {
                wurzel["tools"] = KiWerkzeuge.ToolsKnoten(register);
                wurzel["toolConfig"] = KiWerkzeuge.ToolConfigKnoten(KiWerkzeugmodus.Auto);
            }

            return wurzel.ToJsonString(eingerueckt ? RUMPF_LESBAR : RUMPF_KOMPAKT);
        }

        /// <summary>Die eigene Runde des Modells als Verlaufseintrag.</summary>
        private static JsonObject ModellrundeKnoten(KiModellantwort modellantwort, bool wegB)
        {
            // Weg A: der Inhaltsblock geht UNVERÄNDERT zurück. Ein nachgebauter Block
            // könnte den functionCall verlieren, auf den sich die Antwort bezieht.
            if (!wegB && !string.IsNullOrEmpty(modellantwort.InhaltJson))
            {
                try
                {
                    JsonNode knoten = JsonNode.Parse(modellantwort.InhaltJson);
                    JsonObject objekt = knoten as JsonObject;
                    if (objekt != null) return objekt;
                }
                catch (JsonException) { }
            }

            return KiWerkzeuge.VerlaufseintragKnoten(KiWerkzeuge.RolleModell,
                                                     KiWerkzeuge.TextteilKnoten(modellantwort.Text));
        }

        /// <summary>Das Ergebnis einer Aktion auf dem Rückweg.</summary>
        private static JsonObject ErgebnisKnoten(string aktion, string rueckmeldungJson, bool wegB)
        {
            if (!wegB)
                return KiWerkzeuge.VerlaufseintragKnoten(
                    KiWerkzeuge.RolleAnwender,
                    KiWerkzeuge.AntwortteilKnoten(aktion, rueckmeldungJson));

            // Weg B kennt kein functionResponse — das Ergebnis geht als Text zurück.
            return KiWerkzeuge.VerlaufseintragKnoten(
                KiWerkzeuge.RolleAnwender,
                KiWerkzeuge.TextteilKnoten(string.Format(ERGEBNIS_VORSATZ, aktion, rueckmeldungJson)));
        }

        /// <summary>
        /// Sendet eine Runde und liefert den ROHEN Antwortrumpf. Ist
        /// <see cref="Modellkanal"/> gesetzt, wird nichts gesendet.
        /// </summary>
        private static async Task<string> RundeSendenAsync(string anfrageRumpf, string modell,
                                                           CancellationToken abbruch)
        {
            Func<string, string, CancellationToken, Task<string>> kanal = Modellkanal;
            if (kanal != null) return await kanal(anfrageRumpf, modell, abbruch).ConfigureAwait(true);

            string url = BASIS_URL + "models/" + modell + ":generateContent";

            // Schlüssel bewusst NICHT als Query-Parameter (A4)
            using (HttpRequestMessage nachricht = new HttpRequestMessage(HttpMethod.Post, url))
            {
                nachricht.Content = new StringContent(anfrageRumpf, Encoding.UTF8, "application/json");

                using (HttpResponseMessage antwort = await SendenAsync(nachricht).ConfigureAwait(true))
                {
                    string body = await antwort.Content.ReadAsStringAsync().ConfigureAwait(true);

                    if (!antwort.IsSuccessStatusCode)
                        throw new Exception("HTTP " + (int)antwort.StatusCode + " - " + KurzFehler(body));

                    return body;
                }
            }
        }

        /// <summary>Hängt einen Absatz an, ohne Leerzeilen zu häufen.</summary>
        private static string Anhaengen(string text, string zusatz)
        {
            if (string.IsNullOrWhiteSpace(zusatz)) return text ?? "";
            if (string.IsNullOrWhiteSpace(text)) return zusatz.Trim();
            return text.TrimEnd() + "\r\n" + zusatz.Trim();
        }

        /// <summary>
        /// Anweisung, mit der das Ergebnis im Weg B zurückgeht ({0} = Aktion, {1} = JSON).
        /// </summary>
        /// <remarks>
        /// Bleibt eine Konstante: dieser Satz geht an das MODELL, nicht an den Anwender.
        /// Eine übersetzte Anweisung würde die Antwortform verändern, die der Parser
        /// erwartet (gleiche Begründung wie im Klassenkopf von <see cref="KiWerkzeuge"/>).
        /// </remarks>
        private const string ERGEBNIS_VORSATZ =
            "Ergebnis der Aktion {0} (JSON). Fasse es in gewoehnlichem Text zusammen und rufe keine " +
            "weitere Aktion, wenn die Frage damit beantwortet ist:\r\n{1}";

        private static readonly JsonSerializerOptions RUMPF_KOMPAKT = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly JsonSerializerOptions RUMPF_LESBAR = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

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

        private static void RegLoeschen(string wert)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                       Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REG_SCHLUESSEL, true))
                {
                    if (key != null && key.GetValue(wert) != null) key.DeleteValue(wert, false);
                }
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // Schlüsselablage (DPAPI) — Sicherheitsmaßnahme A3
        //
        // Der API-Schlüssel lag bisher im Klartext unter
        // HKCU\Software\wp-plan\GeminiApiKey. Jeder Prozess des Benutzers und
        // jede Registry-Sicherung konnte ihn mitlesen. Er liegt jetzt DPAPI-
        // verschlüsselt als Datei — gleiches Hausmuster wie die Lizenzablage
        // (LizenzManager.TokenLaden/TokenSpeichern, gleicher Ordner).
        //
        // Scope CurrentUser (bewusst abweichend von der Lizenz, die LocalMachine
        // nutzt): Der Schlüssel war bisher benutzerbezogen abgelegt (HKCU) und
        // ist ein persönliches Zugangsmittel mit Kostenfolge. LocalMachine würde
        // jedem Windows-Konto dieses Rechners das Entschlüsseln erlauben und die
        // Vertraulichkeit damit gegenüber heute verschlechtern. Das Lizenz-Token
        // dagegen soll bewusst für alle Konten des Arbeitsplatzes gelten.
        // ------------------------------------------------------------------

        private static readonly object _schluesselSperre = new object();
        private static string _schluessel;
        private static bool _schluesselGeladen;

        /// <summary>
        /// Meldung der einmaligen Übernahme aus der Registry
        /// (leer, wenn nichts zu übernehmen war).
        /// </summary>
        public static string MigrationsProtokoll { get; private set; } = "";

        /// <summary>Ablageordner — derselbe wie bei der Lizenz (%APPDATA%\wp-plan).</summary>
        private static string Verzeichnis()
        {
            string pfad = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "wp-plan");
            Directory.CreateDirectory(pfad);
            return pfad;
        }

        /// <summary>Datei mit dem DPAPI-verschlüsselten API-Schlüssel.</summary>
        public static string SchluesselDatei()
        {
            return Path.Combine(Verzeichnis(), "ki-schluessel.dat");
        }

        private static string SchluesselLesen()
        {
            lock (_schluesselSperre)
            {
                if (_schluesselGeladen) return _schluessel ?? "";
                _schluesselGeladen = true;
                _schluessel = "";

                MigriereAusRegistry();

                try
                {
                    string datei = SchluesselDatei();
                    if (File.Exists(datei))
                    {
                        byte[] klartext = ProtectedData.Unprotect(
                            File.ReadAllBytes(datei), null, DataProtectionScope.CurrentUser);
                        _schluessel = Encoding.UTF8.GetString(klartext);
                    }
                }
                catch (Exception ex)
                {
                    // Nicht entschlüsselbar (anderes Konto, beschädigte Datei):
                    // verhalten wie "kein Schlüssel hinterlegt" — wie im LizenzManager.
                    Protokoll("Schlüssel konnte nicht gelesen werden: " + ex.Message);
                    _schluessel = "";
                }

                return _schluessel;
            }
        }

        private static void SchluesselSchreiben(string wert)
        {
            lock (_schluesselSperre)
            {
                string neu = (wert ?? "").Trim();
                try
                {
                    string datei = SchluesselDatei();
                    if (neu.Length == 0)
                    {
                        if (File.Exists(datei)) File.Delete(datei);
                    }
                    else
                    {
                        byte[] verschluesselt = ProtectedData.Protect(
                            Encoding.UTF8.GetBytes(neu), null, DataProtectionScope.CurrentUser);
                        File.WriteAllBytes(datei, verschluesselt);
                    }
                    _schluessel = neu;
                    _schluesselGeladen = true;

                    // Bei jedem Schreiben: sicherstellen, dass kein Klartext zurückbleibt
                    RegLoeschen(REG_APIKEY);
                }
                catch (Exception ex)
                {
                    Protokoll("Schlüssel konnte nicht gespeichert werden: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Einmalige Übernahme eines noch im Klartext in der Registry liegenden
        /// Schlüssels: verschlüsselt ablegen, Registry-Wert löschen. Läuft beim
        /// ersten Zugriff nach der Umstellung, ohne Rückfrage, protokolliert.
        /// </summary>
        private static void MigriereAusRegistry()
        {
            string alt = null;
            try { alt = RegLesen(REG_APIKEY); } catch { }
            if (string.IsNullOrWhiteSpace(alt)) return;

            try
            {
                string datei = SchluesselDatei();
                if (!File.Exists(datei))
                {
                    byte[] verschluesselt = ProtectedData.Protect(
                        Encoding.UTF8.GetBytes(alt.Trim()), null, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(datei, verschluesselt);
                    RegLoeschen(REG_APIKEY);
                    Protokoll("API-Schlüssel aus der Registry übernommen, verschlüsselt abgelegt (" +
                              datei + "); der Registry-Wert wurde gelöscht.");
                }
                else
                {
                    RegLoeschen(REG_APIKEY);
                    Protokoll("Verschlüsselte Ablage war bereits vorhanden; der verbliebene " +
                              "Klartextwert in der Registry wurde gelöscht.");
                }
            }
            catch (Exception ex)
            {
                Protokoll("Übernahme des Registry-Schlüssels fehlgeschlagen: " + ex.Message);
            }
        }

        private static void Protokoll(string meldung)
        {
            MigrationsProtokoll = meldung ?? "";
            System.Diagnostics.Debug.WriteLine("[KI] " + meldung);
            Console.WriteLine("[KI] " + meldung);
        }
    }
}
