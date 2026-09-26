using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Zonendialog (Gebäudesimulation G3, Welle D2; Mehrzonenkonzept 5.1, 5.3): Feldbestand der
/// Grundform, das Bauteilraster mit Summenfuß, der Rückweg (OK gibt eine Kopie des Arbeitsstands,
/// Abbrechen und Esc geben <c>null</c>), die Prüfregeln der Zone genau einmal im Rückruf der
/// Leiste, der Bauteildialog als Überlagerung — und der Dialog ohne Gaben.
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche Beschriftungen und
/// Zahlen.</para>
/// </summary>
public class ZonenDialogTests : EposBunitContext
{
    public ZonenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Eine Zone mit drei Bauteilen: Außenwand 0,3·100 = 30, Fenster 1,1·20 = 22, Dach 0,2·80 = 16,
    /// ψ·L 5 → H_T = 73 W/K.
    /// </summary>
    private static ZoneDaten Zone() => new()
    {
        Id = 7,
        Bezeichner = "Wohnen",
        Nutzflaeche = 150,
        Bauteile =
        {
            new BauteilDaten { Id = 1, Bezeichner = "Wand Süd", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                               Flaeche = 100, UWert = 0.3, Azimut = 180, PsiL = 5 },
            new BauteilDaten { Id = 2, Bezeichner = "Fenster Süd", Bauteilart = DbWerte.BAUTEILART_FENSTER,
                               Flaeche = 20, UWert = 1.1, Azimut = 180 },
            new BauteilDaten { Id = 3, Bezeichner = "Dach", Bauteilart = DbWerte.BAUTEILART_DACH,
                               Flaeche = 80, UWert = 0.2 }
        }
    };

    private IRenderedComponent<ZonenDialog> Aufbauen(ZoneDaten? zone = null, Action<ZoneDaten?>? geschlossen = null,
                                                      double? nutzflaecheGebaeude = 120, Gebaeudevorgaben? gebaeude = null,
                                                      int zonenzahl = 1, IReadOnlyList<NachbarzoneWahl>? nachbarn = null,
                                                      IReadOnlyList<GegenseiteDaten>? gegenseiten = null)
        => Render<ZonenDialog>(p => p
            .Add(x => x.Zone, zone ?? Zone())
            .Add(x => x.NutzflaecheGebaeude, nutzflaecheGebaeude)
            .Add(x => x.Gebaeude, gebaeude)
            .Add(x => x.Zonenzahl, zonenzahl)
            .Add(x => x.Nachbarzonen, nachbarn ?? Array.Empty<NachbarzoneWahl>())
            .Add(x => x.Gegenseiten, gegenseiten ?? Array.Empty<GegenseiteDaten>())
            .Add(x => x.Geschlossen, z => geschlossen?.Invoke(z)));

    /// <summary>
    /// Ein Gebäude mit 200 m² Nutzfläche und 2,5 m Raumhöhe, Sollwerten 20/16/18/15 °C, 26 °C
    /// Maximaltemperatur, 10 kW Heizleistungsgrenze, Luftwechselrate 0,5 1/h, 4 000 W inneren Gewinnen
    /// und 5,7 Bewohnern — eine Zone mit 150 m² trägt davon 75 %.
    /// </summary>
    private static Gebaeudevorgaben Gebaeude() => new(200, 2.5, 20, 16, 18, 15, 26, null, 10, 0.5, null, null, 4000, 5.7,
                                                      false, null, null, null);

