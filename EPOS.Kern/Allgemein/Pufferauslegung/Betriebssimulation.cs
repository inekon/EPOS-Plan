using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // BETRIEBSSIMULATION DER PUFFERSPEICHER-AUSLEGUNG (Konzept 3.3, D1 und D2) - nachgebildet
    // nach dem Waermespeicher-Tool (Quellen/Waermespeicher-Tool/wsp/storage_sim.py, Funktionen
    // simulate, simulate_zweipunkt, find_min_capacity). Energiebilanz je Zeitschritt, kein
    // Temperatur- oder Schichtmodell; Schrittweite 1 h, Reihen in kWh/h = kW.
    //
    // DURCHLAUF (D1): Der Erzeuger deckt zuerst die Last, ein Ueberschuss laedt (bis zur
    //   nutzbaren Kapazitaet), ein Defizit entlaedt (bis 0); der Rest ist Unterdeckung. Start voll.
    // ZWEIPUNKT (D2): Laden bis s_aus, Entladen bis s_ein, Umschalten auf Laden im selben
    //   Schritt, in dem der Fuellstand unter s_ein fiele. Im Modulationsmodus bleibt das Geraet
    //   nach dem Vollladen an, solange die Last nicht unter die Mindestleistung faellt; es
    //   schaltet erst ab, wenn es voll ist UND die Last unter P_min liegt (Runde 1, V4).
    // BISEKTION: kleinstes Volumen, das das Deckungsziel (D1) bzw. das Startziel (D2) haelt.
    //
    // Deterministisch: feste Iterationszahl und Toleranz, keine Zufallsgroesse.
    // ====================================================================================

    /// <summary>Das Ergebnis eines Durchlaufs (D1).</summary>
    public sealed record DurchlaufErgebnis(double KapazitaetKwh, double UnterdeckungKwh, int UnterdeckungH,
                                           double Deckungsgrad, double[] Fuellstand)
    {
        /// <summary>Erreicht der Lauf das Deckungsziel (Rand 1e-9 wie im Tool)?</summary>
        public bool Erreicht(double ziel) => Deckungsgrad >= ziel - 1e-9;
    }

    /// <summary>Das Ergebnis einer Zweipunktsimulation (D2).</summary>
    public sealed record ZweipunktErgebnis(double KapazitaetKwh, double[] Fuellstand, double[] Leistung,
                                           double[] Unterdeckung, bool[] Laden, bool[] Start, int Starts,
                                           double UnterdeckungKwh, int UnterdeckungH, double Deckungsgrad);

    /// <summary>Die Simulationskriterien D1 und D2 samt Bisektion (Konzept 3.3).</summary>
    public static class Betriebssimulation
    {
        /// <summary>Numerischer Rand „keine Unterdeckung“ [kWh] (Tool <c>_EPS</c>).</summary>
        public const double EPS = 1e-9;

        /// <summary>Toleranz der Bisektion [l].</summary>
        public const double TOLERANZ_L = 1.0;

        /// <summary>Höchstzahl der Bisektionsschritte.</summary>
        public const int ITERATIONEN = 60;

        /// <summary>
        /// Die Verfügbarkeit je Stunde (1 frei, 0 gesperrt) aus täglichen Sperrfenstern: Stunde i hat die
        /// Tagesstunde i mod 24 und ist gesperrt, wenn ihr Beginn im Fenster [Beginn, Beginn + Dauer)
        /// liegt; ein Fenster über Mitternacht läuft in den nächsten Tag (Mitternachtsübertrag).
        /// </summary>
        public static double[] Verfuegbarkeit(int n, IReadOnlyList<PufferSperrfenster> fenster)
        {
            var m = new double[n];
            for (int i = 0; i < n; i++) m[i] = 1.0;
            if (fenster == null) return m;
            foreach (PufferSperrfenster f in fenster)
            {
                if (f == null || !(f.DauerH > 0)) continue;
                double b = f.BeginnH, e = f.BeginnH + f.DauerH;
                for (int i = 0; i < n; i++)
                {
                    double h = i % 24;
                    bool gesperrt = f.DauerH >= 24 ||
                                    (e <= 24.0 ? (h >= b && h < e) : (h >= b || h < e - 24.0));
                    if (gesperrt) m[i] = 0.0;
                }
            }
            return m;
        }

        /// <summary>
        /// Die verfügbare Erzeugerleistung je Stunde: Rang 1 nach der Sperrmaske, der Zweiterzeuger nach
        /// der Maske, wenn er mitgesperrt ist, sonst immer.
        /// </summary>
        public static double[] Erzeugerleistung(int n, double rang1Kw, double rang2Kw, bool rang2Frei, double[] maske)
        {
            var p = new double[n];
            for (int i = 0; i < n; i++)
            {
                double a = maske == null ? 1.0 : maske[i];
                p[i] = Math.Max(rang1Kw, 0) * a + Math.Max(rang2Kw, 0) * (rang2Frei ? 1.0 : a);
            }
            return p;
        }

        /// <summary>
        /// Größte rollierende Mittelleistung [kW] über <paramref name="dauerH"/> Stunden — umlaufend über das
        /// Reihenende (Jahreswechsel); ist das Fenster länger als die Reihe, das Mittel der Reihe.
        /// </summary>
        public static double MaxMittelleistung(IReadOnlyList<double> last, double dauerH)
        {
            if (last == null || last.Count == 0 || !(dauerH > 0)) return 0.0;
            int n = last.Count;
            int f = Math.Max((int)Math.Round(dauerH, MidpointRounding.AwayFromZero), 1);
            if (f >= n)
            {
                double s0 = 0;
                for (int i = 0; i < n; i++) s0 += Wert(last, i);
                return s0 / n;
            }
            double summe = 0;
            for (int i = 0; i < f; i++) summe += Wert(last, i);
            double max = summe;
            for (int i = 1; i < n; i++)
            {
                summe += Wert(last, (i + f - 1) % n) - Wert(last, i - 1);
                if (summe > max) max = summe;
            }
            return max / f;
        }

        private static double Wert(IReadOnlyList<double> r, int i)
        {
            double w = r[i];
            return double.IsNaN(w) || double.IsInfinity(w) ? 0.0 : w;
        }

        /// <summary>
        /// Durchlauf (Tool <c>simulate</c>): Kapazität <paramref name="kapazitaetKwh"/> (nutzbar), Start voll
        /// bzw. <paramref name="soc0"/>.
        /// </summary>
        public static DurchlaufErgebnis Durchlauf(IReadOnlyList<double> last, IReadOnlyList<double> pVerfuegbar,
                                                  double kapazitaetKwh, double? soc0 = null)
        {
            int n = last.Count;
            double c = Math.Max(kapazitaetKwh, 0.0);
            double s = soc0.HasValue ? Math.Min(Math.Max(soc0.Value, 0.0), c) : c;
            var soc = new double[n];
            double bedarf = 0, unter = 0;
            int stunden = 0;
            for (int i = 0; i < n; i++)
            {
                double q = Wert(last, i);
                bedarf += q;
                double roh = s + (pVerfuegbar[i] - q);
                double d = 0;
                if (roh > c) s = c;
                else if (roh < 0.0) { d = -roh; s = 0.0; }
                else s = roh;
                if (d < EPS) d = 0;
                if (d > 0) { unter += d; stunden++; }
                soc[i] = s;
            }
            double grad = bedarf > 0 ? 1.0 - unter / bedarf : 1.0;
            return new DurchlaufErgebnis(c, unter, stunden, grad, soc);
        }

        /// <summary>
        /// Zweipunkt (Tool <c>simulate_zweipunkt</c>, erweitert um die Schwellen des Puffers und den
        /// Modulationsmodus): Gesamtkapazität <paramref name="kapazitaetKwh"/>, Laden bis
        /// s_aus · C, Entladen bis s_ein · C, Start im Ladebetrieb mit Füllstand <paramref name="soc0"/>
        /// [kWh]. Starts = Wechsel Entladen → Laden.
        /// </summary>
        public static ZweipunktErgebnis Zweipunkt(IReadOnlyList<double> last, IReadOnlyList<double> pMax,
                                                  double kapazitaetKwh, double sEin, double sAus,
                                                  double pMin = 0, bool modulierend = false, double soc0 = 0)
        {
            int n = last.Count;
            double c = Math.Max(kapazitaetKwh, 0.0);
            double unten = Math.Min(Math.Max(sEin, 0.0), 1.0) * c;
            double oben = Math.Min(Math.Max(sAus, 0.0), 1.0) * c;
            if (oben < unten) oben = unten;

            var socA = new double[n];
            var pA = new double[n];
            var uA = new double[n];
            var lA = new bool[n];
            var sA = new bool[n];
            double soc = Math.Min(Math.Max(soc0, 0.0), c);
            bool laden = true;
            int starts = 0;
            double bedarf = 0, unter = 0;
            int stunden = 0;

            for (int i = 0; i < n; i++)
            {
                double q = Wert(last, i);
                bedarf += q;
                double u = 0, p = 0;
                bool geladen = false;

                // Modulationsmodus: voll und die Last liegt unter der Mindestleistung -> abschalten.
                if (laden && modulierend && soc >= oben - 1e-9 && q < pMin) laden = false;

                if (!laden)
                {
                    double naechst = soc - q;
                    if (naechst < unten)
                    {
                        laden = true;             // im SELBEN Schritt umschalten
                        starts++;
                        sA[i] = true;
                    }
                    else soc = naechst;
                }

                if (laden)
                {
                    geladen = true;
                    p = Math.Min(q + (oben - soc), pMax[i]);
                    if (p < 0) p = 0;
                    double naechst = soc + (p - q);
                    if (naechst < unten)          // Füllstand auf s_ein halten
                    {
                        u = unten - naechst;
                        naechst = unten;
                    }
                    soc = Math.Min(naechst, oben);
                    if (soc >= oben - 1e-9 && !modulierend) laden = false;   // voll -> ab dem nächsten Schritt entladen
                }

                if (u < EPS) u = 0;
                if (u > 0) { unter += u; stunden++; }
                socA[i] = soc;
                pA[i] = p;
                uA[i] = u;
                lA[i] = geladen;
            }
            double grad = bedarf > 0 ? 1.0 - unter / bedarf : 1.0;
            return new ZweipunktErgebnis(c, socA, pA, uA, lA, sA, starts, unter, stunden, grad);
        }

        /// <summary>
        /// Das Betriebsbild einer Zweipunktsimulation: Heizperiode = Stunden mit Bedarf &gt; 0; Starts je
        /// Tag = Starts der Heizperiode ÷ (Heizstunden ÷ 24).
        /// </summary>
        public static PufferBetriebsbild Bild(ZweipunktErgebnis sim, IReadOnlyList<double> last, double volumenL)
        {
            int n = last.Count;
            int heiz = 0, startsHeiz = 0, lauf = 0;
            for (int i = 0; i < n; i++)
            {
                bool bedarf = Wert(last, i) > 0;
                if (bedarf) heiz++;
                if (sim.Start[i] && bedarf) startsHeiz++;
                if (sim.Laden[i]) lauf++;
            }
            double tage = heiz / 24.0;
            return new PufferBetriebsbild
            {
                VolumenL = volumenL,
                Starts = sim.Starts,
                StartsHeizperiode = startsHeiz,
                Heizstunden = heiz,
                StartsJeTag = tage > 0 ? startsHeiz / tage : 0.0,
                Laufstunden = lauf,
                MittlereLaufzeitH = sim.Starts > 0 ? (double)lauf / sim.Starts : lauf,
                Deckungsgrad = sim.Deckungsgrad
            };
        }

        /// <summary>
        /// D1: kleinstes Volumen [l] mit Deckungsgrad ≥ Ziel (Tool <c>find_min_capacity</c>), Bisektion
        /// zwischen 0 und <paramref name="vMaxL"/>. <paramref name="kwhJeLiter"/> = c · Δϑ · η_s / 1000.
        /// Reicht selbst <paramref name="vMaxL"/> nicht, ist das Ergebnis ungültig (Wert = Grenze).
        /// </summary>
        public static (double VolumenL, bool Gueltig, DurchlaufErgebnis Lauf) MindestvolumenDeckung(
            IReadOnlyList<double> last, IReadOnlyList<double> pVerfuegbar, double ziel, double kwhJeLiter, double vMaxL)
        {
            DurchlaufErgebnis lo0 = Durchlauf(last, pVerfuegbar, 0.0);
            if (lo0.Erreicht(ziel)) return (0.0, true, lo0);
            DurchlaufErgebnis hi0 = Durchlauf(last, pVerfuegbar, vMaxL * kwhJeLiter);
            if (!hi0.Erreicht(ziel)) return (vMaxL, false, hi0);
            double lo = 0, hi = vMaxL;
            DurchlaufErgebnis best = hi0;
            for (int k = 0; k < ITERATIONEN && hi - lo > TOLERANZ_L; k++)
            {
                double mitte = 0.5 * (lo + hi);
                DurchlaufErgebnis r = Durchlauf(last, pVerfuegbar, mitte * kwhJeLiter);
                if (r.Erreicht(ziel)) { hi = mitte; best = r; }
                else lo = mitte;
            }
            return (hi, true, best);
        }

        /// <summary>
        /// D2: kleinstes Volumen [l], mit dem die Zweipunktsimulation höchstens <paramref name="startziel"/>
        /// Starts je Tag der Heizperiode macht, Bisektion zwischen 0 und <paramref name="vMaxL"/>.
        /// <paramref name="kwhJeLiter"/> = c · Δϑ / 1000 (Gesamtkapazität; die Schwellen bilden η_s ab).
        /// </summary>
        public static (double VolumenL, bool Gueltig, PufferBetriebsbild Bild) MindestvolumenStarts(
            IReadOnlyList<double> last, IReadOnlyList<double> pMax, double sEin, double sAus, double pMin,
            bool modulierend, double kwhJeLiter, double startziel, double vMaxL)
        {
            PufferBetriebsbild Lauf(double v) =>
                Bild(Zweipunkt(last, pMax, v * kwhJeLiter, sEin, sAus, pMin, modulierend, sEin * v * kwhJeLiter), last, v);

            PufferBetriebsbild b0 = Lauf(0.0);
            if (b0.StartsJeTag <= startziel + 1e-9) return (0.0, true, b0);
            PufferBetriebsbild bMax = Lauf(vMaxL);
            if (bMax.StartsJeTag > startziel + 1e-9) return (vMaxL, false, bMax);
            double lo = 0, hi = vMaxL;
            PufferBetriebsbild best = bMax;
            for (int k = 0; k < ITERATIONEN && hi - lo > TOLERANZ_L; k++)
            {
                double mitte = 0.5 * (lo + hi);
                PufferBetriebsbild b = Lauf(mitte);
                if (b.StartsJeTag <= startziel + 1e-9) { hi = mitte; best = b; }
                else lo = mitte;
            }
            return (hi, true, best);
        }
    }
}
