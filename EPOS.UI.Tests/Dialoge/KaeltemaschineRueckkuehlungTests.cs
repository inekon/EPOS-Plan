using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Gruppe „Rückkühlung“ der <see cref="KaeltemaschineKonfiguration"/></b> (K-F5b, Entwurf Split/VRF/Rückkühlwerk
/// 5.2–5.8, 8): nur mit den Gaben der Hülle und nur bei einer wassergekühlten Kältemaschine; bei Rückkühlart Luft ein
/// Hinweis statt der Gruppe. Die Wahl schreibt in den Arbeitsstand, die Lesezeilen kommen fertig, die Hinweise folgen der
/// Einordnung (nass, Luftfeuchte, Ventilator). „Rückkühlwerke…“ nur mit Delegat.
/// </summary>
public class KaeltemaschineRueckkuehlungTests : EposBunitContext
{
    private int _gemeldet;
    private int _geoeffnet;

    public KaeltemaschineRueckkuehlungTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    internal static RueckkuehlwerkLesung Lesung(string name, int? stamm, bool nass, double? ventilator) => new()
    {
        Bezeichner = name, StammId = stamm, Verdunstungskuehlanlage = nass, VentilatorKw = ventilator,
        Zeilen = new List<(string, string)> { (R.RKW_LBL_BAUART, nass ? "Kühlturm, offen" : "Trockenkühler"), (R.RKW_LBL_NENNLEISTUNG, "150 kW") }
    };

    internal static RueckkuehlungGaben Gaben(bool feuchte = false) => new()
    {
        KlimaMitFeuchte = feuchte,
        Katalog = new[]
        {
            new RueckkuehlwerkEintrag(7, "Trocken 1 · Trockenkühler", Lesung("Trocken 1", 7, false, null)),
            new RueckkuehlwerkEintrag(8, "Turm 1 · Kühlturm, offen", Lesung("Turm 1", 8, true, 3.5)),
        }
    };

    private static KaeltemaschineAnlageDaten Satz(bool luft = false) => new()
    {
        AnlagenId = 7, Bezeichner = "KM Bestand", Anzahl = 1,
        Geraet = new KaeltemaschineGeraetwerte { Bezeichner = "KM-Typ 150", Rueckkuehlart = luft ? "Luft" : "Wasser", Luftgekuehlt = luft }
    };

    private IRenderedComponent<KaeltemaschineKonfiguration> Zeige(KaeltemaschineAnlageDaten d, RueckkuehlungGaben? g,
                                                                  bool aktiv = true, bool knopf = true)
        => Render<KaeltemaschineKonfiguration>(b =>
        {
            b.Add(x => x.Daten, d).Add(x => x.Rueckkuehlung, g).Add(x => x.Aktiv, aktiv)
             .Add(x => x.Geaendert, () => _gemeldet++);
            if (knopf) b.Add(x => x.RueckkuehlwerkeOeffnen, () => _geoeffnet++);
        });

    private static IElement? Gruppe(IRenderedComponent<KaeltemaschineKonfiguration> cut)
        => cut.FindAll(".epos-kaeltemaschine-rueckkuehlung").FirstOrDefault();

    private static IElement Wahl(IRenderedComponent<KaeltemaschineKonfiguration> cut)
        => Gruppe(cut)!.QuerySelector("select")!;

    [Fact]
    public void Ohne_Gaben_steht_weder_Gruppe_noch_Hinweis()
    {
        var cut = Zeige(Satz(), null);
        Assert.Null(Gruppe(cut));
        Assert.DoesNotContain(R.KMK_RKW_GRUPPE, cut.Markup);
        Assert.DoesNotContain(R.KMK_RKW_HINWEIS_LUFT, cut.Markup);
    }

    [Fact]
    public void Bei_Rueckkuehlart_Luft_steht_der_Hinweis_statt_der_Gruppe()
    {
        var cut = Zeige(Satz(luft: true), Gaben());
        Assert.Null(Gruppe(cut));
        Assert.Contains(R.KMK_RKW_HINWEIS_LUFT, cut.Markup);
        Assert.Empty(cut.FindAll(".epos-kaeltemaschine-rueckkuehlwerke"));
    }

    [Fact]
    public void Ohne_Geraetewerte_steht_keine_Gruppe()
    {
        var d = Satz();
        d.Geraet = null;
        var cut = Zeige(d, Gaben());
        Assert.Null(Gruppe(cut));
    }

    [Fact]
    public void Die_Klappliste_fuehrt_keins_und_die_Katalogsaetze_ohne_Wahl_gelten_die_Festwerte()
    {
        var cut = Zeige(Satz(), Gaben());
        Assert.Contains(R.KMK_RKW_GRUPPE, cut.Markup);
        string[] eintraege = Wahl(cut).QuerySelectorAll("option").Select(o => o.TextContent).ToArray();
        Assert.Equal(new[] { R.KMK_RKW_KEINS, "Trocken 1 · Trockenkühler", "Turm 1 · Kühlturm, offen" }, eintraege);
        Assert.Contains(R.KMK_RKW_HINWEIS_KEINS, Gruppe(cut)!.TextContent);
        Assert.DoesNotContain(R.KMK_RKW_HINWEIS_WAHL, Gruppe(cut)!.TextContent);
    }

    [Fact]
    public void Die_Wahl_schreibt_in_den_Arbeitsstand_meldet_und_zeigt_die_Lesezeilen()
    {
        var d = Satz();
        var cut = Zeige(d, Gaben(feuchte: true));
        Wahl(cut).Change("7");
        Assert.Equal(7, d.RueckkuehlwerkWahl);
        Assert.Equal(1, _gemeldet);
        string text = Gruppe(cut)!.TextContent;
        Assert.Contains(R.RKW_LBL_NENNLEISTUNG, text);
        Assert.Contains(R.KMK_RKW_HINWEIS_WAHL, text);
        Assert.DoesNotContain(R.RKW_HINWEIS_BIMSCHV, text);                       // trocken: kein Hinweis
        Assert.DoesNotContain(R.KMK_RKW_HINWEIS_FEUCHTE, text);

        Wahl(cut).Change("");                                                     // zurück auf „keins“
        Assert.Null(d.RueckkuehlwerkWahl);
    }

    [Fact]
    public void Nasse_Bauart_ohne_Feuchte_nennt_Feuchte_BImSchV_und_Ventilator()
    {
        var d = Satz();
        d.RueckkuehlwerkWahl = 8;
        var cut = Zeige(d, Gaben(feuchte: false));
        string text = Gruppe(cut)!.TextContent;
        Assert.Contains(R.KMK_RKW_HINWEIS_FEUCHTE, text);
        Assert.Contains(R.RKW_HINWEIS_BIMSCHV, text);
        Assert.Contains(string.Format(System.Globalization.CultureInfo.CurrentCulture, R.KMK_RKW_HINWEIS_VENTILATOR, 3.5.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture)), text);
    }

    [Fact]
    public void Nasse_Bauart_mit_Feuchte_nennt_nur_die_BImSchV()
    {
        var d = Satz();
        d.RueckkuehlwerkWahl = 8;
        var cut = Zeige(d, Gaben(feuchte: true));
        string text = Gruppe(cut)!.TextContent;
        Assert.DoesNotContain(R.KMK_RKW_HINWEIS_FEUCHTE, text);
        Assert.Contains(R.RKW_HINWEIS_BIMSCHV, text);
    }

    [Fact]
    public void Die_gespeicherte_Projektkopie_zeigt_ihre_eigenen_Lesezeilen()
    {
        var d = Satz();
        d.RueckkuehlwerkWahl = 7;
        d.Rueckkuehlwerk = Lesung("Trocken 1", 7, false, 1.25);
        var cut = Zeige(d, Gaben());
        string text = Gruppe(cut)!.TextContent;
        Assert.Contains(string.Format(System.Globalization.CultureInfo.CurrentCulture, R.KMK_RKW_HINWEIS_VENTILATOR, 1.25.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture)), text);
        Assert.DoesNotContain(R.KMK_RKW_HINWEIS_WAHL, text);                       // unverändert: nichts zu schreiben
    }

    [Fact]
    public void Eine_Projektkopie_ohne_Katalogsatz_steht_als_eigener_Eintrag()
    {
        var d = Satz();
        d.RueckkuehlwerkWahl = KaeltemaschineAnlageDaten.RUECKKUEHLWERK_PROJEKTKOPIE;
        d.Rueckkuehlwerk = Lesung("Eigenbau", null, false, null);
        var cut = Zeige(d, Gaben());
        IElement gewaehlt = Wahl(cut).QuerySelectorAll("option").Single(o => o.HasAttribute("selected"));
        Assert.Equal(string.Format(System.Globalization.CultureInfo.CurrentCulture, R.KMK_RKW_PROJEKTKOPIE, "Eigenbau"), gewaehlt.TextContent);
        Assert.DoesNotContain(R.KMK_RKW_HINWEIS_WAHL, Gruppe(cut)!.TextContent);
    }

    [Fact]
    public void Der_Knopf_oeffnet_die_Verwaltung_nur_mit_Delegat()
    {
        var cut = Zeige(Satz(), Gaben());
        IElement knopf = cut.Find(".epos-kaeltemaschine-rueckkuehlwerke");
        Assert.Equal(R.RKW_MENUE, knopf.TextContent.Trim());
        knopf.Click();
        Assert.Equal(1, _geoeffnet);

        var ohne = Zeige(Satz(), Gaben(), knopf: false);
        Assert.Empty(ohne.FindAll(".epos-kaeltemaschine-rueckkuehlwerke"));
    }

    [Fact]
    public void Gesperrt_ist_die_Wahl_bedienungslos()
    {
        var cut = Zeige(Satz(), Gaben(), aktiv: false);
        Assert.True(Wahl(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void Die_Wahl_ist_eine_Eingabe_des_Feldsatzes()
    {
        var a = Satz();
        var b = a.Kopie();
        Assert.True(a.GleicheEingaben(b));
        b.RueckkuehlwerkWahl = 8;
        Assert.False(a.GleicheEingaben(b));
    }

    [Fact]
    public void Die_Huelle_lehnt_ein_Rueckkuehlwerk_an_einer_luftgekuehlten_Maschine_ab()
    {
        var d = Satz(luft: true);
        d.RueckkuehlwerkWahl = 7;
        Assert.Equal(R.RKW_MSG_LUFT_UNVERTRAEGLICH, KaeltemaschineAnlageHuelle.Pruefen(d));
        d.RueckkuehlwerkWahl = null;
        Assert.True(string.IsNullOrEmpty(KaeltemaschineAnlageHuelle.Pruefen(d)));
    }
}
