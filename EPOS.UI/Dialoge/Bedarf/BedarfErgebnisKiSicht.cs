using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Bedarfs-Ergebnisanzeige für den Hilfe-Assistenten
/// (Welle KI‑F3).
///
/// <para><b>Eine Maske, die nur ZEIGT.</b> Ihre Kennzahlen, die Monatstabelle und die
/// Bilder kommen als gerechnetes Ergebnis herein; geschrieben wird nichts. Einstellbar
/// sind die vier Schalter, mit denen der Anwender die Anzeige wechselt: die
/// Anzeigeeinheit, die Sicht der Tabelle, die Sicht der Grafik und der Jahresverlauf.
/// Sie alle liegen in den lebenden Feldern der Maske — deshalb eine Sichtklasse.</para>
///
/// <para><b>Die Kennzahlen selbst stehen NICHT im Katalog</b>: Sie sind eine Liste von
/// Zeilen mit eigenen Bezeichnern und wechseln mit der Ausprägung; ein Katalogfeld
/// trägt EINEN Wert. Der Assistent liest sie über den Bedarfsreiter der Ergebnisseite,
/// wo sie als Feldsatz stehen.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class BedarfErgebnisKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? EinheitLesen { get; init; }
    public Action<int?>? EinheitSetzen { get; init; }

    public Func<int?>? TabellensichtLesen { get; init; }
    public Action<int?>? TabellensichtSetzen { get; init; }

    public Func<int?>? GrafiksichtLesen { get; init; }
    public Action<int?>? GrafiksichtSetzen { get; init; }

    public Func<bool>? JahresverlaufLesen { get; init; }
    public Action<bool>? JahresverlaufSetzen { get; init; }

    /// <summary>Liefert die Energieeinheiten, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? EinheitEintraege { get; init; }

    /// <summary>Liefert die Sichten, zwischen denen die Maske umschaltet.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SichtEintraege { get; init; }

    /// <summary>Die Energieeinheiten der Klappliste (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> EinheitWahl
        => EinheitEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Sichten der Tabelle (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> TabellensichtWahl
        => SichtEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Sichten der Grafik — dieselbe Liste, eigene Wahl (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> GrafiksichtWahl
        => SichtEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Die Anzeigeeinheit der Energiemengen als Platz in der Klappliste; sie wirkt auf
    /// alle drei Reiterblätter zugleich.
    /// </summary>
    public int? Einheit
    {
        get => EinheitLesen?.Invoke();
        set => EinheitSetzen?.Invoke(value);
    }

    /// <summary>Welche Sicht die TABELLE zeigt; ihr Schlüssel ist der Listenplatz.</summary>
    public int? Tabellensicht
    {
        get => TabellensichtLesen?.Invoke();
        set => TabellensichtSetzen?.Invoke(value);
    }

    /// <summary>
    /// Welche Sicht die GRAFIK zeigt — sie wechselt unabhängig von der Tabelle.
    /// </summary>
    public int? Grafiksicht
    {
        get => GrafiksichtLesen?.Invoke();
        set => GrafiksichtSetzen?.Invoke(value);
    }

    /// <summary>
    /// Zeigt die Grafik den Jahresverlauf? Der Schalter gibt es nur zur
    /// Brauchwassersicht; ein Sichtwechsel weg davon nimmt ihn mit.
    /// </summary>
    public bool Jahresverlauf
    {
        get => JahresverlaufLesen?.Invoke() ?? false;
        set => JahresverlaufSetzen?.Invoke(value);
    }
}
