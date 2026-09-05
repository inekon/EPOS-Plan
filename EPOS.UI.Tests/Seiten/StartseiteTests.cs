using System.Globalization;
using System.Threading;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Start;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>NACHWEIS N3 — die Startseite</b> (iU9-W16b.2, S1 der Vermessung).
///
/// <para>Vorbild: <c>Views/Hauptformular/Form_Start</c> (2 300 Z. + 1 381 Designer,
/// 108 Kartenzeilen). Soll sind die SECHS Reiter, die <b>21 Kacheln</b>, der
/// Kachelzustand aus der Bitmaske des Kerns, die Reitersperre ohne offenes Projekt
/// und der Projektwechsel über den <c>SeitenZustand</c> — ohne Neuaufbau.</para>
///
/// <para>Die Sprache ist auf de-DE gepinnt (Regel seit iU9-W8, Muster
/// <c>GebaeudeKatalogDialogTests</c>): Die Erwartungswerte sind deutsche
/// Beschriftungen, und der Windows-Läufer läuft mit englischer Oberfläche.</para>
/// </summary>
public class StartseiteTests : BunitContext
{
    public StartseiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    // =====================================================================
    //  Probendaten - die 21 Kacheln in der Reihenfolge des Bestands
    // =====================================================================

    /// <summary>
    /// Die 21 Kacheln, wie sie die Hülle liefert. <paramref name="bitmaske"/> ist
    /// die Bitmaske des Kerns (<c>KomponentenBestandCtrl.Bitmaske</c>); die
    /// dreizehn Bestandskacheln lesen ihren Zustand daraus, die fünf
    /// Projektkacheln und der Konfigurationsknopf führen keinen.
    /// </summary>
    private static IReadOnlyList<StartKachel> Kacheln(int bitmaske = 0)
    {
        Kachelstand Stand(int bit) => (bitmaske & bit) == bit ? Kachelstand.An : Kachelstand.Aus;

        return new[]
        {
            Ohne(Kachelschluessel.ProjektNeu, Reiterschluessel.Projekt, "Neues Projekt"),
            Ohne(Kachelschluessel.ProjektOeffnen, Reiterschluessel.Projekt, "Projekt öffnen/bearbeiten"),
            Ohne(Kachelschluessel.ProjektZuletzt, Reiterschluessel.Projekt, "Zuletzt geöffnet"),
            Ohne(Kachelschluessel.ProjektSpeichernUnter, Reiterschluessel.Projekt, "Speichern unter"),
            Ohne(Kachelschluessel.ProjektLoeschen, Reiterschluessel.Projekt, "Projekt löschen"),

            Mit(Kachelschluessel.Gebaeude, Reiterschluessel.Waermebedarf, "Gebäudedaten eingeben", Stand(8)),
            Mit(Kachelschluessel.WaermebedarfDaten, Reiterschluessel.Waermebedarf, "Daten importieren", Stand(16)),
            Mit(Kachelschluessel.Prozesswaerme, Reiterschluessel.Waermebedarf, "Prozesswärme", Stand(32)),
            Mit(Kachelschluessel.Brauchwasser, Reiterschluessel.Waermebedarf, "Brauchwasserwärme", Stand(4096)),

            Mit(Kachelschluessel.StromStandardprofil, Reiterschluessel.Strombedarf, "Standardlastprofil", Stand(64)),
            Mit(Kachelschluessel.StromEigenesProfil, Reiterschluessel.Strombedarf, "Eigenes Profil", Stand(0)),
            Mit(Kachelschluessel.StromMessdaten, Reiterschluessel.Strombedarf, "Messdaten importieren", Stand(128)),

            Mit(Kachelschluessel.Waermepumpe, Reiterschluessel.Erzeuger, "Wärmepumpe", Stand(2)),
            Mit(Kachelschluessel.Heizkessel, Reiterschluessel.Erzeuger, "Heizkessel", Stand(1)),
            Mit(Kachelschluessel.Solarthermie, Reiterschluessel.Erzeuger, "Solarthermie", Stand(512)),
            Mit(Kachelschluessel.Bhkw, Reiterschluessel.Erzeuger, "BHKW", Stand(256)),
            Mit(Kachelschluessel.Photovoltaik, Reiterschluessel.Erzeuger, "Photovoltaik", Stand(1024)),
            Mit(Kachelschluessel.Stromspeicher, Reiterschluessel.Erzeuger, "Stromspeicher", Stand(4)),
            Mit(Kachelschluessel.Pufferspeicher, Reiterschluessel.Erzeuger, "Pufferspeicher", Stand(2048)),

            Ohne(Kachelschluessel.SimulationKonfiguration, Reiterschluessel.Simulation, "Simulation Konfiguration..."),
            Mit(Kachelschluessel.SimulationErgebnis, Reiterschluessel.Simulation, "Simulation", Stand(0))
        };
    }

