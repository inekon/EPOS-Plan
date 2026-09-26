using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Export;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Knopf „Exportieren (gbXML)…" im Gebäudedialog (Gebäudesimulation G7a, Welle W3): kein Delegat, kein
/// Knopf; eine Zeile ohne Projektkopie (vorläufige Id ab 100000) ist weich gesperrt und nennt den Grund;
/// sonst öffnet der Knopf den Exportdialog als Überlagerung für GENAU die markierte Zeile, mit dem Vermerk,
/// ob sie ungespeicherte Änderungen trägt; nach dem Schreiben steht die Rückmeldung im Banner; Esc schließt
/// den Gebäudedialog nicht, solange der Export steht. Die Beschriftung reicht die Hülle (auf iOS „… und
/// teilen").
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche Beschriftungen.</para>
/// </summary>
public class GebaeudeDialogExportTests : EposBunitContext
{
    private const string KNOPF = "Exportieren (gbXML)…";

    public GebaeudeDialogExportTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static GebaeudeProjektZeile Zeile(int idZ, bool kopie, string name = "Haus 1990") => new()
    {
        IdZ = idZ,
        IdGebaeude = 7,
        IdKatalog = 42,
        Name = name,
        Art = "Einfamilienhaus",
        Wohnflaeche = 150,
        Einheit = "Wohnfläche [m²]",
        Jahresnutzungsgrad = 1,
        HatProjektkopie = kopie
    };

    /// <summary>Die Gaben des eingebetteten Exportdialogs — ein schreibbarer Plan, ein Speicherweg mit Pfad.</summary>
    private static IReadOnlyDictionary<string, object> Exportgaben(List<string>? gespeichert = null, bool geaendert = false)
        => new Dictionary<string, object>
        {
            ["Vorbereiten"] = new Func<string, Task<GebaeudeExportAnsicht>>(_ => Task.FromResult(
                new GebaeudeExportAnsicht(new[] { new GebaeudeExportMeldung(WarnStufe.Hinweis, "Info", "Ohne Ort.", "GEXP_PROT_OHNE_ORT") }, null))),
            ["Speichern"] = new Func<string, Task<GebaeudeExportErgebnis>>(plz =>
            {
                gespeichert?.Add(plz);
                return Task.FromResult(new GebaeudeExportErgebnis(true, false, "Gespeichert: C:\\Probe\\haus.xml (1234 Byte)."));
            }),
            ["GespeicherterStand"] = geaendert,
            ["Entprellung"] = 0,
        };

    private IRenderedComponent<GebaeudeDialog> Aufbauen(
        List<GebaeudeProjektZeile> zeilen,
        Func<GebaeudeProjektZeile, bool, IReadOnlyDictionary<string, object>?>? exportGaben,
        string? knopftext = null,
        Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>>? wohnflaecheGaben = null,
        List<bool>? geschlossen = null)
        => Render<GebaeudeDialog>(p =>
        {
            p.Add(x => x.Zeilen, zeilen)
             .Add(x => x.Katalogzeilen, () => Array.Empty<Katalogfilterzeile>())
             .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
             .Add(x => x.ExportGaben, exportGaben)
             .Add(x => x.WohnflaecheGaben, wohnflaecheGaben)
             .Add(x => x.Geschlossen, b => geschlossen?.Add(b));
            if (knopftext is not null) p.Add(x => x.BtnExportText, knopftext);
        });

