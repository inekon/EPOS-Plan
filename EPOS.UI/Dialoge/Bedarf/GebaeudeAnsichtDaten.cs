using System.Buffers.Binary;
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

    /// <summary>Die Körperangaben je Raum (Höhe, Bauteile an Kante, Boden, Decke); fehlt ein Raum, bekommt er die Vorgabe.</summary>
    public IReadOnlyList<GebaeudeAnsichtKoerperraum> Koerperraeume { get; init; } = Array.Empty<GebaeudeAnsichtKoerperraum>();

    /// <summary>Die Höhenlagen der Geschosse; fehlt ein Geschoss oder seine Lage, wird es gestapelt.</summary>
    public IReadOnlyList<GebaeudeAnsichtGeschosslage> Geschosslagen { get; init; } = Array.Empty<GebaeudeAnsichtGeschosslage>();

    /// <summary>
    /// Der Bezugspunkt der Dateikörper [m] (x, y, z im Modellsystem): der kleinste Punkt aller Körper je Achse.
    /// Die Punkte eines <see cref="GebaeudeAnsichtDateikoerper"/> stehen relativ zu ihm — georeferenzierte Dateien
    /// tragen Koordinaten um 10⁶ m, ein <c>float</c> verlöre dort die Zentimeter. Ohne Dateikörper (0, 0, 0).
    /// </summary>
    public IReadOnlyList<double> Bezugspunkt { get; init; } = new double[3];

    /// <summary>Die Dreiecksgrenze der Dateikörper je Gebäude (<see cref="DREIECKSGRENZE"/>; Tests setzen eine kleinere).</summary>
    public int Dreiecksgrenze { get; init; } = DREIECKSGRENZE;

    /// <summary>
    /// Die Dreiecksgrenze je Gebäude (Datenaustauschkonzept 15.4, gleich der Grenze des Kerns): Darüber zeigt die
    /// Ansicht für alle Räume das Prisma aus dem Umriss, mit benanntem Hinweis — auf allen Plattformen gleich.
    /// </summary>
    public const int DREIECKSGRENZE = 300_000;

    /// <summary>Trägt mindestens ein Raum einen Körper aus der Datei? Ohne einen einzigen entfällt der Umschalter.</summary>
    public bool HatDateikoerper => Koerperraeume.Any(k => k.Dateikoerper is not null);

    /// <summary>Die Dreiecke aller Dateikörper der Räume (ohne die Bauteilkörper).</summary>
    public long DateikoerperDreiecke => Koerperraeume.Sum(k => (long)(k.Dateikoerper?.DreieckZahl ?? 0));

    /// <summary>
    /// Die Flächengruppen nach Randbedingung (HottCAD-Verbund 4.1, 4.3) in der Reihenfolge R0 bis R7, alle acht, je Gruppe
    /// die Flächensumme der Bilanz und die Zahl der Dreiecke der Raumkörper; leer = keine Klassifikation (gbXML ohne Körper,
    /// Prismenrückfall). Anzeige, keine Rechengröße.
    /// </summary>
    public IReadOnlyList<GebaeudeAnsichtFlaechengruppe> Flaechengruppen { get; init; } = Array.Empty<GebaeudeAnsichtFlaechengruppe>();

    /// <summary>Die Körper der Hüllbauteile, Fenster und Türen mit ihrer Gruppe (3.2, 4.3); Punkte relativ zum <see cref="Bezugspunkt"/>.</summary>
    public IReadOnlyList<GebaeudeAnsichtBauteilkoerper> Bauteilkoerper { get; init; } = Array.Empty<GebaeudeAnsichtBauteilkoerper>();

    /// <summary>
    /// Hat die Hülle die Gruppen verworfen, weil Raum- und Bauteilkörper zusammen die <see cref="Dreiecksgrenze"/> überschreiten?
    /// Dann ist „Randbedingung“ benannt nicht wählbar (4.4: Prismenrückfall ohne Gruppen).
    /// </summary>
    public bool RandgruppenZuGross { get; init; }

    /// <summary>Trägt das Gebäude eine Klassifikation nach Randbedingung?</summary>
    public bool HatRandgruppen => Flaechengruppen.Count > 0;

    /// <summary>
    /// Ist der Farbmodus „Randbedingung“ wählbar? Nur mit Klassifikation und unter der Dreiecksgrenze — sonst stehen die
    /// Räume als Prismen ohne Gruppen da, und der Umschalter nennt den Grund.
    /// </summary>
    public bool RandbedingungWaehlbar => HatRandgruppen && !RandgruppenZuGross && !DateikoerperZuGross;

    /// <summary>Die Gruppe <paramref name="g"/> der Legende; <c>null</c> ohne Klassifikation.</summary>
    public GebaeudeAnsichtFlaechengruppe? Flaechengruppe(Randgruppe g)
    {
        foreach (GebaeudeAnsichtFlaechengruppe x in Flaechengruppen)
            if (x.Gruppe == g) return x;
        return null;
    }

    /// <summary>Überschreiten die Dateikörper die <see cref="Dreiecksgrenze"/>? Dann zeigt die Ansicht die Prismen.</summary>
    public bool DateikoerperZuGross => DateikoerperDreiecke > Dreiecksgrenze;

    /// <summary>
    /// <b>Die Dateikörper als Bytefeld</b> für das Modul (15.4): je Raum mit Dateikörper, in der Reihenfolge von
    /// <see cref="Koerperraeume"/>, erst die Punkte (float32, je Punkt x, y, z relativ zum <see cref="Bezugspunkt"/>),
    /// dann die Dreiecke (int32, Indextripel), dann die Randkanten (int32, Indexpaare) — Little-Endian, jeder Abschnitt
    /// auf vier Byte ausgerichtet. Daneben das Verzeichnis mit den Byte-Offsets und Zahlen je Raum. Dieselben Daten
    /// geben dasselbe Feld, byteweise.
    /// </summary>
    /// <remarks>
    /// <b>Aufbau mit Randgruppen</b> (HottCAD-Verbund 4.3, HC-2) — der Vertrag mit dem Modul. Das Feld hat drei Teile,
    /// Little-Endian:
    /// <list type="number">
    /// <item><b>Raumkörper</b>, unverändert: je Raum mit Dateikörper Punkte (float32 ×3), Dreiecke (int32 ×3), Randkanten
    /// (int32 ×2) — <see cref="GebaeudeAnsichtKoerperfeld.Verzeichnis"/>.</item>
    /// <item><b>Bauteilkörper</b> in der Reihenfolge von <see cref="Bauteilkoerper"/>, gleich gebaut —
    /// <see cref="GebaeudeAnsichtKoerperfeld.Bauteile"/> mit Bauteilkennung und Gruppe (0–7) je Eintrag.</item>
    /// <item><b>Gruppenbytes</b>: je Raum des Verzeichnisses mit Gruppen ein Byte je Dreieck in der Reihenfolge seiner
    /// Dreiecke — 0 bis 7 = R0 bis R7, <see cref="GebaeudeAnsichtRandgruppen.KEINE"/> = ohne Gruppe (entartetes Dreieck);
    /// der Byte-Offset steht in <see cref="GebaeudeAnsichtKoerperfeldEintrag.GruppenAb"/> (−1 = keine). Das Feld endet auf
    /// vier Byte aufgefüllt.</item>
    /// </list>
    /// Teil 2 und 3 stehen hinter allen Abschnitten des ersten: Jeder Leser, der über die Offsets des Verzeichnisses geht, liest
    /// unverändert; ohne Klassifikation sind beide leer und das Feld ist byteweise das bisherige.
    /// </remarks>
    public GebaeudeAnsichtKoerperfeld Koerperfeld()
    {
        long laenge = 0;
        foreach (GebaeudeAnsichtKoerperraum k in Koerperraeume)
            if (k.Dateikoerper is { } d) laenge += 4L * (d.Punkte.Count + d.Dreiecke.Count + d.Randkanten.Count) + (d.Gruppen?.Count ?? 0);
        foreach (GebaeudeAnsichtBauteilkoerper b in Bauteilkoerper)
            laenge += 4L * (b.Koerper.Punkte.Count + b.Koerper.Dreiecke.Count + b.Koerper.Randkanten.Count);
        laenge = (laenge + 3) / 4 * 4;
        var bytes = new byte[checked((int)laenge)];
        var verzeichnis = new List<GebaeudeAnsichtKoerperfeldEintrag>();
        var raeume = new List<GebaeudeAnsichtDateikoerper>();
        int stelle = 0;
        foreach (GebaeudeAnsichtKoerperraum k in Koerperraeume)
        {
            if (k.Dateikoerper is not { } d) continue;
            verzeichnis.Add(Abschnitte(bytes, ref stelle, k.Kennung, d));
            raeume.Add(d);
        }
        var bauteile = new List<GebaeudeAnsichtKoerperfeldEintrag>();
        foreach (GebaeudeAnsichtBauteilkoerper b in Bauteilkoerper)
            bauteile.Add(Abschnitte(bytes, ref stelle, "", b.Koerper) with { Bauteil = b.Bauteil, Gruppe = (int)b.Gruppe });
        for (int i = 0; i < raeume.Count; i++)
        {
            if (raeume[i].Gruppen is not { } gruppen) continue;
            verzeichnis[i] = verzeichnis[i] with { GruppenAb = stelle };
            foreach (byte x in gruppen) bytes[stelle++] = x;
        }
        return new GebaeudeAnsichtKoerperfeld(bytes, verzeichnis) { Bauteile = bauteile };
    }

    /// <summary>Schreibt Punkte, Dreiecke und Randkanten eines Körpers ab <paramref name="stelle"/> und gibt den Eintrag.</summary>
    private static GebaeudeAnsichtKoerperfeldEintrag Abschnitte(byte[] bytes, ref int stelle, string raum, GebaeudeAnsichtDateikoerper d)
    {
        int punkteAb = stelle;
        foreach (float f in d.Punkte) { BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(stelle, 4), f); stelle += 4; }
        int dreieckeAb = stelle;
        foreach (int i in d.Dreiecke) { BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(stelle, 4), i); stelle += 4; }
        int kantenAb = stelle;
        foreach (int i in d.Randkanten) { BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(stelle, 4), i); stelle += 4; }
        return new GebaeudeAnsichtKoerperfeldEintrag(
            raum, punkteAb, d.Punkte.Count / 3, dreieckeAb, d.Dreiecke.Count / 3, kantenAb, d.Randkanten.Count / 2);
    }

    /// <summary>Die Raumhöhe, wenn die Datei keine nennt [m] — der Körper ist dann schematisch.</summary>
    public const double VORGABEHOEHE_M = 3.0;

    /// <summary>
    /// <b>Die Körper der Ansicht</b>: je Raum mit Umriss ein Körper. Die Unterkante ist die Höhenlage des
    /// Geschosses; ohne sie stehen die Geschosse in ihrer Reihenfolge übereinander — Unterkante des Vorgängers
    /// plus dessen höchster Raum (das erste ohne Lage bei 0 m). Die Höhe ist die des Raums, ohne sie
    /// <see cref="VORGABEHOEHE_M"/> und schematisch. Gerechnet wird nichts weiter; dieselben Daten geben dieselben Körper.
    /// </summary>
    public IReadOnlyList<GebaeudeAnsichtKoerper> Koerper()
    {
        var koerper = new List<GebaeudeAnsichtKoerper>();
        double? vorigeUnterkante = null;
        double vorigeHoehe = 0;
        foreach (GebaeudeAnsichtGeschoss g in Geschosse)
        {
            double? lage = null;
            foreach (GebaeudeAnsichtGeschosslage l in Geschosslagen)
                if (string.Equals(l.Kennung, g.Kennung, StringComparison.Ordinal)) { lage = l.LageM; break; }
            double unterkante = lage ?? (vorigeUnterkante is double v ? v + vorigeHoehe : 0);
            double hoechste = 0;
            foreach (GebaeudeAnsichtRaum r in g.Raeume)
            {
                GebaeudeAnsichtKoerperraum? angaben = Koerperraum(r.Kennung);
                bool vorgabe = angaben?.HoeheM is not (> 0);
                double hoehe = vorgabe ? VORGABEHOEHE_M : angaben!.HoeheM!.Value;
                if (r.Polygone.Count == 0) continue;
                hoechste = Math.Max(hoechste, hoehe);
                koerper.Add(new GebaeudeAnsichtKoerper(r, unterkante, hoehe, vorgabe, angaben));
            }
            vorigeUnterkante = unterkante;
            vorigeHoehe = hoechste > 0 ? hoechste : VORGABEHOEHE_M;
        }
        return koerper;
    }

    /// <summary>
    /// <b>Die Räume der Ansicht „Dateikörper"</b> (15.4) in der Reihenfolge der Geschosse und Räume: ein Raum mit
    /// Dateikörper als <see cref="Koerperherkunft.Datei"/>, jeder andere mit Umriss als Prisma wie in
    /// <see cref="Koerper"/> — <see cref="Koerperherkunft.Schematisch"/>, wenn das Prisma schematisch ist, sonst
    /// <see cref="Koerperherkunft.Umriss"/>. Ist <see cref="DateikoerperZuGross"/>, stehen alle Räume als Prisma da
    /// (benannte Vereinfachung). Ein Raum ohne Dateikörper und ohne Umriss fehlt.
    /// </summary>
    public IReadOnlyList<GebaeudeAnsichtDateiraum> Dateiansicht()
    {
        bool zuGross = DateikoerperZuGross;
        var prismen = new Dictionary<string, GebaeudeAnsichtKoerper>(StringComparer.Ordinal);
        foreach (GebaeudeAnsichtKoerper k in Koerper()) prismen.TryAdd(k.Raum.Kennung, k);
        var raeume = new List<GebaeudeAnsichtDateiraum>();
        foreach (GebaeudeAnsichtGeschoss g in Geschosse)
            foreach (GebaeudeAnsichtRaum r in g.Raeume)
            {
                GebaeudeAnsichtDateikoerper? datei = zuGross ? null : Koerperraum(r.Kennung)?.Dateikoerper;
                if (datei is not null)
                    raeume.Add(new GebaeudeAnsichtDateiraum(r, Koerperherkunft.Datei, datei, null));
                else if (prismen.TryGetValue(r.Kennung, out GebaeudeAnsichtKoerper? prisma))
                    raeume.Add(new GebaeudeAnsichtDateiraum(
                        r, prisma.Schematisch ? Koerperherkunft.Schematisch : Koerperherkunft.Umriss, null, prisma));
            }
        return raeume;
    }

    /// <summary>Die Körperangaben des Raums <paramref name="kennung"/>; <c>null</c> ohne Treffer.</summary>
    public GebaeudeAnsichtKoerperraum? Koerperraum(string? kennung)
    {
        if (kennung is null) return null;
        foreach (GebaeudeAnsichtKoerperraum k in Koerperraeume)
            if (string.Equals(k.Kennung, kennung, StringComparison.Ordinal)) return k;
        return null;
    }

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
/// Die Körperangaben eines Raums für die Ansicht „Körper" (Gebäudesimulation G7b; Datenaustauschkonzept 14.2):
/// die Raumhöhe aus dem Zonengeometrie-Modell und je Kante, Boden und Decke die Art des Bauteils, das dort steht.
/// Polygon, Zone und Herkunft stehen schon im <see cref="GebaeudeAnsichtRaum"/> derselben Kennung — sie werden
/// hier nicht wiederholt. Eine Platte zeigt die Ansicht nur, wo die Datei ein Bauteil kennt; sonst den nackten Körper.
/// </summary>
/// <param name="Kennung">Raumkennung der Datei (dieselbe wie <see cref="GebaeudeAnsichtRaum.Kennung"/>).</param>
/// <param name="HoeheM">Die Raumhöhe [m]; <c>null</c> = unbekannt (dann die Vorgabe, schematisch).</param>
/// <param name="Kanten">Je Polygon je Kante die Bauteilart als sprachneutraler Schlüssel; <c>null</c> = kein Bauteil bekannt.</param>
/// <param name="Boden">Die Bauteilart des Bodens; <c>null</c> = keines bekannt.</param>
/// <param name="Decke">Die Bauteilart der Decke; <c>null</c> = keines bekannt.</param>
public sealed record GebaeudeAnsichtKoerperraum(
    string Kennung, double? HoeheM, IReadOnlyList<IReadOnlyList<string?>> Kanten, string? Boden, string? Decke)
{
    /// <summary>Der Körper des Raums aus der Datei (15.3); <c>null</c> = keiner (dann das Prisma aus dem Umriss).</summary>
    public GebaeudeAnsichtDateikoerper? Dateikoerper { get; init; }

    /// <summary>Woher der Körper des Raums stammt — als Wert, nie als Text.</summary>
    public Koerperherkunft Herkunft { get; init; } = Koerperherkunft.Umriss;

    /// <summary>
    /// Je Polygon je Kante die Gruppe ihrer senkrechten Flächen (R0 bis R3, HottCAD-Verbund 4.3) — die flächengrößte, wenn
    /// mehrere an der Kante liegen; <c>null</c> = keine Fläche des Körpers an der Kante. <c>null</c> insgesamt = ohne Gruppen.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<Randgruppe?>>? Kantengruppen { get; init; }

    /// <summary>Die Fenster und Türen (R7) als Marken auf den Kanten; leer = keine.</summary>
    public IReadOnlyList<GebaeudeAnsichtKantenmarke> Kantenmarken { get; init; } = Array.Empty<GebaeudeAnsichtKantenmarke>();

    /// <summary>Die Gruppe der Bodenfläche (die flächengrößte der Flächen nach unten); <c>null</c> = keine bekannt.</summary>
    public Randgruppe? Bodengruppe { get; init; }
}

