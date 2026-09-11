using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SpeicherEngine;

/// <summary>Vollstaendige, begrenzte Rastersuche; die Aussage gilt nur fuer das gepruefte Raster.</summary>
public static class FlottenOptimierer
{
    private const string NullId = "Nullvariante-ohne-Zusatzspeicher";
    /// <summary>
    /// Prueft das VOLLSTAENDIGE endliche Raster aus Hardwarevarianten und Betriebszielen
    /// und nennt die beste zulaessige Variante (Spezifikation 12.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Das kartesische Raster wird nie gekuerzt: Uebersteigt es
    /// <see cref="FlottenAuslegungEingang.MaximaleKandidaten"/>, wird es abgewiesen.
    /// Jeder Kandidat wird ueber alle gelieferten Projektjahre gerechnet — der End-SoC
    /// jeder Einheit wird zum Start-SoC des Folgejahres —, sonst ueber das eine
    /// vollstaendige Jahr der Istreihe.
    /// </para>
    /// <para>
    /// Rangfolge: technisch ZULAESSIGE Varianten nach hoechstem Kapitalwert, daneben die
    /// zulaessige Nullvariante mit Kapitalwert 0. Ist die Referenz wegen einer harten
    /// Netzgrenze unzulaessig, kann eine technisch noetige Variante trotz negativem
    /// Kapitalwert gewinnen. Sind alle Rechnungen fachlich ungueltig, entsteht ein
    /// Konfigurationsfehler statt einer falschen Null-Empfehlung. Die vollstaendige
    /// Zeitreihe haelt nur der beste Kandidat beziehungsweise die Nullvariante.
    /// </para>
    /// </remarks>
    /// <param name="input">Istwerte, Prognosen, Projektjahre und Verfuegbarkeiten des Standorts.</param>
    /// <param name="config">Ausgangsflotte, Betriebsoptionen, Tarif, Kapitalwerteingaben und Suchraum.</param>
    /// <param name="planer">Der Planer fuer die planenden Ziele; fuer die reaktiven Ziele <c>null</c>.</param>
    /// <param name="progress">Empfaenger der Fortschrittsmeldungen je Kandidat; <c>null</c> = keine.</param>
    /// <param name="cancellationToken">Abbruchmarke; sie wirkt im Kandidaten-, Jahres-, Intervall- und Solverlauf.</param>
    /// <returns>Die Zusammenfassungen aller Kandidaten und — sofern vorhanden — bester Kandidat, Konfiguration, Studie und Zeitreihe.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> oder <paramref name="config"/> ist <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Das Raster ueberschreitet die Kandidatengrenze, ein Projektjahr ist unvollstaendig, oder keine Variante liess sich fachlich bewerten.</exception>
    public static FlottenAuslegungErgebnis Rechne(
        FlottenEingang input,
        FlottenStudieKonfiguration config,
        IFlottenPlaner? planer = null,
        IProgress<FlottenFortschritt>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (config is null) throw new ArgumentNullException(nameof(config));
        var i = FlottenKopie.Eingang(input);
        var basis = FlottenKopie.Konfiguration(config);
        var hardware = BildeHardware(basis);
        var ziele = basis.Auslegung.Betriebsziele.Count == 0
            ? new[] { basis.Optionen.Betriebsziel }
            : basis.Auslegung.Betriebsziele.Distinct().ToArray();
        var totalLong = checked((long)hardware.Count * ziele.Length);
        if (basis.Auslegung.MaximaleKandidaten <= 0 || totalLong > basis.Auslegung.MaximaleKandidaten)
            throw new ArgumentException($"Das vollstaendige Raster umfasst {totalLong} Kandidaten und ueberschreitet die Grenze " +
                $"{basis.Auslegung.MaximaleKandidaten}. Das Raster wurde nicht gekuerzt.");

        // Die Groessenkopplung der ERSTEN aktiven Suchachse geht ins Ergebnis (Auftrag
        // #226): Sie sagt der Groessen-Sicht, welche zwei Groessen die Karte aufspannt.
        // Ohne aktive Achse gibt es kein Raster - dann bleibt die Vorbelegung stehen.
        var result = new FlottenAuslegungErgebnis();
        var ersteAchse = basis.Auslegung.Achsen.FirstOrDefault(x => x.Aktiv);
        if (ersteAchse is not null) result.Achsenmodus = ersteAchse.Modus;
        var done = 0;
        var bestValue = double.NegativeInfinity;
        bool? nullFeasible = null;
        var successfulEvaluations = 0;
        Exception? firstConfigurationError = null;
        FlottenStudienErgebnis? nullStudy = null;
        foreach (var variante in hardware)
        foreach (var ziel in ziele)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var units = variante.Einheiten;
            var candidate = FlottenKopie.Konfiguration(basis);
            candidate.Einheiten = units.Select(FlottenKopie.Einheit).ToList();
            candidate.Optionen.Betriebsziel = ziel;
            candidate.Wirtschaftlichkeit.Einheiten = candidate.Einheiten.Select(FlottenKopie.Einheit).ToList();
            var id = KandidatId(candidate.Einheiten, ziel);
            i.KonfigurationId = id;
            FlottenStudienErgebnis? displayStudy = null;
            var accounts = new List<FlottenJahreskonto>();
            var feasible = true;
            string? reason = null;
            try
            {
                if (i.Projektjahre.Count > 0)
                {
                    var ordered = i.Projektjahre.OrderBy(x => x.Jahr).ToArray();
                    var laufConfig = FlottenKopie.Konfiguration(candidate);
                    for (var yearIndex = 0; yearIndex < ordered.Length; yearIndex++)
                    {
                        var y = ordered[yearIndex];
                        if (!y.IstVollstaendigesJahr)
                            throw new ArgumentException($"Projektjahr {y.Jahr} ist kein vollstaendiges Jahreskonto.");
                        var yearInput = new FlottenEingang
                        {
                            KonfigurationId = i.KonfigurationId,
                            DatenId = $"{i.DatenId}:{y.Jahr}",
                            Istwerte = y.Istwerte.Select(FlottenKopie.Netzintervall).ToList(),
                            VerfuegbarkeitsfaktorNachEinheitId = y.VerfuegbarkeitsfaktorNachEinheitId
                                .ToDictionary(p => p.Key, p => new List<double>(p.Value), StringComparer.Ordinal),
                            Prognosen = y.Prognosen.Select(p => new FlottenPrognoseSnapshot(p.Id, p.BekanntSeit,
                                p.Entscheidungszeitpunkt, p.Art, p.Intervalle)).ToList()
                        };
                        var study = FlottenSimulator.Simuliere(yearInput, laufConfig, planer, cancellationToken);
                        displayStudy ??= study;
                        nullFeasible = (nullFeasible ?? true) && study.ReferenzOhneSpeicher.Zulaessig;
                        feasible &= study.Variante.Zulaessig;
                        reason ??= study.Variante.Unzulaessigkeitsgrund;
                        accounts.Add(FlottenWirtschaftlichkeit.ErzeugeJahreskonto(yearIndex + 1, study,
                            candidate.Einheiten, candidate.Optionen.EnergieAusgleichEuroProKWh, true,
                            $"Tatsaechlich simuliertes Projektjahr {y.Jahr}"));
                        for (var unit = 0; unit < laufConfig.Einheiten.Count; unit++)
                            laufConfig.Einheiten[unit].SocStart = study.Variante.SpeicherKennzahlen[unit].EndenergieKWh
                                / laufConfig.Einheiten[unit].KapazitaetKWh;
                    }
                }
                else
                {
                    if (!IstVollstaendigesJahr(i.Istwerte))
                        throw new ArgumentException("Auslegung mit Kapitalwert benoetigt ein vollstaendiges Jahr; " +
                            "eine Referenzjahr-Wiederholung muss explizit konfiguriert sein.");
                    displayStudy = FlottenSimulator.Simuliere(i, candidate, planer, cancellationToken);
                    nullFeasible = (nullFeasible ?? true) && displayStudy.ReferenzOhneSpeicher.Zulaessig;
                    feasible = displayStudy.Variante.Zulaessig;
                    reason = displayStudy.Variante.Unzulaessigkeitsgrund;
                    accounts.Add(FlottenWirtschaftlichkeit.ErzeugeJahreskonto(1, displayStudy,
                        candidate.Einheiten, candidate.Optionen.EnergieAusgleichEuroProKWh, true));
                }

                var economicsInput = FlottenWirtschaftlichkeit.Kopiere(candidate.Wirtschaftlichkeit);
                economicsInput.Einheiten = candidate.Einheiten.Select(FlottenKopie.Einheit).ToList();
                economicsInput.Jahreskonten = accounts;
                if (i.Projektjahre.Count > 0) economicsInput.ReferenzjahrExplizitWiederholen = false;
                var economics = FlottenWirtschaftlichkeit.Bewerte(economicsInput);
                successfulEvaluations++;
                displayStudy!.Wirtschaftlichkeit = economics;
                nullStudy ??= Nullstudie(displayStudy);
                var summary = Zusammenfassung(id, ziel, candidate.Einheiten, feasible,
                    economics.KapitalwertEuro, reason, variante);
                Kennzahlen(summary, displayStudy, accounts);
                result.Kandidaten.Add(summary);
                if (feasible && economics.KapitalwertEuro > bestValue)
                {
                    bestValue = economics.KapitalwertEuro;
                    result.BesterKandidat = summary;
                    result.BesteKonfiguration = FlottenKopie.Konfiguration(candidate);
                    result.BesteStudie = displayStudy;
                    result.BesteZeitreihe = displayStudy.Variante;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                firstConfigurationError ??= ex;
                result.Kandidaten.Add(Zusammenfassung(id, ziel, candidate.Einheiten, false,
                    double.NegativeInfinity, ex.Message, variante));
            }
            done++;
            progress?.Report(new FlottenFortschritt { Abgeschlossen = done, Gesamt = (int)totalLong, KandidatId = id });
        }
        if (successfulEvaluations == 0)
            throw new InvalidOperationException("Kein Kandidat konnte fachlich ausgewertet werden; Eingaben, Prognosen und Endenergiebewertung pruefen.",
                firstConfigurationError);
        result.NullvarianteZulaessig = nullFeasible == true;
        result.Kandidaten.Insert(0, new FlottenKandidatZusammenfassung
        {
            KandidatId = NullId,
            Betriebsziel = basis.Optionen.Betriebsziel,
            Zulaessig = result.NullvarianteZulaessig,
            KapitalwertEuro = 0,
            Grund = result.NullvarianteZulaessig ? null : "Die technische Anschlussgrenze wird bereits ohne Speicher verletzt."
        });
        if (result.NullvarianteZulaessig && bestValue <= 0)
        {
            result.NullvarianteGewonnen = true;
            result.BesterKandidat = null;
            var empty = FlottenKopie.Konfiguration(basis);
            empty.Einheiten.Clear();
            empty.Wirtschaftlichkeit.Einheiten.Clear();
            result.BesteKonfiguration = empty;
            result.BesteStudie = nullStudy;
            result.BesteZeitreihe = nullStudy?.Variante;
        }
        else
        {
            result.NullvarianteGewonnen = false;
        }
        return result;
    }

