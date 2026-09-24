using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// ETAPPE E17 (V‑G11, DIN EN 17463 6.1 und 8.2) — der Baustein <b>WirkungenListe</b> im
/// Bewertungsblock der Wirtschaftlichkeitsseite: je Wirkung Kategorie, Beschreibung, Dauer und
/// drei Wirkungsgrade als Wahlfelder, die Beurteilung als Anzeige; Zeilen hinzufügen und
/// entfernen; nur lesbar während eines Laufs. Dazu der Weg des Assistenten über die Seite.
///
/// <para><b>Kulturpinnung</b>: die Hausvorrichtung <see cref="EposBunitContext"/> (de-DE).</para>
/// </summary>
public class WirkungenListeTests : EposBunitContext
{
    public WirkungenListeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private int _geaendert;

    private IRenderedComponent<WirkungenListe> Zeige(List<ProjektWirkung> zeilen, bool nurLesen = false)
        => Render<WirkungenListe>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.NurLesen, nurLesen)
            .Add(x => x.Geaendert, () => _geaendert++));

    [Fact]
    public void Ohne_Wirkung_steht_der_Leerzustand_und_der_Knopf_zum_Hinzufuegen()
    {
        var cut = Zeige(new List<ProjektWirkung>());

        Assert.Equal(Resource.WIRT_NM_LEER, cut.Find(".epos-wirt-wirkungen-leer").TextContent);
        Assert.Empty(cut.FindAll("table"));
        Assert.Equal(Resource.WIRT_NM_ZEILE_NEU, cut.Find(".epos-wirt-wirkungen-neu").TextContent.Trim());
    }

    /// <summary>Der Feldbestand je Zeile: fünf Wahlfelder, ein Textfeld, die Beurteilung und das Kreuz;
    /// die Köpfe in der Reihenfolge der Norm.</summary>
    [Fact]
    public void Jede_Zeile_traegt_fuenf_Wahlfelder_ein_Textfeld_und_die_Beurteilung()
    {
        var cut = Zeige(new List<ProjektWirkung>
        {
            new() { Kategorie = NichtMonetaereWirkungen.FINANZIELL, Beschreibung = "Image", Dauer = 2, WirkungOrganisation = 3 },
            new() { Beschreibung = "Komfort" }
        });

        Assert.Equal(new[] { Resource.WIRT_NM_SP_KATEGORIE, Resource.WIRT_NM_SP_BESCHREIBUNG, Resource.WIRT_NM_SP_DAUER,
                             Resource.WIRT_NM_SP_ORGANISATION, Resource.WIRT_NM_SP_MITARBEITER, Resource.WIRT_NM_SP_UMWELT,
                             Resource.WIRT_NM_SP_BEURTEILUNG, Resource.WIRT_NM_SP_ENTFERNEN },
                     cut.FindAll("thead th").Select(t => t.TextContent.Trim()).ToArray());

        IReadOnlyList<IElement> zeilen = cut.FindAll("tbody tr");
        Assert.Equal(2, zeilen.Count);
        Assert.Equal(5, zeilen[0].QuerySelectorAll("select").Length);
        Assert.Single(zeilen[0].QuerySelectorAll("input.epos-eingabe"));

        // Die gewählten Optionen tragen selected; ein leeres Wahlfeld zeigt „—".
        IElement[] wahl = zeilen[0].QuerySelectorAll("select").ToArray();
        Assert.Equal(Resource.WIRT_NM_KAT_FINANZIELL, Gewaehlt(wahl[0]));
        Assert.Equal(Resource.WIRT_NM_DAUER_2, Gewaehlt(wahl[1]));
        Assert.Equal(Resource.WIRT_NM_WIRKUNG_3, Gewaehlt(wahl[2]));
        Assert.Equal("—", Gewaehlt(wahl[3]));

        Assert.Equal("6 von 9", zeilen[0].QuerySelector(".epos-wirt-wirkungen-beurteilung")!.TextContent.Trim());
        Assert.Equal(Resource.WIRT_NM_NICHT_BEURTEILT,
                     zeilen[1].QuerySelector(".epos-wirt-wirkungen-beurteilung")!.TextContent.Trim());

        // Die Felder sprechen ihre Zeile an.
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Resource.WIRT_NM_FELD, Resource.WIRT_NM_SP_DAUER, 2),
                     zeilen[1].QuerySelectorAll("select")[1].GetAttribute("aria-label"));
    }

    private static string Gewaehlt(IElement select)
        => select.QuerySelectorAll("option").First(o => o.HasAttribute("selected")).TextContent.Trim();

    /// <summary>Eine Wahl schreibt in die Zeile, meldet die Änderung, und die Beurteilung folgt sofort.</summary>
    [Fact]
    public void Eine_Wahl_setzt_den_Wert_und_die_Beurteilung_folgt()
    {
        var zeilen = new List<ProjektWirkung> { new() { Beschreibung = "Komfort" } };
        var cut = Zeige(zeilen);

        cut.FindAll("select")[1].Change("3");                // Dauer lang
        cut.FindAll("select")[3].Change("1");                // Mitarbeiter gering
        cut.FindAll("select")[0].Change("0");                // Energiefluss
        cut.Find("input.epos-eingabe").Input("Komfort und Ruhe");
        cut.Render();

        Assert.Equal(3, zeilen[0].Dauer);
        Assert.Equal(1, zeilen[0].WirkungMitarbeiter);
        Assert.Equal(NichtMonetaereWirkungen.ENERGIEFLUSS, zeilen[0].Kategorie);
        Assert.Equal("Komfort und Ruhe", zeilen[0].Beschreibung);
        Assert.Equal(4, _geaendert);
        Assert.Equal("3 von 9", cut.Find(".epos-wirt-wirkungen-beurteilung").TextContent.Trim());

        // Die leere Wahl nimmt den Wert zurück: nicht beurteilt.
        cut.FindAll("select")[1].Change("");
        Assert.Null(zeilen[0].Dauer);
        cut.Render();
        Assert.Equal(Resource.WIRT_NM_NICHT_BEURTEILT, cut.Find(".epos-wirt-wirkungen-beurteilung").TextContent.Trim());
    }

    [Fact]
    public void Hinzufuegen_haengt_eine_Zeile_sonstig_an_und_das_Kreuz_nimmt_genau_ihre_weg()
    {
        var zeilen = new List<ProjektWirkung> { new() { Beschreibung = "A" }, new() { Beschreibung = "B" } };
        var cut = Zeige(zeilen);

        cut.Find(".epos-wirt-wirkungen-neu").Click();
        Assert.Equal(3, zeilen.Count);
        Assert.Equal(NichtMonetaereWirkungen.SONSTIG, zeilen[2].Kategorie);
        Assert.Equal("", zeilen[2].Beschreibung);

        cut.Render();
        cut.FindAll(".epos-wirt-wirkungen-loeschen")[1].Click();
        Assert.Equal(new[] { "A", "" }, zeilen.Select(z => z.Beschreibung));
        Assert.Equal(2, _geaendert);
    }

    /// <summary>Nur lesbar: Wahlfelder gesperrt, Textfeld nur lesen, beide Knöpfe gesperrt, nichts ändert sich.</summary>
    [Fact]
    public void Nur_lesbar_sperrt_Wahl_Text_und_Knoepfe()
    {
        var zeilen = new List<ProjektWirkung> { new() { Beschreibung = "A" } };
        var cut = Zeige(zeilen, nurLesen: true);

        Assert.All(cut.FindAll("select"), s => Assert.True(s.HasAttribute("disabled")));
        Assert.True(cut.Find("input.epos-eingabe").HasAttribute("readonly"));
        Assert.True(cut.Find(".epos-wirt-wirkungen-neu").HasAttribute("disabled"));
        Assert.True(cut.Find(".epos-wirt-wirkungen-loeschen").HasAttribute("disabled"));
        Assert.Single(zeilen);
    }

    // =====================================================================
    //  Der Assistent auf der Seite (Feldkarte wirkung_*)
    // =====================================================================

    /// <summary>
    /// Die Zahl der Wirkungen legt Zeilen an (und öffnet den Block), die Spalten setzen Kategorie,
    /// Beschreibung, Dauer und Wirkungsgrade der Zeile, die Beurteilung ist nur lesbar; geschrieben
    /// wird erst mit „Speichern".
    /// </summary>
    [Fact]
    public void Der_Assistent_legt_eine_Wirkung_an_und_beurteilt_sie()
    {
        IReadOnlyList<ProjektWirkung>? geschrieben = null;
        var cut = Render<WirtschaftlichkeitSeite>(p => p
            .Add(x => x.Laden, () => new WirtschaftlichkeitStand())
            .Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> l) => { geschrieben = l.ToList(); return true; }));

        KiFeldzugang anzahl = KiMaskenbruecke.Feldzugang(KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE, "wirkung_anzahl");
        Assert.NotNull(anzahl);
        Assert.True(anzahl.Setzbar);
        KiFeldumsetzung eins = KiFeldwandler.Wandle(anzahl, "1");
        Assert.True(eins.Ok, eins.Grund);
        cut.InvokeAsync(() => anzahl.Setzen(eins.Wert));
        cut.Render();
        Assert.True(cut.Instance.BewertungOffen);
        Assert.Single(cut.Instance.Wirkungen);

        string Feld(string praefix) => KiMaskenbruecke.Lesen(KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE)
                                                      .Select(w => w.Name)
                                                      .First(n => n.StartsWith(praefix, StringComparison.Ordinal));

        KiFeldzugang beschreibung = KiMaskenbruecke.Feldzugang(KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE, Feld("wirkung_beschreibung"));
        cut.InvokeAsync(() => beschreibung.Setzen("Versorgungssicherheit"));

        foreach ((string praefix, string text) in new[] { ("wirkung_dauer", "lang"), ("wirkung_umwelt", "mittel"),
                                                           ("wirkung_kategorie", "Energiefluss") })
        {
            KiFeldzugang f = KiMaskenbruecke.Feldzugang(KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE, Feld(praefix));
            Assert.NotNull(f);
            KiFeldumsetzung u = KiFeldwandler.Wandle(f, text);
            Assert.True(u.Ok, praefix + ": " + u.Grund);
            cut.InvokeAsync(() => f.Setzen(u.Wert));
        }
        cut.Render();

        ProjektWirkung w = Assert.Single(cut.Instance.Wirkungen);
        Assert.Equal("Versorgungssicherheit", w.Beschreibung);
        Assert.Equal(3, w.Dauer);
        Assert.Equal(2, w.WirkungUmwelt);
        Assert.Equal(NichtMonetaereWirkungen.ENERGIEFLUSS, w.Kategorie);
        Assert.Equal("6 von 9", cut.Find(".epos-wirt-wirkungen-beurteilung").TextContent.Trim());

        KiFeldzugang beurteilung = KiMaskenbruecke.Feldzugang(KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE, Feld("wirkung_beurteilung"));
        Assert.False(beurteilung.Setzbar);
        Assert.Equal("6 von 9", Convert.ToString(beurteilung.Lesen(), CultureInfo.InvariantCulture));

        // Bis „Speichern" ist nichts geschrieben.
        Assert.Null(geschrieben);

        // Die Zahl 0 nimmt die Zeile wieder weg.
        KiFeldumsetzung null_ = KiFeldwandler.Wandle(anzahl, "0");
        Assert.True(null_.Ok, null_.Grund);
        cut.InvokeAsync(() => anzahl.Setzen(null_.Wert));
        cut.Render();
        Assert.Empty(cut.Instance.Wirkungen);
    }
}
