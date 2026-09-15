using System;
using System.Collections.Generic;
using SpeicherEngine;

namespace WindowsFormsApplication1;

/// <summary>
/// EINE Zeile der Investitionsherleitung — die Investition GENAU EINER Einheit, zerlegt in
/// ihre drei Anteile (Auftrag #249, Konzept „Stromspeicher-Dialoge" 2.2 Punkt 4).
///
/// <para><b>Warum es die Zeile gibt.</b> Die Ergebnisansicht nannte unter der
/// Jahresprojektion nur die Summe. Der Anwender fragte am 12.09.2026: „Die Investition des
/// Speichers sollte 150 €/kWh mit 1 395 kWh sein. Im Ergebnis steht 284 250 €. Woher kommt
/// der Unterschied?" — die Antwort ist der feste und der leistungsbezogene Anteil, und die
/// gehört neben die Zahl und nicht in eine Rückfrage.</para>
///
/// <para><b>Es wird nichts nachgerechnet.</b> <see cref="SummeEuro"/> kommt aus
/// <see cref="FlottenWirtschaftlichkeit.Investition"/> — derselben Funktion, deren Summe
/// über alle Einheiten der CAPEX des Kapitalwerts ist. Eine zweite Rechnung an dieser
/// Stelle wäre eine zweite Wahrheit.</para>
/// </summary>
/// <param name="Name">Der Anzeigename der Einheit; ohne Namen ihre Kennung.</param>
/// <param name="FestEuro">Der vom Maß unabhängige Anteil [€].</param>
/// <param name="KapazitaetKWh">Die Kapazität der Einheit [kWh].</param>
/// <param name="SatzEuroProKWh">Der kapazitätsbezogene Satz [€/kWh].</param>
/// <param name="LeistungKw">Die GRÖSSERE der beiden Richtungsleistungen [kW].</param>
/// <param name="SatzEuroProKw">Der leistungsbezogene Satz [€/kW].</param>
/// <param name="SummeEuro">Die Investition dieser Einheit [€] aus der Formel des Rechenkerns.</param>
public sealed record FlottenInvestitionszeile(string Name, double FestEuro,
                                              double KapazitaetKWh, double SatzEuroProKWh,
                                              double LeistungKw, double SatzEuroProKw,
                                              double SummeEuro);

/// <summary>
/// <b>Die HERLEITUNG der Investition</b> (Auftrag #249, Anwenderentscheid vom 13.09.2026).
///
/// <para><b>Warum sie aus dem Kern kommt.</b> Welche Anteile eine Investition hat und
/// welche der zwei Richtungsleistungen den leistungsbezogenen Anteil trägt, ist eine
/// FACHaussage — dieselbe auf Windows und auf iOS. Die Oberfläche formatiert sie nur.</para>
/// </summary>
public static partial class SpeicherFlottenAnzeigeCtrl
{
    /// <summary>
    /// Die Herleitung der Investition — EINE Zeile je Einheit, in der Reihenfolge der
    /// Konfiguration des Laufs.
    /// </summary>
    /// <remarks>
    /// Ohne Lauf und ohne Einheiten eine leere Liste; die Ansicht zeigt dann nur die Summe,
    /// die ohnehin in der Investitionszeile steht. Gerundet und gruppiert wird hier nicht —
    /// beides ist Sache der Darstellung.
    /// </remarks>
    /// <param name="ergebnis">Der fertig gerechnete Flottenlauf; <c>null</c> = leere Liste.</param>
    /// <returns>Je Einheit eine <see cref="FlottenInvestitionszeile"/>.</returns>
    public static IReadOnlyList<FlottenInvestitionszeile> Investitionsherleitung(
        SpeicherFlottenErgebnis ergebnis)
    {
        List<FlottenEinheit> einheiten = Einheiten(ergebnis);
        if (einheiten.Count == 0) return Array.Empty<FlottenInvestitionszeile>();

        var zeilen = new List<FlottenInvestitionszeile>(einheiten.Count);
        foreach (FlottenEinheit einheit in einheiten)
            zeilen.Add(new FlottenInvestitionszeile(
                string.IsNullOrWhiteSpace(einheit.Name) ? einheit.Id : einheit.Name,
                einheit.InvestitionEuro,
                einheit.KapazitaetKWh,
                einheit.InvestitionEuroProKWh,
                Math.Max(einheit.LadeleistungKw, einheit.EntladeleistungKw),
                einheit.InvestitionEuroProKw,
                FlottenWirtschaftlichkeit.Investition(einheit)));
        return zeilen;
    }
}