/// <summary>
/// Die Gruppe einer Fläche nach Randbedingung (HottCAD-Verbund 4.1) — Spiegel der <c>Flaechengruppe</c> des Kerns mit denselben
/// Zahlen; der Zahlwert ist der Gruppenindex im Bytefeld und die Stelle in <see cref="GebaeudeAnsichtRandgruppen.FARBEN"/>.
/// </summary>
public enum Randgruppe : byte
{
    /// <summary>Innen, thermisch neutral.</summary>
    R0 = 0,
    /// <summary>Wand beheizt gegen außen.</summary>
    R1 = 1,
    /// <summary>Wand beheizt gegen unbeheizt.</summary>
    R2 = 2,
    /// <summary>Wand oder Boden gegen Erdreich.</summary>
    R3 = 3,
    /// <summary>Boden gegen unbeheizt oder außen.</summary>
    R4 = 4,
    /// <summary>Decke oder Dach gegen außen.</summary>
    R5 = 5,
    /// <summary>Decke gegen unbeheizt.</summary>
    R6 = 6,
    /// <summary>Fenster und Türen nach außen oder gegen unbeheizt.</summary>
    R7 = 7,
}

/// <summary>
/// <b>Die Farbtafel der Gruppen</b> — die eine Stelle, an der die Farben R0 bis R7 stehen (HottCAD-Verbund 4.1). Der Grundriss
/// zeichnet mit ihr, die Szene des Moduls trägt sie als <c>randfarben</c> mit. Gut unterscheidbar, auf hellem und dunklem
/// Grund lesbar; R0 grau (in 3D halbtransparent).
/// </summary>
public static class GebaeudeAnsichtRandgruppen
{
    /// <summary>Die Zahl der Gruppen.</summary>
    public const int ZAHL = 8;

