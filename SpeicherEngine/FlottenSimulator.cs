using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SpeicherEngine;

/// <summary>Deterministischer AC-Flottensimulator mit Viertelstundenraster.</summary>
public static class FlottenSimulator
{
    private const double Dt = 0.25;
    private const double Eps = 1e-8;

    /// <summary>
    /// Rechnet EINE Variante vollstaendig: denselben Standortdatensatz einmal OHNE
    /// Speicher und einmal MIT der konfigurierten Flotte, beide mit demselben Tarif
    /// (Spezifikation 7.1 und 9.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Eingang und Konfiguration werden vor dem Lauf TIEF KOPIERT; spaetere Aenderungen
    /// am uebergebenen Objekt koennen den laufenden Rechenstand nicht mehr beeinflussen.
    /// </para>
    /// <para>
    /// Bei einem planenden <see cref="FlottenBetriebsziel"/> wird nach
    /// <see cref="FlottenSimulationOptionen.NeuplanungAlleIntervalle"/> mit der REAL
    /// erreichten Energie neu geplant; ausgefuehrt wird stets gegen den tatsaechlichen
    /// Zustand. Scheitert die Planung, bricht der Lauf ab — es sei denn,
    /// <see cref="FlottenSimulationOptionen.PrognoseFallbackErlaubt"/> gibt den
    /// protokollierten Rueckfall auf die reaktive Regel frei.
    /// </para>
    /// </remarks>
    /// <param name="input">Istwerte, archivierte Prognosen und Verfuegbarkeiten des Standorts.</param>
    /// <param name="config">Flotte, Betriebsoptionen, Tarif und Kapitalwerteingaben der Variante.</param>
    /// <param name="planer">Der Planer fuer die planenden Ziele; fuer die reaktiven Ziele <c>null</c>.</param>
    /// <param name="cancellationToken">Abbruchmarke; sie wird je Intervall geprueft.</param>
    /// <returns>Referenz und Variante samt Rechnungen, Endenergieaenderung und — bei vorliegenden Jahreskonten — Kapitalwert.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> oder <paramref name="config"/> ist <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Zeitraster, Einheiten, Reihen oder Optionen sind nicht schluessig.</exception>
    public static FlottenStudienErgebnis Simuliere(
        FlottenEingang input,
        FlottenStudieKonfiguration config,
        IFlottenPlaner? planer = null,
        CancellationToken cancellationToken = default)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (config is null) throw new ArgumentNullException(nameof(config));

        // Kein Aufrufer kann den laufenden Rechenstand durch spaetere DTO-Aenderungen beeinflussen.
        var i = FlottenKopie.Eingang(input);
        var c = FlottenKopie.Konfiguration(config);
        Pruefe(i, c);

        var referenz = SimuliereKern(i, c, Array.Empty<FlottenEinheit>(), null, cancellationToken);
        var variante = SimuliereKern(i, c, c.Einheiten, planer, cancellationToken);
        var referenzrechnung = RechneTarif(referenz, i.Istwerte, c.Tarif);
        var variantenrechnung = RechneTarif(variante, i.Istwerte, c.Tarif);

        var result = new FlottenStudienErgebnis
        {
            ReferenzOhneSpeicher = referenz,
            Variante = variante,
            Referenzrechnung = referenzrechnung,
            Variantenrechnung = variantenrechnung,
            EndenergieAenderungKWhJeSpeicher = variante.SpeicherKennzahlen
                .Select(x => x.EndenergieKWh - x.AnfangsenergieKWh).ToList()
        };
        result.EndenergieAusgleichEuro = result.EndenergieAenderungKWhJeSpeicher.Sum()
            * (c.Optionen.EnergieAusgleichEuroProKWh ?? 0);

        if (c.Wirtschaftlichkeit.Jahreskonten.Count > 0)
            result.Wirtschaftlichkeit = FlottenWirtschaftlichkeit.Bewerte(c.Wirtschaftlichkeit);

