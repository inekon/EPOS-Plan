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
/// <b>Die Kopplung der Zonen im Gebäudeeditor</b> (Gebäudesimulation G6b, Welle W2): der Zonendialog
/// bekommt die Vorgaben des Gebäudes, die übrigen Zonen als Nachbarn (auch vorläufige Ids) und die
/// Trennflächen, die eine andere Zone mit ihm führt; ohne Schemaschritt S-G bietet der Bauteildialog
/// keine Nachbarzone an.
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche Beschriftungen und Zahlen.</para>
/// </summary>
public class GebaeudeZonenkopplungTests : EposBunitContext
{
    public GebaeudeZonenkopplungTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein vollständig belegter Satz (wie in <see cref="GebaeudeZonenlisteTests"/>) mit 10 kW Heizleistungsgrenze.</summary>
    private static GebaeudeKatalogDaten Satz() => new()
    {
        Name = "Haus A", Typ = "Einfamilienhaus", Beschreibung = "Beschreibung Haus A", Gebaeudeart = "Hotel",
        Verwendung = "Wohngebaeude", Baualtersklasse = 4, Bauart = 1, WohnflaecheGesamt = 150, FlaecheNutzer = 35,
        Waermegewinne = 400, Fensterdurchlassgrad = 0.4, Raumhoehe = 2.5, FensterflaecheNord = 10, FensterflaecheSued = 20,
        FensterflaecheOstWest = 15, FlaecheAussenwand = 200, Dachflaeche = 120, Grundflaeche = 100, SonstigeFlaechen = 5,
        UWertAussenwand = 0.3, UWertFenster = 1.3, UWertDachflaeche = 0.2, UWertGrundflaeche = 0.35, UWertSonstiges = 0.5,
        WbvkFensterWand = 0.1, AnschlussFensterWand = 50, SollTag = 20, NachtAbsenkung = 17, MaxTemperatur = 24,
        WochenendAbsenkung = 0, SollFerien = 0, Luftwechselrate = 0.5, HeizleistungMax = 10
    };

    private static ZoneDaten Zone(int id, string name, double? flaeche)
        => new()
        {
            Id = id, Bezeichner = name, Nutzflaeche = flaeche,
            Bauteile =
            {
                new BauteilDaten { Id = id * 10 + 1, Bezeichner = "Wand " + name, Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                                   Flaeche = 100, UWert = 0.3, Azimut = 180 },
                new BauteilDaten { Id = id * 10 + 2, Bezeichner = "Dach " + name, Bauteilart = DbWerte.BAUTEILART_DACH,
                                   Flaeche = 50, UWert = 0.2 }
            }
        };

    /// <summary>
    /// Zwei gespeicherte Zonen, 60 + 90 = 150 m²; das Erdgeschoss führt die Decke zum Obergeschoss als
    /// Trennfläche.
    /// </summary>
    private static ZoneDaten[] ZweiGekoppelt()
    {
        ZoneDaten eg = Zone(1, "Erdgeschoss", 60);
        eg.Bauteile.Add(new BauteilDaten
        {
            Id = 13, Bezeichner = "Decke EG/OG", Bauteilart = DbWerte.BAUTEILART_DECKE, Flaeche = 60, UWert = 0.9,
            Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, IdNachbarzone = 2
        });
        return new[] { eg, Zone(2, "Obergeschoss", 90) };
    }

    private sealed class Weg
    {
        internal string? KopplungSperre;
        internal IReadOnlyList<ZonenluftstromDaten> Luftstroeme = Array.Empty<ZonenluftstromDaten>();
        internal IReadOnlyList<string> Hinweise = Array.Empty<string>();
        internal readonly List<ZonenstandDaten> Geschrieben = new();
        internal readonly List<ZonenstandDaten> Geprueft = new();

        internal GebaeudeZonenweg Zonenweg(IReadOnlyList<ZoneDaten>? zonen) => new()
        {
            Zonen = zonen ?? Array.Empty<ZoneDaten>(),
            Luftstroeme = Luftstroeme,
            Speichern = s => { Geschrieben.Add(s); return ""; },
            Pruefen = s => { Geprueft.Add(s); return ""; },
            Hinweise = _ => Hinweise,
            KopplungSperre = KopplungSperre
        };
    }

