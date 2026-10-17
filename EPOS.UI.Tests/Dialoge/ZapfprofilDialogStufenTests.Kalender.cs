using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Karte „Kalender" der Zone (Konzept Zapfprofil-Benutzerführung ZB5, Welle W3b): zugeklappt die Zusammenfassung
/// der wirksamen Lage, aufgeklappt Quelle, die Werte des gebundenen Gebäudes, die eigenen Ferien als Liste mit
/// Datumsfeldern und das Jahresraster aus dem Rechenweg der Vorschau.
/// </summary>
public partial class ZapfprofilDialogStufenTests
{
    private static IElement Karte(IRenderedComponent<ZapfprofilDialog> cut) => cut.Find(".epos-zapfprofil-kalender");

    private static string Zusammenfassung(IRenderedComponent<ZapfprofilDialog> cut)
        => cut.Find(".epos-zapfprofil-kalender .epos-klappgruppe-zusatz").TextContent.Trim();

    private static void Aufklappen(IRenderedComponent<ZapfprofilDialog> cut)
        => ZapfprofilKarten.Auf(cut, "kalender");

    /// <summary>Die Datumsfelder „von" bzw. „bis" der eigenen Ferien, je Zeile eines.</summary>
    private static IElement[] Datumsfelder(IRenderedComponent<ZapfprofilDialog> cut, string bezeichnung)
        => cut.FindAll(".epos-zapfprofil-ferienzeile label.epos-gemeinjahrdatum")
              .Where(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == bezeichnung)
              .Select(l => l.QuerySelector("input")!).ToArray();

    private static ZapfprofilZonenangabenDaten Angaben(IRenderedComponent<ZapfprofilDialog> cut)
        => cut.Instance.Eingabe.Zonen[0].Angaben!;

    [Fact]
    public void Die_Karte_Kalender_ist_zugeklappt_und_nennt_die_Lage_der_Nutzungsart()
    {
        var cut = Aufbauen();
        Stufe(cut, "Experte", aufklappen: false);

        Assert.Equal("false", cut.Find(".epos-zapfprofil-kalender .epos-klappgruppe-knopf").GetAttribute("aria-expanded"));
        Assert.Equal("Kalender der Nutzungsart · keine eigenen Ferien", Zusammenfassung(cut));
        Assert.Empty(Karte(cut).QuerySelectorAll(".epos-zapfprofil-ferienliste"));
        Assert.DoesNotContain("Wirkt erst mit einer Kalendertabelle je Bundesland", cut.Markup);

        Aufklappen(cut);
        Assert.Equal("true", cut.Find(".epos-zapfprofil-kalender .epos-klappgruppe-knopf").GetAttribute("aria-expanded"));
        // Ein Gebäude im Projekt: die Quelle als waagerechte Wahl aus zwei Einträgen, ohne Gebäude keine Werte „aus dem Gebäude".
        IElement quelle = cut.Find(".epos-zapfprofil-kalender fieldset[aria-label='Quelle']");
        Assert.Equal(new[] { "Kalender der Nutzungsart · Wohnen", "Gebäude „Haus Nord“" },
                     quelle.QuerySelectorAll("label.epos-option").Select(l => l.TextContent.Trim()).ToArray());
        Assert.Empty(cut.FindAll(".epos-zapfprofil-kalender-gebaeude"));
        // ZB8: Format und Vorrang der eigenen Ferien stehen im title, nicht als Dauerzeile.
        Assert.Contains("Eigene Ferien der Zone gehen vor", Karte(cut).QuerySelector(".epos-zapfprofil-ferien-neu")!.GetAttribute("title"));
    }

