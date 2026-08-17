using System;
using System.Collections.Generic;

namespace SpeicherEngine
{
    /// <summary>
    /// Strategie (d) Peak-Shaving (Fachkonzept 6.4): Kappung der Netzbezugsspitze
    /// zur Senkung des Leistungspreises.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Herkunft.</b> Der Algorithmus ist der verifizierte Port der
    /// Lastgangauswertung (Arbeitsmappe <c>Lastgangauswertung_2024_Kauffmann-V4.xlsm</c>,
    /// Blatt <c>Daten</c>) und wurde <b>nicht neu entworfen</b>. Zwei Anpassungen an die
    /// Modulkonventionen sind vorgenommen: das SoC-Band (<c>SoC_min .. SoC_max</c> statt
    /// <c>0 .. E_max</c>) und die energetischen Wirkungsgrade. Der
    /// <see cref="SpeicherModus.ExcelKompatibilitaet"/> stellt die Originalfassung her
    /// und traegt den Regressionstest.
    /// </para>
    /// <para>
    /// <b>Steuergroesse ist die Lastschwelle, nicht die Residuallast.</b> Es geht hier
    /// weder Erzeugung noch Preiszeitreihe ein - <see cref="SpeicherEingang.PvKw"/> und
    /// <see cref="SpeicherEingang.PreisCtKwh"/> bleiben ohne Wirkung. Peak-Shaving ist
    /// deshalb auch ohne konfigurierte PV/BHKW-Kette lauffaehig (Fachkonzept 6.4,
    /// Abgrenzung Rev. 4); <see cref="NurLast"/> baut den passenden Eingang.
    /// </para>
    /// <para>
    /// <b>Unterschied der Kompatibilitaetsmodi.</b> Anders als bei
    /// <see cref="Dauernutzung"/> laesst der Kompatibilitaetsmodus hier
    /// <b>Intervall 0 nicht aus</b>: die Vorlage rechnet das erste Viertelstunden-
    /// intervall mit (Zeile 3 des Blattes ist der erste simulierte Wert). Er setzt
    /// ausschliesslich <c>eta_ch = eta_dis = 1</c>, <c>SoC_min = 0</c> und
    /// <c>Start-SoC = 0</c>. Auf die Monetarisierung wirkt er <b>nicht</b> - die
    /// Vorlage kennt keine Wirtschaftlichkeitsrechnung, die Bewertung nach 6.4 ist
    /// neu und gilt in beiden Modi gleich (insbesondere ohne den pauschalen
    /// Verlustfaktor der V7-Mappe).
    /// </para>
    /// <para>
    /// Die Instanz haelt nur Modus und Parameter und ist damit unveraenderlich und
    /// thread-sicher; dieselbe Instanz darf in <c>Parallel.For</c> verwendet werden.
    /// </para>
    /// </remarks>
    public sealed class PeakShaving : ISpeicherStrategie
    {
        /// <summary>Kumulierte Tage vor dem jeweiligen Monat, Gemeinjahr (Index 0 = Januar).</summary>
        private static readonly int[] TageVorMonatGemein =
            { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };

        /// <summary>Kumulierte Tage vor dem jeweiligen Monat, Schaltjahr.</summary>
        private static readonly int[] TageVorMonatSchalt =
            { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };

        private readonly SpeicherModus _modus;
        private readonly PeakShavingParameter _ps;

        /// <summary>Erzeugt die Strategie mit ihren Steuergroessen (Default: energetisch).</summary>
        /// <param name="psParameter">Schwelle/Adaptiv-Flag und Bewertungsgroessen.</param>
        /// <param name="modus">Rechenmodus.</param>
        public PeakShaving(PeakShavingParameter psParameter,
                           SpeicherModus modus = SpeicherModus.Energetisch)
        {
            _ps = psParameter ?? throw new ArgumentNullException(nameof(psParameter));
            _modus = modus;
        }

