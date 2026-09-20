namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// Das FLACHE Abbild der Maske „Wärmequelle Pufferspeicher" für den Hilfe-Assistenten
/// (Welle KI‑F2).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>QuellePufferspeicherDaten</c>:</b>
/// dieselbe Begründung wie bei <see cref="QuelleErdreichKiSicht"/> — der Satz ist ein
/// unveränderlicher Record, der Dialog erzeugt beim OK über <c>with</c> einen neuen und
/// schreibt nie in den hereingereichten.</para>
///
/// <para><b>ZWEI Erzeugerarten in einer Maske.</b> Die Wärmepumpe pflegt
/// Quelltemperatur, Spreizung, Regeneration und „Quelle unbegrenzt"; der Heizkessel
/// pflegt den Temperaturbezug samt festem Vorlauf und Rücklauf. Die Entnahmehöhe gilt
/// beiden. Welche Felder die Maske zeigt, entscheidet die Erzeugerart — was die jeweils
/// andere angeht, bleibt unangetastet, und ein Setzen darauf bliebe ohne sichtbare
/// Wirkung.</para>
/// </summary>
public sealed class QuellePufferspeicherKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<double?>? QuelltemperaturLesen { get; init; }
    public Action<double?>? QuelltemperaturSetzen { get; init; }

    public Func<double?>? SpreizungLesen { get; init; }
    public Action<double?>? SpreizungSetzen { get; init; }

    public Func<double?>? RegenerationLesen { get; init; }
    public Action<double?>? RegenerationSetzen { get; init; }

    public Func<bool>? UnbegrenztLesen { get; init; }
    public Action<bool>? UnbegrenztSetzen { get; init; }

    public Func<bool>? TemperaturFestLesen { get; init; }
    public Action<bool>? TemperaturFestSetzen { get; init; }

    public Func<int?>? VorlaufLesen { get; init; }
    public Action<int?>? VorlaufSetzen { get; init; }

    public Func<int?>? RuecklaufLesen { get; init; }
    public Action<int?>? RuecklaufSetzen { get; init; }

    public Func<double?>? AnschlusshoeheLesen { get; init; }
    public Action<double?>? AnschlusshoeheSetzen { get; init; }

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Nur Wärmepumpe: die Quelltemperatur am Verdampfer [°C].</summary>
    public double? Quelltemperatur
    {
        get => QuelltemperaturLesen?.Invoke();
        set => QuelltemperaturSetzen?.Invoke(value);
    }

    /// <summary>Nur Wärmepumpe: die nutzbare Spreizung [K]; sie muss größer als 0 sein.</summary>
    public double? Spreizung
    {
        get => SpreizungLesen?.Invoke();
        set => SpreizungSetzen?.Invoke(value);
    }

    /// <summary>Nur Wärmepumpe: die Regenerationsleistung der Quelle [kW].</summary>
    public double? Regeneration
    {
        get => RegenerationLesen?.Invoke();
        set => RegenerationSetzen?.Invoke(value);
    }

    /// <summary>Nur Wärmepumpe: Die Quelle gilt als unbegrenzt verfügbar.</summary>
    public bool Unbegrenzt
    {
        get => UnbegrenztLesen?.Invoke() ?? false;
        set => UnbegrenztSetzen?.Invoke(value);
    }

    /// <summary>
    /// Nur Heizkessel: <c>true</c> = Vorlauf und Rücklauf sind fest vorgegeben,
    /// <c>false</c> = sie werden aus der Anlage berechnet.
    /// </summary>
    public bool TemperaturFest
    {
        get => TemperaturFestLesen?.Invoke() ?? false;
        set => TemperaturFestSetzen?.Invoke(value);
    }

    /// <summary>Nur Heizkessel, nur bei fester Vorgabe: der Vorlauf [°C].</summary>
    public int? Vorlauf
    {
        get => VorlaufLesen?.Invoke();
        set => VorlaufSetzen?.Invoke(value);
    }

    /// <summary>Nur Heizkessel, nur bei fester Vorgabe: der Rücklauf [°C].</summary>
    public int? Ruecklauf
    {
        get => RuecklaufLesen?.Invoke();
        set => RuecklaufSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die Quell-Entnahmehöhe im Speicher, 0 (unten) bis 1 (oben) — für beide
    /// Erzeugerarten; leer heißt „oben".
    /// </summary>
    public double? Anschlusshoehe
    {
        get => AnschlusshoeheLesen?.Invoke();
        set => AnschlusshoeheSetzen?.Invoke(value);
    }
}
