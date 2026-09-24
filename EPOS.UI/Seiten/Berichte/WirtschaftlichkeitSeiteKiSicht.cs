using System.Globalization;
using EPOS.UI.Dienste;
using KiKern;
using WindowsFormsApplication1;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// Das FLACHE Abbild des Reiterblatts „Wirtschaftlichkeit" für den
/// Hilfe-Assistenten (Welle KI‑F4).
///
/// <para><b>Diese Seite trägt Einstellwerte und ist deshalb drin.</b> Sie
/// entscheidet, WELCHE Stände gegeneinander gerechnet werden (Vergleichssicht,
/// Referenz, Paar A und B), unter WELCHEM Szenario (Erwartet, Best, Worst) — und
/// sie pflegt die nicht monetarisierbaren Wirkungen nach DIN EN 17463 als Liste
/// (ETAPPE E17: Kategorie, Beschreibung, Dauer, drei Wirkungsgrade je Zeile). Die
/// Kennzahltabelle, die Herleitungszeilen und das Bild des Kapitalwertverlaufs
/// darunter sind gerechnete Anzeige und bleiben draußen; die Bedienleiste des
/// Verlaufs (Zeitraum, Haken je Stand und Szenario) ist drin.</para>
///
/// <para><b>Warum ein Sichtmodell.</b> Jedes dieser Felder ist an der Seite ein
/// WEG und kein Wert: Die Seite holt sich zu jeder Wahl einen NEUEN Stand aus der
/// Hülle (<c>Uebernehmen</c>) und rechnet das Warnband nach. Ein Katalog am Stand
/// schriebe die Zahl hin, und die Seite zeigte die Kennzahlen von vorhin.</para>
///
/// <para><b>Die Anzeigewahlen von Block 2 sind drin</b> (ValERI-Bewertung,
/// „Zahlungsreihen"; KI‑D‑Q11): Stand und Szenario der Jahrestafel wählen nur, welche
/// schon gelieferte Tafel die Seite zeigt — kein neuer Stand, kein Nachrechnen. Gelesen
/// wird die GEZEIGTE Wahl (Vorgabe: die Leitversion im Erwartungsfall).</para>
///
/// <para><b>Die Vergleichsgruppe bleibt draußen</b>: Welche Varianten angehakt
/// sind, ist eine Menge von Verweisen und kein Feldwert.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
/// </summary>
public sealed class WirtschaftlichkeitSeiteKiSicht
{
    public Func<int?>? SzenarioLesen { get; init; }
    public Action<int?>? SzenarioSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? SzenarioEintraege { get; init; }

    public Func<int?>? SichtLesen { get; init; }
    public Action<int?>? SichtSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? SichtEintraege { get; init; }

    public Func<int?>? ReferenzLesen { get; init; }
    public Action<int?>? ReferenzSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? StandEintraege { get; init; }

    public Func<int?>? ALesen { get; init; }
    public Action<int?>? ASetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? AEintraege { get; init; }

    public Func<int?>? BLesen { get; init; }
    public Action<int?>? BSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? BEintraege { get; init; }

    /// <summary>ETAPPE E17: die Zeilen der Wirkungsliste (Arbeitsstand der Seite).</summary>
    public Func<IReadOnlyList<WirkungKiZeile>>? WirkungenLesen { get; init; }

    /// <summary>ETAPPE E17: setzt die Zahl der Wirkungen (anhängen bzw. vom Ende entfernen).</summary>
    public Action<int>? WirkungsanzahlSetzen { get; init; }

    public Func<int?>? ZahlungsstandLesen { get; init; }
    public Action<int?>? ZahlungsstandSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? ZahlungsstandEintraege { get; init; }

    public Func<int?>? ZahlungsszenarioLesen { get; init; }
    public Action<int?>? ZahlungsszenarioSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? ZahlungsszenarioEintraege { get; init; }

    // =====================================================================
    //  Die Wahllisten (KI-D-Q6)
    // =====================================================================

    /// <summary>Die Szenarien — Erwartet, Best und Worst.</summary>
    public IReadOnlyList<KiWahleintrag> SzenarioWahl
        => SzenarioEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die zwei Vergleichssichten.</summary>
    public IReadOnlyList<KiWahleintrag> VergleichssichtWahl
        => SichtEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Stände der Gruppe — Stamm und Varianten.</summary>
    public IReadOnlyList<KiWahleintrag> ReferenzWahl
        => StandEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die für A wählbaren Stände — ohne den, der auf B steht.</summary>
    public IReadOnlyList<KiWahleintrag> StandAWahl
        => AEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die für B wählbaren Stände — ohne den, der auf A steht.</summary>
    public IReadOnlyList<KiWahleintrag> StandBWahl
        => BEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Stände, für die der Lauf Zahlungsreihen geliefert hat (Block 2).</summary>
    public IReadOnlyList<KiWahleintrag> ZahlungsreihenStandWahl
        => ZahlungsstandEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Szenarien der Zahlungsreihen (Block 2).</summary>
    public IReadOnlyList<KiWahleintrag> ZahlungsreihenSzenarioWahl
        => ZahlungsszenarioEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Seite
    // =====================================================================

