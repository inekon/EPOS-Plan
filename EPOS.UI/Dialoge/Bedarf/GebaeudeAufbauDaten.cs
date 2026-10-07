using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  Die DTO des Bauteilaufbaus in der Oberfläche (Konzept Bauteilaufbau beim Import 5.4, BA-3):
//  die Zuordnungsstufe je Bauteil als WERT (Farbmodus „Aufbau“, Spalte „Aufbau“), die eine
//  Farbtafel der Stufen, die Legendensummen und der Bauteilsteckbrief. Die Komponenten kennen
//  keine Fachklasse des Kerns; gebaut werden die DTO in den Hüllen von EPOS.UI.Daten. Nichts
//  davon schreibt — der Steckbrief zeigt nur an.
// =====================================================================================

/// <summary>
/// Die Zuordnungsstufe eines Bauteils (Konzept 5.2) als Wert der Oberfläche — Spiegel von <c>Bauteilzuordnungsstufe</c>
/// des Kerns, dazu „ohne Bauteil" für Flächen, die kein Bauteil bestimmt. Der Zahlwert ist der Index in
/// <see cref="GebaeudeAnsichtAufbaustufen.FARBEN"/> und das Byte je Dreieck im Körperfeld.
/// </summary>
public enum Aufbaustufe : byte
{
    /// <summary>A: vollständiger relevanter Aufbau (Datei, Projektdatei, Katalog).</summary>
    A = 0,
    /// <summary>B: U-Wert aus der Datei, kein vollständiger Aufbau (Ersatzaufbau).</summary>
    B = 1,
    /// <summary>C: nur Geometrie, U aus der Vorgabe.</summary>
    C = 2,
    /// <summary>Transparent (Fenster, Vorhangfassade): nicht bewertet.</summary>
    Transparent = 3,
    /// <summary>Fläche ohne Bauteil (Rückfall der Flächenklassifikation).</summary>
    OhneBauteil = 4,
}

/// <summary>
/// <b>Die Farbtafel der Zuordnungsstufen</b> — die eine Stelle, an der die Farben A, B, C, transparent und „ohne Bauteil"
/// stehen (Konzept 5.4: A grün, B gelb, C orange, transparent grau). Grundriss, Legende, Steckbrief und Spalte zeichnen mit
/// ihr, die Szene des Moduls trägt sie als <c>aufbaufarben</c> mit. Die sechste Stelle ist die neutrale Innenfläche (R0,
/// zwischen Räumen einer Zone) — in 3D halbtransparent wie im Farbmodus „Randbedingung".
/// </summary>
public static class GebaeudeAnsichtAufbaustufen
{
    /// <summary>Die Zahl der Stufen der Legende (A, B, C, transparent, ohne Bauteil).</summary>
    public const int ZAHL = 5;

    /// <summary>Das Byte einer neutralen Innenfläche (R0) im Körperfeld — halbtransparent, nicht in der Legende.</summary>
    public const byte INNEN_NEUTRAL = 5;

    /// <summary>Das Byte eines Dreiecks ohne Gruppe (entartet).</summary>
    public const byte KEINE = 255;

    /// <summary>Die Farben nach dem Stufenindex: grün, gelb, orange, blaugrau, hellgrau; zuletzt R0 grau.</summary>
    public static readonly IReadOnlyList<string> FARBEN = new[]
    {
        "#2ca02c", "#e5c100", "#f07f13", "#8fa3ad", "#d6d3cc", "#9e9e9e",
    };

    /// <summary>Die Farbe einer Stufe.</summary>
    public static string Farbe(Aufbaustufe s) => FARBEN[(int)s];

    /// <summary>Der sprachneutrale Schlüssel einer Stufe für das Markup (<c>data-stufe</c>).</summary>
    public static string Schluessel(Aufbaustufe s) => s switch
    {
        Aufbaustufe.A => "a",
        Aufbaustufe.B => "b",
        Aufbaustufe.C => "c",
        Aufbaustufe.Transparent => "transparent",
        _ => "ohne",
    };

