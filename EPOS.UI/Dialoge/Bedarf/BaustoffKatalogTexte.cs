using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Anzeigetexte der Verwaltung „Baustoffe" (<see cref="BaustoffKatalogDialog"/>) — EIN
/// Parameter statt vieler (Hausregel „ab etwa zehn Anzeigetexten ein Bündel").
/// </summary>
/// <remarks>
/// Beschriftungen, kein Zustand. Jede Eigenschaft füllt sich selbst aus <c>MyResource</c> und
/// nennt ihren Schlüssel im Kommentar; die neutrale Ressource ist der deutsche Rückfall.
/// Die gemeinsamen Texte der Verwaltungen (<c>ADM_*</c>, <c>ALLG_*</c>) nimmt das Bündel mit,
/// damit ein Wirt oder eine Probe sie an EINER Stelle übersteuern kann.
/// </remarks>
public sealed class BaustoffKatalogTexte
{
    // ------------------------------------------------------------ Kopf, Liste, Stammblatt

    /// <summary><c>BST_TITEL</c></summary>
    public string Titel { get; set; } = Resource.BST_TITEL;

    /// <summary><c>BST_LEER</c> — die leise Zeile über einer leeren Liste.</summary>
    public string LeerKatalog { get; set; } = Resource.BST_LEER;

    /// <summary><c>BST_SCHALTER_NEUTRAL</c> — der Werkzeugschalter „nur herstellerneutral".</summary>
    public string SchalterNeutral { get; set; } = Resource.BST_SCHALTER_NEUTRAL;

    /// <summary><c>BST_NEUTRAL</c> — die Unterzeile eines Stoffes ohne Hersteller.</summary>
    public string Herstellerneutral { get; set; } = Resource.BST_NEUTRAL;

    /// <summary><c>ADM_SB_KENNDATEN</c></summary>
    public string GruppeKenndaten { get; set; } = Resource.ADM_SB_KENNDATEN;

    /// <summary><c>ADM_SB_HERKUNFT</c></summary>
    public string GruppeHerkunft { get; set; } = Resource.ADM_SB_HERKUNFT;

    // ------------------------------------------------------------ Beschriftungen

    /// <summary><c>BST_LBL_BEZEICHNER</c></summary>
    public string LabelBezeichner { get; set; } = Resource.BST_LBL_BEZEICHNER;

    /// <summary><c>BST_LBL_GRUPPE</c></summary>
    public string LabelGruppe { get; set; } = Resource.BST_LBL_GRUPPE;

    /// <summary><c>BST_LBL_HERSTELLER</c></summary>
    public string LabelHersteller { get; set; } = Resource.BST_LBL_HERSTELLER;

    /// <summary><c>BST_LBL_LAMBDA</c></summary>
    public string LabelLambda { get; set; } = Resource.BST_LBL_LAMBDA;

    /// <summary><c>BST_LBL_RHO</c></summary>
    public string LabelRho { get; set; } = Resource.BST_LBL_RHO;

    /// <summary><c>BST_LBL_CP</c></summary>
    public string LabelCp { get; set; } = Resource.BST_LBL_CP;

    /// <summary><c>BST_LBL_QUELLE</c></summary>
    public string LabelQuelle { get; set; } = Resource.BST_LBL_QUELLE;

    /// <summary><c>BST_LBL_HERKUNFT</c></summary>
    public string LabelHerkunft { get; set; } = Resource.BST_LBL_HERKUNFT;

    /// <summary><c>BST_LBL_QUELLKENNUNG</c></summary>
    public string LabelQuellkennung { get; set; } = Resource.BST_LBL_QUELLKENNUNG;

    /// <summary><c>BST_LBL_VERWENDUNG</c></summary>
    public string LabelVerwendung { get; set; } = Resource.BST_LBL_VERWENDUNG;

    /// <summary><c>BST_EINHEIT_LAMBDA</c> — W/(mK).</summary>
    public string EinheitLambda { get; set; } = Resource.BST_EINHEIT_LAMBDA;

    /// <summary><c>BST_EINHEIT_RHO</c> — kg/m³.</summary>
    public string EinheitRho { get; set; } = Resource.BST_EINHEIT_RHO;

    /// <summary><c>BST_EINHEIT_CP</c> — J/(kgK).</summary>
    public string EinheitCp { get; set; } = Resource.BST_EINHEIT_CP;

    /// <summary><c>BST_VERWENDUNG</c> — „in {0} Schicht(en) des Aufbaukatalogs".</summary>
    public string Verwendung { get; set; } = Resource.BST_VERWENDUNG;

    /// <summary><c>BST_VERWENDUNG_KEINE</c></summary>
    public string VerwendungKeine { get; set; } = Resource.BST_VERWENDUNG_KEINE;

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

    /// <summary><c>BST_BTN_LOESCHEN</c></summary>
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

    /// <summary><c>BST_TITEL_NEU</c> — Titel der Überlagerung „Neu…".</summary>
    public string TitelNeu { get; set; } = Resource.BST_TITEL_NEU;

    /// <summary><c>BST_FRAGE_LOESCHEN</c> — „Soll der Baustoff „{0}" gelöscht werden?".</summary>
    public string FrageLoeschen { get; set; } = Resource.BST_FRAGE_LOESCHEN;

    /// <summary><c>BST_AW_LOESCHEN_VERWENDET</c> — der Sperrgrund „in {0} Schicht(en) verwendet".</summary>
    public string LoeschenVerwendet { get; set; } = Resource.BST_AW_LOESCHEN_VERWENDET;

    /// <summary><c>BST_LOESCHEN_BLEIBEN_VERWENDET</c> — „Stehen bleiben (in Schichten verwendet): {0}".</summary>
    public string LoeschenBleibenVerwendet { get; set; } = Resource.BST_LOESCHEN_BLEIBEN_VERWENDET;

    /// <summary><c>BST_MSG_ANGELEGT</c> — „„{0}" angelegt."</summary>
    public string Angelegt { get; set; } = Resource.BST_MSG_ANGELEGT;

    /// <summary><c>BST_MSG_GELOESCHT</c> — „„{0}" gelöscht."</summary>
    public string Geloescht { get; set; } = Resource.BST_MSG_GELOESCHT;

    /// <summary><c>BST_MSG_FEHLER</c> — der Rückfall, wenn der Kern keine Meldung liefert.</summary>
    public string Fehler { get; set; } = Resource.BST_MSG_FEHLER;

    /// <summary><c>ADM_SPEICHERN_GESPERRT</c> — der Grund am weich gesperrten „Speichern".</summary>
    public string SpeichernGesperrt { get; set; } = Resource.ADM_SPEICHERN_GESPERRT;

    /// <summary><c>BST_MSG_NAME_BELEGT</c> — Namensabfrage von „Duplizieren…".</summary>
    public string NameBelegt { get; set; } = Resource.BST_MSG_NAME_BELEGT;

    /// <summary><c>BAUSTOFF_MSG_NAME_LEER</c></summary>
    public string NameLeer { get; set; } = Resource.BAUSTOFF_MSG_NAME_LEER;
}