    /// <summary>Das Gruppenbyte eines Dreiecks ohne Gruppe.</summary>
    public const byte KEINE = 255;

    /// <summary>Die Farben nach dem Gruppenindex: grau, rot, orange, braun, gelb, blau, hellblau, türkis.</summary>
    public static readonly IReadOnlyList<string> FARBEN = new[]
    {
        "#9e9e9e", "#d62728", "#ff7f0e", "#8c564b", "#e5c100", "#1f5fbf", "#7ec8f2", "#17becf",
    };

    /// <summary>Die Farbe einer Gruppe.</summary>
    public static string Farbe(Randgruppe g) => FARBEN[(int)g];

    /// <summary>Steht die Gruppe im Grundriss nur in Legende und Bilanz (Decken R5, R6)?</summary>
    public static bool NurLegende(Randgruppe g) => g is Randgruppe.R5 or Randgruppe.R6;
}

/// <summary>Eine Gruppe der Legende: Gruppe, Flächensumme der Bilanz [m²] und Zahl der Dreiecke der Raumkörper.</summary>
/// <param name="Gruppe">Die Gruppe.</param>
/// <param name="FlaecheM2">Die Flächensumme der Bilanz [m²] (R7 aus den Öffnungsflächen des Mengensatzes).</param>
/// <param name="DreieckZahl">Die Zahl der Dreiecke der Raumkörper in dieser Gruppe.</param>
public sealed record GebaeudeAnsichtFlaechengruppe(Randgruppe Gruppe, double FlaecheM2, int DreieckZahl);

