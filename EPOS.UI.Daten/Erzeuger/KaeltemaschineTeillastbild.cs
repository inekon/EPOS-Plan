using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die kleine Kurve der Gruppe „Teillast und Takten" im Katalogdialog der Kältemaschine</b> (Fachkonzept Teillast
    /// und Takten 7.1): das EER-Verhältnis g über dem Lastgrad, plattformfrei wie <see cref="BhkwKennlinienbild"/>.
    /// </summary>
    /// <remarks>
    /// Gerechnet wird im Kern aus DERSELBEN wirksamen Kurve wie der Lauf (<see cref="KaeltemaschineTeillastDialogrechnung.Lesezeile"/>,
    /// also mit Vorgabekurve und Rückfall auf linear); gezeichnet mit <see cref="ChartRenderer.KesselkennlinieModell"/> —
    /// dasselbe Bild „Wert über der Last" wie bei Heizkessel und BHKW, mit einer Linie.
    /// </remarks>
    public static class KaeltemaschineTeillastbild
    {
        /// <summary>Das Bild zum Arbeitsstand <paramref name="m"/>; <c>null</c> ohne Satz.</summary>
        public static Zeichenmodell Modell(KaeltemaschineModel m)
        {
            if (m == null) return null;
            KaeltemaschineTeillastDialogrechnung.Lesestand l = KaeltemaschineTeillastDialogrechnung.Lesezeile(m);
            List<Kesselkurvenpunkt> punkte = l.Kurve.Select(p => new Kesselkurvenpunkt(p.Lastgrad, p.G)).ToList();
            return ChartRenderer.KesselkennlinieModell(
                MyResource.Resource.KM_BILD_TEILLAST, MyResource.Resource.KM_BILD_ACHSE_LASTGRAD,
                MyResource.Resource.KM_BILD_ACHSE_G,
                new[] { new ChartRenderer.KesselkennlinienReihe(MyResource.Resource.KM_BILD_ACHSE_G, punkte) });
        }
    }
}
