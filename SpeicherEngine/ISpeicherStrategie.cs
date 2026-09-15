namespace SpeicherEngine
{
    /// <summary>
    /// Betriebsstrategie eines Speichers (Fachkonzept 6):
    /// <see cref="Dauernutzung"/>, <see cref="PeakShaving"/> und
    /// <see cref="Arbitrage"/>.
    /// </summary>
    /// <remarks>
    /// Implementierungen muessen zustandslos oder unveraenderlich sein und duerfen
    /// weder Eingang noch Parameter veraendern. Nur so ist die Rastersuche der
    /// Auslegungsoptimierung ueber <c>Parallel.For</c> zulaessig (Fachkonzept 8.1).
    /// </remarks>
    public interface ISpeicherStrategie
    {
        /// <summary>Name der Strategie fuer Anzeige und Protokoll.</summary>
        string Name { get; }

        /// <summary>Rechnet einen Jahreslauf.</summary>
        /// <param name="eingang">Zeitreihen Last, Erzeugung, Preis.</param>
        /// <param name="p">Speicher- und Wirtschaftlichkeitsparameter.</param>
        SpeicherErgebnis Berechne(SpeicherEingang eingang, SpeicherParameter p);
    }
}
