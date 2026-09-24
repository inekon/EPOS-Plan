using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Zone der stochastischen Jahresreihe</b> (4.4, Bilanz): Stelle im Eingang (Seed),
    /// Einheiten n_E, Kategorien, Jahresmenge der Zapfung [kWh/a] (nach Kalibrierung), die normierte
    /// Zeitstruktur, der Kalender der Zone samt Ferien, die Kaltwasserfaktoren und die Spreizung
    /// θ_Zapf − θ_KW(m) je Monat [K], der Kalender der Klimaregion (für die versetzten Ferien) und —
    /// nur bei Kalenderart Wohnen mit Ferien — die Entkopplung der Urlaube mit ihrem größten Versatz.
    /// </summary>
    internal sealed record Jahreszone
    {
        public int Index { get; init; }
        public string Zone { get; init; } = "";
        public int Einheiten { get; init; }
        public Zapfkategoriensatz Kategorien { get; init; }
        public double JahresmengeKwh { get; init; }
        public Zeitstruktur Struktur { get; init; }
        public ZapfTagtyp[] Kalender { get; init; }
        public double[] Kaltwasserfaktor { get; init; }
        public double[] SpreizungJeMonatK { get; init; }
        public int WochentagJan1 { get; init; }
        public bool[] We { get; init; }
        public IReadOnlyList<Ferienfenster> Ferien { get; init; } = new Ferienfenster[0];

        /// <summary>Entkoppelt jede Einheit ihre Ferienfenster (Kalenderart Wohnen, 4.4)?</summary>
        public bool Urlaubsentkopplung { get; init; }

        /// <summary>Größter Versatz der Ferienfenster je Einheit [d] (Parameter, nur mit Entkopplung).</summary>
        public int UrlaubsversatzTage { get; init; }
    }

    /// <summary>
    /// Die Konsistenzprobe der stochastischen Jahresreihe einer Zone (2.4, 4.4): Jahresenergie
    /// deterministisch und im Ensemblemittel, Streuung s_R der Jahresenergie über die R
    /// Realisierungen, Toleranz <c>max(1 %, 3 · s_R / √R)</c>, erfüllt ja/nein und die größte
    /// Abweichung der Anteile je Tagesstunde (Formvektor = Erwartungswert) — alles aus den R Jahren.
    /// Dazu die Jahresenergie der Realisierung zum Seed (r = 0) und der Faktor der Energieprobe
    /// <c>E_det / E_0</c>, der sie auf die Jahresmenge des Mengengerüsts bringt: Sie allein ist die
    /// Bilanzreihe (<see cref="Jahresensemble.Bilanz"/>), das Ensemble prüft nur.
    /// </summary>
    internal sealed record Jahreskonsistenz(double DeterministischKwh, double MittelKwh, double StandardabweichungKwh,
                                            int Realisierungen, double ToleranzKwh, bool Erfuellt, double JahrZumSeedKwh,
                                            double Faktor, double TagesgangAbweichung)
    {
        /// <summary>
        /// Die relative Abweichung des Mittels der R Jahre vom deterministischen Pfad
        /// <c>Ē / E_det − 1</c> [-]; <c>null</c> ohne Jahresmenge (E_det = 0).
        /// </summary>
        internal double? Abweichung => DeterministischKwh != 0 ? MittelKwh / DeterministischKwh - 1.0 : (double?)null;
    }

    /// <summary>
    /// <b>Das Ensemble der Jahresreihe</b> („stochastisch", Bilanz; Umsetzungskonzept
    /// Zapfprofilgenerator 4.4 „Zwei Ensembles", 2.3 Satz 3): R Jahre einer Zone, je Realisierung
    /// die Superposition der n_E Einheiten in Minutenauflösung, auf Stunden summiert. <b>In die
    /// Bilanz geht die eine Realisierung zum Seed</b> (r = 0, „Stundenreihe zum Seed", 4.4
    /// Ausgaben), mit dem Faktor der Energieprobe auf die Jahresmenge gebracht; die R Jahre dienen
    /// allein der Konsistenzprobe (Mittel gegen den deterministischen Pfad, Streuband, s_R) und
    /// erreichen die Bilanz nie.
    ///
    /// <code>
    /// Einheit i:   Q_d,i = Formvektor.Tagesmengen(Q_a / n_E, Zeitstruktur, Kalender_i, f_KW)   Σ_d Q_d,i = Q_a / n_E
    ///              Kalender_i = Kalender der Klimaregion mit den Ferien der Zone um δ_i versetzt,
    ///              δ_i ~ gleichverteilt in [−v; v] (Entkopplung der Urlaube, nur Wohnen), sonst der der Zone
    /// je Tag d:    Ereignisse nach Zapfereignisgenerator mit Q_d,i, Δθ(m(d)), Dichte des Tagtyps
    /// Minute:      E / Dauer je Minute, über Mitternacht in den nächsten Tag (Jahresende → Jahresanfang)
    /// Stunde:      q_h = Σ_{Minuten der Stunde} q_min
    /// Bilanz:      q_h = q_h,0 · E_det / E_0            (Realisierung zum Seed, Faktor der Energieprobe)
    /// Probe:       q̄_h = Σ_r q_h,r / R, Ē, s_R            (Folge r = 0, 1, …; nur Konsistenzprobe)
    /// </code>
    ///
    /// <para><b>Nur Bilanz.</b> Das Ensemble ist eine Bilanzreihe und erreicht die Auslegung nie
    /// (Wache <c>ZapfprofilTrennungWacheTests</c>). Rein, deterministisch je Seed; die
    /// Realisierungen laufen wahlweise parallel über <c>Kulturweitergabe.For</c>, blockweise, und
    /// werden in fester Folge summiert — parallel und seriell liefern dieselben Bits.</para>
    ///
    /// <para><b>Speicher begrenzt.</b> Von den R Jahren bleiben das Jahr zum Seed (die Bilanz), das
    /// Mittel der Stundenwerte und die Jahresenergie je Realisierung — nicht R Reihen zu 8760
    /// Stunden; gerechnet wird in Blöcken zu <see cref="BLOCK"/> Jahren.</para>
    /// </summary>
    internal sealed class Jahresensemble
    {
        /// <summary>Kleinste relative Toleranz der Konsistenzprobe (4.4: 1 %) — numerische Setzung des Papiers.</summary>
        internal const double TOLERANZ_RELATIV = 0.01;

        /// <summary>Faktor der Standardabweichung in der Toleranz (4.4: 3 · s_R / √R) — numerische Setzung des Papiers.</summary>
        internal const double TOLERANZ_STREUUNG = 3.0;

        /// <summary>
        /// <b>Höchstzahl der Jahre der Jahresreihe</b> (numerische Setzung; Vorgabe 10, 4.4).
        /// Begründung: Die Jahre dienen allein der Konsistenzprobe; ihre statistische Toleranz
        /// <c>3 · s_R / √R</c> ist bei R = 1000 auf etwa ein Zehntel von s_R gefallen und liegt damit
        /// unter der Untergrenze von 1 %, sobald s_R unter 10 % der Jahresmenge liegt — mehr Jahre
        /// schärfen die Probe nicht mehr, sie verlängern nur die Laufzeit (je Jahr n_E · 365
        /// gezogene Einheitentage).
        /// </summary>
        internal const int HOECHSTENS = 1000;

        /// <summary>Jahre je Block der parallelen Rechnung (numerische Setzung: 64 Reihen zu 8760 Stunden, rund 4,5 MB).</summary>
        internal const int BLOCK = 64;

        private readonly double[] _jahresenergienKwh;

        private Jahresensemble(string zone, long seed, Bilanzreihe jahrZumSeed, double[] jahresenergienKwh, Bilanzreihe mittel)
        {
            Zone = zone ?? "";
            Seed = seed;
            _jahresenergienKwh = jahresenergienKwh;
            JahrZumSeed = jahrZumSeed;
            Mittel = mittel;
            double summe = 0.0;
            foreach (double e in jahresenergienKwh) summe += e;
            MittelJahresenergieKwh = summe / jahresenergienKwh.Length;
            double q = 0.0;
            foreach (double e in jahresenergienKwh)
            {
                double d = e - MittelJahresenergieKwh;
                q += d * d;
            }
            StandardabweichungKwh = jahresenergienKwh.Length > 1 ? Math.Sqrt(q / (jahresenergienKwh.Length - 1)) : 0.0;
        }

        /// <summary>Die Zone des Ensembles (für benannte Ablehnungen).</summary>
        internal string Zone { get; }

        internal long Seed { get; }

        /// <summary>Die Zahl der Realisierungen R.</summary>
        internal int Realisierungen => _jahresenergienKwh.Length;

        /// <summary>Die Jahresenergie je Realisierung [kWh] (r = 0, 1, …) — die Stichprobe der Konsistenzprobe.</summary>
        internal IReadOnlyList<double> JahresenergienKwh => Array.AsReadOnly(_jahresenergienKwh);

        /// <summary>
        /// Die Realisierung zum Seed (r = 0) vor dem Faktor der Energieprobe — die Grundlage der
        /// Bilanzreihe (<see cref="Bilanz"/>). Sie hängt nicht von R ab: Ein Ensemble aus einem Jahr
        /// und eines aus zehn Jahren zum selben Seed tragen dieselbe Realisierung zum Seed.
        /// </summary>
        internal Bilanzreihe JahrZumSeed { get; }

        /// <summary>Das Mittel über die Realisierungen — allein für die Konsistenzprobe, nie Bilanz.</summary>
        internal Bilanzreihe Mittel { get; }

        /// <summary>Das Mittel der Jahresenergien der Realisierungen [kWh].</summary>
        internal double MittelJahresenergieKwh { get; }

        /// <summary>Die Standardabweichung s_R der Jahresenergie über die Realisierungen [kWh]; 0 bei R = 1.</summary>
        internal double StandardabweichungKwh { get; }

        /// <summary>
        /// <b>Die Konsistenzprobe</b> gegen die deterministische Reihe derselben Zone (2.4, 4.4), aus
        /// den R Jahren: <c>|Ē − E_det| ≤ max(1 % · E_det, 3 · s_R / √R)</c>; die Tagesgangabweichung
        /// ist die größte Differenz der Anteile je Tagesstunde (Summe über das Jahr) — der Formvektor
        /// als Erwartungswert. Dazu der Faktor der Energieprobe <c>E_det / E_0</c>, der die
        /// Realisierung zum Seed auf die Jahresmenge bringt (NaN, wenn sie keine Zapfung trägt, die
        /// Jahresmenge aber positiv ist).
        /// </summary>
        internal Jahreskonsistenz Pruefen(Bilanzreihe deterministisch)
        {
            if (deterministisch == null) throw new ArgumentNullException(nameof(deterministisch));
            double det = deterministisch.JahressummeKwh;
            double mittel = Mittel.JahressummeKwh;
            double toleranz = Math.Max(TOLERANZ_RELATIV * Math.Abs(det), TOLERANZ_STREUUNG * StandardabweichungKwh / Math.Sqrt(Realisierungen));
            double seedJahr = JahrZumSeed.JahressummeKwh;
            double faktor = seedJahr > 0 ? det / seedJahr : (det == 0 ? 1.0 : double.NaN);
            return new Jahreskonsistenz(det, MittelJahresenergieKwh, StandardabweichungKwh, Realisierungen, toleranz,
                Math.Abs(mittel - det) <= toleranz, seedJahr, faktor, Tagesgangabweichung(Mittel, deterministisch));
        }

        /// <summary>
        /// <b>Die Bilanzreihe des Rechenwegs „stochastisch"</b> (2.3 Satz 3, 4.4 Ausgaben): die
        /// Realisierung zum Seed mal dem Faktor der Energieprobe aus <paramref name="k"/> — nie das
        /// Mittel des Ensembles. Trägt die Realisierung zum Seed keine Zapfung bei positiver
        /// Jahresmenge (Faktor nicht endlich), wird benannt abgelehnt
        /// (<see cref="ZapfEingabefehler.StochastikUngueltig"/>).
        /// </summary>
        internal Bilanzreihe Bilanz(Jahreskonsistenz k)
        {
            if (k == null) throw new ArgumentNullException(nameof(k));
            if (double.IsNaN(k.Faktor) || double.IsInfinity(k.Faktor))
                throw Fehler(Zone, "Nicht rechenbar — das gezogene Jahr zum Seed der Zone „" + Zone
                                   + "“ trägt keine Zapfung; die Jahresmenge ist nicht darstellbar.");
            return JahrZumSeed.Mal(k.Faktor);
        }

        // =================================================================================
        // Ziehen
        // =================================================================================

        /// <summary>
        /// <b>Zieht R Jahre einer Zone</b> (Formel oben). Seed je Realisierung, Zone und Einheit
        /// (<see cref="ZapfZufall.Realisierungsseed"/>, <see cref="ZapfZufall.Kindseed"/>); ein
        /// ungültiger Eingang oder R außerhalb 1 … <see cref="HOECHSTENS"/> wird benannt abgelehnt
        /// (<see cref="ZapfEingabefehler.StochastikUngueltig"/>). Mit <paramref name="abbruch"/> endet
        /// die Ziehung je Einheit mit <see cref="OperationCanceledException"/> — der nebenläufige Lauf
        /// der Oberfläche (5.1) bricht ab, ohne ein halbes Ensemble zu liefern.
        /// </summary>
        internal static Jahresensemble Ziehen(Jahreszone z, long seed, int realisierungen, bool parallel = true,
                                              CancellationToken abbruch = default)
        {
            if (z == null) throw new ArgumentNullException(nameof(z));
            string zone = z.Zone ?? "";
            if (realisierungen < 1 || realisierungen > HOECHSTENS)
                throw Fehler(zone, "Nicht rechenbar — die Zahl der Realisierungen der Jahresreihe liegt nicht in 1 … "
                                   + HOECHSTENS.ToString(CultureInfo.InvariantCulture) + ".");
            if (z.Kategorien == null || z.Struktur == null || z.Einheiten < 1 || z.Index < 0)
                throw Fehler(zone, "Nicht rechenbar — die Zone „" + zone + "“ der Jahresreihe ist unvollständig.");
            if (z.Kalender == null || z.Kalender.Length != Zapfkalender.TAGE || z.We == null || z.We.Length != Zapfkalender.TAGE
                || z.Kaltwasserfaktor == null || z.Kaltwasserfaktor.Length != Zapfkalender.MONATE
                || z.SpreizungJeMonatK == null || z.SpreizungJeMonatK.Length != Zapfkalender.MONATE)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.RasterUngueltig, zone,
                    "Nicht rechenbar — Kalender, Kaltwasserfaktor oder Spreizung der Zone „" + zone + "“ haben nicht das Jahresraster.");
            if (z.Urlaubsentkopplung && (z.UrlaubsversatzTage < 0 || z.UrlaubsversatzTage >= Zapfkalender.TAGE))
                throw Fehler(zone, "Nicht rechenbar — der Urlaubsversatz liegt nicht in 0 … 364 Tagen.");

            var vorbereitung = new Vorbereitung(z);
            var energien = new double[realisierungen];
            var mittel = new double[Bilanzreihe.STUNDEN];
            Bilanzreihe jahrZumSeed = null;
            for (int start = 0; start < realisierungen; start += BLOCK)
            {
                int anzahl = Math.Min(BLOCK, realisierungen - start);
                var teil = new Bilanzreihe[anzahl];
                // Parallel oder seriell; eine Ausnahme eines Fadens kommt ausgepackt heraus (benannt, nicht als AggregateException).
                Zapfensemble.Lauf(anzahl, parallel, i => teil[i] = Realisierung(z, vorbereitung, seed, start + i, abbruch), abbruch);
                // Feste Folge r = 0, 1, … — unabhängig von der Reihenfolge der Fäden; die Reihen des Blocks verfallen danach.
                for (int i = 0; i < anzahl; i++)
                {
                    int r = start + i;
                    if (r == 0) jahrZumSeed = teil[i];
                    energien[r] = teil[i].JahressummeKwh;
                    IReadOnlyList<double> s = teil[i].StundenKwh;
                    for (int h = 0; h < Bilanzreihe.STUNDEN; h++) mittel[h] += s[h];
                }
            }
            for (int h = 0; h < Bilanzreihe.STUNDEN; h++) mittel[h] /= realisierungen;
            return new Jahresensemble(zone, seed, jahrZumSeed, energien, new Bilanzreihe(mittel));
        }

        /// <summary>Was für alle Realisierungen gleich ist: Monat je Tag, Dichten je Tagtyp, gemeinsame Tagesmengen.</summary>
        private sealed class Vorbereitung
        {
            internal readonly int[] MonatJeTag = new int[Zapfkalender.TAGE];
            internal readonly Tageszeitdichte[] Dichte = new Tageszeitdichte[Tagesgangsatz.TAGTYPEN];
            internal readonly double JeEinheitKwh;
            internal readonly double[] GemeinsamKwh;

            internal Vorbereitung(Jahreszone z)
            {
                for (int d = 1; d <= Zapfkalender.TAGE; d++) MonatJeTag[d - 1] = Zapfkalender.Monat(d) - 1;
                for (int t = 0; t < Tagesgangsatz.TAGTYPEN; t++) Dichte[t] = Tageszeitdichte.Aus(z.Struktur, (ZapfTagtyp)(t + 1));
                JeEinheitKwh = z.JahresmengeKwh / z.Einheiten;
                if (!z.Urlaubsentkopplung)
                    GemeinsamKwh = Formvektor.Tagesmengen(JeEinheitKwh, z.Struktur, z.Kalender, z.WochentagJan1, z.Kaltwasserfaktor, z.Zone);
            }
        }

        private static Bilanzreihe Realisierung(Jahreszone z, Vorbereitung v, long seed, int r, CancellationToken abbruch)
        {
            const int minutenJahr = Zapfkalender.TAGE * Bedarfstag.MINUTEN;
            var stunden = new double[Bilanzreihe.STUNDEN];
            var ereignisse = new List<Zapfereignis>();
            ulong zs = ZapfZufall.Kindseed(ZapfZufall.Realisierungsseed(seed, r), z.Index);
            for (int u = 0; u < z.Einheiten; u++)
            {
                abbruch.ThrowIfCancellationRequested();
                var zufall = new ZapfZufall(ZapfZufall.Kindseed(zs, u));
                ZapfTagtyp[] kalender = z.Kalender;
                double[] tage = v.GemeinsamKwh;
                if (z.Urlaubsentkopplung)
                {
                    int versatz = zufall.Ganzzahl(2 * z.UrlaubsversatzTage + 1) - z.UrlaubsversatzTage;
                    kalender = Zapfkalender.Bilden(z.WochentagJan1, z.We, Versetzt(z.Ferien, versatz));
                    tage = Formvektor.Tagesmengen(v.JeEinheitKwh, z.Struktur, kalender, z.WochentagJan1, z.Kaltwasserfaktor, z.Zone);
                }
                for (int d = 0; d < Zapfkalender.TAGE; d++)
                {
                    ereignisse.Clear();
                    Zapfereignisgenerator.Ziehen(zufall, z.Kategorien, tage[d], z.SpreizungJeMonatK[v.MonatJeTag[d]],
                                                 v.Dichte[(int)kalender[d] - 1], ereignisse);
                    int tagesbeginn = d * Bedarfstag.MINUTEN;
                    foreach (Zapfereignis e in ereignisse)
                    {
                        double jeMinute = e.EnergieKwh / e.DauerMin;
                        for (int k = 0; k < e.DauerMin; k++)
                        {
                            int m = (tagesbeginn + e.MinuteBeginn + k) % minutenJahr;
                            stunden[m / Bedarfstag.MINUTEN_JE_STUNDE] += jeMinute;
                        }
                    }
                }
            }
            return new Bilanzreihe(stunden);
        }

        /// <summary>
        /// Die Ferienfenster um <paramref name="versatz"/> Tage verschoben, über den Jahreswechsel
        /// umlaufend (das Rechenjahr wiederholt sich); ein Fenster über den Jahreswechsel wird geteilt.
        /// </summary>
        internal static IReadOnlyList<Ferienfenster> Versetzt(IReadOnlyList<Ferienfenster> ferien, int versatz)
        {
            var neu = new List<Ferienfenster>();
            if (ferien == null) return neu;
            foreach (Ferienfenster f in ferien)
            {
                if (f == null) continue;
                int laenge = f.Ende - f.Beginn + 1;
                if (laenge >= Zapfkalender.TAGE)
                {
                    neu.Add(new Ferienfenster(1, Zapfkalender.TAGE));
                    continue;
                }
                int beginn = ((f.Beginn - 1 + versatz) % Zapfkalender.TAGE + Zapfkalender.TAGE) % Zapfkalender.TAGE + 1;
                int ende = beginn + laenge - 1;
                if (ende <= Zapfkalender.TAGE) neu.Add(new Ferienfenster(beginn, ende));
                else
                {
                    neu.Add(new Ferienfenster(beginn, Zapfkalender.TAGE));
                    neu.Add(new Ferienfenster(1, ende - Zapfkalender.TAGE));
                }
            }
            return neu;
        }

        private static double Tagesgangabweichung(Bilanzreihe a, Bilanzreihe b)
        {
            double sa = a.JahressummeKwh, sb = b.JahressummeKwh;
            if (!(sa > 0) || !(sb > 0)) return 0.0;
            IReadOnlyList<double> x = a.StundenKwh, y = b.StundenKwh;
            double groesste = 0.0;
            for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
            {
                double ah = 0.0, bh = 0.0;
                for (int d = 0; d < Zapfkalender.TAGE; d++)
                {
                    ah += x[d * Zapfkalender.STUNDEN_TAG + h];
                    bh += y[d * Zapfkalender.STUNDEN_TAG + h];
                }
                double diff = Math.Abs(ah / sa - bh / sb);
                if (diff > groesste) groesste = diff;
            }
            return groesste;
        }

        private static ZapfprofilEingabeException Fehler(string zone, string text)
            => new ZapfprofilEingabeException(ZapfEingabefehler.StochastikUngueltig, zone, text);
    }
}
