using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die LAMBERT-Koordinaten des DWD — Rechtswert und Hochwert einer
    /// Testreferenzjahr-Datei — in geographische Länge und Breite und zurück
    /// (Auftrag KL-2).
    ///
    /// <para><b>Die Projektion.</b> Der Kopf einer DWD-TRY-Datei führt den Standort
    /// als <c>Rechtswert</c>/<c>Hochwert</c> in <b>ETRS89 / LCC Europa</b>
    /// (<c>EPSG:3034</c>): Lambert konform konisch mit zwei Standardparallelen
    /// 35° N und 65° N, Ursprung 52° N / 10° O, falschem Ostwert 4 000 000 m,
    /// falschem Nordwert 2 800 000 m auf dem Ellipsoid GRS80
    /// (<c>a = 6 378 137 m</c>, <c>1/f = 298,257222101</c>). Die Parameter stehen so
    /// im EPSG-Register.</para>
    ///
    /// <para><b>Nachgemessen, nicht angenommen.</b> Die offenen TRY-Regionaldaten
    /// (<c>RE-Lab-Projects/TRY_DE_2015_2045</c>, <c>data.zip</c> v1.4.0) tragen
    /// Breite und Länge ihrer fünfzehn Regionsmittelpunkte IM DATEINAMEN
    /// (<c>TRY2015_&lt;Breite·1e4&gt;&lt;Länge·1e4&gt;_Jahr.dat</c>) und Rechts-/Hochwert
    /// IM KOPF. Über alle fünfzehn Regionen — von 47,49° N / 11,10° O bis
    /// 54,09° N / 12,14° O — weicht die hier gerechnete Länge/Breite um höchstens
    /// <b>0,00005°</b> vom Dateinamen ab; der Rest ist die Rundung der Kopfwerte auf
    /// das 500-m-Raster. Die Fälle stehen als feste Prüfpunkte in
    /// <c>LambertKoordinatenTests</c>.</para>
    ///
    /// <para><b>Die Formeln</b> sind die ellipsoidischen Gleichungen von Snyder,
    /// <i>Map Projections — A Working Manual</i>, USGS Professional Paper 1395 (1987),
    /// Abschnitt 15 „Lambert Conformal Conic“: (14-15) und (15-1) für die Hilfsgrößen
    /// <c>m</c> und <c>t</c>, (15-2) für den Kegelparameter <c>n</c>, (15-3) für den
    /// Maßstab <c>F</c>, (14-4)/(15-7) für die Hinrechnung und (14-9)…(14-11) mit der
    /// Reihenumkehr (7-9) für die Rückrechnung. Gerechnet wird durchweg in
    /// <c>double</c>.</para>
    ///
    /// <para><b>Plausibilitätsgrenzen.</b> Ein Punkt außerhalb
    /// <see cref="BREITE_MIN"/>…<see cref="BREITE_MAX"/> bzw.
    /// <see cref="LAENGE_MIN"/>…<see cref="LAENGE_MAX"/> ist für eine DWD-TRY-Datei
    /// kein Standort, sondern ein Lesefehler. Beide Rechenwege liefern dann
    /// <c>false</c> und <see cref="double.NaN"/> in den Rückgaben — <b>nie eine stille
    /// Null</b>, die als Punkt im Golf von Guinea weiterliefe.</para>
    ///
    /// <para><b>Plattformfrei, ohne Paket, ohne Netz</b> — reine Rechnung.</para>
    /// </summary>
    public static class LambertKoordinaten
    {
        /// <summary>Der EPSG-Schlüssel der Projektion (ETRS89 / LCC Europa).</summary>
        public const int EPSG = 3034;

        /// <summary>Große Halbachse des Ellipsoids GRS80 [m].</summary>
        public const double HALBACHSE = 6378137.0;

        /// <summary>Kehrwert der Abplattung des Ellipsoids GRS80.</summary>
        public const double ABPLATTUNG_KEHRWERT = 298.257222101;

        /// <summary>Erste Standardparallele [Grad].</summary>
        public const double PARALLELE_1 = 35.0;

        /// <summary>Zweite Standardparallele [Grad].</summary>
        public const double PARALLELE_2 = 65.0;

        /// <summary>Breite des Koordinatenursprungs [Grad].</summary>
        public const double URSPRUNG_BREITE = 52.0;

        /// <summary>Länge des Koordinatenursprungs (Bezugsmeridian) [Grad].</summary>
        public const double URSPRUNG_LAENGE = 10.0;

        /// <summary>Falscher Ostwert — der Rechtswert des Ursprungs [m].</summary>
        public const double FALSCHER_OSTWERT = 4000000.0;

        /// <summary>Falscher Nordwert — der Hochwert des Ursprungs [m].</summary>
        public const double FALSCHER_NORDWERT = 2800000.0;

        /// <summary>Kleinste noch plausible Breite eines TRY-Standorts [Grad].</summary>
        public const double BREITE_MIN = 45.0;

        /// <summary>Größte noch plausible Breite eines TRY-Standorts [Grad].</summary>
        public const double BREITE_MAX = 56.0;

        /// <summary>Kleinste noch plausible Länge eines TRY-Standorts [Grad].</summary>
        public const double LAENGE_MIN = 5.0;

        /// <summary>Größte noch plausible Länge eines TRY-Standorts [Grad].</summary>
        public const double LAENGE_MAX = 16.0;

        /// <summary>Numerische Exzentrizität des Ellipsoids.</summary>
        private static readonly double EXZENTRIZITAET;

        /// <summary>Der Kegelparameter n (Snyder 15-2).</summary>
        private static readonly double KEGEL_N;

        /// <summary>Der Maßstabsfaktor F (Snyder 15-3).</summary>
        private static readonly double MASSSTAB_F;

        /// <summary>Der Radius rho zum Ursprungsbreitenkreis (Snyder 15-7a).</summary>
        private static readonly double RADIUS_URSPRUNG;

        /// <summary>Schrittweite, unter der die Reihenumkehr der Breite abbricht [rad].</summary>
        private const double ABBRUCH_RAD = 1e-13;

        /// <summary>Höchstzahl der Schritte der Reihenumkehr — sie endet stets früher.</summary>
        private const int SCHRITTE_MAX = 30;

        static LambertKoordinaten()
        {
            double f = 1.0 / ABPLATTUNG_KEHRWERT;
            EXZENTRIZITAET = Math.Sqrt(2.0 * f - f * f);

            double p1 = Bogen(PARALLELE_1);
            double p2 = Bogen(PARALLELE_2);
            double p0 = Bogen(URSPRUNG_BREITE);

            double m1 = M(p1), m2 = M(p2);
            double t1 = T(p1), t2 = T(p2), t0 = T(p0);

            KEGEL_N = (Math.Log(m1) - Math.Log(m2)) / (Math.Log(t1) - Math.Log(t2));
            MASSSTAB_F = m1 / (KEGEL_N * Math.Pow(t1, KEGEL_N));
            RADIUS_URSPRUNG = HALBACHSE * MASSSTAB_F * Math.Pow(t0, KEGEL_N);
        }

        /// <summary>
        /// Rechnet Rechts- und Hochwert in geographische Länge und Breite um.
        /// </summary>
        /// <param name="rechtswert">Rechtswert (East) [m].</param>
        /// <param name="hochwert">Hochwert (North) [m].</param>
        /// <param name="laenge">Geographische Länge [Grad, Ost positiv];
        /// <see cref="double.NaN"/>, wenn der Punkt unplausibel ist.</param>
        /// <param name="breite">Geographische Breite [Grad, Nord positiv];
        /// <see cref="double.NaN"/>, wenn der Punkt unplausibel ist.</param>
        /// <returns><c>true</c>, wenn der Punkt in den Plausibilitätsgrenzen liegt.</returns>
        public static bool NachGeographisch(double rechtswert, double hochwert,
                                            out double laenge, out double breite)
        {
            laenge = double.NaN;
            breite = double.NaN;

            if (double.IsNaN(rechtswert) || double.IsInfinity(rechtswert) ||
                double.IsNaN(hochwert) || double.IsInfinity(hochwert)) return false;

            double x = rechtswert - FALSCHER_OSTWERT;
            double y = RADIUS_URSPRUNG - (hochwert - FALSCHER_NORDWERT);

            // Snyder 14-10/14-11: rho traegt das Vorzeichen von n, theta zeigt vom
            // Kegelscheitel zum Punkt.
            double rho = Math.Sign(KEGEL_N) * Math.Sqrt(x * x + y * y);
            if (rho == 0.0) return false;               // der Kegelscheitel selbst

            double theta = KEGEL_N > 0 ? Math.Atan2(x, y) : Math.Atan2(-x, -y);

            double t = Math.Pow(rho / (HALBACHSE * MASSSTAB_F), 1.0 / KEGEL_N);

            // Snyder 7-9: Reihenumkehr von t nach phi; der erste Wert ist die
            // konforme Breite, jeder Schritt setzt die Ellipsoidkorrektur nach.
            double phi = Math.PI / 2.0 - 2.0 * Math.Atan(t);
            for (int i = 0; i < SCHRITTE_MAX; i++)
            {
                double s = EXZENTRIZITAET * Math.Sin(phi);
                double neu = Math.PI / 2.0 - 2.0 * Math.Atan(
                    t * Math.Pow((1.0 - s) / (1.0 + s), EXZENTRIZITAET / 2.0));
                double d = neu - phi;
                phi = neu;
                if (Math.Abs(d) < ABBRUCH_RAD) break;
            }

            double lam = theta / KEGEL_N + Bogen(URSPRUNG_LAENGE);

            double l = Grad(lam);
            double b = Grad(phi);
            if (!ImBereich(l, b)) return false;

            laenge = l;
            breite = b;
            return true;
        }

        /// <summary>
        /// Rechnet geographische Länge und Breite in Rechts- und Hochwert um — die
        /// Gegenrichtung zu <see cref="NachGeographisch"/>.
        /// </summary>
        /// <param name="laenge">Geographische Länge [Grad, Ost positiv].</param>
        /// <param name="breite">Geographische Breite [Grad, Nord positiv].</param>
        /// <param name="rechtswert">Rechtswert [m]; <see cref="double.NaN"/> außerhalb
        /// der Plausibilitätsgrenzen.</param>
        /// <param name="hochwert">Hochwert [m]; <see cref="double.NaN"/> außerhalb der
        /// Plausibilitätsgrenzen.</param>
        /// <returns><c>true</c>, wenn der Punkt in den Plausibilitätsgrenzen liegt.</returns>
        public static bool NachLambert(double laenge, double breite,
                                       out double rechtswert, out double hochwert)
        {
            rechtswert = double.NaN;
            hochwert = double.NaN;

            if (!ImBereich(laenge, breite)) return false;

            double phi = Bogen(breite);
            double rho = HALBACHSE * MASSSTAB_F * Math.Pow(T(phi), KEGEL_N);
            double theta = KEGEL_N * (Bogen(laenge) - Bogen(URSPRUNG_LAENGE));

            rechtswert = FALSCHER_OSTWERT + rho * Math.Sin(theta);
            hochwert = FALSCHER_NORDWERT + RADIUS_URSPRUNG - rho * Math.Cos(theta);
            return true;
        }

        /// <summary>
        /// Der Zahlenrand der Bereichsprüfung [Grad] — rund 0,1 mm. Ohne ihn fiele ein
        /// Punkt GENAU auf einer Grenze durch, sobald ihn die Hin- und Rückrechnung um
        /// das letzte Bit verschiebt; die Grenze ist eine Plausibilitätsmarke, kein
        /// Genauigkeitstor.
        /// </summary>
        private const double BEREICH_RAND = 1e-9;

        /// <summary>
        /// Liegt der Punkt in den Plausibilitätsgrenzen eines TRY-Standorts?
        /// <c>NaN</c> und Unendlich liegen nie darin.
        /// </summary>
        public static bool ImBereich(double laenge, double breite)
            => laenge >= LAENGE_MIN - BEREICH_RAND && laenge <= LAENGE_MAX + BEREICH_RAND &&
               breite >= BREITE_MIN - BEREICH_RAND && breite <= BREITE_MAX + BEREICH_RAND;

        // =====================================================================
        //  Intern — die Hilfsgrößen von Snyder
        // =====================================================================

        /// <summary>Snyder 14-15: m = cos phi / sqrt(1 − e² sin² phi).</summary>
        private static double M(double phi)
        {
            double s = Math.Sin(phi);
            return Math.Cos(phi) / Math.Sqrt(1.0 - EXZENTRIZITAET * EXZENTRIZITAET * s * s);
        }

        /// <summary>Snyder 15-9a: t = tan(pi/4 − phi/2) / ((1 − e sin phi)/(1 + e sin phi))^(e/2).</summary>
        private static double T(double phi)
        {
            double s = EXZENTRIZITAET * Math.Sin(phi);
            return Math.Tan(Math.PI / 4.0 - phi / 2.0)
                 / Math.Pow((1.0 - s) / (1.0 + s), EXZENTRIZITAET / 2.0);
        }

        private static double Bogen(double grad) => grad * Math.PI / 180.0;

        private static double Grad(double bogen) => bogen * 180.0 / Math.PI;
    }
}
