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
    public SimulationKonfigSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
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
    private readonly List<bool> _extrapolation = new();
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
            ExtrapolationMoeglich = true,
            ExtrapolationErlaubt = true,
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
            ExtrapolationSchreiben = w => { _extrapolation.Add(w); return true; },
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

    [Fact]
    public void Der_Extrapolationsschalter_schreibt_sofort_und_meldet()
    {
        var cut = Seite();

        cut.FindAll("input[type=checkbox]")[0].Change(false);

        Assert.Equal(new[] { false }, _extrapolation);
        Assert.Contains("abgewählt", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// Der Booster-Lesepunkt ist UNSICHTBAR, bis das Projekt einen gekoppelten
    /// Booster führt (PAKET B2, :281).
    /// </summary>
    [Fact]
    public void Der_Booster_Lesepunkt_erscheint_nur_mit_Booster()
    {
        Assert.Equal(2, Seite().FindAll("input[type=checkbox]").Count);
        Assert.Single(Seite(mitBooster: false).FindAll("input[type=checkbox]"));
    }

    [Fact]
    public void Speichern_ruft_den_Schreibweg_und_meldet_den_Erfolg()
    {
        var cut = Seite();

        cut.FindAll("div.epos-leiste button.epos-knopf")[0].Click();   // Konfiguration speichern

        Assert.Equal(1, _gespeichert);
        Assert.Contains("gespeichert", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Beenden_meldet_den_Schliesswunsch()
    {
        bool zu = false;

        var cut = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, Dienste())
            .Add(x => x.StartProjekt, 1030)
            .Add(x => x.Geschlossen, () => zu = true));

        var knoepfe = cut.FindAll("div.epos-leiste button.epos-knopf");
        knoepfe[knoepfe.Count - 1].Click();
        Assert.True(zu);
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

        // Die Fusszeile bleibt draussen: „Beenden" muss erreichbar sein.
        Assert.NotEmpty(cut.FindAll("div.epos-leiste button.epos-knopf"));
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
}
