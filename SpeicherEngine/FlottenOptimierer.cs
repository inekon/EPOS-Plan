using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SpeicherEngine;

/// <summary>Vollstaendige, begrenzte Rastersuche; die Aussage gilt nur fuer das gepruefte Raster.</summary>
public static class FlottenOptimierer
{
    private const string NullId = "Nullvariante-ohne-Zusatzspeicher";

    /// <summary>
    /// Die Mindestbreite des Feinrasterfensters [kWh] — woertlich aus der Mappe V7 und aus
    /// <see cref="SpeicherOptimierer"/> uebernommen (Auftrag #224).
    /// </summary>
    /// <remarks>
    /// Sie greift nur bei einem Suchraum, der schmaler als 1 kWh ist; dort kann das Fenster
    /// den Suchraum nach oben um bis zu 1 kWh verlassen. Das ist Vorlagentreue und praktisch
    /// bedeutungslos.
    /// </remarks>
    public const double FEINRASTER_MINDESTBREITE = 1.0;

    /// <summary>
    /// Die TEILUNG der zweiten Phase: Das Feinraster laeuft in Schritten von
    /// <c>Schrittweite / 9</c> (Mappe V7: zehn Stuetzstellen ueber einem Fenster von einer
    /// Grobschrittweite ergeben neun Teilintervalle).
    /// </summary>
    public const int FEINRASTER_TEILUNG = 9;

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
    /// <b>Seit Auftrag #224 in ZWEI PHASEN</b> (Anwenderentscheid SD-E-9, SD-Q10): Auf das
    /// Grobraster folgt — wenn <see cref="FlottenAuslegungEingang.Feinraster"/> es sagt —
    /// ein zweites Raster um das Grob-Optimum, NUR auf der Groessenachse der ersten aktiven
    /// Suchachse (Kapazitaet; im Modus <see cref="FlottenAuslegungsmodus.LeistungUndCRate"/>
    /// die Leistung). Die zweite Achse, die Stueckzahl, alle uebrigen Achsen und das
    /// Betriebsziel bleiben beim Wert des Grob-Optimums. <b>Das Feinraster gewinnt nur bei
    /// STRIKT besserem Kapitalwert</b> — bei Gleichstand bleibt das Grobraster massgeblich
    /// (dieselbe Regel wie <see cref="SpeicherOptimierer"/>, Z. 143).
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
        var uhr = Stopwatch.StartNew();
        var i = FlottenKopie.Eingang(input);
        var basis = FlottenKopie.Konfiguration(config);
        var aktiveAchsen = basis.Auslegung.Achsen.Where(x => x is { Aktiv: true }).ToArray();
        var achsenwahlen = aktiveAchsen.Select((achse, index) => BildeAchse(achse, index)).ToArray();
        var festeEinheiten = FesteEinheiten(basis, aktiveAchsen);
        var hardware = Kombiniere(festeEinheiten, achsenwahlen);
        var ziele = basis.Auslegung.Betriebsziele.Count == 0
            ? new[] { basis.Optionen.Betriebsziel }
            : basis.Auslegung.Betriebsziele.Distinct().ToArray();

        // DIE ZAEHLREGEL steht an EINER Stelle (Auftrag #224): Dieselbe Rechnung, die die
        // Kandidatenzeile der Station „4 Optimierung" anzeigt, entscheidet hier ueber
        // Annahme oder Abweisung. Sie zaehlt BEIDE Phasen.
        FlottenKandidatenzahl zahl = Kandidatenzahl(basis);
        if (!zahl.Zulaessig)
            throw new ArgumentException(
                $"Das vollstaendige Raster umfasst {zahl.Grob} Kandidaten im Grobraster" +
                (zahl.FeinHoechstens > 0 ? $" und bis zu {zahl.FeinHoechstens} im Feinraster" : "") +
                $" und ueberschreitet die Grenze {zahl.Grenze}. Das Raster wurde nicht gekuerzt.");

