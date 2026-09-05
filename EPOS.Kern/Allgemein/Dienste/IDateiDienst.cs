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

        /// <summary>
        /// Öffnet eine ADRESSE (http/https) im Standardbrowser; <c>false</c>, wenn
        /// das nicht möglich war.
        ///
        /// <para><b>Warum nicht <see cref="MitSystemOeffnen"/></b> (iU9-W16c.3):
        /// Der prüft <c>File.Exists</c> und liefert für eine Adresse deshalb
        /// immer <c>false</c>. Der einzige Aufrufer im Bestand war der Menüpunkt
        /// „Hilfe → Dokumentation", der bis W16c unmittelbar <c>Process.Start</c>
        /// rief — die letzte Windows-API dieser Art im Hauptfenster.</para>
        ///
        /// <para><b>Mit Standardumsetzung</b> (<c>false</c>), damit vorhandene
        /// Fassungen — <c>KeineDateiwahl</c> und der iOS-Adapter — durch die
        /// Erweiterung nicht brechen. Auf iOS gibt es das Menü nicht; wer den
        /// Weg dort braucht, legt die Fassung mit iU11 nach.</para>
        /// </summary>
        bool AdresseOeffnen(string adresse) => false;

        // ==================================================================
        //  Die WARTBAREN Zwillinge (Befund W13-B-1, 05.09.2026)
        // ==================================================================
        //
        // WARUM ES SIE GIBT. Ein Dateiwaehler ist ein MODALES SYSTEMFENSTER.
        // Seit Startseite und Hauptfenster Razor sind, kommt jeder Aufruf aus
        // einem Blazor-Ereignis - unter Windows also aus dem
        // WebMessageReceived-Rueckruf der WebView2, auf iOS vom Hauptfaden.
        // Beide Plattformen vertragen das nicht:
        //
        //   Windows: OpenFileDialog.ShowDialog() oeffnet seine verschachtelte
        //            Nachrichtenschleife INNERHALB des Rueckrufs - dasselbe
        //            Muster, das als Befund W16b-B-1 die leeren Dialoge
        //            verursacht hat (Blazorsprung).
        //   iOS:     IosDateiDienst.AufDemHauptfaden liefert vom Hauptfaden aus
        //            default, um einen Selbstblock zu vermeiden - der Waehler
        //            geht dort also gar nicht erst auf.
        //
        // WAS DIE ZWILLINGE AENDERN. Der Aufrufer wartet (await), statt zu
        // blockieren. Blazor kann sein Ereignis abschliessen, und die Fassung
        // der Plattform faehrt das Fenster HINTER dem Ereignis hoch - unter
        // Windows eine gepostete Nachricht spaeter (WindowsDateiDienst ueber
        // Blazornachlauf), auf iOS auf dem Hauptfaden ohne Warten.
        //
        // MIT STANDARDFASSUNG, damit vorhandene Fassungen (KeineDateiwahl, der
        // iOS-Adapter, jeder Pruefstand) durch die Erweiterung nicht brechen:
        // Wer nichts sagt, faellt auf die synchrone Form zurueck - genau das
        // Verhalten von heute. Die Signaturen der synchronen Form bleiben
        // unveraendert; sie hat weiterhin ihre Aufrufer im Bestand.

        /// <summary>
        /// <see cref="DateiOeffnen"/> HINTER dem laufenden Ereignis.
        /// <c>""</c> = abgebrochen.
        /// </summary>
        System.Threading.Tasks.Task<string> DateiOeffnenAsync(
            string titel, string filter, string startOrdner)
            => System.Threading.Tasks.Task.FromResult(DateiOeffnen(titel, filter, startOrdner));

        /// <summary>
        /// <see cref="DateiSpeichern"/> HINTER dem laufenden Ereignis.
        /// <c>""</c> = abgebrochen.
        /// </summary>
        System.Threading.Tasks.Task<string> DateiSpeichernAsync(
            string titel, string filter, string vorschlag)
            => System.Threading.Tasks.Task.FromResult(DateiSpeichern(titel, filter, vorschlag));

        /// <summary>
        /// <see cref="OrdnerWaehlen"/> HINTER dem laufenden Ereignis.
        /// <c>""</c> = abgebrochen.
        /// </summary>
        System.Threading.Tasks.Task<string> OrdnerWaehlenAsync(string titel, string startOrdner)
            => System.Threading.Tasks.Task.FromResult(OrdnerWaehlen(titel, startOrdner));
    }
}
