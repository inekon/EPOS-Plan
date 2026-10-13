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
/// Ein Eintrag einer Zelle der Gegenüberstellung je Gewerk: „n × Komponente“ und
/// darunter leise ihre Kennwerte (aus <c>KomponentenVergleich.JeGewerk</c>).
/// </summary>
public sealed class GewerkVergleichEintrag
{
    /// <summary>Wie oft die Version diese Komponente führt.</summary>
    public int Anzahl { get; set; }

    /// <summary>Der Bezeichner der Komponente.</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Die Kennwerte, fertig formatiert („16 kW“); leer = keine.</summary>
    public string Kennwerte { get; set; } = "";

    /// <summary>Die Merkmale der Komponente als Kurztext; leer = keiner.</summary>
    public string Kurztext { get; set; } = "";
}

/// <summary>Die Zelle einer Version in der Gegenüberstellung je Gewerk.</summary>
public sealed class GewerkVergleichZelle
{
    /// <summary>Die Einträge; leer = die Version führt das Gewerk nicht.</summary>
    public IReadOnlyList<GewerkVergleichEintrag> Eintraege { get; set; } = Array.Empty<GewerkVergleichEintrag>();

    /// <summary>Weicht die Zelle vom Stammprojekt ab? (Der Kern entscheidet.)</summary>
    public bool WeichtAb { get; set; }
}

/// <summary>Eine Zeile der Gegenüberstellung: ein Gewerk, je Version eine Zelle.</summary>
public sealed class GewerkVergleichZeile
{
    /// <summary>Der Anzeigename des Gewerks.</summary>
    public string Gewerk { get; set; } = "";

    /// <summary>Je Version eine Zelle, der Stamm zuerst — in der Reihenfolge der <see cref="UebersichtStand.Versionsspalten"/>.</summary>
    public IReadOnlyList<GewerkVergleichZelle> Zellen { get; set; } = Array.Empty<GewerkVergleichZelle>();

    /// <summary>Weicht wenigstens eine Version in diesem Gewerk vom Stamm ab?</summary>
    public bool WeichtAb
    {
        get
        {
            foreach (GewerkVergleichZelle z in Zellen)
                if (z.WeichtAb) return true;
            return false;
        }
    }
}

/// <summary>Der Spaltenkopf einer Version: ihr Name und ihr Simulationsstand.</summary>
public sealed class VersionsSpalte
{
    /// <summary>„Stamm“ bzw. der Bezeichner der Variante.</summary>
    public string Name { get; set; } = "";

    /// <summary>„simuliert 12.10.26 23:05“ bzw. „nicht simuliert“; leer = unbekannt.</summary>
    public string Stand { get; set; } = "";
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

    /// <summary>
    /// Die Versionen im Vergleich (Ids, Stamm eingeschlossen) — Anwenderwunsch 08.09.2026,
    /// W5‑B‑5. Nur sie bekommen in der Gegenüberstellung eine Spalte; dieselbe Wahl gilt
    /// auf den Seiten Kosten und Wirtschaftlichkeit.
    /// </summary>
    public IReadOnlyList<int> GewaehlteVarianten { get; set; } = Array.Empty<int>();

    /// <summary>Vorbelegung des Bezeichnerfeldes für „Variante anlegen".</summary>
    public string Bezeichnervorschlag { get; set; } = "";

    /// <summary>Überschrift des Komponentenbereichs (Vergleich bzw. Unterschiede).</summary>
    public string KomponentenTitel { get; set; } = "";

    /// <summary>Die Spaltenköpfe des Komponentenbereichs.</summary>
    public IReadOnlyList<string> Spalten { get; set; } = Array.Empty<string>();

    /// <summary>Die Zeilen des Komponentenbereichs.</summary>
    public IReadOnlyList<VergleichZeile> Vergleich { get; set; } = Array.Empty<VergleichZeile>();

    /// <summary>
    /// Die Gegenüberstellung je Gewerk (Stammzeile markiert); leer = keine — dann
    /// zeigt die Seite die <see cref="Vergleich"/>-Zeilen (die Unterschiede einer Variante).
    /// </summary>
    public IReadOnlyList<GewerkVergleichZeile> Gewerke { get; set; } = Array.Empty<GewerkVergleichZeile>();

    /// <summary>Die Spaltenköpfe der Versionen zu <see cref="Gewerke"/>, der Stamm zuerst.</summary>
    public IReadOnlyList<VersionsSpalte> Versionsspalten { get; set; } = Array.Empty<VersionsSpalte>();

    /// <summary>Ist die markierte Zeile löschbar (eine Variante)?</summary>
    public bool Loeschbar { get; set; }

    /// <summary>Lässt sich eine Variante anlegen (es gibt ein Stammprojekt)?</summary>
    public bool AnlegenMoeglich { get; set; }

    /// <summary>Lässt sich simulieren (es gibt eine markierte Zeile)?</summary>
    public bool SimulierenMoeglich { get; set; }

    /// <summary>Die Statuszeile.</summary>
    public string Statuszeile { get; set; } = "";
}
