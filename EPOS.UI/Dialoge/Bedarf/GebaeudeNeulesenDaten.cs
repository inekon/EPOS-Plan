namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  „Datei erneut lesen“ im Gebäudedialog (HottCAD-Verbund 4.3 und 6.5, HC-4; Entscheid E87 F3).
//
//  Die Geometrie eines importierten Gebäudes wird nicht gespeichert. Der Dialog bietet für ein
//  Gebäude mit Importquelle den Knopf „Datei erneut lesen…“; die Hülle lässt die Datei wählen,
//  prüft den SHA-256 gegen die Quelle und baut bei Übereinstimmung dieselbe GebaeudeAnsicht wie
//  der Import. Der Zustand lebt allein im Dialog; nichts wird geschrieben, der Dialog gilt danach
//  nicht als geändert. Die DTO kennen keine Fachklasse des Kerns.
// =====================================================================================

/// <summary>Was das erneute Lesen ergeben hat — der Wert der Hülle, nie ein Anzeigetext.</summary>
public enum GebaeudeNeulesezustand
{
    /// <summary>Die Datei stimmt mit der Importquelle überein; die Ansicht steht.</summary>
    Passend,

    /// <summary>Der SHA-256 weicht ab — keine Ansicht, die Dateiwahl wird erneut angeboten.</summary>
    HashAbweichend,

    /// <summary>Die Datei ließ sich nicht lesen.</summary>
    NichtLesbar,

    /// <summary>Die Datei hat nicht das Format der Quelle.</summary>
    FormatUnbekannt,

    /// <summary>Die Datei liegt über der Größengrenze dieser Plattform.</summary>
    ZuGross,

    /// <summary>Das Gebäude hat keine Importquelle.</summary>
    KeineQuelle,
}

/// <summary>
/// Die gespeicherte Importquelle eines Gebäudes, wie der Dialog sie zeigt: Dateiname, Format und
/// Importzeitpunkt (in der Anzeigekultur) samt der fertigen Zeile „Importquelle: …“.
/// </summary>
/// <param name="Dateiname">Nur der Name, nie ein Pfad — <c>Tab_Importquelle</c> speichert keinen.</param>
/// <param name="Formattext">Das Format als Anzeigetext (gbXML, IFC).</param>
/// <param name="Zeitpunkt">Der Zeitpunkt des Imports in der Anzeigekultur.</param>
/// <param name="Quellzeile">Die Zeile „Importquelle: Name (Format), importiert am …“.</param>
public sealed record GebaeudeImportquelleAngabe(string Dateiname, string Formattext, string Zeitpunkt, string Quellzeile);

/// <summary>Das Ergebnis eines Laufs „Datei erneut lesen“ für den Dialog.</summary>
public sealed class GebaeudeNeulesestand
{
    /// <summary>Der benannte Zustand.</summary>
    public GebaeudeNeulesezustand Zustand { get; init; }

    /// <summary>Die Hinweiszeile — bei <see cref="GebaeudeNeulesezustand.Passend"/> die Bestätigung, sonst der Grund.</summary>
    public string Hinweis { get; init; } = "";

    /// <summary>Woher die Zonen der Ansicht stammen (Regel der Datei, ohne Projektdatei und Handzuordnung); leer = nichts zu sagen.</summary>
    public string Zonenhinweis { get; init; } = "";

    /// <summary>Die Ansicht; allein bei <see cref="GebaeudeNeulesezustand.Passend"/>, sonst <c>null</c>.</summary>
    public GebaeudeAnsichtDaten? Ansicht { get; init; }

    /// <summary>Bietet der Dialog eine andere Dateiwahl an? Bei jedem Fehlzustand außer „keine Quelle“.</summary>
    public bool AndereDateiAnbieten => Zustand is not GebaeudeNeulesezustand.Passend and not GebaeudeNeulesezustand.KeineQuelle;
}
