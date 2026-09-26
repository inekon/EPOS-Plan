using WindowsFormsApplication1.MyResource;

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

    /// <summary>
    /// Das Geschoss mit der Kennung <paramref name="kennung"/>; ohne Treffer — auch ohne Wahl — die
    /// <b>Vorbelegung</b>: das unterste Geschoss, in dem ein Raum einen Umriss trägt, ohne jeden Umriss das
    /// unterste. <c>null</c> ohne Geschosse. Ansicht und Wirt fragen dieselbe Stelle, damit die Raumwahl
    /// neben dem Bild dasselbe Geschoss meint wie das Bild.
    /// </summary>
    /// <param name="kennung">Die gewählte Geschosskennung (leer = ohne Geschoss); <c>null</c> = keine Wahl.</param>
    public GebaeudeAnsichtGeschoss? GeschossOderVorgabe(string? kennung)
    {
        if (kennung is not null)
            foreach (GebaeudeAnsichtGeschoss g in Geschosse)
                if (string.Equals(g.Kennung, kennung, StringComparison.Ordinal)) return g;
        foreach (GebaeudeAnsichtGeschoss g in Geschosse)
            if (g.Raeume.Any(r => r.Polygone.Count > 0)) return g;
        return Geschosse.Count > 0 ? Geschosse[0] : null;
    }

    /// <summary>Die Zone mit dem Schlüssel <paramref name="schluessel"/>; <c>null</c> ohne Treffer oder ohne Schlüssel.</summary>
    public GebaeudeAnsichtZone? Zone(string? schluessel)
    {
        if (schluessel is null) return null;
        foreach (GebaeudeAnsichtZone z in Zonen)
            if (string.Equals(z.Schluessel, schluessel, StringComparison.Ordinal)) return z;
        return null;
    }

    /// <summary>Der Raum mit der Kennung <paramref name="kennung"/> in irgendeinem Geschoss; <c>null</c> ohne Treffer.</summary>
    public GebaeudeAnsichtRaum? Raum(string? kennung)
    {
        if (kennung is null) return null;
        foreach (GebaeudeAnsichtGeschoss g in Geschosse)
            foreach (GebaeudeAnsichtRaum r in g.Raeume)
                if (string.Equals(r.Kennung, kennung, StringComparison.Ordinal)) return r;
        return null;
    }
}

/// <summary>
/// <b>Das Textbündel der Grundrissansicht</b> (Hausregel ab etwa zehn Texten). Beschriftungen, kein
/// Zustand; je Eigenschaft der Ressourcenschlüssel im Kommentar, der deutsche Rückfall ist der
/// Ressourcentext selbst. Ein Wirt, der seine Texte in einer anderen Sprache baut, reicht das Bündel
/// mit herein — sonst gilt die Oberflächensprache beim Anlegen der Komponente.
/// </summary>
public sealed class GebaeudeAnsichtTexte
{
    /// <summary>GIMP_ANS_ANSICHT — Beschriftung des Umschalters „Grundriss | Körper" für die Sprachausgabe.</summary>
    public string Ansicht { get; set; } = Resource.GIMP_ANS_ANSICHT;

    /// <summary>GIMP_ANS_GRUNDRISS</summary>
    public string Grundriss { get; set; } = Resource.GIMP_ANS_GRUNDRISS;

    /// <summary>GIMP_ANS_KOERPER</summary>
    public string Koerper { get; set; } = Resource.GIMP_ANS_KOERPER;

    /// <summary>GIMP_ANS_KOERPER_GESPERRT — Grund der weichen Sperre von „Körper".</summary>
    public string KoerperGesperrt { get; set; } = Resource.GIMP_ANS_KOERPER_GESPERRT;

    /// <summary>GIMP_ANS_GESCHOSSE — Beschriftung der Geschosswahl für die Sprachausgabe.</summary>
    public string Geschosse { get; set; } = Resource.GIMP_ANS_GESCHOSSE;

