namespace WindowsFormsApplication1
{
    /// <summary>
    /// Maskenaufruf. Ersetzt im Kernsatz 35 Aufrufe von
    /// <c>Program.mainfrm.Set&lt;Gewerk&gt;Control(…)</c> und 45 Stellen
    /// <c>new Form_X(); frm.ShowDialog();</c> in 14 Controllern
    /// (Vermessung iU5, Abschnitt A.6).
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
        /// Frischt die Gewerksliste eines Projekts im Detailformular auf — die zwölf
        /// <c>Set*Control</c>-Methoden von <c>FormMain</c> unter einem Dach.
        /// </summary>
        /// <param name="gewerk">Sprachneutraler Gewerksschlüssel, siehe <see cref="Gewerke"/>.</param>
        /// <param name="idProjekt">Projekt-ID; wird von den Gewerken benutzt, die nach ID auffrischen.</param>
        /// <param name="projektname">Projektname; wird von den Gewerken benutzt, die nach Namen auffrischen.</param>
        void OeffneGewerk(string gewerk, int idProjekt, string projektname);

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

    /// <summary>
    /// Die sprachneutralen Gewerksschlüssel für <see cref="INavigation.OeffneGewerk"/>.
    /// Ihre Werte stehen in einer Tabelle in <c>WinFormsNavigation</c> den zwölf
    /// <c>FormMain.Set*Control</c>-Methoden gegenüber.
    /// </summary>
    public static class Gewerke
    {
        /// <summary>Wärmepumpen.</summary>
        public const string Waermepumpe = "WAERMEPUMPE";
        /// <summary>Blockheizkraftwerke.</summary>
        public const string Bhkw = "BHKW";
        /// <summary>Stromspeicher.</summary>
        public const string Stromspeicher = "STROMSPEICHER";
        /// <summary>Heizkessel.</summary>
        public const string Heizkessel = "HEIZKESSEL";
        /// <summary>Gebäude.</summary>
        public const string Gebaeude = "GEBAEUDE";
        /// <summary>Eingelesener Wärmebedarf.</summary>
        public const string WaermebedarfExtern = "WAERMEBEDARF_EXTERN";
        /// <summary>Prozesswärme.</summary>
        public const string Prozesswaerme = "PROZESSWAERME";
        /// <summary>Strombedarf.</summary>
        public const string Strombedarf = "STROMBEDARF";
        /// <summary>Stromganglinien.</summary>
        public const string Stromganglinie = "STROMGANGLINIE";
        /// <summary>Photovoltaik.</summary>
        public const string Photovoltaik = "PHOTOVOLTAIK";
        /// <summary>Pufferspeicher.</summary>
        public const string Pufferspeicher = "PUFFERSPEICHER";
        /// <summary>Solarthermie.</summary>
        public const string Solarthermie = "SOLARTHERMIE";
    }
}
