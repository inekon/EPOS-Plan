using Bunit;
using EPOS.UI.Dialoge.Bedarf;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Prüfhilfe für die Karten des Zapfprofil-Dialogs (Konzept Zapfprofil-Benutzerführung ZB3): Beim Öffnen steht
/// jede Karte außer „Nutzung" zugeklappt, und ihr Inhalt steht nur aufgeklappt im Baum. Wer Felder einer höheren
/// Stufe prüft, klappt die Karten zuerst auf — über ihre Kopfzeile, wie der Anwender.
/// </summary>
internal static class ZapfprofilKarten
{
    /// <summary>Der Klappknopf einer zugeklappten Karte.</summary>
    private const string ZU = ".epos-zapfprofil-karte .epos-klappgruppe-knopf[aria-expanded='false']";

    /// <summary>Klappt jede zugeklappte Karte auf (auch die, die erst eine aufgeklappte Karte zeigt).</summary>
    public static void AlleAuf(IRenderedComponent<ZapfprofilDialog> cut)
    {
        for (int i = 0; i < 40; i++)
        {
            var zu = cut.FindAll(ZU);
            if (zu.Count == 0) return;
            zu[0].Click();
        }
        throw new InvalidOperationException("Die Karten klappen nicht auf.");
    }

    /// <summary>Der Klappknopf der Karte mit der Zusatzklasse <c>epos-zapfprofil-karte--{name}</c>.</summary>
    public static AngleSharp.Dom.IElement Knopf(IRenderedComponent<ZapfprofilDialog> cut, string name)
        => cut.Find(".epos-zapfprofil-karte--" + name + " .epos-klappgruppe-knopf");

    /// <summary>Die Zusammenfassung im Kopf der Karte <c>epos-zapfprofil-karte--{name}</c>.</summary>
    public static string Zusatz(IRenderedComponent<ZapfprofilDialog> cut, string name)
        => cut.Find(".epos-zapfprofil-karte--" + name + " .epos-klappgruppe-zusatz").TextContent.Trim();

    /// <summary>Klappt die Karte <c>epos-zapfprofil-karte--{name}</c> auf, falls sie zu ist.</summary>
    public static void Auf(IRenderedComponent<ZapfprofilDialog> cut, string name)
    {
        if (Knopf(cut, name).GetAttribute("aria-expanded") == "false") Knopf(cut, name).Click();
    }
}
