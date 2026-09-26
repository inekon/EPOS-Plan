using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Zonenliste im Gebäudeeditor</b> (Gebäudesimulation G6a, Welle 3): drei Zonen laden, die
/// zweite öffnen und mit OK zurückgeben — alle drei stehen noch (Behebung der stillen Löschung); ▲▼,
/// Duplizieren und „+ Neue Zone …"; die Rückfragen vor der ersten und der zweiten Zone, die Sperre auf
/// dem Tagesbilanz-Weg und ab der Höchstzahl der Zonen; die
/// Pflichtfläche im Zonendialog; Summenfuß und Hinweise; die KI-Sicht der Zonenliste.
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche Beschriftungen und Zahlen.</para>
/// </summary>
public class GebaeudeZonenlisteTests : EposBunitContext
{
    private const string NEUE_ZONE = "+ Neue Zone …";

    public GebaeudeZonenlisteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein vollständig belegter Satz (derselbe wie in <see cref="GebaeudeZonenTests"/>), der die Prüfung besteht.</summary>
    private static GebaeudeKatalogDaten Satz(string? modell = null) => new()
    {
        Name = "Haus A", Typ = "Einfamilienhaus", Beschreibung = "Beschreibung Haus A", Gebaeudeart = "Hotel",
        Verwendung = "Wohngebaeude", Baualtersklasse = 4, Bauart = 1, WohnflaecheGesamt = 150, FlaecheNutzer = 35,
        Waermegewinne = 400, Fensterdurchlassgrad = 0.4, Raumhoehe = 2.5, FensterflaecheNord = 10, FensterflaecheSued = 20,
        FensterflaecheOstWest = 15, FlaecheAussenwand = 200, Dachflaeche = 120, Grundflaeche = 100, SonstigeFlaechen = 5,
        UWertAussenwand = 0.3, UWertFenster = 1.3, UWertDachflaeche = 0.2, UWertGrundflaeche = 0.35, UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1, AnschlussFensterWand = 50, SollTag = 20, NachtAbsenkung = 17, MaxTemperatur = 24,
        WochenendAbsenkung = 0, SollFerien = 0, Luftwechselrate = 0.5, Modell = modell
    };

    private static ZoneDaten Zone(int id, string name, double? flaeche, double u = 0.3)
        => new()
        {
            Id = id, Bezeichner = name, Nutzflaeche = flaeche,
            Bauteile =
            {
                new BauteilDaten { Id = id * 10 + 1, Bezeichner = "Wand " + name, Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                   Flaeche = 100, UWert = u, Azimut = 180 },
                new BauteilDaten { Id = id * 10 + 2, Bezeichner = "Dach " + name, Bauteilart = DbWerte.BAUTEILART_DACH,
                                   Flaeche = 50, UWert = 0.2 }
            }
        };

    /// <summary>Drei gespeicherte Zonen, 60 + 50 + 40 = 150 m² — so viel wie das Gebäude.</summary>
    private static ZoneDaten[] DreiZonen() => new[] { Zone(1, "Erdgeschoss", 60), Zone(2, "Obergeschoss", 50), Zone(3, "Dach", 40) };

    private sealed class Weg
    {
        internal readonly List<IReadOnlyList<ZoneDaten>> Zonengeschrieben = new();