        /// <summary>Rechenmodus dieser Instanz.</summary>
        public SpeicherModus Modus => _modus;

        /// <summary>Steuergroessen dieser Instanz.</summary>
        public PeakShavingParameter Parameter => _ps;

        /// <inheritdoc/>
        public string Name => _modus == SpeicherModus.ExcelKompatibilitaet
            ? "Peak-Shaving (Excel-Kompatibilitaet)"
            : "Peak-Shaving";

        /// <summary>
        /// Baut einen <see cref="SpeicherEingang"/> allein aus dem Lastgang - der
        /// Regelfall des separaten Peak-Shaving-Einstiegs, der ohne Erzeugungs- und
        /// Preisreihen auskommt (Fachkonzept 6.4, Abgrenzung Rev. 4).
        /// </summary>
        public static SpeicherEingang NurLast(double[] lastKw)
        {
            if (lastKw == null) throw new ArgumentNullException(nameof(lastKw));
            return SpeicherEingang.MitFixpreis(lastKw, new double[lastKw.Length], 0.0);
        }

        /// <summary>
        /// Rechnet einen Jahreslauf im gemeinsamen Ergebnisformat der Engine.
        /// Entspricht <c><see cref="BerechnePeakShaving(SpeicherEingang, SpeicherParameter)"/>.Basis</c>.
        /// </summary>
        public SpeicherErgebnis Berechne(SpeicherEingang eingang, SpeicherParameter p)
            => BerechnePeakShaving(eingang, p).Basis;

        /// <summary>Rechnet einen Jahreslauf mit dem vollstaendigen Peak-Shaving-Ergebnis.</summary>
        public PeakShavingErgebnis BerechnePeakShaving(SpeicherEingang eingang, SpeicherParameter p)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            return BerechnePeakShaving(eingang.LastKw, p);
        }

