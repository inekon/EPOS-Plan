using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Freie Kühlung über die Wärmequelle — die Rechenprobe</b> (KU3-6b, F2/F3) mit synthetischen Stunden und
    /// ohne Datenbank: Frei gekühlt wird nur, solange Quellentemperatur plus Grädigkeit den Kaltwasser-Vorlauf nicht
    /// übersteigt; die Grädigkeit ohne Eingabe ist der Festwert 3 K; die Leistungsgrenze deckelt (ohne sie die
    /// Kälteleistung der Kennlinie); der Rest der Stunde läuft über den Verdichter nach Kennlinie; der Strom der
    /// freien Deckung ist Kälte / EER 15 mal (1 + Hilfsstromanteil); die Zähler stimmen; ohne Schalter rechnet die
    /// Stunde bitgleich; eine Quelle, die die freie Kühlung nicht trägt, wird erkannt.
    ///
    /// <para><b>Phantasie-Kennlinie:</b> runde, erfundene Werte, kein Produkt.</para>
    /// </summary>
    public sealed class FreieKuehlungSoleRechenwegTests
    {
        private const int VORLAUF = 18;
        private const double EER = 5.0;
        private const double PKUEHL = 10.0;

        /// <summary>Eine flache Kennlinie: Vorlauf 18 °C, Pkuehl 10 kW und EER 5 über 0 bis 40 °C.</summary>
        private static Kuehlkennlinie Kennlinie()
        {
            var z = new List<KuehlkennlinienZeile>();
            int id = 1;
            foreach (int t in new[] { 0, 10, 20, 30, 40 })
                z.Add(new KuehlkennlinienZeile(id++, VORLAUF, t, EER, PKUEHL, 100));
            return Kuehlkennlinie.Bilden(z, VORLAUF);
        }

        private static double[] Konstant(double wert)
        {
            var r = new double[Kaeltekaskade.STUNDEN];
            for (int h = 0; h < r.Length; h++) r[h] = wert;
            return r;
        }

        private static bool[] AlleKuehltage()
        {
            var k = new bool[Kaeltekaskade.TAGE];
            for (int d = 0; d < k.Length; d++) k[d] = true;
            return k;
        }

        /// <summary>Die Quelle: Stunde 0 bei 10 °C, Stunde 1 bei 15 °C, Stunde 2 bei 16 °C, sonst 30 °C.</summary>
        private static double[] Quelle()
        {
            double[] q = Konstant(30.0);
            q[0] = 10.0;
            q[1] = 15.0;
            q[2] = 16.0;
            return q;
        }

        private static Kaelteerzeuger Erzeuger(bool frei, double hilfs = 0.0, double? grenze = null,
                                               double graedigkeit = KaelteFestwerte.FREIE_KUEHLUNG_SOLE_GRAEDIGKEIT_K,
                                               double[] zeitanteil = null)
        {
            return new Kaelteerzeuger
            {
                AnlagenID = 1,
                IdWp = 1,
                Bezeichner = "Sole",
                Modulindex = 0,
                Kennlinie = Kennlinie(),
                Hilfsstromanteil = hilfs,
                Quelltemperatur = Quelle(),
                Zeitanteil = zeitanteil ?? Kaeltekaskade.ZeitanteilBilden(AlleKuehltage(), null, false, 0, 0),
                FreieKuehlungSole = frei,
                FreieKuehlungGraedigkeitK = graedigkeit,
                FreieKuehlungLeistungKw = grenze,
                KuehlVorlaufC = VORLAUF,
            };
        }

        private static Kaeltekaskade Rechnen(Kaelteerzeuger e, double[] bedarf)
        {
            var k = new Kaeltekaskade { Kuehltage = AlleKuehltage() };
            k.Erzeuger.Add(e);
            k.Rechnen(bedarf, false);
            return k;
        }

        [Fact]
        public void Der_Festwert_der_Graedigkeit_ist_3_K()
        {
            Assert.Equal(3.0, KaelteFestwerte.FREIE_KUEHLUNG_SOLE_GRAEDIGKEIT_K);
            Assert.Equal(KaelteFestwerte.FREIE_KUEHLUNG_SOLE_GRAEDIGKEIT_K, new Kaelteerzeuger().FreieKuehlungGraedigkeitK);
        }

        /// <summary>
        /// Frei nur unter der Schwelle: 10 + 3 und 15 + 3 ≤ 18 kühlen frei, 16 + 3 &gt; 18 nicht; Last 6 kWh unter der
        /// Kennlinienleistung wird in den freien Stunden ganz frei gedeckt, mit Strom 6 / 15 · (1 + Hilfsstrom).
        /// </summary>
        [Fact]
        public void Frei_nur_unter_der_Schwelle_mit_Strom_ueber_EER_15_und_Hilfsstrom()
        {
            const double HILFS = 0.1;
            Kaelteerzeuger e = Erzeuger(true, HILFS);
            Kaeltekaskade k = Rechnen(e, Konstant(6.0));

            double freiVerdichter = 6.0 / KaelteFestwerte.FREIE_KUEHLUNG_EER;
            double freiStrom = freiVerdichter * (1.0 + HILFS);
            Assert.Equal(6.0, e.Kaelte_stuendlich[0], 12);
            Assert.Equal(freiStrom, e.Strom_stuendlich[0], 12);
            Assert.Equal(freiStrom, e.Strom_stuendlich[1], 12);
            // Stunde 2 und alle warmen: Verdichter nach Kennlinie.
            Assert.Equal(6.0 / EER * (1.0 + HILFS), e.Strom_stuendlich[2], 12);
            Assert.Equal(6.0 / EER * (1.0 + HILFS), e.Strom_stuendlich[100], 12);

            Assert.Equal(2, e.StundenFreieKuehlung);
            Assert.Equal(12.0, e.KaelteFreiKwh, 9);
            Assert.Equal(2, k.StundenFreieKuehlungWp);
            Assert.Equal(6.0, k.FreieKuehlungWp_stuendlich[1], 12);
            Assert.Equal(0.0, k.FreieKuehlungWp_stuendlich[2]);

            // Summenreihen tragen beide Anteile; der Hilfsstrom ist der Zuschlag auf beide.
            Assert.Equal(6.0, k.Deckung_stuendlich[0], 12);
            Assert.Equal(freiStrom, k.Stromverbrauch_Kuehlung_stuendlich[0], 12);
            double hilfsJahr = 2 * freiVerdichter * HILFS + (Kaeltekaskade.STUNDEN - 2) * 6.0 / EER * HILFS;
            Assert.Equal(hilfsJahr, e.HilfsstromGesamtKwh, 6);
            Assert.Equal(0.0, k.RestGesamtKwh, 9);
        }

        /// <summary>Eine gepflegte Grädigkeit verschiebt die Schwelle: mit 2 K kühlt auch die Stunde bei 16 °C frei.</summary>
        [Fact]
        public void Eine_gepflegte_Graedigkeit_verschiebt_die_Schwelle()
        {
            Kaelteerzeuger e = Erzeuger(true, graedigkeit: 2.0);
            Rechnen(e, Konstant(6.0));
            Assert.Equal(3, e.StundenFreieKuehlung);

            Kaelteerzeuger streng = Erzeuger(true, graedigkeit: 9.0);
            Rechnen(streng, Konstant(6.0));
            Assert.Equal(0, streng.StundenFreieKuehlung);
        }

        /// <summary>
        /// Die Leistungsgrenze deckelt: Grenze 4 kW, Last 15 kWh — frei 4, Verdichter 10 (Kennlinienleistung), Rest 1;
        /// Strom = 4 / 15 + 10 / 5. Ohne Grenze deckelt die Kennlinienleistung: frei 10, Verdichter 5.
        /// </summary>
        [Fact]
        public void Die_Leistungsgrenze_deckelt_und_der_Rest_laeuft_ueber_den_Verdichter()
        {
            Kaelteerzeuger e = Erzeuger(true, grenze: 4.0);
            Kaeltekaskade k = Rechnen(e, Konstant(15.0));
            Assert.Equal(14.0, e.Kaelte_stuendlich[0], 12);
            Assert.Equal(4.0 / KaelteFestwerte.FREIE_KUEHLUNG_EER + 10.0 / EER, e.Strom_stuendlich[0], 12);
            Assert.Equal(1.0, k.Rest_stuendlich[0], 12);
            Assert.Equal(4.0, k.FreieKuehlungWp_stuendlich[0], 12);

            Kaelteerzeuger ohne = Erzeuger(true);
            Kaeltekaskade k2 = Rechnen(ohne, Konstant(15.0));
            Assert.Equal(15.0, ohne.Kaelte_stuendlich[0], 12);
            Assert.Equal(10.0 / KaelteFestwerte.FREIE_KUEHLUNG_EER + 5.0 / EER, ohne.Strom_stuendlich[0], 12);
            Assert.Equal(0.0, k2.Rest_stuendlich[0], 12);
            Assert.Equal(20.0, ohne.KaelteFreiKwh, 12);
        }

        /// <summary>Der Zeitanteil begrenzt auch die freie Deckung: halbe Stunde, Grenze 4 kW → frei 2, Verdichter 5.</summary>
        [Fact]
        public void Der_Zeitanteil_begrenzt_die_freie_Deckung()
        {
            Kaelteerzeuger e = Erzeuger(true, grenze: 4.0, zeitanteil: Konstant(0.5));
            Rechnen(e, Konstant(15.0));
            Assert.Equal(7.0, e.Kaelte_stuendlich[0], 12);
            Assert.Equal(4.0, e.KaelteFreiKwh, 12);   // Stunden 0 und 1 je 2 kWh
        }

        /// <summary>Ohne Schalter rechnet jede Stunde bitgleich wie ein Erzeuger ohne die Felder der freien Kühlung.</summary>
        [Fact]
        public void Ohne_Schalter_bitgleich()
        {
            var bedarf = Konstant(0.0);
            var rnd = new Random(17);
            for (int h = 0; h < bedarf.Length; h++) bedarf[h] = rnd.NextDouble() * 14.0;

            Kaelteerzeuger aus = Erzeuger(false, 0.07, grenze: 3.0, graedigkeit: 0.0);
            aus.Mindestanteil = 0.3;
            Kaeltekaskade ka = Rechnen(aus, bedarf);

            var alt = new Kaelteerzeuger
            {
                AnlagenID = 1, IdWp = 1, Bezeichner = "Sole", Modulindex = 0, Kennlinie = Kennlinie(),
                Hilfsstromanteil = 0.07, Quelltemperatur = Quelle(), Mindestanteil = 0.3,
                Zeitanteil = Kaeltekaskade.ZeitanteilBilden(AlleKuehltage(), null, false, 0, 0),
            };
            Kaeltekaskade kb = Rechnen(alt, bedarf);

            for (int h = 0; h < Kaeltekaskade.STUNDEN; h++)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(alt.Kaelte_stuendlich[h]), BitConverter.DoubleToInt64Bits(aus.Kaelte_stuendlich[h]));
                Assert.Equal(BitConverter.DoubleToInt64Bits(alt.Strom_stuendlich[h]), BitConverter.DoubleToInt64Bits(aus.Strom_stuendlich[h]));
                Assert.Equal(BitConverter.DoubleToInt64Bits(kb.Stromverbrauch_Kuehlung_stuendlich[h]),
                             BitConverter.DoubleToInt64Bits(ka.Stromverbrauch_Kuehlung_stuendlich[h]));
            }
            Assert.Equal(BitConverter.DoubleToInt64Bits(kb.HilfsstromGesamtKwh), BitConverter.DoubleToInt64Bits(ka.HilfsstromGesamtKwh));
            Assert.Equal(alt.Starts, aus.Starts);
            Assert.Equal(0, aus.StundenFreieKuehlung);
            Assert.Equal(0.0, aus.KaelteFreiKwh);
            Assert.Equal(0, ka.StundenFreieKuehlungWp);
        }

        /// <summary>
        /// F2: Nur Sole-Wasser und Wasser-Wasser mit der Quelle Erdreich, Konstant, Profil oder CSV tragen die freie
        /// Kühlung; Luft-Wasser, die leere Quelle, Außenluft und der Pufferspeicher nicht — dann bleibt der Schalter
        /// im Lauf ohne Wirkung und wird benannt (die Warnung prüft die Datenbankprobe).
        /// </summary>
        [Theory]
        [InlineData(DbWerte.WP_BAUART_SOLE_WASSER, DbWerte.WQ_TYP_ERDREICH, true)]
        [InlineData(DbWerte.WP_BAUART_SOLE_WASSER, DbWerte.WQ_TYP_KONSTANT, true)]
        [InlineData(DbWerte.WP_BAUART_WASSER_WASSER, DbWerte.WQ_TYP_PROFIL, true)]
        [InlineData(DbWerte.WP_BAUART_WASSER_WASSER, DbWerte.WQ_TYP_CSV, true)]
        [InlineData(DbWerte.WP_BAUART_LUFT_WASSER, DbWerte.WQ_TYP_ERDREICH, false)]
        [InlineData(DbWerte.WP_BAUART_SOLE_WASSER, DbWerte.WQ_TYP_OHNE, false)]
        [InlineData(DbWerte.WP_BAUART_SOLE_WASSER, null, false)]
        [InlineData(DbWerte.WP_BAUART_SOLE_WASSER, DbWerte.WQ_TYP_AUSSENLUFT, false)]
        [InlineData(DbWerte.WP_BAUART_SOLE_WASSER, DbWerte.WQ_TYP_PUFFERSPEICHER, false)]
        [InlineData("", DbWerte.WQ_TYP_ERDREICH, false)]
        public void Nur_eine_tragende_Quelle_laesst_die_freie_Kuehlung_zu(string bauart, string quelle, bool erwartet)
        {
            Assert.Equal(erwartet, SimulationControl.FreieKuehlungSoleMoeglich(bauart, quelle));
        }
    }
}
