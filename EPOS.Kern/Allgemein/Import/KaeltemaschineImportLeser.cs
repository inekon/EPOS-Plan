using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Leser der Copper-Kurvendatei</b> (PNNL Copper <c>chiller_curves.json</c>, BSD-2; KM1). Die Datei
    /// ist ein Objekt mit laufenden Nummern als Schlüssel; jeder Satz führt <c>ref_cap</c> (+ <c>ref_cap_unit</c>),
    /// <c>full_eff</c> (+ <c>full_eff_unit</c>), <c>condenser_type</c>, <c>compressor_type</c>,
    /// <c>compressor_speed</c>, <c>min_plr</c>, <c>min_unloading</c>, <c>model</c> und unter
    /// <c>set_of_curves</c> die Kurven <c>cap-f-t</c> und <c>eir-f-t</c> (bi-quadratisch, Gültigkeitsgrenzen
    /// <c>x_min</c> … <c>y_max</c>, Einheiten <c>si</c>/<c>ip</c>).
    ///
    /// <para>Gelesen werden nur EIR-Sätze mit Kondensator-EINTRITT (<c>model = ect_lwt</c>) — die
    /// „Reformulated“-Sätze mit Kondensator-Austritt (<c>lct_lwt</c>) bräuchten eine Iteration und werden mit
    /// Grund übergangen, ebenso Sätze ohne Referenzleistung oder ohne bi-quadratische Kurven.</para>
    /// </summary>
    public static class CopperKaltwassersatzLeser
    {
        /// <summary>kW je US-Kältetonne.</summary>
        public const double KW_JE_TON = 3.51685;

        /// <summary>Das Ergebnis: die lesbaren Sätze und je übergangenem Satz „Nr: Grund“.</summary>
        public sealed class Ergebnis
        {
            /// <summary>Die lesbaren Kurvensätze in der Folge der Datei.</summary>
            public List<KaeltemaschinenKurvensatz> Saetze { get; } = new List<KaeltemaschinenKurvensatz>();

            /// <summary>Übergangene Sätze mit Grund.</summary>
            public List<string> Uebergangen { get; } = new List<string>();
        }

        /// <summary>Sieht der Text wie eine Copper-Kurvendatei aus (JSON-Objekt)?</summary>
        public static bool IstCopper(string text)
        {
            string t = (text ?? "").TrimStart('﻿', ' ', '\t', '\r', '\n');
            return t.StartsWith("{", StringComparison.Ordinal);
        }

        /// <summary>Liest den Text einer Copper-Kurvendatei.</summary>
        public static Ergebnis Lesen(string json)
        {
            var e = new Ergebnis();
            using (JsonDocument doc = JsonDocument.Parse(json ?? "", new JsonDocumentOptions { AllowTrailingCommas = true }))
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    throw new FormatException("Copper: oberste Ebene ist kein Objekt.");
                foreach (JsonProperty p in doc.RootElement.EnumerateObject())
                {
                    KaeltemaschinenKurvensatz satz = SatzLesen(p.Name, p.Value, out string grund);
                    if (satz != null) e.Saetze.Add(satz);
                    else e.Uebergangen.Add(p.Name + ": " + grund);
                }
            }
            return e;
        }

        /// <summary>Liest einen Satz; <c>null</c> mit <paramref name="grund"/>, wenn er nicht lesbar ist.</summary>
        public static KaeltemaschinenKurvensatz SatzLesen(string nr, JsonElement s, out string grund)
        {
            grund = "";
            if (s.ValueKind != JsonValueKind.Object) { grund = "kein Objekt"; return null; }
            string modell = Text(s, "model");
            if (!string.IsNullOrEmpty(modell) && !string.Equals(modell, "ect_lwt", StringComparison.OrdinalIgnoreCase))
            {
                grund = "Modell " + modell + " (nur ect_lwt)";
                return null;
            }
            double? refCap = Zahl(s, "ref_cap");
            if (!(refCap > 0)) { grund = "ohne Referenzleistung"; return null; }
            double kw = (Text(s, "ref_cap_unit") ?? "").ToLowerInvariant() switch
            {
                "ton" or "tons" => refCap.Value * KW_JE_TON,
                "w" => refCap.Value / 1000.0,
                "btu/h" => refCap.Value * 0.00029307107,
                _ => refCap.Value
            };
            double? eff = Zahl(s, "full_eff");
            if (!(eff > 0)) { grund = "ohne Volllasteffizienz"; return null; }
            double cop = (Text(s, "full_eff_unit") ?? "cop").ToLowerInvariant() switch
            {
                "eer" => eff.Value / 3.412,
                "kw/ton" => KW_JE_TON / eff.Value,
                _ => eff.Value
            };
            if (!s.TryGetProperty("set_of_curves", out JsonElement kurven) || kurven.ValueKind != JsonValueKind.Object)
            {
                grund = "ohne Kurven";
                return null;
            }
            KaeltemaschinenKurve cap = Kurve(kurven, "cap-f-t");
            KaeltemaschinenKurve eir = Kurve(kurven, "eir-f-t");
            if (cap == null || eir == null) { grund = "ohne bi-quadratische cap-f-t/eir-f-t"; return null; }
            return new KaeltemaschinenKurvensatz
            {
                Nr = nr ?? "",
                Kondensator = string.Equals(Text(s, "condenser_type"), "air", StringComparison.OrdinalIgnoreCase) ? "air" : "water",
                Verdichter = Text(s, "compressor_type") ?? "",
                Drehzahl = Text(s, "compressor_speed") ?? "",
                ReferenzleistungKw = kw,
                ReferenzCop = cop,
                MinPlr = Zahl(s, "min_plr"),
                MinUnloading = Zahl(s, "min_unloading"),
                CapFT = cap,
                EirFT = eir
            };
        }

        private static KaeltemaschinenKurve Kurve(JsonElement kurven, string name)
        {
            if (!kurven.TryGetProperty(name, out JsonElement k) || k.ValueKind != JsonValueKind.Object) return null;
            if (!string.Equals(Text(k, "type"), "bi_quad", StringComparison.OrdinalIgnoreCase)) return null;
            var c = new KaeltemaschinenKurve
            {
                C1 = Zahl(k, "coeff1") ?? 0, C2 = Zahl(k, "coeff2") ?? 0, C3 = Zahl(k, "coeff3") ?? 0,
                C4 = Zahl(k, "coeff4") ?? 0, C5 = Zahl(k, "coeff5") ?? 0, C6 = Zahl(k, "coeff6") ?? 0,
                XMin = Zahl(k, "x_min") ?? double.NegativeInfinity, XMax = Zahl(k, "x_max") ?? double.PositiveInfinity,
                YMin = Zahl(k, "y_min") ?? double.NegativeInfinity, YMax = Zahl(k, "y_max") ?? double.PositiveInfinity,
                AusMin = Zahl(k, "out_min"), AusMax = Zahl(k, "out_max"),
                Fahrenheit = string.Equals(Text(k, "units"), "ip", StringComparison.OrdinalIgnoreCase)
            };
            return c;
        }

        private static string Text(JsonElement e, string name) =>
            e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static double? Zahl(JsonElement e, string name)
        {
            if (!e.TryGetProperty(name, out JsonElement v)) return null;
            if (v.ValueKind == JsonValueKind.Number) return v.GetDouble();
            if (v.ValueKind == JsonValueKind.String &&
                double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
            return null;
        }

        /// <summary>Der neutrale Bezeichner eines importierten Copper-Satzes, z. B. „Kurvensatz 17 Wasser Turbo 1780 kW“.</summary>
        public static string Bezeichner(KaeltemaschinenKurvensatz satz)
        {
            (double q, _) = KaeltemaschinenKennfeld.Nennpunkt(satz);
            return "Kurvensatz " + satz.Nr + " " + (satz.IstLuft ? "Luft" : "Wasser") + " " +
                   KaeltemaschinenKennfeld.Verdichtername(satz.Verdichter, satz.Drehzahl) + " " +
                   KaeltemaschinenKennfeld.Leistungstext(q) + " kW";
        }
    }

    /// <summary>
    /// <b>Der Leser der offenen CSV-Kennfeldvorlage</b> (KM1; Vorlage <c>Quellen/Kaeltemaschine_Kennfeldvorlage.csv</c>).
    ///
    /// <para>Aufbau: Kommentarzeilen beginnen mit <c>#</c>. Kopfzeilen „Schlüssel;Wert“ — Bezeichner, Firma, Typ,
    /// Beschreibung, Rückkühlart (LUFT/TROCKENKUEHLER/NASSKUEHLER/WASSER), Nennkälteleistung [kW], Nenn-EER,
    /// Mindestteillast [%], Kältemittel —, danach Datenzeilen
    /// <c>Rueckkuehltemperatur;Kaltwassertemperatur;Kaelteleistung_kW;EER</c>; eine Spaltenkopfzeile mit diesen
    /// Namen wird übergangen. Jede weitere Zeile „Bezeichner“ beginnt ein neues Gerät.</para>
    ///
    /// <para>Trennzeichen <c>;</c>, Tabulator oder <c>,</c> werden erkannt (in dieser Rangfolge); das
    /// Dezimalzeichen ist bei <c>;</c> und Tabulator Punkt oder Komma, bei <c>,</c> der Punkt. Fehlen
    /// Nennkälteleistung oder Nenn-EER, kommen sie aus dem Kennfeld am Eurovent-Nennpunkt.</para>
    /// </summary>
    public static class KaeltemaschineCsvLeser
    {
        /// <summary>Das Ergebnis: die Geräte und die Meldungen (Zeile und Grund).</summary>
        public sealed class Ergebnis
        {
            /// <summary>Die gelesenen Geräte.</summary>
            public List<KaeltemaschineModel> Geraete { get; } = new List<KaeltemaschineModel>();

            /// <summary>Übergangene Zeilen: „Zeile n: Grund“.</summary>
            public List<string> Uebergangen { get; } = new List<string>();

            /// <summary>Das erkannte Trennzeichen.</summary>
            public char Trennzeichen { get; set; }
        }

        /// <summary>Erkennt das Trennzeichen über die Nicht-Kommentarzeilen.</summary>
        public static char TrennzeichenErkennen(IEnumerable<string> zeilen)
        {
            var nutz = zeilen.Where(z => !string.IsNullOrWhiteSpace(z) && !z.TrimStart().StartsWith("#", StringComparison.Ordinal)).ToList();
            if (nutz.Any(z => z.IndexOf(';') >= 0)) return ';';
            if (nutz.Any(z => z.IndexOf('\t') >= 0)) return '\t';
            return ',';
        }

        /// <summary>Liest den Text der Vorlage.</summary>
        public static Ergebnis Lesen(string text)
        {
            var e = new Ergebnis();
            string[] zeilen = (text ?? "").TrimStart('﻿').Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            char trenn = TrennzeichenErkennen(zeilen);
            e.Trennzeichen = trenn;
            KaeltemaschineModel aktuell = null;

            for (int i = 0; i < zeilen.Length; i++)
            {
                string z = zeilen[i].Trim();
                if (z.Length == 0 || z.StartsWith("#", StringComparison.Ordinal)) continue;
                string[] f = z.Split(trenn).Select(s => s.Trim().Trim('"').Trim()).ToArray();
                while (f.Length > 0 && f[f.Length - 1].Length == 0) f = f.Take(f.Length - 1).ToArray();
                if (f.Length == 0) continue;

                // Eine Datenzeile: vier Zahlen.
                if (f.Length >= 4 && Zahl(f[0], trenn).HasValue)
                {
                    double? rk = Zahl(f[0], trenn), kw = Zahl(f[1], trenn), q = Zahl(f[2], trenn), eer = Zahl(f[3], trenn);
                    if (aktuell == null) { e.Uebergangen.Add(Zeile(i, "Kennfeldpunkt ohne Bezeichner")); continue; }
                    if (!rk.HasValue || !kw.HasValue || !q.HasValue || !eer.HasValue)
                    {
                        e.Uebergangen.Add(Zeile(i, "Kennfeldpunkt unvollständig"));
                        continue;
                    }
                    aktuell.Kennlinie.Add(new KaeltemaschineKenndatenModel
                    {
                        Rueckkuehltemperatur = rk.Value, Kaltwassertemperatur = kw.Value, Kaelteleistung_kW = q.Value, EER = eer.Value
                    });
                    continue;
                }

                string schluessel = Schluessel(f[0]);
                string wert = f.Length > 1 ? string.Join(trenn.ToString(), f.Skip(1)).Trim() : "";
                switch (schluessel)
                {
                    case "BEZEICHNER":
                        if (aktuell != null) Abschliessen(e, aktuell);
                        aktuell = new KaeltemaschineModel { Bezeichner = wert };
                        break;
                    case "RUECKKUEHLTEMPERATUR":
                        break; // die Spaltenkopfzeile
                    default:
                        if (aktuell == null) { e.Uebergangen.Add(Zeile(i, "Kopfzeile vor dem Bezeichner")); break; }
                        if (!KopfSetzen(aktuell, schluessel, wert, trenn))
                            e.Uebergangen.Add(Zeile(i, "unbekannte oder ungültige Angabe „" + f[0] + "“"));
                        break;
                }
            }
            if (aktuell != null) Abschliessen(e, aktuell);
            return e;
        }

        private static void Abschliessen(Ergebnis e, KaeltemaschineModel m)
        {
            if (string.IsNullOrWhiteSpace(m.Bezeichner)) { e.Uebergangen.Add("Gerät ohne Bezeichner"); return; }
            if (string.IsNullOrEmpty(m.Rueckkuehlart)) m.Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_LUFT;
            KaeltemaschinenKennfeld.NennwerteErgaenzen(m);
            e.Geraete.Add(m);
        }

        private static bool KopfSetzen(KaeltemaschineModel m, string schluessel, string wert, char trenn)
        {
            switch (schluessel)
            {
                case "FIRMA": case "HERSTELLER": m.Firma = Leer(wert); return true;
                case "TYP": m.Typ = Leer(wert); return true;
                case "BESCHREIBUNG": m.Beschreibung = Leer(wert); return true;
                case "KAELTEMITTEL": m.Kaeltemittel = Leer(wert); return true;
                case "RUECKKUEHLART":
                    string art = Rueckkuehlart(wert);
                    if (art == null) return false;
                    m.Rueckkuehlart = art;
                    return true;
                case "NENNKAELTELEISTUNG": case "NENNKAELTELEISTUNG_KW":
                    m.Nennkaelteleistung_kW = Zahl(wert, trenn);
                    return m.Nennkaelteleistung_kW.HasValue || wert.Length == 0;
                case "NENN_EER": case "NENNEER":
                    m.Nenn_EER = Zahl(wert, trenn);
                    return m.Nenn_EER.HasValue || wert.Length == 0;
                case "MINDESTTEILLAST": case "MINDESTTEILLAST_PROZENT":
                    m.Mindestteillast_Prozent = Zahl(wert, trenn);
                    return m.Mindestteillast_Prozent.HasValue || wert.Length == 0;
                default:
                    return false;
            }
        }

        /// <summary>Der Persistenzwert einer Rückkühlart aus Schlüssel oder deutscher Bezeichnung; <c>null</c> = unbekannt.</summary>
        public static string Rueckkuehlart(string wert)
        {
            string s = Schluessel(wert);
            switch (s)
            {
                case "LUFT": case "LUFTGEKUEHLT": return KaeltemaschineSchema.RUECKKUEHLART_LUFT;
                case "TROCKENKUEHLER": return KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER;
                case "NASSKUEHLER": case "KUEHLTURM": case "VERDUNSTUNGSKUEHLER": return KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER;
                case "WASSER": case "WASSERGEKUEHLT": return KaeltemaschineSchema.RUECKKUEHLART_WASSER;
                default: return null;
            }
        }

        /// <summary>Normiert einen Kopfschlüssel: Umlaute ausgeschrieben, groß, Leer-/Bindestriche als „_“, Einheit in Klammern weg.</summary>
        internal static string Schluessel(string text)
        {
            string s = (text ?? "").Trim();
            int k = s.IndexOfAny(new[] { '[', '(' });
            if (k > 0) s = s.Substring(0, k).Trim();
            s = s.TrimEnd(':').Trim()
                 .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue")
                 .Replace("Ä", "Ae").Replace("Ö", "Oe").Replace("Ü", "Ue").Replace("ß", "ss")
                 .ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
            return s;
        }

        /// <summary>Eine Zahl mit Punkt oder (bei Trennzeichen ≠ Komma) Komma als Dezimalzeichen.</summary>
        internal static double? Zahl(string text, char trenn)
        {
            string s = (text ?? "").Trim().Replace(" ", "");
            if (s.Length == 0) return null;
            if (trenn != ',') s = s.Replace(',', '.');
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && !double.IsNaN(d) && !double.IsInfinity(d)
                ? d : (double?)null;
        }

        private static string Leer(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static string Zeile(int i, string grund) => "Zeile " + (i + 1).ToString(CultureInfo.InvariantCulture) + ": " + grund;
    }

    /// <summary>
    /// <b>Die Datei eines Kältemaschinenimports</b> (KM1): erkennt Copper-JSON oder CSV-Vorlage am Inhalt und
    /// liefert die Katalogsätze samt Meldungen.
    /// </summary>
    public static class KaeltemaschineImportDatei
    {
        /// <summary>Das Ergebnis: die Katalogsätze (mit Quelle und Nummer) und die übergangenen Einträge.</summary>
        public sealed class Ergebnis
        {
            /// <summary>Die Sätze.</summary>
            public List<(KaeltemaschineModel Modell, string Quelle)> Saetze { get; } = new List<(KaeltemaschineModel, string)>();

            /// <summary>Übergangen mit Grund.</summary>
            public List<string> Uebergangen { get; } = new List<string>();

            /// <summary><c>true</c> = Copper-JSON, sonst CSV-Vorlage.</summary>
            public bool Copper { get; set; }
        }

        /// <summary>Quellkennung eines Copper-Satzes.</summary>
        public const string QUELLE_COPPER = "PNNL Copper";

        /// <summary>Quellkennung der CSV-Vorlage.</summary>
        public const string QUELLE_CSV = "CSV";

        /// <summary>Liest eine Datei vom Datenträger.</summary>
        public static Ergebnis Lesen(string pfad) => AusText(File.ReadAllText(pfad, Encoding.UTF8));

        /// <summary>Liest den Inhalt einer Datei.</summary>
        public static Ergebnis AusText(string text)
        {
            var e = new Ergebnis();
            if (CopperKaltwassersatzLeser.IstCopper(text))
            {
                e.Copper = true;
                CopperKaltwassersatzLeser.Ergebnis c = CopperKaltwassersatzLeser.Lesen(text);
                foreach (KaeltemaschinenKurvensatz s in c.Saetze)
                {
                    KaeltemaschineModel m = KaeltemaschinenKennfeld.Modell(
                        s, CopperKaltwassersatzLeser.Bezeichner(s), typ: KaeltemaschinenKennfeld.TYP,
                        beschreibung: "Kurvensatz aus offenen US-Kurvendaten (PNNL Copper, BSD-2), Datensatz " + s.Nr +
                                      "; Nennpunkt Eurovent");
                    e.Saetze.Add((m, QUELLE_COPPER));
                }
                e.Uebergangen.AddRange(c.Uebergangen);
                return e;
            }
            KaeltemaschineCsvLeser.Ergebnis v = KaeltemaschineCsvLeser.Lesen(text);
            foreach (KaeltemaschineModel m in v.Geraete) e.Saetze.Add((m, QUELLE_CSV));
            e.Uebergangen.AddRange(v.Uebergangen);
            return e;
        }
    }
}
