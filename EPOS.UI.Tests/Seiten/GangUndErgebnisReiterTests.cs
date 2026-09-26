using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die drei Reiter des Ergebnisblocks — Waermegang (<c>NavigatorWaerme</c>),
/// Stromgang (<c>NavigatorStrom</c>) und der Ergebnisreiter selbst
/// (R11 + <c>DashboardForm</c>), iU9-W11b.10 und .11.
///
/// <para>Soll: die drei Zustandsachsen des Waermegangs, das Ausblenden
/// fehlender Reihen, der ergaenzte Sortiertumschalter des Stromgangs
/// (Befund W11-B41), die vier Navigationsblaetter und die Autarkiekacheln samt
/// der NICHT gespeicherten Was-waere-wenn-Kapazitaet (Befund W11-B32).</para>
/// </summary>
public class GangUndErgebnisReiterTests : EposBunitContext
{
    private readonly List<Bildauftrag> _auftraege = new();

    public GangUndErgebnisReiterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Die Zeichenmodelle aller Bilder dieser Reiter, nach Bild und Schalterstellung
    /// getrennt und je EINMAL gebaut. Der Baustein <c>DiagrammSvg</c> vergleicht die
    /// Modellreferenz; ein je Zeichenlauf neu gebautes Modell setzte seinen Baum
    /// jedes Mal neu und nähme ihm Zoom und abgewählte Reihe.
    /// </summary>
    private readonly Dictionary<string, Zeichenmodell> _modelle = new();

    private Zeichenmodell? Modell(Bildauftrag a)
    {
        _auftraege.Add(a);

        string schluessel = a.Bild + (a.Sortiert ? "-s" : "") + "|" + a.Kanal;
        if (_modelle.TryGetValue(schluessel, out Zeichenmodell? vorhanden)) return vorhanden;

        Zeichenmodell neu = a.Bild == Bilder.AutarkieMonate || a.Bild == Bilder.WaermeAutarkieMonate
            ? Monatsstapel()
            : Gangbild(a.Bild, a.Sortiert);
        _modelle[schluessel] = neu;
        return neu;
    }

    /// <summary>
    /// Der MONATSSTAPEL der Autarkie (Etappe DG-E3, Gruppe (c)): zwölf starre Fächer
    /// ohne Zeichenfläche — kein Zoom, dafür der Wert der Schicht unter dem Zeiger.
    /// </summary>
    private static Zeichenmodell Monatsstapel()
    {
        var direkt = new double[12];
        var speicher = new double[12];
        for (int m = 0; m < 12; m++)
        {
            direkt[m] = 100.0 + 10.0 * m;
            speicher[m] = 40.0 + 2.0 * m;
        }

        return ChartRenderer.MonatsStapelModell("Deckung", "kWh", new[]
        {
            new ChartRenderer.Reihe("Direkt", direkt, ChartRenderer.C_PV),
            new ChartRenderer.Reihe("Aus Speicher", speicher, ChartRenderer.C_SPEICHER[0])
        });
    }

    /// <summary>
    /// Ein echtes Gangbild aus einer KURZEN Reihe (eine Woche) — die Fälle prüfen die
    /// Bedienung, nicht die Rechenzeit eines Jahres. Die zweite Achse trägt den
    /// Speicherfüllstand, wie im Bild des Kerns.
    /// </summary>
    private static Zeichenmodell Gangbild(string name, bool sortiert)
    {
        var werte = new double[168];
        var fuellstand = new double[168];
        for (int i = 0; i < werte.Length; i++)
        {
            werte[i] = 150.0 + 90.0 * Math.Sin(2 * Math.PI * i / 24.0);
            fuellstand[i] = 40.0 + 20.0 * Math.Cos(2 * Math.PI * i / 24.0);
        }
        if (sortiert) Array.Sort(werte, (x, y) => y.CompareTo(x));

        return ChartRenderer.ErzeugerStapelModell(
            name,
            new[] { new ChartRenderer.Reihe("Summe", werte, ChartRenderer.C_WP) },
            Array.Empty<ChartRenderer.Reihe>(), null,
            "kW", ChartRenderer.Achse.Jahresstunden, sortiert,
            new[] { new ChartRenderer.Reihe("Puffer 1", fuellstand, ChartRenderer.C_SPEICHER[0]) },
            "kWh");
    }

    // =====================================================================
    // Waermegang
    // =====================================================================

