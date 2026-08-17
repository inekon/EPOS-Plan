using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Ergebnis einer Speichersimulation: Zeitreihen, Energiesummen und der
    /// Wirtschaftlichkeitsblock.
    /// </summary>
    /// <remarks>
    /// Die Instanz wird von der Strategie vollstaendig gefuellt und danach nicht
    /// mehr veraendert. Die Arrays gehoeren dem Ergebnis; der Aufrufer darf sie
    /// lesen, aber nicht veraendern.
    /// </remarks>
    public sealed class SpeicherErgebnis
    {
        /// <summary>Ladezustand am Ende des jeweiligen Intervalls [kWh] (V7-Blattspalte E).</summary>
        public double[] SoCKwh { get; }

        /// <summary>Geldwert des jeweiligen Intervalls [EUR] (V7-Blattspalte F).</summary>
        public double[] GeldwertEur { get; }

        /// <summary>
        /// Sequenzielle Summe ueber <see cref="GeldwertEur"/> [EUR/a]
        /// (V7: <c>SUM(F...)</c>, vor dem pauschalen Verlustabschlag).
        /// </summary>
        public double SummeGeldwertEur { get; }

        /// <summary>
        /// AC-seitig in den Speicher geladene Energie ueber alle Intervalle [kWh].
        /// Im energetischen Modus vor Ladeverlust, also groesser als der SoC-Zuwachs.
        /// </summary>
        public double LadeenergieKwh { get; }

        /// <summary>
        /// AC-seitig aus dem Speicher entnommene Energie ueber alle Intervalle [kWh].
        /// Im energetischen Modus nach Entladeverlust, also kleiner als die SoC-Abnahme.
        /// </summary>
        public double EntladeenergieKwh { get; }

        /// <summary>
        /// AC-seitig geladene Energie je Intervall [kWh] - die Intervallaufloesung von
        /// <see cref="LadeenergieKwh"/>.
        /// </summary>
        /// <remarks>
        /// Gebraucht vom Hauptprojekt (AP2b), um die Speicherwirkung in die
        /// Simulationskette einzubetten: Die Ladung speist sich aus dem Erzeugungs-
        /// ueberschuss und mindert die <b>Einspeisung</b>, nicht den Netzbezug. Nie
        /// <c>null</c> und immer so lang wie <see cref="SoCKwh"/>; ohne Angabe ein
        /// Nullvektor.
        /// </remarks>
        public double[] LadungAcKwh { get; }

        /// <summary>
        /// AC-seitig entnommene Energie je Intervall [kWh] - die Intervallaufloesung von
        /// <see cref="EntladeenergieKwh"/>.
        /// </summary>
        /// <remarks>
        /// Gegenstueck zu <see cref="LadungAcKwh"/>: Um diesen Betrag sinkt der
        /// Netzbezug des Intervalls. Laden und Entladen schliessen einander je
        /// Intervall aus (<see cref="Vorverarbeitung"/>), es ist also immer hoechstens
        /// eine der beiden Reihen belegt.
        /// </remarks>
        public double[] EntladungAcKwh { get; }

        /// <summary>Rechenmodus, mit dem das Ergebnis entstanden ist.</summary>
        public SpeicherModus Modus { get; }

        /// <summary>Wirtschaftlichkeitsblock zu diesem Lauf.</summary>
        public WirtschaftlichkeitErgebnis Wirtschaftlichkeit { get; }

        /// <summary>
        /// Energetische Kennzahlen: Ladeenergie je Quelle, Vollzyklen,
        /// Verschleissausweis und Jahresbilanz (Fachkonzept 5.4 / 7.1).
        /// Nie <c>null</c> - ohne Angabe <see cref="SpeicherKennzahlen.Leer"/>.
        /// </summary>
        public SpeicherKennzahlen Kennzahlen { get; }

        /// <summary>Anzahl der Intervalle n.</summary>
        public int Anzahl => SoCKwh.Length;

        /// <summary>Erzeugt ein Ergebnis. Wird ausschliesslich von den Strategien aufgerufen.</summary>
        /// <exception cref="ArgumentException">
        /// Wenn eine der Intervallreihen nicht so lang ist wie <paramref name="soCKwh"/>.
        /// </exception>
        public SpeicherErgebnis(
            double[] soCKwh,
            double[] geldwertEur,
            double summeGeldwertEur,
            double ladeenergieKwh,
            double entladeenergieKwh,
            SpeicherModus modus,
            WirtschaftlichkeitErgebnis wirtschaftlichkeit,
            SpeicherKennzahlen? kennzahlen = null,
            double[]? ladungAcKwh = null,
            double[]? entladungAcKwh = null)
        {
            Kennzahlen = kennzahlen ?? SpeicherKennzahlen.Leer;
            SoCKwh = soCKwh ?? throw new ArgumentNullException(nameof(soCKwh));
            GeldwertEur = geldwertEur ?? throw new ArgumentNullException(nameof(geldwertEur));
            SummeGeldwertEur = summeGeldwertEur;
            LadeenergieKwh = ladeenergieKwh;
            EntladeenergieKwh = entladeenergieKwh;
            Modus = modus;
            Wirtschaftlichkeit = wirtschaftlichkeit ?? throw new ArgumentNullException(nameof(wirtschaftlichkeit));

            // Die Intervallreihen sind nachgeruestet (AP2b) und deshalb optional - ein
            // Ergebnis ohne sie fuehrt Nullvektoren statt null, damit kein Aufrufer
            // pruefen muss.
            if (ladungAcKwh != null && ladungAcKwh.Length != soCKwh.Length)
                throw new ArgumentException("Die Ladereihe muss so lang sein wie der SoC-Verlauf.", nameof(ladungAcKwh));
            if (entladungAcKwh != null && entladungAcKwh.Length != soCKwh.Length)
                throw new ArgumentException("Die Entladereihe muss so lang sein wie der SoC-Verlauf.", nameof(entladungAcKwh));

            LadungAcKwh = ladungAcKwh ?? new double[soCKwh.Length];
            EntladungAcKwh = entladungAcKwh ?? new double[soCKwh.Length];
        }
    }
}
