using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace KiKern
{
    /// <summary>
    /// Die Platzhaltertabelle einer Sitzung (Fachkonzept 4.2, Zeile „Bezeichner").
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nach aussen geht „Name 1", zurueck kommt „Name 1", angezeigt wird der Klarname. Das
    /// Modell rechnet also mit Platzhaltern und IDs, der Anwender sieht Klartext. Die
    /// Tabelle lebt nur in der Sitzung und wird nirgends abgelegt.
    /// </para>
    /// <para>
    /// Die Zuordnung ist eineindeutig: derselbe Klarname bekommt in derselben Sitzung
    /// immer denselben Platzhalter. Sonst koennte das Modell zwei Nennungen desselben
    /// Projekts nicht als dasselbe erkennen.
    /// </para>
    /// </remarks>
    public sealed class KiPlatzhalter
    {
        /// <summary>Wortstamm aller Platzhalter.</summary>
        public const string Stamm = "Name";

        /// <summary>Obergrenze der Tabelle - gegen unbegrenztes Wachsen in langen Sitzungen.</summary>
        public const int MaxEintraege = 500;

        private readonly Dictionary<string, string> _nachKlarname = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _nachPlatzhalter = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Zahl der gefuehrten Bezeichner.</summary>
        public int Anzahl => _nachKlarname.Count;

        /// <summary>
        /// Der Platzhalter fuer einen Klarnamen; legt ihn beim ersten Mal an. Leere Texte
        /// bleiben leer - es gibt nichts zu verbergen.
        /// </summary>
        public string Fuer(string? klarname)
        {
            if (string.IsNullOrWhiteSpace(klarname)) return "";
            string k = klarname!;

            if (_nachKlarname.TryGetValue(k, out string? vorhanden)) return vorhanden;
            if (_nachKlarname.Count >= MaxEintraege) return Stamm + " ?";

            string neu = Stamm + " " + (_nachKlarname.Count + 1).ToString(CultureInfo.InvariantCulture);
            _nachKlarname[k] = neu;
            _nachPlatzhalter[neu] = k;
            return neu;
        }

        /// <summary>Der Klarname zu einem Platzhalter; <c>null</c>, wenn unbekannt.</summary>
        public string? Klarname(string? platzhalter)
            => platzhalter != null && _nachPlatzhalter.TryGetValue(platzhalter, out string? k) ? k : null;

        /// <summary>
        /// Ersetzt alle bekannten Platzhalter eines Modelltextes durch die Klarnamen - der
        /// Rueckweg vor der Anzeige im Chat.
        /// </summary>
        /// <remarks>
        /// Ersetzt wird von der hoechsten Nummer abwaerts, sonst wuerde „Name 1" den Anfang
        /// von „Name 12" treffen.
        /// </remarks>
        public string Aufloesen(string? text)
        {
            if (string.IsNullOrEmpty(text) || _nachPlatzhalter.Count == 0) return text ?? "";

            var platzhalter = new List<string>(_nachPlatzhalter.Keys);
            platzhalter.Sort((a, b) => b.Length.CompareTo(a.Length) != 0
                                       ? b.Length.CompareTo(a.Length)
                                       : string.CompareOrdinal(b, a));

            string ergebnis = text!;
            foreach (string p in platzhalter)
                ergebnis = ergebnis.Replace(p, _nachPlatzhalter[p]);
            return ergebnis;
        }

        /// <summary>
        /// Der Rueckweg fuer EINEN Argumentwert: aus „Name 3" wird wieder der Klarname.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum es das braucht.</b> Der Hinweg ersetzt JEDEN Bezeichner einer
        /// Ergebniszeile vollstaendig (<see cref="KiRueckmeldung"/>). Das Modell kennt
        /// Projekte und Varianten danach nur noch als „Name n" - und uebergibt genau das
        /// als Parameter, wenn es sich auf eine Ergebniszeile bezieht. Ohne diesen
        /// Rueckweg suchte die Namensaufloesung des Registers nach einem Projekt namens
        /// „Name 3" und fande keines: der Aufruf scheiterte, obwohl Modell und Programm
        /// dasselbe meinen.
        /// </para>
        /// <para>
        /// <b>Zwei Faelle in einer Methode.</b> Ist der GANZE Wert ein bekannter
        /// Platzhalter, steht danach genau der Klarname da - auch dann, wenn dieser selbst
        /// wie ein Platzhalter aussaehe. Sonst wird ueber <see cref="Aufloesen"/> jeder
        /// EINGEBETTETE Platzhalter ersetzt; so bleibt eine Semikolonliste
        /// („Name 3; Name 5") eine Semikolonliste, und „Name 12" wird nicht als „Name 1"
        /// mit angehaengter Zwoelf missdeutet.
        /// </para>
        /// <para>
        /// Unbekannter Text geht unveraendert durch. Das ist der Regelfall, wenn der
        /// Anwender einen Klarnamen selbst getippt hat - er soll die lokale
        /// Namensaufloesung unbeschadet erreichen.
        /// </para>
        /// </remarks>
        public string KlarnameOderText(string? text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";

            string? genau = Klarname(text!.Trim());
            if (genau != null) return genau;

            return Aufloesen(text);
        }

        /// <summary>
        /// Der Rueckweg fuer die ARGUMENTE eines Werkzeugaufrufs: jeder Textwert des
        /// JSON-Objekts laeuft durch <see cref="KlarnameOderText"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nur Zeichenketten.</b> Zahlen, Wahrheitswerte und ID-Listen gehen unberuehrt
        /// durch - sie waren nie platzgehalten und haben mit der Bezeichnertabelle nichts
        /// zu tun. Wuerde hier eine ID angefasst, entstuende genau die Verwechslung, die
        /// die Platzhalterung vermeiden soll.
        /// </para>
        /// <para>
        /// <b>Fehlertoleranz mit Absicht.</b> Ist der Argumenttext kein JSON-Objekt, geht
        /// er unveraendert zurueck. Die Beanstandung gehoert der Parameterpruefung
        /// (<see cref="KiPruefung"/>), die dem Modell einen Klartextgrund nennen kann -
        /// hier waere sie nur eine zweite, stille Fehlerquelle.
        /// </para>
        /// </remarks>
        public string ArgumenteAufloesen(string? argumenteJson)
        {
            if (string.IsNullOrWhiteSpace(argumenteJson)) return argumenteJson ?? "";
            if (_nachPlatzhalter.Count == 0) return argumenteJson!;

            try
            {
                JsonNode? wurzel = JsonNode.Parse(argumenteJson!);
                if (!(wurzel is JsonObject)) return argumenteJson!;

                JsonNode? neu = Umschreiben(wurzel);
                return neu == null ? argumenteJson! : neu.ToJsonString(ArgumentSchreibweise);
            }
            catch (JsonException)
            {
                return argumenteJson!;
            }
        }

        /// <summary>Leert die Tabelle (Sitzungswechsel, Tests).</summary>
        public void Leeren()
        {
            _nachKlarname.Clear();
            _nachPlatzhalter.Clear();
        }

        // ===================================================================== Innen

        /// <summary>
        /// Kompakt und ohne Maskierung der Umlaute - der Text geht sofort wieder in
        /// <see cref="KiPruefung.PruefeJson"/> und soll dabei lesbar bleiben.
        /// </summary>
        private static readonly JsonSerializerOptions ArgumentSchreibweise = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Baut den Knoten NEU auf, statt ihn zu aendern: ein Knoten mit Elternteil laesst
        /// sich nicht umhaengen, und ein Woerterbuch waehrend der Aufzaehlung zu
        /// beschreiben ist ohnehin nicht erlaubt.
        /// </summary>
        private JsonNode? Umschreiben(JsonNode? knoten)
        {
            switch (knoten)
            {
                case null:
                    return null;

                case JsonObject objekt:
                    {
                        var neu = new JsonObject();
                        foreach (KeyValuePair<string, JsonNode?> e in objekt)
                            neu[e.Key] = Umschreiben(e.Value);
                        return neu;
                    }

                case JsonArray feld:
                    {
                        var neu = new JsonArray();
                        foreach (JsonNode? glied in feld) neu.Add(Umschreiben(glied));
                        return neu;
                    }

                case JsonValue wert:
                    return wert.TryGetValue(out string? s)
                        ? JsonValue.Create(KlarnameOderText(s))
                        : wert.DeepClone();

                default:
                    return knoten.DeepClone();
            }
        }
    }

    /// <summary>
    /// Verdichtet ein <see cref="KiErgebnis"/> zu der Rueckmeldung, die an das Modell geht
    /// (Fachkonzept 3.6 und 4.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nicht dasselbe wie die Chatantwort.</b> In den Chat geht der volle Text mit
    /// Klarnamen; an das Modell geht eine gekuerzte und platzgehaltene Fassung. Die Regel
    /// aus 4.2 ist hier umgesetzt:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Zahlen, Wahrheitswerte und IDs gehen unveraendert mit - sie sind
    /// technische Kennwerte ohne Bezug.</description></item>
    /// <item><description>JEDE Zeichenkette eines Nutzdatenfeldes wird platzgehalten.
    /// Bewusst pauschal: eine Liste „welcher Text ist ein Bezeichner" waere eine zweite
    /// Regelquelle, die man zu pflegen vergisst. Der Anwender sieht ohnehin den
    /// Klarnamen.</description></item>
    /// <item><description>Nie ganze Reihen: hoechstens <see cref="MaxZeilen"/> Zeilen, der
    /// Rest nur als Zahl.</description></item>
    /// </list>
    /// </remarks>
    public static class KiRueckmeldung
    {
        /// <summary>Hoechstzahl der Zeilen, die an das Modell gehen.</summary>
        public const int MaxZeilen = 20;

        private static readonly JsonSerializerOptions Kompakt = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Die Rueckmeldung als JSON-Objekt:
        /// <c>{"status":…,"anzahl":…,"text":…,"zeilen":[…],"meldungen":[…]}</c>.
        /// </summary>
        public static string Erzeuge(KiAufruf aufruf, KiErgebnis ergebnis,
                                     KiPlatzhalter? platzhalter = null, int maxZeilen = MaxZeilen)
        {
            if (aufruf == null) throw new ArgumentNullException(nameof(aufruf));
            if (ergebnis == null) throw new ArgumentNullException(nameof(ergebnis));

            // ERST die Zeilen: dabei entstehen die Platzhalter. Wuerde der Ergebnissatz
            // zuerst gesaeubert, waere die Tabelle noch leer und ein Klarname, der NUR im
            // Satz steht, ginge ungeschuetzt hinaus.
            JsonArray? zeilenfeld = null;
            int weitere = 0;
            if (ergebnis.Zeilen.Count > 0)
            {
                zeilenfeld = new JsonArray();
                int genommen = Math.Min(maxZeilen, ergebnis.Zeilen.Count);
                for (int i = 0; i < genommen; i++)
                    zeilenfeld.Add(ZeilenKnoten(ergebnis.Zeilen[i], platzhalter));
                weitere = ergebnis.Zeilen.Count - genommen;
            }

            var objekt = new JsonObject
            {
                ["aktion"] = aufruf.Name,
                ["status"] = SchutzstufeText.Schluessel(ergebnis.Status),
                ["anzahl"] = ergebnis.Anzahl,
                ["text"] = Saeubern(ergebnis.Text, platzhalter)
            };

            if (zeilenfeld != null)
            {
                objekt["zeilen"] = zeilenfeld;
                if (weitere > 0) objekt["weitere_zeilen"] = weitere;
            }

            if (ergebnis.Meldungen.Count > 0)
            {
                var meldungen = new JsonArray();
                foreach (string m in ergebnis.Meldungen) meldungen.Add(Saeubern(m, platzhalter));
                objekt["meldungen"] = meldungen;
            }

            return objekt.ToJsonString(Kompakt);
        }

        /// <summary>
        /// Die Rueckmeldung fuer einen Versuch, der gar nicht erst lief: fehlerhafte
        /// Parameter, unbekannte Aktion, geschlossener Riegel. Sie geht als
        /// <c>functionResponse</c> zurueck und loest die Korrekturrunde aus.
        /// </summary>
        /// <remarks>
        /// Auch der Ablehnungsgrund ist ein SATZ auf dem Weg zum Modell und laeuft durch
        /// <see cref="Saeubern"/>: bekannte Bezeichner werden ersetzt, Bedien- und
        /// Fachsprache bleibt fuer die Korrekturrunde stehen - dieselbe Regel wie fuer
        /// den Ergebnissatz in <see cref="Erzeuge"/> (Fachkonzept 4.2). Klarnamen, die
        /// die Tabelle noch nicht kennt, muss der Aufrufer VORHER anmelden
        /// (<see cref="KiPlatzhalter.Fuer"/>) - aus dem Satz allein laesst sich nicht
        /// ablesen, welcher Teil ein Bezeichner ist.
        /// </remarks>
        public static string Abgelehnt(string? aktion, string grund, KiPlatzhalter? platzhalter = null)
        {
            var objekt = new JsonObject
            {
                ["aktion"] = aktion ?? "",
                ["status"] = SchutzstufeText.Schluessel(KiStatus.Abgelehnt),
                ["grund"] = Saeubern(grund, platzhalter)
            };
            return objekt.ToJsonString(Kompakt);
        }

        // ===================================================================== Innen

        private static JsonObject ZeilenKnoten(IReadOnlyDictionary<string, object?>? zeile,
                                               KiPlatzhalter? platzhalter)
        {
            var knoten = new JsonObject();
            if (zeile == null) return knoten;

            foreach (KeyValuePair<string, object?> feld in zeile)
                knoten[feld.Key] = WertKnoten(feld.Value, platzhalter);
            return knoten;
        }

        private static JsonNode? WertKnoten(object? wert, KiPlatzhalter? platzhalter)
        {
            switch (wert)
            {
                case null: return null;
                case bool b: return JsonValue.Create(b);
                case string s: return JsonValue.Create(Ersetze(s, platzhalter));
                case long l: return JsonValue.Create(l);
                case int i: return JsonValue.Create(i);
                case short sh: return JsonValue.Create((int)sh);
                case byte by: return JsonValue.Create((int)by);
                case double d: return JsonValue.Create(d);
                case float f: return JsonValue.Create((double)f);
                case decimal m: return JsonValue.Create((double)m);
                case DateTime dt: return JsonValue.Create(dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                default:
                    return JsonValue.Create(Ersetze(Convert.ToString(wert, CultureInfo.InvariantCulture),
                                                    platzhalter));
            }
        }

        /// <summary>
        /// Ein einzelner Nutzdatentext wird VOLLSTAENDIG durch seinen Platzhalter ersetzt -
        /// Feldwerte sind Bezeichner, keine Saetze.
        /// </summary>
        private static string Ersetze(string? text, KiPlatzhalter? platzhalter)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            if (platzhalter == null) return text!;
            return platzhalter.Fuer(text);
        }

        /// <summary>
        /// Der Ergebnissatz ist ein SATZ, kein Feldwert. Aus ihm koennen nur die bereits
        /// bekannten Bezeichner ersetzt werden - alles andere ist Bedien- und Fachsprache.
        /// </summary>
        private static string Saeubern(string? satz, KiPlatzhalter? platzhalter)
        {
            if (string.IsNullOrEmpty(satz)) return "";
            if (platzhalter == null) return satz!;

            string ergebnis = satz!;
            foreach (KeyValuePair<string, string> e in BekannteKlarnamen(platzhalter))
                ergebnis = ergebnis.Replace(e.Key, e.Value);
            return ergebnis;
        }

        private static IEnumerable<KeyValuePair<string, string>> BekannteKlarnamen(KiPlatzhalter platzhalter)
        {
            // Die Tabelle wird nur gelesen; angelegt wird hier nichts. Lange Klarnamen
            // zuerst, damit ein enthaltener kurzer Name den langen nicht zerschneidet.
            var namen = new List<string>();
            for (int i = 1; i <= platzhalter.Anzahl; i++)
            {
                string? k = platzhalter.Klarname(KiPlatzhalter.Stamm + " " + i.ToString(CultureInfo.InvariantCulture));
                if (k != null) namen.Add(k);
            }
            namen.Sort((a, b) => b.Length.CompareTo(a.Length));

            foreach (string k in namen)
                yield return new KeyValuePair<string, string>(k, platzhalter.Fuer(k));
        }
    }
}
