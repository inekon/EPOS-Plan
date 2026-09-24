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
/// <para><b>Was nicht auf der Maske steht, ist nicht setzbar</b>: Der Rechenweg und die Angaben
/// der Stufe „Erweitert" stehen ab dieser Stufe, Seed, Realisierungen (nur beim Rechenweg
/// „stochastisch") und die Fachwerte erst in der Stufe „Experte"; die Wohnungstabelle nur bei
/// einer Nutzungsart „Wohnen", die Felder einer Zirkulationsmethode nur bei ihr. Davor nennt die
/// Absage, was sie sichtbar macht. Die Angaben der höheren Stufen gelten der GEWÄHLTEN Zone —
/// wie ihr Eingabeblock — und dem Gebäude (Stufe Z4).</para>
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

    public Func<bool>? TyptagewegLesen { get; init; }
    public Func<bool, string?>? TyptagewegSetzen { get; init; }

    public Func<int?>? TyptagzoneLesen { get; init; }
    public Func<int?, string?>? TyptagzoneSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? TyptagzoneEintraege { get; init; }

    public Func<int?>? TyptagartLesen { get; init; }
    public Func<int?, string?>? TyptagartSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? TyptagartEintraege { get; init; }

    public Func<int?>? SeedLesen { get; init; }
    public Func<int?, string?>? SeedSetzen { get; init; }

    public Func<int?>? RealisierungenLesen { get; init; }
    public Func<int?, string?>? RealisierungenSetzen { get; init; }

    // =====================================================================
    //  Die Wahllisten (KI‑D‑Q6)
    // =====================================================================

    /// <summary>Die drei Stufen Einfach, Erweitert und Experte — alle wählbar.</summary>
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

    /// <summary>
    /// <b>Rechnet der Jahresgang über die eingespielten Typtage?</b> (Stufe Experte, 4.2; Z4b)
    /// Eine Größe des PROJEKTS. Ohne eingespielte Typtage benannt abgelehnt — dieselbe Sperre wie
    /// am Schalter des Dialogs.
    /// </summary>
    public bool Typtageweg
    {
        get => TyptagewegLesen?.Invoke() ?? false;
        set => ZapfprofilKiRegeln.Setze(TyptagewegSetzen, value);
    }

    /// <summary>Die Klimazone der Typtage — nur eine Nummer des eingespielten Pakets; leer = keine Wahl.</summary>
    public int? Typtagzone
    {
        get => TyptagzoneLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(TyptagzoneSetzen, value);
    }

    /// <summary>Die Gebäudeart der Typtage — nur eine des eingespielten Pakets; leer = keine Wahl.</summary>
    public int? Typtagart
    {
        get => TyptagartLesen?.Invoke();
        set => ZapfprofilKiRegeln.Setze(TyptagartSetzen, value);
    }

    /// <summary>Die Klimazonen des eingespielten Pakets (KI‑D‑Q6) — Schlüssel ist die Nummer.</summary>
    public IReadOnlyList<KiWahleintrag> TyptagzoneWahl => TyptagzoneEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Gebäudearten des eingespielten Pakets (KI‑D‑Q6) — Schlüssel ist der Platz in der Liste.</summary>
    public IReadOnlyList<KiWahleintrag> TyptagartWahl => TyptagartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

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

    // =====================================================================
    //  Stufen Erweitert und Experte (Z4): die GEWÄHLTE Zone und das Gebäude
    // =====================================================================
    //
    // Die Angaben der höheren Stufen stehen — wie auf der Maske — für die gewählte Zone
    // (Satzwahl „zone") und fürs Gebäude. Gelesen und gesetzt wird über EINEN Weg je Richtung,
    // benannt nach der Eigenschaft: Der Dialog prüft Stufe, Sichtbarkeit und Grenzen wie am Feld
    // und nimmt den Weg des Eingabefeldes.

    /// <summary>Liest eine Angabe der höheren Stufen nach dem Namen ihrer Eigenschaft; <c>null</c> = leer.</summary>
    public Func<string, object?>? AngabeLesen { get; init; }

    /// <summary>Setzt eine Angabe nach dem Namen ihrer Eigenschaft; zurück kommt der Grund einer Ablehnung.</summary>
    public Func<string, object?, string?>? AngabeSetzen { get; init; }

    /// <summary>Die Einträge einer Wahl der höheren Stufen nach dem Namen ihrer Eigenschaft.</summary>
    public Func<string, IReadOnlyList<KiWahleintrag>>? AngabeWahl { get; init; }

    /// <summary>Die Wohnungstabelle der gewählten Zone — je Wohnungstyp eine Zeile.</summary>
    public Func<IReadOnlyList<ZapfprofilWohnungKiZeile>>? WohnungenLesen { get; init; }

    /// <summary>Die Zeilen der Wohnungstabelle der gewählten Zone (nur Wohnen, Stufe Erweitert).</summary>
    public IReadOnlyList<ZapfprofilWohnungKiZeile> Wohnungen
        => WohnungenLesen?.Invoke() ?? Array.Empty<ZapfprofilWohnungKiZeile>();

    public IReadOnlyList<KiWahleintrag> AusstattungWahl => Wahl(nameof(ZapfprofilWohnungKiZeile.Ausstattung));

    // ---- Belegung und Anlage (Erweitert) --------------------------------------------

    public double? PersonenJeWe { get => Zahl(nameof(PersonenJeWe)); set => Setze(nameof(PersonenJeWe), value); }
    public double? WohnflaecheJeWe { get => Zahl(nameof(WohnflaecheJeWe)); set => Setze(nameof(WohnflaecheJeWe), value); }
    public int? Topologie { get => Ganz(nameof(Topologie)); set => Setze(nameof(Topologie), value); }
    public IReadOnlyList<KiWahleintrag> TopologieWahl => Wahl(nameof(Topologie));
    public int? ZirkulationVorhanden { get => Ganz(nameof(ZirkulationVorhanden)); set => Setze(nameof(ZirkulationVorhanden), value); }
    public IReadOnlyList<KiWahleintrag> ZirkulationVorhandenWahl => Wahl(nameof(ZirkulationVorhanden));
    public int? Kalender { get => Ganz(nameof(Kalender)); set => Setze(nameof(Kalender), value); }
    public IReadOnlyList<KiWahleintrag> KalenderWahl => Wahl(nameof(Kalender));

    /// <summary>Die vier Ferienzeiträume der gewählten Zone als Zeilen (Muster Gebäudekatalog).</summary>
    public Func<IReadOnlyList<ZapfprofilFerienKiZeile>>? FerienLesen { get; init; }

    /// <summary>Die Ferienzeiträume 1 … 4 der gewählten Zone — Beginn und Ende als Tag und Monat.</summary>
    public IReadOnlyList<ZapfprofilFerienKiZeile> Ferien
        => FerienLesen?.Invoke() ?? Array.Empty<ZapfprofilFerienKiZeile>();

    public double? Jahresmesswert { get => Zahl(nameof(Jahresmesswert)); set => Setze(nameof(Jahresmesswert), value); }
    public int? MesswertEinheit { get => Ganz(nameof(MesswertEinheit)); set => Setze(nameof(MesswertEinheit), value); }
    public IReadOnlyList<KiWahleintrag> MesswertEinheitWahl => Wahl(nameof(MesswertEinheit));
    public int? MesswertGrenze { get => Ganz(nameof(MesswertGrenze)); set => Setze(nameof(MesswertGrenze), value); }
    public IReadOnlyList<KiWahleintrag> MesswertGrenzeWahl => Wahl(nameof(MesswertGrenze));
    public double? Speicherverlust { get => Zahl(nameof(Speicherverlust)); set => Setze(nameof(Speicherverlust), value); }
    public string MesswertQuelle { get => Text(nameof(MesswertQuelle)); set => Setze(nameof(MesswertQuelle), value); }
    public string MesswertZeitraum { get => Text(nameof(MesswertZeitraum)); set => Setze(nameof(MesswertZeitraum), value); }

    // ---- Tagesbedarf, Ladeleistung, Zirkulation (Erweitert) --------------------------

    public int? TagesbedarfModus { get => Ganz(nameof(TagesbedarfModus)); set => Setze(nameof(TagesbedarfModus), value); }
    public IReadOnlyList<KiWahleintrag> TagesbedarfModusWahl => Wahl(nameof(TagesbedarfModus));
    public double? TagesbedarfManuell { get => Zahl(nameof(TagesbedarfManuell)); set => Setze(nameof(TagesbedarfManuell), value); }
    public int? LadeleistungModus { get => Ganz(nameof(LadeleistungModus)); set => Setze(nameof(LadeleistungModus), value); }
    public IReadOnlyList<KiWahleintrag> LadeleistungModusWahl => Wahl(nameof(LadeleistungModus));
    public double? LadeleistungManuell { get => Zahl(nameof(LadeleistungManuell)); set => Setze(nameof(LadeleistungManuell), value); }
    public double? Ladefenster { get => Zahl(nameof(Ladefenster)); set => Setze(nameof(Ladefenster), value); }
    public double? LadefensterBeginn { get => Zahl(nameof(LadefensterBeginn)); set => Setze(nameof(LadefensterBeginn), value); }
    public int? ZirkulationModus { get => Ganz(nameof(ZirkulationModus)); set => Setze(nameof(ZirkulationModus), value); }
    public IReadOnlyList<KiWahleintrag> ZirkulationModusWahl => Wahl(nameof(ZirkulationModus));
    public int? ZirkMethode { get => Ganz(nameof(ZirkMethode)); set => Setze(nameof(ZirkMethode), value); }
    public IReadOnlyList<KiWahleintrag> ZirkMethodeWahl => Wahl(nameof(ZirkMethode));
    public double? ZirkLaenge { get => Zahl(nameof(ZirkLaenge)); set => Setze(nameof(ZirkLaenge), value); }
    public double? ZirkVerlust { get => Zahl(nameof(ZirkVerlust)); set => Setze(nameof(ZirkVerlust), value); }
    public double? ZirkAnteil { get => Zahl(nameof(ZirkAnteil)); set => Setze(nameof(ZirkAnteil), value); }
    public int? ZirkLage { get => Ganz(nameof(ZirkLage)); set => Setze(nameof(ZirkLage), value); }
    public IReadOnlyList<KiWahleintrag> ZirkLageWahl => Wahl(nameof(ZirkLage));
    public double? ZirkManuell { get => Zahl(nameof(ZirkManuell)); set => Setze(nameof(ZirkManuell), value); }
    public double? Leitungsinhalt { get => Zahl(nameof(Leitungsinhalt)); set => Setze(nameof(Leitungsinhalt), value); }

    // ---- Fachwerte (Experte) ---------------------------------------------------------

    public double? BedarfSpez { get => Zahl(nameof(BedarfSpez)); set => Setze(nameof(BedarfSpez), value); }
    public double? Zapftemperatur { get => Zahl(nameof(Zapftemperatur)); set => Setze(nameof(Zapftemperatur), value); }
    public double? KaltwasserMittel { get => Zahl(nameof(KaltwasserMittel)); set => Setze(nameof(KaltwasserMittel), value); }
    public double? KaltwasserAmplitude { get => Zahl(nameof(KaltwasserAmplitude)); set => Setze(nameof(KaltwasserAmplitude), value); }

    /// <summary>Der Auslastungsgang: zwölf Monatsfaktoren, leer je Monat = Katalog.</summary>
    public double?[]? Auslastungsgang { get => Reihe(nameof(Auslastungsgang)); set => Setze(nameof(Auslastungsgang), value); }

    public int? Tagesgangsatz { get => Ganz(nameof(Tagesgangsatz)); set => Setze(nameof(Tagesgangsatz), value); }
    public IReadOnlyList<KiWahleintrag> TagesgangsatzWahl => Wahl(nameof(Tagesgangsatz));
    public double? KaltwasserAuslegung { get => Zahl(nameof(KaltwasserAuslegung)); set => Setze(nameof(KaltwasserAuslegung), value); }
    public double? Speichertemperatur { get => Zahl(nameof(Speichertemperatur)); set => Setze(nameof(Speichertemperatur), value); }
    public double? ZirkKennwert { get => Zahl(nameof(ZirkKennwert)); set => Setze(nameof(ZirkKennwert), value); }
    public double? ZirkFlaeche { get => Zahl(nameof(ZirkFlaeche)); set => Setze(nameof(ZirkFlaeche), value); }
    public double? ZirkLaufzeit { get => Zahl(nameof(ZirkLaufzeit)); set => Setze(nameof(ZirkLaufzeit), value); }
    public double? Anzeigetemperatur { get => Zahl(nameof(Anzeigetemperatur)); set => Setze(nameof(Anzeigetemperatur), value); }
    public double? Stundenschwelle { get => Zahl(nameof(Stundenschwelle)); set => Setze(nameof(Stundenschwelle), value); }

    private double? Zahl(string feld) => AngabeLesen?.Invoke(feld) as double?;
    private int? Ganz(string feld) => AngabeLesen?.Invoke(feld) as int?;
    private string Text(string feld) => AngabeLesen?.Invoke(feld) as string ?? "";
    private double?[]? Reihe(string feld) => AngabeLesen?.Invoke(feld) as double?[];

    private IReadOnlyList<KiWahleintrag> Wahl(string feld) => AngabeWahl?.Invoke(feld) ?? Array.Empty<KiWahleintrag>();

    private void Setze(string feld, object? wert)
        => ZapfprofilKiRegeln.Setze(AngabeSetzen is null ? null : new Func<object?, string?>(w => AngabeSetzen(feld, w)), wert);

    private static IReadOnlyList<KiWahleintrag> Liste(Func<IReadOnlyList<KiWahleintrag>>? quelle)
        => quelle?.Invoke() ?? Array.Empty<KiWahleintrag>();
}

