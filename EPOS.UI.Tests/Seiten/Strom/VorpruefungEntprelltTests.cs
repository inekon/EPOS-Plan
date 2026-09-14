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

        Tippen(cut, "");

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

        // DIE ENTPRELLZEIT IST DER ZEITGEBER DES PRUEFSTANDS, nicht die Wanduhr. Die zwei
        // ersten Tastendruecke legen je eine Entprellung auf, die im ganzen Lauf nicht
        // ablaeuft — sie kann also nur verschwinden, weil der naechste Tastendruck sie
        // abbricht. Erst der DRITTE bekommt eine kurze; er ist der einzige, der feuern darf.
        // Frueher trugen alle drei 40 ms: Verzoegerte sich ein Tastendruck unter Last um
        // mehr als diese 40 ms, feuerte eine ueberholte Entprellung doch noch mit, und der
        // Fall war rot, ohne dass sich an der Seite etwas geaendert haette.
        var cut = Station(zaehler, entprellungMs: 60000);
        int vollVorher = zaehler.Voll;

        Tippen(cut, "1");
        Tippen(cut, "10");
        Assert.Equal(vollVorher, zaehler.Voll);

        cut.Render(p => p.Add(x => x.EntprellungMs, 1));
        Tippen(cut, "100");

        Assert.Equal(3, zaehler.Schnell);

        // Auf den gezeichneten Zustand warten, nicht auf die Uhr.
        cut.WaitForAssertion(() => Assert.Equal(vollVorher + 1, zaehler.Voll));

        // Die zwei ueberholten Entprellungen sind abgebrochen — kaemen sie noch, stuende
        // der Zaehler hier hoeher.
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

        Tippen(cut, "1");
        cut.WaitForAssertion(() => Assert.Equal(vollVorher + 1, zaehler.Voll));

        Tippen(cut, "10");
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

        Tippen(cut, "20");
        int vorher = zaehler.Voll;

        Auslegungshilfe.Rechenknopf(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(vorher + 1, zaehler.Voll));
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

        // Ein NEGATIVER Befund braucht eine Schranke: „die Pruefung kommt nicht mehr" ist
        // erst gezeigt, wenn die offene Entprellung laengst faellig gewesen waere. Die
        // Schranke ist deshalb das Dreifache der Entprellzeit — und die Entprellzeit ist
        // gross genug, dass sie zwischen dem gezeichneten Tastendruck und dem Verlassen der
        // Seite nicht von selbst ablaufen kann.
        const int entprellung = 200;
        var cut = Station(zaehler, entprellungMs: entprellung);

        Tippen(cut, "20");
        int vorher = zaehler.Voll;

        cut.Instance.Dispose();
        await Task.Delay(3 * entprellung);

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

        Tippen(cut, "20");

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

        Tippen(cut, "20");

        Assert.Single(cut.Instance.Vorpruefung);
        Assert.Equal(0, zaehler.Schnell);
    }

    // =====================================================================
    //  Was der Zeitgeber NICHT darf
    // =====================================================================

    /// <summary>
    /// <b>Der Zeitgeber überschreibt kein Zahlenfeld.</b> Feuert die Entprellung mitten in
    /// der Eingabefolge, zeichnet die Seite neu — und der Feldtext bleibt, was der Anwender
    /// getippt hat: die frische Zahl ebenso wie das NICHTS. Sonst sähe ein Anwender, der
    /// tippt, den Modellwert zurückspringen (das Muster des Befunds „1 bleibt stehen").
    /// </summary>
    /// <remarks>
    /// Gewartet wird auf den ZÄHLER der vollen Stufe, nicht auf eine Wanduhr: Er zählt genau
    /// dann hoch, wenn die Entprellung abgelaufen ist und die Seite danach neu zeichnet.
    /// </remarks>
    [Fact]
    public void Der_Zeitgeber_zwischen_zwei_Eingaben_ueberschreibt_das_Feld_nicht()
    {
        var zaehler = new Pruefzaehler();
        var cut = Station(zaehler, entprellungMs: 1);

        TippenUndEntprellen(cut, zaehler, "100");
        Assert.Equal("100", Schrittfeld(cut).GetAttribute("value"));
        Assert.Equal(100.0, Achse(cut).KapazitaetSchrittKWh);

        // Das GELEERTE Feld: Das Modell nimmt die 0, die Anzeige bleibt leer.
        TippenUndEntprellen(cut, zaehler, "");
        Assert.Equal("", Schrittfeld(cut).GetAttribute("value"));
        Assert.Equal(0.0, Achse(cut).KapazitaetSchrittKWh);

        // Und das naechste Zeichen landet vorn, nicht hinter einer zurueckgeschriebenen 0.
        Tippen(cut, "2");
        Assert.Equal(2.0, Achse(cut).KapazitaetSchrittKWh);
    }

    // ================================================================= Prüfstand

    /// <summary>
    /// Tippt in das Schrittfeld und <b>wartet auf den gezeichneten Zustand</b>: Die Fassung
    /// des Arbeitsstandes ist weitergezählt UND das Feld zeigt das Getippte.
    /// </summary>
    /// <remarks>
    /// bunit gibt den Tastendruck in den Zeichenverteiler und kehrt zurück, ohne den
    /// Zeichenlauf abzuwarten. Ist der Verteiler belegt — und genau das tut die Fortsetzung
    /// einer abgelaufenen Entprellung, die aus dem Fadenvorrat zurückmeldet —, läuft der
    /// Tastendruck erst danach, und eine Prüfung unmittelbar dahinter liest den Stand VOR
    /// dem Zeichen. Die <c>Fassung</c> ist der verlässliche Merkposten: Sie zählt bei JEDER
    /// gemeldeten Änderung hoch, auch dort, wo der Feldtext derselbe bleibt.
    /// </remarks>
    private static void Tippen(IRenderedComponent<StromspeicherAuslegungSeite> cut, string text)
    {
        int fassung = cut.Instance.Fassung;
        Schrittfeld(cut).Input(text);
        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.Fassung > fassung, "Der Tastendruck ist noch nicht angekommen.");
            Assert.Equal(text, Schrittfeld(cut).GetAttribute("value"));
        });
    }

    /// <summary>
    /// Tippt und wartet, bis die Entprellung DIESES Tastendrucks abgelaufen ist und voll
    /// geprüft hat. Der Zählerstand wird VOR dem Zeichen gemerkt — sonst käme der Merkwert
    /// zu spät, wenn die Entprellung schon während des Wartens auf den Zeichenlauf feuert.
    /// </summary>
    private static void TippenUndEntprellen(IRenderedComponent<StromspeicherAuslegungSeite> cut,
                                            Pruefzaehler zaehler, string text)
    {
        int vorher = zaehler.Voll;
        Tippen(cut, text);
        cut.WaitForAssertion(() => Assert.True(zaehler.Voll > vorher,
                                               "Die Entprellung hat noch nicht gefeuert."));
    }

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
