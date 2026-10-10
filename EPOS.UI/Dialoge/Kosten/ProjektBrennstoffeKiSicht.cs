using KiKern;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild des <see cref="ProjektBrennstoffeDialog"/> für den Hilfe-Assistenten
/// (Freigabe der Masken, Teil C): die Katalogwahl zum Übernehmen und die zehn Werte der
/// Bearbeitung.
/// </summary>
/// <remarks>
/// <para><b>Kein Feld schreibt sofort:</b> Die Katalogwahl merkt sich nur den Brennstoff für
/// „Übernehmen", die zehn Werte stehen im Arbeitsstand der Überlagerung „Bearbeiten…" — in die
/// Datenbank schreiben erst „Übernehmen" und „Speichern" (je Satz), und beide bleiben Klicks des
/// Anwenders. Die Sicht hält keinen Zustand: gelesen über <c>wert</c>, gesetzt über
/// <c>setzen</c> — derselbe Weg wie das Zahlenfeld.</para>
/// <para><b>Sperrgrund:</b> Die zehn Werte gibt es nur, solange eine Zeile in Bearbeitung
/// steht; welche, wählt der Anwender mit „Bearbeiten…".</para>
/// </remarks>
public sealed class ProjektBrennstoffeKiSicht
{
    private readonly Func<string, double?> _wert;
    private readonly Action<string, double?> _setzen;

    /// <summary>Legt die Sicht über den Arbeitsstand des Dialogs; Feldschlüssel aus <see cref="ProjektBrennstoffFelder"/>.</summary>
    public ProjektBrennstoffeKiSicht(Func<string, double?> wert, Action<string, double?> setzen)
    {
        _wert = wert ?? throw new ArgumentNullException(nameof(wert));
        _setzen = setzen ?? throw new ArgumentNullException(nameof(setzen));
    }

    public Func<int>? KatalogwahlLesen { get; init; }
    public Action<int>? KatalogwahlSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? KatalogwahlEintraege { get; init; }

    /// <summary>Die Wahl des Feldes <c>Katalogwahl</c> — die Katalogsätze, die das Projekt noch nicht führt.</summary>
    public IReadOnlyList<KiWahleintrag> KatalogwahlWahl
        => KatalogwahlEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Der Brennstoff des Katalogs, den „Übernehmen" in das Projekt kopiert.</summary>
    public int Katalogwahl
    {
        get => KatalogwahlLesen?.Invoke() ?? 0;
        set => (KatalogwahlSetzen ?? throw new InvalidOperationException(nameof(Katalogwahl)))(value);
    }

    /// <summary>Der Heizwert der Zeile in Bearbeitung.</summary>
    public double? Hi
    {
        get => _wert(ProjektBrennstoffFelder.Hi);
        set => _setzen(ProjektBrennstoffFelder.Hi, value);
    }

    /// <summary>Der Brennwert der Zeile in Bearbeitung.</summary>
    public double? Hs
    {
        get => _wert(ProjektBrennstoffFelder.Hs);
        set => _setzen(ProjektBrennstoffFelder.Hs, value);
    }

    /// <summary>Der CO₂-Faktor der Zeile in Bearbeitung.</summary>
    public double? Co2
    {
        get => _wert(ProjektBrennstoffFelder.Co2);
        set => _setzen(ProjektBrennstoffFelder.Co2, value);
    }

    /// <summary>Der SO₂-Faktor der Zeile in Bearbeitung.</summary>
    public double? So2
    {
        get => _wert(ProjektBrennstoffFelder.So2);
        set => _setzen(ProjektBrennstoffFelder.So2, value);
    }

    /// <summary>Der NOₓ-Faktor der Zeile in Bearbeitung.</summary>
    public double? Nox
    {
        get => _wert(ProjektBrennstoffFelder.Nox);
        set => _setzen(ProjektBrennstoffFelder.Nox, value);
    }

    /// <summary>Der Staubfaktor der Zeile in Bearbeitung.</summary>
    public double? Staub
    {
        get => _wert(ProjektBrennstoffFelder.Staub);
        set => _setzen(ProjektBrennstoffFelder.Staub, value);
    }

    /// <summary>Der Primärenergiefaktor der Zeile in Bearbeitung.</summary>
    public double? PeFaktor
    {
        get => _wert(ProjektBrennstoffFelder.Pe);
        set => _setzen(ProjektBrennstoffFelder.Pe, value);
    }

    /// <summary>Der Grundpreis der Zeile in Bearbeitung.</summary>
    public double? Grundpreis
    {
        get => _wert(ProjektBrennstoffFelder.Grundpreis);
        set => _setzen(ProjektBrennstoffFelder.Grundpreis, value);
    }

    /// <summary>Der Arbeitspreis der Zeile in Bearbeitung.</summary>
    public double? Arbeitspreis
    {
        get => _wert(ProjektBrennstoffFelder.Arbeitspreis);
        set => _setzen(ProjektBrennstoffFelder.Arbeitspreis, value);
    }

    /// <summary>Der Leistungspreis der Zeile in Bearbeitung.</summary>
    public double? Leistungspreis
    {
        get => _wert(ProjektBrennstoffFelder.Leistungspreis);
        set => _setzen(ProjektBrennstoffFelder.Leistungspreis, value);
    }
}
