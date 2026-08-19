using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace KiKern
{
    /// <summary>Befund der Parameterpruefung.</summary>
    public sealed class KiPruefErgebnis
    {
        internal KiPruefErgebnis(KiAufruf? aufruf, IReadOnlyList<string> fehler)
        {
            Aufruf = aufruf;
            Fehler = fehler;
        }

        /// <summary>Der gepruefte Aufruf; <c>null</c>, wenn die Pruefung fehlschlug.</summary>
        public KiAufruf? Aufruf { get; }

        /// <summary>Alle Beanstandungen in Klartext; leer, wenn alles stimmt.</summary>
        public IReadOnlyList<string> Fehler { get; }

        /// <summary>true, wenn ein ausfuehrbarer Aufruf entstanden ist.</summary>
        public bool Gueltig => Aufruf != null && Fehler.Count == 0;

        /// <summary>Alle Beanstandungen in einem Absatz - fuer Chat und Modellrueckfrage.</summary>
        public string FehlerText() => string.Join(" ", Fehler);
    }

    /// <summary>
    /// Prueft die Parameter eines Aufrufs gegen die Aktionsdeklaration
    /// (Fachkonzept 3.2, Verwendung b).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Geprueft wird gegen DIESELBE Deklaration, aus der auch das Schema entsteht - es
    /// gibt keine zweite Regelquelle. Die Pruefung ist STRENG: unbekannte Parameter werden
    /// abgewiesen statt stillschweigend verworfen, damit ein erfundener Parametername des
    /// Modells eine Korrekturrunde ausloest und nicht unbemerkt bleibt.
    /// </para>
    /// <para>
    /// <b>Kulturregel.</b> Werte kommen invariant herein (JSON-Zahlen, invariante
    /// Zeichenketten). Es wird NIE in der Anwenderkultur geparst - sonst wuerde „1.5"
    /// unter de-DE zu 15.
    /// </para>
    /// </remarks>
    public static class KiPruefung
    {
        /// <summary>Prueft einen Aufruf gegen das Register.</summary>
        public static KiPruefErgebnis Pruefe(KiRegister register, string? aktionsname,
                                             IReadOnlyDictionary<string, object?>? rohwerte)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));

            KiAktion? aktion = register.Finde(aktionsname);
            if (aktion == null)
            {
                return new KiPruefErgebnis(null, new[]
                {
                    string.Format(CultureInfo.InvariantCulture, KiTexte.AktionUnbekannt,
                                  aktionsname ?? "", string.Join(", ", register.Namen()))
                });
            }
            return Pruefe(aktion, rohwerte);
        }

        /// <summary>Prueft einen Aufruf gegen eine einzelne Aktion.</summary>
        public static KiPruefErgebnis Pruefe(KiAktion aktion, IReadOnlyDictionary<string, object?>? rohwerte)
        {
            if (aktion == null) throw new ArgumentNullException(nameof(aktion));

            var fehler = new List<string>();
            var werte = new Dictionary<string, object>(StringComparer.Ordinal);

            // 1. Unbekannte Parameter melden - nicht stillschweigend verwerfen.
            if (rohwerte != null)
            {
                foreach (string name in rohwerte.Keys)
                {
                    if (aktion.Finde(name) != null) continue;
                    fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.ParameterUnbekannt,
                                             name, ErlaubteNamen(aktion)));
                }
            }

            // 2. Jeden deklarierten Parameter holen, wandeln, pruefen.
            foreach (KiParameter p in aktion.Parameter)
            {
                object? roh = null;
                bool vorhanden = rohwerte != null && rohwerte.TryGetValue(p.Name, out roh) && !IstLeer(roh);

                if (!vorhanden)
                {
                    if (p.Pflicht)
                        fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.PflichtfeldFehlt,
                                                 p.Anzeigename, p.Name));
                    continue;
                }

                object? wert = Wandle(p, roh, fehler);
                if (wert != null) werte[p.Name] = wert;
            }

            if (fehler.Count > 0) return new KiPruefErgebnis(null, fehler);
            return new KiPruefErgebnis(new KiAufruf(aktion, werte), Array.Empty<string>());
        }

        /// <summary>
        /// Prueft einen Aufruf, dessen Parameter als JSON-Text vorliegen - der Weg, auf dem
        /// eine Modellantwort hereinkommt (Fachkonzept 3.3, Wege A und B).
        /// </summary>
        public static KiPruefErgebnis PruefeJson(KiRegister register, string? aktionsname, string? json)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));

            KiAktion? aktion = register.Finde(aktionsname);
            if (aktion == null)
            {
                return new KiPruefErgebnis(null, new[]
                {
                    string.Format(CultureInfo.InvariantCulture, KiTexte.AktionUnbekannt,
                                  aktionsname ?? "", string.Join(", ", register.Namen()))
                });
            }

            if (string.IsNullOrWhiteSpace(json)) return Pruefe(aktion, null);

            Dictionary<string, object?> roh;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json!);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return new KiPruefErgebnis(null, new[] { KiTexte.KeinObjekt });

                roh = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (JsonProperty e in doc.RootElement.EnumerateObject())
                    roh[e.Name] = Entpacke(e.Value);
            }
            catch (JsonException)
            {
                return new KiPruefErgebnis(null, new[] { KiTexte.KeinObjekt });
            }

            return Pruefe(aktion, roh);
        }

        // ======================================================================= Wandeln

        private static object? Wandle(KiParameter p, object? rohEingang, List<string> fehler)
        {
            // Ein JsonElement wird EINMAL hier ausgepackt - danach arbeiten alle
            // Wandler auf einfachen CLR-Werten und muessen den Sonderfall nicht kennen.
            object? roh = rohEingang is JsonElement je ? Entpacke(je) : rohEingang;

            switch (p.Typ)
            {
                case KiParameterTyp.Ganzzahl: return WandleGanzzahl(p, roh, fehler);
                case KiParameterTyp.Zahl: return WandleZahl(p, roh, fehler);
                case KiParameterTyp.Text: return WandleText(p, roh, fehler);
                case KiParameterTyp.Wahrheitswert: return WandleWahrheit(p, roh, fehler);
                case KiParameterTyp.Aufzaehlung: return WandleAufzaehlung(p, roh, fehler);
                case KiParameterTyp.GanzzahlListe: return WandleListe(p, roh, fehler);
                default: throw new ArgumentOutOfRangeException(nameof(p));
            }
        }

        private static object? WandleGanzzahl(KiParameter p, object? roh, List<string> fehler)
        {
            if (!VersucheGanzzahl(roh, out long wert))
            {
                fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.KeineGanzzahl,
                                         p.Anzeigename, Rohtext(roh)));
                return null;
            }
            return BereichOk(p, wert, fehler) ? (object)wert : null;
        }

        private static object? WandleZahl(KiParameter p, object? roh, List<string> fehler)
        {
            if (!VersucheZahl(roh, out double wert))
            {
                fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.KeineZahl,
                                         p.Anzeigename, Rohtext(roh)));
                return null;
            }
            return BereichOk(p, wert, fehler) ? (object)wert : null;
        }

        private static object? WandleText(KiParameter p, object? roh, List<string> fehler)
        {
            if (!(roh is string s))
            {
                fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.KeinText,
                                         p.Anzeigename, Rohtext(roh)));
                return null;
            }

            s = s.Trim();
            if (s.Length == 0)
            {
                fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.TextLeer, p.Anzeigename));
                return null;
            }
            if (p.MaxLaenge.HasValue && s.Length > p.MaxLaenge.Value)
            {
                fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.TextZuLang,
                                         p.Anzeigename, p.MaxLaenge.Value));
                return null;
            }
            return s;
        }

        private static object? WandleWahrheit(KiParameter p, object? roh, List<string> fehler)
        {
            switch (roh)
            {
                case bool b: return b;
                case string s when bool.TryParse(s.Trim(), out bool b2): return b2;
                default:
                    fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.KeinWahrheitswert,
                                             p.Anzeigename, Rohtext(roh)));
                    return null;
            }
        }

        private static object? WandleAufzaehlung(KiParameter p, object? roh, List<string> fehler)
        {
            if (!(roh is string s))
            {
                fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.KeinText,
                                         p.Anzeigename, Rohtext(roh)));
                return null;
            }

            s = s.Trim();
            foreach (string erlaubt in p.Werte)
                if (string.Equals(erlaubt, s, StringComparison.Ordinal)) return erlaubt;

            // Zweiter Anlauf ohne Gross-/Kleinschreibung: der GESPEICHERTE Wert bleibt der
            // aus DbWerte, aber „bhkw" statt „BHKW" soll keine Korrekturrunde kosten.
            foreach (string erlaubt in p.Werte)
                if (string.Equals(erlaubt, s, StringComparison.OrdinalIgnoreCase)) return erlaubt;

            fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.WertNichtErlaubt,
                                     p.Anzeigename, s, string.Join(", ", p.Werte)));
            return null;
        }

        private static object? WandleListe(KiParameter p, object? roh, List<string> fehler)
        {
            var werte = new List<long>();
            IEnumerable? liste = (roh is IEnumerable folge && !(roh is string)) ? folge : null;

            if (liste == null)
            {
                // Ein einzelner Skalar wird als einelementige Liste angenommen - ein
                // haeufiger und harmloser Fehlgriff des Modells.
                if (VersucheGanzzahl(roh, out long einzeln)) werte.Add(einzeln);
                else
                {
                    fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.KeineListe,
                                             p.Anzeigename, Rohtext(roh)));
                    return null;
                }
            }
            else
            {
                foreach (object? glied in liste)
                {
                    if (!VersucheGanzzahl(glied, out long wert))
                    {
                        fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.KeineListe,
                                                 p.Anzeigename, Rohtext(roh)));
                        return null;
                    }
                    werte.Add(wert);
                }
            }

            if (werte.Count == 0)
            {
                fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.ListeLeer, p.Anzeigename));
                return null;
            }

            foreach (long w in werte)
                if (!BereichOk(p, w, fehler)) return null;

            return werte.ToArray();
        }

        // ======================================================================= Hilfen

        private static bool BereichOk(KiParameter p, double wert, List<string> fehler)
        {
            bool unten = p.Min.HasValue && wert < p.Min.Value;
            bool oben = p.Max.HasValue && wert > p.Max.Value;
            if (!unten && !oben) return true;

            string w = Zahltext(wert);
            if (p.Min.HasValue && p.Max.HasValue)
                fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.AusserhalbBereich,
                                         p.Anzeigename, w, Zahltext(p.Min.Value), Zahltext(p.Max.Value)));
            else if (unten)
                fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.UnterGrenze,
                                         p.Anzeigename, w, Zahltext(p.Min!.Value)));
            else
                fehler.Add(string.Format(CultureInfo.InvariantCulture, KiTexte.UeberGrenze,
                                         p.Anzeigename, w, Zahltext(p.Max!.Value)));
            return false;
        }

        private static bool IstLeer(object? roh)
        {
            if (roh == null) return true;
            if (roh is JsonElement je && je.ValueKind == JsonValueKind.Null) return true;
            return false;
        }

        private static bool VersucheGanzzahl(object? roh, out long wert)
        {
            wert = 0;
            object? o = roh is JsonElement je ? Entpacke(je) : roh;

            switch (o)
            {
                case long l: wert = l; return true;
                case int i: wert = i; return true;
                case short s: wert = s; return true;
                case byte b: wert = b; return true;
                case double d:
                    if (double.IsNaN(d) || double.IsInfinity(d)) return false;
                    if (Math.Abs(d - Math.Round(d)) > 1e-9) return false;   // 3.5 ist keine ID
                    if (d > long.MaxValue || d < long.MinValue) return false;
                    wert = (long)Math.Round(d);
                    return true;
                case decimal m:
                    if (m != Math.Floor(m)) return false;
                    wert = (long)m;
                    return true;
                case string t:
                    return long.TryParse(t.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out wert);
                default:
                    return false;
            }
        }

        private static bool VersucheZahl(object? roh, out double wert)
        {
            wert = 0.0;
            object? o = roh is JsonElement je ? Entpacke(je) : roh;

            switch (o)
            {
                case double d: wert = d; return !double.IsNaN(d) && !double.IsInfinity(d);
                case float f: wert = f; return true;
                case long l: wert = l; return true;
                case int i: wert = i; return true;
                case decimal m: wert = (double)m; return true;
                case string t:
                    return double.TryParse(t.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out wert);
                default:
                    return false;
            }
        }

        /// <summary>Wandelt ein <see cref="JsonElement"/> in einen einfachen CLR-Wert.</summary>
        private static object? Entpacke(JsonElement e)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.String: return e.GetString();
                case JsonValueKind.Number:
                    if (e.TryGetInt64(out long l)) return l;
                    return e.GetDouble();
                case JsonValueKind.Array:
                    {
                        var liste = new List<object?>();
                        foreach (JsonElement k in e.EnumerateArray()) liste.Add(Entpacke(k));
                        return liste;
                    }
                default:
                    return e.GetRawText();
            }
        }

        private static string ErlaubteNamen(KiAktion aktion)
        {
            if (aktion.Parameter.Count == 0) return KiTexte.KeineAngaben;
            var namen = new List<string>(aktion.Parameter.Count);
            foreach (KiParameter p in aktion.Parameter) namen.Add(p.Name);
            return string.Join(", ", namen);
        }

        private static string Rohtext(object? roh)
        {
            object? o = roh is JsonElement je ? Entpacke(je) : roh;
            if (o == null) return "";
            if (o is IEnumerable liste && !(o is string))
            {
                var teile = new List<string>();
                foreach (object? e in liste) teile.Add(Convert.ToString(e, CultureInfo.InvariantCulture) ?? "");
                return "[" + string.Join(", ", teile) + "]";
            }
            return Convert.ToString(o, CultureInfo.InvariantCulture) ?? "";
        }

        private static string Zahltext(double wert)
        {
            return wert == Math.Floor(wert) && Math.Abs(wert) < 1e15
                ? ((long)wert).ToString(CultureInfo.InvariantCulture)
                : wert.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}
