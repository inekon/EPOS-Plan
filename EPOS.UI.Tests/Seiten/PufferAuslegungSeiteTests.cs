using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Pufferspeicher;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>Die Ansicht „Pufferspeicher-Auslegung"</b> (Konzept Pufferspeicher-Auslegung, Abschnitt 6,
/// Stufe P2) mit einem Prüfstand statt der Hülle: vier Schritte mit Vorbelegung und Herkunft, der
/// Vorlagenwechsel ändert die Kriterienkarten und rechnet neu, die Tiefe blendet Karten und
/// Expertenfelder, das Ergebnis zeigt Zonen, Empfehlung und Warncodes, die Übernahme ruft die
/// Hülle mit Ändern bzw. Neuanlegen, der Fehlertext der Reihen steht benannt da, und die Ansicht
/// zeichnet ohne Gaben. Deutsch über die Kulturvorrichtung; Englisch in
/// <see cref="PufferAuslegungSeiteEnglischTests"/>.
/// </summary>
public class PufferAuslegungSeiteTests : EposBunitContext
{
    public PufferAuslegungSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =============================================================================
    //  Prüfstand
    // =============================================================================

    /// <summary>Der Ersatz der Hülle: rechnet je Vorlage ein festes Ergebnis und merkt jeden Aufruf.</summary>
    internal sealed class Pruefstand
    {
        internal readonly List<PufferAuslegungEingabeDaten> Gerechnet = new();
        internal readonly List<PufferAuslegungEingabeDaten> Gespeichert = new();
        internal readonly List<PufferUebernahmeDaten> Uebernommen = new();
        internal double Empfehlung = 3000;

        internal PufferAuslegungErgebnisDaten Rechnen(PufferAuslegungEingabeDaten e)
        {
            Gerechnet.Add(e.Kopie());
            return Ergebnis(e, Empfehlung);
        }

        internal PufferAuslegungDienste Dienste() => new(
            Rechnen,
            e => { Gespeichert.Add(e.Kopie()); return null; },
            (e, u) =>
            {
                Uebernommen.Add(u);
                return new PufferUebernahmeErgebnis(true, "Übernommen: „" + (u.Neu ? u.Bezeichner : "Speicher 1") + "“ mit 3.000 l.",
                                                    u.Neu ? 99 : 7);
            });
    }