        return result;
    }

    /// <summary>
    /// Bildet die Stromrechnung EINER Abrechnungsperiode aus den am Netzanschluss
    /// bezogenen und eingespeisten Mengen (Spezifikation 9.1).
    /// </summary>
    /// <remarks>
    /// Die Rechnung addiert KEINE getrennt geschaetzten Batterieerloese: Bewertet wird
    /// allein, was ueber den Anschluss geflossen ist — Bezug zum Bezugspreis, die drei
    /// Einspeiseanteile zu ihren jeweiligen Verkaufspreisen. Der Leistungspreis wird
    /// einmal je Periode auf den hoechsten Netzbezug angesetzt. Referenz und Variante
    /// werden mit demselben Tarif gerechnet.
    /// </remarks>
    /// <param name="simulation">Der ausgefuehrte Lauf, dessen Intervalle abgerechnet werden.</param>
    /// <param name="eingang">Die Preisreihe derselben Laenge; sie liefert Bezugs- und Verkaufspreise je Intervall.</param>
    /// <param name="tarif">Leistungspreis, Fixkosten und der optionale eigene Batterieverkaufspreis.</param>
    /// <returns>Arbeits-, Leistungs- und Fixkosten [EUR], der abgerechnete Peak [kW] und ihre Summe.</returns>
    /// <exception cref="ArgumentNullException">Einer der drei Rechnungseingaenge ist <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Simulation und Preisreihe sind unterschiedlich lang.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Der Tarif enthaelt negative oder nicht endliche Werte.</exception>
    public static FlottenRechnung RechneTarif(
        FlottenSimulationErgebnis simulation,
        IReadOnlyList<FlottenNetzintervall> eingang,
        FlottenTarif tarif)
    {
        if (simulation is null || eingang is null || tarif is null)
            throw new ArgumentNullException("Rechnungseingang darf nicht null sein.");
        if (simulation.Intervalle.Count != eingang.Count)
            throw new ArgumentException("Simulation und Preisreihe muessen gleich lang sein.");
        if (!IstEndlichNichtNegativ(tarif.LeistungspreisEuroProKw) ||
            !IstEndlichNichtNegativ(tarif.FixkostenEuro) ||
            tarif.BatterieVerkaufspreisEuroProKWh is double batteriepreis && !double.IsFinite(batteriepreis))
            throw new ArgumentOutOfRangeException(nameof(tarif));

        var arbeit = 0.0;
        for (var n = 0; n < eingang.Count; n++)
        {
            var x = simulation.Intervalle[n];
            var p = eingang[n];
            arbeit += Dt * (x.NetzbezugKw * p.BezugspreisEuroProKWh
                - x.PvNetzeinspeisungKw * p.PvVerkaufspreisEuroProKWh
                - x.BhkwNetzeinspeisungKw * p.BhkwVerkaufspreisEuroProKWh
                - x.BatterieNetzeinspeisungKw * (tarif.BatterieVerkaufspreisEuroProKWh
                    ?? p.BatterieVerkaufspreisEuroProKWh));
        }

        var peak = simulation.Intervalle.Count == 0 ? 0 : simulation.Intervalle.Max(x => x.NetzbezugKw);
        var leistung = peak * tarif.LeistungspreisEuroProKw;
        return new FlottenRechnung
        {
            EnergiekostenEuro = arbeit,
            LeistungskostenEuro = leistung,
            FixkostenEuro = tarif.FixkostenEuro,
            PeakKw = peak,
            GesamtEuro = arbeit + leistung + tarif.FixkostenEuro
        };
    }

    private static FlottenSimulationErgebnis SimuliereKern(
        FlottenEingang input,
        FlottenStudieKonfiguration config,
        IReadOnlyList<FlottenEinheit> einheiten,
        IFlottenPlaner? planer,
        CancellationToken cancellationToken)
    {
        var o = config.Optionen;
        var ergebnis = new FlottenSimulationErgebnis
        {
            KonfigurationId = input.KonfigurationId,
            DatenId = input.DatenId,
            Zulaessig = true
        };
        var energie = einheiten.Select(x => x.SocStart * x.KapazitaetKWh).ToArray();
        var anfang = (double[])energie.Clone();
        var ladeenergie = new double[einheiten.Count];
        var entladeenergie = new double[einheiten.Count];
        var socPfade = new List<double>[einheiten.Count];
        for (var j = 0; j < einheiten.Count; j++)
            socPfade[j] = new List<double> { einheiten[j].SocStart };

        FlottenPlan? plan = null;
        var planStart = -1;
        string? fallbackGrund = null;
        var hilfsleistung = einheiten.Sum(x => x.HilfsverbrauchKw);
        var bisherigerPeak = 0.0;

        for (var t = 0; t < input.Istwerte.Count; t++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = input.Istwerte[t];
            var ziel = o.Betriebsziel;
            var planend = einheiten.Count > 0 &&
                ziel is FlottenBetriebsziel.PvPlanung or FlottenBetriebsziel.Arbitrage or FlottenBetriebsziel.MultiUse;
            if (planend && (plan is null || t - planStart >= o.NeuplanungAlleIntervalle))
            {
                plan = null;
                fallbackGrund = null;
                try
                {
                    if (planer is null)
                        throw new InvalidOperationException("Kein IFlottenPlaner wurde bereitgestellt.");
                    var required = Math.Min(o.PlanungshorizontIntervalle, input.Istwerte.Count - t);
                    var snapshot = WaehleSnapshot(input.Prognosen, row.Zeitstempel, o.PrognoseArt, required);
                    var sliced = snapshot.Ausschnitt(row.Zeitstempel, required);
                    plan = planer.Plane(new FlottenPlanAnfrage
                    {
                        Entscheidungszeitpunkt = row.Zeitstempel,
                        Modus = ziel == FlottenBetriebsziel.PvPlanung ? FlottenPlanerModus.PvPlanung
                            : ziel == FlottenBetriebsziel.Arbitrage ? FlottenPlanerModus.Arbitrage
                            : FlottenPlanerModus.MultiUse,
                        // Der Ausschnitt teilt den unveraenderlichen internen Snapshot-Puffer; keine Zeilenkopie je Neuplanung.
                        Prognose = sliced,
                        Einheiten = einheiten.Select(FlottenKopie.Einheit).ToList(),
                        EnergieKWh = energie.ToList(),
                        Optionen = FlottenKopie.Optionen(o),
                        BisherigerAbrechnungspeakKw = bisherigerPeak,
                        LeistungspreisEuroProKw = config.Tarif.LeistungspreisEuroProKw,
                        EndenergieAusgleichEuroProKWh = o.EnergieAusgleichEuroProKWh ?? 0,
                        VerfuegbarkeitsfaktorJeSpeicher = BaueVerfuegbarkeit(input, einheiten, t, sliced.Intervalle.Count)
                    }, cancellationToken);
                    PruefePlan(plan, sliced, row.Zeitstempel, einheiten.Count);
                    planStart = t;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    if (!o.PrognoseFallbackErlaubt) throw;
                    fallbackGrund = ex.Message;
                    planStart = t;
                }
            }

            var nOhneSpeicher = row.LastKw - row.PvKw - row.BhkwKw + hilfsleistung;
            var release = o.WirtschaftlicherPeakZielwertKw.HasValue && nOhneSpeicher > o.WirtschaftlicherPeakZielwertKw.Value;
            var soll = new double[einheiten.Count];
            var curtailRequest = 0.0;
            if (planend && plan is not null && fallbackGrund is null)
            {
                var k = t - planStart;
                if (k < 0 || k >= plan.Intervalle.Count || plan.Intervalle[k].Zeitstempel != row.Zeitstempel)
                    throw new InvalidOperationException("Der Fahrplan deckt das Ausfuehrungsintervall nicht ab.");
                var pi = plan.Intervalle[k];
                for (var j = 0; j < einheiten.Count; j++)
                    soll[j] = pi.EntladeleistungKwJeSpeicher[j] - pi.LadeleistungKwJeSpeicher[j];
                curtailRequest = pi.PvAbregelungKw;
                if (ziel == FlottenBetriebsziel.MultiUse && release)
                {
                    var peakBedarf = nOhneSpeicher - o.WirtschaftlicherPeakZielwertKw!.Value;
                    if (soll.Sum() < peakBedarf - Eps)
                        soll = Verteile(Math.Max(soll.Sum(), peakBedarf), einheiten, energie, o.Verteilung,
                            true, row.Zeitstempel, AktuelleVerfuegbarkeit(input, einheiten, t));
                }
            }
            else
            {
                var effektivesZiel = ziel == FlottenBetriebsziel.MultiUse ? FlottenBetriebsziel.PeakShaving
                    : planend ? FlottenBetriebsziel.PvGreedy : ziel;
                var request = BestimmeReaktiveAnforderung(effektivesZiel, nOhneSpeicher, row, o);
                soll = Verteile(request, einheiten, energie, o.Verteilung, release, row.Zeitstempel,
                    AktuelleVerfuegbarkeit(input, einheiten, t));
            }

            var interval = FuehreAus(row, einheiten, energie, soll, curtailRequest, o,
                AktuelleVerfuegbarkeit(input, einheiten, t));
            if (fallbackGrund is not null)
            {
                interval.PlanFallback = true;
                interval.PlanFallbackGrund = fallbackGrund;
                ergebnis.PlanFallbackIntervalle++;
            }
            else if (plan is not null)
            {
                interval.PlanId = plan.PlanId;
                interval.PrognoseId = plan.PrognoseId;
            }

            ergebnis.Intervalle.Add(interval);
            energie = interval.EnergieEndeKWhJeSpeicher.ToArray();
            bisherigerPeak = Math.Max(bisherigerPeak, interval.NetzbezugKw);
            ergebnis.NetzbezugKWh += interval.NetzbezugKw * Dt;
            ergebnis.NetzeinspeisungKWh += interval.NetzeinspeisungKw * Dt;
            ergebnis.PvAbregelungKWh += interval.PvAbregelungKw * Dt;
            ergebnis.VerlusteKWh += interval.UmwandlungsverlustKWh;
            ergebnis.MaximalerNetzbezugKw = Math.Max(ergebnis.MaximalerNetzbezugKw, interval.NetzbezugKw);
            if (interval.TechnischeImportverletzungKw > Eps)
            {
                ergebnis.Zulaessig = false;
                ergebnis.Unzulaessigkeitsgrund ??= "Technische Netzbezugsgrenze ueberschritten; es wurde keine Last abgeworfen.";
            }
            if (interval.TechnischeExportverletzungKw > Eps)
            {
                ergebnis.Zulaessig = false;
                ergebnis.Unzulaessigkeitsgrund ??= "Technische Netzeinspeisungsgrenze ueberschritten; nicht abregelbare Erzeugung blieb sichtbar.";
            }
            for (var j = 0; j < einheiten.Count; j++)
            {
                var p = interval.IstleistungKwJeSpeicher[j];
                if (p < 0) ladeenergie[j] += -p * Dt;
                else entladeenergie[j] += p * Dt;
                socPfade[j].Add(energie[j] / einheiten[j].KapazitaetKWh);
            }
        }

        for (var j = 0; j < einheiten.Count; j++)
        {
            var ziel = o.Endbedingung == FlottenEndbedingung.JeSpeicherWieAnfang ? anfang[j]
                : o.Endbedingung == FlottenEndbedingung.JeSpeicherZiel ? o.EndenergieZielKWh[j]
                : energie[j];
            if (o.Endbedingung != FlottenEndbedingung.KeineVorgabe && Math.Abs(energie[j] - ziel) > 1e-5)
            {
                ergebnis.Zulaessig = false;
                ergebnis.Unzulaessigkeitsgrund ??= "Die geforderte Endenergie je Speicher wurde nicht erreicht.";
            }
            var rain = FlottenRainflow.Auswerten(socPfade[j], einheiten[j].RainflowKurve);
            ergebnis.SpeicherKennzahlen.Add(new FlottenSpeicherKennzahlen
            {
                SpeicherId = einheiten[j].Id,
                AnfangsenergieKWh = anfang[j],
                EndenergieKWh = energie[j],
                LadeenergieAcKWh = ladeenergie[j],
                EntladeenergieAcKWh = entladeenergie[j],
                AequivalenteVollzyklen = (einheiten[j].Ladewirkungsgrad * ladeenergie[j]
                    + entladeenergie[j] / einheiten[j].Entladewirkungsgrad) / (2 * einheiten[j].KapazitaetKWh),
                RainflowSchaden = rain.Schaden,
                RainflowZyklen = rain.Zyklen
            });
        }
        return ergebnis;
    }

    private static FlottenIntervallErgebnis FuehreAus(
        FlottenNetzintervall row,
        IReadOnlyList<FlottenEinheit> einheiten,
        IReadOnlyList<double> energie,
        IReadOnlyList<double> commands,
        double curtailRequest,
        FlottenSimulationOptionen o,
        IReadOnlyList<double> verfuegbarkeit)
    {
        if (commands.Count != einheiten.Count || energie.Count != einheiten.Count)
            throw new ArgumentException("Je Speicher werden Zustand und Sollleistung benoetigt.");
        if (commands.Any(x => !double.IsFinite(x))) throw new ArgumentException("Nicht endliche Sollleistung.");
        if (commands.Any(x => x < -Eps) && commands.Any(x => x > Eps))
            throw new InvalidOperationException("Gleichzeitiges Laden und Entladen innerhalb der Flotte ist verboten.");

        var aux = einheiten.Sum(x => x.HilfsverbrauchKw);
        var vorPvVerbrauch = o.ErzeugerPrioritaet == FlottenErzeugerPrioritaet.PvVorBhkw
            ? row.LastKw + aux
            : Math.Max(0, row.LastKw + aux - row.BhkwKw);
        var maxCurtail = Math.Max(0, row.PvKw - vorPvVerbrauch);
        var curtail = Math.Min(Math.Max(0, curtailRequest), maxCurtail);
        var n = row.LastKw - row.PvKw - row.BhkwKw + aux + curtail;
        var release = o.WirtschaftlicherPeakZielwertKw.HasValue && n > o.WirtschaftlicherPeakZielwertKw.Value;
        var ist = new double[einheiten.Count];
        for (var j = 0; j < einheiten.Count; j++)
        {
            var (charge, discharge) = Grenzen(einheiten[j], energie[j], release, verfuegbarkeit[j]);
            ist[j] = Math.Max(-charge, Math.Min(discharge, commands[j]));
        }

        var total = ist.Sum();
        var importLimit = o.NetzbezugGrenzeKw ?? double.MaxValue / 4;
        var exportLimit = o.NetzeinspeisungGrenzeKw ?? double.MaxValue / 4;
        var chargeCeiling = Math.Max(0, importLimit - n);
        if (o.WirtschaftlicherPeakZielwertKw.HasValue)
            chargeCeiling = Math.Min(chargeCeiling, Math.Max(0, o.WirtschaftlicherPeakZielwertKw.Value - n));
        if (!o.NetzladungErlaubt) chargeCeiling = Math.Min(chargeCeiling, Math.Max(0, -n));
        var allowed = Math.Max(total, -chargeCeiling);
        if (!o.BatterieexportErlaubt) allowed = Math.Min(allowed, Math.Max(0, n));
        allowed = Math.Min(allowed, Math.Max(0, n + exportLimit));
        if (Math.Abs(total) > Eps && Math.Abs(allowed) < Math.Abs(total))
            for (var j = 0; j < ist.Length; j++) ist[j] *= allowed / total;

        var next = new double[einheiten.Count];
        var losses = new double[einheiten.Count];
        var loss = 0.0;
        for (var j = 0; j < einheiten.Count; j++)
        {
            var charge = Math.Max(0, -ist[j]);
            var discharge = Math.Max(0, ist[j]);
            next[j] = energie[j] + Dt * (einheiten[j].Ladewirkungsgrad * charge
                - discharge / einheiten[j].Entladewirkungsgrad);
            losses[j] = Dt * ((1 - einheiten[j].Ladewirkungsgrad) * charge
                + (1 / einheiten[j].Entladewirkungsgrad - 1) * discharge);
            loss += losses[j];
            var emin = einheiten[j].SocMin * einheiten[j].KapazitaetKWh;
            var emax = einheiten[j].SocMax * einheiten[j].KapazitaetKWh;
            if (next[j] < emin - 1e-6 || next[j] > emax + 1e-6)
                throw new ArithmeticException("Energiebilanz verletzt die Speichergrenzen.");
        }

        var rawGrid = n - ist.Sum();
        var extraCurtail = Math.Min(maxCurtail - curtail, Math.Max(0, -exportLimit - rawGrid));
        curtail += extraCurtail;
        var grid = rawGrid + extraCurtail;
        var export = Math.Max(0, -grid);
        var chargeKw = ist.Where(x => x < 0).Sum(x => -x);
        var directExport = Math.Min(export,
            Math.Max(0, row.PvKw - curtail + row.BhkwKw - row.LastKw - aux - chargeKw));
        var batteryExport = Math.Max(0, export - directExport);
        var (pvExport, bhkwExport) = TeileDirektexport(row.PvKw - curtail, row.BhkwKw,
            directExport, o.ErzeugerPrioritaet);

        return new FlottenIntervallErgebnis
        {
            Zeitstempel = row.Zeitstempel,
            LastKw = row.LastKw,
            PvVerfuegbarKw = row.PvKw,
            BhkwKw = row.BhkwKw,
            HilfsverbrauchKw = aux,
            NetzleistungKw = grid,
            NetzbezugKw = Math.Max(0, grid),
            NetzeinspeisungKw = export,
            PvNetzeinspeisungKw = pvExport,
            BhkwNetzeinspeisungKw = bhkwExport,
            BatterieNetzeinspeisungKw = batteryExport,
            PvAbregelungKw = curtail,
            UmwandlungsverlustKWh = loss,
            UmwandlungsverlustKWhJeSpeicher = losses.ToList(),
            SollabweichungKw = commands.Zip(ist, (s, actual) => Math.Abs(s - actual)).Sum(),
            TechnischeImportverletzungKw = o.NetzbezugGrenzeKw.HasValue
                ? Math.Max(0, grid - o.NetzbezugGrenzeKw.Value) : 0,
            TechnischeExportverletzungKw = o.NetzeinspeisungGrenzeKw.HasValue
                ? Math.Max(0, -grid - o.NetzeinspeisungGrenzeKw.Value) : 0,
            WirtschaftlichePeakverletzungKw = o.WirtschaftlicherPeakZielwertKw.HasValue
                ? Math.Max(0, grid - o.WirtschaftlicherPeakZielwertKw.Value) : 0,
            SollleistungKwJeSpeicher = commands.ToList(),
            IstleistungKwJeSpeicher = ist.ToList(),
            EnergieStartKWhJeSpeicher = energie.ToList(),
            EnergieEndeKWhJeSpeicher = next.ToList()
        };
    }

    private static (double Pv, double Bhkw) TeileDirektexport(double pv, double bhkw, double export,
        FlottenErzeugerPrioritaet prioritaet)
    {
        var eigenverbrauch = Math.Max(0, pv + bhkw - export);
        double pvVerbraucht;
        double bhkwVerbraucht;
        if (prioritaet == FlottenErzeugerPrioritaet.PvVorBhkw)
        {
            pvVerbraucht = Math.Min(pv, eigenverbrauch);
            bhkwVerbraucht = Math.Min(bhkw, eigenverbrauch - pvVerbraucht);
        }
        else
        {
            bhkwVerbraucht = Math.Min(bhkw, eigenverbrauch);
            pvVerbraucht = Math.Min(pv, eigenverbrauch - bhkwVerbraucht);
        }
        return (Math.Max(0, pv - pvVerbraucht), Math.Max(0, bhkw - bhkwVerbraucht));
    }

    private static double BestimmeReaktiveAnforderung(FlottenBetriebsziel ziel, double n,
        FlottenNetzintervall row, FlottenSimulationOptionen o)
    {
        return ziel switch
        {
            FlottenBetriebsziel.PeakShaving => n - (o.WirtschaftlicherPeakZielwertKw
                ?? throw new InvalidOperationException("Peak Shaving benoetigt einen wirtschaftlichen Peak-Zielwert.")),
            FlottenBetriebsziel.PvGreedy => n,
            FlottenBetriebsziel.Arbitrage when o.ArbitrageLadepreisSchwelle.HasValue &&
                row.BezugspreisEuroProKWh <= o.ArbitrageLadepreisSchwelle.Value => double.NegativeInfinity,
            FlottenBetriebsziel.Arbitrage when o.ArbitrageEntladepreisSchwelle.HasValue &&
                row.BezugspreisEuroProKWh >= o.ArbitrageEntladepreisSchwelle.Value => Math.Max(0, n),
            _ => 0
        };
    }

    private static double[] Verteile(double request, IReadOnlyList<FlottenEinheit> einheiten,
        IReadOnlyList<double> energie, FlottenVerteilung modus, bool release, DateTimeOffset zeit,
        IReadOnlyList<double> verfuegbarkeit)
    {
        var result = new double[einheiten.Count];
        if (einheiten.Count == 0 || Math.Abs(request) < Eps) return result;
        var discharge = request > 0;
        var caps = new double[einheiten.Count];
        for (var j = 0; j < einheiten.Count; j++)
        {
            var limits = Grenzen(einheiten[j], energie[j], release, verfuegbarkeit[j]);
            caps[j] = discharge ? limits.Discharge : limits.Charge;
        }
        var remaining = double.IsInfinity(request) ? caps.Sum() : Math.Min(Math.Abs(request), caps.Sum());

        if (modus == FlottenVerteilung.KapazitaetsProportional)
        {
            var weights = new double[einheiten.Count];
            var active = new HashSet<int>();
            for (var j = 0; j < einheiten.Count; j++)
            {
                var floor = einheiten[j].SocMin * einheiten[j].KapazitaetKWh +
                    (release ? 0 : einheiten[j].PeakReserveKWh);
                weights[j] = Math.Max(Eps, discharge ? energie[j] - floor
                    : einheiten[j].SocMax * einheiten[j].KapazitaetKWh - energie[j]);
                if (caps[j] > Eps) active.Add(j);
            }
            while (remaining > Eps && active.Count > 0)
            {
                var weight = active.Sum(j => weights[j]);
                var saturated = new List<int>();
                foreach (var j in active)
                {
                    var proposed = remaining * weights[j] / weight;
                    if (proposed >= caps[j] - result[j] - Eps) saturated.Add(j);
                }
                if (saturated.Count == 0)
                {
                    foreach (var j in active) result[j] += remaining * weights[j] / weight;
                    remaining = 0;
                }
                else
                {
                    foreach (var j in saturated)
                    {
                        var add = caps[j] - result[j];
                        result[j] += add;
                        remaining -= add;
                        active.Remove(j);
                    }
                }
            }
        }
        else
        {
            var order = Enumerable.Range(0, einheiten.Count).ToList();
            if (modus == FlottenVerteilung.Kaskade)
            {
                var day = (long)Math.Floor(zeit.ToUniversalTime().ToUnixTimeSeconds() / 86400.0);
                var shift = (int)(((day % order.Count) + order.Count) % order.Count);
                order = order.Skip(shift).Concat(order.Take(shift)).ToList();
            }
            else
            {
                order.Sort((a, b) =>
                {
                    var cost = discharge
                        ? einheiten[a].GrenzverschleissEuroProKWhEntladung.CompareTo(einheiten[b].GrenzverschleissEuroProKWhEntladung)
                        : einheiten[b].Ladewirkungsgrad.CompareTo(einheiten[a].Ladewirkungsgrad);
                    if (cost != 0) return cost;
                    var eta = einheiten[b].Entladewirkungsgrad.CompareTo(einheiten[a].Entladewirkungsgrad);
                    return eta != 0 ? eta : string.CompareOrdinal(einheiten[a].Id, einheiten[b].Id);
                });
            }
            foreach (var j in order)
            {
                var p = Math.Min(caps[j], remaining);
                result[j] = p;
                remaining -= p;
                if (remaining <= Eps) break;
            }
        }
        if (!discharge) for (var j = 0; j < result.Length; j++) result[j] = -result[j];
        return result;
    }

    private static (double Charge, double Discharge) Grenzen(FlottenEinheit b, double e, bool release,
        double verfuegbarkeit)
    {
        var emin = b.SocMin * b.KapazitaetKWh;
        var emax = b.SocMax * b.KapazitaetKWh;
        if (e < emin - 1e-6 || e > emax + 1e-6) throw new ArgumentOutOfRangeException(nameof(e));
        var floor = emin + (release ? 0 : b.PeakReserveKWh);
        var discharge = Math.Min(b.EntladeleistungKw * verfuegbarkeit, Math.Max(0, e - floor) * b.Entladewirkungsgrad / Dt);
        var charge = Math.Min(b.LadeleistungKw * verfuegbarkeit, Math.Max(0, emax - e) / (b.Ladewirkungsgrad * Dt));
        return (charge, discharge);
    }

    private static FlottenPrognoseSnapshot WaehleSnapshot(IReadOnlyList<FlottenPrognoseSnapshot> snapshots,
        DateTimeOffset entscheidung, PrognoseArt art, int benoetigteIntervalle)
    {
        var candidates = snapshots.Where(x => x.Art == art && x.Entscheidungszeitpunkt <= entscheidung);
        if (art == PrognoseArt.VerifiziertBekannt)
            candidates = candidates.Where(x => x.BekanntSeit <= entscheidung);
        // Auch Oracle-Zukunftsdaten muessen als eigener Snapshot explizit geliefert sein.
        candidates = candidates.Where(x => DecktAb(x, entscheidung, benoetigteIntervalle));
        var selected = candidates.OrderByDescending(x => x.Entscheidungszeitpunkt)
            .ThenByDescending(x => x.BekanntSeit).FirstOrDefault();
        return selected ?? throw new InvalidOperationException($"Kein expliziter Prognose-Snapshot der Art {art} ist verfuegbar.");
    }

    private static bool DecktAb(FlottenPrognoseSnapshot snapshot, DateTimeOffset start, int count)
    {
        for (var i = 0; i < snapshot.Intervalle.Count; i++)
            if (snapshot.Intervalle[i].Zeitstempel == start)
                return snapshot.Intervalle.Count - i >= count;
        return false;
    }

    private static void PruefePlan(FlottenPlan plan, FlottenPrognoseSnapshot prognose,
        DateTimeOffset entscheidung, int count)
    {
        if (plan is null) throw new InvalidOperationException("Planer gab keinen Fahrplan zurueck.");
        if (plan.Status is not (FlottenPlanStatus.Optimal or FlottenPlanStatus.Zulaessig))
            throw new InvalidOperationException($"Planerstatus {plan.Status}: {plan.StatusText}");
        if (plan.PrognoseId != prognose.Id || plan.Entscheidungszeitpunkt != entscheidung)
            throw new InvalidOperationException("Fahrplan und Prognose-Snapshot passen nicht zusammen.");
        if (plan.Intervalle.Count != prognose.Intervalle.Count)
            throw new InvalidOperationException("Fahrplan und Prognosehorizont sind verschieden lang.");
        for (var t = 0; t < plan.Intervalle.Count; t++)
        {
            var x = plan.Intervalle[t];
            if (x.Zeitstempel != prognose.Intervalle[t].Zeitstempel)
                throw new InvalidOperationException("Fahrplan-Zeitachse stimmt nicht mit der Prognose ueberein.");
            if (x.LadeleistungKwJeSpeicher.Count != count || x.EntladeleistungKwJeSpeicher.Count != count)
                throw new InvalidOperationException("Fahrplan hat nicht fuer jede Einheit einen Wert.");
            if (x.LadeleistungKwJeSpeicher.Any(v => v < -Eps || !double.IsFinite(v)) ||
                x.EntladeleistungKwJeSpeicher.Any(v => v < -Eps || !double.IsFinite(v)))
                throw new InvalidOperationException("Fahrplanleistung ist ungueltig.");
            if (x.LadeleistungKwJeSpeicher.Sum() > Eps && x.EntladeleistungKwJeSpeicher.Sum() > Eps)
                throw new InvalidOperationException("Der Fahrplan laedt und entlaedt die Flotte gleichzeitig.");
        }
    }

    private static List<double> AktuelleVerfuegbarkeit(FlottenEingang input,
        IReadOnlyList<FlottenEinheit> units, int interval)
    {
        var result = new List<double>(units.Count);
        foreach (var unit in units)
            result.Add(input.VerfuegbarkeitsfaktorNachEinheitId.TryGetValue(unit.Id, out var values)
                ? values[interval] : 1.0);
        return result;
    }

    private static List<List<double>> BaueVerfuegbarkeit(FlottenEingang input,
        IReadOnlyList<FlottenEinheit> units, int start, int intervals)
    {
        var result = new List<List<double>>(units.Count);
        foreach (var unit in units)
        {
            if (input.VerfuegbarkeitsfaktorNachEinheitId.TryGetValue(unit.Id, out var values))
                result.Add(values.Skip(start).Take(intervals).ToList());
            else
                result.Add(Enumerable.Repeat(1.0, intervals).ToList());
        }
        return result;
    }

    private static void Pruefe(FlottenEingang input, FlottenStudieKonfiguration config)
    {
        if (input.Istwerte.Count == 0) throw new ArgumentException("Die Zeitreihe ist leer.");
        DateTimeOffset? previous = null;
        foreach (var x in input.Istwerte)
        {
            if (x.Zeitstempel.Offset != TimeSpan.Zero)
                throw new ArgumentException("Die Flottenzeitachse muss explizit in UTC vorliegen.");
            if (x.Zeitstempel.Ticks % TimeSpan.FromMinutes(15).Ticks != 0)
                throw new ArgumentException("Zeitstempel liegt nicht auf dem Viertelstundenraster.");
            if (previous.HasValue && x.Zeitstempel - previous.Value != TimeSpan.FromMinutes(15))
                throw new ArgumentException("Zeitreihe enthaelt Luecke, Duplikat oder falsche Reihenfolge.");
            if (!IstEndlichNichtNegativ(x.LastKw) || !IstEndlichNichtNegativ(x.PvKw) ||
                !IstEndlichNichtNegativ(x.BhkwKw) || !double.IsFinite(x.BezugspreisEuroProKWh) ||
                !double.IsFinite(x.PvVerkaufspreisEuroProKWh) || !double.IsFinite(x.BhkwVerkaufspreisEuroProKWh) ||
                !double.IsFinite(x.BatterieVerkaufspreisEuroProKWh))
                throw new ArgumentException("Ungueltige Last-, Erzeugungs- oder Preiswerte.");
            previous = x.Zeitstempel;
        }
        if (config.Einheiten.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != config.Einheiten.Count)
            throw new ArgumentException("Speicher-IDs muessen eindeutig sein.");
        foreach (var b in config.Einheiten) PruefeEinheit(b);
        foreach (var pair in input.VerfuegbarkeitsfaktorNachEinheitId)
            if (pair.Value.Count != input.Istwerte.Count || pair.Value.Any(v => !double.IsFinite(v) || v < 0 || v > 1))
                throw new ArgumentException($"Ungueltige Verfuegbarkeitsreihe fuer '{pair.Key}'.");
        var o = config.Optionen;
        if (!IstOptionaleNichtnegativeZahl(o.NetzbezugGrenzeKw) ||
            !IstOptionaleNichtnegativeZahl(o.NetzeinspeisungGrenzeKw) ||
            !IstOptionaleNichtnegativeZahl(o.WirtschaftlicherPeakZielwertKw) ||
            !IstOptionaleEndlicheZahl(o.ArbitrageLadepreisSchwelle) ||
            !IstOptionaleEndlicheZahl(o.ArbitrageEntladepreisSchwelle) ||
            !IstOptionaleNichtnegativeZahl(o.EnergieAusgleichEuroProKWh) ||
            o.PlanungshorizontIntervalle <= 0 || o.NeuplanungAlleIntervalle <= 0)
            throw new ArgumentException("Ungueltige Anschluss- oder Planungsgrenze.");
        if (o.Betriebsziel == FlottenBetriebsziel.PeakShaving && o.WirtschaftlicherPeakZielwertKw is null)
            throw new ArgumentException("Peak Shaving benoetigt einen Zielwert.");
        if (o.Endbedingung == FlottenEndbedingung.JeSpeicherZiel && o.EndenergieZielKWh.Count != config.Einheiten.Count)
            throw new ArgumentException("Endenergieziel muss je Speicher vorliegen.");
        if (o.Endbedingung == FlottenEndbedingung.KeineVorgabe && config.Einheiten.Count > 0 &&
            o.EnergieAusgleichEuroProKWh is null)
            throw new ArgumentException("Ohne Endenergiegleichheit ist ein offengelegter Energie-Ausgleichswert erforderlich.");
    }

    private static void PruefeEinheit(FlottenEinheit b)
    {
        if (string.IsNullOrWhiteSpace(b.Id) || string.IsNullOrWhiteSpace(b.Name) ||
            !IstEndlichPositiv(b.KapazitaetKWh) || !IstEndlichNichtNegativ(b.LadeleistungKw) ||
            !IstEndlichNichtNegativ(b.EntladeleistungKw) || !IstWirkungsgrad(b.Ladewirkungsgrad) ||
            !IstWirkungsgrad(b.Entladewirkungsgrad) || b.SocMin < 0 || b.SocMax > 1 ||
            b.SocMin > b.SocStart || b.SocStart > b.SocMax ||
            !IstEndlichNichtNegativ(b.PeakReserveKWh) ||
            b.PeakReserveKWh > (b.SocMax - b.SocMin) * b.KapazitaetKWh + Eps ||
            !IstEndlichNichtNegativ(b.HilfsverbrauchKw) ||
            !IstEndlichNichtNegativ(b.GrenzverschleissEuroProKWhEntladung) ||
            !IstEndlichNichtNegativ(b.InvestitionEuro) || !IstEndlichNichtNegativ(b.InvestitionEuroProKWh) ||
            !IstEndlichNichtNegativ(b.InvestitionEuroProKw) || !IstEndlichNichtNegativ(b.JaehrlicheFixeOpexEuro) ||
            !IstEndlichNichtNegativ(b.JaehrlicheOpexEuroProKWhKapazitaet) ||
            !IstEndlichNichtNegativ(b.JaehrlicheOpexEuroProKw) ||
            !IstEndlichNichtNegativ(b.DurchsatzkostenEuroProKWhEntladung) ||
            !IstEndlichNichtNegativ(b.ErsatzkostenEuro) || b.ErsatzintervallJahre < 0 ||
            !IstEndlichNichtNegativ(b.RestwertEuro))
            throw new ArgumentException($"Ungueltige Parameter fuer Speicher '{b.Id}'.");
    }

    private static bool IstWirkungsgrad(double x) => double.IsFinite(x) && x > 0 && x <= 1;
    private static bool IstEndlichPositiv(double x) => double.IsFinite(x) && x > 0;
    private static bool IstEndlichNichtNegativ(double x) => double.IsFinite(x) && x >= 0;
    private static bool IstOptionaleNichtnegativeZahl(double? x) => !x.HasValue || IstEndlichNichtNegativ(x.Value);
    private static bool IstOptionaleEndlicheZahl(double? x) => !x.HasValue || double.IsFinite(x.Value);
}

