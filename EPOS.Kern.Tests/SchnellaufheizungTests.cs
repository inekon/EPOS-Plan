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
    /// <b>Schnellaufheizung im Rechenkern</b> (SA1, E128, Art (b)): Im Vorheizfenster rechnet die Heizkurve einer Zone mit
    /// wirksamem Heizkreis mit θ_T + Δθ; die Überhöhung endet, sobald die Raumluft am Beginn einer Stunde den Ankunftsbezug
    /// min(θ_T, θ_stat) − ε erreicht; der überhöhte Vorlauf steigt höchstens bis zum Vorlaufangebot der Anlage
    /// (<c>Vorlauf_Max</c>), der Deckel bleibt wirksam; die Vorausschau rechnet sie mit. Ohne Schalter, mit der Sollwertrampe
    /// und an Gebäuden ohne Heizkreis bleibt alles bitgleich. Dazu die Messung an 1054, 1047, 1058 und 1065 (nur mit
    /// <c>EPOS_MESSUNG=1</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public class SchnellaufheizungTests : IClassFixture<TestDatenbank>
    {
        private const int STUNDEN = 8760;

        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public SchnellaufheizungTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static Aufheizvorgabe Mit(Aufheizverfahren verfahren, bool schnell, double? dK = null, int? tV = null)
            => new Aufheizvorgabe(true, null, null, null, null)
            {
                Vorheizen = new Vorheizvorgabe(verfahren, verfahren == Aufheizverfahren.Vorgabe ? tV ?? 6 : null)
                    .MitSchnellaufheizung(schnell, dK),
            };

        private static (Aufheizplan Plan, GebaeudeModellErgebnis Lauf, GebaeudeModellEingang E) Rechnen(
            Func<GebaeudeModellEingang> bau, Aufheizvorgabe v)
        {
            GebaeudeModellEingang e = bau();
            (Aufheizplan plan, GebaeudeModellErgebnis lauf) = VorheizAnkunftsbezugTests.Rechnen(e, v);
            return (plan, lauf, e);
        }

        private static GebaeudeModellEingang Ak1() => VorheizAnkunftsbezugTests.Gekoppelt(1.0);

        private static GebaeudeModellEingang Ideal()
        {
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            e.HeizsollwertMitRampeSetzen(VorheizVorgabeTests.Kalender());
            return e;
        }

        private static void Bitgleich(GebaeudeModellErgebnis a, GebaeudeModellErgebnis b)
        {
            for (int h = 0; h < STUNDEN; h++)
            {
                Assert.Equal(Bits(a.HeizlastW[h]), Bits(b.HeizlastW[h]));
                Assert.Equal(Bits(a.Raumtemperatur[h]), Bits(b.Raumtemperatur[h]));
            }
        }

        // =====================================================================
        //  Vorgabe
        // =====================================================================

        [Fact]
        public void Vorgabe_ist_aus_und_wirkt_nur_mit_den_neuen_Verfahren()
        {
            Assert.False(new Vorheizvorgabe(Aufheizverfahren.Berechnet).Schnellaufheizung);
            Assert.Equal(Vorheizvorgabe.UEBERHOEHUNG_VORGABE_K, new Vorheizvorgabe(Aufheizverfahren.Berechnet).UeberhoehungWirksamK);
            Assert.False(Vorheizvorgabe.Sollwertrampe.MitSchnellaufheizung(true).SchnellaufheizungWirksam);
            Assert.True(new Vorheizvorgabe(Aufheizverfahren.Vorgabe, 6).MitSchnellaufheizung(true).SchnellaufheizungWirksam);
            Assert.True(new Vorheizvorgabe(Aufheizverfahren.Berechnet).MitSchnellaufheizung(true, 5.0).SchnellaufheizungWirksam);
            Assert.Throws<ArgumentOutOfRangeException>(() => new Vorheizvorgabe(Aufheizverfahren.Berechnet).MitSchnellaufheizung(true, 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Vorheizvorgabe(Aufheizverfahren.Berechnet).MitSchnellaufheizung(true, 10.5));
            // Die Abwandlungen tragen Schalter und Überhöhung mit; die Sollwertrampe bleibt die Sollwertrampe (Gleichheit).
            Vorheizvorgabe v = new Vorheizvorgabe(Aufheizverfahren.Berechnet).MitSchnellaufheizung(true, 4.0).MitGenauigkeit(0.5);
            Assert.True(v.Schnellaufheizung);
            Assert.Equal(4.0, v.UeberhoehungK);
            Assert.Equal(Vorheizvorgabe.Sollwertrampe, new Vorheizvorgabe(Aufheizverfahren.Sollwertrampe));
            Assert.Equal(0.0, Vorheizplanung.UeberhoehungFuer(new Aufheizvorgabe(true, null, null, null, null)
            {
                Vorheizen = Vorheizvorgabe.Sollwertrampe.MitSchnellaufheizung(true, 5.0),
            }));
        }

        // =====================================================================
        //  Löser: Ende bei Ankunft, Vorlaufgrenze
        // =====================================================================

        [Fact]
        public void Der_Loeser_hebt_den_Vorlauf_nur_vor_der_Ankunft_an_und_endet_bei_Ankunft()
        {
            GebaeudeModellEingang e = Ak1();
            ZonenEingang zone = ZonenEingang.Einzeln(e);
            int h = Enumerable.Range(1, STUNDEN - 1).First(x => e.ThetaOut[x] < 0.0 && !double.IsNaN(e.ThetaSoll[x]));
            Stundenrand r = zone.Rand(h, false, ReadOnlySpan<double>.Empty, false);
            Assert.True(r.MitUebergabe && !double.IsNaN(r.VorlaufC));
            double roh = e.Heizkurve.VorlaufUngedeckeltC(r.ThetaSoll + 5.0, e.ThetaOut[h]);

            // Angekommen (Bezug weit unten): dieselbe Stunde Zeichen für Zeichen wie ohne Überhöhung.
            var a = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            var b = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            a.Zuruecksetzen(14.0);
            b.Zuruecksetzen(14.0);
            Stundenergebnis ohne = a.Schritt(in r);
            Stundenergebnis an = b.Schritt(r.MitUeberhoehung(roh, -100.0));
            Assert.Equal(Bits(ohne.HeizleistungW), Bits(an.HeizleistungW));
            Assert.Equal(Bits(ohne.VorlaufC), Bits(an.VorlaufC));
            Assert.False(an.Ueberhoeht);

            // Nicht angekommen: der Vorlauf steigt, die Leistung auch, die Stunde ist gekennzeichnet.
            var c = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            c.Zuruecksetzen(14.0);
            Stundenergebnis hoch = c.Schritt(r.MitUeberhoehung(roh, 100.0));
            Assert.True(hoch.Ueberhoeht);
            Assert.True(hoch.VorlaufC > ohne.VorlaufC);
            Assert.True(hoch.HeizleistungW >= ohne.HeizleistungW);
            Assert.True(hoch.VorlaufC <= e.Uebergabe.AuslegungVorlaufC + 1e-12, "ohne Angebot gilt der Auslegungsvorlauf als Grenze");
        }

        [Fact]
        public void Das_Vorlaufangebot_der_Anlage_begrenzt_den_ueberhoehten_Vorlauf()
        {
            GebaeudeModellEingang e = Ak1();
            ZonenEingang zone = ZonenEingang.Einzeln(e);
            int h = Enumerable.Range(1, STUNDEN - 1).First(x => e.ThetaOut[x] < 0.0 && !double.IsNaN(e.ThetaSoll[x]));
            Stundenrand r = zone.Rand(h, false, ReadOnlySpan<double>.Empty, false);
            double grenze = r.VorlaufC + 2.0;
            Stundenrand mit = r.MitVerfuegbarkeit(double.NaN, Verfuegbarkeitsgrund.KeineBegrenzung, grenze)
                               .MitUeberhoehung(r.VorlaufC + 10.0, 100.0)
                               .UeberhoehungAnwenden();
            Assert.Equal(grenze, mit.VorlaufC);
            Assert.True(mit.UeberhoehungWirksam && mit.UeberhoehungBegrenzt && mit.VorlaufAnlageGekappt);
            // Die Reihenfolge ist gleich: erst die Überhöhung, dann das Angebot.
            Stundenrand umgekehrt = r.MitUeberhoehung(r.VorlaufC + 10.0, 100.0)
                                     .MitVerfuegbarkeit(double.NaN, Verfuegbarkeitsgrund.KeineBegrenzung, grenze)
                                     .UeberhoehungAnwenden();
            Assert.Equal(Bits(mit.VorlaufC), Bits(umgekehrt.VorlaufC));
            // Steht das Angebot schon am Vorlauf, bleibt die Überhöhung wirkungslos.
            Stundenrand am = r.MitVerfuegbarkeit(double.NaN, Verfuegbarkeitsgrund.KeineBegrenzung, r.VorlaufC)
                              .MitUeberhoehung(r.VorlaufC + 10.0, 100.0)
                              .UeberhoehungAnwenden();
            Assert.False(am.UeberhoehungWirksam);
            Assert.True(am.UeberhoehungWirkungslos && am.UeberhoehungBegrenzt);
            Assert.Equal(Bits(r.VorlaufC), Bits(am.VorlaufC));
        }

        // =====================================================================
        //  Lauf: nur im Fenster, nur mit Heizkreis, Deckel, Vorausschau
        // =====================================================================

        [Fact]
        public void Die_Ueberhoehung_wirkt_nur_im_Fenster_endet_vor_seinem_Ende_und_der_Deckel_bleibt()
        {
            (Aufheizplan plan, GebaeudeModellErgebnis lauf, GebaeudeModellEingang e) = Rechnen(Ak1, Mit(Aufheizverfahren.Vorgabe, true, 5.0, 8));
            Vorheizplan vp = plan.Vorheizen;
            Assert.Equal(5.0, vp.UeberhoehungK);
            Assert.NotNull(vp.UeberhoehungReiheK);
            Assert.Same(e.UeberhoehungReiheK.GetType(), vp.UeberhoehungReiheK.GetType());
            Assert.NotNull(lauf.UeberhoehtStunden);
            int fenster = 0, ueber = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                bool imFenster = vp.UeberhoehungReiheK[h] > 0.0;
                if (imFenster) fenster++;
                if (lauf.UeberhoehtStunden[h])
                {
                    ueber++;
                    Assert.True(imFenster, "Überhöhung außerhalb des Fensters in Stunde " + h);
                }
                // Der Deckel der Stunde bleibt wirksam.
                double deckel = e.HeizleistungMaxBei(h);
                if (!double.IsNaN(deckel)) Assert.True(lauf.HeizlastW[h] <= deckel * (1.0 + 1e-9) + 1e-6, "Deckel verletzt in Stunde " + h);
            }
            Assert.True(ueber > 0);
            Assert.True(ueber < fenster, "Die Überhöhung endet nie bei Ankunft (" + ueber + " von " + fenster + " Fensterstunden).");
            Assert.Equal(ueber, vp.Nachweis.UeberhoehungStundenH);
            Assert.True(vp.Nachweis.UeberhoehungVorlaufMaxC > 0.0);
            // Am Ende jeder überhöhten Fensterfolge liegt eine Stunde ohne Überhöhung, wenn die Luft angekommen ist.
            _aus.WriteLine(FormattableString.Invariant($"Fensterstunden {fenster}, überhöht {ueber}, höchster Vorlauf {vp.Nachweis.UeberhoehungVorlaufMaxC:0.0} °C"));
        }

        [Fact]
        public void Die_Vorausschau_rechnet_die_Ueberhoehung_mit_und_die_berechnete_Vorheizzeit_wird_kuerzer()
        {
            (Aufheizplan ohne, _, _) = Rechnen(Ak1, Mit(Aufheizverfahren.Berechnet, false));
            (Aufheizplan mit, GebaeudeModellErgebnis lauf, _) = Rechnen(Ak1, Mit(Aufheizverfahren.Berechnet, true, 5.0));
            List<Vorheizsprung> a = ohne.Vorheizen.Spruenge.ToList(), b = mit.Vorheizen.Spruenge.ToList();
            Assert.Equal(a.Count, b.Count);
            int kuerzer = 0;
            for (int i = 0; i < a.Count; i++)
            {
                Assert.Equal(a[i].Sprungstunde, b[i].Sprungstunde);
                if (a[i].Unerreichbar || b[i].Unerreichbar) continue;
                Assert.True(b[i].BedarfH <= a[i].BedarfH, "Bedarf mit Überhöhung länger an h_s " + a[i].Sprungstunde);
                if (b[i].BedarfH < a[i].BedarfH) kuerzer++;
            }
            Assert.True(kuerzer > 0);
            Assert.True(mit.Vorheizen.VorheizzeitH <= ohne.Vorheizen.VorheizzeitH);
            Assert.Equal(5.0, mit.Vorheizen.Vorausschau.UeberhoehungK);
            Assert.Equal(0.0, ohne.Vorheizen.Vorausschau.UeberhoehungK);
            _aus.WriteLine(FormattableString.Invariant(
                $"t_V ohne {ohne.Vorheizen.VorheizzeitH} h, mit {mit.Vorheizen.VorheizzeitH} h; kürzer an {kuerzer} von {a.Count} Sprüngen; überhöht {lauf.UeberhoehtStunden?.Count(x => x) ?? 0} h"));
        }

        [Theory]
        [InlineData(Aufheizverfahren.Vorgabe)]
        [InlineData(Aufheizverfahren.Berechnet)]
        public void Ohne_Schalter_bleibt_der_Lauf_bitgleich(Aufheizverfahren verfahren)
        {
            (Aufheizplan pa, GebaeudeModellErgebnis la, _) = Rechnen(Ak1, new Aufheizvorgabe(true, null, null, null, null)
            {
                Vorheizen = new Vorheizvorgabe(verfahren, verfahren == Aufheizverfahren.Vorgabe ? 6 : null),
            });
            // Überhöhung gesetzt, Schalter aus: kein Unterschied.
            (Aufheizplan pb, GebaeudeModellErgebnis lb, GebaeudeModellEingang eb) = Rechnen(Ak1, Mit(verfahren, false, 7.0));
            Bitgleich(la, lb);
            Assert.Equal(pa.Vorheizen.VorheizzeitH, pb.Vorheizen.VorheizzeitH);
            Assert.Null(pb.Vorheizen.UeberhoehungReiheK);
            Assert.Null(eb.UeberhoehungReiheK);
            Assert.Null(lb.UeberhoehtStunden);
        }

        [Fact]
        public void Ein_Gebaeude_ohne_Heizkreis_bleibt_bitgleich_und_wird_gemeldet()
        {
            using var k = new Kulturvorrichtung("de-DE");
            (Aufheizplan pa, GebaeudeModellErgebnis la, _) = Rechnen(Ideal, Mit(Aufheizverfahren.Berechnet, false));
            (Aufheizplan pb, GebaeudeModellErgebnis lb, _) = Rechnen(Ideal, Mit(Aufheizverfahren.Berechnet, true, 5.0));
            Bitgleich(la, lb);
            Assert.Equal(pa.Vorheizen.VorheizzeitH, pb.Vorheizen.VorheizzeitH);
            Assert.True(pb.Vorheizen.UeberhoehungOhneHeizkreis);
            Assert.Equal(0.0, pb.Vorheizen.UeberhoehungK);
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Keiner, Vorheizplanung.Gebaeudewerte(new[] { pb }), "Gebäude O");
            Assert.Contains(p.Hinweise, z => z.Contains("Die Schnellaufheizung wirkt nur an Zonen mit Heizkreis und Heizkurve; an 1 Zonen bleibt sie ohne Wirkung.", StringComparison.Ordinal));
        }

        // =====================================================================
        //  Hinweise
        // =====================================================================

        [Fact]
        public void Die_Hinweise_nennen_Stunden_Vorlauf_Grenze_und_Sperrzeit()
        {
            using var k = new Kulturvorrichtung("de-DE");
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            var v = new Vorheizgebaeude
            {
                UeberhoehungK = 3.0,
                UeberhoehungStundenH = 120,
                UeberhoehungVorlaufMaxC = 58.25,
                UeberhoehungBegrenztH = 14,
                UeberhoehungWirkungslosH = 6,
                UeberhoehungGesperrtH = 9,
            };
            Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Keiner, v, "Gebäude S");
            Assert.Contains(p.Hinweise, z => z.Contains("Schnellaufheizung mit 3 K Überhöhung: 120 Stunden mit angehobenem Vorlauf, höchster Vorlauf 58,3 °C, in 14 Stunden begrenzt durch die Vorlaufgrenze der Anlage.", StringComparison.Ordinal));
            Assert.Contains(p.Hinweise, z => z.Contains("In 6 Fensterstunden bleibt die Schnellaufheizung wirkungslos: Der Vorlauf steht schon an der Vorlaufgrenze der Anlage (Vorlauf_Max).", StringComparison.Ordinal));
            Assert.Contains(p.Hinweise, z => z.Contains("In 9 Fensterstunden liegt die Schnellaufheizung in einer Sperrzeit der Anlage und bleibt dort ohne Wirkung.", StringComparison.Ordinal));
            // Ohne Schnellaufheizung schweigen die vier Texte.
            SimulationProtokoll q = SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisVorheizen(Vorheizrueckfall.Keiner, new Vorheizgebaeude(), "Gebäude T");
            Assert.DoesNotContain(q.Hinweise, z => z.Contains("Schnellaufheizung", StringComparison.Ordinal));
        }

        // =====================================================================
        //  Projektläufe (AK3-Kreis) und Messung
        // =====================================================================

        /// <summary>Der ganze Projektlauf mit Option 2 und — über die Testnaht bis SA2 — der Schnellaufheizung Δθ (NaN = aus).</summary>
        internal static SimulationRunner Projektlauf(int projekt, double dK)
        {
            Aufheizvorgabe basis = VorheizAk3Tests.ProjektvorgabeAn(projekt);
            Aufheizvorgabe v = basis with
            {
                Vorheizen = new Vorheizvorgabe(Aufheizverfahren.Berechnet, null, Vorheiztoleranzart.Prozent, 20.0, 1.0),
            };
            SimulationWaermebedarf.SchnellaufheizungTestnaht = double.IsNaN(dK) ? null : x => x.MitSchnellaufheizung(true, dK);
            try
            {
                return VorheizAk3Tests.Projektlauf(projekt, v);
            }
            finally
            {
                SimulationWaermebedarf.SchnellaufheizungTestnaht = null;
            }
        }

        private static IEnumerable<GebaeudeModellErgebnis> Ergebnisse(SimulationRunner r)
            => r.simulation_Waermebedarf.GebaeudeErgebnisse.Alle.Where(e => e != null);

        [Fact]
        public void Der_AK3_Kreis_bekommt_die_Ueberhoehung()
        {
            if (!_db.Vorhanden) return;
            using var k = new Kulturvorrichtung("de-DE");
            SimulationRunner r = Projektlauf(1058, 3.0);
            Assert.NotEmpty(r.simulation_Waermebedarf.Ak3.Gebaeude);
            int mit = 0;
            foreach (Ak3Weg.Eintrag g in r.simulation_Waermebedarf.Ak3.Gebaeude)
            {
                List<ZonenEingang> zonen = g.Stepper.Schleife == null
                    ? new List<ZonenEingang> { g.Stepper.ZoneEinzone }
                    : g.Stepper.Schleife.Laeufe.Select(l => l.Zone).ToList();
                foreach (ZonenEingang z in zonen.Where(z => z.Aufheizplan?.Vorheizen?.UeberhoehungReiheK != null))
                {
                    Assert.NotNull(z.Eingang.UeberhoehungReiheK);
                    mit++;
                }
                GebaeudeModellErgebnis e = r.simulation_Waermebedarf.GebaeudeErgebnisse.Ergebnis(g.Index);
                if (e.Vorheizen != null && e.Vorheizen.UeberhoehungK > 0.0) Assert.True(e.Vorheizen.UeberhoehungStundenH > 0);
            }
            Assert.True(mit > 0, "Keine Zone im Kreis trägt die Überhöhung.");
            Assert.Contains(r.Protokoll.Hinweise, z => z.Contains("Schnellaufheizung mit 3 K Überhöhung", StringComparison.Ordinal));
        }

        /// <summary>
        /// <b>Messung SA1</b> (kein harter Wert): je Projekt Option 2 (Toleranz 20 %, ε 1 K) ohne Schnellaufheizung und mit
        /// Δθ = 2, 3, 5 K über den ganzen Projektlauf — t_V, Bedarf, Nächte ohne Absenkung, Tage über t_V, Unterschreitung,
        /// Heizwärme, Strom und Arbeitszahl der Wärmepumpe, Spitze, höchster Vorlauf, Stunden an der Vorlaufgrenze.
        /// </summary>
        [Fact]
        public void Messung_SA1()
        {
            if (!_db.Vorhanden || Environment.GetEnvironmentVariable("EPOS_MESSUNG") != "1") return;
            using var k = new Kulturvorrichtung("de-DE");
            CultureInfo c = CultureInfo.InvariantCulture;
            _aus.WriteLine("| Projekt | Δθ K | t_V h | Bedarf max h | Nächte o. Absenkung | Tage über t_V | Unterschr. K | Heizwärme MWh | WP-Strom MWh | WP-Wärme MWh | AZ | Spitze kW | Vorlauf max °C | Std. überhöht | Std. Vorlauf_Max | wirkungslos | Zeit s |");
            _aus.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (int projekt in new[] { 1054, 1047, 1058, 1065 })
                foreach (double dK in new[] { double.NaN, 2.0, 3.0, 5.0 })
                {
                    var uhr = System.Diagnostics.Stopwatch.StartNew();
                    SimulationRunner r = Projektlauf(projekt, dK);
                    double s = uhr.Elapsed.TotalSeconds;
                    List<GebaeudeModellErgebnis> alle = Ergebnisse(r).ToList();
                    double q = alle.Sum(e => e.HeizlastW.Sum()) / 1e6;
                    SimulationWaermepumpe wp = r.sim.simulation_wp;
                    double strom = wp.WpStrombedarfGesamtKwh / 1000.0, wpq = wp.WpWaermeproduktionGesamtKwh / 1000.0;
                    double vorlaufMax = alle.Where(e => e.Heizkreis != null).SelectMany(e => e.Heizkreis.VorlaufC)
                                            .Where(x => !double.IsNaN(x)).DefaultIfEmpty(double.NaN).Max();
                    foreach (GebaeudeModellErgebnis e in alle.Where(x => x.Vorheizen != null))
                    {
                        Vorheizgebaeude v = e.Vorheizen;
                        _aus.WriteLine(string.Format(c,
                            "| {0} | {1} | {2} | {3} | {4} | {5} | {6:0.00} | {7:0.000} | {8:0.000} | {9:0.000} | {10:0.000} | {11:0.0} | {12:0.0} | {13} | {14} | {15} | {16:0} |",
                            projekt, double.IsNaN(dK) ? "aus" : dK.ToString("0", c), v.VorheizzeitMaxH, v.BedarfMaxH,
                            v.NaechteOhneAbsenkung, v.TageUeberVorheizzeit, v.UnterschreitungMaxK, q, strom, wpq,
                            strom > 0.0 ? wpq / strom : double.NaN, v.SpitzeW / 1000.0, vorlaufMax, v.UeberhoehungStundenH,
                            v.UeberhoehungBegrenztH, v.UeberhoehungWirkungslosH, s));
                    }
                    foreach (string z in r.Protokoll.Hinweise.Where(z => z.Contains("Schnellaufheizung", StringComparison.Ordinal)))
                        _aus.WriteLine("  " + z);
                }
        }
    }
}
