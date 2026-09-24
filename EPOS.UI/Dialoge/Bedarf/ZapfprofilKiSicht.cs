using System.Globalization;
using KiKern;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das flache Abbild des Dialogs „Brauchwasser-Zapfprofil" für den Hilfe-Assistenten
/// (Welle #458, Stufe 3a) — eine Überlagerung der Bedarfsprofile in der Ausprägung
/// Brauchwasser.
///
/// <para><b>Eine ÜBERLAGERUNG mit eigenem Arbeitsstand.</b> Der Dialog bearbeitet eine
/// KOPIE der Zonen; „OK" gibt sie an die Bedarfsprofile zurück, und geschrieben wird mit
/// deren OK. Einen Speicherweg meldet er deshalb nicht an: <c>dialog_speichern</c> lehnt
/// benannt ab (Vorbild der Kennlinieneditor).</para>
///
/// <para><b>Gesetzt wird auf den Wegen der Eingabefelder.</b> Jeder Setzer ruft den Weg,
/// den auch die Eingabe von Hand nimmt: Die Vorschau rechnet entprellt neu, ein Lauf der
/// Jahresreihe verfällt, und eine Änderung, die die Auslegung ändert, macht deren Punkt
/// überholt. Ein Setzer gibt den Grund einer Ablehnung zurück; die Eigenschaft wirft ihn
/// als benannte Ausnahme.</para>
///
/// <para><b>Die Zonen sind SPALTEN</b> (<see cref="Zonen"/>) mit dem Zonennamen als
/// Kennzeichen — der Assistent setzt die Bezugsgröße der Zone „Wohnen" ohne sie vorher zu
/// wählen. <see cref="Zone"/> wählt trotzdem, welche Zone ihren Eingabeblock zeigt.</para>
///
/// <para><b>Was nicht auf der Maske steht, ist nicht setzbar</b>: Rechenweg, Seed und
/// Realisierungen stehen erst in der Stufe „Experte" (die Realisierungen dazu nur beim
/// Rechenweg „stochastisch"); davor nennt die Absage, was sie sichtbar macht.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class ZapfprofilKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? StufeLesen { get; init; }
    public Func<int?, string?>? StufeSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? StufeEintraege { get; init; }

    public Func<int?>? ZoneLesen { get; init; }
    public Func<int?, string?>? ZoneSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? ZoneEintraege { get; init; }

    /// <summary>Die Zeilen der Zonenliste — je Zone eine, frisch bei jedem Zugriff.</summary>
    public Func<IReadOnlyList<ZapfprofilZoneKiZeile>>? ZonenLesen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? NutzungsartEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? NiveauEintraege { get; init; }

    public Func<int?>? AnsichtLesen { get; init; }
    public Func<int?, string?>? AnsichtSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? AnsichtEintraege { get; init; }

    public Func<int?>? RechenwegLesen { get; init; }
    public Func<int?, string?>? RechenwegSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? RechenwegEintraege { get; init; }

    public Func<int?>? SeedLesen { get; init; }
    public Func<int?, string?>? SeedSetzen { get; init; }

    public Func<int?>? RealisierungenLesen { get; init; }
    public Func<int?, string?>? RealisierungenSetzen { get; init; }

    // =====================================================================
    //  Die Wahllisten (KI‑D‑Q6)
    // =====================================================================

    /// <summary>Die drei Stufen; „Erweitert" steht darin und nennt beim Setzen ihren Grund.</summary>
    public IReadOnlyList<KiWahleintrag> StufeWahl => Liste(StufeEintraege);

    /// <summary>Die Zonen nach ihrer Nummer in der Liste (ab 1), mit dem Namen als Text.</summary>
    public IReadOnlyList<KiWahleintrag> ZoneWahl => Liste(ZoneEintraege);

    /// <summary>Die Nutzungsarten des Katalogs — die Wahl der Spalte „Nutzungsart".</summary>
    public IReadOnlyList<KiWahleintrag> NutzungsartWahl => Liste(NutzungsartEintraege);

    /// <summary>Die drei Bedarfsniveaus — die Wahl der Spalte „Bedarfsniveau".</summary>
    public IReadOnlyList<KiWahleintrag> NiveauWahl => Liste(NiveauEintraege);

    /// <summary>Die Ansichten der Vorschau; leer ohne gerechnete Vorschau.</summary>
    public IReadOnlyList<KiWahleintrag> AnsichtWahl => Liste(AnsichtEintraege);

    /// <summary>Deterministisch oder stochastisch.</summary>
    public IReadOnlyList<KiWahleintrag> RechenwegWahl => Liste(RechenwegEintraege);

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Die Stufe des Dialogs (Einfach, Erweitert, Experte).</summary>
    public int? Stufe
    {
        get => StufeLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(StufeSetzen, value);
    }

    /// <summary>Die gewählte Zone als Nummer in der Liste (ab 1); leer ohne Zone.</summary>
    public int? Zone
    {
        get => ZoneLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(ZoneSetzen, value);
    }

    /// <summary>
    /// Die Zonen der Liste — Spalten Name, Nutzungsart, Bezugsgröße, Bedarfsniveau und der
    /// Jahresbedarf der Vorschau.
    /// </summary>
    public IReadOnlyList<ZapfprofilZoneKiZeile> Zonen
        => ZonenLesen?.Invoke() ?? Array.Empty<ZapfprofilZoneKiZeile>();

    /// <summary>„Anzeigen für" — die Ansicht der Vorschau (Summe oder eine Zone).</summary>
    public int? Ansicht
    {
        get => AnsichtLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(AnsichtSetzen, value);
    }

    /// <summary>Der Rechenweg der Jahresreihe — 0 deterministisch, 1 stochastisch.</summary>
    public int? Rechenweg
    {
        get => RechenwegLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(RechenwegSetzen, value);
    }

    /// <summary>Die Zufallssaat; leer = Vorgabe.</summary>
    public int? Seed
    {
        get => SeedLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(SeedSetzen, value);
    }

    /// <summary>Die Realisierungen der Jahresreihe; leer = Vorgabe.</summary>
    public int? Realisierungen
    {
        get => RealisierungenLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(RealisierungenSetzen, value);
    }

    private static IReadOnlyList<KiWahleintrag> Liste(Func<IReadOnlyList<KiWahleintrag>>? quelle)
        => quelle?.Invoke() ?? Array.Empty<KiWahleintrag>();
}

