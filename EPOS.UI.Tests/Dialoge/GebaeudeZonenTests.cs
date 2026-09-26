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
/// Zonen und Bauteile im Gebäudedialog (Gebäudesimulation G3, Welle D2; Softwarearchitektur 2.9,
/// 3.2, 3.3): der Gebäudeeditor in der Betriebsart Projekt — die Herleitungszeile „Rechenweg der
/// Hülle" in beiden Stellungen, der Knopf „Gebäude als eine Zone übernehmen" (frei, weich gesperrt
/// mit Grund) samt Rückfrage, die Zone im Arbeitsstand mit negativen Ids, der Zonenreiter leer und
/// mit Zone, die abgeleitete Hülle (Summenregel), der OK-Weg in benannten Schritten, „Speichern
/// unter" mit Zone (Rückfrage VOR dem Schreiben) — dazu die Skalierungsangabe mit Zone und der Knopf
/// „Hülle und Zonen…" des Wirtes.
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche Beschriftungen und
/// Zahlen.</para>
/// </summary>
public class GebaeudeZonenTests : EposBunitContext
{
    private static readonly string[] TYPEN = { "Einfamilienhaus", "Hotel" };
    private static readonly string[] ARTEN = { "Einfamilienhaus", "Hotel", "Kaufhaus" };
    private static readonly string[] KLASSEN = { "vor 1919", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968", "1969 bis 1978" };

    private const string KNOPF = "Gebäude als eine Zone übernehmen";

    public GebaeudeZonenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein vollständig belegter Satz (derselbe wie im Katalogeditor-Test): H_T = 185 W/K.</summary>
    private static GebaeudeKatalogDaten Satz(string name = "Haus A") => new()
    {
        Name = name,
        Typ = "Einfamilienhaus",
        Beschreibung = "Beschreibung " + name,
        Gebaeudeart = "Hotel",
        Verwendung = "Wohngebaeude",
        Baualtersklasse = 4,
        Bauart = 1,
        WohnflaecheGesamt = 150,
        FlaecheNutzer = 35,
        Waermegewinne = 400,
        Fensterdurchlassgrad = 0.4,
        Raumhoehe = 2.5,
        FensterflaecheNord = 10,
        FensterflaecheSued = 20,
        FensterflaecheOstWest = 15,
        FlaecheAussenwand = 200,
        Dachflaeche = 120,
        Grundflaeche = 100,
        SonstigeFlaechen = 5,
        UWertAussenwand = 0.3,
        UWertFenster = 1.3,
        UWertDachflaeche = 0.2,
        UWertGrundflaeche = 0.35,
        UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1,
        AnschlussFensterWand = 50,
        SollTag = 20,
        NachtAbsenkung = 17,
        MaxTemperatur = 24,
        WochenendAbsenkung = 0,
        SollFerien = 0,
        Luftwechselrate = 0.5
    };

    /// <summary>
    /// Die Zone eines Vorschlags mit Faktor 1,5: Außenwand 0,3·300 = 90, Fenster 1,3·60 = 78, Dach
    /// 0,2·180 = 36 → H_T = 204 W/K; negative Ids, weil noch nichts geschrieben ist.
    /// </summary>
    private static ZoneDaten Vorschlagszone() => new()
    {
        Id = -1,
        Bezeichner = "Haus A",
        Nutzflaeche = 225,
        Bauteile =
        {
            new BauteilDaten { Id = -1, Bezeichner = "Außenwand", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                               Flaeche = 300, UWert = 0.3, Azimut = 180 },
            new BauteilDaten { Id = -2, Bezeichner = "Fenster Süd", Bauteilart = DbWerte.BAUTEILART_FENSTER,
                               Flaeche = 60, UWert = 1.3, Azimut = 180 },
            new BauteilDaten { Id = -3, Bezeichner = "Dach", Bauteilart = DbWerte.BAUTEILART_DACH,
                               Flaeche = 180, UWert = 0.2 }
        }
    };

    /// <summary>Die Gegenseite des Editors: merkt sich jeden Schreib- und Übernahmeaufruf.</summary>
    private sealed class Weg
    {
        internal readonly List<(GebaeudeKatalogDaten Daten, bool IstNeu, string Name)> Gespeichert = new();
        internal readonly List<IReadOnlyList<ZoneDaten>> Zonengeschrieben = new();
        internal readonly List<GebaeudeKatalogDaten> Probestaende = new();
        internal ZonenuebernahmeDaten Vorschlag = new(true, "", 1.5, Vorschlagszone(), 150, "Wohnfläche [m²]", 150, false);
        internal string Zonenfehler = "";

        internal GebaeudeKatalogErgebnis Speichern(GebaeudeKatalogDaten d, bool istNeu, string name)
        {
            Gespeichert.Add((d, istNeu, name));
            return new GebaeudeKatalogErgebnis(true, "");
        }

        internal GebaeudeZonenweg Zonenweg(IReadOnlyList<ZoneDaten>? zonen = null) => new()
        {
            Zonen = zonen ?? Array.Empty<ZoneDaten>(),
            Uebernehmen = d => { Probestaende.Add(d); return Task.FromResult(Vorschlag); },
            Speichern = s => { Zonengeschrieben.Add(s.Zonen.Select(z => z.Kopie()).ToList()); return Zonenfehler; }
        };
    }

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(
        Weg weg,
        GebaeudeKatalogModus modus = GebaeudeKatalogModus.Projekt,
        IReadOnlyList<ZoneDaten>? zonen = null,
        GebaeudeKatalogDaten? daten = null,
        Action<bool>? geschlossen = null)
        => Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Satz())
            .Add(x => x.Modus, modus)
            .Add(x => x.Gebaeudetypen, () => TYPEN)
            .Add(x => x.Gebaeudearten, () => ARTEN)
            .Add(x => x.Baualtersklassen, KLASSEN)
            .Add(x => x.Speichern, weg.Speichern)
            .Add(x => x.Zonen, modus == GebaeudeKatalogModus.Projekt ? weg.Zonenweg(zonen) : null)
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IReadOnlyList<IElement> Knoepfe(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll("button").Where(b => b.TextContent.Trim() == text).ToList();

    private static void ReiterWaehlen(IRenderedComponent<GebaeudeKatalogDialog> cut, string titel)
        => cut.FindAll("button[role=tab]").First(b => b.TextContent.Trim() == titel).Click();

    private static IElement Antwort(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == text);

    private static void Ok(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll(".epos-leiste button.epos-knopf--primaer").First(b => b.TextContent.Trim() == "OK").Click();

    private static void Uebernehmen(IRenderedComponent<GebaeudeKatalogDialog> cut, bool ja = true)
    {
        Knoepfe(cut, KNOPF)[0].Click();
        Antwort(cut, ja ? "Ja" : "Nein").Click();
    }

    // =================================================================================
    // Herleitungszeile und Knopf - in beiden Stellungen
    // =================================================================================

    [Fact]
    public void Ein_Katalogsatz_rechnet_den_Klassenweg_und_der_Knopf_nennt_den_Grund()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg, GebaeudeKatalogModus.Bearbeiten);

        Assert.StartsWith("Rechenweg der Hülle: Klassenweg", cut.Instance.Huellwegzeile);
        Assert.Contains("Ein Katalogsatz trägt keine Zonen", cut.Instance.Huellwegzeile);
        Assert.Contains(cut.FindAll(".epos-herleitung"), z => z.TextContent == cut.Instance.Huellwegzeile);

        IElement knopf = Knoepfe(cut, KNOPF).Single();
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Contains("Katalogsatz", knopf.GetAttribute("title"));

        knopf.Click();
        Assert.Contains("Katalogsatz trägt keine Zonen", cut.Instance.Meldung);
        Assert.Empty(weg.Probestaende);
        Assert.False(cut.Instance.UebernahmefrageOffen);
    }