internal static class FlottenKopie
{
    internal static FlottenEinheit Einheit(FlottenEinheit x) => new()
    {
        Id = x.Id, Name = x.Name, AnlageId = x.AnlageId, EigeneKosten = x.EigeneKosten,
        KapazitaetKWh = x.KapazitaetKWh, LadeleistungKw = x.LadeleistungKw,
        EntladeleistungKw = x.EntladeleistungKw, Ladewirkungsgrad = x.Ladewirkungsgrad,
        Entladewirkungsgrad = x.Entladewirkungsgrad, SocMin = x.SocMin, SocMax = x.SocMax,
        SocStart = x.SocStart, PeakReserveKWh = x.PeakReserveKWh, HilfsverbrauchKw = x.HilfsverbrauchKw,
        GrenzverschleissEuroProKWhEntladung = x.GrenzverschleissEuroProKWhEntladung,
        InvestitionEuro = x.InvestitionEuro, InvestitionEuroProKWh = x.InvestitionEuroProKWh,
        InvestitionEuroProKw = x.InvestitionEuroProKw, JaehrlicheFixeOpexEuro = x.JaehrlicheFixeOpexEuro,
        JaehrlicheOpexEuroProKWhKapazitaet = x.JaehrlicheOpexEuroProKWhKapazitaet,
        JaehrlicheOpexEuroProKw = x.JaehrlicheOpexEuroProKw,
        DurchsatzkostenEuroProKWhEntladung = x.DurchsatzkostenEuroProKWhEntladung,
        ErsatzkostenEuro = x.ErsatzkostenEuro, ErsatzintervallJahre = x.ErsatzintervallJahre,
        RestwertEuro = x.RestwertEuro,
        RainflowKurve = x.RainflowKurve.Select(p => new FlottenRainflowPunkt
            { Entladetiefe = p.Entladetiefe, ZyklenBisEol = p.ZyklenBisEol }).ToList()
    };

