using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KiKern;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Pruefteil der Werkzeugrunde (Fachkonzept 8/Etappe 2) - OHNE NETZ und OHNE MODELL.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Modellanbindung wird bewusst nicht automatisiert getestet (Kosten,
    /// Nichtdeterminismus). Pruefbar ist dagegen alles, was NACH der Antwort geschieht:
    /// Absichtserkennung, Rundendeckel, Schutzstufen-Riegel, Cache-Umgehung,
    /// Platzhalterung und der Aufbau der Anfrage. Dafuer wird
    /// <see cref="KiChatService.Modellkanal"/> gesetzt: er liefert eine EINGESPEISTE
    /// Antwort und merkt sich den Anfragerumpf, den der Dienst gesendet haette.
    /// </para>
    /// <para>
    /// Es wird dabei kein API-Schluessel gebraucht, keine Anfrage gezaehlt und kein Byte
    /// uebertragen. Die Aktionen selbst laufen dagegen ECHT - gegen die Arbeitskopie,
    /// mit Protokollzeile.
    /// </para>
    /// </remarks>
    internal static class Werkzeugrunde
    {
        private const string KONTEXT = "Projektverwaltung";

        /// <summary>Die Anfragerumpfe, die der Dienst gesendet haette - je Runde einer.</summary>
        private static readonly List<string> _anfragen = new List<string>();

        /// <summary>
        /// Dieselben Rumpfe fuer die Schreibrunde (Etappe 3). Sie prueft die
        /// Bestaetigungsschicht mit demselben eingespeisten Kanal - eine zweite
        /// Fassung dieser Helfer waere eine zweite Quelle derselben Regel.
        /// </summary>
        internal static IReadOnlyList<string> Anfragen => _anfragen;

        private static int _geprueft;
        private static int _gefallen;
        private static Protokoll _log;

        // =====================================================================

        internal static void Pruefen(Protokoll log, string protokollDatei, ref int zeilenVorher)
        {
            _log = log;
            _geprueft = 0;
            _gefallen = 0;

            int anfragenVorher = KiChatService.AnfragenHeute;
            bool wegBVorher = KiChatService.WegBErzwingen;

            TextlieferantPruefen();

            try
            {
                zeilenVorher = WegAPruefen(protokollDatei, zeilenVorher);
                zeilenVorher = RundendeckelPruefen(protokollDatei, zeilenVorher);
                zeilenVorher = RiegelPruefen(protokollDatei, zeilenVorher);
                zeilenVorher = CachePruefen(protokollDatei, zeilenVorher);
                zeilenVorher = WegBPruefen(protokollDatei, zeilenVorher);
                VorschauPruefen();
            }
            finally
            {
                KiChatService.Modellkanal = null;
                KiChatService.WegBErzwingen = wegBVorher;
            }

            Pruefe(KiChatService.AnfragenHeute == anfragenVorher,
                   "Kein Modellaufruf gezaehlt (" + anfragenVorher + " vorher, " +
                   KiChatService.AnfragenHeute + " nachher)");

            _log.Leerzeile();
            _log.Zeile("Werkzeugrunde: " + (_geprueft - _gefallen) + " von " + _geprueft + " Pruefungen bestanden.");
        }

        // ============================================================== Textlieferant

        /// <summary>
        /// Der Kern kennt nur Schluessel; die Anwendung beantwortet sie aus MyResource
        /// (Fachkonzept 3.7, Paket B5). Geprueft wird die ECHTE Verdrahtung, nicht eine
        /// nachgebaute - und dass ein unbekannter Schluessel auf die Vorgabe zurueckfaellt.
        /// </summary>
        private static void TextlieferantPruefen()
        {
            _log.Leerzeile();
            _log.Zeile("--- Textlieferant des Kerns (KiTexte.Lieferant -> MyResource) ---");

            string vorgabeVorher = KiTexte.RiegelZu;      // noch ohne Lieferant
            KiTextlieferant.Einrichten();

            Pruefe(KiTexte.Lieferant != null, "Lieferant ist gesetzt");
            Pruefe(!string.IsNullOrEmpty(MyResource.Resource.KI_KERN_RIEGEL_ZU),
                   "Kerntext KI_KERN_RIEGEL_ZU liegt in MyResource");
            Pruefe(KiTexte.RiegelZu == MyResource.Resource.KI_KERN_RIEGEL_ZU,
                   "KiTexte.RiegelZu kommt aus MyResource");
            Pruefe(KiTexte.StufeSchreiben == MyResource.Resource.KI_KERN_STUFE_SCHREIBEN,
                   "KiTexte.StufeSchreiben kommt aus MyResource");
            Pruefe(KiTexte.RiegelZu == vorgabeVorher,
                   "Ressourcentext und Vorgabe des Kerns stimmen ueberein");
            Pruefe(KiTexte.Hole("KI_KERN_GIBT_ES_NICHT", "Vorgabe") == "Vorgabe",
                   "unbekannter Schluessel faellt auf die Vorgabe zurueck");
            Pruefe(!string.IsNullOrEmpty(MyResource.Resource.KI_REG_PROJEKTE_GEFUNDEN),
                   "Registertext KI_REG_PROJEKTE_GEFUNDEN liegt in MyResource");
            Pruefe(!string.IsNullOrEmpty(MyResource.Resource.KI_AUS_LAEUFT_BEREITS),
                   "Ausfuehrertext KI_AUS_LAEUFT_BEREITS liegt in MyResource");
        }

        // ===================================================================== Weg A

        /// <summary>
        /// Der Regelfall: Werkzeugwunsch, Ausfuehrung, Ergebnis zurueck, Textantwort.
        /// Zugleich die Pruefung der Datenschutzschicht (4.2).
        /// </summary>
        private static int WegAPruefen(string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Werkzeugrunde Weg A (eingespeiste Modellantwort, kein Netz) ---");

            Kanal(Werkzeugantwort("projekte_auflisten", "{}"),
                  Textantwort("In der Datenbank stehen mehrere Projekte."));

            var platzhalter = new KiPlatzhalter();
            KiAntwort antwort = Frage("Welche Projekte gibt es?", platzhalter, null);

            Pruefe(!antwort.WegB, "Weg A gewaehlt");
            Pruefe(antwort.Erfolg, "Anfrage erfolgreich: " + Einzeilig(antwort.Fehler));
            Pruefe(antwort.Runden == 2, "2 Runden verbraucht (gemessen: " + antwort.Runden + ")");
            Pruefe(antwort.Schritte.Count == 1, "genau ein Aktionsschritt (gemessen: " + antwort.Schritte.Count + ")");
            Pruefe(!antwort.AusCache, "nicht aus dem Cache bedient");

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 1, "genau eine Protokollzeile (gemessen: " + neu + ")");

            if (antwort.Schritte.Count > 0)
            {
                KiSchritt s = antwort.Schritte[0];
                Pruefe(s.Ausgefuehrt, "Aktion ausgefuehrt: " + Einzeilig(s.Grund));
                Pruefe(s.Aktion == "projekte_auflisten", "richtige Aktion: " + s.Aktion);
                Pruefe(s.Protokollzeile.Length > 0, "Protokollzeile am Schritt vermerkt");
                _log.Roh("      Protokollzeile: " + Einzeilig(s.Protokollzeile));
            }

            // ---- Anfrageaufbau: Katalog mit, Modus AUTO, niemals ANY.
            Pruefe(_anfragen.Count == 2, "zwei Anfragen gebaut (gemessen: " + _anfragen.Count + ")");
            if (_anfragen.Count > 0)
            {
                string erste = _anfragen[0];
                Pruefe(erste.Contains("\"tools\""), "Werkzeugkatalog in der Anfrage");
                Pruefe(erste.Contains("\"functionDeclarations\""), "functionDeclarations vorhanden");
                Pruefe(erste.Contains("\"mode\":\"AUTO\""), "toolConfig im Modus AUTO");
                Pruefe(!erste.Contains("\"ANY\""), "Modus ANY kommt NICHT vor");
                Pruefe(!erste.Contains("automaticFunctionCalling"),
                       "kein Automatic Function Calling in der Anfrage");
                Pruefe(erste.Contains("projekte_auflisten"), "Aktionsname im Katalog");
            }

            // ---- Datenschutz: die Rueckmeldung an das Modell fuehrt KEINEN Klarnamen.
            List<string> klarnamen = Klarnamen(antwort);
            _log.Roh("      Bezeichner aus dem Ergebnis: " + klarnamen.Count +
                     ", Platzhalter angelegt: " + platzhalter.Anzahl);

            if (_anfragen.Count > 1 && klarnamen.Count > 0)
            {
                string zweite = _anfragen[1];
                List<string> gefunden = Enthalten(zweite, klarnamen);
                Pruefe(gefunden.Count == 0,
                       "kein Klarname in der Rueckmeldung an das Modell" +
                       (gefunden.Count > 0 ? " - gefunden: " + string.Join(", ", gefunden) : ""));
                Pruefe(zweite.Contains("functionResponse"), "Ergebnis als functionResponse zurueck");
                Pruefe(zweite.Contains(KiPlatzhalter.Stamm + " 1"), "Platzhalter in der Rueckmeldung");
            }

            _klarnamen = klarnamen;
            _kurzfassungWegA = antwort.Schritte.Count > 0 ? antwort.Schritte[0].Kurzfassung : "";
            return zeilenVorher;
        }

        private static List<string> _klarnamen = new List<string>();
        private static string _kurzfassungWegA = "";

        // ===================================================================== Deckel

        /// <summary>
        /// Ein Modell, das immer wieder dieselbe Aktion ruft, muss nach drei Runden
        /// angehalten werden (Fachkonzept 3.3, Festlegung 5).
        /// </summary>
        private static int RundendeckelPruefen(string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Rundendeckel (Modell ruft immer wieder dieselbe Aktion) ---");

            Kanal(Werkzeugantwort("projekte_auflisten", "{}"));   // immer dasselbe

            KiAntwort antwort = Frage("Und noch einmal alle Projekte?", new KiPlatzhalter(), null);

            Pruefe(antwort.Deckel, "Rundendeckel hat den Lauf beendet");
            Pruefe(antwort.Runden == KiWerkzeuge.Rundendeckel,
                   "genau " + KiWerkzeuge.Rundendeckel + " Runden (gemessen: " + antwort.Runden + ")");
            Pruefe(antwort.Schritte.Count == KiWerkzeuge.Rundendeckel,
                   KiWerkzeuge.Rundendeckel + " Aktionsschritte (gemessen: " + antwort.Schritte.Count + ")");
            Pruefe(antwort.Text.Length > 0, "Abbruch mit Klartext");
            _log.Roh("      Abbruchtext: " + Einzeilig(antwort.Text));

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == KiWerkzeuge.Rundendeckel,
                   KiWerkzeuge.Rundendeckel + " Protokollzeilen (gemessen: " + neu + ")");
            return zeilenVorher;
        }

        // ===================================================================== Riegel

        /// <summary>
        /// Der Riegel der Etappe 3 hat zwei Kanten (Fachkonzept 4.1): Stufe 3 ist
        /// ueberhaupt nicht freigegeben, Stufe 2 laeuft nur nach einem Klick. Geprueft
        /// wird beides an Probeaktionen aus einem eigenen Register - laeuft eine von
        /// ihnen, steht der Riegel offen.
        /// </summary>
        private static int RiegelPruefen(string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Schutzstufen-Riegel (Probeaktionen der Stufen 2 und 3) ---");

            // ---- Stufe 3: gar nicht erst freigegeben.
            bool gelaufen3 = false;
            var probe3 = new KiRegister();
            probe3.Aufnehmen(new KiAktion(
                "probe_rechnen",
                "Probeaktion des Harnischs; Stufe 3 kommt erst mit Etappe 4.",
                Schutzstufe.Rechnen,
                "keiner",
                ausfuehren: a => { gelaufen3 = true; return KiErgebnis.Ok("DIESE ZEILE DARF ES NICHT GEBEN"); },
                vorschau: a => "Ich wuerde rechnen."));

            Kanal(Werkzeugantwort("probe_rechnen", "{}"),
                  Textantwort("Das darf ich noch nicht."));

            KiAntwort a3 = Frage("Bitte rechnen.", new KiPlatzhalter(), probe3);

            Pruefe(!gelaufen3, "die Rechenaktion ist NICHT gelaufen");
            Pruefe(a3.Schritte.Count == 1, "ein Schritt vermerkt (gemessen: " + a3.Schritte.Count + ")");
            if (a3.Schritte.Count > 0)
            {
                Pruefe(!a3.Schritte[0].Ausgefuehrt, "Schritt als nicht ausgefuehrt vermerkt");
                Pruefe(a3.Schritte[0].Grund.Length > 0, "Klartextgrund vorhanden");
                _log.Roh("      Grund: " + Einzeilig(a3.Schritte[0].Grund));
            }
            if (_anfragen.Count > 1)
                Pruefe(_anfragen[1].Contains("abgelehnt"),
                       "Ablehnung geht als functionResponse an das Modell zurueck");

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 0, "keine Protokollzeile - es lief ja nichts (gemessen: " + neu + ")");

            // ---- Stufe 2: freigegeben, aber ohne Weg zur Bestaetigung laeuft nichts.
            //      Genau das ist die Lage in jedem Prozess ohne Chatfenster.
            bool gelaufen2 = false;
            var probe2 = new KiRegister();
            probe2.Aufnehmen(new KiAktion(
                "probe_schreiben",
                "Probeaktion des Harnischs; sie darf ohne Bestaetigung nicht laufen.",
                Schutzstufe.Schreiben,
                "keiner",
                ausfuehren: a => { gelaufen2 = true; return KiErgebnis.Ok("DIESE ZEILE DARF ES NICHT GEBEN"); },
                vorschau: a => "Ich wuerde etwas anlegen."));

            KiBestaetigungsfrage wegVorher = KiChatService.Bestaetigungsweg;
            KiChatService.Bestaetigungsweg = null;

            Kanal(Werkzeugantwort("probe_schreiben", "{}"),
                  Textantwort("Dafuer brauche ich Ihre Bestaetigung."));

            KiAntwort a2;
            try
            {
                a2 = Frage("Bitte etwas schreiben.", new KiPlatzhalter(), probe2);
            }
            finally
            {
                KiChatService.Bestaetigungsweg = wegVorher;
            }

            Pruefe(!gelaufen2, "die Schreibaktion ist OHNE Bestaetigungsweg NICHT gelaufen");
            Pruefe(a2.Schritte.Count == 1, "ein Schritt vermerkt (gemessen: " + a2.Schritte.Count + ")");
            if (a2.Schritte.Count > 0)
            {
                Pruefe(a2.Schritte[0].Bestaetigungspflichtig, "als bestaetigungspflichtig vermerkt");
                Pruefe(!a2.Schritte[0].Ausgefuehrt, "Schritt als nicht ausgefuehrt vermerkt");
                _log.Roh("      Grund: " + Einzeilig(a2.Schritte[0].Grund));
            }
            if (_anfragen.Count > 1)
                Pruefe(_anfragen[1].Contains("abgelehnt"),
                       "Ablehnung geht als functionResponse an das Modell zurueck");

            neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 1, "genau eine Protokollzeile fuer den Versuch (gemessen: " + neu + ")");

            return zeilenVorher;
        }

        // ===================================================================== Cache

        /// <summary>
        /// Dieselbe Frage zweimal, mit unterschiedlicher eingespeister Antwort. Kaeme die
        /// zweite aus dem Cache, stuende dort noch die erste - genau das darf im
        /// Aktionsbetrieb nicht passieren.
        /// </summary>
        private static int CachePruefen(string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Cache-Umgehung (gleiche Frage, andere Antwort) ---");

            const string frage = "Wie viele Projekte sind es?";

            Kanal(Textantwort("Erste Antwort."));
            KiAntwort a1 = Frage(frage, new KiPlatzhalter(), null);

            Kanal(Textantwort("Zweite Antwort."));
            KiAntwort a2 = Frage(frage, new KiPlatzhalter(), null);

            Pruefe(a1.Text.Contains("Erste"), "erste Antwort geliefert: " + Einzeilig(a1.Text));
            Pruefe(a2.Text.Contains("Zweite"), "zweite Antwort NICHT aus dem Cache: " + Einzeilig(a2.Text));
            Pruefe(!a2.AusCache, "AusCache bleibt falsch");

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 0, "keine Protokollzeile ohne Aktion (gemessen: " + neu + ")");
            return zeilenVorher;
        }

        // ===================================================================== Weg B

        /// <summary>
        /// Bei abgeschaltetem Werkzeugpfad muss Weg B DENSELBEN Aufruf liefern
        /// (Fachkonzept 8/Etappe 2, Abnahme).
        /// </summary>
        private static int WegBPruefen(string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Weg B (Aktionsvorschlag als JSON im Antworttext) ---");

            KiChatService.WegBErzwingen = true;

            Kanal(Textantwort("Ich sehe nach.\n```json\n{\"aktion\":\"projekte_auflisten\",\"parameter\":{}}\n```"),
                  Textantwort("In der Datenbank stehen mehrere Projekte."));

            KiAntwort antwort = Frage("Welche Projekte gibt es?", new KiPlatzhalter(), null);

            Pruefe(antwort.WegB, "Weg B gewaehlt");
            Pruefe(antwort.Schritte.Count == 1, "ein Aktionsschritt (gemessen: " + antwort.Schritte.Count + ")");

            string kurz = antwort.Schritte.Count > 0 ? antwort.Schritte[0].Kurzfassung : "";
            Pruefe(kurz == _kurzfassungWegA,
                   "derselbe Aufruf wie in Weg A (A: '" + _kurzfassungWegA + "', B: '" + kurz + "')");

            if (_anfragen.Count > 0)
            {
                Pruefe(!_anfragen[0].Contains("\"tools\""), "kein Werkzeugkatalog in der Anfrage");
                Pruefe(_anfragen[0].Contains("Verfuegbare Aktionen"), "Katalog als Text im Prompt");
            }
            if (_anfragen.Count > 1)
                Pruefe(!_anfragen[1].Contains("functionResponse"), "Ergebnis als Text statt functionResponse");

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 1, "genau eine Protokollzeile (gemessen: " + neu + ")");

            KiChatService.WegBErzwingen = false;
            return zeilenVorher;
        }

        // ===================================================================== Vorschau

        /// <summary>
        /// „Was wird gesendet?" muss AUCH mit Werkzeugkatalog ohne Projekt-, Kunden- und
        /// Anlagennamen auskommen (Fachkonzept 4.2).
        /// </summary>
        private static void VorschauPruefen()
        {
            _log.Leerzeile();
            _log.Zeile("--- Selbstpruefung „Was wird gesendet?\" mit Werkzeugkatalog ---");

            string vorschau = KiChatService.SendeVorschau("Welche Projekte gibt es?", KONTEXT, null, true);

            Pruefe(vorschau.Contains("functionDeclarations"), "Werkzeugkatalog wird gezeigt");
            Pruefe(!vorschau.Contains(KiChatService.ApiKey ?? " ") || string.IsNullOrEmpty(KiChatService.ApiKey),
                   "kein Schluessel in der Vorschau");

            List<string> gefunden = Enthalten(vorschau, _klarnamen);
            Pruefe(gefunden.Count == 0,
                   "kein Klarname in der Vorschau (" + _klarnamen.Count + " Bezeichner geprueft)" +
                   (gefunden.Count > 0 ? " - gefunden: " + string.Join(", ", gefunden) : ""));

            _log.Roh("      Vorschaulaenge: " + vorschau.Length + " Zeichen");
        }

        // ===================================================================== Hilfen

        private static KiAntwort Frage(string text, KiPlatzhalter platzhalter, KiRegister register)
        {
            try
            {
                return KiChatService
                    .FrageMitAktionenAsync(text, KONTEXT, null, platzhalter, register, CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.FehlerZeile("Werkzeugrunde: AUSNAHME nach aussen durchgeschlagen - " + ex);
                return new KiAntwort { Fehler = ex.Message };
            }
        }

        /// <summary>Speist die Antworten ein; die letzte gilt fuer alle weiteren Runden.</summary>
        internal static void Kanal(params string[] antworten)
        {
            _anfragen.Clear();
            int i = 0;
            KiChatService.Modellkanal = delegate (string rumpf, string modell, CancellationToken tok)
            {
                _anfragen.Add(rumpf);
                string a = antworten[Math.Min(i, antworten.Length - 1)];
                i++;
                return Task.FromResult(a);
            };
        }

        internal static string Werkzeugantwort(string name, string argumenteJson)
        {
            return "{\"candidates\":[{\"content\":{\"role\":\"model\",\"parts\":[{\"functionCall\":{\"name\":" +
                   JsonSerializer.Serialize(name) + ",\"args\":" + argumenteJson +
                   "}}]},\"finishReason\":\"STOP\"}]}";
        }

        internal static string Textantwort(string text)
        {
            return "{\"candidates\":[{\"content\":{\"role\":\"model\",\"parts\":[{\"text\":" +
                   JsonSerializer.Serialize(text) + "}]},\"finishReason\":\"STOP\"}]}";
        }

        /// <summary>Alle Bezeichner aus den Ergebniszeilen - das, was NICHT hinausgehen darf.</summary>
        private static List<string> Klarnamen(KiAntwort antwort)
        {
            var namen = new List<string>();
            foreach (KiSchritt s in antwort.Schritte)
            {
                if (s.Ergebnis == null) continue;
                foreach (IReadOnlyDictionary<string, object> zeile in s.Ergebnis.Zeilen)
                    foreach (KeyValuePair<string, object> feld in zeile)
                    {
                        string wert = feld.Value as string;
                        if (string.IsNullOrWhiteSpace(wert)) continue;
                        if (wert.Length < 4) continue;          // zu kurz, um ein Bezeichner zu sein
                        if (!namen.Contains(wert)) namen.Add(wert);
                    }
            }
            return namen;
        }

        private static List<string> Enthalten(string text, List<string> namen)
        {
            var treffer = new List<string>();
            foreach (string n in namen)
                if (text.IndexOf(n, StringComparison.Ordinal) >= 0) treffer.Add(n);
            return treffer;
        }

        private static int Neue(string protokollDatei, ref int zeilenVorher)
        {
            int jetzt = Protokollzeilen(protokollDatei);
            int neu = jetzt - zeilenVorher;
            zeilenVorher = jetzt;
            return neu;
        }

        private static int Protokollzeilen(string datei)
        {
            if (!System.IO.File.Exists(datei)) return 0;
            try
            {
                int n = 0;
                foreach (string z in System.IO.File.ReadLines(datei, System.Text.Encoding.UTF8))
                    if (z.Length > 0 && !z.StartsWith("#", StringComparison.Ordinal)) n++;
                return n;
            }
            catch { return 0; }
        }

        private static void Pruefe(bool bedingung, string was)
        {
            _geprueft++;
            if (bedingung)
            {
                _log.Roh("      OK      " + was);
            }
            else
            {
                _gefallen++;
                _log.FehlerZeile("Werkzeugrunde: " + was);
            }
        }

        private static string Einzeilig(string text)
        {
            return (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }
}
