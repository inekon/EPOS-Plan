using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild des Gebäude-Katalogeditors für den Hilfe-Assistenten (Welle KI‑F3).
///
/// <para><b>EIN Katalogeintrag, ZWEI Stände — und deshalb ein Sichtmodell.</b> Das erste
/// Reiterblatt (Kenngrößen, Flächen, U‑Werte) bindet unmittelbar an
/// <c>GebaeudeKatalogDaten</c>; das zweite (Raumtemperaturen, Wärmebrücken,
/// Anschlussmaße, Luftwechsel) führte bis Stufe G1 einen EIGENEN Arbeitsstand in den Feldern
/// der Maske; seit G1 greifen alle Wege auf den EINEN Arbeitsstand des Dialogs (E27/U1) —
/// die Zugriffswege bleiben, damit die Feldliste des Kerns (<c>KiDialoge</c>) unverändert gilt. Ein Katalog, der beide Blätter
/// an den Satz hängte, schriebe in Zahlen, die die Maske im selben Augenblick wieder
/// überschreibt. Diese Klasse legt sich über BEIDE Stände unter einem Namen — dieselbe
/// Bauart wie <c>KomponentenKonfigurationKiSicht</c>.</para>
///
/// <para><b>Die sechs Klapplisten sind WAHLFELDER</b> (KI‑D‑Q6): Gebäudetyp, Gebäudeart,
/// Baujahr, Verwendung und Bauart. Die VERWENDUNG trägt als Schlüssel ihren Steuerwert
/// und nicht den Anzeigetext; die BAUART zieht die Bauweise nach, genau wie die
/// Klappliste.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten bzw. liest den lebenden Satz.</para>
/// </summary>
public sealed class GebaeudeKatalogKiSicht
{
    // =====================================================================
    //  Der Satz des ERSTEN Reiterblatts
    // =====================================================================

    /// <summary>
    /// Liefert den Feldsatz, den die Maske gerade führt (den ARBEITSSTAND,
    /// <c>GebaeudeArbeitsstand.Stand</c>); darf <c>null</c> liefern.
    /// </summary>
    public Func<GebaeudeKatalogDaten?>? StandLesen { get; init; }

    private GebaeudeKatalogDaten? Daten => StandLesen?.Invoke();

    // =====================================================================
    //  Die Satzwahl der GEBÄUDEVERWALTUNG (Welle #465)
    // =====================================================================

    /// <summary>Der gewählte Satz der Verwaltung (Bezeichner der Fokuszeile).</summary>
    public Func<string>? SatzLesen { get; init; }

    /// <summary>
    /// Wählt einen Satz der Verwaltung; Rückgabe: der Grund, warum der Dialog den Wechsel
    /// ablehnt, sonst <c>null</c>.
    /// </summary>
    public Func<string, string?>? SatzSetzen { get; init; }

    /// <summary>Die Sätze der Liste als Einträge des Wahlfeldes „satz".</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SatzEintraege { get; init; }

    /// <summary>
    /// Setzt die Nutzfläche über den Weg des Eingabefeldes — die Bauweise folgt ihr beim
    /// Schreiben; ohne Delegat geht der Wert unmittelbar in den Satz.
    /// </summary>
    public Action<double?>? WohnflaecheSetzen { get; init; }

    // =====================================================================
    //  Die Zugriffswege des ZWEITEN Reiterblatts und des Namens
    // =====================================================================

    public Func<string>? NameLesen { get; init; }
    public Action<string>? NameSetzen { get; init; }

    public Action<int?>? BauartSetzen { get; init; }

    public Func<double?>? SollTagLesen { get; init; }
    public Action<double?>? SollTagSetzen { get; init; }

    public Func<double?>? NachtabsenkungLesen { get; init; }
    public Action<double?>? NachtabsenkungSetzen { get; init; }

    public Func<int?>? NachtBeginnLesen { get; init; }
    public Action<int?>? NachtBeginnSetzen { get; init; }

    public Func<int?>? NachtEndeLesen { get; init; }
    public Action<int?>? NachtEndeSetzen { get; init; }

    public Func<double?>? MaxTemperaturLesen { get; init; }
    public Action<double?>? MaxTemperaturSetzen { get; init; }

    public Func<double?>? WochenendabsenkungLesen { get; init; }
    public Action<double?>? WochenendabsenkungSetzen { get; init; }

    public Func<double?>? SollFerienLesen { get; init; }
    public Action<double?>? SollFerienSetzen { get; init; }

    public Func<double?>? WbvkFensterWandLesen { get; init; }
    public Action<double?>? WbvkFensterWandSetzen { get; init; }

    public Func<double?>? WbvkWandDachLesen { get; init; }
    public Action<double?>? WbvkWandDachSetzen { get; init; }

    public Func<double?>? WbvkAussenwandKellerLesen { get; init; }
    public Action<double?>? WbvkAussenwandKellerSetzen { get; init; }

    public Func<double?>? AnschlussFensterWandLesen { get; init; }
    public Action<double?>? AnschlussFensterWandSetzen { get; init; }

    public Func<double?>? AnschlussWandDachLesen { get; init; }
    public Action<double?>? AnschlussWandDachSetzen { get; init; }

