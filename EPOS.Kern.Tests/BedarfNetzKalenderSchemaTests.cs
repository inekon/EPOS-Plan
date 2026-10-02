using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Netzverluste je Kanal, Zirkulation und Betriebskalender — der Schemaschritt</b>
    /// (Entscheidungsvorlage Modellgrenzen BW4, PW2, BW2; Schritt bei
    /// <see cref="BedarfNetzKalenderSchema"/>).
    ///
    /// <para><b>Geprüft wird:</b> Nummer und Register, die Prüfklauseln an STRICT-Tabellen
    /// (Kanalpaare, Prozentgrenze, Zirkulation, Kalendertabelle), der Schritt aus dem Stand davor —
    /// wiederholbar und ohne Änderung am Bestand —, der Fremdschlüssel mit SET NULL und dass
    /// Migration, Werkzeug und Testkopie ihn aus derselben Quelle führen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BedarfNetzKalenderSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Nummer hinter dem Temperaturpaar je Prozess, Ziel des Schemas, Register mit Art Ddl.</summary>
        [Fact]
        public void Nummer_Ziel_und_Register()
        {
            Assert.Equal(ProzesswaermeTemperaturSchema.SCHRITT + 1, BedarfNetzKalenderSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= BedarfNetzKalenderSchema.SCHRITT);
            Assert.Equal(8, BedarfNetzKalenderSchema.SPALTEN_EINSTELLUNGEN.Count);
            Assert.Equal(new[] { "Z_Projekt_Brauchwasser", "Z_Projekt_Prozesswaerme", "Z_Projekt_Stromverbraucher" },
                         BedarfNetzKalenderSchema.ZUORDNUNGEN);

            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == BedarfNetzKalenderSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
        }

        /// <summary>
        /// Die Prüfklauseln der acht Spalten an einer STRICT-Tabelle: alles leer, Wert mit Einheit,
        /// 100 % und feste Mengen über 100 sind zulässig; ein halbes Paar, ein negativer Wert, mehr
        /// als 100 %, eine fremde Einheit und eine Zirkulation außerhalb der Grenzen nicht.
        /// </summary>
        [Fact]
        public void Die_Pruefklauseln_der_Einstellungsspalten()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            foreach (KeyValuePair<string, string> sp in BedarfNetzKalenderSchema.SPALTEN_EINSTELLUNGEN)
                Ausfuehren(c, "ALTER TABLE T ADD COLUMN \"" + sp.Key + "\" " + sp.Value);

            Assert.False(Wirft(c, "UPDATE T SET Netzverluste_Heizung = 5, Netzverluste_Heizung_Einheit = '%'"));
            Assert.False(Wirft(c, "UPDATE T SET Netzverluste_Prozess = 100, Netzverluste_Prozess_Einheit = '%'"));
            Assert.False(Wirft(c, "UPDATE T SET Netzverluste_Brauchwasser = 25000, Netzverluste_Brauchwasser_Einheit = 'kWh/a'"));
            Assert.False(Wirft(c, "UPDATE T SET Zirkulation_Leistung_kW = 100, Zirkulation_Laufzeit_h_d = 24"));
            Assert.False(Wirft(c, "UPDATE T SET Zirkulation_Leistung_kW = NULL, Zirkulation_Laufzeit_h_d = NULL"));

            Assert.True(Wirft(c, "INSERT INTO T (ID, Netzverluste_Heizung) VALUES (2, 3)"), "Wert ohne Einheit.");
            Assert.True(Wirft(c, "INSERT INTO T (ID, Netzverluste_Heizung_Einheit) VALUES (3, '%')"), "Einheit ohne Wert.");
            Assert.True(Wirft(c, "UPDATE T SET Netzverluste_Heizung = NULL"), "Einheit ohne Wert.");
            Assert.True(Wirft(c, "UPDATE T SET Netzverluste_Heizung = -1"), "Negativ.");
            Assert.True(Wirft(c, "UPDATE T SET Netzverluste_Prozess = 101"), "Über 100 %.");
            Assert.True(Wirft(c, "UPDATE T SET Netzverluste_Heizung_Einheit = 'MWh'"), "Fremde Einheit.");
            Assert.True(Wirft(c, "UPDATE T SET Zirkulation_Leistung_kW = 101"), "Über 100 kW.");
            Assert.True(Wirft(c, "UPDATE T SET Zirkulation_Laufzeit_h_d = 25"), "Über 24 h/d.");
        }

        /// <summary>Die Kalendertabelle: Vorgaben, Prüfklauseln und Ferienpaare.</summary>
        [Fact]
        public void Die_Kalendertabelle_haelt_ihre_Grenzen()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, BedarfNetzKalenderSchema.SqlCreateKalender());
            Ausfuehren(c, "INSERT INTO Tab_Betriebskalender (Bezeichner) VALUES ('Werk')");
            Assert.Equal("0|1|0", Convert.ToString(Skalar(c,
                "SELECT Ferienfaktor || '|' || Feiertag_wie_Sonntag || '|' || Ferien_kuerzen FROM Tab_Betriebskalender"),
                CultureInfo.InvariantCulture).Replace(".0", ""));

            Assert.False(Wirft(c, "UPDATE Tab_Betriebskalender SET Bundesland = 'BY', Ferien1_Von = 355, Ferien1_Bis = 6"));
            Assert.True(Wirft(c, "UPDATE Tab_Betriebskalender SET Bundesland = 'XX'"), "Unbekanntes Land.");
            Assert.True(Wirft(c, "UPDATE Tab_Betriebskalender SET Ferien2_Von = 10"), "Halber Zeitraum.");
            Assert.True(Wirft(c, "UPDATE Tab_Betriebskalender SET Ferien3_Von = 0, Ferien3_Bis = 5"), "Tag 0.");
            Assert.True(Wirft(c, "UPDATE Tab_Betriebskalender SET Ferien4_Von = 1, Ferien4_Bis = 366"), "Tag 366.");
            Assert.True(Wirft(c, "UPDATE Tab_Betriebskalender SET Ferienfaktor = 1.5"), "Faktor über 1.");
            Assert.True(Wirft(c, "UPDATE Tab_Betriebskalender SET Ferien_kuerzen = 2"), "Schalter 0/1.");
            Assert.True(Wirft(c, "INSERT INTO Tab_Betriebskalender (Bezeichner) VALUES ('')"), "Leerer Name.");
        }

        /// <summary>
        /// Der Schritt aus dem Stand davor: Tabelle und Spalten werden entfernt; der Schritt legt alle
        /// zwölf Teile an, die Bestandswerte bleiben, alles Neue ist leer, ein zweiter Lauf legt nichts an.
        /// Danach setzt das Löschen eines Kalenders die Zuordnung auf leer (ON DELETE SET NULL).
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            Assert.True(BedarfNetzKalenderSchema.Vollstaendig());
            Assert.Empty(BedarfNetzKalenderSchema.Anweisungen);
            List<string> vorher = Bestand();

            foreach (string z in BedarfNetzKalenderSchema.ZUORDNUNGEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + z + "\" DROP COLUMN \"ID_Betriebskalender\"");
            foreach (KeyValuePair<string, string> sp in BedarfNetzKalenderSchema.SPALTEN_EINSTELLUNGEN.Reverse())
                DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Einstellungen\" DROP COLUMN \"" + sp.Key + "\"");
            DataRepository.ExecuteNonQuery("DROP TABLE \"Tab_Betriebskalender\"");
            Assert.False(BedarfNetzKalenderSchema.Vollstaendig());
            Assert.Equal(12, BedarfNetzKalenderSchema.Anweisungen.Count());

            var bericht = new List<string>();
            Assert.Equal(12, BedarfNetzKalenderSchema.Ausfuehren(bericht));
            Assert.Equal("Tab_Betriebskalender anlegen", bericht[0]);
            Assert.True(BedarfNetzKalenderSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Netzverluste_Heizung IS NOT NULL " +
                                  "OR Netzverluste_Brauchwasser IS NOT NULL OR Netzverluste_Prozess IS NOT NULL " +
                                  "OR Zirkulation_Leistung_kW IS NOT NULL OR Zirkulation_Laufzeit_h_d IS NOT NULL"));
            foreach (string z in BedarfNetzKalenderSchema.ZUORDNUNGEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + z + "\" WHERE ID_Betriebskalender IS NOT NULL"));

            var zweiter = new List<string>();
            Assert.Equal(0, BedarfNetzKalenderSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));

            // ON DELETE SET NULL: Die Zuordnung steht nach dem Löschen des Kalenders ohne ihn.
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Betriebskalender (Bezeichner) VALUES (?)", new DbParam("?", "Werk"));
            long id = Zahl("SELECT MAX(ID) FROM Tab_Betriebskalender");
            DataRepository.ExecuteNonQuery("UPDATE Z_Projekt_Brauchwasser SET ID_Betriebskalender = ? " +
                                           "WHERE ID = (SELECT MIN(ID) FROM Z_Projekt_Brauchwasser)", new DbParam("?", id));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Z_Projekt_Brauchwasser WHERE ID_Betriebskalender IS NOT NULL"));
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Betriebskalender WHERE ID = ?", new DbParam("?", id));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Z_Projekt_Brauchwasser WHERE ID_Betriebskalender IS NOT NULL"));
        }

        /// <summary>
        /// Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den Schritt
        /// aus derselben Quelle, hinter dem Temperaturpaar je Prozess.
        /// </summary>
        [Fact]
        public void Werkzeug_Migration_und_Testkopie_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("BedarfNetzKalenderSchema.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("ProzesswaermeTemperaturSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_BEDARF_NETZ_KALENDER = BedarfNetzKalenderSchema.SCHRITT", migration);
            Assert.True(migration.IndexOf("new Schritt(SCHRITT_BEDARF_NETZ_KALENDER", StringComparison.Ordinal) >
                        migration.IndexOf("new Schritt(SCHRITT_PROZESSWAERME_TEMPERATUR", StringComparison.Ordinal));
            Assert.Contains("BedarfNetzKalenderSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("BedarfNetzKalenderSchema.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("ProzesswaermeTemperaturSchema.Ausfuehren(null)", StringComparison.Ordinal));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Bestandswerte der vier Tabellen ohne die neuen Spalten.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            var abfragen = new[]
            {
                "SELECT ID, ID_Projekt, Netzverluste, NetzverlusteEinheit FROM Tab_Einstellungen ORDER BY ID",
                "SELECT ID, ID_Projekt, ID_Brauchwasser, Summe FROM Z_Projekt_Brauchwasser ORDER BY ID",
                "SELECT ID, ID_Projekt, ID_Prozesswaerme, Summe FROM Z_Projekt_Prozesswaerme ORDER BY ID",
                "SELECT ID, ID_Projekt, ID_Stromverbraucher, Summe FROM Z_Projekt_Stromverbraucher ORDER BY ID"
            };
            foreach (string sql in abfragen)
            {
                DataTable dt = DataRepository.GetDataTable(sql);
                foreach (DataRow r in dt.Rows)
                    liste.Add(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
            return liste;
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static object Skalar(SqliteConnection c, string sql)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar();
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try { Ausfuehren(c, sql); return false; }
            catch (SqliteException) { return true; }
        }

        private static string Repowurzel()
        {
            string d = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && d != null; i++)
            {
                if (File.Exists(Path.Combine(d, "WP-Plan.sln"))) return d;
                d = Path.GetDirectoryName(d.TrimEnd(Path.DirectorySeparatorChar));
            }
            return null;
        }
    }
}
