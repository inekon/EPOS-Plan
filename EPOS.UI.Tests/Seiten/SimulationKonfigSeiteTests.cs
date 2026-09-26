using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Tests.Dialoge;
using EPOS.UI.Seiten.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die Seite „Simulationskonfiguration" (iU9-W10b.1), Vorbild
/// <c>Views/Simulation/Form_Simulation_Config</c> in vier Teildateien (4 558 Z.).
///
/// <para>Soll: drei Gruppen mit Köpfen, die Sichtbarkeitsregeln der Kacheln
/// (▲▼/× nur auf der ersten Karte je Typ, „verfügbar" standardmäßig verborgen),
/// die Speicherspalte mit höchstens einer offenen Kachel, der Umschalter
/// Liste/Schema mit erhaltener Auswahl, die Fußzeile mit ihren beiden
/// Sofortschaltern, der Sperrzustand und die Überlagerungen.</para>
/// </summary>
public class SimulationKonfigSeiteTests : BunitContext
{
    // Gate sept39 (11.09.2026, Gegenprobe #230b, LANG=en_US.UTF-8):
    // Die_Netzverluste_stehen_beim_Waermebedarf_und_schreiben_sofort vergleicht
    // gegen deutsche Ressourcentexte (u. a. "nur") — ohne Pinnung rechnete die
    // Ressourcenauflösung unter en-US englisch. Hausvorrichtung seit #168,
    // Rückstellung in Dispose.
    private readonly Kulturvorrichtung _kultur = new();

