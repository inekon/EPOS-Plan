using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der Reiter „Berichte &amp; Kosten" (iU9-W5.6), Vorbild
/// <c>Views/BerichteKosten/UcBerichteKosten</c> (810 Z., K4).
///
/// <para>Soll (Konzept Navigation Berichte &amp; Kosten, Variante A): die vier Bereiche
/// als Reiterzeile auf dem Hausbaustein <c>Reiter</c> mit Statuszeile je Reiter, rechts in
/// derselben Zeile Stammname, Platzhalter-Umschalter, Hilfe und — als Ansicht — der
/// Rückweg; genau EINE sichtbare Seite, der Hinweis statt der Seite ohne Stammprojekt und
/// der Projektwechsel über den <c>SeitenZustand</c> — ohne Neuaufbau der Hülle.</para>
/// </summary>
public class BerichteKostenSeiteTests : BunitContext
{
    public BerichteKostenSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private readonly List<string> _gefragt = new();

    /// <summary>Ein Parametersatz je Seite; die Gruppenseiten nur mit Stamm.</summary>
    private IReadOnlyDictionary<string, object>? Gaben(string seite, bool mitStamm = true)
    {
        _gefragt.Add(seite);

        if (!mitStamm && (seite == BerichteKostenSeite.SEITE_WIRTSCHAFT
                          || seite == BerichteKostenSeite.SEITE_BERICHT))
            return null;

        return seite switch
        {
            BerichteKostenSeite.SEITE_UEBERSICHT => new Dictionary<string, object>
            {
                ["Laden"] = new Func<UebersichtStand>(() => new UebersichtStand())
            },
            BerichteKostenSeite.SEITE_KOSTEN => new Dictionary<string, object>
            {
                ["Laden"] = new Func<KostenStand>(() => new KostenStand
                {
                    Projektzeile = "Projekt: Musterhaus",
                    Kacheln = new[]
                    {
                        new KachelZeile { Titel = "Investition" },
                        new KachelZeile { Titel = "Betrieb" },
                        new KachelZeile { Titel = "Energie" }
                    }
                })
            },
            BerichteKostenSeite.SEITE_WIRTSCHAFT => new Dictionary<string, object>
            {
                ["Laden"] = new Func<WirtschaftlichkeitStand>(() => new WirtschaftlichkeitStand())
            },
            _ => new Dictionary<string, object>
            {
                ["Laden"] = new Func<BerichtStand>(() => new BerichtStand())
            }
        };
    }

