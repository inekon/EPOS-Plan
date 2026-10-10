using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die gemeinsame Probe der Ergebnistabellen der Simulationsreiter (Auftrag TA):
/// Kopf und Wert einer Spalte stehen übereinander. Eine Zahlenspalte trägt
/// <c>epos-simerg-zahl</c> an Kopf UND Zellen (rechtsbündig), eine Textspalte an
/// keinem von beiden (linksbündig). Die Tabellen mit der Hausklasse
/// <c>epos-raster</c> tragen dazu <c>epos-simerg-tabelle</c> (Breite nach Inhalt).
///
/// <para>Verglichen wird die letzte Kopfzeile mit jeder Datenzeile, die ebenso viele
/// Zellen ohne <c>colspan</c> hat — Gruppen- und Hinweiszeilen fallen heraus, die Fehlerzeile
/// des Variantenvergleichs und leere Zellen tragen keine Ausrichtung.
/// Keine Sprachbindung: geprüft werden nur Klassen.</para>
/// </summary>
internal static class Tabellenausrichtung
{
    public const string ZAHL = "epos-simerg-zahl";

    /// <summary>Prüft eine Tabelle und gibt die Zahl der Zahlenspalten zurück.</summary>
    public static int Pruefe(IElement tabelle)
    {
        if (tabelle.ClassList.Contains("epos-raster"))
            Assert.Contains("epos-simerg-tabelle", tabelle.ClassList);

        var kopfzeilen = tabelle.QuerySelectorAll("thead tr");
        Assert.NotEmpty(kopfzeilen);
        IElement[] koepfe = kopfzeilen.Last().Children.Where(c => c.LocalName == "th").ToArray();

        var datenzeilen = tabelle.QuerySelectorAll("tbody tr")
            .Where(z => !z.ClassList.Contains("epos-simerg-fehler"))   // Fehlerzeile: Meldung statt Werte
            .Select(z => z.Children.Where(c => c.LocalName is "td" or "th").ToArray())
            .Where(z => z.Length == koepfe.Length && z.All(c => !c.HasAttribute("colspan")))
            .ToList();
        Assert.NotEmpty(datenzeilen);

        foreach (IElement[] zellen in datenzeilen)
            for (int i = 0; i < koepfe.Length; i++)
                if (zellen[i].TextContent.Trim().Length > 0)   // leere Zelle: keine Ausrichtung
                    Assert.True(koepfe[i].ClassList.Contains(ZAHL) == zellen[i].ClassList.Contains(ZAHL),
                        $"Spalte {i + 1} „{koepfe[i].TextContent.Trim()}“: Kopf "
                        + (koepfe[i].ClassList.Contains(ZAHL) ? "rechts" : "links")
                        + ", Wert „" + zellen[i].TextContent.Trim() + "“ "
                        + (zellen[i].ClassList.Contains(ZAHL) ? "rechts" : "links"));

        return koepfe.Count(k => k.ClassList.Contains(ZAHL));
    }

    /// <summary>Prüft alle Tabellen und gibt je Tabelle die Zahl der Zahlenspalten.</summary>
    public static IReadOnlyList<int> PruefeAlle(IEnumerable<IElement> tabellen)
        => tabellen.Select(Pruefe).ToList();

    /// <summary>Der Textkopf einer Tabelle (Spalte <paramref name="nr"/>) steht links.</summary>
    public static void TextkopfLinks(IElement tabelle, int nr)
    {
        IElement kopf = tabelle.QuerySelectorAll("thead tr").Last().Children
            .Where(c => c.LocalName == "th").ElementAt(nr);
        Assert.DoesNotContain(ZAHL, kopf.ClassList);
    }
}
