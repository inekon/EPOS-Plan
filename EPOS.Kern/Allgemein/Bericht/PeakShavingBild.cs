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
            => Modell(r, mitSoC, false, false);

        /// <summary>Ladeleistung — dieselbe Rolle wie die Speicherladung in jedem Bild.</summary>
        public static readonly Farbrolle RolleLadung = Farbrolle.SPEICHERLADUNG;

        /// <summary>Entladeleistung — die Rolle des Speicherstroms.</summary>
        public static readonly Farbrolle RolleEntladung = Farbrolle.STROM_SPEICHER;

        /// <summary>Die erreichte Schwelle als waagerechte Linie.</summary>
        public static readonly Farbrolle RolleSchwelle = Farbrolle.REST;

        /// <summary>
        /// Welle RZ: dasselbe Bild mit zuschaltbaren Reihen — Lade- und Entladeleistung [kW] und die erreichte
        /// Schwelle (<see cref="PeakShavingErgebnis.ErreichteSchwelleKw"/>) als Linien hinter den beiden
        /// Lastgängen. Beide ab Werk aus; ohne sie ist das Modell dasselbe wie <see cref="Modell(PeakShavingErgebnis, bool)"/>.
        /// </summary>
        /// <param name="r">Das Ergebnis des Laufs; <c>null</c> liefert <c>null</c>.</param>
        /// <param name="mitSoC">Den Ladezustand auf der Sekundaerachse mitzeichnen.</param>
        /// <param name="mitLeistung">Lade- und Entladeleistung mitzeichnen.</param>
        /// <param name="mitSchwelle">Die erreichte Schwelle mitzeichnen.</param>
        public static Zeichnung.Zeichenmodell Modell(PeakShavingErgebnis r, bool mitSoC, bool mitLeistung,
                                                     bool mitSchwelle)
        {
            if (r == null) return null;

            List<ChartRenderer.Reihe> linien = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(MyResource.Resource.PEAK_SERIE_ALT, r.PAltKw, RolleAlt),
                new ChartRenderer.Reihe(MyResource.Resource.PEAK_SERIE_NEU, r.PNeuKw, RolleNeu)
            };
            if (mitLeistung)
            {
                linien.Add(new ChartRenderer.Reihe(MyResource.Resource.PEAK_SERIE_LADELEISTUNG, LadeleistungKw(r),
                                                   RolleLadung));
                linien.Add(new ChartRenderer.Reihe(MyResource.Resource.PEAK_SERIE_ENTLADELEISTUNG,
                                                   EntladeleistungKw(r), RolleEntladung));
            }
            if (mitSchwelle)
                linien.Add(new ChartRenderer.Reihe(MyResource.Resource.PEAK_SERIE_SCHWELLE, SchwelleKw(r),
                                                   RolleSchwelle));

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

        /// <summary>
        /// Die Intervalldauer [h] der Reihe: 35 040 Werte sind Viertelstunden, 8 760 Stunden — allgemein
        /// 8 760 / Länge, dieselbe Regel wie die Achse.
        /// </summary>
        private static double IntervallStunden(int laenge) => laenge > 0 ? 8760.0 / laenge : 1.0;

        /// <summary>Die AC-Ladeleistung je Intervall [kW]: geladene Energie durch Intervalldauer.</summary>
        public static double[] LadeleistungKw(PeakShavingErgebnis r) => Leistung(r?.LadungAcKwh);

        /// <summary>Die AC-Entladeleistung je Intervall [kW]: entladene Energie durch Intervalldauer.</summary>
        public static double[] EntladeleistungKw(PeakShavingErgebnis r) => Leistung(r?.EntladungAcKwh);

        /// <summary>Die erreichte Schwelle als Reihe gleicher Länge wie der Lastgang [kW].</summary>
        public static double[] SchwelleKw(PeakShavingErgebnis r)
        {
            if (r == null) return new double[0];
            var s = new double[r.PAltKw?.Length ?? 0];
            for (int i = 0; i < s.Length; i++) s[i] = r.ErreichteSchwelleKw;
            return s;
        }

        private static double[] Leistung(double[] energieKwh)
        {
            if (energieKwh == null) return new double[0];
            double dt = IntervallStunden(energieKwh.Length);
            var p = new double[energieKwh.Length];
            for (int i = 0; i < p.Length; i++) p[i] = energieKwh[i] / dt;
            return p;
        }
    }
}
