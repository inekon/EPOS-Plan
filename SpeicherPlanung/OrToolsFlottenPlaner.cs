using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Google.OrTools.LinearSolver;
using SpeicherEngine;

namespace SpeicherPlanung;

/// <summary>
/// Prognoseplaner fuer eine Flotte AC-gekoppelter Stromspeicher.
/// </summary>
/// <remarks>
/// Modellierungsgrundlage: https://developers.google.com/optimization/lp/mpsolver .
/// Das native Solverpaket ist ausschliesslich in diesem Projekt referenziert:
/// https://www.nuget.org/packages/Google.OrTools/9.15.6755 .
/// </remarks>
public sealed class OrToolsFlottenPlaner : IFlottenPlaner
{
    private const double DtH = 0.25;
    private const double Toleranz = 1e-6;
    private const string SolverName = "SCIP";

    /// <summary>Prueft, ob die native SCIP-Laufzeit auf dem aktuellen Ziel geladen werden kann.</summary>
    public static bool Verfuegbar
    {
        get
        {
            using Solver? solver = Solver.CreateSolver(SolverName);
            return solver is not null;
        }
    }

    /// <inheritdoc />
    public FlottenPlan Plane(FlottenPlanAnfrage anfrage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(anfrage);
        cancellationToken.ThrowIfCancellationRequested();
        Eingaben eingaben = Pruefen(anfrage);

        Solver? solver = Solver.CreateSolver(SolverName);
        if (solver is null)
        {
            return ErgebnisOhneFahrplan(anfrage, FlottenPlanStatus.Fehler,
                "Der MILP-Solver SCIP ist in der installierten OR-Tools-Laufzeit nicht verfuegbar.");
        }

        Modell modell;
        try
        {
            modell = Erstellen(solver, anfrage, eingaben);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ErgebnisOhneFahrplan(anfrage, FlottenPlanStatus.Fehler,
                "Das MILP-Modell konnte nicht aufgebaut werden: " + ex.Message);
        }

        var laufzeit = Stopwatch.StartNew();
        using CancellationTokenRegistration registrierung = cancellationToken.Register(
            static state => ((Solver)state!).InterruptSolve(), solver);

        IReadOnlyList<Ziel> ziele = Ziele(anfrage, modell);
        for (var index = 0; index < ziele.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Ziel ziel = ziele[index];
            Solver.ResultStatus status = Loesen(solver, ziel, anfrage.Zeitlimit, laufzeit);
            cancellationToken.ThrowIfCancellationRequested();

            if (status == Solver.ResultStatus.OPTIMAL)
            {
                if (index + 1 < ziele.Count)
                {
                    FixiereOptimum(solver, ziel, index);
                }

                continue;
            }

            if (status == Solver.ResultStatus.FEASIBLE)
            {
                return ErgebnisMitFahrplan(anfrage, modell, FlottenPlanStatus.Zulaessig,
                    $"Zeitlimit in Zielfunktionsstufe '{ziel.Name}': Ein zulaessiger Incumbent wurde gefunden, " +
                    "aber die Optimalitaet nicht bewiesen.");
            }

            if (status == Solver.ResultStatus.INFEASIBLE)
            {
                return ErgebnisOhneFahrplan(anfrage, FlottenPlanStatus.Unzulaessig,
                    "Das MILP ist unzulaessig. Anschlussgrenzen werden nicht durch Lastabwurf oder einen Nullfahrplan verdeckt.");
            }

            if (status == Solver.ResultStatus.NOT_SOLVED)
            {
                return ErgebnisOhneFahrplan(anfrage, FlottenPlanStatus.Zeitlimit,
                    $"Zeitlimit in Zielfunktionsstufe '{ziel.Name}': Der Solver hat keinen zulaessigen Incumbent gefunden.");
            }

            return ErgebnisOhneFahrplan(anfrage, FlottenPlanStatus.Fehler,
                "Der MILP-Solver beendete die Planung mit Status " + status + ".");
        }

        return ErgebnisMitFahrplan(anfrage, modell, FlottenPlanStatus.Optimal,
            "Alle Zielfunktionsstufen wurden optimal geloest.");
    }