/// <summary>
/// Ein Ferienzeitraum der gewählten Zone als ZEILE der Sichtklasse <see cref="ZapfprofilKiSicht"/>
/// (Stufe Erweitert) — Beginn und Ende als Tag und Monat, wie die vier Felder der Maske; das
/// Kennzeichen ist die Nummer des Zeitraums.
/// </summary>
public sealed class ZapfprofilFerienKiZeile
{
    private readonly ZapfprofilFerienDaten _ferien;
    private readonly int _nummer;

    /// <summary>Legt die Zeile zu einem Ferienzeitraum an; <paramref name="nummer"/> zählt ab 1.</summary>
    public ZapfprofilFerienKiZeile(ZapfprofilFerienDaten ferien, int nummer)
    {
        _ferien = ferien ?? throw new ArgumentNullException(nameof(ferien));
        _nummer = nummer;
    }

    public Func<int?, string?>? BeginnTagSetzen { get; init; }
    public Func<int?, string?>? BeginnMonatSetzen { get; init; }
    public Func<int?, string?>? EndeTagSetzen { get; init; }
    public Func<int?, string?>? EndeMonatSetzen { get; init; }

    /// <summary>Das ZEILENKENNZEICHEN — die Nummer des Zeitraums („2"), wie die Maske ihn beschriftet.</summary>
    public string Zeitraum => _nummer.ToString(CultureInfo.InvariantCulture);

