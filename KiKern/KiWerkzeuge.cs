using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace KiKern
{
    /// <summary>
    /// Wie der Werkzeugkatalog auf die Leitung kommt - fuer BEIDE Wege der
    /// Absichtserkennung (Fachkonzept 3.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Weg A (Hauptweg).</b> Der Katalog geht als <c>tools</c>-Feld mit, dazu
    /// <c>toolConfig</c> im Modus <see cref="KiWerkzeugmodus.Auto"/>. Der Aufruf kommt als
    /// <c>functionCall</c> zurueck, das Ergebnis geht als <c>functionResponse</c> hinaus.
    /// </para>
    /// <para>
    /// <b>Weg B (Rueckfall).</b> Derselbe Katalog steht als Text im Prompt, das Modell
    /// antwortet mit einem JSON-Objekt. Entscheidend ist, dass BEIDE Wege aus DERSELBEN
    /// Deklaration gespeist werden (<see cref="KiSchema.WerkzeugkatalogKnoten"/>) - nur so
    /// koennen sie fuer dieselbe Aeusserung denselben <see cref="KiAufruf"/> erzeugen.
    /// </para>
    /// <para>
    /// <b>Was hier bewusst NICHT steht.</b> Kein <c>mode: ANY</c> (das erzwaenge einen
    /// Aufruf, auch wenn der Anwender nur gefragt hat, Fachkonzept 3.3 Festlegung 1) und
    /// keine Bequemwege eines SDK (<c>AddFunction</c>, <c>Invoke</c>,
    /// <c>AutomaticFunctionCallingConfig</c>): sie wuerden die Aktion selbst ausfuehren und
    /// damit Riegel, Bestaetigungsschicht und Protokoll umgehen (Festlegungen 2 und 3).
    /// </para>
    /// <para>
    /// <b>Warum die Texte hier Konstanten bleiben.</b> Die Anweisungssaetze des Weges B
    /// gehen an das MODELL, nicht an den Anwender. Sie sind damit weder Anzeigetext noch
    /// Persistenzwert (Drei-Schichten-Regel) und gehoeren nicht in die Ressourcen: eine
    /// uebersetzte Anweisung wuerde die Antwortform veraendern, die der Parser erwartet.
    /// </para>
    /// </remarks>
    public static class KiWerkzeuge
    {
        /// <summary>
        /// Hoechstzahl der Modellrunden je Anwenderaeusserung (Fachkonzept 3.3, Festlegung 5):
        /// Aufruf, Ergebnis, Antwort - plus nichts. Schuetzt vor Schleifen und Tageslimit.
        /// </summary>
        public const int Rundendeckel = 3;

        /// <summary>Schluessel des Antwortfeldes „Aktion" im Weg B.</summary>
        public const string FeldAktion = "aktion";

        /// <summary>Schluessel des Antwortfeldes „Parameter" im Weg B.</summary>
        public const string FeldParameter = "parameter";

        /// <summary>Aktionsname, mit dem das Modell im Weg B „keine Aktion" ausdrueckt.</summary>
        public const string KeineAktion = "keine";

        private static readonly JsonSerializerOptions Kompakt = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly JsonSerializerOptions Eingerueckt = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // ===================================================================== Weg A

        /// <summary>
        /// Das <c>tools</c>-Feld der Anfrage:
        /// <c>[{"functionDeclarations":[{"name":…,"description":…,"parameters":{…}}]}]</c>.
        /// </summary>
        /// <remarks>
        /// Aktionen ohne Parameter bekommen KEIN <c>parameters</c>-Feld. Ein leeres
        /// <c>properties</c>-Objekt ist in der OpenAPI-Teilmenge des Anbieters nicht
        /// vorgesehen und wird je nach Modellstand beanstandet; das Weglassen ist die
        /// dokumentierte Form fuer parameterlose Funktionen.
        /// </remarks>
        public static JsonArray ToolsKnoten(KiRegister register)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));

            return new JsonArray
            {
                new JsonObject
                {
                    ["functionDeclarations"] = KiSchema.WerkzeugkatalogKnoten(register.Alle, true)
                }
            };
        }

        /// <summary>Das <c>tools</c>-Feld als Text - fuer Anzeige und Selbstpruefung.</summary>
        public static string Tools(KiRegister register, bool eingerueckt = false)
            => Schreibe(ToolsKnoten(register), eingerueckt);

        /// <summary>
        /// Das <c>toolConfig</c>-Feld: <c>{"functionCallingConfig":{"mode":"AUTO"}}</c>.
        /// </summary>
        public static JsonObject ToolConfigKnoten(KiWerkzeugmodus modus)
        {
            return new JsonObject
            {
                ["functionCallingConfig"] = new JsonObject
                {
                    ["mode"] = ModusSchluessel(modus)
                }
            };
        }

        /// <summary>Das <c>toolConfig</c>-Feld als Text.</summary>
        public static string ToolConfig(KiWerkzeugmodus modus, bool eingerueckt = false)
            => Schreibe(ToolConfigKnoten(modus), eingerueckt);

        /// <summary>Drahtname des Modus.</summary>
        public static string ModusSchluessel(KiWerkzeugmodus modus)
        {
            switch (modus)
            {
                case KiWerkzeugmodus.Auto: return "AUTO";
                case KiWerkzeugmodus.Aus: return "NONE";
                default: throw new ArgumentOutOfRangeException(nameof(modus));
            }
        }

        /// <summary>
        /// Ein Verlaufsteil mit dem Ergebnis einer Aktion:
        /// <c>{"functionResponse":{"name":…,"response":{…}}}</c>.
        /// </summary>
        public static JsonObject AntwortteilKnoten(string name, string inhaltJson)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name fehlt.", nameof(name));

            JsonNode? inhalt;
            try { inhalt = JsonNode.Parse(string.IsNullOrWhiteSpace(inhaltJson) ? "{}" : inhaltJson); }
            catch (JsonException) { inhalt = new JsonObject { ["text"] = inhaltJson }; }

            return new JsonObject
            {
                ["functionResponse"] = new JsonObject
                {
                    ["name"] = name,
                    ["response"] = inhalt ?? new JsonObject()
                }
            };
        }

        /// <summary>
        /// Ein Verlaufsteil mit dem Aufruf des Modells:
        /// <c>{"functionCall":{"name":…,"args":{…}}}</c>. Wird gebraucht, um die eigene
        /// Runde des Modells in den Verlauf zurueckzuschreiben, bevor das Ergebnis folgt.
        /// </summary>
        public static JsonObject AufrufteilKnoten(string name, string argumenteJson)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name fehlt.", nameof(name));

            JsonNode? args;
            try { args = JsonNode.Parse(string.IsNullOrWhiteSpace(argumenteJson) ? "{}" : argumenteJson); }
            catch (JsonException) { args = new JsonObject(); }

            return new JsonObject
            {
                ["functionCall"] = new JsonObject
                {
                    ["name"] = name,
                    ["args"] = args ?? new JsonObject()
                }
            };
        }

        /// <summary>Ein Verlaufsteil mit reinem Text.</summary>
        public static JsonObject TextteilKnoten(string? text)
            => new JsonObject { ["text"] = text ?? "" };

        /// <summary>
        /// Ein Verlaufseintrag <c>{"role":…,"parts":[…]}</c>.
        /// </summary>
        /// <remarks>
        /// Zulaessig sind nur die Rollen „user" und „model"; das Ergebnis einer Aktion geht
        /// deshalb als „user"-Eintrag zurueck, obwohl es nicht der Anwender geschrieben hat.
        /// </remarks>
        public static JsonObject VerlaufseintragKnoten(string rolle, params JsonNode[] teile)
        {
            var feld = new JsonArray();
            foreach (JsonNode t in teile) feld.Add(t);
            return new JsonObject { ["role"] = rolle, ["parts"] = feld };
        }

        /// <summary>Rollenname des Anwenders (und der Werkzeugergebnisse).</summary>
        public const string RolleAnwender = "user";

        /// <summary>Rollenname des Modells.</summary>
        public const string RolleModell = "model";

        // ===================================================================== Weg B

        /// <summary>
        /// Der Anweisungsblock des Rueckfallweges: Antwortform, Verbot der Erfindung,
        /// Kulturregel und der vollstaendige Katalog.
        /// </summary>
        public static string WegBAnweisung(KiRegister register)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));

            var sb = new StringBuilder();
            sb.Append("Verlangt die Frage eine der unten aufgefuehrten Aktionen, antworte AUSSCHLIESSLICH ")
              .Append("mit einem JSON-Objekt und ohne jeden weiteren Text:\n");
            sb.Append("{\"").Append(FeldAktion).Append("\":\"<name>\",\"")
              .Append(FeldParameter).Append("\":{ ... }}\n");
            sb.Append("Passt keine Aktion, antworte in gewoehnlichem Text und ohne JSON.\n");
            sb.Append("Erfinde weder Aktionsnamen noch Parameter. Nimm Werte nur aus der Frage oder aus ")
              .Append("einem vorangegangenen Aktionsergebnis; was fehlt, erfragst du im Klartext.\n");
            sb.Append("Zahlen invariant schreiben (Punkt als Dezimaltrennzeichen, kein Tausenderzeichen).\n");
            sb.Append("Hoechstens EINE Aktion je Antwort.\n");
            sb.Append("Verfuegbare Aktionen:\n");
            sb.Append(KiSchema.Werkzeugkatalog(register, true));
            sb.Append('\n');
            return sb.ToString();
        }

        // ===================================================================== Hilfen

        private static string Schreibe(JsonNode knoten, bool eingerueckt)
            => knoten.ToJsonString(eingerueckt ? Eingerueckt : Kompakt);
    }

    /// <summary>
    /// Steuerung des Werkzeugaufrufs (Fachkonzept 3.3, Festlegung 1).
    /// </summary>
    /// <remarks>
    /// Der Modus „ANY" (Aufruf erzwingen) ist bewusst NICHT abgebildet. Er wuerde das
    /// Modell zwingen, auch auf eine reine Frage hin eine Aktion zu rufen - und damit die
    /// Grenze zwischen Auskunft und Eingriff aufheben.
    /// </remarks>
    public enum KiWerkzeugmodus
    {
        /// <summary>Keine Werkzeuge - das Modell darf nur reden.</summary>
        Aus = 0,

        /// <summary>Das Modell entscheidet selbst, ob es eine Aktion ruft.</summary>
        Auto = 1
    }
}
