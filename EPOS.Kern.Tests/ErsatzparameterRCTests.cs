using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die harten Prüfungen des Parametersatzes <see cref="ErsatzparameterRC"/> (Stufe G0,
    /// Umsetzungskonzept 1.3, Konzept 4.8): Jeder unphysikalische Wert bricht mit seinem
    /// benannten Grund ab. Dazu die Zusammenfassung von Wänden und Fenstern zur
    /// Außenbauteilgruppe nach VDI 6007 Blatt 1, Gl. (27)–(28c) (E14): Regelfall, Grenzfall
    /// (28a)/(28b), Untergrenze (28c), die Übergänge zwischen ihnen und der Löser im
    /// Grenzfall. Alle Zahlen sind Phantasiewerte.
    /// </summary>
    public class ErsatzparameterRCTests
    {
        /// <summary>Innerer Übergang des Grundsatzes: R_conv,AW = 0,01 ∥ R_rad = 0,002.</summary>
        private const double R_ALPHA_I = 1.0 / (1.0 / 0.01 + 1.0 / 0.002);

        /// <summary>
        /// Rest des Fensterzweigs (U·A = 30 W/K, R_1,AF = 0,01 K/W, 20 von 120 m²), so gebildet,
        /// dass die Zweigsumme R_1,AF + R_Rest,AF + Flächenanteil an R_α,i gleich 1/(U·A) ist —
        /// der äußere Übergang liegt damit im Rest (Rechenschritte A7a).
        /// </summary>
        private const double R_REST_AF = 1.0 / 30.0 - 0.01 - R_ALPHA_I * 120.0 / 20.0;

        private static ErsatzparameterRC Satz(
            double cAW = 2.0e7, double cIW = 5.0e7, double r1AW = 0.002, double rRestAW = 0.02,
            double r1IW = 0.001, double rConvAW = 0.01, double rConvIW = 0.004, double rRad = 0.002,
            double rExt = 0.01, double aAW = 100.0, double aIW = 250.0, double ua = 40.0,
            double r1AF = double.PositiveInfinity, double rRestAF = double.PositiveInfinity,
            double aFenster = 0.0, double uaFenster = 0.0, double rAlphaA = double.NaN)
            => new ErsatzparameterRC(cAW, cIW, r1AW, rRestAW, r1IW, rConvAW, rConvIW, rRad, rExt,
                                     aAW, aIW, ua, r1AF, rRestAF, aFenster, uaFenster, rAlphaA);

        /// <summary>Der Grundsatz mit Fensterzweig.</summary>
        private static ErsatzparameterRC MitFenster(double rRestAW = 0.02, double rAlphaA = double.NaN)
            => Satz(rRestAW: rRestAW, r1AF: 0.01, rRestAF: R_REST_AF, aFenster: 20.0, uaFenster: 30.0, rAlphaA: rAlphaA);

        private static void Grund(GebaeudeModellFehler erwartet, Func<ErsatzparameterRC> bau)
        {
            var ex = Assert.Throws<GebaeudeModellException>(() => bau());
            Assert.Equal(erwartet, ex.Grund);
            Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        }

        // =====================================================================
        //  Harte Prüfungen
        // =====================================================================

        [Fact]
        public void Ein_nicht_positiver_Restwiderstand_ohne_aeusseren_Uebergang_ist_benannt()
        {
            // Ohne R_α,A ist (28a) nicht prüfbar: kein Setzwert, sondern der benannte Grund.
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: 0.0));
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: -0.001));
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: double.NaN));
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: double.PositiveInfinity));
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
            Grund(GebaeudeModellFehler.FlaecheUngueltig, () => Satz(aAW: 0.0, r1AF: 0.01, rRestAF: 0.05, aFenster: 20.0));
            Grund(GebaeudeModellFehler.LeitwertUngueltig, () => Satz(ua: -0.5));
            Grund(GebaeudeModellFehler.LeitwertUngueltig, () => Satz(uaFenster: double.NaN));
        }

        [Fact]
        public void Ein_ungueltiger_aeusserer_Uebergangswiderstand_ist_benannt()
        {
            Grund(GebaeudeModellFehler.WiderstandUngueltig, () => Satz(rAlphaA: 0.0));
            Grund(GebaeudeModellFehler.WiderstandUngueltig, () => Satz(rAlphaA: -0.01));
            Grund(GebaeudeModellFehler.WiderstandUngueltig, () => Satz(rAlphaA: double.PositiveInfinity));
        }

        [Fact]
        public void Ein_halber_oder_nicht_positiver_Fensterzweig_ist_benannt()
        {
            Grund(GebaeudeModellFehler.FensterzweigUngueltig, () => Satz(r1AF: 0.01));
            Grund(GebaeudeModellFehler.FensterzweigUngueltig, () => Satz(r1AF: 0.01, rRestAF: 0.0));
            Grund(GebaeudeModellFehler.FensterzweigUngueltig, () => Satz(r1AF: -0.01, rRestAF: 0.05));
            // Ohne Fensterfläche hat der Zweig keinen Anteil am inneren Übergang.
            Grund(GebaeudeModellFehler.FensterzweigUngueltig, () => Satz(r1AF: 0.01, rRestAF: 0.05, aFenster: 0.0));
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

        // =====================================================================
        //  Regelfall, Gl. (27)/(28)
        // =====================================================================

        [Fact]
        public void Ohne_Fenster_ist_die_Gruppe_die_Wand()
        {
            ErsatzparameterRC p = Satz();
            Assert.Equal(AussenbauteilgruppeFall.Regelfall, p.Gruppenfall);
            Assert.False(p.R_1_Untergrenze28c);
            Assert.Equal(p.R_1_AW_KW, p.R_1_AWGruppe_KW);
            Assert.Equal(p.R_Rest_AW_KW, p.R_Rest_AWGruppe_KW);
            Assert.Equal(R_ALPHA_I, p.R_alphaInnen_KW, 15);
            Assert.Equal(0.002 + 0.02 + R_ALPHA_I, p.R_ges_AWGruppe_KW, 15);
            Assert.Equal(100.0, p.A_AW_gesamt_M2);
        }

        [Fact]
        public void Ohne_Fenster_aendert_ein_aeusserer_Uebergang_im_Regelfall_kein_Bit()
        {
            ErsatzparameterRC ohne = Satz(), mit = Satz(rAlphaA: 0.004);
            Assert.Equal(AussenbauteilgruppeFall.Regelfall, mit.Gruppenfall);
            Assert.Equal(BitConverter.DoubleToInt64Bits(ohne.R_1_AWGruppe_KW), BitConverter.DoubleToInt64Bits(mit.R_1_AWGruppe_KW));
            Assert.Equal(BitConverter.DoubleToInt64Bits(ohne.R_Rest_AWGruppe_KW), BitConverter.DoubleToInt64Bits(mit.R_Rest_AWGruppe_KW));
        }

        [Fact]
        public void Der_Fensterzweig_wird_nach_den_Waenden_nach_Gl_27_und_28_angeschlossen()
        {
            ErsatzparameterRC p = MitFenster();

            // R_1 der Gruppe: die beiden inneren Widerstände parallel, Fenster nach den Wänden.
            double r1 = 1.0 / (1.0 / 0.002 + 1.0 / 0.01);
            Assert.Equal(r1, p.R_1_AWGruppe_KW, 15);
            // (27): Leitwerte der Zweige addiert; jeder Zweig mit seinem Flächenanteil am
            // inneren Übergang (Wand 100 von 120 m², Fenster 20 von 120 m²).
            double rGesWand = 0.002 + 0.02 + R_ALPHA_I * 120.0 / 100.0;
            double rGesFenster = 0.01 + R_REST_AF + R_ALPHA_I * 120.0 / 20.0;
            // Die Zweigsumme des Fensters ist sein volles 1/(U·A), äußerer Übergang eingeschlossen.
            Assert.Equal(1.0 / 30.0, rGesFenster, 15);
            double rGes = 1.0 / (1.0 / rGesWand + 1.0 / rGesFenster);
            Assert.Equal(rGes, p.R_ges_AWGruppe_KW, 15);
            // (28): der Rest ist die Differenz; positiv, also Regelfall.
            Assert.Equal(rGes - r1 - R_ALPHA_I, p.R_Rest_AWGruppe_KW, 15);
            Assert.True(p.R_Rest_AWGruppe_KW > 0.0);
            Assert.Equal(AussenbauteilgruppeFall.Regelfall, p.Gruppenfall);
            // Die Fensterfläche zählt zur Oberfläche der Gruppe (E14).
            Assert.Equal(120.0, p.A_AW_gesamt_M2);
            // Die Einzelwerte bleiben lesbar, die Wandkapazität bleibt.
            Assert.Equal(0.02, p.R_Rest_AW_KW);
            Assert.Equal(R_REST_AF, p.R_Rest_AF_KW);
            Assert.Equal(2.0e7, p.C_AW_Jk);
        }

        [Fact]
        public void Mit_Fenstern_leitet_die_Gruppe_mehr_als_die_Wand_allein()
        {
            // Plausibilität: ein zusätzlicher Zweig senkt den Gesamtwiderstand.
            ErsatzparameterRC wand = Satz(), gruppe = MitFenster();
            Assert.True(gruppe.R_ges_AWGruppe_KW < wand.R_ges_AWGruppe_KW);
            Assert.True(gruppe.R_1_AWGruppe_KW + gruppe.R_Rest_AWGruppe_KW
                        < wand.R_1_AWGruppe_KW + wand.R_Rest_AWGruppe_KW);
        }

        // =====================================================================
        //  Grenzfall (28a)/(28b) und Untergrenze (28c)
        // =====================================================================

        [Fact]
        public void Liegt_R_ges_unter_dem_aeusseren_Uebergang_gilt_28a_und_28c()
        {
            // R_ges = 0,002 − 0,001 + R_α,i ≈ 0,00267 K/W < R_α,A = 0,01 K/W.
            ErsatzparameterRC p = Satz(rRestAW: -0.001, rAlphaA: 0.01);

            Assert.Equal(AussenbauteilgruppeFall.Grenzfall28a, p.Gruppenfall);
            Assert.Equal(0.002 - 0.001 + R_ALPHA_I, p.R_ges_AWGruppe_KW, 15);
            // (28a): der Rest ist der äußere Übergang.
            Assert.Equal(0.01, p.R_Rest_AWGruppe_KW);
            // (28b) ergäbe R_ges − R_α,A − R_α,i < 0; (28c) setzt die Untergrenze.
            Assert.True(p.R_1_Untergrenze28c);
            Assert.Equal(ErsatzparameterRC.R_1_NUMERISCH_NULL_KW, p.R_1_AWGruppe_KW);
            Assert.Equal(2.0e7, p.C_AW_Jk);
        }

        [Fact]
        public void Der_Grenzfall_28a_folgt_der_Bedingung_nicht_dem_Vorzeichen_des_Rests()
        {
            // Positiver Rest, aber R_ges ≈ 0,00467 K/W < R_α,A = 0,01 K/W: (28a) greift.
            ErsatzparameterRC p = Satz(rRestAW: 0.001, rAlphaA: 0.01);
            Assert.Equal(AussenbauteilgruppeFall.Grenzfall28a, p.Gruppenfall);
            Assert.Equal(0.01, p.R_Rest_AWGruppe_KW);
            Assert.True(p.R_1_Untergrenze28c);
        }

        [Fact]
        public void Der_Grenzfall_28a_gilt_auch_fuer_die_Gruppe_mit_Fenstern()
        {
            // Wandzweig knapp positiv, Gruppe weit unter dem äußeren Übergang.
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => MitFenster(rRestAW: -0.0035));
            ErsatzparameterRC p = MitFenster(rRestAW: -0.0035, rAlphaA: 0.01);

            double rGesWand = 0.002 - 0.0035 + R_ALPHA_I * 1.2;
            double rGesFenster = 0.01 + R_REST_AF + R_ALPHA_I * 6.0;
            Assert.Equal(1.0 / (1.0 / rGesWand + 1.0 / rGesFenster), p.R_ges_AWGruppe_KW, 15);
            Assert.Equal(AussenbauteilgruppeFall.Grenzfall28a, p.Gruppenfall);
            Assert.Equal(0.01, p.R_Rest_AWGruppe_KW);
            Assert.Equal(ErsatzparameterRC.R_1_NUMERISCH_NULL_KW, p.R_1_AWGruppe_KW);
        }

        [Fact]
        public void Die_Untergrenze_28c_gilt_auch_im_Regelfall()
        {
            ErsatzparameterRC p = Satz(r1AW: 1e-12);
            Assert.Equal(AussenbauteilgruppeFall.Regelfall, p.Gruppenfall);
            Assert.True(p.R_1_Untergrenze28c);
            Assert.Equal(ErsatzparameterRC.R_1_NUMERISCH_NULL_KW, p.R_1_AWGruppe_KW);
            Assert.Equal(0.02, p.R_Rest_AWGruppe_KW);
        }

        [Fact]
        public void Wo_die_Richtlinie_keinen_Fall_hat_bleibt_der_benannte_Fehler()
        {
            // R_ges ≈ 0,00267 K/W ≥ R_α,A = 0,001 K/W, Rest negativ: weder (28) noch (28a).
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: -0.001, rAlphaA: 0.001));
            // R_ges selbst nicht positiv: (27) liefert stets einen positiven Wert.
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: -0.01, rAlphaA: 0.01));
            // Mit Fenstern: Wandzweig mit nicht positivem Gesamtwiderstand.
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => MitFenster(rRestAW: -0.01, rAlphaA: 0.01));
        }

        // =====================================================================
        //  Übergänge zwischen den Fällen
        // =====================================================================

        [Fact]
        public void Ohne_Luecke_schliesst_28a_an_den_Regelfall_an_mit_einem_Sprung_um_R_alpha_i()
        {
            // R_α,A = 0,01 > R_1 + R_α,i: die Grenze R_ges = R_α,A liegt bei positivem Rest
            // R* = R_α,A − R_1 − R_α,i; darunter (28a), darüber Regelfall.
            const double rAlphaA = 0.01;
            double rStern = rAlphaA - 0.002 - R_ALPHA_I;
            double vorher = double.NaN;
            for (int i = -200; i <= 200; i++)
            {
                double rRest = rStern * (1.0 + i * 0.004);
                ErsatzparameterRC p = Satz(rRestAW: rRest, rAlphaA: rAlphaA);
                if (p.R_ges_AWGruppe_KW < rAlphaA)
                {
                    Assert.Equal(AussenbauteilgruppeFall.Grenzfall28a, p.Gruppenfall);
                    // Im Grenzfall ist die Gruppe konstant: Rest = R_α,A, R_1 = Setzwert.
                    Assert.Equal(rAlphaA, p.R_Rest_AWGruppe_KW);
                    Assert.Equal(ErsatzparameterRC.R_1_NUMERISCH_NULL_KW, p.R_1_AWGruppe_KW);
                }
                else
                {
                    Assert.Equal(AussenbauteilgruppeFall.Regelfall, p.Gruppenfall);
                    Assert.Equal(rRest, p.R_Rest_AWGruppe_KW);
                    // Im Regelfall stetig und monoton im Eingang.
                    if (!double.IsNaN(vorher)) Assert.True(p.R_Rest_AWGruppe_KW > vorher);
                    vorher = p.R_Rest_AWGruppe_KW;
                }
            }

            // Der Sprung an der Grenze: Der Durchgangswiderstand R_1 + R_Rest springt von
            // R_α,A − R_α,i (Regelfall) auf R_α,A + Setzwert (28a) — um R_α,i. Die Gruppe
            // leitet im Grenzfall also weniger, als R_ges sagt; beides passiv und positiv.
            ErsatzparameterRC oben = Satz(rRestAW: rStern * (1.0 + 1e-12), rAlphaA: rAlphaA);
            ErsatzparameterRC unten = Satz(rRestAW: rStern * (1.0 - 1e-12), rAlphaA: rAlphaA);
            Assert.Equal(AussenbauteilgruppeFall.Regelfall, oben.Gruppenfall);
            Assert.Equal(AussenbauteilgruppeFall.Grenzfall28a, unten.Gruppenfall);
            double durchOben = oben.R_1_AWGruppe_KW + oben.R_Rest_AWGruppe_KW;
            double durchUnten = unten.R_1_AWGruppe_KW + unten.R_Rest_AWGruppe_KW;
            Assert.Equal(rAlphaA - R_ALPHA_I, durchOben, 12);
            Assert.Equal(R_ALPHA_I, durchUnten - durchOben, 9);
            Assert.True(durchUnten + R_ALPHA_I > unten.R_ges_AWGruppe_KW);
        }

        [Fact]
        public void Mit_Luecke_liegt_zwischen_28a_und_dem_Regelfall_der_benannte_Fehler()
        {
            // R_α,A = 0,003 < R_1 + R_α,i ≈ 0,00367: (28a) bis R_Rest,AW < R_α,A − R_1 − R_α,i,
            // Regelfall ab R_Rest,AW > 0, dazwischen kein Fall der Richtlinie.
            const double rAlphaA = 0.003;
            double grenze28a = rAlphaA - 0.002 - R_ALPHA_I;
            Assert.True(grenze28a < 0.0);

            Assert.Equal(AussenbauteilgruppeFall.Grenzfall28a, Satz(rRestAW: grenze28a * 1.01, rAlphaA: rAlphaA).Gruppenfall);
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: grenze28a * 0.99, rAlphaA: rAlphaA));
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: grenze28a * 0.5, rAlphaA: rAlphaA));
            Grund(GebaeudeModellFehler.RRestAwNichtPositiv, () => Satz(rRestAW: 0.0, rAlphaA: rAlphaA));

            // Der Regelfall läuft stetig gegen null: kein Setzwert, der Rest ist der Eingang.
            foreach (double rRest in new[] { 1e-3, 1e-5, 1e-8 })
            {
                ErsatzparameterRC p = Satz(rRestAW: rRest, rAlphaA: rAlphaA);
                Assert.Equal(AussenbauteilgruppeFall.Regelfall, p.Gruppenfall);
                Assert.Equal(rRest, p.R_Rest_AWGruppe_KW);
            }
        }

        [Fact]
        public void Mit_Fenstern_ist_der_Regelfall_stetig_und_schliesst_an_28a_an()
        {
            // Wandrest von positiv bis tief negativ: Regelfall mit stetig fallendem Rest,
            // dann (28a) mit konstanter Gruppe; nie ein Rest ≤ 0 ohne Fehler.
            const double rAlphaA = 0.01;
            double vorher = double.PositiveInfinity;
            bool grenzfallErreicht = false;
            for (double rRest = 0.02; rRest > -0.0039; rRest -= 0.0001)
            {
                ErsatzparameterRC p;
                try { p = MitFenster(rRestAW: rRest, rAlphaA: rAlphaA); }
                catch (GebaeudeModellException ex)
                {
                    Assert.Equal(GebaeudeModellFehler.RRestAwNichtPositiv, ex.Grund);
                    Assert.False(grenzfallErreicht, "nach (28a) darf kein Regelfall-Fehler mehr folgen");
                    continue;
                }
                Assert.True(p.R_Rest_AWGruppe_KW > 0.0);
                Assert.True(p.R_1_AWGruppe_KW > 0.0);
                if (p.Gruppenfall == AussenbauteilgruppeFall.Grenzfall28a)
                {
                    grenzfallErreicht = true;
                    Assert.True(p.R_ges_AWGruppe_KW < rAlphaA);
                    Assert.Equal(rAlphaA, p.R_Rest_AWGruppe_KW);
                }
                else
                {
                    Assert.False(grenzfallErreicht);
                    Assert.True(p.R_Rest_AWGruppe_KW < vorher);
                    vorher = p.R_Rest_AWGruppe_KW;
                }
            }
            Assert.True(grenzfallErreicht);
        }

        // =====================================================================
        //  Der Löser im Grenzfall
        // =====================================================================

        [Fact]
        public void Im_Grenzfall_28a_bleibt_das_Netz_passiv_und_stationaer_richtig()
        {
            ErsatzparameterRC p = Satz(rRestAW: -0.001, rAlphaA: 0.01);
            var m = new Zonenmodell2K(p);
            foreach (double lambda in m.Eigenwerte)
            {
                Assert.False(double.IsNaN(lambda));
                Assert.True(lambda < 0.0);
            }

            m.Zuruecksetzen(10.0);
            Stundenrand r = Phantasiegebaeude.Rand(thetaOut: -5.0, thetaSoll: 20.0, thetaMax: double.NaN, phiConv: 300.0);
            Stundenergebnis e = default;
            for (int h = 0; h < 4000; h++) e = m.Schritt(in r);

            double rInnen = p.R_conv_AW_KW * (p.R_conv_IW_KW + p.R_rad_KW)
                          / (p.R_conv_AW_KW + p.R_conv_IW_KW + p.R_rad_KW);
            double leitwert = 1.0 / (p.R_Rest_AWGruppe_KW + p.R_1_AWGruppe_KW + rInnen) + 1.0 / p.R_ext_KW;
            double erwartet = leitwert * 25.0 - 300.0;
            Assert.True(Math.Abs(erwartet - e.HeizleistungW) < 1e-6 * erwartet,
                        "erwartet " + erwartet + " W, gerechnet " + e.HeizleistungW + " W");
            Assert.Equal(20.0, e.ThetaAirMittel, 9);
            // Der Massenknoten liegt praktisch am Oberflächenknoten.
            Assert.True(Math.Abs(e.ThetaMAwMittel - e.ThetaSAwMittel) < 1e-6);
        }

        [Fact]
        public void Im_Grenzfall_28a_ist_die_Energiebilanz_eines_Blocks_geschlossen()
        {
            ErsatzparameterRC p = MitFenster(rRestAW: -0.0035, rAlphaA: 0.01);
            Assert.Equal(AussenbauteilgruppeFall.Grenzfall28a, p.Gruppenfall);
            var m = new Zonenmodell2K(p);
            double gRest = 1.0 / p.R_Rest_AWGruppe_KW, gExt = 1.0 / p.R_ext_KW;

            Stundenrand[] stunden =
            {
                Phantasiegebaeude.Rand(-5.0, double.NaN, double.NaN, 100.0, 300.0, 200.0),
                Phantasiegebaeude.Rand(-5.0, 21.0, 26.0, 100.0, 300.0, 200.0, strahlungsanteil: 0.3),
                Phantasiegebaeude.Rand(5.0, 21.0, 26.0, 1500.0, 4000.0, 800.0),
                Phantasiegebaeude.Rand(28.0, 21.0, 24.0, 800.0, 2500.0, 1500.0),
                new Stundenrand(2.0, 9.0, 21.0, 26.0, 400.0, 900.0, 300.0,
                                double.NaN, double.NaN, 0.0, 0.0, 0.0),
            };
            foreach (Stundenrand r in stunden)
            {
                m.Zuruecksetzen(17.0, 23.0);
                double u0 = p.C_AW_Jk * m.ThetaMAw + p.C_IW_Jk * m.ThetaMIw;
                Stundenergebnis e = m.Schritt(in r);
                double u1 = p.C_AW_Jk * e.ThetaMAwEnde + p.C_IW_Jk * e.ThetaMIwEnde;

                double zufluss = gRest * (r.ThetaEq - e.ThetaMAwMittel) + (gExt + r.ZusatzleitwertWK) * (r.ThetaOut - e.ThetaAirMittel)
                               + r.PhiRadAW + r.PhiRadIW + r.PhiConv + e.HeizleistungW - e.KuehlleistungW;
                double speicher = (u1 - u0) / Zonenmodell2K.STUNDE_S;
                Assert.True(Math.Abs(speicher - zufluss) < 1e-6 * (1.0 + Math.Abs(zufluss)),
                            "Bilanz: Speicher " + speicher + " W, Zufluss " + zufluss + " W");
            }
        }
    }
}
