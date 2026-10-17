using System;
using System.Collections.Generic;
using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>Eine Version der Wahl über dem Energieflussbild: Projekt-Id, Name wie im Spaltenkopf, Simulationsstand.</summary>
public sealed class ErgebnisFlussVersion
{
    /// <summary>Die Projekt-Id der Version (Stamm oder Variante).</summary>
    public int IdProjekt { get; init; }

    /// <summary>Der Name wie im Spaltenkopf der Tabelle („Stamm“ bzw. der Bezeichner der Variante).</summary>
    public string Name { get; init; } = "";

    /// <summary>Liegt ein gespeicherter Simulationslauf vor? Sonst steht im Feld „(nicht simuliert)“.</summary>
    public bool Simuliert { get; init; } = true;
}

/// <summary>Ein Band des Energieflussbilds als Zeile der verborgenen Textalternative (Konzept 4.4).</summary>
public sealed class ErgebnisFlussZeile
{
    /// <summary>Der Name des Ausgangsknotens.</summary>
    public string Von { get; init; } = "";

    /// <summary>Der Name des Zielknotens.</summary>
    public string Nach { get; init; } = "";

    /// <summary>Die Jahresmenge, fertig formatiert, ohne Einheit („38,4“).</summary>
    public string Wert { get; init; } = "";

    /// <summary>Der Anteil am Abfluss des Ausgangsknotens, fertig formatiert, ohne Prozentzeichen („71“).</summary>
    public string Anteil { get; init; } = "";

    /// <summary>Woher die Zahl kommt („direkt“, „abgeleitet“, „bilanziert“).</summary>
    public string Herkunft { get; init; } = "";
}

/// <summary>Die Bilanz eines Knotens als Zeile der verborgenen Textalternative: Zufluss, Abfluss, Rest.</summary>
public sealed class ErgebnisFlussKnotenzeile
{
    /// <summary>Der Name des Knotens.</summary>
    public string Name { get; init; } = "";

    /// <summary>Der Zufluss, fertig formatiert in MWh/a ohne Einheit.</summary>
    public string Zufluss { get; init; } = "";

    /// <summary>Der Abfluss, fertig formatiert in MWh/a ohne Einheit.</summary>
    public string Abfluss { get; init; } = "";

    /// <summary>Der Rest (Zufluss − Abfluss), fertig formatiert in MWh/a ohne Einheit.</summary>
    public string Rest { get; init; } = "";
}

/// <summary>
/// <b>Der Stand des Energieflussbilds einer Version</b> (Konzept Energieflussbild 4.3) — fertig für die Komponente
/// <c>UebersichtErgebnisFluss</c>: zwei Zeichenmodelle (breit und schmal; das Stilblatt wählt unter 900 px das schmale),
/// die Zeilen der verborgenen Textalternative und die Hinweiszeilen. Die Komponente kennt keine Fachklasse des Kerns außer
/// dem Zeichenmodell; die Hülle hält den Stand je Version und Simulationsstand fest, die Komponente baut ihren Baum nur
/// bei einer neuen Referenz.
/// </summary>
public sealed class ErgebnisFlussStand
{
    /// <summary>Die Projekt-Id der gezeigten Version.</summary>
    public int IdProjekt { get; init; }

    /// <summary>Der Name der gezeigten Version.</summary>
    public string Version { get; init; } = "";

    /// <summary>Liegt für die Version kein Simulationsergebnis vor? Dann steht statt des Bildes die Karte.</summary>
    public bool NichtSimuliert { get; init; }

    /// <summary>Der benannte Grund, warum kein Bild entsteht (keine Flüsse); leer mit Bild.</summary>
    public string Grund { get; init; } = "";

    /// <summary>Das Zeichenmodell für die breite Fläche (1 180 px); <c>null</c> ohne Bild.</summary>
    public Zeichenmodell? Breit { get; init; }

    /// <summary>Das Zeichenmodell für die schmale Fläche (680 px); <c>null</c> ohne Bild.</summary>
    public Zeichenmodell? Schmal { get; init; }

    /// <summary>Je Band eine Zeile der Textalternative.</summary>
    public IReadOnlyList<ErgebnisFlussZeile> Fluesse { get; init; } = Array.Empty<ErgebnisFlussZeile>();

    /// <summary>Je Knoten seine Bilanz.</summary>
    public IReadOnlyList<ErgebnisFlussKnotenzeile> Knoten { get; init; } = Array.Empty<ErgebnisFlussKnotenzeile>();

    /// <summary>Die leisen Hinweiszeilen unter dem Bild (nicht geschlossene Knoten u. a.).</summary>
    public IReadOnlyList<string> Hinweise { get; init; } = Array.Empty<string>();

    /// <summary>Die Flussliste des Assistenten (<c>EnergieflussBilanz.KiText()</c>).</summary>
    public string KiText { get; init; } = "";

    /// <summary>Steht ein Bild?</summary>
    public bool HatBild => Breit is not null && Schmal is not null;