/// <summary>
/// Eine Zone der Liste als ZEILE der Sichtklasse <see cref="ZapfprofilKiSicht"/>. Sie hält
/// die Zone selbst — nicht ihren Platz —, damit eine Setzung auch nach dem Entfernen einer
/// anderen Zone die richtige trifft; ist die Zone selbst weg, lehnt der Setzer benannt ab.
/// </summary>
public sealed class ZapfprofilZoneKiZeile
{
    private readonly ZapfprofilZoneDaten _zone;

    /// <summary>Legt die Zeile zu einer Zone des Arbeitsstands an.</summary>
    public ZapfprofilZoneKiZeile(ZapfprofilZoneDaten zone)
    {
        _zone = zone ?? throw new ArgumentNullException(nameof(zone));
    }

    public Func<string, string?>? ZonennameSetzen { get; init; }
    public Func<int?, string?>? NutzungsartSetzen { get; init; }
    public Func<double?, string?>? BezugsmengeSetzen { get; init; }
    public Func<int?, string?>? NiveauSetzen { get; init; }
    public Func<string>? JahresbedarfLesen { get; init; }

    /// <summary>Das ZEILENKENNZEICHEN — der Zonenname, wie ihn die Liste zeigt.</summary>
    public string Kennzeichen => _zone.Name ?? "";

    /// <summary>Der Zonenname.</summary>
    public string Zonenname
    {
        get => _zone.Name ?? "";
        set => ZapfprofilKiRegeln.Setze(ZonennameSetzen, value);
    }

    /// <summary>Die Nutzungsart als Id des Katalogs; leer = noch keine.</summary>
    public int? Nutzungsart
    {
        get => _zone.IdNutzungsart > 0 ? _zone.IdNutzungsart : null;
        set => ZapfprofilKiRegeln.Setze(NutzungsartSetzen, value);
    }

