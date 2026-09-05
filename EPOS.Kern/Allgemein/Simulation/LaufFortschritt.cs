namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Phasen eines Simulationslaufs — sprachneutrale ASCII-Schlüssel nach der
    /// Drei-Schichten-Regel (iU9-W11a.4).
    ///
    /// <para><b>Warum nur fünf.</b> Die Arbeitsanweisung nennt „je Erzeuger der
    /// Kaskade". Das gibt der Rechenweg nicht her: <c>Kaskade_Zweikanalig</c> läuft
    /// STUNDENWEISE über das Jahr und bedient in jeder Stunde alle Erzeuger nacheinander
    /// (Phasen A–G der <c>Kaskadenschleife</c>). Es gibt keinen Zeitpunkt, ab dem „die
    /// Wärmepumpe fertig" wäre. Eine Meldung je Erzeuger wäre deshalb entweder erfunden
    /// oder 8 760-mal je Erzeuger — beides schlechter als die ehrliche Phase.</para>
    /// </summary>
    public enum Laufphase
    {
        /// <summary>Der Lauf beginnt; Vorbereitung und Schemaprüfung.</summary>
        Start = 0,
        /// <summary>Die Kaskade rechnet — der weitaus längste Abschnitt.</summary>
        Kaskade = 1,
        /// <summary>Die Photovoltaik wird vom Reststrombedarf abgezogen.</summary>
        Photovoltaik = 2,
        /// <summary>Der Stromspeicher rechnet (SpeicherEngine).</summary>
        Stromspeicher = 3,
        /// <summary>Kennzahlen der Speicher, Restbilanzen, Aufräumen.</summary>
        Abschluss = 4
    }

    /// <summary>
    /// Eine Fortschrittsmeldung des Simulationslaufs.
    /// </summary>
    /// <param name="Phase">Welcher Abschnitt läuft.</param>
    /// <param name="Anteil">
    /// Fortschritt 0…1. Er ist eine SCHÄTZUNG über die Phasenfolge, keine Messung: Die
    /// Kaskade meldet ihren Beginn, nicht ihren Verlauf. Eine Oberfläche, die genauer
    /// sein will, zeigt einen unbestimmten Balken.
    /// </param>
    /// <param name="Text">
    /// Ein bereits übersetzter Anzeigetext, oder <c>null</c>. Der Kern setzt ihn nicht —
    /// die Beschriftung der Phase gehört zur Oberfläche, die Phase selbst hierher.
    /// </param>
    public sealed record LaufFortschritt(Laufphase Phase, double Anteil, string Text = null);
}
