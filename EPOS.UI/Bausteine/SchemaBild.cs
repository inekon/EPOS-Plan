using System.Collections.Generic;

namespace EPOS.UI.Bausteine;

/// <summary>Spalte eines Schema-Knotens — die vier Rubriken des Entwurfs.</summary>
public enum SchemaKnotenart
{
    /// <summary>Spalte 0: Waermequelle (Aussenluft, Erdsonde, Brennstoff …).</summary>
    Quelle,

    /// <summary>Spalte 1: Waermeerzeuger.</summary>
    Erzeuger,

    /// <summary>Spalte 2: Pufferspeicher.</summary>
    Speicher,

    /// <summary>Spalte 3: Abnehmer (Heizkreis, Warmwasser, Prozess).</summary>
    Abnehmer
}

/// <summary>Farbsprache der Verbindungen (Legende des Entwurfs).</summary>
public enum SchemaKantenart
{
    /// <summary>Blau: Quellseite (Quelle → Erzeuger).</summary>
    Quelle,

    /// <summary>Blau gestrichelt: Kaskade (Puffer → nachgeschalteter Erzeuger).</summary>
    Kaskade,

    /// <summary>Koralle: Ladung (Erzeuger → Puffer), Kreis = wirksame Prioritaet.</summary>
    Ladung,

    /// <summary>Gruen: Versorgung / Entladung.</summary>
    Versorgung,

    /// <summary>Violett: Versorgung des PROZESS-Abnehmers.</summary>
    Prozess
}

/// <summary>
/// Ein Kasten des Schemas samt seiner Flaeche — bereits angeordnet
/// (<c>SchemaLayout</c> im Kern).
/// </summary>
/// <param name="Schluessel">Sprachneutraler Knotenschluessel, z. B. „ERZEUGER_11203".</param>
/// <param name="IstWaermepumpe">
/// Nur bei <see cref="SchemaKnotenart.Quelle"/> von Belang: Die Quelle einer Waermepumpe
/// bekommt den blauen Rahmen, jede andere den leisen.
/// </param>
public sealed record SchemaKnoten(
    string Schluessel,
    SchemaKnotenart Art,
    int X, int Y, int Breite, int Hoehe,
    string Rang,
    string Titel,
    IReadOnlyList<string> Zeilen,
    IReadOnlyList<string> Badges,
    string Hinweis,
    bool Warnung,
    string Warntext,
    bool Kaskade,
    bool IstWaermepumpe);

/// <summary>
/// Eine Leitung samt fertigem Bezier-Pfad.
/// </summary>
/// <param name="Pfad">Die <c>d</c>-Angabe („M … C … …").</param>
/// <param name="MitteX">Mitte der Kurve — dort sitzt der Prioritaetskreis.</param>
public sealed record SchemaKante(
    string Von,
    string Nach,
    SchemaKantenart Art,
    int Prioritaet,
    string Pfad,
    int MitteX, int MitteY);

/// <summary>Ein Glied des Kaskadenbands samt seiner Pillenflaeche.</summary>
public sealed record SchemaBandglied(
    string Schluessel,
    string Text,
    SchemaKnotenart Art,
    SchemaKantenart PfeilDavor,
    int X, int Y, int Breite, int Hoehe,
    bool Kettenanfang);

/// <summary>Ein Eintrag der Legende: eine Musterlinie und ihr Text.</summary>
public sealed record SchemaLegendeeintrag(
    string Text,
    SchemaKantenart Art,
    bool Gestrichelt);

/// <summary>
/// Das vollstaendig ANGEORDNETE Hydraulikschema — alles, was der Baustein
/// <c>Schema</c> zum Zeichnen braucht (iU9-W10b.0c).
///
/// <para><b>Warum eine eigene Form und nicht <c>SchemaLayout</c> selbst.</b> Der
/// Kern rechnet die Anordnung; die Komponente kennt seine Fachklassen nicht
/// (Hausregel EPOS.UI, dieselbe Trennung wie bei den sieben Dialogen der Welle 10a).
/// Die Huelle bildet <c>SchemaLayout</c> auf diesen Satz ab — eine Zuordnung ohne
/// Rechnung.</para>
/// </summary>
public sealed record SchemaBild(
    IReadOnlyList<SchemaKnoten> Knoten,
    IReadOnlyList<SchemaKante> Kanten,
    IReadOnlyList<SchemaBandglied> Band,
    IReadOnlyList<SchemaLegendeeintrag> Legende,
    IReadOnlyList<string> Spaltenkoepfe,
    IReadOnlyList<int> SpaltenX,
    IReadOnlyList<int> SpaltenBreite,
    int Breite,
    int Hoehe,
    int Rand,
    int KopfHoehe,
    int BandOben,
    int LegendeOben,
    bool IstLeer)
{
    /// <summary>Ein leeres Bild — der Zustand „noch keine Hydraulik konfiguriert".</summary>
    public static SchemaBild Leer { get; } = new SchemaBild(
        new List<SchemaKnoten>(), new List<SchemaKante>(), new List<SchemaBandglied>(),
        new List<SchemaLegendeeintrag>(), new List<string>(), new List<int>(), new List<int>(),
        890, 200, 18, 26, 100, 140, true);
}
