using System.Text;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

public sealed class SpeicherFlottenFehlerpfadeTests : BunitContext
{
    [Fact]
    public void Berechnungsfehler_und_erfolgloses_Ergebnis_zeigen_den_tatsaechlichen_Text()
    {
        var ausnahme = RenderDialog((_, _) =>
            Task.FromException<SpeicherFlottenErgebnis>(new InvalidOperationException("SCIP-Laufzeit nicht verfügbar")));
        Start(ausnahme);
        Assert.Contains("SCIP-Laufzeit nicht verfügbar", ausnahme.Markup);
        Assert.DoesNotContain(">_fehler<", ausnahme.Markup);

        var fachfehler = RenderDialog((_, _) => Task.FromResult(new SpeicherFlottenErgebnis
        {
            Erfolg = false,
            Meldung = "Harte Netzbezugsgrenze wird verletzt"
        }));
        Start(fachfehler);
        Assert.Contains("Harte Netzbezugsgrenze wird verletzt", fachfehler.Markup);
    }

    [Fact]
    public void Einstellungen_und_Profil_zeigen_Rueckgabe_oder_Ausnahme()
    {
        var cut = Render<SpeicherFlottenDialog>(p => p
            .Add(x => x.Vorgaben, Vorgaben)
            .Add(x => x.EinstellungenSpeichern, _ => Task.FromResult("Datenbank ist schreibgeschützt"))
            .Add(x => x.ProfilSpeichern, (_, _) =>
                Task.FromException<SpeicherOptimierungVorgaben>(new InvalidOperationException("Profilname bereits vergeben"))));
        Datenreiter(cut);

        cut.FindAll("button").Single(x => x.TextContent.Contains("Einstellungen speichern")).Click();
        Assert.Contains("Datenbank ist schreibgeschützt", cut.Markup);

        cut.FindAll("label").Single(x => x.TextContent.Contains("Profilname"))
            .QuerySelector("input")!.Input("Bestand");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Profil speichern")).Click();
        Assert.Contains("Profilname bereits vergeben", cut.Markup);
    }

    [Fact]
    public void Erfolgreiches_Speichern_und_Profilspeichern_loescht_einen_alten_Berechnungsfehler()
    {
        const string alterFehler = "Projektlaufzeit: Jahresdaten fehlen";
        var cut = Render<SpeicherFlottenDialog>(p => p
            .Add(x => x.Vorgaben, Vorgaben)
            .Add(x => x.Rechnen, (_, _) => Task.FromResult(new SpeicherFlottenErgebnis
            {
                Erfolg = false,
                Meldung = alterFehler
            }))
            .Add(x => x.EinstellungenSpeichern, _ => Task.FromResult(""))
            .Add(x => x.ProfilSpeichern, (_, _) => Task.FromResult(Vorgaben())));

        Start(cut);
        Assert.Contains(alterFehler, cut.Markup);
        Datenreiter(cut);
        cut.FindAll("button").Single(x => x.TextContent.Contains("Einstellungen speichern")).Click();
        Assert.DoesNotContain(alterFehler, cut.Markup);
        Assert.Contains("Einstellungen gespeichert", cut.Markup);

        Start(cut);
        Assert.Contains(alterFehler, cut.Markup);
        cut.FindAll("label").Single(x => x.TextContent.Contains("Profilname"))
            .QuerySelector("input")!.Input("Bestand");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Profil speichern")).Click();
        Assert.DoesNotContain(alterFehler, cut.Markup);
        Assert.Contains("Profil gespeichert", cut.Markup);
    }

    [Fact]
    public void Erfolgreicher_CSV_Import_loescht_einen_alten_Berechnungsfehler()
    {
        const string alterFehler = "Bruttolast: Zeitreihe fehlt";
        var vorgaben = Vorgaben();
        vorgaben.Eingaben.Auslegung.Lastquelle = SpeicherAuslegungQuelle.Datei;
        byte[] csv = Encoding.UTF8.GetBytes("Zeit;Wert\n2026-01-01 00:00;1,5\n2026-01-01 00:15;2,0\n");
        var cut = Render<SpeicherFlottenDialog>(p => p
            .Add(x => x.Vorgaben, () => vorgaben)
            .Add(x => x.Rechnen, (_, _) => Task.FromResult(new SpeicherFlottenErgebnis
            {
                Erfolg = false,
                Meldung = alterFehler
            }))
            .Add(x => x.DateiWaehlen, () => Task.FromResult(new SpeicherImportDatei
            {
                Dateiname = "last.csv",
                Inhalt = csv
            })));

        Start(cut);
        Assert.Contains(alterFehler, cut.Markup);
        Datenreiter(cut);
        cut.FindAll("button").Single(x => x.TextContent.Contains("Lastdatei")).Click();
        cut.FindComponent<SpeicherZeitreihenDialog>().FindAll("button")
            .Single(x => x.TextContent.Contains("Übernehmen")).Click();

        Assert.DoesNotContain(alterFehler, cut.Markup);
        Assert.Contains("last.csv", cut.Markup);
    }

