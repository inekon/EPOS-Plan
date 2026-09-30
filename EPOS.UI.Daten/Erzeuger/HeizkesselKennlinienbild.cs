using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die kleine Kurve des Kesseleditors</b> (Konzept Kesselkennlinie 5, erster Punkt) — plattformfrei, damit die
    /// Windows-Hülle (<c>HeizkesselHuelle</c>) sie nur hereinreicht und die Tests sie ohne Schale messen.
    /// </summary>
    /// <remarks>
    /// <para><b>Gerechnet wird im Kern, aus DERSELBEN Funktion wie der Lauf</b> (<see cref="Kesselkennlinie.Kurven"/>):
    /// Nennwirkungsgrad nach Energieträger, η₃₀ gepflegt oder als Normvorgabe nach Bauart, Brennwertkennlinie je Rücklauf.
    /// Gezeichnet wird mit <see cref="ChartRenderer.KesselkennlinieModell"/>; die Oberfläche zeigt das Modell über
    /// <c>DiagrammSvg</c>.</para>
    /// <para><b>Der Arbeitsstand, nicht der gespeicherte Satz:</b> Die Felder kommen so, wie der Dialog sie gerade führt —
    /// leer heißt „Vorgabe“ wie im Lauf. Ohne gewählten Energieträger gilt die 1, wie beim Speichern
    /// (<c>HeizkesselHuelle.NachModell</c>); der Schalter Brennwertkennlinie nur beim Brennwertkessel, wie der Controller
    /// ihn schreibt.</para>
    /// </remarks>
    public static class HeizkesselKennlinienbild
    {
        /// <summary>Der Energieträger, wenn der Satz keinen gewählt hat — wie beim Speichern.</summary>
        public const int BRENNSTOFF_OHNE_WAHL = 1;

        /// <summary>Das Bild zum Arbeitsstand <paramref name="d"/>; <c>null</c> ohne Satz.</summary>
        public static Zeichenmodell Modell(HeizkesselKatalogDaten d)
        {
            if (d == null) return null;
            int brennstoff = d.Brennstoff.HasValue && d.Brennstoff.Value >= 1 ? d.Brennstoff.Value : BRENNSTOFF_OHNE_WAHL;
            IReadOnlyList<Kesselkurve> kurven = Kesselkennlinie.Kurven(
                d.Wirkungsgrad_Gas ?? 0, d.Wirkungsgrad_Oel ?? 0, brennstoff, d.Brennwert, d.Beschreibung,
                d.Wirkungsgrad_Teillast30, d.Kennlinie_Brennwert && d.Brennwert);

            return ChartRenderer.KesselkennlinieModell(
                MyResource.Resource.HZKK_BILD_KENNLINIE, MyResource.Resource.HZKK_BILD_ACHSE_LAST,
                MyResource.Resource.HZKK_BILD_ACHSE_WIRKUNGSGRAD,
                kurven.Select(k => new ChartRenderer.KesselkennlinienReihe(Name(k), k.Punkte)).ToList());
        }

        /// <summary>Der Legendenname einer Kurve: „Rücklauf 30 °C“ bzw. „Teillastkennlinie“.</summary>
        public static string Name(Kesselkurve k)
            => k.RuecklaufC.HasValue
                ? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.HZKK_BILD_RUECKLAUF,
                                k.RuecklaufC.Value.ToString("0", CultureInfo.CurrentCulture))
                : MyResource.Resource.HZKK_BILD_TEILLAST;
    }
}
