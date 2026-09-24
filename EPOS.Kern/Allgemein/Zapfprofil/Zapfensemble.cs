using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

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
        /// <summary>Die Perzentile, zu denen es Werte und Vertretertage gibt.</summary>
        internal static readonly IReadOnlyList<int> Stufen = Array.AsReadOnly(new[] { 50, 90, 95, 99 });

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
                    ZapfSatz.Neu("AUSLEGUNG_PERZENTIL_OHNE_REALISIERUNG"));
            var w = new double[stichprobe.Count];
            for (int i = 0; i < w.Length; i++)
            {
                if (double.IsNaN(stichprobe[i]))
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                        ZapfSatz.Neu("AUSLEGUNG_REALISIERUNG_OHNE_WERT"));
                w[i] = stichprobe[i];
            }
            Array.Sort(w);
            return new Perzentilwerte(w.Length, Wert(w, 50), Wert(w, 90), Wert(w, 95), Wert(w, 99), w[0], w[w.Length - 1]);
        }

        /// <summary>
        /// Der Rang <c>k = ⌈p · n / 100⌉</c> (mindestens 1) des p-Perzentils in einer aufsteigend
        /// geordneten Stichprobe aus <paramref name="anzahl"/> Werten — ganzzahlig gerechnet.
        /// </summary>
        internal static int Rang(int anzahl, int perzentil)
        {
            long k = ((long)perzentil * anzahl + 99) / 100;
            return k < 1 ? 1 : (int)k;
        }

        private static double Wert(double[] geordnet, int p) => geordnet[Rang(geordnet.Length, p) - 1];
    }

    /// <summary>
    /// <b>Die Kennzahlen einer Realisierung</b> des Auslegungsensembles (4.5 b): Tagessumme [kWh],
    /// größte Minuten- und Stundenleistung der Gruppe [kW] und — mit <see cref="Volumenauftrag"/> —
    /// das erforderliche Volumen der Summenlinie beim festen Φ_N [l] (+∞ ohne Nachweis, <c>null</c>
    /// ohne Auftrag). Mehr hält das Ensemble je Realisierung nicht; einen Tag zieht
    /// <see cref="Bedarfstagensemble.Tag"/> bei Bedarf aus seinem Seed nach.
    /// </summary>
    internal sealed record Realisierungskennzahl(int Realisierung, double TagessummeKwh, double MinutenspitzeKw,
                                                 double StundenspitzeKw, double? VolumenL);

    /// <summary>
    /// <b>Ein Vertretertag</b> (4.4 Ausgaben: P50/P90/P95/P99): die Realisierung, deren Kennzahl
    /// im Rang des Perzentils steht — bei gleichen Werten die kleinste Realisierung —, samt ihrem
    /// gezogenen Tag. Nur diese Tage bewahrt das Ensemble auf.
    /// </summary>
    internal sealed record Vertretertag(int Perzentil, int Realisierung, Bedarfstag Tag);

    /// <summary>
    /// Der Volumenauftrag an das Ensemble einer Speichergruppe (4.5 b): die Parameter der
    /// Summenlinie der Gruppe und das feste Φ_N des Summenlinienpunkts [kW]. Mit ihm rechnet das
    /// Ensemble je Realisierung das erforderliche Volumen schon während der Ziehung — so muss es
    /// keinen gezogenen Tag aufbewahren.
    /// </summary>
    internal sealed record Volumenauftrag(Summenlinienparameter Parameter, double LeistungKw);

    /// <summary>
    /// Die Statistik einer Zone im Auslegungsensemble: Einheiten, Tagesmenge, die Minutenspitze
    /// JE EINHEIT über alle Einheiten und Realisierungen (Wohnungsstation je Einheit, N10 (d);
    /// Nenner von GLF_P), mit Volumenauftrag das Volumen der ersten Einheit je Realisierung als
    /// Perzentile (Nenner von GLF_V; <c>null</c> ohne Auftrag oder ohne Tagesmenge) und — privat —
    /// die Minutenstatistik der Einheiten (<see cref="Minutenstatistik"/>, der Minutentyp neben dem
    /// Bedarfstag; Einzelstatistik für <c>μ + z · σ / √N</c>).
    /// </summary>
    internal sealed class Ensemblezonenstatistik
    {
        private readonly Minutenstatistik _einzel;

        internal Ensemblezonenstatistik(string zone, int einheiten, double tagesmengeKwh, Perzentilwerte spitzeJeEinheitKw,
                                        Perzentilwerte volumenEinheitL, Minutenstatistik einzel)
        {
            Zone = zone ?? "";
            Einheiten = einheiten;
            TagesmengeKwh = tagesmengeKwh;
            SpitzeJeEinheitKw = spitzeJeEinheitKw;
            VolumenEinheitL = volumenEinheitL;
            _einzel = einzel ?? throw new ArgumentNullException(nameof(einzel));
        }

        internal string Zone { get; }

        /// <summary>Die Einheiten n_E der Zone.</summary>
        internal int Einheiten { get; }

        /// <summary>Die Tagesmenge der Zone am maßgebenden Tag [kWh] — der Erwartungswert ihrer Ziehung.</summary>
        internal double TagesmengeKwh { get; }

        /// <summary>Die größte Minutenleistung einer Einheit [kW], über n_E · R Stichproben.</summary>
        internal Perzentilwerte SpitzeJeEinheitKw { get; }

        /// <summary>
        /// Das erforderliche Volumen der ersten Einheit je Realisierung [l] beim Anteil ihrer
        /// Tagesmenge an Φ_N, Speicherverlust und Zirkulation (Festlegung, 4.5 b) — <c>null</c> ohne
        /// Volumenauftrag oder ohne Tagesmenge.
        /// </summary>
        internal Perzentilwerte VolumenEinheitL { get; }

        /// <summary>Mittel der Minutenlast einer Einheit in Minute <paramref name="minute"/> [kWh je Minute].</summary>
        internal double MittelKwh(int minute) => _einzel.MittelKwh(minute);

        /// <summary>Varianz der Minutenlast einer Einheit in Minute <paramref name="minute"/> [kWh² je Minute²], ≥ 0.</summary>
        internal double Varianz(int minute) => _einzel.Varianz(minute);
    }

    /// <summary>
    /// <b>Das Auslegungsensemble einer Topologiegruppe</b> (4.4 „Zwei Ensembles", 4.5 b): die
    /// Kennzahlen der R gezogenen Bedarfstage der Gruppe — Superposition der n_E unabhängigen
    /// Einheiten jeder Zone —, je Zone die Einzelstatistik, mit Volumenauftrag die Volumina und die
    /// Vertretertage je Perzentil. Die Gleichzeitigkeit entsteht aus der Superposition; es gibt
    /// keinen Eingabefaktor.
    ///
    /// <para><b>Speicher begrenzt.</b> Je Realisierung bleiben nur die Kennzahlen, je Zone die
    /// Spitzen je Einheit und die Volumina der ersten Einheit; von den gezogenen Tagen nur die
    /// Vertretertage. Jeden anderen Tag zieht <see cref="Tag"/> aus seinem Seed nach — dieselben Bits.</para>
    /// </summary>
    internal sealed class Bedarfstagensemble
    {
        private readonly IReadOnlyList<Ensemblezone> _zonen;

        internal Bedarfstagensemble(IReadOnlyList<Ensemblezone> zonen, long seed, int perzentil,
                                    IReadOnlyList<Realisierungskennzahl> kennzahlen, IReadOnlyList<Ensemblezonenstatistik> statistik,
                                    Speicherensemble volumina, IReadOnlyList<Vertretertag> vertreterMinutenspitze,
                                    IReadOnlyList<Vertretertag> vertreterVolumen)
        {
            _zonen = zonen;
            Seed = seed;
            Perzentil = perzentil;
            Kennzahlen = kennzahlen;
            Zonen = statistik;
            Volumina = volumina;
            VertreterMinutenspitze = vertreterMinutenspitze;
            VertreterVolumen = vertreterVolumen;
            var spitze = new double[kennzahlen.Count];
            var stunde = new double[kennzahlen.Count];
            for (int r = 0; r < kennzahlen.Count; r++)
            {
                spitze[r] = kennzahlen[r].MinutenspitzeKw;
                stunde[r] = kennzahlen[r].StundenspitzeKw;
            }
            MinutenspitzeKw = Perzentilwerte.Aus(spitze);
            StundenspitzeKw = Perzentilwerte.Aus(stunde);
        }

        internal long Seed { get; }

        /// <summary>Das Auslegungsperzentil p (95 oder 99, K3).</summary>
        internal int Perzentil { get; }

        /// <summary>Die Zahl der Realisierungen R.</summary>
        internal int Realisierungen => Kennzahlen.Count;

        /// <summary>Die Kennzahlen je Realisierung (r = 0, 1, …) — die gezogenen Tage selbst bleiben nicht.</summary>
        internal IReadOnlyList<Realisierungskennzahl> Kennzahlen { get; }

        /// <summary>Die Zonen der Gruppe mit ihrer Einzelstatistik.</summary>
        internal IReadOnlyList<Ensemblezonenstatistik> Zonen { get; }

        /// <summary>Die größte Minutenleistung der Gruppe [kW] über die Realisierungen.</summary>
        internal Perzentilwerte MinutenspitzeKw { get; }

        /// <summary>Die größte Stundenleistung der Gruppe [kW] über die Realisierungen (nachrichtlich).</summary>
        internal Perzentilwerte StundenspitzeKw { get; }

        /// <summary>Die Volumina der Speichergruppe; <c>null</c> ohne Volumenauftrag.</summary>
        internal Speicherensemble Volumina { get; }

        /// <summary>Die Vertretertage der Minutenspitze zu P50, P90, P95 und P99.</summary>
        internal IReadOnlyList<Vertretertag> VertreterMinutenspitze { get; }

        /// <summary>Die Vertretertage des Volumens zu P50, P90, P95 und P99; leer ohne Volumenauftrag.</summary>
        internal IReadOnlyList<Vertretertag> VertreterVolumen { get; }

        /// <summary>Genügen die Realisierungen für ein empirisches p-Perzentil (R ≥ 1/(1 − p), 4.4)?</summary>
        internal bool Belastbar => Realisierungen >= Zapfensemble.Mindestzahl(Perzentil);

        /// <summary>
        /// Der gezogene Tag der Realisierung <paramref name="realisierung"/>, aus ihrem Seed neu
        /// gezogen — Bit für Bit derselbe Tag wie in der Ziehung des Ensembles.
        /// </summary>
        internal Bedarfstag Tag(int realisierung)
        {
            if (realisierung < 0 || realisierung >= Realisierungen) throw new ArgumentOutOfRangeException(nameof(realisierung));
            return Zapfensemble.Gruppentag(_zonen, Seed, realisierung);
        }

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
    /// (Speicher, mit <see cref="Volumenauftrag"/>), Gleichzeitigkeit von Leistung und Volumen.
    ///
    /// <para><b>Nie aus der Bilanz.</b> Das Ensemble liest Tagesmengen, Kategorien und
    /// Tageszeitdichten — keine Jahresreihe (Invariante 2.4, Wache
    /// <c>ZapfprofilTrennungWacheTests</c>). Minutenwerte stehen nur in <see cref="Bedarfstag"/> und
    /// <see cref="Minutenstatistik"/>.</para>
    ///
    /// <para><b>Parallel mit fester Summationsfolge.</b> Die Realisierungen laufen blockweise über
    /// <c>Kulturweitergabe.For</c>; summiert wird danach in der Folge r = 0, 1, … — parallel und
    /// seriell liefern dieselben Bits.</para>
    ///
    /// <para><b>Speicher begrenzt.</b> Je Realisierung bleiben die Kennzahlen (Tagessumme, Minuten-
    /// und Stundenspitze, Volumen), je Zone die Spitzen je Einheit (8 Byte je Einheitentag) und die
    /// Volumina der ersten Einheit, von den Tagen nur die Vertretertage je Perzentil — nicht mehr
    /// R Tage zu 1440 Minutenwerten. Zwei benannte Obergrenzen (<see cref="HOECHSTENS"/>,
    /// <see cref="HOECHSTENS_EINHEITSTAGE"/>) lehnen größere Ensembles benannt ab.</para>
    /// </summary>
    internal static class Zapfensemble
    {
        /// <summary>Realisierungen je Block der parallelen Rechnung (numerische Setzung, begrenzt den Speicher eines Blocks).</summary>
        internal const int BLOCK = 64;

        /// <summary>
        /// <b>Höchstzahl der Realisierungen des Bedarfstags</b> (numerische Setzung). Begründung: Das
        /// empirische p-Perzentil aus R Stichproben streut in der Rangwahrscheinlichkeit um
        /// <c>√(p · (1 − p) / R)</c>; bei R = 100 000 sind das für P99 rund 0,03 Prozentpunkte — mehr
        /// Realisierungen ändern das Perzentil nicht mehr erkennbar, nur die Laufzeit. Das ist das
        /// Tausendfache der Mindestzahl für P99 (100). Je Realisierung hält das Ensemble nur ihre
        /// Kennzahlen (einige Zahlen), der Speicher bleibt auch an der Grenze klein.
        /// </summary>
        internal const int HOECHSTENS = 100000;

        /// <summary>
        /// <b>Höchstzahl der Einheitentage R · Σ n_E eines Ensembles</b> (numerische Setzung).
        /// Begründung: Die Spitze je Einheit ist ein Perzentil über alle Einheitentage, das Ensemble
        /// hält dafür je Einheitentag eine Zahl (8 Byte) — an der Grenze 80 MB; die Laufzeit wächst
        /// linear mit den Einheitentagen. Eine Zone mit 1000 Einheiten erlaubt so noch 10 000
        /// Realisierungen, eine mit 100 000 Einheiten (Stadtquartier) die Mindestzahl für P99.
        /// </summary>
        internal const long HOECHSTENS_EINHEITSTAGE = 10000000L;

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
        /// (<see cref="ZapfStochastikParameter.AUSLEGUNG_VIELFACHES"/>). Fehlt beides, benannte
        /// Ablehnung; eine Zahl außerhalb 1 … <see cref="HOECHSTENS"/> lehnt als
        /// <see cref="ZapfEingabefehler.StochastikUngueltig"/> ab.
        /// </summary>
        internal static int RealisierungenAuslegung(int? projekt, int perzentil, Parametersatz ps)
        {
            if (projekt.HasValue) return RealisierungenPruefen(projekt.Value);
            double vielfaches = ps.Wert(ZapfStochastikParameter.AUSLEGUNG_VIELFACHES);
            if (double.IsNaN(vielfaches) || double.IsInfinity(vielfaches) || !(vielfaches > 0))
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_VIELFACHES"));
            double r = Math.Ceiling(vielfaches * Mindestzahl(perzentil));
            if (r > HOECHSTENS) throw ZuViele();
            return (int)r;
        }

        /// <summary>
        /// <b>Zieht das Ensemble des Bedarfstags</b>: R Realisierungen, je Realisierung je Zone
        /// (Reihenfolge des Eingangs) je Einheit ein Tag nach <see cref="Zapfereignisgenerator"/> mit
        /// der Tagesmenge der Zone geteilt durch n_E. Ein Ereignis über Mitternacht läuft am
        /// Tagesanfang weiter (der Bedarfstag wiederholt sich). Mit <paramref name="volumen"/> rechnet
        /// jede Realisierung ihr erforderliches Volumen und das der ersten Einheit jeder Zone
        /// (<see cref="Volumina"/>, 4.5 b) gleich mit. Mit <paramref name="abbruch"/> endet die Ziehung
        /// zwischen zwei Realisierungen mit <see cref="OperationCanceledException"/> (der nebenläufige
        /// Lauf der Oberfläche, 5.1).
        /// </summary>
        internal static Bedarfstagensemble Ziehen(IReadOnlyList<Ensemblezone> zonen, long seed, int realisierungen,
                                                  int perzentil, Volumenauftrag volumen = null, bool parallel = true,
                                                  CancellationToken abbruch = default)
        {
            PerzentilPruefen(perzentil);
            RealisierungenPruefen(realisierungen);
            if (zonen == null || zonen.Count == 0)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_ENSEMBLE_OHNE_ZONE"));
            long einheiten = 0;
            foreach (Ensemblezone z in zonen)
            {
                if (z == null || z.Kategorien == null || z.Dichte == null || z.Einheiten < 1 || z.Index < 0)
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                        ZapfSatz.Neu("AUSLEGUNG_ENSEMBLEZONE_UNVOLLSTAENDIG"));
                Auslegungspruefung.NichtNegativ(z.TagesmengeKwh, ZapfSatz.Neu("BEGRIFF_TAGESMENGE_ZONE", z.Zone ?? ""));
                if (z.TagesmengeKwh > 0) Auslegungspruefung.Positiv(z.SpreizungK, ZapfSatz.Neu("BEGRIFF_SPREIZUNG_ZONE", z.Zone ?? ""));
                einheiten += z.Einheiten;
            }
            if (einheiten * realisierungen > HOECHSTENS_EINHEITSTAGE)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.StochastikUngueltig, "",
                    ZapfSatz.Neu("EINGABE_ENSEMBLE_EINHEITSTAGE", (long)einheiten * realisierungen, (long)HOECHSTENS_EINHEITSTAGE));
            Volumenplan plan = volumen == null ? null : new Volumenplan(volumen, zonen);

            int zahl = zonen.Count;
            var kennzahlen = new Realisierungskennzahl[realisierungen];
            var spitzen = new double[zahl][];
            var volumenEinheit = new double[zahl][];
            var einzel = new Minutenstatistik[zahl];
            for (int zi = 0; zi < zahl; zi++)
            {
                spitzen[zi] = new double[realisierungen * zonen[zi].Einheiten];
                if (plan != null && plan.Eigen[zi] != null) volumenEinheit[zi] = new double[realisierungen];
                einzel[zi] = new Minutenstatistik();
            }

            for (int start = 0; start < realisierungen; start += BLOCK)
            {
                int anzahl = Math.Min(BLOCK, realisierungen - start);
                var teil = new Realisierungsteil[anzahl];
                Lauf(anzahl, parallel, i => teil[i] = Realisierung(zonen, seed, start + i, plan), abbruch);
                // Feste Summationsfolge: Realisierung für Realisierung; die Tage des Blocks verfallen danach.
                for (int i = 0; i < anzahl; i++)
                {
                    int r = start + i;
                    kennzahlen[r] = teil[i].Kennzahl;
                    for (int zi = 0; zi < zahl; zi++)
                    {
                        Array.Copy(teil[i].Spitzen[zi], 0, spitzen[zi], r * zonen[zi].Einheiten, zonen[zi].Einheiten);
                        if (volumenEinheit[zi] != null) volumenEinheit[zi][r] = teil[i].VolumenEinheitL[zi];
                        einzel[zi].Hinzufuegen(teil[i].Einzel[zi]);
                    }
                }
            }

            var statistik = new Ensemblezonenstatistik[zahl];
            for (int zi = 0; zi < zahl; zi++)
                statistik[zi] = new Ensemblezonenstatistik(zonen[zi].Zone, zonen[zi].Einheiten, zonen[zi].TagesmengeKwh,
                    Perzentilwerte.Aus(spitzen[zi]), volumenEinheit[zi] == null ? null : Perzentilwerte.Aus(volumenEinheit[zi]),
                    einzel[zi]);

            var bekannt = new Dictionary<int, Bedarfstag>();
            IReadOnlyList<Vertretertag> vertreterSpitze = Vertreter(zonen, seed, kennzahlen, k => k.MinutenspitzeKw, bekannt);
            IReadOnlyList<Vertretertag> vertreterVolumen = new Vertretertag[0];
            Speicherensemble speicher = null;
            if (plan != null)
            {
                speicher = Volumina(kennzahlen, statistik, plan, perzentil);
                vertreterVolumen = Vertreter(zonen, seed, kennzahlen, k => k.VolumenL.Value, bekannt);
            }
            return new Bedarfstagensemble(zonen, seed, perzentil, Array.AsReadOnly(kennzahlen), Array.AsReadOnly(statistik),
                                          speicher, vertreterSpitze, vertreterVolumen);
        }

        /// <summary>
        /// Der gezogene Tag der Gruppe in Realisierung <paramref name="r"/> — aus dem Seed neu gezogen,
        /// Bit für Bit wie in <see cref="Ziehen"/>.
        /// </summary>
        internal static Bedarfstag Gruppentag(IReadOnlyList<Ensemblezone> zonen, long seed, int r)
            => Realisierung(zonen, seed, r, null).Tag;

        // =================================================================================
        // Volumina der Speichergruppe (4.5 b)
        // =================================================================================

        /// <summary>
        /// Die Parameter der Volumina: je Realisierung das kleinste Volumen mit Nachweis beim festen
        /// Φ_N (Übertrager und Erzeuger fallen auf diese Leistung) mit den übrigen Größen der
        /// Summenlinie der Gruppe; je Zone das der ersten Einheit mit dem Anteil ihrer Tagesmenge an
        /// Φ_N, Speicherverlust und Zirkulation (Festlegung) — <c>null</c> ohne Tagesmenge.
        /// </summary>
        private sealed class Volumenplan
        {
            internal readonly Summenlinienparameter Fest;
            internal readonly Summenlinienparameter[] Eigen;
            internal readonly double LeistungKw;
            internal readonly double TagSummeKwh;

            internal Volumenplan(Volumenauftrag a, IReadOnlyList<Ensemblezone> zonen)
            {
                if (a.Parameter == null) throw new ArgumentNullException(nameof(a), "Der Volumenauftrag trägt keine Parameter.");
                Auslegungspruefung.Positiv(a.LeistungKw, ZapfSatz.Neu("BEGRIFF_PHI_N"));
                LeistungKw = a.LeistungKw;
                Summenlinienparameter p = a.Parameter;
                Fest = p with { ErzeugerKw = LeistungKw, Uebertrager = null };
                foreach (Ensemblezone z in zonen) TagSummeKwh += z.TagesmengeKwh;
                Eigen = new Summenlinienparameter[zonen.Count];
                for (int zi = 0; zi < zonen.Count; zi++)
                {
                    double anteil = TagSummeKwh > 0 ? zonen[zi].TagesmengeKwh / zonen[zi].Einheiten / TagSummeKwh : 0.0;
                    if (!(anteil > 0)) continue;
                    Eigen[zi] = Fest with
                    {
                        ErzeugerKw = LeistungKw * anteil,
                        SpeicherverlustKw = p.SpeicherverlustKw * anteil,
                        Zirkulation = new Zirkulationslast(p.Zirkulation.LeistungKw * anteil, p.Zirkulation.Laufzeit)
                    };
                }
            }
        }

        /// <summary>
        /// Die Volumina (4.5 b): V_r je Realisierung als Perzentile, Realisierungen ohne Nachweis
        /// (V = ∞) und <c>GLF_V = P_p(V_Summe) / Σ_i P_p(V_i)</c> mit Σ_i = Σ_Zonen n_E · P_p(V_Einheit);
        /// <c>null</c> ohne Tagesmenge, bei unendlichem Perzentil oder ohne Nenner.
        /// </summary>
        private static Speicherensemble Volumina(Realisierungskennzahl[] kennzahlen, Ensemblezonenstatistik[] statistik,
                                                 Volumenplan plan, int perzentil)
        {
            var volumen = new double[kennzahlen.Length];
            int ohne = 0;
            for (int r = 0; r < volumen.Length; r++)
            {
                volumen[r] = kennzahlen[r].VolumenL.Value;
                if (double.IsPositiveInfinity(volumen[r])) ohne++;
            }
            Perzentilwerte werte = Perzentilwerte.Aus(volumen);
            double? glf = null;
            double oben = werte.Wert(perzentil);
            if (plan.TagSummeKwh > 0 && !double.IsPositiveInfinity(oben))
            {
                double nenner = 0.0;
                bool endlich = true;
                foreach (Ensemblezonenstatistik z in statistik)
                {
                    if (z.VolumenEinheitL == null) continue;
                    double pz = z.VolumenEinheitL.Wert(perzentil);
                    if (double.IsPositiveInfinity(pz)) endlich = false;
                    nenner += z.Einheiten * pz;
                }
                if (endlich && nenner > 0) glf = oben / nenner;
            }
            return new Speicherensemble(werte, ohne, plan.LeistungKw, glf);
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
        /// Die Vertretertage zu P50, P90, P95 und P99 einer Kennzahl: Rangfolge nach Wert, bei
        /// gleichem Wert nach Realisierung; der Tag im Rang des Perzentils wird aus seinem Seed neu
        /// gezogen (je Realisierung einmal, <paramref name="bekannt"/>).
        /// </summary>
        private static IReadOnlyList<Vertretertag> Vertreter(IReadOnlyList<Ensemblezone> zonen, long seed,
                                                             Realisierungskennzahl[] kennzahlen, Func<Realisierungskennzahl, double> wert,
                                                             Dictionary<int, Bedarfstag> bekannt)
        {
            int n = kennzahlen.Length;
            var ordnung = new int[n];
            for (int i = 0; i < n; i++) ordnung[i] = i;
            Array.Sort(ordnung, (a, b) =>
            {
                int c = wert(kennzahlen[a]).CompareTo(wert(kennzahlen[b]));
                return c != 0 ? c : a.CompareTo(b);
            });
            var liste = new List<Vertretertag>(Perzentilwerte.Stufen.Count);
            foreach (int p in Perzentilwerte.Stufen)
            {
                int r = ordnung[Perzentilwerte.Rang(n, p) - 1];
                if (!bekannt.TryGetValue(r, out Bedarfstag tag))
                {
                    tag = Gruppentag(zonen, seed, r);
                    bekannt.Add(r, tag);
                }
                liste.Add(new Vertretertag(p, r, tag));
            }
            return liste.AsReadOnly();
        }

        // =================================================================================
        // Eine Realisierung
        // =================================================================================

        /// <summary>
        /// Was eine Realisierung beiträgt — ihre Kennzahlen, je Zone die Spitzen je Einheit, das
        /// Volumen der ersten Einheit (NaN ohne Auftrag) und die Minutenstatistik; dazu der Tag
        /// selbst, den nur <see cref="Gruppentag"/> weiterreicht.
        /// </summary>
        private sealed class Realisierungsteil
        {
            internal Bedarfstag Tag;
            internal Realisierungskennzahl Kennzahl;
            internal double[][] Spitzen;
            internal double[] VolumenEinheitL;
            internal Minutenstatistik[] Einzel;
        }

        private static Realisierungsteil Realisierung(IReadOnlyList<Ensemblezone> zonen, long seed, int r, Volumenplan plan)
        {
            int zahl = zonen.Count;
            var teil = new Realisierungsteil
            {
                Spitzen = new double[zahl][], VolumenEinheitL = new double[zahl], Einzel = new Minutenstatistik[zahl]
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
                var einzel = new Minutenstatistik();
                teil.VolumenEinheitL[zi] = double.NaN;
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
                        if (minuten[t] > groesste) groesste = minuten[t];
                    einzel.Hinzufuegen(minuten);
                    spitzen[u] = groesste * Bedarfstag.MINUTEN_JE_STUNDE;
                    if (u == 0 && plan != null && plan.Eigen[zi] != null)
                        teil.VolumenEinheitL[zi] = Volumen(Bedarfstag.AusZiehung(name + ", " + z.Zone + ", Einheit 1", einheit),
                                                           plan.Eigen[zi]);
                    gruppe.AddRange(einheit);
                }
                teil.Spitzen[zi] = spitzen;
                teil.Einzel[zi] = einzel;
            }
            Bedarfstag tag = Bedarfstag.AusZiehung(name, gruppe);
            teil.Tag = tag;
            teil.Kennzahl = new Realisierungskennzahl(r, tag.TagessummeKwh, tag.GroessteMinutenleistungKw,
                                                      tag.GroessteStundenleistungKw, plan == null ? (double?)null : Volumen(tag, plan.Fest));
            return teil;
        }

        /// <summary>
        /// Führt <paramref name="arbeit"/> für 0 … <paramref name="anzahl"/> − 1 aus — parallel über
        /// <c>Kulturweitergabe.For</c> oder seriell. Die Ausnahme eines Arbeitspakets kommt
        /// <b>ausgepackt</b> heraus, nicht als <see cref="AggregateException"/>, damit die Fassaden sie
        /// benannt fangen; werfen mehrere Pakete, gilt die erste benannte
        /// (<see cref="ZapfprofilEingabeException"/>, <see cref="ZapfAuslegungException"/>), sonst die erste.
        /// Mit <paramref name="abbruch"/> endet der Lauf zwischen zwei Paketen mit
        /// <see cref="OperationCanceledException"/> (der nebenläufige Lauf der Oberfläche, 5.1).
        /// </summary>
        internal static void Lauf(int anzahl, bool parallel, Action<int> arbeit, CancellationToken abbruch = default)
        {
            abbruch.ThrowIfCancellationRequested();
            if (!parallel || anzahl <= 1)
            {
                for (int i = 0; i < anzahl; i++)
                {
                    abbruch.ThrowIfCancellationRequested();
                    arbeit(i);
                }
                return;
            }
            try
            {
                SpeicherEngine.Kulturweitergabe.For(0, anzahl,
                    abbruch.CanBeCanceled ? new ParallelOptions { CancellationToken = abbruch } : null, arbeit);
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
                    ZapfSatz.Neu("AUSLEGUNG_PERZENTIL_WERT", perzentil));
        }

        private static int RealisierungenPruefen(int realisierungen)
        {
            if (realisierungen < 1 || realisierungen > HOECHSTENS) throw ZuViele();
            return realisierungen;
        }

        private static ZapfprofilEingabeException ZuViele()
            => new ZapfprofilEingabeException(ZapfEingabefehler.StochastikUngueltig, "",
                   ZapfSatz.Neu("EINGABE_REALISIERUNGEN_BEDARFSTAG", HOECHSTENS));
    }
}
