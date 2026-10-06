using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    // ======================================================================================
    //  Das Zonengeometrie-Modell (Entscheid E11; Stufe G6c, Welle D) — Aufzählungen und Verweise
    // ======================================================================================

    /// <summary>
    /// Woher der Umriss eines Raums, einer Zone oder eines Geschosses stammt (Mehrzonenkonzept 6.7;
    /// Datenaustauschkonzept 14.4 Nr. 4: keine Geometrie ohne Herkunft). <b>Ein Anzeigetext ist nie
    /// Steuerwert</b> — die Oberfläche liest diesen Wert, nicht das Wort „schematisch".
    /// </summary>
    internal enum Geometrieherkunft
    {
        /// <summary>Aus den Raumgrenzen der Datei: Gestalt und Lage sind die der Datei.</summary>
        Raumgrenzen = 0,

        /// <summary>Aus Fläche und Seitenverhältnis gebildet und je Geschoss gereiht — die Anordnung ist erfunden.</summary>
        Schematisch = 1,

        /// <summary>
        /// Gestalt und Lage aus dem Raumkörper der Datei, als Prisma (HC-5, <see cref="Koerpergrundriss"/>): der Grundriss
        /// ist abgeleitet, Dachschrägen und Höhenversprünge gehen benannt verloren.
        /// </summary>
        Dateikoerper = 2,
    }

    /// <summary>Wie ein Umriss hergeleitet ist — der Beleg zu <see cref="Geometrieherkunft"/>.</summary>
    internal enum Umrissherleitung
    {
        /// <summary>Aus den Bodengrenzen des Raums, in die Grundrissebene projiziert.</summary>
        Boden = 0,

        /// <summary>Ohne auswertbare Bodengrenze aus den Deckengrenzen des Raums, projiziert.</summary>
        Decke = 1,

        /// <summary>Rechteck nach den Wandflächen: h = V/A, l = A_NS/(2h), b = A_OW/(2h); l·b trifft die Fläche auf 10 %.</summary>
        Wandflaechen = 2,

        /// <summary>Rechteck mit der Fläche und dem Seitenverhältnis l : b der Wandflächen — l·b wich um mehr als 10 % ab.</summary>
        Seitenverhaeltnis = 3,

        /// <summary>Quadrat der Fläche — ohne Wandflächen in beiden Richtungen oder ohne Höhe.</summary>
        Quadrat = 4,

        /// <summary>Kein Umriss: weder Raumgrenzen noch eine Fläche.</summary>
        Keine = 5,

        /// <summary>Aus den Bodendreiecken des Dateikörpers, in die Grundrissebene projiziert (<see cref="Koerpergrundriss"/>, Stufe 1).</summary>
        KoerperBoden = 6,

        /// <summary>Ohne Bodendreiecke aus den Deckendreiecken des Dateikörpers (<see cref="Koerpergrundriss"/>, Stufe 2).</summary>
        KoerperDecke = 7,

        /// <summary>Die konvexe Hülle aller Punkte des Dateikörpers — Einbuchtungen gehen verloren (Stufe 3).</summary>
        KoerperHuelle = 8,
    }

    /// <summary>Wo eine Grenze am Raum steht.</summary>
    internal enum Grenzstellung
    {
        /// <summary>Senkrecht (Wand, Vorhangfassade) — sie gehört an eine Kante des Umrisses.</summary>
        Wand = 0,

        /// <summary>Unter dem Raum.</summary>
        Boden = 1,

        /// <summary>Über dem Raum (Decke, Dach).</summary>
        Decke = 2,

        /// <summary>Nicht zu entscheiden.</summary>
        Unbestimmt = 3,
    }

    /// <summary>
    /// <b>Der Verweis auf eine Grenze</b> — an einer Kante des Umrisses, an Boden oder Decke eines Raums.
    /// Alles sprachneutrale Kennungen und Aufzählungen der Datei.
    /// </summary>
    /// <param name="Kennung">Die Kennung der Raumgrenze; ohne Raumgrenzen (gbXML) die des Bauteils.</param>
    /// <param name="BauteilKennung">Die Kennung des Bauteils.</param>
    /// <param name="Bauteilart">Die normierte Art des Bauteils.</param>
    /// <param name="Stellung">Wand, Boden oder Decke aus Sicht des Raums.</param>
    /// <param name="Lage">Die Lage der Grenze, wie die Datei sie nennt; ohne Raumgrenze die Randbedingung des Bauteils.</param>
    /// <param name="GegenstueckKennung">Das Gegenstück der Datei (<c>CorrespondingBoundary</c>); <c>null</c> = keines.</param>
    internal sealed record Grenzverweis(string Kennung, string BauteilKennung, Bauteilart Bauteilart,
                                        Grenzstellung Stellung, Randbedingung Lage, string GegenstueckKennung);

    // ======================================================================================
    //  Der Eingang — formatfrei
    // ======================================================================================

    /// <summary>Ein Geschoss des Eingangs; die Höhenlage dient nur der Reihenfolge.</summary>
    /// <param name="Kennung">Kennung der Datei; leer = ohne Geschoss.</param>
    /// <param name="Name">Name; <c>null</c> = keiner.</param>
    /// <param name="LageM">Höhenlage [m]; <c>null</c> = unbekannt.</param>
    internal sealed record Umrissgeschoss(string Kennung, string Name, double? LageM);

    /// <summary>Eine Zone des Eingangs, in der Rangfolge der Zonierung.</summary>
    /// <param name="Schluessel">Der sprachneutrale Schlüssel der Zone (Rückweg eines Klicks, nie ihr Name).</param>
    /// <param name="Name">Der Name der Zone.</param>
    /// <param name="IstBeheizt">Beheizt oder frei schwingend.</param>
    /// <param name="FlaecheM2">Σ der Raumflächen [m²]; <c>null</c> = keine.</param>
    /// <param name="VolumenM3">Das Volumen [m³]; <c>null</c> = keines.</param>
    /// <param name="HoeheM">Die Raumhöhe V/A [m]; <c>null</c> = keine.</param>
    /// <param name="Handgeaendert">Hat eine Zuordnung von Hand die Zone gebildet oder verändert?</param>
    internal sealed record Umrisszone(string Schluessel, string Name, bool IstBeheizt, double? FlaecheM2, double? VolumenM3,
                                      double? HoeheM, bool Handgeaendert);

    /// <summary>
    /// Eine Seite eines Raums: eine Raumgrenze bzw. — ohne Raumgrenzen — ein Bauteil aus Sicht des Raums,
    /// mit ihrem Randpunktring in Weltkoordinaten, soweit die Datei einen trägt.
    /// </summary>
    internal sealed class Umrissseite
    {
        /// <summary>Der Verweis samt Stellung am Raum.</summary>
        internal Grenzverweis Verweis { get; init; }

        /// <summary>Der Randpunktring in Weltkoordinaten [m] (je Punkt x, y, z); <c>null</c> = keiner.</summary>
        internal IReadOnlyList<double[]> RandpunkteM { get; init; }

        /// <summary>Die Bruttofläche dieser Seite [m²] (Polygon, sonst der Anteil des Bauteils); <c>null</c> = keine.</summary>
        internal double? FlaecheM2 { get; init; }

        /// <summary>
        /// Der Himmelsrichtungssektor einer Wand aus Sicht des Raums: 0 Nord, 1 Ost, 2 Süd, 3 West
        /// (<see cref="GebaeudeAggregation.Sektor"/>); <c>null</c> = keine Himmelsrichtung.
        /// </summary>
        internal int? Sektor { get; init; }
    }

    /// <summary>Ein Raum des Eingangs — im Weg <see cref="Zonengeometrie.AusFlaechen"/> auch eine Zone als Ganzes.</summary>
    internal sealed class Umrissraum
    {
        /// <summary>Kennung der Datei.</summary>
        internal string Kennung { get; init; } = "";

        /// <summary>Anzeigename (Name, sonst Kennung).</summary>
        internal string Name { get; init; } = "";

        /// <summary>Kennung des Geschosses; <c>null</c> oder leer = ohne Geschoss.</summary>
        internal string GeschossKennung { get; init; }

        /// <summary>Stelle der Zone in <see cref="Umrisseingang.Zonen"/>; −1 = keine.</summary>
        internal int Zone { get; init; } = -1;

        /// <summary>Wirksam beheizt (mit den Haken der Raumliste).</summary>
        internal bool Beheizt { get; init; }

        /// <summary>Fläche [m²]; <c>null</c> = keine.</summary>
        internal double? FlaecheM2 { get; init; }

        /// <summary>Volumen [m³]; <c>null</c> = keines.</summary>
        internal double? VolumenM3 { get; init; }

        /// <summary>Lichte Höhe [m] der Datei; <c>null</c> = keine (dann gilt V/A).</summary>
        internal double? HoeheM { get; init; }

        /// <summary>Der Körper aus der Datei (15.3), nur Anzeige; <c>null</c> = keiner (gbXML, Export, nicht lesbar).</summary>
        internal Dateikoerper Koerper { get; init; }

        /// <summary>Die Seiten des Raums in der Reihenfolge der Bauteile.</summary>
        internal List<Umrissseite> Seiten { get; } = new List<Umrissseite>();
    }

    /// <summary>
    /// <b>Was die Zonengeometrie liest</b> — formatfrei: Der Importweg füllt es aus Abbild und Zonierung
    /// (<see cref="GebaeudeGrundriss"/>), ein Export aus den Zonen des Projekts.
    /// </summary>
    internal sealed class Umrisseingang
    {
        /// <summary>Die Geschosse, wie die Quelle sie führt (Reihenfolge beliebig — sortiert wird beim Bilden).</summary>
        internal List<Umrissgeschoss> Geschosse { get; } = new List<Umrissgeschoss>();

        /// <summary>Die Zonen in Rangfolge.</summary>
        internal List<Umrisszone> Zonen { get; } = new List<Umrisszone>();

        /// <summary>Die Räume in der Reihenfolge der Datei.</summary>
        internal List<Umrissraum> Raeume { get; } = new List<Umrissraum>();

        /// <summary>Die Drehung des Modells gegen Nord [°], wie gelesen; <c>null</c> = keine Angabe.</summary>
        internal double? NordwinkelGrad { get; init; }

        /// <summary>
        /// Gilt die Drehung für die Azimute (IFC: ja, die Bauteilazimute sind gedreht; gbXML: nein, die
        /// Datei schreibt sie selbst)? Die Außennormale einer Umrisskante folgt derselben Regel.
        /// </summary>
        internal bool NordwinkelAngewandt { get; init; }
    }

    // ======================================================================================
    //  Das Ergebnis
    // ======================================================================================

    /// <summary>
    /// Eine Kante eines Umrisspolygons — vom Punkt <see cref="Index"/> zum nächsten — mit den Grenzen,
    /// die auf ihr stehen.
    /// </summary>
    internal sealed class Umrisskante
    {
        internal Umrisskante(int index, double laengeM, double? azimutGrad, IReadOnlyList<Grenzverweis> grenzen)
        {
            Index = index;
            LaengeM = laengeM;
            AzimutGrad = azimutGrad;
            Grenzen = grenzen ?? Array.Empty<Grenzverweis>();
        }

        /// <summary>Stelle des Startpunkts in <see cref="Umrisspolygon.Punkte"/>.</summary>
        internal int Index { get; }

        /// <summary>Länge [m].</summary>
        internal double LaengeM { get; }

        /// <summary>
        /// Azimut der Außennormalen [°], 0 = Nord, im Uhrzeigersinn — nach derselben Nordregel wie die
        /// Bauteile (<see cref="Umrisseingang.NordwinkelAngewandt"/>); <c>null</c> = Kante ohne Länge.
        /// </summary>
        internal double? AzimutGrad { get; }

        /// <summary>Die Wandgrenzen auf dieser Kante in der Reihenfolge der Seiten; leer = keine bekannt.</summary>
        internal IReadOnlyList<Grenzverweis> Grenzen { get; }

        /// <summary>
        /// Die Strecken der Wände auf dieser Kante (Stufe G7b), vom Startpunkt der Kante gemessen und lückenlos —
        /// nur am schematischen Rechteck; leer = keine (Umriss aus Raumgrenzen). Innere Masse desselben Raums
        /// steht auf keiner Strecke.
        /// </summary>
        internal IReadOnlyList<Kantenabschnitt> Abschnitte { get; init; } = Array.Empty<Kantenabschnitt>();
    }

    /// <summary>Ein Polygon eines Umrisses in der Grundrissebene.</summary>
    internal sealed class Umrisspolygon
    {
        internal Umrisspolygon(IReadOnlyList<double[]> punkte, IReadOnlyList<Umrisskante> kanten, double flaecheM2,
                               double? ebeneM, Grenzverweis quelle)
        {
            Punkte = punkte;
            Kanten = kanten;
            FlaecheM2 = flaecheM2;
            EbeneM = ebeneM;
            Quelle = quelle;
        }

        /// <summary>
        /// Die Punkte [m] als (x, y) — x nach Osten, y nach Norden der Grundrissebene (die x-y-Ebene der
        /// Datei; schematisch: Nord oben). <b>Gegen den Uhrzeigersinn</b> von oben gesehen, ohne
        /// Schlusspunkt; der erste Punkt ist der erste der Datei.
        /// </summary>
        internal IReadOnlyList<double[]> Punkte { get; }

        /// <summary>Je Punkt eine Kante zum nächsten.</summary>
        internal IReadOnlyList<Umrisskante> Kanten { get; }

        /// <summary>Der Flächeninhalt [m²].</summary>
        internal double FlaecheM2 { get; }

        /// <summary>Die Höhe der Ebene, aus der das Polygon stammt [m] (Mittel der Randpunkte); <c>null</c> = schematisch.</summary>
        internal double? EbeneM { get; }

        /// <summary>Die Boden- bzw. Deckengrenze, aus der das Polygon stammt; <c>null</c> = schematisch.</summary>
        internal Grenzverweis Quelle { get; }
    }

    /// <summary>Der Umriss eines Raums: seine Polygone, Boden, Decke und was an keiner Kante steht.</summary>
    internal sealed class Raumumriss
    {
        /// <summary>Kennung der Datei.</summary>
        internal string RaumKennung { get; init; } = "";

        /// <summary>Anzeigename.</summary>
        internal string Name { get; init; } = "";

        /// <summary>Kennung des Geschosses; leer = ohne Geschoss.</summary>
        internal string GeschossKennung { get; init; } = "";

        /// <summary>Stelle des Geschosses in <see cref="Zonengeometrie.Geschosse"/>.</summary>
        internal int Geschoss { get; init; }

        /// <summary>Stelle der Zone in <see cref="Zonengeometrie.Zonen"/>; −1 = keine (grau im Grundriss).</summary>
        internal int Zone { get; init; } = -1;

        /// <summary>Wirksam beheizt.</summary>
        internal bool Beheizt { get; init; }

        /// <summary>Aus Raumgrenzen oder schematisch.</summary>
        internal Geometrieherkunft Herkunft { get; init; }

        /// <summary>Wie der Umriss hergeleitet ist.</summary>
        internal Umrissherleitung Herleitung { get; init; }

        /// <summary>Die Polygone (ohne Polygonvereinigung: je Boden- bzw. Deckengrenze eines, schematisch eines).</summary>
        internal IReadOnlyList<Umrisspolygon> Polygone { get; init; } = Array.Empty<Umrisspolygon>();

        /// <summary>Die Fläche der Datei [m²]; <c>null</c> = keine.</summary>
        internal double? FlaecheM2 { get; init; }

        /// <summary>Σ der Polygonflächen [m²].</summary>
        internal double PolygonflaecheM2 { get; init; }

        /// <summary>Die Höhe [m]: V/A, sonst die der Datei; <c>null</c> = keine.</summary>
        internal double? HoeheM { get; init; }

        /// <summary>Schematisch: die Seite l des Rechtecks in x-Richtung [m] (Nord- und Südkante); sonst <c>null</c>.</summary>
        internal double? LaengeM { get; init; }

        /// <summary>Schematisch: die Seite b des Rechtecks in y-Richtung [m] (Ost- und Westkante); sonst <c>null</c>.</summary>
        internal double? BreiteM { get; init; }

        /// <summary>Die Grenzen unter dem Raum.</summary>
        internal IReadOnlyList<Grenzverweis> Boden { get; init; } = Array.Empty<Grenzverweis>();

        /// <summary>Die Grenzen über dem Raum.</summary>
        internal IReadOnlyList<Grenzverweis> Decke { get; init; } = Array.Empty<Grenzverweis>();

        /// <summary>Wände ohne Kante (kein Ring, kein Sektor, keine passende Kante) und Grenzen unbestimmter Stellung — benannt statt verloren.</summary>
        internal IReadOnlyList<Grenzverweis> OhneKante { get; init; } = Array.Empty<Grenzverweis>();

        /// <summary>Liegt das schematische Rechteck an einem Nachbarraum mit gemeinsamer Trennfläche an (Stufe G7b)?</summary>
        internal bool Angelegt { get; init; }

        /// <summary>
        /// Die Bodengrenzen des schematischen Rechtecks als Streifen quer zur Kante 0, entlang der Kante 0 gemessen
        /// [m], lückenlos, je Grenze im Verhältnis ihrer Fläche (Stufe G7b); leer = keine oder kein Rechteck.
        /// </summary>
        internal IReadOnlyList<Kantenabschnitt> BodenStreifen { get; init; } = Array.Empty<Kantenabschnitt>();

        /// <summary>Die Deckengrenzen des schematischen Rechtecks als Streifen wie <see cref="BodenStreifen"/>.</summary>
        internal IReadOnlyList<Kantenabschnitt> DeckenStreifen { get; init; } = Array.Empty<Kantenabschnitt>();

        /// <summary>
        /// Der Körper des Raums, wie die Datei ihn zeichnet (Datenaustauschkonzept 15.3) — „aus Datei“; <c>null</c> =
        /// kein Dateikörper. Nur Anzeige: Er ersetzt weder <see cref="Polygone"/> noch <see cref="Herkunft"/> und speist
        /// weder Fläche noch Zonierung.
        /// </summary>
        internal Dateikoerper Koerper { get; init; }

        public override string ToString() => Name + " (" + Herkunft + ", " + Polygone.Count.ToString(CultureInfo.InvariantCulture) + " Polygone)";
    }

    /// <summary>
    /// <b>Der Umriss einer Zone in einem Geschoss</b> (Softwarearchitektur 1.3): die Folge der Raumumrisse
    /// ihrer Räume — ein Mehrfachpolygon, keine Vereinigung —, dazu Höhe, Geschoss und Fläche.
    /// </summary>
    internal sealed class Zonenumriss
    {
        /// <summary>Stelle der Zone.</summary>
        internal int Zone { get; init; }

        /// <summary>Stelle des Geschosses.</summary>
        internal int Geschoss { get; init; }

        /// <summary>Die Räume der Zone in diesem Geschoss, in der Reihenfolge der Datei.</summary>
        internal IReadOnlyList<Raumumriss> Raeume { get; init; } = Array.Empty<Raumumriss>();

        /// <summary>Aus Raumgrenzen nur, wenn es jeder ihrer Räume ist.</summary>
        internal Geometrieherkunft Herkunft { get; init; }

        /// <summary>Σ der Polygonflächen [m²].</summary>
        internal double FlaecheM2 { get; init; }

        /// <summary>Die Höhe [m]: ΣV/ΣA der Räume, sonst die der Zone; <c>null</c> = keine.</summary>
        internal double? HoeheM { get; init; }

        /// <summary>Alle Polygone der Räume in ihrer Reihenfolge.</summary>
        internal IEnumerable<Umrisspolygon> Polygone => Raeume.SelectMany(r => r.Polygone);
    }

    /// <summary>Ein Geschoss der Zonengeometrie.</summary>
    internal sealed class Geschossangabe
    {
        /// <summary>Stelle in <see cref="Zonengeometrie.Geschosse"/> — nach Höhenlage, sonst Name.</summary>
        internal int Stelle { get; init; }

        /// <summary>Kennung der Datei; leer = ohne Geschoss.</summary>
        internal string Kennung { get; init; } = "";

        /// <summary>Name; <c>null</c> = keiner.</summary>
        internal string Name { get; init; }

        /// <summary>Höhenlage [m]; <c>null</c> = unbekannt.</summary>
        internal double? LageM { get; init; }

        /// <summary>Trägt das Geschoss schematische Umrisse?</summary>
        internal bool Schematisch { get; init; }

        /// <summary>Die Ausdehnung aller Polygone [m]; ohne Polygon alles 0.</summary>
        internal double MinX { get; init; }

        /// <summary>Siehe <see cref="MinX"/>.</summary>
        internal double MinY { get; init; }

        /// <summary>Siehe <see cref="MinX"/>.</summary>
        internal double MaxX { get; init; }

        /// <summary>Siehe <see cref="MinX"/>.</summary>
        internal double MaxY { get; init; }
    }

    /// <summary>Eine Zone der Zonengeometrie mit ihren Umrissen je Geschoss.</summary>
    internal sealed class Zonenangabe
    {
        /// <summary>Stelle in <see cref="Zonengeometrie.Zonen"/> — die der Zonierung.</summary>
        internal int Stelle { get; init; }

        /// <summary>Der sprachneutrale Schlüssel (Rückweg eines Klicks).</summary>
        internal string Schluessel { get; init; } = "";

        /// <summary>Der Name.</summary>
        internal string Name { get; init; } = "";

        /// <summary>Beheizt oder frei schwingend.</summary>
        internal bool IstBeheizt { get; init; }

        /// <summary>Hat eine Zuordnung von Hand sie gebildet oder verändert?</summary>
        internal bool Handgeaendert { get; init; }

        /// <summary>Aus Raumgrenzen nur, wenn es jeder ihrer Räume ist.</summary>
        internal Geometrieherkunft Herkunft { get; init; }

        /// <summary>Σ der Raumflächen [m²]; <c>null</c> = keine.</summary>
        internal double? FlaecheM2 { get; init; }

        /// <summary>Volumen [m³]; <c>null</c> = keines.</summary>
        internal double? VolumenM3 { get; init; }

        /// <summary>Höhe [m]; <c>null</c> = keine.</summary>
        internal double? HoeheM { get; init; }

        /// <summary>Die Umrisse je Geschoss in der Reihenfolge der Geschosse.</summary>
        internal IReadOnlyList<Zonenumriss> Umrisse { get; init; } = Array.Empty<Zonenumriss>();
    }

    // ======================================================================================
    //  Die Zonengeometrie
    // ======================================================================================

    /// <summary>
    /// <b>Das Zonengeometrie-Modell</b> (Entscheid E11; Mehrzonenkonzept 6.7; Softwarearchitektur 1.3;
    /// Datenaustauschkonzept 14) — eine Quelle für die Grundrissansicht, den gbXML-Export G7b und den
    /// IFC-Export G7e. Unveränderlich nach dem Bilden; kennt weder Datei noch Format noch Oberfläche.
    ///
    /// <list type="bullet">
    /// <item><b>Umriss aus Raumgrenzen</b> (<see cref="AusRaumgrenzen"/>): je Raum die Bodengrenzen mit
    /// Randpunktring, in die Grundrissebene projiziert (x, y der Weltkoordinaten) — so passen die Räume eines
    /// Geschosses zueinander. Ohne auswertbare Bodengrenze tragen die Deckengrenzen den Umriss. Kein
    /// Geometriekern, keine Polygonvereinigung: ein Raum hat so viele Polygone, wie er Grenzen trägt, eine
    /// Zone die Folge der Polygone ihrer Räume.</item>
    /// <item><b>Kanten:</b> Jede Kante behält die Wandgrenzen des Raums, deren Ring in der Projektion auf ihr
    /// liegt (Abstand höchstens <see cref="KANTE_ABSTAND_M"/>, Überdeckung mehr als
    /// <see cref="KANTE_UEBERLAPPUNG_M"/>); Boden und Decke stehen am Raum. Was nirgends hinpasst, steht
    /// benannt in <see cref="Raumumriss.OhneKante"/>.</item>
    /// <item><b>Rechteckersatz</b> (Mehrzonenkonzept 6.7, Datenaustauschkonzept 5.5 und 14.1) für jeden Raum
    /// ohne Umriss aus Raumgrenzen: h = V/A (sonst die Höhe der Datei), l = A_NS/(2h), b = A_OW/(2h) aus den
    /// Bruttoflächen der Wände nach Himmelsrichtung; weicht l·b um mehr als <see cref="RECHTECK_TOLERANZ"/>
    /// von der Fläche ab, gilt die Fläche mit dem Seitenverhältnis l : b; fehlen Wände einer Richtung oder die
    /// Höhe, das Quadrat. Die Rechtecke eines Geschosses stehen rechts neben den Umrissen aus Raumgrenzen,
    /// je Zone in der Reihenfolge der Datei, in Zeilen gereiht; Nord oben, die Kanten Süd, Ost, Nord, West
    /// tragen die Wände ihres Sektors. <b>Diese Anordnung ist erfunden</b> — Herkunft
    /// <see cref="Geometrieherkunft.Schematisch"/>, Pflicht an Bild und Datei.</item>
    /// <item><b>Aneinanderlegen</b> (Stufe G7b, Datenaustauschkonzept 5.5 Punkt 3): Räume mit gemeinsamer
    /// Trennwand (<see cref="Nachbarpaare"/>) stehen als Block, die Strecken der Trennwand deckungsgleich —
    /// flächentreu gestreckt, nie gedreht, keine Polygonvereinigung; ein Paar, dessen Trennwand bei beiden
    /// Räumen nicht auf gegenüberliegenden Himmelsseiten liegt, bleibt getrennt (Hinweis
    /// <see cref="NICHT_ANGELEGT"/>); eine widersprüchliche Anordnung ist benannt abgelehnt
    /// (<see cref="AnordnungAbgelehnt"/>). Die Gegenstücke der Paare sind nachgezogen.</item>
    /// <item><b>Determinismus:</b> dieselbe Eingabe ergibt dieselben Polygone in derselben Reihenfolge — nur
    /// Listen in fester Folge, Sortierung über Höhenlage und Ordinalvergleich.</item>
    /// </list>
    /// </summary>
    internal sealed partial class Zonengeometrie
    {
        // ==================================================================
        //  Meldungen (Schlüssel formatfrei, Präfix ZGEO_)
        // ==================================================================

        /// <summary>I — {0} Zahl, {1} Beispiele: Räume ohne Umriss aus Raumgrenzen sind schematisch gezeichnet.</summary>
        internal const string SCHEMATISCH = "ZGEO_SCHEMATISCH";
        /// <summary>I — {0} Zahl, {1} Beispiele: Rechtecke mit der Fläche und dem Seitenverhältnis der Wände (10-%-Probe).</summary>
        internal const string SEITENVERHAELTNIS = "ZGEO_SEITENVERHAELTNIS";
        /// <summary>I — {0} Zahl, {1} Beispiele: Quadrate ohne Wandflächen beider Richtungen oder ohne Höhe.</summary>
        internal const string QUADRAT = "ZGEO_QUADRAT";
        /// <summary>W — {0} Zahl, {1} Beispiele: Räume weder mit Raumgrenzen noch mit Fläche — nicht im Grundriss.</summary>
        internal const string OHNE_FLAECHE = "ZGEO_OHNE_FLAECHE";

        // ==================================================================
        //  Festwerte
        // ==================================================================

        /// <summary>Größter Abstand [m] eines projizierten Wandpunkts von der Kante, auf der die Wand steht.</summary>
        internal const double KANTE_ABSTAND_M = 0.05;

        /// <summary>Kleinste Überdeckung [m] einer Wand mit einer Kante.</summary>
        internal const double KANTE_UEBERLAPPUNG_M = 0.01;

        /// <summary>Relative Abweichung von l·b gegen die Fläche, bis zu der das Rechteck der Wandflächen gilt (5.5).</summary>
        internal const double RECHTECK_TOLERANZ = 0.10;

        /// <summary>Abstand [m] zwischen zwei schematischen Rechtecken und zwischen ihren Zeilen.</summary>
        internal const double ABSTAND_M = 0.5;

        /// <summary>Abstand [m] der schematischen Rechtecke von den Umrissen aus Raumgrenzen desselben Geschosses.</summary>
        internal const double ABSTAND_BLOCK_M = 2.0;

        /// <summary>
        /// Das Seitenverhältnis Breite : Höhe, auf das die Zeilen der schematischen Rechtecke eines Geschosses
        /// zielen (3 : 2 — breiter als hoch wie die Zeichenfläche); die Zeilenbreite ist mindestens das
        /// breiteste Rechteck.
        /// </summary>
        internal const double BLOCK_SEITENVERHAELTNIS = 1.5;

        /// <summary>Kleinste Fläche [m²] eines projizierten Polygons; darunter ist es entartet (etwa eine Wand).</summary>
        internal const double FLAECHE_MIN_M2 = 1e-6;

        /// <summary>
        /// Größter Abstand eines Randpunkts von der Ebene [m], bis zu dem ein Ring eben ist — dieselbe Grenze
        /// für die Raumgrenzen des IFC-Wegs und die <c>PolyLoop</c> des gbXML-Wegs (Mehrzonenkonzept 6.2).
        /// </summary>
        internal const double EBEN_TOLERANZ_M = 0.001;

        private const double PUNKT_GLEICH_M = 1e-9;

        // ==================================================================
        //  Inhalt
        // ==================================================================

        private Zonengeometrie() { }

        /// <summary>Die Geschosse, die Räume tragen — nach Höhenlage, sonst nach Name; ohne Geschoss zuletzt.</summary>
        internal IReadOnlyList<Geschossangabe> Geschosse { get; private set; } = Array.Empty<Geschossangabe>();

        /// <summary>Die Zonen in der Rangfolge des Eingangs.</summary>
        internal IReadOnlyList<Zonenangabe> Zonen { get; private set; } = Array.Empty<Zonenangabe>();

        /// <summary>Alle Räume in der Reihenfolge des Eingangs — auch die ohne Zone.</summary>
        internal IReadOnlyList<Raumumriss> Raeume { get; private set; } = Array.Empty<Raumumriss>();

        /// <summary>Die Umrisse je Zone und Geschoss: nach Zone, dann nach Geschoss.</summary>
        internal IReadOnlyList<Zonenumriss> Umrisse { get; private set; } = Array.Empty<Zonenumriss>();

        /// <summary>Die Meldungen in Reihenfolge.</summary>
        internal IReadOnlyList<PruefMeldung> Meldungen { get; private set; } = Array.Empty<PruefMeldung>();

        /// <summary>Trägt die Geometrie einen schematischen Umriss? Dann ist „schematisch" Pflicht an Bild und Datei.</summary>
        internal bool Schematisch => Raeume.Any(r => r.Herkunft == Geometrieherkunft.Schematisch && r.Polygone.Count > 0);

        /// <summary>Trägt mindestens ein Raum einen Körper aus der Datei (15.3)?</summary>
        internal bool HatDateikoerper => Raeume.Any(r => r.Koerper != null);

        /// <summary>Die Dreiecke aller Dateikörper (15.4: die Ansicht hält sie gegen <see cref="Dateikoerper.DREIECKSGRENZE"/>).</summary>
        internal int DateikoerperDreiecke => Raeume.Sum(r => r.Koerper?.DreieckZahl ?? 0);

        /// <summary>
        /// Die Nachbarpaare (Stufe G7b): je Trennfläche, die genau zwei Räume teilen, beide Seiten — mit dem
        /// nachgezogenen Gegenstück und dem Stand des Aneinanderlegens; nach Bauteilkennung geordnet.
        /// </summary>
        internal IReadOnlyList<Nachbarpaar> Nachbarpaare { get; private set; } = Array.Empty<Nachbarpaar>();

        /// <summary>
        /// Ist das Aneinanderlegen für mindestens eine Gruppe benannt abgelehnt (widersprüchliche Anordnung)?
        /// Dann steht die Gruppe gereiht wie ohne Nachbarn, die Meldung <see cref="ANORDNUNG_ABGELEHNT"/> nennt das
        /// Paar, und der gbXML-Export schreibt keine Raumgeometrie (Stufe 2 benannt abgelehnt).
        /// </summary>
        internal bool AnordnungAbgelehnt => Nachbarpaare.Any(p => p.Abgelehnt);

        /// <summary>Die Drehung des Modells gegen Nord [°], wie gelesen; <c>null</c> = keine Angabe.</summary>
        internal double? NordwinkelGrad { get; private set; }

        /// <summary>Gilt die Drehung für die Azimute der Kanten?</summary>
        internal bool NordwinkelAngewandt { get; private set; }

        /// <summary>Der Umriss eines Raums; <c>null</c> = kein solcher Raum.</summary>
        internal Raumumriss Raum(string kennung)
            => Raeume.FirstOrDefault(r => string.Equals(r.RaumKennung, kennung, StringComparison.Ordinal));

        /// <summary>Die Räume eines Geschosses in der Reihenfolge des Eingangs.</summary>
        internal IEnumerable<Raumumriss> RaeumeIm(int geschoss) => Raeume.Where(r => r.Geschoss == geschoss);

        // ==================================================================
        //  Bilden
        // ==================================================================

        /// <summary>
        /// <b>Aus den Raumgrenzen</b>: Umrisse aus den Bodengrenzen (sonst Deckengrenzen) mit Randpunktring,
        /// für jeden Raum ohne solche der Rechteckersatz. Schreibt nichts, wirft nicht.
        /// </summary>
        internal static Zonengeometrie AusRaumgrenzen(Umrisseingang eingang) => Bilden(eingang, raumgrenzen: true);

        /// <summary>
        /// <b>Aus den Flächen</b> (ohne Raumgrenzen, etwa aus den Zonen des Projekts): jeder Raum — bzw. jede
        /// Zone als Raum — als Rechteck aus Fläche und Seitenverhältnis der Wandflächen, je Geschoss gereiht.
        /// </summary>
        internal static Zonengeometrie AusFlaechen(Umrisseingang eingang) => Bilden(eingang, raumgrenzen: false);

        private static Zonengeometrie Bilden(Umrisseingang eingang, bool raumgrenzen)
        {
            var z = new Zonengeometrie();
            if (eingang == null) return z;
            z.NordwinkelGrad = eingang.NordwinkelGrad;
            z.NordwinkelAngewandt = eingang.NordwinkelAngewandt;
            double drehung = eingang.NordwinkelAngewandt ? eingang.NordwinkelGrad ?? 0.0 : 0.0;

            // Geschosse: nur die mit Räumen; nach Höhenlage, sonst nach Name, dann Kennung; ohne Geschoss zuletzt.
            List<string> mitRaeumen = eingang.Raeume.Select(r => r.GeschossKennung ?? "").Distinct(StringComparer.Ordinal).ToList();
            var angaben = new List<Umrissgeschoss>();
            foreach (string k in mitRaeumen)
            {
                Umrissgeschoss g = eingang.Geschosse.FirstOrDefault(x => string.Equals(x.Kennung ?? "", k, StringComparison.Ordinal))
                                   ?? new Umrissgeschoss(k, null, null);
                angaben.Add(g);
            }
            List<Umrissgeschoss> geordnet = angaben
                .OrderBy(g => (g.Kennung ?? "").Length == 0 ? 2 : g.LageM.HasValue ? 0 : 1)
                .ThenBy(g => g.LageM ?? 0.0)
                .ThenBy(g => g.Name ?? "", StringComparer.Ordinal)
                .ThenBy(g => g.Kennung ?? "", StringComparer.Ordinal)
                .ToList();
            var geschossStelle = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < geordnet.Count; i++) geschossStelle[geordnet[i].Kennung ?? ""] = i;

            // Je Raum: Umriss aus Raumgrenzen oder Maße des Rechteckersatzes.
            var entwurf = new List<Umrissentwurf>();
            foreach (Umrissraum r in eingang.Raeume)
            {
                var e = new Umrissentwurf
                {
                    Raum = r,
                    Geschoss = geschossStelle[r.GeschossKennung ?? ""],
                    Zone = r.Zone >= 0 && r.Zone < eingang.Zonen.Count ? r.Zone : -1,
                };
                e.Boden = r.Seiten.Where(s => s.Verweis?.Stellung == Grenzstellung.Boden).Select(s => s.Verweis).ToList();
                e.Decke = r.Seiten.Where(s => s.Verweis?.Stellung == Grenzstellung.Decke).Select(s => s.Verweis).ToList();
                if (raumgrenzen)
                {
                    e.Polygone = Projiziert(r, Grenzstellung.Boden, drehung, e.Platziert);
                    e.Herleitung = Umrissherleitung.Boden;
                    if (e.Polygone.Count == 0)
                    {
                        e.Polygone = Projiziert(r, Grenzstellung.Decke, drehung, e.Platziert);
                        e.Herleitung = Umrissherleitung.Decke;
                    }
                }
                if (e.Polygone.Count > 0)
                {
                    e.Herkunft = Geometrieherkunft.Raumgrenzen;
                    e.OhneKante = r.Seiten.Where(s => s.Verweis != null && (s.Verweis.Stellung == Grenzstellung.Unbestimmt
                                                     || (s.Verweis.Stellung == Grenzstellung.Wand && !e.Platziert.Contains(s))))
                                          .Select(s => s.Verweis).ToList();
                }
                else
                {
                    // Bis das Rechteck steht, liegt jede Wand und jede unbestimmte Grenze an keiner Kante;
                    // ein Raum ohne Fläche bleibt so (er steht nicht im Grundriss).
                    e.Herkunft = Geometrieherkunft.Schematisch;
                    (e.L, e.B, e.Herleitung) = Rechteck(r);
                    e.OhneKante = r.Seiten.Where(s => s.Verweis != null && s.Verweis.Stellung != Grenzstellung.Boden
                                                      && s.Verweis.Stellung != Grenzstellung.Decke)
                                          .Select(s => s.Verweis).ToList();
                }
                entwurf.Add(e);
            }

            // Die Nachbarpaare und die Kanten der Wände am Rechteck (Stufe G7b).
            List<Paarentwurf> paare = Paare(entwurf);
            Kantenzuordnung(entwurf, paare);

            // Die Rechtecke je Geschoss reihen: rechts neben den Umrissen aus Raumgrenzen, je Zone in der
            // Reihenfolge der Datei (Räume ohne Zone zuletzt), in Zeilen — Räume mit gemeinsamer Trennfläche
            // aneinandergelegt als ein Block.
            for (int g = 0; g < geordnet.Count; g++)
            {
                List<Umrissentwurf> echt = entwurf.Where(e => e.Geschoss == g && e.Herkunft == Geometrieherkunft.Raumgrenzen).ToList();
                List<Umrissentwurf> ersatz = entwurf.Where(e => e.Geschoss == g && e.Herkunft == Geometrieherkunft.Schematisch && e.L > 0.0)
                                              .OrderBy(e => e.Zone < 0 ? int.MaxValue : e.Zone).ToList();
                if (ersatz.Count == 0) continue;
                double x0 = 0.0, oben = 0.0;
                if (echt.Count > 0)
                {
                    List<double[]> punkte = echt.SelectMany(e => e.Polygone).SelectMany(p => p.Punkte).ToList();
                    x0 = punkte.Max(p => p[0]) + ABSTAND_BLOCK_M;
                    oben = punkte.Max(p => p[1]);
                }
                Reihen(Anlegen(ersatz, paare), x0, oben);
            }

            // Die Gegenstücke der Paare nachziehen (M13): jede Seite verweist auf die Gegenseite.
            Dictionary<Grenzverweis, Grenzverweis> nachgezogen = Nachziehen(paare);
            foreach (Umrissentwurf e in entwurf) Ersetzen(e, nachgezogen);

            // Die Umrisse der Räume.
            var raeume = new List<Raumumriss>(entwurf.Count);
            foreach (Umrissentwurf e in entwurf)
            {
                Umrissraum r = e.Raum;
                raeume.Add(new Raumumriss
                {
                    RaumKennung = r.Kennung ?? "",
                    Name = string.IsNullOrWhiteSpace(r.Name) ? r.Kennung ?? "" : r.Name,
                    GeschossKennung = r.GeschossKennung ?? "",
                    Geschoss = e.Geschoss,
                    Zone = e.Zone,
                    Beheizt = r.Beheizt,
                    Herkunft = e.Herkunft,
                    Herleitung = e.Herleitung,
                    Polygone = e.Polygone,
                    FlaecheM2 = r.FlaecheM2,
                    PolygonflaecheM2 = e.Polygone.Sum(p => p.FlaecheM2),
                    HoeheM = Hoehe(r),
                    LaengeM = e.Herkunft == Geometrieherkunft.Schematisch && e.L > 0.0 ? e.L : (double?)null,
                    BreiteM = e.Herkunft == Geometrieherkunft.Schematisch && e.B > 0.0 ? e.B : (double?)null,
                    Boden = e.Boden,
                    Decke = e.Decke,
                    OhneKante = e.OhneKante,
                    Angelegt = e.Angelegt,
                    BodenStreifen = e.BodenStreifen,
                    DeckenStreifen = e.DeckenStreifen,
                    Koerper = r.Koerper,
                });
            }
            z.Raeume = raeume;

            // Die Geschosse mit ihrer Ausdehnung.
            var geschosse = new List<Geschossangabe>(geordnet.Count);
            for (int g = 0; g < geordnet.Count; g++)
            {
                List<double[]> punkte = raeume.Where(r => r.Geschoss == g).SelectMany(r => r.Polygone).SelectMany(p => p.Punkte).ToList();
                geschosse.Add(new Geschossangabe
                {
                    Stelle = g,
                    Kennung = geordnet[g].Kennung ?? "",
                    Name = geordnet[g].Name,
                    LageM = geordnet[g].LageM,
                    Schematisch = raeume.Any(r => r.Geschoss == g && r.Herkunft == Geometrieherkunft.Schematisch && r.Polygone.Count > 0),
                    MinX = punkte.Count > 0 ? punkte.Min(p => p[0]) : 0.0,
                    MinY = punkte.Count > 0 ? punkte.Min(p => p[1]) : 0.0,
                    MaxX = punkte.Count > 0 ? punkte.Max(p => p[0]) : 0.0,
                    MaxY = punkte.Count > 0 ? punkte.Max(p => p[1]) : 0.0,
                });
            }
            z.Geschosse = geschosse;

            // Die Zonen und ihre Umrisse je Geschoss.
            var zonen = new List<Zonenangabe>(eingang.Zonen.Count);
            var umrisse = new List<Zonenumriss>();
            for (int i = 0; i < eingang.Zonen.Count; i++)
            {
                Umrisszone uz = eingang.Zonen[i];
                List<Raumumriss> eigene = raeume.Where(r => r.Zone == i).ToList();
                var jeGeschoss = new List<Zonenumriss>();
                for (int g = 0; g < geordnet.Count; g++)
                {
                    // Raumumriss und Eingangsraum stehen an derselben Stelle.
                    List<int> stellen = Enumerable.Range(0, raeume.Count).Where(k => raeume[k].Zone == i && raeume[k].Geschoss == g).ToList();
                    if (stellen.Count == 0) continue;
                    List<Raumumriss> hier = stellen.Select(k => raeume[k]).ToList();
                    jeGeschoss.Add(new Zonenumriss
                    {
                        Zone = i,
                        Geschoss = g,
                        Raeume = hier,
                        Herkunft = hier.All(r => r.Herkunft == Geometrieherkunft.Raumgrenzen) ? Geometrieherkunft.Raumgrenzen : Geometrieherkunft.Schematisch,
                        FlaecheM2 = hier.Sum(r => r.PolygonflaecheM2),
                        HoeheM = HoeheAus(stellen.Select(k => eingang.Raeume[k])) ?? uz.HoeheM,
                    });
                }
                umrisse.AddRange(jeGeschoss);
                zonen.Add(new Zonenangabe
                {
                    Stelle = i,
                    Schluessel = uz.Schluessel ?? "",
                    Name = uz.Name ?? "",
                    IstBeheizt = uz.IstBeheizt,
                    Handgeaendert = uz.Handgeaendert,
                    Herkunft = eigene.Count > 0 && eigene.All(r => r.Herkunft == Geometrieherkunft.Raumgrenzen)
                        ? Geometrieherkunft.Raumgrenzen : Geometrieherkunft.Schematisch,
                    FlaecheM2 = uz.FlaecheM2,
                    VolumenM3 = uz.VolumenM3,
                    HoeheM = uz.HoeheM,
                    Umrisse = jeGeschoss,
                });
            }
            z.Zonen = zonen;
            z.Umrisse = umrisse;
            z.Nachbarpaare = paare.Select(p => p.Abschluss(nachgezogen)).ToList();
            z.Meldungen = Melden(raeume).Concat(Paarmeldungen(paare)).ToList();
            return z;
        }

        /// <summary>Der Arbeitsstand eines Raums beim Bilden.</summary>
        private sealed class Umrissentwurf
        {
            internal Umrissraum Raum;
            internal int Geschoss;
            internal int Zone;
            internal Geometrieherkunft Herkunft;
            internal Umrissherleitung Herleitung;
            internal List<Umrisspolygon> Polygone = new List<Umrisspolygon>();
            internal readonly HashSet<Umrissseite> Platziert = new HashSet<Umrissseite>();
            internal List<Grenzverweis> Boden = new List<Grenzverweis>();
            internal List<Grenzverweis> Decke = new List<Grenzverweis>();
            internal List<Grenzverweis> OhneKante = new List<Grenzverweis>();
            internal double L, B;

            // Stufe G7b — Lage und Kanten des schematischen Rechtecks (nie gedreht).
            internal double X, Y;
            internal bool Angelegt;
            internal List<Kantenabschnitt> BodenStreifen = new List<Kantenabschnitt>();
            internal List<Kantenabschnitt> DeckenStreifen = new List<Kantenabschnitt>();
            internal readonly Dictionary<Umrissseite, int> Kante = new Dictionary<Umrissseite, int>();
        }

        // ------------------------------------------------------------------
        //  Umriss aus Raumgrenzen
        // ------------------------------------------------------------------

        /// <summary>
        /// Die Polygone der Seiten einer Stellung mit Randpunktring, in die Grundrissebene projiziert und
        /// gegen den Uhrzeigersinn gerichtet; die Kanten tragen die Wände des Raums, die auf ihnen stehen.
        /// </summary>
        /// <param name="r">Der Raum.</param>
        /// <param name="stellung">Boden oder Decke.</param>
        /// <param name="drehung">Die Drehung gegen Nord [°], die für die Azimute gilt.</param>
        /// <param name="platziert">Nimmt jede Wand auf, die auf einer Kante steht — über alle Polygone des Raums.</param>
        private static List<Umrisspolygon> Projiziert(Umrissraum r, Grenzstellung stellung, double drehung, HashSet<Umrissseite> platziert)
        {
            var polygone = new List<Umrisspolygon>();
            List<Umrissseite> waende = r.Seiten.Where(s => s.Verweis?.Stellung == Grenzstellung.Wand && s.RandpunkteM != null).ToList();
            foreach (Umrissseite s in r.Seiten.Where(x => x.Verweis?.Stellung == stellung && x.RandpunkteM != null))
            {
                List<double[]> punkte = Grundriss(s.RandpunkteM, out double flaeche);
                if (punkte == null) continue;
                var kanten = new List<Umrisskante>(punkte.Count);
                for (int i = 0; i < punkte.Count; i++)
                {
                    double[] a = punkte[i], b = punkte[(i + 1) % punkte.Count];
                    double dx = b[0] - a[0], dy = b[1] - a[1];
                    double laenge = Math.Sqrt(dx * dx + dy * dy);
                    if (laenge < PUNKT_GLEICH_M)
                    {
                        kanten.Add(new Umrisskante(i, laenge, null, Array.Empty<Grenzverweis>()));
                        continue;
                    }
                    // Gegen den Uhrzeigersinn liegt das Innere links; die Außennormale zeigt nach rechts.
                    double azimut = Normiert(ModellAzimut(dy / laenge, -dx / laenge) - drehung);
                    var grenzen = new List<Grenzverweis>();
                    foreach (Umrissseite w in waende)
                    {
                        if (!AufKante(w.RandpunkteM, a, dx / laenge, dy / laenge, laenge)) continue;
                        grenzen.Add(w.Verweis);
                        platziert.Add(w);
                    }
                    kanten.Add(new Umrisskante(i, laenge, azimut, grenzen));
                }
                double ebene = s.RandpunkteM.Average(p => p.Length > 2 ? p[2] : 0.0);
                polygone.Add(new Umrisspolygon(punkte, kanten, flaeche, ebene, s.Verweis));
            }
            return polygone;
        }

        /// <summary>
        /// Der Ring in der Grundrissebene: x und y der Weltkoordinaten, ohne doppelte Folgepunkte und
        /// Schlusspunkt, gegen den Uhrzeigersinn (umgekehrt ab dem ersten Punkt, wenn nötig); <c>null</c>,
        /// wenn weniger als drei Punkte bleiben oder der Inhalt unter <see cref="FLAECHE_MIN_M2"/> liegt.
        /// </summary>
        internal static List<double[]> Grundriss(IReadOnlyList<double[]> ring, out double flaeche)
        {
            flaeche = 0.0;
            if (ring == null) return null;
            var punkte = new List<double[]>();
            foreach (double[] q in ring)
            {
                if (q == null || q.Length < 2) return null;
                var p = new[] { q[0], q[1] };
                if (punkte.Count == 0 || !Gleich(punkte[punkte.Count - 1], p)) punkte.Add(p);
            }
            if (punkte.Count > 1 && Gleich(punkte[0], punkte[punkte.Count - 1])) punkte.RemoveAt(punkte.Count - 1);
            if (punkte.Count < 3) return null;
            double doppelt = DoppelteFlaeche(punkte);
            if (Math.Abs(doppelt) / 2.0 < FLAECHE_MIN_M2) return null;
            if (doppelt < 0.0)
            {
                var umgekehrt = new List<double[]>(punkte.Count) { punkte[0] };
                for (int i = punkte.Count - 1; i >= 1; i--) umgekehrt.Add(punkte[i]);
                punkte = umgekehrt;
            }
            flaeche = Math.Abs(doppelt) / 2.0;
            return punkte;
        }

        /// <summary>Die doppelte vorzeichenbehaftete Fläche (Gaußsche Trapezformel, bezogen auf den ersten Punkt).</summary>
        internal static double DoppelteFlaeche(IReadOnlyList<double[]> punkte)
        {
            double summe = 0.0;
            double x0 = punkte[0][0], y0 = punkte[0][1];
            for (int i = 1; i + 1 < punkte.Count; i++)
                summe += (punkte[i][0] - x0) * (punkte[i + 1][1] - y0) - (punkte[i + 1][0] - x0) * (punkte[i][1] - y0);
            return summe;
        }

        /// <summary>
        /// Steht eine Wand auf der Kante ab <paramref name="a"/> in Richtung (<paramref name="ux"/>,
        /// <paramref name="uy"/>)? Alle projizierten Punkte liegen höchstens <see cref="KANTE_ABSTAND_M"/>
        /// neben der Geraden, und ihre Spanne überdeckt die Kante um mehr als <see cref="KANTE_UEBERLAPPUNG_M"/>.
        /// </summary>
        private static bool AufKante(IReadOnlyList<double[]> ring, double[] a, double ux, double uy, double laenge)
        {
            double tief = double.MaxValue, hoch = double.MinValue;
            foreach (double[] q in ring)
            {
                if (q == null || q.Length < 2) return false;
                double rx = q[0] - a[0], ry = q[1] - a[1];
                if (Math.Abs(ux * ry - uy * rx) > KANTE_ABSTAND_M) return false;
                double t = ux * rx + uy * ry;
                tief = Math.Min(tief, t);
                hoch = Math.Max(hoch, t);
            }
            return Math.Min(hoch, laenge) - Math.Max(tief, 0.0) > KANTE_UEBERLAPPUNG_M;
        }

        private static bool Gleich(double[] a, double[] b)
            => Math.Abs(a[0] - b[0]) < PUNKT_GLEICH_M && Math.Abs(a[1] - b[1]) < PUNKT_GLEICH_M;

        /// <summary>
        /// Die Streuung der Randpunkte eines Rings um seine Ebene [m] — größter minus kleinster Abstand zur
        /// Ebene der Newell-Normalen, wie beim IFC-Weg; <c>null</c>, wenn der Ring keine Fläche aufspannt.
        /// Eben ist er bis <see cref="EBEN_TOLERANZ_M"/>.
        /// </summary>
        internal static double? Ebenheit(IReadOnlyList<double[]> ring)
        {
            if (ring == null || ring.Count < 3 || ring.Any(q => q == null || q.Length < 3)) return null;
            double nx = 0.0, ny = 0.0, nz = 0.0;
            for (int i = 0; i < ring.Count; i++)
            {
                double[] a = ring[i], b = ring[(i + 1) % ring.Count];
                nx += (a[1] - b[1]) * (a[2] + b[2]);
                ny += (a[2] - b[2]) * (a[0] + b[0]);
                nz += (a[0] - b[0]) * (a[1] + b[1]);
            }
            double betrag = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (!(betrag > 1e-12)) return null;
            double ux = nx / betrag, uy = ny / betrag, uz = nz / betrag;
            double tief = double.MaxValue, hoch = double.MinValue;
            foreach (double[] q in ring)
            {
                double d = (q[0] - ring[0][0]) * ux + (q[1] - ring[0][1]) * uy + (q[2] - ring[0][2]) * uz;
                tief = Math.Min(tief, d);
                hoch = Math.Max(hoch, d);
            }
            return hoch - tief;
        }

        // ------------------------------------------------------------------
        //  Rechteckersatz (Mehrzonenkonzept 6.7; Datenaustauschkonzept 5.5)
        // ------------------------------------------------------------------

        /// <summary>
        /// Die Seiten l (x, Nord- und Südkante) und b (y, Ost- und Westkante) des Rechtecks eines Raums:
        /// h = V/A, sonst die Höhe der Datei; A_NS und A_OW sind die Bruttoflächen der Wände nach Sektor. Mit
        /// Höhe und Wänden beider Richtungen l = A_NS/(2h), b = A_OW/(2h) — weicht l·b um mehr als
        /// <see cref="RECHTECK_TOLERANZ"/> von A ab, die Fläche A mit demselben Seitenverhältnis; sonst das
        /// Quadrat √A. Ohne Fläche kein Rechteck (0, 0, <see cref="Umrissherleitung.Keine"/>).
        /// </summary>
        internal static (double L, double B, Umrissherleitung Herleitung) Rechteck(Umrissraum r)
        {
            if (!(r.FlaecheM2 > 0.0)) return (0.0, 0.0, Umrissherleitung.Keine);
            double a = r.FlaecheM2.Value;
            double? h = Hoehe(r);
            double ans = 0.0, aow = 0.0;
            foreach (Umrissseite s in r.Seiten)
            {
                if (s.Verweis?.Stellung != Grenzstellung.Wand || !(s.FlaecheM2 > 0.0) || !s.Sektor.HasValue) continue;
                if (s.Sektor.Value == 0 || s.Sektor.Value == 2) ans += s.FlaecheM2.Value;
                else aow += s.FlaecheM2.Value;
            }
            if (h > 0.0 && ans > 0.0 && aow > 0.0)
            {
                double l = ans / (2.0 * h.Value), b = aow / (2.0 * h.Value);
                if (Math.Abs(l * b - a) <= RECHTECK_TOLERANZ * a) return (l, b, Umrissherleitung.Wandflaechen);
                double f = Math.Sqrt(a / (l * b));
                return (l * f, b * f, Umrissherleitung.Seitenverhaeltnis);
            }
            double seite = Math.Sqrt(a);
            return (seite, seite, Umrissherleitung.Quadrat);
        }

        /// <summary>Die Höhe eines Raums: V/A, sonst die der Datei; <c>null</c> = keine.</summary>
        private static double? Hoehe(Umrissraum r)
            => r.VolumenM3 > 0.0 && r.FlaecheM2 > 0.0 ? r.VolumenM3.Value / r.FlaecheM2.Value : r.HoeheM > 0.0 ? r.HoeheM : null;

        /// <summary>ΣV/ΣA der Räume, wenn jeder beides trägt; sonst <c>null</c>.</summary>
        private static double? HoeheAus(IEnumerable<Umrissraum> raeume)
        {
            double v = 0.0, a = 0.0;
            foreach (Umrissraum r in raeume)
            {
                if (!(r.VolumenM3 > 0.0) || !(r.FlaecheM2 > 0.0)) return null;
                v += r.VolumenM3.Value;
                a += r.FlaecheM2.Value;
            }
            return a > 0.0 ? v / a : (double?)null;
        }

        /// <summary>
        /// Reiht die Blöcke eines Geschosses in Zeilen ab (<paramref name="x0"/>, <paramref name="oben"/>)
        /// nach rechts und unten, oben bündig, Abstand <see cref="ABSTAND_M"/>: Die Zeilenbreite ist die Breite
        /// eines Blocks im Verhältnis <see cref="BLOCK_SEITENVERHAELTNIS"/> aus der Fläche aller Blöcke samt
        /// Abständen, mindestens der breiteste Block. Ein Block ist ein Rechteck oder eine Gruppe
        /// aneinandergelegter Rechtecke (Stufe G7b). Nord oben: die Kanten eines Rechtecks laufen
        /// Süd, Ost, Nord, West, und jede trägt die Wände ihres Sektors.
        /// </summary>
        private static void Reihen(List<Rechteckblock> bloecke, double x0, double oben)
        {
            double breite = Math.Max(bloecke.Max(b => b.Breite),
                                     Math.Sqrt(BLOCK_SEITENVERHAELTNIS * bloecke.Sum(b => (b.Breite + ABSTAND_M) * (b.Tiefe + ABSTAND_M))));
            double x = x0, zeileOben = oben, zeileHoehe = 0.0;
            int inZeile = 0;
            foreach (Rechteckblock b in bloecke)
            {
                if (inZeile > 0 && x - x0 + b.Breite > breite)
                {
                    zeileOben -= zeileHoehe + ABSTAND_M;
                    x = x0;
                    zeileHoehe = 0.0;
                    inZeile = 0;
                }
                foreach (Umrissentwurf e in b.Glieder)
                {
                    e.X = x + (e.X - b.MinX);
                    e.Y = zeileOben - b.Tiefe + (e.Y - b.MinY);
                    Rechteckpolygon(e);
                }
                x += b.Breite + ABSTAND_M;
                zeileHoehe = Math.Max(zeileHoehe, b.Tiefe);
                inZeile++;
            }
        }

        // ------------------------------------------------------------------
        //  Meldungen und Hilfen
        // ------------------------------------------------------------------

        private static List<PruefMeldung> Melden(List<Raumumriss> raeume)
        {
            var meldungen = new List<PruefMeldung>();
            void Sammel(string schluessel, PruefStufe stufe, Func<Raumumriss, bool> wo)
            {
                List<string> namen = raeume.Where(wo).Select(r => r.Name).ToList();
                if (namen.Count == 0) return;
                string beispiele = namen.Count <= 5 ? string.Join(", ", namen) : string.Join(", ", namen.Take(5)) + ", …";
                meldungen.Add(new PruefMeldung(stufe, schluessel, namen.Count.ToString(CultureInfo.InvariantCulture), beispiele));
            }
            Sammel(SCHEMATISCH, PruefStufe.Info, r => r.Herkunft == Geometrieherkunft.Schematisch && r.Polygone.Count > 0);
            Sammel(SEITENVERHAELTNIS, PruefStufe.Info, r => r.Herleitung == Umrissherleitung.Seitenverhaeltnis);
            Sammel(QUADRAT, PruefStufe.Info, r => r.Herleitung == Umrissherleitung.Quadrat);
            Sammel(OHNE_FLAECHE, PruefStufe.Warnung, r => r.Polygone.Count == 0);
            return meldungen;
        }

        /// <summary>Der Azimut einer waagerechten Richtung im Modellsystem [°]: gegen +y, im Uhrzeigersinn (+x = 90°), in [0, 360).</summary>
        internal static double ModellAzimut(double dx, double dy) => Normiert(Math.Atan2(dx, dy) * 180.0 / Math.PI);

        private static double Normiert(double grad)
        {
            double a = grad % 360.0;
            if (a < 0.0) a += 360.0;
            return a + 0.0;
        }
    }
}