    /// <summary>Ein Ergebnis wie aus dem Kern: WP mit Sperrzeit bemessend, BHKW mit Verschiebedauer.</summary>
    internal static PufferAuslegungErgebnisDaten Ergebnis(PufferAuslegungEingabeDaten e, double empfehlung)
    {
        bool bhkw = e.Vorlage == "BHKW";
        var kriterien = new List<PufferKriteriumDaten>
        {
            new("K1", "Vorprüfung Anlagenvolumen", null, true, true, "VDI 4645 E 2026-03, 7.8.3", "3 l/kW · 60 kW = 180 l"),
            new("K2", "Faustwert nach Gerätetyp", 1200, !bhkw, true, "VDI 4645 7.8.4", "20 l/kW · 60 kW = 1 200 l"),
            new("K3", "Mindestlaufzeit", 304, true, true, "VDI 4645 Gl. 22", "18 kW · 10 min / (1,16 · 10 K · 0,85) = 304 l"),
            new("K4", "Sperrzeit", 2428, !bhkw, true, "VDI 4645 Gl. 23", "82 kW · (2 h − 1 h) · 1 000 / (1,16 · 25 K) − 400 l = 2 428 l"),
            new("K4e", "Sperrzeit aus dem Lastgang", 16630, false, true, "Tool C.6.3", "82 kW · 2 h · 1 000 / (1,16 · 10 K · 0,85) = 16 630 l"),
            new("KV", "Verschiebedauer BHKW", 2800, bhkw, true, "Setzung", "2 h · 50 kW")
        };
        var zonen = new List<PufferZoneDaten>
        {
            new("Heizung", "Heizzone", bhkw ? "KV" : "K4", bhkw ? "Verschiebedauer BHKW" : "Sperrzeit",
                bhkw ? 2800 : 2428, false, kriterien,
                new PufferBetriebsbildDaten(3000, 5.3, 1219, 5520, 1.0, 1.2))
        };
        if (e.KlasseBrauchwasser)
            zonen.Add(new("Brauchwasser", "Brauchwasserzone", "B-Frischwasser", "Brauchwasser am Puffer (Station)", 700, false,
                          new[] { new PufferKriteriumDaten("B-Frischwasser", "Brauchwasser am Puffer (Station)", 700, true, true,
                                                           "VDI 4645 Anhang I", "24 kWh · 1,15 / (1,16 · 40 K · 0,85) = 700 l") },
                          null));
        return new PufferAuslegungErgebnisDaten
        {
            Zustand = PufferErgebnisZustand.Gerechnet,
            Zonen = zonen,
            SummeL = zonen.Sum(z => z.VolumenL),
            EmpfehlungL = empfehlung,
            Bemessend = "Heizzone: Sperrzeit",
            Katalogvorschlag = "Speicher 3000 · 3.000 l",
            Kennzahlen = new PufferKennzahlDaten
            {
                Nutzanteil = 0.85, SpreizungK = 10, KapazitaetKwh = 29.6, VerlustKwhJeTag = 5.32, VerlustWJeK = 4.92,
                VerlustKwhJeJahr = 1515, BandMinL = 2050, BandMaxL = 3690, FaustwertL = 1200, StartsJeTag = 5.3,
                StartsHeizperiode = 1219
            },
            Warnungen = new[]
            {
                new PufferWarnungDaten("PA-UEBERGABE-UNBEKANNT", false,
                    "Keine Übergabeart am Gebäude: Die Heizzone rechnet wie Flächenheizung.",
                    "Keine Übergabeart am Gebäude: Die Heizzone rechnet wie Flächenheizung.", "VDI 4645 Tab. 14", "Heizzone"),
                new PufferWarnungDaten("PA-STARTS-TAG", true, "Die Starts je Tag liegen über der Warnschwelle.",
                    "16 Starts je Tag über der Warnschwelle 15.", "Fraunhofer ISE WP-QS", "Heizzone")
            }
        };
    }

    private static readonly IReadOnlyDictionary<string, bool> SCHALTER_WP = new Dictionary<string, bool>
    {
        ["K1"] = true, ["K2"] = true, ["K3"] = true, ["K4"] = true, ["D1"] = false,
        ["D2"] = true, ["K9"] = false, ["K10"] = false, ["KV"] = false
    };

    private static readonly IReadOnlyDictionary<string, bool> SCHALTER_BHKW = new Dictionary<string, bool>
    {
        ["K1"] = false, ["K2"] = false, ["K3"] = true, ["K4"] = false, ["D1"] = false,
        ["D2"] = true, ["K9"] = false, ["K10"] = false, ["KV"] = true
    };

