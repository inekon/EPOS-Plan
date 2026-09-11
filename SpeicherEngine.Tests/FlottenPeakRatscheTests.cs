using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests;

/// <summary>
/// DIE KAUSALE RATSCHE (Spezifikation 5.1.1, Regel R; Paket P7, Auftrag #215).
///
/// <para><b>Der Befund vom 11.09.2026.</b> Bei FESTEM Peak-Ziel entlädt die Flotte auch
/// dann noch bei jeder kleineren Spitze, wenn die Jahresspitze längst verloren ist — der
/// Speicher steht leer, wenn die große Spitze kommt. Der Anwender hat das kausale
/// Gegenmodell als Excel-Makro (<c>Modul2.calc_peakshaving</c>) vorgelegt: Die Schwelle H
/// ist kein Parameter, sondern ein Zustand, der im Lauf nur steigen kann.</para>
///
/// <para><b>Das Makro ist die Referenzrechnung dieses Prüfstands.</b>
/// <see cref="Makro"/> ist sein wortgetreuer Port — dieselbe Reihenfolge, dieselben drei
/// Fälle, dieselbe Zustandsfortschreibung. Für EINE Einheit ohne Verluste und ohne
/// SoC-Fenster ist die Regel R wortgleich mit ihm; der Prüfstand rechnet beide auf
/// demselben synthetischen Lastgang und vergleicht Jahresspitze, H-Treppe und
/// Netzganglinie.</para>
///
/// <para><b>Der Lastgang ist SYNTHETISCH.</b> Der Kundenlastgang aus der Excel-Datei
/// kommt nicht ins Repositorium (Spezifikation 5.1.1); <see cref="Lastgang"/> baut sieben
/// Tage im Viertelstundenraster aus benannten Zahlen — Grundlast 60 kW, fünf Tagesspitzen
/// von 250 bis 400 kW, am sechsten Abend ein 400-kW-Block, der ohne Ladepause in die
/// Spitze von 740 kW des siebten Tages übergeht. Genau dieser Übergang trennt die
/// kausale Regel vom Vorausschau-Optimum.</para>
/// </summary>
public sealed class FlottenPeakRatscheTests : IDisposable
{
    private const double Dt = 0.25;

    /// <summary>Die Grundlast des synthetischen Lastgangs [kW] — zugleich das Maximum der Tagesminima.</summary>
    private const double GrundlastKw = 60.0;

    /// <summary>Die höchste Last des synthetischen Lastgangs [kW].</summary>
    private const double SpitzeKw = 740.0;

    /// <summary>Lade- und Entladeleistung der einen Einheit [kW].</summary>
    private const double LeistungKw = 400.0;

    /// <summary>Nutzbarer Inhalt der einen Einheit [kWh].</summary>
    private const double KapazitaetKWh = 400.0;

    private readonly CultureInfo _vorher = CultureInfo.CurrentCulture;

    public FlottenPeakRatscheTests()
    {
        CultureInfo de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        Thread.CurrentThread.CurrentCulture = de;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CultureInfo.CurrentCulture = _vorher;
        Thread.CurrentThread.CurrentCulture = _vorher;
    }

    // =====================================================================
    //  (a) Die Referenzrechnung: Port des Excel-Makros
    // =====================================================================

    [Fact]
    public void Adaptiv_LiefertDieselbeJahresspitzeUndDieselbeHTreppeWieDasExcelMakro()
    {
        double[] last = Lastgang();
        MakroLauf referenz = Makro(last, GrundlastKw, adaptiv: true);

        FlottenSimulationErgebnis variante =
            FlottenSimulator.Simuliere(Eingang(last), Config(GrundlastKw, adaptiv: true)).Variante;

        Assert.Equal(referenz.Jahresspitze, variante.MaximalerNetzbezugKw, 6);
        Assert.Equal(referenz.SchwelleEnde, variante.ErreichtesPeakZielKw!.Value, 6);
        Assert.Equal(last.Length, variante.Intervalle.Count);
        for (int t = 0; t < last.Length; t++)
        {
            Assert.Equal(referenz.Schwelle[t], variante.Intervalle[t].PeakZielKw!.Value, 6);
            Assert.Equal(referenz.Netzleistung[t], variante.Intervalle[t].NetzleistungKw, 6);
            Assert.Equal(referenz.Ladezustand[t], variante.Intervalle[t].EnergieEndeKWhJeSpeicher[0], 6);
        }
    }