    public Func<double?>? AnschlussAussenwandKellerLesen { get; init; }
    public Action<double?>? AnschlussAussenwandKellerSetzen { get; init; }

    public Func<double?>? LuftwechselrateLesen { get; init; }
    public Action<double?>? LuftwechselrateSetzen { get; init; }

    public Func<string>? BetriebsartLesen { get; init; }

    // ---- Welle #458 Stufe 3b: Randbedingung der Bodenplatte und die Ferien --------

    /// <summary>Liest die Randbedingung der Bodenplatte als Listenplatz (0 Erdreich, 1 Keller, 2 Außenluft).</summary>
    public Func<int?>? RandbedingungLesen { get; init; }

    /// <summary>Wählt die Randbedingung — derselbe Weg wie die Klappliste im Hüll-Raster.</summary>
    public Action<int?>? RandbedingungSetzen { get; init; }

    /// <summary>Liefert die drei Randbedingungen der Klappliste.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? RandbedingungEintraege { get; init; }

    /// <summary>Liefert die vier Ferienzeiträume als Zeilen.</summary>
    public Func<IReadOnlyList<GebaeudeFerienKiZeile>>? FerienLesen { get; init; }

    // =====================================================================
    //  Die Einträge der fünf Wahlfelder (KI-D-Q6)
    // =====================================================================

    public Func<IReadOnlyList<KiWahleintrag>>? TypEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? GebaeudeartEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? BaualtersklasseEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? VerwendungEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? BauartEintraege { get; init; }

    /// <summary>Die Gebäudetypen des Katalogs.</summary>
    public IReadOnlyList<KiWahleintrag> TypWahl
        => TypEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Gebäudearten des Katalogs.</summary>
    public IReadOnlyList<KiWahleintrag> GebaeudeartWahl
        => GebaeudeartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Baualtersklassen A…U; der Schlüssel ist ihr Listenplatz.</summary>
    public IReadOnlyList<KiWahleintrag> BaualtersklasseWahl
        => BaualtersklasseEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Wohngebäude oder Gewerbe; der Schlüssel ist der Steuerwert.</summary>
    public IReadOnlyList<KiWahleintrag> VerwendungWahl
        => VerwendungEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Leichte, schwere oder sehr schwere Bauart.</summary>
    public IReadOnlyList<KiWahleintrag> BauartWahl
        => BauartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Erdreich, Keller oder Außenluft; der Schlüssel ist der Listenplatz.</summary>
    public IReadOnlyList<KiWahleintrag> RandbedingungWahl
        => RandbedingungEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Kenngrößen — das erste Reiterblatt
    // =====================================================================

    /// <summary>
    /// Der Bezeichner des Satzes. In der KATALOGVERWALTUNG wählt er den Satz aus,
    /// den die Maske lädt — denselben Weg nimmt dort die Klappliste; in den beiden
    /// anderen Betriebsarten benennt er den Satz, der geschrieben wird.
    /// </summary>
    public string Name
    {
        get => NameLesen?.Invoke() ?? "";
        set => NameSetzen?.Invoke(value ?? "");
    }

    /// <summary>
    /// <b>Der gewählte Satz der GEBÄUDEVERWALTUNG</b> (Wahlfeld <c>satz</c>, #465). Ihn zu
    /// setzen wählt die Zeile der Liste — derselbe Weg wie ein Klick; ein abgelehnter
    /// Wechsel (ungespeicherte Änderungen) kommt als benannte Ausnahme zurück, damit der
    /// Assistent nicht „gesetzt" meldet, wo nichts geschah. Der Katalogeditor führt das
    /// Feld nicht.
    /// </summary>
    public string Satz
    {
        get => SatzLesen?.Invoke() ?? "";
        set
        {
            string? grund = SatzSetzen?.Invoke(value ?? "");
            if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
        }
    }

    /// <summary>Die Sätze der Liste (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> SatzWahl
        => SatzEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Der Gebäudetyp aus dem Typkatalog; er bringt die Tagesverteilungen mit.</summary>
    public string Typ
    {
        get => Daten?.Typ ?? "";
        set { if (Daten is GebaeudeKatalogDaten d) d.Typ = value ?? ""; }
    }

    /// <summary>Der Freitext des Satzes.</summary>
    public string Beschreibung
    {
        get => Daten?.Beschreibung ?? "";
        set { if (Daten is GebaeudeKatalogDaten d) d.Beschreibung = value ?? ""; }
    }

    /// <summary>Die Gebäudeart (Einfamilienhaus, Bürogebäude …).</summary>
    public string Gebaeudeart
    {
        get => Daten?.Gebaeudeart ?? "";
        set { if (Daten is GebaeudeKatalogDaten d) d.Gebaeudeart = value ?? ""; }
    }

    /// <summary>Die Baualtersklasse als Platz in der Klappliste.</summary>
    public int Baualtersklasse
    {
        get => Daten?.Baualtersklasse ?? 0;
        set { if (Daten is GebaeudeKatalogDaten d) d.Baualtersklasse = value; }
    }

