using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Bildkarte (iU9-W10a.0e) - ein Bild mit Klickflaechen, der Ersatz fuer das
/// WinForms-Steuerelement KlimazonenKarte.
///
/// <para>Geprueft wird, was den Vorlaeufer ausmachte: Bild und Overlay teilen sich die
/// viewBox, ein Klick waehlt, ein Doppelklick uebernimmt, die Auswahl ist am Pfad
/// erkennbar, und ohne Flaechen bleibt das Bild mit dem Ladefehlertext stehen (die
/// Auswahl laeuft dann ueber die Liste des Elterndialogs, Befund W10-B4).</para>
/// </summary>
public class BildkarteTests : BunitContext
{
    private static IReadOnlyList<Bildkarte.Flaeche> DreiFlaechen() => new[]
    {
        new Bildkarte.Flaeche(1, "M0 0 L10 0 L10 10 Z", "Zone 1 — 1.650 h/a"),
        new Bildkarte.Flaeche(2, "M20 0 L30 0 L30 10 Z", "Zone 2 — 1.800 h/a"),
        new Bildkarte.Flaeche(3, "M40 0 L50 0 L50 10 Z", "Zone 3 — 1.650 h/a")
    };

    [Fact]
    public void Das_Bild_und_das_Overlay_teilen_sich_die_ViewBox()
    {
        var cut = Render<Bildkarte>(p => p
            .Add(x => x.BildUrl, "_content/EPOS.UI/bilder/Zonenkarte_Klimazonen.png")
            .Add(x => x.Bildbeschreibung, "Klimazonen nach DIN 4710")
            .Add(x => x.ViewBox, "0 0 1303.65 1349.50")
            .Add(x => x.Flaechen, DreiFlaechen()));

        var bild = cut.Find("img.epos-bildkarte-bild");
        Assert.Equal("_content/EPOS.UI/bilder/Zonenkarte_Klimazonen.png", bild.GetAttribute("src"));
        Assert.Equal("Klimazonen nach DIN 4710", bild.GetAttribute("alt"));

        var svg = cut.Find("svg.epos-bildkarte-flaechen");
        Assert.Equal("0 0 1303.65 1349.50", svg.GetAttribute("viewBox"));
    }

    /// <summary>
    /// Je Flaeche ein Pfad mit fill-rule="evenodd" - ohne diese Regel verschwaenden
    /// die Lochflaechen der Zonen (der Vorlaeufer nahm dafuer FillMode.Alternate).
    /// Der Kurztext steht als title IM Pfad; dort liest ihn die Sprachausgabe, und der
    /// Browser zeigt ihn wie den ToolTip des Vorlaeufers.
    /// </summary>
    [Fact]
    public void Jede_Flaeche_wird_ein_Pfad_mit_evenodd_und_Kurztext()
    {
        var cut = Render<Bildkarte>(p => p
            .Add(x => x.ViewBox, "0 0 100 100")
            .Add(x => x.Flaechen, DreiFlaechen()));

        var pfade = cut.FindAll("path.epos-bildkarte-flaeche");
        Assert.Equal(3, pfade.Count);
        Assert.Equal("M0 0 L10 0 L10 10 Z", pfade[0].GetAttribute("d"));
        Assert.Equal("evenodd", pfade[0].GetAttribute("fill-rule"));
        Assert.Equal("Zone 1 — 1.650 h/a", pfade[0].GetAttribute("aria-label"));
        Assert.Contains("Zone 2 — 1.800 h/a", cut.Markup);
    }

    [Fact]
    public void Der_Klick_meldet_den_Schluessel_der_Flaeche()
    {
        int? gemeldet = null;

        var cut = Render<Bildkarte>(p => p
            .Add(x => x.ViewBox, "0 0 100 100")
            .Add(x => x.Flaechen, DreiFlaechen())
            .Add(x => x.GewaehltChanged, (int z) => gemeldet = z));

        cut.FindAll("path.epos-bildkarte-flaeche")[1].Click();

        Assert.Equal(2, gemeldet);
    }

