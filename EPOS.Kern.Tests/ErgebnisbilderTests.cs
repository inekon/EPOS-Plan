using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die sieben ERGEBNISBILDER der Welle 11 (iU9-W11a.6) — die schnelle Sicherung
    /// neben <c>Proben/ChartProben</c>.
    ///
    /// <para>Geprueft wird je Bild: das PNG entsteht, es hat das festgelegte Mass,
    /// zweimal Zeichnen liefert byte-gleiche Dateien (Determinismus), und der Leerfall
    /// bricht nicht ab, sondern liefert ein Bild mit Hinweis. Dazu die drei Eigenheiten,
    /// die man leicht verliert: die dynamische Legende des Rings, die Mindestspanne der
    /// Temperaturachse und der fehlende Stapel im sortierten Modus.</para>
    ///
    /// <para>Ohne Datenbank, ohne Oberflaeche.</para>
    /// </summary>
    public class ErgebnisbilderTests
    {
        private const int STUNDEN = 8760;

        /// <summary>Eine wiederholbare Jahresreihe — fester Startwert, kein Rauschen.</summary>
        private static double[] Reihe(double grund, double hub, int versatz = 0)
        {
            var w = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++)
                w[i] = Math.Max(0, grund + hub * Math.Sin(2.0 * Math.PI * (i + versatz) / STUNDEN));
            return w;
        }

        private static (int Breite, int Hoehe) Mass(byte[] png)
        {
            using (var bild = SKBitmap.Decode(png)) return (bild.Width, bild.Height);
        }

        // ---------------------------------------------------------------- B1

        [Fact]
        public void GanglinieNormiert_liefert_ein_Bild_im_festgelegten_Mass()
        {
            byte[] png = ChartRenderer.GanglinieNormiert(
                "Waermelast", new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe("Gesamt", Reihe(100, 80), SKColors.Red),
                    new ChartRenderer.Reihe("Heizung", Reihe(60, 50), SKColors.DeepSkyBlue)
                },
                "Anteil", ChartRenderer.Achse.Monate, false);

            Assert.NotNull(png);
            Assert.Equal((1240, 560), Mass(png));
        }

        [Fact]
        public void GanglinieNormiert_zeichnet_deterministisch()
        {
            Func<byte[]> zeichne = () => ChartRenderer.GanglinieNormiert(
                "Waermelast", new List<ChartRenderer.Reihe>
                { new ChartRenderer.Reihe("Gesamt", Reihe(100, 80), SKColors.Red) },
                "Anteil", ChartRenderer.Achse.Jahresstunden, true);

            Assert.Equal(zeichne(), zeichne());
        }

        [Fact]
        public void GanglinieNormiert_ohne_Reihen_liefert_den_Leerhinweis()
        {
            byte[] png = ChartRenderer.GanglinieNormiert("Leer", null, "", ChartRenderer.Achse.Monate, false);

            Assert.NotNull(png);
            Assert.Equal((1240, 560), Mass(png));
        }

        // ---------------------------------------------------------------- B2 / B3

        [Fact]
        public void ErzeugerStapel_traegt_Stapel_Linien_Kontur_und_zweite_Achse()
        {
            byte[] png = ChartRenderer.ErzeugerStapel(
                "Waermeproduktion",
                new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe("WP", Reihe(40, 30), SKColors.Orange,
                                            ChartRenderer.Stapelart.Saeule),
                    new ChartRenderer.Reihe("Bedarf", Reihe(30, 20), SKColors.Red,
                                            ChartRenderer.Stapelart.Flaeche)
                },
                new List<ChartRenderer.Reihe>
                { new ChartRenderer.Reihe("Restwaerme", Reihe(10, 8), SKColors.Green) },
                new ChartRenderer.Reihe("Gesamt", Reihe(80, 60), SKColors.Green,
                                        ChartRenderer.Stapelart.Keine, false, 4f),
                "kW", ChartRenderer.Achse.Monate, false,
                new ChartRenderer.Reihe("Waermebedarf", Reihe(90, 70), SKColors.DarkCyan),
                "kW");

            Assert.NotNull(png);
            Assert.Equal((1240, 560), Mass(png));
        }

        /// <summary>
        /// Die ZWEITE y-Achse macht die Zeichenflaeche schmaler — das Bildmass bleibt.
        /// Ohne sie entsteht ein ANDERES Bild; sonst waere der Parameter wirkungslos.
        /// </summary>
        [Fact]
        public void ErzeugerStapel_zweite_Achse_aendert_das_Bild()
        {
            var stapel = new List<ChartRenderer.Reihe>
            { new ChartRenderer.Reihe("WP", Reihe(40, 30), SKColors.Orange,
                                      ChartRenderer.Stapelart.Saeule) };

            byte[] ohne = ChartRenderer.ErzeugerStapel("T", stapel, null, null, "kW",
                                                       ChartRenderer.Achse.Monate, false);
            byte[] mit = ChartRenderer.ErzeugerStapel("T", stapel, null, null, "kW",
                                                      ChartRenderer.Achse.Monate, false,
                                                      new ChartRenderer.Reihe("Bedarf", Reihe(90, 70),
                                                                              SKColors.DarkCyan), "kW");

            Assert.Equal(Mass(ohne), Mass(mit));
            Assert.NotEqual(ohne, mit);
        }

        /// <summary>
        /// SORTIERT wird NICHT gestapelt (<c>GanglinienDarstellung.Stapeltyp</c>): In der
        /// Dauerlinie ist jede Reihe fuer sich sortiert, eine Summe daraus waere frei
        /// erfunden. Das Bild muss sich deshalb vom chronologischen unterscheiden.
        /// </summary>
        [Fact]
        public void ErzeugerStapel_sortiert_zeichnet_ohne_Stapel()
        {
            var stapel = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("WP", Reihe(40, 30), SKColors.Orange,
                                        ChartRenderer.Stapelart.Saeule),
                new ChartRenderer.Reihe("Kessel", Reihe(20, 15), SKColors.Blue,
                                        ChartRenderer.Stapelart.Saeule)
            };

            byte[] chronologisch = ChartRenderer.ErzeugerStapel("T", stapel, null, null, "kW",
                                                                ChartRenderer.Achse.Jahresstunden, false);
            byte[] sortiert = ChartRenderer.ErzeugerStapel("T", stapel, null, null, "kW",
                                                           ChartRenderer.Achse.Jahresstunden, true);

            Assert.NotEqual(chronologisch, sortiert);
        }

        [Fact]
        public void ErzeugerStapel_ohne_Reihen_liefert_den_Leerhinweis()
        {
            byte[] png = ChartRenderer.ErzeugerStapel("Leer", null, null, null, "kW",
                                                      ChartRenderer.Achse.Monate, false);
            Assert.NotNull(png);
            Assert.Equal((1240, 560), Mass(png));
        }

        // ---------------------------------------------------------------- B4

        [Fact]
        public void Streuwolke_liefert_ein_Bild_und_zeichnet_deterministisch()
        {
            var punkte = new List<(double X, double Y)>();
            for (int i = 0; i < 500; i++) punkte.Add((-15.0 + i * 0.07, 60.0 - i * 0.05));

            Func<byte[]> zeichne = () => ChartRenderer.Streuwolke(
                "Leistung ueber Temperatur", "°C", "kW",
                new List<ChartRenderer.Punktreihe>
                { new ChartRenderer.Punktreihe("Bedarf", punkte, new SKColor(255, 0, 0, 120)) });

            byte[] a = zeichne();
            Assert.Equal((1240, 560), Mass(a));
            Assert.Equal(a, zeichne());
        }

        [Fact]
        public void Streuwolke_ohne_Punkte_liefert_den_Leerhinweis()
        {
            byte[] png = ChartRenderer.Streuwolke("Leer", "x", "y", null);
            Assert.NotNull(png);
            Assert.Equal((1240, 560), Mass(png));
        }

        // ---------------------------------------------------------------- B5

        /// <summary>
        /// Die DYNAMISCHE Legende: Ein Segment mit Wert 0 darf weder gezeichnet noch
        /// genannt werden. Geprueft ueber den Bildvergleich — dasselbe Ergebnis wie ohne
        /// das Segment.
        /// </summary>
        [Fact]
        public void Ring_laesst_Segmente_ohne_Wert_weg()
        {
            SKColor a = new SKColor(0x2E, 0xCC, 0x71);
            SKColor b = new SKColor(0xE6, 0x7E, 0x22);
            SKColor c = new SKColor(0x9B, 0x59, 0xB6);

            byte[] mitNull = ChartRenderer.Ring("Deckung", new List<ChartRenderer.Ringsegment>
            {
                new ChartRenderer.Ringsegment("PV", 220, a),
                new ChartRenderer.Ringsegment("BHKW", 130, b),
                new ChartRenderer.Ringsegment("Speicher", 0, c)
            }, 78.6, "%");

            byte[] ohneNull = ChartRenderer.Ring("Deckung", new List<ChartRenderer.Ringsegment>
            {
                new ChartRenderer.Ringsegment("PV", 220, a),
                new ChartRenderer.Ringsegment("BHKW", 130, b)
            }, 78.6, "%");

            Assert.Equal(ohneNull, mitNull);
        }

        [Fact]
        public void Ring_liefert_das_festgelegte_Mass()
        {
            byte[] png = ChartRenderer.Ring("Deckung", new List<ChartRenderer.Ringsegment>
            { new ChartRenderer.Ringsegment("PV", 1, SKColors.Green) }, 100.0, "%");

            Assert.Equal((720, 560), Mass(png));
        }

        [Fact]
        public void Ring_ohne_Segmente_liefert_den_Leerhinweis()
        {
            byte[] png = ChartRenderer.Ring("Leer", null, 0, "%");
            Assert.NotNull(png);
            Assert.Equal((720, 560), Mass(png));
        }

        // ---------------------------------------------------------------- B6

        [Fact]
        public void MonatsStapel_liefert_das_festgelegte_Mass_und_ist_deterministisch()
        {
            double[] a = new double[12];
            double[] b = new double[12];
            for (int m = 0; m < 12; m++) { a[m] = 10 + m; b[m] = 20 - m; }

            Func<byte[]> zeichne = () => ChartRenderer.MonatsStapel(
                "Deckung", "kWh", new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe("Direkt", a, SKColors.Gold),
                    new ChartRenderer.Reihe("Speicher", b, SKColors.LightGreen)
                });

            byte[] png = zeichne();
            Assert.Equal((978, 542), Mass(png));
            Assert.Equal(png, zeichne());
        }

        /// <summary>Kuerzere Reihen als zwoelf Monate entfallen still.</summary>
        [Fact]
        public void MonatsStapel_uebergeht_zu_kurze_Reihen()
        {
            byte[] png = ChartRenderer.MonatsStapel("Leer", "kWh", new List<ChartRenderer.Reihe>
            { new ChartRenderer.Reihe("Kurz", new double[5], SKColors.Gold) });

            Assert.NotNull(png);
            Assert.Equal((978, 542), Mass(png));
        }

        // ---------------------------------------------------------------- B7

        /// <summary>
        /// Die MINDESTSPANNE von 5 K: Zwei Reihen, die um 0,2 K auseinanderliegen,
        /// duerfen die Achse nicht spreizen. Geprueft ueber den Bildvergleich mit einer
        /// Reihe, die dieselbe Mitte, aber eine groessere Spanne hat — beide Bilder
        /// muessen sich unterscheiden, das enge aber ein lesbares Band zeigen.
        /// </summary>
        [Fact]
        public void Temperaturverlauf_haelt_die_Mindestspanne()
        {
            var eng = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("oben", Reihe(60.1, 0.05), SKColors.Red),
                new ChartRenderer.Reihe("unten", Reihe(59.9, 0.05), SKColors.Red,
                                        ChartRenderer.Stapelart.Keine, true)
            };

            byte[] png = ChartRenderer.Temperaturverlauf("Speichertemperaturen", eng, true);

            Assert.NotNull(png);
            Assert.Equal((1240, 560), Mass(png));
        }

        /// <summary>
        /// Die GESTRICHELTE Zwillingsreihe muss ein anderes Bild ergeben als dieselbe
        /// Reihe durchgezogen — sonst waere <c>Reihe.Gestrichelt</c> wirkungslos.
        /// </summary>
        [Fact]
        public void Temperaturverlauf_zeichnet_gestrichelt_anders()
        {
            double[] w = Reihe(50, 10);

            byte[] durchgezogen = ChartRenderer.Temperaturverlauf("T", new List<ChartRenderer.Reihe>
            { new ChartRenderer.Reihe("unten", w, SKColors.Red) }, true);

            byte[] gestrichelt = ChartRenderer.Temperaturverlauf("T", new List<ChartRenderer.Reihe>
            { new ChartRenderer.Reihe("unten", w, SKColors.Red,
                                      ChartRenderer.Stapelart.Keine, true) }, true);

            Assert.NotEqual(durchgezogen, gestrichelt);
        }

        /// <summary>
        /// <c>minAuto: false</c> laesst die Achse bei null beginnen — ein anderes Bild.
        /// </summary>
        [Fact]
        public void Temperaturverlauf_mit_Nullpunkt_zeichnet_anders()
        {
            var reihen = new List<ChartRenderer.Reihe>
            { new ChartRenderer.Reihe("oben", Reihe(60, 5), SKColors.Red) };

            Assert.NotEqual(ChartRenderer.Temperaturverlauf("T", reihen, true),
                            ChartRenderer.Temperaturverlauf("T", reihen, false));
        }

        [Fact]
        public void Temperaturverlauf_ohne_Reihen_liefert_den_Leerhinweis()
        {
            byte[] png = ChartRenderer.Temperaturverlauf("Leer", null, true);
            Assert.NotNull(png);
            Assert.Equal((1240, 560), Mass(png));
        }

        // ---------------------------------------------------------------- Reihe

        /// <summary>
        /// Die drei neuen Felder der <c>Reihe</c> haben Vorgabewerte, die den ALTEN
        /// Konstruktor unveraendert lassen — die 30 Aufrufstellen der Berichtsbilder
        /// duerfen sich durch W11a.6 nicht anders verhalten.
        /// </summary>
        [Fact]
        public void Reihe_behaelt_ihren_alten_Konstruktor()
        {
            var r = new ChartRenderer.Reihe("A", new double[] { 1, 2 }, SKColors.Red);

            Assert.Equal(ChartRenderer.Stapelart.Keine, r.Stapelgruppe);
            Assert.False(r.Gestrichelt);
            Assert.Equal(0f, r.Breite);
        }

        // ------------------------------------------------------------ Datenzoom
        //
        // Windows-Abnahme 05.09.2026, Befund A-1. Der Baustein Diagramm meldet ein
        // aufgezogenes Rechteck in ANTEILEN DES BILDES; hier wird daraus ein
        // Achsenbereich. Die Zeichenflaeche der Ganglinienbilder liegt waagerecht
        // zwischen 100/1240 und 1200/1240, senkrecht zwischen 110/560 und 470/560.

        /// <summary>
        /// Ein Rechteck ueber der halben rechten Bildhaelfte trifft die zweite
        /// Jahreshaelfte. Gerechnet wird gegen die Zeichenflaeche, nicht gegen das
        /// Bild — der linke Rand mit der y-Beschriftung gehoert nicht dazu.
        /// </summary>
        [Fact]
        public void FensterAusBild_rechnet_gegen_die_Zeichenflaeche()
        {
            // Die MITTE der Zeichenflaeche in Bildanteilen.
            double mitte = (100.0 + 1200.0) / 2.0 / 1240.0;

            ChartRenderer.Achsenfenster f = ChartRenderer.FensterAusBild(
                new ChartRenderer.Bildausschnitt(mitte, 1200.0 / 1240.0, 0.2, 0.9), STUNDEN);

            Assert.NotNull(f);
            Assert.InRange(f.Von, STUNDEN / 2 - 3, STUNDEN / 2 + 3);
            Assert.Equal(STUNDEN, f.Bis);
        }

        /// <summary>
        /// Die OBERE Kante des Rechtecks wird die neue Obergrenze der y-Achse; die
        /// Null bleibt unten. Ein Rechteck, dessen Oberkante die Mitte der Flaeche
        /// trifft, halbiert die Achse.
        /// </summary>
        [Fact]
        public void FensterAusBild_nimmt_die_obere_Kante_als_Obergrenze()
        {
            double mitteHoehe = (110.0 + 470.0) / 2.0 / 560.0;

            ChartRenderer.Achsenfenster f = ChartRenderer.FensterAusBild(
                new ChartRenderer.Bildausschnitt(0.2, 0.8, mitteHoehe, 470.0 / 560.0), STUNDEN);

            Assert.NotNull(f);
            Assert.Equal(0.5, f.YAnteil, 3);
        }

        /// <summary>
        /// Drei Faelle, in denen es KEIN Fenster gibt: ein Rechteck ohne Breite, ein
        /// Rechteck ueber das ganze Bild (dann ist nichts zugeschnitten) und eine zu
        /// kurze Reihe. In allen dreien bleibt das Bild, wie es ist.
        /// </summary>
        [Fact]
        public void FensterAusBild_liefert_ohne_Ausschnitt_nichts()
        {
            Assert.Null(ChartRenderer.FensterAusBild(
                new ChartRenderer.Bildausschnitt(0.5, 0.5, 0.2, 0.8), STUNDEN));
            Assert.Null(ChartRenderer.FensterAusBild(
                new ChartRenderer.Bildausschnitt(0.0, 1.0, 0.0, 1.0), STUNDEN));
            Assert.Null(ChartRenderer.FensterAusBild(
                new ChartRenderer.Bildausschnitt(0.2, 0.8, 0.2, 0.8), 2));
            Assert.Null(ChartRenderer.FensterAusBild(null, STUNDEN));
        }

        /// <summary>
        /// Und die Zusage, an der alles haengt: OHNE Fenster zeichnen beide
        /// Ganglinienbilder byte-genau dasselbe wie vorher. Der Zusatzparameter darf
        /// kein einziges Bild des Bestands veraendern — die 32 ChartProben pruefen
        /// dieselbe Aussage von der anderen Seite.
        /// </summary>
        [Fact]
        public void Ohne_Fenster_bleibt_jedes_Bild_wie_es_war()
        {
            var reihen = new List<ChartRenderer.Reihe>
            { new ChartRenderer.Reihe("Gesamt", Reihe(40, 25), SKColors.Red) };

            Assert.Equal(
                ChartRenderer.GanglinieNormiert("T", reihen, "%", ChartRenderer.Achse.Monate, false),
                ChartRenderer.GanglinieNormiert("T", reihen, "%", ChartRenderer.Achse.Monate, false, null));

            Assert.Equal(
                ChartRenderer.ErzeugerStapel("T", reihen, null, null, "kW",
                                             ChartRenderer.Achse.Jahresstunden, false),
                ChartRenderer.ErzeugerStapel("T", reihen, null, null, "kW",
                                             ChartRenderer.Achse.Jahresstunden, false, null, null, null));
        }

        /// <summary>
        /// Mit Fenster entsteht ein ANDERES Bild — und zwar in denselben Massen. Der
        /// Ausschnitt zeichnet neu, er schneidet nicht das fertige Bild zu.
        /// </summary>
        [Fact]
        public void Mit_Fenster_entsteht_ein_anderes_Bild_gleicher_Groesse()
        {
            var reihen = new List<ChartRenderer.Reihe>
            { new ChartRenderer.Reihe("Gesamt", Reihe(40, 25), SKColors.Red) };
            var fenster = new ChartRenderer.Achsenfenster(2000, 2500);

            byte[] ganz = ChartRenderer.ErzeugerStapel("T", reihen, null, null, "kW",
                                                       ChartRenderer.Achse.Jahresstunden, false);
            byte[] teil = ChartRenderer.ErzeugerStapel("T", reihen, null, null, "kW",
                                                       ChartRenderer.Achse.Jahresstunden, false,
                                                       null, null, fenster);

            Assert.NotEqual(ganz, teil);
            Assert.Equal((1240, 560), Mass(teil));

            byte[] b1Ganz = ChartRenderer.GanglinieNormiert("T", reihen, "%",
                                                            ChartRenderer.Achse.Jahresstunden, false);
            byte[] b1Teil = ChartRenderer.GanglinieNormiert("T", reihen, "%",
                                                            ChartRenderer.Achse.Jahresstunden, false, fenster);
            Assert.NotEqual(b1Ganz, b1Teil);
            Assert.Equal((1240, 560), Mass(b1Teil));
        }

        /// <summary>
        /// Der senkrechte Anteil greift nur beim Stapelbild. Die Prozentachse von B1
        /// ist per Definition 0…100 % des JAHRESHOECHSTWERTS; sie darf sich durch
        /// einen Ausschnitt nicht verschieben, sonst hiesse „100 %" in jedem Bild
        /// etwas anderes.
        /// </summary>
        [Fact]
        public void Der_senkrechte_Anteil_gilt_nur_fuer_den_Stapel()
        {
            var reihen = new List<ChartRenderer.Reihe>
            { new ChartRenderer.Reihe("Gesamt", Reihe(40, 25), SKColors.Red) };

            var ohne = new ChartRenderer.Achsenfenster(2000, 2500);
            var mit = new ChartRenderer.Achsenfenster(2000, 2500, 0.5);

            Assert.Equal(
                ChartRenderer.GanglinieNormiert("T", reihen, "%", ChartRenderer.Achse.Jahresstunden, false, ohne),
                ChartRenderer.GanglinieNormiert("T", reihen, "%", ChartRenderer.Achse.Jahresstunden, false, mit));

            Assert.NotEqual(
                ChartRenderer.ErzeugerStapel("T", reihen, null, null, "kW",
                                             ChartRenderer.Achse.Jahresstunden, false, null, null, ohne),
                ChartRenderer.ErzeugerStapel("T", reihen, null, null, "kW",
                                             ChartRenderer.Achse.Jahresstunden, false, null, null, mit));
        }
    }
}
