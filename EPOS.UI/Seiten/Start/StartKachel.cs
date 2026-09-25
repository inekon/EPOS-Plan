using EPOS.UI.Bausteine;

namespace EPOS.UI.Seiten.Start;

/// <summary>
/// EINE Kachel der Startseite (iU9-W16b.2) — Schlüssel, Beschriftung und der
/// Bestand im Projekt.
///
/// <para><b>Warum die Texte MITREISEN.</b> Der Bestand führte sie je Kachel in
/// drei Dateien (<c>Form_Start.resx</c> neutral/deutsch, <c>.de-DE</c>,
/// <c>.en-US</c>, Befund W16-B21) und band sie über den Steuerelementnamen an.
/// Sie stehen jetzt als <c>MyResource.Resource.START_K_*</c> im Kern; die Hülle
/// setzt sie hier ein — dieselbe Bauart wie
/// <c>Dialoge.Bedarf.KomponentenZeile</c> aus W16a.3. Die Komponente kennt
/// weder Ressourcen noch Datenbank (Hausregel EPOS.UI).</para>
///
/// <para><b><see cref="Zustand"/> ist der Statuspunkt</b> und kommt aus
/// <c>KomponentenBestandCtrl.Bitmaske</c> (K1, Entscheid E-3) — dieselbe
/// Wahrheit, die auch der Komponentenschritt des Assistenten zeigt. <c>null</c>
/// heißt „diese Kachel führt keinen Bestand": Die fünf Projektkacheln und der
/// Konfigurationsknopf haben keinen Punkt, weil es nichts zu zählen gibt. Damit
/// entfallen die dreizehn <c>Paint</c>-Handler des Vorläufers (je 45 Zeilen
/// <c>GraphicsPath</c> und Halbdeckkraft) ersatzlos — der Anstrich ist eine
/// CSS-Klasse.</para>
/// </summary>
public sealed class StartKachel
{
    /// <summary>Sprachneutraler Schlüssel (<see cref="Kachelschluessel"/>).</summary>
    public string Schluessel { get; set; } = "";

    /// <summary>Reiter, auf dem die Kachel steht (<see cref="Reiterschluessel"/>).</summary>
    public string Reiter { get; set; } = "";

    /// <summary>Beschriftung — der <c>Titel</c> bzw. das erste Label des Vorläufers.</summary>
    public string Titel { get; set; } = "";

    /// <summary>Zweite Zeile — die <c>Beschreibung</c> bzw. das zweite Label.</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>
    /// Der Bestand im Projekt: <c>An</c> = im Projekt (grüner Punkt),
    /// <c>Aus</c> = nicht im Projekt (grauer Punkt), <c>null</c> = kein
    /// Statuspunkt.
    /// </summary>
    public Kachelstand? Zustand { get; set; }
}

/// <summary>
/// Die vier Zeilen der Projektzusammenfassung auf dem Reiter „Simulation"
/// (iU9-W16b.2) — wörtlich <c>Form_Start.tabPage5_Enter</c> (:1062-1093).
///
/// <para>Die Zahlen sind bereits FORMATIERT: Der Vorläufer schrieb
/// <c>…ToString("F2") + " MWh/a"</c> unmittelbar in das Label, und die Rechnung
/// dahinter (<c>SimulationStrombedarf.Berechnung</c>,
/// <c>SimulationWaermebedarf.Waermebedarf_berechnen</c>) gehört in die Hülle,
/// nicht in die Anzeige.</para>
/// </summary>
/// <param name="Projektname">Der Name des offenen Projekts.</param>
/// <param name="Waermebedarf">Wärmebedarf mit Einheit, z. B. „123,45 MWh/a".</param>
/// <param name="Strombedarf">Strombedarf mit Einheit.</param>
/// <param name="Komponenten">Die gewählten Technologien, mit „, " verbunden.</param>
public sealed record Zusammenfassung(
    string Projektname,
    string Waermebedarf,
    string Strombedarf,
    string Komponenten);

/// <summary>
/// <b>Woher die Klimadaten des offenen Projekts stammen</b> (Auftrag KL-4,
/// Anwenderwunsch 19.09.2026: „Die verwendeten Klimadaten sollen sich auch auf der
/// Übersicht befinden.").
///
/// <para>Die Texte sind bereits ÜBERSETZT und formatiert: Die Hülle setzt für den
/// Quellenschlüssel (<c>PVGIS</c>, <c>TRY_DATEI</c>, <c>TRY_REGIONAL</c>) seinen
/// Anzeigetext ein und schreibt das ISO-Importdatum in der Landesschreibweise. Die
/// Komponente kennt weder Ressourcen noch Datenbank (Hausregel EPOS.UI).</para>
///
/// <para><b>Leere Felder sind der Regelfall des Bestands:</b> Eine Region, die vor
/// Schemaschritt 95 angelegt wurde, sagt nicht, woher sie kommt. Die Startseite
/// zeigt dann die KURZFORM aus Bezeichner und Standort — nie eine erfundene
/// Quelle.</para>
/// </summary>
/// <param name="Quelle">Anzeigetext der Herkunft — seit Auftrag KL-6 das ganze
/// Wetterjahr, „TRY-Regionaldaten (Deutschland) · 2045 · sommerwarm"; ohne Szenario
/// und Jahr die Quelle allein, leer = Altbestand.</param>
/// <param name="Bezeichner">Name der Klimaregion des Projekts.</param>
/// <param name="Standort">Ortsname oder Koordinatenpaar.</param>
/// <param name="Importdatum">Tag des Imports, kulturgerecht; leer = unbekannt.</param>
public sealed record KlimaHerkunftGaben(string Quelle, string Bezeichner,
                                        string Standort, string Importdatum)
{
    /// <summary>
    /// <b>Die Herkunftszeile</b> — leer, wo es nichts zu sagen gibt. EIN Satzbau für
    /// die Kopfleiste der Startseite und den Projektassistenten.
    ///
    /// <para><b>Zwei Fassungen, und die kurze ist kein Notbehelf:</b> Eine Region aus
    /// dem Altbestand führt weder Quelle noch Importdatum (sie sind erst mit
    /// Schemaschritt 95 entstanden und werden nicht nachdatiert). Die Zeile nennt
    /// dann Bezeichner und Standort — und behauptet nicht, woher die Reihe
    /// stammt.</para>
    /// </summary>
    /// <param name="herkunft">Die Gaben; <c>null</c> = keine Zeile.</param>
    /// <param name="langText">Vorlage mit vier Platzhaltern: Quelle, Bezeichner,
    /// Standort, Importdatum.</param>
    /// <param name="kurzText">Vorlage mit zwei Platzhaltern: Bezeichner, Standort.</param>
    public static string Zeile(KlimaHerkunftGaben? herkunft, string langText, string kurzText)
    {
        if (herkunft is null) return "";

        string bezeichner = herkunft.Bezeichner ?? "";
        string standort = herkunft.Standort ?? "";
        if (bezeichner.Length == 0 && standort.Length == 0) return "";

        bool vollstaendig = (herkunft.Quelle ?? "").Length > 0 && (herkunft.Importdatum ?? "").Length > 0;

        return vollstaendig
            ? string.Format(System.Globalization.CultureInfo.CurrentCulture, langText,
                            herkunft.Quelle, bezeichner, standort, herkunft.Importdatum)
            : string.Format(System.Globalization.CultureInfo.CurrentCulture, kurzText,
                            bezeichner, standort);
    }
}
