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

    /// <summary>
    /// Der Katalogsatz (<c>Tab_Gebaeude_STAMM.ID</c>), aus dem die Projektkopie stammt bzw.
    /// beim Speichern entsteht; <c>null</c> = kein Verweis (Altbestand). Das Neuschreiben
    /// der Liste sucht den Satz darüber, der Name ist nur der Rückfall.
    /// </summary>
    public int? IdKatalog { get; set; }

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

    /// <summary>
    /// Der Rechenweg als ANZEIGETEXT (Stufe G1, Konzept 2.7) — so, wie die Weiche rechnet;
    /// die Hülle setzt ihn aus <c>Gebaeude_Modell</c> und der Vorgabe des Programms.
    /// </summary>
    public string Rechenweg { get; set; } = "";

    /// <summary>Der Wärmeleitwert H_ges [W/K]; <c>null</c> = nicht bekannt.</summary>
    public double? HgesWK { get; set; }

    /// <summary>
    /// Hat die Zeile eine Projektkopie (<c>Tab_Gebaeude</c>)? Erst dann trägt sie Zonen, und erst
    /// dann ist „Hülle und Zonen…" frei (Stufe G3, Welle D2). Eine eben aufgenommene Zeile hat keine.
    /// </summary>
    public bool HatProjektkopie { get; set; }

    /// <summary>
    /// Der Name der Zone, über die das Gebäude rechnet (Stufe G3); <c>null</c> = keine Zone —
    /// dann rechnet es den Klassenweg samt Hochrechnung über die Angabe.
    /// </summary>
    public string? Zone { get; set; }

    /// <summary>
    /// Der UNDURCHSICHTIGE Schlüssel der ausstehenden Herkunft einer Zeile aus dem Gebäudeimport
    /// (Stufe G4, Welle 4); <c>null</c> = keine. Quelle und Paarungen selbst bleiben in der Hülle —
    /// sie legt sie beim Speichern der Liste an das Modell, und erst der Speicherweg schreibt sie an
    /// die neue Projektkopie.
    /// </summary>
    public string? Herkunftsschluessel { get; set; }
}

/// <summary>
/// <b>Der Weg EINES Gebäudeimports aus dem Gebäudedialog</b> (Stufe G4, Welle 4; Architekturentscheid
/// A17: Überlagerung im Gebäudedialog). Die Hülle baut ihn je Klick auf „Importieren…" neu; die
/// Kern-Daten des Laufs bleiben in ihr, der Dialog sieht nur Parametersätze und die neue Zeile.
///
/// <para>Der Ablauf: <see cref="Gaben"/> öffnet den Zuordnungsdialog (samt dem
/// <c>Uebernehmen</c>-Delegat des Wirts). Nach seinem OK liefert <see cref="EditorGaben"/> den
/// Parametersatz des vorbelegten Gebäudeeditors im Modus Neu. Hat der Anwender dort gespeichert,
/// liefert <see cref="Aufnehmen"/> die neue Projektzeile samt
/// <see cref="GebaeudeProjektZeile.Herkunftsschluessel"/> — derselbe Weg wie „In das Projekt
/// übernehmen", geschrieben mit dem OK des Gebäudedialogs.</para>
/// </summary>
public sealed class GebaeudeImportweg
{
    /// <summary>Legt den Weg an.</summary>
    public GebaeudeImportweg(IReadOnlyDictionary<string, object> gaben,
                             Func<IReadOnlyDictionary<string, object>?> editorGaben,
                             Func<GebaeudeProjektZeile?> aufnehmen)
    {
        Gaben = gaben ?? new Dictionary<string, object>();
        EditorGaben = editorGaben ?? (() => null);
        Aufnehmen = aufnehmen ?? (() => null);
    }

    /// <summary>Der Parametersatz des Zuordnungsdialogs — ohne <c>Geschlossen</c>, mit dem <c>Uebernehmen</c> des Wirts.</summary>
    public IReadOnlyDictionary<string, object> Gaben { get; }

    /// <summary>Nach dem OK des Zuordnungsdialogs: der Parametersatz des vorbelegten Editors im Modus Neu; <c>null</c> = keiner.</summary>
    public Func<IReadOnlyDictionary<string, object>?> EditorGaben { get; }

    /// <summary>Nach dem Speichern im Editor: die neue Projektzeile mit Herkunftsschlüssel; <c>null</c> = kein neuer Katalogsatz.</summary>
    public Func<GebaeudeProjektZeile?> Aufnehmen { get; }
}

/// <summary>
/// Der Detailblock zu einem KATALOGSATZ (iU9-W9.2) —
/// <c>listBox_Gebaeude_DB_SelectedIndexChanged</c>:574-602.
/// </summary>
/// <param name="Name">Der Bezeichner.</param>
/// <param name="Art">Die Gebäudeart.</param>
/// <param name="Beschreibung">Die Beschreibung.</param>
/// <param name="Wohnflaeche">Die Gesamtfläche als Text.</param>
/// <param name="Rechenweg">Der Rechenweg als Anzeigetext (Stufe G1).</param>
/// <param name="HgesWK">Der Wärmeleitwert H_ges [W/K]; <c>null</c> = nicht bekannt.</param>
public sealed record GebaeudeStammDetail(
    string Name, string Art, string Beschreibung, string Wohnflaeche,
    string Rechenweg = "", double? HgesWK = null);