    private static Eingaben Pruefen(FlottenPlanAnfrage anfrage)
    {
        if (anfrage.Prognose is null)
        {
            throw new ArgumentException("Eine Prognose ist erforderlich.", nameof(anfrage));
        }

        if (anfrage.Optionen is null)
        {
            throw new ArgumentException("Planungsoptionen sind erforderlich.", nameof(anfrage));
        }

        if (anfrage.Prognose.Art != anfrage.Optionen.PrognoseArt)
        {
            throw new ArgumentException("Prognoseart und Planungsoptionen stimmen nicht ueberein.", nameof(anfrage));
        }

        if (anfrage.Prognose.Art == PrognoseArt.VerifiziertBekannt
            && anfrage.Prognose.BekanntSeit > anfrage.Entscheidungszeitpunkt)
        {
            throw new ArgumentException("Die Prognose war zum Entscheidungszeitpunkt noch nicht bekannt.", nameof(anfrage));
        }

        if (anfrage.Prognose.Entscheidungszeitpunkt != anfrage.Entscheidungszeitpunkt)
        {
            throw new ArgumentException("Prognose und Anfrage haben verschiedene Entscheidungszeitpunkte.", nameof(anfrage));
        }

        if (!Enum.IsDefined(anfrage.Modus)
            || !Enum.IsDefined(anfrage.Optionen.Endbedingung)
            || !Enum.IsDefined(anfrage.Optionen.ErzeugerPrioritaet))
        {
            throw new ArgumentException("Planungsmodus, Endbedingung oder Erzeugerprioritaet ist unbekannt.", nameof(anfrage));
        }

        if (anfrage.Prognose.Intervalle.Count == 0 || anfrage.Einheiten.Count == 0)
        {
            throw new ArgumentException("Prognose und Speicherflotte duerfen nicht leer sein.", nameof(anfrage));
        }

        if (anfrage.EnergieKWh.Count != anfrage.Einheiten.Count)
        {
            throw new ArgumentException("Zu jedem Speicher ist genau ein Energiezustand erforderlich.", nameof(anfrage));
        }

        if (!PositivUndEndlich(anfrage.Zeitlimit.TotalMilliseconds))
        {
            throw new ArgumentException("Das Zeitlimit muss positiv und endlich sein.", nameof(anfrage));
        }

        if (!EndlichNichtNegativ(anfrage.BisherigerAbrechnungspeakKw)
            || !EndlichNichtNegativ(anfrage.LeistungspreisEuroProKw)
            || !EndlichNichtNegativ(anfrage.EndenergieAusgleichEuroProKWh))
        {
            throw new ArgumentException(
                "Abrechnungspeak, Leistungspreis und Endenergie-Ausgleich muessen endlich und nichtnegativ sein.",
                nameof(anfrage));
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < anfrage.Einheiten.Count; i++)
        {
            FlottenEinheit b = anfrage.Einheiten[i];
            if (string.IsNullOrWhiteSpace(b.Id) || !ids.Add(b.Id))
            {
                throw new ArgumentException("Speicher-IDs muessen nichtleer und eindeutig sein.", nameof(anfrage));
            }

            if (!PositivUndEndlich(b.KapazitaetKWh)
                || !EndlichNichtNegativ(b.LadeleistungKw)
                || !EndlichNichtNegativ(b.EntladeleistungKw)
                || !InBereich(b.Ladewirkungsgrad, 0, 1, untereGrenzeOffen: true)
                || !InBereich(b.Entladewirkungsgrad, 0, 1, untereGrenzeOffen: true)
                || !InBereich(b.SocMin, 0, 1)
                || !InBereich(b.SocMax, 0, 1)
                || b.SocMin > b.SocMax
                || !EndlichNichtNegativ(b.PeakReserveKWh)
                || !EndlichNichtNegativ(b.HilfsverbrauchKw)
                || !EndlichNichtNegativ(b.GrenzverschleissEuroProKWhEntladung))
            {
                throw new ArgumentException("Speicherparameter sind ungueltig: " + b.Id, nameof(anfrage));
            }

            double emin = b.SocMin * b.KapazitaetKWh;
            double emax = b.SocMax * b.KapazitaetKWh;
            if (b.PeakReserveKWh > emax - emin + Toleranz
                || !InBereich(anfrage.EnergieKWh[i], emin, emax))
            {
                throw new ArgumentException("Energiezustand oder Peak-Reserve ist ungueltig: " + b.Id, nameof(anfrage));
            }
        }

        var zeilen = anfrage.Prognose.Intervalle;
        if (zeilen[0].Zeitstempel != anfrage.Entscheidungszeitpunkt)
        {
            throw new ArgumentException("Die Prognose muss mit dem Entscheidungsintervall beginnen.", nameof(anfrage));
        }

        for (var t = 0; t < zeilen.Count; t++)
        {
            FlottenNetzintervall x = zeilen[t];
            if (!EndlichNichtNegativ(x.LastKw) || !EndlichNichtNegativ(x.PvKw)
                || !EndlichNichtNegativ(x.BhkwKw)
                || !Endlich(x.BezugspreisEuroProKWh)
                || !Endlich(x.PvVerkaufspreisEuroProKWh)
                || !Endlich(x.BhkwVerkaufspreisEuroProKWh)
                || !Endlich(x.BatterieVerkaufspreisEuroProKWh))
            {
                throw new ArgumentException("Ungueltige Prognosewerte im Intervall " + t + ".", nameof(anfrage));
            }

            DateTimeOffset utc = x.Zeitstempel.ToUniversalTime();
            if (utc.Second != 0 || utc.Millisecond != 0 || utc.Minute % 15 != 0)
            {
                throw new ArgumentException("Zeitstempel muessen auf dem UTC-Viertelstundenraster liegen.", nameof(anfrage));
            }

            if (t > 0 && zeilen[t].Zeitstempel - zeilen[t - 1].Zeitstempel != TimeSpan.FromMinutes(15))
            {
                throw new ArgumentException("Die Prognose muss lueckenlos im Viertelstundenraster vorliegen.", nameof(anfrage));
            }
        }

        double? importGrenze = Grenze(anfrage.Optionen.NetzbezugGrenzeKw, "Netzbezugsgrenze");
        double? exportGrenze = Grenze(anfrage.Optionen.NetzeinspeisungGrenzeKw, "Netzeinspeisungsgrenze");
        double? peakZiel = anfrage.Optionen.WirtschaftlicherPeakZielwertKw;
        if (peakZiel.HasValue && !EndlichNichtNegativ(peakZiel.Value))
        {
            throw new ArgumentException("Der wirtschaftliche Peak-Zielwert ist ungueltig.", nameof(anfrage));
        }

        if (anfrage.Modus == FlottenPlanerModus.MultiUse && peakZiel is null)
        {
            throw new ArgumentException("Multi-Use erfordert einen wirtschaftlichen Peak-Zielwert.", nameof(anfrage));
        }

        double[][] verfuegbarkeit = Verfuegbarkeit(anfrage);
        double[]? endenergie = Endenergie(anfrage);
        return new Eingaben(importGrenze, exportGrenze, peakZiel, verfuegbarkeit, endenergie);
    }

