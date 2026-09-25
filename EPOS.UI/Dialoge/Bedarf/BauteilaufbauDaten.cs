namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Ein Bauteilaufbau des Katalogs samt Schichten</b>, wie ihn die Verwaltung
/// „Bauteilaufbauten" zeigt und bearbeitet (Gebäudesimulation G3, Welle C) — das DTO zwischen
/// <c>BauteilaufbauHuelle</c> und <see cref="BauteilaufbauDialog"/>, ohne Fachklasse des Kerns.
/// </summary>
/// <remarks>
/// <para><b>Der Aufbau ist ein Aggregat</b> (Softwarearchitektur 1.4): Kopf und Schichten
/// werden zusammen gelesen und zusammen geschrieben, in EINER Transaktion des Kerns.</para>
/// <para><b>Die Bauteilart kommt als Persistenzwert</b> (<c>DbWerte.BAUTEILART_*</c>, leer =
/// „für jede Bauteilart") — Steuerwert, nie Anzeigetext; die Texte der Auswahl reicht die
/// Hülle herein.</para>
/// </remarks>
public sealed class BauteilaufbauDaten
{
    /// <summary>Id im Katalog; 0 = noch nicht gespeichert.</summary>
    public int Id { get; set; }

    /// <summary>Name des Aufbaus (Pflicht).</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Beschreibung (Freitext).</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Die Bauteilart als Persistenzwert; leer = für jede Bauteilart.</summary>
    public string Bauteilart { get; set; } = "";

    /// <summary>Regelwerk oder Quelle des Aufbaus.</summary>
    public string Quelle { get; set; } = "";

    /// <summary>Die Herkunft als Anzeigetext — nur Anzeige.</summary>
    public string Herkunft { get; set; } = "";

    /// <summary>Die Kennung eines Imports — nur Anzeige.</summary>
    public string Quellkennung { get; set; } = "";

    /// <summary>Gehört zur Auslieferung (Schloss) — dann nur lesbar.</summary>
    public bool Auslieferung { get; set; }

    /// <summary>Die Schichten, innen → außen; beim Speichern gilt die Listenreihenfolge.</summary>
    public List<BauteilschichtDaten> Schichten { get; set; } = new();

    /// <summary>Eine entkoppelte Kopie samt Schichten — der Arbeitsstand der Verwaltung.</summary>
    public BauteilaufbauDaten Kopie()
    {
        var k = (BauteilaufbauDaten)MemberwiseClone();
        k.Schichten = Schichten.Select(s => s.Kopie()).ToList();
        return k;
    }
}

/// <summary>
/// <b>Eine Schicht eines Aufbaus</b> — eine Zeile des Schichtenrasters, innen → außen.
/// </summary>
/// <remarks>
/// <para><b>Die Dicke steht in mm</b> (die Anzeigeeinheit des Rasters); gespeichert wird sie in
/// m — umgerechnet wird genau einmal, in der Hülle über den Kern
/// (<c>BauteilaufbauCtrl.DickeMm</c>/<c>DickeM</c>).</para>
/// <para><b>Die Stoffwerte sind eine Kopie</b> zum Zeitpunkt der Wahl des Baustoffs und danach
/// überschreibbar; <see cref="IdBaustoff"/> <c>null</c> heißt freie Eingabe. Eine Luftschicht
/// ohne λ ist eine ruhende Luftschicht — ihr Widerstand folgt DIN EN ISO 6946 Tabelle 8.</para>
/// </remarks>
public sealed class BauteilschichtDaten
{
    /// <summary>Id der Schicht (nur Anzeige; der Schreibweg vergibt sie neu).</summary>
    public int Id { get; set; }

    /// <summary>Der Baustoff des Katalogs, aus dem die Werte stammen; <c>null</c> = freie Eingabe.</summary>
    public int? IdBaustoff { get; set; }

    /// <summary>Schichtdicke [mm].</summary>
    public double? DickeMm { get; set; }

    /// <summary>Ruhende Luftschicht (ohne λ) bzw. Luftschicht mit äquivalenter Leitfähigkeit.</summary>
    public bool IstLuftschicht { get; set; }

