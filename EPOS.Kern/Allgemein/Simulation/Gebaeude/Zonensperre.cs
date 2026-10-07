using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Kernschalter der Zonensperre</b> (Entwurf AK3-K, Abschnitt 3, Festlegungen 1–7 und 21; Welle KZ): In einer
    /// Zone wird an einem Tag nie geheizt und gekühlt. Bis zur Basiswelle K5 rechnet die Sperre nur mit diesem Schalter
    /// (ohne Schema, <b>Vorgabe aus</b>, Muster <see cref="Ak3Kernstufe"/>); aus heißt Zeichen für Zeichen der Bestand.
    /// Die Proben und der Referenzlauf (<c>--zonensperre ein</c>) schalten ein.
    /// <para>Die Sperre wirkt in Pass 1 im Jahreslauf des Gebäude-Steppers (<see cref="GebaeudeStepper.Jahr"/>) und damit
    /// auf allen Stufen (ohne Kopplung, AK1, AK2, AK3); der Kreis von AK3 rechnet aus demselben Eingang und übernimmt
    /// die Tagesart als feste Vorgabe. Der Tagesbilanz-Weg rechnet keine Kühlung und bleibt unberührt.</para>
    /// </summary>
    public static class Zonensperre
    {
        /// <summary>Ist die Zonensperre an? Vorgabe <c>false</c>.</summary>
        public static bool An { get; set; }

        /// <summary>Setzt den Schalter für die Dauer eines <c>using</c>-Blocks und stellt danach den vorherigen zurück.</summary>
        public static IDisposable Schalten(bool an)
        {
            bool vorher = An;
            An = an;
            return new Rueckstellung(vorher);
        }

        private sealed class Rueckstellung : IDisposable
        {
            private readonly bool _vorher;
            private bool _erledigt;

            internal Rueckstellung(bool vorher) { _vorher = vorher; }

            public void Dispose()
            {
                if (_erledigt) return;
                _erledigt = true;
                An = _vorher;
            }
        }
    }

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
    /// Gegenseite (der Probetag zeigte beide Seiten) und die gesperrte Energie des Probetags. Bis S1 (K4) nur im Lauf
    /// erhoben und als Hinweis gemeldet; keine Ergebnisspalte.
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
    }
}