    private static Modell Erstellen(Solver solver, FlottenPlanAnfrage anfrage, Eingaben p)
    {
        int n = anfrage.Einheiten.Count;
        int horizon = anfrage.Prognose.Intervalle.Count;
        double aux = anfrage.Einheiten.Sum(static x => x.HilfsverbrauchKw);
        bool netzladung = anfrage.Modus != FlottenPlanerModus.PvPlanung && anfrage.Optionen.NetzladungErlaubt;
        bool batterieexport = anfrage.Modus != FlottenPlanerModus.PvPlanung && anfrage.Optionen.BatterieexportErlaubt;

        Variable[,] laden = new Variable[n, horizon];
        Variable[,] entladen = new Variable[n, horizon];
        Variable[,] energie = new Variable[n, horizon + 1];
        Variable[] import = new Variable[horizon];
        Variable[] export = new Variable[horizon];
        Variable[] abregelung = new Variable[horizon];
        Variable[] pvExport = new Variable[horizon];
        Variable[] bhkwExport = new Variable[horizon];
        Variable[] batterieExport = new Variable[horizon];
        Variable[] spitzenVerletzung = new Variable[horizon];

        for (var i = 0; i < n; i++)
        {
            FlottenEinheit b = anfrage.Einheiten[i];
            double emin = b.SocMin * b.KapazitaetKWh;
            double emax = b.SocMax * b.KapazitaetKWh;
            for (var t = 0; t <= horizon; t++)
            {
                energie[i, t] = solver.MakeNumVar(emin, emax, $"E_{i}_{t}");
            }

            Gleich(solver, energie[i, 0], anfrage.EnergieKWh[i], $"E_start_{i}");
            if (p.Endenergie is not null)
            {
                Gleich(solver, energie[i, horizon], p.Endenergie[i], $"E_ende_{i}");
            }
        }

        Variable peak = solver.MakeNumVar(anfrage.BisherigerAbrechnungspeakKw,
            ObererPeak(anfrage, p, aux), "Abrechnungspeak");
        Variable? peakU = p.PeakZiel.HasValue
            ? solver.MakeNumVar(0, Math.Max(0, ObererPeak(anfrage, p, aux) - p.PeakZiel.Value), "Peak_U")
            : null;

        for (var t = 0; t < horizon; t++)
        {
            FlottenNetzintervall x = anfrage.Prognose.Intervalle[t];
            double sumLadeMax = 0;
            double sumEntladeMax = 0;
            Variable richtung = solver.MakeBoolVar("Batterierichtung_" + t);
            Variable netzrichtung = solver.MakeBoolVar("Netzrichtung_" + t);

            for (var i = 0; i < n; i++)
            {
                FlottenEinheit b = anfrage.Einheiten[i];
                double faktor = p.Verfuegbarkeit[i][t];
                double cmax = b.LadeleistungKw * faktor;
                double dmax = b.EntladeleistungKw * faktor;
                sumLadeMax += cmax;
                sumEntladeMax += dmax;
                laden[i, t] = solver.MakeNumVar(0, cmax, $"c_{i}_{t}");
                entladen[i, t] = solver.MakeNumVar(0, dmax, $"d_{i}_{t}");

                KleinerGleich(solver, laden[i, t], cmax, richtung, $"c_richtung_{i}_{t}");
                Constraint dRichtung = solver.MakeConstraint(double.NegativeInfinity, dmax, $"d_richtung_{i}_{t}");
                dRichtung.SetCoefficient(entladen[i, t], 1);
                dRichtung.SetCoefficient(richtung, dmax);

                Constraint bilanz = solver.MakeConstraint(0, 0, $"E_bilanz_{i}_{t}");
                bilanz.SetCoefficient(energie[i, t + 1], 1);
                bilanz.SetCoefficient(energie[i, t], -1);
                bilanz.SetCoefficient(laden[i, t], -DtH * b.Ladewirkungsgrad);
                bilanz.SetCoefficient(entladen[i, t], DtH / b.Entladewirkungsgrad);

                Constraint reserve = solver.MakeConstraint(
                    b.SocMin * b.KapazitaetKWh + b.PeakReserveKWh,
                    double.PositiveInfinity, $"Reserve_{i}_{t}");
                reserve.SetCoefficient(energie[i, t + 1], 1);
            }

            double importM = Math.Max(0, x.LastKw + aux + sumLadeMax);
            double exportM = Math.Max(0, x.PvKw + x.BhkwKw + sumEntladeMax);
            if (p.ImportGrenze.HasValue)
            {
                importM = Math.Min(importM, p.ImportGrenze.Value);
            }

            if (p.ExportGrenze.HasValue)
            {
                exportM = Math.Min(exportM, p.ExportGrenze.Value);
            }

            import[t] = solver.MakeNumVar(0, importM, "Import_" + t);
            export[t] = solver.MakeNumVar(0, exportM, "Export_" + t);
            KleinerGleich(solver, import[t], importM, netzrichtung, "Import_Richtung_" + t);
            Constraint expRichtung = solver.MakeConstraint(double.NegativeInfinity, exportM, "Export_Richtung_" + t);
            expRichtung.SetCoefficient(export[t], 1);
            expRichtung.SetCoefficient(netzrichtung, exportM);

            double kmax = Math.Min(x.PvKw, Math.Max(0, x.PvKw + x.BhkwKw - x.LastKw - aux));
            abregelung[t] = solver.MakeNumVar(0, kmax, "PV_Abregelung_" + t);
            pvExport[t] = solver.MakeNumVar(0, x.PvKw, "PV_Export_" + t);
            bhkwExport[t] = solver.MakeNumVar(0, x.BhkwKw, "BHKW_Export_" + t);
            batterieExport[t] = solver.MakeNumVar(0, batterieexport ? sumEntladeMax : 0, "Batterie_Export_" + t);

            Variable pvLast = solver.MakeNumVar(0, x.LastKw + aux, "PV_Last_" + t);
            Variable pvLaden = solver.MakeNumVar(0, sumLadeMax, "PV_Laden_" + t);
            Variable bhkwLast = solver.MakeNumVar(0, x.LastKw + aux, "BHKW_Last_" + t);
            Variable bhkwLaden = solver.MakeNumVar(0, sumLadeMax, "BHKW_Laden_" + t);
            Variable batterieLast = solver.MakeNumVar(0, x.LastKw + aux, "Batterie_Last_" + t);
            Variable netzLast = solver.MakeNumVar(0, x.LastKw + aux, "Netz_Last_" + t);
            Variable netzLaden = solver.MakeNumVar(0, netzladung ? sumLadeMax : 0, "Netz_Laden_" + t);

            SummenGleich(solver, $"PV_Fluss_{t}", x.PvKw,
                (pvLast, 1), (pvLaden, 1), (pvExport[t], 1), (abregelung[t], 1));
            SummenGleich(solver, $"BHKW_Fluss_{t}", x.BhkwKw,
                (bhkwLast, 1), (bhkwLaden, 1), (bhkwExport[t], 1));
            SummenGleich(solver, $"Last_Fluss_{t}", x.LastKw + aux,
                (pvLast, 1), (bhkwLast, 1), (batterieLast, 1), (netzLast, 1));
            SummenGleich(solver, $"Laden_Fluss_{t}", 0,
                SpeicherTerme(laden, n, t, -1)
                    .Prepend((netzLaden, 1))
                    .Prepend((bhkwLaden, 1))
                    .Prepend((pvLaden, 1))
                    .ToArray());
            SummenGleich(solver, $"Batterie_Fluss_{t}", 0,
                SpeicherTerme(entladen, n, t, -1)
                    .Prepend((batterieExport[t], 1))
                    .Prepend((batterieLast, 1))
                    .ToArray());
            SummenGleich(solver, $"Import_Fluss_{t}", 0,
                (netzLast, 1), (netzLaden, 1), (import[t], -1));
            SummenGleich(solver, $"Export_Fluss_{t}", 0,
                (pvExport[t], 1), (bhkwExport[t], 1), (batterieExport[t], 1), (export[t], -1));

            // Die Ausfuehrungsrechnung ordnet zunaechst Vor-Ort-Erzeugung der Last
            // und Ladung zu. Erst deren Rest ist Direktexport; eine Entladung, die
            // darueber hinaus Export erzeugt, erhaelt den Batterietarif.
            Variable direktexportAktiv = solver.MakeBoolVar("Direktexport_Aktiv_" + t);
            Constraint direkt = solver.MakeConstraint(double.NegativeInfinity, 0, "Direktexport_Bedingung_" + t);
            direkt.SetCoefficient(pvExport[t], 1);
            direkt.SetCoefficient(bhkwExport[t], 1);
            direkt.SetCoefficient(direktexportAktiv, -(x.PvKw + x.BhkwKw));
            Constraint batterieZurLast = solver.MakeConstraint(
                double.NegativeInfinity, x.LastKw + aux, "Batterie_Last_Bedingung_" + t);
            batterieZurLast.SetCoefficient(batterieLast, 1);
            batterieZurLast.SetCoefficient(direktexportAktiv, x.LastKw + aux);

            // Abregelung und Netzladung duerfen nicht gegeneinander optimiert werden.
            Variable abregelungAktiv = solver.MakeBoolVar("Abregelung_Aktiv_" + t);
            KleinerGleich(solver, abregelung[t], kmax, abregelungAktiv, "Abregelung_Bedingung_" + t);
            Constraint keineNetzladungBeiAbregelung = solver.MakeConstraint(
                double.NegativeInfinity, sumLadeMax, "Abregelung_Ohne_Netzladung_" + t);
            keineNetzladungBeiAbregelung.SetCoefficient(netzLaden, 1);
            keineNetzladungBeiAbregelung.SetCoefficient(abregelungAktiv, sumLadeMax);

            // Die konfigurierte Erzeugerprioritaet verhindert eine rein tarifgetriebene
            // Umdeklaration: Der priorisierte Erzeuger wird vor dem anderen vor Ort genutzt.
            Variable prioritaet = solver.MakeBoolVar("Erzeugerprioritaet_" + t);
            if (anfrage.Optionen.ErzeugerPrioritaet == FlottenErzeugerPrioritaet.PvVorBhkw)
            {
                KleinerGleich(solver, pvExport[t], x.PvKw, prioritaet, "PV_Export_Prioritaet_" + t);
                Constraint c = solver.MakeConstraint(double.NegativeInfinity, x.BhkwKw, "BHKW_Nutzung_Prioritaet_" + t);
                c.SetCoefficient(bhkwLast, 1);
                c.SetCoefficient(bhkwLaden, 1);
                c.SetCoefficient(prioritaet, x.BhkwKw);
            }
            else
            {
                KleinerGleich(solver, bhkwExport[t], x.BhkwKw, prioritaet, "BHKW_Export_Prioritaet_" + t);
                Constraint c = solver.MakeConstraint(double.NegativeInfinity, x.PvKw, "PV_Nutzung_Prioritaet_" + t);
                c.SetCoefficient(pvLast, 1);
                c.SetCoefficient(pvLaden, 1);
                c.SetCoefficient(prioritaet, x.PvKw);
            }

            Constraint peakGrenze = solver.MakeConstraint(0, double.PositiveInfinity, "Peak_" + t);
            peakGrenze.SetCoefficient(peak, 1);
            peakGrenze.SetCoefficient(import[t], -1);

            if (peakU is not null && p.PeakZiel.HasValue)
            {
                spitzenVerletzung[t] = peakU;
                Constraint weich = solver.MakeConstraint(double.NegativeInfinity, p.PeakZiel.Value, "Peak_Ziel_" + t);
                weich.SetCoefficient(import[t], 1);
                weich.SetCoefficient(peakU, -1);
            }
            else
            {
                spitzenVerletzung[t] = solver.MakeNumVar(0, 0, "Peak_U_0_" + t);
            }
        }

        return new Modell(laden, entladen, energie, import, export, abregelung,
            pvExport, bhkwExport, batterieExport, peak, peakU, spitzenVerletzung);
    }

