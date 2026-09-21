using KiKern;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild der Maske „Emissionsarten und Katalog" für den
/// Hilfe-Assistenten (Welle KI‑F4).
///
/// <para><b>Eine Maske, drei Ebenen.</b> Oben die Bilanzierungsmethode, darunter
/// zwei Raster (Arten und ihre Werte) und über ihnen je ein Editor als
/// Überlagerung. Die Editoren gehen IN dieser Maske auf und haben keinen eigenen
/// Katalogschlüssel — dieselbe Lage wie die zwei Reiterblätter des
/// Gebäudekatalogs.</para>
///
/// <para><b>Was die Raster zeigen, bleibt draußen</b>: Kürzel, Name, Einheit, GWP
/// und Herkunft einer Art stehen dort als Anzeige, und die Werte kommen aus dem
/// Katalog. Deklariert sind die MARKIERUNGEN — sie entscheiden, worauf „Ändern",
/// „Löschen" und „Übernehmen" greifen — und die lebenden Felder der beiden
/// Editoren.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
/// </summary>
public sealed class EmissionskatalogKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<bool>? ModusLesen { get; init; }
    public Action<bool>? ModusSetzen { get; init; }

    public Func<int?>? ArtLesen { get; init; }
    public Action<int?>? ArtSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? ArtEintraege { get; init; }

    public Func<int?>? WertLesen { get; init; }
    public Action<int?>? WertSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? WertEintraege { get; init; }

    public Func<string>? ArtKuerzelLesen { get; init; }
    public Action<string>? ArtKuerzelSetzen { get; init; }
    public Func<string>? ArtNameLesen { get; init; }
    public Action<string>? ArtNameSetzen { get; init; }
    public Func<int?>? ArtEinheitLesen { get; init; }
    public Action<int?>? ArtEinheitSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? EinheitEintraege { get; init; }
    public Func<double?>? ArtGwpLesen { get; init; }
    public Action<double?>? ArtGwpSetzen { get; init; }
    public Func<string>? ArtQuelleLesen { get; init; }
    public Action<string>? ArtQuelleSetzen { get; init; }

    public Func<string>? WertQuelleLesen { get; init; }
    public Action<string>? WertQuelleSetzen { get; init; }
    public Func<double?>? WertZahlLesen { get; init; }
    public Action<double?>? WertZahlSetzen { get; init; }
    public Func<bool>? WertCo2eLesen { get; init; }
    public Action<bool>? WertCo2eSetzen { get; init; }
    public Func<bool>? WertVorlageLesen { get; init; }
    public Action<bool>? WertVorlageSetzen { get; init; }

    // =====================================================================
    //  Die Wahllisten (KI-D-Q6)
    // =====================================================================

    /// <summary>Die Emissionsarten des Katalogs — Schlüssel ist ihre Id.</summary>
    public IReadOnlyList<KiWahleintrag> EmissionsartWahl
        => ArtEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Werte der markierten Art — Schlüssel ist ihre Id.</summary>
    public IReadOnlyList<KiWahleintrag> EmissionswertWahl
        => WertEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Einheiten des Arteneditors — Schlüssel ist ihr Listenplatz.</summary>
    public IReadOnlyList<KiWahleintrag> ArtEinheitWahl
        => EinheitEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Rechnet die Maske in CO₂-Äquivalent (GWP₁₀₀) statt in reinem CO₂?</summary>
    public bool AlsCo2e
    {
        get => ModusLesen?.Invoke() ?? false;
        set => ModusSetzen?.Invoke(value);
    }

    /// <summary>Die im oberen Raster markierte Emissionsart.</summary>
    public int? Emissionsart
    {
        get => ArtLesen?.Invoke();
        set => ArtSetzen?.Invoke(value);
    }

    /// <summary>Der im unteren Raster markierte Wert.</summary>
    public int? Emissionswert
    {
        get => WertLesen?.Invoke();
        set => WertSetzen?.Invoke(value);
    }

    // ---- Arteneditor ----------------------------------------------------

    /// <summary>Das Kürzel der bearbeiteten Art (CO2, CH4, N2O).</summary>
    public string ArtKuerzel
    {
        get => ArtKuerzelLesen?.Invoke() ?? "";
        set => ArtKuerzelSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Klartextname der bearbeiteten Art.</summary>
    public string ArtName
    {
        get => ArtNameLesen?.Invoke() ?? "";
        set => ArtNameSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Einheit der bearbeiteten Art.</summary>
    public int? ArtEinheit
    {
        get => ArtEinheitLesen?.Invoke();
        set => ArtEinheitSetzen?.Invoke(value);
    }

    /// <summary>Das Treibhauspotenzial der bearbeiteten Art (GWP₁₀₀).</summary>
    public double? ArtGwp
    {
        get => ArtGwpLesen?.Invoke();
        set => ArtGwpSetzen?.Invoke(value);
    }

    /// <summary>Die Quelle des Äquivalenzfaktors.</summary>
    public string ArtQuelle
    {
        get => ArtQuelleLesen?.Invoke() ?? "";
        set => ArtQuelleSetzen?.Invoke(value ?? "");
    }

    // ---- Werteeditor ----------------------------------------------------

    /// <summary>Die Herkunft des bearbeiteten Wertes (GEMIS, Datenblatt, eigene Messung).</summary>
    public string WertQuelle
    {
        get => WertQuelleLesen?.Invoke() ?? "";
        set => WertQuelleSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Zahlenwert selbst, in der Einheit seiner Art.</summary>
    public double? Wert
    {
        get => WertZahlLesen?.Invoke();
        set => WertZahlSetzen?.Invoke(value);
    }

    /// <summary>Ist der Wert bereits ein CO₂-Äquivalent?</summary>
    public bool WertIstCo2e
    {
        get => WertCo2eLesen?.Invoke() ?? false;
        set => WertCo2eSetzen?.Invoke(value);
    }

    /// <summary>Soll der Wert als Vorlage für weitere Träger gelten?</summary>
    public bool WertAlsVorlage
    {
        get => WertVorlageLesen?.Invoke() ?? false;
        set => WertVorlageSetzen?.Invoke(value);
    }
}
