using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Gruppe „Aufheizung" des Bedarfsdialogs</b> (Entwurf KP3, Welle O2; E60, Festlegungen 22, 25, 41) — je Zustand:
/// bemessen, aus (Auskunft), unerreichbar, gekoppelt, manuell, mit Zonen; die Auslegungsgröße mit ihren Teilen und dem
/// Hinweis zur idealen Spitze in beiden Sprachen; ohne Daten keine Gruppe, im Dialog eingehängt. Runde Phantasiewerte.
/// </summary>
public sealed class GebaeudeBedarfAufheizungTests : EposBunitContext
{
    public GebaeudeBedarfAufheizungTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static GebaeudeBedarfAufheizDaten Bemessen(params string[] hinweise) => new()
    {
        Zustand = "BEMESSEN", Zustandtext = "bemessen", AufheizzeitMaxH = 6, AussenC = -12.0,
        Variante = "kälteste Stunde", Art = "täglich", LeistungKw = 12.0, Quelle = "Zielleistung",
        Aufheiztage = 150, AufheizstundenH = 600, AufheizzeitLaengsteH = 6, TageBegrenzt = 10, TageUnerreichbar = 0,
        TageNachweisband = 0, SpruengeAus = 2, KappungsstundenH = 0.0,
        AuslegungsheizlastKw = 10.0, AufheizzuschlagKw = 2.0, AuslegungsgroesseKw = 12.0,
        SpitzeKw = 20.0, SpitzeTagesmittelKw = 8.0, Hinweise = hinweise,
    };

    private IRenderedComponent<GebaeudeBedarfAufheizung> Zeichne(GebaeudeBedarfAufheizDaten? daten)
        => Render<GebaeudeBedarfAufheizung>(p => p.Add(x => x.Daten, daten));

    private static string Zelle(IRenderedComponent<GebaeudeBedarfAufheizung> cut, string zeile, int spalte = 1)
        => cut.Find("tr." + zeile).QuerySelectorAll("td")[spalte].TextContent.Trim();

    [Fact]
    public void Ohne_Daten_steht_keine_Gruppe()
    {
        Assert.Empty(Zeichne(null).FindAll(".gebb-aufheizung"));
    }

    [Fact]
    public void Bemessen_zeigt_Zeit_mit_Aussentemperatur_und_Variante_die_Zaehlungen_und_die_Hinweise()
    {
        var cut = Zeichne(Bemessen("W2 — durch die Absenkdauer begrenzt: An 10 Tagen …"));
        Assert.Equal("BEMESSEN", cut.Find(".gebb-aufheizung").GetAttribute("data-zustand"));
        Assert.Contains("Aufheizung", cut.Markup);
        Assert.Equal("6 h bei -12,0 °C (kälteste Stunde)", Zelle(cut, "gebb-aufheiz-zeit").Replace('−', '-'));
        Assert.Equal("täglich", Zelle(cut, "gebb-aufheiz-art"));
        Assert.Equal("150", Zelle(cut, "gebb-aufheiz-tage"));
        Assert.Equal("600", Zelle(cut, "gebb-aufheiz-stunden"));
        Assert.Equal("10", Zelle(cut, "gebb-aufheiz-begrenzt"));
        Assert.Equal("2", Zelle(cut, "gebb-aufheiz-spruenge"));
        Assert.Equal("0,0", Zelle(cut, "gebb-aufheiz-kappung"));
        Assert.Single(cut.FindAll(".gebb-aufheiz-hinweise li"));
        Assert.Empty(cut.FindAll(".gebb-aufheiz-zonen"));
    }