    /// <summary>Das Baujahr als Jahreszahl (1500 … 2100); leer = unbekannt.</summary>
    public int? Baujahr
    {
        get => Daten?.Baujahr;
        set { if (Daten is GebaeudeKatalogDaten d) d.Baujahr = value; }
    }

    /// <summary>
    /// Die Verwendung als STEUERWERT (nicht als Anzeigetext): Sie entscheidet, aus
    /// welcher Liste die Gebäudearten kommen.
    /// </summary>
    public string Verwendung
    {
        get => Daten?.Verwendung ?? "";
        set { if (Daten is GebaeudeKatalogDaten d) d.Verwendung = value ?? ""; }
    }

    /// <summary>
    /// Die Bauart als Platz in der Klappliste. Ein Setzen zieht die BAUWEISE nach —
    /// derselbe Weg, den die Klappliste geht.
    /// </summary>
    public int Bauart
    {
        get => Daten?.Bauart ?? 0;
        set => BauartSetzen?.Invoke(value);
    }

    /// <summary>Die gesamte Wohn- oder Nutzfläche [m²].</summary>
    public double? WohnflaecheGesamt
    {
        get => Daten?.WohnflaecheGesamt;
        set
        {
            if (WohnflaecheSetzen is not null) WohnflaecheSetzen(value);
            else if (Daten is GebaeudeKatalogDaten d) d.WohnflaecheGesamt = value;
        }
    }

    /// <summary>Die Fläche je Nutzer [m²].</summary>
    public double? FlaecheNutzer
    {
        get => Daten?.FlaecheNutzer;
        set { if (Daten is GebaeudeKatalogDaten d) d.FlaecheNutzer = value; }
    }

    /// <summary>Die inneren Wärmegewinne [W].</summary>
    public double? Waermegewinne
    {
        get => Daten?.Waermegewinne;
        set { if (Daten is GebaeudeKatalogDaten d) d.Waermegewinne = value; }
    }

    /// <summary>Der Gesamtenergiedurchlassgrad der Fenster als Anteil (z. B. 0,4).</summary>
    public double? Fensterdurchlassgrad
    {
        get => Daten?.Fensterdurchlassgrad;
        set { if (Daten is GebaeudeKatalogDaten d) d.Fensterdurchlassgrad = value; }
    }

    /// <summary>Die mittlere Raumhöhe [m].</summary>
    public double? Raumhoehe
    {
        get => Daten?.Raumhoehe;
        set { if (Daten is GebaeudeKatalogDaten d) d.Raumhoehe = value; }
    }

    // =====================================================================
    //  Flächen [m²]
    // =====================================================================

    /// <summary>Die Fensterfläche nach Norden [m²].</summary>
    public double? FensterflaecheNord
    {
        get => Daten?.FensterflaecheNord;
        set { if (Daten is GebaeudeKatalogDaten d) d.FensterflaecheNord = value; }
    }

    /// <summary>Die Fensterfläche nach Süden [m²].</summary>
    public double? FensterflaecheSued
    {
        get => Daten?.FensterflaecheSued;
        set { if (Daten is GebaeudeKatalogDaten d) d.FensterflaecheSued = value; }
    }

    /// <summary>Die Fensterfläche nach Osten und Westen zusammen [m²].</summary>
    public double? FensterflaecheOstWest
    {
        get => Daten?.FensterflaecheOstWest;
        set { if (Daten is GebaeudeKatalogDaten d) d.FensterflaecheOstWest = value; }
    }

    /// <summary>Die Außenwandfläche ohne Fenster [m²].</summary>
    public double? FlaecheAussenwand
    {
        get => Daten?.FlaecheAussenwand;
        set { if (Daten is GebaeudeKatalogDaten d) d.FlaecheAussenwand = value; }
    }

    /// <summary>Die Dachfläche [m²].</summary>
    public double? Dachflaeche
    {
        get => Daten?.Dachflaeche;
        set { if (Daten is GebaeudeKatalogDaten d) d.Dachflaeche = value; }
    }

    /// <summary>Die Grundfläche gegen Erdreich oder Keller [m²].</summary>
    public double? Grundflaeche
    {
        get => Daten?.Grundflaeche;
        set { if (Daten is GebaeudeKatalogDaten d) d.Grundflaeche = value; }
    }

    /// <summary>Die übrigen wärmeübertragenden Flächen [m²].</summary>
    public double? SonstigeFlaechen
    {
        get => Daten?.SonstigeFlaechen;
        set { if (Daten is GebaeudeKatalogDaten d) d.SonstigeFlaechen = value; }
    }

    // =====================================================================
    //  U-Werte [W/(m²·K)]
    // =====================================================================

    /// <summary>Der U-Wert der Außenwand.</summary>
    public double? UWertAussenwand
    {
        get => Daten?.UWertAussenwand;
        set { if (Daten is GebaeudeKatalogDaten d) d.UWertAussenwand = value; }
    }

    /// <summary>Der U-Wert der Fenster.</summary>
    public double? UWertFenster
    {
        get => Daten?.UWertFenster;
        set { if (Daten is GebaeudeKatalogDaten d) d.UWertFenster = value; }
    }

    /// <summary>Der U-Wert der Dachfläche.</summary>
    public double? UWertDachflaeche
    {
        get => Daten?.UWertDachflaeche;
        set { if (Daten is GebaeudeKatalogDaten d) d.UWertDachflaeche = value; }
    }

