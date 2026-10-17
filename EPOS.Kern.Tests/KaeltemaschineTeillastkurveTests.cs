using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KM3 — die Teillastkurve ohne Datenbank</b>: Anpassung nach kleinsten Quadraten, Normierung, die Vorgabekurven je
    /// Verdichterregelung (abgeleitet aus den Typkennfeldern, in den Plausibilitätsgrenzen) und der Kontrollwert Nenn-EER
    /// (Fachkonzept Teillast und Takten 3.2, 3.5 und 4.4).
    /// </summary>
    public sealed class KaeltemaschineTeillastkurveTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Anpassen_trifft_ein_exaktes_Polynom_und_braucht_drei_Lastgrade()
        {
            var punkte = new[] { 0.2, 0.4, 0.6, 0.8, 1.0 }.Select(x => (x, 0.12 + 0.55 * x + 0.33 * x * x)).ToList();
            KaeltemaschineTeillastkurve.Kurve? k = KaeltemaschineTeillastkurve.Anpassen(punkte);
            Assert.True(k.HasValue);
            Assert.Equal(0.12, k.Value.A, 9);
            Assert.Equal(0.55, k.Value.B, 9);
            Assert.Equal(0.33, k.Value.C, 9);
            Assert.Null(KaeltemaschineTeillastkurve.Anpassen(new[] { (0.5, 0.5), (0.5, 0.6), (1.0, 1.0) }));
            Assert.Null(KaeltemaschineTeillastkurve.Anpassen(null));
        }

        [Fact]
        public void Normieren_setzt_EIRFPLR_von_1_auf_1_und_erkennt_die_Gerade()
        {
            KaeltemaschineTeillastkurve.Kurve? k = KaeltemaschineTeillastkurve.Normieren(0.2, 0.5, 0.3 + 0.012);
            Assert.True(k.HasValue);
            Assert.Equal(1.0, k.Value.Volllast, 5);
            Assert.Null(KaeltemaschineTeillastkurve.Normieren(-0.5, 0.2, 0.1));
            Assert.True(KaeltemaschineTeillastkurve.IstLinear(KaeltemaschineTeillastkurve.Normieren(0, 1.02, 0).Value));
            Assert.False(KaeltemaschineTeillastkurve.IstLinear(k.Value));
        }

        /// <summary>
        /// Die Vorgabekurven in <see cref="KaelteFestwerte"/> sind genau die Ableitung aus den Typkennfeldern (Fachkonzept
        /// 4.4): je Regelung die normierte Kurve des Satzes, dessen g(0,5) der untere Median der Gruppe der Sätze mit Weg
        /// KURVE ist — und sie bestehen die Plausibilität ab dem unteren Prüflastgrad.
        /// </summary>
        [Fact]
        public void Die_Vorgabekurven_folgen_aus_den_Typkennfeldern_und_sind_plausibel()
        {
            var gruppen = KaeltemaschinenTypkennfelder.Lesen()
                .Select(t => t.Modell())
                .Where(m => m.Teillast_Weg == KaeltemaschineTeillastSchema.WEG_KURVE)
                .GroupBy(m => m.Verdichterregelung)
                .ToDictionary(g => g.Key, g => g.ToList());
            Assert.Equal(KaeltemaschineTeillastSchema.VERDICHTERREGELUNGEN.OrderBy(s => s), gruppen.Keys.OrderBy(s => s));
            foreach (string regelung in KaeltemaschineTeillastSchema.VERDICHTERREGELUNGEN)
            {
                List<KaeltemaschineModel> l = gruppen[regelung]
                    .OrderBy(m => G(m.Teillastkurve_a.Value, m.Teillastkurve_b.Value, m.Teillastkurve_c.Value)).ToList();
                KaeltemaschineModel median = l[(l.Count - 1) / 2];
                KaeltemaschineTeillastkurve.Kurve v = KaeltemaschineTeillastkurve.Vorgabekurve(regelung).Value;
                Assert.Equal(median.Teillastkurve_a.Value, v.A, 6);
                Assert.Equal(median.Teillastkurve_b.Value, v.B, 6);
                Assert.Equal(median.Teillastkurve_c.Value, v.C, 6);
                Assert.True(KaeltemaschineTeillastkurve.Bereich(v), regelung);
                Assert.True(KaeltemaschineStammCtrl.KurvePlausibel(v.A, v.B, v.C, 0.0), regelung);
                Assert.Equal(1.0, v.Volllast, 5);
            }
            // Die Folge der Regelungen: Drehzahlregelung hält den EER bei Teillast am besten.
            double gEinAus = G(KaelteFestwerte.VORGABEKURVE_EIN_AUS_A, KaelteFestwerte.VORGABEKURVE_EIN_AUS_B, KaelteFestwerte.VORGABEKURVE_EIN_AUS_C);
            double gStufen = G(KaelteFestwerte.VORGABEKURVE_STUFEN_A, KaelteFestwerte.VORGABEKURVE_STUFEN_B, KaelteFestwerte.VORGABEKURVE_STUFEN_C);
            double gDrehzahl = G(KaelteFestwerte.VORGABEKURVE_DREHZAHL_A, KaelteFestwerte.VORGABEKURVE_DREHZAHL_B, KaelteFestwerte.VORGABEKURVE_DREHZAHL_C);
            Assert.True(gDrehzahl > gEinAus && gDrehzahl > gStufen);
            Assert.Null(KaeltemaschineTeillastkurve.Vorgabekurve(null));
            Assert.Null(KaeltemaschineTeillastkurve.Vorgabekurve("UNBEKANNT"));
        }

        /// <summary>Kontrollwert Nenn-EER: bis 10 % kein Hinweis, darüber ein Hinweis — Pruefen lehnt nie ab.</summary>
        [Fact]
        public void Der_Kontrollwert_Nenn_EER_meldet_eine_Abweichung_als_Hinweis()
        {
            var m = new KaeltemaschineModel
            {
                Bezeichner = "Probe", Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_WASSER, Nenn_EER = 5.0,
                Kennlinie = new List<KaeltemaschineKenndatenModel>
                {
                    new KaeltemaschineKenndatenModel { Rueckkuehltemperatur = 25, Kaltwassertemperatur = 7, Kaelteleistung_kW = 100, EER = 6.0 },
                    new KaeltemaschineKenndatenModel { Rueckkuehltemperatur = 35, Kaltwassertemperatur = 7, Kaelteleistung_kW = 90, EER = 4.0 },
                }
            };
            // Am Nennpunkt (Kühlwasser 30 °C, Kaltwasser 7 °C) liegt das Kennfeld bei 5,0.
            Assert.Null(KaeltemaschineStammCtrl.NennEerHinweis(m));
            m.Nenn_EER = 5.45;
            Assert.Null(KaeltemaschineStammCtrl.NennEerHinweis(m));
            m.Nenn_EER = 5.6;
            string h = KaeltemaschineStammCtrl.NennEerHinweis(m);
            Assert.NotNull(h);
            Assert.Contains("5,60", h, StringComparison.Ordinal);
            Assert.Contains("5,00", h, StringComparison.Ordinal);
            Assert.Null(KaeltemaschineStammCtrl.Pruefen(m));
            m.Nenn_EER = 4.4;
            Assert.NotNull(KaeltemaschineStammCtrl.NennEerHinweis(m));
            m.Kennlinie.Clear();
            Assert.Null(KaeltemaschineStammCtrl.NennEerHinweis(m));
            m.Nenn_EER = null;
            Assert.Null(KaeltemaschineStammCtrl.NennEerHinweis(m));
        }

        private static double G(double a, double b, double c) => new KaeltemaschineTeillastkurve.Kurve(a, b, c).EerVerhaeltnis(0.5);
    }
}
