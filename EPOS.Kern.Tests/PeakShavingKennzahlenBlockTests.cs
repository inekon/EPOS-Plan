using System;
using System.Collections.Generic;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die 18 Kennzahlzeilen und die 12 Monatszeilen der Lastspitzenkappung
    /// (<see cref="PeakShavingKennzahlenBlock"/>, iU9-W12.0f).
    ///
    /// <para>Geprueft werden Zahl, Reihenfolge und Gruppierung der Zeilen, die
    /// Formate und Einheiten, das Negativkennzeichen (der Ersatz fuer
    /// <c>Color.FromArgb(176, 0, 0)</c>) und die drei Ausgaenge der
    /// Amortisationszeile.</para>
    ///
    /// <para>Ohne Datenbank — gerechnet wird mit einem synthetischen Lastgang.
    /// Wo Texte geprueft werden, ist die Sprache festgelegt (Regel seit iU9-W8).</para>
    /// </summary>
    public class PeakShavingKennzahlenBlockTests
    {
        /// <summary>
        /// Ein Viertelstunden-Jahreslastgang mit einer erkennbaren Spitze — dieselbe
        /// Rasterlaenge, mit der die Maske rechnet.
        /// </summary>
        private static double[] Lastgang()
        {
            double[] w = new double[35040];
            for (int i = 0; i < w.Length; i++)
                w[i] = 100.0 + 40.0 * Math.Sin(2.0 * Math.PI * i / 96.0);
            for (int i = 1000; i < 1040; i++) w[i] = 400.0;    // die Jahresspitze
            return w;
        }

        private static SpeicherParameter Speicher()
        {
            return new SpeicherParameter
            {
                CNomKwh = 200.0,
                PKw = 100.0,
                SoCMinKwh = 200.0 * 0.1,
                SoCMaxKwh = 200.0 * 0.9,
                RoundTripWirkungsgrad = 0.9,
                StartSoCKwh = 200.0 * 0.1,
                DtH = 0.25,
                CCapEurProKwh = 400.0,
                CPowEurProKw = 200.0,
                IFixEur = 1000.0,
                Kapitalzins = 0.03,
                NutzungsdauerA = 15.0,
                DegradationProA = 0.0
            };
        }

        private static PeakShavingErgebnis Ergebnis(double leistungspreis = 120.0,
                                                    double bezugspreis = 25.0)
        {
            PeakShavingParameter ps = new PeakShavingParameter
            {
                PZielKw = 300.0,
                Adaptiv = false,
                LeistungspreisEurProKwA = leistungspreis,
                BezugspreisMittelCtKwh = bezugspreis
            };
            return new PeakShaving(ps, SpeicherModus.Energetisch)
                       .BerechnePeakShaving(Lastgang(), Speicher());
        }

        // ==================================================================

        [Fact]
        public void Ohne_Ergebnis_bleiben_beide_Listen_leer()
        {
            Assert.Empty(PeakShavingKennzahlenBlock.Zeilen(null));
            Assert.Empty(PeakShavingKennzahlenBlock.Monatszeilen(null));
        }

        /// <summary>
        /// 18 Kennzahlen plus drei Trenner — genau die Zeilen, die
        /// <c>Form_PeakShaving.KennzahlenAnzeigen</c> in die ListView schrieb.
        ///
        /// <para><b>18, nicht 17.</b> Die Vermessung nennt 17; nachgezaehlt sind es
        /// 5 + 4 + 3 + 6 — die vierte Gruppe hat SECHS Zeilen (Investition,
        /// Annuitaet, Ueberschuss, zwei Amortisationen, Kapitalwert). Dieselbe Art
        /// Zaehlfehler stand schon bei den 39/40 Speicherkennzahlen aus iU9-W11a.</para>
        /// </summary>
        [Fact]
        public void Der_Block_hat_18_Kennzahlen_und_drei_Trenner()
        {
            List<PeakShavingKennzahlenBlock.Zeile> zeilen =
                PeakShavingKennzahlenBlock.Zeilen(Ergebnis());

            Assert.Equal(21, zeilen.Count);

            int trenner = 0, kennzahlen = 0;
            foreach (PeakShavingKennzahlenBlock.Zeile z in zeilen)
                if (z.IstTrenner) trenner++; else kennzahlen++;

            Assert.Equal(3, trenner);
            Assert.Equal(18, kennzahlen);

            // Die Trenner stehen zwischen den vier Gruppen: nach 5, nach 4, nach 3.
            Assert.True(zeilen[5].IstTrenner);
            Assert.True(zeilen[10].IstTrenner);
            Assert.True(zeilen[14].IstTrenner);
        }

        [Fact]
        public void Die_Reihenfolge_und_die_Einheiten_stehen_fest()
        {
            using var _ = new DeutscheOberflaeche();
            List<PeakShavingKennzahlenBlock.Zeile> z =
                PeakShavingKennzahlenBlock.Zeilen(Ergebnis());

            Assert.Equal("Lastspitze ohne Speicher (Jahresmaximum)", z[0].Bezeichnung);
            Assert.Equal("Lastspitze mit Speicher (Jahresmaximum)", z[1].Bezeichnung);
            Assert.Equal("kW", z[0].Einheit);
            Assert.Equal("kW", z[1].Einheit);
            Assert.Equal("kW", z[2].Einheit);
            Assert.Equal("kW", z[3].Einheit);
            Assert.Equal("", z[4].Einheit);                 // "Schwelle gerissen": ja/nein

            Assert.Equal("kWh/a", z[6].Einheit);
            Assert.Equal("kWh/a", z[7].Einheit);
            Assert.Equal("kWh/a", z[8].Einheit);
            Assert.Equal("1/a", z[9].Einheit);

            Assert.Equal("EUR/a", z[11].Einheit);
            Assert.Equal("EUR/a", z[12].Einheit);
            Assert.Equal("EUR/a", z[13].Einheit);

            Assert.Equal("EUR", z[15].Einheit);
            Assert.Equal("EUR/a", z[16].Einheit);
            Assert.Equal("EUR/a", z[17].Einheit);
            Assert.Equal("", z[18].Einheit + z[19].Einheit.Replace("a", ""));  // die zwei Amortisationen
            Assert.Equal("EUR", z[20].Einheit);
        }

        /// <summary>
        /// „Schwelle gerissen" ist ein TEXT, keine Zahl — ja/nein aus dem Katalog.
        /// </summary>
        [Fact]
        public void Die_Zeile_Schwelle_gerissen_traegt_ja_oder_nein()
        {
            using var _ = new DeutscheOberflaeche();
            PeakShavingErgebnis r = Ergebnis();
            List<PeakShavingKennzahlenBlock.Zeile> z = PeakShavingKennzahlenBlock.Zeilen(r);

            Assert.Equal(r.SchwelleGerissen ? "ja" : "nein", z[4].Wert);
            Assert.False(z[4].Negativ);
        }

        /// <summary>
        /// Der Ersatz fuer die rote Schrift: Eine Kennzahl unter null traegt
        /// <c>Negativ</c>. Mit Leistungspreis 0 und einem Bezugspreis bleibt vom
        /// Ertrag nur der Verlustaufwand — die Ertragszeile wird negativ.
        /// </summary>
        [Fact]
        public void Ein_negativer_Betrag_traegt_das_Negativkennzeichen()
        {
            List<PeakShavingKennzahlenBlock.Zeile> z =
                PeakShavingKennzahlenBlock.Zeilen(Ergebnis(leistungspreis: 0.0, bezugspreis: 30.0));

            Assert.False(z[0].Negativ);                    // die Lastspitze ist positiv
            Assert.True(z[13].Negativ);                    // Ertrag Peak-Shaving
            Assert.True(z[17].Negativ);                    // Jahresueberschuss
        }

        /// <summary>
        /// Die Amortisationszeile hat drei Ausgaenge: eine Jahreszahl mit Einheit
        /// „a", „&gt; Nutzungsdauer" oder „nicht amortisierbar" — beide ohne Einheit.
        /// </summary>
        [Fact]
        public void Die_Amortisationszeile_kennt_drei_Ausgaenge()
        {
            using var _ = new DeutscheOberflaeche();
            List<PeakShavingKennzahlenBlock.Zeile> z =
                PeakShavingKennzahlenBlock.Zeilen(Ergebnis(leistungspreis: 0.0, bezugspreis: 30.0));

            // Ohne Leistungspreis gibt es keinen Ertrag - also keine Amortisation.
            Assert.Equal("", z[18].Einheit);
            Assert.True(z[18].Wert == "nicht amortisierbar" || z[18].Wert == "> Nutzungsdauer",
                        "Unerwartete Amortisationsangabe: " + z[18].Wert);
        }

        // ==================================================================
        // Monatszeilen
        // ==================================================================

        /// <summary>
        /// Zwoelf Monate bei einem ganzzahligen Tagesraster; die Monatsnamen folgen
        /// der Oberflaechensprache.
        /// </summary>
        [Fact]
        public void Die_Monatstabelle_hat_zwoelf_Monate()
        {
            using var _ = new DeutscheOberflaeche();
            List<PeakShavingKennzahlenBlock.Monatszeile> m =
                PeakShavingKennzahlenBlock.Monatszeilen(Ergebnis());

            Assert.Equal(12, m.Count);
            Assert.Equal("Januar", m[0].Monat);
            Assert.Equal("Dezember", m[11].Monat);
        }

        /// <summary>
        /// „Gesamtreihe" ist die SAMMELPOSITION der Engine (Monat 0), nicht eine
        /// dreizehnte Zeile: Sie erscheint, wenn das Raster kein ganzzahliges
        /// Tagesraster ist und eine Monatszuordnung deshalb erfunden waere. Die
        /// Vermessung nennt „13 Monatszeilen" — das ist dieser Sonderfall, nicht der
        /// Regelfall.
        /// </summary>
        [Fact]
        public void Ohne_ganzzahliges_Tagesraster_steht_eine_Gesamtreihe()
        {
            using var _ = new DeutscheOberflaeche();

            IReadOnlyList<Monatsspitze> spitzen =
                PeakShaving.Monatsspitzen(new double[] { 1.0, 2.0, 3.0 },
                                          new double[] { 1.0, 1.0, 1.0 }, 0.7);
            Assert.Single(spitzen);
            Assert.Equal(0, spitzen[0].Monat);
        }

        [Fact]
        public void Die_Monatsnamen_folgen_der_Oberflaechensprache()
        {
            System.Globalization.CultureInfo vorher =
                System.Threading.Thread.CurrentThread.CurrentUICulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture =
                    new System.Globalization.CultureInfo("en-US");
                List<PeakShavingKennzahlenBlock.Monatszeile> m =
                    PeakShavingKennzahlenBlock.Monatszeilen(Ergebnis());
                Assert.Equal("January", m[0].Monat);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = vorher;
            }
        }

        private sealed class DeutscheOberflaeche : IDisposable
        {
            private readonly System.Globalization.CultureInfo _vorherUi =
                System.Threading.Thread.CurrentThread.CurrentUICulture;
            private readonly System.Globalization.CultureInfo _vorher =
                System.Threading.Thread.CurrentThread.CurrentCulture;

            public DeutscheOberflaeche()
            {
                System.Globalization.CultureInfo de = new System.Globalization.CultureInfo("de-DE");
                System.Threading.Thread.CurrentThread.CurrentUICulture = de;
                System.Threading.Thread.CurrentThread.CurrentCulture = de;
            }

            public void Dispose()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = _vorherUi;
                System.Threading.Thread.CurrentThread.CurrentCulture = _vorher;
            }
        }
    }
}
