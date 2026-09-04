namespace WindowsFormsApplication1
{
    /// <summary>
    /// Rückgabefach für Masken, die ein Projekt AUSWÄHLEN — die Projektauswahl und der
    /// Löschdialog.
    ///
    /// <para><b>Wozu ein eigenes Fach.</b> <see cref="INavigation.OeffneMaske"/> liefert
    /// nur „mit OK beendet". Zwei Masken tragen aber ein Ergebnis heraus: welches
    /// Projekt der Anwender gewählt hat. Statt die Schnittstelle um Ausgabeparameter
    /// aufzublähen, reicht der Aufrufer dieses Fach als Argument hinein und liest es
    /// danach aus — dieselbe Bauform, mit der der Bestand die Felder
    /// <c>frm.m_ID_Projekt</c>/<c>frm.m_szProjekt</c> nach dem Dialog abholt, nur ohne
    /// dass der Aufrufer die Maske kennen muss.</para>
    /// </summary>
    public sealed class Projektwahl
    {
        /// <summary><c>Tab_Projekt.ID</c> des gewählten Projekts; <c>0</c> = keins.</summary>
        public int Id;

        /// <summary>Name des gewählten Projekts; <c>""</c> = keins.</summary>
        public string Name = "";

        /// <summary>
        /// Der Anwender hat dem Löschen ALLER Projekte dieses Namens ausdrücklich
        /// zugestimmt (iU9-W15a, Entscheid O-3 vom 04.09.2026).
        ///
        /// <para>Regulär bleibt das Feld <c>false</c>: <c>Tab_Projekt</c> trägt den
        /// eindeutigen Index <c>Projektname</c>, ein Name trifft also genau ein Projekt.
        /// Führt ein Altbestand ohne diesen Index den Fall doch, fragt der Löschdialog
        /// nach und setzt bei „Ja" dieses Feld; der Aufrufer reicht es als
        /// <c>mehrdeutigZugelassen</c> an <c>ProjektCtrl.LoeschenMitVorarbeiten</c>
        /// weiter. Id und Name bleiben davon unberührt (Befund W15a-B45).</para>
        /// </summary>
        public bool AlleGleichenNamens;
    }
}
