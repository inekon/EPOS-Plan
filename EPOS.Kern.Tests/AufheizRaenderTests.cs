using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Ränder des Aufheizplans einer Zone</b> (Entwurf KP3 Abschnitt 7, Welle R2; Teilkonzept 4.1,
    /// 4.5–4.7): N-AH6 „unerreichbar" und N-AH10 ohne Zonen — Sprung am 1. Januar über den Ring, D &lt; n
    /// (W2), Übergang aus „aus" (W4), Beginn der Heizperiode (W4, D ab 00:00), Bemessung innerhalb der
    /// Heizperiode, Deckel 48 am Montag nach dem Büro-Wochenende (D = 61), Kühlkappung an θ_K − 1 K,
    /// AK1-Gebäude (W5), gestufte Absenkung nach E58 F2 (b), Überlappung zweier Rampen, ΔT ≤ 0,01 K und
    /// „fest" mit t_auf,max = 0. <b>Mit Zonen</b> (Welle R3): AK1-Gebäude mit Zonen (W5) samt unbeheizter Zone
    /// im selben Gebäude, Zone ohne Kalender (B5), beide Aufbauten der Zonen, Schalter aus, Determinismus.
    ///
    /// <para><b>Synthetisch:</b> der Prüfsatz des Hauses aus 1045 (<see cref="AufheizantwortTests.Pruefsatz"/>)
    /// mit Strahlungsanteil 0,3, Außenluft als einzige Randtemperatur (θ_eq = T_a, ohne Erdreich),
    /// Zusatzleitwert 0; die Grenze P_auf (Quelle <c>Heizleistung_Max</c>, Augenblicksform) wird aus der
    /// Stufenformel so gesetzt, dass der Bemessungsfall ein bestimmtes n verlangt. Jede Probe prüft
    /// zusätzlich die Reihe gegen die Vorschrift der Festlegung 8, nachgerechnet aus der Sprungliste.</para>
    /// </summary>
    public class AufheizRaenderTests
    {
        internal const double ANTEIL = 0.3;
        private const int STUNDEN = 8760;

        // =====================================================================
        //  Bausteine
        // =====================================================================

        internal static Zonenmodell2K Modell() => new Zonenmodell2K(AufheizantwortTests.Pruefsatz(), "Prüfsatz");

        internal static double[] Konstant(double wert)
        {
            var r = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++) r[h] = wert;
            return r;
        }

        /// <summary>Tag/Nacht: <paramref name="tag"/> in [<paramref name="beginn"/>, <paramref name="ende"/>), sonst <paramref name="nacht"/>.</summary>
        internal static double[] TagNacht(double tag, double nacht, int beginn, int ende)
        {
            var r = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++) r[h] = h % 24 >= beginn && h % 24 < ende ? tag : nacht;
            return r;
        }

        internal static Aufheizzone Zone(double[] soll, double aussenC = -12.0, double heizMaxW = double.NaN,
                                        double[] kuehl = null, double[] aussen = null)
            => new Aufheizzone
            {
                Bezeichnung = "Prüfsatz",
                Soll = soll,
                Kuehl = kuehl,
                Aussen = aussen ?? Konstant(aussenC),
                AequivalentN = (tag, ta) => ta,
                Zusatzleitwert = h => 0.0,
                AuslegungZusatzleitwertWK = 0.0,
                Strahlungsanteil = ANTEIL,
                HeizleistungMaxW = heizMaxW,
                Nutzungszeit = h => h % 24 >= 6 && h % 24 < 22,
                Gekoppelt = false,
                Modell = Modell(),
            };

        internal static Aufheizvorgabe An(bool fest = false, bool abzug = false)
            => new Aufheizvorgabe(true, abzug ? DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG : null, null, null,
                                  fest ? DbWerte.AUFHEIZ_ART_FEST : null);

        internal static double PhiStat(double thetaT, double ta) => Modell().StationaereHeizlastW(thetaT, ta, ta, ANTEIL, 0.0);

        /// <summary>Die Grenze, bei der ein Sprung um <paramref name="deltaT"/> genau n Stufen braucht (Augenblicksform).</summary>
        internal static double Grenze(double thetaT, double deltaT, double ta, int n)
            => Aufheizstufen.AugenblickW(Modell().Aufheizantwort(ANTEIL, 0.0), PhiStat(thetaT, ta), deltaT, n);

        internal static int Ring(int h) => ((h % STUNDEN) + STUNDEN) % STUNDEN;

        /// <summary>
        /// Die Vorschrift der Festlegung 8, aus der Sprungliste nachgerechnet: s'(h) = max(s(h), min(θ_N +
        /// ΔT·j/n, θ_K(h) − 1 K)), Überlappung über max; die Sprungstunde und jede Stunde „aus" bleiben;
        /// die Rampenmaske ist s' &gt; s; die Kühlprüfung θ_K ≥ s' + 1 K hält, wo beide endlich sind.
        /// </summary>
        internal static void Vorschrift(Aufheizzone z, Aufheizplan p)
        {
            double[] s = z.Soll;
            var e = (double[])s.Clone();
            foreach (Aufheizsprung sp in p.Spruenge)
                for (int j = 1; j < sp.N; j++)
                {
                    int h = Ring(sp.Sprungstunde - sp.N + j);
                    double w = sp.ThetaNC + sp.DeltaTK * j / sp.N;
                    double k = z.Kuehl == null ? double.PositiveInfinity : z.Kuehl[h];
                    if (!double.IsInfinity(k))
                    {
                        double kappe = Aufheizoptimierung.Kuehlkappe(k);
                        if (kappe < w) w = kappe;
                    }
                    if (w > e[h]) e[h] = w;
                }
            int maske = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                Assert.True(BitConverter.DoubleToInt64Bits(e[h]) == BitConverter.DoubleToInt64Bits(p.Reihe[h]),
                    string.Format(CultureInfo.InvariantCulture, "Stunde {0}: Vorschrift {1:R}, Plan {2:R}", h, e[h], p.Reihe[h]));
                Assert.Equal(e[h] > s[h], p.Rampenmaske[h]);
                if (p.Rampenmaske[h]) maske++;
                if (double.IsNaN(s[h])) Assert.True(double.IsNaN(p.Reihe[h]), "„aus\" beschrieben in Stunde " + h);
                double kk = z.Kuehl == null ? double.PositiveInfinity : z.Kuehl[h];
                if (!double.IsInfinity(kk) && !double.IsNaN(p.Reihe[h]))
                    Assert.True(kk >= p.Reihe[h] + GebaeudeFestwerte.KUEHLSOLLWERT_ABSTAND_K, "Kühlprüfung in Stunde " + h);
            }
            // Die eigene Rampe schreibt ihre Sprungstunde nie (j < n); liegt sie im Fenster eines späteren
            // Sprungs, hebt dessen Rampe sie über das Maximum (gestufte Absenkung) — dann zeigt es die Vorschrift.
            foreach (Aufheizsprung sp in p.Spruenge)
                if (!p.Spruenge.Any(x => x.Sprungstunde != sp.Sprungstunde && Ring(sp.Sprungstunde - x.Sprungstunde + x.N - 1) < x.N - 1))
                    Assert.True(BitConverter.DoubleToInt64Bits(s[sp.Sprungstunde]) == BitConverter.DoubleToInt64Bits(p.Reihe[sp.Sprungstunde]),
                        "Sprungstunde beschrieben: " + sp.Sprungstunde);
            Assert.Equal(maske, p.MaskenstundenH);
            Assert.Equal(maske > 0, p.Geaendert);
            if (!p.Geaendert) Assert.Same(s, p.Reihe);
            Assert.Equal(p.Spruenge.Sum(x => x.N - 1), p.AufheizstundenH);
        }

        /// <summary>Der Büro-Kalender: werktags 7–18 Uhr <paramref name="tag"/>, sonst <paramref name="absenkung"/>; Wochenende ganztägig abgesenkt.</summary>
        internal static double[] Buero(double tag, double absenkung, out bool[] wochenende)
        {
            wochenende = Vdi6007Probe.Wochenende();
            var r = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++)
                r[h] = !wochenende[h / 24] && h % 24 >= 7 && h % 24 < 18 ? tag : absenkung;
            return r;
        }

        internal static bool IstMontag(bool[] wochenende, int tag) => !wochenende[tag] && wochenende[(tag + 364) % 365];

        // =====================================================================
        //  N-AH6 Unerreichbar
        // =====================================================================

        /// <summary>
        /// <b>N-AH6:</b> P_auf = 0,95·Φ_stat (Grenze unter der stationären Last, Büro-Kalender, konstant
        /// −12 °C): W1 an jedem Sprungtag samt Unterzahl „P_auf ≤ Φ_stat", n = min(D + 1, 48), kein W2;
        /// Zustand UNERREICHBAR, t_auf,max fehlt in beiden Varianten, 47 als Obergrenze im Lauf.
        /// </summary>
        [Fact]
        public void N_AH6_Unerreichbar_rampt_die_ganze_Absenkung_bis_zum_Deckel()
        {
            double[] soll = Buero(20.0, 16.0, out bool[] we);
            Aufheizzone z = Zone(soll, heizMaxW: 0.95 * PhiStat(20.0, -12.0));
            Aufheizplan p = Aufheizoptimierung.Planen(z, An());

            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, p.Zustand);
            Assert.Equal(DbWerte.AUFHEIZ_QUELLE_GRENZE, p.Bemessung.Quelle);
            Assert.False(p.Bemessung.VarianteA.Erreichbar);
            Assert.Null(p.Bemessung.VarianteA.AufheizzeitMaxH);
            Assert.Null(p.Bemessung.VarianteB.AufheizzeitMaxH);
            Assert.Equal(47, p.Bemessung.ObergrenzeH);

            int werktage = Enumerable.Range(0, 365).Count(d => !we[d]);
            Assert.Equal(werktage, p.Spruenge.Count);
            Assert.Equal(werktage, p.TageUnerreichbar);
            Assert.Equal(werktage, p.TageUnterStationaer);
            Assert.Equal(0, p.TageBegrenzt);
            Assert.Equal(0, p.SpruengeAus);
            int montage = 0;
            foreach (Aufheizsprung sp in p.Spruenge)
            {
                Assert.True(sp.Unerreichbar && sp.UnterStationaer && !sp.Begrenzt);
                Assert.Equal(Aufheizoptimierung.DECKEL + 1, sp.BedarfN);
                Assert.Equal(Math.Min(sp.AbsenkdauerH + 1, Aufheizoptimierung.DECKEL), sp.N);
                if (IstMontag(we, sp.Sprungstunde / 24))
                {
                    montage++;
                    Assert.Equal(61, sp.AbsenkdauerH);
                    Assert.Equal(48, sp.N);
                }
                else
                {
                    Assert.Equal(13, sp.AbsenkdauerH);
                    Assert.Equal(14, sp.N);
                }
                Assert.Equal(16.0, sp.ThetaNC);
            }
            Assert.True(montage >= 52);
            Assert.Equal(47, p.LaengsteRampeH);
            Vorschrift(z, p);
        }

        // =====================================================================
        //  N-AH10 Ränder
        // =====================================================================

        /// <summary><b>Sprung am 1. Januar über den Ring</b> (Festlegung 6, B8): D und Rampe laufen über 8 759 → 0.</summary>
        [Fact]
        public void N_AH10_Sprung_am_1_Januar_rampt_ueber_den_Jahreswechsel()
        {
            double[] soll = Konstant(20.0);
            for (int h = 8752; h < STUNDEN; h++) soll[h] = 17.0;
            Aufheizzone z = Zone(soll, heizMaxW: Grenze(20.0, 3.0, -12.0, 4));
            Aufheizplan p = Aufheizoptimierung.Planen(z, An());

            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, p.Zustand);
            Assert.Equal(3, p.Bemessung.VarianteA.AufheizzeitMaxH);
            Assert.Single(p.Spruenge);
            Aufheizsprung sp = p.Spruenge[0];
            Assert.Equal(0, sp.Sprungstunde);
            Assert.Equal(8, sp.AbsenkdauerH);
            Assert.Equal(4, sp.N);
            int[] gerampt = Enumerable.Range(0, STUNDEN).Where(h => p.Rampenmaske[h]).ToArray();
            Assert.Equal(new[] { 8757, 8758, 8759 }, gerampt);
            Assert.Equal(17.75, p.Reihe[8757]);
            Assert.Equal(18.5, p.Reihe[8758]);
            Assert.Equal(19.25, p.Reihe[8759]);
            Assert.Equal(1, p.Aufheiztage);
            Vorschrift(z, p);
        }

        /// <summary><b>D &lt; n (W2):</b> Die Rampe füllt die Absenkung, der Tag zählt als begrenzt (Festlegung 18).</summary>
        [Fact]
        public void N_AH10_Kurze_Absenkung_begrenzt_die_Rampe_W2()
        {
            double[] soll = Konstant(20.0);
            for (int h = 0; h < STUNDEN; h++) if (h % 24 == 4 || h % 24 == 5) soll[h] = 17.0;
            Aufheizzone z = Zone(soll, heizMaxW: Grenze(20.0, 3.0, -12.0, 4));
            Aufheizplan p = Aufheizoptimierung.Planen(z, An());

            Assert.Equal(3, p.Bemessung.VarianteA.AufheizzeitMaxH);
            Assert.Equal(365, p.Spruenge.Count);
            foreach (Aufheizsprung sp in p.Spruenge)
            {
                Assert.Equal(2, sp.AbsenkdauerH);
                Assert.Equal(4, sp.BedarfN);
                Assert.Equal(3, sp.N);
                Assert.True(sp.Begrenzt && !sp.Unerreichbar);
            }
            Assert.Equal(365, p.TageBegrenzt);
            Assert.Equal(0, p.TageUnerreichbar);
            Assert.Equal(2, p.KuerzesteAbsenkdauerH);
            Vorschrift(z, p);
        }

        /// <summary><b>Übergang aus „aus" (W4):</b> keine Rampe, der Übergang wird gezählt (Festlegung 7).</summary>
        [Fact]
        public void N_AH10_Uebergang_aus_aus_bekommt_keine_Rampe_W4()
        {
            double[] soll = Konstant(20.0);
            for (int h = 0; h < STUNDEN; h++) if (h % 24 < 6) soll[h] = double.NaN;
            Aufheizzone z = Zone(soll, heizMaxW: Grenze(20.0, 3.0, -12.0, 4));
            Aufheizplan p = Aufheizoptimierung.Planen(z, An());

            Assert.Empty(p.Spruenge);
            Assert.Equal(365, p.SpruengeAus);
            Assert.Equal(0, p.SpruengeAusHeizperiode);
            Assert.False(p.Geaendert);
            Assert.Same(soll, p.Reihe);
            Assert.Equal(0, p.Bemessung.VarianteA.AufheizzeitMaxH);
            Vorschrift(z, p);
        }

        /// <summary>
        /// <b>Beginn der Heizperiode und Bemessung innerhalb der Heizperiode</b> (4.7): Der Übergang aus
        /// „aus" um 00:00 bekommt keine Rampe (W4, Unterzahl Heizperiode); D des ersten Morgens zählt ab
        /// 00:00 (D = 6 statt 8, W2); T_a,B ist die kälteste Stunde mit Heizsollwert, nicht des Jahres.
        /// </summary>
        [Fact]
        public void N_AH10_Beginn_der_Heizperiode_und_Bemessung_in_der_Heizperiode()
        {
            const int beginn = 100;
            double[] soll = TagNacht(20.0, 18.0, 6, 22);
            var aussen = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++)
            {
                bool drin = h / 24 >= beginn;
                if (!drin) soll[h] = double.NaN;
                aussen[h] = drin ? -5.0 : -30.0;
            }
            Aufheizzone z = Zone(soll, heizMaxW: Grenze(20.0, 2.0, -5.0, 8), aussen: aussen);
            Aufheizplan p = Aufheizoptimierung.Planen(z, An());

            Assert.Equal(-5.0, p.Bemessung.AussenMinC);
            Assert.Equal(-5.0, p.Bemessung.VarianteA.AussenC);
            Assert.Equal(-7.0, p.Bemessung.VarianteB.AussenC);
            Assert.Equal(7, p.Bemessung.VarianteA.AufheizzeitMaxH);
            Assert.Equal(1, p.SpruengeAus);
            Assert.Equal(1, p.SpruengeAusHeizperiode);
            Assert.Equal(365 - beginn, p.Spruenge.Count);

            Aufheizsprung erster = p.Spruenge[0];
            Assert.Equal(beginn * 24 + 6, erster.Sprungstunde);
            Assert.Equal(6, erster.AbsenkdauerH);
            Assert.Equal(7, erster.N);
            Assert.True(erster.Begrenzt);
            Assert.True(p.Rampenmaske[beginn * 24]);
            Assert.True(double.IsNaN(p.Reihe[beginn * 24 - 1]));
            foreach (Aufheizsprung sp in p.Spruenge.Skip(1))
            {
                Assert.Equal(8, sp.AbsenkdauerH);
                Assert.Equal(8, sp.N);
                Assert.False(sp.Begrenzt);
            }
            Assert.Equal(1, p.TageBegrenzt);
            Vorschrift(z, p);
        }

        /// <summary>
        /// <b>Deckel 48 am Montag nach dem Büro-Wochenende</b> (D = 61, Teilkonzept 4.6): Der Bemessungsfall
        /// verlangt genau 48 Stufen (t_auf,max = 47); der Montag rampt 47 Stunden, nicht 61, und ist nicht
        /// begrenzt; die Werktage füllen ihre Absenkung (D = 13, W2).
        /// </summary>
        [Fact]
        public void N_AH10_Deckel_48_am_Montag_nach_dem_Buerowochenende()
        {
            double[] soll = Buero(20.0, 16.0, out bool[] we);
            Aufheizzone z = Zone(soll, heizMaxW: Grenze(20.0, 4.0, -12.0, 48));
            Aufheizplan p = Aufheizoptimierung.Planen(z, An());

            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, p.Zustand);
            Assert.Equal(47, p.Bemessung.VarianteA.AufheizzeitMaxH);
            int montage = 0;
            foreach (Aufheizsprung sp in p.Spruenge)
            {
                Assert.False(sp.Unerreichbar);
                if (IstMontag(we, sp.Sprungstunde / 24))
                {
                    montage++;
                    Assert.Equal(61, sp.AbsenkdauerH);
                    Assert.Equal(48, sp.N);
                    Assert.False(sp.Begrenzt);
                    Assert.Equal(47, sp.GeschriebeneStunden);
                    Assert.False(p.Rampenmaske[Ring(sp.Sprungstunde - 48)]);
                    Assert.True(p.Rampenmaske[Ring(sp.Sprungstunde - 47)]);
                }
                else
                {
                    Assert.Equal(14, sp.N);
                    Assert.True(sp.Begrenzt);
                }
            }
            Assert.True(montage >= 52);
            Assert.Equal(47, p.LaengsteRampeH);
            Assert.Equal(0, p.TageBemessungBegrenzt);
            Vorschrift(z, p);
        }

        /// <summary>
        /// <b>Kühlkappung mit Kühlkalender</b> (Festlegung 8, F17): Die Rampe wird an θ_K(h) − 1 K gekappt,
        /// jede gekappte Stunde gezählt, und die stündliche Kühlprüfung θ_K ≥ θ_H + 1 K hält danach.
        /// </summary>
        [Fact]
        public void N_AH10_Kuehlkappung_haelt_die_stuendliche_Kuehlpruefung()
        {
            double[] soll = TagNacht(20.0, 18.0, 6, 22);
            double[] kuehl = Konstant(double.PositiveInfinity);
            for (int h = 0; h < STUNDEN; h++) if (h % 24 >= 3 && h % 24 < 6) kuehl[h] = 19.5;
            Aufheizzone z = Zone(soll, heizMaxW: Grenze(20.0, 2.0, -12.0, 5), kuehl: kuehl);
            Aufheizplan p = Aufheizoptimierung.Planen(z, An());

            Assert.Equal(4, p.Bemessung.VarianteA.AufheizzeitMaxH);
            Assert.All(p.Spruenge, sp => Assert.Equal(5, sp.N));
            Assert.Equal(3 * 365, p.KuehlgekappteStundenH);
            Assert.Equal(18.4, p.Reihe[2], 12);
            Assert.Equal(18.5, p.Reihe[3]);
            Assert.Equal(18.5, p.Reihe[5]);
            Vorschrift(z, p);
        }

        /// <summary><b>AK1-Gebäude (W5):</b> benannt nicht optimiert, die Reihe des Eingangs bleibt dieselbe Instanz (F13).</summary>
        [Fact]
        public void N_AH10_Gekoppeltes_Gebaeude_bleibt_unveraendert_W5()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            SolardatenModel[] klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(g, klima, Vdi6007Probe.Wochenende(),
                Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false,
                DbWerte.ANLAGENKOPPLUNG_AK1, double.NaN, 1.0);
            Assert.True(e.KopplungWirksam);
            double[] vorher = e.ThetaSoll;
            double[] kopie = (double[])vorher.Clone();

            Aufheizplan p = Aufheizoptimierung.Anwenden(ZonenEingang.Einzeln(e), An());
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, p.Zustand);
            Assert.True(p.Gekoppelt);
            Assert.Null(p.Bemessung);
            Assert.False(p.Geaendert);
            Assert.Same(vorher, e.ThetaSoll);
            Assert.Same(vorher, p.Reihe);
            Assert.Equal(kopie, e.ThetaSoll);

            // Dieselbe Zone ohne Kopplung rampt (Gegenprobe der Probe).
            Aufheizzone frei = Aufheizzone.Aus(ZonenEingang.Einzeln(Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), klima)));
            Aufheizplan q = Aufheizoptimierung.Planen(frei with { HeizleistungMaxW = 1.02 * PhiStatZone(frei) }, An());
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, q.Zustand);
            Assert.True(q.Geaendert);
        }

        private static double PhiStatZone(Aufheizzone z)
        {
            double ta = z.Aussen.Min();
            int kalt = Array.IndexOf(z.Aussen, ta);
            return z.Modell.StationaereHeizlastW(20.0, ta, z.AequivalentN(kalt / 24, ta), z.Strahlungsanteil, 0.0);
        }

        /// <summary>
        /// <b>Gestufte Absenkung nach E58 F2 (b)</b> und <b>Überlappung zweier Rampen</b>: Wochenende 16 °C,
        /// Nacht 18 °C, Tag 20 °C — Montag 0 Uhr 16 → 18 und 6 Uhr 18 → 20 °C. Der 6-Uhr-Sprung rampt ab
        /// dem kleinsten Sollwert des Fensters (16 °C), nicht ab s(h_s − 1) = 18 °C; beide Rampen liegen in
        /// den Sonntagsstunden übereinander, es gilt das Maximum.
        /// </summary>
        [Fact]
        public void N_AH10_Gestufte_Absenkung_und_Ueberlappung()
        {
            bool[] we = Vdi6007Probe.Wochenende();
            double[] soll = TagNacht(20.0, 18.0, 6, 22);
            for (int h = 0; h < STUNDEN; h++) if (we[h / 24]) soll[h] = 16.0;
            Aufheizzone z = Zone(soll, heizMaxW: Grenze(20.0, 4.0, -12.0, 20));
            Aufheizplan p = Aufheizoptimierung.Planen(z, An());

            Assert.Equal(19, p.Bemessung.VarianteA.AufheizzeitMaxH);
            Assert.Equal(16.0, p.Bemessung.VarianteA.ThetaNC);
            int geprueft = 0, ueberlappt = 0;
            foreach (Aufheizsprung sp in p.Spruenge)
            {
                int tag = sp.Sprungstunde / 24;
                if (!IstMontag(we, tag)) continue;
                if (sp.Sprungstunde % 24 == 6)
                {
                    Assert.Equal(16.0, sp.ThetaNC);
                    Assert.Equal(18.0, soll[sp.Sprungstunde - 1]);
                    Assert.Equal(20, sp.N);
                    Aufheizsprung null_uhr = p.Spruenge.Single(x => x.Sprungstunde == tag * 24);
                    Assert.Equal(18.0, null_uhr.ThetaTC);
                    Assert.Equal(16.0, null_uhr.ThetaNC);
                    Assert.True(null_uhr.N > 1, "Der 0-Uhr-Sprung rampt nicht.");
                    // Die Sprungstunde 0 Uhr liegt im Fenster der 6-Uhr-Rampe: deren Wert 16 + 4·14/20 gilt (max).
                    Assert.Equal(16.0 + 4.0 * 14 / 20, p.Reihe[tag * 24]);
                    // Die Sonntagsstunden vor 0 Uhr: beide Rampen schreiben, es gilt das Maximum.
                    for (int k = 1; k < null_uhr.N && k <= 3; k++)
                    {
                        int h = tag * 24 - k;
                        double a = 16.0 + 2.0 * (null_uhr.N - k) / null_uhr.N;
                        double b = 16.0 + 4.0 * (20 - 6 - k) / 20;
                        Assert.Equal(Math.Max(a, b), p.Reihe[h]);
                        ueberlappt++;
                    }
                    geprueft++;
                }
            }
            Assert.True(geprueft >= 52);
            Assert.True(ueberlappt > 0);
            foreach (Aufheizsprung sp in p.Spruenge.Where(x => !IstMontag(we, x.Sprungstunde / 24)))
                Assert.Equal(18.0, sp.ThetaNC);
            Vorschrift(z, p);
        }

        /// <summary><b>ΔT ≤ 0,01 K ist kein Sprung</b> (Festlegung 7, über den Rand).</summary>
        [Fact]
        public void N_AH10_Kein_Sprung_bis_ein_Hundertstel_Kelvin()
        {
            Assert.False(Aufheizoptimierung.IstSprung(19.99, 20.0));
            Assert.False(Aufheizoptimierung.IstSprung(20.0, 20.01));
            Assert.False(Aufheizoptimierung.IstSprung(20.0, 20.0));
            Assert.True(Aufheizoptimierung.IstSprung(19.98, 20.0));

            foreach (double nacht in new[] { 19.995, 19.99 })
            {
                double[] soll = TagNacht(20.0, nacht, 6, 22);
                Aufheizzone z = Zone(soll, heizMaxW: Grenze(20.0, 0.01, -12.0, 4));
                Aufheizplan p = Aufheizoptimierung.Planen(z, An());
                Assert.Empty(p.Spruenge);
                Assert.Same(soll, p.Reihe);
                Vorschrift(z, p);
            }
            Aufheizplan mit = Aufheizoptimierung.Planen(Zone(TagNacht(20.0, 19.98, 6, 22), heizMaxW: 1e9), An());
            Assert.Equal(365, mit.Spruenge.Count);
        }

        /// <summary>
        /// <b>„fest" mit t_auf,max = 0</b> (Teilkonzept 4.6): n = min(t_auf,max + 1, D + 1) = 1, keine Rampe.
        /// Dazu fest ≥ täglich je Sprung mit t_auf,max &gt; 0 über ein Jahr mit Tagesgang (N-AH5).
        /// </summary>
        [Fact]
        public void N_AH10_Fest_mit_Aufheizzeit_null_rampt_nicht_und_fest_ist_nie_kuerzer_als_taeglich()
        {
            double[] soll = TagNacht(20.0, 17.0, 6, 22);
            Aufheizplan null_h = Aufheizoptimierung.Planen(Zone(soll, heizMaxW: 1e9), An(fest: true));
            Assert.Equal(0, null_h.Bemessung.VarianteA.AufheizzeitMaxH);
            Assert.All(null_h.Spruenge, sp => Assert.Equal(1, sp.N));
            Assert.False(null_h.Geaendert);
            Assert.Equal(0, null_h.Aufheiztage);

            var aussen = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++) aussen[h] = Vdi6007Probe.Jahresgang(h);
            double ta = aussen.Min();
            Aufheizzone z = Zone(soll, heizMaxW: Grenze(20.0, 3.0, ta, 4), aussen: aussen);
            Aufheizplan fest = Aufheizoptimierung.Planen(z, An(fest: true));
            Aufheizplan taeglich = Aufheizoptimierung.Planen(z, An());
            Assert.Equal(3, fest.Bemessung.VarianteA.AufheizzeitMaxH);
            Assert.Equal(fest.Spruenge.Count, taeglich.Spruenge.Count);
            for (int i = 0; i < fest.Spruenge.Count; i++)
            {
                Assert.Equal(4, fest.Spruenge[i].N);
                Assert.True(fest.Spruenge[i].N >= taeglich.Spruenge[i].N);
            }
            Assert.Equal(365, fest.Aufheiztage);
            Assert.True(taeglich.Aufheiztage < fest.Aufheiztage);
            Assert.True(taeglich.Aufheiztage > 0);
            Vorschrift(z, fest);
            Vorschrift(z, taeglich);
        }

        // =====================================================================
        //  N-AH10 mit Zonen (Welle R3)
        // =====================================================================

        private static int Stelle(IReadOnlyList<ZonenEingang> zonen, int id) => AufheizMehrzonenTests.Stelle(zonen, id);

        private static void Bitgleich(Mehrzonenergebnis a, Mehrzonenergebnis b, string wo)
        {
            Assert.Equal(a.Zonen.Count, b.Zonen.Count);
            AufheizGrenzfallTests.Bitgleich(a.Gebaeude, b.Gebaeude, wo + ", Gebäude");
            for (int z = 0; z < a.Zonen.Count; z++)
                AufheizGrenzfallTests.Bitgleich(a.Zonen[z], b.Zonen[z], wo + ", " + a.Eingaenge[z].Bezeichnung);
        }

        /// <summary>
        /// <b>AK1-Gebäude mit Zonen (W5) und eine unbeheizte Zone im selben Gebäude:</b> Mit wirksamer Stufe
        /// rechnen die Zonen als ideale Last (A4 (a)); die Planung liefert für jede beheizte Zone den Zustand
        /// GEKOPPELT (keine Ablehnung, die Reihe bleibt dieselbe Instanz), für den Keller UNBEHEIZT; das
        /// Gebäude ist GEKOPPELT, und der Lauf ist bitgleich zu „aus". Gegenprobe: ohne Stufe rampt dasselbe
        /// Gebäude.
        /// </summary>
        [Fact]
        public void N_AH10_Zonen_AK1_Gebaeude_und_unbeheizte_Zone_bleiben_unveraendert_W5()
        {
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            GebaeudeKlima klima = AufheizMehrzonenTests.Klima();
            Mehrzonenergebnis aus = Zonenrechnung.Rechnen(g, klima, false, DbWerte.ANLAGENKOPPLUNG_AK1, 0, g.ID_Gebaeude);
            Mehrzonenergebnis an = Zonenrechnung.Rechnen(g, klima, false, DbWerte.ANLAGENKOPPLUNG_AK1, 0, g.ID_Gebaeude,
                                                         aufheizvorgabe: An());
            foreach (ZonenEingang z in an.Eingaenge)
            {
                Aufheizplan p = z.Aufheizplan;
                Assert.NotNull(p);
                Assert.False(p.Geaendert);
                Assert.Null(p.Bemessung);
                Assert.Null(p.Rampenmaske);
                Assert.Same(z.Eingang.ThetaSoll, p.Reihe);
                if (z.IstBeheizt)
                {
                    Assert.True(z.Eingang.KopplungAlsIdealeLast);
                    Assert.False(z.Eingang.KopplungWirksam);
                    Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, p.Zustand);
                    Assert.True(p.Gekoppelt);
                }
                else
                    Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, p.Zustand);
            }
            Aufheizgebaeude geb = an.Aufheizgebaeude;
            Assert.True(geb.Gekoppelt);
            Assert.Equal(2, geb.ZonenGekoppelt);
            Assert.Equal(1, geb.ZonenUnbeheizt);
            Assert.Null(geb.AufheizzeitMaxH);
            Assert.Equal(0, geb.Aufheiztage);
            Bitgleich(aus, an, "AK1 mit Zonen");

            // Gegenprobe: ohne Stufe rampt dasselbe Gebäude.
            Mehrzonenergebnis frei = Zonenrechnung.Rechnen(g, klima, false, null, 0, g.ID_Gebaeude, aufheizvorgabe: An());
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, frei.Aufheizgebaeude.Zustand);
            Assert.True(frei.Aufheizgebaeude.Aufheiztage > 0);
        }

        /// <summary>Eine Woche, deren Werte die Funktion (Wochentag, Stunde) liefert.</summary>
        private static Konditionierungskalender Wochenkalender(Konditionierungsgroesse g, Func<int, int, double> wert)
        {
            var woche = new double[Kalenderwoche.WOCHENWERTE];
            for (int w = 0; w < 7; w++)
                for (int st = 0; st < 24; st++)
                    woche[Kalenderwoche.Stelle(w, st)] = wert(w, st);
            return new Konditionierungskalender(g, Kalenderangabe.AusWoche(woche), null, null);
        }

        /// <summary>
        /// <b>Zone ohne Kalender (B5):</b> θ_T,max der Zielleistung kommt aus der eigenen Reihe der Zone — die
        /// Zone mit eigenem Tagwert 22 °C ohne Kalender bemisst mit 22 °C, obwohl ihre Auslegungsraumtemperatur
        /// der Kopplung der Tagwert des Gebäudes (20 °C) bleibt; die Zone ohne eigenen Tagwert erbt 20 °C; eine
        /// Zone mit Heizkalender nimmt dessen höchsten Wert der Nutzungszeit (23 °C), die Nachbarzone ohne
        /// Kalender bleibt bei ihrem Wert.
        /// </summary>
        [Fact]
        public void N_AH10_Zone_ohne_Kalender_bemisst_mit_ihrer_eigenen_Reihe_B5()
        {
            double f = 0.5 * Vdi6007Probe.Gebaeude().Nutzflaeche;
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen(
                eins: new Zoneneingaben(Nutzflaeche: f, SollTag: 22.0, SollNacht: 16.0),
                zwei: new Zoneneingaben(Nutzflaeche: f, SollNacht: 17.0));
            Assert.Equal(20.0, g.Raumsolltemperatur_Tag);
            GebaeudeKlima klima = AufheizMehrzonenTests.Klima();

            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, klima, aufheizvorgabe: An());
            ZonenEingang w1 = zonen[Stelle(zonen, AufheizMehrzonenTests.WOHNUNG_1)];
            ZonenEingang w2 = zonen[Stelle(zonen, AufheizMehrzonenTests.WOHNUNG_2)];
            Assert.Equal(22.0, w1.Aufheizplan.Bemessung.ThetaTMaxC);
            Assert.Equal(20.0, w1.Eingang.AuslegungsraumtemperaturHeizC);   // B5: die Kopplung bleibt beim Gebäude
            Assert.Equal(20.0, w2.Aufheizplan.Bemessung.ThetaTMaxC);
            Assert.True(w1.Aufheizplan.Geaendert && w2.Aufheizplan.Geaendert);

            // Wohnung 2 mit Heizkalender: 6–22 Uhr 23 °C, sonst 15 °C; Wohnung 1 ohne Kalender.
            var satz = new Konditionierungssatz(GebaeudeModellEingang.WochentagDesErstenTags(Vdi6007Probe.Wochenende()), 2025);
            satz.Setzen(Konditionierungsgroesse.Heizsoll,
                        Wochenkalender(Konditionierungsgroesse.Heizsoll, (w, st) => st >= 6 && st < 22 ? 23.0 : 15.0));
            IReadOnlyList<ZonenEingang> mitKalender = ZonenEingang.Bauen(g, klima, aufheizvorgabe: An(),
                konditionierung: id => id == AufheizMehrzonenTests.WOHNUNG_2 ? satz : null);
            Aufheizplan k1 = mitKalender[Stelle(mitKalender, AufheizMehrzonenTests.WOHNUNG_1)].Aufheizplan;
            Aufheizplan k2 = mitKalender[Stelle(mitKalender, AufheizMehrzonenTests.WOHNUNG_2)].Aufheizplan;
            Assert.Equal(22.0, k1.Bemessung.ThetaTMaxC);
            Assert.Equal(23.0, k2.Bemessung.ThetaTMaxC);
            Assert.All(k2.Spruenge, sp => Assert.Equal(15.0, sp.ThetaNC));
            Assert.True(k2.Bemessung.AufheizleistungW > w2.Aufheizplan.Bemessung.AufheizleistungW);
        }

        /// <summary>
        /// <b>Beide Aufbauten</b> (Festlegung 1): Der adiabate Vorlauf der 4-K-Regel (jede Zone für sich, ohne
        /// Nachbarn) plant mit der Außenform, der gekoppelte Lauf mit der Nachbarform — beide setzen ihre
        /// Rampen am Ende von <see cref="ZonenEingang.Bauen"/>. Mit der Trennwand nach der Regel läuft die
        /// Zonenrechnung über beide Aufbauten durch.
        /// </summary>
        [Fact]
        public void N_AH10_Zonen_planen_in_beiden_Aufbauten()
        {
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
            GebaeudeZonensatz w1 = g.Zonen[0];
            var regel = w1.Bauteile.Select(b => b.Rand == Bauteilrand.Zone && b.IdNachbarzone == AufheizMehrzonenTests.WOHNUNG_2
                                                    ? b.MitZuordnung(Trennflaechenzuordnung.Regel) : b).ToList();
            g.Zonen = new[] { new GebaeudeZonensatz(w1.ZonenId, w1.Bezeichnung, regel, w1.Nutzflaeche_M2, w1.Eingaben, w1.Rang),
                              g.Zonen[1], g.Zonen[2] };
            GebaeudeKlima klima = AufheizMehrzonenTests.Klima();

            IReadOnlyList<ZonenEingang> adiabat = ZonenEingang.Bauen(g, klima, adiabat: true, aufheizvorgabe: An());
            foreach (ZonenEingang z in adiabat)
            {
                Assert.False(z.Gekoppelt);
                Assert.Equal(z.IstBeheizt ? DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN : DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT, z.Aufheizplan.Zustand);
                Assert.Equal(z.IstBeheizt, z.Aufheizplan.Geaendert);
            }

            Mehrzonenergebnis m = Zonenrechnung.Rechnen(g, klima, false, null, 0, g.ID_Gebaeude, aufheizvorgabe: An());
            Assert.Single(m.Paare);
            Assert.All(m.Eingaenge.Where(z => z.IstBeheizt), z => Assert.True(z.Gekoppelt && z.Aufheizplan.Geaendert));
            Assert.Equal(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, m.Aufheizgebaeude.Zustand);
        }

        /// <summary>
        /// <b>Schalter aus = kein Aufruf</b> (Grundsatz 3) mit Zonen: Mit der Vorgabe „aus" trägt keine Zone einen
        /// Plan, das Gebäude keine Aufheizwerte, und der Lauf ist bitgleich zum Lauf ohne Vorgabe.
        /// </summary>
        [Fact]
        public void N_AH10_Zonen_Schalter_aus_ruft_nicht()
        {
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
            GebaeudeKlima klima = AufheizMehrzonenTests.Klima();
            Mehrzonenergebnis ohne = Zonenrechnung.Rechnen(g, klima, false, null, 0, g.ID_Gebaeude);
            Mehrzonenergebnis aus = Zonenrechnung.Rechnen(g, klima, false, null, 0, g.ID_Gebaeude, aufheizvorgabe: Aufheizvorgabe.Aus);
            Assert.Null(aus.Aufheizgebaeude);
            Assert.All(aus.Eingaenge, z => Assert.Null(z.Aufheizplan));
            Bitgleich(ohne, aus, "Schalter aus");
        }

        /// <summary>
        /// <b>Determinismus mit Zonen:</b> Zwei Läufe des Dreizonengebäudes mit Wochenende und Aufheizplanung
        /// geben je Zone dieselbe Reihe mit Rampe, dieselben Zähler und denselben Lauf, Bit für Bit, und
        /// dieselben Gebäudewerte.
        /// </summary>
        [Fact]
        public void N_AH10_Zonen_zwei_Laeufe_sind_bitgleich()
        {
            double f = 0.5 * Vdi6007Probe.Gebaeude().Nutzflaeche;
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen(
                eins: new Zoneneingaben(Nutzflaeche: f, SollTag: 21.0, SollNacht: 16.0, SollWochenende: 15.0),
                zwei: new Zoneneingaben(Nutzflaeche: f, SollTag: 20.0, SollNacht: 17.0, SollWochenende: 16.0));
            var klima = new GebaeudeKlima(Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang), Vdi6007Probe.Wochenende(),
                                          Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE);
            Mehrzonenergebnis a = Zonenrechnung.Rechnen(g, klima, false, null, 0, g.ID_Gebaeude, aufheizvorgabe: An());
            Mehrzonenergebnis b = Zonenrechnung.Rechnen(g, klima, false, null, 0, g.ID_Gebaeude, aufheizvorgabe: An());
            Bitgleich(a, b, "Determinismus");
            for (int z = 0; z < a.Eingaenge.Count; z++)
            {
                Aufheizplan p = a.Eingaenge[z].Aufheizplan, q = b.Eingaenge[z].Aufheizplan;
                Assert.Equal(p.Zustand, q.Zustand);
                AufheizGrenzfallTests.Bitgleich(p.Reihe, q.Reihe, "Reihe " + z);
                Assert.Equal(p.Spruenge, q.Spruenge);
                Assert.Equal(p.Rampenmaske, q.Rampenmaske);
                Assert.Equal(p.Aufheiztage, q.Aufheiztage);
                Assert.Equal(p.TageBegrenzt, q.TageBegrenzt);
                Assert.Equal(p.SpruengeAus, q.SpruengeAus);
                if (p.Bemessung != null) Assert.Equal(Bits(p.Bemessung.AufheizleistungW), Bits(q.Bemessung.AufheizleistungW));
            }
            Aufheizgebaeude x = a.Aufheizgebaeude, y = b.Aufheizgebaeude;
            Assert.Equal(x with { Rampenmaske = null }, y with { Rampenmaske = null });
            Assert.Equal(x.Rampenmaske, y.Rampenmaske);
            Assert.True(x.Aufheiztage > 0);
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);
    }
}