    internal static FlottenSimulationOptionen Optionen(FlottenSimulationOptionen x) => new()
    {
        Betriebsziel = x.Betriebsziel, Verteilung = x.Verteilung, NetzbezugGrenzeKw = x.NetzbezugGrenzeKw,
        NetzeinspeisungGrenzeKw = x.NetzeinspeisungGrenzeKw, NetzladungErlaubt = x.NetzladungErlaubt,
        BatterieexportErlaubt = x.BatterieexportErlaubt, WirtschaftlicherPeakZielwertKw = x.WirtschaftlicherPeakZielwertKw,
        ArbitrageLadepreisSchwelle = x.ArbitrageLadepreisSchwelle,
        ArbitrageEntladepreisSchwelle = x.ArbitrageEntladepreisSchwelle, PrognoseArt = x.PrognoseArt,
        ErzeugerPrioritaet = x.ErzeugerPrioritaet, PlanungshorizontIntervalle = x.PlanungshorizontIntervalle,
        NeuplanungAlleIntervalle = x.NeuplanungAlleIntervalle, Endbedingung = x.Endbedingung,
        EndenergieZielKWh = new List<double>(x.EndenergieZielKWh), EnergieAusgleichEuroProKWh = x.EnergieAusgleichEuroProKWh,
        PrognoseFallbackErlaubt = x.PrognoseFallbackErlaubt
    };

