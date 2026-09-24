using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das flache Abbild der Überlagerung „Tagesgang bearbeiten" für den Hilfe-Assistenten
/// (Umsetzungskonzept Zapfprofilgenerator 5.1; Stufe Z4, Gruppe 2b).
///
/// <para><b>Eine ÜBERLAGERUNG, die zum Katalog gehört.</b> Ihr OK schreibt den Tagesgang in
/// EINER Transaktion — das bleibt der Klick des Anwenders; einen Speicherweg meldet sie nicht
/// an.</para>
///
/// <para><b>Die 24 Stundenanteile des gezeigten Tagtyps sind EINE Zahlenreihe</b>
/// (<see cref="Stunden"/>, in Prozent, Stunde 1 bis 24), die Wochenfaktoren eine zweite
/// (<see cref="Wochenfaktoren"/>, Montag bis Sonntag). Setzen heißt dasselbe wie Tippen;
/// gesperrt ist, was die Maske nur lesbar zeigt (eine gesperrte Nutzungsart vor „Als eigene
/// Kopie bearbeiten…"). Die Vorlage ist eine Wahl — „Vorlage laden", „Normieren" und
/// „Zurücksetzen" bleiben Klicks des Anwenders. Die Sicht hält keinen Zustand.</para>
/// </summary>
public sealed class TagesgangEditorKiSicht
{
    public Func<int?>? TagtypLesen { get; init; }
    public Func<int?, string?>? TagtypSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? TagtypEintraege { get; init; }

    public Func<double?[]?>? StundenLesen { get; init; }
    public Func<double?[]?, string?>? StundenSetzen { get; init; }

    public Func<double?[]?>? WocheLesen { get; init; }
    public Func<double?[]?, string?>? WocheSetzen { get; init; }

    public Func<int?>? VorlageLesen { get; init; }
    public Func<int?, string?>? VorlageSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? VorlageEintraege { get; init; }

    public Func<string>? KatalogversionLesen { get; init; }
    public Func<string, string?>? KatalogversionSetzen { get; init; }

    /// <summary>Werktag, Samstag, Sonn-/Feiertag, Ruhetag.</summary>
    public IReadOnlyList<KiWahleintrag> TagtypWahl => TagtypEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Tagesgangsätze des Katalogs; ein unvollständiger nennt beim Setzen seinen Grund.</summary>
    public IReadOnlyList<KiWahleintrag> VorlageWahl => VorlageEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Der gezeigte Tagtyp (0 = Werktag … 3 = Ruhetag).</summary>
    public int? Tagtyp
    {
        get => TagtypLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(TagtypSetzen, value);
    }

    /// <summary>Die 24 Stundenanteile des gezeigten Tagtyps [%].</summary>
    public double?[]? Stunden
    {
        get => StundenLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(StundenSetzen, value);
    }

    /// <summary>Die sieben Wochenfaktoren Montag bis Sonntag [%].</summary>
    public double?[]? Wochenfaktoren
    {
        get => WocheLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(WocheSetzen, value);
    }

    /// <summary>Der Tagesgangsatz, den „Vorlage laden" übernimmt; leer = keiner.</summary>
    public int? Vorlage
    {
        get => VorlageLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(VorlageSetzen, value);
    }

    /// <summary>Die Katalogversion der eigenen Kopie — nur bei einer gesperrten Nutzungsart im Kopiermodus.</summary>
    public string Katalogversion
    {
        get => KatalogversionLesen?.Invoke() ?? "";
        set => ZapfprofilKiRegeln.Setze(KatalogversionSetzen, value);
    }
}
