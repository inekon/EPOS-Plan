using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Solarkollektor-Katalogeditor (iU9-W7.6). Soll ist die Feldkarte von
/// <c>Form_SolarDB</c>: 27 Zeilen — vier Textfelder, acht Pflichtzahlen, zwei
/// Ganzzahlen mit erlaubter Leere und vier Knöpfe.
/// </summary>
public class SolarkollektorKatalogDialogTests : BunitContext
{
    public SolarkollektorKatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static SolarkollektorKatalogDaten Voll() => new()
    {
        KatalogId = 12,
        Name = "Vitosol 200",
        Firma = "Viessmann",
        Beschreibung = "Flachkollektor",
        Kollektortyp = "Flach",
        Modulflaeche = 2.51,
        Aperturflaeche = 2.31,
        H0 = 0.79,
        K1 = 3.95,
        K2 = 0.0122,
        Kdir = 0.93,
        Kdiff = 0.9,
        Kosten = 850,
        Vorlauf = 60,
        Ruecklauf = 40
    };

    private IRenderedComponent<SolarkollektorKatalogDialog> Aufbauen(
        SolarkollektorKatalogDaten? daten = null,
        KatalogModus modus = KatalogModus.Bearbeiten,
        Func<SolarkollektorKatalogDaten, KatalogSpeicherErgebnis>? ueberschreiben = null,
        Func<SolarkollektorKatalogDaten, string, KatalogSpeicherErgebnis>? anlegen = null,
        Action<bool>? geschlossen = null)
        => Render<SolarkollektorKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Voll())
            .Add(x => x.Modus, modus)
            .Add(x => x.Ueberschreiben, ueberschreiben ?? (_ => new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", "Vitosol 200")))
            .Add(x => x.Anlegen, anlegen ?? ((_, n) => new KatalogSpeicherErgebnis(true, "Datensatz gespeichert", n)))
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<SolarkollektorKatalogDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht()
    {
        var cut = Aufbauen();

        // Vier Textfelder (Name, Hersteller, Beschreibung mehrzeilig, Kollektortyp).
        var bezeichnung = cut.FindAll(".epos-gruppenkopf-koerper")[0];
        Assert.Equal(3, bezeichnung.QuerySelectorAll("input").Length);
        Assert.Single(bezeichnung.QuerySelectorAll("textarea"));

        // Zehn Zahlen: acht Pflicht plus Vorlauf und Ruecklauf.
        Assert.Equal(10, cut.FindAll(".epos-gruppenkopf-koerper")[1].QuerySelectorAll("input").Length);

        var knopftexte = cut.FindAll(".epos-leiste button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Überschreiben", "Speichern unter", "Abbrechen", "Speichern" }, knopftexte);
    }

    [Fact]
    public void Die_Beschriftungen_kommen_aus_dem_Designer_nicht_aus_der_Karte()
    {
        // R-W6-7 / A-12: Die Karte ordnet "k2 :" dem Feld textBox_Kosten zu und laesst
        // textBox_k2 ohne Beschriftung. Die Designer-Koordinaten sagen: Label15 "k2:"
        // (80,327) steht LINKS von textBox_k2 (111,327), Label25
        // "Investitionskosten:" (356,297) UEBER textBox_Kosten (359,319).
        var cut = Aufbauen();
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();

        Assert.Contains("k2 :", texte);
        Assert.Contains("Investitionskosten :", texte);
        Assert.Equal(1, texte.Count(t => t == "k2 :"));
    }

    [Fact]
    public void Alle_Beschriftungen_der_Karte_stehen()
    {
        var cut = Aufbauen();
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();

        foreach (string soll in new[]
                 {
                     "Kollektorname :", "Hersteller :", "Beschreibung :", "Kollektortype :",
                     "Modulfläche :", "Aperturfläche :", "h0 :", "k1 :", "k2 :", "Kdir :",
                     "Kdiff :", "Investitionskosten :", "Vorlauf:", "Rücklauf:"
                 })
            Assert.Contains(soll, texte);
    }

    /// <summary>Die Maske ist lokalisiert (12 englische Texte, W7.9).</summary>
    [Fact]
    public void Die_englischen_Texte_lassen_sich_setzen()
    {
        var cut = Render<SolarkollektorKatalogDialog>(p => p
            .Add(x => x.Daten, Voll())
            .Add(x => x.TitelText, "Collector Edit")
            .Add(x => x.LabelName, "Collector name:")
            .Add(x => x.BtnUeberschreibenText, "Overwrite"));

        Assert.Equal("Collector Edit", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Collector name:", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
        Assert.Contains("Overwrite", cut.FindAll("button").Select(b => b.TextContent.Trim()));
    }

    // =================================================================================
    // Modus
    // =================================================================================

    [Fact]
    public void Im_Modus_Bearbeiten_sind_Ueberschreiben_und_Speichern_unter_frei()
    {
        var cut = Aufbauen(modus: KatalogModus.Bearbeiten);

        Assert.False(Knopf(cut, "Überschreiben").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Speichern unter").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Speichern").HasAttribute("disabled"));
    }

    [Fact]
    public void Im_Modus_Neu_ist_nur_Speichern_frei()
    {
        var cut = Aufbauen(modus: KatalogModus.Neu);

        Assert.True(Knopf(cut, "Überschreiben").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Speichern unter").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Speichern").HasAttribute("disabled"));
    }

    [Fact]
    public void Der_Name_ist_in_beiden_Modi_nur_lesbar()
    {
        // Der Designer traegt textBox_Name mit Enabled = false; umbenannt wird ueber
        // "Speichern unter".
        foreach (KatalogModus m in new[] { KatalogModus.Bearbeiten, KatalogModus.Neu })
        {
            var cut = Aufbauen(modus: m);
            IElement name = cut.FindAll(".epos-gruppenkopf-koerper")[0].QuerySelectorAll("input")[0];
            Assert.True(name.HasAttribute("readonly"));
        }
    }

    // =================================================================================
    // Pruefungen
    // =================================================================================

    [Fact]
    public void Jede_der_acht_Pflichtzahlen_wird_beim_Namen_genannt()
    {
        (string Feld, Action<SolarkollektorKatalogDaten> Leeren)[] faelle =
        {
            ("Modulfläche",        d => d.Modulflaeche = null),
            ("Aperturfläche",      d => d.Aperturflaeche = null),
            ("h0",                 d => d.H0 = null),
            ("k1",                 d => d.K1 = null),
            ("k2",                 d => d.K2 = null),
            ("Kdir",               d => d.Kdir = null),
            ("Kdiff",              d => d.Kdiff = null),
            ("Investitionskosten", d => d.Kosten = null)
        };

        foreach (var fall in faelle)
        {
            var daten = Voll();
            fall.Leeren(daten);

            bool geschrieben = false;
            var cut = Aufbauen(daten, ueberschreiben: _ =>
            {
                geschrieben = true;
                return new KatalogSpeicherErgebnis(true, "", "");
            });

            Knopf(cut, "Überschreiben").Click();

            Assert.False(geschrieben);
            Assert.Contains(fall.Feld, cut.Find(".epos-warnbanner").TextContent);
        }
    }

    [Fact]
    public void Vorlauf_und_Ruecklauf_duerfen_leer_bleiben()
    {
        // Program.GanzzahlPruefen(..., leerErlaubt: true) - dort galt "" schon als 0.
        var daten = Voll();
        daten.Vorlauf = null;
        daten.Ruecklauf = null;

        bool geschrieben = false;
        var cut = Aufbauen(daten, ueberschreiben: _ =>
        {
            geschrieben = true;
            return new KatalogSpeicherErgebnis(true, "", "");
        });

        Knopf(cut, "Überschreiben").Click();
        Assert.True(geschrieben);
    }

    [Fact]
    public void Speichern_ohne_Namen_meldet()
    {
        var daten = Voll();
        daten.Name = "";

        bool geschrieben = false;
        var cut = Aufbauen(daten, KatalogModus.Neu, anlegen: (_, _) =>
        {
            geschrieben = true;
            return new KatalogSpeicherErgebnis(true, "", "");
        });

        Knopf(cut, "Speichern").Click();

        Assert.False(geschrieben);
        Assert.Contains("Kollektorname", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ein_belegter_Name_bleibt_als_Banner_stehen()
    {
        bool zu = false;
        var cut = Aufbauen(modus: KatalogModus.Neu,
                           anlegen: (_, _) => new KatalogSpeicherErgebnis(false, "Name existiert bereits!", ""),
                           geschlossen: _ => zu = true);

        Knopf(cut, "Speichern").Click();

        Assert.False(zu);
        Assert.Contains("Name existiert bereits!", cut.Find(".epos-warnbanner").TextContent);
    }

    // =================================================================================
    // Speichern unter
    // =================================================================================

    [Fact]
    public void Speichern_unter_fragt_den_Namen_in_der_Ueberlagerung()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));

        Knopf(cut, "Speichern unter").Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
        Assert.True(cut.Instance.Namensfrage);
    }

    [Fact]
    public void Speichern_unter_legt_unter_dem_NEUEN_Namen_an()
    {
        string? verwendet = null;
        var daten = Voll();
        var cut = Aufbauen(daten, anlegen: (_, n) =>
        {
            verwendet = n;
            return new KatalogSpeicherErgebnis(true, "", n);
        });

        Knopf(cut, "Speichern unter").Click();
        cut.Find(".epos-ueberlagerung input").Input("Vitosol 300");
        cut.Find(".epos-ueberlagerung").QuerySelectorAll("button")
           .First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal("Vitosol 300", verwendet);

        // Das Namensfeld des Dialogs bleibt unveraendert - der Vorlaeufer setzte es
        // ausdruecklich erst NACH der Pruefung (btn_Speichern_Unter_Click:250).
        Assert.Equal("Vitosol 200", daten.Name);
    }

    // =================================================================================
    // Ergebnis und Tastatur
    // =================================================================================

    [Fact]
    public void Erfolg_schliesst_den_Dialog()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "Überschreiben").Click();
        Assert.True(ergebnis);
    }

    [Fact]
    public void Abbrechen_und_Esc_liefern_false()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);
        Knopf(cut, "Abbrechen").Click();
        Assert.False(ergebnis);

        ergebnis = null;
        var cut2 = Aufbauen(geschlossen: b => ergebnis = b);
        cut2.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_bei_offener_Namensfrage_nur_diese()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(geschlossen: b => ergebnis = b);

        Knopf(cut, "Speichern unter").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(ergebnis);
    }
}
