namespace WindowsFormsApplication1
{
    /// <summary>
    /// Schlüssel-Wert-Ablage für Einstellungen. Ersetzt im Kern die Registry
    /// (<c>HKCU\Software\wp-plan</c>, 10 Dateien) UND die lesenden Zugriffe auf
    /// <c>Properties.Settings.Default</c> (Vermessung iU5, Abschnitt A.4).
    ///
    /// <para><b>Was NICHT hierher wandert.</b> <c>Properties.Settings</c> bleibt als
    /// Ablage bestehen — die neun Schlüssel <c>DBPath</c>, <c>DBName</c>,
    /// <c>PVGISUrl</c>, <c>GeoKodierung</c>, <c>WordPressUrl</c>, <c>VDI3805Path</c>,
    /// <c>DBExportPath</c>, <c>DBImportPath</c>, <c>AllgemeinPath</c> werden vom
    /// Einstellungsdialog geschrieben und von der Erststart-Migration gepflegt. Diese
    /// Schnittstelle ersetzt nur die LESENDEN Zugriffe aus Kern-Code; die
    /// Windows-Fassung fragt dafür zuerst <c>Properties.Settings</c> und dann die
    /// Registry.</para>
    /// </summary>
    public interface IEinstellungen
    {
        /// <summary>Zeichenkette lesen; <paramref name="vorgabe"/>, wenn nichts hinterlegt ist.</summary>
        string Lies(string schluessel, string vorgabe = null);

        /// <summary>Ganzzahl lesen; <paramref name="vorgabe"/>, wenn nichts hinterlegt oder nichts lesbar ist.</summary>
        int LiesZahl(string schluessel, int vorgabe = 0);

        /// <summary>Zeichenkette schreiben.</summary>
        void Schreib(string schluessel, string wert);

        /// <summary>Ganzzahl schreiben (unter Windows als <c>DWord</c>).</summary>
        void SchreibZahl(string schluessel, int wert);

        /// <summary>Eintrag entfernen. Fehlt er, geschieht nichts.</summary>
        void Loesche(string schluessel);

        /// <summary>
        /// MASCHINENWEITER Lesezugriff, den ein Anwender nicht überschreiben kann —
        /// unter Windows <c>HKLM\Software\wp-plan</c> in beiden Registry-Sichten.
        /// Einzige Fundstelle: der maschinenweite KI-Abschalter
        /// (<c>KiEinwilligung.AbschalterMaschine</c>), den die Verwaltung einer
        /// Kundeninstallation setzt. Es gibt bewusst kein Gegenstück zum Schreiben.
        /// </summary>
        string LiesMaschine(string schluessel, string vorgabe = null);
    }
}
