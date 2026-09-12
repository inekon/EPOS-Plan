using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Strom;
using EPOS.UI.Standards;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// STATION 4 „OPTIMIERUNG" (Auftrag #224, Anwenderentscheid SD‑E‑9 „Empfehlung" vom
/// 11.09.2026, Konzept „Stromspeicher-Dialoge" 7.4).
///
/// <para><b>Der Befund, den sie beantwortet</b> (Konzept 7.1/7.2): Die Rastersuche gab
/// es die ganze Zeit — erreichbar nur über DREI Schalter an DREI Orten, und das Wort
/// „Optimierung" kam in der Ablaufleiste nicht vor. Station 4 hieß „Bewerten".</para>
///
/// <para><b>Was hier geprüft wird:</b> der Aufbau der Station nach dem Mappenblatt V7 —
/// Kopf mit Ziel und Suchwahl, Suchraum je Einheit als Tabelle, die Kandidatenzeile
/// LIVE gegen die Grenze, der Feinraster-Schalter, der Rechenknopf und der Kasten
/// „Bestes Ergebnis". Und die zwei Umzüge: Schritt 1 ohne Größenbereich, Schritt 2 mit
/// der Jahresprojektion.</para>
/// </summary>
public sealed class OptimierungStationTests : EposBunitContext
{
    /// <summary>Die Ansicht trägt einen <c>InfoKnopf</c> und braucht deshalb den Hilfedienst.</summary>
    public OptimierungStationTests()
        => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    // =====================================================================
    //  Der Kopf: Ziel und die Wahl „bewerten" / „suchen"
    // =====================================================================

