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
    /// <para><b>Mit Kühlkalender ist die Schwelle eine Reihe</b> (Stufe KP1b, Konzept
    /// Konditionierungsprofile 3.6): θ_K(h) − 3 K, wo θ_K(h) endlich ist, sonst — Kühlung „aus",
    /// +∞ — die feste Schwelle 23 °C. Der Konstruktor mit der Reihe und
    /// <see cref="Stunde(int, double, double)"/> gelten nur dann; ohne Kühlkalender bleibt jeder
    /// Aufruf bei der einen Zahl <see cref="SchwelleDerStunde"/> = <c>_schwelle</c>, also wörtlich
    /// beim Bestandsausdruck.</para>
    ///
    /// <para><b>Der Außenabstand ΔT steht als Feld</b> (Stufe KP1b, Konzept 3.7, P9): Die
    /// Sommerlüftung nimmt die Konstante <see cref="GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_AUSSEN"/>
    /// = 2 K, die <b>Nachtauskühlung</b> ihr <c>Bedingt_K</c> (0 … 5 K, leer = 2 K). Er wirkt an
    /// genau EINER Stelle — <see cref="Schalten"/> —, Ein- und Ausschaltseite; die Hysterese von
    /// 1 K bleibt dieselbe.</para>
    ///
    /// <para>Ohne Datenbank, ohne Statik; ein Exemplar je Lauf eines Gebäudes (ab G6b je Zone), ab
    /// KP1b ein zweites je Zone für die Nachtauskühlung.</para>
    /// </summary>
    internal sealed class Sommerlueftungsregel
    {
        private readonly double _schwelle;
        private readonly double[] _kuehlsollwerteC;
        private readonly double _abstandAussenK;
        private bool _aktiv;

        /// <summary>Die Regel mit der festen Schwelle der Stufe G2 (23 °C).</summary>
        internal Sommerlueftungsregel()
            : this(GebaeudeFestwerte.SOMMERLUEFTUNG_SCHWELLE)
        {
        }

        /// <summary>
        /// Die Regel mit eigener Einschaltschwelle [°C] (ab KU1 θ_kuehl − 3 K) und, ab KP1b, eigenem
        /// Außenabstand ΔT [K] — die Vorgabe ist die Konstante der Sommerlüftung (2 K).
        /// </summary>
        internal Sommerlueftungsregel(double schwelle,
                                      double abstandAussenK = GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_AUSSEN)
        {
            if (double.IsNaN(schwelle) || double.IsInfinity(schwelle))
                throw new ArgumentOutOfRangeException(nameof(schwelle));
            if (!double.IsFinite(abstandAussenK) || abstandAussenK < 0.0)
                throw new ArgumentOutOfRangeException(nameof(abstandAussenK));
            _schwelle = schwelle;
            _abstandAussenK = abstandAussenK;
        }

        /// <summary>
        /// <b>Die Regel mit der Kühlsollwertreihe</b> (Stufe KP1b, Konzept 3.6): Die Schwelle einer
        /// Stunde ist θ_K(h) − 3 K, wo θ_K(h) endlich ist, sonst die feste Schwelle 23 °C. Sie
        /// gehört zum Kühlkalender; ohne ihn gilt der Konstruktor mit der einen Zahl.
        /// </summary>
        /// <param name="kuehlsollwerteC">Die obere Regelgrenze je Stunde [°C] (<c>ThetaMax</c>); +∞ heißt „aus".</param>
        /// <param name="abstandAussenK">
        /// Der Außenabstand ΔT [K]; Vorgabe die Konstante der Sommerlüftung (2 K). Die
        /// Nachtauskühlung (KP1b, P9) setzt hier ihr <c>Bedingt_K</c> ein.
        /// </param>
        internal Sommerlueftungsregel(double[] kuehlsollwerteC,
                                      double abstandAussenK = GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_AUSSEN)
        {
            _kuehlsollwerteC = kuehlsollwerteC ?? throw new ArgumentNullException(nameof(kuehlsollwerteC));
            if (!double.IsFinite(abstandAussenK) || abstandAussenK < 0.0)
                throw new ArgumentOutOfRangeException(nameof(abstandAussenK));
            _schwelle = GebaeudeFestwerte.SOMMERLUEFTUNG_SCHWELLE;
            _abstandAussenK = abstandAussenK;
        }

        /// <summary>Ist die Sommerlüftung in der laufenden Stunde eingeschaltet?</summary>
        internal bool Aktiv => _aktiv;

        /// <summary>Schaltet die Regel aus — zu Beginn eines Laufs.</summary>
        internal void Zuruecksetzen() => _aktiv = false;

        /// <summary>
        /// Die Einschaltschwelle der Stunde <paramref name="h"/> [°C]: ohne Kühlkalender die eine
        /// Zahl des Konstruktors, mit ihm θ_K(h) − 3 K bzw. 23 °C bei „aus" (Konzept 3.6).
        /// </summary>
        internal double SchwelleDerStunde(int h)
        {
            if (_kuehlsollwerteC == null) return _schwelle;
            double kuehl = _kuehlsollwerteC[h];
            return double.IsNaN(kuehl) || double.IsInfinity(kuehl)
                ? GebaeudeFestwerte.SOMMERLUEFTUNG_SCHWELLE
                : kuehl - GebaeudeFestwerte.SOMMERLUEFTUNG_ABSTAND_KUEHLSOLLWERT;
        }

        /// <summary>
        /// Bestimmt den Zustand der kommenden Stunde aus Raumluft- und Außentemperatur der
        /// Vorstunde [°C]. NaN (keine Vorstunde) schaltet aus.
        /// </summary>
        internal bool Stunde(double thetaAirVorstunde, double thetaOutVorstunde)
            => Schalten(thetaAirVorstunde, thetaOutVorstunde, _schwelle);

        /// <summary>
        /// Derselbe Schaltvorgang mit der Schwelle der Stunde <paramref name="h"/>
        /// (<see cref="SchwelleDerStunde"/>) — ohne Kühlkalender ist das dieselbe Zahl und damit
        /// derselbe Ausdruck wie <see cref="Stunde(double, double)"/>.
        /// </summary>
        internal bool Stunde(int h, double thetaAirVorstunde, double thetaOutVorstunde)
            => Schalten(thetaAirVorstunde, thetaOutVorstunde, SchwelleDerStunde(h));

        /// <summary>Der eine Schaltvorgang der Regel (Ein, Hysterese, Aus) mit der Schwelle <paramref name="schwelle"/>.</summary>
        private bool Schalten(double thetaAirVorstunde, double thetaOutVorstunde, double schwelle)
        {
            if (double.IsNaN(thetaAirVorstunde) || double.IsNaN(thetaOutVorstunde))
            {
                _aktiv = false;
                return false;
            }

            double abstand = thetaAirVorstunde - thetaOutVorstunde;
            if (!_aktiv)
                _aktiv = thetaAirVorstunde > schwelle
                         && abstand > _abstandAussenK;
            else
                _aktiv = !(thetaAirVorstunde < schwelle - GebaeudeFestwerte.SOMMERLUEFTUNG_HYSTERESE
                           || abstand < _abstandAussenK - GebaeudeFestwerte.SOMMERLUEFTUNG_HYSTERESE);
            return _aktiv;
        }
    }
}
