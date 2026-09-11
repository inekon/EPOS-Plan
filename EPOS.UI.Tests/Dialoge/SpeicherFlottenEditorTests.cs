using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

public sealed class SpeicherFlottenEditorTests : EposBunitContext
{
    [Fact]
    public void Einheiten_lassen_sich_hinzufuegen_kopieren_und_entfernen()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, new FlottenStudieKonfiguration())
            .Add(x => x.WertChanged, x => gemeldet = x));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Speicher hinzufügen")).Click();
        Assert.NotNull(gemeldet);
        Assert.Single(gemeldet!.Einheiten);
        Assert.Single(gemeldet.Auslegung.Achsen);

        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Kopieren").Click();
        Assert.Equal(2, gemeldet.Einheiten.Count);
        Assert.Equal(2, gemeldet.Auslegung.Achsen.Count);
        Assert.NotEqual(gemeldet.Einheiten[0].Id, gemeldet.Einheiten[1].Id);

        cut.FindAll("button").First(x => x.TextContent.Trim() == "Entfernen").Click();
        Assert.Single(gemeldet.Einheiten);
        Assert.Single(gemeldet.Auslegung.Achsen);
    }

    [Fact]
    public void Rueckruf_erhaelt_einen_unabhaengigen_Snapshot()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var eingang = KonfigurationMitEinheit();
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, eingang)
            .Add(x => x.WertChanged, x => gemeldet = x));

        Eingabe(cut, "Maximale Ladeleistung:").Input("61");

        Assert.NotNull(gemeldet);
        Assert.Equal(61, gemeldet!.Einheiten[0].LadeleistungKw);
        Assert.Equal(40, eingang.Einheiten[0].LadeleistungKw);
        gemeldet.Einheiten[0].LadeleistungKw = 999;
        Assert.Equal(61, cut.Instance.AktuellerSnapshot.Einheiten[0].LadeleistungKw);
    }

    [Fact]
    public void Technik_haelt_Laden_und_Entladen_sowie_Wirkungsgrade_getrennt()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, KonfigurationMitEinheit())
            .Add(x => x.WertChanged, x => gemeldet = x));

        Eingabe(cut, "Maximale Ladeleistung:").Input("35");
        Eingabe(cut, "Maximale Entladeleistung:").Input("72");
        Eingabe(cut, "Ladewirkungsgrad:").Input("93");
        Eingabe(cut, "Entladewirkungsgrad:").Input("96");

        Assert.Equal(35, gemeldet!.Einheiten[0].LadeleistungKw);
        Assert.Equal(72, gemeldet.Einheiten[0].EntladeleistungKw);
        Assert.Equal(0.93, gemeldet.Einheiten[0].Ladewirkungsgrad, 10);
        Assert.Equal(0.96, gemeldet.Einheiten[0].Entladewirkungsgrad, 10);
    }

    /// <remarks>
    /// Multi Use PLANT und ist ohne Fahrplan-Löser gesperrt (Auftrag #170c); dieser Fall
    /// prüft die Felder des Ziels und setzt den Planer deshalb ausdrücklich voraus.
    /// </remarks>
    [Fact]
    public void MultiUse_zeigt_Peak_und_Prognoseparameter()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, KonfigurationMitEinheit())
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.WertChanged, x => gemeldet = x));

        Auswahl(cut, "Betriebsziel:").Change(((int)FlottenBetriebsziel.MultiUse).ToString());

        Assert.Contains("Wirtschaftlicher Peak-Zielwert", cut.Markup);
        Assert.Contains("Informationsstand", cut.Markup);
        Assert.Single(cut.FindAll("label"), x => x.TextContent.Contains("Wirtschaftlicher Peak-Zielwert"));
        Assert.Single(cut.FindAll("label"), x => x.TextContent.Contains("Laden aus dem Netz erlauben"));
        Assert.Single(cut.FindAll("label"), x => x.TextContent.Contains("Batterieexport ins Netz erlauben"));
        Auswahl(cut, "Informationsstand:").Change(((int)PrognoseArt.Oracle).ToString());
        Eingabe(cut, "Planungshorizont:").Input("96");

        Assert.Equal(PrognoseArt.Oracle, gemeldet!.Optionen.PrognoseArt);
        Assert.Equal(96, gemeldet.Optionen.PlanungshorizontIntervalle);
        Assert.Contains("Vergleichslauf", cut.Markup);
    }

    [Fact]
    public void Auslegungsachse_wechselt_von_Leistung_auf_CRate()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, KonfigurationMitEinheit())
            .Add(x => x.WertChanged, x => gemeldet = x));

        Auswahl(cut, "Größenkopplung:").Change(((int)FlottenAuslegungsmodus.KapazitaetUndCRate).ToString());

        Assert.NotNull(Kategorie(cut, "Kapazität [kWh]"));
        Assert.NotNull(Kategorie(cut, "C-Rate [1/h]"));
        Assert.DoesNotContain(cut.FindAll("section.epos-flotte-auslegungskategorie"),
            x => x.GetAttribute("aria-label") == "Leistung [kW]");
        KategorieEingabe(cut, "C-Rate [1/h]", "Bis:").Input("1,75");
        KategorieEingabe(cut, "Anzahl", "Bis:").Input("3");
        Assert.Equal(1.75, gemeldet!.Auslegung.Achsen[0].CRateBis, 10);
        Assert.Equal(3, gemeldet.Auslegung.Achsen[0].AnzahlBis);
    }

    [Fact]
    public void Auslegungsbereich_gruppiert_Anzahl_Kapazitaet_und_Leistung()
    {
        var cut = Render<SpeicherFlottenEditor>(p => p.Add(x => x.Wert, KonfigurationMitEinheit()));

        var kategorien = cut.FindAll("section.epos-flotte-auslegungskategorie");
        Assert.Contains(kategorien, x => x.GetAttribute("aria-label") == "Anzahl");
        Assert.Contains(kategorien, x => x.GetAttribute("aria-label") == "Kapazität [kWh]");
        Assert.Contains(kategorien, x => x.GetAttribute("aria-label") == "Leistung [kW]");
        Assert.DoesNotContain(kategorien, x => x.GetAttribute("aria-label") == "C-Rate [1/h]");
        Assert.Equal(3, Kategorie(cut, "Kapazität [kWh]").QuerySelectorAll("input").Length);
        Assert.Equal(3, Kategorie(cut, "Leistung [kW]").QuerySelectorAll("input").Length);
        Assert.Equal(3, Kategorie(cut, "Anzahl").QuerySelectorAll("input").Length);
        Assert.True(KategorieEingabe(cut, "Anzahl", "Schritt:").HasAttribute("disabled"));
        Assert.Equal("1", KategorieEingabe(cut, "Anzahl", "Schritt:").GetAttribute("value"));
    }

    [Fact]
    public void Projektlaufzeit_ist_immer_sichtbar_und_bleibt_im_Snapshot()
    {
        FlottenStudieKonfiguration? gemeldet = null;
        var cut = Render<SpeicherFlottenEditor>(p => p
            .Add(x => x.Wert, KonfigurationMitEinheit())
            .Add(x => x.WertChanged, x => gemeldet = x));

        Assert.Contains("Ein bereitgestelltes Jahr kann einmal bewertet", cut.Markup);
        Eingabe(cut, "Projektlaufzeit:").Input("1");
        Assert.False(gemeldet!.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen);
        Assert.Equal(1, gemeldet.Wirtschaftlichkeit.ProjektjahreBeiWiederholung);

        Auswahl(cut, "Jahresprojektion:").Change("1");
        Eingabe(cut, "Projektlaufzeit:").Input("15");
        Eingabe(cut, "Ersatzkosten:").Input("12000");
        Eingabe(cut, "Ersatzintervall:").Input("8");
        Eingabe(cut, "Restwert der Einheit:").Input("2500");

        Assert.True(gemeldet!.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen);
        Assert.Equal(15, gemeldet.Wirtschaftlichkeit.ProjektjahreBeiWiederholung);
        Assert.Equal(12000, gemeldet.Einheiten[0].ErsatzkostenEuro);
        Assert.Equal(8, gemeldet.Einheiten[0].ErsatzintervallJahre);
        Assert.Equal(2500, gemeldet.Einheiten[0].RestwertEuro);
    }

    [Fact]
    public void Eigene_Kosten_oeffnen_den_Einheiten_Override()
    {
        var cut = Render<SpeicherFlottenEditor>(p => p.Add(x => x.Wert, KonfigurationMitEinheit()));

        Assert.DoesNotContain("Investition Kapazität", cut.Markup);
        Schalter(cut, "Eigene Kosten für diese Einheit verwenden").Change(true);

        Assert.Contains("Investition Kapazität", cut.Markup);
        Assert.Contains("Betriebskosten Leistung", cut.Markup);
    }

    private static FlottenStudieKonfiguration KonfigurationMitEinheit()
    {
        var einheit = new FlottenEinheit
        {
            Id = "s1", Name = "Hauptspeicher", KapazitaetKWh = 100,
            LadeleistungKw = 40, EntladeleistungKw = 50,
            Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.96,
            SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
        };
        return new FlottenStudieKonfiguration
        {
            Einheiten = new() { einheit },
            Auslegung = new FlottenAuslegungEingang
            {
                Achsen = new()
                {
                    new FlottenAuslegungsAchse
                    {
                        Aktiv = true, Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                        AnzahlVon = 1, AnzahlBis = 1,
                        KapazitaetVonKWh = 100, KapazitaetBisKWh = 300, KapazitaetSchrittKWh = 50,
                        LeistungVonKw = 40, LeistungBisKw = 80, LeistungSchrittKw = 20,
                        Vorlage = einheit
                    }
                }
            }
        };
    }

    private static AngleSharp.Dom.IElement Eingabe(IRenderedComponent<SpeicherFlottenEditor> cut, string label) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("input")!;

    private static AngleSharp.Dom.IElement Kategorie(IRenderedComponent<SpeicherFlottenEditor> cut, string bezeichnung) =>
        cut.FindAll("section.epos-flotte-auslegungskategorie")
            .Single(x => x.GetAttribute("aria-label") == bezeichnung);

    private static AngleSharp.Dom.IElement KategorieEingabe(
        IRenderedComponent<SpeicherFlottenEditor> cut, string kategorie, string label) =>
        Kategorie(cut, kategorie).QuerySelectorAll("label")
            .Single(x => x.TextContent.Contains(label)).QuerySelector("input")!;

    private static AngleSharp.Dom.IElement Auswahl(IRenderedComponent<SpeicherFlottenEditor> cut, string label) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("select")!;

    private static AngleSharp.Dom.IElement Schalter(IRenderedComponent<SpeicherFlottenEditor> cut, string label) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("input")!;
}
