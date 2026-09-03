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
    }
}
