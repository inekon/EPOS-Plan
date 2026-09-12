namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// EINE Zeile der Projektliste des Gebäudedialogs (iU9-W9.2) — das plattformfreie Abbild
/// von <c>Z_ProjGebModel</c>.
///
/// <para><b><see cref="IdZ"/> ist der Schlüssel, nicht <see cref="IdGebaeude"/>.</b> Zwei
/// gleiche Gebäude im Projekt haben dieselbe Stamm-Id; die Zuordnung unterscheidet sie
/// über <c>Z_ProjektGebaeude.ID</c>. Genau daran hängt, dass „▶" die richtige Zeile
/// entfernt (<c>btn_Entfernen_Click</c>:283-287 nennt den früheren Fehler beim Namen).
/// Eine noch nicht gespeicherte Zeile bekommt eine geratene Id ab 100000.</para>
///
/// <para><b>Veränderlich, nicht als <c>record</c></b> — der Dialog schreibt beim Ändern
/// hinein, und die Liste gehört der Hülle (Muster der Wellen 6 und 7).</para>
/// </summary>
public sealed class GebaeudeProjektZeile
{
    /// <summary>Der Schlüssel der ZUORDNUNG (<c>Z_ProjektGebaeude.ID</c>).</summary>
    public int IdZ { get; set; }

    /// <summary>Der Schlüssel des Projektgebäudes (<c>Tab_Gebaeude.ID_ProjektGebaeude</c>).</summary>
    public int IdGebaeude { get; set; }

    /// <summary>Der Gebäudename.</summary>
    public string Name { get; set; } = "";

    /// <summary>Die Gebäudeart aus dem Katalogsatz.</summary>
    public string Art { get; set; } = "";

    /// <summary>Die Beschreibung aus dem Katalogsatz.</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>
    /// Die Baualtersklasse — beim Anlegen der Zeile der gespeicherte BUCHSTABE, in der
    /// Anzeige der Klartext. Die Hülle setzt beides über
    /// <c>GebaeudeStammCtrl.KlassenIndex</c> um.
    /// </summary>
    public string Baualtersklasse { get; set; } = "";

    /// <summary>Der Verbrauch bzw. die Wohnfläche der Zuordnung.</summary>
    public double Wohnflaeche { get; set; }

    /// <summary>Die Bedarfsart samt Einheit, z. B. „Wohnfläche [m²]".</summary>
    public string Einheit { get; set; } = "";

    /// <summary>Der Jahresnutzungsgrad der Zuordnung.</summary>
    public double Jahresnutzungsgrad { get; set; }

    /// <summary>Dezentrale Warmwasserbereitung.</summary>
    public bool DezentralWarmwasser { get; set; }
}

/// <summary>
/// EINE Zeile des Katalograsters (iU9-W9.2) — Name und, in einer zweiten Spalte, Art und
/// Fläche. Der Vorläufer stellte beides in EINE Zelle („Art\nFläche [m²]"); getrennte
/// Spalten sind lesbar und lassen sich sortieren.
/// </summary>
/// <param name="Name">Der Bezeichner des Katalogsatzes.</param>
/// <param name="Art">Die Gebäudeart.</param>
/// <param name="Wohnflaeche">Die Gesamtfläche, bereits als Text mit zwei Nachkommastellen.</param>
public sealed record GebaeudeKatalogZeile(string Name, string Art, string Wohnflaeche);

/// <summary>
/// Der Detailblock zu einem KATALOGSATZ (iU9-W9.2) —
/// <c>listBox_Gebaeude_DB_SelectedIndexChanged</c>:574-602.
/// </summary>
/// <param name="Name">Der Bezeichner.</param>
/// <param name="Art">Die Gebäudeart.</param>
/// <param name="Beschreibung">Die Beschreibung.</param>
/// <param name="Wohnflaeche">Die Gesamtfläche als Text.</param>
public sealed record GebaeudeStammDetail(
    string Name, string Art, string Beschreibung, string Wohnflaeche);
