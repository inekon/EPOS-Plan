using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Die Anzeigetexte der Verwaltung „Rückkühlwerke" (<see cref="RueckkuehlwerkDialog"/>, K-F5a) — EIN Parameter statt
/// vieler. Beschriftungen, kein Zustand; jede Eigenschaft füllt sich selbst aus <c>MyResource</c> und nennt ihren
/// Schlüssel im Kommentar. Die Einheiten sind sprachneutral.
/// </summary>
public sealed class RueckkuehlwerkTexte
{
    // ------------------------------------------------------------ Kopf, Liste, Gruppen

    /// <summary><c>RKW_TITEL</c></summary>
    public string Titel { get; set; } = Resource.RKW_TITEL;

    /// <summary><c>RKW_LEER</c> — die leise Zeile über einer leeren Liste.</summary>
    public string LeerKatalog { get; set; } = Resource.RKW_LEER;

    /// <summary><c>RKW_GRUPPE_NENNPUNKT</c></summary>
    public string GruppeNennpunkt { get; set; } = Resource.RKW_GRUPPE_NENNPUNKT;

    /// <summary><c>RKW_GRUPPE_ANNAEHERUNG</c></summary>
    public string GruppeAnnaeherung { get; set; } = Resource.RKW_GRUPPE_ANNAEHERUNG;

    /// <summary><c>RKW_GRUPPE_VENTILATOR</c></summary>
    public string GruppeVentilator { get; set; } = Resource.RKW_GRUPPE_VENTILATOR;

    /// <summary><c>RKW_GRUPPE_WASSER</c> — nur bei nassen Bauarten.</summary>
    public string GruppeWasser { get; set; } = Resource.RKW_GRUPPE_WASSER;

    /// <summary><c>RKW_GRUPPE_FREIKUEHLUNG</c></summary>
    public string GruppeFreikuehlung { get; set; } = Resource.RKW_GRUPPE_FREIKUEHLUNG;

    /// <summary><c>ADM_SB_HERKUNFT</c></summary>
    public string GruppeHerkunft { get; set; } = Resource.ADM_SB_HERKUNFT;

    // ------------------------------------------------------------ Beschriftungen

    /// <summary><c>RKW_LBL_BEZEICHNER</c></summary>
    public string LabelBezeichner { get; set; } = Resource.RKW_LBL_BEZEICHNER;

    /// <summary><c>RKW_LBL_BESCHREIBUNG</c></summary>
    public string LabelBeschreibung { get; set; } = Resource.RKW_LBL_BESCHREIBUNG;

    /// <summary><c>RKW_LBL_BAUART</c></summary>
    public string LabelBauart { get; set; } = Resource.RKW_LBL_BAUART;

    /// <summary><c>RKW_LBL_NENNLEISTUNG</c></summary>
    public string LabelNennleistung { get; set; } = Resource.RKW_LBL_NENNLEISTUNG;

    /// <summary><c>RKW_LBL_ANNAEHERUNG_NENN</c></summary>
    public string LabelAnnaeherungNenn { get; set; } = Resource.RKW_LBL_ANNAEHERUNG_NENN;

    /// <summary><c>RKW_LBL_ANNAEHERUNG_WEG</c></summary>
    public string LabelAnnaeherungWeg { get; set; } = Resource.RKW_LBL_ANNAEHERUNG_WEG;

    /// <summary><c>RKW_LBL_VENTILATOR_NENN</c></summary>
    public string LabelVentilatorNenn { get; set; } = Resource.RKW_LBL_VENTILATOR_NENN;

    /// <summary><c>RKW_LBL_VENTILATOR_REGELUNG</c></summary>
    public string LabelVentilatorRegelung { get; set; } = Resource.RKW_LBL_VENTILATOR_REGELUNG;

    /// <summary><c>RKW_LBL_VENTILATOR_STUFEN</c></summary>
    public string LabelVentilatorStufen { get; set; } = Resource.RKW_LBL_VENTILATOR_STUFEN;

    /// <summary><c>RKW_LBL_VENTILATOR_DREHZAHL_MIN</c></summary>
    public string LabelVentilatorDrehzahlMin { get; set; } = Resource.RKW_LBL_VENTILATOR_DREHZAHL_MIN;

    /// <summary><c>RKW_LBL_BEFEUCHTUNG_WIRKUNGSGRAD</c></summary>
    public string LabelBefeuchtungWirkungsgrad { get; set; } = Resource.RKW_LBL_BEFEUCHTUNG_WIRKUNGSGRAD;

    /// <summary><c>RKW_LBL_BEFEUCHTUNG_AB</c></summary>
    public string LabelBefeuchtungAb { get; set; } = Resource.RKW_LBL_BEFEUCHTUNG_AB;

    /// <summary><c>RKW_LBL_VERDUNSTUNG_FAKTOR</c></summary>
    public string LabelVerdunstungFaktor { get; set; } = Resource.RKW_LBL_VERDUNSTUNG_FAKTOR;

    /// <summary><c>RKW_LBL_EINDICKUNG</c></summary>
    public string LabelEindickung { get; set; } = Resource.RKW_LBL_EINDICKUNG;

    /// <summary><c>RKW_LBL_DRIFT_ANTEIL</c></summary>
    public string LabelDriftAnteil { get; set; } = Resource.RKW_LBL_DRIFT_ANTEIL;

    /// <summary><c>RKW_LBL_FREIKUEHLUNG_SCHALTUNG</c></summary>
    public string LabelFreikuehlungSchaltung { get; set; } = Resource.RKW_LBL_FREIKUEHLUNG_SCHALTUNG;

    /// <summary><c>RKW_LBL_MODULKOSTEN</c></summary>
    public string LabelModulkosten { get; set; } = Resource.RKW_LBL_MODULKOSTEN;

