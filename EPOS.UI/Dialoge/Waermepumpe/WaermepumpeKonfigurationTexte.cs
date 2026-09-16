using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Waermepumpe;

/// <summary>
/// Die Beschriftungen des Bausteins <see cref="WaermepumpeKonfiguration"/> — des Blocks,
/// der bis zum 16.09.2026 im Anlagendialog unter der Überschrift „Wärmeerzeuger
/// Spitzenlast:" stand.
///
/// <para><b>Warum der Block einen eigenen Namen bekam.</b> Die alte Überschrift benannte
/// nur den ERSTEN Schalter (die elektrische Nachheizung); darunter standen aber Sperrzeit,
/// bivalenter Betrieb, Bivalenztemperatur und der Energieträger — alles, was den Rechenweg
/// der Wärmepumpe parametriert. Seit dem Anwenderentscheid vom 16.09.2026 heißt der Block
/// deshalb „Konfiguration" und ist von zwei Stellen aus erreichbar: aus der Detailansicht
/// über den Knopf „Konfiguration…" und aus Simulation › Konfiguration.</para>
///
/// <para>Ein BÜNDEL nach der Bauart <c>PvModellTexte</c> (Hausregel EPOS.UI, „ab etwa zehn
/// Anzeigetexten ein Bündel"): Es füllt sich SELBST aus <c>MyResource</c> in der
/// Oberflächensprache; ein fehlender Schlüssel fällt auf den deutschen Wortlaut zurück. Die
/// Hülle muss nichts beisteuern — der Baustein zeichnet auch ohne jede Gabe. Jede
/// Eigenschaft nennt ihren Ressourcenschlüssel.</para>
///
/// <para>Die Eigenschaften sind SETZBAR: Ein Wirt, der seine Texte aus einer Hülle bezieht
/// (Prüfstand, Fremdsprache aus einem anderen Katalog), überschreibt einzelne davon.</para>
/// </summary>
public sealed class WaermepumpeKonfigurationTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    // --- Heizstab und Überschrift ----------------------------------------------

    /// <summary>WPA_CHK_HEIZSTAB — der Text des Heizstab-Häkchens.</summary>
    public string LabelHeizstab { get; set; } = T("WPA_CHK_HEIZSTAB",
        "Elektrische Nachheizung aktivieren (falls vorhanden)");

    /// <summary>WPA_HINWEIS_SPITZENLAST — die Herleitungszeile unter dem Häkchen.</summary>
    public string HinweisSpitzenlast { get; set; } = T("WPA_HINWEIS_SPITZENLAST",
        "Ein Spitzenlast Wärmeerzeuger kann notwendig sein aufgrund:");

    // --- Sperrzeit --------------------------------------------------------------

    /// <summary>WPA_LBL_SPERRZEIT — Titel der Formulargruppe (<c>label19</c> des Vorbilds).</summary>
    public string GruppeSperrzeit { get; set; } = T("WPA_LBL_SPERRZEIT",
        "Wärmepumpenleistung / maximale Betriebszeit:");

    /// <summary>WPA_CHK_SPERRZEIT — der Text des Sperrzeit-Häkchens.</summary>
    public string LabelSperrzeitSchalter { get; set; } = T("WPA_CHK_SPERRZEIT",
        "Sperrzeit durch Energieversorger");

    /// <summary>WPA_LBL_VON</summary>
    public string LabelVon { get; set; } = T("WPA_LBL_VON", "Sperrzeit von");

    /// <summary>WPA_LBL_BIS</summary>
    public string LabelBis { get; set; } = T("WPA_LBL_BIS", "Sperrzeit bis");

    /// <summary>
    /// Einheit der Sperrzeit — <c>label35</c> des Vorbilds. Sie steht in beiden Sprachen
    /// gleich und braucht deshalb keinen Ressourcenschlüssel.
    /// </summary>
    public string EinheitStundeTag { get; set; } = "h/Tag";

    // --- Außentemperaturgesteuerter Betrieb -------------------------------------

    /// <summary>WPA_HINWEIS_BETRIEB — Titel der Formulargruppe.</summary>
    public string GruppeBetrieb { get; set; } = T("WPA_HINWEIS_BETRIEB",
        "Außentemperaturgesteuerter Betrieb:");

    /// <summary>WPA_LBL_BIVALENT</summary>
    public string LabelBivalent { get; set; } = T("WPA_LBL_BIVALENT", "Bivalenter Betrieb");

    /// <summary>WPA_LBL_BETRIEBSART</summary>
    public string LabelBetriebsart { get; set; } = T("WPA_LBL_BETRIEBSART", "Betriebsart");

    /// <summary>WPA_LBL_ABSCHALTTEMP</summary>
    public string LabelAbschalttemp { get; set; } = T("WPA_LBL_ABSCHALTTEMP", "Bivalenztemperatur");

    /// <summary>WPA_LBL_ABSCHALTTEMP — derselbe Schlüssel als FELDNAME der Meldung.</summary>
    public string LabelAbschalttempKurz { get; set; } = T("WPA_LBL_ABSCHALTTEMP", "Bivalenztemperatur");

    /// <summary>Einheit der Bivalenztemperatur; in beiden Sprachen gleich.</summary>
    public string EinheitGrad { get; set; } = "°C";

    // --- Energieträger (ET-5) ----------------------------------------------------

    /// <summary>ETW_GRP_TITEL — Titel der Formulargruppe.</summary>
    public string GruppeEnergietraeger { get; set; } = T("ETW_GRP_TITEL", "Energieträger");

    /// <summary>ETW_LBL_GRUPPE</summary>
    public string LabelTraegerGruppe { get; set; } = T("ETW_LBL_GRUPPE", "Energieträger:");

    /// <summary>ETW_LBL_ART</summary>
    public string LabelTraegerArt { get; set; } = T("ETW_LBL_ART", "Art:");

    // --- Die drei Erklärkästen ---------------------------------------------------

    /// <summary>WPA_ERL_ALTERNATIV — der grüne Kasten (<c>label21</c> des Vorbilds).</summary>
    public string ErlaeuterungAlternativ { get; set; } = T("WPA_ERL_ALTERNATIV",
        "Bei der bivalent-alternativen Betriebsweise wird der Wärmebedarf bis zum Erreichen des "
        + "Bivalenzpunktes allein von der Wärmepumpe getragen. Der zweite Wärmeerzeuger springt bei "
        + "der Unterschreitung des Bivalenzpunktes ein und übernimmt den alleinigen Heizbetrieb.");

    /// <summary>WPA_ERL_PARALLEL — der gelbe Kasten (<c>label22</c>).</summary>
    public string ErlaeuterungParallel { get; set; } = T("WPA_ERL_PARALLEL",
        "Bei der bivalent-parallelen Betriebsweise wird der Wärmebedarf bis zum Erreichen des "
        + "Bivalenzpunktes allein von der Wärmepumpe getragen. Bei der Unterschreitung des "
        + "Bivalenzpunktes unterstützt der zweite Wärmeerzeuger den Heizbetrieb der Wärmepumpe.");

    /// <summary>WPA_ERL_TEILPARALLEL — der türkise Kasten (<c>label23</c>).</summary>
    public string ErlaeuterungTeilparallel { get; set; } = T("WPA_ERL_TEILPARALLEL",
        "Der bivalent-teilparallele Betrieb ist eine Mischung aus bivalent-paralleler und "
        + "bivalent-alternativer Betriebsweise. Die Wärmepumpe arbeitet bis zum Bivalenzpunkt allein "
        + "und wird anschließend vom zweiten Wärmeerzeuger unterstützt. Bei Erreichen einer weiteren "
        + "festgelegten Temperatur (z. B. -2 °C) schaltet sich die Wärmepumpe ab.");
}
