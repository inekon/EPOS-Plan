using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Überlagerung „Auslegung" des Zapfprofils samt Konstruktor (Umsetzungskonzept
/// Zapfprofilgenerator 5.1, 5.7; Mockup Abschnitt 3; Stufe Z2, Gruppe 2): drei Karten, der EINE
/// empfohlene Punkt, der Verfahrensvergleich nachrichtlich, die Warnliste, die benannt
/// gesperrten Elemente (Perzentil, Übergabe), die Neuberechnung je Eingabe, die Rückfrage beim
/// Stundenprofil, der Konstruktor als Unterüberlagerung und der Rückweg (OK mit DTO, Abbrechen
/// mit <c>null</c>) — dazu der Fall ohne Gaben.
///
/// <para>Die Auslegung kommt aus einem Prüfdelegaten — der Dialog rechnet nicht; die Entprellung
/// steht auf 0, außer im Fall, der sie selbst prüft. Kultur de-DE, alle Zahlen erfunden.</para>
/// </summary>
public class ZapfprofilAuslegungDialogTests : EposBunitContext
{
    public ZapfprofilAuslegungDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =================================================================================
    // Prüfdaten (erfunden)
    // =================================================================================

    /// <summary>Eine gerechnete Speichergruppe: Punkt aus dem Speicher-°C, Vergleich mit drei Verfahren.</summary>
    private static ZapfprofilAuslegungsgruppeDaten Speichergruppe(double volumenL = 300, bool mitWoche = false) => new()
    {
        Topologie = "Speicher-Ladesystem",
        Speicher = true,
        Zonen = { "Zone 1", "Zone 2" },
        Bedarfstag = "Tag A",
        BedarfstagWahl = "Vorgaberegel: Tag A",
        SpeicherC = 60,
        SpeicherCHerkunft = "Parameter",
        Hauptwert = new ZapfprofilKarteDaten { Stand = ZapfprofilKartenstand.Gerechnet, VolumenL = volumenL, LeistungKw = 25, Empfohlen = true },
        Normvergleich = new ZapfprofilKarteDaten { Stand = ZapfprofilKartenstand.Gerechnet, VolumenL = 400 },
        Empfehlung = new ZapfprofilEmpfehlungDaten { Rechenbar = true, Speicher = true, VolumenL = volumenL, LeistungKw = 25, NenninhaltL = 400, Schnellauslegung = true },
        LadezeitH = 2,
        ZeitkonstanteMin = 30,
        KennzahlN = 12,
        Vergleich = new ZapfprofilVergleichDaten
        {
            Verfahren =
            {
                new ZapfprofilVerfahrenDaten { Verfahren = "profilbasiert", VolumenL = null, Gueltig = true, Kennwert = "D_max = 0", Rechenweg = "Defizit" },
                new ZapfprofilVerfahrenDaten { Verfahren = "Normverfahren", VolumenL = 380, Gueltig = true, Groesster = true, Kennwert = "N = 12", Rechenweg = "Norm" },
                new ZapfprofilVerfahrenDaten { Verfahren = "Faustwert", VolumenL = 500, Nachrichtlich = true, Kennwert = "l je Person", Rechenweg = "Faust" }
            },
            BandMinL = 300,
            BandMaxL = 400,
            NenninhaltL = 400,
            KennzahlN = 12,
            LadeleistungKw = 25,
            Personen = 30,
            Nutzanteil = 0.9,
            Zuschlag = 0.1,
            ProfilbasiertVorhanden = false,
            Zeitpunkt = "Zeitpunkt des Minimums: Montag 7 Uhr",
            WochenModell = mitWoche ? Woche() : null
        },
        Warnliste =
        {
            new ZapfprofilWarnDaten("ZPG_AUSHINW_A", "Warnung A", "Satz A.", ZapfprofilWarnstufe.Warnung),
            new ZapfprofilWarnDaten("ZPG_AUSHINW_B", "Hinweis B", "Satz B.", ZapfprofilWarnstufe.Hinweis)
        }
    };

    /// <summary>Ein Wochenbild aus dem Kern (168 Stunden, synthetisch).</summary>
    private static Zeichenmodell Woche()
    {
        double[] z = Enumerable.Range(0, 168).Select(h => h % 24 == 7 ? 20.0 : 2.0).ToArray();
        double[] zirk = Enumerable.Repeat(1.0, 168).ToArray();
        double[] ladung = Enumerable.Repeat(5.0, 168).ToArray();
        double[] defizit = new double[168];
        double[] fuellstand = Enumerable.Repeat(10.0, 168).ToArray();
        return ZapfprofilBilder.AuslegungswocheModell(z, zirk, ladung, defizit, fuellstand, 300, 8, null);
    }

    private static ZapfprofilAuslegungDaten Ergebnis(params ZapfprofilAuslegungsgruppeDaten[] gruppen)
    {
        var d = new ZapfprofilAuslegungDaten { Zustand = ZapfprofilAuslegungZustand.Gerechnet, Status = "gerechnet" };
        d.Gruppen.AddRange(gruppen);
        return d;
    }

