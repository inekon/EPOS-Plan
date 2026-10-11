using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Bereich, der nur im Entwicklermodus sichtbar ist.</summary>
    /// <param name="Schluessel">Die stabile Kennung (eine Konstante aus <see cref="Entwicklerbereiche"/>).</param>
    /// <param name="Zweck">Ein Satz, wozu der Bereich dient.</param>
    public sealed record Entwicklerbereich(string Schluessel, string Zweck);

    /// <summary>
    /// Das Register der Entwicklerbereiche (Entwicklerhandbuch Abschnitt 2.1). Ein neuer
    /// Bereich bekommt hier eine Konstante und einen Eintrag in <see cref="Alle"/>
    /// (Handbuch Abschnitt 2.3).
    /// </summary>
    public static class Entwicklerbereiche
    {
        public const string PROJEKTDATEI_IMPORT = "PROJEKTDATEI_IMPORT";
        public const string PROJEKTDATEI_PROFILE = "PROJEKTDATEI_PROFILE";
        public const string PROJEKTDATEI_IOS = "PROJEKTDATEI_IOS";
        public const string PROJEKTDATEI_KI = "PROJEKTDATEI_KI";
        public const string KATALOG_WIEDERHERSTELLEN = "KATALOG_WIEDERHERSTELLEN";
        public const string KI_WERKZEUGE = "KI_WERKZEUGE";
        public const string EXPORT_IDS = "EXPORT_IDS";
        public const string WEBVIEW_WERKZEUGE = "WEBVIEW_WERKZEUGE";

        /// <summary>Alle Entwicklerbereiche in der Reihenfolge des Handbuchs.</summary>
        public static readonly IReadOnlyList<Entwicklerbereich> Alle = new[]
        {
            new Entwicklerbereich(PROJEKTDATEI_IMPORT,
                "Lädt Zonen, Nutzung, Ganglinien und Kalender aus der Projektdatei des CAD-Programms (.sqproj) zur IFC-Datei im Gebäudeimport dazu."),
            new Entwicklerbereich(PROJEKTDATEI_PROFILE,
                "Legt Nutzungsprofile je DIN-Nummer aus einer Projektdatei in einer eigenen Katalogkategorie an."),
            new Entwicklerbereich(PROJEKTDATEI_IOS,
                "Macht die Dateiendung .sqproj in der iOS-Dateiwahl wählbar."),
            new Entwicklerbereich(PROJEKTDATEI_KI,
                "Gibt dem Hilfe-Assistenten die Feldkarten und Aktionen der Projektdatei frei."),
            new Entwicklerbereich(KATALOG_WIEDERHERSTELLEN,
                "Stellt einen behaltenen Katalogsatz über den Katalogabgleich auf den Auslieferungsstand zurück."),
            new Entwicklerbereich(KI_WERKZEUGE,
                "Zeigt die Prüf- und Fehlersuchwerkzeuge des KI-Assistenten: Werkzeuge, Protokoll, Modellwahl und Weg B."),
            new Entwicklerbereich(EXPORT_IDS,
                "Öffnet die Exportzusage (IDS) des IFC-Exports als Prüfartefakt."),
            new Entwicklerbereich(WEBVIEW_WERKZEUGE,
                "Öffnet die Entwicklerwerkzeuge der WebView2 für DOM, Stilblatt, Konsole und Netz der Razor-Oberfläche."),
        };
    }
}
