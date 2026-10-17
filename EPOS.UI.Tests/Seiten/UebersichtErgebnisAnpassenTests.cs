using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// „Anzeige anpassen…“ im Abschnitt „Ergebnisse — Energie“ (<see cref="UebersichtErgebnisse"/>,
/// <see cref="ErgebnisAnpassenDialog"/>) mit der echten Hülle <see cref="UebersichtErgebnisGaben"/>, synthetischen
/// Ergebnissen und einer Attrappe der Einstellungsnaht (<see cref="FluechtigeEinstellungen"/>).
/// </summary>
public class UebersichtErgebnisAnpassenTests : EposBunitContext
{
    public UebersichtErgebnisAnpassenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private readonly FluechtigeEinstellungen _ablage = new();

    private static VariantenDaten Daten(int id, bool stamm) => new()
    {
        IdProjekt = id, IstStamm = stamm, CO2Gesamt = id == 1031 ? 22 : 20,
        EnergiekostenJeTraeger = new List<EnergieTraegerNachweis>
        {
            new() { CarrierId = 1, Traeger = "Erdgas", MengeMWh = 30, WaermeMengeMWh = 30 },
            new() { CarrierId = 2, Traeger = "Strom", Netzstrom = true, MengeMWh = 50, Einheit = "kWh" }
        },
        Ergebnis = new ErgebnisModel
        {
            Energiebedarf = new ErgebnisEnergiebedarfModel
            {
                Waermebedarf_Gesamt = id == 1031 ? 110 : 100, Strombedarf_Gesamt = 50, Stromrestbedarf = 50, Waermerestbedarf = 0
            },
            Waermepumpe = new ErgebnisWaermepumpeModel { Stromverbrauch_Kuehlung = 3 }
        }
    };

    private static UebersichtStand Stand() => new()
    {
        StammId = 1030,
        Zeilen = new[]
        {
            new VarianteZeile { IdProjekt = 1030, IstStamm = true, Bezeichner = "(Stammprojekt)", SimZeitpunkt = "02.09.26 10:00" },
            new VarianteZeile { IdProjekt = 1031, Bezeichner = "WP klein", SimZeitpunkt = "02.09.26 10:00" }
        },
        MarkierteId = 1030,
        GewaehlteVarianten = new[] { 1030, 1031 }
    };

    /// <summary>Die Hülle über der Ablage der Klasse; die Ansicht steht auf der Tabelle, denn „Anzeige anpassen…“ wirkt auf sie.</summary>
    private UebersichtErgebnisGaben Huelle()
    {
        if (_ablage.Lies(UebersichtErgebnisGaben.EINSTELLUNG_ANSICHT) is null)
            _ablage.Schreib(UebersichtErgebnisGaben.EINSTELLUNG_ANSICHT, UebersichtErgebnisse.ANSICHT_TABELLE);
        return new(Daten, _ablage);
    }

    /// <summary>Der Abschnitt mit dem vollen Parametersatz der Hülle (wie die Seite ihn splattet).</summary>
    private IRenderedComponent<UebersichtErgebnisse> Zeige(UebersichtErgebnisGaben? huelle = null)
    {
        IReadOnlyDictionary<string, object> satz = (huelle ?? Huelle()).Gaben();
        UebersichtStand stand = Stand();
        var wurzel = Render(b =>
        {
            b.OpenComponent<UebersichtErgebnisse>(0);
            b.AddMultipleAttributes(1, satz);
            b.AddAttribute(2, nameof(UebersichtErgebnisse.Stand), stand);
            b.CloseComponent();
        });
        return wurzel.FindComponent<UebersichtErgebnisse>();
    }

    private static IElement Gruppe(IRenderedComponent<UebersichtErgebnisse> cut, string schluessel)
        => cut.Find($".epos-ergebnis-anpassen-gruppe[data-gruppe='{schluessel}']");

    private static void Zustand(IRenderedComponent<UebersichtErgebnisse> cut, string gruppe, ErgebnisGruppenZustand z)
        => Gruppe(cut, gruppe).QuerySelectorAll("input[type=radio]")[(int)z].Change(true);

    private static void Ok(IRenderedComponent<UebersichtErgebnisse> cut)
        => cut.Find(".epos-ergebnis-anpassen .epos-knopf--primaer").Click();

    private static string[] Gruppentitel(IRenderedComponent<UebersichtErgebnisse> cut)
        => cut.FindAll("tr.epos-ergebnis-gruppe").Select(t => t.GetAttribute("data-gruppe")!).ToArray();

    [Fact]
    public void Knopf_oeffnet_den_Dialog_mit_allen_Gruppen()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-ergebnis-anpassen"));
        IElement knopf = cut.Find(".epos-ergebnis-kopf .epos-ergebnis-anpassen-knopf");
        Assert.Equal("Anzeige anpassen…", knopf.TextContent);
        knopf.Click();

