using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using KiKern;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>Die Werte JE ANLAGE von Schritt ① über den Assistenten</b> (Welle #458, Stufe 1,
/// Nachzug der Feldkarte SIMULATION): Wärmequelle, konstante Quelltemperatur,
/// WP-Priorität und Betriebsmodus der gewählten Karte gehen dieselben Wege wie die
/// Überlagerungen der Karte (Quellenwahl, WertAbfrage, BetriebsmodusDialog) — mit
/// denselben Vorprüfungen und benannten Absagen.
/// </summary>
/// <remarks>
/// Gerechnet wird am GEZEICHNETEN Schritt ①: Die Sichtklasse bekommt die lebende
/// Komponente, wie in <c>SimulationSeite</c>. Gesetzt wird über den Verteiler der
/// Komponente (<c>InvokeAsync</c>), weil die Wege neu laden und zeichnen.
/// </remarks>
public sealed class SimulationKonfigKiTests : EposBunitContext
{
    private const int WP = 10353;
    private const int KESSEL = 20001;
    private const int BHKW = 14920;

    private readonly List<string> _geschrieben = new();
    private string _quelle = "Außenluft";
    private bool _gesperrt;

    public SimulationKonfigKiTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static ErzeugerZeile Karte(string dbWert, string titel, int idAnlage, bool wp, bool quelle)
        => new ErzeugerZeile
        {
            DbWert = dbWert,
            IdAnlage = idAnlage,
            IdType = wp ? 1 : 11,
            Bezeichner = titel,
            HatAnlage = true,
            IstWaermepumpe = wp,
            QuellenwahlMoeglich = quelle,
            Prioritaet = wp ? 2 : 0,
            Kachel = new ErzeugerKachelDaten { Schluessel = dbWert, Titel = titel, Editierbar = true }
        };

    private SimulationKonfigDienste Dienste() => new SimulationKonfigDienste
    {
        Laden = _ => _gesperrt
            ? new SimulationKonfigDaten { IdProjekt = 1030, Gesperrt = true, Sperrgrund = "Migration offen." }
            : new SimulationKonfigDaten
            {
                IdProjekt = 1030,
                PvGewaehlt = false,
                Gruppen = new List<KachelGruppe>
                {
                    new KachelGruppe
                    {
                        Titel = "Wärmeerzeuger",
                        Zeilen = new List<ErzeugerZeile>
                        {
                            Karte("BHKW", "BHKW 1", BHKW, wp: false, quelle: false),
                            Karte("Wärmepumpe", "WP 1", WP, wp: true, quelle: true),
                            Karte("Heizkessel", "Kessel 1", KESSEL, wp: false, quelle: true)
                        }
                    }
                }
            },
        BetriebsmodusGaben = id => new Dictionary<string, object>
        {
            ["Bezeichner"] = "WP 1",
            ["AktuellerModus"] = "",
            ["SteuerwertLaufzeit"] = "Laufzeit",
            ["SteuerwertLeistung"] = "Leistung",
            ["SteuerwertPv"] = "PV",
            ["RbLaufzeit"] = "laufzeitoptimiert",
            ["RbLeistung"] = "leistungsoptimiert",
            ["RbPv"] = "PV-optimiert"
        },
        BetriebsmodusSchreiben = (a, m) => _geschrieben.Add("modus:" + a + ":" + m),
        PrioritaetSchreiben = (a, p) => _geschrieben.Add("prio:" + a + ":" + p),
        Quellentypen = _ => new List<Quellentyp>
        {
            new Quellentyp("", "Systemrücklauf"),
            new Quellentyp("Außenluft", "Außenluft"),
            new Quellentyp("Konstant", "konstante Temperatur"),
            new Quellentyp("Erdreich", "Erdreich"),
            new Quellentyp("CSV", "Profil aus Datei")
        },
        QuelleTyp = _ => _quelle,
        QuelleTemperatur = _ => 8.5,
        QuelleEinfachSchreiben = (a, t, w) =>
            _geschrieben.Add("quelle:" + a + ":" + t + ":" + w.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        QuelleErdreichGaben = _ => new Dictionary<string, object>
        {
            ["Daten"] = new EPOS.UI.Dialoge.Simulation.QuelleErdreichDaten
            {
                WPName = "WP 1", IdProjekt = 1030, IdAnlage = WP, Tiefe = 1.8, Flaeche = 250,
                Klimazone = 6, Spreizung = 4
            }
        }
    };

    private (IRenderedComponent<SimulationKonfigSeite> Seite, SimulationKiSicht Sicht) Aufbau()
    {
        IRenderedComponent<SimulationKonfigSeite> cut = Render<SimulationKonfigSeite>(p => p
            .Add(x => x.Dienste, Dienste())
            .Add(x => x.StartProjekt, 1030));

        var sicht = new SimulationKiSicht(() => cut.Instance.Stand, () => null, () => null,
                                          () => null, () => "", () => "",
                                          konfigseite: () => cut.Instance);
        return (cut, sicht);
    }

    private static Task Setzen(IRenderedComponent<SimulationKonfigSeite> cut, Action aktion)
        => cut.InvokeAsync(aktion);

    private static async Task<string> Absage(IRenderedComponent<SimulationKonfigSeite> cut, Action aktion)
    {
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.InvokeAsync(aktion));
        return ex.Message;
    }

    // =====================================================================
    //  Die Anlage
    // =====================================================================

    [Fact]
    public async Task Die_Quellanlage_waehlt_die_Karte_und_kennt_nur_Karten_mit_Quellenwahl()
    {
        var (cut, sicht) = Aufbau();

        Assert.Equal(0, sicht.Quellanlage);
        Assert.Equal(new[] { WP.ToString(), KESSEL.ToString() },
                     sicht.QuellanlageWahl.Select(e => e.Schluessel).ToArray());
        Assert.Equal(new[] { "WP 1", "Kessel 1" }, sicht.QuellanlageWahl.Select(e => e.Text).ToArray());

        await Setzen(cut, () => sicht.Quellanlage = WP);
        Assert.Equal("ERZEUGER_" + WP, cut.Instance.Auswahl);
        Assert.Equal(WP, sicht.Quellanlage);

        // Das BHKW hat keine Quellenwahl - die Absage nennt die Id, die Auswahl bleibt.
        string grund = await Absage(cut, () => sicht.Quellanlage = BHKW);
        Assert.Equal(string.Format(Resource.KI_SIM_ANLAGE_UNBEKANNT, BHKW), grund);
        Assert.Equal("ERZEUGER_" + WP, cut.Instance.Auswahl);
    }

    [Fact]
    public async Task Ohne_gewaehlte_Anlage_lehnt_jede_Setzung_benannt_ab()
    {
        var (cut, sicht) = Aufbau();

        Assert.Equal("", sicht.Waermequelle);
        Assert.Empty(sicht.WpBetriebsmodusWahl);

        Assert.Equal(Resource.KI_SIM_ANLAGE_FEHLT, await Absage(cut, () => sicht.WpPrioritaet = 3));
        Assert.Equal(Resource.KI_SIM_ANLAGE_FEHLT, await Absage(cut, () => sicht.Waermequelle = "Außenluft"));
        Assert.Empty(_geschrieben);
    }

    // =====================================================================
    //  Priorität und Betriebsmodus — nur für Wärmepumpen
    // =====================================================================

    [Fact]
    public async Task Prioritaet_und_Betriebsmodus_gehen_den_Weg_der_Ueberlagerung()
    {
        var (cut, sicht) = Aufbau();
        await Setzen(cut, () => sicht.Quellanlage = WP);

        Assert.Equal(2, sicht.WpPrioritaet);
        await Setzen(cut, () => sicht.WpPrioritaet = 3);
        Assert.Equal(Resource.KI_SIM_PRIORITAET_POSITIV, await Absage(cut, () => sicht.WpPrioritaet = 0));

        // Leer heisst laufzeitoptimiert - dieselbe Rueckfallregel wie im Dialog.
        Assert.Equal("Laufzeit", sicht.WpBetriebsmodus);
        Assert.Equal(new[] { "Laufzeit", "Leistung", "PV" },
                     sicht.WpBetriebsmodusWahl.Select(e => e.Schluessel).ToArray());

        await Setzen(cut, () => sicht.WpBetriebsmodus = "Leistung");
        Assert.Equal(string.Format(Resource.KI_SIM_WERT_UNBEKANNT, "Turbo"),
                     await Absage(cut, () => sicht.WpBetriebsmodus = "Turbo"));

        Assert.Equal(new[] { "prio:" + WP + ":3", "modus:" + WP + ":Leistung" }, _geschrieben);
    }

    [Fact]
    public async Task Der_PV_Modus_ohne_PV_in_der_Simulation_meldet_wie_der_OK_Weg()
    {
        var (cut, sicht) = Aufbau();
        await Setzen(cut, () => sicht.Quellanlage = WP);

        await Setzen(cut, () => sicht.WpBetriebsmodus = "PV");

        Assert.Contains("modus:" + WP + ":PV", _geschrieben);
        Assert.Contains(cut.Instance.MsgPvAuswahl, cut.Markup);
    }

    [Fact]
    public async Task Fuer_einen_Heizkessel_gelten_Prioritaet_und_Betriebsmodus_nicht()
    {
        var (cut, sicht) = Aufbau();
        await Setzen(cut, () => sicht.Quellanlage = KESSEL);

        string erwartet = string.Format(Resource.KI_SIM_NUR_WP, "Kessel 1");
        Assert.Equal(erwartet, await Absage(cut, () => sicht.WpPrioritaet = 4));
        Assert.Equal(erwartet, await Absage(cut, () => sicht.WpBetriebsmodus = "Leistung"));
        Assert.Equal("", sicht.WpBetriebsmodus);
        Assert.Empty(_geschrieben);
    }

    // =====================================================================
    //  Wärmequelle und konstante Quelltemperatur
    // =====================================================================

    [Fact]
    public async Task Die_Waermequelle_schreibt_die_einfachen_Zweige_sofort()
    {
        var (cut, sicht) = Aufbau();
        await Setzen(cut, () => sicht.Quellanlage = WP);

        Assert.Equal("Außenluft", sicht.Waermequelle);
        Assert.Equal(5, sicht.WaermequelleWahl.Count);

        await Setzen(cut, () => sicht.Waermequelle = "");
        await Setzen(cut, () => sicht.Waermequelle = "Konstant");

        // „Konstant" schreibt mit der gespeicherten Temperatur - wie die WertAbfrage,
        // deren Vorgabe sie ist.
        Assert.Equal(new[] { "quelle:" + WP + "::0", "quelle:" + WP + ":Konstant:8.5" }, _geschrieben);
    }

    [Fact]
    public async Task Eine_Quelle_mit_eigenem_Dialog_oeffnet_ihn_und_sagt_es()
    {
        var (cut, sicht) = Aufbau();
        await Setzen(cut, () => sicht.Quellanlage = WP);

        string grund = await Absage(cut, () => sicht.Waermequelle = "Erdreich");

        Assert.Equal(string.Format(Resource.KI_SIM_QUELLE_DIALOG, "Erdreich", cut.Instance.ErdreichTitel), grund);
        Assert.Equal("QuelleErdreich", cut.Instance.OffenerUntereditor);
        Assert.Empty(_geschrieben);

        // Solange die Ueberlagerung steht, haelt jede Setzung an - sie schriebe sonst
        // ihren Stand von vorhin mit OK zurueck.
        Assert.Equal(Resource.KI_SIM_UEBERLAGERUNG_OFFEN, await Absage(cut, () => sicht.WpPrioritaet = 5));
    }

    [Fact]
    public async Task Eine_Datei_waehlt_der_Anwender_selbst()
    {
        var (cut, sicht) = Aufbau();
        await Setzen(cut, () => sicht.Quellanlage = WP);

        Assert.Equal(string.Format(Resource.KI_SIM_QUELLE_DATEI, "Profil aus Datei"),
                     await Absage(cut, () => sicht.Waermequelle = "CSV"));
        Assert.Equal("Keine", cut.Instance.OffenerEditor);
        Assert.Empty(_geschrieben);
    }

    [Fact]
    public async Task Die_Quelltemperatur_gilt_nur_fuer_die_Quelle_Konstant()
    {
        var (cut, sicht) = Aufbau();
        await Setzen(cut, () => sicht.Quellanlage = WP);

        Assert.Equal(8.5, sicht.QuelltemperaturKonstant);
        Assert.Equal(string.Format(Resource.KI_SIM_QUELLTEMP_NUR_KONSTANT, "konstante Temperatur"),
                     await Absage(cut, () => sicht.QuelltemperaturKonstant = 12.0));

        _quelle = "Konstant";
        await Setzen(cut, () => sicht.QuelltemperaturKonstant = 12.0);
        Assert.Equal(new[] { "quelle:" + WP + ":Konstant:12" }, _geschrieben);
    }

    [Fact]
    public async Task Eine_gesperrte_Seite_nennt_ihren_Sperrgrund()
    {
        _gesperrt = true;
        var (cut, sicht) = Aufbau();

        Assert.Empty(sicht.QuellanlageWahl);
        Assert.Equal("Migration offen.", await Absage(cut, () => sicht.Quellanlage = WP));
    }

    [Fact]
    public void Ohne_Schritt_1_lesen_die_Felder_leer_und_lehnen_ab()
    {
        var sicht = new SimulationKiSicht(() => null, () => null, () => null, () => null, () => "", () => "");

        Assert.Equal(0, sicht.Quellanlage);
        Assert.Equal("", sicht.Waermequelle);
        Assert.Empty(sicht.QuellanlageWahl);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => sicht.WpPrioritaet = 2);
        Assert.Equal(Resource.KI_SIM_SCHRITT1_FEHLT, ex.Message);
    }

    // =====================================================================
    //  An der Brücke: die Wahlquellen lösen auf
    // =====================================================================

    [Fact]
    public async Task An_der_Bruecke_tragen_die_drei_Wahlfelder_ihre_Eintraege()
    {
        var (cut, sicht) = Aufbau();
        await Setzen(cut, () => sicht.Quellanlage = WP);

        using KiMaskenanmeldung anmeldung =
            KiMaskenanmeldung.Fuer(KiMaskennamen.SIMULATION, () => sicht, new KiMaskenhaken());

        Assert.Equal(2, KiMaskenbruecke.Feldzugang(KiMaskennamen.SIMULATION, "quellanlage").Wahleintraege().Count);
        Assert.Equal(5, KiMaskenbruecke.Feldzugang(KiMaskennamen.SIMULATION, "waermequelle").Wahleintraege().Count);
        Assert.Equal(3, KiMaskenbruecke.Feldzugang(KiMaskennamen.SIMULATION, "wp_betriebsmodus").Wahleintraege().Count);

        // Die Anlage ist eine SATZWAHL: Sie schreibt nichts in den Stand.
        Assert.True(KiDialoge.Katalog.Finde(KiMaskennamen.SIMULATION)!
                             .Felder.Single(f => f.Name == "quellanlage").Satzwahl);
    }
}
