namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Eine Kältemaschinen-Anlage des Projekts</b> im Erzeugerdialog (KU3-4c) — der Feldsatz, in den Maske und
/// Hilfe-Assistent schreiben. Die Hülle (<c>KaeltemaschineAnlageHuelle</c>) bildet ihn auf
/// <c>KaeltemaschineAnlageModel</c> des Kerns ab; geschrieben wird erst mit „OK".
/// </summary>
public sealed class KaeltemaschineAnlageDaten
{
    /// <summary><c>Tab_Energieanlagen.ID</c>; 0 = im Dialog gewählt, beim OK angelegt.</summary>
    public int AnlagenId { get; set; }

    /// <summary>Der Katalogsatz einer NEUEN Anlage (<c>Tab_Kaeltemaschine_STAMM.ID</c>); 0 bei einer gespeicherten.</summary>
    public int StammId { get; set; }

    /// <summary>Die Projektkopie (<c>ID_Kaeltemaschine</c>); <c>null</c> bei einer neuen oder nach dem Löschen des Geräts.</summary>
    public int? GeraetId { get; set; }

    /// <summary>Anzeigename der Anlage.</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Anzahl gleicher Maschinen; <c>null</c> = leeres Feld (die Prüfung lehnt es ab).</summary>
    public int? Anzahl { get; set; } = 1;

    /// <summary>Kaltwasservorlauf [°C]; <c>null</c> = kleinste Stützstelle der Kennlinie.</summary>
    public double? KuehlVorlauf { get; set; }

    /// <summary>Hilfsstromanteil [0…1); <c>null</c> = kein Zuschlag.</summary>
    public double? KuehlHilfsstromanteil { get; set; }

    /// <summary>Kühlträger (<c>Kuehl_ID_Carrier</c>); <c>null</c> = Stromträger des Projekts.</summary>
    public int? KuehlCarrierId { get; set; }

    /// <summary>Abrechnung über einen eigenen Zähler — wirkt nur bei abweichendem Kühlträger.</summary>
    public bool KuehlEigenerZaehler { get; set; }

    /// <summary>Die Kennwerte des Geräts zum Lesen; <c>null</c> = keine bekannt.</summary>
    public KaeltemaschineGeraetwerte? Geraet { get; set; }

    /// <summary>Eine unabhängige Kopie (die Kennwerte werden geteilt, sie sind nur zu lesen).</summary>
    public KaeltemaschineAnlageDaten Kopie() => (KaeltemaschineAnlageDaten)MemberwiseClone();

    /// <summary>Tragen beide Sätze dieselben Eingaben?</summary>
    public bool GleicheEingaben(KaeltemaschineAnlageDaten? b)
        => b is not null && Bezeichner == b.Bezeichner && Anzahl == b.Anzahl && KuehlVorlauf == b.KuehlVorlauf
           && KuehlHilfsstromanteil == b.KuehlHilfsstromanteil && KuehlCarrierId == b.KuehlCarrierId
           && KuehlEigenerZaehler == b.KuehlEigenerZaehler;
}

/// <summary>
/// <b>Die Kennwerte eines Kältemaschinen-Geräts</b> — Katalogsatz oder Projektkopie, fertig für die Anzeige.
/// Die Rückkühlart kommt als Anzeigetext; sie ist hier nie ein Steuerwert.
/// </summary>
public sealed class KaeltemaschineGeraetwerte
{
    public string Bezeichner { get; init; } = "";
    public string Firma { get; init; } = "";
    public string Typ { get; init; } = "";
    public string Rueckkuehlart { get; init; } = "";
    public double? Nennkaelteleistung { get; init; }
    public double? NennEer { get; init; }
    public double? Mindestteillast { get; init; }
    public double? KaltwasserMin { get; init; }

    /// <summary>Die Kaltwassertemperaturen der Kennlinie, aufsteigend und ohne Doppel.</summary>
    public IReadOnlyList<double> Kaltwasserstuetzstellen { get; init; } = Array.Empty<double>();
}
