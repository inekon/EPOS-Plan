using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Jahresband der Freigabe</b> (Entwurf AK3-K 3.3, Festlegung 7; Welle KZ) — die Datenseite des Reiters
    /// Konditionierung: je Tag, ob der Kalender eines Orts (Gebäude oder Zone) Heizen, Kühlen, beides oder keines
    /// freigibt. Dieselbe Regel wie im Lauf (<see cref="Zonenfreigabe"/>): Heizen ist frei, wo der geltende Heizkalender
    /// eine Stunde nicht „aus" führt (ohne Kalender der Bestand: jeder Tag), an einer unbeheizten Zone nie; Kühlen, wo
    /// die Kühlung am Ort wirksam ist und der geltende Kühlkalender eine Stunde nicht „aus" führt. Der Projektschalter
    /// „Kühlung rechnen" steht nicht im Arbeitsstand — das Band zeigt die Freigabe des Gebäudes bzw. der Zone. Rein,
    /// ohne Datenbank.
    /// </summary>
    public static class Konditionierungsfreigabe
    {
        /// <summary>Die Freigabe der 365 Tage am Ort <paramref name="zone"/> (<c>null</c> = das Gebäude).</summary>
        /// <exception cref="ArgumentException">Die Zone steht nicht im Arbeitsstand.</exception>
        public static Freigabeart[] Band(Konditionierungsarbeitsstand a, long? zone)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            Konditionierungszone z = null;
            if (zone.HasValue)
                z = a.Zone(zone.Value) ?? throw new ArgumentException("Die Zone steht nicht im Arbeitsstand.", nameof(zone));
            Matrixeingang bestand = z == null ? a.Gebaeude.Bestand : a.AufgeloesterBestand(z);

            double[] heiz = a.GeltenderKalender(Konditionierungsgroesse.Heizsoll, zone)?.Auswerten(a.W0, a.Referenzjahr);
            if (z != null && !z.IstBeheizt)
            {
                heiz = new double[8760];
                Array.Fill(heiz, double.NaN);
            }
            double[] kuehl = bestand.KuehlungWirksam
                ? a.GeltenderKalender(Konditionierungsgroesse.Kuehlsoll, zone)?.Auswerten(a.W0, a.Referenzjahr)
                : null;
            return Zonenfreigabe.Jahr(heiz, kuehl, bestand.KuehlungWirksam);
        }
    }
}
