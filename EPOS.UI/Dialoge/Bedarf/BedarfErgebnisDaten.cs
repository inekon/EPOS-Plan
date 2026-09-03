namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Welche der beiden Ergebnisansichten gezeigt wird (iU9-W8.0e).
///
/// <para>Die drei abgelösten Masken <c>Form_ErgStromverbraucher</c>,
/// <c>Form_ErgProzesswaerme</c> und <c>Form_ErgBrauchwasserwaerme</c> tragen denselben
/// Aufbau — drei Reiter „Ergebnisse", „monatlich", „Grafik" —, unterscheiden sich aber in
/// dem, was sie zeigen: EINE Reihe (Strom) gegenüber zwei oder drei Reihen (Wärme).</para>
/// </summary>
public enum ErgebnisSicht
{
    /// <summary>Strombedarf: vier Kennzahlen, eine Monatsreihe (<c>Form_ErgStromverbraucher</c>).</summary>
    Strom,

    /// <summary>Wärmebedarf: sieben Kennzahlen, Prozesse und Gebäude, optional Brauchwasser.</summary>
    Waerme
}

/// <summary>
/// Was der Ergebnisdialog anzeigt — ein EINGEFRORENER Auszug des Rechenobjekts
/// (iU9-W8.0e).
///
/// <para><b>Warum eingefroren.</b> Die drei Vorläufer bekamen das LEBENDE
/// <c>SimulationStrombedarf</c> bzw. <c>SimulationWaermebedarf</c> in die Hand und lasen
/// beim Umschalten der Optionsknöpfe immer wieder daraus. Sie sind reine ANZEIGEN — nichts
/// schreibt zurück. Die Hülle baut daraus dieses Datenobjekt und rendert die Bilder vorab;
/// die Komponente kennt die Simulationsklassen nicht (Regel aus
/// <c>EPOS.UI/CLAUDE.md</c>).</para>
///
/// <para><b>Fehlende Reihen bleiben <c>null</c></b> und zeigen „—" statt einer 0: Ein nicht
/// gerechneter Wert ist etwas anderes als ein gerechneter Wert von null.</para>
/// </summary>
public sealed class BedarfErgebnisDaten
{
    /// <summary>Welche der beiden Ansichten.</summary>
    public ErgebnisSicht Sicht { get; set; } = ErgebnisSicht.Waerme;

    /// <summary>
    /// Zeigt die Wärmeansicht die dritte Sicht „Brauchwasser"? Nur
    /// <c>Form_ErgBrauchwasserwaerme</c> hatte sie; <c>Form_ErgProzesswaerme</c> kennt nur
    /// Prozesse und Gebäude.
    /// </summary>
    public bool MitBrauchwasser { get; set; }

    /// <summary>
    /// Zusatz hinter dem Fenstertitel („ - ‹Name›"), den
    /// <c>Form_Brauchwasser.btn_Berechnen_Click</c>:308 anhängte. Leer = ohne.
    /// </summary>
    public string TitelZusatz { get; set; } = "";

    /// <summary>
    /// Welcher Reiter beim Öffnen vorn steht: 0 = Kennzahlen, 1 = Monatswerte,
    /// 2 = Grafik. Wörtlich der Parameter von <c>SetPage</c>.
    /// </summary>
    public int StartReiter { get; set; }

    // --- Kennzahlen ------------------------------------------------------------

    /// <summary>
    /// Die Kennzahlen des Blattes in Anzeigereihenfolge — Beschriftung, Wert (bereits
    /// mit <c>F2</c> formatiert) und Einheit. Die Hülle füllt sie je Ausprägung:
    /// vier beim Strom, sieben bei der Wärme.
    /// </summary>
    public IReadOnlyList<ErgebnisKennzahl> Kennzahlen { get; set; } = Array.Empty<ErgebnisKennzahl>();

    // --- Monatsreihen ----------------------------------------------------------

    /// <summary>
    /// Die wählbaren Monatssichten in Anzeigereihenfolge. Strom hat genau eine (ohne
    /// Optionsgruppe), Wärme zwei oder drei.
    /// </summary>
    public IReadOnlyList<Monatssicht> Sichten { get; set; } = Array.Empty<Monatssicht>();

    /// <summary>
    /// Der Jahresverlauf des Brauchwassers (8 760 Stunden) als fertiges Bild; <c>null</c> =
    /// der Schalter „Jahresverlauf" erscheint nicht.
    /// </summary>
    public byte[]? JahresverlaufBild { get; set; }
}

/// <summary>Eine Kennzahl des ersten Reiters — Beschriftung, Wert, Einheit.</summary>
/// <param name="Bezeichnung">Die Beschriftung, z. B. „Gesamter Wärmebedarf:".</param>
/// <param name="Wert">Der bereits formatierte Wert; leer zeigt „—".</param>
/// <param name="Einheit">Die Einheit rechts, z. B. „MWh".</param>
public sealed record ErgebnisKennzahl(string Bezeichnung, string Wert, string Einheit);

/// <summary>
/// Eine wählbare Monatssicht — der Optionsknopf des Vorläufers samt seiner Tabelle und
/// seinem Bild.
/// </summary>
/// <param name="Bezeichnung">Beschriftung des Optionsknopfes, z. B. „Prozesse".</param>
/// <param name="Werte">
/// Die zwölf Monatswerte, bereits mit <c>F2</c> formatiert; <c>null</c> zeigt „—".
/// </param>
/// <param name="Bild">Die Monatssäulen dieser Sicht als PNG (die Hülle rendert vorab).</param>
/// <param name="IstBrauchwasser">
/// Bei dieser Sicht erscheint der Schalter „Jahresverlauf" — nur die Brauchwassersicht
/// von <c>Form_ErgBrauchwasserwaerme</c> hatte ihn.
/// </param>
public sealed record Monatssicht(string Bezeichnung, IReadOnlyList<string>? Werte,
                                 byte[]? Bild, bool IstBrauchwasser = false);
