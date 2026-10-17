using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Taktverlust der Wärmepumpe nach EN 14825</b> (Welle M4, Punkt WP1 der Entscheidungsvorlage
    /// Modellgrenzen): reine, zustandslose Funktionen, die Rechenweg, Oberfläche und Tests gleichermaßen
    /// rufen.
    /// </summary>
    /// <remarks>
    /// <para><b>Die Regel.</b> Unterhalb der kleinsten Modulationsleistung P_min taktet das Gerät. Je
    /// Stunde gilt das Lastverhältnis CR = Q_Stunde / (P_min · 1 h); für 0 &lt; CR &lt; 1 sinkt die
    /// Leistungszahl auf COP_takt = COP · CR / (C_d · CR + (1 − C_d)), sonst bleibt sie. C_d ist der
    /// Teillastkoeffizient der EN 14825; ohne Messwert gilt dort 0,9.</para>
    /// <para><b>Ohne Mindestleistung keine Taktrechnung.</b> Leer oder 0 heißt „moduliert bis null" —
    /// die Stunde rechnet bitgleich wie zuvor (<see cref="RechnetMitTakt"/>).</para>
    /// <para><b>Starts</b> zählt die Stunde wie beim Kessel (<see cref="Kesselkennlinie.StartsImTakt"/>):
    /// im Takt so viele, wie Mindestläufe die Wärme braucht, höchstens 60 durch die Mindestlaufzeit.
    /// Die Wärmepumpe führt keine eigene Mindestlaufzeit; es gilt <see cref="MINDESTLAUFZEIT_MIN"/>.
    /// Beide Schwellen tragen den Zahlenrand (<see cref="Kesselkennlinie.Taktet"/>).</para>
    /// </remarks>
    public static class Waermepumpentakt
    {
        /// <summary>Vorgabe des Teillastkoeffizienten C_d nach EN 14825, wenn er nicht gemessen ist.</summary>
        public const double VORGABE_CD = 0.9;

        /// <summary>
        /// Die Mindestlaufzeit je Start [min], mit der die Startzahl einer Taktstunde gezählt wird —
        /// dieselbe Vorgabe wie beim Kessel (<see cref="Kesselkennlinie.VORGABE_MINDESTLAUFZEIT_MIN"/>).
        /// </summary>
        public const int MINDESTLAUFZEIT_MIN = Kesselkennlinie.VORGABE_MINDESTLAUFZEIT_MIN;

        /// <summary>Rechnet ein Gerät mit dieser Mindestleistung den Taktverlust? Nur mit P_min &gt; 0.</summary>
        public static bool RechnetMitTakt(double? mindestleistungKw)
            => mindestleistungKw.HasValue && Endlich(mindestleistungKw.Value) && mindestleistungKw.Value > 0;

        /// <summary>Der wirksame Teillastkoeffizient: gepflegt (0 … 1), sonst <see cref="VORGABE_CD"/>.</summary>
        public static double CdWirksam(double? cd)
            => cd.HasValue && Endlich(cd.Value) && cd.Value >= 0 && cd.Value <= 1 ? cd.Value : VORGABE_CD;

        /// <summary>Das Lastverhältnis CR = Q / (P_min · 1 h); 0 ohne Mindestleistung.</summary>
        public static double Lastverhaeltnis(double waermeKwh, double mindestleistungKw)
            => mindestleistungKw > 0 && Endlich(waermeKwh) ? waermeKwh / mindestleistungKw : 0.0;

        /// <summary>
        /// Der Teillastfaktor f = CR / (C_d · CR + (1 − C_d)) für 0 &lt; CR &lt; 1, sonst 1 —
        /// COP_takt = COP · f.
        /// </summary>
        public static double Teillastfaktor(double cr, double cd)
        {
            if (!(cr > 0) || !(cr < 1)) return 1.0;
            return cr / (cd * cr + (1.0 - cd));
        }

        /// <summary>Die Leistungszahl im Takt: COP · <see cref="Teillastfaktor"/>.</summary>
        public static double CopTakt(double cop, double cr, double cd) => cop * Teillastfaktor(cr, cd);

        /// <summary>
        /// TAKTET das Gerät in einer Stunde mit der Wärme <paramref name="waermeKwh"/>? Bei
        /// 0 &lt; Q &lt; P_min, mit dem Zahlenrand des Kessels (<see cref="Kesselkennlinie.Taktet"/>).
        /// </summary>
        public static bool Taktet(double waermeKwh, double mindestleistungKw)
            => mindestleistungKw > 0 && Kesselkennlinie.Taktet(waermeKwh, mindestleistungKw);

        /// <summary>Die Starts einer Taktstunde (<see cref="Kesselkennlinie.StartsImTakt"/> mit <see cref="MINDESTLAUFZEIT_MIN"/>).</summary>
        public static int StartsImTakt(double waermeKwh, double mindestleistungKw)
            => Kesselkennlinie.StartsImTakt(waermeKwh, mindestleistungKw, MINDESTLAUFZEIT_MIN);

        /// <summary>
        /// Der MEHRSTROM einer Taktstunde [kWh]: der Verdichterstrom <paramref name="stromKwh"/> bei
        /// unveränderter Wärme durch den Teillastfaktor geteilt, minus der Strom selbst —
        /// P · (1/f − 1). 0, wenn die Stunde nicht taktet.
        /// </summary>
        public static double Mehrstrom(double stromKwh, double waermeKwh, double mindestleistungKw, double cd)
        {
            if (!(stromKwh > 0) || !Taktet(waermeKwh, mindestleistungKw)) return 0.0;
            double f = Teillastfaktor(Lastverhaeltnis(waermeKwh, mindestleistungKw), cd);
            if (!(f > 0)) return 0.0;
            return stromKwh / f - stromKwh;
        }

        private static bool Endlich(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
}
