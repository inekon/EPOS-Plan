namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Der WÄRMEBEDARF EINES GEBÄUDES (iU9-W9.8, Anwenderwunsch <b>W9‑E‑2</b> vom
/// 05.09.2026) — das eingefrorene Ergebnis von
/// <c>EPOS.Kern/Controller/GebaeudeBedarfCtrl</c>, wie es die Hülle hereinreicht.
///
/// <para><b>Nur HEIZUNG.</b> Kein Brauchwasser, keine Prozesswärme, keine Summe über
/// die Bedarfsarten — der Anwender hat das ausdrücklich so gewünscht.</para>
///
/// <para><b>Die Einheit steht AM WERT</b> (Hausregel seit W8‑O‑5): Die Energiemengen
/// liegen in MWh, die Last in kW, die Vollbenutzungsstunden in h/a. Umgerechnet wird
/// erst an der Anzeigekante, über <c>Energieeinheit</c>.</para>
///
/// <para>Die 8 760 Stundenwerte stehen NICHT hier: Ein Bild zeichnet der Kern, die
/// Komponente holt es über einen Delegaten (Risiko R‑W8‑2 — <c>EPOS.UI</c> ruft keinen
/// Renderer).</para>
/// </summary>
public sealed class GebaeudeBedarfDaten
{
    /// <summary>Der Gebäudename der Projektkopie; er steht in der Kontextzeile.</summary>
    public string Name { get; init; } = "";

    /// <summary>Die Jahressumme der Heizwärme in <b>MWh</b>.</summary>
    public double HeizwaermeMwh { get; init; }

    /// <summary>Die höchste Stundenlast in <b>kW</b>.</summary>
    public double MaxLastKw { get; init; }

    /// <summary>
    /// Die Vollbenutzungsstunden [h/a]. <c>null</c> heißt „gibt es nicht" (Höchstlast 0)
    /// und zeigt „—" statt einer erfundenen Zahl.
    /// </summary>
    public double? VollbenutzungsstundenH { get; init; }

    /// <summary>Die zwölf Monatssummen in <b>MWh</b>; leer = keine Monatsübersicht.</summary>
    public IReadOnlyList<double> MonatswerteMwh { get; init; } = new List<double>();

    // ---- Stufen G1 und G2 (Umsetzungskonzept Gebaeudesimulation 1.4, 2.7) --------
    //
    // Alle nullbar: ohne Wert steht "—", nie eine erfundene Zahl.

    /// <summary>
    /// Der Rechenweg als Anzeigetext — auf dem Altweg „Tagesbilanz (Bestandsweg)".
    /// Leer = keine Zeile.
    /// </summary>
    public string Modelltext { get; init; } = "";

    /// <summary>Größtes gleitendes Mittel über 24 Stunden in <b>kW</b>.</summary>
    public double? SpitzeTagesmittelKw { get; init; }

    /// <summary>95-%-Quantil der Stundenlast in <b>kW</b>.</summary>
    public double? SpitzeQuantil95Kw { get; init; }

    /// <summary>
    /// Kühlbedarf in <b>MWh</b> — auf dem VDI-Weg die Kühlreihe des Modells, auf dem
    /// Bestandsweg 0 (F-K18); sonst <c>null</c>.
    /// </summary>
    public double? KuehlenergieMwh { get; init; }

    /// <summary>Stunden mit Kühlbedarf [h] — nur auf dem VDI-Weg.</summary>
    public int? KuehlstundenH { get; init; }

    /// <summary>Mittlere Raumlufttemperatur in der Nutzungszeit [°C] — nur auf dem VDI-Weg.</summary>
    public double? MittlereRaumtemperaturC { get; init; }

    /// <summary>Stunden der Nutzungszeit über der oberen Raumtemperatur [h] — nur auf dem VDI-Weg.</summary>
    public int? UeberhitzungsstundenH { get; init; }

    /// <summary>Stunden mit Sommerlüftung [h] — nur auf dem VDI-Weg mit eingeschalteter Regel.</summary>
    public int? SommerlueftungsstundenH { get; init; }

    /// <summary>
    /// Der jeweils ANDERE Rechenweg desselben Gebäudes — die zweite Spalte des Vergleichs
    /// alt/neu (Konzept 8.2, Umsetzungskonzept 2.7). <c>null</c> = kein Vergleich (der andere
    /// Weg lieferte nichts). Er lebt, solange es zwei Rechenwege gibt (bis Stufe GA,
    /// Löschliste Kapitel 6).
    /// </summary>
    public GebaeudeBedarfDaten? Vergleich { get; init; }

    /// <summary>Rechnet dieser Satz auf dem VDI-Weg? Nur dann gibt es das Bild „Raumtemperatur".</summary>
    public bool IstVdi6007 { get; init; }

    // ---- Stufe KU1: der Abschnitt „Kältebedarf" (Kühlkonzept 8.4; E21, F-K18) --------
    //
    // Dieselben Bausteine wie die Wärmeseite: Kennzahltabelle, eigenes Bild, Monatswerte.
    // Auf dem Bestandsweg steht der Abschnitt mit 0 und Hinweis — nicht „—", keine leere
    // Gruppe (8.6); ohne Satz (kein Gebäude, keine Gaben) steht er nicht da.

    /// <summary>Steht der Abschnitt „Kältebedarf"?</summary>
    public bool KaelteAbschnitt { get; init; }

    /// <summary>Die höchste Stundenkühllast in <b>kW</b>.</summary>
    public double? KaeltelastMaxKw { get; init; }

    /// <summary>Vollbenutzungsstunden der Kälte [h/a]; <c>null</c> = keine Kältelast.</summary>
    public double? VollbenutzungsstundenKaelteH { get; init; }

    /// <summary>Stunden mit gleichzeitigem Heizen und Kühlen [h] (K6, nicht saldiert).</summary>
    public int? StundenHeizenUndKuehlenH { get; init; }

    /// <summary>Die zwölf Monatssummen der Kühlreihe in <b>MWh</b>; leer = keine Kühlspalte.</summary>
    public IReadOnlyList<double> KuehlMonatswerteMwh { get; init; } = new List<double>();

    /// <summary>
    /// Die Herleitungszeilen des Abschnitts, fertig formuliert: wie der Kältebedarf entsteht
    /// (wirksame Kühlung, informativ, Bestandsweg) und die Grenze der Zahl (K5, sensible Kälte
    /// ohne Entfeuchtung) — sie steht an JEDER Kältezahl.
    /// </summary>
    public IReadOnlyList<string> KaelteHerleitung { get; init; } = new List<string>();
}
