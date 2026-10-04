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
    }
}
