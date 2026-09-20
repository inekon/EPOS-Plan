using System.Collections.Generic;
using SpeicherEngine;
using WindowsFormsApplication1.Zeichnung;

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
    /// <para><b>Die drei Reihen nennen ihre FARBROLLE</b> (DG-E5, Anwenderentscheid
    /// DG-Q8): Netzbezug ohne Speicher, Netzbezug mit Speicher und Speicherfuellstand.
    /// Ihre Vorgabefarben sind die des Bildes, aenderbar sind sie seither in
    /// Administration › Einstellungen › Diagramme — und im Bild ueber das Farbfeld
    /// ihres Legendeneintrags.</para>
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
        /// <summary>Lastgang ohne Speicher.</summary>
        public static readonly Farbrolle RolleAlt = Farbrolle.NETZ_OHNE_SPEICHER;

        /// <summary>Lastgang mit Speicher.</summary>
        public static readonly Farbrolle RolleNeu = Farbrolle.NETZ_MIT_SPEICHER;

        /// <summary>Ladezustand — dieselbe Rolle wie in jedem Bild, das ihn zeigt.</summary>
        public static readonly Farbrolle RolleSoC = Farbrolle.SPEICHERFUELLSTAND;

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
            Zeichnung.Zeichenmodell modell = Modell(r, mitSoC);
            return modell == null ? null : Zeichnung.SkiaMaler.Png(modell);
        }

        /// <summary>
        /// <b>DASSELBE BILD ALS ZEICHENMODELL</b> (Etappe DG-E3, Gruppen (b)/(c),
        /// Oberfläche) — der Zwilling von <see cref="Lastgang"/>.
        ///
        /// <para>Gleiche Reihenfolge, gleiche Reihen, gleiche Achsen: Beide Wege gehen
        /// durch <c>ErzeugerStapelModell</c>, der PNG-Weg malt das Ergebnis nur noch.
        /// Die Oberfläche gibt das Modell an <c>DiagrammSvg</c> und bekommt damit Zoom,
        /// Legende und Zeigerzeile, ohne dass ein Bild neu gerechnet wird.</para>
        /// </summary>
        /// <param name="r">Das Ergebnis des Laufs; <c>null</c> liefert <c>null</c>.</param>
        /// <param name="mitSoC">Den Ladezustand auf der Sekundaerachse mitzeichnen.</param>
        public static Zeichnung.Zeichenmodell Modell(PeakShavingErgebnis r, bool mitSoC)
        {
            if (r == null) return null;

            List<ChartRenderer.Reihe> linien = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(MyResource.Resource.PEAK_SERIE_ALT, r.PAltKw, RolleAlt),
                new ChartRenderer.Reihe(MyResource.Resource.PEAK_SERIE_NEU, r.PNeuKw, RolleNeu)
            };

            // #234: Die zweite Achse nimmt seither eine LISTE. Hier steht genau eine
            // Reihe darauf; die Achse traegt damit unveraendert deren Farbe.
            List<ChartRenderer.Reihe> soc = mitSoC
                ? new List<ChartRenderer.Reihe>
                  { new ChartRenderer.Reihe(MyResource.Resource.PEAK_SERIE_SOC, r.SoCKwh, RolleSoC) }
                : null;

            return ChartRenderer.ErzeugerStapelModell(
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
