using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// MA-1b: „Modell prüfen…“ in der Kopfleiste IM PROJEKT — für die gewählte Projektzeile, ohne Wahl die erste; ohne Projektzeile
/// weich gesperrt; nicht im Assistenten; das Blatt steht als Überlagerung, sein „Simulation…“ nimmt den Weg des Satzes.
/// </summary>
public partial class GebaeudeDialogTests
{
    private IRenderedComponent<GebaeudeDialog> MitModellpruefung(List<GebaeudeProjektZeile> zeilen, List<string> geoeffnet,
                                                                bool wizard = false,
                                                                Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>?>? bedarf = null)
    {
        var cut = Aufbauen(zeilen: zeilen, wizard: wizard, bedarfGaben: bedarf);
        cut.Render(p => p.Add(x => x.ModellpruefungGaben, z =>
        {
            geoeffnet.Add(z.Name);
            return new ModellpruefungProbeweg().Weg();
        }));
        return cut;
    }

    private static IElement? Pruefknopf(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-zweispalten-bereich--projekt .epos-zweispalten-kopfleiste button.epos-gebaeude-modellpruefung").SingleOrDefault();

    private static List<GebaeudeProjektZeile> GespeicherteZeilen()
        => DreiZeilen().Select(z => { z.HatProjektkopie = true; return z; }).ToList();

    [Fact]
    public void MA1b_Modell_pruefen_steht_in_der_Kopfleiste_und_oeffnet_das_Blatt_fuer_die_erste_Zeile()
    {
        var geoeffnet = new List<string>();
        var cut = MitModellpruefung(GespeicherteZeilen(), geoeffnet);
        IElement? knopf = Pruefknopf(cut);
        Assert.NotNull(knopf);
        Assert.Equal(R.MPB_BTN_KOPF, knopf!.TextContent.Trim());
        Assert.Equal(R.MPB_BTN_KOPF_HINWEIS, knopf.GetAttribute("title"));
        Assert.False(cut.Instance.ModellpruefungOffen);

        knopf.Click();
        Assert.Equal(new[] { "Haus A" }, geoeffnet);
        Assert.True(cut.Instance.ModellpruefungOffen);
        Assert.Equal(5, cut.FindAll(".epos-modellpruefung-gruppe").Count);
        Assert.Equal(R.MPB_TITEL, cut.FindAll(".epos-ueberlagerung-titel").Last().TextContent);
    }

    [Fact]
    public void MA1b_Modell_pruefen_nimmt_die_gewaehlte_Zeile()
    {
        var geoeffnet = new List<string>();
        var zeilen = GespeicherteZeilen();
        var cut = MitModellpruefung(zeilen, geoeffnet);
        cut.FindAll(".epos-zweispalten-bereich--projekt tbody tr")[2].QuerySelector(".epos-zeilenzelle--name, .epos-projektzeile")!.Click();
        Pruefknopf(cut)!.Click();
        Assert.Equal(new[] { "Haus C" }, geoeffnet);
        Assert.Same(zeilen[2], cut.Instance.ModellpruefungZeile);
    }

    [Fact]
    public void MA1b_Ohne_Projektzeile_weich_gesperrt_im_Assistenten_und_ohne_Delegat_kein_Knopf()
    {
        var geoeffnet = new List<string>();
        var leer = MitModellpruefung(new List<GebaeudeProjektZeile>(), geoeffnet);
        IElement knopf = Pruefknopf(leer)!;
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(R.MPB_SPERRE_LEER, knopf.GetAttribute("title"));
        knopf.Click();
        Assert.Empty(geoeffnet);
        Assert.False(leer.Instance.ModellpruefungOffen);

        Assert.Null(Pruefknopf(MitModellpruefung(GespeicherteZeilen(), geoeffnet, wizard: true)));
        Assert.Null(Pruefknopf(Aufbauen(zeilen: GespeicherteZeilen())));
    }

    [Fact]
    public void MA1b_Simulation_im_Blatt_oeffnet_den_Waermebedarf_des_Gebaeudes()
    {
        var geoeffnet = new List<string>();
        var gerechnet = new List<string>();
        var cut = MitModellpruefung(GespeicherteZeilen(), geoeffnet,
                                    bedarf: z => { gerechnet.Add(z.Name); return BedarfMit(z.Name, 10, 5); });
        Pruefknopf(cut)!.Click();
        cut.Find("button.epos-modellpruefung-simulation").Click();
        Assert.Equal(new[] { "Haus A" }, gerechnet);
        Assert.True(cut.Instance.ModellpruefungOffen);   // das Blatt bleibt darunter stehen
    }

    [Fact]
    public void MA1b_Ansicht_ohne_Importquelle_gesperrt_mit_Grund()
    {
        var cut = MitModellpruefung(GespeicherteZeilen(), new List<string>());
        Pruefknopf(cut)!.Click();
        IElement ansicht = cut.FindAll("button.epos-modellpruefung-ansicht")[0];
        Assert.Equal("true", ansicht.GetAttribute("aria-disabled"));
        Assert.Equal(R.MPB_SPERRE_ANSICHT, ansicht.GetAttribute("title"));
    }

    [Theory]
    [InlineData(Pruefsprungziel.Zonen, GebaeudeAnsichtSchluessel.Zonen)]
    [InlineData(Pruefsprungziel.Randbedingung, GebaeudeAnsichtSchluessel.Rand)]
    [InlineData(Pruefsprungziel.Aufbau, GebaeudeAnsichtSchluessel.Aufbau)]
    [InlineData(Pruefsprungziel.Befund, GebaeudeAnsichtSchluessel.Befund)]
    public void MA1b_Sprungziel_waehlt_den_Farbmodus_der_Ansicht(Pruefsprungziel ziel, string erwartet)
        => Assert.Equal(erwartet, GebaeudeDialog.Farbmodusschluessel(ziel));
}

/// <summary>Die Schlüssel der Farbmodi der Gebäudeansicht (Spiegel der Konstanten für die Theorie).</summary>
internal static class GebaeudeAnsichtSchluessel
{
    public const string Zonen = "zonen";
    public const string Rand = "randbedingung";
    public const string Aufbau = "aufbau";
    public const string Befund = "befund";
}
