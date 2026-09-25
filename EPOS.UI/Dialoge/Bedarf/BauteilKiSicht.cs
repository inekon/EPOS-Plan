using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild des <see cref="BauteilDialog"/> für den Hilfe-Assistenten (Gebäudesimulation
/// G3, Welle D2). Sie hält keinen Zustand: Jede Eigenschaft ruft bei jedem Zugriff ihren Delegaten,
/// den der Dialog beim Anmelden setzt.
///
/// <para><b>Bauteilart, Randbedingung und Aufbau sind WAHLFELDER</b> (KI‑D‑Q6): Ihre Schlüssel sind
/// die Listenplätze bzw. Ids der Maske; die Persistenzwerte dahinter setzt der Dialog.</para>
/// </summary>
public sealed class BauteilKiSicht
{
    public Func<int?>? ArtLesen { get; init; }
    public Action<int?>? ArtSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? ArtEintraege { get; init; }

    public Func<string>? BezeichnungLesen { get; init; }
    public Action<string>? BezeichnungSetzen { get; init; }

    public Func<double?>? FlaecheLesen { get; init; }
    public Action<double?>? FlaecheSetzen { get; init; }

    public Func<double?>? AzimutLesen { get; init; }
    public Action<double?>? AzimutSetzen { get; init; }

    public Func<double?>? NeigungLesen { get; init; }
    public Action<double?>? NeigungSetzen { get; init; }

    public Func<int?>? RandLesen { get; init; }
    public Action<int?>? RandSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? RandEintraege { get; init; }

    public Func<double?>? GWertLesen { get; init; }
    public Action<double?>? GWertSetzen { get; init; }

    public Func<double?>? RahmenLesen { get; init; }
    public Action<double?>? RahmenSetzen { get; init; }

    public Func<double?>? VerschattungLesen { get; init; }
    public Action<double?>? VerschattungSetzen { get; init; }

    public Func<double?>? PsiLLesen { get; init; }
    public Action<double?>? PsiLSetzen { get; init; }

    public Func<double?>? UWertLesen { get; init; }
    public Action<double?>? UWertSetzen { get; init; }

    public Func<int?>? AufbauLesen { get; init; }
    public Action<int?>? AufbauSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? AufbauEintraege { get; init; }

    /// <summary>Die neun Bauteilarten.</summary>
    public IReadOnlyList<KiWahleintrag> ArtWahl => ArtEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Randbedingungen samt „keine Angabe" (Vorgabe nach Bauteilart).</summary>
    public IReadOnlyList<KiWahleintrag> RandbedingungWahl => RandEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Aufbauten des Projekts samt „ohne Aufbau".</summary>
    public IReadOnlyList<KiWahleintrag> AufbauWahl => AufbauEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Bauteilart als Listenplatz.</summary>
    public int? Art { get => ArtLesen?.Invoke(); set => ArtSetzen?.Invoke(value); }

    /// <summary>Der Name des Bauteils.</summary>
    public string Bezeichnung { get => BezeichnungLesen?.Invoke() ?? ""; set => BezeichnungSetzen?.Invoke(value); }

    /// <summary>Fläche [m²].</summary>
    public double? Flaeche { get => FlaecheLesen?.Invoke(); set => FlaecheSetzen?.Invoke(value); }

    /// <summary>Azimut [°], 0° = Nord.</summary>
    public double? Azimut { get => AzimutLesen?.Invoke(); set => AzimutSetzen?.Invoke(value); }

    /// <summary>Neigung [°]; leer = Vorgabe nach Bauteilart.</summary>
    public double? Neigung { get => NeigungLesen?.Invoke(); set => NeigungSetzen?.Invoke(value); }

    /// <summary>Die Randbedingung als Listenplatz (0 = keine Angabe).</summary>
    public int? Randbedingung { get => RandLesen?.Invoke(); set => RandSetzen?.Invoke(value); }

    /// <summary>g-Wert (Fenster, Vorhangfassade); leer = Wert des Gebäudes.</summary>
    public double? GWert { get => GWertLesen?.Invoke(); set => GWertSetzen?.Invoke(value); }

    /// <summary>Rahmenanteil (Fenster, Vorhangfassade); leer = Wert des Gebäudes.</summary>
    public double? Rahmenanteil { get => RahmenLesen?.Invoke(); set => RahmenSetzen?.Invoke(value); }

    /// <summary>Verschattungsfaktor (Fenster, Vorhangfassade); leer = Wert des Gebäudes.</summary>
    public double? Verschattung { get => VerschattungLesen?.Invoke(); set => VerschattungSetzen?.Invoke(value); }

    /// <summary>Wärmebrücke ψ·L [W/K].</summary>
    public double? PsiL { get => PsiLLesen?.Invoke(); set => PsiLSetzen?.Invoke(value); }

    /// <summary>U-Wert [W/(m²K)]; leer = aus dem Aufbau.</summary>
    public double? UWert { get => UWertLesen?.Invoke(); set => UWertSetzen?.Invoke(value); }

    /// <summary>Der Aufbau des Projekts als Id (0 = ohne Aufbau).</summary>
    public int? Aufbau { get => AufbauLesen?.Invoke(); set => AufbauSetzen?.Invoke(value); }
}