        /// <summary>
        /// Rechnet einen Jahreslauf allein aus dem Lastgang (Fachkonzept 6.4).
        /// </summary>
        /// <param name="lastKw">Lastgang [kW] je Intervall, ueblich 35.040 oder 8.760 Werte.</param>
        /// <param name="p">Speicher- und Wirtschaftlichkeitsparameter.</param>
        /// <remarks>
        /// <para>Portierter Pseudocode (Fachkonzept 6.4):</para>
        /// <code>
        /// P_ziel = adaptiv ? 0 : P_ziel_vorgabe
        /// SoC    = SoC_start
        /// fuer jedes Intervall i:
        ///     dMax = min( P, (SoC - SoC_min)*eta_dis/dt )
        ///     if adaptiv and (P_last[i] - dMax) &gt; P_ziel:
        ///         P_ziel = P_last[i] - dMax
        ///     if P_last[i] &gt; P_ziel:
        ///         pd       = min( dMax, P_last[i] - P_ziel )
        ///         P_neu[i] = P_last[i] - pd ;  SoC -= pd*dt/eta_dis
        ///     else:
        ///         pc       = min( P, P_ziel - P_last[i], (SoC_max - SoC)/(eta_ch*dt) )
        ///         P_neu[i] = P_last[i] + pc ;  SoC += pc*dt*eta_ch
        /// </code>
        /// <para>
        /// Die Reihenfolge der Begrenzungen ist unveraendert uebernommen, damit die
        /// IEEE-754-Doubles im Kompatibilitaetsmodus bitgleich zur Vorlage
        /// herauskommen.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Wenn eine Pflichtangabe fehlt.</exception>
        /// <exception cref="ArgumentException">Wenn der Lastgang leer ist.</exception>
        public PeakShavingErgebnis BerechnePeakShaving(double[] lastKw, SpeicherParameter p)
        {
            if (lastKw == null) throw new ArgumentNullException(nameof(lastKw));
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (lastKw.Length == 0)
                throw new ArgumentException("Der Lastgang darf nicht leer sein.", nameof(lastKw));
            p.Pruefe();
            _ps.Pruefe();

            bool kompatibel = _modus == SpeicherModus.ExcelKompatibilitaet;

            int n = lastKw.Length;
            double dt = p.DtH;
            double maxPower = p.PKw;
            double socMin = kompatibel ? 0.0 : p.SoCMinKwh;
            double socMax = p.SoCMaxKwh;
            double etaCh = kompatibel ? 1.0 : p.EtaCh;
            double etaDis = kompatibel ? 1.0 : p.EtaDis;
            double startSoC = kompatibel ? 0.0 : p.StartSoCEffektivKwh;
            bool adaptiv = _ps.Adaptiv;

            double[] pAlt = (double[])lastKw.Clone();
            double[] pNeu = new double[n];
            double[] soc = new double[n];
            double[] ladung = new double[n];
            double[] entladung = new double[n];
            double[] geldwert = new double[n];

            double ziel = adaptiv ? 0.0 : _ps.PZielKw;
            double stand = startSoC;
            double ladeenergie = 0.0;
            double entladeenergie = 0.0;
            double entladeenergieDc = 0.0;
            double pAltMax = double.NegativeInfinity;
            double pNeuMax = double.NegativeInfinity;
            double preis = _ps.BezugspreisMittelCtKwh;

            for (int i = 0; i < n; i++)
            {
                double last = pAlt[i];

                // Maximal moegliche Entladeleistung dieses Intervalls [kW].
                double dMax = (stand - socMin) * etaDis / dt;
                if (dMax > maxPower) dMax = maxPower;
                if (dMax < 0.0) dMax = 0.0;

                // Adaptiv: die Schwelle nur so weit nachziehen, wie sie nicht zu
                // halten ist. Sie steigt monoton und ist am Ende die minimal
                // erreichbare Spitze.
                if (adaptiv && last - dMax > ziel) ziel = last - dMax;

                double pd = 0.0;   // Entladeleistung [kW], AC-seitig
                double pc = 0.0;   // Ladeleistung    [kW], AC-seitig

                if (last > ziel)
                {
                    pd = last - ziel;
                    if (pd > dMax) pd = dMax;
                    if (pd < 0.0) pd = 0.0;

                    pNeu[i] = last - pd;
                    stand -= pd * dt / etaDis;
                }
                else
                {
                    // Laden, ohne die Schwelle zu reissen.
                    pc = maxPower;
                    double schranke = ziel - last;
                    if (schranke < pc) pc = schranke;
                    schranke = (socMax - stand) / (etaCh * dt);
                    if (schranke < pc) pc = schranke;
                    if (pc < 0.0) pc = 0.0;

                    pNeu[i] = last + pc;
                    stand += pc * dt * etaCh;
                }

                soc[i] = stand;

                double eCh = pc * dt;
                double eDis = pd * dt;
                ladung[i] = eCh;
                entladung[i] = eDis;
                ladeenergie += eCh;
                entladeenergie += eDis;
                entladeenergieDc += eDis / etaDis;

                // Intervallaufgeloeste Bewertung der Verschiebung: was zusaetzlich
                // bezogen wird, kostet den mittleren Bezugspreis; was entnommen wird,
                // spart ihn. Die Summe ist der zweite Term der Monetarisierung.
                geldwert[i] = -(eCh - eDis) * preis / 100.0;

                if (last > pAltMax) pAltMax = last;
                if (pNeu[i] > pNeuMax) pNeuMax = pNeu[i];
            }

            // ---------------------------------------------------- Monetarisierung 6.4
            double kappung = pAltMax - pNeuMax;
            double leistungspreisersparnis = kappung * _ps.LeistungspreisEurProKwA;
            double verlustkosten = (ladeenergie - entladeenergie) * preis / 100.0;
            double ertragPs = leistungspreisersparnis - verlustkosten;

            WirtschaftlichkeitErgebnis w = Wirtschaftlichkeit.Berechne(new WirtschaftlichkeitEingang
            {
                ErtragReferenzjahrEur = ertragPs,
                InvestitionEur = p.InvestitionEur,
                Kapitalzins = p.Kapitalzins,
                NutzungsdauerA = p.NutzungsdauerA,
                DegradationProA = p.DegradationProA
            });

            double cNutz = kompatibel ? socMax - socMin : p.CNutzKwh;
            double vollzyklen = cNutz > 0.0 ? entladeenergieDc / cNutz : 0.0;

            // Bilanzgroessen: "ohne Speicher" ist hier der ungekappte Lastgang, "mit
            // Speicher" der gekappte. Erzeugung gibt es in dieser Strategie nicht,
            // die Quellen- und Einspeisefelder bleiben deshalb leer.
            double lastKwh = Numerik.SummeSequenziell(pAlt) * dt;
            SpeicherKennzahlen kennzahlen = new SpeicherKennzahlen
            {
                EntladeenergieDcKwh = entladeenergieDc,
                AequivalenteVollzyklen = vollzyklen,
                VerschleisskostenEurProA = vollzyklen * p.CNomKwh * p.CVerEurProKwhZyklus,
                SpeicherverlusteKwh = ladeenergie - entladeenergie - (stand - startSoC),
                LastKwh = lastKwh,
                NetzbezugOhneSpeicherKwh = lastKwh,
                NetzbezugMitSpeicherKwh = lastKwh + ladeenergie - entladeenergie
            };

            SpeicherErgebnis basis = new SpeicherErgebnis(
                soc, geldwert, Numerik.SummeSequenziell(geldwert),
                ladeenergie, entladeenergie, _modus, w, kennzahlen, ladung, entladung);

            // Im festen Modus zeigt eine ueberschrittene Schwelle an, dass der
            // Speicher zu klein ist; im adaptiven Modus ist sie per Konstruktion
            // erreicht.
            bool gerissen = !adaptiv &&
                            pNeuMax > ziel + 1e-9 * Math.Max(1.0, Math.Abs(ziel));

            return new PeakShavingErgebnis(
                basis, pAlt, pNeu, pAltMax, pNeuMax, ziel, gerissen,
                Monatsspitzen(pAlt, pNeu, dt),
                leistungspreisersparnis, verlustkosten, ertragPs);
        }

