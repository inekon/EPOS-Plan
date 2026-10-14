using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Rechenproben der Teil-Freikühlung in Reihe ohne Datenbank</b> (K-F3; Entwurf Split/VRF/Rückkühlwerk 5.5): die
    /// Mischstunde gegen die Handrechnung, freie Kühlung deckt alles, der Abstand verfehlt (gleich der Maschine in
    /// <c>PARALLEL</c>), die Schwelle mit Zahlenrand, der Rücklauf auf AK3 gegen den Spreizungsweg, der Ventilatorstrom
    /// statt des EER-Ersatzes (<c>PARALLEL</c> und <c>REIHE</c>) und die Energieprobe Kälte — Teil-Freikühlung plus
    /// Verdichter gleich Kälte der Stunde, auch über die Kältekaskade.
    /// </summary>
    public class RueckkuehlwerkReiheTests
    {
        private static long Bits(double d) => BitConverter.DoubleToInt64Bits(d);

        /// <summary>
        /// Eine Kältemaschine (Nenn 90 kW, Kaltwasser 7 °C) mit Kennlinie über die Rückkühltemperatur 20…40 °C; unter 20 °C
        /// liest sie am Rand: 100 kW, EER 6.
        /// </summary>
        private static Kaeltemaschine Maschine(double hilfsstromKw = 0.0)
        {
            var punkte = new List<(double, double, double?, double?)>
            {
                (20.0, 7.0, 6.0, 100.0), (30.0, 7.0, 4.5, 90.0), (40.0, 7.0, 3.0, 80.0),
            };
            return new Kaeltemaschine
            {
                Bezeichner = "KM", Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER, NennleistungKw = 90,
                Kaltwassertemperatur = 7.0, HilfsstromRueckkuehlungKw = hilfsstromKw,
                Kennlinie = new KaeltemaschinenKennlinie(punkte),
            };
        }

        /// <summary>Ein trockenes Rückkühlwerk mit Annäherung 5 K in der gewählten Schaltung.</summary>
        private static RueckkuehlwerkModel Modell(string schaltung, double? ventilatorKw = null, double? nennKw = null) =>
            new RueckkuehlwerkModel
            {
                Annaeherung_Nenn_K = 5.0, Freikuehlung_Schaltung = schaltung, Ventilator_Nenn_kW = ventilatorKw,
                Nennleistung_kW = nennKw,
            };

        private static Kaeltemaschine Angesetzt(RueckkuehlwerkModel m, double[] aussen, double hilfsstromKw = 0.0)
        {
            Kaeltemaschine k = Maschine(hilfsstromKw);
            k.RueckkuehlwerkSetzen(Rueckkuehlwerk.AusModell(m));
            k.Rueckkuehltemperatur_stuendlich = k.RueckkuehltemperaturenBilden(aussen, null, out _);
            return k;
        }

        [Fact]
        public void Reihe_wird_gerechnet_und_meldet_kein_Merkmal_mehr()
        {
            Rueckkuehlwerk r = Rueckkuehlwerk.AusModell(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE));
            Assert.True(r.Reihe);
            Assert.Empty(r.NichtGerechnet);
            Assert.False(Rueckkuehlwerk.AusModell(Modell(RueckkuehlwerkSchema.SCHALTUNG_PARALLEL)).Reihe);
            Assert.False(Rueckkuehlwerk.AusModell(Modell(null)).Reihe);
        }

        /// <summary>
        /// Handrechnung (Spreizung konstant, Kaltwasserstrom folgt der Last): Außen 4 °C, Fluid 9 °C; Rücklauf 7 + 5 = 12 °C,
        /// Schwelle 12 − 3 = 9 °C erreicht. Anteil (12 − 9) / (12 − 7) = 0,6; Last 80 → Vorkühlung 48, Verdichter 32 bei EER 6
        /// (Rand) = 5,333 kWh; EER-Ersatz 48 / 15 = 3,2 kWh. Hilfsstrom 3 kW: Verdichter 3 · 32/100, Vorkühlung 3 · 48/90.
        /// </summary>
        [Fact]
        public void Mischstunde_gegen_die_Handrechnung()
        {
            Kaeltemaschine k = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE), new[] { 4.0 }, hilfsstromKw: 3.0);
            Assert.Equal(9.0, k.Rueckkuehltemperatur_stuendlich[0], 12);
            KaeltemaschinenStunde s = k.Stunde(0, 80.0, 7.0);
            Assert.False(s.FreieKuehlung);
            Assert.Equal(80.0, s.KaelteKwh, 12);
            Assert.Equal(48.0, s.TeilFreiKwh, 12);
            Assert.Equal(32.0, s.VerdichterKaelteKwh, 12);
            Assert.Equal(32.0 / 6.0 + 48.0 / KaelteFestwerte.FREIE_KUEHLUNG_EER, s.VerdichterKwh, 12);
            Assert.Equal(3.0 * 32.0 / 100.0 + 3.0 * 48.0 / 90.0, s.HilfsstromKwh, 12);
            Assert.True(s.MitRueckkuehlwerk);
            Assert.Equal(80.0 + 32.0 / 6.0, s.Rueckkuehlung.AbwaermeKwh, 12);
            Assert.Equal(Rueckkuehlwerk.TeilFreikuehlungKwh(80.0, 7.0, 12.0, 9.0, 90.0), s.TeilFreiKwh, 12);
        }

        /// <summary>
        /// Teillast 40 %: Der Kaltwasserstrom folgt der Last, der Vorkühler deckt den Temperaturanteil, nicht die ganze Last.
        /// Spreizung 10 K: Vorlauf 7, Rücklauf 17, Fluid 13 (Außen 8) → Anteil (17 − 13) / 10 = 0,4; Last 36 (40 % von 90)
        /// → Vorkühlung 14,4. Mit dem Nenn-Massenstrom (90 · 4 / 10 = 36) wäre es fälschlich die ganze Last.
        /// </summary>
        [Fact]
        public void Teillast_deckt_den_Temperaturanteil_nicht_die_ganze_Last()
        {
            Kaeltemaschine k = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE), new[] { 8.0 });
            k.ReiheSpreizungK = 10.0;
            KaeltemaschinenStunde s = k.Stunde(0, 36.0, 7.0);
            Assert.False(s.FreieKuehlung);
            Assert.Equal(0.4 * 36.0, s.TeilFreiKwh, 12);
            Assert.Equal(0.6 * 36.0, s.VerdichterKaelteKwh, 12);
            Assert.Equal(0.4 * 36.0, Rueckkuehlwerk.TeilFreikuehlungKwh(36.0, 7.0, 17.0, 13.0, 1000.0), 12);
            // Der Anteil hängt nicht an der Last: Volllast 90 → 36.
            Assert.Equal(0.4 * 90.0, Rueckkuehlwerk.TeilFreikuehlungKwh(90.0, 7.0, 17.0, 13.0, 1000.0), 12);
        }

        [Fact]
        public void Volllast_Vorkuehlung_und_Verdichter()
        {
            Kaeltemaschine k = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE), new[] { 4.0 });
            KaeltemaschinenStunde s = k.Stunde(0, 90.0, 7.0);
            Assert.Equal(90.0, s.KaelteKwh, 12);
            Assert.Equal(54.0, s.TeilFreiKwh, 12);
            Assert.Equal(36.0, s.VerdichterKaelteKwh, 12);
        }

        [Fact]
        public void Fluid_am_Vorlauf_kuehlt_ganz_frei()
        {
            // Außen 2 °C, Fluid 7 °C = Vorlauf: Anteil 1, die ganze Last frei (begrenzt nur durch den Rückkühler).
            Kaeltemaschine k = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE), new[] { 2.0 });
            KaeltemaschinenStunde s = k.Stunde(0, 80.0, 7.0);
            Assert.True(s.FreieKuehlung);
            Assert.Equal(80.0, s.KaelteKwh, 12);
            Assert.Equal(0.0, s.TeilFreiKwh);
            Assert.Equal(80.0 / KaelteFestwerte.FREIE_KUEHLUNG_EER, s.VerdichterKwh, 12);
            // Fluid unter dem Vorlauf: der Anteil bleibt bei 1.
            Assert.Equal(80.0, Rueckkuehlwerk.TeilFreikuehlungKwh(80.0, 7.0, 12.0, 3.0, 1000.0), 12);
        }

        [Fact]
        public void Rueckkuehlerleistung_begrenzt()
        {
            // Nennleistung 30 kW: Anteil 0,6 von 80 = 48, begrenzt auf 30; 50 über den Verdichter.
            Kaeltemaschine g = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE, nennKw: 30.0), new[] { 4.0 });
            KaeltemaschinenStunde t = g.Stunde(0, 80.0, 7.0);
            Assert.Equal(30.0, t.TeilFreiKwh, 12);
            Assert.Equal(50.0, t.VerdichterKaelteKwh, 12);
            // Ganz frei, aber über der Rückkühlerleistung: 30 frei, der Rest über den Verdichter.
            Kaeltemaschine f = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE, nennKw: 30.0), new[] { 2.0 });
            KaeltemaschinenStunde u = f.Stunde(0, 40.0, 7.0);
            Assert.Equal(30.0, u.TeilFreiKwh, 12);
            Assert.Equal(10.0, u.VerdichterKaelteKwh, 12);
        }

        [Fact]
        public void Abstand_verfehlt_rechnet_wie_die_Maschine_in_Parallel()
        {
            var aussen = new[] { 4.5 };    // Fluid 9,5 °C > 12 − 3
            Kaeltemaschine reihe = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE), aussen, 3.0);
            Kaeltemaschine parallel = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_PARALLEL), aussen, 3.0);
            KaeltemaschinenStunde a = reihe.Stunde(0, 50.0, 7.0), b = parallel.Stunde(0, 50.0, 7.0);
            Assert.False(a.FreieKuehlung);
            Assert.Equal(0.0, a.TeilFreiKwh);
            Assert.Equal(Bits(b.KaelteKwh), Bits(a.KaelteKwh));
            Assert.Equal(Bits(b.VerdichterKwh), Bits(a.VerdichterKwh));
            Assert.Equal(Bits(b.HilfsstromKwh), Bits(a.HilfsstromKwh));
            Assert.Equal(0.0, Rueckkuehlwerk.TeilFreikuehlungKwh(50.0, 7.0, 12.0, 9.5, 1000.0));
            // θ_FK ≥ θ_R − 3 K: 0, auch bei einem Fluid zwischen Rücklauf − 3 K und Rücklauf.
            Assert.Equal(0.0, Rueckkuehlwerk.TeilFreikuehlungKwh(40.0, 7.0, 12.0, 10.0, 1000.0));
        }

        [Fact]
        public void Schwelle_traegt_den_Zahlenrand()
        {
            Assert.True(Rueckkuehlwerk.TeilFreikuehlungGreift(12.0, 9.0));
            Assert.True(Rueckkuehlwerk.TeilFreikuehlungGreift(12.0, 9.0 + 1e-12));
            Assert.False(Rueckkuehlwerk.TeilFreikuehlungGreift(12.0, 9.0 + 1e-6));
            Assert.False(Rueckkuehlwerk.TeilFreikuehlungGreift(double.NaN, 5.0));
            // Spreizung der Stunde ≤ 0 (am Rand): keine Teil-Freikühlung.
            Assert.Equal(0.0, Rueckkuehlwerk.TeilFreikuehlungKwh(40.0, 7.0, 7.0, 2.0, 1000.0));
            Assert.Equal(0.0, Rueckkuehlwerk.TeilFreikuehlungKwh(40.0, 7.0, 7.0 + 1e-12, 2.0, 1000.0));
            Assert.Equal(0.0, Rueckkuehlwerk.TeilFreikuehlungKwh(40.0, 8.0, 7.0, 2.0, 1000.0));
        }

        [Fact]
        public void Ruecklauf_auf_AK3_gegen_den_Spreizungsweg()
        {
            Kaeltemaschine k = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE), new[] { 4.0 });
            // Spreizungsweg (NaN): Rücklauf 12 °C → Anteil 3/5, 48 von 80.
            Assert.Equal(48.0, k.Stunde(0, 80.0, 7.0, double.NaN).TeilFreiKwh, 12);
            // AK3: gerechneter Rücklauf 13,5 °C, gerechnete Spreizung 6,5 K → Anteil 4,5/6,5.
            Assert.Equal(80.0 * 4.5 / 6.5, k.Stunde(0, 80.0, 7.0, 13.5).TeilFreiKwh, 12);
            // AK3: Rücklauf 12,5 °C → Anteil 3,5/5,5.
            Assert.Equal(80.0 * 3.5 / 5.5, k.Stunde(0, 80.0, 7.0, 12.5).TeilFreiKwh, 12);
            // AK3: Rücklauf 11 °C → 11 − 3 K < Fluid 9 °C: greift nicht.
            Assert.Equal(0.0, k.Stunde(0, 80.0, 7.0, 11.0).TeilFreiKwh);
            // Ein Rücklauf unter dem Vorlauf zählt als Vorlauf (Spreizung 0: greift nicht).
            Assert.Equal(0.0, k.Stunde(0, 80.0, 7.0, 5.0).TeilFreiKwh);
            // Gepflegte Spreizung 8 K: Rücklauf 15 °C → Anteil 6/8, 60 von 80.
            k.ReiheSpreizungK = 8.0;
            Assert.Equal(60.0, k.Stunde(0, 80.0, 7.0).TeilFreiKwh, 12);
        }

        [Fact]
        public void Ventilator_ersetzt_den_EER_Ersatz_parallel()
        {
            var aussen = new[] { -5.0 };   // Fluid 0 °C ≤ 7 − 3: parallel ganz frei
            Kaeltemaschine ohne = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_PARALLEL), aussen);
            KaeltemaschinenStunde a = ohne.Stunde(0, 30.0, 7.0);
            Assert.True(a.FreieKuehlung);
            Assert.Equal(30.0 / KaelteFestwerte.FREIE_KUEHLUNG_EER, a.VerdichterKwh, 12);

            Kaeltemaschine mit = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_PARALLEL, ventilatorKw: 2.0, nennKw: 150.0), aussen, 5.0);
            KaeltemaschinenStunde b = mit.Stunde(0, 30.0, 7.0);
            Assert.True(b.FreieKuehlung);
            Assert.Equal(30.0, b.KaelteKwh, 12);
            Assert.Equal(0.0, b.VerdichterKwh);
            Assert.Equal(2.0 * 30.0 / 150.0, b.HilfsstromKwh, 12);
            Assert.Equal(b.HilfsstromKwh, b.StromKwh);
        }

        [Fact]
        public void Ventilator_ersetzt_den_EER_Ersatz_in_Reihe()
        {
            Kaeltemaschine k = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE, ventilatorKw: 2.0, nennKw: 150.0),
                                         new[] { 4.0 }, 5.0);
            KaeltemaschinenStunde s = k.Stunde(0, 80.0, 7.0);
            // Rückkühlerleistung 150 kW begrenzt nicht: 48 frei, 32 über den Verdichter.
            Assert.Equal(48.0, s.TeilFreiKwh, 12);
            Assert.Equal(32.0 / 6.0, s.VerdichterKwh, 12);
            double x = (80.0 + 32.0 / 6.0) / 150.0;
            Assert.Equal(2.0 * x, s.HilfsstromKwh, 12);
            Assert.Equal(s.Rueckkuehlung.VentilatorKwh, s.HilfsstromKwh);

            // Ganz frei in Reihe (Fluid am Vorlauf): allein der Ventilator.
            Kaeltemaschine g = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE, ventilatorKw: 2.0, nennKw: 150.0),
                                         new[] { 2.0 }, 5.0);
            KaeltemaschinenStunde f = g.Stunde(0, 40.0, 7.0);
            Assert.True(f.FreieKuehlung);
            Assert.Equal(0.0, f.VerdichterKwh);
            Assert.Equal(2.0 * 40.0 / 150.0, f.HilfsstromKwh, 12);
        }

        [Theory]
        [InlineData(10.0)]
        [InlineData(40.0)]
        [InlineData(80.0)]
        [InlineData(95.0)]
        [InlineData(130.0)]
        public void Energieprobe_Kaelte_Teil_Freikuehlung_plus_Verdichter(double last)
        {
            Kaeltemaschine k = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE), new[] { 4.0 });
            KaeltemaschinenStunde s = k.Stunde(0, last, 7.0);
            Assert.False(s.FreieKuehlung);
            Assert.Equal(0.6 * last, s.TeilFreiKwh, 12);
            Assert.Equal(s.KaelteKwh, s.TeilFreiKwh + s.VerdichterKaelteKwh, 12);
            // Der Verdichter (100 kW am Rand) trägt 0,4 der Last: die ganze Last ist gedeckt.
            Assert.Equal(last, s.KaelteKwh, 12);
        }

        [Fact]
        public void Energieprobe_ueber_die_Kaeltekaskade()
        {
            const int n = 48;
            double[] aussen = Enumerable.Range(0, n).Select(h => -4.0 + 12.0 * (h % 24) / 23.0).ToArray();
            double[] bedarf = Enumerable.Range(0, n).Select(h => 20.0 + 3.0 * (h % 24)).ToArray();
            Kaeltemaschine k = Angesetzt(Modell(RueckkuehlwerkSchema.SCHALTUNG_REIHE), aussen);
            var kaskade = new Kaeltekaskade { Kuehltage = Enumerable.Repeat(true, 365).ToArray() };
            var e = new Kaelteerzeuger { Bezeichner = "KM", Modulindex = -1, Maschine = k };
            kaskade.Erzeuger.Add(e);
            kaskade.Beginnen(true);
            for (int h = 0; h < n; h++) kaskade.StundeRechnen(h, bedarf[h]);

            Assert.True(e.RkTeilFreiStunden > 0);
            Assert.True(e.StundenFreieKuehlung > 0);
            Assert.True(e.RkTeilFreiKwh > 0);
            Assert.Equal(bedarf.Sum(), kaskade.DeckungGesamtKwh + kaskade.RestGesamtKwh, 9);
            Assert.Equal(e.KaelteGesamtKwh, kaskade.DeckungGesamtKwh, 9);
            // Freie Kühlung (ganze Stunden) + Teil-Freikühlung + Verdichterkälte = Kälte der Maschine.
            double verdichter = 0.0;
            for (int h = 0; h < n; h++)
            {
                KaeltemaschinenStunde s = k.Stunde(h, bedarf[h], 7.0);
                if (!s.FreieKuehlung) verdichter += s.VerdichterKaelteKwh;
            }
            Assert.Equal(e.KaelteGesamtKwh, e.KaelteFreiKwh + e.RkTeilFreiKwh + verdichter, 9);
        }
    }
}
