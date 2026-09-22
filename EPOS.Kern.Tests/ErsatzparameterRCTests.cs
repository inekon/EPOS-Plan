using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die harten Prüfungen des Parametersatzes <see cref="ErsatzparameterRC"/> (Stufe G0,
    /// Umsetzungskonzept 1.3, Konzept 4.8): Jeder unphysikalische Wert bricht mit seinem
    /// benannten Grund ab, ein Klemmwert wird nie gesetzt. Dazu der Anschluss des
    /// Fensterzweigs an die Außenwände (E14). Alle Zahlen sind Phantasiewerte.
    /// </summary>
    public class ErsatzparameterRCTests
    {
        private static ErsatzparameterRC Satz(
            double cAW = 2.0e7, double cIW = 5.0e7, double r1AW = 0.002, double rRestAW = 0.02,
            double r1IW = 0.001, double rConvAW = 0.01, double rConvIW = 0.004, double rRad = 0.002,
            double rExt = 0.01, double aAW = 100.0, double aIW = 250.0, double ua = 40.0,
            double r1AF = double.PositiveInfinity, double rRestAF = double.PositiveInfinity,
            double aFenster = 0.0, double uaFenster = 0.0)
            => new ErsatzparameterRC(cAW, cIW, r1AW, rRestAW, r1IW, rConvAW, rConvIW, rRad, rExt,
                                     aAW, aIW, ua, r1AF, rRestAF, aFenster, uaFenster);

        private static void Grund(GebaeudeModellFehler erwartet, Func<ErsatzparameterRC> bau)
        {
            var ex = Assert.Throws<GebaeudeModellException>(() => bau());
            Assert.Equal(erwartet, ex.Grund);
            Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        }

        [Fact]
        public void Ein_nicht_positiver_Restwiderstand_der_Aussenwand_ist_benannt_und_wird_nicht_geklemmt()
        {
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: 0.0));
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: -0.001));
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: double.NaN));
        }

        [Fact]
        public void Kapazitaeten_Widerstaende_Flaechen_und_Leitwerte_werden_geprueft()
        {
            Grund(GebaeudeModellFehler.KapazitaetUngueltig, () => Satz(cAW: 0.0));
            Grund(GebaeudeModellFehler.KapazitaetUngueltig, () => Satz(cIW: double.PositiveInfinity));
            Grund(GebaeudeModellFehler.WiderstandUngueltig, () => Satz(r1AW: 0.0));
            Grund(GebaeudeModellFehler.WiderstandUngueltig, () => Satz(r1IW: -1.0));
            Grund(GebaeudeModellFehler.WiderstandUngueltig, () => Satz(rConvAW: double.NaN));
            Grund(GebaeudeModellFehler.WiderstandUngueltig, () => Satz(rRad: double.PositiveInfinity));
            Grund(GebaeudeModellFehler.WiderstandUngueltig, () => Satz(rExt: 0.0));
            Grund(GebaeudeModellFehler.FlaecheUngueltig, () => Satz(aIW: 0.0));
            Grund(GebaeudeModellFehler.FlaecheUngueltig, () => Satz(aAW: -1.0));
            Grund(GebaeudeModellFehler.FlaecheUngueltig, () => Satz(aAW: 0.0, aFenster: 0.0));
            Grund(GebaeudeModellFehler.LeitwertUngueltig, () => Satz(ua: -0.5));
            Grund(GebaeudeModellFehler.LeitwertUngueltig, () => Satz(uaFenster: double.NaN));
        }

        [Fact]
        public void Ein_halber_oder_nicht_positiver_Fensterzweig_ist_benannt()
        {
            Grund(GebaeudeModellFehler.FensterzweigUngueltig, () => Satz(r1AF: 0.01));
            Grund(GebaeudeModellFehler.FensterzweigUngueltig, () => Satz(r1AF: 0.01, rRestAF: 0.0));
            Grund(GebaeudeModellFehler.FensterzweigUngueltig, () => Satz(r1AF: -0.01, rRestAF: 0.05));
        }

        [Fact]
        public void Ohne_Lueftungszweig_ist_R_ext_unendlich_erlaubt()
        {
            ErsatzparameterRC p = Satz(rExt: double.PositiveInfinity);
            var m = new Zonenmodell2K(p);
            m.Zuruecksetzen(20.0);
            Stundenergebnis e = m.Schritt(Phantasiegebaeude.Rand(20.0, double.NaN, double.NaN));
            Assert.Equal(20.0, e.ThetaAirMittel, 12);
        }

        [Fact]
        public void Ohne_Fenster_ist_die_Gruppe_die_Wand()
        {
            ErsatzparameterRC p = Satz();
            Assert.Equal(p.R_1_AW_KW, p.R_1_AWGruppe_KW);
            Assert.Equal(p.R_Rest_AW_KW, p.R_Rest_AWGruppe_KW);
            Assert.Equal(100.0, p.A_AW_gesamt_M2);
        }

        [Fact]
        public void Der_Fensterzweig_wird_nach_den_Waenden_parallel_angeschlossen()
        {
            ErsatzparameterRC p = Satz(r1AF: 0.01, rRestAF: 0.05, aFenster: 20.0, uaFenster: 30.0);

            // R_1 der Gruppe: die beiden inneren Widerstände parallel.
            Assert.Equal(1.0 / (1.0 / 0.002 + 1.0 / 0.01), p.R_1_AWGruppe_KW, 15);
            // Gesamtweg der Gruppe: beide Zweige parallel; der Rest ist die Differenz.
            double rGes = 1.0 / (1.0 / 0.022 + 1.0 / 0.06);
            Assert.Equal(rGes - p.R_1_AWGruppe_KW, p.R_Rest_AWGruppe_KW, 15);
            Assert.True(p.R_Rest_AWGruppe_KW > 0.0);
            // Die Fensterfläche zählt zur Oberfläche der Gruppe (E14).
            Assert.Equal(120.0, p.A_AW_gesamt_M2);
            // Die Einzelwerte bleiben lesbar.
            Assert.Equal(0.02, p.R_Rest_AW_KW);
            Assert.Equal(0.05, p.R_Rest_AF_KW);
        }
    }
}