    private static ZapfprofilAuslegungStartDaten Start(ZapfprofilAuslegungDaten? ergebnis = null, ZapfprofilAuslegungEingabeDaten? eingabe = null) => new()
    {
        Kontext = "Summe aller Zonen · 2 Zonen · Stufe Einfach",
        Eingabe = eingabe ?? new ZapfprofilAuslegungEingabeDaten { SpeicherC = 60, ErzeugerKw = 25 },
        Bedarfstage =
        {
            new ZapfprofilBedarfstagDaten { Id = 5, Bezeichner = "Tag A", Herkunft = "Testquelle", Quelle = ZapfprofilBedarfstagquelle.Konstruktor },
            new ZapfprofilBedarfstagDaten { Id = 6, Bezeichner = "Normtag", Herkunft = "Norm", Quelle = ZapfprofilBedarfstagquelle.Din4708Profil,
                                            Waehlbar = false, Sperrgrund = "Das DIN-4708-Profil rechnet der Kern aus der Kennzahl." }
        },
        Regeln = { new ZapfprofilRegelDaten("Dusche", 8, 5, 40), new ZapfprofilRegelDaten("Wanne", 12, 10, 40) },
        NameVorschlag = "Eigener Tag",
        Verfuegbar = true,
        Ergebnis = ergebnis ?? Ergebnis(Speichergruppe())
    };

