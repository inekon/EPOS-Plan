namespace SpeicherEngine
{
    /// <summary>
    /// Betriebsart des Speichers nach der Quellen-Matrix (Fachkonzept 2.1).
    /// Sie legt fest, aus welchen Quellen geladen werden darf.
    /// </summary>
    /// <remarks>
    /// Welche Erzeugungsquellen zulaessig sind, steht in den Flags
    /// <see cref="SpeicherParameter.PvZulaessig"/> und
    /// <see cref="SpeicherParameter.BhkwUeberschussZulaessig"/>; die Betriebsart
    /// entscheidet ausschliesslich ueber den <b>Netzpfad</b>. Wirksam wird sie deshalb
    /// nur in der <see cref="Arbitrage"/> (AP10): Dort gibt sie frei, ob aus dem Netz
    /// geladen werden darf. In den uebrigen Strategien bleibt sie reiner Ausweis, weil
    /// die dort keinen Netzpfad kennen.
    /// </remarks>
    public enum SpeicherBetriebsart
    {
        /// <summary>
        /// Gruenstromspeicher: Laden ausschliesslich aus Erzeugungsueberschuss
        /// (PV und/oder BHKW), <b>keine</b> Netzladung. Verguetungsanspruch und
        /// Netzentgeltbefreiung haengen an dieser Ausschliesslichkeit; die rechtliche
        /// Wuerdigung bleibt beim Anwender.
        /// </summary>
        Gruenstrom = 0,

        /// <summary>
        /// Graustromspeicher: zusaetzlich Netzladung zulaessig (preisgesteuert, AP10).
        /// </summary>
        Graustrom = 1
    }
}