    internal static FlottenEingang Eingang(FlottenEingang x) => new()
    {
        KonfigurationId = x.KonfigurationId, DatenId = x.DatenId,
        Istwerte = x.Istwerte.Select(Netzintervall).ToList(),
        Prognosen = x.Prognosen.Select(p => new FlottenPrognoseSnapshot(p.Id, p.BekanntSeit,
            p.Entscheidungszeitpunkt, p.Art, p.Intervalle)).ToList(),
        VerfuegbarkeitsfaktorNachEinheitId = x.VerfuegbarkeitsfaktorNachEinheitId
            .ToDictionary(p => p.Key, p => new List<double>(p.Value), StringComparer.Ordinal),
        Projektjahre = x.Projektjahre.Select(y => new FlottenProjektjahr
        {
            Jahr = y.Jahr,
            IstVollstaendigesJahr = y.IstVollstaendigesJahr,
            Istwerte = y.Istwerte.Select(Netzintervall).ToList(),
            VerfuegbarkeitsfaktorNachEinheitId = y.VerfuegbarkeitsfaktorNachEinheitId
                .ToDictionary(p => p.Key, p => new List<double>(p.Value), StringComparer.Ordinal),
            Prognosen = y.Prognosen.Select(p => new FlottenPrognoseSnapshot(p.Id, p.BekanntSeit,
                p.Entscheidungszeitpunkt, p.Art, p.Intervalle)).ToList()
        }).ToList()
    };