/// <summary>Der Körper eines Bauteils, Fensters oder einer Tür mit seiner Gruppe (Punkte relativ zum Bezugspunkt).</summary>
/// <param name="Bauteil">Die Bauteilkennung der Datei.</param>
/// <param name="Gruppe">Die Gruppe des Bauteils.</param>
/// <param name="Koerper">Der Körper.</param>
public sealed record GebaeudeAnsichtBauteilkoerper(string Bauteil, Randgruppe Gruppe, GebaeudeAnsichtDateikoerper Koerper);

/// <summary>Eine Öffnung (R7) als Marke auf einer Kante des Grundrisses: Polygon, Kante und der Abschnitt als Anteil 0…1.</summary>
/// <param name="Polygon">Die Stelle des Polygons im Raum.</param>
/// <param name="Kante">Die Kante (von Punkt <c>Kante</c> nach <c>Kante + 1</c>).</param>
/// <param name="Von">Anfang der Marke als Anteil der Kantenlänge.</param>
/// <param name="Bis">Ende der Marke als Anteil der Kantenlänge.</param>
/// <param name="Bauteil">Die Kennung der Öffnung.</param>
public sealed record GebaeudeAnsichtKantenmarke(int Polygon, int Kante, double Von, double Bis, string Bauteil);

/// <summary>Die Herkunft des Körpers eines Raums (Datenaustauschkonzept 15.4) — der Steuerwert der Kennzeichen.</summary>
public enum Koerperherkunft
{
    /// <summary>Der Raum trägt einen Körper aus der Datei.</summary>
    Datei,

