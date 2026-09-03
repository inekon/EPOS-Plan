using System;
using System.Collections.Generic;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// Eine Zeile des Komponentenbereichs der Übersichtsseite (iU9-W5.5).
///
/// <para>Sie trägt beide Ansichten: die GEGENÜBERSTELLUNG auf der Stammzeile
/// (Gewerk · Merkmal · Stamm · je Variante eine Spalte) und die
/// UNTERSCHIEDE auf einer Variantenzeile (Gewerk · Merkmal · Stamm ·
/// Variante · Aktion). Der Unterschied steckt allein in der Spaltenzahl und
/// darin, ob <see cref="MitAktion"/> gesetzt ist.</para>
/// </summary>
public sealed class VergleichZeile
{
    /// <summary>
    /// Schlüssel der Zeile für die Übernahme; 0 = keine (Vergleichsansicht).
    /// Die Hülle erkennt daran, welches Merkmal bzw. welches Gewerk gemeint
    /// ist.
    /// </summary>
    public int Schluessel { get; set; }

    /// <summary>Das Gewerk — nur in der ersten Zeile seines Blocks belegt (fett).</summary>
    public string Gewerk { get; set; } = "";

    /// <summary>Das Merkmal bzw. „Komponente n".</summary>
    public string Merkmal { get; set; } = "";

    /// <summary>Je Version eine fertig formatierte Zelle.</summary>
    public IReadOnlyList<string> Zellen { get; set; } = Array.Empty<string>();

    /// <summary>Kurztexte je Zelle (die Merkmale einer Komponente); leer = keiner.</summary>
    public IReadOnlyList<string> Kurztexte { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Trägt diese Zeile die Aktionsspalte? Nur in der Unterschiedsansicht.
    /// </summary>
    public bool MitAktion { get; set; }

    /// <summary>
    /// Warum die Übernahme nicht trägt (Schlüsselspalte, unbekanntes Gewerk);
    /// leer = sie trägt. Der Vorläufer setzte dafür statt des Knopfes einen
    /// grauen Strich mit Begründung — „ein sichtbarer, aber wirkungsloser
    /// Knopf wäre die schlechtere Auskunft".
    /// </summary>
    public string Sperrgrund { get; set; } = "";

    /// <summary>Kurztext des Übernahmeknopfes.</summary>
    public string AktionKurztext { get; set; } = "";
}

/// <summary>
/// Der Anzeigestand der Übersichtsseite — was der Vorläufer in
/// <c>LadeProjekte</c>, <c>LadeAuswahl</c> und <c>ZeigeKomponenten</c>
/// zusammentrug, in einer Antwort.
/// </summary>
public sealed class UebersichtStand
{
    /// <summary>Die wählbaren Stammprojekte (Id = <c>Tab_Projekt.ID</c>).</summary>
    public IReadOnlyList<(int Id, string Text)> Staemme { get; set; }
        = Array.Empty<(int, string)>();

    /// <summary>Das gewählte Stammprojekt; <c>null</c> = keins.</summary>
    public int? StammId { get; set; }

    /// <summary>Stand des Filters „nur Stammprojekte".</summary>
    public bool NurStaemme { get; set; }

    /// <summary>Stamm und Varianten der Gruppe, Stamm zuerst.</summary>
    public IReadOnlyList<VarianteZeile> Zeilen { get; set; } = Array.Empty<VarianteZeile>();

    /// <summary>Die markierte Zeile (<c>Tab_Projekt.ID</c>); -1 = keine.</summary>
    public int MarkierteId { get; set; } = -1;

    /// <summary>Vorbelegung des Bezeichnerfeldes für „Variante anlegen".</summary>
    public string Bezeichnervorschlag { get; set; } = "";

    /// <summary>Überschrift des Komponentenbereichs (Vergleich bzw. Unterschiede).</summary>
    public string KomponentenTitel { get; set; } = "";

    /// <summary>Die Spaltenköpfe des Komponentenbereichs.</summary>
    public IReadOnlyList<string> Spalten { get; set; } = Array.Empty<string>();

    /// <summary>Die Zeilen des Komponentenbereichs.</summary>
    public IReadOnlyList<VergleichZeile> Vergleich { get; set; } = Array.Empty<VergleichZeile>();

    /// <summary>Ist die markierte Zeile löschbar (eine Variante)?</summary>
    public bool Loeschbar { get; set; }

    /// <summary>Lässt sich eine Variante anlegen (es gibt ein Stammprojekt)?</summary>
    public bool AnlegenMoeglich { get; set; }

    /// <summary>Lässt sich simulieren (es gibt eine markierte Zeile)?</summary>
    public bool SimulierenMoeglich { get; set; }

    /// <summary>Die Statuszeile.</summary>
    public string Statuszeile { get; set; } = "";
}
