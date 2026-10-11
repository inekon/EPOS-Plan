using KiKern;

namespace EPOS.UI.Dialoge.Import;

/// <summary>
/// Das FLACHE Abbild der Kopfeingaben des <see cref="GebaeudeImportDialog"/> für den Hilfe-Assistenten
/// (Freigabe der Masken, Teil B): die Einstellungen des Einlesens und der Zuordnung, die für das ganze
/// Gebäude gelten — nicht je Zeile, Raum, Zone oder Bauteil.
/// </summary>
/// <remarks>
/// <para><b>Warum eine Sichtklasse:</b> Der Dialog führt seine Wahl in privaten Feldern. Die Sicht hält
/// keinen Zustand: Gelesen wird über <c>lesen</c>, gesetzt über den Delegaten <c>setzen</c> — derselbe Weg
/// wie die Eingabe von Hand (neu zuordnen, Zonenplan neu bilden, Datei mit der neuen Nordrichtung neu lesen).</para>
/// <para><b>Sperrgrund:</b> Ein Feld, das der Dialog gerade nicht führt (vor dem Lesen, ohne Zonierung, ohne
/// Projektdatei mit beiden Zonierungen, ohne Bauteilvorschlag), und ein Wechsel, vor dem der Dialog wegen
/// Zuordnungen von Hand nachfragt, sagen ab, bevor der Anwender bestätigt. „Übernehmen" legt das Gebäude in
/// der Datenbank an und bleibt ein Klick des Anwenders.</para>
/// <para><b>Nicht hier:</b> die Felder <c>np_*</c> des Blatts Nutzungsprofile — die löst die Sicht des Wirts
/// <c>GebaeudeDialog</c> über <c>RaumnutzungKiZugang</c> auf.</para>
/// </remarks>
public sealed class GebaeudeImportKiSicht
{
    private readonly Func<string, object?> _lesen;
    private readonly Action<string, object?> _setzen;
    private readonly Func<string, IReadOnlyList<KiWahleintrag>> _wahl;
    private readonly Func<string, string?> _sperrgrund;

    /// <summary>Legt die Sicht über die Kopfeingaben des Dialogs.</summary>
    public GebaeudeImportKiSicht(Func<string, object?> lesen, Action<string, object?> setzen,
                                 Func<string, IReadOnlyList<KiWahleintrag>> wahl,
                                 Func<string, string?> sperrgrund)
    {
        _lesen = lesen ?? throw new ArgumentNullException(nameof(lesen));
        _setzen = setzen ?? throw new ArgumentNullException(nameof(setzen));
        _wahl = wahl ?? throw new ArgumentNullException(nameof(wahl));
        _sperrgrund = sperrgrund ?? throw new ArgumentNullException(nameof(sperrgrund));
    }

    /// <summary>Der Sperrgrund je Katalogfeld (<c>KiMaskenhaken.Sperrgrund</c>); <c>null</c> = frei.</summary>
    public string? Sperrgrund(string feld) => _sperrgrund(feld);

    /// <summary>Die Wahl des Feldes <c>Baualtersklasse</c> — dieselbe Liste wie in der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> BaualtersklasseWahl => _wahl(nameof(Baualtersklasse));

    /// <summary>Die Baualtersklasse (eigene Wahl, sonst die aus dem Baujahr der Datei).</summary>
    public int? Baualtersklasse
    {
        get => (int?)_lesen(nameof(Baualtersklasse));
        set => _setzen(nameof(Baualtersklasse), value);
    }

    /// <summary>Die Wahl des Feldes <c>Quelle</c> — dieselbe Liste wie in der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> QuelleWahl => _wahl(nameof(Quelle));

    /// <summary>Die Quelle (Format) der nächsten Dateiwahl; leer = alle Formate.</summary>
    public int? Quelle
    {
        get => (int?)_lesen(nameof(Quelle));
        set => _setzen(nameof(Quelle), value);
    }

    /// <summary>Die Wahl des Feldes <c>Gebaeude</c> — dieselbe Liste wie in der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> GebaeudeWahl => _wahl(nameof(Gebaeude));

    /// <summary>Das Gebäude der Datei, das übernommen wird.</summary>
    public int? Gebaeude
    {
        get => (int?)_lesen(nameof(Gebaeude));
        set => _setzen(nameof(Gebaeude), value);
    }

    /// <summary>Der Name des neuen Gebäudes.</summary>
    public string Name
    {
        get => (string?)_lesen(nameof(Name)) ?? "";
        set => _setzen(nameof(Name), value ?? "");
    }

    /// <summary>Raumtemperatur der Datei als Heizsollwert übernehmen.</summary>
    public bool CadSollwert
    {
        get => (bool?)_lesen(nameof(CadSollwert)) ?? false;
        set => _setzen(nameof(CadSollwert), value);
    }

    /// <summary>Die Richtung der Planoberseite in Grad (Nordrichtung).</summary>
    public double? Nordrichtung
    {
        get => (double?)_lesen(nameof(Nordrichtung));
        set => _setzen(nameof(Nordrichtung), value);
    }

    /// <summary>Die Wahl des Feldes <c>Zonenregel</c> — dieselbe Liste wie in der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> ZonenregelWahl => _wahl(nameof(Zonenregel));

    /// <summary>Die Zonenregel, nach der die Zonen der Datei gebildet werden.</summary>
    public int? Zonenregel
    {
        get => (int?)_lesen(nameof(Zonenregel));
        set => _setzen(nameof(Zonenregel), value);
    }

    /// <summary>Die Wahl des Feldes <c>Zonierung</c> — dieselbe Liste wie in der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> ZonierungWahl => _wahl(nameof(Zonierung));

    /// <summary>Die Zonierung der Projektdatei (DIN oder Simulation).</summary>
    public int? Zonierung
    {
        get => (int?)_lesen(nameof(Zonierung));
        set => _setzen(nameof(Zonierung), value);
    }

    /// <summary>Als Zone mit Bauteilen übernehmen.</summary>
    public bool AlsZone
    {
        get => (bool?)_lesen(nameof(AlsZone)) ?? false;
        set => _setzen(nameof(AlsZone), value);
    }
}
