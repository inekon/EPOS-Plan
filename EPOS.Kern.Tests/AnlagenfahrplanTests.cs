using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Anlagenfahrplan, die Verteilung und die vierte Grenze</b> (Anlagenkopplung AK2-2a; 4.5, 5.3, 5.4, 6.2,
    /// Proben 11.1 der Stufe AK2) — ohne Datenbank, mit Phantasiewerten.
    /// </summary>
    public class AnlagenfahrplanTests
    {
        private const int H = 8760;

        private static bool[] Taeglich(int von, int bis)
        {
            var m = new bool[H];
            for (int h = 0; h < H; h++) m[h] = h % 24 >= von && h % 24 < bis;
            return m;
        }

        private static Anlagenzeitprogramm Programm(Func<int, double> faktor)
            => Anlagenzeitprogramm.Lesen(string.Join(";", Enumerable.Range(0, 168).Select(i =>
                   faktor(i).ToString(System.Globalization.CultureInfo.InvariantCulture))));

        // =====================================================================
        //  Der Fahrplan (5.4, F1, F7)
        // =====================================================================

        [Fact]
        public void Sperrzeit_halbiert_die_Leistung_an_der_Sperrstunde()
        {
            var f = Anlagenfahrplan.Rechnen(new[]
            {
                new Fahrplanerzeuger { NennleistungKw = 10.0, Gesperrt = Taeglich(14, 17), VorlaufAngebotC = 55.0 },
                new Fahrplanerzeuger { NennleistungKw = 10.0, VorlaufAngebotC = 50.0 },
            }, 0.0);
            Assert.Equal(10.0, f.Stunde(24 + 14).LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.Sperrzeit, f.Stunde(24 + 14).Grund);
            Assert.Equal(50.0, f.Stunde(24 + 14).VorlaufC);
            Assert.Equal(20.0, f.Stunde(24 + 10).LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, f.Stunde(24 + 10).Grund);
            Assert.Equal(55.0, f.Stunde(24 + 10).VorlaufC);
        }

        [Fact]
        public void Zeitprogramm_null_ergibt_null_mit_Grund_Zeitprogramm_und_die_Sperrzeit_geht_vor()
        {
            var f = Anlagenfahrplan.Rechnen(new[]
            {
                new Fahrplanerzeuger { NennleistungKw = 10.0, Zeitprogramm = Programm(_ => 0.0) },
            }, 0.0);
            Assert.All(f.Stunden, v => Assert.Equal(0.0, v.LeistungKw));
            Assert.All(f.Stunden, v => Assert.Equal(Verfuegbarkeitsgrund.Zeitprogramm, v.Grund));

            var g = Anlagenfahrplan.Rechnen(new[]
            {
                new Fahrplanerzeuger { NennleistungKw = 10.0, Zeitprogramm = Programm(_ => 0.5), Gesperrt = Taeglich(2, 3) },
            }, 0.0);
            Assert.Equal(5.0, g.Stunde(10).LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.Zeitprogramm, g.Stunde(10).Grund);
            Assert.Equal(0.0, g.Stunde(2).LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.Sperrzeit, g.Stunde(2).Grund);
        }

        [Fact]
        public void Speichervorrat_deckt_eine_zweistuendige_Sperre_nicht_eine_achtstuendige()
        {
            var kurz = Anlagenfahrplan.Rechnen(new[] { new Fahrplanerzeuger { NennleistungKw = 10.0, Gesperrt = Taeglich(14, 16) } }, 20.0);
            Assert.Equal(10.0, kurz.Stunde(14).LeistungKw);
            Assert.Equal(10.0, kurz.Stunde(15).LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, kurz.Stunde(15).Grund);

            var lang = Anlagenfahrplan.Rechnen(new[] { new Fahrplanerzeuger { NennleistungKw = 10.0, Gesperrt = Taeglich(8, 16) } }, 20.0);
            Assert.Equal(2.5, lang.Stunde(8).LeistungKw, 12);
            Assert.Equal(Verfuegbarkeitsgrund.SpeicherLeer, lang.Stunde(8).Grund);
            Assert.Equal(10.0, lang.Stunde(17).LeistungKw);

            var ohne = Anlagenfahrplan.Rechnen(new[] { new Fahrplanerzeuger { NennleistungKw = 10.0, Gesperrt = Taeglich(8, 16) } }, 0.0);
            Assert.Equal(Verfuegbarkeitsgrund.Sperrzeit, ohne.Stunde(8).Grund);
        }

        [Fact]
        public void Abschaltpunkt_und_kein_Erzeuger_tragen_ihren_Grund_und_der_Vorlauf_folgt_den_verfuegbaren()
        {
            var aus = new bool[H];
            aus[5] = true;
            var f = Anlagenfahrplan.Rechnen(new[]
            {
                new Fahrplanerzeuger { NennleistungKw = 30.0, Abgeschaltet = aus, VorlaufAngebotC = 45.0 },
                new Fahrplanerzeuger { NennleistungKw = 10.0, VorlaufAngebotC = 70.0 },
                new Fahrplanerzeuger { NennleistungKw = 5.0 },
            }, 0.0);
            Assert.Equal(15.0, f.Stunde(5).LeistungKw);
            Assert.Equal(Verfuegbarkeitsgrund.Abschaltpunkt, f.Stunde(5).Grund);
            Assert.True(double.IsNaN(f.Stunde(5).VorlaufC), "ein Erzeuger ohne Angebot: keine Vorlaufgrenze");

            var g = Anlagenfahrplan.Rechnen(new[]
            {
                new Fahrplanerzeuger { NennleistungKw = 30.0, VorlaufAngebotC = 45.0 },
                new Fahrplanerzeuger { NennleistungKw = 10.0, Gesperrt = Taeglich(0, 24), VorlaufAngebotC = 70.0 },
            }, 0.0);
            Assert.Equal(45.0, g.Stunde(3).VorlaufC);

            var leer = Anlagenfahrplan.Rechnen(Array.Empty<Fahrplanerzeuger>(), 0.0);
            Assert.All(leer.Stunden, v => Assert.Equal(Verfuegbarkeitsgrund.KeinErzeuger, v.Grund));
            Assert.All(leer.Stunden, v => Assert.Equal(0.0, v.LeistungKw));
        }

        // =====================================================================
        //  Die Verteilung (6.2, F4)
        // =====================================================================

        private static double[][] Bedarf(int n, int saat)
        {
            var z = new Random(saat);
            var b = new double[n][];
            for (int i = 0; i < n; i++)
            {
                b[i] = new double[H];
                for (int h = 0; h < H; h++) b[i][h] = h % 48 == 7 ? 0.0 : 30000.0 * z.NextDouble();   // W
            }
            return b;
        }

        private static Anlagenfahrplan Fahrplan()
            => Anlagenfahrplan.Rechnen(new[]
            {
                new Fahrplanerzeuger { NennleistungKw = 20.0, Gesperrt = Taeglich(14, 17) },
                new Fahrplanerzeuger { NennleistungKw = 15.0, Zeitprogramm = Programm(i => i % 24 < 6 ? 0.0 : 1.0) },
            }, 0.0);

        [Fact]
        public void Verfuegbarkeit_traegt_immer_einen_Grund()
        {
            double[][] b = Bedarf(3, 7);
            var s = SimulationWaermebedarf.Verteilen(Fahrplan(), new long[] { 3, 1, 2 }, new[] { true, true, false }, b,
                                                     new IReadOnlyList<double[]>[3], leistungsgrenzeAlsSchranke: true);
            int geprueft = 0;
            for (int i = 0; i < 2; i++)
                for (int h = 0; h < H; h++)
                {
                    Anlagenverfuegbarkeit v = s[i][0][h];
                    if (v.LeistungKw * 1000.0 < b[i][h])
                    {
                        geprueft++;
                        Assert.NotEqual(Verfuegbarkeitsgrund.KeineBegrenzung, v.Grund);
                    }
                }
            Assert.True(geprueft > 100, "die Probe sieht begrenzte Stunden: " + geprueft);
            Assert.Null(s[2]);
            Assert.Contains(s[0][0], v => v.Grund == Verfuegbarkeitsgrund.Leistungsgrenze);

            // Festlegung AK2-2a: Ohne Ausfall trägt die Stunde im Lauf keine Schranke (NaN) - mit Ausfall dieselbe.
            var lauf = SimulationWaermebedarf.Verteilen(Fahrplan(), new long[] { 3, 1, 2 }, new[] { true, true, false }, b,
                                                        new IReadOnlyList<double[]>[3]);
            Assert.True(double.IsNaN(lauf[0][0][24 + 10].LeistungKw));
            Assert.True(lauf[0][0][24 + 14].LeistungKw.Equals(s[0][0][24 + 14].LeistungKw));

            // Am Stundenrand: Auch ohne Grund des Fahrplans trägt eine Kappung einen Anlagengrund.
            var r = new Stundenrand(0, 0, 20, double.NaN, 0, 0, 0).MitVerfuegbarkeit(1000.0, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN);
            Assert.Equal(Verfuegbarkeitsgrund.Leistungsgrenze, r.GrundBeiKappung);
        }

        [Fact]
        public void Verteilung_auf_mehrere_Gebaeude_haengt_nicht_an_der_Zeilenreihenfolge()
        {
            double[][] b = Bedarf(3, 11);
            var vor = SimulationWaermebedarf.Verteilen(Fahrplan(), new long[] { 5, 9, 2 }, new[] { true, true, true }, b,
                                                       new IReadOnlyList<double[]>[3]);
            var rueck = SimulationWaermebedarf.Verteilen(Fahrplan(), new long[] { 2, 9, 5 }, new[] { true, true, true },
                                                         new[] { b[2], b[1], b[0] }, new IReadOnlyList<double[]>[3]);
            for (int h = 0; h < H; h++)
            {
                Assert.True(vor[0][0][h].LeistungKw.Equals(rueck[2][0][h].LeistungKw), "Id 5, Stunde " + h);
                Assert.True(vor[1][0][h].LeistungKw.Equals(rueck[1][0][h].LeistungKw), "Id 9, Stunde " + h);
                Assert.True(vor[2][0][h].LeistungKw.Equals(rueck[0][0][h].LeistungKw), "Id 2, Stunde " + h);
                Assert.Equal(vor[0][0][h].Grund, rueck[2][0][h].Grund);
            }
        }

        [Fact]
        public void Verteilung_Randfall_und_Rundungsrest()
        {
            var ids = new long[] { 4, 2, 7 };
            var key = new[] { 1.0 / 3.0, 2.0 / 7.0, 0.1 };
            double schranke = 10.0 / 3.0;
            double[] a = Verfuegbarkeitsverteilung.Verteilen(schranke, ids, key);
            Assert.True(Math.Abs(a.Sum() - schranke) <= 1e-9 * schranke);
            double summe = key.Sum();
            // Der Rest geht an den größten Anteil (Index 0), nicht an den letzten: die übrigen sind wörtlich proportional.
            Assert.True(a[1].Equals(schranke * (key[1] / summe)));
            Assert.True(a[2].Equals(schranke * (key[2] / summe)));

            double[] null3 = Verfuegbarkeitsverteilung.Verteilen(schranke, ids, new[] { 0.0, 0.0, -1.0 });
            Assert.All(null3, x => Assert.Equal(schranke, x));

            // Gleichstand: der Rest geht an die kleinere Id.
            double[] gleich = Verfuegbarkeitsverteilung.Verteilen(1.0, new long[] { 8, 3, 5 }, new[] { 1.0, 1.0, 1.0 });
            Assert.True(gleich[0].Equals(1.0 * (1.0 / 3.0)) && gleich[2].Equals(1.0 * (1.0 / 3.0)));
            Assert.Equal(1.0, gleich.Sum(), 12);
        }

        [Fact]
        public void Zweite_Verteilungsstufe_eine_Zone_bitgleich_zwei_Zonen_ergeben_die_Gebaeudeschranke()
        {
            double[][] b = Bedarf(2, 3);
            var ohne = SimulationWaermebedarf.Verteilen(Fahrplan(), new long[] { 1, 2 }, new[] { true, true }, b,
                                                        new IReadOnlyList<double[]>[2], leistungsgrenzeAlsSchranke: true);
            var eineZone = SimulationWaermebedarf.Verteilen(Fahrplan(), new long[] { 1, 2 }, new[] { true, true }, b,
                                                            new IReadOnlyList<double[]>[] { new[] { b[0] }, null }, leistungsgrenzeAlsSchranke: true);
            double[][] zonen = Bedarf(2, 19);
            var zweiZonen = SimulationWaermebedarf.Verteilen(Fahrplan(), new long[] { 1, 2 }, new[] { true, true }, b,
                                                             new IReadOnlyList<double[]>[] { zonen, null }, leistungsgrenzeAlsSchranke: true);
            Assert.Single(eineZone[0]);
            Assert.Equal(2, zweiZonen[0].Length);
            for (int h = 0; h < H; h++)
            {
                Assert.True(ohne[0][0][h].LeistungKw.Equals(eineZone[0][0][h].LeistungKw), "Stunde " + h);
                double g = ohne[0][0][h].LeistungKw;
                if (!(zonen[0][h] > 0.0) && !(zonen[1][h] > 0.0))
                {
                    // Randfall: keine Zone verlangt etwas - jede bekommt die volle Gebäudeschranke.
                    Assert.True(zweiZonen[0][0][h].LeistungKw.Equals(g) && zweiZonen[0][1][h].LeistungKw.Equals(g));
                    continue;
                }
                double s = zweiZonen[0][0][h].LeistungKw + zweiZonen[0][1][h].LeistungKw;
                Assert.True(Math.Abs(s - g) <= 1e-9 * Math.Max(1.0, g), "Stunde " + h + ": " + s + " / " + g);
            }
        }

        // =====================================================================
        //  Schritt H mit der vierten Grenze (4.5, 10.2)
        // =====================================================================

        private static Stundenrand Rand(Uebergabekennwerte k, double vorlauf, double heizMaxW = double.NaN)
            => new Stundenrand(-10.0, -10.0, 20.0, double.PositiveInfinity, 0.0, 0.0, 100.0, heizMaxW, double.NaN, 0.3, 0.0, 0.0,
                               uebergabe: k, vorlaufC: vorlauf, reglerbandK: 1.0);

        [Fact]
        public void Schritt_H_kappt_an_der_Verfuegbarkeit_der_Ruecklauf_steigt_und_der_Grund_ist_gepaart()
        {
            var k = new Uebergabekennwerte(8000.0, 1.3, 55.0, 45.0, 20.0);
            var frei = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var begrenzt = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            frei.Zuruecksetzen(19.0);
            begrenzt.Zuruecksetzen(19.0);
            Stundenergebnis a = frei.Schritt(Rand(k, 55.0));
            Stundenergebnis e = begrenzt.Schritt(Rand(k, 55.0, heizMaxW: 5000.0)
                                                 .MitVerfuegbarkeit(1500.0, Verfuegbarkeitsgrund.Sperrzeit, double.NaN));
            Assert.True(a.HeizleistungW > 1500.0, "ohne Schranke " + a.HeizleistungW);
            Assert.Equal(1500.0, e.HeizleistungW, 6);
            Assert.Equal(Begrenzungsgrund.Verfuegbarkeit, e.Begrenzungsgrund);
            Assert.Equal(Verfuegbarkeitsgrund.Sperrzeit, e.Verfuegbarkeitsgrund);
            Assert.Equal(1.0, e.VerfuegbarkeitAnteil, 12);
            Assert.Equal(0.0, e.HeizleistungMaxAnteil);
            Assert.True(e.RuecklaufC > a.RuecklaufC, "Rücklauf " + e.RuecklaufC + " gegen " + a.RuecklaufC);
            Assert.Equal(55.0 - e.HeizleistungW / k.WHWK, e.RuecklaufC, 9);
            Assert.True(e.ThetaAirMittel < a.ThetaAirMittel);

            // Liegt Heizleistung_Max darunter, bleibt sie die Grenze - mit ihrem eigenen Grund.
            var hl = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            hl.Zuruecksetzen(19.0);
            Stundenergebnis m = hl.Schritt(Rand(k, 55.0, heizMaxW: 1000.0).MitVerfuegbarkeit(1500.0, Verfuegbarkeitsgrund.Sperrzeit, double.NaN));
            Assert.Equal(Begrenzungsgrund.HeizleistungMax, m.Begrenzungsgrund);
            Assert.Equal(Verfuegbarkeitsgrund.KeineBegrenzung, m.Verfuegbarkeitsgrund);
        }

        [Fact]
        public void Eine_Schranke_die_nie_greift_rechnet_bitgleich()
        {
            var k = new Uebergabekennwerte(6000.0, 1.3, 55.0, 45.0, 20.0);
            var a = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var b = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            a.Zuruecksetzen(18.0);
            b.Zuruecksetzen(18.0);
            for (int h = 0; h < 24 * 20; h++)
            {
                double aussen = -5.0 + 8.0 * Math.Sin(2.0 * Math.PI * h / 24.0);
                double soll = h % 24 >= 6 && h % 24 <= 21 ? 20.0 : 16.0;
                var r = new Stundenrand(aussen, aussen, soll, double.PositiveInfinity, 0.0, 0.0, 200.0,
                                        h % 5 == 0 ? 4000.0 : double.NaN, double.NaN, 0.3, 0.0, 0.0,
                                        uebergabe: k, vorlaufC: 55.0, reglerbandK: 1.0);
                Stundenergebnis x = a.Schritt(in r);
                Stundenergebnis y = b.Schritt(r.MitVerfuegbarkeit(1e6, Verfuegbarkeitsgrund.KeineBegrenzung, 90.0));
                Assert.True(x.HeizleistungW.Equals(y.HeizleistungW) && x.ThetaAirMittel.Equals(y.ThetaAirMittel)
                            && x.RuecklaufC.Equals(y.RuecklaufC) && x.ThetaMAwEnde.Equals(y.ThetaMAwEnde), "Stunde " + h);
                Assert.Equal(x.Begrenzungsgrund, y.Begrenzungsgrund);
                Assert.False(y.VerfuegbarkeitBegrenzt);
            }
        }

        [Fact]
        public void Das_kleinere_Vorlaufangebot_gewinnt_mit_Grund_Vorlauf_Anlage()
        {
            var k = new Uebergabekennwerte(1500.0, 1.3, 55.0, 45.0, 20.0);
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            m.Zuruecksetzen(19.0);
            Stundenergebnis e = m.Schritt(Rand(k, 55.0).MitVerfuegbarkeit(double.NaN, Verfuegbarkeitsgrund.KeineBegrenzung, 45.0));
            Assert.Equal(45.0, e.VorlaufC);
            Assert.Equal(Begrenzungsgrund.VorlaufAnlage, e.Begrenzungsgrund);
            Assert.Equal(1.0, e.VorlaufAnlageAnteil, 12);
            Assert.Equal(1.0, e.UebergabeBegrenztAnteil, 12);

            // Ein höheres Angebot ändert nichts.
            Stundenrand r = Rand(k, 55.0);
            Stundenrand hoch = r.MitVerfuegbarkeit(double.NaN, Verfuegbarkeitsgrund.KeineBegrenzung, 70.0);
            Assert.Equal(55.0, hoch.VorlaufC);
            Assert.False(hoch.VorlaufAnlageGekappt);
        }
    }
}