    private static FlottenKandidatZusammenfassung Zusammenfassung(string id, FlottenBetriebsziel ziel,
        IReadOnlyList<FlottenEinheit> units, bool feasible, double npv, string? reason,
        Hardwarevariante variante) => new()
    {
        KandidatId = id,
        Betriebsziel = ziel,
        Zulaessig = feasible,
        KapitalwertEuro = npv,
        KapazitaetKWh = units.Sum(x => x.KapazitaetKWh),
        LadeleistungKw = units.Sum(x => x.LadeleistungKw),
        EntladeleistungKw = units.Sum(x => x.EntladeleistungKw),
        Grund = reason,
        Rasterzeile = variante.Rasterzeile,
        Rasterspalte = variante.Rasterspalte,
        Einheiten = units.Select(x => new FlottenKandidatEinheit
        {
            Id = x.Id,
            KapazitaetKWh = x.KapazitaetKWh,
            LadeleistungKw = x.LadeleistungKw,
            EntladeleistungKw = x.EntladeleistungKw
        }).ToList()
    };

    /// <summary>
    /// Die BETRIEBSkennzahlen eines Kandidaten aus dem Lauf, der ohnehin gerechnet wurde
    /// (Auftrag #193, Konzept „Stromspeicher-Dialoge" 2.5) — eine zweite Simulation gibt
    /// es nicht.
    /// </summary>
    /// <remarks>
    /// <para>Gelesen wird das ERSTE gerechnete Jahr: Durchsatz, Bezugsspitze und
    /// Diagnose stehen in seinem Variantenlauf, die Ersparnis in seinem Jahreskonto.
    /// Ueber mehrere Projektjahre gemittelt waeren es Zahlen, die in keinem Jahr so
    /// dastanden; die Jahresreihe selbst haelt ohnehin nur der beste Kandidat.</para>
    /// <para>Die ERSPARNIS ist die Rechnungsdifferenz abzueglich Betriebsaufwand und
    /// Durchsatzkosten — der laufende Vorteil OHNE Kapitaldienst. Investition,
    /// Ersatzinvestition und Restwert bleiben aussen vor; sie stecken im Kapitalwert
    /// daneben, und beide Zahlen zusammen beantworten „lohnt der Betrieb?" und „traegt
    /// sich die Anschaffung?" getrennt.</para>
    /// </remarks>
    private static void Kennzahlen(FlottenKandidatZusammenfassung summary,
        FlottenStudienErgebnis study, IReadOnlyList<FlottenJahreskonto> accounts)
    {
        summary.DurchsatzKWh = study.Variante.SpeicherKennzahlen.Sum(x => x.EntladeenergieAcKWh);
        summary.Vollzyklen = summary.KapazitaetKWh > 0.0
            ? summary.DurchsatzKWh / summary.KapazitaetKWh : 0.0;
        summary.BezugsspitzeKw = study.Variante.MaximalerNetzbezugKw;
        summary.Arbeitslos = study.Variante.Diagnose.Arbeitslos;

        if (accounts.Count > 0)
        {
            var konto = accounts[0];
            summary.ErsparnisEuroJahr = konto.Referenzrechnung.GesamtEuro
                - konto.Variantenrechnung.GesamtEuro - konto.OpexEuro - konto.DurchsatzkostenEuro;
        }
    }

