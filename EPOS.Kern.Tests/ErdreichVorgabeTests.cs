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
    /// <b>EV1 — der wirksame U-Wert der Bodenplatte als Vorgabe</b> (E65, <see cref="ErdreichVorgabeSchema"/>) und der
    /// Laufhinweis bei leerer Aufheizreserve (E64, <c>SIMENG_AUFH_RESERVE_VORGABE</c>).
    ///
    /// <para><b>Geprüft wird:</b> Nummer, Paketanhebung und Sicht; die Prüfklausel an einer STRICT-Tabelle; der Stand
    /// der Testdatenbank und die Rundreise 179 → 180; die Rechnung mit Vorgabe (U_g = Vorgabe, kein B′, Quelle
    /// VORGABE) und ohne (unverändert nach DIN EN ISO 13370); der Leser der Sicht, das Projektduplikat und der
    /// Projekttransfer (Katalog → Projekt, OK, Speichern unter und Hülle und Zonen hält
    /// <see cref="GebaeudeRundlaufTests"/>); der Reservehinweis und die Herleitungszeile.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErdreichVorgabeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Ein Projekt mit genau einem Gebäude (wie in den Schematests der Anlagenkopplung).</summary>
        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_folgt_lueckenlos_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(PufferAuslegungErgaenzungSchema.SCHRITT + 1, ErdreichVorgabeSchema.SCHRITT);
            Assert.Equal(180, ErdreichVorgabeSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= ErdreichVorgabeSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == ErdreichVorgabeSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal(new[] { "Tab_Gebaeude.Erdreich_U_Wirksam", "Tab_Gebaeude_STAMM.Erdreich_U_Wirksam" },
                         ErdreichVorgabeSchema.SPALTEN.Select(x => x.Tabelle + "." + x.Spalte).ToArray());
        }

        [Fact]
        public void Der_neunte_Sichtneubau_haengt_die_Vorgabe_hinter_die_manuelle_Aufheizzeit()
        {
            Assert.Equal(104, GebaeudeSchema.SICHT_ERDREICH_VORGABE.Length);
            Assert.Equal("Erdreich_U_Wirksam", GebaeudeSchema.SICHT_ERDREICH_VORGABE[103]);
            Assert.Equal(GebaeudeSchema.SICHT_AUFHEIZ_MANUELL, GebaeudeSchema.SICHT_ERDREICH_VORGABE.Take(103));
            Assert.Equal(GebaeudeSchema.SICHT_ERDREICH_VORGABE, GebaeudeSchema.SICHT_AKTUELL);
            Assert.Equal(GebaeudeSchema.SQL_VIEW_ERDREICH_VORGABE, GebaeudeSchema.SQL_VIEW_AKTUELL);
            Assert.Contains("Tab_Gebaeude.Erdreich_U_Wirksam", GebaeudeSchema.SQL_VIEW_AKTUELL, StringComparison.Ordinal);
        }

        /// <summary>Die Prüfklausel an einer STRICT-Tabelle: NULL und positive Werte ja, 0 und negative nein.</summary>
        [Fact]
        public void Die_Pruefklausel_verweigert_null_und_negative_Werte()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            Ausfuehren(c, ErdreichVorgabeSchema.Anlegen(("T", ErdreichVorgabeSchema.SPALTE, ErdreichVorgabeSchema.TYP)));
            foreach (string gut in new[] { "NULL", "0.25", "1e-6", "5" })
                Assert.False(Wirft(c, "UPDATE T SET Erdreich_U_Wirksam = " + gut), gut);
            foreach (string schlecht in new[] { "0", "0.0", "-0.1", "-5", "'abc'" })
                Assert.True(Wirft(c, "UPDATE T SET Erdreich_U_Wirksam = " + schlecht), schlecht);
        }

        // =============================================================================
        //  Teil 2 - die Rechnung
        // =============================================================================

        /// <summary>Mit Vorgabe: U_g = Vorgabe für jeden Boden am Erdreich, B′ NaN, Quelle Vorgabe, Text VORGABE.</summary>
        [Fact]
        public void Die_Vorgabe_ersetzt_U_g_und_rechnet_kein_B()
        {
            var u = new double[1];
            Erdreichkennwerte k = Erdreichwiderstand.Bauteilsatz(new[] { (100.0, 180.0, 0.35) }, 100.0, 40.0, u, 0.2);
            Assert.Equal(Erdreichumfangsquelle.Vorgabe, k.Quelle);
            Assert.Equal(Erdreichkennwerte.QUELLE_VORGABE, k.QuelleText);
            Assert.True(double.IsNaN(k.B_M));
            Assert.Equal(0.2, k.Ug_WM2K, 12);
            Assert.Equal(1.0 / 0.2 - 1.0 / 0.35, k.Rg_M2KW, 12);
            Assert.Equal(0.2, u[0], 12);
            Assert.Equal(0.2, k.UWirksam_WM2K, 12);

            // Eine Vorgabe über dem eingetragenen U-Wert hebt ihn nie an (R_g = 0).
            Erdreichwiderstand.Bauteilsatz(new[] { (100.0, 180.0, 0.35) }, 100.0, 40.0, u, 0.5);
            Assert.Equal(0.35, u[0], 12);
        }

        /// <summary>Ohne Vorgabe (NaN, 0, ∞) rechnet der Satz wörtlich wie vorher nach DIN EN ISO 13370.</summary>
        [Fact]
        public void Ohne_Vorgabe_rechnet_der_Satz_unveraendert()
        {
            var satz = new[] { (1000.0, 180.0, 1.0), (300.0, 90.0, 0.5) };
            var bestand = new double[2];
            Erdreichkennwerte soll = Erdreichwiderstand.Bauteilsatz(satz, 0.0, 0.0, bestand);
            foreach (double keine in new[] { double.NaN, 0.0, -1.0, double.PositiveInfinity })
            {
                var u = new double[2];
                Erdreichkennwerte ist = Erdreichwiderstand.Bauteilsatz(satz, 0.0, 0.0, u, keine);
                Assert.Equal(soll, ist);
                Assert.Equal(bestand, u);
            }
            Assert.Equal(Erdreichumfangsquelle.Quadrat, soll.Quelle);
        }

        /// <summary>
        /// Klassenweg, Lauf und Export mit Vorgabe am Probegebäude: U_g = Vorgabe, Quelle VORGABE im Export, kein
        /// <c>Erdreich_B</c>, und der Hinweis auf den Umfang entfällt.
        /// </summary>
        [Fact]
        public void Klassenweg_Lauf_und_Export_mit_Vorgabe()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Erdreich_U_Wirksam = 0.3;
            SolardatenModel[] klima = Vdi6007Probe.Klima(h => 5.0 + 10.0 * Math.Sin(2.0 * Math.PI * (h - 2400) / 8760.0));
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(g, klima);
            Assert.NotNull(e.Erdreich);
            Assert.Equal(Erdreichumfangsquelle.Vorgabe, e.Erdreich.Quelle);
            Assert.Equal(0.3, e.Erdreich.Ug_WM2K, 12);
            Assert.Equal(0.3, e.Erdreich.UWirksam_WM2K, 12);
            Assert.Equal(0.8, e.U_Grund);

            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, g.ID_Gebaeude);
            GebaeudeExportsatz satz = GebaeudeErgebnisexport.Satz(r);
            Assert.DoesNotContain(satz.Skalare, s => s.Key == "Geb[0].Erdreich_B");
            Assert.Equal(0.3, satz.Skalare.Single(s => s.Key == "Geb[0].Erdreich_Ug").Value, 12);
            Assert.Equal("Vorgabe", satz.Texte.Single(s => s.Key == "Geb[0].Erdreich_Umfangsquelle").Value);

            SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisErdreichumfang(e.Erdreich, "Probegebäude");
            Assert.Empty(SimulationProtokoll.Aktuell.Hinweise);

            // NULL rechnet wie vorher: Quadrat, B′ gerechnet.
            ProjektGebaeudeModel ohne = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang eo = Vdi6007Probe.Eingang(ohne, klima);
            Assert.Equal(Erdreichumfangsquelle.Quadrat, eo.Erdreich.Quelle);
            Assert.Equal(88.0 / (0.5 * 4.0 * Math.Sqrt(88.0)), eo.Erdreich.B_M, 12);
        }

        // =============================================================================
        //  Teil 3 - die Testdatenbank, die Rundreise und die Kopierwege
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= ErdreichVorgabeSchema.SCHRITT);
            Assert.True(ErdreichVorgabeSchema.Vollstaendig());
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL,
                         Convert.ToString(DataRepository.ExecuteScalar("SELECT sql FROM sqlite_master WHERE type = 'view' AND name = ?",
                                                                       new DbParam("?", GebaeudeSchema.VIEW)), CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Erdreich_U_Wirksam IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Erdreich_U_Wirksam IS NOT NULL"));
            Assert.True(Wirft("UPDATE Tab_Gebaeude SET Erdreich_U_Wirksam = 0 WHERE ID = " + GEBAEUDE));
            Assert.True(Wirft("UPDATE Tab_Gebaeude_STAMM SET Erdreich_U_Wirksam = -0.1 WHERE ID = (SELECT MIN(ID) FROM Tab_Gebaeude_STAMM)"));
        }

        /// <summary>Rundreise 179 → 180: aus dem Stand davor legt der Schritt beide Spalten und die Sicht an; zweimal = nichts.</summary>
        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP);
            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Gebaeude DROP COLUMN Erdreich_U_Wirksam");
            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Gebaeude_STAMM DROP COLUMN Erdreich_U_Wirksam");
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_AUFHEIZ_MANUELL);
            long zeilen = Zahl("SELECT COUNT(*) FROM Tab_Gebaeude");
            Assert.False(ErdreichVorgabeSchema.Vollstaendig());
            Assert.True(AufheizManuellSchema.SichtSteht());

            var bericht = new List<string>();
            Assert.Equal(2, ErdreichVorgabeSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("104 Spalten", StringComparison.Ordinal));
            Assert.True(ErdreichVorgabeSchema.Vollstaendig());
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            Assert.Equal(zeilen, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Erdreich_U_Wirksam IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            Assert.Equal(0, ErdreichVorgabeSchema.Ausfuehren(null));
        }

        /// <summary>Der Leser der Sicht liefert den Wert, NULL bleibt null.</summary>
        [Fact]
        public void Der_Leser_der_Sicht_liefert_die_Vorgabe()
        {
            if (!_db.Vorhanden) return;
            Assert.Null(Projektgebaeude().Erdreich_U_Wirksam);
            Setzen(0.27);
            Assert.Equal(0.27, Projektgebaeude().Erdreich_U_Wirksam);
            Assert.Equal(0.27, GebaeudeStammCtrl.LiesProjektkopie(GEBAEUDE).Erdreich_U_Wirksam);
        }

        /// <summary>Das Projektduplikat (derselbe Weg wie „Variante anlegen") trägt den Wert.</summary>
        [Fact]
        public void Projektduplikat_traegt_die_Vorgabe()
        {
            if (!_db.Vorhanden) return;
            Setzen(0.27);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " EV1");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(0.27, Wert(neu));
        }

        /// <summary>Der Projekttransfer (Export und Import eines Pakets) trägt den Wert über die Paketgrenze.</summary>
        [Fact]
        public void Projekttransfer_traegt_die_Vorgabe()
        {
            if (!_db.Vorhanden) return;
            Setzen(0.27);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-ev1-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer EV1", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Assert.Equal(0.27, Wert(neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        /// <summary>Das Katalogpaket (Katalogabgleich) führt die Spalte des Gebäudekatalogs.</summary>
        [Fact]
        public void Das_Katalogpaket_fuehrt_die_Spalte()
        {
            string quelle = File.ReadAllText(Path.Combine(Repowurzel(), "EPOS.Kern", "Allgemein", "Katalog", "Katalogfassung.cs"));
            Assert.Contains("\"Energiestandard\", \"Erdreich_U_Wirksam\"", quelle, StringComparison.Ordinal);
        }

        // =============================================================================
        //  Teil 4 - der Reservehinweis (E64) und die Herleitungszeile
        // =============================================================================

        [Fact]
        public void Leere_Reserve_meldet_den_Hinweis_einmal()
        {
            using var kultur = new Kulturvorrichtung();
            SimulationProtokoll.NeuStarten();
            var leer = new Aufheizvorgabe(true, null, null, null, null);
            Vdi6007Rechenweg.HinweisReserveVorgabe(leer);
            Vdi6007Rechenweg.HinweisReserveVorgabe(leer);
            string hinweis = Assert.Single(SimulationProtokoll.Aktuell.Hinweise);
            Assert.Contains("Aufheizreserve nicht vorgegeben; es gelten 20 %.", hinweis, StringComparison.Ordinal);
        }

        [Fact]
        public void Gesetzte_Reserve_oder_ausgeschaltete_Optimierung_melden_nichts()
        {
            using var kultur = new Kulturvorrichtung();
            SimulationProtokoll.NeuStarten();
            Vdi6007Rechenweg.HinweisReserveVorgabe(new Aufheizvorgabe(true, null, null, 0.2, null));
            Vdi6007Rechenweg.HinweisReserveVorgabe(new Aufheizvorgabe(false, null, null, null, null));
            Vdi6007Rechenweg.HinweisReserveVorgabe(null);
            Assert.Empty(SimulationProtokoll.Aktuell.Hinweise);
        }

        [Fact]
        public void Die_Herleitungszeile_nennt_die_Quelle_der_Reserve()
        {
            using var kultur = new Kulturvorrichtung();
            CultureInfo k = CultureInfo.CurrentCulture;
            AufheizHerleitungsdaten Daten(string quelle, double? rho, bool vorgabe) =>
                new AufheizHerleitungsdaten("Hotel", DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, 5,
                                            -9.26, 34.64, 34.64, 1.0, quelle, ReserveAnteil: rho, ReserveVorgabe: vorgabe);
            Assert.EndsWith("Zielleistung · Reserve 20 % (Vorgabe)",
                            AufheizHerleitungszeile.Zeile(Daten(DbWerte.AUFHEIZ_QUELLE_ZIEL, 0.2, true), k), StringComparison.Ordinal);
            string eingabe = AufheizHerleitungszeile.Zeile(Daten(DbWerte.AUFHEIZ_QUELLE_ZIEL, 0.15, false), k);
            Assert.EndsWith("Zielleistung · Reserve 15 %", eingabe, StringComparison.Ordinal);
            Assert.DoesNotContain("Reserve", AufheizHerleitungszeile.Zeile(Daten(DbWerte.AUFHEIZ_QUELLE_GRENZE, 0.2, true), k),
                                  StringComparison.Ordinal);
            Assert.DoesNotContain("Reserve", AufheizHerleitungszeile.Zeile(Daten(DbWerte.AUFHEIZ_QUELLE_ZIEL, null, false), k),
                                  StringComparison.Ordinal);
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        private static void Setzen(double wert)
            => Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Erdreich_U_Wirksam = ? WHERE ID = ?",
                                                     new DbParam("@w", wert), new DbParam("@id", GEBAEUDE)));

        private static double? Wert(int idProjekt)
        {
            object w = DataRepository.ExecuteScalar("SELECT Erdreich_U_Wirksam FROM Tab_Gebaeude WHERE ID_Projekt = ?",
                                                    new DbParam("?", idProjekt));
            return w == null || w == DBNull.Value ? null : Convert.ToDouble(w, CultureInfo.InvariantCulture);
        }

        private static ProjektGebaeudeModel Projektgebaeude()
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            return ctrl.items.Single(x => x.ID_Gebaeude == GEBAEUDE);
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static bool Wirft(string sql)
        {
            try { return !DataRepository.ExecuteSQL(sql); }
            catch (Exception) { return true; }
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

        private static string Repowurzel()
        {
            string d = AppContext.BaseDirectory;
            while (d != null && !File.Exists(Path.Combine(d, "WP-Plan.sln"))) d = Path.GetDirectoryName(d);
            Assert.NotNull(d);
            return d;
        }
        // ---------------------------------------------------------------------------------
        //  Die Erdreichauskunft der Oberfläche (Teil B): GebaeudeKatalogHuelle.Erdreichweg
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Die Auskunft der Hülle rechnet wie der Klassenweg: ohne Vorgabe B′ = A/(0,5·P) mit dem Quadrat-Umfang
        /// (A 100 m², P 40 m ⇒ B′ 5 m) und U_g nach 9.1; mit Vorgabe kein B′ und U_g = Vorgabe.
        /// </summary>
        [Fact]
        public void Die_Auskunft_der_Huelle_nennt_B_Strich_und_U_g_oder_die_Vorgabe()
        {
            var d = new EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogDaten { Grundflaeche = 100, UWertGrundflaeche = 0.35 };
            var weg = GebaeudeKatalogHuelle.Erdreichweg();

            EPOS.UI.Dialoge.Bedarf.ErdreichAuskunftDaten rechnung = weg(d);
            Assert.False(rechnung.Vorgabe);
            Assert.Equal(5.0, rechnung.BStrichM!.Value, 9);
            Erdreichkennwerte k = Erdreichwiderstand.Bauteilsatz(new[] { (100.0, 180.0, 0.35) }, 100.0, 0.0, new double[1]);
            Assert.Equal(k.Ug_WM2K, rechnung.UgWM2K, 12);

            d.ErdreichUWirksam = 0.3;
            EPOS.UI.Dialoge.Bedarf.ErdreichAuskunftDaten vorgabe = weg(d);
            Assert.True(vorgabe.Vorgabe);
            Assert.Null(vorgabe.BStrichM);
            Assert.Equal(0.3, vorgabe.UgWM2K, 12);
        }

        /// <summary>Keine Auskunft bei Randbedingung Keller oder Außenluft, ohne Grundfläche oder ohne U-Wert.</summary>
        [Fact]
        public void Ohne_Erdreichkorrektur_gibt_die_Huelle_keine_Auskunft()
        {
            var weg = GebaeudeKatalogHuelle.Erdreichweg();
            Assert.Null(weg(new EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogDaten
                { Grundflaeche = 100, UWertGrundflaeche = 0.35, GrundflaecheRandbedingung = DbWerte.GRUND_KELLER }));
            Assert.Null(weg(new EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogDaten
                { Grundflaeche = 100, UWertGrundflaeche = 0.35, GrundflaecheRandbedingung = DbWerte.GRUND_AUSSENLUFT }));
            Assert.Null(weg(new EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogDaten { UWertGrundflaeche = 0.35 }));
            Assert.Null(weg(new EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogDaten { Grundflaeche = 100 }));
            Assert.NotNull(weg(new EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogDaten
                { Grundflaeche = 100, UWertGrundflaeche = 0.35, GrundflaecheRandbedingung = DbWerte.GRUND_ERDREICH }));
        }
    }
}
