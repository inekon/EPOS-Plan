using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Technische und wirtschaftliche Parameter eines Batteriespeichers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Der Typ ist ein <c>record</c> mit ausschliesslich <c>init</c>-Settern und damit
    /// nach der Konstruktion unveraenderlich. Das ist Voraussetzung dafuer, dass die
    /// Rastersuche der Auslegungsoptimierung dieselbe Instanz gefahrlos ueber
    /// <c>Parallel.For</c> verteilen kann (Fachkonzept 8.1). Varianten werden ueber
    /// <c>parameter with { CNomKwh = ..., PKw = ... }</c> gebildet.
    /// </para>
    /// <para>
    /// Einheiten: Energie [kWh], Leistung [kW], Zeit [h], Preise [ct/kWh],
    /// Investitionsgroessen [EUR]. Zinssatz und Degradation als Bruch (0,03 = 3 %).
    /// </para>
    /// </remarks>
    public sealed record SpeicherParameter
    {
        // ---------------------------------------------------------------- Technik

        /// <summary>Nennkapazitaet C_nom [kWh] (V7: J3).</summary>
        public double CNomKwh { get; init; }

        /// <summary>
        /// Gemeinsame Lade- und Entladeleistung P [kW] (V7: J4). Stufe 1 kennt
        /// bewusst nur eine Leistung fuer beide Richtungen.
        /// </summary>
        public double PKw { get; init; }

        /// <summary>Untere Grenze des nutzbaren Ladezustandsbands [kWh] (V7: J6).</summary>
        public double SoCMinKwh { get; init; }

        /// <summary>Obere Grenze des nutzbaren Ladezustandsbands [kWh] (V7: J7).</summary>
        public double SoCMaxKwh { get; init; }

        /// <summary>
        /// Round-Trip-Wirkungsgrad eta_RT [-], Default 0,90 (Fachkonzept 5.2).
        /// Intern symmetrisch aufgeteilt: eta_ch = eta_dis = sqrt(eta_RT).
        /// </summary>
        public double RoundTripWirkungsgrad { get; init; } = 0.90;

        /// <summary>
        /// Ladezustand zu Beginn der Simulation [kWh]. <c>null</c> bedeutet
        /// <see cref="SoCMinKwh"/>. Im Excel-Kompatibilitaetsmodus wird der Wert
        /// ignoriert; dort startet die Simulation immer bei 0 (VBA-Verhalten).
        /// </summary>
        public double? StartSoCKwh { get; init; }

        /// <summary>Intervalldauer dt [h], Default 0,25 (Viertelstundenraster).</summary>
        public double DtH { get; init; } = 0.25;

        // -------------------------------------------------------- Quellen-Matrix

        /// <summary>
        /// Betriebsart nach der Quellen-Matrix (Fachkonzept 2.1), Default
        /// <see cref="SpeicherBetriebsart.Gruenstrom"/>. Steuert allein den Netzpfad
        /// und wirkt deshalb nur in der <see cref="Arbitrage"/> (AP10); fuer die
        /// uebrigen Strategien ist sie reiner Ausweis.
        /// </summary>
        public SpeicherBetriebsart Betriebsart { get; init; } = SpeicherBetriebsart.Gruenstrom;

        /// <summary>
        /// PV-Ueberschuss ist zulaessige Ladequelle, Default <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Der Default haelt die Rueckwaertskompatibilitaet: ohne BHKW-Reihe und mit
        /// PV zulaessig rechnet der energetische Modus exakt wie vor AP2.
        /// </remarks>
        public bool PvZulaessig { get; init; } = true;

        /// <summary>
        /// BHKW-<b>Ueberschuss</b> ist zulaessige Ladequelle, Default <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Ohne BHKW-Reihe im <see cref="SpeicherEingang"/> ist der Ueberschuss
        /// konstant 0; das Flag ist dann wirkungslos. Die Gruen-Untervariante
        /// "nur PV" (Fachkonzept 2.1) setzt es ausdruecklich auf <c>false</c>.
        /// </remarks>
        public bool BhkwUeberschussZulaessig { get; init; } = true;

        /// <summary>
        /// Stromgefuehrtes BHKW-Nachladen zugelassen, Default <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <b>In diesem Arbeitspaket nur Flag, keine Logik.</b> Die Option setzt
        /// voraus, dass die anfallende Waerme abgenommen oder gepuffert wird, und
        /// haengt an der Projekteinstellung <c>Betriebsart = 1</c> (stromgefuehrt).
        /// Der Rechenzweig folgt in einem spaeteren Paket; bis dahin veraendert das
        /// Flag kein Ergebnis.
        /// </remarks>
        public bool BhkwStromgefuehrtZulaessig { get; init; }

        // ------------------------------------------------------- Wirtschaftlichkeit

        /// <summary>
        /// Kapazitaetsbezogene Investition c_cap [EUR/kWh] (V7: N5).
        /// Anmerkung zum Labelfehler der Mappe: die Zelle war mit "EUR/kW"
        /// beschriftet, wurde aber mit der Kapazitaet multipliziert; faktisch
        /// EUR/kWh (Fachkonzept 5.1).
        /// </summary>
        public double CCapEurProKwh { get; init; }

        /// <summary>Leistungsbezogene Investition c_pow [EUR/kW], Default 0.</summary>
        public double CPowEurProKw { get; init; }

        /// <summary>Leistungsunabhaengiger Investitionsanteil I_fix [EUR], Default 0.</summary>
        public double IFixEur { get; init; }

        /// <summary>Kalkulatorischer Kapitalzins i_z [-] (V7: N4).</summary>
        public double Kapitalzins { get; init; }

        /// <summary>Nutzungsdauer N [a] (V7: N7).</summary>
        public double NutzungsdauerA { get; init; }

        /// <summary>
        /// Kapazitaetsdegradation d [-] pro Jahr als Bruch (0,001 = 0,1 %/a, V7: J8).
        /// Wirkt ausschliesslich in der Wirtschaftlichkeitsprojektion (Fachkonzept 5.3),
        /// nicht in der Jahressimulation.
        /// </summary>
        public double DegradationProA { get; init; }

        /// <summary>
        /// Einspeiseverguetung [ct/kWh] (V7: J23). Bewertet die entgangene
        /// Verguetung fuer PV-Energie, die statt der Einspeisung in den Speicher geht.
        /// </summary>
        /// <remarks>
        /// Dies ist der <b>Standardwert</b>. Getrennte Verguetungsreihen v_pv[i] und
        /// v_bhkw[i] stehen im <see cref="SpeicherEingang"/>
        /// (<see cref="SpeicherEingang.VerguetungPvCtKwh"/> /
        /// <see cref="SpeicherEingang.VerguetungBhkwCtKwh"/>); fehlt eine davon, gilt
        /// dieser Wert fuer alle Intervalle.
        /// </remarks>
        public double VerguetungCtKwh { get; init; }

        /// <summary>
        /// Zyklus-Verschleisskosten c_ver [EUR/(kWh Nennkapazitaet * Vollzyklus)],
        /// Default 0,025 (Fachkonzept 5.4, Wert aus der V7-Mappe N2).
        /// </summary>
        /// <remarks>
        /// Wirkt in diesem Arbeitspaket ausschliesslich als Ausweis
        /// (<see cref="SpeicherKennzahlen.VerschleisskostenEurProA"/>) und geht weder
        /// in die Geldwertsumme noch in den Jahresueberschuss ein. Im
        /// Excel-Kompatibilitaetsmodus gilt per Definition c_ver = 0.
        /// </remarks>
        public double CVerEurProKwhZyklus { get; init; } = 0.025;

        /// <summary>
        /// Pauschaler Verlustfaktor [-] der V7-Mappe (J5), Default 0,1.
        /// <b>Wirkt ausschliesslich im Excel-Kompatibilitaetsmodus</b> als Abschlag
        /// auf die Euro-Jahressumme (<c>N10 = Summe(F) * (1 - Verlustfaktor)</c>).
        /// Im energetischen Modus wird er nicht angewendet, weil die Verluste dort
        /// bereits energetisch ueber eta_ch/eta_dis wirken - sonst waeren sie
        /// doppelt gezaehlt (Fachkonzept 5.2).
        /// </summary>
        public double VerlustfaktorPauschal { get; init; } = 0.1;

        // ------------------------------------------------------------- Abgeleitetes

        /// <summary>Ladewirkungsgrad eta_ch = sqrt(eta_RT) [-].</summary>
        public double EtaCh => Math.Sqrt(RoundTripWirkungsgrad);

        /// <summary>Entladewirkungsgrad eta_dis = sqrt(eta_RT) [-].</summary>
        public double EtaDis => Math.Sqrt(RoundTripWirkungsgrad);

        /// <summary>Tatsaechlicher Start-Ladezustand [kWh]: <see cref="StartSoCKwh"/> oder <see cref="SoCMinKwh"/>.</summary>
        public double StartSoCEffektivKwh => StartSoCKwh ?? SoCMinKwh;

        /// <summary>Nutzbare Kapazitaet C_nutz = SoC_max - SoC_min [kWh].</summary>
        public double CNutzKwh => SoCMaxKwh - SoCMinKwh;

        /// <summary>
        /// Investition I = c_cap*C_nom + c_pow*P + I_fix [EUR] (V7: N6 = J3*N5,
        /// dort ohne Leistungs- und Fixanteil).
        /// </summary>
        public double InvestitionEur => CCapEurProKwh * CNomKwh + CPowEurProKw * PKw + IFixEur;

        /// <summary>
        /// Prueft die Parameter auf Plausibilitaet und wirft bei Verstoss.
        /// Wird von den Strategien vor der Simulation aufgerufen.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Bei unbrauchbaren Werten.</exception>
        public void Pruefe()
        {
            if (!(DtH > 0.0))
                throw new ArgumentOutOfRangeException(nameof(DtH), DtH, "dt muss groesser 0 sein.");
            if (PKw < 0.0)
                throw new ArgumentOutOfRangeException(nameof(PKw), PKw, "Leistung darf nicht negativ sein.");
            if (SoCMaxKwh < SoCMinKwh)
                throw new ArgumentOutOfRangeException(nameof(SoCMaxKwh), SoCMaxKwh, "SoC_max muss groesser oder gleich SoC_min sein.");
            if (!(RoundTripWirkungsgrad > 0.0) || RoundTripWirkungsgrad > 1.0)
                throw new ArgumentOutOfRangeException(nameof(RoundTripWirkungsgrad), RoundTripWirkungsgrad, "eta_RT muss im Bereich (0..1] liegen.");
            if (CVerEurProKwhZyklus < 0.0)
                throw new ArgumentOutOfRangeException(nameof(CVerEurProKwhZyklus), CVerEurProKwhZyklus, "c_ver darf nicht negativ sein.");
        }
    }
}
