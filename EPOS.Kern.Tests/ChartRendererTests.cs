using System.Collections.Generic;
using SkiaSharp;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Diagramm-Renderer des Berichts liegt seit iU7-5 im Kern (Paket iU7-8).
    ///
    /// <para>Geprueft wird hier, was OHNE Datenbank und ohne Oberflaeche entscheidbar
    /// ist und in einer Sekunde durchlaeuft: die beiden Verdichtungen, die aus
    /// Stundenreihen Diagrammreihen machen, und dass das Zeichnen selbst ein Bild in
    /// der festgelegten Groesse liefert — auf Linux und macOS genauso wie auf
    /// Windows. Die vollstaendige Bildpruefung (neun Bilder, Farbvorkommen, Masse)
    /// macht die Probe <c>Proben\ChartProben</c>, den Pixelvergleich gegen den
    /// eingefrorenen GDI+-Stand der Modus <c>bildvergleich</c> der
    /// Referenzlauf-Suite unter Windows. Diese drei Tests sind die schnelle
    /// Sicherung dazwischen — sie laufen in JEDEM Kern-Lauf mit.</para>
    /// </summary>
    public class ChartRendererTests
    {
        // =====================================================================
        // 1 — die Verdichtungen: feste Reihen, exakte Erwartung
        // =====================================================================

        /// <summary>
        /// <c>TagesMittel</c> mittelt je 24 Stunden, <c>MonatsSummenMWh</c> summiert je
        /// Kalendermonat (Gemeinjahr, kein 29. Februar) und rechnet kWh in MWh.
        ///
        /// <para>Die Erwartungen stehen OHNE Toleranz: Alle Summanden sind ganzzahlig
        /// und in <c>double</c> verlustfrei, die abschliessende Division ist korrekt
        /// gerundet und trifft damit denselben Wert wie das Literal daneben. Weicht
        /// eine Stelle ab, ist das eine echte Aenderung der Rechnung und keine
        /// Gleitkomma-Unschaerfe.</para>
        /// </summary>
        [Fact]
        public void Verdichtungen_rechnen_wie_festgelegt()
        {
            // --- TagesMittel: zwei volle Tage, Stundenwert = Stundenindex ----------
            var zweiTage = new double[48];
            for (int i = 0; i < zweiTage.Length; i++) zweiTage[i] = i;

            double[] mittel = ChartRenderer.TagesMittel(zweiTage);
            Assert.Equal(2, mittel.Length);
            Assert.Equal(11.5, mittel[0]);          // (0 + … + 23) / 24 = 276 / 24
            Assert.Equal(35.5, mittel[1]);          // (24 + … + 47) / 24 = 852 / 24

            // Ein angebrochener Tag zaehlt nicht mit — 50 Stunden sind zwei Tage.
            Assert.Equal(2, ChartRenderer.TagesMittel(new double[50]).Length);
            Assert.Null(ChartRenderer.TagesMittel(null));

            // --- MonatsSummenMWh: volles Jahr mit 1 kWh je Stunde ------------------
            double[] monate = ChartRenderer.MonatsSummenMWh(Eins(8760));
            double[] soll = { 0.744, 0.672, 0.744, 0.720, 0.744, 0.720,
                              0.744, 0.744, 0.720, 0.744, 0.720, 0.744 };
            Assert.Equal(soll, monate);             // 8760 kWh = 8,760 MWh

            // Eine zu kurze Reihe bricht nicht ab, sie fuellt nur den Anfang.
            double[] kurz = ChartRenderer.MonatsSummenMWh(Eins(100));
            Assert.Equal(0.1, kurz[0]);
            for (int m = 1; m < 12; m++) Assert.Equal(0.0, kurz[m]);

            // Ohne Reihe zwoelf Nullen — nicht null.
            double[] leer = ChartRenderer.MonatsSummenMWh(null);
            Assert.Equal(12, leer.Length);
            for (int m = 0; m < 12; m++) Assert.Equal(0.0, leer[m]);
        }

        /// <summary>Stundenreihe mit durchgehend 1 kWh.</summary>
        private static double[] Eins(int stunden)
        {
            var w = new double[stunden];
            for (int i = 0; i < stunden; i++) w[i] = 1.0;
            return w;
        }

        // =====================================================================
        // 2 — das Zeichnen liefert ein PNG in der festgelegten Groesse
        // =====================================================================

        /// <summary>
        /// <c>Kuchen</c> zeichnet in 960×600 (Konzept Kap. 6.1) und gibt PNG-Bytes
        /// zurueck. Der Test ist zugleich der Nachweis, dass die NATIVE
        /// SkiaSharp-Bibliothek auf dem Bausystem vorhanden ist: Fehlt sie, wirft
        /// schon das Anlegen der Zeichenflaeche.
        /// </summary>
        [Fact]
        public void Kuchen_liefert_PNG_in_960x600()
        {
            var segmente = new List<ChartRenderer.Segment>
            {
                new ChartRenderer.Segment("Wärmepumpe", 60.0, ChartRenderer.C_WP),
                new ChartRenderer.Segment("Spitzenkessel", 40.0, ChartRenderer.C_KESSEL)
            };

            byte[] png = ChartRenderer.Kuchen("Wärmedeckung", segmente);

            Assert.NotNull(png);
            byte[] signatur = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            Assert.True(png.Length > signatur.Length);
            for (int i = 0; i < signatur.Length; i++) Assert.Equal(signatur[i], png[i]);

            using (SKBitmap bild = SKBitmap.Decode(png))
            {
                Assert.NotNull(bild);
                Assert.Equal(960, bild.Width);
                Assert.Equal(600, bild.Height);
            }
        }

        // =====================================================================
        // 3 — Determinismus
        // =====================================================================

        /// <summary>
        /// Zwei Laeufe desselben Diagramms muessen byte-gleich sein. Ohne diese Zusage
        /// waere ein Bericht zwischen zwei Erzeugungen nicht vergleichbar und der
        /// Bildvergleich der Referenzlauf-Suite ohne Aussage.
        /// </summary>
        [Fact]
        public void Zweimal_gezeichnet_ergibt_dieselben_Bytes()
        {
            var balken = new List<ChartRenderer.Balken>
            {
                new ChartRenderer.Balken("Stamm", 412.0, true),
                new ChartRenderer.Balken("Variante A", 355.0, false),
                new ChartRenderer.Balken("Variante B", 298.0, false)
            };

            byte[] a = ChartRenderer.BalkenHorizontal("Brennstoffeinsatz", "MWh/a", balken);
            byte[] b = ChartRenderer.BalkenHorizontal("Brennstoffeinsatz", "MWh/a", balken);

            Assert.NotNull(a);
            Assert.Equal(a, b);

            // Die Hoehe waechst mit der Zahl der Balken: 150 + n * 64.
            using (SKBitmap bild = SKBitmap.Decode(a))
            {
                Assert.Equal(1240, bild.Width);
                Assert.Equal(150 + balken.Count * 64, bild.Height);
            }
        }
    }
}
