using System;
using System.Globalization;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests des energetischen Produktivmodus (Abnahmekriterium 5 des
    /// Arbeitspakets AP1, Fachkonzept 5.2 / 6.2).
    /// </summary>
    /// <remarks>
    /// Das Mini-Beispiel ist so gewaehlt, dass jeder Zwischenwert von Hand
    /// nachrechenbar bleibt: dt = 0,25 h, P = 10 kW (also P*dt = 2,5 kWh),
    /// SoC-Band 0 .. 10 kWh, eta_RT = 0,81 und damit
    /// eta_ch = eta_dis = sqrt(0,81) = 0,9 <b>exakt</b> als IEEE-754-Double.
    /// </remarks>
    public sealed class EnergetischerModusTests
    {
        // Mini-Eingang: drei Ladeintervalle, zwei Entladeintervalle, ein Leerlauf.
        private static readonly double[] MiniLastKw = { 0.0, 0.0, 0.0, 20.0, 20.0, 0.0 };
        private static readonly double[] MiniPvKw = { 8.0, 8.0, 8.0, 0.0, 0.0, 0.0 };
        private const double MiniPreisCtKwh = 20.0;
        private const double MiniVerguetungCtKwh = 5.0;

        private static SpeicherParameter MiniParameter(double etaRt = 0.81) => new SpeicherParameter
        {
            CNomKwh = 10.0,
            PKw = 10.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 10.0,
            RoundTripWirkungsgrad = etaRt,
            DtH = 0.25,
            VerguetungCtKwh = MiniVerguetungCtKwh,
            CCapEurProKwh = 500.0,
            Kapitalzins = 0.03,
            NutzungsdauerA = 20.0,
            DegradationProA = 0.0
        };

        private static SpeicherEingang MiniEingang()
            => SpeicherEingang.MitFixpreis(MiniLastKw, MiniPvKw, MiniPreisCtKwh);

        // ------------------------------------------------------------------ 5a

        /// <summary>
        /// Von Hand nachgerechnetes Mini-Beispiel (eta_ch = eta_dis = 0,9):
        /// <code>
        /// k=0 Ueberschuss  8 kW -> E_ac = 2,00 kWh (&lt; 2,5)   SoC = 0,0 + 2,00*0,9 = 1,80   F = -2,00*5/100  = -0,100
        /// k=1 wie k=0                                          SoC = 3,60                     F = -0,100
        /// k=2 wie k=0                                          SoC = 5,40                     F = -0,100
        /// k=3 Defizit    20 kW -> 5,00 kWh, Leistungsgrenze    SoC = 5,40 - 2,50/0,9 = 2,6222 F = +2,50*20/100 = +0,500
        ///     E_ac = 2,50 kWh   (SoC-Grenze 5,40*0,9 = 4,86 greift nicht)
        /// k=4 Defizit    20 kW -> 5,00 kWh, SoC-Grenze         SoC = 0,00                     F = +2,36*20/100 = +0,472
        ///     E_ac = 2,6222*0,9 = 2,36 kWh
        /// k=5 weder Ueberschuss noch Defizit                   SoC = 0,00                     F =  0,000
        /// </code>
        /// Summen: Ladeenergie 6,00 kWh (AC), Entladeenergie 4,86 kWh (AC),
        /// Sigma F = 0,672 EUR. Energiebilanz: 6,00*0,9 - 4,86/0,9 = 0 = SoC_Ende - SoC_Start.
        /// </summary>
        [Fact]
        public void Energetisch_Mini_Rechnet_Von_Hand_Nach()
        {
            SpeicherErgebnis r = new Dauernutzung(SpeicherModus.Energetisch)
                .Berechne(MiniEingang(), MiniParameter());

            double[] sollSoc = { 1.8, 3.6, 5.4, 5.4 - 2.5 / 0.9, 0.0, 0.0 };
            double[] sollGeld = { -0.1, -0.1, -0.1, 0.5, 0.472, 0.0 };

            Assert.Equal(6, r.Anzahl);
            for (int k = 0; k < 6; k++)
            {
                Assert.True(Math.Abs(r.SoCKwh[k] - sollSoc[k]) <= 1e-12,
                    "SoC[" + k + "] = " + r.SoCKwh[k].ToString("R", CultureInfo.InvariantCulture) +
                    ", erwartet " + sollSoc[k].ToString("R", CultureInfo.InvariantCulture));
                Assert.True(Math.Abs(r.GeldwertEur[k] - sollGeld[k]) <= 1e-12,
                    "F[" + k + "] = " + r.GeldwertEur[k].ToString("R", CultureInfo.InvariantCulture) +
                    ", erwartet " + sollGeld[k].ToString("R", CultureInfo.InvariantCulture));
            }

            Assert.Equal(6.0, r.LadeenergieKwh, 12);
            Assert.Equal(4.86, r.EntladeenergieKwh, 12);
            Assert.Equal(0.672, r.SummeGeldwertEur, 12);
            Assert.Equal(SpeicherModus.Energetisch, r.Modus);

            // Gesamtbilanz: geladen*eta_ch - entladen/eta_dis = SoC_Ende - SoC_Start
            double bilanz = r.LadeenergieKwh * 0.9 - r.EntladeenergieKwh / 0.9;
            Assert.Equal(r.SoCKwh[5] - 0.0, bilanz, 12);
        }

        // ------------------------------------------------------------------ 5b

        /// <summary>
        /// Der Ladezustand verlaesst das Band SoC_min .. SoC_max nicht, und die
        /// Energiebilanz schliesst in <b>jedem</b> Intervall.
        /// </summary>
        /// <remarks>
        /// Toleranz 1e-9 kWh: die Engine kappt bewusst nicht auf das Band, damit die
        /// Formeln des Fachkonzepts 5.2 unveraendert bleiben. Ein Intervall, das den
        /// Speicher genau leer faehrt, kann durch <c>E_ac = (SoC - SoC_min)*eta_dis</c>
        /// und die anschliessende Division durch eta_dis um wenige ULP unter SoC_min
        /// landen. Physikalisch ist das bedeutungslos.
        /// </remarks>
        [Theory]
        [InlineData(0.81)]
        [InlineData(0.90)]
        [InlineData(1.00)]
        public void Energetisch_SoC_Bleibt_Im_Band_Und_Bilanz_Schliesst(double etaRt)
        {
            SpeicherParameter p = Referenzdaten.V7Parameter() with
            {
                RoundTripWirkungsgrad = etaRt
            };
            SpeicherEingang eingang = Referenzdaten.V7Eingang();
            SpeicherErgebnis r = new Dauernutzung(SpeicherModus.Energetisch).Berechne(eingang, p);

            const double toleranz = 1e-9;
            double etaCh = p.EtaCh;
            double etaDis = p.EtaDis;
            double dt = p.DtH;
            double leistungsgrenze = p.PKw * dt;

            double prev = p.StartSoCEffektivKwh;
            Assert.Equal(p.SoCMinKwh, prev);   // Default-Start ist SoC_min

            double summeLade = 0.0;
            double summeEntlade = 0.0;

            for (int k = 0; k < r.Anzahl; k++)
            {
                double soc = r.SoCKwh[k];

                Assert.True(soc >= p.SoCMinKwh - toleranz,
                    "SoC[" + k + "] = " + soc.ToString("R", CultureInfo.InvariantCulture) +
                    " liegt unter SoC_min = " + p.SoCMinKwh.ToString("R", CultureInfo.InvariantCulture));
                Assert.True(soc <= p.SoCMaxKwh + toleranz,
                    "SoC[" + k + "] = " + soc.ToString("R", CultureInfo.InvariantCulture) +
                    " liegt ueber SoC_max = " + p.SoCMaxKwh.ToString("R", CultureInfo.InvariantCulture));

                // Unabhaengige Nachrechnung der AC-Energien aus Eingang und SoC[k-1].
                double lade = 0.0;
                double entlade = 0.0;
                if (eingang.PvKw[k] > eingang.LastKw[k])
                {
                    lade = Math.Max(0.0, Math.Min((eingang.PvKw[k] - eingang.LastKw[k]) * dt,
                                                  Math.Min(leistungsgrenze, (p.SoCMaxKwh - prev) / etaCh)));
                }
                else
                {
                    entlade = Math.Max(0.0, Math.Min((eingang.LastKw[k] - eingang.PvKw[k]) * dt,
                                                      Math.Min(leistungsgrenze, (prev - p.SoCMinKwh) * etaDis)));
                }

                double erwartet = prev + lade * etaCh - entlade / etaDis;
                Assert.True(Math.Abs(soc - erwartet) <= 1e-9,
                    "Energiebilanz verletzt in Intervall " + k + ": SoC = " +
                    soc.ToString("R", CultureInfo.InvariantCulture) + ", erwartet " +
                    erwartet.ToString("R", CultureInfo.InvariantCulture));

                // Bewertung: Entladung zum Bezugspreis, Ladung zur entgangenen Verguetung.
                double geldErwartet = entlade > 0.0
                    ? entlade * eingang.PreisCtKwh[k] / 100.0
                    : (lade > 0.0 ? -lade * p.VerguetungCtKwh / 100.0 : 0.0);
                Assert.True(Math.Abs(r.GeldwertEur[k] - geldErwartet) <= 1e-9,
                    "Geldwert weicht ab in Intervall " + k);

                summeLade += lade;
                summeEntlade += entlade;
                prev = soc;
            }

            Assert.Equal(summeLade, r.LadeenergieKwh, 6);
            Assert.Equal(summeEntlade, r.EntladeenergieKwh, 6);

            // Gesamtbilanz ueber das Jahr.
            Assert.Equal(r.SoCKwh[r.Anzahl - 1] - p.StartSoCEffektivKwh,
                         r.LadeenergieKwh * etaCh - r.EntladeenergieKwh / etaDis, 6);
        }

        /// <summary>Verluste verkleinern den Ertrag: eta_RT &lt; 1 liefert weniger als eta_RT = 1.</summary>
        [Fact]
        public void Energetisch_Kleinerer_Wirkungsgrad_Liefert_Weniger_Ertrag()
        {
            SpeicherParameter p1 = Referenzdaten.V7Parameter() with { RoundTripWirkungsgrad = 1.00 };
            SpeicherParameter p2 = p1 with { RoundTripWirkungsgrad = 0.81 };
            var strategie = new Dauernutzung(SpeicherModus.Energetisch);
            SpeicherEingang eingang = Referenzdaten.V7Eingang();

            SpeicherErgebnis r1 = strategie.Berechne(eingang, p1);
            SpeicherErgebnis r2 = strategie.Berechne(eingang, p2);

            Assert.True(r2.EntladeenergieKwh < r1.EntladeenergieKwh);
            Assert.True(r2.SummeGeldwertEur < r1.SummeGeldwertEur);
        }

        // ------------------------------------------------------------------ 5c

        /// <summary>
        /// Bei eta_RT = 1 rechnet der energetische Modus - ab gleichem Start-SoC und
        /// gleicher Startzeile - identisch zum Excel-Kompatibilitaetsmodus.
        /// </summary>
        /// <remarks>
        /// Der Kompatibilitaetsmodus laesst Index 0 aus und startet bei SoC = 0; der
        /// energetische Modus rechnet ab Index 0. Verglichen wird deshalb der
        /// energetische Lauf ueber <c>Ausschnitt(1, n-1)</c> mit StartSoC = 0 gegen
        /// die Intervalle 1 .. n-1 des Kompatibilitaetslaufs.
        /// Der SoC-Verlauf ist dabei bitgleich: mit eta = 1 sind <c>x*eta</c> und
        /// <c>x/eta</c> exakte Identitaeten, und die Reihenfolge der Begrenzungen ist
        /// in beiden Zweigen dieselbe. Der Geldwert kann um wenige ULP abweichen, weil
        /// der Kompatibilitaetsmodus ueber <c>delta = SoC_neu - SoC_alt</c> bewertet,
        /// der energetische Modus direkt ueber die AC-Energie.
        /// </remarks>
        [Fact]
        public void Energetisch_Bei_Eta_1_Identisch_Zum_Kompatibilitaetsmodus()
        {
            SpeicherParameter pKompat = Referenzdaten.V7Parameter();
            SpeicherEingang voll = Referenzdaten.V7Eingang();

            SpeicherErgebnis kompat = new Dauernutzung(SpeicherModus.ExcelKompatibilitaet)
                .Berechne(voll, pKompat);

            SpeicherParameter pEnerg = pKompat with
            {
                RoundTripWirkungsgrad = 1.0,
                StartSoCKwh = 0.0
            };
            SpeicherErgebnis energ = new Dauernutzung(SpeicherModus.Energetisch)
                .Berechne(voll.Ausschnitt(1, voll.Anzahl - 1), pEnerg);

            Assert.Equal(kompat.Anzahl - 1, energ.Anzahl);

            for (int k = 0; k < energ.Anzahl; k++)
            {
                Assert.True(V7ReferenzTests.Bitgleich(energ.SoCKwh[k], kompat.SoCKwh[k + 1]),
                    "SoC weicht ab in Intervall " + k + ": energetisch = " +
                    V7ReferenzTests.Bits(energ.SoCKwh[k]) + ", kompatibel = " +
                    V7ReferenzTests.Bits(kompat.SoCKwh[k + 1]));

                double abw = Math.Abs(energ.GeldwertEur[k] - kompat.GeldwertEur[k + 1]);
                Assert.True(abw <= 1e-12,
                    "Geldwert weicht ab in Intervall " + k + " um " +
                    abw.ToString("R", CultureInfo.InvariantCulture) + " EUR");
            }

            // Jahressumme trifft den Verifikationsanker ebenfalls.
            Assert.True(Math.Abs(energ.SummeGeldwertEur - Referenzdaten.SummeGeldwertSollEur) <= 1e-9,
                "Sigma F energetisch = " + energ.SummeGeldwertEur.ToString("R", CultureInfo.InvariantCulture));

            // Ohne pauschalen Verlustabschlag ist E_a,1 hier die ungekuerzte Summe.
            Assert.Equal(energ.SummeGeldwertEur, energ.Wirtschaftlichkeit.ErtragReferenzjahrEur);
            Assert.Equal(kompat.SummeGeldwertEur * (1.0 - pKompat.VerlustfaktorPauschal),
                         kompat.Wirtschaftlichkeit.ErtragReferenzjahrEur);
        }

        /// <summary>Dieselbe Gleichheit noch einmal am Mini-Beispiel (eta_RT = 1).</summary>
        [Fact]
        public void Energetisch_Bei_Eta_1_Identisch_Zum_Kompatibilitaetsmodus_Mini()
        {
            SpeicherParameter p = MiniParameter(1.0) with { SoCMinKwh = 0.0, SoCMaxKwh = 10.0 };
            SpeicherEingang voll = MiniEingang();

            SpeicherErgebnis kompat = new Dauernutzung(SpeicherModus.ExcelKompatibilitaet).Berechne(voll, p);
            SpeicherErgebnis energ = new Dauernutzung(SpeicherModus.Energetisch)
                .Berechne(voll.Ausschnitt(1, voll.Anzahl - 1), p with { StartSoCKwh = 0.0 });

            for (int k = 0; k < energ.Anzahl; k++)
            {
                Assert.True(V7ReferenzTests.Bitgleich(energ.SoCKwh[k], kompat.SoCKwh[k + 1]));
                Assert.True(Math.Abs(energ.GeldwertEur[k] - kompat.GeldwertEur[k + 1]) <= 1e-12);
            }
        }

        // ------------------------------------------------------------------ Start-SoC

        /// <summary>Ohne Angabe startet der energetische Modus bei SoC_min.</summary>
        [Fact]
        public void Energetisch_Startet_Standardmaessig_Bei_SoC_Min()
        {
            SpeicherParameter p = MiniParameter() with { SoCMinKwh = 2.0 };
            Assert.Equal(2.0, p.StartSoCEffektivKwh);

            // Erstes Intervall laedt 2 kWh AC -> SoC = 2,0 + 2,0*0,9 = 3,8
            SpeicherErgebnis r = new Dauernutzung(SpeicherModus.Energetisch).Berechne(MiniEingang(), p);
            Assert.Equal(3.8, r.SoCKwh[0], 12);
        }

        /// <summary>Ein abweichender Start-SoC wird uebernommen.</summary>
        [Fact]
        public void Energetisch_Uebernimmt_Abweichenden_Start_SoC()
        {
            SpeicherParameter p = MiniParameter() with { StartSoCKwh = 5.0 };
            Assert.Equal(5.0, p.StartSoCEffektivKwh);

            SpeicherErgebnis r = new Dauernutzung(SpeicherModus.Energetisch).Berechne(MiniEingang(), p);
            Assert.Equal(5.0 + 2.0 * 0.9, r.SoCKwh[0], 12);
        }

        /// <summary>Der Kompatibilitaetsmodus ignoriert den Start-SoC und beginnt bei 0.</summary>
        [Fact]
        public void Kompatibilitaetsmodus_Ignoriert_Start_SoC()
        {
            SpeicherParameter p = MiniParameter() with { StartSoCKwh = 7.0 };
            SpeicherErgebnis r = new Dauernutzung(SpeicherModus.ExcelKompatibilitaet).Berechne(MiniEingang(), p);

            Assert.Equal(0.0, r.SoCKwh[0]);        // Index 0 wird nicht simuliert
            Assert.Equal(0.0, r.GeldwertEur[0]);
            Assert.Equal(2.0, r.SoCKwh[1], 12);    // ab 0 geladen, eta = 1
        }

        // ------------------------------------------------------------------ Vertrag

        /// <summary>Die Strategie ist unveraenderlich und liefert einen sprechenden Namen.</summary>
        [Fact]
        public void Strategie_Ist_Unveraenderlich_Und_Benannt()
        {
            ISpeicherStrategie energetisch = new Dauernutzung(SpeicherModus.Energetisch);
            ISpeicherStrategie kompatibel = new Dauernutzung(SpeicherModus.ExcelKompatibilitaet);

            Assert.Equal("Dauernutzung", energetisch.Name);
            Assert.Equal("Dauernutzung (Excel-Kompatibilitaet)", kompatibel.Name);
            Assert.Equal(SpeicherModus.Energetisch, new Dauernutzung().Modus);
        }

        /// <summary>Ungueltige Parameter und Eingaenge werden abgewiesen.</summary>
        [Fact]
        public void Ungueltige_Eingaben_Werden_Abgewiesen()
        {
            var strategie = new Dauernutzung(SpeicherModus.Energetisch);

            Assert.Throws<ArgumentNullException>(() => strategie.Berechne(null!, MiniParameter()));
            Assert.Throws<ArgumentNullException>(() => strategie.Berechne(MiniEingang(), null!));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                strategie.Berechne(MiniEingang(), MiniParameter() with { DtH = 0.0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                strategie.Berechne(MiniEingang(), MiniParameter() with { SoCMaxKwh = -1.0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                strategie.Berechne(MiniEingang(), MiniParameter() with { RoundTripWirkungsgrad = 1.5 }));

            Assert.Throws<ArgumentException>(() => new SpeicherEingang(new double[3], new double[2], new double[3]));
            Assert.Throws<ArgumentException>(() => new SpeicherEingang(new double[0], new double[0], new double[0]));
            Assert.Throws<ArgumentNullException>(() => new SpeicherEingang(null!, new double[1], new double[1]));
        }

        /// <summary>Der Fixpreis-Konstruktor befuellt die Preisreihe konstant.</summary>
        [Fact]
        public void Fixpreis_Eingang_Ist_Konstant_Befuellt()
        {
            SpeicherEingang e = SpeicherEingang.MitFixpreis(MiniLastKw, MiniPvKw, 20.0);
            Assert.Equal(MiniLastKw.Length, e.Anzahl);
            for (int k = 0; k < e.Anzahl; k++) Assert.Equal(20.0, e.PreisCtKwh[k]);
        }

        /// <summary>Die sequenzielle Summation ist die Referenz - kein LINQ, keine Kompensation.</summary>
        [Fact]
        public void Sequenzielle_Summation_Addiert_In_Reihenfolge()
        {
            double[] werte = { 1.0, 1e-16, 1e-16, -1.0 };
            Assert.Equal(0.0, Numerik.SummeSequenziell(werte));            // 1 + 1e-16 = 1 (Ausloeschung)
            Assert.Equal(2e-16, Numerik.SummeSequenziell(werte, 1, 2));    // Teilsumme ohne Ausloeschung
            Assert.Equal(0.0, Numerik.SummeSequenziell(new double[0]));
            Assert.Throws<ArgumentNullException>(() => Numerik.SummeSequenziell(null!));
        }
    }
}
