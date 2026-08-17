using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Numerische Hilfsfunktionen der Engine.
    /// </summary>
    public static class Numerik
    {
        /// <summary>
        /// Summiert strikt sequenziell in <c>double</c>, ohne Kompensation und ohne
        /// Umordnung. Portierung von <c>summe_sequenziell</c> aus <c>speicher_sim.py</c>.
        /// </summary>
        /// <remarks>
        /// Excels <c>SUM(F2:F35137)</c> und <c>WorksheetFunction.Sum</c> addieren in
        /// Blattreihenfolge. Nur so wird N10 bitgenau reproduziert. Kompensierte oder
        /// paarweise Verfahren (Kahan, <c>numpy.sum</c>, LINQ <c>Enumerable.Sum</c> mit
        /// abweichender Reihenfolge) weichen um rund 1e-10 EUR ab. Deshalb hier
        /// bewusst eine nackte <c>for</c>-Schleife und kein LINQ.
        /// </remarks>
        public static double SummeSequenziell(double[] werte)
        {
            if (werte == null) throw new ArgumentNullException(nameof(werte));
            double s = 0.0;
            for (int i = 0; i < werte.Length; i++)
            {
                s += werte[i];
            }
            return s;
        }

        /// <summary>
        /// Summiert den Ausschnitt <c>[start .. start+laenge)</c> sequenziell.
        /// </summary>
        public static double SummeSequenziell(double[] werte, int start, int laenge)
        {
            if (werte == null) throw new ArgumentNullException(nameof(werte));
            if (start < 0 || start > werte.Length)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (laenge < 0 || start + laenge > werte.Length)
                throw new ArgumentOutOfRangeException(nameof(laenge));

            double s = 0.0;
            int ende = start + laenge;
            for (int i = start; i < ende; i++)
            {
                s += werte[i];
            }
            return s;
        }
    }
}
