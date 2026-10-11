using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Verwaltung „Kältemittel"</b> (KM4-E3-c, Fachkonzept Kältemittel als Stammdatum 7.6): Liste mit den neun
/// Spalten und dem Schloss der Auslieferungszeilen, Satzbereich mit den Fundstellen als Lesetext, Pflege einer
/// Anwenderzeile (Neu, Speichern, Löschen nur ohne Verwendung, Code einer verwendeten Zeile fest), Schloss über die
/// Rückfrage, Rückweg und der Fall ohne Gaben. Kultur de-DE über die Hausvorrichtung.
/// </summary>
public class KaeltemittelDialogTests : EposBunitContext
{
    private static readonly KaeltemittelWahl[] GRUPPEN =
    {
        new("HFKW", "HFKW"), new("HFO", "HFO"), new("GEMISCH", "Gemisch"), new("NATUERLICH", "natürlich")
    };

    private static readonly KaeltemittelWahl[] STATUS =
    {
        new("GESICHERT", "gesichert"), new("VORLAEUFIG", "vorläufig")
    };

    private static KaeltemittelZeile R32() => new()
    {
        Id = 1, Code = "R32", Bezeichnung = "Difluormethan", Gruppe = "HFKW", GruppeText = "HFKW", Gwp = 675,
        FGas = true, Natuerlich = false, Status = "GESICHERT", StatusText = "gesichert", Gesperrt = true,
        GwpQuelle = "Fundstelle GWP A", Quelle = "Fundstelle Kennzeichen A", Verwendung = 0
    };

    private static KaeltemittelZeile Eigen(int verwendung = 0) => new()
    {
        Id = 7, Code = "R999X", Bezeichnung = "Prüfstoff", Gruppe = "HFO", GruppeText = "HFO", Gwp = 4,
        Status = "VORLAEUFIG", StatusText = "vorläufig", Verwendung = verwendung
    };

