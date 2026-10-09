using KiKern;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Das FLACHE Abbild der Maske „Photovoltaik Ganglinie" für den Hilfe-Assistenten (PVG) — dieselbe Bauart
/// wie <c>SolarganglinieKiSicht</c>.
///
/// <para><b>Die Katalogwahl ist das Wahlfeld dieser Maske</b>: Sie zu setzen markiert die Ganglinie, die
/// „In das Projekt übernehmen" aufnimmt — derselbe Weg wie ein Klick in die Liste. Alles Übrige liest der
/// Assistent nur: Quelle (Beschreibung), Raster, Jahresarbeit und Nennleistung der markierten Ganglinie,
/// ob sie dem Projekt zugeordnet ist, worin ihre Projektkopie vom Katalogsatz abweicht, und wie der Import
/// steht. „Aus dem Katalog erneuern…" bleibt eine Handlung des Anwenders.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren Delegaten.</para>
/// </summary>
public sealed class PvGanglinieKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? KatalogLesen { get; init; }
    public Action<string>? KatalogSetzen { get; init; }

    public Func<string>? ProjektganglinieLesen { get; init; }
    public Func<string>? QuelleLesen { get; init; }
    public Func<string>? AufloesungLesen { get; init; }
    public Func<string>? JahressummeLesen { get; init; }
    public Func<string>? NennleistungLesen { get; init; }
    public Func<bool>? ImProjektLesen { get; init; }
    public Func<string>? KatalogAbweichungLesen { get; init; }
    public Func<string>? ImportzustandLesen { get; init; }
    public Func<bool>? KatalogbetriebLesen { get; init; }

    /// <summary>Liefert die Ganglinien, die die Katalogliste führt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? KatalogEintraege { get; init; }

    /// <summary>Die PV-Ganglinien des Katalogs.</summary>
    public IReadOnlyList<KiWahleintrag> KatalogganglinieWahl
        => KatalogEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Die im KATALOG markierte Ganglinie. Sie zu setzen markiert sie in der Liste; der Detailblock zieht nach.
    /// </summary>
    public string Katalogganglinie
    {
        get => KatalogLesen?.Invoke() ?? "";
        set => KatalogSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die im PROJEKT markierte Ganglinie; leer, wenn der Katalog markiert ist.</summary>
    public string Projektganglinie => ProjektganglinieLesen?.Invoke() ?? "";

    /// <summary>Die Beschreibung der markierten Ganglinie — der Kopftext der Datei.</summary>
    public string Quelle => QuelleLesen?.Invoke() ?? "";

    /// <summary>Das Raster der markierten Ganglinie (Stunden- oder Viertelstundenwerte).</summary>
    public string Aufloesung => AufloesungLesen?.Invoke() ?? "";

    /// <summary>Die Jahresarbeit der markierten Ganglinie, mit Einheit.</summary>
    public string Jahressumme => JahressummeLesen?.Invoke() ?? "";

    /// <summary>Die Nennleistung der markierten Ganglinie, mit Einheit — oder warum keine gilt.</summary>
    public string Nennleistung => NennleistungLesen?.Invoke() ?? "";

    /// <summary>Steht die markierte Ganglinie in der Projektliste?</summary>
    public bool ImProjekt => ImProjektLesen?.Invoke() ?? false;

    /// <summary>
    /// Worin die Projektkopie der markierten Projektganglinie vom Katalogsatz abweicht („weicht vom Katalog ab: …");
    /// leer, wenn sie ihm gleicht oder keine Projektzeile markiert ist.
    /// </summary>
    public string KatalogAbweichung => KatalogAbweichungLesen?.Invoke() ?? "";

    /// <summary>Wie der Import steht.</summary>
    public string Importzustand => ImportzustandLesen?.Invoke() ?? "";

    /// <summary>Ist die Maske ohne Projekt geöffnet?</summary>
    public bool Katalogbetrieb => KatalogbetriebLesen?.Invoke() ?? false;
}
