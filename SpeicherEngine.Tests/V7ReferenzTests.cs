using System;
using System.Globalization;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Referenztests des Excel-Kompatibilitaetsmodus gegen die V7-Mappe
    /// (Abnahmekriterien 1 bis 3 des Arbeitspakets AP1).
    /// </summary>
    /// <remarks>
    /// Datengrundlage sind ausschliesslich die beiden CSV-Kopien unter
    /// <c>TestData\</c>; alle Parameter und alle Sollwerte stammen aus
    /// <c>psim_param.csv</c> bzw. <c>psim_daten.csv</c>. Die Simulation laeuft
    /// genau einmal je Testklasseninstanz - xunit erzeugt pro Testmethode eine
    /// neue Instanz, der Lauf dauert wenige Millisekunden.
    /// </remarks>
    public sealed class V7ReferenzTests
    {
        private readonly Referenzdaten.Zeitreihen _reihen;
        private readonly SpeicherParameter _p;
        private readonly SpeicherErgebnis _ergebnis;

        public V7ReferenzTests()
        {
            _reihen = Referenzdaten.Reihen;
            _p = Referenzdaten.V7Parameter();
            _ergebnis = new Dauernutzung(SpeicherModus.ExcelKompatibilitaet)
                .Berechne(Referenzdaten.V7Eingang(), _p);
        }

        // ------------------------------------------------------------------ 0

        /// <summary>Die Referenzdatei muss die erwartete Laenge und Struktur haben.</summary>
        [Fact]
        public void Referenzdatei_Hat_35137_Intervalle()
        {
            Assert.Equal(35137, _reihen.Anzahl);
            Assert.Equal(_reihen.Anzahl, _reihen.PvKw.Length);
            Assert.Equal(_reihen.Anzahl, _reihen.PreisCtKwh.Length);
            Assert.Equal(_reihen.Anzahl, _ergebnis.Anzahl);

            // Parameter des Referenzfalls, so wie sie in psim_param.csv stehen.
            Assert.Equal(5000.0, _p.CNomKwh);
            Assert.Equal(2500.0, _p.PKw);
            Assert.Equal(500.0, _p.SoCMinKwh);
            Assert.Equal(4500.0, _p.SoCMaxKwh);
            Assert.Equal(0.1, _p.VerlustfaktorPauschal);
            Assert.Equal(5.0, _p.VerguetungCtKwh);
            Assert.Equal(1250000.0, _p.InvestitionEur);   // I = J3 * N5 = N6
        }

        // ------------------------------------------------------------------ 1

        /// <summary>
        /// Abnahmekriterium 1: der SoC-Verlauf ist in jedem Intervall
        /// <b>bitgenau</b> gleich der Blattspalte E.
        /// </summary>
        [Fact]
        public void SoC_Ist_Bitgenau_Gleich_Blattspalte_E()
        {
            double[] ist = _ergebnis.SoCKwh;
            double[] soll = _reihen.SollSoCKwh;

            for (int k = 0; k < soll.Length; k++)
            {
                if (!Bitgleich(ist[k], soll[k]))
                {
                    Assert.Fail(
                        "SoC weicht ab in Intervall " + k.ToString(CultureInfo.InvariantCulture) +
                        " (Blattzeile " + (k + 2).ToString(CultureInfo.InvariantCulture) + ")." +
                        Environment.NewLine + "  ist  = " + Bits(ist[k]) +
                        Environment.NewLine + "  soll = " + Bits(soll[k]) +
                        Environment.NewLine + "  Differenz = " +
                        (ist[k] - soll[k]).ToString("R", CultureInfo.InvariantCulture) + " kWh");
                }
            }
        }

        // ------------------------------------------------------------------ 2

        /// <summary>
        /// Abnahmekriterium 2a: je Intervall gilt |F_ist - F_soll| &lt;= 1e-12 EUR.
        /// </summary>
        /// <remarks>
        /// Bitgleichheit ist hier nicht erreichbar und auch nicht gefordert: VBA
        /// wertet <c>-delta * d / 100</c> auf der x87-FPU mit 80-Bit-Zwischenergebnis
        /// aus. Genau 3 der 35.137 Zeilen weichen deshalb um 1 ULP (3,6e-15 EUR) ab
        /// (dokumentiert im Kopf von <c>speicher_sim.py</c>). Die Jahressumme bleibt
        /// davon unberuehrt.
        /// </remarks>
        [Fact]
        public void Geldwert_Je_Intervall_Weicht_Hoechstens_1e_12_Ab()
        {
            double[] ist = _ergebnis.GeldwertEur;
            double[] soll = _reihen.SollGeldwertEur;

            double maxAbw = 0.0;
            int maxIndex = -1;
            for (int k = 0; k < soll.Length; k++)
            {
                double abw = Math.Abs(ist[k] - soll[k]);
                if (abw > maxAbw) { maxAbw = abw; maxIndex = k; }
            }

            Assert.True(maxAbw <= 1e-12,
                "Groesste Abweichung " + maxAbw.ToString("R", CultureInfo.InvariantCulture) +
                " EUR in Intervall " + maxIndex.ToString(CultureInfo.InvariantCulture) +
                " (Blattzeile " + (maxIndex + 2).ToString(CultureInfo.InvariantCulture) + ")." +
                Environment.NewLine + "  ist  = " + Bits(maxIndex < 0 ? 0.0 : ist[maxIndex]) +
                Environment.NewLine + "  soll = " + Bits(maxIndex < 0 ? 0.0 : soll[maxIndex]));
        }

        /// <summary>
        /// Abnahmekriterium 2b: die Jahressumme trifft den Verifikationsanker
        /// Sigma F = 60.616,562388122424 EUR auf 1e-9 EUR.
        /// </summary>
        /// <remarks>
        /// Die Blattformel summiert F2:F35137, die Engine alle 35.137 Werte. Der
        /// zusaetzliche letzte Wert ist exakt 0 und aendert die Summe nicht.
        /// Entscheidend ist die strikt sequenzielle Addition
        /// (<see cref="Numerik.SummeSequenziell(double[])"/>); paarweise oder
        /// kompensierte Verfahren weichen um rund 1e-10 EUR ab.
        /// </remarks>
        [Fact]
        public void Jahressumme_Geldwert_Trifft_Referenz()
        {
            double ist = _ergebnis.SummeGeldwertEur;
            double soll = Referenzdaten.SummeGeldwertSollEur;

            Assert.True(Math.Abs(ist - soll) <= 1e-9,
                "Sigma F = " + ist.ToString("R", CultureInfo.InvariantCulture) +
                " EUR, erwartet " + soll.ToString("R", CultureInfo.InvariantCulture) +
                " EUR (Differenz " + (ist - soll).ToString("R", CultureInfo.InvariantCulture) + ").");

            // Der Anker wird in dieser Konstellation sogar bitgenau getroffen.
            Assert.True(Bitgleich(ist, soll),
                "Sigma F ist nicht bitgleich: ist = " + Bits(ist) + ", soll = " + Bits(soll));
        }

        // ------------------------------------------------------------------ 3

        /// <summary>
        /// Abnahmekriterium 3: der Wirtschaftlichkeitsblock trifft N6/N10/N12/N13/
        /// N15/N16/N17 aus <c>psim_param.csv</c> relativ auf 1e-12.
        /// </summary>
        [Fact]
        public void Wirtschaftlichkeitsblock_Trifft_Blattwerte()
        {
            WirtschaftlichkeitErgebnis w = _ergebnis.Wirtschaftlichkeit;

            // N6 = J3 * N5 - Investition
            Rel("N6", Referenzdaten.Wert("N6"), w.InvestitionEur);

            // N10 = SUM(F2:F35137) * (1 - J5) - Ertrag Referenzjahr
            Rel("N10", Referenzdaten.Wert("N10"), w.ErtragReferenzjahrEur);

            // N12 = Annuitaet (Kapitaldienst)
            Rel("N12", Referenzdaten.Wert("N12"), w.AnnuitaetEur);

            // N13 = N10 - N12 - Jahresgewinn nach Kapitaldienst
            Rel("N13", Referenzdaten.Wert("N13"), w.JahresueberschussEur);

            // N15 = N6 / N10 - statische Amortisation
            Assert.Equal(AmortisationStatus.Amortisierbar, w.StatischeAmortisation.Status);
            Rel("N15", Referenzdaten.Wert("N15"), w.StatischeAmortisation.Jahre);

            // N16 - Textfall "> Nutzungsdauer"
            Assert.Equal("> Nutzungsdauer", Referenzdaten.Text("N16"));
            Assert.Equal(AmortisationStatus.UeberNutzungsdauer, w.DynamischeAmortisation.Status);
            Assert.Equal(Referenzdaten.Text("N16"), w.DynamischeAmortisation.ToString());

            // N17 - Kapitalwert (NPV)
            Rel("N17", Referenzdaten.Wert("N17"), w.KapitalwertEur);
        }

        /// <summary>
        /// Dieselben Sollwerte noch einmal gegen die einzeln portierten
        /// Blattformeln - so schlaegt ein Fehler in der Zusammensetzung
        /// (<see cref="Wirtschaftlichkeit.Berechne"/>) getrennt von einem Fehler in
        /// den Formeln selbst durch.
        /// </summary>
        [Fact]
        public void Portierte_Blattformeln_Treffen_Blattwerte()
        {
            double n6 = Referenzdaten.Wert("J3") * Referenzdaten.Wert("N5");
            double n10 = _ergebnis.SummeGeldwertEur * (1.0 - Referenzdaten.Wert("J5"));
            double zins = Referenzdaten.Wert("N4");
            double dauer = Referenzdaten.Wert("N7");

            Rel("N6", Referenzdaten.Wert("N6"), n6);
            Rel("N10", Referenzdaten.Wert("N10"), n10);

            double n12 = Wirtschaftlichkeit.Annuitaet(n6, zins, dauer);
            Rel("N12", Referenzdaten.Wert("N12"), n12);
            Rel("N13", Referenzdaten.Wert("N13"), n10 - n12);

            Amortisation n15 = Wirtschaftlichkeit.StatischeAmortisation(n10, n6);
            Assert.True(n15.IstAmortisierbar);
            Rel("N15", Referenzdaten.Wert("N15"), n15.Jahre);

            Amortisation n16 = Wirtschaftlichkeit.DynamischeAmortisation(n10, n6, zins, dauer);
            Assert.Equal(Referenzdaten.Text("N16"), n16.ToString());

            double n17 = Wirtschaftlichkeit.Kapitalwert(n10, n6, zins, dauer);
            Rel("N17", Referenzdaten.Wert("N17"), n17);
        }

        /// <summary>
        /// Der Kompatibilitaetsmodus muss reproduzierbar sein und darf den Eingang
        /// nicht veraendern - Voraussetzung fuer die spaetere Rastersuche ueber
        /// <c>Parallel.For</c> (Fachkonzept 8.1).
        /// </summary>
        [Fact]
        public void Wiederholter_Lauf_Liefert_Bitgleiches_Ergebnis()
        {
            SpeicherEingang eingang = Referenzdaten.V7Eingang();
            var strategie = new Dauernutzung(SpeicherModus.ExcelKompatibilitaet);

            SpeicherErgebnis a = strategie.Berechne(eingang, _p);
            SpeicherErgebnis b = strategie.Berechne(eingang, _p);

            Assert.True(Bitgleich(a.SummeGeldwertEur, b.SummeGeldwertEur));
            for (int k = 0; k < a.Anzahl; k++)
            {
                Assert.True(Bitgleich(a.SoCKwh[k], b.SoCKwh[k]));
                Assert.True(Bitgleich(a.GeldwertEur[k], b.GeldwertEur[k]));
            }

            // Eingang unveraendert?
            for (int k = 0; k < eingang.Anzahl; k++)
            {
                Assert.True(Bitgleich(eingang.LastKw[k], _reihen.LastKw[k]));
                Assert.True(Bitgleich(eingang.PvKw[k], _reihen.PvKw[k]));
                Assert.True(Bitgleich(eingang.PreisCtKwh[k], _reihen.PreisCtKwh[k]));
            }
        }

        // ------------------------------------------------------------------ Hilfen

        private static void Rel(string name, double soll, double ist)
        {
            double nenner = Math.Abs(soll);
            double abw = Math.Abs(ist - soll);
            double rel = nenner > 0.0 ? abw / nenner : abw;
            Assert.True(rel <= 1e-12,
                name + ": ist = " + ist.ToString("R", CultureInfo.InvariantCulture) +
                ", soll = " + soll.ToString("R", CultureInfo.InvariantCulture) +
                ", relative Abweichung = " + rel.ToString("R", CultureInfo.InvariantCulture));
        }

        internal static bool Bitgleich(double a, double b)
            => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);

        internal static string Bits(double d)
            => d.ToString("R", CultureInfo.InvariantCulture) +
               " [0x" + BitConverter.DoubleToInt64Bits(d).ToString("X16", CultureInfo.InvariantCulture) + "]";
    }
}