    private static StartKachel Ohne(string schluessel, string reiter, string titel)
        => new StartKachel { Schluessel = schluessel, Reiter = reiter, Titel = titel };

    private static StartKachel Mit(string schluessel, string reiter, string titel, Kachelstand stand)
        => new StartKachel { Schluessel = schluessel, Reiter = reiter, Titel = titel, Zustand = stand };

    /// <summary>
    /// Die Vorgabe-Zusammenfassung des Reiters „Simulation". OHNE sie springt er
    /// beim Betreten auf Reiter 1 zurueck - das ist der Zustand „Klimaregion nicht
    /// gesetzt" aus <c>tabPage5_Enter</c> und hat seinen eigenen Fall.
    /// </summary>
    private static Zusammenfassung? Bereitschaft()
        => new Zusammenfassung("Referenzprojekt", "0,00 MWh/a", "0,00 MWh/a", "");

    /// <summary>Die Seite mit einem offenen Projekt (Id 1030) und leerem Bestand.</summary>
    private IRenderedComponent<Startseite> Zeige(
        int idProjekt = 1030,
        int bitmaske = 0,
        SeitenZustand? zustand = null,
        Action<string>? geklickt = null,
        Action<int>? varianteGewaehlt = null,
        Func<Zusammenfassung?>? bericht = null,
        Func<string, (bool Fehler, string Text)>? klimaSpeichern = null)
    {
        return Render<Startseite>(p => p
            .Add(x => x.Zustand, zustand)
            .Add(x => x.Kacheln, () => Kacheln(bitmaske))
            .Add(x => x.ProjektId, () => idProjekt)
            .Add(x => x.Varianten, () => new[] { (1030, "Referenzprojekt"), (1007, "Laurentiuskirche") })
            .Add(x => x.Klimaregionen, () => new[] { "München", "Berlin" })
            .Add(x => x.Klimaregion, () => "München")
            .Add(x => x.Geklickt, geklickt)
            .Add(x => x.VarianteGewaehlt, varianteGewaehlt)
            .Add(x => x.Bericht, bericht ?? Bereitschaft)
            .Add(x => x.KlimaSpeichern, klimaSpeichern));
    }

    // =====================================================================
    //  Die sechs Reiter
    // =====================================================================

    /// <summary>
    /// SECHS Reiter, in der Reihenfolge des Bestands — <c>tabPage1</c> …
    /// <c>tabPage6</c>. Der sechste ist die Seite aus W5.
    /// </summary>
    [Fact]
    public void Die_Startseite_fuehrt_sechs_Reiter()
    {
        var cut = Zeige();

        var knoepfe = cut.FindAll("[role='tab']");
        Assert.Equal(6, knoepfe.Count);

        Assert.Equal("Projekt", knoepfe[0].TextContent.Trim());
        Assert.Equal("Wärmebedarf", knoepfe[1].TextContent.Trim());
        Assert.Equal("Strombedarf", knoepfe[2].TextContent.Trim());
        Assert.Equal("Energieerzeuger", knoepfe[3].TextContent.Trim());
        Assert.Equal("Simulation", knoepfe[4].TextContent.Trim());
        Assert.Equal("Berichte & Kosten", knoepfe[5].TextContent.Trim());
    }

    /// <summary>
    /// Ohne offenes Projekt sind die Reiter 2 bis 6 GESPERRT — wörtlich
    /// <c>Form_Start_Load</c> (:80), und der Hinweis der Reitersperre steht
    /// dabei (die zwei Sätze aus <c>tabControl_Wizard_Selecting</c>).
    /// </summary>
    [Fact]
    public void Ohne_Projekt_sind_fuenf_Reiter_gesperrt_und_der_Hinweis_steht()
    {
        var cut = Zeige(idProjekt: 0);

        var knoepfe = cut.FindAll("[role='tab']");
        Assert.False(knoepfe[0].HasAttribute("disabled"));
        for (int i = 1; i < 6; i++)
            Assert.True(knoepfe[i].HasAttribute("disabled"), "Reiter " + i + " ist nicht gesperrt.");

        string banner = cut.Find(".epos-warnbanner").TextContent;
        Assert.Contains("Bitte zuerst ein Projekt auswählen!", banner, StringComparison.Ordinal);
        Assert.Contains("Projekt öffnen oder zuletzt geöffnet", banner, StringComparison.Ordinal);
    }

