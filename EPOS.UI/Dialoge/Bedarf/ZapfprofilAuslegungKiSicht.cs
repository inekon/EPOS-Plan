using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das flache Abbild der Überlagerung „Auslegung Brauchwasser" für den Hilfe-Assistenten
/// (Welle #458, Stufe 3a).
///
/// <para><b>Eine ÜBERLAGERUNG mit eigenem Arbeitsstand.</b> Die Auslegung bearbeitet eine
/// KOPIE der Eingaben; „OK" legt sie samt dem EINEN empfohlenen Punkt in den Arbeitsstand
/// des Zapfprofils, geschrieben wird mit dem OK der Bedarfsprofile. Einen Speicherweg meldet
/// sie nicht an.</para>
///
/// <para><b>Warum eine Sichtklasse und keine Reflection</b> auf
/// <see cref="ZapfprofilAuslegungEingabeDaten"/>: Der Bedarfstag ist EINE Wahl, die Quelle
/// UND Katalogtag zugleich setzt; Speicherart, Erzeugerart und Werkstoff sind
/// Aufzählungen; und jede Eingabe rechnet die Karten neu (<c>Geaendert</c>). Die nackten
/// Eigenschaften kennten nichts davon.</para>
///
/// <para><b>Was nicht auf der Maske steht, ist nicht setzbar</b>: Perzentil und
/// Realisierungen stehen nur mit „Stochastisch rechnen". Der Punkt ist Ergebnis und nur
/// lesbar. Die Sicht hält keinen Zustand.</para>
/// </summary>
public sealed class ZapfprofilAuslegungKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — die Überlagerung setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? BedarfstagLesen { get; init; }
    public Func<int?, string?>? BedarfstagSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? BedarfstagEintraege { get; init; }

    public Func<double?>? SpeichertemperaturLesen { get; init; }
    public Func<double?, string?>? SpeichertemperaturSetzen { get; init; }

    public Func<double?>? ErzeugerleistungLesen { get; init; }
    public Func<double?, string?>? ErzeugerleistungSetzen { get; init; }

    public Func<double?>? UebertragerleistungLesen { get; init; }
    public Func<double?, string?>? UebertragerleistungSetzen { get; init; }

    public Func<int?>? SpeicherartLesen { get; init; }
    public Func<int?, string?>? SpeicherartSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? SpeicherartEintraege { get; init; }

    public Func<double?>? SensorhoeheLesen { get; init; }
    public Func<double?, string?>? SensorhoeheSetzen { get; init; }

    public Func<int?>? ErzeugerartLesen { get; init; }
    public Func<int?, string?>? ErzeugerartSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? ErzeugerartEintraege { get; init; }

    public Func<int?>? WerkstoffLesen { get; init; }
    public Func<int?, string?>? WerkstoffSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? WerkstoffEintraege { get; init; }

    public Func<bool>? StochastischLesen { get; init; }
    public Func<bool, string?>? StochastischSetzen { get; init; }

    public Func<int?>? PerzentilLesen { get; init; }
    public Func<int?, string?>? PerzentilSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? PerzentilEintraege { get; init; }

    public Func<int?>? RealisierungenLesen { get; init; }
    public Func<int?, string?>? RealisierungenSetzen { get; init; }

    public Func<string>? PunktLesen { get; init; }

    // =====================================================================
    //  Die Wahllisten (KI‑D‑Q6)
    // =====================================================================

    /// <summary>Die Quellen des Bedarfstags samt Katalogtagen; gesperrte nennen beim Setzen ihren Grund.</summary>
    public IReadOnlyList<KiWahleintrag> BedarfstagWahl => Liste(BedarfstagEintraege);

    /// <summary>Ladespeicher oder gemischter Speicher.</summary>
    public IReadOnlyList<KiWahleintrag> SpeicherartWahl => Liste(SpeicherartEintraege);

    /// <summary>Keine Angabe, Kessel oder Wärmepumpe.</summary>
    public IReadOnlyList<KiWahleintrag> ErzeugerartWahl => Liste(ErzeugerartEintraege);

    /// <summary>Keine Angabe, Stahl oder Edelstahl.</summary>
    public IReadOnlyList<KiWahleintrag> WerkstoffWahl => Liste(WerkstoffEintraege);

    /// <summary>Die Perzentile des Schemas (P95, P99).</summary>
    public IReadOnlyList<KiWahleintrag> PerzentilWahl => Liste(PerzentilEintraege);

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Der Bedarfstag: Vorgaberegel, Stundenprofil, DIN-4708-Profil, Entwurf oder Katalogtag.</summary>
    public int? Bedarfstag
    {
        get => BedarfstagLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(BedarfstagSetzen, value);
    }

    /// <summary>Speichertemperatur [°C]; leer = Vorgabe.</summary>
    public double? Speichertemperatur
    {
        get => SpeichertemperaturLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(SpeichertemperaturSetzen, value);
    }

    /// <summary>Erzeugerleistung [kW]; leer = angesetzte Ladeleistung.</summary>
    public double? Erzeugerleistung
    {
        get => ErzeugerleistungLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(ErzeugerleistungSetzen, value);
    }

    /// <summary>Wärmeübertragerleistung [kW]; leer = aus U·A, Fläche oder Schätzformel.</summary>
    public double? Uebertragerleistung
    {
        get => UebertragerleistungLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(UebertragerleistungSetzen, value);
    }

    /// <summary>Die Speicherart als Zahl des Kerns.</summary>
    public int? Speicherart
    {
        get => SpeicherartLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(SpeicherartSetzen, value);
    }

    /// <summary>Sensorhöhe h_sensor/h_sto [-]; leer = Vorgabe.</summary>
    public double? Sensorhoehe
    {
        get => SensorhoeheLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(SensorhoeheSetzen, value);
    }

    /// <summary>Die Erzeugerart am Speicher — eine Laufangabe.</summary>
    public int? Erzeugerart
    {
        get => ErzeugerartLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(ErzeugerartSetzen, value);
    }

    /// <summary>Der Werkstoff des Übertragers — eine Laufangabe.</summary>
    public int? Werkstoff
    {
        get => WerkstoffLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(WerkstoffSetzen, value);
    }

    /// <summary>„Stochastisch rechnen" — zieht das Ensemble des Bedarfstags; eine Laufangabe.</summary>
    public bool Stochastisch
    {
        get => StochastischLesen?.Invoke() ?? false;
        set => ZapfprofilKiRegeln.Setze(StochastischSetzen, value);
    }

    /// <summary>Das Auslegungsperzentil (95 oder 99); ohne Eingabe die Vorgabe.</summary>
    public int? Perzentil
    {
        get => PerzentilLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(PerzentilSetzen, value);
    }

    /// <summary>Die Realisierungen des Bedarfstags; leer = Vorgabe des Kerns.</summary>
    public int? Realisierungen
    {
        get => RealisierungenLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(RealisierungenSetzen, value);
    }

    /// <summary>Der empfohlene Punkt, den OK übernimmt; leer ohne rechenbaren Punkt.</summary>
    public string Punkt => PunktLesen?.Invoke() ?? "";

    private static IReadOnlyList<KiWahleintrag> Liste(Func<IReadOnlyList<KiWahleintrag>>? quelle)
        => quelle?.Invoke() ?? Array.Empty<KiWahleintrag>();
}
