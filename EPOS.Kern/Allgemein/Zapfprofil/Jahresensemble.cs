using System;
using System.Collections.Generic;
using System.Globalization;

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
    /// Realisierungen, Toleranz <c>max(1 %, 3 · s_R / √R)</c>, erfüllt ja/nein, der Faktor, der das
    /// Ensemblemittel auf die Jahresmenge des Mengengerüsts bringt (Energieprobe), und die größte
    /// Abweichung der Anteile je Tagesstunde (Formvektor = Erwartungswert).
    /// </summary>
    internal sealed record Jahreskonsistenz(double DeterministischKwh, double MittelKwh, double StandardabweichungKwh,
                                            int Realisierungen, double ToleranzKwh, bool Erfuellt, double Faktor,
                                            double TagesgangAbweichung);

    /// <summary>
    /// <b>Das Ensemble der Jahresreihe</b> („stochastisch", Bilanz; Umsetzungskonzept
    /// Zapfprofilgenerator 4.4 „Zwei Ensembles", 2.3 Satz 3): R Jahre einer Zone, je Realisierung
    /// die Superposition der n_E Einheiten in Minutenauflösung, auf Stunden summiert, und das
    /// Mittel über die Realisierungen.
    ///
    /// <code>
    /// Einheit i:   Q_d,i = Formvektor.Tagesmengen(Q_a / n_E, Zeitstruktur, Kalender_i, f_KW)   Σ_d Q_d,i = Q_a / n_E
    ///              Kalender_i = Kalender der Klimaregion mit den Ferien der Zone um δ_i versetzt,
    ///              δ_i ~ gleichverteilt in [−v; v] (Entkopplung der Urlaube, nur Wohnen), sonst der der Zone
    /// je Tag d:    Ereignisse nach Zapfereignisgenerator mit Q_d,i, Δθ(m(d)), Dichte des Tagtyps
    /// Minute:      E / Dauer je Minute, über Mitternacht in den nächsten Tag (Jahresende → Jahresanfang)
    /// Stunde:      q_h = Σ_{Minuten der Stunde} q_min
    /// Mittel:      q̄_h = Σ_r q_h,r / R      (Folge r = 0, 1, …)
    /// </code>
    ///
    /// <para><b>Nur Bilanz.</b> Das Ensemble ist eine Bilanzreihe und erreicht die Auslegung nie
    /// (Wache <c>ZapfprofilTrennungWacheTests</c>). Rein, deterministisch je Seed; die
    /// Realisierungen laufen wahlweise parallel über <c>Kulturweitergabe.For</c>, summiert wird in
    /// fester Folge — parallel und seriell liefern dieselben Bits.</para>
    /// </summary>
    internal sealed class Jahresensemble
    {
        /// <summary>Kleinste relative Toleranz der Konsistenzprobe (4.4: 1 %) — numerische Setzung des Papiers.</summary>
        internal const double TOLERANZ_RELATIV = 0.01;

        /// <summary>Faktor der Standardabweichung in der Toleranz (4.4: 3 · s_R / √R) — numerische Setzung des Papiers.</summary>
        internal const double TOLERANZ_STREUUNG = 3.0;

        private readonly Bilanzreihe[] _realisierungen;

        private Jahresensemble(long seed, Bilanzreihe[] realisierungen, Bilanzreihe mittel)
        {
            Seed = seed;
            _realisierungen = realisierungen;
            Mittel = mittel;
            double summe = 0.0;
            foreach (Bilanzreihe r in realisierungen) summe += r.JahressummeKwh;
            MittelJahresenergieKwh = summe / realisierungen.Length;
            double q = 0.0;
            foreach (Bilanzreihe r in realisierungen)
            {
                double d = r.JahressummeKwh - MittelJahresenergieKwh;
                q += d * d;
            }
            StandardabweichungKwh = realisierungen.Length > 1 ? Math.Sqrt(q / (realisierungen.Length - 1)) : 0.0;
        }

        internal long Seed { get; }

        /// <summary>Die Zahl der Realisierungen R.</summary>
        internal int Realisierungen => _realisierungen.Length;

        /// <summary>Die Jahresreihe je Realisierung.</summary>
        internal IReadOnlyList<Bilanzreihe> JeRealisierung => Array.AsReadOnly(_realisierungen);

        /// <summary>Das Mittel über die Realisierungen — die Bilanzreihe des Rechenwegs „stochastisch".</summary>
        internal Bilanzreihe Mittel { get; }

        /// <summary>Das Mittel der Jahresenergien der Realisierungen [kWh].</summary>
        internal double MittelJahresenergieKwh { get; }

        /// <summary>Die Standardabweichung s_R der Jahresenergie über die Realisierungen [kWh]; 0 bei R = 1.</summary>
        internal double StandardabweichungKwh { get; }

        /// <summary>
        /// <b>Die Konsistenzprobe</b> gegen die deterministische Reihe derselben Zone (2.4, 4.4):
        /// <c>|Ē − E_det| ≤ max(1 % · E_det, 3 · s_R / √R)</c>; der Faktor <c>E_det / Ē_Mittelreihe</c>
        /// bringt das Mittel auf die Jahresmenge (Energieprobe); die Tagesgangabweichung ist die größte
        /// Differenz der Anteile je Tagesstunde (Summe über das Jahr) — der Formvektor als Erwartungswert.
        /// </summary>
        internal Jahreskonsistenz Pruefen(Bilanzreihe deterministisch)
        {
            if (deterministisch == null) throw new ArgumentNullException(nameof(deterministisch));
            double det = deterministisch.JahressummeKwh;
            double mittel = Mittel.JahressummeKwh;
            double toleranz = Math.Max(TOLERANZ_RELATIV * Math.Abs(det), TOLERANZ_STREUUNG * StandardabweichungKwh / Math.Sqrt(Realisierungen));
            double faktor = mittel > 0 ? det / mittel : (det == 0 ? 1.0 : double.NaN);
            return new Jahreskonsistenz(det, MittelJahresenergieKwh, StandardabweichungKwh, Realisierungen, toleranz,
                Math.Abs(mittel - det) <= toleranz, faktor, Tagesgangabweichung(Mittel, deterministisch));
        }

        // =================================================================================
        // Ziehen
        // =================================================================================

        /// <summary>
        /// <b>Zieht R Jahre einer Zone</b> (Formel oben). Seed je Realisierung, Zone und Einheit
        /// (<see cref="ZapfZufall.Realisierungsseed"/>, <see cref="ZapfZufall.Kindseed"/>); ein
        /// ungültiger Eingang oder R &lt; 1 wird benannt abgelehnt (<see cref="ZapfEingabefehler.StochastikUngueltig"/>).
        /// </summary>
        internal static Jahresensemble Ziehen(Jahreszone z, long seed, int realisierungen, bool parallel = true)
        {
            if (z == null) throw new ArgumentNullException(nameof(z));
            string zone = z.Zone ?? "";
            if (realisierungen < 1 || realisierungen > Zapfensemble.HOECHSTENS)
                throw Fehler(zone, "Nicht rechenbar — die Zahl der Realisierungen der Jahresreihe liegt nicht in 1 … "
                                   + Zapfensemble.HOECHSTENS.ToString(CultureInfo.InvariantCulture) + ".");
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
            var reihen = new Bilanzreihe[realisierungen];
            if (parallel && realisierungen > 1)
                SpeicherEngine.Kulturweitergabe.For(0, realisierungen, null, r => reihen[r] = Realisierung(z, vorbereitung, seed, r));
            else
                for (int r = 0; r < realisierungen; r++) reihen[r] = Realisierung(z, vorbereitung, seed, r);

            // Mittel in fester Folge r = 0, 1, … — unabhängig von der Reihenfolge der Fäden.
            var mittel = new double[Bilanzreihe.STUNDEN];
            foreach (Bilanzreihe r in reihen)
            {
                IReadOnlyList<double> s = r.StundenKwh;
                for (int h = 0; h < Bilanzreihe.STUNDEN; h++) mittel[h] += s[h];
            }
            for (int h = 0; h < Bilanzreihe.STUNDEN; h++) mittel[h] /= realisierungen;
            return new Jahresensemble(seed, reihen, new Bilanzreihe(mittel));
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

        private static Bilanzreihe Realisierung(Jahreszone z, Vorbereitung v, long seed, int r)
        {
            const int minutenJahr = Zapfkalender.TAGE * Bedarfstag.MINUTEN;
            var stunden = new double[Bilanzreihe.STUNDEN];
            var ereignisse = new List<Zapfereignis>();
            ulong zs = ZapfZufall.Kindseed(ZapfZufall.Realisierungsseed(seed, r), z.Index);
            for (int u = 0; u < z.Einheiten; u++)
            {
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
