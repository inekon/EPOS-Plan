using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Bezugsspitze des Netzbezugs</b> — Jahres- und Monatsspitze der
    /// Viertelstundenreihe, in kW.
    ///
    /// <para><b>Wozu.</b> Der Leistungspreis eines Stromtarifs bemisst sich an der
    /// gemessenen Bezugsspitze, nicht an einer vorgehaltenen Anschlussleistung. Die
    /// Messung läuft beim Netzbetreiber im <b>Viertelstundenmittel</b>
    /// (Registrierende Leistungsmessung) — genau dem Raster, in dem der Rechenkern
    /// den Reststrombedarf führt. Deshalb wird die Spitze hier aus der
    /// Viertelstundenreihe gebildet und <b>nicht</b> aus der auf Stunden gemittelten
    /// Ganglinie: Das Stundenmittel glättet die Spitze und fiele regelmäßig zu
    /// niedrig aus (<see cref="StromMatrix.MaxBezugKW"/> ist genau diese kleinere
    /// Zahl und bleibt der Tarifstruktur vorbehalten).</para>
    ///
    /// <para><b>Woran sie hängt.</b> Quelle ist die Reihe, die auch der Speicher
    /// kappt (<c>SimulationControl.Rest_Strombedarf_viertelstuendlich</c>: bei
    /// aktivierter Flotte die Flottennetzbilanz, sonst der Rest nach Abzug der
    /// Entladung). Damit misst die Spitze den Effekt der Lastspitzenkappung — der
    /// Unterschied zwischen Stamm und Speichervariante steht in dieser Zahl.</para>
    ///
    /// <para><b>Feste Raster.</b> Das Jahr hat 8 760 Stunden bzw. 35 040
    /// Viertelstunden und kein Schaltjahr (Hausregel). Die Monatsgrenzen folgen den
    /// Tagen 31/28/31/30/31/30/31/31/30/31/30/31 = 365.</para>
    /// </summary>
    public sealed class Netzbezugsspitze
    {
        /// <summary>Tage je Monat im festen Raster des Rechenkerns (kein Schaltjahr).</summary>
        public static readonly int[] TageJeMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Höchster Viertelstundenwert des Jahres [kW]; 0 = keine Reihe.</summary>
        public double JahrKW;

        /// <summary>Höchster Viertelstundenwert je Kalendermonat [kW], zwölf Werte.</summary>
        public double[] MonatKW = new double[12];

        /// <summary>Summe der zwölf Monatsspitzen [kW] — die Basis des Monats-Leistungspreises.</summary>
        public double MonatssummeKW
        {
            get
            {
                double s = 0;
                for (int m = 0; m < MonatKW.Length; m++) s += MonatKW[m];
                return s;
            }
        }

        /// <summary>
        /// Bildet die Spitzen aus einer Leistungsreihe in kW. Erwartet werden 35 040
        /// Viertelstundenwerte; eine 8 760er-Reihe wird als Stundenreihe gelesen
        /// (dann ist die Spitze das Stundenmaximum — mehr gibt die Reihe nicht her).
        /// Andere Längen liefern <c>null</c>: Eine Spitze aus einem unbekannten Raster
        /// wäre ein Fantasiewert, und der ist schlimmer als ein fehlender.
        /// </summary>
        public static Netzbezugsspitze AusReihe(double[] reihe)
        {
            if (reihe == null) return null;

            int jeStunde;
            if (reihe.Length == ZeitreihenSatz.Stunden * 4) jeStunde = 4;
            else if (reihe.Length == ZeitreihenSatz.Stunden) jeStunde = 1;
            else return null;

            var s = new Netzbezugsspitze();
            int i = 0;
            for (int m = 0; m < 12; m++)
            {
                int schritte = TageJeMonat[m] * 24 * jeStunde;
                double max = 0;
                for (int k = 0; k < schritte; k++, i++)
                {
                    double w = reihe[i];
                    if (w > max) max = w;
                }
                s.MonatKW[m] = max;
                if (max > s.JahrKW) s.JahrKW = max;
            }
            return s;
        }
    }
}