    /// <summary>Der U-Wert der Grundfläche.</summary>
    public double? UWertGrundflaeche
    {
        get => Daten?.UWertGrundflaeche;
        set { if (Daten is GebaeudeKatalogDaten d) d.UWertGrundflaeche = value; }
    }

    /// <summary>Der U-Wert der übrigen Flächen.</summary>
    public double? UWertSonstiges
    {
        get => Daten?.UWertSonstiges;
        set { if (Daten is GebaeudeKatalogDaten d) d.UWertSonstiges = value; }
    }

    // =====================================================================
    //  Raumtemperaturen — das ZWEITE Reiterblatt
    // =====================================================================

    /// <summary>Die Solltemperatur am Tag [°C].</summary>
    public double? SollTag
    {
        get => SollTagLesen?.Invoke();
        set => SollTagSetzen?.Invoke(value);
    }

    /// <summary>Die Nachtabsenkung [°C].</summary>
    public double? Nachtabsenkung
    {
        get => NachtabsenkungLesen?.Invoke();
        set => NachtabsenkungSetzen?.Invoke(value);
    }

    /// <summary>Beginn der Nachtabsenkung [Stunde 0 … 23]; mit dem Ende leer = Vorgabe 22 Uhr (E43).</summary>
    public int? NachtBeginn
    {
        get => NachtBeginnLesen?.Invoke();
        set => NachtBeginnSetzen?.Invoke(value);
    }

    /// <summary>Ende der Nachtabsenkung [Stunde 0 … 23]; mit dem Beginn leer = Vorgabe 6 Uhr (E43).</summary>
    public int? NachtEnde
    {
        get => NachtEndeLesen?.Invoke();
        set => NachtEndeSetzen?.Invoke(value);
    }

    /// <summary>Die höchste zulässige Raumtemperatur [°C]; unter 1 gilt 24.</summary>
    public double? MaxTemperatur
    {
        get => MaxTemperaturLesen?.Invoke();
        set => MaxTemperaturSetzen?.Invoke(value);
    }

    /// <summary>Die Absenkung am Wochenende [°C]; über 0 schaltet sie den Betrieb ein.</summary>
    public double? Wochenendabsenkung
    {
        get => WochenendabsenkungLesen?.Invoke();
        set => WochenendabsenkungSetzen?.Invoke(value);
    }

    /// <summary>Die Solltemperatur in den Ferien [°C]; über 0 schaltet sie den Betrieb ein.</summary>
    public double? SollFerien
    {
        get => SollFerienLesen?.Invoke();
        set => SollFerienSetzen?.Invoke(value);
    }

    // =====================================================================
    //  Wärmebrücken [W/(m·K)] und Anschlussmaße [m]
    // =====================================================================

    /// <summary>Der Wärmebrückenverlustkoeffizient Fenster–Wand.</summary>
    public double? WbvkFensterWand
    {
        get => WbvkFensterWandLesen?.Invoke();
        set => WbvkFensterWandSetzen?.Invoke(value);
    }

    /// <summary>Der Wärmebrückenverlustkoeffizient Wand–Dach.</summary>
    public double? WbvkWandDach
    {
        get => WbvkWandDachLesen?.Invoke();
        set => WbvkWandDachSetzen?.Invoke(value);
    }

    /// <summary>Der Wärmebrückenverlustkoeffizient Außenwand–Keller.</summary>
    public double? WbvkAussenwandKeller
    {
        get => WbvkAussenwandKellerLesen?.Invoke();
        set => WbvkAussenwandKellerSetzen?.Invoke(value);
    }

    /// <summary>Die Anschlusslänge Fenster–Wand [m].</summary>
    public double? AnschlussFensterWand
    {
        get => AnschlussFensterWandLesen?.Invoke();
        set => AnschlussFensterWandSetzen?.Invoke(value);
    }

    /// <summary>Die Anschlusslänge Wand–Dach [m].</summary>
    public double? AnschlussWandDach
    {
        get => AnschlussWandDachLesen?.Invoke();
        set => AnschlussWandDachSetzen?.Invoke(value);
    }

    /// <summary>Die Anschlusslänge Außenwand–Keller [m].</summary>
    public double? AnschlussAussenwandKeller
    {
        get => AnschlussAussenwandKellerLesen?.Invoke();
        set => AnschlussAussenwandKellerSetzen?.Invoke(value);
    }

    /// <summary>Die Luftwechselrate [1/h].</summary>
    public double? Luftwechselrate
    {
        get => LuftwechselrateLesen?.Invoke();
        set => LuftwechselrateSetzen?.Invoke(value);
    }

    // =====================================================================
    //  Modellparameter VDI 6007 (Stufen G1 und G2) — am Satz, wie die Kenngrößen
    // =====================================================================

    /// <summary>Anteil des Fensterrahmens an der Fensterfläche (VDI 6007); leer = Vorgabe 0,3.</summary>
    public double? Rahmenanteil
    {
        get => Daten?.Rahmenanteil;
        set { if (Daten is GebaeudeKatalogDaten d) d.Rahmenanteil = value; }
    }

