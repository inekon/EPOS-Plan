using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeicherEngine;

/// <summary>Zusammenhaengende Rainflow-Auswertung; sie behauptet keine kalendarische Alterung.</summary>
public static class FlottenRainflow
{
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
        var curve = lebensdauerkurve.OrderBy(x => x.Entladetiefe).ToArray();
        if (curve.Any(x => !double.IsFinite(x.Entladetiefe) || x.Entladetiefe <= 0 ||
            x.Entladetiefe > 1 || !double.IsFinite(x.ZyklenBisEol) || x.ZyklenBisEol <= 0) ||
            curve.Select(x => x.Entladetiefe).Distinct().Count() != curve.Length)
            throw new ArgumentException("Rainflow-Kurve ist ungueltig.");
        foreach (var cycle in cycles)
            result.Schaden += cycle.Anzahl / InterpoliereLogZyklen(cycle.Entladetiefe, curve);
        return result;
    }

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
