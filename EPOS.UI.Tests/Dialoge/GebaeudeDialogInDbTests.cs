using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using EPOS.UI.Standards;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>„In DB übernehmen"</b> im Gebäudedialog: der dritte Knopf der Übernahmeleiste schreibt die
/// Projektkopie der markierten Projektzeile nach einer Namensabfrage als neuen Katalogsatz.
///
/// <para>Geprüft wird: kein Delegat, kein Knopf; aktiv nur mit gewählter Projektzeile (eine
/// Katalogwahl sperrt ihn); weich gesperrt mit Grund bei einer ungespeicherten Zeile; die Abfrage
/// mit Vorschlag der Hülle; die Absage am Namensfeld lässt sie offen; nach dem Erfolg liest der
/// Katalog neu, der neue Satz ist gewählt und die Statuszeile meldet ihn; Abbrechen schreibt nichts.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt — die Erwartungswerte sind deutsche Beschriftungen.</para>
/// </summary>
public class GebaeudeDialogInDbTests : EposBunitContext
{
    private const string KNOPF = "In die Datenbank übernehmen…";

    public GebaeudeDialogInDbTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static Katalogfilterzeile Katalogsatz(int id, string name)
        => new Katalogfilterzeile(id, name)
            .MitText(Katalogfilterprofil.SpBezeichner, name)
            .MitText(Katalogfilterprofil.SpGebaeudeart, "Einfamilienhaus")
            .MitText(Katalogfilterprofil.SpVerwendung, "Wohngebäude")
            .MitText(Katalogfilterprofil.SpBaualtersklasse, "1984 bis 1994")
            .MitZahl(Katalogfilterprofil.SpFlaecheM2, 150, 0);

    private static GebaeudeProjektZeile Zeile(int idZ, string name, bool kopie) => new()
    {
        IdZ = idZ,
        IdGebaeude = 7,
        Name = name,
        Art = "Einfamilienhaus",
        Wohnflaeche = 150,
        Einheit = "Wohnfläche [m²]",
        Jahresnutzungsgrad = 1,
        HatProjektkopie = kopie
    };

    /// <summary>Der Katalog der Probe — wächst mit jeder erfolgreichen Übernahme.</summary>
    private readonly List<Katalogfilterzeile> _katalog = new() { Katalogsatz(1, "Haus 1990") };

    /// <summary>Was die Hülle geschrieben hätte: Zeile, Wahl, Name.</summary>
    private readonly List<(GebaeudeProjektZeile Zeile, bool Ueberschreiben, string Name)> _aufrufe = new();

    /// <summary>Der Sperrgrund des Ursprungs in der Vorschau (Vorgabe: überschreiben möglich).</summary>
    private Rueckwegabsage _ursprung = Rueckwegabsage.Keine;

    private GebaeudeRueckwegVorschau Vorschau(GebaeudeProjektZeile z)
        => new(z.IdZ, 70, z.Name, 1, "Haus 1990", _ursprung, z.Name + " (Projekt)",
               new[] { new GebaeudeAbweichung("Wohnflaeche_gesamt", "140", "150") },
               new[] { new GebaeudeKindzahl(GebaeudeRueckweg.ART_ZONEN, 2, 1), new GebaeudeKindzahl(GebaeudeRueckweg.ART_BAUTEILE, 9, 4) });

    /// <summary>Der Rückweg der Hülle im Kleinen (Muster <c>GebaeudeHuelle.Rueckweg</c>).</summary>
    private GebaeudeDbUebernahme Hullenweg(GebaeudeProjektZeile z, bool ueberschreiben, string name)
    {
        _aufrufe.Add((z, ueberschreiben, name));
        if (ueberschreiben) return new GebaeudeDbUebernahme(true, "Haus 1990", "Gebäude „Haus 1990“ überschrieben.");
        if (_katalog.Any(k => k.Bezeichner == name))
            return new GebaeudeDbUebernahme(false, "", "Ein Gebäude mit diesem Namen steht schon im Katalog.", true);
        _katalog.Add(Katalogsatz(_katalog.Count + 1, name));
        return new GebaeudeDbUebernahme(true, name, "Gebäude „" + name + "“ in die Datenbank übernommen.");
    }

