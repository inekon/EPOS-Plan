using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die außentemperaturgeführte Kühlkurve</b> (Entwurf KK, 2.1; Festlegungen 2–5, 7) — der Kaltwasser-Vorlauf eines
    /// kühlgekoppelten Gebäudes als <b>Zwei-Punkt-Kurve</b> über die Außentemperatur θ_out:
    /// <code>
    /// θ_out ≤ θ_max            : θ_V = θ_V,F                                   (Fußpunkt, waagrecht, nie „aus“)
    /// θ_max &lt; θ_out &lt; θ_out,K,N : θ_V = θ_V,F + (θ_V,K,N − θ_V,F)·(θ_out − θ_max)/(θ_out,K,N − θ_max)
    /// θ_out ≥ θ_out,K,N        : θ_V = θ_V,K,N                                 (Auslegungspunkt)
    /// </code>
    /// θ_max ist der Kühlsollwert der Stunde, θ_V,K,N der Auslegungsvorlauf der Kühlübergabe, θ_out,K,N die hergeleitete
    /// Auslegungs-Außentemperatur der Kühlung (<see cref="AuslegungAussentemperaturC"/>, Festlegung 3). Keine gespiegelte
    /// Heizkurve, kein Exponent (Festlegung 2): Kühllast entsteht auch bei kühler Außenluft (Sonne, innere Lasten).
    /// <para><b>Grenzen</b> (<see cref="VorlaufC(double, double, double, out bool)"/>): oben hält jeder Vorlauf den
    /// Mindestabstand <see cref="GebaeudeFestwerte.KUEHLKURVE_FUSSPUNKT_ABSTAND_K"/> unter dem Kühlsollwert der Stunde
    /// (sonst wäre die Übergabeleistung null); kälter als der Erzeuger liefert, wird der Vorlauf nie (Mischgruppe am
    /// Gebäude); unten steht <b>immer</b> die Vorlaufgrenze <c>Kuehl_Vorlaufgrenze</c> (Festlegung 5) — sie gewinnt gegen
    /// jede andere Grenze. Ohne endlichen Kühlsollwert gilt der Fußpunkt.</para>
    /// </summary>
    internal sealed class Kuehlkurve
    {
        /// <summary>Die künftige Spalte des Schalters (Schema mit KK4, Entwurf KK 4.1).</summary>
        internal const string SPALTE_AKTIV = "Kuehlkurve_Aktiv";

        /// <summary>Die künftige Spalte des Fußpunkts θ_V,F [°C]; leer = Auslegungsrücklauf (Festlegung 4).</summary>
        internal const string SPALTE_FUSSPUNKT = "Kuehlkurve_Fusspunkt";

        /// <summary>Die künftige Spalte des Raumeinflusses k_K [K/K]; leer oder 0 = aus (Festlegung 1, 6).</summary>
        internal const string SPALTE_RAUMEINFLUSS = "Kuehlkurve_Raumeinfluss";

        /// <param name="fusspunktC">Fußpunkt θ_V,F [°C].</param>
        /// <param name="auslegungVorlaufC">Auslegungsvorlauf der Kühlübergabe θ_V,K,N [°C].</param>
        /// <param name="auslegungAussenC">Auslegungs-Außentemperatur der Kühlung θ_out,K,N [°C].</param>
        /// <param name="vorlaufgrenzeC">Die Vorlaufgrenze [°C]; NaN = keine (Gebläsekonvektor).</param>
        internal Kuehlkurve(double fusspunktC, double auslegungVorlaufC, double auslegungAussenC, double vorlaufgrenzeC)
        {
            FusspunktC = fusspunktC;
            AuslegungVorlaufC = auslegungVorlaufC;
            AuslegungAussenC = auslegungAussenC;
            VorlaufgrenzeC = vorlaufgrenzeC;
        }

        /// <summary>Fußpunkt θ_V,F [°C].</summary>
        internal double FusspunktC { get; }

        /// <summary>Auslegungsvorlauf θ_V,K,N [°C].</summary>
        internal double AuslegungVorlaufC { get; }

        /// <summary>Auslegungs-Außentemperatur der Kühlung θ_out,K,N [°C].</summary>
        internal double AuslegungAussenC { get; }

        /// <summary>Die Vorlaufgrenze [°C]; NaN = keine.</summary>
        internal double VorlaufgrenzeC { get; }

        /// <summary>
        /// <b>Die Auslegungs-Außentemperatur der Kühlung</b> [°C] (Festlegung 3): das höchste Tagesmittel der Außenluft
        /// der Klimareihe — derselbe Tag wie der Auslegungstag der Nennleistung (<see cref="GebaeudeModellEingang.WaermsterTag"/>),
        /// keine eigene Spalte.
        /// </summary>
        internal static double AuslegungAussentemperaturC(double[] thetaOut)
        {
            GebaeudeModellEingang.WaermsterTag(thetaOut, out double mittel);
            return mittel;
        }

        /// <summary>Die obere Grenze der Stunde [°C]: θ_max − Mindestabstand; +∞ ohne endlichen Kühlsollwert.</summary>
        internal static double ObergrenzeC(double kuehlSollC)
            => double.IsNaN(kuehlSollC) || double.IsInfinity(kuehlSollC)
               ? double.PositiveInfinity
               : kuehlSollC - GebaeudeFestwerte.KUEHLKURVE_FUSSPUNKT_ABSTAND_K;

        /// <summary>
        /// Die Kurve selbst [°C], ohne Grenzen — nie NaN: Fußpunkt bis zum Kühlsollwert der Stunde (auch ohne endlichen
        /// Sollwert), Auslegungsvorlauf ab der Auslegungs-Außentemperatur, dazwischen linear. Liegt die
        /// Auslegungs-Außentemperatur nicht über dem Sollwert, springt die Kurve am Sollwert vom Fußpunkt auf den
        /// Auslegungspunkt.
        /// </summary>
        internal double KurveC(double kuehlSollC, double aussenC)
        {
            if (!(aussenC > kuehlSollC)) return FusspunktC;
            if (!(aussenC < AuslegungAussenC)) return AuslegungVorlaufC;
            return FusspunktC + (AuslegungVorlaufC - FusspunktC) * (aussenC - kuehlSollC) / (AuslegungAussenC - kuehlSollC);
        }

        /// <summary>
        /// <b>Der Vorlauf der Stunde</b> [°C] mit allen Grenzen: oben der Mindestabstand unter dem Kühlsollwert, dann nie
        /// kälter als der Erzeuger <paramref name="erzeugerC"/> (NaN = ohne Erzeugerwert), zuletzt unten die
        /// Vorlaufgrenze, die immer gewinnt.
        /// </summary>
        /// <param name="kuehlSollC">Kühlsollwert θ_max der Stunde [°C]; NaN oder ∞ = keine Kühlung, dann gilt der Fußpunkt.</param>
        /// <param name="aussenC">Außentemperatur der Stunde [°C].</param>
        /// <param name="erzeugerC">Der kälteste Vorlauf, den der Erzeuger liefert [°C]; NaN = keine Grenze.</param>
        /// <param name="anVorlaufgrenze">true, wenn die Vorlaufgrenze den Vorlauf anhebt (Grund <c>VORLAUFGRENZE_KUEHLUNG</c>).</param>
        internal double VorlaufC(double kuehlSollC, double aussenC, double erzeugerC, out bool anVorlaufgrenze)
        {
            double v = Math.Min(KurveC(kuehlSollC, aussenC), ObergrenzeC(kuehlSollC));
            if (!double.IsNaN(erzeugerC) && erzeugerC > v) v = erzeugerC;
            anVorlaufgrenze = !double.IsNaN(VorlaufgrenzeC) && v < VorlaufgrenzeC;
            return anVorlaufgrenze ? VorlaufgrenzeC : v;
        }
    }
}
