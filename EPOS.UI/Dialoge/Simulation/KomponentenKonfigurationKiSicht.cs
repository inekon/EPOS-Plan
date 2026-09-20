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
/// <para><b>Dieselben Felder dürfen zweimal im Katalog stehen.</b> Die sieben Werte der
/// Wärmepumpen-Konfiguration sind auch unter <c>Form_WP_Anlage</c> deklariert — dort
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

    public Func<int>? BhkwBetriebsartLesen { get; init; }
    public Action<int>? BhkwBetriebsartSetzen { get; init; }

    public Func<int>? BhkwLeistungsgrenzeLesen { get; init; }
    public Action<int>? BhkwLeistungsgrenzeSetzen { get; init; }

    /// <summary>
    /// Die Anlagendaten der Wärmepumpe; <c>null</c> = die Karte führt keine Anlage
    /// oder die Plattform stellt den Weg nicht — dann bleiben die sieben Felder leer.
    /// </summary>
    public Func<EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageDaten?>? AnlageLesen { get; init; }

    // =====================================================================
    //  Die Einträge der drei Wahlfelder (KI-F1b)
    // =====================================================================

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
        set { if (Anlage is { } a) a.Heizstab = value; }
    }

    /// <summary>Die Anlage hat eine tägliche Sperrzeit.</summary>
    public bool Sperrung
    {
        get => Anlage?.Sperrung ?? false;
        set { if (Anlage is { } a) a.Sperrung = value; }
    }

    /// <summary>Beginn der täglichen Sperrzeit [h/Tag].</summary>
    public int? SperrzeitVon
    {
        get => Anlage?.SperrzeitVon;
        set { if (Anlage is { } a) a.SperrzeitVon = value; }
    }

    /// <summary>Ende der täglichen Sperrzeit [h/Tag].</summary>
    public int? SperrzeitBis
    {
        get => Anlage?.SperrzeitBis;
        set { if (Anlage is { } a) a.SperrzeitBis = value; }
    }

    /// <summary>Bivalenter Betrieb mit einem zweiten Erzeuger.</summary>
    public bool BivalenterBetrieb
    {
        get => Anlage?.BivalenterBetrieb ?? false;
        set { if (Anlage is { } a) a.BivalenterBetrieb = value; }
    }

    /// <summary>Der Steuerwert der Betriebsart: alternativ, parallel oder teilparallel.</summary>
    public string Betriebsart
    {
        get => Anlage?.Betriebsart ?? "";
        set { if (Anlage is { } a) a.Betriebsart = value ?? ""; }
    }

    /// <summary>
    /// Die Id des Energieträgers der Anlage; er bestimmt Preis und Emissionen des
    /// Antriebsstroms. <c>0</c> heißt „keiner gewählt".
    /// </summary>
    public int Energietraeger
    {
        get => Anlage?.CarrierId ?? 0;
        set { if (Anlage is { } a) a.CarrierId = value; }
    }

    /// <summary>Die Bivalenztemperatur [°C], ab der der zweite Erzeuger übernimmt.</summary>
    public double? Abschaltpunkt
    {
        get => Anlage?.Abschaltpunkt;
        set { if (Anlage is { } a) a.Abschaltpunkt = value; }
    }

    private EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageDaten? Anlage => AnlageLesen?.Invoke();
}