    private IRenderedComponent<GebaeudeDialog> Aufbauen(List<GebaeudeProjektZeile> zeilen, bool mitWeg = true,
                                                         Func<string>? listeSpeichern = null)
    {
        return Render<GebaeudeDialog>(p =>
        {
            p.Add(x => x.Zeilen, zeilen)
             .Add(x => x.ListeSpeichern, listeSpeichern)
             .Add(x => x.Wizard, listeSpeichern is null)
             .Add(x => x.Katalogzeilen, () => _katalog.ToList())
             .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerGebaeude(
                 s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s))
             .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
             .Add(x => x.StammDetail, n => new GebaeudeStammDetail(n, "Einfamilienhaus", "Katalogtext", "150,00"));
            if (mitWeg)
                p.Add(x => x.RueckwegVorschau, Vorschau)
                 .Add(x => x.Rueckweg, Hullenweg)
                 .Add(x => x.RueckwegNameBelegt, n => _katalog.Any(k => k.Bezeichner == n));
        });
    }

    private static IElement? InDbKnopf(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-zweispalten-bereich--projekt .epos-zweispalten-kopfleiste button.epos-gebaeude-in-db").SingleOrDefault();

    private static void Uebernehmen(IRenderedComponent<GebaeudeDialog> cut)
        => cut.Find("button.epos-rueckweg-uebernehmen").Click();

    [Fact]
    public void Ohne_Delegat_kein_Knopf_mit_Delegat_in_der_Projekt_Kopfleiste()
    {
        var ohne = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) }, mitWeg: false);
        Assert.Null(InDbKnopf(ohne));

        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) });
        IElement knopf = InDbKnopf(cut)!;
        Assert.Equal(KNOPF, knopf.TextContent.Trim());
        Assert.Equal(R.KATRUECK_BTN_HINWEIS, knopf.GetAttribute("title"));
        Assert.False(knopf.HasAttribute("disabled"));
    }

    [Fact]
    public void Der_Knopf_ist_nur_mit_gewaehlter_Projektzeile_aktiv()
    {
        var leer = Aufbauen(new List<GebaeudeProjektZeile>());
        Assert.True(InDbKnopf(leer)!.HasAttribute("disabled"));

        // Eine Katalogwahl nimmt die Projektmarkierung - der Knopf sperrt.
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) });
        Assert.False(InDbKnopf(cut)!.HasAttribute("disabled"));
        cut.FindAll(".epos-katalogliste tbody tr").First().QuerySelector(".epos-zeilenzelle--name, .epos-anlagenwahl")!.Click();
        Assert.Null(cut.Instance.Gewaehlt);
        Assert.True(InDbKnopf(cut)!.HasAttribute("disabled"));
    }

    [Fact]
    public void Mehrere_gewaehlte_Zeilen_sperren_den_Knopf_mit_Grund()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true), Zeile(2, "Haus B", true) });
        cut.InvokeAsync(() => cut.Instance.ProjektWahl.Setzen(new[] { "1", "2" }));
        cut.Render();
        IElement knopf = InDbKnopf(cut)!;
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(R.GEB_SPERRE_RUECKWEG_MEHRERE, knopf.GetAttribute("title"));
        knopf.Click();
        Assert.Null(cut.Instance.Rueckwegzeilen);
        Assert.Equal(R.GEB_SPERRE_RUECKWEG_MEHRERE, cut.Instance.Meldung);
    }

    [Fact]
    public void Im_Assistenten_ist_eine_ungespeicherte_Zeile_weich_gesperrt_und_nennt_den_Grund()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(100000, "Neu", false) });
        IElement knopf = InDbKnopf(cut)!;
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(R.GEB_SPERRE_IN_DB_ASSISTENT, knopf.GetAttribute("title"));

        knopf.Click();
        Assert.Null(cut.Instance.Rueckwegzeilen);
        Assert.Empty(_aufrufe);
        Assert.Equal(R.GEB_SPERRE_IN_DB_ASSISTENT, cut.Instance.Meldung);
    }

    [Fact]
    public void Eine_ungespeicherte_Zeile_wird_vorher_still_gespeichert()
    {
        int gespeichert = 0;
        GebaeudeProjektZeile neu = Zeile(100000, "Neu", false);
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { neu },
                           listeSpeichern: () => { gespeichert++; neu.IdZ = 55; neu.HatProjektkopie = true; return ""; });
        InDbKnopf(cut)!.Click();
        Assert.Equal(1, gespeichert);
        Assert.NotNull(cut.Instance.Rueckwegzeilen);
        Assert.Equal(55, cut.Instance.Rueckwegzeilen![0].Id);
        Assert.Equal("Neu (Projekt)", cut.Find(".epos-rueckweg-namensfeld").GetAttribute("value"));
    }

    [Fact]
    public void Scheitert_das_stille_Speichern_oeffnet_keine_Rueckfrage()
    {
        GebaeudeProjektZeile neu = Zeile(100000, "Neu", false);
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { neu },
                           listeSpeichern: () => "Die Gebäudeliste wurde nicht gespeichert.");
        InDbKnopf(cut)!.Click();
        Assert.Null(cut.Instance.Rueckwegzeilen);
        Assert.Empty(_aufrufe);
        Assert.Equal("Die Gebäudeliste wurde nicht gespeichert.", cut.Instance.Meldung);
    }

    [Fact]
    public void Die_Rueckfrage_zeigt_Vorschlag_Abweichungen_Kindzeilen_und_was_bleibt()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) }, listeSpeichern: () => "");
        InDbKnopf(cut)!.Click();
        Assert.Equal("Haus A (Projekt)", cut.Find(".epos-rueckweg-namensfeld").GetAttribute("value"));
        Assert.Contains("Wohnflaeche_gesamt (140 → 150)", cut.Find(".epos-gebaeude-rueckweg-abweichungen").TextContent);
        string kinder = cut.Find(".epos-gebaeude-rueckweg-kindzeilen").TextContent;
        Assert.Contains("Zonen 2 / 1", kinder);
        Assert.Contains("Bauteile 9 / 4", kinder);
        Assert.Equal(R.GEB_RUECKWEG_BLEIBT, cut.Find(".epos-rueckweg-bleibt").TextContent.Trim());
        Assert.DoesNotContain(R.KATRUECK_HINWEIS_KOSTEN, cut.Markup);
        Assert.False(cut.Find(".epos-rueckweg-ueber").HasAttribute("disabled"));
    }

    [Fact]
    public void Neu_schreibt_den_Satz_waehlt_ihn_im_Katalog_und_meldet_ihn()
    {
        GebaeudeProjektZeile zeile = Zeile(1, "Haus A", true);
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { zeile }, listeSpeichern: () => "");
        InDbKnopf(cut)!.Click();
        Uebernehmen(cut);

        Assert.Null(cut.Instance.Rueckwegzeilen);
        Assert.Single(_aufrufe);
        Assert.Same(zeile, _aufrufe[0].Zeile);
        Assert.False(_aufrufe[0].Ueberschreiben);
        Assert.Equal("Haus A (Projekt)", _aufrufe[0].Name);
        Assert.Equal("Haus A (Projekt)", cut.Instance.Katalogzeile?.Bezeichner);
        Assert.Equal("Gebäude „Haus A (Projekt)“ in die Datenbank übernommen.", cut.Instance.Meldung);
        Assert.Single(cut.Instance.Zeilen);
    }

    [Fact]
    public void Ueberschreiben_sperrt_das_Namensfeld_und_schreibt_den_Ursprung()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) }, listeSpeichern: () => "");
        InDbKnopf(cut)!.Click();
        cut.Find(".epos-rueckweg-ueber").Change(true);
        IElement feld = cut.Find(".epos-rueckweg-namensfeld");
        Assert.True(feld.HasAttribute("disabled"));
        Assert.Equal("Haus 1990", feld.GetAttribute("value"));
        Uebernehmen(cut);

        Assert.Single(_aufrufe);
        Assert.True(_aufrufe[0].Ueberschreiben);
        Assert.Equal("Gebäude „Haus 1990“ überschrieben.", cut.Instance.Meldung);
        Assert.Single(_katalog);
    }

    [Fact]
    public void Ein_gesperrter_Ursprung_sperrt_Ueberschreiben_mit_Grund()
    {
        _ursprung = Rueckwegabsage.UrsprungGesperrt;
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) }, listeSpeichern: () => "");
        InDbKnopf(cut)!.Click();
        Assert.True(cut.Find(".epos-rueckweg-ueber").HasAttribute("disabled"));
        Assert.Contains(R.KATRUECK_GRUND_GESPERRT, cut.Find(".epos-rueckweg-grund").TextContent);
    }

    [Fact]
    public void Ein_belegter_Name_steht_am_Feld_und_sperrt_Uebernehmen()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) }, listeSpeichern: () => "");
        InDbKnopf(cut)!.Click();
        cut.Find(".epos-rueckweg-namensfeld").Input("Haus 1990");
        Assert.Equal(R.KATRUECK_HINWEIS_NAME_BELEGT, cut.Find(".epos-rueckweg-namenshinweis").TextContent.Trim());
        Assert.True(cut.Find("button.epos-rueckweg-uebernehmen").HasAttribute("disabled"));

        cut.Find(".epos-rueckweg-namensfeld").Input("Haus A neu");
        Uebernehmen(cut);
        Assert.Null(cut.Instance.Rueckwegzeilen);
        Assert.Equal(2, _katalog.Count);
    }

    [Fact]
    public void Abbrechen_schreibt_nichts()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) }, listeSpeichern: () => "");
        InDbKnopf(cut)!.Click();
        cut.FindAll(".epos-rueckweg-fuss button").First(b => b.TextContent.Trim() == R.KATRUECK_BTN_ABBRECHEN).Click();

        Assert.Null(cut.Instance.Rueckwegzeilen);
        Assert.Empty(_aufrufe);
        Assert.Single(_katalog);
    }
}
