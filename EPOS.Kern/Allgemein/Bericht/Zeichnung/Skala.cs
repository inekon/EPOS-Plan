using System;

namespace WindowsFormsApplication1.Zeichnung
{
    /// <summary>
    /// Die Achsenstufungen des Berichts an EINER Stelle (Etappe E1).
    ///
    /// <para><b>Vier benannte Varianten, nicht eine Rechnung.</b> Der Bestand hat
    /// fünf Skalenrechnungen, und sie liefern nicht dasselbe: <see cref="Nice"/>
    /// rundet nur die Obergrenze, <see cref="Rund"/> die Schrittweite,
    /// <see cref="Stufe"/> zusätzlich die Untergrenze, und <see cref="Bedarf"/>
    /// kennt als einzige Zehntelschritte. Sie sind hier zusammengezogen und
    /// benannt, aber NICHT vereinheitlicht: Jede Vereinfachung, die ein Ergebnis
    /// verschöbe, verschöbe ein Bild. Die Hash-Messlatte der ChartProben lässt das
    /// nicht zu.</para>
    /// </summary>
    public static class Skala
    {
        /// <summary>
        /// Die nächstgrößere „runde" Schrittweite (1 / 2 / 2,5 / 5 × 10^k) — die
        /// Stufenfolge der Kennlinien, der Schnitt- und Stückzahlkurven, der
        /// Streuwolke, der Jahresprojektion und der gefensterten x-Achse.
        /// </summary>
        public static double Rund(double roh)
        {
            if (roh <= 0 || double.IsNaN(roh) || double.IsInfinity(roh)) return 1;
            double zehner = Math.Pow(10, Math.Floor(Math.Log10(roh)));
            foreach (double f in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 })
                if (zehner * f >= roh) return zehner * f;
            return zehner * 10.0;
        }

        /// <summary>„Schöne" Achsen-Obergrenze (1 / 2 / 2,5 / 5 × 10^k).</summary>
        public static double Nice(double max)
        {
            if (max <= 0) return 1;
            double exp = Math.Pow(10, Math.Floor(Math.Log10(max)));
            double f = max / exp;
            double nf = f <= 1 ? 1 : f <= 2 ? 2 : f <= 2.5 ? 2.5 : f <= 5 ? 5 : 10;
            return nf * exp;
        }

        /// <summary>
        /// Die Achsenstufung der Liniendiagramme mit fünf Rasterlinien: rundet
        /// <paramref name="min"/> ab und <paramref name="max"/> auf und liefert die
        /// Schrittweite. Der Kapitalwert-Verlauf und die Kennlinien nehmen sie.
        /// </summary>
        public static double Stufe(ref double min, ref double max)
        {
            if (max - min < 1e-9) max = min + 1;
            double roh = (max - min) / 5.0;
            double zehner = Math.Pow(10, Math.Floor(Math.Log10(roh)));
            double schritt = zehner;
            foreach (double f in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 })
                if (zehner * f >= roh) { schritt = zehner * f; break; }
            min = Math.Floor(min / schritt) * schritt;
            max = Math.Ceiling(max / schritt) * schritt;
            return schritt;
        }

        /// <summary>
        /// Die „schönen" Schrittweiten der Bedarfsmasken. Wörtlich aus
        /// <c>Form_ErgBrauchwasserwaerme.SkaliereYAchse</c>:288 — eine andere Reihe
        /// als die der übrigen Bilder (dort 1/2/2,5/5/10), weil die Bedarfsbilder
        /// auch Zehntel brauchen.
        /// </summary>
        private static readonly double[] SCHOENE_SCHRITTE =
        { 0.1, 0.2, 0.25, 0.5, 1.0, 2.0, 2.5, 5.0, 10.0 };

        /// <summary>
        /// Die y-Achse der Bedarfsbilder: Schrittweite, Obergrenze und Zahlenformat
        /// aus dem Größtwert. Wörtlich aus <c>SkaliereYAchse</c> — samt dem Rückfall
        /// „Maximum 5, Intervall 1", wenn alle Werte null sind, und der Sicherung
        /// gegen eine Schrittweite ≤ 0.
        /// </summary>
        public static (double Schritt, double Max, string Format) Bedarf(double maxWert)
        {
            if (maxWert <= 0) return (1.0, 5.0, "N0");

            double zielSchrittweite = (maxWert * 1.1) / 4.5;
            double groessenordnung = Math.Pow(10, Math.Floor(Math.Log10(zielSchrittweite)));
            double normiert = zielSchrittweite / groessenordnung;

            double gewaehlt = SCHOENE_SCHRITTE[SCHOENE_SCHRITTE.Length - 1];
            foreach (double schritt in SCHOENE_SCHRITTE)
                if (normiert <= schritt) { gewaehlt = schritt; break; }

            double finale = gewaehlt * groessenordnung;
            double obergrenze = Math.Round(Math.Ceiling((maxWert * 1.05) / finale) * finale, 4);
            if (finale <= 0) { finale = 0.5; obergrenze = 2.0; }

            string format = finale >= 1.0 ? "N0" : finale >= 0.1 ? "N1" : "N2";
            return (finale, obergrenze, format);
        }
    }
}