    /// <summary>Mit offenem Projekt ist keiner der sechs Reiter gesperrt.</summary>
    [Fact]
    public void Mit_Projekt_sind_alle_sechs_Reiter_frei()
    {
        var cut = Zeige();

        foreach (IElement knopf in cut.FindAll("[role='tab']"))
            Assert.False(knopf.HasAttribute("disabled"));

        Assert.DoesNotContain("Bitte zuerst ein Projekt auswählen!",
                              cut.Markup, StringComparison.Ordinal);
    }

    // =====================================================================
    //  Die 21 Kacheln
    // =====================================================================

    /// <summary>
    /// <b>21 Kacheln über sechs Reiter</b> — fünf, vier, drei, sieben und zwei
    /// (ein Knopf und eine Kachel). Gezählt wird über alle Reiter hinweg, denn
    /// ein ungewähltes <c>Reiterblatt</c> zeichnet gar nichts.
    /// </summary>
    [Fact]
    public void Die_Startseite_fuehrt_einundzwanzig_Kacheln()
    {
        var cut = Zeige();

        int gezaehlt = 0;
        int[] soll = { 5, 4, 3, 7, 2 };

        for (int i = 0; i < soll.Length; i++)
        {
            cut.FindAll("[role='tab']")[i].Click();

            // Reiter 5 traegt EINEN Knopf ("Simulation Konfiguration...") UND eine
            // Kachel; beide zaehlen als Kachel der Startseite.
            int kacheln = cut.FindAll(".epos-kachel").Count;
            int knoepfe = cut.FindAll(".epos-startreiter-leiste .epos-knopf").Count;

            Assert.Equal(soll[i], kacheln + knoepfe);
            gezaehlt += kacheln + knoepfe;
        }

        Assert.Equal(Kachelschluessel.Anzahl, gezaehlt);
        Assert.Equal(21, gezaehlt);
    }

    /// <summary>
    /// Ein Klick meldet den SCHLÜSSEL der Kachel — der Ersatz für die drei
    /// Bindemuster des Vorläufers (Befund W16-B19).
    /// </summary>
    [Fact]
    public void Ein_Kachelklick_meldet_ihren_Schluessel()
    {
        List<string> gemeldet = new List<string>();
        var cut = Zeige(geklickt: s => gemeldet.Add(s));

        cut.FindAll(".epos-kachel")[0].Click();
        cut.FindAll(".epos-kachel")[2].Click();

        Assert.Equal(new[] { Kachelschluessel.ProjektNeu, Kachelschluessel.ProjektZuletzt }, gemeldet);
    }

    /// <summary>Auch der Konfigurationsknopf meldet — er ist die 21. Kachel.</summary>
    [Fact]
    public void Der_Konfigurationsknopf_meldet_seinen_Schluessel()
    {
        List<string> gemeldet = new List<string>();
        var cut = Zeige(geklickt: s => gemeldet.Add(s));

        cut.FindAll("[role='tab']")[4].Click();
        cut.Find(".epos-startreiter-leiste .epos-knopf").Click();

        Assert.Equal(new[] { Kachelschluessel.SimulationKonfiguration }, gemeldet);
    }

    // =====================================================================
    //  Der Kachelzustand aus der Bitmaske
    // =====================================================================

