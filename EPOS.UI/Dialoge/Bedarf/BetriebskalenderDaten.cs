namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// EIN Betriebskalender der Bedarfsprofile als DTO der Oberfläche (Entscheidungsvorlage
/// Modellgrenzen PW2, BW2) — das Abbild einer Zeile von <c>Tab_Betriebskalender</c>. Die Hülle
/// übersetzt zwischen ihm und dem Modell des Kerns; die Regeln stehen im Kern.
///
/// <para><b>Die Ferien</b> stehen als vier Paare Jahrestag 1 … 365 im Gemeinjahr da (Beginn nach Ende =
/// über den Jahreswechsel); ein leeres Paar ist kein Zeitraum. Weitere Zeiträume stehen in der Liste
/// <see cref="Liste"/> (<c>Tab_Betriebskalenderferien</c>), wie die Ferienliste des Gebäudes. Der Ferienfaktor steht in
/// Prozent.</para>
/// </summary>
public sealed class BetriebskalenderDaten
{
    /// <summary>Zahl der Ferienzeiträume.</summary>
    public const int FERIEN = 4;

    /// <summary>Schlüssel; 0 = neu.</summary>
    public int Id { get; set; }

    /// <summary>Bezeichnung.</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Länderkennung; <c>null</c> = nur die bundeseinheitlichen Feiertage.</summary>
    public string? Bundesland { get; set; }

    /// <summary>Beginn der Ferienzeiträume (Jahrestag).</summary>
    public int?[] Von { get; set; } = new int?[FERIEN];

    /// <summary>Ende der Ferienzeiträume (Jahrestag).</summary>
    public int?[] Bis { get; set; } = new int?[FERIEN];

    /// <summary>Die weiteren Betriebsferien in ihrer Reihenfolge (nach den vier Paaren).</summary>
    public List<BetriebsferienZeile> Liste { get; set; } = new();

    /// <summary>Ferienfaktor in Prozent des Tagesmittels (0 … 100).</summary>
    public double FerienfaktorProzent { get; set; }

    /// <summary>Feiertag wie Sonntag (Vorgabe ja).</summary>
    public bool FeiertagWieSonntag { get; set; } = true;

    /// <summary>Ferien wie Sonntag (Vorgabe nein); gesetzt wirkt der Ferienfaktor nicht.</summary>
    public bool FerienWieSonntag { get; set; }

    /// <summary>Ferien kürzen die Monatsmenge.</summary>
    public bool FerienKuerzen { get; set; }

    /// <summary>Eine Kopie mit eigenen Feldern.</summary>
    public BetriebskalenderDaten Kopie() => new()
    {
        Id = Id,
        Bezeichner = Bezeichner,
        Bundesland = Bundesland,
        Von = (int?[])Von.Clone(),
        Bis = (int?[])Bis.Clone(),
        Liste = Liste.Select(z => z.Kopie()).ToList(),
        FerienfaktorProzent = FerienfaktorProzent,
        FeiertagWieSonntag = FeiertagWieSonntag,
        FerienWieSonntag = FerienWieSonntag,
        FerienKuerzen = FerienKuerzen
    };
}

/// <summary>
/// Eine Zeile der Liste „Weitere Betriebsferien“: wahlfreier Name und Jahrestage 1 … 365 im Gemeinjahr; eine Zeile ohne
/// beide Grenzen ist kein Zeitraum, eine halbe ist eine Fehleingabe.
/// </summary>
public sealed class BetriebsferienZeile
{
    /// <summary>Wahlfreier Name.</summary>
    public string? Name { get; set; }

    /// <summary>Beginn (Jahrestag).</summary>
    public int? Von { get; set; }

    /// <summary>Ende (Jahrestag).</summary>
    public int? Bis { get; set; }

    /// <summary>Eine Kopie.</summary>
    public BetriebsferienZeile Kopie() => new() { Name = Name, Von = Von, Bis = Bis };
}

/// <summary>
/// Ein Projektgebäude als Quelle für „Aus Gebäude übernehmen…“ (Konzept Kältebedarfsprofile 7.2): Feiertagsland und
/// alle Ferienzeiträume des Gebäudes. Der Kalender übernimmt eine KOPIE — die Herkunft steht nie als ID am Kalender.
/// </summary>
/// <param name="Bezeichner">Der Gebäudename.</param>
/// <param name="Bundesland">Länderkennung; <c>null</c> = nur die bundeseinheitlichen Feiertage.</param>
/// <param name="Ferien">Die Ferienzeiträume als Jahrestage (Beginn, Ende) in ihrer Reihenfolge.</param>
public sealed record BetriebskalenderGebaeude(string Bezeichner, string? Bundesland, IReadOnlyList<(int Von, int Bis)> Ferien);

/// <summary>Ein Bundesland zur Wahl: Kennung und Name in der Oberflächensprache.</summary>
/// <param name="Kennung">BW … TH.</param>
/// <param name="Name">Klartext.</param>
public sealed record Bundeslandeintrag(string Kennung, string Name);
