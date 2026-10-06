using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Beschriftungen der Gruppe „Aufheizung" im Bedarfsdialog (Entwurf KP3, Welle O2) — ein BÜNDEL nach der
/// Hausregel; jede Eigenschaft nennt ihren Ressourcenschlüssel. Zustände, Quellen und Hinweise formuliert die Hülle.
/// </summary>
public sealed class GebaeudeBedarfAufheizTexte
{
    /// <summary><c>GEBB_AUFH_GRP</c></summary>
    public string Gruppe { get; set; } = Resource.GEBB_AUFH_GRP;

    /// <summary><c>GEBB_AUFH_LBL_ZUSTAND</c></summary>
    public string LabelZustand { get; set; } = Resource.GEBB_AUFH_LBL_ZUSTAND;

    /// <summary><c>GEBB_AUFH_LBL_ZEIT_MAX</c></summary>
    public string LabelZeitMax { get; set; } = Resource.GEBB_AUFH_LBL_ZEIT_MAX;

    /// <summary><c>GEBB_AUFH_ZEIT_BEI</c> — {0} t_auf,max [h], {1} T_a,B [°C], {2} Variante.</summary>
    public string ZeitBei { get; set; } = Resource.GEBB_AUFH_ZEIT_BEI;

    /// <summary><c>GEBB_AUFH_LBL_ART</c></summary>
    public string LabelArt { get; set; } = Resource.GEBB_AUFH_LBL_ART;

    /// <summary><c>GEBB_AUFH_LBL_LEISTUNG</c></summary>
    public string LabelLeistung { get; set; } = Resource.GEBB_AUFH_LBL_LEISTUNG;

    /// <summary><c>GEBB_AUFH_LBL_QUELLE</c></summary>
    public string LabelQuelle { get; set; } = Resource.GEBB_AUFH_LBL_QUELLE;

    /// <summary><c>GEBB_AUFH_LBL_TAGE</c></summary>
    public string LabelTage { get; set; } = Resource.GEBB_AUFH_LBL_TAGE;

    /// <summary><c>GEBB_AUFH_LBL_STUNDEN</c></summary>
    public string LabelStunden { get; set; } = Resource.GEBB_AUFH_LBL_STUNDEN;

    /// <summary><c>GEBB_AUFH_LBL_LAENGSTE</c></summary>
    public string LabelLaengste { get; set; } = Resource.GEBB_AUFH_LBL_LAENGSTE;

    /// <summary><c>GEBB_AUFH_LBL_BEGRENZT</c> (W2)</summary>
    public string LabelBegrenzt { get; set; } = Resource.GEBB_AUFH_LBL_BEGRENZT;

    /// <summary><c>GEBB_AUFH_LBL_UNERREICHBAR</c> (W1)</summary>
    public string LabelUnerreichbar { get; set; } = Resource.GEBB_AUFH_LBL_UNERREICHBAR;

    /// <summary><c>GEBB_AUFH_LBL_NACHWEISBAND</c> (W3)</summary>
    public string LabelNachweisband { get; set; } = Resource.GEBB_AUFH_LBL_NACHWEISBAND;

    /// <summary><c>GEBB_AUFH_LBL_SPRUENGE_AUS</c> (W4)</summary>
    public string LabelSpruengeAus { get; set; } = Resource.GEBB_AUFH_LBL_SPRUENGE_AUS;

    /// <summary><c>GEBB_AUFH_LBL_KAPPUNG</c></summary>
    public string LabelKappung { get; set; } = Resource.GEBB_AUFH_LBL_KAPPUNG;

    /// <summary><c>GEBB_AUFH_LBL_AUSLEGUNG</c> — die Auslegungsgröße Φ_HL + Φ_RH (E60).</summary>
    public string LabelAuslegung { get; set; } = Resource.GEBB_AUFH_LBL_AUSLEGUNG;

