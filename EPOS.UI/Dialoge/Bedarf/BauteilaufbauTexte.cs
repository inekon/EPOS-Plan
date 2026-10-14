using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Anzeigetexte der Verwaltung „Bauteilaufbauten" (<see cref="BauteilaufbauDialog"/>) — EIN
/// Parameter statt vieler; das Schichtenraster bringt sein eigenes Bündel mit
/// (<see cref="Schichten"/>). Jede Eigenschaft füllt sich selbst aus <c>MyResource</c> und nennt
/// ihren Schlüssel; die neutrale Ressource ist der deutsche Rückfall.
/// </summary>
public sealed class BauteilaufbauTexte
{
    // ------------------------------------------------------------ Kopf, Liste, Stammblatt

    /// <summary><c>BTA_TITEL</c></summary>
    public string Titel { get; set; } = Resource.BTA_TITEL;

    /// <summary><c>BTA_LEER</c> — die leise Zeile über einer leeren Liste.</summary>
    public string LeerKatalog { get; set; } = Resource.BTA_LEER;

    /// <summary><c>BTA_TITEL_NEU</c> — Titel der Überlagerung „Neu…".</summary>
    public string TitelNeu { get; set; } = Resource.BTA_TITEL_NEU;

    /// <summary><c>ADM_SB_KENNDATEN</c></summary>
    public string GruppeKenndaten { get; set; } = Resource.ADM_SB_KENNDATEN;

    /// <summary><c>BTA_GRP_SCHICHTEN</c></summary>
    public string GruppeSchichten { get; set; } = Resource.BTA_GRP_SCHICHTEN;

    /// <summary><c>ADM_SB_HERKUNFT</c></summary>
    public string GruppeHerkunft { get; set; } = Resource.ADM_SB_HERKUNFT;

    /// <summary><c>BTA_SCHICHTEN_ZAHL</c> — „{0} Schichten" in der Unterzeile.</summary>
    public string SchichtenZahl { get; set; } = Resource.BTA_SCHICHTEN_ZAHL;

    /// <summary><c>BTA_SCHICHTEN_EINS</c> — „1 Schicht".</summary>
    public string SchichtenEins { get; set; } = Resource.BTA_SCHICHTEN_EINS;

    // ------------------------------------------------------------ Beschriftungen

    /// <summary><c>BTA_LBL_BEZEICHNER</c></summary>
    public string LabelBezeichner { get; set; } = Resource.BTA_LBL_BEZEICHNER;

    /// <summary><c>BTA_LBL_BAUTEILART</c></summary>
    public string LabelBauteilart { get; set; } = Resource.BTA_LBL_BAUTEILART;

    /// <summary><c>BTA_LBL_BESCHREIBUNG</c></summary>
    public string LabelBeschreibung { get; set; } = Resource.BTA_LBL_BESCHREIBUNG;

    /// <summary><c>BTA_LBL_QUELLE</c></summary>
    public string LabelQuelle { get; set; } = Resource.BTA_LBL_QUELLE;

    /// <summary><c>BKAT_LBL_GRUPPE</c> — die Gruppe des Aufbaus (BK-2b).</summary>
    public string LabelGruppe { get; set; } = Resource.BKAT_LBL_GRUPPE;

    /// <summary><c>BKAT_GRUPPE_KEINE</c> — die leere Zeile der Gruppenwahl (Gruppe aus der Bauteilart).</summary>
    public string GruppeKeine { get; set; } = Resource.BKAT_GRUPPE_KEINE;

    /// <summary><c>BKAT_LBL_KLASSE</c></summary>
    public string LabelKlasse { get; set; } = Resource.BKAT_LBL_KLASSE;

    /// <summary><c>BKAT_KLASSE_KEINE</c></summary>
    public string KlasseKeine { get; set; } = Resource.BKAT_KLASSE_KEINE;

    /// <summary><c>BKAT_LBL_EIGNUNG</c> — Beschriftung des Lesewerts der Eignung.</summary>
    public string LabelEignung { get; set; } = Resource.BKAT_LBL_EIGNUNG;

    /// <summary><c>BKAT_GEEIGNET</c></summary>
    public string Geeignet { get; set; } = Resource.BKAT_GEEIGNET;

    /// <summary><c>BKAT_UNGEEIGNET</c> — „für die Gebäudesimulation meist nicht geeignet".</summary>
    public string Ungeeignet { get; set; } = Resource.BKAT_UNGEEIGNET;

    /// <summary><c>BKAT_SCHALTER_NUR_GEEIGNETE</c> — der Schalter über der Liste.</summary>
    public string SchalterNurGeeignete { get; set; } = Resource.BKAT_SCHALTER_NUR_GEEIGNETE;

