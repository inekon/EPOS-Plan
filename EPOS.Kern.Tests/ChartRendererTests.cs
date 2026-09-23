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

        // -----------------------------------------------------------------------------
        // 9 — Die sieben Zeitreihen-Modelle der Ergebnisreiter (Etappe E3, Gruppe a)
        // -----------------------------------------------------------------------------

        /// <summary>Eine Jahresreihe mit Jahres- und Tagesgang — deterministisch.</summary>
        private static double[] Jahresreihe(double grund, double jahresHub, double tagesHub,
                                            double versatz = 0)
        {
            var w = new double[8760];
            for (int i = 0; i < w.Length; i++)
                w[i] = grund
                     + jahresHub * Math.Sin(2 * Math.PI * i / 8760.0 - Math.PI / 2 + versatz)
                     + tagesHub * Math.Sin(2 * Math.PI * (i % 24) / 24.0);
            return w;
        }

        private static List<ChartRenderer.Reihe> Stapelreihen()
            => new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Waermepumpe", Jahresreihe(60, 45, 12), SKColors.Orange,
                                        ChartRenderer.Stapelart.Saeule),
                new ChartRenderer.Reihe("Heizkessel", Jahresreihe(50, 40, 10, 0.2), SKColors.Blue,
                                        ChartRenderer.Stapelart.Saeule)
            };

        private static List<ChartRenderer.Reihe> Temperaturreihen()
            => new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Puffer oben", Jahresreihe(62, 8, 0), SKColors.Firebrick),
                new ChartRenderer.Reihe("Puffer unten", Jahresreihe(48, 6, 0), SKColors.Firebrick,
                                        ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt)
            };

        /// <summary>
        /// <b>Ein Modell, zwei Ausgaben — für alle sieben Bilder der Gruppe (a).</b>
        /// Jede <c>byte[]</c>-Methode ist seit der Etappe E3 nur noch
        /// <c>SkiaMaler.Png(…Modell(…))</c>; das muss Byte für Byte dasselbe Bild
        /// ergeben, sonst hätte der Umbau den Bericht verändert. Die Hash-Messlatte der
        /// ChartProben sagt dasselbe über die ganze Bildmenge — dieser Fall sagt es in
        /// jedem Kern-Lauf.
        /// </summary>
        [Fact]
        public void Die_sieben_Bilder_liefern_dieselben_Bytes_wie_der_Maler_aus_ihrem_Modell()
        {
            double[] profil = Jahresreihe(24, 6, 3);
            double[] woche = Jahresreihe(1, 0.4, 0.3).Take(168).ToArray();
            double[] bedarf = Jahresreihe(140, 90, 30);
            List<ChartRenderer.Reihe> stapel = Stapelreihen();
            List<ChartRenderer.Reihe> temperaturen = Temperaturreihen();
            var speicher = new ChartRenderer.Reihe("Ladezustand", Jahresreihe(400, 120, 180),
                                                   SKColors.MediumVioletRed);

            Assert.Equal(ChartRenderer.Kostenprofil("K", profil, "ct/kWh", "Monat"),
                         SkiaMaler.Png(ChartRenderer.KostenprofilModell("K", profil, "ct/kWh", "Monat")));

            Assert.Equal(ChartRenderer.Stundenprofil("S", woche, 24, "Stunde", "Verteilung"),
                         SkiaMaler.Png(ChartRenderer.StundenprofilModell("S", woche, 24, "Stunde",
                                                                         "Verteilung")));

            Assert.Equal(ChartRenderer.Jahresverlauf("J", bedarf, "kW", SKColors.SteelBlue),
                         SkiaMaler.Png(ChartRenderer.JahresverlaufModell("J", bedarf, "kW",
                                                                         SKColors.SteelBlue)));

            Assert.Equal(ChartRenderer.GanglinieNormiert("G", stapel, "%",
                                                         ChartRenderer.Achse.Monate, false),
                         SkiaMaler.Png(ChartRenderer.GanglinieNormiertModell(
                             "G", stapel, "%", ChartRenderer.Achse.Monate, false)));

            Assert.Equal(ChartRenderer.ErzeugerStapel("E", stapel, null, null, "kW",
                                                      ChartRenderer.Achse.Monate, false),
                         SkiaMaler.Png(ChartRenderer.ErzeugerStapelModell(
                             "E", stapel, null, null, "kW", ChartRenderer.Achse.Monate, false)));

            Assert.Equal(ChartRenderer.Temperaturverlauf("T", temperaturen, true),
                         SkiaMaler.Png(ChartRenderer.TemperaturverlaufModell("T", temperaturen, true)));

            Assert.Equal(ChartRenderer.Speicherbetrieb("L", temperaturen, "kW", speicher, "kWh"),
                         SkiaMaler.Png(ChartRenderer.SpeicherbetriebModell(
                             "L", temperaturen, "kW", speicher, "kWh")));
        }

        /// <summary>
        /// Dasselbe im FENSTER und im LEERFALL — die beiden Lagen, in denen ein Bild
        /// einen anderen Weg nimmt.
        /// </summary>
        [Fact]
        public void Die_sieben_Bilder_bleiben_auch_im_Fenster_und_leer_byte_gleich()
        {
            double[] bedarf = Jahresreihe(140, 90, 30);
            List<ChartRenderer.Reihe> stapel = Stapelreihen();
            var fenster = new ChartRenderer.Achsenfenster(3000, 3500, 0.6);

            Assert.Equal(ChartRenderer.Jahresverlauf("J", bedarf, "kW", SKColors.SteelBlue, fenster),
                         SkiaMaler.Png(ChartRenderer.JahresverlaufModell("J", bedarf, "kW",
                                                                         SKColors.SteelBlue, fenster)));
            Assert.Equal(ChartRenderer.GanglinieNormiert("G", stapel, "%",
                                                         ChartRenderer.Achse.Jahresstunden, true,
                                                         fenster),
                         SkiaMaler.Png(ChartRenderer.GanglinieNormiertModell(
                             "G", stapel, "%", ChartRenderer.Achse.Jahresstunden, true, fenster)));
            Assert.Equal(ChartRenderer.ErzeugerStapel("E", stapel, null, null, "kW",
                                                      ChartRenderer.Achse.Jahresstunden, false,
                                                      null, null, fenster),
                         SkiaMaler.Png(ChartRenderer.ErzeugerStapelModell(
                             "E", stapel, null, null, "kW", ChartRenderer.Achse.Jahresstunden,
                             false, null, null, fenster)));
            Assert.Equal(ChartRenderer.Temperaturverlauf("T", Temperaturreihen(), true, fenster),
                         SkiaMaler.Png(ChartRenderer.TemperaturverlaufModell(
                             "T", Temperaturreihen(), true, fenster)));

            Assert.Equal(ChartRenderer.Kostenprofil("K", null, "ct/kWh", "Monat"),
                         SkiaMaler.Png(ChartRenderer.KostenprofilModell("K", null, "ct/kWh", "Monat")));
            Assert.Equal(ChartRenderer.Stundenprofil("S", null, 24, "x", "y"),
                         SkiaMaler.Png(ChartRenderer.StundenprofilModell("S", null, 24, "x", "y")));
            Assert.Equal(ChartRenderer.Jahresverlauf("J", null, "kW", SKColors.SteelBlue),
                         SkiaMaler.Png(ChartRenderer.JahresverlaufModell("J", null, "kW",
                                                                         SKColors.SteelBlue)));
            Assert.Equal(ChartRenderer.GanglinieNormiert("G", null, "%",
                                                         ChartRenderer.Achse.Monate, false),
                         SkiaMaler.Png(ChartRenderer.GanglinieNormiertModell(
                             "G", null, "%", ChartRenderer.Achse.Monate, false)));
            Assert.Equal(ChartRenderer.ErzeugerStapel("E", null, null, null, "kW",
                                                      ChartRenderer.Achse.Monate, false),
                         SkiaMaler.Png(ChartRenderer.ErzeugerStapelModell(
                             "E", null, null, null, "kW", ChartRenderer.Achse.Monate, false)));
            Assert.Equal(ChartRenderer.Temperaturverlauf("T", null, true),
                         SkiaMaler.Png(ChartRenderer.TemperaturverlaufModell("T", null, true)));
            Assert.Equal(ChartRenderer.Speicherbetrieb("L", null),
                         SkiaMaler.Png(ChartRenderer.SpeicherbetriebModell("L", null)));
        }

        /// <summary>
        /// Ohne Reihen trägt jedes der sieben Modelle NUR den Leerhinweis: keine
        /// Zeichenfläche (ein Bild ohne Reihen hat keine) und keine Datenreihe.
        /// </summary>
        [Fact]
        public void Die_sieben_Modelle_ohne_Reihen_tragen_nur_den_Leerhinweis()
        {
            var leer = new List<Zeichenmodell>
            {
                ChartRenderer.KostenprofilModell("K", null, "ct/kWh", "Monat"),
                ChartRenderer.StundenprofilModell("S", null, 24, "x", "y"),
                ChartRenderer.JahresverlaufModell("J", null, "kW", SKColors.SteelBlue),
                ChartRenderer.GanglinieNormiertModell("G", null, "%", ChartRenderer.Achse.Monate, false),
                ChartRenderer.ErzeugerStapelModell("E", null, null, null, "kW",
                                                   ChartRenderer.Achse.Monate, false),
                ChartRenderer.TemperaturverlaufModell("T", null, true),
                ChartRenderer.SpeicherbetriebModell("L", null)
            };

            foreach (Zeichenmodell m in leer)
            {
                Assert.Null(m.Flaeche);
                Assert.Empty(m.Reihen);
                Assert.Contains(m.Befehle, b => b.Marke == "leerhinweis");
            }
        }

        /// <summary>
        /// <b>Kostenprofil:</b> die Zeichenfläche des PNG, x als INDEX der Reihe, eine
        /// Linie in der Rolle <c>KOSTENPROFIL</c> mit den ungekürzten Werten, und die
        /// Marken samt Nulllinie (die Reihe läuft ins Negative).
        /// </summary>
        [Fact]
        public void KostenprofilModell_traegt_Flaeche_Reihe_und_Marken()
        {
            double[] profil = Jahresreihe(4, 12, 3);      // reicht unter null
            Zeichenmodell m = ChartRenderer.KostenprofilModell("Kostenprofil", profil,
                                                               "ct/kWh", "Monat");

            Assert.Equal(1296, m.Breite);
            Assert.Equal(780, m.Hoehe);
            Assert.Equal(new Rahmen(110f, 80f, 1146f, 560f), m.Flaeche.Bild);
            Assert.Equal(0.0, m.Flaeche.Daten.XVon);
            Assert.Equal(8759.0, m.Flaeche.Daten.XBis);
            Assert.True(m.Flaeche.Daten.YVon < 0.0, "die Skala reicht unter null");

            Datenreihe r = Assert.Single(m.Reihen);
            Assert.Equal("Kostenprofil", r.Name);
            Assert.Equal(Farbrolle.KOSTENPROFIL, r.Ton.Rolle);
            Assert.Equal(Reihenart.Linie, r.Art);
            Assert.Equal(8760, r.Werte.Length);
            Assert.Equal(m.Flaeche.Daten, r.Fenster);

            Assert.Equal(new[] { "titel", "yachse", "xachse", "nulllinie", "reihe:Kostenprofil",
                                 "legende:Kostenprofil" }.OrderBy(s => s, StringComparer.Ordinal),
                         Marken(m).OrderBy(s => s, StringComparer.Ordinal));
        }

        /// <summary>
        /// <b>Stundenprofil:</b> EINE Reihe, gezeichnet als FLÄCHE mit Randlinie
        /// (DG-E3-2) — Füllung <c>PROFILFLAECHE</c>, Rand <c>PROFILLINIE</c>. Ihr
        /// Fenster beginnt bei 1: Wert <c>i</c> steht am rechten Rand seines Fachs,
        /// während die Zeichenfläche von 0 bis <c>n</c> läuft (DG-E3-1).
        /// </summary>
        [Fact]
        public void StundenprofilModell_traegt_die_Flaeche_mit_ihrer_Randlinie()
        {
            double[] woche = Jahresreihe(1, 0.4, 0.3).Take(168).ToArray();
            Zeichenmodell m = ChartRenderer.StundenprofilModell("Wochenwerte", woche, 24,
                                                                "Wochenstunde", "Verteilung");

            Assert.Equal(new Rahmen(100f, 76f, 1044f, 300f), m.Flaeche.Bild);
            Assert.Equal(0.0, m.Flaeche.Daten.XVon);
            Assert.Equal(168.0, m.Flaeche.Daten.XBis);
            Assert.Equal(0.0, m.Flaeche.Daten.YVon);

            Datenreihe r = Assert.Single(m.Reihen);
            Assert.Equal("Verteilung", r.Name);
            Assert.Equal(Reihenart.Flaeche, r.Art);
            Assert.Equal(Farbrolle.PROFILFLAECHE, r.Ton.Rolle);
            Assert.Equal(Farbrolle.PROFILLINIE, r.Randton.Rolle);
            Assert.Equal(2f, r.Staerke);
            Assert.Null(r.Unten);                       // sie schliesst auf der Achsennull
            Assert.Equal(1.0, r.Fenster.XVon);
            Assert.Equal(168.0, r.Fenster.XBis);

            Assert.Equal(new[] { "titel", "yachse", "xachse", "reihe:Verteilung" }
                             .OrderBy(s => s, StringComparer.Ordinal),
                         Marken(m).OrderBy(s => s, StringComparer.Ordinal));
        }

        /// <summary>
        /// <b>Jahresverlauf:</b> x zählt Jahresstunden, im Fenster dessen Grenzen; die
        /// Reihe trägt die zugeschnittenen Werte, und die Marke der x-Achse steht an
        /// den nachgezeichneten Stundenmarken.
        /// </summary>
        [Fact]
        public void JahresverlaufModell_traegt_Flaeche_Reihe_und_Marken()
        {
            double[] bedarf = Jahresreihe(140, 90, 30);
            Zeichenmodell voll = ChartRenderer.JahresverlaufModell("Jahresuebersicht", bedarf,
                                                                   "Waermebedarf [kW]",
                                                                   SKColors.SteelBlue);

            Assert.Equal(new Rahmen(100f, 80f, 838f, 380f), voll.Flaeche.Bild);
            Assert.Equal(0.0, voll.Flaeche.Daten.XVon);
            Assert.Equal(8759.0, voll.Flaeche.Daten.XBis);
            Assert.Equal(0.0, voll.Flaeche.Daten.YVon);

            Datenreihe r = Assert.Single(voll.Reihen);
            Assert.Equal("Waermebedarf [kW]", r.Name);
            Assert.Equal(Reihenart.Linie, r.Art);
            Assert.Equal(8760, r.Werte.Length);

            Assert.Equal(new[] { "titel", "yachse", "xachse", "reihe:Waermebedarf [kW]" }
                             .OrderBy(s => s, StringComparer.Ordinal),
                         Marken(voll).OrderBy(s => s, StringComparer.Ordinal));

            Zeichenmodell teil = ChartRenderer.JahresverlaufModell(
                "Jahresuebersicht", bedarf, "Waermebedarf [kW]", SKColors.SteelBlue,
                new ChartRenderer.Achsenfenster(3000, 3500));
            Assert.Equal(3000.0, teil.Flaeche.Daten.XVon);
            Assert.Equal(3499.0, teil.Flaeche.Daten.XBis);
            Assert.Equal(500, teil.Reihen[0].Werte.Length);
        }

        /// <summary>
        /// <b>Normierte Ganglinie:</b> Die Datenreihen führen die Werte des BILDES,
        /// also PROZENT des gemeinsamen Höchstwerts — die Achse läuft bis 100,2.
        /// </summary>
        [Fact]
        public void GanglinieNormiertModell_fuehrt_Prozentwerte()
        {
            List<ChartRenderer.Reihe> reihen = Stapelreihen();
            Zeichenmodell m = ChartRenderer.GanglinieNormiertModell(
                "Waermelast", reihen, "Anteil", ChartRenderer.Achse.Monate, false);

            Assert.Equal(0.0, m.Flaeche.Daten.YVon);
            Assert.Equal(100.2, m.Flaeche.Daten.YBis);
            Assert.Equal(2, m.Reihen.Count);

            double bezug = reihen.Max(x => x.Werte.Max());
            Assert.Equal(reihen[0].Werte[17] / bezug * 100.0, m.Reihen[0].Werte[17], 9);
            Assert.All(m.Reihen, r => Assert.All(r.Werte, w => Assert.InRange(w, -0.001, 100.001)));
            Assert.All(m.Reihen, r => Assert.Equal(Reihenart.Linie, r.Art));

            // Die Dauerlinie sortiert JEDE Reihe fuer sich - x zaehlt dann den Rang.
            Zeichenmodell dauer = ChartRenderer.GanglinieNormiertModell(
                "Waermelast", reihen, "Anteil", ChartRenderer.Achse.Jahresstunden, true);
            for (int i = 1; i < 100; i++)
                Assert.True(dauer.Reihen[0].Werte[i] <= dauer.Reihen[0].Werte[i - 1]);
        }

        /// <summary>
        /// <b>Erzeugerstapel:</b> jede Schicht eine FLÄCHE mit ihrer Unterkante, die
        /// Kontur- und Bedarfslinie Linien, die zweite Achse mit EIGENEM Fenster und
        /// der Marke <c>yachse2</c> (DG-E3-1/2).
        /// </summary>
        [Fact]
        public void ErzeugerStapelModell_stapelt_Flaechen_und_traegt_die_zweite_Achse()
        {
            double[] gesamt = Jahresreihe(180, 120, 40);
            List<ChartRenderer.Reihe> stapel = Stapelreihen();
            var speicher = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Puffer 1", Jahresreihe(900, 600, 250),
                                        SKColors.MediumVioletRed)
            };

            Zeichenmodell m = ChartRenderer.ErzeugerStapelModell(
                "Waermeproduktion", stapel,
                new List<ChartRenderer.Reihe>
                { new ChartRenderer.Reihe("Waermebedarf", gesamt, SKColors.DarkCyan) },
                new ChartRenderer.Reihe("Gesamt", gesamt, SKColors.Green,
                                        ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Durchgezogen,4f),
                "Waermelast [kW]", ChartRenderer.Achse.Monate, false,
                speicher, "Speicherinhalt [kWh]");

            // Zeichenreihenfolge: Kontur, Stapel von unten, Linien, zweite Achse.
            Assert.Equal(new[] { "Gesamt", "Waermepumpe", "Heizkessel", "Waermebedarf", "Puffer 1" },
                         m.Reihen.Select(r => r.Name).ToArray());

            Assert.Equal(Reihenart.Linie, m.Reihen[0].Art);
            Assert.Equal(4f, m.Reihen[0].Staerke);

            // Die erste Schicht liegt auf der Null, die zweite auf der ersten.
            Datenreihe wp = m.Reihen[1], kessel = m.Reihen[2];
            Assert.Equal(Reihenart.Flaeche, wp.Art);
            Assert.Equal(Reihenart.Flaeche, kessel.Art);
            Assert.All(wp.Unten, u => Assert.Equal(0.0, u));
            Assert.Equal(wp.Werte, kessel.Unten);
            for (int i = 0; i < 50; i++)
                Assert.Equal(wp.Werte[i] + Math.Max(stapel[1].Werte[i], 0), kessel.Werte[i], 9);

            // Die zweite Achse hat ihre EIGENE Skala; die linke bleibt die der Flaeche.
            Datenreihe puffer = m.Reihen[4];
            Assert.Equal(Reihenart.Linie, puffer.Art);
            Assert.Equal(0.0, puffer.Fenster.YVon);
            Assert.True(puffer.Fenster.YBis > m.Flaeche.Daten.YBis,
                        "die rechte Achse reicht weiter als die linke");
            Assert.Equal(m.Flaeche.Daten.XVon, puffer.Fenster.XVon);

            Assert.Contains("yachse2", Marken(m));
            Assert.Contains("legende:Puffer 1", Marken(m));
        }

        /// <summary>
        /// <b>Temperaturverlauf:</b> eine Achse OHNE Nullpunkt — das Datenfenster
        /// beginnt beim kleinsten Wert des Ausschnitts, nicht bei null. Die untere
        /// Speicherschicht trägt ihr Strichmuster mit ins Modell.
        /// </summary>
        [Fact]
        public void TemperaturverlaufModell_traegt_Flaeche_Reihen_und_Marken()
        {
            Zeichenmodell m = ChartRenderer.TemperaturverlaufModell(
                "Speichertemperaturen", Temperaturreihen(), true);

            Assert.Equal(new Rahmen(100f, 110f, 1100f, 360f), m.Flaeche.Bild);
            Assert.True(m.Flaeche.Daten.YVon > 30.0, "die Achse beginnt am kleinsten Wert");

            Assert.Equal(2, m.Reihen.Count);
            Assert.Null(m.Reihen[0].Muster);
            Assert.NotNull(m.Reihen[1].Muster);         // die untere Schicht ist gestrichelt
            Assert.All(m.Reihen, r => Assert.Equal(Reihenart.Linie, r.Art));
            Assert.All(m.Reihen, r => Assert.Equal(m.Flaeche.Daten, r.Fenster));

            Assert.Equal(new[] { "titel", "yachse", "xachse", "legende:Puffer oben",
                                 "legende:Puffer unten", "reihe:Puffer oben", "reihe:Puffer unten" }
                             .OrderBy(s => s, StringComparer.Ordinal),
                         Marken(m).OrderBy(s => s, StringComparer.Ordinal));
        }

        /// <summary>
        /// <b>Speicherbetrieb:</b> dieselbe Zeichnung mit der rechten Achse — der
        /// Ladezustand ist eine ENERGIE und bekommt sein eigenes Fenster ab null
        /// (DG-E3-1), die Achse selbst die Marke <c>yachse2</c>.
        /// </summary>
        [Fact]
        public void SpeicherbetriebModell_traegt_die_zweite_Achse()
        {
            var speicher = new ChartRenderer.Reihe("Ladezustand", Jahresreihe(400, 120, 180),
                                                   SKColors.MediumVioletRed);
            Zeichenmodell m = ChartRenderer.SpeicherbetriebModell(
                "Lastgang", Temperaturreihen(), "Leistung [kW]", speicher, "Ladezustand [kWh]");

            Assert.Equal(3, m.Reihen.Count);
            Datenreihe rechts = m.Reihen[2];
            Assert.Equal("Ladezustand", rechts.Name);
            Assert.Equal(0.0, rechts.Fenster.YVon);
            Assert.True(rechts.Fenster.YBis >= speicher.Werte.Max());
            Assert.NotEqual(m.Flaeche.Daten.YVon, rechts.Fenster.YVon);

            Assert.Contains("yachse2", Marken(m));
            Assert.Contains("reihe:Ladezustand", Marken(m));
        }

        /// <summary>Alle Marken eines Modells — auch die in Gruppen.</summary>
        private static HashSet<string> Marken(Zeichenmodell m)
        {
            var marken = new HashSet<string>(StringComparer.Ordinal);
            Sammle(m.Befehle, marken);
            return marken;
        }

        private static void Sammle(IReadOnlyList<Zeichenbefehl> befehle, HashSet<string> ziel)
        {
            foreach (Zeichenbefehl b in befehle)
            {
                if (b.Marke != null) ziel.Add(b.Marke);
                if (b is Gruppe g) Sammle(g.Befehle, ziel);
            }
        }
    }
}
