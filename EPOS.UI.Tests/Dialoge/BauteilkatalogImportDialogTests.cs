using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// „Bauteilkatalog einlesen…“ (BK-3): Vorschau mit Zahlen je Satzart, Sperre des Übernehmens ohne gültige Vorschau,
/// Übernehmen mit Meldung, Rückweg und der Fall ohne Gaben — deutsch.
/// </summary>
public abstract class BauteilkatalogImportProbe : EposBunitContext
{
    protected BauteilkatalogImportProbe(string kultur) : base(kultur)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    internal static BauteilkatalogVorschauDaten Probe(string? abgelehnt = null) => new(
        abgelehnt, "Probekatalog, Datenstand 2025-06",
        new[]
        {
            new BauteilkatalogVorschauZeile("Baustoffe", 3, 1, 1, 0, 2),
            new BauteilkatalogVorschauZeile("Aufbauten", 5, 0, 0, 0, 0)
        },
        2, 3, new[] { "Baustoffe: ohne Kennung (1)" }, new[] { "Baustoffe.csv MaterialGroupType 12 (1)" },
        "Richtung: 1 von 1", new[] { "Quelle: Probekatalog" });

    internal sealed class Zaehler
    {
        public int Vorschauen, Uebernahmen;
        public bool? Geschlossen;
    }

    internal IRenderedComponent<BauteilkatalogImportDialog> Dialog(Zaehler z, BauteilkatalogVorschauDaten? vorschau = null, bool erfolg = true) =>
        Render<BauteilkatalogImportDialog>(p => p
            .Add(x => x.Pfad, "/probe/Baustoffe.csv")
            .Add(x => x.Waehlen, (Func<string, Task<string?>>)(_ => Task.FromResult<string?>("/probe/Baustoffe.csv")))
            .Add(x => x.Vorschau, (Func<string, BauteilkatalogVorschauDaten>)(_ => { z.Vorschauen++; return vorschau ?? Probe(); }))
            .Add(x => x.Uebernehmen, (Func<string, BauteilkatalogImportErgebnis>)(_ =>
            {
                z.Uebernahmen++;
                return new BauteilkatalogImportErgebnis(erfolg, erfolg ? "Übernommen: 3 neu" : "Nichts übernommen: Probefehler");
            }))
            .Add(x => x.Geschlossen, ok => z.Geschlossen = ok));

    internal static IElement Knopf(IRenderedComponent<BauteilkatalogImportDialog> cut, string text) =>
        cut.FindAll("button").First(b => b.TextContent.Trim() == text);
}

/// <summary>Die Proben unter de-DE.</summary>
public class BauteilkatalogImportDialogTests : BauteilkatalogImportProbe
{
    public BauteilkatalogImportDialogTests() : base("de-DE") { }

    [Fact]
    public void Die_Vorschau_zeigt_Zahlen_je_Satzart_Kennzeichen_Codes_und_Gruende()
    {
        var z = new Zaehler();
        IRenderedComponent<BauteilkatalogImportDialog> cut = Dialog(z);
        Assert.Contains("Bauteilkatalog einlesen", cut.Find("h1").TextContent);
        Assert.Empty(cut.FindAll("table.epos-bki-zahlen"));
        Knopf(cut, "Vorschau").Click();
        Assert.Equal(1, z.Vorschauen);
        Assert.Equal(2, cut.FindAll("table.epos-bki-zahlen tbody tr").Count);
        Assert.Contains("Mit Katalog zusammengeführt", cut.Find("table.epos-bki-zahlen thead").TextContent);
        Assert.Contains("2 Aufbauten sind für die Gebäudesimulation meist nicht geeignet", cut.Markup);
        Assert.Contains("MaterialGroupType 12", cut.Find("ul.epos-bki-codes").TextContent);
        Assert.Contains("ohne Kennung", cut.Find("ul.epos-bki-gruende").TextContent);
        Assert.Contains("Probekatalog, Datenstand 2025-06", cut.Markup);
    }

    [Fact]
    public void Uebernehmen_ist_ohne_Vorschau_weich_gesperrt_und_schreibt_danach_genau_einmal()
    {
        var z = new Zaehler();
        IRenderedComponent<BauteilkatalogImportDialog> cut = Dialog(z);
        IElement ok = Knopf(cut, "Übernehmen");
        Assert.Equal("true", ok.GetAttribute("aria-disabled"));
        ok.Click();
        Assert.Equal(0, z.Uebernahmen);
        Assert.Contains("Erst eine Vorschau", cut.Instance.Meldung);

        Knopf(cut, "Vorschau").Click();
        Knopf(cut, "Übernehmen").Click();
        Assert.Equal(1, z.Uebernahmen);
        Assert.Equal("Übernommen: 3 neu", cut.Instance.Meldung);
        Knopf(cut, "Übernehmen").Click();
        Assert.Equal(1, z.Uebernahmen);

        Knopf(cut, "Schließen").Click();
        Assert.True(z.Geschlossen);
    }

    [Fact]
    public void Eine_abgelehnte_Quelle_sperrt_Uebernehmen_und_Abbrechen_meldet_false()
    {
        var z = new Zaehler();
        IRenderedComponent<BauteilkatalogImportDialog> cut = Dialog(z, Probe("Baustoffe.csv: Pflichtspalte fehlt (UId)"));
        Knopf(cut, "Vorschau").Click();
        Assert.Contains("Pflichtspalte fehlt (UId)", cut.Find(".epos-status--fehler").TextContent);
        Knopf(cut, "Übernehmen").Click();
        Assert.Equal(0, z.Uebernahmen);
        cut.Find(".epos-bauteilkatalog-import").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        Assert.False(z.Geschlossen);
    }

    [Fact]
    public void Ein_Fehler_beim_Uebernehmen_bleibt_offen_mit_Meldung()
    {
        var z = new Zaehler();
        IRenderedComponent<BauteilkatalogImportDialog> cut = Dialog(z, erfolg: false);
        Knopf(cut, "Vorschau").Click();
        Knopf(cut, "Übernehmen").Click();
        Assert.Equal("Nichts übernommen: Probefehler", cut.Instance.Meldung);
        Assert.Null(z.Geschlossen);
        Knopf(cut, "Übernehmen").Click();
        Assert.Equal(2, z.Uebernahmen);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_und_der_Vorschauknopf_ist_gesperrt()
    {
        IRenderedComponent<BauteilkatalogImportDialog> cut = Render<BauteilkatalogImportDialog>();
        Assert.Contains("Bauteilkatalog einlesen", cut.Markup);
        Assert.True(cut.Find("button.epos-bki-vorschauknopf").HasAttribute("disabled"));
    }
}

/// <summary>Dieselben Proben unter en-US: die Texte kommen aus der englischen Ressource.</summary>
public class BauteilkatalogImportDialogEnglischTests : BauteilkatalogImportProbe
{
    public BauteilkatalogImportDialogEnglischTests() : base("en-US") { }

    [Fact]
    public void Die_Texte_sind_englisch()
    {
        var z = new Zaehler();
        IRenderedComponent<BauteilkatalogImportDialog> cut = Dialog(z);
        Assert.Contains("Import building element catalogue", cut.Find("h1").TextContent);
        Knopf(cut, "Preview").Click();
        Assert.Contains("Merged with catalogue", cut.Find("table.epos-bki-zahlen thead").TextContent);
        Assert.Contains("constructions are mostly unsuitable", cut.Markup);
        Assert.NotNull(Knopf(cut, "Import"));
        Assert.NotNull(Knopf(cut, "Close"));
    }
}
