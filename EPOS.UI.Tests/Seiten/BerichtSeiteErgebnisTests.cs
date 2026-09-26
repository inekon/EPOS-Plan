using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Das Ergebnis eines Berichtslaufs auf der Seite „Bericht": eine kurze Erfolgszeile mit
/// Dateiname, Vorlage und „Öffnen", die Warnungen sichtbar, die Hinweise als Klappzeile im
/// Muster des Hinweisbands der Simulationsseite — eingeklappt als Vorgabe, aufgeklappt eine
/// gegliederte Liste je Stand.
/// </summary>
public class BerichtSeiteErgebnisTests : EposBunitContext
{
    public BerichtSeiteErgebnisTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const string PFAD = @"C:\Users\Anwender\Documents\Musterhaus_Bericht_2026-09-26.docx";

    private static BerichtStand Stand() => new BerichtStand
    {
        Varianten = new[]
        {
            new VarianteZeile { IdProjekt = 1030, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                                Projektname = "Musterhaus", IstStamm = true }
        },
        GewaehlteVarianten = new[] { 1030 },
        Bausteine = new[] { new BausteinZeile { Schluessel = "KOPF", Titel = "Projektkopf" } },
        AktiveBausteine = new[] { "KOPF" },
        Zielordner = @"C:\Berichte"
    };

    private static LaufErgebnis Ergebnis(IReadOnlyList<Laufhinweisgruppe>? warnungen = null,
                                         IReadOnlyList<Laufhinweisgruppe>? hinweise = null) => new LaufErgebnis
    {
        Erfolg = true,
        Statuszeile = "Bericht erstellt: " + PFAD,
        Meldung = "Bericht erstellt:\r\n" + PFAD + "\r\n\r\nLANGER FLIESSTEXT",
        Datei = PFAD,
        Dateien = new[] { PFAD },
        Vorlage = "Standard (EPOS-Plan)",
        Warnungen = warnungen ?? Array.Empty<Laufhinweisgruppe>(),
        Hinweise = hinweise ?? Array.Empty<Laufhinweisgruppe>()
    };

    private static IReadOnlyList<Laufhinweisgruppe> Hinweisgruppen() => new[]
    {
        new Laufhinweisgruppe("Alle Stände", new[]
        {
            new Laufhinweispunkt("Zeitbasis Klimadaten: UTC -> MEZ/MESZ"),
            new Laufhinweispunkt("Wirtschaftlichkeit — CO₂-Preis: jahresgenauer Pfad")
        }),
        new Laufhinweisgruppe("Variante „mit PV“", new[] { new Laufhinweispunkt("PV-Modul: Nennleistung weicht ab") }),
        new Laufhinweisgruppe("Word-Bericht", new[]
        {
            new Laufhinweispunkt("Platzhalter ohne Wert, mit dem Leerwert gefüllt: 1",
                                 new[] { "{{projekt.kunde}}: leer" }, Platzhalter: true)
        })
    };

    private string? _geoeffnet;