    private static IElement Knopf(IRenderedComponent<GebaeudeDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_Exportknopf()
    {
        var ohne = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, kopie: true) }, exportGaben: null);
        Assert.DoesNotContain(KNOPF, ohne.Markup);

        var mit = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, kopie: true) }, (_, _) => Exportgaben());
        Assert.Contains(KNOPF, mit.Markup);
        Assert.Equal("Die Daten des Gebäudes im Projekt als gbXML-Datei ausgeben — Zonen, Bauteilflächen und Schichtaufbauten, ohne Geometrie",
                     Knopf(mit, KNOPF).GetAttribute("title"));
    }

    [Fact]
    public void Der_Knopf_steht_neben_Huelle_und_Zonen_im_Aktionsschlitz()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, kopie: true) }, (_, _) => Exportgaben());
        IElement knopf = Knopf(cut, KNOPF);
        Assert.Contains("epos-gebaeude-export", knopf.ClassName);
        Assert.Equal("epos-leiste", knopf.ParentElement!.ClassName);
        Assert.DoesNotContain("epos-knopf--primaer", knopf.ClassName);
    }

    [Fact]
    public void Eine_ungespeicherte_Zeile_ist_weich_gesperrt_und_nennt_den_Grund()
    {
        int gefragt = 0;
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(100000, kopie: false) },
                           (_, _) => { gefragt++; return Exportgaben(); });

        IElement knopf = Knopf(cut, KNOPF);
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("disabled"));
        const string GRUND = "Das Gebäude ist noch nicht im Projekt gespeichert. Erst mit OK speichern, dann exportieren.";
        Assert.Equal(GRUND, knopf.GetAttribute("title"));

        knopf.Click();

        Assert.Equal(0, gefragt);
        Assert.False(cut.Instance.ExportOffen);
        Assert.Contains(GRUND, cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ohne_Parametersatz_meldet_der_Knopf_den_Grund()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(5, kopie: true) }, (_, _) => null);

        Knopf(cut, KNOPF).Click();

        Assert.False(cut.Instance.ExportOffen);
        Assert.Contains("noch nicht im Projekt gespeichert", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Der_Knopf_oeffnet_den_Export_fuer_genau_die_markierte_Zeile()
    {
        var gereicht = new List<(int IdZ, bool Geaendert)>();
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(4711, kopie: true) },
                           (z, g) => { gereicht.Add((z.IdZ, g)); return Exportgaben(); });

        Knopf(cut, KNOPF).Click();

        Assert.True(cut.Instance.ExportOffen);
        Assert.Equal((4711, false), Assert.Single(gereicht));
        Assert.Contains("Gebäude exportieren (gbXML)", cut.Find("[role=dialog]").TextContent);
        Assert.Single(cut.FindComponents<GebaeudeExportDialog>());
    }

    [Fact]
    public void Nach_dem_Schreiben_steht_die_Rueckmeldung_im_Banner()
    {
        var gespeichert = new List<string>();
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(4711, kopie: true) }, (_, _) => Exportgaben(gespeichert));

        Knopf(cut, KNOPF).Click();
        cut.Find(".epos-gebexport-dialog input[type=checkbox]").Change(true);
        cut.Find(".epos-gebexport-dialog > .epos-leiste button.epos-knopf--primaer").Click();

        Assert.Equal(new[] { "" }, gespeichert);
        Assert.False(cut.Instance.ExportOffen);
        IElement banner = cut.Find(".epos-warnbanner");
        Assert.Contains("epos-warnbanner--erfolg", banner.ClassName);
        Assert.Contains("haus.xml", banner.TextContent);
    }

    [Fact]
    public void Esc_schliesst_den_Gebaeudedialog_nicht_solange_der_Export_steht()
    {
        var geschlossen = new List<bool>();
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(4711, kopie: true) }, (_, _) => Exportgaben(),
                           geschlossen: geschlossen);

        Knopf(cut, KNOPF).Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(geschlossen);
    }

    [Fact]
    public void Eine_geaenderte_Zeile_reicht_den_Vermerk_gespeicherter_Stand()
    {
        var gereicht = new List<bool>();
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(4711, kopie: true) },
                           (_, g) => { gereicht.Add(g); return Exportgaben(geaendert: g); },
                           wohnflaecheGaben: z => new Dictionary<string, object>
                           {
                               ["Gebaeudename"] = z.Name,
                               ["Wert"] = z.Wohnflaeche,
                               ["Jahresnutzungsgrad"] = z.Jahresnutzungsgrad,
                               ["Einheit"] = z.Einheit
                           });

        Knopf(cut, "Fläche und Verbrauch…").Click();
        Assert.True(cut.Instance.WohnflaecheOffen);
        cut.Find("[role=dialog] .epos-leiste button.epos-knopf--primaer").Click();
        Assert.False(cut.Instance.WohnflaecheOffen);

        Knopf(cut, KNOPF).Click();

        Assert.Equal(new[] { true }, gereicht);
        Assert.Contains("Exportiert wird der gespeicherte Stand", cut.Find(".epos-gebexport-gespeichert").TextContent);
    }

    [Fact]
    public void Die_Beschriftung_reicht_die_Huelle_auf_iOS_mit_teilen()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, kopie: true) }, (_, _) => Exportgaben(),
                           knopftext: "Exportieren und teilen (gbXML)…");
        Assert.Contains("Exportieren und teilen (gbXML)…", cut.Markup);
        Assert.DoesNotContain(">" + KNOPF + "<", cut.Markup);
    }
}
