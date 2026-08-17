using System;
using System.Collections.Generic;

namespace SpeicherEngine
{
    /// <summary>
    /// Lastspitze eines Kalendermonats vor und nach der Kappung (Fachkonzept 6.4,
    /// offener Punkt 4: monatliche Auswertung als <b>Option</b> neben dem
    /// Jahresmaximum).
    /// </summary>
    public sealed record Monatsspitze
    {
        /// <summary>Kalendermonat 1 .. 12.</summary>
        public int Monat { get; init; }

        /// <summary>Hoechste Viertelstundenleistung des Monats ohne Speicher [kW].</summary>
        public double PAltMaxKw { get; init; }

        /// <summary>Hoechste Viertelstundenleistung des Monats mit Speicher [kW].</summary>
        public double PNeuMaxKw { get; init; }

        /// <summary>Anzahl der Intervalle, die diesem Monat zugeordnet sind.</summary>
        public int Intervalle { get; init; }

        /// <summary>Kappung des Monats [kW].</summary>
        public double KappungKw => PAltMaxKw - PNeuMaxKw;
    }

    /// <summary>
    /// Ergebnis eines Peak-Shaving-Laufs (Fachkonzept 6.4 und 7.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Instanz wird von <see cref="PeakShaving"/> vollstaendig gefuellt und
    /// danach nicht mehr veraendert. Die Arrays gehoeren dem Ergebnis; der Aufrufer
    /// darf sie lesen, aber nicht veraendern.
    /// </para>
    /// <para>
    /// <b>Verhaeltnis zu <see cref="SpeicherErgebnis"/>.</b> Peak-Shaving ist
    /// technisch eine Strategie derselben Engine; <see cref="Basis"/> haelt deshalb
    /// das gemeinsame Ergebnisformat (SoC-Verlauf, Lade- und Entladereihen,
    /// Kennzahlen, Wirtschaftlichkeitsblock). Die Groessen, die nur die
    /// Lastspitzenkappung kennt - gekappter Lastgang, Spitzen, Schwelle,
    /// Monatsspitzen und die Monetarisierung nach 6.4 - stehen hier daneben.
    /// Haeufig gebrauchte Werte aus <see cref="Basis"/> sind durchgereicht.
    /// </para>
    /// <para>
    /// <b>Zur Geldwertreihe.</b> <c>Basis.GeldwertEur[i]</c> traegt ausschliesslich
    /// die intervallaufgeloeste Bewertung der Verschiebeverluste
    /// (<c>-(Ladung[i] - Entladung[i]) * p_bezug,mittel / 100</c>) und summiert sich
    /// deshalb auf <c>-<see cref="VerlustkostenEur"/></c>. Der erste Term der
    /// Monetarisierung - die Leistungspreisersparnis - ist eine <b>Jahresgroesse</b>
    /// und hat kein Intervallgegenstueck; er steckt nur in
    /// <see cref="ErtragPsEur"/>, und dieser Wert ist es auch, der als E_a,1 in
    /// <c>Basis.Wirtschaftlichkeit</c> eingeht.
    /// </para>
    /// </remarks>
    public sealed class PeakShavingErgebnis
    {
        /// <summary>Gemeinsames Ergebnisformat der Engine (SoC, Reihen, Wirtschaftlichkeit).</summary>
        public SpeicherErgebnis Basis { get; }

        /// <summary>Lastgang ohne Speicher P_alt [kW] je Intervall (Kopie der Eingangsreihe).</summary>
        public double[] PAltKw { get; }

        /// <summary>Gekappter Lastgang P_neu [kW] je Intervall (Fachkonzept 6.4).</summary>
        public double[] PNeuKw { get; }

        /// <summary>
        /// Lastspitze ohne Speicher [kW]. <b>Default nach offenem Punkt 4:
        /// Jahresmaximum der Viertelstundenleistung</b>, also
        /// <c>max(P_alt)</c> ueber alle Intervalle.
        /// </summary>
        public double PAltMaxKw { get; }

        /// <summary>Lastspitze mit Speicher [kW]: <c>max(P_neu)</c>.</summary>
        public double PNeuMaxKw { get; }

        /// <summary>
        /// Am Ende des Laufs gueltige Schwelle [kW]. Im festen Modus die Vorgabe
        /// <see cref="PeakShavingParameter.PZielKw"/>, im adaptiven Modus die
        /// nachgezogene Schwelle - dort gilt konstruktionsbedingt
        /// <c>ErreichteSchwelleKw == <see cref="PNeuMaxKw"/></c>.
        /// </summary>
        /// <remarks>
        /// Der adaptive Wert ist eine haltbare obere Schranke, nicht zwingend die
        /// minimal erreichbare Spitze; siehe
        /// <see cref="PeakShaving.MinimaleSchwelleKw"/>.
        /// </remarks>
        public double ErreichteSchwelleKw { get; }

        /// <summary>
        /// <c>true</c>, wenn im festen Modus <c>P_neu_max &gt; P_ziel</c> bleibt - der
        /// Speicher ist fuer die Zielschwelle zu klein (Fachkonzept 6.4). Im adaptiven
        /// Modus immer <c>false</c>.
        /// </summary>
        public bool SchwelleGerissen { get; }

        /// <summary>
        /// Monatsspitzen vor und nach Kappung. Option nach offenem Punkt 4; die
        /// Zuordnung setzt eine Reihe voraus, die am 1. Januar beginnt
        /// (siehe <see cref="PeakShaving.Monatsspitzen"/>).
        /// </summary>
        public IReadOnlyList<Monatsspitze> Monatsspitzen { get; }

