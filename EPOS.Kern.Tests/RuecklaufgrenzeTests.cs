using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Rücklaufgrenze</b> (Übergabegrenze UB‑E3; Fachkonzept 4.4, 2.11, 2.12, 8.1 Zeilen 13, 14, 17): abgeleitete
    /// Grenze, R744-Faktor auf Leistung und COP bei gleichem Strom, harte Grenze mit k = 0 und die BHKW-Abschaltgrenze.
    /// Ohne Datenbank.
    /// </summary>
    public class RuecklaufgrenzeTests
    {
        private static Uebergabezone Heizkoerper() => new Uebergabezone(10.0, 75.0, 60.0, 20.0, 1.3);

        [Fact]
        public void Abgeleitete_Grenze_52_schaltet_bei_52_5_ab()
        {
            Geraetegrenzen g = Geraetegrenzen.Bilden(Geraetespalten.Leer, 55.0);
            Assert.True(Ruecklaufgrenze.Pruefen(g, 52.5).Aus);
            Assert.False(Ruecklaufgrenze.Pruefen(g, 52.0).Aus);
            Assert.Equal(1.0, Ruecklaufgrenze.Pruefen(g, 52.0).Faktor);
            Assert.False(Ruecklaufgrenze.Pruefen(g, double.NaN).Aus);

            var modul = new Bivalenzmodul(new[] { Heizkoerper() }, null, 55.0, g.SpreizungMinK, true,
                                          Bivalenzbetriebsart.Parallel, true, true, g, "DIREKT", double.NaN);
            Bereichsergebnis b = modul.Bereich(0, true, Verfuegbarkeitsgrund.KeineBegrenzung, 60.0, 52.5, 8.0, 10.0);
            Assert.Equal(Betriebsbereich.NichtVerfuegbar, b.Bereich);
            Assert.Equal(Verfuegbarkeitsgrund.RuecklaufMax, b.Grund);
            Assert.Equal(0.0, b.LeistungKw);
            modul.Zaehlen(b, 0.0);
            Assert.Equal(1, modul.RuecklaufUeberschrittenStunden);

            // Eine Sperrstunde behält ihren Grund (Verfügbarkeit vor Rücklaufgrenze).
            Bereichsergebnis s = modul.Bereich(1, false, Verfuegbarkeitsgrund.Sperrzeit, 60.0, 52.5, 8.0, 10.0);
            Assert.Equal(Verfuegbarkeitsgrund.Sperrzeit, s.Grund);
        }

        [Fact]
        public void Feld_50_senkt_die_Grenze()
        {
            Geraetegrenzen g = Geraetegrenzen.Bilden(Geraetespalten.Leer with { RuecklaufMaxC = 50.0 }, 55.0);
            Assert.True(Ruecklaufgrenze.Pruefen(g, 50.5).Aus);
            Assert.False(Ruecklaufgrenze.Pruefen(g, 50.0).Aus);
        }

        [Fact]
        public void R744_wertet_Leistung_und_COP_ab_bei_gleichem_Strom()
        {
            var s = new Geraetespalten(null, null, null, null, 40.0, 30.0, 2.5, "R744");
            Geraetegrenzen g = Geraetegrenzen.Bilden(s, 80.0);
            Ruecklaufpruefung p = Ruecklaufgrenze.Pruefen(g, 34.0);
            Assert.False(p.Aus);
            Assert.Equal(0.90, p.Faktor, 12);
            Assert.True(Ruecklaufgrenze.Pruefen(g, 40.1).Aus);
            Assert.Equal(1.0, Ruecklaufgrenze.Pruefen(g, 29.0).Faktor);

            // Kennfeld 10 kW bei COP 3: Leistung 9 kW, COP 2,7, Strom 10/3 kW unverändert.
            double kennfeld = 10.0, cop = 3.0;
            double leistung = kennfeld * p.Faktor, copF = cop * p.Faktor;
            Assert.Equal(kennfeld / cop, leistung / copF, 12);

            var modul = new Bivalenzmodul(new[] { Heizkoerper() }, null, 80.0, g.SpreizungMinK, false,
                                          Bivalenzbetriebsart.Parallel, true, true, g, "DIREKT", double.NaN);
            Bereichsergebnis b = modul.Bereich(0, true, Verfuegbarkeitsgrund.KeineBegrenzung, 45.0, 34.0, 5.0, kennfeld);
            Assert.Equal(Betriebsbereich.WpAllein, b.Bereich);
            Assert.Equal(0.90, b.RuecklaufFaktor, 12);
            Assert.Equal(9.0, b.LeistungKw, 12);
        }

        [Fact]
        public void R744_ohne_Abwertung_ist_die_harte_Grenze()
        {
            var s = new Geraetespalten(null, null, null, null, 35.0, null, 0.0, "R744");
            Geraetegrenzen g = Geraetegrenzen.Bilden(s, 80.0);
            Assert.Equal(1.0, Ruecklaufgrenze.Pruefen(g, 34.0).Faktor);
            Assert.False(Ruecklaufgrenze.Pruefen(g, 35.0).Aus);
            Assert.True(Ruecklaufgrenze.Pruefen(g, 35.5).Aus);
        }

        [Fact]
        public void Bhkw_ueber_der_Grenze_liefert_nichts_ohne_Feld_unveraendert()
        {
            Assert.True(Ruecklaufgrenze.BhkwAus(70.0, 71.0));
            Assert.True(Ruecklaufgrenze.BhkwAus(70.0, 70.0));
            Assert.False(Ruecklaufgrenze.BhkwAus(70.0, 69.9));
            Assert.False(Ruecklaufgrenze.BhkwAus(null, 71.0));
            Assert.False(Ruecklaufgrenze.BhkwAus(70.0, double.NaN));
        }

        [Fact]
        public void Bhkw_Auslegungsruecklauf_unter_der_Abschaltgrenze()
        {
            Assert.True(Ruecklaufgrenze.BhkwAuslegungHinweis(70.0, 70.0));
            Assert.True(Ruecklaufgrenze.BhkwAuslegungHinweis(75.0, 70.0));
            Assert.False(Ruecklaufgrenze.BhkwAuslegungHinweis(60.0, 70.0));
            Assert.False(Ruecklaufgrenze.BhkwAuslegungHinweis(60.0, null));
        }
    }
}
