using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace KiKern.Tests
{
    /// <summary>Wie Weg B das JSON in den Antworttext einbettet.</summary>
    /// <remarks>
    /// Die fuenf Formen sind keine Spielerei: genau so rahmen Modelle ihre Antworten
    /// (Fachkonzept 3.3, Spalte B - Modelle rahmen JSON gern in Prosa oder Codezaeune).
    /// Jede Aeusserung des Datensatzes bekommt eine andere Form, damit der Toleranzparser
    /// nicht nur den bequemen Fall sieht.
    /// </remarks>
    internal enum Rahmen
    {
        /// <summary>Nur das JSON, sonst nichts.</summary>
        Blank = 0,

        /// <summary>In einem Codezaun mit Sprachangabe.</summary>
        Zaun = 1,

        /// <summary>In einem Codezaun ohne Sprachangabe.</summary>
        ZaunOhneSprache = 2,

        /// <summary>Ein Satz davor.</summary>
        Vorspann = 3,

        /// <summary>Ein Satz davor, ein Satz danach, dazwischen ein Codezaun.</summary>
        Umrahmt = 4
    }

    /// <summary>Eine vorformulierte Anwenderaeusserung samt hinterlegtem Antwortmuster.</summary>
    internal sealed class Aeusserung
    {
        internal Aeusserung(string text, string aktion, string argumente,
                            string erwartetesJson, Rahmen rahmen)
        {
            Text = text;
            Aktion = aktion;
            Argumente = argumente;
            ErwartetesJson = erwartetesJson;
            Rahmen = rahmen;
        }

        /// <summary>Was der Anwender tippt.</summary>
        internal string Text { get; }

        /// <summary>Die Aktion, die dabei herauskommen muss.</summary>
        internal string Aktion { get; }

        /// <summary>Die Argumente, so wie ein Modell sie liefert (Antwortmuster).</summary>
        internal string Argumente { get; }

        /// <summary>Der erwartete Aufruf als KiAufruf.AlsJson - in Deklarationsreihenfolge.</summary>
        internal string ErwartetesJson { get; }

        /// <summary>Rahmung des Weges B.</summary>
        internal Rahmen Rahmen { get; }

        /// <inheritdoc/>
        public override string ToString() => Text;
    }

    /// <summary>
    /// Der Datensatz der 20 Anwenderaeusserungen (Fachkonzept 8, Etappe 2, Abnahme).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Was hier geprueft wird und was nicht.</b> Automatisiert pruefbar ist die
    /// ZUORDNUNG Modellantwort auf KiAufruf: aus einem hinterlegten Antwortmuster muss auf
    /// beiden Wegen derselbe Aufruf entstehen. NICHT geprueft wird, ob ein echtes Modell
    /// auf die Aeusserung hin dieses Muster liefert - das kostet Geld und ist nicht
    /// deterministisch (Fachkonzept 8, Etappe 2: die Modellanbindung selbst wird nicht
    /// automatisiert getestet, sondern ueber eine Pruefliste von Hand).
    /// </para>
    /// <para>
    /// <b>Kein Netzverkehr.</b> Es gibt in diesem Projekt keinen HTTP-Aufruf und keinen
    /// API-Schluessel; alle Rumpfe entstehen hier im Speicher.
    /// </para>
    /// </remarks>
    internal static class Aeusserungen
    {
        /// <summary>Die 20 Aeusserungen mit erwarteter Aktion und Antwortmuster.</summary>
        internal static IReadOnlyList<Aeusserung> Alle { get; } = new[]
        {
            new Aeusserung("Welche Projekte gibt es?",
                           "projekte_auflisten", "{}", "{}", Rahmen.Blank),

            new Aeusserung("Zeig mir alle Projekte in der Datenbank.",
                           "projekte_auflisten", "{}", "{}", Rahmen.Zaun),

            new Aeusserung("Zeig mir die Varianten von Beispiel WP WG 1.",
                           "varianten_auflisten",
                           "{\"projekt_id\":1007}", "{\"projekt_id\":1007}", Rahmen.Vorspann),

            new Aeusserung("Was steht im Kopf von Projekt 1042?",
                           "projekt_lesen",
                           "{\"projekt_id\":1042}", "{\"projekt_id\":1042}", Rahmen.Blank),

            new Aeusserung("Welche Speichervarianten hat Projekt 1042?",
                           "speichervarianten_auflisten",
                           "{\"projekt_id\":1042}", "{\"projekt_id\":1042}", Rahmen.ZaunOhneSprache),

            new Aeusserung("Welche Speichervariante ist bei Projekt 8 aktiv?",
                           "speichervarianten_auflisten",
                           "{\"projekt_id\":8}", "{\"projekt_id\":8}", Rahmen.Umrahmt),

            new Aeusserung("Vergleich mir die Wirtschaftlichkeit von Projekt 1042 und 1043.",
                           "ergebnisse_lesen",
                           "{\"projekt_ids\":[1042,1043]}", "{\"projekt_ids\":[1042,1043]}", Rahmen.Zaun),

            new Aeusserung("Gibt es fuer Projekt 12 schon ein Wirtschaftlichkeitsergebnis?",
                           "ergebnisse_lesen",
                           "{\"projekt_ids\":[12]}", "{\"projekt_ids\":[12]}", Rahmen.Blank),

            new Aeusserung("Mit welchem Stromtarif rechnet Projekt 1042?",
                           "wirtschaftlichkeit_parameter_lesen",
                           "{\"projekt_id\":1042}", "{\"projekt_id\":1042}", Rahmen.Vorspann),

            new Aeusserung("Passt die Kostenlage beim BHKW?",
                           "kostenlage_pruefen",
                           "{\"projekt_id\":1042,\"komponente\":\"BHKW\"}",
                           "{\"projekt_id\":1042,\"komponente\":\"BHKW\"}", Rahmen.Umrahmt),

            new Aeusserung("Stimmen die Kosten der Waermepumpe in Projekt 7?",
                           "kostenlage_pruefen",
                           "{\"komponente\":\"W\u00e4rmepumpe\",\"projekt_id\":7}",
                           "{\"projekt_id\":7,\"komponente\":\"W\u00e4rmepumpe\"}", Rahmen.Zaun),

            new Aeusserung("Was wuerde sich aendern, wenn ich die Waermepumpe von Projekt 3 nach 4 uebernehme?",
                           "uebernahme_vorschau",
                           "{\"von_projekt\":3,\"nach_projekt\":4,\"gewerk\":\"W\u00e4rmepumpe\"}",
                           "{\"von_projekt\":3,\"nach_projekt\":4,\"gewerk\":\"W\u00e4rmepumpe\"}", Rahmen.Blank),

            new Aeusserung("Kann ich das BHKW von Projekt 10 auf Projekt 11 uebernehmen?",
                           "uebernahme_vorschau",
                           "{\"von_projekt\":10,\"nach_projekt\":11,\"gewerk\":\"BHKW\"}",
                           "{\"von_projekt\":10,\"nach_projekt\":11,\"gewerk\":\"BHKW\"}", Rahmen.ZaunOhneSprache),

            new Aeusserung("Laesst sich die Bauart der Waermepumpe von Projekt 3 auf Projekt 4 uebertragen?",
                           "merkmal_vorschau",
                           "{\"von_projekt\":3,\"nach_projekt\":4,\"merkmal\":\"Tab_WP.Bauart\"}",
                           "{\"von_projekt\":3,\"nach_projekt\":4,\"merkmal\":\"Tab_WP.Bauart\"}", Rahmen.Vorspann),

            new Aeusserung("Pruef mir bitte die Lastgangdatei D:\\Lastgang\\werk.csv.",
                           "lastgang_pruefen",
                           "{\"dateipfad\":\"D:\\\\Lastgang\\\\werk.csv\"}",
                           "{\"dateipfad\":\"D:\\\\Lastgang\\\\werk.csv\"}", Rahmen.Zaun),

            new Aeusserung("Welche Ganglinien stehen fuer Projekt 1042 zur Auswahl?",
                           "ganglinien_auflisten",
                           "{\"projekt_id\":1042}", "{\"projekt_id\":1042}", Rahmen.Blank),

            new Aeusserung("Welche Stromganglinien gibt es im Stammkatalog?",
                           "ganglinien_auflisten", "{}", "{}", Rahmen.Umrahmt),

            new Aeusserung("Welche Lastspitze ist mit dem Speicher haltbar?",
                           "minimale_spitze_ermitteln",
                           "{\"ganglinie_id\":55,\"kapazitaet_kwh\":200,\"leistung_kw\":100}",
                           "{\"ganglinie_id\":55,\"kapazitaet_kwh\":200,\"leistung_kw\":100}", Rahmen.Vorspann),

            new Aeusserung("Wie weit komme ich mit 500 kWh und 250 kW bei Ganglinie 61 herunter?",
                           "minimale_spitze_ermitteln",
                           "{\"ganglinie_id\":61,\"kapazitaet_kwh\":500,\"leistung_kw\":250,\"wirkungsgrad_rt\":0.92}",
                           "{\"ganglinie_id\":61,\"kapazitaet_kwh\":500,\"leistung_kw\":250,\"wirkungsgrad_rt\":0.92}",
                           Rahmen.Zaun),

            new Aeusserung("Was hast du in dieser Sitzung bisher gemacht?",
                           "letzte_aktionen",
                           "{\"anzahl\":5}", "{\"anzahl\":5}", Rahmen.ZaunOhneSprache)
        };

        /// <summary>
        /// Gegenprobe: Aeusserungen, die KEINE Aktion ausloesen duerfen. Sie sind Fragen an
        /// die Hilfe, nicht Auftraege - der Assistent muss sie beantworten, nicht ausfuehren.
        /// </summary>
        internal static IReadOnlyList<string> OhneAktion { get; } = new[]
        {
            "Wie funktioniert die Waermepumpen-Simulation?",
            "Was bedeutet der Kapitalwert nach DIN EN 17463?",
            "Wo finde ich die Klimadaten in der Maske?"
        };

        // ============================================================ Antwortmuster

        /// <summary>Weg A: der Antwortrumpf des Anbieters mit einem functionCall-Teil.</summary>
        internal static string RumpfA(string aktion, string argumenteJson, string begleittext = "")
        {
            var teile = new JsonArray();
            if (!string.IsNullOrEmpty(begleittext))
                teile.Add(new JsonObject { ["text"] = begleittext });

            teile.Add(new JsonObject
            {
                ["functionCall"] = new JsonObject
                {
                    ["name"] = aktion,
                    ["args"] = JsonNode.Parse(argumenteJson)
                }
            });

            return Rumpf(teile, "STOP");
        }

        /// <summary>Weg A: ein Rumpf ohne Werkzeugaufruf - reine Textantwort.</summary>
        internal static string RumpfAOhneAufruf(string text)
            => Rumpf(new JsonArray { new JsonObject { ["text"] = text } }, "STOP");

        /// <summary>Weg A: ein Rumpf mit mehreren Aufrufen (Festlegung 4 - nur der erste zaehlt).</summary>
        internal static string RumpfAMehrfach(params string[] aktionen)
        {
            var teile = new JsonArray();
            foreach (string a in aktionen)
                teile.Add(new JsonObject
                {
                    ["functionCall"] = new JsonObject { ["name"] = a, ["args"] = new JsonObject() }
                });
            return Rumpf(teile, "STOP");
        }

        private static string Rumpf(JsonArray teile, string abschlussgrund)
        {
            var wurzel = new JsonObject
            {
                ["candidates"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["content"] = new JsonObject { ["role"] = "model", ["parts"] = teile },
                        ["finishReason"] = abschlussgrund
                    }
                }
            };
            return wurzel.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }

        /// <summary>Weg B: der Antworttext des Modells, in der jeweiligen Rahmung.</summary>
        internal static string TextB(string aktion, string argumenteJson, Rahmen rahmen)
        {
            string kern = "{\"" + KiWerkzeuge.FeldAktion + "\":\"" + aktion + "\",\""
                          + KiWerkzeuge.FeldParameter + "\":" + argumenteJson + "}";

            switch (rahmen)
            {
                case Rahmen.Blank:
                    return kern;
                case Rahmen.Zaun:
                    return "```json\n" + kern + "\n```";
                case Rahmen.ZaunOhneSprache:
                    return "```\n" + kern + "\n```";
                case Rahmen.Vorspann:
                    return "Ich sehe nach:\n" + kern;
                case Rahmen.Umrahmt:
                    return "Klar, das kann ich nachschlagen.\n```json\n" + kern
                           + "\n```\nDanach nenne ich das Ergebnis.";
                default:
                    return kern;
            }
        }
    }
}
