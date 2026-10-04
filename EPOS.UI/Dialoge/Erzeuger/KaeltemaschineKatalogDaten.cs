namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Der Feldsatz der Verwaltung „Kältemaschinen"</b> (KU3-1) — das plattformfreie Abbild von
/// <c>KaeltemaschineModel</c> samt Kennlinie. Die Hülle (<c>KaeltemaschineKatalogHuelle</c>) bildet
/// zwischen beiden ab; die Komponente kennt weder Tabelle noch Persistenzwerte.
///
/// <para><b>Die Rückkühlart ist ein LISTENPLATZ</b> (<see cref="RueckkuehlartIndex"/>), kein Text: Der
/// Anzeigetext ist sprachabhängig und nie ein Steuerwert; die Abbildung Platz → Persistenzwert liegt
/// in der Hülle über <c>KaeltemaschineSchema.RUECKKUEHLARTEN</c>.</para>
///
/// <para><b>Leer ist nicht null:</b> Alle Zahlen sind <c>double?</c> — ein leerer Hilfsstrom heißt
/// „im EER enthalten", eine 0 wäre eine gemessene Null.</para>
/// </summary>
public sealed class KaeltemaschineDaten
{
    /// <summary>Id des Katalogsatzes; 0 = neu.</summary>
    public int Id { get; set; }

    /// <summary>Name des Geräts (im Katalog eindeutig).</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Hersteller.</summary>
    public string Firma { get; set; } = "";

    /// <summary>Typbezeichnung.</summary>
    public string Typ { get; set; } = "";

    /// <summary>Freitext.</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Nennkälteleistung [kW].</summary>
    public double? Nennkaelteleistung { get; set; }

    /// <summary>EER im Nennpunkt [—].</summary>
    public double? NennEer { get; set; }

    /// <summary>Kältemittel.</summary>
    public string Kaeltemittel { get; set; } = "";

    /// <summary>Listenplatz der Rückkühlart; <c>null</c> = keine Angabe.</summary>
    public int? RueckkuehlartIndex { get; set; }

    /// <summary>Kleinste Teillast [%].</summary>
    public double? Mindestteillast { get; set; }

    /// <summary>Elektrische Leistung der Rückkühlung im Nennpunkt [kW]; <c>null</c> = im EER enthalten.</summary>
    public double? Hilfsstrom { get; set; }

    /// <summary>Kleinster zulässiger Kaltwasservorlauf [°C].</summary>
    public double? KaltwasserMin { get; set; }

    /// <summary>Gerätepreis [€].</summary>
    public double? Modulkosten { get; set; }

    /// <summary>Auslieferungssatz (<c>ReadOnly</c>)? Dann ist das Blatt nur lesbar.</summary>
    public bool Auslieferung { get; set; }

    /// <summary>Die Kennlinienpunkte in der Reihenfolge des Blatts.</summary>
    public List<KaeltemaschinePunktDaten> Kennlinie { get; set; } = new();

    /// <summary>Eine tiefe Kopie — der Arbeitsstand des Blatts teilt keinen Punkt mit dem gespeicherten.</summary>
    public KaeltemaschineDaten Kopie()
    {
        var k = (KaeltemaschineDaten)MemberwiseClone();
        k.Kennlinie = Kennlinie.Select(p => p.Kopie()).ToList();
        return k;
    }

    /// <summary>Wie viele Felder weichen ab? Die Kennlinie zählt als EIN Feld.</summary>
    public int Abweichungen(KaeltemaschineDaten anderer)
    {
        if (anderer is null) return 0;
        int n = 0;
        if (!Gleich(Bezeichner, anderer.Bezeichner)) n++;
        if (!Gleich(Firma, anderer.Firma)) n++;
        if (!Gleich(Typ, anderer.Typ)) n++;
        if (!Gleich(Beschreibung, anderer.Beschreibung)) n++;
        if (Nennkaelteleistung != anderer.Nennkaelteleistung) n++;
        if (NennEer != anderer.NennEer) n++;
        if (!Gleich(Kaeltemittel, anderer.Kaeltemittel)) n++;
        if (RueckkuehlartIndex != anderer.RueckkuehlartIndex) n++;
        if (Mindestteillast != anderer.Mindestteillast) n++;
        if (Hilfsstrom != anderer.Hilfsstrom) n++;
        if (KaltwasserMin != anderer.KaltwasserMin) n++;
        if (Modulkosten != anderer.Modulkosten) n++;
        if (Kennlinie.Count != anderer.Kennlinie.Count
            || Kennlinie.Where((p, i) => !p.Gleich(anderer.Kennlinie[i])).Any()) n++;
        return n;
    }

    private static bool Gleich(string? a, string? b)
        => string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.Ordinal);
}

/// <summary>Ein Punkt der Kennlinie: EER und Kälteleistung über Rückkühl- und Kaltwassertemperatur.</summary>
public sealed class KaeltemaschinePunktDaten
{
    /// <summary>Eintrittstemperatur des Rückkühlmediums [°C]; leer nur im Arbeitsstand eines neuen Punkts.</summary>
    public double? Rueckkuehltemperatur { get; set; }

    /// <summary>Kaltwasservorlauf [°C].</summary>
    public double? Kaltwassertemperatur { get; set; }

    /// <summary>EER im Punkt [—].</summary>
    public double? Eer { get; set; }

    /// <summary>Kälteleistung im Punkt [kW].</summary>
    public double? Kaelteleistung { get; set; }

    /// <summary>Eine Kopie des Punkts.</summary>
    public KaeltemaschinePunktDaten Kopie() => (KaeltemaschinePunktDaten)MemberwiseClone();

    /// <summary>Tragen beide Punkte dieselben vier Werte?</summary>
    public bool Gleich(KaeltemaschinePunktDaten p)
        => p is not null && Rueckkuehltemperatur == p.Rueckkuehltemperatur && Kaltwassertemperatur == p.Kaltwassertemperatur
           && Eer == p.Eer && Kaelteleistung == p.Kaelteleistung;
}

/// <summary>Der Ausgang eines Schreibwegs der Hülle: bei Erfolg die Id, sonst der Grund.</summary>
public sealed record KaeltemaschineSpeicherErgebnis(bool Ok, string Meldung, int Id);