    /// <summary>Ein Startstand wie aus der Hülle: Projekt 1045, Speicher 1, Wärmepumpe an Rang 1.</summary>
    internal static PufferAuslegungStartDaten Start(Pruefstand stand, int? idPuffer = 7, string reihenFehler = "")
    {
        var eingabe = new PufferAuslegungEingabeDaten
        {
            KlasseHeizung = true, Vorlage = "WP_MONO", HeizgrenzeC = 15, Uebergabeart = "",
            Sperrprofil = PufferSperrprofilWert.ZweiMalZwei
        };
        var d = new PufferAuslegungStartDaten
        {
            IdProjekt = 1045,
            IdPuffer = idPuffer,
            Projektname = "Beispielprojekt",
            PufferBezeichner = idPuffer.HasValue ? "Speicher 1" : "",
            Einstieg = "geöffnet aus ① Konfiguration",
            Eingabe = eingabe,
            Herkunft = new[]
            {
                new PufferHerkunftDaten("Klassen", "Puffer", "Puffer", "Klassen-Set des Puffers"),
                new PufferHerkunftDaten("Vorlage", "Kaskade", "Kaskade", "Wärmepumpe ohne Zweiterzeuger"),
                new PufferHerkunftDaten("HeizgrenzeC", "Projekt", "Projekt", "Tab_Einstellungen.Kessel_Heizgrenze"),
                new PufferHerkunftDaten("Erzeuger", "Kaskade", "Kaskade", "Nennleistung 60 kW; Zweiterzeuger 0 kW")
            },
            Nutzungsprofil = "Wohnen",
            NutzungsprofilHerkunft = "Zapf-Nutzungsart „Mehrfamilienhaus“",
            Vorlagen = new[]
            {
                new PufferVorlageDaten("WP_MONO", "Wärmepumpe", "monovalent", "Die Wärmepumpe deckt allein.", SCHALTER_WP, "Faustwert 20 l/kW"),
                new PufferVorlageDaten("BHKW", "BHKW", "lange Laufzeiten", "Mindestlaufzeit und wenige Starts.", SCHALTER_BHKW, "")
            },
            Uebergabearten = new[]
            {
                new PufferWahlDaten("", "keine Angabe (rechnet wie Flächenheizung)"),
                new PufferWahlDaten("RADIATOR", "Heizkörper"),
                new PufferWahlDaten("FLAECHE", "Flächenheizung")
            },
            Zirkulationswege = new[] { new PufferWahlDaten("", "automatisch"), new PufferWahlDaten("JE_WE", "je Wohneinheit") },
            Reihen = reihenFehler.Length > 0 ? Array.Empty<PufferReiheDaten>() : new[]
            {
                new PufferReiheDaten("Heizung", 82, 182000),
                new PufferReiheDaten("Brauchwasser", 26, 39000),
                new PufferReiheDaten("Prozess", 0, 0)
            },
            ReihenFehler = reihenFehler,
            Erzeuger = new[] { "Nennleistung 60 kW; Zweiterzeuger 0 kW" },
            IstWaermepumpe = true,
            VorlaufC = 55, RuecklaufC = 45, SchwelleEin = 0.10, SchwelleAus = 0.95,
            ZapfVorhanden = true, ZapfTopologie = "Frischwasser", ZapfDmaxKwh = 24, ZapfPersonen = 40, ZapfTagesbedarfL = 1600,
            BezeichnerMuster = "Pufferspeicher {0} l"
        };
        d.Ergebnis = reihenFehler.Length > 0
            ? PufferAuslegungErgebnisDaten.MitFehler("Die Bedarfsreihen sind nicht rechenbar: " + reihenFehler)
            : Ergebnis(eingabe, stand.Empfehlung);
        return d;
    }

    private int _geschlossen;

    private IRenderedComponent<PufferAuslegungSeite> Zeige(Pruefstand stand, PufferAuslegungStartDaten? daten = null)
        => Render<PufferAuslegungSeite>(p => p
            .Add(x => x.Daten, daten ?? Start(stand))
            .Add(x => x.Dienste, stand.Dienste())
            .Add(x => x.Geschlossen, () => _geschlossen++));

    private static void Schritt(IRenderedComponent<PufferAuslegungSeite> cut, int nummer)
        => cut.FindAll(".epos-ablaufleiste-schritt button")[nummer - 1].Click();

    private static void Stufe(IRenderedComponent<PufferAuslegungSeite> cut, PufferAuslegungStufe stufe)
        => cut.FindAll(".epos-pausl-leiste input[type=radio]")[(int)stufe].Change("");

    private static IReadOnlyList<string> Karten(IRenderedComponent<PufferAuslegungSeite> cut)
        => cut.FindAll("section.epos-pausl-karte").Select(k => k.GetAttribute("data-kriterium")!).ToList();

    // =============================================================================
    //  Schritte und Vorbelegung
    // =============================================================================

