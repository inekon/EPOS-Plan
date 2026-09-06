using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Wirkungsgradkennlinie eines Wechselrichters</b> — sechs Stützstellen,
    /// der europäische Wirkungsgrad und die Umrechnung aus den Sandia-Koeffizienten
    /// (Konzept Wechselrichter 3.3, Anwenderentscheid <b>W6‑E‑2‑Q1</b> vom 06.09.2026:
    /// „Stützstellen, Sandia nur mitschreiben").
    ///
    /// <para><b>Warum Stützstellen und nicht das Sandia-Modell.</b> Das
    /// Sandia-Wechselrichtermodell (King u. a. 2007) braucht <c>U_dc</c>, die
    /// MPP-Spannung des Strangs in dieser Stunde. Die entsteht erst mit einem
    /// Ein-Dioden-Modell (Stufe E3 des PV-Ertragsmodells, zurückgestellt). Ohne E3
    /// bliebe nur <c>U_dc = U_dco</c> — dann fallen C1…C3 heraus, und übrig bleibt eine
    /// Parabel, also wieder eine Kennlinie mit drei Freiheitsgraden. Der
    /// Genauigkeitsgewinn wäre null, der Aufwand nicht (Konzept 3.3.2).</para>
    ///
    /// <para><b>Diese Klasse rechnet NICHT im Rechenweg.</b> Sie gehört zur
    /// Katalogpflege: Der Import füllt damit die sechs Spalten
    /// <c>Eta05…Eta100</c> und <c>Eta_Euro</c>. Der Rechenweg (Stufe S3) liest später
    /// die Spalten, nicht diese Klasse — deshalb bleibt der Referenzlauf in Stufe S1
    /// byte-gleich.</para>
    /// </summary>
    public static class WechselrichterKennlinie
    {
        /// <summary>
        /// Die sechs Auslastungen, an denen die Kennlinie abgelegt ist — Anteile der
        /// AC-Nennleistung (5, 10, 20, 30, 50 und 100 %).
        /// </summary>
        /// <remarks>
        /// <b>Ohne 75 %</b> (Entscheidungsfrage W6‑E‑2‑Q1, Empfehlung angenommen):
        /// <c>Eta_Euro</c> ist der Ausweis, den Datenblätter nennen; die kalifornische
        /// CEC-Wichtung, die zusätzlich 75 % braucht, ist in Europa ohne Belang.
        /// </remarks>
        public static readonly double[] STUETZSTELLEN = { 0.05, 0.10, 0.20, 0.30, 0.50, 1.00 };

        /// <summary>
        /// Die europäische Wichtung zu <see cref="STUETZSTELLEN"/>:
        /// 0,03 / 0,06 / 0,13 / 0,10 / 0,48 / 0,20. Ihre Summe ist 1.
        /// </summary>
        public static readonly double[] EURO_GEWICHTE = { 0.03, 0.06, 0.13, 0.10, 0.48, 0.20 };

        /// <summary>
        /// Der europäische Wirkungsgrad aus den sechs Stützstellen (Konzept 3.3.1):
        /// <c>η_euro = 0,03·η5 + 0,06·η10 + 0,13·η20 + 0,10·η30 + 0,48·η50 + 0,20·η100</c>.
        /// </summary>
        /// <param name="etas">
        /// Sechs Stützstellen in der Reihenfolge von <see cref="STUETZSTELLEN"/>.
        /// </param>
        /// <returns>
        /// Der gewichtete Wirkungsgrad, oder <c>null</c>, sobald eine Stützstelle
        /// fehlt — <b>ein Ausweis aus Teilwerten wäre eine erfundene Zahl</b>. Der
        /// Katalog trägt <c>Eta_Euro</c> dann NULL, und die Verwaltung zeigt einen
        /// Strich.
        /// </returns>
        public static double? EuroWirkungsgrad(double?[] etas)
        {
            if (etas == null || etas.Length != EURO_GEWICHTE.Length) return null;

            double summe = 0.0;
            for (int i = 0; i < etas.Length; i++)
            {
                if (!etas[i].HasValue) return null;
                summe += EURO_GEWICHTE[i] * etas[i].Value;
            }
            return summe;
        }

        /// <summary>
        /// <b>Sandia → sechs Stützstellen</b> bei <c>U_dc = U_dco</c> (Konzept 3.3.3).
        ///
        /// <para>Dort gilt <c>A = Pdco</c>, <c>B = Pso</c>, <c>C = C0</c>, und die
        /// Modellgleichung wird ein geschlossener Ausdruck:</para>
        /// <code>
        /// P_AC(P_DC) = [ Paco/(Pdco − Pso) − C0·(Pdco − Pso) ] · (P_DC − Pso)
        ///              + C0·(P_DC − Pso)²
        /// </code>
        /// <para>Für jede Stützstelle <c>x</c> wird <c>P_DC</c> so gesucht, dass
        /// <c>P_AC = x·Paco</c> ist. Mit <c>y = P_DC − Pso</c> ist das eine QUADRATISCHE
        /// Gleichung <c>C0·y² + k·y − x·Paco = 0</c> mit
        /// <c>k = Paco/(Pdco − Pso) − C0·(Pdco − Pso)</c> — in einem Schritt lösbar;
        /// <c>η(x) = x·Paco / (y + Pso)</c>.</para>
        ///
        /// <para><b>Prüfwert:</b> Bei <c>x = 1</c> ist <c>y = Pdco − Pso</c> und damit
        /// <c>η100 = Paco/Pdco</c> <i>exakt</i> — der Wirkungsgrad, den auch das
        /// Datenblatt bei Nennlast nennt. Der Nachweis
        /// <c>WechselrichterKennlinieTests</c> rechnet genau das nach.</para>
        ///
        /// <para><b>Welche Wurzel.</b> <c>C0</c> ist im CEC-Bestand klein und negativ;
        /// beide Wurzeln sind dann positiv, und physikalisch gemeint ist die KLEINERE —
        /// die größere liegt jenseits des Scheitels, wo die Modellparabel wieder fällt.
        /// Ein <c>C0 = 0</c> (bzw. numerisch null) ist der lineare Fall und wird
        /// getrennt gerechnet, statt durch null zu teilen.</para>
        /// </summary>
        /// <param name="paco">AC-Nennleistung [W] (CEC <c>Paco</c>).</param>
        /// <param name="pdco">DC-Leistung bei AC-Nennleistung [W] (CEC <c>Pdco</c>).</param>
        /// <param name="pso">Einschaltschwelle [W] (CEC <c>Pso</c>).</param>
        /// <param name="c0">Sandia-Koeffizient C0 [1/W].</param>
        /// <returns>
        /// Sechs Stützstellen in der Reihenfolge von <see cref="STUETZSTELLEN"/>. Eine
        /// Stützstelle, die sich nicht bestimmen lässt oder außerhalb (0; 1] fiele, ist
        /// <c>null</c> — <b>eine unmögliche Zahl wird nicht geschrieben</b>. Sind die
        /// Eingangswerte selbst unbrauchbar (<c>Paco ≤ 0</c>,
        /// <c>Pdco ≤ Pso</c>), sind alle sechs <c>null</c>.
        /// </returns>
        public static double?[] AusSandia(double paco, double pdco, double pso, double c0)
        {
            var etas = new double?[STUETZSTELLEN.Length];

            if (paco <= 0.0 || pdco <= pso) return etas;

            double spanne = pdco - pso;
            double k = paco / spanne - c0 * spanne;
            if (k <= 0.0) return etas;

            for (int i = 0; i < STUETZSTELLEN.Length; i++)
            {
                double ziel = STUETZSTELLEN[i] * paco;
                double y;

                if (Math.Abs(c0) < 1e-14)
                {
                    // Linearer Fall: k·y = ziel.
                    y = ziel / k;
                }
                else
                {
                    double diskriminante = k * k + 4.0 * c0 * ziel;
                    if (diskriminante < 0.0) continue;

                    double wurzel = Math.Sqrt(diskriminante);
                    double y1 = (-k + wurzel) / (2.0 * c0);
                    double y2 = (-k - wurzel) / (2.0 * c0);

                    y = KleinerePositive(y1, y2);
                    if (double.IsNaN(y)) continue;
                }

                double pDc = y + pso;
                if (pDc <= 0.0) continue;

                double eta = ziel / pDc;
                if (eta <= 0.0 || eta > 1.0) continue;

                etas[i] = eta;
            }

            return etas;
        }

        /// <summary>
        /// Die kleinere der beiden positiven Wurzeln; <c>NaN</c>, wenn keine positiv
        /// ist. Siehe „Welche Wurzel" bei <see cref="AusSandia"/>.
        /// </summary>
        private static double KleinerePositive(double a, double b)
        {
            bool aOk = a > 0.0 && !double.IsNaN(a) && !double.IsInfinity(a);
            bool bOk = b > 0.0 && !double.IsNaN(b) && !double.IsInfinity(b);

            if (aOk && bOk) return Math.Min(a, b);
            if (aOk) return a;
            if (bOk) return b;
            return double.NaN;
        }
    }
}
