using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Entscheide F17–F20 der Aufheizoptimierung Fassung 2</b> (Anwender 11.10.2026, E125; Welle V3c): das 95-%-Quantil
    /// des Bedarfs an konstruierten Profilen samt Grenzfall weniger Sprünge (F19), unerreichbare Sprünge außerhalb der Bemessung
    /// (F18), die Meldung der Tage über t_V, der Sperrzeit-Hinweis an einer synthetischen Schranke 0 in den Fensterstunden (F20)
    /// — ohne Wirkung auf Plan und Lauf — und Option 1 bitgleich unter beiden Ankunftsbezügen (F17).
    /// </summary>
    [Collection("Testdatenbank")]
    public class VorheizEntscheideTests
    {
        private const int STUNDEN = 8760;

        private readonly ITestOutputHelper _aus;

        public VorheizEntscheideTests(ITestOutputHelper aus) => _aus = aus;

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static Aufheizvorgabe Vorgabe(int tV, Vorheizankunftsbezug bezug = Vorheizvorgabe.ANKUNFTSBEZUG_VORGABE)
            => new Aufheizvorgabe(true, null, null, null, null)
            {
                Vorheizen = new Vorheizvorgabe(Aufheizverfahren.Vorgabe, tV) { Ankunftsbezug = bezug },
            };

        // =====================================================================
        //  F19: das Quantil
        // =====================================================================

        [Fact]
        public void Das_Quantil_ist_das_kleinste_t_das_95_Prozent_abdeckt()
        {
            // 100 Sprünge mit Bedarf 1 … 100 h: Rang ⌈95⌉ = 95.
            Assert.Equal(95, Vorheizplanung.Quantil(Enumerable.Range(1, 100), 95));
            // 20 Sprünge: Rang 19 — der größte fällt heraus.
            Assert.Equal(19, Vorheizplanung.Quantil(Enumerable.Range(1, 20), 95));
            // Grenzfall wenige Sprünge: unter 20 ist das Quantil das Maximum.
            Assert.Equal(19, Vorheizplanung.Quantil(Enumerable.Range(1, 19), 95));
            Assert.Equal(7, Vorheizplanung.Quantil(new[] { 7 }, 95));
            Assert.Equal(9, Vorheizplanung.Quantil(new[] { 9, 2 }, 95));
            Assert.Equal(0, Vorheizplanung.Quantil(Array.Empty<int>(), 95));
            // Reihenfolge und Gleichstand: 38 Sprünge mit 4 h, 2 mit 30 h — 38/40 = 95 % sind mit 4 h gedeckt.
            int[] profil = Enumerable.Repeat(30, 2).Concat(Enumerable.Repeat(4, 38)).ToArray();
            Assert.Equal(4, Vorheizplanung.Quantil(profil, 95));
            // Drei Ausreißer unter 40 sind mehr als 5 %: dann deckt erst der Ausreißer.
            int[] drei = Enumerable.Repeat(30, 3).Concat(Enumerable.Repeat(4, 37)).ToArray();
            Assert.Equal(30, Vorheizplanung.Quantil(drei, 95));
            Assert.Equal(30, Vorheizplanung.Quantil(profil, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => Vorheizplanung.Quantil(profil, 0));
        }

        [Fact]
        public void Die_Analyse_bemisst_nur_die_erreichbaren_Spruenge()
        {
            // 40 erreichbare Sprünge (38 × 4 h, 2 × 12 h) und 5 unerreichbare mit min(D, 47) = 47 h; ein Sprung ohne Fenster.
            var bedarf = new List<int>();
            var unerreichbar = new List<bool>();
            for (int i = 0; i < 38; i++) { bedarf.Add(4); unerreichbar.Add(false); }
            for (int i = 0; i < 2; i++) { bedarf.Add(12); unerreichbar.Add(false); }
            for (int i = 0; i < 5; i++) { bedarf.Add(47); unerreichbar.Add(true); }
            bedarf.Add(0);
            unerreichbar.Add(false);
            var a = new Vorheizanalyse { Bedarf = bedarf.ToArray(), Unerreichbar = unerreichbar.ToArray() };
            Assert.Equal(4, a.BedarfQuantilH);
            Assert.Equal(12, a.BedarfMaxErreichbarH);
            Assert.Equal(47, a.BedarfMaxH);
            // Ohne erreichbaren Sprung: 0 (t_V fällt dann auf 1 h).
            var leer = new Vorheizanalyse { Bedarf = new[] { 47, 47 }, Unerreichbar = new[] { true, true } };
            Assert.Equal(0, leer.BedarfQuantilH);
        }

        [Fact]
        public void Der_Hinweis_nennt_die_Tage_ueber_tV_mit_Bedarf_und_Unterschreitung()
        {
            using var k = new Kulturvorrichtung("de-DE");
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            var ohne = new bool[365];
            for (int t = 0; t < 6; t++) ohne[t] = true;
            var v = new Vorheizgebaeude
            {
                Berechnet = true,
                VorheizzeitMaxH = 5,
                BedarfMaxH = 9,
                BedarfMedianH = 3,
                SpruengeUeberVorheizzeit = 4,
                TageUeberVorheizzeit = 4,
                UnterschreitungUeberVorheizzeitK = 0.8,
                TageOhneAnkunft = ohne,
                UnterschreitungMaxK = 1.6,
            };
            Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Keiner, v, "Gebäude Q");
            Assert.Contains(p.Hinweise, z => z.Contains("Vorheizzeit berechnet: 5 h — sie reicht für 95 % der erreichbaren Nutzungsbeginne (Median 3 h, größter Bedarf 9 h, 4 Nutzungsbeginne bräuchten mehr)", StringComparison.Ordinal));
            Assert.Contains(p.Hinweise, z => z.Contains("An 4 Tagen erreicht die Raumluft zum Nutzungsbeginn den Sollwert nicht (bis 0,8 K darunter); nötig wären bis 9 h Vorheizen statt 5 h.", StringComparison.Ordinal));
            // Ohne Tage über t_V bleibt es beim Text der Tage ohne Ankunft.
            SimulationProtokoll q = SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Keiner, v with { TageUeberVorheizzeit = 0, BedarfMaxH = 5 }, "Gebäude R");
            Assert.Contains(q.Hinweise, z => z.Contains("An 6 Tagen erreicht die Raumluft zum Nutzungsbeginn den Sollwert nicht (bis 1,6 K darunter).", StringComparison.Ordinal));
            Assert.DoesNotContain(q.Hinweise, z => z.Contains("nötig wären", StringComparison.Ordinal));
        }

        // =====================================================================
        //  F20: Sperrzeit im Vorheizfenster
        // =====================================================================

        [Fact]
        public void Uhrzeiten_fassen_Bereiche_zusammen_auch_ueber_Mitternacht()
        {
            static bool[] U(params int[] h)
            {
                var u = new bool[24];
                foreach (int x in h) u[x] = true;
                return u;
            }
            Assert.Equal("0–6", Vorheizplanung.Uhrzeiten(U(0, 1, 2, 3, 4, 5)));
            Assert.Equal("6–7, 23–24", Vorheizplanung.Uhrzeiten(U(6, 23)));
            Assert.Equal("22–6", Vorheizplanung.Uhrzeiten(U(22, 23, 0, 1, 2, 3, 4, 5)));
            Assert.Equal("–", Vorheizplanung.Uhrzeiten(new bool[24]));
            Assert.Equal("0–24", Vorheizplanung.Uhrzeiten(Enumerable.Repeat(true, 24).ToArray()));
        }

        /// <summary>Die synthetische Schranke: 0 bis 6 Uhr keine Leistung (Sperrzeit), sonst reichlich.</summary>
        private static Anlagenverfuegbarkeit[] Nachtsperre()
        {
            var v = new Anlagenverfuegbarkeit[STUNDEN];
            for (int h = 0; h < STUNDEN; h++)
                v[h] = h % 24 < 6
                    ? new Anlagenverfuegbarkeit(0.0, double.NaN, Verfuegbarkeitsgrund.Sperrzeit)
                    : new Anlagenverfuegbarkeit(1.0e6, double.NaN, Verfuegbarkeitsgrund.KeineBegrenzung);
            return v;
        }

        [Fact]
        public void Die_Sperrzeit_im_Fenster_wird_gemeldet_und_aendert_den_Plan_nicht()
        {
            GebaeudeModellEingang frei = VorheizAnkunftsbezugTests.Gekoppelt(1.5);
            (Aufheizplan pFrei, _) = VorheizAnkunftsbezugTests.Rechnen(frei, Vorgabe(8));
            GebaeudeModellEingang gesperrt = VorheizAnkunftsbezugTests.Gekoppelt(1.5);
            gesperrt.Verfuegbarkeit = Nachtsperre();
            Assert.True(gesperrt.FahrplanWirksam);
            (Aufheizplan pSperre, _) = VorheizAnkunftsbezugTests.Rechnen(gesperrt, Vorgabe(8));

            // Keine Sonderlogik: Fenster, Sollwertreihe und Deckel bleiben, wie sie ohne Sperre sind.
            Assert.Equal(pFrei.Vorheizen.Spruenge.Select(x => x.FensterH), pSperre.Vorheizen.Spruenge.Select(x => x.FensterH));
            for (int h = 0; h < STUNDEN; h++)
            {
                Assert.Equal(Bits(pFrei.Reihe[h]), Bits(pSperre.Reihe[h]));
                Assert.Equal(Bits(pFrei.Deckelreihe[h]), Bits(pSperre.Deckelreihe[h]));
            }
            Assert.Equal(0, pFrei.Vorheizen.Nachweis.FensterstundenGesperrt);
            Assert.Equal(0, pFrei.Vorheizen.Nachweis.TageSperrzeit.Count(t => t));

            // Gemeldet: jeder Tag mit einem Fenster (Sprung 7 Uhr, Fenster 23–7 Uhr) liegt in der Sperre 0–6 Uhr.
            Vorheiznachweis n = pSperre.Vorheizen.Nachweis;
            var tage = new HashSet<int>(pSperre.Vorheizen.Spruenge.Where(x => x.FensterH > 0).Select(x => x.Sprungstunde / 24));
            Assert.Equal(tage.Count, n.TageSperrzeit.Count(t => t));
            int erwartet = pSperre.Vorheizen.Spruenge.Sum(x => Enumerable.Range(1, x.FensterH).Count(kk => Aufheizoptimierung.Ring(x.Sprungstunde - kk) % 24 < 6));
            Assert.Equal(erwartet, n.FensterstundenGesperrt);
            Assert.Equal("0–6", Vorheizplanung.Uhrzeiten(n.SperrUhr));
            Assert.True(n.FreiUhr[6] && n.FreiUhr[23]);
            Assert.False(n.FreiUhr[3]);

            using var k = new Kulturvorrichtung("de-DE");
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            Vorheizgebaeude g = Vorheizplanung.Gebaeudewerte(new[] { pSperre });
            Assert.Equal(tage.Count, g.TageSperrzeit);
            Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Keiner, g, "Gebäude S");
            string erwarteterText = "Das Vorheizfenster liegt an " + tage.Count + " Tagen in einer Sperrzeit der Anlage (0–6 Uhr, "
                                    + erwartet + " Fensterstunden ohne verfügbare Wärme); vorheizen können nur die Stunden "
                                    + Vorheizplanung.Uhrzeiten(n.FreiUhr) + " Uhr.";
            Assert.Contains(p.Hinweise, z => z.Contains("Gebäude S", StringComparison.Ordinal) && z.Contains(erwarteterText, StringComparison.Ordinal));
            _aus.WriteLine(p.Hinweise.First(z => z.Contains("Sperrzeit", StringComparison.Ordinal)));
        }

        [Fact]
        public void Eine_Teilsperre_mit_verfuegbarer_Waerme_und_eine_Zone_ohne_Heizkreis_melden_nichts()
        {
            GebaeudeModellEingang e = VorheizAnkunftsbezugTests.Gekoppelt(1.5);
            Anlagenverfuegbarkeit[] v = Nachtsperre();
            for (int h = 0; h < STUNDEN; h++)
                if (h % 24 < 6) v[h] = new Anlagenverfuegbarkeit(5.0, double.NaN, Verfuegbarkeitsgrund.Sperrzeit);
            e.Verfuegbarkeit = v;
            (Aufheizplan p, _) = VorheizAnkunftsbezugTests.Rechnen(e, Vorgabe(8));
            Assert.Equal(0, p.Vorheizen.Nachweis.FensterstundenGesperrt);

            // Ohne Kopplung wirkt keine Schranke (F10) — also auch kein Hinweis.
            GebaeudeModellEingang ideal = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            ideal.HeizsollwertMitRampeSetzen(VorheizVorgabeTests.Kalender());
            ideal.Verfuegbarkeit = Nachtsperre();
            Assert.False(ideal.FahrplanWirksam);
            (Aufheizplan q, _) = VorheizAnkunftsbezugTests.Rechnen(ideal, Vorgabe(8));
            Assert.Equal(0, q.Vorheizen.Nachweis.FensterstundenGesperrt);
        }

        // =====================================================================
        //  F17: Option 1 bleibt unter beiden Bezügen bitgleich
        // =====================================================================

        [Fact]
        public void Option1_rechnet_unter_beiden_Ankunftsbezuegen_bitgleich()
        {
            (Aufheizplan pa, GebaeudeModellErgebnis la) = VorheizAnkunftsbezugTests.Rechnen(VorheizAnkunftsbezugTests.Gekoppelt(0.4),
                                                                                            Vorgabe(6, Vorheizankunftsbezug.Sollwert));
            (Aufheizplan pb, GebaeudeModellErgebnis lb) = VorheizAnkunftsbezugTests.Rechnen(VorheizAnkunftsbezugTests.Gekoppelt(0.4),
                                                                                            Vorgabe(6));
            Assert.Equal(Vorheizankunftsbezug.Uebergabe, pb.Vorheizen.Ankunftsbezug);
            for (int h = 0; h < STUNDEN; h++)
            {
                Assert.Equal(Bits(pa.Reihe[h]), Bits(pb.Reihe[h]));
                Assert.Equal(Bits(la.HeizlastW[h]), Bits(lb.HeizlastW[h]));
                Assert.Equal(Bits(la.Raumtemperatur[h]), Bits(lb.Raumtemperatur[h]));
            }
            // Nur der Nachweis misst anders: (b) verfehlt höchstens so viele Tage wie (a).
            Assert.True(pb.Vorheizen.Nachweis.TageOhneAnkunftAnzahl <= pa.Vorheizen.Nachweis.TageOhneAnkunftAnzahl);
        }
    }
}
