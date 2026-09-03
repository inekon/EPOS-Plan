using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der Reiter „Berichte &amp; Kosten" (iU9-W5.6), Vorbild
/// <c>Views/BerichteKosten/UcBerichteKosten</c> (810 Z., K4).
///
/// <para>Soll: die senkrechte Navigation mit vier Einträgen, die Kopfzeile mit
/// Titel und Stammnamen, genau EINE sichtbare Seite, der Hinweis statt der
/// Seite ohne Stammprojekt und der Projektwechsel über den
/// <c>SeitenZustand</c> — ohne Neuaufbau der Hülle.</para>
/// </summary>
public class BerichteKostenSeiteTests : BunitContext
{
    public BerichteKostenSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private readonly List<string> _gefragt = new();

    /// <summary>Ein Parametersatz je Seite; die Gruppenseiten nur mit Stamm.</summary>
    private IReadOnlyDictionary<string, object>? Gaben(string seite, bool mitStamm = true)
    {
        _gefragt.Add(seite);

        if (!mitStamm && (seite == BerichteKostenSeite.SEITE_WIRTSCHAFT
                          || seite == BerichteKostenSeite.SEITE_BERICHT))
            return null;

        return seite switch
        {
            BerichteKostenSeite.SEITE_UEBERSICHT => new Dictionary<string, object>
            {
                ["Laden"] = new Func<UebersichtStand>(() => new UebersichtStand())
            },
            BerichteKostenSeite.SEITE_KOSTEN => new Dictionary<string, object>
            {
                ["Laden"] = new Func<KostenStand>(() => new KostenStand
                {
                    Projektzeile = "Projekt: Musterhaus",
                    Kacheln = new[]
                    {
                        new KachelZeile { Titel = "Investition" },
                        new KachelZeile { Titel = "Betrieb" },
                        new KachelZeile { Titel = "Energie" }
                    }
                })
            },
            BerichteKostenSeite.SEITE_WIRTSCHAFT => new Dictionary<string, object>
            {
                ["Laden"] = new Func<WirtschaftlichkeitStand>(() => new WirtschaftlichkeitStand())
            },
            _ => new Dictionary<string, object>
            {
                ["Laden"] = new Func<BerichtStand>(() => new BerichtStand())
            }
        };
    }