    /// <summary>
    /// Die Kanten des ungefilterten Bildes als JSON für die Hervorhebung (Konzept 3.3): je Band
    /// <c>["fluss:&lt;von&gt;:&lt;nach&gt;", "&lt;von&gt;", "&lt;nach&gt;"]</c>. Das Modul zerlegt die Marke nicht; ein gefiltertes
    /// Bild ist eine Teilmenge, eine Marke ohne Element wird übergangen. Leer = keine Hervorhebung.
    /// </summary>
    public string KantenJson { get; init; } = "[]";

    /// <summary>Die Kennungen der Busse des ungefilterten Bildes als JSON (<c>["strom", …]</c>) — ein Bus beendet den Weg.</summary>
    public string BusseJson { get; init; } = "[]";

    /// <summary>
    /// Baut das Bild einer Darstellung und eines Formfilters (Hülle, Kern <c>EnergieflussAbbildung.Gefiltert</c> →
    /// <c>ChartRenderer.EnergieflussModell</c> breit und schmal). Gerufen nur beim ersten Bedarf über <see cref="Bild"/>.
    /// </summary>
    public Func<string, string, ErgebnisFlussBild>? Bildbau { get; init; }

    private readonly Dictionary<string, ErgebnisFlussBild> _bilder = new(StringComparer.Ordinal);
    private readonly object _sperre = new();

    /// <summary>Wie viele Bilder dieser Stand schon gebaut hat (Prüfhilfe des Puffers).</summary>
    public int GebauteBilder { get { lock (_sperre) return _bilder.Count; } }

    /// <summary>
    /// <b>Das Bild einer Darstellung und eines Filters</b> (Konzept 3.5) — je Stand höchstens einmal gebaut und dann
    /// dieselbe Referenz, nie je Rendern. Kette und „Alle“ sind <see cref="Breit"/> und <see cref="Schmal"/>; ohne
    /// <see cref="Bildbau"/> gilt für jede Wahl dieses Bild. Unbekannte Werte gelten als Kette bzw. „Alle“.
    /// </summary>
    public ErgebnisFlussBild Bild(string? darstellung, string? filter)
    {
        string d = ErgebnisFlussWahl.Darstellung(darstellung), f = ErgebnisFlussWahl.Filter(filter);
        string schluessel = d + "|" + f;
        lock (_sperre)
        {
            if (_bilder.TryGetValue(schluessel, out ErgebnisFlussBild? da)) return da;
            ErgebnisFlussBild neu;
            if (d == ErgebnisFlussWahl.KETTE && f == ErgebnisFlussWahl.ALLE) neu = new ErgebnisFlussBild { Breit = Breit, Schmal = Schmal };
            else if (Bildbau is null) neu = Bild(ErgebnisFlussWahl.KETTE, ErgebnisFlussWahl.ALLE);   // die Sperre ist wiedereintrittsfähig
            else neu = Bildbau(d, f);
            _bilder[schluessel] = neu;
            return neu;
        }
    }
}

/// <summary>Ein Bild der Ansicht „Fluss“ zu einer Darstellung und einem Filter: breit, schmal und der Hinweis eines leeren Filters.</summary>
public sealed class ErgebnisFlussBild
{
    /// <summary>Das Zeichenmodell für die breite Fläche (1 180 px).</summary>
    public Zeichenmodell? Breit { get; init; }

    /// <summary>Das Zeichenmodell für die schmale Fläche (680 px).</summary>
    public Zeichenmodell? Schmal { get; init; }

    /// <summary>Der benannte Grund eines leeren Filters („Kälte: keine Flüsse in dieser Version“); leer = keiner.</summary>
    public string Hinweis { get; init; } = "";
}

/// <summary>
/// Die Ablagewerte der beiden Wahlen über dem Energieflussbild: Darstellung „Kette | Stufen“ (je Benutzer gemerkt, SK-Q17)
/// und Formfilter „Alle | Strom | Wärme | Kälte“ (je Sitzung). Sprachneutral; die Hülle bildet sie auf den Kern ab.
/// </summary>
public static class ErgebnisFlussWahl
{
    /// <summary>Die gestraffte Kette (Vorgabe, auch im Wortbericht).</summary>
    public const string KETTE = "kette";

    /// <summary>Die vier Stufen mit Verbindern.</summary>
    public const string STUFEN = "stufen";

    /// <summary>Jeder Fluss (Vorgabe).</summary>
    public const string ALLE = "alle";

    /// <summary>Nur der Strom und die Wandler, die ihn erzeugen oder verbrauchen.</summary>
    public const string STROM = "strom";

    /// <summary>Nur die Wärme und die Wandler, die sie erzeugen oder verbrauchen.</summary>
    public const string WAERME = "waerme";

    /// <summary>Nur die Kälte und die Wandler, die sie erzeugen oder verbrauchen.</summary>
    public const string KAELTE = "kaelte";

    /// <summary>Die Stellungen der Darstellung in Reihenfolge.</summary>
    public static readonly IReadOnlyList<string> DARSTELLUNGEN = new[] { KETTE, STUFEN };

    /// <summary>Die Stellungen des Filters in Reihenfolge.</summary>
    public static readonly IReadOnlyList<string> FILTER = new[] { ALLE, STROM, WAERME, KAELTE };

