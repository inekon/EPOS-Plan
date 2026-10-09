using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Die Anzeigetexte des Erzeugerdialogs „Kältemaschinen im Projekt"</b> (KU3-4c) — ein Bündel, das sich
/// selbst aus <c>MyResource</c> füllt (beide Sprachen). Einheiten sind Symbole und keine Übersetzung.
/// </summary>
public sealed class KaeltemaschineAnlageTexte
{
    public string Titel { get; set; } = Resource.KMA_TITEL;
    public string Leer { get; set; } = Resource.KMA_LEER;
    public string GruppeAnlagen { get; set; } = Resource.KMA_GRUPPE_ANLAGEN;
    public string SpalteName { get; set; } = Resource.KMA_SP_NAME;
    public string SpalteAnzahl { get; set; } = Resource.KMA_SP_ANZAHL;
    public string SpalteLeistung { get; set; } = Resource.KMA_SP_LEISTUNG;

    public string KnopfHinzu { get; set; } = Resource.KMA_BTN_HINZU;
    public string KnopfLoeschen { get; set; } = Resource.KMA_BTN_LOESCHEN;
    public string KnopfUebernehmen { get; set; } = Resource.KMA_BTN_UEBERNEHMEN;
    public string Ok { get; set; } = Resource.ALLG_BTN_OK;
    public string Abbrechen { get; set; } = Resource.ALLG_BTN_ABBRECHEN;
    public string Ja { get; set; } = Resource.ALLG_BTN_JA;
    public string Nein { get; set; } = Resource.ALLG_BTN_NEIN;

    public string TitelKatalog { get; set; } = Resource.KMA_TITEL_KATALOG;
    public string KatalogKeineWahl { get; set; } = Resource.KMA_KATALOG_KEINE_WAHL;
    public string KatalogLeer { get; set; } = Resource.KMA_KATALOG_LEER;

    public string GruppeGeraet { get; set; } = Resource.KMA_GRUPPE_GERAET;
    public string HinweisKatalog { get; set; } = Resource.KMA_HINWEIS_KATALOG;
    public string HinweisNeu { get; set; } = Resource.KMA_HINWEIS_NEU;
    public string LabelBezeichner { get; set; } = Resource.KM_LBL_BEZEICHNER;
    public string LabelFirma { get; set; } = Resource.KM_LBL_FIRMA;
    public string LabelTyp { get; set; } = Resource.KM_LBL_TYP;
    public string LabelNennkaelteleistung { get; set; } = Resource.KM_LBL_NENNKAELTELEISTUNG;
    public string LabelNennEer { get; set; } = Resource.KM_LBL_NENN_EER;
    public string LabelRueckkuehlart { get; set; } = Resource.KM_LBL_RUECKKUEHLART;
    public string LabelMindestteillast { get; set; } = Resource.KM_LBL_MINDESTTEILLAST;
    public string LabelKaltwasserMin { get; set; } = Resource.KM_LBL_KALTWASSER_MIN;

    public string GruppeBetrieb { get; set; } = Resource.KMA_GRUPPE_BETRIEB;

    /// <summary>Block „Wärmepumpen im Kühlbetrieb“ unter der Anlagenliste.</summary>
    public string GruppeWaermepumpen { get; set; } = Resource.KMA_GRUPPE_WP;
    public string WaermepumpenLeer { get; set; } = Resource.KMA_WP_LEER;
    public string WaermepumpenHinweis { get; set; } = Resource.KMA_WP_HINWEIS;
    public string WaermepumpenFehler { get; set; } = Resource.KMA_WP_FEHLER;
    public string LabelName { get; set; } = Resource.KMA_LBL_NAME;
    public string LabelAnzahl { get; set; } = Resource.KMA_LBL_ANZAHL;
    public string LabelVorlauf { get; set; } = Resource.KMA_LBL_VORLAUF;
    public string PlatzhalterVorlauf { get; set; } = Resource.KMA_PLATZHALTER_VORLAUF;
    public string HinweisVorlaufMin { get; set; } = Resource.KMA_HINWEIS_VORLAUF_MIN;
    public string LabelHilfsstrom { get; set; } = Resource.KMA_LBL_HILFSSTROM;
    public string HinweisHilfsstrom { get; set; } = Resource.KMA_HINWEIS_HILFSSTROM;
    public string LabelTraeger { get; set; } = Resource.KMA_LBL_TRAEGER;
    public string PlatzhalterTraeger { get; set; } = Resource.KMA_PLATZHALTER_TRAEGER;
    public string LabelZaehler { get; set; } = Resource.KMA_LBL_ZAEHLER;
    public string HinweisZaehler { get; set; } = Resource.KMA_HINWEIS_ZAEHLER;
    public string HinweisReihenfolge { get; set; } = Resource.KMA_HINWEIS_REIHENFOLGE;

    public string FrageLoeschen { get; set; } = Resource.KMA_FRAGE_LOESCHEN;
    public string AnlegenFehlt { get; set; } = Resource.KMA_MSG_ANLEGEN_FEHLT;

    // ------------------------------------------------------------ Einheiten (Symbole)
    public string EinheitKw { get; set; } = "kW";
    public string EinheitProzent { get; set; } = "%";
    public string EinheitGradC { get; set; } = "°C";
}
