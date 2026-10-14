using System;

namespace WindowsFormsApplication1
{
    /// <summary>Herkunft der wirksamen Teillastkurve einer Kältemaschine (Fachkonzept Teillast und Takten 3.2, 4.1, 4.4).</summary>
    public enum KaeltemaschinenKurvenherkunft
    {
        /// <summary><c>Teillast_Weg</c> leer: der heutige Weg — linear, Takt ohne Verlust, gleichmäßige Teilung.</summary>
        Bestand,
        /// <summary><c>Teillast_Weg = LINEAR</c> (oder <c>KURVE</c> ohne Beiwerte und ohne Regelung): g = 1 mit Taktverlust.</summary>
        Linear,
        /// <summary><c>Teillast_Weg = KURVE</c> mit gepflegten Beiwerten, die die Plausibilität bestehen.</summary>
        Kurve,
        /// <summary><c>Teillast_Weg = KURVE</c> ohne Beiwerte: die Vorgabekurve der Verdichterregelung.</summary>
        Vorgabekurve,
        /// <summary>Eine Kurve war gewählt, besteht die Plausibilität aber nicht: Rückfall auf linear mit Taktverlust.</summary>
        Verworfen
    }

    /// <summary>Das Ergebnis einer Stunde EINER Kältemaschine auf der Lastachse (ohne Hilfsstrom der Rückkühlung).</summary>
    public readonly struct KaeltemaschinenTeillaststunde
    {
        /// <summary>Gedeckte Kälte Q [kWh].</summary>
        public readonly double KaelteKwh;

        /// <summary>Verdichterstrom P_el [kWh] einschließlich des Mehrstroms aus dem Takten.</summary>
        public readonly double VerdichterKwh;

        /// <summary>Mehrstrom des Taktens [kWh] (Teil von <see cref="VerdichterKwh"/>).</summary>
        public readonly double MehrstromKwh;

        /// <summary>Lastgrad PLR = Q / (Q_av · 1 h).</summary>
        public readonly double Lastgrad;

        /// <summary><c>true</c> = die Stunde taktet (0 &lt; Q &lt; P_min).</summary>
        public readonly bool Takt;

        /// <summary>Starts der Taktstunde (<see cref="Waermepumpentakt.StartsImTakt"/>); 0 ohne Takt.</summary>
        public readonly int Starts;

        /// <summary>Konstruktor.</summary>
        public KaeltemaschinenTeillaststunde(double kaelte, double verdichter, double mehrstrom, double lastgrad, bool takt, int starts)
        {
            KaelteKwh = kaelte;
            VerdichterKwh = verdichter;
            MehrstromKwh = mehrstrom;
            Lastgrad = lastgrad;
            Takt = takt;
            Starts = starts;
        }

        /// <summary>Wirksamer EER der Stunde Q / P_el; 0 ohne Strom.</summary>
        public double Eer => VerdichterKwh > 0 ? KaelteKwh / VerdichterKwh : 0.0;
    }

    /// <summary>
    /// <b>Die Teillast der Kältemaschine</b> (KM3, Fachkonzept Teillast und Takten 3.2, 3.3, 3.6): bildet aus den acht
    /// Katalogfeldern die wirksame Kurve und rechnet den Verdichterstrom einer Stunde. Ohne Datenbank, ohne Dienste,
    /// deterministisch; ein Objekt je Maschine, unveränderlich.
    ///
    /// <para><b>Lastachse.</b> Die Kurve EIRFPLR(x) = a + b·x + c·x² wird auf Volllast normiert: E(x) = EIRFPLR(x) geteilt
    /// durch EIRFPLR(1). Das Gütemaß ist g(x) = x / E(x), das Verhältnis des EER bei Teillast zum EER bei Volllast.
    /// Unterhalb der Kurvengültigkeit x_u bleibt das Verhältnis an x_u stehen. Der Strom über der Mindestteillast ist
    /// Volllaststrom (Q_av durch EER_KF) mal E(max(PLR, x_u)) mal PLR durch max(PLR, x_u).</para>
    ///
    /// <para><b>Takten.</b> Unter der kleinsten Dauerleistung P_min ist der Strom ohne Taktverlust die Kälte durch
    /// (EER_KF mal g am Mindestpunkt); darauf kommt der Mehrstrom der Hausklasse <see cref="Waermepumpentakt"/> mit C_d
    /// (Vorgabe <see cref="Waermepumpentakt.VORGABE_CD"/>) — dieselbe Formel wie an der Wärmepumpe.</para>
    ///
    /// <para><b>Bestandsweg.</b> <c>Teillast_Weg</c> leer rechnet Zeichen für Zeichen wie heute: Strom = Kälte durch
    /// EER_KF, Takt nur als Kennzeichen, kein Mehrstrom.</para>
    /// </summary>
    public sealed class Kaeltemaschinenteillast
    {
        /// <summary>Herkunft der wirksamen Kurve.</summary>
        public KaeltemaschinenKurvenherkunft Herkunft { get; }