    /// <summary>Pauschaler Verschattungsfaktor der Fenster (VDI 6007); leer = Vorgabe 0,9.</summary>
    public double? Verschattungsfaktor
    {
        get => Daten?.Verschattungsfaktor;
        set { if (Daten is GebaeudeKatalogDaten d) d.Verschattungsfaktor = value; }
    }

    /// <summary>Anteil der Speichermasse in den Außenbauteilen (VDI 6007); leer = Vorgabe 0,3.</summary>
    public double? MasseanteilAussen
    {
        get => Daten?.MasseanteilAussen;
        set { if (Daten is GebaeudeKatalogDaten d) d.MasseanteilAussen = value; }
    }

    /// <summary>Innenbauteilfläche je m² Nutzfläche (VDI 6007); leer = Vorgabe 2,5.</summary>
    public double? Innenflaechenfaktor
    {
        get => Daten?.Innenflaechenfaktor;
        set { if (Daten is GebaeudeKatalogDaten d) d.Innenflaechenfaktor = value; }
    }

    /// <summary>Strahlungsanteil der Wärmeübergabe (VDI 6007); leer = Vorgabe 0,3.</summary>
    public double? HeizungStrahlungsanteil
    {
        get => Daten?.HeizungStrahlungsanteil;
        set { if (Daten is GebaeudeKatalogDaten d) d.HeizungStrahlungsanteil = value; }
    }

    /// <summary>Größte Heizleistung des Stundenmodells in kW; leer = unbegrenzt.</summary>
    public double? HeizleistungMax
    {
        get => Daten?.HeizleistungMax;
        set { if (Daten is GebaeudeKatalogDaten d) d.HeizleistungMax = value; }
    }

    /// <summary>Rechnet Sonneneinstrahlung und langwellige Abstrahlung auf die opaken Außenbauteile ein (VDI 6007).</summary>
    public bool AussenbauteileStrahlung
    {
        get => Daten?.AussenbauteileStrahlung ?? false;
        set { if (Daten is GebaeudeKatalogDaten d) d.AussenbauteileStrahlung = value; }
    }

    /// <summary>Luftwechsel durch Undichtheiten (VDI 6007); sind Infiltration und Nutzerlüftung leer, gilt die Luftwechselrate.</summary>
    public double? LuftwechselInfiltration
    {
        get => Daten?.LuftwechselInfiltration;
        set { if (Daten is GebaeudeKatalogDaten d) d.LuftwechselInfiltration = value; }
    }

    /// <summary>Luftwechsel durch Fensterlüftung der Nutzer (VDI 6007); zusammen mit der Infiltration der Luftwechsel des Stundenmodells.</summary>
    public double? LuftwechselNutzer
    {
        get => Daten?.LuftwechselNutzer;
        set { if (Daten is GebaeudeKatalogDaten d) d.LuftwechselNutzer = value; }
    }

    /// <summary>Erhöhter Luftwechsel an warmen Tagen, wenn die Außenluft kühler ist (VDI 6007).</summary>
    public bool Sommerlueftung
    {
        get => Daten?.Sommerlueftung ?? false;
        set { if (Daten is GebaeudeKatalogDaten d) d.Sommerlueftung = value; }
    }

    /// <summary>Fensterfläche nach Osten in m²; leer = die Hälfte von Ost + West.</summary>
    public double? FensterflaecheOst
    {
        get => Daten?.FensterflaecheOst;
        set { if (Daten is GebaeudeKatalogDaten d) d.FensterflaecheOst = value; }
    }

    /// <summary>Fensterfläche nach Westen in m²; leer = die Hälfte von Ost + West.</summary>
    public double? FensterflaecheWest
    {
        get => Daten?.FensterflaecheWest;
        set { if (Daten is GebaeudeKatalogDaten d) d.FensterflaecheWest = value; }
    }

    /// <summary>Temperatur des unbeheizten Kellers unter der Bodenplatte in °C; leer = Vorgabe 10 °C.</summary>
    public double? Kellertemperatur
    {
        get => Daten?.Kellertemperatur;
        set { if (Daten is GebaeudeKatalogDaten d) d.Kellertemperatur = value; }
    }

    /// <summary>
    /// Wird das Gebäude gekühlt? (Stufe KU1) Wirkt nur mit Kühlsollwert und in einem Projekt mit
    /// der Projekteinstellung „Kühlung rechnen".
    /// </summary>
    public bool KuehlungAktiv
    {
        get => Daten?.KuehlungAktiv ?? false;
        set { if (Daten is GebaeudeKatalogDaten d) d.KuehlungAktiv = value; }
    }

    /// <summary>Kühlsollwert in °C; leer = Kühlung aus. Mindestens 1 K über dem höchsten Heizsollwert.</summary>
    public double? KuehlSollwert
    {
        get => Daten?.KuehlSollwert;
        set { if (Daten is GebaeudeKatalogDaten d) d.KuehlSollwert = value; }
    }

    /// <summary>Größte Kühlleistung in kW; leer = unbegrenzt.</summary>
    public double? KuehlleistungMax
    {
        get => Daten?.KuehlleistungMax;
        set { if (Daten is GebaeudeKatalogDaten d) d.KuehlleistungMax = value; }
    }

