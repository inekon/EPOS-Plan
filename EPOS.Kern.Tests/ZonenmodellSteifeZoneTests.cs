using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Abschnittsregel bei steifer Zone</b> (Befund „Sportheim_1970_unsaniert", Betriebsfall
    /// <see cref="Betriebsfall.HeizenGeregelt"/> mit negativer Leistung). Eine Zone mit sehr kleiner
    /// Außenbauteilmasse hinter sehr kleinen Widerständen hat zwei schnelle Moden (Zeitkonstanten rund
    /// 2 ms und 17 s): Die geregelte Heizlast fällt binnen Millisekunden tief unter null, weil die warme
    /// Innenbauteilmasse die Raumluft über den Sollwert treibt, und steht nach wenigen Minuten zahlengleich
    /// auf ihrem positiven Endwert. Endpunkt und Ableitung am Ende sind dann zulässig bzw. null, die
    /// Innenprüfung (RP2a) sieht keinen Vorzeichenwechsel der Ableitung, und der Goldene Schnitt der
    /// Mittelprüfung (RB-Z4) sieht nur die Ebene — der Abschnitt blieb die ganze Stunde geregelt und buchte im
    /// Mittel Kälte. Die Probe hält den Rückfall auf das exakte Extremum: Der Abschnitt endet am ersten
    /// Nulldurchgang, danach läuft die Zone frei.
    /// </summary>
    public class ZonenmodellSteifeZoneTests
    {
        private readonly ITestOutputHelper _aus;

        public ZonenmodellSteifeZoneTests(ITestOutputHelper aus) => _aus = aus;

        private static ErsatzparameterRC SteifeZone()
            => new ErsatzparameterRC(4574.5936188272135, 15370565.954731598, 3.236560582860946E-07, 0.2737333717578045,
                                     9.62292104857054E-07, 2.0457513907011714E-07, 3.4837069955644774E-07,
                                     3.6535522602430453E-07, 0.00833858551787456, 1762.195, 1268.193, 1466.932);

        private const double AW = 12.473294489306069, IW = 28.765476294218317;

        /// <summary>Kalte Stunde ohne Gewinne, Sollwert rund 17 °C, die Heizung zur Hälfte als Strahlung.</summary>
        private static Stundenrand Stunde()
            => new Stundenrand(6.4967920638140235, 12.657280111525802, 16.991058051116326, double.PositiveInfinity,
                               0.0, 0.0, 0.0, heizungStrahlungsanteil: 0.5682127422505118);

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

        /// <summary>
        /// Der Befund nachgestellt: Die geregelte Heizlast startet bei rund +452 kW, fällt binnen 10 ms auf rund
        /// −10 MW und steht ab rund 300 s auf +1 274 W. Vor der Behebung brach die Stunde mit der Abschnittsregel
        /// ab (Mittel −48 992 W); jetzt endet der geregelte Abschnitt am ersten Nulldurchgang, die Stunde läuft
        /// danach frei — keine Kälte, keine negative Heizleistung, die Bilanz geschlossen.
        /// </summary>
        [Fact]
        public void Steife_Zone_endet_den_geregelten_Abschnitt_am_ersten_Nulldurchgang()
        {
            ErsatzparameterRC p = SteifeZone();
            Stundenrand r = Stunde();
            var m = new Zonenmodell2K(p, "Steife Zone");
            m.Zuruecksetzen(AW, IW);

            Stundenergebnis e = m.Schritt(in r);
            Stundenmuster muster = m.LetztesMuster;
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0} Abschnitte, erster {1} {2:G6} s, Heizen {3:G6} W, Kühlen {4} W, Raumluft {5:F4} °C",
                e.Abschnitte, muster.Folge[0], muster.Dauer[0], e.HeizleistungW, e.KuehlleistungW, e.ThetaAirMittel));

            Assert.True(e.Abschnitte >= 2);
            Assert.Equal(Betriebsfall.HeizenGeregelt, muster.Folge[0]);
            Assert.InRange(muster.Dauer[0], 0.0, 1.0);
            Assert.NotEqual(Betriebsfall.HeizenGeregelt, muster.Folge[1]);
            Assert.True(e.HeizleistungW >= 0.0);
            Assert.Equal(0.0, e.KuehlleistungW);
            Assert.True(e.ThetaAirMittel >= r.ThetaSoll - 1e-6);
            Bilanz(p, AW, IW, in r, in e);
        }

        /// <summary>
        /// Der Gegenfall: Die Abschnittsregel bleibt scharf. Wird dieselbe Stunde als ein geregelter Abschnitt
        /// über die ganze Stunde erzwungen (festes Muster, ohne Innenprüfung), bucht er im Mittel Kälte — das
        /// ist ein echter Vorzeichenfehler und fällt laut, ohne Klemmwert.
        /// </summary>
        [Fact]
        public void Erzwungener_geregelter_Abschnitt_mit_negativem_Mittel_verletzt_die_Abschnittsregel()
        {
            var m = new Zonenmodell2K(SteifeZone(), "Steife Zone") { InnenpruefungObergrenzeFuerProbe = 0 };
            m.Zuruecksetzen(AW, IW);
            Stundenrand r = Stunde();
            var muster = new Stundenmuster(new[] { Betriebsfall.HeizenGeregelt }, new[] { Zonenmodell2K.STUNDE_S });

            var ex = Assert.Throws<GebaeudeModellException>(() => m.SchrittMitMuster(in r, muster));
            Assert.Equal(GebaeudeModellFehler.AbschnittsregelVerletzt, ex.Grund);
            Assert.Contains("HeizenGeregelt", ex.Message);
            Assert.Equal(AW, m.ThetaMAw);
            Assert.Equal(IW, m.ThetaMIw);
        }
    }
}