    private static IElement? FeldOderNichts(IElement bereich, string beschriftung)
        => bereich.QuerySelectorAll("label.epos-feld")
                  .FirstOrDefault(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
                  ?.QuerySelector("input, select");

    private static IElement Feld(IElement bereich, string beschriftung)
        => bereich.QuerySelectorAll("label.epos-feld")
                  .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
                  .QuerySelector("input, select")!;

    private static IElement Knopf(IElement bereich, string text)
        => bereich.QuerySelectorAll("button").First(b => b.TextContent.Trim() == text);

    private static IReadOnlyList<IElement> Zeilen(IRenderedComponent<ZonenDialog> cut)
        => cut.FindAll(".epos-zonenbauteil").ToList();

    private static void Ok(IRenderedComponent<ZonenDialog> cut)
        => cut.Find(".epos-zonendialog > .epos-leiste button.epos-knopf--primaer").Click();

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Die_Grundform_traegt_Bezeichnung_Nutzflaeche_und_das_Bauteilraster()
    {
        var cut = Aufbauen();
        IElement wurzel = cut.Find(".epos-zonendialog");

        Assert.Equal("Wohnen", Feld(wurzel, "Bezeichnung").GetAttribute("value"));
        Assert.Equal("150", Feld(wurzel, "Nutzfläche").GetAttribute("value"));

        // Die Zeilen sind nur Anzeige: je Bauteil acht Werte, Öffnen und Entfernen immer sichtbar.
        IReadOnlyList<IElement> zeilen = Zeilen(cut);
        Assert.Equal(3, zeilen.Count);
        Assert.Contains("Außenwand", zeilen[0].TextContent);
        Assert.Contains("Wand Süd", zeilen[0].TextContent);
        Assert.Contains("0,3", zeilen[0].TextContent);
        Assert.Contains("180°", zeilen[0].TextContent);
        Assert.Empty(zeilen[0].QuerySelectorAll("input, select"));
        Assert.Equal(2, zeilen[0].QuerySelectorAll("button").Length);

        // Die leere Neigung zeigt ihre Vorgabe in Klammern: senkrecht an der Wand.
        Assert.Contains("(90°)", zeilen[0].TextContent);

        Assert.Contains("+ Neues Bauteil …", cut.Markup);
    }

    [Fact]
    public void Der_Summenfuss_nennt_die_Gruppen_und_H_T()
    {
        var cut = Aufbauen();

        // H_T = 0,3·100 + 1,1·20 + 0,2·80 + 5 = 73 W/K.
        Assert.Contains("73,0", cut.Markup);
        Assert.Contains("H_T", cut.Markup);
    }

    [Fact]
    public void Die_leere_Nutzflaeche_zeigt_die_des_Gebaeudes_als_Vorgabe()
    {
        ZoneDaten z = Zone();
        z.Nutzflaeche = null;
        var cut = Aufbauen(z);
        IElement feld = Feld(cut.Find(".epos-zonendialog"), "Nutzfläche");

        Assert.Equal("", feld.GetAttribute("value") ?? "");
        Assert.Contains("120", feld.GetAttribute("placeholder") ?? "");
        Assert.Contains("120", cut.Find(".epos-zonendialog > .epos-herleitung").TextContent);
    }

    [Fact]
    public void Ohne_Bauteile_steht_die_leise_Zeile_und_der_Neu_Knopf()
    {
        ZoneDaten z = Zone();
        z.Bauteile.Clear();
        var cut = Aufbauen(z);

        Assert.Empty(Zeilen(cut));
        Assert.NotNull(cut.Find(".epos-zr-neuzeile .epos-leisezeile"));
        Assert.NotNull(Knopf(cut.Find(".epos-zonendialog"), "+ Neues Bauteil …"));
    }

    // =================================================================================
    // Stufe G6b: die Werte der Zone und ihre Vorgaben
    // =================================================================================

    /// <summary>
    /// Ein leeres Feld zeigt den Wert, den die Zone erbt — aus der Vorgabenkaskade des Kerns: vom
    /// Gebäude, anteilig nach der Fläche (150 / 200 = 75 %), abgeleitet (Volumen) oder die
    /// Modellvorgabe; ab zwei Zonen gilt die Leistungsgrenze anteilig.
    /// </summary>
    [Fact]
    public void Ein_leeres_Feld_zeigt_die_Vorgabe_der_Kaskade()
    {
        var cut = Aufbauen(gebaeude: Gebaeude(), zonenzahl: 2);
        IElement w = cut.Find(".epos-zonendialog");

        Assert.Equal("Vorgabe: 2,5", Feld(w, "Raumhöhe").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 375 (Fläche × Raumhöhe)", Feld(w, "Luftvolumen").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 20", Feld(w, "Soll am Tag").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 16", Feld(w, "Nachtabsenkung auf").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 26", Feld(w, "Maximalraumtemperatur").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 3000 (75 % des Gebäudes)", Feld(w, "Interne Wärmegewinne").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 7,5 (75 % des Gebäudes)", Feld(w, "Heizleistungsgrenze").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: " + Gebaeudemodellvorgaben.HeizungStrahlungsanteil.ToString("0.##", CultureInfo.CurrentCulture),
                     Feld(w, "Strahlungsanteil Heizung").GetAttribute("placeholder"));
        // Infiltration und Nutzerlüftung leer, das Gebäude trägt eine Luftwechselrate: sie gilt.
        Assert.Equal("", Feld(w, "Infiltration").GetAttribute("placeholder") ?? "");
        Assert.Contains("Luftwechsel der Zone: 0,5 1/h (Luftwechselrate des Gebäudes).", cut.Markup);
        Assert.Contains("Nachtzeit, Ferien und Kühlung kommen immer vom Gebäude", cut.Markup);
    }

    /// <summary>Bei einer einzigen Zone bleibt die Leistungsgrenze die des Gebäudes (Festlegung 5).</summary>
    [Fact]
    public void Bei_einer_Zone_gilt_die_Grenze_des_Gebaeudes_unveraendert()
    {
        var cut = Aufbauen(gebaeude: Gebaeude(), zonenzahl: 1);

        Assert.Equal("Vorgabe: 10", Feld(cut.Find(".epos-zonendialog"), "Heizleistungsgrenze").GetAttribute("placeholder"));
    }

    [Fact]
    public void Eine_geaenderte_Nutzflaeche_zieht_die_anteiligen_Vorgaben_nach()
    {
        var cut = Aufbauen(gebaeude: Gebaeude(), zonenzahl: 2);

        Feld(cut.Find(".epos-zonendialog"), "Nutzfläche").Input("100");

        Assert.Equal("Vorgabe: 2000 (50 % des Gebäudes)",
                     Feld(cut.Find(".epos-zonendialog"), "Interne Wärmegewinne").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 250 (Fläche × Raumhöhe)",
                     Feld(cut.Find(".epos-zonendialog"), "Luftvolumen").GetAttribute("placeholder"));
    }

    /// <summary>Eine unbeheizte Zone schwingt frei: Sollwerte und Heizung stehen nicht da, eine Zeile sagt warum.</summary>
    [Fact]
    public void Unbeheizt_blendet_Sollwerte_und_Heizung_aus()
    {
        ZoneDaten? zurueck = null;
        var cut = Aufbauen(geschlossen: z => zurueck = z, gebaeude: Gebaeude());
        IElement w = cut.Find(".epos-zonendialog");
        Assert.NotNull(FeldOderNichts(w, "Soll am Tag"));

        cut.Find(".epos-zonendialog input.epos-schalter-kasten").Change(false);

        w = cut.Find(".epos-zonendialog");
        foreach (string feld in new[] { "Soll am Tag", "Nachtabsenkung auf", "Wochenendabsenkung", "Soll in Ferien",
                                        "Maximalraumtemperatur", "Strahlungsanteil Heizung", "Heizleistungsgrenze" })
            Assert.Null(FeldOderNichts(w, feld));
        Assert.NotNull(FeldOderNichts(w, "Interne Wärmegewinne"));
        Assert.Contains("schwingt frei", cut.Markup);

        Ok(cut);
        Assert.NotNull(zurueck);
        Assert.False(zurueck!.IstBeheizt);
    }

    [Fact]
    public void OK_traegt_die_Werte_der_Zone_zurueck()
    {
        ZoneDaten? zurueck = null;
        var cut = Aufbauen(geschlossen: z => zurueck = z, gebaeude: Gebaeude());
        IElement w = cut.Find(".epos-zonendialog");

        Feld(w, "Raumhöhe").Input("3");
        Feld(w, "Soll am Tag").Input("22");
        Feld(w, "Bewohner").Input("4");
        Feld(w, "Heizleistungsgrenze").Input("6");
        Ok(cut);

        Assert.NotNull(zurueck);
        Assert.Equal(3.0, zurueck!.Raumhoehe);
        Assert.Equal(22.0, zurueck.SollTag);
        Assert.Equal(4.0, zurueck.Bewohner);
        Assert.Equal(6.0, zurueck.HeizleistungMaxKw);
        Assert.Null(zurueck.Volumen);
        Assert.True(zurueck.IstBeheizt);
    }

    [Theory]
    [InlineData("Raumhöhe", "Die Raumhöhe der Zone „Wohnen“ muss größer als null sein.")]
    [InlineData("Luftvolumen", "Das Luftvolumen der Zone „Wohnen“ muss größer als null sein.")]
    public void Raumhoehe_und_Volumen_null_werden_benannt_abgelehnt(string feld, string meldung)
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        Feld(cut.Find(".epos-zonendialog"), feld).Input("0");
        Ok(cut);

        Assert.False(gerufen);
        Assert.Equal(meldung, cut.Instance.Meldung);
    }

