using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Aufbau in zwei Ebenen (Konzept Zapfprofil-Benutzerführung ZB3, Entscheid ZB-Q1 A; Welle W4): die Karten des
/// Zonenblocks und der Gruppe „Gebäude und Anlage" mit ihrer Zusammenfassung, ihr Auf/Zu über den Zonenwechsel, die
/// Karten je Stufe, die Hinweise der Stufe Einfach (ZB8) und das Blatt „Katalog der Nutzungsarten" (ZB9).
/// </summary>
public partial class ZapfprofilDialogStufenTests
{
    private static string[] Kartentitel(IRenderedComponent<ZapfprofilDialog> cut)
        => cut.FindAll(".epos-zapfprofil-karte .epos-klappgruppe-text").Select(k => k.TextContent.Trim()).ToArray();

    [Fact]
    public void ZB3_Die_Stufe_bestimmt_die_Karten_und_Nutzung_steht_immer_offen()
    {
        var cut = Aufbauen();
        Assert.Empty(Kartentitel(cut));
        Assert.True(HatFeld(cut, "Nutzungsart"));

        Stufe(cut, "Erweitert", aufklappen: false);
        Assert.Equal(new[] { "Belegung", "Kalender", "Messwert", "Tagesbedarf", "Zirkulation", "Ladeleistung und Ladefenster",
                             "Leitungsinhalt", "Stochastik · Jahresreihe" }, Kartentitel(cut));
        // „Nutzung" hat keine Klappe: Ihre Felder stehen in jeder Stufe.
        Assert.Empty(cut.FindAll(".epos-zapfprofil-karte--nutzung .epos-klappgruppe-knopf"));
        Assert.True(HatFeld(cut, "Bezugsgröße"));

        Stufe(cut, "Experte", aufklappen: false);
        Assert.Equal(new[] { "Belegung", "Kalender", "Messwert", "Tagesbedarf", "Fachwerte der Zone", "Zirkulation",
                             "Ladeleistung und Ladefenster", "Leitungsinhalt", "Fachwerte · Gebäude", "Stochastik · Jahresreihe" },
                     Kartentitel(cut));
        // Der Typtagweg ist eine Angabe des Projekts: Er steht bei der Jahresreihe, nicht bei den Fachwerten der Zone.
        ZapfprofilKarten.Auf(cut, "jahresreihe");
        Assert.NotNull(cut.Find(".epos-zapfprofil-karte--jahresreihe .epos-zapfprofil-typtage"));
    }

