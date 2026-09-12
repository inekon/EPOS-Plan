using System;
using System.Collections.Generic;
using SkiaSharp;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das EINE Bild „Lastgang und Speicherbetrieb" aus einem gerechneten Speicherlauf
    /// — Netzbezug ohne und mit Speicher, die Speicherleistung mit Vorzeichen und der
    /// LADEZUSTAND auf einer zweiten Achse (Anwenderwunsch W11b‑B‑26, 10.09.2026).
    ///
    /// <para><b>Warum EIN Bild.</b> Der Anwender hat es zweimal verlangt — für den
    /// Optimierungsdialog („Lastgang und Speicherung in einer Grafik", 09.09.2026,
    /// W11b‑B‑25) und jetzt für den Stromspeicher-Reiter der Ergebnisseite: „Der
    /// Lastgang und die Kappung durch den Stromspeicher sowie der Ladezustand des
    /// Stromspeichers sollen in einer Grafik sichtbar sein." Zwei Bilder untereinander
    /// beantworten die eigentliche Frage nicht: Um wie viel senkt der Speicher den
    /// Netzbezug, wann tut er es, und wie voll ist er dabei? Das steht erst da, wo alle
    /// Kurven über DERSELBEN Zeitachse liegen.</para>
    ///
    /// <para><b>Warum diese Klasse neben <see cref="SpeicherOptimierungCtrl"/> steht.</b>
    /// Dort entsteht dasselbe Bild für den BESTPUNKT einer Rastersuche — aus einem
    /// eigens nachgerechneten Lauf, weil ein <c>OptimiererPunkt</c> bewusst keine
    /// Zeitreihen hält. Hier gibt es den Lauf schon: Er steht als
    /// <see cref="SpeicherErgebnis"/> in der Simulation. Gemeinsam ist beiden die
    /// RECHNUNG — Vorzeichen der Speicherleistung, Residuallast, Netzbezug —, und die
    /// steht deshalb genau einmal, nämlich hier (<see cref="LeistungKw"/>,
    /// <see cref="Netzbezug"/>); der Optimierungscontroller ruft sie.</para>
    ///
    /// <para><b>Der Ergebnisreiter kennt keine Schwelle.</b> Die Reihe
    /// <see cref="REIHE_SCHWELLE"/> gehört zur Lastspitzenkappung, und die ist eine
    /// Berechnungsart der AUSLEGUNGSOPTIMIERUNG (W11b‑E‑3) bzw. die eigene Maske
    /// <c>PeakShaving</c> — der Simulationslauf fährt Dauernutzung, Nachtnutzung oder
    /// Preissteuerung (<c>StromspeicherSimCtrl.BaueStrategie</c>). <see cref="Zeichnen"/>
    /// nimmt die Schwelle trotzdem entgegen: Eine Berechnungsart mit Kappung hätte sonst
    /// im Reiter kein Bild, sondern eine Ausnahme von der Regel.</para>
    /// </summary>
    public static class SpeicherBetriebsbild
    {
        // Die sprachneutralen REIHENSCHLUESSEL des Bildes. Sie standen bis W11b-B-26 in
        // SpeicherOptimierungCtrl; dort ruft sie jetzt der Dialog weiter ab
        // (SpeicherOptimierungCtrl.REIHE_* verweisen hierher), denn das Vokabular
        // gehört zum BILD und nicht zu einem seiner beiden Aufrufer.

        /// <summary>Reihenschlüssel: Netzbezug ohne Speicher.</summary>
        public const string REIHE_OHNE = "OHNE_SPEICHER";

        /// <summary>Reihenschlüssel: Netzbezug mit Speicher.</summary>
        public const string REIHE_MIT = "MIT_SPEICHER";

        /// <summary>Reihenschlüssel: die erreichte Kappungsschwelle (nur Lastspitzenkappung).</summary>
        public const string REIHE_SCHWELLE = "SCHWELLE";

        /// <summary>Reihenschlüssel: Speicherleistung, Entladen positiv.</summary>
        public const string REIHE_SPEICHER = "SPEICHERLEISTUNG";

        /// <summary>
        /// Reihenschlüssel: der Ladezustand [kWh] auf der ZWEITEN Achse (W11b‑B‑26).
        /// </summary>
        public const string REIHE_SOC = "LADEZUSTAND";

        /// <summary>
        /// Die Farbe des Ladezustands — woertlich die des Vorlaeufers
        /// (<c>Color.FromArgb(120, 130, 140)</c>), dieselbe, die das SoC-Bild des
        /// Reiters trug und die <see cref="PeakShavingBild.FarbeSoC"/> führt.
        /// </summary>
        public static readonly SKColor FarbeSoC = PeakShavingBild.FarbeSoC;

        // =================================================================
        // Die Rechnung (eine Stelle für beide Aufrufer)
        // =================================================================

        /// <summary>
        /// Speicherleistung je Intervall [kW]: <b>Entladen positiv, Laden negativ</b>.
        /// </summary>
        /// <remarks>
        /// Das Vorzeichen folgt der WIRKUNG auf den Netzbezug: Entladen senkt ihn, Laden
        /// hebt ihn. Damit liegt die Kurve genau dort, wo die beiden Netzbezugskurven
        /// auseinanderlaufen — und um die Nulllinie, statt eine zweite Achse zu
        /// brauchen.
        /// </remarks>
        /// <param name="ergebnis">Der gerechnete Lauf; <c>null</c> liefert Nullen.</param>
        /// <param name="dtH">Intervalllänge [h] — aus kWh je Intervall wird damit kW.</param>
        /// <param name="laenge">Länge der Zielreihe; 0 = die Länge des Ergebnisses.</param>
        public static double[] LeistungKw(SpeicherErgebnis ergebnis, double dtH, int laenge = 0)
        {
            if (laenge <= 0) laenge = ergebnis == null ? 0 : ergebnis.SoCKwh.Length;
            double[] kw = new double[laenge];
            if (ergebnis == null || dtH <= 0.0) return kw;

            double[] laden = ergebnis.LadungAcKwh;
            double[] entladen = ergebnis.EntladungAcKwh;
            for (int i = 0; i < laenge; i++)
                kw[i] = (Wert(entladen, i) - Wert(laden, i)) / dtH;

            return kw;
        }

        /// <summary>
        /// Netzbezug OHNE und MIT Speicher [kW] je Intervall.
        /// </summary>
        /// <remarks>
        /// „Ohne Speicher" ist die RESIDUALLAST — Last abzüglich der Erzeugung, die
        /// ohnehin da wäre. Bewusst NICHT bei 0 gekappt: Ein Überschuss ist eine Aussage
        /// des Bildes, keine Störung. „Mit Speicher" zieht die Speicherleistung ab —
        /// Entladen senkt den Bezug, Laden hebt ihn (siehe <see cref="LeistungKw"/>).
        /// </remarks>
        public static void Netzbezug(SpeicherEingang eingang, double[] leistungKw,
                                     out double[] ohne, out double[] mit)
        {
            int n = leistungKw == null ? 0 : leistungKw.Length;
            ohne = new double[n];
            mit = new double[n];
            if (eingang == null) return;

            for (int i = 0; i < n; i++)
            {
                double erzeugung = Wert(eingang.PvKw, i) + Wert(eingang.BhkwKw, i);
                ohne[i] = Wert(eingang.LastKw, i) - erzeugung;
                mit[i] = ohne[i] - leistungKw[i];
            }
        }

        // =================================================================
        // Die Reihen
        // =================================================================

        /// <summary>
        /// Die Reihen der LINKEN Achse [kW] in Zeichenreihenfolge.
        /// </summary>
        /// <param name="eingang">Lastgang und Erzeugung des Laufs.</param>
        /// <param name="ergebnis">Der gerechnete Lauf.</param>
        /// <param name="dtH">Intervalllänge [h].</param>
        /// <param name="wahl">Die gewählten Reihenschlüssel; <c>null</c> = alle,
        /// LEERE Liste = keine (Hausregel der Ergebnisseite,
        /// Doku_Simulationsergebnis_Darstellung.md, 5).</param>
        /// <param name="schwelleKw">Die erreichte Kappungsschwelle [kW];
        /// <c>double.NaN</c> = der Lauf kennt keine, dann entfällt die Reihe.</param>
        public static List<ChartRenderer.Reihe> Leistungsreihen(
            SpeicherEingang eingang, SpeicherErgebnis ergebnis, double dtH,
            IReadOnlyList<string> wahl, double schwelleKw = double.NaN)
        {
            var liste = new List<ChartRenderer.Reihe>();
            if (eingang == null || ergebnis == null) return liste;

            double[] leistung = LeistungKw(ergebnis, dtH, eingang.Anzahl);
            double[] ohne, mit;
            Netzbezug(eingang, leistung, out ohne, out mit);

            if (Gewaehlt(wahl, REIHE_OHNE))
                liste.Add(new ChartRenderer.Reihe(MyResource.Resource.OPT_BETRIEB_R_OHNE,
                                                  ohne, ChartRenderer.C_BEDARF));
            if (Gewaehlt(wahl, REIHE_MIT))
                liste.Add(new ChartRenderer.Reihe(MyResource.Resource.OPT_BETRIEB_R_MIT,
                                                  mit, ChartRenderer.C_NETZ));
            if (!double.IsNaN(schwelleKw) && Gewaehlt(wahl, REIHE_SCHWELLE))
                liste.Add(new ChartRenderer.Reihe(MyResource.Resource.OPT_BETRIEB_R_SCHWELLE,
                                                  Konstante(schwelleKw, ohne.Length),
                                                  ChartRenderer.C_RASTER_SCHLECHT) { Gestrichelt = true });
            if (Gewaehlt(wahl, REIHE_SPEICHER))
                liste.Add(new ChartRenderer.Reihe(MyResource.Resource.OPT_BETRIEB_R_LEISTUNG,
                                                  leistung, ChartRenderer.C_WP));
            return liste;
        }

        /// <summary>
        /// Die Reihe der RECHTEN Achse — der Ladezustand [kWh] — oder <c>null</c>, wenn
        /// der Anwender sie abgewählt hat.
        /// </summary>
        /// <remarks>
        /// Ihr NAME nennt die Einheit („Ladezustand [kWh]"), denn er steht in derselben
        /// Legende wie drei Leistungen in kW. Eine Legende, in der eine Reihe eine andere
        /// Einheit hat als ihre Nachbarn, muss das sagen — die Achsenbeschriftung allein
        /// stellt die Zuordnung nicht her.
        /// </remarks>
        public static ChartRenderer.Reihe Ladezustand(SpeicherErgebnis ergebnis,
                                                      IReadOnlyList<string> wahl)
        {
            if (ergebnis == null || !Gewaehlt(wahl, REIHE_SOC)) return null;
            return new ChartRenderer.Reihe(MyResource.Resource.PEAK_CHART_Y2,
                                           ergebnis.SoCKwh, FarbeSoC);
        }

        // =================================================================
        // Das Bild
        // =================================================================

        /// <summary>
        /// Zeichnet das Bild aus einem gerechneten Lauf. Es wirft nicht: Ohne Lauf oder
        /// ohne Eingang kommt <c>null</c> heraus, und der Reiter zeigt einfach keines.
        /// </summary>
        /// <param name="titel">Überschrift des Bildes.</param>
        /// <param name="eingang">Lastgang und Erzeugung des Laufs.</param>
        /// <param name="ergebnis">Der gerechnete Lauf (SoC, Ladung, Entladung).</param>
        /// <param name="dtH">Intervalllänge [h].</param>
        /// <param name="wahl">Die gewählten Reihenschlüssel; <c>null</c> = alle, leer = keine.</param>
        /// <param name="sortiert">Dauerlinie statt Ganglinie — jede Reihe für sich absteigend.</param>
        /// <param name="fenster">Der Datenzoom (W11b‑B‑24); <c>null</c> = das ganze Jahr.</param>
        /// <param name="schwelleKw">Kappungsschwelle [kW]; <c>double.NaN</c> = keine.</param>
        public static byte[] Zeichnen(string titel, SpeicherEingang eingang,
                                      SpeicherErgebnis ergebnis, double dtH,
                                      IReadOnlyList<string> wahl, bool sortiert,
                                      ChartRenderer.Achsenfenster fenster,
                                      double schwelleKw = double.NaN)
        {
            if (eingang == null || ergebnis == null) return null;

            return ChartRenderer.Speicherbetrieb(
                titel,
                Leistungsreihen(eingang, ergebnis, dtH, wahl, schwelleKw),
                MyResource.Resource.PEAK_CHART_Y,
                Ladezustand(ergebnis, wahl),
                MyResource.Resource.PEAK_CHART_Y2,
                sortiert,
                fenster);
        }

        // =================================================================

        /// <summary>Eine waagerechte Linie als Reihe — die Schwelle hat kein Zeitprofil.</summary>
        private static double[] Konstante(double wert, int laenge)
        {
            double[] ziel = new double[laenge];
            for (int i = 0; i < laenge; i++) ziel[i] = wert;
            return ziel;
        }

        /// <summary>Ein Wert der Reihe, oder 0, wenn es die Reihe (oder den Wert) nicht gibt.</summary>
        private static double Wert(double[] reihe, int i)
            => reihe != null && i < reihe.Length ? reihe[i] : 0.0;

        /// <summary>
        /// Ist die Reihe gewählt? <c>null</c> heißt „keine Angabe" und damit ALLE; eine
        /// LEERE Liste heißt „der Anwender hat alles abgewählt" und damit KEINE
        /// (Hausregel der Ergebnisseite — kein <c>Count == 0</c>-Rückfall).
        /// </summary>
        private static bool Gewaehlt(IReadOnlyList<string> wahl, string schluessel)
        {
            if (wahl == null) return true;
            for (int i = 0; i < wahl.Count; i++)
                if (string.Equals(wahl[i], schluessel, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
