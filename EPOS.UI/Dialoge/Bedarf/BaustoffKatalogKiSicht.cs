using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Verwaltung „Baustoffe" für den Hilfe-Assistenten (Gebäudesimulation
/// G3, Welle C).
///
/// <para><b>Der Baustoff ist eine WAHL, keine Eingabe</b> — die Satzwahl der Verwaltung: Ein
/// Setzen wählt die Zeile der Liste, denselben Weg wie ein Klick; geänderte Werte halten die
/// Wahl an wie von Hand. Der Schlüssel ist die Id des Satzes (zwei Hersteller dürfen einen
/// Stoff gleichen Namens führen).</para>
///
/// <para><b>Die sieben Kenndaten</b> schreiben in den ARBEITSSTAND des Stammblatts, denselben,
/// den die Felder zeigen; geschrieben wird mit „Speichern". Einen Auslieferungssatz schützt
/// der Haken <c>Schreibgeschuetzt</c> der Maske.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class BaustoffKatalogKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? BaustoffLesen { get; init; }
    public Action<string>? BaustoffSetzen { get; init; }

    /// <summary>Liefert die Zeilen der Liste (Schlüssel = Id, Text = Name).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BaustoffEintraege { get; init; }

    public Func<BaustoffDaten?>? ArbeitLesen { get; init; }

    /// <summary>Meldet eine Änderung am Arbeitsstand — derselbe Nachzug wie nach einer Eingabe von Hand.</summary>
    public Action? Geaendert { get; init; }

    /// <summary>Die Baustoffe der Liste (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> BaustoffWahl
        => BaustoffEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Der gewählte Baustoff (Schlüssel = Id). Ein Setzen WÄHLT die Zeile.</summary>
    public string Baustoff
    {
        get => BaustoffLesen?.Invoke() ?? "";
        set => BaustoffSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Name im Arbeitsstand.</summary>
    public string Bezeichner
    {
        get => ArbeitLesen?.Invoke()?.Bezeichner ?? "";
        set => Setzen(d => d.Bezeichner = value ?? "");
    }

    /// <summary>Die Ordnungsgruppe im Arbeitsstand.</summary>
    public string Gruppe
    {
        get => ArbeitLesen?.Invoke()?.Gruppe ?? "";
        set => Setzen(d => d.Gruppe = value ?? "");
    }

    /// <summary>Der Hersteller im Arbeitsstand; leer = herstellerneutral.</summary>
    public string Hersteller
    {
        get => ArbeitLesen?.Invoke()?.Hersteller ?? "";
        set => Setzen(d => d.Hersteller = value ?? "");
    }

    /// <summary>Wärmeleitfähigkeit λ [W/(mK)].</summary>
    public double? Lambda
    {
        get => ArbeitLesen?.Invoke()?.Lambda;
        set => Setzen(d => d.Lambda = value);
    }

    /// <summary>Rohdichte ρ [kg/m³].</summary>
    public double? Rho
    {
        get => ArbeitLesen?.Invoke()?.Rho;
        set => Setzen(d => d.Rho = value);
    }

    /// <summary>Spezifische Wärmekapazität c_p [J/(kgK)].</summary>
    public double? Cp
    {
        get => ArbeitLesen?.Invoke()?.Cp;
        set => Setzen(d => d.Cp = value);
    }

    /// <summary>Die Quelle der Werte.</summary>
    public string Quelle
    {
        get => ArbeitLesen?.Invoke()?.Quelle ?? "";
        set => Setzen(d => d.Quelle = value ?? "");
    }

    private void Setzen(Action<BaustoffDaten> schritt)
    {
        BaustoffDaten? d = ArbeitLesen?.Invoke();
        if (d is null) return;
        schritt(d);
        Geaendert?.Invoke();
    }
}