    [Fact]
    public void Die_Auslegungsgroesse_steht_mit_ihren_Teilen_neben_idealer_Spitze_Tagesmittel_und_P_auf()
    {
        var cut = Zeichne(Bemessen());
        Assert.Equal("12,0", Zelle(cut, "gebb-aufheiz-auslegungsgroesse"));
        Assert.Equal("10,0", Zelle(cut, "gebb-aufheiz-auslegungsheizlast"));
        Assert.Equal("2,0", Zelle(cut, "gebb-aufheiz-zuschlag"));
        Assert.Equal("20,0", Zelle(cut, "gebb-aufheiz-spitze"));
        Assert.Equal("8,0", Zelle(cut, "gebb-aufheiz-tagesmittel"));
        Assert.Equal("12,0", Zelle(cut, "gebb-aufheiz-leistung"));
        Assert.Equal("Zielleistung", Zelle(cut, "gebb-aufheiz-quelle"));
        Assert.Contains("keine Auslegungsgröße", cut.Markup);
        Assert.DoesNotContain("Bemessung ohne Jahreslauf", cut.Markup);
    }

    [Fact]
    public void Schalter_aus_zeigt_nur_die_Auslegungsgroesse_aus_der_Auskunft()
    {
        var cut = Zeichne(new GebaeudeBedarfAufheizDaten
        {
            Zustand = GebaeudeBedarfAufheizDaten.AUS, Zustandtext = "Aufheizoptimierung aus",
            AuslegungsheizlastKw = 10.0, AufheizzuschlagKw = 2.0, AuslegungsgroesseKw = 12.0, SpitzeKw = 20.0,
            SpitzeTagesmittelKw = 8.0, LeistungKw = 12.0, Quelle = "Zielleistung",
        });
        Assert.Equal("AUS", cut.Find(".gebb-aufheizung").GetAttribute("data-zustand"));
        Assert.Empty(cut.FindAll("tr.gebb-aufheiz-zeit"));
        Assert.Empty(cut.FindAll("tr.gebb-aufheiz-tage"));
        Assert.Equal("12,0", Zelle(cut, "gebb-aufheiz-auslegungsgroesse"));
        Assert.Contains("Bemessung ohne Jahreslauf", cut.Markup);
        Assert.Contains("keine Auslegungsgröße", cut.Markup);
    }

    [Fact]
    public void Unerreichbar_zeigt_keine_Zeit_und_den_Hinweis_W1()
    {
        var cut = Zeichne(new GebaeudeBedarfAufheizDaten
        {
            Zustand = "UNERREICHBAR", Zustandtext = "unerreichbar — keine Rampe bis 48 h hält", AussenC = -12.0,
            TageUnerreichbar = 30, Hinweise = new[] { "W1 — Aufheizleistung reicht nicht: …" },
        });
        Assert.Equal("—", Zelle(cut, "gebb-aufheiz-zeit"));
        Assert.Equal("30", Zelle(cut, "gebb-aufheiz-unerreichbar"));
        Assert.Equal("—", Zelle(cut, "gebb-aufheiz-auslegungsgroesse"));
        Assert.StartsWith("W1", cut.Find(".gebb-aufheiz-hinweise li").TextContent);
    }

    [Fact]
    public void Gekoppelt_nennt_W5_und_hat_keine_Art()
    {
        var cut = Zeichne(new GebaeudeBedarfAufheizDaten
        {
            Zustand = "GEKOPPELT", Zustandtext = "gekoppelt — nicht optimiert", KappungsstundenH = 4.5,
            Hinweise = new[] { "W5 — gekoppeltes Gebäude nicht optimiert: …" },
        });
        Assert.Equal("gekoppelt — nicht optimiert", Zelle(cut, "gebb-aufheiz-zustand"));
        Assert.Empty(cut.FindAll("tr.gebb-aufheiz-art"));
        Assert.Equal("4,5", Zelle(cut, "gebb-aufheiz-kappung"));
        Assert.StartsWith("W5", cut.Find(".gebb-aufheiz-hinweise li").TextContent);
    }