    // =====================================================================
    //  Stufe AK1: die Gruppe „Wärmeübergabe" (Anlagenkopplung 9.1, 9.2)
    // =====================================================================
    //
    // Schalter, Art, Schnellwahl des Bandes und Zeitprogramm gehen über die WEGE DES
    // ARBEITSSTANDS (Vorschlag der Heizkurve, „ideal" hält NULL, strenger Leser); die Zahlen
    // unmittelbar in den Satz - leer schreibt NULL, wie das Feld.

    /// <summary>Der Weg des Hakens „Übergabe rechnen" (schlägt beim ersten Einschalten die Heizkurve vor).</summary>
    public Action<bool>? HeizkreisSetzen { get; init; }

    /// <summary>Der Weg des Hakens „Heizkurve fahren" (die Wahl gilt danach als die des Anwenders, kein Vorschlag).</summary>
    public Action<bool>? HeizkurveSetzen { get; init; }

    /// <summary>Wählt die Übergabeart über ihren Steuerwert; Rückgabe: der Grund einer Ablehnung, sonst <c>null</c>.</summary>
    public Func<string, string?>? UebergabeArtSetzen { get; init; }

    /// <summary>Setzt das Zeitprogramm aus Text (168 Werte, „;"); Rückgabe: der Grund einer Ablehnung, sonst <c>null</c>.</summary>
    public Func<string, string?>? SollwertprofilSetzen { get; init; }

    /// <summary>Die Übergabearten als Einträge des Wahlfeldes (Schlüssel = Steuerwert).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? UebergabeArtEintraege { get; init; }

    /// <summary>
    /// „Übergabe rechnen" (Stufe AK1) — wirkt nur mit einer Übergabeart und in einem Projekt mit
    /// der Projekteinstellung Anlagenkopplung „Heizkreis (AK1)".
    /// </summary>
    public bool HeizkreisAktiv
    {
        get => Daten?.HeizkreisAktiv ?? false;
        set
        {
            if (HeizkreisSetzen is not null) HeizkreisSetzen(value);
            else if (Daten is GebaeudeKatalogDaten d) d.HeizkreisAktiv = value;
        }
    }

    /// <summary>Die Übergabeart als Steuerwert: IDEAL, RADIATOR, FLAECHE oder KONVEKTOR.</summary>
    public string UebergabeArt
    {
        get => string.IsNullOrEmpty(Daten?.UebergabeArt) ? WindowsFormsApplication1.DbWerte.UEBERGABE_IDEAL : Daten!.UebergabeArt!;
        set
        {
            string? grund = UebergabeArtSetzen?.Invoke(value ?? "");
            if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
        }
    }

    /// <summary>Die vier Übergabearten.</summary>
    public IReadOnlyList<KiWahleintrag> UebergabeArtWahl
        => UebergabeArtEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Exponent der Übergabe; leer = Vorgabe der Art.</summary>
    public double? UebergabeExponent
    {
        get => Daten?.UebergabeExponent;
        set { if (Daten is GebaeudeKatalogDaten d) d.UebergabeExponent = value; }
    }

    /// <summary>Nennleistung der Übergabe in kW; leer = hergeleitet aus der stationären Heizlast.</summary>
    public double? UebergabeNennleistung
    {
        get => Daten?.UebergabeLeistungNennKw;
        set { if (Daten is GebaeudeKatalogDaten d) d.UebergabeLeistungNennKw = value; }
    }

    /// <summary>Auslegungsvorlauf in °C; leer = Vorgabe der Art.</summary>
    public double? AuslegungVorlauf
    {
        get => Daten?.AuslegungVorlauf;
        set { if (Daten is GebaeudeKatalogDaten d) d.AuslegungVorlauf = value; }
    }

    /// <summary>Auslegungsrücklauf in °C; leer = Vorgabe der Art.</summary>
    public double? AuslegungRuecklauf
    {
        get => Daten?.AuslegungRuecklauf;
        set { if (Daten is GebaeudeKatalogDaten d) d.AuslegungRuecklauf = value; }
    }

    /// <summary>Raumtemperatur im Auslegungspunkt in °C; leer = das Soll am Tag.</summary>
    public double? AuslegungRaumtemperatur
    {
        get => Daten?.AuslegungRaumtemperatur;
        set { if (Daten is GebaeudeKatalogDaten d) d.AuslegungRaumtemperatur = value; }
    }

    /// <summary>Auslegungs-Außentemperatur in °C; leer = kältestes Tagesmittel der Klimareihe.</summary>
    public double? AuslegungAussentemperatur
    {
        get => Daten?.AuslegungAussentemperatur;
        set { if (Daten is GebaeudeKatalogDaten d) d.AuslegungAussentemperatur = value; }
    }

    /// <summary>„Heizkurve fahren" — sonst fester Vorlauf.</summary>
    public bool HeizkurveAktiv
    {
        get => Daten?.HeizkurveAktiv ?? false;
        set
        {
            if (HeizkurveSetzen is not null) HeizkurveSetzen(value);
            else if (Daten is GebaeudeKatalogDaten d) d.HeizkurveAktiv = value;
        }
    }

