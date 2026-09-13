using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Strom;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// DIE ZWEI STUFEN DER VORPRÜFUNG in Station 4 „Optimierung" (Auftrag #254).
///
/// <para><b>Der Befund.</b> Jede Änderung eines Suchraumfelds rief die VOLLE Vorprüfung
/// — Kosten aus der Datenbank, Standortzeitreihen aus dem Lauf, Peak-Ziel-Regeln. Am
/// Prüfprojekt 1046 kostete ein solcher Aufruf im Median 70 ms und sechs
/// Datenbankvorgänge; ein Suchraumfeld ändert aber weder Kosten noch Lastgang.</para>
///
/// <para><b>Was seither gilt.</b> Je Tastendruck läuft die SCHNELLE Stufe — ohne
/// Datenbank, ohne Zeitreihen —, und die Kandidatenzeile und die Sperre des
/// Rechenknopfs stehen unverzüglich, weil sie ohnehin aus der Konfiguration allein
/// kommen. Die VOLLE Stufe läuft entprellt: einmal nach dem letzten Zeichen, und
/// außerdem sofort beim Öffnen, beim Stationswechsel und vor dem Lauf.</para>
///
/// <para><b>Warum die Entprellzeit ein Parameter ist:</b> Damit dieser Prüfstand sie auf
/// 0 setzen kann und nicht auf eine Wanduhr warten muss (Hausmuster #169/#245/#253: auf
/// den gezeichneten Zustand warten, keine Sofort-Asserts nach dem Klick).</para>
/// </summary>
public sealed class VorpruefungEntprelltTests : EposBunitContext
{
    /// <summary>Die Ansicht trägt einen <c>InfoKnopf</c> und braucht deshalb den Hilfedienst.</summary>
    public VorpruefungEntprelltTests()
        => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    // =====================================================================
    //  Der Tastendruck
    // =====================================================================

    /// <summary>
    /// <b>Der Tastendruck meldet SOFORT und prüft NICHT voll.</b> Das geleerte
    /// Schrittfeld macht das Raster ungültig, die Kandidatenzeile sagt es und der
    /// Rechenknopf ist gesperrt — alles im selben Zeichenlauf; die teure Stufe wartet.
    /// </summary>
    [Fact]
    public void Ein_Tastendruck_sperrt_sofort_und_prueft_noch_nicht_voll()
    {
        var zaehler = new Pruefzaehler();
        var cut = Station(zaehler, entprellungMs: 60000);
        int vollVorher = zaehler.Voll;
        int schnellVorher = zaehler.Schnell;

        Schrittfeld(cut).Input("");

        // SOFORT: das ungueltige Raster und die Sperre.
        Assert.Contains(Resource.FLOTTE_OPT_RASTER_UNGUELTIG,
                        cut.Find("p.epos-flotte-kandidatenzeile").TextContent,
                        StringComparison.Ordinal);
        Assert.True(Auslegungshilfe.Rechenknopf(cut).HasAttribute("disabled"));

        // SOFORT: die schnelle Stufe. NICHT: die volle.
        Assert.Equal(schnellVorher + 1, zaehler.Schnell);
        Assert.Equal(vollVorher, zaehler.Voll);
    }

    /// <summary>
    /// <b>Drei schnelle Tastendrücke ergeben EINE volle Prüfung</b> — die Entprellung
    /// verwirft die zwei überholten, nicht die letzte.
    /// </summary>
    [Fact]
    public void Drei_schnelle_Tastendruecke_ergeben_genau_eine_volle_Pruefung()
    {
        var zaehler = new Pruefzaehler();
        var cut = Station(zaehler, entprellungMs: 40);
        int vollVorher = zaehler.Voll;

        Schrittfeld(cut).Input("1");
        Schrittfeld(cut).Input("10");
        Schrittfeld(cut).Input("100");

        Assert.Equal(3, zaehler.Schnell);

        // Auf den gezeichneten Zustand warten, nicht auf die Uhr.
        cut.WaitForAssertion(() => Assert.Equal(vollVorher + 1, zaehler.Voll));

        // Die zwei ueberholten Entprellungen sind abgebrochen — danach kommt nichts mehr.
        Assert.Equal(vollVorher + 1, zaehler.Voll);
        Assert.Equal(100.0, Achse(cut).KapazitaetSchrittKWh);
    }

