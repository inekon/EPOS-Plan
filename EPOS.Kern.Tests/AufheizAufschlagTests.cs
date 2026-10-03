using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>N-AH11 — der Aufschlag auf die Aufheizrampe</b> (Entscheid E59 (2), Folgeentscheid P16 vom 03.10.2026;
    /// Entwurf KP3 Abschnitt 7, Festlegung 35): n' = min(48, n + max(Aufschlag_H, ⌈n · Aufschlag_Prozent/100⌉))
    /// nur auf Rampen, die ein Sprung des Heizkalenders auslöst und die schon eine sind (n &gt; 1), danach ≤ D + 1;
    /// W2 zählt mit n'; t_auf,max und die Zahl der Rampentage bleiben; die Rampe nach Festlegung 8.
    ///
    /// <para><b>Synthetisch</b> mit dem Prüfsatz der Rändertests (<see cref="AufheizRaenderTests"/>): Tag/Nacht
    /// 20/17 °C mit der Nacht von 22 bis 6 Uhr (D = 8 h), konstant −12 °C, die Grenze so, dass der Bemessungsfall
    /// n = 4 verlangt (t_auf,max = 3 h); der Büro-Kalender für den Deckel. <b>Am Lauf</b> das VDI-Gebäude des
    /// Projekts 1018 und das Dreizonengebäude der Mehrzonenprobe.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AufheizAufschlagTests : IDisposable
    {
        private const int STUNDEN = 8760;
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public AufheizAufschlagTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        private static Aufheizvorgabe An(int? h, double? p, bool fest = false)
            => new Aufheizvorgabe(true, null, null, null, fest ? DbWerte.AUFHEIZ_ART_FEST : null, h, p);

        /// <summary>Tag/Nacht 20/17 °C, Nacht 22–6 Uhr (D = 8), die Grenze für n = 4 am Bemessungsfall.</summary>
        private static Aufheizzone Standard()
            => AufheizRaenderTests.Zone(AufheizRaenderTests.TagNacht(20.0, 17.0, 6, 22),
                                        heizMaxW: AufheizRaenderTests.Grenze(20.0, 3.0, -12.0, 4));

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        // =====================================================================
        //  Die Formel
        // =====================================================================

        /// <summary>
        /// <b>Die Formel</b> an n = 4 und an der Aufrundung: Stunden, Prozent, beides mit Maximum, der Deckel 48,
        /// die Aufrundung trägt den Zahlenrand (genau ganzzahlige Anteile runden nicht weiter); n = 1 und ohne
        /// Aufschlag bleibt n.
        /// </summary>
        [Theory]
        [InlineData(0, 0.0, 4, 4)]
        [InlineData(2, 0.0, 4, 6)]
        [InlineData(0, 50.0, 4, 6)]
        [InlineData(2, 50.0, 4, 6)]
        [InlineData(3, 50.0, 4, 7)]
        [InlineData(1, 75.0, 4, 7)]
        [InlineData(24, 100.0, 4, 28)]
        [InlineData(0, 30.0, 4, 6)]      // 1,2 → 2
        [InlineData(0, 25.0, 4, 5)]      // genau 1 → 1
        [InlineData(0, 0.1, 4, 5)]       // 0,004 → 1
        [InlineData(0, 12.5, 8, 9)]      // genau 1 → 1
        [InlineData(0, 2.5, 40, 41)]     // genau 1 → 1
        [InlineData(24, 100.0, 30, 48)]  // Deckel
        [InlineData(0, 100.0, 47, 48)]   // Deckel
        [InlineData(24, 100.0, 1, 1)]    // keine Rampe - kein Aufschlag
        public void Die_Formel_mit_Maximum_Aufrundung_und_Deckel(int h, double p, int n, int erwartet)
        {
            Assert.Equal(erwartet, Aufheizoptimierung.MitAufschlag(n, An(h, p)));
            Assert.Equal(n, Aufheizoptimierung.MitAufschlag(n, An(null, null)));
        }

        // =====================================================================
        //  Der Plan einer Zone
        // =====================================================================

        /// <summary>
        /// <b>N-AH11 an der Zone, täglich und fest:</b> Jede Rampe (n = 4) wird n' nach der Formel, höchstens D + 1 = 9;
        /// W2 zählt, wo der Aufschlag an D + 1 stößt; t_auf,max, die Bemessung und die Zahl der Rampentage bleiben,
        /// Σ (n − 1) und die längste Rampe zählen mit n'; die Reihe folgt der Vorschrift der Festlegung 8.
        /// </summary>
        [Theory]
        [InlineData(0, 0.0, false, 4)]
        [InlineData(2, 0.0, false, 6)]
        [InlineData(0, 50.0, false, 6)]
        [InlineData(2, 50.0, false, 6)]
        [InlineData(24, 100.0, false, 9)]
        [InlineData(6, 0.0, false, 9)]
        [InlineData(2, 0.0, true, 6)]
        [InlineData(24, 100.0, true, 9)]
        public void N_AH11_Jede_Rampe_nach_der_Formel_und_hoechstens_D_plus_1(int h, double p, bool fest, int erwartet)
        {
            Aufheizzone z = Standard();
            Aufheizplan ohne = Aufheizoptimierung.Planen(z, An(null, null, fest));
            Aufheizplan mit = Aufheizoptimierung.Planen(z, An(h, p, fest));

            Assert.Equal(3, ohne.Bemessung.VarianteA.AufheizzeitMaxH);
            Assert.All(ohne.Spruenge, sp => Assert.Equal(4, sp.N));
            Assert.Equal(365, mit.Spruenge.Count);
            int formel = Math.Min(48, 4 + Math.Max(h, (int)Math.Ceiling(4 * p / 100.0)));
            bool begrenzt = formel > 9;
            foreach (Aufheizsprung sp in mit.Spruenge)
            {
                Assert.Equal(8, sp.AbsenkdauerH);
                Assert.Equal(erwartet, sp.N);
                Assert.Equal(begrenzt, sp.Begrenzt);
                Assert.False(sp.Unerreichbar);
            }
            // Was bleibt: Bemessung, t_auf,max, Zustand, Zahl der Rampentage, W1, W4.
            Assert.Equal(ohne.Bemessung, mit.Bemessung);
            Assert.Equal(ohne.AufheizzeitMaxH, mit.AufheizzeitMaxH);
            Assert.Equal(ohne.Zustand, mit.Zustand);
            Assert.Equal(ohne.Aufheiztage, mit.Aufheiztage);
            Assert.Equal(ohne.TageUnerreichbar, mit.TageUnerreichbar);
            Assert.Equal(ohne.SpruengeAus, mit.SpruengeAus);
            // Was mit n' zählt: W2, Σ (n − 1), die längste Rampe.
            Assert.Equal(begrenzt ? 365 : 0, mit.TageBegrenzt);
            Assert.Equal(365 * (erwartet - 1), mit.AufheizstundenH);
            Assert.Equal(erwartet - 1, mit.LaengsteRampeH);
            Assert.Equal(fest ? DbWerte.AUFHEIZ_ART_FEST : DbWerte.AUFHEIZ_ART_TAEGLICH, mit.Art);
            Assert.Null(mit.ManuellH);
            AufheizRaenderTests.Vorschrift(z, mit);
        }

        /// <summary>
        /// <b>Ohne Aufschlag bitgleich</b> (Grundsatz 3): (0, 0) und NULL ergeben denselben Record und denselben Plan
        /// wie die Vorgabe ohne die Felder — Reihe, Maske und Sprünge Bit für Bit.
        /// </summary>
        [Fact]
        public void N_AH11_Null_Null_ist_bitgleich_ohne_Felder()
        {
            Assert.Equal(AufheizRaenderTests.An(), An(0, 0.0));
            Assert.Equal(AufheizRaenderTests.An(), An(null, null));
            Aufheizzone z = Standard();
            Aufheizplan a = Aufheizoptimierung.Planen(z, AufheizRaenderTests.An());
            Aufheizplan b = Aufheizoptimierung.Planen(z, An(0, 0.0));
            Assert.Equal(a.Spruenge, b.Spruenge);
            for (int h = 0; h < STUNDEN; h++)
            {
                Assert.Equal(Bits(a.Reihe[h]), Bits(b.Reihe[h]));
                Assert.Equal(a.Rampenmaske[h], b.Rampenmaske[h]);
            }
        }

        /// <summary>
        /// <b>Kein Aufschlag ohne Rampe und ohne Sprung</b> (P16): Hält n = 1 an jedem Sprung (P_auf weit über dem
        /// Bedarf), bleibt die Reihe dieselbe Instanz, auch mit (24, 100); eine Reihe ohne Sprung ebenso.
        /// </summary>
        [Fact]
        public void N_AH11_Kein_Aufschlag_bei_n_gleich_1_und_ohne_Sprung()
        {
            double[] soll = AufheizRaenderTests.TagNacht(20.0, 17.0, 6, 22);
            Aufheizzone z = AufheizRaenderTests.Zone(soll, heizMaxW: 1.0e9);
            Aufheizplan p = Aufheizoptimierung.Planen(z, An(24, 100.0));
            Assert.Equal(365, p.Spruenge.Count);
            Assert.All(p.Spruenge, sp => Assert.Equal(1, sp.N));
            Assert.False(p.Geaendert);
            Assert.Same(soll, p.Reihe);
            Assert.Equal(0, p.Aufheiztage);
            Assert.Equal(0, p.TageBegrenzt);

            double[] konstant = AufheizRaenderTests.Konstant(20.0);
            Aufheizplan k = Aufheizoptimierung.Planen(AufheizRaenderTests.Zone(konstant, heizMaxW: 1.0e3), An(24, 100.0));
            Assert.Empty(k.Spruenge);
            Assert.Same(konstant, k.Reihe);
            Assert.Equal(0, k.AufheizstundenH);
        }

        /// <summary>
        /// <b>Der Deckel 48 am Montag nach dem Büro-Wochenende</b> (D = 61): Der Bemessungsfall verlangt n = 30;
        /// mit (24, 0) wird der Montag 48 (Deckel, nicht W2), die Werktage (D = 13) bleiben bei 14 und W2.
        /// </summary>
        [Fact]
        public void N_AH11_Deckel_48_am_Montag()
        {
            double[] soll = AufheizRaenderTests.Buero(20.0, 16.0, out bool[] we);
            Aufheizzone z = AufheizRaenderTests.Zone(soll, heizMaxW: AufheizRaenderTests.Grenze(20.0, 4.0, -12.0, 30));
            Aufheizplan ohne = Aufheizoptimierung.Planen(z, AufheizRaenderTests.An());
            Aufheizplan mit = Aufheizoptimierung.Planen(z, An(24, null));
            Assert.Equal(29, mit.Bemessung.VarianteA.AufheizzeitMaxH);
            int montage = 0;
            for (int i = 0; i < mit.Spruenge.Count; i++)
            {
                Aufheizsprung a = ohne.Spruenge[i], b = mit.Spruenge[i];
                if (AufheizRaenderTests.IstMontag(we, b.Sprungstunde / 24))
                {
                    montage++;
                    Assert.Equal(30, a.N);
                    Assert.Equal(48, b.N);
                    Assert.False(b.Begrenzt);
                }
                else
                {
                    Assert.Equal(14, a.N);
                    Assert.Equal(14, b.N);
                    Assert.True(a.Begrenzt && b.Begrenzt);
                }
            }
            Assert.True(montage >= 52);
            Assert.Equal(47, mit.LaengsteRampeH);
            Assert.Equal(ohne.Aufheiztage, mit.Aufheiztage);
            AufheizRaenderTests.Vorschrift(z, mit);
        }

        /// <summary>
        /// <b>Die Rampe nach Festlegung 8 mit Kühlkappung:</b> Die verlängerte Rampe wird an θ_K − 1 K gekappt, die
        /// Sprungstunde nie geschrieben, die stündliche Kühlprüfung hält (Vorschrift samt Kühlprüfung).
        /// </summary>
        [Fact]
        public void N_AH11_Kuehlkappung_haelt_mit_dem_Aufschlag()
        {
            double[] soll = AufheizRaenderTests.TagNacht(20.0, 17.0, 6, 22);
            double[] kuehl = AufheizRaenderTests.TagNacht(26.0, 19.5, 6, 22);
            Aufheizzone z = AufheizRaenderTests.Zone(soll, heizMaxW: AufheizRaenderTests.Grenze(20.0, 3.0, -12.0, 4), kuehl: kuehl);
            Aufheizplan p = Aufheizoptimierung.Planen(z, An(3, null));
            Assert.All(p.Spruenge, sp => Assert.Equal(7, sp.N));
            Assert.True(p.KuehlgekappteStundenH > 0);
            AufheizRaenderTests.Vorschrift(z, p);
        }

        // =====================================================================
        //  Mehrzonen und Lauf
        // =====================================================================

        /// <summary>
        /// <b>Mehrzonen:</b> Je beheizte Zone wird jede Rampe n &gt; 1 nach der Formel verlängert und auf D + 1 begrenzt,
        /// Sprünge mit n = 1 bleiben; die unbeheizte Zone bleibt ohne Rampe; die Bemessung jeder Zone bleibt.
        /// </summary>
        [Fact]
        public void N_AH11_Mehrzonen_je_Zone_nach_der_Formel()
        {
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
            Mehrzonenergebnis ohne = Zonenrechnung.Rechnen(g, AufheizMehrzonenTests.Klima(), false, null, 0, g.ID_Gebaeude,
                                                           aufheizvorgabe: AufheizMehrzonenTests.An());
            Aufheizvorgabe mitVorgabe = An(2, 50.0);
            Mehrzonenergebnis mit = Zonenrechnung.Rechnen(g, AufheizMehrzonenTests.Klima(), false, null, 0, g.ID_Gebaeude,
                                                          aufheizvorgabe: mitVorgabe);
            int verlaengert = 0;
            foreach (int id in new[] { AufheizMehrzonenTests.WOHNUNG_1, AufheizMehrzonenTests.WOHNUNG_2 })
            {
                Aufheizplan a = ohne.Eingaenge[AufheizMehrzonenTests.Stelle(ohne.Eingaenge, id)].Aufheizplan;
                Aufheizplan b = mit.Eingaenge[AufheizMehrzonenTests.Stelle(mit.Eingaenge, id)].Aufheizplan;
                Assert.Equal(a.Bemessung, b.Bemessung);
                Assert.Equal(a.Spruenge.Count, b.Spruenge.Count);
                for (int i = 0; i < a.Spruenge.Count; i++)
                {
                    int n = a.Spruenge[i].N, d = a.Spruenge[i].AbsenkdauerH;
                    int soll = n <= 1 ? n : Math.Min(Aufheizoptimierung.MitAufschlag(n, mitVorgabe), d + 1);
                    Assert.Equal(soll, b.Spruenge[i].N);
                    if (b.Spruenge[i].N > n) verlaengert++;
                }
                Assert.Equal(a.Aufheiztage, b.Aufheiztage);
            }
            Assert.True(verlaengert > 0, "Keine Rampe verlängert.");
            Assert.True(mit.Eingaenge[AufheizMehrzonenTests.Stelle(mit.Eingaenge, AufheizMehrzonenTests.KELLER)].Aufheizplan.Unbeheizt);
            Assert.True(mit.Aufheizgebaeude.AufheizstundenH >= ohne.Aufheizgebaeude.AufheizstundenH);
            _aus.WriteLine("Mehrzonen: {0} Rampen verlängert, Rampenstunden {1} -> {2} h", verlaengert,
                           ohne.Aufheizgebaeude.AufheizstundenH, mit.Aufheizgebaeude.AufheizstundenH);
        }

        /// <summary>
        /// <b>Am Testdatenbankgebäude (1018):</b> (0, 0) gespeichert liest sich als NULL und rechnet Bit für Bit wie ohne
        /// Aufschlag; mit (2, 50) bleiben Rampentage und t_auf,max, die Rampenstunden wachsen; Schalter aus mit Aufschlag
        /// rechnet wie „aus".
        /// </summary>
        [Fact]
        public void N_AH11_Am_Testdatenbankgebaeude()
        {
            if (!_db.Vorhanden) return;
            SimulationWaermebedarf aus = Bedarf(1018);
            double[] reiheAus = (double[])aus.GebaeudeErgebnisse.Ergebnis(0).HeizlastW.Clone();

            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(1018, new Aufheizvorgabe(false, null, null, null, null, 2, 50.0)));
            Aufheizvorgabe gelesen = KonfigurationCtrl.AufheizvorgabeLesen(1018);
            Assert.False(gelesen.An);
            Assert.Equal(2, gelesen.AufschlagH);
            GebaeudeModellErgebnis ausMitAufschlag = Bedarf(1018).GebaeudeErgebnisse.Ergebnis(0);
            Assert.Null(ausMitAufschlag.Aufheizung);
            Gleich(reiheAus, ausMitAufschlag.HeizlastW, "Schalter aus mit Aufschlag");

            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(1018, An(0, 0.0)));
            Assert.Null(KonfigurationCtrl.AufheizvorgabeLesen(1018).AufschlagH);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Einstellungen WHERE ID_Projekt = 1018 AND (Aufheiz_Aufschlag_H IS NOT NULL OR Aufheiz_Aufschlag_Prozent IS NOT NULL)")));
            GebaeudeModellErgebnis nullNull = Bedarf(1018).GebaeudeErgebnisse.Ergebnis(0);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(1018, An(null, null)));
            GebaeudeModellErgebnis ohne = Bedarf(1018).GebaeudeErgebnisse.Ergebnis(0);
            Gleich(ohne.HeizlastW, nullNull.HeizlastW, "(0, 0) gegen ohne");
            Assert.Equal(ohne.Aufheizung with { Rampenmaske = null, Nachweisbandtage = null },
                         nullNull.Aufheizung with { Rampenmaske = null, Nachweisbandtage = null });

            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(1018, An(2, 50.0)));
            GebaeudeModellErgebnis mit = Bedarf(1018).GebaeudeErgebnisse.Ergebnis(0);
            Assert.Equal(ohne.Aufheizung.AufheizzeitMaxH, mit.Aufheizung.AufheizzeitMaxH);
            Assert.Equal(ohne.Aufheizung.Aufheiztage, mit.Aufheizung.Aufheiztage);
            Assert.True(mit.Aufheizung.AufheizstundenH >= ohne.Aufheizung.AufheizstundenH);
            if (ohne.Aufheizung.Aufheiztage > 0)
                Assert.True(mit.Aufheizung.AufheizstundenH > ohne.Aufheizung.AufheizstundenH);
            _aus.WriteLine("1018: Rampentage {0}, Rampenstunden {1} -> {2} h, t_auf,max {3} h, längste Rampe {4} -> {5} h",
                           ohne.Aufheizung.Aufheiztage, ohne.Aufheizung.AufheizstundenH, mit.Aufheizung.AufheizstundenH,
                           ohne.Aufheizung.AufheizzeitMaxH, ohne.Aufheizung.AufheizzeitLaengsteH, mit.Aufheizung.AufheizzeitLaengsteH);
        }

        private static SimulationWaermebedarf Bedarf(int projekt)
        {
            var p = new ProjektCtrl();
            p.ReadSingle(projekt);
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(projekt, p.m_ID_Klimaregion);
            return sim;
        }

        private static void Gleich(double[] a, double[] b, string wo)
        {
            Assert.Equal(a.Length, b.Length);
            for (int h = 0; h < a.Length; h++)
                Assert.True(Bits(a[h]) == Bits(b[h]), wo + ", Stunde " + h.ToString(CultureInfo.InvariantCulture));
        }
    }
}