    /// <summary>Prisma aus den Raumgrenzen (Umriss um die Raumhöhe extrudiert).</summary>
    Umriss,

    /// <summary>Prisma aus einem erfundenen Umriss (Fläche und Seitenverhältnis).</summary>
    Schematisch,
}

/// <summary>
/// Der Körper eines Raums, wie die Datei ihn zeichnet (15.3, 15.4), für die Ansicht: die Punkte als Folge
/// (x, y, z, x, y, z, …) in Metern <b>relativ zum <see cref="GebaeudeAnsichtDaten.Bezugspunkt"/></b>, die Dreiecke
/// als Indextripel, die Randkanten der Ursprungsflächen als Indexpaare (keine Triangulationsdiagonalen), dazu die
/// gelesene Darstellungsart und die Vermerke als Schlüssel (Namen von <c>Koerpervermerk</c>; leer = exakt gelesen).
/// </summary>
/// <param name="Punkte">Die Punkte relativ zum Bezugspunkt [m], drei Werte je Punkt.</param>
/// <param name="Dreiecke">Die Dreiecke, drei Indizes je Dreieck.</param>
/// <param name="Randkanten">Die Randkanten, zwei Indizes je Kante.</param>
/// <param name="DreieckZahl">Die Zahl der Dreiecke.</param>
/// <param name="Art">Die Darstellungsart als Schlüssel (etwa <c>FacetedBrep</c>).</param>
/// <param name="Vermerke">Die Vermerke als Schlüssel, aufsteigend.</param>
public sealed record GebaeudeAnsichtDateikoerper(
    IReadOnlyList<float> Punkte, IReadOnlyList<int> Dreiecke, IReadOnlyList<int> Randkanten, int DreieckZahl,
    string Art, IReadOnlyList<string> Vermerke)
{
    /// <summary>
    /// Je Dreieck der Gruppenindex (0–7 = <see cref="Randgruppe"/>, <see cref="GebaeudeAnsichtRandgruppen.KEINE"/> = ohne);
    /// <c>null</c> = ohne Klassifikation. Geht als dritter Teil ins <see cref="GebaeudeAnsichtDaten.Koerperfeld"/>.
    /// </summary>
    public IReadOnlyList<byte>? Gruppen { get; init; }

    /// <summary>Die Schlüssel der Vermerke in ihrer Reihenfolge — die Namen von <c>Koerpervermerk</c> des Kerns.</summary>
    public static readonly string[] VERMERKE = { "Bogen", "Loch", "Uneben", "OhneBeschnitt", "Offen", "Mehrschale" };
}

