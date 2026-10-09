using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Teillastkurve der Kältemaschine aus ihren Quellen</b> (KM3, Fachkonzept Teillast und Takten 3.2, 4.3 und
    /// 4.4): EIRFPLR(x) = a + b·x + c·x² aus einem Copper-Satz (<c>eir-f-plr</c>) oder aus den Teillastzeilen der
    /// CSV-Vorlage (EER-Verhältnisse, Anpassung nach kleinsten Quadraten), auf Volllast normiert
    /// (E(x) = EIRFPLR(x) / EIRFPLR(1), gespeichert mit EIRFPLR(1) = 1), dazu die Verdichterregelung aus Verdichterart
    /// und Drehzahl und die Vorgabekurve je Regelung. Ohne Rechenwirkung — die Lastachse rechnet erst die Welle E2.
    /// </summary>
    public static class KaeltemaschineTeillastkurve
    {
        /// <summary>Nachkommastellen der gespeicherten Beiwerte (Hauswert).</summary>
        public const int NACHKOMMA_BEIWERTE = 6;

        /// <summary>Nachkommastellen der gespeicherten unteren Gültigkeit x_u (Hauswert).</summary>
        public const int NACHKOMMA_LASTGRAD = 3;

        /// <summary>Mindestzahl verschiedener Lastgrade für eine angepasste Kurve (Fachkonzept 3.2 b).</summary>
        public const int MIN_ZEILEN = 3;

        /// <summary>Stützstellen, an denen eine kubische Quellkurve auf [x_u, 1] quadratisch angepasst wird (Hauswert).</summary>
        public const int STUETZSTELLEN_KUBISCH = 50;

        /// <summary>Toleranz, unter der ein Beiwert als 0 bzw. 1 gilt (Erkennung der linearen Kurve).</summary>
        public const double TOLERANZ_LINEAR = 1e-9;

        /// <summary>Eine quadratische Kurve EIRFPLR(x) = A + B·x + C·x².</summary>
        public readonly record struct Kurve(double A, double B, double C)
        {
            /// <summary>Der Wert bei <paramref name="x"/>.</summary>
            public double Wert(double x) => A + B * x + C * x * x;

            /// <summary>EIRFPLR(1) = A + B + C.</summary>
            public double Volllast => A + B + C;

            /// <summary>Das EER-Verhältnis g(x) = x / E(x) der normierten Kurve.</summary>
            public double EerVerhaeltnis(double x) => x / (Wert(x) / Volllast);
        }

        /// <summary>
        /// Normiert eine Kurve auf EIRFPLR(1) = 1 (Fachkonzept 3.2) und rundet die Beiwerte auf
        /// <see cref="NACHKOMMA_BEIWERTE"/> Stellen; <c>null</c>, wenn EIRFPLR(1) nicht positiv oder nicht endlich ist.
        /// </summary>
        public static Kurve? Normieren(double a, double b, double c)
        {
            double e1 = a + b + c;
            if (!(e1 > 0) || double.IsInfinity(e1)) return null;
            return new Kurve(Runden(a / e1), Runden(b / e1), Runden(c / e1));
        }

        /// <summary>Ist die Kurve die lineare E(x) = x (a = 0, b = 1, c = 0)? Dann gilt der Weg <c>LINEAR</c>.</summary>
        public static bool IstLinear(Kurve k) =>
            Math.Abs(k.A) < TOLERANZ_LINEAR && Math.Abs(k.B - 1) < TOLERANZ_LINEAR && Math.Abs(k.C) < TOLERANZ_LINEAR;

        /// <summary>
        /// Passt ein Polynom zweiten Grades nach kleinsten Quadraten an die Punkte (x, E) an (Normalgleichungen, 3 × 3);
        /// <c>null</c> bei weniger als <see cref="MIN_ZEILEN"/> verschiedenen x oder singulärem System. Ohne Normierung.
        /// </summary>
        public static Kurve? Anpassen(IReadOnlyList<(double X, double E)> punkte)
        {
            if (punkte == null) return null;
            var p = punkte.Where(q => Endlich(q.X) && Endlich(q.E)).ToList();
            if (p.Select(q => q.X).Distinct().Count() < MIN_ZEILEN) return null;
            // Summen der Potenzen: S_k = Σ x^k (k = 0 … 4), T_k = Σ E·x^k (k = 0 … 2).
            var s = new double[5];
            var t = new double[3];
            foreach ((double x, double e) in p)
            {
                double xk = 1;
                for (int k = 0; k <= 4; k++)
                {
                    s[k] += xk;
                    if (k <= 2) t[k] += e * xk;
                    xk *= x;
                }
            }
            var m = new double[3, 4]
            {
                { s[0], s[1], s[2], t[0] },
                { s[1], s[2], s[3], t[1] },
                { s[2], s[3], s[4], t[2] },
            };
            // Gauß mit Spaltenpivot.
            for (int i = 0; i < 3; i++)
            {
                int piv = i;
                for (int r = i + 1; r < 3; r++) if (Math.Abs(m[r, i]) > Math.Abs(m[piv, i])) piv = r;
                if (Math.Abs(m[piv, i]) < 1e-12) return null;
                if (piv != i)
                    for (int c = 0; c < 4; c++) (m[i, c], m[piv, c]) = (m[piv, c], m[i, c]);
                for (int r = 0; r < 3; r++)
                {
                    if (r == i) continue;
                    double f = m[r, i] / m[i, i];
                    for (int c = i; c < 4; c++) m[r, c] -= f * m[i, c];
                }
            }
            double a = m[0, 3] / m[0, 0], b = m[1, 3] / m[1, 1], cc = m[2, 3] / m[2, 2];
            return Endlich(a) && Endlich(b) && Endlich(cc) ? new Kurve(a, b, cc) : (Kurve?)null;
        }

        /// <summary>
        /// Die Kurve aus den Teillastzeilen der CSV-Vorlage (Fachkonzept 3.2 b): je Zeile Lastgrad x_i und EER-Verhältnis
        /// g_i (EER bei Teillast durch EER bei Volllast); daraus E(x_i) = x_i / g_i, angepasst und normiert. Zeilen mit
        /// x außerhalb (0, 1] oder g ≤ 0 zählen nicht; <c>null</c> mit weniger als <see cref="MIN_ZEILEN"/> verschiedenen
        /// Lastgraden.
        /// </summary>
        public static Kurve? AusEerVerhaeltnis(IReadOnlyList<(double Lastgrad, double Verhaeltnis)> zeilen)
        {
            if (zeilen == null) return null;
            var punkte = zeilen.Where(z => z.Lastgrad > 0 && z.Lastgrad <= 1 && z.Verhaeltnis > 0)
                               .Select(z => (z.Lastgrad, z.Lastgrad / z.Verhaeltnis)).ToList();
            Kurve? k = Anpassen(punkte);
            return k.HasValue ? Normieren(k.Value.A, k.Value.B, k.Value.C) : null;
        }

        /// <summary>
        /// Die Verdichterregelung aus Drehzahl und Verdichterart der Quelle (Fachkonzept 4.3 und 4.4): <c>variable</c> →
        /// <c>DREHZAHL</c>; sonst Hubkolben und Scroll → <c>EIN_AUS</c>, Schraube und Turbo → <c>STUFEN</c>;
        /// unbekannt → <c>null</c>.
        /// </summary>
        public static string Verdichterregelung(string verdichter, string drehzahl)
        {
            if (string.Equals(drehzahl, "variable", StringComparison.OrdinalIgnoreCase))
                return KaeltemaschineTeillastSchema.REGELUNG_DREHZAHL;
            return (verdichter ?? "").ToLowerInvariant() switch
            {
                "scroll" or "reciprocating" => KaeltemaschineTeillastSchema.REGELUNG_EIN_AUS,
                "screw" or "centrifugal" => KaeltemaschineTeillastSchema.REGELUNG_STUFEN,
                _ => null
            };
        }

        /// <summary>
        /// Die untere Gültigkeit x_u eines Copper-Satzes: <c>x_min</c> der Teillastkurve (Fachkonzept 3.2 a), ersatzweise
        /// <c>min_plr</c>, dann <c>min_unloading</c>; gerundet auf <see cref="NACHKOMMA_LASTGRAD"/> Stellen, <c>null</c>
        /// ohne Angabe in [0, 1].
        /// </summary>
        public static double? UntereGueltigkeit(KaeltemaschinenKurvensatz s)
        {
            foreach (double? v in new[] { s?.TeillastXMin, s?.MinPlr, s?.MinUnloading })
                if (v.HasValue && v.Value >= 0 && v.Value <= 1 + 1e-9)
                    return Math.Round(Math.Min(v.Value, 1.0), NACHKOMMA_LASTGRAD, MidpointRounding.AwayFromZero);
            return null;
        }

        /// <summary>
        /// Die quadratische Kurve eines Copper-Satzes vor der Normierung: <c>quad</c> unmittelbar aus <c>coeff1</c> bis
        /// <c>coeff3</c>, <c>cubic</c> nach kleinsten Quadraten an <see cref="STUETZSTELLEN_KUBISCH"/> Stellen auf
        /// [x_u, 1] (benannt in <paramref name="hinweise"/>); <c>null</c> ohne Kurve.
        /// </summary>
        public static Kurve? Rohkurve(KaeltemaschinenKurvensatz s, IList<string> hinweise = null)
        {
            double[] w = s?.TeillastBeiwerte;
            if (w == null) return null;
            if (w.Length == 3) return new Kurve(w[0], w[1], w[2]);
            if (w.Length != 4) return null;
            double von = UntereGueltigkeit(s) ?? 0.0;
            if (von >= 1) von = 0;
            var punkte = new List<(double, double)>();
            for (int i = 0; i < STUETZSTELLEN_KUBISCH; i++)
            {
                double x = von + (1 - von) * i / (STUETZSTELLEN_KUBISCH - 1);
                punkte.Add((x, w[0] + w[1] * x + w[2] * x * x + w[3] * x * x * x));
            }
            Kurve? k = Anpassen(punkte);
            if (k.HasValue)
                hinweise?.Add(Satzname(s) + ": kubische Teillastkurve auf [" + Zahl(von) + "; 1] quadratisch angepasst");
            return k;
        }

        /// <summary>
        /// Setzt die Felder von Teillast und Takten eines Katalogsatzes aus einem Copper-Satz (Fachkonzept 4.3): die
        /// Verdichterregelung immer, wenn bekannt; eine Kurve, die die Plausibilität besteht, mit <c>Teillast_Weg</c>
        /// <c>KURVE</c>, den normierten Beiwerten und x_u; die lineare Kurve als <c>LINEAR</c> ohne Beiwerte; eine
        /// unplausible Kurve lässt den Weg leer und meldet es in <paramref name="hinweise"/>. C_d und Randweg bleiben leer
        /// (Vorgabe).
        /// </summary>
        public static void Uebernehmen(KaeltemaschineModel m, KaeltemaschinenKurvensatz s, IList<string> hinweise = null)
        {
            if (m == null || s == null) return;
            m.Verdichterregelung = Verdichterregelung(s.Verdichter, s.Drehzahl);
            Kurve? roh = Rohkurve(s, hinweise);
            if (!roh.HasValue) return;
            Kurve? k = Normieren(roh.Value.A, roh.Value.B, roh.Value.C);
            if (k.HasValue && IstLinear(k.Value))
            {
                m.Teillast_Weg = KaeltemaschineTeillastSchema.WEG_LINEAR;
                return;
            }
            double? xu = UntereGueltigkeit(s);
            double pruefXu = xu ?? (m.Mindestteillast_Prozent.HasValue ? m.Mindestteillast_Prozent.Value / 100.0 : 0.0);
            if (!k.HasValue || !Bereich(k.Value) ||
                !KaeltemaschineStammCtrl.KurvePlausibel(k.Value.A, k.Value.B, k.Value.C, pruefXu))
            {
                hinweise?.Add(Satzname(s) + ": Teillastkurve nicht plausibel, Teillast ohne Kurve übernommen");
                return;
            }
            Setzen(m, k.Value, xu);
        }

        /// <summary>Setzt Weg <c>KURVE</c>, Beiwerte und x_u eines Satzes.</summary>
        public static void Setzen(KaeltemaschineModel m, Kurve k, double? xu)
        {
            m.Teillast_Weg = KaeltemaschineTeillastSchema.WEG_KURVE;
            m.Teillastkurve_a = k.A;
            m.Teillastkurve_b = k.B;
            m.Teillastkurve_c = k.C;
            m.Teillastkurve_Lastgrad_Min = xu;
        }

        /// <summary>Liegen die Beiwerte in den Eingabegrenzen (Fachkonzept 4.1)?</summary>
        public static bool Bereich(Kurve k) =>
            k.A >= KaeltemaschineTeillastSchema.KURVE_A_MIN && k.A <= KaeltemaschineTeillastSchema.KURVE_A_MAX &&
            k.B >= KaeltemaschineTeillastSchema.KURVE_BC_MIN && k.B <= KaeltemaschineTeillastSchema.KURVE_BC_MAX &&
            k.C >= KaeltemaschineTeillastSchema.KURVE_BC_MIN && k.C <= KaeltemaschineTeillastSchema.KURVE_BC_MAX;

        /// <summary>
        /// Die Vorgabekurve einer Verdichterregelung (Fachkonzept 4.4, KM3‑Q3) aus <see cref="KaelteFestwerte"/>;
        /// <c>null</c> ohne oder mit unbekannter Regelung (dann gilt linear).
        /// </summary>
        public static Kurve? Vorgabekurve(string verdichterregelung) => verdichterregelung switch
        {
            KaeltemaschineTeillastSchema.REGELUNG_EIN_AUS =>
                new Kurve(KaelteFestwerte.VORGABEKURVE_EIN_AUS_A, KaelteFestwerte.VORGABEKURVE_EIN_AUS_B, KaelteFestwerte.VORGABEKURVE_EIN_AUS_C),
            KaeltemaschineTeillastSchema.REGELUNG_STUFEN =>
                new Kurve(KaelteFestwerte.VORGABEKURVE_STUFEN_A, KaelteFestwerte.VORGABEKURVE_STUFEN_B, KaelteFestwerte.VORGABEKURVE_STUFEN_C),
            KaeltemaschineTeillastSchema.REGELUNG_DREHZAHL =>
                new Kurve(KaelteFestwerte.VORGABEKURVE_DREHZAHL_A, KaelteFestwerte.VORGABEKURVE_DREHZAHL_B, KaelteFestwerte.VORGABEKURVE_DREHZAHL_C),
            _ => null
        };

        private static string Satzname(KaeltemaschinenKurvensatz s) => "Kurvensatz " + (s?.Nr ?? "");

        private static double Runden(double x) => Math.Round(x, NACHKOMMA_BEIWERTE, MidpointRounding.AwayFromZero);

        private static bool Endlich(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        private static string Zahl(double x) => x.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
