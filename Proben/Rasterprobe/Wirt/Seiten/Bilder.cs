using System.Collections.Generic;
using SkiaSharp;
using SvgProbe;
using WindowsFormsApplication1;

namespace Rasterprobe.Wirt.Seiten;

/// <summary>
/// Das PNG des Bestands zu den Pruefreihen der SVG-Probe (DG-1): dasselbe Bild,
/// derselbe Renderer, den die Anwendung heute zeigt — <c>ChartRenderer.Jahresgang</c>
/// mit 1 304 × 440 und den Hausfarben. Nur so ist der Vergleich SVG gegen PNG fair.
/// </summary>
internal static class Bilder
{
    public static byte[] Png(double[][] reihen)
    {
        var liste = new List<ChartRenderer.Reihe>();
        for (int r = 0; r < reihen.Length; r++)
            liste.Add(new ChartRenderer.Reihe("Reihe " + (r + 1), reihen[r],
                                              SKColor.Parse(SvgZeichner.FARBEN[r % SvgZeichner.FARBEN.Length])));
        return ChartRenderer.Jahresgang("Jahresgang der Prüfreihen (" + reihen.Length + " Reihen, PNG)",
                                        liste, "Monat", "kW");
    }
}