        /// <summary>
        /// Sucht die <b>kleinste feste Zielschwelle</b> [kW], die derselbe Speicher
        /// ueber den ganzen Lastgang haelt.
        /// </summary>
        /// <param name="lastKw">Lastgang [kW].</param>
        /// <param name="p">Speicherparameter.</param>
        /// <param name="modus">Rechenmodus (wirkt wie bei der Simulation).</param>
        /// <param name="schritte">Bisektionsschritte, Default 60 (Aufloesung weit unter 1 W).</param>
        /// <remarks>
        /// <para>
        /// <b>Warum es diese Methode gibt.</b> Der adaptive Modus des Fachkonzepts 6.4
        /// ist ein <b>einstufiges Greedy-Verfahren</b>: die Schwelle startet bei 0 und
        /// wird nur nachgezogen, wenn der Speicher sie im aktuellen Zustand nicht
        /// halten kann; sie faellt nie wieder. In der Anlaufphase ist die Schwelle
        /// deshalb dicht an der Last, die Ladeschranke <c>P_ziel - P_last</c> nahe 0
        /// und der Speicher kann sich nicht vorladen. Er geht damit fast leer in die
        /// erste grosse Spitze, muss nachziehen - und der einmal erreichte Wert bleibt
        /// stehen. Das Ergebnis ist eine <b>haltbare obere Schranke</b>, nicht
        /// notwendig die minimal erreichbare Spitze.
        /// </para>
        /// <para>
        /// Am Referenzlastgang der Kauffmann-Mappe (200 kW / 300 kWh) liefert der
        /// adaptive Lauf 687,2 kW, waehrend eine feste Vorgabe von 565,76 kW noch
        /// haltbar ist - 121,44 kW Unterschied. Der adaptive Modus bleibt unveraendert
        /// (er traegt den Regressionstest gegen die verifizierte Vorlage); wer die
        /// tatsaechlich minimale Spitze braucht, nimmt diese Methode und rechnet
        /// anschliessend im festen Modus.
        /// </para>
        /// <para>
        /// <b>Verfahren.</b> Bisektion ueber die Haltbarkeit zwischen 0 und
        /// <c>max(P_last)</c>. Die obere Grenze ist immer haltbar (bei
        /// <c>P_ziel = max(P_last)</c> wird nie entladen), und die Invariante der
        /// Schleife haelt fest, dass die zurueckgelieferte Schranke <b>geprueft</b>
        /// haltbar ist - die Monotonie der Haltbarkeit in der Schwelle wird also
        /// vorausgesetzt, aber das Ergebnis nicht darauf gestuetzt.
        /// </para>
        /// </remarks>
        public static double MinimaleSchwelleKw(double[] lastKw, SpeicherParameter p,
                                                SpeicherModus modus = SpeicherModus.Energetisch,
                                                int schritte = 60)
        {
            if (lastKw == null) throw new ArgumentNullException(nameof(lastKw));
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (lastKw.Length == 0)
                throw new ArgumentException("Der Lastgang darf nicht leer sein.", nameof(lastKw));
            if (schritte < 1) throw new ArgumentOutOfRangeException(nameof(schritte));
            p.Pruefe();

            double hi = double.NegativeInfinity;
            for (int i = 0; i < lastKw.Length; i++) if (lastKw[i] > hi) hi = lastKw[i];
            if (hi <= 0.0) return hi;

            double lo = 0.0;
            if (Haelt(lastKw, p, modus, lo)) return lo;

            for (int k = 0; k < schritte; k++)
            {
                double mitte = 0.5 * (lo + hi);
                if (mitte <= lo || mitte >= hi) break;
                if (Haelt(lastKw, p, modus, mitte)) hi = mitte; else lo = mitte;
            }
            return hi;
        }

