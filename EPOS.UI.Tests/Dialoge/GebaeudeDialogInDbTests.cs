using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
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
    private const string KNOPF = "In DB übernehmen";

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

    /// <summary>Die Aufrufe der Hülle: Zeile und Name.</summary>
    private readonly List<(GebaeudeProjektZeile Zeile, string Name)> _aufrufe = new();

    private GebaeudeDbUebernahme Hullenweg(GebaeudeProjektZeile z, string name)
    {
        _aufrufe.Add((z, name));
        if (string.IsNullOrWhiteSpace(name)) return new GebaeudeDbUebernahme(false, "", "Bitte einen Namen eingeben.", true);
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
                p.Add(x => x.InDbVorschlag, z => z.Name + " (2)")
                 .Add(x => x.InDbUebernehmen, Hullenweg);
        });
    }

    private static IElement? InDbKnopf(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-zweispalten-uebernahme button.epos-gebaeude-in-db").SingleOrDefault();

    private static void Ok(IRenderedComponent<GebaeudeDialog> cut)
        => cut.FindAll(".epos-gebaeude-in-db-abfrage .epos-leiste button.epos-knopf--primaer").Single().Click();

    [Fact]
    public void Ohne_Delegat_kein_Knopf_mit_Delegat_der_dritte_Knopf_der_Leiste()
    {
        var ohne = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) }, mitWeg: false);
        Assert.Null(InDbKnopf(ohne));
        Assert.Equal(2, ohne.FindAll(".epos-zweispalten-uebernahme button").Count);

        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) });
        IReadOnlyList<IElement> leiste = cut.FindAll(".epos-zweispalten-uebernahme button");
        Assert.Equal(3, leiste.Count);
        Assert.Equal(KNOPF, leiste[2].TextContent.Trim());
        Assert.Equal("Das in der Projektliste markierte Gebäude als neuen Satz in die Datenbank übernehmen",
                     leiste[2].GetAttribute("title"));
        Assert.False(leiste[2].HasAttribute("disabled"));
    }

    [Fact]
    public void Der_Knopf_ist_nur_mit_gewaehlter_Projektzeile_aktiv()
    {
        // Leere Projektliste: nichts gewählt.
        var leer = Aufbauen(new List<GebaeudeProjektZeile>());
        Assert.True(InDbKnopf(leer)!.HasAttribute("disabled"));

        // Eine Katalogwahl nimmt die Projektmarkierung - der Knopf sperrt.
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) });
        Assert.False(InDbKnopf(cut)!.HasAttribute("disabled"));
        cut.FindAll(".epos-katalogliste tbody tr").First().QuerySelector("button.epos-anlagenwahl")!.Click();
        Assert.Null(cut.Instance.Gewaehlt);
        Assert.True(InDbKnopf(cut)!.HasAttribute("disabled"));
    }

    /// <summary>Im Assistenten (kein stiller Speicherweg) bleibt eine ungespeicherte Zeile weich gesperrt.</summary>
    [Fact]
    public void Im_Assistenten_ist_eine_ungespeicherte_Zeile_weich_gesperrt_und_nennt_den_Grund()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(100000, "Neu", false) });
        IElement knopf = InDbKnopf(cut)!;
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(R.GEB_SPERRE_IN_DB_NEUE_ZEILE, knopf.GetAttribute("title"));

        knopf.Click();
        Assert.False(cut.Instance.InDbOffen);
        Assert.Empty(_aufrufe);
        Assert.Equal(R.GEB_SPERRE_IN_DB_NEUE_ZEILE, cut.Instance.Meldung);
    }

    /// <summary>
    /// Anwenderentscheid 06.10.2026: Außerhalb des Assistenten speichert der Klick eine ungespeicherte
    /// Zeile zuerst still (derselbe Weg wie Bearbeiten und Exportieren), danach öffnet die Abfrage für
    /// die nun gespeicherte Zeile und schreibt sie.
    /// </summary>
    [Fact]
    public void Eine_ungespeicherte_Zeile_wird_vorher_still_gespeichert()
    {
        int gespeichert = 0;
        GebaeudeProjektZeile neu = Zeile(100000, "Neu", false);
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { neu },
                           listeSpeichern: () => { gespeichert++; neu.IdZ = 55; neu.HatProjektkopie = true; return ""; });
        IElement knopf = InDbKnopf(cut)!;
        Assert.Null(knopf.GetAttribute("aria-disabled"));
        Assert.Equal("Das in der Projektliste markierte Gebäude als neuen Satz in die Datenbank übernehmen",
                     knopf.GetAttribute("title"));

        knopf.Click();
        Assert.Equal(1, gespeichert);
        Assert.True(cut.Instance.InDbOffen);
        Assert.Equal("Neu (2)", cut.Instance.InDbName);

        Ok(cut);
        Assert.Single(_aufrufe);
        Assert.Same(neu, _aufrufe[0].Zeile);
        Assert.Equal(55, _aufrufe[0].Zeile.IdZ);
        Assert.Equal("Gebäude „Neu (2)“ in die Datenbank übernommen.", cut.Instance.Meldung);
    }

    /// <summary>Scheitert das stille Speichern, steht seine Meldung und es wird nicht übernommen.</summary>
    [Fact]
    public void Scheitert_das_stille_Speichern_wird_nicht_uebernommen()
    {
        GebaeudeProjektZeile neu = Zeile(100000, "Neu", false);
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { neu },
                           listeSpeichern: () => "Die Gebäudeliste wurde nicht gespeichert.");

        InDbKnopf(cut)!.Click();
        Assert.False(cut.Instance.InDbOffen);
        Assert.Empty(_aufrufe);
        Assert.False(neu.HatProjektkopie);
        Assert.Equal("Die Gebäudeliste wurde nicht gespeichert.", cut.Instance.Meldung);
    }

    /// <summary>Eine gespeicherte Zeile speichert nicht erst still.</summary>
    [Fact]
    public void Eine_gespeicherte_Zeile_speichert_nicht_vorher()
    {
        int gespeichert = 0;
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) },
                           listeSpeichern: () => { gespeichert++; return ""; });
        InDbKnopf(cut)!.Click();
        Assert.True(cut.Instance.InDbOffen);
        Assert.Equal(0, gespeichert);
    }

    [Fact]
    public void Die_Abfrage_schreibt_den_Satz_waehlt_ihn_im_Katalog_und_meldet_ihn()
    {
        GebaeudeProjektZeile zeile = Zeile(1, "Haus A", true);
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { zeile });

        InDbKnopf(cut)!.Click();
        Assert.True(cut.Instance.InDbOffen);
        Assert.Equal("Haus A (2)", cut.Instance.InDbName);
        Assert.Equal("Haus A (2)", cut.Find(".epos-gebaeude-in-db-name input").GetAttribute("value"));
        Assert.Contains("Zonen und Bauteile bleiben im Projekt", cut.Find(".epos-gebaeude-in-db-regel").TextContent);

        Ok(cut);

        Assert.False(cut.Instance.InDbOffen);
        Assert.Single(_aufrufe);
        Assert.Same(zeile, _aufrufe[0].Zeile);
        Assert.Equal("Haus A (2)", _aufrufe[0].Name);
        Assert.Equal("Haus A (2)", cut.Instance.Katalogzeile?.Bezeichner);
        Assert.Contains("Haus A (2)", cut.FindAll(".epos-katalogliste tbody tr").Select(tr => tr.TextContent).Aggregate((a, b) => a + b));
        Assert.Equal("Gebäude „Haus A (2)“ in die Datenbank übernommen.", cut.Instance.Meldung);
        // Die Projektliste bleibt, wie sie war.
        Assert.Single(cut.Instance.Zeilen);
    }

    [Fact]
    public void Ein_Doppelname_steht_am_Feld_und_die_Abfrage_bleibt_offen()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) });
        InDbKnopf(cut)!.Click();
        cut.Find(".epos-gebaeude-in-db-name input").Input("Haus 1990");
        Ok(cut);

        Assert.True(cut.Instance.InDbOffen);
        Assert.Equal("Ein Gebäude mit diesem Namen steht schon im Katalog.", cut.Instance.InDbNameFehler);
        Assert.Equal("Ein Gebäude mit diesem Namen steht schon im Katalog.",
                     cut.Find(".epos-gebaeude-in-db-name .epos-kond-feldmeldung").TextContent.Trim());
        Assert.Single(_katalog);

        // Eine Eingabe nimmt die Absage zurück; ein freier Name geht durch.
        cut.Find(".epos-gebaeude-in-db-name input").Input("Haus A neu");
        Assert.Null(cut.Instance.InDbNameFehler);
        Ok(cut);
        Assert.False(cut.Instance.InDbOffen);
        Assert.Equal(2, _katalog.Count);
    }

    [Fact]
    public void Abbrechen_schreibt_nichts()
    {
        var cut = Aufbauen(new List<GebaeudeProjektZeile> { Zeile(1, "Haus A", true) });
        InDbKnopf(cut)!.Click();
        cut.FindAll(".epos-gebaeude-in-db-abfrage .epos-leiste button")
           .First(b => b.TextContent.Trim() == "Abbrechen").Click();

        Assert.False(cut.Instance.InDbOffen);
        Assert.Empty(_aufrufe);
        Assert.Single(_katalog);
    }
}