    [Fact]
    public void ZB3_Auf_und_Zu_jeder_Karte_bleibt_beim_Zonenwechsel_und_beim_Stufenwechsel()
    {
        var cut = Aufbauen();
        Stufe(cut, "Erweitert", aufklappen: false);

        ZapfprofilKarten.Knopf(cut, "messwert").Click();
        ZapfprofilKarten.Knopf(cut, "ladeleistung").Click();
        Assert.True(cut.Instance.KarteIstOffen("messwert"));
        Assert.True(HatFeld(cut, "Jahresmesswert"));
        Assert.False(HatFeld(cut, "Anlagentopologie"));

        // Zonenwechsel: Die Karten der neuen Zone stehen wie zuvor - offen, was offen war.
        cut.FindAll("table.epos-zapfprofil-zonen tbody tr")[1].QuerySelector("button")!.Click();
        Assert.Equal("Zone „Büro“", Gruppen(cut)[1]);
        Assert.Equal("true", ZapfprofilKarten.Knopf(cut, "messwert").GetAttribute("aria-expanded"));
        Assert.Equal("false", ZapfprofilKarten.Knopf(cut, "belegung").GetAttribute("aria-expanded"));
        Assert.True(HatFeld(cut, "Jahresmesswert"));
        Assert.True(HatFeld(cut, "Ladezeitfenster"));

        // Zuklappen wirkt ebenso über den Wechsel hinweg.
        ZapfprofilKarten.Knopf(cut, "messwert").Click();
        cut.FindAll("table.epos-zapfprofil-zonen tbody tr")[0].QuerySelector("button")!.Click();
        Assert.Equal("false", ZapfprofilKarten.Knopf(cut, "messwert").GetAttribute("aria-expanded"));
        Assert.False(HatFeld(cut, "Jahresmesswert"));

        // Über Einfach und zurück bleibt die Ladeleistung offen.
        Stufe(cut, "Einfach", aufklappen: false);
        Stufe(cut, "Erweitert", aufklappen: false);
        Assert.Equal("true", ZapfprofilKarten.Knopf(cut, "ladeleistung").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void ZB3_Die_Zusammenfassungen_nennen_die_wirksamen_Werte()
    {
        ZapfprofilEingabeDaten e = Eingabe();
        ZapfprofilZonenangabenDaten a = e.Zonen[0].Angaben!;
        a.Wohnungen.Add(new ZapfprofilWohnungDaten { Anzahl = 20, Personen = 2 });
        a.Wohnungen.Add(new ZapfprofilWohnungDaten { Anzahl = 4, Personen = 3 });
        a.Topologie = ZapfprofilTopologie.Durchfluss;
        a.Zirkulation = false;
        a.Jahresmesswert = 12000;
        e.Gebaeude!.LeitungsinhaltL = 4.5;
        e.Gebaeude.LadefensterH = 6;
        e.Gebaeude.KaltwasserAuslegungC = 10;
        e.JahresreiheStochastisch = true;
        e.Realisierungen = 20;
        var cut = Aufbauen(Daten(e));
        Stufe(cut, "Experte", aufklappen: false);

        Assert.Equal("24 WE aus der Wohnungstabelle · Durchfluss · Zirkulation nein", ZapfprofilKarten.Zusatz(cut, "belegung"));
        Assert.Equal("12.000 kWh/a", ZapfprofilKarten.Zusatz(cut, "messwert"));
        Assert.Equal("4,5 l", ZapfprofilKarten.Zusatz(cut, "leitungsinhalt"));
        Assert.Equal("auto · Ladefenster 6 h ab 22 Uhr", ZapfprofilKarten.Zusatz(cut, "ladeleistung"));
        Assert.Equal("1 eigener Wert", ZapfprofilKarten.Zusatz(cut, "fachwerte-gebaeude"));
        Assert.Equal("stochastisch · 20 Jahre · Seed 1", ZapfprofilKarten.Zusatz(cut, "jahresreihe"));

        // Ohne Wohnungstabelle nennt die Belegung die Personen je WE, wo sie eingetragen sind.
        ZapfprofilKarten.Auf(cut, "belegung");
        cut.FindAll(".epos-zapfprofil-wohnung-entfernen")[0].Click();
        cut.FindAll(".epos-zapfprofil-wohnung-entfernen")[0].Click();
        Feld(cut, "Personen je Wohneinheit").Input("2,2");
        Assert.Equal("2,2 Personen je WE · Durchfluss · Zirkulation nein", ZapfprofilKarten.Zusatz(cut, "belegung"));
    }

    // ---- ZB8: Hinweise der Stufe Einfach --------------------------------------------------------

    /// <summary>Eine Vorschau mit je einem Hinweis, den Einfach beheben kann, und einem, der erst ab Erweitert steht.</summary>
    private static ZapfprofilVorschauDaten VorschauMitHinweisen(ZapfprofilEingabeDaten e)
    {
        ZapfprofilVorschauDaten v = Vorschau(e);
        v.Warnliste.Clear();
        v.Warnliste.Add(new ZapfprofilWarnDaten("ZPG_WARN_BEDARF_AUSSERHALB_BANDBREITE", "Bedarf außerhalb der Bandbreite",
                                                "Der Bedarf liegt außerhalb der Bandbreite.", ZapfprofilWarnstufe.Hinweis, 0));
        v.Warnliste.Add(new ZapfprofilWarnDaten("ZPG_WARN_ZIRKULATION_OHNE_FLAECHE", "Zirkulation ohne Fläche",
                                                "Keine Fläche für den Flächenkennwert.", ZapfprofilWarnstufe.Hinweis) { AbErweitert = true });
        v.Meldungen.Add(new ZapfprofilMeldung("ZPG_SATZ_BAND", "Wohnen", "Bandbreite am Block.", ZapfprofilMeldungsart.Hinweis, "", 0));
        v.Meldungen.Add(new ZapfprofilMeldung("ZPG_SATZ_ZONE_OHNE_FLAECHE", "Wohnen", "Zone außerhalb der Zirkulation am Block.",
                                              ZapfprofilMeldungsart.Hinweis, "", 0) { AbErweitert = true });
        v.Meldungen.Add(new ZapfprofilMeldung("ZPG_SATZ_NETZ", "", "Netzverlust allgemein.", ZapfprofilMeldungsart.Hinweis) { AbErweitert = true });
        return v;
    }

    [Fact]
    public void ZB8_Einfach_zeigt_nur_Hinweise_die_dort_zu_beheben_sind()
    {
        ZapfprofilEingabeDaten e = Eingabe();
        ZapfprofilDaten d = Daten(e);
        d.Vorschau = VorschauMitHinweisen(e);
        var cut = Aufbauen(d, vorschau: VorschauMitHinweisen);

        string[] Warnungen() => cut.FindAll(".epos-zapfprofil-warnliste li").Select(l => l.GetAttribute("data-kennung") ?? "").ToArray();
        Assert.Equal(new[] { "ZPG_WARN_BEDARF_AUSSERHALB_BANDBREITE" }, Warnungen());
        Assert.Contains("Bandbreite am Block.", cut.Markup);
        Assert.DoesNotContain("Zone außerhalb der Zirkulation am Block.", cut.Markup);
        Assert.DoesNotContain("Netzverlust allgemein.", cut.Markup);

        Stufe(cut, "Erweitert", aufklappen: false);
        Assert.Equal(new[] { "ZPG_WARN_BEDARF_AUSSERHALB_BANDBREITE", "ZPG_WARN_ZIRKULATION_OHNE_FLAECHE" }, Warnungen());
        Assert.Contains("Zone außerhalb der Zirkulation am Block.", cut.Markup);
    }

    // ---- ZB9: das Blatt „Katalog der Nutzungsarten" ---------------------------------------------

    [Fact]
    public void ZB9_Katalog_oeffnet_das_Blatt_die_Zeile_waehlt_und_fuehrt_zurueck()
    {
        var cut = Aufbauen();
        Stufe(cut, "Erweitert", aufklappen: false);

        Knopf(cut, "Katalog…").Click();
        Assert.True(cut.Instance.KatalogOffen);
        // Das Blatt tauscht den Inhalt: keine Zonenliste, keine Leiste, nur das Blatt mit seinem Rückweg.
        Assert.Empty(cut.FindAll("table.epos-zapfprofil-zonen"));
        Assert.Empty(cut.FindAll(".epos-zapfprofil-vorschauspalte"));
        IElement blatt = cut.Find(".epos-blatt.epos-zapfprofil-katalogblatt");
        Assert.Equal("Katalog der Nutzungsarten", blatt.QuerySelector(".epos-blatt-titel")!.TextContent.Trim());
        Assert.Contains("‹ Brauchwasser-Zapfprofil – Beispielprojekt", blatt.QuerySelector(".epos-blatt-zurueck")!.TextContent);
        Assert.Contains("Nutzungsart der Zone „Wohnen“", blatt.TextContent);
        IElement[] zeilen = blatt.QuerySelectorAll("tbody tr").ToArray();
        Assert.Equal(2, zeilen.Length);
        Assert.Contains("Arbeitstage", zeilen[1].TextContent);
        Assert.Contains("fiktiv (Testdaten) · Testquelle", zeilen[1].TextContent);
        // Nur lesend: keine Eingabefelder im Blatt.
        Assert.Empty(blatt.QuerySelectorAll("input:not([type=radio]), select, textarea"));

        // Die Zeile „Büro B" wählt die Nutzungsart der Zone und führt zurück.
        zeilen[1].QuerySelector("button, input")!.Click();
        Assert.False(cut.Instance.KatalogOffen);
        Assert.Equal(2, cut.Instance.Eingabe.Zonen[0].IdNutzungsart);
        Assert.NotNull(cut.Find("table.epos-zapfprofil-zonen"));

        // Esc auf dem Blatt führt ohne Wahl zurück - und schließt den Dialog nicht.
        ZapfprofilErgebnisDaten? ergebnis = null;
        bool geschlossen = false;
        var zwei = Aufbauen(geschlossen: x => { ergebnis = x; geschlossen = true; });
        Stufe(zwei, "Erweitert", aufklappen: false);
        Knopf(zwei, "Katalog…").Click();
        zwei.Find(".epos-blatt").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(zwei.Instance.KatalogOffen);
        Assert.False(geschlossen);
        Assert.Equal(1, zwei.Instance.Eingabe.Zonen[0].IdNutzungsart);

        // Der Rückknopf ebenso.
        Knopf(zwei, "Katalog…").Click();
        zwei.Find(".epos-blatt-zurueck").Click();
        Assert.False(zwei.Instance.KatalogOffen);
        Assert.Equal(1, zwei.Instance.Eingabe.Zonen[0].IdNutzungsart);
        Assert.Null(ergebnis);
    }
}