    private IRenderedComponent<BerichteKostenSeite> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<BerichteKostenSeite>>? mehr = null,
        bool mitStamm = true, Func<string>? stamm = null)
    {
        _gefragt.Clear();
        return Render<BerichteKostenSeite>(p =>
        {
            p.Add(x => x.SeitenGaben, (string s) => Gaben(s, mitStamm));
            p.Add(x => x.Stamm, stamm ?? (() => "Musterhaus"));
            mehr?.Invoke(p);
        });
    }

    /// <summary>Die vier Reiterknöpfe des Rahmens — an ihrem Vorsatz, nicht an einem Reiter einer Seite.</summary>
    private static IReadOnlyList<IElement> Navknoepfe(IRenderedComponent<BerichteKostenSeite> cut)
        => cut.FindAll("button[role='tab'][id^='bk-reiter-']");

    /// <summary>Die Reiterleiste des Rahmens (die erste; eine Seite darunter darf eigene tragen).</summary>
    private static IElement Leiste(IRenderedComponent<BerichteKostenSeite> cut)
        => cut.Find(".epos-berichtekosten > .epos-reiter > .epos-reiter-kopfzeile > .epos-reiter-leiste");

    /// <summary>Das Ende der Reiterzeile des Rahmens.</summary>
    private static IElement Leistenende(IRenderedComponent<BerichteKostenSeite> cut)
        => cut.Find(".epos-berichtekosten > .epos-reiter > .epos-reiter-kopfzeile > .epos-reiter-leistenende");

    // =====================================================================
    // Aufbau
    // =====================================================================

    [Fact]
    public void Die_Reiterzeile_traegt_vier_Reiter_in_der_Reihenfolge_des_Vorlaeufers()
    {
        var cut = Zeige();

        var knoepfe = Navknoepfe(cut);
        Assert.Equal(4, knoepfe.Count);
        Assert.Contains("Übersicht", knoepfe[0].TextContent);
        Assert.Contains("Kosten", knoepfe[1].TextContent);
        Assert.Contains("Wirtschaftlichkeit", knoepfe[2].TextContent);
        Assert.Contains("Bericht", knoepfe[3].TextContent);
    }

    [Fact]
    public void Die_Reiterzeile_ist_der_Hausbaustein_Reiter()
    {
        var cut = Zeige();

        Assert.Equal("tablist", Leiste(cut).GetAttribute("role"));
        Assert.Equal("tab", Navknoepfe(cut)[0].GetAttribute("role"));
        Assert.Equal("true", Navknoepfe(cut)[0].GetAttribute("aria-selected"));

        IElement blatt = cut.Find("#bk-blatt-UEBERSICHT");
        Assert.Equal("tabpanel", blatt.GetAttribute("role"));
        Assert.Equal("bk-reiter-UEBERSICHT", blatt.GetAttribute("aria-labelledby"));

        // Die dunkle Seitennavigation gibt es nicht mehr.
        Assert.DoesNotContain("epos-navigation", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Startseite trägt selbst einen <c>Reiter</c> (und die Simulationsseite einen mit
    /// „UEBERSICHT"): Die Knöpfe und Blätter dieses Rahmens tragen den Vorsatz „bk", damit
    /// keine HTML-Kennung doppelt steht.
    /// </summary>
    [Fact]
    public void Die_Kennungen_tragen_den_Vorsatz_bk()
    {
        var cut = Zeige();

        Assert.Equal(new[] { "bk-reiter-UEBERSICHT", "bk-reiter-KOSTEN", "bk-reiter-WIRTSCHAFT", "bk-reiter-BERICHT" },
                     Navknoepfe(cut).Select(k => k.Id).ToArray());
        Assert.Equal("bk-blatt-KOSTEN", Navknoepfe(cut)[1].GetAttribute("aria-controls"));
        Assert.Empty(cut.FindAll("#reiter-UEBERSICHT"));
        Assert.Empty(cut.FindAll("#blatt-UEBERSICHT"));
    }

    [Fact]
    public void Beim_Aufbau_steht_die_Uebersicht_vorn()
    {
        var cut = Zeige();

        Assert.Equal(BerichteKostenSeite.SEITE_UEBERSICHT, cut.Instance.AktiveSeite);
        Assert.Equal(new[] { BerichteKostenSeite.SEITE_UEBERSICHT }, _gefragt);
    }

    [Fact]
    public void Die_Startseite_laesst_sich_vorgeben()
    {
        var cut = Zeige(p => p.Add(x => x.Startseite, BerichteKostenSeite.SEITE_BERICHT));

        Assert.Equal(BerichteKostenSeite.SEITE_BERICHT, cut.Instance.AktiveSeite);
    }

    [Fact]
    public void Ein_unbekannter_Schluessel_faellt_auf_die_Uebersicht_zurueck()
    {
        var cut = Zeige(p => p.Add(x => x.Startseite, "GIBTESNICHT"));

        Assert.Equal(BerichteKostenSeite.SEITE_UEBERSICHT, cut.Instance.AktiveSeite);
    }

    /// <summary>
    /// BN-Q3: Stammname, Platzhalter-Umschalter und Hilfe stehen RECHTS in der Reiterzeile —
    /// neben der <c>tablist</c>, nicht in ihr; eine eigene Kopfzeile gibt es nicht.
    /// </summary>
    [Fact]
    public void Das_Leistenende_traegt_Stammnamen_Umschalter_und_Hilfe()
    {
        var cut = Zeige(p => p.Add(x => x.StammBeschriftung, "Stamm:"));

        IElement ende = Leistenende(cut);
        IElement stamm = ende.QuerySelector(".epos-berichtekosten-stamm")!;
        Assert.Equal("Stamm:", stamm.QuerySelector(".epos-berichtekosten-stamm-beschriftung")!.TextContent);
        Assert.Equal("Musterhaus", stamm.QuerySelector(".epos-berichtekosten-stamm-name")!.TextContent);
        Assert.Equal("Stamm: Musterhaus", stamm.GetAttribute("title"));
        Assert.NotNull(ende.QuerySelector(".epos-vorlagenfeld-umschalter"));
        Assert.NotNull(ende.QuerySelector(".epos-hilfepille"));

        // Nicht in der tablist: die trägt nur die vier Reiter.
        Assert.Null(Leiste(cut).QuerySelector(".epos-vorlagenfeld-umschalter"));
        Assert.Equal(4, Leiste(cut).Children.Length);
    }

    [Fact]
    public void Ohne_Stammnamen_steht_im_Leistenende_keiner()
    {
        var cut = Zeige(stamm: () => "");

        Assert.Empty(cut.FindAll(".epos-berichtekosten-stamm"));
        Assert.NotNull(Leistenende(cut).QuerySelector(".epos-vorlagenfeld-umschalter"));
    }

    // =====================================================================
    // Umschalten — eine Seite zur Zeit
    // =====================================================================

    [Fact]
    public void Der_Klick_stellt_die_Seite_um_und_holt_ihre_Gaben()
    {
        var cut = Zeige();

        Navknoepfe(cut)[1].Click();

        Assert.Equal(BerichteKostenSeite.SEITE_KOSTEN, cut.Instance.AktiveSeite);
        Assert.Contains(BerichteKostenSeite.SEITE_KOSTEN, _gefragt);
        Assert.Contains("Projekt: Musterhaus", cut.Markup);
    }

    /// <summary>
    /// Die schweren Seiten entstehen erst beim ersten Aufruf — ein nicht
    /// gewählter Zweig wird gar nicht gezeichnet.
    /// </summary>
    [Fact]
    public void Es_ist_immer_nur_eine_Seite_gezeichnet()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-kennzahlkachel"));   // Kosten: drei Karten

        Navknoepfe(cut)[1].Click();
        Assert.Equal(3, cut.FindAll(".epos-kennzahlkachel").Count);

        Navknoepfe(cut)[3].Click();
        Assert.Empty(cut.FindAll(".epos-kennzahlkachel"));
    }

    /// <summary>Wie jeder Reiter des Hauses: ← → wandern, Pos1 und Ende springen, und die Gaben folgen.</summary>
    [Fact]
    public void Pfeil_rechts_und_links_wandern_durch_die_Reiter()
    {
        var cut = Zeige();

        Leiste(cut).KeyDown("ArrowRight");
        Assert.Equal(BerichteKostenSeite.SEITE_KOSTEN, cut.Instance.AktiveSeite);
        Assert.Contains(BerichteKostenSeite.SEITE_KOSTEN, _gefragt);

        Leiste(cut).KeyDown("ArrowLeft");
        Assert.Equal(BerichteKostenSeite.SEITE_UEBERSICHT, cut.Instance.AktiveSeite);

        Leiste(cut).KeyDown("End");
        Assert.Equal(BerichteKostenSeite.SEITE_BERICHT, cut.Instance.AktiveSeite);

        Leiste(cut).KeyDown("Home");
        Assert.Equal(BerichteKostenSeite.SEITE_UEBERSICHT, cut.Instance.AktiveSeite);
    }

    [Fact]
    public void Nur_der_aktive_Knopf_steht_im_Tabulatorzyklus()
    {
        var cut = Zeige();

        Assert.Equal("0", Navknoepfe(cut)[0].GetAttribute("tabindex"));
        Assert.Equal("-1", Navknoepfe(cut)[1].GetAttribute("tabindex"));
    }

    // =====================================================================
    // Ohne Stammprojekt
    // =====================================================================

    [Fact]
    public void Ohne_Stammprojekt_steht_der_Hinweis_statt_der_Seite()
    {
        var cut = Zeige(p => p.Add(x => x.KeinStammText, "Bitte zuerst ein Stammprojekt wählen."),
                        mitStamm: false);

        Navknoepfe(cut)[2].Click();   // Wirtschaftlichkeit

        Assert.Contains("Stammprojekt wählen", cut.Find(".epos-warnbanner").TextContent);
    }

    // =====================================================================
    // Projektwechsel über den Zustand
    // =====================================================================

    [Fact]
    public void Ein_Projektwechsel_holt_die_Gaben_neu()
    {
        var zustand = new SeitenZustand();
        var cut = Zeige(p => p.Add(x => x.Zustand, zustand));

        Assert.Single(_gefragt);

        zustand.ProjektSetzen(1031, "Variante");

        Assert.Equal(2, _gefragt.Count);
        Assert.Equal(BerichteKostenSeite.SEITE_UEBERSICHT, cut.Instance.AktiveSeite);
    }

    [Fact]
    public void Ein_Seitenwunsch_der_Huelle_stellt_die_Seite_um()
    {
        var zustand = new SeitenZustand();
        string wunsch = "";
        var cut = Zeige(p => p
            .Add(x => x.Zustand, zustand)
            .Add(x => x.Seitenwunsch, () => { string w = wunsch; wunsch = ""; return w; }));

        wunsch = BerichteKostenSeite.SEITE_BERICHT;
        zustand.Auffrischen();

        Assert.Equal(BerichteKostenSeite.SEITE_BERICHT, cut.Instance.AktiveSeite);

        // Der Wunsch gilt genau einmal.
        zustand.Auffrischen();
        Assert.Equal(BerichteKostenSeite.SEITE_BERICHT, cut.Instance.AktiveSeite);
    }

    [Fact]
    public void Nach_dem_Entsorgen_meldet_der_Zustand_nicht_mehr()
    {
        var zustand = new SeitenZustand();
        var cut = Zeige(p => p.Add(x => x.Zustand, zustand));

        int vorher = _gefragt.Count;
        cut.Instance.Dispose();
        zustand.ProjektSetzen(1040, "Anderes");

        Assert.Equal(vorher, _gefragt.Count);
    }

    // =====================================================================
    // Der Rueckweg (Anwenderentscheid W16c-E-3, 04.09.2026)
    // =====================================================================

    [Fact]
    public void Ohne_Rueckruf_gibt_es_keinen_Rueckwegknopf()
    {
        // Als sechstes REITERBLATT der Startseite hat die Seite kein "Zurueck":
        // Dort fuehrt die Reiterleiste hinaus, und ein zweiter Weg waere ein
        // Knopf, der nichts tut.
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-berichtekosten-zurueck"));
    }

    [Fact]
    public void Mit_Rueckruf_steht_der_Rueckwegknopf_und_meldet()
    {
        // Als eigene ANSICHT der AppWurzel (Menuepunkt "Varianten und
        // Bericht…") ist er der einzige Weg zurueck zur Startansicht.
        int gemeldet = 0;
        var cut = Zeige(p => p
            .Add(x => x.ZurueckText, "◀ Zurück")
            .Add(x => x.Geschlossen, () => gemeldet++));

        var knopf = cut.Find(".epos-berichtekosten-zurueck");
        Assert.Equal("◀ Zurück", knopf.TextContent.Trim());

        // Er steht im Ende der Reiterzeile, rechts außen wie das „← zurück" der Simulation.
        Assert.Contains("epos-berichtekosten-zurueck", Leistenende(cut).Children.Last().ClassName,
                        StringComparison.Ordinal);

        knopf.Click();

        Assert.Equal(1, gemeldet);
    }

    // =====================================================================
    //  Stamm → Variante → Stamm auf DERSELBEN Seiteninstanz
    //  (Anwenderbefund 16.09.2026)
    // =====================================================================

    /// <summary>
    /// Der Weg des Anwenders, gefahren über die echte
    /// <see cref="Berichtsgruppe"/> — so, wie die Hülle der Schale ihn fährt:
    /// Projektkontext setzen, danach die Gaben der Kostenseite aus dem Stand
    /// bauen. Vorher stand nach dem Rückwechsel auf das Stammprojekt
    /// „Kein Projekt gewählt." da.
    /// </summary>
    private sealed class Kostenweg
    {
        internal readonly Berichtsgruppe Stand = new();

        internal Kostenweg(int idStamm, string stammName)
            => Stand.StammSetzen(idStamm, stammName);

        /// <summary>Der Projektkontext hat gewechselt (Kopfzeile bzw. Variantenwahl).</summary>
        internal void Kontext(int idProjekt, string name, int idStammRef)
            => Stand.KontextGewechselt(idProjekt, name, idStammRef);

        /// <summary>Der Parametersatz der Kostenseite — die eine Entscheidung der Hülle.</summary>
        internal IReadOnlyDictionary<string, object> Gaben() => new Dictionary<string, object>
        {
            ["Laden"] = new Func<KostenStand>(() => Stand.KostenId <= 0
                ? new KostenStand { Projektzeile = "Kein Projekt gewählt.", Bedienbar = false }
                : new KostenStand { Projektzeile = "Projekt: " + Stand.KostenName, Bedienbar = true })
        };
    }

    [Fact]
    public void Der_Rueckwechsel_auf_das_Stammprojekt_zeigt_wieder_dessen_Kosten()
    {
        const int stamm = 1030, variante = 1047;
        const string stammName = "Booster-Kette mit Kombi-Speicher";
        const string variantenName = "Booster-Kette mit Kombi-Speicher - Schichtspeicher";

        var weg = new Kostenweg(stamm, stammName);
        var zustand = new SeitenZustand();

        weg.Kontext(stamm, stammName, 0);

        var cut = Render<BerichteKostenSeite>(p => p
            .Add(x => x.Zustand, zustand)
            .Add(x => x.Startseite, BerichteKostenSeite.SEITE_KOSTEN)
            .Add(x => x.SeitenGaben, (string s) => weg.Gaben()));

        Assert.Equal("Projekt: " + stammName, cut.Find(".epos-seite-titel").TextContent.Trim());

        // Die Variante wird zum aktiven Projekt.
        weg.Kontext(variante, variantenName, stamm);
        zustand.ProjektSetzen(variante, variantenName);

        Assert.Equal("Projekt: " + variantenName, cut.Find(".epos-seite-titel").TextContent.Trim());

        // Und zurueck auf das Stammprojekt - OHNE den Bereich zu verlassen.
        weg.Kontext(stamm, stammName, 0);
        zustand.ProjektSetzen(stamm, stammName);

        Assert.Equal("Projekt: " + stammName, cut.Find(".epos-seite-titel").TextContent.Trim());
        Assert.DoesNotContain("Kein Projekt", cut.Markup);
        Assert.Empty(cut.FindAll(".epos-knopf[disabled]"));
    }

    // =====================================================================
    // „Zum Bericht ›" — Bereichswechsel mit Vorbelegung
    // =====================================================================

    private const string WIRTSCHAFT = "WIRTSCHAFT";

    /// <summary>Gaben mit Ergebnissen: Vergleichsgruppe 1030 (Stamm), 1031, 1032, angehakt 1030 und 1032.</summary>
    private static IReadOnlyDictionary<string, object>? GabenMitGruppe(string seite) => seite switch
    {
        BerichteKostenSeite.SEITE_WIRTSCHAFT => new Dictionary<string, object>
        {
            ["Laden"] = new Func<WirtschaftlichkeitStand>(() => new WirtschaftlichkeitStand
            {
                Varianten = Gruppe(),
                GewaehlteVarianten = new[] { 1030, 1032 },
                Szenarien = new[] { (0, "Erwartet"), (1, "Günstig"), (2, "Ungünstig") },
                SzenarioId = 1,
                HatErgebnisse = true
            })
        },
        BerichteKostenSeite.SEITE_BERICHT => new Dictionary<string, object>
        {
            ["Laden"] = new Func<BerichtStand>(() => new BerichtStand
            {
                Varianten = Gruppe(),
                GewaehlteVarianten = new[] { 1030, 1031, 1032 },
                Bausteine = new[]
                {
                    new BausteinZeile { Schluessel = "KOPF", Titel = "Projektkopf" },
                    new BausteinZeile { Schluessel = WIRTSCHAFT, Titel = "Wirtschaftlichkeit" }
                },
                AktiveBausteine = new[] { "KOPF" }
            }),
            ["BausteinWirtschaft"] = WIRTSCHAFT
        },
        _ => new Dictionary<string, object>
        {
            ["Laden"] = new Func<UebersichtStand>(() => new UebersichtStand())
        }
    };

    private static VarianteZeile[] Gruppe() => new[]
    {
        new VarianteZeile { IdProjekt = 1030, Art = "Stamm", Bezeichner = "(Stammprojekt)", Projektname = "Musterhaus", IstStamm = true },
        new VarianteZeile { IdProjekt = 1031, Art = "Variante", Bezeichner = "Kessel groß", Projektname = "Musterhaus" },
        new VarianteZeile { IdProjekt = 1032, Art = "Variante", Bezeichner = "WP klein", Projektname = "Musterhaus" }
    };

    /// <summary>
    /// „Zum Bericht ›" auf der Wirtschaftlichkeitsseite wechselt in den Bereich „Bericht"
    /// DERSELBEN Ansicht — kein Fenster, kein Lauf —, und die Berichtsseite zeigt den
    /// Baustein Wirtschaftlichkeit angehakt, die Versionen der Vergleichsgruppe und das
    /// Szenario. Die Vorbelegung wird dabei verbraucht: Ein späterer Wechsel zurück zeigt
    /// wieder den gespeicherten Stand.
    /// </summary>
    [Fact]
    public void Zum_Bericht_wechselt_den_Bereich_und_belegt_die_Berichtsseite_vor()
    {
        var cut = Render<BerichteKostenSeite>(p => p
            .Add(x => x.SeitenGaben, (string s) => GabenMitGruppe(s))
            .Add(x => x.Startseite, BerichteKostenSeite.SEITE_WIRTSCHAFT));

        cut.Find(".epos-wirt-berichtknopf").Click();

        Assert.Equal(BerichteKostenSeite.SEITE_BERICHT, cut.Instance.AktiveSeite);
        Assert.Empty(cut.FindComponents<WirtschaftlichkeitSeite>());
        Assert.Null(cut.Instance.WartendeVorbelegung);

        BerichtSeite bericht = cut.FindComponent<BerichtSeite>().Instance;
        Assert.Equal(new[] { 1030, 1032 }, bericht.Gewaehlte.OrderBy(i => i).ToArray());
        Assert.Contains(WIRTSCHAFT, bericht.AktiveBausteine);
        Assert.Contains("KOPF", bericht.AktiveBausteine);
        Assert.Contains("Günstig", cut.Find(".epos-bericht-vorbelegt").TextContent);

        // Einmal verbraucht: zurück und wieder hin zeigt den gespeicherten Stand.
        Navknoepfe(cut)[2].Click();
        Navknoepfe(cut)[3].Click();
        BerichtSeite wieder = cut.FindComponent<BerichtSeite>().Instance;
        Assert.Equal(new[] { 1030, 1031, 1032 }, wieder.Gewaehlte.OrderBy(i => i).ToArray());
        Assert.DoesNotContain(WIRTSCHAFT, wieder.AktiveBausteine);
        Assert.Empty(cut.FindAll(".epos-bericht-vorbelegt"));
    }

    /// <summary>
    /// Ohne Ergebnisse ist der Knopf weich gesperrt: Ein Klick wechselt nicht, und nichts
    /// wartet auf die Berichtsseite.
    /// </summary>
    [Fact]
    public void Zum_Bericht_ohne_Ergebnisse_wechselt_nicht()
    {
        var cut = Render<BerichteKostenSeite>(p => p
            .Add(x => x.SeitenGaben, (string s) => s == BerichteKostenSeite.SEITE_WIRTSCHAFT
                ? new Dictionary<string, object>
                {
                    ["Laden"] = new Func<WirtschaftlichkeitStand>(() => new WirtschaftlichkeitStand { Varianten = Gruppe() })
                }
                : GabenMitGruppe(s))
            .Add(x => x.Startseite, BerichteKostenSeite.SEITE_WIRTSCHAFT));

        IElement knopf = cut.Find(".epos-wirt-berichtknopf");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        knopf.Click();

        Assert.Equal(BerichteKostenSeite.SEITE_WIRTSCHAFT, cut.Instance.AktiveSeite);
        Assert.Null(cut.Instance.WartendeVorbelegung);
    }

    // =====================================================================
    //  Statuszeile je Reiter (Konzept Navigation Berichte & Kosten, A2/A3)
    // =====================================================================

    /// <summary>Kurzstände wie in Mockup A: leise, Warnung mit Kurzform, bester Kapitalwert, zuletzt erstellt.</summary>
    private static Reiterstatus? Kurzstaende(string seite) => seite switch
    {
        BerichteKostenSeite.SEITE_UEBERSICHT => new Reiterstatus("3 Versionen · simuliert", "3 Versionen"),
        BerichteKostenSeite.SEITE_KOSTEN => new Reiterstatus("3 Energieträger · 2 Warnungen", "2", Statusstufe.Warnung),
        BerichteKostenSeite.SEITE_WIRTSCHAFT => new Reiterstatus("beste: mit PV, +61.500 €", "+61.500 €"),
        BerichteKostenSeite.SEITE_BERICHT => new Reiterstatus("zuletzt 26.09.2026 18:12", "26.09. 18:12"),
        _ => null
    };

    [Fact]
    public void Jeder_Reiter_traegt_seinen_Kurzstand_als_Statuszeile()
    {
        var cut = Zeige(p => p.Add(x => x.Status, (string s) => Kurzstaende(s)));

        var knoepfe = Navknoepfe(cut);
        Assert.All(knoepfe, k => Assert.Contains("epos-reiter-knopf--status", k.ClassName, StringComparison.Ordinal));

        Assert.Equal("Übersicht", knoepfe[0].QuerySelector(".epos-reiter-titel")!.TextContent.Trim());
        Assert.Equal("3 Versionen · simuliert", knoepfe[0].QuerySelector(".epos-reiter-status-lang")!.TextContent.Trim());
        Assert.Equal("3 Versionen", knoepfe[0].QuerySelector(".epos-reiter-status-kurz")!.TextContent.Trim());
        Assert.Contains("+61.500 €", knoepfe[2].QuerySelector(".epos-reiter-status")!.TextContent, StringComparison.Ordinal);
        Assert.Equal("26.09. 18:12", knoepfe[3].QuerySelector(".epos-reiter-status-kurz")!.TextContent.Trim());
    }

    /// <summary>Eine Warnung trägt Warnklasse UND Zeichen — das Zeichen nur für das Auge.</summary>
    [Fact]
    public void Die_Warnung_der_Kosten_traegt_das_Warnzeichen()
    {
        var cut = Zeige(p => p.Add(x => x.Status, (string s) => Kurzstaende(s)));

        IElement kosten = Navknoepfe(cut)[1];
        IElement zeile = kosten.QuerySelector(".epos-reiter-status")!;
        Assert.Contains("epos-reiter-status--warnung", zeile.ClassName, StringComparison.Ordinal);
        IElement zeichen = zeile.QuerySelector(".epos-reiter-warnzeichen")!;
        Assert.Equal("▲", zeichen.TextContent);
        Assert.Equal("true", zeichen.GetAttribute("aria-hidden"));
        Assert.Equal("2", zeile.QuerySelector(".epos-reiter-status-kurz")!.TextContent.Trim());

        // Die leisen Reiter tragen kein Zeichen.
        Assert.Null(Navknoepfe(cut)[0].QuerySelector(".epos-reiter-warnzeichen"));
        Assert.DoesNotContain("--warnung", Navknoepfe(cut)[0].QuerySelector(".epos-reiter-status")!.ClassName,
                              StringComparison.Ordinal);
    }

    [Fact]
    public void Ohne_Kurzstand_traegt_der_Reiter_nur_seinen_Titel()
    {
        var cut = Zeige(p => p.Add(x => x.Status,
            (string s) => s == BerichteKostenSeite.SEITE_BERICHT ? new Reiterstatus("zuletzt 26.09.2026 18:12") : null));

        Assert.Empty(Navknoepfe(cut)[0].QuerySelectorAll(".epos-reiter-status"));
        Assert.Equal("Übersicht", Navknoepfe(cut)[0].TextContent.Trim());

        // Ohne eigene Kurzform steht EIN Text — keine lange und kurze Fassung nebeneinander.
        IElement bericht = Navknoepfe(cut)[3];
        Assert.Single(bericht.QuerySelectorAll(".epos-reiter-status-text"));
        Assert.Empty(bericht.QuerySelectorAll(".epos-reiter-status-kurz"));
    }

    /// <summary>Ein Fehler der Hülle kostet nur die Zeile, nie die Seite.</summary>
    [Fact]
    public void Ein_Fehler_im_Kurzstand_kostet_nur_die_Zeile()
    {
        var cut = Zeige(p => p.Add(x => x.Status, (string s) => throw new InvalidOperationException("kaputt")));

        Assert.Equal(4, Navknoepfe(cut).Count);
        Assert.Empty(cut.FindAll(".epos-reiter-status"));
    }

    /// <summary>
    /// Ein geänderter Kurzstand (Seite geladen, Bericht erstellt) zeichnet die Reiterzeile neu —
    /// OHNE neue Gaben und ohne die offene Seite neu aufzubauen.
    /// </summary>
    [Fact]
    public void Ein_geaenderter_Kurzstand_zeichnet_nur_die_Reiterzeile_neu()
    {
        var zustand = new SeitenZustand();
        Reiterstatus? bericht = new Reiterstatus("noch keiner erstellt", "—");
        string stamm = "";
        var cut = Zeige(p => p
            .Add(x => x.Zustand, zustand)
            .Add(x => x.Status, (string s) => s == BerichteKostenSeite.SEITE_BERICHT ? bericht : null),
            stamm: () => stamm);

        UebersichtSeite vorher = cut.FindComponent<UebersichtSeite>().Instance;
        int gefragt = _gefragt.Count;

        bericht = new Reiterstatus("zuletzt 27.09.2026 14:05", "27.09. 14:05");
        stamm = "Musterhaus";
        zustand.KurzstandMelden();

        cut.WaitForAssertion(() =>
            Assert.Contains("zuletzt 27.09.2026 14:05", Navknoepfe(cut)[3].TextContent, StringComparison.Ordinal));
        Assert.Equal("Musterhaus", cut.Find(".epos-berichtekosten-stamm-name").TextContent);
        Assert.Contains("Musterhaus", Leistenende(cut).TextContent, StringComparison.Ordinal);
        Assert.Equal(gefragt, _gefragt.Count);
        Assert.Same(vorher, cut.FindComponent<UebersichtSeite>().Instance);
        Assert.Equal(BerichteKostenSeite.SEITE_UEBERSICHT, cut.Instance.AktiveSeite);
    }

    /// <summary>Nach dem Entsorgen hört die Seite auch auf den Kurzstand nicht mehr.</summary>
    [Fact]
    public void Nach_dem_Entsorgen_meldet_auch_der_Kurzstand_nicht_mehr()
    {
        var zustand = new SeitenZustand();
        int gefragt = 0;
        var cut = Zeige(p => p
            .Add(x => x.Zustand, zustand)
            .Add(x => x.Status, (string s) => { gefragt++; return null; }));

        cut.Instance.Dispose();
        int vorher = gefragt;
        zustand.KurzstandMelden();

        Assert.Equal(vorher, gefragt);
    }

    /// <summary>
    /// Der Menüsprung „Varianten und Bericht…" mit Ziel „Kosten" stellt den REITER um — dieselben
    /// Seitenschlüssel, derselbe Seitenwunsch der Hülle.
    /// </summary>
    [Fact]
    public void Der_Seitenwunsch_stellt_den_aktiven_Reiter_um()
    {
        var zustand = new SeitenZustand();
        string wunsch = "";
        var cut = Zeige(p => p
            .Add(x => x.Zustand, zustand)
            .Add(x => x.Seitenwunsch, () => { string w = wunsch; wunsch = ""; return w; }));

        wunsch = BerichteKostenSeite.SEITE_KOSTEN;
        zustand.Auffrischen();

        cut.WaitForAssertion(() => Assert.Equal("true", Navknoepfe(cut)[1].GetAttribute("aria-selected")));
        Assert.Equal("false", Navknoepfe(cut)[0].GetAttribute("aria-selected"));
        Assert.Equal("0", Navknoepfe(cut)[1].GetAttribute("tabindex"));
        Assert.NotNull(cut.Find("#bk-blatt-KOSTEN"));
        Assert.Empty(cut.FindAll("#bk-blatt-UEBERSICHT"));
    }
}
