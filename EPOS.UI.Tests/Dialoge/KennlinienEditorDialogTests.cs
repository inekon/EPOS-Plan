using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Kennliniendaten bearbeiten (iU9-W7.2). Soll ist die Feldkarte von
/// <c>Kenndaten</c>: die Vorlaufliste, das Raster, die Gruppe „Neue Stützstelle"
/// mit drei Feldern und vier Knöpfe (Daten übernehmen, Neue Vorlauftemperatur,
/// OK, Abbruch).
/// </summary>
public class KennlinienEditorDialogTests : BunitContext
{
    public KennlinienEditorDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static List<KennlinienZeile> Proben() => new()
    {
        new KennlinienZeile { Id = 1, Vorlauf = 35, Temperatur = -7, Cop = 2.8, Ptherm = 6.1 },
        new KennlinienZeile { Id = 2, Vorlauf = 35, Temperatur = 2,  Cop = 3.6, Ptherm = 7.4 },
        new KennlinienZeile { Id = 3, Vorlauf = 55, Temperatur = -7, Cop = 1.9, Ptherm = 5.2 }
    };

    private IRenderedComponent<KennlinienEditorDialog> Aufbauen(
        List<KennlinienZeile>? zeilen = null,
        bool nurLesen = false,
        Action<IReadOnlyList<KennlinienZeile>?>? geschlossen = null)
        => Render<KennlinienEditorDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? Proben())
            .Add(x => x.IdWp, 42)
            .Add(x => x.NurLesen, nurLesen)
            .Add(x => x.Geschlossen, l => geschlossen?.Invoke(l)));

    /// <summary>Die Stützstellenzeilen des Zeilenrasters.</summary>
    private static int Zeilenzahl(IRenderedComponent<KennlinienEditorDialog> cut)
        => cut.FindAll(".epos-zr-zeile").Count;

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        // Gruppe "Neue Stuetzstelle": Temperatur, COP, Ptherm.
        var gruppe = cut.Find(".epos-gruppenkopf-koerper");
        Assert.Equal(3, gruppe.QuerySelectorAll("input").Length);

        var knopftexte = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("Daten übernehmen", knopftexte);
        Assert.Contains("Neue Vorlauftemperatur", knopftexte);
        Assert.Contains("OK", knopftexte);
        Assert.Contains("Abbruch", knopftexte);
    }

    [Fact]
    public void Die_Beschriftungen_stehen_wie_im_Designer()
    {
        var cut = Aufbauen();
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();

        Assert.Contains("Temperatur", texte);
        Assert.Contains("COP", texte);
        Assert.Contains("Ptherm", texte);
        Assert.Contains("Neue Vorlauftemperatur", texte);
        Assert.Equal("Neue Stützstelle", cut.Find(".epos-gruppenkopf-titel").TextContent.Trim());
    }

    /// <summary>Die Maske ist lokalisiert (12 englische Texte, W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<KennlinienEditorDialog>(p => p
            .Add(x => x.Zeilen, Proben())
            .Add(x => x.TitelText, "Characteristics")
            .Add(x => x.LabelTemperatur, "Temperature")
            .Add(x => x.BtnItemNeuText, "Accept data")
            .Add(x => x.AbbrechenText, "Abort"));

        Assert.Equal("Characteristics", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Temperature", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
        var knoepfe = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("Accept data", knoepfe);
        Assert.Contains("Abort", knoepfe);
    }

    // =================================================================================
    // Vorlaufstufen
    // =================================================================================

    [Fact]
    public void Die_Vorlaufliste_kommt_aus_den_Zeilen()
    {
        var cut = Aufbauen();
        var stufen = cut.FindAll(".epos-raster tbody tr td:last-child")
                        .Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "35", "55" }, stufen);
    }

    [Fact]
    public void Vorbelegt_ist_der_Vorlauf_der_ersten_Zeile()
    {
        // Konstruktor:19 filterte auf dr[2] der ERSTEN Zeile, nicht auf die kleinste Stufe.
        var zeilen = new List<KennlinienZeile>
        {
            new() { Id = 7, Vorlauf = 55, Temperatur = 0, Cop = 2, Ptherm = 5 },
            new() { Id = 8, Vorlauf = 35, Temperatur = 0, Cop = 3, Ptherm = 6 }
        };
        var cut = Aufbauen(zeilen);

        Assert.Equal(55, cut.Instance.Vorlauf);
        Assert.Equal(1, Zeilenzahl(cut));
    }

    [Fact]
    public void Die_Wahl_einer_Stufe_filtert_das_Raster()
    {
        var cut = Aufbauen();
        Assert.Equal(2, Zeilenzahl(cut));                       // Stufe 35

        cut.FindAll(".epos-raster tbody tr button")[1].Click();  // Stufe 55
        Assert.Equal(1, Zeilenzahl(cut));
    }

    [Fact]
    public void Eine_neue_Vorlaufstufe_erscheint_OHNE_Zeile()
    {
        // btn_NeuVorlauf_Click:86 baut die Zeile, fuegt sie aber nicht ein -
        // dt.Rows.Add(newRow) ist dort auskommentiert.
        var cut = Aufbauen();

        cut.Find(".epos-neuzeile input").Input("45");
        cut.Find(".epos-neuzeile button").Click();

        var stufen = cut.FindAll(".epos-raster tbody tr td:last-child")
                        .Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "35", "45", "55" }, stufen);
        Assert.Equal(45, cut.Instance.Vorlauf);
        Assert.Equal(0, Zeilenzahl(cut));
    }

    [Fact]
    public void Eine_neue_Vorlaufstufe_ohne_Wert_meldet()
    {
        var cut = Aufbauen();
        cut.Find(".epos-neuzeile button").Click();

        Assert.Contains("Neue Vorlauftemperatur", cut.Find(".epos-warnbanner").TextContent);
    }

    // =================================================================================
    // Stuetzstelle uebernehmen
    // =================================================================================

    private static IElement Knopf(IRenderedComponent<KennlinienEditorDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void Daten_uebernehmen_legt_die_Zeile_an_und_leert_die_Felder()
    {
        var zeilen = Proben();
        var cut = Aufbauen(zeilen);

        var felder = cut.Find(".epos-gruppenkopf-koerper").QuerySelectorAll("input");
        felder[0].Input("7");
        felder[1].Input("4,2");
        felder[2].Input("8,5");
        Knopf(cut, "Daten übernehmen").Click();

        Assert.Equal(3, Zeilenzahl(cut));                    // Stufe 35 hatte zwei
        Assert.Equal(4, zeilen.Count);

        KennlinienZeile neu = zeilen[^1];
        Assert.Equal(0, neu.Id);                             // 0 = neu
        Assert.Equal(35, neu.Vorlauf);
        Assert.Equal(7, neu.Temperatur);
        Assert.Equal(4.2, neu.Cop);
        Assert.Equal(8.5, neu.Ptherm);

        // Die drei Felder sind danach leer.
        foreach (var f in cut.Find(".epos-gruppenkopf-koerper").QuerySelectorAll("input"))
            Assert.Equal("", f.GetAttribute("value"));
    }

    [Fact]
    public void Daten_uebernehmen_ohne_Vorlaufstufe_meldet()
    {
        var cut = Aufbauen(new List<KennlinienZeile>());       // keine Zeile, keine Stufe

        Knopf(cut, "Daten übernehmen").Click();
        Assert.Contains("Vorlauftemperatur selektieren!", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Jedes_der_drei_Pflichtfelder_wird_beim_Namen_genannt()
    {
        var cut = Aufbauen();
        var felder = cut.Find(".epos-gruppenkopf-koerper").QuerySelectorAll("input");

        Knopf(cut, "Daten übernehmen").Click();
        Assert.Contains("Temperatur", cut.Find(".epos-warnbanner").TextContent);

        felder[0].Input("7");
        Knopf(cut, "Daten übernehmen").Click();
        Assert.Contains("COP", cut.Find(".epos-warnbanner").TextContent);

        cut.Find(".epos-gruppenkopf-koerper").QuerySelectorAll("input")[1].Input("4,2");
        Knopf(cut, "Daten übernehmen").Click();
        Assert.Contains("Ptherm", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Eine_unlesbare_Zahl_uebernimmt_nichts()
    {
        var zeilen = Proben();
        var cut = Aufbauen(zeilen);

        var felder = cut.Find(".epos-gruppenkopf-koerper").QuerySelectorAll("input");
        felder[0].Input("7");
        felder[1].Input("keine Zahl");     // faerbt das Feld, meldet keinen Wert
        felder[2].Input("8,5");
        Knopf(cut, "Daten übernehmen").Click();

        Assert.Equal(3, zeilen.Count);
        Assert.Contains("COP", cut.Find(".epos-warnbanner").TextContent);
    }

    // =================================================================================
    // Bearbeiten und Loeschen
    // =================================================================================

    [Fact]
    public void Eine_Zelle_schreibt_in_die_Zeile_zurueck()
    {
        var zeilen = Proben();
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-zr-zeile")[0].QuerySelectorAll("input")[1].Input("3,1");
        Assert.Equal(3.1, zeilen[0].Cop);
    }

    [Fact]
    public void Loeschen_nimmt_genau_diese_Stuetzstelle()
    {
        var zeilen = Proben();
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-zr-zeile")[0].QuerySelector("button")!.Click();

        Assert.Equal(2, zeilen.Count);
        Assert.DoesNotContain(zeilen, z => z.Id == 1);
        Assert.Equal(1, Zeilenzahl(cut));
    }

    [Fact]
    public void Die_Stufe_bleibt_stehen_wenn_ihre_letzte_Zeile_geht()
    {
        var zeilen = Proben();
        var cut = Aufbauen(zeilen);

        cut.FindAll(".epos-raster tbody tr button")[1].Click();          // Stufe 55
        cut.FindAll(".epos-zr-zeile")[0].QuerySelector("button")!.Click();

        var stufen = cut.FindAll(".epos-raster tbody tr td:last-child")
                        .Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "35", "55" }, stufen);
        Assert.Equal(0, Zeilenzahl(cut));
    }

    // =================================================================================
    // NurLesen, Ergebnis, Tastatur
    // =================================================================================

    [Fact]
    public void NurLesen_sperrt_alles_und_sagt_warum()
    {
        var cut = Aufbauen(nurLesen: true);

        Assert.Contains("schreibgeschützt", cut.Find(".epos-warnbanner").TextContent);
        Assert.All(cut.FindAll(".epos-zr-zeile input"), e => Assert.True(e.HasAttribute("disabled")));
        Assert.All(cut.FindAll(".epos-gruppenkopf-koerper input"),
                   e => Assert.True(e.HasAttribute("disabled")));
        Assert.True(Knopf(cut, "Daten übernehmen").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Neue Vorlauftemperatur").HasAttribute("disabled"));
    }

    [Fact]
    public void OK_liefert_die_Liste()
    {
        IReadOnlyList<KennlinienZeile>? ergebnis = null;
        var zeilen = Proben();
        var cut = Aufbauen(zeilen, geschlossen: l => ergebnis = l);

        Knopf(cut, "OK").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(3, ergebnis!.Count);
    }

    [Fact]
    public void Abbruch_und_Esc_liefern_null()
    {
        object? ergebnis = "nicht gerufen";
        var cut = Aufbauen(geschlossen: l => ergebnis = l);

        Knopf(cut, "Abbruch").Click();
        Assert.Null(ergebnis);

        ergebnis = "nicht gerufen";
        var cut2 = Aufbauen(geschlossen: l => ergebnis = l);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }
    // =====================================================================
    //  Formularraster — Anwenderwunsch iU8‑E‑2, Paket P1 (05.09.2026)
    // =====================================================================

    /// <summary>
    /// <b>iU8‑E‑2, Paket P1:</b> „Darstellung der Dialoge kompakter und
    /// übersichtlicher — Parameterblöcke rechts."
    ///
    /// <para>Der Block „Neue Stützstelle" steht seither im <c>Formularraster</c>: Die Beschriftung
    /// fällt NEBEN das Feld, die Felder ordnen sich in eine oder zwei Spalten,
    /// und ein Zahlenfeld ist kurz mit der Einheit unmittelbar dahinter. Zuvor
    /// nahm jedes Feld die volle Breite und die Beschriftung stand darüber.</para>
    ///
    /// <para>Die Regeln dahinter hält <c>Bausteine/FormularrasterTests</c>;
    /// hier steht nur, dass der Block ihn TRÄGT.</para>
    /// </summary>
    [Fact]
    public void Der_Block_fuer_die_neue_Stuetzstelle_steht_im_Formularraster()
    {
        var cut = Aufbauen();

        var raster = cut.FindAll(".epos-formularraster");
        Assert.NotEmpty(raster);
        Assert.Contains(raster, r => r.QuerySelectorAll(".epos-feld").Length > 0);

        // Ein Zahlenfeld meldet sich als KURZES Feld, und seine Einheit steht in
        // derselben Feldzeile — im Vorbild 4 px hinter dem Feld, im Befund am
        // rechten Rand des Blocks.
        var kurz = cut.FindAll(".epos-formularraster .epos-feld--kurz");
        Assert.NotEmpty(kurz);
        Assert.Contains(kurz, f => f.QuerySelector(".epos-feld-zeile .epos-einheit") is not null);
    }
}