    /// <summary>
    /// Der Kopf nennt das ZIEL im Klartext — der Kapitalwert gegenüber „ohne Speicher"
    /// (SD‑Q11; die Mappe V7 maximiert den Jahresüberschuss, das Programm den
    /// Kapitalwert) — und stellt die zwei Wege zur Wahl.
    /// </summary>
    [Fact]
    public void Der_Kopf_nennt_das_Ziel_und_die_zwei_Wege()
    {
        var cut = Station();

        Assert.Contains(Resource.FLOTTE_OPT_ZIEL_TEXT, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_SUCHE_AUS, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_SUCHE_AN, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Wahl SETZT <c>Auslegung.FlottenGroessenOptimieren</c> — denselben Wert, den
    /// bis #224 der Schalter unter der Einheitenliste in Schritt 1 trug — und der
    /// Suchraum erscheint erst dann.
    /// </summary>
    [Fact]
    public void Die_Wahl_schaltet_den_Suchlauf_und_zeigt_erst_dann_den_Suchraum()
    {
        var cut = Station(suchen: false);

        Assert.DoesNotContain(Resource.FLOTTE_OPT_SUCHRAUM, cut.Markup, StringComparison.Ordinal);

        Suchwahl(cut, Resource.FLOTTE_OPT_SUCHE_AN).Change(true);

        Assert.True(cut.Instance.Eingaben.Auslegung!.FlottenGroessenOptimieren);
        Assert.Contains(Resource.FLOTTE_OPT_SUCHRAUM, cut.Markup, StringComparison.Ordinal);
    }

    // =====================================================================
    //  Der Suchraum als Tabelle
    // =====================================================================

    /// <summary>
    /// EINE ZEILE JE EINHEIT (Zielbild 7.4 Punkt 2) — mit Namen, Schalter „variieren",
    /// Größenkopplung, Anzahl und den drei Größenbereichen. Die Bereiche, die die
    /// Kopplung RECHNET statt zu rastern, stehen als „—" da.
    /// </summary>
    [Fact]
    public void Der_Suchraum_traegt_eine_Zeile_je_Einheit()
    {
        var cut = Station();

        IElement tabelle = cut.Find("table.epos-flotte-suchraum");
        IElement zeile = Assert.Single(tabelle.QuerySelectorAll("tbody tr"));

        Assert.Contains("Hauptspeicher", zeile.TextContent, StringComparison.Ordinal);
        Assert.Equal(7, tabelle.QuerySelectorAll("thead th").Length);

        // Kopplung „Kapazitaet und Leistung": die C-Raten-Spalte ist abgeleitet.
        Assert.Single(zeile.QuerySelectorAll("span.epos-flotte-suchraum-leer"));
    }

    /// <summary>Ein Bereich der Tabelle schreibt in die Achse der Einheit.</summary>
    [Fact]
    public void Ein_Bereich_der_Tabelle_schreibt_in_die_Achse()
    {
        var cut = Station();

        Zellenfelder(cut, spalte: 4)[1].Input("640");

        FlottenAuslegungsAchse achse =
            Assert.Single(cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen);
        Assert.Equal(640.0, achse.KapazitaetBisKWh, 9);
    }

    // =====================================================================
    //  #245 — der Fokus bleibt beim Tippen im Feld
    // =====================================================================

    /// <summary>
    /// EINE EINGABE LÄSST DIE ZEILE STEHEN (Anwenderbefund <b>#245</b>, 12.09.2026:
    /// „bei jeder Tastatureingabe springt der Fokus aus dem Feld").
    /// </summary>
    /// <remarks>
    /// <para><b>Was hier wirklich gemessen wird.</b> Nicht die DOM-Knoten — bunit liest
    /// das Markup nach jedem Zeichenlauf, der es ÄNDERT, komplett neu ein; ein
    /// <c>&lt;tr&gt;</c> ist danach IMMER eine andere AngleSharp-Instanz, auch wenn
    /// Blazor die Zeile im Browser stehen ließe. Gemessen wird deshalb die Identität der
    /// KOMPONENTEN in der Zeile: Bleibt jedes <c>Zahlenfeld</c>/<c>Ganzzahlfeld</c>
    /// dieselbe Instanz, hat Blazor den Teilbaum behalten — und damit im Browser auch
    /// dessen <c>&lt;input&gt;</c> samt Fokus. Wird er abgerissen, sind es neue
    /// Instanzen.</para>
    /// <para><b>Vor dem Fix war das so</b>: <c>&lt;tr @@key="a"&gt;</c> hing an der
    /// <c>FlottenAuslegungsAchse</c> (Referenzvergleich), und
    /// <c>StromspeicherAuslegungSeite.FlotteGeschrieben</c> ersetzt die Konfiguration
    /// nach JEDEM gemeldeten Wert durch eine JSON-Tiefenkopie — neuer Schlüssel je
    /// Tastendruck, Zeile weg. Seither steht dort eine Wertidentität
    /// (<c>OptimierungBlock.Zeilenschluessel</c>).</para>
    /// </remarks>
    [Fact]
    public void Eine_Eingabe_im_Suchraum_laesst_die_Zeile_und_ihre_Felder_stehen()
    {
        var cut = Station();

        IReadOnlyList<object> vorher = Zeilenkomponenten(cut);
        // Die Zeile fuehrt zwei Ganzzahlfelder (Anzahl von/bis) und sechs Zahlenfelder
        // (Kapazitaet und Leistung, je von/bis/Schritt) — die C-Rate ist abgeleitet.
        Assert.Equal(8, vorher.Count);

        Zellenfelder(cut, spalte: 4)[1].Input("640");

        IReadOnlyList<object> nachher = Zeilenkomponenten(cut);
        Assert.Equal(vorher.Count, nachher.Count);
        for (int i = 0; i < vorher.Count; i++) Assert.Same(vorher[i], nachher[i]);

        FlottenAuslegungsAchse achse =
            Assert.Single(cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen);
        Assert.Equal(640.0, achse.KapazitaetBisKWh, 9);
    }

    /// <summary>
    /// Die ANGEFANGENE Dezimalzahl bleibt stehen: Wer „640," getippt hat, findet „640,"
    /// vor und nicht „640" — die zweite Hälfte desselben Befunds #245.
    /// </summary>
    /// <remarks>
    /// „640," ist bereits eine gültige Zahl (<c>Zahlen.ZahlParsen</c> nimmt Komma wie
    /// Punkt), der Wert 640 geht also hinaus; <c>Zahlenfeld.OnParametersSet</c> lässt den
    /// Text dann in Ruhe, weil er denselben Wert meint. Riss die Zeile ab, entstand ein
    /// FRISCHES Feld ohne Texterinnerung — es schrieb den Anzeigetext „640" und nahm dem
    /// Anwender mitten in der Eingabe das Trennzeichen weg.
    /// </remarks>
    [Fact]
    public void Eine_angefangene_Dezimalzahl_bleibt_im_Feld_stehen()
    {
        var cut = Station();

        Zellenfelder(cut, spalte: 4)[1].Input("640,");

        Assert.Equal("640,", Zellenfelder(cut, spalte: 4)[1].GetAttribute("value"));
    }

    /// <summary>
    /// Der Wechsel der GRÖSSENKOPPLUNG tauscht die Spalten, die gerastert werden —
    /// dieselben Modellfelder wie bis #224 im Einheiteneditor.
    /// </summary>
    [Fact]
    public void Die_Groessenkopplung_wechselt_die_gerasterten_Spalten()
    {
        var cut = Station();

        cut.Find("table.epos-flotte-suchraum tbody tr")
           .QuerySelectorAll("select")[0]
           .Change(((int)FlottenAuslegungsmodus.LeistungUndCRate).ToString());

        FlottenAuslegungsAchse achse =
            Assert.Single(cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen);
        Assert.Equal(FlottenAuslegungsmodus.LeistungUndCRate, achse.Modus);

        // Jetzt ist die KAPAZITAET die abgeleitete Groesse.
        IElement zeile = cut.Find("table.epos-flotte-suchraum tbody tr");
        Assert.Empty(zeile.QuerySelectorAll("td")[4].QuerySelectorAll("input"));
        Assert.Equal(3, zeile.QuerySelectorAll("td")[5].QuerySelectorAll("input").Length);
    }

    // =====================================================================
    //  Die Kandidatenzeile — live, und sie sperrt
    // =====================================================================

    /// <summary>
    /// Die Kandidatenzeile nennt beide Phasen und die Grenze — und sie kommt aus der
    /// ZÄHLREGEL des Kerns, nicht aus einer zweiten Formel in der Seite.
    /// </summary>
    [Fact]
    public void Die_Kandidatenzeile_nennt_Grobraster_Feinraster_und_Grenze()
    {
        var cut = Station();

        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(
            cut.Instance.Eingaben.Auslegung!.Flotte!);
        IElement zeile = cut.Find("p.epos-flotte-kandidatenzeile");

        Assert.True(zahl.Grob > 1);
        Assert.True(zahl.FeinHoechstens > 0);
        Assert.Contains(zahl.Grob.ToString(Kultur), zeile.TextContent, StringComparison.Ordinal);
        Assert.Contains(zahl.Gesamt.ToString(Kultur), zeile.TextContent, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_RASTER_OK, zeile.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("epos-flotte-kandidatenzeile--rot", zeile.ClassName!, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Der Grenzfall.</b> Reißt das Raster die Grenze, wird die Zeile ROT, sagt es in
    /// WORTEN (Farbe allein trägt keine Aussage) — und der Rechenknopf ist gesperrt und
    /// nennt den Grund. Bis #224 flog das erst BEIM START als Ausnahme auf.
    /// </summary>
    [Fact]
    public void Ein_zu_grosses_Raster_faerbt_die_Zeile_rot_und_sperrt_den_Rechenknopf()
    {
        var cut = Station(maximaleKandidaten: 3);

        IElement zeile = cut.Find("p.epos-flotte-kandidatenzeile");
        Assert.Contains("epos-flotte-kandidatenzeile--rot", zeile.ClassName!, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_RASTER_ZUVIEL, zeile.TextContent, StringComparison.Ordinal);

        IElement knopf = cut.Find(".epos-flotte-optimierung-lauf button");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Contains(Resource.FLOTTE_OPT_RASTER_ZUVIEL, knopf.GetAttribute("title")!,
                        StringComparison.Ordinal);
    }

    /// <summary>Die Gegenprobe: Mit weiter Grenze ist derselbe Suchraum zulässig.</summary>
    [Fact]
    public void Mit_weiter_Grenze_ist_derselbe_Suchraum_zulaessig()
    {
        var cut = Station(maximaleKandidaten: 10000);

        Assert.DoesNotContain("epos-flotte-kandidatenzeile--rot",
                              cut.Find("p.epos-flotte-kandidatenzeile").ClassName!,
                              StringComparison.Ordinal);
        Assert.False(cut.Find(".epos-flotte-optimierung-lauf button").HasAttribute("disabled"));
    }

    // =====================================================================
    //  Der Feinraster-Schalter
    // =====================================================================

    /// <summary>
    /// Der Schalter steht mit seiner Erklärzeile da und ist VORGEGEBEN AN (SD‑Q10) —
    /// ein Stand ohne das Feld verhält sich damit wie die Mappe V7.
    /// </summary>
    [Fact]
    public void Der_Feinraster_Schalter_steht_an_und_laesst_sich_abschalten()
    {
        var cut = Station();

        Assert.True(cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Feinraster);
        Assert.Contains(Resource.FLOTTE_OPT_FEINRASTER_HINWEIS, cut.Markup, StringComparison.Ordinal);

        Feinrasterschalter(cut).Change(false);

        Assert.False(cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Feinraster);

        // Ohne zweite Phase nennt die Zeile nur noch das Grobraster.
        Assert.Equal(0, FlottenOptimierer.Kandidatenzahl(
            cut.Instance.Eingaben.Auslegung!.Flotte!).FeinHoechstens);
    }

    // =====================================================================
    //  Der Kasten „Bestes Ergebnis"
    // =====================================================================

    /// <summary>
    /// Ohne Suche steht hier der Satz „Noch keine Suche gerechnet." und kein leerer
    /// Kasten.
    /// </summary>
    [Fact]
    public void Ohne_Suche_steht_kein_Kasten()
    {
        var cut = Station();

        Assert.Contains(Resource.FLOTTE_OPT_KEIN_ERGEBNIS, cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".epos-flotte-bestes"));
    }

    /// <summary>
    /// Nach einer Suche steht der Kasten mit Kapitalwert, jährlicher Ersparnis (SD‑Q11),
    /// Größe, Einheitenzahl, geprüften und zulässigen Kandidaten, Rechendauer — und der
    /// MARKE, aus welcher Phase der Beste stammt.
    /// </summary>
    [Fact]
    public void Nach_einer_Suche_steht_der_Kasten_mit_allen_sieben_Angaben()
    {
        var cut = Gerechnet();
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Optimierung);

        IElement kasten = cut.Find(".epos-flotte-bestes");

        Assert.Contains("1.500", kasten.TextContent, StringComparison.Ordinal);   // Kapitalwert
        Assert.Contains("180", kasten.TextContent, StringComparison.Ordinal);     // Ersparnis
        Assert.Contains("30", kasten.TextContent, StringComparison.Ordinal);      // Kapazitaet
        Assert.Contains("3", kasten.TextContent, StringComparison.Ordinal);       // geprueft
        Assert.Contains(Resource.FLOTTE_OPT_BEST_GEPRUEFT, kasten.TextContent, StringComparison.Ordinal);

        // Die Phasenmarke sagt: der Beste kommt aus dem FEINRASTER.
        Assert.Contains(Resource.FLOTTE_OPT_MARKE_FEIN,
                        cut.Find(".epos-flotte-phasenmarke").TextContent, StringComparison.Ordinal);
    }

    /// <summary>Ein Bester aus dem Grobraster trägt die andere Marke — die Gegenprobe.</summary>
    [Fact]
    public void Ein_Bester_aus_dem_Grobraster_traegt_die_andere_Marke()
    {
        var cut = Gerechnet(phase: FlottenKandidatPhase.Grob);
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Optimierung);

        Assert.Contains(Resource.FLOTTE_OPT_MARKE_GROB,
                        cut.Find(".epos-flotte-phasenmarke").TextContent, StringComparison.Ordinal);
    }

    // =====================================================================
    //  Die zwei Umzuege — Schritt 1 und Schritt 2
    // =====================================================================

    /// <summary>
    /// SCHRITT 1 trägt nur noch die Einheiten (Zielbild 7.4): kein Größenbereich, keine
    /// Jahresprojektion, kein Schalter „Speicheranzahl und Größenbereiche optimieren".
    /// </summary>
    [Fact]
    public void Schritt_eins_traegt_nur_noch_die_Einheiten()
    {
        var cut = Station();
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Speicher);

        Assert.Single(cut.FindComponents<SpeicherFlottenEditor>());
        Assert.Empty(cut.FindComponents<OptimierungBlock>());
        Assert.Empty(cut.FindComponents<SpeicherFlottenWirtschaftBlock>());
        Assert.DoesNotContain(Resource.FLOTTE_DLG_CHK_OPTIMIEREN, cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Maximale Auslegungskandidaten", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// SCHRITT 2 trägt die wirtschaftliche Jahresprojektion (SD‑Q12) samt den DREI
    /// Erklärzeilen aus Konzept 7.3 — Ausgleichswert, Restwert der Studie, maximale
    /// Kandidaten.
    /// </summary>
    [Fact]
    public void Schritt_zwei_traegt_die_Jahresprojektion_mit_drei_Erklaerzeilen()
    {
        var cut = Station();
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Daten);

        Assert.Single(cut.FindComponents<SpeicherFlottenWirtschaftBlock>());
        Assert.Contains(Resource.FLOTTE_ED_AUSGLEICH_ERL, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_ED_RESTWERT_ERL, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_ED_KANDIDATEN_ERL, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// SCHRITT 3 trägt „Netz und Planung" — es beschreibt den BETRIEB und nicht eine
    /// Einheit (Zielbild 7.4).
    /// </summary>
    [Fact]
    public void Schritt_drei_traegt_Netz_und_Planung()
    {
        var cut = Station();
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);

        Assert.Single(cut.FindComponents<SpeicherFlottenNetzBlock>());
        Assert.Contains(Resource.FLOTTE_ED_BEZUGSGRENZE, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Der Ausgleichswert zeigt vier Nachkommastellen</b>, der GESPEICHERTE Wert
    /// bleibt ganz (Konzept 7.8): Der Vorschlag entsteht als mittlerer Bezugspreis und
    /// trägt deshalb einen Gleitkommarest.
    /// </summary>
    [Fact]
    public void Der_Ausgleichswert_wird_gerundet_angezeigt_aber_nicht_gespeichert()
    {
        var cut = Station(ausgleich: 0.31746000000002055);
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Daten);

        IElement feld = cut.FindAll("label")
            .Single(x => x.TextContent.Contains(Resource.FLOTTE_ED_AUSGLEICH, StringComparison.Ordinal))
            .QuerySelector("input")!;

        Assert.Equal("0,3175", feld.GetAttribute("value"));
        Assert.Equal(0.31746000000002055,
                     cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.EnergieAusgleichEuroProKWh!.Value,
                     15);
    }

    // =====================================================================
    //  Die Vorpruefungshinweise sind kompakt (Konzept 7.8)
    // =====================================================================

    /// <summary>
    /// EIN Hinweis ist EINE Zeile — kein Warnbanner über die ganze Breite. Die
    /// Vorprüfung SPERRT nicht, und eine Warnfläche meldete einen Fehler, den es nicht
    /// gibt.
    /// </summary>
    [Fact]
    public void Ein_Vorpruefungshinweis_ist_eine_Zeile()
    {
        var cut = Station(hinweise: new[] { Hinweis("Betriebsaufwand 1,00 €/a") });

        Assert.Single(cut.FindAll("p.epos-flotte-hinweiszeile"));
        Assert.Empty(cut.FindAll(".epos-flotte-hinweiszeilen--block"));
        Assert.Contains("Betriebsaufwand", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// ZWEI Hinweise stehen in einem aufklappbaren Block — beim ersten Erscheinen
    /// offen, danach entscheidet der Anwender.
    /// </summary>
    [Fact]
    public void Zwei_Vorpruefungshinweise_stehen_in_einem_aufklappbaren_Block()
    {
        var cut = Station(hinweise: new[]
        {
            Hinweis("Betriebsaufwand 1,00 €/a"), Hinweis("Start-Ladezustand 50 %")
        });

        Assert.Single(cut.FindAll(".epos-flotte-hinweiszeilen--block"));
        Assert.Equal(2, cut.FindAll("p.epos-flotte-hinweiszeile").Count);

        cut.Find(".epos-flotte-hinweiszeilen__kopf").Click();

        Assert.Empty(cut.FindAll("p.epos-flotte-hinweiszeile"));
        Assert.Single(cut.FindAll(".epos-flotte-hinweiszeilen--block"));
    }

    // ================================================================= Prüfstand

    private static System.Globalization.CultureInfo Kultur
        => System.Globalization.CultureInfo.CurrentCulture;

    /// <summary>Die Ansicht, auf Station 4 gestellt.</summary>
    private IRenderedComponent<StromspeicherAuslegungSeite> Station(
        bool suchen = true, int maximaleKandidaten = 10000, double? ausgleich = null,
        IReadOnlyList<FlottenHinweis>? hinweise = null)
    {
        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Flotte = Flotte(maximaleKandidaten, ausgleich),
                    FlottenGroessenOptimieren = suchen
                }
            }
        };

        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste
            {
                Vorgaben = () => vorgaben,
                FlotteRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis()),
                Vorpruefen = _ => hinweise ?? Array.Empty<FlottenHinweis>()
            })
            .Add(x => x.PlanerVerfuegbar, true));

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Optimierung);
        return cut;
    }

    /// <summary>Die Ansicht nach einem Suchlauf mit drei Kandidaten.</summary>
    private IRenderedComponent<StromspeicherAuslegungSeite> Gerechnet(
        FlottenKandidatPhase phase = FlottenKandidatPhase.Fein)
    {
        FlottenKandidatZusammenfassung bester = Kandidat("K-30", 30, phase, 1500);
        var ergebnis = new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Auslegung = new FlottenAuslegungErgebnis
            {
                Kandidaten = new List<FlottenKandidatZusammenfassung>
                {
                    Kandidat("K-10", 10, FlottenKandidatPhase.Grob, 500),
                    Kandidat("K-20", 20, FlottenKandidatPhase.Grob, 900),
                    bester
                },
                BesterKandidat = bester,
                FeinrasterGerechnet = phase == FlottenKandidatPhase.Fein,
                Rechendauer = TimeSpan.FromSeconds(0.7)
            }
        };

        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Flotte = Flotte(10000, null),
                    FlottenGroessenOptimieren = true
                }
            }
        };

        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste
            {
                Vorgaben = () => vorgaben,
                FlotteRechnen = (_, _) => Task.FromResult(ergebnis)
            })
            .Add(x => x.PlanerVerfuegbar, true));

        Auslegungshilfe.Rechenknopf(cut).Click();
        return cut;
    }

    private static FlottenKandidatZusammenfassung Kandidat(
        string id, double kapazitaet, FlottenKandidatPhase phase, double kapitalwert) => new()
    {
        KandidatId = id,
        Betriebsziel = FlottenBetriebsziel.PeakShaving,
        Zulaessig = true,
        Phase = phase,
        KapitalwertEuro = kapitalwert,
        KapazitaetKWh = kapazitaet,
        LadeleistungKw = kapazitaet / 2.0,
        EntladeleistungKw = kapazitaet / 2.0,
        ErsparnisEuroJahr = 180,
        Einheiten = new List<FlottenKandidatEinheit>
        {
            new()
            {
                Id = "s1",
                KapazitaetKWh = kapazitaet,
                LadeleistungKw = kapazitaet / 2.0,
                EntladeleistungKw = kapazitaet / 2.0
            }
        }
    };

    private static FlottenStudieKonfiguration Flotte(int maximaleKandidaten, double? ausgleich)
    {
        var einheit = new FlottenEinheit
        {
            Id = "s1", Name = "Hauptspeicher", KapazitaetKWh = 100,
            LadeleistungKw = 50, EntladeleistungKw = 50,
            Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
            SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
        };
        return new FlottenStudieKonfiguration
        {
            Einheiten = new List<FlottenEinheit> { einheit },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving,
                EnergieAusgleichEuroProKWh = ausgleich ?? 0.3
            },
            Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
            {
                Kalkulationszins = 0.03, ProjektjahreBeiWiederholung = 20
            },
            Auslegung = new FlottenAuslegungEingang
            {
                MaximaleKandidaten = maximaleKandidaten,
                Achsen = new List<FlottenAuslegungsAchse>
                {
                    new()
                    {
                        Aktiv = true, Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                        AnzahlVon = 1, AnzahlBis = 1,
                        KapazitaetVonKWh = 100, KapazitaetBisKWh = 300, KapazitaetSchrittKWh = 50,
                        LeistungVonKw = 40, LeistungBisKw = 80, LeistungSchrittKw = 20,
                        CRateVon = 0.5, CRateBis = 2.0, CRateSchritt = 0.5,
                        Vorlage = einheit
                    }
                }
            }
        };
    }

    private static FlottenHinweis Hinweis(string text)
        => new() { Stufe = FlottenHinweisStufe.Hinweis, Text = text };

    private static IElement Suchwahl(IRenderedComponent<StromspeicherAuslegungSeite> cut, string text)
        => cut.FindAll("input[type=radio]")
              .Single(x => x.ParentElement!.TextContent.Contains(text, StringComparison.Ordinal));

    private static IElement Feinrasterschalter(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.FindAll("label")
              .Single(x => x.TextContent.Contains(Resource.FLOTTE_OPT_FEINRASTER, StringComparison.Ordinal))
              .QuerySelector("input")!;

    /// <summary>
    /// Die Zahlen- und Ganzzahlfelder der Suchraumzeile als KOMPONENTENinstanzen —
    /// die Prüfgröße des Befunds #245 (siehe dort, warum nicht die DOM-Knoten).
    /// </summary>
    private static IReadOnlyList<object> Zeilenkomponenten(
        IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.FindComponents<Ganzzahlfeld>().Select(x => (object)x.Instance)
              .Concat(cut.FindComponents<Zahlenfeld>().Select(x => (object)x.Instance))
              .ToList();

    /// <summary>Die Eingabefelder EINER Spalte der Suchraumtabelle.</summary>
    private static IReadOnlyList<IElement> Zellenfelder(
        IRenderedComponent<StromspeicherAuslegungSeite> cut, int spalte)
        => cut.Find("table.epos-flotte-suchraum tbody tr")
              .QuerySelectorAll("td")[spalte]
              .QuerySelectorAll("input")
              .ToList();
}
