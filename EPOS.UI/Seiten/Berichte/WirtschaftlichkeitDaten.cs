using System;
using System.Collections.Generic;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>Eine Kennzahl-Karte über der Vergleichstabelle (KD6a).</summary>
public sealed class KachelZeile
{
    /// <summary>Überschrift der Karte.</summary>
    public string Titel { get; set; } = "";

    /// <summary>Der fertig formatierte Wert; leer = „—".</summary>
    public string Wert { get; set; } = "";

    /// <summary>Leise Zeile darunter: woher der Wert stammt.</summary>
    public string Quelle { get; set; } = "";
}

/// <summary>
/// Eine Zeile der Vergleichstabelle: der Kennzahltitel und je Version eine
/// Zelle (Vorbild <c>UcWirtschaftlichkeit.Zeile</c>).
/// </summary>
public sealed class MatrixZeile
{
    /// <summary>Der Kennzahltitel (erste Spalte, fett).</summary>
    public string Titel { get; set; } = "";

    /// <summary>Je Version eine fertig formatierte Zelle.</summary>
    public IReadOnlyList<string> Zellen { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Die Vergleichstabelle als Matrix.
///
/// <para><b>Warum kein <c>Raster</c>.</b> Ein <c>QuickGrid</c> braucht
/// Spalten, die zur Übersetzungszeit feststehen; hier entstehen sie zur
/// Laufzeit — eine je Version der Gruppe. Der Vorläufer baute dafür
/// <c>grid.Columns.Add</c> je Ergebniszeile. Die Blazor-Fassung schreibt eine
/// gewöhnliche Tabelle mit der Hausklasse <c>epos-raster</c>: dieselbe Optik,
/// ohne dem Baustein eine Fähigkeit anzudichten, die er nicht hat.</para>
/// </summary>
public sealed class ErgebnisMatrix
{
    /// <summary>Die Spaltenköpfe: „Kennzahl" und je Version einer.</summary>
    public IReadOnlyList<string> Spalten { get; set; } = Array.Empty<string>();

    /// <summary>Die Zeilen in Anzeigereihenfolge.</summary>
    public IReadOnlyList<MatrixZeile> Zeilen { get; set; } = Array.Empty<MatrixZeile>();
}

/// <summary>
/// Was ein Szenariowechsel neu zeigt (Vorbild
/// <c>UcWirtschaftlichkeit.ZeigeErgebnisse</c>): die vier Kennzahl-Karten und
/// die Vergleichstabelle. Gerechnet wird dabei nichts — die Hülle liest den
/// Lauf, der schon im Speicher liegt.
/// </summary>
public sealed class ErgebnisAnsicht
{
    /// <summary>Die vier Kennzahl-Karten (Kapitalwert, Annuität, Amortisation, IRR).</summary>
    public IReadOnlyList<KachelZeile> Kacheln { get; set; } = Array.Empty<KachelZeile>();

    /// <summary>Die Vergleichstabelle.</summary>
    public ErgebnisMatrix Matrix { get; set; } = new();
}

/// <summary>
/// Der Anzeigestand der Wirtschaftlichkeitsseite — was der Vorläufer in
/// <c>LadeDaten</c>, <c>AktualisiereListe</c>, <c>ZeigeParameterzeile</c> und
/// <c>BauePhotovoltaikKnopf</c> verteilt zusammentrug, in einer Antwort.
/// </summary>
public sealed class WirtschaftlichkeitStand
{
    /// <summary>Stamm und Varianten der Vergleichsgruppe, Stamm zuerst.</summary>
    public IReadOnlyList<VarianteZeile> Varianten { get; set; } = Array.Empty<VarianteZeile>();

    /// <summary>Die Ids der angehakten Versionen (der Stamm ist immer dabei).</summary>
    public IReadOnlyList<int> GewaehlteVarianten { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Die wählbaren Szenarien. Die PERSISTENZWERTE
    /// (<c>Tab_ErgebnisWirtschaftlichkeit.Szenario</c>) kennt nur die Hülle;
    /// hier stehen Nummer und Anzeigetext (Drei-Schichten-Regel).
    /// </summary>
    public IReadOnlyList<(int Id, string Text)> Szenarien { get; set; }
        = Array.Empty<(int, string)>();

    /// <summary>Das vorgewählte Szenario (0 = „Erwartet").</summary>
    public int SzenarioId { get; set; }

    /// <summary>Der Parameternachweis als eine Zeile (L12/L13).</summary>
    public string Parameterzeile { get; set; } = "";

    /// <summary>Die Kennzahlen und die Tabelle des vorgewählten Szenarios.</summary>
    public ErgebnisAnsicht Ansicht { get; set; } = new();

    /// <summary>Führt die Gruppe Photovoltaik? Dann erscheint „Photovoltaik…".</summary>
    public bool MitPhotovoltaik { get; set; }

    /// <summary>Führt die Gruppe ein BHKW? Dann erscheint „BHKW-Wirtschaftlichkeit…".</summary>
    public bool MitBhkw { get; set; }

    /// <summary>Wärmepumpe in der Gruppe oder aktive Tarifstruktur? Dann „Strombezug…".</summary>
    public bool MitStrombezug { get; set; }

    /// <summary>Die Statuszeile beim Aufbau (gespeicherter Stand, veraltet, keiner).</summary>
    public string Statuszeile { get; set; } = "";
}
