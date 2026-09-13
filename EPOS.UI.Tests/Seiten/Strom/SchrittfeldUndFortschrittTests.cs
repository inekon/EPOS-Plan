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
/// WINDOWS-ABNAHME 13.09.2026 — die zwei Befunde der Station 4 „Optimierung".
///
/// <para><b>Befund 1: „Fehler in Eingabefeld ‚Kapazität Schritt': 1 bleibt stehen,
/// Eingabe nicht korrekt möglich."</b> Wer die „10" im Schrittfeld rückwärts löscht,
/// meldet erst „1" — der Suchraum nimmt sie — und dann die LEERE Eingabe. Ein leeres
/// Feld meldet <c>null</c>; der Suchraum übernahm <c>null</c> nicht, behielt seine 1,
/// und das Feld schrieb sie in die Anzeige zurück. Die 1 ließ sich nicht mehr
/// entfernen, jedes weitere Zeichen landete dahinter. Seither bleibt ein geleertes Feld
/// leer (<c>Zahlenfeld</c>), und die leere Eingabe ist im Suchraum die 0 — also ein
/// benannt ungültiges Raster, das den Lauf sperrt, statt ihn mit einer Zahl rechnen zu
/// lassen, die niemand mehr sieht.</para>
///
/// <para><b>Befund 2: „Progress bar bei Berechnung nicht mehr vorhanden."</b> Der
/// Fortschrittsbalken der Ansicht steht über der Ablaufleiste, ganz oben; der
/// Rechenknopf steht in Station 4, am Ende einer langen Seite. Wer ihn drückt, hat den
/// Balken nicht im Bild. Seither zeichnet die Station ihn unter ihrem Knopf, und die
/// Ansicht lässt ihren eigenen dann weg — EIN Lauf, EIN Fortschritt (Muster #220).</para>
/// </summary>
public sealed class SchrittfeldUndFortschrittTests : EposBunitContext
{
    /// <summary>Die Ansicht trägt einen <c>InfoKnopf</c> und braucht deshalb den Hilfedienst.</summary>
    public SchrittfeldUndFortschrittTests()
        => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    // =====================================================================
    //  1. Das Schrittfeld
    // =====================================================================

    /// <summary>
    /// <b>Die Eingabefolge des Anwenders kommt vollständig im Modell an</b>, und das Feld
    /// zeigt nach JEDEM Zeichen das Getippte — auch das Nichts.
    /// </summary>
    [Fact]
    public void Die_Eingabefolge_im_Schrittfeld_kommt_vollstaendig_im_Modell_an()
    {
        var cut = Station();
        Assert.Equal("10", Schrittfeld(cut).GetAttribute("value"));

        // „10" rueckwaerts geloescht: erst die 1 …
        Schrittfeld(cut).Input("1");
        Assert.Equal("1", Schrittfeld(cut).GetAttribute("value"));
        Assert.Equal(1.0, Achse(cut).KapazitaetSchrittKWh);

        // … dann ganz leer. DAS Feld bleibt leer (der Befund: hier blieb die 1 stehen).
        Schrittfeld(cut).Input("");
        Assert.Equal("", Schrittfeld(cut).GetAttribute("value"));

        // Ein zweites Loeschen aendert daran nichts.
        Schrittfeld(cut).Input("");
        Assert.Equal("", Schrittfeld(cut).GetAttribute("value"));

        // Und jetzt die neue Schrittweite, Zeichen fuer Zeichen.
        foreach ((string getippt, double erwartet) in new[]
                 { ("1", 1.0), ("10", 10.0), ("100", 100.0), ("1000", 1000.0) })
        {
            Schrittfeld(cut).Input(getippt);
            Assert.Equal(getippt, Schrittfeld(cut).GetAttribute("value"));
            Assert.Equal(erwartet, Achse(cut).KapazitaetSchrittKWh);
        }
    }

