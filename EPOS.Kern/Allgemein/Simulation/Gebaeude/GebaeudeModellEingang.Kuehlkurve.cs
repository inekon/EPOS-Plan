using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// KK3 (Entwurf KK 2.1, 2.3; E106 Q-KK-1 (a)): die Untergrenze der Kühlkurve am gleitenden Erzeuger.
    /// </summary>
    internal sealed partial class GebaeudeModellEingang
    {
        /// <summary>
        /// Der Erzeugerwert, unter den die Kühlkurve den Vorlauf nie legt [°C]: ohne gleitenden Erzeuger der feste
        /// Anlagenvorlauf, mit ihm der kälteste erreichbare Erzeugervorlauf (<see cref="KuehlkurveUntergrenzeSetzen"/>);
        /// NaN = keine Grenze oder keine Kühlkurve. Zugleich die Untergrenze des Raumeinflusses der Kühlkurve im Kreis.
        /// </summary>
        internal double KuehlkurveErzeugerC { get; private set; } = double.NaN;

        /// <summary>
        /// <b>Der gleitende Erzeuger als Untergrenze der Kühlkurve</b> (Entwurf KK 2.1, 2.3; offener Punkt aus KK1): Fährt der
        /// Kälteerzeuger je Stunde den kältesten verlangten Vorlauf, liegt die Untergrenze der Kurve nicht mehr am festen
        /// Anlagenvorlauf, sondern am kältesten erreichbaren Erzeugervorlauf <paramref name="erzeugerMinC"/> (kleinste
        /// Stützstelle der Kühlkennlinie, <c>Kaltwasser_Vorlauf_Min</c> der Kältemaschine) — sonst könnte die Kurve den
        /// Vorlauf nur anheben. Die Reihe wird mit allen Grenzen neu gebildet; die Vorlaufgrenze gewinnt weiter
        /// (Festlegung 5). Ohne wirksame Kurve oder ohne endlichen Wert geschieht nichts (Schalter aus bitgleich).
        /// </summary>
        internal void KuehlkurveUntergrenzeSetzen(double erzeugerMinC)
        {
            if (!KuehlkurveWirksam || Kuehlkurve == null || !Endlich(erzeugerMinC)) return;
            KuehlkurveErzeugerC = erzeugerMinC;
            var reihe = new double[8760];
            var anGrenze = new bool[8760];
            for (int h = 0; h < 8760; h++)
                reihe[h] = Kuehlkurve.VorlaufC(ThetaMax[h], ThetaOut[h], erzeugerMinC, out anGrenze[h]);
            KuehlVorlaufC = reihe;
            KuehlVorlaufAnGrenze = anGrenze;
        }
    }
}