    /// <summary>Niveau der Heizkurve in K; leer = 0.</summary>
    public double? HeizkurveNiveau
    {
        get => Daten?.HeizkurveNiveau;
        set { if (Daten is GebaeudeKatalogDaten d) d.HeizkurveNiveau = value; }
    }

    /// <summary>Steilheit der Heizkurve; leer = 1,0.</summary>
    public double? HeizkurveSteilheit
    {
        get => Daten?.HeizkurveSteilheit;
        set { if (Daten is GebaeudeKatalogDaten d) d.HeizkurveSteilheit = value; }
    }

    /// <summary>Proportionalband des Raumreglers in K (0 bis 5); leer = 1 K.</summary>
    public double? Proportionalband
    {
        get => Daten?.ReglerProportionalband;
        set { if (Daten is GebaeudeKatalogDaten d) d.ReglerProportionalband = value; }
    }

    /// <summary>
    /// Das Sollwert-Zeitprogramm: 168 Werte in °C, Montag 00 Uhr bis Sonntag 23 Uhr, getrennt
    /// durch „;", Dezimalpunkt; leer = die Bestandssollwerte. Streng gelesen — eine falsche
    /// Wertzahl wird benannt abgelehnt, nicht aufgefüllt.
    /// </summary>
    public string Sollwertprofil
    {
        get => Daten?.Sollwertprofil ?? "";
        set
        {
            string? grund = SollwertprofilSetzen?.Invoke(value ?? "");
            if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
        }
    }

    // =====================================================================
    //  E37: der Unterabschnitt „Kühlübergabe" der Gruppe „Kühlung"
    // =====================================================================
    //
    // Schalter und Art gehen über die WEGE DES ARBEITSSTANDS („ideal" hält NULL, die Art bleibt
    // beim Abschalten); die Zahlen unmittelbar in den Satz - leer schreibt NULL, wie das Feld.

    /// <summary>Der Weg des Hakens „Kühlübergabe rechnen".</summary>
    public Action<bool>? KuehluebergabeSetzen { get; init; }

    /// <summary>Wählt die Kühlübergabeart über ihren Steuerwert; Rückgabe: der Grund einer Ablehnung, sonst <c>null</c>.</summary>
    public Func<string, string?>? KuehlUebergabeArtSetzen { get; init; }

    /// <summary>Die Kühlübergabearten als Einträge des Wahlfeldes (Schlüssel = Steuerwert).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? KuehlUebergabeArtEintraege { get; init; }

    /// <summary>
    /// „Kühlübergabe rechnen" (E37) — wirkt nur mit Kühlung, Kühlsollwert, einer Kühlübergabeart
    /// und in einem Projekt mit Anlagenkopplung „Heizkreis (AK1)" und Kühlbetrieb.
    /// </summary>
    public bool KuehluebergabeAktiv
    {
        get => Daten?.KuehluebergabeAktiv ?? false;
        set
        {
            if (KuehluebergabeSetzen is not null) KuehluebergabeSetzen(value);
            else if (Daten is GebaeudeKatalogDaten d) d.KuehluebergabeAktiv = value;
        }
    }

    /// <summary>Die Kühlübergabeart als Steuerwert: IDEAL, KUEHLDECKE, FLAECHENKUEHLUNG oder GEBLAESEKONVEKTOR.</summary>
    public string KuehlUebergabeArt
    {
        get => string.IsNullOrEmpty(Daten?.KuehlUebergabeArt) ? WindowsFormsApplication1.DbWerte.KUEHLUEBERGABE_IDEAL : Daten!.KuehlUebergabeArt!;
        set
        {
            string? grund = KuehlUebergabeArtSetzen?.Invoke(value ?? "");
            if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
        }
    }

    /// <summary>Die vier Kühlübergabearten.</summary>
    public IReadOnlyList<KiWahleintrag> KuehlUebergabeArtWahl
        => KuehlUebergabeArtEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Exponent der Kühlübergabe; leer = Vorgabe der Art.</summary>
    public double? KuehlUebergabeExponent
    {
        get => Daten?.KuehlUebergabeExponent;
        set { if (Daten is GebaeudeKatalogDaten d) d.KuehlUebergabeExponent = value; }
    }

    /// <summary>Nennleistung der Kühlübergabe in kW, sensibel; leer = die Kühllast des Auslegungstags.</summary>
    public double? KuehlUebergabeNennleistung
    {
        get => Daten?.KuehlUebergabeLeistungNennKw;
        set { if (Daten is GebaeudeKatalogDaten d) d.KuehlUebergabeLeistungNennKw = value; }
    }

    /// <summary>Auslegungsvorlauf der Kühlübergabe in °C; leer = Vorgabe der Art.</summary>
    public double? KuehlAuslegungVorlauf
    {
        get => Daten?.KuehlAuslegungVorlauf;
        set { if (Daten is GebaeudeKatalogDaten d) d.KuehlAuslegungVorlauf = value; }
    }

    /// <summary>Auslegungsrücklauf der Kühlübergabe in °C; leer = Vorgabe der Art.</summary>
    public double? KuehlAuslegungRuecklauf
    {
        get => Daten?.KuehlAuslegungRuecklauf;
        set { if (Daten is GebaeudeKatalogDaten d) d.KuehlAuslegungRuecklauf = value; }
    }

