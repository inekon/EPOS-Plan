#nullable enable

using System;

namespace WindowsFormsApplication1
{
    /// <summary>Das Ergebnis der Rücklaufprüfung einer Stunde.</summary>
    /// <param name="Aus">θ_R,WP &gt; θ_R,grenz — die Wärmepumpe liefert nichts (Grund <c>RUECKLAUF_MAX</c>).</param>
    /// <param name="Faktor">R744-Faktor f auf Leistung und COP (1 ohne Abwertung, 0 bei <paramref name="Aus"/>).</param>
    internal readonly record struct Ruecklaufpruefung(bool Aus, double Faktor)
    {
        /// <summary>Ohne Rücklauf oder ohne Grenze: keine Wirkung.</summary>
        internal static readonly Ruecklaufpruefung Ohne = new Ruecklaufpruefung(false, 1.0);
    }

    /// <summary>
    /// <b>Die Rücklaufgrenze der Wärmepumpe und des BHKW</b> (Fachkonzept Übergabegrenze 4.4, 2.11, 2.12; Umsetzungskonzept
    /// 3.1, 3.2 Schritte 3 und 8):
    /// <code>
    /// θ_R,WP(h) &gt; θ_R,grenz   →   Φ_WP = 0                       Grund RUECKLAUF_MAX
    /// R744:  f(h) = 1 − k · (θ_R,WP − θ_R,bez)   für θ_R,bez &lt; θ_R,WP ≤ Ruecklauf_Max, sonst f = 1
    ///        Φ_WP = Φ_KF · f ,   COP = COP_KF · f ,   Stromaufnahme unverändert
    /// </code>
    /// θ_R,WP ist der Rücklauf zur Wärmepumpe: direkt der Heizkreisrücklauf, an Weiche oder Überströmventil der gemischte
    /// (<see cref="Hydraulikgrenze"/>), am Puffer die unterste Pufferzone. Ohne Rücklauf (NaN) ruht die Grenze.
    /// Am BHKW: Rücklauf ≥ <c>Ruecklauf_Max</c> → das Modul liefert in der Stunde nichts (Abschaltgrenze des
    /// Motorkühlkreises); leeres Feld = keine Grenze. Reine Zahlen, deterministisch.
    /// </summary>
    internal static class Ruecklaufgrenze
    {
        /// <summary>Die Prüfung der Wärmepumpe bei Rücklauf <paramref name="ruecklaufWpC"/> gegen <paramref name="g"/>.</summary>
        internal static Ruecklaufpruefung Pruefen(Geraetegrenzen? g, double ruecklaufWpC)
        {
            if (g == null || double.IsNaN(ruecklaufWpC) || double.IsInfinity(ruecklaufWpC)) return Ruecklaufpruefung.Ohne;
            double grenze = g.RuecklaufGrenzeC;
            if (!double.IsNaN(grenze) && ruecklaufWpC > grenze) return new Ruecklaufpruefung(true, 0.0);
            return new Ruecklaufpruefung(false, Faktor(g, ruecklaufWpC));
        }

        /// <summary>
        /// Der R744-Faktor f = 1 − k/100·(θ_R − θ_R,bez) für θ_R,bez &lt; θ_R ≤ Ruecklauf_Max (leeres Feld: θ_R,grenz),
        /// sonst 1; nie unter 0. Ohne Abwertung (k = 0) immer 1 — dann wirkt allein die harte Grenze.
        /// </summary>
        internal static double Faktor(Geraetegrenzen g, double ruecklaufC)
        {
            if (g == null || !g.Abwertung || double.IsNaN(ruecklaufC) || double.IsNaN(g.BezugsruecklaufC)) return 1.0;
            double oben = double.IsNaN(g.RuecklaufFeldC) ? g.RuecklaufGrenzeC : g.RuecklaufFeldC;
            if (!(ruecklaufC > g.BezugsruecklaufC)) return 1.0;
            if (!double.IsNaN(oben) && ruecklaufC > oben) return 1.0;
            double f = 1.0 - g.AbwertungProzentJeK / 100.0 * (ruecklaufC - g.BezugsruecklaufC);
            return f > 0.0 ? f : 0.0;
        }

        /// <summary>
        /// BHKW (UB‑Q9, U‑3): liefert das Modul in der Stunde nichts? Wahr, wenn <paramref name="ruecklaufMaxC"/> gepflegt ist
        /// und der Rücklauf zum BHKW <paramref name="ruecklaufC"/> darüber liegt (wie bei der Wärmepumpe); ohne Feld oder ohne Rücklauf nie.
        /// </summary>
        internal static bool BhkwAus(double? ruecklaufMaxC, double ruecklaufC)
            => ruecklaufMaxC.HasValue && !double.IsNaN(ruecklaufMaxC.Value)
               && !double.IsNaN(ruecklaufC) && !double.IsInfinity(ruecklaufC) && ruecklaufC > ruecklaufMaxC.Value;

        /// <summary>
        /// Prüfregel des BHKW-Stammblatts (U‑3): der Auslegungsrücklauf <c>Ruecklauf</c> muss unter der Abschaltgrenze
        /// <c>Ruecklauf_Max</c> liegen — sonst ein Hinweis (wahr). Ohne Feld oder ohne Auslegungsrücklauf kein Hinweis.
        /// </summary>
        internal static bool BhkwAuslegungHinweis(double auslegungRuecklaufC, double? ruecklaufMaxC)
            => ruecklaufMaxC.HasValue && !double.IsNaN(ruecklaufMaxC.Value)
               && auslegungRuecklaufC > 0.0 && !double.IsNaN(auslegungRuecklaufC) && auslegungRuecklaufC >= ruecklaufMaxC.Value;
    }
}
