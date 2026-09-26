using System;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Wert eines Diagrammplatzhalters</b> (Konzept Berichtsvorlagen 4.6 BV-P5, 5.4, 6.5; Etappe BV-E5) —
    /// was die Quelle eines Bildschlüssels (<c>bild.*</c>, <c>stand.bild.*</c>, <c>stamm.bild.*</c>) liefert:
    /// nicht das fertige Bild, sondern wie es entsteht. Die Engine kennt erst am Rahmen des Platzhalterbildes
    /// das Zielmaß und ruft dann <see cref="Baue"/> (Stufe 2: gezeichnet in der Zielgröße).
    ///
    /// <para><b>Maße.</b> <see cref="Faktor"/> sind die Bildpunkte des Modells je Anzeigepunkt (96 dpi) — 2 bei
    /// den breiten Bildern (1240 → 620), 960/420 beim Deckungskuchen: So bleibt die Schrift im Bericht so groß
    /// wie im Bausteinweg. Unter <see cref="Mindestbreite"/> (Bildpunkte des Modells) zeichnet der Renderer
    /// nicht; ein schmalerer Rahmen bekommt das Bild in der Mindestbreite, verkleinert (Stufe 1).</para>
    /// </summary>
    public sealed class Diagrammbild
    {
        private readonly Func<Bildmass?, Zeichenmodell> _baue;

        /// <summary>Legt den Wert an.</summary>
        /// <param name="baue">Das Modell im Zielmaß (<c>null</c> = Vorgabe); <c>null</c> = kein Bild.</param>
        /// <param name="faktor">Bildpunkte des Modells je Anzeigepunkt.</param>
        /// <param name="mindestbreite">Die kleinste Breite, in der das Bild gezeichnet wird [Modell].</param>
        /// <param name="grundOhneModell">Der Grund, wenn <paramref name="baue"/> kein Modell liefert.</param>
        public Diagrammbild(Func<Bildmass?, Zeichenmodell> baue, double faktor, int mindestbreite, string grundOhneModell)
        {
            _baue = baue ?? throw new ArgumentNullException(nameof(baue));
            Faktor = faktor > 0 ? faktor : 2.0;
            Mindestbreite = Math.Max(Bildmass.MIN_BREITE, mindestbreite);
            GrundOhneModell = grundOhneModell;
        }

        /// <summary>Bildpunkte des Modells je Anzeigepunkt.</summary>
        public double Faktor { get; }

        /// <summary>Die kleinste Breite, in der das Bild gezeichnet wird [Bildpunkte des Modells].</summary>
        public int Mindestbreite { get; }

        /// <summary>Warum es kein Bild gibt, wenn <see cref="Baue"/> <c>null</c> liefert (Sprache des Berichts).</summary>
        public string GrundOhneModell { get; }

        /// <summary>Das Modell im Zielmaß; <c>null</c> = der Lauf führt die Werte dieses Bildes nicht.</summary>
        public Zeichenmodell Baue(Bildmass? mass) { return _baue(mass); }

        /// <summary>
        /// Das Zielmaß zu einem Rahmen in EMU (Konzept 6.5): Breite und Höhe mal <see cref="Faktor"/>; ein Maß
        /// von 0 bleibt 0 (Vorgabe des Bildes). Ohne Rahmen <c>null</c>.
        /// </summary>
        public Bildmass? MassFuer(long breiteEmu, long hoeheEmu)
        {
            if (breiteEmu <= 0 && hoeheEmu <= 0) return null;
            int b = breiteEmu <= 0 ? 0 : (int)Math.Round(breiteEmu / (double)Wordbilder.EMU_JE_PIXEL * Faktor, MidpointRounding.AwayFromZero);
            int h = hoeheEmu <= 0 ? 0 : (int)Math.Round(hoeheEmu / (double)Wordbilder.EMU_JE_PIXEL * Faktor, MidpointRounding.AwayFromZero);
            return new Bildmass(b, h);
        }
    }
}
