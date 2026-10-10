using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Die Anzeigetexte der Verwaltung „Kältemaschinen"</b> (KU3-1) — ein Bündel, das sich selbst aus
/// <c>MyResource</c> füllt (beide Sprachen); ein Test oder ein Wirt kann einzelne überschreiben.
/// Einheiten sind Symbole und keine Übersetzung.
/// </summary>
public sealed class KaeltemaschineKatalogTexte
{
    // ------------------------------------------------------------ Kopf, Liste, Stammblatt
    public string Titel { get; set; } = Resource.KM_TITEL;
    public string LeerKatalog { get; set; } = Resource.KM_LEER;
    public string GruppeKenndaten { get; set; } = Resource.ADM_SB_KENNDATEN;
    public string GruppeKennlinie { get; set; } = Resource.KM_GRUPPE_KENNLINIE;
    /// <summary>Der Schalter über der Liste — <c>KM_CHK_OHNE_TYPKENNFELDER</c>.</summary>
    public string SchalterOhneTypkennfelder { get; set; } = Resource.KM_CHK_OHNE_TYPKENNFELDER;

    // ------------------------------------------------------------ Beschriftungen
    public string LabelBezeichner { get; set; } = Resource.KM_LBL_BEZEICHNER;
    public string LabelFirma { get; set; } = Resource.KM_LBL_FIRMA;
    public string LabelTyp { get; set; } = Resource.KM_LBL_TYP;
    public string LabelBeschreibung { get; set; } = Resource.KM_LBL_BESCHREIBUNG;
    public string LabelNennkaelteleistung { get; set; } = Resource.KM_LBL_NENNKAELTELEISTUNG;
    public string LabelNennEer { get; set; } = Resource.KM_LBL_NENN_EER;
    public string LabelKaeltemittel { get; set; } = Resource.KM_LBL_KAELTEMITTEL;
    public string LabelRueckkuehlart { get; set; } = Resource.KM_LBL_RUECKKUEHLART;
    public string LabelMindestteillast { get; set; } = Resource.KM_LBL_MINDESTTEILLAST;
    public string LabelHilfsstrom { get; set; } = Resource.KM_LBL_HILFSSTROM;
    public string HilfsstromLeer { get; set; } = Resource.KM_HILFSSTROM_LEER;
    public string LabelKaltwasserMin { get; set; } = Resource.KM_LBL_KALTWASSER_MIN;
    public string LabelModulkosten { get; set; } = Resource.KM_LBL_MODULKOSTEN;
    public string RueckkuehlartKeine { get; set; } = Resource.KM_RUECKKUEHLART_KEINE;

    /// <summary>
    /// Die Anzeigetexte der Rückkühlarten in der Reihenfolge der Listenplätze — nur der Rückfall, wenn
    /// die Hülle keine liefert; die Hülle nimmt sie aus dem Kern (<c>KaeltemaschineStammCtrl.RueckkuehlartText</c>).
    /// </summary>
    public IReadOnlyList<string> Rueckkuehlarten { get; set; } = new[]
    {
        Resource.KM_RUECKKUEHLART_LUFT, Resource.KM_RUECKKUEHLART_WASSER,
        Resource.KM_RUECKKUEHLART_TROCKENKUEHLER, Resource.KM_RUECKKUEHLART_NASSKUEHLER
    };

