using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Woche mit der größten Unterschreitung</b> (Anlagenkopplung 9.4, E80) — die Wahl des Ausschnitts
    /// für das Bild „Raumtemperatur und Sollwert". Gewählt wird unter allen Wochen, die an einem Tagesbeginn
    /// anfangen (Tag 0 bis 358), die Woche mit der größten Summe der Unterschreitungen (Sollwert − Raumluft) in den
    /// gezählten Stunden der Maske — dieselbe Zählung wie die Kelvinstunden (<see cref="Komfortkennzahlen"/>).
    /// Gleichstand: die frühere Woche. Gerechnet wird nur auf den Reihen; das Modul liest nichts nach.
    /// </summary>
    public static class Komfortwoche
    {
        /// <summary>Stunden einer Woche.</summary>
        public const int WOCHE = 168;

        /// <summary>
        /// Die erste Stunde der Woche mit der größten Unterschreitung; <c>-1</c>, wenn keine Stunde zählt oder eine
        /// Reihe fehlt oder kürzer als eine Woche ist.
        /// </summary>
        /// <param name="soll">Heizsollwert je Stunde [°C].</param>
        /// <param name="luft">Raumlufttemperatur je Stunde [°C].</param>
        /// <param name="maske">Die gezählten Stunden (Unterschreitung über der Schwelle in der Nutzungszeit).</param>
        public static int GroessteUnterschreitung(double[] soll, double[] luft, bool[] maske)
        {
            if (soll == null || luft == null || maske == null) return -1;
            int n = Math.Min(Math.Min(soll.Length, luft.Length), maske.Length);
            if (n < WOCHE) return -1;

            var gewicht = new double[n];
            bool irgendeine = false;
            for (int h = 0; h < n; h++)
            {
                if (!maske[h]) continue;
                double d = soll[h] - luft[h];
                // Eine gezählte Stunde wiegt mindestens etwas - so gewinnt auch ohne endliche Reihe die Woche mit
                // den meisten Stunden.
                gewicht[h] = double.IsNaN(d) || double.IsInfinity(d) ? 0.0 : Math.Max(d, 0.0) + 1e-9;
                irgendeine = true;
            }
            if (!irgendeine) return -1;

            int beste = -1;
            double besterWert = double.NegativeInfinity;
            for (int start = 0; start + WOCHE <= n; start += 24)
            {
                double summe = 0.0;
                for (int h = start; h < start + WOCHE; h++) summe += gewicht[h];
                if (summe > besterWert + 1e-12)
                {
                    besterWert = summe;
                    beste = start;
                }
            }
            return besterWert > 0.0 ? beste : -1;
        }

        /// <summary>Ein Ausschnitt von <see cref="WOCHE"/> Stunden ab <paramref name="start"/>; <c>null</c> ohne Reihe.</summary>
        public static double[] Ausschnitt(double[] reihe, int start)
        {
            if (reihe == null || start < 0 || start + WOCHE > reihe.Length) return null;
            var teil = new double[WOCHE];
            Array.Copy(reihe, start, teil, 0, WOCHE);
            return teil;
        }

        /// <summary>Derselbe Ausschnitt für die Maske.</summary>
        public static bool[] Ausschnitt(bool[] maske, int start)
        {
            if (maske == null || start < 0 || start + WOCHE > maske.Length) return null;
            var teil = new bool[WOCHE];
            Array.Copy(maske, start, teil, 0, WOCHE);
            return teil;
        }
    }
}
