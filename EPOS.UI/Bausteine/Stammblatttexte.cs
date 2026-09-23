using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Eine Kennzahl im Kopf des Stammblatts</b> (V9): Wert samt Einheit und ihr Name
/// darunter („50 kW" / „Nennleistung").
/// </summary>
public sealed record Stammblattkennzahl(string Wert, string Name);

/// <summary>
/// <b>Die Beschriftungen des Stammblatts</b> (Konzept Administrationsdialoge, Stufe 3,
/// V9 und V12) — EIN Bündel (Hausregel ab etwa zehn Anzeigetexten). Sie füllen sich
/// selbst aus <c>MyResource</c>; ein Wirt überschreibt nur, was er anders nennt.
/// </summary>
public sealed class Stammblatttexte
{
    /// <summary>Name des Bereichs für die Sprachausgabe — <c>ADM_SB_TITEL</c>.</summary>
    public string Beschriftung { get; set; } = Resource.ADM_SB_TITEL;

    /// <summary>Ohne Fokuszeile — <c>ADM_SB_LEER</c>.</summary>
    public string Leer { get; set; } = Resource.ADM_SB_LEER;

    /// <summary>Zurück zur Liste im schmalen Fenster — <c>ADM_SB_LISTE</c>.</summary>
    public string ZurListe { get; set; } = Resource.ADM_SB_LISTE;

    /// <summary>Gruppe „Kenndaten" — <c>ADM_SB_KENNDATEN</c>.</summary>
    public string Kenndaten { get; set; } = Resource.ADM_SB_KENNDATEN;

    /// <summary>Gruppe „Kosten" — <c>ADM_SB_KOSTEN</c>.</summary>
    public string Kosten { get; set; } = Resource.ADM_SB_KOSTEN;

    /// <summary>
    /// Der Hinweis im Kopf eines Auslieferungssatzes (V13) — <c>ADM_SB_NUR_LESEN</c>.
    /// Der Kurztext am Schloss erreicht eine Berührung nicht; hier steht er in Worten.
    /// </summary>
    public string NurLesen { get; set; } = Resource.ADM_SB_NUR_LESEN;

    /// <summary>Kopf des Vergleichs — <c>KFLT_VERGLEICH_TITEL</c>.</summary>
    public string VergleichTitel { get; set; } = Resource.KFLT_VERGLEICH_TITEL;

    /// <summary>„{0} Sätze" unter dem Kopf des Vergleichs — <c>ADM_VG_SAETZE</c>.</summary>
    public string VergleichSaetze { get; set; } = Resource.ADM_VG_SAETZE;

    /// <summary>Der Fuß des Vergleichs: nur lesbar — <c>ADM_VG_NUR_LESEN</c>.</summary>
    public string VergleichNurLesen { get; set; } = Resource.ADM_VG_NUR_LESEN;

    /// <summary>„‹ Stammblatt von {0}" — <c>ADM_VG_ZURUECK</c>.</summary>
    public string VergleichZurueck { get; set; } = Resource.ADM_VG_ZURUECK;

    /// <summary>Spaltenkopf der Parameter — <c>KFLT_VERGLEICH_SP_PARAMETER</c>.</summary>
    public string SpalteParameter { get; set; } = Resource.KFLT_VERGLEICH_SP_PARAMETER;

    /// <summary>„abweichend" — <c>KFLT_VERGLEICH_ABWEICHEND</c>.</summary>
    public string Abweichend { get; set; } = Resource.KFLT_VERGLEICH_ABWEICHEND;

    /// <summary>„stimmig" — <c>KFLT_VERGLEICH_STIMMIG</c>.</summary>
    public string Stimmig { get; set; } = Resource.KFLT_VERGLEICH_STIMMIG;

    /// <summary>Schalter „nur abweichende Werte" — <c>ADM_VG_NUR_ABWEICHENDE</c>.</summary>
    public string NurAbweichende { get; set; } = Resource.ADM_VG_NUR_ABWEICHENDE;

    /// <summary>Wenn der Schalter nichts übrig lässt — <c>ADM_VG_KEINE_ABWEICHUNG</c>.</summary>
    public string KeineAbweichung { get; set; } = Resource.ADM_VG_KEINE_ABWEICHUNG;
}
