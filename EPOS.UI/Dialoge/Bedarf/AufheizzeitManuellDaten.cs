using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Vorschläge neben „Aufheizzeit manuell (h)"</b> (Entwurf KP3, Welle O2; E59, Festlegung 40, P15 (b)) — aus der
/// Auskunft der Bemessung des Projektgebäudes, gebaut von der Hülle. Die Spanne fehlt ohne Bemessung (kein Sprung,
/// gekoppelt, Tagesbilanz); die bemessene Zeit fehlt bei unerreichbarer Bemessung (W1).
/// </summary>
public sealed class AufheizzeitManuellDaten
{
    /// <summary>Ist die Aufheizoptimierung des Projekts eingeschaltet? Sonst wirkt der Wert erst später.</summary>
    public bool SchalterAn { get; init; }

    /// <summary>t_auf,max dieses Gebäudes zum Übernehmen [h]; <c>null</c> = kein Vorschlag.</summary>
    public int? BemessenH { get; init; }

    /// <summary>Untere Grenze der Spanne [h]; <c>null</c> = keine Spanne.</summary>
    public int? VonH { get; init; }

    /// <summary>Obere Grenze der Spanne [h].</summary>
    public int? BisH { get; init; }

    /// <summary>τ₂ [h] — die langsame Zeitkonstante, aus der die Spanne folgt.</summary>
    public double? Tau2H { get; init; }

    /// <summary>Ist die Bemessung unerreichbar (W1)?</summary>
    public bool Unerreichbar { get; init; }

    /// <summary>Gibt es eine Spanne?</summary>
    public bool HatSpanne => VonH.HasValue && BisH.HasValue;
}

/// <summary>Die Beschriftungen des Feldes „Aufheizzeit manuell (h)" — ein Bündel; jede Eigenschaft nennt ihren Schlüssel.</summary>
public sealed class AufheizzeitManuellTexte
{
    /// <summary><c>KOND_AUFH_MANUELL_GRP</c></summary>
    public string Gruppe { get; set; } = Resource.KOND_AUFH_MANUELL_GRP;

    /// <summary><c>KOND_AUFH_MANUELL_LBL</c></summary>
    public string Label { get; set; } = Resource.KOND_AUFH_MANUELL_LBL;

    /// <summary><c>KOND_AUFH_MANUELL_PLATZHALTER</c></summary>
    public string Platzhalter { get; set; } = Resource.KOND_AUFH_MANUELL_PLATZHALTER;

    /// <summary><c>KOND_AUFH_MANUELL_VORSCHLAG_BEMESSEN</c> — {0} t_auf,max [h].</summary>
    public string VorschlagBemessen { get; set; } = Resource.KOND_AUFH_MANUELL_VORSCHLAG_BEMESSEN;

    /// <summary><c>KOND_AUFH_MANUELL_BTN_UEBERNEHMEN</c></summary>
    public string KnopfUebernehmen { get; set; } = Resource.KOND_AUFH_MANUELL_BTN_UEBERNEHMEN;

    /// <summary><c>KOND_AUFH_MANUELL_BTN_TITEL</c></summary>
    public string KnopfTitel { get; set; } = Resource.KOND_AUFH_MANUELL_BTN_TITEL;

    /// <summary><c>KOND_AUFH_MANUELL_SPANNE</c> — {0} von, {1} bis [h], {2} τ₂ [h].</summary>
    public string Spanne { get; set; } = Resource.KOND_AUFH_MANUELL_SPANNE;

    /// <summary><c>KOND_AUFH_MANUELL_HRL_AUSSERHALB</c> — {0} Wert, {1} von, {2} bis [h].</summary>
    public string HinweisAusserhalb { get; set; } = Resource.KOND_AUFH_MANUELL_HRL_AUSSERHALB;

    /// <summary><c>KOND_AUFH_MANUELL_HRL_OHNE_SCHALTER</c></summary>
    public string HinweisOhneSchalter { get; set; } = Resource.KOND_AUFH_MANUELL_HRL_OHNE_SCHALTER;

    /// <summary><c>KOND_AUFH_MANUELL_HRL_LEER</c></summary>
    public string HinweisLeer { get; set; } = Resource.KOND_AUFH_MANUELL_HRL_LEER;

    /// <summary><c>KOND_AUFH_MANUELL_HRL_UNERREICHBAR</c></summary>
    public string HinweisUnerreichbar { get; set; } = Resource.KOND_AUFH_MANUELL_HRL_UNERREICHBAR;

    /// <summary><c>KOND_AUFH_MANUELL_HRL_OHNE_VORSCHLAG</c></summary>
    public string HinweisOhneVorschlag { get; set; } = Resource.KOND_AUFH_MANUELL_HRL_OHNE_VORSCHLAG;

    /// <summary><c>KOND_AUFH_MANUELL_HRL_ZONEN</c> — {0} Zahl der Zonen, {1} der geerbte Wert.</summary>
    public string HinweisZonen { get; set; } = Resource.KOND_AUFH_MANUELL_HRL_ZONEN;

    /// <summary><c>GEBB_EINHEIT_H</c></summary>
    public string EinheitH { get; set; } = Resource.GEBB_EINHEIT_H;
}