    [Fact]
    public void Im_Projekt_ohne_Zone_ist_der_Knopf_frei()
    {
        var cut = Aufbauen(new Weg());

        Assert.StartsWith("Rechenweg der Hülle: Klassenweg – U-Wert-Gruppen und Bauweise. Mit",
                          cut.Instance.Huellwegzeile);
        IElement knopf = Knoepfe(cut, KNOPF).Single();
        Assert.Null(knopf.GetAttribute("aria-disabled"));
        Assert.Null(knopf.GetAttribute("title"));
    }

    [Fact]
    public void Auf_dem_Tagesbilanz_Weg_ist_der_Knopf_weich_gesperrt()
    {
        GebaeudeKatalogDaten d = Satz();
        d.Modell = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
        var weg = new Weg();
        var cut = Aufbauen(weg, daten: d);

        IElement knopf = Knoepfe(cut, KNOPF).Single();
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        knopf.Click();
        Assert.Contains("Tagesbilanz", cut.Instance.Meldung);
        Assert.Empty(weg.Probestaende);
    }

    // =================================================================================
    // Übernahme: Rückfrage, Arbeitsstand, abgeleitete Hülle
    // =================================================================================

    [Fact]
    public void Die_Uebernahme_fragt_mit_Faktor_und_Flaechen_und_legt_die_Zone_in_den_Arbeitsstand()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg);

        Knoepfe(cut, KNOPF)[0].Click();

        // Der Vorschlag rechnet auf dem Stand der Maske, nicht auf dem gespeicherten.
        Assert.Single(weg.Probestaende);
        Assert.True(cut.Instance.UebernahmefrageOffen);
        string frage = cut.Find(".epos-rueckfrage-text").TextContent;
        Assert.Contains("Faktor 1,5", frage);
        Assert.Contains("150,00 m² → 225,00 m²", frage);
        Assert.Contains("„150,00 Wohnfläche [m²]“", frage);
        Assert.Empty(cut.Instance.ZonenImArbeitsstand);

        Antwort(cut, "Ja").Click();

        ZoneDaten zone = Assert.Single(cut.Instance.ZonenImArbeitsstand);
        Assert.True(zone.Id < 0);
        Assert.All(zone.Bauteile, b => Assert.True(b.Id < 0));
        Assert.Equal("Rechenweg der Hülle: Bauteilweg – Zone „Haus A“, 3 Bauteile. Die U-Wert-Gruppen sind aus den " +
                     "Bauteilen abgeleitet; die Werte des Gebäudes gelten wieder ohne Zone.", cut.Instance.Huellwegzeile);

        // Geschrieben ist noch nichts - erst mit OK.
        Assert.Empty(weg.Gespeichert);
        Assert.Empty(weg.Zonengeschrieben);

        // Mit Zone ist der Knopf weich gesperrt und nennt die Zone.
        IElement knopf = Knoepfe(cut, KNOPF).Single();
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Contains("„Haus A“", knopf.GetAttribute("title"));
    }

    [Fact]
    public void Nein_laesst_den_Arbeitsstand_unberuehrt()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg);

        Uebernehmen(cut, ja: false);

        Assert.False(cut.Instance.UebernahmefrageOffen);
        Assert.Empty(cut.Instance.ZonenImArbeitsstand);
    }

    /// <summary>
    /// Die Leistungsgrenzen stehen mit ihrem Wert in der Rückfrage — nur die, die das Gebäude trägt —,
    /// und sie sagt, dass sie nicht hochgerechnet werden (E40, Konzept N1.45 Punkt 4).
    /// </summary>
    [Theory]
    [InlineData(12.5, null, "(Heizung 12,5 kW)")]
    [InlineData(null, 8.0, "(Kühlung 8 kW)")]
    [InlineData(12.5, 8.0, "(Heizung 12,5 kW, Kühlung 8 kW)")]
    public void Die_Leistungsgrenzen_stehen_mit_Wert_in_der_Rueckfrage(double? heiz, double? kuehl, string erwartet)
    {
        var weg = new Weg();
        weg.Vorschlag = weg.Vorschlag with { HeizgrenzeKw = heiz, KuehlgrenzeKw = kuehl };
        var cut = Aufbauen(weg);

        Knoepfe(cut, KNOPF)[0].Click();

        string frage = cut.Find(".epos-rueckfrage-text").TextContent;
        Assert.Contains("Die Leistungsgrenzen werden nicht hochgerechnet " + erwartet, frage);
        Assert.Contains("unverändert der hochgerechneten Hülle", frage);
    }

    [Fact]
    public void Ohne_Leistungsgrenze_nennt_die_Rueckfrage_keine()
    {
        var cut = Aufbauen(new Weg());

        Knoepfe(cut, KNOPF)[0].Click();

        Assert.DoesNotContain("Leistungsgrenze", cut.Find(".epos-rueckfrage-text").TextContent);
    }

    [Fact]
    public void Ein_gescheiterter_Vorschlag_meldet_seinen_Grund_ohne_Rueckfrage()
    {
        var weg = new Weg();
        weg.Vorschlag = new ZonenuebernahmeDaten(false, "Kein Klima für das Projekt.", double.NaN, null, 150, "", 0, false);
        var cut = Aufbauen(weg);

        Knoepfe(cut, KNOPF)[0].Click();

        Assert.False(cut.Instance.UebernahmefrageOffen);
        Assert.Equal("Kein Klima für das Projekt.", cut.Instance.Meldung);
        Assert.Empty(cut.Instance.ZonenImArbeitsstand);
    }

    /// <summary>Mit Zone sind die Werte der Hülle abgeleitete Anzeigen (Summenregel, Mehrzonenkonzept 4.3).</summary>
    [Fact]
    public void Mit_Zone_zeigt_die_Huelle_die_Summenregel_und_H_T_der_Bauteile()
    {
        var cut = Aufbauen(new Weg(), zonen: new[] { Vorschlagszone() });

        Assert.Contains("Abgeleitet aus den Bauteilen der Zone „Haus A“", cut.Markup);
        Assert.Contains(204.0.ToString("N1", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + " W/K", cut.Markup);
        Assert.Contains("225,00", cut.Markup);
    }

    // =================================================================================
    // Der Zonenreiter
    // =================================================================================

    [Fact]
    public void Der_leere_Zonenreiter_ist_bedienbar_mit_dem_Knopf()
    {
        var cut = Aufbauen(new Weg());

        ReiterWaehlen(cut, "Zonen");

        Assert.Contains("Noch keine Zone", cut.Markup);
        Assert.Empty(cut.FindAll("table.epos-zonenliste"));
        Assert.Single(Knoepfe(cut, KNOPF));
    }

    [Fact]
    public void Der_Zonenreiter_zeigt_die_Zone_und_oeffnet_den_Zonendialog()
    {
        var cut = Aufbauen(new Weg(), zonen: new[] { Vorschlagszone() });

        ReiterWaehlen(cut, "Zonen");

        IElement zeile = cut.Find("table.epos-zonenliste tbody tr");
        Assert.Contains("Haus A", zeile.TextContent);
        Assert.Contains("225,00 m²", zeile.TextContent);
        Assert.Empty(Knoepfe(cut, KNOPF));

        Knoepfe(cut, "Öffnen…")[0].Click();
        Assert.True(cut.Instance.ZonendialogOffen);

        IElement name = cut.Find(".epos-zonendialog label.epos-feld input");
        name.Input("Wohnen");
        cut.FindAll(".epos-zonendialog > .epos-leiste button.epos-knopf--primaer").Single().Click();

        Assert.False(cut.Instance.ZonendialogOffen);
        Assert.Equal("Wohnen", cut.Instance.ZonenImArbeitsstand[0].Bezeichner);
    }

    [Fact]
    public void Zone_entfernen_fragt_mit_Vorgabe_Nein_und_aendert_nur_den_Arbeitsstand()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg, zonen: new[] { Vorschlagszone() });
        ReiterWaehlen(cut, "Zonen");

        Knoepfe(cut, "Zone entfernen")[0].Click();
        Assert.Contains("epos-knopf--primaer", Antwort(cut, "Nein").ClassName);
        Antwort(cut, "Ja").Click();

        Assert.Empty(cut.Instance.ZonenImArbeitsstand);
        Assert.Empty(weg.Zonengeschrieben);
        Assert.StartsWith("Rechenweg der Hülle: Klassenweg", cut.Instance.Huellwegzeile);
    }

    // =================================================================================
    // Der OK-Weg und „Speichern unter"
    // =================================================================================

    [Fact]
    public void OK_schreibt_Gebaeudedaten_und_Zonen_in_benannten_Schritten()
    {
        var weg = new Weg();
        bool? zu = null;
        var cut = Aufbauen(weg, geschlossen: b => zu = b);
        Uebernehmen(cut);

        Ok(cut);

        var (daten, istNeu, name) = Assert.Single(weg.Gespeichert);
        Assert.False(istNeu);
        Assert.Equal("Haus A", name);
        IReadOnlyList<ZoneDaten> zonen = Assert.Single(weg.Zonengeschrieben);
        Assert.Equal(3, Assert.Single(zonen).Bauteile.Count);
        Assert.True(zu);
    }

    [Fact]
    public void Ohne_Aenderung_an_den_Zonen_schreibt_OK_keine_Zonen()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg, zonen: new[] { Vorschlagszone() });

        Ok(cut);

        Assert.Single(weg.Gespeichert);
        Assert.Empty(weg.Zonengeschrieben);
    }

    [Fact]
    public void Scheitert_der_Zonenschritt_bleibt_der_Dialog_offen_mit_Meldung()
    {
        var weg = new Weg { Zonenfehler = "Bauteil ohne Fläche." };
        bool? zu = null;
        var cut = Aufbauen(weg, geschlossen: b => zu = b);
        Uebernehmen(cut);

        Ok(cut);

        Assert.Null(zu);
        Assert.Contains("Die Zonen wurden nicht gespeichert: Bauteil ohne Fläche.", cut.Instance.Meldung);

        // Ein zweites OK schreibt die unveränderten Gebäudedaten nicht noch einmal.
        weg.Zonenfehler = "";
        Ok(cut);
        Assert.Single(weg.Gespeichert);
        Assert.Equal(2, weg.Zonengeschrieben.Count);
        Assert.True(zu);
    }

    [Fact]
    public void Speichern_unter_mit_Zone_fragt_vor_dem_Schreiben()
    {
        var weg = new Weg();
        bool? zu = null;
        var cut = Aufbauen(weg, zonen: new[] { Vorschlagszone() }, geschlossen: b => zu = b);

        Knoepfe(cut, "Speichern unter")[0].Click();

        Assert.Contains("trägt die Zone „Haus A“ mit 3 Bauteilen nicht mit", cut.Find(".epos-rueckfrage-text").TextContent);
        Assert.Empty(weg.Gespeichert);

        Antwort(cut, "Nein").Click();
        Assert.Empty(weg.Gespeichert);
        Assert.Null(zu);

        Knoepfe(cut, "Speichern unter")[0].Click();
        Antwort(cut, "Ja").Click();
        var (_, istNeu, _) = Assert.Single(weg.Gespeichert);
        Assert.True(istNeu);
        Assert.Empty(weg.Zonengeschrieben);
        Assert.True(zu);
    }

    [Fact]
    public void Speichern_unter_ohne_Zone_fragt_nicht()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg);

        Knoepfe(cut, "Speichern unter")[0].Click();

        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.True(Assert.Single(weg.Gespeichert).IstNeu);
    }

    /// <summary>Esc kaskadiert: Solange eine Rückfrage steht, schließt Esc nicht den Editor.</summary>
    [Fact]
    public void Esc_schliesst_nicht_solange_die_Rueckfrage_steht()
    {
        bool? zu = null;
        var cut = Aufbauen(new Weg(), geschlossen: b => zu = b);
        Knoepfe(cut, KNOPF)[0].Click();

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(zu);
    }

    // =================================================================================
    // Die Skalierungsangabe mit Zone
    // =================================================================================

    private IRenderedComponent<GebaeudeWohnflaecheDialog> Skalierung(string zone, Action<GebaeudeWohnflaecheErgebnis?>? geschlossen = null)
        => Render<GebaeudeWohnflaecheDialog>(p => p
            .Add(x => x.TitelText, "Eingabe")
            .Add(x => x.Gebaeudename, "Haus 1")
            .Add(x => x.Wert, 30.0)
            .Add(x => x.Jahresnutzungsgrad, 0.85)
            .Add(x => x.Einheit, "Verbrauch  [MWh/a]")
            .Add(x => x.Bedarfsarten, new[] { "Verbrauch  [MWh/a]", "Wohnfläche [m²]" })
            .Add(x => x.Zone, zone)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));

    [Fact]
    public void Mit_Zone_ist_die_Angabe_weich_gesperrt_und_die_Herleitungszeile_nennt_die_Zone()
    {
        GebaeudeWohnflaecheErgebnis? ergebnis = null;
        var cut = Skalierung("Wohnen", e => ergebnis = e);

        Assert.Contains(cut.FindAll(".epos-herleitung"), z => z.TextContent.Contains("Zone „Wohnen“"));
        Assert.Empty(cut.FindAll("select"));
        IElement sperre = cut.Find(".epos-gebw-zonensperre");
        Assert.Equal("true", sperre.GetAttribute("aria-disabled"));

        sperre.Click();
        Assert.Contains("Nutzfläche seiner Zone", cut.Instance.Meldung);

        // Der Schalter bleibt einstellbar; die Angabe reist unverändert zurück.
        cut.Find("input[type=checkbox]").Change(true);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "OK").Click();
        Assert.NotNull(ergebnis);
        Assert.Equal(30.0, ergebnis!.Wert);
        Assert.Equal("Verbrauch  [MWh/a]", ergebnis.Einheit);
        Assert.True(ergebnis.DezentralWarmwasser);
    }

    [Fact]
    public void Ohne_Zone_bleibt_die_Angabe_bedienbar()
    {
        var cut = Skalierung("");

        Assert.Single(cut.FindAll("select"));
        Assert.Empty(cut.FindAll(".epos-gebw-zonensperre"));
    }

    // =================================================================================
    // Der Wirt: „Hülle und Zonen…"
    // =================================================================================

    private IRenderedComponent<GebaeudeDialog> Wirt(bool hatKopie, Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>?>? gaben)
        => Render<GebaeudeDialog>(p => p
            .Add(x => x.Zeilen, new List<GebaeudeProjektZeile>
            {
                new()
                {
                    IdZ = 1, IdGebaeude = 7, IdKatalog = 42, Name = "Haus 1990", Art = "Einfamilienhaus",
                    Wohnflaeche = 150, Einheit = "Wohnfläche [m²]", Jahresnutzungsgrad = 1, HatProjektkopie = hatKopie
                }
            })
            .Add(x => x.Katalogzeilen, () => Array.Empty<Katalogfilterzeile>())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.ProjektGaben, gaben));

    [Fact]
    public void Ohne_Projektkopie_ist_Huelle_und_Zonen_weich_gesperrt()
    {
        int gerufen = 0;
        var cut = Wirt(false, _ => { gerufen++; return null; });

        IElement knopf = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Gebäude im Projekt bearbeiten…");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        knopf.Click();

        Assert.Contains("noch nicht im Projekt gespeichert", cut.Instance.Meldung);
        Assert.Equal(0, gerufen);
        Assert.False(cut.Instance.ProjekteditorOffen);
    }

    [Fact]
    public void Mit_Projektkopie_oeffnet_Huelle_und_Zonen_den_Editor_als_Ueberlagerung()
    {
        var weg = new Weg();
        var cut = Wirt(true, _ => new Dictionary<string, object>
        {
            ["Daten"] = Satz(),
            ["Modus"] = GebaeudeKatalogModus.Projekt,
            ["Gebaeudetypen"] = new Func<IReadOnlyList<string>>(() => TYPEN),
            ["Gebaeudearten"] = new Func<IReadOnlyList<string>>(() => ARTEN),
            ["Baualtersklassen"] = (IReadOnlyList<string>)KLASSEN,
            ["Speichern"] = new Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>(weg.Speichern),
            ["Zonen"] = weg.Zonenweg()
        });

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Gebäude im Projekt bearbeiten…").Click();

        Assert.True(cut.Instance.ProjekteditorOffen);
        Assert.Contains(KNOPF, cut.Markup);
    }

    [Fact]
    public void Ohne_Delegat_gibt_es_keinen_Knopf_Huelle_und_Zonen()
    {
        var cut = Wirt(true, null);

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Gebäude im Projekt bearbeiten…");
    }
}