    /// <summary>
    /// Der Doppelklick meldet BEIDES - erst die Auswahl, dann die Uebernahme. Der
    /// Vorlaeufer rief AuswahlAnzeigen() und setzte danach DialogResult, die Auswahl
    /// stand also fest, bevor der Dialog schloss (karte_ZoneUebernommen:90-94).
    /// </summary>
    [Fact]
    public void Der_Doppelklick_waehlt_und_uebernimmt()
    {
        int? gewaehlt = null;
        int? uebernommen = null;

        var cut = Render<Bildkarte>(p => p
            .Add(x => x.ViewBox, "0 0 100 100")
            .Add(x => x.Flaechen, DreiFlaechen())
            .Add(x => x.GewaehltChanged, (int z) => gewaehlt = z)
            .Add(x => x.Uebernommen, (int z) => uebernommen = z));

        cut.FindAll("path.epos-bildkarte-flaeche")[2].DoubleClick();

        Assert.Equal(3, gewaehlt);
        Assert.Equal(3, uebernommen);
    }

    [Fact]
    public void Die_gewaehlte_Flaeche_traegt_ihre_Zustandsklasse()
    {
        var cut = Render<Bildkarte>(p => p
            .Add(x => x.ViewBox, "0 0 100 100")
            .Add(x => x.Flaechen, DreiFlaechen())
            .Add(x => x.Gewaehlt, 2));

        var pfade = cut.FindAll("path.epos-bildkarte-flaeche");
        Assert.DoesNotContain("--gewaehlt", pfade[0].ClassName);
        Assert.Contains("epos-bildkarte-flaeche--gewaehlt", pfade[1].ClassName);
        Assert.Equal("true", pfade[1].GetAttribute("aria-pressed"));
        Assert.Equal("false", pfade[0].GetAttribute("aria-pressed"));
    }

    /// <summary>
    /// Der Tastaturweg ist NEU (ein GraphicsPath kannte keinen Fokus): Eingabe und
    /// Leertaste waehlen, jede Flaeche traegt tabindex.
    /// </summary>
    [Fact]
    public void Eingabe_und_Leertaste_waehlen()
    {
        var gemeldet = new List<int>();

        var cut = Render<Bildkarte>(p => p
            .Add(x => x.ViewBox, "0 0 100 100")
            .Add(x => x.Flaechen, DreiFlaechen())
            .Add(x => x.GewaehltChanged, (int z) => gemeldet.Add(z)));

        Assert.Equal("0", cut.FindAll("path.epos-bildkarte-flaeche")[0].GetAttribute("tabindex"));

        cut.FindAll("path.epos-bildkarte-flaeche")[0].KeyDown("Enter");
        cut.FindAll("path.epos-bildkarte-flaeche")[1].KeyDown(" ");
        cut.FindAll("path.epos-bildkarte-flaeche")[2].KeyDown("Escape");   // wirkt nicht

        Assert.Equal(new[] { 1, 2 }, gemeldet);
    }

    /// <summary>
    /// Ohne Flaechen bleibt das BILD stehen und darueber der Ladefehlertext - genau so
    /// verhielt sich der Vorlaeufer, wenn die Zuordnung nicht zustande kam. Die Auswahl
    /// laeuft dann ueber die Liste des Elterndialogs (Befund W10-B4).
    /// </summary>
    [Fact]
    public void Ohne_Flaechen_bleibt_das_Bild_mit_dem_Ladefehlertext()
    {
        var cut = Render<Bildkarte>(p => p
            .Add(x => x.BildUrl, "bild.png")
            .Add(x => x.ViewBox, "0 0 100 100")
            .Add(x => x.LadefehlerText,
                 "Die Klimazonenkarte konnte nicht geladen werden — die Auswahl bleibt " +
                 "über die Liste möglich."));

        Assert.NotNull(cut.Find("img.epos-bildkarte-bild"));
        Assert.Empty(cut.FindAll("svg.epos-bildkarte-flaechen"));
        Assert.Contains("die Auswahl bleibt über die Liste möglich",
                        cut.Find("p.epos-bildkarte-ladefehler").TextContent);
    }

    /// <summary>Ohne Flaechen UND ohne Text steht nur das Bild da.</summary>
    [Fact]
    public void Ohne_Flaechen_und_ohne_Text_steht_nur_das_Bild()
    {
        var cut = Render<Bildkarte>(p => p.Add(x => x.BildUrl, "bild.png"));

        Assert.NotNull(cut.Find("img.epos-bildkarte-bild"));
        Assert.Empty(cut.FindAll("svg.epos-bildkarte-flaechen"));
        Assert.Empty(cut.FindAll("p.epos-bildkarte-ladefehler"));
    }
}