        // Die Groessenkopplung der ERSTEN aktiven Suchachse geht ins Ergebnis (Auftrag
        // #226): Sie sagt der Groessen-Sicht, welche zwei Groessen die Karte aufspannt.
        // Ohne aktive Achse gibt es kein Raster - dann bleibt die Vorbelegung stehen.
        var result = new FlottenAuslegungErgebnis();
        var ersteAchse = aktiveAchsen.FirstOrDefault();
        if (ersteAchse is not null) result.Achsenmodus = ersteAchse.Modus;

        var lauf = new Laufstand
        {
            Eingang = i,
            Basis = basis,
            Planer = planer,
            Fortschritt = progress,
            Ergebnis = result,
            Gesamt = zahl.Gesamt
        };

        RechnePhase(lauf, hardware, ziele, FlottenKandidatPhase.Grob, cancellationToken);

        // ---------------------------------------------------------------- Phase 2
        if (basis.Auslegung.Feinraster && ersteAchse is not null && lauf.BesteVariante is { } beste
            && beste.Wahlen.Count > 0 && beste.Wahlen[0].Anzahl > 0)
        {
            var feinwerte = Feinrasterwerte(ersteAchse, beste.Wahlen[0].ErsterWert);
            if (feinwerte.Count > 0)
            {
                var feinAchsen = new List<Achsenwahl>[achsenwahlen.Length];
                feinAchsen[0] = BildeFeinachse(ersteAchse, 0, beste.Wahlen[0], feinwerte);
                for (int a = 1; a < achsenwahlen.Length; a++)
                    feinAchsen[a] = new List<Achsenwahl> { beste.Wahlen[a] };

                var feinVarianten = Kombiniere(festeEinheiten, feinAchsen);
                lauf.Gesamt = zahl.Grob + feinVarianten.Count;
                result.FeinrasterGerechnet = true;
                RechnePhase(lauf, feinVarianten, new[] { lauf.BestesZiel }, FlottenKandidatPhase.Fein,
                            cancellationToken);
            }
        }