    /// <summary>
    /// <b>Der Statuspunkt kommt aus der Bitmaske des Kerns.</b> Geprüft am
    /// Reiter „Energieerzeuger": Bit 2 (Wärmepumpe) und Bit 1024 (Photovoltaik)
    /// gesetzt, alles andere aus — grün nur an diesen zwei Kacheln, grau an den
    /// übrigen fünf. Das ist der Ersatz für die dreizehn <c>Paint</c>-Handler,
    /// die dieselbe Aussage mit je 45 Zeilen GDI+ malten.
    /// </summary>
    [Fact]
    public void Der_Kachelzustand_kommt_aus_der_Bitmaske()
    {
        var cut = Zeige(bitmaske: 2 | 1024);
        cut.FindAll("[role='tab']")[3].Click();

        var kacheln = cut.FindAll(".epos-kachel");
        Assert.Equal(7, kacheln.Count);

        // Reihenfolge: WP, Heizkessel, Solarthermie, BHKW, PV, Stromspeicher, Puffer.
        bool[] sollAn = { true, false, false, false, true, false, false };

        for (int i = 0; i < sollAn.Length; i++)
        {
            IElement punkt = kacheln[i].QuerySelector(".epos-kachel-statuspunkt")!;
            bool an = !punkt.ClassList.Contains("epos-kachel-statuspunkt--aus");
            Assert.True(an == sollAn[i], "Kachel " + i + " hat den falschen Zustand.");
        }
    }

    /// <summary>
    /// JEDE Bestandskachel trägt einen Punkt, die fünf Projektkacheln keinen —
    /// die Projektwege führen keinen Bestand, es gibt nichts zu zählen.
    /// </summary>
    [Fact]
    public void Die_Projektkacheln_tragen_keinen_Statuspunkt()
    {
        var cut = Zeige();

        foreach (IElement kachel in cut.FindAll(".epos-kachel"))
            Assert.Null(kachel.QuerySelector(".epos-kachel-statuspunkt"));

        cut.FindAll("[role='tab']")[1].Click();
        foreach (IElement kachel in cut.FindAll(".epos-kachel"))
            Assert.NotNull(kachel.QuerySelector(".epos-kachel-statuspunkt"));
    }

    // =====================================================================
    //  Der Projektwechsel ueber den SeitenZustand
    // =====================================================================

    /// <summary>
    /// <b>Der Projektwechsel läuft über den <c>SeitenZustand</c></b> — dieselbe
    /// Bauart wie in <c>BerichteKostenSeite</c> (W5.6): Die Hülle meldet, die
    /// Seite liest ihre Gaben neu, die WebView bleibt stehen.
    /// </summary>
    [Fact]
    public void Ein_Projektwechsel_ueber_den_Zustand_laedt_neu()
    {
        SeitenZustand zustand = new SeitenZustand();
        int maske = 0;
        int gelesen = 0;

        var cut = Render<Startseite>(p => p
            .Add(x => x.Zustand, zustand)
            .Add(x => x.Kacheln, () => { gelesen++; return Kacheln(maske); })
            .Add(x => x.ProjektId, () => 1030));

        Assert.Equal(1, gelesen);
        cut.FindAll("[role='tab']")[3].Click();
        Assert.All(cut.FindAll(".epos-kachel-statuspunkt"),
                   e => Assert.Contains("epos-kachel-statuspunkt--aus", e.ClassList));

        // Der Wechsel: ein anderes Projekt, ein anderer Bestand.
        maske = 2 | 1024;
        cut.InvokeAsync(() => zustand.ProjektSetzen(1007, "Laurentiuskirche"));

        Assert.True(gelesen > 1, "Die Seite hat nach dem Wechsel nicht neu gelesen.");

        var punkte = cut.FindAll(".epos-kachel-statuspunkt");
        Assert.Equal(7, punkte.Count);
        Assert.False(punkte[0].ClassList.Contains("epos-kachel-statuspunkt--aus"));
        Assert.False(punkte[4].ClassList.Contains("epos-kachel-statuspunkt--aus"));
    }

    /// <summary>
    /// Ohne offenes Projekt steht die Seite auf Reiter 1 — wörtlich der
    /// Anfangszustand von <c>Form_Start_Load</c>. Ein Wechsel auf „kein Projekt"
    /// (das offene wurde gelöscht) holt sie dorthin zurück.
    /// </summary>
    [Fact]
    public void Ohne_Projekt_steht_die_Seite_auf_Reiter_eins()
    {
        SeitenZustand zustand = new SeitenZustand();
        int id = 1030;

        var cut = Render<Startseite>(p => p
            .Add(x => x.Zustand, zustand)
            .Add(x => x.Kacheln, () => Kacheln(0))
            .Add(x => x.ProjektId, () => id));

        cut.FindAll("[role='tab']")[3].Click();
        Assert.Equal(Reiterschluessel.Erzeuger, cut.Instance.AktiverReiter);

        // SeitenZustand meldet nur eine echte Aenderung; er steht anfangs auf
        // (0, "") - deshalb erst das offene Projekt eintragen und dann leeren.
        cut.InvokeAsync(() => zustand.ProjektSetzen(1030, "Referenzprojekt"));

        id = 0;
        cut.InvokeAsync(() => zustand.ProjektSetzen(0, ""));

        Assert.Equal(Reiterschluessel.Projekt, cut.Instance.AktiverReiter);
    }

