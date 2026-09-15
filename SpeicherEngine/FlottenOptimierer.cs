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
    /// <b>Seit Auftrag #247 entscheidet die SUCHMETHODE, was variiert wird</b>
    /// (<see cref="FlottenAuslegungEingang.Suchmethode"/>, Anwenderentscheid SD-E-10):
    /// unter <see cref="FlottenSuchmethode.Groesse"/> zwei der drei Groessen bei FESTER
    /// Stueckzahl, unter <see cref="FlottenSuchmethode.Stueckzahl"/> die Stueckzahl bei
    /// FESTER Groesse, unter <see cref="FlottenSuchmethode.Bewerten"/> nichts — dann
    /// bleibt es bei der eingestellten Flotte. Das Mischraster Stueckzahl × Groesse gibt
    /// es nicht mehr.
    /// </para>
    /// <para>
    /// <b>Die GROESSENSUCHE rechnet GERAETE</b> (Anwenderentscheid vom 15.09.2026): Der
    /// Anwender gibt Bereiche fuer Kapazitaet und Leistung vor, und
    /// <see cref="FlottenGeraetewahl.Waehle"/> waehlt daraufhin unter den Saetzen der
    /// <see cref="FlottenAuslegungsAchse.Quelle"/>. Es wird nichts mehr skaliert und nichts
    /// mehr gerastert; die Kandidatenzahl ist die Zahl der gefundenen Geraete.
    /// </para>
    /// <para>
    /// <b>Es gibt nur EINE Phase.</b> Die frueher zweite verfeinerte die Groessenachse
    /// ZWISCHEN ihren Stuetzstellen — das setzte eine frei skalierbare Groesse voraus.
    /// Zwischen zwei Geraeten liegt kein drittes; eine zwischengerechnete Groesse waere ein
    /// Speicher, den es nicht gibt. <see cref="FlottenAuslegungEingang.Feinraster"/> ist
    /// deshalb wirkungslos, und <see cref="FlottenKandidatenzahl.FeinHoechstens"/> ist
    /// immer 0.
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
    /// <exception cref="ArgumentException">Der Suchraum ist unbrauchbar (siehe <see cref="FlottenSuchbefund"/>), das Raster ueberschreitet die Kandidatengrenze, ein Projektjahr ist unvollstaendig, oder keine Variante liess sich fachlich bewerten.</exception>
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

        // DIE SUCHMETHODE ENTSCHEIDET, WAS VARIIERT WIRD (Auftrag #247, SD-E-10): Unter
        // „Bewerten" gibt es keine Suchachse — die eingestellte Flotte wird genau einmal
        // je Betriebsziel gerechnet, auch wenn Achsen im Stand stehen.
        // AELTERE STAENDE WERDEN BENANNT UMGESETZT (Anwenderentscheid vom 15.09.2026):
        // Ein Stand mit C-Rate-Kopplung traegt seine Absicht in den C-Raten-Grenzen; sie
        // wird in Kapazitaets- und Leistungsgrenzen umgerechnet, nicht verworfen. Danach
        // gibt es im ganzen Lauf nur noch KapazitaetUndLeistung.
        FlottenAltstand.Normalisiere(basis.Auslegung);

        FlottenSuchmethode methode = basis.Auslegung.Suchmethode;
        var aktiveAchsen = methode == FlottenSuchmethode.Bewerten
            ? Array.Empty<FlottenAuslegungsAchse>()
            : basis.Auslegung.Achsen.Where(x => x is { Aktiv: true }).ToArray();

        // DIE ZAEHLREGEL steht an EINER Stelle (Auftrag #224): Dieselbe Rechnung, die die
        // Kandidatenzeile der Station „4 Optimierung" anzeigt, entscheidet hier ueber
        // Annahme oder Abweisung. Sie zaehlt BEIDE Phasen. Sie steht VOR dem Bau der
        // Achsen (Auftrag #247), damit ein unbrauchbarer Bereich seine BENANNTE Ablehnung
        // bekommt und nicht die rohe Ausnahme aus BildeAchse.
        FlottenKandidatenzahl zahl = Kandidatenzahl(basis);

        // DIE BENANNTEN ABLEHNUNGEN ZUERST (Auftrag #247): Ein leerer Stueckzahlbereich
        // und ein unbrauchbarer Groessenbereich sind EINGABEfehler und sollen als solche
        // dastehen — bis dahin fielen sie als „0 Kandidaten ueberschreiten die Grenze"
        // aus der Zaehlregel, was den Anwender an die falsche Stelle schickte.
        if (zahl.Befund == FlottenSuchbefund.StueckzahlbereichLeer)
            throw new ArgumentException(
                "Ein Stueckzahlbereich der Suche ist leer: „bis“ liegt unter „von“. " +
                "Die Stueckzahl zaehlt in ganzen Schritten ab dem Von-Wert.");
        if (zahl.Befund == FlottenSuchbefund.GroessenbereichUnbrauchbar)
            throw new ArgumentException(
                "Ein Groessenbereich der Suche ist unbrauchbar: „bis“ liegt unter „von“ " +
                "oder ein Wert ist nicht positiv.");
        if (zahl.Befund == FlottenSuchbefund.KeineGeraete)
            throw new ArgumentException(
                "Die gewaehlte Quelle fuehrt kein Geraet, das gerechnet werden koennte. " +
                "Die Groessensuche rechnet vorhandene Speicher; ohne Bestand gibt es keine Kandidaten.");
        if (zahl.Befund == FlottenSuchbefund.KeineAktiveAchse)
            throw new ArgumentException(
                "Die gewaehlte Suchmethode braucht mindestens eine Einheit mit „variieren“; " +
                "ohne sie gibt es nichts zu suchen.");

        if (!zahl.Zulaessig)
            throw new ArgumentException(
                $"Der Suchraum umfasst {zahl.Grob} Kandidaten und ueberschreitet die Grenze " +
                $"{zahl.Grenze}. Er wurde nicht gekuerzt.");

        var achsenwahlen = aktiveAchsen.Select((achse, index) => BildeAchse(achse, index, methode)).ToArray();
        var festeEinheiten = FesteEinheiten(basis, aktiveAchsen);
        var hardware = Kombiniere(festeEinheiten, achsenwahlen);
        var ziele = basis.Auslegung.Betriebsziele.Count == 0
            ? new[] { basis.Optionen.Betriebsziel }
            : basis.Auslegung.Betriebsziele.Distinct().ToArray();

        // Die Groessenkopplung geht ins Ergebnis (Auftrag #226): Sie sagt der
        // Groessen-Sicht, welche zwei Groessen die Karte aufspannt. Seit der Umstellung auf
        // Geraetekandidaten ist es immer Kapazitaet x Leistung - die Achse traegt es
        // trotzdem weiter, damit ein aufbewahrtes Ergebnis seine eigene Auskunft behaelt.
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

        // ES GIBT NUR EINE PHASE: Zwischen zwei Geraeten liegt kein drittes (siehe
        // FlottenAuslegungEingang.Feinraster). FeinrasterGerechnet bleibt damit false.
        RechnePhase(lauf, hardware, ziele, FlottenKandidatPhase.Grob, cancellationToken);

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
    /// <para>Sie baut keine Einheiten: Unter <see cref="FlottenSuchmethode.Groesse"/>
    /// zaehlt sie die GERAETE, die <see cref="FlottenGeraetewahl.Waehle"/> waehlt, unter
    /// <see cref="FlottenSuchmethode.Stueckzahl"/> die Stueckzahlen — je Achse, alles
    /// multipliziert und mit der Zahl der Betriebsziele mal genommen.</para>
    /// <para><b>Die Kandidatenzahl kann nicht mehr explodieren.</b> Sie war das Produkt
    /// zweier Rasterachsen und wuchs mit jeder feineren Schrittweite; jetzt ist sie
    /// hoechstens so gross wie der Geraetebestand der Quelle. Die Schranke
    /// <see cref="FlottenAuslegungEingang.MaximaleKandidaten"/> bleibt trotzdem stehen —
    /// sie zaehlt weiter Geraete × Betriebsziele und faengt einen sehr grossen Bestand ab.</para>
    /// <para><b>Sie wirft nicht.</b> Ein unbrauchbarer Bereich (leer, negativ, nicht positiv)
    /// liefert <see cref="FlottenKandidatenzahl.Gueltig"/> = <c>false</c>; eine Quelle ohne
    /// passendes Geraet ebenso, mit dem Befund <see cref="FlottenSuchbefund.KeineGeraete"/>.</para>
    /// </remarks>
    /// <param name="config">Die Studienkonfiguration samt Suchraum; <c>null</c> = leeres Raster.</param>
    public static FlottenKandidatenzahl Kandidatenzahl(FlottenStudieKonfiguration? config)
    {
        FlottenAuslegungEingang auslegung = config?.Auslegung ?? new FlottenAuslegungEingang();
        FlottenSuchmethode methode = auslegung.Suchmethode;

        // „Nur bewerten" kennt keinen Suchraum: Die eingestellte Flotte ist der eine
        // Kandidat je Betriebsziel — auch dann, wenn Achsen im Stand stehen (SD-Q16).
        var aktive = methode == FlottenSuchmethode.Bewerten
            ? Array.Empty<FlottenAuslegungsAchse>()
            : (auslegung.Achsen ?? new List<FlottenAuslegungsAchse>())
                .Where(x => x is { Aktiv: true }).ToArray();

        long hardware = 1;
        bool gueltig = true;
        FlottenSuchbefund befund = methode != FlottenSuchmethode.Bewerten && aktive.Length == 0
            ? FlottenSuchbefund.KeineAktiveAchse : FlottenSuchbefund.Inordnung;

        foreach (FlottenAuslegungsAchse achse in aktive)
        {
            long n = Achsengroesse(achse, methode);
            if (n < 0)
            {
                gueltig = false;
                if (befund == FlottenSuchbefund.Inordnung)
                    befund = achse is null || achse.AnzahlVon < 0 || achse.AnzahlBis < achse.AnzahlVon
                        ? FlottenSuchbefund.StueckzahlbereichLeer
                        : FlottenSuchbefund.GroessenbereichUnbrauchbar;
                break;
            }
            // KEIN GERAET IST EINE BENANNTE ABLEHNUNG, keine Null: Ein Lauf ohne Kandidaten
            // rechnete nichts und meldete doch „in Ordnung".
            if (n == 0)
            {
                gueltig = false;
                if (befund == FlottenSuchbefund.Inordnung) befund = FlottenSuchbefund.KeineGeraete;
                break;
            }
            if (hardware > long.MaxValue / Math.Max(1L, n)) { gueltig = false; break; }
            hardware *= n;
        }

        int ziele = (auslegung.Betriebsziele?.Count ?? 0) == 0
            ? 1 : auslegung.Betriebsziele!.Distinct().Count();
        long grob = gueltig ? hardware * ziele : 0;

        // KEIN FEINRASTER MEHR: Die Groessensuche waehlt unter vorhandenen Geraeten,
        // zwischen zweien liegt kein drittes (siehe FlottenAuslegungEingang.Feinraster).
        const long fein = 0;

        return new FlottenKandidatenzahl(grob, fein, auslegung.MaximaleKandidaten, gueltig, befund);
    }

    /// <summary>
    /// Die Zahl der Hardwarevarianten EINER Achse unter der gewaehlten Suchmethode;
    /// <c>-1</c> = unbrauchbarer Bereich, <c>0</c> = kein Geraet.
    /// </summary>
    /// <remarks>
    /// <para><b>Unter <see cref="FlottenSuchmethode.Groesse"/></b> ist es die Zahl der
    /// GERAETE, die <see cref="FlottenGeraetewahl.Waehle"/> waehlt — dieselbe Regel, die
    /// auch den Lauf und die Kandidatentabelle fuellt. Die Stueckzahl zaehlt als EIN
    /// Stuetzpunkt (<see cref="FlottenAuslegungsAchse.AnzahlVon"/>, 0 gilt als 1).</para>
    /// <para><b>Unter <see cref="FlottenSuchmethode.Stueckzahl"/></b> ist es die Zahl der
    /// Stueckzahlen Von…Bis, die 0 eingeschlossen — sie bedeutet „diese Einheit
    /// entfaellt"; die Groesse bleibt die der Vorlage.</para>
    /// </remarks>
    private static long Achsengroesse(FlottenAuslegungsAchse achse, FlottenSuchmethode methode)
    {
        if (achse is null || achse.AnzahlVon < 0 || achse.AnzahlBis < achse.AnzahlVon) return -1;
        if (methode == FlottenSuchmethode.Bewerten) return 1;

        if (methode == FlottenSuchmethode.Stueckzahl)
            return achse.AnzahlBis - achse.AnzahlVon + 1L;

        if (!FlottenGeraetewahl.BereichBrauchbar(achse)) return -1;
        return FlottenGeraetewahl.Waehle(achse).Count;
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
        // Die Stueckzahl je aktiver Achse (Auftrag #247) — sie traegt die Achsen der
        // Ergebnissicht unter „Stueckzahl suchen" (SD-Q17).
        Stueckzahlen = variante.Wahlen.Select(w => w.Anzahl).ToList(),
        // DIE HERKUNFT DES GERAETS: Sie steht bei genau EINER Suchachse da — bei mehreren
        // gaebe es mehrere Geraete und damit mehrere Abstaende, und eine Zahl fuer zwei
        // Geraete waere schlimmer als keine. Die Kandidatentabelle zeigt sie.
        Quellkennung = variante.Wahlen.Count == 1 ? variante.Wahlen[0].Quellkennung : "",
        Abweichung = variante.Wahlen.Count == 1 ? variante.Wahlen[0].Abweichung : 0.0,
        NeutraleKennwerte = variante.Wahlen.Any(w => w.NeutraleKennwerte),
        // DIE HERLEITUNG steht wie die Herkunft bei genau EINER Suchachse: Bei mehreren
        // Geraeten waere eine gemeinsame Zeile eine Behauptung ueber zwei Saetze.
        Kennwertherkunft = variante.Wahlen.Count == 1
            ? variante.Wahlen[0].Herkunft
            : FlottenKennwertherkunft.Keine,
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
    /// Unter <see cref="FlottenSuchmethode.Groesse"/> steht hinter jeder Wahl EIN GERAET;
    /// seine Herkunft und sein Abstand zur Vorgabe wandern in die Zusammenfassung des
    /// Kandidaten und von dort in die Kandidatentabelle.
    /// </remarks>
    private sealed class Achsenwahl
    {
        /// <summary>Kennung des Geraets in seiner Quelle; leer unter „Stueckzahl suchen".</summary>
        public string Quellkennung { get; init; } = string.Empty;

        /// <summary>Der normierte Abstand des Geraets zur Vorgabe; 0 = Treffer.</summary>
        public double Abweichung { get; init; }

        /// <summary>Am Geraet musste wenigstens eine neutrale Vorgabe greifen.</summary>
        public bool NeutraleKennwerte { get; init; }

        /// <summary>Was vom GERAET kam; alles Uebrige stammt aus der Vorlage des Anwenders.</summary>
        public FlottenKennwertherkunft Herkunft { get; init; } = FlottenKennwertherkunft.Keine;

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

    /// <summary>
    /// Die Wahlen EINER Suchachse unter der gewaehlten Methode.
    /// </summary>
    /// <remarks>
    /// <para><b>Unter <see cref="FlottenSuchmethode.Groesse"/></b> ist jede Wahl EIN GERAET
    /// aus <see cref="FlottenGeraetewahl.Waehle"/>; die Stueckzahl steht fest auf
    /// <see cref="FlottenAuslegungsAchse.AnzahlVon"/> (0 gilt als 1 — eine Einheit, deren
    /// Geraet gesucht wird, muss es geben).</para>
    /// <para><b>Die Rasterstelle kommt aus den vorkommenden WERTEN.</b> Geraete bilden kein
    /// Gitter: Die Zeilen sind die aufsteigend sortierten VERSCHIEDENEN Kapazitaeten der
    /// gewaehlten Geraete, die Spalten ihre verschiedenen Entladeleistungen. Die Karte der
    /// Groessen-Sicht wird damit DUENN besetzt — jede belegte Zelle ist ein Geraet, das es
    /// wirklich gibt, jede leere eine Groessenkombination, die niemand baut. Das ist die
    /// wahre Auskunft; ein lueckenloses Gitter waere eine erfundene.</para>
    /// <para><b>Unter <see cref="FlottenSuchmethode.Stueckzahl"/></b> laeuft es ueber die
    /// Stueckzahl Von…Bis; die Groesse bleibt Zeichen fuer Zeichen die der Vorlage. Eine
    /// Rasterstelle gibt es dabei NICHT (<c>-1</c>): Das Gitter der Groessen-Sicht spannen
    /// zwei Groessenachsen auf, und die Stueckzahl steht in
    /// <see cref="FlottenKandidatZusammenfassung.Stueckzahlen"/>.</para>
    /// </remarks>
    private static List<Achsenwahl> BildeAchse(FlottenAuslegungsAchse axis, int axisIndex,
                                               FlottenSuchmethode methode)
    {
        if (axis.AnzahlVon < 0 || axis.AnzahlBis < axis.AnzahlVon)
            throw new ArgumentException("Ungueltiger Anzahlbereich in Auslegungsachse.");

        if (methode == FlottenSuchmethode.Stueckzahl) return BildeStueckzahlachse(axis, axisIndex);

        IReadOnlyList<FlottenGeraetekandidat> geraete = FlottenGeraetewahl.Waehle(axis);
        if (geraete.Count == 0)
            throw new ArgumentException(
                "Die Groessensuche fand kein Geraet: Die gewaehlte Quelle fuehrt keinen Satz, " +
                "der sich rechnen liesse.");

        // DIE STUECKZAHL IST EIN STUETZPUNKT: Unter „Groesse suchen" steht sie fest auf dem
        // Von-Wert der Karte; 0 waere „die Einheit entfaellt" und liesse nichts uebrig,
        // dessen Geraet zu suchen waere.
        int count = Math.Max(axis.AnzahlVon, 1);

        var zeilenwerte = geraete.Select(g => g.KapazitaetKWh).Distinct().OrderBy(x => x).ToList();
        var spaltenwerte = geraete.Select(g => g.LeistungKw).Distinct().OrderBy(x => x).ToList();

        var result = new List<Achsenwahl>(geraete.Count);
        foreach (FlottenGeraetekandidat g in geraete)
        {
            List<FlottenEinheit> einheiten = BaueGeraeteeinheiten(axis, g, axisIndex, count,
                                                                  out bool neutral,
                                                                  out FlottenKennwertherkunft herkunft);
            result.Add(new Achsenwahl
            {
                Einheiten = einheiten,
                Rasterzeile = zeilenwerte.IndexOf(g.KapazitaetKWh),
                Rasterspalte = spaltenwerte.IndexOf(g.LeistungKw),
                Anzahl = count,
                ErsterWert = g.KapazitaetKWh,
                ZweiterWert = g.LeistungKw,
                Quellkennung = g.Quellkennung ?? "",
                Abweichung = g.Abweichung,
                NeutraleKennwerte = g.NeutraleKennwerte || neutral,
                Herkunft = herkunft
            });
        }
        return result;
    }

    /// <summary>
    /// Die Einheiten EINER Geraetewahl: <paramref name="count"/> Stueck DESSELBEN Geraets
    /// auf der VORLAGE des Anwenders.
    /// </summary>
    /// <remarks>
    /// <para><b>Geraet ODER eigene Parameter — die eine Regel steht in
    /// <see cref="FlottenGeraeteuebernahme"/>:</b> Die Einheit entsteht aus der Vorlage
    /// der Achse (Schritt 1 des Anwenders), und das Geraet ueberschreibt daraus genau
    /// das, was sein Satz wirklich fuehrt — immer die Groesse, darueber hinaus nur
    /// gefuehrte Kennwerte. Ohne diese Regel fielen Wirkungsgrade, SoC-Band,
    /// Hilfsverbrauch und Kostensaetze des Anwenders bei jeder Geraetewahl weg; unter
    /// „Stueckzahl suchen" bleiben sie seit jeher stehen, und beide Suchmethoden duerfen
    /// nicht verschieden rechnen.</para>
    /// <para><b>Ohne Vorlage bleibt es beim Geraet allein</b> — dann gibt es nichts zu
    /// erhalten. <b>Hier wird nichts skaliert:</b> Fehlt am Ende noch etwas, greifen die
    /// neutralen Vorgaben aus <see cref="FlottenGeraetevorgaben"/> an EINER Stelle, und
    /// <paramref name="neutral"/> sagt es der Kandidatentabelle.</para>
    /// </remarks>
    /// <param name="axis">Die Suchachse; ihre Vorlage traegt die Eingaben des Anwenders.</param>
    /// <param name="kandidat">Das gewaehlte Geraet.</param>
    /// <param name="axisIndex">Nummer der Achse — sie geht in Kennung und Name.</param>
    /// <param name="count">Stueckzahl dieser Wahl.</param>
    /// <param name="neutral">Es musste wenigstens eine neutrale Vorgabe greifen.</param>
    /// <param name="herkunft">Was vom Geraet kam — die Herleitung des Kandidaten.</param>
    private static List<FlottenEinheit> BaueGeraeteeinheiten(FlottenAuslegungsAchse axis,
                                                             FlottenGeraetekandidat kandidat,
                                                             int axisIndex, int count,
                                                             out bool neutral,
                                                             out FlottenKennwertherkunft herkunft)
    {
        neutral = false;
        herkunft = FlottenKennwertherkunft.Groesse;
        var einheiten = new List<FlottenEinheit>(count);
        for (var n = 0; n < count; n++)
        {
            var b = axis?.Vorlage is null
                ? FlottenKopie.Einheit(kandidat.Geraet)
                : FlottenKopie.Einheit(axis.Vorlage);
            herkunft = axis?.Vorlage is null
                ? FlottenKennwertherkunft.Groesse | kandidat.Gefuehrt
                : FlottenGeraeteuebernahme.Uebernehmen(b, kandidat);
            b.Id = $"{(string.IsNullOrWhiteSpace(b.Id) ? "Speicher" : b.Id)}-A{axisIndex + 1}-N{n + 1}";
            b.Name = $"{(string.IsNullOrWhiteSpace(kandidat.Geraet.Name) ? "Speicher" : kandidat.Geraet.Name)} {n + 1}";
            neutral |= FlottenGeraetevorgaben.LueckenFuellen(b);
            einheiten.Add(b);
        }
        return einheiten;
    }

    /// <summary>Die Wahlen der Methode „Stueckzahl suchen": Von…Bis Stueck der Vorlage.</summary>
    private static List<Achsenwahl> BildeStueckzahlachse(FlottenAuslegungsAchse axis, int axisIndex)
    {
        var result = new List<Achsenwahl>();
        for (var count = axis.AnzahlVon; count <= axis.AnzahlBis; count++)
        {
            if (count == 0)
            {
                result.Add(new Achsenwahl { Anzahl = 0 });
                continue;
            }
            result.Add(new Achsenwahl
            {
                Einheiten = BaueEinheiten(axis, axisIndex, count),
                Anzahl = count,
                ErsterWert = axis.Vorlage?.KapazitaetKWh ?? 0.0,
                ZweiterWert = axis.Vorlage?.EntladeleistungKw ?? 0.0
            });
        }
        return result;
    }

    /// <summary>
    /// Die Einheiten EINER Stueckzahlwahl: die Groesse der Vorlage bleibt stehen — ein
    /// Katalog- oder Projektspeicher HAT eine Groesse. Skaliert wird nichts, vervielfacht
    /// die Einheit.
    /// </summary>
    private static List<FlottenEinheit> BaueEinheiten(FlottenAuslegungsAchse axis, int axisIndex,
                                                      int count)
    {
        var stueck = new List<FlottenEinheit>(count);
        for (var n = 0; n < count; n++)
        {
            var v = FlottenKopie.Einheit(axis.Vorlage);
            v.Id = $"{(string.IsNullOrWhiteSpace(v.Id) ? "Speicher" : v.Id)}-A{axisIndex + 1}-N{n + 1}";
            v.Name = $"{(string.IsNullOrWhiteSpace(v.Name) ? "Speicher" : v.Name)} {n + 1}";
            stueck.Add(v);
        }
        return stueck;
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
        Suchmethode = x.Suchmethode,
        Betriebsziele = new List<FlottenBetriebsziel>(x.Betriebsziele),
        Achsen = x.Achsen.Select(a => new FlottenAuslegungsAchse
        {
            Aktiv = a.Aktiv, ErsetztEinheitId = a.ErsetztEinheitId, Modus = a.Modus,
            AnzahlVon = a.AnzahlVon, AnzahlBis = a.AnzahlBis,
            KapazitaetVonKWh = a.KapazitaetVonKWh, KapazitaetBisKWh = a.KapazitaetBisKWh,
            KapazitaetSchrittKWh = a.KapazitaetSchrittKWh, LeistungVonKw = a.LeistungVonKw,
            LeistungBisKw = a.LeistungBisKw, LeistungSchrittKw = a.LeistungSchrittKw,
            CRateVon = a.CRateVon, CRateBis = a.CRateBis, CRateSchritt = a.CRateSchritt,
            Quelle = a.Quelle,
            Geraete = (a.Geraete ?? new List<FlottenGeraetekandidat>())
                .Select(g => new FlottenGeraetekandidat
                {
                    Quellkennung = g.Quellkennung,
                    NeutraleKennwerte = g.NeutraleKennwerte,
                    Gefuehrt = g.Gefuehrt,
                    Abweichung = g.Abweichung,
                    Geraet = FlottenKopie.Einheit(g.Geraet)
                }).ToList(),
            Vorlage = FlottenKopie.Einheit(a.Vorlage)
        }).ToList()
    };
}
