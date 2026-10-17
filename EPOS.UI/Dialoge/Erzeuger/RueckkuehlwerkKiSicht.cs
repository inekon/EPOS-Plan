using KiKern;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Das FLACHE Abbild der Verwaltung „Rückkühlwerke" für den Hilfe-Assistenten (K-F5a).
///
/// <para><b>Das Rückkühlwerk ist eine WAHL</b> — die Satzwahl der Verwaltung: Ein Setzen wählt die Zeile der Liste,
/// denselben Weg wie ein Klick. Der Schlüssel ist die Id des Satzes.</para>
///
/// <para><b>Die Felder</b> schreiben in den ARBEITSSTAND des Stammblatts; geschrieben wird mit „Speichern". Bauart, Weg
/// der Annäherung, Ventilatorregelung und Schaltung der freien Kühlung sind Wahlen über den Persistenzwert, nie über
/// den Anzeigetext. Einen Auslieferungssatz schützt der Haken <c>Schreibgeschuetzt</c> der Maske.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren Delegaten.</para>
/// </summary>
public sealed class RueckkuehlwerkKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? SatzLesen { get; init; }
    public Action<string>? SatzSetzen { get; init; }

    /// <summary>Liefert die Zeilen der Liste (Schlüssel = Id, Text = Name).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SatzEintraege { get; init; }

    public Func<RueckkuehlwerkDaten?>? ArbeitLesen { get; init; }

    /// <summary>Meldet eine Änderung am Arbeitsstand — derselbe Nachzug wie nach einer Eingabe von Hand.</summary>
    public Action? Geaendert { get; init; }

    /// <summary>Die Wertemengen der vier Wahlen (Persistenzwert, Anzeigetext).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BauartEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? WegEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? RegelungEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? SchaltungEintraege { get; init; }

    private static IReadOnlyList<KiWahleintrag> Liste(Func<IReadOnlyList<KiWahleintrag>>? f)
        => f?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Die Rückkühlwerke der Liste.</summary>
    public IReadOnlyList<KiWahleintrag> RueckkuehlwerkWahl => Liste(SatzEintraege);

    /// <summary>Das gewählte Rückkühlwerk (Schlüssel = Id). Ein Setzen WÄHLT die Zeile.</summary>
    public string Rueckkuehlwerk
    {
        get => SatzLesen?.Invoke() ?? "";
        set => SatzSetzen?.Invoke(value ?? "");
    }

    public string Bezeichner
    {
        get => ArbeitLesen?.Invoke()?.Bezeichner ?? "";
        set => Setzen(d => d.Bezeichner = value ?? "");
    }

    public string Beschreibung
    {
        get => ArbeitLesen?.Invoke()?.Beschreibung ?? "";
        set => Setzen(d => d.Beschreibung = value ?? "");
    }

    public IReadOnlyList<KiWahleintrag> BauartWahl => Liste(BauartEintraege);

    /// <summary>Die Bauart als Persistenzwert; leer = trocken.</summary>
    public string Bauart
    {
        get => ArbeitLesen?.Invoke()?.Bauart ?? "";
        set => Setzen(d => d.Bauart = value ?? "");
    }

    public double? Nennleistung
    {
        get => ArbeitLesen?.Invoke()?.NennleistungKw;
        set => Setzen(d => d.NennleistungKw = value);
    }

    public double? AnnaeherungNenn
    {
        get => ArbeitLesen?.Invoke()?.AnnaeherungNennK;
        set => Setzen(d => d.AnnaeherungNennK = value);
    }

    public IReadOnlyList<KiWahleintrag> AnnaeherungWegWahl => Liste(WegEintraege);

    public string AnnaeherungWeg
    {
        get => ArbeitLesen?.Invoke()?.AnnaeherungWeg ?? "";
        set => Setzen(d => d.AnnaeherungWeg = value ?? "");
    }

    public double? VentilatorNenn
    {
        get => ArbeitLesen?.Invoke()?.VentilatorNennKw;
        set => Setzen(d => d.VentilatorNennKw = value);
    }

    public IReadOnlyList<KiWahleintrag> VentilatorRegelungWahl => Liste(RegelungEintraege);

    public string VentilatorRegelung
    {
        get => ArbeitLesen?.Invoke()?.VentilatorRegelung ?? "";
        set => Setzen(d => d.VentilatorRegelung = value ?? "");
    }

    public int? VentilatorStufen
    {
        get => ArbeitLesen?.Invoke()?.VentilatorStufen;
        set => Setzen(d => d.VentilatorStufen = value);
    }

    public double? VentilatorDrehzahlMin
    {
        get => ArbeitLesen?.Invoke()?.VentilatorDrehzahlMin;
        set => Setzen(d => d.VentilatorDrehzahlMin = value);
    }

    public double? BefeuchtungWirkungsgrad
    {
        get => ArbeitLesen?.Invoke()?.BefeuchtungWirkungsgrad;
        set => Setzen(d => d.BefeuchtungWirkungsgrad = value);
    }

    public double? BefeuchtungAb
    {
        get => ArbeitLesen?.Invoke()?.BefeuchtungAbC;
        set => Setzen(d => d.BefeuchtungAbC = value);
    }

    public double? VerdunstungFaktor
    {
        get => ArbeitLesen?.Invoke()?.VerdunstungFaktor;
        set => Setzen(d => d.VerdunstungFaktor = value);
    }

    public double? Eindickung
    {
        get => ArbeitLesen?.Invoke()?.Eindickung;
        set => Setzen(d => d.Eindickung = value);
    }

    public double? DriftAnteil
    {
        get => ArbeitLesen?.Invoke()?.DriftAnteil;
        set => Setzen(d => d.DriftAnteil = value);
    }

    public IReadOnlyList<KiWahleintrag> FreikuehlungSchaltungWahl => Liste(SchaltungEintraege);

    public string FreikuehlungSchaltung
    {
        get => ArbeitLesen?.Invoke()?.FreikuehlungSchaltung ?? "";
        set => Setzen(d => d.FreikuehlungSchaltung = value ?? "");
    }

    private void Setzen(Action<RueckkuehlwerkDaten> schritt)
    {
        RueckkuehlwerkDaten? d = ArbeitLesen?.Invoke();
        if (d is null) return;
        schritt(d);
        Geaendert?.Invoke();
    }

    // =====================================================================
    //  Der Ladestand der Fangstelle
    // =====================================================================

    /// <summary>Liefert die Bannertexte der Fangstelle (<c>Katalogladung</c>); leer = alles geladen.</summary>
    public Func<string>? LadestandLesen { get; init; }

    /// <summary>Was der Dialog nicht lesen konnte, im Wortlaut seines Warnbanners — nur lesbar.</summary>
    public string Ladestand => LadestandLesen?.Invoke() ?? "";
}
