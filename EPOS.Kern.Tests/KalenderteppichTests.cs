using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Teppichbild eines Kalenders im Kern</b> (Entwurf KP2, Welle K4, Festlegung 8; Befund
    /// B12): die Rohreihe über 8 760 Stunden mit NaN für „aus" — bei <b>jeder</b> Größe — und je Tag
    /// die Quelle.
    ///
    /// <para><b>Die rote Probe von B12</b> steht vorn: <c>Auswerten</c> ersetzt „aus" bei der Lüftung
    /// und den Anteilen durch 0 — im Bild wäre die abgeschaltete Stunde von einer mit 0 1/h nicht zu
    /// unterscheiden. Die Rohreihe hält das Kennzeichen; <c>Auswerten</c> bleibt unberührt (der
    /// Datenweg des Laufs), und ohne „aus" ist die Rohreihe Stunde für Stunde dieselbe.</para>
    ///
    /// <para>Ohne Datenbank, ohne Uhr; das Bezugsjahr ist 2025, das Jahr des Laufs ohne Projekt
    /// (1. Januar ein Mittwoch, w₀ = 2).</para>
    /// </summary>
    public class KalenderteppichTests
    {
        private const int JAHR = 2025;

        /// <summary>Tag 0 … 364 des Gemeinjahres zu einem Datum.</summary>
        private static int Tag(int monat, int tagImMonat) => Feiertage.Gemeinjahrestag(monat, tagImMonat) - 1;

        /// <summary>
        /// Die Büro-Woche: Mo–Fr 7–18 Uhr der Tagwert, sonst der Nachtwert; Sa und So wahlweise
        /// „aus" (NaN) oder der Nachtwert.
        /// </summary>
        private static double[] Buerowoche(double tag, double nacht, bool wochenendeAus)
        {
            var w = new double[Kalenderwoche.WOCHENWERTE];
            for (int wt = 0; wt < 7; wt++)
                for (int s = 0; s < 24; s++)
                {
                    double v = wt < 5 && s >= 7 && s < 18 ? tag : nacht;
                    if (wt >= 5 && wochenendeAus) v = double.NaN;
                    w[Kalenderwoche.Stelle(wt, s)] = v;
                }
            return w;
        }

        /// <summary>
        /// Heizen „Büro": Standardwoche 20/16 °C, Wochenende 16 °C; Herbstferien 16 °C (Rang 200),
        /// Neujahr und Ostermontag wie Sonntag (Rang 100, 101), Heizperiode 01.10.–30.04. — außerhalb
        /// die Saison „aus" (Rang 900).
        /// </summary>
        private static Konditionierungskalender BueroHeizen(bool mitSaison = true)
        {
            var perioden = new List<Kalenderregel>
            {
                Kalenderregel.Feiertag(100, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR, Kalenderangabe.AlsWochentag(7)),
                Kalenderregel.Feiertag(101, "Ostermontag", DbWerte.KOND_FEIERTAG_OSTERMONTAG, Kalenderangabe.AlsWochentag(7)),
                Kalenderregel.Zeitraum(200, DbWerte.KOND_ART_FERIEN, "Herbstferien",
                                       Tag(10, 20) + 1, Tag(10, 31) + 1, Kalenderangabe.AusWert(16.0)),
            };
            if (mitSaison)
                perioden.Add(Kalenderregel.Zeitraum(Standardfahrplan.RANG_SAISON, DbWerte.KOND_ART_BETRIEBSPAUSE,
                                                    Standardfahrplan.BEZEICHNER_SAISON,
                                                    Tag(5, 1) + 1, Tag(9, 30) + 1, Kalenderangabe.Abgeschaltet));
            return new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                Kalenderangabe.AusWoche(Buerowoche(20.0, 16.0, false)), null, perioden);
        }

        // =============================================================================
        //  Die rote Probe von B12 — „aus" verschwindet bei Lüftung und Anteilen
        // =============================================================================

        /// <summary>
        /// <b>B12:</b> Die Lüftung „aus" am Wochenende wird in <c>Auswerten</c> zu 0 1/h — dieselbe
        /// Zahl wie eine gewollte Nullzelle. Die Rohreihe trägt dort NaN, die Nullzelle bleibt 0.
        /// </summary>
        [Fact]
        public void Lueftung_aus_bleibt_in_der_Rohreihe_NaN_und_wird_im_Lauf_0()
        {
            double[] woche = Buerowoche(2.0, 0.0, wochenendeAus: true);     // Nacht 0 1/h, Wochenende „aus"
            var k = new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                                 Kalenderangabe.AusWoche(woche), null, null);
            double[] lauf = k.Auswerten(2, JAHR);
            Kalenderteppich t = Kalenderteppich.Bilden(k, JAHR);

            int samstag = Tag(1, 4);                        // 04.01.2025 ist ein Samstag
            Assert.Equal(5, t.Wochentag(samstag));
            Assert.Equal(0.0, lauf[samstag * 24 + 10]);    // der Lauf: „aus" ist 0 1/h
            Assert.True(double.IsNaN(t.Wert(samstag, 10)), "Die Rohreihe verliert „aus“ nicht.");

            int montag = Tag(1, 6);
            Assert.Equal(0.0, t.Wert(montag, 3));          // die gewollte Nullzelle bleibt 0
            Assert.Equal(2.0, t.Wert(montag, 10));
        }

        /// <summary>Dasselbe für einen Anteil: Personen „aus" sind im Lauf 0, im Teppich NaN.</summary>
        [Fact]
        public void Anteil_aus_bleibt_NaN_und_die_Anzeige_rechnet_in_Prozent()
        {
            double[] woche = Buerowoche(0.8, 0.1, wochenendeAus: true);
            var k = new Konditionierungskalender(Konditionierungsgroesse.Personen,
                                                 Kalenderangabe.AusWoche(woche), 700.0, null);
            Kalenderteppich t = Kalenderteppich.Bilden(k, JAHR);
            double[] anzeige = t.Anzeigereihe();

            int sonntag = Tag(1, 5);
            Assert.True(double.IsNaN(anzeige[sonntag * 24 + 12]));
            Assert.Equal(80.0, anzeige[Tag(1, 6) * 24 + 9], 9);
            Assert.Equal(10.0, anzeige[Tag(1, 6) * 24 + 2], 9);
            Assert.Equal("%", Kalenderteppich.Einheit(Konditionierungsgroesse.Personen));
            Assert.Equal("1/h", Kalenderteppich.Einheit(Konditionierungsgroesse.Lueftung));
            Assert.Equal("°C", Kalenderteppich.Einheit(Konditionierungsgroesse.Kuehlsoll));
            Assert.Equal(100.0, Kalenderteppich.Anzeigefaktor(Konditionierungsgroesse.Geraete));
            Assert.Equal(1.0, Kalenderteppich.Anzeigefaktor(Konditionierungsgroesse.Heizsoll));
        }

        // =============================================================================
        //  Büro-Heizen mit Ferien, Feiertagen und Heizperiode
        // =============================================================================

        [Fact]
        public void Buero_Heizen_nennt_je_Tag_die_Quelle_der_Ebene()
        {
            Kalenderteppich t = Kalenderteppich.Bilden(BueroHeizen(), JAHR);
            Assert.Equal(JAHR, t.Bezugsjahr);
            Assert.Equal(2, t.WochentagDesErstenTags);                       // 01.01.2025: Mittwoch
            Assert.Equal(Kalenderteppich.TAGE, t.Quellen.Count);
            Assert.Equal(8760, t.Rohreihe.Count);

            // Montag, 06.01.: die Standardwoche - 7-18 Uhr 20 °C, sonst 16 °C.
            int montag = Tag(1, 6);
            Assert.Same(Teppichquelle.Standardwoche, t.Quellen[montag]);
            Assert.Equal(16.0, t.Wert(montag, 6));
            Assert.Equal(20.0, t.Wert(montag, 7));
            Assert.Equal(20.0, t.Wert(montag, 17));
            Assert.Equal(16.0, t.Wert(montag, 18));

            // Neujahr: Feiertag, wie Sonntag (ganztags 16 °C), obwohl der 01.01. ein Mittwoch ist.
            Teppichquelle neujahr = t.Quellen[0];
            Assert.Equal(Teppichquellart.Feiertag, neujahr.Art);
            Assert.Equal("Neujahr", neujahr.Bezeichner);
            Assert.Equal(DbWerte.KOND_ART_FEIERTAG, neujahr.Periodenart);
            Assert.Equal(16.0, t.Wert(0, 10));

            // Herbstferien 20.-31.10.: Ferien 16 °C.
            Teppichquelle ferien = t.Quellen[Tag(10, 22)];
            Assert.Equal(Teppichquellart.Ferien, ferien.Art);
            Assert.Equal("Herbstferien", ferien.Bezeichner);
            Assert.Equal(200, ferien.Rang);
            Assert.Equal(16.0, t.Wert(Tag(10, 22), 10));

            // Außerhalb der Heizperiode (01.05.-30.09.): die Saison „aus" - jede Stunde NaN.
            int juli = Tag(7, 1);
            Assert.Equal(Teppichquellart.Saison, t.Quellen[juli].Art);
            for (int s = 0; s < 24; s++) Assert.True(double.IsNaN(t.Wert(juli, s)));
            Assert.Equal(Teppichquellart.Saison, t.Quellen[Tag(5, 1)].Art);
            Assert.Same(Teppichquelle.Standardwoche, t.Quellen[Tag(4, 30)]);
            Assert.Same(Teppichquelle.Standardwoche, t.Quellen[Tag(10, 1)]);
        }

        /// <summary>
        /// Der Feiertag wandert mit dem Bezugsjahr: Ostermontag ist 2025 der 21.04., 2026 der 06.04.
        /// — der Kalender speichert die Regel, nicht den Tag (B13).
        /// </summary>
        [Fact]
        public void Der_Feiertag_folgt_dem_Bezugsjahr()
        {
            Kalenderteppich t25 = Kalenderteppich.Bilden(BueroHeizen(), 2025);
            Kalenderteppich t26 = Kalenderteppich.Bilden(BueroHeizen(), 2026);

            Assert.Equal("Ostermontag", t25.Quellen[Tag(4, 21)].Bezeichner);
            Assert.Same(Teppichquelle.Standardwoche, t25.Quellen[Tag(4, 6)]);
            Assert.Equal("Ostermontag", t26.Quellen[Tag(4, 6)].Bezeichner);
            Assert.Same(Teppichquelle.Standardwoche, t26.Quellen[Tag(4, 21)]);
            Assert.Equal(16.0, t25.Wert(Tag(4, 21), 10));                 // Montag, aber wie Sonntag
            Assert.Equal(3, t26.WochentagDesErstenTags);                  // 01.01.2026: Donnerstag
        }

        /// <summary>
        /// Eine Periode über den Jahreswechsel (21.12.–10.01.) enthält beide Enden — und
        /// schlägt an Neujahr die Feiertagsregel mit dem kleineren Rang.
        /// </summary>
        [Fact]
        public void Die_Periode_ueber_den_Jahreswechsel_enthaelt_beide_Enden()
        {
            var perioden = new List<Kalenderregel>
            {
                Kalenderregel.Feiertag(100, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR, Kalenderangabe.AlsWochentag(7)),
                Kalenderregel.Zeitraum(400, DbWerte.KOND_ART_ZEITRAUM, "Betriebsruhe",
                                       Tag(12, 21) + 1, Tag(1, 10) + 1, Kalenderangabe.Abgeschaltet),
            };
            var k = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(20.0),
                                                 null, perioden);
            Kalenderteppich t = Kalenderteppich.Bilden(k, JAHR);

            foreach (int d in new[] { Tag(12, 21), Tag(12, 31), 0, Tag(1, 10) })
            {
                Assert.Equal(Teppichquellart.Zeitraum, t.Quellen[d].Art);
                Assert.Equal("Betriebsruhe", t.Quellen[d].Bezeichner);
                Assert.True(double.IsNaN(t.Wert(d, 12)));
            }
            Assert.Same(Teppichquelle.Grundangabe, t.Quellen[Tag(1, 11)]);
            Assert.Same(Teppichquelle.Grundangabe, t.Quellen[Tag(12, 20)]);
            Assert.Equal(20.0, t.Wert(Tag(1, 11), 12));
        }

        // =============================================================================
        //  Der Rundlauf: ohne „aus" Stunde für Stunde Auswerten
        // =============================================================================

        /// <summary>
        /// <b>Ohne „aus" ist die Rohreihe <c>Auswerten</c></b> — bitgleich, Stunde für Stunde; mit
        /// „aus" steht NaN genau dort, wo <c>Auswerten</c> den Wert der Größe einsetzt.
        /// </summary>
        [Theory]
        [InlineData(2025, 2)]
        [InlineData(2024, 0)]
        [InlineData(2026, 5)]
        public void Ohne_aus_ist_die_Rohreihe_Auswerten_Stunde_fuer_Stunde(int jahr, int w0)
        {
            Konditionierungskalender ohneAus = BueroHeizen(mitSaison: false);
            Assert.False(ohneAus.TraegtAus());
            Kalenderteppich t = Kalenderteppich.Bilden(ohneAus, w0, jahr);
            double[] lauf = ohneAus.Auswerten(w0, jahr);
            for (int h = 0; h < 8760; h++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(lauf[h]), BitConverter.DoubleToInt64Bits(t.Rohreihe[h]));

            foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
            {
                double[] woche = Buerowoche(g == Konditionierungsgroesse.Heizsoll || g == Konditionierungsgroesse.Kuehlsoll ? 22.0 : 0.6,
                                            g == Konditionierungsgroesse.Heizsoll || g == Konditionierungsgroesse.Kuehlsoll ? 18.0 : 0.0,
                                            wochenendeAus: true);
                var k = new Konditionierungskalender(g, Kalenderangabe.AusWoche(woche),
                                                     Konditionierungsgroessen.HatNennwert(g) ? 500.0 : (double?)null, null);
                Kalenderteppich tg = Kalenderteppich.Bilden(k, w0, jahr);
                double[] lg = k.Auswerten(w0, jahr);
                double aus = Konditionierungsgroessen.AusWert(g);
                for (int h = 0; h < 8760; h++)
                {
                    if (double.IsNaN(tg.Rohreihe[h]))
                        Assert.Equal(BitConverter.DoubleToInt64Bits(aus), BitConverter.DoubleToInt64Bits(lg[h]));
                    else
                        Assert.Equal(BitConverter.DoubleToInt64Bits(lg[h]), BitConverter.DoubleToInt64Bits(tg.Rohreihe[h]));
                }
            }
        }

        // =============================================================================
        //  Bezugsjahr und Grenzen
        // =============================================================================

        /// <summary>
        /// Im Katalog gibt es kein Feiertagsjahr, das Raster ist das Rückfallraster ohne Projekt (E115: 1. Januar =
        /// Sonntag, <see cref="ProfilBedarf.WOCHENTAG_ALTKONVENTION"/>) — keine Uhr, keine Datenbank.
        /// </summary>
        [Fact]
        public void Das_Raster_im_Katalog_ist_das_Rueckfallraster()
        {
            Assert.Null(Konditionierungdatenweg.Bezugsjahr(0));
            Assert.Null(Konditionierungdatenweg.Bezugsjahr(-1));
            Assert.Equal(new Gemeinjahrkalender(ProfilBedarf.WOCHENTAG_ALTKONVENTION), Konditionierungdatenweg.Raster(0));
            Assert.Equal(Konditionierungdatenweg.Rueckfallraster, Konditionierungdatenweg.Raster(-1));
            Assert.False(Konditionierungdatenweg.Raster(0).MitJahr);
            Assert.Equal(2, Kalenderteppich.WochentagDesErstenTagsIm(2025));
            Assert.Equal(0, Kalenderteppich.WochentagDesErstenTagsIm(2024));
            Assert.Equal(GebaeudeModellEingang.WochentagDesErstenTags(KlimakalenderGemeinsam.WochenendmaskeBilden(2031)),
                         Kalenderteppich.WochentagDesErstenTagsIm(2031));
        }

        [Fact]
        public void Grundangabe_Wert_und_aus_und_benannte_Grenzen()
        {
            var wert = new Konditionierungskalender(Konditionierungsgroesse.Lueftung, Kalenderangabe.AusWert(0.5), null, null);
            Kalenderteppich tw = Kalenderteppich.Bilden(wert, JAHR);
            Assert.All(tw.Quellen, q => Assert.Same(Teppichquelle.Grundangabe, q));
            Assert.All(tw.Rohreihe, v => Assert.Equal(0.5, v));

            var aus = new Konditionierungskalender(Konditionierungsgroesse.Kuehlsoll, Kalenderangabe.Abgeschaltet, null, null);
            Assert.All(Kalenderteppich.Bilden(aus, JAHR).Rohreihe, v => Assert.True(double.IsNaN(v)));

            Assert.Throws<ArgumentNullException>(() => Kalenderteppich.Bilden(null, 0, JAHR));
            Assert.Throws<ArgumentOutOfRangeException>(() => Kalenderteppich.Bilden(wert, 7, JAHR));
            Assert.Throws<ArgumentOutOfRangeException>(() => Kalenderteppich.Bilden(wert, -1, JAHR));
        }

        /// <summary>Zwei Quellen derselben Periode sind gleich; die Betriebspause ohne Saisonrang bleibt eine Betriebspause.</summary>
        [Fact]
        public void Die_Quelle_ist_wertgleich_und_trennt_Saison_und_Betriebspause()
        {
            Kalenderregel r = Kalenderregel.Zeitraum(500, DbWerte.KOND_ART_BETRIEBSPAUSE, "Betriebsferien", 200, 210,
                                                     Kalenderangabe.Abgeschaltet);
            Assert.Equal(Teppichquelle.AusPeriode(r), Teppichquelle.AusPeriode(r));
            Assert.Equal(Teppichquellart.Betriebspause, Teppichquelle.AusPeriode(r).Art);
            Assert.NotEqual(Teppichquelle.Standardwoche, Teppichquelle.Grundangabe);
            Assert.Throws<ArgumentNullException>(() => Teppichquelle.AusPeriode(null));
        }
    }
}
