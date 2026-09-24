using System.Globalization;
using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das flache Abbild der Überlagerung „Bedarfstag konstruieren" für den Hilfe-Assistenten
/// (Welle #458, Stufe 3a).
///
/// <para><b>Eine ÜBERLAGERUNG mit eigenem Arbeitsstand.</b> Der Konstruktor bearbeitet
/// Kopien der Zeilen; „OK" baut den Tag über den Delegaten der Hülle und gibt ihn als Entwurf
/// in die Auslegung — geschrieben wird nichts, und einen Speicherweg meldet er nicht an.</para>
///
/// <para><b>Die Zeilen sind SPALTEN</b> (<see cref="Zeilen"/>) mit Zeitfenster und
/// Verbraucher als Kennzeichen. Was eine Zeile nicht bedienbar zeigt — die Anzahl ohne
/// Zapfregel, Volumen und Zapftemperatur mit einer —, ist auch hier nicht setzbar; die
/// Absage nennt, was es bedienbar macht. Die Sicht hält keinen Zustand.</para>
/// </summary>
public sealed class BedarfstagKonstruktorKiSicht
{
    public Func<string>? NameLesen { get; init; }
    public Func<string, string?>? NameSetzen { get; init; }

    /// <summary>Die Zeilen der Tabelle — je Zeile eine, frisch bei jedem Zugriff.</summary>
    public Func<IReadOnlyList<BedarfstagKonstruktorKiZeile>>? ZeilenLesen { get; init; }

    /// <summary>Die Einträge der Regelwahl: „Volumen direkt" und die Zapfregeln des Katalogs.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? RegelEintraege { get; init; }

    /// <summary>„Volumen direkt" und die Zapfregeln — die Wahl der Spalte „Zapfregel".</summary>
    public IReadOnlyList<KiWahleintrag> RegelWahl => RegelEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // Der Bezug des Tags (Schritt 124, N10 (j); Zapfprofil Z4, Gruppe 2b)
    public Func<int?>? BezugsartLesen { get; init; }
    public Func<int?, string?>? BezugsartSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? BezugsartEintraege { get; init; }
    public Func<double?>? BezugsmengeLesen { get; init; }
    public Func<double?, string?>? BezugsmengeSetzen { get; init; }

    /// <summary>„ohne Bezug" (0) und die Bezugsarten des Schemas.</summary>
    public IReadOnlyList<KiWahleintrag> BezugsartWahl => BezugsartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Bezugsart des Tags; 0 = ohne Bezug (dann fällt auch die Menge weg).</summary>
    public int? Bezugsart
    {
        get => BezugsartLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(BezugsartSetzen, value);
    }

    /// <summary>Die Bezugsmenge des Tags (größer 0) — nur mit einer Bezugsart.</summary>
    public double? Bezugsmenge
    {
        get => BezugsmengeLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(BezugsmengeSetzen, value);
    }

    /// <summary>Der Name des Bedarfstags.</summary>
    public string Name
    {
        get => NameLesen?.Invoke() ?? "";
        set => ZapfprofilKiRegeln.Setze(NameSetzen, value);
    }

    /// <summary>Die Zeilen des Konstruktors.</summary>
    public IReadOnlyList<BedarfstagKonstruktorKiZeile> Zeilen
        => ZeilenLesen?.Invoke() ?? Array.Empty<BedarfstagKonstruktorKiZeile>();
}

/// <summary>
/// Eine Zeile des Konstruktors als ZEILE der Sichtklasse
/// <see cref="BedarfstagKonstruktorKiSicht"/>: Sie liest die Zeile selbst und setzt über die
/// Wege des Konstruktors, die Zulässigkeit und Grenzen prüfen.
/// </summary>
public sealed class BedarfstagKonstruktorKiZeile
{
    private readonly ZapfprofilKonstruktorZeileDaten _zeile;

    /// <summary>Legt die Zeile zu einer Zeile des Arbeitsstands an.</summary>
    public BedarfstagKonstruktorKiZeile(ZapfprofilKonstruktorZeileDaten zeile)
    {
        _zeile = zeile ?? throw new ArgumentNullException(nameof(zeile));
    }

    public Func<double?, string?>? BeginnSetzen { get; init; }
    public Func<double?, string?>? EndeSetzen { get; init; }
    public Func<int>? RegelLesen { get; init; }
    public Func<int?, string?>? RegelSetzen { get; init; }
    public Func<double?, string?>? AnzahlSetzen { get; init; }
    public Func<double?, string?>? VolumenSetzen { get; init; }
    public Func<double?, string?>? TemperaturSetzen { get; init; }
    public Func<string, string?>? VerbraucherSetzen { get; init; }

    /// <summary>
    /// Das ZEILENKENNZEICHEN: das Zeitfenster und, wenn genannt, der Verbraucher —
    /// „6–8 h · Küche".
    /// </summary>
    public string Kennzeichen
    {
        get
        {
            string fenster = Zahl(_zeile.BeginnH) + "–" + Zahl(_zeile.EndeH) + " h";
            string verbraucher = (_zeile.Verbraucher ?? "").Trim();
            return verbraucher.Length == 0 ? fenster : fenster + " · " + verbraucher;
        }
    }

    /// <summary>Beginn des Fensters [h].</summary>
    public double? Beginn
    {
        get => _zeile.BeginnH;
        set => ZapfprofilKiRegeln.Setze(BeginnSetzen, value);
    }

    /// <summary>Ende des Fensters [h].</summary>
    public double? Ende
    {
        get => _zeile.EndeH;
        set => ZapfprofilKiRegeln.Setze(EndeSetzen, value);
    }

    /// <summary>Die Zapfregel als Platz der Regelwahl: 0 = Volumen direkt, 1 … n = die Regeln.</summary>
    public int? Regel
    {
        get => RegelLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(RegelSetzen, value);
    }

    /// <summary>Die Anzahl der Vorgänge der Regel.</summary>
    public double? Anzahl
    {
        get => _zeile.Anzahl;
        set => ZapfprofilKiRegeln.Setze(AnzahlSetzen, value);
    }

    /// <summary>Das Volumen [l] ohne Regel.</summary>
    public double? Volumen
    {
        get => _zeile.VolumenL;
        set => ZapfprofilKiRegeln.Setze(VolumenSetzen, value);
    }

    /// <summary>Die Zapftemperatur [°C] ohne Regel.</summary>
    public double? Temperatur
    {
        get => _zeile.ZapftemperaturC;
        set => ZapfprofilKiRegeln.Setze(TemperaturSetzen, value);
    }

    /// <summary>Der Verbraucher — ein neutraler Name der Zapfstelle.</summary>
    public string Verbraucher
    {
        get => _zeile.Verbraucher ?? "";
        set => ZapfprofilKiRegeln.Setze(VerbraucherSetzen, value);
    }

    private static string Zahl(double? wert)
        => wert is double w ? w.ToString("0.##", CultureInfo.CurrentCulture) : "–";
}