    private static FlottenStudienErgebnis Nullstudie(FlottenStudienErgebnis source)
    {
        var reference = new FlottenSimulationErgebnis
        {
            KonfigurationId = NullId,
            DatenId = source.ReferenzOhneSpeicher.DatenId,
            Zulaessig = source.ReferenzOhneSpeicher.Zulaessig,
            Unzulaessigkeitsgrund = source.ReferenzOhneSpeicher.Unzulaessigkeitsgrund,
            Intervalle = source.ReferenzOhneSpeicher.Intervalle,
            SpeicherKennzahlen = source.ReferenzOhneSpeicher.SpeicherKennzahlen,
            NetzbezugKWh = source.ReferenzOhneSpeicher.NetzbezugKWh,
            NetzeinspeisungKWh = source.ReferenzOhneSpeicher.NetzeinspeisungKWh,
            PvAbregelungKWh = source.ReferenzOhneSpeicher.PvAbregelungKWh,
            VerlusteKWh = source.ReferenzOhneSpeicher.VerlusteKWh,
            MaximalerNetzbezugKw = source.ReferenzOhneSpeicher.MaximalerNetzbezugKw,
            PlanFallbackIntervalle = 0
        };
        return new FlottenStudienErgebnis
        {
            ReferenzOhneSpeicher = reference,
            Variante = reference,
            Referenzrechnung = source.Referenzrechnung,
            Variantenrechnung = source.Referenzrechnung,
            Wirtschaftlichkeit = new FlottenWirtschaftlichkeitErgebnis
            {
                InvestitionEuro = 0,
                KapitalwertEuro = 0,
                JahresCashflowsEuro = new List<double>()
            }
        };
    }

