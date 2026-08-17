using System;
using System.Globalization;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Grenzfaelle und geschlossene Formeln der Wirtschaftlichkeitsrechnung
    /// (Abnahmekriterium 4 des Arbeitspakets AP1, Fachkonzept 5.3 / 6.2).
    /// </summary>
    public sealed class WirtschaftlichkeitTests
    {
        // ------------------------------------------------------------- RBF_deg

        /// <summary>
        /// Beispiel des Fachkonzepts 5.3: d = 0,1 %/a, i_z = 3 %, N = 20 a
        /// ergibt RBF_deg = 14,751 (gegen 14,877 ohne Degradation).
        /// </summary>
        [Fact]
        public void RbfDeg_Trifft_Fachkonzeptbeispiel()
        {
            double rbfDeg = Wirtschaftlichkeit.RbfDeg(0.001, 0.03, 20.0);
            Assert.True(Math.Abs(rbfDeg - 14.751) <= 0.001,
                "RBF_deg = " + rbfDeg.ToString("R", CultureInfo.InvariantCulture) + ", erwartet 14,751 +/- 0,001.");

            double rbfOhne = Wirtschaftlichkeit.RbfDeg(0.0, 0.03, 20.0);
            Assert.True(Math.Abs(rbfOhne - 14.877) <= 0.001,
                "RBF ohne Degradation = " + rbfOhne.ToString("R", CultureInfo.InvariantCulture) + ", erwartet 14,877 +/- 0,001.");

            // Abschlag rund 0,85 %.
            double abschlag = 1.0 - rbfDeg / rbfOhne;
            Assert.True(Math.Abs(abschlag - 0.0085) <= 0.0005,
                "Abschlag = " + abschlag.ToString("R", CultureInfo.InvariantCulture) + ", erwartet rund 0,85 %.");
        }

        /// <summary>Grenzfall d = 0: RBF_deg faellt auf den gewoehnlichen Rentenbarwertfaktor zurueck.</summary>
        [Theory]
        [InlineData(0.03, 20.0)]
        [InlineData(0.07, 15.0)]
        [InlineData(0.001, 25.0)]
        public void RbfDeg_Ohne_Degradation_Ist_Rentenbarwertfaktor(double zins, double dauer)
        {
            double erwartet = (1.0 - Math.Pow(1.0 + zins, -dauer)) / zins;
            Assert.Equal(erwartet, Wirtschaftlichkeit.RbfDeg(0.0, zins, dauer), 12);
            Assert.Equal(erwartet, Wirtschaftlichkeit.Rentenbarwertfaktor(zins, dauer), 12);
        }

        /// <summary>Grenzfall i_z = 0: RBF_deg = (1 - (1-d)^N) / d.</summary>
        [Theory]
        [InlineData(0.001, 20.0)]
        [InlineData(0.02, 20.0)]
        [InlineData(0.05, 10.0)]
        public void RbfDeg_Ohne_Zins_Nutzt_Geschlossenen_Grenzfall(double d, double dauer)
        {
            double erwartet = (1.0 - Math.Pow(1.0 - d, dauer)) / d;
            Assert.Equal(erwartet, Wirtschaftlichkeit.RbfDeg(d, 0.0, dauer), 12);
        }

        /// <summary>Grenzfall d = 0 und i_z = 0: RBF_deg = N.</summary>
        [Theory]
        [InlineData(20.0)]
        [InlineData(1.0)]
        [InlineData(30.0)]
        public void RbfDeg_Ohne_Zins_Und_Ohne_Degradation_Ist_Nutzungsdauer(double dauer)
        {
            Assert.Equal(dauer, Wirtschaftlichkeit.RbfDeg(0.0, 0.0, dauer));
            Assert.Equal(dauer, Wirtschaftlichkeit.Rentenbarwertfaktor(0.0, dauer));
        }

        /// <summary>
        /// Unabhaengige Gegenprobe: die geschlossene Form muss der jahresscharfen
        /// Barwertsummation entsprechen,
        /// RBF_deg = Summe ueber t = 1..N von (1-d)^(t-1) / (1+i_z)^t.
        /// </summary>
        [Theory]
        [InlineData(0.001, 0.03, 20.0)]
        [InlineData(0.02, 0.05, 15.0)]
        [InlineData(0.0, 0.04, 12.0)]
        [InlineData(0.03, 0.0, 18.0)]
        [InlineData(0.0, 0.0, 20.0)]
        public void RbfDeg_Entspricht_Jahresscharfer_Barwertsummation(double d, double zins, double dauer)
        {
            int n = (int)dauer;
            double summe = 0.0;
            for (int t = 1; t <= n; t++)
                summe += Math.Pow(1.0 - d, t - 1) / Math.Pow(1.0 + zins, t);

            double geschlossen = Wirtschaftlichkeit.RbfDeg(d, zins, dauer);
            double rel = Math.Abs(geschlossen - summe) / Math.Abs(summe);
            Assert.True(rel <= 1e-12,
                "RBF_deg geschlossen = " + geschlossen.ToString("R", CultureInfo.InvariantCulture) +
                ", summiert = " + summe.ToString("R", CultureInfo.InvariantCulture) +
                ", relative Abweichung " + rel.ToString("R", CultureInfo.InvariantCulture));
        }

        // --------------------------------------------------- Annuitaet / Kapitalwert

        /// <summary>Grenzfall i_z = 0: A = I / N und a = 1 / N.</summary>
        [Fact]
        public void Annuitaet_Ohne_Zins_Ist_Investition_Durch_Nutzungsdauer()
        {
            Assert.Equal(1250000.0 / 20.0, Wirtschaftlichkeit.Annuitaet(1250000.0, 0.0, 20.0));
            Assert.Equal(1.0 / 20.0, Wirtschaftlichkeit.Annuitaetsfaktor(0.0, 20.0));
            Assert.Equal(Wirtschaftlichkeit.Annuitaet(1250000.0, 0.03, 20.0),
                         1250000.0 * Wirtschaftlichkeit.Annuitaetsfaktor(0.03, 20.0), 9);
        }

        /// <summary>Grenzfall i_z = 0: NPV = E * N - I.</summary>
        [Fact]
        public void Kapitalwert_Ohne_Zins_Ist_Linear()
        {
            Assert.Equal(50000.0 * 20.0 - 1250000.0, Wirtschaftlichkeit.Kapitalwert(50000.0, 1250000.0, 0.0, 20.0));
        }

        // ------------------------------------------------------------ Amortisation

        /// <summary>Ertrag kleiner oder gleich 0: beide Amortisationen melden "nicht amortisierbar".</summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(-12345.678)]
        public void Amortisation_Ohne_Ertrag_Ist_Nicht_Amortisierbar(double ertrag)
        {
            Amortisation stat = Wirtschaftlichkeit.StatischeAmortisation(ertrag, 1250000.0);
            Amortisation dyn = Wirtschaftlichkeit.DynamischeAmortisation(ertrag, 1250000.0, 0.03, 20.0);

            Assert.Equal(AmortisationStatus.NichtAmortisierbar, stat.Status);
            Assert.Equal(AmortisationStatus.NichtAmortisierbar, dyn.Status);
            Assert.False(stat.IstAmortisierbar);
            Assert.Equal("nicht amortisierbar", stat.ToString());
            Assert.Equal("nicht amortisierbar", dyn.ToString());
            Assert.Equal(double.PositiveInfinity, stat.Jahre);
        }

        /// <summary>Grenzfall i_z = 0: die dynamische Amortisation faellt auf die statische zurueck.</summary>
        [Fact]
        public void Dynamische_Amortisation_Ohne_Zins_Ist_Statisch()
        {
            Amortisation dyn = Wirtschaftlichkeit.DynamischeAmortisation(100000.0, 1250000.0, 0.0, 20.0);
            Assert.Equal(AmortisationStatus.Amortisierbar, dyn.Status);
            Assert.Equal(12.5, dyn.Jahre);
        }

        /// <summary>
        /// Reicht der Barwert der Ertraege ueber die Nutzungsdauer nicht an die
        /// Investition heran, meldet die dynamische Amortisation "&gt; Nutzungsdauer".
        /// </summary>
        [Fact]
        public void Dynamische_Amortisation_Meldet_Ueber_Nutzungsdauer()
        {
            // 54.554,91 * RBF(3 %, 20 a) = 811.639 EUR < 1.250.000 EUR
            Amortisation dyn = Wirtschaftlichkeit.DynamischeAmortisation(54554.906149310184, 1250000.0, 0.03, 20.0);
            Assert.Equal(AmortisationStatus.UeberNutzungsdauer, dyn.Status);
            Assert.Equal("> Nutzungsdauer", dyn.ToString());
        }

        /// <summary>Normalfall: die dynamische Amortisation liegt ueber der statischen und unter N.</summary>
        [Fact]
        public void Dynamische_Amortisation_Im_Normalfall()
        {
            const double ertrag = 150000.0;
            const double invest = 1250000.0;
            const double zins = 0.03;
            const double dauer = 20.0;

            Amortisation stat = Wirtschaftlichkeit.StatischeAmortisation(ertrag, invest);
            Amortisation dyn = Wirtschaftlichkeit.DynamischeAmortisation(ertrag, invest, zins, dauer);

            Assert.Equal(AmortisationStatus.Amortisierbar, dyn.Status);
            double erwartet = -Math.Log(1.0 - invest * zins / ertrag) / Math.Log(1.0 + zins);
            Assert.Equal(erwartet, dyn.Jahre, 12);
            Assert.True(dyn.Jahre > stat.Jahre);
            Assert.True(dyn.Jahre < dauer);
        }

        // --------------------------------------------------------------- Gesamtblock

        /// <summary>
        /// Ohne Degradation ist der Block identisch zur V7-Logik: E_a,aeq = E_a,1,
        /// NPV = E_a,1 * RBF - I.
        /// </summary>
        [Fact]
        public void Block_Ohne_Degradation_Entspricht_V7()
        {
            var e = new WirtschaftlichkeitEingang
            {
                ErtragReferenzjahrEur = 54554.906149310184,
                InvestitionEur = 1250000.0,
                Kapitalzins = 0.03,
                NutzungsdauerA = 20.0,
                DegradationProA = 0.0
            };
            WirtschaftlichkeitErgebnis w = Wirtschaftlichkeit.Berechne(e);

            Assert.Equal(e.ErtragReferenzjahrEur, w.ErtragAequivalentEur, 9);
            Assert.Equal(Wirtschaftlichkeit.Kapitalwert(e.ErtragReferenzjahrEur, e.InvestitionEur, 0.03, 20.0),
                         w.KapitalwertEur, 6);
            Assert.Equal(Wirtschaftlichkeit.Annuitaet(e.InvestitionEur, 0.03, 20.0), w.AnnuitaetEur);
        }

        /// <summary>Grenzfall i_z = 0 und d = 0 im Gesamtblock: RBF_deg = N, a = 1/N.</summary>
        [Fact]
        public void Block_Ohne_Zins_Und_Ohne_Degradation()
        {
            var e = new WirtschaftlichkeitEingang
            {
                ErtragReferenzjahrEur = 100000.0,
                InvestitionEur = 1250000.0,
                Kapitalzins = 0.0,
                NutzungsdauerA = 20.0,
                DegradationProA = 0.0
            };
            WirtschaftlichkeitErgebnis w = Wirtschaftlichkeit.Berechne(e);

            Assert.Equal(20.0, w.RbfDeg);
            Assert.Equal(1.0 / 20.0, w.Annuitaetsfaktor);
            Assert.Equal(100000.0, w.ErtragAequivalentEur, 9);
            Assert.Equal(62500.0, w.AnnuitaetEur);
            Assert.Equal(37500.0, w.JahresueberschussEur, 9);
            Assert.Equal(100000.0 * 20.0 - 1250000.0, w.KapitalwertEur, 6);
            Assert.Equal(12.5, w.StatischeAmortisation.Jahre, 9);
            Assert.Equal(12.5, w.DynamischeAmortisation.Jahre, 9);
        }

        /// <summary>Grenzfall i_z = 0 mit Degradation: RBF_deg = (1 - (1-d)^N)/d, a = 1/N.</summary>
        [Fact]
        public void Block_Ohne_Zins_Mit_Degradation()
        {
            var e = new WirtschaftlichkeitEingang
            {
                ErtragReferenzjahrEur = 100000.0,
                InvestitionEur = 1250000.0,
                Kapitalzins = 0.0,
                NutzungsdauerA = 20.0,
                DegradationProA = 0.001
            };
            WirtschaftlichkeitErgebnis w = Wirtschaftlichkeit.Berechne(e);

            double rbf = (1.0 - Math.Pow(0.999, 20.0)) / 0.001;
            Assert.Equal(rbf, w.RbfDeg, 12);
            Assert.Equal(100000.0 * rbf * (1.0 / 20.0), w.ErtragAequivalentEur, 9);
            Assert.Equal(100000.0 * rbf - 1250000.0, w.KapitalwertEur, 6);
            Assert.True(w.ErtragAequivalentEur < 100000.0);   // Degradation mindert den Ertrag
        }

        /// <summary>Degradation senkt Kapitalwert und aequivalenten Jahresertrag.</summary>
        [Fact]
        public void Degradation_Mindert_Kapitalwert()
        {
            var ohne = new WirtschaftlichkeitEingang
            {
                ErtragReferenzjahrEur = 150000.0,
                InvestitionEur = 1250000.0,
                Kapitalzins = 0.03,
                NutzungsdauerA = 20.0,
                DegradationProA = 0.0
            };
            var mit = ohne with { DegradationProA = 0.001 };

            WirtschaftlichkeitErgebnis wOhne = Wirtschaftlichkeit.Berechne(ohne);
            WirtschaftlichkeitErgebnis wMit = Wirtschaftlichkeit.Berechne(mit);

            Assert.True(wMit.RbfDeg < wOhne.RbfDeg);
            Assert.True(wMit.KapitalwertEur < wOhne.KapitalwertEur);
            Assert.True(wMit.ErtragAequivalentEur < wOhne.ErtragAequivalentEur);
            Assert.Equal(wOhne.AnnuitaetEur, wMit.AnnuitaetEur);           // Kapitaldienst unveraendert

            // NPV_deg = E_a,1 * RBF_deg - I
            Assert.Equal(150000.0 * wMit.RbfDeg - 1250000.0, wMit.KapitalwertEur, 6);
            // E_a,aeq = E_a,1 * RBF_deg * a
            Assert.Equal(150000.0 * wMit.RbfDeg * wMit.Annuitaetsfaktor, wMit.ErtragAequivalentEur, 9);
        }
    }
}
