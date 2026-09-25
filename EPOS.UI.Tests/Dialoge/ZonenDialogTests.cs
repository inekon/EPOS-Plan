using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
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
                                                      double? nutzflaecheGebaeude = 120)
        => Render<ZonenDialog>(p => p
            .Add(x => x.Zone, zone ?? Zone())
            .Add(x => x.NutzflaecheGebaeude, nutzflaecheGebaeude)
            .Add(x => x.Geschlossen, z => geschlossen?.Invoke(z)));

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
    }
}