    private static IReadOnlyList<Ziel> Ziele(FlottenPlanAnfrage anfrage, Modell m)
    {
        var ziele = new List<Ziel>();
        if (anfrage.Modus == FlottenPlanerModus.MultiUse)
        {
            ziele.Add(new Ziel("kleinste Peak-Verletzung", (m.PeakU!, 1)));
        }

        if (anfrage.Modus == FlottenPlanerModus.PvPlanung)
        {
            ziele.Add(new Ziel("minimaler Netzbezug",
                m.Import.Select(static v => (v, DtH)).ToArray()));
            ziele.Add(new Ziel("minimale PV-Abregelung",
                m.Abregelung.Select(static v => (v, DtH)).ToArray()));
            var speicherstand = new List<(Variable, double)>();
            for (var i = 0; i < m.Energie.GetLength(0); i++)
            {
                for (var t = 1; t < m.Energie.GetLength(1); t++)
                {
                    speicherstand.Add((m.Energie[i, t], DtH));
                }
            }

            ziele.Add(new Ziel("minimale gespeicherte Energie", speicherstand.ToArray()));
            return ziele;
        }

        ziele.Add(WirtschaftlichesZiel(anfrage, m));
        return ziele;
    }

    private static Ziel WirtschaftlichesZiel(FlottenPlanAnfrage anfrage, Modell m)
    {
        var terme = new List<(Variable, double)>();
        for (var t = 0; t < m.Import.Length; t++)
        {
            FlottenNetzintervall x = anfrage.Prognose.Intervalle[t];
            terme.Add((m.Import[t], DtH * x.BezugspreisEuroProKWh));
            terme.Add((m.PvExport[t], -DtH * x.PvVerkaufspreisEuroProKWh));
            terme.Add((m.BhkwExport[t], -DtH * x.BhkwVerkaufspreisEuroProKWh));
            terme.Add((m.BatterieExport[t], -DtH * x.BatterieVerkaufspreisEuroProKWh));
            for (var i = 0; i < anfrage.Einheiten.Count; i++)
            {
                terme.Add((m.Entladen[i, t], DtH * anfrage.Einheiten[i].GrenzverschleissEuroProKWhEntladung));
            }
        }

        terme.Add((m.Peak, anfrage.LeistungspreisEuroProKw));
        if (anfrage.Optionen.Endbedingung == FlottenEndbedingung.KeineVorgabe)
        {
            for (var i = 0; i < anfrage.Einheiten.Count; i++)
            {
                terme.Add((m.Energie[i, m.Energie.GetLength(1) - 1], -anfrage.EndenergieAusgleichEuroProKWh));
            }
        }

        return new Ziel("minimale wirtschaftliche Kosten", terme.ToArray());
    }

