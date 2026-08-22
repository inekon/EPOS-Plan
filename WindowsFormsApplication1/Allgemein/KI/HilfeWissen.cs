using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Wissensabschnitt der Hilfe (Titel, Bereich, Inhalt).</summary>
    public class WissensAbschnitt
    {
        public string Titel = "";
        public string Bereich = "";     // grobe Zuordnung, z. B. "Simulation Konfiguration"
        public string Inhalt = "";

        public WissensAbschnitt() { }

        public WissensAbschnitt(string titel, string bereich, string inhalt)
        {
            Titel = titel;
            Bereich = bereich;
            Inhalt = inhalt;
        }
    }

    /// <summary>
    /// Lokale Wissensbasis des KI-Assistenten (Grundlage für RAG).
    ///
    /// Die Abschnitte werden aus zwei Quellen gespeist:
    ///  1. fest eingebaute Basistexte zur Bedien- und Rechenlogik (immer verfügbar,
    ///     auch ohne Internet und ohne WordPress-Server)
    ///  2. optional der lokale Hilfe-Cache "help_cache.json" des WordPress-Katalogs
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

            // Zusätzlich den lokalen WordPress-Hilfecache einlesen, falls vorhanden
            try
            {
                string pfad = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Application.ProductName ?? "WP-Plan", "help_cache.json");

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
        /// Sucht die passendsten Abschnitte zu einer Frage.
        /// Bewertet werden Wortübereinstimmungen in Titel (dreifach), Bereich
        /// (doppelt) und Inhalt; der aktuelle Bedienkontext gibt einen Bonus.
        /// </summary>
        public static List<WissensAbschnitt> Suchen(string frage, string kontext, int anzahl = 4)
        {
            if (string.IsNullOrWhiteSpace(frage)) return new List<WissensAbschnitt>();

            string[] worte = Zerlegen(frage);
            string kontextKlein = (kontext ?? "").ToLowerInvariant();

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

                if (punkte > 0) bewertet.Add(new KeyValuePair<double, WissensAbschnitt>(punkte, a));
            }

            return bewertet.OrderByDescending(p => p.Key)
                           .Take(anzahl)
                           .Select(p => p.Value)
                           .ToList();
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
                    "Die ausführliche Online-Dokumentation steht unter " +
                    "https://epos-plan.de/epos-plan/epos-plan-dokumetation/ bereit und ist im Menü " +
                    "unter 'Hilfe > Dokumentation' verlinkt. Die Lizenzvereinbarung und die " +
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
            };
        }
    }
}
