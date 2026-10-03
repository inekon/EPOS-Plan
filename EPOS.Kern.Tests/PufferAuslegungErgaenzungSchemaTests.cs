using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schemaschritt der Ergänzungen der Pufferspeicher-Auslegung</b> (Welle P4c): Nummer und
    /// Register, Prüfklauseln und Fremdschlüssel der sieben Spalten und der Schritt aus dem Stand vor
    /// ihm — zweimal. Kein Referenzgebäude und kein Referenzpuffer trägt einen Wert.
    /// </summary>
    [Collection("Testdatenbank")]
    public class PufferAuslegungErgaenzungSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Nummer_Ziel_und_Register()
        {
            Assert.True(PufferAuslegungErgaenzungSchema.SCHRITT > ProjektkopienKatalogeSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= PufferAuslegungErgaenzungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl,
                         Paketanhebung.Stufen.Single(x => x.Nr == PufferAuslegungErgaenzungSchema.SCHRITT).Wirkung);
            Assert.Equal(7, PufferAuslegungErgaenzungSchema.SPALTEN.Count);
            Assert.Equal(511, PufferAuslegungErgaenzungSchema.MASKE_MAX);
        }

        [Fact]
        public void Pruefklauseln_und_Fremdschluessel()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "PRAGMA foreign_keys = ON");
            Ausfuehren(c, "CREATE TABLE Tab_Projekt (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE Tab_Pufferspeicher (ID INTEGER PRIMARY KEY, ID_Projekt INTEGER) STRICT");
            Ausfuehren(c, "CREATE TABLE Tab_Pufferspeicher_STAMM (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE Tab_Gebaeude (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE Tab_Konditionierungsvorlage_STAMM (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, PufferAuslegungSchema.SqlCreateAuslegung());
            foreach (var s in PufferAuslegungErgaenzungSchema.SPALTEN)
                Ausfuehren(c, PufferAuslegungErgaenzungSchema.Anlegen(s));
            Ausfuehren(c, "INSERT INTO Tab_Projekt (ID) VALUES (1)");

            Assert.False(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Kriterien_Aktiv, Sperrzeit_Expertenweg, " +
                                  "Auslegungsheizlast_kW, Wohneinheiten, Anzeigestufe) VALUES (1, 511, 1, 12.5, 8, 'EXPERTE')"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Kriterien_Aktiv) VALUES (1, 512)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Kriterien_Aktiv) VALUES (1, -1)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Sperrzeit_Expertenweg) VALUES (1, 2)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Auslegungsheizlast_kW) VALUES (1, -0.1)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Wohneinheiten) VALUES (1, -1)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Anzeigestufe) VALUES (1, 'PROFI')"));

            // Der Katalogsatz geht, der Verweis wird leer; ein fremder Verweis wird abgelehnt.
            Ausfuehren(c, "INSERT INTO Tab_Pufferspeicher_STAMM (ID) VALUES (7)");
            Ausfuehren(c, "INSERT INTO Tab_Konditionierungsvorlage_STAMM (ID) VALUES (3)");
            Ausfuehren(c, "INSERT INTO Tab_Pufferspeicher (ID, ID_Projekt, ID_Stamm) VALUES (5, 1, 7)");
            Ausfuehren(c, "INSERT INTO Tab_Gebaeude (ID, ID_Konditionierungsvorlage) VALUES (9, 3)");
            Assert.True(Wirft(c, "INSERT INTO Tab_Pufferspeicher (ID, ID_Projekt, ID_Stamm) VALUES (6, 1, 99)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_Gebaeude (ID, ID_Konditionierungsvorlage) VALUES (10, 99)"));
            Ausfuehren(c, "DELETE FROM Tab_Pufferspeicher_STAMM");
            Ausfuehren(c, "DELETE FROM Tab_Konditionierungsvorlage_STAMM");
            Assert.Equal(1L, Zahl(c, "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Stamm IS NULL"));
            Assert.Equal(1L, Zahl(c, "SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Konditionierungsvorlage IS NULL"));
        }

        /// <summary>
        /// Der Schritt aus dem Stand VOR ihm: Die sieben Spalten werden entfernt; der Schritt legt sie leer
        /// an, ein zweiter Lauf tut nichts. Kein Referenzgebäude und kein Referenzpuffer trägt einen Wert.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            Assert.True(PufferAuslegungErgaenzungSchema.Vollstaendig());
            foreach (var s in PufferAuslegungErgaenzungSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
            Assert.False(PufferAuslegungErgaenzungSchema.Vollstaendig());
            Assert.Equal(7, PufferAuslegungErgaenzungSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(7, PufferAuslegungErgaenzungSchema.Ausfuehren(bericht));
            Assert.Equal(7, bericht.Count);
            Assert.True(PufferAuslegungErgaenzungSchema.Vollstaendig());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Konditionierungsvorlage IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Stamm IS NOT NULL"));

            var zweiter = new List<string>();
            Assert.Equal(0, PufferAuslegungErgaenzungSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("bereits"));
        }

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql));

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            k.ExecuteNonQuery();
        }

        private static long Zahl(SqliteConnection c, string sql)
        {
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            return Convert.ToInt64(k.ExecuteScalar());
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try { Ausfuehren(c, sql); return false; }
            catch (SqliteException) { return true; }
        }
    }
}
