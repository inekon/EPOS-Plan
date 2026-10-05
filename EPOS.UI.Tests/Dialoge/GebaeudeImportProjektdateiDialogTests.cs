using AngleSharp.Dom;
using Bunit;
using EPOS.Kern.Tests;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Probe 37 — Projektdatei dazuladen</b> (Datenaustauschkonzept 16.4 und 16.7): der Knopf im Kopf nur beim HottCAD-Export
/// aktiv (sonst ausgegraut mit Grund), das Lesen einer zur Laufzeit erzeugten Projektdatei über die echte Hülle mit Bilanz,
/// Zonen der Projektdatei im Baum (Nutzung, Profil als Tooltip, Heizsollwert Tag, <c>data-herkunft</c>), die Räume ohne
/// Gegenstück rechts, die Ablehnung einer Datei ohne Zonentabelle als Banner, die Rückfrage vor dem Verwerfen von Schritten
/// von Hand und „Projektdatei entfernen“; die Texte deutsch und englisch. Das Speichern hält <c>SqprojHuelleTests</c>.
/// </summary>
public class GebaeudeImportProjektdateiDialogTests : EposBunitContext
{
    private readonly List<string> _dateien = new();

    public GebaeudeImportProjektdateiDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
        base.Dispose(disposing);
    }

    private string Merken(string pfad)
    {
        _dateien.Add(pfad);
        return pfad;
    }

    private IRenderedComponent<GebaeudeImportDialog> Bauen(bool hottcad, SqprojProbenErzeuger? probe, bool mitProjektdatei = true,
                                                           GebaeudeImportTexte? texte = null)
    {
        string ifc = hottcad
            ? Merken(SqprojProbenErzeuger.HottcadZonenhaus(Wurzel()))
            : Path.Combine(Wurzel(), "Referenzlaeufe", "Importproben", "ifc4_zonen.ifc");
        string? sq = probe is null ? null : Merken(probe.Schreiben(SqprojProbenErzeuger.TempPfad("dialog")));
        var huelle = new GebaeudeImportHuelle();
        IReadOnlyDictionary<string, object> g = huelle.Gaben();
        return Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, g["Profil"] as GebaeudeImportProfilDaten);
            c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)g["Baualtersklassen"]);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl(ifc, Path.GetFileName(ifc), new FileInfo(ifc).Length))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)g["Lesen"]);
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)g["Zuordnen"]);
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)g["Pruefen"]);
            if (mitProjektdatei)
            {
                c.Add(x => x.ProjektdateiWaehlen, (Func<Task<string?>>)(() => Task.FromResult(sq)));
                c.Add(x => x.ProjektdateiLesen, (Func<string, int, CancellationToken, Task<GebaeudeProjektdateiDaten>>)g["ProjektdateiLesen"]);
                c.Add(x => x.ProjektdateiEntfernen, (Action)g["ProjektdateiEntfernen"]);
            }
            if (texte is not null) c.Add(x => x.Texte, texte);
        });
    }

    /// <summary>Liest die Gebäudedatei — und wartet, bis der Lauf zu Ende ist (die Dateiwahl wieder aktiv).</summary>
    private static void Einlesen(IRenderedComponent<GebaeudeImportDialog> cut)
    {
        IElement Dateiknopf() => cut.FindAll("button").First(k => k.TextContent.Contains(cut.Instance.Texte.DateiKnopf.TrimEnd('…', '.')));
        Dateiknopf().Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zeilen tbody tr")));
        cut.WaitForAssertion(() => Assert.False(Dateiknopf().HasAttribute("disabled")));
    }

    private static IElement Aktion(IRenderedComponent<GebaeudeImportDialog> cut, string aktion)
        => cut.Find("button[data-aktion='" + aktion + "']");

    private static IReadOnlyList<IElement> Herkunftszeilen(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.FindAll("tr.epos-gebimport-planzone[data-herkunft='" + GebaeudePlanschrittArt.PROJEKTDATEI + "']");

    private static IElement Zone(IRenderedComponent<GebaeudeImportDialog> cut, string name)
        => cut.FindAll("tr.epos-gebimport-planzone").Single(z => z.QuerySelector("input.epos-gebimport-zonenname")!.GetAttribute("value") == name);

    private static void Laden(IRenderedComponent<GebaeudeImportDialog> cut)
    {
        Aktion(cut, "projektdatei").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-sqbilanz, .epos-gebimport-sqbanner")));
        cut.WaitForAssertion(() => Assert.False(Aktion(cut, "projektdatei").HasAttribute("disabled")));
    }

    [Fact]
    public void Der_Knopf_ist_nur_beim_HottCAD_Export_aktiv()
    {
        IRenderedComponent<GebaeudeImportDialog> fremd = Bauen(hottcad: false, SqprojProbenErzeuger.Zonenhaus());
        Einlesen(fremd);
        IElement knopf = Aktion(fremd, "projektdatei");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Equal(fremd.Instance.Texte.SqKnopfGrund, knopf.GetAttribute("title"));
        Assert.Equal("Projektdatei dazuladen (.sqproj)…", knopf.TextContent);

        IRenderedComponent<GebaeudeImportDialog> ohne = Bauen(hottcad: true, null, mitProjektdatei: false);
        Einlesen(ohne);
        Assert.Empty(ohne.FindAll("button[data-aktion='projektdatei']"));

        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(hottcad: true, SqprojProbenErzeuger.Zonenhaus());
        Einlesen(cut);
        knopf = Aktion(cut, "projektdatei");
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Null(knopf.GetAttribute("title"));
        Assert.Empty(cut.FindAll("button[data-aktion='projektdatei-entfernen']"));
    }

    [Fact]
    public void Lesen_zeigt_Bilanz_und_die_Zonen_der_Projektdatei_im_Baum()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(hottcad: true,
            SqprojProbenErzeuger.Zonenhaus().Raum("R7", "Galerie", "FO", null, 5.0));
        Einlesen(cut);
        Laden(cut);

        Assert.Contains("6 abgeglichen · 1 nicht abgeglichen · 0 IFC-Räume ohne Gegenstück", cut.Find("dd[data-wert='raeume']").TextContent);
        Assert.Contains("übernommen von 6", cut.Find("dd[data-wert='zonen']").TextContent);
        Assert.EndsWith(".sqproj", cut.Find("dd[data-wert='datei']").TextContent);
        Assert.Contains(cut.FindAll(".epos-gebimport-sqliste li[data-groesse]"), li => li.TextContent.StartsWith("Heiz", StringComparison.Ordinal));
        Assert.Equal(SqprojProtokollKennung("RAUM_OHNE_TREFFER"), cut.Find(".epos-gebimport-sqbanner").GetAttribute("data-kennung"));
        Assert.Contains(GebaeudePlanschrittArt.PROJEKTDATEI, cut.Instance.Planschritte.Select(s => s.Art));

        Assert.True(Herkunftszeilen(cut).Count >= 2);
        IElement eg = Zone(cut, "Simulation EG");
        Assert.Equal(GebaeudePlanschrittArt.PROJEKTDATEI, eg.GetAttribute("data-herkunft"));
        Assert.Equal("WOHNEN", eg.QuerySelector("select.epos-gebimport-nutzung")!.GetAttribute("value"));
        Assert.Equal("Nutzungsprofil 71 nach DIN V 18599", eg.QuerySelector(".epos-gebimport-sqherkunft")!.GetAttribute("title"));
        Assert.Equal("aus Projektdatei", eg.QuerySelector(".epos-gebimport-sqherkunft")!.TextContent);
        Assert.Equal("20 °C", eg.QuerySelector(".epos-gebimport-zonensollwert")!.TextContent.Trim());
        IElement og = Zone(cut, "Simulation OG");
        Assert.Equal("BUERO", og.QuerySelector("select.epos-gebimport-nutzung")!.GetAttribute("value"));
        Assert.Equal("21 °C", og.QuerySelector(".epos-gebimport-zonensollwert")!.TextContent.Trim());

        Assert.Contains("Obergeschoss/Galerie", cut.Find(".epos-gebimport-offenspalte .epos-gebimport-sqohnetreffer").TextContent);
        Assert.Contains("(1)", cut.FindAll(".epos-gebimport-offenspalte .epos-gebimport-offentitel").Last().TextContent);

        // Projektdatei entfernen: Bilanz, Kennzeichen und der Schritt fallen weg, der Regelvorschlag steht wieder.
        Aktion(cut, "projektdatei-entfernen").Click();
        Assert.Empty(cut.FindAll(".epos-gebimport-sqbilanz"));
        Assert.Empty(Herkunftszeilen(cut));
        Assert.Empty(cut.Instance.Planschritte);
        Assert.Empty(cut.FindAll("button[data-aktion='projektdatei-entfernen']"));
        Assert.False(Aktion(cut, "projektdatei").HasAttribute("disabled"));
    }

    [Fact]
    public void Eine_fehlende_Tabelle_steht_als_Banner()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(hottcad: true, SqprojProbenErzeuger.Zonenhaus().Ohne("BmZone"));
        Einlesen(cut);
        Laden(cut);
        IElement banner = cut.Find(".epos-gebimport-sqbanner");
        Assert.Equal(SqprojProtokollKennung("TABELLE_FEHLT"), banner.GetAttribute("data-kennung"));
        Assert.Contains("BmZone", banner.TextContent);
        Assert.Empty(cut.FindAll(".epos-gebimport-sqbilanz"));
        Assert.Empty(Herkunftszeilen(cut));
        Assert.DoesNotContain(GebaeudePlanschrittArt.PROJEKTDATEI, cut.Instance.Planschritte.Select(s => s.Art));
        Assert.NotEmpty(cut.FindAll("button[data-aktion='projektdatei-entfernen']"));
    }

    [Fact]
    public void Schritte_von_Hand_gehen_erst_nach_Rueckfrage_verloren()
    {
        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(hottcad: true, SqprojProbenErzeuger.Zonenhaus());
        Einlesen(cut);
        Aktion(cut, "aufheben").Click();
        cut.FindAll("button").First(k => k.TextContent == "Ja").Click();
        Assert.Single(cut.Instance.Planschritte);

        Laden(cut);
        Assert.True(cut.Instance.ProjektdateiFrageOffen);
        Assert.Contains("1 Schritte von Hand", cut.Markup);
        cut.FindAll("button").First(k => k.TextContent == "Nein").Click();
        Assert.False(cut.Instance.ProjektdateiFrageOffen);
        Assert.Equal(GebaeudePlanschrittArt.AUFHEBEN, Assert.Single(cut.Instance.Planschritte).Art);
        Assert.Contains("in der Datei, noch nicht übernommen", cut.Find("dd[data-wert='zonen']").TextContent);

        Aktion(cut, "projektdatei-uebernehmen").Click();
        Assert.True(cut.Instance.ProjektdateiFrageOffen);
        cut.FindAll("button").First(k => k.TextContent == "Ja").Click();
        Assert.Equal(GebaeudePlanschrittArt.PROJEKTDATEI, Assert.Single(cut.Instance.Planschritte).Art);
        Assert.NotEmpty(Herkunftszeilen(cut));
    }

    [Fact]
    public void Die_Texte_stehen_auch_englisch()
    {
        GebaeudeImportTexte englisch;
        using (new Kulturvorrichtung("en-US")) englisch = new GebaeudeImportTexte();
        Assert.Equal("Add project file (.sqproj)…", englisch.SqKnopf);
        Assert.Equal("Remove project file", englisch.SqEntfernen);
        Assert.Equal("from project file", englisch.SqHerkunft);
        Assert.StartsWith("A project file (.sqproj) can only be added", englisch.SqKnopfGrund);
        var deutsch = new GebaeudeImportTexte();
        Assert.Equal("Projektdatei entfernen", deutsch.SqEntfernen);

        IRenderedComponent<GebaeudeImportDialog> cut = Bauen(hottcad: false, null, texte: englisch);
        Einlesen(cut);
        Assert.Equal("Add project file (.sqproj)…", Aktion(cut, "projektdatei").TextContent);
    }

    private static string SqprojProtokollKennung(string name) => "IMP_SQ_PROT_" + name;

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}
