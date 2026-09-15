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
/// <para><b>Befund 1: „1 bleibt stehen, Eingabe nicht korrekt möglich."</b> Wer eine
/// Zahl in einem Feld des Suchraums rückwärts löscht, meldet erst „1" — der Suchraum
/// nimmt sie — und dann die LEERE Eingabe. Ein leeres Feld meldet <c>null</c>; der
/// Suchraum übernahm <c>null</c> nicht, behielt seine 1, und das Feld schrieb sie in die
/// Anzeige zurück. Die 1 ließ sich nicht mehr entfernen, jedes weitere Zeichen landete
/// dahinter. Seither bleibt ein geleertes Feld leer (<c>Zahlenfeld</c>), und die leere
/// Eingabe ist im Suchraum die 0 — also ein benannt ungültiger Bereich, der den Lauf
/// sperrt, statt ihn mit einer Zahl rechnen zu lassen, die niemand mehr sieht.</para>
///
/// <para><b>Gemessen wird am Feld „Kapazität bis".</b> Die Größensuche wählt unter
/// vorhandenen Geräten und kennt keine Schrittweite mehr; die Befundlage ist dieselbe —
/// ein Zahlenfeld des Suchraums, dessen geleerte Eingabe im Modell ankommen muss.</para>
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
    //  1. Das Zahlenfeld des Suchraums
    // =====================================================================

    /// <summary>
    /// <b>Die Eingabefolge des Anwenders kommt vollständig im Modell an</b>, und das Feld
    /// zeigt nach JEDEM Zeichen das Getippte — auch das Nichts.
    /// </summary>
    [Fact]
    public void Die_Eingabefolge_im_Suchraumfeld_kommt_vollstaendig_im_Modell_an()
    {
        var cut = Station();
        Assert.Equal("10", Kapazitaetsfeld(cut).GetAttribute("value"));

        // „10" rueckwaerts geloescht: erst die 1 …
        Tippen(cut, "1");
        Assert.Equal(1.0, Achse(cut).KapazitaetBisKWh);

        // … dann ganz leer. DAS Feld bleibt leer (der Befund: hier blieb die 1 stehen).
        Tippen(cut, "");

        // Ein zweites Loeschen aendert daran nichts.
        Tippen(cut, "");

        // Und jetzt die neue Obergrenze, Zeichen fuer Zeichen.
        foreach ((string getippt, double erwartet) in new[]
                 { ("1", 1.0), ("10", 10.0), ("100", 100.0), ("1000", 1000.0) })
        {
            Tippen(cut, getippt);
            Assert.Equal(erwartet, Achse(cut).KapazitaetBisKWh);
        }
    }

    /// <summary>
    /// <b>Ein geleertes Feld ist die 0 und damit ein ungültiger Bereich</b> — die
    /// Kandidatenzeile sagt es, und der Rechenknopf ist gesperrt. Der alte Wert bleibt
    /// NICHT unsichtbar im Suchraum stehen.
    /// </summary>
    [Fact]
    public void Ein_geleertes_Suchraumfeld_macht_den_Bereich_ungueltig_und_sperrt_den_Lauf()
    {
        var cut = Station();

        Tippen(cut, "");

        Assert.Equal(0.0, Achse(cut).KapazitaetBisKWh);
        Assert.Contains(Resource.FLOTTE_OPT_RASTER_UNGUELTIG,
                        cut.Find("p.epos-flotte-kandidatenzeile").TextContent,
                        StringComparison.Ordinal);
        Assert.True(Auslegungshilfe.Rechenknopf(cut).HasAttribute("disabled"));

        // Die frische Obergrenze gibt den Lauf wieder frei.
        Tippen(cut, "1000");
        Assert.False(Auslegungshilfe.Rechenknopf(cut).HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Anzeige und Vorprüfung führen DIESELBE Zahl.</b> Sechs Geräte im Bereich gegen
    /// eine Grenze von drei: Die Kandidatenzeile nennt beide Zahlen, färbt sich rot, und
    /// die Vorprüfung sperrt den Lauf mit derselben Begründung.
    /// </summary>
    /// <remarks>
    /// <b>Die Kandidatenzahl kann nicht mehr explodieren</b> — sie ist höchstens so groß
    /// wie der Gerätebestand der Quelle. Die Grenze bleibt trotzdem stehen und fängt einen
    /// sehr großen Bestand ab; genau das misst dieser Fall.
    /// </remarks>
    [Fact]
    public void Die_Kandidatenzeile_nennt_dieselbe_Zahl_wie_die_Vorpruefung()
    {
        var cut = Station(kapazitaetBis: 5000, maximaleKandidaten: 3);

        string zeile = cut.Find("p.epos-flotte-kandidatenzeile").TextContent;
        Assert.Contains("6", zeile, StringComparison.Ordinal);
        Assert.Contains("3", zeile, StringComparison.Ordinal);
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
        }, kapazitaetBis: 1000);

        Assert.Empty(cut.FindAll(".epos-fortschritt"));

        Task klick = Auslegungshilfe.Rechenknopf(cut).ClickAsync(new());

        cut.WaitForAssertion(() => Assert.True(cut.Instance.Laeuft));
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

        var cut = Station(d => d.FlotteRechnen = (_, _) => quelle.Task, kapazitaetBis: 1000);

        Task klick = Auslegungshilfe.Rechenknopf(cut).ClickAsync(new());
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Laeuft));
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Speicher);

        Assert.Single(cut.FindAll(".epos-fortschritt"));
        Assert.Empty(cut.FindAll(".epos-flotte-bedienblock .epos-fortschritt"));

        quelle.SetResult(new SpeicherFlottenErgebnis());
        await klick;
    }

    // ================================================================= Prüfstand

    /// <summary>
    /// Tippt ein Zeichen in das Feld „Kapazität bis" und <b>wartet auf den gezeichneten Zustand</b>:
    /// die Fassung des Arbeitsstandes ist weitergezählt UND das Feld zeigt das Getippte.
    /// </summary>
    /// <remarks>
    /// bunit gibt den Tastendruck in den Zeichenverteiler und kehrt zurück, ohne den
    /// Zeichenlauf abzuwarten; eine Prüfung unmittelbar dahinter liest sonst den Stand VOR
    /// dem Zeichen (Hausmuster: auf den gezeichneten Zustand warten, nie sofort prüfen).
    /// Die <c>Fassung</c> ist der verlässliche Merkposten — sie zählt bei JEDER gemeldeten
    /// Änderung hoch, auch dort, wo der Feldtext derselbe bleibt (zweimal leeren).
    /// </remarks>
    private static void Tippen(IRenderedComponent<StromspeicherAuslegungSeite> cut, string text)
    {
        int fassung = cut.Instance.Fassung;
        Kapazitaetsfeld(cut).Input(text);
        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Instance.Fassung > fassung, "Der Tastendruck ist noch nicht angekommen.");
            Assert.Equal(text, Kapazitaetsfeld(cut).GetAttribute("value"));
        });
    }

    private static FlottenAuslegungsAchse Achse(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen[0];

    /// <summary>Sechs Geräte von 10 bis 5000 kWh, alle mit 100 kW.</summary>
    private static List<FlottenGeraetekandidat> Bestand()
        => new[] { 10.0, 50.0, 100.0, 500.0, 1000.0, 5000.0 }
            .Select(kWh => new FlottenGeraetekandidat
            {
                Quellkennung = kWh.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                Geraet = new FlottenEinheit
                {
                    Id = "G" + kWh.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                    Name = "Speicher " + kWh.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                    KapazitaetKWh = kWh, LadeleistungKw = 100, EntladeleistungKw = 100,
                    Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
                    SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
                }
            }).ToList();

    private static IElement Kapazitaetsfeld(IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.Find("article.epos-flotte-einheitskarte")
              .QuerySelectorAll("label.epos-feld")
              .Single(x => x.TextContent.Contains(Resource.FLOTTE_ED_KAPAZITAET_BIS,
                                                  StringComparison.Ordinal))
              .QuerySelector("input")!;

    /// <summary>
    /// Die Ansicht auf Station 4: eine Einheit 4180 kWh / 125 kW / 1 Stück, Kapazität
    /// 1…<paramref name="kapazitaetBis"/> kWh, Leistung 1…5000 kW — und ein Bestand von
    /// SECHS Geräten von 10 bis 5000 kWh.
    /// </summary>
    /// <param name="anpassen">Zusätzliche Wege der Ansicht.</param>
    /// <param name="kapazitaetBis">Die Obergrenze der Kapazität; 10 kWh lässt genau ein Gerät übrig.</param>
    /// <param name="maximaleKandidaten">Die Schranke der Kandidatenzahl.</param>
    private IRenderedComponent<StromspeicherAuslegungSeite> Station(
        Action<StromspeicherAuslegungDienste>? anpassen = null, double kapazitaetBis = 10,
        int maximaleKandidaten = 10000)
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
                MaximaleKandidaten = maximaleKandidaten,
                Suchmethode = FlottenSuchmethode.Groesse,
                Achsen = new List<FlottenAuslegungsAchse>
                {
                    new()
                    {
                        Aktiv = true, Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                        AnzahlVon = 1, AnzahlBis = 1,
                        KapazitaetVonKWh = 1, KapazitaetBisKWh = kapazitaetBis,
                        LeistungVonKw = 1, LeistungBisKw = 5000,
                        Geraete = Bestand(),
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
            .Add(x => x.PlanerVerfuegbar, true)
            // OHNE ENTPRELLUNG: Die volle Vorpruefung laeuft im selben Zeichenlauf statt
            // aus einem Zeitgeber. Sonst meldet sich ihre Fortsetzung mitten in der
            // Eingabefolge aus dem Fadenvorrat zurueck, belegt den Zeichenverteiler und
            // schiebt den naechsten Tastendruck hinter die Pruefung. Die Entprellung
            // selbst pruefen die VorpruefungEntprelltTests.
            .Add(x => x.EntprellungMs, 0));

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Optimierung);
        return cut;
    }
}
