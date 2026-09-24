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
