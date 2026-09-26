using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Analyse „Band je Größenklasse"</b> — ein benanntes Zusatzmaß neben dem Bandkriterium (b),
    /// <b>ohne Ampel</b>. Die Ampel bleibt das Konzeptkriterium: die Messspitze im P85–P95-Band der
    /// gerechneten Dauerlinie (Parameter <c>Zapfprofil.Validierung.Band.*</c>, Prüfliste ZU21
    /// bestätigt). Diese Analyse liefert die Zahlen für einen Entscheid über das Band, nicht den
    /// Entscheid.
    ///
    /// <para><b>Zwei Maße je Objekt.</b></para>
    /// <list type="number">
    /// <item><b>Perzentil der Messspitze in der Dauerlinie</b>: Welches Quantil der gerechneten
    /// Dauerlinie trifft die Messspitze? Das Konzeptband sagt „zwischen 0,85 und 0,95"; die Analyse
    /// zeigt, wo die Messspitzen je Größenklasse wirklich liegen, und daraus, welches Band sie
    /// eingefangen hätte. 1 heißt: auf oder über der größten gerechneten Stunde — dann fängt kein
    /// Quantil der Dauerlinie die Messspitze.</item>
    /// <item><b>Die Messspitze gegen die Ensemblespitzen</b>: die Jahresspitze jeder Realisierung,
    /// bezogen auf die Spitze der verglichenen Realisierung (r = 0). So wird die Messspitze
    /// (Spitzenverhältnis, ebenfalls auf die verglichene Reihe bezogen) gegen die <b>Streuung der
    /// gerechneten Jahresspitzen</b> gehalten statt gegen ein Quantil einer Reihe — der Vorschlag des
    /// ersten Laufs für kleine Einheitenzahlen. Beide Seiten sind Verhältnisse; die Ensemblespitzen
    /// sind Spitzen der Zapfung, die verglichene Reihe trägt bei der Bilanzgrenze
    /// <c>MitVerteilung</c> auch die Zirkulation — dort ist das Maß eine Näherung.</item>
    /// </list>
    ///
    /// <para><b>Die Größenklassen</b> sind eine Setzung der Analyse: unter 10 Einheiten (Haushalte),
    /// 10 bis 99 (Mehrfamilienhäuser), ab 100 (große Objekte).</para>
    /// </summary>
    internal static class Bandanalyse
    {
        /// <summary>Die Grenzen der Größenklassen (untere Grenzen der zweiten und dritten Klasse).</summary>
        internal const int KLASSE_MITTEL = 10;
        internal const int KLASSE_GROSS = 100;

        /// <summary>Die Größenklasse einer Einheitenzahl als Text.</summary>
        internal static string Klasse(int n)
            => n < KLASSE_MITTEL ? "N < 10" : n < KLASSE_GROSS ? "10 ≤ N < 100" : "N ≥ 100";

        /// <summary>
        /// Die beiden Maße eines Objekts aus der verglichenen Reihe und den Ensemblespitzen. Ohne
        /// Spitzenverhältnis oder ohne gerechnete Spitze bleibt alles leer; ohne Ensemble bleibt der
        /// Ensembleteil leer.
        /// </summary>
        internal static void Objekt(Objektbefund b, Bilanzreihe verglichen, IReadOnlyList<double> ensembleSpitzen)
        {
            if (b.Spitzenverhaeltnis is not double s || !(s > 0.0) || verglichen?.StundenKwh == null) return;
            IReadOnlyList<double> reihe = verglichen.StundenKwh;
            double max = verglichen.GroessterStundenwertKw;
            if (!(max > 0.0) || reihe.Count == 0) return;

            // Relativ zur Spitze der Reihe - dieselbe Bezugsgröße wie das Spitzenverhältnis; ein
            // Absolutwert der Messung entsteht dabei nicht.
            int darunter = 0;
            foreach (double w in reihe) if (w / max <= s) darunter++;
            b.MessspitzePerzentil = darunter / (double)reihe.Count;

            if (ensembleSpitzen == null || ensembleSpitzen.Count < 2 || !(ensembleSpitzen[0] > 0.0)) return;
            double[] rho = ensembleSpitzen.Select(p => p / ensembleSpitzen[0]).ToArray();
            b.EnsembleUnten = rho.Min();
            b.EnsembleOben = rho.Max();
            b.EnsembleAnteilDarunter = rho.Count(r => r <= s) / (double)rho.Length;
        }

        /// <summary>Der Abschnitt des Sammelberichts: je Objekt die Maße, je Größenklasse die Verdichtung.</summary>
        internal static string Markdown(IReadOnlyList<Objektbefund> befunde)
        {
            var s = new StringBuilder();
            var liste = befunde.Where(b => b.Abbruch == null && b.MessspitzePerzentil.HasValue)
                               .OrderBy(b => b.Einheiten).ThenBy(b => b.Kennung, StringComparer.Ordinal).ToList();
            s.AppendLine("## Analyse: Band je Größenklasse (keine Ampel)");
            s.AppendLine();
            s.AppendLine("Zusatzmaß neben dem Bandkriterium; die Ampel bleibt das Konzeptkriterium. **Perzentil**:");
            s.AppendLine("welches Quantil der gerechneten Dauerlinie die Messspitze trifft (1 = auf oder über der");
            s.AppendLine("größten gerechneten Stunde). **Ensemble**: die Jahresspitzen der Realisierungen, bezogen");
            s.AppendLine("auf die verglichene Realisierung, gegen die die Messspitze (Spitzenverhältnis) steht.");
            s.AppendLine();
            if (liste.Count == 0)
            {
                s.AppendLine("Kein Objekt trägt die Maße.");
                s.AppendLine();
                return s.ToString();
            }
            s.AppendLine("| Kennung | N | Klasse | Spitzen­verhältnis | Perzentil der Messspitze | Ensemble­spitzen | Anteil der Ensemblespitzen darunter |");
            s.AppendLine("|---|---|---|---|---|---|---|");
            foreach (Objektbefund b in liste)
                s.Append("| ").Append(b.Kennung).Append(" | ").Append(Bericht.Ganz(b.Einheiten))
                 .Append(" | ").Append(Klasse(b.Einheiten))
                 .Append(" | ").Append(Bericht.Zahl(b.Spitzenverhaeltnis, 4))
                 .Append(" | ").Append(Bericht.Zahl(b.MessspitzePerzentil, 4))
                 .Append(" | ").Append(Bericht.Zahl(b.EnsembleUnten, 3)).Append(" … ").Append(Bericht.Zahl(b.EnsembleOben, 3))
                 .Append(" | ").Append(Bericht.Zahl(b.EnsembleAnteilDarunter, 2)).AppendLine(" |");
            s.AppendLine();

            s.AppendLine("| Klasse | Objekte | Perzentil kleinstes … Median … größtes | im Konzeptband | über der Rechenspitze | im Bereich der Ensemblespitzen |");
            s.AppendLine("|---|---|---|---|---|---|");
            foreach (IGrouping<string, Objektbefund> g in liste.GroupBy(b => Klasse(b.Einheiten)))
            {
                double[] q = g.Select(b => b.MessspitzePerzentil.Value).OrderBy(x => x).ToArray();
                double median = q.Length % 2 == 1 ? q[q.Length / 2] : 0.5 * (q[q.Length / 2 - 1] + q[q.Length / 2]);
                int imBand = g.Count(b => b.Lage == Spitzenlage.ImBand);
                int ueber = g.Count(b => b.Spitzenverhaeltnis > 1.0);
                int imEnsemble = g.Count(b => b.EnsembleUnten.HasValue
                                              && b.Spitzenverhaeltnis >= b.EnsembleUnten && b.Spitzenverhaeltnis <= b.EnsembleOben);
                s.Append("| ").Append(g.Key).Append(" | ").Append(Bericht.Ganz(q.Length))
                 .Append(" | ").Append(Bericht.Zahl(q[0], 4)).Append(" … ").Append(Bericht.Zahl(median, 4))
                 .Append(" … ").Append(Bericht.Zahl(q[q.Length - 1], 4))
                 .Append(" | ").Append(Bericht.Ganz(imBand)).Append(" | ").Append(Bericht.Ganz(ueber))
                 .Append(" | ").Append(Bericht.Ganz(imEnsemble)).AppendLine(" |");
            }
            s.AppendLine();
            return s.ToString();
        }
    }
}