    [Fact]
    public void Adaptiv_SenktDieJahresspitzeDesSynthetischenLastgangsVon740Auf540()
    {
        FlottenSimulationErgebnis variante =
            FlottenSimulator.Simuliere(Eingang(Lastgang()), Config(GrundlastKw, adaptiv: true)).Variante;

        // Die Schwelle steht zuerst auf der Grundlast, wird an der ersten Tagesspitze auf
        // 250 kW nachgezogen (leerer Speicher) und am siebten Tag zweimal: erst auf
        // 340 kW (740 − 400 kW Entladeleistung), dann auf 540 kW, als der Inhalt nur noch
        // 200 kW trägt.
        Assert.Equal(540.0, variante.MaximalerNetzbezugKw, 6);
        Assert.Equal(540.0, variante.ErreichtesPeakZielKw!.Value, 6);
        Assert.Equal(new[] { GrundlastKw, 250.0, 340.0, 540.0 },
                     Stufen(variante).Select(x => Math.Round(x, 6)));
    }

    [Fact]
    public void Fest_LaesstDieselbeFlotteMitDemselbenStartwertBeiDerVollenSpitzeStehen()
    {
        // Befund SP-O-10 in Reinform: Ein festes Ziel auf der Grundlast erlaubt keine
        // Wiederaufladung (Ladedeckel max(0, H − N) = 0), der Speicher bleibt leer.
        FlottenSimulationErgebnis variante =
            FlottenSimulator.Simuliere(Eingang(Lastgang()), Config(GrundlastKw, adaptiv: false)).Variante;

        Assert.Equal(SpitzeKw, variante.MaximalerNetzbezugKw, 6);
        Assert.True(variante.Diagnose.Arbeitslos);
    }

    // =====================================================================
    //  (b) Monotonie
    // =====================================================================

    [Fact]
    public void DieSchwelleSteigtImLaufNurUndFaelltNie()
    {
        FlottenSimulationErgebnis variante =
            FlottenSimulator.Simuliere(Eingang(Lastgang()), Config(GrundlastKw, adaptiv: true)).Variante;

        double vorher = double.NegativeInfinity;
        foreach (FlottenIntervallErgebnis x in variante.Intervalle)
        {
            Assert.True(x.PeakZielKw!.Value >= vorher,
                $"Die Schwelle ist von {vorher} auf {x.PeakZielKw} gefallen.");
            vorher = x.PeakZielKw.Value;
        }
        Assert.Equal(vorher, variante.ErreichtesPeakZielKw!.Value, 10);
    }

    [Fact]
    public void DieDiagnoseZaehltJedesNachziehenDerSchwelle()
    {
        FlottenSimulationErgebnis adaptiv =
            FlottenSimulator.Simuliere(Eingang(Lastgang()), Config(GrundlastKw, adaptiv: true)).Variante;
        FlottenSimulationErgebnis fest =
            FlottenSimulator.Simuliere(Eingang(Lastgang()), Config(GrundlastKw, adaptiv: false)).Variante;

        Assert.Equal(Stufen(adaptiv).Count - 1, adaptiv.Diagnose.IntervalleSchwelleNachgezogen);
        Assert.Equal(0, fest.Diagnose.IntervalleSchwelleNachgezogen);
    }

    // =====================================================================
    //  (c) Fest gegen adaptiv
    // =====================================================================