    /// <summary>Die Bezugsmenge in der Bezugsgröße der Nutzungsart.</summary>
    public double? Bezugsmenge
    {
        get => _zone.Bezugsmenge;
        set => ZapfprofilKiRegeln.Setze(BezugsmengeSetzen, value);
    }

    /// <summary>Das Bedarfsniveau als Zahl des Kerns (1 niedrig … 3 hoch).</summary>
    public int? Niveau
    {
        get => (int)_zone.Niveau;
        set => ZapfprofilKiRegeln.Setze(NiveauSetzen, value);
    }

    /// <summary>Der Jahresbedarf der Zapfung aus der aktuellen Vorschau, wie die Liste ihn zeigt.</summary>
    public string Jahresbedarf => JahresbedarfLesen?.Invoke() ?? "";
}

/// <summary>
/// Die gemeinsamen Absagen der drei Zapfprofil-Masken beim Hilfe-Assistenten (Welle #458,
/// Stufe 3a) — EINE Stelle für die Sätze, damit Zapfprofil, Auslegung und Konstruktor
/// dieselbe Ablehnung gleich formulieren.
/// </summary>
internal static class ZapfprofilKiRegeln
{
    /// <summary>
    /// Ruft einen Setzweg und wirft seine Ablehnung als benannte Ausnahme; ohne Weg sagt
    /// die Absage, dass es keinen Schreibweg gibt.
    /// </summary>
    internal static void Setze<T>(Func<T, string?>? weg, T wert)
    {
        string? grund = weg is null ? Resource.KI_SIM_KEIN_SCHREIBWEG : weg(wert);
        if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
    }

    /// <summary>„„Seed" steht nur mit „Experte" auf der Maske — bitte zuerst das wählen."</summary>
    internal static string NurMit(string feld, string bedingung)
        => string.Format(CultureInfo.CurrentCulture, Resource.KI_DLG_ZPG_NUR_MIT, Ohne(feld), Ohne(bedingung));

    /// <summary>„„A100-Referenzprofil" ist gesperrt." — wenn der Eintrag selbst keinen Grund nennt.</summary>
    internal static string Gesperrt(string eintrag)
        => string.Format(CultureInfo.CurrentCulture, Resource.KI_DLG_ZPG_GESPERRT, Ohne(eintrag));

    /// <summary>Die Zeile, die gesetzt werden soll, steht nicht mehr in der Liste.</summary>
    internal static string ZeileFehlt => Resource.KI_DLG_ZPG_ZEILE_FEHLT;

    /// <summary>
    /// Liegt <paramref name="wert"/> in den Grenzen des Feldes? <c>null</c> = ja (ein leerer
    /// Wert ist immer „in den Grenzen" — ob leer erlaubt ist, sagt der Katalog). Dieselben
    /// Grenzen, die das Eingabefeld der Maske trägt (<c>Min</c>/<c>Max</c>).
    /// </summary>
    internal static string? Bereich(string feld, double? wert, double? min, double? max)
    {
        if (wert is not double w) return null;
        bool zuKlein = min.HasValue && w < min.Value;
        bool zuGross = max.HasValue && w > max.Value;
        if (!zuKlein && !zuGross) return null;

        if (min == 0 && !max.HasValue)
            return string.Format(CultureInfo.CurrentCulture, Resource.KBROW_MSG_WERT_NEGATIV, Ohne(feld));

        return string.Format(CultureInfo.CurrentCulture, Resource.KI_DLG_SIM_BEREICH, Ohne(feld),
                             Zahl(min), Zahl(max));
    }

    /// <summary>Die Beschriftung ohne Doppelpunkt am Ende — sie steht in Anführungszeichen.</summary>
    private static string Ohne(string text) => (text ?? "").Trim().TrimEnd(':').Trim();

    private static string Zahl(double? wert)
        => wert is double w ? w.ToString("0.##", CultureInfo.CurrentCulture) : "…";
}
