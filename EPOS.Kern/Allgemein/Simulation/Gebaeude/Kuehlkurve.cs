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
    /// θ_max ist der Kühlsollwert der Stunde, θ_V,K,N der Auslegungsvorlauf der Kühlübergabe, θ_out,K,N die
    /// Auslegungs-Außentemperatur der Kühlung nach dem gewählten Weg (<see cref="Bilden"/>, Festlegung 3, E107). Ein Fußpunkt
    /// kälter als θ_V,K,N wird auf θ_V,K,N geklemmt (Fußpunktregel, Festlegung 18). Keine gespiegelte
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

        /// <summary>Die künftige Spalte des Auslegungswegs (E107); leer = <see cref="DbWerte.KUEHLKURVE_AUSLEGUNG_TAGESMITTEL"/>.</summary>
        internal const string SPALTE_AUSLEGUNG_WEG = "Kuehlkurve_Auslegung_Weg";

        /// <summary>Die künftige Spalte der eingegebenen Auslegungs-Außentemperatur [°C] (E107, nur Weg „eingabe“).</summary>
        internal const string SPALTE_AUSLEGUNG_AUSSEN = "Kuehlkurve_Auslegung_Aussen";

        /// <param name="fusspunktC">Fußpunkt θ_V,F [°C]; kälter als <paramref name="auslegungVorlaufC"/> wird er auf den
        /// Auslegungsvorlauf geklemmt (Fußpunktregel, <see cref="FusspunktGeklemmt"/>).</param>
        /// <param name="auslegungVorlaufC">Auslegungsvorlauf der Kühlübergabe θ_V,K,N [°C].</param>
        /// <param name="auslegungAussenC">Auslegungs-Außentemperatur der Kühlung θ_out,K,N [°C].</param>
        /// <param name="vorlaufgrenzeC">Die Vorlaufgrenze [°C]; NaN = keine (Gebläsekonvektor).</param>
        internal Kuehlkurve(double fusspunktC, double auslegungVorlaufC, double auslegungAussenC, double vorlaufgrenzeC)
        {
            // Fußpunktregel (Entwurf KK, Festlegung 18): Die Kurve fällt nie mit steigender Außentemperatur an — ein
            // Fußpunkt kälter als der Auslegungsvorlauf ließe den Vorlauf an heißen Tagen steigen; er wird geklemmt.
            FusspunktEingabeC = fusspunktC;
            FusspunktGeklemmt = fusspunktC < auslegungVorlaufC;
            FusspunktC = FusspunktGeklemmt ? auslegungVorlaufC : fusspunktC;
            AuslegungVorlaufC = auslegungVorlaufC;
            AuslegungAussenC = auslegungAussenC;
            VorlaufgrenzeC = vorlaufgrenzeC;
        }

        /// <summary>Fußpunkt θ_V,F [°C], wie die Kurve ihn rechnet (nach der Fußpunktregel).</summary>
        internal double FusspunktC { get; }

        /// <summary>Der Fußpunkt, wie er hereinkam [°C] — für die Meldung der Fußpunktregel.</summary>
        internal double FusspunktEingabeC { get; }

        /// <summary>War der Fußpunkt kälter als der Auslegungsvorlauf und wurde auf ihn geklemmt (Fußpunktregel)?</summary>
        internal bool FusspunktGeklemmt { get; }

        /// <summary>Der wirksame Auslegungsweg (E107): einer der drei Werte <c>DbWerte.KUEHLKURVE_AUSLEGUNG_*</c>; gesetzt
        /// von <see cref="Bilden"/>, sonst <c>null</c> (Auslegungs-Außentemperatur unmittelbar übergeben).</summary>
        internal string AuslegungWeg { get; private set; }

        /// <summary>Fiel der Weg „eingabe“ auf den Weg „tagesmittel“ zurück (Eingabe fehlt oder liegt nicht über dem
        /// Kühlsollwert plus <see cref="GebaeudeFestwerte.KUEHLKURVE_AUSLEGUNG_EINGABE_ABSTAND_K"/>)?</summary>
        internal bool AuslegungRueckfall { get; private set; }

        /// <summary>Die eingegebene Auslegungs-Außentemperatur [°C]; NaN ohne Eingabe — für die Meldung des Rückfalls.</summary>
        internal double AuslegungEingabeC { get; private set; } = double.NaN;

        /// <summary>Auslegungsvorlauf θ_V,K,N [°C].</summary>
        internal double AuslegungVorlaufC { get; }

        /// <summary>Auslegungs-Außentemperatur der Kühlung θ_out,K,N [°C].</summary>
        internal double AuslegungAussenC { get; }

        /// <summary>Die Vorlaufgrenze [°C]; NaN = keine.</summary>
        internal double VorlaufgrenzeC { get; }

        /// <summary>
        /// <b>Das wärmste Tagesmittel</b> der Außenluft der Klimareihe [°C] — derselbe Tag wie der Auslegungstag der
        /// Nennleistung (<see cref="GebaeudeModellEingang.WaermsterTag"/>); Grundlage des Wegs „tagesmittel“.
        /// </summary>
        internal static double AuslegungAussentemperaturC(double[] thetaOut)
        {
            GebaeudeModellEingang.WaermsterTag(thetaOut, out double mittel);
            return mittel;
        }

        /// <summary>Ist <paramref name="weg"/> ein bekannter Auslegungsweg? Leer zählt (Vorgabe „tagesmittel“).</summary>
        internal static bool WegBekannt(string weg)
            => string.IsNullOrEmpty(weg)
               || weg == DbWerte.KUEHLKURVE_AUSLEGUNG_STUNDE
               || weg == DbWerte.KUEHLKURVE_AUSLEGUNG_TAGESMITTEL
               || weg == DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE;

        /// <summary>
        /// <b>Die Auslegungs-Außentemperatur der Kühlung</b> θ_out,K,N [°C] nach dem gewählten Weg (Entwurf KK, Festlegung 3;
        /// E107 „1, 2, 3 wählbar, Default 2“):
        /// <list type="number">
        /// <item><c>stunde</c> — die höchste Stundentemperatur der Klimareihe;</item>
        /// <item><c>tagesmittel</c> (Vorgabe, auch leer) — das wärmste Tagesmittel, mindestens Kühlsollwert +
        /// <see cref="GebaeudeFestwerte.KUEHLKURVE_AUSLEGUNG_SPANNE_K"/>; ohne endlichen Kühlsollwert das Tagesmittel allein;</item>
        /// <item><c>eingabe</c> — die Eingabe am Gebäude; fehlt sie oder liegt sie nicht über dem Kühlsollwert +
        /// <see cref="GebaeudeFestwerte.KUEHLKURVE_AUSLEGUNG_EINGABE_ABSTAND_K"/>, gilt Weg 2 und
        /// <paramref name="rueckfall"/> wird <c>true</c> (Laufhinweis).</item>
        /// </list>
        /// Ein unbekannter Weg ist Sache des Aufrufers (<see cref="WegBekannt"/>); hier rechnet er wie die Vorgabe.
        /// </summary>
        internal static double AuslegungAussentemperaturC(string weg, double? eingabeC, double kuehlSollC, double[] thetaOut,
                                                         out string wirksamerWeg, out bool rueckfall)
        {
            rueckfall = false;
            bool sollEndlich = !double.IsNaN(kuehlSollC) && !double.IsInfinity(kuehlSollC);
            if (weg == DbWerte.KUEHLKURVE_AUSLEGUNG_STUNDE)
            {
                wirksamerWeg = weg;
                double hoechste = double.NegativeInfinity;
                for (int h = 0; h < thetaOut.Length; h++)
                    if (thetaOut[h] > hoechste) hoechste = thetaOut[h];
                return hoechste;
            }
            if (weg == DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE)
            {
                double e = eingabeC ?? double.NaN;
                bool traegt = !double.IsNaN(e) && !double.IsInfinity(e)
                              && (!sollEndlich || e > kuehlSollC + GebaeudeFestwerte.KUEHLKURVE_AUSLEGUNG_EINGABE_ABSTAND_K);
                if (traegt)
                {
                    wirksamerWeg = weg;
                    return e;
                }
                rueckfall = true;
            }
            wirksamerWeg = DbWerte.KUEHLKURVE_AUSLEGUNG_TAGESMITTEL;
            double mittel = AuslegungAussentemperaturC(thetaOut);
            return sollEndlich ? Math.Max(mittel, kuehlSollC + GebaeudeFestwerte.KUEHLKURVE_AUSLEGUNG_SPANNE_K) : mittel;
        }

        /// <summary>
        /// Die Kurve des Gebäudes mit dem Auslegungsweg (E107): Auslegungs-Außentemperatur nach
        /// <see cref="AuslegungAussentemperaturC(string, double?, double, double[], out string, out bool)"/>, dazu die
        /// Fußpunktregel des Konstruktors.
        /// </summary>
        internal static Kuehlkurve Bilden(double fusspunktC, double auslegungVorlaufC, double vorlaufgrenzeC,
                                          string weg, double? eingabeC, double kuehlSollC, double[] thetaOut)
        {
            double aussen = AuslegungAussentemperaturC(weg, eingabeC, kuehlSollC, thetaOut, out string wirksam, out bool rueckfall);
            return new Kuehlkurve(fusspunktC, auslegungVorlaufC, aussen, vorlaufgrenzeC)
            {
                AuslegungWeg = wirksam,
                AuslegungRueckfall = rueckfall,
                AuslegungEingabeC = eingabeC ?? double.NaN,
            };
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
