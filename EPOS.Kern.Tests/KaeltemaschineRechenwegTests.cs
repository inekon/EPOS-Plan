using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Rechenproben der Kältemaschine ohne Datenbank</b> (KU3-2; Kühlkonzept 10.2): Kennlinie bilinear
    /// und am Rand, Rückkühltemperatur je Art, Mindestteillast, freie Kühlung an und aus, Kältestrom mit
    /// und ohne Hilfsstrom der Rückkühlung, die Reihenfolge in der Kältekaskade.
    /// </summary>
    public sealed class KaeltemaschineRechenwegTests
    {
        private const double EPS = 1e-9;

        /// <summary>Das luftgekühlte Beispielgerät der Saat (50 kW, 25 % Mindestteillast, ohne Hilfsstrom).</summary>
        private static KaeltemaschineModel Saatgeraet(int index)
        {
            KaeltemaschineSchema.Beispielgeraet b = KaeltemaschineSchema.SAAT[index];
            var m = new KaeltemaschineModel
            {
                Id = index + 1,
                Bezeichner = b.Bezeichner,
                Nennkaelteleistung_kW = b.Nennkaelteleistung,
                Nenn_EER = b.NennEer,
                Rueckkuehlart = b.Rueckkuehlart,
                Mindestteillast_Prozent = b.Mindestteillast,
                Hilfsstrom_Rueckkuehlung_kW = b.HilfsstromRueckkuehlung,
                Kaltwasser_Vorlauf_Min = b.KaltwasserVorlaufMin,
            };
            foreach (var k in b.Kennlinie)
                m.Kennlinie.Add(new KaeltemaschineKenndatenModel
                {
                    Rueckkuehltemperatur = k.Rueckkuehl, Kaltwassertemperatur = k.Kaltwasser,
                    EER = k.Eer, Kaelteleistung_kW = k.Leistung
                });
            return m;
        }

        private static Kaeltemaschine Maschine(int index, params double[] rueckkuehl)
        {
            Kaeltemaschine k = Kaeltemaschine.AusModell(Saatgeraet(index), out _);
            var r = new double[Kaeltekaskade.STUNDEN];
            for (int h = 0; h < r.Length; h++) r[h] = h < rueckkuehl.Length ? rueckkuehl[h] : 35.0;
            k.Rueckkuehltemperatur_stuendlich = r;
            return k;
        }

        [Fact]
        public void Kennlinie_ist_bilinear_im_Raster()
        {
            Kaeltemaschine k = Maschine(0);
            KaeltemaschinenPunkt p = k.Kennlinie.Auswerten(30.0, 9.0);
            // Kaltwasser 6: 55/50 -> 52,5; Kaltwasser 12: 62/57 -> 59,5; Mitte 56,0.
            Assert.Equal(56.0, p.LeistungKw, 9);
            // EER: 3,8/3,0 -> 3,4; 4,5/3,6 -> 4,05; Mitte 3,725.
            Assert.Equal(3.725, p.Eer, 9);
            Assert.False(p.Randwert);

            KaeltemaschinenPunkt s = k.Kennlinie.Auswerten(35.0, 6.0);
            Assert.Equal(50.0, s.LeistungKw, 9);
            Assert.Equal(3.0, s.Eer, 9);
            Assert.False(s.Randwert);
        }

        [Fact]
        public void Ausserhalb_der_Stuetzstellen_gilt_der_Randwert_mit_Meldung()
        {
            Kaeltemaschine k = Maschine(0);
            KaeltemaschinenPunkt oben = k.Kennlinie.Auswerten(50.0, 6.0);
            Assert.Equal(44.0, oben.LeistungKw, 9);
            Assert.Equal(2.5, oben.Eer, 9);
            Assert.True(oben.Randwert);

            KaeltemaschinenPunkt unten = k.Kennlinie.Auswerten(20.0, 3.0);
            Assert.Equal(55.0, unten.LeistungKw, 9);
            Assert.Equal(3.8, unten.Eer, 9);
            Assert.True(unten.Randwert);

            Assert.Equal(25.0, k.Kennlinie.RueckkuehlMin, 9);
            Assert.Equal(45.0, k.Kennlinie.RueckkuehlMax, 9);
        }

        [Fact]
        public void Kennlinie_ohne_EER_oder_Leistung_ist_leer()
        {
            var k = new KaeltemaschinenKennlinie(new[] { (25.0, 6.0, (double?)null, (double?)50.0) });
            Assert.True(k.Leer);
            Assert.Equal(0.0, k.Auswerten(25, 6).LeistungKw);
        }

        [Fact]
        public void Rueckkuehltemperatur_je_Art()
        {
            Assert.Equal(25.0, Kaeltemaschine.Rueckkuehltemperatur(KaeltemaschineSchema.RUECKKUEHLART_LUFT, 20.0, null, out bool o1), 9);
            Assert.False(o1);
            Assert.Equal(30.0, Kaeltemaschine.Rueckkuehltemperatur(KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER, 20.0, 50.0, out _), 9);
            Assert.Equal(KaelteFestwerte.RUECKKUEHLTEMPERATUR_WASSER_C,
                         Kaeltemaschine.Rueckkuehltemperatur(KaeltemaschineSchema.RUECKKUEHLART_WASSER, -5.0, null, out _), 9);

            // Nasskühler mit Feuchte: Feuchtkugel nach Stull (20 °C, 50 % -> rund 13,7 °C) + 5 K.
            double nass = Kaeltemaschine.Rueckkuehltemperatur(KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER, 20.0, 50.0, out bool o2);
            Assert.False(o2);
            Assert.InRange(nass, 18.5, 19.0);
            Assert.True(Kaeltemaschine.Feuchtkugeltemperatur(20.0, 100.0) <= 20.0);

            // Nasskühler ohne Feuchte: Außentemperatur - 3 K, mit Hinweis.
            Assert.Equal(17.0, Kaeltemaschine.Rueckkuehltemperatur(KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER, 20.0, null, out bool o3), 9);
            Assert.True(o3);

            double[] reihe = Kaeltemaschine.RueckkuehltemperaturenBilden(KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER,
                new[] { 20.0, 25.0 }, new[] { 50.0, double.NaN }, out int ohne);
            Assert.Equal(1, ohne);
            Assert.Equal(22.0, reihe[1], 9);
        }

        [Fact]
        public void Kaltwasser_ist_die_kleinste_Stuetzstelle_mindestens_die_Grenze()
        {
            KaeltemaschineModel m = Saatgeraet(0);
            Kaeltemaschine k = Kaeltemaschine.AusModell(m, out bool angehoben);
            Assert.Equal(6.0, k.Kaltwassertemperatur, 9);
            Assert.False(angehoben);

            m.Kaltwasser_Vorlauf_Min = 8.0;
            k = Kaeltemaschine.AusModell(m, out angehoben);
            Assert.Equal(8.0, k.Kaltwassertemperatur, 9);
            Assert.True(angehoben);
            Assert.Equal(0.25, k.Mindestteillast, 9);
            Assert.Equal(50.0, k.NennleistungKw, 9);
        }

        [Fact]
        public void Mindestteillast_taktet_und_die_Leistungsgrenze_haelt()
        {
            Kaeltemaschine k = Maschine(0, 35.0, 35.0);
            // Pmin = 25 % von 50 kW = 12,5 kW; bei 35/6 liefert die Kennlinie 50 kW mit EER 3,0.
            KaeltemaschinenStunde klein = k.Stunde(0, 5.0);
            Assert.True(klein.Takt);
            Assert.Equal(5.0, klein.KaelteKwh, 9);
            Assert.Equal(5.0 / 3.0, klein.VerdichterKwh, 9);
            Assert.Equal(0.0, klein.HilfsstromKwh, 9);

            KaeltemaschinenStunde gross = k.Stunde(1, 80.0);
            Assert.False(gross.Takt);
            Assert.Equal(50.0, gross.KaelteKwh, 9);
            Assert.Equal(50.0, gross.KapazitaetKw, 9);
            Assert.Equal(50.0 / 3.0, gross.StromKwh, 9);
        }

        [Fact]
        public void Kaeltestrom_mit_Hilfsstrom_der_Rueckkuehlung()
        {
            // Trockenkühler 200 kW, Hilfsstrom 6 kW; Rückkühlung 35 °C, Kaltwasser 6 °C: 200 kW, EER 4,0.
            Kaeltemaschine k = Maschine(1, 35.0);
            KaeltemaschinenStunde s = k.Stunde(0, 100.0);
            Assert.False(s.FreieKuehlung);
            Assert.Equal(100.0, s.KaelteKwh, 9);
            Assert.Equal(25.0, s.VerdichterKwh, 9);
            Assert.Equal(6.0 * 100.0 / 200.0, s.HilfsstromKwh, 9);
            Assert.Equal(28.0, s.StromKwh, 9);

            k.HilfsstromRueckkuehlungKw = 0.0;   // NULL: im EER enthalten
            Assert.Equal(25.0, k.Stunde(0, 100.0).StromKwh, 9);
        }

        [Fact]
        public void Freie_Kuehlung_an_und_aus()
        {
            // Trockenkühler, Kaltwasser 6 °C: frei bis Rückkühlung 3 °C.
            Kaeltemaschine k = Maschine(1, 3.0, 3.5);
            Assert.True(k.FreieKuehlungMoeglich);
            Assert.True(k.FreieKuehlung(0));
            Assert.False(k.FreieKuehlung(1));

            KaeltemaschinenStunde frei = k.Stunde(0, 40.0);
            Assert.True(frei.FreieKuehlung);
            Assert.Equal(40.0, frei.KaelteKwh, 9);
            Assert.Equal(40.0 / KaelteFestwerte.FREIE_KUEHLUNG_EER, frei.VerdichterKwh, 9);
            Assert.Equal(6.0 * 40.0 / 200.0, frei.HilfsstromKwh, 9);

            KaeltemaschinenStunde verdichter = k.Stunde(1, 40.0);
            Assert.False(verdichter.FreieKuehlung);
            Assert.True(verdichter.Randwert);   // 3,5 °C liegt unter der kleinsten Rückkühlstelle 25 °C

            // Luftgekühlt kennt keine freie Kühlung, auch bei -30 °C nicht.
            Kaeltemaschine luft = Maschine(0, -30.0);
            Assert.False(luft.FreieKuehlungMoeglich);
            Assert.False(luft.Stunde(0, 10.0).FreieKuehlung);
        }

        [Fact]
        public void Kaskade_freie_Kuehlung_zuerst_sonst_Listenreihenfolge()
        {
            Kaeltemaschine luft = Maschine(0, 35.0, 35.0);
            Kaeltemaschine trocken = Maschine(1, 0.0, 30.0);   // Stunde 0 frei, Stunde 1 nicht
            var a = new Kaelteerzeuger { Bezeichner = "Luft", Modulindex = -1, Maschine = luft };
            var b = new Kaelteerzeuger { Bezeichner = "Trocken", Modulindex = -1, Maschine = trocken };
            var kaskade = new Kaeltekaskade { Erzeuger = { a, b } };
            var bedarf = new double[Kaeltekaskade.STUNDEN];
            bedarf[0] = 30.0;
            bedarf[1] = 30.0;
            bedarf[2] = 300.0;

            kaskade.Rechnen(bedarf, false);

            Assert.Equal(0.0, a.Kaelte_stuendlich[0], 9);    // frei: der Trockenkühler zuerst
            Assert.Equal(30.0, b.Kaelte_stuendlich[0], 9);
            Assert.Equal(30.0, a.Kaelte_stuendlich[1], 9);   // nicht frei: die Liste
            Assert.Equal(0.0, b.Kaelte_stuendlich[1], 9);
            Assert.Equal(1, b.StundenFreieKuehlung);
            Assert.Equal(30.0, b.KaelteFreiKwh, 9);

            // Stunde 2: 50 kW Luft + 200 kW Trocken (Rückkühlung 35 °C) - 50 kWh bleiben offen.
            Assert.Equal(50.0, a.Kaelte_stuendlich[2], 9);
            Assert.Equal(200.0, b.Kaelte_stuendlich[2], 9);
            Assert.Equal(50.0, kaskade.Rest_stuendlich[2], 9);
            Assert.Equal(50.0, kaskade.RestGesamtKwh, 9);
            Assert.Equal(1, b.StundenLeistungsgrenze);
            Assert.Equal(50.0, b.OffenAnLeistungsgrenzeKwh, 9);

            double strom = Enumerable.Range(0, 3).Sum(h => a.Strom_stuendlich[h] + b.Strom_stuendlich[h]);
            Assert.Equal(strom, kaskade.StromGesamtKwh, 9);
            Assert.Equal(kaskade.StromGesamtKwh, kaskade.Stromverbrauch_Kuehlung_stuendlich.Sum(), 9);
            Assert.Equal(310.0, kaskade.DeckungGesamtKwh, 9);
            Assert.True(kaskade.HilfsstromGesamtKwh > 0);
        }

        // =====================================================================
        //  KM3-E2-b: Teillast, Takten und Randweg im Rechenweg (Fachkonzept Teillast und Takten 3, 5, 8.1)
        // =====================================================================

        /// <summary>Kurve des Zahlenbeispiels 3.2 (a 0,10, b 0,60, c 0,30, x_u 0,2) mit der Mindestteillast der Saat (25 %).</summary>
        private static Kaeltemaschinenteillast Kurve(string randweg = null)
            => Kaeltemaschinenteillast.Bilden(KaeltemaschineTeillastSchema.WEG_KURVE, 0.10, 0.60, 0.30, 0.2, null, null, randweg, 0.25);

        private static Kaeltemaschinenteillast Weg(string weg, string randweg = null)
            => Kaeltemaschinenteillast.Bilden(weg, null, null, null, null, null, null, randweg, 0.25);

        [Fact]
        public void Kurve_Verdichterstrom_je_Stunde_wie_die_Teillastklasse()
        {
            Kaeltemaschine k = Maschine(0, 35.0, 35.0);
            k.Teillast = Kurve();
            Assert.True(k.TeillastWirksam);
            // 35/6: Q_av 50 kW, EER 3,0. Last 25 kWh, PLR 0,5 -> E(0,5) = 0,475; P_el = 50/3 * 0,475.
            KaeltemaschinenStunde s = k.Stunde(0, 25.0);
            KaeltemaschinenTeillaststunde t = k.Teillast.Stunde(25.0, 50.0, 3.0, 50.0, 0.25);
            Assert.Equal(t.VerdichterKwh, s.VerdichterKwh, 12);
            Assert.Equal(50.0 / 3.0 * 0.475, s.VerdichterKwh, 9);
            Assert.Equal(25.0, s.KaelteKwh, 9);
            Assert.Equal(0.5, s.Lastgrad, 12);
            Assert.False(s.Takt);
            Assert.Equal(0.0, s.MehrstromKwh);
            Assert.Equal(1, s.Laufend);
            Assert.Equal(50.0, s.KapazitaetKw, 9);
            Assert.NotEqual(25.0 / 3.0, s.VerdichterKwh, 6);   // der Bestand rechnete linear
        }

        [Fact]
        public void Taktstunde_liefert_Mehrstrom_und_Start()
        {
            Kaeltemaschine k = Maschine(0, 35.0);
            k.Teillast = Kurve();
            // P_min 12,5 kW; Last 5 kWh taktet.
            KaeltemaschinenStunde s = k.Stunde(0, 5.0);
            KaeltemaschinenTeillaststunde t = k.Teillast.Stunde(5.0, 50.0, 3.0, 50.0, 0.25);
            Assert.True(s.Takt);
            Assert.True(s.MehrstromKwh > 0);
            Assert.Equal(t.MehrstromKwh, s.MehrstromKwh, 12);
            Assert.Equal(t.VerdichterKwh, s.VerdichterKwh, 12);
            Assert.Equal(Waermepumpentakt.StartsImTakt(5.0, 12.5), s.Starts);
            Assert.True(s.Starts >= 1);

            // Bestand: dieselbe Stunde taktet nur als Kennzeichen, ohne Mehrstrom.
            Kaeltemaschine b = Maschine(0, 35.0);
            KaeltemaschinenStunde sb = b.Stunde(0, 5.0);
            Assert.True(sb.Takt);
            Assert.Equal(0.0, sb.MehrstromKwh);
            Assert.Equal(5.0 / 3.0, sb.VerdichterKwh, 12);
        }

        [Fact]
        public void Randweg_Guetegrad_setzt_eine_Randstunde_fort_und_zaehlt_sie()
        {
            // Rückkühlung 50 °C über dem Rand 45 °C: Randwert 44 kW, EER 2,5 bei 45/6.
            Kaeltemaschine k = Maschine(0, 50.0, 35.0);
            k.Teillast = Weg(null, KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD);
            Assert.False(k.TeillastWirksam);
            Assert.True(k.GuetegradWirksam);
            double eer = KaeltemaschinenRand.EerFortgesetzt(2.5, 6.0, 45.0, 6.0, 50.0);
            Assert.True(eer < 2.5);
            KaeltemaschinenStunde s = k.Stunde(0, 30.0);
            Assert.True(s.Randwert);
            Assert.True(s.Extrapoliert);
            Assert.Equal(44.0, s.KapazitaetKw, 9);
            Assert.Equal(30.0 / eer, s.VerdichterKwh, 12);
            // Im Kennfeld: wie der Bestand.
            KaeltemaschinenStunde innen = k.Stunde(1, 30.0);
            Assert.False(innen.Extrapoliert);
            Assert.Equal(10.0, innen.VerdichterKwh, 12);

            var e = new Kaelteerzeuger { Bezeichner = "KM", Modulindex = -1, Maschine = k };
            var kaskade = new Kaeltekaskade { Erzeuger = { e } };
            var bedarf = new double[Kaeltekaskade.STUNDEN];
            bedarf[0] = 30.0;
            bedarf[1] = 30.0;
            kaskade.Rechnen(bedarf, false);
            Assert.Equal(1, e.StundenExtrapoliert);
            Assert.Equal(1, e.StundenRandwert);
            Assert.Equal(30.0 / eer + 10.0, e.StromGesamtKwh, 9);
            Assert.Equal(0, e.Starts);   // ohne Teillast_Weg keine Startzählung
        }

        [Fact]
        public void Folgeschaltung_zweier_Maschinen_nur_mit_Weg()
        {
            // Drei Maschinen je 50 kW; Last 60 kWh: mit Weg laufen zwei je 30 kWh, ohne Weg drei je 20 kWh.
            Kaeltemaschine mit = Maschine(0, 35.0, 35.0);
            mit.Anzahl = 3;
            mit.Teillast = Weg(KaeltemaschineTeillastSchema.WEG_LINEAR);
            KaeltemaschinenStunde s = mit.Stunde(0, 60.0);
            Assert.Equal(2, s.Laufend);
            Assert.Equal(60.0, s.KaelteKwh, 9);
            Assert.Equal(0.6, s.Lastgrad, 12);
            Assert.Equal(20.0, s.VerdichterKwh, 9);
            Assert.Equal(150.0, s.KapazitaetKw, 9);

            // Schwachlast 15 kWh: ohne Weg takten drei Maschinen (je 5 < 12,5), mit Weg läuft eine ohne Takt.
            Kaeltemaschine ohne = Maschine(0, 35.0);
            ohne.Anzahl = 3;
            Assert.True(ohne.Stunde(0, 15.0).Takt);
            KaeltemaschinenStunde f = mit.Stunde(1, 15.0);
            Assert.Equal(1, f.Laufend);
            Assert.False(f.Takt);
            Assert.Equal(0.3, f.Lastgrad, 12);
        }

        [Fact]
        public void Ohne_Weg_rechnet_das_Jahr_Zeichen_fuer_Zeichen_wie_ohne_Einbau()
        {
            var rk = Enumerable.Range(0, Kaeltekaskade.STUNDEN).Select(h => 20.0 + 30.0 * (h % 24) / 23.0).ToArray();
            var bedarf = Enumerable.Range(0, Kaeltekaskade.STUNDEN).Select(h => (h % 7) * 9.5).ToArray();
            Kaelteerzeuger Lauf(Kaeltemaschinenteillast t)
            {
                Kaeltemaschine k = Maschine(0);
                k.Rueckkuehltemperatur_stuendlich = rk;
                k.Anzahl = 2;
                k.Teillast = t;
                var e = new Kaelteerzeuger { Bezeichner = "KM", Modulindex = -1, Maschine = k, Hilfsstromanteil = 0.05 };
                new Kaeltekaskade { Erzeuger = { e } }.Rechnen(bedarf, false);
                return e;
            }
            Kaelteerzeuger alt = Lauf(null);
            Kaelteerzeuger bestand = Lauf(Weg(null));
            Kaelteerzeuger randwert = Lauf(Weg(null, KaeltemaschineTeillastSchema.RANDWEG_RANDWERT));
            Assert.False(Kaeltemaschine.AusModell(Saatgeraet(0), out _).MitWeg);
            foreach (Kaelteerzeuger e in new[] { bestand, randwert })
            {
                Assert.False(e.Maschine.MitWeg);
                for (int h = 0; h < Kaeltekaskade.STUNDEN; h++)
                {
                    Assert.Equal(BitConverter.DoubleToInt64Bits(alt.Strom_stuendlich[h]), BitConverter.DoubleToInt64Bits(e.Strom_stuendlich[h]));
                    Assert.Equal(BitConverter.DoubleToInt64Bits(alt.Kaelte_stuendlich[h]), BitConverter.DoubleToInt64Bits(e.Kaelte_stuendlich[h]));
                }
                Assert.Equal(alt.StundenTakt, e.StundenTakt);
                Assert.Equal(alt.StundenRandwert, e.StundenRandwert);
                Assert.Equal(0, e.Starts);
                Assert.Equal(0.0, e.TaktstromKwh);
                Assert.Equal(0, e.StundenExtrapoliert);
            }
        }

        [Fact]
        public void Kaskade_bucht_Mehrstrom_vor_dem_Hilfsstromzuschlag_und_Starts_je_Laufphase()
        {
            Kaeltemaschine k = Maschine(0);
            k.Teillast = Kurve();
            var e = new Kaelteerzeuger { Bezeichner = "KM", Modulindex = -1, Maschine = k, Hilfsstromanteil = 0.1 };
            var kaskade = new Kaeltekaskade { Erzeuger = { e } };
            var bedarf = new double[Kaeltekaskade.STUNDEN];
            bedarf[0] = 30.0; bedarf[1] = 30.0; bedarf[2] = 50.0;   // eine Laufphase: ein Start
            bedarf[4] = 30.0;                                       // neue Laufphase: ein Start
            bedarf[5] = 5.0;                                        // Taktstunde: Starts im Takt
            kaskade.Rechnen(bedarf, false);

            KaeltemaschinenStunde takt = k.Stunde(5, 5.0);
            Assert.Equal(takt.VerdichterKwh * 1.1, e.Strom_stuendlich[5], 12);
            Assert.Equal(takt.MehrstromKwh, e.Taktstrom_stuendlich[5], 12);
            Assert.Equal(takt.MehrstromKwh, e.TaktstromKwh, 12);
            Assert.Equal(2 + takt.Starts, e.Starts);
            Assert.Equal(1, e.StundenTakt);
            Assert.Equal(3, e.StundenTeillast);   // 0,6 in Stunde 0, 1, 4; Volllast in Stunde 2 zählt nicht
            double gewichtet = (30.0 * 0.6 * 3 + 50.0 * 1.0 + 5.0 * 0.1) / 145.0;
            Assert.Equal(gewichtet, e.LastgradMittel, 12);
            Assert.Equal(e.Strom_stuendlich.Sum(), kaskade.StromGesamtKwh, 9);
        }

        [Fact]
        public void Kaeltespeicher_senkt_die_Taktstunden()
        {
            int Taktstunden(bool mitSpeicher, out double taktstrom)
            {
                Kaeltemaschine k = Maschine(0);
                k.Teillast = Kurve();
                var e = new Kaelteerzeuger { Bezeichner = "KM", Modulindex = -1, Maschine = k };
                var kaskade = new Kaeltekaskade { Erzeuger = { e }, Kuehltage = Enumerable.Repeat(true, Kaeltekaskade.TAGE).ToArray() };
                if (mitSpeicher)
                {
                    var sp = new SimulationPufferspeicher { Bezeichner = "Kaltwasser", ID_Pufferspeicher = 7 };
                    sp.InitKaelte(2000, 6, 12, 0);
                    sp.SchwelleEin = 0.10;
                    sp.SchwelleAus = 0.95;
                    kaskade.Speicher.Add(sp);
                }
                var bedarf = new double[Kaeltekaskade.STUNDEN];
                for (int h = 0; h < 48; h++) bedarf[h] = 5.0;
                kaskade.Rechnen(bedarf, false);
                Assert.Equal(0.0, kaskade.RestGesamtKwh, 9);
                taktstrom = e.TaktstromKwh;
                return e.StundenTakt;
            }
            int ohne = Taktstunden(false, out double stromOhne);
            int mit = Taktstunden(true, out double stromMit);
            Assert.Equal(48, ohne);
            Assert.True(mit < ohne, "mit Speicher " + mit + ", ohne " + ohne);
            Assert.True(stromMit < stromOhne);
        }
    }
}
