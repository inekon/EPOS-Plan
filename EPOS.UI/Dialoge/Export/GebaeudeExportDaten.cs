using EPOS.UI.Bausteine;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Export;

// =====================================================================================
//  Die DTO des Gebäudeexports (Gebäudesimulation G7a, Welle W3; Softwarearchitektur 3.2).
//
//  Die Komponente GebaeudeExportDialog kennt keine Fachklasse des Kerns: Sie bekommt die
//  Ansicht des Exportplans (fertige Meldungstexte, Ablehnung), einen Speicherweg, der Dateiwahl
//  und Schreiben der Plattform kapselt, und die Anzeigetexte als Bündel. Ablauf und Schreiber
//  liegen im Kern, die Datenseite in der Hülle (EPOS.UI.Daten, GebaeudeExportHuelle).
// =====================================================================================

/// <summary>Eine Meldung vor dem Schreiben — Stufe als Warnstufe und Anzeigetext, dazu der fertige Text.</summary>
/// <param name="Stufe">Die Stufe (Fehler, Warnung, Hinweis).</param>
/// <param name="Stufentext">Die Stufe als Anzeigetext.</param>
/// <param name="Text">Der Meldungstext in der Anzeigesprache.</param>
/// <param name="Schluessel">Der sprachneutrale Schlüssel der Meldung (für Tests und Protokoll).</param>
public sealed record GebaeudeExportMeldung(WarnStufe Stufe, string Stufentext, string Text, string Schluessel);

/// <summary>
/// Die Ansicht eines Exportplans: die Meldungen vor dem Schreiben und — bei einer Ablehnung — ihr
/// Grund. Eine abgelehnte Ansicht bietet kein Speichern an.
/// </summary>
/// <param name="Meldungen">Die Meldungen in der Reihenfolge des Ablaufs.</param>
/// <param name="Ablehnung">Der Grund der Ablehnung als Anzeigetext; <c>null</c> = schreibbar.</param>
public sealed record GebaeudeExportAnsicht(IReadOnlyList<GebaeudeExportMeldung> Meldungen, string? Ablehnung)
{
    /// <summary>Ist der Export abgelehnt?</summary>
    public bool Abgelehnt => Ablehnung is not null;
}

/// <summary>Das Ergebnis des Speicherwegs.</summary>
/// <param name="Gespeichert">Wurde die Datei geschrieben?</param>
/// <param name="Abgebrochen">Hat der Anwender die Dateiwahl abgebrochen (nichts geschrieben, keine Meldung)?</param>
/// <param name="Text">Die Rückmeldung (mit Pfad) bzw. der Fehlergrund.</param>
public sealed record GebaeudeExportErgebnis(bool Gespeichert, bool Abgebrochen, string Text)
{
    /// <summary>Die Dateiwahl ist abgebrochen.</summary>
    public static GebaeudeExportErgebnis Abbruch { get; } = new(false, true, "");
}

/// <summary>Die Anzeigetexte des Exportdialogs (Bündel).</summary>
public sealed class GebaeudeExportTexte
{
    /// <summary>GEXP_TITEL</summary>
    public string Titel { get; set; } = Resource.GEXP_TITEL;

    /// <summary>GEXP_LBL_FORMAT</summary>
    public string Format { get; set; } = Resource.GEXP_LBL_FORMAT;

    /// <summary>GEXP_FORMAT_GBXML</summary>
    public string FormatGbxml { get; set; } = Resource.GEXP_FORMAT_GBXML;

    /// <summary>GEXP_LBL_STUFE</summary>
    public string Stufe { get; set; } = Resource.GEXP_LBL_STUFE;

    /// <summary>GEXP_STUFE_DATEN</summary>
    public string StufeDaten { get; set; } = Resource.GEXP_STUFE_DATEN;

    /// <summary>GEXP_LBL_PLZ</summary>
    public string Plz { get; set; } = Resource.GEXP_LBL_PLZ;

    /// <summary>GEXP_PLZ_HINWEIS</summary>
    public string PlzHinweis { get; set; } = Resource.GEXP_PLZ_HINWEIS;

    /// <summary>GEXP_GESPEICHERTER_STAND</summary>
    public string GespeicherterStand { get; set; } = Resource.GEXP_GESPEICHERTER_STAND;

    /// <summary>GEXP_GRP_MELDUNGEN</summary>
    public string GruppeMeldungen { get; set; } = Resource.GEXP_GRP_MELDUNGEN;

    /// <summary>GEXP_SP_STUFE</summary>
    public string SpalteStufe { get; set; } = Resource.GEXP_SP_STUFE;

    /// <summary>GEXP_SP_MELDUNG</summary>
    public string SpalteMeldung { get; set; } = Resource.GEXP_SP_MELDUNG;

    /// <summary>GEXP_KEINE_MELDUNGEN</summary>
    public string KeineMeldungen { get; set; } = Resource.GEXP_KEINE_MELDUNGEN;

    /// <summary>GEXP_BESTAETIGEN</summary>
    public string Bestaetigen { get; set; } = Resource.GEXP_BESTAETIGEN;

    /// <summary>GEXP_SPERRE_BESTAETIGEN</summary>
    public string SperreBestaetigen { get; set; } = Resource.GEXP_SPERRE_BESTAETIGEN;

    /// <summary>GEXP_BTN_SPEICHERN bzw. auf iOS GEXP_BTN_SPEICHERN_IOS — die Hülle wählt.</summary>
    public string Speichern { get; set; } = Resource.GEXP_BTN_SPEICHERN;

    /// <summary>ALLG_BTN_ABBRECHEN</summary>
    public string Abbrechen { get; set; } = Resource.ALLG_BTN_ABBRECHEN;

    /// <summary>GEXP_ABGELEHNT — {0} = Grund.</summary>
    public string Abgelehnt { get; set; } = Resource.GEXP_ABGELEHNT;

    /// <summary>GEXP_BTN_SCHLIESSEN — der einzige Knopf eines abgelehnten Exports.</summary>
    public string Schliessen { get; set; } = Resource.GEXP_BTN_SCHLIESSEN;

    /// <summary>GEXP_VORBEREITUNG — solange die Daten gelesen werden.</summary>
    public string Vorbereitung { get; set; } = Resource.GEXP_VORBEREITUNG;
}

/// <summary>
/// Das FLACHE Abbild des <see cref="GebaeudeExportDialog"/> für den Hilfe-Assistenten (G7a, Welle W3):
/// die Postleitzahl (setzbar) und die Bestätigung der Meldungen (nur zu lesen — bestätigen, die Meldungen
/// gelesen zu haben, kann nur der Anwender). Gespeichert wird mit einem Klick des Anwenders auf
/// „Speichern…". Sie hält keinen Zustand: Jede Eigenschaft ruft ihren Delegaten.
/// </summary>
public sealed class GebaeudeExportKiSicht
{
    public Func<string>? PlzLesen { get; init; }
    public Action<string>? PlzSetzen { get; init; }
    public Func<bool>? BestaetigtLesen { get; init; }

    /// <summary>Die Postleitzahl des Standorts (freiwillig).</summary>
    public string Plz { get => PlzLesen?.Invoke() ?? ""; set => PlzSetzen?.Invoke(value ?? ""); }

    /// <summary>Sind die Meldungen bestätigt?</summary>
    public bool Bestaetigt => BestaetigtLesen?.Invoke() ?? false;
}