    [Fact]
    public void Manuell_nennt_die_manuelle_Zeit_neben_der_bemessenen()
    {
        var daten = Bemessen();
        var manuell = new GebaeudeBedarfAufheizDaten
        {
            Zustand = daten.Zustand, Zustandtext = daten.Zustandtext, AufheizzeitMaxH = 6, AussenC = -12.0,
            Variante = daten.Variante, Art = "manuell (9 h)", AufheizzeitManuellH = 9,
        };
        var cut = Zeichne(manuell);
        Assert.Equal("manuell (9 h)", Zelle(cut, "gebb-aufheiz-art"));
        Assert.Contains("manuellen Aufheizzeit 9 h", cut.Markup);
    }

    [Fact]
    public void Zonen_bekommen_je_eine_Zeile_ab_zwei_Zonen()
    {
        var cut = Zeichne(new GebaeudeBedarfAufheizDaten
        {
            Zustand = "BEMESSEN", Zustandtext = "bemessen",
            Zonen = new[]
            {
                new GebaeudeBedarfAufheizZoneDaten { Name = "Zone 1", Zustandtext = "bemessen", AufheizzeitMaxH = 4, LeistungKw = 5.0, Quelle = "Zielleistung", Aufheiztage = 100, AufheizzeitLaengsteH = 4, KappungsstundenH = 0.0 },
                new GebaeudeBedarfAufheizZoneDaten { Name = "Zone 2", Zustandtext = "unbeheizt — ohne Rampe" },
            },
        });
        var zeilen = cut.FindAll(".gebb-aufheiz-zonen tbody tr");
        Assert.Equal(2, zeilen.Count);
        Assert.Equal("4", zeilen[0].QuerySelectorAll("td")[2].TextContent.Trim());
        Assert.Equal("—", zeilen[1].QuerySelectorAll("td")[3].TextContent.Trim());
        Assert.Contains("Vereinigung der beheizten Zonen", cut.Markup);
    }

    [Fact]
    public void Englisch_heissen_Gruppe_und_Auslegungsgroesse_nach_dem_Glossar()
    {
        using var en = new Kulturvorrichtung("en-US");
        var cut = Zeichne(Bemessen());
        Assert.Contains("Preheating", cut.Markup);
        Assert.Contains("Design capacity Φ_HL + Φ_RH:", cut.Markup);
        Assert.Contains("Heating-up capacity Φ_RH:", cut.Markup);
        Assert.Contains("the ideal peak is not a design capacity", cut.Markup);
    }

    [Fact]
    public void Der_Bedarfsdialog_haengt_die_Gruppe_ein()
    {
        var cut = Render<GebaeudeBedarfDialog>(p => p
            .Add(x => x.Daten, new GebaeudeBedarfDaten { Name = "X", HeizwaermeMwh = 1, IstVdi6007 = true, Aufheizung = Bemessen() }));
        Assert.Single(cut.FindAll(".gebb-aufheizung"));
        var ohne = Render<GebaeudeBedarfDialog>(p => p.Add(x => x.Daten, new GebaeudeBedarfDaten { Name = "X", HeizwaermeMwh = 1 }));
        Assert.Empty(ohne.FindAll(".gebb-aufheizung"));
    }
    /// <summary>Anlagenkopplung AK3 (Festlegung 20): die Rückstufe der Auskunft steht als Zeile in der Gruppe — nur mit Text.</summary>
    [Fact]
    public void Die_Rueckstufe_der_Auskunft_steht_als_Zeile()
    {
        const string RUECK = "Berechnet ohne geschlossenen Kreis (Profilweg): Probe.";
        var mit = Zeichne(new GebaeudeBedarfAufheizDaten
        {
            Zustand = GebaeudeBedarfAufheizDaten.AUS, Zustandtext = "aus", AuslegungsheizlastKw = 10.0,
            AufheizzuschlagKw = 2.0, AuslegungsgroesseKw = 12.0, SpitzeKw = 20.0, Rueckstufe = RUECK,
        });
        Assert.Contains(RUECK, mit.Markup);
        var ohne = Zeichne(Bemessen());
        Assert.DoesNotContain("geschlossenen Kreis", ohne.Markup);
    }
}
