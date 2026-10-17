using System.IO;
using System.Text.RegularExpressions;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using EPOS.UI.Seiten.Assistent;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Das linke Band des Projektassistenten nach dem Anwenderwunsch vom 06.10.2026:
/// Die Projektliste füllt die Höhe des Bandes, und über den Kacheln steht der Steckbrief
/// des markierten Projekts. bunit misst keine Höhe — die Höhe wird als STILREGEL geprüft,
/// die Struktur am Markup.
/// </summary>
public class AssistentSteckbriefTests : EposBunitContext
{
    public AssistentSteckbriefTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static IReadOnlyList<ProjektKopfZeile> Projekte() => new[]
    {
        new ProjektKopfZeile(1030, "Referenz BHKW-Kaskade"),
        new ProjektKopfZeile(1007, "Laurentiuskirche")
    };

    private static ProjektSteckbrief Voll(int id) => new(
        id, id == 1007 ? "Laurentiuskirche" : "Referenz BHKW-Kaskade",
        Beschreibung: "Kirche mit Gemeindehaus",
        Kunde: "Gemeinde",
        Bearbeiter: "PE",
        Erstellt: new DateTime(2026, 8, 19, 9, 5, 0),
        Geaendert: new DateTime(2026, 10, 6, 14, 30, 0),
        Klimaregion: "München",
        Varianten: 2,
        LetzteSimulation: new DateTime(2026, 8, 30, 6, 11, 59));

    private IRenderedComponent<AssistentSeite> Zeige(Func<int, ProjektSteckbrief?>? laden)
        => Render<AssistentSeite>(p => p
            .Add(x => x.Betriebsart, 1)
            .Add(x => x.SeiteGaben, new Func<int, IReadOnlyDictionary<string, object>?>(_ => new Dictionary<string, object>()))
            .Add(x => x.SeiteAktiv, nr => nr <= 1)
            .Add(x => x.Projekte, Projekte())
            .Add(x => x.SteckbriefLaden, laden));

    [Fact]
    public void Ohne_Markierung_steht_kein_Steckbrief()
    {
        var cut = Zeige(Voll);

        Assert.Single(cut.FindAll(".epos-assistent-band > .epos-projektliste"));
        Assert.Empty(cut.FindAll(".epos-projektsteckbrief"));
    }

    [Fact]
    public void Bei_Markierung_erscheint_der_Steckbrief_ueber_den_Kacheln_nicht_im_Band()
    {
        var geholt = new List<int>();
        var cut = Zeige(id => { geholt.Add(id); return Voll(id); });

        cut.Find(".epos-assistent-band tbody tr button").Click();

        var block = cut.Find(".epos-assistent-inhalt .epos-projektsteckbrief");
        Assert.Equal("Laurentiuskirche", block.QuerySelector(".epos-projektsteckbrief-name")!.TextContent);
        Assert.Null(block.PreviousElementSibling);
        Assert.Empty(cut.FindAll(".epos-assistent-band .epos-projektsteckbrief"));

        // Im Band bleiben nur Ueberschrift, Liste und „Projekt öffnen".
        var band = cut.Find(".epos-assistent-band");
        Assert.Contains(band.Children, k => k.ClassName == "epos-projektliste");
        Assert.Contains("epos-leiste", band.LastElementChild!.ClassName);

        // Je Projekt EINMAL geholt, auch wenn neu gezeichnet wird.
        cut.Render();
        Assert.Equal(new[] { 1007 }, geholt);

        // Eine andere Markierung holt das andere Projekt.
        cut.FindAll(".epos-assistent-band tbody tr button")[1].Click();
        Assert.Equal("Referenz BHKW-Kaskade",
            cut.Find(".epos-assistent-inhalt .epos-projektsteckbrief-name").TextContent);
        Assert.Equal(new[] { 1007, 1030 }, geholt);
    }

    [Fact]
    public void Ohne_Ladeweg_bleibt_der_Steckbrief_weg_und_die_Markierung_wirkt()
    {
        var cut = Zeige(null);

        cut.Find(".epos-assistent-band tbody tr button").Click();

        Assert.Empty(cut.FindAll(".epos-projektsteckbrief"));
        Assert.False(cut.Find(".epos-assistent-band .epos-leiste button").HasAttribute("disabled"));
    }

