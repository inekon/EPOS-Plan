using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Sondenfeld der Wärmequelle „Erdreich" mit Entzugsrückwirkung (Konzept
    /// Simulationsablauf, Abschnitt 23).
    ///
    /// Mittlere Soletemperatur zu Beginn der Stunde t (Last q je Sondenmeter, Entzug positiv):
    ///
    ///   T_f(t) = T_u − ΔT_V(t) − Σ_{i&lt;t} q_i · [G(t−i) − G(t−i−1)] − R_b · q_{t−1}
    ///
    /// G ist die über das Feld gemittelte Sprungantwort der endlichen Linienquelle mit
    /// Spiegelquelle (mittlere Wandtemperatur nach Claesson und Javed), ΔT_V der Beitrag der
    /// Vorjahre. G wird beim Aufbau einmal auf einem logarithmischen Zeitraster gerechnet und
    /// für jede ganze Stunde tabelliert; die Faltung läuft danach als Tabellenzugriff.
    ///
    /// Die Klasse ist frei von Datenbank und Oberfläche; die Simulation reicht Länge, Zahl,
    /// Bodenkennwerte und ungestörte Temperatur herein und meldet je Stunde den Entzug.
    /// </summary>
    public sealed class Erdsondenfeld
    {
        // ------------------------------------------------------------------
        // Festwerte (Konzept 23.3)
        // ------------------------------------------------------------------

        /// <summary>Sondenabstand [m] — Bezug der Tabelle B2 nach VDI 4640 Blatt 2.</summary>
        public const double SONDENABSTAND_M = 6.0;

        /// <summary>Bohrlochradius [m] — Bohrloch 150 mm, Bezug der Tabelle B2.</summary>
        public const double BOHRLOCHRADIUS_M = 0.075;

        /// <summary>Bohrlochwiderstand R_b [m·K/W] — Doppel-U 32 × 3,0, Verfüllung λ = 0,8 W/(m·K).</summary>
        public const double BOHRLOCHWIDERSTAND = 0.10;

        /// <summary>Kopfüberdeckung D [m] — Tiefe des Sondenkopfs unter Gelände.</summary>
        public const double KOPFUEBERDECKUNG_M = 2.0;

        /// <summary>Betrachtungsjahr n: Das Rechenjahr folgt auf n − 1 Vorjahre.</summary>
        public const int BETRACHTUNGSJAHR = 10;

        /// <summary>Heizgrenze [°C] der Heizgradstunden, nach denen die Vorjahreslast auf Monate fällt.</summary>
        public const double HEIZGRENZE_C = 15.0;

        /// <summary>Stunden des Rechenjahres.</summary>
        public const int STUNDEN = 8760;

        private const int PUNKTE_JE_DEKADE = 25;
        private const int SIMPSON_INTERVALLE = 160;
        private static readonly int[] TAGE_PRO_MONAT = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        // ------------------------------------------------------------------
        // Kennwerte
        // ------------------------------------------------------------------

        /// <summary>Länge je Sonde H [m].</summary>
        public double LaengeM { get; }

        /// <summary>Zahl der Sonden N.</summary>
        public int Anzahl { get; }

        /// <summary>Wärmeleitfähigkeit des Bodens λ [W/(m·K)].</summary>
        public double Lambda { get; }

        /// <summary>Temperaturleitfähigkeit a [m²/s].</summary>
        public double A_m2s { get; }

        /// <summary>Ungestörte Erdreichtemperatur T_u [°C].</summary>
        public double TUngestoert { get; }

        /// <summary>Sondenabstand B [m].</summary>
        public double AbstandM { get; }

        /// <summary>Bohrlochwiderstand R_b [m·K/W].</summary>
        public double Rb { get; }

        /// <summary>Betrachtungsjahr n: Das Rechenjahr folgt auf n − 1 Vorjahre.</summary>
        public int Betrachtungsjahr { get; }

        /// <summary>Geometrie und Bohrlochkennwerte, mit denen das Feld rechnet (bereinigt).</summary>
        public Sondenfeldgeometrie Geometrie { get; }

        /// <summary>Gesamte Sondenlänge N · H [m].</summary>
        public double Sondenmeter { get { return LaengeM * Anzahl; } }

        /// <summary>Zahl der Vorjahre, mit denen <see cref="VorjahreSetzen"/> gerechnet hat.</summary>
        public int Vorjahre { get; private set; }

        /// <summary>Summe des gemeldeten Entzugs im Rechenjahr [kWh] (Rückspeisung negativ).</summary>
        public double EntzugKwh { get; private set; }

        private readonly double _rbRadius;
        private readonly double _kopf;
        private readonly double[] _abstaende;      // eindeutige Paarabstände
        private readonly double[] _gewichte;       // Häufigkeit / N
        private double[] _rasterLnTau;             // ln τ [h]
        private double[] _rasterG;                 // G(τ) [K·m/W]
        private readonly double[] _puls;           // P[k] = G(k+1) − G(k), k = 0…8759
        private readonly double[] _last = new double[STUNDEN];   // W/m je Stunde
        private readonly double[] _vorjahr = new double[STUNDEN]; // ΔT_V [K] je Stunde
        private int _gemeldet;                      // Zahl der gemeldeten Stunden

        /// <summary>Baut das Feld und tabelliert die Sprungantwort.</summary>
        /// <param name="laengeM">Länge je Sonde H [m], &gt; 0.</param>
        /// <param name="anzahl">Zahl der Sonden; Werte &lt; 1 gelten als 1.</param>
        /// <param name="lambda">Wärmeleitfähigkeit λ [W/(m·K)], &gt; 0.</param>
        /// <param name="rhoCpMJ">volumetrische Wärmekapazität ρ·c_p [MJ/(m³·K)], &gt; 0.</param>
        /// <param name="tUngestoert">ungestörte Erdreichtemperatur T_u [°C].</param>
        /// <param name="geometrie">Geometrie und Bohrlochkennwerte; null = <see cref="Sondenfeldgeometrie.Norm"/>.</param>
        public Erdsondenfeld(double laengeM, int anzahl, double lambda, double rhoCpMJ, double tUngestoert,
                             Sondenfeldgeometrie geometrie = null)
        {
            if (!(laengeM > 0)) throw new ArgumentOutOfRangeException(nameof(laengeM));
            if (!(lambda > 0)) throw new ArgumentOutOfRangeException(nameof(lambda));
            if (!(rhoCpMJ > 0)) throw new ArgumentOutOfRangeException(nameof(rhoCpMJ));

            LaengeM = laengeM;
            Anzahl = Math.Max(1, anzahl);
            Lambda = lambda;
            A_m2s = lambda / (rhoCpMJ * 1.0e6);
            TUngestoert = tUngestoert;
            Geometrie = (geometrie ?? Sondenfeldgeometrie.Norm).Bereinigt();
            AbstandM = Geometrie.AbstandM;
            Rb = Geometrie.Bohrlochwiderstand;
            _rbRadius = Geometrie.BohrlochradiusM;
            _kopf = Geometrie.KopfueberdeckungM;
            Betrachtungsjahr = Geometrie.Betrachtungsjahr;

            AbstaendeBilden(out _abstaende, out _gewichte);
            RasterRechnen(Betrachtungsjahr + 1);

            double[] g = new double[STUNDEN + 1];
            for (int k = 1; k <= STUNDEN; k++) g[k] = G(k);
            _puls = new double[STUNDEN];
            for (int k = 0; k < STUNDEN; k++) _puls[k] = g[k + 1] - g[k];
        }

        // ------------------------------------------------------------------
        // Geometrie
        // ------------------------------------------------------------------

        /// <summary>
        /// Lage der Sonden: möglichst quadratisches Raster mit Abstand B, zeilenweise gefüllt.
        /// </summary>
        public static List<(double X, double Y)> Lagen(int anzahl, double abstandM)
        {
            int n = Math.Max(1, anzahl);
            int spalten = (int)Math.Ceiling(Math.Sqrt(n));
            var lagen = new List<(double, double)>(n);
            for (int i = 0; i < n; i++)
                lagen.Add(((i % spalten) * abstandM, (i / spalten) * abstandM));
            return lagen;
        }

        private void AbstaendeBilden(out double[] abstaende, out double[] gewichte)
        {
            var lagen = Lagen(Anzahl, AbstandM);
            var zaehler = new SortedDictionary<long, (double r, int n)>();
            for (int j = 0; j < lagen.Count; j++)
                for (int k = 0; k < lagen.Count; k++)
                {
                    double r = j == k ? _rbRadius
                        : Math.Sqrt(Math.Pow(lagen[j].X - lagen[k].X, 2) + Math.Pow(lagen[j].Y - lagen[k].Y, 2));
                    long schluessel = (long)Math.Round(r * 1.0e6);
                    if (zaehler.TryGetValue(schluessel, out var e)) zaehler[schluessel] = (e.r, e.n + 1);
                    else zaehler[schluessel] = (r, 1);
                }

            abstaende = new double[zaehler.Count];
            gewichte = new double[zaehler.Count];
            int i = 0;
            foreach (var e in zaehler.Values)
            {
                abstaende[i] = e.r;
                gewichte[i] = (double)e.n / Anzahl;
                i++;
            }
        }

        // ------------------------------------------------------------------
        // Sprungantwort
        // ------------------------------------------------------------------

        private void RasterRechnen(int jahre)
        {
            double lnMin = 0.0;                                    // 1 h
            double lnMax = Math.Log(jahre * (double)STUNDEN);
            double schritt = Math.Log(10.0) / PUNKTE_JE_DEKADE;
            int n = (int)Math.Ceiling((lnMax - lnMin) / schritt) + 1;
            _rasterLnTau = new double[n];
            _rasterG = new double[n];
            for (int i = 0; i < n; i++)
            {
                double ln = lnMin + i * schritt;
                _rasterLnTau[i] = ln;
                _rasterG[i] = GDirekt(Plattformrundung.Exp(ln));
            }
        }

        /// <summary>
        /// Sprungantwort G(τ) [K·m/W] des Feldes für die Zeit τ [h]: mittlere Wandtemperatur
        /// je W/m Dauerlast, über alle Sonden gemittelt. Interpoliert linear in ln τ.
        /// </summary>
        public double G(double tauStunden)
        {
            if (!(tauStunden > 0)) return 0.0;
            double ln = Math.Log(tauStunden);
            if (ln <= _rasterLnTau[0])
            {
                // unter 1 h: linear zur Null (nur für Analysen; die Faltung braucht τ ≥ 1 h)
                return _rasterG[0] * tauStunden;
            }
            int letzte = _rasterLnTau.Length - 1;
            if (ln >= _rasterLnTau[letzte])
            {
                // über das Raster hinaus: aus den letzten beiden Punkten fortsetzen
                double s = (_rasterG[letzte] - _rasterG[letzte - 1]) / (_rasterLnTau[letzte] - _rasterLnTau[letzte - 1]);
                return _rasterG[letzte] + s * (ln - _rasterLnTau[letzte]);
            }
            double schritt = _rasterLnTau[1] - _rasterLnTau[0];
            int i = (int)((ln - _rasterLnTau[0]) / schritt);
            if (i >= letzte) i = letzte - 1;
            double f = (ln - _rasterLnTau[i]) / schritt;
            return _rasterG[i] + f * (_rasterG[i + 1] - _rasterG[i]);
        }

        /// <summary>G(τ) unmittelbar aus dem Integral (ohne Raster).</summary>
        public double GDirekt(double tauStunden)
        {
            if (!(tauStunden > 0)) return 0.0;
            double summe = 0.0;
            for (int i = 0; i < _abstaende.Length; i++)
                summe += _gewichte[i] * H(_abstaende[i], tauStunden * 3600.0);
            return summe / (4.0 * Math.PI * Lambda);
        }

        /// <summary>
        /// h(r, t) = ∫_{s₀}^{∞} e^{−r²s²} · Y(Hs, Ds) / (H s²) ds mit s₀ = 1/√(4at), gerechnet in
        /// ln s mit der Simpsonregel.
        /// </summary>
        private double H(double r, double tSekunden)
        {
            double s0 = 1.0 / Math.Sqrt(4.0 * A_m2s * tSekunden);
            double sMax = 7.0 / r;
            if (s0 >= sMax) return 0.0;
            double u0 = Math.Log(s0), u1 = Math.Log(sMax);
            int n = SIMPSON_INTERVALLE;
            double hSchritt = (u1 - u0) / n;
            double summe = 0.0;
            for (int i = 0; i <= n; i++)
            {
                double s = Plattformrundung.Exp(u0 + i * hSchritt);
                double f = Plattformrundung.Exp(-r * r * s * s) * Y(LaengeM * s, _kopf * s) / (LaengeM * s);
                double w = (i == 0 || i == n) ? 1.0 : (i % 2 == 1 ? 4.0 : 2.0);
                summe += w * f;
            }
            return summe * hSchritt / 3.0;
        }

        private static double Y(double x, double d)
        {
            return 2.0 * Ierf(x) + 2.0 * Ierf(x + 2.0 * d) - Ierf(2.0 * x + 2.0 * d) - Ierf(2.0 * d);
        }

        /// <summary>ierf(X) = X·erf(X) − (1 − e^{−X²})/√π.</summary>
        internal static double Ierf(double x)
        {
            if (x < 0.05)
            {
                // Reihe gegen die Auslöschung: X²/√π − X⁴/(6√π) + X⁶/(30√π)
                double x2 = x * x;
                return x2 / Math.Sqrt(Math.PI) * (1.0 - x2 / 6.0 + x2 * x2 / 30.0);
            }
            return x * Erf(x) - (1.0 - Plattformrundung.Exp(-x * x)) / Math.Sqrt(Math.PI);
        }

        /// <summary>Fehlerfunktion nach Abramowitz/Stegun 7.1.26 (Fehler &lt; 1,5·10⁻⁷).</summary>
        internal static double Erf(double x)
        {
            double vz = x < 0 ? -1.0 : 1.0;
            x = Math.Abs(x);
            double t = 1.0 / (1.0 + 0.3275911 * x);
            double y = 1.0 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Plattformrundung.Exp(-x * x);
            return vz * y;
        }

        // ------------------------------------------------------------------
        // Vorjahre
        // ------------------------------------------------------------------

        /// <summary>
        /// Setzt den Beitrag der Vorjahre: <paramref name="vorjahre"/> Jahre mit den zwölf
        /// Monatslasten <paramref name="monatslastWm"/> [W/m] vor dem Rechenjahr. Ausgewertet je
        /// Tag zur Tagesmitte; der Wert gilt für alle Stunden des Tages.
        /// </summary>
        public void VorjahreSetzen(double[] monatslastWm, int vorjahre)
        {
            Array.Clear(_vorjahr, 0, _vorjahr.Length);
            Vorjahre = 0;
            if (monatslastWm == null || monatslastWm.Length != 12 || vorjahre <= 0) return;
            Vorjahre = vorjahre;

            double[] beginn = new double[12], ende = new double[12];
            double h = 0;
            for (int m = 0; m < 12; m++)
            {
                beginn[m] = h;
                h += TAGE_PRO_MONAT[m] * 24.0;
                ende[m] = h;
            }

            for (int tag = 0; tag < 365; tag++)
            {
                double t = tag * 24.0 + 12.0;
                double dT = 0.0;
                for (int y = 1; y <= vorjahre; y++)
                {
                    double versatz = t + y * (double)STUNDEN;
                    for (int m = 0; m < 12; m++)
                    {
                        double q = monatslastWm[m];
                        if (q == 0) continue;
                        dT += q * (G(versatz - beginn[m]) - G(versatz - ende[m]));
                    }
                }
                for (int s = 0; s < 24; s++) _vorjahr[tag * 24 + s] = dT;
            }
        }

        /// <summary>
        /// Setzt den Beitrag der Vorjahre aus einer Stundenlast (zweiter Feldlauf, Konzept 23.4):
        /// <paramref name="vorjahre"/> Jahre, deren jedes die Last <paramref name="lastKw"/> [kW] je
        /// Stunde trägt (Entzug positiv, Rückspeisung negativ). Weil alle Vorjahre dieselbe Reihe
        /// tragen, fassen sich ihre Pulsantworten zu EINEM Kern zusammen,
        ///
        ///   S(m) = Σ_{y=1}^{n−1} [G(m + 1 + y·8760) − G(m + y·8760)],  m = −8760 … 8758,
        ///
        /// und der Beitrag ist eine einzige Faltung über ein Jahr: ΔT_V(t) = Σ_i q_i · S(t − 1 − i).
        /// Stundengenau, ohne Monats- oder Tagesblöcke; Aufwand 8760² Multiplikationen.
        /// </summary>
        public void VorjahreSetzenStuendlich(double[] lastKw, int vorjahre)
        {
            Array.Clear(_vorjahr, 0, _vorjahr.Length);
            Vorjahre = 0;
            if (lastKw == null || lastKw.Length < STUNDEN || vorjahre <= 0) return;
            Vorjahre = vorjahre;

            // G an jeder ganzen Stunde bis (n + 1) Jahre
            int gLaenge = (vorjahre + 1) * STUNDEN + 1;
            double[] g = new double[gLaenge];
            for (int k = 1; k < gLaenge; k++) g[k] = G(k);

            // Kern S über m + STUNDEN = 0 … 2·STUNDEN − 2
            double[] kern = new double[2 * STUNDEN];
            for (int m = -STUNDEN; m < STUNDEN - 1; m++)
            {
                double summe = 0.0;
                for (int y = 1; y <= vorjahre; y++)
                {
                    int k = m + y * STUNDEN;
                    summe += g[k + 1] - g[k];
                }
                kern[m + STUNDEN] = summe;
            }

            double[] q = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++)
                q[i] = double.IsFinite(lastKw[i]) ? lastKw[i] * 1000.0 / Sondenmeter : 0.0;

            for (int t = 0; t < STUNDEN; t++)
            {
                double dT = 0.0;
                int basis = t - 1 + STUNDEN;    // Index von S(t − 1 − i) ist basis − i
                for (int i = 0; i < STUNDEN; i++) dT += q[i] * kern[basis - i];
                _vorjahr[t] = dT;
            }
        }

        /// <summary>
        /// Die im Rechenjahr gemeldete Last je Stunde [kW] (Entzug positiv, Rückspeisung negativ);
        /// nicht gemeldete Stunden stehen auf 0.
        /// </summary>
        public double[] LastKw()
        {
            double[] r = new double[STUNDEN];
            for (int s = 0; s < _gemeldet && s < STUNDEN; s++) r[s] = _last[s] * Sondenmeter / 1000.0;
            return r;
        }

        /// <summary>
        /// Verteilt eine Jahresentzugsarbeit [kWh/a] nach Heizgradstunden (Heizgrenze
        /// <see cref="HEIZGRENZE_C"/>) der Außentemperatur auf zwölf Monatslasten [W/m] dieses
        /// Feldes. Ohne brauchbare Außentemperatur oder ohne Heizgradstunden gleichmäßig.
        /// </summary>
        public double[] MonatslastenAusJahresentzug(double jahresentzugKwh, double[] aussentemp8760)
        {
            double[] gewicht = new double[12];
            double summe = 0;
            if (aussentemp8760 != null && aussentemp8760.Length >= STUNDEN)
            {
                for (int s = 0; s < STUNDEN; s++)
                {
                    double g = HEIZGRENZE_C - aussentemp8760[s];
                    if (g > 0) { gewicht[ErdreichTemperatur.MonatAusStunde(s)] += g; summe += g; }
                }
            }
            double[] last = new double[12];
            for (int m = 0; m < 12; m++)
            {
                double anteil = summe > 0 ? gewicht[m] / summe : TAGE_PRO_MONAT[m] / 365.0;
                double stunden = TAGE_PRO_MONAT[m] * 24.0;
                last[m] = jahresentzugKwh * anteil * 1000.0 / stunden / Sondenmeter;
            }
            return last;
        }

        // ------------------------------------------------------------------
        // Stundenschritt
        // ------------------------------------------------------------------

        /// <summary>Beitrag der Vorjahre ΔT_V [K] zur Stunde (Absenkung positiv).</summary>
        public double VorjahrAbsenkung(int stunde)
        {
            return stunde >= 0 && stunde < STUNDEN ? _vorjahr[stunde] : 0.0;
        }

        /// <summary>
        /// Meldet den Entzug der nächsten Stunde [kW] (Rückspeisung negativ) und liefert die
        /// mittlere Soletemperatur zu Beginn der Stunde danach (Kopplung über die Vorstunde).
        /// Die Stunden müssen lückenlos ab 0 gemeldet werden.
        /// </summary>
        public double StundeMelden(double entzugKw)
        {
            if (_gemeldet >= STUNDEN) return Temperatur(STUNDEN - 1);
            double q = double.IsFinite(entzugKw) ? entzugKw * 1000.0 / Sondenmeter : 0.0;
            _last[_gemeldet] = q;
            EntzugKwh += double.IsFinite(entzugKw) ? entzugKw : 0.0;
            _gemeldet++;
            return Temperatur(Math.Min(_gemeldet, STUNDEN - 1));
        }

        /// <summary>
        /// Mittlere Soletemperatur T_f [°C] zu Beginn der Stunde <paramref name="stunde"/> aus den
        /// bis dahin gemeldeten Lasten (Gl. 1).
        /// </summary>
        public double Temperatur(int stunde)
        {
            if (stunde < 0) stunde = 0;
            if (stunde >= STUNDEN) stunde = STUNDEN - 1;
            int bis = Math.Min(stunde, _gemeldet);
            double dT = 0.0;
            for (int i = 0; i < bis; i++) dT += _last[i] * _puls[stunde - 1 - i];
            double rbAnteil = bis > 0 && bis == stunde ? Rb * _last[stunde - 1] : 0.0;
            return TUngestoert - _vorjahr[stunde] - dT - rbAnteil;
        }

        /// <summary>Temperatur zu Beginn des Rechenjahres (ohne Last des Jahres, mit Vorjahren).</summary>
        public double Starttemperatur { get { return TUngestoert - _vorjahr[0]; } }

        /// <summary>
        /// Jahresreihe ohne Last des Rechenjahres: T_u − ΔT_V(t). Die Simulation belegt die Reihe
        /// damit vor, bevor sie Stunde um Stunde überschreibt.
        /// </summary>
        public double[] Vorbelegung()
        {
            double[] r = new double[STUNDEN];
            for (int s = 0; s < STUNDEN; s++) r[s] = TUngestoert - _vorjahr[s];
            return r;
        }

        /// <summary>
        /// Rechnet eine ganze Lastreihe [kW] je Stunde durch und liefert die Soletemperatur je
        /// Stunde (für Tests und Analysen; das Feld bleibt danach mit dieser Reihe belegt).
        /// </summary>
        public double[] Durchrechnen(double[] entzugKw)
        {
            double[] t = new double[STUNDEN];
            t[0] = Temperatur(0);
            for (int s = 0; s < STUNDEN; s++)
            {
                double w = StundeMelden(entzugKw != null && s < entzugKw.Length ? entzugKw[s] : 0.0);
                if (s + 1 < STUNDEN) t[s + 1] = w;
            }
            return t;
        }
    }
}
