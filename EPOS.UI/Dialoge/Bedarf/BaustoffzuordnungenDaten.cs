using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  Die DTO der Ansicht „Baustoff-Zuordnungen…" (Nacharbeit G4b; Softwarearchitektur 3.2).
//
//  Die Komponente BaustoffzuordnungenDialog kennt keine Fachklasse des Kerns: Sie bekommt die
//  gemerkten Zuordnungen des Projekts als fertige Anzeigezeilen, einen Schreibweg für das Entfernen
//  und die Anzeigetexte als Bündel. Die Datenbankseite liegt im Kern (BaustoffabgleichCtrl), die
//  Datenseite in der Hülle (EPOS.UI.Daten, BaustoffzuordnungenHuelle).
// =====================================================================================

/// <summary>Eine gemerkte Zuordnung als Anzeigezeile.</summary>
/// <param name="Schluessel">Der gespeicherte, normalisierte Materialname — der Schlüssel für das Entfernen.</param>
/// <param name="Materialname">Der Materialname, wie er angezeigt wird.</param>
/// <param name="Baustoff">Der zugeordnete Katalogbaustoff als Anzeigetext.</param>
/// <param name="Zeitpunkt">Der Zeitpunkt der Zuordnung in der Anzeigekultur.</param>
public sealed record BaustoffzuordnungZeile(string Schluessel, string Materialname, string Baustoff, string Zeitpunkt);

/// <summary>Die Anzeigetexte der Ansicht „Baustoff-Zuordnungen" (Bündel).</summary>
public sealed class BaustoffzuordnungenTexte
{
    /// <summary>BSZU_TITEL</summary>
    public string Titel { get; set; } = Resource.BSZU_TITEL;

    /// <summary>BSZU_ERKLAERUNG</summary>
    public string Erklaerung { get; set; } = Resource.BSZU_ERKLAERUNG;

    /// <summary>BSZU_SP_MATERIALNAME</summary>
    public string SpalteMaterialname { get; set; } = Resource.BSZU_SP_MATERIALNAME;

    /// <summary>BSZU_SP_BAUSTOFF</summary>
    public string SpalteBaustoff { get; set; } = Resource.BSZU_SP_BAUSTOFF;

    /// <summary>BSZU_SP_ZEITPUNKT</summary>
    public string SpalteZeitpunkt { get; set; } = Resource.BSZU_SP_ZEITPUNKT;

    /// <summary>BSZU_BTN_ENTFERNEN</summary>
    public string Entfernen { get; set; } = Resource.BSZU_BTN_ENTFERNEN;

    /// <summary>BSZU_BTN_ENTFERNEN_HINWEIS — {0} = Materialname.</summary>
    public string EntfernenHinweis { get; set; } = Resource.BSZU_BTN_ENTFERNEN_HINWEIS;

    /// <summary>BSZU_KEINE</summary>
    public string Keine { get; set; } = Resource.BSZU_KEINE;

    /// <summary>ALLG_BTN_OK</summary>
    public string Ok { get; set; } = Resource.ALLG_BTN_OK;

    /// <summary>ALLG_BTN_ABBRECHEN</summary>
    public string Abbrechen { get; set; } = Resource.ALLG_BTN_ABBRECHEN;
}
