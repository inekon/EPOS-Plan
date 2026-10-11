using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die gemeinsamen Prüfungen des Schemas „Knopfleiste am Satz"</b> (AW3b, Entscheide KA-E-17 bis KA-E-22) für
/// die Dialoge, deren Projektliste keine eigene Testklasse mit Hilfsmitteln teilt (Ganglinien, Gebäude): die Zeile
/// ist die Wahl, die Knopfleiste steht am gewählten Satz, Schloss… und Löschen sind beim Projektsatz weich gesperrt.
/// </summary>
internal static class KnopfleisteAmSatzPruefung
{
    private const string PROJEKT = ".epos-zweispalten-bereich--projekt";

    internal static IReadOnlyList<IElement> Projektzeilen<T>(IRenderedComponent<T> cut) where T : IComponent
        => cut.Find(PROJEKT).QuerySelectorAll(".epos-projektzeile").ToList();

    internal static IElement AmSatz<T>(IRenderedComponent<T> cut, string klasse) where T : IComponent
        => cut.Find(".epos-zweispalten-satzknoepfe ." + klasse);

    /// <summary>Kein Wahlknopf, je Zeile die Namenszelle als Wahl, das Kästchen bleibt; Bearbeiten nicht im Projektkopf.</summary>
    internal static void ZeileIstWahl<T>(IRenderedComponent<T> cut, int zeilen) where T : IComponent
    {
        var liste = cut.Find(PROJEKT);
        Assert.Empty(liste.QuerySelectorAll(".epos-zeilenwahl, .epos-anlagenwahl, input[type=radio]"));
        Assert.Equal(zeilen, liste.QuerySelectorAll("td.epos-spalte-zeilenzelle > .epos-zeilenzelle").Length);
        Assert.Equal(zeilen, liste.QuerySelectorAll("td .epos-wahlkaestchen").Length);
        Assert.NotNull(cut.Find(PROJEKT + " .epos-zweispalten-inhalt--zeilenwahl"));
        Assert.Null(cut.Find(PROJEKT + " > .epos-zweispalten-kopfleiste").QuerySelector(".epos-knopf--bearbeiten-projekt"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-fussleiste"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzkopf > .epos-zweispalten-bearbeiten"));
    }

    /// <summary>Ein Klick in die Zeile wählt und hebt hervor; die Kästchen bleiben unberührt.</summary>
    internal static void KlickWaehlt<T>(IRenderedComponent<T> cut, int index, string name) where T : IComponent
    {
        Projektzeilen(cut)[index].Click();
        var zeilen = cut.Find(PROJEKT).QuerySelectorAll("tbody tr").ToList();
        Assert.Single(zeilen, tr => tr.ClassList.Contains("epos-zeile--gewaehlt"));
        Assert.Contains("epos-zeile--gewaehlt", zeilen[index].ClassList);
        Assert.Equal("true", zeilen[index].QuerySelector(".epos-zeilenzelle")!.GetAttribute("aria-current"));
        Assert.Equal(name, cut.Find(".epos-zweispalten-satzname").TextContent);
        Assert.Equal(R.AUSWAHL_MARKE_PROJEKTSATZ, cut.Find(".epos-zweispalten-satzzeile .epos-zweispalten-marke--satz").TextContent);
        Assert.Empty(cut.Find(PROJEKT).QuerySelectorAll("td .epos-wahlkaestchen[aria-checked=true]"));
    }

    /// <summary>Die Liste ist ein Tabulatorhalt; ↓, Ende, Pos1 bewegen die Wahl (erste, zweite, letzte, erste Zeile).</summary>
    internal static void PfeiltastenBewegen<T>(IRenderedComponent<T> cut, string erste, string zweite, string letzte) where T : IComponent
    {
        const string HUELLE = PROJEKT + " .epos-raster-huelle";
        Assert.Equal("0", cut.Find(HUELLE).GetAttribute("tabindex"));
        Projektzeilen(cut)[0].Click();
        cut.Find(HUELLE).KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal(zweite, cut.Find(".epos-zweispalten-satzname").TextContent);
        cut.Find(HUELLE).KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.Equal(letzte, cut.Find(".epos-zweispalten-satzname").TextContent);
        cut.Find(HUELLE).KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.Equal(erste, cut.Find(".epos-zweispalten-satzname").TextContent);
    }

    /// <summary>Beim Projektsatz: Schloss… und Löschen fokussierbar, aria-disabled, Grund als Kurztext; Klick meldet.</summary>
    internal static void WeichGesperrt<T>(IRenderedComponent<T> cut, Schlosspruefung schloss,
                                          string loeschen = "epos-knopf--loeschen") where T : IComponent
    {
        foreach (string klasse in new[] { "epos-katalogschloss", loeschen })
        {
            var knopf = AmSatz(cut, klasse);
            Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
            Assert.False(knopf.HasAttribute("disabled"));
            Assert.Equal(R.AUSWAHL_SPERRE_KATALOGSATZ, knopf.GetAttribute("title"));
        }
        AmSatz(cut, loeschen).Click();
        Assert.Contains(cut.FindAll(".epos-warnbanner-text"), b => b.TextContent.Contains(R.AUSWAHL_SPERRE_KATALOGSATZ));
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        AmSatz(cut, "epos-katalogschloss").Click();
        Assert.Empty(schloss.Aufrufe);
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-katalog"));
    }

    /// <summary>Beim Katalogsatz: Schloss… und Löschen wirken, Bearbeiten… ist das des Katalogs.</summary>
    internal static void KatalogsatzFrei<T>(IRenderedComponent<T> cut, string loeschen = "epos-knopf--loeschen") where T : IComponent
    {
        foreach (string klasse in new[] { "epos-katalogschloss", loeschen })
            Assert.False(AmSatz(cut, klasse).HasAttribute("aria-disabled"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt"));
        Assert.NotNull(AmSatz(cut, "epos-knopf--bearbeiten-katalog"));
    }

    /// <summary>Die Schlussleiste: Abbrechen und OK als letzte Knöpfe, ohne Symbol.</summary>
    internal static void Schlussleiste<T>(IRenderedComponent<T> cut, string ok, string abbrechen) where T : IComponent
    {
        var knoepfe = cut.FindAll(".epos-dialog > .epos-leiste button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { abbrechen, ok }, knoepfe.TakeLast(2).ToArray());
        Assert.DoesNotContain(knoepfe, k => k.Contains('❌') || k.Contains('✔'));
    }
}
