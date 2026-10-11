using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Maske „Wärmebedarf eines Gebäudes" für den Hilfe-Assistenten
/// (Welle KI‑F3).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>GebaeudeBedarfDaten</c>.</b> Jener Satz
/// trägt das eingefrorene Ergebnis und nur dieses; die ZWEI Bedienelemente der Maske —
/// die Anzeigeeinheit und der Schalter „sortiert" — liegen in ihren lebenden Feldern.
/// Ein am Satz angemeldeter Katalog hätte genau die zwei Werte nicht geführt, die der
/// Anwender hier einstellen kann.</para>
///
/// <para><b>Die Kennzahlen stehen in MWh und kW</b>, so wie der Rechenkern sie liefert.
/// Welche Einheit die Maske gerade ZEIGT, sagt das Wahlfeld daneben.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class GebaeudeBedarfKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<int?>? EinheitLesen { get; init; }
    public Action<int?>? EinheitSetzen { get; init; }

    public Func<bool>? SortiertLesen { get; init; }
    public Action<bool>? SortiertSetzen { get; init; }

    public Func<GebaeudeBedarfDaten?>? SatzLesen { get; init; }

    public Func<int?>? DiagrammLesen { get; init; }
    public Action<int?>? DiagrammSetzen { get; init; }

    /// <summary>Liefert die Einträge der Diagrammwahl (Stufe G6b): das Gebäude und seine Zonen.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? DiagrammEintraege { get; init; }

    /// <summary>Die Einträge der Diagrammwahl; leer bei einem Gebäude mit höchstens einer Zone.</summary>
    public IReadOnlyList<KiWahleintrag> DiagrammWahl
        => DiagrammEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Liefert die Energieeinheiten, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? EinheitEintraege { get; init; }

    /// <summary>Die Energieeinheiten der Klappliste (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> EinheitWahl
        => EinheitEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    private GebaeudeBedarfDaten? Satz => SatzLesen?.Invoke();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Die Anzeigeeinheit der Energiemengen als Platz in der Klappliste; sie wirkt auf
    /// die Kennzahlen, die Monatsübersicht und das Bild zugleich.
    /// </summary>
    public int? Einheit
    {
        get => EinheitLesen?.Invoke();
        set => EinheitSetzen?.Invoke(value);
    }

    /// <summary>
    /// Zeigt das Bild die DAUERLINIE statt der Jahresganglinie? Sortiert heißt: die
    /// 8 760 Stundenwerte der Größe nach.
    /// </summary>
    public bool Sortiert
    {
        get => SortiertLesen?.Invoke() ?? false;
        set => SortiertSetzen?.Invoke(value);
    }

    /// <summary>
    /// Für wen die Bilder Wärmelast und Raumtemperatur gelten (Stufe G6b): 0 = das ganze Gebäude,
    /// 1 … = die Zone dieses Rangs; leer bei einem Gebäude mit höchstens einer Zone.
    /// </summary>
    public int? Diagramm
    {
        get => DiagrammLesen?.Invoke();
        set => DiagrammSetzen?.Invoke(value);
    }

    /// <summary>Der Name des Gebäudes, dessen Bedarf die Maske zeigt.</summary>
    public string Gebaeude => Satz?.Name ?? "";

    /// <summary>Die Jahressumme der Heizwärme [MWh].</summary>
    public double? HeizwaermeMwh => Satz?.HeizwaermeMwh;

    /// <summary>Die höchste Stundenlast [kW].</summary>
    public double? MaxLastKw => Satz?.MaxLastKw;

    /// <summary>
    /// Die Vollbenutzungsstunden [h/a]; leer, wenn es sie nicht gibt (Höchstlast 0).
    /// </summary>
    public double? VollbenutzungsstundenH => Satz?.VollbenutzungsstundenH;

    // ---- Stufe KP3, Welle O2: Lüftungs- und Aufheizwerte (nur Anzeige) ----

    /// <summary>Stunden mit Sommerlüftung [h]; <c>null</c> ohne Sommerlüftung.</summary>
    public int? SommerlueftungsstundenH => Satz?.SommerlueftungsstundenH;

    /// <summary>Stunden mit wirksamer Nachtauskühlung [h]; <c>null</c> ohne Nachtauskühlung.</summary>
    public int? NachtauskuehlstundenH => Satz?.NachtauskuehlstundenH;

    // ---- Anlagenkopplung AK3 (Festlegungen 20 und 22): Kennzahlen des Kreises und Rückstufe (nur Anzeige) ----

    /// <summary>Durchläufe je Stunde im Mittel; leer ohne Lauf mit AK3.</summary>
    public double? Ak3DurchlaeufeMittel => Satz?.Ak3DurchlaeufeMittel;

    /// <summary>Die größte Zahl der Durchläufe einer Stunde.</summary>
    public int? Ak3DurchlaeufeMax => Satz?.Ak3DurchlaeufeMax;

    /// <summary>Wechsel der Stützstelle und des Betriebsfalls.</summary>
    public int? Ak3Fallwechsel => Satz?.Ak3Fallwechsel;

    /// <summary>Stunden an der Schranke des Angebots [h].</summary>
    public int? Ak3SchrankeStundenH => Satz?.Ak3SchrankeStundenH;

    /// <summary>Stunden mit leerem Heizungspuffer [h].</summary>
    public int? Ak3SpeicherLeerStundenH => Satz?.Ak3SpeicherLeerStundenH;

    /// <summary>Stunden mit Restbedarf der Kaskade [h].</summary>
    public int? Ak3RestbedarfStundenH => Satz?.Ak3RestbedarfStundenH;

    /// <summary>Der Hinweis der Rückstufe der Auskunft; leer ohne Rückstufe.</summary>
    public string Ak3Rueckstufe => Satz?.Rueckstufe ?? "";

    private GebaeudeBedarfAufheizDaten? Aufheizung => Satz?.Aufheizung;

    /// <summary>Der Zustand der Aufheizrechnung als Anzeigetext; leer ohne Gruppe.</summary>
    public string AufheizZustand => Aufheizung?.Zustandtext ?? "";

    /// <summary>t_auf,max — die bemessene Aufheizzeit [h].</summary>
    public int? AufheizzeitMaxH => Aufheizung?.AufheizzeitMaxH;

    /// <summary>T_a,B [°C].</summary>
    public double? AufheizAussenC => Aufheizung?.AussenC;

    /// <summary>P_auf [kW].</summary>
    public double? AufheizLeistungKw => Aufheizung?.LeistungKw;

    /// <summary>Die Quelle von P_auf als Anzeigetext.</summary>
    public string AufheizQuelle => Aufheizung?.Quelle ?? "";

    /// <summary>Tage mit einer Rampe.</summary>
    public int? Aufheiztage => Aufheizung?.Aufheiztage;

    /// <summary>Σ der Rampenstunden [h].</summary>
    public int? AufheizstundenH => Aufheizung?.AufheizstundenH;

    /// <summary>Die längste Rampe [h].</summary>
    public int? AufheizzeitLaengsteH => Aufheizung?.AufheizzeitLaengsteH;

    /// <summary>Σ der Kappungsanteile an der Heizleistungsgrenze [h].</summary>
    public double? KappungsstundenH => Aufheizung?.KappungsstundenH;

    /// <summary>Die Auslegungsgröße Φ_HL + Φ_RH [kW] (E60).</summary>
    public double? AuslegungsgroesseKw => Aufheizung?.AuslegungsgroesseKw;

    /// <summary>Φ_HL [kW].</summary>
    public double? AuslegungsheizlastKw => Aufheizung?.AuslegungsheizlastKw;

    /// <summary>Φ_RH [kW].</summary>
    public double? AufheizzuschlagKw => Aufheizung?.AufheizzuschlagKw;

    /// <summary>
    /// Die Zeilen der Zonentabelle (Stufe G6b; AK1z, E63) mit dem Zonennamen als Kennzeichen — nur
    /// zum Lesen; leer bei einem Gebäude mit höchstens einer Zone.
    /// </summary>
    public IReadOnlyList<GebaeudeBedarfZoneKiZeile> Zonen
        => Satz is { Zonen.Count: >= 2 } s
            ? s.Zonen.Select(z => new GebaeudeBedarfZoneKiZeile(z)).ToList()
            : Array.Empty<GebaeudeBedarfZoneKiZeile>();
}

