using System;
using System.Collections.Generic;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// Wie eine Tabellenzeile der Kostenseite zu lesen ist (iU9-W5.4). Der
/// Vorläufer trug das als Hintergrundfarbe: rosa = das Gewerk führt nirgends
/// eine Position, gelb = Positionen ohne (gültige) Anlagenzuordnung.
/// </summary>
public enum ZeilenArt
{
    /// <summary>Gewöhnliche Zeile.</summary>
    Normal,

    /// <summary>Verbautes Gewerk ohne Kostenposition — eine FEHLENDE EINGABE.</summary>
    OhnePosition,

    /// <summary>Positionen ohne (gültige) Anlagenzuordnung; sie rechnen mit.</summary>
    OhneZuordnung,

    /// <summary>Die Summenzeile (fett).</summary>
    Summe,

    /// <summary>Eine erklärende Zeile statt eines leeren Rasters (grau, kursiv).</summary>
    Hinweis
}

/// <summary>Eine Zeile der Anlagen-/Komponententabelle.</summary>
public sealed class KostenZeile
{
    /// <summary>Schlüssel der Zeile — die Hülle erkennt daran, was gemeint ist.</summary>
    public int Schluessel { get; set; }

    /// <summary>„Komponente — Bezeichner" bzw. „Komponente — ohne Anlagenzuordnung".</summary>
    public string Anzeige { get; set; } = "";

    /// <summary>Investitionssumme als Text; „—" = keine Position.</summary>
    public string Summe { get; set; } = "";

    /// <summary>Betriebskosten als Text.</summary>
    public string Betrieb { get; set; } = "";

    /// <summary>Der Energieträger dieser Anlage (0 = keiner) — die Zeile markiert ihn rechts.</summary>
    public int TraegerId { get; set; }

    /// <summary>Wie die Zeile zu lesen ist.</summary>
    public ZeilenArt Art { get; set; } = ZeilenArt.Normal;

    /// <summary>Kurztext (der Tooltip des Vorläufers).</summary>
    public string Kurztext { get; set; } = "";

    /// <summary>
    /// Lassen sich die losen Positionen dieser Zeile löschen? Der Vorläufer
    /// nahm dafür einen DOPPELKLICK auf die gelbe Zeile; hier steht ein Knopf
    /// in der Zeile (Abweichung A-3 der Welle 5).
    /// </summary>
    public bool Loeschbar { get; set; }
}

/// <summary>Eine Zeile der Energieträgertabelle (zehn Spalten).</summary>
public sealed class TraegerZeile
{
    /// <summary><c>energy_carrier.id</c> — Schlüssel für die Markierung aus der Anlagenzeile.</summary>
    public int TraegerId { get; set; }

    /// <summary>Die Zellen in Spaltenreihenfolge (fertig formatiert).</summary>
    public IReadOnlyList<string> Zellen { get; set; } = Array.Empty<string>();

    /// <summary>Wie die Zeile zu lesen ist (Fehlzeile = rosa, Hinweis = grau/kursiv).</summary>
    public ZeilenArt Art { get; set; } = ZeilenArt.Normal;

    /// <summary>Kurztext der ganzen Zeile (Verursacher, Grund).</summary>
    public string Kurztext { get; set; } = "";

    /// <summary>Kurztext der drei Emissionsspalten (Herkunftsebene, Modus).</summary>
    public string EmissionKurztext { get; set; } = "";
}

/// <summary>
/// Der Anzeigestand der Kostenseite — was der Vorläufer in
/// <c>UcBkKosten.Aktualisiere</c>, <c>LadeKomponenten</c> und
/// <c>LadeTraeger</c> zusammentrug, in einer Antwort.
/// </summary>
public sealed class KostenStand
{
    /// <summary>„Projekt: &lt;Name&gt;" bzw. der Satz „kein Projekt gewählt".</summary>
    public string Projektzeile { get; set; } = "";

    /// <summary>Gibt es ein Projekt? <c>false</c> sperrt die beiden Knöpfe.</summary>
    public bool Bedienbar { get; set; }

    /// <summary>Die drei Kategorie-Karten (Investition, Betrieb, Energie).</summary>
    public IReadOnlyList<KachelZeile> Kacheln { get; set; } = Array.Empty<KachelZeile>();

    /// <summary>Die Anlagen-/Komponentenzeilen samt Summenzeile.</summary>
    public IReadOnlyList<KostenZeile> Komponenten { get; set; } = Array.Empty<KostenZeile>();

    /// <summary>Die zehn Spaltenköpfe der Energieträgertabelle.</summary>
    public IReadOnlyList<string> TraegerSpalten { get; set; } = Array.Empty<string>();

    /// <summary>Die Energieträgerzeilen.</summary>
    public IReadOnlyList<TraegerZeile> Traeger { get; set; } = Array.Empty<TraegerZeile>();

    /// <summary>Die Fußzeile mit allen Befunden (Vorbild <c>BK_KOSTEN_STATUS</c> &amp;c.).</summary>
    public string Statuszeile { get; set; } = "";
}