    [Fact]
    public void Ein_gebundenes_Gebaeude_zeigt_Wochenende_Feiertage_und_Ferien_aus_dem_Gebaeude()
    {
        var cut = Aufbauen();
        Stufe(cut, "Experte");
        Aufklappen(cut);

        Option(cut, "Quelle", "Gebäude „Haus Nord“", "10614");
        Assert.Equal(10614, Angaben(cut).IdGebaeude);
        Assert.Equal("wie Gebäude „Haus Nord“ · 2 Ferienzeiträume · Feiertage Baden-Württemberg", Zusammenfassung(cut));

        IElement gebaeude = cut.Find(".epos-zapfprofil-kalender-gebaeude");
        Assert.Contains("aus dem Gebäude „Haus Nord“", gebaeude.TextContent);
        string[] werte = gebaeude.QuerySelectorAll("dd").Select(d => d.TextContent.Trim()).ToArray();
        Assert.Equal("Sa + So", werte[0]);
        Assert.Equal("Baden-Württemberg", werte[1]);
        Assert.Equal(new[] { "Sommer: 01.07. – 14.08.", "Ferien 2: 26.10. – 30.10." },
                     gebaeude.QuerySelectorAll(".epos-zapfprofil-kalender-ferien li").Select(l => l.TextContent.Trim()).ToArray());

        // Eigene Ferien gehen vor: Die Zusammenfassung nennt sie, die Karte sagt, dass die des Gebäudes nicht gelten.
        Knopf(cut, "+ Ferien").Click();
        Datumsfelder(cut, "bis")[0].Change("06.01.");
        Assert.Equal("wie Gebäude „Haus Nord“ · 1 eigener Ferienzeitraum · Feiertage Baden-Württemberg", Zusammenfassung(cut));
        Assert.Contains("Die Zone trägt eigene Ferien; die Ferien des Gebäudes gelten nicht.", cut.Markup);

        // Zurück auf den Kalender der Nutzungsart löst die Bindung.
        Option(cut, "Quelle", "Kalender der Nutzungsart · Wohnen", "0");
        Assert.Null(Angaben(cut).IdGebaeude);
        Assert.Empty(cut.FindAll(".epos-zapfprofil-kalender-gebaeude"));
    }

