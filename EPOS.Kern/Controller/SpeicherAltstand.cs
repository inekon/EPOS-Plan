namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die BENANNTE UMSETZUNG gespeicherter Stände</b> des Stromspeichers auf die
    /// gültigen Persistenzwerte.
    /// </summary>
    /// <remarks>
    /// <para><b>Was hier steht und warum.</b> Die Betriebsart „Nachtnutzung" ist als
    /// Berechnungsart des Simulationslaufs entfallen (Anwenderentscheid 15.09.2026). Ein
    /// Anwenderprojekt kann sie in <c>Tab_StromspeicherVariante.Berechnungsart</c>
    /// tragen — die Spalte bleibt, nur dieser eine Wert gilt nicht mehr. Der Stand soll
    /// sich öffnen lassen: nicht mit einem Absturz, aber auch nicht stillschweigend.
    /// Deshalb wird er UMGESETZT und dabei GESAGT, statt verworfen oder kommentarlos
    /// anders gerechnet zu werden — dasselbe Muster wie
    /// <c>FlottenAltstand.Normalisiere</c> für die C-Rate-Kopplung der Flotte.</para>
    /// <para><b>Wohin er umgesetzt wird.</b> Auf die Dauernutzung
    /// (<see cref="DbWerte.SP_BERECHNUNG_DAUERNUTZUNG"/>) — sie ist der
    /// referenzverifizierte Standardfall und dieselbe Rechnung, gegen die die
    /// Nachtnutzung im Lauf ohnehin verglichen wurde.</para>
    /// <para><b>Gesagt wird es an zwei Stellen</b>, und zwar an denen, an denen der
    /// Anwender hinsieht: im Protokoll des Simulationslaufs
    /// (<c>SP_ALTSTAND_BERECHNUNGSART</c>, gesetzt von
    /// <c>StromspeicherSimCtrl.BaueStrategie</c>) und in jedem Anzeigetext der
    /// Berechnungsart (<c>SP_BERECHNUNG_ANZEIGE_ALTSTAND</c>, gesetzt von
    /// <see cref="SpeicherAnzeigeCtrl.BerechnungsartText"/>).</para>
    /// <para><b>Kein Schemaschritt.</b> Die Spalte wird nicht angefasst und der
    /// gespeicherte Text nicht überschrieben: Erst wenn der Anwender die Berechnungsart
    /// selbst setzt, wandert der neue Wert in die Datenbank. Bis dahin bleibt lesbar,
    /// was er einmal gewollt hat.</para>
    /// </remarks>
    public static class SpeicherAltstand
    {
        /// <summary>
        /// Der entfallene Persistenzwert der Berechnungsart. Er steht hier und
        /// NUR hier — in <see cref="DbWerte"/> hat er nichts mehr verloren, weil er
        /// nicht mehr geschrieben wird.
        /// </summary>
        public const string BERECHNUNG_NACHTNUTZUNG = "Nachtnutzung";

        /// <summary>
        /// <c>true</c>, wenn der gespeicherte Wert die entfallene Berechnungsart ist.
        /// </summary>
        /// <param name="wert">Der gespeicherte Wert; <c>null</c> ergibt <c>false</c>.</param>
        public static bool IstEntfalleneBerechnungsart(string wert)
            => wert == BERECHNUNG_NACHTNUTZUNG;

        /// <summary>
        /// Setzt eine gespeicherte Berechnungsart auf einen gültigen Wert um.
        /// </summary>
        /// <param name="wert">Der gespeicherte Wert.</param>
        /// <returns>
        /// Die Dauernutzung, wenn der Wert die entfallene Berechnungsart ist — sonst der
        /// Wert unverändert. Ein unbekannter Wert bleibt unbekannt: Der Lauf sagt dazu
        /// seinen eigenen Satz, und eine Behauptung über Daten, die man nicht kennt,
        /// gehört nicht hierher.
        /// </returns>
        public static string Berechnungsart(string wert)
            => IstEntfalleneBerechnungsart(wert) ? DbWerte.SP_BERECHNUNG_DAUERNUTZUNG : wert;
    }
}