    [Fact]
    public void Die_Ansicht_zeigt_vier_Schritte_und_die_Vorbelegung_mit_Herkunft()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand);

        Assert.Equal("Pufferspeicher-Auslegung", cut.Find("h1.epos-seite-titel").TextContent);
        Assert.Contains("Projekt „Beispielprojekt“ · Speicher „Speicher 1“", cut.Find(".epos-pausl-kontext").TextContent);
        var schritte = cut.FindAll(".epos-ablaufleiste-schritt .epos-ablaufleiste-titel").Select(s => s.TextContent).ToList();
        Assert.Equal(new[] { "Klasse und Erzeuger", "Bedarf und Randbedingungen", "Kriterien", "Ergebnis und Übernahme" }, schritte);
        Assert.Equal(PufferAuslegungSchritt.Anlage, cut.Instance.Schritt);

        // Schritt 1: Klassen aus dem Puffer, Vorlage aus der Kaskade, Nutzungsprofil als Anzeige.
        var klassen = cut.FindAll(".epos-pausl-klassen input[type=checkbox]");
        Assert.Equal(3, klassen.Count);
        Assert.True(klassen[0].HasAttribute("checked"));
        Assert.False(klassen[1].HasAttribute("checked"));
        Assert.Equal("Puffer", cut.Find(".epos-pausl-marke[data-feld=Klassen]").TextContent);
        Assert.Equal("Kaskade", cut.Find(".epos-pausl-marke[data-feld=Vorlage]").TextContent);
        Assert.Equal("true", cut.Find("[data-vorlage=WP_MONO]").GetAttribute("aria-checked"));
        Assert.Contains("Wohnen", cut.Find(".epos-pausl-nutzungsprofil").TextContent);
        Assert.Contains("Fixed-Speed", cut.Find(".epos-pausl-geraetetyp").TextContent);
        Assert.Contains("Nennleistung 60 kW", cut.Find(".epos-pausl-erzeuger").TextContent);

        // Schritt 2: Reihen mit Spitze und Jahressumme, Sperrprofil vorbelegt, Zapfprofil-Zone.
        Schritt(cut, 2);
        Assert.Equal(PufferAuslegungSchritt.Bedarf, cut.Instance.Schritt);
        var reihen = cut.FindAll(".epos-pausl-reihen tbody tr");
        Assert.Equal(3, reihen.Count);
        Assert.Contains("82,0 kW", reihen[0].TextContent);
        Assert.Contains("182,00 MWh", reihen[0].TextContent);
        Assert.Contains("2 × 2 h", cut.Markup);

        // Ohne Klasse Brauchwasser keine Zapfprofil-Zone; mit ihr steht sie da.
        Assert.Empty(cut.FindAll(".epos-pausl-topologie"));
        Schritt(cut, 1);
        cut.FindAll(".epos-pausl-klassen input[type=checkbox]")[1].Change(true);
        Schritt(cut, 2);
        Assert.Equal("Frischwasserstation", cut.Find(".epos-pausl-topologie").TextContent);
        Assert.Contains("24,0 kWh", cut.Markup);
    }

    [Fact]
    public void Der_Vorlagenwechsel_aendert_die_Kriterienkarten_und_rechnet_neu()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand);
        Stufe(cut, PufferAuslegungStufe.Schnell);

        Schritt(cut, 3);
        Assert.Equal(new[] { "K1", "K2", "K3", "K4", "D2" }, Karten(cut));
        Assert.Contains("2.428 l", cut.Find("section[data-kriterium=K4] .epos-pausl-karte-wert").TextContent);
        Assert.Contains("epos-pausl-karte--bemessend", cut.Find("section[data-kriterium=K4]").ClassName);

        Schritt(cut, 1);
        cut.Find("[data-vorlage=BHKW]").Click();
        Assert.Equal("BHKW", stand.Gerechnet.Last().Vorlage);
        Assert.Equal("überschrieben", cut.Find(".epos-pausl-marke[data-feld=Vorlage]").TextContent);
        Assert.Empty(cut.FindAll(".epos-pausl-geraetetyp"));

        Schritt(cut, 3);
        Assert.Equal(new[] { "K3", "D2", "KV" }, Karten(cut));
        Assert.Contains("epos-pausl-karte--bemessend", cut.Find("section[data-kriterium=KV]").ClassName);
    }

    [Fact]
    public void Ein_Kriterienschalter_weicht_von_der_Vorlage_ab_und_rechnet_neu()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand);
        Schritt(cut, 3);

        // Standard zeigt alle neun Karten, abgeschaltete gedämpft.
        Assert.Equal(9, Karten(cut).Count);
        Assert.Contains("epos-pausl-karte--aus", cut.Find("section[data-kriterium=D1]").ClassName);

        cut.Find("section[data-kriterium=D1] input[type=checkbox]").Change(true);
        Assert.True(stand.Gerechnet.Last().Kriterien["D1"]);
        Assert.Equal("überschrieben", cut.Find("section[data-kriterium=D1] .epos-pausl-marke").TextContent);

        // Zurück auf den Vorlagenwert: keine Abweichung mehr.
        cut.Find("section[data-kriterium=D1] input[type=checkbox]").Change(false);
        Assert.Empty(stand.Gerechnet.Last().Kriterien);
    }

    [Fact]
    public void Die_Stufe_Schnell_blendet_Expertenfelder_und_Rechenwege_aus()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand);
        Schritt(cut, 2);

        // Standard: Heizlast, Anlagenvolumen und Ziele, aber keine Expertenfelder.
        Assert.Contains("Auslegungsheizlast", cut.Markup);
        Assert.Contains("Mindestlaufzeit je Start", cut.Markup);
        Assert.DoesNotContain("Kleinste Dauerleistung", cut.Markup);
        Assert.DoesNotContain("Sperrzeit aus dem Lastgang bemessen", cut.Markup);

        Stufe(cut, PufferAuslegungStufe.Schnell);
        Assert.DoesNotContain("Auslegungsheizlast", cut.Markup);
        Assert.DoesNotContain("Mindestlaufzeit je Start", cut.Markup);
        Assert.Contains("Heizgrenze", cut.Markup);
        Assert.Contains("Sperrprofil", cut.Markup);

        Stufe(cut, PufferAuslegungStufe.Experte);
        Assert.Contains("Kleinste Dauerleistung", cut.Markup);
        Assert.Contains("Sperrzeit aus dem Lastgang bemessen (Expertenweg)", cut.Markup);

        // Schritt 3: Rechenweg ab Standard, die Expertenzeile K4e nur bei Experte.
        Schritt(cut, 3);
        Assert.NotEmpty(cut.FindAll("[data-kriterium=K4e]"));
        Stufe(cut, PufferAuslegungStufe.Standard);
        Assert.Empty(cut.FindAll("[data-kriterium=K4e]"));
        Assert.NotEmpty(cut.FindAll(".epos-pausl-rechenweg"));
        Stufe(cut, PufferAuslegungStufe.Schnell);
        Assert.Empty(cut.FindAll(".epos-pausl-rechenweg"));
    }

    [Fact]
    public void Ein_Zahlenfeld_markiert_das_Ergebnis_als_veraltet_und_Schritt_4_rechnet_nach()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand);
        int vorher = stand.Gerechnet.Count;
        Schritt(cut, 2);

        cut.FindAll("input[inputmode=decimal]")[0].Input("12");   // Heizgrenze
        Assert.True(cut.Instance.Veraltet);
        Assert.Equal(vorher, stand.Gerechnet.Count);
        Assert.Equal("überschrieben", cut.Find(".epos-pausl-marke[data-feld=HeizgrenzeC]").TextContent);
        Assert.NotEmpty(cut.FindAll(".epos-pausl-veraltet"));

        Schritt(cut, 4);
        Assert.False(cut.Instance.Veraltet);
        Assert.Equal(12, stand.Gerechnet.Last().HeizgrenzeC);
    }

    // =============================================================================
    //  Ergebnis und Übernahme
    // =============================================================================

    [Fact]
    public void Das_Ergebnis_zeigt_Zonen_Empfehlung_Kennzahlen_und_Warncodes()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand);
        Schritt(cut, 4);

        IElement zone = cut.Find(".epos-pausl-zonen tr[data-zone=Heizung]");
        Assert.Contains("Heizzone", zone.TextContent);
        Assert.Contains("Sperrzeit", zone.TextContent);
        Assert.Contains("2.428 l", zone.TextContent);
        Assert.Contains("3.000 l · 29,6 kWh nutzbar", cut.Find(".epos-pausl-empfehlung").TextContent);
        Assert.Contains("Speicher 3000", cut.Find(".epos-pausl-katalog").TextContent);

        Assert.Contains("5,3", cut.Find(".epos-pausl-kz-starts-tag").TextContent);
        Assert.Contains("1.219", cut.Find(".epos-pausl-kz-starts-jahr").TextContent);
        Assert.Contains("5,32 kWh/d · 4,92 W/K", cut.Find(".epos-pausl-kz-verlust").TextContent);
        Assert.Contains("1.515 kWh/a", cut.Find(".epos-pausl-kz-betriebsverlust").TextContent);
        Assert.Contains("Plausibilitätsband nach Übergabeart: 2.050–3.690 l", cut.Markup);

        var codes = cut.FindAll(".epos-pausl-warnungen tbody tr").Select(z => z.GetAttribute("data-code")).ToList();
        Assert.Equal(new[] { "PA-UEBERGABE-UNBEKANNT", "PA-STARTS-TAG" }, codes);
        IElement warnung = cut.Find(".epos-pausl-warnungen tr[data-code=PA-STARTS-TAG]");
        Assert.Contains("Warnung", warnung.TextContent);
        Assert.Contains("Fraunhofer ISE WP-QS", warnung.TextContent);
        Assert.Equal("16 Starts je Tag über der Warnschwelle 15.", warnung.QuerySelectorAll("td")[2].GetAttribute("title"));
        Assert.Contains("Schwellen und Temperaturpaar bleiben", cut.Find(".epos-pausl-bleibt").TextContent);
    }

    [Fact]
    public void Die_Uebernahme_aendert_den_Speicher_oder_legt_einen_neuen_an()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand);
        Schritt(cut, 4);

        // Vorgewählt: den Speicher ändern.
        cut.Find("button.epos-pausl-uebernehmen").Click();
        PufferUebernahmeDaten erste = Assert.Single(stand.Uebernommen);
        Assert.False(erste.Neu);
        Assert.Equal("", erste.Bezeichner);
        Assert.Contains("Übernommen", cut.Instance.Meldung);

        // Neu anlegen mit eigener Bezeichnung.
        var ziele = cut.FindAll(".epos-pausl-schritt--ergebnis input[type=radio]");
        Assert.Equal(2, ziele.Count);
        ziele[1].Change("");
        cut.Find(".epos-pausl-schritt--ergebnis input[type=text]").Input("Kombispeicher Nord");
        cut.Find("button.epos-pausl-uebernehmen").Click();
        Assert.Equal(2, stand.Uebernommen.Count);
        Assert.True(stand.Uebernommen[1].Neu);
        Assert.Equal("Kombispeicher Nord", stand.Uebernommen[1].Bezeichner);
        Assert.Contains("„Kombispeicher Nord“ ändern", cut.Markup);

        // Speichern ruft den eigenen Weg.
        cut.Find("button.epos-pausl-speichern").Click();
        Assert.Single(stand.Gespeichert);
        Assert.Equal("Auslegung gespeichert.", cut.Instance.Meldung);
    }

    [Fact]
    public void Ohne_Puffer_legt_die_Uebernahme_mit_dem_Namensvorschlag_neu_an()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand, Start(stand, idPuffer: null));
        Assert.Contains("neuer Pufferspeicher", cut.Find(".epos-pausl-kontext").TextContent);
        Schritt(cut, 4);

        Assert.Empty(cut.FindAll(".epos-pausl-schritt--ergebnis input[type=radio]"));
        Assert.Equal("Pufferspeicher 3.000 l", cut.Find(".epos-pausl-schritt--ergebnis input[type=text]").GetAttribute("placeholder"));
        cut.Find("button.epos-pausl-uebernehmen").Click();
        PufferUebernahmeDaten u = Assert.Single(stand.Uebernommen);
        Assert.True(u.Neu);
        Assert.Equal("Pufferspeicher 3.000 l", u.Bezeichner);
    }

    [Fact]
    public void Ohne_Empfehlung_ist_die_Uebernahme_weich_gesperrt_und_meldet_den_Grund()
    {
        var stand = new Pruefstand { Empfehlung = 0 };
        var cut = Zeige(stand);
        Schritt(cut, 4);

        IElement knopf = cut.Find("button.epos-pausl-uebernehmen");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("disabled"));
        knopf.Click();
        Assert.Empty(stand.Uebernommen);
        Assert.Equal("Die Auslegung empfiehlt keinen Puffer — es gibt nichts zu übernehmen.", cut.Instance.Meldung);
    }

    [Fact]
    public void Der_Fehlertext_der_Reihen_steht_benannt_in_Schritt_2_und_im_Ergebnis()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand, Start(stand, reihenFehler: "Für das Projekt ist keine Klimaregion gewählt."));
        Schritt(cut, 2);
        Assert.Contains("Die Bedarfsreihen sind nicht rechenbar: Für das Projekt ist keine Klimaregion gewählt.",
                        cut.Find(".epos-pausl-schritt--bedarf").TextContent);
        Assert.Empty(cut.FindAll(".epos-pausl-reihen"));

        Schritt(cut, 1);
        Schritt(cut, 3);
        cut.Find("button.epos-pausl-rechnen").Click();
        Assert.Equal(PufferAuslegungSchritt.Ergebnis, cut.Instance.Schritt);
    }

    [Fact]
    public void Ohne_Klasse_wird_nicht_gerechnet_und_der_Grund_steht_da()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand);
        int vorher = stand.Gerechnet.Count;
        cut.FindAll(".epos-pausl-klassen input[type=checkbox]")[0].Change(false);

        Assert.Equal(vorher, stand.Gerechnet.Count);
        Assert.Equal(PufferErgebnisZustand.Fehler, cut.Instance.Ergebnis!.Zustand);
        Assert.Contains("Mindestens eine Speicherklasse wählen", cut.Markup);
    }

    [Fact]
    public void Zurueck_und_Kreuz_schliessen_die_Ansicht()
    {
        var stand = new Pruefstand();
        var cut = Zeige(stand);
        cut.Find("button.epos-pausl-zurueck").Click();
        Assert.Equal(1, _geschlossen);
        cut.Find(".epos-pausl-kopfaktionen .epos-dialog-zu").Click();
        Assert.Equal(2, _geschlossen);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_die_Ansicht_alle_vier_Schritte()
    {
        var cut = Render<PufferAuslegungSeite>();
        Assert.Equal("Pufferspeicher-Auslegung", cut.Find("h1").TextContent);
        for (int i = 1; i <= 4; i++) Schritt(cut, i);
        Assert.Contains("Mindestens eine Speicherklasse wählen", cut.Markup);
        Assert.Empty(cut.FindAll("button.epos-pausl-uebernehmen"));
    }

    [Fact]
    public void Ein_Fehler_beim_Vorbelegen_steht_statt_der_Schritte()
    {
        var stand = new Pruefstand();
        var d = new PufferAuslegungStartDaten { Fehler = "Die Auslegung kann nicht vorbelegt werden: Der Pufferspeicher 5 gehört nicht zum Projekt 1045." };
        var cut = Zeige(stand, d);
        Assert.Contains("gehört nicht zum Projekt", cut.Markup);
        Assert.Empty(cut.FindAll(".epos-ablaufleiste"));
    }
}

