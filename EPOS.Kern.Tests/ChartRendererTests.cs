using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
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

        // -----------------------------------------------------------------------------
        // 7 — Jahresgang, zweireihig (iU9-W10a.0d)
        // -----------------------------------------------------------------------------

        private static List<ChartRenderer.Reihe> Jahresgangreihen()
        {
            var quelle = new double[8760];
            var aussen = new double[8760];
            for (int i = 0; i < 8760; i++)
            {
                double jahr = 2.0 * Math.PI * i / 8760.0;
                quelle[i] = 9 + 4 * Math.Sin(jahr - Math.PI / 2);
                aussen[i] = 9 + 14 * Math.Sin(jahr - Math.PI / 2);
            }
            return new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Quelltemperatur", quelle, ChartRenderer.C_QUELLTEMPERATUR),
                new ChartRenderer.Reihe("Aussentemperatur", aussen, ChartRenderer.C_AUSSENTEMPERATUR)
            };
        }

        /// <summary>
        /// Das Bild des Erdreich-Dialogs misst 1304x440 — die doppelte Zielaufloesung des
        /// abgeloesten WinForms-Chart (652x170) plus 100 px fuer die Legende, die dort
        /// INNERHALB der Zeichenflaeche stand und den Jahresanfang verdeckte.
        /// </summary>
        [Fact]
        public void Jahresgang_zeichnet_deterministisch_in_1304x440()
        {
            List<ChartRenderer.Reihe> reihen = Jahresgangreihen();

            byte[] a = ChartRenderer.Jahresgang("Jahresgang", reihen, "Monat", "Temperatur");
            byte[] b = ChartRenderer.Jahresgang("Jahresgang", reihen, "Monat", "Temperatur");

            Assert.NotNull(a);
            Assert.Equal(a, b);
            using (SKBitmap bild = SKBitmap.Decode(a))
            {
                Assert.Equal(1304, bild.Width);
                Assert.Equal(440, bild.Height);
            }
        }

        /// <summary>
        /// Die beiden Farben stehen woertlich im Vorlaeufer (SaddleBrown mit Deckung 200,
        /// SteelBlue mit Deckung 90). Die dickere Braunlinie muss im fertigen Bild
        /// ankommen — ueber Weiss gemischt ergibt sie 164/109/70.
        /// </summary>
        [Fact]
        public void Jahresgang_traegt_die_Quelltemperatur_in_SaddleBrown()
        {
            Assert.Equal(new SKColor(0x8B, 0x45, 0x13, 200), ChartRenderer.C_QUELLTEMPERATUR);
            Assert.Equal(new SKColor(0x46, 0x82, 0xB4, 90), ChartRenderer.C_AUSSENTEMPERATUR);

            byte[] png = ChartRenderer.Jahresgang("Jahresgang", Jahresgangreihen(),
                                                  "Monat", "Temperatur");
            using (SKBitmap bild = SKBitmap.Decode(png))
            {
                var vorhanden = new HashSet<uint>();
                foreach (SKColor p in bild.Pixels) vorhanden.Add((uint)p);
                Assert.Contains((uint)new SKColor(164, 109, 70), vorhanden);
            }
        }

        /// <summary>
        /// EINE Reihe geht auch: Der Vorlaeufer zeichnete die Aussentemperatur nur, wenn
        /// das Projekt Klimadaten fuehrt. Ohne Reihen kommt ein Bild MIT HINWEIS zurueck,
        /// nicht <c>null</c> — dieselbe Zusage wie bei allen Bildern dieser Datei.
        /// </summary>
        [Fact]
        public void Jahresgang_traegt_eine_Reihe_und_meldet_keine()
        {
            List<ChartRenderer.Reihe> eine = Jahresgangreihen();
            eine.RemoveAt(1);
            Assert.NotNull(ChartRenderer.Jahresgang("J", eine, "Monat", "Temperatur"));

            Assert.NotNull(ChartRenderer.Jahresgang("J", null, "Monat", "Temperatur"));
            Assert.NotNull(ChartRenderer.Jahresgang(
                "J", new List<ChartRenderer.Reihe>(), "Monat", "Temperatur"));

            // Eine Reihe mit weniger als zwei Werten ist keine Linie und faellt weg.
            Assert.NotNull(ChartRenderer.Jahresgang("J", new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("kurz", new double[1], ChartRenderer.C_QUELLTEMPERATUR)
            }, "Monat", "Temperatur"));
        }

        /// <summary>
        /// DER ZEITAUSSCHNITT (Anwenderwunsch KL-8): Mit Fenster zeichnet der Jahresgang
        /// ein ANDERES Bild — dasselbe Mass, dieselbe Gestalt, aber die zugeschnittene
        /// Reihe und die Jahresstunden auf der x-Achse. OHNE Fenster bleibt er Bild fuer
        /// Bild das des Bestands; das ist die Zusage an jeden bisherigen Aufrufer und an
        /// die ChartProben, die Bilder vergleichen.
        /// </summary>
        [Fact]
        public void Jahresgang_zeichnet_den_Zeitausschnitt_und_laesst_das_Jahr_unberuehrt()
        {
            List<ChartRenderer.Reihe> reihen = Jahresgangreihen();

            byte[] ganz = ChartRenderer.Jahresgang("Jahresgang", reihen, "Monat", "Temperatur");
            byte[] ohneFenster = ChartRenderer.Jahresgang("Jahresgang", reihen, "Monat",
                                                          "Temperatur", false, null);
            Assert.Equal(ganz, ohneFenster);

            var fenster = new ChartRenderer.Achsenfenster(2900, 3400);
            byte[] teil = ChartRenderer.Jahresgang("Jahresgang", reihen, "Monat",
                                                   "Temperatur", false, fenster);
            byte[] teilZweimal = ChartRenderer.Jahresgang("Jahresgang", reihen, "Monat",
                                                          "Temperatur", false, fenster);

            Assert.NotEqual(ganz, teil);
            Assert.Equal(teil, teilZweimal);

            using (SKBitmap bild = SKBitmap.Decode(teil))
            {
                Assert.Equal(1304, bild.Width);
                Assert.Equal(440, bild.Height);
            }

            // Ein anderer Ausschnitt ergibt ein anderes Bild - sonst waere das Fenster
            // still uebergangen.
            byte[] anderer = ChartRenderer.Jahresgang(
                "Jahresgang", reihen, "Monat", "Temperatur", false,
                new ChartRenderer.Achsenfenster(100, 600));
            Assert.NotEqual(teil, anderer);
        }

        // -----------------------------------------------------------------------------
        // 8 — Das Modell hinter dem Bild (Konzept Diagramme, Etappe E2)
        // -----------------------------------------------------------------------------

        /// <summary>
        /// <b>Ein Modell, zwei Ausgaben.</b> <c>Jahresgang</c> ist seit der Etappe E2
        /// nur noch <c>SkiaMaler.Png(JahresgangModell(...))</c> — das muss Byte fuer
        /// Byte dasselbe Bild ergeben, sonst haette der Umbau den Bericht veraendert.
        /// Geprueft in allen drei Lagen: Vollansicht, Fenster und der Leerfall.
        /// </summary>
        [Fact]
        public void Jahresgang_liefert_dieselben_Bytes_wie_der_Maler_aus_dem_Modell()
        {
            List<ChartRenderer.Reihe> reihen = Jahresgangreihen();
            var fenster = new ChartRenderer.Achsenfenster(2900, 3400);

            Assert.Equal(
                ChartRenderer.Jahresgang("Jahresgang", reihen, "Monat", "Temperatur"),
                SkiaMaler.Png(ChartRenderer.JahresgangModell(
                    "Jahresgang", reihen, "Monat", "Temperatur")));

            Assert.Equal(
                ChartRenderer.Jahresgang("Jahresgang", reihen, "Monat", "Temperatur",
                                         false, fenster),
                SkiaMaler.Png(ChartRenderer.JahresgangModell(
                    "Jahresgang", reihen, "Monat", "Temperatur", false, fenster)));

            Assert.Equal(
                ChartRenderer.Jahresgang("J", null, "Monat", "Temperatur"),
                SkiaMaler.Png(ChartRenderer.JahresgangModell("J", null, "Monat", "Temperatur")));
        }

        /// <summary>
        /// Das Modell traegt ueber die Befehlsliste hinaus DREIERLEI: die
        /// Zeichenflaeche samt Datenfenster, je Reihe eine <c>Datenreihe</c> mit den
        /// UNGEKUERZTEN Werten (der Pixelpfad im Befehl ist auf jeden n-ten Wert
        /// gekuerzt — Entscheid DG-E2-2) und die Marken, an denen die Oberflaeche
        /// Gruppen schaltet.
        /// </summary>
        [Fact]
        public void JahresgangModell_traegt_Flaeche_Reihen_und_Marken()
        {
            Zeichenmodell m = ChartRenderer.JahresgangModell(
                "Jahresgang", Jahresgangreihen(), "Monat", "Temperatur");

            Assert.Equal(1304, m.Breite);
            Assert.Equal(440, m.Hoehe);

            // Die Zeichenflaeche: dasselbe Rechteck, mit dem das Layout rechnet.
            Assert.NotNull(m.Flaeche);
            Assert.Equal(110f, m.Flaeche.Bild.X);
            Assert.Equal(130f, m.Flaeche.Bild.Y);
            Assert.Equal(1154f, m.Flaeche.Bild.Breite);
            Assert.Equal(240f, m.Flaeche.Bild.Hoehe);
            Assert.Equal(0.0, m.Flaeche.Daten.XVon);
            Assert.Equal(8759.0, m.Flaeche.Daten.XBis);
            Assert.True(m.Flaeche.Daten.YVon < 0.0, "die Skala reicht unter null");
            Assert.True(m.Flaeche.Daten.YBis > 0.0);

            // Je Reihe eine Datenreihe - Name, Rolle, Staerke wie im PNG, alle Werte.
            Assert.Equal(2, m.Reihen.Count);
            Assert.Equal("Quelltemperatur", m.Reihen[0].Name);
            Assert.Equal("Aussentemperatur", m.Reihen[1].Name);
            Assert.Equal(8760, m.Reihen[0].Werte.Length);
            Assert.Equal(2f, m.Reihen[0].Staerke);
            Assert.Equal(1f, m.Reihen[1].Staerke);
            Assert.Equal(Farbrolle.QUELLTEMPERATUR, m.Reihen[0].Ton.Rolle);
            Assert.Equal(Farbrolle.AUSSENTEMPERATUR, m.Reihen[1].Ton.Rolle);

            var marken = new HashSet<string>(
                m.Befehle.Where(b => b.Marke != null).Select(b => b.Marke),
                StringComparer.Ordinal);
            Assert.Contains("titel", marken);
            Assert.Contains("xachse", marken);
            Assert.Contains("yachse", marken);
            Assert.Contains("nulllinie", marken);
            Assert.Contains("legende:Quelltemperatur", marken);
            Assert.Contains("legende:Aussentemperatur", marken);
            Assert.Contains("reihe:Quelltemperatur", marken);
            Assert.Contains("reihe:Aussentemperatur", marken);

            // Das ACHSENKREUZ bleibt ohne Marke: Es steht auch dann, wenn die
            // Oberflaeche beim Zoom die Teilung der x-Achse ausblendet.
            Assert.Contains(m.Befehle, b => b.Marke == null);
        }

        /// <summary>
        /// Ohne Reihen gibt es keine Zeichenflaeche und keine Datenreihen — aber den
        /// LEERHINWEIS, damit die Oberflaeche ihn findet.
        /// </summary>
        [Fact]
        public void JahresgangModell_ohne_Reihen_traegt_nur_den_Leerhinweis()
        {
            Zeichenmodell m = ChartRenderer.JahresgangModell("J", null, "Monat", "Temperatur");

            Assert.Null(m.Flaeche);
            Assert.Empty(m.Reihen);
            Assert.Contains(m.Befehle, b => b.Marke == "leerhinweis");
        }

        /// <summary>
        /// Im Fenster steht das Datenfenster auf den FENSTERSTUNDEN, und die Reihen
        /// tragen die zugeschnittenen Werte — genau die Zahlen, aus denen die
        /// Oberflaeche ihre Zeigerzeile liest.
        /// </summary>
        [Fact]
        public void JahresgangModell_im_Fenster_zeigt_die_Fensterstunden()
        {
            Zeichenmodell m = ChartRenderer.JahresgangModell(
                "Jahresgang", Jahresgangreihen(), "Monat", "Temperatur", false,
                new ChartRenderer.Achsenfenster(2900, 3400));

            Assert.Equal(2900.0, m.Flaeche.Daten.XVon);
            Assert.Equal(3399.0, m.Flaeche.Daten.XBis);
            Assert.Equal(500, m.Reihen[0].Werte.Length);
        }

        /// <summary>
        /// <b><c>Jahresstundenteilung</c> ist die Regel, mit der die x-Achse eines
        /// zugeschnittenen Bildes geteilt wird</b> — als reine Funktion, damit die
        /// Oberflaeche die Ticks beim Zoom nachzeichnen kann, ohne den Kern zu rufen.
        /// Sie muss sich mit dem decken, was ins Bild geht: dieselben Stunden,
        /// dieselben Beschriftungen, dieselbe Reihenfolge.
        /// </summary>
        [Fact]
        public void Jahresstundenteilung_deckt_sich_mit_der_gezeichneten_Achse()
        {
            Zeichenmodell m = ChartRenderer.JahresgangModell(
                "Jahresgang", Jahresgangreihen(), "Monat", "Temperatur", false,
                new ChartRenderer.Achsenfenster(2900, 3400));

            List<string> gezeichnet = m.Befehle
                .OfType<WindowsFormsApplication1.Zeichnung.Text>()
                .Where(t => t.Marke == "xachse")
                .Select(t => t.Inhalt)
                .ToList();
            int rasterlinien = m.Befehle
                .OfType<WindowsFormsApplication1.Zeichnung.Linie>()
                .Count(l => l.Marke == "xachse");

            IReadOnlyList<(int Stunde, string Text)> teilung =
                ChartRenderer.Jahresstundenteilung(2900, 3399);

            Assert.NotEmpty(teilung);
            Assert.Equal(teilung.Count, rasterlinien);

            // Die Beschriftungen, dann der Achsentitel (CHART_ACHSE_JAHRESSTUNDEN).
            Assert.Equal(teilung.Count + 1, gezeichnet.Count);
            for (int i = 0; i < teilung.Count; i++)
                Assert.Equal(teilung[i].Text, gezeichnet[i]);

            // Die Stunden liegen im Fenster und steigen.
            Assert.All(teilung, t => Assert.InRange(t.Stunde, 2900, 3399));
            for (int i = 1; i < teilung.Count; i++)
                Assert.True(teilung[i].Stunde > teilung[i - 1].Stunde);

            // Ohne Spanne gibt es keine Teilung statt einer Ausnahme.
            Assert.Empty(ChartRenderer.Jahresstundenteilung(100, 100));
        }
    }
}
