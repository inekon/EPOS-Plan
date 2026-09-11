using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Strom;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// DIE WAHL „adaptiv (kausal) | fest" und ihre Folgen in der Anzeige (Spezifikation
/// 5.1.1, Anwenderentscheide PS‑Q1 und PS‑Q2 vom 11.09.2026, Paket P7 / Auftrag #215).
///
/// <para><b>Was hier geprüft wird.</b> Schritt 3 der Ansicht: die Wahl, die Umbenennung
/// des Zahlenfeldes in „Startwert", die Vorbelegung mit der Grundlast und die
/// Umbenennung des Bisektionsknopfes in „mit Vorausschau erreichbar". Schritt 5: die
/// Zeile „kausal erreicht" neben „mit Vorausschau erreichbar". Und die Betriebszeile des
/// Stromspeicher-Reiters, die den Modus eines GESPEICHERTEN Standes nennt.</para>
///
/// <para><b>Der Rechenweg steht nicht hier</b>, sondern in
/// <c>SpeicherEngine.Tests/FlottenPeakRatscheTests</c> — dort gegen den Port des
/// Excel-Makros. Diese Prüfstände sehen nur Markup.</para>
/// </summary>
public sealed class PeakRatscheAnsichtTests : EposBunitContext
{
    /// <summary>Die Ansicht trägt einen <c>InfoKnopf</c> und braucht deshalb den Hilfedienst.</summary>
    public PeakRatscheAnsichtTests() => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    // =====================================================================
    //  Schritt 3 — der Betriebseditor
    // =====================================================================