    private static Solver.ResultStatus Loesen(Solver solver, Ziel ziel, TimeSpan limit, Stopwatch laufzeit)
    {
        TimeSpan rest = limit - laufzeit.Elapsed;
        if (rest <= TimeSpan.Zero)
        {
            return Solver.ResultStatus.NOT_SOLVED;
        }

        solver.SetTimeLimit(Math.Max(1L, (long)Math.Ceiling(rest.TotalMilliseconds)));
        Objective objective = solver.Objective();
        objective.Clear();
        foreach ((Variable variable, double faktor) in ziel.Terme)
        {
            objective.SetCoefficient(variable, faktor);
        }

        objective.SetMinimization();
        return solver.Solve();
    }

    private static void FixiereOptimum(Solver solver, Ziel ziel, int nummer)
    {
        double optimum = ziel.Terme.Sum(static x => x.Variable.SolutionValue() * x.Faktor);
        double spielraum = Toleranz * Math.Max(1, Math.Abs(optimum));
        Constraint fest = solver.MakeConstraint(double.NegativeInfinity, optimum + spielraum,
            "Lexikografisches_Optimum_" + nummer);
        foreach ((Variable variable, double faktor) in ziel.Terme)
        {
            fest.SetCoefficient(variable, faktor);
        }
    }

