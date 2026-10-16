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

    /// <summary>Die leise Zeile zur Bedienung des Zeigers (<c>BK_ERG_FLUSS_ZEIGER</c>).</summary>
    public string Zeiger { get; init; } =
        "Die Werte erscheinen beim Überfahren oder Antippen eines Bandes oder Knotens; die Zahlen stehen im Reiter „Tabelle“.";
}