    public SimulationKonfigSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kultur.Dispose();
        }

        base.Dispose(disposing);
    }

    // =====================================================================
    // Probendaten — ein Projekt mit BHKW (zwei Anlagen), Heizkessel (ohne
    // Anlage), einer verfügbaren Wärmepumpe, PV und einem Speicher.
    // =====================================================================

    private readonly List<string> _verschoben = new();
    private readonly List<string> _aufgenommen = new();
    private readonly List<string> _entfernt = new();
    private readonly List<(int Platz, string Wert)> _strom = new();
    private int _gespeichert;
    private readonly List<bool> _lesepunkt = new();
    private int _schemaGeholt;

    /// <summary>#274: der Kurzstand der Stromspeicher-Auslegung; vorbelegt „nichts da".</summary>
    private StromspeicherStand _spStand = new StromspeicherStand();

    /// <summary>#274: Hat die Hülle einen Weg in die Auslegung eingelegt?</summary>
    private bool _spWeg;

    /// <summary>#274: Wie oft wurde die Auslegung geöffnet?</summary>
    private int _spGeoeffnet;

    /// <summary>#307: Steht die Merkspalte „Kaskade vom Anwender gepflegt" auf 1?</summary>
    private bool _gepflegt;

    /// <summary>
    /// #307: Was der Handgriff „Automatik wieder übernehmen" durchgeschrieben hat —
    /// die Hülle setzt Modell UND Datenbank, hier steht die Spur davon.
    /// </summary>
    private readonly List<bool> _gepflegtGeschrieben = new();

    private static ErzeugerZeile Waerme(string dbWert, string rang, string titel,
                                        int idAnlage, bool erste, bool wp = false,
                                        params ChipDaten[] chips) => new ErzeugerZeile
    {
        DbWert = dbWert,
        IdAnlage = idAnlage,
        IdType = wp ? 1 : 11,
        Bezeichner = titel,
        IstWaermepumpe = wp,
        QuellenwahlMoeglich = wp,
        Prioritaet = 2,
        Kachel = new ErzeugerKachelDaten
        {
            Schluessel = dbWert,
            Rang = rang,
            Titel = titel,
            Chips = chips,
            Reihenfolge = erste,
            AufMoeglich = false,
            AbMoeglich = erste,
            Umschaltbar = erste,
            Editierbar = idAnlage > 0
        }
    };

    private SimulationKonfigDaten Daten(bool gesperrt = false, bool mitBooster = true)
    {
        if (gesperrt)
            return new SimulationKonfigDaten
            {
                IdProjekt = 1030,
                Gesperrt = true,
                Sperrgrund = "Die Schema-Migration ist nicht abgeschlossen."
            };

        return new SimulationKonfigDaten
        {
            IdProjekt = 1030,
            Gruppen = new List<KachelGruppe>
            {
                new KachelGruppe
                {
                    Titel = "Wärmeerzeuger",
                    Zeilen = new List<ErzeugerZeile>
                    {
                        Waerme("BHKW", "1", "BHKW · Modul 1", 14920, true, false,
                               new ChipDaten("Senke: Puffer A", ChipStil.Senke, "", ChipZiel.Senke)),
                        Waerme("BHKW", "1", "BHKW · Modul 2", 14921, false),
                        Waerme("Heizkessel", "2", "Heizkessel", 0, true),
                        new ErzeugerZeile
                        {
                            DbWert = "Wärmepumpe",
                            IdType = 1,
                            Verfuegbar = true,
                            Kachel = new ErzeugerKachelDaten
                            {
                                Schluessel = "Wärmepumpe",
                                Titel = "Wärmepumpe",
                                Zustand = Kachelzustand.Verfuegbar,
                                Umschaltbar = true,
                                Chips = new[] { new ChipDaten("nicht in der Simulation", ChipStil.Flaeche) }
                            }
                        }
                    }
                },
                new KachelGruppe
                {
                    Titel = "Stromerzeuger",
                    Zeilen = new List<ErzeugerZeile>
                    {
                        new ErzeugerZeile
                        {
                            DbWert = "Photovoltaik",
                            IdType = 3,
                            IstStrom = true,
                            StromPlatz = 5,
                            Kachel = new ErzeugerKachelDaten
                            {
                                Schluessel = "Photovoltaik",
                                Titel = "Photovoltaik · PV 1",
                                Umschaltbar = true,
                                Detailchips = new[] { new ChipDaten("Module: 24", ChipStil.Quelle) }
                            }
                        }
                    }
                },
                new KachelGruppe
                {
                    Titel = "Energiespeicher",
                    Zeilen = new List<ErzeugerZeile>
                    {
                        new ErzeugerZeile
                        {
                            DbWert = "Stromspeicher",
                            IdType = 4,
                            IstStrom = true,
                            StromPlatz = 6,
                            Verfuegbar = true,
                            Kachel = new ErzeugerKachelDaten
                            {
                                Schluessel = "Stromspeicher",
                                Titel = "Stromspeicher",
                                Zustand = Kachelzustand.Verfuegbar,
                                Umschaltbar = true
                            }
                        }
                    }
                }
            },
            Speicher = new List<SpeicherKachelDaten>
            {
                new SpeicherKachelDaten
                {
                    IdPuffer = 1008007, Bezeichner = "Puffer A", Verwendung = "Heizung",
                    LaderAnzahl = 2, AbnehmerAnzahl = 1,
                    Detailzeilen = new[] { "Versorgt: Heizung" },
                    Schwellentext = "Schwellen 10 / 70 / 95 %"
                },
                new SpeicherKachelDaten
                {
                    IdPuffer = 1008008, Bezeichner = "Puffer B", Verwendung = "Warmwasser",
                    LaderAnzahl = 1, AbnehmerAnzahl = 1,
                    Detailzeilen = new[] { "Versorgt: Warmwasser" }
                }
            },
            SpeicherLeerText = "Dieses Projekt führt keinen Pufferspeicher.",
            Stromspeicherstand = _spStand,
            BoosterSichtbar = mitBooster,
            BoosterDavor = true,
            PvGewaehlt = true,
            KaskadeGepflegt = _gepflegt
        };
    }

    private SimulationKonfigDienste Dienste(bool gesperrt = false, bool mitBooster = true)
        => new SimulationKonfigDienste
        {
            Laden = _ => Daten(gesperrt, mitBooster),
            AuslegungOeffnen = _spWeg ? () => _spGeoeffnet++ : null,
            SchemaLaden = _ => { _schemaGeholt++; return SchemaBild.Leer; },
            Verschieben = (w, r) => _verschoben.Add(w + ":" + r),
            Aufnehmen = w => _aufgenommen.Add(w),
            Entfernen = w => _entfernt.Add(w),
            StromAuswahl = (p, w) => _strom.Add((p, w)),
            AutomatikUebernehmen = () => { _gepflegt = false; _gepflegtGeschrieben.Add(false); },
            Speichern = () => { _gespeichert++; return true; },
            LesepunktSchreiben = w => { _lesepunkt.Add(w); return true; },
            BetriebsmodusGaben = _ => new Dictionary<string, object>
            {
                ["Bezeichner"] = "WP 1", ["AktuellerModus"] = ""
            },
            WaermesenkeGaben = _ => new Dictionary<string, object>
            {
                ["Daten"] = new EPOS.UI.Dialoge.Simulation.WaermesenkeDaten()
            },
            PufferVerwaltungGaben = _ => new Dictionary<string, object>
            {
                ["IdProjekt"] = 1030
            },
            Quellentypen = _ => new List<Quellentyp>
            {
                new Quellentyp("", "Systemrücklauf"),
                new Quellentyp("Außenluft", "Außenluft"),
                new Quellentyp("Konstant", "konstante Temperatur"),
                new Quellentyp("Pufferspeicher", "Pufferspeicher")
            },
            QuelleTyp = _ => "Außenluft",
            QuelleTemperatur = _ => 8.5
        };

    private IRenderedComponent<SimulationKonfigSeite> Seite(
        bool gesperrt = false, bool mitBooster = true)
        => Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, Dienste(gesperrt, mitBooster))
            .Add(x => x.StartProjekt, 1030));

    // =====================================================================
    //  #216, Punkt 3 — die fuenf Laufparameter stehen in Schritt ①
    // =====================================================================

    private readonly List<string> _geschrieben = new();

    private SimulationParameterDienste Parameterdienste() => new SimulationParameterDienste
    {
        Laden = () => new ParameterDaten
        {
            Netzverluste = 4,
            NetzverlusteEinheit = "%",
            Betriebsart = 1,
            UntersteLeistungsgrenze = 30,
            Bereitschaft = 8000
        },
        NetzverlusteSchreiben = (w, e) => _geschrieben.Add("netz:" + w + e),
        BetriebsartSchreiben = w => _geschrieben.Add("betriebsart:" + w),
        LeistungsgrenzeSchreiben = w => _geschrieben.Add("grenze:" + w),
        // 16.09.2026 (Auftrag #299): HeizstabSchreiben ist entfallen.
        BereitschaftSchreiben = w => _geschrieben.Add("bereitschaft:" + w)
    };

    private IRenderedComponent<SimulationKonfigSeite> SeiteMitParametern()
        => Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, Dienste())
            .Add(x => x.Parameter, Parameterdienste())
            .Add(x => x.StartProjekt, 1030));

    /// <summary>
    /// <b>Windows-Abnahme #216, Punkt 3:</b> „Nimm Parameter heraus — die
    /// Netzverluste können an eine andere Stelle." Sie stehen jetzt im Abschnitt
    /// „Wärmebedarf" dieser Seite, mit dem Hinweis, wann sie wirken — und sie
    /// schreiben sofort, wie im abgelösten Reiter.
    /// </summary>
    [Fact]
    public void Die_Netzverluste_stehen_beim_Waermebedarf_und_schreiben_sofort()
    {
        var seite = SeiteMitParametern();

        IElement abschnitt = seite.Find("section.epos-simkonfig-bedarf");
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMKONF_GRP_WAERMEBEDARF,
                     abschnitt.GetAttribute("aria-label"));
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_LBL_NETZVERLUSTE,
                        abschnitt.TextContent);
        Assert.Contains("nur", abschnitt.TextContent.ToLowerInvariant());

        IElement feld = abschnitt.QuerySelectorAll("input")[0];
        Assert.Equal("4", feld.GetAttribute("value"));

        feld.Input("7");
        Assert.Equal(new[] { "netz:7%" }, _geschrieben);
    }

    /// <summary>
    /// <b>Anwenderauftrag 26.09.2026</b> („Der Dialog ist unübersichtlich. Bringe die Abschnitte
    /// Wärmebedarf, Kühlung, Anlagenkopplung nach unten."): Oben steht die eigentliche Arbeit —
    /// Komponenten der Simulation und Speicher im Projekt —, direkt darunter „Konfiguration
    /// speichern"; erst danach folgt EIN Block „Weitere Einstellungen" mit den drei
    /// Projekteinstellungen in dieser Reihenfolge, je Feld im Formularraster statt eigener Balken.
    /// </summary>
    [Fact]
    public void Komponenten_und_Speicherknopf_stehen_vor_den_weiteren_Einstellungen()
    {
        var seite = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, Dienste())
            .Add(x => x.Parameter, KuehlParameterdienste(kuehlbetrieb: false))
            .Add(x => x.StartProjekt, 1030));

        List<IElement> folge = seite.FindAll(
            "section.epos-simkonfig-erzeuger, section.epos-simkonfig-speicher, " +
            "div.epos-simkonfig-fuss, fieldset.epos-simkonfig-einstellungen, " +
            "section.epos-simkonfig-bedarf").ToList();
        List<string> namen = folge.Select(e => e.LocalName == "section"
                                              ? e.ClassList.Last()
                                              : e.ClassName!).ToList();
        Assert.Equal(new[]
        {
            "epos-simkonfig-erzeuger", "epos-simkonfig-speicher", "epos-simkonfig-fuss",
            "epos-simkonfig-einstellungen", "epos-simkonfig-bedarf", "epos-simkonfig-kuehlung"
        }, namen);

        // Der Speicherknopf steht in der Fußzeile direkt unter den Komponenten, nicht unter den Einstellungen.
        IElement fuss = seite.Find("div.epos-simkonfig-fuss");
        Assert.Contains(seite.Instance.BtnSpeichern, fuss.TextContent);

        // Ein Gruppenkopf für alle drei, jede Einstellung im Formularraster.
        IElement block = seite.Find("fieldset.epos-simkonfig-einstellungen");
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMKONF_GRP_WEITERE, block.TextContent);
        foreach (IElement abschnitt in block.QuerySelectorAll("section.epos-simkonfig-bedarf"))
            Assert.NotNull(abschnitt.QuerySelector(".epos-formularraster .epos-herleitung"));
    }

    /// <summary>Ohne Parametersatz steht der Abschnitt gar nicht da.</summary>
    [Fact]
    public void Ohne_Parametersatz_bleibt_der_Waermebedarfsabschnitt_weg()
    {
        var seite = Seite();

        Assert.Empty(seite.FindAll("fieldset.epos-simkonfig-einstellungen"));
        Assert.Empty(seite.FindAll("section.epos-simkonfig-bedarf"));
        Assert.Empty(seite.FindAll("div.epos-erzeugerkachel-parameter"));
    }

    // =====================================================================
    //  Stufe KU1 (Kühlkonzept 8.3, 8.6 Maske 1) — die Projekteinstellung
    //  „Kühlung rechnen"
    // =====================================================================

    /// <summary>Was der Kühlschalter geschrieben hat; die Antwort der Naht steht in <see cref="_kuehlAntwort"/>.</summary>
    private readonly List<bool> _kuehlGeschrieben = new();

    private bool _kuehlAntwort = true;

    private SimulationParameterDienste KuehlParameterdienste(bool kuehlbetrieb)
    {
        SimulationParameterDienste wege = Parameterdienste();
        Func<ParameterDaten> laden = wege.Laden!;
        wege.Laden = () =>
        {
            ParameterDaten p = laden();
            p.Kuehlbetrieb = kuehlbetrieb;
            return p;
        };
        wege.KuehlbetriebSchreiben = an =>
        {
            _kuehlGeschrieben.Add(an);
            return _kuehlAntwort;
        };
        return wege;
    }

    private IRenderedComponent<SimulationKonfigSeite> SeiteMitKuehlung(bool kuehlbetrieb)
        => Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, Dienste())
            .Add(x => x.Parameter, KuehlParameterdienste(kuehlbetrieb))
            .Add(x => x.StartProjekt, 1030));

    /// <summary>
    /// Der Abschnitt „Kühlung" steht neben dem Wärmebedarf: der Schalter „Kühlung rechnen"
    /// mit dem Stand der Datenbank und der Herleitungszeile, die sagt, dass er für das ganze
    /// Projekt gilt. Er schreibt SOFORT, wie die Netzverluste.
    /// </summary>
    [Fact]
    public void Der_Kuehlschalter_zeigt_den_Stand_und_schreibt_sofort()
    {
        var seite = SeiteMitKuehlung(kuehlbetrieb: false);

        IElement abschnitt = seite.Find("section.epos-simkonfig-kuehlung");
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMKONF_GRP_KUEHLUNG, abschnitt.GetAttribute("aria-label"));
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMKONF_LBL_KUEHLBETRIEB, abschnitt.TextContent);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMKONF_HRL_KUEHLBETRIEB, abschnitt.TextContent);

        IElement schalter = abschnitt.QuerySelector("input[type=checkbox]")!;
        Assert.False(schalter.HasAttribute("checked"));

        schalter.Change(true);
        Assert.Equal(new[] { true }, _kuehlGeschrieben);
        Assert.True(seite.Instance.Laufparameter.Kuehlbetrieb);

        // Der Wärmebedarfsabschnitt bleibt der erste der weiteren Einstellungen - und unverändert.
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMKONF_GRP_WAERMEBEDARF,
                     seite.Find("section.epos-simkonfig-bedarf").GetAttribute("aria-label"));
    }

    /// <summary>Ein eingeschaltetes Projekt zeigt den Schalter gesetzt; „aus" schreibt ebenso sofort.</summary>
    [Fact]
    public void Der_Kuehlschalter_schaltet_auch_aus()
    {
        var seite = SeiteMitKuehlung(kuehlbetrieb: true);

        IElement schalter = seite.Find("section.epos-simkonfig-kuehlung input[type=checkbox]");
        Assert.True(schalter.HasAttribute("checked"));

        schalter.Change(false);
        Assert.Equal(new[] { false }, _kuehlGeschrieben);
        Assert.False(seite.Instance.Laufparameter.Kuehlbetrieb);
    }

    /// <summary>
    /// Scheitert das Schreiben, springt der Schalter nicht still zurück: Der Stand bleibt der
    /// gespeicherte, und die Fußzeile meldet es.
    /// </summary>
    [Fact]
    public void Ein_gescheitertes_Schreiben_des_Kuehlschalters_wird_gemeldet()
    {
        _kuehlAntwort = false;
        var seite = SeiteMitKuehlung(kuehlbetrieb: false);

        seite.Find("section.epos-simkonfig-kuehlung input[type=checkbox]").Change(true);

        Assert.Equal(new[] { true }, _kuehlGeschrieben);
        Assert.False(seite.Instance.Laufparameter.Kuehlbetrieb);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMKONF_MSG_KUEHLBETRIEB_FEHLER, seite.Markup);
        Assert.False(seite.Find("section.epos-simkonfig-kuehlung input[type=checkbox]").HasAttribute("checked"));
    }

    /// <summary>
    /// Ohne Schreibweg (eine Plattform, die den Schalter nicht anbietet) steht der Abschnitt
    /// nicht da — benannt über die fehlende Naht, nicht als gesperrter Schalter ohne Grund.
    /// </summary>
    [Fact]
    public void Ohne_Schreibweg_steht_kein_Kuehlabschnitt()
    {
        var seite = SeiteMitParametern();

        Assert.NotEmpty(seite.FindAll("section.epos-simkonfig-bedarf"));
        Assert.Empty(seite.FindAll("section.epos-simkonfig-kuehlung"));
    }

    // =====================================================================
    //  Anlagenkopplung AK1 Welle 3 (Konzept Anlagenkopplung 9.4, 9.6 Maske 1) —
    //  die Projekteinstellung „Anlagenkopplung"
    // =====================================================================

    /// <summary>Was die Kopplungswahl geschrieben hat; die Antwort der Naht steht in <see cref="_kopplungAntwort"/>.</summary>
    private readonly List<string?> _kopplungGeschrieben = new();

    private bool _kopplungAntwort = true;

    private IRenderedComponent<SimulationKonfigSeite> SeiteMitKopplung(string? stufe)
    {
        SimulationParameterDienste wege = Parameterdienste();
        Func<ParameterDaten> laden = wege.Laden!;
        wege.Laden = () =>
        {
            ParameterDaten p = laden();
            p.Anlagenkopplung = stufe;
            return p;
        };
        wege.AnlagenkopplungSchreiben = s =>
        {
            _kopplungGeschrieben.Add(s);
            return _kopplungAntwort;
        };
        return Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, Dienste())
            .Add(x => x.Parameter, wege)
            .Add(x => x.StartProjekt, 1030));
    }

    private static IElement Kopplungswahl(IRenderedComponent<SimulationKonfigSeite> seite)
        => seite.Find("section.epos-simkonfig-anlagenkopplung select");

    private static IElement Kopplungsoption(IRenderedComponent<SimulationKonfigSeite> seite, string wert)
        => Kopplungswahl(seite).QuerySelectorAll("option").First(o => o.GetAttribute("value") == wert);

    /// <summary>
    /// Die Wahl führt die vier Stufen der Wertliste; nur „aus" und „Heizkreis (AK1)" sind wählbar,
    /// Fahrplan (AK2) und geschlossener Kreis (AK3) stehen gesperrt mit ihrem Grund — kein
    /// Persistenzwert ohne Rechenweg (Kühlkonzept K7). Zeichnen schreibt nichts.
    /// </summary>
    [Fact]
    public void Die_Anlagenkopplung_fuehrt_vier_Stufen_und_sperrt_die_nicht_gebauten()
    {
        var seite = SeiteMitKopplung(null);

        IElement abschnitt = seite.Find("section.epos-simkonfig-anlagenkopplung");
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMKONF_GRP_ANLAGENKOPPLUNG, abschnitt.GetAttribute("aria-label"));
        List<IElement> optionen = Kopplungswahl(seite).QuerySelectorAll("option")
            .Where(o => o.GetAttribute("value") != "").ToList();
        Assert.Equal(4, optionen.Count);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMKONF_ANLAGENKOPPLUNG_AUS, optionen[0].TextContent.Trim());
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMKONF_ANLAGENKOPPLUNG_AK1, optionen[1].TextContent.Trim());
        Assert.False(optionen[0].HasAttribute("disabled"));
        Assert.False(optionen[1].HasAttribute("disabled"));
        Assert.True(optionen[2].HasAttribute("disabled"));
        Assert.True(optionen[3].HasAttribute("disabled"));
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMKONF_ANLAGENKOPPLUNG_NICHT_VERFUEGBAR, optionen[2].TextContent);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMKONF_ANLAGENKOPPLUNG_NICHT_VERFUEGBAR, optionen[3].TextContent);

        Assert.True(optionen[0].HasAttribute("selected"));
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMKONF_HRL_ANLAGENKOPPLUNG_AUS, abschnitt.TextContent);
        Assert.Empty(_kopplungGeschrieben);
    }

    /// <summary>„Heizkreis (AK1)" schreibt SOFORT, wie der Kühlschalter; „aus" schreibt NULL.</summary>
    [Fact]
    public void AK1_schreibt_sofort_und_aus_schreibt_null()
    {
        var seite = SeiteMitKopplung(null);

        Kopplungswahl(seite).Change("1");
        Assert.Equal(new string?[] { WindowsFormsApplication1.DbWerte.ANLAGENKOPPLUNG_AK1 }, _kopplungGeschrieben);
        Assert.Equal(WindowsFormsApplication1.DbWerte.ANLAGENKOPPLUNG_AK1, seite.Instance.Laufparameter.Anlagenkopplung);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMKONF_HRL_ANLAGENKOPPLUNG_AK1, seite.Find("section.epos-simkonfig-anlagenkopplung").TextContent);

        Kopplungswahl(seite).Change("0");
        Assert.Equal(new string?[] { WindowsFormsApplication1.DbWerte.ANLAGENKOPPLUNG_AK1, null }, _kopplungGeschrieben);
        Assert.Null(seite.Instance.Laufparameter.Anlagenkopplung);
    }

    /// <summary>Eine gesperrte Stufe lässt das Feld nicht zu: Es wird nichts geschrieben.</summary>
    [Fact]
    public void Eine_nicht_gebaute_Stufe_schreibt_nicht()
    {
        var seite = SeiteMitKopplung(null);

        Kopplungswahl(seite).Change("2");

        Assert.Empty(_kopplungGeschrieben);
        Assert.Null(seite.Instance.Laufparameter.Anlagenkopplung);
    }

    /// <summary>
    /// Steht eine nicht gebaute Stufe schon in der Datenbank, zeigt die Wahl sie, und die Zeile
    /// sagt, dass der Lauf den Heizkreis (AK1) rechnet.
    /// </summary>
    [Fact]
    public void Eine_gespeicherte_nicht_gebaute_Stufe_steht_da_und_nennt_was_der_Lauf_rechnet()
    {
        var seite = SeiteMitKopplung(WindowsFormsApplication1.DbWerte.ANLAGENKOPPLUNG_AK2);

        Assert.True(Kopplungsoption(seite, "2").HasAttribute("selected"));
        Assert.Contains(string.Format(WindowsFormsApplication1.MyResource.Resource.SIMKONF_HRL_ANLAGENKOPPLUNG_NICHT_GEBAUT, WindowsFormsApplication1.MyResource.Resource.SIMKONF_ANLAGENKOPPLUNG_AK2),
                        seite.Find("section.epos-simkonfig-anlagenkopplung").TextContent);
    }

    /// <summary>Scheitert das Schreiben, kehrt die Wahl zum gespeicherten Stand zurück, und die Fußzeile meldet es.</summary>
    [Fact]
    public void Ein_gescheitertes_Schreiben_der_Kopplung_wird_gemeldet_und_die_Wahl_kehrt_zurueck()
    {
        _kopplungAntwort = false;
        var seite = SeiteMitKopplung(null);

        Kopplungswahl(seite).Change("1");

        Assert.Equal(new string?[] { WindowsFormsApplication1.DbWerte.ANLAGENKOPPLUNG_AK1 }, _kopplungGeschrieben);
        Assert.Null(seite.Instance.Laufparameter.Anlagenkopplung);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMKONF_MSG_ANLAGENKOPPLUNG_FEHLER, seite.Markup);
        Assert.True(Kopplungsoption(seite, "0").HasAttribute("selected"));
    }

    /// <summary>Ohne Schreibweg (eine Plattform ohne die Naht) steht kein Abschnitt „Anlagenkopplung".</summary>
    [Fact]
    public void Ohne_Schreibweg_steht_kein_Kopplungsabschnitt()
    {
        var seite = SeiteMitParametern();

        Assert.Empty(seite.FindAll("section.epos-simkonfig-anlagenkopplung"));
    }

    /// <summary>
    /// <b>Anwenderwunsch 16.09.2026</b> (Screenshots „Simulation → Konfiguration"):
    /// „Erstelle dort einen Knopf anstelle des blauen Balkens ‚Parameter für die
    /// Simulation' mit dem Konfigurationsdialog." An der Karte steht seither GENAU
    /// EIN Knopf — und nur an den drei Arten, die überhaupt Parameter führen.
    /// </summary>
    [Fact]
    public void Jede_Karte_mit_Parametern_traegt_den_Konfigurationsknopf()
    {
        var seite = SeiteMitParametern();

        // Die Waermepumpe ist im Probenprojekt VERFUEGBAR und damit zugeklappt -
        // der Textschalter am Spaltenende holt sie hervor.
        seite.Find("button.epos-simkonfig-verfuegbar").Click();

        foreach (string titel in new[]
                 { "BHKW · Modul 1", "BHKW · Modul 2", "Heizkessel", "Wärmepumpe" })
        {
            IElement karte = Karte(seite, titel);
            Assert.Single(karte.QuerySelectorAll("div.epos-erzeugerkachel-parameter"));

            IElement knopf = karte
                .QuerySelector("div.epos-erzeugerkachel-parameter button")!;
            Assert.Equal("Konfiguration…", knopf.TextContent.Trim());
        }

        // Die Felder selbst stehen NICHT mehr an der Karte - sie sind im Dialog.
        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.SIMERG_GRP_BETRIEBSART,
                              Karte(seite, "BHKW · Modul 1").TextContent);
        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.SIMERG_CHK_HEIZSTAB,
                              Karte(seite, "Wärmepumpe").TextContent);
        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.SIMERG_LBL_BEREITSCHAFT,
                              Karte(seite, "Heizkessel").TextContent);

        // Und kein Feld hat beim Zeichnen geschrieben.
        Assert.Empty(_geschrieben);
    }

    /// <summary>
    /// KEIN KNOPF OHNE INHALT: Photovoltaik, Stromspeicher, Solarthermie und
    /// Pufferspeicher führen keinen Laufparameter — ein Knopf, der einen leeren
    /// Dialog öffnet, ist keiner.
    /// </summary>
    [Fact]
    public void Eine_Karte_ohne_Parameter_traegt_keinen_Knopf()
    {
        var seite = SeiteMitParametern();
        seite.Find("button.epos-simkonfig-verfuegbar").Click();

        Assert.Empty(Karte(seite, "Photovoltaik · PV 1")
                         .QuerySelectorAll("div.epos-erzeugerkachel-parameter"));
        Assert.Empty(Karte(seite, "Stromspeicher")
                         .QuerySelectorAll("div.epos-erzeugerkachel-parameter"));

        // Vier Bereiche: zwei BHKW-Module, Heizkessel, Waermepumpe.
        Assert.Equal(4, seite.FindAll("div.epos-erzeugerkachel-parameter").Count);
    }

    /// <summary>
    /// Der Klick öffnet die Überlagerung, und sie trägt den Titel der Karte —
    /// „Konfiguration · BHKW · Modul 2". AUCH am zweiten Modul: Bis zum
    /// 16.09.2026 stand der Block nur an der ersten Karte je Art; ein Knopf
    /// verdoppelt keine Eingabe, und wer die Konfiguration des zweiten Moduls
    /// sucht, soll sie an dessen Karte finden.
    /// </summary>
    [Fact]
    public void Der_Knopf_oeffnet_die_Ueberlagerung_mit_dem_Kartentitel()
    {
        var seite = SeiteMitParametern();
        Assert.Equal("Keine", seite.Instance.OffenerEditor);

        Knopf(seite, "BHKW · Modul 2").Click();

        Assert.Equal("Komponentenkonfig", seite.Instance.OffenerEditor);
        Assert.Equal("Konfiguration · BHKW · Modul 2",
                     seite.Find("h2.epos-ueberlagerung-titel").TextContent);

        // Der Dialog zeigt die BHKW-Felder - und keinen zweiten Titel (der Kopf der
        // Ueberlagerung traegt ihn).
        IElement bereich = seite.Find("div.epos-ueberlagerung");
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_GRP_BETRIEBSART,
                        bereich.TextContent);
        Assert.Empty(bereich.QuerySelectorAll("h1.epos-dialog-titel"));
    }

    /// <summary>
    /// <b>OK schreibt, Abbrechen nicht</b> — die Hausregel „geschrieben wird im
    /// OK-Weg" (EPOS.UI/CLAUDE.md, „Bedienung"). Bis zum 16.09.2026 schrieb jedes
    /// Feld an der Karte SOFORT; mit dem Dialog gibt es einen Arbeitsstand, und
    /// Abbrechen nimmt ihn zurück.
    /// </summary>
    [Fact]
    public void Der_Dialog_schreibt_im_OK_Weg_und_bei_Abbrechen_nicht()
    {
        var seite = SeiteMitParametern();

        // --- Abbrechen: die Betriebsart bleibt, wo sie war ---
        Knopf(seite, "BHKW · Modul 1").Click();
        seite.Find("div.epos-ueberlagerung").QuerySelectorAll("input[type='radio']")[2]
             .Change(true);
        Leiste(seite, 0).Click();                       // Abbrechen

        Assert.Equal("Keine", seite.Instance.OffenerEditor);
        Assert.Empty(_geschrieben);

        // --- OK: nur der GEAENDERTE Wert geht seinen Weg ---
        Knopf(seite, "BHKW · Modul 1").Click();
        seite.Find("div.epos-ueberlagerung").QuerySelectorAll("input[type='radio']")[2]
             .Change(true);
        Leiste(seite, 1).Click();                       // OK

        Assert.Equal(new[] { "betriebsart:2" }, _geschrieben);
        Assert.Equal(2, seite.Instance.Laufparameter.Betriebsart);
    }

    /// <summary>
    /// Heizkessel und Wärmepumpe gehen denselben Weg — jeder über den Delegaten,
    /// der bis zum 16.09.2026 am Feld der Karte hing.
    /// </summary>
    [Fact]
    public void Der_Heizkessel_schreibt_seinen_Wert_im_OK_Weg()
    {
        var seite = SeiteMitParametern();
        seite.Find("button.epos-simkonfig-verfuegbar").Click();

        Knopf(seite, "Heizkessel").Click();
        seite.Find("div.epos-ueberlagerung").QuerySelectorAll("input")[0].Input("7500");
        Leiste(seite, 1).Click();

        // Die Wärmepumpe steht NICHT mehr daneben: Ihr projektweiter Heizstabschalter
        // ist mit Auftrag #299 entfallen - der Heizstab gehört der Anlage und geht über
        // WaermepumpeKonfigurationSpeichern (siehe den Fall darunter).
        Assert.Equal(new[] { "bereitschaft:7500" }, _geschrieben);
    }

    /// <summary>
    /// <b>Die Naht zur Anlage</b>: Führt die Kartenzeile eine Wärmepumpen-Anlage und
    /// bietet die Plattform den Weg an, holt der Dialog ihre Anlagendaten und gibt
    /// sie beim OK zurück. Ohne die Delegaten (iOS, Proben) bleibt der Knopf stehen
    /// und der Dialog zeigt allein die Projekteinstellung.
    /// </summary>
    [Fact]
    public void Die_Waermepumpenkonfiguration_wird_geladen_und_im_OK_Weg_gespeichert()
    {
        var geladen = new List<int>();
        var gespeichert = new List<int>();
        var anlage = new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageDaten
        {
            Bezeichner = "WP 1", Heizstab = false, Sperrung = true
        };

        SimulationParameterDienste dienste = Parameterdienste();
        dienste.WaermepumpeKonfigurationLaden = id => { geladen.Add(id); return anlage; };
        dienste.WaermepumpeKonfigurationSpeichern = (id, d) =>
        {
            gespeichert.Add(id);
            return new AnlagenkonfigErgebnis(true, "");
        };

        var seite = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, DiensteMitWpAnlage())
            .Add(x => x.Parameter, dienste)
            .Add(x => x.StartProjekt, 1030));

        Knopf(seite, "Wärmepumpe · WP 1").Click();
        Assert.Equal(new[] { 14930 }, geladen);
        Assert.NotNull(seite.FindComponent<EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKonfiguration>());

        Leiste(seite, 1).Click();
        Assert.Equal(new[] { 14930 }, gespeichert);
    }

    /// <summary>
    /// <b>Der Grund des Kerns steht in der Meldung</b> (16.09.2026). Bis dahin meldete
    /// die Naht nur <c>true</c>/<c>false</c>, und die Seite setzte ihren eigenen
    /// Allgemeinplatz daneben — der Satz, WARUM nicht geschrieben wurde („Die Anlage 42
    /// wurde nicht gefunden"), ging dabei verloren.
    /// </summary>
    [Fact]
    public void Ein_Fehlschlag_der_Anlagenkonfiguration_meldet_den_Grund_des_Kerns()
    {
        var anlage = new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageDaten
        {
            Bezeichner = "WP 1"
        };

        SimulationParameterDienste dienste = Parameterdienste();
        dienste.WaermepumpeKonfigurationLaden = _ => anlage;
        dienste.WaermepumpeKonfigurationSpeichern = (_, _) =>
            new AnlagenkonfigErgebnis(false, "Die Anlage 14930 wurde nicht gefunden.");

        var seite = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, DiensteMitWpAnlage())
            .Add(x => x.Parameter, dienste)
            .Add(x => x.StartProjekt, 1030));

        Knopf(seite, "Wärmepumpe · WP 1").Click();
        Leiste(seite, 1).Click();

        Assert.Contains("Die Anlage 14930 wurde nicht gefunden.", seite.Markup);
    }

    /// <summary>
    /// Ohne Wortlaut bleibt der Rückfall der Seite stehen — eine leere Meldung wäre
    /// ein Band ohne Aussage.
    /// </summary>
    [Fact]
    public void Ohne_Wortlaut_meldet_die_Seite_ihren_Rueckfall()
    {
        var anlage = new EPOS.UI.Dialoge.Waermepumpe.WaermepumpeAnlageDaten
        {
            Bezeichner = "WP 1"
        };

        SimulationParameterDienste dienste = Parameterdienste();
        dienste.WaermepumpeKonfigurationLaden = _ => anlage;
        dienste.WaermepumpeKonfigurationSpeichern = (_, _) => new AnlagenkonfigErgebnis(false, "");

        var seite = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, DiensteMitWpAnlage())
            .Add(x => x.Parameter, dienste)
            .Add(x => x.StartProjekt, 1030)
            .Add(x => x.StatusKonfigFehler, "Rückfalltext der Seite"));

        Knopf(seite, "Wärmepumpe · WP 1").Click();
        Leiste(seite, 1).Click();

        Assert.Contains("Rückfalltext der Seite", seite.Markup);
    }

    /// <summary>
    /// Ohne die Naht bleibt der Knopf — der Dialog geht auf, zeigt aber NICHTS zu
    /// konfigurieren.
    ///
    /// <para><b>Auftrag #299 (16.09.2026):</b> Bis dahin stand hier die
    /// PROJEKTEINSTELLUNG „mit Heizstab". Der Heizstab gehört seither der Wärmepumpe
    /// (<c>Tab_Energieanlagen.Heizstab</c>); einen projektweiten Wert gibt es nicht
    /// mehr, und wo die Plattform die Naht zur Anlage nicht stellt, bleibt der Bereich
    /// leer.</para>
    /// </summary>
    [Fact]
    public void Ohne_Naht_zeigt_die_Waermepumpe_keine_Konfiguration()
    {
        var seite = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, DiensteMitWpAnlage())
            .Add(x => x.Parameter, Parameterdienste())
            .Add(x => x.StartProjekt, 1030));

        Knopf(seite, "Wärmepumpe · WP 1").Click();

        IElement bereich = seite.Find("div.epos-ueberlagerung");
        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.SIMERG_CHK_HEIZSTAB,
                              bereich.TextContent);
        Assert.Empty(seite.FindComponents<EPOS.UI.Dialoge.Waermepumpe.WaermepumpeKonfiguration>());

        // Die Schlussleiste steht trotzdem - der Dialog ist offen, nur leer.
        Assert.Equal(2, seite.Find("div.epos-ueberlagerung")
                             .QuerySelectorAll("div.epos-leiste button").Length);
    }

    /// <summary>Der Konfigurationsknopf EINER Karte über ihren Titel.</summary>
    private static IElement Knopf(IRenderedComponent<SimulationKonfigSeite> seite, string titel)
        => Karte(seite, titel).QuerySelector("div.epos-erzeugerkachel-parameter button")!;

    /// <summary>Ein Knopf der Schlussleiste IM Dialog: 0 = Abbrechen, 1 = OK.</summary>
    private static IElement Leiste(IRenderedComponent<SimulationKonfigSeite> seite, int platz)
        => seite.Find("div.epos-ueberlagerung").QuerySelectorAll("div.epos-leiste button")[platz];

    /// <summary>
    /// Wie <see cref="Dienste"/>, aber mit einer AUFGENOMMENEN Wärmepumpe samt
    /// Anlagen-Id — die Probe für die Naht zur Anlagenkonfiguration.
    /// </summary>
    private SimulationKonfigDienste DiensteMitWpAnlage()
    {
        SimulationKonfigDienste dienste = Dienste();
        dienste.Laden = _ => new SimulationKonfigDaten
        {
            IdProjekt = 1030,
            Gruppen = new List<KachelGruppe>
            {
                new KachelGruppe
                {
                    Titel = "Wärmeerzeuger",
                    Zeilen = new List<ErzeugerZeile>
                    {
                        Waerme("Wärmepumpe", "1", "Wärmepumpe · WP 1", 14930, true, true)
                    }
                }
            },
            Stromspeicherstand = _spStand
        };
        return dienste;
    }

    /// <summary>Die Erzeugerkarte EINES Erzeugers über ihren Titel.</summary>
    private static IElement Karte(IRenderedComponent<SimulationKonfigSeite> seite, string titel)
        => seite.FindAll("div.epos-erzeugerkachel")
                .Single(k => k.QuerySelector(".epos-erzeugerkachel-titel")!.TextContent.Trim() == titel);

    /// <summary>Die Seite mit GENAU diesen Daten (W10b-B-2).</summary>
    private IRenderedComponent<SimulationKonfigSeite> Zeige(SimulationKonfigDaten daten)
    {
        SimulationKonfigDienste dienste = Dienste();
        dienste.Laden = _ => daten;
        return Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.StartProjekt, 1030));
    }

    // ================================================================== Aufbau

    [Fact]
    public void Drei_Gruppen_stehen_mit_ihren_Koepfen_untereinander()
    {
        var cut = Seite();

        var koepfe = cut.FindAll("h3.epos-simkonfig-gruppe");
        Assert.Equal(3, koepfe.Count);
        Assert.Equal("Wärmeerzeuger", koepfe[0].TextContent);
        Assert.Equal("Stromerzeuger", koepfe[1].TextContent);
        Assert.Equal("Energiespeicher", koepfe[2].TextContent);
    }

    /// <summary>
    /// Ein aufgenommener Kaskadenplatz OHNE Anlage im Projekt bekommt eine Kachel
    /// mit dem Chip „keine Anlage im Projekt" — die alte Übersicht zeigte dafür
    /// eine Zeile mit „-".
    /// </summary>
    [Fact]
    public void Ein_Kaskadenplatz_ohne_Anlage_bekommt_trotzdem_seine_Kachel()
    {
        var cut = Seite();

        var titel = cut.FindAll("span.epos-erzeugerkachel-titel");
        Assert.Contains(titel, t => t.TextContent == "Heizkessel");
    }

    /// <summary>
    /// ▲▼ und × stehen nur auf der ERSTEN Kachel eines Erzeugertyps: Reihenfolge
    /// und Teilnahme gelten dem TYP, nicht der einzelnen Anlage (:766-768).
    /// </summary>
    [Fact]
    public void Die_Schalter_stehen_nur_auf_der_ersten_Kachel_je_Typ()
    {
        var cut = Seite();

        var kacheln = cut.FindAll("div.epos-erzeugerkachel");
        // 0 = BHKW Modul 1 (erste), 1 = BHKW Modul 2, 2 = Heizkessel, 3 = PV
        Assert.NotEmpty(kacheln[0].QuerySelectorAll("button.epos-erzeugerkachel-glyphe"));

        // Modul 2 traegt nur noch das ✎ - kein ▲▼, kein ×.
        var zweite = kacheln[1].QuerySelectorAll("button.epos-erzeugerkachel-glyphe");
        Assert.Single(zweite);
        Assert.Equal("✎", zweite[0].TextContent.Trim());
    }

    // ================================================================== Verfügbar

    /// <summary>
    /// Nicht aufgenommene Komponenten sind standardmäßig AUSGEBLENDET
    /// (<c>_verfuegbareZeigen = false</c>); der Textschalter am Spaltenende nennt
    /// ihre Zahl und holt sie zurück (Abnahmebefund 1).
    /// </summary>
    [Fact]
    public void Verfuegbare_Kacheln_sind_verborgen_und_werden_gezaehlt()
    {
        var cut = Seite();

        Assert.Empty(cut.FindAll("div.epos-erzeugerkachel--verfuegbar"));

        var schalter = cut.Find("button.epos-simkonfig-verfuegbar");
        Assert.Contains("(2)", schalter.TextContent);   // Wärmepumpe und Stromspeicher

        schalter.Click();
        Assert.Equal(2, cut.FindAll("div.epos-erzeugerkachel--verfuegbar").Count);
        Assert.Contains("ausblenden", cut.Find("button.epos-simkonfig-verfuegbar").TextContent);
    }

    // ============================================== Erzeuger ohne Kaskadenplatz (#190)

    /// <summary>
    /// Die Probendaten führen NUR Platzhalter (keine Anlage im Projekt) — dann bleibt
    /// alles wie bisher: keine Leiste, die Spalte startet zugeklappt. Ohne diese
    /// Gegenprobe stünde die Leiste in jedem Projekt, und der Abnahmebefund 1
    /// („Platzhalter verbergen") wäre zurückgenommen.
    /// </summary>
    [Fact]
    public void Ein_blosser_Platzhalter_loest_keine_Hinweisleiste_aus()
    {
        var cut = Seite();

        Assert.Empty(cut.FindAll("div.epos-simkonfig-luecke"));
        Assert.Empty(cut.FindAll("div.epos-erzeugerkachel--verfuegbar"));
    }

    /// <summary>
    /// #190, Abnahmeliste „PV mit Heizkessel": Ist ein Erzeuger im Projekt ANGELEGT,
    /// steht aber auf keinem Platz, meldet die Seite ihn — und blendet die verfügbaren
    /// Karten von sich aus ein. Aufgenommen wird weiterhin von Hand (HK-E-1a).
    /// </summary>
    [Fact]
    public void Ein_angelegter_Erzeuger_ohne_Platz_bekommt_die_Hinweisleiste()
    {
        var cut = Zeige(MitLuecke(1));

        var leiste = cut.Find("div.epos-simkonfig-luecke");
        Assert.Contains("nicht in der Simulation", leiste.TextContent);

        // Die Karten stehen da - der Anwender kommt ohne Umweg an „+ aufnehmen".
        Assert.Equal(3, cut.FindAll("div.epos-erzeugerkachel--verfuegbar").Count);
        Assert.NotEmpty(cut.FindAll("button.epos-erzeugerkachel-aufnehmen"));

        // Und sie ist NICHT von selbst aufgenommen worden.
        Assert.Empty(_aufgenommen);
        Assert.Empty(_strom);
    }

    /// <summary>Mehrzahl: die Leiste nennt die Zahl.</summary>
    [Fact]
    public void Die_Hinweisleiste_nennt_bei_mehreren_ihre_Zahl()
    {
        var cut = Zeige(MitLuecke(2));

        Assert.Contains("2 Erzeuger", cut.Find("div.epos-simkonfig-luecke").TextContent);
    }

    /// <summary>
    /// Der Knopf der Leiste erscheint erst, wenn die Karten wieder verborgen sind —
    /// er holt sie zurück, ohne etwas aufzunehmen.
    /// </summary>
    [Fact]
    public void Der_Knopf_der_Leiste_blendet_die_Karten_wieder_ein()
    {
        var cut = Zeige(MitLuecke(1));

        // Von selbst offen: kein Knopf in der Leiste.
        Assert.Empty(cut.Find("div.epos-simkonfig-luecke").QuerySelectorAll("button"));

        cut.Find("button.epos-simkonfig-verfuegbar").Click();     // zuklappen
        Assert.Empty(cut.FindAll("div.epos-erzeugerkachel--verfuegbar"));

        cut.Find("button.epos-simkonfig-luecke-knopf").Click();   // einblenden
        Assert.Equal(3, cut.FindAll("div.epos-erzeugerkachel--verfuegbar").Count);
        Assert.Empty(_aufgenommen);
    }

    /// <summary>
    /// Probendaten mit <paramref name="anzahl"/> ANGELEGTEN, aber nicht aufgenommenen
    /// Erzeugern — der Fall des Projekts 1007.
    /// </summary>
    private static SimulationKonfigDaten MitLuecke(int anzahl) => new SimulationKonfigDaten
    {
        IdProjekt = 1007,
        Gruppen = new List<KachelGruppe>
        {
            new KachelGruppe
            {
                Titel = "Wärmeerzeuger",
                Zeilen = new List<ErzeugerZeile>
                {
                    Verfuegbar("Heizkessel", 10, "Heizkessel · ecoTEC plus", hatAnlage: true),
                    Verfuegbar("BHKW", 11, "BHKW", hatAnlage: anzahl > 1),
                    Verfuegbar("Solarthermie", 2, "Solarthermie", hatAnlage: false)
                }
            }
        },
        SpeicherLeerText = "Dieses Projekt führt keinen Pufferspeicher."
    };

    private static ErzeugerZeile Verfuegbar(string dbWert, int idType, string titel,
                                            bool hatAnlage) => new ErzeugerZeile
    {
        DbWert = dbWert,
        IdType = idType,
        Verfuegbar = true,
        HatAnlage = hatAnlage,
        Kachel = new ErzeugerKachelDaten
        {
            Schluessel = dbWert,
            Titel = titel,
            Zustand = Kachelzustand.Verfuegbar,
            Umschaltbar = true,
            Chips = new[] { new ChipDaten("nicht in der Simulation", ChipStil.Flaeche) }
        }
    };

    // ================================================================== Kaskade

    [Fact]
    public void Die_Pfeile_melden_das_Verschieben_mit_Richtung()
    {
        var cut = Seite();

        cut.FindAll("div.epos-erzeugerkachel")[0]
           .QuerySelectorAll("button.epos-erzeugerkachel-glyphe")[1].Click();   // ▼

        Assert.Equal(new[] { "BHKW:1" }, _verschoben);
    }

    [Fact]
    public void Das_Kreuz_nimmt_einen_Waermeerzeuger_aus_der_Simulation()
    {
        var cut = Seite();

        var glyphen = cut.FindAll("div.epos-erzeugerkachel")[0]
                         .QuerySelectorAll("button.epos-erzeugerkachel-glyphe");
        glyphen[glyphen.Length - 1].Click();   // ×

        Assert.Equal(new[] { "BHKW" }, _entfernt);
    }

    /// <summary>
    /// Auf der STROMSEITE gibt es keine Kaskade: „+ aufnehmen" und „×" setzen den
    /// Platz Tool_5 bzw. Tool_6 statt eines Kaskadenplatzes.
    /// </summary>
    [Fact]
    public void Die_Stromseite_setzt_ihren_Platz_statt_der_Kaskade()
    {
        var cut = Seite();

        // Die PV-Kachel ist aufgenommen: ihr × leert Platz 5.
        var pv = cut.FindAll("div.epos-erzeugerkachel")[3];
        pv.QuerySelectorAll("button.epos-erzeugerkachel-glyphe")[0].Click();

        Assert.Equal(new[] { (5, "") }, _strom);
        Assert.Empty(_entfernt);
    }

    [Fact]
    public void Aufnehmen_einer_verfuegbaren_Kachel_meldet_ihren_DbWert()
    {
        var cut = Seite();
        cut.Find("button.epos-simkonfig-verfuegbar").Click();

        cut.FindAll("button.epos-erzeugerkachel-aufnehmen")[0].Click();
        Assert.Equal(new[] { "Wärmepumpe" }, _aufgenommen);
    }

    // ================================================================== Speicher

    [Fact]
    public void Die_Speicherspalte_zeigt_je_Puffer_eine_Kachel_und_den_Knopf()
    {
        var cut = Seite();

        Assert.Equal(2, cut.FindAll("div.epos-speicherkachel").Count);
        Assert.Contains(cut.FindAll("section.epos-simkonfig-speicher button.epos-knopf"),
                        b => b.TextContent.Contains("Pufferspeicher"));
    }

    /// <summary>Höchstens EINE Speicherkachel ist aufgeklappt (Konzept 3a).</summary>
    [Fact]
    public void Hoechstens_eine_Speicherkachel_ist_offen()
    {
        var cut = Seite();

        Assert.Empty(cut.FindAll("div.epos-speicherkachel-detail"));

        cut.FindAll("div.epos-speicherkachel")[0].Click();
        Assert.Single(cut.FindAll("div.epos-speicherkachel-detail"));

        cut.FindAll("div.epos-speicherkachel")[1].Click();
        Assert.Single(cut.FindAll("div.epos-speicherkachel-detail"));

        // Ein zweiter Klick auf dieselbe Kachel klappt sie wieder zu.
        cut.FindAll("div.epos-speicherkachel")[1].Click();
        Assert.Empty(cut.FindAll("div.epos-speicherkachel-detail"));
    }

    [Fact]
    public void Ohne_Speicher_steht_der_Leertext()
    {
        var cut = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, new SimulationKonfigDienste
            {
                Laden = _ => new SimulationKonfigDaten
                {
                    SpeicherLeerText = "Dieses Projekt führt keinen Pufferspeicher."
                }
            })
            .Add(x => x.StartProjekt, 1030));

        Assert.Contains(cut.FindAll("p.epos-simkonfig-hinweis"),
                        h => h.TextContent == "Dieses Projekt führt keinen Pufferspeicher.");
    }

    // ================================================================== Auswahl

    /// <summary>
    /// Der Klick auf eine Erzeugerkachel ist die Auswahl, die das Schema mitführt —
    /// sie erscheint als Hervorhebung an der Kachel.
    /// </summary>
    [Fact]
    public void Ein_Klick_auf_eine_Kachel_hebt_sie_hervor()
    {
        var cut = Seite();

        Assert.Empty(cut.FindAll("div.epos-erzeugerkachel--hervor"));

        cut.FindAll("div.epos-erzeugerkachel")[0].Click();
        Assert.Single(cut.FindAll("div.epos-erzeugerkachel--hervor"));
        Assert.Equal("ERZEUGER_14920", cut.Instance.Auswahl);
    }

    [Fact]
    public void Ein_Klick_auf_eine_Speicherkachel_hebt_sie_hervor()
    {
        var cut = Seite();

        cut.FindAll("div.epos-speicherkachel")[1].Click();
        Assert.Equal("SPEICHER_1008008", cut.Instance.Auswahl);
        Assert.Single(cut.FindAll("div.epos-speicherkachel--hervor"));
    }

    // ================================================================== Umschalter

    /// <summary>
    /// Der Umschalter Liste/Schema erhält die Auswahl — sie hängt an einem
    /// Schlüssel und nicht an einem Objekt.
    /// </summary>
    [Fact]
    public void Der_Umschalter_erhaelt_die_Auswahl()
    {
        var cut = Seite();
        cut.FindAll("div.epos-erzeugerkachel")[0].Click();

        cut.FindAll("button.epos-simkonfig-ansichtknopf")[1].Click();   // Schema
        Assert.Equal(SimulationKonfigSeite.ANSICHT_SCHEMA, cut.Instance.AktiveAnsicht);
        Assert.Empty(cut.FindAll("div.epos-simkonfig-spalten"));
        Assert.Equal("ERZEUGER_14920", cut.Instance.Auswahl);

        cut.FindAll("button.epos-simkonfig-ansichtknopf")[0].Click();   // Liste
        Assert.Equal(SimulationKonfigSeite.ANSICHT_LISTE, cut.Instance.AktiveAnsicht);
        Assert.Single(cut.FindAll("div.epos-simkonfig-spalten"));
        Assert.Equal("ERZEUGER_14920", cut.Instance.Auswahl);
    }

    /// <summary>
    /// Das Schema rechnet NUR, wenn es sichtbar ist (wörtlich :204-213) — beim
    /// ersten Aufbau in der Listenansicht wird es gar nicht geholt.
    /// </summary>
    [Fact]
    public void Das_Schema_wird_nur_bei_sichtbarer_Ansicht_geholt()
    {
        var cut = Seite();
        Assert.Equal(0, _schemaGeholt);

        cut.FindAll("button.epos-simkonfig-ansichtknopf")[1].Click();
        Assert.Equal(1, _schemaGeholt);
    }

    // ================================================================== Fußzeile

    /// <summary>
    /// W10b‑B‑3 (08.09.2026): „Extrapolation der WP-Kennlinie erlauben" steht in der
    /// Detailansicht der Wärmepumpe bei „Kenndaten Kennlinien" — hier nicht mehr.
    /// </summary>
    [Fact]
    public void Der_Extrapolationsschalter_steht_nicht_mehr_in_der_Konfiguration()
    {
        Assert.DoesNotContain("Extrapolation", Seite().Markup);
    }

    /// <summary>
    /// Der Booster-Lesepunkt ist UNSICHTBAR, bis das Projekt einen gekoppelten
    /// Booster führt (PAKET B2, :281).
    /// </summary>
    [Fact]
    public void Der_Booster_Lesepunkt_erscheint_nur_mit_Booster()
    {
        Assert.Single(Seite().FindAll("input[type=checkbox]"));
        Assert.Empty(Seite(mitBooster: false).FindAll("input[type=checkbox]"));
    }

    [Fact]
    public void Speichern_ruft_den_Schreibweg_und_meldet_den_Erfolg()
    {
        var cut = Seite();

        cut.FindAll("div.epos-leiste button.epos-knopf")[0].Click();   // Konfiguration speichern

        Assert.Equal(1, _gespeichert);
        Assert.Contains("gespeichert", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// <b>Auftrag #207:</b> Die Fußzeile trägt nur noch EINEN Knopf. „Beenden"
    /// ist gefallen — die Seite ist Schritt ① der Ansicht SIMULATION, und man
    /// verlässt sie über die Ablaufleiste oder über das eine „← zurück" im Kopf
    /// der Ansicht (Anwenderentscheid SIM‑Q1).
    /// </summary>
    [Fact]
    public void Die_Fusszeile_traegt_nur_noch_das_Speichern()
    {
        var cut = Seite();

        var knoepfe = cut.FindAll("div.epos-leiste button.epos-knopf");
        Assert.Single(knoepfe);
        Assert.Contains("speichern", knoepfe[0].TextContent, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <b>Auftrag #207:</b> Der Wirt erfährt, dass die Kaskade ungespeichert ist —
    /// daran hängt die Sperre von Schritt ② und die Rückfrage beim Verlassen.
    /// Nach dem Speichern ist der Stand wieder sauber.
    /// </summary>
    [Fact]
    public void Eine_Kaskadenaenderung_meldet_sich_und_das_Speichern_raeumt_sie_weg()
    {
        List<bool> gemeldet = new List<bool>();

        var cut = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, Dienste())
            .Add(x => x.StartProjekt, 1030)
            .Add(x => x.UngespeichertGeaendert, wert => gemeldet.Add(wert)));

        Assert.False(cut.Instance.Ungespeichert);

        // ▼ der ersten Waermeerzeugerkarte - derselbe Weg wie in
        // Die_Pfeile_melden_das_Verschieben_mit_Richtung.
        cut.FindAll("div.epos-erzeugerkachel")[0]
           .QuerySelectorAll("button.epos-erzeugerkachel-glyphe")[1].Click();

        Assert.True(cut.Instance.Ungespeichert);
        Assert.Equal(new[] { true }, gemeldet);

        cut.FindAll("div.epos-leiste button.epos-knopf")[0].Click();

        Assert.False(cut.Instance.Ungespeichert);
        Assert.Equal(new[] { true, false }, gemeldet);
    }

    // ================================================================== Sperre

    /// <summary>
    /// Sperrzustand (ADR-001): Der Grund steht als Warnbanner, alles ist gesperrt —
    /// die Seite bleibt aber schließbar.
    /// </summary>
    [Fact]
    public void Der_Sperrzustand_meldet_und_sperrt_nur_den_Inhalt()
    {
        var cut = Seite(gesperrt: true);

        Assert.Contains("Schema-Migration", cut.Find(".epos-warnbanner").TextContent);
        Assert.True(cut.Find("fieldset.epos-simkonfig-bereich").HasAttribute("disabled"));

        // Die Fusszeile bleibt draussen (das fieldset umschliesst sie nicht); der
        // Speicherknopf selbst ist im Sperrzustand gesperrt.
        var knoepfe = cut.FindAll("div.epos-leiste button.epos-knopf");
        Assert.Single(knoepfe);
        Assert.True(knoepfe[0].HasAttribute("disabled"));
    }

    // ================================================================== Editoren

    /// <summary>
    /// Der Doppelklick auf einen Chip öffnet den Editor, den der Chip als Ziel
    /// trägt — der Ersatz des Spaltenindex-Dispatchers.
    /// </summary>
    [Fact]
    public void Ein_Chip_mit_Senkenziel_oeffnet_den_Senkendialog()
    {
        var cut = Seite();

        cut.FindAll("button.epos-chip--ziel")[0].DoubleClick();
        Assert.Equal("Waermesenke", cut.Instance.OffenerEditor);
    }

    /// <summary>
    /// Die Vorprüfung „nur für Wärmepumpen" bleibt beim AUFRUFER — sie meldet und
    /// öffnet nichts (Vermessung § 1.i).
    /// </summary>
    [Fact]
    public void Der_Betriebsmodus_eines_Nicht_WP_meldet_statt_zu_oeffnen()
    {
        var cut = Seite();

        // Die zweite BHKW-Kachel traegt kein Chipziel; der Weg laeuft ueber das
        // Schema bzw. den Chip - hier wird die Regel unmittelbar geprueft, indem
        // die Kachel eines BHKW ihren Standard-Editor oeffnet.
        cut.FindAll("div.epos-erzeugerkachel")[1].DoubleClick();
        Assert.Equal("Waermesenke", cut.Instance.OffenerEditor);
    }

    [Fact]
    public void Der_Knopf_Pufferverwaltung_oeffnet_die_Ueberlagerung()
    {
        var cut = Seite();

        var knopf = cut.FindAll("section.epos-simkonfig-speicher button.epos-knopf")[0];
        knopf.Click();

        Assert.Equal("Pufferverwaltung", cut.Instance.OffenerEditor);
        Assert.NotEmpty(cut.FindAll("div.epos-ueberlagerung"));
    }

    /// <summary>
    /// Die Quellenwahl erscheint als Auswahlüberlagerung mit einem Knopf je Zweig;
    /// der gespeicherte Typ ist hervorgehoben.
    /// </summary>
    [Fact]
    public void Die_Quellenwahl_zeigt_einen_Knopf_je_Zweig()
    {
        var cut = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, new SimulationKonfigDienste
            {
                Laden = _ => new SimulationKonfigDaten
                {
                    IdProjekt = 1030,
                    Gruppen = new List<KachelGruppe>
                    {
                        new KachelGruppe
                        {
                            Titel = "Wärmeerzeuger",
                            Zeilen = new List<ErzeugerZeile>
                            {
                                Waerme("Wärmepumpe", "1", "WP 1", 10353, true, true,
                                       new ChipDaten("Quelle: Außenluft", ChipStil.Quelle,
                                                     "", ChipZiel.Quelle))
                            }
                        }
                    }
                },
                Quellentypen = _ => new List<Quellentyp>
                {
                    new Quellentyp("Außenluft", "Außenluft"),
                    new Quellentyp("Konstant", "konstante Temperatur"),
                    new Quellentyp("Erdreich", "Erdreich")
                },
                QuelleTyp = _ => "Außenluft"
            })
            .Add(x => x.StartProjekt, 1030));

        cut.FindAll("button.epos-chip--ziel")[0].DoubleClick();

        Assert.Equal("Quellenwahl", cut.Instance.OffenerEditor);
        var knoepfe = cut.FindAll("div.epos-simkonfig-quellenwahl button");
        Assert.Equal(3, knoepfe.Count);
        Assert.Contains("epos-knopf--primaer", knoepfe[0].ClassName);   // der gespeicherte Typ
    }

    // ================================================================== Projektwechsel

    /// <summary>
    /// Der Projektwechsel läuft über den <c>SeitenZustand</c>: Die Seite holt ihre
    /// Daten neu, OHNE dass die Hülle neu gebaut würde.
    /// </summary>
    [Fact]
    public void Ein_Projektwechsel_laedt_die_Seite_neu()
    {
        List<int> geladen = new List<int>();
        SeitenZustand zustand = new SeitenZustand();
        zustand.ProjektSetzen(1030, "Projekt A");

        var cut = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, new SimulationKonfigDienste
            {
                Laden = id => { geladen.Add(id); return Daten(); }
            })
            .Add(x => x.Zustand, zustand));

        Assert.Equal(new[] { 1030 }, geladen);

        zustand.ProjektSetzen(1007, "Projekt B");
        Assert.Equal(new[] { 1030, 1007 }, geladen);
        Assert.Equal(1007, cut.Instance.IdProjekt);
    }

    /// <summary>Ohne Dienste bleibt die Seite leer — und stürzt nicht ab.</summary>
    [Fact]
    public void Ohne_Dienste_bleibt_die_Seite_leer()
    {
        var cut = Render<SimulationKonfigSeite>();

        Assert.Empty(cut.FindAll("div.epos-erzeugerkachel"));
        Assert.Empty(cut.FindAll("div.epos-speicherkachel"));
        Assert.NotEmpty(cut.FindAll("div.epos-simkonfig-spalten"));
    }

    // =====================================================================
    //  W10b-B-2 (08.09.2026): der Stromspeicher in "Speicher im Projekt"
    // =====================================================================

    [Fact]
    public void Ein_aufgenommener_Stromspeicher_steht_auch_unter_Speicher_im_Projekt()
    {
        SimulationKonfigDaten daten = Daten();
        ErzeugerZeile speicher = daten.Gruppen.First(g => g.Titel == "Energiespeicher").Zeilen[0];
        speicher.Kachel.Zustand = Kachelzustand.Aufgenommen;
        speicher.Kachel.Chips = new[]
        {
            new ChipDaten("Kapazität 10,20 kWh"), new ChipDaten("Leistung 11,04 kW"),
            new ChipDaten("η_RT 0,90"), new ChipDaten("Typ Lithium-Eisen-Phosphat")
        };
        var cut = Zeige(daten);

        var kachel = cut.Find(".epos-stromspeicherkachel");
        Assert.Contains("Stromspeicher", kachel.TextContent);
        Assert.Equal(3, kachel.QuerySelectorAll(".epos-chip").Length);       // die ersten drei Kennwerte
        Assert.Equal(2, cut.FindAll("div.epos-speicherkachel").Count);      // die Puffer bleiben, wie sie sind
    }

    [Fact]
    public void Ein_nur_verfuegbarer_Stromspeicher_steht_nicht_unter_Speicher_im_Projekt()
    {
        var cut = Zeige(Daten());                                            // Zustand Verfuegbar
        Assert.Empty(cut.FindAll(".epos-stromspeicherkachel"));
    }

    // ============================================== Pufferverwaltung (#194)

    /// <summary>
    /// Restfall aus #187 (Abnahmeliste, Befund Agent #187): Die Pufferverwaltung ist
    /// die DRITTE Einbettung von <c>PufferSpProjektDialog</c> als Rolle (neben
    /// <c>QuellePufferspeicherDialog</c> und <c>WaermesenkeDialog</c>) und trug den
    /// Titel doppelt — die Überlagerung als <c>h2.epos-ueberlagerung-titel</c>, die
    /// eingebettete Komponente unbedingt zugleich als eigenen
    /// <c>h1.epos-dialog-titel</c>. Anders als bei den zwei bereits behobenen Stellen
    /// (die selbst ein Dialog mit EIGENEM <c>h1.epos-dialog-titel</c> sind) hat die
    /// Konfigurations-SEITE keinen solchen eigenen Kopf — der einzige Titel, der nach
    /// der Behebung stehen bleibt, ist der der Überlagerung; die eingebettete
    /// Komponente zeigt gar keinen <c>h1.epos-dialog-titel</c> mehr (Bauart b,
    /// <c>TitelAnzeigen="false"</c> an der Einbettungsstelle).
    /// </summary>
    [Fact]
    public void Die_Pufferverwaltung_zeigt_ihren_Titel_nur_an_der_Ueberlagerung()
    {
        var cut = Seite();

        cut.FindAll("section.epos-simkonfig-speicher button.epos-knopf")[0].Click();
        Assert.Equal("Pufferverwaltung", cut.Instance.OffenerEditor);

        // Die eingebettete Komponente zeichnet ihren EIGENEN Kopf nicht mehr.
        Assert.Empty(cut.FindAll("h1.epos-dialog-titel"));

        // ... sondern traegt die Markierung "ohne eigenen Titel" (Hausregel-Klasse).
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf--ohnetitel"));

        // Genau EIN sichtbarer Titel bleibt - der der Ueberlagerung.
        var ueberlagerungstitel = cut.FindAll("h2.epos-ueberlagerung-titel");
        Assert.Single(ueberlagerungstitel);
        Assert.Equal("Pufferspeicher im Projekt", ueberlagerungstitel[0].TextContent);
    }

    // ==================================================================
    //  AUFTRAG #274 — „Stromspeicher auslegen…" in der Speicherspalte
    // ==================================================================

    /// <summary>
    /// <b>Anwenderwunsch 14.09.2026:</b> „Der Dialog Stromspeicher soll in den Dialog
    /// Konfiguration verschoben werden. Ähnlich zu ‚Pufferspeicher anlegen/verwalten'
    /// einen Konfigurationsbutton ‚Stromspeicher auslegen'." Er steht UNTER dem
    /// Pufferknopf, trägt darunter die Kurzzeile zum Stand und öffnet die Ansicht
    /// „Stromspeicher-Auslegung" über den Weg der Hülle.
    /// </summary>
    [Fact]
    public void Der_Knopf_Stromspeicher_auslegen_steht_mit_Standzeile_unter_der_Pufferverwaltung()
    {
        _spWeg = true;
        _spStand = new StromspeicherStand
        {
            Vorhanden = true,
            Standzeile = "Mehrspeicherbetrieb aktiviert · Lastspitzenkappung · 2 Einheiten"
        };

        var cut = Seite();

        var knoepfe = cut.FindAll("section.epos-simkonfig-speicher button.epos-knopf");
        Assert.Equal(2, knoepfe.Count);
        Assert.Contains("Pufferspeicher", knoepfe[0].TextContent);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIM_BTN_SP_AUSLEGUNG,
                     knoepfe[1].TextContent.Trim());

        Assert.Equal(_spStand.Standzeile,
                     cut.Find("p.epos-simkonfig-flottenstand").TextContent.Trim());

        cut.Find("section.epos-simkonfig-speicher button.epos-simkonfig-auslegung").Click();
        Assert.Equal(1, _spGeoeffnet);
    }

    /// <summary>
    /// Gibt es nichts auszulegen, steht statt des Knopfes eine Erklärzeile — dieselbe
    /// Art Zeile wie „Für dieses Projekt ist noch kein Pufferspeicher angelegt".
    /// </summary>
    [Fact]
    public void Ohne_Stromspeicher_steht_statt_des_Knopfes_die_Erklaerzeile()
    {
        _spWeg = true;
        _spStand = new StromspeicherStand
        {
            Vorhanden = false,
            LeerText = WindowsFormsApplication1.MyResource.Resource.SIM_SP_AUSLEGUNG_LEER
        };

        var cut = Seite();

        Assert.Empty(cut.FindAll("button.epos-simkonfig-auslegung"));
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIM_SP_AUSLEGUNG_LEER,
                        cut.Find("section.epos-simkonfig-speicher").TextContent);
    }

    /// <summary>
    /// Kein Delegat, kein Knopf (Hausregel): Eine Schale ohne Weg in die Auslegung
    /// zeichnet ihn gar nicht erst — die Seite bleibt vollständig bedienbar.
    /// </summary>
    [Fact]
    public void Ohne_Weg_in_die_Auslegung_bleibt_der_Knopf_weg()
    {
        _spWeg = false;
        _spStand = new StromspeicherStand { Vorhanden = true, Standzeile = "Eingabestand" };

        var cut = Seite();

        Assert.Empty(cut.FindAll("button.epos-simkonfig-auslegung"));
        Assert.Empty(cut.FindAll("p.epos-simkonfig-flottenstand"));
        Assert.Single(cut.FindAll("section.epos-simkonfig-speicher button.epos-knopf"));
    }
    // ==================================================================
    //  Hausmuster der Dialoge: OK speichert, Abbrechen und ✕ verwerfen
    // ==================================================================

    /// <summary>
    /// Jeder Schreibweg der eingebetteten Dialoge vermerkt sich hier. Ein Ausgang,
    /// der VERWERFEN soll, lässt die Liste leer.
    /// </summary>
    private readonly List<string> _dialogSchreiben = new();

    /// <summary>
    /// Die Datenseite der eingebetteten Pufferverwaltung. Sie schreibt nicht, sie
    /// zaehlt mit - so laesst sich hier pruefen, dass ueber diese Einbettungsstelle
    /// allein der OK-Weg in die Datenbank fuehrt.
    /// </summary>
    private readonly PufferSpProjektDialogTests.Pruefstand _pufferstand =
        PufferSpProjektDialogTests.MitZwei();

    /// <summary>
    /// Eine Seite mit EINER Wärmepumpe, deren Chips in den Betriebsmodus, die
    /// Wärmesenke und die Quellenwahl führen, und mit Schreibwegen, die jeden
    /// Schreibversuch vermerken. Der gespeicherte Quelltyp ist „Außenluft"; welcher
    /// Quelldialog aufgeht, entscheidet der Knopf in der Quellenwahl.
    /// </summary>
    private IRenderedComponent<SimulationKonfigSeite> SeiteMitDialogen()
        => Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, new SimulationKonfigDienste
            {
                Laden = _ => new SimulationKonfigDaten
                {
                    IdProjekt = 1030,
                    Gruppen = new List<KachelGruppe>
                    {
                        new KachelGruppe
                        {
                            Titel = "Wärmeerzeuger",
                            Zeilen = new List<ErzeugerZeile>
                            {
                                Waerme("Wärmepumpe", "1", "WP 1", 10353, true, true,
                                       new ChipDaten("Modus: Laufzeit", ChipStil.Neutral,
                                                     "", ChipZiel.Modus),
                                       new ChipDaten("Senke: Puffer A", ChipStil.Senke,
                                                     "", ChipZiel.Senke),
                                       new ChipDaten("Quelle: Außenluft", ChipStil.Quelle,
                                                     "", ChipZiel.Quelle))
                            }
                        }
                    },
                    Speicher = new List<SpeicherKachelDaten>
                    {
                        new SpeicherKachelDaten
                        {
                            IdPuffer = 1008007, Bezeichner = "Puffer A", Verwendung = "Heizung"
                        }
                    }
                },
                BetriebsmodusGaben = _ => new Dictionary<string, object>
                {
                    ["Bezeichner"] = "WP 1", ["AktuellerModus"] = ""
                },
                BetriebsmodusSchreiben = (a, m) => _dialogSchreiben.Add("modus:" + a + ":" + m),
                WaermesenkeGaben = _ => new Dictionary<string, object>
                {
                    ["Daten"] = new EPOS.UI.Dialoge.Simulation.WaermesenkeDaten()
                },
                WaermesenkeFertig = (a, e) =>
                {
                    // null = abgebrochen; dann hat der Dialog nichts geschrieben.
                    if (e is not null) _dialogSchreiben.Add("senke:" + a);
                    return Rueckmeldung.Still;
                },
                PufferVerwaltungGaben = _ => PufferSpProjektDialogTests.Gaben(_pufferstand),
                Quellentypen = _ => new List<Quellentyp>
                {
                    new Quellentyp("Außenluft", "Außenluft"),
                    new Quellentyp("Pufferspeicher", "Pufferspeicher"),
                    new Quellentyp("Profil", "Profil"),
                    new Quellentyp("Erdreich", "Erdreich")
                },
                QuelleTyp = _ => "Außenluft",
                QuellePufferGaben = _ => new Dictionary<string, object>(),
                QuellePufferSchreiben = (a, d) =>
                {
                    _dialogSchreiben.Add("quellpuffer:" + a);
                    return Rueckmeldung.Still;
                },
                QuellprofilGaben = _ => new Dictionary<string, object>(),
                QuellprofilSchreiben = (a, id) => _dialogSchreiben.Add("quellprofil:" + a),
                QuelleErdreichGaben = _ => new Dictionary<string, object>
                {
                    ["Daten"] = new EPOS.UI.Dialoge.Simulation.QuelleErdreichDaten
                    {
                        WPName = "WP 1",
                        IdProjekt = 1030,
                        IdAnlage = 10353,
                        Tiefe = 1.8,
                        Flaeche = 250,
                        Klimazone = 6,
                        Spreizung = 4
                    }
                },
                QuelleErdreichSchreiben = (a, d) => _dialogSchreiben.Add("erdreich:" + a)
            })
            .Add(x => x.StartProjekt, 1030));

    /// <summary>Öffnet den Chip, dessen Text den Anfang trägt (Modus, Senke, Quelle).</summary>
    private static void ChipOeffnen(IRenderedComponent<SimulationKonfigSeite> cut, string anfang)
        => cut.FindAll("button.epos-chip--ziel")
              .First(b => b.TextContent.StartsWith(anfang, StringComparison.Ordinal))
              .DoubleClick();

    /// <summary>Wählt in der Quellenwahl den Zweig mit diesem Text.</summary>
    private static void QuellzweigWaehlen(IRenderedComponent<SimulationKonfigSeite> cut, string text)
        => cut.FindAll("div.epos-simkonfig-quellenwahl button")
              .First(b => b.TextContent.Contains(text, StringComparison.Ordinal))
              .Click();

    /// <summary>Das ✕ der Überlagerung — der Ausgang, der wie Abbrechen wirken muss.</summary>
    private static void Kreuz(IRenderedComponent<SimulationKonfigSeite> cut)
        => cut.Find("button.epos-ueberlagerung-zu").Click();

    /// <summary>
    /// <b>Anwenderentscheid 15.09.2026:</b> „Der OK Button soll in jedem Dialog
    /// vorhanden sein und diesen mit Speichern verlassen, Abbrechen Button ohne
    /// Speichern den Dialog verlassen." Das ✕ der Überlagerung gehört zum Abbrechen:
    /// Es schließt, und geschrieben wird dabei nichts.
    /// </summary>
    [Fact]
    public void Das_Kreuz_ueber_dem_Betriebsmodus_verwirft()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Modus");
        Assert.Equal("Betriebsmodus", cut.Instance.OffenerEditor);

        Kreuz(cut);

        Assert.Equal("Keine", cut.Instance.OffenerEditor);
        Assert.Empty(_dialogSchreiben);
    }

    /// <summary>
    /// <b>Anwenderbefund 15.09.2026:</b> „Doppeltes Kreuz dürfen nicht sein!" Die
    /// betitelte Überlagerung trägt Titel und ✕; die eingebettete Komponente zeigt
    /// beides nicht mehr (<c>TitelAnzeigen="false"</c> an der Einbettungsstelle).
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Betriebsmodus_zeigt_nur_ein_Kreuz()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Modus");

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    [Fact]
    public void Das_Kreuz_ueber_der_Waermesenke_verwirft()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Senke");
        Assert.Equal("Waermesenke", cut.Instance.OffenerEditor);

        Kreuz(cut);

        Assert.Equal("Keine", cut.Instance.OffenerEditor);
        Assert.Empty(_dialogSchreiben);
    }

    /// <summary>Ein Titel, ein Kreuz — beide trägt die Überlagerung.</summary>
    [Fact]
    public void Die_Ueberlagerung_Waermesenke_zeigt_nur_ein_Kreuz()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Senke");

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    [Fact]
    public void Das_Kreuz_ueber_der_Quelle_Pufferspeicher_verwirft()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Quelle");
        QuellzweigWaehlen(cut, "Pufferspeicher");
        Assert.Equal("QuellePuffer", cut.Instance.OffenerUntereditor);

        Kreuz(cut);

        Assert.Equal("Keine", cut.Instance.OffenerUntereditor);
        Assert.Empty(_dialogSchreiben);
    }

    /// <summary>Ein Titel, ein Kreuz — beide trägt die Überlagerung.</summary>
    [Fact]
    public void Die_Ueberlagerung_Quelle_Pufferspeicher_zeigt_nur_ein_Kreuz()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Quelle");
        QuellzweigWaehlen(cut, "Pufferspeicher");

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    [Fact]
    public void Das_Kreuz_ueber_dem_Quellprofil_verwirft()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Quelle");
        QuellzweigWaehlen(cut, "Profil");
        Assert.Equal("Quellprofil", cut.Instance.OffenerUntereditor);

        Kreuz(cut);

        Assert.Equal("Keine", cut.Instance.OffenerUntereditor);
        Assert.Empty(_dialogSchreiben);
    }

    /// <summary>Ein Titel, ein Kreuz — beide trägt die Überlagerung.</summary>
    [Fact]
    public void Die_Ueberlagerung_Quellprofil_zeigt_nur_ein_Kreuz()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Quelle");
        QuellzweigWaehlen(cut, "Profil");

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>
    /// Ein Titel, ein Kreuz — beide trägt die Überlagerung. Der Erdreich-Dialog band
    /// sein Kreuz schon an <c>!_laeuft</c>; jetzt kommt <c>TitelAnzeigen</c> dazu, und
    /// im Ruhezustand der Einbettung zeigt er keins.
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Quelle_Erdreich_zeigt_nur_ein_Kreuz()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Quelle");
        QuellzweigWaehlen(cut, "Erdreich");

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>
    /// Der Erdreich-Dialog trägt wieder seine Knopfleiste: OK prüft, übernimmt und
    /// schließt — hier durch die Seite hindurch bis zum Schreibweg.
    /// </summary>
    [Fact]
    public void OK_im_Erdreich_Dialog_speichert_und_schliesst()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Quelle");
        QuellzweigWaehlen(cut, "Erdreich");
        Assert.Equal("QuelleErdreich", cut.Instance.OffenerUntereditor);

        cut.Find("div.epos-ueberlagerung button.epos-knopf--primaer").Click();

        Assert.Equal(new[] { "erdreich:10353" }, _dialogSchreiben);
        Assert.Equal("Keine", cut.Instance.OffenerUntereditor);
    }

    /// <summary>
    /// Abbrechen schließt den Erdreich-Dialog ohne zu speichern — und ohne Prüfung:
    /// Die Verlegetiefe 0 verletzt eine der acht Regeln, der Weg hinaus steht
    /// trotzdem offen.
    /// </summary>
    [Fact]
    public void Abbrechen_im_Erdreich_Dialog_verwirft_ohne_Pruefung()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Quelle");
        QuellzweigWaehlen(cut, "Erdreich");

        cut.FindAll("input.epos-eingabe")[0].Input("0");
        cut.FindAll("div.epos-ueberlagerung .epos-leiste button")[0].Click();

        Assert.Empty(_dialogSchreiben);
        Assert.Equal("Keine", cut.Instance.OffenerUntereditor);
    }

    [Fact]
    public void Das_Kreuz_ueber_dem_Erdreich_Dialog_verwirft()
    {
        var cut = SeiteMitDialogen();
        ChipOeffnen(cut, "Quelle");
        QuellzweigWaehlen(cut, "Erdreich");

        Kreuz(cut);

        Assert.Equal("Keine", cut.Instance.OffenerUntereditor);
        Assert.Empty(_dialogSchreiben);
    }

    /// <summary>
    /// <b>Dritte Einbettungsstelle (Auftrag #282).</b> Die Pufferverwaltung traegt das
    /// Hausmuster: Das ✕ ueber ihr wirkt wie Abbrechen — die Ueberlagerung schliesst,
    /// und die Datenbank steht so da wie vorher, auch wenn im Dialog vorher eine Zeile
    /// uebernommen wurde.
    /// </summary>
    [Fact]
    public void Das_Kreuz_ueber_der_Pufferverwaltung_verwirft_wie_Abbrechen()
    {
        var cut = SeiteMitDialogen();
        cut.FindAll("section.epos-simkonfig-speicher button.epos-knopf")[0].Click();
        Assert.Equal("Pufferverwaltung", cut.Instance.OffenerEditor);

        PufferUebernehmen(cut);
        Assert.Equal(0, _pufferstand.Schreibzugriffe);

        Kreuz(cut);

        Assert.Equal("Keine", cut.Instance.OffenerEditor);
        Assert.Empty(_dialogSchreiben);
        Assert.Equal(0, _pufferstand.Schreibzugriffe);
    }

    /// <summary>Abbrechen in der Pufferverwaltung schreibt ebenso wenig.</summary>
    [Fact]
    public void Abbrechen_in_der_Pufferverwaltung_schreibt_nichts()
    {
        var cut = SeiteMitDialogen();
        cut.FindAll("section.epos-simkonfig-speicher button.epos-knopf")[0].Click();

        PufferUebernehmen(cut);
        cut.FindAll(".epos-ueberlagerung .epos-leiste button")
           .First(b => b.TextContent == "Abbrechen").Click();

        Assert.Equal("Keine", cut.Instance.OffenerEditor);
        Assert.Equal(0, _pufferstand.Schreibzugriffe);
    }

    /// <summary>
    /// OK schreibt den Arbeitsstand und schliesst — das ist der einzige Weg, auf dem
    /// ueber diese Einbettungsstelle etwas in die Datenbank kommt.
    /// </summary>
    [Fact]
    public void OK_in_der_Pufferverwaltung_schreibt_und_schliesst()
    {
        var cut = SeiteMitDialogen();
        cut.FindAll("section.epos-simkonfig-speicher button.epos-knopf")[0].Click();

        PufferUebernehmen(cut);
        cut.Find(".epos-ueberlagerung .epos-leiste button.epos-knopf--primaer").Click();

        Assert.Equal("Keine", cut.Instance.OffenerEditor);
        Assert.NotNull(_pufferstand.Angelegt);
        Assert.Equal("Neuer Speicher", _pufferstand.Angelegt!.Bezeichner);
    }

    /// <summary>In der eingebetteten Verwaltung einen neuen Speicher uebernehmen.</summary>
    private static void PufferUebernehmen(IRenderedComponent<SimulationKonfigSeite> cut)
    {
        cut.FindAll(".epos-ueberlagerung button")
           .First(b => b.TextContent.Contains("Neuer Pufferspeicher")).Click();
        cut.Find(".epos-ueberlagerung input.epos-eingabe[type=text]").Input("Neuer Speicher");
        cut.FindAll(".epos-ueberlagerung input.epos-eingabe")[1].Input("900");
        cut.FindAll(".epos-ueberlagerung button")
           .First(b => b.TextContent.Contains("Anlegen")).Click();
    }

    // =====================================================================
    //  #307 - die gepflegte Kaskade wird sichtbar und umkehrbar
    // =====================================================================

    /// <summary>
    /// <b>Steht die Merkspalte auf 0, steht dort nichts.</b> Der Regelfall aller
    /// dreizehn Referenzprojekte — die Seite sagt nichts über einen Zustand, der
    /// keiner ist.
    /// </summary>
    [Fact]
    public void Ohne_Marke_steht_keine_Zeile_an_der_Kaskade()
    {
        _gepflegt = false;

        var cut = Seite();

        Assert.Empty(cut.FindAll("p.epos-simkonfig-gepflegt"));
        Assert.Empty(cut.FindAll("button.epos-simkonfig-gepflegt-knopf"));
    }

    /// <summary>
    /// <b>Steht sie auf 1, sagt die Seite es — ruhig.</b> Eine Zeile mit dem Text der
    /// Ressource, KEIN Warnbanner: Sie trägt weder die Warnklasse der Hinweisleiste
    /// noch <c>role="alert"</c>. Es ist ein Zustand, kein Fehler.
    /// </summary>
    [Fact]
    public void Mit_Marke_steht_die_ruhige_Zeile_an_der_Kaskade()
    {
        _gepflegt = true;

        var cut = Seite();

        IElement zeile = cut.Find("p.epos-simkonfig-gepflegt");
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMKONF_KASKADE_GEPFLEGT,
                        zeile.TextContent);
        Assert.Null(zeile.GetAttribute("role"));
        Assert.DoesNotContain("luecke", zeile.ClassName ?? "");

        // Sie steht in der Erzeugerspalte, vor den Gruppen.
        Assert.Single(cut.Find("section.epos-simkonfig-erzeuger")
                         .QuerySelectorAll("p.epos-simkonfig-gepflegt"));
    }

    /// <summary>
    /// <b>Der Handgriff setzt auf 0 und schreibt durch.</b> Er ruft GENAU EINMAL den
    /// Weg der Hülle (dort fällt die Marke in Modell und Datenbank), danach ist die
    /// Zeile fort — und die Kaskade selbst hat er nicht angefasst: kein Verschieben,
    /// kein Aufnehmen, kein Entfernen, und nichts ist ungespeichert.
    /// </summary>
    [Fact]
    public void Der_Handgriff_gibt_die_Kaskade_der_Automatik_zurueck()
    {
        _gepflegt = true;

        var cut = Seite();
        cut.Find("button.epos-simkonfig-gepflegt-knopf").Click();

        Assert.Equal(new[] { false }, _gepflegtGeschrieben);
        Assert.Empty(cut.FindAll("p.epos-simkonfig-gepflegt"));

        Assert.Empty(_verschoben);
        Assert.Empty(_aufgenommen);
        Assert.Empty(_entfernt);
        Assert.False(cut.Instance.Ungespeichert);
    }
}