    public KaeltemittelDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<KaeltemittelDialog> Zeige(
        IReadOnlyList<KaeltemittelZeile>? zeilen = null,
        Func<KaeltemittelZeile, KaeltemittelAntwort>? speichern = null,
        Func<int, KaeltemittelAntwort>? loeschen = null,
        Func<IReadOnlyList<int>, bool, SchlossErgebnis>? schloss = null,
        Action<bool>? geschlossen = null,
        bool lesemodus = false)
    {
        var liste = zeilen ?? new[] { R32(), Eigen() };
        return Render<KaeltemittelDialog>(p => p
            .Add(x => x.Zeilen, liste)
            .Add(x => x.Neuladen, () => liste)
            .Add(x => x.Speichern, speichern ?? (z => new KaeltemittelAntwort(true, "gespeichert", z.Id > 0 ? z.Id : 99)))
            .Add(x => x.Loeschen, loeschen ?? (id => new KaeltemittelAntwort(true, "", id)))
            .Add(x => x.Schloss, new Schlossweg(schloss ?? ((ids, g) => new SchlossErgebnis(true, "") { Geaendert = ids })))
            .Add(x => x.Gruppen, GRUPPEN)
            .Add(x => x.Statuswerte, STATUS)
            .Add(x => x.Lesemodus, lesemodus)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));
    }

    private static AngleSharp.Dom.IElement Knopf(IRenderedComponent<KaeltemittelDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void Die_Liste_zeigt_neun_Spalten_und_das_Schloss_der_Auslieferungszeile()
    {
        var cut = Zeige();

        var koepfe = cut.FindAll(".epos-kmt-liste thead th").Select(t => t.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Code", "Bezeichnung", "Gruppe", "GWP", "F-Gas", "natürlich", "Sicherheitsgruppe", "PFAS", "Status" },
                     koepfe);
        var zeilen = cut.FindAll(".epos-kmt-liste tbody tr");
        Assert.Equal(2, zeilen.Count);
        Assert.Single(zeilen[0].QuerySelectorAll(".epos-schloss"));
        Assert.Empty(zeilen[1].QuerySelectorAll(".epos-schloss"));
        Assert.Contains("675", zeilen[0].TextContent);
        Assert.Contains("ja", zeilen[0].TextContent);
        Assert.Contains("gesichert", zeilen[0].TextContent);
    }

    [Fact]
    public void Eine_Auslieferungszeile_ist_nur_lesbar_und_zeigt_die_Fundstellen_als_Lesetext()
    {
        var cut = Zeige();

        Assert.Equal(1, cut.Instance.Arbeit.Id);
        Assert.All(cut.FindAll("input.epos-eingabe[type=text]"), i => Assert.True(i.HasAttribute("readonly") || i.HasAttribute("disabled")));
        Assert.Equal(4, cut.FindAll("input.epos-eingabe[type=text]").Count);   // Code, Bezeichnung, GWP, Sortierung
        Assert.Equal("Fundstelle GWP A", cut.Find(".epos-kmt-gwp-quelle").TextContent.Trim());
        Assert.Equal("Fundstelle Kennzeichen A", cut.Find(".epos-kmt-quelle").TextContent.Trim());
        Assert.Contains("Schloss aufheben", cut.Find(".epos-katalogschloss").TextContent);
        Assert.Equal("true", cut.Find(".epos-kmt-loeschen").GetAttribute("aria-disabled"));

        // Löschen ist weich gesperrt und meldet den Grund, statt zu fragen.
        cut.Find(".epos-kmt-loeschen").Click();
        Assert.Equal("", cut.Instance.OffeneFrage);
        Assert.Contains("Schloss aufheben", cut.Instance.Meldung);
    }

    [Fact]
    public void Neu_und_Speichern_schreiben_eine_Anwenderzeile()
    {
        KaeltemittelZeile? geschrieben = null;
        var cut = Zeige(speichern: z => { geschrieben = z; return new KaeltemittelAntwort(true, "gespeichert", 99); });

        cut.Find(".epos-kmt-neu").Click();
        Assert.Equal(0, cut.Instance.Arbeit.Id);
        Assert.Equal(6, cut.FindAll("input.epos-eingabe[type=text]").Count);   // dazu die zwei Fundstellen
        cut.FindAll("input.epos-eingabe[type=text]")[0].Input("R999N");
        cut.FindAll("select")[0].Change("2");                                    // Gruppe HFO

        Knopf(cut, "Speichern").Click();

        Assert.NotNull(geschrieben);
        Assert.Equal("R999N", geschrieben!.Code);
        Assert.Equal("HFO", geschrieben.Gruppe);
        Assert.True(cut.Instance.Geschrieben);
    }

    [Fact]
    public void Eine_Ablehnung_des_Kerns_steht_im_Banner_und_der_Dialog_bleibt_offen()
    {
        bool? zu = null;
        var cut = Zeige(speichern: _ => new KaeltemittelAntwort(false, "Den Code gibt es schon.", 0),
                        geschlossen: b => zu = b);
        cut.Find(".epos-kmt-neu").Click();
        cut.FindAll("input.epos-eingabe[type=text]")[0].Input("r32");

        Knopf(cut, "OK").Click();

        Assert.Null(zu);
        Assert.Equal("Den Code gibt es schon.", cut.Instance.Meldung);
    }

    [Fact]
    public void Der_Code_einer_verwendeten_Zeile_ist_fest_und_Loeschen_gesperrt()
    {
        int geloescht = 0;
        var cut = Zeige(zeilen: new[] { Eigen(verwendung: 2) }, loeschen: id => { geloescht++; return new(true, "", id); });

        Assert.True(cut.FindAll("input.epos-eingabe[type=text]")[0].HasAttribute("readonly"));
        Assert.False(cut.FindAll("input.epos-eingabe[type=text]")[1].HasAttribute("readonly"));
        cut.Find(".epos-kmt-loeschen").Click();
        Assert.Equal("", cut.Instance.OffeneFrage);
        Assert.Contains("2 Kältemaschine", cut.Instance.Meldung);
        Assert.Equal(0, geloescht);
    }

    [Fact]
    public void Loeschen_einer_unbenutzten_Anwenderzeile_fragt_und_loescht()
    {
        int geloescht = 0;
        var cut = Zeige(zeilen: new[] { Eigen() }, loeschen: id => { geloescht = id; return new(true, "", id); });

        cut.Find(".epos-kmt-loeschen").Click();
        Assert.Contains("R999X", cut.Instance.OffeneFrage);
        Knopf(cut, "Ja").Click();

        Assert.Equal(7, geloescht);
        Assert.True(cut.Instance.Geschrieben);
        Assert.Contains("gelöscht", cut.Instance.Meldung);
    }

    [Fact]
    public void Schloss_aufheben_geht_ueber_die_Rueckfrage()
    {
        (int Id, bool Gesperrt)? weg = null;
        var cut = Zeige(schloss: (ids, g) => { weg = (ids[0], g); return new SchlossErgebnis(true, "") { Geaendert = ids }; });

        cut.Find(".epos-katalogschloss").Click();
        Assert.Contains("R32", cut.Find(".epos-rueckfrage-text").TextContent);
        Knopf(cut, "Nein").Click();
        Assert.Null(weg);

        cut.Find(".epos-katalogschloss").Click();
        Knopf(cut, "Ja").Click();
        Assert.Equal((1, false), weg);
        Assert.True(cut.Instance.Geschrieben);
    }

    [Fact]
    public void Abbrechen_schliesst_ohne_zu_schreiben()
    {
        bool? zu = null;
        int geschrieben = 0;
        var cut = Zeige(speichern: z => { geschrieben++; return new(true, "", 7); }, geschlossen: b => zu = b);
        cut.FindAll(".epos-kmt-liste tbody tr")[1].Click();
        cut.FindAll("input.epos-eingabe[type=text]")[1].Input("anders");

        Knopf(cut, "Abbrechen").Click();

        Assert.False(zu);
        Assert.Equal(0, geschrieben);
    }

    [Fact]
    public void Im_Lesemodus_gibt_es_keinen_Schreibweg()
    {
        var cut = Zeige(zeilen: new[] { Eigen() }, lesemodus: true);

        Assert.True(cut.Find(".epos-kmt-neu").HasAttribute("disabled"));
        Assert.True(cut.Find(".epos-kmt-loeschen").HasAttribute("disabled"));
        Assert.All(cut.FindAll("input.epos-eingabe[type=text]"), i => Assert.True(i.HasAttribute("readonly") || i.HasAttribute("disabled")));
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_die_leere_Liste_ohne_Handlungen()
    {
        var cut = Render<KaeltemittelDialog>();

        Assert.Equal("Die Kältemitteltabelle ist leer.", cut.Find(".epos-leerzustand").TextContent.Trim());
        Assert.Empty(cut.FindAll(".epos-kmt-neu"));
        Assert.Empty(cut.FindAll(".epos-kmt-loeschen"));
        Assert.Empty(cut.FindAll(".epos-katalogschloss"));
    }
}
