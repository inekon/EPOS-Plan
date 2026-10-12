using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// EINE Zuordnung „externe Wärmebedarfsganglinie ↔ Projekt" (iU9-W9.4) — das
/// plattformfreie Abbild von <c>Z_ProjWaermebedarfModel</c>.
///
/// <para><b>Der KANAL gilt je Zuordnung, nicht je Ganglinie</b> (Migrationsschritt 48,
/// Entscheidung F18). Dieselbe Ganglinie darf einem Projekt mehrfach zugeordnet sein und
/// dabei einmal in den Heizbedarf und einmal in den Brauchwasserbedarf laufen — deshalb
/// hat jede Zeile ihr eigenes Modell und ihren eigenen Kanal.</para>
///
/// <para><b><see cref="Kanal"/> ist ein STEUERWERT</b>
/// (<c>DbWerte.KANAL_HEIZUNG</c>/<c>_BRAUCHWASSER</c>/<c>_PROZESS</c>), nie ein
/// Anzeigetext. Was der Anwender liest, steht in den Kanaleinträgen des Dialogs.</para>
/// </summary>
public sealed class WaermebedarfExternZeile
{
    /// <summary>Der Schlüssel der Zuordnung (<c>Z_ProjektWaermebedarf.ID_Z</c>).</summary>
    public int IdZ { get; set; }

    /// <summary>Die Stamm-Id der Ganglinie (<c>Tab_Waermebedarf.ID</c>).</summary>
    public int IdGanglinie { get; set; }

    /// <summary>Der Bezeichner der Ganglinie.</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Der Bedarfskanal als Steuerwert.</summary>
    public string Kanal { get; set; } = "";

    /// <summary>
    /// Die Art der Kältebedarfszeile als Steuerwert (KWN-0): <c>DbWerte.KAELTE_ART_RAUM</c> (Vorgabe) oder
    /// <c>DbWerte.KAELTE_ART_PROZESS</c>; wirksam nur im Kanal Kühlung.
    /// </summary>
    public string KaelteArt { get; set; } = WindowsFormsApplication1.DbWerte.KAELTE_ART_RAUM;

    /// <summary>
    /// Die Feldgruppe „Deckung“ (Konzept Kältebedarf 3.4, Schritt 213); wirksam nur im Kanal Kühlung — jeder andere
    /// Kanal schreibt beim Speichern „zentral“ mit leeren Split-Feldern (Kern, <c>Normalisieren</c>).
    /// </summary>
    public DeckungsartDaten Deckung { get; set; } = new();

    // ---- Die Deckungsfelder flach für den Hilfe-Assistenten (KI-Feldkarte der Maske Form_Waermebedarf) ----------------

    /// <summary>Deckungsart als Steuerwert (<c>zentral</c>/<c>split</c>).</summary>
    public string Deckungsart
    {
        get => Deckung.Deckungsart;
        set => Deckung.DeckungSetzen(value);
    }

    /// <summary>Die Einträge der Deckungsart für den Assistenten.</summary>
    public IReadOnlyList<KiKern.KiWahleintrag> DeckungsartWahl => DeckungsartDaten.KiEintraege(DeckungsartDaten.Deckungsarten);

    /// <summary>EER-Weg als Steuerwert (<c>fest</c>/<c>linear</c>).</summary>
    public string? EerWeg
    {
        get => Deckung.EerWeg;
        set => Deckung.EerWegSetzen(value ?? "");
    }

    /// <summary>Die Einträge des EER-Wegs für den Assistenten.</summary>
    public IReadOnlyList<KiKern.KiWahleintrag> EerWegWahl => DeckungsartDaten.KiEintraege(DeckungsartDaten.EerWege);

    /// <summary>Fester EER bzw. EER am Punkt 1.</summary>
    public double? Eer1 { get => Deckung.Eer1; set => Deckung.Eer1 = value; }

    /// <summary>Außentemperatur am Punkt 1 [°C].</summary>
    public double? Taussen1 { get => Deckung.Taussen1; set => Deckung.Taussen1 = value; }

    /// <summary>EER am Punkt 2 (nur linear).</summary>
    public double? Eer2 { get => Deckung.Eer2; set => Deckung.Eer2 = value; }

    /// <summary>Außentemperatur am Punkt 2 [°C] (nur linear).</summary>
    public double? Taussen2 { get => Deckung.Taussen2; set => Deckung.Taussen2 = value; }

    /// <summary>Kühlträger des Split-Stroms; <c>null</c> = Stromträger des Projekts. Die Einträge reicht der Dialog herein.</summary>
    public int? KuehlIdCarrier { get => Deckung.KuehlIdCarrier; set => Deckung.KuehlIdCarrier = value; }

    /// <summary>Eigener Zähler des Split-Stroms.</summary>
    public bool KuehlEigenerZaehler { get => Deckung.KuehlEigenerZaehler; set => Deckung.KuehlEigenerZaehler = value; }
}
