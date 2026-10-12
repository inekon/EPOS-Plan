using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog „Wärmebedarf extern“ als „Kältebedarf extern“ (K5a, Konzept Kältebedarf 3.2, 3.4, OP-2): Modus
/// <see cref="WaermebedarfExternModus.NurKuehlung"/>, die Feldgruppe „Deckung“ in der Detailzeile der Projektzeile, die
/// Ablehnung „Split ohne EER“ und der OP-2-Hinweis im Modus „Alle“.
/// </summary>
public class WaermebedarfExternKaelteTests : EposBunitContext
{
    private static readonly (string Wert, string Text)[] KANAELE =
    {
        ("HEIZUNG", "Heizung"), ("BRAUCHWASSER", "Brauchwasser"), ("PROZESS", "Prozesswärme"), ("KUEHLUNG", "Kühlung")
    };

    public WaermebedarfExternKaelteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static WaermebedarfExternZeile Zeile(int idZ, string name, string kanal) => new()
    {
        IdZ = idZ, IdGanglinie = 5, Bezeichner = name, Kanal = kanal
    };

    private static List<WaermebedarfExternZeile> Gemischt() => new()
    {
        Zeile(1, "Heizlast", DbWerte.KANAL_HEIZUNG),
        Zeile(2, "Kaeltelast", DbWerte.KANAL_KUEHLUNG),
        Zeile(3, "Prozess", DbWerte.KANAL_PROZESS)
    };

    private IRenderedComponent<WaermebedarfExternDialog> Zeige(List<WaermebedarfExternZeile> zeilen,
        WaermebedarfExternModus modus, Action<bool>? geschlossen = null, Action? geaendert = null)
        => Render<WaermebedarfExternDialog>(p => p
            .Add(x => x.Modus, modus)
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogzeilen, () => Task.FromResult<IReadOnlyList<Katalogfilterzeile>>(new[]
            {
                Zeitreihenproben.Zeile(1, "Ganglinie A", jahresarbeitMwh: 10, spitzeKw: 5)
            }))
            .Add(x => x.Katalogprofil, Zeitreihenproben.ProjektProfil(Zeitreihenart.Waermebedarf))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Aufnehmen, n => new WaermebedarfExternZeile { IdZ = 0, IdGanglinie = 9, Bezeichner = n, Kanal = "HEIZUNG" })
            .Add(x => x.Kanaele, KANAELE)
            .Add(x => x.Stromtraeger, new[] { (7, "Strom") })
            .Add(x => x.Geaendert, geaendert)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IReadOnlyList<IElement> Projektzeilen(IRenderedComponent<WaermebedarfExternDialog> cut)
        => cut.FindAll(".epos-projektzeile");

    private static IElement Knopf(IRenderedComponent<WaermebedarfExternDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void NurKuehlung_zeigt_nur_Kuehlzeilen_und_keine_Kanalwahl()
    {
        var cut = Zeige(Gemischt(), WaermebedarfExternModus.NurKuehlung);

        Assert.Equal(new[] { "Kaeltelast" }, Projektzeilen(cut).Select(z => z.TextContent.Trim()));
        Assert.Equal("Kaeltelast", cut.Instance.Gewaehlt?.Bezeichner);
        Assert.DoesNotContain(cut.FindAll(".epos-zweispalten-kanal .epos-feld-text"), e => e.TextContent.Trim() == "Kanal");
        Assert.True(cut.Instance.DeckungSichtbar);
        Assert.Single(cut.FindComponents<DeckungsartFelder>());
        // OP-2 gilt nur im Modus „Alle".
        Assert.Empty(cut.FindAll(".epos-zweispalten-kanalinfo"));
    }

    [Fact]
    public void NurKuehlung_legt_eine_neue_Zeile_auf_Kuehlung_an()
    {
        var zeilen = Gemischt();
        var cut = Zeige(zeilen, WaermebedarfExternModus.NurKuehlung);

        cut.FindAll(".epos-zweispalten-spalte--unten .epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();

        WaermebedarfExternZeile neu = zeilen.Last();
        Assert.Equal("Ganglinie A", neu.Bezeichner);
        Assert.Equal(DbWerte.KANAL_KUEHLUNG, neu.Kanal);
        Assert.Equal(2, Projektzeilen(cut).Count);
        // Die Wärmezeilen bleiben unberührt in der Liste der Hülle.
        Assert.Equal(DbWerte.KANAL_HEIZUNG, zeilen[0].Kanal);
        Assert.Equal(DbWerte.KANAL_PROZESS, zeilen[2].Kanal);
    }

    [Fact]
    public void Alle_zeigt_bei_einer_Kuehlzeile_den_OP2_Hinweis_und_die_Deckung()
    {
        var cut = Zeige(Gemischt(), WaermebedarfExternModus.Alle);
        Assert.Equal(3, Projektzeilen(cut).Count);

        // Heizzeile gewählt: keine Deckung, kein Hinweis.
        Assert.False(cut.Instance.DeckungSichtbar);
        Assert.Empty(cut.FindAll(".epos-zweispalten-kanalinfo"));

        Projektzeilen(cut)[1].Click();
        Assert.True(cut.Instance.DeckungSichtbar);
        Assert.Equal(Resource.WBX_HRL_KAELTE_EXTERN, cut.Find(".epos-zweispalten-kanalinfo").TextContent.Trim());
        Assert.Single(cut.FindComponents<DeckungsartFelder>());
    }

    [Fact]
    public void Ok_lehnt_Split_ohne_EER_ab_und_bleibt_offen()
    {
        var zeilen = Gemischt();
        zeilen[1].Deckung.DeckungSetzen(KaeltebedarfSchema.DECKUNG_SPLIT);
        zeilen[1].Deckung.Eer1 = 0;
        bool? geschlossen = null;
        var cut = Zeige(zeilen, WaermebedarfExternModus.Alle, b => geschlossen = b);

        Knopf(cut, "OK").Click();

        Assert.Null(geschlossen);
        Assert.Equal(Resource.DECK_MSG_OHNE_EER, cut.Instance.Meldung);
        Assert.Same(zeilen[1], cut.Instance.Gewaehlt);
        Assert.Contains(Resource.DECK_MSG_OHNE_EER, cut.Find(".epos-warnbanner-text").TextContent);

        // Mit EER > 0 geht OK durch.
        zeilen[1].Deckung.Eer1 = 2.5;
        Knopf(cut, "OK").Click();
        Assert.True(geschlossen);
    }

    [Fact]
    public void Eine_Wahl_in_der_Deckung_meldet_die_Aenderung_an_die_Huelle()
    {
        int gemeldet = 0;
        var zeilen = Gemischt();
        var cut = Zeige(zeilen, WaermebedarfExternModus.NurKuehlung, geaendert: () => gemeldet++);

        // Die Gruppe steht bei „zentral" zugeklappt; der Kopf klappt sie auf.
        cut.Find(".epos-deckung button").Click();
        cut.FindComponent<DeckungsartFelder>().FindAll("select")[0].Change("1");

        Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, zeilen[1].Deckungsart);
        Assert.Equal(DeckungsartDaten.VORGABE_EER_1, zeilen[1].Eer1);
        Assert.True(gemeldet > 0);
        Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, zeilen[0].Deckungsart);
    }
}