    private static WaermegangDaten Waerme() => new WaermegangDaten
    {
        Erzeuger = new[]
        {
            new Ganglinienreihe("GESAMT", "Summe Wärmeerzeugung", true),
            new Ganglinienreihe("WAERMEPUMPE", "Wärmepumpe", true),
            new Ganglinienreihe("HEIZSTAB", "Heizstab", true),
            new Ganglinienreihe("HEIZKESSEL", "Heizkessel", true),
            new Ganglinienreihe("SOLARTHERMIE", "Solarthermie", false),
            new Ganglinienreihe("BHKW_WAERME", "BHKW", false)
        },
        Speicher = new[]
        {
            new Ganglinienreihe("PUFFER_1018023", "Puffer 1", true)
        },
        Bedarfsarten = new (int, string)[] { (-1, "Gesamt"), (0, "Heizung"), (1, "Brauchwasser") }
    };

    /// <summary>
    /// Das Sitzungsgedaechtnis DIESES Falles (#234). xunit legt je Testmethode eine
    /// neue Instanz der Klasse an — damit hat jeder Fall seinen eigenen Stand, und
    /// keiner sieht den des Nachbarn, auch wenn bunit sie nebeneinander faehrt. Der
    /// prozessweite <c>Ganglinienregister</c>-Stand bleibt dabei unberuehrt.
    /// </summary>
    private readonly Ganglinienstand _waermeStand = new Ganglinienstand();
    private readonly Ganglinienstand _stromStand = new Ganglinienstand();

    private IRenderedComponent<WaermegangReiter> WaermeZeichnen(Action<(int, IReadOnlyList<string>,
                                                                        IReadOnlyList<string>)>? csv = null,
                                                                Ganglinienstand? stand = null)
        => Render<WaermegangReiter>(p =>
        {
            p.Add(x => x.Daten, Waerme());
            p.Add(x => x.Modell, Modell);
            p.Add(x => x.Gedaechtnis, stand ?? _waermeStand);
            if (csv is not null)
                p.Add(x => x.Csv, EventCallback.Factory.Create<(int, IReadOnlyList<string>,
                                                                IReadOnlyList<string>)>(this, csv));
        });

    /// <summary>
    /// <b>Das Wärmegangbild steht als SVG im Baum</b> (Etappe DG-E3, Gruppe (a)) —
    /// unter der Kennung <c>simerg-waermegang</c>, mit der Einheit der linken und der
    /// rechten Achse, und ohne ein Pixelbild daneben.
    /// </summary>
    [Fact]
    public void Waermegang_steht_als_DiagrammSvg()
    {
        var seite = WaermeZeichnen();
        DiagrammSvg bild = seite.FindComponent<DiagrammSvg>().Instance;

        Assert.Single(seite.FindComponents<DiagrammSvg>());
        Assert.Equal("simerg-waermegang", bild.Kennung);
        Assert.Equal("kW", bild.Einheit);
        Assert.Equal("kWh", bild.EinheitRechts);
        Assert.Empty(seite.FindAll("img"));
    }

    /// <summary>Fehlende Reihen erscheinen gar nicht — und koennen nicht in den Export.</summary>
    [Fact]
    public void Waermegang_zeigt_nur_die_vorhandenen_Erzeuger()
    {
        var seite = WaermeZeichnen();
        string text = seite.Markup;

        Assert.Contains("Wärmepumpe", text);
        Assert.Contains("Heizkessel", text);
        Assert.DoesNotContain("Solarthermie", text);
    }