    [Theory]
    [InlineData(60.0)]
    [InlineData(250.0)]
    [InlineData(340.0)]
    [InlineData(500.0)]
    public void AdaptivIstBeiGleichemStartwertNieSchlechterAlsEinFestesZiel(double h0)
    {
        double adaptiv = FlottenSimulator.Simuliere(Eingang(Lastgang()), Config(h0, adaptiv: true))
            .Variante.MaximalerNetzbezugKw;
        double fest = FlottenSimulator.Simuliere(Eingang(Lastgang()), Config(h0, adaptiv: false))
            .Variante.MaximalerNetzbezugKw;

        Assert.True(adaptiv <= fest + 1e-9,
            $"Adaptiv {adaptiv} kW liegt über fest {fest} kW (Startwert {h0} kW).");
    }

    [Fact]
    public void MitDemVorausschauOptimumAlsStartwertIstDieRatscheDasFesteZiel()
    {
        // Spezifikation 5.1.1, Alternative S-C: „Mit H0 = M* ist R mit dem festen Ziel
        // identisch." Die Schwelle wird dann nie nachgezogen.
        double stern = VorausschauOptimum();

        FlottenSimulationErgebnis adaptiv =
            FlottenSimulator.Simuliere(Eingang(Lastgang()), Config(stern, adaptiv: true)).Variante;
        FlottenSimulationErgebnis fest =
            FlottenSimulator.Simuliere(Eingang(Lastgang()), Config(stern, adaptiv: false)).Variante;

        Assert.Equal(0, adaptiv.Diagnose.IntervalleSchwelleNachgezogen);
        Assert.Equal(fest.MaximalerNetzbezugKw, adaptiv.MaximalerNetzbezugKw, 10);
        for (int t = 0; t < adaptiv.Intervalle.Count; t++)
            Assert.Equal(fest.Intervalle[t].NetzleistungKw, adaptiv.Intervalle[t].NetzleistungKw, 10);
    }

    // =====================================================================
    //  (d) M* <= H_end
    // =====================================================================

    [Fact]
    public void DasVorausschauOptimumLiegtNichtUeberDerKausalErreichtenSchwelle()
    {
        double stern = VorausschauOptimum();
        double hEnde = FlottenSimulator.Simuliere(Eingang(Lastgang()), Config(GrundlastKw, adaptiv: true))
            .Variante.ErreichtesPeakZielKw!.Value;

        Assert.True(stern <= hEnde + 1e-6, $"M* {stern} kW liegt über H_end {hEnde} kW.");
        // Auf diesem Lastgang ist die Differenz der WERT DER VORAUSSCHAU: Der 400-kW-Block
        // am sechsten Abend geht ohne Ladepause in die Spitze des siebten Tages über.
        Assert.Equal(340.0, stern, 1);
        Assert.Equal(540.0, hEnde, 6);
    }

    // =====================================================================
    //  (e) Bit-Identitaet des bisherigen Rechenwegs
    // =====================================================================

    [Fact]
    public void OhneRatscheRechnetDerSimulatorGenauDasBisherigeFesteZiel()
    {
        double[] last = Lastgang();
        MakroLauf referenz = Makro(last, 250.0, adaptiv: false);

        FlottenSimulationErgebnis variante =
            FlottenSimulator.Simuliere(Eingang(last), Config(250.0, adaptiv: false)).Variante;

        Assert.Null(variante.ErreichtesPeakZielKw);
        Assert.Equal(referenz.Jahresspitze, variante.MaximalerNetzbezugKw, 6);
        for (int t = 0; t < last.Length; t++)
        {
            Assert.Equal(250.0, variante.Intervalle[t].PeakZielKw!.Value, 10);
            Assert.Equal(referenz.Netzleistung[t], variante.Intervalle[t].NetzleistungKw, 6);
        }
    }

    [Fact]
    public void OhnePeakZielBleibtDieGanglinieDerSchwelleLeer()
    {
        FlottenStudieKonfiguration config = Config(GrundlastKw, adaptiv: true);
        config.Optionen.Betriebsziel = FlottenBetriebsziel.PvGreedy;
        config.Optionen.WirtschaftlicherPeakZielwertKw = null;

        FlottenSimulationErgebnis variante = FlottenSimulator.Simuliere(Eingang(Lastgang()), config).Variante;

        Assert.Null(variante.ErreichtesPeakZielKw);
        Assert.All(variante.Intervalle, x => Assert.Null(x.PeakZielKw));
    }

