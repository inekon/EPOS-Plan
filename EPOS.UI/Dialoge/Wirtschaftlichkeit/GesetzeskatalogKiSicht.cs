using KiKern;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Das FLACHE Abbild der Maske „Gesetzliche Parameter" für den Hilfe-Assistenten
/// (Welle KI‑F4).
///
/// <para><b>Was die Maske führt.</b> Sie wählt eine KLASSE (CO₂-Preispfad,
/// KWKG-Sätze, Energie- und Stromsteuer …) und zeigt deren Jahreszeilen in einer
/// Katalogliste. Ihre zwei Einstellwerte sind die Klasse und die markierte Zeile;
/// gepflegt wird eine Zeile im Zeileneditor, der einen eigenen Katalogschlüssel
/// trägt.</para>
///
/// <para><b>Die Katalogliste selbst bleibt draußen</b>: Sie ist der Baustein
/// <c>Katalogliste</c> mit eigenem Filter, eigener Suche und eigener Sortierung —
/// sechs Spalten Anzeige je Zeile, keine davon hier eingebbar.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
/// </summary>
public sealed class GesetzeskatalogKiSicht
{
    public Func<string>? KlasseLesen { get; init; }
    public Action<string>? KlasseSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? KlassenEintraege { get; init; }

    public Func<string>? ZeileLesen { get; init; }
    public Action<string>? ZeileSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? ZeilenEintraege { get; init; }

    /// <summary>Die Klassen des Katalogs — Schlüssel ist ihr Steuerwert.</summary>
    public IReadOnlyList<KiWahleintrag> KlasseWahl
        => KlassenEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Zeilen der gewählten Klasse — Schlüssel ist ihr Bezeichner.</summary>
    public IReadOnlyList<KiWahleintrag> ZeileWahl
        => ZeilenEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Klasse, deren Jahreszeilen die Liste zeigt.</summary>
    public string Klasse
    {
        get => KlasseLesen?.Invoke() ?? "";
        set => KlasseSetzen?.Invoke(value ?? "");
    }

    /// <summary>
    /// Die in der Liste markierte Zeile. Sie entscheidet, worauf „Ändern" und
    /// „Löschen" greifen.
    /// </summary>
    public string Zeile
    {
        get => ZeileLesen?.Invoke() ?? "";
        set => ZeileSetzen?.Invoke(value ?? "");
    }
}
