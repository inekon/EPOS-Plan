using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using WindowsFormsApplication1;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>KD1 — „Energiekosten…" im Kältemaschinen-Dialog</b> (Anwenderentscheid 13.10.2026, Befund KA1): Der Knopf
/// der <c>KostenKnoepfeLeiste</c> führt wie bei Wärmepumpe und BHKW in die TRÄGERKARTE — hier des Kühlträgers der
/// gewählten Anlage (<c>Kuehl_ID_Carrier</c>, ohne eigenen der Stromträger des Projekts). Der Weg ist
/// plattformfrei: Die Gabe <c>EnergietraegerGaben</c> füllt die Hülle selbst, die Karte erscheint als
/// Überlagerung desselben Fensters.
///
/// <para>Geprüft werden: Knopf vorhanden und aktiv mit Träger, die richtige Träger-Id in der Gabe, der Rückfall
/// auf den Projektträger, die benannte weiche Sperre ohne Träger und bei einer gewählten Wärmepumpe, und das
/// Nachziehen der Kostensummen nach dem Schließen.</para>
/// </summary>
public class KaeltemaschineAnlageEnergiekostenTests : EposBunitContext
{
    public KaeltemaschineAnlageEnergiekostenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const int ID_ANLAGE = 77;
    private const int ID_GERAET = 404;
    private const int ID_TRAEGER = 31;
    private const int ID_PROJEKTTRAEGER = 9;
    private const int ID_WP = 501;

    /// <summary>Der Kühlträger der Anlage; <c>null</c> = die Anlage führt keinen eigenen.</summary>
    private int? _traegerDerAnlage = ID_TRAEGER;

    private bool _gabeLiefert = true;

    /// <summary>Jeder Ruf der Gabe mit Träger- und Geräte-Id — die Messlatte des Sprungziels.</summary>
    private readonly List<(int Traeger, int Geraet)> _rufe = new();

    private int _summenrufe;

    private readonly List<KuehlWaermepumpeZeile> _wp = new()
    {
        new(ID_WP, 40, "WP Bestand", "Neutral", "Luft-Wasser", 1, 1, 38, 3.4, 7, "Strom", false, "zulässig")
    };

    private static readonly KaeltemaschineAnlageTexte TEXTE = new();

    private KaeltemaschineAnlageDaten Anlage() => new()
    {
        AnlagenId = ID_ANLAGE, GeraetId = ID_GERAET, Bezeichner = "KM 1", Anzahl = 1,
        KuehlVorlauf = 7, KuehlCarrierId = _traegerDerAnlage
    };

    private IReadOnlyDictionary<string, object>? Gabe(int traeger, int geraet)
    {
        _rufe.Add((traeger, geraet));
        return _gabeLiefert ? new Dictionary<string, object>() : null;
    }

    private IRenderedComponent<KaeltemaschineAnlageDialog> Aufbauen(bool mitGabe = true, int projekttraeger = 0,
                                                                    bool mitWp = false)
        => Render<KaeltemaschineAnlageDialog>(b => b
            .Add(x => x.Anlagen, () => (IReadOnlyList<KaeltemaschineAnlageDaten>)new[] { Anlage() })
            .Add(x => x.Stromtraeger, new[] { (ID_TRAEGER, "Strom Kälte"), (ID_PROJEKTTRAEGER, "Strom") })
            .Add(x => x.ProjektStromtraeger, projekttraeger)
            .Add(x => x.KuehlWaermepumpen,
                 mitWp ? () => (IReadOnlyList<KuehlWaermepumpeZeile>)_wp.ToList() : null)
            .Add(x => x.Kostensumme, _ => { _summenrufe++; return (1000.0, 50.0); })
            .Add(x => x.EnergietraegerGaben,
                 mitGabe ? Gabe : (Func<int, int, IReadOnlyDictionary<string, object>?>?)null));

    private static void Projektzeile(IRenderedComponent<KaeltemaschineAnlageDialog> cut, string name)
        => cut.FindAll(".epos-zweispalten-bereich--projekt tbody tr").First(tr => tr.TextContent.Contains(name))
              .QuerySelector(".epos-zeilenzelle")!.Click();

