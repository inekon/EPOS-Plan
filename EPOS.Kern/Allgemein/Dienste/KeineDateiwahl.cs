namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorbelegung von <see cref="Dienste.Datei"/>: Es gibt keine Oberfläche, also
    /// wählt niemand etwas aus. Alle drei Wahlmethoden liefern <c>""</c> — genau das,
    /// was ein abgebrochener Dialog liefert; der Bestand prüft an jeder Fundstelle auf
    /// leer und tut dann nichts.
    ///
    /// <para><see cref="MitSystemOeffnen"/> liefert <c>false</c>: Ohne Schreibtisch gibt
    /// es keine Standardanwendung. Das ist derselbe Rückgabewert, mit dem
    /// <c>ToolsClass.OpenFileWithDefaultApp</c> heute einen fehlgeschlagenen Start
    /// meldet.</para>
    /// </summary>
    public sealed class KeineDateiwahl : IDateiDienst
    {
        /// <inheritdoc/>
        public string DateiOeffnen(string titel, string filter, string startOrdner)
        {
            return "";
        }

        /// <inheritdoc/>
        public string DateiSpeichern(string titel, string filter, string vorschlag)
        {
            return "";
        }

        /// <inheritdoc/>
        public string OrdnerWaehlen(string titel, string startOrdner)
        {
            return "";
        }

        /// <inheritdoc/>
        public bool MitSystemOeffnen(string pfad)
        {
            return false;
        }
    }
}