    /// <summary>
    /// Die Stufe einer Bauteilzeile des Zonendialogs — derselbe Vertrag wie <c>Bauteilzuordnung.Stufe(BauteilModel, …)</c> des
    /// Kerns (Wache in <c>GebaeudeAufbauHuelleTests</c>): Fenster und Vorhangfassade transparent; mit Aufbau, der kein
    /// Ersatzaufbau ist (<paramref name="ersatzaufbau"/>), A; mit U-Wert, der nicht aus der Vorgabe stammt, B; sonst C.
    /// </summary>
    public static Aufbaustufe Stufe(BauteilDaten b, bool ersatzaufbau)
    {
        if (b.Bauteilart is WindowsFormsApplication1.DbWerte.BAUTEILART_FENSTER or WindowsFormsApplication1.DbWerte.BAUTEILART_VORHANGFASSADE)
            return Aufbaustufe.Transparent;
        if (b.MitAufbau && !ersatzaufbau) return Aufbaustufe.A;
        if (b.UWert.HasValue && !string.Equals(b.Herkunft, WindowsFormsApplication1.DbWerte.HERKUNFT_VORGABE, StringComparison.Ordinal))
            return Aufbaustufe.B;
        return Aufbaustufe.C;
    }

    /// <summary>Ist die Stufe „nicht vollständig zugeordnet" (B oder C) — der Filter des Zonendialogs und der Liste?</summary>
    public static bool Unvollstaendig(Aufbaustufe s) => s is Aufbaustufe.B or Aufbaustufe.C;
}

/// <summary>Zahl und Fläche der Bauteile einer Stufe, getrennt nach Hülle (außen) und Innenbauteilen — eine Legendenzeile.</summary>
/// <param name="Stufe">Die Stufe.</param>
/// <param name="ZahlAussen">Zahl der Hüllbauteile.</param>
/// <param name="FlaecheAussenM2">Fläche der Hüllbauteile [m²].</param>
/// <param name="ZahlInnen">Zahl der Innenbauteile.</param>
/// <param name="FlaecheInnenM2">Fläche der Innenbauteile [m²].</param>
public sealed record GebaeudeAnsichtAufbausumme(Aufbaustufe Stufe, int ZahlAussen, double FlaecheAussenM2, int ZahlInnen, double FlaecheInnenM2);

/// <summary>Die sprachneutralen Schlüssel der Herkunft eines Werts im Steckbrief (Stilklasse, Prüfwert) — nie übersetzt.</summary>
public static class SteckbriefHerkunft
{
    /// <summary>Aus der Datei (IFC, gbXML).</summary>
    public const string Datei = "datei";
    /// <summary>Aus den Schichten gerechnet.</summary>
    public const string Schichten = "schichten";
    /// <summary>Vorgabe (Baualtersklasse, Typaufbau).</summary>
    public const string Vorgabe = "vorgabe";
    /// <summary>Aus der Projektdatei (HottCAD-Verbund, BA-4).</summary>
    public const string Projektdatei = "projektdatei";
    /// <summary>Aus dem Katalog (Namensabgleich, Katalogaufbau).</summary>
    public const string Katalog = "katalog";
    /// <summary>Vom Anwender gesetzt.</summary>
    public const string Manuell = "manuell";
    /// <summary>Unbekannt bzw. ohne Wert.</summary>
    public const string Leer = "leer";
}