    /// <summary>
    /// Das Kopfband meldet den Variantenwechsel — wörtlich
    /// <c>comboBox_Varianten_SelectedIndexChanged</c> (:2260-2271).
    /// </summary>
    [Fact]
    public void Das_Kopfband_meldet_den_Variantenwechsel()
    {
        List<int> gemeldet = new List<int>();
        var cut = Zeige(varianteGewaehlt: id => gemeldet.Add(id));

        cut.Find("#epos-start-variante").Change("1007");

        Assert.Equal(new[] { 1007 }, gemeldet);
    }

    // =====================================================================
    //  Kopfband, Fussleiste und Meldungen
    // =====================================================================

    /// <summary>
    /// Das Statuszeichen ist der grüne Haken bei offenem Projekt und das rote
    /// Warnzeichen sonst (<c>label_ProjektStatus</c>).
    /// </summary>
    [Fact]
    public void Das_Statuszeichen_folgt_dem_offenen_Projekt()
    {
        Assert.Contains("epos-startseite-status--offen",
                        Zeige().Find(".epos-startseite-status").ClassList);
        Assert.Contains("epos-startseite-status--keins",
                        Zeige(idProjekt: 0).Find(".epos-startseite-status").ClassList);
    }

    /// <summary>
    /// „Weiter" und „Zurück" wandern durch die sechs Reiter und bleiben an den
    /// Enden stehen — wörtlich <c>btn_Weiter_Click</c> (:1568) und
    /// <c>btn_Zurueck_Click</c> (:1641).
    /// </summary>
    [Fact]
    public void Weiter_und_Zurueck_wandern_durch_die_Reiter()
    {
        var cut = Zeige();

        IElement Zurueck() => cut.FindAll(".epos-startseite-fuss .epos-knopf")[0];
        IElement Weiter() => cut.FindAll(".epos-startseite-fuss .epos-knopf")[1];

        Assert.True(Zurueck().HasAttribute("disabled"));

        Weiter().Click();
        Assert.Equal(Reiterschluessel.Waermebedarf, cut.Instance.AktiverReiter);

        Zurueck().Click();
        Assert.Equal(Reiterschluessel.Projekt, cut.Instance.AktiverReiter);

        for (int i = 0; i < 5; i++) Weiter().Click();
        Assert.Equal(Reiterschluessel.BerichteKosten, cut.Instance.AktiverReiter);
        Assert.True(Weiter().HasAttribute("disabled"));
    }

