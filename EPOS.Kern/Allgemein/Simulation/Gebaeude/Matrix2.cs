using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Spaltenvektor mit zwei Einträgen — der Zustand der beiden Massenknoten.</summary>
    internal readonly struct Vektor2
    {
        internal Vektor2(double a, double b)
        {
            A = a;
            B = b;
        }

        /// <summary>Erster Eintrag (Außenbauteilmasse).</summary>
        internal double A { get; }

        /// <summary>Zweiter Eintrag (Innenbauteilmasse).</summary>
        internal double B { get; }

        public static Vektor2 operator +(Vektor2 x, Vektor2 y) => new Vektor2(x.A + y.A, x.B + y.B);

        public static Vektor2 operator -(Vektor2 x, Vektor2 y) => new Vektor2(x.A - y.A, x.B - y.B);

        public static Vektor2 operator *(double s, Vektor2 x) => new Vektor2(s * x.A, s * x.B);
    }

    /// <summary>Eine 2×2-Matrix, zeilenweise: [[M11, M12], [M21, M22]].</summary>
    internal readonly struct Matrix2
    {
        internal Matrix2(double m11, double m12, double m21, double m22)
        {
            M11 = m11;
            M12 = m12;
            M21 = m21;
            M22 = m22;
        }

        internal double M11 { get; }
        internal double M12 { get; }
        internal double M21 { get; }
        internal double M22 { get; }

        /// <summary>Die Einheitsmatrix.</summary>
        internal static Matrix2 Einheit => new Matrix2(1.0, 0.0, 0.0, 1.0);

        /// <summary>Spur M11 + M22.</summary>
        internal double Spur => M11 + M22;

        /// <summary>Determinante M11·M22 − M12·M21.</summary>
        internal double Determinante => M11 * M22 - M12 * M21;

        public static Matrix2 operator +(Matrix2 x, Matrix2 y)
            => new Matrix2(x.M11 + y.M11, x.M12 + y.M12, x.M21 + y.M21, x.M22 + y.M22);

        public static Matrix2 operator -(Matrix2 x, Matrix2 y)
            => new Matrix2(x.M11 - y.M11, x.M12 - y.M12, x.M21 - y.M21, x.M22 - y.M22);

        public static Matrix2 operator *(double s, Matrix2 x)
            => new Matrix2(s * x.M11, s * x.M12, s * x.M21, s * x.M22);

        public static Matrix2 operator *(Matrix2 x, Matrix2 y)
            => new Matrix2(x.M11 * y.M11 + x.M12 * y.M21, x.M11 * y.M12 + x.M12 * y.M22,
                           x.M21 * y.M11 + x.M22 * y.M21, x.M21 * y.M12 + x.M22 * y.M22);

        public static Vektor2 operator *(Matrix2 m, Vektor2 v)
            => new Vektor2(m.M11 * v.A + m.M12 * v.B, m.M21 * v.A + m.M22 * v.B);

        /// <summary>Die Inverse; wirft bei (numerisch) singulärer Matrix.</summary>
        internal Matrix2 Inverse()
        {
            double det = Determinante;
            if (det == 0.0 || double.IsNaN(det))
                throw new GebaeudeModellException(GebaeudeModellFehler.SystemSingulaer,
                    "Eine 2×2-Matrix des Gebäudemodells ist singulär.");
            return new Matrix2(M22 / det, -M12 / det, -M21 / det, M11 / det);
        }
    }

    /// <summary>
    /// Die drei Übergangsmatrizen einer Zeitspanne τ für stückweise konstante Eingänge
    /// (Rechenschritte Kapitel 5):
    /// <c>x(τ) = Φ·x₀ + Γ·b</c> und <c>∫₀^τ x dt = Γ·x₀ + Ψ·b</c>.
    /// </summary>
    internal readonly struct Uebergang
    {
        internal Uebergang(double tauS, Matrix2 phi, Matrix2 gamma, Matrix2 psi)
        {
            TauS = tauS;
            Phi = phi;
            Gamma = gamma;
            Psi = psi;
        }

        /// <summary>Die Zeitspanne [s].</summary>
        internal double TauS { get; }

        /// <summary>Φ = exp(A·τ).</summary>
        internal Matrix2 Phi { get; }

        /// <summary>Γ = ∫₀^τ exp(A·s) ds [s].</summary>
        internal Matrix2 Gamma { get; }

        /// <summary>Ψ = ∫₀^τ ∫₀^t exp(A·s) ds dt [s²].</summary>
        internal Matrix2 Psi { get; }

        /// <summary>Der Zustand am Ende der Spanne.</summary>
        internal Vektor2 Ende(Vektor2 x0, Vektor2 b) => Phi * x0 + Gamma * b;

        /// <summary>Das exakte Mittel des Zustands über die Spanne (Blockmittel).</summary>
        internal Vektor2 Mittel(Vektor2 x0, Vektor2 b) => (1.0 / TauS) * (Gamma * x0 + Psi * b);
    }

    /// <summary>
    /// Die exakte Diskretisierung einer konstanten 2×2-Systemmatrix A über die
    /// Sylvester-Formel (Rechenschritte Kapitel 5). Eigenwerte, Mittelpunkt und
    /// <c>A − μ·I</c> werden einmal gebildet; <see cref="Bei"/> liefert Φ, Γ, Ψ zu jeder
    /// Zeitspanne τ &gt; 0.
    ///
    /// <para><b>Die Formel.</b> Mit μ = ½·Spur(A), d = √(μ² − det A), λ₁,₂ = μ ± d gilt
    /// für eine skalare Funktion f:
    /// <c>f(A) = ½·(f(λ₁) + f(λ₂))·I + (f(λ₁) − f(λ₂))/(λ₁ − λ₂)·(A − μ·I)</c>, im
    /// zusammenfallenden Fall <c>f(A) = f(μ)·I + f′(μ)·(A − μ·I)</c>. Die drei Funktionen
    /// werden über z = λ·τ als <c>e^z</c>, <c>τ·(e^z − 1)/z</c> und
    /// <c>τ²·(e^z − 1 − z)/z²</c> gebildet; für |z| &lt; 1 über ihre Reihen, damit sich
    /// Zähler und Nenner nicht auslöschen. Die Abschneidegrenze der Richtlinie für den
    /// abklingenden Exponentialterm (Blatt 1, 6.4) deckt <see cref="Math.Exp"/> selbst ab:
    /// Es läuft für große negative Argumente ohne Fehler gegen 0.</para>
    ///
    /// <para><b>Eigenwerte.</b> Die Systemmatrix eines passiven RC-Netzes ist einer
    /// symmetrisch negativ definiten ähnlich; ihre Eigenwerte sind reell und negativ. Ein
    /// negatives μ² − det A jenseits des Zahlenrands oder ein Eigenwert ≥ 0 ist ein
    /// benannter Fehler.</para>
    /// </summary>
    internal sealed class Uebergangsrechner
    {
        /// <summary>Relativer Abstand der Eigenwerte, unter dem der zusammenfallende Zweig gilt.</summary>
        internal const double ZUSAMMENFALL_RELATIV = 1e-7;

        /// <summary>Betrag von z = λ·τ, unter dem die Reihen statt der geschlossenen Formen gelten.</summary>
        private const double REIHE_BIS = 1.0;

        /// <summary>Glieder der Reihen; für |z| &lt; 1 ist das 30. Glied kleiner als 1e-32.</summary>
        private const int REIHE_GLIEDER = 30;

        private readonly Matrix2 _a;
        private readonly Matrix2 _aMinusMu;
        private readonly double _mu;
        private readonly double _l1;
        private readonly double _l2;
        private readonly bool _zusammenfallend;

        internal Uebergangsrechner(Matrix2 a)
        {
            _a = a;
            _mu = 0.5 * a.Spur;
            double diskriminante = _mu * _mu - a.Determinante;
            double skala = _mu * _mu;
            if (double.IsNaN(diskriminante) || diskriminante < -1e-12 * skala)
                throw new GebaeudeModellException(GebaeudeModellFehler.EigenwerteNichtNegativ,
                    "Die Systemmatrix hat komplexe Eigenwerte (μ² − det A = " +
                    diskriminante.ToString("G6", CultureInfo.InvariantCulture) + ").");
            double d = diskriminante > 0.0 ? Math.Sqrt(diskriminante) : 0.0;
            _l1 = _mu + d;
            _l2 = _mu - d;
            if (!(_l1 < 0.0) || !(_l2 < 0.0))
                throw new GebaeudeModellException(GebaeudeModellFehler.EigenwerteNichtNegativ,
                    "Die Systemmatrix hat einen Eigenwert ≥ 0 (λ₁ = " +
                    _l1.ToString("G6", CultureInfo.InvariantCulture) + " 1/s, λ₂ = " +
                    _l2.ToString("G6", CultureInfo.InvariantCulture) + " 1/s).");
            _zusammenfallend = (_l1 - _l2) <= ZUSAMMENFALL_RELATIV * Math.Abs(_mu);
            _aMinusMu = a - _mu * Matrix2.Einheit;
        }

        /// <summary>Die Systemmatrix A [1/s].</summary>
        internal Matrix2 A => _a;

        /// <summary>Beide Eigenwerte [1/s], der betragskleinere (langsamere) zuerst.</summary>
        internal double[] Eigenwerte => new[] { _l1, _l2 };

        /// <summary>Gilt der zusammenfallende Zweig der Sylvester-Formel?</summary>
        internal bool Zusammenfallend => _zusammenfallend;

        /// <summary>Φ, Γ und Ψ zur Zeitspanne <paramref name="tauS"/> &gt; 0.</summary>
        internal Uebergang Bei(double tauS)
        {
            if (!(tauS > 0.0) || double.IsInfinity(tauS))
                throw new ArgumentOutOfRangeException(nameof(tauS), tauS, "Die Zeitspanne muss endlich und größer null sein.");

            Matrix2 phi, gamma, psi;
            if (_zusammenfallend)
            {
                double z = _mu * tauS;
                double e = Math.Exp(z);
                phi = e * Matrix2.Einheit + (tauS * e) * _aMinusMu;
                gamma = (tauS * G1(z, e)) * Matrix2.Einheit + (tauS * tauS * G1Strich(z, e)) * _aMinusMu;
                psi = (tauS * tauS * G2(z, e)) * Matrix2.Einheit + (tauS * tauS * tauS * G2Strich(z, e)) * _aMinusMu;
            }
            else
            {
                double z1 = _l1 * tauS, z2 = _l2 * tauS;
                double e1 = Math.Exp(z1), e2 = Math.Exp(z2);
                double dl = _l1 - _l2;
                phi = Sylvester(e1, e2, dl);
                gamma = Sylvester(tauS * G1(z1, e1), tauS * G1(z2, e2), dl);
                psi = Sylvester(tauS * tauS * G2(z1, e1), tauS * tauS * G2(z2, e2), dl);
            }
            return new Uebergang(tauS, phi, gamma, psi);
        }

        private Matrix2 Sylvester(double f1, double f2, double dl)
            => (0.5 * (f1 + f2)) * Matrix2.Einheit + ((f1 - f2) / dl) * _aMinusMu;

        /// <summary>g₁(z) = (e^z − 1)/z.</summary>
        internal static double G1(double z, double ez)
        {
            if (Math.Abs(z) < REIHE_BIS)
            {
                // Σ z^k/(k+1)!
                double summe = 0.0, glied = 1.0;
                for (int k = 0; k < REIHE_GLIEDER; k++)
                {
                    summe += glied;
                    glied *= z / (k + 2);
                }
                return summe;
            }
            return (ez - 1.0) / z;
        }

        /// <summary>g₂(z) = (e^z − 1 − z)/z².</summary>
        internal static double G2(double z, double ez)
        {
            if (Math.Abs(z) < REIHE_BIS)
            {
                // Σ z^k/(k+2)!
                double summe = 0.0, glied = 0.5;
                for (int k = 0; k < REIHE_GLIEDER; k++)
                {
                    summe += glied;
                    glied *= z / (k + 3);
                }
                return summe;
            }
            return (ez - 1.0 - z) / (z * z);
        }

        /// <summary>g₁′(z) = ((z − 1)·e^z + 1)/z².</summary>
        internal static double G1Strich(double z, double ez)
        {
            if (Math.Abs(z) < REIHE_BIS)
            {
                // Σ_{k≥1} k·z^(k−1)/(k+1)!
                double summe = 0.0, potenzDurchFak = 0.5; // z^(k−1)/(k+1)! für k = 1
                for (int k = 1; k <= REIHE_GLIEDER; k++)
                {
                    summe += k * potenzDurchFak;
                    potenzDurchFak *= z / (k + 2);
                }
                return summe;
            }
            return ((z - 1.0) * ez + 1.0) / (z * z);
        }

        /// <summary>g₂′(z) = ((z − 2)·e^z + 2 + z)/z³.</summary>
        internal static double G2Strich(double z, double ez)
        {
            if (Math.Abs(z) < REIHE_BIS)
            {
                // Σ_{k≥1} k·z^(k−1)/(k+2)!
                double summe = 0.0, potenzDurchFak = 1.0 / 6.0; // z^(k−1)/(k+2)! für k = 1
                for (int k = 1; k <= REIHE_GLIEDER; k++)
                {
                    summe += k * potenzDurchFak;
                    potenzDurchFak *= z / (k + 3);
                }
                return summe;
            }
            return ((z - 2.0) * ez + 2.0 + z) / (z * z * z);
        }
    }
}
