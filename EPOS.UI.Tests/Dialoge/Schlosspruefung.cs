using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Prüfstand für „Schloss setzen…" / „Schloss aufheben…"</b> (Entscheid AD-Q15) — was
/// die Tests der zehn Verwaltungen gemeinsam brauchen: ein Katalog im Speicher, dessen
/// Schloss ein <see cref="Schlossweg"/> umschaltet, der Knopf in der Auswahlleiste und das
/// „Ja" der Rückfrage.
/// </summary>
internal sealed class Schlosspruefung
{
    /// <summary>Die IDs, die gerade ein Schloss tragen — der „Katalog".</summary>
    public HashSet<int> Gesperrt { get; }

    /// <summary>Jeder Aufruf des Wegs: die IDs und die Richtung.</summary>
    public List<(IReadOnlyList<int> Ids, bool Gesperrt)> Aufrufe { get; } = new();

    public Schlosspruefung(params int[] gesperrt)
    {
        Gesperrt = new HashSet<int>(gesperrt);
    }

    /// <summary>
    /// Der Weg, den die Hülle hereinreicht — er schaltet den Katalog im Speicher; hält der
    /// Prüfling feste Zeilenobjekte (<paramref name="zeilen"/>), trägt er das Schloss auch dort nach.
    /// </summary>
    public Schlossweg Weg(bool lesemodus = false, IReadOnlyList<Katalogfilterzeile>? zeilen = null) => new((ids, gesperrt) =>
    {
        Aufrufe.Add((ids, gesperrt));
        foreach (int id in ids)
        {
            if (gesperrt) Gesperrt.Add(id);
            else Gesperrt.Remove(id);
        }
        if (zeilen is not null) Markieren(zeilen);
        return new SchlossErgebnis(true, "") { Geaendert = ids };
    })
    { Lesemodus = () => lesemodus };

    /// <summary>Die Zeilen mit dem Schloss, das der Katalog gerade trägt.</summary>
    public IReadOnlyList<Katalogfilterzeile> Markieren(IEnumerable<Katalogfilterzeile> zeilen)
    {
        var liste = zeilen.ToList();
        foreach (Katalogfilterzeile z in liste) z.Geschuetzt = Gesperrt.Contains(z.Id);
        return liste;
    }

    /// <summary>Der Knopf der Handlung in der Auswahlleiste (er trägt die Breitenvorlage).</summary>
    public static IElement Knopf<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.Find(".epos-auswahlleiste .epos-auswahlleiste-knopf--zweitext");

    /// <summary>Die gültige Beschriftung des Knopfs (ohne die Breitenvorlage).</summary>
    public static string Beschriftung<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent
        => Knopf(cut).QuerySelector(".epos-auswahlleiste-text")!.TextContent;

    /// <summary>Die Handlungen der Auswahlleiste in ihrer Reihenfolge (gültige Beschriftung).</summary>
    public static IReadOnlyList<string> Handlungen<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll(".epos-auswahlleiste .epos-auswahlleiste-knopf:not(.epos-nur-schmal)")
              .Select(k => (k.QuerySelector(".epos-auswahlleiste-text")?.TextContent ?? k.TextContent).Trim())
              .ToList();

    /// <summary>
    /// Die offene Rückfrage: „Nein" ist die Vorgabe (primär), ein Klick auf „Ja" antwortet.
    /// </summary>
    public static void Ja<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent
    {
        IElement frage = cut.Find(".epos-rueckfrage");
        var knoepfe = frage.QuerySelectorAll(".epos-knopf");
        Assert.Contains("epos-knopf--primaer", knoepfe[1].ClassList);     // Vorgabe Nein
        Assert.DoesNotContain("epos-knopf--primaer", knoepfe[0].ClassList);
        knoepfe[0].Click();
    }

    /// <summary>Der Fragetext der offenen Rückfrage.</summary>
    public static string Frage<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.Find(".epos-rueckfrage .epos-rueckfrage-text").TextContent;

    /// <summary>Das Band „Schloss aufgehoben" im Stammblatt?</summary>
    public static bool Band<T>(IRenderedComponent<T> cut) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll(".epos-stammblatt-schutz--entsperrt").Count == 1;
}
