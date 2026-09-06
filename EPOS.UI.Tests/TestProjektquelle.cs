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

    /// <summary>
    /// Der Parametersatz der Startseite (iU9-W16c.2, K7) - <c>null</c>, solange
    /// keiner gesetzt ist; genau der Zustand der iOS-Huelle vor iU11.
    /// </summary>
    internal IReadOnlyDictionary<string, object>? Startseite { get; set; }

    /// <summary>Der Parametersatz von „Berichte &amp; Kosten" (iU9-W16c.2, K7).</summary>
    internal IReadOnlyDictionary<string, object>? BerichteKosten { get; set; }

    public IReadOnlyDictionary<string, object>? StartseiteGaben(int idProjekt) => Startseite;

    public IReadOnlyDictionary<string, object>? BerichteKostenGaben(int idProjekt) => BerichteKosten;

    /// <summary>
    /// Das Lagebild der Lizenz (Welle iF30) — <c>null</c> = kein Banner, und das ist
    /// die Vorgabe: Kein bestehender Fall soll durch die Erweiterung ein Banner bekommen.
    /// </summary>
    internal WindowsFormsApplication1.LizenzLage? Lizenz { get; set; }

    public WindowsFormsApplication1.LizenzLage? Lizenzlage() => Lizenz;
}