        if (lauf.Auswertungen == 0)
            throw new InvalidOperationException("Kein Kandidat konnte fachlich ausgewertet werden; Eingaben, Prognosen und Endenergiebewertung pruefen.",
                lauf.ErsterFehler);
        result.NullvarianteZulaessig = lauf.NullZulaessig == true;
        result.Kandidaten.Insert(0, new FlottenKandidatZusammenfassung
        {
            KandidatId = NullId,
            Betriebsziel = basis.Optionen.Betriebsziel,
            Zulaessig = result.NullvarianteZulaessig,
            KapitalwertEuro = 0,
            Grund = result.NullvarianteZulaessig ? null : "Die technische Anschlussgrenze wird bereits ohne Speicher verletzt."
        });
        if (result.NullvarianteZulaessig && lauf.BesterWert <= 0)
        {
            result.NullvarianteGewonnen = true;
            result.BesterKandidat = null;
            var empty = FlottenKopie.Konfiguration(basis);
            empty.Einheiten.Clear();
            empty.Wirtschaftlichkeit.Einheiten.Clear();
            result.BesteKonfiguration = empty;
            result.BesteStudie = lauf.Nullstudie;
            result.BesteZeitreihe = lauf.Nullstudie?.Variante;
        }
        else
        {
            result.NullvarianteGewonnen = false;
        }
        uhr.Stop();
        result.Rechendauer = uhr.Elapsed;
        return result;
    }

    // =====================================================================
    //  Die Zaehlregel — eine Wahrheit fuer Seite und Lauf (Auftrag #224)
    // =====================================================================

    /// <summary>
    /// Wie viele Kandidaten dieser Suchraum ergibt — OHNE einen einzigen zu rechnen.
    /// </summary>
    /// <remarks>
    /// <para>Sie baut keine Einheiten, sondern zaehlt die Stuetzstellen: je aktiver Achse
    /// <c>(Anzahl 0 ja/nein) + Stueckzahlen × erste Groesse × zweite Groesse</c>, alles
    /// multipliziert und mit der Zahl der Betriebsziele mal genommen. Dazu die Obergrenze
    /// des Feinrasters aus der ERSTEN aktiven Achse.</para>
    /// <para><b>Sie wirft nicht.</b> Ein unbrauchbarer Bereich (leer, negativ, Schritt 0 bei
    /// echter Spanne) liefert <see cref="FlottenKandidatenzahl.Gueltig"/> = <c>false</c>;
    /// die Seite zeigt dann den Eingabefehler, den sie ohnehin schon rechnet, und keine
    /// erfundene Zahl.</para>
    /// </remarks>
    /// <param name="config">Die Studienkonfiguration samt Suchraum; <c>null</c> = leeres Raster.</param>
    public static FlottenKandidatenzahl Kandidatenzahl(FlottenStudieKonfiguration? config)
    {
        FlottenAuslegungEingang auslegung = config?.Auslegung ?? new FlottenAuslegungEingang();
        var aktive = (auslegung.Achsen ?? new List<FlottenAuslegungsAchse>())
            .Where(x => x is { Aktiv: true }).ToArray();

        long hardware = 1;
        bool gueltig = true;
        foreach (FlottenAuslegungsAchse achse in aktive)
        {
            long n = Achsengroesse(achse);
            if (n < 0) { gueltig = false; break; }
            if (hardware > long.MaxValue / Math.Max(1L, n)) { gueltig = false; break; }
            hardware *= n;
        }

        int ziele = (auslegung.Betriebsziele?.Count ?? 0) == 0
            ? 1 : auslegung.Betriebsziele!.Distinct().Count();
        long grob = gueltig ? hardware * ziele : 0;
        long fein = gueltig && auslegung.Feinraster && aktive.Length > 0
            ? FeinrasterHoechstzahl(aktive[0]) : 0;

        return new FlottenKandidatenzahl(grob, fein, auslegung.MaximaleKandidaten, gueltig);
    }

    /// <summary>Die Zahl der Hardwarevarianten EINER Achse; <c>-1</c> = unbrauchbarer Bereich.</summary>
    private static long Achsengroesse(FlottenAuslegungsAchse achse)
    {
        if (achse is null || achse.AnzahlVon < 0 || achse.AnzahlBis < achse.AnzahlVon) return -1;
        long erste = Rasterzahl(GroesseVon(achse), GroesseBis(achse), GroesseSchritt(achse));
        long zweite = Rasterzahl(ZweitVon(achse), ZweitBis(achse), ZweitSchritt(achse));
        if (erste < 0 || zweite < 0) return -1;

        long mitEinheiten = Math.Max(0L, achse.AnzahlBis - Math.Max(achse.AnzahlVon, 1) + 1L);
        long ohne = achse.AnzahlVon <= 0 ? 1L : 0L;
        return ohne + mitEinheiten * erste * zweite;
    }

    /// <summary>
    /// Die Hoechstzahl der Feinrasterpunkte dieser Achse — der groesstmoegliche Fall ueber
    /// alle Lagen des Grob-Optimums.
    /// </summary>
    /// <remarks>
    /// Das Fenster ist hoechstens zwei Grobschritte breit (eine Schrittweite nach jeder
    /// Seite) und nie breiter als der Suchraum selbst; gerastert wird es mit
    /// <c>Schrittweite / 9</c>.
    /// </remarks>
    private static long FeinrasterHoechstzahl(FlottenAuslegungsAchse achse)
    {
        double von = GroesseVon(achse), bis = GroesseBis(achse), schritt = GroesseSchritt(achse);
        if (!double.IsFinite(von) || !double.IsFinite(bis) || von <= 0 || bis < von) return 0;
        if (bis - von <= 1e-12) return 0;                       // ein einziger Stuetzwert
        if (!double.IsFinite(schritt) || schritt <= 0) return 0;

        double breite = Math.Min(2.0 * schritt, bis - von);
        if (breite < FEINRASTER_MINDESTBREITE) breite = FEINRASTER_MINDESTBREITE;
        return Stuetzstellen(breite, schritt / FEINRASTER_TEILUNG);
    }

    /// <summary>Die Feinrasterwerte um <paramref name="bestGroesse"/> — die Regel der Mappe V7.</summary>
    /// <remarks>
    /// <code>
    /// unten = max(von, best - schritt)
    /// oben  = min(bis, best + schritt)
    /// if (oben - unten &lt; 1 kWh) oben = unten + 1 kWh     // Mindestbreite
    /// Werte im Abstand schritt / 9, die Obergrenze immer dabei
    /// </code>
    /// Die Klemmung ist der Grund, warum ein Grob-Optimum am Rand ein EINSEITIGES
    /// Feinraster bekommt: Die Suche laeuft nicht ueber die vom Anwender gesetzten Grenzen
    /// hinaus.
    /// </remarks>
    public static List<double> Feinrasterwerte(FlottenAuslegungsAchse achse, double bestGroesse)
    {
        double von = GroesseVon(achse), bis = GroesseBis(achse), schritt = GroesseSchritt(achse);
        if (!double.IsFinite(von) || !double.IsFinite(bis) || von <= 0 || bis < von) return new List<double>();
        if (bis - von <= 1e-12) return new List<double>();
        if (!double.IsFinite(schritt) || schritt <= 0) return new List<double>();
        if (!double.IsFinite(bestGroesse)) return new List<double>();

        double unten = Math.Max(von, bestGroesse - schritt);
        double oben = Math.Min(bis, bestGroesse + schritt);
        if (oben - unten < FEINRASTER_MINDESTBREITE) oben = unten + FEINRASTER_MINDESTBREITE;

        return Raster(unten, oben, schritt / FEINRASTER_TEILUNG, "Feinraster");
    }

    private static double GroesseVon(FlottenAuslegungsAchse a) =>
        a.Modus == FlottenAuslegungsmodus.LeistungUndCRate ? a.LeistungVonKw : a.KapazitaetVonKWh;
    private static double GroesseBis(FlottenAuslegungsAchse a) =>
        a.Modus == FlottenAuslegungsmodus.LeistungUndCRate ? a.LeistungBisKw : a.KapazitaetBisKWh;
    private static double GroesseSchritt(FlottenAuslegungsAchse a) =>
        a.Modus == FlottenAuslegungsmodus.LeistungUndCRate ? a.LeistungSchrittKw : a.KapazitaetSchrittKWh;
    private static double ZweitVon(FlottenAuslegungsAchse a) =>
        a.Modus == FlottenAuslegungsmodus.KapazitaetUndLeistung ? a.LeistungVonKw : a.CRateVon;
    private static double ZweitBis(FlottenAuslegungsAchse a) =>
        a.Modus == FlottenAuslegungsmodus.KapazitaetUndLeistung ? a.LeistungBisKw : a.CRateBis;
    private static double ZweitSchritt(FlottenAuslegungsAchse a) =>
        a.Modus == FlottenAuslegungsmodus.KapazitaetUndLeistung ? a.LeistungSchrittKw : a.CRateSchritt;

    /// <summary>Die Zahl der Stuetzstellen eines Bereichs; <c>-1</c> = unbrauchbar.</summary>
    private static long Rasterzahl(double von, double bis, double schritt)
    {
        if (!double.IsFinite(von) || !double.IsFinite(bis) || von <= 0 || bis < von) return -1;
        if (Math.Abs(bis - von) <= 1e-12) return 1;
        if (!double.IsFinite(schritt) || schritt <= 0) return -1;
        return Stuetzstellen(bis - von, schritt);
    }

    /// <summary>Wie viele Punkte <see cref="Raster"/> ueber dieser Spanne legt.</summary>
    private static long Stuetzstellen(double spanne, double schritt)
    {
        long n = (long)Math.Floor(spanne / schritt + 1e-9) + 1;
        if ((n - 1) * schritt < spanne - 1e-9) n++;             // die Obergrenze kommt dazu
        return n;
    }

    // =====================================================================
    //  Der Lauf einer Phase
    // =====================================================================

    /// <summary>Der veraenderliche Stand eines Suchlaufs ueber beide Phasen.</summary>
    private sealed class Laufstand
    {
        public FlottenEingang Eingang = null!;
        public FlottenStudieKonfiguration Basis = null!;
        public IFlottenPlaner? Planer;
        public IProgress<FlottenFortschritt>? Fortschritt;
        public FlottenAuslegungErgebnis Ergebnis = null!;
        public long Gesamt;

        public int Erledigt;
        public int Auswertungen;
        public double BesterWert = double.NegativeInfinity;
        public bool? NullZulaessig;
        public Exception? ErsterFehler;
        public FlottenStudienErgebnis? Nullstudie;
        public Hardwarevariante? BesteVariante;
        public FlottenBetriebsziel BestesZiel;
    }

    private static void RechnePhase(Laufstand lauf, IReadOnlyList<Hardwarevariante> hardware,
                                    IReadOnlyList<FlottenBetriebsziel> ziele,
                                    FlottenKandidatPhase phase, CancellationToken cancellationToken)
    {
        FlottenEingang i = lauf.Eingang;
        FlottenStudieKonfiguration basis = lauf.Basis;
        FlottenAuslegungErgebnis result = lauf.Ergebnis;

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
                        var study = FlottenSimulator.Simuliere(yearInput, laufConfig, lauf.Planer, cancellationToken);
                        displayStudy ??= study;
                        lauf.NullZulaessig = (lauf.NullZulaessig ?? true) && study.ReferenzOhneSpeicher.Zulaessig;
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
                    displayStudy = FlottenSimulator.Simuliere(i, candidate, lauf.Planer, cancellationToken);
                    lauf.NullZulaessig = (lauf.NullZulaessig ?? true) && displayStudy.ReferenzOhneSpeicher.Zulaessig;
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
                lauf.Auswertungen++;
                displayStudy!.Wirtschaftlichkeit = economics;
                lauf.Nullstudie ??= Nullstudie(displayStudy);
                var summary = Zusammenfassung(id, ziel, candidate.Einheiten, feasible,
                    economics.KapitalwertEuro, reason, variante, phase);
                Kennzahlen(summary, displayStudy, accounts);
                result.Kandidaten.Add(summary);

                // STRENGER Groesser-Vergleich: Bei Gleichstand bleibt der frueher gerechnete
                // Kandidat massgeblich — und weil Phase 2 NACH Phase 1 laeuft, heisst das:
                // Das Feinraster gewinnt nur bei STRIKT besserem Kapitalwert (Vorlage:
                // SpeicherOptimierer.cs Z. 143).
                if (feasible && economics.KapitalwertEuro > lauf.BesterWert)
                {
                    lauf.BesterWert = economics.KapitalwertEuro;
                    result.BesterKandidat = summary;
                    result.BesteKonfiguration = FlottenKopie.Konfiguration(candidate);
                    result.BesteStudie = displayStudy;
                    result.BesteZeitreihe = displayStudy.Variante;
                    lauf.BesteVariante = variante;
                    lauf.BestesZiel = ziel;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                lauf.ErsterFehler ??= ex;
                result.Kandidaten.Add(Zusammenfassung(id, ziel, candidate.Einheiten, false,
                    double.NegativeInfinity, ex.Message, variante, phase));
            }
            lauf.Erledigt++;
            lauf.Fortschritt?.Report(new FlottenFortschritt
            {
                Abgeschlossen = lauf.Erledigt,
                Gesamt = (int)Math.Min(int.MaxValue, lauf.Gesamt),
                KandidatId = id
            });
        }
    }

    private static FlottenKandidatZusammenfassung Zusammenfassung(string id, FlottenBetriebsziel ziel,
        IReadOnlyList<FlottenEinheit> units, bool feasible, double npv, string? reason,
        Hardwarevariante variante, FlottenKandidatPhase phase) => new()
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
        Phase = phase,
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
    /// EINE Wahl auf EINER Suchachse: die Einheiten, ihre Stelle im Grobgitter und die zwei
    /// Werte, aus denen sie entstanden sind.
    /// </summary>
    /// <remarks>
    /// Die zwei WERTE sind seit Auftrag #224 dabei: Das Feinraster baut die Achse mit einem
    /// anderen Groessenwert, aber DERSELBEN Stueckzahl und DEMSELBEN zweiten Wert noch
    /// einmal auf — ohne sie muesste es aus den fertigen Einheiten zurueckrechnen.
    /// </remarks>
    private sealed class Achsenwahl
    {
        public List<FlottenEinheit> Einheiten { get; init; } = new();

        /// <summary>Stelle auf der ERSTEN Achse (Kapazitaet bzw. Leistung); -1 = keine.</summary>
        public int Rasterzeile { get; init; } = -1;

        /// <summary>Stelle auf der ZWEITEN Achse (Leistung bzw. C-Rate); -1 = keine.</summary>
        public int Rasterspalte { get; init; } = -1;

        /// <summary>Die Stueckzahl dieser Wahl; 0 = die Einheit entfaellt.</summary>
        public int Anzahl { get; init; }

        /// <summary>Der Wert auf der Groessenachse.</summary>
        public double ErsterWert { get; init; }

        /// <summary>Der Wert auf der zweiten Achse.</summary>
        public double ZweiterWert { get; init; }
    }

    /// <summary>
    /// EINE Hardwarevariante des Rasters samt ihrer Stelle darin (Auftrag #193).
    /// </summary>
    /// <remarks>
    /// Die zwei Stellen sind nur bei GENAU EINER aktiven Suchachse belegt: Erst dann ist
    /// das Raster zweidimensional und laesst sich als Karte zeichnen. Bei mehreren Achsen
    /// bleiben sie auf <c>-1</c> — ein erfundener Index waere schlimmer als keiner. Ein
    /// FEINRASTERPUNKT traegt sie ebenfalls nicht: Er liegt zwischen den Stuetzstellen.
    /// </remarks>
    private sealed class Hardwarevariante
    {
        public Hardwarevariante(List<FlottenEinheit> einheiten, IReadOnlyList<Achsenwahl> wahlen,
                                int zeile = -1, int spalte = -1)
        {
            Einheiten = einheiten;
            Wahlen = wahlen;
            Rasterzeile = zeile;
            Rasterspalte = spalte;
        }

        public List<FlottenEinheit> Einheiten { get; }

        /// <summary>Die gewaehlte Stelle je aktiver Achse, in deren Reihenfolge.</summary>
        public IReadOnlyList<Achsenwahl> Wahlen { get; }

        /// <summary>Stelle auf der ERSTEN Achse (Kapazitaet bzw. Leistung); -1 = keine.</summary>
        public int Rasterzeile { get; }

        /// <summary>Stelle auf der ZWEITEN Achse (Leistung bzw. C-Rate); -1 = keine.</summary>
        public int Rasterspalte { get; }
    }

    /// <summary>Die Einheiten, die KEINE Achse ersetzt — sie stehen in jeder Variante.</summary>
    private static List<FlottenEinheit> FesteEinheiten(FlottenStudieKonfiguration config,
                                                       IReadOnlyList<FlottenAuslegungsAchse> aktive)
    {
        if (aktive.Count == 0) return config.Einheiten.Select(FlottenKopie.Einheit).ToList();
        var replacedIds = new HashSet<string>(aktive.Select(a => a.ErsetztEinheitId ?? a.Vorlage.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
        return config.Einheiten.Where(x => !replacedIds.Contains(x.Id)).Select(FlottenKopie.Einheit).ToList();
    }

    /// <summary>Das kartesische Produkt der Achsenwahlen ueber den festen Einheiten.</summary>
    private static List<Hardwarevariante> Kombiniere(List<FlottenEinheit> festeEinheiten,
                                                     IReadOnlyList<List<Achsenwahl>> achsenwahlen)
    {
        if (achsenwahlen.Count == 0)
            return new List<Hardwarevariante> { new(festeEinheiten, Array.Empty<Achsenwahl>()) };

        // Die Stelle im Raster ueberlebt nur, solange es EINE Achse gibt (siehe
        // Hardwarevariante); bei mehreren wird sie verworfen.
        bool eineAchse = achsenwahlen.Count == 1;
        var result = new List<Hardwarevariante> { new(festeEinheiten, Array.Empty<Achsenwahl>()) };
        foreach (var choices in achsenwahlen)
        {
            var next = new List<Hardwarevariante>(result.Count * choices.Count);
            foreach (var prefix in result)
            foreach (var choice in choices)
            {
                var combined = prefix.Einheiten.Select(FlottenKopie.Einheit).ToList();
                combined.AddRange(choice.Einheiten.Select(FlottenKopie.Einheit));
                var wahlen = new List<Achsenwahl>(prefix.Wahlen) { choice };
                next.Add(eineAchse
                    ? new Hardwarevariante(combined, wahlen, choice.Rasterzeile, choice.Rasterspalte)
                    : new Hardwarevariante(combined, wahlen));
            }
            result = next;
        }
        return result;
    }

    private static List<Achsenwahl> BildeAchse(FlottenAuslegungsAchse axis, int axisIndex)
    {
        if (axis.AnzahlVon < 0 || axis.AnzahlBis < axis.AnzahlVon)
            throw new ArgumentException("Ungueltiger Anzahlbereich in Auslegungsachse.");
        var firstAxis = axis.Modus == FlottenAuslegungsmodus.LeistungUndCRate
            ? Raster(axis.LeistungVonKw, axis.LeistungBisKw, axis.LeistungSchrittKw, "Leistung")
            : Raster(axis.KapazitaetVonKWh, axis.KapazitaetBisKWh, axis.KapazitaetSchrittKWh, "Kapazitaet");
        var secondAxis = axis.Modus == FlottenAuslegungsmodus.KapazitaetUndLeistung
            ? Raster(axis.LeistungVonKw, axis.LeistungBisKw, axis.LeistungSchrittKw, "Leistung")
            : Raster(axis.CRateVon, axis.CRateBis, axis.CRateSchritt, "C-Rate");
        var result = new List<Achsenwahl>();
        for (var count = axis.AnzahlVon; count <= axis.AnzahlBis; count++)
        {
            if (count == 0)
            {
                result.Add(new Achsenwahl());
                continue;
            }
            for (var ersteStelle = 0; ersteStelle < firstAxis.Count; ersteStelle++)
            for (var zweiteStelle = 0; zweiteStelle < secondAxis.Count; zweiteStelle++)
            {
                double first = firstAxis[ersteStelle], second = secondAxis[zweiteStelle];
                result.Add(new Achsenwahl
                {
                    Einheiten = BaueEinheiten(axis, axisIndex, count, first, second),
                    Rasterzeile = ersteStelle,
                    Rasterspalte = zweiteStelle,
                    Anzahl = count,
                    ErsterWert = first,
                    ZweiterWert = second
                });
            }
        }
        return result;
    }

    /// <summary>
    /// Die Achse der ZWEITEN Phase: dieselbe Stueckzahl, derselbe zweite Wert, nur andere
    /// Groessenwerte (Auftrag #224, SD-Q10).
    /// </summary>
    private static List<Achsenwahl> BildeFeinachse(FlottenAuslegungsAchse axis, int axisIndex,
                                                   Achsenwahl beste, IReadOnlyList<double> groessen)
    {
        var result = new List<Achsenwahl>(groessen.Count);
        foreach (double groesse in groessen)
            result.Add(new Achsenwahl
            {
                Einheiten = BaueEinheiten(axis, axisIndex, beste.Anzahl, groesse, beste.ZweiterWert),
                Anzahl = beste.Anzahl,
                ErsterWert = groesse,
                ZweiterWert = beste.ZweiterWert
            });
        return result;
    }

    /// <summary>Die Einheiten EINER Achsenwahl — dieselbe Rechnung fuer Grob- und Feinraster.</summary>
    private static List<FlottenEinheit> BaueEinheiten(FlottenAuslegungsAchse axis, int axisIndex,
                                                      int count, double first, double second)
    {
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
        return units;
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
        Feinraster = x.Feinraster,
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
