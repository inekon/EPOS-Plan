using System.Text;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Seiten.Strom;
using EPOS.UI.Tests.Seiten.Strom;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

public sealed class SpeicherAuslegungEditorTests : EposBunitContext
{
    public SpeicherAuslegungEditorTests() => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    [Fact]
    public void Die_Groessenachse_wechselt_auf_Leistung_mit_eigenem_Bereich()
    {
        SpeicherOptimierungEingaben? gemeldet = null;
        var cut = Render<SpeicherAuslegungEditor>(p => p
            .Add(x => x.Wert, new SpeicherOptimierungEingaben())
            .Add(x => x.WertChanged, x => gemeldet = x));

        cut.Find("select").Change("1");

        Assert.NotNull(gemeldet);
        Assert.Equal(OptimiererGroessenachse.LeistungKw, gemeldet!.Groessenachse);
        Assert.Contains("kW", cut.Markup);
        Assert.DoesNotContain("kWh\" value=\"500", cut.Markup);
    }

    [Fact]
    public void Ein_Profil_wird_als_unabhaengige_Kopie_geladen()
    {
        SpeicherOptimierungEingaben? gemeldet = null;
        var original = new SpeicherOptimierungEingaben { PMinKw = 77, Auslegung = new SpeicherAuslegungKonfiguration() };
        var cut = Render<SpeicherAuslegungEditor>(p => p
            .Add(x => x.Wert, new SpeicherOptimierungEingaben())
            .Add(x => x.Auslegungsprofile, new[] { new SpeicherAuslegungProfil { Name = "Industrie", Eingaben = original } })
            .Add(x => x.WertChanged, x => gemeldet = x));

        cut.FindAll("select").Single(x => x.TextContent.Contains("Industrie")).Change("0");

        Assert.Equal(77, gemeldet!.PMinKw);
        Assert.NotSame(original, gemeldet);
        Assert.NotSame(original.Auslegung, gemeldet.Auslegung);
    }

    [Fact]
    public void Fehler_des_Speicherwegs_bleibt_sichtbar()
    {
        var cut = Render<SpeicherAuslegungEditor>(p => p
            .Add(x => x.Wert, new SpeicherOptimierungEingaben())
            .Add(x => x.EinstellungenSpeichern, _ => Task.FromResult("Schreibfehler")));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Einstellungen speichern")).Click();

        Assert.Contains("Schreibfehler", cut.Markup);
    }

    [Fact]
    public void Csv_und_Preisprofil_verlangen_die_ausdrueckliche_Modelljahrzuordnung()
    {
        var eingaben = new SpeicherOptimierungEingaben
        {
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                Lastquelle = SpeicherAuslegungQuelle.Datei,
                PvQuelle = SpeicherAuslegungQuelle.Datei,
                Preisquelle = SpeicherAuslegungQuelle.Preisprofil
            }
        };

        var cut = Render<SpeicherAuslegungEditor>(p => p.Add(x => x.Wert, eingaben));