    /// <summary><c>BKAT_HERKUNFT_IMPORT</c> — die Marke „Import" im Stammblattkopf.</summary>
    public string MarkeImport { get; set; } = Resource.BKAT_HERKUNFT_IMPORT;

    /// <summary><c>BKAT_IMPORT_QUELLE</c> — die Herleitung unter der festen Quelle eines Importsatzes.</summary>
    public string ImportQuelle { get; set; } = Resource.BKAT_IMPORT_QUELLE;

    /// <summary><c>BTA_LBL_HERKUNFT</c></summary>
    public string LabelHerkunft { get; set; } = Resource.BTA_LBL_HERKUNFT;

    /// <summary><c>BTA_LBL_QUELLKENNUNG</c></summary>
    public string LabelQuellkennung { get; set; } = Resource.BTA_LBL_QUELLKENNUNG;

    /// <summary><c>BTA_ART_JEDE</c> — die Wahl „für jede Bauteilart" ohne Hülle.</summary>
    public string BauteilartJede { get; set; } = Resource.BTA_ART_JEDE;

    /// <summary><c>BTA_KZ_U</c></summary>
    public string KennzahlU { get; set; } = Resource.BTA_KZ_U;

    /// <summary><c>BTA_KZ_R</c></summary>
    public string KennzahlR { get; set; } = Resource.BTA_KZ_R;

    /// <summary><c>BTA_KZ_C</c> — die flächenbezogene Kapazität im Stammblattkopf.</summary>
    public string KennzahlC { get; set; } = Resource.BTA_KZ_C;

    /// <summary><c>BTA_EINHEIT_U</c> — W/(m²K).</summary>
    public string EinheitU { get; set; } = Resource.BTA_EINHEIT_U;

    /// <summary><c>BTA_EINHEIT_R</c> — m²K/W.</summary>
    public string EinheitR { get; set; } = Resource.BTA_EINHEIT_R;

    /// <summary><c>BTA_EINHEIT_KAPAZITAET</c> — kJ/(m²K).</summary>
    public string EinheitKapazitaet { get; set; } = Resource.BTA_EINHEIT_KAPAZITAET;

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

    /// <summary><c>BTA_BTN_LOESCHEN</c></summary>
    public string KnopfLoeschen { get; set; } = Resource.BTA_BTN_LOESCHEN;

    /// <summary><c>ALLG_BTN_OK</c></summary>
    public string Ok { get; set; } = Resource.ALLG_BTN_OK;

    /// <summary><c>ALLG_BTN_ABBRECHEN</c></summary>
    public string Abbrechen { get; set; } = Resource.ALLG_BTN_ABBRECHEN;

    /// <summary><c>ALLG_BTN_JA</c></summary>
    public string Ja { get; set; } = Resource.ALLG_BTN_JA;

    /// <summary><c>ALLG_BTN_NEIN</c></summary>
    public string Nein { get; set; } = Resource.ALLG_BTN_NEIN;

    // ------------------------------------------------------------ Meldungen

    /// <summary><c>BTA_FRAGE_LOESCHEN</c> — „Soll der Bauteilaufbau „{0}" gelöscht werden?".</summary>
    public string FrageLoeschen { get; set; } = Resource.BTA_FRAGE_LOESCHEN;

    /// <summary><c>BTA_MSG_ANGELEGT</c></summary>
    public string Angelegt { get; set; } = Resource.BTA_MSG_ANGELEGT;

    /// <summary><c>BTA_MSG_GELOESCHT</c></summary>
    public string Geloescht { get; set; } = Resource.BTA_MSG_GELOESCHT;

    /// <summary><c>BTA_MSG_FEHLER</c> — der Rückfall, wenn der Kern keine Meldung liefert.</summary>
    public string Fehler { get; set; } = Resource.BTA_MSG_FEHLER;

    /// <summary><c>ADM_SPEICHERN_GESPERRT</c> — der Grund am weich gesperrten „Speichern".</summary>
    public string SpeichernGesperrt { get; set; } = Resource.ADM_SPEICHERN_GESPERRT;

    /// <summary><c>BTA_MSG_NAME_BELEGT</c> — Namensabfrage von „Duplizieren…".</summary>
    public string NameBelegt { get; set; } = Resource.BTA_MSG_NAME_BELEGT;

    /// <summary><c>BAUTEIL_MSG_AUFBAU_NAME_LEER</c></summary>
    public string NameLeer { get; set; } = Resource.BAUTEIL_MSG_AUFBAU_NAME_LEER;

    /// <summary>Die Texte des Schichtenrasters samt Summenfuß.</summary>
    public BauteilschichtenTexte Schichten { get; set; } = new();
}
