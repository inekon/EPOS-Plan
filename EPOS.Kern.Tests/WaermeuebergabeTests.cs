using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Rechenproben der Wärmeübergabe</b> (Anlagenkopplung AK1; 3.1 bis 3.4, 10.2, 10.3,
    /// 11.1) — ohne Datenbank, ohne Normzahlen. Das Beispiel „Gebäude A" aus 10.3 trägt neutrale,
    /// runde Werte: 10 kW, 55/45 °C, 20 °C innen, −12 °C außen, Radiator mit n = 1,3.
    ///
    /// <para><b>Rundung des Papiers.</b> 10.3 rechnet von Hand mit gerundeten Zwischenwerten
    /// (zwei Nachkommastellen); die Proben halten die gedruckten Zahlen deshalb auf 0,01 und die
    /// geschlossenen Aussagen — Auslegungspunkt, Teillast, Bilanz — auf 1e-9. Fall 3 ergibt
    /// genau 8,844 kW; das Papier druckt 8,85.</para>
    /// </summary>
    public class WaermeuebergabeTests
    {
        /// <summary>Beispiel A in W (Faktor 1 000) oder in kW (Faktor 1).</summary>
        private static Uebergabekennwerte BeispielA(double faktor = 1000.0)
            => new Uebergabekennwerte(10.0 * faktor, 1.3, 55.0, 45.0, 20.0);

        private static Heizkurve KurveA() => new Heizkurve(BeispielA(), -12.0, 0.0, 1.0);

        // =====================================================================
        //  Das Zahlenbeispiel 10.3
        // =====================================================================

        [Fact]
        public void Zahlenbeispiel_Fall1_Die_Heizkurve_trifft_den_Auslegungspunkt()
        {
            Uebergabekennwerte k = BeispielA();
            Heizkurve kurve = KurveA();
            Assert.Equal(30.0, k.DeltaThetaMNK, 12);
            Assert.Equal(1000.0, k.WHWK, 9);

            Assert.Equal(55.0, kurve.VorlaufC(20.0, -12.0), 9);
            Assert.Equal(45.0, kurve.RuecklaufSollC(20.0, -12.0), 9);
            double phi = Waermeuebergabe.LeistungOffenW(k, 55.0, 20.0);
            Assert.True(Math.Abs(phi - 10000.0) <= 1e-6, "Φ_ue = " + phi + " W statt der Nennleistung");
            Assert.Equal(45.0, Waermeuebergabe.RuecklaufC(k, 55.0, phi), 9);
        }

        [Fact]
        public void Zahlenbeispiel_Fall2_Die_Uebergabe_liefert_genau_die_Teillast()
        {
            Uebergabekennwerte k = BeispielA();
            Heizkurve kurve = KurveA();
            double v = kurve.VorlaufC(20.0, 0.0);
            double rSoll = kurve.RuecklaufSollC(20.0, 0.0);
            Assert.Equal(0.625, kurve.Relativlast(20.0, 0.0), 12);
            Assert.True(Math.Abs(v - 44.03) <= 0.01, "Vorlauf " + v);
            Assert.True(Math.Abs(rSoll - 37.78) <= 0.01, "Rücklauf (Soll) " + rSoll);

            // Die Kurve ist so gebaut, dass die Übergabe im eingeschwungenen Zustand genau φ·Φ_N
            // liefert und der gerechnete Rücklauf die Sollkurve trifft.
            double phi = Waermeuebergabe.LeistungOffenW(k, v, 20.0);
            Assert.True(Math.Abs(phi - 6250.0) <= 1e-6, "Φ_ue = " + phi + " W");
            Assert.Equal(rSoll, Waermeuebergabe.RuecklaufC(k, v, phi), 9);
        }

        [Fact]
        public void Zahlenbeispiel_Fall3_Aufheizen_nach_der_Absenkung_ist_gesaettigt()
        {
            Uebergabekennwerte k = BeispielA();
            double v = KurveA().VorlaufC(20.0, -5.0);
            Assert.True(Math.Abs(v - 48.72) <= 0.01, "Vorlauf " + v);

            double phi = Waermeuebergabe.LeistungOffenW(k, v, 17.0);
            Assert.True(Math.Abs(phi - 8850.0) <= 10.0, "Φ = " + phi + " W (Papier: 8,85 kW)");
            Assert.True(Math.Abs(phi - 8844.27) <= 0.1, "Φ = " + phi + " W (geschlossen: 8,844 kW)");
            double r = Waermeuebergabe.RuecklaufC(k, v, phi);
            Assert.True(Math.Abs(r - 39.87) <= 0.01, "Rücklauf " + r);
            double thetaM = v - phi / (2.0 * k.WHWK);
            Assert.True(Math.Abs(thetaM - 44.30) <= 0.01, "θ_m " + thetaM);

            // Gesättigt: θ_soll − θ_i = 3 K liegt über Xp = 1 K, also y = 1 und G = G_H.
            Assert.Equal(1.0, Waermeuebergabe.Stellgrad(20.0, 1.0, 17.0));
            double gH = Waermeuebergabe.SteigungOffenWK(k, phi, v, 17.0);
            Assert.True(Math.Abs(gH - 348.0) <= 0.5, "G_H = " + gH + " W/K (Papier: 0,348 kW/K)");
            double thetaH = 17.0 + phi / gH;
            Assert.True(Math.Abs(thetaH - 42.4) <= 0.05, "θ_H " + thetaH);
            Assert.True(Math.Abs(gH * (thetaH - 17.0) - phi) <= 1e-6, "Probe G_H·(θ_H − θ_i) = Φ");
        }

        // =====================================================================
        //  Proben aus 11.1
        // =====================================================================

        [Fact]
        public void Die_Heizkurve_faellt_streng_monoton_mit_der_Aussentemperatur()
        {
            foreach (double n in new[] { 1.0, 1.1, 1.3, 1.4, 1.6 })
            foreach (double steilheit in new[] { 0.2, 1.0, 3.0 })
            foreach (double niveau in new[] { -5.0, 0.0, 5.0 })
            {
                var k = new Uebergabekennwerte(10000.0, n, 55.0, 45.0, 20.0);
                var kurve = new Heizkurve(k, -12.0, niveau, steilheit);
                double vorher = double.PositiveInfinity;
                for (double aussen = -11.5; aussen < 19.9; aussen += 0.5)
                {
                    double v = kurve.VorlaufC(20.0, aussen);
                    Assert.False(double.IsNaN(v), "unter der Heizgrenze ist die Kurve an");
                    if (v < k.AuslegungVorlaufC && vorher < k.AuslegungVorlaufC)
                        Assert.True(v < vorher, "n " + n + ", S " + steilheit + ", N " + niveau + ", θ_out " + aussen);
                    Assert.True(v <= k.AuslegungVorlaufC, "oben an θ_V,N gekappt");
                    vorher = v;
                }
                Assert.True(double.IsNaN(kurve.VorlaufC(20.0, 20.0)), "an der Heizgrenze ist die Kurve aus");
                Assert.True(double.IsNaN(kurve.VorlaufC(20.0, 25.0)), "über der Heizgrenze ist die Kurve aus");
            }
        }

        /// <summary>Ein zulässiger Parametersatz nach den Prüfregeln (9.1), deterministisch gezogen.</summary>
        private static Uebergabekennwerte Zufallssatz(Random z, double faktor = 1000.0)
        {
            double n = 1.0 + 0.6 * z.NextDouble();
            double raum = 15.0 + 11.0 * z.NextDouble();
            double vor = Math.Max(25.0, raum + 1.0) + (90.0 - Math.Max(25.0, raum + 1.0)) * z.NextDouble();
            double rueck = raum + 0.2 + (vor - raum - 0.4) * z.NextDouble();
            double phiN = Math.Pow(10.0, -1.0 + 5.0 * z.NextDouble()) * faktor;   // 0,1 kW … 10 MW
            return new Uebergabekennwerte(phiN, n, vor, rueck, raum);
        }

        [Fact]
        public void Die_Heizkreisbilanz_schliesst_fuer_zehntausend_zufaellige_Saetze()
        {
            var z = new Random(20260924);
            for (int i = 0; i < 10000; i++)
            {
                Uebergabekennwerte k = Zufallssatz(z);
                double thetaI = -10.0 + 40.0 * z.NextDouble();
                double thetaV = thetaI + 0.01 + 80.0 * z.NextDouble();
                double phi = Waermeuebergabe.LeistungOffenW(k, thetaV, thetaI);
                Assert.True(phi > 0.0 && !double.IsInfinity(phi), "Satz " + i);
                double r = Waermeuebergabe.RuecklaufC(k, thetaV, phi);
                Assert.True(Math.Abs(phi - k.WHWK * (thetaV - r)) <= 1e-6 + 1e-12 * phi, "Bilanz, Satz " + i);
                double rest = k.PhiNW * Math.Pow((thetaV - phi / (2.0 * k.WHWK) - thetaI) / k.DeltaThetaMNK, k.Exponent) - phi;
                Assert.True(Math.Abs(rest) <= Waermeuebergabe.NEWTON_ABBRUCH_W + Waermeuebergabe.NEWTON_ABBRUCH_RELATIV * phi,
                            "Übergabegleichung, Satz " + i + ": Rest " + rest + " W");
            }
        }

        [Fact]
        public void Newton_konvergiert_in_hoechstens_acht_Schritten()
        {
            var z = new Random(17);
            int hoechste = 0;
            for (int i = 0; i < 10000; i++)
            {
                Uebergabekennwerte k = Zufallssatz(z);
                double a = 0.01 + 100.0 * z.NextDouble();
                // Mit Antwort der Raumluft: s von 0 bis 0,1 K/W (ein sehr kleiner Raum).
                double s = z.NextDouble() < 0.5 ? 0.0 : Math.Pow(10.0, -8.0 + 7.0 * z.NextDouble());
                Waermeuebergabe.Loesen(k.PhiNW, k.Exponent, a, 0.5 / k.WHWK + s, k.DeltaThetaMNK, out int schritte);
                Assert.True(schritte <= Waermeuebergabe.NEWTON_SCHRITTE_MAX, "Satz " + i + ": " + schritte + " Schritte");
                hoechste = Math.Max(hoechste, schritte);
            }
            Assert.True(hoechste >= 1, "die Probe muss Schritte zählen");
        }

        [Fact]
        public void Der_Sekantenleitwert_trifft_die_Leistung_und_ist_positiv()
        {
            var z = new Random(3);
            for (int i = 0; i < 2000; i++)
            {
                Uebergabekennwerte k = Zufallssatz(z);
                double thetaI = 10.0 + 15.0 * z.NextDouble();
                double thetaV = thetaI + 0.5 + 50.0 * z.NextDouble();
                double phi = Waermeuebergabe.LeistungOffenW(k, thetaV, thetaI);
                double g = Waermeuebergabe.SteigungOffenWK(k, phi, thetaV, thetaI);
                Assert.True(g > 0.0, "G > 0, solange θ_m > θ_i");
                double thetaH = thetaI + phi / g;
                Assert.True(Math.Abs(g * (thetaH - thetaI) - phi) <= 1e-6 + 1e-12 * phi, "G·(θ_H − θ_i) = Φ");
            }
        }

        [Fact]
        public void Der_Leitwert_je_Saettigungszustand_trifft_die_numerische_Ableitung()
        {
            Uebergabekennwerte k = BeispielA();
            const double V = 48.0, SOLL = 20.0, XP = 1.0, H = 1e-4;

            // Gesättigt (θ_i ≤ θ_soll − Xp): G = G_H.
            double iG = 17.0;
            double ableitungG = -(Waermeuebergabe.LeistungKennlinieW(k, V, SOLL, XP, iG + H)
                                  - Waermeuebergabe.LeistungKennlinieW(k, V, SOLL, XP, iG - H)) / (2.0 * H);
            double gH = Waermeuebergabe.SteigungOffenWK(k, Waermeuebergabe.LeistungOffenW(k, V, iG), V, iG);
            Assert.True(Math.Abs(ableitungG - gH) <= 1e-6 * gH + 1e-3, "gesättigt: " + ableitungG + " gegen G_H " + gH);

            // Regelbereich (0 < y < 1): G = Φ_ue,max/Xp + y·G_H.
            double iR = 19.5;
            double ableitungR = -(Waermeuebergabe.LeistungKennlinieW(k, V, SOLL, XP, iR + H)
                                  - Waermeuebergabe.LeistungKennlinieW(k, V, SOLL, XP, iR - H)) / (2.0 * H);
            double phiMax = Waermeuebergabe.LeistungOffenW(k, V, iR);
            double y = Waermeuebergabe.Stellgrad(SOLL, XP, iR);
            double gR = phiMax / XP + y * Waermeuebergabe.SteigungOffenWK(k, phiMax, V, iR);
            Assert.True(Math.Abs(ableitungR - gR) <= 1e-6 * gR + 1e-3, "Regelbereich: " + ableitungR + " gegen " + gR);

            // Die Probe fällt, sobald beide Bereiche denselben Leitwert benutzten.
            double gHimBand = Waermeuebergabe.SteigungOffenWK(k, phiMax, V, iR);
            Assert.True(gR > 5.0 * gHimBand, "Im Regelbereich ist der Reglerbeitrag das Hauptglied.");
        }

        [Fact]
        public void Der_Ruecklauf_steigt_wenn_die_Leistung_gedrosselt_wird()
        {
            Uebergabekennwerte k = BeispielA();
            const double V = 48.0, SOLL = 20.0, XP = 2.0, I = 19.0;
            double offen = Waermeuebergabe.LeistungOffenW(k, V, I);
            double gedrosselt = Waermeuebergabe.LeistungKennlinieW(k, V, SOLL, XP, I);
            Assert.True(gedrosselt < offen);
            double rOffen = Waermeuebergabe.RuecklaufC(k, V, offen);
            double rGedrosselt = Waermeuebergabe.RuecklaufC(k, V, gedrosselt);
            Assert.True(rGedrosselt > rOffen, "konstanter Massenstrom: weniger Leistung, höherer Rücklauf");
            Assert.True(Math.Abs(gedrosselt - k.WHWK * (V - rGedrosselt)) <= 1e-6);
        }

        [Fact]
        public void Kilowatt_und_Watt_ergeben_dasselbe_Ergebnis()
        {
            var z = new Random(11);
            for (int i = 0; i < 1000; i++)
            {
                int saat = z.Next();
                Uebergabekennwerte w = Zufallssatz(new Random(saat), 1000.0);
                Uebergabekennwerte kw = Zufallssatz(new Random(saat), 1.0);
                double thetaI = 10.0 + 15.0 * z.NextDouble();
                double thetaV = thetaI + 0.5 + 50.0 * z.NextDouble();
                double phiW = Waermeuebergabe.LeistungOffenW(w, thetaV, thetaI);
                double phiKw = Waermeuebergabe.LeistungOffenW(kw, thetaV, thetaI);
                Assert.True(Math.Abs(phiW - 1000.0 * phiKw) <= 1e-9 * phiW + 1e-6, "Satz " + i);
                Assert.Equal(Waermeuebergabe.RuecklaufC(w, thetaV, phiW), Waermeuebergabe.RuecklaufC(kw, thetaV, phiKw), 6);
            }
        }

        [Fact]
        public void Ein_groesserer_Exponent_liefert_unter_dem_Auslegungspunkt_weniger()
        {
            double vorher = double.PositiveInfinity;
            foreach (double n in new[] { 1.0, 1.1, 1.3, 1.6 })
            {
                var k = new Uebergabekennwerte(10000.0, n, 55.0, 45.0, 20.0);
                double phi = Waermeuebergabe.LeistungOffenW(k, 45.0, 20.0);
                Assert.True(phi < vorher, "n = " + n);
                vorher = phi;
            }
        }

        [Fact]
        public void Stellgrad_und_Kennlinie_des_P_Reglers()
        {
            Assert.Equal(1.0, Waermeuebergabe.Stellgrad(20.0, 1.0, 18.5));
            Assert.Equal(0.5, Waermeuebergabe.Stellgrad(20.0, 1.0, 19.5), 12);
            Assert.Equal(0.0, Waermeuebergabe.Stellgrad(20.0, 1.0, 20.5));
            Assert.Equal(1.0, Waermeuebergabe.Stellgrad(20.0, 0.0, 19.999));
            Assert.Equal(0.0, Waermeuebergabe.Stellgrad(20.0, 0.0, 20.0));

            Uebergabekennwerte k = BeispielA();
            Assert.Equal(0.0, Waermeuebergabe.LeistungOffenW(k, 20.0, 20.0));
            Assert.Equal(0.0, Waermeuebergabe.LeistungOffenW(k, double.NaN, 20.0));
            var unbegrenzt = new Uebergabekennwerte(double.PositiveInfinity, 1.3, 55.0, 45.0, 20.0);
            Assert.True(unbegrenzt.Unbegrenzt);
            Assert.True(double.IsPositiveInfinity(Waermeuebergabe.LeistungOffenW(unbegrenzt, 55.0, 20.0)));
            Assert.Equal(55.0, Waermeuebergabe.RuecklaufC(unbegrenzt, 55.0, 1234.0));
        }

        [Fact]
        public void Die_Kappungstemperatur_trifft_die_Leistungsgrenze()
        {
            Uebergabekennwerte k = BeispielA();
            const double V = 50.0, SOLL = 20.0;
            foreach (double xp in new[] { 0.0, 0.5, 1.0, 2.0, 5.0 })
            foreach (double hl in new[] { 500.0, 3000.0, 8000.0, 12000.0 })
            {
                double theta = Waermeuebergabe.Kappungstemperatur(k, V, SOLL, xp, hl);
                if (xp == 0.0 && theta == SOLL) continue;   // die Übergabe am Sollwert liegt noch über der Grenze
                double phi = Waermeuebergabe.LeistungKennlinieW(k, V, SOLL, xp, theta);
                Assert.True(Math.Abs(phi - hl) <= 1e-6 * hl, "Xp " + xp + ", HL " + hl + ": Φ(θ_cap) = " + phi);
            }

            // Unbegrenzte Übergabe mit Xp = 0: genau der Sollwert — die Grenze des Bestands.
            var unbegrenzt = new Uebergabekennwerte(double.PositiveInfinity, 1.3, 55.0, 45.0, 20.0);
            Assert.Equal(SOLL, Waermeuebergabe.Kappungstemperatur(unbegrenzt, V, SOLL, 0.0, 3000.0));
        }

        [Fact]
        public void Der_Arbeitspunkt_im_Regelbereich_ist_in_sich_stimmig()
        {
            Uebergabekennwerte k = BeispielA();
            const double V = 45.0, SOLL = 20.0, XP = 1.0, THETA0 = 18.0, S = 0.0004;
            Waermeuebergabe.ArbeitspunktRegelbereich(k, V, SOLL, XP, THETA0, S, out double thetaStern,
                                                     out double phiStern, out double g);
            Assert.True(thetaStern > SOLL - XP && thetaStern < SOLL, "im Band: " + thetaStern);
            Assert.True(Math.Abs(THETA0 + S * phiStern - thetaStern) <= 1e-9, "θ* = θ₀ + s·Φ*");
            Assert.Equal(Waermeuebergabe.LeistungKennlinieW(k, V, SOLL, XP, thetaStern), phiStern, 6);
            double phiMax = Waermeuebergabe.LeistungOffenW(k, V, thetaStern);
            double y = Waermeuebergabe.Stellgrad(SOLL, XP, thetaStern);
            Assert.Equal(phiMax / XP + y * Waermeuebergabe.SteigungOffenWK(k, phiMax, V, thetaStern), g, 6);
        }

        [Fact]
        public void Die_Kopplung_wirkt_nur_mit_Stufe_Schalter_und_Uebergabeart()
        {
            var g = new ProjektGebaeudeModel { Heizkreis_Aktiv = true, Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR };
            Assert.True(Waermeuebergabe.KopplungWirksamFuer(g, DbWerte.ANLAGENKOPPLUNG_AK1));
            Assert.True(Waermeuebergabe.KopplungWirksamFuer(g, DbWerte.ANLAGENKOPPLUNG_AK2));
            Assert.False(Waermeuebergabe.KopplungWirksamFuer(g, null));
            Assert.False(Waermeuebergabe.KopplungWirksamFuer(g, DbWerte.ANLAGENKOPPLUNG_AUS));
            g.Heizkreis_Aktiv = false;
            Assert.False(Waermeuebergabe.KopplungWirksamFuer(g, DbWerte.ANLAGENKOPPLUNG_AK1));
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_IDEAL;
            Assert.False(Waermeuebergabe.KopplungWirksamFuer(g, DbWerte.ANLAGENKOPPLUNG_AK1));
            g.Uebergabe_Art = null;
            Assert.False(Waermeuebergabe.KopplungWirksamFuer(g, DbWerte.ANLAGENKOPPLUNG_AK1));
        }
    }
}