        /// <summary>
        /// Schlanker Lauf ohne Reihen: haelt der Speicher die feste Schwelle
        /// <paramref name="zielKw"/> ueber den ganzen Lastgang? Rechenkern und
        /// Reihenfolge der Begrenzungen sind identisch zu
        /// <see cref="BerechnePeakShaving(double[], SpeicherParameter)"/>.
        /// </summary>
        private static bool Haelt(double[] lastKw, SpeicherParameter p, SpeicherModus modus, double zielKw)
        {
            bool kompatibel = modus == SpeicherModus.ExcelKompatibilitaet;
            double dt = p.DtH;
            double maxPower = p.PKw;
            double socMin = kompatibel ? 0.0 : p.SoCMinKwh;
            double socMax = p.SoCMaxKwh;
            double etaCh = kompatibel ? 1.0 : p.EtaCh;
            double etaDis = kompatibel ? 1.0 : p.EtaDis;
            double stand = kompatibel ? 0.0 : p.StartSoCEffektivKwh;
            double schranke = zielKw + 1e-9 * Math.Max(1.0, Math.Abs(zielKw));

            for (int i = 0; i < lastKw.Length; i++)
            {
                double last = lastKw[i];
                double dMax = (stand - socMin) * etaDis / dt;
                if (dMax > maxPower) dMax = maxPower;
                if (dMax < 0.0) dMax = 0.0;

                if (last > zielKw)
                {
                    double pd = last - zielKw;
                    if (pd > dMax) pd = dMax;
                    if (pd < 0.0) pd = 0.0;
                    if (last - pd > schranke) return false;
                    stand -= pd * dt / etaDis;
                }
                else
                {
                    double pc = maxPower;
                    double grenze = zielKw - last;
                    if (grenze < pc) pc = grenze;
                    grenze = (socMax - stand) / (etaCh * dt);
                    if (grenze < pc) pc = grenze;
                    if (pc < 0.0) pc = 0.0;
                    stand += pc * dt * etaCh;
                }
            }
            return true;
        }

