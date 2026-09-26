using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die Anzeigetexte der Zonen im Bedarfsdialog eines Gebäudes (Gebäudesimulation G6b, Welle W5):
/// Zonentabelle, Herleitung und Diagrammwahl. EIN Parameter statt vieler; jede Eigenschaft füllt
/// sich aus <c>MyResource</c> und nennt ihren Schlüssel.
/// </summary>
public sealed class GebaeudeBedarfZonenTexte
{
    /// <summary><c>GEBB_GRP_ZONEN</c></summary>
    public string Gruppe { get; set; } = Resource.GEBB_GRP_ZONEN;

    /// <summary><c>GEBZ_SP_ZONE</c></summary>
    public string SpalteZone { get; set; } = Resource.GEBZ_SP_ZONE;

    /// <summary><c>GEBZ_SP_BEHEIZT</c></summary>
    public string SpalteBeheizt { get; set; } = Resource.GEBZ_SP_BEHEIZT;

    /// <summary><c>GEBZ_BEHEIZT_JA</c></summary>
    public string BeheiztJa { get; set; } = Resource.GEBZ_BEHEIZT_JA;

    /// <summary><c>GEBZ_BEHEIZT_NEIN</c></summary>
    public string BeheiztNein { get; set; } = Resource.GEBZ_BEHEIZT_NEIN;

    /// <summary><c>GEBB_SP_ZONE_HEIZWAERME</c> — die Einheit steht dahinter.</summary>
    public string SpalteHeizwaerme { get; set; } = Resource.GEBB_SP_ZONE_HEIZWAERME;

    /// <summary><c>GEBB_SP_ZONE_MAX_LAST</c></summary>
    public string SpalteMaxLast { get; set; } = Resource.GEBB_SP_ZONE_MAX_LAST;

    /// <summary><c>GEBB_SP_ZONE_RAUMTEMPERATUR</c></summary>
    public string SpalteRaumtemperatur { get; set; } = Resource.GEBB_SP_ZONE_RAUMTEMPERATUR;

    /// <summary><c>GEBB_SP_ZONE_UEBERHITZUNG</c></summary>
    public string SpalteUeberhitzung { get; set; } = Resource.GEBB_SP_ZONE_UEBERHITZUNG;

    /// <summary><c>GEBB_HRL_ZONEN</c> — wie die Gebäudezahlen aus den Zonen entstehen (Festlegung 10).</summary>
    public string Herleitung { get; set; } = Resource.GEBB_HRL_ZONEN;

    /// <summary><c>GEBB_LBL_DIAGRAMM</c></summary>
    public string LabelDiagramm { get; set; } = Resource.GEBB_LBL_DIAGRAMM;

    /// <summary><c>GEBB_DIAGRAMM_GEBAEUDE</c> — der erste Eintrag der Diagrammwahl.</summary>
    public string EintragGebaeude { get; set; } = Resource.GEBB_DIAGRAMM_GEBAEUDE;

    /// <summary><c>GEBB_BILD_ZONE_UNBEHEIZT</c> — {0} Name der Zone; statt des Wärmelastbilds.</summary>
    public string BildUnbeheizt { get; set; } = Resource.GEBB_BILD_ZONE_UNBEHEIZT;
}