        internal GebaeudeZonenweg Zonenweg(IReadOnlyList<ZoneDaten>? zonen) => new()
        {
            Zonen = zonen ?? Array.Empty<ZoneDaten>(),
            Uebernehmen = _ => Task.FromResult(new ZonenuebernahmeDaten(true, "", 1.0, Zone(-1, "Übernahme", 150), 150, "Wohnfläche [m²]", 150, false)),
            Speichern = s => { Zonengeschrieben.Add(s.Zonen.Select(z => z.Kopie()).ToList()); return ""; }
        };
    }

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(Weg weg, IReadOnlyList<ZoneDaten>? zonen = null,
                                                               GebaeudeKatalogDaten? daten = null)
    {
        var cut = Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Satz())
            .Add(x => x.Modus, GebaeudeKatalogModus.Projekt)
            .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
            .Add(x => x.Gebaeudearten, () => new[] { "Hotel" })
            .Add(x => x.Baualtersklassen, new[] { "a", "b", "c", "d", "e" })
            .Add(x => x.Speichern, (d, neu, name) => new GebaeudeKatalogErgebnis(true, ""))
            .Add(x => x.Zonen, weg.Zonenweg(zonen)));
        cut.FindAll("button[role=tab]").First(b => b.TextContent.Trim() == "Zonen").Click();
        return cut;
    }

    private static IReadOnlyList<IElement> Knoepfe(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll("button").Where(b => b.TextContent.Trim() == text).ToList();

    private static IElement Antwort(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == text);

    private static IReadOnlyList<IElement> Zeilen(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll("table.epos-zonenliste tbody tr");

    private static void ZonendialogOk(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll(".epos-zonendialog > .epos-leiste button.epos-knopf--primaer").Single().Click();

    private static void Ok(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll(".epos-leiste button.epos-knopf--primaer").First(b => b.TextContent.Trim() == "OK").Click();

    private static string[] Namen(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.Instance.ZonenImArbeitsstand.Select(z => z.Bezeichner).ToArray();

    // =================================================================================
    // Die Liste
    // =================================================================================

    /// <summary>
    /// <b>Die Regressionsprobe der Oberfläche</b>: drei Zonen, die zweite geöffnet und mit OK
    /// zurückgegeben — alle drei stehen noch, die zweite geändert; das OK des Editors schreibt alle drei.
    /// </summary>
    [Fact]
    public void Drei_Zonen_die_zweite_oeffnen_OK_alle_drei_stehen_noch()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg, DreiZonen());

        Assert.Equal(3, Zeilen(cut).Count);
        Knoepfe(cut, "Öffnen…")[1].Click();
        Assert.True(cut.Instance.ZonendialogOffen);
        cut.Find(".epos-zonendialog label.epos-feld input").Input("Obergeschoss neu");
        ZonendialogOk(cut);

        Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss neu", "Dach" }, Namen(cut));
        Assert.Equal(new[] { 1, 2, 3 }, cut.Instance.ZonenImArbeitsstand.Select(z => z.Id));

        Ok(cut);
        IReadOnlyList<ZoneDaten> geschrieben = Assert.Single(weg.Zonengeschrieben);
        Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss neu", "Dach" }, geschrieben.Select(z => z.Bezeichner));
    }

    [Fact]
    public void Die_Liste_zeigt_jede_Zone_mit_Summenfuss()
    {
        var cut = Aufbauen(new Weg(), DreiZonen());

        IReadOnlyList<IElement> zeilen = Zeilen(cut);
        Assert.Equal(3, zeilen.Count);
        Assert.Contains("Obergeschoss", zeilen[1].TextContent);
        Assert.Contains("50,00 m²", zeilen[1].TextContent);
        // H_T je Zone: 0,3·100 + 0,2·50 = 40 W/K; Σ 120 W/K, Σ 150 m², Σ 6 Bauteile.
        Assert.Contains("40,0 W/K", zeilen[1].TextContent);
        IElement fuss = cut.Find("table.epos-zonenliste tfoot tr");
        Assert.Contains("Summe", fuss.TextContent);
        Assert.Contains("150,00 m²", fuss.TextContent);
        Assert.Contains("120,0 W/K", fuss.TextContent);
        Assert.Contains("6", fuss.QuerySelectorAll("td")[4].TextContent);
        // 150 gegen 150 m²: kein Flächenhinweis.
        Assert.DoesNotContain("Σ Zonen", cut.Markup);
    }

    [Fact]
    public void Weicht_die_Flaechensumme_ab_nennt_die_Herleitungszeile_beide_Summen()
    {
        ZoneDaten[] zonen = DreiZonen();
        zonen[2].Nutzflaeche = 80;              // 190 gegen 150 m²
        var cut = Aufbauen(new Weg(), zonen);

        Assert.Contains(cut.FindAll(".epos-herleitung"), z => z.TextContent.StartsWith("Σ Zonen 190 m² gegen Gebäude 150 m² (+26,7 %)", StringComparison.Ordinal));
    }

    [Fact]
    public void Hoch_und_runter_ordnen_um_am_Rand_ist_der_Knopf_gesperrt()
    {
        var cut = Aufbauen(new Weg(), DreiZonen());

        IReadOnlyList<IElement> zeilen = Zeilen(cut);
        Assert.True(zeilen[0].QuerySelector("button.epos-zone-hoch")!.HasAttribute("disabled"));
        Assert.True(zeilen[2].QuerySelector("button.epos-zone-runter")!.HasAttribute("disabled"));

        Zeilen(cut)[2].QuerySelector("button.epos-zone-hoch")!.Click();
        Assert.Equal(new[] { "Erdgeschoss", "Dach", "Obergeschoss" }, Namen(cut));
        Zeilen(cut)[0].QuerySelector("button.epos-zone-runter")!.Click();
        Assert.Equal(new[] { "Dach", "Erdgeschoss", "Obergeschoss" }, Namen(cut));
    }

    [Fact]
    public void Duplizieren_legt_die_Kopie_hinter_die_Zone_mit_neuer_Id_und_Vorlage()
    {
        var cut = Aufbauen(new Weg(), DreiZonen());

        Knoepfe(cut, "Duplizieren")[0].Click();

        Assert.False(cut.Instance.NachfrageOffen);      // aus drei werden vier - keine Rückfrage
        Assert.Equal(new[] { "Erdgeschoss", "Erdgeschoss (Kopie)", "Obergeschoss", "Dach" }, Namen(cut));
        ZoneDaten kopie = cut.Instance.ZonenImArbeitsstand[1];
        Assert.True(kopie.Id < 0);
        Assert.Equal(1, kopie.VorlageId);
        Assert.Equal(2, kopie.Bauteile.Count);
        Assert.All(kopie.Bauteile, b => Assert.True(b.Id < 0));
    }

    [Fact]
    public void Entfernen_einer_von_drei_Zonen_nennt_Name_und_Bauteile_und_laesst_die_anderen()
    {
        var cut = Aufbauen(new Weg(), DreiZonen());

        Knoepfe(cut, "Zone entfernen")[1].Click();
        string frage = cut.Find(".epos-rueckfrage-text").TextContent;
        Assert.Contains("Zone „Obergeschoss“ mit 2 Bauteilen entfernen?", frage);
        Assert.Contains("Die übrigen Zonen bleiben", frage);
        Antwort(cut, "Ja").Click();

        Assert.Equal(new[] { "Erdgeschoss", "Dach" }, Namen(cut));
    }

    // =================================================================================
    // „+ Neue Zone …", Rückfragen, Sperren
    // =================================================================================

    [Fact]
    public void Vor_der_ersten_Zone_fragt_der_Dialog_und_legt_sie_mit_OK_an()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg);

        Assert.Single(Knoepfe(cut, NEUE_ZONE));
        Knoepfe(cut, NEUE_ZONE)[0].Click();
        Assert.True(cut.Instance.NachfrageOffen);
        Assert.StartsWith("Ab jetzt rechnet dieses Gebäude über die Bauteile seiner Zone", cut.Instance.Nachfragetext);
        Antwort(cut, "Ja").Click();

        Assert.True(cut.Instance.ZonendialogOffen);
        Assert.False(cut.Instance.ZonendialogPflicht);          // die einzige Zone darf die Fläche des Gebäudes nehmen
        Assert.Empty(cut.Instance.ZonenImArbeitsstand);         // erst das OK des Zonendialogs legt sie an
        ZonendialogOk(cut);

        ZoneDaten neu = Assert.Single(cut.Instance.ZonenImArbeitsstand);
        Assert.Equal("Zone 1", neu.Bezeichner);
        Assert.True(neu.Id < -1);                               // unter der Id der Übernahme
    }

    [Fact]
    public void Abbrechen_des_Zonendialogs_legt_keine_Zone_an_und_Nein_oeffnet_keinen()
    {
        var cut = Aufbauen(new Weg());

        Knoepfe(cut, NEUE_ZONE)[0].Click();
        Antwort(cut, "Nein").Click();
        Assert.False(cut.Instance.ZonendialogOffen);

        Knoepfe(cut, NEUE_ZONE)[0].Click();
        Antwort(cut, "Ja").Click();
        cut.FindAll(".epos-zonendialog > .epos-leiste button").First(b => b.TextContent.Trim() == "Abbrechen").Click();
        Assert.Empty(cut.Instance.ZonenImArbeitsstand);
    }

    [Fact]
    public void Vor_der_zweiten_Zone_fragt_der_Dialog_und_verlangt_die_Flaeche()
    {
        var cut = Aufbauen(new Weg(), new[] { Zone(1, "Haus", null) });

        Knoepfe(cut, NEUE_ZONE)[0].Click();
        Assert.True(cut.Instance.NachfrageOffen);
        Assert.StartsWith("Mit zwei Zonen rechnet die Simulation jede Zone für sich", cut.Instance.Nachfragetext);
        Assert.Contains("epos-knopf--primaer", Antwort(cut, "Nein").ClassName);
        Antwort(cut, "Ja").Click();

        // Die zweite Zone braucht ihre Fläche: kein Platzhalter „Nutzfläche des Gebäudes", OK ohne Fläche hält an.
        Assert.True(cut.Instance.ZonendialogPflicht);
        IElement flaeche = cut.FindAll(".epos-zonendialog label.epos-feld input")[1];
        Assert.True(string.IsNullOrEmpty(flaeche.GetAttribute("placeholder")));
        Assert.Contains("Mit mehreren Zonen braucht jede Zone ihre eigene Nutzfläche", cut.Find(".epos-zonendialog").TextContent);
        ZonendialogOk(cut);
        Assert.True(cut.Instance.ZonendialogOffen);
        Assert.Contains("Zone „Zone 2“: Mit mehreren Zonen braucht jede Zone ihre eigene Nutzfläche", cut.Find(".epos-zonendialog").TextContent);
        cut.FindAll(".epos-zonendialog label.epos-feld input")[1].Input("40");
        ZonendialogOk(cut);

        Assert.Equal(new[] { "Haus", "Zone 2" }, Namen(cut));
        Assert.DoesNotContain(cut.FindAll(".epos-warnbanner"), w => w.TextContent.Contains("lehnt die Simulation"));
        // Die erste Zone hat keine eigene Fläche: Die Liste nennt das, und OK hält mit der Regel des Kerns an.
        Assert.Contains("fehlt", Zeilen(cut)[0].TextContent);
    }

    [Fact]
    public void Duplizieren_aus_einer_Zone_fragt_vor_der_zweiten()
    {
        var cut = Aufbauen(new Weg(), new[] { Zone(1, "Haus", 80) });

        Knoepfe(cut, "Duplizieren")[0].Click();
        Assert.True(cut.Instance.NachfrageOffen);
        Assert.Single(cut.Instance.ZonenImArbeitsstand);
        Antwort(cut, "Ja").Click();

        Assert.Equal(new[] { "Haus", "Haus (Kopie)" }, Namen(cut));
    }

    [Fact]
    public void Auf_dem_Tagesbilanz_Weg_ist_Neue_Zone_weich_gesperrt()
    {
        var cut = Aufbauen(new Weg(), daten: Satz(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ));

        IElement knopf = Knoepfe(cut, NEUE_ZONE).Single();
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Contains("Tagesbilanz", knopf.GetAttribute("title"));
        knopf.Click();
        Assert.False(cut.Instance.NachfrageOffen);
        Assert.Contains("Tagesbilanz", cut.Instance.Meldung);
    }

    [Fact]
    public void Ab_der_Hoechstzahl_sind_Neue_Zone_und_Duplizieren_gesperrt()
    {
        var weg = new Weg();
        ZoneDaten[] alle = Enumerable.Range(1, GebaeudeZonenregeln.PFLEGEGRENZE).Select(i => Zone(i, "Zone " + i, 10)).ToArray();
        var fast = Aufbauen(weg, alle.Take(GebaeudeZonenregeln.PFLEGEGRENZE - 1).ToArray());
        Assert.Null(fast.Instance.NeueZoneSperre);             // bis zur Höchstzahl geht noch eine

        var cut = Aufbauen(weg, alle);
        const string sperre = "Ein Gebäude trägt höchstens 50 Zonen.";
        IElement knopf = Knoepfe(cut, NEUE_ZONE).Single();
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal(sperre, knopf.GetAttribute("title"));
        Assert.All(Knoepfe(cut, "Duplizieren"), k => Assert.Equal("true", k.GetAttribute("aria-disabled")));
        knopf.Click();
        Assert.False(cut.Instance.NachfrageOffen);
        Assert.Equal(sperre, cut.Instance.Meldung);
    }

    [Fact]
    public void Speichern_unter_nennt_die_Zonen_die_an_der_Projektkopie_bleiben()
    {
        var cut = Aufbauen(new Weg(), DreiZonen());

        Assert.Contains(cut.FindAll(".epos-herleitung"),
                        z => z.TextContent == "Der neue Katalogsatz trägt keine Zonen; die Zonen (3) mit 6 Bauteilen bleiben an der Projektkopie.");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Speichern unter").Click();
        Assert.Contains("Der Katalogsatz trägt die 3 Zonen mit 6 Bauteilen nicht mit; sie bleiben an der Projektkopie",
                        cut.Find(".epos-rueckfrage-text").TextContent);
    }

    [Fact]
    public void Esc_schliesst_nicht_solange_die_Rueckfrage_steht()
    {
        bool geschlossen = false;
        var cut = Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, Satz())
            .Add(x => x.Modus, GebaeudeKatalogModus.Projekt)
            .Add(x => x.Speichern, (d, neu, name) => new GebaeudeKatalogErgebnis(true, ""))
            .Add(x => x.Zonen, new Weg().Zonenweg(null))
            .Add(x => x.Geschlossen, _ => geschlossen = true));
        cut.FindAll("button[role=tab]").First(b => b.TextContent.Trim() == "Zonen").Click();
        cut.FindAll("button").First(b => b.TextContent.Trim() == NEUE_ZONE).Click();

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(geschlossen);
    }

    // =================================================================================
    // Die KI-Sicht
    // =================================================================================

    [Fact]
    public void Die_KI_Sicht_zeigt_die_Zonenliste_zum_Lesen()
    {
        var stand = new GebaeudeArbeitsstand();
        stand.Laden(Satz(), false);
        stand.ZonenLaden(DreiZonen(), true);

        GebaeudeKatalogKiSicht sicht = stand.KiSicht(new GebaeudeKiWege());
        Assert.Equal(new[] { "1", "2", "3" }, sicht.Zonen.Select(z => z.Nummer));
        Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss", "Dach" }, sicht.Zonen.Select(z => z.Name));
        Assert.Equal(50.0, sicht.Zonen[1].Nutzflaeche);
        Assert.Equal(40.0, sicht.Zonen[1].HT, 9);
        Assert.Equal(2, sicht.Zonen[1].Bauteile);

        // Je Zugriff neu über dem Arbeitsstand.
        stand.ZoneEntfernen(2);
        Assert.Equal(2, sicht.Zonen.Count);
    }
}