    private static FlottenPlan ErgebnisMitFahrplan(
        FlottenPlanAnfrage anfrage,
        Modell m,
        FlottenPlanStatus status,
        string statusText)
    {
        var intervalle = new List<FlottenPlanIntervall>(m.Import.Length);
        for (var t = 0; t < m.Import.Length; t++)
        {
            var laden = new List<double>(anfrage.Einheiten.Count);
            var entladen = new List<double>(anfrage.Einheiten.Count);
            for (var i = 0; i < anfrage.Einheiten.Count; i++)
            {
                laden.Add(Bereinigen(m.Laden[i, t].SolutionValue()));
                entladen.Add(Bereinigen(m.Entladen[i, t].SolutionValue()));
            }

            intervalle.Add(new FlottenPlanIntervall
            {
                Zeitstempel = anfrage.Prognose.Intervalle[t].Zeitstempel,
                LadeleistungKwJeSpeicher = laden,
                EntladeleistungKwJeSpeicher = entladen,
                PvAbregelungKw = Bereinigen(m.Abregelung[t].SolutionValue())
            });
        }

        return new FlottenPlan
        {
            PlanId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            PrognoseId = anfrage.Prognose.Id,
            Entscheidungszeitpunkt = anfrage.Entscheidungszeitpunkt,
            Status = status,
            StatusText = statusText,
            ZielfunktionswertEuro = WirtschaftlicheKosten(anfrage, m),
            GeplanteImportverletzungKw = 0,
            Intervalle = intervalle
        };
    }

