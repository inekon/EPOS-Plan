using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>„Als Mehrzonenmodell übernehmen…“</b> (Welle GD-4; Entwurf Gebäudedialog 2.4, E130 F7) und der <b>Sprung Kühlung</b>
/// (Konzept Kältebedarf 3.5): der Knopf im Projektgebäude, weich gesperrt ohne gespeicherte Quelle; die benannte Ablehnung
/// der Prüfsumme; der Zonenbaum des Imports als Blatt (der Importdialog ohne eigenen Titel); Abbrechen merkt nichts vor und
/// schreibt nichts; erst das OK des Gebäudes schreibt das vorgemerkte Modell — statt des Zonenabgleichs. Dazu der Sprung:
/// Reiter „Gebäude und Hülle“ mit der Gruppe „Kühlung“, ohne Projektgebäude der Hinweis „Noch kein Gebäude im Projekt“.
/// Kultur de-DE (deutsche Beschriftungen).
/// </summary>
public class GebaeudeMehrzonenDialogTests : EposBunitContext
{
    private const string KNOPF = "Als Mehrzonenmodell übernehmen…";

    public GebaeudeMehrzonenDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static GebaeudeKatalogDaten Satz() => new()
    {
        Name = "Haus A", Typ = "Einfamilienhaus", Beschreibung = "Beschreibung Haus A", Gebaeudeart = "Hotel",
        Verwendung = "Wohngebaeude", Baualtersklasse = 4, Bauart = 1, WohnflaecheGesamt = 150, FlaecheNutzer = 35,
        Waermegewinne = 400, Fensterdurchlassgrad = 0.4, Raumhoehe = 2.5, FensterflaecheNord = 10, FensterflaecheSued = 20,
        FensterflaecheOstWest = 15, FlaecheAussenwand = 200, Dachflaeche = 120, Grundflaeche = 100, SonstigeFlaechen = 5,
        UWertAussenwand = 0.3, UWertFenster = 1.3, UWertDachflaeche = 0.2, UWertGrundflaeche = 0.35, UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1, AnschlussFensterWand = 50, SollTag = 20, NachtAbsenkung = 17, MaxTemperatur = 24,
        WochenendAbsenkung = 0, SollFerien = 0, Luftwechselrate = 0.5
    };

    /// <summary>Die Gegenseite: eine Hülle, die vormerkt, wenn der Importdialog übernimmt, und Schreibaufrufe zählt.</summary>
    private sealed class Weg
    {
        internal bool Vorgemerkt;
        internal int Geschrieben;
        internal int Zonengeschrieben;
        internal string? Startmeldung;

        internal GebaeudeMehrzonenweg Mehrzonen(string? sperre = null) => new()
        {
            Sperre = sperre,
            Quelle = "haus.sqproj (Projektdatei)",
            Oeffnen = () => Task.FromResult<GebaeudeMehrzonenstart?>(Startmeldung is not null
                ? new GebaeudeMehrzonenstart(Startmeldung, null, "haus.sqproj")
                : new GebaeudeMehrzonenstart(null, new Dictionary<string, object>
                {
                    ["Uebernehmen"] = new Func<GebaeudeImportErgebnis, Task<string?>>(_ => { Vorgemerkt = true; return Task.FromResult<string?>(null); })
                }, "haus.sqproj")),
            Ausstehend = () => Vorgemerkt ? "Mehrzonenmodell steht bereit" : null,
            Schreiben = () => { if (Vorgemerkt) { Geschrieben++; Vorgemerkt = false; } return null; }
        };

