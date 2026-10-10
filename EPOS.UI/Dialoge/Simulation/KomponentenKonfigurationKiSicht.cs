using KiKern;

namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// Das FLACHE Abbild der Maske „Konfiguration einer Komponente" für den Hilfe-Assistenten
/// (Welle KI‑F2).
///
/// <para><b>EINE Maske, ZWEI Arbeitskopien.</b> Der Dialog zeigt je nach Komponentenart
/// etwas anderes: beim Heizkessel und beim BHKW die projektweiten Laufparameter
/// (<c>ParameterDaten</c>), bei der Wärmepumpe die Konfiguration DIESER Anlage
/// (<c>WaermepumpeAnlageDaten</c>, als Baustein
/// <c>WaermepumpeKonfiguration</c> eingebettet). Ein Katalogeintrag nennt EIN
/// Daten-Objekt vor dem Punkt — deshalb legt diese Klasse beide Stände unter einem
/// Namen zusammen, genau wie <c>SimulationKiSicht</c> vier Stände zusammenlegt.</para>
///
/// <para><b>Warum die Laufparameter nicht unmittelbar gehen.</b>
/// <c>ParameterDaten</c> trägt öffentliche FELDER und keine Eigenschaften; die
/// Maskenbrücke löst über <c>GetProperty</c> auf und fände dort nichts.</para>
///
/// <para><b>Dieselben Felder dürfen zweimal im Katalog stehen.</b> Die Werte der
/// Wärmepumpen-Konfiguration — samt der fünf Felder der Gruppe „Kühlbetrieb" (Stufe KU2 Welle 3) —
/// sind auch unter <c>Form_WP_Anlage</c> deklariert — dort
/// stehen sie im Anlagendialog, hier in der Konfiguration der Simulation. Eine Maske
/// ist, was offen ist; welche das gerade ist, sagt die Anmeldung.</para>
/// </summary>
public sealed class KomponentenKonfigurationKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<double>? BereitschaftLesen { get; init; }
    public Action<double>? BereitschaftSetzen { get; init; }

    public Func<double?>? HeizgrenzeLesen { get; init; }
    public Action<double?>? HeizgrenzeSetzen { get; init; }

    public Func<int>? BhkwBetriebsartLesen { get; init; }
    public Action<int>? BhkwBetriebsartSetzen { get; init; }

    public Func<int>? BhkwLeistungsgrenzeLesen { get; init; }
    public Action<int>? BhkwLeistungsgrenzeSetzen { get; init; }

    /// <summary>
    /// Die Anlagendaten der Wärmepumpe; <c>null</c> = die Karte führt keine Anlage
    /// oder die Plattform stellt den Weg nicht — dann bleiben die sieben Felder leer.
    /// </summary>
    public Func<EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageDaten?>? AnlageLesen { get; init; }

    /// <summary>
    /// Die Kühlgaben des Dialogs — Stützstellen, Sperrgrund und Stromträger des Projekts (Stufe
    /// KU2 Welle 3). <c>null</c> = die Maske zeigt keine Gruppe „Kühlbetrieb"; dann lehnt jede
    /// Setzung eines Kühlfeldes benannt ab.
    /// </summary>
    public Func<EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlGaben?>? Kuehlgaben { get; init; }

    // =====================================================================
    //  Die Einträge der drei Wahlfelder (KI-F1b)
    // =====================================================================

    /// <summary>
    /// Der Meldeweg der Hand nach einer Setzung eines Wärmepumpen-Werts — derselbe, den der
    /// Baustein nach jeder Eingabe geht (<c>Melden</c> → <c>Geaendert</c> → Neuzeichnen samt
    /// Räumen der Kühlwarnung); <c>null</c> = die Maske meldet nicht.
    /// </summary>
    public Action? AnlageGemeldet { get; init; }

    /// <summary>
    /// Der Text, den die Maske ohne Anlage zeigt (<c>SIMKONF_MSG_WP_OHNE_ANLAGE</c>); leer = <c>KI_FELD_KEIN_SATZ</c>.
    /// </summary>
    public Func<string?>? OhneAnlageText { get; init; }

    /// <summary>Liefert die BHKW-Betriebsarten, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BhkwBetriebsartEintraege { get; init; }

    /// <summary>Liefert die Betriebsarten der Wärmepumpe, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BetriebsartEintraege { get; init; }

    /// <summary>Liefert die Energieträger, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? EnergietraegerEintraege { get; init; }

    /// <summary>
    /// Wärmegeführt, stromgeführt und ohne Einspeisung — die Auswahl der Maske
    /// (KI-F1b); der Schlüssel ist der Steuerwert 0/1/2 des Bestands.
    /// </summary>
    public IReadOnlyList<KiWahleintrag> BhkwBetriebsartWahl
        => BhkwBetriebsartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Alternativ, parallel und teilparallel — die Auswahl der Maske (KI-F1b).</summary>
    public IReadOnlyList<KiWahleintrag> BetriebsartWahl
        => BetriebsartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Energieträger des Katalogs — die Auswahl der Maske (KI-F1b).</summary>
    public IReadOnlyList<KiWahleintrag> EnergietraegerWahl
        => EnergietraegerEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die projektweiten Laufparameter
    // =====================================================================

    /// <summary>Nur Heizkessel: die Betriebsbereitschaft [h/a] — ein Wert des PROJEKTS.</summary>
    public double Bereitschaft
    {
        get => BereitschaftLesen?.Invoke() ?? 0.0;
        set => BereitschaftSetzen?.Invoke(value);
    }

    /// <summary>
    /// Nur Heizkessel: die Heizgrenze der Kesselbereitschaft [°C] — ein Wert des PROJEKTS;
    /// <c>null</c> = leer (Vorgabe). Geprüft wird beim OK des Dialogs.
    /// </summary>
    public double? Heizgrenze
    {
        get => HeizgrenzeLesen?.Invoke();
        set => HeizgrenzeSetzen?.Invoke(value);
    }

    /// <summary>
    /// Nur BHKW: die Betriebsart des Projekts — 0 = wärmegeführt, 1 = stromgeführt,
    /// 2 = ohne Einspeisung.
    /// </summary>
    public int BhkwBetriebsart
    {
        get => BhkwBetriebsartLesen?.Invoke() ?? 0;
        set => BhkwBetriebsartSetzen?.Invoke(value);
    }

    /// <summary>Nur BHKW: die projektweite untere Modulationsgrenze [%].</summary>
    public int BhkwLeistungsgrenze
    {
        get => BhkwLeistungsgrenzeLesen?.Invoke() ?? 0;
        set => BhkwLeistungsgrenzeSetzen?.Invoke(value);
    }

    // =====================================================================
    //  Die Konfiguration DIESER Wärmepumpe
    // =====================================================================

    /// <summary>Elektrische Nachheizung vorhanden.</summary>
    public bool Heizstab
    {
        get => Anlage?.Heizstab ?? false;
        set => Anlagewert(a => a.Heizstab = value);
    }

    /// <summary>Die Anlage hat eine tägliche Sperrzeit.</summary>
    public bool Sperrung
    {
        get => Anlage?.Sperrung ?? false;
        set => Anlagewert(a => a.Sperrung = value);
    }

    /// <summary>Beginn der täglichen Sperrzeit [h/Tag].</summary>
    public int? SperrzeitVon
    {
        get => Anlage?.SperrzeitVon;
        set => Anlagewert(a => a.SperrzeitVon = value);
    }

    /// <summary>Der höchste Vorlauf der Anlage [°C] (Gruppe „Betriebszeiten"); leer = projektierter Vorlauf.</summary>
    public double? VorlaufMax
    {
        get => Anlage?.VorlaufMax;
        set => Anlagewert(a => a.VorlaufMax = value);
    }

    /// <summary>Ende der täglichen Sperrzeit [h/Tag].</summary>
    public int? SperrzeitBis
    {
        get => Anlage?.SperrzeitBis;
        set => Anlagewert(a => a.SperrzeitBis = value);
    }

    /// <summary>Bivalenter Betrieb mit einem zweiten Erzeuger.</summary>
    public bool BivalenterBetrieb
    {
        get => Anlage?.BivalenterBetrieb ?? false;
        set => Anlagewert(a => a.BivalenterBetrieb = value);
    }

    /// <summary>Der Steuerwert der Betriebsart: alternativ, parallel oder teilparallel.</summary>
    public string Betriebsart
    {
        get => Anlage?.Betriebsart ?? "";
        set => Anlagewert(a => a.Betriebsart = value ?? "");
    }

    /// <summary>
    /// Die Id des Energieträgers der Anlage; er bestimmt Preis und Emissionen des
    /// Antriebsstroms. <c>0</c> heißt „keiner gewählt".
    /// </summary>
    public int Energietraeger
    {
        get => Anlage?.CarrierId ?? 0;
        set => Anlagewert(a => a.CarrierId = value);
    }

    /// <summary>Die Bivalenztemperatur [°C], ab der der zweite Erzeuger übernimmt.</summary>
    public double? Abschaltpunkt
    {
        get => Anlage?.Abschaltpunkt;
        set => Anlagewert(a => a.Abschaltpunkt = value);
    }

    // =====================================================================
    //  Die Gruppe „Kühlbetrieb" DIESER Wärmepumpe (Stufe KU2 Welle 3)
    // =====================================================================
    //
    // Dieselben fünf Felder und dieselben Wege wie unter Form_WP_Anlage
    // (WaermepumpeKuehlKiWege): Was die Maske weich sperrt, lehnt die Setzung benannt ab.

    /// <summary>„Maschine auch zum Kühlen benutzen" — gesperrt ohne Kühlkennlinie oder mit Quellspeicher.</summary>
    public bool Kuehlbetrieb
    {
        get => Anlage?.Kuehlbetrieb ?? false;
        set { EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.KuehlbetriebSetzen(Anlage, Gaben, value); AnlageGemeldet?.Invoke(); }
    }

    /// <summary>Der Kühl-Vorlauf [°C] aus den Stützstellen; leer = kleinster Stützwert.</summary>
    public int? KuehlVorlauf
    {
        get => Anlage?.KuehlVorlauf;
        set { EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.VorlaufSetzen(Anlage, Gaben, value); AnlageGemeldet?.Invoke(); }
    }

    /// <summary>Die Stützstellen der Kühlkennlinie, die die Maske zur Wahl stellt — ohne die gesperrten.</summary>
    public IReadOnlyList<KiWahleintrag> KuehlVorlaufWahl
        => EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.VorlaufWahl(Anlage, Gaben);

    /// <summary>Der Hilfsstromanteil in PROZENT, wie die Maske ihn zeigt; gespeichert wird der Anteil.</summary>
    public double? KuehlHilfsstromanteil
    {
        get => EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.HilfsstromProzent(Anlage);
        set { EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.HilfsstromSetzen(Anlage, Gaben, value); AnlageGemeldet?.Invoke(); }
    }

    /// <summary>Der Stromträger des Kältestroms; leer = wie Heizbetrieb.</summary>
    public int? KuehlCarrierId
    {
        get => Anlage?.KuehlCarrierId;
        set { EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.KuehltraegerSetzen(Anlage, Gaben, value); AnlageGemeldet?.Invoke(); }
    }

    /// <summary>Die Stromträger des Projekts, die die Maske für den Kältestrom zur Wahl stellt.</summary>
    public IReadOnlyList<KiWahleintrag> KuehlCarrierIdWahl
        => EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.TraegerWahl(Gaben);

    /// <summary>Die Abrechnungsart des Kältestroms (E34): 0 = anteilig am Netzbezug, 1 = eigener Zähler.</summary>
    public int KuehlAbrechnung
    {
        get => EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.Abrechnung(Anlage);
        set { EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.AbrechnungSetzen(Anlage, Gaben, value); AnlageGemeldet?.Invoke(); }
    }

    /// <summary>Die zwei Abrechnungsarten der Maske — dieselben Texte wie ihre Optionsgruppe.</summary>
    public IReadOnlyList<KiWahleintrag> KuehlAbrechnungWahl
        => EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.AbrechnungWahl();

    /// <summary>„Freie Kühlung über die Wärmequelle" (KU3-6) — gesperrt ohne Sole-/Wasser-Wasser-Bauart oder gepflegte Quelle.</summary>
    public bool KuehlFrei
    {
        get => Anlage?.KuehlFrei ?? false;
        set { EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.KuehlFreiSetzen(Anlage, Gaben, value); AnlageGemeldet?.Invoke(); }
    }

    /// <summary>Die Grädigkeit des Wärmetauschers der freien Kühlung [K]; leer = 3,0 K.</summary>
    public double? KuehlFreiGraedigkeitK
    {
        get => Anlage?.KuehlFreiGraedigkeitK;
        set { EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.KuehlFreiGraedigkeitSetzen(Anlage, Gaben, value); AnlageGemeldet?.Invoke(); }
    }

    /// <summary>Die Leistungsgrenze der freien Kühlung [kW]; leer = Kälteleistung der Kennlinie.</summary>
    public double? KuehlFreiLeistungKw
    {
        get => Anlage?.KuehlFreiLeistungKw;
        set { EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlKiWege.KuehlFreiLeistungSetzen(Anlage, Gaben, value); AnlageGemeldet?.Invoke(); }
    }

    // =====================================================================
    //  Sperrgrund und Schreibweg der Wärmepumpen-Felder
    // =====================================================================

    /// <summary>
    /// Die Feldschlüssel des Dialogkatalogs, die an der Anlage der Wärmepumpe hängen — die
    /// neun der Konfiguration und die sieben der Gruppe „Kühlbetrieb".
    /// </summary>
    public static readonly IReadOnlySet<string> ANLAGENFELDER = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "heizstab", "sperrzeit", "sperrzeit_von", "sperrzeit_bis", "vorlauf_max",
        "bivalenter_betrieb", "betriebsart", "energietraeger", "bivalenztemperatur",
        "kuehlbetrieb", "kuehl_vorlauf", "hilfsstromanteil", "kuehltraeger",
        "kuehl_abrechnung", "kuehl_frei", "kuehl_frei_graedigkeit", "kuehl_frei_leistung"
    };

    /// <summary>
    /// Der Sperrgrund je Feld (<c>KiMaskenhaken.Sperrgrund</c>); <c>null</c> = frei.
    /// </summary>
    /// <remarks>
    /// Ohne Anlage — die Plattform bietet den Weg nicht, oder die Kartenzeile führt keine
    /// Anlage — zeigt die Maske die Wärmepumpen-Felder gar nicht; ein Wert dafür wird VOR der
    /// Bestätigung abgelehnt, mit dem Text, den die Maske an ihrer Stelle zeigt. Den
    /// Energieträger setzt der Assistent hier: Die Trägerwahl schreibt nur in die Arbeitskopie,
    /// in die Datenbank trägt sie erst der OK-Weg des Wirts.
    /// </remarks>
    public string? Sperrgrund(string feld)
    {
        if (string.IsNullOrWhiteSpace(feld) || !ANLAGENFELDER.Contains(feld)) return null;
        return Anlage is null ? OhneAnlage : null;
    }

    private string OhneAnlage
    {
        get
        {
            string? text = OhneAnlageText?.Invoke();
            return string.IsNullOrWhiteSpace(text)
                ? WindowsFormsApplication1.MyResource.Resource.KI_FELD_KEIN_SATZ
                : text;
        }
    }

    /// <summary>
    /// Schreibt in die Arbeitskopie der Anlage und geht danach den Meldeweg der Hand; ohne
    /// Anlage eine benannte Absage statt einer stillen Verwerfung.
    /// </summary>
    private void Anlagewert(Action<EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageDaten> schreiben)
    {
        EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageDaten a = Anlage
            ?? throw new InvalidOperationException(OhneAnlage);
        schreiben(a);
        AnlageGemeldet?.Invoke();
    }

    private EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageDaten? Anlage => AnlageLesen?.Invoke();

    private EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKuehlGaben? Gaben => Kuehlgaben?.Invoke();
}
