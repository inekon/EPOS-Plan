using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Aufheizantwort des geregelten Falls</b> einer Zone (Entwurf KP3 Abschnitt 2 Nr. 2,
    /// Festlegungen 4 und 5; Teilkonzept Konditionierungsprofile 4.2) — gebildet von
    /// <see cref="Zonenmodell2K.Aufheizantwort(double, double)"/> zu einem Strahlungsanteil und einem
    /// Zusatzleitwert. Unveränderlich, ohne Zustand.
    ///
    /// <para><b>Die Größen.</b> Im geregelten Fall gilt dx/dt = A·x + b(θ_soll) und für die Leistung
    /// Φ = z·x + c₂(θ_soll), beides affin im Sollwert: ΔB = ∂b/∂θ_soll, G_0 = ∂c₂/∂θ_soll. Mit
    /// v = A⁻¹·ΔB ist der eingeschwungene Zustand x*(θ) = x*(0) − θ·v, die Sprungantwort je Kelvin
    /// bei festen Rändern g(t) = H_s + z·Φ(t)·v mit Φ(t) = exp(A·t), dem stationären Leitwert
    /// H_s = G_0 − z·v und dem Sprungleitwert g(0) = G_0. Die Überschusswärme je Kelvin ist
    /// C_w = ∫₀^∞ z·Φ(t)·v dt = −z·A⁻¹·v.</para>
    ///
    /// <para><b>Moden nur für die Herleitungszeile.</b> Bei getrennten Eigenwerten zerfällt
    /// z·Φ(t)·v = r₁·e^(−t/τ₁) + r₂·e^(−t/τ₂) mit den Residuen r_k = z·P_k·v,
    /// P_k = (A − λ_j·I)/(λ_k − λ_j), und den Modalkapazitäten C_k = r_k·τ_k; die Stufenformel
    /// (<see cref="Aufheizstufen"/>) braucht sie nicht, sie rechnet über die Matrixfunktionen
    /// (Festlegung 5). Index 1 ist der schnelle, Index 2 der langsame Modus (Teilkonzept 4.2:
    /// τ = 1,02 und 4,54 h, C₁ = 0,24 und C₂ = 5,55 kWh/K). <b>Im zusammenfallenden Zweig</b> des
    /// Lösers (<see cref="Uebergangsrechner.Zusammenfallend"/>) gibt es keine Zerlegung in zwei Moden:
    /// τ₁ = τ₂ = −1/μ, r_k und C_k sind NaN, C_w bleibt endlich (benannte Festlegung dieser Welle).</para>
    /// </summary>
    internal sealed class Aufheizantwort
    {
        private readonly Uebergangsrechner _rechner;

        /// <param name="strahlungsanteil">Strahlungsanteil der Heizung [–] (Schlüssel).</param>
        /// <param name="zusatzleitwertWK">Zusatzleitwert [W/K] (Schlüssel).</param>
        /// <param name="rechner">Der Übergangsrechner der geregelten Lage (Matrix A).</param>
        /// <param name="z">Die Ausgangszeile der Leistung (Z₂₀, Z₂₁) [W/K].</param>
        /// <param name="deltaB">ΔB = ∂b/∂θ_soll [1/s].</param>
        /// <param name="g0WK">G_0 = ∂c₂/∂θ_soll [W/K].</param>
        internal Aufheizantwort(double strahlungsanteil, double zusatzleitwertWK, Uebergangsrechner rechner,
                                Vektor2 z, Vektor2 deltaB, double g0WK)
        {
            _rechner = rechner ?? throw new ArgumentNullException(nameof(rechner));
            Strahlungsanteil = strahlungsanteil;
            ZusatzleitwertWK = zusatzleitwertWK;
            Z = z;
            DeltaB = deltaB;
            G0WK = g0WK;

            Matrix2 aInvers = rechner.A.Inverse();
            V = aInvers * deltaB;
            HsWK = g0WK - Punkt(z, V);
            CwJk = -Punkt(z, aInvers * V);
            Stunde = rechner.Bei(Zonenmodell2K.STUNDE_S);

            // Der Rechner führt den langsamen Eigenwert zuerst; hier steht der schnelle Modus vorn.
            double[] l = rechner.Eigenwerte;
            double lSchnell = l[1], lLangsam = l[0];
            Tau1S = -1.0 / lSchnell;
            Tau2S = -1.0 / lLangsam;
            if (rechner.Zusammenfallend)
            {
                R1WK = double.NaN;
                R2WK = double.NaN;
                C1Jk = double.NaN;
                C2Jk = double.NaN;
            }
            else
            {
                R1WK = Residuum(rechner.A, lSchnell, lLangsam, z, V);
                R2WK = Residuum(rechner.A, lLangsam, lSchnell, z, V);
                C1Jk = R1WK * Tau1S;
                C2Jk = R2WK * Tau2S;
            }
        }

        /// <summary>Strahlungsanteil der Heizung [–], mit dem die Antwort gebildet ist.</summary>
        internal double Strahlungsanteil { get; }

        /// <summary>Zusatzleitwert Außenluft ↔ Raumluft [W/K], mit dem die Antwort gebildet ist.</summary>
        internal double ZusatzleitwertWK { get; }

        /// <summary>Die Systemmatrix A der geregelten Lage [1/s].</summary>
        internal Matrix2 A => _rechner.A;

        /// <summary>Die Ausgangszeile z = (Z₂₀, Z₂₁) der Leistung im Zustand [W/K].</summary>
        internal Vektor2 Z { get; }

        /// <summary>Die Empfindlichkeit ΔB = ∂b/∂θ_soll [1/s].</summary>
        internal Vektor2 DeltaB { get; }

        /// <summary>v = A⁻¹·ΔB [–]: −v ist die Änderung des eingeschwungenen Zustands je Kelvin Sollwert.</summary>
        internal Vektor2 V { get; }

        /// <summary>Sprungleitwert G_0 = ∂c₂/∂θ_soll = g(0) [W/K].</summary>
        internal double G0WK { get; }

        /// <summary>Stationärer Leitwert H_s = G_0 − z·v [W/K] (ohne Lasten, θ_eq = θ_out).</summary>
        internal double HsWK { get; }

        /// <summary>Wirksame Aufheizkapazität C_w = −z·A⁻¹·v [J/K] — die Überschusswärme je Kelvin Sprung.</summary>
        internal double CwJk { get; }

        /// <summary>Zeitkonstante des schnellen Modus τ₁ [s].</summary>
        internal double Tau1S { get; }

        /// <summary>Zeitkonstante des langsamen Modus τ₂ [s].</summary>
        internal double Tau2S { get; }

        /// <summary>Residuum r₁ des schnellen Modus [W/K]; NaN im zusammenfallenden Zweig.</summary>
        internal double R1WK { get; }

        /// <summary>Residuum r₂ des langsamen Modus [W/K]; NaN im zusammenfallenden Zweig.</summary>
        internal double R2WK { get; }

        /// <summary>Modalkapazität C₁ = r₁·τ₁ [J/K]; NaN im zusammenfallenden Zweig.</summary>
        internal double C1Jk { get; }

        /// <summary>Modalkapazität C₂ = r₂·τ₂ [J/K]; NaN im zusammenfallenden Zweig.</summary>
        internal double C2Jk { get; }

        /// <summary>Gilt der zusammenfallende Zweig der Sylvester-Formel (wie im Löser)?</summary>
        internal bool Zusammenfallend => _rechner.Zusammenfallend;

        /// <summary>Φ, Γ und Ψ der vollen Stunde.</summary>
        internal Uebergang Stunde { get; }

        /// <summary>Φ, Γ und Ψ zur Zeitspanne <paramref name="tauS"/> &gt; 0 — über die Naht <see cref="Plattformrundung"/>.</summary>
        internal Uebergang Bei(double tauS) => _rechner.Bei(tauS);

        /// <summary>z·M·v [W/K bzw. W·s/K] — die Ausgangsform jeder Matrixfunktion der Stufenformel.</summary>
        internal double Ausgang(Matrix2 m) => Punkt(Z, m * V);

        private static double Punkt(Vektor2 a, Vektor2 b) => a.A * b.A + a.B * b.B;

        private static double Residuum(Matrix2 a, double lk, double lj, Vektor2 z, Vektor2 v)
        {
            double d = lk - lj;
            var p = new Matrix2((a.M11 - lj) / d, a.M12 / d, a.M21 / d, (a.M22 - lj) / d);
            return Punkt(z, p * v);
        }
    }
}
