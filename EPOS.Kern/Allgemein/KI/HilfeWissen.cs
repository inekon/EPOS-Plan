using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Wissensabschnitt der Hilfe (Titel, Bereich, Inhalt).</summary>
    public class WissensAbschnitt
    {
        public string Titel = "";
        public string Bereich = "";     // grobe Zuordnung, z. B. "Simulation Konfiguration"
        public string Inhalt = "";

        /// <summary>
        /// Quelle im Netz, falls der Abschnitt aus der Online-Dokumentation stammt
        /// (H4, <see cref="WikiWissen"/>) - leer beim eingebauten Wissen. Nur die
        /// ANZEIGE nutzt ihn; in den Prompt geht die Adresse nicht.
        /// </summary>
        public string QuellUrl = "";

        /// <summary>
        /// Die sprachneutrale MELDUNGSKENNUNG, wenn dieser Abschnitt eine benannte
        /// Meldung erklärt (<see cref="KiMeldungskennung"/>, Auftrag #199) — leer bei
        /// jedem anderen Abschnitt.
        /// </summary>
        /// <remarks>
        /// Sie ist der Grund, warum „erklären lassen" auch OHNE Modell etwas liefert:
        /// <see cref="HilfeWissen.Suchen"/> bewertet einen Kennungstreffer höher als
        /// jedes Stichwort, und <see cref="HilfeWissen.AbschnittFuerKennung"/> findet
        /// ihn unmittelbar.
        /// </remarks>
        public string Kennung = "";

        public WissensAbschnitt() { }

        public WissensAbschnitt(string titel, string bereich, string inhalt, string quellUrl = "")
        {
            Titel = titel;
            Bereich = bereich;
            Inhalt = inhalt;
            QuellUrl = quellUrl ?? "";
        }

        /// <summary>Ein Abschnitt des Aktionswissens — mit Kennung (Auftrag #199).</summary>
        public WissensAbschnitt(string kennung, string titel, string bereich, string inhalt,
                                string quellUrl)
            : this(titel, bereich, inhalt, quellUrl)
        {
            Kennung = kennung ?? "";
        }
    }

    /// <summary>
    /// Lokale Wissensbasis des KI-Assistenten (Grundlage für RAG).
    ///
    /// Die Abschnitte werden aus drei Quellen gespeist:
    ///  1. fest eingebaute Basistexte zur Bedien- und Rechenlogik (immer verfügbar,
    ///     auch ohne Internet und ohne WordPress-Server)
    ///  2. seit H13 (06.09.2026) die Seiten der Wiki-Rubrik "Programm Dokumentation/
    ///     Berechnung" - je Seite ein Abschnitt, gelesen aus den eingebetteten
    ///     .wiki-Dateien des Kerns (BerechnungsHilfe). Damit beantwortet der
    ///     Assistent "Wie wird die Photovoltaik berechnet?" auch ohne Netz.
    ///  3. optional der lokale Hilfe-Cache "help_cache.json" des WordPress-Katalogs
    ///
    /// Die Suche ist bewusst einfach gehalten (Stichwort-Treffer mit Gewichtung).
    /// Sie läuft vollständig lokal und kostenlos - nur die wenigen besten
    /// Abschnitte werden anschließend an das Sprachmodell übergeben. Genau das
    /// hält die Token-Menge und damit die Kosten je Frage sehr klein.
    /// </summary>
    public static class HilfeWissen
    {
        private static List<WissensAbschnitt> _abschnitte = null;

        /// <summary>Alle bekannten Wissensabschnitte (wird verzögert aufgebaut).</summary>
        public static List<WissensAbschnitt> Abschnitte
        {
            get
            {
                if (_abschnitte == null) Aufbauen();
                return _abschnitte;
            }
        }

        private static void Aufbauen()
        {
            _abschnitte = new List<WissensAbschnitt>();
            _abschnitte.AddRange(Basiswissen());
            _abschnitte.AddRange(Berechnungswissen());
            _abschnitte.AddRange(Aktionswissen());

            // Zusätzlich den lokalen WordPress-Hilfecache einlesen, falls vorhanden
            try
            {
                // Produktdaten (%APPDATA%\<Produktname>) - derselbe Ordner, den
                // WikiHelpCatalog.SicherungsPfad benutzt, NICHT %APPDATA%\wp-plan.
                string pfad = Dienste.Pfade.Verbinde(Dienste.Pfade.Produktdaten, "help_cache.json");

                if (File.Exists(pfad))
                {
                    string json = File.ReadAllText(pfad);
                    using (System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        foreach (var eintrag in doc.RootElement.EnumerateObject())
                        {
                            // Seit F7 (Konzept Hilfesystem) ist der Schluessel der
                            // Link-Pfad und nicht mehr der Slug - anders gingen die
                            // acht Seiten mit doppelt vergebenem Slug verloren. Fuer
                            // die Stichwortsuche zaehlt weiterhin nur der letzte
                            // Abschnitt; sonst schluegen "epos-plan", "grundlagen"
                            // und "english" bei praktisch jeder Frage an.
                            string titel = LetzterPfadabschnitt(eintrag.Name);
                            if (eintrag.Value.TryGetProperty("Tooltip", out var tt))
                            {
                                string text = tt.GetString() ?? "";
                                if (!string.IsNullOrWhiteSpace(text))
                                    _abschnitte.Add(new WissensAbschnitt(titel, "Online-Hilfe", text));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hilfe-Cache konnte nicht gelesen werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Letzter Abschnitt eines Link-Pfades ("/a/b/c/" -> "c"). Ein Schluessel
        /// ohne Schraegstrich bleibt unveraendert - so liest sich auch eine
        /// aeltere, slug-geschluesselte Sicherung noch richtig.
        /// </summary>
        private static string LetzterPfadabschnitt(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return "";

            string kern = schluessel.Trim('/');
            if (kern.Length == 0) return schluessel;

            int letzter = kern.LastIndexOf('/');
            return letzter < 0 ? kern : kern.Substring(letzter + 1);
        }

        /// <summary>
        /// Der Zuschlag für einen Abschnitt, dessen <see cref="WissensAbschnitt.Kennung"/>
        /// der gesuchten entspricht (Auftrag #199).
        /// </summary>
        /// <remarks>
        /// Er liegt bewusst ÜBER allem, was Stichworte erreichen können: Wer „erklären
        /// lassen" an einem Banner drückt, hat keine Frage gestellt, sondern auf EINE
        /// Meldung gezeigt. Der Abschnitt zu dieser Meldung ist dann kein Treffer unter
        /// mehreren, sondern die Antwort.
        /// </remarks>
        public const double KENNUNG_BONUS = 100;

        /// <summary>
        /// Sucht die passendsten Abschnitte zu einer Frage.
        /// Bewertet werden Wortübereinstimmungen in Titel (dreifach), Bereich
        /// (doppelt) und Inhalt; der aktuelle Bedienkontext gibt einen Bonus.
        /// </summary>
        /// <param name="frage">Die Frage im Klartext.</param>
        /// <param name="kontext">Der Bedienkontext (<c>KiChatKontext</c>).</param>
        /// <param name="anzahl">Höchstzahl der Treffer.</param>
        /// <param name="kennung">
        /// Die Meldungskennung des Aufrufs (<see cref="KiMeldungskennung"/>, Auftrag
        /// #199). Der Abschnitt zu dieser Kennung steht dann VORN, auch ohne Modell
        /// und ohne passendes Stichwort.
        /// <para><c>null</c> (Vorgabe) heisst „die Kennung des gemeldeten
        /// Dialogaufrufs" — damit trägt „erklären lassen" durch die ganze Kette, ohne
        /// dass <c>KiChatService</c> oder die Hüllen eine zusätzliche Angabe
        /// durchreichen müssten. Eine LEERE Zeichenkette heisst ausdrücklich „keine
        /// Kennung" und schaltet den Zuschlag ab.</para>
        /// </param>
        public static List<WissensAbschnitt> Suchen(string frage, string kontext, int anzahl = 4,
                                                    string kennung = null)
        {
            if (kennung == null)
            {
                KiAufrufkontext aufruf = KiChatKontext.Aufruf;
                kennung = aufruf == null ? "" : aufruf.Kennung;
            }

            bool mitKennung = !string.IsNullOrWhiteSpace(kennung);
            if (string.IsNullOrWhiteSpace(frage) && !mitKennung) return new List<WissensAbschnitt>();

            string[] worte = Zerlegen(frage ?? "");
            string kontextKlein = (kontext ?? "").ToLowerInvariant();
            string gesucht = mitKennung ? kennung.Trim() : "";

            var bewertet = new List<KeyValuePair<double, WissensAbschnitt>>();

            foreach (WissensAbschnitt a in Abschnitte)
            {
                string titel = a.Titel.ToLowerInvariant();
                string bereich = a.Bereich.ToLowerInvariant();
                string inhalt = a.Inhalt.ToLowerInvariant();

                double punkte = 0;
                foreach (string w in worte)
                {
                    if (w.Length < 4) continue;                       // Füllwörter ignorieren
                    if (titel.Contains(w)) punkte += 3;
                    if (bereich.Contains(w)) punkte += 2;
                    if (inhalt.Contains(w)) punkte += 1;
                }

                // Bonus, wenn der Abschnitt zum aktuellen Bereich passt
                if (!string.IsNullOrEmpty(bereich) && kontextKlein.Contains(bereich)) punkte += 2.5;

                // Der Abschnitt ZU DIESER MELDUNG schlaegt jedes Stichwort (#199).
                if (mitKennung && string.Equals(a.Kennung, gesucht, StringComparison.OrdinalIgnoreCase))
                    punkte += KENNUNG_BONUS;

                if (punkte > 0) bewertet.Add(new KeyValuePair<double, WissensAbschnitt>(punkte, a));
            }

            return bewertet.OrderByDescending(p => p.Key)
                           .Take(anzahl)
                           .Select(p => p.Value)
                           .ToList();
        }

        /// <summary>
        /// Der Abschnitt zu einer Meldungskennung; <c>null</c>, wenn es keinen gibt
        /// (Auftrag #199).
        /// </summary>
        public static WissensAbschnitt AbschnittFuerKennung(string kennung)
        {
            if (string.IsNullOrWhiteSpace(kennung)) return null;
            string gesucht = kennung.Trim();

            foreach (WissensAbschnitt a in Abschnitte)
                if (string.Equals(a.Kennung, gesucht, StringComparison.OrdinalIgnoreCase)) return a;

            return null;
        }

        private static string[] Zerlegen(string text)
        {
            char[] trenner = { ' ', '\t', '\r', '\n', ',', ';', '.', '?', '!', ':', '(', ')', '"', '\'', '/', '-' };
            return text.ToLowerInvariant()
                       .Split(trenner, StringSplitOptions.RemoveEmptyEntries)
                       .Distinct()
                       .ToArray();
        }

        /// <summary>
        /// Fest eingebautes Grundwissen zur Bedienung und zur Rechenlogik.
        /// Diese Texte sollten mit der Software gepflegt werden - sie sind die
        /// Grundlage für die Antwortqualität des Assistenten.
        /// </summary>
        private static List<WissensAbschnitt> Basiswissen()
        {
            return new List<WissensAbschnitt>
            {
                new WissensAbschnitt("Dokumentation und Lizenz", "Hilfe",
                    "Die ausführliche Online-Dokumentation steht als Wiki unter " +
                    "https://wiki.epos-plan.de bereit und ist im Menü " +
                    "unter 'Hilfe > Dokumentation' verlinkt. Die Hilfeseiten zu den einzelnen " +
                    "Dialogen liegen dort in der Rubrik 'Programm Dokumentation'; die Info-Schaltflächen " +
                    "an den Eingabefeldern öffnen genau diese Seiten. Die Lizenzvereinbarung und die " +
                    "Allgemeinen Geschäftsbedingungen zeigt der Menüpunkt 'Hilfe > Lizenz'. " +
                    "Lizenzgeber ist Dr. Dirk Engelmann, INEKON, Breitwiesenstr. 13, 70565 Stuttgart. " +
                    "Die Software wird als Einzelplatzlizenz überlassen; die Ergebnisse sind vom " +
                    "Anwender fachlich auf Plausibilität zu prüfen (Ingenieurvorbehalt)."),

                new WissensAbschnitt("Ablauf einer Simulation", "Simulation",
                    "Die Simulation rechnet ein volles Jahr in 8760 Stundenschritten. Grundlage ist der berechnete " +
                    "Wärme- und Strombedarf. Die Wärmeerzeuger arbeiten als Kaskade in der eingestellten Reihenfolge: " +
                    "Der erste Erzeuger deckt so viel Bedarf wie möglich, der nächste übernimmt den verbleibenden Rest " +
                    "der jeweiligen Stunde. Was am Ende ungedeckt bleibt, erscheint als Restwärmebedarf. " +
                    "Die Reihenfolge wird im Dialog 'Simulation Konfiguration' über die vier Auswahlfelder unter " +
                    "'Wärmeerzeuger' festgelegt, Priorität absteigend."),

                new WissensAbschnitt("Simulation Konfiguration - Übersicht", "Simulation Konfiguration",
                    "Der Dialog 'Simulation Konfiguration' legt fest, welche Erzeuger in welcher Reihenfolge rechnen. " +
                    "Links werden Wärmeerzeuger, Pufferspeicher, Stromerzeuger und Energiespeicher ausgewählt. " +
                    "Rechts oben zeigt die 'Übersicht ausgewählte Erzeuger' alle im Projekt angelegten Anlagen. " +
                    "In dieser Übersicht lassen sich per Doppelklick bearbeiten: WP-Priorität, Wärmequelle, " +
                    "Wärmesenke, Betriebsmodus und über die Spalte Pufferspeicher die Speicherregelung. " +
                    "Darunter steht die Tabelle 'Pufferspeicher Zuordnung' mit Vorlauf- und Rücklauftemperatur."),

                new WissensAbschnitt("WP-Priorität", "Simulation Konfiguration",
                    "Die WP-Priorität legt die Einsatzreihenfolge mehrerer Wärmepumpen fest. Die Wärmepumpe mit " +
                    "Priorität 1 wird zuerst eingesetzt, die nächste deckt den verbleibenden Bedarf der Stunde. " +
                    "Ändern per Doppelklick auf die Spalte 'WP-Prio' in der Übersicht."),

                new WissensAbschnitt("Wärmequelle der Wärmepumpe", "Simulation Konfiguration",
                    "Bei Luft-Wasser-Wärmepumpen ist die Wärmequelle immer die Außenluft, also die Außentemperatur " +
                    "der gewählten Klimaregion. Bei Sole-Wasser- und Wasser-Wasser-Wärmepumpen stehen vier Varianten " +
                    "zur Wahl: konstante Temperatur, Pufferspeicher, Quellprofil aus Monats- und Wochenwerten sowie " +
                    "ein Temperaturprofil aus einer CSV-Datei mit 8760 Stundenwerten. Die Quelltemperatur geht in die " +
                    "Kennlinie ein und bestimmt damit COP und Leistung der Wärmepumpe."),

                new WissensAbschnitt("Quellprofil (Monats- und Wochenwerte)", "Simulation Konfiguration",
                    "Das Quellprofil wird wie die Brauchwasser-Stundenverteilung eingegeben: Auf dem Reiter " +
                    "'Monatswerte' stehen zwölf Monats-Mitteltemperaturen der Quelle in Grad Celsius, auf dem Reiter " +
                    "'Wochenwerte' der Tagesgang je Wochentag als Abweichung in Kelvin mit 24 Stundenwerten, " +
                    "kopierbar von Tag zu Tag. Das Jahresprofil ergibt sich als Monatswert plus Wochenwert. " +
                    "Der Reiter 'Grafik' zeigt das fertige Jahresprofil über 8760 Stunden."),

                new WissensAbschnitt("Wärmequelle Pufferspeicher", "Simulation Konfiguration",
                    "Wird ein Pufferspeicher als Wärmequelle gewählt, öffnet sich ein Auswahldialog mit den " +
                    "verfügbaren Speichern aus den Stammdaten. Anzugeben sind Quelltemperatur, nutzbare Spreizung " +
                    "und eine Regenerationsleistung in Kilowatt. In der Simulation entzieht die Wärmepumpe dem " +
                    "Speicher je Stunde die Verdampferwärme, also Wärmeproduktion minus Stromaufnahme. Reicht der " +
                    "Speicherinhalt nicht, wird die Leistung der Wärmepumpe begrenzt. Ohne Regeneration ist ein " +
                    "reiner Speicher als Quelle schnell erschöpft - das ist physikalisch korrekt."),

                new WissensAbschnitt("Wärmesenke", "Simulation Konfiguration",
                    "Die Wärmesenke legt fest, welchen Bedarf eine Wärmepumpe deckt: nur Warmwasser, nur Heizwärme " +
                    "oder beides. Ist nur Warmwasser angehakt, läuft die Wärmepumpe ausschließlich für den " +
                    "Warmwasserbedarf und bleibt aus, wenn keiner anliegt. Sind beide angehakt, gilt " +
                    "Warmwasservorrang: Zuerst wird der Warmwasserbedarf gedeckt, der Rest geht auf die Heizwärme."),

                new WissensAbschnitt("Betriebsmodus der Wärmepumpe", "Simulation Konfiguration",
                    "Drei Betriebsmodi steuern die Leistung: Laufzeitoptimiert bedeutet volle Leistung, die über den " +
                    "Bedarf hinaus erzeugte Wärme lädt den Pufferspeicher - das ergibt lange Laufzeiten und wenig " +
                    "Takten. Leistungsoptimiert bedeutet, die Wärmepumpe moduliert exakt auf den Wärmebedarf und " +
                    "erzeugt keinen Überschuss. PV-optimiert bedeutet erhöhte Leistung nur bei verfügbarem " +
                    "PV-Strom, begrenzt auf den PV-Überschuss; sonst arbeitet sie leistungsoptimiert. " +
                    "Für den PV-Modus muss im Bereich Stromerzeuger die Photovoltaik ausgewählt sein."),

                new WissensAbschnitt("Pufferspeicher und Speicherregelung", "Simulation Konfiguration",
                    "Die nutzbare Kapazität eines Pufferspeichers ergibt sich aus Volumen mal 1,16 Wh je Liter und " +
                    "Kelvin mal der Spreizung zwischen Vorlauf und Rücklauf. Ein 600-Liter-Speicher mit 65 auf 45 " +
                    "Grad hat also rund 13,9 Kilowattstunden. Die Speicherregelung arbeitet mit Hysterese: " +
                    "Unterschreitet der Füllstand die Einschaltschwelle, läuft die Wärmepumpe an und lädt bis zur " +
                    "Abschaltschwelle durch; dazwischen bleibt sie aus und der Bedarf wird aus dem Speicher gedeckt. " +
                    "Beide Schwellen sind in Prozent der Kapazität einstellbar, Vorgabe 10 und 95 Prozent. " +
                    "Die Abschaltschwelle liegt bewusst unter 100 Prozent, weil die Bereitschaftsverluste den " +
                    "Füllstand laufend absenken."),

                new WissensAbschnitt("Warum liegt die Wärmeproduktion über dem Bedarf?", "Ergebnis",
                    "Wenn ein Pufferspeicher zugeordnet ist, erzeugt die Wärmepumpe nicht nur den Bedarf der Stunde, " +
                    "sondern lädt zusätzlich den Speicher. Diese Ladung zählt zur Wärmeproduktion, deckt aber keinen " +
                    "Bedarf. In anderen Stunden deckt der Speicher den Bedarf und die Wärmepumpe steht still. " +
                    "Im Jahresmittel entspricht der Überschuss genau den Bereitschaftsverlusten des Speichers. " +
                    "Beispiel: 2,1 Kilowattstunden pro 24 Stunden ergeben rund 0,77 Megawattstunden im Jahr."),

                new WissensAbschnitt("Bivalenzpunkt und Heizstab", "Wärmepumpe",
                    "Der Bivalenzpunkt ist die Außentemperatur, unterhalb derer die Wärmepumpe den Wärmebedarf " +
                    "nicht mehr allein decken kann. Ist der Heizstab aktiviert, deckt er den verbleibenden Rest, " +
                    "sonst geht dieser an den nächsten Erzeuger der Kaskade oder bleibt als Restwärmebedarf stehen. " +
                    "Die Betriebsarten Alternativ-, Parallel- und Teilparallelbetrieb steuern, wie Wärmepumpe und " +
                    "zweiter Erzeuger unterhalb des Bivalenzpunkts zusammenarbeiten."),

                new WissensAbschnitt("Wärmelast Jahresganglinie", "Wärmepumpe",
                    "Das Diagramm zeigt den Bedarf getrennt nach Heizwärmebedarf und Warmwasserbedarf sowie " +
                    "Heizstab und Wärmeproduktion über das Jahr. Mit der Checkbox 'sortiert' wechselt die " +
                    "Darstellung zur geordneten Jahresdauerlinie. Einzelne Kurven lassen sich über die Legende " +
                    "ein- und ausblenden."),

                new WissensAbschnitt("CSV-Export", "Export",
                    "CSV-Exporte stehen an drei Stellen bereit: im Bereich Energiebedarf für Wärmelast und " +
                    "Strombedarf, im Bereich Wärmepumpe für Wärmebedarf, Heizstab, Wärmeproduktion und Strombedarf " +
                    "einschließlich der Pufferspeicher-Ganglinien, sowie in den Ergebnis-Charts für die gerade " +
                    "ausgewählten Kurven. Jede Datei enthält Zeitstempel, Außentemperatur und die Werte. " +
                    "Trennzeichen ist das Semikolon, Dezimaltrennzeichen das Komma; der zuletzt genutzte " +
                    "Ausgabeordner wird gemerkt."),

                new WissensAbschnitt("Wärme Produktion Chart", "Ergebnis",
                    "Das Ergebnis-Diagramm 'Wärme Produktion Chart' zeigt die Jahresganglinie der Wärmeproduktion. " +
                    "Über die Checkboxen lassen sich Gesamt, Wärmepumpe, Heizstab, Heizkessel, Solarthermie, BHKW " +
                    "und der Pufferspeicher-Füllstand einblenden. Der Füllstand wird in Kilowattstunden dargestellt " +
                    "und macht das Laden und Entladen des Speichers sichtbar."),

                new WissensAbschnitt("Klimaregion und Außentemperatur", "Projekt",
                    "Die Klimaregion liefert den stündlichen Außentemperaturgang für das gesamte Simulationsjahr. " +
                    "Sie wird oben im Hauptfenster ausgewählt und muss vor der Berechnung des Wärmebedarfs gesetzt " +
                    "sein. Ohne Klimaregion bricht die Simulation mit einem Hinweis ab."),

                new WissensAbschnitt("Energiebedarf berechnen", "Energiebedarf",
                    "Der Wärmebedarf setzt sich zusammen aus dem Gebäudebedarf, externen Lastgängen, Prozesswärme, " +
                    "Brauchwasser und den Netzverlusten. Der Strombedarf entsteht aus Stromprofilen und " +
                    "Stromganglinien und wird intern in Viertelstundenwerten geführt. Beide werden vor der " +
                    "Erzeuger-Simulation berechnet und im Bereich Energiebedarf als Jahresganglinie dargestellt."),

                // Anwenderbefund W15b-E-4 (Windows-Abnahme 05.09.2026): "Es ist unklar,
                // was ausgefuehrt werden kann und wie." Der Abschnitt steht im
                // EINGEBAUTEN Wissen und nicht nur im Wiki - er wird damit auch ohne
                // Netz gefunden, und zwar von derselben Suche, die "Nur suchen"
                // benutzt. Die Werkzeugliste verweist mit ihrem Infoknopf hierher.
                new WissensAbschnitt("Aktionen des Assistenten", "Hilfe-Assistent",
                    "Der Assistent kann nicht nur antworten, sondern auch Aktionen im Programm ausführen. " +
                    "Dafür muss im Chatfenster das Kästchen 'Aktionen zulassen' angehakt sein; ohne die einmalige " +
                    "Einwilligung wird nichts übertragen und nichts ausgeführt. Es gibt zwei Wege: Sie fragen im " +
                    "Klartext, oder Sie wählen die Aktion über 'Werkzeuge...' von Hand aus - dieser zweite Weg " +
                    "kommt ohne Sprachmodell und damit ohne Kosten aus. " +
                    "Jede Aktion ist entweder lesend oder verändernd. Lesende Aktionen laufen sofort; verändernde " +
                    "zeigen zuerst eine Vorschau und laufen erst nach Ihrer ausdrücklichen Bestätigung, die nach " +
                    "kurzer Zeit verfällt. Vor der ersten Änderung legt das Programm einen Sicherungspunkt der " +
                    "Datenbank an. Jeder Versuch bekommt eine Zeile im Aktionsprotokoll ('Protokoll anzeigen'). " +
                    "Ein Beispiel: 'Lege zum Projekt Musterhaus eine Variante Wärmepumpe statt Kessel an.' Der " +
                    "Assistent schlägt daraufhin die Aktion 'Variante anlegen' mit dem Stammprojekt Musterhaus " +
                    "und dem Bezeichner 'Wärmepumpe statt Kessel' vor und zeigt die Vorschau; erst 'Ausführen' " +
                    "legt die Variante an. Von Hand erreichen Sie dasselbe über 'Werkzeuge...': links 'Variante " +
                    "anlegen' wählen, rechts die zwei Pflichtfelder 'Stammprojekt' und 'Bezeichner' ausfüllen " +
                    "und 'Ausführen' drücken. " +
                    "Lesende Aktionen sind unter anderem: Projekte auflisten, Projekt suchen, Varianten " +
                    "auflisten, Wirtschaftlichkeitsergebnisse lesen, Lastgangdatei prüfen und die kleinste " +
                    "Netzbezugsspitze ermitteln. Verändernd sind: Variante anlegen, Speichervariante aktiv " +
                    "setzen, Kostenposition setzen sowie das Ausfüllen von Feldern und das Auslösen von Knöpfen " +
                    "einer geöffneten Maske."),
            };
        }

        /// <summary>
        /// H13 — die Rechenwege aus der Wiki-Rubrik „Programm Dokumentation/Berechnung".
        ///
        /// <para><b>Warum sie hier stehen und nicht nur im Wiki.</b> Die Rubrik ist die
        /// Antwort auf den Anwenderwunsch vom 06.09.2026: Die Details der Berechnung
        /// gehören nicht in die allgemeine Erklärung der Funktionen, sondern in eine
        /// eigene Rubrik. Der Assistent soll dieselben Sätze kennen — und zwar auch
        /// ohne Netz und bevor der Anwender die Seiten im Wiki angelegt hat. Die Texte
        /// liegen deshalb als eingebettete <c>.wiki</c>-Dateien im Kern
        /// (<see cref="BerechnungsHilfe"/>); hier kommt je Seite EIN Abschnitt an.</para>
        ///
        /// <para><b>Titel und Bereich.</b> Titel ist „Berechnung: &lt;Thema&gt;",
        /// Bereich schlicht „Berechnung". Beides zählt in <see cref="Suchen"/> dreifach
        /// bzw. doppelt — eine Frage nach „Berechnung Photovoltaik" trifft damit die
        /// Rechenwegseite und nicht die Bedienhilfe. An Suche und Gewichtung selbst
        /// ändert das Paket nichts.</para>
        ///
        /// <para>Inhalt ist der KLARTEXT der Seite, nicht das Markup: Der Kopfblock mit
        /// den Quelldateien und die Wiki-Auszeichnung gehören nicht in den Prompt, jede
        /// Zahl und jede Formelzeile dagegen schon.</para>
        /// </summary>
        private static List<WissensAbschnitt> Berechnungswissen()
        {
            var abschnitte = new List<WissensAbschnitt>();

            try
            {
                foreach (BerechnungsSeite seite in BerechnungsHilfe.Seiten)
                {
                    if (seite == null || string.IsNullOrWhiteSpace(seite.Klartext)) continue;

                    abschnitte.Add(new WissensAbschnitt(
                        seite.Titel, BerechnungsHilfe.BEREICH, seite.Klartext));
                }
            }
            catch (Exception ex)
            {
                // Ein fehlender Rechenwegtext ist ein Schoenheitsfehler, kein Grund,
                // das ganze Wissen scheitern zu lassen.
                Console.WriteLine("Berechnungswissen nicht lesbar: " + ex.Message);
            }

            return abschnitte;
        }

        // =================================================================
        //  AKTIONSWISSEN je MELDUNGSKENNUNG (Auftrag #199, Stufe S1, Weg 2)
        // =================================================================

        /// <summary>
        /// Die Wiki-Wurzel der Bedienhilfe — dieselbe Adresse, die der Info-Knopf
        /// öffnet (<c>help_mapping.txt</c>, Kopf).
        /// </summary>
        private const string WIKI = "https://wiki.epos-plan.de/wiki/Programm_Dokumentation/";

        /// <summary>
        /// Je erklärbarer Meldung EIN Abschnitt: Bedeutung, Ursache, Abhilfe und der
        /// Verweis auf die Wiki-Seite (Aufgabensteuerungskonzept 7.1 „Aktionswissen",
        /// Dialogkonzept 3.2).
        ///
        /// <para><b>Warum als Daten und nicht als Modellantwort.</b> „Erklären lassen"
        /// muss auch dann etwas liefern, wenn kein Schlüssel hinterlegt ist, kein Netz
        /// besteht oder das Tageslimit erschöpft ist — genau die Lagen, in denen der
        /// Anwender vor einer Warnung steht und nicht weiterkommt. Diese Abschnitte
        /// sind deshalb Treffer der STICHWORTSUCHE; das Modell bekommt sie, wenn es
        /// eines gibt, als Beleg mit.</para>
        ///
        /// <para><b>Deutsch, wie das übrige Einbauwissen.</b> Die Wissensbasis ist
        /// einsprachig (<see cref="Basiswissen"/>, <see cref="Berechnungswissen"/>);
        /// zweisprachig sind die OBERFLÄCHENTEXTE des Weges — der Link „erklären
        /// lassen" und die vorbelegten Fragen <c>KI_FRAGE_*</c>. Eine zweite
        /// Sprachfassung der Wissensbasis ist ein eigener Schritt und wäre hier die
        /// erste ihrer Art.</para>
        ///
        /// <para><b>Der Titel trägt die Kennung mit.</b> Er zählt in
        /// <see cref="Suchen"/> dreifach, und die Kennung ist ein Wort — damit findet
        /// auch ein Anwender den Abschnitt, der sie aus einem Protokoll abschreibt.</para>
        /// </summary>
        private static List<WissensAbschnitt> Aktionswissen()
        {
            return new List<WissensAbschnitt>
            {
                // ---- Speicherflotte: die fünf Prüfhinweise (P1, #183) --------------
                new WissensAbschnitt(KiMeldungskennung.FLOTTE_PEAKZIEL_UNTER_TAGESMINIMUM,
                    "Meldung FLOTTE_PEAKZIEL_UNTER_TAGESMINIMUM: Peak-Ziel unter dem Maximum der Tagesminima",
                    KiChatKontext.B_STROMSPEICHER,
                    "BEDEUTUNG: Das eingestellte Peak-Ziel liegt unter dem höchsten Tagesminimum der " +
                    "Lastzeitreihe. An mindestens einem Tag fällt die Last nie unter dieses Ziel. " +
                    "URSACHE: Geladen wird bei der Lastspitzenkappung nur, wenn die Last UNTER dem Peak-Ziel " +
                    "liegt - der Abstand zum Ziel ist der Ladedeckel. Ist der Deckel einen ganzen Tag lang 0 " +
                    "und ist die Netzladung verboten, bleibt der Speicher leer und kann die Spitze am Abend " +
                    "nicht kappen. ABHILFE: entweder das Peak-Ziel anheben - die Vorprüfung nennt den " +
                    "Vorschlag aus der Referenzzeitreihe, und der Knopf 'Peak-Ziel bestimmen...' rechnet ihn " +
                    "aus -, oder in Schritt 3 'Betriebsführung' die Netzladung erlauben; dann lädt die Flotte " +
                    "auch oberhalb des Ziels aus dem Netz. WIKI: Programm Dokumentation/Stromspeicher.",
                    WIKI + "Stromspeicher"),

                new WissensAbschnitt(KiMeldungskennung.FLOTTE_PEAKZIEL_UEBER_REFERENZSPITZE,
                    "Meldung FLOTTE_PEAKZIEL_UEBER_REFERENZSPITZE: Peak-Ziel über der Referenzspitze",
                    KiChatKontext.B_STROMSPEICHER,
                    "BEDEUTUNG: Das Peak-Ziel liegt höher als die grösste Last der Referenzzeitreihe. " +
                    "URSACHE: Gekappt wird nur, was über dem Ziel liegt - liegt das Ziel über allem, gibt es " +
                    "nichts zu kappen. Der Lauf rechnet trotzdem, die Flotte entlädt aber nie für die " +
                    "Spitzenkappung, und die Einsparung beim Leistungspreis bleibt 0. ABHILFE: das Peak-Ziel " +
                    "unter die Referenzspitze setzen. Wieviel wirtschaftlich ist, beantwortet der Knopf " +
                    "'Peak-Ziel bestimmen...'; er sucht die Schwelle, ab der die Kappung mehr einspart als " +
                    "sie kostet. WIKI: Programm Dokumentation/Stromspeicher.",
                    WIKI + "Stromspeicher"),

                new WissensAbschnitt(KiMeldungskennung.FLOTTE_BETRIEBSKOSTEN_SEHR_NIEDRIG,
                    "Meldung FLOTTE_BETRIEBSKOSTEN_SEHR_NIEDRIG: Betriebsaufwand auffällig klein",
                    KiChatKontext.B_STROMSPEICHER,
                    "BEDEUTUNG: Der jährliche Betriebsaufwand der Flotte liegt unter einem Tausendstel der " +
                    "Investition. URSACHE: In aller Regel ein vergessenes Feld - der Betriebsaufwand steht " +
                    "noch auf der Vorbelegung, während die Investition schon gepflegt ist. Ein Speicher für " +
                    "15 000 Euro mit 1 Euro Betrieb im Jahr ist keine Eingabe, sondern eine Lücke. " +
                    "ABHILFE: In Schritt 2 'Kosten' den Betriebsaufwand je Einheit prüfen (Wartung, " +
                    "Versicherung, Prüfung). Die Meldung SPERRT nicht: Wer den Wert bewusst so führt, rechnet " +
                    "unverändert weiter - die Wirtschaftlichkeit fällt dann günstiger aus, als sie ist. " +
                    "WIKI: Programm Dokumentation/Stromspeicher.",
                    WIKI + "Stromspeicher"),

                new WissensAbschnitt(KiMeldungskennung.FLOTTE_START_SOC_AUF_MINIMUM,
                    "Meldung FLOTTE_START_SOC_AUF_MINIMUM: Start-Ladezustand auf dem SoC-Minimum",
                    KiChatKontext.B_STROMSPEICHER,
                    "BEDEUTUNG: Der Ladezustand zu Beginn des Rechenzeitraums entspricht der unteren " +
                    "SoC-Grenze. URSACHE: Ein Speicher, der leer anfängt, hat am ersten Tag nichts zu " +
                    "entladen - er muss erst laden. Bei einem kurzen Zeitraum verzerrt das die Bilanz " +
                    "deutlich; über ein volles Jahr fällt es kaum ins Gewicht. ABHILFE: den Start-SoC in " +
                    "Schritt 1 auf einen mittleren Wert setzen (etwa die Mitte des SoC-Bandes), wenn der " +
                    "Zeitraum kurz ist. Für den Jahreslauf ist die Meldung ein Hinweis und kein Fehler. " +
                    "WIKI: Programm Dokumentation/Stromspeicher.",
                    WIKI + "Stromspeicher"),

                new WissensAbschnitt(KiMeldungskennung.FLOTTE_ARBEITSLOS,
                    "Meldung FLOTTE_ARBEITSLOS: Die Flotte hat weder geladen noch entladen",
                    KiChatKontext.B_STROMSPEICHER,
                    "BEDEUTUNG: Über den ganzen Zeitraum ist weder Lade- noch Entladeenergie angefallen. " +
                    "'Mit Flotte' ist dann zahlengleich zu 'Ohne Speicher', der Netto-Cashflow ist genau der " +
                    "negative Betriebsaufwand, und die Kacheln zeigen Nullen. URSACHE: Die Diagnose zählt " +
                    "vier Gründe je Intervall mit und nennt sie im Banner - Last über dem Peak-Ziel, " +
                    "Ladedeckel 0 durch die Peak-Regel, Ladedeckel 0 durch das Netzladeverbot, " +
                    "Entladeanforderung bei leerem Speicher. ABHILFE: Die drei Knöpfe am Banner führen " +
                    "dorthin: 'Peak-Ziel bestimmen...' rechnet ein tragfähiges Ziel aus, 'Netzladung " +
                    "erlauben' hebt das Ladeverbot auf, 'Zu Schritt 3' öffnet die Betriebsführung, in der " +
                    "Betriebsziel, Peak-Ziel und Netzladung zusammenstehen. " +
                    "WIKI: Programm Dokumentation/Stromspeicher.",
                    WIKI + "Stromspeicher"),

                // ---- Simulationslauf: Erzeuger ohne Platz (#190) -------------------
                new WissensAbschnitt(KiMeldungskennung.LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ,
                    "Meldung LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ: Wärmeerzeuger rechnet nicht mit",
                    KiChatKontext.B_SIM_KONFIG,
                    "BEDEUTUNG: Das Projekt führt einen Wärmeerzeuger - Heizkessel, BHKW, Wärmepumpe oder " +
                    "Solarthermie -, der auf keinem der vier Kaskadenplätze steht. Er erscheint in der " +
                    "Ergebnisübersicht mit 0,00 und dem Zusatz '(nicht in der Kaskade)'. URSACHE: Eine " +
                    "Anlage ANZULEGEN belegt keinen Platz. Die Reihenfolge der Erzeuger ist eine eigene " +
                    "Angabe der Simulationskonfiguration; wer eine Anlage im Projektassistenten oder im " +
                    "Erzeugerdialog anlegt, hat sie damit noch nicht in den Ablauf aufgenommen. " +
                    "ABHILFE: 'Simulation Konfiguration' öffnen; die Anlage steht dort als verfügbare Karte " +
                    "und wird mit '+ aufnehmen' auf einen Platz gesetzt. Danach die Simulation erneut " +
                    "rechnen. Der Assistent nimmt die Anlage NICHT von selbst auf - das wäre eine " +
                    "Ergebnisänderung an jedem Bestandsprojekt mit einer solchen Lücke. " +
                    "WIKI: Programm Dokumentation/Simulation.",
                    WIKI + "Simulation"),

                new WissensAbschnitt(KiMeldungskennung.LAUF_W_ERZEUGER_OHNE_STROMPLATZ,
                    "Meldung LAUF_W_ERZEUGER_OHNE_STROMPLATZ: Stromerzeuger oder Speicher rechnet nicht mit",
                    KiChatKontext.B_SIM_KONFIG,
                    "BEDEUTUNG: Dasselbe wie LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ, nur für die zwei " +
                    "Stromplätze: Photovoltaik steht auf dem Platz 'Stromerzeuger', der Stromspeicher auf " +
                    "dem Platz 'Energiespeicher'. Eine angelegte PV-Anlage ohne Platz liefert 0 kWh, ein " +
                    "angelegter Speicher lädt nie. URSACHE: Auch hier belegt das Anlegen keinen Platz. " +
                    "ABHILFE: In 'Simulation Konfiguration' unter Stromerzeuger bzw. Energiespeicher die " +
                    "Anlage aufnehmen und erneut rechnen. Achtung beim PV-optimierten Betrieb der " +
                    "Wärmepumpe: Er setzt eine PLATZIERTE Photovoltaik voraus - ohne sie arbeitet die " +
                    "Wärmepumpe leistungsoptimiert weiter. WIKI: Programm Dokumentation/Simulation.",
                    WIKI + "Simulation"),

                // ---- Strangampel P1 bis P8 (Konzept Wechselrichter 4.2) ------------
                new WissensAbschnitt(KiMeldungskennung.PV_STRANG_P1,
                    "Meldung PV_STRANG_P1: Leerlaufspannung im kalten Fall zu hoch (rot)",
                    KiChatKontext.B_PHOTOVOLTAIK,
                    "BEDEUTUNG: Die Leerlaufspannung des Strangs bei der kalten Auslegungstemperatur liegt " +
                    "über der maximalen DC-Eingangsspannung des Wechselrichters. Das ist die einzige " +
                    "Regel, deren Verletzung das Gerät ZERSTÖREN kann - deshalb rot. URSACHE: zu viele " +
                    "Module in Reihe. Die Leerlaufspannung steigt mit fallender Temperatur; gerechnet wird " +
                    "mit dem Temperaturkoeffizienten beta_OC des Moduls, und fehlt er, mit einem " +
                    "Ersatzfaktor. ABHILFE: Module aus der Reihe nehmen (Spalte 'Module in Reihe') oder " +
                    "einen Wechselrichter mit höherer DC-Grenze wählen. Der Knopf 'Auslegung vorschlagen' " +
                    "nennt eine Reihenlänge, die P1 bis P3 einhält. " +
                    "WIKI: Programm Dokumentation/Berechnung/Photovoltaik#wechselrichter.",
                    WIKI + "Berechnung/Photovoltaik#wechselrichter"),

                new WissensAbschnitt(KiMeldungskennung.PV_STRANG_P2,
                    "Meldung PV_STRANG_P2: MPP-Spannung im heissen Fall unter dem MPP-Fenster (rot)",
                    KiChatKontext.B_PHOTOVOLTAIK,
                    "BEDEUTUNG: Die MPP-Spannung des Strangs bei 70 Grad Celsius Modultemperatur liegt " +
                    "unter der unteren MPP-Grenze des Wechselrichters. Im Sommer verlässt der Strang damit " +
                    "das Regelfenster: Das Gerät kann den Arbeitspunkt nicht mehr halten und schaltet ab " +
                    "oder regelt auf einen schlechteren Punkt. URSACHE: zu wenige Module in Reihe. Die " +
                    "MPP-Spannung sinkt mit steigender Temperatur. ABHILFE: Module in die Reihe aufnehmen " +
                    "oder einen Wechselrichter mit tieferer MPP-Untergrenze wählen. " +
                    "WIKI: Programm Dokumentation/Berechnung/Photovoltaik#wechselrichter.",
                    WIKI + "Berechnung/Photovoltaik#wechselrichter"),

                new WissensAbschnitt(KiMeldungskennung.PV_STRANG_P3,
                    "Meldung PV_STRANG_P3: MPP-Spannung im kalten Fall über dem MPP-Fenster (gelb)",
                    KiChatKontext.B_PHOTOVOLTAIK,
                    "BEDEUTUNG: Die MPP-Spannung des Strangs bei minus 10 Grad Celsius liegt über der " +
                    "oberen MPP-Grenze des Wechselrichters. URSACHE: zu viele Module in Reihe - dieselbe " +
                    "Richtung wie P1, nur eine Stufe früher. Gelb und nicht rot, weil das Gerät dabei nicht " +
                    "beschädigt wird: Es fährt in kalten Stunden aus dem Regelfenster und erntet weniger. " +
                    "ABHILFE: Reihenlänge verringern; der Vorschlag der Auslegungshilfe hält P1 bis P3 " +
                    "gemeinsam ein. Hinweis: Fehlt dem Modul ein eigener Temperaturkoeffizient für die " +
                    "MPP-Spannung, rechnen P2 und P3 mit beta_OC als Näherung - der Werkzeugtipp der Ampel " +
                    "sagt es. WIKI: Programm Dokumentation/Berechnung/Photovoltaik#wechselrichter.",
                    WIKI + "Berechnung/Photovoltaik#wechselrichter"),

                new WissensAbschnitt(KiMeldungskennung.PV_STRANG_P4,
                    "Meldung PV_STRANG_P4: Eingangsstrom je MPPT zu hoch (rot)",
                    KiChatKontext.B_PHOTOVOLTAIK,
                    "BEDEUTUNG: Der Strom aller Stränge an einem MPP-Tracker überschreitet dessen " +
                    "zulässigen Eingangsstrom. URSACHE: zu viele parallele Stränge an einem Tracker oder " +
                    "ein Modul mit hohem Kurzschlussstrom. Gerechnet wird die thermische Korrektur im " +
                    "heissen Fall, nicht der Auslegungsfaktor 1,25 der Leitungsdimensionierung - das ist " +
                    "eine Frage des Kabelquerschnitts und nicht des Wechselrichters. ABHILFE: Stränge auf " +
                    "mehrere Tracker verteilen, die Parallelzahl verringern oder ein Gerät mit höherem " +
                    "Eingangsstrom wählen. WIKI: Programm Dokumentation/Berechnung/Photovoltaik#wechselrichter.",
                    WIKI + "Berechnung/Photovoltaik#wechselrichter"),

                new WissensAbschnitt(KiMeldungskennung.PV_STRANG_P5,
                    "Meldung PV_STRANG_P5: Mehr Stränge an einem MPPT, als das Gerät führt (gelb)",
                    KiChatKontext.B_PHOTOVOLTAIK,
                    "BEDEUTUNG: An einem MPP-Tracker hängen mehr Stränge, als der Hersteller für diesen " +
                    "Tracker angibt. URSACHE: In aller Regel eine Verteilung, die mehr Stränge auf einen " +
                    "Tracker legt, als er Eingänge hat. Gelb, weil sich das mit einem externen " +
                    "Generatoranschlusskasten bauen lässt - der Wechselrichter sieht dann einen " +
                    "zusammengeführten Strang. ABHILFE: Stränge gleichmässig auf die Tracker verteilen " +
                    "oder die Zusammenführung bewusst so planen; zusammen mit P4 prüfen, ob der Strom " +
                    "dann noch passt. WIKI: Programm Dokumentation/Berechnung/Photovoltaik#wechselrichter.",
                    WIKI + "Berechnung/Photovoltaik#wechselrichter"),

                new WissensAbschnitt(KiMeldungskennung.PV_STRANG_P6,
                    "Meldung PV_STRANG_P6: DC/AC-Verhältnis ausserhalb des Bandes (gelb)",
                    KiChatKontext.B_PHOTOVOLTAIK,
                    "BEDEUTUNG: Das Verhältnis aus DC-Generatorleistung (kWp) und AC-Nennleistung des " +
                    "Wechselrichters liegt ausserhalb des empfohlenen Bandes. URSACHE: Unterhalb des " +
                    "Bandes ist der Wechselrichter zu gross gewählt - er arbeitet meist im Teillastbereich " +
                    "mit schlechterem Wirkungsgrad und kostet zuviel. Oberhalb ist er zu klein: Er " +
                    "begrenzt an Sonnentagen auf seine Nennleistung, die Kappungsverluste steigen. " +
                    "ABHILFE: Generatorgrösse oder Gerät anpassen. Eine leichte Überbelegung ist " +
                    "verbreitet und oft wirtschaftlich - deshalb gelb und nicht rot. " +
                    "WIKI: Programm Dokumentation/Berechnung/Photovoltaik#wechselrichter.",
                    WIKI + "Berechnung/Photovoltaik#wechselrichter"),

                new WissensAbschnitt(KiMeldungskennung.PV_STRANG_P7,
                    "Meldung PV_STRANG_P7: DC-Eingangsleistung über der Herstellergrenze (gelb)",
                    KiChatKontext.B_PHOTOVOLTAIK,
                    "BEDEUTUNG: Die angeschlossene DC-Leistung übersteigt die vom Hersteller genannte " +
                    "höchste DC-Eingangsleistung des Wechselrichters. URSACHE: zu viele Module am Gerät. " +
                    "Anders als P6, das ein Verhältnis bewertet, ist das eine harte Angabe des " +
                    "Datenblatts. ABHILFE: Module abziehen, auf ein zweites Gerät verteilen oder einen " +
                    "grösseren Wechselrichter wählen. " +
                    "WIKI: Programm Dokumentation/Berechnung/Photovoltaik#wechselrichter.",
                    WIKI + "Berechnung/Photovoltaik#wechselrichter"),

                new WissensAbschnitt(KiMeldungskennung.PV_STRANG_P8,
                    "Meldung PV_STRANG_P8: Modulsumme weicht von der Anlagenangabe ab (gelb)",
                    KiChatKontext.B_PHOTOVOLTAIK,
                    "BEDEUTUNG: Die Summe aus 'Module in Reihe' mal 'Stränge parallel' über alle Stränge " +
                    "stimmt nicht mit der 'Anzahl Module' der PV-Anlage überein. URSACHE: Die Anlage " +
                    "wurde nach dem Anlegen der Stränge geändert - oder die Stränge bilden die Anlage " +
                    "nicht vollständig ab. Die Jahreserzeugung rechnet mit der ANLAGENANGABE; die Stränge " +
                    "beschreiben die elektrische Auslegung. Zwei Zahlen, die auseinanderlaufen, sind " +
                    "deshalb kein Rechenfehler, aber ein Planungsfehler. ABHILFE: Entweder die " +
                    "Modulanzahl der Anlage auf die Strangsumme setzen oder die fehlenden Stränge " +
                    "ergänzen. WIKI: Programm Dokumentation/Berechnung/Photovoltaik#wechselrichter.",
                    WIKI + "Berechnung/Photovoltaik#wechselrichter")
            };
        }
    }
}
