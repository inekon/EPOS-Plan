using KiKern;

namespace EPOS.UI.Dialoge.Solarthermie;

/// <summary>
/// Das FLACHE Abbild der Maske „Solarganglinien im Projekt" für den Hilfe-Assistenten
/// (Welle KI‑F3).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>ErzeugerZeile</c>.</b> Diese Maske führt
/// keinen Einstellwert der Anlage: Sie ordnet Ganglinien zu und zeigt zu der
/// markierten Zeile Name und Beschreibung — beide in privaten Feldern der Komponente,
/// die je nach Markierung aus der Projektliste ODER aus dem Katalog kommen.</para>
///
/// <para><b>Die Katalogwahl ist das Wahlfeld dieser Maske</b> (KI‑D‑Q6): Sie zu setzen
/// markiert die Ganglinie, die „In das Projekt übernehmen" aufnimmt — derselbe Weg wie
/// ein Klick in die Liste.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class SolarganglinieKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? KatalogLesen { get; init; }
    public Action<string>? KatalogSetzen { get; init; }

    public Func<string>? ProjektganglinieLesen { get; init; }
    public Func<string>? BeschreibungLesen { get; init; }

    /// <summary>Liefert die Ganglinien, die die Katalogliste führt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? KatalogEintraege { get; init; }

    /// <summary>Die Solarganglinien des Katalogs (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> KatalogganglinieWahl
        => KatalogEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Die im KATALOG markierte Ganglinie. Sie zu setzen markiert sie in der Liste;
    /// Name und Beschreibung ziehen nach.
    /// </summary>
    public string Katalogganglinie
    {
        get => KatalogLesen?.Invoke() ?? "";
        set => KatalogSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die im PROJEKT markierte Ganglinie; leer, wenn der Katalog markiert ist.</summary>
    public string Projektganglinie => ProjektganglinieLesen?.Invoke() ?? "";

    /// <summary>Die Beschreibung der markierten Ganglinie.</summary>
    public string Beschreibung => BeschreibungLesen?.Invoke() ?? "";
}
