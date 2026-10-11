using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>DAS BIVALENZDIAGRAMM</b> (Fachkonzept Übergabegrenze 7.2, Umsetzungskonzept UB‑E4‑a):
    /// <c>ChartRenderer.Bivalenzdiagramm</c> — Leistung über der Außentemperatur mit Bereichsflächen, Heizlast, Kennfeld
    /// und Übergabegrenze bei 55 °C, der Wärmepumpe als Fläche und den Marken θ_biv,1, θ_biv,2, Abschaltpunkt und „nach
    /// Kennfeld“.
    ///
    /// <para><b>Das Zahlenbeispiel des Fachkonzepts</b> (FK 4.5), fest verdrahtet, ohne Datenbank: Gebäude 10 kW bei
    /// −12 °C, Raum 20 °C, Heizkörper 75/60 (n = 1,3), Höchstvorlauf 55 °C, Kennfeld 7 kW bei −7 °C bis 9 kW bei +7 °C,
    /// σ_min 3 K, teilparallel mit Vorwärmbetrieb, Abschaltpunkt −10 °C, Kessel 10 kW. Herleitung und Modell rechnet der
    /// Kern (<c>Bivalenzherleitung.Rechnen</c>, <c>BivalenzdiagrammModell.Aus</c>); die Stundenpunkte sind eine
    /// synthetische Reihe aus dem Betrieb des Diagramms.</para>
    /// </summary>
    internal static partial class Program
    {
        private static BivalenzGeraetedaten BivalenzGeraet(bool vorwaermbetrieb, double kennfeldKalt)
            => new BivalenzGeraetedaten
            {
                HoechstvorlaufC = 55.0,
                Kennfeld = new[] { new Kennfeldpunkt(-7.0, kennfeldKalt), new Kennfeldpunkt(7.0, 9.0) },
                SpreizungMinK = 3.0,
                Betriebsart = Bivalenzbetriebsart.Teilparallel,
                AbschaltpunktC = -10.0,
                Vorwaermbetrieb = vorwaermbetrieb,
                Kesselleistung = 10.0,
                Einbindung = "DIREKT",
            };

        private static Bivalenzherleitung BivalenzHerleitung(bool mitUebergabe, bool vorwaermbetrieb = true,
                                                             double kennfeldKalt = 7.0)
            => Bivalenzherleitung.Rechnen(new BivalenzGebaeudedaten
            {
                Gebaeude = mitUebergabe ? new Uebergabezone(10.0, 75.0, 60.0, 20.0, 1.3) : null,
                HeizlastN = 10.0,
                AuslegungAussenC = -12.0,
                AuslegungRaumC = 20.0,
            }, BivalenzGeraet(vorwaermbetrieb, kennfeldKalt));

        /// <summary>Synthetische Stundenpunkte: 400 Stunden über −12 … +19 °C, Leistung des Diagrammbetriebs mit Streuung ±10 %.</summary>
        private static List<BivalenzStundenpunkt> BivalenzStunden(Bivalenzherleitung h)
        {
            var punkte = new List<BivalenzStundenpunkt>();
            for (int i = 0; i < 400; i++)
            {
                double ta = -12.0 + (i * 37 % 311) / 10.0;
                double streuung = 1.0 + 0.1 * Math.Sin(i * 0.7);
                punkte.Add(new BivalenzStundenpunkt(ta, h.Betrieb(ta).Waermepumpe * streuung));
            }
            return punkte;
        }

        private static byte[] Bivalenzbild(bool vorwaermbetrieb = true, double kennfeldKalt = 7.0, bool stunden = false)
        {
            Bivalenzherleitung h = BivalenzHerleitung(true, vorwaermbetrieb, kennfeldKalt);
            return ChartRenderer.Bivalenzdiagramm(BivalenzdiagrammModell.Aus(h, stunden ? BivalenzStunden(h) : null));
        }

        private static byte[] BivalenzPlatzhalterbild()
            => ChartRenderer.Bivalenzdiagramm(BivalenzdiagrammModell.Aus(BivalenzHerleitung(false)));

        private static void BivalenzdiagrammProben(string ziel)
        {
            int b = ChartRenderer.BIVALENZ_BREITE, h = ChartRenderer.BIVALENZ_HOEHE;

            // Maßproben: Heizlast, Kennfeld und Übergabegrenze in ihren Rollen; mit Stundenpunkten dazu SERIE_5.
            Pruefe(ziel, "bivalenzdiagramm", b, h,
                   new[] { Rollenfarbe(Farbrolle.BEDARF), Rollenfarbe(Farbrolle.SERIE_3), Rollenfarbe(Farbrolle.SERIE_8) },
                   () => Bivalenzbild());
            Pruefe(ziel, "bivalenzdiagramm_stunden", b, h,
                   new[] { Rollenfarbe(Farbrolle.BEDARF), Rollenfarbe(Farbrolle.SERIE_5) },
                   () => Bivalenzbild(stunden: true));
            Pruefe(ziel, "bivalenzdiagramm_platzhalter", b, h,
                   new[] { Rollenfarbe(Farbrolle.ACHSE) },
                   BivalenzPlatzhalterbild);

            // Gegenproben: ein anderes Kennfeld, der Schalter Vorwärmbetrieb, die Stundenpunkte und der Platzhalter
            // kommen im Bild an.
            Unterschiedlich("bivalenz_kennfeld_wirkt", () => Bivalenzbild(), () => Bivalenzbild(kennfeldKalt: 6.0));
            Unterschiedlich("bivalenz_vorwaermung_wirkt", () => Bivalenzbild(), () => Bivalenzbild(vorwaermbetrieb: false));
            Unterschiedlich("bivalenz_stunden_wirkt", () => Bivalenzbild(), () => Bivalenzbild(stunden: true));
            Unterschiedlich("bivalenz_platzhalter_wirkt", () => Bivalenzbild(), BivalenzPlatzhalterbild);
        }
    }
}
