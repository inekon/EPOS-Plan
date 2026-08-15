using System;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Erdreichmodell der Wärmequelle "Erdreich" (Paket 3, Stufe 1).
    ///
    /// Grundlage: VDI 4640 Blatt 1, Entwurf 2021-12 (Gründruck). Der Entwurfsstand
    /// ist bewusst gewählt (Konzept 13.1, Normstand E13) und in Ergebnissen und
    /// Programmdokumentation als solcher auszuweisen.
    ///
    /// Zwei Quellsysteme:
    ///
    ///   Erdkollektor - Jahresgang nach Kusuda:
    ///       T(z,t) = T_m − A · e^(−z/d) · cos( 2π·(t − t_min)/8760 − z/d )
    ///       d = √(2a/ω),  ω = 2π/8760 h⁻¹,  a = λ / (ρ·c_p)
    ///
    ///   Erdsonde - konstante Quelltemperatur, weil der Jahresgang ab der
    ///   neutralen Zone (10…20 m) abgeklungen ist:
    ///       T = T_m + ΔT_Oberflaeche + grad_geo · max(0, Sondenlänge/2 − 20 m)
    ///
    /// T_m, A und t_min stammen aus dem 8760er-Außentemperaturvektor der
    /// Klimaregion. Amplitude und Phasenlage werden über eine Sinusregression
    /// der zwölf Monatsmittel bestimmt - ausdrücklich NICHT aus den Extrema der
    /// Stundenwerte, die die Amplitude erheblich überschätzen (Konzept 4.5).
    ///
    /// Die Klasse ist bewusst frei von Datenbank- und UI-Abhängigkeiten: der
    /// Außentemperaturvektor wird durchgereicht (Konzept 4.5, "Datenlage
    /// verifiziert"), damit das Modell isoliert prüfbar bleibt. Entzugsleistung
    /// und Regeneration werden nicht modelliert (bewusste Vereinfachung).
    /// </summary>
    public static class ErdreichTemperatur
    {
        // ------------------------------------------------------------------
        // Konstanten
        // ------------------------------------------------------------------

        /// <summary>Quellsystem Erdkollektor (Flachkollektor, WQ_Quellsystem).</summary>
        public const string QUELLSYSTEM_KOLLEKTOR = DbWerte.WQ_QUELLSYSTEM_KOLLEKTOR;

        /// <summary>Quellsystem Erdsonde (WQ_Quellsystem).</summary>
        public const string QUELLSYSTEM_SONDE = DbWerte.WQ_QUELLSYSTEM_SONDE;

        /// <summary>Katalogschlüssel des Vorgabe-Bodentyps.</summary>
        public const string BODENTYP_DEFAULT = DbWerte.BODENTYP_SAND_FEUCHT;

        /// <summary>Vorgabe-Verlegetiefe des Kollektors [m] (Konzept 4.5).</summary>
        public const double TIEFE_DEFAULT = 1.5;

        /// <summary>Oberflächenoffset ΔT_Oberflaeche der Sonde [K] (Konzept 13.1).</summary>
        public const double OBERFLAECHENOFFSET = 1.5;

        /// <summary>Geothermischer Gradient grad_geo [K/m] (Konzept 13.1).</summary>
        public const double GEOTHERM_GRADIENT = 0.03;

        /// <summary>
        /// Untergrenze der neutralen Zone [m]. Bis dahin stammt die Energie nach
        /// VDI 4640 Bl. 1, Abschn. 4.1, "fast ausschließlich aus Sonneneinstrahlung
        /// und Sickerwasser"; der geothermische Wärmestrom wirkt erst darüber.
        /// </summary>
        public const double NEUTRALE_ZONE_M = 20.0;

        /// <summary>Stunden des Simulationsjahres (fest, wie im Rechenkern).</summary>
        public const int STUNDEN_JAHR = 8760;

        /// <summary>Kreisfrequenz des Jahresgangs ω = 2π/8760 [1/h].</summary>
        public const double OMEGA = 2.0 * Math.PI / STUNDEN_JAHR;

        private static readonly int[] TAGE_PRO_MONAT = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Monatskürzel für die Kennwertanzeige ("min 4,2 °C (Feb)").</summary>
        public static readonly string[] MONATSKUERZEL =
        { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };

        // Ersatzwerte, wenn kein brauchbarer Außentemperaturvektor vorliegt:
        // Deutschland-Mittel nach VDI 4640 Bl. 1, Abschn. 4.1 ("etwa 9,5 °C"),
        // typische Amplitude und Minimum in der zweiten Januarhälfte.
        private const double ERSATZ_MITTEL = 9.5;
        private const double ERSATZ_AMPLITUDE = 8.5;
        private const double ERSATZ_STUNDE_MIN = 480.0;

        // Plausibilitätsschranken für den Außentemperaturvektor. Hintergrund:
        // ein 8760er-Array kann auch dann formal vollständig sein, wenn es gar
        // keine Klimadaten enthält - Form_Simulation_Config bildet DBNull auf 0f
        // ab, und SimulationWaermebedarf.Stundentemperatur_aus_DB füllt ein
        // vorbelegtes Array nur so weit, wie Tab_Solar Zeilen hat. Ein Vektor aus
        // Nullen liefe sonst als "echter" Jahresgang mit T_m = 0 °C durch, ohne
        // dass der Dialog den Ersatzwert-Hinweis zeigt.
        /// <summary>Höchstanteil exakter Nullen, ab dem der Vektor als unbrauchbar gilt.</summary>
        private const double NULLANTEIL_MAX = 0.05;

        // Jahresmittel der Außentemperatur bewohnter Regionen; außerhalb dieses
        // Bandes liegt kein plausibler Standort mehr (Ersatzwerte).
        private const double MITTEL_MIN = -10.0;
        private const double MITTEL_MAX = 25.0;

        // Ein nahezu konstanter Gang (A < 1 K) ist physikalisch nur in engen
        // Grenzen plausibel - z. B. ein konstant vorgegebener Quellvektor. Liegt
        // T_m dabei außerhalb dieses engeren Bandes, sind es keine Klimadaten.
        private const double KONSTANT_AMPLITUDE = 1.0;
        private const double KONSTANT_MITTEL_MIN = 0.0;
        private const double KONSTANT_MITTEL_MAX = 20.0;

        // ------------------------------------------------------------------
        // Bodentyp-Katalog nach VDI 4640 Blatt 1, Tabelle 1
        // ------------------------------------------------------------------

        /// <summary>
        /// Kennwerte eines Untergrundtyps. Eingangsgrößen sind λ und ρ·c_p wie in
        /// Tabelle 1 der Norm; die Temperaturleitfähigkeit a wird daraus abgeleitet.
        /// So bleibt der Normbezug nachvollziehbar und eine Normfortschreibung ist
        /// reine Datenpflege (Konzept 13.1).
        /// </summary>
        public class Bodenkennwerte
        {
            /// <summary>Katalogschlüssel, wird in WQ_Bodentyp gespeichert.</summary>
            public string Schluessel;

            /// <summary>
            /// Ressourcenschlüssel des Anzeigenamens (Schicht 2 der Drei-Schichten-Regel:
            /// sprachneutral, ASCII). Der sichtbare Text kommt daraus erst zur Laufzeit.
            /// </summary>
            public string AnzeigeSchluessel;

            /// <summary>
            /// Anzeigename (Spalte "Untergrund" der Normtabelle), lokalisiert.
            /// Wird bei JEDEM Zugriff aufgelöst, nicht bei der Katalog-Initialisierung -
            /// sonst fröre die Sprache auf den Stand des ersten Zugriffs auf
            /// <see cref="Katalog"/> ein. Ist der Schlüssel unbekannt, steht er selbst
            /// da: sichtbar falsch ist besser als leer.
            /// </summary>
            public string Untergrund
            {
                get
                {
                    if (string.IsNullOrEmpty(AnzeigeSchluessel)) return "";
                    string s = MyResource.Resource.ResourceManager.GetString(
                        AnzeigeSchluessel, MyResource.Resource.Culture);
                    return string.IsNullOrEmpty(s) ? AnzeigeSchluessel : s;
                }
            }

            /// <summary>Wärmeleitfähigkeit λ [W/(m·K)] - empfohlener Rechenwert.</summary>
            public double Lambda;

            /// <summary>Volumenbezogene spezifische Wärmekapazität ρ·c_p [MJ/(m³·K)].</summary>
            public double RhoCp;

            /// <summary>Temperaturleitfähigkeit a = λ/(ρ·c_p) [m²/s].</summary>
            public double A_m2s { get { return Lambda / (RhoCp * 1.0e6); } }

            /// <summary>Temperaturleitfähigkeit a [mm²/s] - Anzeigeeinheit der Konzepttabelle.</summary>
            public double A_mm2s { get { return A_m2s * 1.0e6; } }

            /// <summary>
            /// Dämpfungstiefe d = √(2a/ω) [m]. a wird dafür von m²/s in m²/h
            /// umgerechnet (·3600), damit ω in 1/h eingesetzt werden kann und d
            /// in Metern herauskommt.
            /// </summary>
            public double Daempfungstiefe { get { return Math.Sqrt(2.0 * (A_m2s * 3600.0) / OMEGA); } }

            /// <summary>Verbleibender Anteil der Oberflächenamplitude in Tiefe z [0…1].</summary>
            public double Amplitudenanteil(double tiefeM)
            {
                if (tiefeM < 0) tiefeM = 0;
                return Math.Exp(-tiefeM / Daempfungstiefe);
            }
        }

        /// <summary>
        /// Bodentyp-Katalog: exakt die 13 Zeilen aus Konzept 13.1 bzw.
        /// VDI 4640 Blatt 1 (Entwurf 2021-12), Tabelle 1. λ ist der empfohlene
        /// Rechenwert, ρ·c_p der Mittelwert des dort angegebenen Bereichs.
        /// Weitere Gesteinstypen (Dolomit, Basalt, Marmor, Quarzit, Torf) lassen
        /// sich nach demselben Muster ergänzen.
        /// </summary>
        public static readonly Bodenkennwerte[] Katalog =
        {
            // Spalte 1 = Katalogschlüssel (Persistenzwert, DbWerte), Spalte 2 = Ressourcen-
            // schlüssel des Anzeigetexts (Paket 9 / L2; deutscher und englischer Wortlaut
            // stehen in MyResource/Resource[.en-US].resx).
            //   Schlüssel                        Anzeigeschlüssel                       λ      ρ·c_p     -> a [mm²/s]   d [m]
            Neu(DbWerte.BODENTYP_TON_TROCKEN,  "SIMQ_BODENTYP_TON_TROCKEN",         0.5,   1.55),  //   0,32        1,80
            Neu(DbWerte.BODENTYP_TON_NASS,     "SIMQ_BODENTYP_TON_NASS",            1.8,   2.40),  //   0,75        2,74
            Neu(DbWerte.BODENTYP_SAND_TROCKEN, "SIMQ_BODENTYP_SAND_TROCKEN",        0.4,   1.45),  //   0,28        1,66
            Neu(DbWerte.BODENTYP_SAND_FEUCHT,  "SIMQ_BODENTYP_SAND_FEUCHT",         1.4,   1.90),  //   0,74        2,72  (Default)
            Neu(DbWerte.BODENTYP_SAND_NASS,    "SIMQ_BODENTYP_SAND_NASS",           2.4,   2.50),  //   0,96        3,10
            Neu(DbWerte.BODENTYP_KIES_TROCKEN, "SIMQ_BODENTYP_KIES_TROCKEN",        0.4,   1.45),  //   0,28        1,66
            Neu(DbWerte.BODENTYP_KIES_NASS,    "SIMQ_BODENTYP_KIES_NASS",           1.8,   2.40),  //   0,75        2,74
            Neu(DbWerte.BODENTYP_MERGEL_LEHM,  "SIMQ_BODENTYP_MERGEL_LEHM",         2.4,   2.00),  //   1,20        3,47
            Neu(DbWerte.BODENTYP_TONSTEIN,     "SIMQ_BODENTYP_TONSTEIN",            2.2,   2.25),  //   0,98        3,13
            Neu(DbWerte.BODENTYP_SANDSTEIN,    "SIMQ_BODENTYP_SANDSTEIN",           2.8,   2.20),  //   1,27        3,57
            Neu(DbWerte.BODENTYP_KALKSTEIN,    "SIMQ_BODENTYP_KALKSTEIN",           2.7,   2.25),  //   1,20        3,47
            Neu(DbWerte.BODENTYP_GRANIT,       "SIMQ_BODENTYP_GRANIT",              3.2,   2.55),  //   1,25        3,55
            Neu(DbWerte.BODENTYP_GNEIS,        "SIMQ_BODENTYP_GNEIS",               2.9,   2.10)   //   1,38        3,72
        };

        private static Bodenkennwerte Neu(string schluessel, string anzeigeSchluessel, double lambda, double rhoCp)
        {
            Bodenkennwerte b = new Bodenkennwerte();
            b.Schluessel = schluessel;
            b.AnzeigeSchluessel = anzeigeSchluessel;
            b.Lambda = lambda;
            b.RhoCp = rhoCp;
            return b;
        }

        /// <summary>
        /// Liefert die Kennwerte zu einem Katalogschlüssel; unbekannte oder leere
        /// Schlüssel ergeben den Vorgabetyp (Sand, feucht).
        /// </summary>
        public static Bodenkennwerte Bodentyp(string schluessel)
        {
            if (!string.IsNullOrEmpty(schluessel))
            {
                for (int i = 0; i < Katalog.Length; i++)
                    if (string.Equals(Katalog[i].Schluessel, schluessel, StringComparison.OrdinalIgnoreCase))
                        return Katalog[i];
            }
            return Vorgabetyp();
        }

        /// <summary>Vorgabetyp SAND_FEUCHT (Konzept 13.1).</summary>
        public static Bodenkennwerte Vorgabetyp()
        {
            for (int i = 0; i < Katalog.Length; i++)
                if (Katalog[i].Schluessel == BODENTYP_DEFAULT) return Katalog[i];
            return Katalog[0];
        }

        /// <summary>
        /// Anzeigenamen des Katalogs in Katalogreihenfolge (für Dropdowns).
        /// Bei jedem Aufruf neu aufgelöst, also in der zum Aufrufzeitpunkt gültigen
        /// Sprache. Die Reihenfolge ist die des Katalogs und damit der Index, über den
        /// <c>Form_QuelleErdreich</c> liest und schreibt - der Anzeigetext ist NIE
        /// Steuerwert.
        /// </summary>
        public static string[] KatalogAnzeige()
        {
            string[] a = new string[Katalog.Length];
            for (int i = 0; i < Katalog.Length; i++) a[i] = Katalog[i].Untergrund;
            return a;
        }

        /// <summary>Index eines Katalogschlüssels; -1 wenn unbekannt.</summary>
        public static int KatalogIndex(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return -1;
            for (int i = 0; i < Katalog.Length; i++)
                if (string.Equals(Katalog[i].Schluessel, schluessel, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        // ------------------------------------------------------------------
        // Jahresgang der Außentemperatur (T_m, A, t_min)
        // ------------------------------------------------------------------

        /// <summary>Kenngrößen des Jahresgangs der Außentemperatur.</summary>
        public class Jahresgang
        {
            /// <summary>Jahresmittel T_m [°C].</summary>
            public double Mittel;

            /// <summary>Amplitude A [K] des angepassten Jahres-Sinus.</summary>
            public double Amplitude;

            /// <summary>Phasenlage t_min [h] - Stunde des Temperaturminimums.</summary>
            public double StundeMin;

            /// <summary>true, wenn die Werte aus einem echten Klimavektor stammen.</summary>
            public bool AusKlimadaten;
        }

        /// <summary>
        /// Bestimmt T_m, Amplitude und Phasenlage aus dem 8760er-Außentemperatur-
        /// vektor. T_m ist das Jahresmittel; Amplitude und Phase folgen aus einer
        /// Regression des Jahres-Sinus über die zwölf Monatsmittel.
        ///
        /// Modell:  T(t) = T_m − A · cos( ω·(t − t_min) )
        ///               = T_m + a1·cos(ω t) + b1·sin(ω t)
        ///          mit  a1 = −A·cos(ω t_min),  b1 = −A·sin(ω t_min)
        ///
        /// Die Ausgleichsrechnung nutzt als Regressoren nicht cos/sin in der
        /// Monatsmitte, sondern den exakten Mittelwert von cos bzw. sin über das
        /// Monatsintervall. Damit ist die Anpassung für einen reinen Sinus
        /// erwartungstreu; die sonst übliche Monatsmittelung würde die Amplitude
        /// systematisch um rund 1 % unterschätzen.
        ///
        /// Der Vektor wird nicht nur auf seine Länge, sondern auch auf
        /// Plausibilität geprüft (Anteil exakter Nullen, Jahresmittel, nahezu
        /// konstanter Gang). Fällt eine dieser Schranken, gelten die Ersatzwerte
        /// 9,5 °C / 8,5 K und AusKlimadaten bleibt false - der Dialog weist das
        /// mit "(ohne Klimadaten - Ersatzwerte)" aus.
        /// </summary>
        public static Jahresgang AnalysiereJahresgang(float[] aussentemp)
        {
            Jahresgang jg = new Jahresgang();

            if (aussentemp == null || aussentemp.Length < STUNDEN_JAHR)
                return Ersatzwerte(jg);

            // 1. Jahresmittel und Anteil exakter Nullen
            double summe = 0;
            int nullen = 0;
            for (int i = 0; i < STUNDEN_JAHR; i++)
            {
                summe += aussentemp[i];
                if (aussentemp[i] == 0f) nullen++;
            }
            jg.Mittel = summe / STUNDEN_JAHR;

            // Ein nennenswerter Anteil exakter Nullen bedeutet in der Praxis ein
            // nur teilweise befülltes oder gar nicht befülltes Array (DBNull → 0f,
            // zu wenige Tab_Solar-Zeilen). Gemessene Stundenwerte treffen die
            // 0,0 °C nie so häufig.
            if (nullen > NULLANTEIL_MAX * STUNDEN_JAHR) return Ersatzwerte(jg);

            // Jahresmittel außerhalb jedes bewohnbaren Standorts
            if (jg.Mittel < MITTEL_MIN || jg.Mittel > MITTEL_MAX) return Ersatzwerte(jg);

            // 2. Monatsmittel
            double[] monatsmittel = new double[12];
            int[] monatsstart = new int[13];
            int index = 0;
            for (int m = 0; m < 12; m++)
            {
                monatsstart[m] = index;
                int stunden = TAGE_PRO_MONAT[m] * 24;
                double s = 0;
                for (int h = 0; h < stunden; h++) s += aussentemp[index + h];
                monatsmittel[m] = s / stunden;
                index += stunden;
            }
            monatsstart[12] = index; // 8760

            // 3. Ausgleichsrechnung der Residuen gegen cos/sin (2x2-Normalgleichung)
            double saa = 0, sab = 0, sbb = 0, sra = 0, srb = 0;
            for (int m = 0; m < 12; m++)
            {
                double t0 = monatsstart[m];
                double t1 = monatsstart[m + 1];
                double dt = t1 - t0;

                // exakte Monatsmittel der Regressoren
                double ca = (Math.Sin(OMEGA * t1) - Math.Sin(OMEGA * t0)) / (OMEGA * dt);
                double sa = (Math.Cos(OMEGA * t0) - Math.Cos(OMEGA * t1)) / (OMEGA * dt);
                double r = monatsmittel[m] - jg.Mittel;

                saa += ca * ca;
                sab += ca * sa;
                sbb += sa * sa;
                sra += r * ca;
                srb += r * sa;
            }

            double det = saa * sbb - sab * sab;
            if (Math.Abs(det) < 1e-12)
            {
                jg.Amplitude = 0;
                jg.StundeMin = 0;
            }
            else
            {
                double a1 = (sra * sbb - srb * sab) / det;
                double b1 = (srb * saa - sra * sab) / det;

                jg.Amplitude = Math.Sqrt(a1 * a1 + b1 * b1);

                double phase = Math.Atan2(-b1, -a1);      // = ω · t_min
                if (phase < 0) phase += 2.0 * Math.PI;
                jg.StundeMin = phase / OMEGA;
            }

            // 4. Nahezu konstanter Gang: als bewusst gesetzter Konstantvektor
            //    plausibel (z. B. durchgehend 12 °C), als Rest eines nicht
            //    befüllten Arrays dagegen nicht. Entscheidend ist das Niveau.
            if (jg.Amplitude < KONSTANT_AMPLITUDE &&
                (jg.Mittel < KONSTANT_MITTEL_MIN || jg.Mittel > KONSTANT_MITTEL_MAX))
                return Ersatzwerte(jg);

            jg.AusKlimadaten = true;
            return jg;
        }

        /// <summary>
        /// Belegt den Jahresgang mit den Ersatzwerten (Deutschland-Mittel nach
        /// VDI 4640 Bl. 1, Abschn. 4.1) und löscht AusKlimadaten.
        /// </summary>
        private static Jahresgang Ersatzwerte(Jahresgang jg)
        {
            jg.Mittel = ERSATZ_MITTEL;
            jg.Amplitude = ERSATZ_AMPLITUDE;
            jg.StundeMin = ERSATZ_STUNDE_MIN;
            jg.AusKlimadaten = false;
            return jg;
        }

        // ------------------------------------------------------------------
        // Erdkollektor - Kusuda
        // ------------------------------------------------------------------

        /// <summary>
        /// Jahresprofil (8760 Stundenwerte) der Quelltemperatur eines
        /// Erdkollektors in der Verlegetiefe <paramref name="tiefeM"/>.
        /// </summary>
        /// <param name="aussentemp8760">Außentemperatur der Klimaregion [°C]</param>
        /// <param name="tiefeM">Verlegetiefe z [m]; ≤ 0 ergibt die Vorgabetiefe 1,5 m</param>
        /// <param name="bodentyp">Katalogschlüssel; unbekannt ergibt SAND_FEUCHT</param>
        public static float[] JahresprofilKollektor(float[] aussentemp8760, double tiefeM, string bodentyp)
        {
            if (tiefeM <= 0) tiefeM = TIEFE_DEFAULT;

            Jahresgang jg = AnalysiereJahresgang(aussentemp8760);
            Bodenkennwerte boden = Bodentyp(bodentyp);

            double d = boden.Daempfungstiefe;
            double daempfung = Math.Exp(-tiefeM / d);
            double phasenversatz = tiefeM / d;            // [rad]

            float[] profil = new float[STUNDEN_JAHR];
            for (int t = 0; t < STUNDEN_JAHR; t++)
            {
                double arg = OMEGA * (t - jg.StundeMin) - phasenversatz;
                profil[t] = (float)(jg.Mittel - jg.Amplitude * daempfung * Math.Cos(arg));
            }
            return profil;
        }

        // ------------------------------------------------------------------
        // Erdsonde - konstante Quelltemperatur
        // ------------------------------------------------------------------

        /// <summary>
        /// Konstante Quelltemperatur einer Erdsonde [°C] nach Konzept 13.1:
        ///
        ///   T = T_m + ΔT_Oberflaeche + grad_geo · max(0, Sondenlänge/2 − 20 m)
        ///
        /// Maßgeblich ist die mittlere Tiefe (Länge/2); der Abzug von 20 m bildet
        /// die neutrale Zone ab, unterhalb derer der geothermische Wärmestrom
        /// überhaupt erst spürbar wird (VDI 4640 Bl. 1, Abschn. 4.1).
        /// Beispiele bei T_m = 9,5 °C: 50 m → 11,15 °C, 100 m → 11,9 °C.
        /// </summary>
        public static double SondenTemperatur(float[] aussentemp8760, double sondenlaengeM)
        {
            Jahresgang jg = AnalysiereJahresgang(aussentemp8760);
            return SondenTemperatur(jg.Mittel, sondenlaengeM);
        }

        /// <summary>Sondentemperatur aus einem bereits bekannten Jahresmittel.</summary>
        public static double SondenTemperatur(double jahresmittel, double sondenlaengeM)
        {
            if (sondenlaengeM < 0) sondenlaengeM = 0;
            double mittlereTiefe = sondenlaengeM / 2.0;
            double geothermisch = GEOTHERM_GRADIENT * Math.Max(0, mittlereTiefe - NEUTRALE_ZONE_M);
            return jahresmittel + OBERFLAECHENOFFSET + geothermisch;
        }

        /// <summary>Konstantes Jahresprofil (8760 Werte) - Quellprofil der Erdsonde.</summary>
        public static float[] JahresprofilSonde(float[] aussentemp8760, double sondenlaengeM)
        {
            float t = (float)SondenTemperatur(aussentemp8760, sondenlaengeM);
            float[] profil = new float[STUNDEN_JAHR];
            for (int i = 0; i < STUNDEN_JAHR; i++) profil[i] = t;
            return profil;
        }

        // ------------------------------------------------------------------
        // Kennwerte für die Dialog-Vorschau
        // ------------------------------------------------------------------

        /// <summary>Minimum, Maximum und Mittel eines Jahresprofils samt Monat der Extrema.</summary>
        public class Kennwerte
        {
            public double Min;
            public double Max;
            public double Mittel;
            public int MonatMin;   // 0…11
            public int MonatMax;   // 0…11

            /// <summary>
            /// Anzeigezeile im Stil des Konzept-Mockups 4.5.
            /// Die Formatangabe "F1" kommt aus dem Quelltext (Lesehinweis des
            /// Ressourcenkatalogs); der Katalogeintrag führt die Platzhalter
            /// normalisiert als {0}…{4}. Deshalb werden die Zahlen VOR dem Einsetzen
            /// formatiert. Die Monatskürzel bleiben deutsch - für sie gibt es keinen
            /// Katalogeintrag (Monatsnamen sind im Katalog ausdrücklich ausgenommen).
            /// </summary>
            public string Zeile()
            {
                return string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMQ_PROFIL_KENNWERTE_ZEILE,
                    Min.ToString("F1", CultureInfo.CurrentCulture), MONATSKUERZEL[MonatMin],
                    Max.ToString("F1", CultureInfo.CurrentCulture), MONATSKUERZEL[MonatMax],
                    Mittel.ToString("F1", CultureInfo.CurrentCulture));
            }
        }

        /// <summary>Ermittelt die Kennwerte eines 8760er-Profils.</summary>
        public static Kennwerte ProfilKennwerte(float[] profil)
        {
            Kennwerte k = new Kennwerte();
            if (profil == null || profil.Length == 0) return k;

            double min = double.MaxValue, max = double.MinValue, summe = 0;
            int iMin = 0, iMax = 0;
            for (int i = 0; i < profil.Length; i++)
            {
                if (profil[i] < min) { min = profil[i]; iMin = i; }
                if (profil[i] > max) { max = profil[i]; iMax = i; }
                summe += profil[i];
            }

            k.Min = min;
            k.Max = max;
            k.Mittel = summe / profil.Length;
            k.MonatMin = MonatAusStunde(iMin);
            k.MonatMax = MonatAusStunde(iMax);
            return k;
        }

        /// <summary>Monatsindex (0…11) einer Jahresstunde.</summary>
        public static int MonatAusStunde(int stunde)
        {
            int grenze = 0;
            for (int m = 0; m < 12; m++)
            {
                grenze += TAGE_PRO_MONAT[m] * 24;
                if (stunde < grenze) return m;
            }
            return 11;
        }

        // ------------------------------------------------------------------
        // Selbsttest - ausschließlich im Debug-Build (kein Testcode im Release)
        // ------------------------------------------------------------------
#if DEBUG

        /// <summary>
        /// Rechnet die Validierungsangaben aus Konzept 13.1 nach und liefert das
        /// Ergebnis als Protokolltext. Wird nicht automatisch aufgerufen; die
        /// Zahlen sind im Umsetzungsprotokoll Paket 3 festgehalten. Nur im
        /// Debug-Build vorhanden (kein Testcode im Release-Assembly).
        ///
        /// ZUGESICHERT wird (jede Verletzung setzt das Gesamtergebnis auf
        /// FEHLGESCHLAGEN):
        ///   1. a und d als Stichprobe gegen die Konzepttabelle: SAND_FEUCHT
        ///      a = 0,7368 mm²/s / d = 2,7199 m, GNEIS a = 1,3810 / d = 3,7233
        ///      (Toleranz 1e-4 bzw. 1 mm - die Konzeptangaben sind gerundet)
        ///   2. Amplitudenrest in 10 m ≤ 7 % für alle 13 Katalogtypen
        ///   3. a = 4,17e-7 m²/s → d ≈ 2,05 m, Phase in 6,4 m ≈ 182 d
        ///   4. Sondenformel: 50 m → 11,15 °C, 100 m → 11,90 °C bei T_m = 9,5 °C
        ///   5. Rückgewinnung von T_m, A und t_min aus einem synthetischen Jahresgang
        ///      sowie Störfestigkeit gegen Tagesgang und Rauschen
        ///   6. Amplitude des Kollektorprofils = A · e^(−z/d)
        ///   7. Plausibilitätsschranke: Nullvektor, ab h 4000 genullter Vektor und
        ///      unplausibles Jahresmittel fallen auf die Ersatzwerte zurück, ein
        ///      konstanter Vektor mit 12 °C bleibt gültig
        ///
        /// Nur AUSGEGEBEN, nicht zugesichert, werden a und d der übrigen elf
        /// Katalogtypen sowie die Amplitudenanteile in 1,5 m und 4 m.
        /// </summary>
        public static string Selbsttest()
        {
            StringBuilder sb = new StringBuilder();
            CultureInfo ci = CultureInfo.InvariantCulture;
            bool allesOk = true;

            sb.AppendLine("Selbsttest ErdreichTemperatur (VDI 4640 Bl. 1, Entwurf 2021-12)");
            sb.AppendLine();

            // --- 1. Katalog ------------------------------------------------
            sb.AppendLine("1. Bodentyp-Katalog: a = lambda/(rho*cp), d = sqrt(2a/omega)");
            sb.AppendLine("   Schluessel        lambda   rho*cp   a[mm2/s]   d[m]    A(1,5m)  A(4m)   A(10m)");
            for (int i = 0; i < Katalog.Length; i++)
            {
                Bodenkennwerte b = Katalog[i];
                sb.AppendLine(string.Format(ci,
                    "   {0,-15} {1,6:F1} {2,8:F2} {3,9:F2} {4,7:F2} {5,8:P0} {6,7:P0} {7,7:P1}",
                    b.Schluessel, b.Lambda, b.RhoCp, b.A_mm2s, b.Daempfungstiefe,
                    b.Amplitudenanteil(1.5), b.Amplitudenanteil(4.0), b.Amplitudenanteil(10.0)));

                if (b.Amplitudenanteil(10.0) > 0.07)
                {
                    sb.AppendLine("   FEHLER: Amplitudenrest in 10 m ueber 7 % bei " + b.Schluessel);
                    allesOk = false;
                }
            }

            // Stichproben-Asserts gegen die Konzepttabelle 13.1. Ohne sie faellt
            // ein Zahlendreher in lambda oder rho*cp nicht auf (Befund der Review).
            // Die Konzeptangaben sind auf vier Nachkommastellen gerundet, deshalb
            // 1 mm Toleranz auf d.
            allesOk &= KatalogProbe(sb, ci, "SAND_FEUCHT", 0.7368, 2.7199);
            allesOk &= KatalogProbe(sb, ci, "GNEIS", 1.3810, 3.7233);
            sb.AppendLine();

            // --- 2. Referenz a = 4,17e-7 m²/s ------------------------------
            double aRef = 4.17e-7;                       // m²/s
            double dRef = Math.Sqrt(2.0 * (aRef * 3600.0) / OMEGA);
            double zRef = 6.4;                           // m
            double phasenStunden = (zRef / dRef) / OMEGA;
            double phasenTage = phasenStunden / 24.0;
            double phasenMonate = phasenTage / 30.4375;

            sb.AppendLine("2. Referenz a = 4,17e-7 m2/s (Fachliteratur, nicht in die Modellbildung eingeflossen)");
            sb.AppendLine(string.Format(ci, "   d              = {0:F3} m      (Konzept: 2,05 m)", dRef));
            sb.AppendLine(string.Format(ci, "   Phase in 6,4 m = {0:F0} h = {1:F1} d = {2:F2} Monate  (Konzept: 182 d = 6,0 Monate)",
                phasenStunden, phasenTage, phasenMonate));
            if (Math.Abs(dRef - 2.05) > 0.01) { sb.AppendLine("   FEHLER: d weicht ab"); allesOk = false; }
            if (Math.Abs(phasenTage - 182.0) > 2.0) { sb.AppendLine("   FEHLER: Phasenverschiebung weicht ab"); allesOk = false; }
            sb.AppendLine();

            // --- 3. Sondenformel -------------------------------------------
            double t50 = SondenTemperatur(9.5, 50);
            double t100 = SondenTemperatur(9.5, 100);
            double t40 = SondenTemperatur(9.5, 40);
            sb.AppendLine("3. Erdsonde bei T_m = 9,5 C");
            sb.AppendLine(string.Format(ci, "    40 m -> {0:F2} C   (kein geothermischer Anteil, mittlere Tiefe = 20 m)", t40));
            sb.AppendLine(string.Format(ci, "    50 m -> {0:F2} C   (Konzept: 11,15 C)", t50));
            sb.AppendLine(string.Format(ci, "   100 m -> {0:F2} C   (Konzept: 11,90 C)", t100));
            if (Math.Abs(t50 - 11.15) > 0.005) { sb.AppendLine("   FEHLER: 50-m-Sonde"); allesOk = false; }
            if (Math.Abs(t100 - 11.90) > 0.005) { sb.AppendLine("   FEHLER: 100-m-Sonde"); allesOk = false; }
            sb.AppendLine();

            // --- 4. Rueckgewinnung des Jahresgangs --------------------------
            // Synthetischer Jahresgang: T_m = 9,5 C, A = 9,0 K, Minimum am 20.01.
            double sollMittel = 9.5, sollAmplitude = 9.0, sollTmin = 480.0;
            float[] synth = new float[STUNDEN_JAHR];
            for (int t = 0; t < STUNDEN_JAHR; t++)
                synth[t] = (float)(sollMittel - sollAmplitude * Math.Cos(OMEGA * (t - sollTmin)));

            Jahresgang jg = AnalysiereJahresgang(synth);
            sb.AppendLine("4. Rueckgewinnung aus synthetischem Jahresgang (T_m 9,5 C, A 9,0 K, t_min 480 h)");
            sb.AppendLine(string.Format(ci, "   T_m   = {0:F3} C", jg.Mittel));
            sb.AppendLine(string.Format(ci, "   A     = {0:F3} K", jg.Amplitude));
            sb.AppendLine(string.Format(ci, "   t_min = {0:F1} h", jg.StundeMin));
            if (Math.Abs(jg.Mittel - sollMittel) > 0.02) { sb.AppendLine("   FEHLER: T_m"); allesOk = false; }
            if (Math.Abs(jg.Amplitude - sollAmplitude) > 0.05) { sb.AppendLine("   FEHLER: Amplitude"); allesOk = false; }
            if (Math.Abs(jg.StundeMin - sollTmin) > 5.0) { sb.AppendLine("   FEHLER: t_min"); allesOk = false; }
            sb.AppendLine();

            // Gegenprobe: Extrema der Stundenwerte ueberschaetzen die Amplitude.
            // Dazu wird dem synthetischen Gang ein Tagesgang + Rauschen ueberlagert.
            float[] gestoert = new float[STUNDEN_JAHR];
            Random rnd = new Random(4640);
            for (int t = 0; t < STUNDEN_JAHR; t++)
                gestoert[t] = (float)(synth[t] + 4.0 * Math.Sin(2.0 * Math.PI * (t % 24) / 24.0)
                                      + 2.0 * (rnd.NextDouble() - 0.5));
            Jahresgang jgG = AnalysiereJahresgang(gestoert);
            double extremAmplitude = 0;
            {
                double mn = double.MaxValue, mx = double.MinValue;
                for (int t = 0; t < STUNDEN_JAHR; t++) { if (gestoert[t] < mn) mn = gestoert[t]; if (gestoert[t] > mx) mx = gestoert[t]; }
                extremAmplitude = (mx - mn) / 2.0;
            }
            sb.AppendLine("5. Gegenprobe mit Tagesgang und Rauschen (Konzept 4.5: nicht aus Extrema rechnen)");
            sb.AppendLine(string.Format(ci, "   Regression ueber Monatsmittel : A = {0:F2} K", jgG.Amplitude));
            sb.AppendLine(string.Format(ci, "   (Max-Min)/2 der Stundenwerte  : A = {0:F2} K  -> Ueberschaetzung um {1:P0}",
                extremAmplitude, extremAmplitude / jgG.Amplitude - 1.0));
            if (Math.Abs(jgG.Amplitude - sollAmplitude) > 0.1) { sb.AppendLine("   FEHLER: Regression stoeranfaellig"); allesOk = false; }
            sb.AppendLine();

            // --- 5. Kollektorprofil ----------------------------------------
            float[] profil = JahresprofilKollektor(synth, 1.5, BODENTYP_DEFAULT);
            Kennwerte k = ProfilKennwerte(profil);
            Bodenkennwerte sand = Bodentyp(BODENTYP_DEFAULT);
            double erwarteteAmplitude = sollAmplitude * sand.Amplitudenanteil(1.5);
            sb.AppendLine("6. Kollektorprofil 1,5 m, Sand feucht, aus dem synthetischen Jahresgang");
            sb.AppendLine(string.Format(ci, "   min {0:F2} C ({1})  max {2:F2} C ({3})  Mittel {4:F2} C",
                k.Min, MONATSKUERZEL[k.MonatMin], k.Max, MONATSKUERZEL[k.MonatMax], k.Mittel));
            sb.AppendLine(string.Format(ci, "   Amplitude {0:F2} K, erwartet {1:F2} K (= 9,0 K * {2:P1})",
                (k.Max - k.Min) / 2.0, erwarteteAmplitude, sand.Amplitudenanteil(1.5)));
            if (Math.Abs((k.Max - k.Min) / 2.0 - erwarteteAmplitude) > 0.05) { sb.AppendLine("   FEHLER: Kollektoramplitude"); allesOk = false; }
            sb.AppendLine();

            // --- 6. Plausibilitaetsschranke ---------------------------------
            sb.AppendLine("7. Plausibilitaetsschranke des Aussentemperaturvektors");

            float[] nullvektor = new float[STUNDEN_JAHR];                    // 8760 x 0,0
            allesOk &= PlausibilitaetsProbe(sb, ci, "8760 x 0,0 C", nullvektor, false);

            float[] teilbefuellt = new float[STUNDEN_JAHR];                  // ab h 4000 genullt
            Array.Copy(synth, teilbefuellt, 4000);
            allesOk &= PlausibilitaetsProbe(sb, ci, "ab h 4000 genullt", teilbefuellt, false);

            float[] konstant = new float[STUNDEN_JAHR];                      // durchgehend 12 C
            for (int t = 0; t < STUNDEN_JAHR; t++) konstant[t] = 12.0f;
            allesOk &= PlausibilitaetsProbe(sb, ci, "konstant 12,0 C", konstant, true);

            float[] zuKalt = new float[STUNDEN_JAHR];                        // T_m = -30 C
            for (int t = 0; t < STUNDEN_JAHR; t++) zuKalt[t] = (float)(-30.0 - 5.0 * Math.Cos(OMEGA * (t - 480.0)));
            allesOk &= PlausibilitaetsProbe(sb, ci, "T_m = -30 C", zuKalt, false);

            allesOk &= PlausibilitaetsProbe(sb, ci, "echter Jahresgang", synth, true);
            sb.AppendLine();

            sb.AppendLine(allesOk ? "ERGEBNIS: alle Pruefungen bestanden." : "ERGEBNIS: mindestens eine Pruefung FEHLGESCHLAGEN.");
            return sb.ToString();
        }

        /// <summary>
        /// Stichprobe eines Katalogeintrags gegen die Konzepttabelle 13.1
        /// (a in mm²/s, d in m). Liefert false bei Abweichung.
        /// </summary>
        private static bool KatalogProbe(StringBuilder sb, CultureInfo ci, string schluessel,
                                         double sollA, double sollD)
        {
            Bodenkennwerte b = Bodentyp(schluessel);
            bool ok = Math.Abs(b.A_mm2s - sollA) <= 1e-4 && Math.Abs(b.Daempfungstiefe - sollD) <= 1e-3;
            sb.AppendLine(string.Format(ci, "   Probe {0,-12} a = {1:F5} (soll {2:F4})   d = {3:F5} (soll {4:F4}){5}",
                schluessel, b.A_mm2s, sollA, b.Daempfungstiefe, sollD, ok ? "" : "   FEHLER"));
            return ok;
        }

        /// <summary>
        /// Prüft, ob ein Vektor von der Plausibilitätsschranke angenommen
        /// (erwartetAusKlimadaten = true) oder auf die Ersatzwerte zurückgeführt
        /// wird. Liefert false, wenn das Verhalten abweicht.
        /// </summary>
        private static bool PlausibilitaetsProbe(StringBuilder sb, CultureInfo ci, string bezeichnung,
                                                 float[] vektor, bool erwartetAusKlimadaten)
        {
            Jahresgang j = AnalysiereJahresgang(vektor);
            bool ok = j.AusKlimadaten == erwartetAusKlimadaten;
            if (ok && !erwartetAusKlimadaten)
                ok = Math.Abs(j.Mittel - ERSATZ_MITTEL) < 1e-9 && Math.Abs(j.Amplitude - ERSATZ_AMPLITUDE) < 1e-9;

            sb.AppendLine(string.Format(ci, "   {0,-20} -> T_m {1,6:F2} C  A {2,5:F2} K  AusKlimadaten {3,-5} (erwartet {4}){5}",
                bezeichnung, j.Mittel, j.Amplitude, j.AusKlimadaten, erwartetAusKlimadaten, ok ? "" : "   FEHLER"));
            return ok;
        }

#endif
    }
}