        /// <summary><c>true</c> = der heutige Weg (Teillast_Weg leer).</summary>
        public bool Bestandsweg => Herkunft == KaeltemaschinenKurvenherkunft.Bestand;

        /// <summary><c>true</c> = E(x) = x, g = 1 (Bestand, linear, verworfen).</summary>
        public bool Linear => Herkunft != KaeltemaschinenKurvenherkunft.Kurve && Herkunft != KaeltemaschinenKurvenherkunft.Vorgabekurve;

        /// <summary>Beiwert a der ungenormten Kurve (linear: 0).</summary>
        public double A { get; }

        /// <summary>Beiwert b der ungenormten Kurve (linear: 1).</summary>
        public double B { get; }

        /// <summary>Beiwert c der ungenormten Kurve (linear: 0).</summary>
        public double C { get; }

        /// <summary>Untere Gültigkeit x_u der Kurve: gepflegt, sonst Mindestteillast, sonst 0.</summary>
        public double UntereGueltigkeit { get; }

        /// <summary>Wirksamer Teillastkoeffizient C_d (<see cref="Waermepumpentakt.CdWirksam"/>).</summary>
        public double Cd { get; }

        /// <summary>Wirksamer Randweg (<see cref="KaeltemaschineTeillastSchema.RANDWEG_RANDWERT"/> oder <c>GUETEGRAD</c>).</summary>
        public string Randweg { get; }

        private Kaeltemaschinenteillast(KaeltemaschinenKurvenherkunft herkunft, double a, double b, double c, double xu, double cd, string randweg)
        {
            Herkunft = herkunft;
            A = a;
            B = b;
            C = c;
            UntereGueltigkeit = xu;
            Cd = cd;
            Randweg = randweg;
        }

        /// <summary>
        /// Bildet die wirksame Teillast aus den acht Katalogfeldern und der Mindestteillast (Anteil 0 … 1).
        /// Weg leer oder unbekannt = Bestand; LINEAR = linear mit Taktverlust; KURVE mit drei Beiwerten = diese Kurve,
        /// KURVE ohne Beiwerte = Vorgabekurve der Verdichterregelung, ohne Regelung linear; eine Kurve, die die
        /// Plausibilität (<see cref="KaeltemaschineStammCtrl.KurvePlausibel"/>) nicht besteht oder nur teilweise gepflegt
        /// ist, fällt auf linear zurück (<see cref="KaeltemaschinenKurvenherkunft.Verworfen"/>).
        /// </summary>
        public static Kaeltemaschinenteillast Bilden(string weg, double? a, double? b, double? c, double? lastgradMin,
                                                     double? cd, string verdichterregelung, string randweg, double mindestteillast)
        {
            string w = Normal(weg);
            string rw = Normal(randweg) == KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD
                ? KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD : KaeltemaschineTeillastSchema.RANDWEG_RANDWERT;
            double m = Endlich(mindestteillast) ? Math.Max(0.0, Math.Min(1.0, mindestteillast)) : 0.0;
            double xu = lastgradMin.HasValue && Endlich(lastgradMin.Value)
                ? Math.Max(0.0, Math.Min(1.0, lastgradMin.Value)) : m;
            double cdW = Waermepumpentakt.CdWirksam(cd);

            if (w == KaeltemaschineTeillastSchema.WEG_LINEAR)
                return new Kaeltemaschinenteillast(KaeltemaschinenKurvenherkunft.Linear, 0, 1, 0, xu, cdW, rw);
            if (w != KaeltemaschineTeillastSchema.WEG_KURVE)
                return new Kaeltemaschinenteillast(KaeltemaschinenKurvenherkunft.Bestand, 0, 1, 0, xu, cdW, rw);

            bool alle = a.HasValue && b.HasValue && c.HasValue;
            bool keine = !a.HasValue && !b.HasValue && !c.HasValue;
            KaeltemaschinenKurvenherkunft herkunft;
            double ka, kb, kc;
            if (alle)
            {
                herkunft = KaeltemaschinenKurvenherkunft.Kurve;
                ka = a.Value; kb = b.Value; kc = c.Value;
            }
            else if (keine)
            {
                KaeltemaschineTeillastkurve.Kurve? v = KaeltemaschineTeillastkurve.Vorgabekurve(Normal(verdichterregelung));
                if (!v.HasValue)
                    return new Kaeltemaschinenteillast(KaeltemaschinenKurvenherkunft.Linear, 0, 1, 0, xu, cdW, rw);
                herkunft = KaeltemaschinenKurvenherkunft.Vorgabekurve;
                ka = v.Value.A; kb = v.Value.B; kc = v.Value.C;
            }
            else
            {
                return new Kaeltemaschinenteillast(KaeltemaschinenKurvenherkunft.Verworfen, 0, 1, 0, xu, cdW, rw);
            }

            if (!Endlich(ka) || !Endlich(kb) || !Endlich(kc) || !KaeltemaschineStammCtrl.KurvePlausibel(ka, kb, kc, xu))
                return new Kaeltemaschinenteillast(KaeltemaschinenKurvenherkunft.Verworfen, 0, 1, 0, xu, cdW, rw);
            return new Kaeltemaschinenteillast(herkunft, ka, kb, kc, xu, cdW, rw);
        }

