using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.Kern.Tests;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Komponente „Profile aus einer Projektdatei"</b> (Konzept Nutzungsprofile Q46, Stufe NP4b): Knopf nur mit Weg,
/// Vorschau mit Zeilen und Hinweisen, Übernehmen erst auf Knopfdruck, bei vorhandener Kategorie die Wahl
/// „Ergänzen | Ersetzen" (weich gesperrt mit Grund, der Versuch wird gemeldet), Ablehnung, Abbruch der Dateiwahl,
/// Schließen; dazu die beiden Wirte — das Blatt „Nutzungsprofile" (liest nach der Übernahme neu) und der Gebäudeimport
/// (Knopf erst mit geladener Projektdatei). Werte der Probe: runde Phantasiewerte.
/// </summary>
public class ProjektdateiProfileTests : EposBunitContext
{
    private sealed class Probeweg
    {
        internal readonly List<string> Spur = new();
        internal ProjektdateiProfileVorschau? Vorschau;

        internal ProjektdateiProfileWeg Weg() => new ProjektdateiProfileWeg
        {
            Texte = new ProjektdateiProfileTexte { GrundKeine = "keine", GrundAlleDa = "alle da", HinweisZuordnung = "Zuordnung umstellen" },
            Laden = () =>
            {
                Spur.Add("Laden");
                return Task.FromResult(Vorschau);
            },
            Uebernehmen = ersetzen =>
            {
                Spur.Add("Uebernehmen:" + ersetzen);
                return new ProjektdateiProfileErgebnis(true, "2 neu", new[] { "Hinweis nach" });
            },
        };
    }

    private static ProjektdateiProfileVorschau Vorschau(bool vorhanden, bool alleDa = false)
        => new("Probe.sqproj", "Projektdatei Probe.sqproj", vorhanden,
               new[]
               {
                   new ProjektdateiProfilZeile("1", "Probe A", "20 °C", 2, alleDa),
                   new ProjektdateiProfilZeile("2", "Probe B", "", 1, alleDa || vorhanden),
               },
               new[] { "Hinweis eins" });

    private IRenderedComponent<ProjektdateiProfile> Bauen(Probeweg p, Action? uebernommen = null)
        => Render<ProjektdateiProfile>(c =>
        {
            c.Add(x => x.Weg, p.Weg());
            c.Add(x => x.Knopftext, "Aus Projektdatei…");
            if (uebernommen is not null) c.Add(x => x.Uebernommen, uebernommen);
        });

    private static IElement Knopf(IRenderedComponent<ProjektdateiProfile> cut, string klasse) => cut.Find("button." + klasse);

