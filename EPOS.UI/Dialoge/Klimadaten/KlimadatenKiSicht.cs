using KiKern;

namespace EPOS.UI.Dialoge.Klimadaten;

/// <summary>
/// Das FLACHE Abbild der Klimadatenmaske für den Hilfe-Assistenten (Welle KI‑F3).
///
/// <para><b>Warum ein Sichtmodell.</b> Diese Maske führt kein Daten-Objekt: Quelle,
/// Standort und die TRY-Angaben stehen in ihren lebenden Feldern, und erst der Knopf
/// „Daten einlesen" baut daraus einen <c>KlimaImportAuftrag</c>. Ein daran angemeldeter
/// Katalog hätte in ein Objekt geschrieben, das es zwischen zwei Läufen gar nicht
/// gibt.</para>
///
/// <para><b>Die beiden DATEIPFADE sind nur lesbar.</b> Getippt wird ein Pfad nicht, er
/// wird gewählt: Das Feld der Maske ist gesperrt, und der Plattformdialog setzt ihn.
/// Der Assistent liest ihn und sagt, welche Datei ansteht.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class KlimadatenKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? QuelleLesen { get; init; }
    public Action<int?>? QuelleSetzen { get; init; }

    public Func<string>? OrtsnameLesen { get; init; }
    public Action<string>? OrtsnameSetzen { get; init; }

    public Func<string>? BezeichnungLesen { get; init; }
    public Action<string>? BezeichnungSetzen { get; init; }

    public Func<double?>? LaengengradLesen { get; init; }
    public Action<double?>? LaengengradSetzen { get; init; }

    public Func<double?>? BreitengradLesen { get; init; }
    public Action<double?>? BreitengradSetzen { get; init; }

    public Func<int?>? JahrLesen { get; init; }
    public Action<int?>? JahrSetzen { get; init; }

    public Func<int?>? SzenarioLesen { get; init; }
    public Action<int?>? SzenarioSetzen { get; init; }

    public Func<string>? TryDateiLesen { get; init; }
    public Func<string>? TryPaketLesen { get; init; }

    /// <summary>Liefert die drei Datenquellen, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? QuelleEintraege { get; init; }

    /// <summary>Liefert die TRY-Jahre, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? JahrEintraege { get; init; }

    /// <summary>Liefert die drei TRY-Szenarien.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SzenarioEintraege { get; init; }

    /// <summary>PVGIS, TRY-Datei oder TRY-Paket (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> QuelleWahl
        => QuelleEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Das Bezugsjahr des Testreferenzjahres (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> JahrWahl
        => JahrEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Mittleres Jahr, sommerwarm oder winterkalt (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> SzenarioWahl
        => SzenarioEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Woher die Klimadaten kommen: aus dem PVGIS-Dienst, aus einer einzelnen
    /// TRY-Datei oder aus einem TRY-Paket. Sie entscheidet, welche Felder darunter
    /// dastehen.
    /// </summary>
    public int? Quelle
    {
        get => QuelleLesen?.Invoke();
        set => QuelleSetzen?.Invoke(value);
    }

    /// <summary>Der Ortsname, zu dem die Koordinaten gesucht werden.</summary>
    public string Ortsname
    {
        get => OrtsnameLesen?.Invoke() ?? "";
        set => OrtsnameSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Name, unter dem die Klimaregion gespeichert wird.</summary>
    public string Bezeichnung
    {
        get => BezeichnungLesen?.Invoke() ?? "";
        set => BezeichnungSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Längengrad des Standorts, in Dezimalgrad.</summary>
    public double? Laengengrad
    {
        get => LaengengradLesen?.Invoke();
        set => LaengengradSetzen?.Invoke(value);
    }

    /// <summary>Der Breitengrad des Standorts, in Dezimalgrad.</summary>
    public double? Breitengrad
    {
        get => BreitengradLesen?.Invoke();
        set => BreitengradSetzen?.Invoke(value);
    }

    /// <summary>Das Bezugsjahr des Testreferenzjahres.</summary>
    public int? Jahr
    {
        get => JahrLesen?.Invoke();
        set => JahrSetzen?.Invoke(value);
    }

    /// <summary>Das Szenario des Testreferenzjahres als Platz in der Klappliste.</summary>
    public int? Szenario
    {
        get => SzenarioLesen?.Invoke();
        set => SzenarioSetzen?.Invoke(value);
    }

    /// <summary>Die gewählte TRY-Datei; sie kommt aus dem Dateidialog der Plattform.</summary>
    public string TryDatei => TryDateiLesen?.Invoke() ?? "";

    /// <summary>Das gewählte TRY-Paket; es kommt aus dem Dateidialog der Plattform.</summary>
    public string TryPaket => TryPaketLesen?.Invoke() ?? "";
}
