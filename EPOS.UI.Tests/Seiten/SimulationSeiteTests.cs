using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die ANSICHT „SIMULATION" (Auftrag #207, Stufe S1 des Konzepts
/// „Simulationsablauf ohne Dialog").
///
/// <para><b>Soll</b> (Konzept 2, Anwenderentscheid SIM‑Q1): drei Schritte in EINER
/// Ansicht — ① Konfiguration, ② „Simulation starten ▶" als KNOPF, ③ Ergebnis.
/// ② ist gesperrt, solange die Konfiguration ungespeichert oder die Vorprüfung rot
/// ist, und nennt den Grund; ③ ist gesperrt, solange kein Lauf gerechnet ist. Eine
/// Marke <c>schritt=3</c> ohne Ergebnis fällt auf ① zurück und sagt warum. „← zurück"
/// fragt bei ungespeicherter Konfiguration nach (62b‑E‑1).</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt (Hausregel seit iU9‑W8): Die
/// Erwartungswerte sind deutsche Beschriftungen aus <c>MyResource</c>, und die
/// CI-Läufer auf macOS und Windows laufen englisch.</para>
/// </summary>
public class SimulationSeiteTests : EposBunitContext
{
    public SimulationSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    //  Probendaten — die zwei eingebetteten Seiten in ihrer schmalsten Form
    // =====================================================================

    private readonly List<string> _verschoben = new();
    private int _gespeichert;
    private int _laeufe;
    private bool _speichernGelingt = true;
    private TaskCompletionSource<Rueckmeldung>? _laufFertig;

    /// <summary>Eine Wärmeerzeugerkarte reicht — an ihr hängen ▲▼ und damit die Änderung.</summary>
    private SimulationKonfigDaten Konfigdaten() => new SimulationKonfigDaten
    {
        IdProjekt = 1030,
        Gruppen = new[]
        {
            new KachelGruppe
            {
                Titel = "Wärmeerzeuger",
                Zeilen = new[]
                {
                    new ErzeugerZeile
                    {
                        DbWert = "BHKW",
                        IdAnlage = 4711,
                        IdType = 11,
                        Bezeichner = "BHKW 1",
                        HatAnlage = true,
                        Kachel = new ErzeugerKachelDaten
                        {
                            Schluessel = "BHKW",
                            Rang = "1.",
                            Titel = "BHKW 1",
                            Reihenfolge = true,
                            AbMoeglich = true
                        }
                    }
                }
            }
        },
        SpeicherLeerText = "Dieses Projekt führt keinen Pufferspeicher."
    };

    private SimulationKonfigDienste Konfigdienste() => new SimulationKonfigDienste
    {
        Laden = _ => Konfigdaten(),
        Verschieben = (w, r) => _verschoben.Add(w + ":" + r),
        Speichern = () => { _gespeichert++; return _speichernGelingt; }
    };

    private SimulationErgebnisDienste Ergebnisdienste() => new SimulationErgebnisDienste
    {
        Laden = _ => new SimulationErgebnisDaten
        {
            IdProjekt = 1030,
            ErgebnisGueltig = true,
            // Das Blatt „Stromspeicher" muss es GEBEN, sonst faellt der Reiter auf
            // das erste zurueck - und die Marke aus der Auslegung liefe ins Leere.
            ReiterStromspeicher = true
        },
        Bild = _ => null,
        Laufen = melder =>
        {
            _laeufe++;
            _laufFertig = new TaskCompletionSource<Rueckmeldung>();
            return _laufFertig.Task;
        },
        Speichern = () => new Rueckmeldung(true, "gespeichert")
    };

    private SimulationAnsichtDienste Dienste(bool ergebnisDa = false, string sperrgrund = "")
        => new SimulationAnsichtDienste
        {
            Konfiguration = new Dictionary<string, object>
            {
                ["Dienste"] = Konfigdienste(),
                ["StartProjekt"] = 1030
            },
            Ergebnis = new Dictionary<string, object>
            {
                ["Dienste"] = Ergebnisdienste(),
                ["StartProjekt"] = 1030
            },
            Sperrgrund = () => sperrgrund,
            ErgebnisVorhanden = () => ergebnisDa
        };

