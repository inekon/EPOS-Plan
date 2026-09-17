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
        /// LADEDECKEL [kW]: hoechste Netzlast, die das LADEN erzeugen darf.
        /// <c>null</c> heisst "kein eigener Deckel" - dann gilt wie bisher die
        /// Zielschwelle selbst, und der Bestand rechnet unveraendert.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Das Feld trennt die beiden Rollen der Schwelle, die bis dahin eine waren:
        /// ENTLADEN geschieht oberhalb <see cref="PZielKw"/> (bzw. der nachgezogenen
        /// Schwelle), LADEN nur bis <c>min(Ziel, Ladedeckel)</c>. Zwei Faelle des
        /// Einzelspeichers im Projektlauf brauchen genau das:
        /// </para>
        /// <list type="bullet">
        ///   <item><description><b>Gruenstrom</b> - Deckel 0: Geladen wird
        ///     ausschliesslich aus Erzeugungsueberschuss, also nur, solange die
        ///     Netzlast negativ ist. Ohne Deckel lud der Speicher aus dem Netz,
        ///     weil jede Last unterhalb der Schwelle Luft zum Laden laesst.</description></item>
        ///   <item><description><b>Ziel ueber der Bezugsspitze</b> (LS-E-3) - Deckel
        ///     bei der Referenzspitze: Ein zu hoch gewaehltes Ziel darf die Spitze
        ///     nicht ANHEBEN. Ohne Deckel lud der Speicher bis zum Ziel und schuefe
        ///     damit eine neue, hoehere Spitze.</description></item>
        /// </list>
        /// <para>
        /// Auf das ENTLADEN wirkt der Deckel nicht: Er ist eine Schranke des
        /// Ladepfads, keine zweite Zielschwelle.
        /// </para>
        /// </remarks>
        public double? LadedeckelKw { get; init; }

        /// <summary>
        /// Der Parametersatz der NACHZIEHENDEN Schwelle - P_ziel = 0 und
        /// <see cref="Adaptiv"/>, also genau die Betriebsweise, mit der die
        /// Peak-Shaving-Maske rechnet.
        /// </summary>
        /// <remarks>
        /// Anwenderentscheid W11b-E-3 (10.09.2026): Die Lastspitzenkappung ist seither
        /// auch eine Berechnungsart der Auslegungsoptimierung. Beide Einstiege muessen
        /// dieselbe Betriebsweise rechnen, sonst waere der Bestpunkt der Rastersuche in
        /// der eigenen Maske nicht wiederzufinden - der Parametersatz entsteht deshalb
        /// an EINER Stelle. Eine feste Zielschwelle kann die Rastersuche ohnehin nicht
        /// brauchen: Jeder Rasterpunkt hat eine andere Auslegung und damit eine andere
        /// haltbare Spitze.
        /// </remarks>
        /// <param name="leistungspreisEurProKwA">Leistungspreis L_P [EUR/(kW*a)].</param>
        /// <param name="bezugspreisMittelCtKwh">Mittlerer Bezugspreis [ct/kWh].</param>
        public static PeakShavingParameter Nachziehend(double leistungspreisEurProKwA,
                                                       double bezugspreisMittelCtKwh)
        {
            return new PeakShavingParameter
            {
                PZielKw = 0.0,
                Adaptiv = true,
                LeistungspreisEurProKwA = leistungspreisEurProKwA,
                BezugspreisMittelCtKwh = bezugspreisMittelCtKwh
            };
        }

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
            if (LadedeckelKw.HasValue && LadedeckelKw.Value < 0.0)
                throw new ArgumentOutOfRangeException(nameof(LadedeckelKw), LadedeckelKw,
                    "Der Ladedeckel darf nicht negativ sein.");
            if (LeistungspreisEurProKwA < 0.0)
                throw new ArgumentOutOfRangeException(nameof(LeistungspreisEurProKwA), LeistungspreisEurProKwA,
                    "Der Leistungspreis darf nicht negativ sein.");
        }
    }
}
