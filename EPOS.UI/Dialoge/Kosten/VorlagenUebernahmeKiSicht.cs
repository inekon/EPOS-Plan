using KiKern;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild des <see cref="VorlagenUebernahmeDialog"/> für den Hilfe-Assistenten
/// (Freigabe der Masken, Teil C): Ziel und Quelle der Übernahme.
/// </summary>
/// <remarks>
/// <para><b>Warum eine Sichtklasse:</b> Der Dialog führt seine Wahl in privaten Feldern. Die Sicht
/// hält keinen Zustand: Gelesen wird über <c>lesen</c>, gesetzt über den Delegaten <c>setzen</c> —
/// derselbe Weg wie ein Griff in die Klappliste (Anlagenliste nachziehen, Vorschau neu).</para>
/// <para><b>Sperrgrund:</b> Kategorie und Variante gelten nur für die Quelle „Vorlage",
/// Quellprojekt und Quellanlage nur für die Quelle „Projekt"; ein festes Zielprojekt wählt
/// niemand. „OK" legt die Kostenpositionen an und bleibt ein Klick des Anwenders.</para>
/// </remarks>
public sealed class VorlagenUebernahmeKiSicht
{
    private readonly Func<string, int?> _lesen;
    private readonly Action<string, object?> _setzen;
    private readonly Func<string, IReadOnlyList<KiWahleintrag>> _wahl;
    private readonly Func<string, string?> _sperrgrund;

    /// <summary>Legt die Sicht über die Wahl des Dialogs.</summary>
    public VorlagenUebernahmeKiSicht(Func<string, int?> lesen, Action<string, object?> setzen,
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

    /// <summary>Die Wahl des Feldes <c>Zielprojekt</c> — dieselbe Liste wie in der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> ZielprojektWahl => _wahl(nameof(Zielprojekt));

    /// <summary>Das Zielprojekt.</summary>
    public int? Zielprojekt
    {
        get => _lesen(nameof(Zielprojekt));
        set => _setzen(nameof(Zielprojekt), value);
    }

    /// <summary>Die Wahl des Feldes <c>Quelle</c> — dieselbe Liste wie in der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> QuelleWahl => _wahl(nameof(Quelle));

    /// <summary>Die Quelle (Vorlage oder Projekt).</summary>
    public int? Quelle
    {
        get => _lesen(nameof(Quelle));
        set => _setzen(nameof(Quelle), value);
    }

    /// <summary>Die Wahl des Feldes <c>Kategorie</c> — dieselbe Liste wie in der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> KategorieWahl => _wahl(nameof(Kategorie));

    /// <summary>Die Kategorie der Vorlage (Betrieb oder Investition).</summary>
    public int? Kategorie
    {
        get => _lesen(nameof(Kategorie));
        set => _setzen(nameof(Kategorie), value);
    }

    /// <summary>Die Wahl des Feldes <c>Variante</c> — dieselbe Liste wie in der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> VarianteWahl => _wahl(nameof(Variante));

    /// <summary>Die Variante der Vorlage.</summary>
    public int? Variante
    {
        get => _lesen(nameof(Variante));
        set => _setzen(nameof(Variante), value);
    }

    /// <summary>Die Wahl des Feldes <c>Quellprojekt</c> — dieselbe Liste wie in der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> QuellprojektWahl => _wahl(nameof(Quellprojekt));

    /// <summary>Das Quellprojekt.</summary>
    public int? Quellprojekt
    {
        get => _lesen(nameof(Quellprojekt));
        set => _setzen(nameof(Quellprojekt), value);
    }

    /// <summary>Die Wahl des Feldes <c>Quellanlage</c> — dieselbe Liste wie in der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> QuellanlageWahl => _wahl(nameof(Quellanlage));

    /// <summary>Die Quellanlage des Quellprojekts.</summary>
    public int? Quellanlage
    {
        get => _lesen(nameof(Quellanlage));
        set => _setzen(nameof(Quellanlage), value);
    }
}
