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
    /// <b>Schemaschritt der Pufferspeicher-Auslegung</b> (Konzept Pufferspeicher-Auslegung, Stufe P1,
    /// Welle W1, Abschnitt 4.1): Nummer und Register, Prüfklauseln der Auslegungstabelle, die Saat
    /// aus der EINEN Vorgabeliste und der Schritt aus dem Stand vor ihm — zweimal.
    /// </summary>
    [Collection("Testdatenbank")]
    public class PufferAuslegungSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die Zahl der Saatzeilen: 63 Grundwerte (davon 6 des Aufheizkriteriums) und je Vorlage 9 Schalter und 4 Beispielwerte.</summary>
        private const int SAATZAHL = 63 + 7 * 13;

        [Fact]
        public void Nummer_Ziel_und_Register()
        {
            Assert.Equal(StromViertelstundenSchema.SCHRITT + 1, PufferAuslegungSchema.SCHRITT);
            // Das Ziel steht mindestens auf diesem Schritt; ein spaeterer Schritt (170, Katalogempfehlung der
            // Hilfsenergie) hebt es weiter, wie bei den uebrigen Schemaschritten.
            Assert.True(SchemaStand.Zielversion >= PufferAuslegungSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + PufferAuslegungSchema.SCHRITT + ".");
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == PufferAuslegungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
        }

        [Fact]
        public void Die_Saatliste_ist_eindeutig_und_vollstaendig()
        {
            Assert.Equal(SAATZAHL, PufferAuslegungVorgaben.Anzahl);
            Assert.Equal(PufferAuslegungVorgaben.Anzahl,
                         PufferAuslegungVorgaben.EINTRAEGE.Select(v => v.Schluessel).Distinct(StringComparer.Ordinal).Count());
            Assert.All(PufferAuslegungVorgaben.EINTRAEGE, v =>
            {
                Assert.StartsWith(PufferAuslegungVorgaben.PRAEFIX, v.Schluessel, StringComparison.Ordinal);
                Assert.Contains(v.Herkunftsart, PufferHerkunftsart.ALLE);
                Assert.False(string.IsNullOrWhiteSpace(v.Quelle));
            });
            foreach (string typ in PufferAuslegungVorgaben.VORLAGEN)
                foreach (string k in PufferAuslegungVorgaben.VORLAGE_SCHALTER)
                {
                    double w = PufferAuslegungVorgaben.AlsWoerterbuch()[PufferAuslegungVorgaben.VorlageSchluessel(typ, k)];
                    Assert.True(w == 0 || w == 1, typ + "." + k);
                }
            Assert.Equal(ProjektPuffer.WH_JE_LITER_KELVIN,
                         PufferAuslegungVorgaben.AlsWoerterbuch()["Pufferauslegung.Konstante.Wh_je_l_K"]);
        }

        [Fact]
        public void Die_Pruefklauseln_der_Auslegungstabelle()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "PRAGMA foreign_keys = ON");
            Ausfuehren(c, "CREATE TABLE Tab_Projekt (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE Tab_Pufferspeicher (ID INTEGER PRIMARY KEY, ID_Projekt INTEGER) STRICT");
            Ausfuehren(c, PufferAuslegungSchema.SqlCreateAuslegung());
            Ausfuehren(c, PufferAuslegungSchema.SqlCreateParameter());
            Ausfuehren(c, "INSERT INTO Tab_Projekt (ID) VALUES (1)");
            Ausfuehren(c, "INSERT INTO Tab_Pufferspeicher (ID, ID_Projekt) VALUES (5, 1)");

            Assert.False(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, ID_Pufferspeicher, Klasse_Heizung, Vorlage, " +
                                  "Sperrprofil, Deckungsziel, Zirkulation_Weg) VALUES (1, 5, 1, 'WP_MONO', 'ZWEI_MAL_ZWEI', 1, 'JE_WE')"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Klasse_Heizung) VALUES (1, 2)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Vorlage) VALUES (1, 'WAERMEPUMPE')"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Deckungsziel) VALUES (1, 1.5)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Mindestlaufzeit_min) VALUES (1, 0)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, Sperrdauer_h) VALUES (1, 25)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt, WP_Geregelt) VALUES (1, -1)"));
            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegung (ID_Projekt) VALUES (99)"));

            // Der Puffer geht, die Auslegung bleibt als „neu anlegen“; das Projekt geht, die Zeile mit.
            Ausfuehren(c, "DELETE FROM Tab_Pufferspeicher WHERE ID = 5");
            Assert.Equal(0L, Zahl(c, "SELECT COUNT(*) FROM Tab_PufferAuslegung WHERE ID_Pufferspeicher IS NOT NULL"));
            Assert.Equal(1L, Zahl(c, "SELECT COUNT(*) FROM Tab_PufferAuslegung"));
            Ausfuehren(c, "DELETE FROM Tab_Projekt WHERE ID = 1");
            Assert.Equal(0L, Zahl(c, "SELECT COUNT(*) FROM Tab_PufferAuslegung"));

            Assert.True(Wirft(c, "INSERT INTO Tab_PufferAuslegungParameter_STAMM (Schluessel, Wert, Quelle, Herkunftsart) " +
                                 "VALUES ('x', 1, 'q', 'NORM')"));
        }

        /// <summary>
        /// Der Schritt aus dem Stand VOR ihm: Beide Tabellen werden entfernt; der Schritt legt sie an
        /// und sät jede Vorgabe, ein zweiter Lauf tut nichts. Eine gepflegte Zeile bleibt (INSERT OR IGNORE).
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            Assert.True(PufferAuslegungSchema.Vollstaendig());
            DataRepository.ExecuteNonQuery("DROP TABLE \"" + PufferAuslegungSchema.TAB + "\"");
            DataRepository.ExecuteNonQuery("DROP TABLE \"" + PufferAuslegungSchema.TAB_PARAMETER + "\"");
            Assert.False(PufferAuslegungSchema.Vollstaendig());
            Assert.Equal(2, PufferAuslegungSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(2 + SAATZAHL, PufferAuslegungSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_PufferAuslegung anlegen"));
            Assert.Contains(bericht, z => z.Contains(SAATZAHL.ToString(CultureInfo.InvariantCulture) + " Vorgabe(n)"));
            Assert.True(PufferAuslegungSchema.Vollstaendig());
            Assert.Equal((long)SAATZAHL, Zahl("SELECT COUNT(*) FROM Tab_PufferAuslegungParameter_STAMM"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_PufferAuslegung"));
            Assert.Equal(20.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Wert FROM Tab_PufferAuslegungParameter_STAMM WHERE Schluessel = ?",
                new DbParam("@s", "Pufferauslegung.Faustwert.FixedSpeed_l_kW")), CultureInfo.InvariantCulture));
            Assert.Equal("VDI 4645 E 2026-03, Tabelle 14", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Quelle FROM Tab_PufferAuslegungParameter_STAMM WHERE Schluessel = ?",
                new DbParam("@s", "Pufferauslegung.Sperrzeit.Stillstand.15.RADIATOR_h"))));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_PufferAuslegungParameter_STAMM WHERE ReadOnly <> 1"));

            // Eine gepflegte Zeile bleibt; eine fehlende kommt zurück.
            DataRepository.ExecuteNonQuery("UPDATE Tab_PufferAuslegungParameter_STAMM SET Wert = 7 WHERE Schluessel = ?",
                                           new DbParam("@s", "Pufferauslegung.Takt.Startziel_je_Tag"));
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_PufferAuslegungParameter_STAMM WHERE Schluessel = ?",
                                           new DbParam("@s", "Pufferauslegung.Mindestlaufzeit_min"));
            Assert.False(PufferAuslegungSchema.Vollstaendig());
            Assert.Equal(1, PufferAuslegungSchema.Ausfuehren(null));
            Assert.Equal(7.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Wert FROM Tab_PufferAuslegungParameter_STAMM WHERE Schluessel = ?",
                new DbParam("@s", "Pufferauslegung.Takt.Startziel_je_Tag")), CultureInfo.InvariantCulture));

            var zweiter = new List<string>();
            Assert.Equal(0, PufferAuslegungSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        /// <summary>Die Parameter lesen die Tabelle; ein gepflegter Wert gilt, ein fehlender fällt auf die Vorgabe.</summary>
        [Fact]
        public void Die_Parameter_lesen_die_Tabelle_mit_Rueckfall()
        {
            if (!_db.Vorhanden) return;

            DataRepository.ExecuteNonQuery("UPDATE Tab_PufferAuslegungParameter_STAMM SET Wert = 8 WHERE Schluessel = ?",
                                           new DbParam("@s", "Pufferauslegung.Takt.Startziel_je_Tag"));
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_PufferAuslegungParameter_STAMM WHERE Schluessel = ?",
                                           new DbParam("@s", "Pufferauslegung.Mindestlaufzeit_min"));
            PufferAuslegungParameter p = PufferAuslegungParameter.Lesen();
            Assert.Equal(8.0, p.Wert(PufferAuslegungVorgaben.STARTZIEL));
            Assert.Equal(10.0, p.Wert(PufferAuslegungVorgaben.MINDESTLAUFZEIT));
            Assert.True(p.AusTabelle(PufferAuslegungVorgaben.STARTZIEL));
            Assert.False(p.AusTabelle(PufferAuslegungVorgaben.MINDESTLAUFZEIT));
        }

        private static long Zahl(string sql) =>
            Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

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
