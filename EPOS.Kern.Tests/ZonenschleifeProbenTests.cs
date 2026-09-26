using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using static EPOS.Kern.Tests.BauteilwegLaufProbe;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Proben der Zonenschleife (Stufe G6b, Welle W4; Mehrzonenkonzept 8.1)</b> — 1 (Lauf 1:
    /// festgehaltener Nachbar = Einzonenrechnung), 4 (Energiebilanz der Kopplung), 5a (Fixpunkt des
    /// Stundenmittelmodells) und 5b (4×4-Gesamtsystem, gemessen; Anwenderentscheid A8 (a)), 6
    /// (Vorstunde gegen Iteration, gemessen), die 4-K-Regel mit adiabatem Vorlauf, der verlängerte
    /// Vorlauf (A3), der interne Einstieg samt Meldungen und die Messprobe mit 10 und 50 Zonen.
    /// Ohne Datenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZonenschleifeProbenTests
    {
        private readonly ITestOutputHelper _aus;

        public ZonenschleifeProbenTests(ITestOutputHelper aus) { _aus = aus; }

        /// <summary>Das Kriterium von Probe 5b [K] (Anwenderentscheid vom 26.09.2026, A8 (a)).</summary>
        internal const double KRITERIUM_PROBE_5B_K = 0.001;

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static string F(double x, string format = "E2") => x.ToString(format, CultureInfo.InvariantCulture);

        /// <summary>Zwei Hälften mit Trennwand der Außengruppe, eigenen Sollwerten und Luftaustausch.</summary>
        private static ProjektGebaeudeModel Gekoppelt(double sollEins, double sollZwei, double stromM3h)
        {
            ProjektGebaeudeModel g = ZonenschleifeTests.Haelften(Trennflaechenzuordnung.Aussen);
            GebaeudeZonensatz a = g.Zonen[0], b = g.Zonen[1];
            g.Zonen = new[]
            {
                new GebaeudeZonensatz(a.ZonenId, a.Bezeichnung, a.Bauteile, a.Nutzflaeche_M2,
                                      new Zoneneingaben(Nutzflaeche: a.Nutzflaeche_M2, SollTag: sollEins, SollNacht: sollEins), a.Rang),
                new GebaeudeZonensatz(b.ZonenId, b.Bezeichnung, b.Bauteile, b.Nutzflaeche_M2,
                                      new Zoneneingaben(Nutzflaeche: b.Nutzflaeche_M2, SollTag: sollZwei, SollNacht: sollZwei), b.Rang),
            };
            if (stromM3h > 0.0) g.Zonenluftstroeme = new[] { new Zonenluftstrom(1, 2, stromM3h) };
            return g;
        }

        // =====================================================================
        //  Probe 1, Lauf 1 — festgehaltener Nachbar
        // =====================================================================

        /// <summary>
        /// <b>Probe 1, Lauf 1</b> (Mehrzonenkonzept 8.1, A7 (b)): Die Kellerzone wird für ihre Nachbarn
        /// auf 15,0 °C gehalten; die Wohnzone rechnet durch die ganze Zonenschleife Bit für Bit wie die
        /// Einzonenrechnung mit unbeheiztem Rand auf 15 °C — dieselbe Fläche am Ende der Bauteilliste,
        /// derselbe Vorlauf (720 h und Wiederholung). Das Band von Testbeispiel 10 mit der Trennfläche
        /// hält <c>BauteilreduktionNormTests</c> (Messentscheid A7, Probe 11); Lauf 2 mit frei
        /// schwingendem Keller ist <c>ZonenschleifeTests.Wohnzone_und_Keller_rechnen_gekoppelt</c>.
        /// </summary>
        [Fact]
        public void Probe_1_Lauf_1_Festgehaltener_Keller_ist_die_Einzonenrechnung()
        {
            const double keller = 15.0;
            Nachbaruebergang regel = GebaeudeFestwerte.NACHBARUEBERGANG;
            if (regel != Nachbaruebergang.WieUnbeheizt) return;

            ProjektGebaeudeModel g = ZonenEingangTests.MitKeller(out List<BauteilEingang> wohnteile);
            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, ZonenschleifeTests.KlimaDes());
            var schleife = new Zonenschleife(zonen, "Probe 1") { LuftvorgabeFuerProbe = (z, h) => z == 1 ? keller : double.NaN };
            schleife.Vorlauf();
            schleife.Jahr();
            GebaeudeModellErgebnis ist = schleife.Zonenergebnisse(0, 1)[0];

            // Die Einzonenrechnung mit demselben Vorlauf.
            ProjektGebaeudeModel einzonen = Vdi6007Probe.Gebaeude();
            einzonen.Kellertemperatur = keller;
            einzonen.Zonen = new[] { new GebaeudeZonensatz(ZonenEingangTests.WOHNEN, "Wohnen", wohnteile
                .Select(b => b.Rand == Bauteilrand.Zone
                    ? new BauteilEingang(b.Bezeichnung, b.Art, b.Flaeche_M2, Bauteilrand.Unbeheizt, schichten: b.Schichten) : b).ToList(), g.Nutzflaeche) };
            ZonenEingang e1 = ZonenEingang.Einzeln(GebaeudeModellEingang.Bauen(einzonen, ZonenschleifeTests.KlimaDes()));
            var lauf = new Zonenlauf(e1);
            int start = 8760 - Vdi6007Rechenweg.VORLAUF_H;
            lauf.Beginnen(e1.Eingang.ThetaSoll[start]);
            ReadOnlySpan<double> keine = ReadOnlySpan<double>.Empty;
            int vorlaeufe = schleife.VorlaufVerlaengert ? 3 : 2;
            for (int v = 0; v < vorlaeufe; v++)
                for (int h = v == 2 ? 8760 - Zonenschleife.VORLAUF_LANG_H : start; h < 8760; h++)
                {
                    bool s = lauf.Sommerlueftung();
                    lauf.VorlaufUebernehmen(h, lauf.Modell.Schritt(e1.Rand(h, s, keine)));
                }
            for (int h = 0; h < 8760; h++)
            {
                bool s = lauf.Sommerlueftung();
                lauf.Uebernehmen(h, s, lauf.Modell.Schritt(e1.Rand(h, s, keine)));
            }
            GebaeudeModellErgebnis soll = lauf.Ergebnis(0, 1);
            Assert.Equal(ZonenschleifeTests.Abdruck(soll.HeizlastW), ZonenschleifeTests.Abdruck(ist.HeizlastW));
            Assert.Equal(ZonenschleifeTests.Abdruck(soll.Raumtemperatur), ZonenschleifeTests.Abdruck(ist.Raumtemperatur));
            Assert.Equal(ZonenschleifeTests.Abdruck(soll.OperativeTemperatur), ZonenschleifeTests.Abdruck(ist.OperativeTemperatur));
        }

        // =====================================================================
        //  Probe 4 — Energiebilanz der Kopplung
        // =====================================================================

        /// <summary>
        /// <b>Probe 4</b> (Mehrzonenkonzept 8.1, 2.2 Punkt 3): Die Jahresbilanz je Zone — Heizung und
        /// Lasten gegen den Strom über Lüftung und Luftaustausch (g_ext·(θ̄_air − θ_Lue)), den Strom des
        /// AW-Zweigs gegen θ_eq samt Nachbarglied (g_Rest·(θ̄_m,AW − θ_eq)) und die Änderung der
        /// Speicherinhalte — schließt auf weniger als 0,1 % der Jahresenergie; die Zonenluftströme
        /// summieren sich stündlich zu null.
        ///
        /// <para><b>Befund, über das Gebäude gemessen:</b> Das Nachbarglied g_Rest·B_NR·(θ̄_m,AW −
        /// θ̄_air,Nachbar) rechnet jede Zone gegen die Luft ihres Nachbarn, die Gegenseite nimmt diesen
        /// Strom nicht auf — die Masse der AW-Gruppe ist mit den Außenwänden zusammengefasst und kälter
        /// als beide Räume. Über das Jahr bleibt dieser Anteil offen; die Probe weist ihn aus (hier rund
        /// 7 % der Jahresheizwärme bei zwei Zonen mit 22 °C und 20 °C, 30 m² Trennwand der Außengruppe)
        /// und hält ihn nur grob. Das Konzept (2.2 Punkt 3) erwartete den Schluss im Jahresmittel.</para>
        /// </summary>
        [Fact]
        public void Probe_4_Energiebilanz_je_Zone_und_Bilanz_der_Trennflaechen()
        {
            ProjektGebaeudeModel g = Gekoppelt(22.0, 20.0, 200.0);
            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, ZonenschleifeTests.KlimaDes());
            var stunde = new Stundenergebnis[2, 8760];
            var schleife = new Zonenschleife(zonen, "Probe 4") { BeobachterFuerProbe = (z, h, s) => stunde[z, h] = s };
            schleife.Vorlauf();
            var start = new double[2, 2];
            for (int z = 0; z < 2; z++)
            {
                start[z, 0] = schleife.Laeufe[z].Modell.ThetaMAw;
                start[z, 1] = schleife.Laeufe[z].Modell.ThetaMIw;
            }
            schleife.Jahr();

            double gLuft = zonen[0].Eingang.Luftkopplungen[0].Leitwert_WK;
            Assert.Equal(gLuft, zonen[1].Eingang.Luftkopplungen[0].Leitwert_WK);
            double offen = 0.0, heizGebaeude = 0.0;
            var luft = new double[2];
            for (int z = 0; z < 2; z++)
            {
                ZonenEingang ze = zonen[z];
                ErsatzparameterRC p = ze.Eingang.Parameter;
                double gRest = 1.0 / p.R_Rest_AWGruppe_KW;
                double b = ze.Eingang.Nachbarglieder[0].UA_WK / ze.Eingang.UaSummeGewichtung_WK;
                double rein = 0.0, raus = 0.0;
                for (int h = 0; h < 8760; h++)
                {
                    Stundenergebnis s = stunde[z, h];
                    luft[z] = s.ThetaAirMittel;
                    luft[1 - z] = stunde[1 - z, h].ThetaAirMittel;
                    if (z == 0) Assert.Equal(0.0, gLuft * (luft[1] - luft[0]) + gLuft * (luft[0] - luft[1]));
                    double gExt = 1.0 / p.R_ext_KW;
                    double lue = ze.ThetaLue(h, false, luft);
                    double eq = ze.ThetaEq(h, luft);
                    rein += s.HeizleistungW - s.KuehlleistungW + ze.Eingang.PhiRadAW[h] + ze.Eingang.PhiRadIW[h] + ze.Eingang.PhiConv[h];
                    raus += gExt * (s.ThetaAirMittel - lue) + gRest * (s.ThetaMAwMittel - eq);
                    offen += gRest * b * (s.ThetaMAwMittel - luft[1 - z]);
                    heizGebaeude += s.HeizleistungW;
                }
                Stundenergebnis letzte = stunde[z, 8759];
                double speicher = (p.C_AW_Jk * (letzte.ThetaMAwEnde - start[z, 0]) + p.C_IW_Jk * (letzte.ThetaMIwEnde - start[z, 1]))
                                  / Zonenmodell2K.STUNDE_S;
                double rest = rein - raus - speicher;
                _aus.WriteLine("Probe 4, Zone " + (z + 1) + ": Bilanzrest " + F(rest / Math.Max(rein, 1.0)) + " der zugeführten Energie");
                Assert.True(Math.Abs(rest) < 1e-3 * rein, "Zone " + (z + 1) + ": Bilanzrest " + F(rest) + " Wh");
            }
            double rel = offen / heizGebaeude;
            _aus.WriteLine("Probe 4: offener Anteil der Trennflächen über das Jahr " + F(offen / 1e6, "F3") + " MWh, relativ zur Heizwärme " + F(rel));
            Assert.True(double.IsFinite(rel) && Math.Abs(rel) < 0.2, "Bilanz der Trennflächen relativ " + F(rel));
        }

        // =====================================================================
        //  Probe 5a / 5b (A8 (a))
        // =====================================================================

        /// <summary>
        /// <b>Probe 5a</b> (A8 (a)): Gauß-Seidel mit den Schwellen der Festlegung 6 gegen den Fixpunkt
        /// desselben Stundenmittelmodells (Schwellen 1e‑11 K und 1e‑8 W): alle Stundenwerte besser als
        /// 0,001 K und 0,1 W.
        /// </summary>
        [Fact]
        public void Probe_5a_Gauss_Seidel_trifft_den_Fixpunkt()
        {
            ProjektGebaeudeModel g = Gekoppelt(22.0, 20.0, 200.0);
            GebaeudeModellErgebnis[] a = Lauf(g, null);
            GebaeudeModellErgebnis[] b = Lauf(g, s => { s.Schwellen = (1e-11, 1e-8); s.Hoechstzahl = 1000; });
            double dK = 0.0, dW = 0.0;
            for (int z = 0; z < 2; z++)
                for (int h = 0; h < 8760; h++)
                {
                    dK = Math.Max(dK, Math.Abs(a[z].Raumtemperatur[h] - b[z].Raumtemperatur[h]));
                    dW = Math.Max(dW, Math.Abs(a[z].HeizlastW[h] - b[z].HeizlastW[h]));
                }
            _aus.WriteLine("Probe 5a: größte Abweichung vom Fixpunkt " + F(dK) + " K, " + F(dW) + " W");
            Assert.True(dK < 0.001, "Raumluft " + F(dK) + " K");
            Assert.True(dW < 0.1, "Heizlast " + F(dW) + " W");
        }

        private static GebaeudeModellErgebnis[] Lauf(ProjektGebaeudeModel g, Action<Zonenschleife> stellen)
        {
            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, ZonenschleifeTests.KlimaDes());
            var s = new Zonenschleife(zonen, "Probe");
            stellen?.Invoke(s);
            s.Vorlauf();
            s.Jahr();
            return s.Zonenergebnisse(0, 1);
        }

        /// <summary>
        /// <b>Probe 5b</b> (A8 (a)): Gauß-Seidel koppelt über das Stundenmittel, das 4×4-Gesamtsystem
        /// augenblicklich. Zwei frei schwingende Zonen (Sollwert −20 °C, nie erreicht) mit Trennwand der
        /// Außengruppe und Luftaustausch; das Gesamtsystem rechnet exakt diskretisiert je Stunde vom
        /// selben Zustand nach dem Vorlauf. Gemessen wird die größte Stundenabweichung der Raumluft;
        /// Kriterium &lt; 0,001 K (Anwenderentscheid vom 26.09.2026 nach der Messung, A8 (a); gemessen
        /// höchstens 1,5e‑4 K bei 400 m³/h).
        /// </summary>
        [Fact]
        public void Probe_5b_Gauss_Seidel_gegen_das_4x4_Gesamtsystem()
        {
            foreach (double strom in new[] { 0.0, 100.0, 400.0 })
            {
                ProjektGebaeudeModel g = Gekoppelt(-20.0, -20.0, strom);
                IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, ZonenschleifeTests.KlimaDes());
                var s = new Zonenschleife(zonen, "Probe 5b");
                s.Vorlauf();
                var x = new double[4];
                for (int z = 0; z < 2; z++)
                {
                    x[2 * z] = s.Laeufe[z].Modell.ThetaMAw;
                    x[2 * z + 1] = s.Laeufe[z].Modell.ThetaMIw;
                }
                s.Jahr();
                GebaeudeModellErgebnis[] r = s.Zonenergebnisse(0, 1);
                Assert.All(r, e => Assert.All(e.HeizlastW, w => Assert.Equal(0.0, w)));

                var system = new Gesamtsystem(zonen);
                double groesste = 0.0;
                for (int h = 0; h < 8760; h++)
                {
                    double[] luft = system.Stunde(h, x);
                    for (int z = 0; z < 2; z++) groesste = Math.Max(groesste, Math.Abs(luft[z] - r[z].Raumtemperatur[h]));
                }
                _aus.WriteLine("Probe 5b, Luftaustausch " + F(strom, "F0") + " m³/h: größte Stundenabweichung der Raumluft " + F(groesste) + " K");
                Assert.True(groesste < KRITERIUM_PROBE_5B_K, "Luftaustausch " + strom + " m³/h: " + F(groesste) + " K");
            }
        }

        /// <summary>
        /// Das 4×4-Gesamtsystem zweier frei schwingender Zonen mit augenblicklicher Kopplung, exakt
        /// diskretisiert (Matrixexponential) — die Netzgleichungen des Lösers, die algebraischen
        /// Knoten beider Zonen gemeinsam gelöst.
        /// </summary>
        private sealed class Gesamtsystem
        {
            private readonly IReadOnlyList<ZonenEingang> _z;
            private readonly double[,] _linv;       // 6×6: y = −L⁻¹ (P x + q)
            private readonly double[,] _p;          // 6×4
            private readonly double[,] _phi, _gamma, _psi;  // 4×4
            private readonly double[,] _nMat;       // 4×6 (Zustandsgleichung, Anteil der algebraischen Knoten)
            private readonly double[] _cMasse;      // g_Rest/(S·C1) je Zone für den Zähler
            private const double Tau = 3600.0;

            internal Gesamtsystem(IReadOnlyList<ZonenEingang> z)
            {
                _z = z;
                var l = new double[6, 6];
                _p = new double[6, 4];
                var m = new double[4, 4];
                _nMat = new double[4, 6];
                _cMasse = new double[2];
                for (int i = 0; i < 2; i++)
                {
                    ErsatzparameterRC p = z[i].Eingang.Parameter;
                    double g1 = 1.0 / p.R_1_AWGruppe_KW, gRest = 1.0 / p.R_Rest_AWGruppe_KW, g2 = 1.0 / p.R_1_IW_KW;
                    double gcAw = 1.0 / p.R_conv_AW_KW, gcIw = 1.0 / p.R_conv_IW_KW, gRad = 1.0 / p.R_rad_KW, gGes = 1.0 / p.R_ext_KW;
                    double gLuft = z[i].Eingang.Luftkopplungen.Count > 0 ? z[i].Eingang.Luftkopplungen[0].Leitwert_WK : 0.0;
                    double ua = z[i].Eingang.Nachbarglieder[0].UA_WK, s = z[i].Eingang.UaSummeGewichtung_WK;
                    int o = 3 * i, oj = 3 * (1 - i);
                    l[o, o] = -(g1 + gRad + gcAw); l[o, o + 1] = gRad; l[o, o + 2] = gcAw;
                    l[o + 1, o] = gRad; l[o + 1, o + 1] = -(g2 + gRad + gcIw); l[o + 1, o + 2] = gcIw;
                    l[o + 2, o] = gcAw; l[o + 2, o + 1] = gcIw; l[o + 2, o + 2] = -(gcAw + gcIw + gGes); l[o + 2, oj + 2] = gLuft;
                    _p[o, 2 * i] = g1;
                    _p[o + 1, 2 * i + 1] = g2;
                    m[2 * i, 2 * i] = -(gRest + g1) / p.C_AW_Jk;
                    m[2 * i + 1, 2 * i + 1] = -g2 / p.C_IW_Jk;
                    _nMat[2 * i, o] = g1 / p.C_AW_Jk;
                    _nMat[2 * i, oj + 2] = gRest * ua / (s * p.C_AW_Jk);
                    _nMat[2 * i + 1, o + 1] = g2 / p.C_IW_Jk;
                    _cMasse[i] = gRest / (s * p.C_AW_Jk);
                }
                _linv = Inverse(l);
                // A = M − N·L⁻¹·P
                double[,] a = Minus(m, Mal(Mal(_nMat, _linv), _p));
                _phi = Expm(a, Tau);
                double[,] ainv = Inverse(a);
                double[,] ident = Einheit(4);
                _gamma = Mal(ainv, Minus(_phi, ident));
                _psi = Mal(ainv, Minus(_gamma, Skaliert(ident, Tau)));
            }

            /// <summary>Rechnet Stunde <paramref name="h"/> vom Zustand <paramref name="x"/> (fortgeschrieben) und gibt die mittlere Raumluft je Zone.</summary>
            internal double[] Stunde(int h, double[] x)
            {
                var q = new double[6];
                var c = new double[4];
                for (int i = 0; i < 2; i++)
                {
                    GebaeudeModellEingang e = _z[i].Eingang;
                    double gLuft = e.Luftkopplungen.Count > 0 ? e.Luftkopplungen[0].Leitwert_WK : 0.0;
                    double gAussen = 1.0 / e.Parameter.R_ext_KW - gLuft;
                    int o = 3 * i;
                    q[o] = e.PhiRadAW[h];
                    q[o + 1] = e.PhiRadIW[h];
                    q[o + 2] = gAussen * e.ThetaOut[h] + e.PhiConv[h];
                    c[2 * i] = _cMasse[i] * e.ThetaEqZaehler[h];
                }
                // b = c − N·L⁻¹·q
                double[] lq = Mal(_linv, q);
                double[] b = new double[4];
                for (int r = 0; r < 4; r++)
                {
                    double v = c[r];
                    for (int k = 0; k < 6; k++) v -= _nMat[r, k] * lq[k];
                    b[r] = v;
                }
                double[] mittel = new double[4];
                double[] ende = new double[4];
                for (int r = 0; r < 4; r++)
                {
                    double sm = 0.0, se = 0.0;
                    for (int k = 0; k < 4; k++)
                    {
                        sm += _gamma[r, k] * x[k] + _psi[r, k] * b[k];
                        se += _phi[r, k] * x[k] + _gamma[r, k] * b[k];
                    }
                    mittel[r] = sm / Tau;
                    ende[r] = se;
                }
                Array.Copy(ende, x, 4);
                // Mittlere algebraische Knoten: y = −L⁻¹ (P x̄ + q).
                var px = new double[6];
                for (int r = 0; r < 6; r++)
                {
                    double v = q[r];
                    for (int k = 0; k < 4; k++) v += _p[r, k] * mittel[k];
                    px[r] = v;
                }
                double[] y = Mal(_linv, px);
                return new[] { -y[2], -y[5] };
            }

            private static double[,] Einheit(int n)
            {
                var e = new double[n, n];
                for (int i = 0; i < n; i++) e[i, i] = 1.0;
                return e;
            }

            private static double[,] Skaliert(double[,] a, double f)
            {
                int n = a.GetLength(0), m = a.GetLength(1);
                var r = new double[n, m];
                for (int i = 0; i < n; i++) for (int j = 0; j < m; j++) r[i, j] = a[i, j] * f;
                return r;
            }

            private static double[,] Minus(double[,] a, double[,] b)
            {
                int n = a.GetLength(0), m = a.GetLength(1);
                var r = new double[n, m];
                for (int i = 0; i < n; i++) for (int j = 0; j < m; j++) r[i, j] = a[i, j] - b[i, j];
                return r;
            }

            private static double[,] Mal(double[,] a, double[,] b)
            {
                int n = a.GetLength(0), k = a.GetLength(1), m = b.GetLength(1);
                var r = new double[n, m];
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                    {
                        double s = 0.0;
                        for (int t = 0; t < k; t++) s += a[i, t] * b[t, j];
                        r[i, j] = s;
                    }
                return r;
            }

            private static double[] Mal(double[,] a, double[] v)
            {
                int n = a.GetLength(0), k = a.GetLength(1);
                var r = new double[n];
                for (int i = 0; i < n; i++)
                {
                    double s = 0.0;
                    for (int t = 0; t < k; t++) s += a[i, t] * v[t];
                    r[i] = s;
                }
                return r;
            }

            /// <summary>Inverse nach Gauß-Jordan mit Spaltenpivot.</summary>
            private static double[,] Inverse(double[,] a)
            {
                int n = a.GetLength(0);
                var m = new double[n, 2 * n];
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++) m[i, j] = a[i, j];
                    m[i, n + i] = 1.0;
                }
                for (int c = 0; c < n; c++)
                {
                    int p = c;
                    for (int i = c + 1; i < n; i++) if (Math.Abs(m[i, c]) > Math.Abs(m[p, c])) p = i;
                    if (p != c) for (int j = 0; j < 2 * n; j++) (m[c, j], m[p, j]) = (m[p, j], m[c, j]);
                    double d = m[c, c];
                    for (int j = 0; j < 2 * n; j++) m[c, j] /= d;
                    for (int i = 0; i < n; i++)
                    {
                        if (i == c) continue;
                        double f = m[i, c];
                        if (f == 0.0) continue;
                        for (int j = 0; j < 2 * n; j++) m[i, j] -= f * m[c, j];
                    }
                }
                var r = new double[n, n];
                for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) r[i, j] = m[i, n + j];
                return r;
            }

            /// <summary>e^(A·t) über Skalieren und Quadrieren mit Taylorreihe.</summary>
            private static double[,] Expm(double[,] a, double t)
            {
                int n = a.GetLength(0);
                double[,] at = Skaliert(a, t);
                double norm = 0.0;
                for (int i = 0; i < n; i++)
                {
                    double s = 0.0;
                    for (int j = 0; j < n; j++) s += Math.Abs(at[i, j]);
                    norm = Math.Max(norm, s);
                }
                int quadrate = Math.Max(0, (int)Math.Ceiling(Math.Log(norm / 0.1, 2)));
                double[,] b = Skaliert(at, Math.Pow(2.0, -quadrate));
                double[,] summe = Einheit(n), glied = Einheit(n);
                for (int k = 1; k <= 20; k++)
                {
                    glied = Skaliert(Mal(glied, b), 1.0 / k);
                    summe = Plus(summe, glied);
                }
                for (int i = 0; i < quadrate; i++) summe = Mal(summe, summe);
                return summe;
            }

            private static double[,] Plus(double[,] a, double[,] b)
            {
                int n = a.GetLength(0), m = a.GetLength(1);
                var r = new double[n, m];
                for (int i = 0; i < n; i++) for (int j = 0; j < m; j++) r[i, j] = a[i, j] + b[i, j];
                return r;
            }
        }

        // =====================================================================
        //  Probe 6 — Vorstunde gegen Iteration
        // =====================================================================

        /// <summary>
        /// <b>Probe 6</b> (Mehrzonenkonzept 8.1, 2.4): Weg A (Kopplung über die Vorstunde) gegen Weg B
        /// (Gauß-Seidel) — am Gebäude mit unbeheiztem Keller und an zwei Zonen mit starkem
        /// Luftaustausch. Gemessen werden die größte Stundenabweichung der Raumluft und die Abweichung
        /// der Jahresenergie je Zone; das Ergebnis begründet die Wegwahl, es ist nicht ihre Voraussetzung.
        /// </summary>
        [Fact]
        public void Probe_6_Vorstunde_gegen_Iteration()
        {
            foreach ((string name, ProjektGebaeudeModel g) in new[]
            {
                ("Keller", ZonenEingangTests.MitKeller(out _)),
                ("Luftaustausch 400 m³/h", Gekoppelt(22.0, 18.0, 400.0)),
            })
            {
                GebaeudeModellErgebnis[] b = Lauf(g, null);
                GebaeudeModellErgebnis[] a = Lauf(g, s => s.VorstundeFuerProbe = true);
                var zeile = new List<string>();
                for (int z = 0; z < a.Length; z++)
                {
                    double dK = 0.0;
                    for (int h = 0; h < 8760; h++) dK = Math.Max(dK, Math.Abs(a[z].Raumtemperatur[h] - b[z].Raumtemperatur[h]));
                    double eB = b[z].JahresheizwaermeMwh;
                    double rel = eB > 0.0 ? a[z].JahresheizwaermeMwh / eB - 1.0 : double.NaN;
                    zeile.Add("Zone " + (z + 1) + ": " + F(dK, "F3") + " K, Jahresenergie " + (double.IsNaN(rel) ? "–" : F(rel)));
                    Assert.True(double.IsFinite(dK));
                }
                _aus.WriteLine("Probe 6, " + name + ": " + string.Join("; ", zeile));
            }
        }

        // =====================================================================
        //  4-K-Regel und Vorlauf
        // =====================================================================

        /// <summary>
        /// Die 4-K-Regel (Mehrzonenkonzept 2.2, A1 = M3 (b)): Zwei gleiche Hälften bleiben im adiabaten
        /// Vorlauf unter 4 K auseinander und koppeln adiabat (zwei Teilgruppen); mit verschiedenen Sollwerten
        /// (23 °C gegen 17 °C) liegen sie darüber und koppeln über die Außengruppe (eine Teilgruppe).
        /// </summary>
        [Fact]
        public void Die_4K_Regel_ordnet_nach_dem_adiabaten_Vorlauf()
        {
            Mehrzonenergebnis gleich = ZonenschleifeTests.Rechnen(ZonenschleifeTests.Haelften(Trennflaechenzuordnung.Regel));
            Zonenpaarzuordnung p = Assert.Single(gleich.Paare);
            Assert.Equal(Trennflaechenzuordnung.Innen, p.Gruppe);
            Assert.True(p.DeltaVorlaufK < 1e-9);
            Assert.Equal(2, gleich.Schleife.Gruppen.Count);

            ProjektGebaeudeModel g = Gekoppelt(23.0, 17.0, 0.0);
            g.Zonen = g.Zonen.Select(z => new GebaeudeZonensatz(z.ZonenId, z.Bezeichnung,
                z.Bauteile.Select(b => b.Rand == Bauteilrand.Zone ? b.MitZuordnung(Trennflaechenzuordnung.Regel) : b).ToList(),
                z.Nutzflaeche_M2, z.Eingaben, z.Rang)).ToList();
            Mehrzonenergebnis verschieden = ZonenschleifeTests.Rechnen(g);
            p = Assert.Single(verschieden.Paare);
            _aus.WriteLine("4-K-Regel: Δϑ Vorlauf " + F(p.DeltaVorlaufK, "F2") + " K, im Lauf " + F(p.DeltaLaufK, "F2") + " K, Gruppe " + p.Gruppe);
            Assert.Equal(Trennflaechenzuordnung.Aussen, p.Gruppe);
            Assert.Single(verschieden.Schleife.Gruppen);
            Assert.False(p.Ueberschritten);
        }

        /// <summary>
        /// A3 = M6 (a): Weicht die Wiederholung des Vorlaufs um mehr als die Probe ab, wird auf 90 Tage
        /// verlängert. Ein sehr träger Keller mit Luftaustausch zur Wohnung bleibt mit 0,05 K darunter
        /// (gemessen und ausgewiesen); die Verlängerung selbst prüft die Probe mit der Schwelle 0.
        /// </summary>
        [Fact]
        public void Der_Vorlauf_wird_nach_der_Probe_verlaengert()
        {
            ProjektGebaeudeModel g = ZonenEingangTests.MitKeller(out _);
            GebaeudeZonensatz keller = g.Zonen.Single(z => z.ZonenId == ZonenEingangTests.KELLER);
            var schwer = new Schicht(0.5, 2.0, 2400.0, 1000.0);
            var bauteile = new List<BauteilEingang>
            {
                new BauteilEingang("Kellerwände", Bauteilart.Aussenwand, 400.0, Bauteilrand.Erdreich, schichten: new[] { schwer, schwer }),
                new BauteilEingang("Kellerboden", Bauteilart.Bodenplatte, 88.0, Bauteilrand.Erdreich, schichten: new[] { schwer, schwer }),
            };
            g.Zonen = new[] { g.Zonen.Single(z => z.ZonenId == ZonenEingangTests.WOHNEN),
                              new GebaeudeZonensatz(keller.ZonenId, keller.Bezeichnung, bauteile, keller.Nutzflaeche_M2, keller.Eingaben, keller.Rang) };
            g.Zonenluftstroeme = new[] { new Zonenluftstrom(ZonenEingangTests.WOHNEN, ZonenEingangTests.KELLER, 300.0) };
            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, ZonenschleifeTests.KlimaDes());

            var regulaer = new Zonenschleife(zonen, "Vorlauf");
            regulaer.Vorlauf();
            _aus.WriteLine("Vorlauf: Abweichung der Wiederholung " + F(regulaer.VorlaufAbweichungK, "F4") + " K, " + regulaer.VorlaufStunden + " h");
            Assert.Equal(regulaer.VorlaufAbweichungK > Zonenschleife.VORLAUF_PROBE_K, regulaer.VorlaufVerlaengert);

            var streng = new Zonenschleife(ZonenEingang.Bauen(g, ZonenschleifeTests.KlimaDes()), "Vorlauf") { VorlaufProbeK = 0.0 };
            streng.Vorlauf();
            Assert.True(streng.VorlaufVerlaengert);
            Assert.Equal(2 * Vdi6007Rechenweg.VORLAUF_H + Zonenschleife.VORLAUF_LANG_H, streng.VorlaufStunden);
        }

        // =====================================================================
        //  Der interne Einstieg und die Laufgrenze
        // =====================================================================

        /// <summary>
        /// Der Lauf lehnt mehrere Zonen weiter benannt ab (Laufgrenze 1, Freigabe in W5); der interne
        /// Einstieg rechnet sie, legt die Summe in Zielpuffer und Träger und meldet AK1 als ideale Last
        /// (A4). Ein Kopplungsfehler kommt als Meldung der Stufe Fehler mit <c>false</c> zurück — der
        /// Bedarfslauf bricht ab (Festlegung 12).
        /// </summary>
        [Fact]
        public void Der_interne_Einstieg_rechnet_und_meldet_die_Laufgrenze_bleibt()
        {
            ProjektGebaeudeModel g = ZonenEingangTests.MitKeller(out _);
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            KlimakalenderGemeinsam gemeinsam = Vdi6007Probe.Kalender(ZonenschleifeTests.Klima);
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            {
                var traeger = new GebaeudeErgebnistraeger();
                var weg = new Vdi6007Rechenweg(traeger) { Anlagenkopplung = DbWerte.ANLAGENKOPPLUNG_AK1 };
                var ziel = new double[8760];

                // Die Laufgrenze: mehrere Zonen benannt abgelehnt.
                Assert.False(GebaeudeZonenregeln.Rechenbar(2));
                Assert.False(weg.Rechnen(g, 0, ziel, gemeinsam, out _));
                Assert.Contains(protokoll.Fehler, m => m.Contains(nameof(GebaeudeModellFehler.MehrereZonen)));

                // Der interne Einstieg.
                Assert.True(weg.RechnenMehrzonen(g, 0, ziel, gemeinsam, out double kwh));
                Mehrzonenergebnis m = weg.LetztesMehrzonenergebnis;
                Assert.Equal(m.Gebaeude.HeizlastW, ziel);
                Assert.Same(m.Gebaeude, traeger.Ergebnis(0));
                Assert.Equal(m.Gebaeude.VerbrauchAltKwh, kwh);
                Assert.Contains(m.Eingaenge, z => z.Eingang.KopplungAlsIdealeLast);
                Assert.All(m.Eingaenge, z => Assert.False(z.Eingang.KopplungWirksam));
                Assert.Contains(protokoll.Warnungen, x => x.Contains(string.Format(CultureInfo.CurrentCulture,
                    WindowsFormsApplication1.MyResource.Resource.SIMENG_G6_AK1_IDEAL, "Probegebäude (4711)", "2")));

                // Ein Kopplungsfehler: benannt, false.
                g.Zonenluftstroeme = new[] { new Zonenluftstrom(ZonenEingangTests.WOHNEN, 999, 10.0) };
                Assert.False(weg.RechnenMehrzonen(g, 0, ziel, gemeinsam, out _));
                Assert.Contains(protokoll.Fehler, x => x.Contains(nameof(GebaeudeModellFehler.ZonenkopplungUngueltig)));
            }
        }

        // =====================================================================
        //  Messprobe
        // =====================================================================

        /// <summary>
        /// <b>Messprobe</b> (Auftrag G6b, W4; MZ 2.9): synthetische Gebäude mit 10 und 50 Zonen —
        /// Wohnungen in einer Kette mit Trennwänden nach der 4-K-Regel, ein unbeheizter Keller unter der
        /// ersten, ein Treppenhaus mit Luftaustausch zu jeder Wohnung. Median aus drei Läufen;
        /// ausgewiesen ms je Zone und Jahr, Durchläufe (Mittel, Maximum) und der Anteil der Vorläufe.
        /// Ziel nach MZ 2.9: 1,1–2,0 s für 50 Zonen; rot erst ab dem Fünffachen (10 s).
        /// </summary>
        [Fact]
        public void Messprobe_10_und_50_Zonen()
        {
            foreach (int n in new[] { 10, 50 })
            {
                ProjektGebaeudeModel g = Synthetisch(n);
                var zeiten = new List<double>();
                Mehrzonenergebnis r = null;
                for (int i = 0; i < 3; i++)
                {
                    var uhr = Stopwatch.StartNew();
                    r = ZonenschleifeTests.Rechnen(g);
                    zeiten.Add(uhr.Elapsed.TotalMilliseconds);
                }
                zeiten.Sort();
                double median = zeiten[1];
                _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "Messprobe {0} Zonen: {1:F0} ms (Median aus 3), {2:F1} ms je Zone und Jahr; Durchläufe Mittel {3:F2}, max {4}; Vorläufe {5:P0}; Teilgruppen {6}; Paare IW/AW {7}/{8}",
                    n, median, median / n, r.Schleife.DurchlaeufeMittel, r.Schleife.DurchlaeufeMax,
                    r.ZeitVorlaeufeMs / r.ZeitGesamtMs, r.Schleife.Gruppen.Count,
                    r.Paare.Count(p => p.Gruppe == Trennflaechenzuordnung.Innen), r.Paare.Count(p => p.Gruppe == Trennflaechenzuordnung.Aussen)));
                Assert.Equal(n, r.Zonen.Count);
                if (n == 50) Assert.True(median < 5 * 2000.0, "50 Zonen: " + F(median, "F0") + " ms, mehr als das Fünffache von 2 s.");
            }
        }

        /// <summary>Das synthetische Gebäude der Messprobe mit <paramref name="n"/> Zonen (Klassenkopf).</summary>
        internal static ProjektGebaeudeModel Synthetisch(int n)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(g);
            int wohnungen = n - 2;
            double f = 1.0 / wohnungen;
            const int keller = 1000, treppe = 1001;
            var zonen = new List<GebaeudeZonensatz>();
            var stroeme = new List<Zonenluftstrom>();
            for (int i = 0; i < wohnungen; i++)
            {
                int id = i + 1;
                List<BauteilEingang> b = basis.Bauteile.Select(x => Skaliert(x, f)).ToList();
                if (i + 1 < wohnungen)
                    b.Add(new BauteilEingang("Trennwand " + id, Bauteilart.Innenwand, 20.0, Bauteilrand.Zone,
                                             schichten: new[] { Putz, Innenmauerwerk, Putz }, idNachbarzone: id + 1));
                if (i == 0)
                    b.Add(new BauteilEingang("Kellerdecke", Bauteilart.Decke, 40.0, Bauteilrand.Zone, neigungGrad: 180.0,
                                             schichten: new[] { Estrich, Daemmung, Beton }, idNachbarzone: keller));
                double soll = 19.0 + (i % 4);
                zonen.Add(new GebaeudeZonensatz(id, "Wohnung " + id, b, g.Nutzflaeche * f,
                                                new Zoneneingaben(Nutzflaeche: g.Nutzflaeche * f, SollTag: soll), id));
                stroeme.Add(new Zonenluftstrom(id, treppe, 30.0));
            }
            zonen.Add(new GebaeudeZonensatz(keller, "Keller", new List<BauteilEingang>
            {
                new BauteilEingang("Kellerwände", Bauteilart.Aussenwand, 60.0, Bauteilrand.Erdreich, schichten: new[] { Beton }),
                new BauteilEingang("Kellerboden", Bauteilart.Bodenplatte, 40.0, Bauteilrand.Erdreich, schichten: new[] { Estrich, Beton }),
            }, 40.0, new Zoneneingaben(Nutzflaeche: 40.0, IstBeheizt: false), wohnungen + 1));
            zonen.Add(new GebaeudeZonensatz(treppe, "Treppenhaus", new List<BauteilEingang>
            {
                new BauteilEingang("Treppenhauswand", Bauteilart.Aussenwand, 60.0, Bauteilrand.Aussenluft,
                                   schichten: new[] { Putz, Mauerwerk, Putz }, azimutGrad: 0.0),
                new BauteilEingang("Treppenhausfenster", Bauteilart.Fenster, 6.0, Bauteilrand.Aussenluft, 2.8, azimutGrad: 0.0),
            }, 20.0, new Zoneneingaben(Nutzflaeche: 20.0, IstBeheizt: false), wohnungen + 2));
            g.Zonen = zonen;
            g.Zonenluftstroeme = stroeme;
            return g;
        }

        private static BauteilEingang Skaliert(BauteilEingang b, double f)
            => new BauteilEingang(b.Bezeichnung, b.Art, f * b.Flaeche_M2, b.Rand, b.UWert_WM2K, b.Schichten, b.NeigungGrad, b.AzimutGrad,
                                  b.GWert, b.Rahmenanteil, b.Verschattungsfaktor, f * b.PsiL_WK, b.AlphaKonInnen_WM2K, b.AlphaKonAussen_WM2K);
    }
}
