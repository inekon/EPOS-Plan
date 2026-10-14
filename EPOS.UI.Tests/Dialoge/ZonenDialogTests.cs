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
        Assert.Equal("Vorgabe: 26", Feld(w, "Maximalraumtemperatur").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 7,5 (75 % des Gebäudes)", Feld(w, "Heizleistungsgrenze").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: " + Gebaeudemodellvorgaben.HeizungStrahlungsanteil.ToString("0.##", CultureInfo.CurrentCulture),
                     Feld(w, "Strahlungsanteil Heizung").GetAttribute("placeholder"));
        // Stufe KP2, Welle U4: Sollwerte, Lüftung und Gewinne stehen in der Zonenmatrix; ohne Weg der
        // Konditionierung nennt ihr Platzhalter die Vorgabenkaskade („Vorgabe 20"), anteilig nach der Fläche.
        Assert.Equal("Vorgabe 20", Feld(w, "Heizen · Tag").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 16", Feld(w, "Heizen · Nacht").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 3000", Feld(w, "Geräte · Nennwert").GetAttribute("placeholder"));
        // Infiltration und Nutzerlüftung leer, das Gebäude trägt nur eine Luftwechselrate: „wie Gebäude".
        Assert.Equal("wie Gebäude", Feld(w, "Lüftung · Infiltration").GetAttribute("placeholder"));
        Assert.Contains("Luftwechsel der Zone: 0,5 1/h (Luftwechselrate des Gebäudes).", cut.Markup);
        Assert.Contains("Ferien und Kühlung kommen immer vom Gebäude; Sollwerte, Nachtzeit und Lüftung stehen in der Vorgabe-Matrix der Zone", cut.Markup);
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

        Assert.Equal("Vorgabe 2000",
                     Feld(cut.Find(".epos-zonendialog"), "Geräte · Nennwert").GetAttribute("placeholder"));
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
        Assert.NotNull(FeldOderNichts(w, "Heizen · Tag"));

        cut.Find(".epos-zonendialog input.epos-schalter-kasten").Change(false);

        // Stufe KP2, Welle U4: Die Heizspalte der Zonenmatrix steht weich gesperrt mit Grund da.
        w = cut.Find(".epos-zonendialog");
        foreach (string feld in new[] { "Heizen · Tag", "Heizen · Nacht", "Soll am Wochenende (ganztägig)",
                                        "Soll in Ferien (ganztägig)", "Maximalraumtemperatur", "Strahlungsanteil Heizung",
                                        "Heizleistungsgrenze" })
            Assert.Null(FeldOderNichts(w, feld));
        Assert.Contains(cut.FindAll(".epos-kond-gesperrt"),
                        k => k.GetAttribute("title")!.StartsWith("Eine unbeheizte Zone hat weder Heiz- noch Kühlwerte", StringComparison.Ordinal));
        Assert.NotNull(FeldOderNichts(w, "Geräte · Nennwert"));
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
        Feld(w, "Heizen · Tag").Input("22");
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
    // Die Zonenmatrix (Stufe KP2, Welle U4; Teilkonzept Konditionierungsprofile 3.4, 7.3)
    // =================================================================================

    /// <summary>
    /// Das Gebäude für die Zonenmatrix: 200 m², Sollwerte 20/16 °C, getrennte Lüftung 0,3/0,4 1/h,
    /// 4 000 W innere Gewinne, Personen-Nennwert 1 000 W mit 50 % am Tag — der Arbeitsstand des
    /// Editors, gebaut über die Bearbeitung des Gebäudes und den reinen Weg des Kerns.
    /// </summary>
    private static (GebaeudeArbeitsstand Arbeit, KonditionierungWeg Weg) Matrixgebaeude(bool heizkalender = false,
                                                                                         bool gesamtangabe = false)
    {
        var d = new GebaeudeKatalogDaten
        {
            Name = "Haus", WohnflaecheGesamt = 200, SollTag = 20, NachtAbsenkung = 16,
            LuftwechselInfiltration = gesamtangabe ? null : 0.3, LuftwechselNutzer = gesamtangabe ? null : 0.4,
            Luftwechselrate = 0.7, Waermegewinne = 4000, KuehlungAktiv = true, KuehlSollwert = 26,
            Konditionierung = new KonditionierungDaten()
        };
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(d, neu: false);
        KonditionierungWeg weg = KonditionierungHuelle.ReinerWeg(Kalendereigentuemer.Gebaeude, projekt: true);
        var gebaeude = new KonditionierungBearbeitung(arbeit, () => weg);
        Assert.True(gebaeude.WertSetzen(KonditionierungGroesse.Personen, KonditionierungZeile.Nennwert, 1000));
        Assert.True(gebaeude.WertSetzen(KonditionierungGroesse.Personen, KonditionierungZeile.Tag, 50));
        if (heizkalender) Assert.True(gebaeude.Anlegen(KonditionierungGroesse.Heizen));
        return (arbeit, weg);
    }

    private IRenderedComponent<ZonenDialog> MitMatrix(GebaeudeArbeitsstand arbeit, KonditionierungWeg weg,
                                                      Action<ZoneDaten?>? geschlossen = null, ZoneDaten? zone = null)
        => Render<ZonenDialog>(p => p
            .Add(x => x.Zone, zone ?? new ZoneDaten { Id = 7, Bezeichner = "Wohnen", Nutzflaeche = 150 })
            .Add(x => x.NutzflaecheGebaeude, 200.0)
            .Add(x => x.Gebaeudestand, arbeit)
            .Add(x => x.Konditionierung, weg)
            .Add(x => x.Geschlossen, z => geschlossen?.Invoke(z)));

    private static IElement Zustandszeile(IRenderedComponent<ZonenDialog> cut, KonditionierungGroesse g)
        => cut.Find(".epos-kond-zonenzeile[data-groesse=\"" + (int)g + "\"]");

    private static IElement Matrixzelle(IRenderedComponent<ZonenDialog> cut, KonditionierungGroesse g, KonditionierungZeile z)
        => cut.Find("td.epos-kond-zelle[data-groesse=\"" + (int)g + "\"][data-zeile=\"" + (int)z + "\"]");

    [Fact]
    public void Die_Zonenmatrix_nennt_die_geerbten_Werte_als_Platzhalter()
    {
        (GebaeudeArbeitsstand arbeit, KonditionierungWeg weg) = Matrixgebaeude();
        var cut = MitMatrix(arbeit, weg);
        IElement w = cut.Find(".epos-zonendialog");

        Assert.Equal("Vorgabe 20", Feld(w, "Heizen · Tag").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 16", Feld(w, "Heizen · Nacht").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 0,3", Feld(w, "Lüftung · Infiltration").GetAttribute("placeholder"));
        // Die inneren Gewinne und der Personen-Nennwert im Flächenanteil (150 / 200 = 75 %, Konzept 3.4);
        // die Gewinne des Gebäudes nach P1: 4 000 W − 500 W Personenmittel = 3 500 W, davon 75 %.
        Assert.Equal("Vorgabe 2625", Feld(w, "Geräte · Nennwert").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 750", Feld(w, "Personen · Nennwert").GetAttribute("placeholder"));
        Assert.Equal("Vorgabe 50 %", Feld(w, "Personen · Tag").GetAttribute("placeholder"));
        // Ohne Wert am Gebäude: „wie Gebäude".
        Assert.Equal("wie Gebäude", Feld(w, "Lüftung · Wochenende").GetAttribute("placeholder"));
        Assert.Contains("Eine leere Zelle gilt wie im Gebäude", cut.Markup);

        // Eine andere Nutzfläche schlüsselt neu.
        Feld(w, "Nutzfläche").Input("100");
        Assert.Equal("Vorgabe 500", Feld(cut.Find(".epos-zonendialog"), "Personen · Nennwert").GetAttribute("placeholder"));
    }

    [Fact]
    public void Eine_eigene_Zelle_ueberschreibt_und_OK_traegt_sie_samt_Konditionierung_zurueck()
    {
        (GebaeudeArbeitsstand arbeit, KonditionierungWeg weg) = Matrixgebaeude();
        ZoneDaten? zurueck = null;
        var cut = MitMatrix(arbeit, weg, z => zurueck = z);

        Feld(cut.Find(".epos-zonendialog"), "Heizen · Tag").Input("22");
        Feld(cut.Find(".epos-zonendialog"), "Personen · Nennwert").Input("300");
        Feld(cut.Find(".epos-zonendialog"), "Lüftung · Nachtauskühlung").Input("1");
        Assert.Equal("", cut.Instance.Meldung);
        Ok(cut);

        Assert.NotNull(zurueck);
        Assert.Equal(22.0, zurueck!.SollTag);
        Assert.NotNull(zurueck.Konditionierung);
        Assert.True(zurueck.Konditionierung!.Fassung > 0);
        Assert.Equal(300.0, zurueck.Konditionierung.Spalte(KonditionierungGroesse.Personen).Nennwert.Wert);
        Assert.Equal(1.0, zurueck.Konditionierung.Spalte(KonditionierungGroesse.Lueftung).Nacht.Wert);
        // Das Gebäude liest der Zonendialog nur.
        Assert.Equal(20.0, arbeit.Stand.SollTag);
        Assert.Null(arbeit.Stand.Konditionierung!.Spalte(KonditionierungGroesse.Lueftung).Nacht.Wert);
    }

    [Fact]
    public void Eine_geleerte_Zelle_erbt_wieder()
    {
        (GebaeudeArbeitsstand arbeit, KonditionierungWeg weg) = Matrixgebaeude();
        ZoneDaten? zurueck = null;
        var cut = MitMatrix(arbeit, weg, z => zurueck = z,
                            new ZoneDaten { Id = 7, Bezeichner = "Wohnen", Nutzflaeche = 150, SollTag = 22 });

        IElement tag = Feld(cut.Find(".epos-zonendialog"), "Heizen · Tag");
        Assert.Equal("22", tag.GetAttribute("value"));
        tag.Input("");
        Assert.Equal("Vorgabe 20", Feld(cut.Find(".epos-zonendialog"), "Heizen · Tag").GetAttribute("placeholder"));
        Ok(cut);
        Assert.Null(zurueck!.SollTag);
    }

    [Fact]
    public void Vom_Gebaeude_steht_mit_Grund_und_uebernehmen_legt_eine_eigene_Kopie_an()
    {
        (GebaeudeArbeitsstand arbeit, KonditionierungWeg weg) = Matrixgebaeude(heizkalender: true);
        ZoneDaten? zurueck = null;
        var cut = MitMatrix(arbeit, weg, z => zurueck = z);

        IElement zeile = Zustandszeile(cut, KonditionierungGroesse.Heizen);
        Assert.Contains("vom Gebäude", zeile.TextContent);
        Assert.Contains("Die Zone folgt dem Kalender des Gebäudes", zeile.TextContent);
        // Die Zellen der Größe sind ohne Wirkung - leise, der Grund am Element.
        IElement zelle = Matrixzelle(cut, KonditionierungGroesse.Heizen, KonditionierungZeile.Tag);
        Assert.Contains("epos-kond--ohnewirkung", zelle.ClassName);
        Assert.Equal("Ohne Wirkung, solange die Zone dem Kalender des Gebäudes folgt.", zelle.GetAttribute("title"));
        // Die übrigen Größen folgen der Matrix.
        Assert.Contains("aus der Matrix", Zustandszeile(cut, KonditionierungGroesse.Lueftung).TextContent);

        zeile.QuerySelectorAll("button").Single(b => b.TextContent.Trim() == "Vom Gebäude übernehmen und anpassen").Click();

        zeile = Zustandszeile(cut, KonditionierungGroesse.Heizen);
        Assert.Contains("eigener Kalender", zeile.TextContent);
        Assert.Empty(zeile.QuerySelectorAll("button"));
        Assert.DoesNotContain("epos-kond--ohnewirkung",
                              Matrixzelle(cut, KonditionierungGroesse.Heizen, KonditionierungZeile.Tag).ClassName);

        Ok(cut);
        KonditionierungKalender k = zurueck!.Konditionierung!.Spalte(KonditionierungGroesse.Heizen).Kalender;
        Assert.Equal(KonditionierungZustand.Angelegt, k.Zustand);
        Assert.Equal(arbeit.Stand.Konditionierung!.Spalte(KonditionierungGroesse.Heizen).Kalender.Woche, k.Woche);
    }

    /// <summary>
    /// KU3-3 (E67/E68): Die Kühlspalte der Zone ist bedienbar, solange die Zone gekühlt wird (eigener
    /// Schalter, leer = der des Gebäudes); ihre Tagzelle schreibt den Kühlsollwert der Zone. Schaltet die
    /// Zone die Kühlung aus, ist die Spalte weich gesperrt und nennt den Grund.
    /// </summary>
    [Fact]
    public void Die_Kuehlspalte_der_Zone_folgt_dem_Kuehlschalter_der_Zone()
    {
        (GebaeudeArbeitsstand arbeit, KonditionierungWeg weg) = Matrixgebaeude();
        var cut = MitMatrix(arbeit, weg);

        IElement tag = Matrixzelle(cut, KonditionierungGroesse.Kuehlen, KonditionierungZeile.Tag);
        Assert.Null(tag.QuerySelector(".epos-kond-gesperrt"));
        IElement eingabe = Feld(cut.Find(".epos-zonendialog"), "Kühlen · Tag");
        Assert.Contains("26", eingabe.GetAttribute("placeholder"));
        eingabe.Input("24");
        Assert.Equal(24.0, cut.Instance.Arbeitsstand.KuehlSollwert);
        Assert.NotEmpty(cut.FindAll(".epos-kond-zonenzeile[data-groesse=\"1\"]"));

        cut.Find(".epos-zonenkuehlung select").Change("2");
        IElement kuehlen = Matrixzelle(cut, KonditionierungGroesse.Kuehlen, KonditionierungZeile.Tag)
                               .QuerySelector(".epos-kond-gesperrt")!;
        Assert.Equal("true", kuehlen.GetAttribute("aria-disabled"));
        Assert.StartsWith("Die Kühlspalte wirkt nur mit Kühlbetrieb", kuehlen.GetAttribute("title"));
        Assert.Empty(cut.FindAll(".epos-kond-zonenzeile[data-groesse=\"1\"]"));
        Assert.Empty(cut.FindAll(".epos-kond-karte[data-groesse=\"1\"]"));
    }

    [Fact]
    public void Die_Aufteilung_der_Gesamtangabe_bleibt_am_Gebaeude()
    {
        (GebaeudeArbeitsstand arbeit, KonditionierungWeg weg) = Matrixgebaeude(gesamtangabe: true);
        var cut = MitMatrix(arbeit, weg);

        Feld(cut.Find(".epos-zonendialog"), "Lüftung · Nachtauskühlung").Input("1");

        Assert.StartsWith("Die Lüftung des Gebäudes steht als Gesamtangabe", cut.Instance.Meldung);
        Assert.Null(arbeit.Stand.LuftwechselInfiltration);
        Assert.Equal(0.7, arbeit.Stand.Luftwechselrate);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Ohne_Weg_bleiben_die_Bestandszellen_der_Zone_bedienbar()
    {
        ZoneDaten? zurueck = null;
        var cut = Aufbauen(geschlossen: z => zurueck = z, gebaeude: Gebaeude());

        // Ohne Konditionierungstabellen nennt die Matrix ihren Grund; die neuen Zellen zeigen „—".
        Assert.Contains("Konditionierung", cut.Find(".epos-kond-zone .epos-kond-sperrzeile").TextContent);
        Assert.Null(FeldOderNichts(cut.Find(".epos-zonendialog"), "Personen · Nennwert"));
        Feld(cut.Find(".epos-zonendialog"), "Lüftung · Nutzerlüftung").Input("0,5");
        Ok(cut);
        Assert.Equal(0.5, zurueck!.LuftwechselNutzer);
        Assert.Null(zurueck.Konditionierung);
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

    /// <summary>
    /// <b>Die Zonenmatrix beim Assistenten</b> (Stufe KP2, Welle U4): Die Felder der Zonenkarte lesen leer
    /// als „wie Gebäude" (<c>null</c>) und setzen über dieselbe Bearbeitung wie die Zellen — eine eigene
    /// Zelle der Zone, das Gebäude bleibt; eine Bestandszelle geht ebenso über ihre Zelle, die Kühlspalte
    /// kennt die Zone nicht, und eine Ablehnung nennt den Grund des Reiters.
    /// </summary>
    [Fact]
    public void Der_Assistent_setzt_die_Zonenmatrix_ueber_die_Bearbeitung()
    {
        (GebaeudeArbeitsstand arbeit, KonditionierungWeg weg) = Matrixgebaeude();
        var cut = MitMatrix(arbeit, weg);

        KiFeldzugang personen = KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "kond_personen_nennwert");
        Assert.NotNull(personen);
        Assert.True(personen.Setzbar);
        Assert.Null(personen.Lesen());
        personen.Setzen(300.0);
        cut.Render();
        Assert.Equal(300.0, personen.Lesen());
        Assert.Equal(300.0, cut.Instance.Arbeitsstand.Konditionierung!
                                .Spalte(KonditionierungGroesse.Personen).Zelle(KonditionierungZeile.Nennwert).Wert);
        Assert.Equal(1000.0, arbeit.Stand.Konditionierung!
                                .Spalte(KonditionierungGroesse.Personen).Zelle(KonditionierungZeile.Nennwert).Wert);

        // Das Nachtfenster der Heizspalte steht an der Zone in der Zelle: erst beide Grenzen gehen an den Weg.
        KiFeldzugang von = KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "kond_heizen_nacht_von");
        KiFeldzugang bis = KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "kond_heizen_nacht_bis");
        Assert.NotNull(von);
        Assert.NotNull(bis);
        von.Setzen(21);
        Assert.Null(cut.Instance.Konditionierungsbearbeitung.Zeiten(KonditionierungGroesse.Heizen, KonditionierungZeile.Nacht).Von);
        bis.Setzen(5);
        Assert.Equal(((int?)21, (int?)5), cut.Instance.Konditionierungsbearbeitung.Zeiten(KonditionierungGroesse.Heizen, KonditionierungZeile.Nacht));

        // Eine Bestandszelle: soll_tag geht über die Zelle der Zone - dieselbe wie das Feld der Matrix.
        KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "soll_tag")!.Setzen(22.0);
        cut.Render();
        Assert.Equal(22.0, cut.Instance.Arbeitsstand.SollTag);
        Assert.Equal("22", Matrixzelle(cut, KonditionierungGroesse.Heizen, KonditionierungZeile.Tag)
                               .QuerySelector("input")!.GetAttribute("value"));

        // Die Kühlspalte kennt die Zone nicht (Zonenregel bis KU3).
        Assert.Null(KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "kond_kuehlen_nacht_von"));
    }

    /// <summary>
    /// Die Gegenprobe der Zonenregel beim Assistenten: In einer unbeheizten Zone lehnt die Heizspalte ab —
    /// mit dem Grund des Reiters, nichts wird gesetzt.
    /// </summary>
    [Fact]
    public void Der_Assistent_nennt_die_Zonenregel_einer_unbeheizten_Zone()
    {
        (GebaeudeArbeitsstand arbeit, KonditionierungWeg weg) = Matrixgebaeude();
        var cut = MitMatrix(arbeit, weg, zone: new ZoneDaten { Id = 7, Bezeichner = "Lager", Nutzflaeche = 50, IstBeheizt = false });

        KiFeldzugang heizen = KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "kond_heizen_nacht_von")!;
        heizen.Setzen(22);
        var fehler = Assert.Throws<InvalidOperationException>(
            () => KiMaskenbruecke.Feldzugang(KiMaskennamen.ZONE, "kond_heizen_nacht_bis")!.Setzen(6));
        Assert.Contains("nicht beheizt", fehler.Message);
        Assert.Null(cut.Instance.Arbeitsstand.Konditionierung);
    }
}