    /// <summary>GIMP_ANS_OHNE_GESCHOSS — das Geschoss der Räume ohne Geschoss.</summary>
    public string OhneGeschoss { get; set; } = Resource.GIMP_ANS_OHNE_GESCHOSS;

    /// <summary>GIMP_ANS_GESCHOSS_SCHEMATISCH — Reiter eines schematischen Geschosses, {0} = Geschoss.</summary>
    public string GeschossSchematisch { get; set; } = Resource.GIMP_ANS_GESCHOSS_SCHEMATISCH;

    /// <summary>GIMP_ANS_SCHEMATISCH — die Kennzeichnung am Bild, in der Legende und je Raum.</summary>
    public string Schematisch { get; set; } = Resource.GIMP_ANS_SCHEMATISCH;

    /// <summary>GIMP_ANS_SCHEMATISCH_HINWEIS — die Zeile über einem schematischen Geschoss.</summary>
    public string SchematischHinweis { get; set; } = Resource.GIMP_ANS_SCHEMATISCH_HINWEIS;

    /// <summary>GIMP_ANS_RAUMGRENZEN — Herkunft einer Zone, deren Umrisse aus Raumgrenzen stammen.</summary>
    public string Raumgrenzen { get; set; } = Resource.GIMP_ANS_RAUMGRENZEN;

    /// <summary>GIMP_ANS_VON_HAND — eine Zone, die eine Zuordnung von Hand gebildet oder verändert hat.</summary>
    public string VonHand { get; set; } = Resource.GIMP_ANS_VON_HAND;

    /// <summary>GIMP_ANS_UNBEHEIZT</summary>
    public string Unbeheizt { get; set; } = Resource.GIMP_ANS_UNBEHEIZT;

    /// <summary>GIMP_ANS_OHNE_ZONE — der graue Eintrag der Legende.</summary>
    public string OhneZone { get; set; } = Resource.GIMP_ANS_OHNE_ZONE;

    /// <summary>GIMP_ANS_LEGENDE — Beschriftung der Legende für die Sprachausgabe.</summary>
    public string Legende { get; set; } = Resource.GIMP_ANS_LEGENDE;

    /// <summary>GIMP_ANS_BILD — Beschriftung des Bildes für die Sprachausgabe, {0} = Geschoss.</summary>
    public string Bild { get; set; } = Resource.GIMP_ANS_BILD;

    /// <summary>GIMP_ANS_RAUM — Beschreibung eines Raums, {0} = Name, {1} = Fläche, {2} = Zone.</summary>
    public string Raum { get; set; } = Resource.GIMP_ANS_RAUM;

    /// <summary>GIMP_ANS_RAUM_OHNE_ZONE — Beschreibung eines Raums ohne Zone, {0} = Name, {1} = Fläche.</summary>
    public string RaumOhneZone { get; set; } = Resource.GIMP_ANS_RAUM_OHNE_ZONE;

    /// <summary>GIMP_ANS_ZUORDNEN — was ein Klick tut, {0} = die gewählte Zone.</summary>
    public string Zuordnen { get; set; } = Resource.GIMP_ANS_ZUORDNEN;

    /// <summary>GIMP_ANS_ABTRENNEN — was ein Klick ohne gewählte Zone tut.</summary>
    public string Abtrennen { get; set; } = Resource.GIMP_ANS_ABTRENNEN;

    /// <summary>GIMP_ANS_KEINE_UMRISSE — ein Geschoss, in dem kein Raum einen Umriss trägt.</summary>
    public string KeineUmrisse { get; set; } = Resource.GIMP_ANS_KEINE_UMRISSE;

    /// <summary>GIMP_ANS_NUR_ANZEIGE — der Hinweis, wenn sich nichts zuordnen lässt.</summary>
    public string NurAnzeige { get; set; } = Resource.GIMP_ANS_NUR_ANZEIGE;

    /// <summary>GIMP_ANS_LEER — ohne Daten.</summary>
    public string Leer { get; set; } = Resource.GIMP_ANS_LEER;
}
