using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Seiten.Simulation;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Der STROMSPEICHER-Reiter (iU9-W11b.9), Vorbild <c>tabPage_Stromspeicher</c> —
/// die Seite mit NULL Designer-Kindern, die im Vorlaeufer vollstaendig
/// programmatisch entstand.
///
/// <para>Soll: Kopfzeile mit bzw. ohne Lauf, zwoelf Kacheln, das Bild „Lastgang
/// und Speicherbetrieb" samt Umschalter und Reihenwahl (W11b‑B‑26), die
/// 39 Kennzahlzeilen in drei Gruppen mit ihrer Warnstufe, die Vergleichsspalte
/// nur mit Vergleichslauf, die Ampel, die Warnzeile „ohne Erzeugung" und der
/// Vergleichsknopf erst ab zwei Varianten.</para>
/// <para>Der Selektor nennt seit der Windows-Abnahme 05.09.2026 die Klasse
/// <c>epos-simerg-knopf</c>: Jedes Diagramm steht seither im Baustein
/// <c>Diagramm</c> und bringt seine eigenen Knöpfe („1:1“, „Bereich“) mit.
/// <c>FindAll("button")</c> zählte die mit und prüfte damit nicht mehr, was
/// der Fall behauptet — nämlich die Knöpfe DIESES Reiters.</para>
///
/// <para><b>Seit W11b‑B‑28 steht der SPEICHERPARAMETERBLOCK ganz oben</b>
/// (Anwenderwunsch 10.09.2026). Er bringt eigene Kontrollkästchen, eigene
/// Hinweisabsätze und — mit Diensten — eigene Knöpfe mit. Die Fälle, die den
/// BILDbereich meinen, greifen deshalb ausdrücklich in
/// <c>section.epos-simerg-diagrammzeile</c>; was der Block selbst tut, steht in
/// <c>SpeicherParameterBlockTests</c>.</para>
/// </summary>
public class StromspeicherReiterTests : EposBunitContext
{
    private readonly List<Bildauftrag> _auftraege = new();

    public StromspeicherReiterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static SpeicherKennzahlenBlock.Zeile Z(string gruppe, string name, string wert,
                                                   string vergleich = "",
                                                   KennzahlStufe stufe = KennzahlStufe.Unbestimmt,
                                                   string untergruppe = "",
                                                   KennzahlArt art = KennzahlArt.Normal,
                                                   string hinweis = "")
        => new SpeicherKennzahlenBlock.Zeile(gruppe, name, wert, vergleich, "kWh/a", stufe,
                                             untergruppe, art, hinweis);

    private static SpeicherErgebnisDaten Daten(bool lauf = true, bool vergleich = false,
                                               bool mehrere = true, string hinweis = "")
        => new SpeicherErgebnisDaten
        {
            LaufVorhanden = lauf,
            Kopf = lauf ? "Speicher 1 · Grünstrom · Dauernutzung" : "Noch keine Speicherrechnung",
            Kacheln = lauf
                ? new (string, string)[]
                {
                    ("Kapazität", "10,0"), ("Leistung", "11,0"), ("SoC [%]", "10 … 90"),
                    ("SoC [kWh]", "1,0 … 9,0"), ("Betriebsart", "Grünstrom"),
                    ("Berechnungsart", "Dauernutzung"), ("Ertrag", "312,50"),
                    ("Überschuss", "40,00"), ("Amortisation", "12,5"),
                    ("Vollzyklen", "180,0"), ("Eigenverbrauch", "62,5"), ("Autarkie", "38,1")
                }
                : Array.Empty<(string, string)>(),
            Kennzahlen = lauf
                ? new[]
                {
                    Z(SpeicherKennzahlenBlock.GRUPPE_ENERGIE, "Last", "12.000", vergleich ? "11.500" : ""),
                    Z(SpeicherKennzahlenBlock.GRUPPE_SPEICHER, "Vollzyklen", "180,0",
                      vergleich ? "175,0" : "", KennzahlStufe.Knapp),
                    Z(SpeicherKennzahlenBlock.GRUPPE_WIRTSCHAFT, "Ertrag", "312,50",
                      vergleich ? "300,00" : "", KennzahlStufe.Ok)
                }
                : Array.Empty<SpeicherKennzahlenBlock.Zeile>(),
            MitVergleich = vergleich,
            Ampel = lauf ? "180 von 250 Zyklen" : "",
            AmpelWarnung = false,
            Erzeugungshinweis = hinweis,
            MehrereVarianten = mehrere
        };

