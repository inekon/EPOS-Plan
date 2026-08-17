using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Steuer- und Bewertungsgroessen der Strategie (d) Peak-Shaving
    /// (Fachkonzept 6.4). Ergaenzt <see cref="SpeicherParameter"/> um genau die
    /// Angaben, die nur die Lastspitzenkappung kennt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Der Typ ist ein <c>record</c> mit ausschliesslich <c>init</c>-Settern und
    /// damit nach der Konstruktion unveraenderlich - dieselbe Voraussetzung wie bei
    /// <see cref="SpeicherParameter"/>, damit eine Rastersuche die Instanz gefahrlos
    /// ueber <c>Parallel.For</c> verteilen kann (Fachkonzept 8.1). Varianten werden
    /// ueber <c>parameter with { PZielKw = ... }</c> gebildet.
    /// </para>
    /// <para>
    /// Einheiten: Leistung [kW], Leistungspreis [EUR/(kW*a)], Arbeitspreis [ct/kWh].
    /// </para>
    /// </remarks>
    public sealed record PeakShavingParameter
    {
        /// <summary>
        /// Zielschwelle P_ziel [kW], auf die der Netzbezug gekappt werden soll.
        /// Wirkt nur, wenn <see cref="Adaptiv"/> <c>false</c> ist.
        /// </summary>
        public double PZielKw { get; init; }

        /// <summary>
        /// Adaptive Schwellensuche. <c>true</c> startet bei P_ziel = 0 und zieht die
        /// Schwelle nur so weit nach, wie der Speicher sie nicht halten kann; das
        /// Ergebnis steht danach in
        /// <see cref="PeakShavingErgebnis.ErreichteSchwelleKw"/> (Fachkonzept 6.4).
        /// <c>false</c> rechnet gegen die feste Vorgabe <see cref="PZielKw"/>.
        /// </summary>
        /// <remarks>
        /// <b>Achtung:</b> Das Verfahren ist ein einstufiges Greedy und liefert eine
        /// <b>haltbare obere Schranke</b>, nicht zwingend die minimal erreichbare
        /// Spitze - am Referenzlastgang liegt es 121,44 kW darueber. Wer die
        /// tatsaechliche Untergrenze braucht, nimmt
        /// <see cref="PeakShaving.MinimaleSchwelleKw"/> und rechnet damit im festen
        /// Modus. Der adaptive Modus bleibt unveraendert, weil er die verifizierte
        /// Vorlage abbildet.
        /// </remarks>
        public bool Adaptiv { get; init; }

        /// <summary>
        /// Leistungspreis L_P [EUR/(kW*a)] (Fachkonzept 4.4).
        /// </summary>
        /// <remarks>
        /// Bewusst ein <b>eigenes</b> Feld mit explizit deklarierter Einheit: das
        /// vorhandene Feld <c>energy_price.leistungspreis</c> des Kostenmoduls fuehrt
        /// keine durchgesetzte Einheitensemantik (das UI-Label sagt "EUR/kWh", die
        /// Auslese-Eigenschaft heisst <c>LeistungspreisEurYear</c>). Es darf L_P
        /// vorbelegen, aber nicht definieren (Fachkonzept 4.4, offener Punkt 3).
        /// </remarks>
        public double LeistungspreisEurProKwA { get; init; }

        /// <summary>
        /// Mittlerer Bezugspreis p_bezug,mittel [ct/kWh] zur Bewertung der
        /// Umwandlungsverluste (Fachkonzept 6.4, zweiter Term der Monetarisierung).
        /// </summary>
        /// <remarks>
        /// Peak-Shaving verschiebt Energie nur und verliert dabei; der Verlust wird
        /// mit dem mittleren Bezugspreis bewertet, weil die zusaetzlich bezogene
        /// Energie zum Vollpreis eingekauft wird. Eine zeitaufgeloeste Preisreihe ist
        /// hier bewusst nicht vorgesehen - die Steuergroesse der Strategie ist die
        /// Lastschwelle, nicht der Preis.
        /// </remarks>
        public double BezugspreisMittelCtKwh { get; init; }

        /// <summary>
        /// Prueft die Parameter auf Plausibilitaet und wirft bei Verstoss.
        /// Wird von <see cref="PeakShaving"/> vor der Simulation aufgerufen.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Bei unbrauchbaren Werten.</exception>
        public void Pruefe()
        {
            if (!Adaptiv && PZielKw < 0.0)
                throw new ArgumentOutOfRangeException(nameof(PZielKw), PZielKw,
                    "Die feste Zielschwelle darf nicht negativ sein.");
            if (LeistungspreisEurProKwA < 0.0)
                throw new ArgumentOutOfRangeException(nameof(LeistungspreisEurProKwA), LeistungspreisEurProKwA,
                    "Der Leistungspreis darf nicht negativ sein.");
        }
    }
}
