namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Ein Rückkühlwerk des Katalogs</b>, wie es die Verwaltung „Rückkühlwerke" zeigt und bearbeitet (K-F5a) — das DTO
/// zwischen <c>RueckkuehlwerkHuelle</c> und <see cref="RueckkuehlwerkDialog"/>, ohne Fachklasse des Kerns.
/// </summary>
/// <remarks>
/// <para><b>Leer heißt Vorgabe:</b> Ein Wert <c>null</c> bzw. ein leerer Steuerwert rechnet mit dem Festwert der passenden
/// Rückkühlart (<c>KaelteFestwerte</c>); die Verwaltung zeigt ihn als Platzhalter.</para>
/// <para><b>Die Auswahlen tragen den Persistenzwert</b> (<c>TROCKEN</c>, <c>FEST</c>, <c>STUFEN</c>, <c>REIHE</c> …), nie
/// den Anzeigetext; die Texte kommen als <see cref="RueckkuehlwerkWahl"/> aus der Hülle.</para>
/// <para><b>Der Gerätepreis</b> (<see cref="Modulkosten"/>) reist nur mit — ihn pflegt die Kostenverwaltung.</para>
/// </remarks>
public sealed class RueckkuehlwerkDaten
{
    /// <summary>Id im Katalog; 0 = noch nicht gespeichert.</summary>
    public int Id { get; set; }

    /// <summary>Name (Pflicht, im Katalog eindeutig).</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Freitext.</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Bauart (Persistenzwert); leer = trocken.</summary>
    public string Bauart { get; set; } = "";

    /// <summary>Abzuführende Wärme im Nennpunkt [kW].</summary>
    public double? NennleistungKw { get; set; }

    /// <summary>Grädigkeit im Nennpunkt [K]; leer = Festwert der passenden Rückkühlart.</summary>
    public double? AnnaeherungNennK { get; set; }

    /// <summary>Weg der Annäherung (Persistenzwert); leer = fest.</summary>
    public string AnnaeherungWeg { get; set; } = "";

    /// <summary>Ventilatorleistung im Nennpunkt [kW]; leer = Hilfsstrom der Rückkühlung an der Kältemaschine.</summary>
    public double? VentilatorNennKw { get; set; }

    /// <summary>Ventilatorregelung (Persistenzwert); leer = Ein/Aus.</summary>
    public string VentilatorRegelung { get; set; } = "";

    /// <summary>Zahl der Stufen bei Regelung Stufen (≥ 2).</summary>
    public int? VentilatorStufen { get; set; }

    /// <summary>Kleinste Drehzahl als Anteil der Nenndrehzahl [0…1] bei Regelung Drehzahl.</summary>
    public double? VentilatorDrehzahlMin { get; set; }

    /// <summary>Befeuchtungswirkungsgrad [0…1] (adiabat/hybrid).</summary>
    public double? BefeuchtungWirkungsgrad { get; set; }

    /// <summary>Außentemperatur, ab der befeuchtet wird [°C].</summary>
    public double? BefeuchtungAbC { get; set; }

    /// <summary>Faktor auf die rechnerische Verdunstung [—].</summary>
    public double? VerdunstungFaktor { get; set; }

    /// <summary>Eindickung (&gt; 1) [—].</summary>
    public double? Eindickung { get; set; }

    /// <summary>Driftverlust als Anteil des Umlaufs [0…1].</summary>
    public double? DriftAnteil { get; set; }

    /// <summary>Schaltung der freien Kühlung (Persistenzwert); leer = parallel.</summary>
    public string FreikuehlungSchaltung { get; set; } = "";

    /// <summary>Gerätepreis [€] — reist nur mit, die Verwaltung schreibt ihn nicht.</summary>
    public double? Modulkosten { get; set; }

    /// <summary>Gehört zur Auslieferung (Schloss) — dann nur lesbar.</summary>
    public bool Auslieferung { get; set; }

    /// <summary>Eine entkoppelte Kopie — der Arbeitsstand der Verwaltung.</summary>
    public RueckkuehlwerkDaten Kopie() => (RueckkuehlwerkDaten)MemberwiseClone();

    /// <summary>Wie viele der bearbeitbaren Felder von <paramref name="anderer"/> abweichen — der Zähler „n Felder geändert".</summary>
    public int Abweichungen(RueckkuehlwerkDaten anderer)
    {
        if (anderer is null) return 0;
        int n = 0;
        if (!Gleich(Bezeichner, anderer.Bezeichner)) n++;
        if (!Gleich(Beschreibung, anderer.Beschreibung)) n++;
        if (!Gleich(Bauart, anderer.Bauart)) n++;
        if (NennleistungKw != anderer.NennleistungKw) n++;
        if (AnnaeherungNennK != anderer.AnnaeherungNennK) n++;
        if (!Gleich(AnnaeherungWeg, anderer.AnnaeherungWeg)) n++;
        if (VentilatorNennKw != anderer.VentilatorNennKw) n++;
        if (!Gleich(VentilatorRegelung, anderer.VentilatorRegelung)) n++;
        if (VentilatorStufen != anderer.VentilatorStufen) n++;
        if (VentilatorDrehzahlMin != anderer.VentilatorDrehzahlMin) n++;
        if (BefeuchtungWirkungsgrad != anderer.BefeuchtungWirkungsgrad) n++;
        if (BefeuchtungAbC != anderer.BefeuchtungAbC) n++;
        if (VerdunstungFaktor != anderer.VerdunstungFaktor) n++;
        if (Eindickung != anderer.Eindickung) n++;
        if (DriftAnteil != anderer.DriftAnteil) n++;
        if (!Gleich(FreikuehlungSchaltung, anderer.FreikuehlungSchaltung)) n++;
        return n;
    }

    private static bool Gleich(string? a, string? b)
        => string.Equals((a ?? "").Trim(), (b ?? "").Trim(), System.StringComparison.Ordinal);
}

/// <summary>Was ein Schreibversuch der Verwaltung ergab — gelungen, die Meldung des Kerns, die Id des Satzes.</summary>
public sealed record RueckkuehlwerkSpeicherErgebnis(bool Ok, string Meldung, int Id);

/// <summary>Ein Eintrag einer Wertemenge: Persistenzwert und Anzeigetext.</summary>
public sealed record RueckkuehlwerkWahl(string Code, string Text);

/// <summary>Eine Bauart samt ihrer Einordnung: <c>Nass</c> = Verdunstungskühlanlage (Gruppe Wasser), <c>Befeuchtend</c> = adiabat/hybrid.</summary>
public sealed record RueckkuehlwerkBauart(string Code, string Text, bool Nass, bool Befeuchtend);

/// <summary>
/// Die Vorgaben, mit denen ein leeres Feld rechnet — aus dem Rechenweg des Kerns für die Bauart des Arbeitsstands
/// (<c>Rueckkuehlwerk.AusModell</c>); <c>Verdunstungskuehlanlage</c> trägt den Hinweis auf die 42. BImSchV.
/// </summary>
public sealed record RueckkuehlwerkVorgaben(
    double AnnaeherungK, int VentilatorStufen, double VentilatorDrehzahlMin, double BefeuchtungWirkungsgrad,
    double BefeuchtungAbC, double VerdunstungFaktor, double Eindickung, double DriftAnteil, bool Verdunstungskuehlanlage);