/// <summary>
/// Eine Schicht im Steckbrief, innen → außen, als Anzeigetexte: Name, Dicke, λ, ρ, c, die Herkunft der Stoffwerte; eine
/// weggelassene Schicht (Relevanzregel) mit Grund.
/// </summary>
/// <param name="Name">Stoffname; leer = unbekannt.</param>
/// <param name="Dicke">Dicke mit Einheit.</param>
/// <param name="Lambda">λ mit Einheit oder Strich.</param>
/// <param name="Rohdichte">ρ mit Einheit oder Strich.</param>
/// <param name="Cp">c mit Einheit oder Strich.</param>
/// <param name="Herkunft">Herkunft der Stoffwerte als Anzeigetext.</param>
/// <param name="HerkunftSchluessel">Herkunft als Schlüssel (<see cref="SteckbriefHerkunft"/>).</param>
public sealed record BauteilsteckbriefSchicht(string Name, string Dicke, string Lambda, string Rohdichte, string Cp,
                                              string Herkunft, string HerkunftSchluessel)
{
    /// <summary>Hat die Relevanzregel die Schicht weggelassen? Dann ausgegraut mit <see cref="Grund"/>.</summary>
    public bool Weggelassen { get; init; }

    /// <summary>Der Grund des Weglassens; leer sonst.</summary>
    public string Grund { get; init; } = "";

    /// <summary>Der Schlüssel des Materialnamens im Abschnitt „Baustoffe" (Sprung); <c>null</c> = kein Sprung.</summary>
    public string? Materialschluessel { get; init; }
}

/// <summary>
/// <b>Der Bauteilsteckbrief</b> (BA-3, Anwenderentscheid vom 06.10.2026): alles, was die Rechnung über ein Bauteil weiß, als
/// fertige Anzeigetexte — Kopf, Rechengrößen samt Herkunft des U-Werts, R₁ und C₁ (für die Anzeige über die Bauteilreduktion
/// gerechnet, die Rechnung bleibt unberührt), der Aufbau als Schichtliste innen → außen, bei Fenster und Tür U, g und Rahmen.
/// Gebaut im Import aus dem Vorschlag, in „Datei erneut lesen" aus den gespeicherten Bauteilen.
/// </summary>
public sealed record BauteilsteckbriefDaten
{
    /// <summary>Die Bauteilkennung der Datei — der Schlüssel des Klicks.</summary>
    public string Kennung { get; init; } = "";

    /// <summary>Name des Bauteils.</summary>
    public string Name { get; init; } = "";

    /// <summary>Bauteilart als Anzeigetext.</summary>
    public string Art { get; init; } = "";

    /// <summary>Die Zuordnungsstufe (Farbe wie im Farbmodus „Aufbau").</summary>
    public Aufbaustufe Stufe { get; init; } = Aufbaustufe.C;

    /// <summary>Ist das Bauteil transparent (Fenster, Vorhangfassade) bzw. eine Tür — dann U, g und Rahmen statt Aufbau?</summary>
    public bool Transparent { get; init; }

    /// <summary>Fläche mit Einheit.</summary>
    public string Flaeche { get; init; } = "";

    /// <summary>Azimut in Grad oder Strich.</summary>
    public string Azimut { get; init; } = "";

    /// <summary>Neigung in Grad oder Strich.</summary>
    public string Neigung { get; init; } = "";

    /// <summary>Randbedingung als Anzeigetext.</summary>
    public string Randbedingung { get; init; } = "";

    /// <summary>Der U-Wert, der rechnet, mit Einheit oder Strich.</summary>
    public string UWert { get; init; } = "";

    /// <summary>Die Herkunft des U-Werts als Anzeigetext (Datei, Schichten, Vorgabe, Projektdatei).</summary>
    public string UHerkunft { get; init; } = "";

    /// <summary>Die Herkunft des U-Werts als Schlüssel (<see cref="SteckbriefHerkunft"/>).</summary>
    public string UHerkunftSchluessel { get; init; } = SteckbriefHerkunft.Leer;

    /// <summary>Der Hinweis bei mehr als 10 % Abweichung zwischen U der Datei und U aus den Schichten; leer = keiner.</summary>
    public string UHinweis { get; init; } = "";

    /// <summary>R₁ mit Einheit; leer = nicht gerechnet (siehe <see cref="Rechengrund"/>).</summary>
    public string R1 { get; init; } = "";

