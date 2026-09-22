using System;
using System.Diagnostics;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Ein Gebäude aus runden PHANTASIEWERTEN für die Rechenproben des 2-K-Lösers (Stufe G0
    /// der Gebäudesimulation). <b>Keine Zahl hier stammt aus einer Richtlinie</b> — weder aus
    /// den Testräumen noch aus den Ergebnistabellen; die Normfälle laufen nur lokal über die
    /// Vorrichtung <see cref="Normzahlen"/>.
    /// </summary>
    internal static class Phantasiegebaeude
    {
        /// <summary>Der Grundsatz; mit Fenstern ein Fensterzweig im Außenwandpfad.</summary>
        internal static ErsatzparameterRC Standard(bool mitFenster = false, double rExt = 0.01,
                                                   double cAW = 2.0e7, double cIW = 5.0e7)
            => new ErsatzparameterRC(
                c_AW_Jk: cAW,
                c_IW_Jk: cIW,
                r_1_AW_KW: 0.002,
                r_Rest_AW_KW: 0.02,
                r_1_IW_KW: 0.001,
                r_conv_AW_KW: 0.01,
                r_conv_IW_KW: 0.004,
                r_rad_KW: 0.002,
                r_ext_KW: rExt,
                a_AW_opak_M2: 100.0,
                a_IW_M2: 250.0,
                summeUA_opak_WK: 40.0,
                r_1_AF_KW: mitFenster ? 0.01 : double.PositiveInfinity,
                // Zweigsumme R_1,AF + R_Rest,AF + Flächenanteil an R_conv,AW ∥ R_rad (hier 0,01)
                // = 1/(U·A) des Fensters: der äußere Übergang liegt im Rest (Rechenschritte A7a).
                r_Rest_AF_KW: mitFenster ? 1.0 / 30.0 - 0.01 - 0.01 : double.PositiveInfinity,
                a_Fenster_M2: mitFenster ? 20.0 : 0.0,
                uA_Fenster_WK: mitFenster ? 30.0 : 0.0);

        /// <summary>Eine Stunde mit θ_eq = θ_out.</summary>
        internal static Stundenrand Rand(double thetaOut, double thetaSoll, double thetaMax,
                                         double phiRadAW = 0.0, double phiRadIW = 0.0, double phiConv = 0.0,
                                         double heizMaxW = double.NaN, double kuehlMaxW = double.NaN,
                                         double strahlungsanteil = 0.0, double kuehlungInnen = 0.0,
                                         double zusatzleitwert = 0.0)
            => new Stundenrand(thetaOut, thetaOut, thetaSoll, thetaMax, phiRadAW, phiRadIW, phiConv,
                               heizMaxW, kuehlMaxW, strahlungsanteil, kuehlungInnen, zusatzleitwert);
    }

    /// <summary>
    /// Die reinen Rechenproben des 2-K-Lösers <see cref="Zonenmodell2K"/> — ohne Datenbank,
    /// ohne Normzahlen (Rechenschritte 10.4, Umsetzungskonzept 1.9): Eigenwerte,
    /// Übergangsmatrizen, stationärer Grenzfall, Energiebilanz, Determinismus, Kappung,
    /// Vorzeichen, Abschnittsdeckel und die Gegenprobe des Blockmittels gegen eine
    /// unabhängige feine Zeitintegration.
    /// </summary>
    public class Zonenmodell2KTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public Zonenmodell2KTests(ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
        }

        // =====================================================================
        //  Eigenwerte und Übergangsmatrizen
        // =====================================================================

        [Fact]
        public void Eigenwerte_sind_reell_und_negativ_in_jedem_Betriebsfall()
        {
            foreach (bool fenster in new[] { false, true })
            foreach (double rExt in new[] { 0.01, double.PositiveInfinity })
            {
                var m = new Zonenmodell2K(Phantasiegebaeude.Standard(fenster, rExt));
                Pruefen(m.Eigenwerte);
                Pruefen(m.EigenwerteImFall(Betriebsfall.HeizenGeregelt, 0.0));
                Pruefen(m.EigenwerteImFall(Betriebsfall.HeizenGeregelt, 0.3));
                Pruefen(m.EigenwerteImFall(Betriebsfall.KuehlenGeregelt, 0.0));
                Pruefen(m.EigenwerteImFall(Betriebsfall.KuehlenGeregelt, 1.0));
            }

            static void Pruefen(double[] l)
            {
                Assert.Equal(2, l.Length);
                Assert.All(l, w => Assert.True(w < 0.0 && !double.IsNaN(w), "Eigenwert " + w));
                Assert.True(l[0] >= l[1], "der langsamere Eigenwert zuerst");
            }
        }

        [Fact]
        public void Die_Systemmatrix_ist_voll_besetzt()
        {
            // Der Strukturfehler, den die Richtlinie ausschließt: eine diagonale Matrix.
            Matrix2 a = new Zonenmodell2K(Phantasiegebaeude.Standard()).SystemmatrixFrei;
            Assert.True(a.M12 > 0.0);
            Assert.True(a.M21 > 0.0);
            Assert.True(a.M11 < 0.0 && a.M22 < 0.0);
        }

        [Theory]
        [InlineData(3600.0)]
        [InlineData(1.7)]
        [InlineData(250000.0)]
        public void Gamma_und_Psi_folgen_aus_Phi(double tau)
        {
            Matrix2 a = new Zonenmodell2K(Phantasiegebaeude.Standard()).SystemmatrixFrei;
            var rechner = new Uebergangsrechner(a);
            Uebergang u = rechner.Bei(tau);
            Matrix2 ainv = a.Inverse();

            MatrixGleich(ExpReferenz(a, tau), u.Phi, 1e-12);
            MatrixGleich(ainv * (u.Phi - Matrix2.Einheit), u.Gamma, 1e-9 * tau);
            MatrixGleich(ainv * (u.Gamma - tau * Matrix2.Einheit), u.Psi, 1e-9 * tau * tau);
        }

        [Fact]
        public void Zusammenfallende_Eigenwerte_rechnen_die_Ableitungsform()
        {
            // Nicht diagonalisierbar: A = [[λ, c], [0, λ]], exp(A·τ) = e^(λτ)·[[1, cτ], [0, 1]].
            const double lambda = -1e-4, c = 2e-5, tau = 3600.0;
            var a = new Matrix2(lambda, c, 0.0, lambda);
            var rechner = new Uebergangsrechner(a);
            Assert.True(rechner.Zusammenfallend);

            Uebergang u = rechner.Bei(tau);
            double e = Math.Exp(lambda * tau);
            MatrixGleich(new Matrix2(e, c * tau * e, 0.0, e), u.Phi, 1e-14);
            Matrix2 ainv = a.Inverse();
            MatrixGleich(ainv * (u.Phi - Matrix2.Einheit), u.Gamma, 1e-9 * tau);
            MatrixGleich(ainv * (u.Gamma - tau * Matrix2.Einheit), u.Psi, 1e-9 * tau * tau);

            // Fast zusammenfallend, knapp jenseits der Schwelle: beide Zweige treffen dieselbe Zahl.
            var nah = new Matrix2(lambda, c, 1e-22, lambda * (1.0 + 1e-6));
            var rNah = new Uebergangsrechner(nah);
            Assert.False(rNah.Zusammenfallend);
            MatrixGleich(ExpReferenz(nah, tau), rNah.Bei(tau).Phi, 1e-10);
        }

        [Fact]
        public void Die_Reihen_nahe_null_gehen_stetig_in_die_geschlossenen_Formen_ueber()
        {
            foreach (double rand in new[] { -1.0, 1.0 })
            {
                double innen = rand * (1.0 - 1e-9), aussen = rand * (1.0 + 1e-9);
                double ei = Math.Exp(innen), ea = Math.Exp(aussen);
                Assert.Equal(Uebergangsrechner.G1(innen, ei), Uebergangsrechner.G1(aussen, ea), 8);
                Assert.Equal(Uebergangsrechner.G2(innen, ei), Uebergangsrechner.G2(aussen, ea), 8);
                Assert.Equal(Uebergangsrechner.G1Strich(innen, ei), Uebergangsrechner.G1Strich(aussen, ea), 8);
                Assert.Equal(Uebergangsrechner.G2Strich(innen, ei), Uebergangsrechner.G2Strich(aussen, ea), 8);
            }

            // Grenzwerte bei z = 0: 1, 1/2, 1/2, 1/6 — und keine Auslöschung bei winzigem z.
            Assert.Equal(1.0, Uebergangsrechner.G1(1e-14, Math.Exp(1e-14)), 12);
            Assert.Equal(0.5, Uebergangsrechner.G2(-1e-14, Math.Exp(-1e-14)), 12);
            Assert.Equal(0.5, Uebergangsrechner.G1Strich(1e-14, Math.Exp(1e-14)), 12);
            Assert.Equal(1.0 / 6.0, Uebergangsrechner.G2Strich(1e-14, Math.Exp(1e-14)), 12);
        }

        // =====================================================================
        //  Stationärer Grenzfall
        // =====================================================================

        [Theory]
        [InlineData(false, 0.0)]
        [InlineData(true, 0.0)]
        [InlineData(false, 500.0)]
        [InlineData(true, 500.0)]
        public void Stationaer_ist_die_Heizlast_Leitwert_mal_Temperaturdifferenz(bool fenster, double konvektiveGewinne)
        {
            ErsatzparameterRC p = Phantasiegebaeude.Standard(fenster);
            var m = new Zonenmodell2K(p);
            m.Zuruecksetzen(10.0);
            Stundenrand r = Phantasiegebaeude.Rand(thetaOut: -5.0, thetaSoll: 20.0, thetaMax: double.NaN,
                                                   phiConv: konvektiveGewinne);
            Stundenergebnis e = default;
            for (int h = 0; h < 4000; h++) e = m.Schritt(in r);

            // Luft → θ_s,AW: R_conv,AW parallel (R_conv,IW + R_rad); dann R_1 und R_Rest der Gruppe.
            double rInnen = p.R_conv_AW_KW * (p.R_conv_IW_KW + p.R_rad_KW)
                          / (p.R_conv_AW_KW + p.R_conv_IW_KW + p.R_rad_KW);
            double leitwert = 1.0 / (p.R_Rest_AWGruppe_KW + p.R_1_AWGruppe_KW + rInnen) + 1.0 / p.R_ext_KW;
            double erwartet = leitwert * 25.0 - konvektiveGewinne;

            Assert.Equal(erwartet, e.HeizleistungW, 6);
            Assert.Equal(0.0, e.KuehlleistungW);
            Assert.Equal(20.0, e.ThetaAirMittel, 9);
            if (fenster)
            {
                // Der Fensterzweig ist im Leitwert sichtbar.
                double ohne = 1.0 / (p.R_Rest_AW_KW + p.R_1_AW_KW + rInnen) + 1.0 / p.R_ext_KW;
                Assert.True(leitwert > ohne + 1.0);
            }
        }

        [Fact]
        public void Die_Regelung_haelt_den_Sollwert_auf_1e9_K()
        {
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard());
            m.Zuruecksetzen(15.0);
            for (int h = 0; h < 48; h++)
            {
                Stundenrand r = Phantasiegebaeude.Rand(thetaOut: -10.0 + 0.5 * h, thetaSoll: 21.0, thetaMax: double.NaN,
                                                       phiRadAW: 50.0, phiRadIW: 150.0, phiConv: 100.0,
                                                       strahlungsanteil: 0.3);
                Stundenergebnis e = m.Schritt(in r);
                Assert.True(e.HeizleistungW > 0.0);
                Assert.Equal(1, e.Abschnitte);
                Assert.True(Math.Abs(e.ThetaAirMittel - 21.0) < 1e-9, "Stunde " + h + ": " + e.ThetaAirMittel);
            }
        }

        // =====================================================================
        //  Energiebilanz, Determinismus
        // =====================================================================

        [Fact]
        public void Die_Energiebilanz_eines_Blocks_ist_geschlossen()
        {
            ErsatzparameterRC p = Phantasiegebaeude.Standard(mitFenster: true);
            var m = new Zonenmodell2K(p);
            double gRest = 1.0 / p.R_Rest_AWGruppe_KW, gExt = 1.0 / p.R_ext_KW;

            foreach (Stundenrand r in Probestunden())
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

        [Fact]
        public void Nach_Zuruecksetzen_rechnet_derselbe_Lauf_byte_gleich()
        {
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard(mitFenster: true));
            Stundenergebnis[] erster = Lauf(m, 400);
            Stundenergebnis[] zweiter = Lauf(m, 400);
            Stundenergebnis[] fremd = Lauf(new Zonenmodell2K(Phantasiegebaeude.Standard(mitFenster: true)), 400);

            for (int i = 0; i < erster.Length; i++)
            {
                BitGleich(erster[i], zweiter[i], i);
                BitGleich(erster[i], fremd[i], i);
            }
        }

        // =====================================================================
        //  Kappung, Grenzen, Vorzeichen
        // =====================================================================

        [Fact]
        public void Die_Kappung_liefert_Kuehlleistung_und_haelt_die_obere_Grenze()
        {
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard());
            m.Zuruecksetzen(24.0);
            Stundenrand r = Phantasiegebaeude.Rand(thetaOut: 30.0, thetaSoll: 20.0, thetaMax: 26.0,
                                                   phiRadIW: 3000.0, phiConv: 1000.0);
            Stundenergebnis e = default;
            for (int h = 0; h < 200; h++) e = m.Schritt(in r);

            Assert.True(e.KuehlleistungW > 0.0);
            Assert.Equal(0.0, e.HeizleistungW);
            Assert.True(e.LastW < 0.0);
            Assert.True(Math.Abs(e.ThetaAirMittel - 26.0) < 1e-9);
        }

        [Fact]
        public void Die_Leistungsgrenzen_halten()
        {
            // Heizgrenze: Die Heizung liefert genau ihre Grenze, die Luft bleibt darunter.
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard());
            m.Zuruecksetzen(20.0);
            Stundenergebnis h = default;
            for (int i = 0; i < 100; i++)
                h = m.Schritt(Phantasiegebaeude.Rand(-10.0, 20.0, double.NaN, heizMaxW: 1500.0));
            Assert.Equal(1500.0, h.HeizleistungW, 9);
            Assert.True(h.ThetaAirMittel < 20.0);

            // Kühlgrenze: dieselbe Lage oben.
            m.Zuruecksetzen(26.0);
            Stundenergebnis k = default;
            for (int i = 0; i < 100; i++)
                k = m.Schritt(Phantasiegebaeude.Rand(32.0, 20.0, 26.0, phiConv: 4000.0, kuehlMaxW: 2000.0));
            Assert.Equal(2000.0, k.KuehlleistungW, 9);
            Assert.True(k.ThetaAirMittel > 26.0);
            Assert.Equal(0.0, k.HeizleistungW);
        }

        [Fact]
        public void Vorzeichen_Heizen_und_Kuehlen_sind_beide_positiv_die_Last_traegt_das_Vorzeichen()
        {
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard());

            m.Zuruecksetzen(20.0);
            Stundenergebnis heiz = m.Schritt(Phantasiegebaeude.Rand(0.0, 20.0, 26.0));
            Assert.True(heiz.HeizleistungW > 0.0);
            Assert.Equal(0.0, heiz.KuehlleistungW);
            Assert.Equal(heiz.HeizleistungW, heiz.LastW);

            m.Zuruecksetzen(26.0);
            Stundenergebnis kuehl = m.Schritt(Phantasiegebaeude.Rand(35.0, 20.0, 26.0, phiConv: 2000.0));
            Assert.True(kuehl.KuehlleistungW > 0.0);
            Assert.Equal(0.0, kuehl.HeizleistungW);
            Assert.Equal(-kuehl.KuehlleistungW, kuehl.LastW);

            // Ohne Sollwert und ohne Grenze: freier Lauf, keine Leistung.
            m.Zuruecksetzen(20.0);
            Stundenergebnis frei = m.Schritt(Phantasiegebaeude.Rand(0.0, double.NaN, double.NaN, phiConv: 300.0));
            Assert.Equal(0.0, frei.HeizleistungW);
            Assert.Equal(0.0, frei.KuehlleistungW);
        }

        [Fact]
        public void Eine_Umschaltstunde_traegt_Heiz_und_Kuehlanteil_getrennt()
        {
            // Sollwert und obere Grenze fallen zusammen: vor dem Umschalten heizt die
            // Zone, danach kühlt sie. Beide Anteile bleiben stehen.
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard(cAW: 2.0e6, cIW: 2.0e6));
            m.Zuruecksetzen(15.0);
            Stundenergebnis e = m.Schritt(Phantasiegebaeude.Rand(20.0, 21.0, 21.0, phiRadIW: 4000.0));
            Assert.True(e.Abschnitte >= 2, "Abschnitte " + e.Abschnitte);
            Assert.True(e.HeizleistungW > 0.0);
            Assert.True(e.KuehlleistungW > 0.0);
        }

        // =====================================================================
        //  Blockmittel gegen feine Unterteilung
        // =====================================================================

        [Fact]
        public void Das_Blockmittel_trifft_die_feine_Zeitintegration()
        {
            ErsatzparameterRC p = Phantasiegebaeude.Standard(mitFenster: true, cAW: 4.0e6, cIW: 6.0e6);
            var m = new Zonenmodell2K(p);
            int mehrAbschnitte = 0;

            foreach (Stundenrand r in Probestunden())
            {
                foreach ((double a, double b) start in new[] { (17.0, 23.0), (21.0, 21.0), (25.0, 27.0) })
                {
                    m.Zuruecksetzen(start.a, start.b);
                    Stundenergebnis e = m.Schritt(in r);
                    if (e.Abschnitte > 1) mehrAbschnitte++;
                    Feinrechnung.Stunde(p, in r, start.a, start.b, 0.25,
                                        out double heiz, out double kuehl, out double air, out double x1, out double x2);

                    string wo = " (Start " + start.a + "/" + start.b + ", Abschnitte " + e.Abschnitte + ")";
                    Assert.True(Math.Abs(e.HeizleistungW - heiz) < 1e-3, "Heizen " + e.HeizleistungW + " gegen " + heiz + wo);
                    Assert.True(Math.Abs(e.KuehlleistungW - kuehl) < 1e-3, "Kühlen " + e.KuehlleistungW + " gegen " + kuehl + wo);
                    Assert.True(Math.Abs(e.ThetaAirMittel - air) < 1e-6, "Luft " + e.ThetaAirMittel + " gegen " + air + wo);
                    Assert.True(Math.Abs(e.ThetaMAwEnde - x1) < 1e-6, "Masse AW " + e.ThetaMAwEnde + " gegen " + x1 + wo);
                    Assert.True(Math.Abs(e.ThetaMIwEnde - x2) < 1e-6, "Masse IW " + e.ThetaMIwEnde + " gegen " + x2 + wo);
                }
            }

            // Die Probe muss Umschaltstunden enthalten, sonst prüft sie die Bisektion nicht.
            Assert.True(mehrAbschnitte >= 2, "nur " + mehrAbschnitte + " Umschaltstunden");
        }

        // =====================================================================
        //  Benannte Fehler des Lösers
        // =====================================================================

        [Fact]
        public void Der_Abschnittsdeckel_ist_ein_benannter_Fehler_ohne_Teilstunde()
        {
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard(cAW: 2.0e6, cIW: 2.0e6), "Probegebäude", abschnittsdeckel: 1);
            m.Zuruecksetzen(15.0);
            var ex = Assert.Throws<GebaeudeModellException>(
                () => m.Schritt(Phantasiegebaeude.Rand(20.0, 21.0, 21.0, phiRadIW: 4000.0)));
            Assert.Equal(GebaeudeModellFehler.AbschnittsdeckelErreicht, ex.Grund);
            Assert.Contains("Probegebäude", ex.Message);
            Assert.Contains(nameof(Betriebsfall.HeizenGeregelt), ex.Message);
            Assert.Equal(15.0, m.ThetaMAw);
            Assert.Equal(15.0, m.ThetaMIw);
        }

        [Fact]
        public void Ein_ungueltiger_Rand_ist_ein_benannter_Fehler()
        {
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard());
            Stundenrand[] falsch =
            {
                Phantasiegebaeude.Rand(double.NaN, 20.0, 26.0),
                Phantasiegebaeude.Rand(0.0, 20.0, 19.0),
                Phantasiegebaeude.Rand(0.0, 20.0, 26.0, heizMaxW: -1.0),
                Phantasiegebaeude.Rand(0.0, 20.0, 26.0, strahlungsanteil: 1.5),
                Phantasiegebaeude.Rand(0.0, 20.0, 26.0, phiConv: double.PositiveInfinity),
                Phantasiegebaeude.Rand(0.0, 20.0, 26.0, zusatzleitwert: -1.0),
            };
            foreach (Stundenrand r in falsch)
            {
                var ex = Assert.Throws<GebaeudeModellException>(() => m.Schritt(in r));
                Assert.Equal(GebaeudeModellFehler.RandUngueltig, ex.Grund);
            }
        }

        // =====================================================================
        //  Rechenzeit (nur Ausweis, keine Schwelle)
        // =====================================================================

        [Fact]
        public void Rechenzeit_je_Gebaeude_und_Jahr_wird_gemessen()
        {
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard(mitFenster: true));
            Messen(m, mitKuehlung: false, out _, out _);   // Warmlauf

            double msOhne = Messen(m, mitKuehlung: false, out int umschaltOhne, out double heizOhne);
            double msMit = Messen(m, mitKuehlung: true, out int umschaltMit, out double heizMit);

            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Rechenzeit 8760 Stunden ohne Kühlung: {0:F2} ms ({1} Stunden mit Umschaltung)", msOhne, umschaltOhne));
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Rechenzeit 8760 Stunden mit Kappung, Heiz- und Kühlgrenze: {0:F2} ms ({1} Stunden mit Umschaltung)", msMit, umschaltMit));
            Assert.True(heizOhne > 0.0 && heizMit > 0.0);
        }

        private static double Messen(Zonenmodell2K m, bool mitKuehlung, out int umschaltstunden, out double heizsumme)
        {
            Stundenrand[] jahr = Jahr(mitKuehlung);
            m.Zuruecksetzen(20.0);
            umschaltstunden = 0;
            heizsumme = 0.0;
            var uhr = Stopwatch.StartNew();
            for (int h = 0; h < jahr.Length; h++)
            {
                Stundenergebnis e = m.Schritt(in jahr[h]);
                if (e.Abschnitte > 1) umschaltstunden++;
                heizsumme += e.HeizleistungW;
            }
            uhr.Stop();
            return uhr.Elapsed.TotalMilliseconds;
        }

        /// <summary>Ein synthetisches Jahr: Jahres- und Tagesgang außen, Tagesgewinne, Nachtabsenkung.</summary>
        private static Stundenrand[] Jahr(bool mitKuehlung)
        {
            var jahr = new Stundenrand[8760];
            for (int h = 0; h < jahr.Length; h++)
            {
                int stunde = h % 24;
                double aussen = 10.0 - 12.0 * Math.Cos(2.0 * Math.PI * h / 8760.0) - 5.0 * Math.Cos(2.0 * Math.PI * (stunde - 3) / 24.0);
                bool tag = stunde >= 6 && stunde < 22;
                double sonne = Math.Max(0.0, Math.Sin(Math.PI * (stunde - 6) / 12.0)) * (1500.0 - 800.0 * Math.Cos(2.0 * Math.PI * h / 8760.0));
                jahr[h] = new Stundenrand(
                    aussen, aussen + 0.002 * sonne, tag ? 21.0 : 17.0,
                    mitKuehlung ? 26.0 : double.NaN,
                    0.2 * sonne, 0.7 * sonne, 0.1 * sonne + (tag ? 300.0 : 100.0),
                    heizleistungMaxW: mitKuehlung ? 4000.0 : double.NaN,
                    kuehlleistungMaxW: mitKuehlung ? 3000.0 : double.NaN,
                    heizungStrahlungsanteil: 0.3);
            }
            return jahr;
        }

        // =====================================================================
        //  Werkzeug
        // =====================================================================

        /// <summary>Stunden, die jeden Betriebsfall und jede Umschaltung einmal treffen.</summary>
        private static Stundenrand[] Probestunden() => new[]
        {
            Phantasiegebaeude.Rand(-5.0, double.NaN, double.NaN, 100.0, 300.0, 200.0),                    // freier Lauf
            Phantasiegebaeude.Rand(-5.0, 21.0, 26.0, 100.0, 300.0, 200.0, strahlungsanteil: 0.3),       // Heizen
            Phantasiegebaeude.Rand(5.0, 21.0, 26.0, 1500.0, 4000.0, 800.0),                              // Heizen → Totband
            Phantasiegebaeude.Rand(-5.0, 21.0, 26.0, heizMaxW: 1200.0),                                  // Heizgrenze
            Phantasiegebaeude.Rand(28.0, 21.0, 24.0, 800.0, 2500.0, 1500.0),                             // Totband → Kühlen
            Phantasiegebaeude.Rand(30.0, 21.0, 24.0, 800.0, 2500.0, 1500.0, kuehlMaxW: 1000.0),          // Kühlgrenze
            Phantasiegebaeude.Rand(24.0, 21.0, 24.0, 800.0, 6000.0, 800.0, kuehlungInnen: 1.0),          // Flächenkühlung
            Phantasiegebaeude.Rand(15.0, 21.0, 21.0, 0.0, 5000.0, 0.0),                                  // Heizen → Kühlen
            Phantasiegebaeude.Rand(12.0, 21.0, 25.0, 300.0, 3000.0, 600.0, zusatzleitwert: 80.0),        // Lüftung der Stunde
            Phantasiegebaeude.Rand(12.0, double.NaN, double.NaN, 300.0, 900.0, 600.0, zusatzleitwert: 40.0), // frei mit Lüftung
        };

        private static Stundenergebnis[] Lauf(Zonenmodell2K m, int stunden)
        {
            m.Zuruecksetzen(18.0);
            Stundenrand[] rand = Probestunden();
            var erg = new Stundenergebnis[stunden];
            for (int h = 0; h < stunden; h++) erg[h] = m.Schritt(in rand[h % rand.Length]);
            return erg;
        }

        private static void BitGleich(Stundenergebnis a, Stundenergebnis b, int stunde)
        {
            double[] x = Felder(a), y = Felder(b);
            for (int i = 0; i < x.Length; i++)
                Assert.True(BitConverter.DoubleToInt64Bits(x[i]) == BitConverter.DoubleToInt64Bits(y[i]),
                            "Stunde " + stunde + ", Feld " + i);
            Assert.Equal(a.Abschnitte, b.Abschnitte);
        }

        private static double[] Felder(Stundenergebnis e) => new[]
        {
            e.HeizleistungW, e.KuehlleistungW, e.ThetaAirMittel, e.ThetaOpMittel, e.ThetaSAwMittel,
            e.ThetaSIwMittel, e.ThetaMAwMittel, e.ThetaMIwMittel, e.ThetaMAwEnde, e.ThetaMIwEnde,
        };

        private static void MatrixGleich(Matrix2 erwartet, Matrix2 ist, double toleranz)
        {
            Assert.True(Math.Abs(erwartet.M11 - ist.M11) <= toleranz, "M11 " + erwartet.M11 + " gegen " + ist.M11);
            Assert.True(Math.Abs(erwartet.M12 - ist.M12) <= toleranz, "M12 " + erwartet.M12 + " gegen " + ist.M12);
            Assert.True(Math.Abs(erwartet.M21 - ist.M21) <= toleranz, "M21 " + erwartet.M21 + " gegen " + ist.M21);
            Assert.True(Math.Abs(erwartet.M22 - ist.M22) <= toleranz, "M22 " + erwartet.M22 + " gegen " + ist.M22);
        }

        /// <summary>exp(A·τ) unabhängig von der Sylvester-Formel: Skalieren, Taylor, Quadrieren.</summary>
        private static Matrix2 ExpReferenz(Matrix2 a, double tau)
        {
            Matrix2 m = tau * a;
            double norm = Math.Abs(m.M11) + Math.Abs(m.M12) + Math.Abs(m.M21) + Math.Abs(m.M22);
            int quadrierungen = 0;
            while (norm > 0.25) { norm *= 0.5; quadrierungen++; }
            m = Math.Pow(0.5, quadrierungen) * m;
            Matrix2 summe = Matrix2.Einheit, glied = Matrix2.Einheit;
            for (int k = 1; k <= 25; k++)
            {
                glied = (1.0 / k) * (glied * m);
                summe = summe + glied;
            }
            for (int i = 0; i < quadrierungen; i++) summe = summe * summe;
            return summe;
        }
    }

    /// <summary>
    /// Eine UNABHÄNGIGE Zweitrechnung einer Blockstunde für die Gegenprobe: dieselben
    /// Knotenbilanzen, aber ohne Elimination und ohne Übergangsmatrizen — je Teilschritt
    /// werden die drei algebraischen Knoten mit Gauß gelöst, die Regelung entscheidet
    /// augenblicklich, integriert wird mit dem klassischen Runge-Kutta-Verfahren vierter
    /// Ordnung und kleinem Schritt.
    /// </summary>
    internal static class Feinrechnung
    {
        internal static void Stunde(ErsatzparameterRC p, in Stundenrand r, double x1Start, double x2Start, double dt,
                                    out double heizW, out double kuehlW, out double airMittel,
                                    out double x1Ende, out double x2Ende)
        {
            int n = (int)Math.Round(Zonenmodell2K.STUNDE_S / dt);
            double h = Zonenmodell2K.STUNDE_S / n;
            double x1 = x1Start, x2 = x2Start;
            double akkH = 0.0, akkK = 0.0, akkA = 0.0;
            Stundenrand rand = r;
            for (int i = 0; i < n; i++)
            {
                Auswerten(p, in rand, x1, x2, out double d11, out double d21, out double q1, out double a1);
                Auswerten(p, in rand, x1 + 0.5 * h * d11, x2 + 0.5 * h * d21, out double d12, out double d22, out double q2, out double a2);
                Auswerten(p, in rand, x1 + 0.5 * h * d12, x2 + 0.5 * h * d22, out double d13, out double d23, out double q3, out double a3);
                Auswerten(p, in rand, x1 + h * d13, x2 + h * d23, out double d14, out double d24, out double q4, out double a4);
                x1 += h / 6.0 * (d11 + 2.0 * d12 + 2.0 * d13 + d14);
                x2 += h / 6.0 * (d21 + 2.0 * d22 + 2.0 * d23 + d24);
                akkH += h / 6.0 * (Math.Max(q1, 0) + 2.0 * Math.Max(q2, 0) + 2.0 * Math.Max(q3, 0) + Math.Max(q4, 0));
                akkK += h / 6.0 * (Math.Max(-q1, 0) + 2.0 * Math.Max(-q2, 0) + 2.0 * Math.Max(-q3, 0) + Math.Max(-q4, 0));
                akkA += h / 6.0 * (a1 + 2.0 * a2 + 2.0 * a3 + a4);
            }
            heizW = akkH / Zonenmodell2K.STUNDE_S;
            kuehlW = akkK / Zonenmodell2K.STUNDE_S;
            airMittel = akkA / Zonenmodell2K.STUNDE_S;
            x1Ende = x1;
            x2Ende = x2;
        }

        /// <summary>Ableitungen, Leistung und Lufttemperatur im Zustand (x1, x2) bei idealer Regelung.</summary>
        private static void Auswerten(ErsatzparameterRC p, in Stundenrand r, double x1, double x2,
                                      out double dx1, out double dx2, out double q, out double air)
        {
            double wAW = p.A_AW_gesamt_M2 / (p.A_AW_gesamt_M2 + p.A_IW_M2);
            double wIW = 1.0 - wAW;
            double s = r.HeizungStrahlungsanteil, k = r.KuehlungAnteilInnenflaeche;
            double s1, s2;

            bool fertig = false;
            q = 0.0; air = double.NaN; s1 = s2 = double.NaN;
            if (!double.IsNaN(r.ThetaSoll))
            {
                Geregelt(p, in r, x1, x2, r.ThetaSoll, s * wAW, s * wIW, 1.0 - s, out s1, out s2, out q);
                if (q > 0.0)
                {
                    if (!double.IsNaN(r.HeizleistungMaxW) && q > r.HeizleistungMaxW)
                    {
                        q = r.HeizleistungMaxW;
                        Frei(p, in r, x1, x2, q, s * wAW, s * wIW, 1.0 - s, out s1, out s2, out air);
                    }
                    else air = r.ThetaSoll;
                    fertig = true;
                }
            }
            if (!fertig && !double.IsNaN(r.ThetaMax))
            {
                Geregelt(p, in r, x1, x2, r.ThetaMax, 0.0, k, 1.0 - k, out s1, out s2, out q);
                if (q < 0.0)
                {
                    if (!double.IsNaN(r.KuehlleistungMaxW) && -q > r.KuehlleistungMaxW)
                    {
                        q = -r.KuehlleistungMaxW;
                        Frei(p, in r, x1, x2, q, 0.0, k, 1.0 - k, out s1, out s2, out air);
                    }
                    else air = r.ThetaMax;
                    fertig = true;
                }
            }
            if (!fertig)
            {
                q = 0.0;
                Frei(p, in r, x1, x2, 0.0, 0.0, 0.0, 1.0, out s1, out s2, out air);
            }

            dx1 = ((r.ThetaEq - x1) / p.R_Rest_AWGruppe_KW + (s1 - x1) / p.R_1_AWGruppe_KW) / p.C_AW_Jk;
            dx2 = ((s2 - x2) / p.R_1_IW_KW) / p.C_IW_Jk;
        }

        /// <summary>Luft fest, Leistung unbekannt: Unbekannte (θ_s,AW, θ_s,IW, Φ).</summary>
        private static void Geregelt(ErsatzparameterRC p, in Stundenrand r, double x1, double x2, double theta,
                                     double eAW, double eIW, double eLuft, out double s1, out double s2, out double q)
        {
            Leitwerte(p, out double g1, out double g2, out double gcA, out double gcI, out double gr, out double ge);
            ge += r.ZusatzleitwertWK;
            double[,] m =
            {
                { -(g1 + gr + gcA), gr, eAW, -(g1 * x1 + gcA * theta + r.PhiRadAW) },
                { gr, -(g2 + gr + gcI), eIW, -(g2 * x2 + gcI * theta + r.PhiRadIW) },
                { gcA, gcI, eLuft, (gcA + gcI + ge) * theta - ge * r.ThetaOut - r.PhiConv },
            };
            double[] z = Gauss(m);
            s1 = z[0]; s2 = z[1]; q = z[2];
        }

        /// <summary>Leistung fest, Luft unbekannt: Unbekannte (θ_s,AW, θ_s,IW, θ_air).</summary>
        private static void Frei(ErsatzparameterRC p, in Stundenrand r, double x1, double x2, double q,
                                 double eAW, double eIW, double eLuft, out double s1, out double s2, out double air)
        {
            Leitwerte(p, out double g1, out double g2, out double gcA, out double gcI, out double gr, out double ge);
            ge += r.ZusatzleitwertWK;
            double[,] m =
            {
                { -(g1 + gr + gcA), gr, gcA, -(g1 * x1 + r.PhiRadAW + eAW * q) },
                { gr, -(g2 + gr + gcI), gcI, -(g2 * x2 + r.PhiRadIW + eIW * q) },
                { gcA, gcI, -(gcA + gcI + ge), -(ge * r.ThetaOut + r.PhiConv + eLuft * q) },
            };
            double[] z = Gauss(m);
            s1 = z[0]; s2 = z[1]; air = z[2];
        }

        private static void Leitwerte(ErsatzparameterRC p, out double g1, out double g2, out double gcA,
                                      out double gcI, out double gr, out double ge)
        {
            g1 = 1.0 / p.R_1_AWGruppe_KW;
            g2 = 1.0 / p.R_1_IW_KW;
            gcA = 1.0 / p.R_conv_AW_KW;
            gcI = 1.0 / p.R_conv_IW_KW;
            gr = 1.0 / p.R_rad_KW;
            ge = double.IsPositiveInfinity(p.R_ext_KW) ? 0.0 : 1.0 / p.R_ext_KW;
        }

        /// <summary>Gauß mit Spaltenpivot für ein 3×3-System in erweiterter Form.</summary>
        private static double[] Gauss(double[,] m)
        {
            const int n = 3;
            for (int k = 0; k < n; k++)
            {
                int piv = k;
                for (int i = k + 1; i < n; i++) if (Math.Abs(m[i, k]) > Math.Abs(m[piv, k])) piv = i;
                for (int j = 0; j <= n; j++) { double t = m[k, j]; m[k, j] = m[piv, j]; m[piv, j] = t; }
                for (int i = k + 1; i < n; i++)
                {
                    double f = m[i, k] / m[k, k];
                    for (int j = k; j <= n; j++) m[i, j] -= f * m[k, j];
                }
            }
            var z = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double s = m[i, n];
                for (int j = i + 1; j < n; j++) s -= m[i, j] * z[j];
                z[i] = s / m[i, i];
            }
            return z;
        }
    }
}