    /// <summary>
    /// <b>Ein geleertes Schrittfeld ist die 0 und damit ein ungültiges Raster</b> — die
    /// Kandidatenzeile sagt es, und der Rechenknopf ist gesperrt. Der alte Wert bleibt
    /// NICHT unsichtbar im Suchraum stehen.
    /// </summary>
    [Fact]
    public void Ein_geleertes_Schrittfeld_macht_das_Raster_ungueltig_und_sperrt_den_Lauf()
    {
        var cut = Station();

        Schrittfeld(cut).Input("");

        Assert.Equal(0.0, Achse(cut).KapazitaetSchrittKWh);
        Assert.Contains(Resource.FLOTTE_OPT_RASTER_UNGUELTIG,
                        cut.Find("p.epos-flotte-kandidatenzeile").TextContent,
                        StringComparison.Ordinal);
        Assert.True(Auslegungshilfe.Rechenknopf(cut).HasAttribute("disabled"));

        // Die frische Schrittweite gibt den Lauf wieder frei.
        Schrittfeld(cut).Input("1000");
        Assert.False(Auslegungshilfe.Rechenknopf(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Die Zahl des Bildschirmfotos</b>: Kapazität 50…5000 in Schritten von 10 kWh,
    /// Leistung 50…5000 in Schritten von 20 kW — 496 × 249 = 123 504 Grobpunkte, bis zu
    /// 19 im Feinraster, zusammen 123 523 von höchstens 10 000. Anzeige und Vorprüfung
    /// führen dieselbe Zahl, und der Lauf wird abgewiesen.
    /// </summary>
    [Fact]
    public void Die_Kandidatenzeile_der_Abnahme_nennt_dieselbe_Zahl_wie_die_Vorpruefung()
    {
        var cut = Station();

        string zeile = cut.Find("p.epos-flotte-kandidatenzeile").TextContent;
        Assert.Contains("123504", zeile, StringComparison.Ordinal);
        Assert.Contains("19", zeile, StringComparison.Ordinal);
        Assert.Contains("123523", zeile, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_RASTER_ZUVIEL, zeile, StringComparison.Ordinal);

        // Die Vorpruefung der Seite sperrt den Lauf mit derselben Begruendung.
        Assert.True(Auslegungshilfe.Rechenknopf(cut).HasAttribute("disabled"));
        Assert.Contains(Resource.FLOTTE_OPT_RASTER_ZUVIEL, cut.Markup, StringComparison.Ordinal);
    }

    // =====================================================================
    //  2. Der Fortschritt in Station 4
    // =====================================================================

    /// <summary>
    /// <b>Der Balken steht unter dem Rechenknopf</b>, solange gerechnet wird: Anteil und
    /// Schritttext wachsen mit den Meldungen, nach dem Ende verschwindet er — und die
    /// Ansicht zeichnet keinen zweiten.
    /// </summary>
    [Fact]
    public async Task Der_Fortschritt_steht_in_Station_4_unter_dem_Rechenknopf()
    {
        var quelle = new TaskCompletionSource<SpeicherFlottenErgebnis>();
        Action<double?, string>? melder = null;

        var cut = Station(d =>
        {
            d.FlotteRechnen = (_, meldung) => { melder = meldung; return quelle.Task; };
            d.Abbrechen = () => { };
        }, schritt: 1000);

        Assert.Empty(cut.FindAll(".epos-fortschritt"));

        Task klick = Auslegungshilfe.Rechenknopf(cut).ClickAsync(new());

        Assert.True(cut.Instance.Laeuft);
        // GENAU EINER — und er steht im Bedienblock der Station, nicht im Seitenkopf.
        Assert.Single(cut.FindAll(".epos-fortschritt"));
        Assert.Single(cut.FindAll(".epos-flotte-bedienblock .epos-fortschritt"));
        Assert.NotNull(cut.Find(".epos-flotte-bedienblock progress"));
        Assert.Single(cut.FindAll(".epos-flotte-bedienblock button.epos-fortschritt-abbruch"));

        melder!(0.5, "Variante 3 von 6");
        cut.WaitForAssertion(() =>
            Assert.Contains("Variante 3 von 6",
                            cut.Find(".epos-flotte-bedienblock .epos-fortschritt-text").TextContent,
                            StringComparison.Ordinal));
        Assert.Equal("50", cut.Find(".epos-flotte-bedienblock progress").GetAttribute("value"));

        quelle.SetResult(new SpeicherFlottenErgebnis());
        await klick;

        Assert.False(cut.Instance.Laeuft);
        Assert.Empty(cut.FindAll(".epos-fortschritt"));
    }

    /// <summary>
    /// Auf den ANDEREN Blättern bleibt der Balken, wo er war — über der Ablaufleiste.
    /// </summary>
    [Fact]
    public async Task Auf_den_anderen_Blaettern_steht_der_Balken_weiterhin_im_Kopf()
    {
        var quelle = new TaskCompletionSource<SpeicherFlottenErgebnis>();

        var cut = Station(d => d.FlotteRechnen = (_, _) => quelle.Task, schritt: 1000);

        Task klick = Auslegungshilfe.Rechenknopf(cut).ClickAsync(new());
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Speicher);

        Assert.Single(cut.FindAll(".epos-fortschritt"));
        Assert.Empty(cut.FindAll(".epos-flotte-bedienblock .epos-fortschritt"));

        quelle.SetResult(new SpeicherFlottenErgebnis());
        await klick;
    }

    // ================================================================= Prüfstand

    private static FlottenAuslegungsAchse Achse(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen[0];

    private static IElement Schrittfeld(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.Find("article.epos-flotte-einheitskarte")
              .QuerySelectorAll("label.epos-feld")
              .Single(x => x.TextContent.Contains(Resource.FLOTTE_ED_KAPAZITAET_SCHRITT,
                                                  StringComparison.Ordinal))
              .QuerySelector("input")!;

    /// <summary>
    /// Die Ansicht auf Station 4, mit dem Suchraum des Bildschirmfotos: eine Einheit
    /// 4180 kWh / 125 kW / 1 Stück, Kopplung „Kapazität und Leistung", Kapazität 50…5000,
    /// Leistung 50…5000 in Schritten von 20 kW, Feinraster an.
    /// </summary>
    /// <param name="anpassen">Zusätzliche Wege der Ansicht.</param>
    /// <param name="schritt">Die Kapazitäts-Schrittweite; 10 kWh wie im Bildschirmfoto.</param>
    private IRenderedComponent<StromspeicherAuslegungSeite> Station(
        Action<StromspeicherAuslegungDienste>? anpassen = null, double schritt = 10)
    {
        var einheit = new FlottenEinheit
        {
            Id = "s1", Name = "Speicher 1", KapazitaetKWh = 4180,
            LadeleistungKw = 125, EntladeleistungKw = 125,
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
                Feinraster = true,
                Achsen = new List<FlottenAuslegungsAchse>
                {
                    new()
                    {
                        Aktiv = true, Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                        AnzahlVon = 1, AnzahlBis = 1,
                        KapazitaetVonKWh = 50, KapazitaetBisKWh = 5000, KapazitaetSchrittKWh = schritt,
                        LeistungVonKw = 50, LeistungBisKw = 5000, LeistungSchrittKw = 20,
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
            Vorpruefen = _ => Array.Empty<FlottenHinweis>()
        };
        anpassen?.Invoke(dienste);

        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.PlanerVerfuegbar, true));

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Optimierung);
        return cut;
    }
}
