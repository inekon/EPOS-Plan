using KiKern;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild der Maske „Energieträger-Variante anlegen" für den
/// Hilfe-Assistenten (Welle KI‑F4).
///
/// <para><b>Warum ein Sichtmodell und kein Daten-Objekt.</b> Der Dialog führt seinen
/// Stand in zwei privaten Feldern (<c>_auswahl</c>, <c>_variantenName</c>); das
/// Ergebnis <c>EnergietraegerVarianteErgebnis</c> entsteht erst beim OK und ist
/// unveränderlich. Ein daran angemeldeter Katalog läse den Stand von vorhin und
/// setzte ins Leere — dieselbe Lage wie bei der Wohnflächenangabe (Welle KI‑F3).</para>
///
/// <para><b>Der Energieträger ist ein WAHLFELD</b> (KI‑D‑Q6) und trägt als Schlüssel
/// seine Katalog-Id. Ihn zu setzen belegt den Variantennamen vor — derselbe Weg wie
/// ein Griff in die Klappliste.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
/// </summary>
public sealed class EnergietraegerVarianteKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? TraegerLesen { get; init; }
    public Action<int?>? TraegerSetzen { get; init; }

    /// <summary>Liefert die Energieträger, die die Klappliste führt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? TraegerEintraege { get; init; }

    public Func<string>? NameLesen { get; init; }
    public Action<string>? NameSetzen { get; init; }

    /// <summary>Die Energieträger der Klappliste (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> EnergietraegerWahl
        => TraegerEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Der gewählte Energieträger. Ihn zu setzen belegt den Variantennamen vor.
    /// </summary>
    public int? Energietraeger
    {
        get => TraegerLesen?.Invoke();
        set => TraegerSetzen?.Invoke(value);
    }

    /// <summary>Die Bezeichnung der neuen Variante.</summary>
    public string Variantenname
    {
        get => NameLesen?.Invoke() ?? "";
        set => NameSetzen?.Invoke(value ?? "");
    }
}
