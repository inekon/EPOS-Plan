using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Waermepumpe;

/// <summary>
/// Die Beschriftungen des Bausteins <see cref="WaermepumpeStammFelder"/> — des
/// Feldrasters eines Wärmepumpen-STAMMSATZES.
///
/// <para><b>Warum das Raster einen eigenen Baustein bekam.</b> Seit dem
/// Anwenderentscheid vom 16.09.2026 stehen die Parameter des Stammgeräts nicht mehr
/// nur in der Stammdatenpflege, sondern auch in der Detailansicht der Anlage — dort
/// unmittelbar und bearbeitbar, statt hinter dem Knopf „Parameter Bearbeiten…". Zwei
/// Fassungen desselben Rasters wären genau das, was die Hausregel seit iZ5 verbietet;
/// deshalb zeichnen BEIDE Masken denselben Baustein.</para>
///
/// <para>Ein BÜNDEL nach der Bauart <c>PvModellTexte</c>: Es füllt sich SELBST aus
/// <c>MyResource</c>, ein fehlender Schlüssel fällt auf den deutschen Wortlaut zurück.
/// Die Eigenschaften sind SETZBAR — die Stammdatenpflege bezieht ihre Beschriftungen
/// weiterhin als Einzelgaben aus ihrer Hülle und legt sie hier ab, die Detailansicht
/// nimmt das Bündel, wie es sich selbst füllt.</para>
/// </summary>
public sealed class WaermepumpeStammFelderTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    /// <summary>WPS_LBL_NAME</summary>
    public string LabelName { get; set; } = T("WPS_LBL_NAME", "Name");

    /// <summary>WPS_LBL_HERSTELLER</summary>
    public string LabelHersteller { get; set; } = T("WPS_LBL_HERSTELLER", "Hersteller");

    /// <summary>WPS_LBL_BESCHREIBUNG</summary>
    public string LabelBeschreibung { get; set; } = T("WPS_LBL_BESCHREIBUNG", "Beschreibung");

    /// <summary>WPS_LBL_TYP</summary>
    public string LabelTyp { get; set; } = T("WPS_LBL_TYP", "Wärmepumpentyp");

    /// <summary>WPS_LBL_REGELUNG</summary>
    public string LabelRegelung { get; set; } = T("WPS_LBL_REGELUNG", "Leistungsstufen");

    /// <summary>WPS_LBL_AUFSTELLUNG</summary>
    public string LabelAufstellung { get; set; } = T("WPS_LBL_AUFSTELLUNG", "Aufstellung");

    /// <summary>WPS_LBL_BAUJAHR</summary>
    public string LabelBaujahr { get; set; } = T("WPS_LBL_BAUJAHR", "Baujahr");

    /// <summary>WPS_LBL_NENNLEISTUNG</summary>
    public string LabelNennleistung { get; set; } = T("WPS_LBL_NENNLEISTUNG", "Nennleistung");

    /// <summary>WPS_LBL_HEIZSTAB</summary>
    public string LabelHeizstab { get; set; } = T("WPS_LBL_HEIZSTAB", "Heizstab");

    /// <summary>WPS_LBL_KUEHLLEISTUNG</summary>
    public string LabelKuehlleistung { get; set; } = T("WPS_LBL_KUEHLLEISTUNG", "Kühlleistung");

    /// <summary>WPS_LBL_MINDESTLEISTUNG — die kleinste Modulationsleistung (Welle M4, WP1).</summary>
    public string LabelMindestleistung { get; set; } = T("WPS_LBL_MINDESTLEISTUNG", "Mindestleistung");

    /// <summary>WPS_FELD_MINDESTLEISTUNG — der Feldname in Meldungen.</summary>
    public string FeldMindestleistung { get; set; } = T("WPS_FELD_MINDESTLEISTUNG", "Mindestleistung");

    /// <summary>WPS_LBL_TAKTVERLUST_CD — der Teillastkoeffizient nach EN 14825 (Welle M4, WP1).</summary>
    public string LabelTaktverlustCd { get; set; } = T("WPS_LBL_TAKTVERLUST_CD", "Teillastkoeffizient C_d");

    /// <summary>WPS_FELD_TAKTVERLUST_CD — der Feldname in Meldungen.</summary>
    public string FeldTaktverlustCd { get; set; } = T("WPS_FELD_TAKTVERLUST_CD", "Teillastkoeffizient C_d");

    /// <summary>WPS_HINT_TAKTVERLUST_CD — der Hinweis hinter dem Feld.</summary>
    public string HinweisTaktverlustCd { get; set; } = T("WPS_HINT_TAKTVERLUST_CD", "(Vorgabe 0,9 nach EN 14825)");

    /// <summary>WPS_PLATZHALTER_OHNE_TAKT — leere Mindestleistung: keine Taktrechnung.</summary>
    public string PlatzhalterOhneTakt { get; set; } = T("WPS_PLATZHALTER_OHNE_TAKT", "keine Taktrechnung");

    /// <summary>WPS_PLATZHALTER_CD_VORGABE — leeres C_d: die Vorgabe.</summary>
    public string PlatzhalterCdVorgabe { get; set; } = T("WPS_PLATZHALTER_CD_VORGABE", "0,9");

    /// <summary>
    /// MODK_LBL_MODULKOSTEN — derselbe Schlüssel, den der Verwendungskatalog für diese
    /// Spalte führt (<c>ParameterVerwendung.Waermepumpe</c>); Raster und Aufklapper
    /// nennen den Parameter damit wortgleich.
    /// </summary>
    public string LabelModulkosten { get; set; } = T("MODK_LBL_MODULKOSTEN", "Modulkosten");

    /// <summary>WPS_HERL_MODULKOSTEN — die Herleitungszeile unter dem Lesewert (W14a‑O‑1).</summary>
    public string HerleitungModulkosten { get; set; } = T("WPS_HERL_MODULKOSTEN",
        "aus dem Datenbestand; Gerätekosten werden in der Kostenverwaltung gepflegt");

    /// <summary>WPS_HINWEIS_MODULKOSTEN_LEER — die zweite, leise Zeile bei einem Wert von 0.</summary>
    public string HinweisModulkostenLeer { get; set; } = T("WPS_HINWEIS_MODULKOSTEN_LEER",
        "kein Planwert im Datenbestand");

    /// <summary>Einheit der Leistungen; in beiden Sprachen gleich.</summary>
    public string EinheitKw { get; set; } = "kW";

    /// <summary>
    /// Einheit der Modulkosten. <b>Euro, nicht €/kW:</b> <c>TechnikPlanwertCtrl</c> nimmt
    /// den Wert als Basis „Modulpreis" UNVERÄNDERT als Betrag. Sie steht in beiden
    /// Sprachen gleich und braucht deshalb keinen Ressourcenschlüssel.
    /// </summary>
    public string EinheitEuro { get; set; } = "€";
}
