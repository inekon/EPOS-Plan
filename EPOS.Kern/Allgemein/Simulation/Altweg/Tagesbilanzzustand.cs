namespace WindowsFormsApplication1.Altweg
{
    /// <summary>
    /// Der Zustand des Tagesbilanz-Wegs (<see cref="TagesbilanzPhysik.TaeglHeizlastWG"/>): die
    /// <b>Vortemperatur</b> des Kapazitätsmodells, die über die 24 Stunden eines Tages UND
    /// über aufeinanderfolgende Tagesaufrufe hinweg mitgeführt wird.
    ///
    /// <para><b>Warum eine Instanz und kein statisches Feld.</b> Die native DLL hielt die
    /// Vortemperatur in einer globalen Variablen (0x4211F8). Ein statisches Feld trüge sie
    /// über Gebäude, Projekte und Rechenläufe eines Prozesses hinweg: Gebäude 2 begänne
    /// seinen Vorlauf mit der Raumtemperatur, die Gebäude 1 am 31.12. hinterlassen hat, und
    /// zwei Rechnungen in verschiedenen Fäden beschrieben sich gegenseitig. Der Rechenweg
    /// (<see cref="TagesbilanzRechenweg"/>) hält den Zustand je Instanz und setzt ihn je
    /// Aufruf über <see cref="ResetState"/> zurück.</para>
    ///
    /// <para><b>Ergebnisneutral.</b> Der Jahreslauf beginnt mit Tag 1, und dort setzt
    /// <see cref="TagesbilanzPhysik.TaeglHeizlastWG"/> die Vortemperatur ohnehin auf den
    /// Nachtsollwert; die Heizlasten des Vorlaufs (Tage 351–365) überschreibt der Jahreslauf.
    /// Der Startwert des Vorlaufs erreicht deshalb kein Ergebnis.</para>
    /// </summary>
    public sealed class Tagesbilanzzustand
    {
        /// <summary>Die Raumtemperatur am Ende der zuletzt gerechneten Stunde in °C.</summary>
        public double Vortemperatur { get; internal set; }

        /// <summary>Setzt den Zustand zurück (entspricht DllMain/DLL_PROCESS_ATTACH: 0).</summary>
        public void ResetState() => Vortemperatur = 0.0;
    }
}
