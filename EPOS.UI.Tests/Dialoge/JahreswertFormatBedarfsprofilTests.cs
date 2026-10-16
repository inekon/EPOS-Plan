using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>BP-3: einheitliches Zahlformat der Jahreswerte in MWh</b> (Anwenderentscheid 16.10.2026) im
/// Bedarfsprofil-Dialog. Projektliste, Katalogliste, Summe im Listenkopf und Satzzeile zeigen die
/// Jahressumme als ganze Zahl mit Tausendertrennung der Kultur (de „1.200“, en-US „1,200“); das
/// Eingabefeld und der Lesewert der Überlagerung bleiben genau, gespeichert wird ungerundet.
/// </summary>
public class JahreswertFormatBedarfsprofilTests : EposBunitContext
{
    public JahreswertFormatBedarfsprofilTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static Katalogfilterzeile Katalogzeile(int id, string name, double summe)
        => new Katalogfilterzeile(id, name)
            .MitText(Katalogfilterprofil.SpBezeichner, name)
            .MitText(Katalogfilterprofil.SpTyp, "Typ 1")
            .MitZahl(Katalogfilterprofil.SpJahressummeMwh, summe, Energieeinheit.JAHRESWERT_MWH_NACHKOMMASTELLEN)
            .MitText(Katalogfilterprofil.SpBeschreibung, "Beschreibung " + name)
            .MitKennzeichen(Katalogfilterprofil.SpAuslieferung, false);

    private IRenderedComponent<BedarfsProfileDialog> Aufbauen(List<BedarfsProfilZeile> zeilen,
                                                             BedarfsArt art = BedarfsArt.Kaelte)
    {
        IReadOnlyList<Katalogfilterzeile> katalog = new[] { Katalogzeile(1, "Katalog A", 2345.6) };
        return Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, art)
            .Add(x => x.TitelText, "Bedarf")
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogzeilen, () => katalog)
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(art, s => s).MitVerwendungsspalte(s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "Beschreibung " + n, "Typ 1"))
            .Add(x => x.Jahressumme, _ => 2345.6)
            .Add(x => x.Aufnehmen, n => new BedarfsProfilZeile { IdZ = 100000, IdStamm = 9, Name = n, Summe = 2345.6 })
            .Add(x => x.KatalogLoeschen, _ => true)
            .Add(x => x.ProjektGespeichert, () => true)
            .Add(x => x.Simulieren, _ => new Dictionary<string, object>())
            .Add(x => x.Einheit, Energieeinheit.MWh));
    }

    private static List<BedarfsProfilZeile> Zeilen() => new()
    {
        new() { IdZ = 1, IdStamm = 3, Name = "Profil A", Summe = 1200.4 },
        new() { IdZ = 2, IdStamm = 3, Name = "Profil B", Summe = 0.3 }
    };

    /// <summary>Die Zellen der Spalte „Jahressumme“ der Projektliste, in Zeilenfolge.</summary>
    private static List<string> Projektsummen(IRenderedComponent<BedarfsProfileDialog> cut)
        => cut.FindAll(".epos-zweispalten-bereich--projekt tbody tr")
              .Select(tr => tr.QuerySelectorAll("td")
                              .Select(td => td.TextContent.Trim())
                              .First(t => t.Length > 0 && char.IsDigit(t[0])))
              .ToList();

    /// <summary>Die Werte der Satzzeile (Kurzangaben des gewählten Satzes).</summary>
    private static List<string> Satzzeile(IRenderedComponent<BedarfsProfileDialog> cut)
        => cut.FindAll(".epos-satzzusammenfassung dd").Select(e => e.TextContent.Trim()).ToList();

    [Theory]
    [InlineData("de-DE", "1.200", "0", "1.201")]
    [InlineData("en-US", "1,200", "0", "1,201")]
    public void Projektliste_Summe_und_Projektsatz_zeigen_MWh_ganzzahlig(string kultur, string zeileA,
                                                                       string zeileB, string summe)
    {
        using var k = new Kulturvorrichtung(kultur);
        var cut = Aufbauen(Zeilen());

        Assert.Equal(new[] { zeileA, zeileB }, Projektsummen(cut));
        Assert.Equal(summe, cut.Find(".epos-zweispalten-summe b").TextContent);

        cut.FindAll(".epos-projektzeile")[0].Click();
        Assert.Contains(zeileA, Satzzeile(cut));
    }

    [Theory]
    [InlineData("de-DE", "2.346")]
    [InlineData("en-US", "2,346")]
    public void Katalogliste_und_Katalogsatz_zeigen_MWh_ganzzahlig(string kultur, string erwartet)
    {
        using var k = new Kulturvorrichtung(kultur);
        var cut = Aufbauen(Zeilen());

        var zelle = cut.FindAll(".epos-katalogliste tbody td").Select(td => td.TextContent.Trim());
        Assert.Contains(erwartet, zelle);

        cut.FindAll(".epos-katalogliste .epos-zeilenzelle--name")[0].Click();
        Assert.Contains(erwartet, Satzzeile(cut));
    }

    [Theory]
    [InlineData("de-DE", "1.200", ",4")]
    [InlineData("en-US", "1,200", ".4")]
    public void Satzkopf_Kenndaten_zeigen_die_Jahressumme_ganzzahlig(string kultur, string erwartet, string verboten)
    {
        using var k = new Kulturvorrichtung(kultur);
        var cut = Aufbauen(Zeilen());

        cut.FindAll(".epos-projektzeile")[0].Click();
        var kopf = cut.FindAll(".epos-zweispalten-satzkenndaten").Select(e => e.TextContent.Trim()).ToList();
        Assert.NotEmpty(kopf);
        Assert.All(kopf, t =>
        {
            Assert.Contains(erwartet, t);
            Assert.DoesNotContain(erwartet + verboten, t);
        });
    }

    [Fact]
    public void Eingabefeld_und_Lesewert_der_Ueberlagerung_bleiben_genau()
    {
        var zeilen = Zeilen();
        var cut = Aufbauen(zeilen, BedarfsArt.Prozesswaerme);

        cut.FindAll(".epos-projektzeile")[0].Click();
        Assert.Equal("1200,40", cut.Instance.Jahresverbrauch);

        cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt").Click();
        IElement feld = cut.Find(".epos-satzueberlagerung-koerper input[inputmode=decimal]");
        feld.Input("1234,567");
        Assert.Equal("1234,567", cut.Find(".epos-satzueberlagerung-koerper input[inputmode=decimal]").GetAttribute("value"));

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Übernehmen").Click();
        Assert.Equal(1234.567, zeilen[0].Summe);
        Assert.Equal("1.235", Projektsummen(cut)[0]);
    }
}
