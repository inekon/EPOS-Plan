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