    private static double WirtschaftlicheKosten(FlottenPlanAnfrage anfrage, Modell m)
    {
        double wert = anfrage.LeistungspreisEuroProKw
            * (m.Peak.SolutionValue() - anfrage.BisherigerAbrechnungspeakKw);
        for (var t = 0; t < m.Import.Length; t++)
        {
            FlottenNetzintervall x = anfrage.Prognose.Intervalle[t];
            wert += DtH * (x.BezugspreisEuroProKWh * m.Import[t].SolutionValue()
                - x.PvVerkaufspreisEuroProKWh * m.PvExport[t].SolutionValue()
                - x.BhkwVerkaufspreisEuroProKWh * m.BhkwExport[t].SolutionValue()
                - x.BatterieVerkaufspreisEuroProKWh * m.BatterieExport[t].SolutionValue());
            for (var i = 0; i < anfrage.Einheiten.Count; i++)
            {
                wert += DtH * anfrage.Einheiten[i].GrenzverschleissEuroProKWhEntladung
                    * m.Entladen[i, t].SolutionValue();
            }
        }

        if (anfrage.Optionen.Endbedingung == FlottenEndbedingung.KeineVorgabe)
        {
            for (var i = 0; i < anfrage.Einheiten.Count; i++)
            {
                wert -= anfrage.EndenergieAusgleichEuroProKWh
                    * (m.Energie[i, m.Energie.GetLength(1) - 1].SolutionValue() - anfrage.EnergieKWh[i]);
            }
        }

        return Bereinigen(wert);
    }

    private static FlottenPlan ErgebnisOhneFahrplan(
        FlottenPlanAnfrage anfrage,
        FlottenPlanStatus status,
        string text)
        => new()
        {
            PlanId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            PrognoseId = anfrage.Prognose?.Id ?? string.Empty,
            Entscheidungszeitpunkt = anfrage.Entscheidungszeitpunkt,
            Status = status,
            StatusText = text
        };

    private static double[][] Verfuegbarkeit(FlottenPlanAnfrage anfrage)
    {
        int n = anfrage.Einheiten.Count;
        int horizon = anfrage.Prognose.Intervalle.Count;
        if (anfrage.VerfuegbarkeitsfaktorJeSpeicher.Count == 0)
        {
            return Enumerable.Range(0, n).Select(_ => Enumerable.Repeat(1d, horizon).ToArray()).ToArray();
        }

        if (anfrage.VerfuegbarkeitsfaktorJeSpeicher.Count != n)
        {
            throw new ArgumentException("Die Verfuegbarkeit muss fuer jeden Speicher angegeben werden.", nameof(anfrage));
        }

        var result = new double[n][];
        for (var i = 0; i < n; i++)
        {
            if (anfrage.VerfuegbarkeitsfaktorJeSpeicher[i].Count != horizon)
            {
                throw new ArgumentException("Die Verfuegbarkeit muss den gesamten Prognosehorizont abdecken.", nameof(anfrage));
            }

            result[i] = anfrage.VerfuegbarkeitsfaktorJeSpeicher[i].ToArray();
            if (result[i].Any(static x => !InBereich(x, 0, 1)))
            {
                throw new ArgumentException("Verfuegbarkeitsfaktoren muessen zwischen null und eins liegen.", nameof(anfrage));
            }
        }

        return result;
    }

