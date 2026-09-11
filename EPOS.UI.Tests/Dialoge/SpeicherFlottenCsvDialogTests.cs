using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using EPOS.UI.Dienste;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Seiten.Strom;
using EPOS.UI.Tests.Seiten.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

public sealed class SpeicherFlottenCsvDialogTests : EposBunitContext
{
    /// <summary>
    /// Die Ansicht traegt einen <c>InfoKnopf</c>; ohne Hilfedienst wirft der
    /// Blazor-Verteiler schon beim ersten Zeichnen.
    /// </summary>
    public SpeicherFlottenCsvDialogTests() => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    [Fact]
    public void Kopfzeile_belegt_Zeit_Werte_und_Prognosemetadaten_vor()
    {
        var cut = Render<SpeicherFlottenCsvDialog>(p => p
            .Add(x => x.Datei, PrognoseCsv())
            .Add(x => x.Prognosen, true));

        Assert.Equal("0", AusgewaehlterWert(cut, "Zeitstempelspalte:"));
        Assert.Equal("1", AusgewaehlterWert(cut, "Bruttolast:"));
        Assert.Equal("2", AusgewaehlterWert(cut, "PV-Leistung"));
        Assert.Equal("4", AusgewaehlterWert(cut, "Bezugspreis:"));
        Assert.Equal("8", AusgewaehlterWert(cut, "Snapshot-ID:"));
        Assert.Equal("9", AusgewaehlterWert(cut, "Bekannt seit:"));
        Assert.Equal("10", AusgewaehlterWert(cut, "Entscheidungszeitpunkt:"));
        Assert.Equal(11, cut.Find("table.epos-tabelle thead").QuerySelectorAll("th").Length);
    }

    [Fact]
    public void Prognose_CSV_uebergibt_gewaehlte_Einheiten_und_Optionen()
    {
        List<FlottenPrognoseSnapshot>? prognosen = null;
        SpeicherFlottenCsvOptionen? optionen = null;
        var cut = Render<SpeicherFlottenCsvDialog>(p => p
            .Add(x => x.Datei, PrognoseCsv())
            .Add(x => x.Prognosen, true)
            .Add(x => x.PrognosenUebernommen, (List<FlottenPrognoseSnapshot> p, SpeicherFlottenCsvOptionen o) =>
            {
                prognosen = p;
                optionen = o;
                return Task.CompletedTask;
            }));

        Auswahl(cut, "Einheit der Leistungsspalten:").Change("1");
        Auswahl(cut, "Einheit der Preisspalten:").Change("4");
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "CSV übernehmen").Click();

        FlottenPrognoseSnapshot prognose = Assert.Single(prognosen!);
        Assert.Equal(SpeicherZeitreihenEinheit.Megawatt, optionen!.LeistungEinheit);
        Assert.Equal(SpeicherZeitreihenEinheit.CentJeKilowattstunde, optionen.PreisEinheit);
        Assert.Equal(1000, prognose.Intervalle[0].LastKw);
        Assert.Equal(0.002, prognose.Intervalle[0].BezugspreisEuroProKWh, 10);
    }

    [Fact]
    public void Flottendialog_uebernimmt_Dateiname_Werte_und_CSV_Optionen_in_den_Profilstand()
    {
        SpeicherOptimierungEingaben? gespeichert = null;
        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Flotte = new FlottenStudieKonfiguration
                    {
                        Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang { ProjektjahreBeiWiederholung = 1 }
                    }
                }
            }
        };
        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste
            {
                Vorgaben = () => vorgaben,
                DateiWaehlen = () => Task.FromResult(PrognoseCsv()),
                EinstellungenSpeichern = e => { gespeichert = e; return Task.FromResult(""); }
            }));

        // Die Prognosentabelle ist seit Paket P3 eine UEBERLAGERUNG des Blattes
        // „Daten & Kosten" und kein vierter Reiter mehr.
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Daten);
        cut.FindAll("button").Single(x => x.TextContent.Trim()
            == WindowsFormsApplication1.MyResource.Resource.FLOTTE_SEITE_BTN_PROGNOSEN).Click();
        cut.FindAll("button").Single(x => x.TextContent.Contains("Prognose-CSV importieren")).Click();
        Auswahl(cut.FindComponent<SpeicherFlottenCsvDialog>(), "Einheit der Preisspalten:").Change("4");
        cut.FindComponent<SpeicherFlottenCsvDialog>().FindAll("button")
            .Single(x => x.TextContent.Trim() == "CSV übernehmen").Click();
        cut.FindAll("button").Single(x => x.TextContent.Contains("Einstellungen speichern")).Click();

        Assert.NotNull(gespeichert);
        Assert.Equal("prognose.csv", gespeichert!.Auslegung.FlottenPrognoseDatei);
        Assert.Single(gespeichert.Auslegung.FlottenPrognosen);
        Assert.Equal(SpeicherZeitreihenEinheit.CentJeKilowattstunde,
            gespeichert.Auslegung.FlottenPrognoseCsvOptionen!.PreisEinheit);
    }

    private static SpeicherImportDatei PrognoseCsv() => new()
    {
        Dateiname = "prognose.csv",
        Inhalt = Encoding.UTF8.GetBytes(
            "timestamp;load_kw;pv_kw;bhkw_kw;buy_eur_kwh;pv_sell_eur_kwh;bhkw_sell_eur_kwh;battery_sell_eur_kwh;snapshot_id;known_at;decision_at\n" +
            "2026-01-01 00:00;1;2;0;0,2;0,08;0,05;0,04;p1;2026-01-01 00:00;2026-01-01 00:00\n" +
            "2026-01-01 00:15;1,5;1;0;0,3;0,08;0,05;0,04;p1;2026-01-01 00:00;2026-01-01 00:00\n")
    };

    private static AngleSharp.Dom.IElement Auswahl(IRenderedComponent<SpeicherFlottenCsvDialog> cut, string label) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("select")!;

    private static string? AusgewaehlterWert(IRenderedComponent<SpeicherFlottenCsvDialog> cut, string label) =>
        Auswahl(cut, label).QuerySelector("option[selected]")?.GetAttribute("value");
}
