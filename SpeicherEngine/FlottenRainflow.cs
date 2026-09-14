using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeicherEngine;

/// <summary>Zusammenhaengende Rainflow-Auswertung; sie behauptet keine kalendarische Alterung.</summary>
public static class FlottenRainflow
{
    /// <summary>
    /// Zerlegt einen zusammenhaengenden SoC-Verlauf in Voll- und Halbzyklen und
    /// summiert daraus den Miner-Schaden (Spezifikation 8.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Verarbeitet wird die AELTERE Schwingweite, sobald die neuere mindestens ebenso
    /// gross ist; offene Randzyklen zaehlen mit 0,5. Der Schaden lautet
    /// <c>D = Summe Anzahl_k / ZyklenBisEol(DoD_k)</c>, wobei zwischen den
    /// Stuetzstellen LOGARITHMISCH interpoliert wird.
    /// </para>
    /// <para>
    /// Ausserhalb der gelieferten Stuetzstellen wird NICHT extrapoliert, sondern
    /// abgebrochen. Eine leere Kurve liefert die gezaehlten Zyklen ohne Schaden. Eine
    /// kalendarische Alterung behauptet die Auswertung ausdruecklich nicht.
    /// </para>
    /// </remarks>
    /// <param name="socVerlauf">Der SoC-Verlauf [-] einer Einheit, Werte von 0 bis 1, in zeitlicher Reihenfolge.</param>
    /// <param name="lebensdauerkurve">Die Stuetzstellen (Entladetiefe, Zyklen bis EOL); leer = nur zaehlen.</param>
    /// <returns>Die gezaehlten Zyklen und der Miner-Schaden [-].</returns>
    /// <exception cref="ArgumentNullException">Einer der beiden Eingaenge ist <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Der SoC-Verlauf oder die Lebensdauerkurve enthaelt ungueltige Werte.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Eine Zyklustiefe liegt ausserhalb der gelieferten Lebensdauerkurve.</exception>
    public static FlottenRainflowErgebnis Auswerten(
        IReadOnlyList<double> socVerlauf,
        IReadOnlyList<FlottenRainflowPunkt> lebensdauerkurve)
    {
        if (socVerlauf is null) throw new ArgumentNullException(nameof(socVerlauf));
        if (lebensdauerkurve is null) throw new ArgumentNullException(nameof(lebensdauerkurve));
        if (socVerlauf.Any(x => !double.IsFinite(x) || x < -1e-9 || x > 1 + 1e-9))
            throw new ArgumentException("SoC-Verlauf enthaelt ungueltige Werte.");

        var reversals = Wendepunkte(socVerlauf);
        var stack = new List<double>();
        var cycles = new List<FlottenRainflowZyklus>();
        foreach (var point in reversals)
        {
            stack.Add(point);
            while (stack.Count >= 3)
            {
                var older = Math.Abs(stack[^2] - stack[^3]);
                var newer = Math.Abs(stack[^1] - stack[^2]);
                if (older > newer) break;
                if (stack.Count == 3)
                {
                    Add(cycles, stack[0], stack[1], 0.5);
                    stack.RemoveAt(0);
                }
                else
                {
                    Add(cycles, stack[^3], stack[^2], 1.0);
                    var last = stack[^1];
                    stack.RemoveRange(stack.Count - 3, 3);
                    stack.Add(last);
                }
            }
        }
        for (var i = 0; i + 1 < stack.Count; i++) Add(cycles, stack[i], stack[i + 1], 0.5);

        var result = new FlottenRainflowErgebnis { Zyklen = cycles };
        if (lebensdauerkurve.Count == 0) return result;
        // DIE BEDINGUNG STEHT EINMAL - in PruefeKurve (Auftrag #257). Der Wortlaut der
        // Ausnahme bleibt, was er war; die Vorpruefung des Kerns ruft dieselbe Funktion
        // und sagt VORHER, welcher Punkt woran haengt.
        if (PruefeKurve(lebensdauerkurve) is not null)
            throw new ArgumentException("Rainflow-Kurve ist ungueltig.");
        var curve = lebensdauerkurve.OrderBy(x => x.Entladetiefe).ToArray();
        foreach (var cycle in cycles)
            result.Schaden += cycle.Anzahl / InterpoliereLogZyklen(cycle.Entladetiefe, curve);
        return result;
    }

