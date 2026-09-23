using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Bilanzreihe des Brauchwasserkanals</b> (Umsetzungskonzept Zapfprofilgenerator
    /// Kapitel 0 Punkt 3, 2.1): 8760 Stundenwerte in kWh je Stunde, dazu zwölf Monatssummen und
    /// die Jahressumme in kWh — getrennt für Zapfung und Zirkulation.
    ///
    /// <para><b>Unveränderlich.</b> Der Konstruktor kopiert die Werte; nach außen gibt es sie nur
    /// lesend (<see cref="StundenKwh"/>) oder als neue Kopie (<see cref="KopieStundenKwh"/>) für
    /// die Übergabe an den Brauchwasserkanal des Laufs (Gruppe 2, Weiche). Summen entstehen
    /// einmal beim Bau in fester Reihenfolge (Stunde 1 bis 8760).</para>
    ///
    /// <para><b>Nur Bilanz.</b> Keine Auslegungsklasse nimmt diesen Typ an (Invariante 2.4,
    /// Wache in Z2).</para>
    /// </summary>
    internal sealed class Bilanzreihe
    {
        /// <summary>Stunden des Rechenjahres.</summary>
        internal const int STUNDEN = Zapfkalender.STUNDEN_JAHR;

        private readonly double[] _stundenKwh;
        private readonly double[] _monateKwh;

        /// <summary>Baut eine Reihe aus 8760 endlichen Stundenwerten; die Werte werden kopiert.</summary>
        internal Bilanzreihe(IReadOnlyList<double> stundenKwh)
        {
            if (stundenKwh == null) throw new ArgumentNullException(nameof(stundenKwh));
            if (stundenKwh.Count != STUNDEN)
                throw new ArgumentException("Eine Bilanzreihe trägt 8760 Stundenwerte, nicht " + stundenKwh.Count + ".",
                                            nameof(stundenKwh));
            _stundenKwh = new double[STUNDEN];
            _monateKwh = new double[Zapfkalender.MONATE];
            double jahr = 0.0;
            double groesster = 0.0;
            int h = 0;
            for (int m = 0; m < Zapfkalender.MONATE; m++)
            {
                double monat = 0.0;
                int ende = h + Zapfkalender.TageJeMonat[m] * Zapfkalender.STUNDEN_TAG;
                for (; h < ende; h++)
                {
                    double w = stundenKwh[h];
                    if (double.IsNaN(w) || double.IsInfinity(w))
                        throw new ArgumentException("Stunde " + (h + 1) + " der Bilanzreihe ist keine endliche Zahl.",
                                                    nameof(stundenKwh));
                    _stundenKwh[h] = w;
                    monat += w;
                    jahr += w;
                    if (w > groesster) groesster = w;
                }
                _monateKwh[m] = monat;
            }
            JahressummeKwh = jahr;
            GroessterStundenwertKw = groesster;
        }

        /// <summary>Eine Reihe aus lauter Nullen.</summary>
        internal static Bilanzreihe Null() => new Bilanzreihe(new double[STUNDEN]);

        /// <summary>Die 8760 Stundenwerte [kWh je Stunde] — nur lesbar.</summary>
        internal IReadOnlyList<double> StundenKwh => Array.AsReadOnly(_stundenKwh);

        /// <summary>Eine neue Kopie der Stundenwerte für eine Übergabe, die ein <c>double[]</c> verlangt.</summary>
        internal double[] KopieStundenKwh() => (double[])_stundenKwh.Clone();

        /// <summary>Die zwölf Monatssummen [kWh] (Januar … Dezember, Jahr ohne Schaltjahr).</summary>
        internal IReadOnlyList<double> MonatssummenKwh => Array.AsReadOnly(_monateKwh);

        /// <summary>Die Jahressumme [kWh], Stunde für Stunde aufsummiert.</summary>
        internal double JahressummeKwh { get; }

        /// <summary>Der größte Stundenwert [kWh je Stunde = kW]; ein Bilanzwert, keine Auslegungsgröße.</summary>
        internal double GroessterStundenwertKw { get; }

        /// <summary>Die stundenweise Summe mehrerer Reihen — in der Reihenfolge der Aufzählung addiert.</summary>
        internal static Bilanzreihe Summe(IEnumerable<Bilanzreihe> reihen)
        {
            var s = new double[STUNDEN];
            if (reihen != null)
                foreach (Bilanzreihe r in reihen)
                {
                    if (r == null) continue;
                    for (int h = 0; h < STUNDEN; h++) s[h] += r._stundenKwh[h];
                }
            return new Bilanzreihe(s);
        }

        /// <summary>Die Reihe mit einem Faktor gestreckt.</summary>
        internal Bilanzreihe Mal(double faktor)
        {
            var s = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++) s[h] = _stundenKwh[h] * faktor;
            return new Bilanzreihe(s);
        }

        /// <summary>Wie viele Stunden liegen über einer Schwelle [kW]?</summary>
        internal int StundenUeber(double schwelleKw)
        {
            int n = 0;
            for (int h = 0; h < STUNDEN; h++) if (_stundenKwh[h] > schwelleKw) n++;
            return n;
        }
    }
}
