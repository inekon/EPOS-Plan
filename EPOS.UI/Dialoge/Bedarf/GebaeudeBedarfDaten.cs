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
}