    /// <summary>C₁ mit Einheit; leer = nicht gerechnet.</summary>
    public string C1 { get; init; } = "";

    /// <summary>Die flächenbezogene Wärmekapazität Σ d·ρ·c mit Einheit; leer = nicht gerechnet.</summary>
    public string Kapazitaet { get; init; } = "";

    /// <summary>Warum R₁/C₁ fehlen (kein Aufbau, Stoffwert fehlt …); leer = gerechnet.</summary>
    public string Rechengrund { get; init; } = "";

    /// <summary>Der Name des Aufbaus; leer = keiner.</summary>
    public string Aufbau { get; init; } = "";

    /// <summary>Die Herkunft des Aufbaus als Anzeigetext.</summary>
    public string AufbauHerkunft { get; init; } = "";

    /// <summary>Die Herkunft des Aufbaus als Schlüssel (<see cref="SteckbriefHerkunft"/>).</summary>
    public string AufbauHerkunftSchluessel { get; init; } = SteckbriefHerkunft.Leer;

    /// <summary>Der Rang des Aufbaus nach der Rangfolge mit Projektdatei (Rang 1 bis 4) als Anzeigetext; leer = ohne Projektdatei bzw. ohne Rang.</summary>
    public string Aufbaurang { get; init; } = "";

    /// <summary>Die Schichten innen → außen, weggelassene eingereiht und markiert.</summary>
    public IReadOnlyList<BauteilsteckbriefSchicht> Schichten { get; init; } = Array.Empty<BauteilsteckbriefSchicht>();

    /// <summary>Bei einem Ersatzaufbau Typ und abgeglichener Wert (Dämmdicke bzw. λ); leer = kein Ersatzaufbau.</summary>
    public string Ersatz { get; init; } = "";

    /// <summary>Was zum vollständigen Aufbau fehlt; leer = nichts.</summary>
    public string Fehlt { get; init; } = "";

    /// <summary>g-Wert (Fenster) oder Strich; leer bei opaken Bauteilen.</summary>
    public string GWert { get; init; } = "";

    /// <summary>Rahmenanteil (Fenster) oder Strich; leer bei opaken Bauteilen.</summary>
    public string Rahmenanteil { get; init; } = "";

    /// <summary>Der Befund des Bauteils (Abstimmung G5, B1; Farbmodus „Befund").</summary>
    public Bauteilbefundstufe Befund { get; init; } = Bauteilbefundstufe.Ohne;

    /// <summary>Der Grund des Befunds als Text; leer ohne Befund.</summary>
    public string Befundgrund { get; init; } = "";

    /// <summary>Das gespeicherte Bauteil (Datei erneut lesen) — Sprung in den Bauteildialog; <c>null</c> = nicht gespeichert.</summary>
    public int? IdBauteil { get; init; }
}

