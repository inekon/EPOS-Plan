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
/// <param name="Meldungen">Die Meldungen eines Schreibens, das abgebrochen ist (Stufe G7c: die Schemaprüfung
/// des IFC-Schreibers findet Verstöße, es entsteht keine Datei) — sie ersetzen die Meldungen der Vorschau;
/// <c>null</c> = die Vorschau bleibt.</param>
public sealed record GebaeudeExportErgebnis(bool Gespeichert, bool Abgebrochen, string Text,
                                            IReadOnlyList<GebaeudeExportMeldung>? Meldungen = null)
{
    /// <summary>Die Dateiwahl ist abgebrochen.</summary>
    public static GebaeudeExportErgebnis Abbruch { get; } = new(false, true, "");
}

/// <summary>
/// Ein wählbares Format des Gebäudeexports (Stufe G7c). <paramref name="Wert"/> ist der Steuerwert
/// (<c>GebaeudeQuelle.FORMAT_GBXML</c> bzw. <c>FORMAT_IFC</c>), den die Hülle an das Exportprofil reicht;
/// angezeigt werden nur <paramref name="Text"/> und <paramref name="Umfang"/>.
/// </summary>
/// <param name="Wert">Der Steuerwert des Formats.</param>
/// <param name="Text">Der Anzeigetext (<c>GEXP_FORMAT_*</c>).</param>
/// <param name="Umfang">Der Umfang der Datei als Anzeigetext (<c>GEXP_STUFE_*</c>).</param>
/// <param name="MitZusage">Führt das Format eine Exportzusage (IDS)? Dann steht der Knopf dazu im Dialog.</param>
/// <param name="MitAnreicherung">Kann das Format die importierte Originaldatei anreichern (Stufe G7d, IFC)? Dann
/// steht — wenn das Gebäude eine Importquelle des Formats hat — die Wahl des Ausgabewegs im Dialog.</param>
public sealed record GebaeudeExportFormat(string Wert, string Text, string Umfang, bool MitZusage = false,
                                          bool MitAnreicherung = false);

/// <summary>
/// Die gewählte Originaldatei der Anreicherung (Stufe G7d, Datenaustauschkonzept 6.6) — fertige Anzeigetexte:
/// Dateiname, Importquelle (Dateiname und Importzeitpunkt) und die Meldungen der Vorschau (Sperren als Fehler,
/// bei Freigabe der Beipackzettel als Warnung). Die Datei selbst behält die Hülle im Speicher.
/// </summary>
/// <param name="Dateiname">Der Name der gewählten Datei (ohne Pfad).</param>
/// <param name="Quelle">Die Importquelle als Anzeigetext.</param>
/// <param name="Meldungen">Die Meldungen der Vorschau.</param>
/// <param name="Grund">Der Grund der Verweigerung als Anzeigetext; <c>null</c> = frei (Beipackzettel zu bestätigen).</param>
public sealed record GebaeudeAnreicherungWahl(string Dateiname, string Quelle,
                                              IReadOnlyList<GebaeudeExportMeldung> Meldungen, string? Grund)
{
    /// <summary>Ist die Anreicherung verweigert?</summary>
    public bool Verweigert => Grund is not null;
}

/// <summary>Die Eingaben des Anwenders, nach denen der Plan gebildet und die Datei geschrieben wird.</summary>
/// <param name="Plz">Die Postleitzahl des Standorts (freiwillig).</param>
/// <param name="Format">Der Steuerwert des gewählten Formats; leer = das erste Format (gbXML).</param>
public sealed record GebaeudeExportEingabe(string Plz, string Format);

/// <summary>Die Anzeigetexte des Exportdialogs (Bündel).</summary>
public sealed class GebaeudeExportTexte
{
    /// <summary>GEXP_TITEL_FORMATWAHL — ohne Format, das steht im Dialog.</summary>
    public string Titel { get; set; } = Resource.GEXP_TITEL_FORMATWAHL;

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

    /// <summary>GEXP_MSG_NICHTS_GESCHRIEBEN — das Schreiben ist abgebrochen, die Meldungen nennen den Grund.</summary>
    public string NichtsGeschrieben { get; set; } = Resource.GEXP_MSG_NICHTS_GESCHRIEBEN;

    /// <summary>GEXP_BTN_ZUSAGE bzw. auf iOS GEXP_BTN_ZUSAGE_IOS — die Hülle wählt.</summary>
    public string Zusage { get; set; } = Resource.GEXP_BTN_ZUSAGE;

    /// <summary>GEXP_ZUSAGE_HINWEIS</summary>
    public string ZusageHinweis { get; set; } = Resource.GEXP_ZUSAGE_HINWEIS;

    /// <summary>GEXP_ANR_LBL_WEG</summary>
    public string Weg { get; set; } = Resource.GEXP_ANR_LBL_WEG;

    /// <summary>GEXP_ANR_WEG_EIGEN</summary>
    public string WegEigen { get; set; } = Resource.GEXP_ANR_WEG_EIGEN;

    /// <summary>GEXP_ANR_WEG_ANREICHERN</summary>
    public string WegAnreichern { get; set; } = Resource.GEXP_ANR_WEG_ANREICHERN;

    /// <summary>GEXP_ANR_HINWEIS</summary>
    public string AnreicherungHinweis { get; set; } = Resource.GEXP_ANR_HINWEIS;

    /// <summary>GEXP_ANR_BTN_WAEHLEN</summary>
    public string OriginalWaehlen { get; set; } = Resource.GEXP_ANR_BTN_WAEHLEN;

    /// <summary>GEXP_ANR_LBL_DATEI</summary>
    public string Originaldatei { get; set; } = Resource.GEXP_ANR_LBL_DATEI;

    /// <summary>GEXP_ANR_LBL_QUELLE</summary>
    public string Importquelle { get; set; } = Resource.GEXP_ANR_LBL_QUELLE;

    /// <summary>GEXP_ANR_VERWEIGERT — {0} = Grund.</summary>
    public string Verweigert { get; set; } = Resource.GEXP_ANR_VERWEIGERT;

    /// <summary>GEXP_ANR_BTN_EIGENE</summary>
    public string StattEigene { get; set; } = Resource.GEXP_ANR_BTN_EIGENE;

    /// <summary>GEXP_ANR_BESTAETIGEN</summary>
    public string BeipackBestaetigen { get; set; } = Resource.GEXP_ANR_BESTAETIGEN;

    /// <summary>GEXP_ANR_SPERRE_BESTAETIGEN</summary>
    public string SperreBeipack { get; set; } = Resource.GEXP_ANR_SPERRE_BESTAETIGEN;

    /// <summary>GEXP_ANR_SPERRE_DATEI</summary>
    public string SperreOriginal { get; set; } = Resource.GEXP_ANR_SPERRE_DATEI;
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
