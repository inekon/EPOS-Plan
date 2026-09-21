using EPOS.UI.Dienste;
using KiKern;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild der Maske „Kostenfaktoren-Katalog" für den Hilfe-Assistenten
/// (Welle KI‑F4).
///
/// <para><b>Warum ein Sichtmodell.</b> Der Dialog hält seinen Stand in zwei privaten
/// Feldern: dem Namen der neuen Zeile und der Markierung im Raster. Ein Daten-Objekt
/// gibt es nicht; <c>KostenfaktorZeile</c> ist ein unveränderlicher Record der
/// Liste.</para>
///
/// <para><b>Der markierte Faktor ist ein WAHLFELD</b> (KI‑D‑Q6) und trägt als
/// Schlüssel seine Stamm-Id. Ihn zu setzen markiert ihn im Raster — derselbe Weg wie
/// ein Klick auf den Wahlknopf der Zeile; erst danach greift „Löschen".</para>
///
/// <para><b>Das Raster selbst bleibt draußen</b>: Es zeigt je Faktor nur seine
/// Bezeichnung, und die ist der Anzeigetext der Wahl.</para>
/// </summary>
public sealed class KostenfaktorKatalogKiSicht
{
    public Func<string>? NeuerNameLesen { get; init; }
    public Action<string>? NeuerNameSetzen { get; init; }

    public Func<int?>? MarkierungLesen { get; init; }
    public Action<int?>? MarkierungSetzen { get; init; }

    /// <summary>Liefert die Faktoren, die das Raster führt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? FaktorEintraege { get; init; }

    /// <summary>Die Kostenfaktoren des Katalogs (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> KostenfaktorWahl
        => FaktorEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Der Bezeichner, unter dem „Neu" einen Faktor anlegt.</summary>
    public string NeuerName
    {
        get => NeuerNameLesen?.Invoke() ?? "";
        set => NeuerNameSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der im Raster markierte Kostenfaktor.</summary>
    public int? Kostenfaktor
    {
        get => MarkierungLesen?.Invoke();
        set => MarkierungSetzen?.Invoke(value);
    }
}