    [Fact]
    public void Der_volle_Steckbrief_zeigt_alle_Felder_in_deutscher_Kultur()
    {
        var cut = Render<ProjektSteckbriefBlock>(p => p.Add(x => x.Steckbrief, Voll(1007)));

        var paare = Paare(cut);
        Assert.Equal(new[]
        {
            "Beschreibung", "Kunde", "Bearbeiter", "Klimaregion", "Varianten",
            "Erstellt am", "Geändert am", "Letzte Simulation"
        }, paare.Keys);
        Assert.Equal("19.08.2026 09:05", paare["Erstellt am"]);
        Assert.Equal("06.10.2026 14:30", paare["Geändert am"]);
        Assert.Equal("30.08.2026 06:11", paare["Letzte Simulation"]);
        Assert.Equal("2", paare["Varianten"]);
        Assert.Equal("Projektdaten", cut.Find("section").GetAttribute("aria-label"));
    }

    [Fact]
    public void Fehlende_Felder_fehlen_statt_leer_zu_stehen()
    {
        var karg = new ProjektSteckbrief(1023, "Wöhler - Test1", Bearbeiter: "  ",
            Erstellt: new DateTime(2026, 8, 2), StammName: "Wöhler");

        var cut = Render<ProjektSteckbriefBlock>(p => p.Add(x => x.Steckbrief, karg));

        var paare = Paare(cut);
        Assert.Equal(new[] { "Variante von", "Erstellt am" }, paare.Keys);
        Assert.Equal("Wöhler", paare["Variante von"]);
        Assert.Empty(cut.FindAll("dd:empty"));
    }

    [Fact]
    public void Ohne_Steckbrief_zeichnet_der_Baustein_nichts()
    {
        var cut = Render<ProjektSteckbriefBlock>();
        Assert.Equal("", cut.Markup.Trim());
    }

    [Fact]
    public void Die_Liste_im_Band_fuellt_die_Hoehe_statt_einer_festen_Hoechsthoehe()
    {
        string css = Hausblatt();

        string band = Regel(css, ".epos-assistentseite .epos-assistent-band");
        Assert.Contains("position: sticky", band);
        Assert.Contains("100dvh", band);

        string liste = Regel(css, ".epos-assistentseite .epos-assistent-band > .epos-projektliste");
        Assert.Contains("flex: 1 1 0", liste);
        Assert.Contains("min-height: 14rem", liste);

        string huelle = Regel(css, ".epos-assistentseite .epos-assistent-band > .epos-projektliste > .epos-raster-huelle");
        Assert.Contains("max-height: none", huelle);
        Assert.Contains("flex: 1 1 0", huelle);
    }

    private static Dictionary<string, string> Paare(IRenderedComponent<ProjektSteckbriefBlock> cut)
    {
        var dt = cut.FindAll("dt");
        var dd = cut.FindAll("dd");
        Assert.Equal(dt.Count, dd.Count);
        var paare = new Dictionary<string, string>();
        for (int i = 0; i < dt.Count; i++) paare.Add(dt[i].TextContent, dd[i].TextContent);
        return paare;
    }

    internal static string Regel(string css, string selektor)
    {
        Match m = Regex.Match(css, "(?m)^" + Regex.Escape(selektor) + @"\s*\{([^}]*)\}");
        Assert.True(m.Success, "Regel fehlt: " + selektor);
        return m.Groups[1].Value;
    }

    internal static string Hausblatt()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));
    }
}

/// <summary>Derselbe Steckbrief unter en-US: Beschriftungen aus dem Bündel, Datum nach Kultur.</summary>
public class AssistentSteckbriefEnglischTests : EposBunitContext
{
    public AssistentSteckbriefEnglischTests() : base("en-US") { }

    [Fact]
    public void Datum_und_Beschriftung_folgen_der_englischen_Kultur()
    {
        var s = new ProjektSteckbrief(1, "P", Geaendert: new DateTime(2026, 10, 6, 14, 30, 0));
        var texte = new ProjektSteckbriefTexte { Titel = "Project details", Geaendert = "Modified" };

        var cut = Render<ProjektSteckbriefBlock>(p => p
            .Add(x => x.Steckbrief, s)
            .Add(x => x.Texte, texte));

        Assert.Equal("Modified", cut.Find("dt").TextContent);
        string wert = cut.Find("dd").TextContent;
        Assert.StartsWith("10/6/2026 2:30", wert);
        Assert.EndsWith("PM", wert);
        Assert.Equal("Project details", cut.Find("section").GetAttribute("aria-label"));
    }
}