/// <summary>Dieselbe Ansicht unter en-US: Titel, Schritte und Marken aus <c>Resource.en-US</c>.</summary>
public class PufferAuslegungSeiteEnglischTests : EposBunitContext
{
    public PufferAuslegungSeiteEnglischTests() : base("en-US")
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    [Fact]
    public void Die_Ansicht_spricht_Englisch()
    {
        var stand = new PufferAuslegungSeiteTests.Pruefstand();
        var cut = Render<PufferAuslegungSeite>(p => p
            .Add(x => x.Daten, PufferAuslegungSeiteTests.Start(stand))
            .Add(x => x.Dienste, stand.Dienste()));

        Assert.Equal("Buffer storage sizing", cut.Find("h1").TextContent);
        var schritte = cut.FindAll(".epos-ablaufleiste-schritt .epos-ablaufleiste-titel").Select(s => s.TextContent).ToList();
        Assert.Equal(new[] { "Class and generator", "Demand and boundary conditions", "Criteria", "Result and transfer" }, schritte);
        Assert.Contains("Storage class and use", cut.Markup);
        Assert.Equal(Resource.PAUS_KLASSE_HEIZUNG, "Space heating");

        cut.FindAll(".epos-ablaufleiste-schritt button")[2].Click();
        Assert.Contains("Lock-out time", cut.Markup);
    }
}
