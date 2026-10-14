using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Ankunftsbezug (b) eines Gebäudes mit Heizkreis</b> (Welle V3b, <see cref="Vorheizankunftsbezug.Uebergabe"/>): Die
    /// Ankunft am Beginn von h_s gilt gegen min(θ_T, θ_stat) − ε statt gegen θ_T − ε. θ_stat ist die Raumluft, die die Übergabe am
    /// <b>durchgewärmten</b> Bau zur Kalenderstunde h_s hält — das Gleichgewicht des Zonenmodells, kein Lauf.
    ///
    /// <para><b>Herleitung.</b> Zwischen zwei Stunden trägt das <see cref="Zonenmodell2K"/> nur die beiden Massentemperaturen
    /// x = (θ_m,AW, θ_m,IW); eine Stunde mit dem Rand r ist die Abbildung x ↦ M_r(x) (<see cref="Zonenmodell2K.Schritt"/> mit
    /// Sollwert θ_T, der Grenze der Stunde, Außenluft, Sonne, Gewinnen, Lüftung, Erdreich und dem Heizkurvenvorlauf samt
    /// P-Regler der Übergabe von h_s). Der durchgewärmte Bau unter diesem festgehaltenen Rand ist ihr Fixpunkt x* = M_r(x*):
    /// Dort ändern sich die Massen über die Stunde nicht, Übergabe und Verluste halten sich die Waage. Der Fixpunkt wird mit dem
    /// Newton-Verfahren auf F(x) = M_r(x) − x gelöst (Jacobi-Matrix 2 × 2 aus Differenzenquotienten); der lineare Teil des
    /// Modells macht F stückweise affin, das Verfahren endet in wenigen Schritten. θ_stat ist die Raumluft am Beginn der Stunde in
    /// x* (<see cref="Zonenmodell2K.LuftAmBeginn"/>, dieselbe Regel wie die Ankunft). Begrenzt nichts, ist θ_stat = θ_T (der
    /// geregelte Fall); begrenzt die Übergabe am Heizkurvenvorlauf, liegt θ_stat darunter — die Ankunft fordert dann nicht mehr,
    /// als die Übergabe am warmen Bau überhaupt hält.</para>
    /// </summary>
    internal static class Vorheizankunft
    {
        private const int NEWTON_MAX = 40;
        private const double DIFFERENZ_K = 1e-3;
        private const double KONVERGENZ_K = 1e-9;
        private const int ITERATION_MAX = 100_000;

        /// <summary>Der Bezug der Ankunft [°C]: θ_T, mit (b) an einer Zone mit Heizkreis min(θ_T, θ_stat).</summary>
        /// <param name="modell">Ein Rechenmodell der Zone (sein Zustand wird überschrieben).</param>
        /// <param name="r">Der Rand von h_s, wie die Ankunft ihn prüft.</param>
        /// <param name="awStart">Startwert der Außenmasse [°C] (der Zustand vor h_s).</param>
        /// <param name="iwStart">Startwert der Innenmasse [°C].</param>
        internal static double Bezug(Vorheizankunftsbezug bezug, GebaeudeModellEingang e, Zonenmodell2K modell, in Stundenrand r,
                                     double thetaTC, double awStart, double iwStart)
        {
            if (bezug != Vorheizankunftsbezug.Uebergabe || e == null || !e.KopplungWirksam) return thetaTC;
            double stat = StationaereLuft(modell, in r, awStart, iwStart);
            return double.IsNaN(stat) || stat >= thetaTC ? thetaTC : stat;
        }

        /// <summary>
        /// <b>θ_stat</b> [°C]: die Raumluft am Beginn der Stunde im Gleichgewicht des Modells unter dem festgehaltenen Rand
        /// <paramref name="r"/> (Fixpunkt der Stundenabbildung, Newton; ohne Konvergenz die einfache Iteration).
        /// </summary>
        internal static double StationaereLuft(Zonenmodell2K modell, in Stundenrand r, double awStart, double iwStart)
        {
            if (modell == null) throw new ArgumentNullException(nameof(modell));
            double x = awStart, y = iwStart;
            bool fertig = false;
            for (int it = 0; it < NEWTON_MAX && !fertig; it++)
            {
                (double fx, double fy) = Rest(modell, in r, x, y);
                if (Math.Abs(fx) < KONVERGENZ_K && Math.Abs(fy) < KONVERGENZ_K)
                {
                    fertig = true;
                    break;
                }
                (double ax, double ay) = Rest(modell, in r, x + DIFFERENZ_K, y);
                (double bx, double by) = Rest(modell, in r, x, y + DIFFERENZ_K);
                double j00 = (ax - fx) / DIFFERENZ_K, j10 = (ay - fy) / DIFFERENZ_K;
                double j01 = (bx - fx) / DIFFERENZ_K, j11 = (by - fy) / DIFFERENZ_K;
                double det = j00 * j11 - j01 * j10;
                if (Math.Abs(det) < 1e-14 || double.IsNaN(det)) break;
                x -= (j11 * fx - j01 * fy) / det;
                y -= (-j10 * fx + j00 * fy) / det;
            }
            if (!fertig)
                for (int it = 0; it < ITERATION_MAX; it++)
                {
                    (double fx, double fy) = Rest(modell, in r, x, y);
                    x += fx;
                    y += fy;
                    if (Math.Abs(fx) < KONVERGENZ_K && Math.Abs(fy) < KONVERGENZ_K) break;
                }
            modell.Zuruecksetzen(x, y);
            return modell.LuftAmBeginn(in r);
        }

        private static (double, double) Rest(Zonenmodell2K modell, in Stundenrand r, double x, double y)
        {
            modell.Zuruecksetzen(x, y);
            modell.Schritt(in r);
            return (modell.ThetaMAw - x, modell.ThetaMIw - y);
        }
    }
}