/// <summary>
/// Ein Raum der Ansicht „Dateikörper" (<see cref="GebaeudeAnsichtDaten.Dateiansicht"/>): der Raum, die gezeigte
/// Herkunft, der Dateikörper (bei <see cref="Koerperherkunft.Datei"/>) oder das Prisma aus dem Umriss.
/// </summary>
/// <param name="Raum">Der Raum mit Zone und Kennung.</param>
/// <param name="Herkunft">Die gezeigte Herkunft.</param>
/// <param name="Datei">Der Dateikörper; <c>null</c> beim Prisma.</param>
/// <param name="Prisma">Das Prisma aus dem Umriss; <c>null</c> beim Dateikörper.</param>
public sealed record GebaeudeAnsichtDateiraum(
    GebaeudeAnsichtRaum Raum, Koerperherkunft Herkunft, GebaeudeAnsichtDateikoerper? Datei, GebaeudeAnsichtKoerper? Prisma);

/// <summary>Die Dateikörper eines Gebäudes als ein Bytefeld samt Verzeichnis (<see cref="GebaeudeAnsichtDaten.Koerperfeld"/>).</summary>
/// <param name="Bytes">Das Feld: je Raum Punkte (float32), Dreiecke und Kanten (int32), Little-Endian.</param>
/// <param name="Verzeichnis">Je Raum mit Dateikörper Kennung, Byte-Offsets und Zahlen.</param>
public sealed record GebaeudeAnsichtKoerperfeld(byte[] Bytes, IReadOnlyList<GebaeudeAnsichtKoerperfeldEintrag> Verzeichnis)
{
    /// <summary>Je Bauteilkörper Bauteilkennung, Gruppe, Byte-Offsets und Zahlen (zweiter Teil des Felds).</summary>
    public IReadOnlyList<GebaeudeAnsichtKoerperfeldEintrag> Bauteile { get; init; } = Array.Empty<GebaeudeAnsichtKoerperfeldEintrag>();
}

/// <summary>Ein Eintrag des Verzeichnisses im Bytefeld: Raumkennung, je Abschnitt Byte-Offset und Zahl.</summary>
/// <param name="Raum">Die Raumkennung.</param>
/// <param name="PunkteAb">Byte-Offset der Punkte.</param>
/// <param name="PunktZahl">Zahl der Punkte (je drei float32).</param>
/// <param name="DreieckeAb">Byte-Offset der Dreiecke.</param>
/// <param name="DreieckZahl">Zahl der Dreiecke (je drei int32).</param>
/// <param name="KantenAb">Byte-Offset der Randkanten.</param>
/// <param name="KantenZahl">Zahl der Randkanten (je zwei int32).</param>
public sealed record GebaeudeAnsichtKoerperfeldEintrag(
    string Raum, int PunkteAb, int PunktZahl, int DreieckeAb, int DreieckZahl, int KantenAb, int KantenZahl)
{
    /// <summary>Byte-Offset der Gruppenbytes (ein Byte je Dreieck); −1 = keine.</summary>
    public int GruppenAb { get; init; } = -1;

    /// <summary>Die Bauteilkennung (Einträge der Bauteilkörper); <c>null</c> bei Räumen.</summary>
    public string? Bauteil { get; init; }

    /// <summary>Die Gruppe des Bauteilkörpers (0–7); −1 bei Räumen.</summary>
    public int Gruppe { get; init; } = -1;
}

