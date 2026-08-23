using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace KiKern
{
    /// <summary>Befund der Absichtserkennung: was das Modell wollte und ob es geht.</summary>
    public sealed class KiAbsichtBefund
    {
        internal KiAbsichtBefund(string werkzeugname, KiAufruf? aufruf, string text,
                                 IReadOnlyList<string> fehler, IReadOnlyList<string> uebergangen)
        {
            Werkzeugname = werkzeugname ?? "";
            Aufruf = aufruf;
            Text = text ?? "";
            Fehler = fehler ?? Array.Empty<string>();
            Uebergangen = uebergangen ?? Array.Empty<string>();
        }

        /// <summary>Name, den das Modell gerufen hat; leer, wenn es nur geredet hat.</summary>
        public string Werkzeugname { get; }

        /// <summary>Der gepruefte Aufruf; <c>null</c>, wenn keiner zustande kam.</summary>
        public KiAufruf? Aufruf { get; }

        /// <summary>Begleittext des Modells (kann leer sein).</summary>
        public string Text { get; }

        /// <summary>Beanstandungen in Klartext - Grundlage der Korrekturrunde.</summary>
        public IReadOnlyList<string> Fehler { get; }

        /// <summary>Weitere Aktionsnamen, die das Modell zugleich vorgeschlagen hat.</summary>
        public IReadOnlyList<string> Uebergangen { get; }

        /// <summary>Wollte das Modell ueberhaupt eine Aktion?</summary>
        public bool HatAbsicht => Werkzeugname.Length > 0;

        /// <summary>Steht ein ausfuehrbarer Aufruf bereit?</summary>
        public bool Gueltig => Aufruf != null;

        /// <summary>Alle Beanstandungen in einem Absatz.</summary>
        public string FehlerText() => string.Join(" ", Fehler);

        /// <summary>Ein Befund ohne Absicht - das Modell hat nur geantwortet.</summary>
        public static KiAbsichtBefund NurText(string? text)
            => new KiAbsichtBefund("", null, text ?? "", Array.Empty<string>(), Array.Empty<string>());
    }

    /// <summary>
    /// Aus einer Modellantwort wird ein <see cref="KiAufruf"/> - fuer BEIDE Wege der
    /// Absichtserkennung (Fachkonzept 3.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Eine Pruefstelle fuer beide Wege.</b> Weg A und Weg B unterscheiden sich nur
    /// darin, WIE Name und Argumenttext aus der Antwort geholt werden. Danach laeuft
    /// beides durch dieselbe <see cref="KiPruefung.PruefeJson"/> gegen dasselbe Register -
    /// deshalb kann fuer dieselbe Aeusserung nur derselbe Aufruf herauskommen. Genau das
    /// prueft die Abnahme („bei abgeschaltetem Werkzeugpfad liefert Weg B dieselben
    /// Aufrufe", Fachkonzept 8/Etappe 2).
    /// </para>
    /// <para>
    /// <b>Hoechstens eine Aktion je Aeusserung</b> (Fachkonzept 3.3, Festlegung 4). Liefert
    /// das Modell mehrere Aufrufe, wird der erste genommen; die uebrigen stehen in
    /// <see cref="KiAbsichtBefund.Uebergangen"/> und werden dem Anwender im Klartext als
    /// Vorschlag angeboten - nicht ausgefuehrt.
    /// </para>
    /// </remarks>
    public static class KiAbsicht
    {
        /// <summary>Schluessel, unter denen ein Modell den Aktionsnamen liefern koennte.</summary>
        private static readonly string[] NAMENSFELDER =
            { KiWerkzeuge.FeldAktion, "name", "funktion", "function", "tool", "werkzeug", "action" };

        /// <summary>Schluessel, unter denen ein Modell die Parameter liefern koennte.</summary>
        private static readonly string[] PARAMETERFELDER =
            { KiWerkzeuge.FeldParameter, "parameters", "args", "arguments", "argumente", "werte" };

        /// <summary>Namen, mit denen ein Modell „keine Aktion" ausdrueckt.</summary>
        private static readonly string[] LEERNAMEN =
            { KiWerkzeuge.KeineAktion, "none", "null", "keine_aktion", "nichts" };

        // ======================================================================= Weg A

        /// <summary>
        /// Weg A: aus den <c>functionCall</c>-Teilen der Antwort einen Aufruf machen.
        /// </summary>
        /// <param name="register">Das Aktionsregister.</param>
        /// <param name="antwort">Die zerlegte Modellantwort.</param>
        /// <param name="platzhalter">
        /// Bezeichnertabelle der Sitzung; <c>null</c> = ohne Rueckuebersetzung der
        /// Argumente (Aktionsharnisch, Tests ohne Datenschutzschicht).
        /// </param>
        public static KiAbsichtBefund AusWerkzeugantwort(KiRegister register, KiModellantwort? antwort,
                                                         KiPlatzhalter? platzhalter = null)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));
            if (antwort == null || !antwort.HatWerkzeugruf)
                return KiAbsichtBefund.NurText(antwort?.Text);

            KiWerkzeugruf erster = antwort.Werkzeugrufe[0];

            var uebergangen = new List<string>();
            for (int i = 1; i < antwort.Werkzeugrufe.Count; i++)
                uebergangen.Add(antwort.Werkzeugrufe[i].Name);

            return Bauen(register, erster.Name, erster.ArgumenteJson, antwort.Text, uebergangen,
                         platzhalter);
        }

        // ======================================================================= Weg B

        /// <summary>
        /// Weg B: aus dem Antworttext ein JSON-Objekt herausloesen und daraus einen Aufruf
        /// machen. Kommt kein verwertbares Objekt vor, ist es eine reine Textantwort.
        /// </summary>
        /// <param name="register">Das Aktionsregister.</param>
        /// <param name="text">Der Antworttext des Modells.</param>
        /// <param name="platzhalter">
        /// Bezeichnertabelle der Sitzung; <c>null</c> = ohne Rueckuebersetzung der
        /// Argumente. Der BEGLEITTEXT bleibt hier in jedem Fall unangetastet - er wird
        /// erst unmittelbar vor der Anzeige aufgeloest.
        /// </param>
        public static KiAbsichtBefund AusText(KiRegister register, string? text,
                                              KiPlatzhalter? platzhalter = null)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));

            string? json = JsonAusText(text);
            if (json == null) return KiAbsichtBefund.NurText(text);

            string name;
            string argumente;
            if (!Zerlege(json, out name, out argumente)) return KiAbsichtBefund.NurText(text);

            foreach (string leer in LEERNAMEN)
                if (string.Equals(name, leer, StringComparison.OrdinalIgnoreCase))
                    return KiAbsichtBefund.NurText(Ohne(text, json));

            // Ohne(...) MUSS vor der Rueckuebersetzung stehen: es sucht den JSON-Block
            // woertlich im Antworttext, und ein umgeschriebener Block waere dort nicht
            // mehr zu finden - der Begleittext trueg das JSON dann doppelt.
            return Bauen(register, name, argumente, Ohne(text, json), new List<string>(), platzhalter);
        }

        /// <summary>
        /// Toleranzparser: findet das erste vollstaendige JSON-Objekt im Text, auch wenn es
        /// in Prosa oder in einem Codezaun steckt. Liefert <c>null</c>, wenn keines drin ist.
        /// </summary>
        /// <remarks>
        /// Modelle rahmen JSON gern in <c>```json … ```</c> oder schreiben einen Satz davor
        /// (Fachkonzept 3.3, Spalte B: „braucht Toleranzparser"). Gezaehlt wird ueber die
        /// Klammertiefe, Zeichenketten und Maskierungen werden dabei uebersprungen - sonst
        /// beendete eine geschweifte Klammer IN einem Wert das Objekt zu frueh.
        /// </remarks>
        public static string? JsonAusText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string t = text!;

            int start = t.IndexOf('{');
            while (start >= 0)
            {
                int tiefe = 0;
                bool inText = false;
                bool maskiert = false;

                for (int i = start; i < t.Length; i++)
                {
                    char c = t[i];

                    if (maskiert) { maskiert = false; continue; }
                    if (c == '\\' && inText) { maskiert = true; continue; }
                    if (c == '"') { inText = !inText; continue; }
                    if (inText) continue;

                    if (c == '{') tiefe++;
                    else if (c == '}')
                    {
                        tiefe--;
                        if (tiefe == 0)
                        {
                            string kandidat = t.Substring(start, i - start + 1);
                            if (IstObjekt(kandidat)) return kandidat;
                            break;      // dieses Objekt war unbrauchbar - naechstes suchen
                        }
                    }
                }

                start = t.IndexOf('{', start + 1);
            }
            return null;
        }

        // ======================================================================= Innen

        /// <summary>Der gemeinsame Rest beider Wege: pruefen und Befund bauen.</summary>
        /// <remarks>
        /// <b>Die Platzhalter fallen VOR der Pruefung.</b> Das Modell hat die Bezeichner
        /// nur als „Name n" gesehen (Fachkonzept 4.2) und gibt genau das zurueck. Wuerde
        /// erst geprueft und danach uebersetzt, liefe der Platzhalter durch die
        /// Namensaufloesung des Registers - und die suchte ein Projekt namens „Name 3".
        /// Weil nur ZEICHENKETTEN angefasst werden, bleiben IDs und Zahlen, wie sie sind.
        /// </remarks>
        private static KiAbsichtBefund Bauen(KiRegister register, string name, string argumenteJson,
                                             string? text, List<string> uebergangen,
                                             KiPlatzhalter? platzhalter = null)
        {
            string argumente = platzhalter != null
                ? platzhalter.ArgumenteAufloesen(argumenteJson)
                : argumenteJson;

            KiPruefErgebnis pruefung = KiPruefung.PruefeJson(register, name, argumente);

            if (uebergangen.Count > 0)
            {
                // Der Hinweis gehoert in den Klartext, nicht in die Fehlerliste: der erste
                // Aufruf ist ja in Ordnung, es wurde nur nicht alles genommen.
                text = Anhaengen(text, string.Format(CultureInfo.CurrentCulture,
                                                     KiTexte.MehrereWerkzeuge, name,
                                                     string.Join(", ", uebergangen)));
            }

            return new KiAbsichtBefund(name, pruefung.Aufruf, text ?? "", pruefung.Fehler, uebergangen);
        }

        private static bool Zerlege(string json, out string name, out string argumente)
        {
            name = "";
            argumente = "{}";

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;

                foreach (string feld in NAMENSFELDER)
                {
                    if (doc.RootElement.TryGetProperty(feld, out JsonElement n)
                        && n.ValueKind == JsonValueKind.String)
                    {
                        name = (n.GetString() ?? "").Trim();
                        break;
                    }
                }
                if (name.Length == 0) return false;

                foreach (string feld in PARAMETERFELDER)
                {
                    if (doc.RootElement.TryGetProperty(feld, out JsonElement p))
                    {
                        if (p.ValueKind == JsonValueKind.Object) argumente = p.GetRawText();
                        // Ein Modell schickt die Parameter gelegentlich als JSON-TEXT statt
                        // als Objekt - das ist ein haeufiger und harmloser Fehlgriff.
                        else if (p.ValueKind == JsonValueKind.String) argumente = p.GetString() ?? "{}";
                        break;
                    }
                }
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool IstObjekt(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                return doc.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch (JsonException) { return false; }
        }

        /// <summary>Der Text ohne den JSON-Block - das, was das Modell zusaetzlich sagte.</summary>
        private static string Ohne(string? text, string json)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int i = text!.IndexOf(json, StringComparison.Ordinal);
            if (i < 0) return text.Trim();

            string rest = text.Substring(0, i) + text.Substring(i + json.Length);
            return rest.Replace("```json", "").Replace("```", "").Trim();
        }

        private static string Anhaengen(string? text, string zusatz)
            => string.IsNullOrWhiteSpace(text) ? zusatz : text!.TrimEnd() + "\n" + zusatz;
    }
}