    private static double[]? Endenergie(FlottenPlanAnfrage anfrage)
    {
        if (anfrage.Optionen.Endbedingung == FlottenEndbedingung.KeineVorgabe)
        {
            return null;
        }

        double[] ziel = anfrage.Optionen.Endbedingung == FlottenEndbedingung.JeSpeicherWieAnfang
            ? anfrage.EnergieKWh.ToArray()
            : anfrage.Optionen.EndenergieZielKWh.ToArray();
        if (ziel.Length != anfrage.Einheiten.Count)
        {
            throw new ArgumentException("Zu jedem Speicher ist genau ein Endenergiewert erforderlich.", nameof(anfrage));
        }

        for (var i = 0; i < ziel.Length; i++)
        {
            FlottenEinheit b = anfrage.Einheiten[i];
            double untergrenze = b.SocMin * b.KapazitaetKWh + b.PeakReserveKWh;
            double obergrenze = b.SocMax * b.KapazitaetKWh;
            if (!InBereich(ziel[i], untergrenze, obergrenze))
            {
                throw new ArgumentException("Ein Endenergiewert verletzt SoC-Grenze oder Peak-Reserve.", nameof(anfrage));
            }
        }

        return ziel;
    }

    private static double ObererPeak(FlottenPlanAnfrage anfrage, Eingaben p, double aux)
    {
        double physisch = anfrage.Prognose.Intervalle.Max(x => x.LastKw + aux)
            + anfrage.Einheiten.Sum(static x => x.LadeleistungKw);
        if (p.ImportGrenze.HasValue)
        {
            physisch = Math.Min(physisch, p.ImportGrenze.Value);
        }

        return Math.Max(anfrage.BisherigerAbrechnungspeakKw, physisch);
    }

    private static double? Grenze(double? wert, string name)
    {
        if (!wert.HasValue)
        {
            return null;
        }

        if (!EndlichNichtNegativ(wert.Value))
        {
            throw new ArgumentException(name + " muss endlich und nichtnegativ sein.");
        }

        return wert.Value;
    }

    private static (Variable, double)[] SpeicherTerme(Variable[,] variablen, int n, int t, double faktor)
    {
        var result = new (Variable, double)[n];
        for (var i = 0; i < n; i++)
        {
            result[i] = (variablen[i, t], faktor);
        }

        return result;
    }

    private static void Gleich(Solver solver, Variable variable, double wert, string name)
    {
        Constraint c = solver.MakeConstraint(wert, wert, name);
        c.SetCoefficient(variable, 1);
    }

    private static void KleinerGleich(
        Solver solver,
        Variable links,
        double faktor,
        Variable binaer,
        string name)
    {
        Constraint c = solver.MakeConstraint(double.NegativeInfinity, 0, name);
        c.SetCoefficient(links, 1);
        c.SetCoefficient(binaer, -faktor);
    }

    private static void SummenGleich(
        Solver solver,
        string name,
        double rechteSeite,
        params (Variable Variable, double Faktor)[] terme)
    {
        Constraint c = solver.MakeConstraint(rechteSeite, rechteSeite, name);
        foreach ((Variable variable, double faktor) in terme)
        {
            c.SetCoefficient(variable, faktor);
        }
    }

    private static bool Endlich(double wert) => !double.IsNaN(wert) && !double.IsInfinity(wert);

    private static bool PositivUndEndlich(double wert) => Endlich(wert) && wert > 0;

    private static bool EndlichNichtNegativ(double wert) => Endlich(wert) && wert >= 0;

    private static bool InBereich(double wert, double min, double max, bool untereGrenzeOffen = false)
        => Endlich(wert) && (untereGrenzeOffen ? wert > min : wert >= min) && wert <= max;

    private static double Bereinigen(double wert) => Math.Abs(wert) <= Toleranz ? 0 : wert;

    private sealed record Eingaben(
        double? ImportGrenze,
        double? ExportGrenze,
        double? PeakZiel,
        double[][] Verfuegbarkeit,
        double[]? Endenergie);

    private sealed record Ziel(string Name, params (Variable Variable, double Faktor)[] Terme);

    private sealed record Modell(
        Variable[,] Laden,
        Variable[,] Entladen,
        Variable[,] Energie,
        Variable[] Import,
        Variable[] Export,
        Variable[] Abregelung,
        Variable[] PvExport,
        Variable[] BhkwExport,
        Variable[] BatterieExport,
        Variable Peak,
        Variable? PeakU,
        Variable[] SpitzenVerletzung);
}
