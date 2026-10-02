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
    /// <b>Die Bodenalbedo je Photovoltaik- und Solarthermie-Anlage</b> (Entscheidungsvorlage
    /// Modellgrenzen, PV4; Schritt bei <see cref="AlbedoSchema"/>, Leseregel bei
    /// <see cref="Bodenalbedo"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Leseregel (leer und Unzulässiges = 0,2), die Spalte samt
    /// Prüfklausel, der Schritt aus dem Stand davor und wiederholbar, dass Migration, Werkzeug und
    /// Testkopie ihn aus derselben Quelle führen, die Transposition (Vorgabe bitgleich, mehr Albedo
    /// mehr Einstrahlung auf die geneigte Fläche), der Speicherweg der Anlagenzeile und der
    /// PV-Lauf.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AlbedoSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private const int TESTPROJEKT = 1040;

        // =============================================================================
        //  Teil 1 - Leseregel, Spalte, Transposition (ohne Datenbank)
        // =============================================================================

        /// <summary>Leer, NaN und außerhalb 0 … 1 rechnen die Vorgabe 0,2; sonst der gepflegte Wert.</summary>
        [Fact]
        public void Die_Leseregel()
        {
            Assert.Equal(0.2, Bodenalbedo.VORGABE);
            Assert.Equal(SolarCalculator.ALBEDO_BODEN, Bodenalbedo.VORGABE);
            Assert.Equal(0.2, Bodenalbedo.Wert((double?)null));
            Assert.Equal(0.2, Bodenalbedo.Wert(double.NaN));
            Assert.Equal(0.2, Bodenalbedo.Wert(-0.1));
            Assert.Equal(0.2, Bodenalbedo.Wert(1.1));
            Assert.Equal(0.0, Bodenalbedo.Wert(0.0));
            Assert.Equal(0.75, Bodenalbedo.Wert(0.75));
            Assert.Equal(1.0, Bodenalbedo.Wert(1.0));
            Assert.Equal(0.2, Bodenalbedo.Wert((WErzeugerModel)null));
            Assert.Equal(0.5, Bodenalbedo.Wert(new WErzeugerModel { Albedo = 0.5 }));

            Assert.True(Bodenalbedo.Zulaessig(null));
            Assert.True(Bodenalbedo.Zulaessig(0.6));
            Assert.False(Bodenalbedo.Zulaessig(1.5));
            Assert.False(Bodenalbedo.Zulaessig(-1));
        }

        /// <summary>Die Spalte an einer STRICT-Tabelle: nullbar, ohne Vorgabe, nur 0 … 1 zulässig.</summary>
        [Fact]
        public void Die_Spalte_ist_nullbar_mit_Pruefklausel()
        {
            Assert.Equal(KesselBereitschaftEinheitSchema.SCHRITT + 1, AlbedoSchema.SCHRITT);
            Assert.Equal(163, AlbedoSchema.SCHRITT);
            Assert.Equal(AlbedoSchema.SCHRITT, SchemaStand.Zielversion);
            Assert.Equal("Tab_Energieanlagen", AlbedoSchema.TABELLE);

            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == AlbedoSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);

            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            Ausfuehren(c, "ALTER TABLE T ADD COLUMN \"" + AlbedoSchema.SPALTE + "\" " + AlbedoSchema.TYP);
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (2)");
            Assert.Equal(2L, Skalar(c, "SELECT COUNT(*) FROM T WHERE Albedo IS NULL"));
            Assert.False(Wirft(c, "UPDATE T SET Albedo = 0.6 WHERE ID = 2"));
            Assert.False(Wirft(c, "UPDATE T SET Albedo = 0 WHERE ID = 1"));
            Assert.False(Wirft(c, "UPDATE T SET Albedo = 1 WHERE ID = 1"));
            Assert.True(Wirft(c, "UPDATE T SET Albedo = 1.01"), "Über 1 ist unzulässig.");
            Assert.True(Wirft(c, "UPDATE T SET Albedo = -0.1"), "Unter 0 ist unzulässig.");
            Assert.False(Wirft(c, "UPDATE T SET Albedo = NULL"));
        }

        /// <summary>
        /// Alle drei Transpositionen: ohne Argument bitgleich mit 0,2 ausdrücklich; mehr Albedo heißt
        /// mehr Einstrahlung auf eine geneigte Fläche, auf der waagrechten wirkt sie nicht.
        /// </summary>
        [Fact]
        public void Die_Transposition_nimmt_die_Albedo()
        {
            const double lon = 9.18, lat = 48.78, ghi = 600, dni = 500, dhi = 200;
            const int tag = 172;
            const double stunde = 11.0;

            double iso = SolarCalculator.CalculateHourly(lon, lat, 60, 0, ghi, dni, dhi, 20, tag, stunde);
            Assert.Equal(BitConverter.DoubleToInt64Bits(iso),
                         BitConverter.DoubleToInt64Bits(SolarCalculator.CalculateHourly(lon, lat, 60, 0, ghi, dni, dhi, 20, tag, stunde, 0.2)));
            Assert.True(SolarCalculator.CalculateHourly(lon, lat, 60, 0, ghi, dni, dhi, 20, tag, stunde, 0.7) > iso);
            Assert.Equal(SolarCalculator.CalculateHourly(lon, lat, 0, 0, ghi, dni, dhi, 20, tag, stunde, 0.7),
                         SolarCalculator.CalculateHourly(lon, lat, 0, 0, ghi, dni, dhi, 20, tag, stunde), 12);

            double hd = SolarCalculator.CalculateHourlyHayDavies(lon, lat, 90, 0, ghi, dni, dhi, tag, stunde);
            Assert.Equal(BitConverter.DoubleToInt64Bits(hd),
                         BitConverter.DoubleToInt64Bits(SolarCalculator.CalculateHourlyHayDavies(lon, lat, 90, 0, ghi, dni, dhi, tag, stunde, 0.2)));
            // Senkrechte Fläche: Sichtfaktor zum Boden 1/2 - der Unterschied ist GHI · ΔAlbedo / 2.
            double hd8 = SolarCalculator.CalculateHourlyHayDavies(lon, lat, 90, 0, ghi, dni, dhi, tag, stunde, 0.8);
            Assert.Equal(ghi * 0.6 * 0.5, hd8 - hd, 6);

            double einfach = SolarCalculator.Calculate(ghi, dhi, lat, lon, tag, stunde, 45, 0);
            Assert.Equal(BitConverter.DoubleToInt64Bits(einfach),
                         BitConverter.DoubleToInt64Bits(SolarCalculator.Calculate(ghi, dhi, lat, lon, tag, stunde, 45, 0, 0.2)));
            Assert.True(SolarCalculator.Calculate(ghi, dhi, lat, lon, tag, stunde, 45, 0, 0.5) > einfach);
        }

        // =============================================================================
        //  Teil 2 - der Schritt auf der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Der Schritt aus dem Stand davor: Die Spalte wird entfernt; der Schritt legt sie an, jede
        /// Zeile bleibt leer, die übrigen Werte bleiben, und ein zweiter Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_ist_wiederholbar_und_laesst_den_Bestand_leer()
        {
            if (!_db.Vorhanden) return;

            Assert.True(AlbedoSchema.Vollstaendig());
            Assert.Empty(AlbedoSchema.Anweisungen);
            List<string> vorher = Bestand();

            DataRepository.ExecuteNonQuery("ALTER TABLE \"" + AlbedoSchema.TABELLE + "\" DROP COLUMN \"" +
                                           AlbedoSchema.SPALTE + "\"");
            Assert.False(AlbedoSchema.Vollstaendig());
            Assert.Single(AlbedoSchema.Anweisungen);

            // Ohne Spalte liest der Controller leer.
            List<WErzeugerModel> ohne = WErzeugerCtrl.ModelleJeTyp(TESTPROJEKT, WizardItemClass.PV_TYP);
            Assert.NotEmpty(ohne);
            Assert.All(ohne, a => Assert.Null(a.Albedo));

            var bericht = new List<string>();
            Assert.Equal(1, AlbedoSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Energieanlagen.Albedo anlegen"));
            Assert.True(AlbedoSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE Albedo IS NOT NULL"));

            var zweiter = new List<string>();
            Assert.Equal(0, AlbedoSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        /// <summary>
        /// Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den Schritt
        /// aus derselben Quelle, hinter der Einheit des Bereitschaftsverlusts.
        /// </summary>
        [Fact]
        public void Werkzeug_Migration_und_Testkopie_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.True(werkzeug.IndexOf("AlbedoSchema.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("KesselBereitschaftEinheitSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_ALBEDO = AlbedoSchema.SCHRITT", migration);
            Assert.True(migration.IndexOf("new Schritt(SCHRITT_ALBEDO", StringComparison.Ordinal) >
                        migration.IndexOf("new Schritt(SCHRITT_KESSEL_BEREITSCHAFT_EINHEIT", StringComparison.Ordinal));
            Assert.Contains("AlbedoSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("AlbedoSchema.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("KesselBereitschaftEinheitSchema.Ausfuehren(null)", StringComparison.Ordinal));
        }

        // =============================================================================
        //  Teil 3 - Speicherweg und Lauf
        // =============================================================================

        /// <summary>
        /// Der Speicherweg der Anlagenzeile (dieselbe Anweisung wie Löschen + Neuanlegen) schreibt die
        /// Albedo, leer bleibt leer, ein unzulässiger Wert fällt zu leer statt das Einfügen scheitern
        /// zu lassen.
        /// </summary>
        [Fact]
        public void Der_Speicherweg_traegt_die_Albedo()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0.55, Anlegen("Albedo-Probe gepflegt", 0.55));
            Assert.Null(Anlegen("Albedo-Probe leer", null));
            Assert.Null(Anlegen("Albedo-Probe unzulaessig", 1.7));
        }

        /// <summary>
        /// Der PV-Lauf liest die Albedo der Anlage: 0,2 ausdrücklich ist bitgleich zu leer, ein
        /// hellerer Boden bringt mehr Ertrag auf die geneigten Anlagen des Testprojekts.
        /// </summary>
        [Fact]
        public void Der_PV_Lauf_rechnet_mit_der_Albedo_der_Anlage()
        {
            if (!_db.Vorhanden) return;

            double leer = Jahresertrag();
            Assert.True(leer > 0, "Das Testprojekt führt eine PV-Anlage.");

            AlbedoSetzen(0.2);
            Assert.Equal(BitConverter.DoubleToInt64Bits(leer), BitConverter.DoubleToInt64Bits(Jahresertrag()));

            AlbedoSetzen(0.7);
            Assert.True(Jahresertrag() > leer, "Mehr Bodenreflex, mehr Ertrag.");

            AlbedoSetzen(null);
            Assert.Equal(BitConverter.DoubleToInt64Bits(leer), BitConverter.DoubleToInt64Bits(Jahresertrag()));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static double Jahresertrag()
        {
            var pv = new SimulationPV();
            pv.Berechnung(TESTPROJEKT);
            return pv.Modul_Ergebnisse.Sum(m => m.StromproduktionKwh);
        }

        private static void AlbedoSetzen(double? wert)
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET Albedo = ? WHERE ID_Projekt = ? AND ID_Type = ?",
                ProjektPuffer.Par("@a", DbParamTyp.Double, wert.HasValue ? (object)wert.Value : null),
                new DbParam("@p", TESTPROJEKT), new DbParam("@t", WizardItemClass.PV_TYP));
        }

        /// <summary>Legt eine PV-Anlagenzeile über den Speicherweg an und liest ihre Albedo zurück.</summary>
        private static double? Anlegen(string bezeichner, double? albedo)
        {
            int idPv = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_PV FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? LIMIT 1",
                new DbParam("@p", TESTPROJEKT), new DbParam("@t", WizardItemClass.PV_TYP)),
                CultureInfo.InvariantCulture);
            var m = new WErzeugerCtrl
            {
                ID_Projekt = TESTPROJEKT,
                Bezeichner = bezeichner,
                ID_Type = WizardItemClass.PV_TYP,
                ID_PV = idPv,
                PV_Leistung = 4,
                m_Neigung = 30,
                m_Azimut = 0,
                Albedo = albedo
            };
            Assert.True(m.Insert());
            WErzeugerModel gelesen = WErzeugerCtrl.ModelleJeTyp(TESTPROJEKT, WizardItemClass.PV_TYP)
                .Single(a => a.Bezeichner == bezeichner);
            return gelesen.Albedo;
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        /// <summary>Die Anlagenzeilen ohne die neue Spalte — der Schritt darf sie nicht ändern.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ID_Projekt, Bezeichner, ID_Type, Neigung, Azimut, PV_Systemverluste FROM Tab_Energieanlagen ORDER BY ID");
            foreach (DataRow r in dt.Rows)
                liste.Add(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            return liste;
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static long Skalar(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try
            {
                Ausfuehren(c, sql);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
        }

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