    /// <summary>
    /// EINE Hardwarevariante des Rasters samt ihrer Stelle darin (Auftrag #193).
    /// </summary>
    /// <remarks>
    /// Die zwei Stellen sind nur bei GENAU EINER aktiven Suchachse belegt: Erst dann ist
    /// das Raster zweidimensional und laesst sich als Karte zeichnen. Bei mehreren Achsen
    /// bleiben sie auf <c>-1</c> — ein erfundener Index waere schlimmer als keiner.
    /// </remarks>
    private sealed class Hardwarevariante
    {
        public Hardwarevariante(List<FlottenEinheit> einheiten, int zeile = -1, int spalte = -1)
        {
            Einheiten = einheiten;
            Rasterzeile = zeile;
            Rasterspalte = spalte;
        }

        public List<FlottenEinheit> Einheiten { get; }

        /// <summary>Stelle auf der ERSTEN Achse (Kapazitaet bzw. Leistung); -1 = keine.</summary>
        public int Rasterzeile { get; }

        /// <summary>Stelle auf der ZWEITEN Achse (Leistung bzw. C-Rate); -1 = keine.</summary>
        public int Rasterspalte { get; }
    }

    private static List<Hardwarevariante> BildeHardware(FlottenStudieKonfiguration config)
    {
        var active = config.Auslegung.Achsen.Where(x => x.Aktiv).ToArray();
        if (active.Length == 0)
            return new List<Hardwarevariante>
                { new(config.Einheiten.Select(FlottenKopie.Einheit).ToList()) };
        var axisChoices = active.Select((axis, index) => BildeAchse(axis, index)).ToArray();
        long count = 1;
        foreach (var choices in axisChoices) count = checked(count * choices.Count);
        if (count > config.Auslegung.MaximaleKandidaten)
            throw new ArgumentException($"Hardware-Raster umfasst {count} Kombinationen; keine stille Kuerzung.");
        var replacedIds = new HashSet<string>(active.Select(a => a.ErsetztEinheitId ?? a.Vorlage.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
        var fixedUnits = config.Einheiten.Where(x => !replacedIds.Contains(x.Id)).Select(FlottenKopie.Einheit).ToList();

        // Die Stelle im Raster ueberlebt nur, solange es EINE Achse gibt (siehe
        // Hardwarevariante); bei mehreren wird sie verworfen.
        bool eineAchse = axisChoices.Length == 1;
        var result = new List<Hardwarevariante>((int)count) { new(fixedUnits) };
        foreach (var choices in axisChoices)
        {
            var next = new List<Hardwarevariante>(result.Count * choices.Count);
            foreach (var prefix in result)
            foreach (var choice in choices)
            {
                var combined = prefix.Einheiten.Select(FlottenKopie.Einheit).ToList();
                combined.AddRange(choice.Einheiten.Select(FlottenKopie.Einheit));
                next.Add(eineAchse
                    ? new Hardwarevariante(combined, choice.Rasterzeile, choice.Rasterspalte)
                    : new Hardwarevariante(combined));
            }
            result = next;
        }
        return result;
    }

    private static List<Hardwarevariante> BildeAchse(FlottenAuslegungsAchse axis, int axisIndex)
    {
        if (axis.AnzahlVon < 0 || axis.AnzahlBis < axis.AnzahlVon)
            throw new ArgumentException("Ungueltiger Anzahlbereich in Auslegungsachse.");
        var firstAxis = axis.Modus == FlottenAuslegungsmodus.LeistungUndCRate
            ? Raster(axis.LeistungVonKw, axis.LeistungBisKw, axis.LeistungSchrittKw, "Leistung")
            : Raster(axis.KapazitaetVonKWh, axis.KapazitaetBisKWh, axis.KapazitaetSchrittKWh, "Kapazitaet");
        var secondAxis = axis.Modus == FlottenAuslegungsmodus.KapazitaetUndLeistung
            ? Raster(axis.LeistungVonKw, axis.LeistungBisKw, axis.LeistungSchrittKw, "Leistung")
            : Raster(axis.CRateVon, axis.CRateBis, axis.CRateSchritt, "C-Rate");
        var result = new List<Hardwarevariante>();
        for (var count = axis.AnzahlVon; count <= axis.AnzahlBis; count++)
        {
            if (count == 0)
            {
                result.Add(new Hardwarevariante(new List<FlottenEinheit>()));
                continue;
            }
            for (var ersteStelle = 0; ersteStelle < firstAxis.Count; ersteStelle++)
            for (var zweiteStelle = 0; zweiteStelle < secondAxis.Count; zweiteStelle++)
            {
                double first = firstAxis[ersteStelle], second = secondAxis[zweiteStelle];
                var capacity = axis.Modus == FlottenAuslegungsmodus.LeistungUndCRate ? first / second : first;
                var power = axis.Modus == FlottenAuslegungsmodus.KapazitaetUndCRate ? first * second :
                    axis.Modus == FlottenAuslegungsmodus.LeistungUndCRate ? first : second;
                var templatePower = Math.Max(axis.Vorlage.LadeleistungKw, axis.Vorlage.EntladeleistungKw);
                if (!double.IsFinite(templatePower) || templatePower <= 0)
                    throw new ArgumentException("Die Auslegungsvorlage braucht mindestens eine positive Richtungsleistung.");
                var powerScale = power / templatePower;
                var units = new List<FlottenEinheit>(count);
                for (var n = 0; n < count; n++)
                {
                    var b = FlottenKopie.Einheit(axis.Vorlage);
                    b.Id = $"{(string.IsNullOrWhiteSpace(b.Id) ? "Speicher" : b.Id)}-A{axisIndex + 1}-N{n + 1}";
                    b.Name = $"{(string.IsNullOrWhiteSpace(b.Name) ? "Speicher" : b.Name)} {n + 1}";
                    b.KapazitaetKWh = capacity;
                    b.LadeleistungKw = axis.Vorlage.LadeleistungKw * powerScale;
                    b.EntladeleistungKw = axis.Vorlage.EntladeleistungKw * powerScale;
                    units.Add(b);
                }
                result.Add(new Hardwarevariante(units, ersteStelle, zweiteStelle));
            }
        }
        return result;
    }

    private static List<double> Raster(double from, double to, double step, string name)
    {
        if (!double.IsFinite(from) || !double.IsFinite(to) || from <= 0 || to < from)
            throw new ArgumentException($"Ungueltiger {name}sbereich.");
        if (Math.Abs(to - from) <= 1e-12) return new List<double> { from };
        if (!double.IsFinite(step) || step <= 0) throw new ArgumentException($"{name}sschritt muss positiv sein.");
        var count = (int)Math.Floor((to - from) / step + 1e-9) + 1;
        var result = new List<double>(count);
        for (var n = 0; n < count; n++) result.Add(from + n * step);
        if (result[^1] < to - 1e-9) result.Add(to);
        return result;
    }

    private static string KandidatId(IReadOnlyList<FlottenEinheit> units, FlottenBetriebsziel ziel) =>
        $"{ziel}:{string.Join(";", units.Select(x =>
            $"{x.Id}:{x.KapazitaetKWh:G9}kWh:{x.LadeleistungKw:G9}kWladen:{x.EntladeleistungKw:G9}kWentladen"))}";

    private static bool IstVollstaendigesJahr(IReadOnlyList<FlottenNetzintervall> rows)
    {
        if (rows.Count is not (35040 or 35136)) return false;
        return rows[^1].Zeitstempel - rows[0].Zeitstempel == TimeSpan.FromMinutes(15 * (rows.Count - 1));
    }

    internal static FlottenAuslegungEingang Kopiere(FlottenAuslegungEingang x) => new()
    {
        MaximaleKandidaten = x.MaximaleKandidaten,
        Betriebsziele = new List<FlottenBetriebsziel>(x.Betriebsziele),
        Achsen = x.Achsen.Select(a => new FlottenAuslegungsAchse
        {
            Aktiv = a.Aktiv, ErsetztEinheitId = a.ErsetztEinheitId, Modus = a.Modus,
            AnzahlVon = a.AnzahlVon, AnzahlBis = a.AnzahlBis,
            KapazitaetVonKWh = a.KapazitaetVonKWh, KapazitaetBisKWh = a.KapazitaetBisKWh,
            KapazitaetSchrittKWh = a.KapazitaetSchrittKWh, LeistungVonKw = a.LeistungVonKw,
            LeistungBisKw = a.LeistungBisKw, LeistungSchrittKw = a.LeistungSchrittKw,
            CRateVon = a.CRateVon, CRateBis = a.CRateBis, CRateSchritt = a.CRateSchritt,
            Vorlage = FlottenKopie.Einheit(a.Vorlage)
        }).ToList()
    };
}