        internal GebaeudeZonenweg Zonenweg(string? sperre = null) => new()
        {
            Mehrzonen = Mehrzonen(sperre),
            Speichern = _ => { Zonengeschrieben++; return ZonenSchreibergebnis.Gut; }
        };
    }

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(Weg weg, GebaeudeZonenweg? zonen = null,
                                                                GebaeudeKatalogModus modus = GebaeudeKatalogModus.Projekt,
                                                                string sprungziel = "")
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, Satz())
            .Add(x => x.Modus, modus)
            .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
            .Add(x => x.Gebaeudearten, () => new[] { "Hotel" })
            .Add(x => x.Baualtersklassen, new[] { "a", "b", "c", "d", "e" })
            .Add(x => x.Speichern, (_, _, _) => new GebaeudeKatalogErgebnis(true, ""))
            .Add(x => x.Zonen, zonen ?? weg.Zonenweg())
            .Add(x => x.StartReiter, GebaeudeKatalogDialog.REITER_ZONEN)
            .Add(x => x.Sprungziel, sprungziel));

    private static IElement Knopf(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll("button.epos-mehrzonen-knopf").First();

    [Fact]
    public void Ohne_gespeicherte_Quelle_ist_der_Knopf_weich_gesperrt_und_nennt_den_Grund()
    {
        var weg = new Weg();
        const string grund = "Kein Import gespeichert — Gebäude über „Import…“ anlegen.";
        var cut = Aufbauen(weg, weg.Zonenweg(grund));

        IElement knopf = Knopf(cut);
        Assert.Equal(KNOPF, knopf.TextContent.Trim());
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(grund, knopf.GetAttribute("title"));
        knopf.Click();
        Assert.False(cut.Instance.MehrzonenblattOffen);
        Assert.Contains(grund, cut.Markup);

        // Ohne Weg (keine Hülle) und am Katalogsatz ebenso gesperrt, je mit Grund.
        Assert.Equal(grund, Aufbauen(new Weg(), new GebaeudeZonenweg()).Instance.MehrzonenSperre);
        Assert.StartsWith("Nur am Gebäude im Projekt", Aufbauen(new Weg(), modus: GebaeudeKatalogModus.Bearbeiten).Instance.MehrzonenSperre);
    }

    [Fact]
    public void Eine_abweichende_Pruefsumme_steht_als_Meldung_ohne_Blatt()
    {
        var weg = new Weg { Startmeldung = "Die Datei „haus.sqproj“ weicht von der gespeicherten Importquelle ab." };
        var cut = Aufbauen(weg);
        Assert.Null(cut.Instance.MehrzonenSperre);
        Knopf(cut).Click();
        cut.WaitForAssertion(() => Assert.Contains("weicht von der gespeicherten Importquelle ab", cut.Markup));
        Assert.False(cut.Instance.MehrzonenblattOffen);
    }

    [Fact]
    public void Der_Zonenbaum_steht_als_Blatt_und_Abbrechen_merkt_nichts_vor_und_schreibt_nichts()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg);
        Knopf(cut).Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.MehrzonenblattOffen));

        // Das Blatt trägt den Kopf; der Importdialog darin zeigt weder Titel noch Kreuz, der Editorinhalt ist getauscht.
        Assert.Contains("Mehrzonenmodell aus der Importquelle", cut.Markup);
        IElement import = cut.Find(".epos-gebimport");
        Assert.Empty(import.QuerySelectorAll("h1.epos-dialog-titel"));
        Assert.Empty(cut.FindAll("button.epos-mehrzonen-knopf"));

        cut.FindAll(".epos-gebimport .epos-leiste button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        cut.WaitForAssertion(() => Assert.False(cut.Instance.MehrzonenblattOffen));
        Assert.False(weg.Vorgemerkt);
        Assert.Null(cut.Instance.MehrzonenAusstehend);
        Assert.Equal(0, weg.Geschrieben);
    }

    [Fact]
    public void Das_vorgemerkte_Modell_schreibt_erst_das_OK_des_Gebaeudes_statt_des_Zonenabgleichs()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg);
        weg.Vorgemerkt = true;   // so steht es nach dem OK des Blatts (die Hülle hat vorgemerkt)
        cut.Render();
        Assert.Contains("Mehrzonenmodell steht bereit", cut.Find(".epos-mehrzonen-ausstehend").TextContent);
        Assert.Equal(0, weg.Geschrieben);

        cut.FindAll(".epos-leiste button.epos-knopf--primaer").First(b => b.TextContent.Trim() == "OK").Click();
        Assert.Equal(1, weg.Geschrieben);
        Assert.Equal(0, weg.Zonengeschrieben);
    }

    [Fact]
    public void Sprung_Kuehlung_oeffnet_den_Reiter_Huelle_mit_der_Gruppe_Kuehlung()
    {
        var cut = Aufbauen(new Weg(), sprungziel: Sprungziele.Kuehlung);
        Assert.Equal("HUELLE", cut.Instance.AktiverReiter);
        Assert.NotNull(cut.Find("[data-sprungziel='KUEHLUNG']"));
        Assert.False(cut.Instance.SprungAusstehend);
    }

    [Fact]
    public void Sprung_Kuehlung_ohne_Projektgebaeude_nennt_den_Grund_und_mit_Gebaeude_oeffnet_den_Editor()
    {
        var ohne = Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile>())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Sprungziel, Sprungziele.Kuehlung));
        Assert.Contains("Noch kein Gebäude im Projekt.", ohne.Markup);

        var zeile = new GebaeudeProjektZeile { IdZ = 1, Name = "Haus A", HatProjektkopie = true };
        var mit = Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile> { zeile })
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.ProjektGaben, _ => new Dictionary<string, object>
            {
                ["Daten"] = Satz(), ["Modus"] = GebaeudeKatalogModus.Projekt
            })
            .Add(x => x.Sprungziel, Sprungziele.Kuehlung));
        Assert.DoesNotContain("Noch kein Gebäude im Projekt.", mit.Markup);
        var editor = mit.FindComponent<GebaeudeKatalogDialog>();
        Assert.Equal("HUELLE", editor.Instance.AktiverReiter);
        Assert.Equal(Sprungziele.Kuehlung, editor.Instance.Sprungziel);
    }
}
