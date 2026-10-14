#nullable enable

using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Schritt UB‑a</b> (Fachkonzept Übergabegrenze 4.2): die Übergabe einer Zone — bzw. des
    /// Gebäudes ohne Zonen — kalibriert aus ihrem Auslegungspunkt. Reine Zahlen, ohne Datenbank.
    /// Die Leistungseinheit ist frei (kW in den Proben, W im Profilweg); W_H trägt dieselbe
    /// Leistungseinheit je K.
    /// </summary>
    internal sealed class Uebergabezone
    {
        /// <param name="phiN">Nennleistung der Übergabe Φ_N (Leistungseinheit frei), &gt; 0.</param>
        /// <param name="auslegungVorlaufC">Auslegungsvorlauf θ_V,N [°C].</param>
        /// <param name="auslegungRuecklaufC">Auslegungsrücklauf θ_R,N [°C], unter θ_V,N.</param>
        /// <param name="auslegungRaumC">Raumtemperatur im Auslegungspunkt θ_i,N [°C], unter dem mittleren Heizmittel.</param>
        /// <param name="exponent">Exponent n der Übergabe [–], &gt; 0.</param>
        internal Uebergabezone(double phiN, double auslegungVorlaufC, double auslegungRuecklaufC,
                               double auslegungRaumC, double exponent)
        {
            if (!(phiN > 0.0) || double.IsInfinity(phiN))
                throw new ArgumentOutOfRangeException(nameof(phiN), "Φ_N muss endlich und positiv sein.");
            if (!(auslegungVorlaufC > auslegungRuecklaufC))
                throw new ArgumentOutOfRangeException(nameof(auslegungRuecklaufC), "Der Auslegungsrücklauf muss unter dem Auslegungsvorlauf liegen.");
            if (!(exponent > 0.0))
                throw new ArgumentOutOfRangeException(nameof(exponent), "Der Exponent muss positiv sein.");
            PhiN = phiN;
            AuslegungVorlaufC = auslegungVorlaufC;
            AuslegungRuecklaufC = auslegungRuecklaufC;
            AuslegungRaumC = auslegungRaumC;
            Exponent = exponent;
            WH = phiN / (auslegungVorlaufC - auslegungRuecklaufC);
            DeltaThetaMN = 0.5 * (auslegungVorlaufC + auslegungRuecklaufC) - auslegungRaumC;
            if (!(DeltaThetaMN > 0.0))
                throw new ArgumentOutOfRangeException(nameof(auslegungRaumC), "Die Raumtemperatur muss unter dem mittleren Heizmittel liegen.");
        }

        /// <summary>Die Zone aus den Kennwerten der Wärmeübergabe (AK1, Leistung in W).</summary>
        internal static Uebergabezone AusKennwerten(Uebergabekennwerte k)
        {
            if (k == null) throw new ArgumentNullException(nameof(k));
            return new Uebergabezone(k.PhiNW, k.AuslegungVorlaufC, k.AuslegungRuecklaufC, k.AuslegungRaumC, k.Exponent);
        }

        /// <summary>Nennleistung Φ_N.</summary>
        internal double PhiN { get; }

        /// <summary>Auslegungsvorlauf θ_V,N [°C].</summary>
        internal double AuslegungVorlaufC { get; }

        /// <summary>Auslegungsrücklauf θ_R,N [°C].</summary>
        internal double AuslegungRuecklaufC { get; }

        /// <summary>Raumtemperatur im Auslegungspunkt θ_i,N [°C].</summary>
        internal double AuslegungRaumC { get; }

        /// <summary>Exponent n [–].</summary>
        internal double Exponent { get; }

        /// <summary>W_H = Φ_N/(θ_V,N − θ_R,N) — Wärmekapazitätsstrom bei Auslegungsmassenstrom (UB‑a).</summary>
        internal double WH { get; }

        /// <summary>Δθ_m,N = (θ_V,N + θ_R,N)/2 − θ_i,N [K] (UB‑a, arithmetisches Mittel wie AK1).</summary>
        internal double DeltaThetaMN { get; }
    }

    /// <summary>Ergebnis der Übergabegrenze einer Zone (UB‑b).</summary>
    /// <param name="PhiUeMax">Φ_UE,max — Leistung der Übergabe beim Höchstvorlauf (Einheit wie Φ_N).</param>
    /// <param name="RuecklaufC">θ_R,UE = θ_WP,max − Φ_UE,max/W_H [°C].</param>
    /// <param name="SpreizungK">Δθ_UE = θ_WP,max − θ_R,UE [K].</param>
    /// <param name="Begrenzt">Begrenzt die Übergabe (θ_WP,max unter dem Auslegungsvorlauf)?</param>
    /// <param name="Schritte">Zahl der Iterationsschritte (Newton und Bisektion).</param>
    /// <param name="Bisektion">Ist mindestens ein Schritt auf die Bisektion zurückgefallen?</param>
    internal readonly record struct Zonengrenze(double PhiUeMax, double RuecklaufC, double SpreizungK,
                                                bool Begrenzt, int Schritte, bool Bisektion);

    /// <summary>Ergebnis der Übergabegrenze eines Gebäudes bzw. Projekts: die Zonen und ihre Summe.</summary>
    internal sealed class Gebaeudegrenze
    {
        internal Gebaeudegrenze(IReadOnlyList<Zonengrenze> zonen, double phiN, double hoechstvorlaufC)
        {
            Zonen = zonen;
            PhiN = phiN;
            double s = 0.0;
            bool begrenzt = false;
            foreach (Zonengrenze z in zonen)
            {
                s += z.PhiUeMax;
                begrenzt |= z.Begrenzt;
            }
            PhiUeMax = s;
            Begrenzt = begrenzt;
            HoechstvorlaufC = hoechstvorlaufC;
        }

        /// <summary>Die Grenzen je Zone in der Reihenfolge der Eingabe.</summary>
        internal IReadOnlyList<Zonengrenze> Zonen { get; }

        /// <summary>Σ Φ_N der Zonen.</summary>
        internal double PhiN { get; }

        /// <summary>Φ_UE,max = Σ_z Φ_UE,max,z.</summary>
        internal double PhiUeMax { get; }

        /// <summary>Höchstvorlauf θ_WP,max [°C], zu dem gerechnet wurde.</summary>
        internal double HoechstvorlaufC { get; }

        /// <summary>Begrenzt mindestens eine Zone?</summary>
        internal bool Begrenzt { get; }

        /// <summary>Anteil Φ_UE,max/Φ_N [–].</summary>
        internal double Anteil => PhiN > 0.0 ? PhiUeMax / PhiN : 0.0;

        /// <summary>
        /// Gemischter Rücklauf θ_R,UE [°C], massenstromgewichtet: θ_WP,max − Φ_UE,max/Σ W_H. Wird
        /// über <see cref="Uebergabegrenze.Gebaeude"/> gesetzt.
        /// </summary>
        internal double RuecklaufC { get; init; } = double.NaN;

        /// <summary>Spreizung Δθ_UE = θ_WP,max − θ_R,UE [K].</summary>
        internal double SpreizungK => HoechstvorlaufC - RuecklaufC;
    }

    /// <summary>
    /// <b>Die Übergabegrenze beim Höchstvorlauf der Wärmepumpe</b> (Fachkonzept Übergabegrenze
    /// 4.3, Schritt UB‑b): je Zone das Gleichgewicht von Wasserseite und Heizflächengleichung bei
    /// Auslegungsmassenstrom und Vorlauf θ_WP,max,
    /// <code>
    /// f(Φ) = Φ_N·((θ_WP,max − Φ/(2·W_H) − θ_i)/Δθ_m,N)^n − Φ = 0
    /// </code>
    /// f fällt streng auf [0, 2·W_H·(θ_WP,max − θ_i)], f(0) &gt; 0, f am rechten Rand &lt; 0 —
    /// die Nullstelle ist eindeutig. Gelöst mit Newton (höchstens <see cref="NEWTON_SCHRITTE_MAX"/>
    /// Schritte, wie H2 in <see cref="Waermeuebergabe"/>), der Startwert ist übergebbar (Vorstunde);
    /// verlässt ein Newton-Schritt das Einschlussintervall, rechnet der Schritt als Bisektion.
    /// Ohne Konvergenz nach den Newton-Schritten führt die Bisektion zu Ende (feste Höchstzahl) —
    /// deterministisch, ohne Ausnahme, ohne Parallelität. Keine Rechenwirkung in UB‑E1.
    /// </summary>
    internal static class Uebergabegrenze
    {
        /// <summary>Höchstzahl der Newton-Schritte (wie H2).</summary>
        internal const int NEWTON_SCHRITTE_MAX = Waermeuebergabe.NEWTON_SCHRITTE_MAX;

        /// <summary>Höchstzahl der Bisektionsschritte nach den Newton-Schritten (Intervall / 2^200 ≪ double).</summary>
        internal const int BISEKTION_SCHRITTE_MAX = 200;

        /// <summary>Abbruch, relativ zu Φ_N: |f| ≤ ABBRUCH_RELATIV·Φ_N.</summary>
        internal const double ABBRUCH_RELATIV = 1e-12;

        /// <summary>
        /// Die Grenze einer Zone bei Höchstvorlauf <paramref name="hoechstvorlaufC"/> und Raumtemperatur
        /// <paramref name="raumC"/> (Stunde bzw. Auslegung). <paramref name="startwert"/>: Nullstelle der
        /// Vorstunde; NaN oder außerhalb des Intervalls → Start an der linearisierten Form.
        /// </summary>
        internal static Zonengrenze Zone(Uebergabezone z, double hoechstvorlaufC, double raumC, double startwert = double.NaN)
        {
            if (z == null) throw new ArgumentNullException(nameof(z));
            bool begrenzt = hoechstvorlaufC < z.AuslegungVorlaufC;
            double a = hoechstvorlaufC - raumC;
            if (!(a > 0.0))
            {
                // θ_WP,max ≤ θ_i: keine Übergabe; der Rücklauf steht beim Vorlauf (≈ Raumtemperatur).
                return new Zonengrenze(0.0, hoechstvorlaufC, 0.0, begrenzt, 0, false);
            }
            double phi = Nullstelle(z.PhiN, z.Exponent, a, 0.5 / z.WH, z.DeltaThetaMN, startwert,
                                    out int schritte, out bool bisektion);
            double ruecklauf = hoechstvorlaufC - phi / z.WH;
            return new Zonengrenze(phi, ruecklauf, hoechstvorlaufC - ruecklauf, begrenzt, schritte, bisektion);
        }

        /// <summary>
        /// <b>Die Übergabegrenze am gemischten Vorlauf</b> (Weiche mit ṁ_WP &lt; ṁ_HK, Fachkonzept 4.4 und 2.8): die
        /// Wärmepumpe fährt θ_WP,max, der Heizkreisvorlauf ist die Mischung θ_V,HK = θ_R + (θ_WP,max − θ_R)/r mit
        /// r = ṁ_HK/ṁ_WP &gt; 1 (je Zone derselbe Anteil des Stroms der Wärmepumpe). Mit Φ = ṁ_WP·(θ_WP,max − θ_R) wird
        /// die mittlere Heizflächentemperatur θ_WP,max − Φ·(r − ½)/W_H — die zweite Gleichung
        /// <code>
        /// f(Φ) = Φ_N·((θ_WP,max − Φ·(r − ½)/W_H − θ_i)/Δθ_m,N)^n − Φ = 0
        /// </code>
        /// mit derselben Nullstellenroutine; f fällt streng, die Nullstelle ist eindeutig. r ≤ 1 rechnet wie
        /// <see cref="Zone(Uebergabezone, double, double, double)"/> (ungemischt).
        /// </summary>
        internal static Zonengrenze ZoneGemischt(Uebergabezone z, double hoechstvorlaufC, double raumC, double r,
                                                 double startwert = double.NaN)
        {
            if (z == null) throw new ArgumentNullException(nameof(z));
            if (!(r > 1.0)) return Zone(z, hoechstvorlaufC, raumC, startwert);
            bool begrenzt = hoechstvorlaufC < z.AuslegungVorlaufC;
            double a = hoechstvorlaufC - raumC;
            if (!(a > 0.0)) return new Zonengrenze(0.0, hoechstvorlaufC, 0.0, begrenzt, 0, false);
            double phi = Nullstelle(z.PhiN, z.Exponent, a, (r - 0.5) / z.WH, z.DeltaThetaMN, startwert,
                                    out int schritte, out bool bisektion);
            double ruecklauf = hoechstvorlaufC - phi * r / z.WH;
            return new Zonengrenze(phi, ruecklauf, hoechstvorlaufC - ruecklauf, begrenzt, schritte, bisektion);
        }

        /// <summary>
        /// Die Grenze eines Gebäudes: Summe der Zonengrenzen; ohne Zonen rechnet
        /// <paramref name="gebaeude"/> als eine Zone.
        /// </summary>
        internal static Gebaeudegrenze Gebaeude(IReadOnlyList<Uebergabezone>? zonen, Uebergabezone? gebaeude,
                                                double hoechstvorlaufC, double raumC)
        {
            IReadOnlyList<Uebergabezone> liste = zonen != null && zonen.Count > 0
                ? zonen
                : gebaeude != null ? new[] { gebaeude }
                : throw new ArgumentException("Weder Zonen noch Gebäudeübergabe angegeben.", nameof(gebaeude));
            var ergebnisse = new Zonengrenze[liste.Count];
            double phiN = 0.0, wh = 0.0;
            for (int i = 0; i < liste.Count; i++)
            {
                ergebnisse[i] = Zone(liste[i], hoechstvorlaufC, raumC);
                phiN += liste[i].PhiN;
                wh += liste[i].WH;
            }
            var g = new Gebaeudegrenze(ergebnisse, phiN, hoechstvorlaufC)
            {
                RuecklaufC = hoechstvorlaufC > raumC ? hoechstvorlaufC - Summe(ergebnisse) / wh : hoechstvorlaufC,
            };
            return g;
        }

        /// <summary>Die Projektgrenze: Summe der Gebäudegrenzen [Leistungseinheit wie Φ_N].</summary>
        internal static double Projekt(IEnumerable<Gebaeudegrenze> gebaeude)
        {
            double s = 0.0;
            foreach (Gebaeudegrenze g in gebaeude) s += g.PhiUeMax;
            return s;
        }

        private static double Summe(IReadOnlyList<Zonengrenze> z)
        {
            double s = 0.0;
            foreach (Zonengrenze e in z) s += e.PhiUeMax;
            return s;
        }

        /// <summary>
        /// Löst Φ = Φ_N·((a − b·Φ)/c)^n auf (0, a/b) — dieselbe Gleichung wie
        /// <see cref="Waermeuebergabe.Loesen"/>, hier mit Einschluss und Bisektionsrückfall.
        /// </summary>
        internal static double Nullstelle(double phiN, double n, double a, double b, double c, double startwert,
                                          out int schritte, out bool bisektion)
        {
            schritte = 0;
            bisektion = false;
            double lo = 0.0, hi = a / b;
            double toleranz = ABBRUCH_RELATIV * phiN;
            double phi;
            if (startwert > lo && startwert < hi) phi = startwert;
            else
            {
                // Nullstelle der Tangente in Φ = 0: liegt unter der Lösung (f konvex).
                double g0 = phiN * Math.Pow(a / c, n);
                phi = g0 / (1.0 + n * g0 * b / a);
                if (!(phi > lo && phi < hi)) phi = 0.5 * (lo + hi);
            }
            for (int k = 0; k < NEWTON_SCHRITTE_MAX + BISEKTION_SCHRITTE_MAX; k++)
            {
                double t = (a - b * phi) / c;
                double g = phiN * Math.Pow(t, n);
                double f = g - phi;
                if (Math.Abs(f) <= toleranz) return phi;
                if (f > 0.0) lo = phi; else hi = phi;
                if (!(hi - lo > 1e-15 * Math.Max(1.0, hi))) return 0.5 * (lo + hi);
                schritte++;
                double neu = double.NaN;
                if (k < NEWTON_SCHRITTE_MAX)
                {
                    double ableitung = -n * phiN * Math.Pow(t, n - 1.0) * b / c - 1.0;
                    neu = phi - f / ableitung;
                }
                if (!(neu > lo && neu < hi))
                {
                    neu = 0.5 * (lo + hi);
                    bisektion = true;
                }
                phi = neu;
            }
            return phi;
        }
    }
}
