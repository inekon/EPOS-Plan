using System;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Seiten.Strom;
using Xunit;

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
/// <para>Sie kennt nur Markup, keine Fachlogik: Schritte und Rechenknopf. <b>Den
/// Modus-Umschalter gibt es seit #206 nicht mehr</b> (Anwenderentscheid SD‑E‑8): Die
/// Ansicht rechnet immer die Flotte, ein Einzelspeicher ist eine Flotte mit einer
/// Einheit.</para>
///
/// <para><b>Seit Auftrag #224 sind es FÜNF Blätter</b> (SD‑E‑9, Option A): Station 4
/// ist die Seite „Optimierung", und der RECHENKNOPF steht in ihr statt in der
/// Ablaufleiste. <see cref="Rechenknopf"/> wechselt deshalb zuerst auf dieses Blatt —
/// jeder übernommene Prüffall, der „rechnen" sagt, findet ihn damit weiterhin in
/// einer Zeile.</para>
/// </summary>
internal static class Auslegungshilfe
{
    /// <summary>Die fünf Blätter in der Reihenfolge der Ablaufleiste.</summary>
    private static readonly string[] Reihenfolge =
    {
        AuslegungSchritt.Speicher, AuslegungSchritt.Daten,
        AuslegungSchritt.Betrieb, AuslegungSchritt.Optimierung, AuslegungSchritt.Ergebnis
    };

    /// <summary>Die fünf Knöpfe der Ablaufleiste — seit #224 ohne Aktionsplatz.</summary>
    internal static IElement Schrittknopf(IRenderedComponent<StromspeicherAuslegungSeite> cut,
                                          string schluessel)
    {
        int index = Array.IndexOf(Reihenfolge, schluessel);
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(schluessel), schluessel);
        return cut.FindAll("button.epos-ablaufleiste-knopf")[index];
    }

    /// <summary>
    /// Wechselt auf ein Blatt und <b>wartet auf den gezeichneten Zustand</b>.
    ///
    /// <para><b>Warum gewartet wird.</b> bunit gibt ein Ereignis in den Zeichenverteiler
    /// und kehrt zurück, ohne den Zeichenlauf abzuwarten. Ist der Verteiler gerade belegt
    /// — die Ansicht legt je Tastendruck eine entprellte Vorprüfung auf, deren Fortsetzung
    /// aus dem Fadenvorrat zurückmeldet —, läuft der Klick erst danach ab, und eine Prüfung
    /// unmittelbar hinter dem Aufruf liest den Stand VOR dem Klick. Das ist die lastabhängige
    /// Fallgrube; das Hausmuster dagegen heißt: auf den gezeichneten Zustand warten, nie
    /// sofort prüfen.</para>
    ///
    /// <para>Ein WEICH GESPERRTES Blatt (<c>aria-disabled</c>) wechselt nicht — dort ist der
    /// Klick abgearbeitet, sobald die Sperre am Knopf steht.</para>
    /// </summary>
    internal static void Schritt(IRenderedComponent<StromspeicherAuslegungSeite> cut, string schluessel)
    {
        Schrittknopf(cut, schluessel).Click();
        cut.WaitForAssertion(() => Assert.True(
            string.Equals(cut.Instance.Schritt, schluessel, StringComparison.Ordinal)
            || Schrittknopf(cut, schluessel).HasAttribute("aria-disabled"),
            "Das Blatt „" + schluessel + "“ steht nach dem Klick nicht."));
    }

    /// <summary>
    /// Der Rechenknopf — er steht seit #224 IN Station 4 („Optimierung"), und die
    /// Hilfe wechselt dorthin, bevor sie ihn sucht.
    /// </summary>
    internal static IElement Rechenknopf(IRenderedComponent<StromspeicherAuslegungSeite> cut)
    {
        if (cut.Instance.Schritt != AuslegungSchritt.Optimierung)
            Schritt(cut, AuslegungSchritt.Optimierung);
        return cut.Find(".epos-flotte-optimierung-lauf button");
    }

    /// <summary>Ein Knopf der Seite mit genau diesem Beschriftungstext.</summary>
    internal static IElement Knopf(IRenderedComponent<StromspeicherAuslegungSeite> cut, string text)
        => cut.FindAll("button").Single(b => b.TextContent.Trim() == text);
}
