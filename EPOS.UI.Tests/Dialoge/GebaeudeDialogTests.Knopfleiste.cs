using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Standards;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// GD-6a (Anwenderwünsche 12.10.2026): „Simulation…" in der Kopfleiste des Abschnitts IM PROJEKT — ohne Kästchen
/// alle Gebäude, sonst die angekreuzten; mehrere als Liste je Gebäude mit Sprung in den Einzelbedarf; die
/// Mehrfachwahl der Projektzeilen lässt die Satzwahl des Katalogs stehen; „Baustoff-Zuordnungen…" steht nicht mehr
/// im Gebäudedialog, sondern im Import und im Editor.
/// </summary>
public partial class GebaeudeDialogTests
{
    private static readonly string[] DreiNamen = { "Haus A", "Haus B", "Haus C" };

    private static List<GebaeudeProjektZeile> DreiZeilen()
        => DreiNamen.Select((n, i) => Zeile(i + 1, n)).ToList();

    /// <summary>Ein Parametersatz des Wärmebedarfs mit Heizwärme und Höchstlast — wie die Hülle ihn reicht.</summary>
    private static IReadOnlyDictionary<string, object> BedarfMit(string name, double mwh, double kw)
        => new Dictionary<string, object> { ["Daten"] = new GebaeudeBedarfDaten { Name = name, HeizwaermeMwh = mwh, MaxLastKw = kw } };

    private static IElement? Kopfknopf(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-zweispalten-bereich--projekt .epos-zweispalten-kopfleiste button.epos-gebaeude-simulation-projekt")
              .SingleOrDefault();

    private static IReadOnlyList<IElement> Projektkaestchen(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-zweispalten-bereich--projekt tbody .epos-wahlkaestchen");

    private static string Format(string vorlage, int zahl) => string.Format(CultureInfo.CurrentCulture, vorlage, zahl);

    [Fact]
    public void GD6a_Simulation_steht_in_der_Kopfleiste_und_zaehlt_alle_oder_die_gewaehlten()
    {
        var cut = Aufbauen(zeilen: DreiZeilen(), bedarfGaben: z => BedarfMit(z.Name, 10, 5));

        IElement? knopf = Kopfknopf(cut);
        Assert.NotNull(knopf);
        Assert.Equal(Format(R.GEBSIM_BTN_ALLE, 3), knopf!.TextContent.Trim());
        Assert.Equal(R.GEBSIM_BTN_HINWEIS, knopf.GetAttribute("title"));

        Projektkaestchen(cut)[0].Click();
        Projektkaestchen(cut)[2].Click();
        Assert.Equal(Format(R.GEBSIM_BTN_GEWAEHLT, 2), Kopfknopf(cut)!.TextContent.Trim());

        // Das Kopfkästchen wählt alle - der Zähltext folgt.
        cut.Find(".epos-zweispalten-bereich--projekt thead .epos-wahlkaestchen--kopf").Click();
        Assert.Equal(Format(R.GEBSIM_BTN_GEWAEHLT, 3), Kopfknopf(cut)!.TextContent.Trim());
    }

    [Fact]
    public void GD6a_Ohne_Delegat_und_im_Assistenten_steht_kein_Kopfknopf()
    {
        Assert.Null(Kopfknopf(Aufbauen(zeilen: DreiZeilen())));
        Assert.Null(Kopfknopf(Aufbauen(zeilen: DreiZeilen(), wizard: true, bedarfGaben: _ => new Dictionary<string, object>())));
    }

    [Fact]
    public void GD6a_Die_Katalogverwaltung_traegt_keinen_Simulationsknopf()
    {
        var admin = Render<GebaeudeAdminDialog>();
        Assert.Empty(admin.FindAll("button.epos-gebaeude-simulation-projekt"));
        Assert.Empty(admin.FindAll("button.epos-gebaeude-simulation"));
    }

    [Fact]
    public void GD6a_Ein_Gebaeude_oeffnet_den_Waermebedarf_direkt()
    {
        var cut = Aufbauen(zeilen: new List<GebaeudeProjektZeile> { Zeile(1, "Haus A") },
                           bedarfGaben: z => BedarfMit(z.Name, 12, 6));

        Assert.Equal(Format(R.GEBSIM_BTN_ALLE, 1), Kopfknopf(cut)!.TextContent.Trim());
        Kopfknopf(cut)!.Click();

        Assert.True(cut.Instance.BedarfOffen);
        Assert.False(cut.Instance.SimulationslisteOffen);
    }

    [Fact]
    public void GD6a_Mehrere_Gebaeude_rechnen_je_Zeile_und_stehen_als_Liste_mit_Sprung()
    {
        var gerechnet = new List<string>();
        var cut = Aufbauen(zeilen: DreiZeilen(),
                           bedarfGaben: z =>
                           {
                               gerechnet.Add(z.Name);
                               return z.Name == "Haus B" ? null : BedarfMit(z.Name, z.Name == "Haus A" ? 10 : 20, 5);
                           });

        Kopfknopf(cut)!.Click();

        Assert.Equal(DreiNamen, gerechnet);
        Assert.True(cut.Instance.SimulationslisteOffen);
        Assert.False(cut.Instance.BedarfOffen);
        Assert.Equal(DreiNamen, cut.Instance.Simulationsliste.Select(e => e.Name));
        Assert.Equal(10, cut.Instance.Simulationsliste[0].HeizwaermeMwh);
        Assert.Null(cut.Instance.Simulationsliste[1].HeizwaermeMwh);
        Assert.Equal(cut.Instance.MeldungKeinBedarf, cut.Instance.Simulationsliste[1].Befund);

        // Die Liste zeigt drei Zeilen, zwei mit Sprung; die Summe steht im Fuß.
        Assert.Equal(3, cut.FindAll(".epos-gebaeude-simulationsliste tbody tr").Count);
        IReadOnlyList<IElement> spruenge = cut.FindAll("button.epos-gebaeude-simulationsliste-oeffnen");
        Assert.Equal(2, spruenge.Count);
        Assert.Single(cut.FindAll(".epos-gebaeude-simulationsliste tfoot tr"));

        spruenge[1].Click();
        Assert.True(cut.Instance.BedarfOffen);
        Assert.True(cut.Instance.SimulationslisteOffen);   // die Liste bleibt darunter stehen
    }

    [Fact]
    public void GD6a_Nur_die_angekreuzten_Gebaeude_werden_gerechnet()
    {
        var gerechnet = new List<string>();
        var cut = Aufbauen(zeilen: DreiZeilen(), bedarfGaben: z => { gerechnet.Add(z.Name); return BedarfMit(z.Name, 1, 1); });

        Projektkaestchen(cut)[1].Click();
        Projektkaestchen(cut)[2].Click();
        Kopfknopf(cut)!.Click();

        Assert.Equal(new[] { "Haus B", "Haus C" }, gerechnet);
        Assert.Equal(2, cut.Instance.Simulationsliste.Count);
    }

    [Fact]
    public void GD6a_Die_Mehrfachwahl_der_Projektzeilen_laesst_die_Satzwahl_des_Katalogs_stehen()
    {
        var cut = Aufbauen(zeilen: DreiZeilen(), bedarfGaben: _ => new Dictionary<string, object>());

        KatalogWaehlen(cut, "Hotel Sonne");
        Katalogfilterzeile? katalogsatz = cut.Instance.Katalogzeile;
        Assert.NotNull(katalogsatz);

        Projektkaestchen(cut)[0].Click();
        Projektkaestchen(cut)[1].Click();

        Assert.Equal(2, cut.Instance.ProjektWahl.Anzahl);
        Assert.Null(cut.Instance.Gewaehlt);
        Assert.Same(katalogsatz, cut.Instance.Katalogzeile);
    }

    [Fact]
    public void GD6a_Baustoff_Zuordnungen_stehen_nicht_mehr_im_Gebaeudedialog()
    {
        var cut = Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, DreiZeilen())
            .Add(x => x.Katalogzeilen, () => Katalog())
            .Add(x => x.Katalogprofil, Profil())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.StammDetail, n => new GebaeudeStammDetail(n, "Einfamilienhaus", "Katalogtext", "150,00"))
            .Add(x => x.BaustoffzuordnungenGaben, () => new Dictionary<string, object>()));