    /// <summary>
    /// Der Speicherweg der Klimaregion meldet über EIN Banner — der Ersatz für
    /// die fünf <c>MessageBox</c> von <c>btn_Speichern_Click</c>.
    /// </summary>
    [Fact]
    public void Der_Klimaspeicherweg_meldet_ueber_ein_Banner()
    {
        string gewaehlt = "";
        var cut = Zeige(klimaSpeichern: r => { gewaehlt = r; return (false, "Klimaregion gespeichert."); });

        cut.Find("#epos-start-klima").Change("Berlin");
        cut.FindAll(".epos-startseite-klima .epos-knopf")[0].Click();

        Assert.Equal("Berlin", gewaehlt);
        Assert.Contains("Klimaregion gespeichert.",
                        cut.Find(".epos-warnbanner").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Reiter „Simulation" zeigt die Projektzusammenfassung — wörtlich
    /// <c>tabPage5_Enter</c>.
    /// </summary>
    [Fact]
    public void Der_Reiter_Simulation_zeigt_die_Zusammenfassung()
    {
        var cut = Zeige(bericht: () =>
            new Zusammenfassung("Referenzprojekt", "123,45 MWh/a", "67,89 MWh/a", "Heizkessel, BHKW"));

        cut.FindAll("[role='tab']")[4].Click();

        string text = cut.Find(".epos-startzusammenfassung").TextContent;
        Assert.Contains("Referenzprojekt", text, StringComparison.Ordinal);
        Assert.Contains("123,45 MWh/a", text, StringComparison.Ordinal);
        Assert.Contains("67,89 MWh/a", text, StringComparison.Ordinal);
        Assert.Contains("Heizkessel, BHKW", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// OHNE gesetzte Klimaregion springt die Seite auf Reiter 1 zurück und
    /// meldet — wörtlich <c>tabPage5_Enter</c> (:1066-1070), nur als Banner
    /// statt als <c>MessageBox</c>.
    /// </summary>
    [Fact]
    public void Ohne_Klimaregion_springt_der_Simulationsreiter_zurueck()
    {
        var cut = Zeige(bericht: () => null);

        cut.FindAll("[role='tab']")[4].Click();

        Assert.Equal(Reiterschluessel.Projekt, cut.Instance.AktiverReiter);
        Assert.Contains("Die Klimaregion ist nicht gesetzt!",
                        cut.Find(".epos-warnbanner").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Kurzhinweis „Projekt … geöffnet!" ist ein Banner mit Selbstverfall —
    /// der Ersatz für <c>Form_Hinweis</c> (drei Sekunden, dieselbe Frist).
    /// </summary>
    [Fact]
    public void Der_Kurzhinweis_verfaellt_nach_drei_Sekunden()
    {
        TimeSpan? gefragt = null;
        TaskCompletionSource<bool> uhr = new TaskCompletionSource<bool>();

        var cut = Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Kacheln(0))
            .Add(x => x.ProjektId, () => 1030)
            .Add(x => x.Uhr, (frist, marke) => { gefragt = frist; return uhr.Task; }));

        cut.InvokeAsync(() => cut.Instance.HinweisProjektGeoeffnet("Projekt Referenzprojekt geöffnet!"));

        Assert.Contains("Projekt Referenzprojekt geöffnet!",
                        cut.Find(".epos-warnbanner").TextContent, StringComparison.Ordinal);
        Assert.Equal(TimeSpan.FromSeconds(3), gefragt);

        uhr.SetResult(true);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-warnbanner")));
    }

    // =====================================================================
    //  E-5: die zwei Simulationsansichten ohne zweites Fenster
    // =====================================================================

    /// <summary>
    /// <b>Entscheid E-5, erste Hälfte.</b> Die SIMULATIONSKONFIGURATION löst die
    /// Startseite ab — eine freie Ansicht in derselben WebView statt eines
    /// zweiten Fensters (der Entscheid R-W10b-1 ist damit geschlossen). Der
    /// Kachelklick meldet sich dabei NICHT mehr über <c>Geklickt</c>.
    /// </summary>
    [Fact]
    public void Die_Simulationskonfiguration_loest_die_Startseite_ab()
    {
        List<string> gemeldet = new List<string>();
        int gefragt = 0;

        var cut = Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Kacheln(0))
            .Add(x => x.ProjektId, () => 1030)
            .Add(x => x.Bericht, Bereitschaft)
            .Add(x => x.Geklickt, sch => gemeldet.Add(sch))
            .Add(x => x.SimulationKonfigGaben, () =>
            {
                gefragt++;
                return new Dictionary<string, object> { ["StartProjekt"] = 1030 };
            }));

        cut.FindAll("[role='tab']")[4].Click();
        cut.Find(".epos-startreiter-leiste .epos-knopf").Click();

        Assert.Equal(1, gefragt);
        Assert.Empty(gemeldet);

        // Die Startseite ist weg - kein Kopfband, keine Reiterleiste mehr.
        Assert.Empty(cut.FindAll(".epos-startseite"));
        Assert.Empty(cut.FindAll("[role='tab']"));
    }

    /// <summary>
    /// <b>Entscheid E-5, zweite Hälfte.</b> Das SIMULATIONSERGEBNIS erscheint als
    /// <c>Ueberlagerung</c> — modal, aber in derselben WebView (R-W11-1
    /// geschlossen). Die Startseite bleibt darunter stehen.
    /// </summary>
    [Fact]
    public void Das_Simulationsergebnis_erscheint_als_Ueberlagerung()
    {
        List<string> gemeldet = new List<string>();
        int gefragt = 0;

        var cut = Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Kacheln(0))
            .Add(x => x.ProjektId, () => 1030)
            .Add(x => x.Bericht, Bereitschaft)
            .Add(x => x.Geklickt, sch => gemeldet.Add(sch))
            .Add(x => x.SimulationErgebnisGaben, () =>
            {
                gefragt++;
                return new Dictionary<string, object> { ["StartProjekt"] = 1030 };
            }));

        cut.FindAll("[role='tab']")[4].Click();
        cut.Find(".epos-kachel").Click();

        Assert.Equal(1, gefragt);
        Assert.Empty(gemeldet);

        // Die Ueberlagerung steht, die Startseite darunter ebenfalls.
        Assert.NotEmpty(cut.FindAll("[role='dialog']"));
        Assert.NotEmpty(cut.FindAll(".epos-startseite"));
    }

