using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Summen je Monat, Woche und Tag des Grafikreiters „Wärme-/Strombedarf“
    /// (<see cref="Zeitsummen"/>), das Wochenraster des CSV-Schreibers und das Säulenstapelbild
    /// (<see cref="ChartRenderer.SaeulenstapelModell"/>).
    /// </summary>
    public sealed class ZeitsummenTests
    {
        /// <summary>Eine Stundenreihe, die in Stunde i den Tag (i / 24) + 1 als Leistung trägt.</summary>
        private static double[] Tagesnummern(int jeTag)
            => Enumerable.Range(0, 365 * jeTag).Select(i => (double)(i / jeTag + 1)).ToArray();

        [Fact]
        public void Die_Faecher_sind_12_52_und_365()
        {
            Assert.Equal(12, Zeitsummen.Faecher(Zeitraster.Monat));
            Assert.Equal(52, Zeitsummen.Faecher(Zeitraster.Woche));
            Assert.Equal(365, Zeitsummen.Faecher(Zeitraster.Tag));
            Assert.Equal(0, Zeitsummen.Faecher(Zeitraster.Stunde));
        }

        /// <summary>
        /// <b>Die Wochenregel:</b> 52 × 7 = 364 Tage, der 365. Tag gehört zur 52. Woche — sie zählt
        /// acht Tage, jede andere sieben.
        /// </summary>
        [Fact]
        public void Der_365_Tag_gehoert_zur_52_Woche()
        {
            Assert.Equal(0, Zeitsummen.WocheDesTages(0));
            Assert.Equal(0, Zeitsummen.WocheDesTages(6));
            Assert.Equal(1, Zeitsummen.WocheDesTages(7));
            Assert.Equal(51, Zeitsummen.WocheDesTages(357));
            Assert.Equal(51, Zeitsummen.WocheDesTages(363));
            Assert.Equal(51, Zeitsummen.WocheDesTages(364));

            double[] eins = Enumerable.Repeat(1000.0, 8760).ToArray();        // 1 000 kW = 24 MWh je Tag
            double[] wochen = Zeitsummen.SummenMwh(eins, Zeitraster.Woche);
            Assert.Equal(52, wochen.Length);
            for (int w = 0; w < 51; w++) Assert.Equal(7 * 24.0, wochen[w], 9);
            Assert.Equal(8 * 24.0, wochen[51], 9);
            Assert.Equal(8760.0, wochen.Sum(), 6);
        }

        [Fact]
        public void Die_Monate_folgen_dem_Gemeinjahr()
        {
            Assert.Equal(0, Zeitsummen.MonatDesTages(30));
            Assert.Equal(1, Zeitsummen.MonatDesTages(31));
            Assert.Equal(1, Zeitsummen.MonatDesTages(58));
            Assert.Equal(2, Zeitsummen.MonatDesTages(59));
            Assert.Equal(11, Zeitsummen.MonatDesTages(364));

            double[] eins = Enumerable.Repeat(1000.0, 8760).ToArray();
            double[] monate = Zeitsummen.SummenMwh(eins, Zeitraster.Monat);
            Assert.Equal(12, monate.Length);
            for (int m = 0; m < 12; m++) Assert.Equal(Zeitsummen.MONATSTAGE[m] * 24.0, monate[m], 9);
        }

        /// <summary>
        /// Tagessummen in MWh; eine Viertelstundenreihe [kW] trägt je Wert eine Viertelstunde und
        /// ergibt dieselben Summen wie die gleich hohe Stundenreihe.
        /// </summary>
        [Fact]
        public void Tagessummen_aus_Stunden_und_Viertelstunden_stimmen_ueberein()
        {
            double[] stunden = Zeitsummen.SummenMwh(Tagesnummern(24), Zeitraster.Tag);
            double[] viertel = Zeitsummen.SummenMwh(Tagesnummern(96), Zeitraster.Tag);
            Assert.Equal(365, stunden.Length);
            for (int t = 0; t < 365; t++)
            {
                Assert.Equal((t + 1) * 24 * 0.001, stunden[t], 9);
                Assert.Equal(stunden[t], viertel[t], 9);
            }
        }

        [Fact]
        public void Fremde_Laengen_und_Raster_ohne_Summen_geben_null()
        {
            Assert.Null(Zeitsummen.SummenMwh(new double[8000], Zeitraster.Monat));
            Assert.Null(Zeitsummen.SummenMwh(null, Zeitraster.Monat));
            Assert.Null(Zeitsummen.SummenMwh(new double[8760], Zeitraster.Stunde));
        }

        [Fact]
        public void Nicht_endliche_Werte_zaehlen_als_null()
        {
            double[] r = Enumerable.Repeat(1000.0, 8760).ToArray();
            r[0] = double.NaN;
            r[1] = double.PositiveInfinity;
            Assert.Equal(22.0, Zeitsummen.SummenMwh(r, Zeitraster.Tag)[0], 9);
        }

        [Fact]
        public void Das_Wochenraster_benennt_die_erste_CSV_Spalte()
        {
            Assert.Equal(Zeitraster.Woche, ZeitreihenCsv.RasterAus(52));
            Assert.Equal("Woche", ZeitreihenCsv.Rasterkopf(Zeitraster.Woche));
            string text = ZeitreihenCsv.Text(Zeitraster.Woche,
                new[] { new ZeitreihenSpalte("Gebäude", "MWh", new double[52]) });
            string[] zeilen = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal("Woche;Gebäude [MWh]", zeilen[0]);
            Assert.Equal(53, zeilen.Length);
        }

        // ---------------------------------------------------------------------
        // Das Säulenstapelbild
        // ---------------------------------------------------------------------

        private static List<ChartRenderer.Reihe> Reihen(int n) => new List<ChartRenderer.Reihe>
        {
            new ChartRenderer.Reihe("Prozesse", Enumerable.Repeat(2.0, n).ToArray(), Farbrolle.PROZESSWAERME),
            new ChartRenderer.Reihe("Gebäude", Enumerable.Range(0, n).Select(i => (double)i).ToArray(), Farbrolle.HEIZWAERME)
        };

        private static IEnumerable<Zeichenbefehl> Alle(IReadOnlyList<Zeichenbefehl> befehle)
        {
            if (befehle == null) yield break;
            foreach (Zeichenbefehl b in befehle)
            {
                yield return b;
                if (b is Gruppe g)
                    foreach (Zeichenbefehl k in Alle(g.Befehle)) yield return k;
            }
        }

        /// <summary>
        /// Je Fach und Reihe mit Wert über null eine Säulenschicht mit Marke und Zeigewert; ein reines
        /// Pixelbild ohne Zeichenfläche und Datenreihe; Maß wie der Jahresgang.
        /// </summary>
        [Theory]
        [InlineData(12)]
        [InlineData(52)]
        [InlineData(365)]
        public void Der_Saeulenstapel_traegt_je_Fach_und_Reihe_eine_Schicht(int n)
        {
            string[] namen = Enumerable.Range(1, n).Select(i => "Fach " + i).ToArray();
            string[] achse = Enumerable.Range(1, n).Select(i => i % 4 == 1 ? i.ToString() : "").ToArray();
            Zeichenmodell m = ChartRenderer.SaeulenstapelModell("Wärmebedarf", "MWh", Reihen(n), namen, achse);

            Assert.Null(m.Flaeche);
            Assert.Empty(m.Reihen);
            Assert.Equal(ChartRenderer.SAEULENSTAPEL_BREITE, m.Breite);
            Assert.Equal(ChartRenderer.SAEULENSTAPEL_HOEHE, m.Hoehe);

            List<Zeichenbefehl> prozesse = Alle(m.Befehle).Where(b => b.Marke == "reihe:Prozesse").ToList();
            List<Zeichenbefehl> gebaeude = Alle(m.Befehle).Where(b => b.Marke == "reihe:Gebäude").ToList();
            Assert.Equal(n, prozesse.Count);
            Assert.Equal(n - 1, gebaeude.Count);                  // Fach 1 trägt 0 — keine Schicht
            Assert.All(prozesse, b => Assert.StartsWith("Fach ", b.Wert));
            Assert.Contains(gebaeude, b => b.Wert.StartsWith("Fach " + n + " · Gebäude", StringComparison.Ordinal));
            Assert.True(m.Gleicht(ChartRenderer.SaeulenstapelModell("Wärmebedarf", "MWh", Reihen(n), namen, achse)));
        }

        [Fact]
        public void Ohne_Reihen_steht_der_Leerhinweis()
        {
            Zeichenmodell m = ChartRenderer.SaeulenstapelModell("Leer", "MWh",
                new List<ChartRenderer.Reihe>(), new[] { "Jan" }, new[] { "Jan" });
            Assert.Contains(Alle(m.Befehle), b => b.Marke == "leerhinweis");
        }
    }
}
