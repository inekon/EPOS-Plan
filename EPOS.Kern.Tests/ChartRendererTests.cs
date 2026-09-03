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
    /// macht die Probe <c>Proben\ChartProben</c> (der Pixelvergleich gegen den
    /// eingefrorenen GDI+-Stand ist mit Entscheid iF23 am 03.09.2026 samt dem
    /// GDI+-Renderer geloescht). Diese drei Tests sind die schnelle
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
        /// waere ein Bericht zwischen zwei Erzeugungen nicht vergleichbar und die
        /// Determinismuspruefung der ChartProben ohne Aussage.
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

        // =====================================================================
        // 4 — Kostenprofil (iU9-W3.4)
        // =====================================================================

        /// <summary>
        /// Das Kostenprofil des Preisdialogs zeichnet in 1296x780 — der doppelten
        /// Zielaufloesung des abgeloesten WinForms-Chart (648x390) — und ist
        /// deterministisch. Beides ist Bedingung dafuer, dass der Dialog das Bild
        /// zwischenspeichern und die Probe <c>Proben\ChartProben</c> es
        /// pixelweise pruefen kann.
        ///
        /// <para>Die Reihe laeuft hier ins Negative: Ein Wochenwert des
        /// Kostenprofils ist eine ABWEICHUNG und darf den Monatswert unter null
        /// ziehen. Der Renderer muss dafuer eine vorzeichenfaehige Achse
        /// aufspannen, ohne die Linie abzuschneiden.</para>
        /// </summary>
        [Fact]
        public void Kostenprofil_zeichnet_deterministisch_in_1296x780()
        {
            var profil = new double[8760];
            for (int i = 0; i < profil.Length; i++)
                profil[i] = 25.0 + 6.0 * System.Math.Sin(2.0 * System.Math.PI * i / 8760.0)
                          + 3.0 * System.Math.Sin(2.0 * System.Math.PI * (i % 24) / 24.0)
                          - (i > 8000 ? 40.0 : 0.0);          // Schlussabschnitt unter null

            byte[] a = ChartRenderer.Kostenprofil("Kostenprofil", profil, "ct/kWh", "Monat");
            byte[] b = ChartRenderer.Kostenprofil("Kostenprofil", profil, "ct/kWh", "Monat");

            Assert.NotNull(a);
            Assert.Equal(a, b);

            using (SKBitmap bild = SKBitmap.Decode(a))
            {
                Assert.Equal(1296, bild.Width);
                Assert.Equal(780, bild.Height);
            }
        }

        /// <summary>
        /// Ohne Reihe (oder mit einer zu kurzen) liefert der Renderer trotzdem ein
        /// Bild in voller Groesse mit einem Hinweis darin — genau wie
        /// <c>KapitalwertVerlauf</c> bei „keine berechenbaren Reihen". Der Dialog
        /// braucht in jedem Fall etwas zum Anzeigen.
        /// </summary>
        [Fact]
        public void Kostenprofil_ohne_Reihe_liefert_ein_leeres_Bild_statt_null()
        {
            byte[] leer = ChartRenderer.Kostenprofil("Kostenprofil", null, "ct/kWh", "Monat");
            byte[] kurz = ChartRenderer.Kostenprofil("Kostenprofil", new double[1], "ct/kWh", "Monat");

            Assert.NotNull(leer);
            Assert.NotNull(kurz);

            using (SKBitmap bild = SKBitmap.Decode(leer))
            {
                Assert.Equal(1296, bild.Width);
                Assert.Equal(780, bild.Height);
            }
        }

        // =============================================================== Kennlinien (W7.0c)

        /// <summary>Drei Vorlaufstufen mit je vier Stuetzstellen — genug fuer Farbe und Marke.</summary>
        private static System.Collections.Generic.List<ChartRenderer.KennlinienReihe> Kennlinienproben()
        {
            var l = new System.Collections.Generic.List<ChartRenderer.KennlinienReihe>();
            foreach (int vorlauf in new[] { 35, 45, 55 })
            {
                var p = new System.Collections.Generic.List<(double, double)>();
                for (int t = -15; t <= 15; t += 10) p.Add((t, 5.0 - (vorlauf - 35) * 0.02 + t * 0.1));
                l.Add(new ChartRenderer.KennlinienReihe(vorlauf, p));
            }
            return l;
        }

        [Fact]
        public void Kennlinien_zeichnet_in_der_festgelegten_Groesse_und_deterministisch()
        {
            var reihen = Kennlinienproben();

            byte[] a = ChartRenderer.Kennlinien("Kennlinien COP", "COP", "Temperatur",
                                                reihen, ChartRenderer.Kennlinienmarke.Kreis);
            byte[] b = ChartRenderer.Kennlinien("Kennlinien COP", "COP", "Temperatur",
                                                reihen, ChartRenderer.Kennlinienmarke.Kreis);

            Assert.NotNull(a);
            Assert.Equal(a, b);   // zweimal zeichnen = byte-gleich

            using (SKBitmap bild = SKBitmap.Decode(a))
            {
                Assert.Equal(968, bild.Width);
                Assert.Equal(520, bild.Height);
            }
        }

        /// <summary>
        /// Die beiden Punktmarken sollen SICHTBAR verschieden sein — sonst waeren die
        /// Reiterblaetter „COP" und „Leistung" bei gleichen Werten nicht zu unterscheiden.
        /// </summary>
        [Fact]
        public void Kreis_und_Kreuz_ergeben_verschiedene_Bilder()
        {
            var reihen = Kennlinienproben();

            byte[] kreis = ChartRenderer.Kennlinien("K", "COP", "Temperatur",
                                                    reihen, ChartRenderer.Kennlinienmarke.Kreis);
            byte[] kreuz = ChartRenderer.Kennlinien("K", "COP", "Temperatur",
                                                    reihen, ChartRenderer.Kennlinienmarke.Kreuz);

            Assert.NotEqual(kreis, kreuz);
        }

        /// <summary>
        /// Ohne Reihen liefert der Renderer ein Bild in voller Groesse mit Hinweis —
        /// dieselbe Zusage wie beim Kostenprofil: Der Dialog braucht in jedem Fall
        /// etwas zum Anzeigen. Das trifft die Waermepumpen ohne Kennlinien.
        /// </summary>
        [Fact]
        public void Kennlinien_ohne_Reihen_liefert_ein_leeres_Bild_statt_null()
        {
            byte[] leer = ChartRenderer.Kennlinien("K", "COP", "Temperatur", null,
                                                   ChartRenderer.Kennlinienmarke.Kreis);
            byte[] ohnePunkte = ChartRenderer.Kennlinien("K", "COP", "Temperatur",
                new[] { new ChartRenderer.KennlinienReihe(35, System.Array.Empty<(double, double)>()) },
                ChartRenderer.Kennlinienmarke.Kreis);

            Assert.NotNull(leer);
            Assert.NotNull(ohnePunkte);

            using (SKBitmap bild = SKBitmap.Decode(leer))
            {
                Assert.Equal(968, bild.Width);
                Assert.Equal(520, bild.Height);
            }
        }

        // =========================================================== Bedarfsbilder (iU9-W8.0c)

        private static double[] Monatsprobe()
        {
            var w = new double[12];
            for (int m = 0; m < 12; m++) w[m] = 10.0 + m;
            return w;
        }

        /// <summary>
        /// Mass und Determinismus der drei neuen Bilder. Sie ersetzen die Charts der zehn
        /// Bedarfsmasken der Welle 8; ohne diese Zusage koennte sich ein Bild zwischen zwei
        /// Laeufen unterscheiden, und die ChartProben wuerden es erst spaeter melden.
        /// </summary>
        [Fact]
        public void Die_drei_Bedarfsbilder_haben_ihr_Mass_und_sind_deterministisch()
        {
            byte[] saeulen = ChartRenderer.MonatsSaeulen("M", Monatsprobe(), SKColors.YellowGreen, "MWh");
            byte[] saeulen2 = ChartRenderer.MonatsSaeulen("M", Monatsprobe(), SKColors.YellowGreen, "MWh");
            Assert.Equal(saeulen, saeulen2);
            using (SKBitmap bild = SKBitmap.Decode(saeulen))
            {
                Assert.Equal(978, bild.Width);
                Assert.Equal(542, bild.Height);
            }

            var profil = new double[168];
            for (int i = 0; i < 168; i++) profil[i] = 0.5 + 0.4 * System.Math.Sin(i / 4.0);
            byte[] stunden = ChartRenderer.Stundenprofil("P", profil, 24, "Stunde", "Verteilung");
            byte[] stunden2 = ChartRenderer.Stundenprofil("P", profil, 24, "Stunde", "Verteilung");
            Assert.Equal(stunden, stunden2);
            using (SKBitmap bild = SKBitmap.Decode(stunden))
            {
                Assert.Equal(1244, bild.Width);
                Assert.Equal(464, bild.Height);
            }

            var jahr = new double[8760];
            for (int i = 0; i < jahr.Length; i++) jahr[i] = 100 + 40 * System.Math.Sin(i / 700.0);
            byte[] verlauf = ChartRenderer.Jahresverlauf("J", jahr, "kW", SKColors.SteelBlue);
            byte[] verlauf2 = ChartRenderer.Jahresverlauf("J", jahr, "kW", SKColors.SteelBlue);
            Assert.Equal(verlauf, verlauf2);
            using (SKBitmap bild = SKBitmap.Decode(verlauf))
            {
                Assert.Equal(978, bild.Width);
                Assert.Equal(542, bild.Height);
            }
        }

        /// <summary>
        /// Der Rueckfall „alles null" der Vorlaeufer (<c>SkaliereYAchse</c>: Maximum 5,
        /// Intervall 1) darf das Bild nicht zerstoeren — zwoelf Nullen sind ein
        /// gueltiger Zustand, solange die Simulation noch nicht gelaufen ist.
        /// </summary>
        [Fact]
        public void Monatssaeulen_mit_lauter_Nullen_bleiben_ein_Bild()
        {
            byte[] png = ChartRenderer.MonatsSaeulen("M", new double[12], SKColors.Red, "MWh");
            Assert.NotNull(png);
            using (SKBitmap bild = SKBitmap.Decode(png))
            {
                Assert.Equal(978, bild.Width);
                Assert.Equal(542, bild.Height);
            }
        }

        /// <summary>
        /// Zu kurze oder fehlende Reihen liefern ein Bild MIT HINWEIS, nicht <c>null</c> —
        /// dieselbe Zusage wie bei Kostenprofil und Kennlinien. Der Ergebnisdialog bekommt
        /// die Reihe direkt aus der Simulation und kann leer aufgerufen werden.
        /// </summary>
        [Fact]
        public void Bedarfsbilder_ohne_Werte_liefern_ein_leeres_Bild_statt_null()
        {
            Assert.NotNull(ChartRenderer.MonatsSaeulen("M", null, SKColors.Red, "MWh"));
            Assert.NotNull(ChartRenderer.MonatsSaeulen("M", new double[3], SKColors.Red, "MWh"));
            Assert.NotNull(ChartRenderer.Stundenprofil("P", null, 24, "x", "y"));
            Assert.NotNull(ChartRenderer.Jahresverlauf("J", null, "kW", SKColors.SteelBlue));
        }
    }
}