        Assert.True(cut.Instance.AnpassenSteht);
        Assert.Equal("Anzeige anpassen", cut.Find(".epos-ueberlagerung-titel").TextContent);
        string[] gruppen = cut.FindAll(".epos-ergebnis-anpassen-gruppe").Select(g => g.GetAttribute("data-gruppe")!).ToArray();
        Assert.Equal(Energievergleich.GRUPPEN, gruppen);
        // Drei Zustände je Gruppe, die Vorgabe des Kerns gewählt.
        var radios = Gruppe(cut, Energievergleich.GRUPPE_KAELTE).QuerySelectorAll("input[type=radio]");
        Assert.Equal(3, radios.Length);
        Assert.True(radios[(int)ErgebnisGruppenZustand.Zugeklappt].HasAttribute("checked"));
        // Die Kennzahlenliste kennt die Trägerzeile der aktuellen Daten.
        Gruppe(cut, Energievergleich.GRUPPE_ENDENERGIE).QuerySelector(".epos-klappgruppe-knopf")!.Click();
        Assert.NotNull(Gruppe(cut, Energievergleich.GRUPPE_ENDENERGIE).QuerySelector("[data-kennzahl='endenergie:1']"));
        Assert.Equal("Zeilen ohne Wert ausblenden",
                     cut.Find(".epos-ergebnis-anpassen-ohnewert .epos-feld-text").TextContent);
    }

    [Fact]
    public void Zustaende_und_Haken_wirken_nach_OK_und_werden_gespeichert()
    {
        var cut = Zeige();
        Assert.Contains(Energievergleich.GRUPPE_BEDARF, Gruppentitel(cut));
        Assert.NotEmpty(cut.FindAll("tr[data-kennzahl='endenergie:1']"));

        cut.Find(".epos-ergebnis-anpassen-knopf").Click();
        Zustand(cut, Energievergleich.GRUPPE_BEDARF, ErgebnisGruppenZustand.Ausgeblendet);
        Zustand(cut, Energievergleich.GRUPPE_KAELTE, ErgebnisGruppenZustand.Offen);
        Gruppe(cut, Energievergleich.GRUPPE_ENDENERGIE).QuerySelector(".epos-klappgruppe-knopf")!.Click();
        cut.Find("[data-kennzahl='endenergie:1'] input[type=checkbox]").Change(false);
        Ok(cut);

        Assert.False(cut.Instance.AnpassenSteht);
        string[] tafel = Gruppentitel(cut);
        Assert.DoesNotContain(Energievergleich.GRUPPE_BEDARF, tafel);
        Assert.Contains(Energievergleich.GRUPPE_KAELTE, tafel);           // jetzt offen in der Tafel
        Assert.Empty(cut.FindAll("tr[data-kennzahl='endenergie:1']"));

        // Gespeichert über die Naht: nur die Abweichungen von der Vorgabe.
        Assert.Equal("bedarf=ausgeblendet;kaelte=offen", _ablage.Lies(UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN));
        Assert.Equal("endenergie:1", _ablage.Lies(UebersichtErgebnisAnzeige.SCHLUESSEL_KENNZAHLEN_AUS));
        Assert.Equal(1, _ablage.LiesZahl(UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS, -1));

        // Eine neue Hülle (ein neues Fenster) liest denselben Grundzustand.
        var neu = Zeige(new UebersichtErgebnisGaben(Daten, _ablage));
        Assert.DoesNotContain(Energievergleich.GRUPPE_BEDARF, Gruppentitel(neu));
    }

    [Fact]
    public void Abbrechen_aendert_nichts()
    {
        var cut = Zeige();
        string[] vorher = Gruppentitel(cut);

        cut.Find(".epos-ergebnis-anpassen-knopf").Click();
        Zustand(cut, Energievergleich.GRUPPE_BEDARF, ErgebnisGruppenZustand.Ausgeblendet);
        cut.Find(".epos-ergebnis-anpassen-ohnewert input[type=checkbox]").Change(false);
        cut.FindAll(".epos-ergebnis-anpassen .epos-leiste .epos-knopf").Single(k => k.TextContent == "Abbrechen").Click();

        Assert.False(cut.Instance.AnpassenSteht);
        Assert.Equal(vorher, Gruppentitel(cut));
        Assert.Null(_ablage.Lies(UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN));

        // Esc wirkt wie Abbrechen; der nächste Aufruf beginnt beim gespeicherten Stand.
        cut.Find(".epos-ergebnis-anpassen-knopf").Click();
        var radios = Gruppe(cut, Energievergleich.GRUPPE_BEDARF).QuerySelectorAll("input[type=radio]");
        Assert.True(radios[(int)ErgebnisGruppenZustand.Offen].HasAttribute("checked"));
        cut.Find(".epos-ergebnis-anpassen").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.AnpassenSteht);
    }

    [Fact]
    public void Standard_wiederherstellen_setzt_die_Vorgabe()
    {
        _ablage.Schreib(UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN, "bedarf=ausgeblendet;emissionen=offen");
        _ablage.Schreib(UebersichtErgebnisAnzeige.SCHLUESSEL_KENNZAHLEN_AUS, "endenergie:1");
        _ablage.SchreibZahl(UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS, 0);
        var cut = Zeige();
        Assert.DoesNotContain(Energievergleich.GRUPPE_BEDARF, Gruppentitel(cut));

        cut.Find(".epos-ergebnis-anpassen-knopf").Click();
        var dialog = cut.FindComponent<ErgebnisAnpassenDialog>();
        Assert.False(dialog.Instance.OhneWertAus);
        cut.Find(".epos-ergebnis-anpassen-standard").Click();

        foreach (EnergievergleichGruppe g in Energievergleich.GRUPPEN_DATEN)
        {
            var radios = Gruppe(cut, g.Schluessel).QuerySelectorAll("input[type=radio]");
            int vorgabe = (int)(g.VorgabeOffen ? ErgebnisGruppenZustand.Offen : ErgebnisGruppenZustand.Zugeklappt);
            Assert.True(radios[vorgabe].HasAttribute("checked"), g.Schluessel);
        }
        Assert.Empty(dialog.Instance.KennzahlenAus);
        Assert.True(dialog.Instance.OhneWertAus);

        // Enter bestätigt: die Vorgabe gilt, die Schlüssel verschwinden aus der Ablage.
        cut.Find(".epos-ergebnis-anpassen").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        Assert.Contains(Energievergleich.GRUPPE_BEDARF, Gruppentitel(cut));
        Assert.Null(_ablage.Lies(UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN));
        Assert.Null(_ablage.Lies(UebersichtErgebnisAnzeige.SCHLUESSEL_KENNZAHLEN_AUS));
        Assert.Equal(-1, _ablage.LiesZahl(UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS, -1));
    }

    [Fact]
    public void Hinweis_nennt_die_Kennzahlen_ohne_Wert()
    {
        var cut = Zeige();
        int n = cut.Instance.Ergebnis!.OhneWertAusgeblendet;
        Assert.True(n > 0);
        string erwartet = n == 1 ? "1 Kennzahl ohne Wert ausgeblendet" : n + " Kennzahlen ohne Wert ausgeblendet";
        Assert.Equal(erwartet, cut.Find(".epos-ergebnis-ohnewert").TextContent);
        int zeilenVorher = cut.Instance.Ergebnis.Gruppen.Sum(g => g.Zeilen.Count);

        // Schalter aus: der Hinweis entfällt, die Zeilen ohne Wert stehen wieder da.
        cut.Find(".epos-ergebnis-anpassen-knopf").Click();
        cut.Find(".epos-ergebnis-anpassen-ohnewert input[type=checkbox]").Change(false);
        Ok(cut);

        Assert.Empty(cut.FindAll(".epos-ergebnis-ohnewert"));
        Assert.Equal(zeilenVorher + n, cut.Instance.Ergebnis!.Gruppen.Sum(g => g.Zeilen.Count));
        Assert.Equal(0, _ablage.LiesZahl(UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS, -1));
    }

    [Fact]
    public void Ohne_Speicherweg_kein_Knopf()
    {
        var huelle = Huelle();
        var cut = Render<UebersichtErgebnisse>(p => p.Add(x => x.Stand, Stand()).Add(x => x.Laden, huelle.Laden));

        Assert.Empty(cut.FindAll(".epos-ergebnis-anpassen-knopf"));
        Assert.NotEmpty(cut.FindAll("tr.epos-ergebnis-gruppe"));
    }

    [Fact]
    public void Dialog_ohne_Gaben_zeichnet_und_meldet_null_bei_Abbruch()
    {
        ErgebnisAnzeigeEinstellung? gemeldet = ErgebnisAnzeigeEinstellung.Standard();
        var cut = Render<ErgebnisAnpassenDialog>(p => p.Add(x => x.Geschlossen, (ErgebnisAnzeigeEinstellung? e) => gemeldet = e));

        Assert.Equal("Anzeige anpassen", cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.Empty(cut.FindAll(".epos-ergebnis-anpassen-gruppe"));
        cut.FindAll(".epos-leiste .epos-knopf").Single(k => k.TextContent == "Abbrechen").Click();
        Assert.Null(gemeldet);
    }
}
