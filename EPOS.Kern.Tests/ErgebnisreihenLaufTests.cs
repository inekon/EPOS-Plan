using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// EB-1: Der Lauf schreibt die drei Ergebnisreihen als Jahressummen in den Ergebnisbaum — die Einspeisung des BHKW
    /// (<c>Tab_ErgebnisBHKW.Stromeinspeisung</c>), den Standby des Speichersystems
    /// (<c>Tab_ErgebnisStromspeicher.Eigenverbrauch_System</c>) und die PV-Abregelung
    /// (<c>Tab_ErgebnisPhotovoltaik.Abregelung</c>) —, ohne den Rechenweg zu ändern: Jede Zahl ist dieselbe wie die der
    /// Laufreihe und des Reiters. Schreib- und Leseweg, Kennzahl und der Rückfall des Energieflussbilds bei einem
    /// Altergebnis ohne Wert.
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErgebnisreihenLaufTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

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
            Assert.True(r.Simuliere(idProjekt, out string fehler), "Lauf " + idProjekt + " gescheitert: " + fehler);
            return new Lauf
            {
                Runner = r,
                Ergebnis = SimulationRunner.BaueErgebnis(idProjekt, r.simulation_Waermebedarf, r.simulation_Strombedarf, r.sim),
                Reihen = ZeitreihenExtraktor.AusLauf(r)
            };
        }

        private static double Summe(double[] reihe) => reihe == null ? 0.0 : reihe.Sum();

        /// <summary>1018 (BHKW, Kessel, Puffer): die Einspeisung der Ergebniszeile ist die Reihe BHKW_UEBERSCHUSS des Laufs.</summary>
        [Fact]
        public void Projekt_1018_schreibt_die_BHKW_Einspeisung_als_Jahressumme()
        {
            if (!_db.Vorhanden) return;
            Lauf l = Rechne(1018);
            ErgebnisBHKWModel b = l.Ergebnis.BHKW;
            Assert.NotNull(b);
            Assert.True(b.StromeinspeisungMwh.HasValue);
            Assert.True(b.StromeinspeisungMwh.Value > 1.0, "Einspeisung " + b.StromeinspeisungMwh);
            Assert.Equal(SimulationErgebnisCtrl.BhkwEinspeisungMwh(l.Sim), b.StromeinspeisungMwh.Value, 9);
            Assert.Equal(Summe(l.Reihen.Hole(ZeitreihenSatz.BHKW_UEBERSCHUSS)) / 1000.0, b.StromeinspeisungMwh.Value, 6);
            Assert.True(b.StromeinspeisungMwh.Value <= b.Stromproduktion + 1e-9, "Einspeisung über der Erzeugung");
            // Kein Rechenweg ändert sich: Erzeugung und Deckung sind die bisherigen Zahlen der Zeile.
            Assert.Equal(l.Sim.simulation_bhkw.Stromproduktion_BHKW_MWh, b.Stromproduktion, 9);
        }

        /// <summary>1046 (Flotte mit Standby): der Standby der Speicherzeile ist der Eigenverbrauch des Laufs und die Reihe.</summary>
        [Fact]
        public void Projekt_1046_schreibt_den_Standby_des_Speichersystems_und_die_Abregelung_null()
        {
            if (!_db.Vorhanden) return;
            Lauf l = Rechne(1046);
            ErgebnisStromspeicherModel sp = Assert.Single(l.Ergebnis.Stromspeicher);
            Assert.True(sp.EigenverbrauchSystemKwh.HasValue);
            Assert.True(sp.EigenverbrauchSystemKwh.Value > 100.0, "Standby " + sp.EigenverbrauchSystemKwh);
            Assert.Equal(l.Sim.SpeichersystemEigenverbrauchKwh, sp.EigenverbrauchSystemKwh.Value, 9);
            Assert.Equal(Summe(l.Reihen.Hole(ZeitreihenSatz.SPEICHER_EIGENVERBRAUCH)), sp.EigenverbrauchSystemKwh.Value, 3);
            // Photovoltaik ohne Einspeisegrenze: die Abregelung ist erhoben und 0 (nicht NULL).
            Assert.NotNull(l.Ergebnis.Photovoltaik);
            Assert.Equal(0.0, l.Ergebnis.Photovoltaik.AbregelungMwh);
            Assert.Equal(0.0, Summe(l.Reihen.Hole(ZeitreihenSatz.PV_ABREGELUNG)));   // Flottenpfad: Reihe stets da, hier 0
        }

        /// <summary>
        /// Synthetisch (kein Referenzprojekt trägt eine Einspeisegrenze): 1040 mit 1 kW Grenze — die Abregelung der
        /// Ergebniszeile ist die des Reiters und der Reihe PV_ABREGELUNG; Erzeugung und Überschuss bleiben die bisherigen
        /// Zahlen (Rechenweg unverändert, Wache in StromViertelstundenTests).
        /// </summary>
        [Fact]
        public void Projekt_1040_mit_Einspeisegrenze_schreibt_die_Abregelung_als_Jahressumme()
        {
            if (!_db.Vorhanden) return;
            Lauf vorher = Rechne(1040);
            Assert.Equal(0.0, vorher.Ergebnis.Photovoltaik.AbregelungMwh);
            Assert.True(KonfigurationCtrl.EinspeisegrenzeSetzen(1040, new Einspeisegrenze(1.0, DbWerte.EINSPEISEGRENZE_KW)));
            Lauf l = Rechne(1040);
            ErgebnisPhotovoltaikModel p = l.Ergebnis.Photovoltaik;
            Assert.True(p.AbregelungMwh.HasValue);
            Assert.True(p.AbregelungMwh.Value > 0.1, "Abregelung " + p.AbregelungMwh);
            Assert.Equal(l.Sim.simulation_pv.AbregelungGesamtKwh / 1000.0, p.AbregelungMwh.Value, 9);
            Assert.Equal(SimulationErgebnisCtrl.Photovoltaik(l.Sim).AbregelungMwh, p.AbregelungMwh.Value, 9);
            Assert.Equal(SimulationErgebnisCtrl.PvAbregelungMwh(l.Sim), p.AbregelungMwh.Value, 9);
            Assert.Equal(Summe(l.Reihen.Hole(ZeitreihenSatz.PV_ABREGELUNG)) / 1000.0, p.AbregelungMwh.Value, 6);
            Assert.Equal(vorher.Ergebnis.Photovoltaik.Stromproduktion, p.Stromproduktion, 9);
            Assert.Equal(vorher.Ergebnis.Photovoltaik.Ueberschuss - p.AbregelungMwh.Value, p.Ueberschuss, 9);
        }

        /// <summary>
        /// Speichern und Lesen: die drei Spalten stehen kaufmännisch gerundet in der Datenbank, der Leseweg liefert sie
        /// nullbar; die Kennzahl „PV-Eigenverbrauchsquote“ nimmt die gespeicherte Abregelung auch ohne Zeitreihen.
        /// </summary>
        [Fact]
        public void Speichern_und_Lesen_der_drei_Spalten_und_die_Kennzahl_liest_die_gespeicherte_Abregelung()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KonfigurationCtrl.EinspeisegrenzeSetzen(1040, new Einspeisegrenze(1.0, DbWerte.EINSPEISEGRENZE_KW)));
            foreach (int projekt in new[] { 1018, 1046, 1040 })
            {
                int kopf = new SimulationRunner().SimuliereUndSpeichere(projekt, out string fehler);
                Assert.True(kopf > 0, "Lauf " + projekt + " gescheitert: " + fehler);
            }

            VariantenDaten v1018 = EnergievergleichCtrl.Lade(1018, true);
            Assert.True(v1018.Ergebnis.BHKW.StromeinspeisungMwh.HasValue);
            Assert.Equal(Math.Round(v1018.Ergebnis.BHKW.StromeinspeisungMwh.Value, 2, MidpointRounding.AwayFromZero),
                         v1018.Ergebnis.BHKW.StromeinspeisungMwh.Value, 9);
            Assert.True(v1018.Ergebnis.BHKW.StromeinspeisungMwh.Value > 1.0);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"Tab_ErgebnisBHKW\" WHERE \"ID_Ergebnis\" = ? AND \"Stromeinspeisung\" IS NOT NULL",
                                  new DbParam("@e", v1018.Ergebnis.ID)));

            VariantenDaten v1046 = EnergievergleichCtrl.Lade(1046, true);
            ErgebnisStromspeicherModel sp = Assert.Single(v1046.Ergebnis.Stromspeicher);
            Assert.True(sp.EigenverbrauchSystemKwh.HasValue && sp.EigenverbrauchSystemKwh.Value > 100.0);
            Assert.Equal(0.0, v1046.Ergebnis.Photovoltaik.AbregelungMwh);

            VariantenDaten v1040 = EnergievergleichCtrl.Lade(1040, true);
            ErgebnisPhotovoltaikModel p = v1040.Ergebnis.Photovoltaik;
            Assert.True(p.AbregelungMwh.HasValue && p.AbregelungMwh.Value > 0.1);
            Kennzahl quote = KennzahlenKatalog.Alle().Single(k => k.Schluessel == "eff.pv_eigen");
            v1040.Zeitreihen = null;                                   // ohne Reihen: die gespeicherte Jahressumme trägt
            double? wert = quote.Wert(v1040);
            Assert.True(wert.HasValue);
            Assert.Equal((p.Stromproduktion - p.Ueberschuss - p.AbregelungMwh.Value) / p.Stromproduktion * 100.0, wert.Value, 9);
        }

        /// <summary>
        /// Altergebnis (Ergebnis vor Schritt 239): mit NULL in den drei Spalten bilanziert das Energieflussbild die
        /// BHKW-Einspeisung wie bisher am Stromknoten (Herkunft „bilanziert“); Standby und Abregelung fehlen dann.
        /// </summary>
        [Fact]
        public void Altergebnis_ohne_Werte_faellt_im_Energieflussbild_auf_die_Bilanzierung_zurueck()
        {
            if (!_db.Vorhanden) return;
            int kopf = new SimulationRunner().SimuliereUndSpeichere(1018, out string fehler);
            Assert.True(kopf > 0, "Lauf 1018 gescheitert: " + fehler);
            EnergieflussBilanz direkt = EnergieflussCtrl.Lade(1018, true, false);
            Assert.Equal(EnergieflussHerkunft.Direkt, direkt.FlussMit(Energiefluss.STROM, Energiefluss.EINSPEISUNG_BHKW).Herkunft);

            DataRepository.ExecuteNonQuery("UPDATE \"Tab_ErgebnisBHKW\" SET \"Stromeinspeisung\" = NULL WHERE \"ID_Ergebnis\" = ?",
                                           new DbParam("@e", kopf));
            VariantenDaten alt = EnergievergleichCtrl.Lade(1018, true);
            Assert.Null(alt.Ergebnis.BHKW.StromeinspeisungMwh);
            EnergieflussBilanz bilanziert = EnergieflussCtrl.Lade(1018, true, false);
            EnergieflussFluss e = bilanziert.FlussMit(Energiefluss.STROM, Energiefluss.EINSPEISUNG_BHKW);
            Assert.NotNull(e);
            Assert.Equal(EnergieflussHerkunft.Bilanziert, e.Herkunft);
            Assert.Equal(direkt.FlussMit(Energiefluss.STROM, Energiefluss.EINSPEISUNG_BHKW).Mwh, e.Mwh, 1);
            Assert.True(bilanziert.KnotenMit(Energiefluss.STROM).Geschlossen);
        }

        /// <summary>
        /// Energieflussbild aus einem synthetischen Ergebnisbaum (EB-Q1): Die PV-Quelle trägt das Potenzial (abgegeben +
        /// abgeregelt), der Abzweig „abgeregelt“ ist ein graues Verlustband; ab 1 % der Erzeugung ein Band im Bild, darunter
        /// nur im Zeigertext der Quelle (Kleinverlustregel). Der Stromknoten schließt ohne „nicht zugeordnet“.
        /// </summary>
        [Theory]
        [InlineData(3.0, true)]
        [InlineData(0.5, false)]
        public void Abregelung_ist_ein_grauer_Abzweig_an_der_PV_Quelle_nach_der_Kleinverlustregel(double abregelungMwh, bool bandImBild)
        {
            var e = new ErgebnisModel
            {
                Energiebedarf = new ErgebnisEnergiebedarfModel { Strombedarf_Gesamt = 80.0 - abregelungMwh, Stromrestbedarf = 0.0 },
                Photovoltaik = new ErgebnisPhotovoltaikModel { Stromproduktion = 100.0, Ueberschuss = 20.0, AbregelungMwh = abregelungMwh },
            };
            EnergieflussBilanz m = Energiefluss.Aus(e, null, false, "Probe");
            Assert.False(m.Leer);
            EnergieflussFluss ab = m.FlussMit(Energiefluss.PHOTOVOLTAIK, Energiefluss.ABREGELUNG_PV);
            Assert.NotNull(ab);
            Assert.Equal(abregelungMwh, ab.Mwh, 9);
            Assert.Equal(EnergieflussForm.Verlust, ab.Form);
            Assert.Equal(EnergieflussHerkunft.Direkt, ab.Herkunft);
            Assert.Equal("abgeregelt", m.KnotenMit(Energiefluss.ABREGELUNG_PV).Name);
            Assert.Equal(EnergieflussKnotenart.Verlust, m.KnotenMit(Energiefluss.ABREGELUNG_PV).Art);
            Assert.Equal(100.0 - abregelungMwh, m.Fluss(Energiefluss.PHOTOVOLTAIK, Energiefluss.STROM), 9);
            Assert.Equal(100.0, m.KnotenMit(Energiefluss.PHOTOVOLTAIK).AbflussMwh, 9);
            Assert.True(m.KnotenMit(Energiefluss.STROM).Geschlossen);
            Assert.Null(m.KnotenMit(Energiefluss.NICHT_ZUGEORDNET));
            Assert.Empty(m.Hinweise);

            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(m);
            bool band = d.Baender.Any(b => b.Nach == Energiefluss.ABREGELUNG_PV);
            Assert.Equal(bandImBild, band);
            EnergieflussBildKnoten pv = d.Knoten.Single(k => k.Kennung == Energiefluss.PHOTOVOLTAIK);
            if (bandImBild)
                Assert.Equal(EnergieflussBandart.Verlust, d.Baender.Single(b => b.Nach == Energiefluss.ABREGELUNG_PV).Art);
            else
                Assert.Contains("abgeregelt", pv.Zeigertext ?? "", StringComparison.Ordinal);
            // Ohne Abregelung (0 oder nicht erhoben) gibt es weder Knoten noch Band - das Bild der Altergebnisse bleibt.
            e.Photovoltaik.AbregelungMwh = null;
            e.Energiebedarf.Strombedarf_Gesamt = 80.0;
            Assert.Null(Energiefluss.Aus(e, null, false, "Probe").KnotenMit(Energiefluss.ABREGELUNG_PV));
        }

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);
    }
}
