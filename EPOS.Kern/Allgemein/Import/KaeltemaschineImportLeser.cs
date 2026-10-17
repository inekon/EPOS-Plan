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
            TeillastkurveLesen(kurven, out string plrForm, out double[] plrBeiwerte, out double? plrXMin);
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
                EirFT = eir,
                TeillastForm = plrForm,
                TeillastBeiwerte = plrBeiwerte,
                TeillastXMin = plrXMin
            };
        }

        /// <summary>
        /// Liest die Teillastkurve <c>eir-f-plr</c> (KM3): <c>quad</c> mit <c>coeff1</c> bis <c>coeff3</c> (ein Wert in
        /// <c>coeff4</c> gehört nicht zur quadratischen Form und wird nicht gelesen), <c>cubic</c> mit <c>coeff1</c> bis
        /// <c>coeff4</c>; jede andere Form oder ein fehlender Beiwert = keine Kurve.
        /// </summary>
        private static void TeillastkurveLesen(JsonElement kurven, out string form, out double[] beiwerte, out double? xMin)
        {
            form = null;
            beiwerte = null;
            xMin = null;
            if (!kurven.TryGetProperty("eir-f-plr", out JsonElement k) || k.ValueKind != JsonValueKind.Object) return;
            string typ = (Text(k, "type") ?? "").ToLowerInvariant();
            int n = typ == "quad" ? 3 : typ == "cubic" ? 4 : 0;
            if (n == 0) return;
            var w = new double[n];
            for (int i = 0; i < n; i++)
            {
                double? c = Zahl(k, "coeff" + (i + 1).ToString(CultureInfo.InvariantCulture));
                if (!c.HasValue || double.IsNaN(c.Value) || double.IsInfinity(c.Value)) return;
                w[i] = c.Value;
            }
            form = typ;
            beiwerte = w;
            xMin = Zahl(k, "x_min");
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

            /// <summary>Hinweise zu gelesenen Geräten (Teillastkurve, Typkennfeld), „Bezeichner: Grund“.</summary>
            public List<string> Hinweise { get; } = new List<string>();

            /// <summary>Die Form je Gerät (<see cref="KaeltemaschineImportVarianten.FORMEN"/>), in der Folge von <see cref="Geraete"/>.</summary>
            public List<string> Formen { get; } = new List<string>();
        }

        /// <summary>Was ein Gerät außer seinem Modell in der Datei angibt (K-C): Form, Pdesignc, Verdichter, Kaltwasser, Punkte.</summary>
        private sealed class Geraetangaben
        {
            public string Form;
            public double? PdesignKw;
            public string Verdichter;
            public double? KaltwasserC;
            public readonly List<OekodesignPunkteLeser.Punkt> Punkte = new List<OekodesignPunkteLeser.Punkt>();
            public readonly List<(double Lastgrad, double Verhaeltnis)> Teillast = new List<(double, double)>();
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
            var angaben = new Geraetangaben();

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
                        if (aktuell != null) Abschliessen(e, aktuell, angaben);
                        aktuell = new KaeltemaschineModel { Bezeichner = wert };
                        angaben = new Geraetangaben();
                        break;
                    case "RUECKKUEHLTEMPERATUR":
                        break; // die Spaltenkopfzeile
                    case "TEILLAST":
                        // Teillastzeile „Teillast;Lastgrad;EER-Verhaeltnis“ (KM3, Fachkonzept 3.2 b).
                        double? x = f.Length > 1 ? Zahl(f[1], trenn) : null, g = f.Length > 2 ? Zahl(f[2], trenn) : null;
                        if (aktuell == null) e.Uebergangen.Add(Zeile(i, "Teillastzeile ohne Bezeichner"));
                        else if (!x.HasValue || !g.HasValue || !(x.Value > 0) || x.Value > 1 || !(g.Value > 0))
                            e.Uebergangen.Add(Zeile(i, "Teillastzeile ungültig (Lastgrad 0 bis 1, EER-Verhältnis > 0)"));
                        else angaben.Teillast.Add((x.Value, g.Value));
                        break;
                    case OekodesignPunkteLeser.SCHLUESSEL:
                        // Teillastpunkt A–D eines Ökodesign-Datenblatts (K-C).
                        OekodesignPunkteLeser.Punkt punkt = OekodesignPunkteLeser.ZeileLesen(f, trenn, i + 1, out string pf);
                        if (pf != null) e.Uebergangen.Add(Zeile(i, pf));
                        else if (punkt == null) break; // Spaltenkopfzeile
                        else if (aktuell == null) e.Uebergangen.Add(Zeile(i, "Teillastpunkt ohne Bezeichner"));
                        else angaben.Punkte.Add(punkt);
                        break;
                    case "FORM":
                        if (aktuell == null) { e.Uebergangen.Add(Zeile(i, "Kopfzeile vor dem Bezeichner")); break; }
                        angaben.Form = KaeltemaschineImportVarianten.Form(wert);
                        if (angaben.Form == null)
                            e.Uebergangen.Add(Zeile(i, "unbekannte Form „" + wert + "“ (KENNFELD, NENNWERTE oder OEKODESIGN)"));
                        break;
                    case "PDESIGNC": case "PDESIGN":
                        if (aktuell == null) { e.Uebergangen.Add(Zeile(i, "Kopfzeile vor dem Bezeichner")); break; }
                        angaben.PdesignKw = Zahl(wert, trenn);
                        if (!(angaben.PdesignKw > 0) && wert.Length > 0)
                        {
                            angaben.PdesignKw = null;
                            e.Uebergangen.Add(Zeile(i, "Pdesignc keine Zahl größer 0"));
                        }
                        break;
                    case "VERDICHTER":
                        if (aktuell == null) { e.Uebergangen.Add(Zeile(i, "Kopfzeile vor dem Bezeichner")); break; }
                        string vd = Schluessel(wert);
                        angaben.Verdichter = vd.Length == 0 ? null : vd;
                        if (vd.Length > 0 && !KaeltemaschineImportVarianten.VERDICHTER.ContainsKey(vd))
                        {
                            angaben.Verdichter = null;
                            e.Uebergangen.Add(Zeile(i, "unbekannter Verdichter „" + wert + "“ (SCROLL, SCHRAUBE, TURBO oder HUBKOLBEN)"));
                        }
                        break;
                    case "KALTWASSERTEMPERATUR":
                        if (aktuell == null) { e.Uebergangen.Add(Zeile(i, "Kopfzeile vor dem Bezeichner")); break; }
                        angaben.KaltwasserC = Zahl(wert, trenn);
                        if (!angaben.KaltwasserC.HasValue && wert.Length > 0)
                            e.Uebergangen.Add(Zeile(i, "Kaltwassertemperatur keine Zahl"));
                        break;
                    default:
                        if (aktuell == null) { e.Uebergangen.Add(Zeile(i, "Kopfzeile vor dem Bezeichner")); break; }
                        if (!KopfSetzen(aktuell, schluessel, wert, trenn))
                            e.Uebergangen.Add(Zeile(i, "unbekannte oder ungültige Angabe „" + f[0] + "“"));
                        break;
                }
            }
            if (aktuell != null) Abschliessen(e, aktuell, angaben);
            return e;
        }

        private static void Abschliessen(Ergebnis e, KaeltemaschineModel m, Geraetangaben a)
        {
            if (string.IsNullOrWhiteSpace(m.Bezeichner)) { e.Uebergangen.Add("Gerät ohne Bezeichner"); return; }
            if (string.IsNullOrEmpty(m.Rueckkuehlart))
                m.Rueckkuehlart = m.Geraeteart == KaelteKatalogfelderSchema.GERAETEART_KWS_WASSER
                    ? KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER
                    : KaeltemaschineSchema.RUECKKUEHLART_LUFT;
            string form = KaeltemaschineImportVarianten.FormErkennen(a.Form, m.Kennlinie.Count > 0, a.Punkte.Count > 0);
            string grund = form switch
            {
                KaeltemaschineImportVarianten.FORM_NENNWERTE => Nennwerte(e, m, a),
                KaeltemaschineImportVarianten.FORM_OEKODESIGN => Oekodesign(e, m, a),
                _ => m.Kennlinie.Count == 0 ? "Form KENNFELD ohne Kennfeldzeilen" : null
            };
            if (grund != null) { e.Uebergangen.Add(m.Bezeichner + ": " + grund); return; }
            KaeltemaschinenKennfeld.NennwerteErgaenzen(m);
            // Punkte A–D neben einem Kennfeld: sie geben die Teillastkurve gegen dieses Kennfeld (Entwurf K-D 2.2).
            if (form == KaeltemaschineImportVarianten.FORM_KENNFELD && a.Punkte.Count > 0)
                PunkteAnwenden(e, m, a);
            TeillastAnpassen(e, m, a.Teillast);
            KatalogfelderAnpassen(e, m);
            e.Geraete.Add(m);
            e.Formen.Add(form);
        }

        /// <summary>
        /// Form „Nennwerte“ (K-C): Nennkälteleistung und Nenn-EER sind Pflicht; das Kennfeld kommt aus dem gewählten
        /// Typkennfeld, am Eurovent-Nennpunkt der Rückkühlart auf die Nennwerte skaliert; Teillast und Takten des Typs,
        /// soweit die Vorlage nichts angibt. <c>null</c> = gelesen, sonst der Grund.
        /// </summary>
        private static string Nennwerte(Ergebnis e, KaeltemaschineModel m, Geraetangaben a)
        {
            if (m.Kennlinie.Count > 0) return "Form NENNWERTE verträgt keine Kennfeldzeilen";
            if (a.Punkte.Count > 0) return "Form NENNWERTE verträgt keine Teillastpunkte (Form OEKODESIGN)";
            if (!(m.Nennkaelteleistung_kW > 0) || !(m.Nenn_EER > 0))
                return "Form NENNWERTE braucht Nennkälteleistung und Nenn-EER größer 0";
            string fehler = AusTypkennfeld(e, m, a, KaeltemaschinenKennfeld.Nennrueckkuehltemperatur(m.Rueckkuehlart),
                                           KaeltemaschinenKennfeld.NENN_KALTWASSER_C, m.Nennkaelteleistung_kW.Value, m.Nenn_EER.Value,
                                           "auf den Nennpunkt", out KaeltemaschineImportVarianten.Wahl wahl);
            if (fehler != null) return fehler;
            if (a.Teillast.Count == 0) KaeltemaschineImportVarianten.TeillastVomTyp(m, wahl);
            return null;
        }

        /// <summary>
        /// Form „Ökodesign-Datenblatt A–D“ (K-C): die Punkte A–D (Pflicht), die Kaltwassertemperatur der Prüfung (Pflicht),
        /// bei Wasserkühlung je Punkt die Rückkühltemperatur. Das Kennfeld kommt aus dem gewählten Typkennfeld, am
        /// Bezugspunkt (<see cref="OekodesignPunkteLeser.Bezugspunkt"/>) auf dessen Leistung und EER skaliert; die
        /// Teillastkurve aus den Punkten gegen dieses Kennfeld. <c>null</c> = gelesen, sonst der Grund.
        /// </summary>
        private static string Oekodesign(Ergebnis e, KaeltemaschineModel m, Geraetangaben a)
        {
            if (m.Kennlinie.Count > 0) return "Form OEKODESIGN verträgt keine Kennfeldzeilen";
            string grund = OekodesignPunkteLeser.Pruefen(a.Punkte, a.PdesignKw);
            if (grund != null) return grund;
            if (!a.KaltwasserC.HasValue) return "Form OEKODESIGN braucht die Kaltwassertemperatur der Prüfung";
            OekodesignPunkteLeser.Punkt fehlt = a.Punkte.FirstOrDefault(p => !KaeltemaschineImportVarianten.Rueckkuehltemperatur(p, m.Rueckkuehlart).HasValue);
            if (fehlt != null)
                return "Teillastpunkt " + fehlt.Name + " (Zeile " + fehlt.Zeile.ToString(CultureInfo.InvariantCulture) +
                       ") ohne Rückkühltemperatur — bei Wasserkühlung Pflicht";
            OekodesignPunkteLeser.Punkt bezug = OekodesignPunkteLeser.Bezugspunkt(a.Punkte);
            if (a.PdesignKw.HasValue && Math.Abs(bezug.LeistungKw - a.PdesignKw.Value) > OekodesignPunkteLeser.TOLERANZ_TAKT * a.PdesignKw.Value)
                e.Hinweise.Add(m.Bezeichner + ": Leistung des Punkts " + bezug.Name + " weicht von Pdesignc ab; das Kennfeld folgt dem Punkt");
            string fehler = AusTypkennfeld(e, m, a, KaeltemaschineImportVarianten.Rueckkuehltemperatur(bezug, m.Rueckkuehlart).Value,
                                           a.KaltwasserC.Value, bezug.LeistungKw, bezug.Eer, "auf den Punkt " + bezug.Name, out _);
            if (fehler != null) return fehler;
            m.Nennkaelteleistung_kW = null;
            m.Nenn_EER = null;
            KaeltemaschinenKennfeld.NennwerteErgaenzen(m);
            PunkteAnwenden(e, m, a);
            return null;
        }

        /// <summary>Wählt und skaliert das Typkennfeld, schreibt Hinweis und Herkunft in die Beschreibung (wenn leer).</summary>
        private static string AusTypkennfeld(Ergebnis e, KaeltemaschineModel m, Geraetangaben a, double rk, double kw, double q, double eer,
                                             string bezugText, out KaeltemaschineImportVarianten.Wahl wahl)
        {
            wahl = KaeltemaschineImportVarianten.TypkennfeldWaehlen(KaeltemaschinenTypkennfelder.Lesen(), m.Rueckkuehlart,
                                                                     a.Verdichter, m.Verdichterregelung, q);
            if (wahl == null) return "kein Typkennfeld zur Rückkühlart " + m.Rueckkuehlart;
            (double FaktorLeistung, double FaktorEer)? f = KaeltemaschineImportVarianten.Skalieren(m, wahl, rk, kw, q, eer);
            if (!f.HasValue) return wahl.Begruendung + " ohne Wert am Bezugspunkt";
            string text = "Kennfeld aus " + wahl.Begruendung + " " + bezugText + " skaliert (Leistung × " +
                          KaeltemaschineImportVarianten.Faktortext(f.Value.FaktorLeistung) + ", EER × " +
                          KaeltemaschineImportVarianten.Faktortext(f.Value.FaktorEer) + ")";
            e.Hinweise.Add(m.Bezeichner + ": " + text);
            m.Beschreibung ??= text;
            return null;
        }

        /// <summary>Die Teillast aus den Punkten A–D, sofern die Vorlage keine Teillastzeilen und keine Beiwerte nennt.</summary>
        private static void PunkteAnwenden(Ergebnis e, KaeltemaschineModel m, Geraetangaben a)
        {
            if (a.Teillast.Count > 0 || m.Teillastkurve_a.HasValue || m.Teillastkurve_b.HasValue || m.Teillastkurve_c.HasValue)
            {
                e.Hinweise.Add(m.Bezeichner + ": Teillastzeilen bzw. Beiwerte gehen vor, Punkte A–D nicht für die Teillastkurve genutzt");
                return;
            }
            if (!a.KaltwasserC.HasValue)
            {
                e.Hinweise.Add(m.Bezeichner + ": Punkte A–D ohne Kaltwassertemperatur, keine Teillastkurve");
                return;
            }
            if (a.Punkte.Any(p => !KaeltemaschineImportVarianten.Rueckkuehltemperatur(p, m.Rueckkuehlart).HasValue))
            {
                e.Hinweise.Add(m.Bezeichner + ": Punkte A–D ohne Rückkühltemperatur, keine Teillastkurve");
                return;
            }
            OekodesignPunkteLeser.Teillastabbildung t = KaeltemaschineImportVarianten.TeillastAusPunkten(m, a.Punkte, a.PdesignKw, a.KaltwasserC.Value);
            foreach (string h in t.Hinweise) e.Hinweise.Add(m.Bezeichner + ": " + h);
        }

        /// <summary>
        /// Die Teillastzeilen eines Geräts (KM3, Fachkonzept 3.2 b und 4.3): ab <see cref="KaeltemaschineTeillastkurve.MIN_ZEILEN"/>
        /// verschiedenen Lastgraden die Kurve nach kleinsten Quadraten, normiert, x_u = kleinster Lastgrad, Weg <c>KURVE</c>
        /// (ein Weg aus der Kopfzeile bleibt); weniger Zeilen oder eine unplausible Kurve: keine Kurve, mit Hinweis.
        /// Gepflegte Beiwerte aus den Kopfzeilen gehen vor. Zum Schluss die Prüfung der acht Felder.
        /// </summary>
        private static void TeillastAnpassen(Ergebnis e, KaeltemaschineModel m, List<(double Lastgrad, double Verhaeltnis)> teillast)
        {
            bool gepflegt = m.Teillastkurve_a.HasValue || m.Teillastkurve_b.HasValue || m.Teillastkurve_c.HasValue;
            if (teillast != null && teillast.Count > 0 && !gepflegt)
            {
                KaeltemaschineTeillastkurve.Kurve? k = KaeltemaschineTeillastkurve.AusEerVerhaeltnis(teillast);
                double xu = Math.Round(teillast.Min(z => z.Lastgrad), KaeltemaschineTeillastkurve.NACHKOMMA_LASTGRAD,
                                       MidpointRounding.AwayFromZero);
                if (!k.HasValue)
                    e.Hinweise.Add(m.Bezeichner + ": weniger als " +
                                   KaeltemaschineTeillastkurve.MIN_ZEILEN.ToString(CultureInfo.InvariantCulture) +
                                   " Teillastzeilen mit verschiedenem Lastgrad, Teillast ohne Kurve");
                else if (KaeltemaschineTeillastkurve.IstLinear(k.Value))
                    m.Teillast_Weg ??= KaeltemaschineTeillastSchema.WEG_LINEAR;
                else if (!KaeltemaschineTeillastkurve.Bereich(k.Value) ||
                         !KaeltemaschineStammCtrl.KurvePlausibel(k.Value.A, k.Value.B, k.Value.C, m.Teillastkurve_Lastgrad_Min ?? xu))
                    e.Hinweise.Add(m.Bezeichner + ": angepasste Teillastkurve nicht plausibel, Teillast ohne Kurve");
                else
                {
                    string weg = m.Teillast_Weg;
                    KaeltemaschineTeillastkurve.Setzen(m, k.Value, m.Teillastkurve_Lastgrad_Min ?? xu);
                    if (weg != null) m.Teillast_Weg = weg;
                }
            }
            string grund = KaeltemaschineStammCtrl.TeillastPruefen(m);
            if (grund != null)
            {
                e.Hinweise.Add(m.Bezeichner + ": " + grund);
                m.Teillast_Weg = null;
                m.Teillastkurve_a = m.Teillastkurve_b = m.Teillastkurve_c = null;
                m.Teillastkurve_Lastgrad_Min = null;
                if (KaeltemaschineStammCtrl.TeillastPruefen(m) != null)
                {
                    m.Taktverlustfaktor_Cd = null;
                    m.Verdichterregelung = null;
                    m.Kennfeld_Randweg = null;
                }
            }
        }

        /// <summary>
        /// Die Katalogfelder eines Geräts (K-A): fehlt die Geräteart, gilt die Rückfüllregel aus der Rückkühlart
        /// (<see cref="KaelteKatalogfelderSchema.GeraeteartAusRueckkuehlart"/>); verwirft die Prüfung die Felder, bleiben
        /// GWP, Füllmenge und saisonale Kennzahl leer und die Geräteart folgt der Regel — mit Hinweis.
        /// </summary>
        private static void KatalogfelderAnpassen(Ergebnis e, KaeltemaschineModel m)
        {
            m.Geraeteart = KaelteKatalogfelderSchema.GeraeteartWirksam(m.Geraeteart, m.Rueckkuehlart);
            string grund = KaeltemaschineStammCtrl.KatalogfelderPruefen(m);
            if (grund == null) return;
            e.Hinweise.Add(m.Bezeichner + ": " + grund);
            m.Geraeteart = KaelteKatalogfelderSchema.GeraeteartAusRueckkuehlart(m.Rueckkuehlart);
            m.Kaeltemittel_GWP = m.Kaeltemittel_Fuellmenge_kg = m.Saisonkennzahl = null;
            m.Saisonkennzahl_Art = null;
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
                // ---- Katalogfelder (K-A); leer = Rueckfuellregel bzw. keine Angabe ----
                case "GERAETEART":
                    return Auswahl(wert, KaelteKatalogfelderSchema.GERAETEARTEN, w => m.Geraeteart = w);
                case "GWP": case "KAELTEMITTEL_GWP":
                    m.Kaeltemittel_GWP = Zahl(wert, trenn);
                    return m.Kaeltemittel_GWP.HasValue || wert.Length == 0;
                case "FUELLMENGE": case "KAELTEMITTEL_FUELLMENGE": case "KAELTEMITTEL_FUELLMENGE_KG": case "FUELLMENGE_KG":
                    m.Kaeltemittel_Fuellmenge_kg = Zahl(wert, trenn);
                    return m.Kaeltemittel_Fuellmenge_kg.HasValue || wert.Length == 0;
                case "SEER":
                    m.Saisonkennzahl = Zahl(wert, trenn);
                    m.Saisonkennzahl_Art = m.Saisonkennzahl.HasValue ? KaelteKatalogfelderSchema.SAISON_SEER : null;
                    return m.Saisonkennzahl.HasValue || wert.Length == 0;
                case "ETA_S_C":
                    m.Saisonkennzahl = Zahl(wert, trenn);
                    m.Saisonkennzahl_Art = m.Saisonkennzahl.HasValue ? KaelteKatalogfelderSchema.SAISON_ETA_S_C : null;
                    return m.Saisonkennzahl.HasValue || wert.Length == 0;
                // ---- Teillast und Takten (KM3, Fachkonzept 4.1 und 4.3); leer = Vorgabe ----
                case "TEILLAST_WEG":
                    return Auswahl(wert, KaeltemaschineTeillastSchema.TEILLAST_WEGE, w => m.Teillast_Weg = w);
                case "VERDICHTERREGELUNG":
                    return Auswahl(wert, KaeltemaschineTeillastSchema.VERDICHTERREGELUNGEN, w => m.Verdichterregelung = w);
                case "KENNFELD_RANDWEG": case "RANDWEG":
                    return Auswahl(wert, KaeltemaschineTeillastSchema.RANDWEGE, w => m.Kennfeld_Randweg = w);
                case "TAKTVERLUSTFAKTOR_CD": case "TAKTVERLUSTFAKTOR": case "CDC": case "CD":
                    m.Taktverlustfaktor_Cd = Zahl(wert, trenn);
                    return m.Taktverlustfaktor_Cd.HasValue || wert.Length == 0;
                case "TEILLASTKURVE_LASTGRAD_MIN":
                    m.Teillastkurve_Lastgrad_Min = Zahl(wert, trenn);
                    return m.Teillastkurve_Lastgrad_Min.HasValue || wert.Length == 0;
                case "TEILLASTKURVE_A":
                    m.Teillastkurve_a = Zahl(wert, trenn);
                    return m.Teillastkurve_a.HasValue || wert.Length == 0;
                case "TEILLASTKURVE_B":
                    m.Teillastkurve_b = Zahl(wert, trenn);
                    return m.Teillastkurve_b.HasValue || wert.Length == 0;
                case "TEILLASTKURVE_C":
                    m.Teillastkurve_c = Zahl(wert, trenn);
                    return m.Teillastkurve_c.HasValue || wert.Length == 0;
                default:
                    return false;
            }
        }

        /// <summary>Ein Persistenzwert aus einer Werteliste (Schlüssel normiert, „/“ als „_“); leer = keine Angabe.</summary>
        private static bool Auswahl(string wert, IReadOnlyList<string> liste, Action<string> setzen)
        {
            if (string.IsNullOrWhiteSpace(wert)) { setzen(null); return true; }
            string s = Schluessel(wert).Replace('/', '_');
            if (!liste.Contains(s)) return false;
            setzen(s);
            return true;
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

            /// <summary>Hinweise zu übernommenen Sätzen (Teillastkurve: angepasst, nicht plausibel), „Satz: Grund“.</summary>
            public List<string> Hinweise { get; } = new List<string>();

            /// <summary><c>true</c> = Copper-JSON, sonst CSV-Vorlage.</summary>
            public bool Copper { get; set; }
        }

        /// <summary>Quellkennung eines Copper-Satzes.</summary>
        public const string QUELLE_COPPER = "PNNL Copper";

        /// <summary>Quellkennung der CSV-Vorlage (Form Kennfeld).</summary>
        public const string QUELLE_CSV = "CSV";

        /// <summary>Quellkennung der CSV-Vorlage in der Form „Nennwerte“ (K-C).</summary>
        public const string QUELLE_CSV_NENNWERTE = "CSV Nennwerte";

        /// <summary>Quellkennung der CSV-Vorlage in der Form „Ökodesign-Datenblatt A–D“ (K-C).</summary>
        public const string QUELLE_CSV_OEKODESIGN = "CSV Ökodesign A–D";

        /// <summary>Die Quellkennung einer Form der CSV-Vorlage.</summary>
        public static string Quelle(string form) => form switch
        {
            KaeltemaschineImportVarianten.FORM_NENNWERTE => QUELLE_CSV_NENNWERTE,
            KaeltemaschineImportVarianten.FORM_OEKODESIGN => QUELLE_CSV_OEKODESIGN,
            _ => QUELLE_CSV
        };

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
                                      "; Nennpunkt Eurovent", hinweise: e.Hinweise);
                    e.Saetze.Add((m, QUELLE_COPPER));
                }
                e.Uebergangen.AddRange(c.Uebergangen);
                return e;
            }
            KaeltemaschineCsvLeser.Ergebnis v = KaeltemaschineCsvLeser.Lesen(text);
            for (int i = 0; i < v.Geraete.Count; i++)
                e.Saetze.Add((v.Geraete[i], Quelle(v.Formen[i])));
            e.Uebergangen.AddRange(v.Uebergangen);
            e.Hinweise.AddRange(v.Hinweise);
            return e;
        }
    }
}
