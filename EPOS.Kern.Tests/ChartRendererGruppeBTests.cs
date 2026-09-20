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
    /// <b>Die sechs Bilder der Gruppe (b)</b> (Konzept Diagramme, Etappe DG-E3):
    /// Kapitalwert-Verlauf, Jahresprojektion, Kennlinien, Streuwolke, Schnittkurve und
    /// Stückzahlkurve haben seit dieser Etappe ein öffentliches Zeichenmodell.
    ///
    /// <para>Geprüft wird hier, was der PNG-Vergleich der <c>ChartProben</c> nicht
    /// sieht, weil es am Bild gar nichts ändert: die <b>Achsenart</b> und die
    /// Einheiten (DG-E3-4), die <b>Punktwolke</b> samt ihren x-Stellen (DG-E3-5), die
    /// Aufteilung in Bilder MIT innerem <c>&lt;svg&gt;</c> und <b>reine Pixelbilder</b>
    /// (DG-E3-7), die Marken, die Datenreihen — und dass zweimal Erzeugen dasselbe
    /// Modell liefert.</para>
    ///
    /// <para>Die <see cref="Kulturvorrichtung"/> pinnt die Kultur auf <c>de-DE</c>:
    /// Die Reihennamen der Kennlinien und die Achsentexte entstehen mit deutscher
    /// Zahlenschreibweise, und der Windows-Läufer steht auf <c>en-US</c>.</para>
    /// </summary>
    public sealed class ChartRendererGruppeBTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =====================================================================
        // Die Proben-Daten — fest verdrahtet, ohne Zufall
        // =====================================================================

        private static List<ChartRenderer.Reihe> Barwerte()
            => new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Stamm", Rampe(21, -12000, 1400), ChartRenderer.C_STAMM),
                // Die zweite Reihe ist KUERZER: Sie muss ihr eigenes Fenster bekommen.
                new ChartRenderer.Reihe("Variante A", Rampe(11, -9000, 1700), ChartRenderer.C_WP)
            };

        private static double[] Rampe(int n, double start, double schritt)
        {
            var w = new double[n];
            for (int i = 0; i < n; i++) w[i] = start + i * schritt;
            return w;
        }

        private static List<ChartRenderer.KennlinienReihe> Kennlinien()
        {
            var reihen = new List<ChartRenderer.KennlinienReihe>();
            foreach (int vorlauf in new[] { 35, 45 })
            {
                var punkte = new List<(double Temperatur, double Wert)>();
                for (int t = -15; t <= 15; t += 5)
                    punkte.Add((t, 5.0 - (vorlauf - 35) * 0.04 + t * 0.05));
                reihen.Add(new ChartRenderer.KennlinienReihe(vorlauf, punkte));
            }
            return reihen;
        }

        private static List<ChartRenderer.Punktreihe> Wolken()
        {
            var eins = new List<(double X, double Y)>();
            var zwei = new List<(double X, double Y)>();
            for (int i = 0; i < 200; i++)
            {
                double t = -12.0 + 30.0 * i / 200.0;
                eins.Add((t, Math.Max(0, 50.0 - 2.0 * t)));
                zwei.Add((t, Math.Max(0, 30.0 - 1.2 * t)));
            }
            return new List<ChartRenderer.Punktreihe>
            {
                new ChartRenderer.Punktreihe("Bedarf", eins, new SKColor(255, 0, 0, 120)),
                new ChartRenderer.Punktreihe("Produktion", zwei, new SKColor(0, 0, 255, 120))
            };
        }

        /// <summary>
        /// Die Kapazitäten einer Schnittkurve MIT Feinraster: zwischen 2 000 und 2 500
        /// liegen zwei Feinpunkte — die Stützstellen sind also ungleichmäßig.
        /// </summary>
        private static readonly double[] KAPAZITAETEN =
        { 500, 1000, 1500, 2000, 2150, 2350, 2500, 3000 };

        private static readonly bool[] FEINPUNKTE =
        { false, false, false, false, true, true, false, false };

        private static readonly double[] SCHNITTWERTE =
        { -2000, 400, 1800, 2400, 2520, 2480, 2300, 1100 };

        private static Zeichenmodell Schnittkurve()
            => ChartRenderer.SchnittkurveModell("Schnittkurve bei 1,5 C", "Kapazität [kWh]",
                                                "ΔJ [€/a]", KAPAZITAETEN, SCHNITTWERTE,
                                                2150, 2520, FEINPUNKTE);

        private static readonly int[] JAHRE =
        { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        private static Zeichenmodell Jahresprojektion()
        {
            var netto = new double[12];
            for (int i = 0; i < 12; i++) netto[i] = -800 + 220 * (i + 1);
            var kumuliert = new double[12];
            double summe = -9000;
            for (int i = 0; i < 12; i++) { summe += netto[i]; kumuliert[i] = summe; }

            return ChartRenderer.JahresprojektionModell(
                "Jahresprojektion [€]", JAHRE,
                new ChartRenderer.Reihe("Netto-Cashflow", netto, ChartRenderer.C_PV),
                new ChartRenderer.Reihe("kumuliert", kumuliert, ChartRenderer.C_STAMM),
                new[] { 7 }, null, "Zahlung [€]", "Projektjahr");
        }

        private static Zeichenmodell Stueckzahlkurve()
            => ChartRenderer.StueckzahlkurveModell("Kapitalwert über Stückzahl",
                    "Stückzahl [Stück]", "Kapitalwert [€]",
                    new[] { 1, 2, 3, 4 }, new[] { 12000.0, 21000.0, 9000.0, -4000.0 },
                    1, new[] { false, false, true, false });

        /// <summary>Alle Marken des Modells, auch die in Gruppen.</summary>
        private static List<string> Marken(IReadOnlyList<Zeichenbefehl> befehle)
        {
            var liste = new List<string>();
            foreach (Zeichenbefehl b in befehle ?? new List<Zeichenbefehl>())
            {
                if (b.Marke != null) liste.Add(b.Marke);
                if (b is Gruppe g) liste.AddRange(Marken(g.Befehle));
            }
            return liste;
        }

        // =====================================================================
        // 1 — DG-E3-7: wer eine Zeichenfläche hat, und wer nicht
        // =====================================================================

        /// <summary>
        /// <b>Drei Bilder tragen eine Zeichenfläche, drei nicht (Entscheid
        /// DG-E3-7).</b> Ein Zeitachsen-Zoom hat nur dort eine Aussage, wo die x-Achse
        /// eine stetige Größe zählt: das Projektjahr, die Außentemperatur, die
        /// Kapazität. Kennlinien, Jahresprojektion und Stückzahlkurve zählen
        /// Stützstellen, Jahre und ganze Geräte — und ein inneres <c>&lt;svg&gt;</c>
        /// ersetzte dort die Säulen und Punktmarken durch Pfade.
        /// </summary>
        [Fact]
        public void NurDreiDerSechsBilderTragenEineZeichenflaeche()
        {
            Assert.NotNull(ChartRenderer.KapitalwertVerlaufModell("K", Barwerte(), null).Flaeche);
            Assert.NotNull(ChartRenderer.StreuwolkeModell("S", "T", "P", Wolken()).Flaeche);
            Assert.NotNull(Schnittkurve().Flaeche);

            Assert.Null(ChartRenderer.KennlinienModell("K", "COP", "T", Kennlinien(),
                                                       ChartRenderer.Kennlinienmarke.Kreis).Flaeche);
            Assert.Null(Jahresprojektion().Flaeche);
            Assert.Null(Stueckzahlkurve().Flaeche);
        }

        // =====================================================================
        // 2 — DG-E3-4: was x je Bild zählt, und in welcher Einheit
        // =====================================================================

        /// <summary>
        /// <b>Jede Zeichenfläche nennt ihre Achsenart (Entscheid DG-E3-4).</b> Alle
        /// drei Bilder der Gruppe (b) führen eine freie Größe: das Projektjahr in
        /// Jahren, die Außentemperatur in Grad — und die Größenachse der Rastersuche,
        /// deren Einheit mit dem Suchmodus wechselt und deshalb NICHT im Modell steht.
        /// </summary>
        [Fact]
        public void JedeFlaecheNenntIhreAchsenartUndEinheit()
        {
            Zeichenflaeche kapital =
                ChartRenderer.KapitalwertVerlaufModell("K", Barwerte(), null).Flaeche;
            Assert.Equal(Achsenart.Wert, kapital.X);
            Assert.Equal("a", kapital.XEinheit);
            Assert.Equal(0, kapital.Daten.XVon);
            Assert.Equal(20, kapital.Daten.XBis);            // 21 Stützstellen = 20 Jahre

            Zeichenflaeche streu = ChartRenderer.StreuwolkeModell("S", "T", "P", Wolken()).Flaeche;
            Assert.Equal(Achsenart.Wert, streu.X);
            Assert.Equal("°C", streu.XEinheit);
            Assert.True(streu.Daten.XVon <= -12.0 && streu.Daten.XBis >= 17.8);

            Zeichenflaeche schnitt = Schnittkurve().Flaeche;
            Assert.Equal(Achsenart.Wert, schnitt.X);
            Assert.Null(schnitt.XEinheit);
            Assert.Equal(500, schnitt.Daten.XVon);
            Assert.Equal(3000, schnitt.Daten.XBis);
        }

        /// <summary>
        /// <b>Die Flächen der Gruppe (a) sind nachgezogen.</b> Kostenprofil und
        /// Stundenprofil legen ihre Reihe über deren EIGENE Länge auf eine feste Achse
        /// — dort zählt x einen Index. Die übrigen Zeitreihenbilder zählen
        /// Jahresstunden, die Vorgabe.
        /// </summary>
        [Fact]
        public void DieFlaechenDerGruppeATragenStundenOderIndex()
        {
            var profil = new double[48];
            for (int i = 0; i < profil.Length; i++) profil[i] = 10 + i % 7;

            Assert.Equal(Achsenart.Index,
                ChartRenderer.KostenprofilModell("K", profil, "ct/kWh", "Monat").Flaeche.X);
            Assert.Equal(Achsenart.Index,
                ChartRenderer.StundenprofilModell("S", profil, 24, "x", "y").Flaeche.X);
            Assert.Equal(Achsenart.Stunden,
                ChartRenderer.JahresverlaufModell("J", profil, "kW", SKColors.SteelBlue).Flaeche.X);
        }

        /// <summary>
        /// <b>Die Achsenteilung folgt der Achsenart</b> (DG-E3-4): Stunden über die
        /// Jahresstundenteilung, ein Index GANZZAHLIG, ein Wert auf den „schönen"
        /// Stufen. Jede Marke liegt im Fenster.
        /// </summary>
        [Fact]
        public void DieAchsenteilungFolgtDerAchsenart()
        {
            var bild = new Rahmen(0, 0, 1000, 400);

            var stunden = new Zeichenflaeche(bild, new Datenfenster(0, 8759, 0, 100));
            IReadOnlyList<(double Wert, string Text)> ts =
                ChartRenderer.Achsenteilung(stunden, 3000, 3500);
            Assert.NotEmpty(ts);
            Assert.All(ts, m => Assert.InRange(m.Wert, 3000, 3500));
            Assert.Equal(ChartRenderer.Jahresstundenteilung(3000, 3500).Select(t => (double)t.Stunde),
                         ts.Select(m => m.Wert));

            var index = new Zeichenflaeche(bild, new Datenfenster(0, 167, 0, 100), Achsenart.Index);
            IReadOnlyList<(double Wert, string Text)> ti =
                ChartRenderer.Achsenteilung(index, 0, 12);
            Assert.NotEmpty(ti);
            Assert.All(ti, m => Assert.Equal(Math.Floor(m.Wert), m.Wert));

            var wert = new Zeichenflaeche(bild, new Datenfenster(-20, 20, 0, 100),
                                          Achsenart.Wert, "°C");
            IReadOnlyList<(double Wert, string Text)> tw =
                ChartRenderer.Achsenteilung(wert, -18.2, 20.3);
            Assert.Equal(new double[] { -10, 0, 10, 20 }, tw.Select(m => m.Wert));
            // Die Null heisst "0" und nicht "-0".
            Assert.Equal("0", tw[1].Text);

            // Ohne Flaeche und ohne Fensterbreite gibt es nichts zu teilen.
            Assert.Empty(ChartRenderer.Achsenteilung(null, 0, 10));
            Assert.Empty(ChartRenderer.Achsenteilung(wert, 5, 5));
        }

        /// <summary>
        /// Eine <see cref="Achsenart.Index"/> nimmt NIE eine gebrochene Stufe: Die
        /// Stufenfolge kennt 2,5 — zwischen zwei Stützstellen steht aber kein Wert.
        /// </summary>
        [Fact]
        public void EineIndexachseTraegtNurGanzeStufen()
        {
            var bild = new Rahmen(0, 0, 500, 200);
            for (int bis = 2; bis <= 400; bis++)
            {
                var f = new Zeichenflaeche(bild, new Datenfenster(0, bis, 0, 1), Achsenart.Index);
                foreach ((double w, string _) in ChartRenderer.Achsenteilung(f, 0, bis))
                    Assert.Equal(Math.Floor(w), w);
            }
        }

        // =====================================================================
        // 3 — DG-E3-5: die Punktwolke
        // =====================================================================

        /// <summary>
        /// <b>Die Streuwolke wird eine <see cref="Reihenart.Punkte"/> (DG-E3-5):</b>
        /// je Reihe eine Datenreihe mit der Außentemperatur als <c>XWerte</c> und der
        /// Leistung als <c>Werte</c>. Die Strichstärke ist der DURCHMESSER des Kreises
        /// im Bild (Radius 2,5) — im SVG zeichnet eine runde Strichkappe daraus
        /// denselben Punkt.
        /// </summary>
        [Fact]
        public void DieStreuwolkeWirdEinePunktreiheMitEigenenXWerten()
        {
            List<ChartRenderer.Punktreihe> wolken = Wolken();
            Zeichenmodell m = ChartRenderer.StreuwolkeModell("S", "Temperatur", "Leistung [kW]",
                                                             wolken);

            Assert.Equal(wolken.Count, m.Reihen.Count);
            for (int i = 0; i < wolken.Count; i++)
            {
                Datenreihe r = m.Reihen[i];
                Assert.Equal(wolken[i].Name, r.Name);
                Assert.Equal(Reihenart.Punkte, r.Art);
                Assert.Equal(5f, r.Staerke);
                Assert.Equal("Leistung [kW]", r.Einheit);
                Assert.NotNull(r.XWerte);
                Assert.Equal(wolken[i].Punkte.Count, r.XWerte.Length);
                Assert.Equal(wolken[i].Punkte.Select(p => p.X), r.XWerte);
                Assert.Equal(wolken[i].Punkte.Select(p => p.Y), r.Werte);
            }
        }

        /// <summary>
        /// <b>Die Schnittkurve bringt ihre x-Stellen mit</b> (DG-E3-5): Die zweite
        /// Suchphase schiebt Feinpunkte zwischen die Grobpunkte, die Stützstellen
        /// liegen also ungleichmäßig. Ohne eigene x-Stelle säße jeder Feinpunkt an der
        /// falschen Kapazität.
        /// </summary>
        [Fact]
        public void DieSchnittkurveTraegtIhreUngleichmaessigenStuetzstellen()
        {
            Datenreihe r = Assert.Single(Schnittkurve().Reihen);

            Assert.Equal(Reihenart.Linie, r.Art);
            Assert.Equal(KAPAZITAETEN, r.XWerte);
            Assert.Equal(SCHNITTWERTE, r.Werte);
            Assert.Equal("ΔJ [€/a]", r.Einheit);

            // Die Stuetzstellen sind wirklich ungleichmaessig — sonst pruefte der Fall
            // nichts.
            Assert.NotEqual(r.XWerte[1] - r.XWerte[0], r.XWerte[4] - r.XWerte[3]);
        }

        // =====================================================================
        // 4 — Die Datenreihen der reinen Pixelbilder
        // =====================================================================

        /// <summary>
        /// <b>Kennlinien und Jahresprojektion führen ihre Reihen trotzdem</b>
        /// (DG-E3-7): Ohne Zeichenfläche zeichnet der Schreiber daraus nichts — die
        /// Reihen sind die Quelle der ZEIGERZEILE, und sie brauchen dafür ihre x-Stelle
        /// je Wert.
        /// </summary>
        [Fact]
        public void DiePixelbilderFuehrenIhreReihenFuerDieZeigerzeile()
        {
            Zeichenmodell k = ChartRenderer.KennlinienModell("K", "COP", "Temperatur",
                                  Kennlinien(), ChartRenderer.Kennlinienmarke.Kreis);
            Assert.Equal(2, k.Reihen.Count);
            Assert.Equal("35°C", k.Reihen[0].Name);
            Assert.Equal("45°C", k.Reihen[1].Name);
            foreach (Datenreihe r in k.Reihen)
            {
                Assert.Equal("COP", r.Einheit);
                Assert.Equal(7, r.Werte.Length);
                Assert.Equal(new double[] { -15, -10, -5, 0, 5, 10, 15 }, r.XWerte);
            }

            Zeichenmodell j = Jahresprojektion();
            Assert.Equal(new[] { "Netto-Cashflow", "kumuliert" },
                         j.Reihen.Select(r => r.Name).ToArray());
            foreach (Datenreihe r in j.Reihen)
            {
                Assert.Equal("€", r.Einheit);
                Assert.Equal(JAHRE.Select(x => (double)x), r.XWerte);
            }
        }

        /// <summary>
        /// <b>Die Stückzahlkurve führt KEINE Datenreihe</b> (DG-E3-7). Die x-Achse
        /// zählt ganze Geräte; eine Zeigerzeile sagte an einer Säule nicht mehr, als
        /// die Säule zeigt. Ihre Marken hat sie trotzdem — die Säulen als Reihe, der
        /// Bestwert als Marke.
        /// </summary>
        [Fact]
        public void DieStueckzahlkurveFuehrtNurMarken()
        {
            Zeichenmodell m = Stueckzahlkurve();

            Assert.Empty(m.Reihen);
            List<string> marken = Marken(m.Befehle);
            Assert.Contains("reihe:Kapitalwert über Stückzahl", marken);
            Assert.Contains("marke", marken);
            Assert.Contains("nulllinie", marken);
        }

        // =====================================================================
        // 5 — Die Marken je Bild
        // =====================================================================

        /// <summary>
        /// <b>Jedes der sechs Bilder trägt Titel und beide Achsen als Marke</b>, und
        /// jedes nennt seine Reihen. Damit schaltet die Oberfläche Gruppen ein und aus
        /// und zeichnet beim Zoom eine Achsenteilung nach.
        /// </summary>
        [Fact]
        public void JedesBildTraegtTitelAchsenUndReihen()
        {
            var bilder = new Dictionary<string, Zeichenmodell>
            {
                ["Kapitalwert"] = ChartRenderer.KapitalwertVerlaufModell("K", Barwerte(), "Fuss"),
                ["Streuwolke"] = ChartRenderer.StreuwolkeModell("S", "T", "P", Wolken()),
                ["Schnittkurve"] = Schnittkurve(),
                ["Kennlinien"] = ChartRenderer.KennlinienModell("K", "COP", "T", Kennlinien(),
                                     ChartRenderer.Kennlinienmarke.Kreuz),
                ["Jahresprojektion"] = Jahresprojektion(),
                ["Stueckzahlkurve"] = Stueckzahlkurve()
            };

            foreach (KeyValuePair<string, Zeichenmodell> b in bilder)
            {
                List<string> marken = Marken(b.Value.Befehle);
                Assert.Contains("titel", marken);
                Assert.Contains("xachse", marken);
                Assert.Contains("yachse", marken);
                Assert.Contains(marken, s => s.StartsWith("reihe:", StringComparison.Ordinal));
                Assert.True(marken.Count > 5, b.Key + " traegt kaum Marken");
            }

            // Die Legende markiert sich selbst — je Eintrag eine Trefferflaeche.
            Assert.Contains("legende:Stamm",
                Marken(ChartRenderer.KapitalwertVerlaufModell("K", Barwerte(), null).Befehle));
            // Die BESTE-Marke der Schnittkurve ist eine Marke, keine Reihe.
            Assert.Contains("marke", Marken(Schnittkurve().Befehle));
        }

        /// <summary>
        /// <b>Das Achsenkreuz bleibt markenlos</b> — die Regel seit E2: Blendet die
        /// Oberfläche beim Zoom die Teilung einer Achse aus, müssen die beiden
        /// Achsenlinien stehen bleiben. In jedem der sechs Bilder stehen deshalb genau
        /// zwei Linien ohne Marke.
        /// </summary>
        [Fact]
        public void DasAchsenkreuzTraegtKeineMarke()
        {
            foreach (Zeichenmodell m in new[]
            {
                ChartRenderer.KapitalwertVerlaufModell("K", Barwerte(), null),
                ChartRenderer.StreuwolkeModell("S", "T", "P", Wolken()),
                Schnittkurve(),
                ChartRenderer.KennlinienModell("K", "COP", "T", Kennlinien(),
                                               ChartRenderer.Kennlinienmarke.Kreis),
                Jahresprojektion(),
                Stueckzahlkurve()
            })
            {
                int ohneMarke = m.Befehle.Count(b => b is Linie && b.Marke == null);
                Assert.Equal(2, ohneMarke);
            }
        }

        // =====================================================================
        // 6 — Das eigene Fenster einer kürzeren Reihe
        // =====================================================================

        /// <summary>
        /// <b>Eine kürzere Reihe endet früher</b> (DG-E3-1): Das Bild zeichnet sie über
        /// ihre eigene Länge, und ihr Datenfenster sagt das auch. Beide Reihen teilen
        /// dieselbe y-Skala.
        /// </summary>
        [Fact]
        public void EineKuerzereReiheBekommtIhrEigenesFenster()
        {
            Zeichenmodell m = ChartRenderer.KapitalwertVerlaufModell("K", Barwerte(), null);

            Assert.Equal(2, m.Reihen.Count);
            Assert.Equal(0, m.Reihen[0].Fenster.XVon);
            Assert.Equal(20, m.Reihen[0].Fenster.XBis);     // 21 Stützstellen
            Assert.Equal(10, m.Reihen[1].Fenster.XBis);     // 11 Stützstellen
            Assert.Equal(m.Flaeche.Daten.YVon, m.Reihen[1].Fenster.YVon);
            Assert.Equal(m.Flaeche.Daten.YBis, m.Reihen[1].Fenster.YBis);
            Assert.All(m.Reihen, r => Assert.Equal("€", r.Einheit));
        }

        // =====================================================================
        // 7 — Leerfälle und Determinismus
        // =====================================================================

        /// <summary>
        /// <b>Ein leeres Bild trägt den Leerhinweis und sonst nichts</b> — keine
        /// Zeichenfläche, keine Reihe. Es bleibt ein gültiges Modell, und der Maler
        /// malt daraus ein Bild in der festgelegten Größe.
        /// </summary>
        [Fact]
        public void DieLeerfaelleTragenIhrenHinweisUndKeineReihe()
        {
            var leer = new Dictionary<string, Zeichenmodell>
            {
                ["Kapitalwert"] = ChartRenderer.KapitalwertVerlaufModell(
                    "K", new List<ChartRenderer.Reihe>(), null),
                ["Streuwolke"] = ChartRenderer.StreuwolkeModell("S", "T", "P", null),
                ["Schnittkurve"] = ChartRenderer.SchnittkurveModell("S", "x", "y",
                    new double[0], new double[0], 0, 0),
                ["Kennlinien"] = ChartRenderer.KennlinienModell("K", "COP", "T", null,
                    ChartRenderer.Kennlinienmarke.Kreis),
                ["Jahresprojektion"] = ChartRenderer.JahresprojektionModell(
                    "J", new int[0], null, null, null),
                ["Stueckzahlkurve"] = ChartRenderer.StueckzahlkurveModell("S", "x", "y",
                    new int[0], new double[0], -1)
            };

            foreach (KeyValuePair<string, Zeichenmodell> b in leer)
            {
                Assert.Contains("leerhinweis", Marken(b.Value.Befehle));
                Assert.Null(b.Value.Flaeche);
                Assert.Empty(b.Value.Reihen);
                Assert.NotEmpty(SkiaMaler.Png(b.Value));
            }
        }

        /// <summary>
        /// <b>Zweimal erzeugt ist dasselbe Modell</b> — Befehl für Befehl, Reihe für
        /// Reihe, samt Fenster, Achsenart und x-Stellen. Ohne diese Zusage wäre weder
        /// die Hash-Messlatte des PNG noch der byte-gleiche SVG-Text zu halten.
        /// </summary>
        [Fact]
        public void ZweimalErzeugtIstDasselbeModell()
        {
            Assert.True(ChartRenderer.KapitalwertVerlaufModell("K", Barwerte(), "F")
                .Gleicht(ChartRenderer.KapitalwertVerlaufModell("K", Barwerte(), "F")));
            Assert.True(ChartRenderer.StreuwolkeModell("S", "T", "P", Wolken())
                .Gleicht(ChartRenderer.StreuwolkeModell("S", "T", "P", Wolken())));
            Assert.True(Schnittkurve().Gleicht(Schnittkurve()));
            Assert.True(ChartRenderer.KennlinienModell("K", "COP", "T", Kennlinien(),
                            ChartRenderer.Kennlinienmarke.Kreis)
                .Gleicht(ChartRenderer.KennlinienModell("K", "COP", "T", Kennlinien(),
                            ChartRenderer.Kennlinienmarke.Kreis)));
            Assert.True(Jahresprojektion().Gleicht(Jahresprojektion()));
            Assert.True(Stueckzahlkurve().Gleicht(Stueckzahlkurve()));
        }

        /// <summary>
        /// <b>Die <c>byte[]</c>-Methode ist nur noch die Weiche</b>: Sie gibt dasselbe
        /// Modell an den Maler, das die <c>…Modell</c>-Methode liefert — Byte für Byte
        /// dasselbe Bild. Das ist die Zusage, an der die Hash-Messlatte der
        /// ChartProben hängt.
        /// </summary>
        [Fact]
        public void DieByteMethodeMaltGenauDiesesModell()
        {
            Assert.Equal(ChartRenderer.KapitalwertVerlauf("K", Barwerte(), "F"),
                         SkiaMaler.Png(ChartRenderer.KapitalwertVerlaufModell("K", Barwerte(), "F")));
            Assert.Equal(ChartRenderer.Streuwolke("S", "T", "P", Wolken()),
                         SkiaMaler.Png(ChartRenderer.StreuwolkeModell("S", "T", "P", Wolken())));
            Assert.Equal(ChartRenderer.Schnittkurve("Schnittkurve bei 1,5 C", "Kapazität [kWh]",
                             "ΔJ [€/a]", KAPAZITAETEN, SCHNITTWERTE, 2150, 2520, FEINPUNKTE),
                         SkiaMaler.Png(Schnittkurve()));
            Assert.Equal(ChartRenderer.Kennlinien("K", "COP", "T", Kennlinien(),
                             ChartRenderer.Kennlinienmarke.Kreis),
                         SkiaMaler.Png(ChartRenderer.KennlinienModell("K", "COP", "T", Kennlinien(),
                             ChartRenderer.Kennlinienmarke.Kreis)));
            Assert.Equal(SkiaMaler.Png(Stueckzahlkurve()),
                         ChartRenderer.Stueckzahlkurve("Kapitalwert über Stückzahl",
                             "Stückzahl [Stück]", "Kapitalwert [€]",
                             new[] { 1, 2, 3, 4 }, new[] { 12000.0, 21000.0, 9000.0, -4000.0 },
                             1, new[] { false, false, true, false }));
        }
    }
}
