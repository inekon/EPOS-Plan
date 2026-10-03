using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Abschnittsregel im Innern eines geregelten Abschnitts</b> (Rechenbefund RB-Z4) und die
    /// <b>Grenze des Fensterzweigs</b> (A7a bei hohem U_w).
    ///
    /// <para>Der Befund: Im Mehrzonenlauf der Verwaltung (IFC, Zonierung Z4) brach eine Zone ab, weil ein
    /// Abschnitt „Heizen geregelt" im Mittel −174 W buchte. Die Zone trägt leichte Außenbauteile
    /// (C_AW 1,8 MJ/K gegen C_IW 71,5 MJ/K, R₁ 4·10⁻⁵ K/W, Zeitkonstante rund 70 s); ihre Innenmasse
    /// war wärmer als der Sollwert. Die Last fällt in der schnellen Mode binnen drei Minuten von
    /// +1 170 W unter null und steigt bis zum Stundenende wieder auf +105 W — die Bisektion prüfte nur
    /// den Endpunkt und ließ die ganze Stunde geregelt. Die Kopplung der Zonen ist nicht die Ursache:
    /// Der Zusatzleitwert der Stunde war null, die Nachbarn wirken nur über θ_eq und die Lasten.</para>
    ///
    /// <para>Die Zahlen des Falls sind die gemessenen Größen der Zone am Abbruch, gerundet.</para>
    /// </summary>
    public class ZonenkopplungAbschnittsregelTests
    {
        private readonly ITestOutputHelper _aus;

        public ZonenkopplungAbschnittsregelTests(ITestOutputHelper aus) => _aus = aus;

        /// <summary>Die Zone am Abbruch: leichte Außenbauteile, schwere Innenbauteile, Fenster U·A 169 W/K.</summary>
        private static ErsatzparameterRC ZoneAmAbbruch()
            => new ErsatzparameterRC(1.8016e6, 7.1519e7, 4.2769e-5, 5.44496e-4, 8.66509e-5, 1.93304e-4, 2.92046e-4,
                                     1.57705e-4, 3.02137e-3, 1762.195, 1268.193, 1466.932, 8.00903e-4, 4.0279e-3,
                                     153.804, 169.184, 2.08768e-5);

        /// <summary>Die Stunde am Abbruch: außen 6,15 °C, θ_eq 16,25 °C, Sollwert 20 °C, ohne Kühlung, mit Gewinnen.</summary>
        private static Stundenrand StundeAmAbbruch()
            => new Stundenrand(6.15, 16.25, 20.0, double.PositiveInfinity, 4055.87, 2684.57, 1945.32);

        private const double AW_START = 19.4985, IW_START = 21.0807;

        /// <summary>
        /// Ohne Korrektur — die Stunde als ein Abschnitt „Heizen geregelt", genau so, wie die Bisektion des
        /// Endpunkts sie ließ — bucht der Abschnitt im Mittel eine negative Heizleistung und fällt mit
        /// <see cref="GebaeudeModellFehler.AbschnittsregelVerletzt"/>. Mit Korrektur endet der geregelte
        /// Abschnitt am ersten Nulldurchgang (rund 200 s), danach läuft die Zone frei über dem Sollwert:
        /// keine Kälte, Heizwärme positiv, die Bilanz der Stunde geschlossen.
        /// </summary>
        [Fact]
        public void Eine_Lastumkehr_im_Innern_beendet_den_geregelten_Abschnitt()
        {
            ErsatzparameterRC p = ZoneAmAbbruch();
            Stundenrand r = StundeAmAbbruch();
            var m = new Zonenmodell2K(p);

            m.Zuruecksetzen(AW_START, IW_START);
            var alt = Assert.Throws<GebaeudeModellException>(() =>
                m.SchrittMitMuster(in r, new Stundenmuster(new[] { Betriebsfall.HeizenGeregelt }, new[] { Zonenmodell2K.STUNDE_S })));
            Assert.Equal(GebaeudeModellFehler.AbschnittsregelVerletzt, alt.Grund);

            m.Zuruecksetzen(AW_START, IW_START);
            double u0 = p.C_AW_Jk * m.ThetaMAw + p.C_IW_Jk * m.ThetaMIw;
            Stundenergebnis e = m.Schritt(in r);
            Stundenmuster muster = m.LetztesMuster;
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Abschnitte {0}, erster {1} {2:F1} s, zweiter {3}; Heizen {4:F3} W, Kühlen {5:F3} W, Raumluft {6:F4} °C",
                e.Abschnitte, muster.Folge[0], muster.Dauer[0], muster.Folge[1], e.HeizleistungW, e.KuehlleistungW, e.ThetaAirMittel));

            Assert.True(e.Abschnitte >= 2);
            Assert.Equal(Betriebsfall.HeizenGeregelt, muster.Folge[0]);
            Assert.Equal(Betriebsfall.Totband, muster.Folge[1]);
            Assert.InRange(muster.Dauer[0], 150.0, 250.0);
            Assert.True(e.HeizleistungW > 0.0);
            Assert.Equal(0.0, e.KuehlleistungW);
            Assert.True(e.ThetaAirMittel > 20.0, "Raumluft " + e.ThetaAirMittel);

            // Bilanz: Speicheränderung = Zuflüsse (wie Zonenmodell2KTests, Energiebilanz eines Blocks).
            double u1 = p.C_AW_Jk * e.ThetaMAwEnde + p.C_IW_Jk * e.ThetaMIwEnde;
            double zufluss = (r.ThetaEq - e.ThetaMAwMittel) / p.R_Rest_AWGruppe_KW + (r.ThetaOut - e.ThetaAirMittel) / p.R_ext_KW
                           + r.PhiRadAW + r.PhiRadIW + r.PhiConv + e.HeizleistungW - e.KuehlleistungW;
            double speicher = (u1 - u0) / Zonenmodell2K.STUNDE_S;
            Assert.True(Math.Abs(speicher - zufluss) < 1e-6 * (1.0 + Math.Abs(zufluss)),
                        "Bilanz: Speicher " + speicher + " W, Zufluss " + zufluss + " W");

            // Das Muster der korrigierten Stunde rechnet nachgerechnet bitgleich (Zonenschleife, Muster halten).
            m.Zuruecksetzen(AW_START, IW_START);
            Stundenergebnis nach = m.SchrittMitMuster(in r, muster);
            Assert.Equal(e.HeizleistungW, nach.HeizleistungW);
            Assert.Equal(e.ThetaAirMittel, nach.ThetaAirMittel);
        }

        /// <summary>
        /// Dieselbe Zone über einen Wintertag mit wechselnder Außentemperatur: Keine Stunde bricht ab,
        /// keine Stunde kühlt, die Raumluft fällt nie unter den Sollwert, jede Stunde ist bilanziert.
        /// </summary>
        [Fact]
        public void Ein_Wintertag_der_leichten_Zone_rechnet_ohne_Abbruch()
        {
            ErsatzparameterRC p = ZoneAmAbbruch();
            var m = new Zonenmodell2K(p);
            m.Zuruecksetzen(AW_START, IW_START);
            double summe = 0.0;
            for (int h = 0; h < 48; h++)
            {
                double aussen = 2.0 + 6.0 * Math.Sin(2.0 * Math.PI * h / 24.0);
                double gewinne = h % 24 >= 8 && h % 24 < 18 ? 1.0 : 0.2;
                var r = new Stundenrand(aussen, aussen + 10.0, 20.0, double.PositiveInfinity,
                                        4000.0 * gewinne, 2700.0 * gewinne, 1950.0 * gewinne);
                double u0 = p.C_AW_Jk * m.ThetaMAw + p.C_IW_Jk * m.ThetaMIw;
                Stundenergebnis e = m.Schritt(in r);
                double u1 = p.C_AW_Jk * e.ThetaMAwEnde + p.C_IW_Jk * e.ThetaMIwEnde;
                double zufluss = (r.ThetaEq - e.ThetaMAwMittel) / p.R_Rest_AWGruppe_KW + (r.ThetaOut - e.ThetaAirMittel) / p.R_ext_KW
                               + r.PhiRadAW + r.PhiRadIW + r.PhiConv + e.HeizleistungW - e.KuehlleistungW;
                Assert.True(Math.Abs((u1 - u0) / Zonenmodell2K.STUNDE_S - zufluss) < 1e-6 * (1.0 + Math.Abs(zufluss)), "Bilanz Stunde " + h);
                Assert.Equal(0.0, e.KuehlleistungW);
                Assert.True(e.HeizleistungW >= 0.0);
                Assert.True(e.ThetaAirMittel >= 20.0 - 1e-6, "Stunde " + h + ": " + e.ThetaAirMittel);
                summe += e.HeizleistungW;
            }
            Assert.True(summe > 0.0);
        }

        // =====================================================================
        //  Fensterzweig (A7a) bei hohem U_w
        // =====================================================================

        /// <summary>
        /// <b>A7a bei hohem U_w und kleiner Innenfläche:</b> R_Rest,AF = 1/(U·A)_w − R_1,AF − Flächenanteil an
        /// R_α,i wird negativ, sobald der Flächenanteil des inneren Übergangs (A_rad = A_IW &lt; A_AW,ges)
        /// größer ist als der innere Widerstand des Fensters — beim Probegebäude mit f_IW = 0,1 schon ab
        /// U_w 2,7. Der Rest ist nur eine Aufteilung: In Gl. (27) geht die Zweigsumme ein, und die bleibt
        /// 1/(U·A)_w. Der Zweig rechnet, die Gruppe bleibt im Regelfall mit positivem Rest, und der
        /// 2-K-Lauf bleibt stabil; mehr Fenster-U heißt mehr Heizwärme.
        /// </summary>
        [Theory]
        [InlineData(2.7)]
        [InlineData(5.8)]
        public void Ein_negativer_Fensterrest_bleibt_eine_gueltige_Aufteilung(double uFenster)
        {
            ErsatzparameterRC p = Klassenweg(uFenster, 0.1);
            Assert.True(p.R_Rest_AF_KW < 0.0, "R_Rest,AF " + p.R_Rest_AF_KW);
            double zweig = p.R_1_AF_KW + p.R_Rest_AF_KW + p.R_alphaInnen_KW * p.A_AW_gesamt_M2 / p.A_Fenster_M2;
            Assert.Equal(1.0 / p.UA_Fenster_WK, zweig, 9);
            Assert.Equal(AussenbauteilgruppeFall.Regelfall, p.Gruppenfall);
            Assert.True(p.R_Rest_AWGruppe_KW > 0.0);
            Assert.True(p.R_1_AWGruppe_KW > 0.0);

            double q = Heizwaerme(p);
            double qNiedrig = Heizwaerme(Klassenweg(1.3, 0.1));
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "U_w {0}: R_Rest,AF {1:E3} K/W, Heizwärme {2:F1} kWh (U_w 1,3: {3:F1} kWh)",
                                         uFenster, p.R_Rest_AF_KW, q, qNiedrig));
            Assert.True(q > qNiedrig);
        }

        /// <summary>Die Grenze bleibt benannt: Ein Fensterzweig mit nicht positiver Zweigsumme fällt mit <see cref="GebaeudeModellFehler.FensterzweigUngueltig"/>.</summary>
        [Fact]
        public void Ein_Fensterzweig_ohne_positive_Zweigsumme_ist_benannt()
        {
            var ex = Assert.Throws<GebaeudeModellException>(() => new ErsatzparameterRC(
                2.0e7, 5.0e7, 0.002, 0.02, 0.001, 0.01, 0.004, 0.002, 0.01, 100.0, 250.0, 40.0,
                r_1_AF_KW: 0.01, r_Rest_AF_KW: -1.0, a_Fenster_M2: 20.0, uA_Fenster_WK: 30.0));
            Assert.Equal(GebaeudeModellFehler.FensterzweigUngueltig, ex.Grund);
            ex = Assert.Throws<GebaeudeModellException>(() => new ErsatzparameterRC(
                2.0e7, 5.0e7, 0.002, 0.02, 0.001, 0.01, 0.004, 0.002, 0.01, 100.0, 250.0, 40.0,
                r_1_AF_KW: 0.01, r_Rest_AF_KW: double.NaN, a_Fenster_M2: 20.0, uA_Fenster_WK: 30.0));
            Assert.Equal(GebaeudeModellFehler.FensterzweigUngueltig, ex.Grund);
        }

        private static ErsatzparameterRC Klassenweg(double uFenster, double innenflaechenfaktor)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.k_Wert_Fenster = uFenster;
            g.Innenflaechenfaktor = innenflaechenfaktor;
            return ErsatzparameterRC.AusKlassenweg(GebaeudeModellEingang.Daten(g));
        }

        /// <summary>Heizwärme [kWh] über 200 Winterstunden (außen −5 °C, Sollwert 20 °C), jede Stunde endlich.</summary>
        private static double Heizwaerme(ErsatzparameterRC p)
        {
            var m = new Zonenmodell2K(p);
            m.Zuruecksetzen(20.0);
            Stundenrand r = Phantasiegebaeude.Rand(-5.0, 20.0, double.NaN);
            double kwh = 0.0;
            for (int h = 0; h < 200; h++)
            {
                Stundenergebnis e = m.Schritt(in r);
                Assert.True(double.IsFinite(e.HeizleistungW) && double.IsFinite(e.ThetaMAwEnde) && double.IsFinite(e.ThetaMIwEnde));
                Assert.Equal(20.0, e.ThetaAirMittel, 9);
                kwh += e.HeizleistungW / 1000.0;
            }
            return kwh;
        }
    }
}