    [Fact]
    public void JSON_Datei_wird_im_neuen_Flotten_CSV_Dialog_abgewiesen()
    {
        byte[] json = Encoding.UTF8.GetBytes("[{\"Jahr\":2026,\"Istwerte\":[]},{\"Jahr\":2026,\"Istwerte\":[]}]");
        var cut = Render<SpeicherFlottenDialog>(p => p
            .Add(x => x.Vorgaben, Vorgaben)
            .Add(x => x.DateiWaehlen, () => Task.FromResult(new SpeicherImportDatei
            {
                Dateiname = "projektjahre.json",
                Inhalt = json
            })));

        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Prognosen & Jahresdaten").Click();
        cut.FindAll("button").Single(x => x.TextContent.Contains("Projektjahre-CSV importieren")).Click();

        Assert.Contains("JSON-Import wird nicht unterstützt", cut.Markup);
        Assert.Single(cut.FindComponents<SpeicherFlottenCsvDialog>());
    }

    [Fact]
    public void Fehlerhafte_CSV_Spalten_werden_im_Importdialog_angezeigt()
    {
        var vorgaben = Vorgaben();
        vorgaben.Eingaben.Auslegung.Lastquelle = SpeicherAuslegungQuelle.Datei;
        byte[] csv = Encoding.UTF8.GetBytes("Zeit\n2026-01-01 00:00\n");
        var cut = Render<SpeicherFlottenDialog>(p => p
            .Add(x => x.Vorgaben, () => vorgaben)
            .Add(x => x.DateiWaehlen, () => Task.FromResult(new SpeicherImportDatei
            {
                Dateiname = "last.csv",
                Inhalt = csv
            })));
        Datenreiter(cut);

        cut.FindAll("button").Single(x => x.TextContent.Contains("Lastdatei")).Click();
        cut.FindComponent<SpeicherZeitreihenDialog>().FindAll("button")
            .Single(x => x.TextContent.Contains("Übernehmen")).Click();

        Assert.Contains("Zeile 2 hat nur 1 Spalten; benoetigt wird Spalte 2", cut.Markup);
        Assert.Single(cut.FindComponents<SpeicherZeitreihenDialog>());
    }

    [Fact]
    public void Laufzeit_Ausgleich_SoC_und_Groessenbereich_werden_gemeinsam_vor_dem_Start_gezeigt()
    {
        var vorgaben = Vorgaben();
        vorgaben.Eingaben.Auslegung.Flotte!.Wirtschaftlichkeit.ProjektjahreBeiWiederholung = 0;
        var einheit = new FlottenEinheit
        {
            Id = "s1", Name = "Hauptspeicher", KapazitaetKWh = 100,
            LadeleistungKw = 50, EntladeleistungKw = 50,
            Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
            SocMin = 0.8, SocStart = 0.5, SocMax = 0.7
        };
        vorgaben.Eingaben.Auslegung.FlottenGroessenOptimieren = true;
        vorgaben.Eingaben.Auslegung.Flotte!.Einheiten.Add(einheit);
        vorgaben.Eingaben.Auslegung.Flotte.Auslegung.Achsen.Add(new FlottenAuslegungsAchse
        {
            Aktiv = true,
            Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
            AnzahlVon = 2,
            AnzahlBis = 1,
            KapazitaetVonKWh = 0,
            KapazitaetBisKWh = 20,
            KapazitaetSchrittKWh = 5,
            LeistungVonKw = 10,
            LeistungBisKw = 20,
            LeistungSchrittKw = 5,
            Vorlage = einheit
        });

        var cut = Render<SpeicherFlottenDialog>(p => p
            .Add(x => x.Vorgaben, () => vorgaben)
            .Add(x => x.Rechnen, (_, _) => Task.FromResult(new SpeicherFlottenErgebnis())));

        Assert.Contains("Projektlaufzeit: Bitte mindestens 1 Jahr", cut.Markup);
        Assert.Contains("Energie-Ausgleichswert", cut.Markup);
        Assert.Contains("SoC-Minimum, Start-SoC und SoC-Maximum", cut.Markup);
        Assert.Contains("Größenbereich 'Hauptspeicher' – Anzahl", cut.Markup);
        Assert.Contains("Größenbereich 'Hauptspeicher' – Kapazität [kWh]", cut.Markup);
        Assert.True(Startknopf(cut).HasAttribute("disabled"));
    }

    private IRenderedComponent<SpeicherFlottenDialog> RenderDialog(
        Func<SpeicherOptimierungEingaben, Action<double?, string>, Task<SpeicherFlottenErgebnis>> rechnen) =>
        Render<SpeicherFlottenDialog>(p => p.Add(x => x.Vorgaben, Vorgaben).Add(x => x.Rechnen, rechnen));

    private static SpeicherOptimierungVorgaben Vorgaben() => new()
    {
        Eingaben = new SpeicherOptimierungEingaben
        {
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                Flotte = new FlottenStudieKonfiguration
                {
                    Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
                    {
                        ProjektjahreBeiWiederholung = 1
                    }
                }
            }
        }
    };

    private static void Start(IRenderedComponent<SpeicherFlottenDialog> cut) => Startknopf(cut).Click();

    private static AngleSharp.Dom.IElement Startknopf(IRenderedComponent<SpeicherFlottenDialog> cut) =>
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Speichervergleich berechnen");

    private static void Datenreiter(IRenderedComponent<SpeicherFlottenDialog> cut) =>
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Daten, Kosten & Profile").Click();
}
