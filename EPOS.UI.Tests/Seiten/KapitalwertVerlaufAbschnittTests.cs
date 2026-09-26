using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using EPOS.UI.Seiten.Simulation;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// ETAPPE E6 (Konzept Wirtschaftlichkeit § 2.13 (5), Mockup Kategorie 8, Anhangzeilen U3,
/// U13 und der Rest von U2) — <b>der Verlauf mit allen drei Szenarien als Abschnitt der
/// Wirtschaftlichkeitsseite</b> statt hinter dem Knopf „Verlauf…".
///
/// <para>Geprüft werden der Abschnitt selbst (<see cref="KapitalwertVerlaufAbschnitt"/>:
/// Platzhalter ohne Datenseite, Bild, Haken, Rechnen mit dem Zeitraum, Abbrechen, Fehler,
/// „Verlauf nach Excel…") und seine Stelle auf der Seite: in „Wie sicher ist das?" nach der
/// Bandbreite und vor der Sensitivität, die Fußleiste mit höchstens vier Knöpfen, kein
/// Verlaufsdialog mehr.</para>
///
/// <para><b>Kulturpinnung</b>: Die Beschriftungen kommen aus dem Bündel
/// <see cref="VerlaufTexte"/> und damit aus <c>MyResource</c>; die Hausvorrichtung
/// <see cref="EposBunitContext"/> pinnt de-DE.</para>
/// </summary>
public class KapitalwertVerlaufAbschnittTests : EposBunitContext
{
    public KapitalwertVerlaufAbschnittTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private const int STAMM = 1030;
    private const int WP = 1031;
    private const int BHKW = 1032;

    // =====================================================================
    //  Probendaten
    // =====================================================================

    /// <summary>Zwei Stände in allen drei Szenarien — gezeichnet vom Renderer des Kerns.</summary>
    private static Zeichenmodell Modell()
    {
        var texte = new ChartRenderer.VerlaufSzenarienTexte();
        var verlauf = new WirtschaftlichkeitVerlaufSzenarien { Jahre = 20 };
        foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
        {
            var lauf = new WirtschaftlichkeitVerlauf { Jahre = 20, Szenario = s };
            lauf.Absolut.Add(new VerlaufSerie { IdProjekt = STAMM, Anzeige = "Stamm", IstStamm = true,
                                                Kumuliert = new double[21] });
            foreach ((int id, string name, double invest) in new[] { (WP, "WP klein", 40000.0), (BHKW, "BHKW", 90000.0) })
            {
                var d = new double[21];
                d[0] = -invest;
                for (int t = 1; t <= 20; t++) d[t] = d[t - 1] + 9000.0 * Math.Pow(0.97, t);
                lauf.Absolut.Add(new VerlaufSerie { IdProjekt = id, Anzeige = name, Kumuliert = d });
                lauf.Differenz.Add(new VerlaufSerie { IdProjekt = id, Anzeige = name, Kumuliert = d });
            }
            verlauf.Laeufe[s] = lauf;
        }
        return ChartRenderer.KapitalwertSzenarienModell("Verlauf",
            ChartRenderer.VerlaufsReihenSzenarien(verlauf, texte), texte, null);
    }

    private static VerlaufAnsicht Ansicht(IReadOnlyList<int>? staende = null,
                                          IReadOnlyList<int>? szenarien = null,
                                          bool mitBild = true) => new VerlaufAnsicht
    {
        Modell = mitBild ? Modell() : null,
        Staende = new[] { (WP, "WP klein"), (BHKW, "BHKW") },
        GewaehlteStaende = staende ?? new[] { WP, BHKW },
        Szenarien = new[] { (0, "Ungünstig"), (1, "Erwartet"), (2, "Günstig") },
        GewaehlteSzenarien = szenarien ?? new[] { 0, 1, 2 },
        Jahre = 20,
        Nulldurchgangszeile = "Nulldurchgänge (dynamische Amortisation): WP klein: Ungünstig 5,10 a · Erwartet 4,70 a · Günstig 4,30 a",
        Restwertzeile = "Restwert-Barwerte am Horizontende (nicht in den Linien enthalten): WP klein: Erwartet 2.000 €",
        Statuszeile = "Verlauf über 20 Jahre, alle drei Szenarien."
    };