    private IRenderedComponent<SimulationSeite> Zeigen(string marke = "",
                                                       bool ergebnisDa = false,
                                                       string sperrgrund = "",
                                                       Action? geschlossen = null)
        => Render<SimulationSeite>(p =>
        {
            p.Add(x => x.Dienste, Dienste(ergebnisDa, sperrgrund));
            p.Add(x => x.Marke, marke);
            p.Add(x => x.ProjektText, "Projekt „B3-Kaskade“");
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen);
        });

    // ---- Lesehilfen ------------------------------------------------------

    private static IReadOnlyList<IElement> Schritte(IRenderedComponent<SimulationSeite> cut)
        => cut.FindAll("button.epos-ablaufleiste-knopf");

    private static IElement Rechnen(IRenderedComponent<SimulationSeite> cut)
        => cut.Find("button.epos-ablaufleiste-rechnen");

    /// <summary>„Ergebnis speichern" der Werkzeugleiste (#216).</summary>
    private static IElement Speichern(IRenderedComponent<SimulationSeite> cut)
        => cut.Find("button.epos-simansicht-speichern");

    /// <summary>Das SICHTBARE Blatt — das andere steht daneben und schweigt.</summary>
    private static IReadOnlyList<IElement> Sichtbar(IRenderedComponent<SimulationSeite> cut)
        => cut.FindAll(".epos-simansicht-blatt:not(.epos-simansicht-blatt--aus)");

    /// <summary>
    /// ▼ der Wärmeerzeugerkarte in Schritt ① — dieselbe Änderung wie in
    /// <c>SimulationKonfigSeiteTests.Die_Pfeile_melden_das_Verschieben_mit_Richtung</c>.
    /// Der erste Glyphenknopf ist ▲ und hier gesperrt; ▼ ist der zweite.
    /// </summary>
    private static void Verschieben(IRenderedComponent<SimulationSeite> cut)
        => cut.FindAll("div.epos-erzeugerkachel")[0]
              .QuerySelectorAll("button.epos-erzeugerkachel-glyphe")[1].Click();

    // =====================================================================
    //  Der Aufbau
    // =====================================================================

    /// <summary>
    /// Drei Schritte, zwei Blätter und EIN Knopf dazwischen — die Ablaufleiste der
    /// Auslegung, auf drei Stationen gekürzt (SIM‑Q1).
    /// </summary>
    [Fact]
    public void Die_Ablaufleiste_fuehrt_zwei_Blaetter_und_einen_Rechenknopf()
    {
        var cut = Zeigen();

        var schritte = Schritte(cut);
        Assert.Equal(2, schritte.Count);
        Assert.Equal("1 Konfiguration", schritte[0].TextContent.Trim());
        Assert.Equal("3 Ergebnis", schritte[1].TextContent.Trim());
        Assert.Contains("Simulation starten", Rechnen(cut).TextContent);
    }

    /// <summary>Kopf: Titel, Projektzeile und das eine „← zurück" (Konzept 2).</summary>
    [Fact]
    public void Der_Kopf_traegt_Titel_Projekt_und_den_einen_Rueckweg()
    {
        var cut = Zeigen();

        Assert.Equal("Simulation", cut.Find("h1.epos-seite-titel").TextContent);
        Assert.Contains("B3-Kaskade", cut.Find(".epos-simansicht-kontext").TextContent);

        var zurueck = cut.FindAll(".epos-simansicht-kopfaktionen button.epos-knopf");
        Assert.Single(zurueck);
        Assert.Equal("← zurück", zurueck[0].TextContent.Trim());
    }

    // =====================================================================
    //  #216, Punkt 2 — EINE rechtsbuendige Werkzeugleiste
    // =====================================================================

    /// <summary>
    /// <b>Windows-Abnahme #216, Punkt 2:</b> „Bringe die Elemente aus dem Dialog auf
    /// die rechte Seite mit besserem Design." Alles, was die Ansicht bedient, steht
    /// in EINER Leiste im Kopf: die Schrittgruppe (kompakt, ohne Lücke), daneben
    /// „Ergebnis speichern", dann das EINE Paar [i] [KI] und das EINE „← zurück".
    /// </summary>
    [Fact]
    public void Die_Werkzeugleiste_steht_im_Kopf_und_traegt_die_Gruppe_zusammen()
    {
        var cut = Zeigen(ergebnisDa: true);

        IElement leiste = cut.Find(".epos-simansicht-kopf .epos-simansicht-werkzeugleiste");

        // Die Schrittgruppe ist EIN Element der Leiste und traegt die kompakte Form.
        Assert.Single(leiste.QuerySelectorAll("nav.epos-ablaufleiste--kompakt"));
        Assert.Single(leiste.QuerySelectorAll("button.epos-simansicht-speichern"));
        Assert.Single(leiste.QuerySelectorAll(".epos-simansicht-kopfaktionen"));

        // Die Ablaufleiste steht NICHT mehr als eigene Zeile unter dem Kopf.
        Assert.Empty(cut.FindAll(".epos-simansicht > nav.epos-ablaufleiste"));
    }

    /// <summary>
    /// EIN Paar [i] [KI] je Ansicht (Hausregel W11b‑B‑9 sinngemäß) und KEINE
    /// Fußleiste mehr: Beides trug die eingebettete Ergebnisseite bis #216 ein
    /// zweites Mal.
    /// </summary>
    [Fact]
    public void Die_Ansicht_traegt_genau_ein_Paar_Info_und_KI_und_keine_Fussleiste()
    {
        var cut = Zeigen(marke: SimulationMarke.SCHRITT_ERGEBNIS, ergebnisDa: true);

        Assert.Single(cut.FindAll("button.epos-infoknopf"));
        Assert.Empty(cut.FindAll("div.epos-simerg-fuss"));
        Assert.Empty(cut.FindAll("div.epos-simerg-kopf"));
    }

    /// <summary>
    /// „Ergebnis speichern" ist NUR in ③ frei — in ① gibt es kein Ergebnis, das
    /// man speichern könnte, und während des Laufs auch nicht.
    /// </summary>
    [Fact]
    public void Ergebnis_speichern_ist_nur_in_Schritt_drei_frei()
    {
        var cut = Zeigen();

        Assert.Equal("Ergebnis speichern", Speichern(cut).TextContent.Trim());
        Assert.True(Speichern(cut).HasAttribute("disabled"));

        Rechnen(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));

        // Waehrend des Laufs bleibt er gesperrt.
        Assert.True(Speichern(cut).HasAttribute("disabled"));

        cut.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        cut.WaitForAssertion(() => Assert.False(Speichern(cut).HasAttribute("disabled")));

        // Zurueck auf ① sperrt ihn wieder - dort gehoert er nicht hin.
        Schritte(cut)[0].Click();
        Assert.True(Speichern(cut).HasAttribute("disabled"));
    }

    /// <summary>Ohne Marke steht Schritt ① — und nur er.</summary>
    [Fact]
    public void Ohne_Marke_steht_die_Konfiguration()
    {
        var cut = Zeigen();

        Assert.Equal(SimulationSchritt.Konfiguration, cut.Instance.Schritt);
        Assert.Single(Sichtbar(cut));
        Assert.NotEmpty(cut.FindAll("div.epos-simkonfig"));
        Assert.Empty(cut.FindAll("div.epos-simerg"));
        Assert.Equal("schritt=1", cut.Instance.AktuelleMarke);
    }

    // =====================================================================
    //  Die Leiste schaltet
    // =====================================================================

    /// <summary>
    /// Mit gerechnetem Lauf schaltet die Leiste auf ③ — und ① BLEIBT im Baum
    /// stehen, nur unsichtbar: Ein <c>@if</c> würde die Komponente entsorgen.
    /// </summary>
    [Fact]
    public void Die_Leiste_schaltet_auf_das_Ergebnis_und_laesst_die_Konfiguration_stehen()
    {
        var cut = Zeigen(ergebnisDa: true);

        Schritte(cut)[1].Click();

        Assert.Equal(SimulationSchritt.Ergebnis, cut.Instance.Schritt);
        Assert.NotEmpty(cut.FindAll("div.epos-simerg"));

        // Beide Blaetter stehen, sichtbar ist genau eines.
        Assert.Equal(2, cut.FindAll(".epos-simansicht-blatt").Count);
        Assert.Single(Sichtbar(cut));
        Assert.Contains("epos-simerg", Sichtbar(cut)[0].InnerHtml);

        // Und zurueck auf ① - ohne dass das Ergebnis entsorgt wuerde.
        Schritte(cut)[0].Click();
        Assert.Equal(SimulationSchritt.Konfiguration, cut.Instance.Schritt);
        Assert.Equal(2, cut.FindAll(".epos-simansicht-blatt").Count);
    }

    // =====================================================================
    //  Schritt ③ ist gesperrt, solange nichts gerechnet ist
    // =====================================================================

    [Fact]
    public void Ohne_Lauf_ist_der_Ergebnisschritt_gesperrt_und_sagt_warum()
    {
        var cut = Zeigen();

        IElement ergebnis = Schritte(cut)[1];
        Assert.Equal("true", ergebnis.GetAttribute("aria-disabled"));
        Assert.Contains("wenn die Simulation einmal gelaufen ist",
                        ergebnis.GetAttribute("title"));

        // WEICHE Sperre (Hausregel W16b-E-6): Der Knopf nimmt den Klick an und
        // MELDET den Grund, statt stumm zu bleiben.
        ergebnis.Click();

        Assert.Equal(SimulationSchritt.Konfiguration, cut.Instance.Schritt);
        Assert.Contains("wenn die Simulation einmal gelaufen ist",
                        cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// Die Kachel „Simulation" öffnet ③ — ohne Ergebnis fällt die Ansicht SELBST
    /// auf ① zurück und sagt warum (Konzept 2, Einstiegsmarke).
    /// </summary>
    [Fact]
    public void Die_Marke_Ergebnis_faellt_ohne_Ergebnis_auf_die_Konfiguration_zurueck()
    {
        var cut = Zeigen(marke: SimulationMarke.SCHRITT_ERGEBNIS);

        Assert.Equal(SimulationSchritt.Konfiguration, cut.Instance.Schritt);
        Assert.Contains("noch kein gerechnetes Ergebnis", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>Mit gerechnetem Lauf macht dieselbe Marke in ③ auf.</summary>
    [Fact]
    public void Die_Marke_Ergebnis_macht_mit_Lauf_in_Schritt_drei_auf()
    {
        var cut = Zeigen(marke: SimulationMarke.SCHRITT_ERGEBNIS, ergebnisDa: true);

        Assert.Equal(SimulationSchritt.Ergebnis, cut.Instance.Schritt);
        Assert.NotEmpty(cut.FindAll("div.epos-simerg"));

        // Schritt ① ist dabei NICHT gebaut: Sein Aufbau liest die Datenbank, und
        // wer ins Ergebnis kommt, will es sehen.
        Assert.Empty(cut.FindAll("div.epos-simkonfig"));
    }

    /// <summary>
    /// Die MARKE trägt das Reiterblatt mit — der Rückweg aus der
    /// Stromspeicher-Auslegung landet damit auf „Stromspeicher".
    /// </summary>
    [Fact]
    public void Die_Marke_traegt_das_Reiterblatt_in_die_Ergebnisseite()
    {
        var cut = Zeigen(marke: SimulationMarke.Schreiben(3, SimulationErgebnisSeite.Blatt.Stromspeicher),
                         ergebnisDa: true);

        Assert.Equal(SimulationSchritt.Ergebnis, cut.Instance.Schritt);
        Assert.Equal(SimulationErgebnisSeite.Blatt.Stromspeicher, cut.Instance.AktuelleMarke.Split('=')[^1]);
    }

    // =====================================================================
    //  Schritt ② — der Rechenknopf
    // =====================================================================

    /// <summary>
    /// Ungespeicherte Konfiguration sperrt ② und nennt den Grund am Knopf UND als
    /// Banner (der <c>title</c> erreicht nur den Zeiger).
    /// </summary>
    [Fact]
    public void Eine_ungespeicherte_Konfiguration_sperrt_den_Rechenknopf()
    {
        var cut = Zeigen();

        Assert.False(Rechnen(cut).HasAttribute("disabled"));

        Verschieben(cut);

        Assert.Equal(new[] { "BHKW:1" }, _verschoben);
        Assert.True(Rechnen(cut).HasAttribute("disabled"));
        Assert.Contains("noch nicht gespeichert", Rechnen(cut).GetAttribute("title"));
        Assert.Contains("noch nicht gespeichert", cut.Find(".epos-warnbanner").TextContent);

        // Speichern macht den Knopf wieder frei.
        cut.Find("div.epos-leiste button.epos-knopf").Click();

        Assert.Equal(1, _gespeichert);
        Assert.False(Rechnen(cut).HasAttribute("disabled"));
    }

    /// <summary>Die ROTE Vorprüfung sperrt ② ebenfalls — und sie gewinnt.</summary>
    [Fact]
    public void Eine_rote_Vorpruefung_sperrt_den_Rechenknopf_mit_ihrem_Grund()
    {
        var cut = Zeigen(sperrgrund: "Die Datenbank ist nicht auf dem benötigten Stand.");

        Assert.True(Rechnen(cut).HasAttribute("disabled"));
        Assert.Contains("benötigten Stand", Rechnen(cut).GetAttribute("title"));
        Assert.Contains("benötigten Stand", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// ② wechselt auf ③ und startet dort den Lauf — der Balken und der Abbruch
    /// gehören der Ergebnisseite, die sie seit jeher führt.
    /// </summary>
    [Fact]
    public void Der_Rechenknopf_wechselt_auf_das_Ergebnis_und_startet_den_Lauf()
    {
        var cut = Zeigen();

        Rechnen(cut).Click();

        Assert.Equal(SimulationSchritt.Ergebnis, cut.Instance.Schritt);
        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[role='progressbar']")));

        cut.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role='progressbar']")));

        // Danach steht ③ offen, auch ohne Auskunft der Huelle.
        Assert.Equal("false", Schritte(cut)[1].GetAttribute("aria-disabled") ?? "false");
    }

    // =====================================================================
    //  #216, Punkt 1 — die Marke schritt=2 (Anwenderentscheid SIM-E-1)
    // =====================================================================

    /// <summary>
    /// <b>SIM‑E‑1 (11.09.2026):</b> „Belege den Button (Kachel ‚Simulation') mit
    /// ‚Simulation starten'." Die Kachel öffnet die Ansicht mit
    /// <c>schritt=2</c> — und die tut, was der Rechenknopf tut: auf ③ wechseln und
    /// den Lauf starten. Schritt ① entsteht dabei gar nicht erst; sein Aufbau
    /// läse die Datenbank.
    /// </summary>
    [Fact]
    public void Die_Marke_Lauf_startet_die_Simulation_und_landet_in_Schritt_drei()
    {
        var cut = Zeigen(marke: SimulationMarke.SCHRITT_LAUF);

        Assert.Equal(SimulationSchritt.Ergebnis, cut.Instance.Schritt);
        cut.WaitForAssertion(() => Assert.Equal(1, _laeufe));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[role='progressbar']")));
        Assert.Empty(cut.FindAll("div.epos-simkonfig"));

        cut.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role='progressbar']")));
    }

    /// <summary>
    /// Eine Marke ist ein WUNSCH, kein Befehl: Ist der Lauf gesperrt, bleibt die
    /// Ansicht bei ① und nennt den Grund — dieselbe weiche Auskunft wie am
    /// Rechenknopf.
    /// </summary>
    [Fact]
    public void Die_Marke_Lauf_bleibt_bei_Sperre_in_Schritt_eins_und_nennt_den_Grund()
    {
        var cut = Zeigen(marke: SimulationMarke.SCHRITT_LAUF,
                         sperrgrund: "Die Datenbank ist nicht auf dem benötigten Stand.");

        Assert.Equal(SimulationSchritt.Konfiguration, cut.Instance.Schritt);
        Assert.Equal(0, _laeufe);
        Assert.NotEmpty(cut.FindAll("div.epos-simkonfig"));
        Assert.Contains("benötigten Stand", cut.Find(".epos-warnbanner").TextContent);
    }

    // =====================================================================
    //  Verlassen (Muster 62b-E-1)
    // =====================================================================

    /// <summary>Ohne Änderung gibt es keine Rückfrage — „← zurück" geht sofort.</summary>
    [Fact]
    public void Ohne_Aenderung_fuehrt_der_Rueckweg_unmittelbar_hinaus()
    {
        int zu = 0;
        var cut = Zeigen(geschlossen: () => zu++);

        cut.Find(".epos-simansicht-kopfaktionen button.epos-knopf").Click();

        Assert.Equal(1, zu);
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    /// <summary>
    /// Mit ungespeicherter Konfiguration kommt die Rückfrage: Speichern /
    /// Verwerfen / Bleiben; „Verwerfen" geht ohne zu schreiben hinaus.
    /// </summary>
    [Fact]
    public void Eine_ungespeicherte_Konfiguration_loest_die_Rueckfrage_aus()
    {
        int zu = 0;
        var cut = Zeigen(geschlossen: () => zu++);

        Verschieben(cut);
        cut.Find(".epos-simansicht-kopfaktionen button.epos-knopf").Click();

        Assert.Equal(0, zu);
        Assert.Contains("Ungespeicherte Konfiguration", cut.Find("[role='dialog']").TextContent);

        // „Verwerfen" ist der zweite der drei Knoepfe der Rueckfrage.
        cut.FindAll("div.epos-rueckfrage div.epos-leiste button")[1].Click();

        cut.WaitForAssertion(() => Assert.Equal(1, zu));
        Assert.Equal(0, _gespeichert);
    }

    /// <summary>„Speichern" schreibt und geht danach hinaus.</summary>
    [Fact]
    public void Die_Rueckfrage_speichert_und_geht_danach_hinaus()
    {
        int zu = 0;
        var cut = Zeigen(geschlossen: () => zu++);

        Verschieben(cut);
        cut.Find(".epos-simansicht-kopfaktionen button.epos-knopf").Click();
        cut.FindAll("div.epos-rueckfrage div.epos-leiste button")[0].Click();

        cut.WaitForAssertion(() => Assert.Equal(1, zu));
        Assert.Equal(1, _gespeichert);
        Assert.False(cut.Instance.Ungespeichert);
    }

    /// <summary>
    /// Ein GESCHEITERTER Speicherlauf lässt die Ansicht stehen — dieselbe Regel
    /// wie beim Assistenten und bei der Auslegung.
    /// </summary>
    [Fact]
    public void Ein_gescheitertes_Speichern_laesst_die_Ansicht_stehen()
    {
        _speichernGelingt = false;

        int zu = 0;
        var cut = Zeigen(geschlossen: () => zu++);

        Verschieben(cut);
        cut.Find(".epos-simansicht-kopfaktionen button.epos-knopf").Click();
        cut.FindAll("div.epos-rueckfrage div.epos-leiste button")[0].Click();

        Assert.Equal(0, zu);
        Assert.Equal(1, _gespeichert);
        Assert.True(cut.Instance.Ungespeichert);
    }

    /// <summary>
    /// <c>FrageVerlassen</c> ist der EINE Weg, auf dem ein Wirt fragt — die
    /// <c>AppWurzel</c> vor einem Ansichtswechsel und
    /// <c>Hauptfensterrahmen.FormClosing</c> vor dem Schließen des Programms.
    /// </summary>
    [Fact]
    public async Task FrageVerlassen_antwortet_ohne_Aenderung_sofort_mit_Verwerfen()
    {
        var cut = Zeigen();

        EPOS.UI.Seiten.Assistent.AssistentVerlassen weg =
            await cut.InvokeAsync(() => cut.Instance.FrageVerlassen());

        Assert.Equal(EPOS.UI.Seiten.Assistent.AssistentVerlassen.Verwerfen, weg);
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    // =====================================================================
    //  Die Marke
    // =====================================================================

    [Theory]
    [InlineData("", 0, "")]
    [InlineData("schritt=1", 1, "")]
    [InlineData("schritt=3", 3, "")]
    [InlineData("schritt=3;blatt=STROMSPEICHER", 3, "STROMSPEICHER")]
    [InlineData(" schritt = 3 ; blatt = STROMSPEICHER ", 3, "STROMSPEICHER")]
    [InlineData("unsinn", 0, "")]
    public void Die_Marke_wird_gelesen_wie_sie_geschrieben_wurde(string marke, int schritt, string blatt)
    {
        (int gelesen, string blattGelesen) = SimulationMarke.Lesen(marke);

        Assert.Equal(schritt, gelesen);
        Assert.Equal(blatt, blattGelesen);
    }

    [Fact]
    public void Die_Marke_schreibt_das_Blatt_nur_wenn_es_eines_gibt()
    {
        Assert.Equal("schritt=1", SimulationMarke.Schreiben(1, ""));
        Assert.Equal("schritt=3;blatt=ERGEBNIS", SimulationMarke.Schreiben(3, "ERGEBNIS"));
    }
}
