using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// DIE GRÖSSEN-SICHT DER SPEICHERFLOTTE (Auftrag #193, Paket P4 des Konzepts
/// „Stromspeicher-Dialoge", Abschnitt 2.5).
///
/// <para>Geprüft wird, was der Baustein aus einem fertigen
/// <see cref="FlottenAuslegungErgebnis"/> macht: Rasterkarte, die zwei Schnitte mit
/// ihren Schiebern, die Einheitenwahl und die Kandidatentabelle mit Sortierung,
/// Spaltenfilter, Optimum-Zeile, Arbeitslos-Kennzeichen und dem Ereignis
/// „übernehmen".</para>
///
/// <para><b>Was hier NICHT geprüft wird: die Bilder selbst.</b> Ein PNG lässt sich
/// nicht befragen. Dass Achsen, Schraffur und Marke stimmen, zeigt
/// <c>EPOS.Kern.Tests/SpeicherFlottenGroessenCtrlTests</c> an den Daten und an den
/// Bildbytes; hier zählt, dass ein Bild ANKOMMT und dass die Schieber die gewählte
/// Stelle melden.</para>
/// </summary>
public sealed class SpeicherFlottenGroessenAnsichtTests : EposBunitContext
{
    // =====================================================================
    //  Bilder, Einheitenwahl, Schieber
    // =====================================================================

    [Fact]
    public void Ohne_Ergebnis_steht_der_Hinweis_und_kein_Bild()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>();

