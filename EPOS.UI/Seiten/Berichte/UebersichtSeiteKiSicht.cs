using KiKern;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// Das FLACHE Abbild des Reiterblatts „Übersicht" für den Hilfe-Assistenten
/// (Welle KI‑F6).
///
/// <para><b>Diese Seite führt VIER Einstellwerte</b>: das Stammprojekt der
/// Vergleichsgruppe, den Filter „nur Stammprojekte", die markierte Variante und
/// den Bezeichner, mit dem sie umbenannt wird. Alles andere darauf ist
/// gerechnete ANZEIGE — die Gegenüberstellung, die Unterschiedstabelle, der
/// Simulations- und der Speicherstand.</para>
///
/// <para><b>Warum eine Sichtklasse und nicht <c>UebersichtStand</c>.</b> Die
/// Seite SCHREIBT nicht in ihren Stand: Sie meldet jede Wahl über einen Rückruf
/// an die Hülle und lädt danach neu (<c>StammGewechselt</c>,
/// <c>FilterGewechselt</c>, <c>ZeileMarkiert</c>). Ein unmittelbar angemeldeter
/// Stand böte an, <c>StammId</c> zu setzen, ohne dass die Gruppe nachzöge — die
/// Seite zeigte dann die alte Tabelle unter einem neuen Kopf.</para>
///
/// <para><b>Die VERGLEICHSWAHL bleibt draußen.</b> Sie ist eine Menge von
/// Verweisen (die Ids der angehakten Varianten) und kein Feldwert — dieselbe
/// Regel und derselbe Grund wie beim Reiterblatt „Kosten" (Welle KI‑F4).</para>
/// </summary>
public sealed class UebersichtSeiteKiSicht
{
    /// <summary>Liest das gewählte Stammprojekt.</summary>
    public Func<int?>? StammLesen { get; init; }

    /// <summary>Wechselt das Stammprojekt — derselbe Weg wie die Klappliste.</summary>
    public Action<int?>? StammSetzen { get; init; }

    /// <summary>Die Stammprojekte, wie die Klappliste sie führt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? StammEintraege { get; init; }

    /// <summary>Liest den Filter „nur Stammprojekte".</summary>
    public Func<bool>? FilterLesen { get; init; }

    /// <summary>Setzt den Filter und lädt die Liste neu.</summary>
    public Action<bool>? FilterSetzen { get; init; }

    /// <summary>Liest die markierte Version der Gruppe.</summary>
    public Func<int?>? VarianteLesen { get; init; }

    /// <summary>Markiert eine Version — derselbe Weg wie die Klappliste.</summary>
    public Action<int?>? VarianteSetzen { get; init; }

    /// <summary>Die Versionen der Vergleichsgruppe.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? VarianteEintraege { get; init; }

    /// <summary>Liest den Bezeichner des Umbenennfeldes.</summary>
    public Func<string>? BezeichnerLesen { get; init; }

    /// <summary>Schreibt den Bezeichner des Umbenennfeldes.</summary>
    public Action<string>? BezeichnerSetzen { get; init; }

    /// <summary>Liest die Simulationszeile der markierten Version.</summary>
    public Func<string>? SimulationLesen { get; init; }

    /// <summary>Liest die Fußzeile der Seite.</summary>
    public Func<string>? StatusLesen { get; init; }

    /// <summary>Die wählbaren Stammprojekte.</summary>
    public IReadOnlyList<KiWahleintrag> StammprojektWahl
        => StammEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Das Stammprojekt, dessen Vergleichsgruppe die Seite zeigt.</summary>
    public int? Stammprojekt
    {
        get => StammLesen?.Invoke();
        set => StammSetzen?.Invoke(value);
    }

    /// <summary>Zeigt die Liste nur Stammprojekte, oder auch deren Varianten?</summary>
    public bool NurStaemme
    {
        get => FilterLesen?.Invoke() ?? false;
        set => FilterSetzen?.Invoke(value);
    }

    /// <summary>Die Versionen der Gruppe.</summary>
    public IReadOnlyList<KiWahleintrag> VarianteWahl
        => VarianteEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>
    /// Die markierte Version — sie entscheidet, welche Unterschiede die Tabelle
    /// zeigt und was Löschen, Umbenennen und Simulieren treffen.
    /// </summary>
    public int? Variante
    {
        get => VarianteLesen?.Invoke();
        set => VarianteSetzen?.Invoke(value);
    }

    /// <summary>
    /// Der Bezeichner des Umbenennfeldes — er wirkt erst mit dem Knopf
    /// „Umbenennen".
    /// </summary>
    public string Bezeichner
    {
        get => BezeichnerLesen?.Invoke() ?? "";
        set => BezeichnerSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Simulationsstand der markierten Version — Anzeige.</summary>
    public string Simulationsstand => SimulationLesen?.Invoke() ?? "";

    /// <summary>Die Fußzeile mit den Befunden der Seite — Anzeige.</summary>
    public string Statuszeile => StatusLesen?.Invoke() ?? "";
}
