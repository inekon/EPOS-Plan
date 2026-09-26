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

    // ---- Anlagenkopplung AK1 (Konzept Anlagenkopplung 9.4, 12.1) ----------------------
    //
    // Nur für ein gekoppelt gerechnetes Gebäude; sonst false bzw. null - die Kacheln stehen dann
    // nicht da, statt „—" zu zeigen.

    /// <summary>Hat der Lauf das Gebäude gekoppelt gerechnet (Wärmeübergabe statt idealer Regelung)?</summary>
    public bool IstGekoppelt { get; init; }

    /// <summary>Heizzeitgewichtetes Mittel des gefahrenen Vorlaufs [°C]; <c>null</c> ohne Heizstunde.</summary>
    public double? VorlaufMittelC { get; init; }

    /// <summary>Dasselbe für den Rücklauf zur gelieferten Leistung [°C].</summary>
    public double? RuecklaufMittelC { get; init; }

    /// <summary>Stunden, in denen die Übergabe die Grenze war [h].</summary>
    public double? UebergabeBegrenztStundenH { get; init; }

    /// <summary>Die leise Zeile unter der Vorlaufkachel, fertig formuliert: Übergabeart und Auslegungspunkt.</summary>
    public string Heizkreiszeile { get; init; } = "";

    // ---- E37: der Kältekreis (Anlagenkopplung 8.3, 10.5), gespiegelt zum Heizkreis ----------
    //
    // Nur für ein kühlgekoppelt gerechnetes Gebäude; die Kacheln stehen im Abschnitt
    // „Kältebedarf" und tragen den Vermerk „sensibel" (K5).

    /// <summary>Hat der Lauf die Kälteseite des Gebäudes gekoppelt gerechnet (Kühlübergabe statt idealer Kühlung)?</summary>
    public bool IstKuehlgekoppelt { get; init; }

    /// <summary>Kältebedarfsgewichtetes Mittel des Kaltwasser-Vorlaufs [°C]; <c>null</c> ohne Kühlstunde.</summary>
    public double? KuehlVorlaufMittelC { get; init; }

    /// <summary>Dasselbe für den Rücklauf zur gelieferten Kühlleistung [°C].</summary>
    public double? KuehlRuecklaufMittelC { get; init; }

    /// <summary>Stunden, in denen die Kühlübergabe die Grenze war [h].</summary>
    public double? KuehlUebergabeBegrenztStundenH { get; init; }

    /// <summary>Die leise Zeile unter der Kühlvorlaufkachel, fertig formuliert: Art, Auslegungspunkt, „sensibel".</summary>
    public string Kuehlkreiszeile { get; init; } = "";

    /// <summary>Die leise Zeile unter der Kachel der begrenzten Stunden: davon an der Vorlaufgrenze, eine Vorgabe.</summary>
    public string KuehlBegrenztzeile { get; init; } = "";

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
    /// (wirksame Kühlung, freier Lauf ohne Kühlbedarf, Bestandsweg) und die Grenze der Zahl (K5, sensible Kälte
    /// ohne Entfeuchtung) — sie steht an JEDER Kältezahl.
    /// </summary>
    public IReadOnlyList<string> KaelteHerleitung { get; init; } = new List<string>();

    /// <summary>
    /// Die Zonen eines Mehrzonengebäudes in Rangfolge (Stufe G6b, Anwenderentscheid A2): eine Zeile
    /// je Zone, auch unbeheizt; leer bei höchstens einer Zone.
    /// </summary>
    public IReadOnlyList<GebaeudeBedarfZoneDaten> Zonen { get; init; } = new List<GebaeudeBedarfZoneDaten>();
}

/// <summary>
/// EINE Zone eines Mehrzonengebäudes im Bedarfsdialog (Stufe G6b) — die Kennzahlen der Zeile; die
/// Bilder holt der Dialog über Delegaten. Energiemengen in MWh, umgerechnet an der Anzeigekante.
/// </summary>
public sealed class GebaeudeBedarfZoneDaten
{
    /// <summary>Der Name der Zone.</summary>
    public string Name { get; init; } = "";

    /// <summary>Wird die Zone beheizt?</summary>
    public bool IstBeheizt { get; init; }

    /// <summary>Jahressumme der Heizwärme [MWh]; <c>null</c> für eine unbeheizte Zone.</summary>
    public double? HeizwaermeMwh { get; init; }

    /// <summary>Höchste Stundenlast [kW]; <c>null</c> für eine unbeheizte Zone.</summary>
    public double? MaxLastKw { get; init; }

    /// <summary>Mittlere Raumlufttemperatur über die Nutzungszeit [°C].</summary>
    public double? MittlereRaumtemperaturC { get; init; }

    /// <summary>Stunden der Nutzungszeit über der oberen Raumtemperatur der Zone [h].</summary>
    public int? UeberhitzungsstundenH { get; init; }
}
