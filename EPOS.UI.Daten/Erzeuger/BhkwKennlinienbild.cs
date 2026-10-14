using System.Collections.Generic;
using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die kleine Kurve des BHKW-Editors</b> (Welle M4, BH1) — elektrischer und thermischer Wirkungsgrad über
    /// der elektrischen Last, plattformfrei, damit die Windows-Hülle (<c>BhkwHuelle</c>) sie nur hereinreicht und
    /// die Tests sie ohne Schale messen.
    /// </summary>
    /// <remarks>
    /// <para><b>Gerechnet wird im Kern, aus DERSELBEN Kennlinie wie der Lauf</b> (<see cref="BhkwTeillast"/>):
    /// die Volllastwerte aus Gesamtwirkungsgrad und Leistungsverhältnis, die Teillastwerte bei 50 % Last, leer
    /// wie Volllast. Gezeichnet wird mit <see cref="ChartRenderer.KesselkennlinieModell"/> — dasselbe Bild
    /// „Wirkungsgrad über der Last“ wie beim Heizkessel, mit zwei Linien.</para>
    /// <para><b>Der Arbeitsstand, nicht der gespeicherte Satz:</b> Der Gesamtwirkungsgrad ist die Summe der zwei
    /// gepflegten Anteile, ohne Anteile der geladene Gesamtwert — wie beim Speichern.</para>
    /// </remarks>
    public static class BhkwKennlinienbild
    {
        /// <summary>Die Laststufen der Kurve: 50 % bis 100 % in Zehnerschritten.</summary>
        public static readonly IReadOnlyList<double> LASTSTUFEN = new[] { 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 };

        /// <summary>Das Bild zum Arbeitsstand <paramref name="d"/>; <c>null</c> ohne Satz.</summary>
        public static Zeichenmodell Modell(BhkwKatalogDaten d)
        {
            if (d == null) return null;
            double pel = d.Pel ?? 0, pth = d.Ptherm ?? 0;
            double eta = BhkwWirkungsgrad.Gesamt(d.WirkungsgradEl, d.WirkungsgradTh) ?? d.Wirkungsgrad ?? 0;
            var t = new BhkwTeillast(pel, pth, eta, d.WirkungsgradEl50, d.WirkungsgradTh50, 0.5, null, null);

            var el = new List<Kesselkurvenpunkt>();
            var th = new List<Kesselkurvenpunkt>();
            if (pel > 0 && pth > 0 && eta > 0)
                foreach (double beta in LASTSTUFEN)
                {
                    el.Add(new Kesselkurvenpunkt(beta, t.EtaEl(beta)));
                    th.Add(new Kesselkurvenpunkt(beta, t.EtaTh(beta)));
                }

            return ChartRenderer.KesselkennlinieModell(
                MyResource.Resource.BHKWK_BILD_KENNLINIE, MyResource.Resource.BHKWK_BILD_ACHSE_LAST,
                MyResource.Resource.HZKK_BILD_ACHSE_WIRKUNGSGRAD,
                new[]
                {
                    new ChartRenderer.KesselkennlinienReihe(MyResource.Resource.BHKWK_BILD_ELEKTRISCH, el),
                    new ChartRenderer.KesselkennlinienReihe(MyResource.Resource.BHKWK_BILD_THERMISCH, th)
                });
        }
    }
}
