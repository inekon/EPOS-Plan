using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Bedienvorgaben der Speicherflotte, die von der gewählten Betriebsführung abhängen
    /// (Konzept Stromspeicher-Dialoge 2.4 Punkt 4, Anwenderentscheid SD‑Q5 vom 11.09.2026).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Diese Klasse gilt ausschließlich für die <b>Vorbelegung einer NEUEN Konfiguration</b>.
    /// Die serialisierte Vorgabe in <see cref="FlottenSimulationOptionen.NetzladungErlaubt"/>
    /// bleibt <c>false</c>, damit gespeicherte Stände unverändert gelesen werden — insbesondere
    /// der Stand <c>@Projektflotte</c> des Prüfprojekts 1046, der die Eigenschaft ausdrücklich
    /// trägt und die Regressionsbasis R7 hält.
    /// </para>
    /// </remarks>
    public static class FlottenVorgaben
    {
        /// <summary>
        /// Die Vorgabe für „Netzladung erlaubt" zu einem Betriebsziel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Lastspitzenkappung</b> bekommt die Freigabe: Die Spezifikation 5.1 sagt
        /// „Die Wiederaufladung nutzt freie Anschlussleistung unter H" — und genau das IST
        /// Netzladung. Ohne die Freigabe kann eine Flotte an einem Standort ohne Überschuss
        /// nie wieder laden und bleibt arbeitslos (Befund SP‑O‑10).
        /// </para>
        /// <para>
        /// <b>PV-Eigenverbrauch</b> bekommt sie nicht: Dort ist der Speicher ausdrücklich
        /// nur für den eigenen Überschuss gedacht.
        /// </para>
        /// <para>
        /// Für die <b>planenden</b> Ziele bleibt es bei der heutigen Vorgabe <c>false</c>;
        /// ihr Fahrplan entscheidet über den Netzbezug selbst, und eine stillschweigende
        /// Freigabe würde die Zielfunktion verändern.
        /// </para>
        /// </remarks>
        /// <param name="ziel">Die gewählte Betriebsführung.</param>
        /// <returns><c>true</c>, wenn eine neue Konfiguration mit freigegebener Netzladung vorbelegt wird.</returns>
        public static bool NetzladungFuer(FlottenBetriebsziel ziel) =>
            ziel == FlottenBetriebsziel.PeakShaving;
    }
}
