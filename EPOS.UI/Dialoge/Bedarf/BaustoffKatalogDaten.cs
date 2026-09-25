namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Ein Baustoff des Katalogs</b>, wie ihn die Verwaltung „Baustoffe" zeigt und bearbeitet
/// (Gebäudesimulation G3, Welle C) — das DTO zwischen <c>BaustoffKatalogHuelle</c> und
/// <see cref="BaustoffKatalogDialog"/>, ohne Fachklasse des Kerns.
/// </summary>
/// <remarks>
/// <para><b>Ein Stoffwert <c>null</c> heißt „nicht angegeben"</b> — eine 0 wäre ein anderer
/// Stoff (λ = 0 ist ein unendlicher Wärmewiderstand). Die Einheiten stehen im Namen der
/// Beschriftung, nicht hier: λ in W/(mK), ρ in kg/m³, c_p in J/(kgK) — dieselben, in denen der
/// Katalog speichert.</para>
/// <para><b>Die Herkunft kommt als Anzeigetext</b> (Steuerwert ≠ Anzeigetext): Die Verwaltung
/// zeigt sie nur; geschrieben wird sie vom Kern (neuer Satz: „Manuell").</para>
/// </remarks>
public sealed class BaustoffDaten
{
    /// <summary>Id im Katalog; 0 = noch nicht gespeichert.</summary>
    public int Id { get; set; }

    /// <summary>Name des Stoffes (Pflicht).</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Ordnungsgruppe (Mauerwerk, Beton, Dämmstoffe …); leer = ohne Gruppe.</summary>
    public string Gruppe { get; set; } = "";

    /// <summary>Hersteller; leer = herstellerneutral (Norm- oder Richtwert).</summary>
    public string Hersteller { get; set; } = "";

    /// <summary>Wärmeleitfähigkeit λ [W/(mK)]; <c>null</c> = nicht angegeben.</summary>
    public double? Lambda { get; set; }

    /// <summary>Rohdichte ρ [kg/m³]; <c>null</c> = nicht angegeben.</summary>
    public double? Rho { get; set; }

    /// <summary>Spezifische Wärmekapazität c_p [J/(kgK)]; <c>null</c> = nicht angegeben.</summary>
    public double? Cp { get; set; }

    /// <summary>Regelwerk oder Datenblatt, aus dem die Werte stammen.</summary>
    public string Quelle { get; set; } = "";

    /// <summary>Die Herkunft als Anzeigetext („Vorgabe", „Manuell" …) — nur Anzeige.</summary>
    public string Herkunft { get; set; } = "";

    /// <summary>Die Kennung eines Imports — nur Anzeige.</summary>
    public string Quellkennung { get; set; } = "";

    /// <summary>Gehört zur Auslieferung (Schloss) — dann nur lesbar.</summary>
    public bool Auslieferung { get; set; }

    /// <summary>Eine entkoppelte Kopie — der Arbeitsstand der Verwaltung.</summary>
    public BaustoffDaten Kopie() => (BaustoffDaten)MemberwiseClone();

    /// <summary>
    /// Wie viele der sieben bearbeitbaren Felder von <paramref name="anderer"/> abweichen
    /// (Name, Gruppe, Hersteller, λ, ρ, c_p, Quelle) — der Zähler „n Felder geändert".
    /// </summary>
    public int Abweichungen(BaustoffDaten anderer)
    {
        if (anderer is null) return 0;
        int n = 0;
        if (!Gleich(Bezeichner, anderer.Bezeichner)) n++;
        if (!Gleich(Gruppe, anderer.Gruppe)) n++;
        if (!Gleich(Hersteller, anderer.Hersteller)) n++;
        if (Lambda != anderer.Lambda) n++;
        if (Rho != anderer.Rho) n++;
        if (Cp != anderer.Cp) n++;
        if (!Gleich(Quelle, anderer.Quelle)) n++;
        return n;
    }

    private static bool Gleich(string? a, string? b)
        => string.Equals((a ?? "").Trim(), (b ?? "").Trim(), System.StringComparison.Ordinal);
}

/// <summary>
/// Was ein Schreibversuch der Baustoffverwaltung ergab — gelungen, die Meldung des Kerns (bei
/// Misserfolg) und die Id des geschriebenen Satzes (beim Anlegen und Duplizieren die neue).
/// </summary>
public sealed record BaustoffSpeicherErgebnis(bool Ok, string Meldung, int Id);
