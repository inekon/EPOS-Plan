using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Periodenliste der Kalenderkarte</b> (Stufe KP2, Welle U3; Entwurf KP2 Festlegung 15; Teilkonzept
/// Konditionierungsprofile 3.2 Ebene 3, 7.5): der Matrixbereich nur lesbar, neue Perioden ZEITRAUM oder
/// FEIERTAG, Rang ▲▼ im Eigenband, Löschen eigener Perioden — als Baustein mit eigenen Rückrufen und
/// im Reiter über dem echten Weg der Hülle ohne Datenbank.
/// </summary>
/// <remarks>Die Kultur ist auf de-DE gepinnt (deutsche Rückfalltexte).</remarks>
public class PeriodenlisteTests : EposBunitContext
{
    public PeriodenlisteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static readonly IReadOnlyList<KonditionierungFeiertag> REGELN = new[]
    {
        new KonditionierungFeiertag("NEUJAHR", "Neujahr"), new KonditionierungFeiertag("KARFREITAG", "Karfreitag")
    };

    /// <summary>Ferien (Matrixbereich), eine Feiertagsregel im Band 100 und zwei eigene Perioden 310, 311 — ranghöchste zuerst.</summary>
    private static List<KonditionierungPeriode> Perioden() => new()
    {
        new() { Rang = 311, Art = KonditionierungPeriodenart.Zeitraum, Name = "Umbau", Von = 100, Bis = 120, Angabe = KonditionierungAngabe.Aus, Eigenband = true },
        new() { Rang = 310, Art = KonditionierungPeriodenart.Zeitraum, Name = "Messe", Von = 60, Bis = 62, Angabe = KonditionierungAngabe.Wert, Wert = 22, Eigenband = true },
        new() { Rang = 202, Art = KonditionierungPeriodenart.Ferien, Name = "Sommerferien", Von = 205, Bis = 246, Angabe = KonditionierungAngabe.Wert, Wert = 16, Matrixbereich = true },
        new() { Rang = 100, Art = KonditionierungPeriodenart.Feiertag, Name = "Neujahr", Feiertagsregel = "NEUJAHR", Angabe = KonditionierungAngabe.WieWochentag, WieWochentag = 7 },
    };

    private readonly List<string> _aufrufe = new();
    private readonly List<string> _verweigert = new();
    private (int? Rang, KonditionierungPeriode Periode)? _gesetzt;

    private IRenderedComponent<Periodenliste> Liste(bool mitRueckrufen = true)
        => Render<Periodenliste>(p =>
        {
            p.Add(x => x.Perioden, Perioden())
             .Add(x => x.Feiertagsregeln, REGELN)
             .Add(x => x.Einheit, "°C")
             .Add(x => x.Verweigert, g => _verweigert.Add(g));
            if (mitRueckrufen)
                p.Add(x => x.Setzen, (r, per) => { _gesetzt = (r, per); return true; })
                 .Add(x => x.Verschieben, (r, h) => { _aufrufe.Add((h ? "hoch " : "runter ") + r); return true; })
                 .Add(x => x.Loeschen, r => { _aufrufe.Add("weg " + r); return true; });
        });

    private static IElement Zeile(IRenderedComponent<Periodenliste> cut, int rang) => cut.Find($"tr[data-rang='{rang}']");

    [Fact]
    public void Der_Matrixbereich_ist_nur_lesbar_die_eigenen_Zeilen_tragen_immer_ihre_Knoepfe()
    {
        IRenderedComponent<Periodenliste> cut = Liste();
        Assert.Equal(new[] { "311", "310", "202", "100" }, cut.FindAll("tbody tr").Select(z => z.GetAttribute("data-rang")).ToArray());

        IElement ferien = Zeile(cut, 202);
        Assert.Contains("epos-kond-periode--matrix", ferien.ClassList);
        Assert.Empty(ferien.QuerySelectorAll("button"));
        Assert.Contains("aus der Matrix", ferien.TextContent);

        foreach (int rang in new[] { 311, 310, 100 })
            Assert.Equal(4, Zeile(cut, rang).QuerySelectorAll(".epos-kond-periode-knoepfe button").Length);

        // Die Zeichen ▲▼ sind aria-hidden, der Name steht im aria-label.
        IElement hoch = Zeile(cut, 310).QuerySelector("button.epos-kond-rang-hoeher")!;
        Assert.Equal("Rang erhöhen", hoch.GetAttribute("aria-label"));
        Assert.Equal("true", hoch.QuerySelector("span")!.GetAttribute("aria-hidden"));
        // Die Aktionsspalte hat einen Kopf mit Beschriftung.
        Assert.Equal("Aktionen", cut.Find("th.epos-kond-periode-aktionen").TextContent.Trim());
    }

    [Fact]
    public void Rang_greift_nur_im_Eigenband_die_Grenzen_und_das_Feiertagsband_sind_weich_gesperrt()
    {
        IRenderedComponent<Periodenliste> cut = Liste();
        Zeile(cut, 310).QuerySelector("button.epos-kond-rang-hoeher")!.Click();
        Assert.Equal(new[] { "hoch 310" }, _aufrufe);

        IElement oben = Zeile(cut, 311).QuerySelector("button.epos-kond-rang-hoeher")!;
        Assert.Equal("true", oben.GetAttribute("aria-disabled"));
        Assert.Null(oben.GetAttribute("disabled"));
        oben.Click();
        IElement band = Zeile(cut, 100).QuerySelector("button.epos-kond-rang-niedriger")!;
        Assert.Equal("true", band.GetAttribute("aria-disabled"));
        band.Click();
        Assert.Equal(new[] { "hoch 310" }, _aufrufe);
        Assert.Equal(2, _verweigert.Count);
        Assert.Contains("höchsten Rang", _verweigert[0]);
        Assert.Contains("Feiertagsregeln", _verweigert[1]);

        Zeile(cut, 100).QuerySelector("button.epos-kond-periode-loeschen")!.Click();
        Assert.Equal("weg 100", _aufrufe.Last());
    }

    [Fact]
    public void Ohne_Rueckrufe_keine_Knoepfe_und_kein_Hinzufuegen()
    {
        IRenderedComponent<Periodenliste> cut = Liste(mitRueckrufen: false);
        Assert.Empty(cut.FindAll("tbody button"));
        Assert.Empty(cut.FindAll("button.epos-kond-periode-neu"));
    }

    /// <summary>
    /// Schmal (unter 600 px, Stilblatt) fällt die Spalte „Von–Bis“: Der Zeitraum steht dann als leise zweite
    /// Zeile unter dem Namen — nur, wo er etwas anderes sagt; die Feiertagsregel heißt wie ihr Tag.
    /// </summary>
    [Fact]
    public void Der_Zeitraum_steht_schmal_unter_dem_Namen_nur_wo_er_etwas_anderes_sagt()
    {
        IRenderedComponent<Periodenliste> cut = Liste();
        IElement messe = Zeile(cut, 310);
        string zeitraum = messe.QuerySelector("td.epos-kond-periode-zeitraum")!.TextContent.Trim();
        Assert.Contains("–", zeitraum);
        Assert.Equal(zeitraum, messe.QuerySelector("td.epos-kond-periode-name .epos-kond-periode-zeitraum-schmal")!.TextContent.Trim());

        IElement neujahr = Zeile(cut, 100);
        Assert.Equal("Neujahr", neujahr.QuerySelector("td.epos-kond-periode-zeitraum")!.TextContent.Trim());
        Assert.Null(neujahr.QuerySelector(".epos-kond-periode-zeitraum-schmal"));
    }

    [Fact]
    public void Ohne_Perioden_nennt_die_Liste_ihren_Leerzustand()
    {
        IRenderedComponent<Periodenliste> cut = Render<Periodenliste>();
        Assert.Contains("Keine Perioden", cut.Find(".epos-kond-perioden-leer").TextContent);
        Assert.Empty(cut.FindAll("table"));
    }

    [Fact]
    public void Ein_neuer_Zeitraum_ist_weich_gesperrt_bis_er_vollstaendig_ist_und_meldet_dann_Art_Tage_und_Angabe()
    {
        IRenderedComponent<Periodenliste> cut = Liste();
        cut.Find("button.epos-kond-periode-neu").Click();
        IElement uebernehmen = cut.Find("button.epos-kond-periode-uebernehmen");
        Assert.Equal("true", uebernehmen.GetAttribute("aria-disabled"));
        uebernehmen.Click();
        Assert.Null(_gesetzt);
        Assert.Contains("Erst Name", _verweigert.Single());

        Feld(cut, "Name").Input("Inventur");
        Feld(cut, "Von").Input("01.06.");
        Feld(cut, "Bis").Input("03.06.");
        Feld(cut, "Wert").Input("12");
        cut.Find("button.epos-kond-periode-uebernehmen").Click();

        Assert.NotNull(_gesetzt);
        Assert.Null(_gesetzt!.Value.Rang);
        KonditionierungPeriode p = _gesetzt.Value.Periode;
        Assert.Equal((KonditionierungPeriodenart.Zeitraum, "Inventur", (int?)152, (int?)154), (p.Art, p.Name, p.Von, p.Bis));
        Assert.Equal((KonditionierungAngabe.Wert, (double?)12), (p.Angabe, p.Wert));
        Assert.Null(p.Feiertagsregel);
        Assert.False(cut.Instance.FormularOffen);
    }

    [Fact]
    public void Ein_Feiertag_belegt_wie_Sonntag_vor_und_nimmt_den_Namen_der_Regel()
    {
        IRenderedComponent<Periodenliste> cut = Liste();
        cut.Find("button.epos-kond-periode-neu").Click();
        Auswahl(cut, "Art").Change(((int)KonditionierungPeriodenart.Feiertag).ToString());
        Auswahl(cut, "Feiertag").Change("1");
        Assert.Equal("Karfreitag", Feld(cut, "Name").GetAttribute("value"));
        cut.Find("button.epos-kond-periode-uebernehmen").Click();

        KonditionierungPeriode p = _gesetzt!.Value.Periode;
        Assert.Equal((KonditionierungPeriodenart.Feiertag, "KARFREITAG", "Karfreitag"), (p.Art, p.Feiertagsregel, p.Name));
        Assert.Equal((KonditionierungAngabe.WieWochentag, (int?)7), (p.Angabe, p.WieWochentag));
        Assert.Null(p.Von);
    }

    [Fact]
    public void Bearbeiten_oeffnet_die_Periode_und_meldet_ihren_Rang()
    {
        IRenderedComponent<Periodenliste> cut = Liste();
        Zeile(cut, 310).QuerySelector("button.epos-kond-periode-bearbeiten")!.Click();
        Assert.Equal("Messe", Feld(cut, "Name").GetAttribute("value"));
        Assert.Empty(cut.FindAll(".epos-kond-periode-form select").Where(s => s.ParentElement!.ParentElement!.TextContent.Contains("Art")));
        Feld(cut, "Wert").Input("21");
        cut.Find("button.epos-kond-periode-uebernehmen").Click();
        Assert.Equal(310, _gesetzt!.Value.Rang);
        Assert.Equal(21.0, _gesetzt.Value.Periode.Wert);
    }

    private static IElement Feld(IRenderedComponent<Periodenliste> cut, string beschriftung)
        => cut.FindAll(".epos-kond-periode-form label.epos-feld")
              .Single(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
              .QuerySelector("input")!;

    private static IElement Auswahl(IRenderedComponent<Periodenliste> cut, string beschriftung)
        => cut.FindAll(".epos-kond-periode-form label.epos-feld")
              .Single(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
              .QuerySelector("select")!;

    // =================================================================================
    // Im Reiter über dem echten Weg: Arbeitsstand, Matrixbereich, Zurücknehmen
    // =================================================================================

    [Fact]
    public void Im_Reiter_legt_die_Liste_eine_Periode_im_Eigenband_an_und_Zuruecknehmen_nimmt_sie_zurueck()
    {
        GebaeudeKatalogDaten satz = KalenderkarteTests.Satz();
        satz.Ferienbeginn = new[] { 0, 0, 205, 0 };
        satz.Ferienende = new[] { 0, 0, 246, 0 };
        satz.SollFerien = 16;
        satz.Ferien = 1;
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(satz, neu: false);
        KonditionierungWeg weg = KalenderkarteTests.Weg(null);
        var meldungen = new List<string>();
        var bearbeitung = new KonditionierungBearbeitung(arbeit, () => weg) { Melden = (m, _) => meldungen.Add(m) };
        IRenderedComponent<KonditionierungReiter> cut = Render<KonditionierungReiter>(p => p
            .Add(x => x.Bearbeitung, bearbeitung).Add(x => x.EntprellungMs, 0).Add(x => x.KartenAufgeklappt, true));

        cut.Find("section.epos-kond-karte[data-groesse='0'] button.epos-kond-anlegen").Click();
        KonditionierungKalender k = bearbeitung.Kalender(KonditionierungGroesse.Heizen)!;
        KonditionierungPeriode ferien = Assert.Single(k.Perioden);
        Assert.True(ferien.Matrixbereich);
        IElement karte = cut.Find("section.epos-kond-karte-einzelheiten[data-groesse='0']");
        Assert.Contains("aus der Matrix", karte.QuerySelector($"tr[data-rang='{ferien.Rang}']")!.TextContent);

        karte.QuerySelector("button.epos-kond-periode-neu")!.Click();
        IElement form = cut.Find("section.epos-kond-karte-einzelheiten[data-groesse='0'] .epos-kond-periode-form");
        form.QuerySelectorAll("label.epos-feld").Single(l => l.QuerySelector(".epos-feld-text")!.TextContent.Trim() == "Name")
            .QuerySelector("input")!.Input("Betriebsruhe");
        Formfeld(cut, "Von").Input("27.12.");
        Formfeld(cut, "Bis").Input("31.12.");
        Formfeld(cut, "Wert").Input("12");
        cut.Find("section.epos-kond-karte-einzelheiten[data-groesse='0'] button.epos-kond-periode-uebernehmen").Click();

        k = bearbeitung.Kalender(KonditionierungGroesse.Heizen)!;
        KonditionierungPeriode neu = k.Perioden.Single(p => p.Name == "Betriebsruhe");
        Assert.Equal(310, neu.Rang);
        Assert.True(neu.Eigenband);
        Assert.Equal("angelegt, 1 eigene Periode", bearbeitung.Zustand(KonditionierungGroesse.Heizen));
        Assert.Empty(meldungen);

        cut.Find("button.epos-kond-zuruecknehmen").Click();
        Assert.DoesNotContain(bearbeitung.Kalender(KonditionierungGroesse.Heizen)!.Perioden, p => p.Name == "Betriebsruhe");
    }

    private static IElement Formfeld(IRenderedComponent<KonditionierungReiter> cut, string beschriftung)
        => cut.FindAll("section.epos-kond-karte-einzelheiten[data-groesse='0'] .epos-kond-periode-form label.epos-feld")
              .Single(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
              .QuerySelector("input")!;
}