    /// <summary>Ein gültiger Wert der Darstellung; unbekannt oder leer = Kette.</summary>
    public static string Darstellung(string? wert) => wert == STUFEN ? STUFEN : KETTE;

    /// <summary>Ein gültiger Wert des Filters; unbekannt oder leer = „Alle“.</summary>
    public static string Filter(string? wert) => wert is STROM or WAERME or KAELTE ? wert : ALLE;
}

/// <summary>
/// Die Beschriftungen der Ansicht „Fluss“ (ein Bündel, Hausregel ab etwa zehn Texten); deutscher Rückfall je Eigenschaft,
/// die Hülle setzt die Ressourcen.
/// </summary>
public sealed class ErgebnisFlussTexte
{
    /// <summary>Die Stellung des Umschalters (<c>BK_ERG_ANSICHT_FLUSS</c>).</summary>
    public string Ansicht { get; init; } = "Fluss";

    /// <summary>Die Beschriftung der Versionswahl (<c>BK_ERG_FLUSS_VERSION</c>).</summary>
    public string Version { get; init; } = "Version";

    /// <summary>Der Zusatz einer nicht simulierten Version in der Wahl (<c>BK_LBL_NICHT_SIMULIERT</c>, in Klammern).</summary>
    public string NichtSimuliertZusatz { get; init; } = "nicht simuliert";

    /// <summary>Die Karte statt des Bildes für eine nicht simulierte Version (<c>BK_ERG_FLUSS_NICHT_SIMULIERT</c>).</summary>
    public string NichtSimuliert { get; init; } =
        "Für diese Version liegt kein Simulationsergebnis vor. Starten Sie die Simulation, um ihr Energieflussbild zu sehen.";

    /// <summary>Die Bezeichnung des Bildes, {0} = Version (<c>BK_ERG_FLUSS_BILD</c>).</summary>
    public string Bild { get; init; } = "Energieflussbild der Version {0}";

    /// <summary>Eine Bandzeile der Textalternative: Von, Nach, Wert, Anteil, Herkunft (<c>BK_ERG_FLUSS_ZEILE</c>).</summary>
    public string Zeile { get; init; } = "{0} → {1}: {2} MWh/a ({3} %) · {4}";

    /// <summary>Eine Knotenzeile der Textalternative: Name, Zufluss, Abfluss, Rest (<c>BK_ERG_FLUSS_KNOTEN</c>).</summary>
    public string Knoten { get; init; } = "{0}: Zufluss {1} · Abfluss {2} · Rest {3} MWh/a";

    /// <summary>Die Überschrift der Bandliste der Textalternative (<c>BK_ERG_FLUSS_TEXT_FLUESSE</c>).</summary>
    public string TextFluesse { get; init; } = "Energieflüsse";

    /// <summary>Die Überschrift der Knotenbilanz der Textalternative (<c>BK_ERG_FLUSS_TEXT_KNOTEN</c>).</summary>
    public string TextKnoten { get; init; } = "Bilanz je Knoten";

    /// <summary>Die Sprachausgabe der Wahl „Kette | Stufen“ (<c>BK_ERG_FLUSS_DARSTELLUNG</c>).</summary>
    public string Darstellung { get; init; } = "Darstellung des Energieflussbilds";

    /// <summary>Die Sprachausgabe der Filterknöpfe (<c>BK_ERG_FLUSS_FILTER</c>).</summary>
    public string Filter { get; init; } = "Energieform";

    /// <summary>Die Beschriftung je Stellung der Darstellung (Schlüssel <see cref="ErgebnisFlussWahl.KETTE"/>, <see cref="ErgebnisFlussWahl.STUFEN"/>; <c>EF_DARSTELLUNG_*</c>).</summary>
    public IReadOnlyDictionary<string, string> Darstellungen { get; init; } = new Dictionary<string, string>
    {
        [ErgebnisFlussWahl.KETTE] = "Kette", [ErgebnisFlussWahl.STUFEN] = "Stufen"
    };

    /// <summary>Die Beschriftung je Filterknopf (Schlüssel aus <see cref="ErgebnisFlussWahl.FILTER"/>; <c>EF_FILTER_ALLE</c>, <c>EF_LEG_*</c>).</summary>
    public IReadOnlyDictionary<string, string> Filterknoepfe { get; init; } = new Dictionary<string, string>
    {
        [ErgebnisFlussWahl.ALLE] = "Alle", [ErgebnisFlussWahl.STROM] = "Strom",
        [ErgebnisFlussWahl.WAERME] = "Wärme", [ErgebnisFlussWahl.KAELTE] = "Kälte"
    };

    /// <summary>Die leise Zeile zur Bedienung des Zeigers (<c>BK_ERG_FLUSS_ZEIGER</c>).</summary>
    public string Zeiger { get; init; } =
        "Die Werte erscheinen beim Überfahren oder Antippen eines Bandes oder Knotens, sein Umfeld wird hervorgehoben; die Zahlen stehen im Reiter „Tabelle“.";
}
