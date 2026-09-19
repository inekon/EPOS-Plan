using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die RASTERPROJEKTION der DWD-Testreferenzjahre — Lambert konform konisch —
    /// und ihre Umkehrung nach WGS84.
    ///
    /// <para><b>Warum es das gibt.</b> Eine TRY-Datei des Deutschen Wetterdienstes
    /// traegt in jeder Datenzeile ihren Rasterpunkt als <c>RW</c>/<c>HW</c> in
    /// Metern. Geographische Koordinaten stehen NICHT darin: Der Kopf nennt nur
    /// „Koordinatensystem : Lambert konform konisch". Der Klimaimport braucht aber
    /// Longitude und Latitude — die Sonnenstandsrechnung
    /// (<see cref="SolarCalculator.CalculateHourly"/>) kennt nichts anderes.</para>
    ///
    /// <para><b>Das Ergebnis ist ein VORSCHLAG, keine Zusage.</b> Die Datei nennt das
    /// System, nicht seine Parameter; die hier gesetzten sind die der
    /// DWD-Rasterdaten (Ellipsoid GRS80, Standardparallelen 48° N und 53° N,
    /// Ursprung 51° N / 10,5° O, Rechtswert-Zuschlag 4 000 000 m, Hochwert-Zuschlag
    /// 2 800 000 m). Der Anwender sieht die umgerechneten Koordinaten im
    /// Klimadaten-Dialog und kann sie dort aendern, bevor er einliest. Faellt
    /// <see cref="InDeutschland"/> durch, wird gar nichts vorbelegt.</para>
    ///
    /// <para><b>Die Rechnung ist die Lehrbuchform der zweiparalleligen
    /// Lambert-Projektion auf dem Ellipsoid</b> (Snyder, „Map Projections — A Working
    /// Manual", Abschnitt 15). Hin- und Rueckweg stehen beide hier, weil nur so die
    /// Rundprobe moeglich ist: Wer <c>rw</c>/<c>hw</c> umrechnet und das Ergebnis
    /// zurueckrechnet, muss auf denselben Punkt kommen (Pruefzusage: unter einem
    /// Meter).</para>
    /// </summary>
    public static class LambertDwd
    {
        /// <summary>Grosse Halbachse des Ellipsoids GRS80 [m].</summary>
        private const double A_GRS80 = 6378137.0;

        /// <summary>Abplattung des Ellipsoids GRS80.</summary>
        private const double F_GRS80 = 1.0 / 298.257222101;

        /// <summary>Erste Standardparallele [Grad].</summary>
        public const double PARALLELE_1 = 48.0;

        /// <summary>Zweite Standardparallele [Grad].</summary>
        public const double PARALLELE_2 = 53.0;

        /// <summary>Breite des Projektionsursprungs [Grad].</summary>
        public const double URSPRUNG_BREITE = 51.0;

        /// <summary>Laenge des Projektionsursprungs [Grad].</summary>
        public const double URSPRUNG_LAENGE = 10.5;

        /// <summary>Zuschlag auf den Rechtswert („False Easting") [m].</summary>
        public const double RECHTSWERT_ZUSCHLAG = 4000000.0;

        /// <summary>Zuschlag auf den Hochwert („False Northing") [m].</summary>
        public const double HOCHWERT_ZUSCHLAG = 2800000.0;

        private const double GRAD = Math.PI / 180.0;

        /// <summary>Numerische Exzentrizitaet des Ellipsoids.</summary>
        private static readonly double E = Math.Sqrt(F_GRS80 * (2.0 - F_GRS80));

        /// <summary>Kegelkonstante n der Projektion.</summary>
        private static readonly double N = Kegelkonstante();

        /// <summary>Massstabsgroesse F der Projektion.</summary>
        private static readonly double GROSS_F =
            M(PARALLELE_1 * GRAD) / (N * Math.Pow(T(PARALLELE_1 * GRAD), N));

        /// <summary>Radius des Ursprungsbreitenkreises [m].</summary>
        private static readonly double RHO_0 =
            A_GRS80 * GROSS_F * Math.Pow(T(URSPRUNG_BREITE * GRAD), N);

        /// <summary>
        /// Rechnet einen Rasterpunkt der DWD-Projektion nach WGS84 um.
        /// </summary>
        /// <param name="rechtswert">RW der Datenzeile [m].</param>
        /// <param name="hochwert">HW der Datenzeile [m].</param>
        /// <returns>Longitude und Latitude in Grad.</returns>
        public static (double Longitude, double Latitude) NachWgs84(double rechtswert,
                                                                    double hochwert)
        {
            double x = rechtswert - RECHTSWERT_ZUSCHLAG;
            double y = RHO_0 - (hochwert - HOCHWERT_ZUSCHLAG);

            double vorzeichen = N < 0 ? -1.0 : 1.0;
            double rho = vorzeichen * Math.Sqrt(x * x + y * y);
            double theta = Math.Atan2(vorzeichen * x, vorzeichen * y);

            double t = Math.Pow(rho / (A_GRS80 * GROSS_F), 1.0 / N);

            // Snyder (7-9): phi wird iterativ genaehert; nach wenigen Durchlaeufen
            // aendert sich nichts mehr (Abbruch bei 1e-12 rad, rund sechs Mikrometer).
            double phi = Math.PI / 2.0 - 2.0 * Math.Atan(t);
            for (int i = 0; i < 30; i++)
            {
                double sinPhi = Math.Sin(phi);
                double neu = Math.PI / 2.0 - 2.0 * Math.Atan(
                    t * Math.Pow((1.0 - E * sinPhi) / (1.0 + E * sinPhi), E / 2.0));
                if (Math.Abs(neu - phi) < 1e-12) { phi = neu; break; }
                phi = neu;
            }

            double lambda = theta / N + URSPRUNG_LAENGE * GRAD;
            return (lambda / GRAD, phi / GRAD);
        }

        /// <summary>
        /// Der Hinweg — geographische Koordinaten in die DWD-Projektion. Er steht
        /// hier fuer die RUNDPROBE; der Import braucht ihn nicht.
        /// </summary>
        public static (double Rechtswert, double Hochwert) AusWgs84(double longitude,
                                                                    double latitude)
        {
            double phi = latitude * GRAD;
            double rho = A_GRS80 * GROSS_F * Math.Pow(T(phi), N);
            double theta = N * (longitude * GRAD - URSPRUNG_LAENGE * GRAD);

            return (RECHTSWERT_ZUSCHLAG + rho * Math.Sin(theta),
                    HOCHWERT_ZUSCHLAG + RHO_0 - rho * Math.Cos(theta));
        }

        /// <summary>
        /// Liegt der Punkt in Deutschland? 47° bis 55° N, 5,5° bis 15,5° O — der
        /// Rahmen, in dem die TRY-Raster des DWD liegen.
        ///
        /// <para><b>Wozu.</b> Stimmen die Projektionsparameter einer Datei nicht mit
        /// den hier gesetzten ueberein, kommt eine Zahl heraus, die wie eine
        /// Koordinate aussieht. Diese Schranke faengt das ab: Faellt sie durch, wird
        /// NICHTS vorbelegt, und der Anwender traegt die Koordinaten selbst
        /// ein.</para>
        /// </summary>
        public static bool InDeutschland(double longitude, double latitude)
        {
            return latitude >= 47.0 && latitude <= 55.0 &&
                   longitude >= 5.5 && longitude <= 15.5;
        }

        private static double Kegelkonstante()
        {
            double m1 = M(PARALLELE_1 * GRAD), m2 = M(PARALLELE_2 * GRAD);
            double t1 = T(PARALLELE_1 * GRAD), t2 = T(PARALLELE_2 * GRAD);
            return (Math.Log(m1) - Math.Log(m2)) / (Math.Log(t1) - Math.Log(t2));
        }

        /// <summary>Snyder (14-15): m = cos(phi) / sqrt(1 − e² sin²(phi)).</summary>
        private static double M(double phi)
        {
            double s = Math.Sin(phi);
            return Math.Cos(phi) / Math.Sqrt(1.0 - E * E * s * s);
        }

        /// <summary>
        /// Snyder (15-9): t = tan(pi/4 − phi/2) / ((1 − e·sin phi)/(1 + e·sin phi))^(e/2).
        /// </summary>
        private static double T(double phi)
        {
            double s = Math.Sin(phi);
            return Math.Tan(Math.PI / 4.0 - phi / 2.0) /
                   Math.Pow((1.0 - E * s) / (1.0 + E * s), E / 2.0);
        }
    }
}
