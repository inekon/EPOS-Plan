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