    /// <summary>
    /// Ohne Parametersatz geht der Klick den GEWÖHNLICHEN Weg — das ist der
    /// Zustand einer Hülle, die die beiden Ansichten nicht anbietet (iOS bis
    /// iU11).
    /// </summary>
    [Fact]
    public void Ohne_Parametersatz_meldet_der_Klick_nur_seinen_Schluessel()
    {
        List<string> gemeldet = new List<string>();
        var cut = Zeige(geklickt: sch => gemeldet.Add(sch));

        cut.FindAll("[role='tab']")[4].Click();
        cut.Find(".epos-startreiter-leiste .epos-knopf").Click();
        cut.Find(".epos-kachel").Click();

        Assert.Equal(new[] { Kachelschluessel.SimulationKonfiguration,
                             Kachelschluessel.SimulationErgebnis }, gemeldet);
        Assert.NotEmpty(cut.FindAll("[role='tab']"));
    }

    // =====================================================================
    //  Die Weiche der Solarthermiekachel
    // =====================================================================

    /// <summary>
    /// Die zwei Auswahlknöpfe der Solarthermie sind eine WEICHE, keine Anzeige:
    /// <c>pBox_Solarthermie_Click</c> prüft <c>radioButton_KollektorProfil.Checked</c>
    /// und öffnet danach den Kollektor- oder den Gangliniendialog (:1262).
    /// </summary>
    [Fact]
    public void Die_Solarweiche_meldet_ihre_Stellung()
    {
        List<bool> gemeldet = new List<bool>();

        var cut = Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Kacheln(0))
            .Add(x => x.ProjektId, () => 1030)
            .Add(x => x.SolarartGewaehlt, an => gemeldet.Add(an)));

        cut.FindAll("[role='tab']")[3].Click();

        var knoepfe = cut.FindAll(".epos-startkachel-wahl input");
        Assert.Equal(2, knoepfe.Count);

        knoepfe[1].Change(true);
        Assert.Equal(new[] { true }, gemeldet);

        cut.FindAll(".epos-startkachel-wahl input")[0].Change(true);
        Assert.Equal(new[] { true, false }, gemeldet);
    }

    // =====================================================================
    //  Die Gattungszeile (Anwenderwunsch 05.09.2026, W16b-E-4)
    // =====================================================================

    /// <summary>
    /// OHNE Kopfleiste darueber steht die Gattungszeile — das ist der Zustand
    /// auf iOS, wo die <c>Kopfleiste</c> der <c>AppWurzel</c> leer ist und diese
    /// Zeile die einzige Nennung des Produkts.
    /// </summary>
    [Fact]
    public void Die_Gattungszeile_steht_ohne_Kopfleiste()
    {
        var cut = Zeige();

        Assert.Single(cut.FindAll(".epos-startseite-gattung"));
        Assert.Equal("Energieplanungs-Software",
                     cut.Find(".epos-startseite-gattung").TextContent.Trim());
    }

    /// <summary>
    /// MIT Kopfleiste darueber faellt sie weg: Das Kopfband des Hauptfensters
    /// nennt dieselbe Gattung schon, und zweimal untereinander ist sie eine
    /// Wiederholung (Anwenderwunsch 05.09.2026).
    /// </summary>
    [Fact]
    public void Die_Gattungszeile_faellt_weg_wenn_eine_Kopfleiste_darueber_steht()
    {
        var cut = Render<Startseite>(p => p
            .Add(x => x.Kacheln, () => Kacheln(0))
            .Add(x => x.ProjektId, () => 1030)
            .Add(x => x.KopfbandZeigen, false));

        Assert.Empty(cut.FindAll(".epos-startseite-gattung"));
        Assert.Empty(cut.FindAll(".epos-startseite-marke"));

        // Das Kopfband selbst bleibt - nur die eine Zeile faellt.
        Assert.Single(cut.FindAll(".epos-startseite-kopf"));
        Assert.Single(cut.FindAll(".epos-startseite-projekt"));
        Assert.Single(cut.FindAll(".epos-startseite-klima"));
    }
}