    /// <summary>
    /// Mit Entprellzeit 0 läuft die volle Stufe unverzüglich — der Weg, den ein
    /// Prüfstand nimmt, wenn er den fertigen Befund im selben Schritt sehen will.
    /// </summary>
    [Fact]
    public void Ohne_Entprellzeit_prueft_jeder_Tastendruck_voll()
    {
        var zaehler = new Pruefzaehler();
        var cut = Station(zaehler, entprellungMs: 0);
        int vollVorher = zaehler.Voll;

        Schrittfeld(cut).Input("1");
        cut.WaitForAssertion(() => Assert.Equal(vollVorher + 1, zaehler.Voll));

        Schrittfeld(cut).Input("10");
        cut.WaitForAssertion(() => Assert.Equal(vollVorher + 2, zaehler.Voll));
    }

    // =====================================================================
    //  Die Stellen, an denen die volle Stufe SOFORT läuft
    // =====================================================================

    /// <summary>
    /// <b>Beim Öffnen und beim Stationswechsel</b> steht der fertige Befund, nicht der
    /// Zwischenstand der schnellen Stufe.
    /// </summary>
    [Fact]
    public void Das_Oeffnen_und_der_Stationswechsel_pruefen_voll()
    {
        var zaehler = new Pruefzaehler();

        // Station() geht selbst auf Blatt 4 — das ist schon ein Stationswechsel.
        var cut = Station(zaehler, entprellungMs: 60000);
        Assert.True(zaehler.Voll >= 2, "Öffnen und Stationswechsel prüfen je voll.");

        int vorher = zaehler.Voll;
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);
        Assert.Equal(vorher + 1, zaehler.Voll);
    }

    /// <summary>
    /// <b>Vor dem Lauf prüft die Seite voll</b> — keine veraltete Hinweisliste begleitet
    /// einen Start, auch wenn die Entprellung noch offen steht.
    /// </summary>
    [Fact]
    public void Vor_dem_Lauf_prueft_die_Seite_voll()
    {
        var zaehler = new Pruefzaehler();
        var cut = Station(zaehler, entprellungMs: 60000);

        Schrittfeld(cut).Input("20");
        int vorher = zaehler.Voll;

        Auslegungshilfe.Rechenknopf(cut).Click();

        Assert.Equal(vorher + 1, zaehler.Voll);
    }

    /// <summary>
    /// <b>Die verlassene Seite prüft nicht nach.</b> Eine offene Entprellung wird in
    /// <c>Dispose</c> abgebrochen — sonst zeichnete eine Komponente, die es nicht mehr
    /// gibt.
    /// </summary>
    [Fact]
    public async Task Die_verlassene_Seite_holt_die_offene_Vorpruefung_nicht_nach()
    {
        var zaehler = new Pruefzaehler();
        var cut = Station(zaehler, entprellungMs: 40);

        Schrittfeld(cut).Input("20");
        int vorher = zaehler.Voll;

        cut.Instance.Dispose();
        await Task.Delay(200);

        Assert.Equal(vorher, zaehler.Voll);
    }

    // =====================================================================
    //  Die EINE Liste
    // =====================================================================

    /// <summary>
    /// <b>Die Hinweisliste bleibt EINE Liste.</b> Was die schnelle Stufe gefunden hat,
    /// steht nach der vollen Stufe genau einmal da — die volle ersetzt, sie ergänzt
    /// nicht.
    /// </summary>
    [Fact]
    public void Die_Hinweise_der_schnellen_Stufe_erscheinen_nicht_doppelt()
    {
        var zaehler = new Pruefzaehler
        {
            SchnelleHinweise = new[] { Hinweis("Betrieb sehr niedrig") },
            VolleHinweise = new[] { Hinweis("Betrieb sehr niedrig"), Hinweis("Peak-Ziel zu klein") }
        };
        var cut = Station(zaehler, entprellungMs: 0);

        Schrittfeld(cut).Input("20");

        cut.WaitForAssertion(() => Assert.Equal(2, cut.Instance.Vorpruefung.Count));
        Assert.Equal(new[] { "Betrieb sehr niedrig", "Peak-Ziel zu klein" },
                     cut.Instance.Vorpruefung.Select(h => h.Text).ToArray());
    }

    /// <summary>
    /// <b>Kein Delegat ist kein Knopf:</b> Ohne die schnelle Stufe bleibt die stehende
    /// Liste, bis die volle sie ersetzt — eine leere Liste wäre eine ungeprüfte Aussage.
    /// </summary>
    [Fact]
    public void Ohne_die_schnelle_Stufe_bleibt_die_stehende_Liste()
    {
        var zaehler = new Pruefzaehler
        {
            OhneSchnelleStufe = true,
            VolleHinweise = new[] { Hinweis("Peak-Ziel zu klein") }
        };
        var cut = Station(zaehler, entprellungMs: 60000);

        Assert.Single(cut.Instance.Vorpruefung);

        Schrittfeld(cut).Input("20");

        Assert.Single(cut.Instance.Vorpruefung);
        Assert.Equal(0, zaehler.Schnell);
    }

    // ================================================================= Prüfstand

    /// <summary>Zählt, welche Stufe der Vorprüfung wie oft gerufen wurde.</summary>
    private sealed class Pruefzaehler
    {
        /// <summary>Aufrufe der vollen Stufe.</summary>
        public int Voll;

        /// <summary>Aufrufe der schnellen Stufe.</summary>
        public int Schnell;

        /// <summary>Was die volle Stufe zurückgibt.</summary>
        public IReadOnlyList<FlottenHinweis> VolleHinweise = Array.Empty<FlottenHinweis>();

        /// <summary>Was die schnelle Stufe zurückgibt.</summary>
        public IReadOnlyList<FlottenHinweis> SchnelleHinweise = Array.Empty<FlottenHinweis>();

        /// <summary>Die Hülle reicht den schnellen Weg NICHT herein.</summary>
        public bool OhneSchnelleStufe;
    }

    private static FlottenHinweis Hinweis(string text)
        => new() { Stufe = FlottenHinweisStufe.Hinweis, Text = text };

    /// <summary>Das Schrittfeld der Kapazität — das Feld des Befunds #253.</summary>
    private static IElement Schrittfeld(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.Find("article.epos-flotte-einheitskarte")
              .QuerySelectorAll("label.epos-feld")
              .Single(x => x.TextContent.Contains(Resource.FLOTTE_ED_KAPAZITAET_SCHRITT, StringComparison.Ordinal))
              .QuerySelector("input")!;

    private static FlottenAuslegungsAchse Achse(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen[0];

    /// <summary>Die Ansicht auf Station 4, mit gezähltem Vorprüfungsweg.</summary>
    /// <param name="zaehler">Der Zähler beider Stufen.</param>
    /// <param name="entprellungMs">Die Entprellzeit der vollen Stufe.</param>
    private IRenderedComponent<StromspeicherAuslegungSeite> Station(
        Pruefzaehler zaehler, int entprellungMs)
    {
        var einheit = new FlottenEinheit
        {
            Id = "s1", Name = "Speicher 1", KapazitaetKWh = 100,
            LadeleistungKw = 50, EntladeleistungKw = 50,
            Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
            SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
        };

        var flotte = new FlottenStudieKonfiguration
        {
            Einheiten = new List<FlottenEinheit> { einheit },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving,
                EnergieAusgleichEuroProKWh = 0.3
            },
            Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
            {
                Kalkulationszins = 0.03, ProjektjahreBeiWiederholung = 20
            },
            Auslegung = new FlottenAuslegungEingang
            {
                MaximaleKandidaten = 10000,
                Suchmethode = FlottenSuchmethode.Groesse,
                Achsen = new List<FlottenAuslegungsAchse>
                {
                    new()
                    {
                        Aktiv = true, Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                        AnzahlVon = 1, AnzahlBis = 1,
                        KapazitaetVonKWh = 100, KapazitaetBisKWh = 300, KapazitaetSchrittKWh = 10,
                        LeistungVonKw = 40, LeistungBisKw = 80, LeistungSchrittKw = 20,
                        CRateVon = 0.5, CRateBis = 2.0, CRateSchritt = 0.5,
                        Vorlage = einheit
                    }
                }
            }
        };

        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Flotte = flotte, FlottenGroessenOptimieren = true
                }
            }
        };

        var dienste = new StromspeicherAuslegungDienste
        {
            Vorgaben = () => vorgaben,
            FlotteRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis()),
            Vorpruefen = _ => { zaehler.Voll++; return zaehler.VolleHinweise; }
        };
        if (!zaehler.OhneSchnelleStufe)
            dienste.VorpruefenSchnell = _ => { zaehler.Schnell++; return zaehler.SchnelleHinweise; };

        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.EntprellungMs, entprellungMs));

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Optimierung);
        return cut;
    }
}
