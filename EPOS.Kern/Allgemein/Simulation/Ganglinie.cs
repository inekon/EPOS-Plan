using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die beiden Rechenregeln der Ergebnis-Ganglinien: Dauerlinie und Anzeigewerte.
    ///
    /// <b>Woher sie kommen.</b> Bis iU9‑W11a standen sie in
    /// <c>WindowsFormsApplication1/Views/Simulation/GanglinienDarstellung.cs</c>, zusammen
    /// mit <c>Stapeltyp</c>/<c>StapelEinstellen</c>. Die beiden letzten arbeiten auf einer
    /// WinForms-<c>Series</c> und bleiben deshalb dort; die beiden hier rechnen nur auf
    /// <c>float[]</c> und gehören in den Kern — der Renderer (B1/B2) und die
    /// Razor-Ergebnisseite brauchen sie ebenso wie die WinForms-Masken.
    ///
    /// Reine Darstellung: hier wird nichts gerechnet, was in ein Ergebnis einginge, und
    /// keine Quellganglinie verändert — <see cref="Dauerlinie"/> arbeitet auf einer Kopie.
    /// </summary>
    public static class Ganglinie
    {
        /// <summary>
        /// Jahresdauerlinie: eine Kopie des Vektors, absteigend sortiert.
        ///
        /// Sortiert wird JEDE SERIE FÜR SICH — die Stunde i der einen Serie hat danach
        /// mit der Stunde i der anderen nichts mehr zu tun. Die Kopie schützt den
        /// Originalvektor, mit dem CSV-Export, Skalierung und das Zurückschalten in die
        /// chronologische Darstellung weiterarbeiten.
        /// </summary>
        public static float[] Dauerlinie(float[] werte)
        {
            if (werte == null) return null;

            float[] kopie = (float[])werte.Clone();
            Array.Sort(kopie);
            Array.Reverse(kopie);
            return kopie;
        }

        /// <summary>
        /// Werte in der aktuellen Darstellungsform. Ohne <paramref name="sortiert"/>
        /// kommt der ORIGINALVEKTOR zurück (keine Kopie) — das Zurückschalten stellt
        /// damit bitgleich denselben Kurvenverlauf her.
        /// </summary>
        public static float[] Anzeigewerte(float[] werte, bool sortiert)
        {
            return sortiert ? Dauerlinie(werte) : werte;
        }
    }
}
