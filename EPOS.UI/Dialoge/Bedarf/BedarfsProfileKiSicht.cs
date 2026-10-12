using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Maske „Bedarfsprofile im Projekt" für den Hilfe-Assistenten
/// (Welle KI‑F3).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>BedarfsProfilZeile</c>.</b> Die Maske
/// führt zwei Listen; was der Anwender hier EINSTELLT, sind die Anzeigeeinheit, der
/// neue Jahresverbrauch und beim Brauchwasser der Rechenweg (Welle #458, Stufe 3a) —
/// alle drei liegen in ihren lebenden Feldern, und der Infoblock
/// daneben ist ein formatierter Stand, kein Satz. Ein an der Zeile angemeldeter Katalog
/// hätte weder das eine noch das andere geführt.</para>
///
/// <para><b>EINE Maske, DREI Ausprägungen.</b> Dieselbe Komponente pflegt die
/// Prozesswärme-, die Stromverbraucher- und die Brauchwasserprofile; welche gerade
/// offen ist, sagt das Feld <c>bedarfsart</c>.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class BedarfsProfileKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? EinheitLesen { get; init; }
    public Action<int?>? EinheitSetzen { get; init; }

    public Func<double?>? NeuerWertLesen { get; init; }
    public Action<double?>? NeuerWertSetzen { get; init; }

    public Func<string>? ProfilLesen { get; init; }
    public Func<string>? TypLesen { get; init; }
    public Func<string>? BeschreibungLesen { get; init; }
    public Func<string>? JahresverbrauchLesen { get; init; }
    public Func<string>? SummeLesen { get; init; }
    public Func<string>? BedarfsartLesen { get; init; }

    /// <summary>Liefert die Energieeinheiten, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? EinheitEintraege { get; init; }

    /// <summary>Die Energieeinheiten der Klappliste (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> EinheitWahl
        => EinheitEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    public Func<int?>? KalenderLesen { get; init; }
    public Action<int?>? KalenderSetzen { get; init; }

    public Func<int?>? KaelteArtLesen { get; init; }
    public Action<int?>? KaelteArtSetzen { get; init; }

    /// <summary>Liefert die Arten einer Kältezeile (Raum/Prozess); leer außerhalb der Kälte.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? KaelteArtEintraege { get; init; }

    /// <summary>Die Arten der Kältezeile der Wahl (KWN-0).</summary>
    public IReadOnlyList<KiWahleintrag> KaelteArtWahl
        => KaelteArtEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Liefert die Betriebskalender der Wahl (0 = ohne Kalender).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? KalenderEintraege { get; init; }

    /// <summary>Die Betriebskalender der Wahl.</summary>
    public IReadOnlyList<KiWahleintrag> BetriebskalenderWahl
        => KalenderEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    public Func<int?>? RechenwegLesen { get; init; }

    /// <summary>
    /// Stellt die Optionsgruppe „Rechenweg Brauchwasser" um — derselbe Weg wie ein Klick;
    /// Rückgabe: der Grund einer Ablehnung, leer = umgestellt.
    /// </summary>
    public Func<int?, string?>? RechenwegSetzen { get; init; }

    /// <summary>Liefert die zwei Wege der Optionsgruppe.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? RechenwegEintraege { get; init; }

    /// <summary>Die Wege der Optionsgruppe: Bestandsprofile oder Zapfprofil.</summary>
    public IReadOnlyList<KiWahleintrag> RechenwegWahl
        => RechenwegEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Die Anzeigeeinheit als Platz in der Klappliste; sie gilt für den Infoblock, die
    /// Summen und das Eingabefeld daneben.
    /// </summary>
    public int? Einheit
    {
        get => EinheitLesen?.Invoke();
        set => EinheitSetzen?.Invoke(value);
    }

    /// <summary>
    /// Der neue Jahresverbrauch der gewählten Zuordnung, in der Einheit daneben. Er
    /// geht erst mit „Übernehmen" in die Zeile — das ist der Speicherweg dieser Maske.
    /// </summary>
    public double? NeuerWert
    {
        get => NeuerWertLesen?.Invoke();
        set => NeuerWertSetzen?.Invoke(value);
    }

    /// <summary>
    /// Der Rechenweg des Brauchwasserkanals (Optionsgruppe „Rechenweg Brauchwasser",
    /// Zapfprofil‑Weiche): Bestandsprofile oder Zapfprofil. Er steht nur in der Ausprägung
    /// Brauchwasser eines gespeicherten Projekts — sonst ist er leer, und das Setzen nennt
    /// den Grund. „Zapfprofil" ohne Zone lehnt die Maske ab wie der gesperrte Knopf.
    /// </summary>
    public int? Rechenweg
    {
        get => RechenwegLesen?.Invoke();
        set
        {
            string? grund = RechenwegSetzen is null
                ? WindowsFormsApplication1.MyResource.Resource.KI_SIM_KEIN_SCHREIBWEG
                : RechenwegSetzen(value);
            if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
        }
    }

    /// <summary>
    /// Der Betriebskalender der markierten Zuordnung (PW2/BW2): Feiertage und Betriebsferien auf ihrem
    /// Wochenprofil; 0 oder leer = ohne Kalender. Geschrieben wird mit OK.
    /// </summary>
    public int? Betriebskalender
    {
        get => KalenderLesen?.Invoke();
        set => KalenderSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die Art der markierten Kältezeile (KWN-0): 0 = Raumkühlung, 1 = Prozesskälte; leer außerhalb der Kältebedarfsprofile.
    /// Geschrieben wird mit OK.
    /// </summary>
    public int? KaelteArt
    {
        get => KaelteArtLesen?.Invoke();
        set => KaelteArtSetzen?.Invoke(value);
    }

    // ---- K5b: die Feldgruppe „Deckung“ der markierten Kältezeile (Konzept Kältebedarf 3.4) ----------------------

    /// <summary>Der Feldsatz „Deckung“ der markierten Kältezeile; <c>null</c> außerhalb einer Projektzeile der Art Kälte.</summary>
    public Func<DeckungsartDaten?>? DeckungLesen { get; init; }

    /// <summary>Liefert die Stromträger des Projekts (Kühlträger).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? TraegerEintraege { get; init; }

    private DeckungsartDaten? DeckungOderNull => DeckungLesen?.Invoke();

    /// <summary>Schreibziel der Deckungsfelder; ohne Kältezeile nennt der Assistent den Grund.</summary>
    private DeckungsartDaten DeckungZumSchreiben
        => DeckungOderNull ?? throw new InvalidOperationException(WindowsFormsApplication1.MyResource.Resource.DECK_MSG_NUR_KAELTE);

    /// <summary>Deckungsart als Steuerwert (<c>zentral</c>/<c>split</c>); leer außerhalb einer Kältezeile.</summary>
    public string? Deckungsart
    {
        get => DeckungOderNull?.Deckungsart;
        set => DeckungZumSchreiben.DeckungSetzen(value ?? "");
    }

    /// <summary>Die Einträge der Deckungsart.</summary>
    public IReadOnlyList<KiWahleintrag> DeckungsartWahl => DeckungsartDaten.KiEintraege(DeckungsartDaten.Deckungsarten);

    /// <summary>EER-Weg als Steuerwert (<c>fest</c>/<c>linear</c>), nur bei dezentraler Deckung.</summary>
    public string? EerWeg
    {
        get => DeckungOderNull?.EerWeg;
        set => DeckungZumSchreiben.EerWegSetzen(value ?? "");
    }

    /// <summary>Die Einträge des EER-Wegs.</summary>
    public IReadOnlyList<KiWahleintrag> EerWegWahl => DeckungsartDaten.KiEintraege(DeckungsartDaten.EerWege);

    /// <summary>Fester EER bzw. EER am Punkt 1.</summary>
    public double? Eer1 { get => DeckungOderNull?.Eer1; set => DeckungZumSchreiben.Eer1 = value; }

    /// <summary>Außentemperatur am Punkt 1 [°C].</summary>
    public double? Taussen1 { get => DeckungOderNull?.Taussen1; set => DeckungZumSchreiben.Taussen1 = value; }

    /// <summary>EER am Punkt 2 (nur linear).</summary>
    public double? Eer2 { get => DeckungOderNull?.Eer2; set => DeckungZumSchreiben.Eer2 = value; }

    /// <summary>Außentemperatur am Punkt 2 [°C] (nur linear).</summary>
    public double? Taussen2 { get => DeckungOderNull?.Taussen2; set => DeckungZumSchreiben.Taussen2 = value; }

    /// <summary>Der Kühlträger (Stromträger des Projekts); leer = Stromträger des Projekts.</summary>
    public int? KuehlIdCarrier { get => DeckungOderNull?.KuehlIdCarrier; set => DeckungZumSchreiben.KuehlIdCarrier = value; }

    /// <summary>Die Stromträger des Projekts zur Wahl des Kühlträgers.</summary>
    public IReadOnlyList<KiWahleintrag> KuehlIdCarrierWahl
        => TraegerEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Eigener Zähler der Split-Geräte.</summary>
    public bool KuehlEigenerZaehler
    {
        get => DeckungOderNull?.KuehlEigenerZaehler ?? false;
        set => DeckungZumSchreiben.KuehlEigenerZaehler = value;
    }

    /// <summary>Der Bezeichner des markierten Profils.</summary>
    public string Profil => ProfilLesen?.Invoke() ?? "";

    /// <summary>Der Typ des markierten Profils aus seinem Kopfsatz.</summary>
    public string Typ => TypLesen?.Invoke() ?? "";

    /// <summary>Die Beschreibung des markierten Profils.</summary>
    public string Beschreibung => BeschreibungLesen?.Invoke() ?? "";

    /// <summary>Der Jahresverbrauch des markierten Profils, wie er dasteht.</summary>
    public string Jahresverbrauch => JahresverbrauchLesen?.Invoke() ?? "";

    /// <summary>Die Summe über alle zugeordneten Profile des Projekts.</summary>
    public string Summe => SummeLesen?.Invoke() ?? "";

    /// <summary>
    /// Welche Ausprägung offen ist: Prozesswärme, Stromverbraucher oder Brauchwasser.
    /// </summary>
    public string Bedarfsart => BedarfsartLesen?.Invoke() ?? "";
}