    /// <summary>Das Szenario, unter dem die Kennzahlen gerechnet werden.</summary>
    public int? Szenario
    {
        get => SzenarioLesen?.Invoke();
        set => SzenarioSetzen?.Invoke(value);
    }

    /// <summary>Alle angehakten Stände gegen die Referenz — oder zwei Stände A und B.</summary>
    public int? Vergleichssicht
    {
        get => SichtLesen?.Invoke();
        set => SichtSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die Referenz der Gruppe — die Unterlassensalternative, gegen die gerechnet
    /// wird.
    /// </summary>
    public int? Referenz
    {
        get => ReferenzLesen?.Invoke();
        set => ReferenzSetzen?.Invoke(value);
    }

    /// <summary>Der Stand A der Paarsicht — ihre Referenz.</summary>
    public int? StandA
    {
        get => ALesen?.Invoke();
        set => ASetzen?.Invoke(value);
    }

    /// <summary>Der Stand B der Paarsicht.</summary>
    public int? StandB
    {
        get => BLesen?.Invoke();
        set => BSetzen?.Invoke(value);
    }

    // =====================================================================
    //  ETAPPE E17 (V‑G11): die nicht monetarisierbaren Wirkungen als Liste
    // =====================================================================

    /// <summary>
    /// Die nicht monetarisierbaren Wirkungen nach DIN EN 17463 (6.1, 8.2) — je Wirkung eine
    /// Zeile mit Kategorie, Beschreibung, Dauer, drei Wirkungsgraden und der Beurteilung als
    /// Anzeige. Geschrieben wird auf Zuruf („Speichern"), nicht bei jedem Setzen.
    /// </summary>
    public IReadOnlyList<WirkungKiZeile> Wirkungen
        => WirkungenLesen?.Invoke() ?? Array.Empty<WirkungKiZeile>();

    /// <summary>
    /// Die ZAHL der Wirkungen — der Weg, Zeilen anzulegen oder zu entfernen: Eine größere
    /// Zahl hängt leere Zeilen der Kategorie „sonstig" an, eine kleinere nimmt Zeilen vom
    /// Ende. Leere Zeilen schreibt der Speicherweg nicht.
    /// </summary>
    public int? Wirkungsanzahl
    {
        get => Wirkungen.Count;
        set => WirkungsanzahlSetzen?.Invoke(Math.Max(0, value ?? 0));
    }

    /// <summary>Die drei Kategorien (Id = Listenplatz).</summary>
    public IReadOnlyList<KiWahleintrag> KategorieWahl => Wahl(NichtMonetaereWirkungen.Kategorien());

    /// <summary>Die Dauerstufen 1 kurz, 2 mittel, 3 lang.</summary>
    public IReadOnlyList<KiWahleintrag> DauerWahl => Wahl(NichtMonetaereWirkungen.Dauerstufen());

    /// <summary>Die Wirkungsgrade 0 keine … 3 stark — Organisation.</summary>
    public IReadOnlyList<KiWahleintrag> OrganisationWahl => Wahl(NichtMonetaereWirkungen.Wirkungsgrade());

    /// <summary>Dieselben Wirkungsgrade — Mitarbeiter.</summary>
    public IReadOnlyList<KiWahleintrag> MitarbeiterWahl => Wahl(NichtMonetaereWirkungen.Wirkungsgrade());

    /// <summary>Dieselben Wirkungsgrade — Umwelt.</summary>
    public IReadOnlyList<KiWahleintrag> UmweltWahl => Wahl(NichtMonetaereWirkungen.Wirkungsgrade());

    private static IReadOnlyList<KiWahleintrag> Wahl(IReadOnlyList<KeyValuePair<int, string>> stufen)
        => KiMaskenanmeldung.Eintraege(stufen, s => s.Key, s => s.Value);

    /// <summary>
    /// Der Stand, dessen Zahlungsreihen Block 2 zeigt — eine Anzeigewahl; <c>null</c> =
    /// keine Jahresreihen in dieser Sitzung.
    /// </summary>
    public int? ZahlungsreihenStand
    {
        get => ZahlungsstandLesen?.Invoke();
        set => ZahlungsstandSetzen?.Invoke(value);
    }

    /// <summary>
    /// Das Szenario, dessen Zahlungsreihen Block 2 zeigt — eine Anzeigewahl, unabhängig
    /// vom Szenario der Kennzahlen.
    /// </summary>
    public int? ZahlungsreihenSzenario
    {
        get => ZahlungsszenarioLesen?.Invoke();
        set => ZahlungsszenarioSetzen?.Invoke(value);
    }

    // =====================================================================
    //  Der Abschnitt „Verlauf" (Welle #458, Stufe 2)
    // =====================================================================

    public Func<int?>? VerlaufZeitraumLesen { get; init; }
    public Action<int?>? VerlaufZeitraumSetzen { get; init; }

    /// <summary>Liefert die Haken des Verlaufs (je Stand, je Szenario); leer ohne Rechnung.</summary>
    public Func<IReadOnlyList<EPOS.UI.Seiten.Simulation.Anzeigeschalter>>? VerlaufschalterLesen { get; init; }

    /// <summary>
    /// Der Zeitraum des Kapitalwertverlaufs [Jahre] — derselbe Wert wie das Feld;
    /// gerechnet wird erst mit „Aktualisieren".
    /// </summary>
    public int? VerlaufZeitraum
    {
        get => VerlaufZeitraumLesen?.Invoke();
        set => VerlaufZeitraumSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die Haken des Verlaufs — eine SPALTE, je Stand und je Szenario eine Zeile mit
    /// seinem Namen als Kennzeichen. Ein Haken zeichnet nur neu.
    /// </summary>
    public IReadOnlyList<EPOS.UI.Seiten.Simulation.Anzeigeschalter> Verlaufsschalter
        => VerlaufschalterLesen?.Invoke() ?? Array.Empty<EPOS.UI.Seiten.Simulation.Anzeigeschalter>();
}

/// <summary>
/// ETAPPE E17 (V‑G11) — eine Zeile der Wirkungsliste für den Hilfe-Assistenten. Sie
/// schreibt in die Zeile des Arbeitsstands der Seite (<see cref="ProjektWirkung"/>) und
/// lässt die Seite danach neu zeichnen; geschrieben in die Datenbank wird erst mit
/// „Speichern". Die Kategorie läuft über ihren Listenplatz
/// (<see cref="NichtMonetaereWirkungen.KATEGORIEN"/>), Dauer und Wirkungsgrade über ihren Wert.
/// </summary>
public sealed class WirkungKiZeile
{
    private readonly ProjektWirkung _zeile;
    private readonly Action _geaendert;

    public WirkungKiZeile(ProjektWirkung zeile, int nummer, Action geaendert)
    {
        _zeile = zeile;
        Nummer = nummer;
        _geaendert = geaendert;
    }

    /// <summary>Die laufende Nummer in der Liste (1, 2, 3 …).</summary>
    public int Nummer { get; }

    /// <summary>Das Kennzeichen der Zeile für den Assistenten: „Wirkung 2: Komfort".</summary>
    public string Kennzeichen
        => string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.WIRT_NM_KENNZEICHEN,
                         Nummer, (_zeile.Beschreibung ?? "").Trim());

    /// <summary>Die Kategorie als Listenplatz (0 Energiefluss, 1 finanziell, 2 sonstig).</summary>
    public int? Kategorie
    {
        get
        {
            for (int i = 0; i < NichtMonetaereWirkungen.KATEGORIEN.Count; i++)
                if (string.Equals(NichtMonetaereWirkungen.KATEGORIEN[i], _zeile.Kategorie, StringComparison.Ordinal))
                    return i;
            return null;
        }
        set
        {
            if (value is int i && i >= 0 && i < NichtMonetaereWirkungen.KATEGORIEN.Count)
            {
                _zeile.Kategorie = NichtMonetaereWirkungen.KATEGORIEN[i];
                _geaendert();
            }
        }
    }

    /// <summary>Die Beschreibung der Wirkung.</summary>
    public string Beschreibung
    {
        get => _zeile.Beschreibung ?? "";
        set { _zeile.Beschreibung = value ?? ""; _geaendert(); }
    }

    /// <summary>Dauer 1 … 3; leer = nicht beurteilt.</summary>
    public int? Dauer
    {
        get => _zeile.Dauer;
        set { _zeile.Dauer = value; _geaendert(); }
    }

    /// <summary>Wirkung auf die Organisation 0 … 3; leer = nicht beurteilt.</summary>
    public int? Organisation
    {
        get => _zeile.WirkungOrganisation;
        set { _zeile.WirkungOrganisation = value; _geaendert(); }
    }

    /// <summary>Wirkung auf die Mitarbeiter 0 … 3; leer = nicht beurteilt.</summary>
    public int? Mitarbeiter
    {
        get => _zeile.WirkungMitarbeiter;
        set { _zeile.WirkungMitarbeiter = value; _geaendert(); }
    }

    /// <summary>Wirkung auf die Umwelt 0 … 3; leer = nicht beurteilt.</summary>
    public int? Umwelt
    {
        get => _zeile.WirkungUmwelt;
        set { _zeile.WirkungUmwelt = value; _geaendert(); }
    }

    /// <summary>Die Beurteilung nach 8.2 — Anzeige („6 von 9" bzw. „nicht beurteilt").</summary>
    public string Beurteilung => NichtMonetaereWirkungen.BeurteilungText(_zeile);
}
