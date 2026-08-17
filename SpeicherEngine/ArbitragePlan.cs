using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Fahrplan der Netzpfade fuer ein ganzes Jahr, erzeugt von
    /// <see cref="ArbitragePlaner"/> (Fachkonzept 6.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die beiden Reihen sind <b>Obergrenzen je Intervall</b>, keine Sollwerte: Der
    /// Dispatch der <see cref="Arbitrage"/> begrenzt zusaetzlich auf Leistungsgrenze
    /// und SoC-Band, genau wie im Pseudocode 6.2. Weil der Planer den kompletten
    /// Ladezustandspfad des Fensters vor jeder Uebernahme geprueft hat, ist die
    /// Obergrenze aus dem Plan die bindende - die uebrigen Schranken greifen nicht mehr.
    /// Ob das tatsaechlich so war, weist
    /// <see cref="ArbitrageKennzahlen.AbweichungVomPlanKwh"/> nach (Sollwert 0).
    /// </para>
    /// <para>
    /// Die Zaehler daneben sind Protokollgroessen: Sie belegen, wie viele Paarungen
    /// angenommen und wie viele an der Pfadpruefung gescheitert sind - der Nachweis,
    /// dass nicht still geklemmt wurde (V7-Fehler G3).
    /// </para>
    /// </remarks>
    public sealed class ArbitragePlan
    {
        /// <summary>Obergrenze der Netzladung je Intervall [kWh AC].</summary>
        public double[] NetzladungAcKwh { get; }

        /// <summary>Obergrenze des Netzverkaufs je Intervall [kWh AC].</summary>
        public double[] VerkaufAcKwh { get; }

        /// <summary>Anzahl der geplanten 24-h-Fenster.</summary>
        public int Fensteranzahl { get; }

        /// <summary>Angenommene Lade-/Entladepaarungen (Fachkonzept 6.5).</summary>
        public int PaareAngenommen { get; }

        /// <summary>
        /// Angenommene <b>ungepaarte</b> Verkaufsslots aus vorhandenem Ladezustand -
        /// der Gruenstromfall (Fachkonzept 2.1).
        /// </summary>
        public int VerkaufsslotsAngenommen { get; }

        /// <summary>
        /// Verworfene Kandidaten, weil der gepruefte Ladezustandspfad des Fensters
        /// unzulaessig geworden waere oder der Eigenverbrauch darunter gelitten haette.
        /// </summary>
        public int VerworfenPfad { get; }

        /// <summary>
        /// Verworfene Kandidaten, weil die zulaessige Energiemenge auf 0 geschrumpft
        /// war (kein SoC-Kopf, kein Band oder kein Budget mehr).
        /// </summary>
        public int VerworfenOhneEnergie { get; }

        /// <summary>Summe der geplanten Netzladung [kWh AC].</summary>
        public double GeplanteNetzladungKwh { get; }

        /// <summary>Summe des geplanten Verkaufs [kWh AC].</summary>
        public double GeplanterVerkaufKwh { get; }

        /// <summary>Jahres-Zyklenbudget DC [kWh/a], das der Planung zugrunde lag; 0 = unbegrenzt.</summary>
        public double ZyklenbudgetDcKwhProA { get; }

        /// <summary>
        /// true, wenn die Planung endete, weil das Zyklenbudget aufgebraucht war
        /// (Fachkonzept 6.5). Der Eigenverbrauchsfluss laeuft danach unveraendert
        /// weiter - er wird nicht geplant, sondern hat Vorrang.
        /// </summary>
        public bool BudgetErschoepft { get; }

        /// <summary>
        /// Verschleiss je ausgespeicherter kWh k_ver [ct/kWh], mit dem die
        /// Rentabilitaetsbedingung gerechnet wurde (Fachkonzept 5.4) - Ausweis, damit
        /// die Schwelle im Protokoll nachvollziehbar ist.
        /// </summary>
        public double VerschleissCtKwh { get; }

        /// <summary>Anzahl der Intervalle n.</summary>
        public int Anzahl => NetzladungAcKwh.Length;

        /// <summary>Erzeugt den Plan. Wird ausschliesslich vom <see cref="ArbitragePlaner"/> aufgerufen.</summary>
        public ArbitragePlan(
            double[] netzladungAcKwh,
            double[] verkaufAcKwh,
            int fensteranzahl,
            int paareAngenommen,
            int verkaufsslotsAngenommen,
            int verworfenPfad,
            int verworfenOhneEnergie,
            double zyklenbudgetDcKwhProA,
            bool budgetErschoepft,
            double verschleissCtKwh)
        {
            NetzladungAcKwh = netzladungAcKwh ?? throw new ArgumentNullException(nameof(netzladungAcKwh));
            VerkaufAcKwh = verkaufAcKwh ?? throw new ArgumentNullException(nameof(verkaufAcKwh));
            if (VerkaufAcKwh.Length != NetzladungAcKwh.Length)
                throw new ArgumentException("Lade- und Verkaufsreihe muessen gleich lang sein.", nameof(verkaufAcKwh));

            Fensteranzahl = fensteranzahl;
            PaareAngenommen = paareAngenommen;
            VerkaufsslotsAngenommen = verkaufsslotsAngenommen;
            VerworfenPfad = verworfenPfad;
            VerworfenOhneEnergie = verworfenOhneEnergie;
            ZyklenbudgetDcKwhProA = zyklenbudgetDcKwhProA;
            BudgetErschoepft = budgetErschoepft;
            VerschleissCtKwh = verschleissCtKwh;

            GeplanteNetzladungKwh = Numerik.SummeSequenziell(NetzladungAcKwh);
            GeplanterVerkaufKwh = Numerik.SummeSequenziell(VerkaufAcKwh);
        }

        /// <summary>Leerer Plan der Laenge <paramref name="n"/> - keine Netzpfade.</summary>
        public static ArbitragePlan Leer(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            return new ArbitragePlan(new double[n], new double[n], 0, 0, 0, 0, 0, 0.0, false, 0.0);
        }
    }
}