    /// <summary>
    /// Die Kostenleiste steht im Fragment „Satz", und das trägt bei Kurzangaben (UeS2) allein die
    /// Satz-Überlagerung — „Bearbeiten…" der Knopfleiste am Satz öffnet sie.
    /// </summary>
    private static void SatzOeffnen(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt").Click();

    private static IElement Energieknopf(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => cut.FindAll(".epos-kostenleiste button").First(b => b.TextContent.Trim() == TEXTE.KostenEnergie);

    private static string Banner(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => string.Join(" ", cut.FindAll(".epos-warnbanner-text").Select(e => e.TextContent));

    /// <summary>Der Hinweis der Satz-Überlagerung — dort steht die Ablehnung, solange sie offen ist.</summary>
    private static string Satzhinweis(IRenderedComponent<KaeltemaschineAnlageDialog> cut)
        => string.Join(" ", cut.FindAll(".epos-satzueberlagerung-hinweis").Select(e => e.TextContent));

    [Fact]
    public void Mit_Traeger_fuehrt_der_Knopf_in_die_Traegerkarte()
    {
        var cut = Aufbauen();
        Projektzeile(cut, "KM 1");
        SatzOeffnen(cut);

        var knopf = Energieknopf(cut);
        Assert.Null(knopf.GetAttribute("aria-disabled"));

        knopf.Click();
        Assert.True(cut.Instance.TraegerkarteOffen);
        Assert.Equal(ID_TRAEGER, cut.Instance.TraegerkarteTraeger);
        Assert.Equal(new[] { (ID_TRAEGER, ID_GERAET) }, _rufe);
        Assert.Contains(R.KDLG_ET_TITEL, cut.Markup);
    }

    [Fact]
    public void Ohne_eigenen_Traeger_gilt_der_Stromtraeger_des_Projekts()
    {
        _traegerDerAnlage = null;
        var cut = Aufbauen(projekttraeger: ID_PROJEKTTRAEGER);
        Projektzeile(cut, "KM 1");
        SatzOeffnen(cut);

        Energieknopf(cut).Click();
        Assert.Equal(new[] { (ID_PROJEKTTRAEGER, ID_GERAET) }, _rufe);
        Assert.Equal(ID_PROJEKTTRAEGER, cut.Instance.TraegerkarteTraeger);
    }

    [Fact]
    public void Ohne_Traeger_bleibt_der_Knopf_benannt_gesperrt()
    {
        _traegerDerAnlage = null;
        var cut = Aufbauen();
        Projektzeile(cut, "KM 1");
        SatzOeffnen(cut);

        var knopf = Energieknopf(cut);
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(R.KMA_ENERGIEKOSTEN_OHNE_TRAEGER, knopf.GetAttribute("title"));

        knopf.Click();
        Assert.False(cut.Instance.TraegerkarteOffen);
        Assert.Empty(_rufe);
        Assert.Contains(R.KMA_ENERGIEKOSTEN_OHNE_TRAEGER, Satzhinweis(cut));
    }

    [Fact]
    public void Bei_einer_Waermepumpe_nennt_der_Knopf_den_Waermepumpendialog()
    {
        var cut = Aufbauen(mitWp: true);
        Projektzeile(cut, "WP Bestand");

        var knopf = cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--energiekosten-wp");
        Assert.Equal(TEXTE.KostenEnergie, knopf.TextContent.Trim());
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(R.KMA_ENERGIEKOSTEN_WP, knopf.GetAttribute("title"));

        knopf.Click();
        Assert.False(cut.Instance.TraegerkarteOffen);
        Assert.Empty(_rufe);
        Assert.Contains(R.KMA_ENERGIEKOSTEN_WP, Banner(cut));
    }

    [Fact]
    public void Ohne_die_Gabe_gibt_es_den_Knopf_nicht()
    {
        var cut = Aufbauen(mitGabe: false);
        Projektzeile(cut, "KM 1");
        SatzOeffnen(cut);
        Assert.DoesNotContain(TEXTE.KostenEnergie,
                              cut.FindAll(".epos-kostenleiste button").Select(b => b.TextContent.Trim()));
    }

    [Fact]
    public void Nach_dem_Schliessen_zieht_der_Satz_die_Kostensummen_nach()
    {
        var cut = Aufbauen();
        Projektzeile(cut, "KM 1");
        SatzOeffnen(cut);
        Energieknopf(cut).Click();
        Assert.True(cut.Instance.TraegerkarteOffen);

        int vorher = _summenrufe;
        cut.FindAll(".epos-ueberlagerung").First(d => d.GetAttribute("aria-label") == R.KDLG_ET_TITEL)
           .QuerySelector(".epos-ueberlagerung-zu")!.Click();

        Assert.False(cut.Instance.TraegerkarteOffen);
        Assert.True(_summenrufe > vorher, "Nach dem Schließen der Trägerkarte frischt der Satz seine Kurzangaben auf.");
    }
}