        /// <summary>Bildet die wirksame Teillast aus einem Katalog- oder Projektsatz (Mindestteillast in Prozent).</summary>
        public static Kaeltemaschinenteillast AusModell(KaeltemaschineModel m)
        {
            if (m == null) return Bilden(null, null, null, null, null, null, null, null, 0.0);
            double mt = Math.Max(0.0, Math.Min(100.0, m.Mindestteillast_Prozent ?? 0.0)) / 100.0;
            return Bilden(m.Teillast_Weg, m.Teillastkurve_a, m.Teillastkurve_b, m.Teillastkurve_c, m.Teillastkurve_Lastgrad_Min,
                          m.Taktverlustfaktor_Cd, m.Verdichterregelung, m.Kennfeld_Randweg, mt);
        }

        /// <summary>Die normierte Kurve E(x) = (a + b·x + c·x²) / (a + b + c); linear E(x) = x.</summary>
        public double E(double x)
        {
            if (Linear) return x;
            return (A + B * x + C * x * x) / (A + B + C);
        }

        /// <summary>
        /// Das Gütemaß g(x) = x' / E(x') mit x' = max(x, x_u) — das Verhältnis EER bei Teillast zu EER bei Volllast;
        /// unter x_u bleibt es am Wert bei x_u stehen. Linear 1.
        /// </summary>
        public double G(double x)
        {
            if (Linear) return 1.0;
            double xe = Math.Max(x, UntereGueltigkeit);
            if (!(xe > 0)) return 1.0;
            double e = E(xe);
            return e > 0 ? xe / e : 1.0;
        }

        /// <summary>
        /// Eine Stunde EINER Maschine: Last <paramref name="lastKwh"/>, verfügbare Kälteleistung Q_av
        /// <paramref name="verfuegbarKw"/> und Volllast-EER <paramref name="eerVolllast"/> aus dem Kennfeld,
        /// Nennleistung und Mindestteillast (Anteil) für P_min = min(m · Q_nenn, Q_av).
        /// </summary>
        public KaeltemaschinenTeillaststunde Stunde(double lastKwh, double verfuegbarKw, double eerVolllast,
                                                    double nennleistungKw, double mindestteillast)
        {
            if (!(lastKwh > 0) || !(verfuegbarKw > 0) || !(eerVolllast > 0) || !Endlich(lastKwh))
                return default;
            double q = Math.Min(lastKwh, verfuegbarKw);
            double plr = q / verfuegbarKw;
            double mt = Endlich(mindestteillast) ? Math.Max(0.0, Math.Min(1.0, mindestteillast)) : 0.0;
            double nenn = Endlich(nennleistungKw) && nennleistungKw > 0 ? nennleistungKw : 0.0;
            double pMin = Math.Min(mt * nenn, verfuegbarKw);

            if (Bestandsweg)
            {
                bool taktB = pMin > 0 && q < pMin;
                return new KaeltemaschinenTeillaststunde(q, q / eerVolllast, 0.0, plr, taktB, 0);
            }

            if (Waermepumpentakt.Taktet(q, pMin))
            {
                double plrMin = pMin / verfuegbarKw;
                double strom0 = q / (eerVolllast * G(plrMin));
                double mehr = Waermepumpentakt.Mehrstrom(strom0, q, pMin, Cd);
                return new KaeltemaschinenTeillaststunde(q, strom0 + mehr, mehr, plr, true, Waermepumpentakt.StartsImTakt(q, pMin));
            }

            double strom = q / (eerVolllast * G(plr));
            return new KaeltemaschinenTeillaststunde(q, strom, 0.0, plr, false, 0);
        }

        /// <summary>
        /// Folgeschaltung gleicher Maschinen (Fachkonzept 3.6): es laufen n = ⌈Last / Q_av⌉ Maschinen, mindestens 1,
        /// höchstens <paramref name="anzahl"/>; die laufenden teilen die Last gleichmäßig. Gibt n und die Last je
        /// laufender Maschine zurück (über der Gesamtleistung bleibt der Rest bei der Kaskade). Ohne Last 0 und 0.
        /// </summary>
        public static (int Laufend, double LastJeMaschineKwh) Folgeschaltung(double lastKwh, double verfuegbarKw, int anzahl)
        {
            if (!(lastKwh > 0) || !Endlich(lastKwh)) return (0, 0.0);
            int max = Math.Max(1, anzahl);
            int n = verfuegbarKw > 0 ? (int)Math.Ceiling(lastKwh / verfuegbarKw - 1e-12) : max;
            n = Math.Max(1, Math.Min(max, n));
            return (n, lastKwh / n);
        }

        private static string Normal(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToUpperInvariant();

        private static bool Endlich(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
}
