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
    /// <summary>„Typkennfelder laden…" in der Katalogwahl — derselbe Text wie in der Verwaltung.</summary>
    public string KnopfTypkennfelder { get; set; } = Resource.KM_BTN_TYPKENNFELDER;
    /// <summary>Ergebnis des Ladens; {0} = neu angelegt, {1} = übersprungen.</summary>
    public string TypkennfelderGeladen { get; set; } = Resource.KM_MSG_TYPKENNFELDER;
    /// <summary>Das Laden der Typkennfelder ist gescheitert, ohne Grund des Kerns.</summary>
    public string TypkennfelderFehler { get; set; } = Resource.KM_MSG_FEHLER;
    /// <summary>„Katalogverwaltung…" in der Katalogwahl.</summary>
    public string KnopfVerwaltung { get; set; } = Resource.KM_ANL_BTN_VERWALTUNG;
    /// <summary>Die Plattform öffnet die Verwaltung nicht.</summary>
    public string VerwaltungNicht { get; set; } = Resource.KM_ANL_MSG_VERWALTUNG_NICHT;
    /// <summary>Ungespeicherte Änderungen halten „Katalogverwaltung…" an.</summary>
    public string VerwaltungUngespeichert { get; set; } = Resource.KM_ANL_MSG_UNGESPEICHERT;

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

    // KM3-E3-b: die Lesewerte der Teillastrechnung (dieselben Beschriftungen wie in der Verwaltung).
    public string LabelTeillastWeg { get; set; } = Resource.KM_LBL_TEILLAST_WEG;
    public string LabelVerdichterregelung { get; set; } = Resource.KM_LBL_VERDICHTERREGELUNG;
    public string LabelTaktverlust { get; set; } = Resource.KM_LBL_TAKTVERLUST_CD;
    public string LabelKennfeldrand { get; set; } = Resource.KM_LBL_KENNFELD_RANDWEG;
    /// <summary>„Folgeschaltung von {0} Maschinen: …“ — nur bei Anzahl &gt; 1 und gesetzter Teillastrechnung.</summary>
    public string HinweisFolgeschaltung { get; set; } = Resource.KMA_HINWEIS_FOLGESCHALTUNG;

    public string GruppeBetrieb { get; set; } = Resource.KMA_GRUPPE_BETRIEB;

    /// <summary>Block „Wärmepumpen im Kühlbetrieb“ unter der Anlagenliste.</summary>
    public string GruppeWaermepumpen { get; set; } = Resource.KMA_GRUPPE_WP;
    public string WaermepumpenLeer { get; set; } = Resource.KMA_WP_LEER;
    public string WaermepumpenHinweis { get; set; } = Resource.KMA_WP_HINWEIS;
    public string WaermepumpenFehler { get; set; } = Resource.KMA_WP_FEHLER;
    /// <summary>Format, wenn die Anlagen beim Aufbau nicht zu lesen sind (Ressource KMA_LADEN_FEHLER).</summary>
    public string LadenFehler { get; set; } = Resource.KMA_LADEN_FEHLER;
    /// <summary>Format, wenn die Wärmepumpen beim Aufbau nicht zu lesen sind (Ressource KMA_WP_LADEN_FEHLER).</summary>
    public string WaermepumpenLadenFehler { get; set; } = Resource.KMA_WP_LADEN_FEHLER;
    /// <summary>Format, wenn der Katalog der Katalogwahl nicht zu lesen ist (Ressource KMA_KATALOG_LADEN_FEHLER).</summary>
    public string KatalogLadenFehler { get; set; } = Resource.KMA_KATALOG_LADEN_FEHLER;
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

    // ------------------------------------------------------------ Katalogauswahl V1 (Stufe 5)
    /// <summary>Titel der Projektliste (KMA_LISTE_PROJEKT).</summary>
    public string ListeProjekt { get; set; } = Resource.KMA_LISTE_PROJEKT;
    /// <summary>Titel der Katalogliste (KMA_LISTE_KATALOG).</summary>
    public string ListeKatalog { get; set; } = Resource.KMA_LISTE_KATALOG;
    /// <summary>Die Summe der Nennkälteleistungen in der Projekt-Kopfleiste (KMA_SUMME).</summary>
    public string Summe { get; set; } = Resource.KMA_SUMME;
    /// <summary>Der Kontext im Dialogkopf (KMA_KOPFBAND).</summary>
    public string Kopfband { get; set; } = Resource.KMA_KOPFBAND;
    /// <summary>Spalte Wahl der Projektliste (KMA_SP_WAHL).</summary>
    public string SpalteWahl { get; set; } = Resource.KMA_SP_WAHL;
    /// <summary>Spalte EER/SEER der Projektliste (KMA_SP_EER).</summary>
    public string SpalteEer { get; set; } = Resource.KMA_SP_EER;
    /// <summary>Spalte Rückkühlart der Projektliste (KM_LBL_RUECKKUEHLART).</summary>
    public string SpalteRueckkuehlart { get; set; } = Resource.KM_LBL_RUECKKUEHLART;
    /// <summary>Spalte Kühlträger der Projektliste (KMA_LBL_TRAEGER).</summary>
    public string SpalteTraeger { get; set; } = Resource.KMA_LBL_TRAEGER;
    /// <summary>Spalte eigener Zähler der Projektliste (KMA_LBL_ZAEHLER).</summary>
    public string SpalteZaehler { get; set; } = Resource.KMA_LBL_ZAEHLER;
    /// <summary>„In das Projekt übernehmen" (AUSWAHL_BTN_UEBERNEHMEN).</summary>
    public string KnopfInsProjekt { get; set; } = Resource.AUSWAHL_BTN_UEBERNEHMEN;
    /// <summary>„Aus dem Projekt entfernen" (AUSWAHL_BTN_ENTFERNEN).</summary>
    public string KnopfEntfernen { get; set; } = Resource.AUSWAHL_BTN_ENTFERNEN;
    /// <summary>Rückfrage vor dem Löschen aus der Datenbank (KMA_FRAGE_KATALOG_LOESCHEN).</summary>
    public string FrageKatalogLoeschen { get; set; } = Resource.KMA_FRAGE_KATALOG_LOESCHEN;
    /// <summary>Titel des Katalogeditors in der Überlagerung (KMA_TITEL_EDITOR).</summary>
    public string TitelEditor { get; set; } = Resource.KMA_TITEL_EDITOR;
    /// <summary>Gruppe der Kenndaten in der Satz-Überlagerung (KMA_GRUPPE_KENNDATEN).</summary>
    public string GruppeKenndaten { get; set; } = Resource.KMA_GRUPPE_KENNDATEN;
    /// <summary>Bezeichnung des Teillastbilds (KM_BILD_TEILLAST).</summary>
    public string BildTeillast { get; set; } = Resource.KM_BILD_TEILLAST;
    /// <summary>Bezeichnung der Reiterleiste der Kennlinienbilder (KM_KL_BEZEICHNUNG).</summary>
    public string KennlinienbildBezeichnung { get; set; } = Resource.KM_KL_BEZEICHNUNG;
    /// <summary>Reiter EER (KM_KL_REITER_EER).</summary>
    public string ReiterEer { get; set; } = Resource.KM_KL_REITER_EER;
    /// <summary>Reiter Kälteleistung (KM_KL_REITER_LEISTUNG).</summary>
    public string ReiterLeistung { get; set; } = Resource.KM_KL_REITER_LEISTUNG;
    /// <summary>Platzhalter ohne vollständigen Kennlinienpunkt (KM_KL_PLATZHALTER).</summary>
    public string PlatzhalterKennlinienbild { get; set; } = Resource.KM_KL_PLATZHALTER;
    /// <summary>Kostenknopf Investition (KDLG_KNOPF_INVEST).</summary>
    public string KostenInvest { get; set; } = Resource.KDLG_KNOPF_INVEST;
    /// <summary>Kostenknopf Betrieb (KDLG_KNOPF_BETRIEB).</summary>
    public string KostenBetrieb { get; set; } = Resource.KDLG_KNOPF_BETRIEB;

    // ------------------------------------------------------------ Einheiten (Symbole)
    public string EinheitKw { get; set; } = "kW";
    public string EinheitProzent { get; set; } = "%";
    public string EinheitGradC { get; set; } = "°C";
}
