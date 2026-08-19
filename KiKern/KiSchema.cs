using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace KiKern
{
    /// <summary>
    /// Erzeugt aus der Aktionsdeklaration das JSON-Schema der Parameter und den
    /// Werkzeugkatalog fuer das Modell (Fachkonzept 3.2, Verwendung a).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum von Hand und nicht mit <c>JsonSchema.Net</c>.</b> Die Pakete
    /// <c>JsonSchema.Net</c> 7.3.4 und <c>JsonSchema.Net.Generation</c> 5.0.4 sind im
    /// Anwendungsprojekt referenziert und wurden geprueft. Sie kommen hier bewusst NICHT
    /// zum Einsatz - aus drei Gruenden:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <c>JsonSchema.Net.Generation</c> erzeugt Schemata aus CLR-TYPEN. Dafuer brauchte
    /// jede Aktion eine zusaetzliche Parameterklasse - also eine ZWEITE Deklaration neben
    /// <see cref="KiAktion.Parameter"/>. Genau das widerspricht dem Leitsatz „eine
    /// Deklaration, drei Verwendungen" (Fachkonzept 3.2): Schema, Pruefung und Klartext
    /// koennten wieder auseinanderlaufen.
    /// </description></item>
    /// <item><description>
    /// Zielformat ist nicht das vollstaendige Draft 2020-12, sondern die TEILMENGE nach
    /// OpenAPI 3.0, die <c>FunctionDeclaration.Parameters</c> annimmt (type, format,
    /// description, enum, items, properties, required). Eine allgemeine Schemabibliothek
    /// schreibt zusaetzliche Schluesselwoerter (<c>$schema</c>, <c>$defs</c>,
    /// <c>additionalProperties</c>), die wieder herausgefiltert werden muessten.
    /// </description></item>
    /// <item><description>
    /// Die Pruefmeldungen muessen deutscher Klartext sein - fuer den Anwender UND fuer die
    /// Korrekturrunde des Modells. <c>JsonSchema.Net</c> liefert englische, pointerbasierte
    /// Befunde, die ohnehin uebersetzt werden muessten.
    /// </description></item>
    /// </list>
    /// <para>
    /// Der Kern bleibt damit ohne jede Paketreferenz (Fachkonzept 3.7). Die beiden Pakete
    /// im Anwendungsprojekt bleiben unangetastet.
    /// </para>
    /// </remarks>
    public static class KiSchema
    {
        private static readonly JsonSerializerOptions Kompakt = new JsonSerializerOptions
        {
            WriteIndented = false,
            // Umlaute bleiben lesbar - die Ausgabe geht in eine Protokolldatei, die ein
            // Mensch lesen koennen muss, und in einen JSON-Rumpf, der UTF-8 traegt.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly JsonSerializerOptions Eingerueckt = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // ============================================================== Parameterschema

        /// <summary>
        /// Das Parameterschema einer Aktion als JSON-Objekt
        /// (<c>{"type":"object","properties":{…},"required":[…]}</c>).
        /// </summary>
        public static string Erzeuge(KiAktion aktion, bool eingerueckt = false)
        {
            if (aktion == null) throw new ArgumentNullException(nameof(aktion));
            return Schreibe(SchemaKnoten(aktion), eingerueckt);
        }

        /// <summary>Das Parameterschema als Knoten - Grundlage von <see cref="Erzeuge"/>.</summary>
        public static JsonObject SchemaKnoten(KiAktion aktion)
        {
            if (aktion == null) throw new ArgumentNullException(nameof(aktion));

            var eigenschaften = new JsonObject();
            var pflicht = new JsonArray();

            foreach (KiParameter p in aktion.Parameter)
            {
                eigenschaften[p.Name] = ParameterKnoten(p);
                if (p.Pflicht) pflicht.Add(p.Name);
            }

            var schema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = eigenschaften
            };
            if (pflicht.Count > 0) schema["required"] = pflicht;
            return schema;
        }

        private static JsonObject ParameterKnoten(KiParameter p)
        {
            var knoten = new JsonObject();

            switch (p.Typ)
            {
                case KiParameterTyp.Ganzzahl:
                    knoten["type"] = "integer";
                    break;
                case KiParameterTyp.Zahl:
                    knoten["type"] = "number";
                    break;
                case KiParameterTyp.Wahrheitswert:
                    knoten["type"] = "boolean";
                    break;
                case KiParameterTyp.Text:
                case KiParameterTyp.Aufzaehlung:
                    knoten["type"] = "string";
                    break;
                case KiParameterTyp.GanzzahlListe:
                    knoten["type"] = "array";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(p));
            }

            knoten["description"] = p.SchemaBeschreibung();

            if (p.Typ == KiParameterTyp.Aufzaehlung)
            {
                var werte = new JsonArray();
                foreach (string w in p.Werte) werte.Add(w);
                knoten["enum"] = werte;
            }

            if (p.Typ == KiParameterTyp.GanzzahlListe)
                knoten["items"] = new JsonObject { ["type"] = "integer" };

            return knoten;
        }

        // ============================================================== Werkzeugkatalog

        /// <summary>
        /// Der Werkzeugkatalog fuer das Modell: ein JSON-Feld aus
        /// <c>{"name":…,"description":…,"parameters":{…}}</c>.
        /// </summary>
        /// <remarks>
        /// Ab Etappe 2 wird daraus die <c>FunctionDeclaration</c>-Liste des Anbieters
        /// gefuellt. In Etappe 1 dient derselbe Katalog der Werkzeugliste im Chat, aus der
        /// der Anwender eine Aktion von Hand waehlt.
        /// </remarks>
        public static string Werkzeugkatalog(IEnumerable<KiAktion> aktionen, bool eingerueckt = false)
            => Schreibe(WerkzeugkatalogKnoten(aktionen), eingerueckt);

        /// <summary>
        /// Der Werkzeugkatalog als Knoten - die EINE Quelle fuer beide Wege der
        /// Absichtserkennung (<see cref="KiWerkzeuge"/>, Fachkonzept 3.3).
        /// </summary>
        /// <param name="aktionen">Die zu beschreibenden Aktionen.</param>
        /// <param name="parameterAuslassenWennLeer">
        /// <c>true</c> laesst bei einer parameterlosen Aktion das Feld <c>parameters</c>
        /// ganz weg - so verlangt es die OpenAPI-Teilmenge des Anbieters fuer
        /// <c>functionDeclarations</c>. Fuer Anzeige und Werkzeugliste bleibt es bei
        /// <c>false</c>, damit die Form ueber alle Aktionen gleich aussieht.
        /// </param>
        public static JsonArray WerkzeugkatalogKnoten(IEnumerable<KiAktion> aktionen,
                                                      bool parameterAuslassenWennLeer = false)
        {
            if (aktionen == null) throw new ArgumentNullException(nameof(aktionen));

            var feld = new JsonArray();
            foreach (KiAktion a in aktionen)
            {
                var eintrag = new JsonObject
                {
                    ["name"] = a.Name,
                    ["description"] = a.Zweck
                };
                if (!parameterAuslassenWennLeer || a.Parameter.Count > 0)
                    eintrag["parameters"] = SchemaKnoten(a);
                feld.Add(eintrag);
            }
            return feld;
        }

        /// <summary>Der Werkzeugkatalog des gesamten Registers.</summary>
        public static string Werkzeugkatalog(KiRegister register, bool eingerueckt = false)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));
            return Werkzeugkatalog(register.Alle, eingerueckt);
        }

        // ============================================================== Werte

        /// <summary>
        /// Die Parameterwerte eines Aufrufs als kompaktes JSON-Objekt - INVARIANT
        /// (Fachkonzept 3.2, Kulturregel). Die Reihenfolge ist die der Deklaration, damit
        /// zwei gleiche Aufrufe dieselbe Protokollzeile ergeben.
        /// </summary>
        public static string WerteAlsJson(KiAufruf aufruf)
        {
            if (aufruf == null) throw new ArgumentNullException(nameof(aufruf));

            var objekt = new JsonObject();
            foreach (KiParameter p in aufruf.Aktion.Parameter)
            {
                if (!aufruf.Werte.TryGetValue(p.Name, out object? wert)) continue;
                objekt[p.Name] = WertKnoten(wert);
            }
            return Schreibe(objekt, false);
        }

        private static JsonNode? WertKnoten(object? wert)
        {
            switch (wert)
            {
                case null: return null;
                case long l: return JsonValue.Create(l);
                case double d: return JsonValue.Create(d);
                case bool b: return JsonValue.Create(b);
                case string s: return JsonValue.Create(s);
                case long[] feld:
                    {
                        var f = new JsonArray();
                        foreach (long v in feld) f.Add(JsonValue.Create(v));
                        return f;
                    }
                default:
                    return JsonValue.Create(Convert.ToString(wert, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Ein einzelner Wert als ANZEIGETEXT in der uebergebenen Kultur - fuer
        /// Bestaetigung und Chat (Fachkonzept 3.2: nur die Anzeige formatiert kulturabhaengig).
        /// </summary>
        public static string WertAlsText(object? wert, CultureInfo kultur)
        {
            if (kultur == null) throw new ArgumentNullException(nameof(kultur));

            switch (wert)
            {
                case null: return "";
                // IDs ohne Tausenderpunkt - "Projekt 1.007" waere irrefuehrend.
                case long l: return l.ToString(kultur);
                case double d: return d.ToString("0.####", kultur);
                case bool b: return b ? "ja" : "nein";
                case string s: return s;
                case long[] feld:
                    {
                        var teile = new List<string>(feld.Length);
                        foreach (long v in feld) teile.Add(v.ToString(kultur));
                        return string.Join(", ", teile);
                    }
                default:
                    return Convert.ToString(wert, kultur) ?? "";
            }
        }

        private static string Schreibe(JsonNode knoten, bool eingerueckt)
            => knoten.ToJsonString(eingerueckt ? Eingerueckt : Kompakt);
    }
}
