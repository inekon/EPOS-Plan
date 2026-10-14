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
    /// Der Schemaschritt der <b>Änderungsstempel für Kosten, Preise und Kostenkatalog</b> (Folge von
    /// #637; Nummer bei <see cref="KostenStempelSchema"/>) und seine Trigger.
    ///
    /// <para><b>Geprüft wird:</b> die Nummer (lückenlos hinter 158) und ihr Eintrag im Register der
    /// Paketanhebung; die Liste der Trigger; der Stand der Testdatenbank (Spalten, alle Trigger,
    /// Stempel leer); der Schritt aus dem Stand davor, wiederholbar; je gestempelte Tabelle, dass
    /// Anlegen, Ändern und Löschen den richtigen Stempel setzen — und nur ihn; die Spaltenliste von
    /// <c>Tab_Energieanlagen</c>; <c>Tab_Projekt</c> und <c>Tab_Applikation</c> ohne Rekursion; die
    /// ausgenommenen Tabellen stempeln nicht; Migration, Werkzeug und Testkopie führen den Schritt
    /// aus derselben Quelle, und die Repo-Datei trägt ihn.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — die Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KostenStempelSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die Vergleichsgruppe 1019 mit den Varianten 1023 („Test1“) und 1024 („Test2“).</summary>
        private const int STAMM = 1019;
        private const int VARIANTE = 1023;

        // =============================================================================
        //  Teil 1 - Definitionen
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf 158 (Brennwertkennzeichen); der Zielstand reicht bis zu ihr.</summary>
        [Fact]
        public void Die_Nummer_folgt_lueckenlos_auf_158()
        {
            Assert.Equal(KesselBrennwertNachzug.SCHRITT + 1, KostenStempelSchema.SCHRITT);
            Assert.Equal(159, KostenStempelSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KostenStempelSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + KostenStempelSchema.SCHRITT + ".");
        }

        /// <summary>Das Register der Paketanhebung führt den Schritt als reines DDL — ein Paket trägt keine Trigger.</summary>
        [Fact]
        public void Das_Register_der_Paketanhebung_fuehrt_den_Schritt_als_DDL()
        {
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == KostenStempelSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.False(string.IsNullOrWhiteSpace(s.Text));
        }

        /// <summary>
        /// Die Liste: 63 Trigger mit eindeutigen Namen, jeder als <c>CREATE TRIGGER IF NOT EXISTS</c>;
        /// die Projekt- und Katalogtabellen je mit Anlegen, Ändern und Löschen, die Sonderfälle wie
        /// geplant.
        /// </summary>
        [Fact]
        public void Die_Triggerliste_ist_vollstaendig_und_eindeutig()
        {
            IReadOnlyList<KostenStempelSchema.Stempeltrigger> alle = KostenStempelSchema.Trigger;
            Assert.Equal(63, alle.Count);
            Assert.Equal(alle.Count, alle.Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            foreach (KostenStempelSchema.Stempeltrigger t in alle)
            {
                Assert.StartsWith("CREATE TRIGGER IF NOT EXISTS \"" + t.Name + "\" AFTER " + t.Ereignis, t.Sql);
                Assert.Contains(" ON \"" + t.Tabelle + "\"", t.Sql);
                Assert.Contains(KostenStempelSchema.JETZT, t.Sql);
            }

            var dreifach = new List<string> { "Tab_Energieanlagen", "Tab_Variante", "Tab_Preisreihe", "Tab_PreisreiheDaten" };
            dreifach.AddRange(KostenStempelSchema.PROJEKTTABELLEN.Select(p => p.Key));
            dreifach.AddRange(KostenStempelSchema.KATALOGTABELLEN);
            foreach (string tabelle in dreifach)
                Assert.Equal(new[] { "DELETE", "INSERT", "UPDATE" },
                             alle.Where(t => t.Tabelle == tabelle).Select(t => t.Ereignis).OrderBy(e => e).ToArray());

            // Nur die Kostenspalten der Anlage, nur der Modus am Projekt und an der Applikation,
            // nur der Äquivalenzfaktor an der Emissionsart.
            Assert.Equal(KostenStempelSchema.SPALTEN_ENERGIEANLAGEN,
                         alle.Single(t => t.Tabelle == "Tab_Energieanlagen" && t.Ereignis == "UPDATE").Spalten);
            Assert.Equal(new[] { "Emission_Berechnungsmodus" }, alle.Single(t => t.Tabelle == "Tab_Projekt").Spalten);
            Assert.Equal(new[] { "Emission_Berechnungsmodus" }, alle.Single(t => t.Tabelle == "Tab_Applikation").Spalten);
            Assert.Equal(new[] { "co2_aequivalent" }, alle.Single(t => t.Tabelle == "emissionsart").Spalten);

            Assert.Equal(31, alle.Count(t => t.Art == KostenStempelSchema.Stempelart.Projekt));
            Assert.Equal(32, alle.Count(t => t.Art == KostenStempelSchema.Stempelart.Katalog));
        }

        /// <summary>Die bewusst ausgenommenen Tabellen tragen keinen Trigger dieses Schritts.</summary>
        [Theory]
        [InlineData("Tab_Kraftwerkspark")]
        [InlineData("Tab_ProjektWirkung")]
        [InlineData("Tab_KostenVorlage")]
        [InlineData("Tab_KostenVorlagePosition")]
        [InlineData("Tab_KostenGruppenKatalog")]
        [InlineData("Tab_ErgebnisWirtschaftlichkeit")]
        [InlineData("Tab_Einstellungen")]
        [InlineData("Tab_Gebaeude")]
        [InlineData("Tab_Heizkessel")]
        public void Ausgenommene_Tabellen_tragen_keinen_Trigger(string tabelle)
        {
            Assert.DoesNotContain(KostenStempelSchema.Trigger, t => t.Tabelle == tabelle);
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und der Schritt aus dem Stand davor
        // =============================================================================

        /// <summary>Spalten und Trigger stehen, die Stempel sind leer, die beiden Tabellen bleiben STRICT.</summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Zielstand_mit_leeren_Stempeln()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= KostenStempelSchema.SCHRITT);
            Assert.True(KostenStempelSchema.Vollstaendig());
            Assert.Empty(KostenStempelSchema.Anweisungen);
            Assert.Empty(KostenStempelSchema.FehlendeTrigger());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE Kosten_Geaendert IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Applikation WHERE Kostenkatalog_Geaendert IS NOT NULL"));

            foreach (string t in new[] { "Tab_Projekt", "Tab_Applikation" })
            {
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("?", t)),
                    CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", ddl.TrimEnd());
            }
        }

        /// <summary>
        /// Der Schritt aus dem Stand VOR ihm (158): Trigger und Spalten werden entfernt; der Schritt
        /// legt alles an, die Zeilen bleiben, ein zweiter Lauf tut nichts.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            List<string> vorher = Bestand();
            TriggerUndSpaltenEntfernen();
            Assert.False(KostenStempelSchema.Vollstaendig());
            Assert.Equal(63, KostenStempelSchema.FehlendeTrigger().Count);
            Assert.Equal(65, KostenStempelSchema.Anweisungen.Count());
            Assert.Equal(63, KostenStempelSchema.Anweisungen.Count(KostenStempelSchema.IstTriggeranweisung));

            var bericht = new List<string>();
            Assert.Equal(65, KostenStempelSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z == "Tab_Projekt.Kosten_Geaendert anlegen");
            Assert.Contains(bericht, z => z == "Tab_Applikation.Kostenkatalog_Geaendert anlegen");
            Assert.True(KostenStempelSchema.Vollstaendig());
            Assert.Equal(vorher, Bestand());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE Kosten_Geaendert IS NOT NULL"));
            Assert.Equal(63L, Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND " +
                                   "(name LIKE 'trg_Kostenstempel_%' OR name LIKE 'trg_Katalogstempel_%')"));

            var zweiter = new List<string>();
            Assert.Equal(0, KostenStempelSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("vorhanden"));
        }

        /// <summary>Ein fehlender einzelner Trigger wird nachgelegt — und nur er.</summary>
        [Fact]
        public void Ein_fehlender_Trigger_wird_allein_nachgelegt()
        {
            if (!_db.Vorhanden) return;

            Schreibe("DROP TRIGGER \"trg_Kostenstempel_Tab_ProjektWerte_U\"");
            Assert.Equal(new[] { "trg_Kostenstempel_Tab_ProjektWerte_U" }, KostenStempelSchema.FehlendeTrigger());
            Assert.Single(KostenStempelSchema.Anweisungen);
            Assert.Equal(1, KostenStempelSchema.Ausfuehren(null));
            Assert.True(KostenStempelSchema.Vollstaendig());
        }

        // =============================================================================
        //  Teil 3 - die Trigger
        // =============================================================================

        /// <summary>Je Projekttabelle: eine Zeile anlegen, ändern, löschen — jedes Mal genau ihr Projekt.</summary>
        public static IEnumerable<object[]> Projektfaelle()
        {
            yield return new object[] { "Tab_ProjektWerte", "ProjektID", 1007,
                "INSERT INTO \"Tab_ProjektWerte\" (\"ProjektID\", \"EingegebenerWert\") VALUES (?, 1.0)" };
            yield return new object[] { "Tab_ProjektWirtschaftlichkeit", "ID_Projekt", 1007,
                "INSERT INTO \"Tab_ProjektWirtschaftlichkeit\" (\"ID_Projekt\", \"Zinssatz\") VALUES (?, 3.0)" };
            yield return new object[] { "Tab_ProjektTarif", "ID_Projekt", 1030,
                "INSERT INTO \"Tab_ProjektTarif\" (\"ID_Projekt\", \"Aktiv\") VALUES (?, 0)" };
            yield return new object[] { "energy_project_settings", "ID_Projekt", 1045,
                "INSERT INTO \"energy_project_settings\" (\"ID_Projekt\", \"ID_Energieträger\") VALUES (?, " +
                "(SELECT MIN(c.\"id\") FROM \"energy_carrier\" AS c WHERE c.\"id\" NOT IN " +
                "(SELECT e.\"ID_Energieträger\" FROM \"energy_project_settings\" AS e WHERE e.\"ID_Projekt\" = 1045)))" };
            yield return new object[] { "energy_price", "ID_Projekt", 1017,
                "INSERT INTO \"energy_price\" (\"ID_Projekt\", \"carrier_id\", \"valid_from\", \"arbeitspreis\") " +
                "VALUES (?, (SELECT MIN(\"id\") FROM \"energy_carrier\"), '2031-01-01 00:00:00', 0.1)" };
            yield return new object[] { "Tab_ProjektPhotovoltaik", "ID_Projekt", 1030,
                "INSERT INTO \"Tab_ProjektPhotovoltaik\" (\"ID_Projekt\", \"Aktiv\") VALUES (?, 0)" };
        }

        [Theory]
        [MemberData(nameof(Projektfaelle))]
        public void Eine_Projekttabelle_stempelt_ihr_Projekt(string tabelle, string projektspalte, int projekt, string anlegen)
        {
            if (!_db.Vorhanden) return;
            Assert.Contains(KostenStempelSchema.PROJEKTTABELLEN, p => p.Key == tabelle && p.Value == projektspalte);

            StempelLeeren();
            int id = Einfuegen(anlegen, new DbParam("?", projekt));
            Assert.Equal(new[] { projekt }, GestempelteProjekte());
            Assert.False(KatalogGestempelt());

            StempelLeeren();
            Schreibe("UPDATE \"" + tabelle + "\" SET \"" + projektspalte + "\" = \"" + projektspalte + "\" WHERE rowid = ?",
                     new DbParam("?", id));
            Assert.Equal(new[] { projekt }, GestempelteProjekte());
            Assert.False(KatalogGestempelt());

            StempelLeeren();
            Schreibe("DELETE FROM \"" + tabelle + "\" WHERE rowid = ?", new DbParam("?", id));
            Assert.Equal(new[] { projekt }, GestempelteProjekte());
            Assert.False(KatalogGestempelt());
        }

        /// <summary>Eine Zeile, die ihr Projekt wechselt, stempelt beide Projekte.</summary>
        [Fact]
        public void Eine_Zeile_mit_neuem_Projekt_stempelt_altes_und_neues()
        {
            if (!_db.Vorhanden) return;

            StempelLeeren();
            Schreibe("UPDATE \"Tab_ProjektWerte\" SET \"ProjektID\" = 1018 WHERE \"ID\" = " +
                     "(SELECT MIN(\"ID\") FROM \"Tab_ProjektWerte\" WHERE \"ProjektID\" = 1007)");
            Assert.Equal(new[] { 1007, 1018 }, GestempelteProjekte());
        }

        /// <summary>
        /// Die Anlage: Anlegen und Löschen stempeln, eine Kostenspalte stempelt, eine technische
        /// Spalte nicht — auch nicht der Vorlauf, den der Anlagendialog mitschreibt.
        /// </summary>
        [Fact]
        public void Die_Anlage_stempelt_nur_ueber_ihre_Kostenspalten()
        {
            if (!_db.Vorhanden) return;
            int anlage = Ganz("SELECT MIN(\"ID\") FROM \"Tab_Energieanlagen\" WHERE \"ID_Projekt\" = 1030");

            StempelLeeren();
            Schreibe("UPDATE \"Tab_Energieanlagen\" SET \"Vorlauf\" = \"Vorlauf\", \"WQ_Temp\" = \"WQ_Temp\", " +
                     "\"Bezeichner\" = \"Bezeichner\" WHERE \"ID\" = ?", new DbParam("?", anlage));
            Assert.Empty(GestempelteProjekte());

            StempelLeeren();
            Schreibe("UPDATE \"Tab_Energieanlagen\" SET \"ID_Carrier\" = \"ID_Carrier\" WHERE \"ID\" = ?", new DbParam("?", anlage));
            Assert.Equal(new[] { 1030 }, GestempelteProjekte());

            StempelLeeren();
            int neu = Einfuegen("INSERT INTO \"Tab_Energieanlagen\" (\"ID_Projekt\", \"Bezeichner\") VALUES (?, 'Probe Stempel')",
                                new DbParam("?", 1030));
            Assert.Equal(new[] { 1030 }, GestempelteProjekte());

            StempelLeeren();
            Schreibe("DELETE FROM \"Tab_Energieanlagen\" WHERE \"ID\" = ?", new DbParam("?", neu));
            Assert.Equal(new[] { 1030 }, GestempelteProjekte());
            Assert.False(KatalogGestempelt());
        }

        /// <summary>Jede der siebzehn gelisteten Spalten stempelt für sich.</summary>
        public static IEnumerable<object[]> Anlagenspalten()
            => KostenStempelSchema.SPALTEN_ENERGIEANLAGEN.Select(s => new object[] { s });

        [Theory]
        [MemberData(nameof(Anlagenspalten))]
        public void Jede_Kostenspalte_der_Anlage_stempelt(string spalte)
        {
            if (!_db.Vorhanden) return;
            StempelLeeren();
            Schreibe("UPDATE \"Tab_Energieanlagen\" SET \"" + spalte + "\" = \"" + spalte + "\" WHERE \"ID\" = " +
                     "(SELECT MIN(\"ID\") FROM \"Tab_Energieanlagen\" WHERE \"ID_Projekt\" = 1046)");
            Assert.Equal(new[] { 1046 }, GestempelteProjekte());
        }

        /// <summary>
        /// <b>Die Wache der Spaltenliste:</b> Jede <c>KWKG_*</c>-Spalte der Anlage steht in
        /// <see cref="KostenStempelSchema.SPALTEN_ENERGIEANLAGEN"/>, und jede gelistete Spalte gibt es.
        /// Kommt eine KWKG-Spalte hinzu, wird diese Wache rot — die Liste und der Trigger (neuer
        /// Schritt: DROP und CREATE) ziehen nach.
        /// </summary>
        [Fact]
        public void Die_Spaltenliste_deckt_jede_KWKG_Spalte_der_Anlage()
        {
            if (!_db.Vorhanden) return;
            List<string> spalten = DataRepository.SpaltenVonTabelle("Tab_Energieanlagen");
            foreach (string s in KostenStempelSchema.SPALTEN_ENERGIEANLAGEN)
                Assert.Contains(s, spalten);
            foreach (string s in spalten.Where(s => s.StartsWith("KWKG_", StringComparison.Ordinal)))
                Assert.Contains(s, KostenStempelSchema.SPALTEN_ENERGIEANLAGEN);
        }

        /// <summary>Eine Variante stempelt sich UND ihren Stamm — beim Anlegen, Ändern und Löschen.</summary>
        [Fact]
        public void Eine_Variante_stempelt_Variante_und_Stamm()
        {
            if (!_db.Vorhanden) return;

            StempelLeeren();
            Schreibe("UPDATE \"Tab_Variante\" SET \"Variantenname\" = \"Variantenname\" WHERE \"ID_Projekt\" = ?",
                     new DbParam("?", VARIANTE));
            Assert.Equal(new[] { STAMM, VARIANTE }, GestempelteProjekte());

            StempelLeeren();
            Schreibe("DELETE FROM \"Tab_Variante\" WHERE \"ID_Projekt\" = ?", new DbParam("?", VARIANTE));
            Assert.Equal(new[] { STAMM, VARIANTE }, GestempelteProjekte());

            StempelLeeren();
            Einfuegen("INSERT INTO \"Tab_Variante\" (\"ID_Projekt\", \"ID_ProjektRef\", \"Variantenname\") VALUES (?, ?, 'Test1')",
                      new DbParam("?", VARIANTE), new DbParam("?", STAMM));
            Assert.Equal(new[] { STAMM, VARIANTE }, GestempelteProjekte());
            Assert.False(KatalogGestempelt());
        }

        /// <summary>
        /// Preisreihen: Eine Reihe mit Projekt stempelt ihr Projekt, Kopf wie Werte; eine Stammreihe
        /// (ohne Projekt, gilt überall) stempelt den Katalog. Das Löschen einer Projektreihe nimmt ihre
        /// Werte mit und stempelt allein das Projekt.
        /// </summary>
        [Fact]
        public void Preisreihen_stempeln_ihr_Projekt_oder_als_Stammreihe_den_Katalog()
        {
            if (!_db.Vorhanden) return;

            StempelLeeren();
            int reihe = Einfuegen("INSERT INTO \"Tab_Preisreihe\" (\"ID_Projekt\", \"Bezeichner\", \"Jahr\") VALUES (?, 'Probe', 2031)",
                                  new DbParam("?", 1030));
            Assert.Equal(new[] { 1030 }, GestempelteProjekte());
            Assert.False(KatalogGestempelt());

            StempelLeeren();
            Einfuegen("INSERT INTO \"Tab_PreisreiheDaten\" (\"ID_Preisreihe\", \"Wert\") VALUES (?, 1.5)", new DbParam("?", reihe));
            Assert.Equal(new[] { 1030 }, GestempelteProjekte());
            Assert.False(KatalogGestempelt());

            StempelLeeren();
            Schreibe("DELETE FROM \"Tab_Preisreihe\" WHERE \"ID\" = ?", new DbParam("?", reihe));
            Assert.Equal(new[] { 1030 }, GestempelteProjekte());
            Assert.False(KatalogGestempelt());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_PreisreiheDaten\" WHERE \"ID_Preisreihe\" = " +
                                  reihe.ToString(CultureInfo.InvariantCulture)));

            // Die Stammreihen der Testdatenbank (ID_Projekt leer)
            int stammreihe = Ganz("SELECT MIN(\"ID\") FROM \"Tab_Preisreihe\" WHERE \"ID_Projekt\" IS NULL");
            StempelLeeren();
            Schreibe("UPDATE \"Tab_Preisreihe\" SET \"Jahr\" = \"Jahr\" WHERE \"ID\" = ?", new DbParam("?", stammreihe));
            Assert.Empty(GestempelteProjekte());
            Assert.True(KatalogGestempelt());

            StempelLeeren();
            Schreibe("UPDATE \"Tab_PreisreiheDaten\" SET \"Wert\" = \"Wert\" WHERE \"ID\" = " +
                     "(SELECT MIN(\"ID\") FROM \"Tab_PreisreiheDaten\" WHERE \"ID_Preisreihe\" = ?)", new DbParam("?", stammreihe));
            Assert.Empty(GestempelteProjekte());
            Assert.True(KatalogGestempelt());
        }

        /// <summary>
        /// Das Projekt selbst: nur ein GEÄNDERTER Emissionsmodus stempelt; derselbe Wert, eine andere
        /// Spalte und das Setzen des Stempels selbst nicht. Keine Rekursion — auch nicht unter
        /// <c>PRAGMA recursive_triggers = ON</c> (sonst bräche SQLite mit „too many levels of trigger
        /// recursion" ab).
        /// </summary>
        [Fact]
        public void Das_Projekt_stempelt_nur_den_geaenderten_Emissionsmodus_ohne_Rekursion()
        {
            if (!_db.Vorhanden) return;

            StempelLeeren();
            Schreibe("UPDATE \"Tab_Projekt\" SET \"Emission_Berechnungsmodus\" = \"Emission_Berechnungsmodus\", " +
                     "\"Beschreibung\" = \"Beschreibung\", \"Aenderungsdatum\" = \"Aenderungsdatum\" WHERE \"ID\" = 1030");
            Assert.Empty(GestempelteProjekte());

            StempelLeeren();
            Schreibe("UPDATE \"Tab_Projekt\" SET \"Kosten_Geaendert\" = '2026-01-01 00:00:00' WHERE \"ID\" = 1030");
            Assert.Equal("2026-01-01 00:00:00", Text("SELECT \"Kosten_Geaendert\" FROM \"Tab_Projekt\" WHERE \"ID\" = 1030"));

            StempelLeeren();
            MitRekursion("UPDATE \"Tab_Projekt\" SET \"Emission_Berechnungsmodus\" = 'CO2E' WHERE \"ID\" = 1030");
            Assert.Equal(new[] { 1030 }, GestempelteProjekte());
            Assert.False(KatalogGestempelt());
        }

        /// <summary>Die Vorgabe des Emissionsmodus stempelt den Katalog — nur geändert, ohne Rekursion; der Projektwechsel nicht.</summary>
        [Fact]
        public void Die_Applikation_stempelt_nur_den_geaenderten_Emissionsmodus_ohne_Rekursion()
        {
            if (!_db.Vorhanden) return;

            StempelLeeren();
            Schreibe("UPDATE \"Tab_Applikation\" SET \"ID_Projekt\" = 1030, \"SchemaVersion\" = \"SchemaVersion\", " +
                     "\"Emission_Berechnungsmodus\" = \"Emission_Berechnungsmodus\"");
            Assert.False(KatalogGestempelt());

            StempelLeeren();
            MitRekursion("UPDATE \"Tab_Applikation\" SET \"Emission_Berechnungsmodus\" = 'CO2E'");
            Assert.True(KatalogGestempelt());
            Assert.Empty(GestempelteProjekte());
        }

        /// <summary>Die Emissionsart stempelt nur über einen geänderten CO₂-Äquivalenzfaktor — nicht über Name oder Auswahl.</summary>
        [Fact]
        public void Die_Emissionsart_stempelt_nur_den_geaenderten_Aequivalenzfaktor()
        {
            if (!_db.Vorhanden) return;
            int art = Ganz("SELECT MIN(\"id\") FROM \"emissionsart\"");

            StempelLeeren();
            Schreibe("UPDATE \"emissionsart\" SET \"name\" = \"name\", \"ausgewaehlt\" = \"ausgewaehlt\", " +
                     "\"co2_aequivalent\" = \"co2_aequivalent\" WHERE \"id\" = ?", new DbParam("?", art));
            Assert.False(KatalogGestempelt());

            StempelLeeren();
            Schreibe("UPDATE \"emissionsart\" SET \"co2_aequivalent\" = COALESCE(\"co2_aequivalent\", 0) + 1 WHERE \"id\" = ?",
                     new DbParam("?", art));
            Assert.True(KatalogGestempelt());
            Assert.Empty(GestempelteProjekte());
        }

        /// <summary>Jede Katalogtabelle: eine Änderung einer Zeile stempelt den Katalog und kein Projekt.</summary>
        [Theory]
        [InlineData("energy_carrier", "name")]
        [InlineData("pricing_model", "name")]
        [InlineData("energy_conversion", "factor")]
        [InlineData("Tab_Brennstoff_Stamm", "Bezeichner")]
        [InlineData("Tab_BrennstoffKategorien", "Gruppe")]
        [InlineData("emissionswert", "wert")]
        [InlineData("Tab_Gesetzesparameter", "Wert")]
        [InlineData("Tab_Kostenfaktor", "Bezeichnung")]
        [InlineData("Tab_KostenKomponente", "Komponente")]
        [InlineData("Tab_Nutzungsdauer", "Quelle")]
        public void Eine_Katalogtabelle_stempelt_den_Katalog(string tabelle, string spalte)
        {
            if (!_db.Vorhanden) return;
            Assert.Contains(tabelle, KostenStempelSchema.KATALOGTABELLEN);

            StempelLeeren();
            Schreibe("UPDATE \"" + tabelle + "\" SET \"" + spalte + "\" = \"" + spalte + "\" WHERE rowid = " +
                     "(SELECT MIN(rowid) FROM \"" + tabelle + "\")");
            Assert.True(KatalogGestempelt());
            Assert.Empty(GestempelteProjekte());
        }

        /// <summary>Anlegen und Löschen einer Katalogzeile stempeln den Katalog.</summary>
        [Theory]
        [InlineData("energy_carrier", "INSERT INTO \"energy_carrier\" (\"name\") VALUES ('Probe Stempel')")]
        [InlineData("pricing_model", "INSERT INTO \"pricing_model\" (\"code\", \"name\") VALUES ('PROBE', 'Probe Stempel')")]
        [InlineData("energy_conversion", "INSERT INTO \"energy_conversion\" (\"factor\") VALUES (1.0)")]
        [InlineData("Tab_BrennstoffKategorien", "INSERT INTO \"Tab_BrennstoffKategorien\" (\"Gruppe\") VALUES ('Probe Stempel')")]
        [InlineData("Tab_Gesetzesparameter", "INSERT INTO \"Tab_Gesetzesparameter\" (\"Schluessel\", \"Klasse\", \"JahrVon\", \"Wert\") VALUES ('PROBE_STEMPEL', 'SYSTEM', 2099, 1)")]
        [InlineData("Tab_Kostenfaktor", "INSERT INTO \"Tab_Kostenfaktor\" (\"Bezeichnung\") VALUES ('Probe Stempel')")]
        [InlineData("Tab_KostenKomponente", "INSERT INTO \"Tab_KostenKomponente\" (\"Komponente\") VALUES ('Probe Stempel')")]
        [InlineData("Tab_Nutzungsdauer", "INSERT INTO \"Tab_Nutzungsdauer\" (\"Positionsart\") VALUES ('Probe Stempel')")]
        public void Anlegen_und_Loeschen_einer_Katalogzeile_stempeln_den_Katalog(string tabelle, string anlegen)
        {
            if (!_db.Vorhanden) return;

            StempelLeeren();
            int id = Einfuegen(anlegen);
            Assert.True(KatalogGestempelt());

            StempelLeeren();
            Schreibe("DELETE FROM \"" + tabelle + "\" WHERE rowid = ?", new DbParam("?", id));
            Assert.True(KatalogGestempelt());
            Assert.Empty(GestempelteProjekte());
        }

        /// <summary>Die ausgenommenen Tabellen stempeln nicht — weder ein Projekt noch den Katalog.</summary>
        [Theory]
        [InlineData("Tab_Kraftwerkspark", "Bezeichner")]
        [InlineData("Tab_KostenGruppenKatalog", "GruppenName")]
        [InlineData("Tab_KostenVorlage", "Name")]
        [InlineData("Tab_KostenVorlagePosition", "Bezeichnung")]
        [InlineData("Tab_ErgebnisWirtschaftlichkeit", "Kapitalwert")]
        [InlineData("Tab_Ergebnis", "Bezeichner")]
        [InlineData("Tab_Einstellungen", "Tool_1")]
        [InlineData("Tab_Gebaeude", "Gebaeudename")]
        [InlineData("Tab_Heizkessel", "Bezeichner")]
        public void Ausgenommene_Tabellen_stempeln_nicht(string tabelle, string spalte)
        {
            if (!_db.Vorhanden) return;

            StempelLeeren();
            Schreibe("UPDATE \"" + tabelle + "\" SET \"" + spalte + "\" = \"" + spalte + "\"");
            Assert.Empty(GestempelteProjekte());
            Assert.False(KatalogGestempelt());
        }

        /// <summary>Der Stempel ist Ortszeit im Format des Ergebnis-Zeitstempels, sekundengenau.</summary>
        [Fact]
        public void Der_Stempel_ist_Ortszeit_im_Format_des_Ergebnis_Zeitstempels()
        {
            if (!_db.Vorhanden) return;

            StempelLeeren();
            DateTime vorher = KostenAenderungsstempel.Sekunde(DateTime.Now);
            Schreibe("UPDATE \"Tab_ProjektWirtschaftlichkeit\" SET \"Zinssatz\" = \"Zinssatz\" WHERE \"ID_Projekt\" = 1030");
            DateTime nachher = DateTime.Now;

            string text = Text("SELECT \"Kosten_Geaendert\" FROM \"Tab_Projekt\" WHERE \"ID\" = 1030");
            Assert.True(DateTime.TryParseExact(text, KostenStempelSchema.FORMAT, CultureInfo.InvariantCulture,
                                               DateTimeStyles.None, out DateTime stempel), text);
            Assert.InRange(stempel, vorher.AddSeconds(-2), nachher.AddSeconds(2));
            Assert.Equal(text, SqliteDatenzugriff.NormalisiereWert(stempel));
        }

        /// <summary>Ein gelöschtes Projekt nimmt seine Zeilen mit; die Trigger der Kaskade schreiben ins Leere, ohne Fehler.</summary>
        [Fact]
        public void Ein_geloeschtes_Projekt_nimmt_seine_Zeilen_ohne_Fehler_mit()
        {
            if (!_db.Vorhanden) return;

            StempelLeeren();
            Schreibe("DELETE FROM \"Tab_Projekt\" WHERE \"ID\" = 1030");
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_ProjektWerte\" WHERE \"ProjektID\" = 1030"));
            Assert.Empty(GestempelteProjekte());
        }

        // =============================================================================
        //  Teil 4 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und die
        /// Nachzieh-Liste der Tests führen den Schritt aus derselben Quelle, hinter Schritt 158 und als
        /// letzten; die REPO-Datei trägt Spalten und Trigger mit leeren Stempeln (nur lesend geöffnet).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wStempel = werkzeug.IndexOf("KostenStempelSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(wStempel > werkzeug.IndexOf("KesselBrennwertNachzug.Ausfuehren(", StringComparison.Ordinal),
                        "Das Werkzeug führt den Schritt nicht hinter 158.");
            Assert.True(wStempel < werkzeug.IndexOf("SET SchemaVersion", StringComparison.Ordinal),
                        "Das Werkzeug setzt den Marker vor dem Schritt.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_KOSTEN_STEMPEL = KostenStempelSchema.SCHRITT", migration);
            int ortBrennwert = migration.IndexOf("new Schritt(SCHRITT_KESSEL_BRENNWERT_NACHZUG", StringComparison.Ordinal);
            int ortStempel = migration.IndexOf("new Schritt(SCHRITT_KOSTEN_STEMPEL", StringComparison.Ordinal);
            Assert.True(ortBrennwert > 0 && ortStempel > ortBrennwert, "Der Schritt steht nicht hinter 158.");
            Assert.Contains("KostenStempelSchema.Anweisungen", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vStempel = vorrichtung.IndexOf("KostenStempelSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(vStempel > vorrichtung.IndexOf("KesselBrennwertNachzug.Ausfuehren(null)", StringComparison.Ordinal),
                        "Die Testkopie führt den Schritt nicht hinter 158.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= KostenStempelSchema.SCHRITT);
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('Tab_Projekt') " +
                                              "WHERE name = 'Kosten_Geaendert' AND type = 'TEXT' AND \"notnull\" = 0 AND dflt_value IS NULL"));
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('Tab_Applikation') " +
                                              "WHERE name = 'Kostenkatalog_Geaendert' AND type = 'TEXT' AND \"notnull\" = 0 AND dflt_value IS NULL"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Projekt WHERE Kosten_Geaendert IS NOT NULL"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_Applikation WHERE Kostenkatalog_Geaendert IS NOT NULL"));

            var namen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (SqliteCommand cmd = verbindung.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'trigger'";
                using SqliteDataReader r = cmd.ExecuteReader();
                while (r.Read()) namen.Add(r.GetString(0));
            }
            foreach (KostenStempelSchema.Stempeltrigger t in KostenStempelSchema.Trigger)
                Assert.True(namen.Contains(t.Name), "Die Repo-Datei trägt den Trigger " + t.Name + " nicht.");
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static void StempelLeeren()
        {
            Schreibe("UPDATE \"Tab_Projekt\" SET \"Kosten_Geaendert\" = NULL");
            Schreibe("UPDATE \"Tab_Applikation\" SET \"Kostenkatalog_Geaendert\" = NULL");
        }

        private static int[] GestempelteProjekte()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"ID\" FROM \"Tab_Projekt\" WHERE \"Kosten_Geaendert\" IS NOT NULL ORDER BY \"ID\"");
            return dt.Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture)).ToArray();
        }

        private static bool KatalogGestempelt()
            => Zahl("SELECT COUNT(*) FROM \"Tab_Applikation\" WHERE \"Kostenkatalog_Geaendert\" IS NOT NULL") > 0;

        /// <summary>Schreibt in einem eigenen Vorgang — ein Fehler wirft und lässt den Fall scheitern.</summary>
        private static void Schreibe(string sql, params DbParam[] parameter)
        {
            using DbVorgang v = DataRepository.Vorgang();
            v.Ausfuehren(sql, parameter);
            v.Commit();
        }

        private static int Einfuegen(string sql, params DbParam[] parameter)
        {
            using DbVorgang v = DataRepository.Vorgang();
            int id = v.EinfuegenUndId(sql, parameter);
            v.Commit();
            return id;
        }

        /// <summary>Dieselbe Anweisung unter <c>PRAGMA recursive_triggers = ON</c> — auf DERSELBEN Verbindung, danach wieder aus.</summary>
        private static void MitRekursion(string sql)
        {
            using DbVorgang v = DataRepository.Vorgang();
            try
            {
                v.Ausfuehren("PRAGMA recursive_triggers = ON");
                v.Ausfuehren(sql);
            }
            finally
            {
                v.Ausfuehren("PRAGMA recursive_triggers = OFF");
            }
            v.Commit();
        }

        /// <summary>Entfernt Trigger und Stempelspalten — der Stand vor dem Schritt.</summary>
        private static void TriggerUndSpaltenEntfernen()
        {
            foreach (KostenStempelSchema.Stempeltrigger t in KostenStempelSchema.Trigger)
                Schreibe("DROP TRIGGER IF EXISTS \"" + t.Name + "\"");
            Schreibe("ALTER TABLE \"Tab_Projekt\" DROP COLUMN \"Kosten_Geaendert\"");
            Schreibe("ALTER TABLE \"Tab_Applikation\" DROP COLUMN \"Kostenkatalog_Geaendert\"");
        }

        /// <summary>Die Zeilen der zwei Tabellen ohne die Stempelspalten — der Schritt darf sie nicht ändern.</summary>
        private static List<string> Bestand()
        {
            var liste = new List<string>();
            foreach (string sql in new[]
                     {
                         "SELECT ID, Projektname, Bearbeiter, Aenderungsdatum, ID_Klimaregion, Emission_Berechnungsmodus " +
                         "FROM Tab_Projekt ORDER BY ID",
                         "SELECT ID, Projektname, ID_Projekt, Emission_Berechnungsmodus FROM Tab_Applikation ORDER BY ID"
                     })
                foreach (DataRow r in DataRepository.GetDataTable(sql).Rows)
                    liste.Add(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            return liste;
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static int Ganz(string sql)
            => Convert.ToInt32(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static string Text(string sql)
            => Convert.ToString(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static long Repo(SqliteConnection verbindung, string sql)
        {
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
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
