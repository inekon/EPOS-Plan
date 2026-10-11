using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Heizkreis des Gebäudes im Mehrzonenweg</b> (E63, AK1z) — die Werte, die EINMAL am Gebäude
    /// gebildet und an alle gekoppelten Zonen gereicht werden (<see cref="GebaeudeModellEingang.ZonenkopplungAufloesen"/>):
    /// Übergabe des Gebäudes (Art, Exponent, Auslegungspunkt, Nennleistung), Heizkurve, Vorlaufreihe,
    /// Auslegungsaußentemperatur und die Auslegungsheizlast als Summe der Zonen. Daraus bildet
    /// <see cref="HeizkreisErgebnis.Bilden(Gebaeudeheizkreis, double[], double[], double[], double, double, double[], double)"/>
    /// das Heizkreisergebnis des Gebäudes. Keine Rechengröße des Lösers — jede Zone rechnet mit ihren
    /// eigenen Kennwerten am gemeinsamen Vorlauf.
    /// </summary>
    internal sealed class Gebaeudeheizkreis
    {
        /// <summary>Die Übergabeart des Gebäudes (<c>DbWerte.UEBERGABE_*</c>).</summary>
        internal string UebergabeArt { get; init; }

        /// <summary>Die Kennwerte der Übergabe des Gebäudes (Nennleistung des Gebäudes, Auslegungspunkt des Gebäudes).</summary>
        internal Uebergabekennwerte Uebergabe { get; init; }

        /// <summary>Ist die Nennleistung des Gebäudes hergeleitet (Summe der Auslegungslasten der Zonen)?</summary>
        internal bool NennleistungHergeleitet { get; init; }

        /// <summary>Die Auslegungsheizlast des Gebäudes [W]: Summe der stationären Lasten der beheizten Zonen.</summary>
        internal double AuslegungsheizlastW { get; init; }

        /// <summary>Die Auslegungsaußentemperatur [°C].</summary>
        internal double AuslegungAussentemperaturC { get; init; }

        /// <summary>Ist die Auslegungsaußentemperatur aus der Klimareihe hergeleitet (H10)?</summary>
        internal bool AuslegungAussentemperaturHergeleitet { get; init; }

        /// <summary>Das Proportionalband des Gebäudes [K] (Vorgabe der Zonen ohne eigenes).</summary>
        internal double ReglerbandK { get; init; }

        /// <summary>Die Heizkurve des Gebäudes.</summary>
        internal Heizkurve Heizkurve { get; init; }

        /// <summary>Fährt das Gebäude die Heizkurve?</summary>
        internal bool HeizkurveAktiv { get; init; }

        /// <summary>Woher der Vorlauf kommt.</summary>
        internal Vorlaufquelle Vorlaufquelle { get; init; }

        /// <summary>Der feste Vorlauf [°C] ohne Heizkurve; NaN mit ihr.</summary>
        internal double VorlaufFestC { get; init; } = double.NaN;

        /// <summary>Der gemeinsame Vorlauf je Stunde [°C]; NaN jenseits der Heizgrenze.</summary>
        internal double[] VorlaufC { get; init; }

        /// <summary>Der Strahlungsanteil der Übergabe des Gebäudes [–] (Feld, sonst Vorgabe der Art).</summary>
        internal double Strahlungsanteil { get; init; }

        /// <summary>Stunden, in denen keine gekoppelte Zone einen Heizsollwert trägt (Heizkalender „aus").</summary>
        internal int StundenOhneHeizungH { get; init; }

        /// <summary>Fährt eine gekoppelte Zone ein Sollwertprofil?</summary>
        internal bool SollwertprofilWirksam { get; init; }

        /// <summary>
        /// <b>Die Reihen des Gebäudes aus den Reihen der gekoppelten Zonen</b> (E63) — je Stunde über die
        /// Zonen mit definiertem Vorlauf:
        /// <list type="bullet">
        /// <item>Vorlauf: der gemeinsame Vorlauf, wenn eine Zone ihn führt; sonst NaN.</item>
        /// <item>Rücklauf <b>massenstromgewichtet</b>: θ_R,geb = Σ W_H,z·θ_R,z / Σ W_H,z — gleichwertig
        /// θ_V − ΣΦ_z/ΣW_H,z, weil jede Zone θ_R,z = θ_V − Φ_z/W_H,z rechnet. Trägt eine Zone eine
        /// unbegrenzte Übergabe (W_H = ∞), ist der Rücklauf der Grenzwert θ_V.</item>
        /// <item>Begrenzt-Anteil: das Maximum über die Zonen.</item>
        /// </list>
        /// </summary>
        /// <param name="zonen">Je gekoppelte Zone: Wärmekapazitätsstrom W_H [W/K], Vorlauf-, Rücklauf- und Begrenzt-Reihe.</param>
        internal static void Mischen(IReadOnlyList<(double WHWK, double[] VorlaufC, double[] RuecklaufC, double[] Begrenzt)> zonen,
                                     out double[] vorlaufC, out double[] ruecklaufC, out double[] begrenzt)
        {
            if (zonen == null || zonen.Count == 0) throw new ArgumentException("Keine gekoppelte Zone.", nameof(zonen));
            vorlaufC = new double[8760];
            ruecklaufC = new double[8760];
            begrenzt = new double[8760];
            for (int h = 0; h < 8760; h++)
            {
                double v = double.NaN, summeW = 0.0, summeWR = 0.0, b = 0.0;
                bool unbegrenzt = false;
                foreach (var z in zonen)
                {
                    if (z.Begrenzt[h] > b) b = z.Begrenzt[h];
                    double vz = z.VorlaufC[h];
                    if (double.IsNaN(vz)) continue;
                    v = vz;
                    if (double.IsPositiveInfinity(z.WHWK)) unbegrenzt = true;
                    else
                    {
                        summeW += z.WHWK;
                        summeWR += z.WHWK * z.RuecklaufC[h];
                    }
                }
                vorlaufC[h] = v;
                ruecklaufC[h] = double.IsNaN(v) ? double.NaN : unbegrenzt ? v : summeWR / summeW;
                begrenzt[h] = b;
            }
        }
    }
}
