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

    // ---------------------------------------------------------------------------------
    //  Gruppe „Gerätegrenzen" (UB‑E3‑b, WaermepumpeGeraetegrenzenFelder)
    // ---------------------------------------------------------------------------------

    /// <summary>WPS_GRP_GERAETEGRENZEN — Titel der Gruppe.</summary>
    public string GruppeGeraetegrenzen { get; set; } = T("WPS_GRP_GERAETEGRENZEN", "Gerätegrenzen");

    /// <summary>WPS_LBL_KAELTEMITTEL</summary>
    public string LabelKaeltemittel { get; set; } = T("WPS_LBL_KAELTEMITTEL", "Kältemittel");

    /// <summary>WPA_OPT_KAELTEMITTEL_LEER — der Eintrag ohne Wahl (derselbe wie in der Konfiguration).</summary>
    public string KaeltemittelLeer { get; set; } = T("WPA_OPT_KAELTEMITTEL_LEER", "nicht gewählt");

    /// <summary>WPS_LBL_SPREIZUNG_AUSLEGUNG</summary>
    public string LabelSpreizungAuslegung { get; set; } = T("WPS_LBL_SPREIZUNG_AUSLEGUNG", "Auslegungsspreizung");

    /// <summary>WPS_LBL_SPREIZUNG_MAX</summary>
    public string LabelSpreizungMax { get; set; } = T("WPS_LBL_SPREIZUNG_MAX", "Größte Spreizung");

    /// <summary>WPS_LBL_SPREIZUNG_MIN</summary>
    public string LabelSpreizungMin { get; set; } = T("WPS_LBL_SPREIZUNG_MIN", "Kleinste Spreizung");

    /// <summary>WPS_LBL_MINDESTVOLUMENSTROM</summary>
    public string LabelMindestvolumenstrom { get; set; } = T("WPS_LBL_MINDESTVOLUMENSTROM", "Mindestvolumenstrom");

    /// <summary>WPS_LBL_RUECKLAUF_MAX</summary>
    public string LabelRuecklaufMax { get; set; } = T("WPS_LBL_RUECKLAUF_MAX", "Größter Rücklauf");

    /// <summary>WPS_LBL_RUECKLAUF_BEZUG</summary>
    public string LabelRuecklaufBezug { get; set; } = T("WPS_LBL_RUECKLAUF_BEZUG", "Bezugsrücklauf");

    /// <summary>WPS_LBL_RUECKLAUF_ABWERTUNG</summary>
    public string LabelRuecklaufAbwertung { get; set; } = T("WPS_LBL_RUECKLAUF_ABWERTUNG", "Abwertung je Kelvin Rücklauf");

    /// <summary>WPS_HINT_KAELTEMITTEL_SCHNELLWAHL — was die Wahl des Kältemittels tut.</summary>
    public string HinweisSchnellwahl { get; set; } = T("WPS_HINT_KAELTEMITTEL_SCHNELLWAHL",
        "Die Wahl des Kältemittels füllt nur leere Gerätefelder mit der Vorgabe nach Kältemittel; gepflegte Werte bleiben stehen.");

    /// <summary>WPS_HINT_SCHNELLWAHL_GEFUELLT — {0} = Zahl der gefüllten Felder.</summary>
    public string HinweisSchnellwahlGefuellt { get; set; } = T("WPS_HINT_SCHNELLWAHL_GEFUELLT",
        "{0} leere Felder mit der Vorgabe nach Kältemittel gefüllt.");

    /// <summary>WPS_HINT_NUR_R744 — Bezugsrücklauf und Abwertung wirken nur bei R744.</summary>
    public string HinweisNurR744 { get; set; } = T("WPS_HINT_NUR_R744", "Bezugsrücklauf und Abwertung wirken nur bei R744.");

    /// <summary>WPS_HINT_GRENZE_BEREICH — {0} Feld, {1} von, {2} bis.</summary>
    public string HinweisBereich { get; set; } = T("WPS_HINT_GRENZE_BEREICH",
        "„{0}“ liegt außerhalb von {1} bis {2} — so lässt sich der Satz nicht speichern.");

    /// <summary>WPS_PLATZHALTER_VORGABE — leeres Feld: der Kern nimmt die Vorgabe.</summary>
    public string PlatzhalterVorgabe { get; set; } = T("WPS_PLATZHALTER_VORGABE", "leer = Vorgabe");

    /// <summary>WPS_HERLEITUNG_GERAETEGRENZEN — Herleitungszeile der Gruppe.</summary>
    public string HerleitungGeraetegrenzen { get; set; } = T("WPS_HERLEITUNG_GERAETEGRENZEN",
        "Leere Felder rechnen mit der Vorgabe nach Kältemittel bzw. der allgemeinen Vorgabe; gepflegte Werte tragen in der Konfiguration die Herkunft „Katalog“.");

    /// <summary>
    /// WPA_OPT_KAELTEMITTEL_&lt;Code&gt; — die Anzeige eines Kältemittelcodes, dieselbe Regel wie
    /// <see cref="WaermepumpeKonfigurationTexte.KaeltemittelText"/>; ein unbekannter Code zeigt sich selbst.
    /// </summary>
    public string KaeltemittelText(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return KaeltemittelLeer;
        string s = new string(code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        return T("WPA_OPT_KAELTEMITTEL_" + s, code);
    }

    /// <summary>Einheiten der Gerätegrenzen; in beiden Sprachen gleich.</summary>
    public string EinheitKelvin { get; set; } = "K";

    /// <summary>Prozent des Nennvolumenstroms.</summary>
    public string EinheitProzent { get; set; } = "%";

    /// <summary>Grad Celsius.</summary>
    public string EinheitGrad { get; set; } = "°C";

    /// <summary>Prozent je Kelvin.</summary>
    public string EinheitProzentJeK { get; set; } = "%/K";
}
