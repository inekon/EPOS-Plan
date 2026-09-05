using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Die Beschriftungen des Bausteins <c>PvModellFelder</c> (Paket A/B des PV-Ertragsmodells,
/// mit Merge 5 am 05.09.2026 aus <c>Form_PV</c> und <c>Form_PVModell</c> nachgezogen).
///
/// <para>Ein BÜNDEL nach der Bauart <c>LizenzTexte</c> (Hausregel EPOS.UI): Es füllt sich
/// SELBST aus <c>MyResource</c> in der Oberflächensprache, weil es reine Katalogeinträge
/// sind; ein fehlender Schlüssel fällt auf den deutschen Wortlaut zurück. Die Hülle muss
/// nichts beisteuern. Jede Eigenschaft nennt ihren Ressourcenschlüssel.</para>
/// </summary>
public sealed class PvModellTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    /// <summary>PVM_ANLAGE_LABEL_MODELL</summary>
    public string LabelModell { get; } = T("PVM_ANLAGE_LABEL_MODELL", "Rechenmodell:");
    /// <summary>PVM_ANLAGE_TIP_MODELL</summary>
    public string TipModell { get; } = T("PVM_ANLAGE_TIP_MODELL",
        "Einfach = Rechenweg des Bestands. Erweitert = Hay-Davies, Schwachlichtmodell nach Huld "
        + "und Wechselrichter-Kennlinie mit Clipping.");
    /// <summary>PVM_MODELL_EINFACH</summary>
    public string ModellEinfach { get; } = T("PVM_MODELL_EINFACH", "Einfach");
    /// <summary>PVM_MODELL_ERWEITERT</summary>
    public string ModellErweitert { get; } = T("PVM_MODELL_ERWEITERT", "Erweitert");

    /// <summary>PV_ANLAGE_LABEL_WRWIRKUNGSGRAD</summary>
    public string LabelWrWirkungsgrad { get; } = T("PV_ANLAGE_LABEL_WRWIRKUNGSGRAD", "WR-Wirkungsgrad [-]:");
    /// <summary>PV_ANLAGE_TIP_WRWIRKUNGSGRAD</summary>
    public string TipWrWirkungsgrad { get; } = T("PV_ANLAGE_TIP_WRWIRKUNGSGRAD",
        "Wechselrichter-Wirkungsgrad als Faktor. Leer = 0,95 (bisheriger fester Wert).");
    /// <summary>PV_ANLAGE_LABEL_SYSTEMVERLUSTE</summary>
    public string LabelSystemverluste { get; } = T("PV_ANLAGE_LABEL_SYSTEMVERLUSTE", "Systemverluste [%]:");
    /// <summary>PV_ANLAGE_TIP_SYSTEMVERLUSTE</summary>
    public string TipSystemverluste { get; } = T("PV_ANLAGE_TIP_SYSTEMVERLUSTE",
        "Pauschale Verluste (Verschmutzung, Leitungen, Abweichung). Leer = 0 %.");

    /// <summary>PVM_ANLAGE_BTN_WECHSELRICHTER</summary>
    public string BtnWechselrichter { get; } = T("PVM_ANLAGE_BTN_WECHSELRICHTER", "Wechselrichter…");
    /// <summary>PVM_ANLAGE_TIP_WECHSELRICHTER</summary>
    public string TipWechselrichter { get; } = T("PVM_ANLAGE_TIP_WECHSELRICHTER",
        "AC-Nennleistung und Teillast-Kennlinie des Wechselrichters (nur im Modell Erweitert).");

    /// <summary>PVM_DLG_TITEL — {0} = Anlage</summary>
    public string DialogTitel { get; } = T("PVM_DLG_TITEL", "Wechselrichter — {0}");
    /// <summary>PVM_DLG_KOPF_EINFACH</summary>
    public string KopfEinfach { get; } = T("PVM_DLG_KOPF_EINFACH",
        "Die Anlage rechnet im Modell Einfach. Die Felder sind deshalb gesperrt - sie wirken erst im Modell Erweitert.");
    /// <summary>PVM_DLG_KOPF_ERWEITERT</summary>
    public string KopfErweitert { get; } = T("PVM_DLG_KOPF_ERWEITERT",
        "Wechselrichterdaten für das Modell Erweitert. Leer = ohne Clipping bzw. Kennlinie des Bestands.");
    /// <summary>PVM_DLG_NENNLEISTUNG</summary>
    public string LabelNennleistung { get; } = T("PVM_DLG_NENNLEISTUNG", "AC-Nennleistung [kW]:");
    /// <summary>PVM_DLG_NENNLEISTUNG_TIP</summary>
    public string TipNennleistung { get; } = T("PVM_DLG_NENNLEISTUNG_TIP",
        "Nennleistung des Wechselrichters; oberhalb wird abgeregelt (Clipping). 0 = ohne Clipping.");
    /// <summary>PVM_DLG_ETA10</summary>
    public string LabelEta10 { get; } = T("PVM_DLG_ETA10", "Wirkungsgrad bei 10 % Last [-]:");
    /// <summary>PVM_DLG_ETA50</summary>
    public string LabelEta50 { get; } = T("PVM_DLG_ETA50", "Wirkungsgrad bei 50 % Last [-]:");
    /// <summary>PVM_DLG_ETA100</summary>
    public string LabelEta100 { get; } = T("PVM_DLG_ETA100", "Wirkungsgrad bei 100 % Last [-]:");
    /// <summary>PVM_DLG_ETA_TIP</summary>
    public string TipEta { get; } = T("PVM_DLG_ETA_TIP",
        "Teillast-Kennlinie des Wechselrichters als Faktor (0 bis 1). 0 = Stützstelle nicht bekannt.");
    /// <summary>PVM_DLG_DCAC — {0} kWp, {1} kW, {2} Verhältnis</summary>
    public string DcAc { get; } = T("PVM_DLG_DCAC", "DC/AC: {0:N2} kWp auf {1:N2} kW = {2:N2}");
    /// <summary>PVM_DLG_DCAC_OHNE</summary>
    public string DcAcOhne { get; } = T("PVM_DLG_DCAC_OHNE", "Ohne AC-Nennleistung: kein Clipping.");
    /// <summary>PVM_DLG_OK</summary>
    public string Ok { get; } = T("PVM_DLG_OK", "OK");
    /// <summary>PVM_DLG_ABBRECHEN</summary>
    public string Abbrechen { get; } = T("PVM_DLG_ABBRECHEN", "Abbrechen");
}