    // ------------------------------------------------------------ Katalogfelder (K-A)
    public string LabelGeraeteart { get; set; } = Resource.KM_LBL_GERAETEART;
    public string LabelGwp { get; set; } = Resource.KM_LBL_GWP;
    public string LabelFuellmenge { get; set; } = Resource.KM_LBL_FUELLMENGE;
    public string LabelSaisonArt { get; set; } = Resource.KM_LBL_SAISON_ART;
    public string LabelSaisonkennzahl { get; set; } = Resource.KM_LBL_SAISONKENNZAHL;
    public string EinheitKg { get; set; } = Resource.KM_EINHEIT_KG;
    /// <summary>Platzhalter der Geräteart: leer = nach der Rückkühlart.</summary>
    public string GeraeteartAusRueckkuehlung { get; set; } = Resource.KM_GERAETEART_AUS_RUECKKUEHLUNG;
    /// <summary>Platzhalter der Art der saisonalen Kennzahl: keine Angabe.</summary>
    public string SaisonArtKeine { get; set; } = Resource.KM_SAISON_ART_KEINE;
    /// <summary>Die Gerätearten in der Folge von <c>KaelteKatalogfelderSchema.GERAETEARTEN</c>.</summary>
    public IReadOnlyList<string> Geraetearten { get; set; } = new[]
    {
        Resource.KM_GERAETEART_KWS_LUFT, Resource.KM_GERAETEART_KWS_WASSER, Resource.KM_GERAETEART_KWS_FREIKUEHLUNG,
        Resource.KM_GERAETEART_SPLIT, Resource.KM_GERAETEART_MULTISPLIT, Resource.KM_GERAETEART_VRF,
        Resource.KM_GERAETEART_ABSORPTION
    };
    /// <summary>Die Arten der saisonalen Kennzahl in der Folge von <c>KaelteKatalogfelderSchema.SAISON_ARTEN</c>.</summary>
    public IReadOnlyList<string> SaisonArten { get; set; } = new[] { Resource.KM_SAISON_SEER, Resource.KM_SAISON_ETA_S_C };

    // ------------------------------------------------------------ Kennlinie
    public string SpalteRueckkuehltemperatur { get; set; } = Resource.KM_SP_RUECKKUEHLTEMPERATUR;
    public string SpalteKaltwassertemperatur { get; set; } = Resource.KM_SP_KALTWASSERTEMPERATUR;
    public string SpalteEer { get; set; } = Resource.KM_SP_EER;
    public string SpalteKaelteleistung { get; set; } = Resource.KM_SP_KAELTELEISTUNG;
    public string KennlinieRaster { get; set; } = Resource.KM_KENNLINIE_RASTER;
    public string Punkt { get; set; } = Resource.KM_KENNLINIE_PUNKT;
    public string PunktNeu { get; set; } = Resource.KM_KENNLINIE_NEU;
    public string KennlinieLeer { get; set; } = Resource.KM_KENNLINIE_LEER;
    public string PunktEntfernen { get; set; } = Resource.KM_KENNLINIE_ENTFERNEN;
    public string KennlinieAnzahl { get; set; } = Resource.KM_KENNLINIE_ANZAHL;

