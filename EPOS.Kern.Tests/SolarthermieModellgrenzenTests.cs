using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Welle M2 Solarthermie am Referenzprojekt 1049</b> (Entscheidungsvorlage Modellgrenzen
    /// ST1 bis ST6), je auf einer Arbeitskopie der Testdatenbank: Bezugsfläche (ST6) und Diffus-IAM
    /// (ST5) über das Potenzial der Felder, Arbeitstemperatur aus dem Speicher (ST2 mit ST4) und
    /// Pumpenstrom (ST1) über den ganzen Lauf.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SolarthermieModellgrenzenTests : IDisposable
    {
        private const int PROJEKT = 1049;

        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>ID der Anlagenzeile des Kollektorfelds von 1049.</summary>
        private static int FeldAnlage()
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Solar IS NOT NULL ORDER BY ID",
                new DbParam("@p", PROJEKT)));

        /// <summary>ID der Projektkopie des Kollektorsatzes von 1049.</summary>
        private static int Kollektorsatz()
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_Solar FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@id", FeldAnlage())));

        /// <summary>
        /// Das Feld von 1049 vorbereitet und einmal ohne Bedarf durch das Jahr geführt — das Potenzial
        /// rechnet je Stunde (ST2); mit fester Arbeitstemperatur hängt es nicht vom Bedarf ab.
        /// </summary>
        private static SimulationSolarthermie Vorbereitet()
        {
            SimulationProtokoll.NeuStarten();
            var st = new SimulationSolarthermie();
            Assert.True(st.Vorbereiten_Zweikanalig(PROJEKT, null));
            Assert.Equal(1, st.FelderAnzahl);

            double[] rest = new double[Kanal.ANZAHL];
            for (int h = 0; h < 8760; h++)
            {
                Array.Clear(rest, 0, rest.Length);
                st.Stunde_Start(h, rest);
                st.Stunde_Bedarf(h, rest);
                st.Stunde_Ende(h);
            }
            st.Abschluss_Zweikanalig();
            return st;
        }

        /// <summary>Ein ganzer Lauf des Projekts 1049 auf der Arbeitskopie.</summary>
        private static SimulationRunner Lauf()
        {
            var l = new SimulationRunner();
            string fehler;
            Assert.True(l.Simuliere(PROJEKT, out fehler), "Lauf gescheitert: " + fehler);
            return l;
        }

        private static void FeldSetzen(string spalte, object wert)
            => DataRepository.ExecuteNonQuery("UPDATE Tab_Energieanlagen SET \"" + spalte + "\" = ? WHERE ID = ?",
                                              new DbParam("@w", wert ?? DBNull.Value), new DbParam("@id", FeldAnlage()));

        // =================================================================
        // ST2 mit ST4 - Arbeitstemperatur aus dem Speicher
        // =================================================================

        /// <summary>
        /// „fest" ausdrücklich gesetzt rechnet Bit für Bit wie das leere Feld (die Vorgabe vor der
        /// Welle); „speicher" bildet die Arbeitstemperatur aus der untersten Zone des Puffers
        /// (35/60 °C, eine Zone) plus 5 K Grädigkeit und 5 K halber Spreizung — der Ertrag ändert sich,
        /// die mittlere Arbeitstemperatur liegt zwischen 45 und 70 °C, und die Wärme der Kaskade geht
        /// weiter auf (Solar plus BHKW plus Kessel deckt den Bedarf).
        /// </summary>
        [Fact]
        public void Fest_ist_byte_gleich_und_Speicher_rechnet_aus_der_untersten_Zone()
        {
            if (!_db.Vorhanden) return;

            SimulationRunner leer = Lauf();
            SimulationSolarthermie a = leer.sim.simulation_solarthermie;
            Assert.Equal(50.0, a.Kollektor_Ergebnisse[0].ArbeitstemperaturMittelC, 9);

            FeldSetzen(SolarthermieFelderSchema.SPALTE_ARBEITSTEMPERATUR, DbWerte.SOLAR_ARBEITSTEMPERATUR_FEST);
            SimulationSolarthermie b = Lauf().sim.simulation_solarthermie;
            for (int h = 0; h < 8760; h++)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(a.Waermeproduktion[h]), BitConverter.DoubleToInt64Bits(b.Waermeproduktion[h]));
                Assert.Equal(BitConverter.DoubleToInt64Bits(a.Ueberschuss[h]), BitConverter.DoubleToInt64Bits(b.Ueberschuss[h]));
            }

            FeldSetzen(SolarthermieFelderSchema.SPALTE_ARBEITSTEMPERATUR, DbWerte.SOLAR_ARBEITSTEMPERATUR_SPEICHER);
            SimulationRunner speicherLauf = Lauf();
            SimulationSolarthermie c = speicherLauf.sim.simulation_solarthermie;
            Assert.Contains(speicherLauf.Protokoll.Hinweise,
                            t => t.Contains("bildet seine Arbeitstemperatur aus der untersten Zone des Puffers"));

            double bruttoFest = a.WaermeproduktionGesamtKwh + a.UeberschussSummeKwh;
            double bruttoSpeicher = c.WaermeproduktionGesamtKwh + c.UeberschussSummeKwh;
            Assert.NotEqual(bruttoFest, bruttoSpeicher);
            Assert.InRange(bruttoSpeicher / bruttoFest, 0.7, 1.3);
            Assert.InRange(c.Kollektor_Ergebnisse[0].ArbeitstemperaturMittelC, 45.0, 70.0);
        }

        // =================================================================
        // ST1 - Pumpenstrom im Lauf
        // =================================================================

        /// <summary>
        /// 80 W Pumpe: Strom nur in Stunden mit Abgabe, als Verbraucher im Strombedarf des Anschlusses
        /// (Restbedarf um genau die Pumpenreihe höher), die Wärmerechnung bleibt unberührt.
        /// </summary>
        [Fact]
        public void Pumpenstrom_geht_in_den_Strombedarf()
        {
            if (!_db.Vorhanden) return;

            SimulationRunner ohne = Lauf();
            FeldSetzen(SolarthermieFelderSchema.SPALTE_PUMPENLEISTUNG, 80.0);
            SimulationRunner mit = Lauf();

            SimulationSolarthermie st = mit.sim.simulation_solarthermie;
            int betrieb = 0;
            for (int h = 0; h < 8760; h++)
            {
                bool abgabe = st.Waermeproduktion[h] > 0;
                Assert.Equal(abgabe ? 0.08 : 0.0, st.Pumpenstrom_stuendlich[h], 12);
                if (abgabe) betrieb++;
            }
            Assert.True(betrieb > 500, "Betriebsstunden " + betrieb);
            Assert.Equal(betrieb * 0.08, st.PumpenstromGesamtKwh, 6);
            Assert.Equal(ohne.sim.simulation_solarthermie.WaermeproduktionGesamtKwh, st.WaermeproduktionGesamtKwh);

            double vorher = ohne.sim.Strombedarf_Verbraucher_viertelstuendlich.Sum() / 4.0;
            double nachher = mit.sim.Strombedarf_Verbraucher_viertelstuendlich.Sum() / 4.0;
            Assert.Equal(st.PumpenstromGesamtKwh, nachher - vorher, 3);

            var e = SimulationErgebnisCtrl.Solarthermie(mit.sim, mit.simulation_Waermebedarf);
            Assert.Equal(st.PumpenstromGesamtKwh / 1000.0, e.PumpenstromMwh, 12);
        }

        // =================================================================
        // ST6 - Bezugsfläche
        // =================================================================

        /// <summary>
        /// Brutto rechnet mit der Modulfläche: Das Potenzial wächst um Brutto/Apertur — umgekehrt liegt
        /// eine Rechnung mit der Apertur um Apertur/Brutto unter der mit Brutto bezogenen Kennwerten.
        /// Ohne Modulfläche bleibt die Apertur, und der Lauf sagt es.
        /// </summary>
        [Fact]
        public void Bezugsflaeche_Brutto_skaliert_das_Potenzial_mit_Brutto_zu_Apertur()
        {
            if (!_db.Vorhanden) return;

            SimulationSolarthermie apertur = Vorbereitet();
            double sumApertur = apertur.PotenzialSumme(0);
            Assert.Equal(2.35 * 35, apertur.FeldFlaeche(0), 9);
            Assert.True(sumApertur > 0);

            int satz = Kollektorsatz();
            DataRepository.ExecuteNonQuery("UPDATE Tab_Solarkollektoren SET Bezugsflaeche = 'brutto' WHERE ID = ?",
                                           new DbParam("@id", satz));

            SimulationSolarthermie ohneBrutto = Vorbereitet();
            Assert.Equal(sumApertur, ohneBrutto.PotenzialSumme(0));
            Assert.Contains(SimulationProtokoll.Aktuell.Warnungen,
                            t => t.Contains("Bruttofläche bezogen") && t.Contains("Aperturfläche"));

            DataRepository.ExecuteNonQuery("UPDATE Tab_Solarkollektoren SET Modulflaeche = 2.51 WHERE ID = ?",
                                           new DbParam("@id", satz));
            SimulationSolarthermie brutto = Vorbereitet();
            Assert.Equal(2.51 * 35, brutto.FeldFlaeche(0), 9);
            Assert.Equal(2.35 / 2.51, sumApertur / brutto.PotenzialSumme(0), 12);
        }

        // =================================================================
        // ST5 - Diffus-IAM
        // =================================================================

        /// <summary>
        /// 1049 führt kein K_dfu (0): Das Potenzial bleibt das der Korrektur der Gesamtstrahlung mit
        /// K_b(θ). Mit K_dfu = 0,9 ändert es sich — die Diffusstrahlung bekommt ihren eigenen Faktor.
        /// </summary>
        [Fact]
        public void Kdfu_wirkt_nur_wenn_gepflegt()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Kdfu FROM Tab_Solarkollektoren WHERE ID = ?", new DbParam("@id", Kollektorsatz()))));
            double ohne = Vorbereitet().PotenzialSumme(0);

            DataRepository.ExecuteNonQuery("UPDATE Tab_Solarkollektoren SET Kdfu = 0.9 WHERE ID = ?",
                                           new DbParam("@id", Kollektorsatz()));
            double mit = Vorbereitet().PotenzialSumme(0);

            Assert.NotEqual(ohne, mit);
            Assert.InRange(mit / ohne, 0.9, 1.1);
        }

        /// <summary>Die Projektkopie trägt die Bezugsfläche des Katalogsatzes.</summary>
        [Fact]
        public void Die_Projektkopie_uebernimmt_die_Bezugsflaeche()
        {
            if (!_db.Vorhanden) return;

            int stamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Solarkollektoren_STAMM WHERE Bezeichner = ?",
                new DbParam("@b", "auroTHERM plus VFK 155/2 H")));
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Solarkollektoren_STAMM SET Bezugsflaeche = 'brutto', Modulflaeche = 2.51 WHERE ID = ?",
                new DbParam("@id", stamm));

            int kopie = new SolarkollektorenCtrl().CopyFromStamm(stamm, PROJEKT);
            Assert.True(kopie > 0);
            Assert.Equal("brutto", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezugsflaeche FROM Tab_Solarkollektoren WHERE ID = ?", new DbParam("@id", kopie))));

            var gelesen = new SolarkollektorenCtrl();
            gelesen.ReadSingle(kopie);
            Assert.Equal("brutto", gelesen.m_Bezugsflaeche);

            SolarkollektorenModel m = SolarkollektorenStammCtrl.ReadById(stamm);
            Assert.Equal("brutto", m.m_Bezugsflaeche);
        }
    }
}
