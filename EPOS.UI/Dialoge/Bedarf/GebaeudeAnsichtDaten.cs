namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  Die DTO der Grundrissansicht (Gebäudesimulation G6c, Welle D; Entscheid E11,
//  Mehrzonenkonzept 6.7, Softwarearchitektur 1.2: GebaeudeAnsicht.razor + GebaeudeAnsichtDaten.cs).
//
//  Die Komponente kennt keine Fachklasse des Kerns: Sie bekommt je Geschoss die Räume als
//  Polygone in Metern, je Zone Schlüssel, Name, Stelle und Beheizung, und die Herkunft als
//  WERT — „schematisch" ist Pflicht am Bild und wird über Schematisch gezeigt, nie über einen
//  Anzeigetext entschieden. Ein Klick meldet die Kennung des Raums und den Schlüssel der Zone,
//  nie einen Namen (Mehrzonenkonzept 6.4, „kein Anzeigetext ist Steuerwert").
// =====================================================================================

/// <summary>
/// Ein Punkt des Grundrisses in Metern: x nach Osten, y nach Norden der Grundrissebene (die
/// Ebene der Datei; schematisch Nord oben). Eine SVG-Zeichnung spiegelt y.
/// </summary>
/// <param name="X">Ost [m].</param>
/// <param name="Y">Nord [m].</param>
public readonly record struct GebaeudeAnsichtPunkt(double X, double Y);

/// <summary>
/// Ein Raum im Grundriss: die Kennung der Datei (der Rückweg eines Klicks), der Name, die Zone als
/// Schlüssel (<c>null</c> = in keiner Zone — grau), die Beheizung, die Herkunft des Umrisses, die
/// Fläche als Anzeigetext und die Polygone (gegen den Uhrzeigersinn, ohne Schlusspunkt).
/// </summary>
/// <param name="Kennung">Raumkennung der Datei.</param>
/// <param name="Name">Anzeigename (Name, sonst Kennung).</param>
/// <param name="Zone">Schlüssel der Zone (<see cref="GebaeudeAnsichtZone.Schluessel"/>); <c>null</c> = keine.</param>
/// <param name="Beheizt">Wirksam beheizt, samt den Haken der Raumliste.</param>
/// <param name="Schematisch">Der Umriss ist aus Fläche und Seitenverhältnis gebildet, die Lage erfunden.</param>
/// <param name="Flaeche">Die Fläche mit Einheit.</param>
/// <param name="Polygone">Die Polygone des Raums; leer = nicht im Grundriss (weder Raumgrenzen noch Fläche).</param>
public sealed record GebaeudeAnsichtRaum(
    string Kennung, string Name, string? Zone, bool Beheizt, bool Schematisch, string Flaeche,
    IReadOnlyList<IReadOnlyList<GebaeudeAnsichtPunkt>> Polygone);

/// <summary>
/// Ein Geschoss des Grundrisses: Kennung, Name, ob es schematische Umrisse trägt, seine Ausdehnung in
/// Metern (für den Zeichenausschnitt) und die Räume in der Reihenfolge der Datei. Die Geschosse stehen
/// nach ihrer Höhenlage, sonst nach Name; ein Geschoss mit leerer Kennung fasst die Räume ohne Geschoss.
/// </summary>
/// <param name="Kennung">Kennung der Datei; leer = ohne Geschoss.</param>
/// <param name="Name">Name, sonst Kennung; leer = ohne Geschoss (die Komponente beschriftet es).</param>
/// <param name="Schematisch">Trägt das Geschoss schematische Umrisse?</param>
/// <param name="MinX">Westlichster Punkt [m].</param>
/// <param name="MinY">Südlichster Punkt [m].</param>
/// <param name="MaxX">Östlichster Punkt [m].</param>
/// <param name="MaxY">Nördlichster Punkt [m].</param>
/// <param name="Raeume">Die Räume des Geschosses.</param>
public sealed record GebaeudeAnsichtGeschoss(
    string Kennung, string Name, bool Schematisch, double MinX, double MinY, double MaxX, double MaxY,
    IReadOnlyList<GebaeudeAnsichtRaum> Raeume);

/// <summary>
/// Eine Zone des Grundrisses: der sprachneutrale Schlüssel (Ziel einer Zuordnung), der Name, die
/// Stelle (für die Farbe je Zone), die Beheizung, die Herkunft und ob eine Zuordnung von Hand sie
/// gebildet oder verändert hat.
/// </summary>
/// <param name="Schluessel">Der Schlüssel der Zone — zurück in eine Zuordnung von Hand.</param>
/// <param name="Name">Der Name der Zone, wie ihn auch die Zonenliste zeigt.</param>
/// <param name="Stelle">Die Stelle in der Rangfolge der Zonen (0, 1, …).</param>
/// <param name="Beheizt">Beheizt oder frei schwingend.</param>
/// <param name="Schematisch">Mindestens einer ihrer Räume ist schematisch.</param>
/// <param name="VonHand">Hat eine Zuordnung von Hand sie gebildet oder verändert?</param>
public sealed record GebaeudeAnsichtZone(string Schluessel, string Name, int Stelle, bool Beheizt, bool Schematisch, bool VonHand);

/// <summary>
/// <b>Die Daten der Grundrissansicht</b>: die Geschosse mit ihren Räumen, die Zonen, ob irgendein
/// Umriss schematisch ist (dann steht „schematisch" sichtbar am Bild), ob Räume von Hand umgehängt
/// werden können (nur unter einer Regel mit mehreren Zonen) und die Hinweise der Geometrie als
/// Anzeigetexte.
/// </summary>
public sealed record GebaeudeAnsichtDaten
{
    /// <summary>Die Geschosse, die Räume tragen, in ihrer Reihenfolge.</summary>
    public IReadOnlyList<GebaeudeAnsichtGeschoss> Geschosse { get; init; } = Array.Empty<GebaeudeAnsichtGeschoss>();

    /// <summary>Die Zonen in Rangfolge; leer = ohne Zonierung.</summary>
    public IReadOnlyList<GebaeudeAnsichtZone> Zonen { get; init; } = Array.Empty<GebaeudeAnsichtZone>();

    /// <summary>Trägt der Grundriss einen schematischen Umriss? Dann ist die Kennzeichnung Pflicht.</summary>
    public bool Schematisch { get; init; }

    /// <summary>Lässt sich ein Raum von Hand einer anderen Zone zuordnen (Regel mit mehreren Zonen)?</summary>
    public bool Umhaengbar { get; init; }

    /// <summary>Die Hinweise der Geometrie (schematische Räume, Rechteckersatz, Räume ohne Umriss) als Anzeigetexte.</summary>
    public IReadOnlyList<string> Hinweise { get; init; } = Array.Empty<string>();
}
