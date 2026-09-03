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
    /// Rohfrage (<see cref="Stichwortliste"/>). Grundlage ist die Ableitung aus
    /// <see cref="HilfeWissen"/> - Woerter ab vier Zeichen, kleingeschrieben,
    /// ohne Wiederholung -, seit H9 zusaetzlich ohne deutsche Fuellwoerter
    /// (<see cref="STOPPWOERTER"/>). Empfaenger ist der eigene Server
    /// <c>wiki.epos-plan.de</c> ueber TLS und ohne Anmeldung - ein ZWEITER
    /// Datenfluss neben dem Modellanbieter, der nach Entscheid 7.4 auch im
    /// Betrieb ohne KI stattfindet und deshalb im Rechtshinweis benannt ist
    /// (Entscheid 7.5, Fassung der Einwilligung bleibt unveraendert). Die
    /// Filterung sendet damit WENIGER als vorher - datenschutzrechtlich eine
    /// Verbesserung, kein neuer Datenfluss.
    /// </para>
    /// <para>
    /// <b>Warum weniger mehr findet (H9, 29.08.2026).</b> Die Volltextsuche
    /// <c>rest.php/v1/search/page</c> verlangt ALLE Terme auf derselben Seite.
    /// Ein einziges Fuellwort in der Kette macht die Trefferliste leer:
    /// gemessen an "wie kann der Warmwasserbedarf angelegt werden" -&gt;
    /// "kann Warmwasserbedarf angelegt werden" -&gt; 0 Treffer, waehrend
    /// "Warmwasserbedarf" allein sofort die richtigen Seiten liefert. Deshalb
    /// erst die Stoppwortliste und dann die Rueckfall-Kaskade
    /// (<see cref="Suchstufen"/>): alle Stichwoerter, sonst die zwei laengsten,
    /// sonst das laengste.
    /// </para>
    /// <para>
    /// <b>Zweite Stufe seit H10 (30.08.2026): Bedeutung statt Buchstaben.</b>
    /// Die Stichwortsuche bleibt fuehrend; was sie nicht findet, ergaenzt der
    /// oertliche Einbettungs-Index (<see cref="SemantikIndex"/>) - er kennt
    /// „Akku" als „Stromspeicher", ohne dass eine Weiterleitung dafuer angelegt
    /// waere. <b>Datenschutzlich aendert sich dadurch nichts</b>: Frage und
    /// Wiki-Text werden AUSSCHLIESSLICH auf diesem Rechner eingebettet, es
    /// entsteht kein neuer Empfaenger und keine neue Uebertragung; hinaus geht
    /// weiterhin nur die Stichwortliste an <c>wiki.epos-plan.de</c>. Steht kein
    /// Index (Modell fehlt, Aufbau laeuft noch, Netz weg), verhaelt sich die
    /// Suche Zeile fuer Zeile wie unter H9.
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

        /// <summary>Alle Adressen der letzten Kaskade, in der Reihenfolge des Versands.</summary>
        private static List<string> _letzteSuchAdressen = new List<string>();

        /// <summary>
        /// Saemtliche Such-Adressen des letzten Laufs (eine je Stufe, hoechstens
        /// drei) - Nachweis, dass die Kaskade greift und dass in KEINER Stufe ein
        /// Fuellwort mitgeht. Reine Diagnose, wie
        /// <see cref="LetzteSuchAdresse"/>; beide sind prozessweit und beim
        /// gleichzeitigen Suchen aus zwei Fenstern entsprechend unzuverlaessig -
        /// auf ihnen haengt keine Programmlogik.
        /// </summary>
        public static IReadOnlyList<string> LetzteSuchAdressen
        {
            get { return _letzteSuchAdressen; }
        }

        /// <summary>
        /// Nummer der Kaskadenstufe, aus der die zuletzt gelieferte Trefferliste
        /// stammt - gezaehlt wird die Stelle in <see cref="Suchstufen"/>, also
        /// 1 fuer die erste TATSAECHLICH gesendete Anfrage. Weil deckungsgleiche
        /// Stufen entfallen, ist 2 nicht zwingend "die zwei laengsten"; sicher
        /// ist nur: 1 = die volle Stichwortliste, alles darueber = ein Rueckfall.
        /// 0 heisst, es wurde nichts gesendet.
        /// </summary>
        public static int LetzteSuchStufe { get; private set; }

        /// <summary>
        /// Wie viele Seiten der letzten Trefferliste NUR ueber die semantische
        /// Stufe hereinkamen - reine Diagnose wie <see cref="LetzteSuchStufe"/>,
        /// keine Programmlogik haengt daran.
        /// </summary>
        public static int LetzteSemantikTreffer { get; private set; }

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
            try { wert = Dienste.Einstellungen.Lies(EINSTELLUNG_BASIS); }
            catch (Exception ex) { Debug.WriteLine("[Wiki] Einstellwert nicht lesbar: " + ex.Message); }

            if (string.IsNullOrWhiteSpace(wert)) wert = WIKI_STANDARD;
            return wert.Trim().TrimEnd('/');
        }

        /// <summary>
        /// Name des Einstellwerts mit der Basis-URL. Er heisst aus
        /// Vertraeglichkeitsgruenden weiterhin <c>WordPressUrl</c> — eine Umbenennung
        /// wuerde gespeicherte Anwenderwerte in der <c>user.config</c> verwerfen
        /// (Entscheid 7.3 des Hilfekonzepts).
        /// </summary>
        public const string EINSTELLUNG_BASIS = "WordPressUrl";

        /// <summary>
        /// Not-Rueckfall fuer die Basis-URL der Wiki-Dokumentation, falls der
        /// Einstellwert leer ist (A2). Derselbe Wert steht als Werksvorgabe in der
        /// <c>app.config</c>; <c>Program.WIKI_STANDARD</c> ist seit iU5 nur noch die
        /// Weiterleitung hierher.
        /// </summary>
        public const string WIKI_STANDARD = "https://wiki.epos-plan.de";

        // ==================================================================
        //  Stichwoerter (B1.1) - die Rohfrage verlaesst den Rechner NICHT
        // ==================================================================

        /// <summary>Trennzeichen wie in <c>HilfeWissen.Zerlegen</c>.</summary>
        private static readonly char[] TRENNER =
            { ' ', '\t', '\r', '\n', ',', ';', '.', '?', '!', ':', '(', ')', '"', '\'', '/', '-' };

        /// <summary>
        /// Deutsche Fuellwoerter, die aus der Stichwortliste fallen (H9).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Bewusst konservativ.</b> Aufgenommen sind nur Modal- und
        /// Hilfsverben sowie Funktionswoerter - also Woerter, die auf JEDER
        /// Seite stehen koennen und deshalb nichts eingrenzen, aber die
        /// UND-Suche des Wikis leerlaufen lassen. FACHVERBEN
        /// ("anlegen", "importieren", "simulieren", "berechnen") bleiben
        /// ausdruecklich drin: sie treffen oft genau die richtige Seite. Was
        /// dennoch zuviel ist, faengt die Kaskade in <see cref="Suchstufen"/> ab.
        /// </para>
        /// <para>
        /// Woerter unter vier Zeichen ("wie", "der", "das", "ist") entfernt schon
        /// die Laengenregel; sie stehen hier nur, wo eine Umlautform ueber die
        /// Grenze kommt. Umlaute werden in beiden Schreibweisen gefuehrt (ue/ü),
        /// weil Anwender beides tippen und <see cref="string.ToLowerInvariant"/>
        /// nicht umschreibt.
        /// </para>
        /// </remarks>
        internal static readonly HashSet<string> STOPPWOERTER =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Modal- und Hilfsverben
            "kann", "kannst", "koennen", "können", "koennte", "könnte",
            "werden", "wird", "wurde", "wurden", "wuerde", "würde",
            "soll", "sollen", "sollte", "muss", "muessen", "müssen",
            "moechte", "möchte", "machen", "macht", "gibt", "geben",
            "haben", "habe", "sein", "sind",
            // Artikel, Pronomen, Bestimmwoerter ab vier Zeichen
            "eine", "einen", "einem", "einer", "eines",
            "welche", "welcher", "welches",
            "dieser", "diese", "dieses", "sich",
            // Praepositionen, Konjunktionen, Partikeln
            "auch", "oder", "aber", "nicht", "beim", "ueber", "über",
            "unter", "fuer", "für", "ohne", "nach", "wenn", "dass",
            "damit", "denn", "dann", "bitte", "viele", "mehr",
            // Fragewoerter ab vier Zeichen
            "wofuer", "wofür", "wozu", "wieso", "warum", "weshalb"
        };

        /// <summary>
        /// Die Stichwoerter einer Frage: Woerter ab vier Zeichen, kleingeschrieben,
        /// ohne Wiederholung - die Regel, nach der auch <see cref="HilfeWissen"/>
        /// lokal bewertet (dort <c>if (w.Length &lt; 4) continue;</c>) - und seit
        /// H9 zusaetzlich ohne die Fuellwoerter aus <see cref="STOPPWOERTER"/>.
        /// </summary>
        /// <remarks>
        /// Bestuende eine Frage AUSSCHLIESSLICH aus Fuellwoertern ("was kann das
        /// denn"), ginge sonst gar nichts mehr hinaus - dann gilt weiter die
        /// ungefilterte Liste. Der Unterschied zu <see cref="HilfeWissen"/> ist
        /// gewollt: dort bewertet jedes Wort nur mit, hier entscheidet es ueber
        /// Treffer oder Leere.
        /// </remarks>
        public static string[] Stichwoerter(string frage)
        {
            if (string.IsNullOrWhiteSpace(frage)) return new string[0];

            string[] roh = frage.ToLowerInvariant()
                                .Split(TRENNER, StringSplitOptions.RemoveEmptyEntries)
                                .Where(w => w.Length >= 4)
                                .Distinct()
                                .ToArray();

            string[] ohneFuellwoerter = roh.Where(w => !STOPPWOERTER.Contains(w)).ToArray();

            return (ohneFuellwoerter.Length > 0 ? ohneFuellwoerter : roh)
                   .Take(MAX_STICHWOERTER)
                   .ToArray();
        }

        /// <summary>
        /// Die Stufen der Rueckfall-Kaskade zu einer Frage (H9), in der
        /// Reihenfolge, in der sie versucht werden: (a) alle Stichwoerter,
        /// (b) die zwei laengsten, (c) das laengste - jede Stufe als fertige
        /// Stichwortliste.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Die Laenge ist der Ersatz fuer eine Gewichtung, die es hier nicht
        /// gibt: das laengste Wort einer Frage ist im Deutschen fast immer das
        /// Fachwort ("Warmwasserbedarf" gegen "angelegt"). Bei GLEICHER Laenge
        /// gewinnt die fruehere Stelle in der Frage -
        /// <see cref="Enumerable.OrderByDescending{TSource,TKey}(IEnumerable{TSource},Func{TSource,TKey})"/>
        /// ist stabil und <see cref="Stichwoerter"/> liefert in Fragereihenfolge.
        /// </para>
        /// <para>
        /// Stufen, die dieselbe Wortmenge ergaeben wie eine vorige, entfallen -
        /// eine Frage mit einem einzigen Stichwort hat deshalb genau eine Stufe
        /// und kostet genau einen Aufruf. Mehr als drei Stufen kann es nicht
        /// geben.
        /// </para>
        /// </remarks>
        internal static List<string> Suchstufen(string frage)
        {
            List<string> stufen = new List<string>();

            string[] worte = Stichwoerter(frage);
            if (worte.Length == 0) return stufen;

            string[] nachLaenge = worte.OrderByDescending(w => w.Length).ToArray();

            List<string[]> mengen = new List<string[]>();
            StufeAnfuegen(mengen, worte);                 // (a) alles
            StufeAnfuegen(mengen, nachLaenge.Take(2));    // (b) die zwei laengsten
            StufeAnfuegen(mengen, nachLaenge.Take(1));    // (c) nur das laengste

            foreach (string[] menge in mengen) stufen.Add(string.Join(" ", menge));
            return stufen;
        }

        /// <summary>
        /// Haengt eine Stufe an, sofern ihre Wortmenge nicht schon vorkommt.
        /// Verglichen wird die MENGE, nicht die Zeichenkette - Stufe (b) reiht
        /// nach Laenge, Stufe (a) nach Fragestellung, bei zwei Woertern waere das
        /// sonst zweimal dieselbe Anfrage in anderer Reihenfolge.
        /// </summary>
        private static void StufeAnfuegen(List<string[]> mengen, IEnumerable<string> worte)
        {
            string[] neu = worte.ToArray();
            if (neu.Length == 0) return;

            foreach (string[] alt in mengen)
                if (alt.Length == neu.Length && !neu.Except(alt, StringComparer.Ordinal).Any())
                    return;

            mengen.Add(neu);
        }

        /// <summary>Die Stichwoerter als eine Zeichenkette - genau das, was gesendet wird.</summary>
        public static string Stichwortliste(string frage)
        {
            return string.Join(" ", Stichwoerter(frage));
        }

        // ==================================================================
        //  Adressen
        // ==================================================================

        /// <summary>
        /// Adresse der Volltextsuche (REST, mit Abschnitts-Ankern) zur ERSTEN
        /// Stufe einer Frage. Welche Stufen tatsaechlich abgefragt werden, sagt
        /// <see cref="Suchstufen"/>.
        /// </summary>
        public static string SuchAdresse(string basis, string frage)
        {
            return SuchAdresseFuer(basis, Stichwortliste(frage));
        }

        /// <summary>
        /// Dieselbe Adresse zu einer fertigen Stichwortliste - der Weg der
        /// Kaskade, deren spaetere Stufen nicht mehr aus der ganzen Frage
        /// entstehen.
        /// </summary>
        public static string SuchAdresseFuer(string basis, string stichwortliste)
        {
            return basis + "/rest.php/v1/search/page?q=" +
                   Uri.EscapeDataString(stichwortliste ?? "") + "&limit=" + SUCH_TREFFER;
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
        /// <para>
        /// Ohne Eintrag bleibt nur noch <c>Unbekannter Bereich</c>.
        /// <c>Ergebnis</c> steht nicht in der Positivliste, ist aber die
        /// Bereichsangabe mehrerer eingebauter Abschnitte - der Eintrag schadet
        /// nicht und trifft, falls der Bereich einmal gesetzt wird.
        /// </para>
        /// <para>
        /// <b>Nachgezogen mit H7</b> (29.08.2026), weil die Rubrik neun Unterseiten
        /// dazubekommt: <c>Bericht</c> und <c>Lizenz</c> hatten bis dahin keine Seite
        /// und fehlten deshalb; <c>Bericht</c> zeigt auf <c>Berichte und Kosten</c>,
        /// weil der Bereich den ganzen Reiter umfasst (Uebersicht, Kosten,
        /// Wirtschaftlichkeit, Bericht) und nicht nur dessen letzte Seite.
        /// <c>Wärmequelle Erdreich (...)</c> zeigte behelfsweise auf
        /// <c>Wärmepumpe</c> und <c>Detaillierte Simulation</c> auf
        /// <c>Simulation</c> - beide haben jetzt eine eigene Seite.
        /// </para>
        /// </remarks>
        private static readonly Dictionary<string, string> SEITE_JE_BEREICH =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Administration",           "Einstellungen" },
            { "Bericht",                  "Berichte und Kosten" },
            { "BHKW",                     "BHKW" },
            { "Brauchwasser",             "Brauchwasser" },
            { "Detaillierte Simulation",  "Simulationsergebnisse" },
            { "Ergebnis",                 "Simulation" },
            { "Gebäude",                  "Gebäude" },
            { "Hauptfenster",             "Programmablauf" },
            { "Heizkessel",               "Heizkessel" },
            { "Hilfe",                    "Hilfe-Assistent" },
            { "Klimadaten",               "Klimadaten" },
            { "Kosten und Preise",        "Kosten" },
            { "Lizenz",                   "Lizenz" },
            { "Photovoltaik",             "Photovoltaik" },
            { "Projektassistent",         "Kurzanleitung" },
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
            { "Wärmequelle Erdreich (Quellsystem, Bodentyp, Auslegungsprüfung VDI 4640)", "Wärmequelle Erdreich" },
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
        internal static Task<List<WissensAbschnitt>> SucheAsync(string basis, string frage,
                                                                string kontext,
                                                                CancellationToken abbruch)
        {
            return SucheAsync(basis, frage, kontext, true, abbruch);
        }

        /// <summary>
        /// Dieselbe Suche mit abschaltbarer Stichwortstufe. <c>false</c> laesst die
        /// H9-Kaskade AUS und misst damit, was die semantische Stufe fuer sich
        /// allein traegt - der Weg des Pruefharnischs (H10-Beweis 3). Im Programm
        /// wird ausschliesslich mit <c>true</c> aufgerufen; es gibt keinen
        /// Schalter dafuer und keinen Einstellwert.
        /// </summary>
        internal static async Task<List<WissensAbschnitt>> SucheAsync(string basis, string frage,
                                                                      string kontext,
                                                                      bool stichwortsuche,
                                                                      CancellationToken abbruch)
        {
            List<WissensAbschnitt> ergebnis = new List<WissensAbschnitt>();
            if (string.IsNullOrWhiteSpace(basis) || string.IsNullOrWhiteSpace(frage)) return ergebnis;

            // Modell und Index anstossen - kehrt sofort zurueck und haelt diese
            // Suche NICHT auf. Beim ersten Mal ist deshalb nichts fertig; die
            // Semantik wirkt ab dem naechsten Aufruf.
            try { SemantikIndex.Anstossen(basis); }
            catch (Exception ex) { Debug.WriteLine("[Wiki] Semantik-Anstoss: " + ex.Message); }

            try
            {
                // ---- 1) Treffer sammeln: Kontextseite immer zuerst, dann die Suche.
                List<Suchtreffer> treffer = new List<Suchtreffer>();

                string kontextTitel = RubrikTitel(KontextSeite(kontext));
                if (kontextTitel.Length > 0)
                    treffer.Add(new Suchtreffer { Titel = kontextTitel });

                if (stichwortsuche)
                {
                    foreach (Suchtreffer t in await SuchtrefferAsync(basis, frage, abbruch)
                                                        .ConfigureAwait(false))
                    {
                        if (treffer.Any(v => string.Equals(v.Titel, t.Titel,
                                                           StringComparison.OrdinalIgnoreCase)))
                            continue;
                        treffer.Add(t);
                    }
                }
                else
                {
                    _letzteSuchAdressen = new List<string>();
                    LetzteSuchStufe = 0;
                }

                // Rubrik-Treffer nach vorn (OrderBy ist stabil, die Kontextseite
                // bleibt damit an erster Stelle).
                List<Suchtreffer> gereiht = treffer
                    .OrderBy(t => t.Titel.StartsWith(RUBRIK, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .Take(MAX_SEITEN)
                    .ToList();

                // ---- 1b) Semantische Stufe: fuellt auf, ueberholt nie.
                SemantikAnfuegen(frage, gereiht);

                if (gereiht.Count == 0) return ergebnis;

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

        // ==================================================================
        //  Semantische Stufe (H10) - sie ergaenzt, sie ersetzt nicht
        // ==================================================================

        /// <summary>
        /// Fuellt die Trefferliste mit semantischen Kandidaten auf, bis
        /// <see cref="MAX_SEITEN"/> erreicht ist.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Drei Zusagen.</b> (1) Was die Stichwortsuche gefunden hat, bleibt
        /// stehen und bleibt vorn - angehaengt wird nur, was noch Platz hat.
        /// (2) Die Kontextseite bleibt der erste Abschnitt; sie steht schon in der
        /// Liste, bevor diese Methode laeuft. (3) Steht kein Index, geschieht
        /// nichts - kein Warten, kein Fehler, kein Unterschied zu H9.
        /// </para>
        /// <para>
        /// Vereinigt wird auf SEITENebene: der beste Abschnitt einer Seite bringt
        /// die Seite herein, in den Prompt geht danach - wie bei jedem anderen
        /// Treffer auch - der ganze Seitenauszug. Die Ueberschrift des besten
        /// Abschnitts wird als Anker mitgegeben, damit der Quellen-Link im Chat
        /// an die richtige Stelle springt.
        /// </para>
        /// </remarks>
        private static void SemantikAnfuegen(string frage, List<Suchtreffer> gereiht)
        {
            LetzteSemantikTreffer = 0;

            try
            {
                int frei = MAX_SEITEN - gereiht.Count;
                if (frei <= 0) return;

                foreach (SemantikIndex.Fund f in SemantikIndex.Suche(frage, MAX_SEITEN))
                {
                    if (gereiht.Any(v => string.Equals(v.Titel, f.Titel,
                                                       StringComparison.OrdinalIgnoreCase)))
                        continue;

                    gereiht.Add(new Suchtreffer
                    {
                        Titel = f.Titel,
                        Anker = f.Ueberschrift.Replace(' ', '_'),
                        Beschreibung = ""
                    });

                    LetzteSemantikTreffer++;
                    if (gereiht.Count >= MAX_SEITEN) break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Wiki] Semantische Stufe uebersprungen: " + ex.Message);
            }
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

        /// <summary>
        /// Die Trefferliste zu einer Frage, ueber die Rueckfall-Kaskade (H9).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Die Suche des Wikis verknuepft ihre Terme mit UND. Bleibt die erste
        /// Stufe unter ZWEI Treffern, war die Kette zu lang - dann wird mit den
        /// zwei laengsten Stichwoertern nachgesucht, und bringt auch das nichts,
        /// mit dem laengsten allein. Es gilt IMMER das Ergebnis EINER Stufe;
        /// Treffer verschiedener Stufen werden nie zusammengeschuettet, weil
        /// deren Rangfolge sonst nichts mehr bedeutet.
        /// </para>
        /// <para>
        /// Eine spaetere Stufe darf eine fruehere nur ueberholen, wenn sie MEHR
        /// findet - sonst waere die Kaskade ein Rueckschritt, sobald der letzte
        /// Versuch leer ausgeht.
        /// </para>
        /// </remarks>
        private static async Task<List<Suchtreffer>> SuchtrefferAsync(string basis, string frage,
                                                                      CancellationToken abbruch)
        {
            _letzteSuchAdressen = new List<string>();
            LetzteSuchStufe = 0;

            List<string> stufen = Suchstufen(frage);
            if (stufen.Count == 0) return new List<Suchtreffer>();   // nichts Brauchbares zu senden

            List<Suchtreffer> beste = new List<Suchtreffer>();
            int besteStufe = 0;

            for (int i = 0; i < stufen.Count; i++)
            {
                List<Suchtreffer> gefunden =
                    await EineSucheAsync(basis, stufen[i], abbruch).ConfigureAwait(false);

                if (gefunden.Count > beste.Count) { beste = gefunden; besteStufe = i + 1; }

                // Stufe 1 muss zwei Seiten finden, jede weitere reicht mit einer.
                if (gefunden.Count >= (i == 0 ? 2 : 1))
                {
                    LetzteSuchStufe = i + 1;
                    return gefunden;
                }

                Debug.WriteLine("[Wiki] Stufe " + (i + 1) + " ('" + stufen[i] + "') = " +
                                gefunden.Count + " Treffer - Rueckfall.");
            }

            LetzteSuchStufe = besteStufe;
            return beste;
        }

        /// <summary>Ein einzelner Suchaufruf zu einer fertigen Stichwortliste.</summary>
        private static async Task<List<Suchtreffer>> EineSucheAsync(string basis, string stichwoerter,
                                                                    CancellationToken abbruch)
        {
            List<Suchtreffer> liste = new List<Suchtreffer>();
            if (string.IsNullOrWhiteSpace(stichwoerter)) return liste;

            string adresse = SuchAdresseFuer(basis, stichwoerter);
            LetzteSuchAdresse = adresse;
            _letzteSuchAdressen.Add(adresse);

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

        /// <summary>
        /// Der Klartext EINER Seite auf demselben Weg wie in der Suche: frischer
        /// Cache, sonst online (und dann in den Cache), sonst abgelaufener Cache,
        /// sonst leer. Der Weg des Indexaufbaus (H10) - er bekommt damit genau
        /// dieselben Auszuege wie der Prompt und kostet nichts extra, sobald der
        /// Tagescache steht.
        /// </summary>
        internal static async Task<string> SeitentextAsync(string basis, string titel,
                                                            CancellationToken abbruch)
        {
            if (string.IsNullOrWhiteSpace(basis) || string.IsNullOrWhiteSpace(titel)) return "";

            string frisch = CacheLesen(basis, titel, Gueltigkeit);
            if (frisch != null) return frisch;

            string geholt = await AuszugEinzelnAsync(basis, titel, abbruch).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(geholt))
            {
                CacheSchreiben(basis, titel, geholt);
                return geholt;
            }

            return CacheLesen(basis, titel, TimeSpan.MaxValue) ?? "";
        }

        /// <summary>
        /// Alle ECHTEN Seiten des Wikis (Namensraum 0, ohne Weiterleitungen) -
        /// die Grundmenge des Einbettungs-Index. Bei jedem Fehler eine leere
        /// Liste; dann wird eben kein Index gebaut.
        /// </summary>
        /// <remarks>
        /// Weiterleitungen bleiben ausdruecklich draussen
        /// (<c>apfilterredir=nonredirects</c>): die 37 Synonymseiten aus H9
        /// tragen keinen eigenen Text und wuerden dieselbe Seite ein zweites Mal
        /// in den Index bringen. <c>apcontinue</c> wird abgearbeitet, damit es
        /// auch ueber 500 Seiten hinaus vollstaendig bleibt.
        /// </remarks>
        internal static async Task<List<string>> SeitenlisteAsync(string basis,
                                                                   CancellationToken abbruch)
        {
            List<string> titel = new List<string>();
            if (string.IsNullOrWhiteSpace(basis)) return titel;

            string weiter = null;

            for (int runde = 0; runde < 10; runde++)
            {
                string adresse = basis + "/api.php?action=query&list=allpages&apnamespace=0" +
                                 "&apfilterredir=nonredirects&aplimit=500&format=json";
                if (weiter != null) adresse += "&apcontinue=" + Uri.EscapeDataString(weiter);

                string rumpf = await HolenAsync(adresse, abbruch).ConfigureAwait(false);
                if (rumpf == null) break;

                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(rumpf))
                    {
                        JsonElement abfrage, seiten;
                        if (!doc.RootElement.TryGetProperty("query", out abfrage)) break;
                        if (!abfrage.TryGetProperty("allpages", out seiten) ||
                            seiten.ValueKind != JsonValueKind.Array) break;

                        foreach (JsonElement s in seiten.EnumerateArray())
                        {
                            string t = Zeichenkette(s, "title");
                            if (t.Length > 0) titel.Add(t);
                        }

                        JsonElement fortsetzung, wert;
                        weiter = null;
                        if (doc.RootElement.TryGetProperty("continue", out fortsetzung) &&
                            fortsetzung.TryGetProperty("apcontinue", out wert) &&
                            wert.ValueKind == JsonValueKind.String)
                            weiter = wert.GetString();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[Wiki] Seitenliste nicht lesbar: " + ex.Message);
                    break;
                }

                if (weiter == null) break;
            }

            return titel;
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
            return Dienste.Pfade.Verbinde(Dienste.Pfade.Anwendungsdaten, "wiki-wissen");
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