        Assert.Contains(Resource.FLOTTE_GROESSEN_LEER, cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("img"));
    }

    /// <summary>
    /// Drei Bilder: die Rasterkarte und die zwei Schnitte. Sie kommen als
    /// <c>data:</c>-URI herein — dasselbe Muster wie in der Ergebnisansicht (#184).
    /// </summary>
    [Fact]
    public void Karte_und_zwei_Schnitte_stehen_als_Bild_da()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        var bilder = cut.FindAll("img.epos-chartbild");
        Assert.Equal(3, bilder.Count);
        Assert.All(bilder, b => Assert.StartsWith("data:image/png;base64,",
            b.GetAttribute("src"), StringComparison.Ordinal));
    }

    /// <summary>Die Aussage des Laufs und der SP‑O‑4-Hinweis stehen über den Bildern.</summary>
    [Fact]
    public void Aussage_und_SPO4_Hinweis_stehen_ueber_den_Bildern()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        Assert.Contains("Beste Variante im geprueften endlichen Raster", cut.Markup,
            StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_GROESSEN_ENDLICHES_RASTER, cut.Markup,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Die EINHEITENWAHL erscheint erst ab zwei Einheiten (Konzept 2.5: „je Einheit
    /// wählbar, bei Anzahl &gt; 1 die Summe").
    /// </summary>
    [Fact]
    public void Die_Einheitenwahl_erscheint_erst_ab_zwei_Einheiten()
    {
        var eine = Render<SpeicherFlottenGroessenAnsicht>(p => p
            .Add(x => x.Ergebnis, Ergebnis()).Add(x => x.Einheiten, 1));
        Assert.DoesNotContain(Resource.FLOTTE_GROESSEN_LBL_EINHEIT, eine.Markup,
            StringComparison.Ordinal);

        var zwei = Render<SpeicherFlottenGroessenAnsicht>(p => p
            .Add(x => x.Ergebnis, Zweierflotte()).Add(x => x.Einheiten, 2));
        Assert.Contains(Resource.FLOTTE_GROESSEN_LBL_EINHEIT, zwei.Markup,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Die Einheitenwahl schaltet die Karte auf die Größen EINER Einheit um — im
    /// Prüfstand halbiert sich damit die Kapazitätsachse.
    /// </summary>
    [Fact]
    public void Die_Einheitenwahl_schaltet_die_Karte_um()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p
            .Add(x => x.Ergebnis, Zweierflotte()).Add(x => x.Einheiten, 2));

        // Ohne bestes Kandidat kennt die Zweierflotte keine Optimum-Stelle; die
        // Schieber beginnen deshalb vorn auf der Achse.
        Assert.Equal(-1, cut.Instance.GewaehlteEinheit);
        Assert.Equal(20.0, cut.Instance.GewaehlteKapazitaet, 9);

        Auswahl(cut, Resource.FLOTTE_GROESSEN_LBL_EINHEIT).Change("0");

        Assert.Equal(0, cut.Instance.GewaehlteEinheit);
        Assert.Equal(10.0, cut.Instance.GewaehlteKapazitaet, 9);
    }

    /// <summary>
    /// Der C-RATEN-SCHIEBER läuft über die STELLEN der Achse und beginnt auf der des
    /// Optimums — der erste Blick liegt auf der Kurve mit dem markierten Besten.
    /// </summary>
    [Fact]
    public void Der_CRaten_Schieber_beginnt_am_Optimum_und_waehlt_die_Stelle()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        Assert.Equal(1.0, cut.Instance.GewaehlteCRate, 9);
        Assert.Equal(20.0, cut.Instance.GewaehlteKapazitaet, 9);

        Schieber(cut, Resource.FLOTTE_GROESSEN_LBL_CRATE).Change("0");
        Assert.Equal(0.5, cut.Instance.GewaehlteCRate, 9);

        Schieber(cut, Resource.FLOTTE_GROESSEN_LBL_KAPAZITAET).Change("2");
        Assert.Equal(30.0, cut.Instance.GewaehlteKapazitaet, 9);
    }

    /// <summary>Eine Stelle außerhalb der Achse wird geklemmt statt eine leere Kurve zu zeigen.</summary>
    [Fact]
    public void Ein_Schieberwert_ausserhalb_der_Achse_wird_geklemmt()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        Schieber(cut, Resource.FLOTTE_GROESSEN_LBL_KAPAZITAET).Change("99");

        Assert.Equal(30.0, cut.Instance.GewaehlteKapazitaet, 9);
        Assert.Equal(3, cut.FindAll("img.epos-chartbild").Count);
    }

    // =====================================================================
    //  Die Kandidatentabelle
    // =====================================================================

    /// <summary>
    /// Die Tabelle führt jeden Kandidaten mit seinen Kennzahlen — Durchsatz als
    /// Vollzyklen, Bezugsspitze, Ersparnis — und eine Aktionsspalte mit beschriftetem
    /// Kopf (W5‑B‑1).
    /// </summary>
    [Fact]
    public void Die_Tabelle_fuehrt_jeden_Kandidaten_mit_seinen_Kennzahlen()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        Assert.Equal(7, Zeilen(cut).Count);
        Assert.Contains(Resource.FLOTTE_GROESSEN_SP_VOLLZYKLEN, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_GROESSEN_SP_SPITZE, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_GROESSEN_SP_ERSPARNIS, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_GROESSEN_SP_AKTION, cut.Markup, StringComparison.Ordinal);
        Assert.Contains("16,74", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>Die Zeile des Optimums ist hervorgehoben und trägt eine MARKE — nicht nur Farbe.</summary>
    [Fact]
    public void Die_Optimum_Zeile_ist_hervorgehoben_und_benannt()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        IElement zeile = Assert.Single(cut.FindAll("tr.epos-flotte-groessen-optimum"));
        Assert.Contains("K-20-1,0", zeile.TextContent, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_GROESSEN_OPTIMUM, zeile.TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ein ARBEITSLOSER Kandidat ist als solcher benannt — genau das konnte die
    /// Kandidatentabelle bis #193 nicht (Konzept 1.5).
    /// </summary>
    [Fact]
    public void Ein_arbeitsloser_Kandidat_wird_benannt()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        IElement marke = Assert.Single(cut.FindAll(".epos-flotte-groessen-arbeitslos"));
        Assert.Equal(Resource.FLOTTE_GROESSEN_ARBEITSLOS, marke.TextContent.Trim());
    }

    /// <summary>Gewinnt die Nullvariante, gibt es keine Optimum-Zeile, aber den Hinweis.</summary>
    [Fact]
    public void Ohne_besten_Kandidaten_steht_der_Nullvarianten_Hinweis()
    {
        FlottenAuslegungErgebnis e = Ergebnis();
        e.BesterKandidat = null;
        e.NullvarianteGewonnen = true;

        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, e));

        Assert.Empty(cut.FindAll("tr.epos-flotte-groessen-optimum"));
        Assert.Contains(Resource.FLOTTE_EMPF_NULLVARIANTE, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Die SORTIERUNG läuft je Spalte auf → ab → aus; die Tabelle zeigt die neue
    /// Reihenfolge, ohne dass die Menge sich ändert.
    /// </summary>
    [Fact]
    public void Ein_Klick_auf_den_Spaltenkopf_sortiert()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        Kopf(cut, Resource.FLOTTE_GROESSEN_SP_KAPITALWERT).Click();
        Assert.Equal("Nullvariante-ohne-Zusatzspeicher", Ersten(cut));

        Kopf(cut, Resource.FLOTTE_GROESSEN_SP_KAPITALWERT).Click();   // auf -> ab
        Assert.Equal("K-10-1,0-PV", Ersten(cut));
        Assert.Equal(7, Zeilen(cut).Count);
    }

    /// <summary>
    /// Der SPALTENFILTER schränkt VOR der Tabelle ein (W14a‑E‑10): Trichter auf,
    /// Zahlenausdruck eingeben, Enter — die Tabelle zeigt die eingeschränkte Menge, und
    /// die Trefferzeile nennt beide Zahlen.
    /// </summary>
    [Fact]
    public void Der_Spaltenfilter_schraenkt_die_Tabelle_ein()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        Trichter(cut, Resource.FLOTTE_GROESSEN_SP_KAPAZITAET).Click();
        cut.Find(".epos-spaltenfilter-feld").Change(">15");

        Assert.Equal(3, Zeilen(cut).Count);
        Assert.Contains(string.Format(Kultur, Resource.FLOTTE_GROESSEN_TREFFER, 3, 7),
            cut.Markup, StringComparison.Ordinal);

        cut.Find(".epos-flotte-groessen-leiste button").Click();     // Filter zurücksetzen
        Assert.Equal(7, Zeilen(cut).Count);
    }

    /// <summary>Passt kein Kandidat, sagt die Ansicht es — statt eine leere Tabelle zu zeigen.</summary>
    [Fact]
    public void Ohne_Treffer_steht_der_Hinweis_statt_einer_leeren_Tabelle()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        Trichter(cut, Resource.FLOTTE_GROESSEN_SP_KAPAZITAET).Click();
        cut.Find(".epos-spaltenfilter-feld").Change(">9999");

        Assert.Empty(Zeilen(cut));
        Assert.Contains(Resource.FLOTTE_GROESSEN_KEIN_TREFFER, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>„Kandidat übernehmen" meldet GENAU den Kandidaten seiner Zeile.</summary>
    [Fact]
    public void Uebernehmen_meldet_den_Kandidaten_der_Zeile()
    {
        FlottenKandidatZusammenfassung? gemeldet = null;
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p
            .Add(x => x.Ergebnis, Ergebnis())
            .Add(x => x.KandidatUebernehmen, k => gemeldet = k));

        IElement zeile = cut.FindAll("tbody tr")
            .Single(x => x.TextContent.Contains("K-30-0,5", StringComparison.Ordinal));
        zeile.QuerySelector(".epos-zellenaktionen button")!.Click();

        Assert.NotNull(gemeldet);
        Assert.Equal("K-30-0,5", gemeldet!.KandidatId);
    }

    /// <summary>
    /// Ohne Delegat bleibt der Knopf gesperrt — „kein Delegat, kein Bedienelement".
    /// </summary>
    [Fact]
    public void Ohne_Delegat_bleibt_der_Uebernahmeknopf_gesperrt()
    {
        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, Ergebnis()));

        Assert.All(cut.FindAll(".epos-zellenaktionen button"),
            b => Assert.True(b.HasAttribute("disabled")));
    }

    /// <summary>
    /// Ein Ergebnis ohne zweidimensionales Raster (nur EIN Kandidat) zeigt keine Karte,
    /// sagt warum — und führt die Tabelle trotzdem.
    /// </summary>
    [Fact]
    public void Ohne_Raster_steht_der_Hinweis_und_die_Tabelle_bleibt()
    {
        var eins = new FlottenAuslegungErgebnis
        {
            Kandidaten = new List<FlottenKandidatZusammenfassung>
            {
                new() { KandidatId = "Nullvariante-ohne-Zusatzspeicher", Zulaessig = true }
            }
        };

        var cut = Render<SpeicherFlottenGroessenAnsicht>(p => p.Add(x => x.Ergebnis, eins));

        Assert.Contains(Resource.FLOTTE_GROESSEN_KEIN_RASTER, cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("img.epos-chartbild"));
        Assert.Single(Zeilen(cut));
    }

    // ================================================================= Prüfstand

    private static System.Globalization.CultureInfo Kultur
        => System.Globalization.CultureInfo.CurrentCulture;

    private static IReadOnlyList<IElement> Zeilen(IRenderedComponent<SpeicherFlottenGroessenAnsicht> cut)
        => cut.FindAll("tbody tr");

    private static string Ersten(IRenderedComponent<SpeicherFlottenGroessenAnsicht> cut)
        => cut.Instance.SichtbareZeilen[0].Schluessel;

    private static IElement Auswahl(IRenderedComponent<SpeicherFlottenGroessenAnsicht> cut, string label)
        => cut.FindAll("label").First(x => x.TextContent.Contains(label, StringComparison.Ordinal))
              .QuerySelector("select")!;

    private static IElement Schieber(IRenderedComponent<SpeicherFlottenGroessenAnsicht> cut, string label)
        => cut.FindAll("label").First(x => x.TextContent.Contains(label, StringComparison.Ordinal))
              .QuerySelector("input[type=range]")!;

    private static IElement Kopf(IRenderedComponent<SpeicherFlottenGroessenAnsicht> cut, string titel)
        => cut.FindAll("th .epos-spaltenkopf-titel")
              .First(x => x.TextContent.Contains(titel, StringComparison.Ordinal));

    private static IElement Trichter(IRenderedComponent<SpeicherFlottenGroessenAnsicht> cut, string titel)
        => cut.FindAll("th").First(x => x.TextContent.Contains(titel, StringComparison.Ordinal))
              .QuerySelector(".epos-trichter")!;

    /// <summary>
    /// Derselbe synthetische 3 × 2-Prüfstand wie in
    /// <c>EPOS.Kern.Tests/SpeicherFlottenGroessenCtrlTests</c>: sieben Kandidaten,
    /// darunter ein arbeitsloser, ein unzulässiger und ein Loch bei 30 kWh / 1,0 C.
    /// </summary>
    private static FlottenAuslegungErgebnis Ergebnis()
    {
        var bester = Kandidat("K-20-1,0", 20, 20, 2000, true, 220);
        return new FlottenAuslegungErgebnis
        {
            Aussage = "Beste Variante im geprueften endlichen Raster",
            Kandidaten = new List<FlottenKandidatZusammenfassung>
            {
                new() { KandidatId = "Nullvariante-ohne-Zusatzspeicher", Zulaessig = true },
                Kandidat("K-10-0,5", 10, 5, 1000, true, 60),
                Kandidat("K-10-1,0", 10, 10, 1500, true, 90),
                Kandidat("K-10-1,0-PV", 10, 10, 9999, false, 0, FlottenBetriebsziel.PvGreedy),
                Kandidat("K-20-0,5", 20, 10, 500, true, 120),
                bester,
                Kandidat("K-30-0,5", 30, 15, 4000, false, 300)
            },
            BesterKandidat = bester
        };
    }

    private static FlottenAuslegungErgebnis Zweierflotte()
    {
        var kandidaten = new List<FlottenKandidatZusammenfassung>();
        foreach (double kapazitaet in new[] { 10.0, 20.0 })
            foreach (double rate in new[] { 0.5, 1.0 })
            {
                var k = Kandidat($"Z-{kapazitaet}-{rate}", 2 * kapazitaet,
                                 2 * kapazitaet * rate, 100 * kapazitaet * rate, true, 10);
                k.Einheiten = new List<FlottenKandidatEinheit>
                {
                    Teil("A", kapazitaet, rate),
                    Teil("B", kapazitaet, rate)
                };
                kandidaten.Add(k);
            }
        return new FlottenAuslegungErgebnis { Kandidaten = kandidaten };
    }

    private static FlottenKandidatEinheit Teil(string id, double kapazitaet, double rate) => new()
    {
        Id = id,
        KapazitaetKWh = kapazitaet,
        LadeleistungKw = kapazitaet * rate,
        EntladeleistungKw = kapazitaet * rate
    };

    private static FlottenKandidatZusammenfassung Kandidat(
        string id, double kapazitaet, double entladen, double kapitalwert,
        bool zulaessig, double durchsatz,
        FlottenBetriebsziel ziel = FlottenBetriebsziel.PeakShaving) => new()
    {
        KandidatId = id,
        Betriebsziel = ziel,
        Zulaessig = zulaessig,
        KapitalwertEuro = kapitalwert,
        KapazitaetKWh = kapazitaet,
        LadeleistungKw = entladen,
        EntladeleistungKw = entladen,
        DurchsatzKWh = durchsatz,
        Vollzyklen = kapazitaet > 0 ? durchsatz / kapazitaet : 0,
        BezugsspitzeKw = 16.74,
        ErsparnisEuroJahr = 180,
        Arbeitslos = durchsatz <= 0,
        Grund = zulaessig ? null : "Prüfstand: unzulässig",
        Einheiten = new List<FlottenKandidatEinheit>
        {
            new()
            {
                Id = "A",
                KapazitaetKWh = kapazitaet,
                LadeleistungKw = entladen,
                EntladeleistungKw = entladen
            }
        }
    };
}