    [Fact]
    public void DerReferenzlaufOhneSpeicherKenntKeineRatsche()
    {
        // Ohne Einheiten gibt es keine Entladeleistung; eine Ratsche zöge die Schwelle
        // stur auf die Spitze und verfälschte die ausgewiesene Peakverletzung der
        // Referenz.
        FlottenStudienErgebnis studie =
            FlottenSimulator.Simuliere(Eingang(Lastgang()), Config(GrundlastKw, adaptiv: true));

        Assert.Null(studie.ReferenzOhneSpeicher.ErreichtesPeakZielKw);
        Assert.All(studie.ReferenzOhneSpeicher.Intervalle,
            x => Assert.Equal(GrundlastKw, x.PeakZielKw!.Value, 10));
    }

    // ================================================================= Prüfstand

    /// <summary>
    /// Der synthetische Viertelstundenlastgang: sieben Tage, Grundlast 60 kW, fünf
    /// Tagesspitzen von 250 bis 400 kW, am sechsten Abend ein 400-kW-Block von 23:00 bis
    /// 24:00 und unmittelbar danach die Spitze von 740 kW von 00:00 bis 00:45.
    /// </summary>
    /// <returns>672 Werte [kW].</returns>
    private static double[] Lastgang()
    {
        var last = new double[7 * 96];
        for (int i = 0; i < last.Length; i++) last[i] = GrundlastKw;

        void Block(int tag, int ab, int anzahl, double kw)
        {
            for (int i = 0; i < anzahl; i++) last[tag * 96 + ab + i] = kw;
        }

        Block(0, 40, 4, 250);     // 10:00-11:00
        Block(1, 40, 4, 300);
        Block(2, 40, 4, 350);
        Block(3, 40, 4, 400);
        Block(4, 40, 4, 250);
        Block(5, 92, 4, 400);     // 23:00-24:00 …
        Block(6, 0, 3, SpitzeKw); // … und ohne Ladepause weiter bis 00:45
        return last;
    }

    private static FlottenEingang Eingang(double[] last) => new()
    {
        KonfigurationId = "R",
        DatenId = "D",
        Istwerte = last.Select((kw, t) => new FlottenNetzintervall
        {
            Zeitstempel = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(15 * t),
            LastKw = kw,
            BezugspreisEuroProKWh = 0.3,
            PvVerkaufspreisEuroProKWh = 0.08,
            BhkwVerkaufspreisEuroProKWh = 0.12,
            BatterieVerkaufspreisEuroProKWh = 0.05
        }).ToList()
    };

    /// <summary>
    /// Die Flotte des Prüfstands: EINE Einheit, 400 kW / 400 kWh, keine Verluste, volles
    /// SoC-Fenster, leerer Start — genau die Annahmen, unter denen die Regel R und das
    /// Excel-Makro wortgleich sind.
    /// </summary>
    private static FlottenStudieKonfiguration Config(double h0, bool adaptiv) => new()
    {
        Einheiten = new List<FlottenEinheit>
        {
            new()
            {
                Id = "A",
                Name = "Prüfspeicher",
                KapazitaetKWh = KapazitaetKWh,
                LadeleistungKw = LeistungKw,
                EntladeleistungKw = LeistungKw,
                Ladewirkungsgrad = 1,
                Entladewirkungsgrad = 1,
                SocMin = 0,
                SocMax = 1,
                SocStart = 0
            }
        },
        Optionen = new FlottenSimulationOptionen
        {
            Betriebsziel = FlottenBetriebsziel.PeakShaving,
            WirtschaftlicherPeakZielwertKw = h0,
            PeakZielAdaptiv = adaptiv,
            NetzladungErlaubt = true,
            EnergieAusgleichEuroProKWh = 0
        }
    };

