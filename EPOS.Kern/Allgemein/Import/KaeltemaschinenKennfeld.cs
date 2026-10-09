using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine bi-quadratische Kurve</b> des EIR-Modells (DOE-2/EnergyPlus, PNNL Copper):
    /// f(x, y) = c1 + c2·x + c3·x² + c4·y + c5·y² + c6·x·y mit x = Kaltwasser-Austritt und
    /// y = Kondensator-Eintritt. Wie in Copper werden die Eingänge auf die Gültigkeitsgrenzen und das
    /// Ergebnis auf seine Grenzen geklemmt — außerhalb gilt der Randwert, nie eine Extrapolation.
    /// </summary>
    public sealed class KaeltemaschinenKurve
    {
        /// <summary>Die sechs Beiwerte c1 … c6.</summary>
        public double C1, C2, C3, C4, C5, C6;

        /// <summary>Gültigkeitsgrenzen der Kaltwasser-Achse x [°C bzw. °F].</summary>
        public double XMin = double.NegativeInfinity, XMax = double.PositiveInfinity;

        /// <summary>Gültigkeitsgrenzen der Kondensator-Achse y [°C bzw. °F].</summary>
        public double YMin = double.NegativeInfinity, YMax = double.PositiveInfinity;

        /// <summary>Grenzen des Ergebnisses; <c>null</c> = offen.</summary>
        public double? AusMin, AusMax;

        /// <summary><c>true</c> = die Kurve rechnet in °F (Copper „ip“); die Eingänge kommen in °C und werden umgerechnet.</summary>
        public bool Fahrenheit;

        /// <summary>Die Gültigkeitsgrenzen der Kondensator-Achse in °C.</summary>
        public double YMinC => Fahrenheit ? NachCelsius(YMin) : YMin;

        /// <summary>Die obere Gültigkeitsgrenze der Kondensator-Achse in °C.</summary>
        public double YMaxC => Fahrenheit ? NachCelsius(YMax) : YMax;

        /// <summary>Wertet die Kurve bei Kaltwasser <paramref name="kaltwasserC"/> und Kondensator <paramref name="kondensatorC"/> [°C] aus.</summary>
        public double Auswerten(double kaltwasserC, double kondensatorC)
        {
            double x = Fahrenheit ? NachFahrenheit(kaltwasserC) : kaltwasserC;
            double y = Fahrenheit ? NachFahrenheit(kondensatorC) : kondensatorC;
            x = Math.Min(Math.Max(x, XMin), XMax);
            y = Math.Min(Math.Max(y, YMin), YMax);
            double f = C1 + C2 * x + C3 * x * x + C4 * y + C5 * y * y + C6 * x * y;
            if (AusMin.HasValue) f = Math.Max(f, AusMin.Value);
            if (AusMax.HasValue) f = Math.Min(f, AusMax.Value);
            return f;
        }

        private static double NachFahrenheit(double c) => c * 9.0 / 5.0 + 32.0;

        private static double NachCelsius(double f) => (f - 32.0) * 5.0 / 9.0;
    }

    /// <summary>
    /// <b>Ein Kurvensatz eines Kaltwassersatzes</b> nach dem EIR-Modell: Referenzleistung, Referenz-COP und
    /// die Kurven CAPFT und EIRFT (Format PNNL Copper <c>chiller_curves.json</c>, BSD-2).
    /// </summary>
    public sealed class KaeltemaschinenKurvensatz
    {
        /// <summary>Die laufende Nummer des Satzes in der Quelle (Copper: der Schlüssel).</summary>
        public string Nr { get; set; } = "";

        /// <summary>Kondensatorart der Quelle: <c>air</c>, <c>water</c> (auch <c>hr_scroll</c> = wassergekühlt).</summary>
        public string Kondensator { get; set; } = "";

        /// <summary>Verdichterart der Quelle: <c>scroll</c>, <c>screw</c>, <c>centrifugal</c>, <c>reciprocating</c>.</summary>
        public string Verdichter { get; set; } = "";

        /// <summary>Drehzahl der Quelle: <c>constant</c>, <c>variable</c> oder leer.</summary>
        public string Drehzahl { get; set; } = "";

        /// <summary>Referenzkälteleistung am Referenzpunkt der Kurven [kW].</summary>
        public double ReferenzleistungKw { get; set; }

        /// <summary>Referenz-COP (= EER) am Referenzpunkt der Kurven [—].</summary>
        public double ReferenzCop { get; set; }

        /// <summary>Kleinstes Teillastverhältnis (unterhalb taktet die Maschine) [—]; <c>null</c> = keine Angabe.</summary>
        public double? MinPlr { get; set; }

        /// <summary>Kleinste Entlastungsstufe [—]; <c>null</c> = keine Angabe.</summary>
        public double? MinUnloading { get; set; }

        /// <summary>Kapazitätskurve CAPFT.</summary>
        public KaeltemaschinenKurve CapFT { get; set; }

        /// <summary>Kurve des Leistungsverhältnisses EIRFT.</summary>
        public KaeltemaschinenKurve EirFT { get; set; }

        /// <summary>Ist der Satz luftgekühlt?</summary>
        public bool IstLuft => string.Equals(Kondensator, "air", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <b>Das Kennfeld aus Kurven</b> (KM1, Stufe 1): Aus CAPFT und EIRFT entsteht das bilineare Raster der
    /// EPOS-Kältemaschine (<c>Tab_Kenndaten_Kaeltemaschine_STAMM</c>) — Q = Q_ref · CAPFT und
    /// EER = COP_ref / EIRFT an jedem Gitterpunkt.
    ///
    /// <para><b>Achsen.</b> Die Kaltwasser-Achse ist der Kaltwasser-VORLAUF (Austritt aus dem Verdampfer) —
    /// dieselbe Achse wie im EIR-Modell und in <see cref="Kaeltemaschine"/>; vier feste Stützstellen
    /// <see cref="KALTWASSER_ACHSE"/>. Die Rückkühl-Achse sind <see cref="RUECKKUEHL_PUNKTE"/> gleichabständige
    /// Stützstellen innerhalb der Gültigkeitsgrenzen beider Kurven, beschnitten auf ein Vorgabeband.</para>
    ///
    /// <para><b>Bezug bei Luftkühlung (Konzeptentscheid KM1).</b> EPOS wertet das Kennfeld einer luftgekühlten
    /// Maschine an Rückkühltemperatur = Außenluft + <see cref="KaelteFestwerte.GRAEDIGKEIT_LUFT_K"/> aus
    /// (<see cref="Kaeltemaschine.Rueckkuehltemperatur"/>), die Copper-Luftkurven gelten an der Außenluft.
    /// Deshalb wird jede Rasterzeile T_rk an der Kurve bei T_kond = T_rk − Grädigkeit ausgewertet; die
    /// Simulation trifft so den Kurvenwert bei der echten Außenluft. Wassergekühlt (Trockenkühler, Nasskühler,
    /// Wasser) ist die Achse der Kondensator-Eintritt selbst, ohne Versatz.</para>
    ///
    /// <para><b>Nennpunkt.</b> Nennkälteleistung und Nenn-EER stehen am Eurovent-Nennpunkt (Kaltwasser 7 °C
    /// Austritt; Außenluft 35 °C bzw. Kühlwasser 30 °C Eintritt), nicht an der US-Referenz der Kurven.</para>
    /// </summary>
    public static class KaeltemaschinenKennfeld
    {
        /// <summary>Der Typ eines Katalogsatzes aus Kurven (Typkennfeld, Copper-Kurvensatz).</summary>
        public const string TYP = "Typkennfeld";

        /// <summary>Die Kaltwasser-Stützstellen (Vorlauf, Austritt) [°C].</summary>
        public static readonly IReadOnlyList<double> KALTWASSER_ACHSE = new[] { 5.0, 7.0, 10.0, 15.0 };

        /// <summary>Zahl der Rückkühl-Stützstellen.</summary>
        public const int RUECKKUEHL_PUNKTE = 6;

        /// <summary>Vorgabeband der Kondensator-Achse bei Luftkühlung (Außenluft) [°C].</summary>
        public const double LUFT_UNTEN_C = 15.0, LUFT_OBEN_C = 45.0;

        /// <summary>Vorgabeband der Kondensator-Achse bei Wasserkühlung (Kühlwasser-Eintritt) [°C].</summary>
        public const double WASSER_UNTEN_C = 15.0, WASSER_OBEN_C = 40.0;

        /// <summary>Eurovent-Nennpunkt: Kaltwasser-Austritt [°C].</summary>
        public const double NENN_KALTWASSER_C = 7.0;

        /// <summary>Eurovent-Nennpunkt: Außenluft bei Luftkühlung [°C].</summary>
        public const double NENN_LUFT_C = 35.0;

        /// <summary>Eurovent-Nennpunkt: Kühlwasser-Eintritt bei Wasserkühlung [°C].</summary>
        public const double NENN_WASSER_C = 30.0;

        /// <summary>Der Versatz zwischen Rückkühltemperatur der Rasterzeile und Kondensatortemperatur der Kurve [K].</summary>
        public static double Versatz(string rueckkuehlart) =>
            rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_LUFT ? KaelteFestwerte.GRAEDIGKEIT_LUFT_K : 0.0;

        /// <summary>Die Rückkühltemperatur [°C], an der EPOS den Eurovent-Nennpunkt einer Rückkühlart auswertet.</summary>
        public static double Nennrueckkuehltemperatur(string rueckkuehlart) =>
            rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_LUFT
                ? NENN_LUFT_C + KaelteFestwerte.GRAEDIGKEIT_LUFT_K
                : NENN_WASSER_C;

        /// <summary>Die Kondensator-Stützstellen eines Satzes [°C], in der Temperatur der KURVE (ohne Versatz).</summary>
        public static IReadOnlyList<double> Kondensatorachse(KaeltemaschinenKurvensatz satz)
        {
            bool luft = satz.IstLuft;
            double unten = Math.Max(luft ? LUFT_UNTEN_C : WASSER_UNTEN_C, Math.Max(satz.CapFT.YMinC, satz.EirFT.YMinC));
            double oben = Math.Min(luft ? LUFT_OBEN_C : WASSER_OBEN_C, Math.Min(satz.CapFT.YMaxC, satz.EirFT.YMaxC));
            if (!(oben - unten >= 1.0))
            {
                // Ein Gültigkeitsbereich ohne Überdeckung mit dem Vorgabeband: dann das Vorgabeband selbst —
                // die Kurve hält ihren Randwert (Copper-Klemmung).
                unten = luft ? LUFT_UNTEN_C : WASSER_UNTEN_C;
                oben = luft ? LUFT_OBEN_C : WASSER_OBEN_C;
            }
            var achse = new List<double>();
            for (int i = 0; i < RUECKKUEHL_PUNKTE; i++)
            {
                double t = Math.Round(unten + (oben - unten) * i / (RUECKKUEHL_PUNKTE - 1), 1, MidpointRounding.AwayFromZero);
                if (achse.Count == 0 || t > achse[achse.Count - 1]) achse.Add(t);
            }
            return achse;
        }

        /// <summary>Q und EER der Kurven bei Kaltwasser-Austritt und Kondensatortemperatur der Kurve.</summary>
        public static (double LeistungKw, double Eer) Kurvenwert(KaeltemaschinenKurvensatz satz, double kaltwasserC, double kondensatorC)
        {
            double cap = satz.CapFT.Auswerten(kaltwasserC, kondensatorC);
            double eir = satz.EirFT.Auswerten(kaltwasserC, kondensatorC);
            return (satz.ReferenzleistungKw * cap, eir > 0 ? satz.ReferenzCop / eir : 0.0);
        }

        /// <summary>Q und EER am Eurovent-Nennpunkt (Kaltwasser 7 °C; Luft 35 °C bzw. Kühlwasser 30 °C).</summary>
        public static (double LeistungKw, double Eer) Nennpunkt(KaeltemaschinenKurvensatz satz) =>
            Kurvenwert(satz, NENN_KALTWASSER_C, satz.IstLuft ? NENN_LUFT_C : NENN_WASSER_C);

        /// <summary>
        /// Das Raster der Kennlinie: je Rückkühl- und Kaltwasser-Stützstelle ein Punkt; die Rückkühltemperatur
        /// trägt bei Luftkühlung den Versatz (siehe Klassenkopf). <paramref name="faktor"/> skaliert die
        /// Kälteleistung (Typkennfeld einer Leistungsklasse), der EER bleibt.
        /// </summary>
        public static List<KaeltemaschineKenndatenModel> Raster(KaeltemaschinenKurvensatz satz, string rueckkuehlart, double faktor = 1.0)
        {
            double versatz = satz.IstLuft ? KaelteFestwerte.GRAEDIGKEIT_LUFT_K : 0.0;
            var punkte = new List<KaeltemaschineKenndatenModel>();
            foreach (double tk in Kondensatorachse(satz))
                foreach (double kw in KALTWASSER_ACHSE)
                {
                    (double q, double eer) = Kurvenwert(satz, kw, tk);
                    punkte.Add(new KaeltemaschineKenndatenModel
                    {
                        Rueckkuehltemperatur = Math.Round(tk + versatz, 1, MidpointRounding.AwayFromZero),
                        Kaltwassertemperatur = kw,
                        Kaelteleistung_kW = Math.Round(q * faktor, 2, MidpointRounding.AwayFromZero),
                        EER = Math.Round(eer, 3, MidpointRounding.AwayFromZero)
                    });
                }
            return punkte;
        }

        /// <summary>Die Rückkühlart, die ein Satz ohne weitere Angabe bekommt: Luft → LUFT, Wasser → NASSKUEHLER.</summary>
        public static string VorgabeRueckkuehlart(KaeltemaschinenKurvensatz satz) =>
            satz.IstLuft ? KaeltemaschineSchema.RUECKKUEHLART_LUFT : KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER;

        /// <summary>Die Mindestteillast [%] aus <c>min_plr</c>, ersatzweise <c>min_unloading</c>; <c>null</c> ohne Angabe.</summary>
        public static double? Mindestteillast(KaeltemaschinenKurvensatz satz)
        {
            double? v = satz.MinPlr ?? satz.MinUnloading;
            if (!v.HasValue || !(v.Value >= 0) || v.Value > 1) return null;
            return Math.Round(v.Value * 100.0, 1, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Baut einen Katalogsatz aus einem Kurvensatz. <paramref name="klasseKw"/> ≠ <c>null</c> skaliert das
        /// Kennfeld so, dass die Kälteleistung am Nennpunkt genau die Klasse ist (Typkennfeld).
        /// Ein Satz mit wassergekühltem Kurvensatz und <see cref="KaeltemaschineSchema.RUECKKUEHLART_LUFT"/> ist
        /// nicht zulässig (die Achse passte nicht) — dann gilt die Vorgabe des Satzes.
        /// </summary>
        public static KaeltemaschineModel Modell(KaeltemaschinenKurvensatz satz, string bezeichner, string rueckkuehlart = null,
                                                 double? klasseKw = null, string typ = null, string beschreibung = null)
        {
            string art = rueckkuehlart ?? VorgabeRueckkuehlart(satz);
            if (satz.IstLuft != (art == KaeltemaschineSchema.RUECKKUEHLART_LUFT)) art = VorgabeRueckkuehlart(satz);
            (double qNenn, double eerNenn) = Nennpunkt(satz);
            double faktor = klasseKw.HasValue && qNenn > 0 ? klasseKw.Value / qNenn : 1.0;
            return new KaeltemaschineModel
            {
                Bezeichner = bezeichner ?? "",
                Firma = null,
                Typ = typ,
                Beschreibung = beschreibung,
                Nennkaelteleistung_kW = Math.Round(qNenn * faktor, 1, MidpointRounding.AwayFromZero),
                Nenn_EER = Math.Round(eerNenn, 2, MidpointRounding.AwayFromZero),
                Kaeltemittel = null,
                Rueckkuehlart = art,
                Mindestteillast_Prozent = Mindestteillast(satz),
                Kennlinie = Raster(satz, art, faktor)
            };
        }

        /// <summary>Der deutsche Anzeigename einer Verdichterart der Quelle (für neutrale Bezeichner).</summary>
        public static string Verdichtername(string verdichter, string drehzahl)
        {
            string v = (verdichter ?? "").ToLowerInvariant() switch
            {
                "scroll" => "Scroll",
                "screw" => "Schraube",
                "centrifugal" => "Turbo",
                "reciprocating" => "Hubkolben",
                _ => string.IsNullOrEmpty(verdichter) ? "Verdichter" : verdichter
            };
            return string.Equals(drehzahl, "variable", StringComparison.OrdinalIgnoreCase) ? v + " drehzahlgeregelt" : v;
        }

        /// <summary>Der deutsche Kurzname einer Rückkühlart für neutrale Bezeichner.</summary>
        public static string Rueckkuehlname(string rueckkuehlart) => rueckkuehlart switch
        {
            KaeltemaschineSchema.RUECKKUEHLART_LUFT => "Luft",
            KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER => "Trockenkühler",
            KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER => "Nasskühler",
            KaeltemaschineSchema.RUECKKUEHLART_WASSER => "Wasser",
            _ => rueckkuehlart ?? ""
        };

        /// <summary>Eine Leistung als ganze Zahl ohne Tausenderpunkt („1000“) für Bezeichner.</summary>
        public static string Leistungstext(double kw) =>
            Math.Round(kw, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);

        /// <summary>
        /// Ergänzt bei einem Satz aus der CSV-Vorlage fehlende Nennwerte aus dem eigenen Kennfeld am
        /// Eurovent-Nennpunkt (bilinear, wie die Simulation).
        /// </summary>
        public static void NennwerteErgaenzen(KaeltemaschineModel m)
        {
            if (m == null || m.Kennlinie == null || m.Kennlinie.Count == 0) return;
            if (m.Nennkaelteleistung_kW.HasValue && m.Nenn_EER.HasValue) return;
            var k = new KaeltemaschinenKennlinie(m.Kennlinie.Select(p => (p.Rueckkuehltemperatur, p.Kaltwassertemperatur, p.EER, p.Kaelteleistung_kW)));
            if (k.Leer) return;
            KaeltemaschinenPunkt p = k.Auswerten(Nennrueckkuehltemperatur(m.Rueckkuehlart), NENN_KALTWASSER_C);
            if (!m.Nennkaelteleistung_kW.HasValue && p.LeistungKw > 0)
                m.Nennkaelteleistung_kW = Math.Round(p.LeistungKw, 1, MidpointRounding.AwayFromZero);
            if (!m.Nenn_EER.HasValue && p.Eer > 0)
                m.Nenn_EER = Math.Round(p.Eer, 2, MidpointRounding.AwayFromZero);
        }
    }
}