    /// <summary><c>RKW_LBL_VERWENDUNG</c></summary>
    public string LabelVerwendung { get; set; } = Resource.RKW_LBL_VERWENDUNG;

    /// <summary><c>RKW_VERWENDUNG</c> — „in {0} Projektanlage(n) gewählt".</summary>
    public string Verwendung { get; set; } = Resource.RKW_VERWENDUNG;

    /// <summary><c>RKW_VERWENDUNG_KEINE</c></summary>
    public string VerwendungKeine { get; set; } = Resource.RKW_VERWENDUNG_KEINE;

    /// <summary><c>RKW_VORGABE</c> — „Vorgabe: {0}", Platzhalter und Herleitung eines leeren Feldes.</summary>
    public string Vorgabe { get; set; } = Resource.RKW_VORGABE;

    /// <summary><c>RKW_VORGABE_VENTILATOR</c> — die Herleitung einer leeren Ventilatorleistung.</summary>
    public string VorgabeVentilator { get; set; } = Resource.RKW_VORGABE_VENTILATOR;

    /// <summary><c>RKW_HINWEIS_BIMSCHV</c> — der Hinweis bei nassen Bauarten.</summary>
    public string HinweisBimschv { get; set; } = Resource.RKW_HINWEIS_BIMSCHV;

    /// <summary><c>RKW_HINWEIS_KOSTEN</c> — der Gerätepreis steht in der Kostenverwaltung.</summary>
    public string HinweisKosten { get; set; } = Resource.RKW_HINWEIS_KOSTEN;

    /// <summary>Einheit Leistung.</summary>
    public string EinheitKw { get; set; } = "kW";

    /// <summary>Einheit Temperaturdifferenz.</summary>
    public string EinheitK { get; set; } = "K";

    /// <summary>Einheit Temperatur.</summary>
    public string EinheitGradC { get; set; } = "°C";

    /// <summary>Einheit Preis.</summary>
    public string EinheitEuro { get; set; } = "€";

    // ------------------------------------------------------------ Knöpfe

    /// <summary><c>ADM_BTN_SPEICHERN</c></summary>
    public string KnopfSpeichern { get; set; } = Resource.ADM_BTN_SPEICHERN;

    /// <summary><c>ADM_BTN_VERWERFEN</c></summary>
    public string KnopfVerwerfen { get; set; } = Resource.ADM_BTN_VERWERFEN;

    /// <summary><c>ADM_BTN_NEU</c></summary>
    public string KnopfNeu { get; set; } = Resource.ADM_BTN_NEU;

    /// <summary><c>ADM_BTN_BEENDEN</c></summary>
    public string KnopfBeenden { get; set; } = Resource.ADM_BTN_BEENDEN;

    /// <summary><c>ADM_BTN_DUPLIZIEREN</c></summary>
    public string KnopfDuplizieren { get; set; } = Resource.ADM_BTN_DUPLIZIEREN;

    /// <summary><c>BST_BTN_LOESCHEN</c> — „Löschen".</summary>
    public string KnopfLoeschen { get; set; } = Resource.BST_BTN_LOESCHEN;

    /// <summary><c>ALLG_BTN_OK</c></summary>
    public string Ok { get; set; } = Resource.ALLG_BTN_OK;

    /// <summary><c>ALLG_BTN_ABBRECHEN</c></summary>
    public string Abbrechen { get; set; } = Resource.ALLG_BTN_ABBRECHEN;

    /// <summary><c>ALLG_BTN_JA</c></summary>
    public string Ja { get; set; } = Resource.ALLG_BTN_JA;

    /// <summary><c>ALLG_BTN_NEIN</c></summary>
    public string Nein { get; set; } = Resource.ALLG_BTN_NEIN;

    // ------------------------------------------------------------ Meldungen

    /// <summary><c>RKW_TITEL_NEU</c></summary>
    public string TitelNeu { get; set; } = Resource.RKW_TITEL_NEU;

    /// <summary><c>RKW_FRAGE_LOESCHEN</c> — „Soll das Rückkühlwerk „{0}“ gelöscht werden?"</summary>
    public string FrageLoeschen { get; set; } = Resource.RKW_FRAGE_LOESCHEN;

    /// <summary><c>RKW_AW_LOESCHEN_VERWENDET</c> — der Sperrgrund von „Löschen" mit der Zahl der Projektanlagen.</summary>
    public string LoeschenVerwendet { get; set; } = Resource.RKW_AW_LOESCHEN_VERWENDET;

    /// <summary><c>RKW_LOESCHEN_BLEIBEN_VERWENDET</c></summary>
    public string LoeschenBleibenVerwendet { get; set; } = Resource.RKW_LOESCHEN_BLEIBEN_VERWENDET;

    /// <summary><c>RKW_MSG_ANGELEGT</c></summary>
    public string Angelegt { get; set; } = Resource.RKW_MSG_ANGELEGT;

    /// <summary><c>RKW_MSG_GELOESCHT</c></summary>
    public string Geloescht { get; set; } = Resource.RKW_MSG_GELOESCHT;

    /// <summary><c>RKW_MSG_FEHLER</c></summary>
    public string Fehler { get; set; } = Resource.RKW_MSG_FEHLER;

    /// <summary><c>ADM_SPEICHERN_GESPERRT</c></summary>
    public string SpeichernGesperrt { get; set; } = Resource.ADM_SPEICHERN_GESPERRT;

    /// <summary><c>RKW_MSG_NAME_BELEGT</c></summary>
    public string NameBelegt { get; set; } = Resource.RKW_MSG_NAME_BELEGT;

    /// <summary><c>RKW_MSG_NAME_LEER</c></summary>
    public string NameLeer { get; set; } = Resource.RKW_MSG_NAME_LEER;
}
