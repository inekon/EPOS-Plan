using KiKern;

namespace EPOS.UI.Dialoge.Admin;

/// <summary>
/// <b>Die Sicht des Assistenten auf die Verwaltung „Kältemittel"</b> (KM4-E3-c; Feldkarte
/// <c>KiDialoge.Kaeltemittel</c>). Die Satzwahl nimmt den Weg eines Klicks, die Felder schreiben in den
/// Arbeitsstand des Satzbereichs — geschrieben wird erst mit „Speichern".
///
/// <para><b>Die Wertemengen sind Wahlen über den Persistenzwert</b> (<c>DbWerte.KAELTEMITTEL_*</c>, bei F-Gas und
/// natürlich „1"/„0"); ein Anzeigetext ist nie ein Steuerwert, ein fremder Wert wird benannt abgelehnt.</para>
/// </summary>
public sealed class KaeltemittelKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    /// <summary>Liest den Schlüssel (die Id) der gewählten Zeile; leer ohne Wahl.</summary>
    public Func<string>? SatzLesen { get; init; }

    /// <summary>Wählt eine Zeile wie ein Klick in die Liste.</summary>
    public Action<string>? SatzSetzen { get; init; }

    /// <summary>Die Einträge der Satzwahl.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SatzEintraege { get; init; }

    /// <summary>Der Arbeitsstand; <c>null</c> ohne Zeile.</summary>
    public Func<KaeltemittelZeile?>? ArbeitLesen { get; init; }

    /// <summary>Schreibt in den Arbeitsstand und zieht „geändert" nach.</summary>
    public Action<Action<KaeltemittelZeile>>? Setzen { get; init; }

    /// <summary>Die Stoffgruppen (Persistenzwert, Anzeigetext).</summary>
    public Func<IReadOnlyList<KaeltemittelWahl>>? Gruppen { get; init; }

    /// <summary>Die Sicherheitsgruppen.</summary>
    public Func<IReadOnlyList<KaeltemittelWahl>>? Sicherheitsgruppen { get; init; }

    /// <summary>Die PFAS-Status.</summary>
    public Func<IReadOnlyList<KaeltemittelWahl>>? PfasWerte { get; init; }

    /// <summary>Die Status.</summary>
    public Func<IReadOnlyList<KaeltemittelWahl>>? Statuswerte { get; init; }

    /// <summary>Die Schalterwerte ja/nein („1"/„0").</summary>
    public Func<IReadOnlyList<KaeltemittelWahl>>? Schalterwerte { get; init; }

    // =====================================================================
    //  Satzwahl
    // =====================================================================

    /// <summary>Die Wahl der Zeile (Schlüssel = Id).</summary>
    public string Kaeltemittel
    {
        get => SatzLesen?.Invoke() ?? "";
        set => SatzSetzen?.Invoke(value ?? "");
    }

    /// <summary>Begleiteigenschaft der Satzwahl.</summary>
    public IReadOnlyList<KiWahleintrag> KaeltemittelWahl => SatzEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Felder
    // =====================================================================

    public string Code { get => Lies(z => z.Code); set => Schreibe(z => z.Code = value ?? ""); }

    public string Bezeichnung { get => Lies(z => z.Bezeichnung); set => Schreibe(z => z.Bezeichnung = value ?? ""); }

    public string Gruppe { get => Lies(z => z.Gruppe); set => Schreibe(z => z.Gruppe = Wert(value, Gruppen, WindowsFormsApplication1.MyResource.Resource.KMT_SP_GRUPPE)); }

    public IReadOnlyList<KiWahleintrag> GruppeWahl => Wahl(Gruppen);

    public double? Gwp { get => ArbeitLesen?.Invoke()?.Gwp; set => Schreibe(z => z.Gwp = value); }

    public string GwpQuelle { get => Lies(z => z.GwpQuelle); set => Schreibe(z => z.GwpQuelle = value ?? ""); }

    public string FGas { get => Lies(z => Schalter(z.FGas)); set => Schreibe(z => z.FGas = Schalter(Wert(value, Schalterwerte, WindowsFormsApplication1.MyResource.Resource.KMT_SP_FGAS))); }

    public IReadOnlyList<KiWahleintrag> FGasWahl => Wahl(Schalterwerte);

    public string Natuerlich
    {
        get => Lies(z => Schalter(z.Natuerlich));
        set => Schreibe(z => z.Natuerlich = Schalter(Wert(value, Schalterwerte, WindowsFormsApplication1.MyResource.Resource.KMT_SP_NATUERLICH)));
    }

    public IReadOnlyList<KiWahleintrag> NatuerlichWahl => Wahl(Schalterwerte);

    public string Sicherheitsgruppe
    {
        get => Lies(z => z.Sicherheitsgruppe);
        set => Schreibe(z => z.Sicherheitsgruppe = Wert(value, Sicherheitsgruppen, WindowsFormsApplication1.MyResource.Resource.KMT_SP_SICHERHEIT));
    }

    public IReadOnlyList<KiWahleintrag> SicherheitsgruppeWahl => Wahl(Sicherheitsgruppen);

    public string Pfas { get => Lies(z => z.PfasStatus); set => Schreibe(z => z.PfasStatus = Wert(value, PfasWerte, WindowsFormsApplication1.MyResource.Resource.KMT_SP_PFAS)); }

    public IReadOnlyList<KiWahleintrag> PfasWahl => Wahl(PfasWerte);

    public string Status { get => Lies(z => z.Status); set => Schreibe(z => z.Status = Wert(value, Statuswerte, WindowsFormsApplication1.MyResource.Resource.KMT_SP_STATUS)); }

    public IReadOnlyList<KiWahleintrag> StatusWahl => Wahl(Statuswerte);

    public string Quelle { get => Lies(z => z.Quelle); set => Schreibe(z => z.Quelle = value ?? ""); }

    public int? Sortierung { get => ArbeitLesen?.Invoke()?.Sortierung; set => Schreibe(z => z.Sortierung = value); }

    // =====================================================================
    //  Bausteine
    // =====================================================================

    private string Lies(Func<KaeltemittelZeile, string> wert)
        => ArbeitLesen?.Invoke() is KaeltemittelZeile z ? wert(z) ?? "" : "";

    private void Schreibe(Action<KaeltemittelZeile> setzen) => Setzen?.Invoke(setzen);

    /// <summary>Leer bleibt leer (unbekannt); ein Wert außerhalb der Menge wird benannt abgelehnt.</summary>
    private static string Wert(string? wert, Func<IReadOnlyList<KaeltemittelWahl>>? menge, string feld)
    {
        string w = (wert ?? "").Trim();
        if (w.Length == 0) return "";
        foreach (KaeltemittelWahl e in menge?.Invoke() ?? Array.Empty<KaeltemittelWahl>())
            if (string.Equals(e.Wert, w, StringComparison.Ordinal)) return w;
        throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
            WindowsFormsApplication1.MyResource.Resource.KMT_MSG_WERTEMENGE, w, feld));
    }

    private static string Schalter(bool? wert) => wert is null ? "" : wert.Value ? "1" : "0";

    private static bool? Schalter(string wert) => wert switch { "1" => true, "0" => false, _ => null };

    private static IReadOnlyList<KiWahleintrag> Wahl(Func<IReadOnlyList<KaeltemittelWahl>>? menge)
        => (menge?.Invoke() ?? Array.Empty<KaeltemittelWahl>()).Select(e => new KiWahleintrag(e.Wert, e.Text)).ToList();
}
