using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Schemaschritt <b>„Katalogempfehlung der Hilfsenergie auf Weg B“</b> (Auftrag P671,
    /// Register E30‑Q12, EZ‑24; Nummer bei <see cref="HilfsenergieEmpfehlungNachzug"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Nummer (lückenlos hinter den Betriebskalendern, zugleich das
    /// Ziel des Schemastands) und ihr Eintrag im Register der Paketanhebung als Katalogstufe; die
    /// Saat mit den neuen Spannen; der Schritt an einer Kopie der Testdatenbank aus dem Stand davor —
    /// welche Zeilen er nachzieht, dass er Projektzeilen, eigene Vorlagen und Weg-A-Zeilen nicht
    /// anfasst und wiederholbar ist; die Gegenprobe, dass eine Projektzeile mit eigenem Satz vorher
    /// wie nachher rechnet; die Repo-Datei trägt ihn; Migration, Werkzeug und Testvorrichtung führen
    /// ihn aus derselben Quelle.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — mehrere Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class HilfsenergieEmpfehlungNachzugTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        /// <summary>Die Pflichtzeile „Hilfsenergiekosten“ der BHKW-Vorlage „Standard“ der Testdatenbank.</summary>
        private const int POS_BHKW = 62;

        /// <summary>Die Pflichtzeile „Hilfsenergiekosten (Strom)“ der Heizkessel-Vorlage „Standard“.</summary>
        private const int POS_KESSEL = 68;

        /// <summary>1030, „Hilfsenergiekosten (Strom)“ an Kessel 11334 (Pflichtzeile, Weg B).</summary>
        private const int ZEILE_KESSEL_1030 = 101600587;

        private const int PROJEKT = 1030;

        // =============================================================================
        //  Teil 1 - Nummer, Register und Saat (ohne Datenbank)
        // =============================================================================

        /// <summary>
        /// 170: die nächste freie Nummer hinter <see cref="PufferAuslegungSchema.SCHRITT"/> (169,
        /// Pufferauslegung P1) — und der Zielstand des Schemas. Die Teillastfelder (167, Welle M4), die
        /// Einspeisegrenze mit Selbstentladung (168, Welle M5) und die Pufferauslegung (169, P1) haben
        /// ihre Nummern zeitgleich belegt und standen zuerst auf dem Arbeitszweig; dieser
        /// Katalogschritt hängt sich dahinter.
        /// </summary>
        [Fact]
        public void Die_Nummer_folgt_lueckenlos_auf_die_Pufferauslegung_und_ist_das_Ziel()
        {
            Assert.Equal(PufferAuslegungSchema.SCHRITT + 1, HilfsenergieEmpfehlungNachzug.SCHRITT);
            Assert.Equal(170, HilfsenergieEmpfehlungNachzug.SCHRITT);
            Assert.Equal(HilfsenergieEmpfehlungNachzug.SCHRITT, SchemaStand.Zielversion);
        }

        /// <summary>Katalogstufe: Ein Paket führt keine Kostenvorlagen, es gibt nichts umzuformen.</summary>
        [Fact]
        public void Das_Register_der_Paketanhebung_fuehrt_den_Schritt_als_Katalog()
        {
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == HilfsenergieEmpfehlungNachzug.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Katalog, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.False(string.IsNullOrWhiteSpace(s.Text));
            Assert.Equal(HilfsenergieEmpfehlungNachzug.SCHRITT, Paketanhebung.Stufen.Max(x => x.Nr));
        }

        /// <summary>
        /// <b>Die Saat führt die Spannen in Weg B</b>: BHKW 0,5–1,5 %, Heizkessel 1–2 %, beide als
        /// Anteil des Endenergiebedarfs. Herleitung als Umrechnung der alten Spanne aus Weg A
        /// (Kostenanteil × Brennstoffpreis ÷ Strompreis, rund 8 ct ÷ 30 ct): Kessel 4–8 % → 1,1–2,1 %,
        /// BHKW 2–4 % → 0,5–1,1 % — die neuen Spannen decken das bis auf die Rundung der
        /// Kessel-Obergrenze. Der Schritt liest die
        /// Zielwerte aus der Saat; beide können nicht auseinanderlaufen.
        /// </summary>
        [Fact]
        public void Die_Saat_fuehrt_die_Spannen_in_Weg_B()
        {
            SchemaKatalog.VorlagenPositionSeed bhkw =
                HilfsenergieEmpfehlungNachzug.Saatposition(DbWerte.KOSTEN_KOMPONENTE_BHKW, "Hilfsenergiekosten");
            SchemaKatalog.VorlagenPositionSeed kessel =
                HilfsenergieEmpfehlungNachzug.Saatposition(DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL, "Hilfsenergiekosten (Strom)");
            Assert.Equal(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, bhkw.Bemessung);
            Assert.Equal(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, kessel.Bemessung);
            Assert.Equal(0.5, bhkw.EmpfehlungVon);
            Assert.Equal(1.5, bhkw.EmpfehlungBis);
            Assert.Equal(1.0, kessel.EmpfehlungVon);
            Assert.Equal(2.0, kessel.EmpfehlungBis);
            Assert.True(bhkw.IstPflicht && kessel.IstPflicht);

            Assert.Equal(2, HilfsenergieEmpfehlungNachzug.Ziele.Count);
            foreach (HilfsenergieEmpfehlungNachzug.Ziel z in HilfsenergieEmpfehlungNachzug.Ziele)
            {
                SchemaKatalog.VorlagenPositionSeed p = HilfsenergieEmpfehlungNachzug.Saatposition(z.Komponente, z.Position);
                Assert.Equal(p.EmpfehlungVon, z.NeuVon);
                Assert.Equal(p.EmpfehlungBis, z.NeuBis);
                // Die Umrechnung der alten Spanne (8 ct Brennstoff, 30 ct Strom): ihre Untergrenze und
                // ihre Mitte liegen in der neuen Spanne (Kessel 1,07 und 1,60; BHKW 0,53 und 0,80); die
                // Obergrenze am Kessel (2,13) liegt um die Rundung darüber.
                Assert.InRange(z.AltVon * 8.0 / 30.0, z.NeuVon, z.NeuBis);
                Assert.InRange((z.AltVon + z.AltBis) / 2.0 * 8.0 / 30.0, z.NeuVon, z.NeuBis);
                Assert.InRange(z.AltBis * 8.0 / 30.0, z.NeuVon, z.NeuBis + 0.15);
            }
            Assert.Equal(2.0, HilfsenergieEmpfehlungNachzug.Ziele[0].AltVon);
            Assert.Equal(4.0, HilfsenergieEmpfehlungNachzug.Ziele[0].AltBis);
            Assert.Equal(4.0, HilfsenergieEmpfehlungNachzug.Ziele[1].AltVon);
            Assert.Equal(8.0, HilfsenergieEmpfehlungNachzug.Ziele[1].AltBis);
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank
        // =============================================================================

        /// <summary>
        /// Aus dem Stand davor (alte Spannen an beiden Auslieferungszeilen): Der Schritt zieht genau
        /// die zwei Zeilen nach, Tab_ProjektWerte bleibt zellgleich, ein zweiter Lauf tut nichts.
        /// </summary>
        [Fact]
        public void Der_Schritt_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            Assert.True(HilfsenergieEmpfehlungNachzug.Vollstaendig(), "Die Testvorrichtung hat den Schritt schon gezogen.");
            Assert.Equal(2, HilfsenergieEmpfehlungNachzug.Nachgezogen());
            Spanne(POS_BHKW, 2.0, 4.0);
            Spanne(POS_KESSEL, 4.0, 8.0);
            Assert.Equal(2, HilfsenergieEmpfehlungNachzug.Offen());
            Assert.False(HilfsenergieEmpfehlungNachzug.Vollstaendig());
            string projektVorher = Abzug("Tab_ProjektWerte");
            string uebrigeVorher = Abzug("Tab_KostenVorlagePosition", "ID NOT IN (62, 68)");

            var zeilen = new List<string>();
            HilfsenergieEmpfehlungNachzug.Bericht b = HilfsenergieEmpfehlungNachzug.Ausfuehren(zeilen);
            Assert.Equal(new[] { 1, 1 }, b.JeZiel);
            Assert.Equal(2, b.Nachgezogen);
            Assert.StartsWith("2 Vorlagenposition(en)", zeilen[0]);
            Assert.Contains(zeilen, z => z.StartsWith("BHKW/Hilfsenergiekosten 2.0-4.0 % -> 0.5-1.5 %", StringComparison.Ordinal));
            Assert.Contains(zeilen, z => z.StartsWith("Heizkessel/Hilfsenergiekosten (Strom) 4.0-8.0 % -> 1.0-2.0 %", StringComparison.Ordinal));

            Assert.Equal((0.5, 1.5), Gelesen(POS_BHKW));
            Assert.Equal((1.0, 2.0), Gelesen(POS_KESSEL));
            Assert.True(HilfsenergieEmpfehlungNachzug.Vollstaendig());
            Assert.Equal(projektVorher, Abzug("Tab_ProjektWerte"));
            Assert.Equal(uebrigeVorher, Abzug("Tab_KostenVorlagePosition", "ID NOT IN (62, 68)"));

            HilfsenergieEmpfehlungNachzug.Bericht zweiter = HilfsenergieEmpfehlungNachzug.Ausfuehren(null);
            Assert.Equal(0, zweiter.Nachgezogen);
            Assert.Equal((0.5, 1.5), Gelesen(POS_BHKW));
        }

        /// <summary>
        /// Nur Auslieferungszeilen in Weg B: Eine eigene Vorlage (<c>ReadOnly = 0</c>) mit der alten
        /// Spanne bleibt, wie der Anwender sie angelegt hat; eine Zeile, die mit Weg A rechnet, behält
        /// ihre Weg-A-Spanne zu Recht.
        /// </summary>
        [Fact]
        public void Eigene_Vorlagen_und_Weg_A_bleiben_unberuehrt()
        {
            if (!_db.Vorhanden) return;

            Spanne(POS_BHKW, 2.0, 4.0);
            Spanne(POS_KESSEL, 4.0, 8.0);
            DataRepository.ExecuteSQL(
                "UPDATE Tab_KostenVorlage SET ReadOnly = 0 WHERE ID = (SELECT VorlageID FROM Tab_KostenVorlagePosition WHERE ID = ?)",
                new DbParam("@id", POS_BHKW));
            DataRepository.ExecuteSQL("UPDATE Tab_KostenVorlagePosition SET Bemessung = ? WHERE ID = ?",
                new DbParam("@b", DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN), new DbParam("@id", POS_KESSEL));

            Assert.Equal(0, HilfsenergieEmpfehlungNachzug.Offen());
            HilfsenergieEmpfehlungNachzug.Bericht b = HilfsenergieEmpfehlungNachzug.Ausfuehren(null);
            Assert.Equal(0, b.Nachgezogen);
            Assert.Equal((2.0, 4.0), Gelesen(POS_BHKW));
            Assert.Equal((4.0, 8.0), Gelesen(POS_KESSEL));
        }

        /// <summary>
        /// <b>Gegenprobe der Ergebnisneutralität:</b> Die Kessel-Hilfsenergiezeile von 1030 mit
        /// eigenem Satz (1,5 %) rechnet vor und nach dem Schritt denselben Betrag und denselben
        /// Kapitalwert — die Empfehlung ist Hinweis, kein Rechenwert.
        /// </summary>
        [Fact]
        public void Eine_Projektzeile_mit_eigenem_Satz_rechnet_vorher_wie_nachher()
        {
            if (!_db.Vorhanden) return;

            DataRepository.ExecuteSQL("UPDATE Tab_ProjektWerte SET Einheitpreis = 1.5 WHERE ID = ?",
                new DbParam("@id", ZEILE_KESSEL_1030));
            Spanne(POS_BHKW, 2.0, 4.0);
            Spanne(POS_KESSEL, 4.0, 8.0);

            Dictionary<int, KostenPositionNachweis> vorher =
                WirtschaftlichkeitCtrl.BetriebNachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            WirtschaftlichkeitErgebnis kwVorher = Rechne();
            Assert.Equal(1.5, vorher[ZEILE_KESSEL_1030].Einheitpreis);
            Assert.True(vorher[ZEILE_KESSEL_1030].BetragJahr > 0, "Die Zeile mit eigenem Satz kostet.");

            Assert.Equal(2, HilfsenergieEmpfehlungNachzug.Ausfuehren(null).Nachgezogen);

            Dictionary<int, KostenPositionNachweis> nachher =
                WirtschaftlichkeitCtrl.BetriebNachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            WirtschaftlichkeitErgebnis kwNachher = Rechne();
            Assert.Equal(vorher[ZEILE_KESSEL_1030].BetragJahr, nachher[ZEILE_KESSEL_1030].BetragJahr);
            Assert.Equal(vorher.Values.Sum(x => x.BetragJahr), nachher.Values.Sum(x => x.BetragJahr));
            Assert.Equal(kwVorher.Kapitalwert, kwNachher.Kapitalwert);
            Assert.Equal(kwVorher.BetriebskostenJahr, kwNachher.BetriebskostenJahr);
        }

        /// <summary>
        /// <b>Die Repo-Datei trägt den Schritt:</b> Schemastand, keine Auslieferungszeile mit der alten
        /// Spanne, beide Pflichtzeilen mit der neuen.
        /// </summary>
        [Fact]
        public void Die_Repo_Datei_traegt_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;
            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad) || new FileInfo(pfad).Length < 1_000_000) return;       // LFS-Zeiger

            using var verbindung = new SqliteConnection("Data Source=" + pfad + ";Mode=ReadOnly");
            verbindung.Open();
            Assert.True(Repo(verbindung, "SELECT SchemaVersion FROM Tab_Applikation") >= HilfsenergieEmpfehlungNachzug.SCHRITT);
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_KostenVorlagePosition WHERE ID = 62 " +
                                              "AND Empfehlung_von = 0.5 AND Empfehlung_bis = 1.5"));
            Assert.Equal(1L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_KostenVorlagePosition WHERE ID = 68 " +
                                              "AND Empfehlung_von = 1.0 AND Empfehlung_bis = 2.0"));
            Assert.Equal(0L, Repo(verbindung, "SELECT COUNT(*) FROM Tab_KostenVorlagePosition WHERE Bezeichnung LIKE " +
                                              "'Hilfsenergiekosten%' AND ((Empfehlung_von = 2.0 AND Empfehlung_bis = 4.0) " +
                                              "OR (Empfehlung_von = 4.0 AND Empfehlung_bis = 8.0))"));
        }

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und die
        /// Testvorrichtung führen den Schritt aus derselben Quelle, jeweils hinter den Betriebskalendern.
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("HilfsenergieEmpfehlungNachzug.Ausfuehren(", werkzeug);
            Assert.True(werkzeug.IndexOf("HilfsenergieEmpfehlungNachzug.Ausfuehren(", StringComparison.Ordinal) >
                        werkzeug.IndexOf("BedarfNetzKalenderSchema.Ausfuehren(", StringComparison.Ordinal));

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_HILFSENERGIE_EMPFEHLUNG = HilfsenergieEmpfehlungNachzug.SCHRITT", migration);
            Assert.Contains("new Schritt(SCHRITT_HILFSENERGIE_EMPFEHLUNG", migration);
            Assert.Contains("HilfsenergieEmpfehlungNachzug.Ausfuehren(zeilen)", migration);
            Assert.True(migration.IndexOf("new Schritt(SCHRITT_HILFSENERGIE_EMPFEHLUNG", StringComparison.Ordinal) >
                        migration.IndexOf("new Schritt(SCHRITT_BEDARF_NETZ_KALENDER", StringComparison.Ordinal));

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.True(vorrichtung.IndexOf("HilfsenergieEmpfehlungNachzug.Ausfuehren(null)", StringComparison.Ordinal) >
                        vorrichtung.IndexOf("BedarfNetzKalenderSchema.Ausfuehren(null)", StringComparison.Ordinal));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static void Spanne(int id, double von, double bis)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_KostenVorlagePosition SET Empfehlung_von = ?, Empfehlung_bis = ? WHERE ID = ?",
                new DbParam("@v", von), new DbParam("@b", bis), new DbParam("@id", id)));
        }

        private static (double, double) Gelesen(int id)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Empfehlung_von, Empfehlung_bis FROM Tab_KostenVorlagePosition WHERE ID = ?",
                new DbParam("@id", id));
            Assert.Equal(1, dt.Rows.Count);
            return (Convert.ToDouble(dt.Rows[0][0], CultureInfo.InvariantCulture),
                    Convert.ToDouble(dt.Rows[0][1], CultureInfo.InvariantCulture));
        }

        /// <summary>Ein zellgenauer Abzug einer Tabelle (invariant formatiert, nach ID geordnet).</summary>
        private static string Abzug(string tabelle, string bedingung = null)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + tabelle + (bedingung == null ? "" : " WHERE " + bedingung) + " ORDER BY ID");
            var sb = new StringBuilder();
            foreach (DataRow r in dt.Rows)
            {
                foreach (object o in r.ItemArray)
                    sb.Append(o == DBNull.Value ? "<null>" : Convert.ToString(o, CultureInfo.InvariantCulture)).Append('|');
                sb.Append('\n');
            }
            Assert.True(dt.Rows.Count > 0, tabelle + " ist leer.");
            return sb.ToString();
        }

        private static WirtschaftlichkeitErgebnis Rechne()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT, IstStamm = true, Projektname = "Empfehlung " + PROJEKT,
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return new WirtschaftlichkeitCtrl().Berechne(daten, p).First(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
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
