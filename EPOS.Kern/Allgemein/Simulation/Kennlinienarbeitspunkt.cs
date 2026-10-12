using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Arbeitspunkt einer Wärmepumpe am eingestellten Vorlauf, ausgewertet am Normpunkt der Quelle
    /// (WP1a, Gerätesteckbrief der Satz-Überlagerung): Heizleistung und COP der PROJEKTKENNLINIEN.
    /// </summary>
    /// <param name="Quellkennung">Der Buchstabe der Quelle nach EN 14511: A (Luft), B (Sole), W (Wasser).</param>
    /// <param name="Quelltemperatur">Die Quelltemperatur des Normpunkts [°C].</param>
    /// <param name="Vorlauf">Der Vorlauf, an dem ausgewertet wurde [°C].</param>
    /// <param name="Heizleistung">Die Heizleistung [kW].</param>
    /// <param name="Cop">Die Leistungszahl.</param>
    public sealed record KennlinienArbeitspunkt(char Quellkennung, int Quelltemperatur, int Vorlauf,
                                                double Heizleistung, double Cop)
    {
        /// <summary>Der Normpunkt als Kurzzeichen, etwa „B0/W55".</summary>
        public string Normpunkt
            => Quellkennung + Quelltemperatur.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/W"
               + Vorlauf.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// <b>Der Arbeitspunkt am eingestellten Vorlauf</b> (WP1a): liest die Projektkennlinien wie der Lauf
    /// (<see cref="SimulationWaermepumpe.KennlinienwahlLaden"/>), wählt die Kennlinie am Vorlauf wie der Lauf
    /// (<c>Kennlinienwahl.Abfragen</c> samt Interpolation über den Vorlauf, <see cref="VorlaufInterpolation"/>)
    /// und wertet sie an der Quelltemperatur mit denselben Funktionen aus wie der Lauf
    /// (<c>berechne_wptherm</c>, <c>KennlinieInterpoliertAuswerten</c>). Nichts davon ist nachgebaut.
    ///
    /// <para><b>Nur lesend und ohne Randregel:</b> Liegt der Vorlauf außerhalb der Stützstellen oder die
    /// Quelltemperatur außerhalb einer der beteiligten Kennlinien, gibt es keinen Arbeitspunkt (<c>null</c>) —
    /// eine Kappung oder Extrapolation wäre eine Aussage des Laufs mit Protokoll, keine Angabe des Geräts.</para>
    /// </summary>
    public static class Kennlinienarbeitspunkt
    {
        /// <summary>Der Normpunkt der Quelle zum Wärmepumpentyp: Luft A2, Sole B0, Wasser W10; sonst <c>null</c>.</summary>
        public static (char Kennung, int Temperatur)? Normpunkt(string typ)
        {
            string t = (typ ?? "").Trim();
            if (t.StartsWith("Luft", StringComparison.OrdinalIgnoreCase)) return ('A', 2);
            if (t.StartsWith("Sole", StringComparison.OrdinalIgnoreCase) ||
                t.StartsWith("Erd", StringComparison.OrdinalIgnoreCase)) return ('B', 0);
            if (t.StartsWith("Wasser", StringComparison.OrdinalIgnoreCase)) return ('W', 10);
            return null;
        }

        /// <summary>Der Arbeitspunkt der Projektwärmepumpe <paramref name="idWp"/> (Tab_Kenndaten) am Vorlauf.</summary>
        public static KennlinienArbeitspunkt Rechnen(int idWp, int vorlauf, string typ)
        {
            if (idWp <= 0 || vorlauf <= 0) return null;
            if (!(Normpunkt(typ) is (char kennung, int quelle))) return null;
            SimulationWaermepumpe.Kennlinienwahl wahl = SimulationWaermepumpe.KennlinienwahlLaden(idWp, null);
            return Auswerten(wahl, vorlauf, kennung, quelle);
        }

        /// <summary>Wertet eine geladene Kennlinienwahl aus (die Probe der Tests ohne Datenbank).</summary>
        internal static KennlinienArbeitspunkt Auswerten(SimulationWaermepumpe.Kennlinienwahl wahl, int vorlauf,
                                                         char kennung, int quelle)
        {
            if (wahl == null || wahl.Kurven == null || wahl.Kurven.Length == 0) return null;
            SimulationWaermepumpe.Vorlauflage lage = wahl.Abfragen(vorlauf, false, out int stelle,
                                                                   out int unten, out double gewicht);
            if (lage != SimulationWaermepumpe.Vorlauflage.Innerhalb) return null;

            var rechner = new SimulationWaermepumpe();
            double[] r;
            if (unten >= 0)
            {
                SimulationWaermepumpe._Kenndaten u = wahl.Kurven[unten], o = wahl.Kurven[unten + 1];
                if (!Umfasst(u, quelle) || !Umfasst(o, quelle)) return null;
                r = rechner.KennlinieInterpoliertAuswerten(quelle, null, u, o, gewicht, -1);
            }
            else
            {
                SimulationWaermepumpe._Kenndaten k = wahl.Kurven[stelle];
                if (!Umfasst(k, quelle)) return null;
                r = rechner.berechne_wptherm(quelle, null, k, -1);
            }
            if (r == null || r[0] == 0 || double.IsNaN(r[1]) || double.IsNaN(r[2])) return null;
            return new KennlinienArbeitspunkt(kennung, quelle, vorlauf, r[2], r[1]);
        }

        /// <summary>Liegt die Quelltemperatur zwischen der kältesten und der wärmsten Stützstelle der Kennlinie?</summary>
        private static bool Umfasst(SimulationWaermepumpe._Kenndaten k, double quelle)
        {
            if (k == null || k.dat == null || k.anz < 1) return false;
            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            for (int i = 0; i < k.anz; i++)
            {
                min = Math.Min(min, k.dat[i].Temperatur);
                max = Math.Max(max, k.dat[i].Temperatur);
            }
            return quelle >= min && quelle <= max;
        }
    }
}
