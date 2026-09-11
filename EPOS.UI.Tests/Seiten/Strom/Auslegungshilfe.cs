using System;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Seiten.Strom;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// Die gemeinsame Handreichung für die Prüfstände der Ansicht
/// „Stromspeicher-Auslegung" (Paket P3, Auftrag #192).
///
/// <para><b>Warum es sie gibt.</b> Die abgelösten Dialoge
/// <c>SpeicherFlottenDialog</c> und <c>SpeicherOptimierungDialog</c> hatten ihre
/// Bedienelemente auf EINEM Blatt; die Ansicht verteilt sie über die fünf Schritte der
/// Ablaufleiste. Jeder übernommene Prüffall muss deshalb erst auf sein Blatt gehen —
/// und das soll er in EINER Zeile tun können, nicht über drei Suchen im Markup.</para>
///
/// <para>Sie kennt nur Markup, keine Fachlogik: Schritte, Modus, Rechenknopf.</para>
/// </summary>
internal static class Auslegungshilfe
{
    /// <summary>Die vier Blätter in der Reihenfolge der Ablaufleiste.</summary>
    private static readonly string[] Reihenfolge =
    {
        AuslegungSchritt.Speicher, AuslegungSchritt.Daten,
        AuslegungSchritt.Betrieb, AuslegungSchritt.Ergebnis
    };

    /// <summary>Die Knöpfe der Ablaufleiste — drei vor dem Rechenknopf, einer dahinter.</summary>
    internal static IElement Schrittknopf(IRenderedComponent<StromspeicherAuslegungSeite> cut,
                                          string schluessel)
    {
        int index = Array.IndexOf(Reihenfolge, schluessel);
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(schluessel), schluessel);
        return cut.FindAll("button.epos-ablaufleiste-knopf")[index];
    }

    /// <summary>Wechselt auf ein Blatt.</summary>
    internal static void Schritt(IRenderedComponent<StromspeicherAuslegungSeite> cut, string schluessel)
        => Schrittknopf(cut, schluessel).Click();

    /// <summary>Der Rechenknopf (Schritt 4).</summary>
    internal static IElement Rechenknopf(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.Find("button.epos-ablaufleiste-rechnen");

    /// <summary>Stellt den Modus-Umschalter um.</summary>
    internal static void Modus(IRenderedComponent<StromspeicherAuslegungSeite> cut, AuslegungModus modus)
        => cut.Find(".epos-ablaufleiste-modus select").Change(((int)modus).ToString());

    /// <summary>Ein Knopf der Seite mit genau diesem Beschriftungstext.</summary>
    internal static IElement Knopf(IRenderedComponent<StromspeicherAuslegungSeite> cut, string text)
        => cut.FindAll("button").Single(b => b.TextContent.Trim() == text);
}