    private IRenderedComponent<BerichtSeite> Laufe(LaufErgebnis erg, bool mitKatalog = false)
    {
        var cut = Render<BerichtSeite>(p =>
        {
            p.Add(x => x.Laden, () => Stand());
            p.Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m) => Task.FromResult(erg));
            p.Add(x => x.DateiOeffnen, (string d) => { _geoeffnet = d; return Task.CompletedTask; });
            p.Add(x => x.Erfolgsdauer, (TimeSpan?)null);
            if (mitKatalog)
                p.Add(x => x.PlatzhalterkatalogGaben,
                      () => (IReadOnlyDictionary<string, object>?)new Dictionary<string, object>());
        });
        cut.Find(".epos-leiste .epos-knopf--primaer").Click();                    // Erstellen
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja zum Start
        return cut;
    }

    [Fact]
    public void Die_Erfolgszeile_ist_kurz_Dateiname_und_Vorlage_der_Pfad_steht_am_Knopf()
    {
        var cut = Laufe(Ergebnis(hinweise: Hinweisgruppen()));

        IElement zeile = cut.Find(".epos-bericht-erfolg .epos-warnbanner");
        Assert.Contains("epos-warnbanner--erfolg", zeile.ClassName);
        Assert.Equal("Bericht erstellt: Musterhaus_Bericht_2026-09-26.docx — Vorlage „Standard (EPOS-Plan)“",
                     zeile.QuerySelector(".epos-warnbanner-text")!.TextContent);
        Assert.DoesNotContain(@"C:\Users", zeile.QuerySelector(".epos-warnbanner-text")!.TextContent);
        Assert.DoesNotContain("LANGER FLIESSTEXT", cut.Markup);   // der Fließtext steht nicht mehr da

        IElement knopf = zeile.QuerySelector(".epos-warnbanner-aktion")!;
        Assert.Equal("Öffnen", knopf.TextContent);
        Assert.Equal(PFAD, knopf.GetAttribute("title"));
    }

    [Fact]
    public void Oeffnen_an_der_Erfolgszeile_oeffnet_die_Datei()
    {
        var cut = Laufe(Ergebnis());
        // Die Rückfrage „öffnen?" gibt es hier nicht (Frage leer) — der Knopf genügt.
        cut.Find(".epos-bericht-erfolg .epos-warnbanner-aktion").Click();
        Assert.Equal(PFAD, _geoeffnet);
    }

    [Fact]
    public void Die_Hinweise_sind_eingeklappt_und_die_Zeile_nennt_ihre_Zahl()
    {
        var cut = Laufe(Ergebnis(hinweise: Hinweisgruppen()));

        IElement band = cut.Find(".epos-bericht-hinweisband");
        Assert.Contains("epos-simerg-laufband", band.ClassName);   // derselbe Baustil wie die Simulationsseite
        Assert.Equal("false", band.GetAttribute("aria-expanded"));
        Assert.Equal("4", band.QuerySelector(".epos-simerg-laufband-zahl")!.TextContent);
        Assert.Contains("4 Hinweise zum Bericht", band.TextContent);
        Assert.Contains("anzeigen", band.TextContent);
        Assert.Empty(cut.FindAll(".epos-bericht-hinweise"));
        Assert.DoesNotContain("Zeitbasis Klimadaten", cut.Markup);
    }

    [Fact]
    public void Aufklappen_zeigt_die_Gruppen_und_die_gleichen_Hinweise_einmal()
    {
        var cut = Laufe(Ergebnis(hinweise: Hinweisgruppen()));
        cut.Find(".epos-bericht-hinweisband").Click();

        Assert.Equal("true", cut.Find(".epos-bericht-hinweisband").GetAttribute("aria-expanded"));
        Assert.Contains("ausblenden", cut.Find(".epos-bericht-hinweisband").TextContent);
        IElement liste = cut.Find(".epos-bericht-hinweise");
        Assert.Equal(new[] { "Alle Stände", "Variante „mit PV“", "Word-Bericht" },
                     liste.QuerySelectorAll(".epos-untergruppe").Select(e => e.TextContent).ToArray());
        Assert.Single(liste.QuerySelectorAll("li").Where(l => l.TextContent.StartsWith("Zeitbasis Klimadaten")));
        Assert.Contains("{{projekt.kunde}}: leer", liste.TextContent);

        // Zuklappen — der Zustand ist ein Feld der Seite.
        cut.Find(".epos-bericht-hinweisband").Click();
        Assert.Empty(cut.FindAll(".epos-bericht-hinweise"));
    }

    [Fact]
    public void Ohne_Katalog_bietet_der_Platzhalterpunkt_keinen_Verweis()
    {
        var cut = Laufe(Ergebnis(hinweise: Hinweisgruppen()));
        cut.Find(".epos-bericht-hinweisband").Click();
        Assert.Empty(cut.FindAll(".epos-bericht-katalog"));
    }

    [Fact]
    public void Mit_Katalog_bietet_der_Platzhalterpunkt_den_Platzhalterkatalog_an()
    {
        var cut = Laufe(Ergebnis(hinweise: Hinweisgruppen()), mitKatalog: true);
        cut.Find(".epos-bericht-hinweisband").Click();
        IElement verweis = Assert.Single(cut.FindAll(".epos-bericht-katalog"));
        Assert.Contains("Platzhalter ohne Wert", verweis.ParentElement!.TextContent);
    }

    [Fact]
    public void Eine_Warnung_bleibt_sichtbar_und_nicht_eingeklappt()
    {
        var warnungen = new[]
        {
            new Laufhinweisgruppe("Variante „mit Stromspeicher“", new[]
            {
                new Laufhinweispunkt("Für diesen Projektlauf fehlen die spezifischen Kostensätze der Speicherflotte.")
            })
        };
        var cut = Laufe(Ergebnis(warnungen: warnungen, hinweise: Hinweisgruppen()));

        IElement warnung = cut.Find(".epos-bericht-warnung .epos-warnbanner");
        Assert.Contains("epos-warnbanner--warnung", warnung.ClassName);
        Assert.Equal("Variante „mit Stromspeicher“: Für diesen Projektlauf fehlen die spezifischen Kostensätze der Speicherflotte.",
                     warnung.QuerySelector(".epos-warnbanner-text")!.TextContent);
        Assert.Equal("4", cut.Find(".epos-simerg-laufband-zahl").TextContent);   // die Warnung zählt nicht mit
    }

    [Fact]
    public void Ohne_Hinweise_gibt_es_keine_Klappzeile()
    {
        var cut = Laufe(Ergebnis());
        Assert.Single(cut.FindAll(".epos-bericht-erfolg"));
        Assert.Empty(cut.FindAll(".epos-bericht-hinweisband"));
        Assert.Empty(cut.FindAll(".epos-bericht-warnung"));
    }

    [Fact]
    public void Die_Erfolgszeile_verfaellt_Warnungen_und_Klappzeile_bleiben()
    {
        var warnungen = new[] { new Laufhinweisgruppe("Stamm", new[] { new Laufhinweispunkt("fehlt") }) };
        var cut = Render<BerichtSeite>(p =>
        {
            p.Add(x => x.Laden, () => Stand());
            p.Add(x => x.Erstellen, (BerichtAuftrag a, Action<Laufschritt> m)
                => Task.FromResult(Ergebnis(warnungen: warnungen, hinweise: Hinweisgruppen())));
            p.Add(x => x.Erfolgsdauer, TimeSpan.FromMilliseconds(20));
        });
        cut.Find(".epos-leiste .epos-knopf--primaer").Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-bericht-erfolg")), TimeSpan.FromSeconds(5));
        Assert.Single(cut.FindAll(".epos-bericht-warnung"));
        Assert.Single(cut.FindAll(".epos-bericht-hinweisband"));
    }

    [Fact]
    public void Ohne_Dateien_bleibt_es_beim_Fliesstext()
    {
        var cut = Laufe(new LaufErgebnis { Erfolg = true, Statuszeile = "fertig", Meldung = "Ergebnis steht." });
        Assert.Empty(cut.FindAll(".epos-bericht-erfolg"));
        Assert.Contains("Ergebnis steht.", cut.Find(".epos-warnbanner").TextContent);
    }
}