    [Fact]
    public void Ohne_Weg_kein_Knopf()
    {
        IRenderedComponent<ProjektdateiProfile> cut = Render<ProjektdateiProfile>();
        Assert.Empty(cut.FindAll("button"));
        cut = Render<ProjektdateiProfile>(c => c.Add(x => x.Weg, new ProjektdateiProfileWeg()));
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Neue_Kategorie_Vorschau_dann_Uebernehmen_und_Hinweis_zur_Zuordnung()
    {
        var p = new Probeweg { Vorschau = Vorschau(vorhanden: false) };
        int gemeldet = 0;
        IRenderedComponent<ProjektdateiProfile> cut = Bauen(p, () => gemeldet++);
        Assert.Equal("Aus Projektdatei…", Knopf(cut, "epos-projektdateiprofile-oeffnen").TextContent);
        Assert.Empty(cut.FindAll(".epos-projektdateiprofile-vorschau"));

        Knopf(cut, "epos-projektdateiprofile-oeffnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.VorschauOffen));
        Assert.Equal(new[] { "1", "2" }, cut.FindAll("tr.epos-projektdateiprofile-zeile").Select(z => z.GetAttribute("data-nummer")));
        Assert.Contains("Hinweis eins", cut.Find(".epos-projektdateiprofile-meldungen").TextContent);
        Assert.Contains("Projektdatei Probe.sqproj", cut.Find(".epos-projektdateiprofile-kopf").TextContent);
        Assert.Empty(cut.FindAll("button.epos-projektdateiprofile-ergaenzen"));
        Assert.Equal(new[] { "Laden" }, p.Spur);   // nichts geschrieben vor „Übernehmen"

        Knopf(cut, "epos-projektdateiprofile-uebernehmen").Click();
        Assert.Equal(new[] { "Laden", "Uebernehmen:False" }, p.Spur);
        Assert.Equal(1, gemeldet);
        Assert.Equal("2 neu", cut.Find(".epos-projektdateiprofile-ergebnis").TextContent);
        Assert.Equal("Zuordnung umstellen", cut.Find(".epos-projektdateiprofile-zuordnung").TextContent);
        Assert.Empty(cut.FindAll("button.epos-projektdateiprofile-uebernehmen"));

        Knopf(cut, "epos-projektdateiprofile-schliessen").Click();
        Assert.False(cut.Instance.VorschauOffen);
    }

    [Fact]
    public void Vorhandene_Kategorie_fragt_Ergaenzen_oder_Ersetzen()
    {
        var p = new Probeweg { Vorschau = Vorschau(vorhanden: true) };
        IRenderedComponent<ProjektdateiProfile> cut = Bauen(p);
        Knopf(cut, "epos-projektdateiprofile-oeffnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.VorschauOffen));
        Assert.NotNull(cut.Find(".epos-projektdateiprofile-frage"));
        Assert.Empty(cut.FindAll("button.epos-projektdateiprofile-uebernehmen"));
        Assert.Null(Knopf(cut, "epos-projektdateiprofile-ergaenzen").GetAttribute("aria-disabled"));
        Assert.Equal("1", cut.FindAll("tr.epos-projektdateiprofile-zeile")[1].GetAttribute("data-vorhanden"));

        Knopf(cut, "epos-projektdateiprofile-ersetzen").Click();
        Assert.Equal(new[] { "Laden", "Uebernehmen:True" }, p.Spur);
    }

    [Fact]
    public void Ergaenzen_ist_weich_gesperrt_wenn_alle_Nummern_da_sind_und_meldet_den_Versuch()
    {
        var p = new Probeweg { Vorschau = Vorschau(vorhanden: true, alleDa: true) };
        IRenderedComponent<ProjektdateiProfile> cut = Bauen(p);
        Knopf(cut, "epos-projektdateiprofile-oeffnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.VorschauOffen));
        IElement ergaenzen = Knopf(cut, "epos-projektdateiprofile-ergaenzen");
        Assert.Equal("true", ergaenzen.GetAttribute("aria-disabled"));
        Assert.Equal("alle da", ergaenzen.GetAttribute("title"));
        Assert.False(ergaenzen.HasAttribute("disabled"));

        ergaenzen.Click();
        Assert.Equal(new[] { "Laden" }, p.Spur);
        Assert.Equal("alle da", cut.Find(".epos-projektdateiprofile-sperrhinweis").TextContent);
        Assert.Null(Knopf(cut, "epos-projektdateiprofile-ersetzen").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Ablehnung_und_Abbruch()
    {
        var p = new Probeweg
        {
            Vorschau = new ProjektdateiProfileVorschau("x.sqproj", "Projektdatei x.sqproj", false, Array.Empty<ProjektdateiProfilZeile>(),
                                                       Array.Empty<string>(), "keine Projektdatei"),
        };
        IRenderedComponent<ProjektdateiProfile> cut = Bauen(p);
        Knopf(cut, "epos-projektdateiprofile-oeffnen").Click();
        cut.WaitForAssertion(() => Assert.Equal("keine Projektdatei", cut.Find(".epos-projektdateiprofile-ablehnung").TextContent));
        Assert.Empty(cut.FindAll("button.epos-projektdateiprofile-uebernehmen, button.epos-projektdateiprofile-ersetzen"));

        p.Vorschau = null;   // Dateiwahl abgebrochen: die Vorschau bleibt, wie sie war
        Knopf(cut, "epos-projektdateiprofile-oeffnen").Click();
        Assert.Equal(2, p.Spur.Count);
        Assert.True(cut.Instance.VorschauOffen);
    }

    [Fact]
    public void Ohne_Zeilen_ist_Uebernehmen_weich_gesperrt()
    {
        var p = new Probeweg
        {
            Vorschau = new ProjektdateiProfileVorschau("x.sqproj", "Projektdatei x.sqproj", false, Array.Empty<ProjektdateiProfilZeile>(),
                                                       new[] { "leer" }),
        };
        IRenderedComponent<ProjektdateiProfile> cut = Bauen(p);
        Knopf(cut, "epos-projektdateiprofile-oeffnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.VorschauOffen));
        IElement k = Knopf(cut, "epos-projektdateiprofile-uebernehmen");
        Assert.Equal("true", k.GetAttribute("aria-disabled"));
        k.Click();
        Assert.Equal(new[] { "Laden" }, p.Spur);
    }

    // =================================================================
    //  Wirt 1: das Blatt „Nutzungsprofile"
    // =================================================================

    [Fact]
    public void Das_Blatt_traegt_den_Kopfknopf_und_liest_nach_der_Uebernahme_neu()
    {
        var p = new Probeweg { Vorschau = Vorschau(vorhanden: false) };
        int kategorien = 0;
        var weg = new RaumnutzungWeg
        {
            Projektdatei = p.Weg(),
            Kategorien = () =>
            {
                kategorien++;
                return new[] { new RaumnutzungKategorieDaten(1, "Projektdatei Probe.sqproj", RaumnutzungArt.Eigen, false, "", "") };
            },
            Profile = _ => Array.Empty<RaumnutzungProfilDaten>(),
            Zuordnungen = () => Array.Empty<RaumnutzungZuordnungDaten>(),
        };
        int geaendert = 0;
        IRenderedComponent<RaumnutzungBlatt> cut = Render<RaumnutzungBlatt>(c =>
        {
            c.Add(x => x.Katalogweg, weg);
            c.Add(x => x.Geaendert, () => geaendert++);
        });
        IElement knopf = cut.Find("button.epos-projektdateiprofile-oeffnen");
        Assert.Equal(new ProjektdateiProfileTexte().KnopfBlatt, knopf.TextContent);
        int vorher = kategorien;

        knopf.Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("button.epos-projektdateiprofile-uebernehmen")));
        cut.Find("button.epos-projektdateiprofile-uebernehmen").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, geaendert));
        Assert.True(kategorien > vorher, "Das Blatt hat den Katalog nach der Übernahme nicht neu gelesen.");

        // Ohne Weg kein Knopf im Blatt.
        IRenderedComponent<RaumnutzungBlatt> ohne = Render<RaumnutzungBlatt>(c => c.Add(x => x.Katalogweg, new RaumnutzungWeg
        {
            Kategorien = () => Array.Empty<RaumnutzungKategorieDaten>(),
            Profile = _ => Array.Empty<RaumnutzungProfilDaten>(),
            Zuordnungen = () => Array.Empty<RaumnutzungZuordnungDaten>(),
        }));
        Assert.Empty(ohne.FindAll("button.epos-projektdateiprofile-oeffnen"));
    }

    // =================================================================
    //  Wirt 2: der Gebäudeimport
    // =================================================================

    [Fact]
    public void Der_Gebaeudeimport_zeigt_den_Knopf_erst_mit_geladener_Projektdatei()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        string ifc = SqprojProbenErzeuger.HottcadZonenhaus(Wurzel());
        string sq = SqprojProbenErzeuger.Zonenhaus().Schreiben(SqprojProbenErzeuger.TempPfad("np4b-dialog"));
        try
        {
            var p = new Probeweg { Vorschau = Vorschau(vorhanden: false) };
            var huelle = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> g = huelle.Gaben();
            Assert.False(g.ContainsKey("ProjektdateiProfile"));   // ohne Projekt keine Datenbank, kein Knopf
            IRenderedComponent<GebaeudeImportDialog> cut = Render<GebaeudeImportDialog>(c =>
            {
                c.Add(x => x.Profil, g["Profil"] as GebaeudeImportProfilDaten);
                c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)g["Baualtersklassen"]);
                c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                    Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl(ifc, Path.GetFileName(ifc), new FileInfo(ifc).Length))));
                c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)g["Lesen"]);
                c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)g["Zuordnen"]);
                c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)g["Pruefen"]);
                c.Add(x => x.ProjektdateiWaehlen, (Func<Task<string?>>)(() => Task.FromResult<string?>(sq)));
                c.Add(x => x.ProjektdateiLesen, (Func<string, int, CancellationToken, Task<GebaeudeProjektdateiDaten>>)g["ProjektdateiLesen"]);
                c.Add(x => x.ProjektdateiEntfernen, (Action)g["ProjektdateiEntfernen"]);
                c.Add(x => x.ProjektdateiProfile, p.Weg());
            });
            IElement Dateiknopf() => cut.FindAll("button").First(k => k.TextContent.Contains(cut.Instance.Texte.DateiKnopf.TrimEnd('…', '.')));
            Dateiknopf().Click();
            cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-zeilen tbody tr")));
            cut.WaitForAssertion(() => Assert.False(Dateiknopf().HasAttribute("disabled")));
            Assert.Empty(cut.FindAll("button.epos-projektdateiprofile-oeffnen"));

            cut.Find("button[data-aktion='projektdatei']").Click();
            cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("button.epos-projektdateiprofile-oeffnen")));
            IElement knopf = cut.Find("button.epos-projektdateiprofile-oeffnen");
            Assert.Equal(new ProjektdateiProfileTexte().KnopfImport, knopf.TextContent);
            knopf.Click();
            cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tr.epos-projektdateiprofile-zeile")));
            cut.Find("button.epos-projektdateiprofile-uebernehmen").Click();
            cut.WaitForAssertion(() => Assert.Contains("Uebernehmen:False", p.Spur));
            Assert.NotEmpty(cut.FindAll("tr.epos-gebimport-planzone"));   // der Plan steht nach dem Neuzuordnen weiter

            cut.Find("button[data-aktion='projektdatei-entfernen']").Click();
            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("button.epos-projektdateiprofile-oeffnen")));
        }
        finally
        {
            foreach (string d in new[] { ifc, sq })
                try { File.Delete(d); } catch (IOException) { }
        }
    }

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}