    private IRenderedComponent<KapitalwertVerlaufAbschnitt> Abschnitt(VerlaufDienste? dienste, int fassung = 0)
        => Render<KapitalwertVerlaufAbschnitt>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.Fassung, fassung));

    // =====================================================================
    //  Der Abschnitt
    // =====================================================================

    /// <summary>
    /// Ohne Datenseite steht der Abschnitt mit Kopf und Platzhalter da — ohne Knöpfe:
    /// kein Delegat, kein Knopf. Eine Seite, die aus einer Kachel aufgeht, muss auch ohne
    /// Gaben zeichnen.
    /// </summary>
    [Fact]
    public void Ohne_Datenseite_steht_der_Abschnitt_mit_Platzhalter()
    {
        var cut = Abschnitt(null);

        Assert.Contains("Der Verlauf über die Zeit — alle drei Szenarien", cut.Markup);
        Assert.Empty(cut.FindAll("button.epos-wirt-verlauf-rechnen"));
        Assert.Empty(cut.FindAll("button.epos-wirt-verlauf-excel"));
        Assert.Contains("Noch kein Diagramm", cut.Find(".epos-chartbild-platzhalter").TextContent);
        Assert.Empty(cut.FindAll("input.epos-schalter-kasten"));
    }

    /// <summary>
    /// Mit einer Ansicht stehen das Bild (als SVG-Baustein), je Stand und je Szenario ein
    /// Haken und die Zeilen darunter — die Nulldurchgänge und die Restwerte.
    /// </summary>
    [Fact]
    public void Mit_Ansicht_stehen_Bild_Haken_und_Zeilen()
    {
        var cut = Abschnitt(new VerlaufDienste { Zeichnen = _ => Ansicht() });

        Assert.Single(cut.FindAll(".epos-diagramm-svg"));
        Assert.Equal(5, cut.FindAll("input.epos-schalter-kasten").Count);   // zwei Stände, drei Szenarien
        Assert.All(cut.FindAll("input.epos-schalter-kasten"), k => Assert.True(k.HasAttribute("checked")));
        Assert.Contains("Nulldurchgänge (dynamische Amortisation)", cut.Markup);
        Assert.Contains("Restwert-Barwerte am Horizontende", cut.Markup);
        Assert.Equal(20, cut.Instance.Ansicht!.Jahre);
    }

    /// <summary>
    /// Ein Haken ZEICHNET nur neu — gerechnet ist schon: Die Wahl geht an den billigen Weg,
    /// der Rechenweg bleibt unberührt.
    /// </summary>
    [Fact]
    public void Ein_Haken_zeichnet_neu_und_rechnet_nicht()
    {
        var wahlen = new List<VerlaufWahl>();
        int gerechnet = 0;
        var cut = Abschnitt(new VerlaufDienste
        {
            Zeichnen = w => { wahlen.Add(w); return Ansicht(w.Staende?.ToList(), w.Szenarien?.ToList()); },
            Berechnen = (j, w, ct) => { gerechnet++; return Task.FromResult(Ansicht()); }
        });

        cut.FindAll("input.epos-schalter-kasten")[0].Change(false);          // WP klein ab

        Assert.Equal(2, wahlen.Count);
        Assert.Equal(new[] { BHKW }, wahlen[^1].Staende);
        Assert.Equal(new[] { 0, 1, 2 }, wahlen[^1].Szenarien);
        Assert.Equal(0, gerechnet);

        cut.FindAll("input.epos-schalter-kasten")[4].Change(false);          // Günstig ab
        Assert.Equal(new[] { 0, 1 }, wahlen[^1].Szenarien);
    }

    // =====================================================================
    //  Die Haken der Stände folgen der Vergleichsgruppe (Fehlermeldung 26.09.2026)
    // =====================================================================

    /// <summary>
    /// Eine Datenseite, die wie <c>KapitalwertVerlaufHuelle</c> filtert: Angeboten werden
    /// die Stände der Vergleichsgruppe mit Linie, gezeichnet die angebotenen, eingeschränkt
    /// auf die Wahl des Abschnitts. Ohne Rechnung kommt die leere Ansicht der Hülle zurück
    /// (keine Stände, keine gewählten Stände, alle drei Szenarien).
    /// </summary>
    private sealed class Huellenattrappe
    {
        public List<(int Id, string Text)> Gruppe { get; } = new();
        public bool Gerechnet { get; set; } = true;
        public List<VerlaufWahl> Excelwahlen { get; } = new();

        public VerlaufAnsicht Zeichnen(VerlaufWahl w)
        {
            var szenarien = new[] { (0, "Ungünstig"), (1, "Erwartet"), (2, "Günstig") };
            if (!Gerechnet)
                return new VerlaufAnsicht { Szenarien = szenarien, GewaehlteSzenarien = new[] { 0, 1, 2 } };
            List<int> gewaehlt = Gruppe.Select(s => s.Id)
                                       .Where(id => w.Staende is null || w.Staende.Contains(id)).ToList();
            return new VerlaufAnsicht
            {
                Modell = gewaehlt.Count > 0 ? Modell() : null,
                Staende = Gruppe.ToList(),
                GewaehlteStaende = gewaehlt,
                Szenarien = szenarien,
                GewaehlteSzenarien = w.Szenarien?.ToList() ?? new List<int> { 0, 1, 2 },
                Jahre = 20
            };
        }

        public VerlaufDienste Dienste() => new VerlaufDienste
        {
            Zeichnen = Zeichnen,
            Berechnen = (j, w, ct) => { Gerechnet = true; return Task.FromResult(Zeichnen(w)); },
            NachExcel = w => { Excelwahlen.Add(w); return Task.FromResult(Rueckmeldung.Still); }
        };
    }

    /// <summary>Die Haken der Zeile „Varianten:" — angehakt oder nicht, in der Reihenfolge der Zeile.</summary>
    private static bool[] Standhaken(IRenderedComponent<KapitalwertVerlaufAbschnitt> cut)
        => cut.FindAll(".epos-wirt-verlauf-wahl")[0].QuerySelectorAll("input.epos-schalter-kasten")
              .Select(k => k.HasAttribute("checked")).ToArray();

    /// <summary>
    /// Fällt eine angehakte Variante aus der Vergleichsgruppe, bleibt die verbliebene
    /// angehakt und das Bild zeichnet sie.
    /// </summary>
    [Fact]
    public void Faellt_eine_Variante_aus_der_Gruppe_bleibt_die_verbliebene_angehakt()
    {
        var huelle = new Huellenattrappe();
        huelle.Gruppe.AddRange(new[] { (WP, "mit PV"), (BHKW, "mit Stromspeicher") });
        VerlaufDienste dienste = huelle.Dienste();
        var cut = Abschnitt(dienste);
        Assert.Equal(new[] { true, true }, Standhaken(cut));

        huelle.Gruppe.RemoveAt(0);                                         // „mit PV" aus der Gruppe
        cut.Render(p => p.Add(x => x.Dienste, dienste).Add(x => x.Fassung, 1));

        Assert.Equal(new[] { true }, Standhaken(cut));
        Assert.Equal(new[] { BHKW }, cut.Instance.Ansicht!.GewaehlteStaende);
        Assert.NotNull(cut.Instance.Ansicht.Modell);
    }

    /// <summary>
    /// Der Fall der Fehlermeldung: Ein Stand, der erst NACH dem ersten Zeichnen eine Linie
    /// bekommt (neu in der Gruppe oder frisch simuliert), wird angehakt — bis zur Behebung
    /// blieb er aus, weil die alte Wahl ihn nicht kannte. Fällt danach die erste Variante aus
    /// der Gruppe, stand der Verlauf ohne einen einzigen Haken da („Keine berechenbaren Reihen").
    /// </summary>
    [Fact]
    public void Eine_neue_Variante_der_Gruppe_wird_angehakt_und_traegt_den_Verlauf_allein()
    {
        var huelle = new Huellenattrappe();
        huelle.Gruppe.Add((WP, "mit PV"));
        VerlaufDienste dienste = huelle.Dienste();
        var cut = Abschnitt(dienste);
        Assert.Equal(new[] { true }, Standhaken(cut));

        huelle.Gruppe.Add((BHKW, "mit Stromspeicher"));                   // neu in der Gruppe
        cut.Render(p => p.Add(x => x.Dienste, dienste).Add(x => x.Fassung, 1));
        Assert.Equal(new[] { true, true }, Standhaken(cut));

        huelle.Gruppe.RemoveAt(0);                                         // „mit PV" abgewählt
        cut.Render(p => p.Add(x => x.Dienste, dienste).Add(x => x.Fassung, 2));

        Assert.Equal(new[] { true }, Standhaken(cut));
        Assert.Equal(new[] { BHKW }, cut.Instance.Ansicht!.GewaehlteStaende);
        Assert.NotNull(cut.Instance.Ansicht.Modell);

        // „Verlauf nach Excel…" schreibt dieselbe Wahl, die das Bild zeigt.
        cut.Find("button.epos-wirt-verlauf-excel").Click();
        Assert.Equal(new[] { BHKW }, huelle.Excelwahlen.Single().Staende);
    }

    /// <summary>
    /// Ohne gerechneten Verlauf kennt die Ansicht keinen Stand. Nach „Aktualisieren" sind
    /// alle Stände angehakt — die leere Wahl der ersten Ansicht heißt nicht „keiner".
    /// </summary>
    [Fact]
    public void Nach_dem_ersten_Aktualisieren_sind_alle_Staende_angehakt()
    {
        var huelle = new Huellenattrappe { Gerechnet = false };
        huelle.Gruppe.AddRange(new[] { (WP, "mit PV"), (BHKW, "mit Stromspeicher") });
        var cut = Abschnitt(huelle.Dienste());
        Assert.Empty(cut.FindAll(".epos-wirt-verlauf-wahl"));

        cut.Find("button.epos-wirt-verlauf-rechnen").Click();

        cut.WaitForAssertion(() => Assert.Equal(new[] { true, true }, Standhaken(cut)));
        Assert.Equal(new[] { WP, BHKW }, cut.Instance.Ansicht!.GewaehlteStaende);
        Assert.NotNull(cut.Instance.Ansicht.Modell);
    }

    /// <summary>
    /// Was der Anwender im Verlauf abgehakt hat, bleibt abgehakt, solange der Stand in der
    /// Gruppe bleibt — der Abgleich hakt nur NEUE Stände an.
    /// </summary>
    [Fact]
    public void Ein_abgehakter_Stand_bleibt_abgehakt_wenn_die_Gruppe_waechst()
    {
        var huelle = new Huellenattrappe();
        huelle.Gruppe.AddRange(new[] { (WP, "mit PV"), (BHKW, "mit Stromspeicher") });
        VerlaufDienste dienste = huelle.Dienste();
        var cut = Abschnitt(dienste);

        cut.FindAll("input.epos-schalter-kasten")[0].Change(false);          // „mit PV" im Verlauf ab
        Assert.Equal(new[] { false, true }, Standhaken(cut));

        huelle.Gruppe.Add((1033, "mit Solarthermie"));
        cut.Render(p => p.Add(x => x.Dienste, dienste).Add(x => x.Fassung, 1));

        Assert.Equal(new[] { false, true, true }, Standhaken(cut));
    }

    /// <summary>
    /// „Aktualisieren" rechnet mit dem Zeitraum des Feldes und meldet die fertige Rechnung
    /// an die Seite.
    /// </summary>
    [Fact]
    public void Aktualisieren_rechnet_mit_dem_Zeitraum_des_Feldes()
    {
        int? jahre = null;
        VerlaufAnsicht? gemeldet = null;
        var dienste = new VerlaufDienste
        {
            Zeichnen = _ => Ansicht(),
            Berechnen = (j, w, ct) => { jahre = j; return Task.FromResult(Ansicht()); }
        };
        var cut = Render<KapitalwertVerlaufAbschnitt>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.Gerechnet, (VerlaufAnsicht a) => gemeldet = a));

        cut.Find(".epos-wirt-verlauf-leiste input").Input("25");
        cut.Find("button.epos-wirt-verlauf-rechnen").Click();

        cut.WaitForAssertion(() => Assert.Equal(25, jahre));
        cut.WaitForAssertion(() => Assert.NotNull(gemeldet));
        Assert.False(cut.Instance.Laeuft);
        Assert.Equal("Verlauf über 20 Jahre, alle drei Szenarien.", cut.Instance.Status);
    }

    /// <summary>
    /// Während der Rechnung heißt derselbe Knopf „Abbrechen" und bricht ab; der Abschnitt
    /// bleibt stehen und sagt es.
    /// </summary>
    [Fact]
    public void Waehrend_der_Rechnung_bricht_derselbe_Knopf_ab()
    {
        var tor = new TaskCompletionSource<VerlaufAnsicht>();
        var cut = Abschnitt(new VerlaufDienste
        {
            Zeichnen = _ => Ansicht(),
            Berechnen = (j, w, ct) =>
            {
                ct.Register(() => tor.TrySetCanceled(ct));
                return tor.Task;
            }
        });

        cut.Find("button.epos-wirt-verlauf-rechnen").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Laeuft));
        Assert.Equal("Abbrechen", cut.Find("button.epos-wirt-verlauf-rechnen").TextContent.Trim());

        cut.Find("button.epos-wirt-verlauf-rechnen").Click();

        cut.WaitForAssertion(() => Assert.False(cut.Instance.Laeuft));
        Assert.Equal("Vorgang abgebrochen.", cut.Instance.Status);
        Assert.Equal("Aktualisieren", cut.Find("button.epos-wirt-verlauf-rechnen").TextContent.Trim());
    }

    /// <summary>Ein Fehler der Rechnung steht als Band an Ort und Stelle — keine Meldebox.</summary>
    [Fact]
    public void Ein_Fehler_der_Rechnung_steht_als_Band()
    {
        var cut = Abschnitt(new VerlaufDienste
        {
            Zeichnen = _ => Ansicht(),
            Berechnen = (j, w, ct) => Task.FromException<VerlaufAnsicht>(new InvalidOperationException("kein Lauf"))
        });

        cut.Find("button.epos-wirt-verlauf-rechnen").Click();

        cut.WaitForAssertion(() => Assert.Contains("Fehler beim Berechnen des Verlaufs: kein Lauf", cut.Markup));
    }

    /// <summary>
    /// U13: „Verlauf nach Excel…" geht über den Dateiweg der Hülle (dort
    /// <c>Dienste.Datei</c>) mit der Wahl des Abschnitts; die Rückmeldung steht in der
    /// Statuszeile.
    /// </summary>
    [Fact]
    public void Verlauf_nach_Excel_ruft_den_Dateiweg_mit_der_Wahl()
    {
        VerlaufWahl? gefragt = null;
        var cut = Abschnitt(new VerlaufDienste
        {
            Zeichnen = _ => Ansicht(new[] { WP }, new[] { 1 }),
            NachExcel = w =>
            {
                gefragt = w;
                return Task.FromResult(new Rueckmeldung(true, "Verlauf nach Excel geschrieben: C:\\probe.xlsx"));
            }
        });

        IElement knopf = cut.Find("button.epos-wirt-verlauf-excel");
        Assert.Equal("Verlauf nach Excel…", knopf.TextContent.Trim());
        knopf.Click();

        cut.WaitForAssertion(() => Assert.NotNull(gefragt));
        Assert.Equal(new[] { WP }, gefragt!.Staende);
        Assert.Equal(new[] { 1 }, gefragt.Szenarien);
        cut.WaitForAssertion(() => Assert.Equal("Verlauf nach Excel geschrieben: C:\\probe.xlsx", cut.Instance.Status));
    }

    /// <summary>Scheitert das Schreiben, steht der Grund als Band.</summary>
    [Fact]
    public void Scheitert_das_Schreiben_steht_der_Grund_als_Band()
    {
        var cut = Abschnitt(new VerlaufDienste
        {
            Zeichnen = _ => Ansicht(),
            NachExcel = _ => Task.FromResult(new Rueckmeldung(false, "Der Verlauf konnte nicht nach Excel geschrieben werden: gesperrt"))
        });

        cut.Find("button.epos-wirt-verlauf-excel").Click();

        cut.WaitForAssertion(() => Assert.Contains("gesperrt", cut.Find(".epos-warnbanner, [role=alert]").TextContent));
    }

    /// <summary>Ohne Bild gibt es nichts zu schreiben — der Knopf ist gesperrt.</summary>
    [Fact]
    public void Ohne_Bild_ist_Verlauf_nach_Excel_gesperrt()
    {
        var cut = Abschnitt(new VerlaufDienste
        {
            Zeichnen = _ => Ansicht(mitBild: false),
            NachExcel = _ => Task.FromResult(Rueckmeldung.Still)
        });

        Assert.True(cut.Find("button.epos-wirt-verlauf-excel").HasAttribute("disabled"));
    }

    /// <summary>
    /// Eine neue FASSUNG des Seitenstandes holt die Ansicht neu (Hausregel
    /// „Fassungsnummer statt Kopie") — dieselbe Fassung nicht.
    /// </summary>
    [Fact]
    public void Eine_neue_Fassung_holt_die_Ansicht_neu()
    {
        int gezeichnet = 0;
        var dienste = new VerlaufDienste { Zeichnen = _ => { gezeichnet++; return Ansicht(); } };
        var cut = Abschnitt(dienste, 1);
        Assert.Equal(1, gezeichnet);

        cut.Render(p => p.Add(x => x.Dienste, dienste).Add(x => x.Fassung, 1));
        Assert.Equal(1, gezeichnet);

        cut.Render(p => p.Add(x => x.Dienste, dienste).Add(x => x.Fassung, 2));
        Assert.Equal(2, gezeichnet);
    }

    // =====================================================================
    //  Die Stelle auf der Seite
    // =====================================================================

    /// <summary>
    /// Das Markup ohne die Ereigniskennungen des Zeichenlaufs (<c>blazor:onpointerenter="6"</c>):
    /// bunit zählt sie je Instanz fort, das Bild selbst tragen sie nicht.
    /// </summary>
    private static string OhneKennungen(string markup)
        => System.Text.RegularExpressions.Regex.Replace(markup, @"\s+blazor:[A-Za-z:]+=""[^""]*""", "");

    private static WirtschaftlichkeitStand Stand() => new WirtschaftlichkeitStand
    {
        Varianten = new[]
        {
            new VarianteZeile { IdProjekt = STAMM, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                                Projektname = "Musterhaus", IstStamm = true },
            new VarianteZeile { IdProjekt = WP, Art = "Variante", Bezeichner = "WP klein", Projektname = "Musterhaus" }
        },
        GewaehlteVarianten = new[] { STAMM, WP },
        Szenarien = new[] { (0, "Erwartet"), (1, "Günstig"), (2, "Ungünstig") },
        MitPhotovoltaik = true,
        MitBhkw = true,
        Ansicht = new ErgebnisAnsicht
        {
            Bandbreite = new ErgebnisMatrix
            {
                Spalten = new[] { "Variante", "Ungünstig", "Erwartet", "Günstig", "Spanne [€]", "Einstufung" },
                Zeilen = new[]
                {
                    new MatrixZeile { Titel = "Stamm", Zellen = new[] { "(Referenz)", "(Referenz)", "(Referenz)", "—", "—" } },
                    new MatrixZeile { Titel = "WP klein", Zellen = new[] { "10.100", "12.300", "14.600", "4.500", "empfohlen" } }
                }
            },
            Sensitivitaet = new ErgebnisMatrix
            {
                Spalten = new[] { "Variante", "Einflussgröße", "bei −Δ [€]", "Basis [€]", "bei +Δ [€]", "Steigung" },
                Zeilen = new[]
                {
                    new MatrixZeile { Titel = "WP klein",
                                      Zellen = new[] { "Zinssatz ±1 %-Pkt", "14.000", "12.300", "10.700", "−1.650,00 €/%-Pkt." } }
                }
            }
        },
        Statuszeile = "Gespeicherte Ergebnisse vom 22.09.2026 20:00."
    };

    /// <summary>
    /// U3 und der Mockup: Der Verlauf steht in „Wie sicher ist das?" NACH der Bandbreite
    /// und VOR der Sensitivität — die Bandbreite zeigt die Spanne am Ende, der Verlauf, wie
    /// sie entsteht.
    /// </summary>
    [Fact]
    public void Der_Verlauf_steht_zwischen_Bandbreite_und_Sensitivitaet()
    {
        var cut = Render<WirtschaftlichkeitSeite>(p => p
            .Add(x => x.Laden, () => Stand())
            .Add(x => x.Verlauf, new VerlaufDienste { Zeichnen = _ => Ansicht() }));

        IElement sicher = cut.FindAll("section.epos-gruppenkopf")[1];
        Assert.Contains("Wie sicher ist das?", sicher.TextContent);

        string[] teile = sicher.QuerySelectorAll(
                ".epos-wirt-bandbreite-teil, .epos-wirt-verlauf-teil, .epos-wirt-sensitivitaet-teil")
            .Select(e => e.ClassName ?? "").ToArray();
        Assert.Equal(3, teile.Length);
        Assert.Contains("epos-wirt-bandbreite-teil", teile[0]);
        Assert.Contains("epos-wirt-verlauf-teil", teile[1]);
        Assert.Contains("epos-wirt-sensitivitaet-teil", teile[2]);

        Assert.Single(sicher.QuerySelectorAll(".epos-diagramm-svg"));
    }

    /// <summary>
    /// ETAPPE E6, Nachtrag E5b (Anwenderentscheid 22.09.2026 zu Frage (4)): Das
    /// SPANNENBILD steht in „Wie sicher ist das?" unter der Bandbreitentafel und vor dem
    /// Verlauf — die Bandbreite als Balken, die Referenz als Nulllinie. Ohne Bild an der
    /// Ansicht (keine Bandbreite) steht dort kein Diagramm.
    /// </summary>
    [Fact]
    public void Das_Spannenbild_steht_unter_der_Bandbreite_vor_dem_Verlauf()
    {
        WirtschaftlichkeitStand stand = Stand();
        stand.Ansicht.Spannenbild = ChartRenderer.KapitalwertSpanneModell(
            new List<ChartRenderer.Spannenbalken>
            {
                new ChartRenderer.Spannenbalken { Name = "WP klein", Worst = 10100.0, Erwartet = 12300.0, Best = 14600.0 }
            }, "Stamm", null);

        var cut = Render<WirtschaftlichkeitSeite>(p => p
            .Add(x => x.Laden, () => stand)
            .Add(x => x.Verlauf, new VerlaufDienste { Zeichnen = _ => Ansicht() }));

        IElement sicher = cut.FindAll("section.epos-gruppenkopf")[1];
        string[] teile = sicher.QuerySelectorAll(
                ".epos-wirt-bandbreite-teil, .epos-wirt-spanne-teil, .epos-wirt-verlauf-teil")
            .Select(e => e.ClassName ?? "").ToArray();
        Assert.Equal(3, teile.Length);
        Assert.Contains("epos-wirt-bandbreite-teil", teile[0]);
        Assert.Contains("epos-wirt-spanne-teil", teile[1]);
        Assert.Contains("epos-wirt-verlauf-teil", teile[2]);

        IElement spanne = sicher.QuerySelector(".epos-wirt-spanne-teil")!;
        Assert.Single(spanne.QuerySelectorAll(".epos-diagramm-svg"));
        Assert.NotNull(spanne.QuerySelector("[data-marke='nulllinie']"));
        Assert.NotNull(spanne.QuerySelector("[data-marke='reihe:WP klein']"));

        // GEGENPROBE: ohne Bild an der Ansicht kein Diagramm in diesem Teil.
        var ohne = Render<WirtschaftlichkeitSeite>(p => p.Add(x => x.Laden, () => Stand()));
        IElement sicherOhne = ohne.FindAll("section.epos-gruppenkopf")[1];
        Assert.Empty(sicherOhne.QuerySelector(".epos-wirt-spanne-teil")!.QuerySelectorAll(".epos-diagramm-svg"));
    }

    /// <summary>
    /// ETAPPE E8a (Frage E6‑Q1, Anwender 23.09.2026: „ja — dieselben Bausteine wie unter
    /// ‚Wie sicher ist das?'"; Mockup-Anhang U49): <b>Block 4 „Unsicherheit" der
    /// ValERI-Ansicht zeigt Spannenbild und Verlauf</b> — das Spannenbild unter der
    /// Bandbreite, der Verlauf vor der Sensitivität, wie in „Wie sicher ist das?".
    ///
    /// <para><b>Dieselben Bausteine, dieselben Daten:</b> Der Verlauf ist die eine
    /// Komponente mit DERSELBEN Datenseite (ihr Zeichenweg wird gerufen), das Spannenbild
    /// dasselbe Fragment mit dem Modell der Ansicht — sein Markup gleicht Zeichen für
    /// Zeichen dem der Darstellung „Kennzahlen". Es steht immer nur eine Darstellung auf
    /// der Seite, also genau ein Verlauf.</para>
    /// </summary>
    [Fact]
    public void Block_4_zeigt_Spannenbild_und_Verlauf_mit_denselben_Bausteinen()
    {
        WirtschaftlichkeitStand stand = Stand();
        stand.Ansicht.Spannenbild = ChartRenderer.KapitalwertSpanneModell(
            new List<ChartRenderer.Spannenbalken>
            {
                new ChartRenderer.Spannenbalken { Name = "WP klein", Worst = 10100.0, Erwartet = 12300.0, Best = 14600.0 }
            }, "Stamm", null);
        int gezeichnet = 0;
        var dienste = new VerlaufDienste { Zeichnen = _ => { gezeichnet++; return Ansicht(); } };

        var cut = Render<WirtschaftlichkeitSeite>(p => p
            .Add(x => x.Laden, () => stand)
            .Add(x => x.Verlauf, dienste));
        // Verglichen wird das gezeichnete SVG ohne die Ereigniskennungen des Zeichenlaufs:
        // Rahmen und Datenelemente tragen je Instanz eigene Kennungen, die bunit fortzählt.
        string spanneKennzahlen = OhneKennungen(cut.FindAll("section.epos-gruppenkopf")[1]
                                     .QuerySelector(".epos-wirt-spanne-teil svg")!.OuterHtml);

        int vorher = gezeichnet;
        cut.FindAll(".epos-wirt-kopf .epos-wirt-umschalter button")[1].Click();   // ValERI-Bewertung

        IElement block4 = cut.FindAll("section.epos-gruppenkopf")[3];
        Assert.Equal("4 · Unsicherheit", block4.QuerySelector(".epos-gruppenkopf-titel")!.TextContent.Trim());

        string[] teile = block4.QuerySelectorAll(
                ".epos-wirt-bandbreite-teil, .epos-wirt-spanne-teil, .epos-wirt-verlauf-teil, .epos-wirt-sensitivitaet-teil")
            .Select(e => e.ClassName ?? "").ToArray();
        Assert.Equal(4, teile.Length);
        Assert.Contains("epos-wirt-bandbreite-teil", teile[0]);
        Assert.Contains("epos-wirt-spanne-teil", teile[1]);
        Assert.Contains("epos-wirt-verlauf-teil", teile[2]);
        Assert.Contains("epos-wirt-sensitivitaet-teil", teile[3]);

        // Das Spannenbild: dasselbe Fragment, dasselbe Modell — dasselbe Markup.
        IElement spanne = block4.QuerySelector(".epos-wirt-spanne-teil")!;
        Assert.NotNull(spanne.QuerySelector("[data-marke='nulllinie']"));
        Assert.NotNull(spanne.QuerySelector("[data-marke='reihe:WP klein']"));
        Assert.Contains("data-wert=", spanneKennzahlen);
        Assert.Equal(spanneKennzahlen, OhneKennungen(spanne.QuerySelector("svg")!.OuterHtml));

        // Der Verlauf: dieselbe Komponente mit derselben Datenseite, Bild und Haken.
        IRenderedComponent<KapitalwertVerlaufAbschnitt> verlauf = cut.FindComponent<KapitalwertVerlaufAbschnitt>();
        Assert.Single(cut.FindComponents<KapitalwertVerlaufAbschnitt>());
        Assert.Same(dienste, verlauf.Instance.Dienste);
        Assert.True(gezeichnet > vorher, "Der Verlauf in Block 4 hat seinen Zeichenweg nicht gerufen.");
        IElement verlaufTeil = block4.QuerySelector(".epos-wirt-verlauf-teil")!;
        Assert.Single(verlaufTeil.QuerySelectorAll(".epos-diagramm-svg"));
        Assert.Equal(5, verlaufTeil.QuerySelectorAll("input.epos-schalter-kasten").Length);

        // Zurück zu „Kennzahlen": wieder genau EIN Verlauf, in „Wie sicher ist das?".
        cut.FindAll(".epos-wirt-kopf .epos-wirt-umschalter button")[0].Click();
        Assert.Single(cut.FindComponents<KapitalwertVerlaufAbschnitt>());
        Assert.Single(cut.FindAll("section.epos-gruppenkopf")[1].QuerySelectorAll(".epos-wirt-verlauf-teil"));
    }

    /// <summary>
    /// K8 und der Rest von U2: Die Fußleiste trägt höchstens DREI Knöpfe — Photovoltaik,
    /// BHKW, Berechnen (Q11, E7b: „Strombezug…" ist entfallen) —, und einen
    /// Verlaufsdialog gibt es nicht mehr: kein Knopf „Verlauf…", kein Unterdialog „Verlauf".
    /// </summary>
    [Fact]
    public void Die_Fussleiste_traegt_drei_Knoepfe_und_keinen_Verlauf()
    {
        var cut = Render<WirtschaftlichkeitSeite>(p => p
            .Add(x => x.Laden, () => Stand())
            .Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => new Dictionary<string, object>())
            .Add(x => x.Verlauf, new VerlaufDienste { Zeichnen = _ => Ansicht() }));

        string[] fuss = cut.FindAll(".epos-seite > .epos-leiste button")
                           .Select(k => k.TextContent.Trim()).ToArray();
        Assert.Equal(3, fuss.Length);
        Assert.Equal("Berechnen", fuss[^1]);
        Assert.DoesNotContain(fuss, t => t.StartsWith("Verlauf", StringComparison.Ordinal));
        Assert.DoesNotContain(fuss, t => t.StartsWith("Strombezug", StringComparison.Ordinal));

        Assert.DoesNotContain("Verlauf", Enum.GetNames(typeof(WirtschaftlichkeitSeite.Unterdialog)));
        Assert.DoesNotContain("Strombezug", Enum.GetNames(typeof(WirtschaftlichkeitSeite.Unterdialog)));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
    }

    /// <summary>
    /// Hat der Verlauf beim Rechnen neu simuliert, passen die gespeicherten Ergebnisse nicht
    /// mehr zum Simulationsstand — die Seite liest neu und sagt es in ihrer Statuszeile.
    /// </summary>
    [Fact]
    public void Eine_neu_simulierende_Rechnung_frischt_die_Seite_auf()
    {
        int geladen = 0;
        VerlaufAnsicht neu = Ansicht();
        neu.NeuSimuliert = true;
        neu.Meldung = "⚠ Für den Verlauf wurde neu simuliert — bitte „Berechnen“.";
        var cut = Render<WirtschaftlichkeitSeite>(p => p
            .Add(x => x.Laden, () => { geladen++; return Stand(); })
            .Add(x => x.Verlauf, new VerlaufDienste
            {
                Zeichnen = _ => Ansicht(),
                Berechnen = (j, w, ct) => Task.FromResult(neu)
            }));
        int vorher = geladen;

        cut.Find("button.epos-wirt-verlauf-rechnen").Click();

        cut.WaitForAssertion(() => Assert.True(geladen > vorher));
        cut.WaitForAssertion(() => Assert.Equal(neu.Meldung, cut.Instance.Status));
    }

    /// <summary>
    /// Ohne Datenseite zeichnet die Seite den Abschnitt mit Platzhalter — jede Seite muss
    /// auch ohne Gaben zeichnen.
    /// </summary>
    [Fact]
    public void Ohne_Datenseite_zeichnet_die_Seite_den_Abschnitt_mit_Platzhalter()
    {
        var cut = Render<WirtschaftlichkeitSeite>(p => p.Add(x => x.Laden, () => Stand()));

        Assert.Single(cut.FindAll(".epos-wirt-verlauf-teil"));
        Assert.Single(cut.FindAll(".epos-wirt-verlauf-teil .epos-chartbild-platzhalter"));
        Assert.Empty(cut.FindAll("button.epos-wirt-verlauf-rechnen"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle #458, Stufe 2) — über den Wirt
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE der Felder, die der Wirt für den Abschnitt anmeldet</b>: je Stand und
    /// je Szenario ein Haken, gesetzt über denselben Weg wie der Haken (er zeichnet nur
    /// neu), und der Zeitraum mit seinen Grenzen.
    /// </summary>
    [Fact]
    public void Der_Assistent_setzt_Haken_und_Zeitraum_ueber_die_Wege_des_Abschnitts()
    {
        var wahlen = new List<VerlaufWahl>();
        var cut = Abschnitt(new VerlaufDienste
        {
            Zeichnen = w => { wahlen.Add(w); return Ansicht(w.Staende?.ToList(), w.Szenarien?.ToList()); }
        });

        IReadOnlyList<Anzeigeschalter> haken = cut.Instance.KiSchalter;
        Assert.Equal(new[] { "WP klein", "BHKW", "Ungünstig", "Erwartet", "Günstig" },
                     haken.Select(h => h.Name).ToArray());
        Assert.All(haken, h => Assert.True(h.An));

        haken[0].An = false;                                               // WP klein ab
        Assert.Equal(new[] { BHKW }, wahlen[^1].Staende);
        Assert.False(cut.Instance.KiSchalter[0].An);

        cut.Instance.KiZeitraum = 30;
        Assert.Equal(30, cut.Instance.KiZeitraum);
        Assert.Throws<InvalidOperationException>(() => cut.Instance.KiZeitraum = 99);
    }

    /// <summary>Ohne gerechneten Verlauf gibt es keine Haken für den Assistenten.</summary>
    [Fact]
    public void Ohne_Verlauf_gibt_es_keine_Haken_fuer_den_Assistenten()
    {
        var cut = Abschnitt(null);

        Assert.Empty(cut.Instance.KiSchalter);
    }
}
