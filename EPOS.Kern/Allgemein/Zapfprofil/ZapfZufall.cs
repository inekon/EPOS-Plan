using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der portable Zufall des Zapfprofilgenerators</b> (Umsetzungskonzept Zapfprofilgenerator
    /// 4.4, Frage ZU8): ein ganzzahliger Generator mit festem Algorithmus — SplitMix64 zum Säen,
    /// xoshiro256** zum Ziehen —, damit derselbe Seed auf Windows und iOS dieselbe Folge liefert.
    ///
    /// <para><b>Bitgleichheit.</b> Die Ziehung kennt nur ganzzahlige Operationen, Vergleiche und
    /// die vier Grundrechenarten auf <c>double</c>; keine transzendente Funktion (kein
    /// <c>Math.Log</c>, <c>Math.Exp</c>, <c>Math.Cos</c>, <c>Math.Sin</c>), deren Ergebnis von der
    /// Mathematik-Bibliothek der Plattform abhinge:</para>
    /// <list type="bullet">
    ///   <item><see cref="Gleich"/>: die oberen 53 Bit mal 2⁻⁵³ — exakt, Werte in [0; 1).</item>
    ///   <item><see cref="Ganzzahl"/>: gleichverteilt in [0; n) nach Lemire (Produkt der
    ///   64-Bit-Zahlen, Verwerfen des verzerrten Rests).</item>
    ///   <item><see cref="Normal"/>: <c>z = Σ_{j=1..12} u_j − 6</c> in fester Folge (4.4) —
    ///   Mittel 0, Varianz 1, auf [−6; 6) beschränkt.</item>
    ///   <item><see cref="Exponential"/>: das Verfahren von J. von Neumann (1951) — nur
    ///   Vergleiche gleichverteilter Zahlen, Mittel 1.</item>
    ///   <item><see cref="Poisson"/>: die Zahl der Ankünfte eines Poisson-Prozesses der Rate 1
    ///   in [0; λ), aus exponentiellen Abständen in fester Summationsfolge.</item>
    /// </list>
    ///
    /// <para><b>Seed-Disziplin (4.4).</b> Realisierung r bekommt den Seed
    /// <c>SplitMix64(SplitMix64(Seed) ⊕ r)</c> (<see cref="Realisierungsseed"/>), jede Zone und jede Einheit
    /// darin einen abgeleiteten (<see cref="Kindseed"/>) — unabhängig von der Reihenfolge, in
    /// der Fäden die Realisierungen rechnen. Die Konstanten sind die des Algorithmus, keine
    /// Normzahlen. Nicht fadensicher: jede Ziehfolge gehört einem Faden.</para>
    /// </summary>
    internal sealed class ZapfZufall
    {
        /// <summary>Schrittweite von SplitMix64 (2⁶⁴ / goldener Schnitt) — Konstante des Algorithmus.</summary>
        internal const ulong SCHRITT = 0x9E3779B97F4A7C15UL;

        /// <summary>Erster Mischfaktor von SplitMix64 — Konstante des Algorithmus.</summary>
        private const ulong MISCH1 = 0xBF58476D1CE4E5B9UL;

        /// <summary>Zweiter Mischfaktor von SplitMix64 — Konstante des Algorithmus.</summary>
        private const ulong MISCH2 = 0x94D049BB133111EBUL;

        /// <summary>2⁻⁵³ — eine Zweierpotenz, das Produkt mit einer 53-Bit-Zahl ist exakt.</summary>
        private const double ZWEI_HOCH_MINUS_53 = 1.0 / 9007199254740992.0;

        /// <summary>Summanden der Normalziehung (4.4: zwölf Gleichverteilte).</summary>
        internal const int NORMAL_SUMMANDEN = 12;

        /// <summary>Versatz der Normalziehung: das Mittel der zwölf Summanden.</summary>
        internal const double NORMAL_VERSATZ = 6.0;

        private ulong _s0, _s1, _s2, _s3;

        /// <summary>
        /// Ein Generator zum <paramref name="seed"/>: die vier Zustandswörter von xoshiro256** sind
        /// die ersten vier Ausgaben von SplitMix64 ab dem Seed (Vorgehen der Autoren des Verfahrens).
        /// </summary>
        internal ZapfZufall(ulong seed)
        {
            ulong zustand = seed;
            _s0 = SplitMix64(ref zustand);
            _s1 = SplitMix64(ref zustand);
            _s2 = SplitMix64(ref zustand);
            _s3 = SplitMix64(ref zustand);
            // SplitMix64 ist eine Bijektion des Zustands; vier Nullen in Folge liefert es nicht.
            // Die Prüfung hält den verbotenen Zustand von xoshiro256** trotzdem ausdrücklich fern.
            if ((_s0 | _s1 | _s2 | _s3) == 0UL) _s0 = SCHRITT;
        }

        // =================================================================================
        // Säen
        // =================================================================================

        /// <summary>Ein Schritt von SplitMix64: Zustand weiterzählen, Ausgabe mischen.</summary>
        internal static ulong SplitMix64(ref ulong zustand)
        {
            unchecked
            {
                zustand += SCHRITT;
                ulong z = zustand;
                z = (z ^ (z >> 30)) * MISCH1;
                z = (z ^ (z >> 27)) * MISCH2;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Die erste Ausgabe von SplitMix64 ab dem Zustand <paramref name="x"/> — <c>SplitMix64(x)</c> des Papiers.</summary>
        internal static ulong Mischen(ulong x)
        {
            ulong z = x;
            return SplitMix64(ref z);
        }

        /// <summary>
        /// Der Seed der Realisierung <paramref name="realisierung"/>: <c>SplitMix64(SplitMix64(Seed) ⊕ r)</c>.
        /// Der Seed wird vor dem ⊕ gemischt — mit <c>SplitMix64(Seed ⊕ r)</c> (4.4) teilten sich
        /// benachbarte Seeds ihre Realisierungen (Seed 1 mit r = 0 wäre Seed 0 mit r = 1, die
        /// Ensembles der Seeds 1 und 2 bestünden aus denselben Jahren).
        /// </summary>
        internal static ulong Realisierungsseed(long seed, int realisierung)
        {
            if (realisierung < 0) throw new ArgumentOutOfRangeException(nameof(realisierung));
            return Mischen(Mischen(unchecked((ulong)seed)) ^ (ulong)realisierung);
        }

        /// <summary>
        /// Ein abgeleiteter Seed — etwa der Zone <paramref name="index"/> einer Realisierung oder
        /// der Einheit <paramref name="index"/> einer Zone: <c>SplitMix64(Basis ⊕ Index)</c>.
        /// </summary>
        internal static ulong Kindseed(ulong basis, int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            return Mischen(basis ^ (ulong)index);
        }

        // =================================================================================
        // Ziehen
        // =================================================================================

        /// <summary>Die nächste 64-Bit-Zahl von xoshiro256**.</summary>
        internal ulong Naechste()
        {
            unchecked
            {
                ulong ergebnis = Links(_s1 * 5UL, 7) * 9UL;
                ulong t = _s1 << 17;
                _s2 ^= _s0;
                _s3 ^= _s1;
                _s1 ^= _s2;
                _s0 ^= _s3;
                _s2 ^= t;
                _s3 = Links(_s3, 45);
                return ergebnis;
            }
        }

        /// <summary>Eine gleichverteilte Zahl in [0; 1): die oberen 53 Bit mal 2⁻⁵³ (exakt).</summary>
        internal double Gleich() => (Naechste() >> 11) * ZWEI_HOCH_MINUS_53;

        /// <summary>
        /// Eine gleichverteilte ganze Zahl in [0; <paramref name="n"/>) nach Lemire: das obere Wort
        /// des 128-Bit-Produkts, ein verzerrter Rest wird verworfen und neu gezogen.
        /// </summary>
        internal int Ganzzahl(int n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Die Obergrenze muss positiv sein.");
            ulong m = (ulong)n;
            ulong oben = Math.BigMul(Naechste(), m, out ulong unten);
            if (unten < m)
            {
                ulong schwelle = unchecked(0UL - m) % m;
                while (unten < schwelle) oben = Math.BigMul(Naechste(), m, out unten);
            }
            return (int)oben;
        }


        /// <summary>
        /// Eine näherungsweise standardnormalverteilte Zahl (4.4): <c>z = Σ_{j=1..12} u_j − 6</c>,
        /// in fester Folge summiert — Mittel 0, Varianz 1, Werte in [−6; 6).
        /// </summary>
        internal double Normal()
        {
            double s = 0.0;
            for (int j = 0; j < NORMAL_SUMMANDEN; j++) s += Gleich();
            return s - NORMAL_VERSATZ;
        }

        /// <summary>
        /// Eine exponentialverteilte Zahl mit Mittel 1 nach J. von Neumann (1951), ohne Logarithmus:
        /// Ziehe u₀ und so lange weiter, wie die Folge u₀ &gt; u₁ &gt; … fällt; hat der fallende Lauf
        /// ungerade Länge, gilt <c>k + u₀</c>, sonst <c>k + 1</c> und von vorn. Die Annahme hat die
        /// Wahrscheinlichkeit e^(−u₀), die Zahl der Neubeginne ist geometrisch — zusammen Exp(1).
        /// </summary>
        internal double Exponential()
        {
            double k = 0.0;
            while (true)
            {
                double u0 = Gleich();
                double u = u0;
                int laenge = 1;
                while (true)
                {
                    double v = Gleich();
                    if (v < u)
                    {
                        u = v;
                        laenge++;
                    }
                    else break;
                }
                if ((laenge & 1) == 1) return k + u0;
                k += 1.0;
            }
        }

        /// <summary>
        /// Eine Poisson-verteilte Zahl mit Mittel <paramref name="lambda"/>: die Ankünfte eines
        /// Poisson-Prozesses der Rate 1 vor λ, aus exponentiellen Abständen (<see cref="Exponential"/>)
        /// in fester Folge summiert. λ ≤ 0 ergibt 0 ohne Ziehung; ein nicht endliches λ wird abgelehnt.
        /// </summary>
        internal int Poisson(double lambda)
        {
            if (double.IsNaN(lambda) || double.IsInfinity(lambda))
                throw new ArgumentOutOfRangeException(nameof(lambda), "Das Mittel der Poisson-Ziehung ist keine endliche Zahl.");
            if (!(lambda > 0.0)) return 0;
            int n = 0;
            double t = Exponential();
            while (t < lambda)
            {
                n++;
                t += Exponential();
            }
            return n;
        }

        private static ulong Links(ulong x, int k) => (x << k) | (x >> (64 - k));
    }
}