    internal static FlottenNetzintervall Netzintervall(FlottenNetzintervall x) => new()
    {
        Zeitstempel = x.Zeitstempel, LastKw = x.LastKw, PvKw = x.PvKw, BhkwKw = x.BhkwKw,
        BezugspreisEuroProKWh = x.BezugspreisEuroProKWh,
        PvVerkaufspreisEuroProKWh = x.PvVerkaufspreisEuroProKWh,
        BhkwVerkaufspreisEuroProKWh = x.BhkwVerkaufspreisEuroProKWh,
        BatterieVerkaufspreisEuroProKWh = x.BatterieVerkaufspreisEuroProKWh
    };

    internal static FlottenStudieKonfiguration Konfiguration(FlottenStudieKonfiguration x) => new()
    {
        Einheiten = x.Einheiten.Select(Einheit).ToList(), Optionen = Optionen(x.Optionen),
        Tarif = new FlottenTarif { LeistungspreisEuroProKw = x.Tarif.LeistungspreisEuroProKw,
            FixkostenEuro = x.Tarif.FixkostenEuro,
            BatterieVerkaufspreisEuroProKWh = x.Tarif.BatterieVerkaufspreisEuroProKWh },
        Wirtschaftlichkeit = FlottenWirtschaftlichkeit.Kopiere(x.Wirtschaftlichkeit),
        Auslegung = FlottenOptimierer.Kopiere(x.Auslegung)
    };
}