    // =================================================================================
    // Stufe G6b: Trennflächen — Nachbar und Gegenseite
    // =================================================================================

    /// <summary>
    /// Eine Trennfläche nennt ihren Nachbarn; die Trennfläche, die eine ANDERE Zone mit dieser führt,
    /// steht gespiegelt und nur zum Lesen da („geführt von …"), ohne Knöpfe und ohne Beitrag zu H_T.
    /// </summary>
    [Fact]
    public void Trennflaechen_zeigen_den_Nachbarn_und_die_Gegenseite_nur_lesend()
    {
        ZoneDaten z = Zone();
        z.Bauteile.Add(new BauteilDaten { Id = 9, Bezeichner = "Wand Treppenhaus", Bauteilart = DbWerte.BAUTEILART_INNENWAND,
                                          Flaeche = 12, UWert = 1.5, Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, IdNachbarzone = -5 });
        var gegen = new GegenseiteDaten(-5, "Treppenhaus", new BauteilDaten
        {
            Id = 21, Bezeichner = "Decke Keller", Bauteilart = DbWerte.BAUTEILART_DECKE, Flaeche = 40, UWert = 0.8,
            Neigung = 0, Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, IdNachbarzone = 7
        });
        var cut = Aufbauen(z, nachbarn: new[] { new NachbarzoneWahl(-5, "Treppenhaus") }, gegenseiten: new[] { gegen });

        IElement trenn = Zeilen(cut)[3];
        Assert.Contains("Nachbarzone", trenn.TextContent);
        Assert.Contains("Treppenhaus", trenn.TextContent);
        Assert.Contains("—", Zeilen(cut)[0].TextContent);

        IElement gegenseite = cut.Find(".epos-zonengegenseite");
        Assert.Contains("Decke Keller", gegenseite.TextContent);
        Assert.Contains("geführt von Treppenhaus", gegenseite.TextContent);
        Assert.Contains("180°", gegenseite.TextContent);            // Neigung 0° gespiegelt
        Assert.Empty(gegenseite.QuerySelectorAll("button, input, select"));
        Assert.Contains("nur zum Lesen", cut.Markup);

        // H_T zählt die Trennflächen nicht (Randbedingung Nachbarzone): unverändert 73 W/K.
        Assert.Contains("73,0", cut.Markup);
    }

