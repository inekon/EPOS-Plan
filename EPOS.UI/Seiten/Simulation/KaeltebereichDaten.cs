using System.Collections.Generic;
using EPOS.UI.Bausteine;

namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// Die Stufen der Kältefolge in einer Kältestunde, wie der Kern sie rechnet (Entwurf Kältebereich 1.4, 3.4) — die
/// Seite kennt die Fachklassen des Kerns nicht und bekommt deshalb diese eigene Aufzählung.
/// </summary>
public enum KaelteStufe
{
    /// <summary>Kältemaschinen mit Trocken- oder Nasskühler in Stunden mit kaltem Rückkühler — vor allen anderen.</summary>
    FreieKuehlung,

    /// <summary>Die Kältespeicher entladen, nach Entladepriorität.</summary>
    Kaeltespeicher,

    /// <summary>Die Wärmepumpen im Kühlbetrieb in der Folge der Wärmekaskade.</summary>
    Waermepumpe,

    /// <summary>Die Kältemaschinen in Anlagenfolge.</summary>
    Kaeltemaschine
}

/// <summary>
/// EIN Kälteerzeuger des Bereichs „Kälte“ in Rechenfolge (Welle KB-A): die Kachel samt den Werten, aus denen sie
/// gebaut ist, und den Kennungen, mit denen die Seite ihre Ereignisse verteilt.
/// </summary>
public sealed class KaelteerzeugerZeile
{
    /// <summary>Die Kachel: Titel, Rang (Nummer in der Folge), Chips Leistung, Vorlauf, Kühlträger, Rückkühlung.</summary>
    public ErzeugerKachelDaten Kachel = new ErzeugerKachelDaten();

    /// <summary><see cref="KaelteStufe.Waermepumpe"/> oder <see cref="KaelteStufe.Kaeltemaschine"/>.</summary>
    public KaelteStufe Art;

    /// <summary>Die Nummer in der Rechenfolge (1, 2, …) — Anzeige, keine Pflege.</summary>
    public int Nummer;

    /// <summary><c>Tab_Energieanlagen.ID</c>.</summary>
    public int IdAnlage;

    /// <summary>Die Projektkopie des Geräts (<c>Tab_WP.ID</c> bzw. <c>Tab_Kaeltemaschine.ID</c>); 0 = keine.</summary>
    public int IdGeraet;

    /// <summary>Der Anzeigename der Anlage.</summary>
    public string Bezeichner = "";

    /// <summary>Anzahl gleicher Geräte (Wärmepumpe 1).</summary>
    public int Anzahl = 1;

    /// <summary>Nennkälteleistung je Gerät [kW]; <c>null</c> = ungepflegt.</summary>
    public double? NennleistungKw;

    /// <summary>Kühlbetrieb an — für die Wärmepumpe „aufgenommen“; die Kältemaschine immer an.</summary>
    public bool Kuehlbetrieb;

    /// <summary>Warum sich der Kühlbetrieb der Wärmepumpe nicht einschalten lässt; <c>null</c> = frei.</summary>
    public string? Sperrgrund;

    /// <summary>Wärmepumpe: steht sie in der Wärmekaskade? Ohne Platz dort kühlt sie nicht (Handgriff „in die Wärme aufnehmen“).</summary>
    public bool InWaermekaskade = true;

    /// <summary>Kühl- bzw. Kaltwasservorlauf [°C] der Projektkopie; <c>null</c> = Vorgabe des Geräts.</summary>
    public double? KuehlVorlaufC;

    /// <summary>Hilfsstromanteil (0…1) der Projektkopie; <c>null</c> = kein Zuschlag.</summary>
    public double? Hilfsstromanteil;

    /// <summary>Kühlträger der Anlagenzeile; <c>null</c> = Stromträger des Projekts.</summary>
    public int? KuehltraegerId;

    /// <summary>Name des Kühlträgers; leer ohne Kühlträger.</summary>
    public string KuehltraegerName = "";

    /// <summary>Abrechnung über einen eigenen Zähler.</summary>
    public bool EigenerZaehler;

    /// <summary>Kühlt frei (Kältemaschine: Trocken-/Nasskühler; Wärmepumpe: Schalter und tragende Quelle).</summary>
    public bool FreieKuehlung;

    /// <summary>Kältemaschine: die Rückkühlart als Anzeigetext; leer bei der Wärmepumpe.</summary>
    public string Rueckkuehlart = "";

    /// <summary>Wie viele Anlagen dieselbe Projektkopie führen; größer 1 = Vorlauf und Hilfsstrom gelten für alle.</summary>
    public int AnlagenJeKopie = 1;
}

/// <summary>Ein Erzeuger mit freier Kühlung — die Lesezeile des Bereichs „Kälte“.</summary>
/// <param name="Art">Wer frei kühlt.</param>
/// <param name="IdAnlage"><c>Tab_Energieanlagen.ID</c>.</param>
/// <param name="Bezeichner">Der Anzeigename.</param>
/// <param name="VorAllenErzeugern"><c>true</c> = Kältemaschine, deckt vor allen; <c>false</c> = Wärmepumpe an ihrem Platz.</param>
public sealed record FreieKuehlungZeile(KaelteStufe Art, int IdAnlage, string Bezeichner, bool VorAllenErzeugern);

/// <summary>
/// <b>Der Bereich „Kälte“ der Simulationskonfiguration</b> (Entwurf Kältebereich 3, Welle KB-A): der Stand, den die
/// Hülle aus dem Kernleser der Kältefolge baut. Der Projektschalter schreibt über
/// <see cref="SimulationParameterDienste.KuehlbetriebSchreiben"/>, der Kühlbetrieb einer Wärmepumpe über
/// <see cref="SimulationKonfigDienste.KuehlbetriebWpSchreiben"/>. Nie <c>null</c>.
/// </summary>
public sealed class KaeltebereichDaten
{
    /// <summary>„Kühlung rechnen“ des Projekts (<c>Tab_Einstellungen.Kuehlbetrieb</c>).</summary>
    public bool Kuehlbetrieb;

    /// <summary>Die Stufen in Rechenfolge — heute fest; die Herleitungszeile nennt sie.</summary>
    public IReadOnlyList<KaelteStufe> Folge = new List<KaelteStufe>();

    /// <summary>Die Kälteerzeuger in Rechenfolge: Wärmepumpen mit Kühlfunktion, dann Kältemaschinen.</summary>
    public IReadOnlyList<KaelteerzeugerZeile> Erzeuger = new List<KaelteerzeugerZeile>();

    /// <summary>Die Kältespeicher (Puffer mit Verwendung Kälte) in Entladefolge, als Speicherkacheln.</summary>
    public IReadOnlyList<SpeicherKachelDaten> Kaeltespeicher = new List<SpeicherKachelDaten>();

    /// <summary>Die Erzeuger mit freier Kühlung: Kältemaschinen (vor allen), dann Wärmepumpen.</summary>
    public IReadOnlyList<FreieKuehlungZeile> FreieKuehlung = new List<FreieKuehlungZeile>();

    /// <summary>true = weder Kälteerzeuger noch Kältespeicher — die Leerzeile sagt, wo man sie anlegt.</summary>
    public bool Leer => Erzeuger.Count == 0 && Kaeltespeicher.Count == 0;
}
