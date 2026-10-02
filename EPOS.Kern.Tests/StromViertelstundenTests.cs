using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Welle M5 „Strom in Viertelstunden"</b> der Entscheidungsvorlage Modellgrenzen: die PV-Bilanz
    /// in 35 040 Viertelstunden (SB1 a), die Einspeisegrenze mit Abregelung (PV3) und der Eigenverbrauch
    /// des Speichersystems (SP1).
    ///
    /// <para><b>Teil 1 ohne Datenbank:</b> Viertelgewichte nach dem Sonnenstand, energieerhaltende
    /// Verteilung, gleichmäßige Nachtstunde, Direktverbrauch je Viertel, Einspeisegrenze in kW und in
    /// Prozent, Laden und Standby vor dem Abregeln. <b>Teil 2 auf der Testdatenbank:</b> die Anker der
    /// Referenzprojekte 1045 und 1046 auf Basis R33 (Werte der Basis R32 im Kommentar), die
    /// Einspeisegrenze an 1046 (Flotte, weiche Grenze) und an 1040 (ohne Speicher), der Standby am
    /// Speicher von 1007, Lesen und Schreiben der Projekteinstellung.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class StromViertelstundenTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int STUNDEN = 8760;
        private const int VIERTEL = STUNDEN * 4;

        // =============================================================================
        //  Teil 1 - ohne Datenbank
        // =============================================================================

        /// <summary>
        /// Die Viertelgewichte einer Mittagsstunde im Sommer summieren zu eins und sind alle positiv; eine
        /// Mitternachtsstunde hat keine Sonne und trägt NaN (gleichmäßig). Am Vormittag steigt das
        /// Gewicht über die Stunde, am Nachmittag fällt es.
        /// </summary>
        [Fact]
        public void Viertelgewichte_folgen_dem_Sonnenstand_und_summieren_zu_eins()
        {
            SolardatenCtrl klima = Klimajahr();
            double[] w = SimulationPV.Viertelgewichte(klima, 10.0, 50.0);
            Assert.Equal(VIERTEL, w.Length);

            int mittag = 171 * 24 + 11;   // 21. Juni, 11 bis 12 Uhr UTC
            double summe = 0;
            for (int k = 0; k < 4; k++)
            {
                Assert.True(w[mittag * 4 + k] > 0.2 && w[mittag * 4 + k] < 0.3, "Gewicht " + w[mittag * 4 + k]);
                summe += w[mittag * 4 + k];
            }
            Assert.Equal(1.0, summe, 12);

            int vormittag = 171 * 24 + 6, nachmittag = 171 * 24 + 16;
            Assert.True(w[vormittag * 4] < w[vormittag * 4 + 3], "Vormittag steigt nicht.");
            Assert.True(w[nachmittag * 4] > w[nachmittag * 4 + 3], "Nachmittag fällt nicht.");

            int nacht = 0;   // 1. Januar, 0 bis 1 Uhr UTC
            for (int k = 0; k < 4; k++) Assert.True(double.IsNaN(w[nacht * 4 + k]));
        }

        /// <summary>
        /// Die Verteilung ist energieerhaltend: Das Mittel der vier Viertel ist der Stundenertrag, Stunde
        /// für Stunde; in einer Stunde ohne Sonne (Gewicht NaN) tragen alle vier Viertel den Stundenwert.
        /// </summary>
        [Fact]
        public void Verteilung_ist_energieerhaltend_und_die_Nachtstunde_gleichmaessig()
        {
            double[] w = SimulationPV.Viertelgewichte(Klimajahr(), 10.0, 50.0);
            var pv = new SimulationPV();
            pv.Strombedarf = new double[VIERTEL];
            for (int h = 0; h < STUNDEN; h++)
                pv.pvPotentialGesamt_stuendlich[h] = 3.0 + Math.Sin(h * 0.37) * 2.0;   // auch nachts > 0
            pv.Bilanzieren(w, null);

            for (int h = 0; h < STUNDEN; h++)
            {
                double mittel = (pv.Stromproduktion_Theoretisch_viertelstunde[h * 4] + pv.Stromproduktion_Theoretisch_viertelstunde[h * 4 + 1] +
                                 pv.Stromproduktion_Theoretisch_viertelstunde[h * 4 + 2] + pv.Stromproduktion_Theoretisch_viertelstunde[h * 4 + 3]) / 4.0;
                Assert.Equal(pv.pvPotentialGesamt_stuendlich[h], mittel, 9);
            }
            Assert.Equal(pv.pvPotentialGesamt_stuendlich.Sum(),
                         SimulationPV.ViertelstundenKwh(pv.Stromproduktion_Theoretisch_viertelstunde), 6);

            // Nachtstunde: gleichmäßig.
            for (int k = 0; k < 4; k++)
                Assert.Equal(pv.pvPotentialGesamt_stuendlich[0], pv.Stromproduktion_Theoretisch_viertelstunde[k]);

            // Ohne Last ist alles Überschuss, nichts Direktverbrauch; die Stundenreihen sind Mittel.
            Assert.Equal(0.0, pv.StromproduktionGesamtKwh);
            Assert.Equal(pv.pvPotentialGesamt_stuendlich.Sum(), pv.Ueberschuss.Sum(), 6);
            Assert.Equal(0.0, pv.AbregelungGesamtKwh);
        }

        /// <summary>
        /// Bei einer Lastspitze in EINEM Viertel deckt die PV je Viertel weniger direkt als im
        /// Stundenmittel: Stunde 4 kW PV gleichmäßig, Last 16/0/0/0 kW (Mittel 4 kW). Stündlich wären
        /// 4 kWh Direktverbrauch, je Viertel nur 1 kWh; der Rest ist Überschuss und Reststrom.
        /// </summary>
        [Fact]
        public void Direktverbrauch_je_Viertel_ist_hoechstens_der_stuendliche_bei_Spitzenlast()
        {
            var pv = new SimulationPV();
            pv.Strombedarf = new double[VIERTEL];
            pv.pvPotentialGesamt_stuendlich[100] = 4.0;
            pv.Strombedarf[400] = 16.0;
            pv.Bilanzieren(null, null);

            double stuendlich = Math.Min(4.0, 16.0 / 4.0);
            Assert.Equal(1.0, pv.Stromproduktion[100], 12);
            Assert.True(pv.Stromproduktion[100] <= stuendlich);
            Assert.Equal(new[] { 4.0, 0.0, 0.0, 0.0 }, pv.Stromproduktion_viertelstunde.Skip(400).Take(4));
            Assert.Equal(new[] { 0.0, 4.0, 4.0, 4.0 }, pv.Ueberschuss_viertelstunde.Skip(400).Take(4));
            Assert.Equal(new[] { 12.0, 0.0, 0.0, 0.0 }, pv.Reststrom_viertelstunde.Skip(400).Take(4));
            Assert.Equal(3.0, pv.Ueberschuss[100], 12);
            Assert.Equal(3.0, pv.Reststrom[100], 12);
        }

        /// <summary>
        /// Ein negativer Bedarf ist BHKW-Überschuss (V1) — kein Bedarf, keine PV-Größe — je Viertel.
        /// </summary>
        [Fact]
        public void Negativer_Bedarf_ist_BHKW_Ueberschuss_je_Viertel()
        {
            var pv = new SimulationPV();
            pv.Strombedarf = new double[VIERTEL];
            pv.pvPotentialGesamt_stuendlich[0] = 2.0;
            pv.Strombedarf[0] = -4.0;
            pv.Strombedarf[1] = 8.0;
            pv.Bilanzieren(null, null);

            Assert.Equal(1.0, pv.BhkwUeberschuss[0], 12);
            Assert.Equal(new[] { 0.0, 2.0, 0.0, 0.0 }, pv.Stromproduktion_viertelstunde.Take(4));
            Assert.Equal(new[] { 2.0, 0.0, 2.0, 2.0 }, pv.Ueberschuss_viertelstunde.Take(4));
        }

        /// <summary>
        /// Einspeisegrenze in kW: 10 kW Überschuss je Viertel gegen 6 kW Grenze - 4 kW Abregelung.
        /// Abregelung = Überschuss − Einspeisung, Viertel für Viertel und im Jahr.
        /// </summary>
        [Fact]
        public void Einspeisegrenze_in_kW_regelt_den_Ueberschuss_darueber_ab()
        {
            var pv = new SimulationPV();
            pv.Strombedarf = new double[VIERTEL];
            for (int h = 0; h < 24; h++) pv.pvPotentialGesamt_stuendlich[h] = 10.0;
            pv.Bilanzieren(null, 6.0);

            Assert.Equal(6.0, pv.EinspeisegrenzeKw);
            Assert.Equal(4.0, pv.Abregelung_viertelstunde[5], 12);
            Assert.Equal(24 * 4.0, pv.AbregelungGesamtKwh, 9);

            pv.EinspeisungAufteilen(null, null, out double[] ein, out double[] abr);
            for (int q = 0; q < 96; q++)
            {
                Assert.Equal(6.0, ein[q], 12);
                Assert.Equal(pv.Ueberschuss_viertelstunde[q] - ein[q], abr[q], 12);
            }
            Assert.Equal(pv.AbregelungGesamtKwh, SimulationPV.ViertelstundenKwh(abr), 9);
            Assert.Equal(24 * 6.0, SimulationPV.ViertelstundenKwh(ein), 9);
        }

        /// <summary>
        /// Ohne Grenze ist die Aufteilung der Überschuss selbst und die Abregelung null — der Vorgabefall.
        /// </summary>
        [Fact]
        public void Ohne_Einspeisegrenze_wird_nichts_abgeregelt()
        {
            var pv = new SimulationPV();
            pv.Strombedarf = new double[VIERTEL];
            pv.pvPotentialGesamt_stuendlich[12] = 50.0;
            pv.Bilanzieren(null, null);
            pv.EinspeisungAufteilen(null, null, out double[] ein, out double[] abr);

            Assert.Null(pv.EinspeisegrenzeKw);
            Assert.Equal(pv.Ueberschuss_viertelstunde, ein);
            Assert.All(abr, a => Assert.Equal(0.0, a));
        }

        /// <summary>
        /// Laden vor Abregeln, Standby vor Einspeisung: 10 kW Überschuss, Ladung 1 kWh (4 kW), Standby
        /// aus PV 1 kW - für das Netz bleiben 5 kW, die Grenze 4 kW regelt 1 kW ab. Ein Speicher, der
        /// den ganzen Überschuss lädt, lässt nichts abzuregeln.
        /// </summary>
        [Fact]
        public void Laden_und_Standby_kommen_vor_dem_Abregeln()
        {
            var pv = new SimulationPV();
            pv.Strombedarf = new double[VIERTEL];
            pv.pvPotentialGesamt_stuendlich[0] = 10.0;
            pv.Bilanzieren(null, 4.0);

            var ladung = new double[VIERTEL];
            var standby = new double[VIERTEL];
            ladung[0] = 1.0;          // kWh im Viertel = 4 kW
            standby[0] = 1.0;         // kW aus PV
            ladung[1] = 2.5;          // der ganze Überschuss
            pv.EinspeisungAufteilen(ladung, standby, out double[] ein, out double[] abr);

            Assert.Equal(4.0, ein[0], 12);
            Assert.Equal(1.0, abr[0], 12);
            Assert.Equal(0.0, ein[1], 12);
            Assert.Equal(0.0, abr[1], 12);
            Assert.Equal(4.0, ein[2], 12);
            Assert.Equal(6.0, abr[2], 12);
        }

        /// <summary>
        /// Einspeisegrenze in Prozent: 70 % von 10 kWp sind 7 kW; ohne installierte Leistung gibt es keine
        /// Bezugsgröße (null); leer, negativ oder nicht endlich heißt keine Grenze; kW bleibt der Wert.
        /// </summary>
        [Fact]
        public void Einspeisegrenze_in_Prozent_bezieht_sich_auf_die_installierte_Leistung()
        {
            Assert.Equal(7.0, new Einspeisegrenze(70, DbWerte.EINSPEISEGRENZE_PROZENT).Kw(10.0).Value, 12);
            Assert.Null(new Einspeisegrenze(70, DbWerte.EINSPEISEGRENZE_PROZENT).Kw(0.0));
            Assert.Equal(12.5, new Einspeisegrenze(12.5, DbWerte.EINSPEISEGRENZE_KW).Kw(100.0).Value, 12);
            Assert.Equal(12.5, new Einspeisegrenze(12.5, null).Kw(0.0).Value, 12);
            Assert.Equal(0.0, new Einspeisegrenze(0, null).Kw(0.0).Value, 12);
            Assert.Equal(Einspeisegrenze.Keine, new Einspeisegrenze(null, null));
            Assert.Equal(Einspeisegrenze.Keine, new Einspeisegrenze(-1, null));
            Assert.Equal(Einspeisegrenze.Keine, new Einspeisegrenze(double.NaN, "kW"));
            Assert.False(Einspeisegrenze.Keine.Gesetzt);
            Assert.Equal("kW", new Einspeisegrenze(5, "kW").EinheitWirksam);
            Assert.Null(new Einspeisegrenze(5, "kW").Einheit);
        }

        // =============================================================================
        //  Teil 2 - auf der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Lesen, Schreiben und Leeren der Projekteinstellung; ohne Satz ist „keine Grenze" schon wahr.
        /// </summary>
        [Fact]
        public void Die_Einspeisegrenze_wird_gelesen_geschrieben_und_geleert()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(Einspeisegrenze.Keine, KonfigurationCtrl.EinspeisegrenzeLesen(1046));

            var grenze = new Einspeisegrenze(60, DbWerte.EINSPEISEGRENZE_PROZENT);
            Assert.True(KonfigurationCtrl.EinspeisegrenzeSetzen(1046, grenze));
            Assert.Equal(grenze, KonfigurationCtrl.EinspeisegrenzeLesen(1046));
            double kwp = PhotovoltaikCtrl.KwpDesProjekts(1046);
            Assert.True(kwp > 0);
            Assert.Equal(0.6 * kwp, KonfigurationCtrl.EinspeisegrenzeKwLesen(1046).Value, 9);

            Assert.True(KonfigurationCtrl.EinspeisegrenzeSetzen(1046, Einspeisegrenze.Keine));
            Assert.Equal(Einspeisegrenze.Keine, KonfigurationCtrl.EinspeisegrenzeLesen(1046));
            Assert.Null(KonfigurationCtrl.EinspeisegrenzeKwLesen(1046));
        }

        /// <summary>
        /// <b>Anker der Basis R33</b> an den Referenzprojekten 1045 (PV ohne Speicher) und 1046 (PV mit
        /// Speicherflotte): Eigenverbrauch (Erzeugung − Einspeisung), Einspeisung und Restbezug in MWh/a.
        /// Die Werte der Basis R32 (Bilanz je Stunde) stehen im Kommentar.
        /// </summary>
        [Fact]
        public void Anker_der_Basis_R33_an_1045_und_1046()
        {
            if (!_db.Vorhanden) return;

            Lauf a = Rechne(1045);
            ErgebnisPhotovoltaikModel p = a.Ergebnis.Photovoltaik;
            Assert.Equal(ANKER_1045_ERZEUGUNG, p.Stromproduktion, 3);
            Assert.Equal(ANKER_1045_EINSPEISUNG, p.Ueberschuss, 3);         // R32: 0,78
            Assert.Equal(ANKER_1045_RESTBEZUG, a.Sim.ReststromMwh, 3);     // R32: 28,7452
            Assert.Equal(0.0, a.Sim.simulation_pv.AbregelungGesamtKwh);

            Lauf b = Rechne(1046);
            ErgebnisPhotovoltaikModel q = b.Ergebnis.Photovoltaik;
            Assert.Equal(ANKER_1046_ERZEUGUNG, q.Stromproduktion, 3);
            Assert.Equal(ANKER_1046_EINSPEISUNG, q.Ueberschuss, 3);         // R32: 0,89
            Assert.Equal(ANKER_1046_RESTBEZUG, b.Sim.ReststromMwh, 3);     // R32: 63,8961
            Assert.NotNull(b.Sim.Speicherflottennetzbilanz);
            Assert.Equal(0.0, b.Sim.Speicherflottennetzbilanz.PvAbregelungKwh);
        }

        /// <summary>
        /// Einspeisegrenze an 1040 (PV ohne Speicher, VDI-Weg): Mit 1 kW wird abgeregelt; die Einspeisung
        /// sinkt um genau die Abregelung, Erzeugung, Direktverbrauch und Restbezug bleiben. Die Reihe steht
        /// in den Zeitreihen des Berichts.
        /// </summary>
        [Fact]
        public void Einspeisegrenze_am_Projekt_1040_regelt_ab_ohne_den_Restbezug_zu_aendern()
        {
            if (!_db.Vorhanden) return;
            Lauf vorher = Rechne(1040);

            Assert.True(KonfigurationCtrl.EinspeisegrenzeSetzen(1040, new Einspeisegrenze(1.0, DbWerte.EINSPEISEGRENZE_KW)));
            Lauf nachher = Rechne(1040);
            ErgebnisPhotovoltaikModel v = vorher.Ergebnis.Photovoltaik, n = nachher.Ergebnis.Photovoltaik;

            double abregelungMwh = nachher.Sim.simulation_pv.AbregelungGesamtKwh / 1000.0;
            Assert.Equal(1.0, nachher.Sim.simulation_pv.EinspeisegrenzeKw);
            Assert.True(abregelungMwh > 0.1, "Abregelung " + abregelungMwh);
            Assert.Equal(v.Stromproduktion, n.Stromproduktion, 9);
            Assert.Equal(v.Ueberschuss - abregelungMwh, n.Ueberschuss, 9);
            Assert.Equal(vorher.Sim.ReststromMwh, nachher.Sim.ReststromMwh, 9);

            SimulationErgebnisCtrl.PhotovoltaikErgebnis reiter = SimulationErgebnisCtrl.Photovoltaik(nachher.Sim);
            Assert.Equal(abregelungMwh, reiter.AbregelungMwh, 9);
            Assert.Equal(abregelungMwh / n.Stromproduktion * 100.0, reiter.AbregelungProzent, 9);
            Assert.NotNull(nachher.Reihen.Hole(ZeitreihenSatz.PV_ABREGELUNG));
            Assert.Null(vorher.Reihen.Hole(ZeitreihenSatz.PV_ABREGELUNG));
        }

        /// <summary>
        /// Einspeisegrenze an 1046 (Speicherflotte): Die Flotte liest sie als WEICHE Grenze - sie lädt
        /// zuerst und regelt dann ab; die Variante bleibt zulässig, und die Abregelung steht in der
        /// Flottenbilanz.
        /// </summary>
        [Fact]
        public void Einspeisegrenze_an_der_Flotte_von_1046_ist_eine_weiche_Grenze()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KonfigurationCtrl.EinspeisegrenzeSetzen(1046, new Einspeisegrenze(10, DbWerte.EINSPEISEGRENZE_PROZENT)));
            Lauf l = Rechne(1046);

            double grenze = l.Sim.simulation_pv.EinspeisegrenzeKw.Value;
            Assert.Equal(0.1 * PhotovoltaikCtrl.KwpDesProjekts(1046), grenze, 9);
            SpeicherFlottenNetzbilanz bilanz = l.Sim.Speicherflottennetzbilanz;
            Assert.NotNull(bilanz);
            Assert.True(l.Sim.Speicherflottenergebnis.Variante.Zulaessig);
            Assert.True(bilanz.PvAbregelungKwh > 0, "keine Abregelung");
            Assert.All(bilanz.PvNetzeinspeisungKw, w => Assert.True(w <= grenze + 1e-9, "Einspeisung " + w));
            Assert.Equal(bilanz.PvAbregelungKwh / 1000.0, SimulationErgebnisCtrl.Photovoltaik(l.Sim).AbregelungMwh, 9);
        }

        /// <summary>
        /// Standby am Speicher von 1007 (Dauernutzung, PV): 20 W je Viertelstunde, gedeckt aus dem
        /// PV-Überschuss nach der Ladung, sonst aus dem Netz - nie aus der Batterie. Der Netzanteil steht im
        /// Restbezug, der PV-Anteil mindert die Einspeisung; die Ladung bleibt, wie sie war.
        /// </summary>
        [Fact]
        public void Standby_am_Speicher_von_1007_kommt_aus_PV_sonst_aus_dem_Netz()
        {
            if (!_db.Vorhanden) return;
            Lauf vorher = Rechne(1007);
            Assert.True(vorher.Sim.bSimulationSSP);
            Assert.Equal(0.0, vorher.Sim.SpeichersystemEigenverbrauchKwh);

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Stromspeicher SET Standby_Verbrauch = 20 WHERE ID IN " +
                "(SELECT ID_SP FROM Tab_Energieanlagen WHERE ID_Projekt = 1007 AND ID_SP IS NOT NULL)");
            Lauf nachher = Rechne(1007);
            SimulationControl s = nachher.Sim;

            double stunden = 8760.0;
            Assert.True(s.SpeichersystemEigenverbrauchKwh > 0);
            double standbyKw = s.Speicherkontext.Parameter.StandbyKw;
            Assert.True(standbyKw >= 0.020 - 1e-12, "Standby " + standbyKw);
            Assert.Equal(standbyKw * stunden, s.SpeichersystemEigenverbrauchKwh, 6);
            double ausNetz = SimulationPV.ViertelstundenKwh(s.SpeichersystemStandbyAusNetzKw);
            double ausPv = SimulationPV.ViertelstundenKwh(s.SpeichersystemStandbyAusPvKw);
            Assert.True(ausNetz > 0 && ausPv > 0, "aus Netz " + ausNetz + ", aus PV " + ausPv);
            Assert.Equal(s.SpeichersystemEigenverbrauchKwh, ausNetz + ausPv, 6);

            // Die Batterie fährt unverändert; der Netzanteil steht im Restbezug, der PV-Anteil fehlt in
            // der Einspeisung.
            Assert.Equal(vorher.Sim.Speicherergebnis.LadeenergieKwh, s.Speicherergebnis.LadeenergieKwh, 9);
            Assert.Equal(vorher.Sim.ReststromMwh + ausNetz / 1000.0, s.ReststromMwh, 9);
            Assert.Equal(vorher.Ergebnis.Photovoltaik.Ueberschuss - ausPv / 1000.0, nachher.Ergebnis.Photovoltaik.Ueberschuss, 9);
            Assert.NotNull(nachher.Reihen.Hole(ZeitreihenSatz.SPEICHER_EIGENVERBRAUCH));
        }

        // -----------------------------------------------------------------------------
        //  Anker (Basis R33)
        // -----------------------------------------------------------------------------

        private const double ANKER_1045_ERZEUGUNG = 3.5455;     // R32: 3,5455 (Erzeugung unverändert)
        private const double ANKER_1045_EINSPEISUNG = 0.7834;
        private const double ANKER_1045_RESTBEZUG = 28.7470;
        private const double ANKER_1046_ERZEUGUNG = 6.0143;     // R32: 6,0143 (Erzeugung unverändert)
        private const double ANKER_1046_EINSPEISUNG = 0.8999;
        private const double ANKER_1046_RESTBEZUG = 63.9011;

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private sealed class Lauf
        {
            public SimulationRunner Runner;
            public ErgebnisModel Ergebnis;
            public ZeitreihenSatz Reihen;
            public SimulationControl Sim => Runner.sim;
        }

        private static Lauf Rechne(int idProjekt)
        {
            var r = new SimulationRunner();
            Assert.True(r.Simuliere(idProjekt, out string fehler), "Lauf gescheitert: " + fehler);
            return new Lauf
            {
                Runner = r,
                Ergebnis = SimulationRunner.BaueErgebnis(idProjekt, r.simulation_Waermebedarf,
                                                          r.simulation_Strombedarf, r.sim),
                Reihen = ZeitreihenExtraktor.AusLauf(r)
            };
        }

        /// <summary>Ein Klimajahr ohne Datenbank: 8 760 Zeilen mit UTC-Herkunft, Stunde für Stunde.</summary>
        private static SolardatenCtrl Klimajahr()
        {
            var k = new SolardatenCtrl();
            for (int h = 0; h < STUNDEN; h++)
                k.items.Add(new SolardatenModel { TagUtc = h / 24 + 1, StundeUtc = h % 24 });
            return k;
        }
    }
}
