using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Zielmaß eines Diagramms</b> — Stufe 2 der Bildgröße (Konzept Berichtsvorlagen 6.5, Etappe
    /// BV-E5): Der <see cref="ChartRenderer"/> zeichnet ein Bild in diesem Maß (Bildpunkte des Modells),
    /// statt das Bild in seinem festen Maß zu zeichnen und danach zu skalieren. So bleibt die Schrift in
    /// einem schmalen Rahmen so groß wie im Bericht, und zwei Bilder nebeneinander bleiben lesbar.
    ///
    /// <para><b>Die Vorgabe lässt das Bild byte-gleich</b> (<c>EPOS.Kern/CLAUDE.md</c>, „Bericht“): Wer
    /// kein Maß übergibt (<c>null</c>), bekommt das Bild wie bisher. Ein Maß von 0 in einer Richtung heißt
    /// „die Vorgabe des Bildes“ in dieser Richtung — Bilder, deren Höhe den Daten folgt (Balken je Stand,
    /// Spannenbild), nehmen nur die Breite.</para>
    ///
    /// <para><b>Grenzen.</b> Unter <see cref="MIN_BREITE"/> × <see cref="MIN_HOEHE"/> zeichnet der Renderer in
    /// der Grenze — dann greift Stufe 1 (das fertige Bild wird verkleinert, die Schrift schrumpft mit), und
    /// der Vorlagenprüfer warnt, sobald der Rahmen unter 80 % der Modellbreite liegt. Über
    /// <see cref="MAX"/> wird ebenfalls begrenzt.</para>
    /// </summary>
    public readonly record struct Bildmass(int Breite, int Hoehe)
    {
        /// <summary>Die kleinste Breite, in der ein Bild neu gezeichnet wird [Bildpunkte des Modells].</summary>
        public const int MIN_BREITE = 560;

        /// <summary>Die kleinste Höhe, in der ein Bild neu gezeichnet wird [Bildpunkte des Modells].</summary>
        public const int MIN_HOEHE = 280;

        /// <summary>Die größte Kante [Bildpunkte des Modells].</summary>
        public const int MAX = 4000;

        /// <summary>Die Breite des Bildes: die Vorgabe ohne Maß oder bei 0, sonst das Maß in den Grenzen.</summary>
        public static int BreiteOder(Bildmass? mass, int vorgabe)
        {
            if (!mass.HasValue || mass.Value.Breite <= 0) return vorgabe;
            return Math.Max(MIN_BREITE, Math.Min(MAX, mass.Value.Breite));
        }

        /// <summary>Die Höhe des Bildes: die Vorgabe ohne Maß oder bei 0, sonst das Maß in den Grenzen.</summary>
        public static int HoeheOder(Bildmass? mass, int vorgabe)
        {
            if (!mass.HasValue || mass.Value.Hoehe <= 0) return vorgabe;
            return Math.Max(MIN_HOEHE, Math.Min(MAX, mass.Value.Hoehe));
        }

        /// <summary>Greift Stufe 2 in dieser Breite — liegt sie nicht unter <see cref="MIN_BREITE"/>?</summary>
        public static bool GreiftBreite(int breite) { return breite >= MIN_BREITE; }
    }
}
