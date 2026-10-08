namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  Die DTO des Befunds je Bauteil (Abstimmung G5, B1; Farbmodus „Befund“): der Befund als
//  WERT, die eine Farbtafel, die Legendenzeile. Gebaut in den Hüllen von EPOS.UI.Daten
//  (GebaeudeAufbauHuelle) neben den Stufen des Farbmodus „Aufbau“; die Ansicht kennt keine
//  Fachklasse des Kerns. Die Razor-Seite des Farbmodus folgt in einem eigenen Schritt.
// =====================================================================================

/// <summary>
/// Der Befund eines Bauteils als Wert der Oberfläche — Spiegel von <c>Bauteilbefund</c> des Kerns, dazu „ohne Bauteil“
/// für Flächen, die kein Bauteil bestimmt. Der Zahlwert ist der Index in <see cref="GebaeudeAnsichtBefundstufen.FARBEN"/>
/// und das Byte je Dreieck im Körperfeld (<see cref="GebaeudeAnsichtKoerperfeldEintrag.BefundAb"/>).
/// </summary>
public enum Bauteilbefundstufe : byte
{
    /// <summary>Ohne Befund (grau).</summary>
    Ohne = 0,
    /// <summary>Körper nicht oder nur teilweise lesbar (orange).</summary>
    KoerperUnlesbar = 1,
    /// <summary>Ohne die Eigenschaften, die die Rechnung braucht: kein U-Wert oder keine Fläche (rot).</summary>
    OhneEigenschaften = 2,
    /// <summary>Fläche ohne Bauteil (Rückfall der Flächenklassifikation).</summary>
    OhneBauteil = 3,
}

/// <summary>
/// <b>Die Farbtafel des Befunds</b> — die eine Stelle der Farben rot, orange, grau (Abstimmung G5, B1). Orange und das
/// Hellgrau „ohne Bauteil“ sind dieselben Töne wie Stufe C und „ohne Bauteil“ der Tafel <see cref="GebaeudeAnsichtAufbaustufen"/>,
/// das Grau „ohne Befund“ ist das neutrale Grau (R0) beider Tafeln: Zwischen den Farbmodi wechselt nur, was etwas sagt.
/// <b>Kontrast</b> (Hausblatt: jede Schrift-auf-Fläche-Paarung 4,5 : 1): Schrift auf einer Stufenfarbe nimmt
/// <see cref="Schriftfarbe"/> — weiß auf Rot (rund 5,0 : 1), schwarz auf Orange (rund 7,8 : 1), Grau (rund 7,9 : 1) und
/// Hellgrau. Orange und Grau liegen in der Helligkeit nah beieinander und unterscheiden sich im Farbton; der Kontrastmodus
/// (<c>forced-colors</c>) zeichnet den Befund über die Stufenschlüssel, nicht über die Farbe.
/// </summary>
public static class GebaeudeAnsichtBefundstufen
{
    /// <summary>Die Zahl der Stufen der Legende (ohne, Körper unlesbar, ohne Eigenschaften, ohne Bauteil).</summary>
    public const int ZAHL = 4;

    /// <summary>Das Byte einer neutralen Innenfläche (R0) im Körperfeld — halbtransparent, nicht in der Legende.</summary>
    public const byte INNEN_NEUTRAL = 4;

    /// <summary>Das Byte eines Dreiecks ohne Gruppe (entartet).</summary>
    public const byte KEINE = 255;

    /// <summary>Die Farben nach dem Stufenindex: grau, orange, rot, hellgrau; zuletzt R0 grau.</summary>
    public static readonly IReadOnlyList<string> FARBEN = new[]
    {
        "#9e9e9e", "#f07f13", "#d62728", "#d6d3cc", "#9e9e9e",
    };

    /// <summary>Die Farbe einer Stufe.</summary>
    public static string Farbe(Bauteilbefundstufe s) => FARBEN[(int)s];

    /// <summary>Die Schriftfarbe auf der Farbe einer Stufe (mindestens 4,5 : 1): weiß auf Rot, sonst schwarz.</summary>
    public static string Schriftfarbe(Bauteilbefundstufe s) => s == Bauteilbefundstufe.OhneEigenschaften ? "#ffffff" : "#000000";

    /// <summary>Der sprachneutrale Schlüssel einer Stufe für das Markup (<c>data-befund</c>).</summary>
    public static string Schluessel(Bauteilbefundstufe s) => s switch
    {
        Bauteilbefundstufe.Ohne => "ohne",
        Bauteilbefundstufe.KoerperUnlesbar => "koerper",
        Bauteilbefundstufe.OhneEigenschaften => "eigenschaften",
        _ => "ohne-bauteil",
    };

    /// <summary>Hat die Stufe einen Befund (orange oder rot) — der Filter der Liste?</summary>
    public static bool MitBefund(Bauteilbefundstufe s) => s is Bauteilbefundstufe.KoerperUnlesbar or Bauteilbefundstufe.OhneEigenschaften;
}

/// <summary>Zahl und Fläche der Bauteile eines Befunds — eine Legendenzeile.</summary>
/// <param name="Stufe">Der Befund.</param>
/// <param name="Zahl">Zahl der Bauteile.</param>
/// <param name="FlaecheM2">Ihre Fläche [m²].</param>
public sealed record GebaeudeAnsichtBefundsumme(Bauteilbefundstufe Stufe, int Zahl, double FlaecheM2);
