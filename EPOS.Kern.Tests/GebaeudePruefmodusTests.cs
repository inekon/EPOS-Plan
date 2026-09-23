using System;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Abnahmekriterium (2) der Gebäudesimulation im Prüfmodus</b> (Leitkonzept 10.4 (2),
    /// Umsetzungskonzept 1.4; Zahlenweg Rechenschritte Kapitel 9 am Gebäude 10645,
    /// „EFH-A-U-347s", 201 m²).
    ///
    /// <para><b>Was der Prüfmodus ist.</b> Das Bündel der Prototyp-Konventionen aus dem Kopf von
    /// Rechenschritte Kapitel 9: Fenster als masseloser Leitwert im Zweig zur Außenluft
    /// (R_ext = 1/(H_ve + (U·A)_w + Σψ·L)), kein Fensterzweig am Außenwandknoten, A_AW,opak in
    /// R_conv,AW und R_rad, a_kon = 0, Regelung ohne Kühlung und ohne Leistungsgrenze. Der
    /// Prüfmodus steht <b>hier</b>, als Parametersatz des Tests, und nicht als Schalter in
    /// <c>Simulation/Gebaeude/</c>: Seine isotropen <c>Sol_*</c>-Spalten sind Bezeichner des
    /// Altwegs, die die <c>Modultrennungswache</c> dort verbietet; der Löser selbst kennt keine
    /// Konvention, er rechnet den Satz, den er bekommt.</para>
    ///
    /// <para><b>Was geprüft wird.</b> Derselbe Löser, der im Lauf rechnet
    /// (<see cref="Zonenmodell2K"/>), gegen die gedruckten Zwischengrößen der Rechenschritte:
    /// Ersatzparameter (9.1), Eigenwerte beider Betriebsfälle (9.2), die Jahresspitzenstunde
    /// (9.4/9.5) und die Stunde mit Umschaltung (9.5). Die Toleranz ist die der Druckstelle —
    /// die Zustände vor der Stunde sind auf vier Nachkommastellen gedruckt. Der Jahreswert
    /// (9.6, 71 916 kWh) ist <b>nicht</b> Gegenstand: Er braucht die isotrope Klimaaufbereitung
    /// des Prototyps in UTC-Reihenfolge, und der Prototyp liegt nicht mehr vor; die 1e-6 relativ
    /// des Leitkonzepts setzen eine laufende Zweitimplementierung voraus.</para>
    /// </summary>
    public class GebaeudePruefmodusTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public GebaeudePruefmodusTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        // Rechenschritte 9.1 — die Eingänge des Gebäudes 10645.
        private const double NUTZFLAECHE = 201.0;
        private const double BAUWEISE_WH_K = 10_050.0;
        private const double A_AW_OPAK = 280.28 + 116.86 + 88.00 + 2.10;   // 487,24 m²
        private const double UA_OPAK = 1.84 * 280.28 + 0.80 * 116.86 + 0.80 * 88.00 + 3.50 * 2.10;
        private const double H_VE = 0.7 * 201.0 * 2.75 * 0.34;
        private const double UA_FENSTER = 2.8 * 45.06;
        private const double PSI_L = 0.15 * 160 + 0.40 * 202.02 + 0.70 * 2.50;

        /// <summary>Der Parametersatz des Prüfmodus (Rechenschritte 9.1, Prototyp-Konvention).</summary>
        private static ErsatzparameterRC Pruefsatz()
        {
            double cGes = BAUWEISE_WH_K * 3600.0;
            double aIw = 2.5 * NUTZFLAECHE;
            double r1Aw = 1.0 / (9.1 * A_AW_OPAK);
            double rRest = 1.0 / UA_OPAK - r1Aw - 0.13 / A_AW_OPAK;   // mit dem Abzug R_si/A (A4)
            return new ErsatzparameterRC(
                c_AW_Jk: 0.3 * cGes,
                c_IW_Jk: 0.7 * cGes,
                r_1_AW_KW: r1Aw,
                r_Rest_AW_KW: rRest,
                r_1_IW_KW: 1.0 / (9.1 * aIw),
                r_conv_AW_KW: 1.0 / (2.7 * A_AW_OPAK),
                r_conv_IW_KW: 1.0 / (2.7 * aIw),
                r_rad_KW: 1.0 / (5.0 * Math.Min(A_AW_OPAK, aIw)),
                r_ext_KW: 1.0 / (H_VE + UA_FENSTER + PSI_L),
                a_AW_opak_M2: A_AW_OPAK,
                a_IW_M2: aIw,
                summeUA_opak_WK: UA_OPAK);
        }

        [Fact]
        public void Die_Ersatzparameter_treffen_Rechenschritte_9_1()
        {
            ErsatzparameterRC p = Pruefsatz();
            Relativ(9.63358e-4, p.R_Rest_AW_KW, 1e-5);
            Relativ(2.25536e-4, p.R_1_AW_KW, 1e-5);
            Relativ(2.18687e-4, p.R_1_IW_KW, 1e-5);
            Relativ(7.60140e-4, p.R_conv_AW_KW, 1e-5);
            Relativ(7.37055e-4, p.R_conv_IW_KW, 1e-5);
            Relativ(4.10475e-4, p.R_rad_KW, 1e-5);
            Relativ(1.0 / 364.281, p.R_ext_KW, 1e-5);
            Relativ(686.953, p.SummeUA_opak_WK, 1e-6);
        }

        [Fact]
        public void Die_Eigenwerte_treffen_Rechenschritte_9_2()
        {
            var m = new Zonenmodell2K(Pruefsatz());

            double[] frei = m.EigenwerteImFall(Betriebsfall.Totband).OrderBy(x => x).ToArray();
            double[] geregelt = m.EigenwerteImFall(Betriebsfall.HeizenGeregelt).OrderBy(x => x).ToArray();
            _ausgabe.WriteLine(Text("frei {0:G6} / {1:G6}; geregelt {2:G6} / {3:G6}",
                                    frei[0], frei[1], geregelt[0], geregelt[1]));

            Relativ(-2.50723e-4, frei[0], 1e-5);
            Relativ(-2.67151e-5, frei[1], 1e-5);
            Relativ(-2.73175e-4, geregelt[0], 1e-5);
            Relativ(-6.11906e-5, geregelt[1], 1e-5);
        }

        /// <summary>
        /// Rechenschritte 9.4/9.5: die Jahresspitzenstunde 1 399 (UTC-Reihenfolge) — geregelt,
        /// ein Abschnitt, 36 440,60 W.
        /// </summary>
        [Fact]
        public void Die_Spitzenstunde_trifft_Rechenschritte_9_5()
        {
            var m = new Zonenmodell2K(Pruefsatz());
            m.Zuruecksetzen(4.3303, 12.8234);
            Stundenergebnis e = m.Schritt(new Stundenrand(
                thetaOut: -17.55, thetaEq: -15.3585, thetaSoll: 20.0, thetaMax: double.PositiveInfinity,
                phiRadAW: 113.72, phiRadIW: 117.28, phiConv: 231.0));

            _ausgabe.WriteLine(Text("Q {0:F2} W, θ_op {1:F3} °C, Ende {2:F4} / {3:F4}, Abschnitte {4}",
                                    e.HeizleistungW, e.ThetaOpMittel, e.ThetaMAwEnde, e.ThetaMIwEnde, e.Abschnitte));

            Assert.Equal(1, e.Abschnitte);
            Assert.Equal(20.0, e.ThetaAirMittel, 9);
            Absolut(36_440.60, e.HeizleistungW, 2.0);    // Zustände vorher vierstellig gedruckt
            Absolut(15.698, e.ThetaOpMittel, 0.002);
            Absolut(4.8281, e.ThetaMAwEnde, 0.0002);
            Absolut(12.9642, e.ThetaMIwEnde, 0.0002);
            Assert.Equal(0.0, e.KuehlleistungW);
        }

        /// <summary>
        /// Rechenschritte 9.5: die Stunde 2 221 mit Umschaltung — geregelt, dann frei;
        /// Blockmittel 207,66 W bei 20,012 °C.
        /// </summary>
        [Fact]
        public void Die_Umschaltstunde_trifft_Rechenschritte_9_5()
        {
            var m = new Zonenmodell2K(Pruefsatz());
            m.Zuruecksetzen(19.0205, 19.3320);
            Stundenergebnis e = m.Schritt(new Stundenrand(
                thetaOut: 18.57, thetaEq: 17.279, thetaSoll: 20.0, thetaMax: double.PositiveInfinity,
                phiRadAW: 2_954.73, phiRadIW: 3_047.22, phiConv: 231.0));

            _ausgabe.WriteLine(Text("Q {0:F2} W, θ_air {1:F3} °C, Abschnitte {2}",
                                    e.HeizleistungW, e.ThetaAirMittel, e.Abschnitte));

            Assert.Equal(2, e.Abschnitte);
            Absolut(207.66, e.HeizleistungW, 2.0);
            Absolut(20.012, e.ThetaAirMittel, 0.002);
        }

        private static void Relativ(double erwartet, double ist, double relativ)
        {
            double abw = Math.Abs(ist - erwartet) / Math.Abs(erwartet);
            Assert.True(abw <= relativ, Text("erwartet {0:G9}, ist {1:G9} (relativ {2:G3})", erwartet, ist, abw));
        }

        private static void Absolut(double erwartet, double ist, double grenze)
        {
            Assert.True(Math.Abs(ist - erwartet) <= grenze,
                        Text("erwartet {0:G9}, ist {1:G9} (Abstand {2:G3}, Grenze {3:G3})",
                             erwartet, ist, Math.Abs(ist - erwartet), grenze));
        }

        private static string Text(string format, params object[] werte)
            => string.Format(CultureInfo.InvariantCulture, format, werte);
    }
}