    /// <summary>Raumtemperatur im Auslegungspunkt der Kühlübergabe in °C; leer = der Kühlsollwert.</summary>
    public double? KuehlAuslegungRaumtemperatur
    {
        get => Daten?.KuehlAuslegungRaumtemperatur;
        set { if (Daten is GebaeudeKatalogDaten d) d.KuehlAuslegungRaumtemperatur = value; }
    }

    /// <summary>Untere Grenze des Kaltwasser-Vorlaufs in °C — eine Vorgabe, keine Taupunktrechnung; leer = Vorgabe der Art.</summary>
    public double? KuehlVorlaufgrenze
    {
        get => Daten?.KuehlVorlaufgrenze;
        set { if (Daten is GebaeudeKatalogDaten d) d.KuehlVorlaufgrenze = value; }
    }

    /// <summary>Der Rechenweg, auf dem das Gebäude rechnet — nur lesend (VDI 6007 oder Tagesbilanz).</summary>
    public string Rechenweg => WindowsFormsApplication1.Gebaeuderechenweg.Wirksam(Daten?.Modell);

    /// <summary>
    /// Die Betriebsart der Maske — Bearbeiten, Neu oder Katalogverwaltung. Sie
    /// entscheidet, welcher der beiden Speicherwege frei ist.
    /// </summary>
    public string Betriebsart => BetriebsartLesen?.Invoke() ?? "";

    // =====================================================================
    //  Welle #458 Stufe 3b: das Hüll-Raster und die Ferien
    // =====================================================================

    /// <summary>
    /// Die Randbedingung der Bodenplatte im Hüll-Raster — der Listenplatz der Klappliste
    /// (0 Erdreich, 1 Keller, 2 Außenluft). Kennwert und Größe jeder Rasterzeile sind die
    /// Felder der U-Werte, Flächen, Wärmebrücken und Anschlussmaße dieser Sicht.
    /// </summary>
    public int? Randbedingung
    {
        get => RandbedingungLesen?.Invoke();
        set => RandbedingungSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die vier Ferienzeiträume als TABELLE — Zeilen mit dem Zeitraum als Kennzeichen,
    /// Spalten Beginn und Ende je Tag und Monat. Die Zellen sind dieselben Felder, an
    /// denen die Eingaben des Reiters „Raumtemperaturen" hängen.
    /// </summary>
    public IReadOnlyList<GebaeudeFerienKiZeile> Ferien
        => FerienLesen?.Invoke() ?? Array.Empty<GebaeudeFerienKiZeile>();
}

/// <summary>
/// EIN Ferienzeitraum des Gebäude-Katalogeditors für den Hilfe-Assistenten (Welle #458
/// Stufe 3b) — eine Zeile der Tabelle <see cref="GebaeudeKatalogKiSicht.Ferien"/>.
/// </summary>
/// <remarks>
/// <b>Sie hält keinen Zustand:</b> Jede Zelle liest und schreibt über ihren Delegaten die
/// Felder der Maske (Tag und Monat je Beginn und Ende); zu Jahrestagen werden sie erst im
/// OK-Weg — derselbe Weg wie bei der Eingabe von Hand.
/// </remarks>
public sealed class GebaeudeFerienKiZeile
{
    public Func<string>? ZeitraumLesen { get; init; }

    public Func<int?>? BeginnTagLesen { get; init; }
    public Action<int?>? BeginnTagSetzen { get; init; }
    public Func<int?>? BeginnMonatLesen { get; init; }
    public Action<int?>? BeginnMonatSetzen { get; init; }
    public Func<int?>? EndeTagLesen { get; init; }
    public Action<int?>? EndeTagSetzen { get; init; }
    public Func<int?>? EndeMonatLesen { get; init; }
    public Action<int?>? EndeMonatSetzen { get; init; }

    /// <summary>Der Name des Zeitraums (Winter, Ostern, Sommer, Herbst) — das Zeilenkennzeichen.</summary>
    public string Zeitraum => ZeitraumLesen?.Invoke() ?? "";

    /// <summary>Tag des Ferienbeginns (1 bis 31); leer = nicht eingetragen.</summary>
    public int? BeginnTag
    {
        get => BeginnTagLesen?.Invoke();
        set => BeginnTagSetzen?.Invoke(value);
    }

    /// <summary>Monat des Ferienbeginns (1 bis 12); leer = nicht eingetragen.</summary>
    public int? BeginnMonat
    {
        get => BeginnMonatLesen?.Invoke();
        set => BeginnMonatSetzen?.Invoke(value);
    }

    /// <summary>Tag des Ferienendes (1 bis 31); leer = nicht eingetragen.</summary>
    public int? EndeTag
    {
        get => EndeTagLesen?.Invoke();
        set => EndeTagSetzen?.Invoke(value);
    }

    /// <summary>Monat des Ferienendes (1 bis 12); leer = nicht eingetragen.</summary>
    public int? EndeMonat
    {
        get => EndeMonatLesen?.Invoke();
        set => EndeMonatSetzen?.Invoke(value);
    }
}
