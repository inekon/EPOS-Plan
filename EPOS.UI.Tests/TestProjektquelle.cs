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

    // =====================================================================
    //  Die fuenf Masken, die die Wurzel seit KI-D-Q8 selbst zeigt
    // =====================================================================
    //
    //  Vorgabe ist ueberall null - der Zustand einer Huelle, die die Maske nicht
    //  fuehrt. Die Wurzel bleibt dann stehen und nennt den Grund.

    /// <summary>Der Parametersatz der Klimadaten.</summary>
    internal IReadOnlyDictionary<string, object>? Klimadaten { get; set; }

    public IReadOnlyDictionary<string, object>? KlimadatenGaben() => Klimadaten;

    /// <summary>Der Parametersatz von „Als Variante speichern".</summary>
    internal IReadOnlyDictionary<string, object>? Projektvariante { get; set; }

    public IReadOnlyDictionary<string, object>? ProjektVarianteGaben(int idProjekt)
        => Projektvariante;

    /// <summary>Die zuletzt uebergebene Antwort des Variantendialogs.</summary>
    internal EPOS.UI.Dialoge.Projekt.ProjektVarianteWahl? VarianteUebernommen { get; private set; }

    /// <summary>Was <see cref="ProjektVarianteUebernehmen"/> zurueckgeben soll.</summary>
    internal string VariantenAntwort { get; set; } = "";

    public string ProjektVarianteUebernehmen(
        int idProjekt, EPOS.UI.Dialoge.Projekt.ProjektVarianteWahl wahl)
    {
        VarianteUebernommen = wahl;
        return VariantenAntwort;
    }

    /// <summary>Der Parametersatz von „Projekt Speichern unter".</summary>
    internal IReadOnlyDictionary<string, object>? Projektkopie { get; set; }

    public IReadOnlyDictionary<string, object>? ProjektKopieGaben() => Projektkopie;

    /// <summary>Der Parametersatz der Lastspitzenkappung.</summary>
    internal IReadOnlyDictionary<string, object>? PeakShaving { get; set; }

    public IReadOnlyDictionary<string, object>? PeakShavingGaben(int idProjekt) => PeakShaving;

    /// <summary>Der Parametersatz der Stromganglinien-Verwaltung.</summary>
    internal IReadOnlyDictionary<string, object>? Stromganglinien { get; set; }

    public IReadOnlyDictionary<string, object>? StromganglinieAdminGaben() => Stromganglinien;

    // ---- Die drei Masken der Wirtschaftlichkeit (E3/8, A19) ----------------

    /// <summary>Der Parametersatz der Kostenverwaltung.</summary>
    internal IReadOnlyDictionary<string, object>? Kostenverwaltung { get; set; }

    public IReadOnlyDictionary<string, object>? KostenverwaltungGaben() => Kostenverwaltung;

    /// <summary>Der Parametersatz der Nutzungsdauern (AfA).</summary>
    internal IReadOnlyDictionary<string, object>? Nutzungsdauern { get; set; }

    public IReadOnlyDictionary<string, object>? NutzungsdauerGaben() => Nutzungsdauern;

    /// <summary>Der Parametersatz des Gesetzeskatalogs.</summary>
    internal IReadOnlyDictionary<string, object>? Gesetzeskatalog { get; set; }

    public IReadOnlyDictionary<string, object>? GesetzeskatalogGaben() => Gesetzeskatalog;

    // ---- Die zwei Kataloge der Gebäudesimulation (G3) -----------------------

    /// <summary>Der Parametersatz der Verwaltung „Baustoffe".</summary>
    internal IReadOnlyDictionary<string, object>? BaustoffKatalog { get; set; }

    public IReadOnlyDictionary<string, object>? BaustoffKatalogGaben() => BaustoffKatalog;

    /// <summary>Der Parametersatz der Verwaltung „Bauteilaufbauten".</summary>
    internal IReadOnlyDictionary<string, object>? BauteilaufbauKatalog { get; set; }

    public IReadOnlyDictionary<string, object>? BauteilaufbauKatalogGaben() => BauteilaufbauKatalog;

    /// <summary>
    /// Das Lagebild der Lizenz (Welle iF30) — <c>null</c> = kein Banner, und das ist
    /// die Vorgabe: Kein bestehender Fall soll durch die Erweiterung ein Banner bekommen.
    /// </summary>
    internal WindowsFormsApplication1.LizenzLage? Lizenz { get; set; }

    public WindowsFormsApplication1.LizenzLage? Lizenzlage() => Lizenz;

    /// <summary>
    /// Der Parametersatz des KI-Hilfe-Assistenten (Auftrag #199) — <c>null</c> ist
    /// die Vorgabe und heisst „steht auf diesem Geraet noch nicht zur Verfuegung",
    /// genau der Zustand der iOS-Huelle vor iU11.
    /// </summary>
    internal IReadOnlyDictionary<string, object>? KiAssistent { get; set; }

    public IReadOnlyDictionary<string, object>? KiAssistentGaben(int idProjekt) => KiAssistent;

    /// <summary>
    /// Der Parametersatz der Ansicht „Simulation" (Auftrag #207) — <c>null</c> ist
    /// die Vorgabe und heisst „die Seite geht nicht auf", genau der Zustand der
    /// iOS-Huelle vor Stufe S2.
    /// </summary>
    internal IReadOnlyDictionary<string, object>? Simulation { get; set; }

    public IReadOnlyDictionary<string, object>? SimulationGaben(int idProjekt) => Simulation;

    /// <summary>
    /// Der Parametersatz des PROJEKTASSISTENTEN (Befund W16a-O-4) — <c>null</c> ist
    /// die Vorgabe und heisst „steht auf diesem Geraet noch nicht zur Verfuegung",
    /// genau der Zustand der iOS-Huelle VOR diesem Befund.
    /// </summary>
    internal Func<int, int, IReadOnlyDictionary<string, object>?>? Assistent { get; set; }

    /// <summary>Was zuletzt nach <see cref="AssistentGaben"/> hereinkam.</summary>
    internal (int Betriebsart, int IdProjekt)? AssistentRuf { get; private set; }

    public IReadOnlyDictionary<string, object>? AssistentGaben(int betriebsart, int idProjekt)
    {
        AssistentRuf = (betriebsart, idProjekt);
        return Assistent?.Invoke(betriebsart, idProjekt);
    }

    /// <summary>Der Parametersatz der Ansicht „Stromspeicher-Auslegung" (#192).</summary>
    internal IReadOnlyDictionary<string, object>? Auslegung { get; set; }

    public IReadOnlyDictionary<string, object>? StromspeicherAuslegungGaben(int idProjekt)
        => Auslegung;
}
