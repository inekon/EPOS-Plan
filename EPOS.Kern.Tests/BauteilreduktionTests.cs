using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G3 — reine Rechenproben des Bauteilwegs</b> (<see cref="Bauteilreduktion"/>;
    /// Mehrzonenkonzept 3.1–3.4, Rechenschritte Kapitel 3): homogene Schicht gegen den
    /// Grenzwert kleiner Frequenz, Symmetrie der Kettenmatrix, entartete Schichten, komplexe
    /// Parallelschaltung gegen die Zweierform Gl. (23)/(24), U-Wert aus Schichten, Bezugsperiode
    /// nach (10a)–(10d), Luftschichten nach DIN EN ISO 6946 Tabelle 8 und die benannten
    /// Fehler. Alle Aufbauten sind erfunden — keine Zahl einer Richtlinie außer den
    /// Bemessungswerten der DIN EN ISO 6946, die <see cref="GebaeudeFestwerte"/> selbst führt.
    /// </summary>
    public class BauteilreduktionTests
    {
        private static readonly Schicht Beton = new Schicht(0.20, 2.0, 2400.0, 1000.0);
        private static readonly Schicht Daemmung = new Schicht(0.10, 0.04, 30.0, 1400.0);
        private static readonly Schicht Putz = new Schicht(0.015, 0.7, 1400.0, 1000.0);

        private static GebaeudeModellFehler Grund(Action a) => Assert.Throws<GebaeudeModellException>(a).Grund;

        private static void Relativ(double erwartet, double ist, double toleranz)
        {
            double rel = Math.Abs(ist - erwartet) / Math.Abs(erwartet);
            Assert.True(rel <= toleranz, $"erwartet {erwartet:R}, ist {ist:R}, relativ {rel:E2} > {toleranz:E0}");
        }

        // =====================================================================
        //  Kettenmatrix und Ersatzgrößen
        // =====================================================================

        [Fact]
        public void Eine_homogene_Schicht_geht_bei_kleiner_Frequenz_gegen_R_durch_6_und_C_halbe()
        {
            // Sehr lange Periode ⇒ ωRC klein; die Reihenentwicklung gibt R₁ → R/6, C₁ → C/2.
            var s = new[] { Beton };
            double r = 0.20 / 2.0, c = 1000.0 * 2400.0 * 0.20;
            Bauteilkennwerte k = Bauteilreduktion.Reduzieren(s, 1.0, 1e5, Waermestromrichtung.Horizontal);
            Relativ(r / 6.0, k.R1_KW, 1e-6);
            Relativ(c / 2.0, k.C1_Jk, 1e-6);
            Relativ(r / 6.0, k.R2_KW, 1e-6);
            Relativ(2.0 * r / 3.0, k.R3_KW, 1e-6);
            Relativ(r, k.RW_KW, 1e-15);

            // Auf die Fläche bezogen: doppelte Fläche halbiert die Widerstände, verdoppelt die Kapazität.
            Bauteilkennwerte k2 = Bauteilreduktion.Reduzieren(s, 2.0, 1e5, Waermestromrichtung.Horizontal);
            Relativ(k.R1_KW / 2.0, k2.R1_KW, 1e-12);
            Relativ(2.0 * k.C1_Jk, k2.C1_Jk, 1e-12);
        }

        [Fact]
        public void Ein_symmetrischer_Aufbau_hat_gleiche_Seiten_ein_gedrehter_tauscht_sie()
        {
            var sym = new[] { Putz, Beton, Putz };
            Bauteilkennwerte k = Bauteilreduktion.Reduzieren(sym, 3.0, 7.0, Waermestromrichtung.Horizontal);
            Relativ(k.R1_KW, k.R2_KW, 1e-12);
            Relativ(k.C1_Jk, k.C2_Jk, 1e-12);

            // Nicht symmetrisch: die Reihenfolge zählt (Gl. (11)); gedreht tauschen Raum- und Außenseite.
            var innen = new[] { Beton, Daemmung, Putz };
            var gedreht = innen.Reverse().ToArray();
            Bauteilkennwerte a = Bauteilreduktion.Reduzieren(innen, 3.0, 7.0, Waermestromrichtung.Horizontal);
            Bauteilkennwerte b = Bauteilreduktion.Reduzieren(gedreht, 3.0, 7.0, Waermestromrichtung.Horizontal);
            Relativ(a.R1_KW, b.R2_KW, 1e-12);
            Relativ(a.R2_KW, b.R1_KW, 1e-12);
            Relativ(a.C1_Jk, b.C2_Jk, 1e-12);
            Assert.True(Math.Abs(a.R1_KW / b.R1_KW - 1.0) > 0.1, "Die Kettenmatrix ist nicht richtungssymmetrisch.");
            // Masse innen: kleiner innerer Widerstand, große wirksame Kapazität.
            Assert.True(a.R1_KW < b.R1_KW && a.C1_Jk > b.C1_Jk);
            Relativ(a.RW_KW, a.R1_KW + a.R2_KW + a.R3_KW, 1e-15);
        }

        [Fact]
        public void Die_Kettenmatrix_teilt_nie_durch_R_oder_k_und_entartete_Schichten_bleiben_endlich()
        {
            double omega = Bauteilreduktion.Kreisfrequenz(7.0);

            // R = 0 (Metall ohne Widerstand): a₁₂ = 0, a₂₁ = jωC, Diagonale 1.
            Kettenmatrix blech = Bauteilreduktion.Schichtmatrix(0.0, 3600.0, omega);
            Assert.Equal(Complex.Zero, blech.D11);
            Assert.Equal(Complex.Zero, blech.A12);
            Assert.Equal(new Complex(0.0, omega * 3600.0), blech.A21);

            // C = 0 (Luft): a₁₂ = R, a₂₁ = 0, Diagonale 1.
            Kettenmatrix luft = Bauteilreduktion.Schichtmatrix(0.17, 0.0, omega);
            Assert.Equal(Complex.Zero, luft.D11);
            Assert.Equal(new Complex(0.17, 0.0), luft.A12);
            Assert.Equal(Complex.Zero, luft.A21);

            // Stetig: winzige Kapazität ≈ keine Kapazität, ohne Sprung an der Reihengrenze.
            Kettenmatrix fast = Bauteilreduktion.Schichtmatrix(0.17, 1e-9, omega);
            Assert.True(Complex.Abs(fast.A12 - luft.A12) < 1e-15);
            foreach (double x in new[] { 0.009999, 0.01, 0.010001 })
            {
                Complex k = new Complex(x, x);
                Complex reihe = Complex.One + k * k / 6.0 + k * k * k * k / 120.0;
                Assert.True(Complex.Abs(Bauteilreduktion.SinhDurchArgument(k) - reihe) < 1e-14);
            }

            // Metallblech, Dämmung, ruhende Luft und Beton in einem Aufbau: alles endlich und positiv.
            var aufbau = new[] { new Schicht(0.001, 50.0, 7850.0, 460.0), new Schicht(0.02, 0.04, 60.0, 840.0),
                                 Schicht.RuhendeLuft(0.05), Beton };
            foreach (double t in new[] { 2.0, 5.0, 7.0 })
            {
                Bauteilkennwerte k = Bauteilreduktion.Reduzieren(aufbau, 10.0, t, Waermestromrichtung.Aufwaerts);
                foreach (double w in new[] { k.R1_KW, k.R2_KW, k.C1_Jk, k.C2_Jk, k.C1korr_Jk })
                    Assert.True(double.IsFinite(w) && w > 0.0, "t = " + t + ": " + k);
            }

            // Ein Blech allein: winziges ωRC, trotzdem endlich und nahe R/6, C/2.
            var nurBlech = new[] { new Schicht(0.001, 50.0, 7850.0, 460.0) };
            Bauteilkennwerte b = Bauteilreduktion.Reduzieren(nurBlech, 1.0, 7.0, Waermestromrichtung.Horizontal);
            Relativ(0.001 / 50.0 / 6.0, b.R1_KW, 1e-6);
            Relativ(460.0 * 7850.0 * 0.001 / 2.0, b.C1_Jk, 1e-6);
        }

        [Fact]
        public void Ein_Aufbau_ohne_Speichermasse_ist_benannt_nicht_reduzierbar()
        {
            var nurLuft = new[] { Schicht.RuhendeLuft(0.05) };
            Assert.Equal(GebaeudeModellFehler.BauteilreduktionUngueltig,
                Grund(() => Bauteilreduktion.Reduzieren(nurLuft, 1.0, 7.0, Waermestromrichtung.Horizontal)));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig,
                Grund(() => Bauteilreduktion.Reduzieren(new[] { Beton }, 0.0, 7.0, Waermestromrichtung.Horizontal)));
        }

        // =====================================================================
        //  Parallelschaltung (Gl. (19)–(24))
        // =====================================================================

        [Fact]
        public void Die_komplexe_Parallelschaltung_gleicht_der_Zweierform_der_Gleichungen_23_und_24()
        {
            var zweige = new List<(double, double)>
            {
                (0.0021, 2.1e6), (0.00085, 7.4e6), (0.012, 3.0e5), (0.0004, 1.9e7),
            };
            double omega = Bauteilreduktion.Kreisfrequenz(GebaeudeFestwerte.BEZUGSPERIODE_RAUM_D);
            (double r, double c) = Bauteilreduktion.Parallel(zweige);

            // Gl. (23)/(24), nacheinander angewandt.
            (double rb, double cb) = zweige[0];
            for (int i = 1; i < zweige.Count; i++)
            {
                (double ra, double ca) = zweige[i];
                double w2 = omega * omega;
                double nenner = (cb + ca) * (cb + ca) + w2 * (rb + ra) * (rb + ra) * cb * cb * ca * ca;
                double rNeu = (rb * cb * cb + ra * ca * ca + w2 * rb * ra * (rb + ra) * cb * cb * ca * ca) / nenner;
                double cNeu = nenner / (cb + ca + w2 * (rb * rb * cb + ra * ra * ca) * cb * ca);
                rb = rNeu;
                cb = cNeu;
            }
            Relativ(rb, r, 1e-12);
            Relativ(cb, c, 1e-12);

            // Ein Bauteil bleibt, wie es ist; zwei gleiche halbieren R und verdoppeln C.
            Assert.Equal(zweige[0], Bauteilreduktion.Parallel(zweige.Take(1).ToList()));
            (double r2, double c2) = Bauteilreduktion.Parallel(new[] { (0.002, 1e6), (0.002, 1e6) });
            Relativ(0.001, r2, 1e-12);
            Relativ(2e6, c2, 1e-12);
            Assert.Throws<ArgumentException>(() => Bauteilreduktion.Parallel(new List<(double, double)>()));
        }

        // =====================================================================
        //  U-Wert aus Schichten, Übergänge, Luftschichten (DIN EN ISO 6946)
        // =====================================================================

        [Fact]
        public void Der_U_Wert_aus_Schichten_nimmt_die_Uebergaenge_nach_Neigung_und_Randbedingung()
        {
            var wand = new[] { Putz, Beton, Daemmung };
            double r = 0.015 / 0.7 + 0.20 / 2.0 + 0.10 / 0.04;
            double c = 1000.0 * 1400.0 * 0.015 + 1000.0 * 2400.0 * 0.20 + 1400.0 * 30.0 * 0.10;

            Schichtkennwerte w = Bauteilreduktion.UWertAusSchichten(wand, 90.0, Bauteilrand.Aussenluft, "Wand");
            Relativ(r, w.R_M2KW, 1e-15);
            Relativ(c, w.Kapazitaet_JM2K, 1e-15);
            Assert.Equal(1.0 / (0.13 + w.R_M2KW + 0.04), w.U_WM2K);

            // Dach (aufwärts), Boden an Erdreich (abwärts, kein R_se), Wand zum unbeheizten Raum (R_se = R_si).
            Assert.Equal(1.0 / (0.10 + w.R_M2KW + 0.04), Bauteilreduktion.UWertAusSchichten(wand, 0.0, Bauteilrand.Aussenluft).U_WM2K);
            Assert.Equal(1.0 / (0.17 + w.R_M2KW + 0.0), Bauteilreduktion.UWertAusSchichten(wand, 180.0, Bauteilrand.Erdreich).U_WM2K);
            Assert.Equal(1.0 / (0.13 + w.R_M2KW + 0.13), Bauteilreduktion.UWertAusSchichten(wand, 90.0, Bauteilrand.Unbeheizt).U_WM2K);

            // Grenzen der Richtung: ±30° um die Waagerechte.
            Assert.Equal(Waermestromrichtung.Aufwaerts, Bauteilreduktion.RichtungAusNeigung(59.9));
            Assert.Equal(Waermestromrichtung.Horizontal, Bauteilreduktion.RichtungAusNeigung(60.0));
            Assert.Equal(Waermestromrichtung.Horizontal, Bauteilreduktion.RichtungAusNeigung(120.0));
            Assert.Equal(Waermestromrichtung.Abwaerts, Bauteilreduktion.RichtungAusNeigung(120.1));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig, Grund(() => Bauteilreduktion.RichtungAusNeigung(-1.0)));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig, Grund(() => Bauteilreduktion.RichtungAusNeigung(double.NaN)));
        }

        [Fact]
        public void Eine_ruhende_Luftschicht_folgt_Tabelle_8_und_traegt_keine_Kapazitaet()
        {
            foreach (Waermestromrichtung r in Enum.GetValues(typeof(Waermestromrichtung)))
            {
                Assert.Equal(0.15, Bauteilreduktion.Luftschichtwiderstand(0.010, r), 12);
                Assert.Equal(0.0, Bauteilreduktion.Luftschichtwiderstand(0.0, r));
            }
            Assert.Equal(0.175, Bauteilreduktion.Luftschichtwiderstand(0.020, Waermestromrichtung.Horizontal), 12);
            Assert.Equal(0.16, Bauteilreduktion.Luftschichtwiderstand(0.200, Waermestromrichtung.Aufwaerts), 12);
            Assert.Equal(0.225, Bauteilreduktion.Luftschichtwiderstand(0.200, Waermestromrichtung.Abwaerts), 12);
            Assert.Equal(0.23, Bauteilreduktion.Luftschichtwiderstand(0.300, Waermestromrichtung.Abwaerts), 12);

            Schicht luft = Schicht.RuhendeLuft(0.04);
            Assert.True(luft.IstRuhendeLuftschicht);
            Assert.Equal(0.0, Bauteilreduktion.FlaechenbezogeneKapazitaet(luft));
            Schichtkennwerte k = Bauteilreduktion.UWertAusSchichten(new[] { Beton, luft, Putz }, 90.0, Bauteilrand.Aussenluft);
            Relativ(0.20 / 2.0 + 0.18 + 0.015 / 0.7, k.R_M2KW, 1e-15);   // 40 mm waagerecht: zwischen 25 und 50 mm je 0,18

            // Über 0,3 m benannt abgelehnt.
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig,
                Grund(() => Bauteilreduktion.UWertAusSchichten(new[] { Schicht.RuhendeLuft(0.31) }, 90.0, Bauteilrand.Aussenluft)));

            // Eine Luftschicht MIT λ ist eine normale Schicht; ihre Rohdichte darf unter dem Band liegen.
            var aequivalent = new Schicht(0.2, 1.1, 1.2, 1000.0, IstLuftschicht: true);
            Assert.False(aequivalent.IstRuhendeLuftschicht);
            Relativ(0.2 / 1.1, Bauteilreduktion.UWertAusSchichten(new[] { aequivalent }, 0.0, Bauteilrand.Innen).R_M2KW, 1e-15);
            Relativ(1000.0 * 1.2 * 0.2, Bauteilreduktion.FlaechenbezogeneKapazitaet(aequivalent), 1e-15);
        }

        [Fact]
        public void Stoffwerte_ausserhalb_des_Bands_sind_benannte_Fehler()
        {
            void Rechne(Schicht s) => Bauteilreduktion.UWertAusSchichten(new[] { s }, 90.0, Bauteilrand.Aussenluft, "Probe");
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig, Grund(() => Rechne(new Schicht(0.2, 0.0, 2000.0, 1000.0))));    // λ ≤ 0
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig, Grund(() => Rechne(new Schicht(0.2, -1.0, 2000.0, 1000.0))));
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig, Grund(() => Rechne(new Schicht(0.2, 600.0, 2000.0, 1000.0))));
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig, Grund(() => Rechne(new Schicht(0.2, 1.0, 3.0, 1000.0))));       // ρ unter dem Band
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig, Grund(() => Rechne(new Schicht(0.2, 1.0, 2000.0, 1.0))));      // c in kJ statt J
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig, Grund(() => Rechne(new Schicht(1.5, 1.0, 2000.0, 1000.0))));   // Dicke
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig, Grund(() => Rechne(new Schicht(0.0005, 1.0, 2000.0, 1000.0))));
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig, Grund(() => Rechne(new Schicht(double.NaN, 1.0, 2000.0, 1000.0))));
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig,
                Grund(() => Bauteilreduktion.UWertAusSchichten(Array.Empty<Schicht>(), 90.0, Bauteilrand.Aussenluft)));

            // Die Grenzen selbst gehören zum Band.
            Rechne(new Schicht(GebaeudeFestwerte.SCHICHT_DICKE_MIN_M, GebaeudeFestwerte.LAMBDA_MAX_WMK,
                               GebaeudeFestwerte.ROHDICHTE_MAX_KGM3, GebaeudeFestwerte.CP_MIN_JKGK));
            Rechne(new Schicht(GebaeudeFestwerte.SCHICHT_DICKE_MAX_M, GebaeudeFestwerte.LAMBDA_MIN_WMK,
                               GebaeudeFestwerte.ROHDICHTE_MIN_KGM3, GebaeudeFestwerte.CP_MAX_JKGK));

            // Die Meldung nennt Bauteil und Schicht.
            var ex = Assert.Throws<GebaeudeModellException>(() => Rechne(new Schicht(0.2, 0.0, 2000.0, 1000.0)));
            Assert.Contains("Probe", ex.Message);
        }

        // =====================================================================
        //  Bezugsperiode (10a)–(10d)
        // =====================================================================

        /// <summary>
        /// Eine Decke mit raumseitig abgehängter Metallkassette, dünner Auflage und ruhender
        /// Luftschicht deckt die Speichermasse ab: (10a) greift, das Bauteil rechnet mit 2 Tagen.
        /// Dieselbe Decke ohne den Vorsatz bleibt bei 7 Tagen.
        /// </summary>
        [Fact]
        public void Eine_abgehaengte_Metalldecke_mit_Luftschicht_schaltet_die_Bezugsperiode_auf_2_Tage()
        {
            var decke = new[] { new Schicht(0.18, 2.3, 2400.0, 1000.0), new Schicht(0.03, 0.04, 100.0, 1000.0), new Schicht(0.05, 1.4, 2000.0, 1000.0) };
            var vorsatz = new[] { new Schicht(0.001, 50.0, 7850.0, 460.0), new Schicht(0.01, 0.04, 60.0, 840.0), Schicht.RuhendeLuft(0.20) };

            Bezugsperiodenwahl mit = Bauteilreduktion.BezugsperiodeWaehlen(vorsatz.Concat(decke).ToArray(), 20.0, Waermestromrichtung.Aufwaerts);
            Bezugsperiodenwahl ohne = Bauteilreduktion.BezugsperiodeWaehlen(decke, 20.0, Waermestromrichtung.Aufwaerts);

            Assert.Equal(GebaeudeFestwerte.BEZUGSPERIODE_ABGEDECKT_D, mit.Periode_d);
            Assert.True(mit.Abgedeckt);
            Assert.True(mit.R1rel > Bauteilreduktion.KRITERIUM_R1REL_OBEN && mit.C1rel < Bauteilreduktion.KRITERIUM_REL_UNTEN);
            Assert.Equal(GebaeudeFestwerte.BEZUGSPERIODE_ABGEDECKT_D, mit.Kennwerte.Periode_d);

            Assert.Equal(GebaeudeFestwerte.BEZUGSPERIODE_BAUTEIL_D, ohne.Periode_d);
            Assert.False(ohne.Abgedeckt);
            Assert.True(ohne.C1rel >= Bauteilreduktion.KRITERIUM_REL_UNTEN);

            // Die gewählte Periode ist die der ausgewiesenen Kennwerte.
            Bauteilkennwerte k7 = Bauteilreduktion.Reduzieren(decke, 20.0, 7.0, Waermestromrichtung.Aufwaerts);
            Assert.Equal(k7, ohne.Kennwerte);
        }
    }
}
