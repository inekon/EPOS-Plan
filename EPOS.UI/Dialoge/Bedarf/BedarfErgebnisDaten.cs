using WindowsFormsApplication1;

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

    /// <summary>
    /// Die GANGLINIE hinter dem Grafikreiter — Woche und Tag (Anwenderwunsch W8‑E‑2 der
    /// Windows-Abnahme 05.09.2026); <c>null</c> = der Reiter zeigt nur die Jahressicht
    /// wie bisher.
    /// </summary>
    public Ganglinienquelle? Ganglinie { get; set; }
}

/// <summary>
/// Welche Sorte Kennzahl auf dem ersten Reiter steht (Anwenderwunsch W8‑E‑2,
/// Windows-Abnahme 05.09.2026).
///
/// <para><b>Warum die Unterscheidung nötig ist.</b> Der Bestand reihte alle vier
/// Stromkennzahlen untereinander, und die erste hieß „max. Strombedarf" — eine
/// LEISTUNG in kW zwischen drei ENERGIEMENGEN in MWh, mit einer Beschriftung, die
/// wie ein vierter Summand klang. Der Anwender hat genau das beanstandet: „max.
/// Strombedarf ist falsch, das ist die max. Leistung, und sie gehört nicht in die
/// Summe."</para>
/// </summary>
public enum Kennzahlart
{
    /// <summary>Eine LEISTUNG in kW — eigener Block, außerhalb der Summe.</summary>
    Leistung,

    /// <summary>Ein Posten der Energiebilanz.</summary>
    Energie,

    /// <summary>Die SUMME der Posten — abgesetzt am Ende des Blattes.</summary>
    Summe
}

/// <summary>Die Zeitstufe des Ganglinienbildes (W8‑E‑2).</summary>
public enum Gangstufe
{
    /// <summary>Das ganze Jahr — die Sicht des Bestands (Monatssäulen).</summary>
    Jahr,

    /// <summary>Eine Woche, 168 Stunden, mit Navigator.</summary>
    Woche,

    /// <summary>Ein Tag, 24 Stunden, mit Navigator.</summary>
    Tag
}

/// <summary>
/// Woher die Bilder der Zeitstufen Woche und Tag kommen (W8‑E‑2).
///
/// <para><b>Ein Delegat, kein Bildvorrat.</b> 52 Wochen und 365 Tage sind 417 Bilder;
/// sie vorab zu zeichnen hieße, für einen Blick auf eine Woche ein Jahr zu rendern.
/// Die Hülle zeichnet deshalb auf Zuruf — dasselbe Muster, mit dem der Stromgang-Reiter
/// der Ergebnisseite (W11b) seine Bilder holt. Die Komponente ruft weiterhin keinen
/// Renderer; sie ruft die Hülle.</para>
/// </summary>
public sealed class Ganglinienquelle
{
    /// <summary>Wie viele Wochen der Navigator kennt (52 bei einem vollen Jahr).</summary>
    public int Wochen { get; init; } = 52;

    /// <summary>Wie viele Tage der Navigator kennt (365 bei einem vollen Jahr).</summary>
    public int Tage { get; init; } = 365;

    /// <summary>
    /// Liefert das Bild zu einer Stufe und einer NULLBASIERTEN Nummer;
    /// <c>null</c> = kein Bild, die Anzeige zeigt ihren Platzhalter.
    /// </summary>
    public Func<Gangstufe, int, byte[]?>? Bild { get; init; }
}

/// <summary>
/// Eine Kennzahl des ersten Reiters — Beschriftung, Wert, Einheit.
///
/// <para><b>Zwei Sorten Kennzahl.</b> Eine LEISTUNG („max. Wärmelast", kW) ist ein
/// fertiger Text mit fester Einheit — daran gibt es nichts umzurechnen. Eine
/// ENERGIEMENGE trägt zusätzlich <see cref="Energie"/> und
/// <see cref="QuelleEinheit"/>: die Zahl und die Einheit, IN DER SIE VORLIEGT. Erst
/// damit kann die Anzeige der Einheitenwahl folgen (Anwenderentscheid W8‑O‑5 vom
/// 04.09.2026). <see cref="Wert"/> und <see cref="Einheit"/> bleiben dann die
/// MWh-Fassung und dienen als Rückfall.</para>
/// </summary>
/// <param name="Bezeichnung">Die Beschriftung, z. B. „Gesamter Wärmebedarf:".</param>
/// <param name="Wert">Der bereits formatierte Wert; leer zeigt „—".</param>
/// <param name="Einheit">Die Einheit rechts, z. B. „MWh".</param>
public sealed record ErgebnisKennzahl(string Bezeichnung, string Wert, string Einheit)
{
    /// <summary>
    /// Der Zahlenwert dieser Energiemenge in <see cref="QuelleEinheit"/>;
    /// <c>null</c> = keine Energiemenge (Leistung, Text), die Anzeige nimmt
    /// <see cref="Wert"/> und <see cref="Einheit"/> unverändert.
    /// </summary>
    public double? Energie { get; init; }

    /// <summary>Die Einheit, in der <see cref="Energie"/> vorliegt.</summary>
    public Energieeinheit? QuelleEinheit { get; init; }

    /// <summary>
    /// Wo die Zeile steht: im Leistungsblock, unter den Posten oder als Summe am Ende
    /// (Anwenderwunsch W8‑E‑2). Vorgabe ist <see cref="Kennzahlart.Energie"/> — ein
    /// Datensatz, der nichts dazu sagt, sieht aus wie vorher.
    /// </summary>
    public Kennzahlart Art { get; init; } = Kennzahlart.Energie;
}

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
                                 byte[]? Bild, bool IstBrauchwasser = false)
{
    /// <summary>
    /// Die zwölf Monatswerte als ZAHL in <see cref="QuelleEinheit"/>; <c>null</c> =
    /// die Tabelle nimmt die fertigen <see cref="Werte"/> und folgt der
    /// Einheitenwahl nicht.
    /// </summary>
    public IReadOnlyList<double>? Zahlen { get; init; }

    /// <summary>Die Einheit, in der <see cref="Zahlen"/> vorliegen.</summary>
    public Energieeinheit? QuelleEinheit { get; init; }

    /// <summary>
    /// Dasselbe Säulenbild mit kWh-Beschriftung; <c>null</c> = es gibt nur
    /// <see cref="Bild"/>. Ein PNG lässt sich nicht umrechnen — die Hülle zeichnet
    /// beide Fassungen vorab, weil die Komponente keinen Renderer aufruft.
    /// </summary>
    public byte[]? BildKWh { get; init; }
}
