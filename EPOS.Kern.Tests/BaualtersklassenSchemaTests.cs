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
    /// Der Schemaschritt der <b>Baualtersklassen und des Energiestandards</b> (Entscheid E47, Konzept
    /// Baualtersklassen Abschnitte 3, 5 und 6; Nummer bei <see cref="BaualtersklassenSchema"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Nummer, die Spalte samt <c>CHECK</c> und der siebte Sichtneubau;
    /// die Umschlüsselung aller 21 alten Buchstaben, der Vorrang des Baujahrs und die Protokollzeilen;
    /// die Umbenennung des Auslieferungskatalogs samt Kette und Kollision; dass ein zweiter Lauf nichts
    /// verschiebt; der Stand der Testdatenbank; die fest verdrahteten Kopierwege von
    /// <c>GebaeudeStammCtrl</c> samt Namensleser NULL-erhaltend; Repo-Datei, Werkzeug und Migration führen
    /// den Schritt zuletzt.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BaualtersklassenSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf 145; der Zielstand reicht bis zu ihr (E51 legt die Saat dahinter).</summary>
        [Fact]
        public void Die_Nummer_folgt_lueckenlos_und_ist_das_Ziel()
        {
            Assert.True(BaualtersklassenSchema.SCHRITT > TwwSchema.SCHRITT_T5_KONSTRUKTOR);
            Assert.True(SchemaStand.Zielversion >= BaualtersklassenSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + BaualtersklassenSchema.SCHRITT + ".");
        }

        /// <summary>Die Spalte: <c>TEXT</c>, nullbar, <c>CHECK</c> auf genau die elf Codes.</summary>
        [Fact]
        public void Die_Spalte_haelt_nur_die_elf_Codes_und_NULL()
        {
            Assert.Equal("TEXT CHECK (Energiestandard IS NULL OR Energiestandard IN ('TEILSANIERT', 'SANIERT', " +
                         "'NIEDRIGENERGIE', 'EH115_100', 'EH85', 'EH70', 'EH55', 'EH40', 'DENKMAL', 'PASSIVHAUS', 'NULLEMISSION'))",
                         GebaeudeSchema.SQLITE_ENERGIESTANDARD);

            using SqliteConnection c = Speicher();
            Ausfuehren(c, "CREATE TABLE T (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, GebaeudeSchema.EnergiestandardAnlegen("T"));
            Ausfuehren(c, "INSERT INTO T (ID) VALUES (1)");
            foreach (string code in Energiestandard.CODES.Concat(new[] { (string)null }))
                Assert.False(Wirft(c, "UPDATE T SET Energiestandard = " + (code == null ? "NULL" : "'" + code + "'")), code);
            foreach (string schlecht in new[] { "''", "'EH155'", "'eh55'", "'Passivhaus'", "1" })
                Assert.True(Wirft(c, "UPDATE T SET Energiestandard = " + schlecht), schlecht);
        }

        /// <summary>Der siebte Sichtneubau hängt den Energiestandard HINTER die 101 Spalten der Nachtzeit.</summary>
        [Fact]
        public void Der_siebte_Sichtneubau_haengt_den_Energiestandard_hinter_die_Nachtzeit()
        {
            Assert.Equal(102, GebaeudeSchema.SICHT_ENERGIESTANDARD.Length);
            Assert.Equal(GebaeudeSchema.SICHT_NACHTZEIT, GebaeudeSchema.SICHT_ENERGIESTANDARD.Take(101));
            Assert.Equal("Energiestandard", GebaeudeSchema.SICHT_ENERGIESTANDARD[101]);
            Assert.Contains("Tab_Gebaeude.Nachtabsenkung_Ende", GebaeudeSchema.SQL_VIEW_ENERGIESTANDARD, StringComparison.Ordinal);
            Assert.Contains("Tab_Gebaeude.Baujahr", GebaeudeSchema.SQL_VIEW_ENERGIESTANDARD, StringComparison.Ordinal);
            Assert.Contains("Tab_Gebaeude.Energiestandard", GebaeudeSchema.SQL_VIEW_ENERGIESTANDARD, StringComparison.Ordinal);
            Assert.DoesNotContain("Energiestandard", GebaeudeSchema.SQL_VIEW_NACHTZEIT, StringComparison.Ordinal);
            Assert.Equal(GebaeudeSchema.SQL_VIEW_ENERGIESTANDARD, GebaeudeSchema.SQL_VIEW_AKTUELL);
            Assert.Equal(GebaeudeSchema.SICHT_ENERGIESTANDARD, GebaeudeSchema.SICHT_AKTUELL);
        }

        /// <summary>
        /// DIE UMSCHLÜSSELUNG ALLER 21 ALTEN BUCHSTABEN ohne Baujahr (Konzept Abschnitt 5): Klasse,
        /// Energiestandard und ob die Klasse eindeutig ist — I, J und S sind es nicht.
        /// </summary>
        [Theory]
        [InlineData("A", "B", null, true)]
        [InlineData("B", "C", null, true)]
        [InlineData("C", "D", null, true)]
        [InlineData("D", "E", null, true)]
        [InlineData("E", "F", null, true)]
        [InlineData("F", "G", null, true)]
        [InlineData("G", "H", null, true)]
        [InlineData("H", "I", null, true)]
        [InlineData("I", "J", Energiestandard.NIEDRIGENERGIE, false)]
        [InlineData("J", "J", Energiestandard.PASSIVHAUS, false)]
        [InlineData("K", "J", null, true)]
        [InlineData("L", "J", Energiestandard.EH70, true)]
        [InlineData("M", "K", null, true)]
        [InlineData("N", "K", Energiestandard.EH70, true)]
        [InlineData("O", "K", Energiestandard.EH55, true)]
        [InlineData("P", "K", null, true)]
        [InlineData("Q", "L", null, true)]
        [InlineData("R", "L", Energiestandard.EH115_100, true)]
        [InlineData("S", "L", null, false)]
        [InlineData("T", "M", Energiestandard.EH55, true)]
        [InlineData("U", "M", Energiestandard.EH40, true)]
        public void Jeder_alte_Buchstabe_hat_seine_neue_Klasse(string alt, string klasse, string standard, bool eindeutig)
        {
            BaualtersklassenSchema.Umschluesselung u = BaualtersklassenSchema.Umschluesseln(alt, null);
            Assert.Equal(klasse, u.Klasse);
            Assert.Equal(standard, u.Energiestandard);
            Assert.Equal(eindeutig, u.Eindeutig);
            Assert.Equal(eindeutig, u.Grund.Length == 0);
            Assert.Equal(klasse, BaualtersklassenSchema.Umschluesseln(alt.ToLowerInvariant(), null).Klasse);
        }

        /// <summary>
        /// DAS BAUJAHR FÜHRT: Mit Baujahr gilt die Klasse aus dem Jahr, der Standard folgt weiter der
        /// Tabelle, und die Klasse ist eindeutig — auch für I, J und S. Ohne alte Klasse gilt das Jahr;
        /// ohne beides bleibt die Zeile leer; ein unbekannter Buchstabe bleibt stehen und wird gemeldet.
        /// </summary>
        [Fact]
        public void Das_Baujahr_fuehrt_die_Umschluesselung()
        {
            AssertU(BaualtersklassenSchema.Umschluesseln("A", 1965), "E", null, true);
            AssertU(BaualtersklassenSchema.Umschluesseln("I", 1998), "I", Energiestandard.NIEDRIGENERGIE, true);
            AssertU(BaualtersklassenSchema.Umschluesseln("J", 2012), "K", Energiestandard.PASSIVHAUS, true);
            AssertU(BaualtersklassenSchema.Umschluesseln("S", 2018), "L", null, true);
            AssertU(BaualtersklassenSchema.Umschluesseln("T", 2023), "M", Energiestandard.EH55, true);
            AssertU(BaualtersklassenSchema.Umschluesseln(null, 1850), "A", null, true);
            AssertU(BaualtersklassenSchema.Umschluesseln("", 1900), "B", null, true);
            AssertU(BaualtersklassenSchema.Umschluesseln(null, null), null, null, true);
            AssertU(BaualtersklassenSchema.Umschluesseln("", null), "", null, true);
            BaualtersklassenSchema.Umschluesselung x = BaualtersklassenSchema.Umschluesseln("X", null);
            AssertU(x, "X", null, false);
            Assert.Contains("unbekannte", x.Grund, StringComparison.Ordinal);
            AssertU(BaualtersklassenSchema.Umschluesseln("X", 2005), "J", null, true);
        }

        /// <summary>Der neue Name: nur der zweite Namensteil, nur wenn er der alte Buchstabe ist, nur bei einer Änderung.</summary>
        [Fact]
        public void Der_neue_Name_tauscht_nur_den_Klassenteil()
        {
            Assert.Equal("AltenH-D-U-252", BaualtersklassenSchema.NeuerName("AltenH-C-U-252", "C", "D"));
            Assert.Equal("Hotel-B-215", BaualtersklassenSchema.NeuerName("Hotel-A-215", "A", "B"));
            Assert.Equal("GMH-J-U-84", BaualtersklassenSchema.NeuerName("GMH-K-U-84", "K", "J"));
            Assert.Null(BaualtersklassenSchema.NeuerName("REH-J-015", "J", "J"));           // keine Aenderung
            Assert.Null(BaualtersklassenSchema.NeuerName("KMH-A-U-250", "B", "C"));         // Name und Klasse passen nicht
            Assert.Null(BaualtersklassenSchema.NeuerName("gr-Hotel-D-162", "D", "E"));      // zweiter Teil ist kein Buchstabe
            Assert.Null(BaualtersklassenSchema.NeuerName("Verw_D_168", "D", "E"));          // anderer Trenner
            Assert.Null(BaualtersklassenSchema.NeuerName("Kaufhaus", "A", "B"));
            Assert.Null(BaualtersklassenSchema.NeuerName("Z-EFH-A-S-126", "A", "B"));
            Assert.Null(BaualtersklassenSchema.NeuerName("", "A", "B"));
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und der Schritt aus dem Stand davor
        // =============================================================================

        /// <summary>
        /// Die Testdatenbank steht auf dem Schritt: die Spalte an beiden Tabellen, die geltende Sicht, nur
        /// noch Klassen A…M (oder leer) und die Energiestandards der Umschlüsselung.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= BaualtersklassenSchema.SCHRITT);
            Assert.True(BaualtersklassenSchema.Vollstaendig());
            Assert.True(NachtzeitSchema.Vollstaendig());
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            string sicht = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = ?", new DbParam("?", GebaeudeSchema.VIEW)),
                CultureInfo.InvariantCulture);
            Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, sicht);

            foreach (string t in GebaeudeSchema.TABELLEN)
            {
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("?", t)),
                    CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", ddl.TrimEnd(), StringComparison.Ordinal);
                Assert.Contains(GebaeudeSchema.SQLITE_ENERGIESTANDARD, ddl, StringComparison.Ordinal);
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + t + "] WHERE Baualtersklasse IS NOT NULL AND " +
                                      "Baualtersklasse NOT IN ('A','B','C','D','E','F','G','H','I','J','K','L','M')"));
            }

            Assert.Equal(34L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Energiestandard = 'NIEDRIGENERGIE'"));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Energiestandard = 'PASSIVHAUS'"));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Energiestandard = 'EH70'"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE Energiestandard IS NOT NULL"));
            // Die Projektkopien trugen nur Bauzeitraeume (alt A 18, D 2, F 4, G 2, H 2) - je einen Buchstaben weiter;
            // H3 mit der Gebaeudekopie des Referenzprojekts Solarthermie 1049 (Vorlage 1018).
            var kopien = DataRepository.GetDataTable(
                "SELECT Baualtersklasse, COUNT(*) AS Anzahl FROM Tab_Gebaeude GROUP BY Baualtersklasse ORDER BY Baualtersklasse")
                .Rows.Cast<DataRow>()
                .Select(r => Convert.ToString(r[0], CultureInfo.InvariantCulture) + Convert.ToString(r[1], CultureInfo.InvariantCulture));
            Assert.Equal(new[] { "B18", "E2", "G4", "H3", "I2" }, kopien);
        }

        /// <summary>
        /// DER SCHRITT AUS DEM STAND DAVOR: Spalte und Sicht werden entfernt, 21 Katalogsätze tragen die 21
        /// alten Buchstaben, dazu Sätze mit Baujahr, Auslieferungssätze mit dem Buchstaben im Namen (eine
        /// Kette, eine Kollision, ein Name, der nicht zur Klasse passt), ein eigener Satz und eine
        /// Projektkopie. Der Schritt schlüsselt jede Zeile nach der Tabelle um, das Baujahr führt, die
        /// Protokollzeilen nennen I, J und S ohne Baujahr, die Namen folgen der Regel — und ein zweiter
        /// Lauf verschiebt nichts.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_und_genau_einmal()
        {
            if (!_db.Vorhanden) return;

            ZurueckAufDenStandDavor();
            Assert.False(BaualtersklassenSchema.Vollstaendig());

            List<int> ids = DataRepository.GetDataTable("SELECT ID FROM Tab_Gebaeude_STAMM ORDER BY ID").Rows
                .Cast<DataRow>().Select(r => Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture)).ToList();
            Assert.True(ids.Count >= 40);

            // 21 Saetze mit den 21 alten Buchstaben, ohne Baujahr, eigene Saetze mit unveraendertem Namen.
            string alt = BaualtersklassenSchema.ALTE_BUCHSTABEN;
            for (int i = 0; i < alt.Length; i++)
                Setze(ids[i], "E47-Probe " + alt[i], alt[i].ToString(), null, 0);
            // Das Baujahr fuehrt.
            Setze(ids[21], "E47-Baujahr A", "A", 1965, 0);
            Setze(ids[22], "E47-Baujahr I", "I", 1998, 0);
            Setze(ids[23], "E47-Baujahr T", "T", 2023, 0);
            // Auslieferungssaetze: einfache Umbenennung, eine Kette, eine Kollision, ein unpassender Name.
            Setze(ids[24], "E47A-A-U-1", "A", null, 1);
            Setze(ids[25], "E47K-A-1", "A", null, 1);
            Setze(ids[26], "E47K-B-1", "B", null, 1);
            Setze(ids[27], "E47X-C-7", "C", null, 1);
            Setze(ids[28], "E47X-D-7", "D", null, 0);      // eigener Satz: bleibt und blockiert den neuen Namen
            Setze(ids[29], "E47M-A-3", "B", null, 1);      // Name sagt A, Klasse ist B: kein Klassenteil
            Setze(ids[30], "E47E-A-9", "A", null, 0);      // eigener Satz: keine Umbenennung
            Setze(ids[31], "E47J-I-5", "I", 1998, 1);      // mit Baujahr: der neue Buchstabe kommt aus dem Jahr
            Setze(ids[32], "E47S-J-2", "J", null, 1);      // J bleibt J: keine Umbenennung
            // Eine Projektkopie mit dem Buchstaben im Namen: Klasse ja, Name nein.
            int kopie = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Gebaeude ORDER BY ID LIMIT 1"),
                                        CultureInfo.InvariantCulture);
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Gebaeudename = ?, Baualtersklasse = 'A', Baujahr = NULL WHERE ID = ?",
                                                  new DbParam("?", "E47P-A-1"), new DbParam("?", kopie)));

            var bericht = new List<string>();
            BaualtersklassenSchema.Bericht b = BaualtersklassenSchema.Ausfuehren(bericht);

            Assert.Equal(2, b.SpaltenAngelegt);
            Assert.True(b.Umgeschluesselt);
            Assert.True(BaualtersklassenSchema.Vollstaendig());
            Assert.Equal(GebaeudeSchema.SICHT_AKTUELL, GebaeudeSchema.SichtSpalten());
            Assert.Contains(bericht, z => z.Contains("(102 Spalten)", StringComparison.Ordinal));

            for (int i = 0; i < alt.Length; i++)
            {
                BaualtersklassenSchema.Umschluesselung erwartet = BaualtersklassenSchema.Umschluesseln(alt[i].ToString(), null);
                DataRow r = Zeile("SELECT Baualtersklasse, Energiestandard, Bezeichner FROM Tab_Gebaeude_STAMM WHERE ID = ?", ids[i]);
                Assert.Equal(erwartet.Klasse, Convert.ToString(r[0], CultureInfo.InvariantCulture));
                Assert.Equal(erwartet.Energiestandard, r[1] == DBNull.Value ? null : Convert.ToString(r[1], CultureInfo.InvariantCulture));
                Assert.Equal("E47-Probe " + alt[i], Convert.ToString(r[2], CultureInfo.InvariantCulture));
            }
            AssertZeile(ids[21], "E", null);
            AssertZeile(ids[22], "I", Energiestandard.NIEDRIGENERGIE);
            AssertZeile(ids[23], "M", Energiestandard.EH55);

            // Protokoll: genau die Probensaetze mit I, J und S ohne Baujahr (plus der Auslieferungssatz J).
            foreach (char c in "IJS")
                Assert.Contains(b.Unklar, z => z.Contains("\"E47-Probe " + c + "\"", StringComparison.Ordinal));
            Assert.DoesNotContain(b.Unklar, z => z.Contains("E47-Baujahr", StringComparison.Ordinal));
            Assert.DoesNotContain(b.Unklar, z => z.Contains("\"E47-Probe A\"", StringComparison.Ordinal));
            Assert.Contains(b.Unklar, z => z.Contains("\"E47S-J-2\"", StringComparison.Ordinal));

            // Die Namen.
            Assert.Equal("E47A-B-U-1", Name(ids[24]));
            Assert.Equal("E47K-B-1", Name(ids[25]));
            Assert.Equal("E47K-C-1", Name(ids[26]));
            Assert.Equal("E47X-C-7", Name(ids[27]));
            Assert.Equal("E47X-D-7", Name(ids[28]));
            Assert.Equal("E47M-A-3", Name(ids[29]));
            Assert.Equal("E47E-A-9", Name(ids[30]));
            Assert.Equal("E47J-I-5", Name(ids[31]));            // aus I wird mit Baujahr 1998 wieder I
            Assert.Equal("E47S-J-2", Name(ids[32]));
            Assert.Contains(b.Umbenannt, z => z == "\"E47A-A-U-1\" -> \"E47A-B-U-1\"");
            Assert.Contains(b.Umbenannt, z => z == "\"E47K-B-1\" -> \"E47K-C-1\"");
            Assert.Single(b.Kollisionen, z => z.Contains("\"E47X-C-7\"", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.StartsWith("Name belassen (Kollision)", StringComparison.Ordinal));
            DataRow p = Zeile("SELECT Gebaeudename, Baualtersklasse FROM Tab_Gebaeude WHERE ID = ?", kopie);
            Assert.Equal("E47P-A-1", Convert.ToString(p[0], CultureInfo.InvariantCulture));
            Assert.Equal("B", Convert.ToString(p[1], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Bezeichner LIKE '~E47%'"));

            // GENAU EINMAL: Ein zweiter Lauf verschiebt nichts - auch eine Klasse A (heute: bis 1859) bleibt A.
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude_STAMM SET Baualtersklasse = 'A' WHERE ID = ?", new DbParam("?", ids[0])));
            List<string> vorher = Stand();
            var zweiter = new List<string>();
            BaualtersklassenSchema.Bericht b2 = BaualtersklassenSchema.Ausfuehren(zweiter);
            Assert.Equal(0, b2.SpaltenAngelegt);
            Assert.False(b2.Umgeschluesselt);
            Assert.Empty(b2.Umbenannt);
            Assert.Empty(b2.Unklar);
            Assert.Equal(vorher, Stand());
            Assert.Equal("A", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Baualtersklasse FROM Tab_Gebaeude_STAMM WHERE ID = ?", new DbParam("?", ids[0])), CultureInfo.InvariantCulture));
            Assert.Contains(zweiter, z => z.Contains("liefen schon", StringComparison.Ordinal));
            Assert.True(BaualtersklassenSchema.Vollstaendig());
        }

        /// <summary>Die Prüfung der Spalte greift an beiden Tabellen; NULL und ein Code gehen durch.</summary>
        [Fact]
        public void Die_Pruefung_der_Spalte_weist_unbekannte_Codes_ab()
        {
            if (!_db.Vorhanden) return;

            foreach (string t in GebaeudeSchema.TABELLEN)
            {
                Assert.True(Wirft("UPDATE [" + t + "] SET Energiestandard = 'EH155'"), t);
                Assert.True(Wirft("UPDATE [" + t + "] SET Energiestandard = ''"), t);
                Assert.True(DataRepository.ExecuteSQL("UPDATE [" + t + "] SET Energiestandard = ?", new DbParam("?", Energiestandard.EH40)), t);
                Assert.True(DataRepository.ExecuteSQL("UPDATE [" + t + "] SET Energiestandard = NULL"), t);
            }
        }

        // =============================================================================
        //  Teil 3 - die fest verdrahteten Kopierwege von GebaeudeStammCtrl
        // =============================================================================

        /// <summary><c>Insert</c> und <c>Overwrite</c>: der Code kommt an, leer und <c>null</c> werden NULL.</summary>
        [Fact]
        public void Insert_und_Overwrite_tragen_den_Energiestandard_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new GebaeudeStammCtrl();
            Assert.True(ctrl.Insert(new GebaeudeModel { Gebaeudename = "E47 mit", Nutzflaeche = 100, Energiestandard = Energiestandard.EH55 }));
            Assert.True(ctrl.Insert(new GebaeudeModel { Gebaeudename = "E47 ohne", Nutzflaeche = 100, Energiestandard = "" }));
            Assert.Equal(Energiestandard.EH55, Convert.ToString(Zeile("SELECT Energiestandard FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", "E47 mit")[0],
                                                               CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, Zeile("SELECT Energiestandard FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", "E47 ohne")[0]);
            Assert.Equal(Energiestandard.EH55, Lesen("E47 mit").Energiestandard);
            Assert.Null(Lesen("E47 ohne").Energiestandard);

            GebaeudeModel g = Lesen("E47 mit");
            g.Energiestandard = null;
            Assert.True(ctrl.Overwrite(g));
            Assert.Equal(DBNull.Value, Zeile("SELECT Energiestandard FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", "E47 mit")[0]);
            g = Lesen("E47 mit");
            g.Energiestandard = Energiestandard.DENKMAL;
            Assert.True(ctrl.Overwrite(g));
            Assert.Equal(Energiestandard.DENKMAL, Lesen("E47 mit").Energiestandard);
        }

        /// <summary><c>CopyFromStamm</c>, der Namensleser der Sicht und „Duplizieren…": der Code geht mit, NULL bleibt NULL.</summary>
        [Fact]
        public void CopyFromStamm_Namensleser_und_Duplizieren_tragen_den_Energiestandard()
        {
            if (!_db.Vorhanden) return;

            DataRow z = DataRepository.GetDataTable("SELECT ID, ID_Projekt FROM Z_ProjektGebaeude ORDER BY ID LIMIT 1").Rows[0];
            int idProjekt = Convert.ToInt32(z["ID_Projekt"], CultureInfo.InvariantCulture);
            int idZuordnung = Convert.ToInt32(z["ID"], CultureInfo.InvariantCulture);
            DataTable stamm = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner FROM Tab_Gebaeude_STAMM WHERE Energiestandard IS NULL ORDER BY ID LIMIT 2");
            int idMit = Convert.ToInt32(stamm.Rows[0]["ID"], CultureInfo.InvariantCulture);
            string mit = Convert.ToString(stamm.Rows[0]["Bezeichner"], CultureInfo.InvariantCulture);
            string ohne = Convert.ToString(stamm.Rows[1]["Bezeichner"], CultureInfo.InvariantCulture);
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude_STAMM SET Energiestandard = ? WHERE ID = ?",
                                                  new DbParam("?", Energiestandard.PASSIVHAUS), new DbParam("?", idMit)));

            var ctrl = new GebaeudeStammCtrl();
            int neuMit = ctrl.CopyFromStamm(mit, idProjekt, idZuordnung);
            int neuOhne = ctrl.CopyFromStamm(ohne, idProjekt, idZuordnung);
            Assert.True(neuMit > 0 && neuOhne > 0);
            DataRow kopieMit = Zeile("SELECT Energiestandard, ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", neuMit);
            Assert.Equal(Energiestandard.PASSIVHAUS, Convert.ToString(kopieMit[0], CultureInfo.InvariantCulture));
            Assert.Equal((long)idMit, Convert.ToInt64(kopieMit[1], CultureInfo.InvariantCulture));   // die Klammer bleibt die letzte Stelle
            Assert.Equal(DBNull.Value, Zeile("SELECT Energiestandard FROM Tab_Gebaeude WHERE ID = ?", neuOhne)[0]);

            var projekt = new ProjektGebaeudeCtrl();
            projekt.ReadAll(idProjekt);
            Assert.Equal(Energiestandard.PASSIVHAUS, projekt.items.Single(x => x.ID_Gebaeude == neuMit).Energiestandard);
            Assert.Null(projekt.items.Single(x => x.ID_Gebaeude == neuOhne).Energiestandard);
            Assert.Contains("Energiestandard", UebergabeHerleitungsquelle.Uebertragen());

            Katalogkopie.Ergebnis e = GebaeudeStammCtrl.Duplizieren(idMit, "E47 Kopie");
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(Energiestandard.PASSIVHAUS,
                         Convert.ToString(Zeile("SELECT Energiestandard FROM Tab_Gebaeude_STAMM WHERE ID = ?", e.Id)[0], CultureInfo.InvariantCulture));
        }

        // =============================================================================
        //  Teil 4 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und
        /// Testkopie führen den Schritt aus derselben Quelle und ZULETZT (hinter der Nachtzeit); die
        /// Repo-Datei trägt den Schritt (lesend geprüft, ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt_zuletzt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wBak = werkzeug.IndexOf("BaualtersklassenSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(wBak > werkzeug.IndexOf("NachtzeitSchema.Alle(", StringComparison.Ordinal) &&
                        wBak > werkzeug.IndexOf("BaujahrSchema.Alle(", StringComparison.Ordinal),
                        "Der siebte Sichtneubau steht im Werkzeug nicht zuletzt.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_BAUALTERSKLASSEN = BaualtersklassenSchema.SCHRITT", migration);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_145_ZAPFPROFIL_KONSTRUKTOR", StringComparison.Ordinal);
            int ortBak = migration.IndexOf("new Schritt(SCHRITT_BAUALTERSKLASSEN", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ortBak > ortVorher, "Der Schritt steht nicht hinter 145.");
            Assert.Contains("BaualtersklassenSchema.Ausfuehren(zeilen)", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vBak = vorrichtung.IndexOf("BaualtersklassenSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(vBak > vorrichtung.IndexOf("NachtzeitSchema.Alle(null)", StringComparison.Ordinal),
                        "Der siebte Sichtneubau steht in der Testkopie nicht zuletzt.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= BaualtersklassenSchema.SCHRITT);
            foreach (string t in GebaeudeSchema.TABELLEN)
            {
                Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM pragma_table_info('" + t + "') WHERE name = 'Energiestandard'"));
                Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM [" + t + "] WHERE Baualtersklasse IN " +
                                                  "('N','O','P','Q','R','S','T','U')"));
            }
            using (SqliteCommand cmd = verbindung.CreateCommand())
            {
                cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'view' AND name = '" + GebaeudeSchema.VIEW + "'";
                Assert.Equal(GebaeudeSchema.SQL_VIEW_AKTUELL, Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>Der Stand vor dem Schritt: Sicht und Spalte weg, die Sicht der Nachtzeit steht.</summary>
        private static void ZurueckAufDenStandDavor()
        {
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP);
            foreach (string t in GebaeudeSchema.TABELLEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + t + "\" DROP COLUMN \"" + GebaeudeSchema.SPALTE_ENERGIESTANDARD + "\"");
            DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_NACHTZEIT);
            foreach (string t in GebaeudeSchema.TABELLEN)
                Assert.False(DataRepository.SpalteVorhanden(t, GebaeudeSchema.SPALTE_ENERGIESTANDARD));
        }

        private static void Setze(int id, string name, string klasse, int? baujahr, int readOnly)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude_STAMM SET Bezeichner = ?, Baualtersklasse = ?, Baujahr = ?, ReadOnly = ? WHERE ID = ?",
                new DbParam("?", name), new DbParam("?", klasse),
                new DbParam("?", baujahr.HasValue ? (object)baujahr.Value : DBNull.Value),
                new DbParam("?", readOnly), new DbParam("?", id)));
        }

        private static void AssertZeile(int id, string klasse, string standard)
        {
            DataRow r = Zeile("SELECT Baualtersklasse, Energiestandard FROM Tab_Gebaeude_STAMM WHERE ID = ?", id);
            Assert.Equal(klasse, Convert.ToString(r[0], CultureInfo.InvariantCulture));
            Assert.Equal(standard, r[1] == DBNull.Value ? null : Convert.ToString(r[1], CultureInfo.InvariantCulture));
        }

        private static void AssertU(BaualtersklassenSchema.Umschluesselung u, string klasse, string standard, bool eindeutig)
        {
            Assert.Equal(klasse, u.Klasse);
            Assert.Equal(standard, u.Energiestandard);
            Assert.Equal(eindeutig, u.Eindeutig);
        }

        private static string Name(int id)
            => Convert.ToString(DataRepository.ExecuteScalar("SELECT Bezeichner FROM Tab_Gebaeude_STAMM WHERE ID = ?",
                                                             new DbParam("?", id)), CultureInfo.InvariantCulture);

        /// <summary>Name, Klasse und Standard beider Tabellen — der Stand, den ein zweiter Lauf nicht ändern darf.</summary>
        private static List<string> Stand()
        {
            var liste = new List<string>();
            foreach (string sql in new[]
                     {
                         "SELECT ID, Gebaeudename, Baualtersklasse, Energiestandard FROM Tab_Gebaeude ORDER BY ID",
                         "SELECT ID, Bezeichner, Baualtersklasse, Energiestandard, ReadOnly FROM Tab_Gebaeude_STAMM ORDER BY ID",
                     })
            {
                DataTable dt = DataRepository.GetDataTable(sql);
                foreach (DataRow r in dt.Rows)
                    liste.Add(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            }
            return liste;
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static DataRow Zeile(string sql, object wert)
        {
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("?", wert));
            Assert.True(dt != null && dt.Rows.Count == 1, "Keine eindeutige Zeile: " + sql);
            return dt.Rows[0];
        }

        private static GebaeudeModel Lesen(string name)
        {
            var c = new GebaeudeStammCtrl();
            c.ReadAll("Bezeichner = '" + name + "'");
            Assert.Single(c.items);
            return c.items[0];
        }

        /// <summary>Scheitert die Anweisung an einer Prüfung? Der Vorgang wird nie festgeschrieben.</summary>
        private static bool Wirft(string sql, params DbParam[] parameter)
        {
            using DbVorgang v = DataRepository.Vorgang();
            try
            {
                v.Ausfuehren(sql, parameter);
                return false;
            }
            catch (SqliteException)
            {
                return true;
            }
        }

        /// <summary>Eine leere Datenbank im Speicher — für die Prüfungen der Definition ohne Testdatenbank.</summary>
        private static SqliteConnection Speicher()
        {
            var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            return c;
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
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
