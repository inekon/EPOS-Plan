using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using SkiaSharp;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERWUNSCH W11b‑B‑26 (10.09.2026): „Der Lastgang und die Kappung durch den
    /// Stromspeicher sowie der Ladezustand des Stromspeichers sollen in einer Grafik
    /// sichtbar sein (Umschaltung wie bisher Lastgang und sortierte Dauerlinie)."
    /// Geprüft wird <see cref="SpeicherBetriebsbild"/> — der Bauteil, aus dem der
    /// Stromspeicher-Reiter der Ergebnisseite sein EINES Bild bekommt.
    ///
    /// <para><b>Der Prüfstand</b> ist ein SYNTHETISCHER Lauf, kein Rastersuchlauf: acht
    /// Tage im Viertelstundenraster, tagsüber 40 kW Last und 60 kW Erzeugung (der
    /// Speicher lädt), nachts 30 kW Last und keine Erzeugung (er entlädt), gerechnet
    /// von der <see cref="Dauernutzung"/> der Engine — also genau der Weg, den auch der
    /// Simulationslauf geht.</para>
    ///
    /// <para><b>Was geprüft wird.</b> Dass ein Bild ENTSTEHT, dass die Reihen aus dem
    /// Lauf kommen und zueinander passen (ohne − mit = Speicherleistung), dass der
    /// Ladezustand als Reihe der ZWEITEN Achse mitgeht, dass „sortiert" jede Reihe FÜR
    /// SICH absteigend ordnet — und dass die Reihenwahl der Hausregel folgt
    /// (<c>null</c> = alle, leer = keine).</para>
    /// </summary>
    public sealed class SpeicherBetriebsbildTests : IDisposable
    {
        private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;
        private readonly CultureInfo _vorherUi = CultureInfo.CurrentUICulture;

        public SpeicherBetriebsbildTests()
        {
            CultureInfo de = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = de;
            CultureInfo.CurrentUICulture = de;
            Thread.CurrentThread.CurrentCulture = de;
            Thread.CurrentThread.CurrentUICulture = de;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CultureInfo.CurrentCulture = _vorher;
            CultureInfo.CurrentUICulture = _vorherUi;
            Thread.CurrentThread.CurrentCulture = _vorher;
            Thread.CurrentThread.CurrentUICulture = _vorherUi;
        }

        // =================================================================
        // Prüfstand
        // =================================================================

        /// <summary>Acht Tage im Viertelstundenraster.</summary>
        private const int Werte = 8 * 24 * 4;

        /// <summary>Die Intervalllänge des Simulationslaufs [h].</summary>
        private const double Dt = 0.25;

        private const string Titel = "Lastgang und Speicherbetrieb";

        private static double[] LastKw()
        {
            double[] last = new double[Werte];
            for (int i = 0; i < Werte; i++)
            {
                int viertel = i % 96;
                last[i] = viertel >= 8 * 4 && viertel < 20 * 4 ? 40.0 : 30.0;
            }
            return last;
        }

        /// <summary>Erzeugung nur tagsüber — sonst hätte der Speicher nichts zu laden.</summary>
        private static double[] PvKw()
        {
            double[] pv = new double[Werte];
            for (int i = 0; i < Werte; i++)
            {
                int viertel = i % 96;
                pv[i] = viertel >= 10 * 4 && viertel < 16 * 4 ? 60.0 : 0.0;
            }
            return pv;
        }

        private static SpeicherEingang Eingang() => SpeicherEingang.MitFixpreis(LastKw(), PvKw(), 30.0);

        private static SpeicherParameter Parameter() => new SpeicherParameter
        {
            CNomKwh = 100.0,
            PKw = 50.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 100.0,
            RoundTripWirkungsgrad = 0.9,
            DtH = Dt,
            CCapEurProKwh = 300.0,
            CPowEurProKw = 100.0,
            IFixEur = 0.0,
            Kapitalzins = 0.0,
            NutzungsdauerA = 20.0,
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.0
        };

        private static SpeicherErgebnis Lauf()
            => new Dauernutzung(SpeicherModus.Energetisch).Berechne(Eingang(), Parameter());

        private static byte[] Bild(IReadOnlyList<string> wahl = null, bool sortiert = false)
            => SpeicherBetriebsbild.Zeichnen(Titel, Eingang(), Lauf(), Dt, wahl, sortiert, null);

        private static (int Breite, int Hoehe) Mass(byte[] png)
        {
            using (var bild = SKBitmap.Decode(png)) return (bild.Width, bild.Height);
        }

        private static double[] Absteigend(double[] werte)
        {
            double[] kopie = (double[])werte.Clone();
            Array.Sort(kopie);
            Array.Reverse(kopie);
            return kopie;
        }

        // =================================================================
        // Das Bild entsteht
        // =================================================================

        [Fact]
        public void Das_Bild_Entsteht_Aus_Einem_Lauf()
        {
            byte[] png = Bild();

            Assert.NotNull(png);
            Assert.NotEmpty(png);
            Assert.Equal((1240, 560), Mass(png));
        }

        [Fact]
        public void Ohne_Lauf_Gibt_Es_Kein_Bild()
        {
            Assert.Null(SpeicherBetriebsbild.Zeichnen(Titel, null, Lauf(), Dt, null, false, null));
            Assert.Null(SpeicherBetriebsbild.Zeichnen(Titel, Eingang(), null, Dt, null, false, null));
        }

        [Fact]
        public void Zweimal_Dasselbe_Bild_Ist_Bitgleich()
        {
            // Die Wache unter den Bitvergleichen dieser Datei: Ohne sie sagte ein
            // NotEqual nichts ueber die Sortierung, sondern ueber den Zeichner.
            Assert.Equal(Bild(), Bild());
        }

        // =================================================================
        // Die Reihen kommen aus dem Lauf
        // =================================================================

        [Fact]
        public void Ohne_Speicher_Minus_Mit_Speicher_Ist_Die_Speicherleistung()
        {
            SpeicherEingang eingang = Eingang();
            SpeicherErgebnis erg = Lauf();

            List<ChartRenderer.Reihe> reihen =
                SpeicherBetriebsbild.Leistungsreihen(eingang, erg, Dt, null);

            // Drei Leistungsreihen - die SCHWELLE gibt es ohne Lastspitzenkappung nicht.
            Assert.Equal(3, reihen.Count);
            Assert.All(reihen, r => Assert.Equal(Werte, r.Werte.Length));

            double[] ohne = reihen[0].Werte, mit = reihen[1].Werte, leistung = reihen[2].Werte;
            for (int i = 0; i < Werte; i++)
                Assert.Equal(leistung[i], ohne[i] - mit[i], 9);

            // Und die Residuallast ist Last minus Erzeugung - NICHT bei null gekappt.
            double[] last = LastKw(), pv = PvKw();
            for (int i = 0; i < Werte; i++)
                Assert.Equal(last[i] - pv[i], ohne[i], 9);
        }

        [Fact]
        public void Die_Speicherleistung_Traegt_Ihr_Vorzeichen()
        {
            SpeicherErgebnis erg = Lauf();
            double[] kw = SpeicherBetriebsbild.LeistungKw(erg, Dt);

            Assert.Equal(Werte, kw.Length);
            for (int i = 0; i < Werte; i++)
                Assert.Equal((erg.EntladungAcKwh[i] - erg.LadungAcKwh[i]) / Dt, kw[i], 9);

            // Der Pruefstand laedt tagsueber und entlaedt nachts: Beide Vorzeichen
            // kommen vor, und genau deshalb liegt die Kurve um die Nulllinie.
            Assert.Contains(kw, w => w > 0.0);
            Assert.Contains(kw, w => w < 0.0);
        }

        // =================================================================
        // Der Ladezustand auf der ZWEITEN Achse
        // =================================================================

        [Fact]
        public void Der_Ladezustand_Ist_Die_Reihe_Der_Zweiten_Achse()
        {
            SpeicherErgebnis erg = Lauf();
            ChartRenderer.Reihe soc = SpeicherBetriebsbild.Ladezustand(erg, null);

            Assert.NotNull(soc);
            Assert.Equal(erg.SoCKwh, soc.Werte);

            // Sein NAME nennt die Einheit - er steht in derselben Legende wie drei
            // Leistungen in kW.
            Assert.Contains("kWh", soc.Name, StringComparison.Ordinal);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PEAK_CHART_Y2, soc.Name);
        }

        [Fact]
        public void Ohne_Den_Ladezustand_Sieht_Das_Bild_Anders_Aus()
        {
            string[] ohneSoc =
            {
                SpeicherBetriebsbild.REIHE_OHNE,
                SpeicherBetriebsbild.REIHE_MIT,
                SpeicherBetriebsbild.REIHE_SPEICHER
            };

            Assert.Null(SpeicherBetriebsbild.Ladezustand(Lauf(), ohneSoc));
            Assert.NotEqual(Bild(), Bild(ohneSoc));
        }

        /// <summary>
        /// Der Ladezustand ALLEIN ist eine gültige Wahl: Dann bleibt die linke Achse leer
        /// (eine Skala ohne Reihe wäre eine Behauptung über nichts), und das Bild zeigt
        /// nur die rechte.
        /// </summary>
        [Fact]
        public void Der_Ladezustand_Allein_Ergibt_Ein_Bild()
        {
            byte[] png = Bild(new[] { SpeicherBetriebsbild.REIHE_SOC });

            Assert.NotEmpty(png);
            Assert.Equal((1240, 560), Mass(png));
            Assert.NotEqual(Bild(), png);
        }

        // =================================================================
        // Der Umschalter „sortiert"
        // =================================================================

        /// <summary>
        /// „Sortiert" ordnet JEDE Reihe FÜR SICH absteigend — die Dauerlinie des
        /// Bedarfsreiters, angewandt auf alle vier Reihen samt der zweiten Achse.
        ///
        /// <para>Gemessen wird am Bild selbst: Das sortierte Bild muss BITGLEICH mit dem
        /// unsortierten Bild ueber den von Hand je Reihe absteigend geordneten Werten
        /// sein. Ordnete der Zeichner die Reihen GEMEINSAM (etwa nach der ersten), käme
        /// etwas anderes heraus.</para>
        /// </summary>
        [Fact]
        public void Sortiert_Ordnet_Jede_Reihe_Fuer_Sich_Absteigend()
        {
            SpeicherEingang eingang = Eingang();
            SpeicherErgebnis erg = Lauf();

            List<ChartRenderer.Reihe> reihen =
                SpeicherBetriebsbild.Leistungsreihen(eingang, erg, Dt, null);
            foreach (ChartRenderer.Reihe r in reihen) r.Werte = Absteigend(r.Werte);

            ChartRenderer.Reihe soc = SpeicherBetriebsbild.Ladezustand(erg, null);
            soc.Werte = Absteigend(soc.Werte);

            byte[] vonHand = ChartRenderer.Speicherbetrieb(
                Titel, reihen, WindowsFormsApplication1.MyResource.Resource.PEAK_CHART_Y, soc,
                WindowsFormsApplication1.MyResource.Resource.PEAK_CHART_Y2, false, null);

            Assert.Equal(vonHand, Bild(null, true));
            Assert.NotEqual(Bild(null, false), Bild(null, true));
        }

        // =================================================================
        // Die Reihenwahl (Hausregel der Ergebnisseite)
        // =================================================================

        [Fact]
        public void Eine_Leere_Reihenwahl_Zeigt_Den_Leerhinweis()
        {
            // null heisst "alle", eine LEERE Liste heisst "keine" - kein Rueckfall
            // (Doku_Simulationsergebnis_Darstellung.md, 5).
            byte[] leer = Bild(new string[0]);

            Assert.Empty(SpeicherBetriebsbild.Leistungsreihen(Eingang(), Lauf(), Dt, new string[0]));
            Assert.Null(SpeicherBetriebsbild.Ladezustand(Lauf(), new string[0]));
            Assert.NotEmpty(leer);
            Assert.Equal((1240, 560), Mass(leer));
            Assert.NotEqual(Bild(), leer);
        }

        [Fact]
        public void Eine_Einzelne_Leistungsreihe_Bringt_Das_Bild_Nicht_Zu_Fall()
        {
            byte[] png = Bild(new[] { SpeicherBetriebsbild.REIHE_MIT });

            Assert.NotEmpty(png);
            Assert.Equal((1240, 560), Mass(png));
        }

        /// <summary>
        /// Die SCHWELLE der Lastspitzenkappung gibt es im Ergebnisreiter nicht (der Lauf
        /// fährt Dauernutzung, Nachtnutzung oder Preissteuerung) — aber das Bild kann
        /// sie, damit eine Berechnungsart mit Kappung keine Ausnahme von der Regel
        /// bräuchte.
        /// </summary>
        [Fact]
        public void Mit_Schwelle_Kommt_Eine_Vierte_Reihe_Dazu()
        {
            SpeicherEingang eingang = Eingang();
            SpeicherErgebnis erg = Lauf();

            List<ChartRenderer.Reihe> ohne =
                SpeicherBetriebsbild.Leistungsreihen(eingang, erg, Dt, null);
            List<ChartRenderer.Reihe> mit =
                SpeicherBetriebsbild.Leistungsreihen(eingang, erg, Dt, null, 25.0);

            Assert.Equal(3, ohne.Count);
            Assert.Equal(4, mit.Count);
            Assert.All(mit[2].Werte, w => Assert.Equal(25.0, w, 9));
            Assert.True(mit[2].Gestrichelt);
        }
    }
}