/// <summary>Die Höhenlage eines Geschosses für die Körper; <c>null</c> = unbekannt (dann gestapelt).</summary>
/// <param name="Kennung">Kennung des Geschosses (<see cref="GebaeudeAnsichtGeschoss.Kennung"/>).</param>
/// <param name="LageM">Unterkante [m]; <c>null</c> = unbekannt.</param>
public sealed record GebaeudeAnsichtGeschosslage(string Kennung, double? LageM);

/// <summary>
/// Ein Körper, wie die Ansicht ihn zeichnet: Unterkante und Höhe in Metern und ob die Höhe die Vorgabe ist
/// (dann ist der Körper schematisch und so gekennzeichnet). Gebildet von <see cref="GebaeudeAnsichtDaten.Koerper"/>.
/// </summary>
/// <param name="Raum">Der Raum mit Polygon, Zone und Herkunft.</param>
/// <param name="UnterkanteM">Unterkante [m].</param>
/// <param name="HoeheM">Höhe [m].</param>
/// <param name="HoeheVorgabe">Die Höhe ist die Vorgabe <see cref="GebaeudeAnsichtDaten.VORGABEHOEHE_M"/>.</param>
/// <param name="Angaben">Die Körperangaben des Raums; <c>null</c> = keine (nackter Körper).</param>
public sealed record GebaeudeAnsichtKoerper(
    GebaeudeAnsichtRaum Raum, double UnterkanteM, double HoeheM, bool HoeheVorgabe, GebaeudeAnsichtKoerperraum? Angaben)
{
    /// <summary>Schematisch: der Umriss erfunden oder die Höhe die Vorgabe.</summary>
    public bool Schematisch => Raum.Schematisch || HoeheVorgabe;
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

    /// <summary>GIMP_ANS_KOERPER_BILD — Beschriftung der Körperansicht für die Sprachausgabe.</summary>
    public string KoerperBild { get; set; } = Resource.GIMP_ANS_KOERPER_BILD;

    /// <summary>GIMP_ANS_KOERPER_SCHEMATISCH — die Zeile „schematisch" über den Körpern.</summary>
    public string KoerperSchematisch { get; set; } = Resource.GIMP_ANS_KOERPER_SCHEMATISCH;

    /// <summary>GIMP_ANS_KOERPER_OHNE_WEBGL — die Plattform kann keine Körper zeichnen (kein WebGL, Modul fehlt).</summary>
    public string KoerperOhneWebgl { get; set; } = Resource.GIMP_ANS_KOERPER_OHNE_WEBGL;

    /// <summary>GIMP_ANS_KOERPER_HOEHE_VORGABE — Herkunft der Höhe, wenn die Datei keine nennt.</summary>
    public string HoeheVorgabe { get; set; } = Resource.GIMP_ANS_KOERPER_HOEHE_VORGABE;

    /// <summary>GIMP_ANS_KOERPER_BEDIENUNG — wie man die Körper dreht, zoomt und eine Zone wählt.</summary>
    public string KoerperBedienung { get; set; } = Resource.GIMP_ANS_KOERPER_BEDIENUNG;

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

    /// <summary>GANS_MODUS — Beschriftung des Umschalters „Dateikörper | Exportmodell" für die Sprachausgabe.</summary>
    public string Modus { get; set; } = Resource.GANS_MODUS;

    /// <summary>GANS_DATEIKOERPER</summary>
    public string Dateikoerper { get; set; } = Resource.GANS_DATEIKOERPER;

    /// <summary>GANS_EXPORTMODELL</summary>
    public string Exportmodell { get; set; } = Resource.GANS_EXPORTMODELL;

    /// <summary>GANS_KENNZEICHEN — {0} Räume aus Datei, {1} aus Umriss, {2} schematisch.</summary>
    public string Kennzeichen { get; set; } = Resource.GANS_KENNZEICHEN;

    /// <summary>GANS_VEREINFACHT — {0} = die Vermerke, mit Komma getrennt.</summary>
    public string Vereinfacht { get; set; } = Resource.GANS_VEREINFACHT;

    /// <summary>GANS_ZU_GROSS — {0} = Dreiecke, {1} = Grenze.</summary>
    public string ZuGross { get; set; } = Resource.GANS_ZU_GROSS;

    /// <summary>GANS_HERKUNFT_DATEI</summary>
    public string HerkunftDatei { get; set; } = Resource.GANS_HERKUNFT_DATEI;

    /// <summary>GANS_HERKUNFT_UMRISS</summary>
    public string HerkunftUmriss { get; set; } = Resource.GANS_HERKUNFT_UMRISS;

    /// <summary>GANS_HERKUNFT_SCHEMATISCH</summary>
    public string HerkunftSchematisch { get; set; } = Resource.GANS_HERKUNFT_SCHEMATISCH;

    /// <summary>GANS_RAEUME — Beschriftung der Raumliste mit der Herkunft je Körper.</summary>
    public string Raeume { get; set; } = Resource.GANS_RAEUME;

    /// <summary>GANS_RAUM_HERKUNFT — {0} = Raum, {1} = Herkunft (Kurztext des Raums).</summary>
    public string RaumHerkunft { get; set; } = Resource.GANS_RAUM_HERKUNFT;

    /// <summary>
    /// GANS_VERMERK_&lt;NAME&gt; — der Text je Vermerk eines Dateikörpers, nach dem Schlüssel (Name von
    /// <c>Koerpervermerk</c>); ein unbekannter Schlüssel zeigt sich selbst.
    /// </summary>
    public IReadOnlyDictionary<string, string> Vermerke { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Bogen"] = Resource.GANS_VERMERK_BOGEN,
        ["Loch"] = Resource.GANS_VERMERK_LOCH,
        ["Uneben"] = Resource.GANS_VERMERK_UNEBEN,
        ["OhneBeschnitt"] = Resource.GANS_VERMERK_OHNEBESCHNITT,
        ["Offen"] = Resource.GANS_VERMERK_OFFEN,
        ["Mehrschale"] = Resource.GANS_VERMERK_MEHRSCHALE,
    };

    /// <summary>GANS_FARBMODUS — Beschriftung des Umschalters „Zonen | Randbedingung" für die Sprachausgabe.</summary>
    public string Farbmodus { get; set; } = Resource.GANS_FARBMODUS;

    /// <summary>GANS_FARBMODUS_ZONEN</summary>
    public string FarbmodusZonen { get; set; } = Resource.GANS_FARBMODUS_ZONEN;

    /// <summary>GANS_FARBMODUS_RAND</summary>
    public string FarbmodusRand { get; set; } = Resource.GANS_FARBMODUS_RAND;

    /// <summary>GANS_RAND_OHNE_KLASSIFIKATION — „Randbedingung" ist nicht wählbar: keine Klassifikation.</summary>
    public string RandOhneKlassifikation { get; set; } = Resource.GANS_RAND_OHNE_KLASSIFIKATION;

    /// <summary>GANS_RAND_ZU_GROSS — „Randbedingung" ist nicht wählbar: über der Dreiecksgrenze.</summary>
    public string RandZuGross { get; set; } = Resource.GANS_RAND_ZU_GROSS;

    /// <summary>GANS_RAND_LEGENDE — Beschriftung der Gruppenlegende.</summary>
    public string RandLegende { get; set; } = Resource.GANS_RAND_LEGENDE;

    /// <summary>GANS_RAND_FLAECHE — {0} = Flächensumme.</summary>
    public string RandFlaeche { get; set; } = Resource.GANS_RAND_FLAECHE;

    /// <summary>GANS_RAND_SCHALTER — Beschriftung des Schalters je Gruppe, {0} = Gruppe.</summary>
    public string RandSchalter { get; set; } = Resource.GANS_RAND_SCHALTER;

    /// <summary>GANS_RAND_NUR_LEGENDE — Decken (R5, R6) stehen im Grundriss nur in Legende und Bilanz.</summary>
    public string RandNurLegende { get; set; } = Resource.GANS_RAND_NUR_LEGENDE;

    /// <summary>GANS_RAND_R0 … GANS_RAND_R7 — die Namen der Gruppen nach dem Gruppenindex.</summary>
    public IReadOnlyList<string> Randgruppen { get; set; } = new[]
    {
        Resource.GANS_RAND_R0, Resource.GANS_RAND_R1, Resource.GANS_RAND_R2, Resource.GANS_RAND_R3,
        Resource.GANS_RAND_R4, Resource.GANS_RAND_R5, Resource.GANS_RAND_R6, Resource.GANS_RAND_R7,
    };

    /// <summary>Der Name einer Gruppe, mit ihrem Kürzel davor („R1 Wand gegen außen").</summary>
    public string Gruppenname(Randgruppe g)
        => g + " " + ((int)g < Randgruppen.Count ? Randgruppen[(int)g] : "");

    /// <summary>Die Herkunft eines Körpers als Text.</summary>
    public string Herkunft(Koerperherkunft herkunft) => herkunft switch
    {
        Koerperherkunft.Datei => HerkunftDatei,
        Koerperherkunft.Schematisch => HerkunftSchematisch,
        _ => HerkunftUmriss,
    };

    /// <summary>Der Text eines Vermerks nach seinem Schlüssel; unbekannt = der Schlüssel.</summary>
    public string Vermerk(string schluessel) => Vermerke.TryGetValue(schluessel, out string? text) ? text : schluessel;
}