/// <summary>
/// Eine Zone der Zonentabelle als ZEILE der Sichtklasse <see cref="GebaeudeBedarfKiSicht"/> — nur
/// zum Lesen: der Name als Kennzeichen und der Heizkreis der Zone (AK1z, E63), leer ohne Kopplung.
/// </summary>
public sealed class GebaeudeBedarfZoneKiZeile
{
    private readonly GebaeudeBedarfZoneDaten _zone;

    /// <summary>Legt die Zeile zu einer Zone des Ergebnisses an.</summary>
    public GebaeudeBedarfZoneKiZeile(GebaeudeBedarfZoneDaten zone)
    {
        _zone = zone ?? throw new ArgumentNullException(nameof(zone));
    }

    /// <summary>Das ZEILENKENNZEICHEN — der Zonenname, wie ihn die Tabelle zeigt.</summary>
    public string Kennzeichen => _zone.Name;

    /// <summary>Der Zonenname.</summary>
    public string Zonenname => _zone.Name;

    /// <summary>Mittlerer Vorlauf des Zonenkreises [°C]; leer ohne Kopplung.</summary>
    public double? VorlaufMittelC => _zone.VorlaufMittelC;

    /// <summary>Mittlerer Rücklauf des Zonenkreises [°C]; leer ohne Kopplung.</summary>
    public double? RuecklaufMittelC => _zone.RuecklaufMittelC;

    /// <summary>Stunden mit begrenzender Übergabe der Zone [h]; leer ohne Kopplung.</summary>
    public double? UebergabeBegrenztH => _zone.UebergabeBegrenztH;
}
