using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Verteilung je Stunde</b> (AK3-W3a, Festlegung 9) — ohne Datenbank: Summe erhalten, Rundungsrest,
    /// ein und mehrere Gebäude, Zonen, Determinismus, und die Gegenprobe „je Stunde wie der Zweipass von AK2“.
    /// </summary>
    public class AnlagenkopplungVerteilungTests
    {
        private static Anlagenverfuegbarkeit Angebot(double kw, Verfuegbarkeitsgrund grund = Verfuegbarkeitsgrund.KeineBegrenzung)
            => new Anlagenverfuegbarkeit(kw, 55.0, grund);

        [Fact]
        public void Ein_Gebaeude_ohne_Probeschritt_bekommt_das_ganze_Angebot_bitgleich()
        {
            var r = Stundenverteilung.Verteilen(Angebot(12.345678), new long[] { 7 }, new[] { true }, null, null);
            Assert.Single(r[0]);
            Assert.Equal(12.345678, r[0][0].LeistungKw);
            Assert.Equal(55.0, r[0][0].VorlaufC);
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, r[0][0].Grund);
        }

        [Fact]
        public void Mehrere_Gebaeude_proportional_Summe_erhalten_Rest_an_den_groessten()
        {
            double angebot = 10.0;
            var ids = new long[] { 3, 1, 2 };
            var schluessel = new[] { 1000.0, 2000.0, 3000.0 };   // W
            var r = Stundenverteilung.Verteilen(Angebot(angebot), ids, new[] { true, true, true }, schluessel, null);
            double summe = r.Sum(g => g[0].LeistungKw);
            Assert.Equal(angebot, summe, 12);
            Assert.Equal(10.0 / 6.0, r[0][0].LeistungKw, 12);
            Assert.Equal(10.0 / 3.0, r[1][0].LeistungKw, 12);
            // Rundungsrest beim größten Anteil (Id 3, Schlüssel 3000 W): er trägt schranke − Σ der übrigen.
            Assert.Equal(angebot * (3000.0 / 6000.0), r[2][0].LeistungKw, 12);
        }

        [Fact]
        public void Knappes_Angebot_heisst_Leistungsgrenze_ein_genannter_Grund_bleibt()
        {
            var ids = new long[] { 1, 2 };
            var r = Stundenverteilung.Verteilen(Angebot(5.0), ids, new[] { true, true }, new[] { 4000.0, 4000.0 }, null);
            Assert.Equal(Verfuegbarkeitsgrund.Leistungsgrenze, r[0][0].Grund);
            var s = Stundenverteilung.Verteilen(Angebot(5.0, Verfuegbarkeitsgrund.Sperrzeit), ids, new[] { true, true },
                                                new[] { 4000.0, 4000.0 }, null);
            Assert.Equal(Verfuegbarkeitsgrund.Sperrzeit, s[0][0].Grund);
            var t = Stundenverteilung.Verteilen(Angebot(9.0), ids, new[] { true, true }, new[] { 4000.0, 4000.0 }, null);
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, t[0][0].Grund);
        }

        [Fact]
        public void Summe_der_Schluessel_null_jeder_bekommt_das_volle_Angebot()
        {
            var r = Stundenverteilung.Verteilen(Angebot(8.0), new long[] { 1, 2 }, new[] { true, true }, new[] { 0.0, -3.0 }, null);
            Assert.Equal(8.0, r[0][0].LeistungKw);
            Assert.Equal(8.0, r[1][0].LeistungKw);
        }

        [Fact]
        public void Ungekoppelte_Gebaeude_zehren_mit_bekommen_aber_nichts()
        {
            var r = Stundenverteilung.Verteilen(Angebot(9.0), new long[] { 1, 2 }, new[] { true, false }, new[] { 1000.0, 2000.0 }, null);
            Assert.Null(r[1]);
            Assert.Equal(3.0, r[0][0].LeistungKw, 12);
        }

        [Fact]
        public void Zonen_teilen_den_Gebaeudeanteil_Summe_erhalten()
        {
            var zonen = new IReadOnlyList<double>[] { new[] { 100.0, 300.0, 0.0 }, new[] { 5.0 } };
            var r = Stundenverteilung.Verteilen(Angebot(12.0), new long[] { 1, 2 }, new[] { true, true }, new[] { 2000.0, 1000.0 }, zonen);
            Assert.Equal(3, r[0].Length);
            Assert.Single(r[1]);
            Assert.Equal(8.0, r[0].Sum(z => z.LeistungKw), 12);
            Assert.Equal(2.0, r[0][0].LeistungKw, 12);
            Assert.Equal(6.0, r[0][1].LeistungKw, 12);
            Assert.Equal(0.0, r[0][2].LeistungKw);
            Assert.Equal(4.0, r[1][0].LeistungKw, 12);
        }

        [Fact]
        public void Umgekehrte_Zeilenreihenfolge_ist_bitgleich()
        {
            var ids = new long[] { 11, 4, 9 };
            var s = new[] { 1234.5, 987.6, 4321.0 };
            var a = Stundenverteilung.Verteilen(Angebot(7.77), ids, new[] { true, true, true }, s, null);
            var b = Stundenverteilung.Verteilen(Angebot(7.77), ids.Reverse().ToArray(), new[] { true, true, true }, s.Reverse().ToArray(), null);
            for (int i = 0; i < 3; i++) Assert.Equal(a[i][0].LeistungKw, b[2 - i][0].LeistungKw);
        }

        [Fact]
        public void Je_Stunde_wie_der_Zweipass_von_AK2_aus_demselben_Angebot()
        {
            // Ein Fahrplan mit Sperre, Zeitprogramm und Speichervorrat liefert 8760 Angebote; die Jahresverteilung
            // von AK2 (mit Schranke auch ohne Ausfall) und die Stundenverteilung müssen bitgleich sein.
            var gesperrt = new bool[8760];
            for (int h = 0; h < 8760; h++) gesperrt[h] = h % 24 >= 12 && h % 24 < 15;
            var fahrplan = Anlagenfahrplan.Rechnen(new[]
            {
                new Fahrplanerzeuger { NennleistungKw = 9.0, Gesperrt = gesperrt, VorlaufAngebotC = 55.0 },
                new Fahrplanerzeuger { NennleistungKw = 6.0, VorlaufAngebotC = 70.0 },
            }, 10.0);
            var zufall = new Random(4711);
            var ids = new long[] { 30, 10, 20 };
            var gekoppelt = new[] { true, true, false };
            var schluessel = new double[3][];
            for (int i = 0; i < 3; i++) schluessel[i] = Enumerable.Range(0, 8760).Select(_ => zufall.NextDouble() * 8000.0).ToArray();
            var zonen = new IReadOnlyList<double[]>[3];
            zonen[1] = new[]
            {
                Enumerable.Range(0, 8760).Select(_ => zufall.NextDouble() * 900.0).ToArray(),
                Enumerable.Range(0, 8760).Select(_ => zufall.NextDouble() * 900.0).ToArray(),
            };

            var jahr = SimulationWaermebedarf.Verteilen(fahrplan, ids, gekoppelt, schluessel, zonen, leistungsgrenzeAlsSchranke: true);
            for (int h = 0; h < 8760; h++)
            {
                var key = new double[3];
                for (int i = 0; i < 3; i++) key[i] = schluessel[i][h];
                var zonenH = new IReadOnlyList<double>[] { null, new[] { zonen[1][0][h], zonen[1][1][h] }, null };
                var stunde = Stundenverteilung.Verteilen(fahrplan.Stunde(h), ids, gekoppelt, key, zonenH);
                for (int i = 0; i < 3; i++)
                {
                    if (!gekoppelt[i])
                    {
                        Assert.Null(stunde[i]);
                        continue;
                    }
                    Assert.Equal(jahr[i].Length, stunde[i].Length);
                    for (int z = 0; z < stunde[i].Length; z++)
                    {
                        Assert.Equal(jahr[i][z][h].LeistungKw, stunde[i][z].LeistungKw);
                        Assert.Equal(jahr[i][z][h].Grund, stunde[i][z].Grund);
                        Assert.Equal(jahr[i][z][h].VorlaufC, stunde[i][z].VorlaufC);
                    }
                }
            }
        }

        [Fact]
        public void Falsche_Laengen_werden_benannt_abgewiesen()
        {
            Assert.Throws<ArgumentException>(() =>
                Stundenverteilung.Verteilen(Angebot(1.0), new long[] { 1, 2 }, new[] { true }, null, null));
            Assert.Throws<ArgumentException>(() =>
                Stundenverteilung.Verteilen(Angebot(1.0), new long[] { 1, 2 }, new[] { true, true }, new[] { 1.0 }, null));
        }
    }
}
