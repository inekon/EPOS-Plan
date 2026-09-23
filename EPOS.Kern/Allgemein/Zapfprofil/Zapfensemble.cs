using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.ExceptionServices;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Zone im Auslegungsensemble (4.4, 4.5 b): <see cref="Index"/> ist ihre Stelle im Eingang
    /// (Seed-Ableitung), <see cref="Einheiten"/> die unabhängigen Einheiten n_E, dazu die Kategorien,
    /// die Tagesmenge der Zone am maßgebenden Tag bei θ_KW,Auslegung [kWh], die Spreizung
    /// θ_Zapf − θ_KW,Auslegung [K] und die Tageszeitdichte des Tagtyps dieses Tages.
    /// </summary>
    internal sealed record Ensemblezone(int Index, string Zone, int Einheiten, Zapfkategoriensatz Kategorien,
                                        double TagesmengeKwh, double SpreizungK, Tageszeitdichte Dichte);

    /// <summary>
    /// Die Perzentile einer Stichprobe über die Realisierungen (4.4: P50/P90/P95/P99 mit Streuband):
    /// nächstgelegener Rang <c>k = ⌈p · n / 100⌉</c> der aufsteigend geordneten Werte, das Streuband
    /// ist die Spannweite Minimum … Maximum. Ein unendlicher Wert (Realisierung ohne Nachweis)
    /// ordnet sich oben ein.
    /// </summary>
    internal sealed record Perzentilwerte(int Anzahl, double P50, double P90, double P95, double P99, double Minimum,
                                          double Maximum)
    {
        /// <summary>Das Perzentil 50, 90, 95 oder 99.</summary>
        internal double Wert(int perzentil)
        {
            switch (perzentil)
            {
                case 50: return P50;
                case 90: return P90;
                case 95: return P95;
                case 99: return P99;
            }
            throw new ArgumentOutOfRangeException(nameof(perzentil), "Perzentile gibt es zu 50, 90, 95 und 99.");
        }

        /// <summary>
        /// Die Perzentile einer Stichprobe (Werte je Realisierung, keine Zeitreihe); eine leere
        /// Stichprobe oder ein NaN wird abgelehnt.
        /// </summary>
        internal static Perzentilwerte Aus(IReadOnlyList<double> stichprobe)
        {
            if (stichprobe == null || stichprobe.Count == 0)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — ein Perzentil braucht mindestens eine Realisierung.");
            var w = new double[stichprobe.Count];
            for (int i = 0; i < w.Length; i++)
            {
                if (double.IsNaN(stichprobe[i]))
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                        "Nicht rechenbar — eine Realisierung trägt keinen Zahlenwert.");
                w[i] = stichprobe[i];
            }
            Array.Sort(w);
            return new Perzentilwerte(w.Length, Rang(w, 50), Rang(w, 90), Rang(w, 95), Rang(w, 99), w[0], w[w.Length - 1]);
        }

        private static double Rang(double[] geordnet, int p)
        {
            int n = geordnet.Length;
            int k = (p * n + 99) / 100;
            return geordnet[(k < 1 ? 1 : k) - 1];
        }
    }

    /// <summary>
    /// Die Statistik einer Zone im Auslegungsensemble: Einheiten, Tagesmenge, die Minutenspitze
    /// JE EINHEIT über alle Einheiten und Realisierungen (Wohnungsstation je Einheit, N10 (d);
    /// Nenner von GLF_P), der gezogene Tag der ersten Einheit je Realisierung (Nenner von GLF_V)
    /// und — privat — Mittel und mittleres Quadrat der Minutenlast einer Einheit (Einzelstatistik
    /// für <c>μ + z · σ / √N</c>).
    /// </summary>
    internal sealed class Ensemblezonenstatistik
    {
        private readonly double[] _mittelKwh;
        private readonly double[] _quadratKwh2;

        internal Ensemblezonenstatistik(string zone, int einheiten, double tagesmengeKwh, Perzentilwerte spitzeJeEinheitKw,
                                        IReadOnlyList<Bedarfstag> vertreter, double[] mittelKwh, double[] quadratKwh2)
        {
            Zone = zone ?? "";
            Einheiten = einheiten;
            TagesmengeKwh = tagesmengeKwh;
            SpitzeJeEinheitKw = spitzeJeEinheitKw;
            Vertreter = vertreter;
            _mittelKwh = mittelKwh;
            _quadratKwh2 = quadratKwh2;
        }

        internal string Zone { get; }

        /// <summary>Die Einheiten n_E der Zone.</summary>
        internal int Einheiten { get; }

        /// <summary>Die Tagesmenge der Zone am maßgebenden Tag [kWh] — der Erwartungswert ihrer Ziehung.</summary>
        internal double TagesmengeKwh { get; }

        /// <summary>Die größte Minutenleistung einer Einheit [kW], über n_E · R Stichproben.</summary>
        internal Perzentilwerte SpitzeJeEinheitKw { get; }

        /// <summary>Der gezogene Tag der ersten Einheit je Realisierung.</summary>
        internal IReadOnlyList<Bedarfstag> Vertreter { get; }

        /// <summary>Mittel der Minutenlast einer Einheit in Minute <paramref name="minute"/> [kWh je Minute].</summary>
        internal double MittelKwh(int minute) => _mittelKwh[minute];

        /// <summary>Varianz der Minutenlast einer Einheit in Minute <paramref name="minute"/> [kWh² je Minute²], ≥ 0.</summary>
        internal double Varianz(int minute)
        {
            double v = _quadratKwh2[minute] - _mittelKwh[minute] * _mittelKwh[minute];
            return v > 0 ? v : 0.0;
        }
    }

    /// <summary>
    /// <b>Das Auslegungsensemble einer Topologiegruppe</b> (4.4 „Zwei Ensembles", 4.5 b): R gezogene
    /// Bedarfstage der Gruppe — Superposition der n_E unabhängigen Einheiten jeder Zone —, dazu je
    /// Zone die Einzelstatistik. Die Gleichzeitigkeit entsteht aus der Superposition; es gibt keinen
    /// Eingabefaktor.
    /// </summary>
    internal sealed class Bedarfstagensemble
    {
        internal Bedarfstagensemble(long seed, int perzentil, IReadOnlyList<Bedarfstag> tage,
                                    IReadOnlyList<Ensemblezonenstatistik> zonen)
        {
            Seed = seed;
            Perzentil = perzentil;
            Tage = tage;
            Zonen = zonen;
            var spitze = new double[tage.Count];
            var stunde = new double[tage.Count];
            for (int r = 0; r < tage.Count; r++)
            {
                spitze[r] = tage[r].GroessteMinutenleistungKw;
                stunde[r] = tage[r].GroessteStundenleistungKw;
            }
            MinutenspitzeKw = Perzentilwerte.Aus(spitze);
            StundenspitzeKw = Perzentilwerte.Aus(stunde);
        }

        internal long Seed { get; }

        /// <summary>Das Auslegungsperzentil p (95 oder 99, K3).</summary>
        internal int Perzentil { get; }

        /// <summary>Die Zahl der Realisierungen R.</summary>
        internal int Realisierungen => Tage.Count;

        /// <summary>Die gezogenen Tage der Gruppe, je Realisierung einer (Summe aller Einheiten).</summary>
        internal IReadOnlyList<Bedarfstag> Tage { get; }

        /// <summary>Die Zonen der Gruppe mit ihrer Einzelstatistik.</summary>
        internal IReadOnlyList<Ensemblezonenstatistik> Zonen { get; }

        /// <summary>Die größte Minutenleistung der Gruppe [kW] über die Realisierungen.</summary>
        internal Perzentilwerte MinutenspitzeKw { get; }

        /// <summary>Die größte Stundenleistung der Gruppe [kW] über die Realisierungen (nachrichtlich).</summary>
        internal Perzentilwerte StundenspitzeKw { get; }

        /// <summary>Genügen die Realisierungen für ein empirisches p-Perzentil (R ≥ 1/(1 − p), 4.4)?</summary>
        internal bool Belastbar => Realisierungen >= Zapfensemble.Mindestzahl(Perzentil);

        /// <summary>
        /// <b>Die Gleichzeitigkeit der Leistung</b> (4.4): <c>GLF_P = P_p(P_Summe) / Σ_i P_p(P_i)</c> mit
        /// der Minutenspitze der Gruppe und der Minutenspitze je Einheit; <c>null</c> ohne Last.
        /// </summary>
        internal double? GleichzeitigkeitLeistung
        {
            get
            {
                double nenner = 0.0;
                foreach (Ensemblezonenstatistik z in Zonen) nenner += z.Einheiten * z.SpitzeJeEinheitKw.Wert(Perzentil);
                return nenner > 0 ? MinutenspitzeKw.Wert(Perzentil) / nenner : (double?)null;
            }
        }

        /// <summary>
        /// <b>Der Vergleich μ + z · σ / √N</b> aus der Einzelstatistik der Einheiten (4.5 b, Konzept
        /// S4c) als Leistung der Gruppe [kW]: <c>max_t (Σ_Zonen N · μ(t) + z · √(Σ_Zonen N · σ²(t))) · 60</c>
        /// — die Näherung der Summe unabhängiger Einheiten durch ihr Mittel und z Standardabweichungen.
        /// </summary>
        internal double WurzelNSchaetzungKw(double quantil)
        {
            double groesste = 0.0;
            for (int t = 0; t < Bedarfstag.MINUTEN; t++)
            {
                double mittel = 0.0, varianz = 0.0;
                foreach (Ensemblezonenstatistik z in Zonen)
                {
                    mittel += z.Einheiten * z.MittelKwh(t);
                    varianz += z.Einheiten * z.Varianz(t);
                }
                double w = mittel + quantil * Math.Sqrt(varianz);
                if (w > groesste) groesste = w;
            }
            return groesste * Bedarfstag.MINUTEN_JE_STUNDE;
        }
    }

    /// <summary>
    /// Die Volumina des Auslegungsensembles einer Speichergruppe (4.5 b): das erforderliche Volumen
    /// V_r jeder Realisierung aus der Summenlinie beim gewählten Φ_N [l] als Perzentile, die Zahl
    /// der Realisierungen ohne Nachweis (V = ∞) und die Gleichzeitigkeit des Volumens.
    /// </summary>
    internal sealed record Speicherensemble(Perzentilwerte VolumenL, int OhneNachweis, double LeistungKw,
                                            double? GleichzeitigkeitVolumen);

    /// <summary>
    /// <b>Das Ensemble des Bedarfstags</b> (Umsetzungskonzept Zapfprofilgenerator 4.4, 4.5 b; Schicht
    /// S3). Es zieht R Tage der Gruppe — je Zone und Einheit ein eigener Zufallsstrom
    /// (<see cref="ZapfZufall.Kindseed"/> aus Realisierung, Zone und Einheit), damit das Ergebnis
    /// nicht von der Reihenfolge der Fäden abhängt — und wertet sie je Topologie aus: Minutenspitze
    /// (Durchfluss, Frischwasser- und Wohnungsstation), erforderliches Volumen der Summenlinie
    /// (Speicher), Gleichzeitigkeit von Leistung und Volumen.
    ///
    /// <para><b>Nie aus der Bilanz.</b> Das Ensemble liest Tagesmengen, Kategorien und
    /// Tageszeitdichten — keine Jahresreihe (Invariante 2.4, Wache
    /// <c>ZapfprofilTrennungWacheTests</c>). Minutenwerte stehen nur in <see cref="Bedarfstag"/>.</para>
    ///
    /// <para><b>Parallel mit fester Summationsfolge.</b> Die Realisierungen laufen blockweise über
    /// <c>Kulturweitergabe.For</c>; summiert wird danach in der Folge r = 0, 1, … — parallel und
    /// seriell liefern dieselben Bits.</para>
    /// </summary>
    internal static class Zapfensemble
    {
        /// <summary>Realisierungen je Block der parallelen Rechnung (numerische Setzung, begrenzt den Speicher).</summary>
        internal const int BLOCK = 64;

        /// <summary>Höchstzahl der Realisierungen (numerische Setzung gegen eine unsinnige Laufzeit).</summary>
        internal const int HOECHSTENS = 100000;

        /// <summary>
        /// Die Mindestzahl der Realisierungen für ein empirisches p-Perzentil: <c>⌈1 / (1 − p)⌉</c>
        /// = ⌈100 / (100 − p)⌉ — ganzzahlig gerechnet (P99 → 100, P95 → 20).
        /// </summary>
        internal static int Mindestzahl(int perzentil)
        {
            PerzentilPruefen(perzentil);
            int rest = 100 - perzentil;
            return (100 + rest - 1) / rest;
        }

        /// <summary>
        /// Die Vorgabe der Realisierungen des Bedarfstags (4.4): Projektwert, sonst
        /// <c>⌈Vielfaches · Mindestzahl⌉</c> mit dem Vielfachen aus dem Parametersatz
        /// (<see cref="ZapfStochastikParameter.AUSLEGUNG_VIELFACHES"/>). Fehlt beides, benannte Ablehnung.
        /// </summary>
        internal static int RealisierungenAuslegung(int? projekt, int perzentil, Parametersatz ps)
        {
            if (projekt.HasValue) return RealisierungenPruefen(projekt.Value);
            double vielfaches = ps.Wert(ZapfStochastikParameter.AUSLEGUNG_VIELFACHES);
            if (double.IsNaN(vielfaches) || double.IsInfinity(vielfaches) || !(vielfaches > 0))
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — das Vielfache der Realisierungen des Bedarfstags ist nicht positiv.");
            double r = Math.Ceiling(vielfaches * Mindestzahl(perzentil));
            if (r > HOECHSTENS)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — die Vorgabe der Realisierungen liegt über " + HOECHSTENS.ToString(CultureInfo.InvariantCulture) + ".");
            return (int)r;
        }

        /// <summary>
        /// <b>Zieht das Ensemble des Bedarfstags</b>: R Realisierungen, je Realisierung je Zone
        /// (Reihenfolge des Eingangs) je Einheit ein Tag nach <see cref="Zapfereignisgenerator"/> mit
        /// der Tagesmenge der Zone geteilt durch n_E. Ein Ereignis über Mitternacht läuft am
        /// Tagesanfang weiter (der Bedarfstag wiederholt sich).
        /// </summary>
        internal static Bedarfstagensemble Ziehen(IReadOnlyList<Ensemblezone> zonen, long seed, int realisierungen,
                                                  int perzentil, bool parallel = true)
        {
            PerzentilPruefen(perzentil);
            RealisierungenPruefen(realisierungen);
            if (zonen == null || zonen.Count == 0)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — das Ensemble hat keine Zone.");
            foreach (Ensemblezone z in zonen)
            {
                if (z == null || z.Kategorien == null || z.Dichte == null || z.Einheiten < 1 || z.Index < 0)
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                        "Nicht rechenbar — eine Zone des Ensembles ist unvollständig (Kategorien, Dichte, Einheiten ≥ 1).");
                Auslegungspruefung.NichtNegativ(z.TagesmengeKwh, "die Tagesmenge der Zone „" + z.Zone + "“");
                if (z.TagesmengeKwh > 0) Auslegungspruefung.Positiv(z.SpreizungK, "die Spreizung der Zone „" + z.Zone + "“");
            }

            int zahl = zonen.Count;
            var tage = new Bedarfstag[realisierungen];
            var spitzen = new double[zahl][];
            var vertreter = new Bedarfstag[zahl][];
            var summe = new double[zahl][];
            var quadrat = new double[zahl][];
            for (int zi = 0; zi < zahl; zi++)
            {
                spitzen[zi] = new double[realisierungen * zonen[zi].Einheiten];
                vertreter[zi] = new Bedarfstag[realisierungen];
                summe[zi] = new double[Bedarfstag.MINUTEN];
                quadrat[zi] = new double[Bedarfstag.MINUTEN];
            }

            for (int start = 0; start < realisierungen; start += BLOCK)
            {
                int anzahl = Math.Min(BLOCK, realisierungen - start);
                var teil = new Realisierungsteil[anzahl];
                Lauf(anzahl, parallel, i => teil[i] = Realisierung(zonen, seed, start + i));
                // Feste Summationsfolge: Realisierung für Realisierung, Minute für Minute.
                for (int i = 0; i < anzahl; i++)
                {
                    int r = start + i;
                    tage[r] = teil[i].Tag;
                    for (int zi = 0; zi < zahl; zi++)
                    {
                        Array.Copy(teil[i].Spitzen[zi], 0, spitzen[zi], r * zonen[zi].Einheiten, zonen[zi].Einheiten);
                        vertreter[zi][r] = teil[i].Vertreter[zi];
                        for (int t = 0; t < Bedarfstag.MINUTEN; t++)
                        {
                            summe[zi][t] += teil[i].Summe[zi][t];
                            quadrat[zi][t] += teil[i].Quadrat[zi][t];
                        }
                    }
                }
            }

            var statistik = new Ensemblezonenstatistik[zahl];
            for (int zi = 0; zi < zahl; zi++)
            {
                double stichproben = (double)realisierungen * zonen[zi].Einheiten;
                var mittel = new double[Bedarfstag.MINUTEN];
                var q = new double[Bedarfstag.MINUTEN];
                for (int t = 0; t < Bedarfstag.MINUTEN; t++)
                {
                    mittel[t] = summe[zi][t] / stichproben;
                    q[t] = quadrat[zi][t] / stichproben;
                }
                statistik[zi] = new Ensemblezonenstatistik(zonen[zi].Zone, zonen[zi].Einheiten, zonen[zi].TagesmengeKwh,
                    Perzentilwerte.Aus(spitzen[zi]), Array.AsReadOnly(vertreter[zi]), mittel, q);
            }
            return new Bedarfstagensemble(seed, perzentil, Array.AsReadOnly(tage), Array.AsReadOnly(statistik));
        }

        /// <summary>
        /// <b>Die Volumina einer Speichergruppe</b> (4.5 b): je Realisierung das kleinste Volumen
        /// mit Nachweis beim festen Φ_N = <paramref name="leistungKw"/> (Übertrager und Erzeuger fallen
        /// auf diese Leistung) mit den übrigen Größen der Summenlinie der Gruppe; eine Realisierung,
        /// die kein Volumen findet, zählt als ∞. Dazu <c>GLF_V = P_p(V_Summe) / Σ_i P_p(V_i)</c>: V_i
        /// ist das Volumen der ersten Einheit jeder Zone mit dem Anteil ihrer Tagesmenge an Φ_N,
        /// Speicherverlust und Zirkulation (Festlegung), Σ_i = Σ_Zonen n_E · P_p(V_Einheit).
        /// </summary>
        internal static Speicherensemble Volumina(Bedarfstagensemble e, Summenlinienparameter p, double leistungKw,
                                                  bool parallel = true)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (p == null) throw new ArgumentNullException(nameof(p));
            Auslegungspruefung.Positiv(leistungKw, "die Leistung Φ_N des Summenlinienpunkts");
            Summenlinienparameter fest = p with { ErzeugerKw = leistungKw, Uebertrager = null };

            var volumen = new double[e.Realisierungen];
            Lauf(e.Realisierungen, parallel, r => volumen[r] = Volumen(e.Tage[r], fest));
            int ohne = 0;
            foreach (double v in volumen) if (double.IsPositiveInfinity(v)) ohne++;
            Perzentilwerte werte = Perzentilwerte.Aus(volumen);

            double tagSumme = 0.0;
            foreach (Ensemblezonenstatistik z in e.Zonen) tagSumme += z.TagesmengeKwh;
            double? glf = null;
            double oben = werte.Wert(e.Perzentil);
            if (tagSumme > 0 && !double.IsPositiveInfinity(oben))
            {
                double nenner = 0.0;
                bool endlich = true;
                foreach (Ensemblezonenstatistik z in e.Zonen)
                {
                    double anteil = z.TagesmengeKwh / z.Einheiten / tagSumme;
                    if (!(anteil > 0)) continue;
                    Summenlinienparameter eigen = fest with
                    {
                        ErzeugerKw = leistungKw * anteil,
                        SpeicherverlustKw = p.SpeicherverlustKw * anteil,
                        Zirkulation = new Zirkulationslast(p.Zirkulation.LeistungKw * anteil, p.Zirkulation.Laufzeit)
                    };
                    var einzel = new double[e.Realisierungen];
                    Lauf(e.Realisierungen, parallel, r => einzel[r] = Volumen(z.Vertreter[r], eigen));
                    double pz = Perzentilwerte.Aus(einzel).Wert(e.Perzentil);
                    if (double.IsPositiveInfinity(pz)) endlich = false;
                    nenner += z.Einheiten * pz;
                }
                if (endlich && nenner > 0) glf = oben / nenner;
            }
            return new Speicherensemble(werte, ohne, leistungKw, glf);
        }

        // =================================================================================
        // Eine Realisierung
        // =================================================================================

        /// <summary>Was eine Realisierung beiträgt — je Zone Spitzen je Einheit, Vertreter, Summen je Minute.</summary>
        private sealed class Realisierungsteil
        {
            internal Bedarfstag Tag;
            internal double[][] Spitzen;
            internal Bedarfstag[] Vertreter;
            internal double[][] Summe;
            internal double[][] Quadrat;
        }

        private static Realisierungsteil Realisierung(IReadOnlyList<Ensemblezone> zonen, long seed, int r)
        {
            int zahl = zonen.Count;
            var teil = new Realisierungsteil
            {
                Spitzen = new double[zahl][], Vertreter = new Bedarfstag[zahl],
                Summe = new double[zahl][], Quadrat = new double[zahl][]
            };
            var gruppe = new List<Zapfereignis>();
            var einheit = new List<Zapfereignis>();
            var minuten = new double[Bedarfstag.MINUTEN];
            ulong rs = ZapfZufall.Realisierungsseed(seed, r);
            string name = "Realisierung " + (r + 1).ToString(CultureInfo.InvariantCulture);
            for (int zi = 0; zi < zahl; zi++)
            {
                Ensemblezone z = zonen[zi];
                ulong zs = ZapfZufall.Kindseed(rs, z.Index);
                double jeEinheitKwh = z.TagesmengeKwh / z.Einheiten;
                var spitzen = new double[z.Einheiten];
                var summe = new double[Bedarfstag.MINUTEN];
                var quadrat = new double[Bedarfstag.MINUTEN];
                for (int u = 0; u < z.Einheiten; u++)
                {
                    var zufall = new ZapfZufall(ZapfZufall.Kindseed(zs, u));
                    einheit.Clear();
                    Zapfereignisgenerator.Ziehen(zufall, z.Kategorien, jeEinheitKwh, z.SpreizungK, z.Dichte, einheit);
                    Array.Clear(minuten, 0, minuten.Length);
                    foreach (Zapfereignis ev in einheit)
                    {
                        double jeMinute = ev.EnergieKwh / ev.DauerMin;
                        for (int k = 0; k < ev.DauerMin; k++) minuten[(ev.MinuteBeginn + k) % Bedarfstag.MINUTEN] += jeMinute;
                    }
                    double groesste = 0.0;
                    for (int t = 0; t < Bedarfstag.MINUTEN; t++)
                    {
                        double x = minuten[t];
                        if (x > groesste) groesste = x;
                        summe[t] += x;
                        quadrat[t] += x * x;
                    }
                    spitzen[u] = groesste * Bedarfstag.MINUTEN_JE_STUNDE;
                    if (u == 0) teil.Vertreter[zi] = Bedarfstag.AusZiehung(name + ", " + z.Zone + ", Einheit 1", einheit);
                    gruppe.AddRange(einheit);
                }
                teil.Spitzen[zi] = spitzen;
                teil.Summe[zi] = summe;
                teil.Quadrat[zi] = quadrat;
            }
            teil.Tag = Bedarfstag.AusZiehung(name, gruppe);
            return teil;
        }

        /// <summary>Das kleinste Volumen mit Nachweis [l]; ohne Nachweis +∞.</summary>
        private static double Volumen(Bedarfstag tag, Summenlinienparameter p)
        {
            try
            {
                return Summenlinie.KleinstesVolumen(tag, p, new List<Auslegungshinweis>()).VolumenL;
            }
            catch (ZapfAuslegungException ex) when (ex.Fehler == ZapfAuslegungsfehler.KeinVolumen)
            {
                return double.PositiveInfinity;
            }
        }

        /// <summary>
        /// Führt <paramref name="arbeit"/> für 0 … <paramref name="anzahl"/> − 1 aus — parallel über
        /// <c>Kulturweitergabe.For</c> oder seriell. Die Ausnahme eines Arbeitspakets kommt
        /// <b>ausgepackt</b> heraus, nicht als <see cref="AggregateException"/>, damit die Fassaden sie
        /// benannt fangen; werfen mehrere Pakete, gilt die erste benannte
        /// (<see cref="ZapfprofilEingabeException"/>, <see cref="ZapfAuslegungException"/>), sonst die erste.
        /// </summary>
        internal static void Lauf(int anzahl, bool parallel, Action<int> arbeit)
        {
            if (!parallel || anzahl <= 1)
            {
                for (int i = 0; i < anzahl; i++) arbeit(i);
                return;
            }
            try
            {
                SpeicherEngine.Kulturweitergabe.For(0, anzahl, null, arbeit);
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(Innere(ex)).Throw();
                throw;
            }
        }

        /// <summary>Die maßgebende innere Ausnahme eines parallelen Laufs (siehe <see cref="Lauf"/>).</summary>
        private static Exception Innere(AggregateException ex)
        {
            IReadOnlyList<Exception> alle = ex.Flatten().InnerExceptions;
            foreach (Exception e in alle)
                if (e is ZapfprofilEingabeException || e is ZapfAuslegungException) return e;
            return alle.Count > 0 ? alle[0] : ex;
        }

        private static void PerzentilPruefen(int perzentil)
        {
            if (perzentil != 95 && perzentil != 99)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — das Auslegungsperzentil ist 95 oder 99 (K3), nicht "
                    + perzentil.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private static int RealisierungenPruefen(int realisierungen)
        {
            if (realisierungen < 1 || realisierungen > HOECHSTENS)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — die Zahl der Realisierungen liegt nicht in 1 … "
                    + HOECHSTENS.ToString(CultureInfo.InvariantCulture) + ".");
            return realisierungen;
        }
    }
}
