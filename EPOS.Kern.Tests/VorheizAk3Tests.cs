using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Vorheizen im Kreis AK3 und in den Auskunftswegen</b> (Entwurf Vorheizrampe Fassung 2, 2.7; Welle V3b): Der Plan beider
    /// Verfahren reist mit dem Eingang in den Stepper des Kreises, der Abschluss des Kreises weist am Kreisergebnis nach (ohne
    /// Eingriff der Anlage bitgleich zum Profilweg, mit Eingriff misst er den Kreis); Projekt 1058 (AK3) rechnet Option 1 und 2
    /// ohne Rückfall; die Auskunftswege bekommen mit gesetztem Verfahren denselben Plan wie der Lauf.
    /// </summary>
    [Collection("Testdatenbank")]
    public class VorheizAk3Tests : IClassFixture<TestDatenbank>
    {
        private const int STUNDEN = 8760;

        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public VorheizAk3Tests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        internal static Aufheizvorgabe Verfahren(bool berechnet, Aufheizvorgabe basis = null)
            => (basis ?? new Aufheizvorgabe(true, null, null, null, null)) with
            {
                Vorheizen = berechnet
                    ? new Vorheizvorgabe(Aufheizverfahren.Berechnet)
                    : new Vorheizvorgabe(Aufheizverfahren.Vorgabe, 6),
            };

        private static GebaeudeModellEingang Probeeingang()
        {
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            e.HeizsollwertMitRampeSetzen(VorheizVorgabeTests.Kalender());
            return e;
        }

        private static void Gleich(double[] a, double[] b, string wo)
        {
            Assert.Equal(a == null, b == null);
            if (a == null) return;
            Assert.Equal(a.Length, b.Length);
            for (int h = 0; h < a.Length; h++)
                if (Bits(a[h]) != Bits(b[h])) Assert.Fail(wo + ": Stunde " + h + " " + a[h] + " ≠ " + b[h]);
        }

        // =====================================================================
        //  Der Kreis (synthetisch)
        // =====================================================================

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Der_Plan_reist_in_den_Kreis_und_der_Abschluss_weist_am_Kreisergebnis_nach(bool berechnet)
        {
            GebaeudeModellEingang e = Probeeingang();
            Aufheizplan plan = Vorheizplanung.AnwendenEinzone(e, Verfahren(berechnet), 0, 1);
            GebaeudeModellErgebnis lauf = Vdi6007Rechenweg.Laufen(e, 0, 1, plan);
            Vorheizplanung.NachweisenEinzone(e, plan, lauf);
            Vorheizgebaeude pass1 = Vorheizplanung.Gebaeudewerte(new[] { plan });
            Assert.NotNull(pass1);

            // Der Kreis baut wie Vdi6007Rechenweg.Rechnen an Ak3Erfassen: derselbe Eingang, der Plan an der Zone.
            GebaeudeStepper Kreis()
            {
                ZonenEingang z = ZonenEingang.Einzeln(e);
                z.AufheizplanSetzen(plan);
                return GebaeudeStepper.Einzone(z);
            }
            GebaeudeStepper st = Kreis();
            Gleich(plan.Reihe, st.EingangEinzone.ThetaSoll, "Sollwertreihe im Stepper");
            Gleich(plan.Deckelreihe, st.EingangEinzone.HeizleistungMaxReiheW, "Deckelreihe im Stepper");
            st.Beginnen();
            st.Jahr();
            GebaeudeModellErgebnis k = Zonenrechnung.Abschluss(st, 0, 1);
            Assert.NotNull(k.Vorheizen);
            Gleich(lauf.HeizlastW, k.HeizlastW, "Kreis ohne Eingriff = Profilweg");
            Assert.Equal(pass1.TageOhneAnkunftAnzahl, k.Vorheizen.TageOhneAnkunftAnzahl);
            Assert.Equal(Bits(pass1.UnterschreitungMaxK), Bits(k.Vorheizen.UnterschreitungMaxK));
            Assert.Equal(Bits(pass1.SpitzeW), Bits(k.Vorheizen.SpitzeW));
            Assert.Equal(Bits(pass1.MehrwaermeKwh), Bits(k.Vorheizen.MehrwaermeKwh));
            Assert.Equal(pass1.VorheizzeitMaxH, k.Vorheizen.VorheizzeitMaxH);

            // Mit Eingriff der Anlage — der Kreis liefert in den Vorheizfenstern nur 300 W: Der Nachweis misst den Kreis.
            GebaeudeStepper st2 = Kreis();
            st2.Beginnen();
            Stundenrand Anlage(int zone, int h, in Stundenrand r)
                => plan.Rampenmaske[h] ? r.MitVorheizen(r.ThetaSoll, 300.0) : r;
            for (int h = 0; h < STUNDEN; h++)
            {
                st2.Schritt(h, Anlage);
                st2.Festschreiben(h);
            }
            GebaeudeModellErgebnis k2 = Zonenrechnung.Abschluss(st2, 0, 1);
            _aus.WriteLine($"berechnet={berechnet}: Tage ohne Ankunft Profilweg {pass1.TageOhneAnkunftAnzahl}, Kreis mit Eingriff {k2.Vorheizen.TageOhneAnkunftAnzahl}");
            Assert.True(k2.Vorheizen.TageOhneAnkunftAnzahl > pass1.TageOhneAnkunftAnzahl);
            Assert.Equal(Bits(k2.HeizlastW.Max()), Bits(k2.Vorheizen.SpitzeW));
        }

        // =====================================================================
        //  Projekt 1058 (AK3) über den ganzen Lauf
        // =====================================================================

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Projekt_1058_AK3_rechnet_beide_Verfahren_im_Kreis_ohne_Rueckfall(bool berechnet)
        {
            if (!_db.Vorhanden) return;
            using var k = new Kulturvorrichtung("de-DE");
            SimulationRunner r = Projektlauf(1058, Verfahren(berechnet, ProjektvorgabeAn(1058)));
            Assert.NotEmpty(r.simulation_Waermebedarf.Ak3.Gebaeude);
            int geprueft = 0;
            foreach (Ak3Weg.Eintrag g in r.simulation_Waermebedarf.Ak3.Gebaeude)
            {
                List<ZonenEingang> zonen = g.Stepper.Schleife == null
                    ? new List<ZonenEingang> { g.Stepper.ZoneEinzone }
                    : g.Stepper.Schleife.Laeufe.Select(l => l.Zone).ToList();
                foreach (ZonenEingang z in zonen.Where(z => z.Aufheizplan?.Vorheizen != null))
                {
                    Assert.Equal(berechnet ? Aufheizverfahren.Berechnet : Aufheizverfahren.Vorgabe, z.Aufheizplan.Vorheizen.Verfahren);
                    Assert.NotNull(z.Aufheizplan.Vorheizen.Nachweis);
                    Gleich(z.Aufheizplan.Deckelreihe, z.Eingang.HeizleistungMaxReiheW, "Deckelreihe im Kreis");
                }
                GebaeudeModellErgebnis e = r.simulation_Waermebedarf.GebaeudeErgebnisse.Ergebnis(g.Index);
                Assert.NotNull(e.Vorheizen);
                // Der Nachweis gehört zum Kreisergebnis: die Spitze ist die Spitze der Heizlast des Kreises.
                Assert.Equal(e.HeizlastW.Max(), e.Vorheizen.SpitzeW, 6);
                geprueft++;
            }
            Assert.True(geprueft > 0);
            Assert.DoesNotContain(r.Protokoll.Hinweise, z => z.Contains("(AK3)", StringComparison.Ordinal) && z.Contains("Vorheiz", StringComparison.Ordinal));
            Assert.Contains(r.Protokoll.Hinweise, z => z.Contains("Vorheiz", StringComparison.Ordinal));
        }

        internal static Aufheizvorgabe ProjektvorgabeAn(int projekt)
        {
            Aufheizvorgabe v = KonfigurationCtrl.AufheizvorgabeLesen(projekt) ?? new Aufheizvorgabe(true, null, null, null, null);
            return v.An ? v : v.Eingeschaltet();
        }

        /// <summary>Der ganze Projektlauf mit der Aufheizvorgabe <paramref name="vorgabe"/> (Testnaht; <c>null</c> = die des Projekts).</summary>
        internal static SimulationRunner Projektlauf(int projekt, Aufheizvorgabe vorgabe)
        {
            SimulationProtokoll.NeuStarten();
            SimulationWaermebedarf.AufheizvorgabeTestnaht = vorgabe == null ? null : _ => vorgabe;
            try
            {
                var r = new SimulationRunner();
                Assert.True(r.Simuliere(projekt, out string fehler), "Lauf " + projekt + " gescheitert: " + fehler);
                return r;
            }
            finally
            {
                SimulationWaermebedarf.AufheizvorgabeTestnaht = null;
            }
        }

        // =====================================================================
        //  Auskunftswege
        // =====================================================================

        [Theory]
        [InlineData(1051, true)]
        [InlineData(1054, true)]
        [InlineData(1054, false)]
        public void Auskunftswege_liefern_mit_gesetztem_Verfahren_denselben_Plan_wie_der_Lauf(int projektId, bool berechnet)
        {
            if (!_db.Vorhanden) return;
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(projektId);
            var sim = new SimulationWaermebedarf { m_ID_Projekt = projektId };
            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
            sim.AufheizvorgabeProjekt = Verfahren(berechnet);
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(projektId);
            int geprueft = 0;
            for (int i = 0; i < ctrl.rows; i++)
            {
                ProjektGebaeudeModel item = ctrl.items[i];
                if (!Gebaeuderechenweg.IstVdi6007(item.Gebaeude_Modell)) continue;
                Assert.True(sim.HeizwaermeEinesGebaeudes(item, i, new double[STUNDEN]));
                Aufheizauskunft a = sim.AufheizbemessungEinesGebaeudes(item);
                Assert.Null(a.Befund);
                Assert.NotNull(a.Zustand);
                List<Aufheizplan> laufplaene, auskunft;
                if (Vdi6007Rechenweg.Mehrzonenweg(item))
                {
                    laufplaene = sim.Vdi6007weg.LetztesMehrzonenergebnis.Eingaenge.Select(z => z.Aufheizplan).ToList();
                    auskunft = sim.Vdi6007weg.ZonenBauen(item, sim.Kalender.Gemeinsam, i).Select(z => z.Aufheizplan).ToList();
                }
                else
                {
                    laufplaene = new List<Aufheizplan> { sim.Vdi6007weg.LetzterAufheizplan };
                    GebaeudeModellEingang e = sim.Vdi6007weg.EingangBauen(item, sim.Kalender.Gemeinsam);
                    auskunft = new List<Aufheizplan> { sim.Vdi6007weg.AufheizplanEinzone(e, i, item.ID_Gebaeude, out _) };
                }
                Assert.Equal(laufplaene.Count, auskunft.Count);
                for (int z = 0; z < laufplaene.Count; z++)
                {
                    Aufheizplan l = laufplaene[z], x = auskunft[z];
                    Assert.Equal(l.Vorheizen == null, x.Vorheizen == null);
                    if (l.Vorheizen == null) continue;
                    Assert.Equal(l.Vorheizen.VorheizzeitH, x.Vorheizen.VorheizzeitH);
                    Assert.Equal(l.Vorheizen.BedarfMaxH, x.Vorheizen.BedarfMaxH);
                    Gleich(l.Reihe, x.Reihe, "Sollwertreihe Zone " + z);
                    Gleich(l.Deckelreihe, x.Deckelreihe, "Deckelreihe Zone " + z);
                    geprueft++;
                }
            }
            Assert.True(geprueft > 0);
        }
    }
}
