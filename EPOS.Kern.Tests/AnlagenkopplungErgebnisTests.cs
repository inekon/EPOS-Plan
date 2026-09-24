using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Ergebnis der Anlagenkopplung je Gebäude</b> (AK1 Welle 3; Konzept Anlagenkopplung
    /// 8.3, 9.4, 12.1; Muster E30) — auf einer Arbeitskopie der Testdatenbank, Projekt 1045 (ein
    /// Gebäude auf dem VDI-Weg): Der gekoppelte Lauf legt den Heizkreis je Gebäude nach
    /// <c>Tab_ErgebnisGebaeude</c> (Schritt 125), <c>Load</c> liest dieselben Zahlen, und ein
    /// Lauf ohne Kopplung schreibt dort NULL.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AnlagenkopplungErgebnisTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1045;
        private const int GEBAEUDE = 10651;       // das Gebäude von 1045 (VDI-Weg)

        private static void Koppeln(int idGebaeude, string art = DbWerte.UEBERGABE_RADIATOR, bool heizkurve = true)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Heizkreis_Aktiv = 1, Uebergabe_Art = ?, Heizkurve_Aktiv = ? WHERE ID = ?",
                new DbParam("@art", art), new DbParam("@kurve", heizkurve ? 1 : 0), new DbParam("@id", idGebaeude)));
        }

        private static SimulationRunner Rechne(int idProjekt)
        {
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(idProjekt, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            return lauf;
        }

        private static DataRow Gebaeudezeile(int idProjekt, int idGebaeude)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT g.* FROM Tab_ErgebnisGebaeude g INNER JOIN Tab_Ergebnis e ON e.ID = g.ID_Ergebnis " +
                "WHERE e.ID_Projekt = ? AND g.ID_Gebaeude = ?",
                new DbParam("@p", idProjekt), new DbParam("@g", idGebaeude));
            Assert.NotNull(dt);
            return Assert.Single(dt.Rows.Cast<DataRow>());
        }

        // =====================================================================
        //  Schritt 125: der Heizkreis je Gebäude in Tab_ErgebnisGebaeude
        // =====================================================================

        /// <summary>
        /// <b>Ohne Kopplung NULL, mit Kopplung die Zahlen des Laufs</b> — ungerundet wie jede
        /// Kennzahl der Tabelle (E30); <c>Load</c> liest dieselben Zahlen, und bei einem einzigen
        /// gekoppelten Gebäude sind es die Zahlen der Projektzeile (Schritt 123).
        /// </summary>
        [Fact]
        public void Der_Lauf_legt_den_Heizkreis_je_Gebaeude_ab_und_Load_liest_ihn()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ErgebnisGebaeudeSchema.HeizkreisVollstaendig(), "Die Testdatenbank steht nicht auf Schritt 125.");

            Rechne(PROJEKT);
            DataRow ohne = Gebaeudezeile(PROJEKT, GEBAEUDE);
            foreach (KeyValuePair<string, string> s in ErgebnisGebaeudeSchema.SpaltenHeizkreis)
                Assert.Equal(DBNull.Value, ohne[s.Key]);
            Assert.All(new ErgebnisCtrl().Load(PROJEKT).Gebaeude, g => Assert.False(g.IstGekoppelt));

            Koppeln(GEBAEUDE);
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            SimulationRunner lauf = Rechne(PROJEKT);

            HeizkreisErgebnis hk = lauf.simulation_Waermebedarf.GebaeudeErgebnisse.Ergebnis(0)?.Heizkreis;
            Assert.NotNull(hk);
            DataRow mit = Gebaeudezeile(PROJEKT, GEBAEUDE);
            Assert.Equal(DbWerte.UEBERGABE_RADIATOR, (string)mit[ErgebnisGebaeudeSchema.SPALTE_UEBERGABE_ART]);
            Assert.Equal(hk.VorlaufMittelC, (double)mit[ErgebnisGebaeudeSchema.SPALTE_VORLAUF_MITTEL]);
            Assert.Equal(hk.RuecklaufMittelC, (double)mit[ErgebnisGebaeudeSchema.SPALTE_RUECKLAUF_MITTEL]);
            Assert.Equal(hk.UebergabeBegrenztStundenH, (double)mit[ErgebnisGebaeudeSchema.SPALTE_UEBERGABE_BEGRENZT]);
            Assert.True(hk.VorlaufMittelC > hk.RuecklaufMittelC);

            // Ein gekoppeltes Gebäude: seine Zahlen sind die der Projektzeile.
            HeizkreisProjekt projekt = lauf.simulation_Waermebedarf.Heizkreis;
            Assert.Equal(projekt.VorlaufMittelC, hk.VorlaufMittelC);
            Assert.Equal(projekt.UebergabeBegrenztStundenH, hk.UebergabeBegrenztStundenH);

            ErgebnisGebaeudeModel gelesen = Assert.Single(new ErgebnisCtrl().Load(PROJEKT).Gebaeude);
            Assert.True(gelesen.IstGekoppelt);
            Assert.Equal(DbWerte.UEBERGABE_RADIATOR, gelesen.UebergabeArt);
            Assert.Equal(hk.VorlaufMittelC, gelesen.VorlaufMittelC);
            Assert.Equal(hk.RuecklaufMittelC, gelesen.RuecklaufMittelC);
            Assert.Equal(hk.UebergabeBegrenztStundenH, gelesen.UebergabeBegrenztStundenH);
        }

        /// <summary>
        /// <b>Vor Schritt 125</b> (die vier Spalten fehlen) schreibt der Lauf die Zeile des
        /// Schritts 107 — gespeichert wird trotzdem, und <c>Load</c> liest „nicht gekoppelt".
        /// </summary>
        [Fact]
        public void Vor_Schritt_125_speichert_der_Lauf_ohne_die_Spalten_des_Heizkreises()
        {
            if (!_db.Vorhanden) return;
            foreach (KeyValuePair<string, string> s in ErgebnisGebaeudeSchema.SpaltenHeizkreis)
                DataRepository.ExecuteNonQuery("ALTER TABLE " + ErgebnisGebaeudeSchema.TAB + " DROP COLUMN \"" + s.Key + "\"");
            Assert.False(ErgebnisGebaeudeSchema.HeizkreisVollstaendig());

            Koppeln(GEBAEUDE);
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            SimulationRunner lauf = Rechne(PROJEKT);
            Assert.NotNull(lauf.simulation_Waermebedarf.Heizkreis);

            DataRow zeile = Gebaeudezeile(PROJEKT, GEBAEUDE);
            Assert.False(zeile.Table.Columns.Contains(ErgebnisGebaeudeSchema.SPALTE_UEBERGABE_ART));
            Assert.False(Assert.Single(new ErgebnisCtrl().Load(PROJEKT).Gebaeude).IstGekoppelt);
        }
    }
}
