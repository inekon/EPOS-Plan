using System;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der wirksame U-Wert der Bodenplatte als Vorgabe</b> im Gebäude-Katalogeditor (EV1, Entscheid E65): das
/// Feld neben Randbedingung und U-Wert der Grundfläche, leer = NULL (Erdreichkorrektur nach DIN EN ISO 13370),
/// ein Wert wird gespeichert und gelesen, bei Randbedingung Keller oder Außenluft ist es gesperrt und behält
/// seinen Wert, die Auskunftszeile nennt B′ und U_g bzw. die Vorgabe, und ein Wert ≤ 0 meldet beim OK.
/// <para>Die Kultur ist über <see cref="EposBunitContext"/> auf de-DE gepinnt.</para>
/// </summary>
public class GebaeudeErdreichVorgabeTests : EposBunitContext
{
    private const string LABEL = "Wirksamer U-Wert Bodenplatte :";
    private static readonly string[] TYPEN = { "Einfamilienhaus", "Hotel" };
    private static readonly string[] ARTEN = { "Einfamilienhaus", "Hotel", "Kaufhaus" };
    private static readonly string[] KLASSEN =
    { "bis 1859", "1860 bis 1918", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968",
      "1969 bis 1978", "1979 bis 1983", "1984 bis 1994", "1995 bis 2001", "2002 bis 2009",
      "2010 bis 2015", "2016 bis 2020", "ab 2021" };

    private readonly GebaeudeHuelleTexte _texte = new();

    public GebaeudeErdreichVorgabeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static GebaeudeKatalogDaten Satz(string name = "Haus A") => new()
    {
        Name = name,
        Typ = "Einfamilienhaus",
        Beschreibung = "Beschreibung " + name,
        Gebaeudeart = "Hotel",
        Verwendung = "Wohngebaeude",
        Baualtersklasse = 4,
        Bauart = 1,
        WohnflaecheGesamt = 150,
        FlaecheNutzer = 35,
        Waermegewinne = 400,
        Fensterdurchlassgrad = 0.4,
        Raumhoehe = 2.5,
        FensterflaecheNord = 10,
        FensterflaecheSued = 20,
        FensterflaecheOstWest = 15,
        FlaecheAussenwand = 200,
        Dachflaeche = 120,
        Grundflaeche = 100,
        SonstigeFlaechen = 5,
        UWertAussenwand = 0.3,
        UWertFenster = 1.3,
        UWertDachflaeche = 0.2,
        UWertGrundflaeche = 0.35,
        UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1,
        AnschlussFensterWand = 50,
        SollTag = 20,
        NachtAbsenkung = 17,
        MaxTemperatur = 24,
        WochenendAbsenkung = 0,
        SollFerien = 0,
        Luftwechselrate = 0.5,
        WwBedarf = 700
    };

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(
        GebaeudeKatalogDaten? daten = null,
        Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>? speichern = null,
        Func<GebaeudeKatalogDaten, ErdreichAuskunftDaten?>? auskunft = null)
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Satz())
            .Add(x => x.Modus, GebaeudeKatalogModus.Bearbeiten)
            .Add(x => x.Gebaeudetypen, () => TYPEN)
            .Add(x => x.Gebaeudearten, () => ARTEN)
            .Add(x => x.Baualtersklassen, KLASSEN)
            .Add(x => x.Speichern, speichern ?? ((_, _, _) => new GebaeudeKatalogErgebnis(true, "")))
            .Add(x => x.ErdreichAuskunft, auskunft));

    private static IElement Eingabe(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll("label.epos-feld")
              .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == LABEL)
              .QuerySelector("input")!;

    private static IElement Bodenplatte(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.Find("table.epos-huelltabelle tr[data-bauteil=Bodenplatte]");

    private static void Ok(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.Find(".epos-leiste button.epos-knopf--primaer").Click();

    private static void Rand(IRenderedComponent<GebaeudeKatalogDialog> cut, string index)
        => Bodenplatte(cut).QuerySelector("select")!.Change(index);

    [Fact]
    public void Das_Feld_steht_in_der_Bodenplattenzeile_und_leer_schreibt_NULL()
    {
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        IElement feld = Eingabe(cut);
        Assert.Contains(LABEL.TrimEnd(' ', ':'), Bodenplatte(cut).TextContent);
        Assert.Equal("", feld.GetAttribute("value") ?? "");
        Assert.Equal(_texte.HinweisErdreichUWirksam, feld.GetAttribute("placeholder"));
        Assert.False(feld.HasAttribute("disabled"));

        Ok(cut);
        Assert.NotNull(geschrieben);
        Assert.Null(geschrieben.ErdreichUWirksam);
    }

    [Fact]
    public void Ein_Wert_wird_gelesen_und_gespeichert()
    {
        GebaeudeKatalogDaten daten = Satz();
        daten.ErdreichUWirksam = 0.3;
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(daten, speichern: (d, _, _) => { geschrieben = d; return new(true, ""); });

        Assert.Equal("0,3", Eingabe(cut).GetAttribute("value"));

        Eingabe(cut).Input("0,42");
        Ok(cut);

        Assert.Equal(0.42, geschrieben.ErdreichUWirksam);
        Assert.Equal(0.3, daten.ErdreichUWirksam);   // der hereingereichte Satz bleibt unberührt
    }

    [Theory]
    [InlineData("1")]   // Keller
    [InlineData("2")]   // Außenluft
    public void Bei_anderer_Randbedingung_ist_das_Feld_gesperrt_und_behaelt_den_Wert(string rand)
    {
        GebaeudeKatalogDaten daten = Satz();
        daten.ErdreichUWirksam = 0.3;
        GebaeudeKatalogDaten geschrieben = null!;
        var cut = Aufbauen(daten, speichern: (d, _, _) => { geschrieben = d; return new(true, ""); },
                           auskunft: _ => new ErdreichAuskunftDaten(null, 0.3, true));

        Assert.DoesNotContain(_texte.SperreErdreichUWirksam, cut.Markup);
        Rand(cut, rand);

        Assert.True(Eingabe(cut).HasAttribute("disabled"));
        Assert.Equal("0,3", Eingabe(cut).GetAttribute("value"));
        Assert.Contains(_texte.SperreErdreichUWirksam, cut.Markup);
        Assert.DoesNotContain("U_g = Vorgabe", cut.Markup);

        Ok(cut);
        Assert.Equal(0.3, geschrieben.ErdreichUWirksam);
    }

    [Fact]
    public void Die_Auskunftszeile_nennt_B_Strich_und_U_g_der_Rechnung()
    {
        GebaeudeKatalogDaten gefragt = null!;
        var cut = Aufbauen(auskunft: d => { gefragt = d; return new ErdreichAuskunftDaten(6.69, 0.198, false); });

        Assert.Contains("Erdreichkorrektur nach DIN EN ISO 13370: B′ = 6,69 m, U_g = 0,20 W/(m²K)", cut.Markup);
        Assert.Equal(100, gefragt.Grundflaeche);
        Assert.Null(gefragt.ErdreichUWirksam);
    }

    [Fact]
    public void Die_Auskunftszeile_nennt_bei_Vorgabe_den_Wert()
    {
        var cut = Aufbauen(auskunft: d => d.ErdreichUWirksam is double ug
                               ? new ErdreichAuskunftDaten(null, ug, true)
                               : new ErdreichAuskunftDaten(6.69, 0.198, false));

        Eingabe(cut).Input("0,35");

        Assert.Contains("U_g = Vorgabe 0,35 W/(m²K)", cut.Markup);
        Assert.DoesNotContain("B′ =", cut.Markup);
    }

    [Fact]
    public void Ohne_Auskunft_entfaellt_die_Zeile()
    {
        var cut = Aufbauen(auskunft: _ => null);

        Assert.DoesNotContain("U_g =", cut.Markup);
        Assert.DoesNotContain(_texte.SperreErdreichUWirksam, cut.Markup);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0,2")]
    public void Ein_Wert_kleiner_gleich_null_meldet_beim_OK(string eingabe)
    {
        bool geschrieben = false;
        var cut = Aufbauen(speichern: (_, _, _) => { geschrieben = true; return new(true, ""); });

        Eingabe(cut).Input(eingabe);
        Ok(cut);

        Assert.False(geschrieben);
        Assert.Contains(_texte.MeldungErdreichU, cut.Instance.Meldung);
    }
}
