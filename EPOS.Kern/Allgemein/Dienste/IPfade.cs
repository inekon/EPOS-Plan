namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Ablagewurzeln, die der Kern kennt — 14 Fundstellen in 12 Dateien
    /// (Vermessung iU5, Abschnitt A.3).
    ///
    /// <para><b>Die Pfade müssen zeichengleich bleiben.</b> Unter ihnen liegen der
    /// Lizenztoken, der KI-Schlüssel, der Wiki- und der Semantik-Zwischenspeicher. Ein
    /// verschobener Ordner entwertet auf einem Bestandsrechner stillschweigend die
    /// Lizenz und wirft alle Zwischenspeicher weg. Die Windows-Fassung bildet deshalb
    /// exakt die heutigen Zeichenketten ab, einschließlich der Groß-/Kleinschreibung
    /// (<c>wp-plan</c> gegen <c>WP-Plan</c>) und der beiden verschiedenen
    /// <c>%APPDATA%</c>-Unterordner.</para>
    ///
    /// <para><b>Zwei Verbindemethoden mit Absicht.</b> Ein Teil der Fundstellen legt den
    /// Ordner beim Bilden des Pfades an (<c>LizenzManager.Verzeichnis</c>,
    /// <c>KiChatService.Verzeichnis</c>), der andere nicht
    /// (<c>SemantikIndex.Ordner</c>, <c>WikiWissen.CacheOrdner</c>,
    /// <c>HelpCatalog.SicherungsPfad</c>). Eine einzige Methode „verbinden und anlegen"
    /// hätte an den zweiten Stellen leere Ordner erzeugt, wo bisher keine entstanden —
    /// eine Wirkung, die iU5 ausdrücklich nicht haben soll.</para>
    /// </summary>
    public interface IPfade
    {
        /// <summary>
        /// <c>%APPDATA%\wp-plan</c> — Lizenz, KI-Schlüssel, Wiki- und Semantikablage.
        /// Kleingeschrieben; das ist der gewachsene Bestand.
        /// </summary>
        string Anwendungsdaten { get; }

        /// <summary>
        /// <c>%APPDATA%\&lt;Produktname&gt;</c> — der Hilfe-Zwischenspeicher
        /// (<c>help_cache.json</c>, Startbestand des Wiki-Katalogs). Das ist ein ANDERER
        /// Ordner als <see cref="Anwendungsdaten"/>: Der Bestand bildet ihn über
        /// <c>Application.ProductName ?? "WP-Plan"</c>, was unter Windows
        /// <c>EPOS-Plan</c> ergibt (<c>AssemblyProduct</c>). Ohne Oberfläche gilt der
        /// dort hinterlegte Rückfall <c>WP-Plan</c>.
        /// </summary>
        string Produktdaten { get; }

        /// <summary>
        /// <c>LocalApplicationData\WP-Plan</c> — entspricht
        /// <c>Program.ApplicationPath_User</c>.
        /// </summary>
        string BenutzerLokal { get; }

        /// <summary>
        /// <c>LocalApplicationData</c> UNVERÄNDERT, ohne Anwendungsordner. Nur für
        /// Ablagen, die nicht unter dem Anwendungsnamen liegen — im Bestand genau eine:
        /// der CEC-Modulzwischenspeicher unter <c>CECModuleImporter\</c>.
        /// </summary>
        string BenutzerLokalBasis { get; }

        /// <summary>
        /// <c>CommonApplicationData\WP-Plan</c> — entspricht
        /// <c>Program.ApplicationPath_Common</c>, unter Windows
        /// <c>C:\ProgramData\WP-Plan</c>.
        /// </summary>
        string Gemeinsam { get; }

        /// <summary>„Eigene Dokumente" — Vorgabeordner für Berichte und CSV-Ausgaben.</summary>
        string Dokumente { get; }

        /// <summary>Setzt einen Pfad zusammen. Legt NICHTS an.</summary>
        string Verbinde(string wurzel, params string[] teile);

        /// <summary>
        /// Setzt einen Pfad zusammen und legt den Ordner an, falls er fehlt.
        /// Schlägt das Anlegen fehl, wird der Pfad trotzdem geliefert — der Aufrufer
        /// scheitert dann erst beim Schreiben, wie im Bestand.
        /// </summary>
        string Unterordner(string wurzel, params string[] teile);
    }
}