    /// <summary><c>GEBB_AUFH_LBL_AUSLEGUNGSHEIZLAST</c></summary>
    public string LabelAuslegungsheizlast { get; set; } = Resource.GEBB_AUFH_LBL_AUSLEGUNGSHEIZLAST;

    /// <summary><c>GEBB_AUFH_LBL_ZUSCHLAG</c></summary>
    public string LabelZuschlag { get; set; } = Resource.GEBB_AUFH_LBL_ZUSCHLAG;

    /// <summary><c>GEBB_AUFH_LBL_SPITZE_IDEAL</c></summary>
    public string LabelSpitzeIdeal { get; set; } = Resource.GEBB_AUFH_LBL_SPITZE_IDEAL;

    /// <summary><c>GEBB_LBL_SPITZE_TAGESMITTEL</c> — dieselbe Beschriftung wie in der Vergleichstafel.</summary>
    public string LabelTagesmittel { get; set; } = Resource.GEBB_LBL_SPITZE_TAGESMITTEL;

    /// <summary><c>GEBB_AUFH_HRL_SPITZE</c> — die ideale Spitze ist bei Schalter aus keine Auslegungsgröße (Festlegung 41).</summary>
    public string HinweisSpitze { get; set; } = Resource.GEBB_AUFH_HRL_SPITZE;

    /// <summary><c>GEBB_AUFH_HRL_AUSKUNFT</c> — bei Schalter aus stammen die Teile aus der Auskunft.</summary>
    public string HinweisAuskunft { get; set; } = Resource.GEBB_AUFH_HRL_AUSKUNFT;

    /// <summary><c>GEBB_AUFH_HRL_FAKTOR_LAUF</c> — Verbrauchsangabe: der Faktor entsteht erst im Lauf.</summary>
    public string HinweisFaktorLauf { get; set; } = Resource.GEBB_AUFH_HRL_FAKTOR_LAUF;

    /// <summary><c>GEBB_AUFH_HRL_MANUELL</c> — {0} die manuelle Aufheizzeit [h].</summary>
    public string HinweisManuell { get; set; } = Resource.GEBB_AUFH_HRL_MANUELL;

    /// <summary><c>GEBZ_SP_ZONE</c></summary>
    public string SpalteZone { get; set; } = Resource.GEBZ_SP_ZONE;

    /// <summary><c>GEBB_AUFH_SP_ZUSTAND</c></summary>
    public string SpalteZustand { get; set; } = Resource.GEBB_AUFH_SP_ZUSTAND;

    /// <summary><c>GEBB_AUFH_SP_ZEIT</c></summary>
    public string SpalteZeit { get; set; } = Resource.GEBB_AUFH_SP_ZEIT;

    /// <summary><c>GEBB_AUFH_SP_LEISTUNG</c></summary>
    public string SpalteLeistung { get; set; } = Resource.GEBB_AUFH_SP_LEISTUNG;

    /// <summary><c>GEBB_AUFH_SP_QUELLE</c></summary>
    public string SpalteQuelle { get; set; } = Resource.GEBB_AUFH_SP_QUELLE;

    /// <summary><c>GEBB_AUFH_SP_TAGE</c></summary>
    public string SpalteTage { get; set; } = Resource.GEBB_AUFH_SP_TAGE;

    /// <summary><c>GEBB_AUFH_SP_LAENGSTE</c></summary>
    public string SpalteLaengste { get; set; } = Resource.GEBB_AUFH_SP_LAENGSTE;

    /// <summary><c>GEBB_AUFH_SP_KAPPUNG</c></summary>
    public string SpalteKappung { get; set; } = Resource.GEBB_AUFH_SP_KAPPUNG;

    /// <summary><c>GEBB_AUFH_HRL_ZONEN</c> — wie die Gebäudewerte aus den Zonen entstehen (Festlegung 22).</summary>
    public string HinweisZonen { get; set; } = Resource.GEBB_AUFH_HRL_ZONEN;

    /// <summary><c>GEBB_EINHEIT_H</c></summary>
    public string EinheitH { get; set; } = Resource.GEBB_EINHEIT_H;
}
