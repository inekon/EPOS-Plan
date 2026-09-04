using System.Collections.Generic;
using SkiaSharp;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das Vorher/Nachher-Bild der Lastspitzenkappung (iU9-W12.6, Vorarbeit W12.0h).
    ///
    /// <para><b>Kein neuer Renderer.</b> Der Vorlaeufer
    /// (<c>Form_PeakShaving.ChartZeichnen</c> :682-728) zeichnete drei Linien ueber
    /// dem Jahresverlauf, davon eine — den Ladezustand — auf einer SEKUNDAERACHSE,
    /// „weil kWh und kW nicht dieselbe Skala teilen". Genau das kann
    /// <see cref="ChartRenderer.ErzeugerStapel"/> seit iU9-W11a (Bild B3): Linien
    /// ohne Stapel, eine Reihe mit eigener Skala rechts, y2 ab null und ohne
    /// Hauptgitter, 1 240 x 560. Deshalb entsteht hier KEINE neue Renderer-Methode
    /// und keine neue ChartProbe — die Zahl der Bilder bleibt 30.</para>
    ///
    /// <para><b>Die drei Farben stehen woertlich im Vorlaeufer</b>: Lastgang ohne
    /// Speicher (190, 90, 90), mit Speicher (40, 110, 180), Ladezustand
    /// (120, 130, 140).</para>
    ///
    /// <para><b>Das Raster ergibt sich aus der Reihenlaenge.</b> 8 760 oder 35 040 —
    /// <c>ChartRenderer.XAchse</c> rechnet die vier Jahresstundenmarken selbst um.
    /// Der Vorlaeufer brauchte dafuer zwei Schalter (<c>MaxXVALUE</c> UND
    /// <c>MitViertelStunde</c>), sonst kappte <c>AddSeries</c> auf 8 760 Punkte.</para>
    ///
    /// <para><b>Zwei Abweichungen, bewusst</b> (A-Zeilen des Protokolls W12.6):
    /// Die y-Obergrenze ist die geglaettete Datenobergrenze des Renderers statt
    /// <c>PAltMax x 1,05</c>, und eine x-Achsenbeschriftung kennt das Bild nicht —
    /// die Jahresstundenmarken stehen an der Achse.</para>
    /// </summary>
    public static class PeakShavingBild
    {
        /// <summary>Lastgang ohne Speicher — woertlich <c>Color.FromArgb(190, 90, 90)</c>.</summary>
        public static readonly SKColor FarbeAlt = new SKColor(190, 90, 90);

        /// <summary>Lastgang mit Speicher — woertlich <c>Color.FromArgb(40, 110, 180)</c>.</summary>
        public static readonly SKColor FarbeNeu = new SKColor(40, 110, 180);

        /// <summary>Ladezustand — woertlich <c>Color.FromArgb(120, 130, 140)</c>.</summary>
        public static readonly SKColor FarbeSoC = new SKColor(120, 130, 140);

        /// <summary>
        /// Zeichnet den Lastgang vor und nach der Kappung.
        /// </summary>
        /// <param name="r">Das Ergebnis des Laufs; <c>null</c> liefert <c>null</c>.</param>
        /// <param name="mitSoC">
        /// Den Ladezustand auf der Sekundaerachse mitzeichnen — der Schalter
        /// „Ladezustand im Diagramm zeigen" des Vorlaeufers.
        /// </param>
        public static byte[] Lastgang(PeakShavingErgebnis r, bool mitSoC)
        {
            if (r == null) return null;

            List<ChartRenderer.Reihe> linien = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(MyResource.Resource.PEAK_SERIE_ALT, r.PAltKw, FarbeAlt),
                new ChartRenderer.Reihe(MyResource.Resource.PEAK_SERIE_NEU, r.PNeuKw, FarbeNeu)
            };

            ChartRenderer.Reihe soc = mitSoC
                ? new ChartRenderer.Reihe(MyResource.Resource.PEAK_SERIE_SOC, r.SoCKwh, FarbeSoC)
                : null;

            return ChartRenderer.ErzeugerStapel(
                MyResource.Resource.PEAK_CHART_TITEL,
                new List<ChartRenderer.Reihe>(),      // kein Stapel — nur Linien
                linien,
                null,                                  // keine Summenkontur
                MyResource.Resource.PEAK_CHART_Y,
                ChartRenderer.Achse.Jahresstunden,
                false,                                 // Ganglinie, keine Dauerlinie
                soc,
                MyResource.Resource.PEAK_CHART_Y2);
        }
    }
}
