using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Importvarianten der CSV-Vorlage einer Kältemaschine</b> (K-C): neben dem Kennfeld (KM1) die Form
    /// „Nennwerte“ (ein Nennpunkt) und die Form „Ökodesign-Datenblatt A–D“ (Teillastpunkte). Beide bekommen ihr Kennfeld
    /// aus einem eingebauten <see cref="KaeltemaschinenTypkennfelder">Typkennfeld</see>, auf den Bezugspunkt skaliert —
    /// Kälteleistung und EER je mit einem Faktor, die Form der Kurven über Rückkühl- und Kaltwassertemperatur bleibt die
    /// des Typkennfelds.
    ///
    /// <para><b>Wahl des Typkennfelds</b> (<see cref="TypkennfeldWaehlen"/>), in dieser Rangfolge: dieselbe Rückkühlart
    /// (sonst dieselbe Klasse Luft bzw. Wasser — eine luftgekühlte Kurve passt nie an eine wassergekühlte Achse);
    /// derselbe Verdichter, wenn angegeben; dieselbe Drehzahlart (Verdichterregelung <c>DREHZAHL</c> ↔ drehzahlgeregelt,
    /// ohne Angabe die ungeregelte); die nächstliegende Leistungsklasse im logarithmischen Abstand; bei Gleichstand die
    /// Folge der Ressource. Die Form des Kennfelds hängt an Rückkühlart und Verdichter, kaum an der Größe — deshalb
    /// stehen sie vor der Klasse.</para>
    ///
    /// <para><b>Grenzen.</b> Ein skaliertes Typkennfeld trifft den Bezugspunkt genau; abseits davon gilt die
    /// Temperaturabhängigkeit des Typs, nicht die des Geräts. Aus den Punkten A–D lassen sich Temperatur- und
    /// Teillasteinfluss nicht trennen (jeder Punkt hat eine andere Außentemperatur UND eine andere Last): Die Abbildung
    /// nimmt die Temperatur aus dem Kennfeld und schreibt den Rest der Teillastkurve zu
    /// (<see cref="OekodesignPunkteLeser.Teillast"/>).</para>
    /// </summary>
    public static class KaeltemaschineImportVarianten
    {
        /// <summary>Form „Kennfeld“: Rasterzeilen Rückkühl- × Kaltwassertemperatur (KM1).</summary>
        public const string FORM_KENNFELD = "KENNFELD";

        /// <summary>Form „Nennwerte“: nur der Nennpunkt, Kennfeld aus einem Typkennfeld.</summary>
        public const string FORM_NENNWERTE = "NENNWERTE";

        /// <summary>Form „Ökodesign-Datenblatt A–D“: Teillastpunkte, Kennfeld aus einem Typkennfeld.</summary>
        public const string FORM_OEKODESIGN = "OEKODESIGN";

        /// <summary>Die drei Formen.</summary>
        public static readonly IReadOnlyList<string> FORMEN = new[] { FORM_KENNFELD, FORM_NENNWERTE, FORM_OEKODESIGN };

        /// <summary>Die Verdichterangaben der Vorlage und ihre Entsprechung in der Quelle der Typkennfelder.</summary>
        public static readonly IReadOnlyDictionary<string, string> VERDICHTER = new Dictionary<string, string>
        {
            ["SCROLL"] = "scroll",
            ["SCHRAUBE"] = "screw",
            ["TURBO"] = "centrifugal",
            ["HUBKOLBEN"] = "reciprocating",
        };

        /// <summary>
        /// Die Form eines Geräts: eine angegebene Form gilt; sonst Kennfeldzeilen → <see cref="FORM_KENNFELD"/>,
        /// Teillastpunkte → <see cref="FORM_OEKODESIGN"/>, sonst <see cref="FORM_NENNWERTE"/>.
        /// </summary>
        public static string FormErkennen(string angegeben, bool kennfeld, bool punkte)
        {
            if (!string.IsNullOrEmpty(angegeben)) return angegeben;
            if (kennfeld) return FORM_KENNFELD;
            return punkte ? FORM_OEKODESIGN : FORM_NENNWERTE;
        }

        /// <summary>Die Form aus dem Wert der Kopfzeile „Form“ (auch „Nennwerte“, „Ökodesign A–D“); <c>null</c> = unbekannt.</summary>
        public static string Form(string wert)
        {
            string s = KaeltemaschineCsvLeser.Schluessel(wert);
            if (s.StartsWith("OEKODESIGN", StringComparison.Ordinal)) return FORM_OEKODESIGN;
            if (s.StartsWith("NENNWERT", StringComparison.Ordinal)) return FORM_NENNWERTE;
            if (s.StartsWith("KENNFELD", StringComparison.Ordinal)) return FORM_KENNFELD;
            return null;
        }

        /// <summary>Ein gewähltes Typkennfeld und die Begründung der Wahl.</summary>
        public sealed record Wahl(KaeltemaschinenTypkennfelder.Typkennfeld Typkennfeld, string Begruendung);

        /// <summary>
        /// Wählt das Typkennfeld für ein Gerät (Rangfolge im Klassenkopf); <c>null</c>, wenn keines zur Rückkühlklasse passt.
        /// </summary>
        /// <param name="liste">Die Typkennfelder (<see cref="KaeltemaschinenTypkennfelder.Lesen"/>).</param>
        /// <param name="rueckkuehlart">Persistenzwert der Rückkühlart.</param>
        /// <param name="verdichter">Verdichter der Vorlage (<see cref="VERDICHTER"/>-Schlüssel) oder <c>null</c>.</param>
        /// <param name="regelung">Verdichterregelung oder <c>null</c>.</param>
        /// <param name="leistungKw">Kälteleistung am Bezugspunkt [kW].</param>
        public static Wahl TypkennfeldWaehlen(IReadOnlyList<KaeltemaschinenTypkennfelder.Typkennfeld> liste, string rueckkuehlart,
                                              string verdichter, string regelung, double leistungKw)
        {
            bool luft = rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_LUFT;
            string quelle = verdichter != null && VERDICHTER.TryGetValue(verdichter, out string v) ? v : null;
            bool drehzahl = regelung == KaeltemaschineTeillastSchema.REGELUNG_DREHZAHL;
            var bewertet = (liste ?? Array.Empty<KaeltemaschinenTypkennfelder.Typkennfeld>())
                .Select((t, i) => (T: t, I: i))
                .Where(x => x.T.Kurven.IstLuft == luft)
                .Select(x => (x.T, x.I,
                              Art: x.T.Rueckkuehlart == rueckkuehlart ? 0 : 1,
                              Verd: quelle == null || string.Equals(x.T.Kurven.Verdichter, quelle, StringComparison.OrdinalIgnoreCase) ? 0 : 1,
                              Dreh: string.Equals(x.T.Kurven.Drehzahl, "variable", StringComparison.OrdinalIgnoreCase) == drehzahl ? 0 : 1,
                              Abstand: leistungKw > 0 && x.T.KlasseKw > 0 ? Math.Abs(Math.Log(leistungKw / x.T.KlasseKw)) : 0.0))
                .OrderBy(x => x.Art).ThenBy(x => x.Verd).ThenBy(x => x.Dreh).ThenBy(x => x.Abstand).ThenBy(x => x.I)
                .ToList();
            if (bewertet.Count == 0) return null;
            var b = bewertet[0];
            var gruende = new List<string>
            {
                b.Art == 0 ? "gleiche Rückkühlart" : "gleiche Rückkühlklasse (" + (luft ? "Luft" : "Wasser") + ")"
            };
            if (quelle != null) gruende.Add(b.Verd == 0 ? "gleicher Verdichter" : "kein Typkennfeld mit diesem Verdichter");
            if (regelung != null || b.Dreh != 0) gruende.Add(b.Dreh == 0 ? "gleiche Drehzahlart" : "andere Drehzahlart");
            gruende.Add("nächstliegende Klasse");
            return new Wahl(b.T, "Typkennfeld „" + b.T.Bezeichner + "“ (" + string.Join(", ", gruende) + ")");
        }

        /// <summary>
        /// Setzt die Kennlinie von <paramref name="m"/> aus dem Typkennfeld <paramref name="wahl"/>, skaliert so, dass das
        /// Kennfeld am Bezugspunkt (<paramref name="bezugRueckkuehlC"/>, <paramref name="bezugKaltwasserC"/>; bilinear wie die
        /// Simulation) die Leistung <paramref name="leistungKw"/> und den EER <paramref name="eer"/> trägt. Gibt die beiden
        /// Faktoren zurück; <c>null</c>, wenn das Typkennfeld am Bezugspunkt keinen Wert hat.
        /// </summary>
        public static (double FaktorLeistung, double FaktorEer)? Skalieren(KaeltemaschineModel m, Wahl wahl, double bezugRueckkuehlC,
                                                                           double bezugKaltwasserC, double leistungKw, double eer)
        {
            KaeltemaschinenTypkennfelder.Typkennfeld t = wahl.Typkennfeld;
            List<KaeltemaschineKenndatenModel> raster = KaeltemaschinenKennfeld.Raster(t.Kurven, m.Rueckkuehlart);
            KaeltemaschinenPunkt p = Auswerten(raster, bezugRueckkuehlC, bezugKaltwasserC);
            if (!(p.LeistungKw > 0) || !(p.Eer > 0)) return null;
            double fq = leistungKw / p.LeistungKw, fe = eer / p.Eer;
            m.Kennlinie = raster.Select(k => new KaeltemaschineKenndatenModel
            {
                Rueckkuehltemperatur = k.Rueckkuehltemperatur,
                Kaltwassertemperatur = k.Kaltwassertemperatur,
                Kaelteleistung_kW = k.Kaelteleistung_kW.HasValue
                    ? Math.Round(k.Kaelteleistung_kW.Value * fq, 2, MidpointRounding.AwayFromZero) : (double?)null,
                EER = k.EER.HasValue ? Math.Round(k.EER.Value * fe, 3, MidpointRounding.AwayFromZero) : (double?)null
            }).ToList();
            return (fq, fe);
        }

        /// <summary>Wertet eine Kennlinie bilinear aus (wie die Simulation).</summary>
        public static KaeltemaschinenPunkt Auswerten(IEnumerable<KaeltemaschineKenndatenModel> kennlinie, double rueckkuehlC, double kaltwasserC)
        {
            var k = new KaeltemaschinenKennlinie(kennlinie.Select(p => (p.Rueckkuehltemperatur, p.Kaltwassertemperatur, p.EER, p.Kaelteleistung_kW)));
            return k.Leer ? default : k.Auswerten(rueckkuehlC, kaltwasserC);
        }

        /// <summary>
        /// Übernimmt Teillast und Takten des Typkennfelds (KM3) in ein Gerät der Form „Nennwerte“, wenn die Vorlage dazu
        /// nichts angibt; die Mindestteillast nur, wenn sie fehlt.
        /// </summary>
        public static void TeillastVomTyp(KaeltemaschineModel m, Wahl wahl)
        {
            KaeltemaschineModel typ = KaeltemaschinenKennfeld.Modell(wahl.Typkennfeld.Kurven, wahl.Typkennfeld.Bezeichner,
                                                                     wahl.Typkennfeld.Rueckkuehlart, wahl.Typkennfeld.KlasseKw);
            m.Mindestteillast_Prozent ??= typ.Mindestteillast_Prozent;
            bool angegeben = m.Teillast_Weg != null || m.Teillastkurve_a.HasValue || m.Teillastkurve_b.HasValue ||
                             m.Teillastkurve_c.HasValue || m.Verdichterregelung != null;
            if (angegeben) return;
            m.Teillast_Weg = typ.Teillast_Weg;
            m.Teillastkurve_a = typ.Teillastkurve_a;
            m.Teillastkurve_b = typ.Teillastkurve_b;
            m.Teillastkurve_c = typ.Teillastkurve_c;
            m.Teillastkurve_Lastgrad_Min ??= typ.Teillastkurve_Lastgrad_Min;
            m.Verdichterregelung = typ.Verdichterregelung;
        }

        /// <summary>
        /// Die Rückkühltemperatur eines Punkts A–D: die zweite Bedingung der Datei; ohne sie bei Luftkühlung die
        /// Außentemperatur plus <see cref="KaelteFestwerte.GRAEDIGKEIT_LUFT_K"/> (der Bezug des Kennfelds, KM1);
        /// bei Wasserkühlung ist sie Pflicht (<c>null</c>).
        /// </summary>
        public static double? Rueckkuehltemperatur(OekodesignPunkteLeser.Punkt p, string rueckkuehlart)
        {
            if (p.Zweittemperatur.HasValue) return p.Zweittemperatur.Value;
            return rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_LUFT
                ? p.Aussentemperatur + KaelteFestwerte.GRAEDIGKEIT_LUFT_K
                : (double?)null;
        }

        /// <summary>
        /// Setzt Teillast und Takten aus den Punkten A–D gegen die Kennlinie des Geräts (<see cref="OekodesignPunkteLeser.Teillast"/>):
        /// eine plausible Kurve → Weg <c>KURVE</c> mit Beiwerten und x_u; keine Kurve, aber taktende Punkte → Weg
        /// <c>LINEAR</c> (mit Taktverlust); die Mindestteillast aus den taktenden Punkten, wenn die Vorlage keine nennt.
        /// Gibt die Abbildung zurück (Hinweise darin).
        /// </summary>
        public static OekodesignPunkteLeser.Teillastabbildung TeillastAusPunkten(KaeltemaschineModel m, IReadOnlyList<OekodesignPunkteLeser.Punkt> punkte,
                                                                                 double? pdesignKw, double kaltwasserC)
        {
            double bezug = m.Nennkaelteleistung_kW ?? pdesignKw ?? 0;
            OekodesignPunkteLeser.Teillastabbildung a = OekodesignPunkteLeser.Teillast(punkte, pdesignKw, bezug, p =>
            {
                double? rk = Rueckkuehltemperatur(p, m.Rueckkuehlart);
                if (!rk.HasValue) return (0, 0);
                KaeltemaschinenPunkt v = Auswerten(m.Kennlinie, rk.Value, kaltwasserC);
                return (v.LeistungKw, v.Eer);
            });
            if (a.Kurve.HasValue)
            {
                if (KaeltemaschineTeillastkurve.IstLinear(a.Kurve.Value)) m.Teillast_Weg = KaeltemaschineTeillastSchema.WEG_LINEAR;
                else KaeltemaschineTeillastkurve.Setzen(m, a.Kurve.Value, a.LastgradMin);
            }
            else if (a.Taktet)
                m.Teillast_Weg = KaeltemaschineTeillastSchema.WEG_LINEAR;
            if (a.MindestteillastAnteil.HasValue && !m.Mindestteillast_Prozent.HasValue)
                m.Mindestteillast_Prozent = Math.Round(a.MindestteillastAnteil.Value * 100.0, 1, MidpointRounding.AwayFromZero);
            return a;
        }

        /// <summary>Ein Faktor als Text mit zwei Nachkommastellen („1,23“ wird „1.23“ — invariant).</summary>
        public static string Faktortext(double f) => f.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