    private IRenderedComponent<ZapfprofilAuslegungDialog> Aufbauen(
        ZapfprofilAuslegungStartDaten? daten = null,
        Func<ZapfprofilAuslegungEingabeDaten, ZapfprofilAuslegungDaten>? rechnen = null,
        Func<IReadOnlyList<ZapfprofilKonstruktorZeileDaten>, string, ZapfprofilKonstruktorErgebnis>? konstruieren = null,
        Action<ZapfprofilAuslegungEingabeDaten?>? geschlossen = null,
        int entprellungMs = 0)
        => Render<ZapfprofilAuslegungDialog>(p => p
            .Add(x => x.Daten, daten ?? Start())
            .Add(x => x.Texte, new ZapfprofilAuslegungTexte())
            .Add(x => x.Rechnen, rechnen)
            .Add(x => x.Konstruieren, konstruieren)
            .Add(x => x.EntprellungMs, entprellungMs)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));

    private static IElement Knopf<T>(IRenderedComponent<T> cut, string text) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static IElement Feld<T>(IRenderedComponent<T> cut, string bezeichnung, string element = "input")
        where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll("label").First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == bezeichnung).QuerySelector(element)!;

    // =================================================================================
    // Karten, Punkt, Vergleich, Warnliste
    // =================================================================================

    [Fact]
    public void Drei_Karten_ein_Punkt_der_Vergleich_und_die_Warnliste_zeichnen()
    {
        var cut = Aufbauen(Start(Ergebnis(Speichergruppe(mitWoche: true))));

        Assert.Equal("Auslegung Brauchwasser", cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.Contains("2 Zonen", cut.Find(".epos-kontextzeile").TextContent);

        // Drei Karten: Summenlinie, Perzentil, Normvergleich.
        IReadOnlyList<IElement> karten = cut.FindAll(".epos-zapfausl-karte");
        Assert.Equal(3, karten.Count);
        Assert.Contains("(a) Summenlinie", karten[0].TextContent);
        Assert.Contains("300 l", karten[0].TextContent);
        Assert.Contains("(b) Perzentil", karten[1].TextContent);
        Assert.Contains("(c) Normvergleich", karten[2].TextContent);
        Assert.Contains("400 l", karten[2].TextContent);

        // Genau EIN empfohlener Punkt, mit Nenninhalt und der Marke der Schnellauslegung.
        IElement empfehlung = Assert.Single(cut.FindAll(".epos-zapfausl-empfehlung"));
        Assert.Contains("Empfohlener Auslegungspunkt: Speicher 300 l · Leistung 25,0 kW", empfehlung.TextContent);
        Assert.Contains("400", empfehlung.TextContent);
        Assert.Equal("Schnellauslegung", empfehlung.QuerySelector(".epos-zapfausl-marke--schnell")!.TextContent);

        // Der Verfahrensvergleich steht nachrichtlich, der größte Wert markiert, „–“ bei D_max = 0.
        IElement vergleich = cut.Find(".epos-zapfausl-vergleich");
        Assert.Contains("nachrichtlich", vergleich.QuerySelector("h4")!.TextContent);
        IReadOnlyList<IElement> zeilen = cut.FindAll("table.epos-zapfausl-verfahren tbody tr");
        Assert.Equal(3, zeilen.Count);
        Assert.Equal("–", zeilen[0].QuerySelector("td")!.TextContent.Trim());
        Assert.Contains("epos-zeile--markiert", zeilen[1].ClassName);
        Assert.Contains("epos-zapfausl-nachrichtlich", zeilen[2].ClassName);
        Assert.Contains("nur nachrichtlich", zeilen[2].TextContent);
        Assert.Contains("Normverfahren · 380 l", vergleich.TextContent);
        Assert.NotNull(vergleich.QuerySelector(".epos-zapfausl-dmax"));
        Assert.Contains("Montag 7 Uhr", vergleich.QuerySelector(".epos-zapfausl-zeitpunkt")!.TextContent);
        Assert.NotNull(vergleich.QuerySelector("svg"));   // das Wochenbild als DiagrammSvg

        // Die Warnliste: je Eintrag Kennung, Stufe und Satz.
        IReadOnlyList<IElement> warnungen = cut.FindAll("ul.epos-zapfausl-warnliste li");
        Assert.Equal(new[] { "ZPG_AUSHINW_A", "ZPG_AUSHINW_B" }, warnungen.Select(w => w.GetAttribute("data-kennung")).ToArray());
        Assert.NotNull(warnungen[0].QuerySelector(".epos-zapfausl-stufe--warnung"));
        Assert.Null(warnungen[1].QuerySelector(".epos-zapfausl-stufe--warnung"));
        Assert.Contains("Warnung A:", warnungen[0].TextContent);
        Assert.Contains("Satz B.", warnungen[1].TextContent);

        // Die Statuszeile der Fußleiste nennt den Punkt.
        cut.WaitForAssertion(() => Assert.Contains("Punkt gewählt: Speicher 300 l · Leistung 25,0 kW", cut.Markup));
    }

    [Fact]
    public void Perzentil_und_Uebergabe_sind_benannt_gesperrt()
    {
        var cut = Aufbauen();

        IElement perzentil = cut.Find(".epos-zapfausl-perzentil");
        Assert.Equal("true", perzentil.GetAttribute("aria-disabled"));
        Assert.Equal("In dieser Fassung noch nicht verfügbar.", perzentil.GetAttribute("title"));
        Assert.StartsWith("noch nicht gerechnet", perzentil.TextContent);

        IElement uebergabe = Knopf(cut, "An Speicherauslegung übergeben…");
        Assert.Equal("true", uebergabe.GetAttribute("aria-disabled"));
        Assert.False(uebergabe.HasAttribute("disabled"));
        Assert.Equal("Die Übergabe an die Speicherauslegung kommt mit einer späteren Fassung.", uebergabe.GetAttribute("title"));

        uebergabe.Click();
        Assert.Equal("Die Übergabe an die Speicherauslegung kommt mit einer späteren Fassung.", cut.Instance.Hinweis);
        Assert.Contains("kommt mit einer späteren Fassung", cut.Find(".epos-zapfausl-hinweis").TextContent);
    }

    [Fact]
    public void Ein_gesperrter_Katalogtag_steht_mit_Grund_in_der_Wahl()
    {
        var cut = Aufbauen();

        IElement wahl = Feld(cut, "Bedarfstag", "select");
        string[] eintraege = wahl.QuerySelectorAll("option").Select(o => o.TextContent).ToArray();
        Assert.StartsWith("Vorgaberegel", eintraege[0]);
        Assert.StartsWith("Stundenprofil", eintraege[1]);
        Assert.Contains("Tag A · Testquelle", eintraege);

        IElement normtag = wahl.QuerySelectorAll("option").First(o => o.TextContent.StartsWith("Normtag"));
        Assert.True(normtag.HasAttribute("disabled"));
        Assert.Equal("Das DIN-4708-Profil rechnet der Kern aus der Kennzahl.", normtag.GetAttribute("title"));
    }

    [Fact]
    public void A100_Referenzprofil_und_Ecodesign_stehen_benannt_gesperrt()
    {
        var cut = Aufbauen(rechnen: _ => Ergebnis(Speichergruppe()));

        IElement wahl = Feld(cut, "Bedarfstag", "select");
        IElement a100 = wahl.QuerySelectorAll("option").Single(o => o.TextContent.StartsWith("A100-Referenzprofil"));
        Assert.True(a100.HasAttribute("disabled"));
        Assert.Equal("Die Katalogzeilen der Art A100-Referenzprofil folgen mit dem Katalogpaket.", a100.GetAttribute("title"));
        IElement eco = wahl.QuerySelectorAll("option").Single(o => o.TextContent.StartsWith("Ecodesign-Zapfprofil"));
        Assert.True(eco.HasAttribute("disabled"));
        Assert.Equal("Die Katalogzeile des Ecodesign-Zapfprofils folgt mit dem Katalogpaket.", eco.GetAttribute("title"));
        // Jeder gesperrte Eintrag trägt SEINEN Grund.
        Assert.Equal("Das DIN-4708-Profil rechnet der Kern aus der Kennzahl.",
                     wahl.QuerySelectorAll("option").First(o => o.TextContent.StartsWith("Normtag")).GetAttribute("title"));

        // Eine Wahl kommt nicht an: Die Quelle bleibt.
        wahl.Change("-2");
        Assert.Equal(ZapfprofilBedarfstagquelle.Vorgaberegel, cut.Instance.Eingabe.Quelle);
        Assert.Null(cut.Instance.Eingabe.IdBedarfstag);

        // Führt der Katalog eine Zeile der Art A100, steht sie als Katalogtag da und die Sperrzeile entfällt.
        ZapfprofilAuslegungStartDaten mitA100 = Start();
        mitA100.Bedarfstage.Add(new ZapfprofilBedarfstagDaten { Id = 9, Bezeichner = "Referenztag", Herkunft = "Katalog",
                                                               Quelle = ZapfprofilBedarfstagquelle.A100Referenz });
        var mit = Aufbauen(mitA100);
        string[] texte = Feld(mit, "Bedarfstag", "select").QuerySelectorAll("option").Select(o => o.TextContent).ToArray();
        Assert.Contains("Referenztag · Katalog", texte);
        Assert.DoesNotContain(texte, t => t.StartsWith("A100-Referenzprofil"));
        Assert.Contains(texte, t => t.StartsWith("Ecodesign-Zapfprofil"));

        // Ebenso das Ecodesign-Zapfprofil (N11 (e), Stufe Z3): Führt der Katalog eine Zeile der Art 5,
        // steht sie als wählbarer Katalogtag da, und die Sperrzeile entfällt.
        ZapfprofilAuslegungStartDaten mitEco = Start();
        mitEco.Bedarfstage.Add(new ZapfprofilBedarfstagDaten { Id = 11, Bezeichner = "Ecodesign-Zapfprofil L", Herkunft = "frei",
                                                              Quelle = ZapfprofilBedarfstagquelle.Ecodesign });
        var eco2 = Aufbauen(mitEco);
        IElement wahl2 = Feld(eco2, "Bedarfstag", "select");
        IElement katalogtag = wahl2.QuerySelectorAll("option").Single(o => o.TextContent == "Ecodesign-Zapfprofil L · frei");
        Assert.False(katalogtag.HasAttribute("disabled"));
        Assert.DoesNotContain(wahl2.QuerySelectorAll("option"), o => o.HasAttribute("disabled")
                                                                    && o.TextContent.StartsWith("Ecodesign-Zapfprofil"));
        Assert.Contains(wahl2.QuerySelectorAll("option"), o => o.TextContent.StartsWith("A100-Referenzprofil"));
    }

    [Fact]
    public void Rohrnetzspitze_und_Konsistenzhinweis_stehen_benannt_gesperrt()
    {
        var durchfluss = new ZapfprofilAuslegungsgruppeDaten
        {
            Topologie = "Durchfluss",
            Zonen = { "Zone 3" },
            Hauptwert = new ZapfprofilKarteDaten { Stand = ZapfprofilKartenstand.Gerechnet },
            Empfehlung = new ZapfprofilEmpfehlungDaten { Rechenbar = true, LeistungKw = 30 }
        };
        var cut = Aufbauen(Start(Ergebnis(Speichergruppe(), durchfluss)));

        // DIN 1988-300 nachrichtlich in jeder Karte (c), gesperrt mit Grund.
        IReadOnlyList<IElement> rohrnetz = cut.FindAll(".epos-zapfausl-rohrnetz");
        Assert.Equal(2, rohrnetz.Count);
        Assert.All(rohrnetz, r =>
        {
            Assert.Equal("true", r.GetAttribute("aria-disabled"));
            Assert.Contains("DIN 1988-300", r.TextContent);
            Assert.Contains("Summe der Entnahmearmaturen", r.GetAttribute("title"));
        });
        Assert.NotNull(cut.FindAll(".epos-zapfausl-karte--norm")[0].QuerySelector(".epos-zapfausl-rohrnetz"));

        // Der Konsistenzhinweis gehört zum Summenlinienpunkt — nur in der Speichergruppe.
        IElement konsistenz = Assert.Single(cut.FindAll(".epos-zapfausl-konsistenz"));
        Assert.Equal("true", konsistenz.GetAttribute("aria-disabled"));
        Assert.Contains("braucht das Perzentil", konsistenz.TextContent);
        Assert.NotNull(cut.FindAll(".epos-zapfausl-warnungen")[0].QuerySelector(".epos-zapfausl-konsistenz"));
    }

    [Fact]
    public void Ohne_Ergebnis_steht_der_Grund_und_es_gibt_keinen_Punkt()
    {
        var ohne = new ZapfprofilAuslegungDaten { Zustand = ZapfprofilAuslegungZustand.NichtGerechnet, Grund = "Keine Zone" };
        ZapfprofilAuslegungEingabeDaten? ergebnis = null;
        var cut = Aufbauen(Start(ohne), geschlossen: e => ergebnis = e);

        Assert.Equal("Keine Auslegung — Keine Zone", cut.Find(".epos-zapfausl-ohne").TextContent);
        Assert.Empty(cut.FindAll(".epos-zapfausl-karte"));
        Assert.Empty(cut.FindAll(".epos-zapfausl-empfehlung"));

        Knopf(cut, "OK").Click();
        Assert.NotNull(ergebnis);
        Assert.Null(ergebnis!.PunktVolumenL);
        Assert.Null(ergebnis.PunktLeistungKw);
        Assert.Equal(60, ergebnis.SpeicherC);
    }

    [Fact]
    public void Eine_Durchflussgruppe_zeigt_die_Minutenspitze_und_ohne_Punkt_den_Grund()
    {
        var durchfluss = new ZapfprofilAuslegungsgruppeDaten
        {
            Topologie = "Durchfluss",
            Zonen = { "Zone 1" },
            Hauptwert = new ZapfprofilKarteDaten { Stand = ZapfprofilKartenstand.NichtRechenbar, Text = "Werkstoff fehlt" },
            Normvergleich = new ZapfprofilKarteDaten { Stand = ZapfprofilKartenstand.AusserhalbGueltigkeit, Text = "nur Wohnen" },
            Empfehlung = new ZapfprofilEmpfehlungDaten { Rechenbar = false, Grund = "Werkstoff fehlt" }
        };
        var cut = Aufbauen(Start(Ergebnis(durchfluss)));

        IReadOnlyList<IElement> karten = cut.FindAll(".epos-zapfausl-karte");
        Assert.Contains("(a) Minutenspitze", karten[0].TextContent);
        Assert.Contains("Werkstoff fehlt", karten[0].QuerySelector(".epos-zapfausl-grund")!.TextContent);
        Assert.Contains("nur Wohnen", karten[2].QuerySelector(".epos-zapfausl-grund")!.TextContent);
        Assert.Equal("Kein Auslegungspunkt — Werkstoff fehlt", cut.Find(".epos-zapfausl-empfehlung").TextContent.Trim());
        Assert.Empty(cut.FindAll(".epos-zapfausl-vergleich"));
        Assert.Contains("Keine Hinweise.", cut.Find(".epos-zapfausl-warnungen").TextContent);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_und_OK_gibt_die_Eingaben()
    {
        ZapfprofilAuslegungEingabeDaten? ergebnis = null;
        var cut = Render<ZapfprofilAuslegungDialog>(p => p
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.Geschlossen, e => ergebnis = e));

        Assert.NotNull(cut.Find(".epos-zapfausl-ohne"));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Bedarfstag konstruieren…");

        // Eine Eingabe ohne Rechendelegat rechnet nicht, stürzt nicht.
        Feld(cut, "Speichertemperatur").Input("55");
        Knopf(cut, "OK").Click();
        Assert.NotNull(ergebnis);
        Assert.Equal(55, ergebnis!.SpeicherC);
        Assert.Null(ergebnis.PunktVolumenL);
    }

    // =================================================================================
    // Neuberechnung
    // =================================================================================

    [Fact]
    public void Jede_Eingabe_rechnet_neu_ueber_den_Delegaten()
    {
        var gerechnet = new List<ZapfprofilAuslegungEingabeDaten>();
        ZapfprofilAuslegungDaten Rechnen(ZapfprofilAuslegungEingabeDaten e)
        {
            gerechnet.Add(e);
            return Ergebnis(Speichergruppe(volumenL: (e.SpeicherC ?? 60) * 5));
        }
        var cut = Aufbauen(rechnen: Rechnen);
        Assert.Empty(gerechnet);   // beim Öffnen gilt das mitgebrachte Ergebnis

        Feld(cut, "Speichertemperatur").Input("70");
        cut.WaitForAssertion(() => Assert.Contains("Speicher 350 l", cut.Find(".epos-zapfausl-empfehlung").TextContent));
        Assert.Equal(70, gerechnet.Last().SpeicherC);

        Feld(cut, "Erzeugerart am Speicher", "select").Change("2");
        cut.WaitForAssertion(() => Assert.Equal(ZapfprofilErzeugerart.Waermepumpe, gerechnet.Last().Erzeugerart));

        cut.FindAll("label.epos-option").First(l => l.TextContent.Contains("gemischter Speicher")).QuerySelector("input")!.Change("2");
        cut.WaitForAssertion(() => Assert.Equal(ZapfprofilSpeicherart.GemischterSpeicher, gerechnet.Last().Speicherart));

        Feld(cut, "Bedarfstag", "select").Change("5");
        cut.WaitForAssertion(() => Assert.Equal(5, gerechnet.Last().IdBedarfstag));
        Assert.Equal(4, gerechnet.Count);
        Assert.Equal(ZapfprofilBedarfstagquelle.Konstruktor, gerechnet.Last().Quelle);
    }

    [Fact]
    public void Entprellt_rechnet_eine_Folge_von_Eingaben_einmal()
    {
        int gerechnet = 0;
        var cut = Aufbauen(rechnen: e => { gerechnet++; return Ergebnis(Speichergruppe(volumenL: (e.SpeicherC ?? 0) * 5)); },
                           entprellungMs: 150);

        IElement feld = Feld(cut, "Speichertemperatur");
        feld.Input("6");
        Feld(cut, "Speichertemperatur").Input("65");
        Assert.Equal(0, gerechnet);

        cut.WaitForAssertion(() => Assert.Contains("Speicher 325 l", cut.Find(".epos-zapfausl-empfehlung").TextContent),
                             TimeSpan.FromSeconds(5));
        Assert.Equal(1, gerechnet);
    }

    // =================================================================================
    // Rückweg
    // =================================================================================

    [Fact]
    public void OK_uebernimmt_Eingaben_und_Punkt_und_laesst_den_Wirt_unberuehrt()
    {
        ZapfprofilAuslegungStartDaten daten = Start();
        ZapfprofilAuslegungEingabeDaten? ergebnis = null;
        var cut = Aufbauen(daten, rechnen: _ => Ergebnis(Speichergruppe(volumenL: 320)), geschlossen: e => ergebnis = e);

        Feld(cut, "Speichertemperatur").Input("62");
        Feld(cut, "Werkstoff des Übertragers", "select").Change("1");
        Knopf(cut, "OK").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal(62, ergebnis!.SpeicherC);
        Assert.Equal(ZapfprofilWerkstoff.Stahl, ergebnis.Werkstoff);
        Assert.Equal(320, ergebnis.PunktVolumenL);
        Assert.Equal(25, ergebnis.PunktLeistungKw);
        Assert.Equal(60, daten.Eingabe.SpeicherC);   // Arbeitsstand auf Kopien
        Assert.Null(daten.Eingabe.PunktVolumenL);
    }

    [Fact]
    public void Abbrechen_Esc_und_Kreuz_verwerfen_mit_null()
    {
        int gerufen = 0;
        ZapfprofilAuslegungEingabeDaten? ergebnis = new();
        var cut = Aufbauen(geschlossen: e => { gerufen++; ergebnis = e; });

        Knopf(cut, "Abbrechen").Click();
        Assert.Equal(1, gerufen);
        Assert.Null(ergebnis);

        cut.Find(".epos-zapfausl").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(2, gerufen);

        cut.Find(".epos-dialog-zu").Click();
        Assert.Equal(3, gerufen);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Eingebettet_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Render<ZapfprofilAuslegungDialog>(p => p.Add(x => x.Daten, Start()).Add(x => x.TitelAnzeigen, false));

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        Assert.NotNull(cut.Find(".epos-dialog-kopf--ohnetitel"));
    }

    [Fact]
    public void Das_Stundenprofil_fragt_vor_dem_OK_nach()
    {
        int gerufen = 0;
        ZapfprofilAuslegungEingabeDaten? ergebnis = null;
        var cut = Aufbauen(rechnen: _ => Ergebnis(Speichergruppe()), geschlossen: e => { gerufen++; ergebnis = e; });

        Feld(cut, "Bedarfstag", "select").Change("-1");
        Assert.Equal(ZapfprofilBedarfstagquelle.Stundenprofil, cut.Instance.Eingabe.Quelle);

        Knopf(cut, "OK").Click();
        Assert.True(cut.Instance.RueckfrageOffen);
        Assert.Equal(0, gerufen);
        Assert.Contains("Spitzen unter einer Stunde sind unterschätzt", cut.Markup);

        Knopf(cut, "Zurück").Click();
        Assert.False(cut.Instance.RueckfrageOffen);
        Assert.Equal(0, gerufen);

        Knopf(cut, "OK").Click();
        Knopf(cut, "Übernehmen").Click();
        Assert.Equal(1, gerufen);
        Assert.Equal(ZapfprofilBedarfstagquelle.Stundenprofil, ergebnis!.Quelle);
        Assert.Equal(300, ergebnis.PunktVolumenL);
    }

    // =================================================================================
    // Konstruktor
    // =================================================================================

    /// <summary>Der Konstruktor wie die Hülle: der Tag samt den Zeilen, aus denen er entstand.</summary>
    private static ZapfprofilKonstruktorErgebnis Bauen(IReadOnlyList<ZapfprofilKonstruktorZeileDaten> zeilen, string name)
        => new(new ZapfprofilBedarfstagDaten
        {
            Id = 0,
            Bezeichner = name,
            Quelle = ZapfprofilBedarfstagquelle.Konstruktor,
            Herkunft = "Eigenkonstruktion",
            Ereignisse = zeilen.Select((z, i) => new ZapfprofilEreignisDaten((int)((z.BeginnH ?? 0) * 60), 10, 1.0 + i)).ToList(),
            Konstruktorzeilen = zeilen.Select(z => z.Kopie()).ToList()
        }, Array.Empty<ZapfprofilMeldung>());

    [Fact]
    public void Der_Konstruktor_liefert_den_Tag_als_Entwurf_der_Auslegung()
    {
        var gerechnet = new List<ZapfprofilAuslegungEingabeDaten>();
        var cut = Aufbauen(rechnen: e => { gerechnet.Add(e); return Ergebnis(Speichergruppe()); }, konstruieren: Bauen);

        Knopf(cut, "Bedarfstag konstruieren…").Click();
        Assert.True(cut.Instance.KonstruktorOffen);

        IRenderedComponent<BedarfstagKonstruktor> k = cut.FindComponent<BedarfstagKonstruktor>();
        Assert.Equal("Eigener Tag", Feld(k, "Name des Bedarfstags").GetAttribute("value"));
        Assert.Single(k.Instance.Zeilen);
        Assert.Equal("Dusche", k.Instance.Zeilen[0].Regel);

        Knopf(k, "Zeile hinzufügen").Click();
        Assert.Equal(2, k.Instance.Zeilen.Count);
        Feld(k, "Name des Bedarfstags").Input("Tag Neu");
        Knopf(k, "OK").Click();

        Assert.False(cut.Instance.KonstruktorOffen);
        Assert.Equal("Tag Neu", cut.Instance.Eingabe.Entwurf!.Bezeichner);
        Assert.Equal(2, cut.Instance.Eingabe.Entwurf.Ereignisse.Count);
        Assert.Equal(ZapfprofilBedarfstagquelle.Konstruktor, cut.Instance.Eingabe.Quelle);
        Assert.Null(cut.Instance.Eingabe.IdBedarfstag);
        Assert.Equal("Tag Neu", gerechnet.Last().Entwurf!.Bezeichner);

        IElement wahl = Feld(cut, "Bedarfstag", "select");
        IElement gewaehlt = wahl.QuerySelectorAll("option").Single(o => o.HasAttribute("selected"));
        Assert.StartsWith("Tag Neu (konstruiert", gewaehlt.TextContent);
    }

    [Fact]
    public void Eine_Ablehnung_des_Konstruktors_steht_als_Banner_und_er_bleibt_offen()
    {
        var meldung = new ZapfprofilMeldung("ZPG_AUS_KON_ZEITFENSTER", "Zeile 1", "Zeile 1: Das Zeitfenster ist leer.", ZapfprofilMeldungsart.Ablehnung);
        var cut = Aufbauen(konstruieren: (_, _) => new ZapfprofilKonstruktorErgebnis(null, new[] { meldung }));

        Knopf(cut, "Bedarfstag konstruieren…").Click();
        IRenderedComponent<BedarfstagKonstruktor> k = cut.FindComponent<BedarfstagKonstruktor>();

        // Die letzte Zeile lässt sich nicht entfernen — weich gesperrt, der Versuch nennt den Grund.
        IElement entfernen = Knopf(k, "Entfernen");
        Assert.Equal("true", entfernen.GetAttribute("aria-disabled"));
        Assert.False(entfernen.HasAttribute("disabled"));
        Assert.Equal("Die letzte Zeile bleibt — ein Bedarfstag braucht mindestens eine Zeile.", entfernen.GetAttribute("title"));
        entfernen.Click();
        Assert.Single(k.Instance.Zeilen);
        Assert.Contains("mindestens eine Zeile", k.Find(".epos-zapfausl-konstruktorhinweis").TextContent);

        Knopf(k, "OK").Click();
        Assert.True(cut.Instance.KonstruktorOffen);
        Assert.Contains("Das Zeitfenster ist leer.", k.Markup);
        Assert.Single(k.Instance.Meldungen);
        Assert.Null(cut.Instance.Eingabe.Entwurf);
    }

    /// <summary>Eine Speichergruppe, der die Vorgaberegel keinen Tag liefert (Kern: KonstruktorOeffnen).</summary>
    private static ZapfprofilAuslegungsgruppeDaten OhneTag()
    {
        ZapfprofilAuslegungsgruppeDaten g = Speichergruppe();
        g.Bedarfstag = "";
        g.KonstruktorOeffnen = true;
        g.Hauptwert = new ZapfprofilKarteDaten { Stand = ZapfprofilKartenstand.NichtRechenbar, Text = "Bedarfstag fehlt" };
        g.Empfehlung = new ZapfprofilEmpfehlungDaten { Rechenbar = false, Grund = "Bedarfstag fehlt" };
        g.Vergleich = null;
        return g;
    }

    [Fact]
    public void Ohne_konstruierten_Tag_oeffnet_die_Auslegung_den_Konstruktor()
    {
        bool mitTag = false;
        var cut = Aufbauen(Start(Ergebnis(OhneTag())), rechnen: _ => Ergebnis(mitTag ? Speichergruppe() : OhneTag()),
                           konstruieren: Bauen);

        // Beim Öffnen steht der Konstruktor über der Auslegung (Regel 4.5).
        Assert.True(cut.Instance.KonstruktorOffen);
        Knopf(cut.FindComponent<BedarfstagKonstruktor>(), "Abbrechen").Click();
        Assert.False(cut.Instance.KonstruktorOffen);

        // Dieselbe Lage nach einer Neuberechnung öffnet ihn nicht nach jedem Abbrechen erneut …
        Feld(cut, "Speichertemperatur").Input("62");
        Assert.False(cut.Instance.KonstruktorOffen);

        // … führt eine Neuberechnung den Fall neu herbei, öffnet er wieder.
        mitTag = true;
        Feld(cut, "Speichertemperatur").Input("63");
        Assert.False(cut.Instance.KonstruktorOffen);
        mitTag = false;
        Feld(cut, "Speichertemperatur").Input("64");
        Assert.True(cut.Instance.KonstruktorOffen);
    }

    [Fact]
    public void Ohne_Konstruktor_Delegat_bleibt_es_beim_Eintrag_der_Warnliste()
    {
        var cut = Aufbauen(Start(Ergebnis(OhneTag())));
        Assert.False(cut.Instance.KonstruktorOffen);
        Assert.Empty(cut.FindComponents<BedarfstagKonstruktor>());
    }

    [Fact]
    public void Ein_Tag_aus_dem_Stundenprofil_traegt_das_Warnbanner()
    {
        ZapfprofilAuslegungsgruppeDaten stunde = Speichergruppe();
        stunde.SpitzenUnterschaetzt = true;
        var cut = Aufbauen(Start(Ergebnis(stunde, Speichergruppe())));

        IElement banner = Assert.Single(cut.FindAll(".epos-warnbanner"),
                                        b => b.TextContent.Contains("Spitzen unterschätzt"));
        Assert.Contains("Zapfspitzen unter einer Stunde", banner.TextContent);
        Assert.Single(cut.FindAll(".epos-warnbanner-text"), b => b.TextContent.StartsWith("Spitzen unterschätzt"));
    }

    [Fact]
    public void Erneut_geoeffnet_beginnt_der_Konstruktor_mit_den_Zeilen_des_Entwurfs()
    {
        var cut = Aufbauen(rechnen: _ => Ergebnis(Speichergruppe()), konstruieren: Bauen);
        Knopf(cut, "Bedarfstag konstruieren…").Click();
        IRenderedComponent<BedarfstagKonstruktor> k = cut.FindComponent<BedarfstagKonstruktor>();
        Knopf(k, "Zeile hinzufügen").Click();
        Feld(k, "Beginn [h], Zeile 2").Input("18");
        Feld(k, "Ende [h], Zeile 2").Input("19");
        Feld(k, "Name des Bedarfstags").Input("Tag Neu");
        Knopf(k, "OK").Click();
        Assert.Equal(2, cut.Instance.Eingabe.Entwurf!.Konstruktorzeilen.Count);

        Knopf(cut, "Bedarfstag konstruieren…").Click();
        k = cut.FindComponent<BedarfstagKonstruktor>();
        Assert.Equal(2, k.Instance.Zeilen.Count);
        Assert.Equal(18, k.Instance.Zeilen[1].BeginnH);
        Assert.Equal("Dusche", k.Instance.Zeilen[0].Regel);
        Assert.Equal("Tag Neu", Feld(k, "Name des Bedarfstags").GetAttribute("value"));

        // Die Zeilen sind Kopien: Abbrechen lässt den Entwurf, wie er war.
        Feld(k, "Beginn [h], Zeile 2").Input("20");
        Knopf(k, "Abbrechen").Click();
        Assert.Equal(18, cut.Instance.Eingabe.Entwurf!.Konstruktorzeilen[1].BeginnH);
    }

    [Fact]
    public void Eine_Fehleingabe_im_Konstruktor_wird_mit_Feld_und_Zeile_genannt()
    {
        int gebaut = 0;
        var cut = Aufbauen(konstruieren: (z, n) => { gebaut++; return Bauen(z, n); });
        Knopf(cut, "Bedarfstag konstruieren…").Click();
        IRenderedComponent<BedarfstagKonstruktor> k = cut.FindComponent<BedarfstagKonstruktor>();

        // Jedes Feld trägt Spalte und Zeile als Beschriftung.
        string[] beschriftungen = k.FindAll("table.epos-zapfausl-konstruktorzeilen .epos-feld-text").Select(s => s.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Beginn [h], Zeile 1", "Ende [h], Zeile 1", "Zapfregel, Zeile 1", "Anzahl, Zeile 1", "Volumen [l], Zeile 1",
                             "Zapftemperatur [°C], Zeile 1", "Verbraucher, Zeile 1" }, beschriftungen);

        Feld(k, "Beginn [h], Zeile 1").Input("7,x");
        Knopf(k, "OK").Click();
        Assert.Equal(0, gebaut);
        Assert.True(cut.Instance.KonstruktorOffen);
        Assert.Equal("ZPG_AUS_KON_FEHLEINGABE", Assert.Single(k.Instance.Meldungen).Kennung);
        Assert.Contains("Keine gültige Zahl: Beginn [h], Zeile 1", k.Markup);

        // Berichtigt baut der Delegat den Tag.
        Feld(k, "Beginn [h], Zeile 1").Input("7");
        Feld(k, "Ende [h], Zeile 1").Input("8");
        Knopf(k, "OK").Click();
        Assert.Equal(1, gebaut);
        Assert.False(cut.Instance.KonstruktorOffen);
    }

    [Fact]
    public void Abbrechen_und_Esc_im_Konstruktor_schliessen_nur_ihn()
    {
        int gerufen = 0;
        var cut = Aufbauen(konstruieren: Bauen, geschlossen: _ => gerufen++);

        Knopf(cut, "Bedarfstag konstruieren…").Click();
        IRenderedComponent<BedarfstagKonstruktor> k = cut.FindComponent<BedarfstagKonstruktor>();
        k.Find(".epos-zapfausl-konstruktor").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(cut.Instance.KonstruktorOffen);

        Knopf(cut, "Bedarfstag konstruieren…").Click();
        k = cut.FindComponent<BedarfstagKonstruktor>();
        Knopf(k, "Abbrechen").Click();
        Assert.False(cut.Instance.KonstruktorOffen);

        Assert.Equal(0, gerufen);
        Assert.Null(cut.Instance.Eingabe.Entwurf);
    }
}