    // ------------------------------------------------------------ Teillast und Takten (KM3-E3-a)
    /// <summary>Die Gruppe „Teillast und Takten" — derselbe Text wie bei BHKW und Heizkessel.</summary>
    public string GruppeTeillast { get; set; } = Resource.BHKWK_GRP_TEILLAST;
    public string LabelTeillastWeg { get; set; } = Resource.KM_LBL_TEILLAST_WEG;
    public string LabelVerdichterregelung { get; set; } = Resource.KM_LBL_VERDICHTERREGELUNG;
    public string LabelKurveA { get; set; } = Resource.KM_LBL_TEILLASTKURVE_A;
    public string LabelKurveB { get; set; } = Resource.KM_LBL_TEILLASTKURVE_B;
    public string LabelKurveC { get; set; } = Resource.KM_LBL_TEILLASTKURVE_C;
    public string LabelKurveLastgradMin { get; set; } = Resource.KM_LBL_TEILLASTKURVE_LASTGRAD_MIN;
    public string LabelCd { get; set; } = Resource.KM_LBL_TAKTVERLUST_CD;
    public string LabelRandweg { get; set; } = Resource.KM_LBL_KENNFELD_RANDWEG;
    /// <summary>Platzhalter der Teillastrechnung: leer = wie bisher.</summary>
    public string TeillastWegBestand { get; set; } = Resource.KM_TEILLAST_WEG_BESTAND;
    /// <summary>Die Teillastwege in der Folge von <c>KaeltemaschineTeillastSchema.TEILLAST_WEGE</c>.</summary>
    public IReadOnlyList<string> TeillastWege { get; set; } = new[] { Resource.KM_TEILLAST_WEG_LINEAR, Resource.KM_TEILLAST_WEG_KURVE };
    public string RegelungKeine { get; set; } = Resource.KM_REGELUNG_KEINE;
    /// <summary>Die Verdichterregelungen in der Folge von <c>KaeltemaschineTeillastSchema.VERDICHTERREGELUNGEN</c>.</summary>
    public IReadOnlyList<string> Verdichterregelungen { get; set; } = new[]
    {
        Resource.KM_REGELUNG_EIN_AUS, Resource.KM_REGELUNG_STUFEN, Resource.KM_REGELUNG_DREHZAHL
    };
    /// <summary>Die Wege am Kennfeldrand in der Folge von <c>KaeltemaschineTeillastSchema.RANDWEGE</c>.</summary>
    public IReadOnlyList<string> Randwege { get; set; } = new[] { Resource.KM_RANDWEG_RANDWERT, Resource.KM_RANDWEG_GUETEGRAD };
    public string PlatzhalterVorgabe { get; set; } = Resource.KM_PH_VORGABE;
    public string PlatzhalterCd { get; set; } = Resource.KM_PH_CD_VORGABE;
    public string PlatzhalterRandweg { get; set; } = Resource.KM_PH_RANDWEG_VORGABE;
    /// <summary>„g(0,25) = {0} · g(0,5) = {1} · g(0,75) = {2}".</summary>
    public string Lesezeile { get; set; } = Resource.KM_TT_LESEZEILE;
    public string VerweisMindestteillast { get; set; } = Resource.KM_TT_VERWEIS_MINDESTTEILLAST;
    public string BildTeillast { get; set; } = Resource.KM_BILD_TEILLAST;
    /// <summary>KD-3: Bezeichnung der Reiterleiste der Kennlinienbilder (KM_KL_BEZEICHNUNG).</summary>
    public string KennlinienbildBezeichnung { get; set; } = Resource.KM_KL_BEZEICHNUNG;
    /// <summary>KD-3: Reiter „EER" (KM_KL_REITER_EER).</summary>
    public string ReiterEer { get; set; } = Resource.KM_KL_REITER_EER;
    /// <summary>KD-3: Reiter „Kälteleistung" (KM_KL_REITER_LEISTUNG).</summary>
    public string ReiterLeistung { get; set; } = Resource.KM_KL_REITER_LEISTUNG;
    /// <summary>KD-3: Platzhalter ohne vollständigen Kennlinienpunkt (KM_KL_PLATZHALTER).</summary>
    public string PlatzhalterKennlinienbild { get; set; } = Resource.KM_KL_PLATZHALTER;
    public string KnopfKurveTypkennfeld { get; set; } = Resource.KM_BTN_KURVE_TYPKENNFELD;
    public string KnopfSkalieren { get; set; } = Resource.KM_BTN_SKALIEREN;
    public string KnopfTeillastpunkte { get; set; } = Resource.KM_BTN_TEILLASTPUNKTE;
    public string LabelTypkennfeld { get; set; } = Resource.KM_LBL_TYPKENNFELD;
    public string TypkennfeldWaehlen { get; set; } = Resource.KM_TYPKENNFELD_WAEHLEN;
    public string MeldungTypkennfeldWaehlen { get; set; } = Resource.KM_MSG_TYPKENNFELD_WAEHLEN;
    /// <summary>„Teillastkurve aus „{0}" übernommen."</summary>
    public string KurveUebernommen { get; set; } = Resource.KM_MSG_KURVE_UEBERNOMMEN;
    public string SkalierenHinweis { get; set; } = Resource.KM_SKAL_HINWEIS;
    public string AuskunftHinweis { get; set; } = Resource.KM_AUSK_HINWEIS;
    /// <summary>„Punkt {0}".</summary>
    public string AuskunftPunkt { get; set; } = Resource.KM_AUSK_PUNKT;
    public string AuskunftAussen { get; set; } = Resource.KM_AUSK_AUSSEN;
    public string AuskunftLastgrad { get; set; } = Resource.KM_AUSK_LASTGRAD;
    public string AuskunftRechnen { get; set; } = Resource.KM_AUSK_RECHNEN;
    public string AuskunftRueckkuehl { get; set; } = Resource.KM_AUSK_SP_RUECKKUEHL;
    public string AuskunftKaelte { get; set; } = Resource.KM_AUSK_SP_KAELTE;
    public string AuskunftLastgradMaschine { get; set; } = Resource.KM_AUSK_SP_LASTGRAD_MASCHINE;
    public string AuskunftLeistungsaufnahme { get; set; } = Resource.KM_AUSK_SP_LEISTUNGSAUFNAHME;
    public string AuskunftTakt { get; set; } = Resource.KM_AUSK_TAKT;
    public string AuskunftRand { get; set; } = Resource.KM_AUSK_RAND;