    private static IElement Antwort(IRenderedComponent<GebaeudeKatalogDialog> cut, string text)
        => cut.FindAll(".epos-rueckfrage button").First(b => b.TextContent.Trim() == text);

    private static void Ok(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll(".epos-leiste button.epos-knopf--primaer").First(b => b.TextContent.Trim() == "OK").Click();

    private static IReadOnlyList<IElement> Zeilen(IRenderedComponent<GebaeudeKatalogDialog> cut)
        => cut.FindAll("table.epos-zonenliste tbody tr");

    private IRenderedComponent<GebaeudeKatalogDialog> Aufbauen(Weg weg, IReadOnlyList<ZoneDaten>? zonen = null)
    {
        var cut = Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, Satz())
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

    private static IElement? Feld(IElement bereich, string beschriftung)
        => bereich.QuerySelectorAll("label.epos-feld")
                  .FirstOrDefault(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == beschriftung)
                  ?.QuerySelector("input, select");

    // =================================================================================
    // Der Zonendialog im Gebäudeeditor
    // =================================================================================

    /// <summary>
    /// Die Vorgaben kommen aus dem Arbeitsstand des Gebäudes: Sollwert unverändert, die Leistungsgrenze
    /// ab zwei Zonen nach dem Flächenanteil (60 / 150 = 40 %).
    /// </summary>
    [Fact]
    public void Der_Zonendialog_zeigt_die_Vorgaben_des_Gebaeudes()
    {
        var cut = Aufbauen(new Weg(), ZweiGekoppelt());

        Knoepfe(cut, "Öffnen…")[0].Click();
        IElement zone = cut.Find(".epos-zonendialog");

        Assert.Equal("Vorgabe: 20", Feld(zone, "Soll am Tag")!.GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 4 (40 % des Gebäudes)", Feld(zone, "Heizleistungsgrenze")!.GetAttribute("placeholder"));
        Assert.Equal("Vorgabe: 160 (40 % des Gebäudes)", Feld(zone, "Interne Wärmegewinne")!.GetAttribute("placeholder"));
    }

    /// <summary>
    /// Das Obergeschoss zeigt die Decke, die das Erdgeschoss führt, gespiegelt und nur zum Lesen; sein
    /// Bauteildialog bietet das Erdgeschoss als Nachbarzone — nicht sich selbst.
    /// </summary>
    [Fact]
    public void Der_Zonendialog_zeigt_die_Gegenseite_und_bietet_die_Nachbarzonen()
    {
        var cut = Aufbauen(new Weg(), ZweiGekoppelt());

        Knoepfe(cut, "Öffnen…")[0].Click();
        IElement trenn = cut.FindAll(".epos-zonenbauteil").Last();
        Assert.Contains("Obergeschoss", trenn.TextContent);
        Assert.Empty(cut.FindAll(".epos-zonengegenseite"));
        cut.FindAll(".epos-zonendialog > .epos-leiste button.epos-knopf--primaer").Single().Click();

        Knoepfe(cut, "Öffnen…")[1].Click();
        IElement gegen = cut.Find(".epos-zonengegenseite");
        Assert.Contains("Decke EG/OG", gegen.TextContent);
        Assert.Contains("geführt von Erdgeschoss", gegen.TextContent);

        Knoepfe(cut, "+ Neues Bauteil …").Single().Click();
        IElement bauteil = cut.Find(".epos-bauteildialog");
        Feld(bauteil, "Randbedingung")!.Change("4");
        IElement nachbar = Feld(cut.Find(".epos-bauteildialog"), "Nachbarzone")!;
        Assert.Contains("Erdgeschoss", nachbar.TextContent);
        Assert.DoesNotContain("Obergeschoss", nachbar.TextContent);
    }

    /// <summary>Eine neue, noch ungespeicherte Zone steht mit ihrer vorläufigen Id als Nachbar zur Wahl.</summary>
    [Fact]
    public void Eine_neue_Zone_steht_mit_vorlaeufiger_Id_als_Nachbar_zur_Wahl()
    {
        var cut = Aufbauen(new Weg(), ZweiGekoppelt());

        Knoepfe(cut, "Duplizieren")[1].Click();
        ZoneDaten kopie = cut.Instance.ZonenImArbeitsstand[2];
        Assert.True(kopie.Id < 0);

        Knoepfe(cut, "Öffnen…")[0].Click();
        Knoepfe(cut, "+ Neues Bauteil …").Single().Click();
        Feld(cut.Find(".epos-bauteildialog"), "Randbedingung")!.Change("4");
        IElement nachbar = Feld(cut.Find(".epos-bauteildialog"), "Nachbarzone")!;
        Assert.Contains(nachbar.QuerySelectorAll("option"),
                        o => o.GetAttribute("value") == kopie.Id.ToString(System.Globalization.CultureInfo.CurrentCulture)
                             && o.TextContent.Contains(kopie.Bezeichner));
    }

    // =================================================================================
    // Der Zonenreiter: Volumen, beheizt, Hinweise
    // =================================================================================

    [Fact]
    public void Die_Zonenliste_nennt_Volumen_und_beheizt()
    {
        ZoneDaten[] zonen = ZweiGekoppelt();
        zonen[1].IstBeheizt = false;
        zonen[1].Volumen = 300;
        var cut = Aufbauen(new Weg(), zonen);

        Assert.Contains("Volumen", cut.Find("table.epos-zonenliste thead").TextContent);
        Assert.Contains("beheizt", cut.Find("table.epos-zonenliste thead").TextContent);
        // 60 m² × 2,5 m = 150 m³ (abgeleitet), das Obergeschoss trägt 300 m³ selbst; Σ 450 m³.
        Assert.Contains("150 m³", Zeilen(cut)[0].TextContent);
        Assert.Contains("ja", Zeilen(cut)[0].TextContent);
        Assert.Contains("300 m³", Zeilen(cut)[1].TextContent);
        Assert.Contains("nein", Zeilen(cut)[1].TextContent);
        Assert.Contains("450 m³", cut.Find("table.epos-zonenliste tfoot").TextContent);
    }

    [Fact]
    public void Die_Hinweise_der_Kopplung_stehen_unter_der_Liste()
    {
        var cut = Aufbauen(new Weg { Hinweise = new[] { "Die Hülle der Zone „Erdgeschoss“ ist nicht geschlossen." } }, ZweiGekoppelt());

        Assert.Contains("Die Hülle der Zone „Erdgeschoss“ ist nicht geschlossen.", cut.Markup);
    }

    // =================================================================================
    // Rückfragen: Zone entfernen (Festlegung 8), Zone duplizieren (Festlegung 9)
    // =================================================================================

    /// <summary>
    /// Festlegung 8: Die Rückfrage nennt die Trennflächen, die auf die Zone zeigen, und die Luftströme;
    /// Ja setzt die Bauteile sichtbar auf „unbeheizt" und nimmt die Luftströme mit, Nein bricht ab.
    /// </summary>
    [Fact]
    public void Entfernen_einer_Nachbarzone_nennt_Trennflaechen_und_Luftstroeme()
    {
        var weg = new Weg { Luftstroeme = new[] { new ZonenluftstromDaten { Id = 5, IdZoneA = 1, IdZoneB = 2, Volumenstrom = 50 } } };
        var cut = Aufbauen(weg, ZweiGekoppelt());

        Knoepfe(cut, "Zone entfernen")[1].Click();
        string frage = cut.Find(".epos-rueckfrage-text").TextContent;
        Assert.Contains("Obergeschoss", frage);
        Assert.Contains("Decke EG/OG (Erdgeschoss)", frage);
        Assert.Contains("unbeheizten Raum", frage);
        Assert.Contains("Ihre Luftströme (1) entfallen.", frage);

        Antwort(cut, "Nein").Click();
        Assert.Equal(2, cut.Instance.ZonenImArbeitsstand.Count);
        Assert.Single(cut.Instance.LuftstroemeImArbeitsstand);

        Knoepfe(cut, "Zone entfernen")[1].Click();
        Antwort(cut, "Ja").Click();
        ZoneDaten eg = Assert.Single(cut.Instance.ZonenImArbeitsstand);
        BauteilDaten decke = eg.Bauteile.Single(b => b.Bezeichner == "Decke EG/OG");
        Assert.Equal(DbWerte.RANDBEDINGUNG_UNBEHEIZT, decke.Randbedingung);
        Assert.Null(decke.IdNachbarzone);
        Assert.Empty(cut.Instance.LuftstroemeImArbeitsstand);

        Ok(cut);
        ZonenstandDaten s = Assert.Single(weg.Geschrieben);
        Assert.NotNull(s.Luftstroeme);
        Assert.Empty(s.Luftstroeme!);
    }

    /// <summary>Festlegung 9: Eine Zone mit eigenen Trennflächen dupliziert erst nach der Rückfrage; die Kopie behält den Nachbarn.</summary>
    [Fact]
    public void Duplizieren_mit_Trennflaechen_fragt_und_behaelt_den_Nachbarn()
    {
        var cut = Aufbauen(new Weg(), ZweiGekoppelt());

        Knoepfe(cut, "Duplizieren")[0].Click();
        Assert.True(cut.Instance.NachfrageOffen);
        Assert.Contains("unverändertem Nachbarn (Obergeschoss)", cut.Instance.Nachfragetext);

        Antwort(cut, "Nein").Click();
        Assert.Equal(2, cut.Instance.ZonenImArbeitsstand.Count);

        Knoepfe(cut, "Duplizieren")[0].Click();
        Antwort(cut, "Ja").Click();
        Assert.Equal(3, cut.Instance.ZonenImArbeitsstand.Count);
        BauteilDaten kopie = cut.Instance.ZonenImArbeitsstand[1].Bauteile.Single(b => b.Bezeichner == "Decke EG/OG");
        Assert.Equal(2, kopie.IdNachbarzone);
        Assert.Equal(DbWerte.RANDBEDINGUNG_ZONE, kopie.Randbedingung);
    }

    // =================================================================================
    // Luftaustausch zwischen Zonen
    // =================================================================================

    [Fact]
    public void Luftaustausch_steht_erst_ab_zwei_Zonen()
    {
        var cut = Aufbauen(new Weg(), new[] { Zone(1, "Erdgeschoss", 60) });
        Assert.Empty(Knoepfe(cut, "Luftaustausch …"));

        cut = Aufbauen(new Weg(), ZweiGekoppelt());
        Assert.Single(Knoepfe(cut, "Luftaustausch …"));
    }

    /// <summary>
    /// Der Luftaustausch schreibt im OK-Weg des Editors: sein OK legt die Luftströme in den Arbeitsstand,
    /// das OK des Editors prüft und schreibt sie mit den Zonen (geändert = nicht null).
    /// </summary>
    [Fact]
    public void Der_Luftaustausch_schreibt_im_OK_Weg_des_Editors()
    {
        var weg = new Weg();
        var cut = Aufbauen(weg, ZweiGekoppelt());

        Knoepfe(cut, "Luftaustausch …").Single().Click();
        Assert.True(cut.Instance.LuftaustauschOffen);
        Knoepfe(cut, "+ Neuer Luftstrom").Single().Click();
        IElement zeile = cut.Find(".epos-luftstrom");
        zeile.QuerySelectorAll("select")[0].Change("1");
        cut.Find(".epos-luftstrom").QuerySelectorAll("select")[1].Change("2");
        cut.Find(".epos-luftstrom input").Input("60");
        cut.FindAll(".epos-luftaustauschdialog > .epos-leiste button.epos-knopf--primaer").Single().Click();

        Assert.False(cut.Instance.LuftaustauschOffen);
        ZonenluftstromDaten l = Assert.Single(cut.Instance.LuftstroemeImArbeitsstand);
        Assert.Equal(60.0, l.Volumenstrom);
        Assert.Contains("Luftströme zwischen den Zonen: 1.", cut.Markup);

        Ok(cut);
        ZonenstandDaten gepr = Assert.Single(weg.Geprueft);
        Assert.Single(gepr.Luftstroeme!);
        Assert.Equal(20.0, gepr.SollTagGebaeude);
        ZonenstandDaten s = Assert.Single(weg.Geschrieben);
        Assert.Equal(2, s.Zonen.Count);
        Assert.Equal(1, Assert.Single(s.Luftstroeme!).IdZoneA);
    }

    /// <summary>Esc kaskadiert: Solange der Luftaustausch steht, schließt Esc nur ihn; sein Abbrechen lässt den Arbeitsstand.</summary>
    [Fact]
    public void Esc_schliesst_erst_den_Luftaustausch()
    {
        var weg = new Weg { Luftstroeme = new[] { new ZonenluftstromDaten { Id = 5, IdZoneA = 1, IdZoneB = 2, Volumenstrom = 50 } } };
        bool geschlossen = false;
        var cut = Render<GebaeudeKatalogDialog>(p => p
            .Add(x => x.Daten, Satz())
            .Add(x => x.Modus, GebaeudeKatalogModus.Projekt)
            .Add(x => x.Gebaeudetypen, () => new[] { "Einfamilienhaus" })
            .Add(x => x.Gebaeudearten, () => new[] { "Hotel" })
            .Add(x => x.Baualtersklassen, new[] { "a", "b", "c", "d", "e" })
            .Add(x => x.Speichern, (d, neu, name) => new GebaeudeKatalogErgebnis(true, ""))
            .Add(x => x.Geschlossen, _ => geschlossen = true)
            .Add(x => x.Zonen, weg.Zonenweg(ZweiGekoppelt())));
        cut.FindAll("button[role=tab]").First(b => b.TextContent.Trim() == "Zonen").Click();

        Knoepfe(cut, "Luftaustausch …").Single().Click();
        cut.Find(".epos-luftstrom input").Input("99");
        cut.Find(".epos-luftaustauschdialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(cut.Instance.LuftaustauschOffen);
        Assert.False(geschlossen);
        Assert.Equal(50.0, Assert.Single(cut.Instance.LuftstroemeImArbeitsstand).Volumenstrom);

        // Unverändert schreibt der OK-Weg die Luftströme nicht mit (null = sie bleiben stehen).
        Knoepfe(cut, "Öffnen…")[0].Click();
        cut.Find(".epos-zonendialog label.epos-feld input").Input("EG");
        cut.FindAll(".epos-zonendialog > .epos-leiste button.epos-knopf--primaer").Single().Click();
        Ok(cut);
        Assert.Null(Assert.Single(weg.Geschrieben).Luftstroeme);
    }

    [Fact]
    public void Ohne_Schemaschritt_ist_der_Luftaustausch_weich_gesperrt()
    {
        var cut = Aufbauen(new Weg { KopplungSperre = "Schemaschritt 147 fehlt." }, new[] { Zone(1, "Erdgeschoss", 60), Zone(2, "Obergeschoss", 90) });

        IElement knopf = Knoepfe(cut, "Luftaustausch …").Single();
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Equal("Schemaschritt 147 fehlt.", knopf.GetAttribute("title"));
        knopf.Click();

        Assert.False(cut.Instance.LuftaustauschOffen);
        Assert.Equal("Schemaschritt 147 fehlt.", cut.Instance.Meldung);
    }

    /// <summary>Ohne Schemaschritt S-G (etwa iOS) bietet der Bauteildialog keine Nachbarzone an.</summary>
    [Fact]
    public void Ohne_Schemaschritt_bietet_der_Bauteildialog_keine_Nachbarzone()
    {
        var cut = Aufbauen(new Weg { KopplungSperre = "Schemaschritt 147 fehlt." }, new[] { Zone(1, "Erdgeschoss", 60), Zone(2, "Obergeschoss", 90) });

        Knoepfe(cut, "Öffnen…")[0].Click();
        Knoepfe(cut, "+ Neues Bauteil …").Single().Click();

        Assert.DoesNotContain("Nachbarzone", Feld(cut.Find(".epos-bauteildialog"), "Randbedingung")!.TextContent);
        Assert.Contains("Schemaschritt 147 fehlt.", cut.Find(".epos-bauteildialog").TextContent);
    }
}
