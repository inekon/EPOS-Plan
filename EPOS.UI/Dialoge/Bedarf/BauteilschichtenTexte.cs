using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Anzeigetexte des Schichtenrasters samt Summenfuß (<see cref="BauteilschichtenFelder"/>) —
/// EIN Parameter statt vieler. Jede Eigenschaft füllt sich selbst aus <c>MyResource</c>
/// (Präfix <c>BTA_</c>) und nennt ihren Schlüssel; Platzhalter <c>{0}</c> … füllt das Raster.
/// </summary>
public sealed class BauteilschichtenTexte
{
    /// <summary><c>BTA_RASTER</c> — die Bezeichnung des Rasters für die Sprachausgabe.</summary>
    public string Raster { get; set; } = Resource.BTA_RASTER;

    /// <summary><c>BTA_LEER_SCHICHTEN</c></summary>
    public string Leer { get; set; } = Resource.BTA_LEER_SCHICHTEN;

    /// <summary><c>BTA_SP_DICKE</c></summary>
    public string SpalteDicke { get; set; } = Resource.BTA_SP_DICKE;

    /// <summary><c>BTA_SP_LAMBDA</c></summary>
    public string SpalteLambda { get; set; } = Resource.BTA_SP_LAMBDA;

    /// <summary><c>BTA_SP_RHO</c></summary>
    public string SpalteRho { get; set; } = Resource.BTA_SP_RHO;

    /// <summary><c>BTA_SP_CP</c></summary>
    public string SpalteCp { get; set; } = Resource.BTA_SP_CP;

    /// <summary><c>BTA_LBL_SCHICHT</c> — „Schicht {0}".</summary>
    public string Schicht { get; set; } = Resource.BTA_LBL_SCHICHT;

    /// <summary><c>BTA_LBL_BAUSTOFF</c></summary>
    public string Baustoff { get; set; } = Resource.BTA_LBL_BAUSTOFF;

    /// <summary><c>BTA_FREIE_EINGABE</c> — der erste Eintrag der Stoffwahl.</summary>
    public string FreieEingabe { get; set; } = Resource.BTA_FREIE_EINGABE;

    /// <summary><c>BTA_LBL_LUFTSCHICHT</c></summary>
    public string Luftschicht { get; set; } = Resource.BTA_LBL_LUFTSCHICHT;

    /// <summary><c>BTA_HINWEIS_LUFTSCHICHT</c> — der Kurztext am Schalter.</summary>
    public string LuftschichtHinweis { get; set; } = Resource.BTA_HINWEIS_LUFTSCHICHT;

    /// <summary><c>BTA_R_SCHICHT</c> — „R = {0} m²K/W".</summary>
    public string RSchicht { get; set; } = Resource.BTA_R_SCHICHT;

    /// <summary><c>BTA_R_OFFEN</c> — R einer unvollständigen Schicht.</summary>
    public string ROffen { get; set; } = Resource.BTA_R_OFFEN;

    /// <summary><c>BTA_BTN_INNEN</c> — „Nach innen" (▲).</summary>
    public string NachInnen { get; set; } = Resource.BTA_BTN_INNEN;

    /// <summary><c>BTA_BTN_AUSSEN</c> — „Nach außen" (▼).</summary>
    public string NachAussen { get; set; } = Resource.BTA_BTN_AUSSEN;

    /// <summary><c>BTA_BTN_ENTFERNEN</c> — „Schicht entfernen" (✕).</summary>
    public string Entfernen { get; set; } = Resource.BTA_BTN_ENTFERNEN;

    /// <summary><c>BTA_BTN_NEUE_SCHICHT</c> — die Abschlusszeile.</summary>
    public string NeueSchicht { get; set; } = Resource.BTA_BTN_NEUE_SCHICHT;

    /// <summary><c>BTA_SUMME_R</c> — „R = Σ d/λ = {0} m²K/W".</summary>
    public string SummeR { get; set; } = Resource.BTA_SUMME_R;

    /// <summary><c>BTA_SUMME_U</c> — „U = {0} W/(m²K)".</summary>
    public string SummeU { get; set; } = Resource.BTA_SUMME_U;

    /// <summary><c>BTA_SUMME_C</c> — „C = Σ ρ·c_p·d = {0} kJ/(m²K)".</summary>
    public string SummeC { get; set; } = Resource.BTA_SUMME_C;

    /// <summary><c>BTA_SUMME_TBT</c> — „T_BT = {0} d · C₁ = {1} kJ/(m²K)".</summary>
    public string SummeTbt { get; set; } = Resource.BTA_SUMME_TBT;

    /// <summary><c>BTA_SUMME_TBT_OFFEN</c> — „T_BT: nicht bestimmbar – {0}".</summary>
    public string SummeTbtOffen { get; set; } = Resource.BTA_SUMME_TBT_OFFEN;

    /// <summary><c>BTA_SUMME_OFFEN</c> — „Summen nicht bestimmbar – {0}".</summary>
    public string SummeOffen { get; set; } = Resource.BTA_SUMME_OFFEN;

    /// <summary><c>BTA_HERLEITUNG_U</c> — die angesetzten Übergänge; {0} R_si, {1} R_se, {2} Neigung.</summary>
    public string HerleitungU { get; set; } = Resource.BTA_HERLEITUNG_U;

    /// <summary><c>BTA_HERLEITUNG_TBT</c> — die Wahl der Bezugsperiode; {0} R₁;rel, {1} C₁;rel.</summary>
    public string HerleitungTbt { get; set; } = Resource.BTA_HERLEITUNG_TBT;

    /// <summary><c>BTA_EINHEIT_MM</c></summary>
    public string EinheitMm { get; set; } = Resource.BTA_EINHEIT_MM;
}