    private IRenderedComponent<StromspeicherReiter> Zeichnen(SpeicherErgebnisDaten daten,
                                                             Action? csv = null,
                                                             Action? vergleich = null,
                                                             bool mitBild = true,
                                                             SpeicherParameterDaten? parameter = null,
                                                             bool konfigWeg = false,
                                                             SimulationErgebnisDienste? dienste = null)
        => Render<StromspeicherReiter>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Parameter, parameter ?? new SpeicherParameterDaten());
            if (dienste is not null) p.Add(x => x.Dienste, dienste);
            if (konfigWeg)
                p.Add(x => x.KonfigurationOeffnen,
                      EventCallback.Factory.Create(this, () => _konfigwechsel++));
            if (mitBild) p.Add(x => x.Modell, Modell);
            if (csv is not null) p.Add(x => x.Csv, EventCallback.Factory.Create(this, csv));
            if (vergleich is not null) p.Add(x => x.Vergleich, EventCallback.Factory.Create(this, vergleich));
        });

    private int _konfigwechsel;

    /// <summary>
    /// Die Zeichenmodelle des Betriebsbildes, nach Schalterstellung getrennt und je
    /// EINMAL gebaut. Der Baustein <c>DiagrammSvg</c> vergleicht die Modellreferenz;
    /// ein je Zeichenlauf neu gebautes Modell setzte seinen Baum jedes Mal neu und
    /// nähme ihm Zoom und abgewählte Reihe.
    /// </summary>
    private readonly Dictionary<bool, Zeichenmodell> _modelle = new();

    private Zeichenmodell? Modell(Bildauftrag a)
    {
        _auftraege.Add(a);

        if (_modelle.TryGetValue(a.Sortiert, out Zeichenmodell? vorhanden)) return vorhanden;

        Zeichenmodell neu = Betriebsbild(a.Sortiert);
        _modelle[a.Sortiert] = neu;
        return neu;
    }

    /// <summary>
    /// „Lastgang und Speicherbetrieb" aus einer KURZEN Reihe (eine Woche) — der
    /// Ladezustand steht auf der ZWEITEN Achse, wie im Bild des Kerns.
    /// </summary>
    private static Zeichenmodell Betriebsbild(bool sortiert)
    {
        var last = new double[168];
        var soc = new double[168];
        for (int i = 0; i < last.Length; i++)
        {
            last[i] = 30.0 + 25.0 * Math.Sin(2 * Math.PI * i / 24.0);
            soc[i] = 5.0 + 4.0 * Math.Cos(2 * Math.PI * i / 24.0);
        }

        return ChartRenderer.SpeicherbetriebModell(
            "Lastgang und Speicherbetrieb",
            new[] { new ChartRenderer.Reihe("Netzbezug", last, ChartRenderer.C_NETZ) },
            "kW",
            new ChartRenderer.Reihe("Ladezustand", soc, ChartRenderer.C_SPEICHER[0]),
            "kWh", sortiert);
    }

    /// <summary>Die Kontrollkästchen DES BILDES — nicht die des Parameterblocks.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Bildschalter(
        IRenderedComponent<StromspeicherReiter> seite)
        => seite.FindAll("section.epos-simerg-diagrammzeile input[type='checkbox']");

    /// <summary>Die Knöpfe DES BILDES (CSV, Vergleich) — nicht die des Parameterblocks.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> Bildknoepfe(
        IRenderedComponent<StromspeicherReiter> seite)
        => seite.FindAll("section.epos-simerg-diagrammzeile button.epos-simerg-knopf");

    /// <summary>Der letzte Auftrag des Betriebsbildes — der, den der Reiter gerade zeigt.</summary>
    private Bildauftrag Letzter => _auftraege.Last(a => a.Bild == Bilder.SpeicherBetrieb);

    // =====================================================================

    [Fact]
    public void Die_Kopfzeile_nennt_Variante_Betriebsart_und_Berechnungsart()
    {
        var seite = Zeichnen(Daten());
        Assert.Contains("Grünstrom", seite.Find("p.epos-simerg-status").TextContent);
    }

    /// <summary>
    /// Ohne Speicherlauf steht nur die Warnzeile da - kein Bild, keine Kacheln,
    /// keine Kennzahlen (<c>SpeicherErgebnisAnzeigen</c> :7196-7211).
    /// </summary>
    [Fact]
    public void Ohne_Lauf_bleibt_nur_die_Warnzeile()
    {
        var seite = Zeichnen(Daten(lauf: false));

        Assert.Contains("epos-simerg-warn", seite.Find("p.epos-simerg-status").ClassName);
        Assert.Empty(seite.FindAll("img"));
        Assert.Empty(seite.FindAll("table"));
        Assert.Empty(seite.FindAll("button.epos-simerg-knopf"));
    }

    /// <summary>
    /// <b>W11b‑B‑28: Der Parameterblock steht ÜBER den Kacheln.</b> Geprüft wird die
    /// Reihenfolge im Markup — die Kopfzeile, dann der Block, dann das Kachelraster;
    /// so, wie der Anwender es beschrieben hat („bringe den Tab Parameter →
    /// Stromspeicher … in den Tab ‚Stromspeicher‘").
    /// </summary>
    [Fact]
    public void Der_Parameterblock_steht_ueber_den_Kacheln()
    {
        var seite = Zeichnen(Daten());

        Assert.Single(seite.FindComponents<SpeicherParameterBlock>());

        string markup = seite.Markup;
        Assert.True(markup.IndexOf("epos-simerg-speicherparameter", StringComparison.Ordinal)
                    < markup.IndexOf("epos-kachelraster", StringComparison.Ordinal));
    }

    /// <summary>
    /// Und er steht auch OHNE Lauf da. Bis W11b‑B‑28 zeigte der Reiter dann nur eine
    /// Warnzeile — gerade vor dem ERSTEN Lauf will der Anwender aber die
    /// Betriebsführung einstellen.
    /// </summary>
    [Fact]
    public void Der_Parameterblock_steht_auch_ohne_Lauf()
    {
        var seite = Zeichnen(Daten(lauf: false),
                             parameter: new SpeicherParameterDaten
                             {
                                 VarianteVorhanden = true,
                                 Variantenstatus = "Aktive Variante: Speicher 1"
                             });

        Assert.Single(seite.FindComponents<SpeicherParameterBlock>());
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_LABEL_SOC_MIN,
                        seite.Markup);
        Assert.Empty(seite.FindAll(".epos-kennzahlkachel"));
    }

    /// <summary>
    /// Der Reiter trägt KEINEN Einstieg in die Auslegung mehr (Auftrag #274,
    /// Anwenderwunsch 14.09.2026) — weder den alten Optimierungsknopf im
    /// Einzelparameterblock noch den Knopf „Speicherflotte &amp; Auslegung öffnen".
    /// Ausgelegt wird in Schritt ①.
    /// </summary>
    [Fact]
    public void Der_Reiter_traegt_keinen_Einstieg_in_die_Auslegung_mehr()
    {
        var seite = Zeichnen(Daten(), konfigWeg: true);

        Assert.DoesNotContain(seite.FindAll("button"),
                              b => b.TextContent.Contains("optimieren"));
        Assert.DoesNotContain("Speicherflotte", seite.Markup);
        Assert.DoesNotContain(
            seite.FindComponent<SpeicherParameterBlock>().FindAll("button"),
            b => b.TextContent.Contains("optimieren"));
    }

    /// <summary>
    /// <b>WINDOWS-ABNAHME #216 (11.09.2026), Punkt 3.</b> Der Einzelanlagenblock hing
    /// bis dahin an der FRAGE, ob die Plattform eine Flotte rechnen kann — unter
    /// Windows ist das immer der Fall, und damit war er unerreichbar. Er hängt jetzt
    /// am STAND: Solange kein Flottenstand aktiviert ist, fährt der Projektlauf die
    /// Einzelanlage, und dann sind das seine Parameter. Er steht unter seiner eigenen
    /// Überschrift, damit man sieht, wozu er gehört.
    /// </summary>
    [Fact]
    public void Ohne_aktivierte_Flotte_steht_der_Einzelanlagenblock_unter_seiner_Ueberschrift()
    {
        var seite = Zeichnen(Daten(), konfigWeg: true);

        Assert.Single(seite.FindComponents<SpeicherParameterBlock>());
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SP_GRP_EINZELANLAGE,
                        seite.Markup);
    }

    /// <summary>
    /// <b>AUFTRAG #274</b> (Anwenderwunsch 14.09.2026): An die Stelle des Blocks
    /// „Speicherflotte und Auslegung" tritt EINE Herkunftszeile — Betriebsziel,
    /// Einheitenzahl, Peak-Ziel — samt dem Verweis zurück in Schritt ①. Kein
    /// Betriebseditor, keine Eingabetabelle, kein Knopf „öffnen".
    /// </summary>
    [Fact]
    public void Aktivierte_Flotte_ersetzt_den_Altkopf_durch_die_Herkunftszeile()
    {
        SpeicherErgebnisDaten daten = Daten();
        daten.FlotteImProjektAktiv = true;
        daten.Kopf = "Speicher 1 · Grünstrom · Dauernutzung";
        daten.FlottenAenderungOhneNeuenLauf = true;
        daten.AktiveFlotte = new FlottenStudieKonfiguration
        {
            Einheiten = new()
            {
                new() { Id = "a", Name = "Hauptspeicher", KapazitaetKWh = 120, LadeleistungKw = 42, EntladeleistungKw = 57 },
                new() { Id = "b", Name = "Schnellspeicher", KapazitaetKWh = 36, LadeleistungKw = 28, EntladeleistungKw = 33 }
            },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.MultiUse,
                Verteilung = FlottenVerteilung.Grenzkosten,
                WirtschaftlicherPeakZielwertKw = 88
            }
        };

        var seite = Zeichnen(daten, konfigWeg: true);

        // Die Herkunft steht da — und der Kopf der Einzelanlage nicht mehr.
        Assert.Contains(string.Format(CultureInfo.CurrentCulture,
            WindowsFormsApplication1.MyResource.Resource.SIM_SP_HERKUNFT,
            WindowsFormsApplication1.MyResource.Resource.FLOTTE_ZIEL_MULTIUSE, 2),
            seite.Find("p.epos-simerg-herkunft").TextContent);
        Assert.Single(seite.FindAll("p.epos-simerg-status"));
        Assert.DoesNotContain(daten.Kopf, seite.Find("p.epos-simerg-status").TextContent);

        // Kein Editor, keine Eingabetabelle, kein zweiter Einstieg.
        Assert.Empty(seite.FindComponents<SpeicherFlottenBetriebEditor>());
        Assert.DoesNotContain("Speicherflotte und Auslegung", seite.Markup);
        Assert.DoesNotContain(seite.FindAll("button"),
            x => x.TextContent.Contains("Auslegung öffnen"));

        // #216: MIT aktivierter Flotte bleibt der Einzelanlagenblock weg — die
        // Betriebsart folgt dann dem Häkchen „Netzladung" der Auslegung.
        Assert.Empty(seite.FindComponents<SpeicherParameterBlock>());
        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.SP_GRP_EINZELANLAGE,
                              seite.Markup);

        // Das Veraltet-Banner bleibt — es gehört zum Ergebnis, nicht zum Editor.
        Assert.Contains("Flotte geändert: Projektsimulation neu berechnen. Angezeigte Ergebnisse gehören noch zum vorherigen Lauf.", seite.Markup);
    }

    /// <summary>
    /// #215 in der Herkunftszeile (#274): Sie nennt das Peak-Ziel samt seinem Modus.
    /// Ein gespeicherter Stand aus der Zeit vor der Ratsche trägt „fest" — und das
    /// steht da, statt stumm zu bleiben (Spezifikation 5.1.1, Anwenderentscheid PS‑Q1).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Die_Herkunftszeile_nennt_den_Modus_der_Entladeschwelle(bool adaptiv)
    {
        SpeicherErgebnisDaten daten = Daten();
        daten.FlotteImProjektAktiv = true;
        daten.AktiveFlotte = new FlottenStudieKonfiguration
        {
            Einheiten = new()
            {
                new() { Id = "a", Name = "Hauptspeicher", KapazitaetKWh = 120, LadeleistungKw = 42, EntladeleistungKw = 57 }
            },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving,
                WirtschaftlicherPeakZielwertKw = 200,
                PeakZielAdaptiv = adaptiv
            }
        };

        var seite = Zeichnen(daten);

        string erwartet = adaptiv
            ? WindowsFormsApplication1.MyResource.Resource.FLOTTE_PEAKMODUS_ADAPTIV
            : WindowsFormsApplication1.MyResource.Resource.FLOTTE_PEAKMODUS_FEST;
        Assert.Contains(string.Format(CultureInfo.CurrentCulture,
            WindowsFormsApplication1.MyResource.Resource.SIM_SP_HERKUNFT_PEAK,
            200.0.ToString("N2", CultureInfo.CurrentCulture), erwartet),
            seite.Find("p.epos-simerg-herkunft").TextContent);
    }

    /// <summary>
    /// Der Verweis „Konfiguration ändern → ①" meldet seinen Klick — der Wirt
    /// wechselt daraufhin in Schritt ① (in der Ansicht SIMULATION) bzw. öffnet sie
    /// dort (Startseiten-Reiter). Ohne Delegat gibt es ihn nicht.
    /// </summary>
    [Fact]
    public void Der_Verweis_in_die_Konfiguration_meldet_seinen_Klick()
    {
        SpeicherErgebnisDaten daten = Daten();
        daten.FlotteImProjektAktiv = true;
        daten.AktiveFlotte = new FlottenStudieKonfiguration
        {
            Einheiten = new() { new() { Id = "a", Name = "Speicher 1", KapazitaetKWh = 100 } }
        };

        Assert.Empty(Zeichnen(daten).FindAll("button.epos-simerg-verweis"));

        var seite = Zeichnen(daten, konfigWeg: true);
        seite.Find("button.epos-simerg-verweis").Click();
        Assert.Equal(1, _konfigwechsel);
    }

    /// <summary>
    /// ANWENDERBEFUND #210 (11.09.2026): Der Abschnitt „Kennzahlen je Speicher" zeigte
    /// nur EINE Zeile, obwohl das Projekt ZWEI Speicheranlagen führt. Die Ursache lag
    /// in der Vorbelegung des Kerns (<c>SpeicherFlottenStudieCtrl.Vorbelegung</c>, Wache
    /// in <c>EPOS.Kern.Tests/SpeicherFlottenAnlagenEinheitenTests</c>); hier wird der
    /// ANZEIGEteil festgehalten: Das Ergebnis führt beide Kennzahlzeilen, und die
    /// Herkunftszeile nennt beide Einheiten.
    /// </summary>
    [Fact]
    public void Zwei_Speicheranlagen_stehen_mit_ihren_Kennzahlen_im_Ergebnis()
    {
        var flotte = new FlottenStudieKonfiguration
        {
            Einheiten = new()
            {
                new() { Id = "a", Name = "Speicher Halle", AnlageId = "14935",
                        KapazitaetKWh = 129, LadeleistungKw = 100, EntladeleistungKw = 100 },
                new() { Id = "b", Name = "Speicher Verwaltung", AnlageId = "14936",
                        KapazitaetKWh = 129, LadeleistungKw = 100, EntladeleistungKw = 100 }
            }
        };

        SpeicherErgebnisDaten daten = Daten();
        daten.FlotteImProjektAktiv = true;
        daten.AktiveFlotte = flotte;
        daten.Flottenergebnis = new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Konfiguration = SpeicherAuslegungKopie.Von(flotte),
            Studie = new FlottenStudienErgebnis
            {
                Variante = new FlottenSimulationErgebnis
                {
                    Zulaessig = true,
                    SpeicherKennzahlen = new()
                    {
                        new() { SpeicherId = "a", LadeenergieAcKWh = 1060.67, EntladeenergieAcKWh = 856.70 },
                        new() { SpeicherId = "b", LadeenergieAcKWh = 980.11, EntladeenergieAcKWh = 790.55 }
                    }
                }
            }
        };

        var seite = Zeichnen(daten, konfigWeg: true);

        // Die Herkunftszeile zählt beide Einheiten …
        Assert.Contains(string.Format(CultureInfo.CurrentCulture,
            WindowsFormsApplication1.MyResource.Resource.SIM_SP_HERKUNFT,
            WindowsFormsApplication1.MyResource.Resource.FLOTTE_ZIEL_PVGREEDY, 2),
            seite.Find("p.epos-simerg-herkunft").TextContent);

        // … und das Ergebnis darunter führt beide Kennzahlzeilen.
        var ansicht = seite.FindComponent<SpeicherFlottenErgebnisAnsicht>();
        var kennzahlen = ansicht.FindAll("table.epos-raster")
            .Single(x => (x.QuerySelector("thead")?.TextContent ?? "").Contains(
                WindowsFormsApplication1.MyResource.Resource.FLOTTE_KENN_SP_VOLLZYKLEN,
                StringComparison.Ordinal))
            .QuerySelectorAll("tbody tr");
        Assert.Equal(new[] { "Speicher Halle", "Speicher Verwaltung" },
                     kennzahlen.Select(r => r.QuerySelectorAll("td")[0].TextContent.Trim()).ToArray());
    }

    [Fact]
    public void Zwoelf_Kacheln_stehen_im_Kachelraster()
    {
        var seite = Zeichnen(Daten());
        Assert.Equal(12, seite.FindAll(".epos-kennzahlkachel").Count);
    }

    /// <summary>
    /// W11b‑B‑26: EIN Bild statt des blossen Ladezustands — vorbelegt mit ALLEN vier
    /// Reihen und als Ganglinie (nicht sortiert).
    /// </summary>
    [Fact]
    public void Das_Betriebsbild_wird_angefordert()
    {
        var seite = Zeichnen(Daten());

        Assert.Contains(_auftraege, a => a.Bild == Bilder.SpeicherBetrieb);
        Assert.False(Letzter.Sortiert);
        Assert.Equal(new[]
        {
            SpeicherBetriebsbild.REIHE_OHNE,
            SpeicherBetriebsbild.REIHE_MIT,
            SpeicherBetriebsbild.REIHE_SPEICHER,
            SpeicherBetriebsbild.REIHE_SOC
        }, seite.Instance.GewaehlteReihen);

        // Das SoC-Bild geht darin auf - es wird nicht daneben noch einmal angefordert.
        Assert.Single(seite.FindComponents<DiagrammSvg>());
    }

    /// <summary>
    /// Der Umschalter „sortiert" des Bedarfsreiters, hier fuer das eine Bild: Er
    /// wechselt NUR den Bildauftrag — dieselbe Schalterstellung, derselbe Schluessel.
    /// </summary>
    [Fact]
    public void Der_Schalter_sortiert_wechselt_den_Bildauftrag()
    {
        var seite = Zeichnen(Daten());

        Assert.Single(seite.FindAll("label.epos-schalter"),
                      l => l.TextContent.Trim() == "sortiert");

        _auftraege.Clear();
        Bildschalter(seite)[0].Change(true);

        Assert.True(seite.Instance.Sortiert);
        Assert.True(Letzter.Sortiert);
    }

    /// <summary>
    /// Je Reihe ein Schalter, beschriftet mit DERSELBEN Ressource wie die Legende
    /// (Doku_Simulationsergebnis_Darstellung.md, 5). Eine abgewaehlte Reihe faellt aus
    /// dem Auftrag — und eine LEERE Liste ist etwas anderes als keine Angabe.
    /// </summary>
    [Fact]
    public void Die_Reihenschalter_stehen_im_Bildauftrag()
    {
        var seite = Zeichnen(Daten());

        // [0] sortiert, [1..4] die vier Reihen - mehr Kaestchen hat die Diagrammzeile
        // nicht. Der Parameterblock darueber bringt eigene mit (W11b-B-28); sie
        // gehoeren nicht zu diesem Fall und stehen deshalb ausserhalb des Selektors.
        var kaesten = Bildschalter(seite);
        Assert.Equal(5, kaesten.Count);

        var namen = seite.FindAll("label.epos-schalter").Select(l => l.TextContent.Trim()).ToArray();
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.OPT_BETRIEB_R_OHNE, namen);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.PEAK_CHART_Y2, namen);

        _auftraege.Clear();
        Bildschalter(seite)[4].Change(false);   // der Ladezustand geht weg

        Assert.DoesNotContain(SpeicherBetriebsbild.REIHE_SOC, seite.Instance.GewaehlteReihen);
        Assert.DoesNotContain(SpeicherBetriebsbild.REIHE_SOC, Letzter.Reihen!);
        Assert.Equal(3, Letzter.Reihen!.Count);
    }

    /// <summary>
    /// Alle vier abgewaehlt: Der Auftrag traegt eine LEERE Liste, nicht <c>null</c> —
    /// der Renderer zeichnet dann seinen Leerhinweis (Hausregel der Ergebnisseite).
    /// </summary>
    [Fact]
    public void Alles_abgewaehlt_ist_nicht_dasselbe_wie_keine_Angabe()
    {
        var seite = Zeichnen(Daten());

        for (int i = 1; i <= 4; i++) Bildschalter(seite)[i].Change(false);

        Assert.NotNull(Letzter.Reihen);
        Assert.Empty(Letzter.Reihen!);
    }

    /// <summary>
    /// „Kein Delegat ist kein Knopf": Ohne Bilddelegat gaebe es nichts neu zu zeichnen —
    /// dann stehen weder der Umschalter noch die Reihenwahl da.
    /// </summary>
    [Fact]
    public void Ohne_Bilddelegat_bleiben_die_Schalter_weg()
    {
        var seite = Zeichnen(Daten(), mitBild: false);

        Assert.Empty(Bildschalter(seite));
        Assert.Empty(_auftraege);
    }

    /// <summary>Drei Gruppen mit je einer Ueberschriftszeile.</summary>
    [Fact]
    public void Die_Kennzahlen_stehen_in_drei_Gruppen()
    {
        var seite = Zeichnen(Daten());

        Assert.Equal(3, seite.FindAll("table.epos-simerg-kennzahlen tbody").Count);
        Assert.Contains("Energie", seite.Markup);
        Assert.Contains("Wirtschaft", seite.Markup);
    }

    /// <summary>
    /// Die Warnstufe faerbt die Zeile - dieselben Werte, die <c>SpWarnfarbe</c> als
    /// <c>Color.FromArgb</c> setzte.
    ///
    /// <para><b>Ausser „unbestimmt“</b> (Anwenderwunsch 08.09.2026, W11b‑B‑14):
    /// Sie ist die Vorgabe von <c>KennzahlStufe</c> und damit die Stufe von 37 der 39
    /// Zeilen; die Klasse legte Grau unter die GANZE Tabelle, und die drei Warnfarben
    /// gingen darin unter. Eine Zeile ohne Aussage traegt jetzt gar keine Klasse.</para>
    /// </summary>
    [Fact]
    public void Die_Warnstufe_faerbt_die_Zeile()
    {
        var seite = Zeichnen(Daten());

        Assert.Single(seite.FindAll("tr.epos-stufe-knapp"));
        Assert.Single(seite.FindAll("tr.epos-stufe-ok"));
        Assert.Empty(seite.FindAll("tr.epos-stufe-unbestimmt"));
        Assert.DoesNotContain("epos-stufe-unbestimmt", seite.Markup);
    }

    /// <summary>Ohne Vergleichslauf gibt es die Vergleichsspalte gar nicht.</summary>
    [Fact]
    public void Die_Vergleichsspalte_haengt_am_Vergleichslauf()
    {
        Assert.Equal(3, Zeichnen(Daten()).FindAll("table.epos-simerg-kennzahlen thead th").Count);
        Assert.Equal(4, Zeichnen(Daten(vergleich: true))
                        .FindAll("table.epos-simerg-kennzahlen thead th").Count);
    }

    /// <summary>Vergleichen laesst sich erst ab zwei Varianten (Fachkonzept 7.3).</summary>
    [Fact]
    public void Der_Vergleichsknopf_erscheint_erst_ab_zwei_Varianten()
    {
        var eine = Zeichnen(Daten(mehrere: false), csv: () => { }, vergleich: () => { });
        Assert.Single(Bildknoepfe(eine));

        var zwei = Zeichnen(Daten(), csv: () => { }, vergleich: () => { });
        Assert.Equal(2, Bildknoepfe(zwei).Count);
    }

    /// <summary>Die Warnzeile eines Laufs ohne jede Erzeugung (Abnahmebefund 2).</summary>
    [Fact]
    public void Ein_Lauf_ohne_Erzeugung_bekommt_seine_Warnzeile()
    {
        Assert.Empty(Zeichnen(Daten()).FindAll("[role='alert']"));
        Assert.Single(Zeichnen(Daten(hinweis: "Der Lauf führte keine Erzeugung."))
                      .FindAll("[role='alert']"));
    }

    [Fact]
    public void Die_beiden_Knoepfe_melden_ihren_Klick()
    {
        int csv = 0, vgl = 0;
        var seite = Zeichnen(Daten(), () => csv++, () => vgl++);

        var knoepfe = Bildknoepfe(seite);
        knoepfe[0].Click();
        knoepfe[1].Click();

        Assert.Equal(1, csv);
        Assert.Equal(1, vgl);
    }

    // =====================================================================
    // Anwenderwunsch 08.09.2026, W11b-B-14: der gegliederte Wirtschaftsblock
    // =====================================================================

    private const string UG_JAHR = "Referenzjahr";
    private const string UG_DAUER = "\u00dcber die Nutzungsdauer";
    private const string UG_NACH = "Nachrichtlich";

    /// <summary>
    /// Ein Wirtschaftsblock, wie ihn der Kern seit W11b-B-14 liefert: drei
    /// Unterabschnitte, eine Summe, ein Ergebnis, eine nachrichtliche Zeile.
    /// </summary>
    private static SpeicherErgebnisDaten Gegliedert()
    {
        var d = Daten();
        d.Kennzahlen = new[]
        {
            Z(SpeicherKennzahlenBlock.GRUPPE_ENERGIE, "Last", "12.000"),
            Z(SpeicherKennzahlenBlock.GRUPPE_WIRTSCHAFT, "Ertrag: vermiedener Netzbezug",
              "244,88", untergruppe: UG_JAHR),
            Z(SpeicherKennzahlenBlock.GRUPPE_WIRTSCHAFT, "Kosten: Netzladung",
              "-12,00", untergruppe: UG_JAHR),
            Z(SpeicherKennzahlenBlock.GRUPPE_WIRTSCHAFT, "Ertrag Referenzjahr E_a,1",
              "177,29", untergruppe: UG_JAHR, art: KennzahlArt.Summe,
              hinweis: "E_a,1 \u2014 Ertrag des Referenzjahrs"),
            Z(SpeicherKennzahlenBlock.GRUPPE_WIRTSCHAFT, "Investition I",
              "0,00", untergruppe: UG_DAUER),
            Z(SpeicherKennzahlenBlock.GRUPPE_WIRTSCHAFT, "Kapitalwert (NPV)",
              "2.604,13", untergruppe: UG_DAUER, art: KennzahlArt.Ergebnis),
            Z(SpeicherKennzahlenBlock.GRUPPE_WIRTSCHAFT, "Betriebskosten: Verschlei\u00df K_ver",
              "40,33", untergruppe: UG_NACH, art: KennzahlArt.Nachrichtlich)
        };
        return d;
    }

    /// <summary>
    /// Jeder Unterabschnitt bekommt GENAU EINE Zwischenueberschrift - auch wenn
    /// mehrere Zeilen dieselbe Untergruppe tragen.
    /// </summary>
    [Fact]
    public void Jeder_Unterabschnitt_bekommt_eine_Zwischenueberschrift()
    {
        var seite = Zeichnen(Gegliedert());

        var koepfe = seite.FindAll("tr.epos-simerg-untergruppe th");
        Assert.Equal(3, koepfe.Count);
        Assert.Equal(UG_JAHR, koepfe[0].TextContent.Trim());
        Assert.Equal(UG_DAUER, koepfe[1].TextContent.Trim());
        Assert.Equal(UG_NACH, koepfe[2].TextContent.Trim());

        // Ohne Untergruppe keine Ueberschrift: Die Energiezeile bekommt keine.
        Assert.Equal(3, seite.FindAll("table.epos-simerg-kennzahlen tbody").Count);
    }

    /// <summary>
    /// Summe, Ergebnis und Nachrichtliches tragen ihre Klasse. WAS sie sind, sagt der
    /// Kern (<c>KennzahlArt</c>) - hier steht nur, dass die Klasse ankommt.
    /// </summary>
    [Fact]
    public void Summe_Ergebnis_und_Nachrichtliches_tragen_ihre_Klasse()
    {
        var seite = Zeichnen(Gegliedert());

        Assert.Single(seite.FindAll("tr.epos-simerg-summe"));
        Assert.Single(seite.FindAll("tr.epos-simerg-ergebnis"));
        Assert.Single(seite.FindAll("tr.epos-simerg-nachrichtlich"));

        Assert.Contains("Ertrag Referenzjahr", seite.Find("tr.epos-simerg-summe").TextContent);
        Assert.Contains("NPV", seite.Find("tr.epos-simerg-ergebnis").TextContent);
    }

    /// <summary>Der Werkzeugtipp des Kerns steht als <c>title</c> an seiner Zeile.</summary>
    [Fact]
    public void Der_Werkzeugtipp_steht_an_seiner_Zeile()
    {
        var seite = Zeichnen(Gegliedert());

        Assert.Equal("E_a,1 \u2014 Ertrag des Referenzjahrs",
                     seite.Find("tr.epos-simerg-summe").GetAttribute("title"));

        // Eine Zeile ohne Hinweis bekommt gar kein Attribut - nicht title="".
        var ohne = seite.FindAll("tr.epos-simerg-nachrichtlich")[0];
        Assert.Null(ohne.GetAttribute("title"));
    }

    /// <summary>
    /// Die Zyklenampel gehoert unter die Gruppe SPEICHER, ueber die sie etwas sagt -
    /// und nicht als loser Absatz unter die ganze Tabelle, also unter die Wirtschaft.
    /// </summary>
    [Fact]
    public void Die_Zyklenampel_steht_unter_der_Gruppe_Speicher()
    {
        var seite = Zeichnen(Daten());

        var ampel = seite.Find("tr.epos-simerg-ampelzeile");
        Assert.Contains("180 von 250 Zyklen", ampel.TextContent);

        var koerper = seite.FindAll("table.epos-simerg-kennzahlen tbody");
        Assert.Contains("epos-simerg-ampelzeile", koerper[1].InnerHtml);
        Assert.DoesNotContain("epos-simerg-ampelzeile", koerper[0].InnerHtml);
        Assert.DoesNotContain("epos-simerg-ampelzeile", koerper[2].InnerHtml);

        // Kein loser Absatz mehr. Der Parameterblock hat eigene Hinweisabsaetze
        // (W11b-B-28) - gemeint ist die Spaltenzeile mit Bild und Tabelle.
        Assert.Empty(seite.FindAll("div.epos-simerg-spalten p.epos-simerg-hinweis"));
    }

    /// <summary>Ohne Ampeltext gibt es die Zeile gar nicht.</summary>
    [Fact]
    public void Ohne_Ampeltext_bleibt_die_Ampelzeile_weg()
    {
        var d = Daten();
        d.Ampel = "";

        Assert.Empty(Zeichnen(d).FindAll("tr.epos-simerg-ampelzeile"));
    }

    /// <summary>
    /// Die Kennzahlenliste steht ueber die GANZE Zeile des Spaltenrasters (W11b-B-14).
    /// In einer Spalte war sie auf die Mindestbreite von 320 Bildpunkten gequetscht.
    /// </summary>
    [Fact]
    public void Die_Kennzahlenliste_steht_ueber_die_ganze_Zeile()
    {
        var seite = Zeichnen(Daten());

        var abschnitt = seite.Find("section.epos-simerg-kennzahlenzeile");
        Assert.NotNull(abschnitt.QuerySelector("table.epos-simerg-kennzahlen"));
    }

    // =====================================================================
    //  DER BAUSTEIN DiagrammSvg (Etappe DG-E3, Gruppe (a))
    //
    //  Gerade dieses Bild braucht den Ausschnitt: Ein Speicher laedt und
    //  entlaedt im TAGESrhythmus, und in der Vollansicht liegen rund
    //  40 Viertelstunden auf einem Bildpunkt. Der Ausschnitt ist seither die
    //  viewBox der Zeichenflaeche - kein Rundlauf in den Kern (DG-E3-9).
    // =====================================================================

    /// <summary>
    /// <b>„Lastgang und Speicherbetrieb" steht als SVG im Baum</b> — unter der
    /// Kennung <c>simerg-speicherbetrieb</c>, mit der Einheit der linken und der
    /// rechten Achse, und ohne ein Pixelbild daneben.
    /// </summary>
    [Fact]
    public void Das_Betriebsbild_steht_als_DiagrammSvg()
    {
        var seite = Zeichnen(Daten());
        DiagrammSvg bild = seite.FindComponent<DiagrammSvg>().Instance;

        Assert.Single(seite.FindComponents<DiagrammSvg>());
        Assert.Equal("simerg-speicherbetrieb", bild.Kennung);
        Assert.Equal("kW", bild.Einheit);
        Assert.Equal("kWh", bild.EinheitRechts);
        Assert.Empty(seite.FindComponents<ChartBild>());
    }

    /// <summary>
    /// Der Zoom ist Bedienung am Bild: Über ihm stehen „Bereich" und „1:1", und der
    /// Schalter „sortiert" stellt die Achsenart auf den RANG um.
    /// </summary>
    [Fact]
    public void Das_Betriebsbild_traegt_den_Bereichsknopf()
    {
        var seite = Zeichnen(Daten());

        Assert.Equal(new[] { "Bereich", "1:1" },
                     seite.FindComponent<DiagrammSvg>()
                          .FindAll("button.epos-diagramm-knopf")
                          .Select(k => k.TextContent.Trim()).ToArray());
    }
}