    [Fact]
    public void Mehrere_Gebaeude_stehen_als_Auswahlliste_und_ein_Gebaeude_ohne_Land_feiert_bundeseinheitlich()
    {
        ZapfprofilDaten daten = Daten();
        daten.Gebaeude.Add(new ZapfprofilKatalogeintragDaten { Id = 20, Name = "Haus Süd" });
        daten.Gebaeudekalender.Add(new ZapfprofilGebaeudekalenderDaten { Id = 20, Name = "Haus Süd", Wochenende = "So" });
        var cut = Aufbauen(daten);
        Stufe(cut, "Experte");
        Aufklappen(cut);

        Auswahl(cut, "Quelle").Change("20");
        Assert.Equal(20, Angaben(cut).IdGebaeude);
        Assert.Equal("wie Gebäude „Haus Süd“ · keine Ferien · bundeseinheitliche Feiertage", Zusammenfassung(cut));
        string[] werte = cut.FindAll(".epos-zapfprofil-kalender-gebaeude dd").Select(d => d.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "So", "nur bundeseinheitliche", "keine" }, werte);
    }

    [Fact]
    public void Eigene_Ferien_ohne_Obergrenze_mit_Namen_Datum_und_Entfernen()
    {
        var cut = Aufbauen();
        Stufe(cut, "Experte");
        Aufklappen(cut);
        Assert.Empty(Angaben(cut).Ferien);

        string[] von = { "01.01.", "30.03.", "18.05.", "01.07.", "26.10." };
        string[] bis = { "06.01.", "10.04.", "29.05.", "14.08.", "30.10." };
        for (int i = 0; i < 5; i++)
        {
            Knopf(cut, "+ Ferien").Click();
            Datumsfelder(cut, "von")[i].Change(von[i]);
            Datumsfelder(cut, "bis")[i].Change(bis[i]);
        }
        Assert.Equal(5, cut.FindAll(".epos-zapfprofil-ferienzeile").Count);
        Assert.Equal(5, Angaben(cut).Ferien.Count);
        Assert.Equal((182, 226), (Angaben(cut).Ferien[3].Von, Angaben(cut).Ferien[3].Bis));
        Assert.Equal("Kalender der Nutzungsart · 5 eigene Ferienzeiträume", Zusammenfassung(cut));

        // Der Name ist wahlfrei; leer trägt er „Ferien n" als Platzhalter.
        IElement name = cut.FindAll(".epos-zapfprofil-ferienzeile")[3].QuerySelector("input")!;
        Assert.Equal("Ferien 4", name.GetAttribute("placeholder"));
        name.Input("Sommer");
        Assert.Equal("Sommer", Angaben(cut).Ferien[3].Name);

        // Ein halber Zeitraum wartet: Er steht in der Liste, zählt aber erst mit seinem Ende.
        Knopf(cut, "+ Ferien").Click();
        Datumsfelder(cut, "von")[5].Change("21.12.");
        Assert.Equal("Kalender der Nutzungsart · 5 eigene Ferienzeiträume", Zusammenfassung(cut));

        // „Entfernen" nimmt genau die Zeile heraus; die übrigen rücken nach.
        cut.FindAll(".epos-zapfprofil-ferienzeile")[1].QuerySelector("button.epos-zapfprofil-ferien-entfernen")!.Click();
        Assert.Equal(5, Angaben(cut).Ferien.Count);
        Assert.Equal("Sommer", Angaben(cut).Ferien[2].Name);
        Assert.Equal("Kalender der Nutzungsart · 4 eigene Ferienzeiträume", Zusammenfassung(cut));
    }

    [Fact]
    public void Ein_halber_oder_ungueltiger_Ferienzeitraum_haelt_das_OK_an_und_faellt_mit_der_Stufe_weg()
    {
        ZapfprofilErgebnisDaten? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);
        Stufe(cut, "Experte");
        Aufklappen(cut);

        // Den 30. Februar gibt es im Gemeinjahr nicht: Das Feld färbt und meldet, das OK hält an.
        Knopf(cut, "+ Ferien").Click();
        Datumsfelder(cut, "von")[0].Change("30.02.");
        Assert.Contains("epos-fehleingabe", Datumsfelder(cut, "von")[0].ClassName);
        Knopf(cut, "OK").Click();
        Assert.Null(ergebnis);
        Assert.Contains("Ferien 1 · von", cut.Instance.OkMeldung);

        Stufe(cut, "Einfach");
        Knopf(cut, "OK").Click();
        Assert.NotNull(ergebnis);
    }

    [Fact]
    public void Das_Jahresraster_zeigt_die_Tagtypen_der_Vorschau_mit_Legende()
    {
        var cut = Aufbauen();
        Stufe(cut, "Experte");
        Aufklappen(cut);

        IElement[] tage = cut.FindAll(".epos-zapfprofil-kalender .epos-jahresraster-tag").ToArray();
        Assert.Equal(365, tage.Length);
        // 6. Januar ein Samstag, 7. Januar ein Sonntag, 1. Mai ein Feiertag, 2. Januar ein Werktag.
        Assert.Contains("epos-kalb-art-1", tage[5].ClassName);
        Assert.Contains("epos-kalb-art-5", tage[6].ClassName);
        Assert.Contains("epos-kalb-art-5", tage[120].ClassName);
        Assert.Contains("epos-kalb-art-0", tage[1].ClassName);
        Assert.Equal("06.01. · Samstag", tage[5].GetAttribute("title"));
        string[] legende = cut.FindAll(".epos-zapfprofil-kalender .epos-kalb-legende li").Select(l => l.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Werktag · 260", "Samstag · 52", "Sonn-/Feiertag · 53", "Ferien · 0" }, legende);

        // Eigene Ferien gehen über die Vorschau ins Raster ein.
        Knopf(cut, "+ Ferien").Click();
        Datumsfelder(cut, "von")[0].Change("01.07.");
        Datumsfelder(cut, "bis")[0].Change("14.08.");
        cut.WaitForAssertion(() => Assert.Contains("epos-kalb-art-4",
            cut.FindAll(".epos-zapfprofil-kalender .epos-jahresraster-tag")[200].ClassName));
    }

    [Fact]
    public void Ohne_Tagtypen_der_Vorschau_nennt_das_Raster_seinen_Weg()
    {
        ZapfprofilDaten daten = Daten();
        foreach (ZapfprofilAnsichtDaten a in daten.Vorschau!.Ansichten) a.Tagtypen = null;
        var cut = Aufbauen(daten, vorschau: e =>
        {
            ZapfprofilVorschauDaten v = Vorschau(e);
            foreach (ZapfprofilAnsichtDaten a in v.Ansichten) a.Tagtypen = null;
            return v;
        });
        Stufe(cut, "Experte");
        Aufklappen(cut);

        Assert.Empty(cut.FindAll(".epos-jahresraster-tag"));
        Assert.Equal("Das Jahresraster erscheint mit der Vorschau.",
                     cut.Find(".epos-zapfprofil-jahresraster-ohne").TextContent.Trim());
    }
}
