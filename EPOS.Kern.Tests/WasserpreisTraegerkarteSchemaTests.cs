using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schemaschritt „Der Wasserpreis wandert in die Trägerkarte"</b> (<see cref="WasserpreisTraegerkarteSchema"/>,
    /// KB-7a): Nummer und Register, der Träger „Wasser“ (einmal, erkannt am Preismodell), die Preisübernahme mit Regel c
    /// und gepflegtem Kartenpreis, der Entfall der Spalte, der leere zweite Lauf, der Rückkühlwerk-Schritt legt die Spalte
    /// nicht wieder an, und die Paketanhebung eines älteren Pakets.
    /// </summary>
    [Collection("Testdatenbank")]
    public class WasserpreisTraegerkarteSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        private const string TAB = WasserpreisTraegerkarteSchema.TAB_ANLAGEN;
        private const string SPALTE = WasserpreisTraegerkarteSchema.ALTSPALTE;

        [Fact]
        public void Die_Nummer_haengt_an_der_Vorgaengerklasse_und_steht_in_den_Registern()
        {
            // Schritt 235, haengt an 234 (BedarfsprofilEingabeartSchema, KBP-5).
            Assert.Equal(BedarfsprofilEingabeartSchema.SCHRITT + 1, WasserpreisTraegerkarteSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= WasserpreisTraegerkarteSchema.SCHRITT);
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == WasserpreisTraegerkarteSchema.SCHRITT &&
                                                       s.Wirkung == Paketanhebung.Art.Umformung);
            Assert.DoesNotContain(RueckkuehlwerkSchema.SPALTEN_ANLAGE, s => s.Spalte == SPALTE);
        }

        [Fact]
        public void Die_Testkopie_fuehrt_den_Traeger_einmal_und_keine_Preisspalte_mehr()
        {
            if (!_db.Vorhanden) return;
            Assert.True(WasserpreisTraegerkarteSchema.Vollstaendig());
            Assert.False(DataRepository.SpalteVorhanden(TAB, SPALTE));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM energy_carrier WHERE pricing_model = 'WATER'"));
            DataTable t = DataRepository.GetDataTable(
                "SELECT name, billing_unit, hi_kwh_per_unit, hs_kwh_per_unit, price_work, price_base, co2, so2, nox, " +
                "ID_Brennstoff, is_active FROM energy_carrier WHERE pricing_model = 'WATER'");
            DataRow r = t.Rows[0];
            Assert.Equal(("Wasser", "m³"), (Convert.ToString(r[0], CultureInfo.InvariantCulture),
                                            Convert.ToString(r[1], CultureInfo.InvariantCulture)));
            Assert.Equal(DBNull.Value, r[2]);
            Assert.Equal(DBNull.Value, r[3]);
            for (int i = 4; i <= 9; i++) Assert.Equal(0.0, Convert.ToDouble(r[i], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(r[10], CultureInfo.InvariantCulture));

            var zweiter = new List<string>();
            Assert.Equal(0, WasserpreisTraegerkarteSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("nichts zu tun", StringComparison.Ordinal));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
        }

        [Fact]
        public void Der_Traeger_wird_angelegt_der_Preis_uebernommen_und_die_Spalte_entfaellt()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("DELETE FROM energy_carrier WHERE pricing_model = 'WATER'");
            DataRepository.ExecuteNonQuery("UPDATE Tab_Applikation SET Kostenkatalog_Geaendert = NULL");   // Stempel des Loeschens
            SpalteWiederAnlegen();
            long anlage = Anlagen(1055).First();
            DataRepository.ExecuteNonQuery("UPDATE \"" + TAB + "\" SET \"" + SPALTE + "\" = ? WHERE ID = ?",
                                           new DbParam("@w", 2.5), new DbParam("@id", anlage));
            Assert.Equal(2, WasserpreisTraegerkarteSchema.Offen());

            WasserpreisTraegerkarteSchema.Laufergebnis e = WasserpreisTraegerkarteSchema.Lauf(null);
            Assert.Equal((1, 1, 0, 0, 1), (e.TraegerAngelegt, e.Uebernommen, e.Verworfen, e.Behalten, e.Entfernt));
            Assert.True(WasserpreisTraegerkarteSchema.Vollstaendig());
            Assert.Equal(2.5, Kartenpreis(1055));
            // Der neue Traeger stempelt den Katalog nicht als geaendert (Preis 0, keine Zuordnung).
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Applikation WHERE Kostenkatalog_Geaendert IS NOT NULL"));

            // Zweiter Lauf: nichts. Ein umbenannter Traeger wird am Preismodell erkannt - kein zweiter.
            DataRepository.ExecuteNonQuery("UPDATE energy_carrier SET name = 'Brauchwasser' WHERE pricing_model = 'WATER'");
            Assert.Equal(0, WasserpreisTraegerkarteSchema.Lauf(null).Handgriffe);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM energy_carrier WHERE pricing_model = 'WATER'"));

            // Der Rueckkuehlwerk-Schritt (214) legt die Spalte nach diesem Schritt nicht wieder an.
            RueckkuehlwerkSchema.Ausfuehren(null);
            Assert.False(DataRepository.SpalteVorhanden(TAB, SPALTE));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
        }

        [Fact]
        public void Mehrere_Preise_kleinste_Anlage_gewinnt_und_ein_gepflegter_Kartenpreis_bleibt()
        {
            if (!_db.Vorhanden) return;
            long traeger = WasserpreisTraegerkarteSchema.TraegerId().Value;
            SpalteWiederAnlegen();
            long[] a1007 = Anlagen(1007).Take(2).ToArray();
            Assert.Equal(2, a1007.Length);
            Setzen(a1007[1], 3.0);
            Setzen(a1007[0], 2.0);
            long a1017 = Anlagen(1017).First();
            Setzen(a1017, 1.5);
            DataRepository.ExecuteNonQuery("INSERT INTO energy_project_settings (ID_Projekt, \"ID_Energieträger\", custom_price_work) " +
                                           "VALUES (?, ?, ?)", new DbParam("@p", 1017), new DbParam("@t", traeger), new DbParam("@w", 4.0));

            var bericht = new List<string>();
            WasserpreisTraegerkarteSchema.Laufergebnis e = WasserpreisTraegerkarteSchema.Lauf(bericht);
            Assert.Equal((0, 1, 1, 1, 1), (e.TraegerAngelegt, e.Uebernommen, e.Verworfen, e.Behalten, e.Entfernt));
            Assert.Equal(2.0, Kartenpreis(1007));
            Assert.Equal(4.0, Kartenpreis(1017));
            string hinweis = Assert.Single(bericht, z => z.Contains("verworfen", StringComparison.Ordinal));
            Assert.Contains("Projekt 1007", hinweis, StringComparison.Ordinal);
            Assert.Contains("Anlage " + a1007[1].ToString(CultureInfo.InvariantCulture) + " (3 EUR/m3)", hinweis, StringComparison.Ordinal);
            Assert.Contains(bericht, z => z.Contains("Projekt 1017", StringComparison.Ordinal) && z.Contains("bleibt", StringComparison.Ordinal));
            Assert.False(DataRepository.SpalteVorhanden(TAB, SPALTE));
            Assert.Equal(0, WasserpreisTraegerkarteSchema.Lauf(null).Handgriffe);
        }

        /// <summary>
        /// Die Nachschlagezeile <c>pricing_model</c> 'WATER' wird angelegt, wenn sie fehlt (alle Flaggen 0, Name „Wasser“),
        /// einmal — der zweite Lauf tut nichts; der Katalogstempel bleibt; die Trägerliste des Katalogs (LEFT JOIN) liest
        /// für den Wasserträger dieselben Flaggen wie ohne die Zeile.
        /// </summary>
        [Fact]
        public void Die_Nachschlagezeile_des_Preismodells_wird_einmal_angelegt()
        {
            if (!_db.Vorhanden) return;
            const string FLAGGEN = "SELECT pm.has_hi, pm.has_hs, pm.has_powerprice FROM energy_carrier AS ec " +
                                   "LEFT JOIN pricing_model AS pm ON ec.pricing_model = pm.code WHERE ec.pricing_model = 'WATER'";
            Assert.True(WasserpreisTraegerkarteSchema.PreismodellVorhanden());
            DataRepository.ExecuteNonQuery("DELETE FROM pricing_model WHERE code = 'WATER'");
            DataRepository.ExecuteNonQuery("UPDATE Tab_Applikation SET Kostenkatalog_Geaendert = NULL");   // Stempel des Loeschens
            Assert.False(WasserpreisTraegerkarteSchema.PreismodellVorhanden());
            Assert.Equal(1, WasserpreisTraegerkarteSchema.Offen());
            DataRow ohne = DataRepository.GetDataTable(FLAGGEN).Rows[0];
            Assert.All(ohne.ItemArray, w => Assert.Equal(DBNull.Value, w));

            var bericht = new List<string>();
            WasserpreisTraegerkarteSchema.Laufergebnis e = WasserpreisTraegerkarteSchema.Lauf(bericht);
            Assert.Equal((0, 0, 0, 0, 0, 1), (e.TraegerAngelegt, e.Uebernommen, e.Verworfen, e.Behalten, e.Entfernt, e.PreismodellAngelegt));
            Assert.Equal(1, e.Handgriffe);
            Assert.Contains(bericht, z => z.Contains("pricing_model", StringComparison.Ordinal));
            Assert.True(WasserpreisTraegerkarteSchema.Vollstaendig());
            DataTable t = DataRepository.GetDataTable(
                "SELECT name, has_hi, has_hs, has_powerprice, has_density FROM pricing_model WHERE code = 'WATER'");
            Assert.Equal(1, t.Rows.Count);
            Assert.Equal("Wasser", Convert.ToString(t.Rows[0][0], CultureInfo.InvariantCulture));
            for (int i = 1; i <= 4; i++) Assert.Equal(0L, Convert.ToInt64(t.Rows[0][i], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Applikation WHERE Kostenkatalog_Geaendert IS NOT NULL"));
            // Der Mapper der Trägerliste liest null wie 0: die Flaggen bleiben „nein“.
            DataRow mit = DataRepository.GetDataTable(FLAGGEN).Rows[0];
            Assert.All(mit.ItemArray, w => Assert.Equal(0L, Convert.ToInt64(w, CultureInfo.InvariantCulture)));

            Assert.Equal(0, WasserpreisTraegerkarteSchema.Lauf(null).Handgriffe);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM pricing_model WHERE code = 'WATER'"));
        }

        [Fact]
        public void Regel_c_haengt_nicht_an_der_Reihenfolge_der_Zeilen()
        {
            var dt = new DataTable { Locale = CultureInfo.InvariantCulture };
            dt.Columns.Add("ID", typeof(long));
            dt.Columns.Add("ID_Projekt", typeof(long));
            dt.Columns.Add("Preis", typeof(double));
            dt.Rows.Add(9L, 1L, 4.0);
            dt.Rows.Add(5L, 1L, 3.0);
            dt.Rows.Add(7L, 1L, 3.0);
            dt.Rows.Add(2L, 2L, 1.0);
            List<WasserpreisTraegerkarteSchema.Projektpreis> p = WasserpreisTraegerkarteSchema.Projektpreise(dt);
            Assert.Equal(2, p.Count);
            Assert.Equal((5L, 3.0), (p[0].IdAnlage, p[0].Preis));
            Assert.Equal(new[] { 9L }, p[0].Verworfen.Select(v => v.IdAnlage));
            Assert.Null(WasserpreisTraegerkarteSchema.HinweisVerworfen(p[1]));
        }

        // =================================================================================
        // Paketanhebung
        // =================================================================================

        [Fact]
        public void Paketanhebung_uebergibt_den_Preis_eines_aelteren_Pakets_an_die_Traegerkarte()
        {
            if (!_db.Vorhanden) return;
            long traeger = WasserpreisTraegerkarteSchema.TraegerId().Value;
            var baum = Baum();
            List<string> bericht = Paketanhebung.Anheben(BedarfsprofilEingabeartSchema.SCHRITT, new[] { baum }, null,
                                                         out List<List<string>> neu);

            Assert.Contains(WasserpreisTraegerkarteSchema.TAB_EINSTELLUNG, neu[0]);
            Dictionary<string, JsonElement> z = Assert.Single(baum[WasserpreisTraegerkarteSchema.TAB_EINSTELLUNG]);
            Assert.Equal((5L, traeger, 2.5), (z["ID_Projekt"].GetInt64(), z[WasserpreisTraegerkarteSchema.SPALTE_TRAEGER].GetInt64(),
                                               z[WasserpreisTraegerkarteSchema.SPALTE_PREIS].GetDouble()));
            Assert.Contains(bericht, b => b.Contains("Schritt " + WasserpreisTraegerkarteSchema.SCHRITT, StringComparison.Ordinal) &&
                                          b.Contains("verworfen", StringComparison.Ordinal));

            // Ein Paket vom Zielstand hebt nichts.
            var heutig = Baum();
            Assert.Empty(Paketanhebung.Anheben(WasserpreisTraegerkarteSchema.SCHRITT, new[] { heutig }, null, out _));
            Assert.False(heutig.ContainsKey(WasserpreisTraegerkarteSchema.TAB_EINSTELLUNG));
        }

        [Fact]
        public void Paketanhebung_nennt_den_Preis_wenn_der_Paketkatalog_dieselbe_Traeger_Id_fuehrt()
        {
            if (!_db.Vorhanden) return;
            long traeger = WasserpreisTraegerkarteSchema.TraegerId().Value;
            var baum = Baum();
            var katalog = new Dictionary<string, List<Dictionary<string, JsonElement>>>
            {
                [WasserpreisTraegerkarteSchema.TAB_TRAEGER] = new List<Dictionary<string, JsonElement>>
                {
                    Paketzeile(("id", traeger), ("name", "Erdgas fremd"))
                }
            };
            List<string> bericht = Paketanhebung.Anheben(BedarfsprofilEingabeartSchema.SCHRITT, new[] { baum }, katalog,
                                                         out List<List<string>> neu);
            Assert.DoesNotContain(WasserpreisTraegerkarteSchema.TAB_EINSTELLUNG, neu[0]);
            Assert.Contains(bericht, b => b.Contains("2.5", StringComparison.Ordinal));
        }

        // =================================================================================
        // Handwerkszeug
        // =================================================================================

        /// <summary>Ein Projektbaum eines älteren Pakets: Projekt 5, zwei Anlagen mit verschiedenem Wasserpreis.</summary>
        private static Dictionary<string, List<Dictionary<string, JsonElement>>> Baum() =>
            new Dictionary<string, List<Dictionary<string, JsonElement>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tab_Projekt"] = new List<Dictionary<string, JsonElement>> { Paketzeile(("ID", 5), ("Projektname", "Alt")) },
                [TAB] = new List<Dictionary<string, JsonElement>>
                {
                    Paketzeile(("ID", 12), ("ID_Projekt", 5), (SPALTE, 4.0)),
                    Paketzeile(("ID", 11), ("ID_Projekt", 5), (SPALTE, 2.5)),
                    Paketzeile(("ID", 13), ("ID_Projekt", 5), (SPALTE, null)),
                }
            };

        private static void SpalteWiederAnlegen() =>
            DataRepository.ExecuteNonQuery("ALTER TABLE \"" + TAB + "\" ADD COLUMN \"" + SPALTE + "\" REAL CHECK (\"" + SPALTE +
                                           "\" IS NULL OR \"" + SPALTE + "\" >= 0)");

        private static void Setzen(long anlage, double preis) =>
            DataRepository.ExecuteNonQuery("UPDATE \"" + TAB + "\" SET \"" + SPALTE + "\" = ? WHERE ID = ?",
                                           new DbParam("@w", preis), new DbParam("@id", anlage));

        private static List<long> Anlagen(int projekt)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? ORDER BY ID",
                                                       new DbParam("@p", projekt));
            return dt.Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r[0], CultureInfo.InvariantCulture)).ToList();
        }

        private static double Kartenpreis(int projekt) => Convert.ToDouble(DataRepository.ExecuteScalar(
            "SELECT custom_price_work FROM energy_project_settings WHERE ID_Projekt = ? AND \"ID_Energieträger\" = ?",
            new DbParam("@p", projekt), new DbParam("@t", WasserpreisTraegerkarteSchema.TraegerId().Value)), CultureInfo.InvariantCulture);

        private static Dictionary<string, JsonElement> Paketzeile(params (string Spalte, object Wert)[] werte)
        {
            var z = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach ((string s, object w) in werte) z[s] = JsonSerializer.SerializeToElement(w);
            return z;
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);
    }
}