        /// <summary>
        /// Ordnet die Intervalle Kalendermonaten zu und liefert je Monat die Spitze
        /// vor und nach der Kappung (Fachkonzept 6.4, offener Punkt 4 - Option neben
        /// dem Jahresmaximum).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Annahme:</b> Die Reihe beginnt am 1. Januar und ist lueckenlos im
        /// Raster <paramref name="dtH"/>. Ob ein Schaltjahr vorliegt, wird an der
        /// Reihenlaenge abgelesen (mehr als 365 Tage). Reicht die Reihe nicht ueber
        /// das ganze Jahr, werden nur die belegten Monate geliefert; der letzte darf
        /// unvollstaendig sein und weist das ueber
        /// <see cref="Monatsspitze.Intervalle"/> aus.
        /// </para>
        /// <para>
        /// Reihen, die laenger als ein Jahr sind, laufen in den Dezember; die
        /// Kennzahl bleibt damit definiert, ohne einen Kalender zu erfinden. Passt
        /// das Raster nicht zu einem ganzen Tag, wird eine einzige Sammelposition
        /// (Monat 0) geliefert - die Monatsauswertung ist dann fachlich nicht
        /// belastbar.
        /// </para>
        /// </remarks>
        public static IReadOnlyList<Monatsspitze> Monatsspitzen(double[] pAltKw, double[] pNeuKw, double dtH)
        {
            if (pAltKw == null) throw new ArgumentNullException(nameof(pAltKw));
            if (pNeuKw == null) throw new ArgumentNullException(nameof(pNeuKw));
            if (pNeuKw.Length != pAltKw.Length)
                throw new ArgumentException("Beide Lastgaenge muessen gleich lang sein.", nameof(pNeuKw));

            int n = pAltKw.Length;
            List<Monatsspitze> ergebnis = new List<Monatsspitze>(12);
            if (n == 0) return ergebnis;

            int proTag = dtH > 0.0 ? (int)Math.Round(24.0 / dtH) : 0;
            if (proTag <= 0 || Math.Abs(proTag * dtH - 24.0) > 1e-9)
            {
                // Kein ganzzahliges Tagesraster - eine Sammelposition statt einer
                // erfundenen Monatszuordnung.
                ergebnis.Add(Spitze(0, pAltKw, pNeuKw, 0, n));
                return ergebnis;
            }

            int tage = (n + proTag - 1) / proTag;
            int[] grenzen = tage > 365 ? TageVorMonatSchalt : TageVorMonatGemein;

            int start = 0;
            for (int m = 1; m <= 12 && start < n; m++)
            {
                // Der Dezember nimmt den Rest auf - auch einen Ueberhang ueber das
                // Jahresende hinaus (siehe Bemerkung oben).
                long endeIndexLang = m == 12 ? n : (long)grenzen[m] * proTag;
                int ende = endeIndexLang > n ? n : (int)endeIndexLang;
                if (ende <= start) continue;

                ergebnis.Add(Spitze(m, pAltKw, pNeuKw, start, ende - start));
                start = ende;
            }

            return ergebnis;
        }

        private static Monatsspitze Spitze(int monat, double[] pAlt, double[] pNeu, int start, int laenge)
        {
            double maxAlt = double.NegativeInfinity;
            double maxNeu = double.NegativeInfinity;
            int ende = start + laenge;
            for (int i = start; i < ende; i++)
            {
                if (pAlt[i] > maxAlt) maxAlt = pAlt[i];
                if (pNeu[i] > maxNeu) maxNeu = pNeu[i];
            }

            return new Monatsspitze
            {
                Monat = monat,
                PAltMaxKw = maxAlt,
                PNeuMaxKw = maxNeu,
                Intervalle = laenge
            };
        }
    }
}
