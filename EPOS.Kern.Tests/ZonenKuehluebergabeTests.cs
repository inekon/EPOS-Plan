using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Kühlübergabe je Zone im Mehrzonenweg</b> (Entwurf KK, Welle KZ1; Festlegungen 14–17; E106 Q-KK-4 (b), Q-KK-7 (a))
    /// — ohne Datenbank: Übernahme der drei Kühlspalten der Zone, die Kaskade, der Kühlkreis je Gebäude am gemeinsamen
    /// festen Vorlauf, der massenstromgewichtete Rücklauf, ideale Zonen, das Vorlaufangebot der Kälteschranke je Zone, die
    /// Stufen AK1 bis AK3 und „ohne Kühlübergabe bitgleich“.
    /// </summary>
    public class ZonenKuehluebergabeTests
    {
        private readonly ITestOutputHelper _aus;

        public ZonenKuehluebergabeTests(ITestOutputHelper aus) { _aus = aus; }

        private static string F(double x) => x.ToString("F3", CultureInfo.InvariantCulture);

        // =====================================================================
        //  Die Vorrichtung
        // =====================================================================

        /// <summary>
        /// Zwei Hälften des Probegebäudes, gekühlt (Gebäude 22 °C), Hälfte 2 heizt auf 14 °C und kühlt auf 16 °C; mit
        /// <paramref name="kuehluebergabe"/> trägt das Gebäude den Gebläsekonvektor (Vorgabe 7/12 °C) und
        /// <c>Kuehluebergabe_Aktiv</c>.
        /// </summary>
        private static ProjektGebaeudeModel Zwei(Zoneneingaben eins = null, Zoneneingaben zwei = null, bool kuehluebergabe = true)
        {
            ProjektGebaeudeModel g = ZonenschleifeTests.Haelften(Trennflaechenzuordnung.Aussen);
            g.Kuehlung_Aktiv = true;
            g.Kuehl_Sollwert = 22.0;
            g.Kuehlleistung_Max = 10.0;
            if (kuehluebergabe)
            {
                g.Kuehluebergabe_Aktiv = true;
                g.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR;
            }
            GebaeudeZonensatz a = g.Zonen[0], b = g.Zonen[1];
            eins ??= new Zoneneingaben(SollTag: 20.0, SollNacht: 20.0);
            zwei ??= new Zoneneingaben(SollTag: 14.0, SollNacht: 14.0, KuehlSollwert: 16.0);
            g.Zonen = new[]
            {
                new GebaeudeZonensatz(a.ZonenId, a.Bezeichnung, a.Bauteile, a.Nutzflaeche_M2, eins with { Nutzflaeche = a.Nutzflaeche_M2 }, a.Rang),
                new GebaeudeZonensatz(b.ZonenId, b.Bezeichnung, b.Bauteile, b.Nutzflaeche_M2, zwei with { Nutzflaeche = b.Nutzflaeche_M2 }, b.Rang),
            };
            return g;
        }

        private static Mehrzonenergebnis Rechnen(ProjektGebaeudeModel g, string stufe, double kuehlVorlaufAnlageC = double.NaN)
            => Zonenrechnung.Rechnen(g, ZonenschleifeTests.KlimaDes(), true, stufe, 0, g.ID_Gebaeude,
                                     kuehlVorlaufAnlageC: kuehlVorlaufAnlageC);

        private static void Bitgleich(double[] a, double[] b, string was)
        {
            if (a == null || b == null)
            {
                Assert.True(a == null && b == null, was + ": nur eine Reihe vorhanden");
                return;
            }
            for (int h = 0; h < 8760; h++)
                Assert.True(BitConverter.DoubleToInt64Bits(a[h]) == BitConverter.DoubleToInt64Bits(b[h]),
                            was + ", Stunde " + h + ": " + a[h].ToString("R", CultureInfo.InvariantCulture) + " ≠ " +
                            b[h].ToString("R", CultureInfo.InvariantCulture));
        }

        private static void ErgebnisBitgleich(Mehrzonenergebnis a, Mehrzonenergebnis b)
        {
            Bitgleich(a.Gebaeude.HeizlastW, b.Gebaeude.HeizlastW, "Heizlast");
            Bitgleich(a.Gebaeude.KuehlbedarfKwh, b.Gebaeude.KuehlbedarfKwh, "Kühlbedarf");
            Bitgleich(a.Gebaeude.Raumtemperatur, b.Gebaeude.Raumtemperatur, "Raumluft");
            for (int z = 0; z < a.Zonen.Count; z++)
            {
                Bitgleich(a.Zonen[z].HeizlastW, b.Zonen[z].HeizlastW, "Heizlast Zone " + z);
                Bitgleich(a.Zonen[z].KuehlbedarfKwh, b.Zonen[z].KuehlbedarfKwh, "Kühlbedarf Zone " + z);
                Bitgleich(a.Zonen[z].Raumtemperatur, b.Zonen[z].Raumtemperatur, "Raumluft Zone " + z);
            }
        }

        // =====================================================================
        //  Übernahme und Kaskade
        // =====================================================================

        [Fact]
        public void Die_drei_Kuehlspalten_der_Zone_werden_uebernommen()
        {
            var z = new ZoneModel
            {
                Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE,
                Kuehl_Uebergabe_Exponent = 1.05,
                Kuehl_Uebergabe_Leistung_Nenn = 3.5,
            };
            Zoneneingaben e = Zoneneingaben.Aus(z);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, e.KuehlUebergabeArt);
            Assert.Equal(1.05, e.KuehlUebergabeExponent);
            Assert.Equal(3.5, e.KuehlUebergabeLeistungNennKw);
            Zoneneingaben leer = Zoneneingaben.Aus(new ZoneModel());
            Assert.Null(leer.KuehlUebergabeArt);
            Assert.Null(leer.KuehlUebergabeExponent);
            Assert.Null(leer.KuehlUebergabeLeistungNennKw);
        }

        [Fact]
        public void Die_Kaskade_erbt_vom_Gebaeude_und_teilt_nach_der_gekuehlten_Flaeche()
        {
            var g = new Gebaeudekuehluebergabe(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR, null, 8.0, null, null);
            Zonenkuehluebergabe erbt = Zonenkuehluebergabevorgaben.Aufloesen(new Zoneneingaben(), g, 24.0, 6000.0, 0.25);
            Assert.False(erbt.Ideal);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR, erbt.Art);
            Assert.Equal(Kuehluebergabe.VorgabeExponent(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR), erbt.Exponent);
            Assert.Equal(8.0, erbt.AuslegungVorlaufC);
            Assert.Equal(Kuehluebergabe.VorgabeRuecklaufC(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR), erbt.AuslegungRuecklaufC);
            Assert.Equal(24.0, erbt.AuslegungRaumtemperaturC);
            Assert.Equal(1500.0, erbt.NennleistungW);
            Assert.Equal(Vorgabeherkunft.GebaeudeAnteilig, erbt.NennleistungHerkunft);

            Zonenkuehluebergabe eigen = Zonenkuehluebergabevorgaben.Aufloesen(
                new Zoneneingaben(KuehlUebergabeArt: DbWerte.KUEHLUEBERGABE_KUEHLDECKE, KuehlUebergabeExponent: 1.1,
                                  KuehlUebergabeLeistungNennKw: 2.0), g, 24.0, 6000.0, 0.25);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, eigen.Art);
            Assert.Equal(1.1, eigen.Exponent);
            Assert.Equal(8.0, eigen.AuslegungVorlaufC);   // Auslegungspunkt allein am Gebäude (Festlegung 15)
            Assert.Equal(Kuehluebergabe.VorgabeRuecklaufC(DbWerte.KUEHLUEBERGABE_KUEHLDECKE), eigen.AuslegungRuecklaufC);
            Assert.Equal(2000.0, eigen.NennleistungW);
            Assert.Equal(Vorgabeherkunft.Zone, eigen.NennleistungHerkunft);

            Zonenkuehluebergabe ideal = Zonenkuehluebergabevorgaben.Aufloesen(
                new Zoneneingaben(KuehlUebergabeArt: DbWerte.KUEHLUEBERGABE_IDEAL), g, 24.0, 6000.0, 0.25);
            Assert.True(ideal.Ideal);
            Assert.True(double.IsNaN(ideal.NennleistungW));

            var flaechen = new List<(bool, double?)> { (true, 30.0), (false, 50.0), (true, 90.0) };
            Assert.Equal(0.25, Zonenkuehluebergabevorgaben.FlaechenanteilGekuehlt(0, flaechen));
            Assert.Equal(0.0, Zonenkuehluebergabevorgaben.FlaechenanteilGekuehlt(1, flaechen));
            Assert.Equal(0.75, Zonenkuehluebergabevorgaben.FlaechenanteilGekuehlt(2, flaechen));
            Assert.Equal(1.0, Zonenkuehluebergabevorgaben.FlaechenanteilGekuehlt(0, new List<(bool, double?)> { (true, null), (false, 5.0) }));
            Assert.True(double.IsNaN(Zonenkuehluebergabevorgaben.FlaechenanteilGekuehlt(0, new List<(bool, double?)> { (true, 3.0), (true, null) })));
        }

        // =====================================================================
        //  Der Kühlkreis je Gebäude
        // =====================================================================

        [Fact]
        public void Der_Kuehlkreis_je_Gebaeude_traegt_alle_gekuehlten_Zonen_am_gemeinsamen_Vorlauf()
        {
            Mehrzonenergebnis m = Rechnen(Zwei(), DbWerte.ANLAGENKOPPLUNG_AK1, 14.0);
            GebaeudeModellEingang e1 = m.Eingaenge[0].Eingang, e2 = m.Eingaenge[1].Eingang;
            Assert.True(e1.KuehlKopplungWirksam);
            Assert.True(e2.KuehlKopplungWirksam);
            Gebaeudekuehlkreis kreis = e1.Gebaeudekuehlkreis;
            Assert.NotNull(kreis);
            Assert.Same(kreis, e2.Gebaeudekuehlkreis);
            Assert.Same(e1.KuehlVorlaufC, e2.KuehlVorlaufC);
            Assert.Equal(14.0, kreis.VorlaufFestC);         // Anlage, keine Vorlaufgrenze beim Konvektor
            Assert.Equal(Vorlaufquelle.Anlage, kreis.Vorlaufquelle);
            Assert.True(kreis.NennleistungHergeleitet);
            Assert.Equal(16.0, kreis.Uebergabe.AuslegungRaumC);  // niedrigster Kühlsollwert der gekoppelten Zonen
            // Nennleistung des Gebäudes = Σ Auslegungskühllasten der Zonen, je Zone nach dem Flächenanteil (zwei Hälften).
            Assert.Equal(e1.AuslegungskuehllastW + e2.AuslegungskuehllastW, kreis.AuslegungskuehllastW, 6);
            Assert.Equal(0.5 * kreis.Uebergabe.PhiNW, e1.KuehlUebergabe.PhiNW, 6);
            Assert.Equal(0.5 * kreis.Uebergabe.PhiNW, e2.KuehlUebergabe.PhiNW, 6);
            Assert.Equal(22.0, e1.KuehlUebergabe.AuslegungRaumC);   // je Zone ihre Auslegungsraumtemperatur
            Assert.Equal(16.0, e2.KuehlUebergabe.AuslegungRaumC);

            Assert.NotNull(m.Zonen[0].Kuehlkreis);
            Assert.NotNull(m.Zonen[1].Kuehlkreis);
            KuehlkreisErgebnis geb = m.Gebaeude.Kuehlkreis;
            Assert.NotNull(geb);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR, geb.UebergabeArt);
            Assert.True(geb.Kuehlstunden > 0);
            Assert.True(geb.RuecklaufMittelC > geb.VorlaufMittelC);
            Assert.False(m.Eingaenge.Any(z => z.Eingang.KopplungAlsIdealeLast));
            _aus.WriteLine("Kühlkreis: Φ_N {0} kW, Vorlauf {1} °C, Rücklauf {2} °C, {3} Kühlstunden, begrenzt {4} h",
                           F(geb.UebergabeNennKw), F(geb.VorlaufMittelC), F(geb.RuecklaufMittelC), geb.Kuehlstunden,
                           F(geb.UebergabeBegrenztStundenH));
        }

        [Fact]
        public void Der_Ruecklauf_des_Kuehlkreises_ist_massenstromgewichtet()
        {
            Mehrzonenergebnis m = Rechnen(Zwei(), DbWerte.ANLAGENKOPPLUNG_AK1, 14.0);
            KuehlkreisErgebnis geb = m.Gebaeude.Kuehlkreis;
            int stunden = 0;
            for (int h = 0; h < 8760; h++)
            {
                double sW = 0.0, sWR = 0.0, v = double.NaN;
                for (int i = 0; i < 2; i++)
                {
                    KuehlkreisErgebnis z = m.Zonen[i].Kuehlkreis;
                    if (double.IsNaN(z.VorlaufC[h])) continue;
                    double w = m.Eingaenge[i].Eingang.KuehlUebergabeGespiegelt.WHWK;
                    v = z.VorlaufC[h];
                    sW += w;
                    sWR += w * z.RuecklaufC[h];
                }
                if (double.IsNaN(v))
                {
                    Assert.True(double.IsNaN(geb.RuecklaufC[h]));
                    continue;
                }
                stunden++;
                Assert.Equal(v, geb.VorlaufC[h]);
                Assert.Equal(sWR / sW, geb.RuecklaufC[h], 9);
            }
            Assert.True(stunden > 0);
        }

        [Fact]
        public void Ideale_und_ungekuehlte_Zonen_rechnen_ohne_Kuehlkreis()
        {
            Mehrzonenergebnis ideal = Rechnen(
                Zwei(zwei: new Zoneneingaben(SollTag: 14.0, SollNacht: 14.0, KuehlSollwert: 16.0,
                                             KuehlUebergabeArt: DbWerte.KUEHLUEBERGABE_IDEAL)), DbWerte.ANLAGENKOPPLUNG_AK1, 14.0);
            Assert.True(ideal.Eingaenge[0].Eingang.KuehlKopplungWirksam);
            Assert.False(ideal.Eingaenge[1].Eingang.KuehlKopplungWirksam);
            Assert.Null(ideal.Zonen[1].Kuehlkreis);
            Assert.Null(ideal.Eingaenge[1].Eingang.Gebaeudekuehlkreis);
            // Die ideale Zone zählt in den Flächenschlüssel (Spiegel der Heizseite): die gekoppelte trägt die Hälfte.
            Gebaeudekuehlkreis kreis = ideal.Eingaenge[0].Eingang.Gebaeudekuehlkreis;
            Assert.Equal(0.5 * kreis.Uebergabe.PhiNW, ideal.Eingaenge[0].Eingang.KuehlUebergabe.PhiNW, 6);
            KuehlkreisErgebnis eins = ideal.Zonen[0].Kuehlkreis, geb = ideal.Gebaeude.Kuehlkreis;
            for (int h = 0; h < 8760; h++)
                if (!double.IsNaN(eins.VorlaufC[h])) Assert.Equal(eins.RuecklaufC[h], geb.RuecklaufC[h], 9);

            Mehrzonenergebnis aus = Rechnen(
                Zwei(zwei: new Zoneneingaben(SollTag: 14.0, SollNacht: 14.0, KuehlungAktiv: false)), DbWerte.ANLAGENKOPPLUNG_AK1, 14.0);
            Assert.True(aus.Eingaenge[0].Eingang.KuehlKopplungWirksam);
            Assert.False(aus.Eingaenge[1].Eingang.KuehlKopplungWirksam);
            Assert.Equal(aus.Eingaenge[0].Eingang.Gebaeudekuehlkreis.Uebergabe.PhiNW,
                         aus.Eingaenge[0].Eingang.KuehlUebergabe.PhiNW, 6);   // einzige gekühlte Zone: Anteil 1
        }

        [Fact]
        public void Eine_eigene_Nennleistung_der_Zone_begrenzt_nur_ihre_Zone()
        {
            Mehrzonenergebnis m = Rechnen(
                Zwei(zwei: new Zoneneingaben(SollTag: 14.0, SollNacht: 14.0, KuehlSollwert: 16.0, KuehlUebergabeLeistungNennKw: 0.05)),
                DbWerte.ANLAGENKOPPLUNG_AK1, 14.0);
            GebaeudeModellEingang e2 = m.Eingaenge[1].Eingang;
            Assert.Equal(50.0, e2.KuehlUebergabe.PhiNW, 9);
            Assert.False(e2.KuehlNennleistungHergeleitet);
            Assert.True(m.Zonen[1].Kuehlkreis.UebergabeBegrenztStundenH > 0.0);
            Assert.True(m.Zonen[1].Kuehlkreis.UebergabeBegrenztStundenH > m.Zonen[0].Kuehlkreis.UebergabeBegrenztStundenH);
            Assert.True(m.Gebaeude.Kuehlkreis.UebergabeBegrenztStundenH >= m.Zonen[1].Kuehlkreis.UebergabeBegrenztStundenH);
        }

        // =====================================================================
        //  Die Kälteschranke je Zone als Vorlaufangebot
        // =====================================================================

        [Fact]
        public void Das_Vorlaufangebot_der_Kaelteschranke_wirkt_nur_an_einer_Zone_im_Kuehlkreis()
        {
            Uebergabekennwerte k = Kuehluebergabe.Gespiegelt(2000.0, 1.0, 7.0, 12.0, 24.0);
            var r = new Stundenrand(30.0, 30.0, 20.0, 24.0, 0.0, 0.0, 0.0, reglerbandK: 1.0,
                                    kuehlUebergabeGespiegelt: k, kuehlVorlaufC: 10.0, kuehlStrahlungsanteil: 0.0);
            Stundenrand ohne = r.MitKaelteverfuegbarkeit(5000.0, Verfuegbarkeitsgrund.KeineBegrenzung, 15.0);
            Assert.Equal(10.0, ohne.KuehlVorlaufC);          // ohne Kühlkreis nur mitgeführt (W6)
            Assert.Equal(15.0, ohne.KuehlVorlaufAngebotC);
            Assert.False(ohne.KuehlVorlaufAngebotGekappt);

            Stundenrand zone = r with { KuehlVorlaufAmAngebot = true };
            Stundenrand warm = zone.MitKaelteverfuegbarkeit(5000.0, Verfuegbarkeitsgrund.KeineBegrenzung, 15.0);
            Assert.Equal(15.0, warm.KuehlVorlaufC);          // kälter als das Angebot wird der Vorlauf nie
            Assert.True(warm.KuehlVorlaufAngebotGekappt);
            Stundenrand kalt = zone.MitKaelteverfuegbarkeit(5000.0, Verfuegbarkeitsgrund.KeineBegrenzung, 8.0);
            Assert.Equal(10.0, kalt.KuehlVorlaufC);          // ein kälteres Angebot mischt die Zone hoch
            Assert.False(kalt.KuehlVorlaufAngebotGekappt);

            Mehrzonenergebnis m = Rechnen(Zwei(), DbWerte.ANLAGENKOPPLUNG_AK1, 14.0);
            ZonenEingang z0 = m.Eingaenge[0];
            int h = Enumerable.Range(0, 8760).First(s => m.Zonen[0].KuehlbedarfKwh[s] > 0.0);
            double[] luft = { 25.0, 25.0 };
            Assert.True(z0.Rand(h, false, luft).KuehlVorlaufAmAngebot);
        }

        [Fact]
        public void Die_Kaelteschranke_wird_nach_dem_Kuehlbedarf_je_Zone_verteilt()
        {
            // Festlegung 17: die Schranke je Gebäude, der Kreis verteilt sie mit dem Schlüssel der Wärmeschranke.
            var kaelte = new Anlagenverfuegbarkeit(3.0, 14.0, Verfuegbarkeitsgrund.Leistungsgrenze);
            Anlagenverfuegbarkeit[][] v = Stundenverteilung.Verteilen(kaelte, new long[] { 1 }, new[] { true }, null,
                                                                      new IReadOnlyList<double>[] { new[] { 1000.0, 3000.0 } });
            Assert.Equal(2, v[0].Length);
            Assert.Equal(0.75, v[0][0].LeistungKw, 6);
            Assert.Equal(2.25, v[0][1].LeistungKw, 6);
            Assert.Equal(14.0, v[0][0].VorlaufC);
            Assert.Equal(14.0, v[0][1].VorlaufC);
        }

        // =====================================================================
        //  Die Stufen und der Schalter
        // =====================================================================

        [Theory]
        [InlineData(DbWerte.ANLAGENKOPPLUNG_AK1)]
        [InlineData(DbWerte.ANLAGENKOPPLUNG_AK2)]
        [InlineData(DbWerte.ANLAGENKOPPLUNG_AK3)]
        public void Die_Kuehluebergabe_je_Zone_gilt_ab_AK1(string stufe)
        {
            Mehrzonenergebnis m = Rechnen(Zwei(), stufe, 14.0);
            Assert.All(m.Eingaenge, z => Assert.True(z.Eingang.KuehlKopplungWirksam));
            Assert.NotNull(m.Gebaeude.Kuehlkreis);
        }

        [Fact]
        public void Ohne_Projektstufe_bleibt_die_Kaelteseite_ideal()
        {
            Mehrzonenergebnis m = Rechnen(Zwei(), null, 14.0);
            Assert.All(m.Eingaenge, z => Assert.False(z.Eingang.KuehlKopplungWirksam));
            Assert.Null(m.Gebaeude.Kuehlkreis);
        }

        [Fact]
        public void Mit_Kuehluebergabe_aendert_sich_nur_die_Kaelteseite_und_die_Raumluft()
        {
            Mehrzonenergebnis ideal = Rechnen(Zwei(kuehluebergabe: false), DbWerte.ANLAGENKOPPLUNG_AK1, 14.0);
            Mehrzonenergebnis gekoppelt = Rechnen(Zwei(), DbWerte.ANLAGENKOPPLUNG_AK1, 14.0);
            double sIdeal = ideal.Gebaeude.KuehlbedarfKwh.Sum(), sGekoppelt = gekoppelt.Gebaeude.KuehlbedarfKwh.Sum();
            _aus.WriteLine("Kältebedarf ideal {0} kWh, gekoppelt {1} kWh", F(sIdeal), F(sGekoppelt));
            Assert.True(sGekoppelt > 0.0);
            Assert.True(sGekoppelt <= sIdeal * 1.05);
        }
    }
}