        Assert.Contains("EPOS-Modelljahr auf die CSV-Zeitachse zuordnen", cut.Markup);
        Assert.Contains("29. Februar", cut.Markup);
        Assert.Contains("CSV-Zeitachse bleibt vollständig erhalten", cut.Markup);
    }

    [Fact]
    public void Manuelle_Kosteneingaben_markieren_die_beiden_Kostengruppen_als_vorhanden()
    {
        SpeicherOptimierungEingaben? gemeldet = null;
        var cut = Render<SpeicherAuslegungEditor>(p => p
            .Add(x => x.Wert, new SpeicherOptimierungEingaben())
            .Add(x => x.WertChanged, x => gemeldet = x));

        cut.FindAll("label").Single(x => x.TextContent.Contains("Investition Leistung"))
            .QuerySelector("input")!.Input("125");
        cut.FindAll("label").Single(x => x.TextContent.Contains("Betrieb Leistung"))
            .QuerySelector("input")!.Input("3,5");

        Assert.True(gemeldet!.Auslegung.DirekteKosten.InvestVorhanden);
        Assert.True(gemeldet.Auslegung.DirekteKosten.BetriebVorhanden);
        Assert.Equal(125, gemeldet.Auslegung.DirekteKosten.InvestEurProKw);
        Assert.Equal(3.5, gemeldet.Auslegung.DirekteKosten.BetriebEurProKwJahr);
    }

    [Fact]
    public void Null_ist_fuer_positive_Achsenwerte_eine_sichtbare_Fehleingabe()
    {
        var cut = Render<SpeicherAuslegungEditor>(p => p
            .Add(x => x.Wert, new SpeicherOptimierungEingaben()));

        cut.FindAll("label").Single(x => x.TextContent.Contains("Von:"))
            .QuerySelector("input")!.Input("0");

        Assert.NotNull(cut.Find("input[aria-invalid='true']"));
    }

    [Fact]
    public void Eine_Ausnahme_des_Speicherwegs_bleibt_im_Dialog_sichtbar()
    {
        var cut = Render<SpeicherAuslegungEditor>(p => p
            .Add(x => x.Wert, new SpeicherOptimierungEingaben())
            .Add(x => x.EinstellungenSpeichern, _ => throw new InvalidOperationException("Datei gesperrt")));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Einstellungen speichern")).Click();

        Assert.Contains("Datei gesperrt", cut.Markup);
    }

    [Fact]
    public void Ein_neu_gespeichertes_Profil_bleibt_nach_dem_Eltern_Rerender_waehlbar()
    {
        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste
            {
                Vorgaben = () => new SpeicherOptimierungVorgaben
                {
                    Eingaben = new SpeicherOptimierungEingaben()
                },
                ProfilSpeichern = (eingaben, name) => Task.FromResult(
                    new SpeicherOptimierungVorgaben
                    {
                        Eingaben = eingaben,
                        Auslegungsprofile = new[]
                        {
                            new SpeicherAuslegungProfil { Name = name, Eingaben = eingaben.Kopie() }
                        }
                    })
            }));

        // Der Auslegungseditor steht auf Blatt 2 der Ablaufleiste.
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Daten);

        cut.FindAll("label").Single(x => x.TextContent.Contains("Profilname"))
            .QuerySelector("input")!.Input("Neue Auslegung");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Profil speichern")).Click();

        Assert.Contains(cut.FindAll("select option"),
            x => x.TextContent.Trim() == "Neue Auslegung");
    }

    [Fact]
    public void Csv_Dialog_zeigt_die_begrenzt_zerlegte_Vorschau()
    {
        byte[] csv = Encoding.UTF8.GetBytes("Zeit;Wert\n2026-01-01 00:00;1,5\n2026-01-01 00:15;2,0\n");
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, new SpeicherImportDatei { Dateiname = "last.csv", Inhalt = csv })
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last));

        Assert.Contains("last.csv", cut.Markup);
        Assert.Contains("2026-01-01 00:15", cut.Markup);
        Assert.False(cut.FindAll("button").Single(x => x.TextContent.Contains("Übernehmen")).HasAttribute("disabled"));
    }

    [Fact]
    public void Eine_Aenderung_markiert_das_Ergebnis_als_veraltet_und_sperrt_die_Uebernahme()
    {
        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste
            {
                Vorgaben = () => new SpeicherOptimierungVorgaben { Eingaben = new SpeicherOptimierungEingaben() },
                EinstellungenSpeichern = _ => Task.FromResult(""),
                EinzelRechnen = (_, _) => Task.FromResult(new SpeicherOptimierungErgebnis { Erfolg = true }),
                AuslegungUebernehmen = (_, _) => new EPOS.UI.Seiten.Simulation.Rueckmeldung(true, "")
            }));

        Auslegungshilfe.Modus(cut, AuslegungModus.Einzelspeicher);
        Auslegungshilfe.Rechenknopf(cut).Click();

        var uebernehmen = cut.FindAll("button").Single(x => x.TextContent.Contains("Bestpunkt übernehmen"));
        Assert.False(uebernehmen.HasAttribute("disabled"));

        // Eine Aenderung auf Blatt 1 entwertet das Ergebnis - dieselbe Aussage wie im
        // abgeloesten Dialog, nur ueber die Ablaufleiste hinweg.
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Speicher);
        cut.FindAll("input[type=text]")[1].Input("4000");
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Ergebnis);

        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_MSG_ERGEBNIS_VERALTET, cut.Markup);
        Assert.True(cut.FindAll("button").Single(x => x.TextContent.Contains("Bestpunkt übernehmen")).HasAttribute("disabled"));
    }
}
