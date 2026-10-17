using System;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>N-AH0 Druckstellen der Aufheizantwort</b> (Entwurf KP3 Abschnitt 7, Welle R1; Teilkonzept
    /// Konditionierungsprofile 4.2, 4.3) am Prüfsatz des Hauses aus Projekt 1045
    /// (<c>GebaeudePruefmodusTests</c>, Rechenschritte 9.1, Prüfmodus-Konvention): H_s, G_0, τ, C_w, die
    /// Spitze ohne Rampe und die Wahl von n bei 35,3 kW, mit Strahlungsanteil 0 und 0,3. Die Toleranz ist
    /// die der Druckstelle (relativ 1e-3). Dazu die inneren Identitäten der Antwort und ihr Speicher.
    /// </summary>
    public class AufheizantwortTests
    {
        private readonly ITestOutputHelper _aus;

        public AufheizantwortTests(ITestOutputHelper aus) { _aus = aus; }

        private const double KWH_JE_K = 3.6e6;          // J/K je kWh/K
        private const double H = Zonenmodell2K.STUNDE_S;

        // Rechenschritte 9.1 — die Eingänge des Gebäudes 10645 (wie GebaeudePruefmodusTests).
        private const double NUTZFLAECHE = 201.0;
        private const double BAUWEISE_WH_K = 10_050.0;
        private const double A_AW_OPAK = 280.28 + 116.86 + 88.00 + 2.10;
        private const double UA_OPAK = 1.84 * 280.28 + 0.80 * 116.86 + 0.80 * 88.00 + 3.50 * 2.10;
        private const double H_VE = 0.7 * 201.0 * 2.75 * 0.34;
        private const double UA_FENSTER = 2.8 * 45.06;
        private const double PSI_L = 0.15 * 160 + 0.40 * 202.02 + 0.70 * 2.50;

        /// <summary>
        /// Der Parametersatz des Prüfmodus (<c>GebaeudePruefmodusTests.cs:38-68</c>), mit Faktoren für
        /// die synthetischen Varianten der Stufenformel-Nachweise.
        /// </summary>
        internal static ErsatzparameterRC Pruefsatz(double faktorRRest = 1.0, double faktorRExt = 1.0,
                                                    double faktorCAw = 1.0, double faktorCIw = 1.0,
                                                    double faktorRRad = 1.0)
        {
            double cGes = BAUWEISE_WH_K * 3600.0;
            double aIw = 2.5 * NUTZFLAECHE;
            double r1Aw = 1.0 / (9.1 * A_AW_OPAK);
            double rRest = 1.0 / UA_OPAK - r1Aw - 0.13 / A_AW_OPAK;
            return new ErsatzparameterRC(
                c_AW_Jk: 0.3 * cGes * faktorCAw,
                c_IW_Jk: 0.7 * cGes * faktorCIw,
                r_1_AW_KW: r1Aw,
                r_Rest_AW_KW: rRest * faktorRRest,
                r_1_IW_KW: 1.0 / (9.1 * aIw),
                r_conv_AW_KW: 1.0 / (2.7 * A_AW_OPAK),
                r_conv_IW_KW: 1.0 / (2.7 * aIw),
                r_rad_KW: faktorRRad / (5.0 * Math.Min(A_AW_OPAK, aIw)),
                r_ext_KW: faktorRExt / (H_VE + UA_FENSTER + PSI_L),
                a_AW_opak_M2: A_AW_OPAK,
                a_IW_M2: aIw,
                summeUA_opak_WK: UA_OPAK);
        }

        private static void Druckstelle(double erwartet, double ist, string was)
        {
            double rel = Math.Abs(ist - erwartet) / Math.Abs(erwartet);
            Assert.True(rel <= 1e-3, was + ": erwartet " + erwartet.ToString("G6", CultureInfo.InvariantCulture) +
                                     ", ist " + ist.ToString("G9", CultureInfo.InvariantCulture));
        }

        private static void Relativ(double erwartet, double ist, double tol, string was)
        {
            double rel = Math.Abs(ist - erwartet) / Math.Max(Math.Abs(erwartet), 1e-300);
            Assert.True(rel <= tol, was + ": erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) +
                                    ", ist " + ist.ToString("R", CultureInfo.InvariantCulture) +
                                    " (relativ " + rel.ToString("G3", CultureInfo.InvariantCulture) + ")");
        }

        private string Zeile(string format, params object[] werte)
        {
            string text = string.Format(CultureInfo.InvariantCulture, format, werte);
            _aus.WriteLine(text);
            return text;
        }

        /// <summary>
        /// Teilkonzept 4.2 (a = 0): H_s 971,8 W/K, G_0 2 425,1 W/K, τ 1,017/4,540 h, C_w 5,781 kWh/K;
        /// mit a = 0,3 C_w 6,931 kWh/K.
        /// </summary>
        [Fact]
        public void N_AH0_Die_Modalgroessen_treffen_Teilkonzept_4_2()
        {
            var m = new Zonenmodell2K(Pruefsatz());
            Aufheizantwort a0 = m.Aufheizantwort(0.0, 0.0);
            Aufheizantwort a3 = m.Aufheizantwort(0.3, 0.0);
            foreach (Aufheizantwort a in new[] { a0, a3 })
                Zeile("a = {0}: H_s {1:F2} W/K, G_0 {2:F2} W/K, τ {3:F4}/{4:F4} h, C {5:F4}/{6:F4} kWh/K, C_w {7:F4} kWh/K",
                      a.Strahlungsanteil, a.HsWK, a.G0WK, a.Tau1S / H, a.Tau2S / H,
                      a.C1Jk / KWH_JE_K, a.C2Jk / KWH_JE_K, a.CwJk / KWH_JE_K);

            Druckstelle(971.8, a0.HsWK, "H_s");
            Druckstelle(2425.1, a0.G0WK, "G_0");
            Druckstelle(1.017, a0.Tau1S / H, "τ_1");
            Druckstelle(4.540, a0.Tau2S / H, "τ_2");
            Druckstelle(5.781, a0.CwJk / KWH_JE_K, "C_w (a = 0)");
            // C_1 und C_2 stehen in 4.2 auf zwei Nachkommastellen: halbe letzte Stelle.
            Assert.InRange(a0.C1Jk / KWH_JE_K, 0.235, 0.245);
            Assert.InRange(a0.C2Jk / KWH_JE_K, 5.545, 5.555);
            Druckstelle(6.931, a3.CwJk / KWH_JE_K, "C_w (a = 0,3)");
            Assert.False(a0.Zusammenfallend);
            Assert.False(a3.Zusammenfallend);
        }

        /// <summary>
        /// Teilkonzept 4.2/4.3 und Entwurf KP3 B30, G17, G18: bei −12 °C, Sprung 17 → 21 °C, die Spitze
        /// ohne Rampe 37,04 kW gegen 32,07 kW stationär (a = 0); bei der Grenze 35,3 kW n = 5 im Mittel
        /// (35,22 kW; am Stundenbeginn 35,66 kW), n = 7 im Augenblick (n = 6: 35,335 kW); mit a = 0,3
        /// Φ_stat 34,42 kW und n = 32 im Mittel.
        /// </summary>
        [Fact]
        public void N_AH0_Spitze_und_Wahl_von_n_treffen_Teilkonzept_4_3()
        {
            const double ta = -12.0, thetaN = 17.0, thetaT = 21.0, grenze = 35_300.0;
            const double dT = thetaT - thetaN;
            var m = new Zonenmodell2K(Pruefsatz());

            Aufheizantwort a0 = m.Aufheizantwort(0.0, 0.0);
            double stat0 = m.StationaereHeizlastW(thetaT, ta, ta, 0.0);
            double spitze0 = Aufheizstufen.StundenmittelW(a0, stat0, dT, 1);
            Aufheizwahl mittel0 = Aufheizstufen.Waehlen(a0, stat0, dT, grenze, Aufheizform.Stundenmittel, 48);
            Aufheizwahl augenblick0 = Aufheizstufen.Waehlen(a0, stat0, dT, grenze, Aufheizform.Augenblick, 48);
            Zeile("a = 0: Φ_stat {0:F1} W, Spitze {1:F1} W, Mittel n = {2} ({3:F1} W, Beginn {4:F1} W), Augenblick n = {5} ({6:F1} W; n = 6: {7:F1} W)",
                  stat0, spitze0, mittel0.N, mittel0.LeistungW, Aufheizstufen.AugenblickW(a0, stat0, dT, mittel0.N),
                  augenblick0.N, augenblick0.LeistungW, Aufheizstufen.AugenblickW(a0, stat0, dT, 6));

            Druckstelle(32_070.0, stat0, "Φ_stat (a = 0)");
            Druckstelle(37_040.0, spitze0, "Spitze ohne Rampe (a = 0)");
            Assert.True(mittel0.Erreichbar);
            Assert.Equal(5, mittel0.N);
            Druckstelle(35_220.0, mittel0.LeistungW, "Φ̄_5");
            Druckstelle(35_660.0, Aufheizstufen.AugenblickW(a0, stat0, dT, 5), "Φ̂_5");
            Assert.True(augenblick0.Erreichbar);
            Assert.Equal(7, augenblick0.N);
            Druckstelle(35_335.0, Aufheizstufen.AugenblickW(a0, stat0, dT, 6), "Φ̂_6");

            Aufheizantwort a3 = m.Aufheizantwort(0.3, 0.0);
            double stat3 = m.StationaereHeizlastW(thetaT, ta, ta, 0.3);
            Aufheizwahl mittel3 = Aufheizstufen.Waehlen(a3, stat3, dT, grenze, Aufheizform.Stundenmittel, 48);
            Aufheizwahl augenblick3 = Aufheizstufen.Waehlen(a3, stat3, dT, grenze, Aufheizform.Augenblick, 48);
            Zeile("a = 0,3: Φ_stat {0:F1} W, Spitze {1:F1} W, Mittel n = {2} ({3:F1} W), Augenblick n = {4} ({5:F1} W)",
                  stat3, Aufheizstufen.StundenmittelW(a3, stat3, dT, 1), mittel3.N, mittel3.LeistungW,
                  augenblick3.N, augenblick3.LeistungW);
            Druckstelle(34_420.0, stat3, "Φ_stat (a = 0,3)");
            Assert.True(mittel3.Erreichbar);
            Assert.Equal(32, mittel3.N);
        }

        /// <summary>
        /// Die inneren Identitäten der Antwort: H_s gleich der stationären Heizlast je Kelvin des Lösers,
        /// G_0 = H_s + r_1 + r_2, C_w = C_1 + C_2, Φ̂_1 = Φ_stat + G_0·ΔT − H_s·ΔT, Φ̄_n und Φ̂_n fallen mit n.
        /// </summary>
        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(0.3, 0.0)]
        [InlineData(0.3, 150.0)]
        [InlineData(1.0, 40.0)]
        public void Die_Antwort_ist_in_sich_stimmig(double anteil, double zusatz)
        {
            var m = new Zonenmodell2K(Pruefsatz());
            Aufheizantwort a = m.Aufheizantwort(anteil, zusatz);
            const double ta = -7.5, thetaT = 20.5, dT = 3.0;
            double stat = m.StationaereHeizlastW(thetaT, ta, ta, anteil, zusatz);

            Relativ(stat / (thetaT - ta), a.HsWK, 1e-12, "H_s gegen StationaereHeizlastW");
            Relativ(a.G0WK, a.HsWK + a.R1WK + a.R2WK, 1e-12, "G_0 = H_s + r_1 + r_2");
            Relativ(a.CwJk, a.C1Jk + a.C2Jk, 1e-12, "C_w = C_1 + C_2");
            Assert.True(a.Tau1S < a.Tau2S, "τ_1 ist der schnelle Modus");
            Assert.True(a.R1WK > 0.0 && a.R2WK > 0.0 && a.CwJk > 0.0);
            Relativ(stat + (a.G0WK - a.HsWK) * dT, Aufheizstufen.AugenblickW(a, stat, dT, 1), 1e-12, "Φ̂_1");
            // C_w ist der Grenzwert von z·Γ(t)·v für t → ∞ (die Überschusswärme je Kelvin).
            Relativ(a.CwJk, a.Ausgang(a.Bei(1000.0 * H).Gamma), 1e-9, "C_w = z·Γ(∞)·v");

            double vorMittel = double.PositiveInfinity, vorAugenblick = double.PositiveInfinity;
            for (int n = 1; n <= 48; n++)
            {
                double mittel = Aufheizstufen.StundenmittelW(a, stat, dT, n);
                double augenblick = Aufheizstufen.AugenblickW(a, stat, dT, n);
                Assert.True(mittel < vorMittel && augenblick < vorAugenblick, "fallend in n, n = " + n.ToString(CultureInfo.InvariantCulture));
                Assert.True(augenblick > mittel, "Augenblick über Mittel, n = " + n.ToString(CultureInfo.InvariantCulture));
                Assert.True(mittel > stat, "über der stationären Last, n = " + n.ToString(CultureInfo.InvariantCulture));
                // Erste Ordnung: Das Mittel liegt nie über Φ_stat + C_w·ΔT/(n·h).
                Assert.True(mittel - stat <= dT * a.CwJk / (n * H) * (1.0 + 1e-12), "Schranke, n = " + n.ToString(CultureInfo.InvariantCulture));
                vorMittel = mittel;
                vorAugenblick = augenblick;
            }

            // P_auf = +∞ hält mit n = 1 (Grenzfall N-AH8); P_auf ≤ Φ_stat ist unerreichbar.
            Assert.Equal(1, Aufheizstufen.Waehlen(a, stat, dT, double.PositiveInfinity, Aufheizform.Augenblick, 48).N);
            Aufheizwahl nie = Aufheizstufen.Waehlen(a, stat, dT, stat, Aufheizform.Stundenmittel, 48);
            Assert.False(nie.Erreichbar);
            Assert.Equal(0, nie.N);
            Assert.Equal(double.PositiveInfinity, Aufheizstufen.ErsteOrdnungMittel(a, stat, dT, stat));
            Assert.Equal(double.PositiveInfinity, Aufheizstufen.ErsteOrdnungAugenblick(a, stat, dT, 0.5 * stat));
            Assert.Equal(1.0, Aufheizstufen.ErsteOrdnungMittel(a, stat, dT, double.PositiveInfinity));
        }

        /// <summary>
        /// Zustandsfrei und gespeichert: Die Antwort ändert den Zustand nicht; gleicher Schlüssel, dieselbe
        /// Antwort; ein verdrängter Schlüssel entsteht bitgleich neu; ein Schlüssel ist bitgenau.
        /// </summary>
        [Fact]
        public void Die_Antwort_ist_zustandsfrei_und_bitgenau_gespeichert()
        {
            var m = new Zonenmodell2K(Pruefsatz());
            m.Zuruecksetzen(12.25, 17.75);
            Aufheizantwort erste = m.Aufheizantwort(0.3, 100.0);
            Assert.Equal(12.25, m.ThetaMAw);
            Assert.Equal(17.75, m.ThetaMIw);
            Assert.Same(erste, m.Aufheizantwort(0.3, 100.0));
            Assert.Equal(1, m.AufheizantwortNeubauten);

            // Ein Bit daneben ist ein anderer Schlüssel.
            Aufheizantwort daneben = m.Aufheizantwort(0.3, Math.BitIncrement(100.0));
            Assert.NotSame(erste, daneben);
            Assert.Equal(2, m.AufheizantwortNeubauten);

            // Über die Plätze hinaus: Der älteste Platz wird ersetzt, die neue Antwort ist bitgleich.
            for (int i = 0; i < Zonenmodell2K.AUFHEIZANTWORT_PLAETZE; i++) m.Aufheizantwort(0.3, 200.0 + i);
            Aufheizantwort neu = m.Aufheizantwort(0.3, 100.0);
            Assert.NotSame(erste, neu);
            Assert.True(erste.HsWK.Equals(neu.HsWK) && erste.CwJk.Equals(neu.CwJk) && erste.G0WK.Equals(neu.G0WK)
                        && erste.V.A.Equals(neu.V.A) && erste.V.B.Equals(neu.V.B));

            // Ein frisches Modell liefert dieselbe Antwort, gleich in welcher Reihenfolge gefragt wird.
            var frisch = new Zonenmodell2K(Pruefsatz());
            Aufheizantwort f = frisch.Aufheizantwort(0.3, 100.0);
            Assert.True(f.HsWK.Equals(erste.HsWK) && f.CwJk.Equals(erste.CwJk) && f.Tau2S.Equals(erste.Tau2S));

            // Mit Strahlungsanteil hängt die Antwort vom Luftwechsel ab, die Zeitkonstanten nicht (B3).
            Aufheizantwort ohne = m.Aufheizantwort(0.3, 0.0);
            Assert.True(neu.CwJk != ohne.CwJk);
            Assert.Equal(ohne.Tau1S, neu.Tau1S);
            Assert.Equal(ohne.Tau2S, neu.Tau2S);
            Aufheizantwort konvektiv = m.Aufheizantwort(0.0, 0.0), konvektivLuft = m.Aufheizantwort(0.0, 300.0);
            Relativ(konvektiv.CwJk, konvektivLuft.CwJk, 1e-12, "C_w bei a = 0 unabhängig vom Luftwechsel");

            Assert.Throws<ArgumentOutOfRangeException>(() => m.Aufheizantwort(-0.1, 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => m.Aufheizantwort(0.3, -1.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => m.Aufheizantwort(0.3, double.PositiveInfinity));
        }
    }
}
