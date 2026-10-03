using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schemaschritt des Sperrprofils der Wärmepumpe</b> (Welle V14): Nummer und Register, die
    /// Prüfklauseln von <c>Tab_Sperrfenster</c> und der Schritt aus dem Stand vor ihm — zweimal.
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermepumpeSperrprofilSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Nummer_Ziel_und_Register()
        {
            Assert.Equal(177, WaermepumpeSperrprofilSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= WaermepumpeSperrprofilSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == WaermepumpeSperrprofilSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
        }

        [Fact]
        public void Die_Pruefklauseln_der_Tabelle()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "PRAGMA foreign_keys = ON");
            Ausfuehren(c, "CREATE TABLE Tab_Energieanlagen (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, WaermepumpeSperrprofilSchema.SqlCreate());
            Ausfuehren(c, WaermepumpeSperrprofilSchema.SQL_INDEX);
            Ausfuehren(c, "INSERT INTO Tab_Energieanlagen (ID) VALUES (7)");

            Assert.False(Wirft(c, "INSERT INTO Tab_Sperrfenster (ID_Energieanlage, Von_h, Dauer_h) VALUES (7, 22, 4)"));
            Assert.Equal(127L, Zahl(c, "SELECT Wochentage FROM Tab_Sperrfenster"));
            Assert.Equal(1L, Zahl(c, "SELECT Heizstab_gesperrt FROM Tab_Sperrfenster"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Sperrfenster (ID_Energieanlage, Von_h, Dauer_h) VALUES (7, 25, 2)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Sperrfenster (ID_Energieanlage, Von_h, Dauer_h) VALUES (7, -1, 2)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Sperrfenster (ID_Energieanlage, Von_h, Dauer_h) VALUES (7, 1, 0)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Sperrfenster (ID_Energieanlage, Von_h, Dauer_h) VALUES (7, 1, 24.5)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Sperrfenster (ID_Energieanlage, Von_h, Dauer_h, Wochentage) VALUES (7, 1, 2, 0)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Sperrfenster (ID_Energieanlage, Von_h, Dauer_h, Wochentage) VALUES (7, 1, 2, 128)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Sperrfenster (ID_Energieanlage, Von_h, Dauer_h, Heizstab_gesperrt) VALUES (7, 1, 2, 2)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Sperrfenster (ID_Energieanlage, Von_h, Dauer_h) VALUES (99, 1, 2)"));

            // Die Anlage geht, ihre Fenster gehen mit.
            Ausfuehren(c, "DELETE FROM Tab_Energieanlagen WHERE ID = 7");
            Assert.Equal(0L, Zahl(c, "SELECT COUNT(*) FROM Tab_Sperrfenster"));
        }

        /// <summary>Der Schritt aus dem Stand VOR ihm: Die Tabelle wird entfernt, der Schritt legt sie an, ein zweiter Lauf tut nichts.</summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            Assert.True(WaermepumpeSperrprofilSchema.Vollstaendig());
            DataRepository.ExecuteNonQuery("DROP TABLE \"" + WaermepumpeSperrprofilSchema.TAB + "\"");
            Assert.False(WaermepumpeSperrprofilSchema.Vollstaendig());
            Assert.Equal(2, WaermepumpeSperrprofilSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(2, WaermepumpeSperrprofilSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Sperrfenster anlegen"));
            Assert.True(WaermepumpeSperrprofilSchema.Vollstaendig());
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Sperrfenster"),
                                             CultureInfo.InvariantCulture));
            string sql = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@n", "Tab_Sperrfenster")));
            Assert.Contains("STRICT", sql);

            var zweiter = new List<string>();
            Assert.Equal(0, WaermepumpeSperrprofilSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        private static long Zahl(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try { Ausfuehren(c, sql); return false; }
            catch (SqliteException) { return true; }
        }
    }
}