    // ------------------------------------------------------------ Einheiten (Symbole)
    public string EinheitKw { get; set; } = "kW";
    public string EinheitProzent { get; set; } = "%";
    public string EinheitGradC { get; set; } = "°C";
    public string EinheitEuro { get; set; } = "€";

    // ------------------------------------------------------------ Knöpfe
    public string KnopfSpeichern { get; set; } = Resource.ADM_BTN_SPEICHERN;
    public string KnopfVerwerfen { get; set; } = Resource.ADM_BTN_VERWERFEN;
    public string KnopfNeu { get; set; } = Resource.ADM_BTN_NEU;
    /// <summary>„Import…" (KM1).</summary>
    public string KnopfImport { get; set; } = Resource.ADM_BTN_IMPORT;
    /// <summary>„Typkennfelder laden…" (KM1).</summary>
    public string KnopfTypkennfelder { get; set; } = Resource.KM_BTN_TYPKENNFELDER;
    /// <summary>Rückfrage vor dem Laden; {0} = Zahl der Typkennfelder.</summary>
    public string FrageTypkennfelder { get; set; } = Resource.KM_FRAGE_TYPKENNFELDER;
    /// <summary>Ergebnis des Ladens; {0} = neu, {1} = übersprungen.</summary>
    public string TypkennfelderGeladen { get; set; } = Resource.KM_MSG_TYPKENNFELDER;
    /// <summary>Die benannte Ablehnung des Dateiimports ohne Importweg (iOS).</summary>
    public string ImportPlattform { get; set; } = Resource.KM_MSG_IMPORT_PLATTFORM;
    public string KnopfBeenden { get; set; } = Resource.ADM_BTN_BEENDEN;
    public string KnopfDuplizieren { get; set; } = Resource.ADM_BTN_DUPLIZIEREN;
    public string KnopfLoeschen { get; set; } = Resource.BST_BTN_LOESCHEN;
    public string Ok { get; set; } = Resource.ALLG_BTN_OK;
    public string Abbrechen { get; set; } = Resource.ALLG_BTN_ABBRECHEN;
    public string Ja { get; set; } = Resource.ALLG_BTN_JA;
    public string Nein { get; set; } = Resource.ALLG_BTN_NEIN;

    // ------------------------------------------------------------ Meldungen
    public string TitelNeu { get; set; } = Resource.KM_TITEL_NEU;
    public string FrageLoeschen { get; set; } = Resource.KM_FRAGE_LOESCHEN;
    public string Angelegt { get; set; } = Resource.KM_MSG_ANGELEGT;
    public string Geloescht { get; set; } = Resource.KM_MSG_GELOESCHT;
    public string Fehler { get; set; } = Resource.KM_MSG_FEHLER;
    public string SpeichernGesperrt { get; set; } = Resource.ADM_SPEICHERN_GESPERRT;
    public string NameBelegt { get; set; } = Resource.KM_MSG_NAME_BELEGT;
    public string NameLeer { get; set; } = Resource.KM_MSG_NAME_LEER;
}
