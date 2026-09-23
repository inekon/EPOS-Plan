using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>DIE SVG-GEGENPROBEN DER GRUPPE (b)</b> (Auftrag DG-E3b) — die sechs Bilder,
    /// die mit dieser Etappe ein öffentliches Zeichenmodell bekommen haben:
    /// Kapitalwert-Verlauf, Jahresprojektion, Kennlinien, Streuwolke, Schnittkurve und
    /// Stückzahlkurve.
    ///
    /// <para><b>Warum eine eigene Datei.</b> <c>Program.cs</c> ist die PNG-Probe; sie
    /// wächst mit jedem Bild. Die Gruppe (b) hängt sich mit EINER Zeile dort ein
    /// (<see cref="GruppeBProben"/>) und bringt ihre synthetischen Daten, ihre
    /// Gegenproben und ihre Sonderprüfung selbst mit. Dieselben privaten Helfer
    /// (<c>Wolke</c>, <c>Rasterfeld</c>, <c>Projektionsreihe</c>, …) stehen einer
    /// partiellen Klasse offen — die Zahlen bleiben also dieselben wie im
    /// PNG-Teil.</para>
    ///
    /// <para><b>Zwei Arten von Gegenprobe.</b> Drei Bilder tragen eine Zeichenfläche
    /// und damit ein inneres <c>&lt;svg&gt;</c>; sie gehen durch dieselbe
    /// <see cref="SvgModellprobe"/> wie die Gruppe (a). Die drei REINEN PIXELBILDER
    /// (Entscheid DG-E3-7) gehen durch <see cref="SvgPixelModellprobe"/>: Dort wird
    /// geprüft, dass es KEIN inneres svg gibt und die Reihenbefehle als gewöhnliche
    /// Pixel-Elemente mit ihrer Marke im Baum stehen.</para>
    /// </summary>
    internal static partial class Program
    {
        /// <summary>
        /// Die Bilder der Gruppe (b), die eine Zeichenfläche führen (DG-E3-7). Die
        /// Liste IST die Prüfung: Was hier steht, muss ein inneres svg haben, was nicht
        /// hier steht, darf keines haben.
        /// </summary>
        private static readonly string[] GRUPPE_B_MIT_FLAECHE =
        { "kapitalwert_absolut", "streuwolke_drei_reihen", "schnittkurve" };

        /// <summary>Die drei halbtransparenten Reihen der Streuwolke — wie im PNG-Teil.</summary>
        private static List<ChartRenderer.Punktreihe> GruppeBWolken()
            => new List<ChartRenderer.Punktreihe>
            {
                new ChartRenderer.Punktreihe("Waermebedarf", Wolke(0), STREU_ROT),
                new ChartRenderer.Punktreihe("Heizstab", Wolke(1), STREU_GELB),
                new ChartRenderer.Punktreihe("Waermeproduktion", Wolke(2), STREU_BLAU)
            };

        /// <summary>
        /// Die sechs Modelle der Gruppe (b) unter dem Namen IHRES PNG. Gleiche Namen
        /// heißt: <c>--ablage</c> legt <c>&lt;name&gt;.png</c> ab, <c>--svg-alle</c>
        /// daneben <c>&lt;name&gt;.svg</c> — die Sichtprüfung vergleicht dann zwei
        /// Dateien desselben Stamms.
        /// </summary>
        private static IReadOnlyList<KeyValuePair<string, Func<Zeichenmodell>>> GruppeBBilder()
        {
            // Die Daten sind dieselben wie im PNG-Teil; nur so vergleicht die
            // Sichtpruefung zwei Bilder DESSELBEN Inhalts.
            List<VerlaufSerie> serien = Beispielserien();
            List<ChartRenderer.KennlinienReihe> kennlinien = Kennlinien(cop: true);

            double[] rasterCRaten = { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0 };
            var rasterKapazitaeten = new double[10];
            for (int i = 0; i < 10; i++) rasterKapazitaeten[i] = 500.0 + i * 500.0;
            double[][] rasterWerte = Rasterfeld(rasterKapazitaeten, rasterCRaten);

            int[] stueckzahlen = { 1, 2, 3, 4, 5 };
            double[] stueckwerte = { 28400.0, 41280.0, 33700.0, 8900.0, -14500.0 };
            bool[] stuecksperre = { false, false, true, false, false };

            var projektjahre = new int[20];
            for (int i = 0; i < 20; i++) projektjahre[i] = i + 1;
            double[] projektionNetto = Projektionsreihe();
            double[] projektionKumuliert = Kumuliert(projektionNetto, -15000.0);
            var projektionBetrieb = new double[20];
            for (int i = 0; i < 20; i++) projektionBetrieb[i] = -420.0 - i * 12.0;

            return new List<KeyValuePair<string, Func<Zeichenmodell>>>
            {
                Modellprobe("kapitalwert_absolut",
                    () => ChartRenderer.KapitalwertVerlaufModell("Kumulierte Barwerte je Projekt",
                            ChartRenderer.VerlaufsReihen(serien, true), null)),
                Modellprobe("streuwolke_drei_reihen",
                    () => ChartRenderer.StreuwolkeModell("Leistung ueber Aussentemperatur",
                            "Temperatur [°C]", "Leistung [kW]", GruppeBWolken())),
                Modellprobe("schnittkurve",
                    () => ChartRenderer.SchnittkurveModell("Schnittkurve bei 1,5 C",
                            "Kapazität [kWh]", "ΔJ [€/a]",
                            rasterKapazitaeten, Rasterspalte(rasterWerte, RASTER_BESTE_SPALTE),
                            rasterKapazitaeten[RASTER_BESTE_ZEILE],
                            rasterWerte[RASTER_BESTE_ZEILE][RASTER_BESTE_SPALTE])),
                Modellprobe("kennlinien_cop",
                    () => ChartRenderer.KennlinienModell("Kennlinien COP", "COP", "Temperatur",
                            kennlinien, ChartRenderer.Kennlinienmarke.Kreis)),
                Modellprobe("jahresprojektion",
                    () => ChartRenderer.JahresprojektionModell("Jahresprojektion [€]", projektjahre,
                            new ChartRenderer.Reihe("Netto-Cashflow", projektionNetto,
                                                    ChartRenderer.C_PV),
                            new ChartRenderer.Reihe("kumuliert", projektionKumuliert,
                                                    ChartRenderer.C_STAMM),
                            new[] { 10 },
                            new List<ChartRenderer.Reihe>
                            {
                                new ChartRenderer.Reihe("Betrieb", projektionBetrieb,
                                                        ChartRenderer.C_BHKW) { Strichart = ChartRenderer.Strichart.Gestrichelt }
                            },
                            "Zahlung [€]", "Projektjahr")),
                Modellprobe("flotte_stueckzahlkurve",
                    () => ChartRenderer.StueckzahlkurveModell(
                            "Kapitalwert über Stückzahl — Growatt WIT-M+APX ESS",
                            "Stückzahl [Stück]", "Kapitalwert [€]",
                            stueckzahlen, stueckwerte, 1, stuecksperre))
            };
        }

        /// <summary>
        /// Die Gegenproben der Gruppe (b) — der EINE Einstieg, den <c>Program.cs</c>
        /// ruft. Er prüft die sechs Modelle, dazu die Punktwolke und die Achsenteilung,
        /// und schreibt mit <c>--svg-alle</c> auch die sechs <c>.svg</c>.
        /// </summary>
        private static void GruppeBProben()
        {
            IReadOnlyList<KeyValuePair<string, Func<Zeichenmodell>>> bilder = GruppeBBilder();

            foreach (KeyValuePair<string, Func<Zeichenmodell>> b in bilder)
                if (Array.IndexOf(GRUPPE_B_MIT_FLAECHE, b.Key) >= 0) SvgModellprobe(b.Key, b.Value);
                else SvgPixelModellprobe(b.Key, b.Value);

            GruppeBPunktwolke();
            GruppeBAchsenteilung();

            if (_svgordner != null) SvgOrdnerSchreiben(bilder);
        }

        /// <summary>
        /// <b>Die Gegenprobe eines REINEN PIXELBILDES</b> (Entscheid DG-E3-7). Geprüft
        /// wird, was der PNG-Vergleich nicht sieht:
        ///
        /// <list type="number">
        ///   <item>Zweimal geschrieben UND zweimal erzeugt ist byte-gleich.</item>
        ///   <item>Das Modell führt KEINE Zeichenfläche, und im Baum steht kein
        ///   <c>svg.epos-flaeche</c> — die Säulen und Punktmarken bleiben also
        ///   stehen.</item>
        ///   <item>Die Befehle der Reihen stehen als gewöhnliche Elemente mit
        ///   <c>data-marke="reihe:…"</c> im Baum.</item>
        ///   <item>Titel und beide Achsen tragen ihre Marke.</item>
        ///   <item>Jede <c>Datenreihe</c> des Modells bringt ihre x-Stelle je Wert mit
        ///   (DG-E3-5) — ohne sie läse die Zeigerzeile den falschen Index.</item>
        /// </list>
        /// </summary>
        private static void SvgPixelModellprobe(string name, Func<Zeichenmodell> bau)
        {
            SvgProbe("svg_" + name, e =>
            {
                Zeichenmodell m = bau();
                string a = SvgSchreiber.Text(m);
                e.Masse = m.Breite + "x" + m.Hoehe;
                e.Groesse = Encoding.UTF8.GetByteCount(a)
                                    .ToString("N0", CultureInfo.InvariantCulture);

                if (!string.Equals(a, SvgSchreiber.Text(m), StringComparison.Ordinal))
                    e.Maengel.Add("zweimal geschrieben ist nicht byte-gleich");
                if (!string.Equals(a, SvgSchreiber.Text(bau()), StringComparison.Ordinal))
                    e.Maengel.Add("zweimal erzeugt ist nicht byte-gleich");

                List<SvgKnoten> alle = SvgSchreiber.Baum(m).Alle().ToList();
                e.Knoten = alle.Count.ToString(CultureInfo.InvariantCulture);

                if (m.Flaeche != null)
                    e.Maengel.Add("das Modell fuehrt eine Zeichenflaeche (DG-E3-7: keine)");
                if (alle.Any(k => Attributwert(k, "class") == SvgSchreiber.KLASSE_FLAECHE))
                    e.Maengel.Add("es gibt ein inneres svg, obwohl das Bild keines haben darf");

                int reihenbefehle = alle.Count(
                    k => (Attributwert(k, "data-marke") ?? "")
                         .StartsWith("reihe:", StringComparison.Ordinal));
                if (reihenbefehle == 0)
                    e.Maengel.Add("kein Befehl traegt die Marke einer Reihe");

                foreach (string marke in new[] { "titel", "xachse", "yachse" })
                    if (!alle.Any(k => Attributwert(k, "data-marke") == marke))
                        e.Maengel.Add("die Marke " + marke + " fehlt");

                foreach (Datenreihe r in m.Reihen)
                {
                    if (r.XWerte == null)
                    { e.Maengel.Add("Datenreihe ohne XWerte: " + r.Name); continue; }
                    if (r.XWerte.Length != r.Werte.Length)
                        e.Maengel.Add("XWerte und Werte sind verschieden lang: " + r.Name);
                }
            });
        }

        /// <summary>
        /// <b>Die Punktwolke der Streuwolke</b> (Entscheid DG-E3-5): EIN Pfad je Reihe,
        /// je Punkt ein Segment <c>M x,y h 0</c>, runde Strichkappe, Strichbreite gleich
        /// dem Punktdurchmesser des PNG — und KEINE Bündelung, also genau so viele
        /// Segmente wie Werte.
        /// </summary>
        private static void GruppeBPunktwolke()
        {
            SvgProbe("svg_streuwolke_punkte", e =>
            {
                Zeichenmodell m = ChartRenderer.StreuwolkeModell("Leistung ueber Aussentemperatur",
                                      "Temperatur [°C]", "Leistung [kW]", GruppeBWolken());
                e.Masse = m.Breite + "x" + m.Hoehe;

                List<SvgKnoten> pfade = SvgSchreiber.Baum(m).Alle()
                    .Where(k => k.Name == "path" &&
                                Attributwert(k, "class") == SvgSchreiber.KLASSE_REIHE)
                    .ToList();
                e.Knoten = pfade.Count.ToString(CultureInfo.InvariantCulture);

                if (pfade.Count != m.Reihen.Count)
                { e.Maengel.Add("Reihenpfade: " + pfade.Count + " statt " + m.Reihen.Count); return; }

                int segmente = 0;
                for (int i = 0; i < pfade.Count; i++)
                {
                    Datenreihe r = m.Reihen[i];
                    if (r.Art != Reihenart.Punkte)
                    { e.Maengel.Add("Reihe ist keine Punktwolke: " + r.Name); continue; }

                    string d = Attributwert(pfade[i], "d") ?? "";
                    int n = Segmentzahl(d);
                    segmente += n;
                    if (n != r.Werte.Length)
                        e.Maengel.Add("Punkte gebuendelt: " + n + " statt " + r.Werte.Length +
                                      " (" + r.Name + ")");
                    if (Attributwert(pfade[i], "stroke-linecap") != "round")
                        e.Maengel.Add("Punktwolke ohne runde Strichkappe: " + r.Name);
                    if (Attributwert(pfade[i], "fill") != "none")
                        e.Maengel.Add("Punktwolke mit Fuellung: " + r.Name);
                    if (Attributwert(pfade[i], "stroke-width") != "5")
                        e.Maengel.Add("Punktdurchmesser ist nicht 5: " +
                                      Attributwert(pfade[i], "stroke-width"));
                }
                e.Groesse = segmente.ToString("N0", CultureInfo.InvariantCulture);
            });
        }

        /// <summary>Wie viele Segmente <c>M …,… h 0</c> der Pfad führt.</summary>
        private static int Segmentzahl(string d)
        {
            int n = 0, i = 0;
            while ((i = d.IndexOf("h 0", i, StringComparison.Ordinal)) >= 0) { n++; i += 3; }
            return n;
        }

        /// <summary>
        /// <b>Die Achsenteilung je Achsenart</b> (Entscheid DG-E3-4). Die Oberfläche
        /// setzt damit beim Zoom die Marken der x-Achse neu; geprüft wird, dass jede
        /// Achsenart ihre eigene Teilung liefert und dass die Marken im Fenster liegen.
        /// </summary>
        private static void GruppeBAchsenteilung()
        {
            SvgProbe("svg_achsenteilung", e =>
            {
                var bild = new Rahmen(0, 0, 1000, 400);

                var stunden = new Zeichenflaeche(bild, new Datenfenster(0, 8759, 0, 100));
                var index = new Zeichenflaeche(bild, new Datenfenster(0, 167, 0, 100),
                                               Achsenart.Index);
                var wert = new Zeichenflaeche(bild, new Datenfenster(-20, 20, 0, 100),
                                              Achsenart.Wert, "°C");

                IReadOnlyList<(double Wert, string Text)> ts =
                    ChartRenderer.Achsenteilung(stunden, 3000, 3500);
                IReadOnlyList<(double Wert, string Text)> ti =
                    ChartRenderer.Achsenteilung(index, 0, 12);
                IReadOnlyList<(double Wert, string Text)> tw =
                    ChartRenderer.Achsenteilung(wert, -18.2, 20.3);

                e.Masse = ts.Count + "/" + ti.Count + "/" + tw.Count;
                e.Knoten = tw.Count == 0 ? "-" : tw[0].Text + "…" + tw[tw.Count - 1].Text;
                e.Groesse = "-";

                if (ts.Count == 0 || ti.Count == 0 || tw.Count == 0)
                { e.Maengel.Add("eine Achsenart liefert keine Marke"); return; }

                Grenzen(e, "Stunden", ts, 3000, 3500);
                Grenzen(e, "Index", ti, 0, 12);
                Grenzen(e, "Wert", tw, -18.2, 20.3);

                // Eine INDEXACHSE traegt nur GANZE Stufen - zwischen zwei Stuetzstellen
                // steht kein Wert.
                foreach ((double w, string _) in ti)
                    if (w != Math.Floor(w)) e.Maengel.Add("Indexmarke ist nicht ganzzahlig: " + w);

                // Die WERTACHSE laeuft auf runden Stufen (hier 10 °C ab -10).
                if (tw.Count < 2) e.Maengel.Add("die Wertachse traegt weniger als zwei Marken");
                else if (Math.Abs((tw[1].Wert - tw[0].Wert) - Skala.Rund((20.3 + 18.2) / 5.0)) > 1e-9)
                    e.Maengel.Add("die Wertachse nimmt nicht die runde Stufe");

                // Ein Fenster ohne Breite hat nichts zu teilen, und ohne Flaeche gibt es
                // keine Achse.
                if (ChartRenderer.Achsenteilung(wert, 5, 5).Count != 0)
                    e.Maengel.Add("ein Fenster ohne Breite liefert Marken");
                if (ChartRenderer.Achsenteilung(null, 0, 10).Count != 0)
                    e.Maengel.Add("ohne Flaeche liefert die Teilung Marken");
            });
        }

        /// <summary>Jede Marke liegt im Fenster, und sie stehen aufsteigend.</summary>
        private static void Grenzen(SvgErgebnis e, string art,
                                    IReadOnlyList<(double Wert, string Text)> marken,
                                    double von, double bis)
        {
            for (int i = 0; i < marken.Count; i++)
            {
                if (marken[i].Wert < von - 1e-9 || marken[i].Wert > bis + 1e-9)
                    e.Maengel.Add(art + ": Marke " + marken[i].Wert + " liegt ausserhalb");
                if (i > 0 && marken[i].Wert <= marken[i - 1].Wert)
                    e.Maengel.Add(art + ": die Marken stehen nicht aufsteigend");
                if (string.IsNullOrEmpty(marken[i].Text))
                    e.Maengel.Add(art + ": eine Marke ohne Beschriftung");
            }
        }
    }
}