    private IRenderedComponent<BerichteKostenSeite> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<BerichteKostenSeite>>? mehr = null,
        bool mitStamm = true)
    {
        _gefragt.Clear();
        return Render<BerichteKostenSeite>(p =>
        {
            p.Add(x => x.SeitenGaben, (string s) => Gaben(s, mitStamm));
            p.Add(x => x.Kopf, (string s) => "Kopf " + s + "  ·  Musterhaus");
            mehr?.Invoke(p);
        });
    }

    private static IReadOnlyList<IElement> Navknoepfe(IRenderedComponent<BerichteKostenSeite> cut)
        => cut.FindAll(".epos-navigation-knopf");

    // =====================================================================
    // Aufbau
    // =====================================================================

    [Fact]
    public void Die_Navigation_traegt_vier_Eintraege_in_der_Reihenfolge_des_Vorlaeufers()
    {
        var cut = Zeige();

        var knoepfe = Navknoepfe(cut);
        Assert.Equal(4, knoepfe.Count);
        Assert.Contains("Übersicht", knoepfe[0].TextContent);
        Assert.Contains("Kosten", knoepfe[1].TextContent);
        Assert.Contains("Wirtschaftlichkeit", knoepfe[2].TextContent);
        Assert.Contains("Bericht", knoepfe[3].TextContent);
    }

    [Fact]
    public void Die_Navigation_meldet_sich_als_Reiterleiste()
    {
        var cut = Zeige();

        Assert.Equal("tablist", cut.Find(".epos-navigation-liste").GetAttribute("role"));
        Assert.Equal("tab", Navknoepfe(cut)[0].GetAttribute("role"));
        Assert.Equal("true", Navknoepfe(cut)[0].GetAttribute("aria-selected"));
        Assert.Equal("tabpanel", cut.Find(".epos-navigation-inhalt").GetAttribute("role"));
    }

    [Fact]
    public void Beim_Aufbau_steht_die_Uebersicht_vorn()
    {
        var cut = Zeige();

        Assert.Equal(BerichteKostenSeite.SEITE_UEBERSICHT, cut.Instance.AktiveSeite);
        Assert.Equal(new[] { BerichteKostenSeite.SEITE_UEBERSICHT }, _gefragt);
    }

    [Fact]
    public void Die_Startseite_laesst_sich_vorgeben()
    {
        var cut = Zeige(p => p.Add(x => x.Startseite, BerichteKostenSeite.SEITE_BERICHT));

        Assert.Equal(BerichteKostenSeite.SEITE_BERICHT, cut.Instance.AktiveSeite);
    }

    [Fact]
    public void Ein_unbekannter_Schluessel_faellt_auf_die_Uebersicht_zurueck()
    {
        var cut = Zeige(p => p.Add(x => x.Startseite, "GIBTESNICHT"));

        Assert.Equal(BerichteKostenSeite.SEITE_UEBERSICHT, cut.Instance.AktiveSeite);
    }

    [Fact]
    public void Die_Kopfzeile_nennt_Seite_und_Stammnamen()
    {
        var cut = Zeige();

        Assert.Contains("Musterhaus", cut.Find(".epos-navigation-kopf").TextContent);
    }

    // =====================================================================
    // Umschalten — eine Seite zur Zeit
    // =====================================================================

    [Fact]
    public void Der_Klick_stellt_die_Seite_um_und_holt_ihre_Gaben()
    {
        var cut = Zeige();

        Navknoepfe(cut)[1].Click();

        Assert.Equal(BerichteKostenSeite.SEITE_KOSTEN, cut.Instance.AktiveSeite);
        Assert.Contains(BerichteKostenSeite.SEITE_KOSTEN, _gefragt);
        Assert.Contains("Projekt: Musterhaus", cut.Markup);
    }

    /// <summary>
    /// Die schweren Seiten entstehen erst beim ersten Aufruf — ein nicht
    /// gewählter Zweig wird gar nicht gezeichnet.
    /// </summary>
    [Fact]
    public void Es_ist_immer_nur_eine_Seite_gezeichnet()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-kennzahlkachel"));   // Kosten: drei Karten

        Navknoepfe(cut)[1].Click();
        Assert.Equal(3, cut.FindAll(".epos-kennzahlkachel").Count);

        Navknoepfe(cut)[3].Click();
        Assert.Empty(cut.FindAll(".epos-kennzahlkachel"));
    }

    [Fact]
    public void Pfeil_ab_und_auf_wandern_durch_die_Navigation()
    {
        var cut = Zeige();

        cut.Find(".epos-navigation-liste").KeyDown("ArrowDown");
        Assert.Equal(BerichteKostenSeite.SEITE_KOSTEN, cut.Instance.AktiveSeite);

        cut.Find(".epos-navigation-liste").KeyDown("ArrowUp");
        Assert.Equal(BerichteKostenSeite.SEITE_UEBERSICHT, cut.Instance.AktiveSeite);

        cut.Find(".epos-navigation-liste").KeyDown("End");
        Assert.Equal(BerichteKostenSeite.SEITE_BERICHT, cut.Instance.AktiveSeite);
    }

    [Fact]
    public void Nur_der_aktive_Knopf_steht_im_Tabulatorzyklus()
    {
        var cut = Zeige();

        Assert.Equal("0", Navknoepfe(cut)[0].GetAttribute("tabindex"));
        Assert.Equal("-1", Navknoepfe(cut)[1].GetAttribute("tabindex"));
    }

    // =====================================================================
    // Ohne Stammprojekt
    // =====================================================================

    [Fact]
    public void Ohne_Stammprojekt_steht_der_Hinweis_statt_der_Seite()
    {
        var cut = Zeige(p => p.Add(x => x.KeinStammText, "Bitte zuerst ein Stammprojekt wählen."),
                        mitStamm: false);

        Navknoepfe(cut)[2].Click();   // Wirtschaftlichkeit

        Assert.Contains("Stammprojekt wählen", cut.Find(".epos-warnbanner").TextContent);
    }

    // =====================================================================
    // Projektwechsel über den Zustand
    // =====================================================================

    [Fact]
    public void Ein_Projektwechsel_holt_die_Gaben_neu()
    {
        var zustand = new SeitenZustand();
        var cut = Zeige(p => p.Add(x => x.Zustand, zustand));

        Assert.Single(_gefragt);

        zustand.ProjektSetzen(1031, "Variante");

        Assert.Equal(2, _gefragt.Count);
        Assert.Equal(BerichteKostenSeite.SEITE_UEBERSICHT, cut.Instance.AktiveSeite);
    }

    [Fact]
    public void Ein_Seitenwunsch_der_Huelle_stellt_die_Seite_um()
    {
        var zustand = new SeitenZustand();
        string wunsch = "";
        var cut = Zeige(p => p
            .Add(x => x.Zustand, zustand)
            .Add(x => x.Seitenwunsch, () => { string w = wunsch; wunsch = ""; return w; }));

        wunsch = BerichteKostenSeite.SEITE_BERICHT;
        zustand.Auffrischen();

        Assert.Equal(BerichteKostenSeite.SEITE_BERICHT, cut.Instance.AktiveSeite);

        // Der Wunsch gilt genau einmal.
        zustand.Auffrischen();
        Assert.Equal(BerichteKostenSeite.SEITE_BERICHT, cut.Instance.AktiveSeite);
    }

    [Fact]
    public void Nach_dem_Entsorgen_meldet_der_Zustand_nicht_mehr()
    {
        var zustand = new SeitenZustand();
        var cut = Zeige(p => p.Add(x => x.Zustand, zustand));

        int vorher = _gefragt.Count;
        cut.Instance.Dispose();
        zustand.ProjektSetzen(1040, "Anderes");

        Assert.Equal(vorher, _gefragt.Count);
    }
}