    [Fact]
    public void Der_Editor_zeigt_die_Wahl_nur_bei_den_zwei_Zielen_mit_Peak_Ziel()
    {
        var cut = Render<SpeicherFlottenBetriebEditor>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen())
            .Add(x => x.PlanerVerfuegbar, true));

        Assert.DoesNotContain(Resource.FLOTTE_BETRIEB_LBL_PEAKMODUS, cut.Markup);
        Auswahl(cut, Resource.FLOTTE_BETRIEB_LBL_ZIEL).Change(((int)FlottenBetriebsziel.PeakShaving).ToString());
        Assert.Contains(Resource.FLOTTE_BETRIEB_LBL_PEAKMODUS, cut.Markup);
        Auswahl(cut, Resource.FLOTTE_BETRIEB_LBL_ZIEL).Change(((int)FlottenBetriebsziel.MultiUse).ToString());
        Assert.Contains(Resource.FLOTTE_BETRIEB_LBL_PEAKMODUS, cut.Markup);
        Auswahl(cut, Resource.FLOTTE_BETRIEB_LBL_ZIEL).Change(((int)FlottenBetriebsziel.PvGreedy).ToString());
        Assert.DoesNotContain(Resource.FLOTTE_BETRIEB_LBL_PEAKMODUS, cut.Markup);
    }

    [Fact]
    public void Die_Wahl_adaptiv_benennt_das_Zahlenfeld_in_Startwert_um_und_meldet_sie()
    {
        FlottenSimulationOptionen? gemeldet = null;
        var cut = Render<SpeicherFlottenBetriebEditor>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving,
                WirtschaftlicherPeakZielwertKw = 200
            })
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.WertChanged, x => gemeldet = x));

        // Ein gespeicherter Stand traegt „fest" — so rechnet er weiter (PS-Q1).
        Assert.Contains(Resource.FLOTTE_BETRIEB_LBL_PEAKZIEL, cut.Markup);
        Assert.DoesNotContain(Resource.FLOTTE_BETRIEB_LBL_PEAKSTART, cut.Markup);
        Assert.Contains(Resource.FLOTTE_PEAK_RATSCHE_NEIN, cut.Markup);

        Auswahl(cut, Resource.FLOTTE_BETRIEB_LBL_PEAKMODUS).Change("1");

        Assert.NotNull(gemeldet);
        Assert.True(gemeldet!.PeakZielAdaptiv);
        Assert.Equal(200, gemeldet.WirtschaftlicherPeakZielwertKw);
        Assert.Contains(Resource.FLOTTE_BETRIEB_LBL_PEAKSTART, cut.Markup);
        Assert.Contains(Resource.FLOTTE_PEAK_RATSCHE_JA, cut.Markup);
    }

    [Fact]
    public void Der_Peak_Ziel_Block_benennt_seine_zwei_Knoepfe_nach_dem_Modus()
    {
        var vorschlag = new FlottenPeakZielVorschlag
        {
            PeakZielKw = 777.4,
            TagesminimumMaxKw = 68.0,
            AusReihe = true,
            Herleitung = "H₀ = max(Referenzspitze 789,4 kW − Σ Entladeleistung 12 kW; 68 kW) = 777,4 kW."
        };

        var fest = Render<PeakZielBlock>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen
            { Betriebsziel = FlottenBetriebsziel.PeakShaving, WirtschaftlicherPeakZielwertKw = 777.4 })
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.Vorschlag, vorschlag)
            .Add(x => x.VorschlagUebernehmen, Microsoft.AspNetCore.Components.EventCallback.Factory
                 .Create(this, () => { }))
            .Add(x => x.Bestimmen, Microsoft.AspNetCore.Components.EventCallback.Factory
                 .Create(this, () => { })));

        Assert.Equal(fest.Instance.VorschlagText, fest.Instance.UebernahmeText);
        Assert.Equal(fest.Instance.BestimmenText, fest.Instance.BestimmenBeschriftung);
        Assert.Contains("H₀ = max(Referenzspitze", fest.Markup);

        var adaptiv = Render<PeakZielBlock>(p => p
            .Add(x => x.Wert, new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving,
                WirtschaftlicherPeakZielwertKw = 68.0,
                PeakZielAdaptiv = true
            })
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.Vorschlag, vorschlag)
            .Add(x => x.GrundlastText, Resource.FLOTTE_PEAK_BTN_GRUNDLAST)
            .Add(x => x.VorausschauText, Resource.FLOTTE_PEAK_BTN_VORAUSSCHAU)
            .Add(x => x.StartwertHerleitung, Resource.FLOTTE_PEAK_STARTWERT_HERLEITUNG)
            .Add(x => x.VorschlagUebernehmen, Microsoft.AspNetCore.Components.EventCallback.Factory
                 .Create(this, () => { }))
            .Add(x => x.Bestimmen, Microsoft.AspNetCore.Components.EventCallback.Factory
                 .Create(this, () => { })));

        Assert.True(adaptiv.Instance.Adaptiv);
        Assert.Equal(Resource.FLOTTE_PEAK_BTN_GRUNDLAST, adaptiv.Instance.UebernahmeText);
        Assert.Equal(Resource.FLOTTE_PEAK_BTN_VORAUSSCHAU, adaptiv.Instance.BestimmenBeschriftung);
        // Die Herleitung nennt jetzt die GRUNDLAST als Startwert, nicht das feste Ziel.
        Assert.Contains("68", adaptiv.Instance.VorschlagHerleitung);
        Assert.DoesNotContain("777,4", adaptiv.Instance.VorschlagHerleitung);
    }

    // =====================================================================
    //  Schritt 3 der ANSICHT — Vorbelegung mit der Grundlast
    // =====================================================================

    [Fact]
    public void Das_Einschalten_der_Ratsche_belegt_den_Startwert_mit_der_Grundlast_vor()
    {
        FlottenStudieKonfiguration flotte = Flotte();
        var cut = Ansicht(new StromspeicherAuslegungDienste
        {
            Vorgaben = () => Vorgaben(flotte),
            PeakZielVorschlag = _ => new FlottenPeakZielVorschlag
            {
                PeakZielKw = 777.4,
                TagesminimumMaxKw = 68.0,
                AusReihe = true,
                Herleitung = "H₀ = max(Referenzspitze 789,4 kW − Σ Entladeleistung 12 kW; 68 kW) = 777,4 kW."
            }
        });

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);
        Assert.Equal(50, cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.WirtschaftlicherPeakZielwertKw);

        cut.FindAll("label").Single(x => x.TextContent.Contains(Resource.FLOTTE_BETRIEB_LBL_PEAKMODUS))
           .QuerySelector("select")!.Change("1");

        Assert.True(cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.PeakZielAdaptiv);
        Assert.Equal(68.0, cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.WirtschaftlicherPeakZielwertKw);

        // Das AUSschalten lässt die Zahl stehen — sie ist jetzt das feste Ziel.
        cut.FindAll("label").Single(x => x.TextContent.Contains(Resource.FLOTTE_BETRIEB_LBL_PEAKMODUS))
           .QuerySelector("select")!.Change("0");

        Assert.False(cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.PeakZielAdaptiv);
        Assert.Equal(68.0, cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.WirtschaftlicherPeakZielwertKw);
    }

    [Fact]
    public void Ein_Zielwechsel_zieht_die_Vorgabe_Ratsche_mit()
    {
        FlottenStudieKonfiguration flotte = Flotte();
        flotte.Optionen.Betriebsziel = FlottenBetriebsziel.PvGreedy;

        var cut = Ansicht(new StromspeicherAuslegungDienste { Vorgaben = () => Vorgaben(flotte) });
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);

        Assert.False(cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.PeakZielAdaptiv);

        cut.FindAll("label").Single(x => x.TextContent.Contains(Resource.FLOTTE_BETRIEB_LBL_ZIEL))
           .QuerySelector("select")!.Change(((int)FlottenBetriebsziel.PeakShaving).ToString());

        Assert.True(cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.PeakZielAdaptiv);
    }

    // =====================================================================
    //  Schritt 5 — die zwei Schwellen
    // =====================================================================

    [Fact]
    public void Die_Ergebnisansicht_nennt_die_kausal_erreichte_Schwelle_und_die_Nachzuege()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p
            .Add(x => x.Ergebnis, Lauf(hEnde: 540, nachzuege: 3)));

        Assert.Equal(540.0, cut.Instance.KausalErreicht);
        Assert.Contains("540", cut.Instance.KausalZeile);
        Assert.Contains("3", cut.Instance.KausalZeile);
        Assert.Contains(cut.Instance.KausalZeile, cut.Markup);
        // Ohne Bisektion steht die zweite Zeile nicht da.
        Assert.Equal("", cut.Instance.VorausschauZeile);
    }

    [Fact]
    public void Neben_der_kausalen_Schwelle_steht_das_Vorausschau_Optimum_und_ihre_Differenz()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p
            .Add(x => x.Ergebnis, Lauf(hEnde: 540, nachzuege: 3))
            .Add(x => x.VorausschauPeakZielKw, 340.0));

        Assert.Contains("340", cut.Instance.VorausschauZeile);
        // Der Wert der Vorausschau: 540 − 340 = 200 kW.
        Assert.Contains("200", cut.Instance.VorausschauZeile);
        Assert.Contains(cut.Instance.VorausschauZeile, cut.Markup);
    }

    [Fact]
    public void Ein_festes_Ziel_kennt_keine_kausal_erreichte_Schwelle()
    {
        var cut = Render<SpeicherFlottenErgebnisAnsicht>(p => p
            .Add(x => x.Ergebnis, Lauf(hEnde: null, nachzuege: 0)));

        Assert.Null(cut.Instance.KausalErreicht);
        Assert.Equal("", cut.Instance.KausalZeile);
        // Und die alte Aussage „erreicht / nicht erreicht" steht unverändert da.
        Assert.Contains(Resource.FLOTTE_STATUS_NICHT_ERREICHT, cut.Markup);
    }

    // =====================================================================
    //  Prüfstände
    // =====================================================================

    private static FlottenStudieKonfiguration Flotte() => new()
    {
        Einheiten = new List<FlottenEinheit>
        {
            new()
            {
                Id = "a", Name = "A", KapazitaetKWh = 24, LadeleistungKw = 10, EntladeleistungKw = 12,
                Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
                SocMin = 0.1, SocMax = 0.9, SocStart = 0.1
            }
        },
        Optionen = new()
        {
            Betriebsziel = FlottenBetriebsziel.PeakShaving,
            WirtschaftlicherPeakZielwertKw = 50,
            EnergieAusgleichEuroProKWh = 0.2
        },
        Wirtschaftlichkeit = new() { ProjektjahreBeiWiederholung = 1 }
    };

    private static SpeicherOptimierungVorgaben Vorgaben(FlottenStudieKonfiguration flotte) => new()
    {
        Eingaben = new SpeicherOptimierungEingaben
        { Auslegung = new SpeicherAuslegungKonfiguration { Flotte = flotte } }
    };

    /// <summary>Ein Laufstand mit — oder ohne — kausal erreichte Schwelle.</summary>
    private static SpeicherFlottenErgebnis Lauf(double? hEnde, int nachzuege) => new()
    {
        Erfolg = true,
        Konfiguration = new FlottenStudieKonfiguration
        {
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving,
                Verteilung = FlottenVerteilung.Kaskade,
                WirtschaftlicherPeakZielwertKw = 60,
                PeakZielAdaptiv = hEnde.HasValue
            }
        },
        Studie = new FlottenStudienErgebnis
        {
            Variante = new FlottenSimulationErgebnis
            {
                Zulaessig = true,
                MaximalerNetzbezugKw = hEnde ?? 740,
                ErreichtesPeakZielKw = hEnde,
                Diagnose = new FlottenDiagnose
                {
                    IntervalleGesamt = 672,
                    IntervalleSchwelleNachgezogen = nachzuege
                }
            }
        }
    };

    private IRenderedComponent<StromspeicherAuslegungSeite> Ansicht(StromspeicherAuslegungDienste dienste)
        => Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, dienste)
            .Add(x => x.PlanerVerfuegbar, true));

    private static AngleSharp.Dom.IElement Auswahl(
        IRenderedComponent<SpeicherFlottenBetriebEditor> cut, string label) =>
        cut.FindAll("label").Single(x => x.TextContent.Contains(label)).QuerySelector("select")!;
}