    [Fact]
    public void Der_Bauteildialog_bietet_die_Nachbarzonen_der_Zone()
    {
        var cut = Aufbauen(nachbarn: new[] { new NachbarzoneWahl(-5, "Treppenhaus") });

        Knopf(cut.Find(".epos-zonendialog"), "+ Neues Bauteil …").Click();
        IElement bauteil = cut.Find(".epos-bauteildialog");

        Assert.Contains("Nachbarzone", Feld(bauteil, "Randbedingung").TextContent);
        Feld(bauteil, "Randbedingung").Change("4");
        Assert.Contains("Treppenhaus", Feld(cut.Find(".epos-bauteildialog"), "Nachbarzone").TextContent);
    }

    // =================================================================================
    // Rückweg
    // =================================================================================

    [Fact]
    public void OK_gibt_den_Arbeitsstand_zurueck_und_laesst_die_Vorlage_unberuehrt()
    {
        ZoneDaten vorlage = Zone();
        ZoneDaten? zurueck = null;
        bool gerufen = false;
        var cut = Aufbauen(vorlage, z => { gerufen = true; zurueck = z; });

        Feld(cut.Find(".epos-zonendialog"), "Bezeichnung").Input("  Wohnen EG  ");
        Feld(cut.Find(".epos-zonendialog"), "Nutzfläche").Input("140");
        Ok(cut);

        Assert.True(gerufen);
        Assert.NotNull(zurueck);
        Assert.Equal("Wohnen EG", zurueck!.Bezeichner);
        Assert.Equal(140, zurueck.Nutzflaeche);
        Assert.Equal(3, zurueck.Bauteile.Count);
        Assert.Equal("Wohnen", vorlage.Bezeichner);
        Assert.Equal(150, vorlage.Nutzflaeche);
    }

