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
    /// <b>Die innere Umkehr der geregelten Last in schnellen Zonen</b> (Folgeauftrag zu Befund „Steife Zone"). Der
    /// dritte Ausgang eines Abschnitts ist c + a₁·e^(λ₁t) + a₂·e^(λ₂t); seine Ableitung hat höchstens eine Nullstelle.
    /// Klingen beide Moden weit vor dem Abschnittsende ab (langsame Zeitkonstante unter rund τ/37, etwa kleine
    /// Massen hinter kleinen Widerständen oder große Glasflächen), ist die Ableitung am Ende nur noch Rundungsrauschen:
    /// Ein Vorzeichenvergleich der Ableitung an Anfang und Ende trifft die Umkehr dann zufällig, und ein Goldener
    /// Schnitt auf [0; τ] sieht nur die Ebene des Endwerts. Die Proben halten, dass jede innere Umkehr der geregelten
    /// Last über die exakte Nullstelle der Ableitung gefunden und der Abschnitt am ersten Nulldurchgang geschnitten
    /// wird — auch bei zulässigem Mittel.
    ///
    /// <para><b>Das Orakel</b> ist vom Löser unabhängig: Es zerlegt jeden geregelten Abschnitt des gefundenen Musters
    /// in Stücke (geometrisch zum Anfang hin verdichtet, dazu ein gleichmäßiges Raster) und rechnet das zerlegte Muster
    /// ohne Innenprüfung nach. Bucht ein Stück im Mittel Leistung mit falschem Vorzeichen, wirft die Abschnittsregel —
    /// der Abschnitt hatte eine verborgene Umkehr.</para>
    /// </summary>
    public class ZonenmodellInnereUmkehrTests
    {
        private readonly ITestOutputHelper _aus;

        public ZonenmodellInnereUmkehrTests(ITestOutputHelper aus) => _aus = aus;

        /// <summary>
        /// Hat der geregelte Abschnitt <paramref name="i"/> des Musters eine Stelle mit falschem Vorzeichen der Last?
        /// Die Stücke reichen bis d·2⁻³⁴ an den Abschnittsbeginn heran; der Rest der Stunde läuft als Totband (bucht
        /// nichts), die Abschnitte davor wie gefunden.
        /// </summary>
        private static bool VerborgeneUmkehr(ErsatzparameterRC p, double aw, double iw, in Stundenrand r, Stundenmuster m, int i)
        {
            double d = m.Dauer[i];
            var grenzen = new SortedSet<double>();
            for (int k = 1; k <= 34; k++) grenzen.Add(d * Math.Pow(2.0, -k));
            for (int j = 1; j < 16; j++) grenzen.Add(d * j / 16.0);
            grenzen.Add(d);
            var folge = new List<Betriebsfall>();
            var dauer = new List<double>();
            for (int k = 0; k < i; k++)
            {
                folge.Add(m.Folge[k]);
                dauer.Add(m.Dauer[k]);
            }
            double vor = 0.0;
            foreach (double g in grenzen)
            {
                folge.Add(m.Folge[i]);
                dauer.Add(g - vor);
                vor = g;
            }
            // Das Muster muss die Stunde genau füllen: Der letzte Abschnitt ist der Rest, wie ihn das Modell zählt.
            if (i < m.Anzahl - 1)
            {
                folge.Add(Betriebsfall.Totband);
                dauer.Add(0.0);
            }
            double summe = 0.0;
            for (int k = 0; k < dauer.Count - 1; k++) summe += dauer[k];
            dauer[dauer.Count - 1] = Zonenmodell2K.STUNDE_S - summe;
            Assert.True(dauer[dauer.Count - 1] > 0.0);
            Assert.True(folge.Count <= Zonenmodell2K.ABSCHNITTSDECKEL);

            var z = new Zonenmodell2K(p, "Orakel") { InnenpruefungObergrenzeFuerProbe = 0 };
            z.Zuruecksetzen(aw, iw);
            try
            {
                z.SchrittMitMuster(in r, new Stundenmuster(folge.ToArray(), dauer.ToArray()));
            }
            catch (GebaeudeModellException ex) when (ex.Grund == GebaeudeModellFehler.AbschnittsregelVerletzt)
            {
                return true;
            }
            return false;
        }

        /// <summary>Die Bilanz einer Stunde: Speicheränderung = Zuflüsse (Muster Zonenmodell2KTests).</summary>
        private static void Bilanz(ErsatzparameterRC p, double aw0, double iw0, in Stundenrand r, in Stundenergebnis e)
        {
            double u0 = p.C_AW_Jk * aw0 + p.C_IW_Jk * iw0;
            double u1 = p.C_AW_Jk * e.ThetaMAwEnde + p.C_IW_Jk * e.ThetaMIwEnde;
            double zufluss = (r.ThetaEq - e.ThetaMAwMittel) / p.R_Rest_AWGruppe_KW + (r.ThetaOut - e.ThetaAirMittel) / p.R_ext_KW
                           + r.PhiRadAW + r.PhiRadIW + r.PhiConv + e.HeizleistungW - e.KuehlleistungW;
            double speicher = (u1 - u0) / Zonenmodell2K.STUNDE_S;
            Assert.True(Math.Abs(speicher - zufluss) < 1e-6 * (1.0 + Math.Abs(zufluss)), "Bilanz: Speicher " + speicher + " W, Zufluss " + zufluss + " W");
        }

        /// <summary>Eine schnelle Zone der Zufallsprobe mit Stunde und Anfangszustand.</summary>
        public sealed class Fall
        {
            internal Fall(string name, ErsatzparameterRC p, double aw, double iw, Stundenrand r, Betriebsfall geregelt)
            {
                Name = name;
                P = p;
                Aw = aw;
                Iw = iw;
                R = r;
                Geregelt = geregelt;
            }

            internal string Name { get; }
            internal ErsatzparameterRC P { get; }
            internal double Aw { get; }
            internal double Iw { get; }
            internal Stundenrand R { get; }
            internal Betriebsfall Geregelt { get; }
            public override string ToString() => Name;
        }

        /// <summary>
        /// Drei Zonen der Zufallsprobe, deren geregelter Abschnitt im Innern umkehrt, obwohl Mittel und Endpunkt
        /// zulässig sind: (a) Heizen, Eigenwerte −0,050/−0,438 1/s; (b) Kühlen, −0,088/−0,706 1/s; (c) Heizen,
        /// −1,388/−2,456 1/s. In allen dreien ist die Ableitung am Stundenende Rauschen.
        /// </summary>
        public static IEnumerable<object[]> Faelle()
        {
            yield return new object[] { new Fall("Heizen, langsame Mode 20 s",
                new ErsatzparameterRC(1043411.6957729504, 1180872.1113639262, 1.10868632176684E-06, 0.0018153266643533435, 1.613460782545891E-05,
                                      1.0846292407075374E-06, 6.967319523815655E-07, 0.005850379749666411, 0.0002933903020225324,
                                      809.6436720619182, 1916.8278341911864, 203.90112553439155),
                8.243944525366624, 23.70687378975883,
                new Stundenrand(6.684496675936735, 10.941738007097385, 16.456666105173838, double.PositiveInfinity,
                                223.41506240117133, 2106.4103791054386, 2239.668148681367, heizungStrahlungsanteil: 0.01391705033086103),
                Betriebsfall.HeizenGeregelt) };
            yield return new object[] { new Fall("Kühlen, langsame Mode 11 s",
                new ErsatzparameterRC(1966510.6408319671, 129999.86594063787, 4.615219358844051E-07, 0.01015328419630635, 8.693183503803457E-05,
                                      8.805958382102845E-07, 1.389444336583198E-07, 2.2791180701659602E-07, 0.09875180458621152,
                                      591.7879107789081, 1594.4416865168334, 842.5809545081952),
                30.514038598869945, 15.262291734228977,
                new Stundenrand(20.60195656521337, -12.44752985772096, 22.638544238935477, 25.563609851786683,
                                2135.748304489883, 1891.1536060698113, 1722.1649245927879, heizungStrahlungsanteil: 0.3094695202584702),
                Betriebsfall.KuehlenGeregelt) };
            yield return new object[] { new Fall("Heizen, langsame Mode 0,7 s",
                new ErsatzparameterRC(401777.2160976056, 100951.16066585536, 2.954100100315421E-07, 0.00014634192698672563, 6.131522423971057E-07,
                                      7.280672807655878E-07, 6.7578497284748245E-06, 0.00018633555325381915, 0.04844081530558359,
                                      775.4223632651485, 1165.8979631615327, 179.91469680327677),
                8.1135306335583, 7.070918004992846,
                new Stundenrand(7.563275374734438, -3.615269199300217, 17.967793346833343, double.PositiveInfinity,
                                1941.9596837563251, 1306.8677863603773, 1438.683874178065, heizungStrahlungsanteil: 0.722215309144098),
                Betriebsfall.HeizenGeregelt) };
        }

        /// <summary>
        /// Die Stunde schneidet den geregelten Abschnitt am ersten Nulldurchgang der Last: Das Orakel findet in keinem
        /// geregelten Abschnitt eine Stelle mit falschem Vorzeichen, der erste Abschnitt endet vor dem Stundenende, die
        /// Gegenseite bleibt leer, die Bilanz geschlossen.
        /// </summary>
        [Theory]
        [MemberData(nameof(Faelle))]
        public void Schnelle_Zone_schneidet_die_innere_Umkehr_bei_zulaessigem_Mittel(Fall f)
        {
            var m = new Zonenmodell2K(f.P, f.Name);
            m.Zuruecksetzen(f.Aw, f.Iw);
            Stundenrand r = f.R;
            Stundenergebnis e = m.Schritt(in r);
            Stundenmuster muster = m.LetztesMuster;
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1} Abschnitte ({2}), erster {3:G6} s, Heizen {4:G6} W, Kühlen {5:G6} W",
                f.Name, e.Abschnitte, string.Join(", ", muster.Folge.ToArray()), muster.Dauer[0], e.HeizleistungW, e.KuehlleistungW));

            Assert.Equal(f.Geregelt, muster.Folge[0]);
            Assert.True(muster.Dauer[0] < Zonenmodell2K.STUNDE_S);
            for (int i = 0; i < muster.Anzahl; i++)
                if (muster.Folge[i] == Betriebsfall.HeizenGeregelt || muster.Folge[i] == Betriebsfall.KuehlenGeregelt)
                    Assert.False(VerborgeneUmkehr(f.P, f.Aw, f.Iw, in r, muster, i), "verborgene Umkehr im Abschnitt " + i);
            if (f.Geregelt == Betriebsfall.HeizenGeregelt) Assert.Equal(0.0, e.KuehlleistungW);
            else Assert.Equal(0.0, e.HeizleistungW);
            Bilanz(f.P, f.Aw, f.Iw, in r, in e);
        }

        /// <summary>
        /// Das feste Muster „ein geregelter Abschnitt über die Stunde" ist in diesen Zonen nicht haltbar: Die
        /// Innenprüfung im festen Muster erkennt die Umkehr und wirft, die Zonenschleife rechnet die Stunde dann frei.
        /// </summary>
        [Theory]
        [MemberData(nameof(Faelle))]
        public void Festes_Muster_ueber_die_Umkehr_ist_nicht_haltbar(Fall f)
        {
            var m = new Zonenmodell2K(f.P, f.Name);
            m.Zuruecksetzen(f.Aw, f.Iw);
            Stundenrand r = f.R;
            var ex = Assert.Throws<GebaeudeModellException>(() =>
                m.SchrittMitMuster(in r, new Stundenmuster(new[] { f.Geregelt }, new[] { Zonenmodell2K.STUNDE_S })));
            Assert.Equal(GebaeudeModellFehler.AbschnittsregelVerletzt, ex.Grund);
            Assert.Contains("im Innern um", ex.Message);
            Assert.Equal(f.Aw, m.ThetaMAw);
            Assert.Equal(f.Iw, m.ThetaMIw);
        }

        /// <summary>
        /// <b>Die Zufallsprobe</b> (6 000 Zonen, Samen 4711; Massen 10³ … 10⁸ J/K, Widerstände 10⁻⁷ … 10⁻¹ K/W, Rand und
        /// Zustand gleichverteilt, 40 % mit Kühlung): Kein geregelter Abschnitt der ersten acht trägt eine verborgene
        /// Umkehr, und keine Stunde bricht mit der Abschnittsregel ab. Unzulässige Parametersätze (komplexe oder
        /// nichtnegative Eigenwerte) zählen nicht.
        /// </summary>
        [Fact]
        public void Zufallsprobe_ohne_verborgene_Umkehr()
        {
            var rnd = new Random(4711);
            double L(double a, double b) => Math.Pow(10.0, a + (b - a) * rnd.NextDouble());
            double U(double a, double b) => a + (b - a) * rnd.NextDouble();
            int geregelt = 0, verborgen = 0;
            var fehler = new Dictionary<GebaeudeModellFehler, int>();
            for (int n = 0; n < 6000; n++)
            {
                ErsatzparameterRC p;
                try
                {
                    p = new ErsatzparameterRC(L(3, 7), L(5, 8), L(-7, -2), L(-4, -1), L(-7, -2), L(-7, -3), L(-7, -3), L(-7, -2), L(-5, -1),
                                              U(10, 2000), U(10, 2000), U(10, 2000));
                }
                catch (GebaeudeModellException)
                {
                    continue;
                }
                double aw = U(5, 35), iw = U(5, 35);
                bool kuehl = rnd.NextDouble() < 0.4;
                double soll = U(15, 23);
                var r = new Stundenrand(U(-15, 30), U(-15, 40), soll, kuehl ? soll + U(2, 6) : double.PositiveInfinity,
                                        U(0, 3000), U(0, 3000), U(0, 3000), heizungStrahlungsanteil: U(0, 1));
                var z = new Zonenmodell2K(p, "Probe");
                z.Zuruecksetzen(aw, iw);
                Stundenmuster m;
                try
                {
                    z.Schritt(in r);
                    m = z.LetztesMuster;
                }
                catch (GebaeudeModellException ex)
                {
                    fehler[ex.Grund] = fehler.TryGetValue(ex.Grund, out int c) ? c + 1 : 1;
                    continue;
                }
                for (int i = 0; i < Math.Min(m.Anzahl, Zonenmodell2K.INNENPRUEFUNG_ABSCHNITTE); i++)
                {
                    if (m.Folge[i] != Betriebsfall.HeizenGeregelt && m.Folge[i] != Betriebsfall.KuehlenGeregelt) continue;
                    geregelt++;
                    if (VerborgeneUmkehr(p, aw, iw, in r, m, i)) verborgen++;
                }
            }
            _aus.WriteLine("geregelte Abschnitte " + geregelt + ", verborgene Umkehr " + verborgen + ", Fehler: " +
                           string.Join(", ", fehler.Select(kv => kv.Key + " " + kv.Value)));
            Assert.True(geregelt > 5000);
            Assert.Equal(0, verborgen);
            Assert.False(fehler.ContainsKey(GebaeudeModellFehler.AbschnittsregelVerletzt));
            Assert.False(fehler.ContainsKey(GebaeudeModellFehler.AbschnittsdeckelErreicht));
        }
    }
}
