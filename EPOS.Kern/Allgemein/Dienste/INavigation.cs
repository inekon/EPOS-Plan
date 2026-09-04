namespace WindowsFormsApplication1
{
    /// <summary>
    /// Maskenaufruf. Ersetzt im Kernsatz 45 Stellen
    /// <c>new Form_X(); frm.ShowDialog();</c> in 14 Controllern
    /// (Vermessung iU5, Abschnitt A.6).
    ///
    /// <para><b>Seit iU9-W16b.1 ohne <c>OeffneGewerk</c>.</b> Die Methode ordnete zwoelf
    /// Gewerksschluessel den zwoelf <c>Set*Control</c>-Methoden des Detailformulars
    /// <c>FormMain</c> zu und existierte AUSSCHLIESSLICH fuer dieses eine Fenster
    /// (Befund W16-B27/B28). Mit dem Anwenderentscheid E-7 (K6-a) ist der Altzweig
    /// stillgelegt; damit fallen die Methode, die Konstantenklasse <c>Gewerke</c>, ihre
    /// drei Umsetzungen und die dreissig Aufrufe der zwoelf Kontextmenue-Controller.</para>
    ///
    /// <para><b>Die Aufrufrichtung dreht sich um.</b> Bis iU5 kannte der Controller die
    /// Maske: Er baute sie, zeigte sie und wertete ihr Ergebnis aus. Danach kennt er nur
    /// noch einen SCHLÜSSEL und überlässt das Bauen der Oberfläche. Deshalb steht diese
    /// Tranche am Ende — ihr Rückweg ist der teuerste.</para>
    ///
    /// <para><b>Schlüssel sind sprachneutral und ASCII</b>, nach der Drei-Schichten-Regel
    /// (Konzept 13.6): <c>"BHKW"</c>, <c>"WAERMEPUMPE"</c>, <c>"Form_WP"</c> — nie ein
    /// Anzeigetext. Ein unbekannter Schlüssel tut nichts und liefert <c>false</c>; das
    /// ist der Zustand ohne Oberfläche und kein Fehler.</para>
    /// </summary>
    public interface INavigation
    {
        /// <summary>
        /// Öffnet eine Maske modal.
        /// </summary>
        /// <param name="maske">Sprachneutraler Maskenschlüssel, im Bestand der Klassenname der Maske.</param>
        /// <param name="argumente">Zusatzangaben der jeweiligen Maske; siehe die Fundstelle.</param>
        /// <returns><c>true</c>, wenn die Maske mit OK beendet wurde.</returns>
        bool OeffneMaske(string maske, params object[] argumente);

        /// <summary>Frischt die Menüleiste auf (Freischaltungen nach Projektwechsel).</summary>
        void MenueAktualisieren();

        /// <summary>
        /// Frischt einen Anzeigebereich der Startmaske auf — <c>"VARIANTEN"</c>,
        /// <c>"BERICHTE_KOSTEN"</c>, <c>"PROJEKT"</c>.
        /// </summary>
        void AnsichtAktualisieren(string bereich);
    }
}
