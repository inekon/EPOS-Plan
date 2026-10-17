using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// KK2 (Entwurf KK 2.3–2.6, Festlegungen 9–11): die Kälteerzeuger am Stundenvorlauf — Kühlkennlinie mit gebrochenem
    /// Vorlauf, Kältemaschine je Stunde, Kälteschranke mit Vorlauf-Argument, Kältekaskade am Stundenvorlauf, kältester
    /// verlangter Vorlauf, Speicherregel. Jede Wirkung am festen Vorlauf bitgleich zum Weg ohne Stundenvorlauf.
    /// </summary>
    public sealed class KaelteStundenvorlaufTests
    {
        private const int H = 8760;
        private static readonly int[] VORLAEUFE = { 7, 12, 18 };
        private static readonly int[] TEMPERATUREN = { 20, 25, 30, 35 };

        private readonly ITestOutputHelper _aus;

        public KaelteStundenvorlaufTests(ITestOutputHelper aus) { _aus = aus; }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        /// <summary>
        /// Eine Kühlkennlinie mit drei Vorläufen, zwei Laststufen, gleichen und abweichenden Dubletten und einem Block in
        /// Heizlage (35 °C) — alles, was <see cref="Kuehlkennlinie.Bilden(IEnumerable{KuehlkennlinienZeile}, int?, bool)"/> entscheidet.
        /// </summary>
        internal static List<KuehlkennlinienZeile> Zeilen(double skalaKw = 10.0)
        {
            var z = new List<KuehlkennlinienZeile>();
            int id = 1;
            foreach (int v in VORLAEUFE)
                foreach (int t in TEMPERATUREN)
                {
                    double pk = skalaKw * (1.0 + 0.03 * (v - 7)) * (1.0 - 0.01 * (t - 20));
                    double eer = 3.0 + 0.12 * (v - 7) - 0.05 * (t - 20);
                    z.Add(new KuehlkennlinienZeile(id++, v, t, eer, pk, 100));
                    z.Add(new KuehlkennlinienZeile(id++, v, t, eer, pk, 100));           // gleiche Dublette
                    z.Add(new KuehlkennlinienZeile(id++, v, t, 9.9, 1.0, 50));          // kleinere Laststufe
                }
            z.Add(new KuehlkennlinienZeile(id++, 12, 25, 1.1, 2.2, 100));                // abweichende Dublette
            foreach (int t in new[] { 0, 10 }) z.Add(new KuehlkennlinienZeile(id++, 35, t, 2.0, 5.0, 100)); // Heizlage
            return z;
        }

        private static double[] Reihe(Func<int, double> f) => Enumerable.Range(0, H).Select(f).ToArray();

        // ------------------------------------------------------------------------------------------
        //  Kühlkennlinie mit gebrochenem Vorlauf
        // ------------------------------------------------------------------------------------------

        [Fact]
        public void Schar_am_ganzzahligen_Vorlauf_bitgleich_zu_Bilden()
        {
            List<KuehlkennlinienZeile> z = Zeilen();
            var schar = new KuehlkennlinienSchar(z);
            double[] temperaturen = { 10.0, 20.0, 22.5, 30.0, 35.0, 38.0 };
            int geprueft = 0;
            for (int v = 0; v <= 40; v++)
                foreach (bool ex in new[] { false, true })
                {
                    Kuehlkennlinie soll = Kuehlkennlinie.Bilden(z, v, true);
                    Kuehlkennlinie ist = schar.Kennlinie(v);
                    Assert.Equal(soll.Vorlauf, ist.Vorlauf);
                    Assert.Equal(soll.Interpoliert, ist.Interpoliert);
                    Assert.Equal(Bits(soll.GewichtOben), Bits(ist.GewichtOben));
                    Assert.Equal(soll.Befund, ist.Befund);
                    Assert.Equal(soll.Rechenbar, ist.Rechenbar);
                    foreach (double t in temperaturen)
                    {
                        KennlinienPunkt a = soll.Auswerten(t, ex), b = schar.Auswerten(v, t, ex);
                        Assert.Equal(Bits(a.Pkuehl), Bits(b.Pkuehl));
                        Assert.Equal(Bits(a.Eer), Bits(b.Eer));
                        Assert.Equal(a.Lage, b.Lage);
                        geprueft++;
                    }
                }
            // Ohne Vorlauf: der kleinste Stützwert wie Kuehl_Vorlauf NULL.
            Assert.Equal(Kuehlkennlinie.Bilden(z, null, true).Vorlauf, schar.Kennlinie(double.NaN).Vorlauf);
            Assert.Equal(7.0, schar.VorlaufMinC);
            Assert.True(new KuehlkennlinienSchar(null).Leer);
            _aus.WriteLine("Schar bitgleich: {0} Punkte", geprueft);
        }

        [Fact]
        public void Schar_stetig_ueber_den_Vorlauf_waermer_mehr_Leistung_und_EER()
        {
            var schar = new KuehlkennlinienSchar(Zeilen());
            const double T = 27.5;
            double sprungMax = 0.0;
            // Stetig an den inneren Stützstellen: links- und rechtsseitiger Grenzwert treffen den Wert der Stützstelle.
            foreach (int s in new[] { 12 })
                foreach (double d in new[] { -1e-9, 1e-9 })
                {
                    sprungMax = Math.Max(sprungMax, Math.Abs(schar.Auswerten(s + d, T, true).Pkuehl - schar.Auswerten(s, T, true).Pkuehl));
                    sprungMax = Math.Max(sprungMax, Math.Abs(schar.Auswerten(s + d, T, true).Eer - schar.Auswerten(s, T, true).Eer));
                }
            Assert.True(sprungMax < 1e-8, "Sprung " + sprungMax);
            // Monoton im Arbeitsbereich: ein wärmerer Vorlauf liefert mehr Kälte mit besserem EER.
            KennlinienPunkt vorher = schar.Auswerten(7.0, T, true);
            for (double v = 7.25; v <= 18.0; v += 0.25)
            {
                KennlinienPunkt p = schar.Auswerten(v, T, true);
                Assert.True(p.Pkuehl > vorher.Pkuehl && p.Eer > vorher.Eer, "Vorlauf " + v);
                vorher = p;
            }
            // Gebrochener Vorlauf: linear zwischen den einschließenden Vorläufen.
            KennlinienPunkt u = schar.Auswerten(12.0, T, true), o = schar.Auswerten(18.0, T, true), m = schar.Auswerten(14.4, T, true);
            Assert.Equal(u.Pkuehl + 0.4 * (o.Pkuehl - u.Pkuehl), m.Pkuehl, 12);
            Assert.Equal(u.Eer + 0.4 * (o.Eer - u.Eer), m.Eer, 12);
            // Außerhalb der Randwert: kälter als 7 °C rechnet 7 °C, wärmer als 18 °C rechnet 18 °C (35 °C ist Heizlage).
            Assert.Equal(Bits(schar.Auswerten(7.0, T, true).Pkuehl), Bits(schar.Auswerten(4.3, T, true).Pkuehl));
            Assert.Equal(Bits(schar.Auswerten(18.0, T, true).Pkuehl), Bits(schar.Auswerten(21.7, T, true).Pkuehl));
            _aus.WriteLine("Sprung an der Stützstelle {0:E2}; P(7) {1:0.000} kW, P(14,4) {2:0.000} kW, P(18) {3:0.000} kW",
                           sprungMax, u.Pkuehl, m.Pkuehl, o.Pkuehl);
        }

        // ------------------------------------------------------------------------------------------
        //  Erzeuger
        // ------------------------------------------------------------------------------------------

        private static Kaelteerzeuger Waermepumpe(int vorlauf, bool freieKuehlung = true)
        {
            List<KuehlkennlinienZeile> z = Zeilen();
            return new Kaelteerzeuger
            {
                Bezeichner = "WP", Modulindex = 0,
                Kennlinie = Kuehlkennlinie.Bilden(z, vorlauf, true),
                Schar = new KuehlkennlinienSchar(z),
                KuehlVorlaufC = vorlauf,
                Quelltemperatur = Reihe(h => 8.0 + 22.0 * (0.5 - 0.5 * Math.Cos(2.0 * Math.PI * h / H)) + 3.0 * Math.Sin(2.0 * Math.PI * h / 24.0)),
                Zeitanteil = Reihe(h => (h % 24) < 3 ? 0.5 : 1.0),
                FreieKuehlungSole = freieKuehlung,
                FreieKuehlungLeistungKw = 4.0,
                Hilfsstromanteil = 0.05,
                Mindestanteil = 0.3,
                Cd = 0.25,
            };
        }

        private static Kaeltemaschine Maschine(double kaltwasser, double min)
            => new Kaeltemaschine
            {
                Bezeichner = "KM", NennleistungKw = 8.0, Mindestteillast = 0.2, HilfsstromRueckkuehlungKw = 0.3,
                Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER,
                Kaltwassertemperatur = kaltwasser, KaltwasserVorlaufMinC = min,
                Kennlinie = new KaeltemaschinenKennlinie(new[]
                {
                    (20.0, 6.0, (double?)4.0, (double?)8.0), (40.0, 6.0, (double?)2.5, (double?)6.5),
                    (20.0, 14.0, (double?)5.5, (double?)9.5), (40.0, 14.0, (double?)3.4, (double?)7.8),
                }),
                Rueckkuehltemperatur_stuendlich = Reihe(h => -2.0 + 36.0 * (0.5 - 0.5 * Math.Cos(2.0 * Math.PI * h / H)) + 4.0 * Math.Sin(2.0 * Math.PI * h / 24.0)),
            };

        [Fact]
        public void Kaeltemaschine_am_festen_Vorlauf_bitgleich_und_Untergrenze()
        {
            Kaeltemaschine m = Maschine(10.0, 7.0);
            m.Anzahl = 2;
            int frei = 0;
            for (int h = 0; h < H; h += 7)
            {
                double last = 3.0 + (h % 13);
                KaeltemaschinenStunde a = m.Stunde(h, last), b = m.Stunde(h, last, m.KaltwasserAmVorlauf(double.NaN));
                Assert.Equal(Bits(a.KaelteKwh), Bits(b.KaelteKwh));
                Assert.Equal(Bits(a.VerdichterKwh), Bits(b.VerdichterKwh));
                Assert.Equal(Bits(a.HilfsstromKwh), Bits(b.HilfsstromKwh));
                Assert.Equal(Bits(a.KapazitaetKw), Bits(b.KapazitaetKw));
                Assert.Equal(m.FreieKuehlung(h), m.FreieKuehlung(h, 10.0));
                if (a.FreieKuehlung) frei++;
            }
            Assert.True(frei > 0);
            // Untergrenze Kaltwasser_Vorlauf_Min: kälter wird nie gefahren.
            Assert.Equal(7.0, m.KaltwasserAmVorlauf(4.0));
            Assert.Equal(12.5, m.KaltwasserAmVorlauf(12.5));
            // Ein wärmerer Kaltwasservorlauf: mehr Leistung, weniger Verdichterstrom je Kälte.
            int h0 = 4000;
            Assert.False(m.FreieKuehlung(h0, 14.0));
            KaeltemaschinenStunde kalt = m.Stunde(h0, 100.0, 6.0), warm = m.Stunde(h0, 100.0, 14.0);
            Assert.True(warm.KapazitaetKw > kalt.KapazitaetKw);
            Assert.True(warm.VerdichterKwh / warm.KaelteKwh < kalt.VerdichterKwh / kalt.KaelteKwh);
            // AusModell: Kaltwasser_Vorlauf_Min, ohne ihn die kleinste Kaltwasser-Stützstelle.
            var modell = new KaeltemaschineModel { Id = 1, Nennkaelteleistung_kW = 8.0 };
            modell.Kennlinie.Add(new KaeltemaschineKenndatenModel { Rueckkuehltemperatur = 30, Kaltwassertemperatur = 9, EER = 3, Kaelteleistung_kW = 8 });
            modell.Kennlinie.Add(new KaeltemaschineKenndatenModel { Rueckkuehltemperatur = 30, Kaltwassertemperatur = 15, EER = 4, Kaelteleistung_kW = 9 });
            Assert.Equal(9.0, Kaeltemaschine.AusModell(modell, out _).KaltwasserVorlaufMinC);
            modell.Kaltwasser_Vorlauf_Min = 11.0;
            Assert.Equal(11.0, Kaeltemaschine.AusModell(modell, out _).KaltwasserVorlaufMinC);
        }

        [Fact]
        public void Kaelteschranke_am_festen_Vorlauf_bitgleich()
        {
            Kaelteerzeuger wp = Waermepumpe(12);
            var km = new Kaelteerzeuger { Bezeichner = "KM", Modulindex = -1, Maschine = Maschine(12.0, 6.0) };
            var heiz = new double[H];
            for (int h = 0; h < H; h++) heiz[h] = (h % 5) * 0.2;
            var wpk = new WaermepumpeKaeltekapazitaet(wp, null, Reihe(h => (h % 24) == 3 ? 1.0 : 0.0).Select(x => x > 0).ToArray(), true)
            {
                Heizzeitanteil = h => heiz[h],
            };
            var schranke = new Kaelteschranke(new IKaelteerzeugerkapazitaet[] { wpk, new KaeltemaschineKapazitaet(km) }, null, 12.0)
            {
                Prozesskaelte = h => (h % 11) * 0.1,
            };
            int geprueft = 0, gleitendMehr = 0;
            for (int h = 0; h < H; h += 3)
            {
                var vorrang = new Stundenvorrang(0, 0, 0, 0);
                Kaeltestundenangebot a = schranke.Angebot(h, vorrang), b = schranke.Angebot(h, vorrang, 12.0);
                Assert.Equal(Bits(a.LeistungKw), Bits(b.LeistungKw));
                Assert.Equal(Bits(a.ErzeugerKw), Bits(b.ErzeugerKw));
                Assert.Equal(Bits(a.KuehlVorlaufC), Bits(b.KuehlVorlaufC));
                Assert.Equal(a.Grund, b.Grund);
                Assert.Equal(Bits(a.LeistungKw), Bits(schranke.Angebot(h, vorrang, double.NaN).LeistungKw));
                if (schranke.Angebot(h, vorrang, 16.0).ErzeugerKw > a.ErzeugerKw) gleitendMehr++;
                geprueft++;
            }
            Assert.True(gleitendMehr > geprueft / 2, "wärmerer Vorlauf ohne Wirkung: " + gleitendMehr);
            // Untergrenze der Wärmepumpe: kälter als die kleinste Stützstelle rechnet sie an ihr.
            Assert.Equal(7.0, wp.VorlaufAmErzeuger(3.0));
            Assert.Equal(Bits(wpk.AbfragenAmVorlauf(4000, 7.0).KapazitaetKw), Bits(wpk.AbfragenAmVorlauf(4000, 3.0).KapazitaetKw));
            _aus.WriteLine("Schranke bitgleich in {0} Stunden; am Vorlauf 16 °C mehr Angebot in {1}", geprueft, gleitendMehr);
        }

        private static Kaeltekaskade Kaskade(double speicherVorlauf)
        {
            var k = new Kaeltekaskade { Kuehltage = Enumerable.Repeat(true, 365).ToArray() };
            k.Erzeuger.Add(new Kaelteerzeuger { Bezeichner = "KM", Modulindex = -1, Maschine = Maschine(12.0, 6.0), Hilfsstromanteil = 0.02 });
            k.Erzeuger.Add(Waermepumpe(12));
            if (!double.IsNaN(speicherVorlauf))
            {
                var sp = new SimulationPufferspeicher { Bezeichner = "Kaltwasser" };
                sp.InitKaelte(3000, (int)speicherVorlauf, (int)speicherVorlauf + 6, 0.5);
                k.Speicher.Add(sp);
            }
            return k;
        }

        private static double[] Bedarf() => Reihe(h => Math.Max(0.0, 12.0 * Math.Sin(Math.PI * (h % 24) / 24.0) * (0.5 - 0.5 * Math.Cos(2.0 * Math.PI * h / H)) - 1.0));

        private static void Gleich(Kaeltekaskade a, Kaeltekaskade b)
        {
            for (int h = 0; h < H; h++)
            {
                Assert.Equal(Bits(a.Deckung_stuendlich[h]), Bits(b.Deckung_stuendlich[h]));
                Assert.Equal(Bits(a.Stromverbrauch_Kuehlung_stuendlich[h]), Bits(b.Stromverbrauch_Kuehlung_stuendlich[h]));
                Assert.Equal(Bits(a.Rest_stuendlich[h]), Bits(b.Rest_stuendlich[h]));
                for (int i = 0; i < a.Erzeuger.Count; i++)
                {
                    Assert.Equal(Bits(a.Erzeuger[i].Kaelte_stuendlich[h]), Bits(b.Erzeuger[i].Kaelte_stuendlich[h]));
                    Assert.Equal(Bits(a.Erzeuger[i].Strom_stuendlich[h]), Bits(b.Erzeuger[i].Strom_stuendlich[h]));
                }
            }
            Assert.Equal(Bits(a.StromGesamtKwh), Bits(b.StromGesamtKwh));
            Assert.Equal(Bits(a.SpeicherladungKwh), Bits(b.SpeicherladungKwh));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(12.0)]
        public void Kaeltekaskade_am_festen_Vorlauf_bitgleich(double speicherVorlauf)
        {
            double[] bedarf = Bedarf();
            Kaeltekaskade fest = Kaskade(speicherVorlauf), stunde = Kaskade(speicherVorlauf);
            fest.Rechnen(bedarf, true);
            stunde.Beginnen(true);
            for (int h = 0; h < H; h++) stunde.StundeRechnen(h, bedarf[h], 12.0);
            stunde.Abschliessen();
            Gleich(fest, stunde);
            Assert.True(fest.DeckungGesamtKwh > 0.0);
            _aus.WriteLine("Speicher {0}: Deckung {1:0.0} kWh, Strom {2:0.0} kWh bitgleich", speicherVorlauf, fest.DeckungGesamtKwh, fest.StromGesamtKwh);
        }

        [Fact]
        public void Kaeltekaskade_am_waermeren_Vorlauf_weniger_Strom_Speicherregel_kappt()
        {
            double[] bedarf = Bedarf();
            Kaeltekaskade fest = Kaskade(double.NaN), warm = Kaskade(double.NaN);
            fest.Rechnen(bedarf, true);
            warm.Beginnen(true);
            for (int h = 0; h < H; h++) warm.StundeRechnen(h, bedarf[h], 16.0);
            warm.Abschliessen();
            Assert.True(warm.StromGesamtKwh < fest.StromGesamtKwh);
            Assert.True(warm.RestGesamtKwh <= fest.RestGesamtKwh + 1e-9);

            // Speicherregel (Festlegung 10): mit Kältespeicher am 9-°C-Vorlauf fährt der Erzeuger nie wärmer als 9 °C —
            // verlangt 16 °C rechnet wie verlangt 9 °C.
            Kaeltekaskade s16 = Kaskade(9.0), s9 = Kaskade(9.0);
            s16.Beginnen(true);
            s9.Beginnen(true);
            for (int h = 0; h < H; h++)
            {
                s16.StundeRechnen(h, bedarf[h], 16.0);
                s9.StundeRechnen(h, bedarf[h], 9.0);
            }
            s16.Abschliessen();
            s9.Abschliessen();
            Gleich(s16, s9);
            _aus.WriteLine("Strom fest 12 °C {0:0.0} kWh, gleitend 16 °C {1:0.0} kWh; mit Speicher 9 °C {2:0.0} kWh",
                           fest.StromGesamtKwh, warm.StromGesamtKwh, s16.StromGesamtKwh);
        }

        [Fact]
        public void Kaeltester_verlangter_Vorlauf_und_Speicherregel_der_Schranke()
        {
            Assert.Equal(15.5, Kaeltevorlauf.KaeltesterVerlangter(new[] { 18.0, double.NaN, 15.5, 20.0 }));
            Assert.True(double.IsNaN(Kaeltevorlauf.KaeltesterVerlangter(new[] { double.NaN })));
            Assert.True(double.IsNaN(Kaeltevorlauf.KaeltesterVerlangter(null)));
            Assert.Equal(9.0, Kaeltevorlauf.MitSpeicherregel(16.0, 9.0));
            Assert.Equal(8.0, Kaeltevorlauf.MitSpeicherregel(8.0, 9.0));
            Assert.Equal(16.0, Kaeltevorlauf.MitSpeicherregel(16.0, double.NaN));

            var a = new SimulationPufferspeicher();
            a.InitKaelte(2000, 10, 16, 0.0);
            var b = new SimulationPufferspeicher();
            b.InitKaelte(2000, 8, 14, 0.0);
            Assert.Equal(8.0, Kaeltevorlauf.SpeicherVorlauf(new[] { a, b }));
            Kaelteerzeuger wp = Waermepumpe(12);
            var schranke = new Kaelteschranke(new IKaelteerzeugerkapazitaet[] { new WaermepumpeKaeltekapazitaet(wp, null, null, true) },
                                              new Kaeltespeicherleser(new[] { a, b }), 12.0);
            Assert.Equal(8.0, schranke.SpeicherVorlaufC);
            Kaeltestundenangebot k = schranke.Angebot(4000, new Stundenvorrang(0, 0, 0, 0), 16.0);
            Assert.Equal(8.0, k.KuehlVorlaufC);
            Assert.Equal(Bits(schranke.Angebot(4000, new Stundenvorrang(0, 0, 0, 0), 8.0).ErzeugerKw), Bits(k.ErzeugerKw));
            Assert.True(double.IsNaN(new Kaelteschranke(null, null, 12.0).SpeicherVorlaufC));
        }
    }
}
