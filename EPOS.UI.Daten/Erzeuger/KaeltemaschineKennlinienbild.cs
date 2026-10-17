using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kennlinienbilder der Kältemaschine im Stammblatt</b> (KD-3) — EER und Kälteleistung über der
    /// Rückkühltemperatur, eine Linie je Kaltwassertemperatur, nach dem Muster der Wärmepumpe (COP und Leistung über
    /// der Außentemperatur, eine Linie je Vorlauf).
    /// </summary>
    /// <remarks>
    /// <para><b>Derselbe Renderer wie die Wärmepumpe</b> (<see cref="ChartRenderer.KennlinienModell"/>): Er zeichnet
    /// Reihen über x mit einer Linie je ganzzahligem Scharparameter und beschriftet die Legende mit „… °C" — das passt
    /// auf die Kaltwassertemperatur der Stützstellen. Ein neues Bild im Kern ist deshalb nicht nötig, die ChartProben
    /// bleiben unberührt.</para>
    /// <para><b>Was gezeichnet wird:</b> nur Punkte mit beiden Temperaturen und dem jeweiligen Wert (EER bzw.
    /// Kälteleistung) — ein halb eingegebener Punkt des Arbeitsstands fällt weg, statt das Bild zu verwerfen. Die
    /// Kaltwassertemperatur wird für die Schar auf ganze Grad gerundet (der Katalog führt ganze Grad); innerhalb einer
    /// Linie stehen die Punkte nach der Rückkühltemperatur sortiert. Bei Luftkühlung ist die Rückkühltemperatur die
    /// Außentemperatur, und die Achse heißt so.</para>
    /// </remarks>
    public static class KaeltemaschineKennlinienbild
    {
        /// <summary>Die zwei Bilder zum Arbeitsstand <paramref name="d"/>; ohne Satz <see cref="KaeltemaschineKennlinienbilder.Leer"/>.</summary>
        public static KaeltemaschineKennlinienbilder Modelle(KaeltemaschineDaten d)
        {
            if (d == null) return KaeltemaschineKennlinienbilder.Leer;

            bool luft = d.RueckkuehlartIndex is int platz && platz >= 0 && platz < KaeltemaschineSchema.RUECKKUEHLARTEN.Count
                        && KaeltemaschineSchema.RUECKKUEHLARTEN[platz] == KaeltemaschineSchema.RUECKKUEHLART_LUFT;
            string x = luft ? MyResource.Resource.KM_KL_ACHSE_AUSSEN : MyResource.Resource.KM_KL_ACHSE_RUECKKUEHL;

            IReadOnlyList<ChartRenderer.KennlinienReihe> eer = Reihen(d, p => p.Eer);
            IReadOnlyList<ChartRenderer.KennlinienReihe> leistung = Reihen(d, p => p.Kaelteleistung);

            return new KaeltemaschineKennlinienbilder(
                eer.Count == 0 ? null : ChartRenderer.KennlinienModell(
                    MyResource.Resource.KM_KL_TITEL_EER, MyResource.Resource.KM_KL_ACHSE_EER, x, eer,
                    ChartRenderer.Kennlinienmarke.Kreis),
                leistung.Count == 0 ? null : ChartRenderer.KennlinienModell(
                    MyResource.Resource.KM_KL_TITEL_LEISTUNG, MyResource.Resource.KM_KL_ACHSE_LEISTUNG, x, leistung,
                    ChartRenderer.Kennlinienmarke.Kreuz));
        }

        /// <summary>
        /// Die Reihen eines Werts: je (gerundeter) Kaltwassertemperatur eine Linie, aufsteigend; die Punkte nach der
        /// Rückkühltemperatur. Nur endliche, vollständige Punkte.
        /// </summary>
        public static IReadOnlyList<ChartRenderer.KennlinienReihe> Reihen(KaeltemaschineDaten d,
                                                                         Func<KaeltemaschinePunktDaten, double?> wert)
        {
            if (d?.Kennlinie == null) return Array.Empty<ChartRenderer.KennlinienReihe>();
            return d.Kennlinie
                .Where(p => p != null && Endlich(p.Rueckkuehltemperatur) && Endlich(p.Kaltwassertemperatur) && Endlich(wert(p)))
                .GroupBy(p => (int)Math.Round(p.Kaltwassertemperatur!.Value, MidpointRounding.AwayFromZero))
                .OrderBy(g => g.Key)
                .Select(g => new ChartRenderer.KennlinienReihe(g.Key,
                    g.OrderBy(p => p.Rueckkuehltemperatur!.Value)
                     .Select(p => (p.Rueckkuehltemperatur!.Value, wert(p)!.Value))
                     .ToList()))
                .ToList();
        }

        private static bool Endlich(double? v) => v.HasValue && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value);
    }
}
