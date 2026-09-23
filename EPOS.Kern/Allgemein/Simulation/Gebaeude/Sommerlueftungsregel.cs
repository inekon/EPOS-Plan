using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Sommerlüftungsregel</b> (Stufe G2; Konzept 4.4, Rechenschritte 7.2, Festlegung
    /// F-P4): In einer Stunde mit Sommerlüftung steigt der Luftwechsel auf 2,0 1/h — der
    /// Anwender öffnet die Fenster, wenn es drinnen warm und draußen kühler ist.
    ///
    /// <para><b>Wann sie schaltet.</b> Ausgewertet wird <b>einmal je Stunde am
    /// Stundenbeginn</b> mit Raumluft- und Außentemperatur der <b>Vorstunde</b>; der Zustand
    /// gilt die ganze Stunde. Die Luft hängt von dem Luftwechsel ab, den die Regel erst wählt —
    /// deshalb die Vorstunde und kein Umschalten innerhalb der Stunde, und deshalb braucht die
    /// Regel im Löser kein eigenes Verletzungsmaß.</para>
    ///
    /// <list type="bullet">
    /// <item><b>Ein</b>, wenn θ_air &gt; 23 °C und θ_out &lt; θ_air − 2 K.</item>
    /// <item><b>Aus</b> erst mit 1 K Abstand (Hysterese): θ_air &lt; 22 °C oder
    /// θ_out &gt; θ_air − 1 K. Dazwischen bleibt der Zustand der Vorstunde.</item>
    /// <item>Die Mindestverweildauer von einer Stunde folgt aus der stündlichen Auswertung.</item>
    /// <item>Ohne Vorstunde (erste Stunde des Vorlaufs) ist die Regel aus.</item>
    /// </list>
    ///
    /// <para>Die Schwelle ist bis KU1 fest 23 °C; ab KU1 wird sie θ_kuehl − 3 K. Die
    /// Hysterese auf den Außenabstand ist eine benannte Festlegung des Klassenwegs
    /// (Rechenschritte 7.2): Ohne sie schaltete die Regel an einem Abend mit gerade 2 K
    /// Abstand Stunde für Stunde hin und her.</para>
    ///
    /// <para>Ohne Datenbank, ohne Statik; ein Exemplar je Lauf eines Gebäudes.</para>
    /// </summary>
    internal sealed class Sommerlueftungsregel
    {
        private readonly double _schwelle;
        private bool _aktiv;

        /// <summary>Die Regel mit der festen Schwelle der Stufe G2 (23 °C).</summary>
        internal Sommerlueftungsregel()
            : this(GebaeudeFestwerte.SOMMERLUEFTUNG_SCHWELLE)
        {
        }

        /// <summary>Die Regel mit eigener Einschaltschwelle [°C] (ab KU1 θ_kuehl − 3 K).</summary>
        internal Sommerlueftungsregel(double schwelle)
        {
            if (double.IsNaN(schwelle) || double.IsInfinity(schwelle))
                throw new ArgumentOutOfRangeException(nameof(schwelle));
            _schwelle = schwelle;
        }

        /// <summary>Ist die Sommerlüftung in der laufenden Stunde eingeschaltet?</summary>
        internal bool Aktiv => _aktiv;

        /// <summary>Schaltet die Regel aus — zu Beginn eines Laufs.</summary>
        internal void Zuruecksetzen() => _aktiv = false;

        /// <summary>
        /// Bestimmt den Zustand der kommenden Stunde aus Raumluft- und Außentemperatur der
        /// Vorstunde [°C]. NaN (keine Vorstunde) schaltet aus.
        /// </summary>
        internal bool Stunde(double thetaAirVorstunde, double thetaOutVorstunde)
        {
            if (double.IsNaN(thetaAirVorstunde) || double.IsNaN(thetaOutVorstunde))
            {
                _aktiv = false;
                return false;
            }

            double abstand = thetaAirVorstunde - thetaOutVorstunde;
            if (!_aktiv)
                _aktiv = thetaAirVorstunde > _schwelle
                         && abstand > GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_AUSSEN;
            else
                _aktiv = !(thetaAirVorstunde < _schwelle - GebaeudeFestwerte.SOMMERLUEFTUNG_HYSTERESE
                           || abstand < GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_AUSSEN - GebaeudeFestwerte.SOMMERLUEFTUNG_HYSTERESE);
            return _aktiv;
        }
    }
}
