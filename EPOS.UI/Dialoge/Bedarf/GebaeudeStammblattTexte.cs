using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Beschriftungen des Gebäude-Stammblatts</b> (<see cref="GebaeudeStammblattFelder"/>,
/// Welle #465) — EIN Bündel (Hausregel ab etwa zehn Anzeigetexten). Die Beschriftungen sind
/// die des Katalogeditors (<c>GEBK_*</c>) ohne den nachgestellten Doppelpunkt; sie füllen sich
/// selbst aus <c>MyResource</c>. Was die VDI-Struktur beschriftet (Hüll-Raster, Modellparameter,
/// Kühlung, Rechenweg), steht im Bündel <see cref="GebaeudeHuelleTexte"/>.
/// </summary>
public sealed class GebaeudeStammblattTexte
{
    private static string F(string s) => GebaeudeArbeitsstand.Feld(s);

    // ------------------------------------------------------------ Gruppen

    /// <summary><c>GEBA_SB_HUELLE</c></summary>
    public string GruppeHuelle { get; set; } = Resource.GEBA_SB_HUELLE;

    /// <summary><c>GEBA_SB_HUELLE_HINWEIS</c> — die Zeile unter dem Hüll-Raster.</summary>
    public string HinweisHuelle { get; set; } = Resource.GEBA_SB_HUELLE_HINWEIS;

    /// <summary><c>GEBK_GRP_FENSTER_ORIENTIERUNG</c></summary>
    public string GruppeFenster { get; set; } = Resource.GEBK_GRP_FENSTER_ORIENTIERUNG;

    /// <summary><c>GEBK_GRP_KENNGROESSEN</c></summary>
    public string GruppeKenngroessen { get; set; } = Resource.GEBK_GRP_KENNGROESSEN;

    /// <summary><c>ADM_SB_ALLE_DATEN</c></summary>
    public string AlleDaten { get; set; } = Resource.ADM_SB_ALLE_DATEN;

    /// <summary><c>GEBK_GRP_RAUMTEMPERATUREN</c></summary>
    public string GruppeRaumtemperaturen { get; set; } = Resource.GEBK_GRP_RAUMTEMPERATUREN;

    /// <summary><c>GEBK_GRP_FERIEN_ANFANG</c></summary>
    public string GruppeFerienAnfang { get; set; } = Resource.GEBK_GRP_FERIEN_ANFANG;

    /// <summary><c>GEBK_GRP_FERIEN_ENDE</c></summary>
    public string GruppeFerienEnde { get; set; } = Resource.GEBK_GRP_FERIEN_ENDE;

    // ------------------------------------------------------------ Kenndaten und Kenngrößen

    /// <summary><c>GEBK_LBL_BAUART</c> — die Bauart bestimmt mit der Fläche die Bauweise.</summary>
    public string LabelBauart { get; set; } = F(Resource.GEBK_LBL_BAUART);

    /// <summary>Die drei Bauarten (<c>GEBK_BAUART_*</c>): leicht, schwer, sehr schwer.</summary>
    public IReadOnlyList<string> Bauarten { get; set; } = new[]
    {
        Resource.GEBK_BAUART_LEICHT, Resource.GEBK_BAUART_SCHWER, Resource.GEBK_BAUART_SEHRSCHWER
    };

    /// <summary><c>GEBK_LBL_FLAECHE_NUTZER</c></summary>
    public string LabelFlaecheNutzer { get; set; } = F(Resource.GEBK_LBL_FLAECHE_NUTZER);

    /// <summary><c>GEBK_LBL_WAERMEGEWINNE</c></summary>
    public string LabelWaermegewinne { get; set; } = F(Resource.GEBK_LBL_WAERMEGEWINNE);

    /// <summary><c>GEBK_LBL_RAUMHOEHE</c></summary>
    public string LabelRaumhoehe { get; set; } = F(Resource.GEBK_LBL_RAUMHOEHE);

    /// <summary><c>GEBK_LBL_FENSTERDURCHLASS</c></summary>
    public string LabelFensterdurchlassgrad { get; set; } = F(Resource.GEBK_LBL_FENSTERDURCHLASS);

    /// <summary><c>GEBK_HINWEIS_FENSTERDURCHLASS</c></summary>
    public string HinweisFensterdurchlassgrad { get; set; } = Resource.GEBK_HINWEIS_FENSTERDURCHLASS;

    /// <summary><c>GEBK_LBL_LUFTWECHSEL</c></summary>
    public string LabelLuftwechsel { get; set; } = F(Resource.GEBK_LBL_LUFTWECHSEL);

    // ------------------------------------------------------------ Fenster

    /// <summary><c>GEBK_LBL_FF_NORD</c></summary>
    public string LabelFFNord { get; set; } = F(Resource.GEBK_LBL_FF_NORD);

    /// <summary><c>GEBK_LBL_FF_SUED</c></summary>
    public string LabelFFSued { get; set; } = F(Resource.GEBK_LBL_FF_SUED);

    // ------------------------------------------------------------ Raumtemperaturen und Ferien

    /// <summary><c>GEBK_LBL_SOLL_TAG</c></summary>
    public string LabelSollTag { get; set; } = F(Resource.GEBK_LBL_SOLL_TAG);

    /// <summary><c>GEBK_LBL_NACHTABSENKUNG</c></summary>
    public string LabelNachtAbsenkung { get; set; } = F(Resource.GEBK_LBL_NACHTABSENKUNG);

    /// <summary><c>GEBK_LBL_MAXTEMPERATUR</c></summary>
    public string LabelMaxTemperatur { get; set; } = F(Resource.GEBK_LBL_MAXTEMPERATUR);

    /// <summary><c>GEBK_LBL_WE_ABSENKUNG</c></summary>
    public string LabelWEAbsenkung { get; set; } = F(Resource.GEBK_LBL_WE_ABSENKUNG);

    /// <summary><c>GEBK_LBL_SOLL_FERIEN</c></summary>
    public string LabelSollFerien { get; set; } = F(Resource.GEBK_LBL_SOLL_FERIEN);

    /// <summary><c>GEBK_LBL_TAG</c></summary>
    public string LabelTag { get; set; } = F(Resource.GEBK_LBL_TAG);

    /// <summary><c>GEBK_LBL_MONAT</c></summary>
    public string LabelMonat { get; set; } = F(Resource.GEBK_LBL_MONAT);

    /// <summary>Die vier Ferienzeiträume (<c>GEBK_FERIEN_*</c>) in ihrer festen Reihenfolge.</summary>
    public IReadOnlyList<string> Ferienzeitraeume { get; set; } = new[]
    {
        F(Resource.GEBK_FERIEN_WINTER), F(Resource.GEBK_FERIEN_OSTERN),
        F(Resource.GEBK_FERIEN_SOMMER), F(Resource.GEBK_FERIEN_HERBST)
    };
}
