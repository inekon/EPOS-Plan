namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Eine Zeile der Liste „Simulation…" mehrerer Gebäude im Projekt (GD-6a, Anwenderwunsch 12.10.2026): Name,
/// Heizwärme und Höchstlast aus dem Parametersatz des Wärmebedarfs (<c>BedarfGaben</c>, Schlüssel <c>Daten</c>),
/// sonst der benannte Grund ohne Zahl. Der Parametersatz bleibt für den Sprung in den Einzelbedarf stehen.
/// </summary>
public sealed class GebaeudeSimulationseintrag
{
    /// <summary>Der Name der Projektzeile.</summary>
    public string Name { get; init; } = "";

    /// <summary>Heizwärme in MWh/a; <c>null</c> ohne Zahl.</summary>
    public double? HeizwaermeMwh { get; init; }

    /// <summary>Höchstlast in kW; <c>null</c> ohne Zahl.</summary>
    public double? MaxLastKw { get; init; }

    /// <summary>Der Grund ohne Zahl; leer, wenn gerechnet wurde.</summary>
    public string Befund { get; init; } = "";

    /// <summary>Der Parametersatz des Einzelbedarfs (<c>GebaeudeBedarfDialog</c>); <c>null</c> ohne Zahl.</summary>
    public IReadOnlyDictionary<string, object>? Gaben { get; init; }

    /// <summary>Gerechnet — es gibt einen Parametersatz.</summary>
    public bool Gerechnet => Gaben is not null;

    /// <summary>Baut den Eintrag aus dem Parametersatz einer Zeile oder ihrem Grund.</summary>
    public static GebaeudeSimulationseintrag Aus(string name, IReadOnlyDictionary<string, object>? gaben, string grund)
    {
        GebaeudeBedarfDaten? daten = gaben is not null && gaben.TryGetValue("Daten", out object? d)
            ? d as GebaeudeBedarfDaten : null;
        return new GebaeudeSimulationseintrag
        {
            Name = name ?? "",
            Gaben = gaben,
            HeizwaermeMwh = daten?.HeizwaermeMwh,
            MaxLastKw = daten?.MaxLastKw,
            Befund = gaben is null ? grund ?? "" : ""
        };
    }
}
