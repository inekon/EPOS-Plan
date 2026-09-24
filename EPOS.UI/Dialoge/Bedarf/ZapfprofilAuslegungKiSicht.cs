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

    // Die Eingaben des Verfahrensvergleichs (4.7, N11 (d); Z4, Gruppe 2b)
    public Func<int?>? LadeleistungModusLesen { get; init; }
    public Func<int?, string?>? LadeleistungModusSetzen { get; init; }
    public Func<double?>? LadeleistungManuellLesen { get; init; }
    public Func<double?, string?>? LadeleistungManuellSetzen { get; init; }
    public Func<double?>? LadefensterLesen { get; init; }
    public Func<double?, string?>? LadefensterSetzen { get; init; }
    public Func<double?>? LadefensterBeginnLesen { get; init; }
    public Func<double?, string?>? LadefensterBeginnSetzen { get; init; }
    public Func<double?>? NutzanteilLesen { get; init; }
    public Func<double?, string?>? NutzanteilSetzen { get; init; }
    public Func<double?>? ZuschlagLesen { get; init; }
    public Func<double?, string?>? ZuschlagSetzen { get; init; }
    public Func<int?>? PersonenModusLesen { get; init; }
    public Func<int?, string?>? PersonenModusSetzen { get; init; }
    public Func<double?>? PersonenManuellLesen { get; init; }
    public Func<double?, string?>? PersonenManuellSetzen { get; init; }
    public Func<int?>? FuellstandBezugLesen { get; init; }
    public Func<int?, string?>? FuellstandBezugSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? AutoManuellEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? FuellstandBezugEintraege { get; init; }

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

    /// <summary>Die Erzeugerart am Speicher — gespeichert mit dem Projekt (Schritt 121).</summary>
    public int? Erzeugerart
    {
        get => ErzeugerartLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(ErzeugerartSetzen, value);
    }

    /// <summary>Der Werkstoff des Übertragers — gespeichert mit dem Projekt (Schritt 121).</summary>
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

    // =====================================================================
    //  Die Eingaben des Verfahrensvergleichs (nachrichtlich; gespeichert mit dem Projekt)
    // =====================================================================

    /// <summary>auto (0) oder manuell (1) — für Ladeleistung und Personen.</summary>
    public IReadOnlyList<KiWahleintrag> LadeleistungModusWahl => Liste(AutoManuellEintraege);

    /// <summary>auto (0) oder manuell (1).</summary>
    public IReadOnlyList<KiWahleintrag> PersonenModusWahl => Liste(AutoManuellEintraege);

    /// <summary>Vorgabe, Nenninhalt des Punkts, Punkt, Nenninhalt des Bands, V_max.</summary>
    public IReadOnlyList<KiWahleintrag> FuellstandBezugWahl => Liste(FuellstandBezugEintraege);

    /// <summary>Die Ladeleistung des Vergleichs: 0 auto (Vorschlag), 1 manuell.</summary>
    public int? LadeleistungModus
    {
        get => LadeleistungModusLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(LadeleistungModusSetzen, value);
    }

    /// <summary>Die manuelle Ladeleistung [kW]; wirkt nur bei „manuell".</summary>
    public double? LadeleistungManuell
    {
        get => LadeleistungManuellLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(LadeleistungManuellSetzen, value);
    }

    /// <summary>Das Ladezeitfenster [h/d]; leer = Vorgabe.</summary>
    public double? Ladefenster
    {
        get => LadefensterLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(LadefensterSetzen, value);
    }

    /// <summary>Der Beginn des Ladezeitfensters [h]; leer = Vorgabe.</summary>
    public double? LadefensterBeginn
    {
        get => LadefensterBeginnLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(LadefensterBeginnSetzen, value);
    }

    /// <summary>Der nutzbare Anteil des Speichervolumens [-]; leer = Vorgabe.</summary>
    public double? Nutzanteil
    {
        get => NutzanteilLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(NutzanteilSetzen, value);
    }

    /// <summary>Der Zuschlag [-]; leer = Vorgabe.</summary>
    public double? Zuschlag
    {
        get => ZuschlagLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(ZuschlagSetzen, value);
    }

    /// <summary>Die Personen des Vergleichs: 0 auto (Mengengerüst), 1 manuell.</summary>
    public int? PersonenModus
    {
        get => PersonenModusLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(PersonenModusSetzen, value);
    }

    /// <summary>Die manuelle Personenzahl; wirkt nur bei „manuell".</summary>
    public double? PersonenManuell
    {
        get => PersonenManuellLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(PersonenManuellSetzen, value);
    }

    /// <summary>Der Bezug des Füllstands als Zahl (0 Vorgabe … 4 V_max).</summary>
    public int? FuellstandBezug
    {
        get => FuellstandBezugLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(FuellstandBezugSetzen, value);
    }

    private static IReadOnlyList<KiWahleintrag> Liste(Func<IReadOnlyList<KiWahleintrag>>? quelle)
        => quelle?.Invoke() ?? Array.Empty<KiWahleintrag>();
}