    [Fact]
    public void Abbrechen_und_Esc_geben_null()
    {
        var antworten = new List<ZoneDaten?>();
        var cut = Aufbauen(geschlossen: z => antworten.Add(z));

        Knopf(cut.Find(".epos-zonendialog > .epos-leiste"), "Abbrechen").Click();
        cut.Find(".epos-zonendialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(2, antworten.Count);
        Assert.All(antworten, Assert.Null);
    }

    [Fact]
    public void Entfernen_nimmt_die_Zeile_nur_aus_dem_Arbeitsstand()
    {
        ZoneDaten vorlage = Zone();
        var cut = Aufbauen(vorlage);

        Zeilen(cut)[1].QuerySelectorAll("button")[1].Click();

        Assert.Equal(2, cut.Instance.Arbeitsstand.Bauteile.Count);
        Assert.DoesNotContain(cut.Instance.Arbeitsstand.Bauteile, b => b.Bezeichner == "Fenster Süd");
        Assert.Equal(3, vorlage.Bauteile.Count);
    }

    // =================================================================================
    // Prüfregeln (Mehrzonenkonzept 5.3, Ebene Zone)
    // =================================================================================

    [Fact]
    public void Ohne_Namen_bleibt_der_Dialog_offen_und_nennt_den_Grund()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        Feld(cut.Find(".epos-zonendialog"), "Bezeichnung").Input("   ");
        Ok(cut);

        Assert.False(gerufen);
        Assert.Equal("Die Zone braucht einen Namen.", cut.Instance.Meldung);
        Assert.Contains("Die Zone braucht einen Namen.", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Eine_Nutzflaeche_von_null_wird_benannt_abgelehnt()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        Feld(cut.Find(".epos-zonendialog"), "Nutzfläche").Input("0");
        Ok(cut);

        Assert.False(gerufen);
        Assert.Contains("Wohnen", cut.Instance.Meldung);
        Assert.Contains("größer als null", cut.Instance.Meldung);
    }

    // =================================================================================
    // Der Bauteildialog als Überlagerung
    // =================================================================================

    [Fact]
    public void Oeffnen_zeigt_den_Bauteildialog_und_sein_OK_ersetzt_die_Zeile()
    {
        var cut = Aufbauen();

        Zeilen(cut)[0].QuerySelectorAll("button")[0].Click();
        Assert.True(cut.Instance.BauteilOffen);

        IElement bauteil = cut.Find(".epos-bauteildialog");
        Feld(bauteil, "Fläche").Input("110");
        bauteil.QuerySelectorAll(".epos-leiste button.epos-knopf--primaer").Last().Click();

        Assert.False(cut.Instance.BauteilOffen);
        Assert.Equal(110, cut.Instance.Arbeitsstand.Bauteile[0].Flaeche);
        Assert.Equal(3, cut.Instance.Arbeitsstand.Bauteile.Count);
    }

    [Fact]
    public void Ein_neues_Bauteil_bekommt_eine_negative_Id_und_wird_angehaengt()
    {
        var cut = Aufbauen();

        Knopf(cut.Find(".epos-zonendialog"), "+ Neues Bauteil …").Click();
        IElement bauteil = cut.Find(".epos-bauteildialog");
        Feld(bauteil, "Bauteilart").Change("2");                 // Bodenplatte
        Feld(bauteil, "Bezeichnung").Input("Bodenplatte");
        Feld(bauteil, "Fläche").Input("80");
        Feld(bauteil, "U-Wert").Input("0,35");
        bauteil.QuerySelectorAll(".epos-leiste button.epos-knopf--primaer").Last().Click();

        BauteilDaten neu = cut.Instance.Arbeitsstand.Bauteile.Last();
        Assert.Equal(4, cut.Instance.Arbeitsstand.Bauteile.Count);
        Assert.True(neu.Id < 0);
        Assert.Equal(DbWerte.BAUTEILART_BODENPLATTE, neu.Bauteilart);
        Assert.Equal(DbWerte.HERKUNFT_MANUELL, neu.Herkunft);
    }

    /// <summary>Esc kaskadiert: Solange der Bauteildialog steht, schließt Esc nur ihn.</summary>
    [Fact]
    public void Esc_schliesst_erst_den_Bauteildialog()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        Zeilen(cut)[0].QuerySelectorAll("button")[0].Click();
        cut.Find(".epos-bauteildialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(cut.Instance.BauteilOffen);
        Assert.False(gerufen);
    }

    // =================================================================================
    // Ohne Gaben und der Hilfe-Assistent
    // =================================================================================

    [Fact]
    public void Ohne_Gaben_steht_eine_leere_Zone()
    {
        var cut = Render<ZonenDialog>();

        Assert.Empty(Zeilen(cut));
        Assert.Contains("Zone", cut.Find(".epos-dialog-titel").TextContent);
        Ok(cut);
        Assert.Equal("Die Zone braucht einen Namen.", cut.Instance.Meldung);
    }

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die Sichtklasse
    /// <c>ZonenKiSicht</c>: Bezeichnung und Nutzfläche sind setzbar, die Bauteile nur lesbar.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an()
    {
        var cut = Aufbauen();

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.ZONE));

        KiFeldzugang name = KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "bezeichnung");
        Assert.NotNull(name);
        Assert.Equal("Wohnen", name.Lesen());
        name.Setzen("Keller");
        cut.Render();
        Assert.Equal("Keller", cut.Instance.Arbeitsstand.Bezeichner);

        KiFeldzugang flaeche = KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "nutzflaeche");
        Assert.NotNull(flaeche);
        Assert.True(flaeche.Setzbar);

        // Stufe G6b: die Werte der Zone sind setzbar - derselbe Weg wie die Tastatur.
        KiFeldzugang soll = KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "soll_tag");
        Assert.NotNull(soll);
        soll.Setzen(21.5);
        KiFeldzugang beheizt = KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "beheizt");
        Assert.NotNull(beheizt);
        Assert.Equal(true, beheizt.Lesen());
        beheizt.Setzen(false);
        cut.Render();
        Assert.Equal(21.5, cut.Instance.Arbeitsstand.SollTag);
        Assert.False(cut.Instance.Arbeitsstand.IstBeheizt);
    }
}
