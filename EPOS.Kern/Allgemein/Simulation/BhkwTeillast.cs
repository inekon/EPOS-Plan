using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Teillastkennlinie und Takten eines BHKW-Moduls</b> (Welle M4, Punkte BH1 und BH2 der
    /// Entscheidungsvorlage Modellgrenzen; Normbezug EN 15316-4-4). Eine Instanz je Modul des Laufs.
    /// </summary>
    /// <remarks>
    /// <para><b>Die Kennlinie (BH1).</b> Zwei Stützpunkte: Volllast (η_el,100, η_th,100) und 50 %
    /// elektrische Last (η_el,50, η_th,50). Dazwischen linear in der elektrischen Auslastung β:
    /// η(β) = η₅₀ + (η₁₀₀ − η₅₀) · (β − 0,5)/0,5, unter β = 0,5 gilt η₅₀, über 1 gilt η₁₀₀. Der
    /// Brennstoff einer Stunde ist B = P_el / η_el(β), die Wärme Q = B · η_th(β) — die Stromkennzahl
    /// ändert sich damit in Teillast. Die Volllastwerte stammen aus Gesamtwirkungsgrad und
    /// Leistungsverhältnis (η_el,100 = η · P_el / (P_el + P_th)), so dass die Volllast genau wie
    /// zuvor rechnet. Ein leerer Teillastwert gilt wie Volllast.</para>
    /// <para><b>Das Takten (BH2).</b> Mit gepflegtem Anfahrverlust oder gepflegter Mindestlaufzeit
    /// läuft ein Modul unter seiner Untergrenze (Grenzfaktor × Nennleistung) im Takt: Es liefert
    /// die Menge der Stunde mit der Kennlinie an der Untergrenze und zählt
    /// n = min(⌊60/t_min⌋, ⌈Q/(P_min · t_min/60)⌉) Starts, je Start den Anfahrverlust
    /// (<see cref="Kesselkennlinie.StartsImTakt"/>). Ohne die beiden Felder bleibt es unter der
    /// Untergrenze aus.</para>
    /// </remarks>
    public sealed class BhkwTeillast
    {
        /// <summary>Die Laststufe des Teillaststützpunkts: 50 % elektrische Last.</summary>
        public const double LASTSTUFE_TEILLAST = 0.5;

        /// <summary>Elektrische Nennleistung [kW].</summary>
        public double Pel { get; }

        /// <summary>Thermische Nennleistung [kW].</summary>
        public double Pth { get; }

        /// <summary>Gesamtwirkungsgrad als Faktor (<c>Tab_BHKW.Wirkungsgrad</c>).</summary>
        public double EtaGesamt { get; }

        /// <summary>η_el bei Volllast, aus Gesamtwirkungsgrad und Leistungsverhältnis.</summary>
        public double EtaEl100 { get; }

        /// <summary>η_th bei Volllast, aus Gesamtwirkungsgrad und Leistungsverhältnis.</summary>
        public double EtaTh100 { get; }

        /// <summary>η_el bei 50 % Last; leer = wie Volllast.</summary>
        public double? EtaEl50 { get; }

        /// <summary>η_th bei 50 % Last; leer = wie Volllast.</summary>
        public double? EtaTh50 { get; }

        /// <summary>Die Untergrenze als Faktor der Nennleistung (<see cref="SimulationBHKW.Grenzfaktor"/>).</summary>
        public double Grenzfaktor { get; }

        /// <summary>Anfahrverlust je Start [kWh Brennstoff]; leer = 0.</summary>
        public double? AnfahrverlustKwh { get; }

        /// <summary>Mindestlaufzeit je Start [min]; leer = Vorgabe des Kessels.</summary>
        public int? MindestlaufzeitMin { get; }

        public BhkwTeillast(double pel, double pth, double etaGesamt, double? etaEl50, double? etaTh50,
                            double grenzfaktor, double? anfahrverlustKwh, int? mindestlaufzeitMin)
        {
            Pel = pel;
            Pth = pth;
            EtaGesamt = etaGesamt;
            double summe = pel + pth;
            EtaEl100 = summe > 0 ? etaGesamt * pel / summe : 0.0;
            EtaTh100 = summe > 0 ? etaGesamt * pth / summe : 0.0;
            EtaEl50 = Gueltig(etaEl50) ? etaEl50 : null;
            EtaTh50 = Gueltig(etaTh50) ? etaTh50 : null;
            Grenzfaktor = grenzfaktor > 0 && grenzfaktor <= 1 ? grenzfaktor : 0.0;
            AnfahrverlustKwh = anfahrverlustKwh.HasValue && Endlich(anfahrverlustKwh.Value) && anfahrverlustKwh.Value >= 0
                ? anfahrverlustKwh : null;
            MindestlaufzeitMin = mindestlaufzeitMin.HasValue && mindestlaufzeitMin.Value >= 1 ? mindestlaufzeitMin : null;
        }

        private static bool Gueltig(double? eta) => eta.HasValue && Endlich(eta.Value) && eta.Value > 0 && eta.Value <= 1.5;

        private static bool Endlich(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        /// <summary>Rechnet das Modul mit der Teillastkennlinie (BH1)? Mit mindestens einem η₅₀ und rechenbaren Nennwerten.</summary>
        public bool MitKennlinie => (EtaEl50.HasValue || EtaTh50.HasValue) && Pel > 0 && Pth > 0 && EtaGesamt > 0;

        /// <summary>Taktet das Modul unter seiner Untergrenze (BH2)? Mit Anfahrverlust oder Mindestlaufzeit und einer Untergrenze.</summary>
        public bool MitTakten => (AnfahrverlustKwh.HasValue || MindestlaufzeitMin.HasValue) && Grenzfaktor > 0 &&
                                 Pel > 0 && Pth > 0 && EtaGesamt > 0;

        /// <summary>Rechnet das Modul anders als mit festen Volllastwirkungsgraden ohne Takten?</summary>
        public bool Aktiv => MitKennlinie || MitTakten;

        /// <summary>Die wirksame Mindestlaufzeit [min] (<see cref="Kesselkennlinie.MindestlaufzeitWirksam"/>).</summary>
        public int MindestlaufzeitWirksam => Kesselkennlinie.MindestlaufzeitWirksam(MindestlaufzeitMin);

        /// <summary>Der wirksame Anfahrverlust je Start [kWh]: gepflegt, sonst 0.</summary>
        public double AnfahrverlustWirksam => AnfahrverlustKwh ?? 0.0;

        /// <summary>
        /// Der Wirkungsgrad an der Laststufe β zwischen den Stützpunkten 0,5 und 1, außerhalb geklemmt;
        /// ohne Teillastwert der Volllastwert — für jedes β bitgleich.
        /// </summary>
        public static double Eta(double beta, double eta100, double? eta50)
        {
            if (!eta50.HasValue) return eta100;
            double b = beta < LASTSTUFE_TEILLAST ? LASTSTUFE_TEILLAST : (beta > 1.0 ? 1.0 : beta);
            return eta50.Value + (eta100 - eta50.Value) * (b - LASTSTUFE_TEILLAST) / (1.0 - LASTSTUFE_TEILLAST);
        }

        /// <summary>η_el an der elektrischen Laststufe β.</summary>
        public double EtaEl(double beta) => Eta(beta, EtaEl100, EtaEl50);

        /// <summary>η_th an der elektrischen Laststufe β.</summary>
        public double EtaTh(double beta) => Eta(beta, EtaTh100, EtaTh50);

        /// <summary>Die Stromkennzahl σ(β) = η_el / η_th an der Laststufe β.</summary>
        public double Stromkennzahl(double beta)
        {
            double th = EtaTh(beta);
            return th > 0 ? EtaEl(beta) / th : 0.0;
        }

        /// <summary>
        /// Die Wärme zu einer Strommenge <paramref name="stromKwh"/> einer Stunde: β = P / P_el,
        /// Q = P · η_th(β) / η_el(β). Ohne Kennlinie der Dreisatz des Bestands, <c>P / P_el · P_th</c>.
        /// </summary>
        public double WaermeAusStrom(double stromKwh)
        {
            if (!MitKennlinie) return stromKwh / Pel * Pth;
            double beta = stromKwh / Pel;
            return stromKwh * EtaTh(beta) / EtaEl(beta);
        }

        /// <summary>
        /// Die Strommenge zu einer Wärme <paramref name="waermeKwh"/> einer Stunde — die Umkehrung von
        /// <see cref="WaermeAusStrom"/>. Ohne Kennlinie der Dreisatz des Bestands, <c>Q / P_th · P_el</c>.
        /// Mit Kennlinie: unter Q(0,5) und über Q(1) linear mit den Randwirkungsgraden, dazwischen durch
        /// Halbierung (Q(β) ist dort stetig und steigend).
        /// </summary>
        public double StromAusWaerme(double waermeKwh)
        {
            if (!MitKennlinie) return waermeKwh / Pth * Pel;
            if (!(waermeKwh > 0)) return 0.0;

            double q50 = WaermeAusStrom(LASTSTUFE_TEILLAST * Pel);
            if (waermeKwh <= q50)
                return waermeKwh * EtaEl(LASTSTUFE_TEILLAST) / EtaTh(LASTSTUFE_TEILLAST);
            double q100 = WaermeAusStrom(Pel);
            if (waermeKwh >= q100)
                return waermeKwh * EtaEl(1.0) / EtaTh(1.0);

            double lo = LASTSTUFE_TEILLAST, hi = 1.0;
            for (int i = 0; i < 60; i++)
            {
                double mitte = 0.5 * (lo + hi);
                if (WaermeAusStrom(mitte * Pel) < waermeKwh) lo = mitte; else hi = mitte;
            }
            double beta = 0.5 * (lo + hi);
            // Die Strommenge aus der Wärme und der Stromkennzahl an β - damit gilt Q = P / σ(β) genau.
            return waermeKwh * Stromkennzahl(beta);
        }

        /// <summary>Die Strommenge einer Taktstunde zur Wärme Q: mit der Stromkennzahl an der Untergrenze.</summary>
        public double TaktStromAusWaerme(double waermeKwh)
            => MitKennlinie ? waermeKwh * Stromkennzahl(Grenzfaktor) : waermeKwh / Pth * Pel;

        /// <summary>Die Wärme einer Taktstunde zur Strommenge P: mit der Stromkennzahl an der Untergrenze.</summary>
        public double TaktWaermeAusStrom(double stromKwh)
        {
            if (!MitKennlinie) return stromKwh / Pel * Pth;
            double s = Stromkennzahl(Grenzfaktor);
            return s > 0 ? stromKwh / s : 0.0;
        }

        /// <summary>Die Mindestwärmeleistung [kW] an der Untergrenze — P_min der Taktregel.</summary>
        public double MindestwaermeKw => MitKennlinie ? WaermeAusStrom(Grenzfaktor * Pel) : Grenzfaktor * Pth;

        /// <summary>
        /// Der Brennstoff einer Stunde [kWh] mit Strom <paramref name="stromKwh"/> und Wärme
        /// <paramref name="waermeKwh"/>: im Takt mit dem Wirkungsgrad der Untergrenze, sonst an
        /// β = P / P_el; ohne Kennlinie der Bestand (Q + P) / η. Ohne Anfahrverlust.
        /// </summary>
        public double Brennstoff(double stromKwh, double waermeKwh, bool imTakt)
        {
            if (!MitKennlinie) return (waermeKwh + stromKwh) / EtaGesamt;
            double beta = imTakt ? Grenzfaktor : stromKwh / Pel;
            double el = EtaEl(beta);
            return el > 0 ? stromKwh / el : (waermeKwh + stromKwh) / EtaGesamt;
        }

        /// <summary>Die Starts einer Taktstunde mit der Wärme Q (<see cref="Kesselkennlinie.StartsImTakt"/>).</summary>
        public int StartsImTakt(double waermeKwh)
            => Kesselkennlinie.StartsImTakt(waermeKwh, MindestwaermeKw, MindestlaufzeitWirksam);
    }
}
