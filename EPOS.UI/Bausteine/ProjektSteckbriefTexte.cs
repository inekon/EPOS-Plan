namespace EPOS.UI.Bausteine;

/// <summary>
/// Die Beschriftungen des <see cref="ProjektSteckbriefBlock"/> — ein Bündel statt zehn
/// einzelner Parameter (Hausregel EPOS.UI). Jede Eigenschaft nennt ihren
/// Ressourcenschlüssel; der Vorgabewert ist der deutsche Rückfall.
/// </summary>
public sealed class ProjektSteckbriefTexte
{
    /// <summary><c>WIZ_STECKBRIEF_TITEL</c> — Name des Blocks für die Sprachausgabe.</summary>
    public string Titel { get; init; } = "Projektdaten";

    /// <summary><c>WIZ_STECKBRIEF_BESCHREIBUNG</c>.</summary>
    public string Beschreibung { get; init; } = "Beschreibung";

    /// <summary><c>WIZ_STECKBRIEF_KUNDE</c>.</summary>
    public string Kunde { get; init; } = "Kunde";

    /// <summary><c>WIZ_STECKBRIEF_BEARBEITER</c>.</summary>
    public string Bearbeiter { get; init; } = "Bearbeiter";

    /// <summary><c>WIZ_STECKBRIEF_ERSTELLT</c>.</summary>
    public string Erstellt { get; init; } = "Erstellt am";

    /// <summary><c>WIZ_STECKBRIEF_GEAENDERT</c>.</summary>
    public string Geaendert { get; init; } = "Geändert am";

    /// <summary><c>WIZ_STECKBRIEF_KLIMA</c>.</summary>
    public string Klima { get; init; } = "Klimaregion";

    /// <summary><c>WIZ_STECKBRIEF_VARIANTE_VON</c>.</summary>
    public string VarianteVon { get; init; } = "Variante von";

    /// <summary><c>WIZ_STECKBRIEF_VARIANTEN</c> — Zahl der abgeleiteten Varianten.</summary>
    public string Varianten { get; init; } = "Varianten";

    /// <summary><c>WIZ_STECKBRIEF_SIMULATION</c> — jüngster Simulationslauf.</summary>
    public string Simulation { get; init; } = "Letzte Simulation";
}
