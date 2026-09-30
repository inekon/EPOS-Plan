using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Pfadregel</b> (Konzept Diagramme, Entscheid DG-Q5: „eine Konstante im
    /// Modell"): Wie viele Stuetzstellen einer Reihe in den SVG-Pfad gehen.
    ///
    /// <para>Geprueft wird dreierlei: die ROHGRENZE (bis 8 760 Stuetzstellen und drei
    /// Reihen geht der Pfad roh, darueber gebuendelt), dass die BUENDELUNG keine
    /// Spitze verliert — sie ist der ganze Grund, warum nicht einfach jeder n-te Wert
    /// genommen wird — und dass sie DETERMINISTISCH ist, ohne die kein byte-gleicher
    /// SVG-Text moeglich waere.</para>
    /// </summary>
    public class PfadregelTests
    {
        // =====================================================================
        // 1 — Die Rohgrenze
        // =====================================================================

        /// <summary>
        /// Die zwei Zahlen stehen als Konstanten da und nicht verstreut in einer
        /// Zeichenmethode: Ein Stundenjahr geht roh, eine Viertelstundenreihe nicht,
        /// und ab der vierten Reihe wird gebuendelt (Pruefstand vom 20.09.2026 —
        /// sechs rohe Reihen liessen einen Bildaufbau von 60 aus).
        /// </summary>
        [Fact]
        public void DieRohgrenzeIstEinStundenjahrUndDreiReihen()
        {
            Assert.Equal(8760, Pfadregel.ROH_BIS_STUETZSTELLEN);
            Assert.Equal(3, Pfadregel.ROH_BIS_REIHEN);

            Assert.True(Pfadregel.Roh(8760, 3));       // genau auf der Grenze
            Assert.True(Pfadregel.Roh(2, 1));
            Assert.False(Pfadregel.Roh(8761, 1));      // eine Stuetzstelle darueber
            Assert.False(Pfadregel.Roh(35040, 1));     // Viertelstundenreihe
            Assert.False(Pfadregel.Roh(8760, 4));      // die vierte Reihe
        }

        // =====================================================================
        // 2 — Die Buendelung haelt die Spitzen
        // =====================================================================

        /// <summary>
        /// Je Bildpunktspalte Minimum UND Maximum: Eine Spitze und ein Tal MITTEN in
        /// einer Spalte bleiben im Pfad, obwohl die Spalte zehn Werte zusammenfasst.
        /// Genau das verliert „jeder n-te Wert", mit dem das PNG heute zeichnet.
        /// </summary>
        [Fact]
        public void DieBuendelungVerliertKeineSpitze()
        {
            var werte = new double[100];
            for (int i = 0; i < werte.Length; i++) werte[i] = 1.0;
            werte[45] = 99.0;    // die Spitze
            werte[47] = -7.0;    // das Tal, in derselben Spalte

            IReadOnlyList<Punkt> punkte = Pfadregel.Gebuendelt(werte, 10);

            Assert.Contains(punkte, p => p.X == 45f && p.Y == 99f);
            Assert.Contains(punkte, p => p.X == 47f && p.Y == -7f);
            Assert.True(punkte.Count <= 2 * 10,
                        "hoechstens zwei Punkte je Spalte, gezaehlt: " + punkte.Count);
        }

        /// <summary>
        /// „In Indexreihenfolge" heisst: Der Pfad laeuft von links nach rechts durch,
        /// auch innerhalb einer Spalte. Liefe er zurueck, entstuende im Bild ein
        /// Zickzack ueber die Spaltenbreite statt einer senkrechten Spanne.
        /// </summary>
        [Fact]
        public void DiePunkteStehenInIndexreihenfolge()
        {
            var werte = new double[500];
            for (int i = 0; i < werte.Length; i++)
                werte[i] = Math.Sin(i * 0.37) + 0.2 * Math.Sin(i * 3.1);

            IReadOnlyList<Punkt> punkte = Pfadregel.Gebuendelt(werte, 60);

            Assert.NotEmpty(punkte);
            for (int i = 1; i < punkte.Count; i++)
                Assert.True(punkte[i].X > punkte[i - 1].X,
                            "Punkt " + i + " springt zurueck: " + punkte[i].X +
                            " nach " + punkte[i - 1].X);
        }

        /// <summary>
        /// Mehr Spalten als Werte: Jede Spalte traegt hoechstens einen Wert, und dann
        /// steht jeder Wert genau einmal im Pfad — die Buendelung ist bei 1:1 vom
        /// rohen Pfad nicht zu unterscheiden.
        /// </summary>
        [Fact]
        public void BeiMehrSpaltenAlsWertenStehtJederWertEinmal()
        {
            var werte = new double[] { 3, 1, 4, 1, 5 };

            IReadOnlyList<Punkt> punkte = Pfadregel.Gebuendelt(werte, 100);

            Assert.Equal(werte.Length, punkte.Count);
            for (int i = 0; i < werte.Length; i++)
            {
                Assert.Equal(i, punkte[i].X);
                Assert.Equal((float)werte[i], punkte[i].Y);
            }
        }

        // =====================================================================
        // 3 — Determinismus
        // =====================================================================

        /// <summary>
        /// Zweimal gebuendelt ergibt dieselbe Punktfolge. Ohne diese Zusage waere der
        /// byte-gleiche SVG-Text nicht zu halten.
        /// </summary>
        [Fact]
        public void ZweimalGebuendeltErgibtDieselbenPunkte()
        {
            var werte = new double[8760];
            for (int i = 0; i < werte.Length; i++)
                werte[i] = 20 + 15 * Math.Sin(2 * Math.PI * i / 8760.0) + Math.Sin(i * 0.9);

            IReadOnlyList<Punkt> a = Pfadregel.Gebuendelt(werte, 1154);
            IReadOnlyList<Punkt> b = Pfadregel.Gebuendelt(werte, 1154);

            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) Assert.Equal(a[i], b[i]);
        }

        /// <summary>Eine leere oder fehlende Reihe gibt keine Punkte statt einer Ausnahme.</summary>
        [Fact]
        public void OhneWerteGibtEsKeinePunkte()
        {
            Assert.Empty(Pfadregel.Gebuendelt(null, 10));
            Assert.Empty(Pfadregel.Gebuendelt(new double[0], 10));
            Assert.Single(Pfadregel.Gebuendelt(new double[] { 7.0 }, 0));   // Spalten < 1

            Assert.Empty(Pfadregel.GebuendelteKante(null, 10, true));
            Assert.Empty(Pfadregel.GebuendelteKante(new double[0], 10, false));
            Assert.Single(Pfadregel.GebuendelteKante(new double[] { 7.0 }, 0, true));
        }

        // =====================================================================
        // Die Flaechenbuendelung (Entscheid DG-E3-2)
        // =====================================================================

        /// <summary>
        /// <b>Je Bildpunktspalte GENAU EIN Punkt</b> — der Hoechstwert fuer die
        /// Oberkante, der Kleinstwert fuer die Unterkante. Zwei Punkte je Spalte
        /// ergaeben an der Oberkante ein Saegeblatt, das Flaeche wegnaehme, die im
        /// Bild steht.
        /// </summary>
        [Fact]
        public void DieKanteTraegtEinenPunktJeSpalte()
        {
            var werte = new double[] { 1, 9, 2, 8, 3, 7 };

            IReadOnlyList<Punkt> oben = Pfadregel.GebuendelteKante(werte, 3, true);
            IReadOnlyList<Punkt> unten = Pfadregel.GebuendelteKante(werte, 3, false);

            Assert.Equal(3, oben.Count);
            Assert.Equal(3, unten.Count);
            // Spalten (1,9) (2,8) (3,7): Hoechstwerte an Index 1, 3, 5.
            Assert.Equal(new[] { 1f, 3f, 5f }, oben.Select(p => p.X).ToArray());
            Assert.Equal(new[] { 9f, 8f, 7f }, oben.Select(p => p.Y).ToArray());
            // Kleinstwerte an Index 0, 2, 4.
            Assert.Equal(new[] { 0f, 2f, 4f }, unten.Select(p => p.X).ToArray());
            Assert.Equal(new[] { 1f, 2f, 3f }, unten.Select(p => p.Y).ToArray());
        }

        /// <summary>
        /// <b>Die Huelle ist KONSERVATIV:</b> Sie liegt nie unter dem Hoechstwert und
        /// nie ueber dem Kleinstwert der Spalte — die gebuendelte Flaeche ist damit nie
        /// kleiner als die rohe, und keine Spitze faellt in sie hinein.
        /// </summary>
        [Fact]
        public void DieHuelleVerliertKeineSpitze()
        {
            var werte = new double[8760];
            for (int i = 0; i < werte.Length; i++)
                werte[i] = 20 + 15 * Math.Sin(2 * Math.PI * i / 8760.0);
            werte[4321] = 300;      // die eine Spitze
            werte[4322] = -80;      // und der eine Einbruch

            IReadOnlyList<Punkt> oben = Pfadregel.GebuendelteKante(werte, 1100, true);
            IReadOnlyList<Punkt> unten = Pfadregel.GebuendelteKante(werte, 1100, false);

            Assert.Contains(oben, p => p.Y == 300f);
            Assert.Contains(unten, p => p.Y == -80f);
            Assert.Equal(werte.Max(), oben.Max(p => p.Y));
            Assert.Equal(werte.Min(), unten.Min(p => p.Y));
        }

        /// <summary>
        /// Deterministisch und kulturfrei — dieselbe Bedingung wie fuer
        /// <see cref="Pfadregel.Gebuendelt"/>: Ohne sie waere der byte-gleiche
        /// SVG-Text nicht zu halten.
        /// </summary>
        [Fact]
        public void ZweimalGebuendelteKanteErgibtDieselbenPunkte()
        {
            var werte = new double[35040];
            for (int i = 0; i < werte.Length; i++)
                werte[i] = 20 + 15 * Math.Sin(2 * Math.PI * i / 35040.0) + Math.Sin(i * 0.9);

            IReadOnlyList<Punkt> a = Pfadregel.GebuendelteKante(werte, 1100, true);
            IReadOnlyList<Punkt> b = Pfadregel.GebuendelteKante(werte, 1100, true);

            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) Assert.Equal(a[i], b[i]);
        }

        /// <summary>
        /// Stehen mehr Spalten als Werte zur Verfuegung, traegt jede belegte Spalte
        /// genau einen Wert — und es gibt nicht mehr Punkte als Werte.
        /// </summary>
        [Fact]
        public void BeiMehrSpaltenAlsWertenTraegtDieKanteJedenWertEinmal()
        {
            var werte = new double[] { 4, 1, 7 };

            IReadOnlyList<Punkt> oben = Pfadregel.GebuendelteKante(werte, 50, true);

            Assert.Equal(3, oben.Count);
            Assert.Equal(new[] { 4f, 1f, 7f }, oben.Select(p => p.Y).ToArray());
        }

        // =====================================================================
        // 4 — Die Stufenregel des Stapels
        // =====================================================================

        /// <summary>Eine Stapelschicht (Flaeche mit Unterkante) aus n Werten.</summary>
        private static Datenreihe Schicht(int n)
            => new Datenreihe("S", Farbton.Aus(Farbrolle.WAERME_WP), 0f, null, new double[n],
                              null, Reihenart.Flaeche, new double[n]);

        /// <summary>
        /// <b>Im Jahresbild ist die Stufe der TAG</b>: 8 760 Stunden auf 1 100 Spalten sind
        /// rund drei Spalten je Tag. Je Spalte gebuendelt, wechselten Nachbarspalten zwischen
        /// Tagesspitze und Tagestief, und der Stapel stuende als Streifen da. Die Stufen
        /// schliessen lueckenlos von der ersten bis zur letzten Stuetzstelle aneinander.
        /// </summary>
        [Fact]
        public void ImJahresbildIstDieStufeDerTag()
        {
            IReadOnlyList<Stufe> stufen = Pfadregel.Stufen(8760, 0, 8760, 1100);

            Assert.Equal(365, stufen.Count);
            Assert.All(stufen, s => Assert.Equal(23, s.Bis - s.Von));
            Assert.Equal(0.0, stufen[0].Links);
            Assert.Equal(1.0, stufen[364].Rechts);
            for (int s = 1; s < stufen.Count; s++)
            {
                Assert.Equal(stufen[s - 1].Bis + 1, stufen[s].Von);
                Assert.Equal(stufen[s - 1].Rechts, stufen[s].Links);
            }

            // Dasselbe fuer ein Viertelstundenjahr: 96 Werte je Tag.
            IReadOnlyList<Stufe> viertel = Pfadregel.Stufen(35040, 0, 35040, 1100);
            Assert.Equal(365, viertel.Count);
            Assert.All(viertel, s => Assert.Equal(95, s.Bis - s.Von));
        }

        /// <summary>
        /// <b>Ab <c>TAG_MIN_SPALTEN</c> Spalten je Tag ist die Stufe die Bildpunktspalte</b>
        /// (etwa ab dem vierfachen Zoom): Dann zeichnet sie den Tagesgang als Kurve ueber
        /// mehrere Stufen. Eine Reihe ohne festes Jahresraster buendelt immer je Spalte.
        /// </summary>
        [Fact]
        public void AbVierSpaltenJeTagIstDieStufeDieSpalte()
        {
            // 2 191 Stunden auf 1 100 Spalten: 12 Spalten je Tag.
            IReadOnlyList<Stufe> gezoomt = Pfadregel.Stufen(8760, 500, 2191, 1100);
            Assert.Equal(1100, gezoomt.Count);
            Assert.All(gezoomt, s => Assert.InRange(s.Bis - s.Von + 1, 1, 3));

            IReadOnlyList<Stufe> frei = Pfadregel.Stufen(20000, 0, 20000, 100);
            Assert.Equal(100, frei.Count);
            Assert.Equal(20000, frei.Sum(s => s.Bis - s.Von + 1));

            Assert.Equal(4, Pfadregel.TAG_MIN_SPALTEN);
            Assert.Equal(24, Pfadregel.WerteJeTag(8760));
            Assert.Equal(96, Pfadregel.WerteJeTag(35040));
            Assert.Equal(0, Pfadregel.WerteJeTag(8761));
        }

        /// <summary>
        /// <b>Die Tage stehen auf dem Jahresanfang, nicht auf dem Ausschnitt</b>: Beginnt ein
        /// Ausschnitt um 6 Uhr, endet seine erste Stufe um Mitternacht.
        /// </summary>
        [Fact]
        public void DieTageStehenAufDemJahresanfang()
        {
            IReadOnlyList<Stufe> stufen = Pfadregel.Stufen(8760, 30, 8000, 400);

            Assert.Equal(0, stufen[0].Von);
            Assert.Equal(17, stufen[0].Bis);          // Stunden 30 … 47
            Assert.Equal(18, stufen[1].Von);          // Stunde 48 = Tag 2, 0 Uhr
            Assert.Equal(18.0 / 7999, stufen[1].Links, 12);
        }

        /// <summary>
        /// <b>Spitzenstunden, Stundenwerte und Treppe</b>: je Stufe die Stunde des größten
        /// endlichen Bezugswerts — bei Gleichstand die erste, ohne endlichen Wert die erste
        /// der Stufe —, die Werte einer Reihe in genau diesen Stunden (nicht endlich = Lücke), und
        /// die Treppe steht je Stufe waagrecht und springt an der Grenze senkrecht — gleiche
        /// Nachbarn ergeben keinen Zwischenpunkt.
        /// </summary>
        [Fact]
        public void SpitzenstundenStundenwerteUndTreppe()
        {
            var bezug = new double[] { 1, 5, 2, double.NaN, 3, 3, double.NaN, double.NaN };
            var werte = new double[] { 10, 20, 30, 40, 50, double.NaN, 70, 80 };
            var stufen = new List<Stufe>
            {
                new Stufe(0, 1, 0.00, 0.25), new Stufe(2, 3, 0.25, 0.50),
                new Stufe(4, 5, 0.50, 0.75), new Stufe(6, 7, 0.75, 1.00)
            };

            int[] stunden = Pfadregel.Spitzenstunden(bezug, stufen);
            // Stufe 1: die 5; Stufe 2: die 2 (NaN zählt nicht); Stufe 3: Gleichstand 3/3 → die
            // erste; Stufe 4: kein endlicher Bezugswert → die erste Stunde der Stufe.
            Assert.Equal(new[] { 1, 2, 4, 6 }, stunden);

            double[] stufenwerte = Pfadregel.Stundenwerte(werte, stunden);
            Assert.Equal(new[] { 20.0, 30.0, 50.0, 70.0 }, stufenwerte);
            Assert.True(double.IsNaN(Pfadregel.Stundenwerte(werte, new[] { 5 })[0]));  // NaN bleibt Lücke

            IReadOnlyList<(double Anteil, double Wert)> treppe = Pfadregel.Treppe(stufen, stufenwerte);
            Assert.Equal(new (double, double)[]
            {
                (0.00, 20.0), (0.25, 20.0), (0.25, 30.0), (0.50, 30.0), (0.50, 50.0),
                (0.75, 50.0), (0.75, 70.0), (1.00, 70.0)
            }, treppe.ToArray());

            // Gleiche Nachbarn: eine einzige waagrechte Kante.
            Assert.Equal(new (double, double)[] { (0.0, 4.0), (1.0, 4.0) },
                         Pfadregel.Treppe(stufen, new[] { 4.0, 4.0, 4.0, 4.0 }).ToArray());
        }

        /// <summary>
        /// <b>Eine Lücke bleibt Lücke</b> (Kopf der Stufenregel): Die Spitzenstunde wählt nur
        /// unter endlichen Bezugswerten; ist die Reihe in dieser Stunde nicht endlich, ist die
        /// Stufe für sie eine Lücke. Die Stücke ohne Lücke nehmen Ober- und Unterkante
        /// zusammen — eine Schicht bricht an beiden Kanten in denselben Stufen ab —, und die
        /// Treppe eines Stücks reicht genau von seiner ersten bis zu seiner letzten Stufe:
        /// keine Null in der Lücke, keine Nachbarstufe hinein.
        /// </summary>
        [Fact]
        public void EineLueckeBleibtLuecke()
        {
            var stufen = new List<Stufe>
            {
                new Stufe(0, 1, 0.0, 0.2), new Stufe(2, 3, 0.2, 0.4), new Stufe(4, 5, 0.4, 0.6),
                new Stufe(6, 7, 0.6, 0.8), new Stufe(8, 9, 0.8, 1.0)
            };
            // Der Bezug ist in Stufe 2 ganz Lücke → dort die erste Stunde (4).
            var bezug = new double[] { 1, 9, 9, 1, double.NaN, double.NaN, 1, 9, 9, 1 };
            // Die Oberkante: in Stufe 2 aus; die Unterkante: in Stunde 7 (Spitze der Stufe 3) aus.
            var oben = new double[] { 5, 6, 7, 5, double.NaN, double.NaN, 6, 8, 9, 5 };
            var unten = new double[] { 1, 2, 3, 1, double.NaN, double.NaN, 2, double.NaN, 4, 1 };

            int[] stunden = Pfadregel.Spitzenstunden(bezug, stufen);
            Assert.Equal(new[] { 1, 2, 4, 7, 8 }, stunden);

            double[] o = Pfadregel.Stundenwerte(oben, stunden);
            double[] u = Pfadregel.Stundenwerte(unten, stunden);
            Assert.True(double.IsNaN(o[2]) && double.IsNaN(u[2]) && double.IsNaN(u[3]));

            // Die Oberkante allein (eine Linie): zwei Stücke um die Stufe 2.
            Assert.Equal(new[] { (0, 1), (3, 4) }, Pfadregel.Stufenstuecke(o).ToArray());
            // Die Schicht: auch Stufe 3 fällt aus, weil ihre Unterkante dort Lücke ist.
            Assert.Equal(new[] { (0, 1), (4, 4) }, Pfadregel.Stufenstuecke(o, u).ToArray());
            // Ohne Lücke: ein einziges Stück über alle Stufen.
            Assert.Equal(new[] { (0, 4) }, Pfadregel.Stufenstuecke(new[] { 1.0, 2, 3, 4, 5 }).ToArray());
            Assert.Empty(Pfadregel.Stufenstuecke(new[] { double.NaN, double.NaN }));

            // Die Treppe eines Stücks: nur seine Stufen, kein NaN, keine Null.
            Assert.Equal(new (double, double)[] { (0.0, 6.0), (0.2, 6.0), (0.2, 7.0), (0.4, 7.0) },
                         Pfadregel.Treppe(stufen, o, 0, 1).ToArray());
            Assert.Equal(new (double, double)[] { (0.6, 8.0), (0.8, 8.0), (0.8, 9.0), (1.0, 9.0) },
                         Pfadregel.Treppe(stufen, o, 3, 4).ToArray());
            // Das ganze Stück ist wörtlich die Treppe über alle Stufen.
            double[] voll = { 1, 2, 3, 4, 5 };
            Assert.Equal(Pfadregel.Treppe(stufen, voll).ToArray(), Pfadregel.Treppe(stufen, voll, 0, 4).ToArray());
        }

        /// <summary>
        /// <b>Die Schichten summieren sich in jeder Stufe zur Oberkante</b> — der Kern der
        /// Regel: Alle Kanten eines Stapels nehmen die Werte DERSELBEN Stunde, also ist die
        /// Dicke jeder Schicht ihr eigener Wert in dieser Stunde, und die Oberkante ist die
        /// Bezugsgröße. Die Gegenprobe zeigt den Fehler der Höchstwerte je Kante: Ein taktender
        /// Erzeuger (30 kW zwei Stunden am Tag) stünde dort Tag für Tag als 30-kW-Band da, auch
        /// über dem Bedarf.
        /// </summary>
        [Fact]
        public void DieSchichtenSummierenSichInJederStufeZurOberkante()
        {
            const int tage = 30;
            var kessel = new double[tage * 24];
            var puffer = new double[tage * 24];
            var bedarf = new double[tage * 24];
            for (int t = 0; t < kessel.Length; t++)
            {
                int stunde = t % 24;
                bedarf[t] = stunde == 18 ? 20.0 + t / 24 % 5 : 8.0;
                kessel[t] = stunde == 3 || stunde == 4 ? 30.0 : Math.Max(0.0, bedarf[t] - 12.0);
                puffer[t] = stunde == 3 || stunde == 4 ? 0.0 : Math.Min(bedarf[t], 12.0);
            }
            var oben1 = new double[kessel.Length];
            var oben2 = new double[kessel.Length];
            for (int t = 0; t < kessel.Length; t++) { oben1[t] = kessel[t]; oben2[t] = oben1[t] + puffer[t]; }

            // 720 Stunden auf 100 Spalten: sieben Spalten je Tag - also je Spalte.
            // Mit 8760 Stunden als ganzer Reihe und 20 Spalten: je Tag.
            IReadOnlyList<Stufe> stufen = Pfadregel.Stufen(8760, 0, kessel.Length, 20);
            Assert.Equal(tage, stufen.Count);
            int[] stunden = Pfadregel.Spitzenstunden(bedarf, stufen);

            double[] k = Pfadregel.Stundenwerte(oben1, stunden);
            double[] kp = Pfadregel.Stundenwerte(oben2, stunden);
            double[] b = Pfadregel.Stundenwerte(bedarf, stunden);
            for (int s = 0; s < stufen.Count; s++)
            {
                Assert.Equal(s * 24 + 18, stunden[s]);                     // die Spitze des Bedarfs
                Assert.Equal(kessel[stunden[s]], k[s], 12);                // Dicke = eigener Wert
                Assert.Equal(puffer[stunden[s]], kp[s] - k[s], 12);
                Assert.Equal(b[s], kp[s], 12);                             // Oberkante = Bedarf
                Assert.True(k[s] < 30.0, "kein Band auf Nennleistung, Stufe " + s);
            }

            // Die Gegenprobe: je Kante der eigene Höchstwert - der Kessel stünde auf 30 kW,
            // über dem Bedarf der Stufe.
            int[] eigene = Pfadregel.Spitzenstunden(oben1, stufen);
            double[] falsch = Pfadregel.Stundenwerte(oben1, eigene);
            for (int s = 0; s < stufen.Count; s++)
                Assert.True(falsch[s] > b[s], "die Gegenprobe greift nicht, Stufe " + s);
        }

        /// <summary>
        /// <b>Tagesstufen und Zeitpunkt</b>: Die Stufen stehen auf Tagen, solange ein Tag
        /// schmaler als vier Spalten ist und die Reihe ein Jahresraster führt; der Zeitpunkt
        /// eines Index nennt Datum und Uhrzeit im Jahr ohne Schaltjahr — für Stunden und
        /// Viertelstunden, sonst nichts.
        /// </summary>
        [Fact]
        public void TagesstufenUndZeitpunkt()
        {
            Assert.True(Pfadregel.TagesStufen(8760, 8760, 1100));
            Assert.False(Pfadregel.TagesStufen(8760, 2191, 1100));   // 4-fach gezoomt: je Spalte
            Assert.True(Pfadregel.TagesStufen(35040, 35040, 1100));
            Assert.False(Pfadregel.TagesStufen(20000, 20000, 100));  // kein Jahresraster
            Assert.False(Pfadregel.TagesStufen(8760, 1, 1100));

            Assert.Equal(new DateTime(2001, 1, 1, 0, 0, 0), Pfadregel.Zeitpunkt(0, 8760));
            Assert.Equal(new DateTime(2001, 1, 14, 18, 0, 0), Pfadregel.Zeitpunkt(13 * 24 + 18, 8760));
            Assert.Equal(new DateTime(2001, 3, 1, 0, 0, 0), Pfadregel.Zeitpunkt(59 * 24, 8760));   // kein 29. Februar
            Assert.Equal(new DateTime(2001, 12, 31, 23, 0, 0), Pfadregel.Zeitpunkt(8759, 8760));
            Assert.Equal(new DateTime(2001, 1, 1, 1, 15, 0), Pfadregel.Zeitpunkt(5, 35040));
            Assert.Equal(new DateTime(2001, 12, 31, 23, 45, 0), Pfadregel.Zeitpunkt(35039, 35040));
            Assert.Null(Pfadregel.Zeitpunkt(8760, 8760));
            Assert.Null(Pfadregel.Zeitpunkt(-1, 8760));
            Assert.Null(Pfadregel.Zeitpunkt(10, 500));
        }

        /// <summary>
        /// <b>Welche Reihe in Stufen geht</b>: eine Stapelschicht oder eine Hüllkurve mit
        /// mehr Werten als Spalten — auch unter der Rohgrenze. Ihr Vollpfad ist dann nie
        /// roh, und die Oberflaeche rechnet ihn beim Zoom nach; eine gewoehnliche Linie und
        /// eine Punktwolke bleiben, wie sie waren.
        /// </summary>
        [Fact]
        public void StapelschichtUndHuelleGehenInStufen()
        {
            Datenreihe schicht = Schicht(8760);
            Datenreihe linie = new Datenreihe("L", Farbton.Aus(Farbrolle.BEDARF), 2f, null, new double[8760]);
            Datenreihe huelle = linie with { Huelle = true };
            Datenreihe punkte = linie with { Art = Reihenart.Punkte, XWerte = new double[8760] };

            Assert.True(Pfadregel.Spaltenweise(schicht, 8760, 1100));
            Assert.False(Pfadregel.Spaltenweise(schicht, 1000, 1100));      // nicht dicht: jede Stunde
            Assert.True(Pfadregel.Spaltenweise(huelle, 8760, 1100));
            Assert.False(Pfadregel.Spaltenweise(linie, 8760, 1100));

            Assert.False(Pfadregel.VollpfadRoh(schicht, 3, 1100));
            Assert.False(Pfadregel.VollpfadRoh(huelle, 3, 1100));
            Assert.True(Pfadregel.VollpfadRoh(linie, 3, 1100));
            Assert.False(Pfadregel.VollpfadRoh(linie, 4, 1100));
            Assert.True(Pfadregel.VollpfadRoh(punkte, 9, 1100));
        }
    }
}
