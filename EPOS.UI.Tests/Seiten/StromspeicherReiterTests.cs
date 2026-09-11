using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
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
                                                             bool optimierung = false,
                                                             SimulationErgebnisDienste? dienste = null)
        => Render<StromspeicherReiter>(p =>
        {
            p.Add(x => x.Daten, daten);
            p.Add(x => x.Parameter, parameter ?? new SpeicherParameterDaten());
            if (dienste is not null) p.Add(x => x.Dienste, dienste);
            if (optimierung)
            {
                p.Add(x => x.OptimierungMoeglich, true);
                p.Add(x => x.Optimierung, EventCallback.Factory.Create(this, () => _optimierungen++));
            }
            if (mitBild) p.Add(x => x.Bild, a => { _auftraege.Add(a); return new byte[] { 1 }; });
            if (csv is not null) p.Add(x => x.Csv, EventCallback.Factory.Create(this, csv));
            if (vergleich is not null) p.Add(x => x.Vergleich, EventCallback.Factory.Create(this, vergleich));
        });

    private int _optimierungen;

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
    /// Der alte Optimierungsknopf im Einzelparameterblock ist entfallen. Der gemeinsame
    /// Einstieg für Einzel- und Mehrspeicherauslegung steht in einer eigenen Karte.
    /// </summary>
    [Fact]
    public void Der_alte_Optimierungsknopf_bleibt_im_Parameterblock_ausgeblendet()
    {
        Assert.DoesNotContain(Zeichnen(Daten()).FindAll("button"),
                              b => b.TextContent.Contains("optimieren"));

        var mit = Zeichnen(Daten(), optimierung: true, dienste: Flottendienste());
        Assert.Empty(mit.FindComponents<SpeicherParameterBlock>());
    }

    [Fact]
    public void Flottendienst_zeigt_den_gemeinsamen_Einstieg_auch_im_Einzelbetrieb()
    {
        var dienste = Flottendienste();
        var seite = Zeichnen(Daten(), optimierung: true, dienste: dienste);

        Assert.Contains("Speicherflotte und Auslegung", seite.Markup);
        Assert.Contains("Eingabestand @Aktuell", seite.Markup);
        Assert.Empty(seite.FindComponents<SpeicherParameterBlock>());
        Assert.Single(seite.FindComponents<SpeicherFlottenBetriebEditor>());
        var knopf = seite.FindAll("button").Single(b => b.TextContent.Trim() == "Speicherflotte & Auslegung öffnen");
        knopf.Click();
        Assert.Equal(1, _optimierungen);
        Assert.Contains("Last EPOS-Projektreihe", seite.Markup);
        Assert.Contains("Kosten Investition direkte Eingabe", seite.Markup);
    }

    [Fact]
    public void Aktivierte_Flotte_ersetzt_Altkopf_und_Einzelparameter()
    {
        SpeicherErgebnisDaten daten = Daten();
        daten.FlotteImProjektAktiv = true;
        daten.Kopf = "Growatt · Grünstrom · Dauernutzung";
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

        var seite = Zeichnen(daten, optimierung: true, dienste: Flottendienste(daten.AktiveFlotte));

        Assert.Contains("Mehrspeicherbetrieb aktiviert", seite.Markup);
        Assert.DoesNotContain("Growatt · Grünstrom · Dauernutzung", seite.Markup);
        Assert.Empty(seite.FindComponents<SpeicherParameterBlock>());
        Assert.Single(seite.FindAll("button"), b => b.TextContent.Trim() == "Speicherflotte & Auslegung öffnen");
        Assert.Contains("Multi Use", seite.Markup);
        Assert.Contains("Grenzkosten", seite.Markup);
        Assert.Equal(88, seite.FindComponent<SpeicherFlottenBetriebEditor>()
            .Instance.Wert.WirtschaftlicherPeakZielwertKw);
        Assert.Contains("Hauptspeicher", seite.Markup);
        Assert.Contains("Schnellspeicher", seite.Markup);
        Assert.Contains("120,00", seite.Markup);
        Assert.Contains("Flotte geändert: Projektsimulation neu berechnen. Angezeigte Ergebnisse gehören noch zum vorherigen Lauf.", seite.Markup);
    }

    /// <summary>
    /// ANWENDERBEFUND #210 (11.09.2026): Der Eingabestand <c>@Aktuell</c> führte EINE
    /// Einheit, obwohl das Projekt ZWEI Speicheranlagen hat — und der Abschnitt
    /// „Kennzahlen je Speicher" zeigte dieselbe eine. Die Ursache lag in der Vorbelegung
    /// des Kerns (<c>SpeicherFlottenStudieCtrl.Vorbelegung</c>, Wache in
    /// <c>EPOS.Kern.Tests/SpeicherFlottenAnlagenEinheitenTests</c>); hier wird der
    /// ANZEIGEteil festgehalten: Beide Einheiten stehen in der Eingabetabelle, beide mit
    /// ihrer HERKUNFT, und das Ergebnis führt beide Kennzahlzeilen.
    /// </summary>
    [Fact]
    public void Zwei_Speicheranlagen_stehen_mit_Herkunft_im_Eingabestand_und_im_Ergebnis()
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

        var seite = Zeichnen(daten, optimierung: true, dienste: Flottendienste(flotte));

        // Der Eingabestand: zwei Zeilen, jede mit ihrer Projektanlage.
        var eingabe = seite.FindAll("table.epos-tabelle").First();
        Assert.Equal("Herkunft", eingabe.QuerySelectorAll("thead th")[1].TextContent.Trim());
        var zeilen = eingabe.QuerySelectorAll("tbody tr");
        Assert.Equal(2, zeilen.Length);
        Assert.Equal(new[] { "Speicher Halle", "Speicher Verwaltung" },
                     zeilen.Select(r => r.QuerySelectorAll("td")[0].TextContent.Trim()).ToArray());
        Assert.Equal(new[] { "Projektanlage 14935", "Projektanlage 14936" },
                     zeilen.Select(r => r.QuerySelectorAll("td")[1].TextContent.Trim()).ToArray());

        // Und das Ergebnis darunter führt beide Kennzahlzeilen.
        var ansicht = seite.FindComponent<SpeicherFlottenErgebnisAnsicht>();
        var kennzahlen = ansicht.FindAll("table.epos-raster")
            .Single(t => (t.QuerySelector("thead")?.TextContent ?? "").Contains(
                WindowsFormsApplication1.MyResource.Resource.FLOTTE_KENN_SP_VOLLZYKLEN,
                StringComparison.Ordinal))
            .QuerySelectorAll("tbody tr");
        Assert.Equal(new[] { "Speicher Halle", "Speicher Verwaltung" },
                     kennzahlen.Select(r => r.QuerySelectorAll("td")[0].TextContent.Trim()).ToArray());
    }

    /// <summary>Eine von Hand angelegte Einheit nennt sich als solche.</summary>
    [Fact]
    public void Eine_Einheit_ohne_Anlagenbezug_heisst_nur_im_Eingabestand()
    {
        var flotte = new FlottenStudieKonfiguration
        {
            Einheiten = new() { new() { Id = "x", Name = "Zusatzspeicher", KapazitaetKWh = 50 } }
        };
        SpeicherErgebnisDaten daten = Daten();
        daten.FlotteImProjektAktiv = true;
        daten.AktiveFlotte = flotte;

        var seite = Zeichnen(daten, optimierung: true, dienste: Flottendienste(flotte));

        Assert.Equal("nur im Eingabestand", seite.FindAll("table.epos-tabelle").First()
            .QuerySelectorAll("tbody tr td")[1].TextContent.Trim());
    }

    /// <summary>
    /// Auftrag #170c: Der Reiter zeigt DENSELBEN Baustein wie der Dialog — also gilt die
    /// Planersperre auch hier. Ohne Fahrplan-Löser stehen die drei planenden Ziele gesperrt
    /// in der Liste und nennen den Grund.
    /// </summary>
    [Fact]
    public void Ohne_Planer_sind_die_planenden_Ziele_auch_im_Reiter_gesperrt()
    {
        var seite = Render<StromspeicherReiter>(p => p
            .Add(x => x.Daten, Daten())
            .Add(x => x.Parameter, new SpeicherParameterDaten())
            .Add(x => x.Dienste, Flottendienste())
            .Add(x => x.PlanerVerfuegbar, false));

        var ziele = seite.FindAll("label").First(x => x.TextContent.Contains("Betriebsziel:"))
                         .QuerySelector("select")!.QuerySelectorAll("option").ToArray();
        Assert.Equal(new[] { "2", "3", "4" },
            ziele.Where(x => x.HasAttribute("disabled")).Select(x => x.GetAttribute("value")).ToArray());
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.FLOTTE_PLANER_FEHLT, seite.Markup);
    }

    /// <summary>
    /// Ein GESPEICHERTES planendes Profil erklärt sich im Reiter mit einem Banner, statt den
    /// Anwender erst im Projektlauf gegen die Ausnahme des Kerns laufen zu lassen.
    /// </summary>
    [Fact]
    public void Gespeichertes_planendes_Profil_erklaert_sich_im_Reiter()
    {
        var flotte = new FlottenStudieKonfiguration
        {
            Einheiten = new() { new() { Id = "s1", Name = "Speicher 1", KapazitaetKWh = 100 } },
            Optionen = new FlottenSimulationOptionen { Betriebsziel = FlottenBetriebsziel.PvPlanung }
        };

        var seite = Render<StromspeicherReiter>(p => p
            .Add(x => x.Daten, Daten())
            .Add(x => x.Parameter, new SpeicherParameterDaten())
            .Add(x => x.Dienste, Flottendienste(flotte))
            .Add(x => x.PlanerVerfuegbar, false));

        Assert.Contains(string.Format(WindowsFormsApplication1.MyResource.Resource.FLOTTE_PLANER_PROFIL,
            "PV-Prognoseplanung"), seite.Markup);
    }

    [Fact]
    public void Mit_Planer_bleibt_die_Zielliste_im_Reiter_vollstaendig_bedienbar()
    {
        var seite = Render<StromspeicherReiter>(p => p
            .Add(x => x.Daten, Daten())
            .Add(x => x.Parameter, new SpeicherParameterDaten())
            .Add(x => x.Dienste, Flottendienste())
            .Add(x => x.PlanerVerfuegbar, true));

        var ziele = seite.FindAll("label").First(x => x.TextContent.Contains("Betriebsziel:"))
                         .QuerySelector("select")!.QuerySelectorAll("option").ToArray();
        Assert.DoesNotContain(ziele, x => x.HasAttribute("disabled"));
        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.FLOTTE_PLANER_FEHLT, seite.Markup);
    }

    [Fact]
    public void Ohne_vollstaendigen_Flottendienst_bleibt_der_Legacyblock_erhalten()
    {
        var unvollstaendig = new SimulationErgebnisDienste
        {
            OptimierungFlottenRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis())
        };

        Assert.Single(Zeichnen(Daten(), dienste: unvollstaendig)
            .FindComponents<SpeicherParameterBlock>());
    }

    [Fact]
    public void Ausdrueckliche_Deaktivierung_bleibt_im_erhaltenen_Aktuellstand_sichtbar()
    {
        SpeicherOptimierungEingaben eingaben = FlottenEingaben();
        eingaben.Auslegung.FlottenProjektbetriebDeaktiviert = true;
        var dienste = new SimulationErgebnisDienste
        {
            OptimierungFlottenRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis()),
            OptimierungVorgaben = () => new SpeicherOptimierungVorgaben { Eingaben = eingaben.Kopie() },
            OptimierungEinstellungenSpeichern = _ => Task.FromResult("")
        };

        Assert.Contains("Projektflottenbetrieb ausdrücklich deaktiviert",
            Zeichnen(Daten(), dienste: dienste).Markup);
    }

    [Fact]
    public async Task Betriebsoptionen_werden_im_Aktuellstand_gespeichert_und_frisch_gelesen()
    {
        SpeicherOptimierungEingaben stand = FlottenEingaben();
        SpeicherOptimierungEingaben? gespeichert = null;
        var dienste = new SimulationErgebnisDienste
        {
            OptimierungFlottenRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis()),
            OptimierungVorgaben = () => new SpeicherOptimierungVorgaben { Eingaben = stand.Kopie() },
            OptimierungEinstellungenSpeichern = e =>
            {
                gespeichert = e.Kopie();
                stand = e.Kopie();
                stand.Auslegung.Revision++;
                return Task.FromResult("");
            }
        };
        var seite = Zeichnen(Daten(), dienste: dienste);
        var optionen = SpeicherAuslegungKopie.Von(
            seite.FindComponent<SpeicherFlottenBetriebEditor>().Instance.Wert);
        optionen.Betriebsziel = FlottenBetriebsziel.MultiUse;
        optionen.Verteilung = FlottenVerteilung.Grenzkosten;
        optionen.ErzeugerPrioritaet = FlottenErzeugerPrioritaet.BhkwVorPv;
        optionen.WirtschaftlicherPeakZielwertKw = 73;
        optionen.NetzladungErlaubt = true;
        optionen.BatterieexportErlaubt = true;

        await seite.InvokeAsync(() => seite.FindComponent<SpeicherFlottenBetriebEditor>()
            .Instance.WertChanged.InvokeAsync(optionen));

        Assert.NotNull(gespeichert);
        Assert.Equal(FlottenBetriebsziel.MultiUse, gespeichert!.Auslegung.Flotte.Optionen.Betriebsziel);
        Assert.Equal(FlottenVerteilung.Grenzkosten, gespeichert.Auslegung.Flotte.Optionen.Verteilung);
        Assert.Equal(FlottenErzeugerPrioritaet.BhkwVorPv, gespeichert.Auslegung.Flotte.Optionen.ErzeugerPrioritaet);
        Assert.Equal(73, gespeichert.Auslegung.Flotte.Optionen.WirtschaftlicherPeakZielwertKw);
        Assert.True(gespeichert.Auslegung.Flotte.Optionen.NetzladungErlaubt);
        Assert.True(gespeichert.Auslegung.Flotte.Optionen.BatterieexportErlaubt);
        Assert.Contains("Revision 8", seite.Markup);
        Assert.Contains("Flotte geändert", seite.Markup);
    }

    private static SimulationErgebnisDienste Flottendienste(FlottenStudieKonfiguration? flotte = null)
    {
        SpeicherOptimierungEingaben eingaben = FlottenEingaben(flotte);
        return new SimulationErgebnisDienste
        {
            OptimierungFlottenRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis()),
            OptimierungVorgaben = () => new SpeicherOptimierungVorgaben { Eingaben = eingaben.Kopie() },
            OptimierungEinstellungenSpeichern = _ => Task.FromResult("")
        };
    }

    private static SpeicherOptimierungEingaben FlottenEingaben(FlottenStudieKonfiguration? flotte = null)
        => new()
        {
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                Flotte = SpeicherAuslegungKopie.Von(flotte) ?? new FlottenStudieKonfiguration
                {
                    Einheiten = new()
                    {
                        new() { Id = "s1", Name = "Speicher 1", KapazitaetKWh = 100,
                                LadeleistungKw = 40, EntladeleistungKw = 50 }
                    }
                },
                Lastquelle = SpeicherAuslegungQuelle.Epos,
                PvQuelle = SpeicherAuslegungQuelle.Epos,
                Preisquelle = SpeicherAuslegungQuelle.Epos,
                Investitionsquelle = SpeicherKostenQuelle.Dialog,
                Betriebsquelle = SpeicherKostenQuelle.Dialog,
                Revision = 7
            }
        };

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
        Assert.Single(seite.FindAll("img"));
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
    //  W11b‑B‑24 — DER DATENZOOM AM BILD DES REITERS
    //  (Anwenderentscheid 09.09.2026; seit W11b‑B‑26 traegt ihn das Bild
    //  „Lastgang und Speicherbetrieb", in dem das SoC-Bild aufgegangen ist)
    // =====================================================================

    /// <summary>
    /// Das aufgezogene Rechteck geht UNVERÄNDERT in den Bildauftrag, „1:1" nimmt es
    /// zurück. Gerade dieses Bild braucht den Ausschnitt: Ein Speicher lädt und
    /// entlädt im TAGESrhythmus, und in der Vollansicht liegen rund 40 Viertelstunden
    /// auf einem Bildpunkt.
    /// </summary>
    [Fact]
    public async Task Das_Betriebsbild_traegt_den_aufgezogenen_Bereich()
    {
        var seite = Zeichnen(Daten());
        EPOS.UI.Bausteine.Diagramm rahmen =
            seite.FindComponent<EPOS.UI.Bausteine.Diagramm>().Instance;

        await seite.InvokeAsync(() => rahmen.BereichGemeldet(0.25, 0.5, 0.1, 0.9));

        Assert.Equal(0.25, seite.Instance.Bereich!.XVon);
        Assert.Equal(0.5, Letzter.Bereich!.XBis);

        seite.FindComponent<EPOS.UI.Bausteine.Diagramm>()
             .FindAll("button.epos-diagramm-knopf")
             .First(k => k.TextContent.Trim() == "1:1").Click();

        Assert.Null(seite.Instance.Bereich);
        Assert.Null(Letzter.Bereich);
    }

    /// <summary>
    /// Sichtbar wird der Datenzoom am Umschalter „Bereich" — kein Rückruf, kein
    /// Knopf. Vor W11b‑B‑24 stand an diesem Bild nur „1:1".
    /// </summary>
    [Fact]
    public void Das_Betriebsbild_traegt_den_Bereichsknopf()
    {
        var seite = Zeichnen(Daten());

        Assert.Equal(new[] { "Bereich", "1:1" },
                     seite.FindComponent<EPOS.UI.Bausteine.Diagramm>()
                          .FindAll("button.epos-diagramm-knopf")
                          .Select(k => k.TextContent.Trim()).ToArray());
    }
}
