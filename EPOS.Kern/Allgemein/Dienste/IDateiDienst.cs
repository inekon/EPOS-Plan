namespace WindowsFormsApplication1
{
    /// <summary>
    /// Datei- und Ordnerwahl sowie das Öffnen mit der Systemanwendung.
    /// 3 Fundstellen im Kernsatz (Vermessung iU5, Abschnitt A.3): der Öffnen-Dialog in
    /// <c>FileDlgClass</c>, der Speichern-Dialog in <c>CsvExportClass</c> und
    /// <c>ToolsClass.OpenFileWithDefaultApp</c>.
    ///
    /// <para>Alle drei Wahlmethoden liefern <c>""</c>, wenn der Anwender abbricht oder
    /// wenn es keine Oberfläche gibt — der Bestand prüft an jeder Stelle auf leer.</para>
    /// </summary>
    public interface IDateiDienst
    {
        /// <summary>Datei zum Lesen wählen; <c>""</c> = abgebrochen.</summary>
        /// <param name="titel">Fenstertitel; <c>null</c> = Vorgabe des Systems.</param>
        /// <param name="filter">Dateifilter in der Windows-Schreibweise
        /// (<c>"xls files (*.xls)|*.xls"</c>); auf anderen Plattformen sinngemäß.</param>
        /// <param name="startOrdner">Vorgeschlagener Ordner; darf leer sein.</param>
        string DateiOeffnen(string titel, string filter, string startOrdner);

        /// <summary>Datei zum Schreiben wählen; <c>""</c> = abgebrochen.</summary>
        /// <param name="titel">Fenstertitel.</param>
        /// <param name="filter">Dateifilter.</param>
        /// <param name="vorschlag">Vorbelegter Dateiname, gern mit vollem Pfad.</param>
        string DateiSpeichern(string titel, string filter, string vorschlag);

        /// <summary>Ordner wählen; <c>""</c> = abgebrochen.</summary>
        string OrdnerWaehlen(string titel, string startOrdner);

        /// <summary>
        /// Öffnet eine Datei mit der im System hinterlegten Anwendung.
        /// <c>false</c>, wenn das nicht möglich war.
        /// </summary>
        bool MitSystemOeffnen(string pfad);
    }
}
