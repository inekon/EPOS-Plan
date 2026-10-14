namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// EIN Betriebskalender der Bedarfsprofile als DTO der Oberfläche (Entscheidungsvorlage
/// Modellgrenzen PW2, BW2) — das Abbild einer Zeile von <c>Tab_Betriebskalender</c>. Die Hülle
/// übersetzt zwischen ihm und dem Modell des Kerns; die Regeln stehen im Kern.
///
/// <para><b>Die Ferien</b> stehen als vier Paare Jahrestag 1 … 365 im Gemeinjahr da (Beginn nach Ende =
/// über den Jahreswechsel); ein leeres Paar ist kein Zeitraum. Der Ferienfaktor steht in Prozent.</para>
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

    /// <summary>Ferienfaktor in Prozent des Tagesmittels (0 … 100).</summary>
    public double FerienfaktorProzent { get; set; }

    /// <summary>Feiertag wie Sonntag (Vorgabe ja).</summary>
    public bool FeiertagWieSonntag { get; set; } = true;

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
        FerienfaktorProzent = FerienfaktorProzent,
        FeiertagWieSonntag = FeiertagWieSonntag,
        FerienKuerzen = FerienKuerzen
    };
}

/// <summary>Ein Bundesland zur Wahl: Kennung und Name in der Oberflächensprache.</summary>
/// <param name="Kennung">BW … TH.</param>
/// <param name="Name">Klartext.</param>
public sealed record Bundeslandeintrag(string Kennung, string Name);