    /// <summary>
    /// Die Bedarfsart schaltet zwischen PRODUKTION (−1) und DECKUNG je Kanal —
    /// die Kernachse der E2-Umschaltung (<c>VektorenSetzen</c> :424-453).
    /// </summary>
    [Fact]
    public void Waermegang_meldet_die_Bedarfsart_im_Bildauftrag()
    {
        var seite = WaermeZeichnen();
        Assert.Equal(-1, seite.Instance.Bedarfsart);

        _auftraege.Clear();
        seite.Find("select").Change("1");

        Assert.Equal(1, seite.Instance.Bedarfsart);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Waermegang && a.Kanal == 1);
    }

    /// <summary>Die drei Schalter: sortiert, Bedarfslinie, dazu die zwei Listen.</summary>
    [Fact]
    public void Waermegang_traegt_Sortiert_und_Bedarfslinie()
    {
        var seite = WaermeZeichnen();
        var haken = seite.FindAll("input[type='checkbox']");

        // 2 Schalter + Summe + 3 Erzeuger + 1 Speicher (je Eintrag einer)
        Assert.True(haken.Count >= 2);

        _auftraege.Clear();
        haken[0].Change(true);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Waermegang && a.Sortiert);
    }

    /// <summary>
    /// Der Export ist IMMER chronologisch und traegt Bedarfsart, Erzeuger und
    /// Speicher — woertlich (<c>btn_CsvExport_Click</c> :299-337).
    /// </summary>
    [Fact]
    public void Waermegang_meldet_Bedarfsart_Erzeuger_und_Speicher_an_den_Export()
    {
        (int Kanal, IReadOnlyList<string> Erz, IReadOnlyList<string> Sp) gemeldet = (0, [], []);
        var seite = WaermeZeichnen(w => gemeldet = w);

        seite.Find("button.epos-simerg-knopf").Click();

        Assert.Equal(-1, gemeldet.Kanal);
        Assert.NotNull(gemeldet.Erz);
        Assert.NotNull(gemeldet.Sp);
    }

    // ------------------------------------------------- #234: Vorbelegung + Gedaechtnis

    /// <summary>
    /// <b>Anwenderrueckmeldung 12.09.2026 (#234): „Die Grafik ist zu Beginn leer."</b>
    /// Der Reiter startete mit LEEREN Auswahllisten und ausgeschalteter Bedarfslinie;
    /// der erste Bildauftrag trug damit keine einzige Reihe, und der Renderer setzte
    /// seinen Leerhinweis. Beim ersten Befuellen sind jetzt ALLE vorhandenen Erzeuger,
    /// ALLE Speicher und die Bedarfslinie an — Bedarfsart „Gesamt", sortiert aus
    /// (Bildschirmfoto 2 der Rueckmeldung).
    /// </summary>
    [Fact]
    public void Waermegang_belegt_beim_ersten_Aufbau_alle_Reihen_vor()
    {
        _auftraege.Clear();
        var seite = WaermeZeichnen();

        Assert.Equal(new[] { "GESAMT", "WAERMEPUMPE", "HEIZSTAB", "HEIZKESSEL" },
                     seite.Instance.GewaehlteErzeuger);
        Assert.Equal(new[] { "PUFFER_1018023" }, seite.Instance.GewaehlteSpeicher);
        Assert.True(seite.Instance.Bedarfslinie);
        Assert.Equal(-1, seite.Instance.Bedarfsart);
        Assert.False(seite.Instance.Sortiert);

        // Der Bildauftrag traegt alle Schluessel plus die Bedarfslinie.
        Bildauftrag auftrag = _auftraege[_auftraege.Count - 1];
        Assert.NotNull(auftrag.Reihen);
        Assert.Equal(new[] { "GESAMT", "WAERMEPUMPE", "HEIZSTAB", "HEIZKESSEL",
                             "PUFFER_1018023", "WAERMEBEDARF" }, auftrag.Reihen);
    }

    /// <summary>
    /// Der zweite Teil der Rueckmeldung („oder die Auswahl des Benutzers gespeichert
    /// werden"): <c>Reiterblatt</c> zeichnet <c>@if (Sichtbar)</c> — beim Blattwechsel
    /// entsteht der Reiter NEU. Der Neuaufbau liest deshalb das Sitzungsgedaechtnis
    /// VOR der Vorbelegung.
    /// </summary>
    [Fact]
    public void Waermegang_liest_beim_Neuaufbau_das_Gedaechtnis()
    {
        var erste = WaermeZeichnen();

        // Anwenderwahl: Bedarfslinie aus, Summe aus, nur noch der Heizstab, kein Speicher.
        erste.FindAll("input[type='checkbox']")[1].Change(false);   // Bedarfslinie
        erste.FindAll("input[type='checkbox']")[2].Change(false);   // Summe Waermeerzeugung
        erste.FindAll("input[type='checkbox']")[3].Change(false);   // Waermepumpe
        erste.FindAll("input[type='checkbox']")[5].Change(false);   // Heizkessel
        erste.FindAll("input[type='checkbox']")[6].Change(false);   // Puffer 1

        Assert.Equal(new[] { "HEIZSTAB" }, erste.Instance.GewaehlteErzeuger);

        // Das Blatt wird gewechselt und wieder betreten - eine NEUE Komponente.
        var zweite = WaermeZeichnen();

        Assert.Equal(new[] { "HEIZSTAB" }, zweite.Instance.GewaehlteErzeuger);
        Assert.Empty(zweite.Instance.GewaehlteSpeicher);
        Assert.False(zweite.Instance.Bedarfslinie);
    }

    // ------------------------------------------- 15.09.2026: die Summenlinie

    /// <summary>
    /// <b>Anwenderwunsch 15.09.2026.</b> Die gruene Summenlinie des
    /// Produktionsdiagramms hatte kein Kaestchen: Sie war da, ob der Anwender sie
    /// wollte oder nicht. Jetzt steht sie als ERSTER Eintrag der Erzeugerliste —
    /// die Summe ueber ihren Summanden —, von ihnen durch eine Trennlinie
    /// abgesetzt, und traegt denselben Namen wie die Legende.
    /// </summary>
    [Fact]
    public void Die_Summenlinie_steht_abgesetzt_ueber_den_Erzeugern()
    {
        var seite = WaermeZeichnen();
        string text = seite.Markup;

        Assert.Contains("Summe Wärmeerzeugung", text);
        Assert.True(text.IndexOf("Summe Wärmeerzeugung", StringComparison.Ordinal)
                    < text.IndexOf("Wärmepumpe", StringComparison.Ordinal),
                    "Die Summe gehoert UEBER ihre Summanden.");

        // Sichtbar abgesetzt: genau eine Trennlinie, und zwar in der Erzeugerliste.
        Assert.Single(seite.FindAll("hr.epos-mehrfachauswahl-absatz"));
    }

    /// <summary>
    /// „Alle" und „Keine" fassen die Summenlinie mit — sie liegt in DERSELBEN
    /// Liste wie ihre Summanden und nicht als Einzelschalter daneben.
    /// </summary>
    [Fact]
    public void Alle_und_Keine_fassen_die_Summenlinie_mit()
    {
        var seite = WaermeZeichnen();
        // Nur die Erzeugerliste traegt Sammelknoepfe (die Speicherliste hat einen Eintrag).
        var knoepfe = seite.FindAll("button.epos-knopf");

        knoepfe[1].Click();                                   // Keine
        Assert.Empty(seite.Instance.GewaehlteErzeuger);

        seite.FindAll("button.epos-knopf")[0].Click();        // Alle
        Assert.Contains("GESAMT", seite.Instance.GewaehlteErzeuger);
    }

    /// <summary>
    /// Der Schalter WIRKT: Abgewaehlt faellt „GESAMT" aus dem Bildauftrag, wieder
    /// angehakt steht es darin. Die Huelle zeichnet die Kontur nur bei
    /// <c>wahl.Contains("GESAMT")</c>.
    /// </summary>
    [Fact]
    public void Die_abgeschaltete_Summenlinie_faellt_aus_dem_Bildauftrag()
    {
        var seite = WaermeZeichnen();
        Assert.Contains("GESAMT", _auftraege[^1].Reihen!);

        _auftraege.Clear();
        seite.FindAll("input[type='checkbox']")[2].Change(false);   // Summe aus
        Assert.DoesNotContain("GESAMT", _auftraege[^1].Reihen!);
        Assert.DoesNotContain("GESAMT", seite.Instance.GewaehlteErzeuger);

        _auftraege.Clear();
        seite.FindAll("input[type='checkbox']")[2].Change(true);    // Summe wieder an
        Assert.Contains("GESAMT", _auftraege[^1].Reihen!);
    }

    /// <summary>
    /// Die neue Reihe faellt in dasselbe Sitzungsgedaechtnis wie jede andere (#234):
    /// Ein Blattwechsel verliert die Wahl nicht.
    /// </summary>
    [Fact]
    public void Die_Wahl_der_Summenlinie_uebersteht_den_Blattwechsel()
    {
        var erste = WaermeZeichnen();
        erste.FindAll("input[type='checkbox']")[2].Change(false);   // Summe aus

        var zweite = WaermeZeichnen();

        Assert.DoesNotContain("GESAMT", zweite.Instance.GewaehlteErzeuger);
        Assert.Contains("WAERMEPUMPE", zweite.Instance.GewaehlteErzeuger);
    }

    /// <summary>
    /// Gemerkt werden SCHLUESSEL, nicht Plaetze — und ein Schluessel, den dieser Lauf
    /// nicht fuehrt, faellt STILL weg (hier die abgewaehlte Solarthermie und ein gar
    /// nicht vorhandener Name). Mit Indizes zeigte derselbe Stand nach dem naechsten
    /// Lauf auf eine andere Reihe.
    /// </summary>
    [Fact]
    public void Ein_unbekannter_Schluessel_im_Gedaechtnis_faellt_still_weg()
    {
        var stand = new Ganglinienstand();
        stand.Merken(-1, false, false, new[] { "HEIZSTAB", "SOLARTHERMIE", "GIBTESNICHT" });

        var seite = WaermeZeichnen(stand: stand);

        Assert.Equal(new[] { "HEIZSTAB" }, seite.Instance.GewaehlteErzeuger);
        Assert.Empty(seite.Instance.GewaehlteSpeicher);
        Assert.False(seite.Instance.Bedarfslinie);
    }

    /// <summary>
    /// <b>Das Gedaechtnis ist je Fall eigen.</b> bunit faehrt Faelle nebeneinander;
    /// ein prozessweiter Halter, den ein Fall zuruecksetzt, waehrend der naechste
    /// liest, waere kein Pruefstand. Jeder Fall gibt deshalb seinen EIGENEN
    /// <see cref="Ganglinienstand"/> herein — dieselbe Bauart wie
    /// <c>Filterstandvorgabe</c> bei den Katalogdialogen (S2.5).
    /// </summary>
    [Fact]
    public void Das_Gedaechtnis_eines_Standes_erreicht_keinen_anderen()
    {
        var eigen = new Ganglinienstand();
        var meins = WaermeZeichnen(stand: eigen);
        meins.FindAll("input[type='checkbox']")[1].Change(false);   // Bedarfslinie aus

        Assert.True(eigen.Belegt);
        Assert.False(eigen.Bedarfslinie);

        var fremd = new Ganglinienstand();
        var anderes = WaermeZeichnen(stand: fremd);

        // Der fremde Stand bekommt die VORBELEGUNG, nicht die Wahl des ersten.
        Assert.True(anderes.Instance.Bedarfslinie);
        Assert.True(fremd.Belegt);
        Assert.True(fremd.Bedarfslinie);
        Assert.False(eigen.Bedarfslinie);
    }

    // =====================================================================
    // Stromgang
    // =====================================================================

    private static StromgangDaten Strom() => new StromgangDaten
    {
        Reihen = new[]
        {
            new Ganglinienreihe("GESAMT", "Summe Stromverbrauch", true),
            new Ganglinienreihe("PROFIL_LASTGANG", "Lastgangprofil", true),
            new Ganglinienreihe("WAERMEPUMPE", "Wärmepumpe", true),
            new Ganglinienreihe("HEIZSTAB", "Heizstab", false),
            new Ganglinienreihe("BHKW_STROM", "BHKW", true),
            new Ganglinienreihe("PV", "PV", false)
        }
    };

    private IRenderedComponent<StromgangReiter> StromZeichnen(Action<IReadOnlyList<string>>? csv = null,
                                                              Ganglinienstand? stand = null)
        => Render<StromgangReiter>(p =>
        {
            p.Add(x => x.Daten, Strom());
            p.Add(x => x.Modell, Modell);
            p.Add(x => x.Gedaechtnis, stand ?? _stromStand);
            if (csv is not null)
                p.Add(x => x.Csv, EventCallback.Factory.Create<IReadOnlyList<string>>(this, csv));
        });

    /// <summary>
    /// <b>Das Stromgangbild steht als SVG im Baum</b> (Etappe DG-E3, Gruppe (a)) —
    /// unter der Kennung <c>simerg-stromgang</c>, und ohne ein Pixelbild daneben. Der
    /// Schalter „sortiert" stellt die Achsenart auf den RANG um.
    /// </summary>
    [Fact]
    public void Stromgang_steht_als_DiagrammSvg()
    {
        var seite = StromZeichnen();

        Assert.Single(seite.FindComponents<DiagrammSvg>());
        Assert.Equal("simerg-stromgang", seite.FindComponent<DiagrammSvg>().Instance.Kennung);
        Assert.Equal("kW", seite.FindComponent<DiagrammSvg>().Instance.Einheit);
        Assert.Empty(seite.FindAll("img"));
    }

    /// <summary>Ausgangszustand „nur Gesamt an" — woertlich <c>SetControl</c> :224-228.</summary>
    [Fact]
    public void Stromgang_startet_mit_nur_der_Gesamtlinie()
    {
        var seite = StromZeichnen();
        Assert.Equal(new[] { "GESAMT" }, seite.Instance.GewaehlteReihen);
    }

    /// <summary>
    /// #234: Die VORGABE bleibt hier „nur Gesamt" (der Anwenderwunsch betraf das
    /// Waermeblatt) — das GEDAECHTNIS gilt aber auch hier, sonst faellt die Wahl beim
    /// Blattwechsel zurueck.
    /// </summary>
    [Fact]
    public void Stromgang_merkt_die_Reihenwahl_ueber_den_Blattwechsel()
    {
        var erste = StromZeichnen();
        Assert.Equal(new[] { "GESAMT" }, erste.Instance.GewaehlteReihen);

        erste.FindAll("input[type='checkbox']")[0].Change(true);   // sortiert
        erste.FindAll("input[type='checkbox']")[2].Change(true);   // Lastgangprofil dazu

        Assert.Equal(new[] { "GESAMT", "PROFIL_LASTGANG" }, erste.Instance.GewaehlteReihen);

        var zweite = StromZeichnen();

        Assert.Equal(new[] { "GESAMT", "PROFIL_LASTGANG" }, zweite.Instance.GewaehlteReihen);
        Assert.True(zweite.Instance.Sortiert);
    }

    /// <summary>Befund W11-B41: Der Sortiertumschalter ist ERGAENZT.</summary>
    [Fact]
    public void Stromgang_hat_jetzt_einen_Sortiertumschalter()
    {
        var seite = StromZeichnen();
        _auftraege.Clear();

        seite.FindAll("input[type='checkbox']")[0].Change(true);

        Assert.True(seite.Instance.Sortiert);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.Stromgang && a.Sortiert);
    }

    [Fact]
    public void Stromgang_zeigt_nur_die_vorhandenen_Reihen()
    {
        var seite = StromZeichnen();
        string text = seite.Markup;

        Assert.Contains("Lastgangprofil", text);
        Assert.DoesNotContain("Heizstab", text);
        Assert.DoesNotContain(">PV<", text);
    }

    [Fact]
    public void Stromgang_meldet_die_gewaehlten_Reihen_an_den_Export()
    {
        IReadOnlyList<string> gemeldet = Array.Empty<string>();
        var seite = StromZeichnen(r => gemeldet = r);

        seite.Find("button.epos-simerg-knopf").Click();
        Assert.Equal(new[] { "GESAMT" }, gemeldet);
    }

    // =====================================================================
    // Ergebnisreiter
    // =====================================================================

    private static AutarkieDaten Autarkie(bool pv = true, bool st = true, bool stBekannt = true)
        => new AutarkieDaten
        {
            HatPv = pv,
            HatSolarthermie = st,
            AutarkiePvProzent = 38.1,
            DeckungStProzent = 12.4,
            DeckungStBekannt = stBekannt,
            NutzungsgradStProzent = 62.0,
            Co2ErsparnisKg = 4820.0,
            SpeichernutzenKwh = 1250.0,
            SpeicherKwh = 5.0,
            SpeichernutzenWaermeKwh = 1234.0,
            WaermeDeckungMonate = st ? "Solare Deckung je Monat: Jan 4 % · Feb 6 %" : ""
        };

    private IRenderedComponent<ErgebnisReiter> ErgebnisZeichnen(AutarkieDaten a,
                                                                Action<double>? kapazitaet = null)
        => Render<ErgebnisReiter>(p =>
        {
            p.Add(x => x.Autarkie, a);
            p.Add(x => x.Modell, Modell);
            p.Add(x => x.WaermegangInhalt, (RenderFragment)(b => b.AddMarkupContent(0, "<i>wg</i>")));
            p.Add(x => x.StromgangInhalt, (RenderFragment)(b => b.AddMarkupContent(0, "<i>sg</i>")));
            if (kapazitaet is not null)
                p.Add(x => x.KapazitaetGeaendert, EventCallback.Factory.Create<double>(this, kapazitaet));
        });

    /// <summary>
    /// Aus den vier Navigationsknoepfen wird ein innerer Reiter — seit #222 mit
    /// DREI Blaettern.
    ///
    /// <para><b>Das Blatt „Uebersicht" ist gefallen</b> (Anwenderentscheid
    /// 11.09.2026, Punkt a: „eine Uebersicht"). Es zeigte dieselben Zahlen wie der
    /// gleichnamige HAUPTreiter, der seit #216 ohnehin das erste Blatt der Seite
    /// ist — zwei Ringe, zwei Rest-Kacheln und das Eigenanteilsraster, alles
    /// doppelt. Das erste Blatt dieses Reiters ist seither die AUTARKIE.</para>
    /// </summary>
    [Fact]
    public void Der_Ergebnisreiter_traegt_drei_Blaetter()
    {
        var seite = ErgebnisZeichnen(Autarkie());
        Assert.Equal(3, seite.FindAll("button[role='tab']").Count);
        Assert.Equal("AUTARKIE", seite.Instance.AktivesBlatt);
        Assert.DoesNotContain("reiter-UEBERSICHT", seite.Markup);
    }

    [Fact]
    public void Die_zwei_Fremdinhalte_erscheinen_in_ihrem_Blatt()
    {
        var seite = ErgebnisZeichnen(Autarkie());

        seite.Find("button[role='tab'][id='reiter-WAERMEGANG']").Click();
        Assert.Contains("<i>wg</i>", seite.Markup);

        seite.Find("button[role='tab'][id='reiter-STROMGANG']").Click();
        Assert.Contains("<i>sg</i>", seite.Markup);
    }

    /// <summary>Kacheln und Balken der Autarkie-Analyse.</summary>
    [Fact]
    public void Die_Autarkie_zeigt_Kacheln_Balken_und_Monatsbild()
    {
        var seite = ErgebnisZeichnen(Autarkie());
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Equal(3, seite.FindAll(".epos-kennzahlkachel").Count);
        Assert.Equal(2, seite.FindAll("meter").Count);
        Assert.Contains(_auftraege, a => a.Bild == Bilder.AutarkieMonate);
    }

    /// <summary>
    /// <b>Der Monatsstapel der Autarkie steht im SVG-Baustein</b> (Etappe DG-E3,
    /// Gruppe (c)). Zwölf starre Fächer tragen keine Zeitachse: Es gibt dort nichts
    /// zu zoomen, und der Baustein lässt die Leiste deshalb von selbst weg — ein
    /// <c>OhneZoom</c> setzt der Reiter nicht. Was das Bild dafür zeigt, ist der
    /// Wert der Schicht unter dem Zeiger (DG-E3-10).
    /// </summary>
    [Fact]
    public void Der_Monatsstapel_der_Autarkie_steht_im_SvgBaustein()
    {
        var seite = ErgebnisZeichnen(Autarkie(st: false));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Empty(seite.FindAll("img"));
        DiagrammSvg bild = seite.FindComponents<DiagrammSvg>().Single().Instance;

        Assert.Equal("simerg-monate", bild.Kennung);
        Assert.False(bild.OhneZoom);
        Assert.True(bild.ZeigtWertAmElement);
        Assert.Empty(seite.FindAll(".epos-diagramm-leiste"));
    }

    /// <summary>
    /// <b>W11b‑B‑23 (09.09.2026).</b> Die zwei Quoten der Autarkiekacheln
    /// standen als letzte Zahlen des Dialogs in „F1" („38,1 %"), während die
    /// Kacheln der Übersicht und jede Kennzahlenliste N2 setzen. Der Balken
    /// darunter behält seinen eigenen Wert: Er ist ein Attribut und keine
    /// Anzeige (0…100, invariant).
    /// </summary>
    [Fact]
    public void Die_Autarkiequoten_stehen_mit_zwei_Nachkommastellen()
    {
        var seite = ErgebnisZeichnen(Autarkie());
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Contains("38,10 %", seite.Markup);
        Assert.Contains("12,40 %", seite.Markup);
        Assert.Contains("62,00", seite.Markup);          // therm. Nutzungsgrad
        Assert.Contains("value=\"38.1\"", seite.Markup);  // der Balken bleibt invariant
    }

    /// <summary>Ohne Waermebedarf steht „nicht benoetigt" statt einer Quote.</summary>
    [Fact]
    public void Ohne_Waermebedarf_steht_nicht_benoetigt()
    {
        var seite = ErgebnisZeichnen(Autarkie(stBekannt: false));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Contains("nicht benötigt", seite.Markup);
    }

    /// <summary>
    /// Befund W11-B32: Die Kapazitaet ist eine Was-waere-wenn-Groesse. Sie meldet
    /// sich an die Huelle (die rechnet neu) und traegt den Hinweis, dass sie
    /// nicht gespeichert wird.
    /// </summary>
    [Fact]
    public void Die_Kapazitaet_meldet_sich_und_nennt_sich_fluechtig()
    {
        double gemeldet = 0;
        var seite = ErgebnisZeichnen(Autarkie(), k => gemeldet = k);
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Contains("nicht gespeichert", seite.Markup);
        seite.Find("input[type='text']").Input("12");
        Assert.Equal(12.0, gemeldet);
    }

    /// <summary>Ohne PV bleiben Kachel, Balken und das Kapazitaetsfeld weg.</summary>
    [Fact]
    public void Ohne_Photovoltaik_bleibt_die_PV_Kachel_weg()
    {
        var seite = ErgebnisZeichnen(Autarkie(pv: false));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Single(seite.FindAll("meter"));
        Assert.Empty(seite.FindAll("input[type='text']"));
    }

    // =====================================================================
    // Waerme-Autarkie (Solarthermie)
    // =====================================================================

    private static List<string> Kennungen(IRenderedComponent<ErgebnisReiter> seite)
        => seite.FindComponents<DiagrammSvg>().Select(k => k.Instance.Kennung).ToList();

    /// <summary>
    /// <b>Nur Solarthermie:</b> Das Blatt zeigt allein den Waerme-Monatsstapel
    /// „Waermebedarf &amp; Deckung" — kein leeres Strombild daneben.
    /// </summary>
    [Fact]
    public void Nur_mit_Solarthermie_steht_allein_das_Waermebild()
    {
        var seite = ErgebnisZeichnen(Autarkie(pv: false, st: true));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Equal(new[] { "simerg-waermemonate" }, Kennungen(seite));
        Assert.Contains(_auftraege, a => a.Bild == Bilder.WaermeAutarkieMonate);
        Assert.DoesNotContain(_auftraege, a => a.Bild == Bilder.AutarkieMonate);
    }

    /// <summary><b>PV und Solarthermie:</b> beide Bilder untereinander, Strom zuerst.</summary>
    [Fact]
    public void Mit_PV_und_Solarthermie_stehen_beide_Bilder_untereinander()
    {
        var seite = ErgebnisZeichnen(Autarkie(pv: true, st: true));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Equal(new[] { "simerg-monate", "simerg-waermemonate" }, Kennungen(seite));
    }

    /// <summary><b>Nur PV:</b> allein das Strombild, kein Waermeauftrag.</summary>
    [Fact]
    public void Ohne_Solarthermie_steht_allein_das_Strombild()
    {
        var seite = ErgebnisZeichnen(Autarkie(pv: true, st: false));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Equal(new[] { "simerg-monate" }, Kennungen(seite));
        Assert.DoesNotContain(_auftraege, a => a.Bild == Bilder.WaermeAutarkieMonate);
    }

    // ---- Speichernutzen je Seite und Monatsdeckung unter dem Waermebild ----

    /// <summary>Die leise Zeile der Ergebniskachel (die letzte Kachel des Blatts).</summary>
    private static string SpeichernutzenQuelle(IRenderedComponent<ErgebnisReiter> seite)
        => seite.FindAll(".epos-kennzahlkachel-quelle").Last().TextContent;

    /// <summary>
    /// <b>Nur Solarthermie:</b> Die Ergebniskachel zeigt den solaren Speicheranteil
    /// statt der PV-Zahl.
    /// </summary>
    [Fact]
    public void Nur_mit_Solarthermie_zeigt_die_Kachel_den_Speichernutzen_Waerme()
    {
        var seite = ErgebnisZeichnen(Autarkie(pv: false, st: true));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Equal("Speichernutzen Wärme: 1.234 kWh/Jahr", SpeichernutzenQuelle(seite));
    }

    /// <summary><b>PV und Solarthermie:</b> beide Zeilen untereinander, Strom zuerst.</summary>
    [Fact]
    public void Mit_PV_und_Solarthermie_zeigt_die_Kachel_beide_Speichernutzen()
    {
        var seite = ErgebnisZeichnen(Autarkie(pv: true, st: true));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Equal("Speichernutzen: 1.250 kWh/Jahr\nSpeichernutzen Wärme: 1.234 kWh/Jahr",
                     SpeichernutzenQuelle(seite));
    }

    /// <summary><b>Nur PV:</b> die Kachel bleibt, wie sie war.</summary>
    [Fact]
    public void Ohne_Solarthermie_zeigt_die_Kachel_nur_den_Speichernutzen_Strom()
    {
        var seite = ErgebnisZeichnen(Autarkie(pv: true, st: false));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Equal("Speichernutzen: 1.250 kWh/Jahr", SpeichernutzenQuelle(seite));
    }

    /// <summary>
    /// Unter dem Waermebild steht die solare Deckung je Monat als Herleitungszeile; ohne
    /// Solarthermie (und damit ohne Waermebild) keine solche Zeile.
    /// </summary>
    [Fact]
    public void Unter_dem_Waermebild_steht_die_Monatsdeckung_in_Prozent()
    {
        var mit = ErgebnisZeichnen(Autarkie(pv: true, st: true));
        mit.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();
        Assert.Equal("Solare Deckung je Monat: Jan 4 % · Feb 6 %",
                     mit.Find(".epos-herleitung-text").TextContent);

        var ohne = ErgebnisZeichnen(Autarkie(pv: true, st: false));
        ohne.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();
        Assert.Empty(ohne.FindAll(".epos-herleitung-text"));
    }

    /// <summary>Eine leere Monatszeile zeichnet keine leere Herleitung.</summary>
    [Fact]
    public void Ohne_Monatszeile_steht_keine_leere_Herleitung()
    {
        AutarkieDaten a = Autarkie(pv: false, st: true);
        a.WaermeDeckungMonate = "";
        var seite = ErgebnisZeichnen(a);
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Empty(seite.FindAll(".epos-herleitung-text"));
    }

    /// <summary>
    /// Ohne PV und ohne Solarthermie bleibt das Blatt, wie es war: der
    /// Strom-Monatsstapel (die Huelle liefert dann ihren Leerhinweis).
    /// </summary>
    [Fact]
    public void Ohne_PV_und_Solarthermie_bleibt_das_Strombild()
    {
        var seite = ErgebnisZeichnen(Autarkie(pv: false, st: false));
        seite.Find("button[role='tab'][id='reiter-AUTARKIE']").Click();

        Assert.Equal(new[] { "simerg-monate" }, Kennungen(seite));
    }
}