        Assert.Empty(cut.FindAll("button.epos-gebaeude-baustoffzuordnungen"));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == R.GEB_BTN_BAUSTOFFZUORDNUNGEN);
        GebaeudeDialogProbe.SatzOeffnen(cut);
        Assert.Empty(cut.FindAll("button.epos-gebaeude-baustoffzuordnungen"));
    }

    [Fact]
    public void GD6a_Der_Dialog_reicht_die_Baustoff_Zuordnungen_an_den_Editor_durch_nicht_im_Assistenten()
    {
        Func<IReadOnlyDictionary<string, object>> gaben = () => new Dictionary<string, object>();
        foreach (bool wizard in new[] { false, true })
        {
            var cut = Render<GebaeudeDialog>(p => p
                .Add(x => x.Zeilen, DreiZeilen())
                .Add(x => x.Wizard, wizard)
                .Add(x => x.Katalogzeilen, () => Katalog())
                .Add(x => x.Katalogprofil, Profil())
                .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
                .Add(x => x.StammDetail, n => new GebaeudeStammDetail(n, "Einfamilienhaus", "Katalogtext", "150,00"))
                .Add(x => x.KatalogGaben, _ => new Dictionary<string, object>())
                .Add(x => x.BaustoffzuordnungenGaben, gaben));

            cut.FindAll("button").First(b => b.TextContent.Trim() == "Gebäude in DB neu...").Click();
            Assert.True(cut.Instance.KatalogeditorOffen);
            var editor = cut.FindComponent<GebaeudeKatalogDialog>();
            if (wizard) Assert.Null(editor.Instance.BaustoffzuordnungenGaben);
            else Assert.Same(gaben, editor.Instance.BaustoffzuordnungenGaben);
        }
    }
}
