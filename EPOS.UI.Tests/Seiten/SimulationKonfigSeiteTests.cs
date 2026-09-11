using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
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
            BoosterSichtbar = mitBooster,
            BoosterDavor = true,
            PvGewaehlt = true
        };
    }

    private SimulationKonfigDienste Dienste(bool gesperrt = false, bool mitBooster = true)
        => new SimulationKonfigDienste
        {
            Laden = _ => Daten(gesperrt, mitBooster),
            SchemaLaden = _ => { _schemaGeholt++; return SchemaBild.Leer; },
            Verschieben = (w, r) => _verschoben.Add(w + ":" + r),
            Aufnehmen = w => _aufgenommen.Add(w),
            Entfernen = w => _entfernt.Add(w),
            StromAuswahl = (p, w) => _strom.Add((p, w)),
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
            Heizstab = true,
            Bereitschaft = 8000
        },
        NetzverlusteSchreiben = (w, e) => _geschrieben.Add("netz:" + w + e),
        BetriebsartSchreiben = w => _geschrieben.Add("betriebsart:" + w),
        LeistungsgrenzeSchreiben = w => _geschrieben.Add("grenze:" + w),
        HeizstabSchreiben = w => _geschrieben.Add("heizstab:" + w),
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
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMKONF_GRP_WAERMEBEDARF,
                        abschnitt.TextContent);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_LBL_NETZVERLUSTE,
                        abschnitt.TextContent);
        Assert.Contains("nur", abschnitt.TextContent.ToLowerInvariant());

        IElement feld = abschnitt.QuerySelectorAll("input")[0];
        Assert.Equal("4", feld.GetAttribute("value"));

        feld.Input("7");
        Assert.Equal(new[] { "netz:7%" }, _geschrieben);
    }

    /// <summary>Ohne Parametersatz steht der Abschnitt gar nicht da.</summary>
    [Fact]
    public void Ohne_Parametersatz_bleibt_der_Waermebedarfsabschnitt_weg()
    {
        var seite = Seite();

        Assert.Empty(seite.FindAll("section.epos-simkonfig-bedarf"));
        Assert.Empty(seite.FindAll("div.epos-erzeugerkachel-parameter"));
    }

    /// <summary>
    /// Die drei Erzeugerwerte stehen an DER Karte, die sie betreffen — BHKW
    /// (Betriebsart und untere Leistungsgrenze), Wärmepumpe (Heizstab),
    /// Heizkessel (Betriebsbereitschaft). Und jeder schreibt sofort.
    /// </summary>
    [Fact]
    public void Jeder_Erzeugerparameter_steht_an_seiner_Karte_und_schreibt_sofort()
    {
        var seite = SeiteMitParametern();

        // Die Waermepumpe ist im Probenprojekt VERFUEGBAR und damit zugeklappt -
        // der Textschalter am Spaltenende holt sie hervor.
        seite.Find("button.epos-simkonfig-verfuegbar").Click();

        IElement bhkw = Karte(seite, "BHKW · Modul 1");
        IElement kessel = Karte(seite, "Heizkessel");
        IElement wp = Karte(seite, "Wärmepumpe");

        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_GRP_BETRIEBSART,
                        bhkw.TextContent);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_LBL_UNTERE_LEISTUNGSGRENZE,
                        bhkw.TextContent);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_CHK_HEIZSTAB,
                        wp.TextContent);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_LBL_BEREITSCHAFT,
                        kessel.TextContent);

        // Die Betriebsart steht auf „stromgefuehrt" (1) - der dritte Knopf waehlt 2.
        bhkw.QuerySelectorAll("input[type='radio']")[2].Change(true);
        wp.QuerySelector("input[type='checkbox']")!.Change(false);
        kessel.QuerySelectorAll("input")[0].Input("7500");

        Assert.Equal(new[] { "betriebsart:2", "heizstab:False", "bereitschaft:7500" },
                     _geschrieben);
    }

    /// <summary>
    /// NUR AN DER ERSTEN KARTE IHRER ART: Das Projekt führt zwei BHKW-Module, die
    /// fünf Werte gelten aber projektweit — an beiden Karten stünde dieselbe
    /// Betriebsart zweimal.
    /// </summary>
    [Fact]
    public void Ein_zweites_Modul_derselben_Art_traegt_keinen_zweiten_Parameterblock()
    {
        var seite = SeiteMitParametern();

        Assert.Empty(Karte(seite, "BHKW · Modul 2")
                         .QuerySelectorAll("div.epos-erzeugerkachel-parameter"));

        // Zwei Bereiche: BHKW und Heizkessel - die Waermepumpe ist zugeklappt.
        Assert.Equal(2, seite.FindAll("div.epos-erzeugerkachel-parameter").Count);

        // Mit ihr sind es drei, und das zweite BHKW-Modul bleibt ohne.
        seite.Find("button.epos-simkonfig-verfuegbar").Click();
        Assert.Equal(3, seite.FindAll("div.epos-erzeugerkachel-parameter").Count);
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
}
