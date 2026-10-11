using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using EPOS.UI.Standards;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Gebäudedialog als Wirt der Katalogauswahl V1</b> (Stufe 4; Konzept Projektdialoge mit Katalogauswahl 4.2,
/// 4.9 Zeile „Gebäude", UeS1/UeS2, 4.10): Katalog-Fußleiste über die Slots, Projekt-Kopfleiste, Stift je Zeile,
/// Verwendungsmarke über den Ursprung, Zusammenfassung der Detailzeile, Satz-Überlagerung mit Merker,
/// Mehrfachwahl und die Projektkopie sofort samt Abräumen beim Abbrechen.
/// </summary>
public class GebaeudeDialogKatalogauswahlTests : EposBunitContext
{
    public GebaeudeDialogKatalogauswahlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static Katalogfilterzeile Katalogsatz(int id, string name, bool gesperrt = false)
    {
        var k = new Katalogfilterzeile(id, name)
            .MitText(Katalogfilterprofil.SpBezeichner, name)
            .MitText(Katalogfilterprofil.SpGebaeudeart, "Einfamilienhaus")
            .MitText(Katalogfilterprofil.SpVerwendung, "Wohngebäude")
            .MitText(Katalogfilterprofil.SpBaualtersklasse, "1984 bis 1994")
            .MitZahl(Katalogfilterprofil.SpFlaecheM2, 150, 0);
        k.Geschuetzt = gesperrt;
        return k;
    }

    private static GebaeudeProjektZeile Zeile(int idZ, string name, int? katalog = null) => new()
    {
        IdZ = idZ,
        IdGebaeude = 7,
        IdKatalog = katalog,
        Name = name,
        Art = "Einfamilienhaus",
        Baualtersklasse = "1984 bis 1994",
        Wohnflaeche = 150,
        Einheit = "Wohnfläche [m²]",
        Jahresnutzungsgrad = 1,
        Rechenweg = "VDI 6007",
        HgesWK = 250,
        HatProjektkopie = true,
        KuehlungAktiv = true,
        Zonenzahl = 2,
    };

    private readonly List<Katalogfilterzeile> _katalog = new()
    {
        Katalogsatz(1, "Haus 1990"), Katalogsatz(2, "Haus 2010"), Katalogsatz(3, "Auslieferung", gesperrt: true)
    };

    private readonly List<string> _editor = new();
    private readonly List<int> _projekteditor = new();
    private readonly List<string> _geloescht = new();
    private readonly List<string> _angelegt = new();
    private int _abgebrochen;

    private IRenderedComponent<GebaeudeDialog> Aufbauen(List<GebaeudeProjektZeile> zeilen, bool sofort = true,
                                                         Func<GebaeudeProjektZeile, string>? anlegen = null)
        => Render<GebaeudeDialog>(p =>
        {
            p.Add(x => x.Zeilen, zeilen)
             .Add(x => x.ListeSpeichern, () => "")
             .Add(x => x.Katalogzeilen, () => _katalog.ToList())
             .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeude(s => R.ResourceManager.GetString(s) ?? s)
                                                         .MitVerwendungsspalte(s => R.ResourceManager.GetString(s) ?? s))
             .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
             .Add(x => x.StammDetail, n => new GebaeudeStammDetail(n, "Einfamilienhaus", "Katalogtext", "150,00", "VDI 6007",
                                                                   300.0, "AW 0,28 · FE 1,30 · DA 0,20 · GR 0,35 W/(m²·K)", 3))
             .Add(x => x.StammSatz, n => new GebaeudeProjektZeile
             {
                 IdZ = 100000 + _angelegt.Count, IdGebaeude = 9, IdKatalog = _katalog.First(k => k.Bezeichner == n).Id,
                 Name = n, Einheit = "Wohnfläche [m²]", Jahresnutzungsgrad = 1, Wohnflaeche = 150
             })
             .Add(x => x.KatalogGaben, n => { _editor.Add(n); return new Dictionary<string, object>(); })
             .Add(x => x.ProjektGaben, z => { _projekteditor.Add(z.IdZ); return new Dictionary<string, object>(); })
             .Add(x => x.KatalogLoeschen, n => { _geloescht.Add(n); _katalog.RemoveAll(k => k.Bezeichner == n); return true; })
             .Add(x => x.KatalogLoeschsperre, n => n == "Auslieferung" ? "Ein Auslieferungssatz bleibt." : "")
             .Add(x => x.Abgebrochen, () => _abgebrochen++);
            if (sofort)
                p.Add(x => x.ZeileAnlegen, anlegen ?? (z => { _angelegt.Add(z.Name); z.IdZ = 500 + _angelegt.Count; z.HatProjektkopie = true; return ""; }));
        });

    private static IReadOnlyList<IElement> Katalogzeilen(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-katalogliste tbody tr");

    private static void KatalogWaehlen(IRenderedComponent<GebaeudeDialog> cut, string name)
        => Katalogzeilen(cut).First(tr => tr.TextContent.Contains(name, StringComparison.Ordinal))
                             .QuerySelector(".epos-zeilenzelle--name")!.Click();

    // ------------------------------------------------------------------ Leisten

    [Fact]
    public void Die_Fussleiste_des_Katalogs_traegt_links_Loeschen_und_Bearbeiten_rechts_Neu_und_Import_ohne_epos_leiste()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", 1) });
        IElement fuss = cut.Find(".epos-zweispalten-fussleiste");
        string[] texte = fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Gebäude in DB löschen", R.AUSWAHL_BTN_BEARBEITEN, "Gebäude in DB neu..." }, texte);
        // Die alte Listenleiste unter der Katalogliste gibt es nicht mehr.
        Assert.Empty(cut.FindAll(".epos-zweispalten-bereich--katalog .epos-leiste"));
        // Die Projekt-Kopfleiste: Bearbeiten… vor dem Entfernen, keine Summe.
        Assert.Single(cut.FindAll(".epos-zweispalten-bereich--projekt button.epos-gebaeude-projekt"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-summe"));
    }

    [Fact]
    public void Bearbeiten_im_Katalog_oeffnet_ungesperrt_den_Editor_und_gesperrt_den_Satz_nur_lesend()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile>());
        KatalogWaehlen(cut, "Haus 2010");
        cut.Find("button.epos-gebaeude-katalog-bearbeiten").Click();
        Assert.Equal(new[] { "Haus 2010" }, _editor);
        Assert.True(cut.Instance.KatalogeditorOffen);
        cut.FindAll(".epos-ueberlagerung-zu").Last().Click();

        KatalogWaehlen(cut, "Auslieferung");
        cut.Find("button.epos-gebaeude-katalog-bearbeiten").Click();
        Assert.Single(_editor);
        Assert.True(cut.Instance.Zweispalten!.SatzUeberlagerungOffen);
        Assert.Contains(R.ADM_SCHLOSS_ERST_AUFHEBEN, cut.Find(".epos-satzueberlagerung-hinweis").TextContent);
        Assert.Empty(cut.FindAll(".epos-satzueberlagerung-ok"));
    }

    // ------------------------------------------------------------------ Stift

    [Fact]
    public void Der_Stift_der_Projektzeile_oeffnet_Huelle_und_Zonen_der_Kopie()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", 1), Zeile(2, "Haus B", 2) });
        cut.FindAll(".epos-zweispalten-bereich--projekt tbody .epos-zeilenstift")[1].Click();
        Assert.Equal(new[] { 2 }, _projekteditor);
        Assert.Equal(2, cut.Instance.Gewaehlt?.IdZ);
    }

    [Fact]
    public void Der_Stift_der_Katalogzeile_waehlt_sie_und_oeffnet_Editor_oder_Satz()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile>());
        Katalogzeilen(cut).First(tr => tr.TextContent.Contains("Haus 1990")).QuerySelector(".epos-zeilenstift")!.Click();
        Assert.Equal(new[] { "Haus 1990" }, _editor);
        Assert.Equal("Haus 1990", cut.Instance.Katalogzeile?.Bezeichner);
    }

    // ------------------------------------------------------------------ Verwendungsmarke

    [Fact]
    public void Die_Verwendungsmarke_folgt_dem_Ursprung_der_Projektzeilen_nicht_dem_Namen()
    {
        var zeilen = new List<GebaeudeProjektZeile> { Zeile(1, "Haus 1990 umbenannt", 2) };
        var cut = Aufbauen(zeilen);
        Assert.False(cut.Instance.Katalog.First(k => k.Bezeichner == "Haus 1990").ImProjekt);
        Assert.True(cut.Instance.Katalog.First(k => k.Bezeichner == "Haus 2010").ImProjekt);

        KatalogWaehlen(cut, "Haus 1990");
        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();
        Assert.True(cut.Instance.Katalog.First(k => k.Bezeichner == "Haus 1990").ImProjekt);
    }

    // ------------------------------------------------------------------ Zusammenfassung und Ueberlagerung

    [Fact]
    public void Die_Detailzeile_fasst_Projekt_und_Katalogsatz_kurz_zusammen()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", 1) });
        var projekt = cut.Instance.Zusammenfassung().ToDictionary(a => a.Beschriftung, a => a.Wert);
        Assert.Equal("150,00", projekt["Nutzfläche [m²]"]);
        Assert.Equal("VDI 6007", projekt.First(kv => kv.Key.StartsWith("Rechenweg")).Value);
        Assert.Equal(R.ALLG_BTN_JA, projekt[R.GEB_ZF_KUEHLUNG]);
        Assert.Equal("2", projekt[R.GEB_ZF_ZONEN]);

        KatalogWaehlen(cut, "Haus 2010");
        var katalog = cut.Instance.Zusammenfassung().ToDictionary(a => a.Beschriftung, a => a.Wert);
        Assert.Equal("AW 0,28 · FE 1,30 · DA 0,20 · GR 0,35 W/(m²·K)", katalog[R.GEB_ZF_UWERTE]);
        Assert.Equal("3", katalog[R.GEB_ZF_ZONEN]);
        Assert.Contains(katalog, kv => kv.Value == "Einfamilienhaus");
        // Die Kurzangaben stehen im Raster der Detailzeile, ohne Rollbereich.
        Assert.NotEmpty(cut.FindAll(".epos-satzzusammenfassung-angabe"));
    }

    [Fact]
    public void Abbrechen_der_Satz_Ueberlagerung_stellt_Flaeche_und_Angabe_wieder_her()
    {
        GebaeudeProjektZeile z = Zeile(1, "Haus A", 1);
        int geaendert = 0;
        var cut = Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile> { z })
            .Add(x => x.Katalogzeilen, () => _katalog.ToList())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Geaendert, () => geaendert++)
            .Add(x => x.WohnflaecheGaben, _ => new Dictionary<string, object>()));
        GebaeudeDialogProbe.SatzOeffnen(cut);
        Assert.True(cut.Instance.Zweispalten!.SatzUeberlagerungOffen);

        // Was die Wohnflächenangabe mit OK ändert, geht in die Zeile (Arbeitsstand) …
        cut.Find("button.epos-gebaeude-flaeche").Click();
        Assert.True(cut.Instance.WohnflaecheOffen);
        cut.InvokeAsync(() => cut.FindComponent<GebaeudeWohnflaecheDialog>().Instance.Geschlossen
                                 .InvokeAsync(new GebaeudeWohnflaecheErgebnis(222, 0.9, "Verbrauch [kWh/a]", true)));
        Assert.Equal(222, z.Wohnflaeche);

        // … und Abbrechen der Satz-Überlagerung nimmt es zurück.
        cut.FindAll(".epos-satzueberlagerung-fuss button").First(b => b.TextContent.Trim() == R.ALLG_BTN_ABBRECHEN).Click();
        Assert.False(cut.Instance.Zweispalten!.SatzUeberlagerungOffen);
        Assert.Equal(150, z.Wohnflaeche);
        Assert.Equal("Wohnfläche [m²]", z.Einheit);
        Assert.Equal(1, z.Jahresnutzungsgrad);
        Assert.False(z.DezentralWarmwasser);
    }

    [Fact]
    public void OK_der_Satz_Ueberlagerung_behaelt_den_Stand()
    {
        GebaeudeProjektZeile z = Zeile(1, "Haus A", 1);
        var cut = Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile> { z })
            .Add(x => x.Katalogzeilen, () => _katalog.ToList())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.WohnflaecheGaben, _ => new Dictionary<string, object>()));
        GebaeudeDialogProbe.SatzOeffnen(cut);
        cut.Find("button.epos-gebaeude-flaeche").Click();
        cut.InvokeAsync(() => cut.FindComponent<GebaeudeWohnflaecheDialog>().Instance.Geschlossen
                                 .InvokeAsync(new GebaeudeWohnflaecheErgebnis(222, 1, "Wohnfläche [m²]", false)));
        cut.Find(".epos-satzueberlagerung-ok").Click();
        Assert.False(cut.Instance.Zweispalten!.SatzUeberlagerungOffen);
        Assert.Equal(222, z.Wohnflaeche);
    }

    // ------------------------------------------------------------------ Mehrfachwahl

    [Fact]
    public void Mehrere_Katalogsaetze_gehen_mit_einem_Klick_ins_Projekt()
    {
        var zeilen = new List<GebaeudeProjektZeile>();
        var cut = Aufbauen(zeilen);
        cut.InvokeAsync(() => cut.Instance.KatalogWahl.Setzen(new[] { "Haus 1990", "Haus 2010" }));
        cut.Render();
        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();
        Assert.Equal(new[] { "Haus 1990", "Haus 2010" }, zeilen.Select(z => z.Name));
        Assert.Equal(new[] { "Haus 1990", "Haus 2010" }, _angelegt);
        Assert.Equal(0, cut.Instance.KatalogWahl.Anzahl);
    }

    [Fact]
    public void Mehrere_Projektzeilen_gehen_mit_einem_Klick_aus_dem_Projekt()
    {
        var zeilen = new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", 1), Zeile(2, "Haus B", 2), Zeile(3, "Haus C", 1) };
        foreach (var z in zeilen) z.Zonenzahl = 0;
        var cut = Aufbauen(zeilen);
        cut.InvokeAsync(() => cut.Instance.ProjektWahl.Setzen(new[] { "1", "3" }));
        cut.Render();
        cut.Find(".epos-zweispalten-knopf--entfernen").Click();
        Assert.Equal(new[] { "Haus B" }, zeilen.Select(z => z.Name));
    }

    [Fact]
    public void Loeschen_mehrerer_Saetze_ueberspringt_gesperrte_und_nennt_sie()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile>());
        cut.InvokeAsync(() => cut.Instance.KatalogWahl.Setzen(new[] { "Haus 1990", "Auslieferung" }));
        cut.Render();
        cut.Find("button.epos-gebaeude-db-loeschen").Click();
        Assert.Contains("„Auslieferung“", cut.Instance.Loeschfrage);
        Assert.StartsWith("Soll Haus 1990 wirklich", cut.Instance.Loeschfrage);
        cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.Equal(new[] { "Haus 1990" }, _geloescht);
    }

    // ------------------------------------------------------------------ Vormerkung

    [Fact]
    public void Uebernehmen_legt_die_Projektkopie_sofort_an_und_Abbrechen_raeumt_ab()
    {
        var zeilen = new List<GebaeudeProjektZeile>();
        bool? ergebnis = null;
        var cut = Aufbauen(zeilen);
        cut.Render(p => p.Add(x => x.Geschlossen, b => ergebnis = b));
        KatalogWaehlen(cut, "Haus 1990");
        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();

        Assert.Equal(new[] { "Haus 1990" }, _angelegt);
        Assert.True(zeilen[0].HatProjektkopie);
        Assert.Equal(501, zeilen[0].IdZ);

        cut.FindAll(".epos-dialog > .epos-leiste button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        Assert.False(ergebnis);
        Assert.Equal(1, _abgebrochen);
    }

    [Fact]
    public void OK_raeumt_nichts_ab()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile>());
        KatalogWaehlen(cut, "Haus 1990");
        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();
        cut.Find(".epos-dialog > .epos-leiste button.epos-knopf--primaer").Click();
        Assert.Equal(0, _abgebrochen);
    }

    [Fact]
    public void Scheitert_das_Anlegen_bleibt_die_Zeile_draussen_und_der_Grund_steht_am_Katalog()
    {
        var zeilen = new List<GebaeudeProjektZeile>();
        var cut = Aufbauen(zeilen, anlegen: _ => "Die Gebäudeliste wurde nicht gespeichert.");
        KatalogWaehlen(cut, "Haus 1990");
        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();
        Assert.Empty(zeilen);
        Assert.Equal("Die Gebäudeliste wurde nicht gespeichert.", cut.Instance.Meldung);
        Assert.True(cut.Instance.MeldungAmKatalog);
    }

    [Fact]
    public void Ohne_Weg_der_Kopie_bleibt_die_Zeile_bis_zum_Speichern_ohne_Kopie()
    {
        var zeilen = new List<GebaeudeProjektZeile>();
        var cut = Aufbauen(zeilen, sofort: false);
        KatalogWaehlen(cut, "Haus 1990");
        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();
        Assert.Single(zeilen);
        Assert.False(zeilen[0].HatProjektkopie);
        Assert.Empty(_angelegt);
    }
}
