using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Die EINE Regel, an der sich <see cref="IntervallKonvention.Automatisch"/>
    /// entscheidet: <b>Eine Reihe mit Intervallende beginnt genau ein Intervall
    /// nach Mitternacht des 01.01.</b>
    ///
    /// <para><b>Warum sie hier steht und nicht zweimal.</b> Die Regel lag als
    /// privater Zweig in <c>GanglinienPruefung.KonventionAnwenden</c>. Der
    /// CSV-Import der Speicher-Zeitreihen (<c>SpeicherZeitreihenImport</c>)
    /// brauchte dieselbe Entscheidung — und eine zweite Abschrift waere eine
    /// zweite Wahrheit darueber, was „automatisch" heisst. Die Pruefung ruft
    /// den Helfer jetzt selbst; ihr Verhalten bleibt unveraendert.</para>
    ///
    /// <para><b>Ortszeit, nicht UTC.</b> Die Regel ist eine Aussage ueber die
    /// WANDUHR der Quelldatei („00:15 am Neujahrstag"), nicht ueber einen
    /// Zeitpunkt. Der Aufrufer reicht deshalb den ersten Zeitstempel so herein,
    /// wie er in der Datei steht.</para>
    /// </summary>
    public static class IntervallKonventionErkennung
    {
        /// <summary>
        /// Anfang oder Ende — erkannt am ersten Zeitstempel einer Reihe.
        /// </summary>
        /// <param name="ersterZeitstempel">Der erste Zeitstempel der Reihe in der
        /// Zeitrechnung der Quelldatei (Ortszeit, nicht UTC).</param>
        /// <param name="schrittMinuten">Das erkannte Quellintervall in Minuten.</param>
        /// <returns><see cref="IntervallKonvention.Ende"/>, wenn die Reihe genau ein
        /// Intervall nach Mitternacht des 01.01. beginnt; sonst
        /// <see cref="IntervallKonvention.Anfang"/>.</returns>
        public static IntervallKonvention Erkenne(DateTime ersterZeitstempel, int schrittMinuten)
        {
            bool ende = ersterZeitstempel.Month == 1 && ersterZeitstempel.Day == 1 &&
                        Math.Abs((ersterZeitstempel - ersterZeitstempel.Date).TotalMinutes - schrittMinuten) < 0.001;
            return ende ? IntervallKonvention.Ende : IntervallKonvention.Anfang;
        }

        /// <summary>
        /// Loest eine gewuenschte Konvention auf: <see cref="IntervallKonvention.Anfang"/>
        /// und <see cref="IntervallKonvention.Ende"/> bleiben stehen, jeder andere Wert
        /// — also <see cref="IntervallKonvention.Automatisch"/> — wird erkannt.
        /// </summary>
        public static IntervallKonvention Aufloesen(
            IntervallKonvention gewuenscht, DateTime ersterZeitstempel, int schrittMinuten)
        {
            if (gewuenscht == IntervallKonvention.Ende) return IntervallKonvention.Ende;
            if (gewuenscht == IntervallKonvention.Anfang) return IntervallKonvention.Anfang;
            return Erkenne(ersterZeitstempel, schrittMinuten);
        }
    }
}
