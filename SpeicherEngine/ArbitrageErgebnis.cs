using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Kennzahlen der Preissteuerung (Fachkonzept 6.5 / 7.1, Arbeitspaket AP10) -
    /// alles, was die Netzpfade zum <see cref="SpeicherKennzahlen"/>-Block hinzufuegen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ein eigener Satz.</b> <see cref="SpeicherKennzahlen"/> wird von jeder
    /// Strategie gefuellt und ist bitgenau referenzgeprueft; die Netzpfadgroessen gibt
    /// es nur bei der <see cref="Arbitrage"/>. Sie stehen deshalb additiv daneben statt
    /// als weitere Felder mit Dauerwert 0.
    /// </para>
    /// <para>
    /// Die vier Geldgroessen sind die Summanden der Bewertungszeile aus Fachkonzept
    /// 6.2 und ergeben zusammen wieder
    /// <see cref="SpeicherErgebnis.SummeGeldwertEur"/> - bis auf die
    /// Summationsreihenfolge, denn <c>Sigma F</c> wird sequenziell ueber die Intervalle
    /// gebildet (<see cref="Numerik.SummeSequenziell(double[])"/>).
    /// </para>
    /// </remarks>
    public sealed record ArbitrageKennzahlen
    {
        /// <summary>Leerer Satz - Vorbelegung fuer Laeufe ohne Netzpfade.</summary>
        public static readonly ArbitrageKennzahlen Leer = new ArbitrageKennzahlen();

        // ---------------------------------------------------------------- Energie

        /// <summary>Aus dem Netz geladene Energie [kWh/a] (AC-seitig, Fachkonzept 2.1 Graustrom).</summary>
        public double LadungNetzKwh { get; init; }

        /// <summary>Ins Netz verkaufte Energie [kWh/a] (AC-seitig, Entladeprioritaet 2 aus 2.2).</summary>
        public double VerkaufKwh { get; init; }

        /// <summary>
        /// DC-seitig entnommene Energie <b>einschliesslich Verkauf</b> [kWh/a] -
        /// Bezugsgroesse des Zyklenbudgets.
        /// </summary>
        public double EntladeenergieDcGesamtKwh { get; init; }

        // ----------------------------------------------------------------- Geld

        /// <summary>Vermiedener Netzbezug [EUR/a] - Summand 1 der Bewertungszeile 6.2.</summary>
        public double BezugsersparnisEur { get; init; }

        /// <summary>Entgangene Einspeiseverguetung [EUR/a], als positiver Abzugsbetrag gefuehrt.</summary>
        public double EntgangeneVerguetungEur { get; init; }

        /// <summary>Kosten der Netzladung [EUR/a], als positiver Abzugsbetrag gefuehrt.</summary>
        public double LadekostenEur { get; init; }

        /// <summary>Erloes aus dem Verkauf ins Netz [EUR/a].</summary>
        public double NetzerloesEur { get; init; }

        // -------------------------------------------------------------- Budget

        /// <summary>Jahres-Zyklenbudget DC [kWh/a]; 0 = unbegrenzt (Fachkonzept 5.4/6.5).</summary>
        public double ZyklenbudgetDcKwhProA { get; init; }

        /// <summary>
        /// Auslastung des Zyklenbudgets [%] - <c>EntladeenergieDcGesamtKwh /
        /// Zyklenbudget</c>. 0, wenn kein Budget gepflegt ist.
        /// </summary>
        public double BudgetauslastungProzent
            => ZyklenbudgetDcKwhProA > 0.0 ? 100.0 * EntladeenergieDcGesamtKwh / ZyklenbudgetDcKwhProA : 0.0;

        /// <summary>true, wenn die Planung endete, weil das Budget aufgebraucht war.</summary>
        public bool BudgetErschoepft { get; init; }

        // ------------------------------------------------------------ Planung

        /// <summary>Verschleiss je ausgespeicherter kWh k_ver [ct/kWh] (Fachkonzept 5.4).</summary>
        public double VerschleissCtKwh { get; init; }

        /// <summary>Angenommene Lade-/Entladepaarungen.</summary>
        public int PaareAngenommen { get; init; }

        /// <summary>Angenommene ungepaarte Verkaufsslots.</summary>
        public int VerkaufsslotsAngenommen { get; init; }

        /// <summary>An der Pfadpruefung gescheiterte Kandidaten.</summary>
        public int VerworfenPfad { get; init; }

        /// <summary>
        /// Summe der Betraege, um die der gefahrene Netzpfad vom Plan abweicht [kWh/a].
        /// </summary>
        /// <remarks>
        /// <b>Sollwert 0.</b> Der Planer hat jeden Plan gegen den vollstaendigen
        /// Ladezustandspfad geprueft; der Dispatch darf ihn deshalb nicht mehr
        /// beschneiden muessen. Ein Wert &gt; 0 waere der Beleg, dass Planer und
        /// Strategie auseinandergelaufen sind - genau das still hinzunehmen war der
        /// V7-Fehler G3.
        /// </remarks>
        public double AbweichungVomPlanKwh { get; init; }
    }

    /// <summary>
    /// Ergebnis eines Arbitrage-Laufs: der gewohnte
    /// <see cref="SpeicherEngine.SpeicherErgebnis"/> plus die Netzpfadreihen und der
    /// Plan, aus dem sie stammen (Arbeitspaket AP10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum die Netzpfade nicht in <see cref="SpeicherEngine.SpeicherErgebnis"/>
    /// stecken.</b> Dessen Reihen
    /// <see cref="SpeicherEngine.SpeicherErgebnis.LadungAcKwh"/> und
    /// <see cref="SpeicherEngine.SpeicherErgebnis.EntladungAcKwh"/> haben eine feste
    /// Bedeutung in der Simulationskette: Die Ladung mindert die <b>Einspeisung</b>,
    /// die Entladung den <b>Netzbezug</b>. Netzladung und Verkauf wirken genau
    /// umgekehrt - sie erhoehen Bezug bzw. Einspeisung. In dieselben Reihen gelegt,
    /// haetten sie jeden Aufrufer im Hauptprojekt still falsch rechnen lassen. Die
    /// beiden Bestandsreihen fuehren deshalb weiterhin <b>nur den
    /// Eigenverbrauchsfluss</b>; die Netzpfade stehen hier daneben. Dasselbe gilt fuer
    /// die Skalare <c>LadeenergieKwh</c> / <c>EntladeenergieKwh</c>.
    /// </para>
    /// <para>
    /// Ausnahme mit Absicht: <see cref="SpeicherKennzahlen.SpeicherverlusteKwh"/> wird
    /// von der Strategie ueber <b>alle vier</b> Pfade gebildet - sonst schloesse die
    /// Energiebilanz nicht. Ebenso enthalten Netzbezug und Einspeisung "mit Speicher"
    /// die Netzpfade.
    /// </para>
    /// </remarks>
    public sealed class ArbitrageErgebnis
    {
        /// <summary>Der Jahreslauf in der gewohnten Form.</summary>
        public SpeicherErgebnis Ergebnis { get; }

        /// <summary>Aus dem Netz geladene Energie je Intervall [kWh AC].</summary>
        public double[] LadungNetzAcKwh { get; }

        /// <summary>Ins Netz verkaufte Energie je Intervall [kWh AC].</summary>
        public double[] VerkaufAcKwh { get; }

        /// <summary>Der Fahrplan, den <see cref="ArbitragePlaner"/> erzeugt hat.</summary>
        public ArbitragePlan Plan { get; }

        /// <summary>Kennzahlen der Netzpfade.</summary>
        public ArbitrageKennzahlen Kennzahlen { get; }

        /// <summary>Erzeugt das Ergebnis. Wird ausschliesslich von <see cref="Arbitrage"/> aufgerufen.</summary>
        public ArbitrageErgebnis(
            SpeicherErgebnis ergebnis,
            double[] ladungNetzAcKwh,
            double[] verkaufAcKwh,
            ArbitragePlan plan,
            ArbitrageKennzahlen? kennzahlen)
        {
            Ergebnis = ergebnis ?? throw new ArgumentNullException(nameof(ergebnis));
            LadungNetzAcKwh = ladungNetzAcKwh ?? throw new ArgumentNullException(nameof(ladungNetzAcKwh));
            VerkaufAcKwh = verkaufAcKwh ?? throw new ArgumentNullException(nameof(verkaufAcKwh));
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            Kennzahlen = kennzahlen ?? ArbitrageKennzahlen.Leer;

            if (LadungNetzAcKwh.Length != ergebnis.Anzahl)
                throw new ArgumentException("Die Netzladereihe muss so lang sein wie der SoC-Verlauf.", nameof(ladungNetzAcKwh));
            if (VerkaufAcKwh.Length != ergebnis.Anzahl)
                throw new ArgumentException("Die Verkaufsreihe muss so lang sein wie der SoC-Verlauf.", nameof(verkaufAcKwh));
        }
    }
}