    /// <summary>Die Stufen der H-Treppe in ihrer Reihenfolge — jede nur einmal.</summary>
    private static List<double> Stufen(FlottenSimulationErgebnis lauf)
    {
        var stufen = new List<double>();
        foreach (FlottenIntervallErgebnis x in lauf.Intervalle)
            if (stufen.Count == 0 || Math.Abs(stufen[^1] - x.PeakZielKw!.Value) > 1e-9)
                stufen.Add(x.PeakZielKw!.Value);
        return stufen;
    }

    /// <summary>
    /// Das Vorausschau-Optimum M*: das kleinste FESTE Ziel, das die Flotte über den
    /// ganzen Lastgang hält. Dieselbe Bisektion wie <c>FlottenPeakZiel.PeakZielBestimmen</c>
    /// im Kern, hier ohne Fortschritt und mit enger Schranke.
    /// </summary>
    private static double VorausschauOptimum()
    {
        double[] last = Lastgang();
        FlottenEingang eingang = Eingang(last);
        double unten = GrundlastKw, oben = SpitzeKw, gefunden = SpitzeKw;
        for (int i = 0; i < 40 && oben - unten > 1e-6; i++)
        {
            double mitte = 0.5 * (unten + oben);
            double spitze = FlottenSimulator.Simuliere(eingang, Config(mitte, adaptiv: false))
                .Variante.MaximalerNetzbezugKw;
            if (spitze <= mitte + 1e-9) { oben = mitte; gefunden = mitte; }
            else unten = mitte;
        }
        return gefunden;
    }

    // =====================================================================
    //  Der Port des Excel-Makros (Modul2.calc_peakshaving)
    // =====================================================================

    /// <summary>Ein Lauf der Referenzrechnung: Jahresspitze, Schwelle, Netzleistung, Inhalt.</summary>
    private sealed record MakroLauf(double Jahresspitze, double SchwelleEnde,
                                    double[] Schwelle, double[] Netzleistung, double[] Ladezustand);

    /// <summary>
    /// Der wortgetreue Port des Excel-Makros <c>Modul2.calc_peakshaving</c>
    /// (Spezifikation 5.1.1, Δt = 0,25 h, SoC-Start 0):
    /// <code>
    /// dMax = min(P_max, SoC / Δt)
    /// wenn adaptiv und cv − dMax &gt; H:  H = cv − dMax
    /// wenn cv &gt; H:  d = min(dMax, cv − H); nv = cv − d; SoC −= d·Δt
    /// sonst:          c = min(P_max, H − cv, (E_max − SoC)/Δt); nv = cv + c; SoC += c·Δt
    /// </code>
    /// </summary>
    /// <param name="last">Der Lastgang [kW] im Viertelstundenraster.</param>
    /// <param name="h0">Die Startschwelle H0 [kW].</param>
    /// <param name="adaptiv">Ratsche an (Regel R) oder festes Ziel (Bestand).</param>
    /// <returns>Jahresspitze, Endschwelle und die drei Ganglinien.</returns>
    private static MakroLauf Makro(double[] last, double h0, bool adaptiv)
    {
        double h = h0, soc = 0.0, neuesMaximum = 0.0;
        var schwelle = new double[last.Length];
        var netz = new double[last.Length];
        var inhalt = new double[last.Length];

        for (int t = 0; t < last.Length; t++)
        {
            double cv = last[t];
            double dMax = Math.Min(LeistungKw, soc / Dt);
            if (adaptiv && cv - dMax > h) h = cv - dMax;

            double nv;
            if (cv > h)
            {
                double d = Math.Min(dMax, cv - h);
                nv = cv - d;
                soc -= d * Dt;
            }
            else
            {
                double c = Math.Min(LeistungKw, Math.Min(h - cv, (KapazitaetKWh - soc) / Dt));
                nv = cv + c;
                soc += c * Dt;
            }

            neuesMaximum = Math.Max(neuesMaximum, nv);
            schwelle[t] = h;
            netz[t] = nv;
            inhalt[t] = soc;
        }
        return new MakroLauf(neuesMaximum, h, schwelle, netz, inhalt);
    }
}
