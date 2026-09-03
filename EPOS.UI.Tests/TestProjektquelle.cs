using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;

namespace EPOS.UI.Tests;

/// <summary>
/// Projektquelle fuer die Tests: liefert vorgegebene Listen und merkt sich, was
/// die Wurzel von ihr wollte. Gegenstueck zu <see cref="TestHilfe"/>.
/// </summary>
internal sealed class TestProjektquelle : IProjektQuelle
{
    private readonly IReadOnlyList<ProjektZeile> _projekte;
    private readonly IReadOnlyList<(int Id, string Name)> _traeger;
    private readonly BhkwDialogDaten? _bhkw;

    internal TestProjektquelle(IReadOnlyList<ProjektZeile>? projekte = null,
                               IReadOnlyList<(int Id, string Name)>? traeger = null,
                               BhkwDialogDaten? bhkw = null)
    {
        _projekte = projekte ?? Array.Empty<ProjektZeile>();
        _traeger = traeger ?? new[] { (3, "Erdgas"), (7, "Fernwaerme") };
        _bhkw = bhkw;
    }

    /// <summary>Zaehlt die Aufrufe von <see cref="Projekte"/> - so wird das Nachladen sichtbar.</summary>
    internal int Geladen { get; private set; }

    /// <summary>Das zuletzt uebergebene Dialogergebnis; <c>null</c> = noch keines.</summary>
    internal EnergietraegerVarianteErgebnis? Uebernommen { get; private set; }

    /// <summary>Was <see cref="EnergietraegerUebernehmen"/> zurueckgeben soll.</summary>
    internal string Antwort { get; set; } = "";

    public IReadOnlyList<ProjektZeile> Projekte()
    {
        Geladen++;
        return _projekte;
    }

    public IReadOnlyList<(int Id, string Name)> Energietraeger() => _traeger;

    public string EnergietraegerUebernehmen(int idProjekt, EnergietraegerVarianteErgebnis ergebnis)
    {
        Uebernommen = ergebnis;
        return Antwort;
    }

    public BhkwDialogDaten? BhkwDaten(int idProjekt) => _bhkw;
}
