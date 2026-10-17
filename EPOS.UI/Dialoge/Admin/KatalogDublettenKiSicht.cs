using KiKern;

namespace EPOS.UI.Dialoge.Admin;

/// <summary>
/// Das FLACHE Abbild des <see cref="KatalogDublettenDialog"/> für den Hilfe-Assistenten
/// (Freigabe der Masken, Teil C): die Wahl des Katalogs, der geprüft wird.
/// </summary>
/// <remarks>
/// Die Wahl ist ein privates Feld des Dialogs; die Sicht hält keinen Zustand und setzt über den
/// Delegaten — derselbe Weg wie die Klappliste (die Meldung fällt). „Prüfen" (Suchlauf),
/// „Bereinigen", „Löschen", „Umbenennen" und „Protokoll speichern" schreiben sofort oder laufen
/// lange und bleiben Handlungen des Anwenders; Baum, Details und Protokoll sind Anzeigen.
/// </remarks>
public sealed class KatalogDublettenKiSicht
{
    public Func<int?>? KatalogLesen { get; init; }
    public Action<int?>? KatalogSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? KatalogEintraege { get; init; }

    /// <summary>Die Wahl des Feldes <c>Katalog</c> — „(alle Kataloge)" und je Katalog ein Eintrag.</summary>
    public IReadOnlyList<KiWahleintrag> KatalogWahl
        => KatalogEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Der Katalog, den „Prüfen" nach Dubletten durchsucht; 0 = alle Kataloge.</summary>
    public int? Katalog
    {
        get => KatalogLesen?.Invoke();
        set => (KatalogSetzen ?? throw new InvalidOperationException(nameof(Katalog)))(value);
    }
}
