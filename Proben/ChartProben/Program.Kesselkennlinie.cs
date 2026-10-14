using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>DIE KLEINE KURVE DES KESSELEDITORS</b> (Konzept Kesselkennlinie 5, erster Punkt):
    /// <c>ChartRenderer.KesselkennlinieModell</c> — der Wirkungsgrad über der Last, mit Brennwertkennlinie je Rücklauf
    /// (30, 50, 60 °C) eine Linie, ohne sie eine.
    ///
    /// <para><b>Die Punkte rechnet der Kern</b> (<c>Kesselkennlinie.Kurven</c>, eine reine Funktion) aus neutralen,
    /// fest verdrahteten Katalogwerten — keine Datenbank, kein Zufall: ein Gas-Brennwertkessel mit η₁₀₀ 0,97 und η₃₀ 1,07
    /// (der Median der Herstellerdateien liegt bei η₁₀₀ + 0,107), einmal mit und einmal ohne Brennwertkennlinie.</para>
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung nicht sehen: dass der Schalter
    /// Brennwertkennlinie im Bild ankommt und dass ein gepflegtes η₃₀ die Kurve gegenüber der Normvorgabe verschiebt.
    /// Die SVG-Proben prüfen das Pixelbild mit Datenreihe (Gruppe (b)) und die Werte an den Punktmarken.</para>
    /// </summary>
    internal static partial class Program
    {
        private const string KESSEL_TITEL = "Wirkungsgrad über der Last";
        private const string KESSEL_X = "Last [%]";
        private const string KESSEL_Y = "Wirkungsgrad";

        /// <summary>Die Linien eines Gas-Brennwertkessels (η₁₀₀ 0,97) aus dem Kern, benannt wie in der Oberfläche.</summary>
        private static List<ChartRenderer.KesselkennlinienReihe> Kesselreihen(bool brennwertkennlinie, double? eta30)
            => Kesselkennlinie.Kurven(0.97, 0.0, 1, true, "Brennwertkessel", eta30, brennwertkennlinie)
                .Select(k => new ChartRenderer.KesselkennlinienReihe(
                    k.RuecklaufC.HasValue
                        ? "Rücklauf " + k.RuecklaufC.Value.ToString("0", CultureInfo.InvariantCulture) + " °C"
                        : "Teillastkennlinie",
                    k.Punkte))
                .ToList();

        private static byte[] Kesselbild(bool brennwertkennlinie, double? eta30)
            => ChartRenderer.KesselkennlinieBild(KESSEL_TITEL, KESSEL_X, KESSEL_Y, Kesselreihen(brennwertkennlinie, eta30));

        private static void KesselkennlinieProben(string ziel)
        {
            int b = ChartRenderer.KESSELKENNLINIE_BREITE, h = ChartRenderer.KESSELKENNLINIE_HOEHE;

            // Maßproben: drei Rückläufe in den ersten drei Serienrollen, die Teillastkennlinie in der ersten.
            Pruefe(ziel, "kesselkennlinie_brennwert", b, h,
                   new[] { Rollenfarbe(Farbrolle.SERIE_1), Rollenfarbe(Farbrolle.SERIE_2), Rollenfarbe(Farbrolle.SERIE_3) },
                   () => Kesselbild(true, 1.07));
            Pruefe(ziel, "kesselkennlinie_teillast", b, h,
                   new[] { Rollenfarbe(Farbrolle.SERIE_1) },
                   () => Kesselbild(false, 1.07));

            // Gegenproben: der Schalter Brennwertkennlinie und das gepflegte η₃₀ kommen im Bild an.
            Unterschiedlich("kesselkennlinie_brennwert_wirkt", () => Kesselbild(true, 1.07), () => Kesselbild(false, 1.07));
            Unterschiedlich("kesselkennlinie_eta30_wirkt", () => Kesselbild(false, 1.07), () => Kesselbild(false, null));

            // SVG-Probe der Gruppe (b): Pixelbild ohne Zeichenfläche, Reihenbefehle mit Marke, Datenreihen mit x-Stelle.
            SvgPixelModellprobe("kesselkennlinie_brennwert",
                () => ChartRenderer.KesselkennlinieModell(KESSEL_TITEL, KESSEL_X, KESSEL_Y, Kesselreihen(true, 1.07)));

            // Die Werte an den Punktmarken: je Linie zehn Marken mit Last und Wirkungsgrad, die Prüfpunkte des Katalogs
            // wörtlich (30 % bei 30 °C = η₃₀, 100 % bei 60 °C = η₁₀₀); die Legende nennt jede Linie.
            SvgProbe("svg_kesselkennlinie_werte", e =>
            {
                Zeichenmodell m = ChartRenderer.KesselkennlinieModell(KESSEL_TITEL, KESSEL_X, KESSEL_Y, Kesselreihen(true, 1.07));
                e.Masse = m.Breite + "x" + m.Hoehe;
                // Der Linienzug nennt nur seinen Namen; die Werte stehen an den Punktmarken.
                var werte = SvgSchreiber.Baum(m).Alle()
                    .Where(k => (Attributwert(k, "data-marke") ?? "").StartsWith("reihe:", StringComparison.Ordinal))
                    .Select(k => Attributwert(k, "data-wert"))
                    .Where(w => w != null && w.StartsWith("Last ", StringComparison.Ordinal)).ToList();
                e.Knoten = werte.Count.ToString(CultureInfo.InvariantCulture);
                if (werte.Count != 3 * Kesselkennlinie.KURVE_STUETZSTELLEN)
                    e.Maengel.Add("Werte an den Marken: " + werte.Count + " statt " + 3 * Kesselkennlinie.KURVE_STUETZSTELLEN);
                foreach (string soll in new[] { "Last 30 % · Rücklauf 30 °C: 1,070", "Last 100 % · Rücklauf 60 °C: 0,970" })
                    if (!werte.Contains(soll)) e.Maengel.Add("Wert fehlt: " + soll);
                if (m.Reihen.Count != 3 || m.Reihen.Any(r => r.Werte.Length != Kesselkennlinie.KURVE_STUETZSTELLEN))
                    e.Maengel.Add("Datenreihen: " + m.Reihen.Count + " statt 3 zu je " + Kesselkennlinie.KURVE_STUETZSTELLEN);
                foreach (string name in new[] { "Rücklauf 30 °C", "Rücklauf 50 °C", "Rücklauf 60 °C" })
                    if (!m.Befehle.Any(bf => bf.Marke == "legende:" + name)) e.Maengel.Add("Legende ohne " + name);

                Zeichenmodell eine = ChartRenderer.KesselkennlinieModell(KESSEL_TITEL, KESSEL_X, KESSEL_Y, Kesselreihen(false, 1.07));
                if (eine.Reihen.Count != 1) e.Maengel.Add("ohne Brennwertkennlinie " + eine.Reihen.Count + " Linien statt einer");
            });
        }
    }
}
