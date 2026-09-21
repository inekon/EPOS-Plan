using KiKern;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Das FLACHE Abbild des Zeileneditors der gesetzlichen Parameter für den
/// Hilfe-Assistenten (Welle KI‑F4).
///
/// <para><b>Warum eine EIGENE Maske und kein Teil des Katalogs.</b> Der Editor ist
/// zwar nur als Überlagerung im Gesetzeskatalog zu sehen — er pflegt aber einen
/// ganz anderen Gegenstand: EINE Jahreszeile mit Schlüssel, Jahr, Wert, Einheit,
/// Status und Quelle. Ein gemeinsamer Katalogeintrag hätte dem Modell verschwiegen,
/// ob es gerade über die Liste oder über eine Zeile spricht.</para>
///
/// <para><b>Warum ein Sichtmodell.</b> Der Editor hält seinen Stand in sieben
/// privaten Feldern; <c>Zeilenwerte</c> entsteht erst beim OK und ist ein
/// unveränderlicher Record.</para>
///
/// <para><b>Drei WAHLFELDER</b> (KI‑D‑Q6): Klasse, Einheit und Status. Beim ÄNDERN
/// sind Schlüssel und Klasse gesperrt — eine Zeile wechselt ihre Identität nicht.</para>
/// </summary>
public sealed class GesetzeskatalogZeileKiSicht
{
    public Func<string>? SchluesselLesen { get; init; }
    public Action<string>? SchluesselSetzen { get; init; }

    public Func<string>? KlasseLesen { get; init; }
    public Action<string>? KlasseSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? KlassenEintraege { get; init; }

    public Func<int?>? JahrLesen { get; init; }
    public Action<int?>? JahrSetzen { get; init; }

    public Func<double?>? WertLesen { get; init; }
    public Action<double?>? WertSetzen { get; init; }

    public Func<string>? EinheitLesen { get; init; }
    public Action<string>? EinheitSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? EinheitEintraege { get; init; }

    public Func<string>? StatusLesen { get; init; }
    public Action<string>? StatusSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? StatusEintraege { get; init; }

    public Func<string>? QuelleLesen { get; init; }
    public Action<string>? QuelleSetzen { get; init; }

    /// <summary>Die Klassen des Katalogs.</summary>
    public IReadOnlyList<KiWahleintrag> KlasseWahl
        => KlassenEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Einheiten, die der Katalog kennt.</summary>
    public IReadOnlyList<KiWahleintrag> EinheitWahl
        => EinheitEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Statuswerte (gesetzt, Prognose, entfallen).</summary>
    public IReadOnlyList<KiWahleintrag> StatusWahl
        => StatusEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Der Schlüssel der Zeile; beim Ändern gesperrt.</summary>
    public string Schluessel
    {
        get => SchluesselLesen?.Invoke() ?? "";
        set => SchluesselSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Klasse, zu der die Zeile gehört; beim Ändern gesperrt.</summary>
    public string Klasse
    {
        get => KlasseLesen?.Invoke() ?? "";
        set => KlasseSetzen?.Invoke(value ?? "");
    }

    /// <summary>Das Jahr, ab dem der Wert gilt.</summary>
    public int? Jahr
    {
        get => JahrLesen?.Invoke();
        set => JahrSetzen?.Invoke(value);
    }

    /// <summary>Der Wert selbst; leer heißt „der Satz ist entfallen".</summary>
    public double? Wert
    {
        get => WertLesen?.Invoke();
        set => WertSetzen?.Invoke(value);
    }

    /// <summary>Die Einheit des Wertes.</summary>
    public string Einheit
    {
        get => EinheitLesen?.Invoke() ?? "";
        set => EinheitSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Status der Zeile.</summary>
    public string Status
    {
        get => StatusLesen?.Invoke() ?? "";
        set => StatusSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Fundstelle, aus der der Wert stammt.</summary>
    public string Quelle
    {
        get => QuelleLesen?.Invoke() ?? "";
        set => QuelleSetzen?.Invoke(value ?? "");
    }
}
