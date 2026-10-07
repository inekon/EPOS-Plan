using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Die Freigabe der Raumkonditionierung einer Zone an einem Tag (Entwurf AK3-K 3.1, 3.3).</summary>
    public enum Freigabeart
    {
        /// <summary>Weder Heizen noch Kühlen freigegeben — keine Raumkonditionierung.</summary>
        Keine = 0,

        /// <summary>Nur Heizen freigegeben — der Tag ist ein Heiztag.</summary>
        Heizen = 1,

        /// <summary>Nur Kühlen freigegeben — der Tag ist ein Kühltag.</summary>
        Kuehlen = 2,

        /// <summary>Beides freigegeben — die Tagesart entscheidet der Bedarf des Probetags.</summary>
        Beides = 3,
    }

    /// <summary>
    /// <b>Die Kalenderfreigabe</b> (Entwurf AK3-K 3.1, 3.3; Festlegung 7): die Lesart des vorhandenen „aus" der Heiz- und
    /// Kühlsollwertreihe als Freigabe je Tag. Heizen ist an einem Tag frei, wenn der Heizsollwert an mindestens einer Stunde
    /// nicht „aus" ist (NaN); Kühlen, wenn die Kühlung wirksam ist (Kühlbetrieb des Projekts, <c>Kuehlung_Aktiv</c>,
    /// Kühlsollwert) und der Kühlsollwert an mindestens einer Stunde nicht „aus" ist (+∞ bzw. NaN). Eine Reihe
    /// <c>null</c> heißt: kein Kalender, der Bestand gibt die Seite an jedem Tag frei. Kein Schemaschritt, keine
    /// Kopierwege — dieselbe Regel für den Lauf (<see cref="GebaeudeStepper"/>) und das Jahresband im Reiter
    /// Konditionierung. Rein, ohne Datenbank.
    /// </summary>
    public static class Zonenfreigabe
    {
        /// <summary>Die Freigabe des Tags <paramref name="tag"/> (0 bis 364).</summary>
        /// <param name="heizsoll">Die Heizsollwertreihe (8 760 Stunden, NaN = „aus"); <c>null</c> = an jedem Tag frei.</param>
        /// <param name="kuehlsoll">Die Kühlsollwertreihe (+∞ oder NaN = „aus"); <c>null</c> = frei, wenn die Kühlung wirksam ist.</param>
        /// <param name="kuehlungWirksam">Ist die Kühlung wirksam? Ohne sie ist Kühlen an keinem Tag frei.</param>
        /// <param name="tag">Der Tag des Jahres, 0-basiert.</param>
        public static Freigabeart Tag(IReadOnlyList<double> heizsoll, IReadOnlyList<double> kuehlsoll, bool kuehlungWirksam, int tag)
        {
            if (tag < 0 || tag >= 365) throw new ArgumentOutOfRangeException(nameof(tag));
            bool heizen = heizsoll == null || IrgendeineStunde(heizsoll, tag);
            bool kuehlen = kuehlungWirksam && (kuehlsoll == null || IrgendeineStunde(kuehlsoll, tag));
            return (heizen ? Freigabeart.Heizen : Freigabeart.Keine) | (kuehlen ? Freigabeart.Kuehlen : Freigabeart.Keine);
        }

        /// <summary>Die Freigabe aller 365 Tage (<see cref="Tag"/>).</summary>
        public static Freigabeart[] Jahr(IReadOnlyList<double> heizsoll, IReadOnlyList<double> kuehlsoll, bool kuehlungWirksam)
        {
            var jahr = new Freigabeart[365];
            for (int d = 0; d < 365; d++) jahr[d] = Tag(heizsoll, kuehlsoll, kuehlungWirksam, d);
            return jahr;
        }

        private static bool IrgendeineStunde(IReadOnlyList<double> reihe, int tag)
        {
            if (reihe.Count < 8760) throw new ArgumentException("Die Sollwertreihe muss 8760 Stunden führen.", nameof(reihe));
            int t0 = tag * 24;
            for (int h = t0; h < t0 + 24; h++)
                if (!double.IsNaN(reihe[h]) && !double.IsInfinity(reihe[h])) return true;
            return false;
        }
    }

    /// <summary>
    /// <b>Die Kennzahlen der Zonensperre einer Zone</b> (Entwurf AK3-K 3.5, Festlegung 20): die Tage mit Sperre der
    /// Gegenseite (der Probetag zeigte beide Seiten) und die gesperrte Energie des Probetags — im Lauf als Hinweis
    /// gemeldet und als Projektsumme über die Zonen im Ergebnis (<see cref="Ak3KSchema.SPALTEN_ZONENSPERRE"/>).
    /// </summary>
    /// <param name="Kuehltage">Mischtage, die als Kühltag gerechnet wurden (Raumheizung gesperrt).</param>
    /// <param name="Heiztage">Mischtage, die als Heiztag gerechnet wurden (Raumkühlung gesperrt).</param>
    /// <param name="StundenHeizen">Stunden mit Raumheizung im Probetag der Kühltage.</param>
    /// <param name="StundenKuehlen">Stunden mit Raumkühlung im Probetag der Heiztage.</param>
    /// <param name="HeizenGesperrtKwh">Raumheizung des Probetags an Kühltagen [kWh].</param>
    /// <param name="KuehlenGesperrtKwh">Raumkühlung des Probetags an Heiztagen [kWh].</param>
    /// <param name="TageBeides">Tage mit beiden Freigaben (an ihnen entscheidet der Bedarf).</param>
    public sealed record Zonensperrkennzahl(int Kuehltage, int Heiztage, int StundenHeizen, int StundenKuehlen,
                                           double HeizenGesperrtKwh, double KuehlenGesperrtKwh, int TageBeides)
    {
        /// <summary>Die Tage mit Sperre der Gegenseite.</summary>
        public int Tage => Kuehltage + Heiztage;

        /// <summary>Die Stunden des Probetags auf der gesperrten Seite.</summary>
        public int Stunden => StundenHeizen + StundenKuehlen;

        /// <summary>Die gesperrte Energie des Probetags [kWh].</summary>
        public double GesperrtKwh => HeizenGesperrtKwh + KuehlenGesperrtKwh;

        /// <summary>Die Kennzahlen im Maßstab des wirklichen Gebäudes: die Energien mal <paramref name="faktor"/>.</summary>
        public Zonensperrkennzahl Skaliert(double faktor)
            => this with { HeizenGesperrtKwh = HeizenGesperrtKwh * faktor, KuehlenGesperrtKwh = KuehlenGesperrtKwh * faktor };

        /// <summary>
        /// Die Summe über Zonen bzw. Gebäude (Tage, Stunden und Energien addiert; Zonentage); <c>null</c>, wenn keiner
        /// der Einträge eine Kennzahl trägt (die Sperre lief nirgends).
        /// </summary>
        public static Zonensperrkennzahl Summe(IEnumerable<Zonensperrkennzahl> teile)
        {
            Zonensperrkennzahl s = null;
            if (teile == null) return null;
            foreach (Zonensperrkennzahl t in teile)
            {
                if (t == null) continue;
                s = s == null ? t : new Zonensperrkennzahl(s.Kuehltage + t.Kuehltage, s.Heiztage + t.Heiztage,
                                                           s.StundenHeizen + t.StundenHeizen, s.StundenKuehlen + t.StundenKuehlen,
                                                           s.HeizenGesperrtKwh + t.HeizenGesperrtKwh,
                                                           s.KuehlenGesperrtKwh + t.KuehlenGesperrtKwh, s.TageBeides + t.TageBeides);
            }
            return s;
        }
    }
}