        /// <summary>
        /// Leistungspreisersparnis <c>(P_alt_max - P_neu_max) * L_P</c> [EUR/a] -
        /// erster Term der Monetarisierung nach Fachkonzept 6.4.
        /// </summary>
        public double LeistungspreisersparnisEur { get; }

        /// <summary>
        /// Bewertung der Verschiebeverluste
        /// <c>(E_lade - E_entlade) * p_bezug,mittel / 100</c> [EUR/a] - zweiter Term
        /// der Monetarisierung nach Fachkonzept 6.4.
        /// </summary>
        /// <remarks>
        /// Die Differenz der AC-Energien enthaelt neben den Umwandlungsverlusten auch
        /// die am Jahresende im Speicher verbliebene Energie
        /// (<c>SoC_Ende - SoC_Start</c>). Das ist die Formel des Fachkonzepts und
        /// bewusst nicht bereinigt; die reinen Umwandlungsverluste stehen getrennt in
        /// <see cref="SpeicherverlusteKwh"/>.
        /// </remarks>
        public double VerlustkostenEur { get; }

        /// <summary>
        /// <c>Ertrag_PS = Leistungspreisersparnis - Verlustkosten</c> [EUR/a]
        /// (Fachkonzept 6.4). Geht als E_a,1 in
        /// <c>Basis.Wirtschaftlichkeit</c> ein.
        /// </summary>
        public double ErtragPsEur { get; }

        // ------------------------------------------------------------ Durchreichung

        /// <summary>Ladezustand am Ende des jeweiligen Intervalls [kWh].</summary>
        public double[] SoCKwh => Basis.SoCKwh;

        /// <summary>AC-seitig geladene Energie je Intervall [kWh].</summary>
        public double[] LadungAcKwh => Basis.LadungAcKwh;

        /// <summary>AC-seitig entnommene Energie je Intervall [kWh].</summary>
        public double[] EntladungAcKwh => Basis.EntladungAcKwh;

        /// <summary>AC-seitig geladene Jahresenergie [kWh/a].</summary>
        public double LadeenergieKwh => Basis.LadeenergieKwh;

        /// <summary>AC-seitig entnommene Jahresenergie [kWh/a].</summary>
        public double EntladeenergieKwh => Basis.EntladeenergieKwh;

        /// <summary>
        /// Umwandlungsverluste [kWh/a] nach der Konvention aus
        /// <see cref="SpeicherKennzahlen.SpeicherverlusteKwh"/>:
        /// <c>E_lade - E_entlade - (SoC_Ende - SoC_Start)</c>.
        /// </summary>
        public double SpeicherverlusteKwh => Basis.Kennzahlen.SpeicherverlusteKwh;

        /// <summary>Energetische Kennzahlen des Laufs.</summary>
        public SpeicherKennzahlen Kennzahlen => Basis.Kennzahlen;

        /// <summary>Wirtschaftlichkeitsblock mit E_a,1 = <see cref="ErtragPsEur"/>.</summary>
        public WirtschaftlichkeitErgebnis Wirtschaftlichkeit => Basis.Wirtschaftlichkeit;

        /// <summary>Rechenmodus, mit dem das Ergebnis entstanden ist.</summary>
        public SpeicherModus Modus => Basis.Modus;

        /// <summary>Kappung der Jahresspitze <c>P_alt_max - P_neu_max</c> [kW].</summary>
        public double KappungKw => PAltMaxKw - PNeuMaxKw;

        /// <summary>Anzahl der Intervalle n.</summary>
        public int Anzahl => PNeuKw.Length;

        /// <summary>Erzeugt ein Ergebnis. Wird ausschliesslich von <see cref="PeakShaving"/> aufgerufen.</summary>
        /// <exception cref="ArgumentException">Wenn die Reihen unterschiedlich lang sind.</exception>
        public PeakShavingErgebnis(
            SpeicherErgebnis basis,
            double[] pAltKw,
            double[] pNeuKw,
            double pAltMaxKw,
            double pNeuMaxKw,
            double erreichteSchwelleKw,
            bool schwelleGerissen,
            IReadOnlyList<Monatsspitze> monatsspitzen,
            double leistungspreisersparnisEur,
            double verlustkostenEur,
            double ertragPsEur)
        {
            Basis = basis ?? throw new ArgumentNullException(nameof(basis));
            PAltKw = pAltKw ?? throw new ArgumentNullException(nameof(pAltKw));
            PNeuKw = pNeuKw ?? throw new ArgumentNullException(nameof(pNeuKw));
            Monatsspitzen = monatsspitzen ?? throw new ArgumentNullException(nameof(monatsspitzen));

            if (pNeuKw.Length != pAltKw.Length)
                throw new ArgumentException("Gekappter und urspruenglicher Lastgang muessen gleich lang sein.", nameof(pNeuKw));
            if (basis.Anzahl != pAltKw.Length)
                throw new ArgumentException("Der SoC-Verlauf muss so lang sein wie der Lastgang.", nameof(basis));

            PAltMaxKw = pAltMaxKw;
            PNeuMaxKw = pNeuMaxKw;
            ErreichteSchwelleKw = erreichteSchwelleKw;
            SchwelleGerissen = schwelleGerissen;
            LeistungspreisersparnisEur = leistungspreisersparnisEur;
            VerlustkostenEur = verlustkostenEur;
            ErtragPsEur = ertragPsEur;
        }
    }
}