    /// <summary>Wärmeleitfähigkeit λ [W/(mK)].</summary>
    public double? Lambda { get; set; }

    /// <summary>Rohdichte ρ [kg/m³].</summary>
    public double? Rho { get; set; }

    /// <summary>Spezifische Wärmekapazität c_p [J/(kgK)].</summary>
    public double? Cp { get; set; }

    /// <summary>Eine entkoppelte Kopie.</summary>
    public BauteilschichtDaten Kopie() => (BauteilschichtDaten)MemberwiseClone();

    /// <summary>Stimmen die eingebbaren Werte überein?</summary>
    public bool GleicheWerte(BauteilschichtDaten? andere)
        => andere is not null && IdBaustoff == andere.IdBaustoff && DickeMm == andere.DickeMm
           && IstLuftschicht == andere.IstLuftschicht && Lambda == andere.Lambda
           && Rho == andere.Rho && Cp == andere.Cp;
}

/// <summary>
/// Ein Baustoff zur Wahl in einer Schicht — Id, Anzeigetext der Suchauswahl und die Werte, die
/// die Wahl als Kopie übernimmt.
/// </summary>
public sealed record BaustoffWahl(int Id, string Text, double? Lambda, double? Rho, double? Cp);

/// <summary>
/// <b>Die Kennwerte eines Aufbaus</b> für Summenfuß und Stammblattkopf — fertig gerechnet in
/// der Hülle aus dem Kern (<c>BauteilaufbauCtrl.Kennwerte</c>); die Oberfläche rechnet nicht.
/// Jede Größe trägt ihre Einheit im Namen; <c>null</c> = nicht bestimmbar.
/// </summary>
public sealed record AufbauKennwerteDaten
{
    /// <summary>Die angesetzte Neigung [°].</summary>
    public double NeigungGrad { get; init; } = 90.0;

    /// <summary>R_si [m²K/W].</summary>
    public double RSi_M2KW { get; init; }

    /// <summary>R_se [m²K/W] — an Außenluft.</summary>
    public double RSe_M2KW { get; init; }

    /// <summary>R je Schicht [m²K/W] in Schichtreihenfolge.</summary>
    public IReadOnlyList<double?> RJeSchicht_M2KW { get; init; } = Array.Empty<double?>();

    /// <summary>R = Σ d/λ [m²K/W].</summary>
    public double? R_M2KW { get; init; }

    /// <summary>U [W/(m²K)].</summary>
    public double? U_WM2K { get; init; }

    /// <summary>Flächenbezogene Wärmekapazität Σ ρ·c_p·d [kJ/(m²K)].</summary>
    public double? Kapazitaet_KJM2K { get; init; }

    /// <summary>Wirksame Kapazität C₁ je m² bei T_BT [kJ/(m²K)].</summary>
    public double? KapazitaetWirksam_KJM2K { get; init; }

    /// <summary>Bezugsperiode T_BT [d] — 7 oder 2.</summary>
    public double? Bezugsperiode_D { get; init; }

    /// <summary>R₁;rel [–].</summary>
    public double? R1Rel { get; init; }

    /// <summary>C₁;rel [–].</summary>
    public double? C1Rel { get; init; }

    /// <summary>Warum R, U und C nicht stehen; leer, wenn sie stehen.</summary>
    public string Grund { get; init; } = "";

    /// <summary>Warum T_BT nicht bestimmbar ist; leer, wenn sie steht.</summary>
    public string PeriodeGrund { get; init; } = "";

    /// <summary>Nichts gerechnet — der Zustand ohne Hülle.</summary>
    public static AufbauKennwerteDaten Leer { get; } = new();
}

/// <summary>
/// Was ein Schreibversuch der Aufbauverwaltung ergab — gelungen, die Meldung des Kerns (bei
/// Misserfolg) und die Id des geschriebenen Aufbaus.
/// </summary>
public sealed record BauteilaufbauSpeicherErgebnis(bool Ok, string Meldung, int Id);