    public int? BeginnTag { get => _ferien.BeginnTag; set => ZapfprofilKiRegeln.Setze(BeginnTagSetzen, value); }
    public int? BeginnMonat { get => _ferien.BeginnMonat; set => ZapfprofilKiRegeln.Setze(BeginnMonatSetzen, value); }
    public int? EndeTag { get => _ferien.EndeTag; set => ZapfprofilKiRegeln.Setze(EndeTagSetzen, value); }
    public int? EndeMonat { get => _ferien.EndeMonat; set => ZapfprofilKiRegeln.Setze(EndeMonatSetzen, value); }
}

/// <summary>
/// Ein Wohnungstyp der Wohnungstabelle der gewählten Zone als ZEILE der Sichtklasse
/// <see cref="ZapfprofilKiSicht"/> (Stufe Erweitert, nur Wohnen). Sie hält die Zeile selbst — ist
/// sie weg, lehnt der Setzer benannt ab.
/// </summary>
public sealed class ZapfprofilWohnungKiZeile
{
    private readonly ZapfprofilWohnungDaten _wohnung;
    private readonly int _nummer;

    /// <summary>Legt die Zeile zu einem Wohnungstyp an; <paramref name="nummer"/> zählt ab 1.</summary>
    public ZapfprofilWohnungKiZeile(ZapfprofilWohnungDaten wohnung, int nummer)
    {
        _wohnung = wohnung ?? throw new ArgumentNullException(nameof(wohnung));
        _nummer = nummer;
    }

    public Func<int?, string?>? AnzahlSetzen { get; init; }
    public Func<double?, string?>? RaumzahlSetzen { get; init; }
    public Func<double?, string?>? PersonenSetzen { get; init; }
    public Func<int?, string?>? AusstattungSetzen { get; init; }

    /// <summary>Das ZEILENKENNZEICHEN — die Nummer der Zeile, wie der Tabellenkopf sie zählt („1").</summary>
    public string Kennzeichen => _nummer.ToString(CultureInfo.InvariantCulture);

    public int? Anzahl { get => _wohnung.Anzahl; set => ZapfprofilKiRegeln.Setze(AnzahlSetzen, value); }
    public double? Raumzahl { get => _wohnung.Raumzahl; set => ZapfprofilKiRegeln.Setze(RaumzahlSetzen, value); }
    public double? Personen { get => _wohnung.Personen; set => ZapfprofilKiRegeln.Setze(PersonenSetzen, value); }
    public int? Ausstattung { get => _wohnung.IdAusstattung; set => ZapfprofilKiRegeln.Setze(AusstattungSetzen, value); }
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
