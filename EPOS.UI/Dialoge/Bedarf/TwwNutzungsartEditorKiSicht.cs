using KiKern;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das flache Abbild des Editors einer Brauchwasser-Nutzungsart für den Hilfe-Assistenten
/// (Umsetzungskonzept Zapfprofilgenerator 5.4; Stufe Z4, Gruppe 3).
///
/// <para><b>Eine ÜBERLAGERUNG, die zum Katalog gehört.</b> Ihr OK schreibt die Nutzungsart in
/// EINER Transaktion — das bleibt der Klick des Anwenders; einen Speicherweg meldet sie nicht an.
/// Setzen heißt dasselbe wie Tippen: dieselben Grenzen wie die Felder (Bedarf und Bandbreite,
/// Ferienfaktor und Monatsfaktoren nicht negativ), eine Wahl nur aus ihrer Liste. Die
/// Wochenfaktoren stehen nicht darin — sie bearbeitet „Tagesgang…". Die Sicht hält keinen
/// Zustand.</para>
/// </summary>
public sealed class TwwNutzungsartEditorKiSicht
{
    // Die Namen der Zahlenfelder — der Editor löst sie in seinen Lese- und Setzweg auf.
    public const string BEDARF_NIEDRIG = "BedarfNiedrig";
    public const string BEDARF_MITTEL = "BedarfMittel";
    public const string BEDARF_HOCH = "BedarfHoch";
    public const string BEDARF_NIEDRIG_MIN = "BedarfNiedrigMin";
    public const string BEDARF_NIEDRIG_MAX = "BedarfNiedrigMax";
    public const string BEDARF_MITTEL_MIN = "BedarfMittelMin";
    public const string BEDARF_MITTEL_MAX = "BedarfMittelMax";
    public const string BEDARF_HOCH_MIN = "BedarfHochMin";
    public const string BEDARF_HOCH_MAX = "BedarfHochMax";
    public const string ZAPFTEMPERATUR = "Zapftemperatur";
    public const string KALTWASSER = "Kaltwasser";
    public const string FERIENFAKTOR = "Ferienfaktor";

    public Func<string>? BezeichnerLesen { get; init; }
    public Func<string, string?>? BezeichnerSetzen { get; init; }
    public Func<string>? KatalogversionLesen { get; init; }
    public Func<string, string?>? KatalogversionSetzen { get; init; }

    public Func<int?>? BezugsartLesen { get; init; }
    public Func<int?, string?>? BezugsartSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? BezugsartEintraege { get; init; }

    public Func<int?>? TagesgangsatzLesen { get; init; }
    public Func<int?, string?>? TagesgangsatzSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? TagesgangsatzEintraege { get; init; }

    public Func<int?>? BilanzgrenzeLesen { get; init; }
    public Func<int?, string?>? BilanzgrenzeSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? BilanzgrenzeEintraege { get; init; }

    public Func<int?>? KalenderLesen { get; init; }
    public Func<int?, string?>? KalenderSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? KalenderEintraege { get; init; }

    /// <summary>Liest ein Zahlenfeld nach seinem Namen (Konstanten oben).</summary>
    public Func<string, double?>? ZahlLesen { get; init; }

    /// <summary>Setzt ein Zahlenfeld nach seinem Namen; Rückgabe der Grund einer Ablehnung.</summary>
    public Func<string, double?, string?>? ZahlSetzen { get; init; }

    public Func<double?[]?>? MonateLesen { get; init; }
    public Func<double?[]?, string?>? MonateSetzen { get; init; }

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Der Bezeichner der Nutzungsart.</summary>
    public string Bezeichner
    {
        get => BezeichnerLesen?.Invoke() ?? "";
        set => ZapfprofilKiRegeln.Setze(BezeichnerSetzen, value ?? "");
    }

    /// <summary>Die Katalogversion — mit dem Bezeichner der natürliche Schlüssel.</summary>
    public string Katalogversion
    {
        get => KatalogversionLesen?.Invoke() ?? "";
        set => ZapfprofilKiRegeln.Setze(KatalogversionSetzen, value ?? "");
    }

    /// <summary>Die Bezugsart (Id).</summary>
    public int? Bezugsart
    {
        get => BezugsartLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(BezugsartSetzen, value);
    }

    /// <summary>Die Bezugsarten der Klappliste.</summary>
    public IReadOnlyList<KiWahleintrag> BezugsartWahl => BezugsartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Der Tagesgangsatz (Id eines vollständigen Satzes).</summary>
    public int? Tagesgangsatz
    {
        get => TagesgangsatzLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(TagesgangsatzSetzen, value);
    }

    /// <summary>Die vollständigen Tagesgangsätze des Katalogs.</summary>
    public IReadOnlyList<KiWahleintrag> TagesgangsatzWahl => TagesgangsatzEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Bilanzgrenze (Id).</summary>
    public int? Bilanzgrenze
    {
        get => BilanzgrenzeLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(BilanzgrenzeSetzen, value);
    }

    public IReadOnlyList<KiWahleintrag> BilanzgrenzeWahl => BilanzgrenzeEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Kalenderart (Id).</summary>
    public int? Kalender
    {
        get => KalenderLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(KalenderSetzen, value);
    }

    public IReadOnlyList<KiWahleintrag> KalenderWahl => KalenderEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    public double? BedarfNiedrig { get => Zahl(BEDARF_NIEDRIG); set => Zahl(BEDARF_NIEDRIG, value); }
    public double? BedarfMittel { get => Zahl(BEDARF_MITTEL); set => Zahl(BEDARF_MITTEL, value); }
    public double? BedarfHoch { get => Zahl(BEDARF_HOCH); set => Zahl(BEDARF_HOCH, value); }
    public double? BedarfNiedrigMin { get => Zahl(BEDARF_NIEDRIG_MIN); set => Zahl(BEDARF_NIEDRIG_MIN, value); }
    public double? BedarfNiedrigMax { get => Zahl(BEDARF_NIEDRIG_MAX); set => Zahl(BEDARF_NIEDRIG_MAX, value); }
    public double? BedarfMittelMin { get => Zahl(BEDARF_MITTEL_MIN); set => Zahl(BEDARF_MITTEL_MIN, value); }
    public double? BedarfMittelMax { get => Zahl(BEDARF_MITTEL_MAX); set => Zahl(BEDARF_MITTEL_MAX, value); }
    public double? BedarfHochMin { get => Zahl(BEDARF_HOCH_MIN); set => Zahl(BEDARF_HOCH_MIN, value); }
    public double? BedarfHochMax { get => Zahl(BEDARF_HOCH_MAX); set => Zahl(BEDARF_HOCH_MAX, value); }
    public double? Zapftemperatur { get => Zahl(ZAPFTEMPERATUR); set => Zahl(ZAPFTEMPERATUR, value); }
    public double? Kaltwasser { get => Zahl(KALTWASSER); set => Zahl(KALTWASSER, value); }
    public double? Ferienfaktor { get => Zahl(FERIENFAKTOR); set => Zahl(FERIENFAKTOR, value); }

    /// <summary>Die zwölf Monatsfaktoren Januar bis Dezember (Mittel 1; der Kern normiert beim Speichern).</summary>
    public double?[]? Monatsfaktoren
    {
        get => MonateLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(MonateSetzen, value);
    }

    private double? Zahl(string feld) => ZahlLesen?.Invoke(feld);

    private void Zahl(string feld, double? wert)
    {
        string? grund = ZahlSetzen is null ? Resource.KI_SIM_KEIN_SCHREIBWEG : ZahlSetzen(feld, wert);
        if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
    }
}