/// <summary>Die Beschriftungen des Bauteilsteckbriefs (ein Bündel; je Eigenschaft ihr Ressourcenschlüssel).</summary>
public sealed class BauteilsteckbriefTexte
{
    /// <summary><c>BTSB_TITEL</c></summary>
    public string Titel { get; set; } = Resource.BTSB_TITEL;
    /// <summary><c>BTSB_SCHLIESSEN</c></summary>
    public string Schliessen { get; set; } = Resource.BTSB_SCHLIESSEN;
    /// <summary><c>BTSB_KENNUNG</c></summary>
    public string Kennung { get; set; } = Resource.BTSB_KENNUNG;
    /// <summary><c>BTSB_STUFE</c></summary>
    public string Stufe { get; set; } = Resource.BTSB_STUFE;
    /// <summary><c>BTSB_GRP_RECHNUNG</c></summary>
    public string GruppeRechnung { get; set; } = Resource.BTSB_GRP_RECHNUNG;
    /// <summary><c>BTSB_FLAECHE</c></summary>
    public string Flaeche { get; set; } = Resource.BTSB_FLAECHE;
    /// <summary><c>BTSB_AZIMUT</c></summary>
    public string Azimut { get; set; } = Resource.BTSB_AZIMUT;
    /// <summary><c>BTSB_NEIGUNG</c></summary>
    public string Neigung { get; set; } = Resource.BTSB_NEIGUNG;
    /// <summary><c>BTSB_RAND</c></summary>
    public string Rand { get; set; } = Resource.BTSB_RAND;
    /// <summary><c>BTSB_UWERT</c></summary>
    public string UWert { get; set; } = Resource.BTSB_UWERT;
    /// <summary><c>BTSB_R1</c></summary>
    public string R1 { get; set; } = Resource.BTSB_R1;
    /// <summary><c>BTSB_C1</c></summary>
    public string C1 { get; set; } = Resource.BTSB_C1;
    /// <summary><c>BTSB_KAPAZITAET</c></summary>
    public string Kapazitaet { get; set; } = Resource.BTSB_KAPAZITAET;
    /// <summary><c>BTSB_ANZEIGE</c></summary>
    public string Anzeige { get; set; } = Resource.BTSB_ANZEIGE;
    /// <summary><c>BTSB_GRP_AUFBAU</c></summary>
    public string GruppeAufbau { get; set; } = Resource.BTSB_GRP_AUFBAU;
    /// <summary><c>BTSB_SP_SCHICHT</c></summary>
    public string SpalteSchicht { get; set; } = Resource.BTSB_SP_SCHICHT;
    /// <summary><c>BTSB_SP_DICKE</c></summary>
    public string SpalteDicke { get; set; } = Resource.BTSB_SP_DICKE;
    /// <summary><c>BTSB_SP_LAMBDA</c></summary>
    public string SpalteLambda { get; set; } = Resource.BTSB_SP_LAMBDA;
    /// <summary><c>BTSB_SP_RHO</c></summary>
    public string SpalteRho { get; set; } = Resource.BTSB_SP_RHO;
    /// <summary><c>BTSB_SP_CP</c></summary>
    public string SpalteCp { get; set; } = Resource.BTSB_SP_CP;
    /// <summary><c>BTSB_SP_HERKUNFT</c></summary>
    public string SpalteHerkunft { get; set; } = Resource.BTSB_SP_HERKUNFT;
    /// <summary><c>BTSB_WEGGELASSEN</c> — {0} Grund.</summary>
    public string Weggelassen { get; set; } = Resource.BTSB_WEGGELASSEN;
    /// <summary><c>BTSB_ERSATZ</c> — {0} Typ und Abgleich.</summary>
    public string Ersatz { get; set; } = Resource.BTSB_ERSATZ;
    /// <summary><c>BTSB_KEIN_AUFBAU</c></summary>
    public string KeinAufbau { get; set; } = Resource.BTSB_KEIN_AUFBAU;
    /// <summary><c>BTSB_FEHLT</c> — {0} was fehlt.</summary>
    public string Fehlt { get; set; } = Resource.BTSB_FEHLT;
    /// <summary><c>BTSB_GWERT</c></summary>
    public string GWert { get; set; } = Resource.BTSB_GWERT;
    /// <summary><c>BTSB_RAHMEN</c></summary>
    public string Rahmen { get; set; } = Resource.BTSB_RAHMEN;
    /// <summary><c>BTSB_BEFUND</c></summary>
    public string Befund { get; set; } = Resource.BTSB_BEFUND;
    /// <summary><c>BTSB_ZUR_BAUSTOFFZUORDNUNG</c></summary>
    public string ZurBaustoffzuordnung { get; set; } = Resource.BTSB_ZUR_BAUSTOFFZUORDNUNG;
    /// <summary><c>BTSB_ZUM_BAUTEIL</c></summary>
    public string ZumBauteil { get; set; } = Resource.BTSB_ZUM_BAUTEIL;
    /// <summary><c>BTSB_NUR_ANZEIGE</c></summary>
    public string NurAnzeige { get; set; } = Resource.BTSB_NUR_ANZEIGE;
}
