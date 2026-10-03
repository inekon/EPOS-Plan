using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Stufenformel gegen den Löser</b> (Entwurf KP3 Abschnitt 7, Welle R1): N-AH1 Identität,
    /// N-AH2 Absenkform, N-AH4 Schranke der ersten Ordnung, N-AH5 Monotonie. Die Formeln
    /// (<see cref="Aufheizstufen"/>) rechnen aus der <see cref="Aufheizantwort"/> über die
    /// Matrixfunktionen; die Gegenprobe ist <see cref="Zonenmodell2K.Schritt"/> mit der Treppe der Rampe
    /// bei festen Rändern (Außenluft als einzige Randtemperatur, ohne Sonne und Gewinne).
    ///
    /// <para><b>Die Parametersätze:</b> alle Projektgebäude der Testdatenbank auf dem VDI-Weg
    /// (Ersatzparameter über den Eingangsbauer, nur lesend; ohne Testdatenbank schweigt dieser Teil) und
    /// synthetisch — der Prüfsatz des Hauses aus 1045, gedämmt (R_Rest × 4, g_ext/2), schwer (C × 3),
    /// leicht (C/3), fast gleiche Eigenwerte und zusammenfallende Eigenwerte (Zweig des Lösers); je
    /// Strahlungsanteil 0 und 0,3, Zusatzleitwert 0 und 150 W/K, Außenluft −18,2, −12 und 0 °C.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizStufenformelTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public AufheizStufenformelTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private const double THETA_N = 17.0, THETA_T = 21.0, DELTA_T = THETA_T - THETA_N;
        private static readonly double[] Anteile = { 0.0, 0.3 };
        private static readonly double[] Zusatzleitwerte = { 0.0, 150.0 };
        private static readonly double[] Aussenluft = { -18.2, -12.0, 0.0 };

        /// <summary>Ein Parametersatz; <see cref="NurAnteil"/> = gilt nur für diesen Strahlungsanteil (angeglichene Diagonale).</summary>
        private sealed class Satz
        {
            internal Satz(string name, ErsatzparameterRC p, double nurAnteil = double.NaN)
            {
                Name = name;
                P = p;
                NurAnteil = nurAnteil;
            }

            internal string Name { get; }
            internal ErsatzparameterRC P { get; }
            internal double NurAnteil { get; }
        }

        /// <summary>Ein Fall: Satz, Modell, Antwort und feste Ränder.</summary>
        private sealed class Fall
        {
            internal string Name;
            internal Zonenmodell2K M;
            internal Aufheizantwort Antwort;
            internal double Anteil, Zusatz, Ta, StatW;

            internal Stundenrand Rand(double soll, double heizMaxW = double.NaN)
                => new Stundenrand(Ta, Ta, soll, double.PositiveInfinity, 0.0, 0.0, 0.0,
                                   heizMaxW, double.NaN, Anteil, 0.0, Zusatz);

            public override string ToString()
                => string.Format(CultureInfo.InvariantCulture, "{0}, a = {1}, Zusatz {2} W/K, T_a {3} °C", Name, Anteil, Zusatz, Ta);
        }

        // =====================================================================
        //  Die Parametersätze
        // =====================================================================

        /// <summary>
        /// Der Prüfsatz mit <b>angeglichener Diagonale</b> der geregelten Matrix (A₁₁ = A₂₂, über C_IW) und
        /// <b>entkoppelten Massen</b>: Im geregelten Fall koppeln die beiden Oberflächen über die Strahlung
        /// (G_rad) und über die Leistung, die zum Strahlungsanteil auf beide verteilt wird; beides hebt
        /// sich auf bei G_rad = a·w_AW·G_c,IW/(1 − a) (beim Prüfsatz gilt w_AW·G_c,IW = w_IW·G_c,AW, also
        /// für beide Zeilen). Dann ist A nahezu ein Vielfaches der Einheit — die Eigenwerte fallen zusammen.
        /// <paramref name="verstimmung"/> &gt; 0 verschiebt G_rad um diesen Anteil: fast gleiche Eigenwerte.
        /// </summary>
        private static ErsatzparameterRC Angeglichen(double anteil, double verstimmung)
        {
            ErsatzparameterRC basis = AufheizantwortTests.Pruefsatz();
            double wAw = basis.A_AW_gesamt_M2 / (basis.A_AW_gesamt_M2 + basis.A_IW_M2);
            double gRadZiel = anteil * wAw / basis.R_conv_IW_KW / (1.0 - anteil);
            double gRad = gRadZiel > 0.0 ? gRadZiel * (1.0 + verstimmung) : 1.0 / basis.R_rad_KW * Math.Max(verstimmung, 1e-12);
            double faktorRRad = 1.0 / basis.R_rad_KW / gRad;
            Matrix2 a = new Zonenmodell2K(AufheizantwortTests.Pruefsatz(faktorRRad: faktorRRad)).Aufheizantwort(anteil, 0.0).A;
            return AufheizantwortTests.Pruefsatz(faktorRRad: faktorRRad, faktorCIw: a.M22 / a.M11);
        }

        private static List<Satz> Synthetisch()
        {
            var saetze = new List<Satz>
            {
                new Satz("Prüfsatz", AufheizantwortTests.Pruefsatz()),
                new Satz("gedämmt", AufheizantwortTests.Pruefsatz(faktorRRest: 4.0, faktorRExt: 2.0)),
                new Satz("schwer", AufheizantwortTests.Pruefsatz(faktorCAw: 3.0, faktorCIw: 3.0)),
                new Satz("leicht", AufheizantwortTests.Pruefsatz(faktorCAw: 1.0 / 3.0, faktorCIw: 1.0 / 3.0)),
            };
            foreach (double a in Anteile)
            {
                saetze.Add(new Satz("fast gleiche Eigenwerte", Angeglichen(a, 1e-3), a));
                saetze.Add(new Satz("zusammenfallende Eigenwerte", Angeglichen(a, 0.0), a));
            }
            return saetze;
        }

        private static readonly object Sperre = new object();
        private static List<Satz> _datenbanksaetze;
        private static string _datenbankpfad;

        /// <summary>
        /// Die Ersatzparameter aller Projektgebäude der Testdatenbank auf dem VDI-Weg — über den
        /// Eingangsbauer mit dem Klimakalender des Projekts, nur lesend. Ein Gebäude, das der Bauer
        /// benannt ablehnt (Mehrzonen ohne Zonenweg), fehlt; gezählt wird es im Protokoll.
        /// </summary>
        private List<Satz> Datenbanksaetze()
        {
            if (!_db.Vorhanden) return new List<Satz>();
            lock (Sperre)
            {
                if (_datenbanksaetze != null && _datenbankpfad == DataRepository.PfadUeberschreibung) return _datenbanksaetze;
                var saetze = new List<Satz>();
                int abgelehnt = 0;
                DataTable projekte = DataRepository.GetDataTable(
                    "SELECT DISTINCT ID_Projekt FROM Z_ProjektGebaeude ORDER BY ID_Projekt");
                foreach (DataRow zeile in projekte.Rows)
                {
                    int idProjekt = Convert.ToInt32(zeile[0], CultureInfo.InvariantCulture);
                    var ctrl = new ProjektGebaeudeCtrl();
                    ctrl.ReadAll(idProjekt);
                    SimulationWaermebedarf sim = null;
                    for (int i = 0; i < ctrl.rows; i++)
                    {
                        ProjektGebaeudeModel item = ctrl.items[i];
                        if (!Gebaeuderechenweg.IstVdi6007(item.Gebaeude_Modell)) continue;
                        if (sim == null)
                        {
                            var projekt = new ProjektCtrl();
                            projekt.ReadSingle(idProjekt);
                            sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
                            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
                        }
                        KlimakalenderGemeinsam g = sim.Kalender.Gemeinsam;
                        try
                        {
                            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(item, g.SolarOrtszeit, g.WochenendeOrtszeit,
                                                                                  g.Laengengrad, g.Breitengrad);
                            saetze.Add(new Satz(string.Format(CultureInfo.InvariantCulture, "Projekt {0}, Gebäude {1}",
                                                              idProjekt, item.ID_Gebaeude), e.Parameter));
                        }
                        catch (GebaeudeModellException)
                        {
                            abgelehnt++;
                        }
                    }
                }
                _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "Testdatenbank: {0} Projektgebäude auf dem VDI-Weg, {1} vom Eingangsbauer abgelehnt", saetze.Count, abgelehnt));
                _datenbanksaetze = saetze;
                _datenbankpfad = DataRepository.PfadUeberschreibung;
                return saetze;
            }
        }

        private IEnumerable<Fall> Faelle()
        {
            var saetze = new List<Satz>(Synthetisch());
            saetze.AddRange(Datenbanksaetze());
            foreach (Satz s in saetze)
            {
                var m = new Zonenmodell2K(s.P, s.Name);
                foreach (double a in Anteile)
                {
                    if (!double.IsNaN(s.NurAnteil) && s.NurAnteil != a) continue;
                    foreach (double zusatz in Zusatzleitwerte)
                    {
                        Aufheizantwort antwort = m.Aufheizantwort(a, zusatz);
                        foreach (double ta in Aussenluft)
                            yield return new Fall
                            {
                                Name = s.Name, M = m, Antwort = antwort, Anteil = a, Zusatz = zusatz, Ta = ta,
                                StatW = m.StationaereHeizlastW(THETA_T, ta, ta, a, zusatz),
                            };
                    }
                }
            }
        }

        private static double Relativ(double erwartet, double ist) => Math.Abs(ist - erwartet) / Math.Abs(erwartet);

        /// <summary>Die Treppe der Rampe: Stufe j von n auf θ_N + ΔT·j/n.</summary>
        private static double Stufe(double thetaN, double dT, int j, int n) => thetaN + dT * j / n;

        // =====================================================================
        //  N-AH1 Identität
        // =====================================================================

        /// <summary>
        /// <b>N-AH1:</b> Φ̄_n gegen das Mittel der n-ten Stunde aus <c>Schritt</c> ab dem Gleichgewicht
        /// x = −A⁻¹·b bei θ_N, n = 1 … 24 — relativ ≤ 1e-9. Φ̂_n gegen die Kappung: eine Grenze
        /// Φ̂_n·(1 + 1e-9) kappt in keiner Stunde der Rampe, Φ̂_n·(1 − 1e-6) kappt in der Sprungstunde.
        /// </summary>
        [Fact]
        public void N_AH1_Die_Stufenformel_trifft_den_Loeser()
        {
            double groesste = 0.0;
            string wo = "";
            int faelle = 0, zusammenfallend = 0;
            foreach (Fall f in Faelle())
            {
                faelle++;
                if (f.Antwort.Zusammenfallend) zusammenfallend++;
                // Die angeglichenen Sätze treffen den Zweig, für den sie gebaut sind.
                if (f.Name == "zusammenfallende Eigenwerte") Assert.True(f.Antwort.Zusammenfallend, f.ToString());
                if (f.Name == "fast gleiche Eigenwerte")
                    Assert.True(!f.Antwort.Zusammenfallend && f.Antwort.Tau2S / f.Antwort.Tau1S - 1.0 < 1e-2, f.ToString());
                Vektor2 x0 = f.M.StationaererZustand(THETA_N, f.Ta, f.Ta, f.Anteil, f.Zusatz);
                for (int n = 1; n <= 24; n++)
                {
                    double mittel = Aufheizstufen.StundenmittelW(f.Antwort, f.StatW, DELTA_T, n);
                    double augenblick = Aufheizstufen.AugenblickW(f.Antwort, f.StatW, DELTA_T, n);

                    f.M.Zuruecksetzen(x0.A, x0.B);
                    Stundenergebnis e = default;
                    for (int j = 1; j <= n; j++)
                    {
                        Stundenrand r = f.Rand(Stufe(THETA_N, DELTA_T, j, n));
                        e = f.M.Schritt(in r);
                        Assert.Equal(1, e.Abschnitte);
                        Assert.Equal(0.0, e.HeizleistungMaxAnteil);
                    }
                    double rel = Relativ(mittel, e.HeizleistungW);
                    if (rel > groesste) { groesste = rel; wo = f + ", n = " + n.ToString(CultureInfo.InvariantCulture); }
                    Assert.True(rel <= 1e-9, "N-AH1 " + f + ", n = " + n.ToString(CultureInfo.InvariantCulture) +
                                             ": Formel " + mittel.ToString("R", CultureInfo.InvariantCulture) +
                                             " W, Löser " + e.HeizleistungW.ToString("R", CultureInfo.InvariantCulture) + " W");

                    // Knapp über dem Augenblickswert: keine Kappung in der ganzen Rampe.
                    f.M.Zuruecksetzen(x0.A, x0.B);
                    for (int j = 1; j <= n; j++)
                    {
                        Stundenrand r = f.Rand(Stufe(THETA_N, DELTA_T, j, n), augenblick * (1.0 + 1e-9));
                        e = f.M.Schritt(in r);
                        Assert.True(e.HeizleistungMaxAnteil == 0.0,
                            "Φ̂_n·(1 + 1e-9) kappt: " + f + ", n = " + n.ToString(CultureInfo.InvariantCulture) +
                            ", Stufe " + j.ToString(CultureInfo.InvariantCulture));
                    }

                    // Knapp darunter: die Sprungstunde kappt.
                    f.M.Zuruecksetzen(x0.A, x0.B);
                    for (int j = 1; j <= n; j++)
                    {
                        Stundenrand r = f.Rand(Stufe(THETA_N, DELTA_T, j, n), augenblick * (1.0 - 1e-6));
                        e = f.M.Schritt(in r);
                    }
                    Assert.True(e.HeizleistungMaxAnteil > 0.0,
                        "Φ̂_n·(1 − 1e-6) kappt nicht: " + f + ", n = " + n.ToString(CultureInfo.InvariantCulture));
                }
            }
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "N-AH1: {0} Fälle × 24 Rampen, davon {1} im zusammenfallenden Zweig; größte relative Abweichung {2:G3} ({3})",
                faelle, zusammenfallend, groesste, wo));
            Assert.Equal(2 * Zusatzleitwerte.Length * Aussenluft.Length, zusammenfallend);
        }

        // =====================================================================
        //  N-AH2 Absenkform
        // =====================================================================

        /// <summary>
        /// <b>N-AH2:</b> eingeschwungen bei θ_T, dann D Stunden auf θ_N vor der Sprungstunde (die letzten
        /// n − 1 als Rampe), D = n − 1 … 12 — die Absenkform trifft das Mittel der Sprungstunde relativ
        /// ≤ 1e-9 und liegt nie über der Gleichgewichtsform; nur mit Φ_stat(θ_T) &gt; G_0·ΔT (Befund B12).
        /// </summary>
        [Fact]
        public void N_AH2_Die_Absenkform_trifft_den_Loeser_und_liegt_nie_ueber_der_Gleichgewichtsform()
        {
            double groesste = 0.0;
            int faelle = 0, offen = 0, rampen = 0;
            foreach (Fall f in Faelle())
            {
                if (!Aufheizstufen.AbsenkformGeschlossen(f.Antwort, f.StatW, DELTA_T)) { offen++; continue; }
                faelle++;
                Vektor2 xT = f.M.StationaererZustand(THETA_T, f.Ta, f.Ta, f.Anteil, f.Zusatz);
                for (int n = 1; n <= 13; n++)
                {
                    double gleichgewicht = Aufheizstufen.StundenmittelW(f.Antwort, f.StatW, DELTA_T, n);
                    for (int d = n - 1; d <= 12; d++)
                    {
                        double absenk = Aufheizstufen.AbsenkformW(f.Antwort, f.StatW, DELTA_T, n, d);
                        Assert.True(absenk <= gleichgewicht, "Absenkform über der Gleichgewichtsform: " + f +
                                    ", n = " + n.ToString(CultureInfo.InvariantCulture) + ", D = " + d.ToString(CultureInfo.InvariantCulture));

                        f.M.Zuruecksetzen(xT.A, xT.B);
                        Stundenergebnis e = default;
                        for (int i = 0; i < d - (n - 1); i++)
                        {
                            Stundenrand r = f.Rand(THETA_N);
                            e = f.M.Schritt(in r);
                            Assert.Equal(1, e.Abschnitte);
                        }
                        for (int j = 1; j <= n; j++)
                        {
                            Stundenrand r = f.Rand(Stufe(THETA_N, DELTA_T, j, n));
                            e = f.M.Schritt(in r);
                            Assert.Equal(1, e.Abschnitte);
                        }
                        double rel = Relativ(absenk, e.HeizleistungW);
                        if (rel > groesste) groesste = rel;
                        Assert.True(rel <= 1e-9, "N-AH2 " + f + ", n = " + n.ToString(CultureInfo.InvariantCulture) +
                                                 ", D = " + d.ToString(CultureInfo.InvariantCulture) + ": Formel " +
                                                 absenk.ToString("R", CultureInfo.InvariantCulture) + " W, Löser " +
                                                 e.HeizleistungW.ToString("R", CultureInfo.InvariantCulture) + " W");
                        rampen++;
                    }
                }
            }
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "N-AH2: {0} Fälle geschlossen ({1} Rampen), {2} Fälle ohne geregelte Absenkung; größte relative Abweichung {3:G3}",
                faelle, rampen, offen, groesste));
            Assert.True(faelle > 0);
        }

        // =====================================================================
        //  N-AH4 Schranke
        // =====================================================================

        /// <summary>
        /// <b>N-AH4:</b> Die erste Ordnung ist eine Schranke ohne Ausnahme — Mittel
        /// n_F = ⌈C_w·ΔT/(h·(P − Φ_stat))⌉ ≥ n, Augenblick ⌈(C_w/h + G_0 − H_s)·ΔT/(P − Φ_stat)⌉ ≥ n.
        /// </summary>
        [Fact]
        public void N_AH4_Die_erste_Ordnung_ist_eine_Schranke()
        {
            double[] verhaeltnisse = { 1.005, 1.01, 1.02, 1.05, 1.1, 1.2, 1.5, 2.0, 3.0 };
            double[] spruenge = { 0.5, 2.0, 4.0, 6.0 };
            const int nMax = 2000;
            int proben = 0, knapp = 0;
            foreach (Fall f in Faelle())
            {
                foreach (double dT in spruenge)
                {
                    foreach (double v in verhaeltnisse)
                    {
                        double p = v * f.StatW;
                        Aufheizwahl mittel = Aufheizstufen.Waehlen(f.Antwort, f.StatW, dT, p, Aufheizform.Stundenmittel, nMax);
                        Aufheizwahl augenblick = Aufheizstufen.Waehlen(f.Antwort, f.StatW, dT, p, Aufheizform.Augenblick, nMax);
                        double nfMittel = Aufheizstufen.ErsteOrdnungMittel(f.Antwort, f.StatW, dT, p);
                        double nfAugenblick = Aufheizstufen.ErsteOrdnungAugenblick(f.Antwort, f.StatW, dT, p);
                        Assert.True(mittel.Erreichbar && augenblick.Erreichbar, "unerreichbar: " + f);
                        Assert.True(nfMittel >= mittel.N, string.Format(CultureInfo.InvariantCulture,
                            "N-AH4 Mittel {0}, ΔT {1}, P/Φ_stat {2}: n_F {3} < n {4}", f, dT, v, nfMittel, mittel.N));
                        Assert.True(nfAugenblick >= augenblick.N, string.Format(CultureInfo.InvariantCulture,
                            "N-AH4 Augenblick {0}, ΔT {1}, P/Φ_stat {2}: n_F {3} < n {4}", f, dT, v, nfAugenblick, augenblick.N));
                        if (nfMittel == mittel.N || nfAugenblick == augenblick.N) knapp++;
                        proben++;
                    }
                }
            }
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "N-AH4: {0} Proben je Form, {1} mit n_F = n", proben, knapp));
        }

        // =====================================================================
        //  N-AH5 Monotonie
        // =====================================================================

        /// <summary>
        /// <b>N-AH5:</b> Gitter T_a −20 … 10 °C, ΔT 0,5 … 6 K, P_auf/Φ_stat(θ_T, −20 °C) 1,01 … 2 bei fester
        /// P_auf: n wächst mit kälterer Luft, größerem ΔT und kleinerer P_auf; die Augenblicksform verlangt
        /// nie weniger Stufen als die Mittelform; die Bemessung (b) bei T_a − ΔT_K (0 … 10 K) nie weniger
        /// als (a). Unerreichbar zählt als n_max + 1. <b>„fest ≥ täglich"</b> (Welle R2): je Gitterpunkt,
        /// t_auf,max ∈ {0, 5, 47} und Absenkdauer D ∈ {2, 13, 61} ist die Stufenzahl der Art „fest" nie
        /// kleiner als die der Art „täglich" (<see cref="Aufheizoptimierung.Stufenzahl"/>) — an den
        /// synthetischen Sätzen, die Projektgebäude tragen dieselbe Formel und kosten nur Laufzeit.
        /// </summary>
        [Fact]
        public void N_AH5_Die_Stufenzahl_ist_monoton()
        {
            double[] aussen = { -20.0, -15.0, -10.0, -5.0, 0.0, 5.0, 10.0 };
            double[] spruenge = { 0.5, 1.0, 2.0, 4.0, 6.0 };
            double[] verhaeltnisse = { 1.01, 1.05, 1.2, 1.5, 2.0 };
            double[] abzuege = { 0.0, 2.0, 5.0, 10.0 };
            const int nMax = 48;
            int punkte = 0, unerreichbar = 0, festPunkte = 0;
            foreach (Fall f in Faelle())
            {
                // Je Satz, Anteil und Zusatzleitwert einmal (die Außenluft läuft im Gitter).
                if (f.Ta != Aussenluft[0]) continue;
                double statKalt = f.M.StationaereHeizlastW(THETA_T, -20.0, -20.0, f.Anteil, f.Zusatz);
                var stat = new double[aussen.Length];
                for (int t = 0; t < aussen.Length; t++)
                    stat[t] = f.M.StationaereHeizlastW(THETA_T, aussen[t], aussen[t], f.Anteil, f.Zusatz);

                foreach (Aufheizform form in new[] { Aufheizform.Stundenmittel, Aufheizform.Augenblick })
                {
                    var n = new int[aussen.Length, spruenge.Length, verhaeltnisse.Length];
                    for (int t = 0; t < aussen.Length; t++)
                        for (int d = 0; d < spruenge.Length; d++)
                            for (int v = 0; v < verhaeltnisse.Length; v++)
                            {
                                Aufheizwahl w = Aufheizstufen.Waehlen(f.Antwort, stat[t], spruenge[d], verhaeltnisse[v] * statKalt, form, nMax);
                                n[t, d, v] = w.Erreichbar ? w.N : nMax + 1;
                                if (!w.Erreichbar) unerreichbar++;
                                punkte++;
                                if (form == Aufheizform.Augenblick)
                                {
                                    Aufheizwahl m = Aufheizstufen.Waehlen(f.Antwort, stat[t], spruenge[d], verhaeltnisse[v] * statKalt,
                                                                         Aufheizform.Stundenmittel, nMax);
                                    Assert.True(n[t, d, v] >= (m.Erreichbar ? m.N : nMax + 1), "Augenblick < Mittel: " + f);
                                }
                            }
                    for (int t = 0; t < aussen.Length; t++)
                        for (int d = 0; d < spruenge.Length; d++)
                            for (int v = 0; v < verhaeltnisse.Length; v++)
                            {
                                string wo = string.Format(CultureInfo.InvariantCulture, "{0}, {1}, T_a {2}, ΔT {3}, P/Φ {4}",
                                                          f, form, aussen[t], spruenge[d], verhaeltnisse[v]);
                                if (t + 1 < aussen.Length) Assert.True(n[t, d, v] >= n[t + 1, d, v], "kältere Luft, weniger Stufen: " + wo);
                                if (d + 1 < spruenge.Length) Assert.True(n[t, d, v] <= n[t, d + 1, v], "größerer Sprung, weniger Stufen: " + wo);
                                if (v + 1 < verhaeltnisse.Length) Assert.True(n[t, d, v] >= n[t, d, v + 1], "kleinere Leistung, weniger Stufen: " + wo);
                            }

                    // (b) ≥ (a): die Bemessung bei T_a − ΔT_K.
                    for (int t = 0; t < aussen.Length; t++)
                        foreach (double abzug in abzuege)
                        {
                            double tb = aussen[t] - abzug;
                            double statB = f.M.StationaereHeizlastW(THETA_T, tb, tb, f.Anteil, f.Zusatz);
                            Aufheizwahl wb = Aufheizstufen.Waehlen(f.Antwort, statB, 4.0, 1.2 * statKalt, form, nMax);
                            int nb = wb.Erreichbar ? wb.N : nMax + 1;
                            Assert.True(nb >= n[t, 3, 2], string.Format(CultureInfo.InvariantCulture,
                                "(b) < (a): {0}, {1}, T_a {2}, ΔT_K {3}", f, form, aussen[t], abzug));
                        }

                    // fest ≥ täglich (R2): dieselbe Antwort, dieselbe Last, beide Arten der Planung.
                    if (f.Name.StartsWith("Projekt", StringComparison.Ordinal)) continue;
                    for (int t = 0; t < aussen.Length; t++)
                        for (int d = 0; d < spruenge.Length; d++)
                            for (int v = 0; v < verhaeltnisse.Length; v++)
                                foreach (int tMax in new[] { 0, 5, 47 })
                                    foreach (int dauer in new[] { 2, 13, 61 })
                                    {
                                        double p = verhaeltnisse[v] * statKalt;
                                        Aufheizstufenzahl taeglich = Aufheizoptimierung.Stufenzahl(f.Antwort, stat[t], spruenge[d], p, form, tMax, dauer, false);
                                        Aufheizstufenzahl fest = Aufheizoptimierung.Stufenzahl(f.Antwort, stat[t], spruenge[d], p, form, tMax, dauer, true);
                                        Assert.True(fest.N >= taeglich.N, string.Format(CultureInfo.InvariantCulture,
                                            "fest < täglich: {0}, {1}, T_a {2}, ΔT {3}, P/Φ {4}, t_max {5}, D {6}",
                                            f, form, aussen[t], spruenge[d], verhaeltnisse[v], tMax, dauer));
                                        Assert.True(taeglich.N <= Math.Min(Math.Min(tMax + 1, dauer + 1), nMax));
                                        festPunkte++;
                                    }
                }
            }
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "N-AH5: {0} Gitterpunkte, davon {1} unerreichbar (n_max = {2}); fest ≥ täglich an {3} Punkten",
                punkte, unerreichbar, nMax, festPunkte));
            Assert.True(festPunkte > 0);
            Assert.True(punkte > 0);
        }
    }
}
