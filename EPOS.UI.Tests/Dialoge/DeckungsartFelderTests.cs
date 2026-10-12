using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Baustein <see cref="DeckungsartFelder"/> (Konzept Kältebedarf 3.4, K5a): Sichtbarkeit der acht Felder, die
/// Vorgaben 3,0 · 35 °C und 4,0 · 25 °C samt Herleitungszeile, die Klappgruppe und die Prüfregel des Kerns.
/// </summary>
public class DeckungsartFelderTests : EposBunitContext
{
    public DeckungsartFelderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private static readonly (int Id, string Text)[] TRAEGER = { (1, "Strom A"), (2, "Strom B") };

    private IRenderedComponent<DeckungsartFelder> Zeige(DeckungsartDaten d, bool klappbar = false, bool gesperrt = false,
                                                        Action? geaendert = null)
        => Render<DeckungsartFelder>(p => p
            .Add(x => x.Daten, d)
            .Add(x => x.Stromtraeger, TRAEGER)
            .Add(x => x.Klappbar, klappbar)
            .Add(x => x.Gesperrt, gesperrt)
            .Add(x => x.Geaendert, () => geaendert?.Invoke()));

    [Fact]
    public void Zentral_zeigt_nur_die_Deckungsart_und_den_Hinweis()
    {
        var cut = Zeige(new DeckungsartDaten());
        Assert.Single(cut.FindAll("select"));
        Assert.Empty(cut.FindAll("input"));
        Assert.Contains(Resource.DECK_HINWEIS, cut.Markup);
        Assert.DoesNotContain(Resource.DECK_LBL_EER_WEG, cut.Markup);
    }

    [Fact]
    public void Der_Wechsel_auf_dezentral_belegt_die_Vorgaben_und_nennt_sie()
    {
        var d = new DeckungsartDaten();
        int meldungen = 0;
        var cut = Zeige(d, geaendert: () => meldungen++);

        cut.FindAll("select")[0].Change("1");

        Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, d.Deckungsart);
        Assert.Equal(KaeltebedarfSchema.EER_FEST, d.EerWeg);
        Assert.Equal(DeckungsartDaten.VORGABE_EER_1, d.Eer1);
        Assert.Equal(DeckungsartDaten.VORGABE_TAUSSEN_1, d.Taussen1);
        Assert.Null(d.Eer2);
        Assert.Equal(1, meldungen);
        // Deckungsart, EER-Weg, Kühlträger; EER, T, Zähler.
        Assert.Equal(3, cut.FindAll("select").Count);
        Assert.Equal(3, cut.FindAll("input").Count);
        Assert.True(d.PunktEinsIstVorgabe);
        Assert.Contains(Resource.DECK_HRL_VORGABE.Split(':')[0].Replace("{0}", "").Trim(), cut.Markup);
        Assert.Contains(Resource.DECK_PLATZHALTER_TRAEGER, cut.Markup);
        Assert.Null(d.Pruefgrund());
    }

    [Fact]
    public void Linear_zeigt_den_zweiten_Punkt_mit_Vorgabe()
    {
        var d = new DeckungsartDaten();
        d.DeckungSetzen(KaeltebedarfSchema.DECKUNG_SPLIT);
        var cut = Zeige(d);

        cut.FindAll("select")[1].Change("1");

        Assert.Equal(KaeltebedarfSchema.EER_LINEAR, d.EerWeg);
        Assert.Equal(DeckungsartDaten.VORGABE_EER_2, d.Eer2);
        Assert.Equal(DeckungsartDaten.VORGABE_TAUSSEN_2, d.Taussen2);
        Assert.Equal(5, cut.FindAll("input").Count);
        Assert.Contains(Resource.DECK_LBL_EER2, cut.Markup);
        Assert.True(d.PunktZweiIstVorgabe);
        Assert.Null(d.Pruefgrund());
    }

    [Fact]
    public void Die_Pruefregel_ist_die_des_Kerns()
    {
        var d = new DeckungsartDaten();
        Assert.Null(d.Pruefgrund());
        d.DeckungSetzen(KaeltebedarfSchema.DECKUNG_SPLIT);
        d.Eer1 = 0;
        Assert.NotNull(d.Pruefgrund());
        d.Eer1 = 3;
        d.EerWegSetzen(KaeltebedarfSchema.EER_LINEAR);
        d.Taussen2 = d.Taussen1;
        Assert.NotNull(d.Pruefgrund());
        Assert.Equal(Kaeltedeckungsregel.Pruefe(d.AlsModell()), d.Pruefgrund());
    }

    [Fact]
    public void Klappbar_ist_zu_bei_zentral_und_offen_bei_dezentral()
    {
        var zentral = Zeige(new DeckungsartDaten(), klappbar: true);
        Assert.False(zentral.Instance.Offen);
        Assert.Empty(zentral.FindAll("select"));
        Assert.Contains(Resource.DECK_ART_ZENTRAL, zentral.Markup);

        var d = new DeckungsartDaten();
        d.DeckungSetzen(KaeltebedarfSchema.DECKUNG_SPLIT);
        var split = Zeige(d, klappbar: true);
        Assert.True(split.Instance.Offen);
        Assert.Equal(3, split.FindAll("select").Count);
    }

    [Fact]
    public void Gesperrt_nimmt_keine_Eingabe_an()
    {
        var d = new DeckungsartDaten();
        d.DeckungSetzen(KaeltebedarfSchema.DECKUNG_SPLIT);
        var cut = Zeige(d, gesperrt: true);
        Assert.All(cut.FindAll("select"), s => Assert.True(s.HasAttribute("disabled")));
        Assert.All(cut.FindAll("input"), i => Assert.True(i.HasAttribute("disabled")));
    }
}
