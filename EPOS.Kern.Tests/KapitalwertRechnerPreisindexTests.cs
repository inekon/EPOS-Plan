using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID W5‑B‑12 (09.09.2026): „Preisindizierung der
    /// Ersatzbeschaffung (p_I) und nicht monetäre Wirkungen."
    ///
    /// <para><b>Der Befund:</b> <see cref="KapitalwertRechner"/> trug den heutigen Betrag
    /// einer Investitionsposition unverändert in jedes Ersatzjahr — die Wärmepumpe, die
    /// in 16 Jahren ersetzt wird, kostete so viel wie die von heute („Vereinfachung W1",
    /// Lücke <b>G4</b> des VALERI-Abgleichs W5‑B‑10). VDI 2067 Blatt 1 schreibt die
    /// kapitalgebundenen Kosten dagegen mit einem eigenen Preisänderungsfaktor fort:
    /// A_n = A₀ · (1 + p_I)^n, und der Restwert steht auf derselben Preisbasis.</para>
    ///
    /// <para><b>Was diese Fälle festhalten:</b> die Zusage <b>p_I = 0 rechnet bitgleich</b>
    /// (kein Toleranzfenster, exakter Vergleich), die Indizierung auf das ABSOLUTE
    /// Zahlungsjahr, die nominal bleibende Erstbeschaffung (auch die nach KD6
    /// verschobene), den Restwert auf der Preisbasis der letzten Beschaffung und die
    /// Richtung der Wirkung (KW sinkt mit Ersatz, bleibt gleich ohne).</para>
    ///
    /// <para><b>Ohne Datenbank</b> — der Rechenkern ist ein reines Verfahren; die Ablage
    /// prüft <see cref="Migration72Tests"/>.</para>
    /// </summary>
    public class KapitalwertRechnerPreisindexTests
    {
        // Ein Fall, der Ersatz UND Restwert erzeugt: 10.000 EUR, 8 a Nutzungsdauer,
        // 20 a Betrachtungszeitraum -> Ersatz in t = 8 und t = 16, Restrestwert 4/8.
        private const double BETRAG = 10000.0;
        private const double N_JAHRE = 8.0;
        private const int T = 20;
        private const double ZINS = 3.0;
        private const double P_B = 1.0;
        private const double P_E = 2.0;
        private const double P_I = 2.0;

        private static List<KapitalwertRechner.InvestPosition> Positionen(
            double betrag, double nutzungsdauer, int startJahr)
        {
            return new List<KapitalwertRechner.InvestPosition>
            {
                new KapitalwertRechner.InvestPosition
                { Betrag = betrag, Nutzungsdauer = nutzungsdauer, StartJahr = startJahr }
            };
        }

        /// <summary>Der Lauf OHNE den neuen Parameter — die Signatur von vor W5‑B‑12.</summary>
        private static KapitalwertRechner.Zahlungsbild Alt(
            List<KapitalwertRechner.InvestPosition> invest)
        {
            return KapitalwertRechner.Rechne(invest, 500.0, 2000.0, 300.0, ZINS, T, P_B, P_E);
        }

        /// <summary>Derselbe Lauf MIT p_I; alle Zwischenparameter auf ihren Vorgaben.</summary>
        private static KapitalwertRechner.Zahlungsbild Neu(
            List<KapitalwertRechner.InvestPosition> invest, double pI)
        {
            return KapitalwertRechner.Rechne(invest, 500.0, 2000.0, 300.0, ZINS, T, P_B, P_E,
                                             0, null, 0, null, null, 0, null, pI);
        }

        // =====================================================================
        // 1 - p_I = 0 rechnet bitgleich
        // =====================================================================

        /// <summary>
        /// Ohne Satz ist das Zahlungsbild EXAKT das von vor dieser Etappe: dieselbe
        /// Ersatzreihe, derselbe Restwert, derselbe Kapitalwert — ohne Toleranzfenster.
        /// Das ist die Zusage, an der der optionale Parameter hängt: Jeder bestehende
        /// Aufrufer bleibt gültig UND rechnet unverändert.
        /// </summary>
        [Fact]
        public void Ohne_Preisaenderungssatz_bleibt_das_Zahlungsbild_bitgleich()
        {
            KapitalwertRechner.Zahlungsbild alt = Alt(Positionen(BETRAG, N_JAHRE, 0));
            KapitalwertRechner.Zahlungsbild neu = Neu(Positionen(BETRAG, N_JAHRE, 0), 0.0);

            Assert.Equal(alt.Kapitalwert, neu.Kapitalwert);
            Assert.Equal(alt.RestwertNominal, neu.RestwertNominal);
            Assert.Equal(alt.RestwertBarwert, neu.RestwertBarwert);
            Assert.Equal(alt.BarwertAusgaben, neu.BarwertAusgaben);
            for (int t = 0; t <= T; t++)
            {
                Assert.Equal(alt.ErsatzJeJahr[t], neu.ErsatzJeJahr[t]);
                Assert.Equal(alt.BarwertReihe[t], neu.BarwertReihe[t]);
                Assert.Equal(alt.NominalReihe[t], neu.NominalReihe[t]);
            }
        }

        /// <summary>
        /// Und die bekannten Zahlen dieses Falls stehen: Ersatz von 10.000 EUR nominal in
        /// t = 8 und t = 16, sonst nichts, Restwert 10.000 × (8 − 4)/8 = 5.000 EUR,
        /// abgezinst über 20 Jahre.
        /// </summary>
        [Fact]
        public void Ohne_Preisaenderungssatz_stehen_die_bekannten_Betraege()
        {
            KapitalwertRechner.Zahlungsbild z = Neu(Positionen(BETRAG, N_JAHRE, 0), 0.0);

            Assert.Equal(BETRAG, z.Investition);            // Erstbeschaffung in t0
            Assert.Equal(BETRAG, z.ErsatzJeJahr[8]);
            Assert.Equal(BETRAG, z.ErsatzJeJahr[16]);
            for (int t = 0; t <= T; t++)
                if (t != 8 && t != 16) Assert.Equal(0.0, z.ErsatzJeJahr[t]);

            Assert.Equal(5000.0, z.RestwertNominal, 9);
            Assert.Equal(5000.0 * Math.Pow(1.03, -20), z.RestwertBarwert, 9);
        }

        // =====================================================================
        // 2 - p_I = 2 %: Ersatz und Restwert werden indiziert
        // =====================================================================

        /// <summary>
        /// T = 20, n = 8, p_I = 2 %: Der Ersatz in t = 8 kostet 10.000 × 1,02⁸, der in
        /// t = 16 kostet 10.000 × 1,02¹⁶ — indiziert auf das ABSOLUTE Zahlungsjahr, nicht
        /// auf den Abstand zum vorigen Ersatz. Die Erstbeschaffung in t0 bleibt nominal.
        /// </summary>
        [Fact]
        public void Die_Ersatzbeschaffungen_werden_auf_ihr_Zahlungsjahr_indiziert()
        {
            KapitalwertRechner.Zahlungsbild z = Neu(Positionen(BETRAG, N_JAHRE, 0), P_I);

            Assert.Equal(BETRAG, z.Investition);                    // t0 nominal
            Assert.Equal(BETRAG * Math.Pow(1.02, 8), z.ErsatzJeJahr[8], 9);
            Assert.Equal(BETRAG * Math.Pow(1.02, 16), z.ErsatzJeJahr[16], 9);

            // Plausibilitaet in Euro, unabhaengig von Math.Pow nachgerechnet:
            // 1,02^8 = 1,171659381 -> 11.716,59 EUR; 1,02^16 = 1,372785705 -> 13.727,86 EUR.
            Assert.Equal(11716.5938, z.ErsatzJeJahr[8], 4);
            Assert.Equal(13727.8571, z.ErsatzJeJahr[16], 4);
        }

        /// <summary>
        /// Der Restwert steht auf der Preisbasis der LETZTEN Beschaffung: Die Anlage
        /// wurde in t = 16 für 10.000 × 1,02¹⁶ gekauft, am Ende von T = 20 sind vier von
        /// acht Nutzungsjahren übrig — also die Hälfte DIESES Betrags, abgezinst.
        /// </summary>
        [Fact]
        public void Der_Restwert_steht_auf_der_Preisbasis_der_letzten_Beschaffung()
        {
            KapitalwertRechner.Zahlungsbild z = Neu(Positionen(BETRAG, N_JAHRE, 0), P_I);

            double erwartet = BETRAG * Math.Pow(1.02, 16) * (8.0 - 4.0) / 8.0;
            Assert.Equal(erwartet, z.RestwertNominal, 9);
            Assert.Equal(6863.9285, z.RestwertNominal, 4);
            Assert.Equal(erwartet * Math.Pow(1.03, -20), z.RestwertBarwert, 9);
        }

        // =====================================================================
        // 3 - verschobener Start (KD6): die Erstbeschaffung bleibt nominal
        // =====================================================================

        /// <summary>
        /// Startjahr 5, n = 10, T = 20: Die Zahlung im Jahr 5 ist der EINGEGEBENE Betrag
        /// (KD6 — der Betrag gilt zum Zahlungszeitpunkt, er wird nicht zusätzlich
        /// indiziert). Der Ersatz im Jahr 15 trägt dagegen 1,02¹⁵, und der Restwert steht
        /// auf derselben Basis: 10.000 × 1,02¹⁵ × (10 − 5)/10.
        /// </summary>
        [Fact]
        public void Die_verschobene_Erstbeschaffung_bleibt_nominal()
        {
            KapitalwertRechner.Zahlungsbild z = Neu(Positionen(BETRAG, 10.0, 5), P_I);

            Assert.Equal(0.0, z.Investition);                       // nichts in t0
            Assert.Equal(BETRAG, z.InvestitionVerschoben);
            Assert.Equal(BETRAG, z.ErsatzJeJahr[5]);                // nominal, KD6
            Assert.Equal(BETRAG * Math.Pow(1.02, 15), z.ErsatzJeJahr[15], 9);
            Assert.Equal(13458.6834, z.ErsatzJeJahr[15], 4);

            double erwartet = BETRAG * Math.Pow(1.02, 15) * (10.0 - 5.0) / 10.0;
            Assert.Equal(erwartet, z.RestwertNominal, 9);
            Assert.Equal(6729.3417, z.RestwertNominal, 4);
        }

        // =====================================================================
        // 4 - Richtung der Wirkung
        // =====================================================================

        /// <summary>
        /// Wo ersetzt wird, sinkt der Kapitalwert mit p_I &gt; 0 — die Ersatzzahlungen
        /// werden teurer, und der höhere Restwert am Ende gleicht das nicht aus (er wird
        /// über 20 Jahre abgezinst, die Ersatzzahlungen liegen früher).
        /// </summary>
        [Fact]
        public void Mit_Ersatzbeschaffung_sinkt_der_Kapitalwert()
        {
            double ohne = Neu(Positionen(BETRAG, N_JAHRE, 0), 0.0).Kapitalwert;
            double mit = Neu(Positionen(BETRAG, N_JAHRE, 0), P_I).Kapitalwert;

            Assert.True(mit < ohne,
                        "KW mit p_I = 2 % (" + mit + ") liegt nicht unter dem ohne (" + ohne + ").");
        }

        /// <summary>
        /// Ohne Ersatzbeschaffung ändert p_I NICHTS — auch nicht am Restwert: Bei
        /// n = 25 &gt; T = 20 gibt es keinen Ersatz, die letzte (und einzige) Beschaffung
        /// ist die nominale Erstbeschaffung, und ihr Restwert steht auf ihrer eigenen
        /// Preisbasis. Exakter Vergleich, kein Toleranzfenster.
        /// </summary>
        [Fact]
        public void Ohne_Ersatzbeschaffung_aendert_der_Satz_nichts()
        {
            KapitalwertRechner.Zahlungsbild ohne = Neu(Positionen(BETRAG, 25.0, 0), 0.0);
            KapitalwertRechner.Zahlungsbild mit = Neu(Positionen(BETRAG, 25.0, 0), P_I);

            for (int t = 0; t <= T; t++) Assert.Equal(0.0, mit.ErsatzJeJahr[t]);
            Assert.Equal(BETRAG * (25.0 - 20.0) / 25.0, mit.RestwertNominal, 9);
            Assert.Equal(ohne.RestwertNominal, mit.RestwertNominal);
            Assert.Equal(ohne.Kapitalwert, mit.Kapitalwert);
        }
    }
}
