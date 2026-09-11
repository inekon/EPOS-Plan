using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Der hergeleitete Vorschlag für das wirtschaftliche Peak-Ziel H₀ [kW].</summary>
    public sealed class FlottenPeakZielVorschlag
    {
        /// <summary>Das vorgeschlagene Peak-Ziel H₀ [kW].</summary>
        public double PeakZielKw { get; set; }

        /// <summary>Die höchste Nettolast des Zeitraums [kW]; im Rückfall der übergebene Wert, sonst 0.</summary>
        public double ReferenzspitzeKw { get; set; }

        /// <summary>Das Maximum der Tagesminima der Nettolast [kW]; im Rückfall 0.</summary>
        public double TagesminimumMaxKw { get; set; }

        /// <summary>Die Summe der Entladeleistungen der Flotte [kW].</summary>
        public double SummeEntladeleistungKw { get; set; }

        /// <summary>Der Vorschlag stammt aus einer vorliegenden Zeitreihe; sonst ist es ein benannter Rückfall.</summary>
        public bool AusReihe { get; set; }

        /// <summary>Die Herleitungszeile im Klartext; sie gehört unter das Eingabefeld.</summary>
        public string Herleitung { get; set; } = "";
    }

    /// <summary>Der Fortschritt der Bisektion „Peak-Ziel bestimmen".</summary>
    public sealed class FlottenPeakZielFortschritt
    {
        /// <summary>Zahl der bereits gerechneten Jahresläufe.</summary>
        public int Lauf { get; set; }

        /// <summary>Die Höchstzahl der Jahresläufe dieser Suche.</summary>
        public int HoechsteLaeufe { get; set; }

        /// <summary>Das zuletzt geprüfte Peak-Ziel H [kW].</summary>
        public double PeakZielKw { get; set; }

        /// <summary>Die dabei verbliebene Bezugsspitze [kW].</summary>
        public double ErreichteSpitzeKw { get; set; }
    }

    /// <summary>Das Ergebnis der Bisektion „Peak-Ziel bestimmen".</summary>
    public sealed class FlottenPeakZielErgebnis
    {
        /// <summary>Das kleinste gefundene Peak-Ziel H [kW], das die Flotte noch hält.</summary>
        public double PeakZielKw { get; set; }

        /// <summary>Die bei <see cref="PeakZielKw"/> verbleibende Bezugsspitze [kW].</summary>
        public double VerbleibendeSpitzeKw { get; set; }

        /// <summary>Die höchste Nettolast des Zeitraums [kW] — die obere Schranke der Suche.</summary>
        public double ReferenzspitzeKw { get; set; }

        /// <summary>Das Maximum der Tagesminima der Nettolast [kW] — die untere Schranke der Suche.</summary>
        public double GrundlastKw { get; set; }

        /// <summary>Zahl der gerechneten Jahresläufe.</summary>
        public int Laeufe { get; set; }

        /// <summary>Die Suche hat das Intervall bis unter die Abbruchschranke eingeschlossen.</summary>
        public bool Konvergiert { get; set; }

        /// <summary>Die Herleitungszeile im Klartext.</summary>
        public string Herleitung { get; set; } = "";
    }

    /// <summary>
    /// Vorbelegung und Bestimmung des wirtschaftlichen Peak-Ziels einer Speicherflotte
    /// (Konzept Stromspeicher-Dialoge 2.4 Punkte 1 und 2, Anwenderentscheid SD‑Q3 vom
    /// 11.09.2026, Aufgabe #183).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Der Befund SP‑O‑10: Die Vorbelegung war fest 50 kW, unabhängig von der Last. Bei
    /// einer Grundlast von 70…80 kW und einer Spitze von 789 kW war die Anforderung
    /// <c>n − H</c> in JEDEM Intervall positiv, der Ladedeckel <c>max(0, H − n)</c>
    /// dauerhaft 0 — die Flotte konnte weder laden noch entladen.
    /// </para>
    /// <para>
    /// Nichts hier ändert einen gespeicherten Stand: Die Vorbelegung greift nur beim
    /// Anlegen, die Bisektion nur auf Knopfdruck.
    /// </para>
    /// </remarks>
    public static class FlottenPeakZiel
    {
        /// <summary>
        /// Der benannte Rückfall [kW], wenn WEDER eine Zeitreihe NOCH eine Bezugsspitze
        /// vorliegt — etwa vor dem ersten Simulationslauf eines Projekts. Er ersetzt die
        /// bisherige, an zwei Stellen fest verdrahtete 50; ein Wert ist nötig, weil die
        /// Lastspitzenkappung ohne Zielwert nicht rechnet (Spezifikation 5.1).
        /// </summary>
        public const double RueckfallPeakZielKw = 50.0;

        /// <summary>
        /// Anteil [-] der Bezugsspitze, der im Rückfall an die Stelle des unbekannten
        /// Maximums der Tagesminima tritt. Ohne Zeitreihe ist die Grundlast nicht bekannt;
        /// ein Peak-Ziel von 0 wäre aber sinnlos, weil dann in jedem Intervall gekappt
        /// werden müsste. Zehn Prozent der Spitze sind eine sichtbare, korrigierbare
        /// Untergrenze — kein Rechenwert hängt daran.
        /// </summary>
        public const double GrundlastAnteilImRueckfall = 0.1;

        /// <summary>Die Höchstzahl der Jahresläufe der Bisektion (Konzept 2.4 Punkt 2: „höchstens ~12").</summary>
        public const int HoechsteLaeufe = 12;

        /// <summary>
        /// Die Nettolast <c>n = Last − PV − BHKW + Hilfsverbrauch</c> [kW] je Intervall —
        /// dieselbe Größe, gegen die der Simulator das Peak-Ziel hält.
        /// </summary>
        /// <param name="reihe">Die Istreihe des Standorts.</param>
        /// <param name="flotte">Die Flotte; ihr Hilfsverbrauch zählt als zusätzliche Standortlast. <c>null</c> = ohne Hilfsverbrauch.</param>
        /// <returns>Die Nettolast je Intervall [kW]; sie darf negativ sein (Überschuss).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="reihe"/> ist <c>null</c>.</exception>
        public static double[] Nettolast(IReadOnlyList<FlottenNetzintervall> reihe,
            FlottenStudieKonfiguration flotte)
        {
            ArgumentNullException.ThrowIfNull(reihe);
            double hilfsverbrauch = flotte?.Einheiten?.Sum(x => x.HilfsverbrauchKw) ?? 0.0;
            double[] netto = new double[reihe.Count];
            for (int i = 0; i < reihe.Count; i++)
                netto[i] = reihe[i].LastKw - reihe[i].PvKw - reihe[i].BhkwKw + hilfsverbrauch;
            return netto;
        }

        /// <summary>
        /// Der Vorschlag H₀ aus der Referenzzeitreihe und der Flotte:
        /// <c>H₀ = max(Referenzspitze − Σ Entladeleistung; max der Tagesminima)</c>.
        /// </summary>
        /// <remarks>
        /// Der erste Term sagt, wie tief die Flotte überhaupt kappen kann; der zweite
        /// verhindert ein Ziel, unter das die Last nie fällt — dann könnte die Flotte
        /// nach der Regel „Wiederaufladung erzeugt keinen neuen Peak über H" nie wieder
        /// laden (Spezifikation 5.1).
        /// </remarks>
        /// <param name="reihe">Die Istreihe des Standorts; die Tage werden nach UTC-Datum getrennt.</param>
        /// <param name="flotte">Die Flotte, aus der Entladeleistung und Hilfsverbrauch stammen.</param>
        /// <returns>Der Vorschlag samt Herleitungszeile.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="reihe"/> ist <c>null</c>.</exception>
        public static FlottenPeakZielVorschlag Vorschlag(IReadOnlyList<FlottenNetzintervall> reihe,
            FlottenStudieKonfiguration flotte)
        {
            ArgumentNullException.ThrowIfNull(reihe);
            double[] netto = Nettolast(reihe, flotte);
            if (netto.Length == 0) return Rueckfall(0, SummeEntladeleistung(flotte));
            return Baue(netto.Max(), MaximumDerTagesminima(netto, reihe),
                SummeEntladeleistung(flotte), true);
        }

        /// <summary>
        /// Wie <see cref="Vorschlag(IReadOnlyList{FlottenNetzintervall}, FlottenStudieKonfiguration)"/>,
        /// aber auf einer bereits gebildeten Nettolastreihe mit festem Tagesraster —
        /// der Weg für das EPOS-Modelljahr, das ohne Kalenderachse auskommt.
        /// </summary>
        /// <param name="nettolastKw">Die Nettolast je Intervall [kW].</param>
        /// <param name="intervalleJeTag">Intervalle je Tag (96 im Viertelstundenraster); kleiner 1 bedeutet „ein einziger Tag".</param>
        /// <param name="summeEntladeleistungKw">Die Summe der Entladeleistungen der Flotte [kW].</param>
        /// <returns>Der Vorschlag samt Herleitungszeile.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="nettolastKw"/> ist <c>null</c>.</exception>
        public static FlottenPeakZielVorschlag Vorschlag(IReadOnlyList<double> nettolastKw,
            int intervalleJeTag, double summeEntladeleistungKw)
        {
            ArgumentNullException.ThrowIfNull(nettolastKw);
            if (nettolastKw.Count == 0) return Rueckfall(0, summeEntladeleistungKw);
            return Baue(nettolastKw.Max(), MaximumDerTagesminima(nettolastKw, intervalleJeTag),
                summeEntladeleistungKw, true);
        }

        /// <summary>
        /// Der benannte Rückfall, wenn keine Zeitreihe vorliegt: Aus der bekannten
        /// Bezugsspitze folgt <c>max(Spitze − Σ Entladeleistung; Anteil der Spitze)</c>,
        /// ohne sie <see cref="RueckfallPeakZielKw"/>.
        /// </summary>
        /// <param name="referenzspitzeKw">Die Bezugsspitze des Lastgangs [kW]; 0 oder kleiner = unbekannt.</param>
        /// <param name="summeEntladeleistungKw">Die Summe der Entladeleistungen der Flotte [kW].</param>
        /// <returns>Der Rückfallvorschlag samt Herleitungszeile.</returns>
        public static FlottenPeakZielVorschlag Rueckfall(double referenzspitzeKw, double summeEntladeleistungKw)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            if (!double.IsFinite(referenzspitzeKw) || referenzspitzeKw <= 0)
                return new FlottenPeakZielVorschlag
                {
                    PeakZielKw = RueckfallPeakZielKw,
                    SummeEntladeleistungKw = Math.Max(0, summeEntladeleistungKw),
                    AusReihe = false,
                    Herleitung = string.Format(k,
                        "Ohne Lastgang und ohne Bezugsspitze: Rückfallwert {0} kW. " +
                        "Nach dem ersten Simulationslauf wird das Peak-Ziel aus der Referenz hergeleitet.",
                        RueckfallPeakZielKw.ToString("0.#", k))
                };

            double entladeleistung = Math.Max(0, double.IsFinite(summeEntladeleistungKw) ? summeEntladeleistungKw : 0);
            double untergrenze = referenzspitzeKw * GrundlastAnteilImRueckfall;
            double h = Math.Max(Math.Max(0, referenzspitzeKw - entladeleistung), untergrenze);
            return new FlottenPeakZielVorschlag
            {
                PeakZielKw = h,
                ReferenzspitzeKw = referenzspitzeKw,
                SummeEntladeleistungKw = entladeleistung,
                AusReihe = false,
                Herleitung = string.Format(k,
                    "Ohne Lastgang: H₀ = max(Bezugsspitze {0} kW − Σ Entladeleistung {1} kW; {2} % der Spitze = {3} kW) = {4} kW. " +
                    "Die Grundlast ist ohne Zeitreihe nicht bekannt.",
                    referenzspitzeKw.ToString("0.#", k), entladeleistung.ToString("0.#", k),
                    (GrundlastAnteilImRueckfall * 100).ToString("0.#", k),
                    untergrenze.ToString("0.#", k), h.ToString("0.#", k))
            };
        }

        /// <summary>
        /// Bestimmt per Bisektion das kleinste Peak-Ziel H, bei dem die verbleibende
        /// Bezugsspitze der Variante noch unter H bleibt (Konzept 2.4 Punkt 2).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Gesucht wird zwischen der Grundlast (Maximum der Tagesminima) und der
        /// Referenzspitze. Jede Halbierung kostet EINEN Jahreslauf des Simulators;
        /// höchstens <see cref="HoechsteLaeufe"/> Läufe werden gerechnet.
        /// </para>
        /// <para>
        /// Gerechnet wird ausdrücklich mit <see cref="FlottenBetriebsziel.PeakShaving"/> —
        /// die Frage „wie tief komme ich mit dieser Flotte" ist die Frage der
        /// Lastspitzenkappung. Für die PLANENDEN Ziele wird die Bestimmung abgewiesen:
        /// Sie bräuchten je Lauf den MILP-Planer, und zwölf geplante Jahresläufe sind
        /// keine Bedienhandlung (SP‑O‑1).
        /// </para>
        /// </remarks>
        /// <param name="eingang">Die Standortzeitreihen des Laufs.</param>
        /// <param name="konfiguration">Die Flotte samt Betriebsoptionen; sie wird kopiert und nicht verändert.</param>
        /// <param name="planer">Der Planer; für die zugelassenen reaktiven Ziele wird er nicht gebraucht und darf <c>null</c> sein.</param>
        /// <param name="fortschritt">Meldung je gerechnetem Jahreslauf; <c>null</c> = keine Meldung.</param>
        /// <param name="token">Abbruchmarke; sie wird je Lauf und innerhalb des Simulators je Intervall geprüft.</param>
        /// <returns>Das gefundene Peak-Ziel, die verbleibende Spitze, die Zahl der Läufe und die Herleitung.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="eingang"/> oder <paramref name="konfiguration"/> ist <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Das Betriebsziel ist planend, die Flotte ist leer oder die Zeitreihe fehlt.</exception>
        public static FlottenPeakZielErgebnis PeakZielBestimmen(
            FlottenEingang eingang,
            FlottenStudieKonfiguration konfiguration,
            IFlottenPlaner planer,
            IProgress<FlottenPeakZielFortschritt> fortschritt,
            CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(eingang);
            ArgumentNullException.ThrowIfNull(konfiguration);
            FlottenBetriebsziel ziel = konfiguration.Optionen.Betriebsziel;
            if (ziel is FlottenBetriebsziel.PvPlanung or FlottenBetriebsziel.Arbitrage
                or FlottenBetriebsziel.MultiUse)
                throw new InvalidOperationException(
                    "Das Peak-Ziel lässt sich nur für die reaktiven Betriebsziele Lastspitzenkappung und " +
                    "PV-Eigenverbrauch bestimmen. Ein planendes Ziel bräuchte für jeden der bis zu " +
                    HoechsteLaeufe.ToString(CultureInfo.InvariantCulture) +
                    " Jahresläufe den Fahrplaner.");
            if (konfiguration.Einheiten == null || konfiguration.Einheiten.Count == 0)
                throw new InvalidOperationException(
                    "Ohne mindestens einen physischen Speicher lässt sich kein Peak-Ziel bestimmen.");
            if (eingang.Istwerte == null || eingang.Istwerte.Count == 0)
                throw new InvalidOperationException(
                    "Ohne Standortzeitreihe lässt sich kein Peak-Ziel bestimmen.");

            CultureInfo k = CultureInfo.CurrentCulture;
            double[] netto = Nettolast(eingang.Istwerte, konfiguration);
            double referenzspitze = Math.Max(0, netto.Max());
            double grundlast = Math.Max(0, MaximumDerTagesminima(netto, eingang.Istwerte));

            var ergebnis = new FlottenPeakZielErgebnis
            {
                PeakZielKw = referenzspitze,
                VerbleibendeSpitzeKw = referenzspitze,
                ReferenzspitzeKw = referenzspitze,
                GrundlastKw = grundlast,
                Konvergiert = true
            };
            if (!(referenzspitze > grundlast))
            {
                ergebnis.Herleitung = string.Format(k,
                    "Die Bezugsspitze {0} kW liegt nicht über der Grundlast {1} kW; es gibt keine Spitze zu kappen.",
                    referenzspitze.ToString("0.#", k), grundlast.ToString("0.#", k));
                return ergebnis;
            }

            FlottenStudieKonfiguration probe = SpeicherAuslegungKopie.Von(konfiguration);
            probe.Optionen.Betriebsziel = FlottenBetriebsziel.PeakShaving;
            // Ohne Endenergiegleichheit verlangt der Simulator einen offengelegten
            // Ausgleichswert. Für die Suche zählt allein die verbleibende Spitze; der
            // Wert bewertet nur die Bestandsänderung und wird hier nicht gelesen.
            probe.Optionen.EnergieAusgleichEuroProKWh ??= 0;
            probe.Wirtschaftlichkeit.Jahreskonten = new List<FlottenJahreskonto>();

            double unten = grundlast, oben = referenzspitze;
            double schranke = Math.Max(0.01, (oben - unten) * 1e-4);
            while (ergebnis.Laeufe < HoechsteLaeufe && oben - unten > schranke)
            {
                token.ThrowIfCancellationRequested();
                double mitte = 0.5 * (unten + oben);
                probe.Optionen.WirtschaftlicherPeakZielwertKw = mitte;
                double spitze = FlottenSimulator.Simuliere(eingang, probe, planer, token)
                    .Variante.MaximalerNetzbezugKw;
                ergebnis.Laeufe++;
                fortschritt?.Report(new FlottenPeakZielFortschritt
                {
                    Lauf = ergebnis.Laeufe,
                    HoechsteLaeufe = HoechsteLaeufe,
                    PeakZielKw = mitte,
                    ErreichteSpitzeKw = spitze
                });
                if (spitze <= mitte + schranke)
                {
                    oben = mitte;
                    ergebnis.PeakZielKw = mitte;
                    ergebnis.VerbleibendeSpitzeKw = spitze;
                }
                else unten = mitte;
            }

            ergebnis.Konvergiert = oben - unten <= schranke;
            ergebnis.Herleitung = string.Format(k,
                "Bisektion zwischen Grundlast {0} kW und Bezugsspitze {1} kW: kleinstes gehaltenes Peak-Ziel " +
                "{2} kW, verbleibende Bezugsspitze {3} kW, {4} Jahresläufe{5}.",
                grundlast.ToString("0.#", k), referenzspitze.ToString("0.#", k),
                ergebnis.PeakZielKw.ToString("0.#", k), ergebnis.VerbleibendeSpitzeKw.ToString("0.#", k),
                ergebnis.Laeufe.ToString(k),
                ergebnis.Konvergiert ? "" : " (Höchstzahl erreicht, Wert ist eine obere Schranke)");
            return ergebnis;
        }

        /// <summary>Das Maximum der Tagesminima [kW]; die Tage werden nach dem UTC-Datum getrennt.</summary>
        /// <param name="netto">Die Nettolast je Intervall [kW].</param>
        /// <param name="reihe">Die zugehörige Reihe mit den Zeitstempeln.</param>
        /// <returns>Das Maximum der Tagesminima; 0 bei leerer Reihe.</returns>
        private static double MaximumDerTagesminima(IReadOnlyList<double> netto,
            IReadOnlyList<FlottenNetzintervall> reihe)
        {
            double maximum = double.NegativeInfinity, minimum = double.PositiveInfinity;
            DateTime tag = default;
            bool erster = true;
            for (int i = 0; i < netto.Count; i++)
            {
                DateTime heute = reihe[i].Zeitstempel.UtcDateTime.Date;
                if (erster) { tag = heute; erster = false; }
                else if (heute != tag)
                {
                    maximum = Math.Max(maximum, minimum);
                    minimum = double.PositiveInfinity;
                    tag = heute;
                }
                minimum = Math.Min(minimum, netto[i]);
            }
            if (!erster) maximum = Math.Max(maximum, minimum);
            return double.IsNegativeInfinity(maximum) ? 0 : maximum;
        }

        /// <summary>Das Maximum der Tagesminima [kW] auf einem festen Tagesraster.</summary>
        /// <param name="netto">Die Nettolast je Intervall [kW].</param>
        /// <param name="intervalleJeTag">Intervalle je Tag; kleiner 1 bedeutet „ein einziger Tag".</param>
        /// <returns>Das Maximum der Tagesminima; 0 bei leerer Reihe.</returns>
        private static double MaximumDerTagesminima(IReadOnlyList<double> netto, int intervalleJeTag)
        {
            if (netto.Count == 0) return 0;
            int schritt = intervalleJeTag > 0 ? intervalleJeTag : netto.Count;
            double maximum = double.NegativeInfinity;
            for (int start = 0; start < netto.Count; start += schritt)
            {
                double minimum = double.PositiveInfinity;
                int ende = Math.Min(start + schritt, netto.Count);
                for (int i = start; i < ende; i++) minimum = Math.Min(minimum, netto[i]);
                maximum = Math.Max(maximum, minimum);
            }
            return double.IsNegativeInfinity(maximum) ? 0 : maximum;
        }

        /// <summary>Die Summe der Entladeleistungen [kW] einer Flotte; 0 ohne Flotte.</summary>
        /// <param name="flotte">Die Flotte oder <c>null</c>.</param>
        /// <returns>Die Summe der Entladeleistungen [kW].</returns>
        private static double SummeEntladeleistung(FlottenStudieKonfiguration flotte) =>
            flotte?.Einheiten?.Sum(x => x.EntladeleistungKw) ?? 0.0;

        /// <summary>Setzt den Vorschlag aus den drei Größen zusammen und schreibt die Herleitungszeile.</summary>
        /// <param name="referenzspitze">Die höchste Nettolast [kW].</param>
        /// <param name="tagesminimumMax">Das Maximum der Tagesminima [kW].</param>
        /// <param name="summeEntladeleistung">Die Summe der Entladeleistungen [kW].</param>
        /// <param name="ausReihe">Der Vorschlag stammt aus einer Zeitreihe.</param>
        /// <returns>Der fertige Vorschlag.</returns>
        private static FlottenPeakZielVorschlag Baue(double referenzspitze, double tagesminimumMax,
            double summeEntladeleistung, bool ausReihe)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            double entladeleistung = Math.Max(0, double.IsFinite(summeEntladeleistung) ? summeEntladeleistung : 0);
            double spitze = double.IsFinite(referenzspitze) ? referenzspitze : 0;
            double grundlast = double.IsFinite(tagesminimumMax) ? tagesminimumMax : 0;
            double ohneFlotte = Math.Max(0, spitze - entladeleistung);
            double h = Math.Max(0, Math.Max(ohneFlotte, grundlast));
            return new FlottenPeakZielVorschlag
            {
                PeakZielKw = h,
                ReferenzspitzeKw = spitze,
                TagesminimumMaxKw = grundlast,
                SummeEntladeleistungKw = entladeleistung,
                AusReihe = ausReihe,
                Herleitung = string.Format(k,
                    "H₀ = max(Referenzspitze {0} kW − Σ Entladeleistung {1} kW; Maximum der Tagesminima {2} kW) = {3} kW.",
                    spitze.ToString("0.#", k), entladeleistung.ToString("0.#", k),
                    grundlast.ToString("0.#", k), h.ToString("0.#", k))
            };
        }
    }
}