    /// <summary>
    /// Prueft eine Lebensdauerkurve auf GENAU die Bedingungen, unter denen
    /// <see cref="Auswerten"/> sie annimmt (Auftrag #257).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Zulaessig ist ein Punkt, dessen Entladetiefe endlich und groesser 0 bis
    /// hoechstens 1 ist, dessen Zyklenzahl endlich und groesser 0 ist und dessen
    /// Entladetiefe in der Kurve nur EINMAL vorkommt. Eine LEERE Kurve ist zulaessig:
    /// Dann werden Zyklen gezaehlt und kein Miner-Schaden gerechnet.
    /// </para>
    /// <para>
    /// Gemeldet wird der ERSTE Mangel in der Reihenfolge der GELIEFERTEN Liste, nicht
    /// der sortierten: Der Editor zeigt genau diese Reihenfolge, und ein Befund soll auf
    /// die Zeile zeigen, die der Anwender vor sich hat.
    /// </para>
    /// </remarks>
    /// <param name="lebensdauerkurve">Die Stuetzstellen; <c>null</c> oder leer = zulaessig.</param>
    /// <returns>Der erste Mangel; <c>null</c>, wenn die Kurve gilt oder leer ist.</returns>
    public static FlottenRainflowBefund? PruefeKurve(IReadOnlyList<FlottenRainflowPunkt>? lebensdauerkurve)
    {
        if (lebensdauerkurve is null || lebensdauerkurve.Count == 0) return null;
        for (var i = 0; i < lebensdauerkurve.Count; i++)
        {
            var punkt = lebensdauerkurve[i];
            if (punkt is null) return Mangel(i, FlottenRainflowMangel.NichtAusgefuellt);
            // DER FRISCH ANGELEGTE PUNKT ZUERST: 0/0 ist kein Wertebereichsfehler,
            // sondern ein noch nicht ausgefuelltes Feldpaar - und genau so heisst es.
            if (punkt.Entladetiefe == 0 && punkt.ZyklenBisEol == 0)
                return Mangel(i, FlottenRainflowMangel.NichtAusgefuellt);
            if (!double.IsFinite(punkt.Entladetiefe) || punkt.Entladetiefe <= 0 || punkt.Entladetiefe > 1)
                return Mangel(i, FlottenRainflowMangel.EntladetiefeAusserhalb);
            if (!double.IsFinite(punkt.ZyklenBisEol) || punkt.ZyklenBisEol <= 0)
                return Mangel(i, FlottenRainflowMangel.ZyklenNichtPositiv);
            for (var j = 0; j < i; j++)
                if (lebensdauerkurve[j].Entladetiefe == punkt.Entladetiefe)
                    return Mangel(i, FlottenRainflowMangel.EntladetiefeDoppelt);
        }
        return null;
    }

    private static FlottenRainflowBefund Mangel(int index, FlottenRainflowMangel mangel)
        => new FlottenRainflowBefund { Index = index, Mangel = mangel };

    private static List<double> Wendepunkte(IReadOnlyList<double> values)
    {
        var compact = new List<double>();
        foreach (var x in values)
            if (compact.Count == 0 || Math.Abs(compact[^1] - x) > 1e-12) compact.Add(x);
        if (compact.Count <= 2) return compact;
        var result = new List<double> { compact[0] };
        for (var i = 1; i < compact.Count - 1; i++)
        {
            var left = compact[i] - compact[i - 1];
            var right = compact[i + 1] - compact[i];
            if (left * right < 0) result.Add(compact[i]);
        }
        result.Add(compact[^1]);
        return result;
    }

    private static void Add(List<FlottenRainflowZyklus> result, double a, double b, double count)
    {
        var range = Math.Abs(a - b);
        if (range <= 1e-12) return;
        result.Add(new FlottenRainflowZyklus
        {
            Entladetiefe = range,
            MittlererSoc = (a + b) / 2,
            Anzahl = count
        });
    }

    private static double InterpoliereLogZyklen(double dod, IReadOnlyList<FlottenRainflowPunkt> curve)
    {
        if (dod < curve[0].Entladetiefe - 1e-12 || dod > curve[^1].Entladetiefe + 1e-12)
            throw new ArgumentOutOfRangeException(nameof(dod),
                "Rainflow-Zyklustiefe liegt ausserhalb der gelieferten Lebensdauerkurve.");
        for (var i = 0; i < curve.Count; i++)
        {
            if (Math.Abs(dod - curve[i].Entladetiefe) <= 1e-12) return curve[i].ZyklenBisEol;
            if (i == 0 || dod > curve[i].Entladetiefe) continue;
            var a = curve[i - 1];
            var b = curve[i];
            var f = (dod - a.Entladetiefe) / (b.Entladetiefe - a.Entladetiefe);
            return Math.Exp(Math.Log(a.ZyklenBisEol) + f *
                (Math.Log(b.ZyklenBisEol) - Math.Log(a.ZyklenBisEol)));
        }
        return curve[^1].ZyklenBisEol;
    }
}

/// <summary>Der Grund, aus dem ein Punkt der Lebensdauerkurve nicht gilt (Auftrag #257).</summary>
public enum FlottenRainflowMangel
{
    /// <summary>Der Punkt ist NICHT AUSGEFUELLT: Entladetiefe 0 UND Zyklen 0.</summary>
    NichtAusgefuellt = 0,

    /// <summary>Die Entladetiefe ist nicht endlich oder liegt nicht im Bereich groesser 0 bis 1.</summary>
    EntladetiefeAusserhalb = 1,

    /// <summary>Die Zyklen bis zum End-of-Life-Kriterium sind nicht endlich oder nicht groesser 0.</summary>
    ZyklenNichtPositiv = 2,

    /// <summary>Dieselbe Entladetiefe steht schon in einem frueheren Punkt der Kurve.</summary>
    EntladetiefeDoppelt = 3
}

/// <summary>Der erste Mangel einer Lebensdauerkurve: welcher Punkt, welcher Grund.</summary>
public sealed class FlottenRainflowBefund
{
    /// <summary>Der Punkt in der Reihenfolge der GELIEFERTEN Liste, 0-basiert.</summary>
    public int Index { get; set; }

    /// <summary>Der Grund, aus dem der Punkt nicht gilt.</summary>
    public FlottenRainflowMangel Mangel { get; set; }
}
