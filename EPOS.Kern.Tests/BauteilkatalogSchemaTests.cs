using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>BK-2</b> — Schemaschritt <see cref="BauteilkatalogSchema"/> (E132): Herkunft EIGEN/IMPORT mit Neubau der vier
    /// Kopftabellen (IDs und Fremdschlüssel bleiben), Gliederung, Kennzeichen, Felder der Quelle, Abschnittstabellen; dazu die
    /// Kennzeichenregel und die Abbildung im Kern und der Rundlauf über die Controller. Alle Werte sind synthetisch.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BauteilkatalogSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static long Zahl(string sql, params DbParam[] p) =>
            Convert.ToInt64(DataRepository.ExecuteScalar(sql, p) ?? 0L, CultureInfo.InvariantCulture);

        // ---- ohne Datenbank -----------------------------------------------------------------

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_ist_das_Ziel_und_die_Paketanhebung_fuehrt_Ddl()
        {
            Assert.Equal(221, BauteilkatalogSchema.SCHRITT);
            Assert.Equal(RaumnutzungNormwerteSchema.SCHRITT + 1, BauteilkatalogSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= BauteilkatalogSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == BauteilkatalogSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
        }

        [Fact]
        public void Die_Herkunftsklausel_ist_die_alte_plus_EIGEN_und_IMPORT()
        {
            Assert.Equal(BauteilkatalogSchema.CHECK_HERKUNFT_ALT.Replace("'SQPROJ'", "'SQPROJ','EIGEN','IMPORT'"),
                         BauteilkatalogSchema.CHECK_HERKUNFT_NEU);
            Assert.Equal(ProjektdateiImportSchema.CHECK_HERKUNFT_NEU, BauteilkatalogSchema.CHECK_HERKUNFT_ALT);
        }

        [Fact]
        public void Gruppen_und_Klassen_sind_eindeutig_und_jede_Klasse_hat_ihre_Gruppe()
        {
            foreach ((IReadOnlyList<string> gruppen, IReadOnlyList<(string Gruppe, string Klasse)> klassen) in new[]
                     {
                         (BauteilkatalogSchema.BAUSTOFF_GRUPPEN, BauteilkatalogSchema.BAUSTOFF_KLASSEN),
                         (BauteilkatalogSchema.AUFBAU_GRUPPEN, BauteilkatalogSchema.AUFBAU_KLASSEN)
                     })
            {
                Assert.Equal(gruppen.Count, gruppen.Distinct().Count());
                Assert.Equal(klassen.Count, klassen.Select(k => k.Klasse).Distinct().Count());
                Assert.All(klassen, k => Assert.Contains(k.Gruppe, gruppen));
                Assert.All(gruppen, g => Assert.Contains(klassen, k => k.Gruppe == g));
            }
        }

        [Fact]
        public void Die_Abbildung_deckt_jede_Stoffgruppe_und_jede_Bauteilart()
        {
            foreach (Baustoffgruppe g in Baustoffgruppen.Alle)
            {
                string code = Bauteilkatalog.GruppeAusStoffgruppe(g.Wert);
                Assert.Contains(code, BauteilkatalogSchema.BAUSTOFF_GRUPPEN);
                Assert.Equal(g.Wert, Bauteilkatalog.StoffgruppeAusGruppe(code));
            }
            Assert.Equal(BauteilkatalogSchema.BAUSTOFF_GRUPPEN.Count, Baustoffgruppen.Alle.Count);
            Assert.Null(Bauteilkatalog.GruppeAusStoffgruppe("keine feste Gruppe"));

            foreach (string art in Bauteilkatalog.BAUTEILARTEN)
            {
                (string gruppe, string klasse) = Bauteilkatalog.GliederungAusBauteilart(art, null);
                Assert.Contains(gruppe, BauteilkatalogSchema.AUFBAU_GRUPPEN);
                Assert.Null(Bauteilkatalog.GliederungPruefen(false, gruppe, klasse));
            }
            Assert.Equal(("DAECHER", "STEILDACH"), Bauteilkatalog.GliederungAusBauteilart(DbWerte.BAUTEILART_DACH, "DA_SPARRENDACH"));
            Assert.Equal(("DAECHER", (string)null), Bauteilkatalog.GliederungAusBauteilart(DbWerte.BAUTEILART_DACH, null));
            Assert.Equal(((string)null, (string)null), Bauteilkatalog.GliederungAusBauteilart(null, null));

            Assert.Equal("BETON", Bauteilkatalog.GruppeAusQuellcode(2));
            Assert.Equal(Bauteilkatalog.GRUPPE_SONSTIGE, Bauteilkatalog.GruppeAusQuellcode(null));
            Assert.Equal(Bauteilkatalog.GRUPPE_SONSTIGE, Bauteilkatalog.GruppeAusQuellcode(999));
            Assert.All(Bauteilkatalog.GRUPPE_AUS_QUELLCODE.Values, g => Assert.Contains(g, BauteilkatalogSchema.BAUSTOFF_GRUPPEN));

            Assert.NotNull(Bauteilkatalog.GliederungPruefen(true, "BETON", "ZIEGEL"));
            Assert.NotNull(Bauteilkatalog.GliederungPruefen(true, "UNBEKANNT", null));
            Assert.Null(Bauteilkatalog.GliederungPruefen(true, "MAUERWERK", "ZIEGEL"));
        }

        [Fact]
        public void Die_Kennzeichenregel_trifft_Schicht_ohne_Lambda_und_Aufbau_ohne_Schicht()
        {
            static BauteilschichtModel S(double? lambda, bool luft = false) =>
                new BauteilschichtModel { Dicke = 0.1, Lambda = lambda, IstLuftschicht = luft };

            Assert.False(Bauteilkatalog.MeistUngeeignet(new BauteilaufbauModel
            {
                Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Schichten = { S(0.5), S(null, luft: true), S(0.04) }
            }));
            Assert.True(Bauteilkatalog.MeistUngeeignet(new BauteilaufbauModel
            {
                Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Schichten = { S(0.5), S(null) }
            }));
            Assert.True(Bauteilkatalog.MeistUngeeignet(new BauteilaufbauModel
            {
                Bauteilart = DbWerte.BAUTEILART_DACH, Schichten = { S(0) }
            }));
            Assert.True(Bauteilkatalog.MeistUngeeignet(new BauteilaufbauModel { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND }));
            var fenster = new BauteilaufbauModel { Bauteilart = DbWerte.BAUTEILART_FENSTER };
            Assert.True(Bauteilkatalog.MeistUngeeignet(fenster));
            fenster.Zusatz[BauteilkatalogSchema.SPALTE_U_QUELLE] = 1.1;
            Assert.False(Bauteilkatalog.MeistUngeeignet(fenster));
            var wand = new BauteilaufbauModel { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND };
            wand.Zusatz[BauteilkatalogSchema.SPALTE_U_QUELLE] = 0.3;
            Assert.True(Bauteilkatalog.MeistUngeeignet(wand));
        }

        // ---- mit der Testdatenbank ----------------------------------------------------------

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_ohne_Importsatz()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= BauteilkatalogSchema.SCHRITT);
            Assert.True(BauteilkatalogSchema.Vollstaendig());
            Assert.True(BauteilkatalogSchema.Lesbar());
            foreach (string t in BauteilkatalogSchema.KOPFTABELLEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE Herkunft = ?", new DbParam("p1", BauteilkatalogSchema.HERKUNFT_IMPORT)));
            foreach (string t in new[] { BauteilkatalogSchema.TAB_BAUSTOFF_STAMM, BauteilkatalogSchema.TAB_AUFBAU_STAMM })
            {
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE ReadOnly = 1 AND Herkunft IS NOT 'VORGABE'"));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE ReadOnly = 0 AND (Herkunft IS NULL OR Herkunft IN ('MANUELL','VORGABE'))"));
            }
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Bauteilabschnitt_STAMM") + Zahl("SELECT COUNT(*) FROM Tab_Bauteilabschnitt"));
        }

        [Fact]
        public void Gruppe_und_Klasse_der_Testdatenbank_sind_aus_Stoffgruppe_und_Bauteilart_abgeleitet()
        {
            if (!_db.Vorhanden) return;
            DataTable stoffe = DataRepository.GetDataTable("SELECT Gruppe, Gruppe_Code, Klasse_Code FROM Tab_Baustoff_STAMM");
            Assert.True(stoffe.Rows.Count > 0);
            foreach (DataRow r in stoffe.Rows)
            {
                string gruppe = r["Gruppe"] as string;
                Assert.Equal(Bauteilkatalog.GruppeAusStoffgruppe(gruppe), r["Gruppe_Code"] as string);
                Assert.Equal(DBNull.Value, r["Klasse_Code"]);
            }
            DataTable aufbauten = DataRepository.GetDataTable(
                "SELECT Bauteilart, Typaufbau, Gruppe_Code, Klasse_Code, Simulation_Ungeeignet FROM Tab_Bauteilaufbau_STAMM");
            Assert.True(aufbauten.Rows.Count > 0);
            foreach (DataRow r in aufbauten.Rows)
            {
                (string g, string k) = Bauteilkatalog.GliederungAusBauteilart(r["Bauteilart"] as string, r["Typaufbau"] as string);
                Assert.Equal(g, r["Gruppe_Code"] as string);
                Assert.Equal(k, r["Klasse_Code"] as string);
            }
            // Die ausgelieferten Aufbauten haben Schichten mit lambda - keiner ist gekennzeichnet.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Bauteilaufbau_STAMM WHERE ReadOnly = 1 AND Simulation_Ungeeignet = 1"));
        }

        /// <summary>
        /// Der Migrationstest: Die Kopftabellen werden auf den Stand vor dem Schritt zurückgebaut (alte Herkunftsklausel,
        /// ohne neue Spalten, Bestandswerte MANUELL), dazu ein synthetischer Eigensatz je Katalog; der Schritt baut sie neu
        /// — IDs, Zeilen und Fremdschlüssel bleiben, Herkunft, Gruppe und Kennzeichen sind abgeleitet, die Prüfsummen der
        /// Auslieferungssätze stimmen weiter, und ein zweiter Lauf tut nichts.
        /// </summary>
        [Fact]
        public void Der_Schritt_baut_um_erhaelt_IDs_und_Fremdschluessel_leitet_ab_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Rueckbau();
            Assert.False(BauteilkatalogSchema.Vollstaendig());
            Assert.True(BauteilkatalogSchema.Offen() > 0);

            // Ein eigener Baustoff und zwei eigene Aufbauten ohne Schicht (Wand, Fenster) im alten Stand.
            long stoff = Einfuegen("INSERT INTO Tab_Baustoff_STAMM (Bezeichner, Gruppe, Lambda, Rho, cp, Herkunft, ReadOnly) " +
                                   "VALUES ('BK2 Probe Stoff', 'Dämmstoffe', 0.04, 30, 1400, 'MANUELL', 0)");
            long wand = Einfuegen("INSERT INTO Tab_Bauteilaufbau_STAMM (Bezeichner, Bauteilart, ReadOnly) VALUES ('BK2 Probe Wand', 'AUSSENWAND', 0)");
            long fenster = Einfuegen("INSERT INTO Tab_Bauteilaufbau_STAMM (Bezeichner, Bauteilart, Herkunft, ReadOnly) " +
                                     "VALUES ('BK2 Probe Fenster', 'FENSTER', 'SQPROJ', 0)");

            string idsVorher = Kennungen();
            var bericht = new List<string>();
            Assert.True(BauteilkatalogSchema.Ausfuehren(bericht) > 0);
            Assert.True(BauteilkatalogSchema.Vollstaendig(), string.Join(" | ", bericht));
            Assert.Equal(idsVorher, Kennungen());
            Assert.Equal(0, DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows.Count);

            Assert.Equal("EIGEN", Text("SELECT Herkunft FROM Tab_Baustoff_STAMM WHERE ID = ?", stoff));
            Assert.Equal("DAEMMSTOFFE", Text("SELECT Gruppe_Code FROM Tab_Baustoff_STAMM WHERE ID = ?", stoff));
            Assert.Equal("EIGEN", Text("SELECT Herkunft FROM Tab_Bauteilaufbau_STAMM WHERE ID = ?", wand));
            Assert.Equal("SQPROJ", Text("SELECT Herkunft FROM Tab_Bauteilaufbau_STAMM WHERE ID = ?", fenster));
            Assert.Equal("WAENDE", Text("SELECT Gruppe_Code FROM Tab_Bauteilaufbau_STAMM WHERE ID = ?", wand));
            Assert.Equal("AUSSENWAND", Text("SELECT Klasse_Code FROM Tab_Bauteilaufbau_STAMM WHERE ID = ?", wand));
            Assert.Equal(1L, Zahl("SELECT Simulation_Ungeeignet FROM Tab_Bauteilaufbau_STAMM WHERE ID = ?", new DbParam("p1", wand)));
            Assert.Equal(1L, Zahl("SELECT Simulation_Ungeeignet FROM Tab_Bauteilaufbau_STAMM WHERE ID = ?", new DbParam("p1", fenster)));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Baustoff_STAMM WHERE ReadOnly = 1 AND Herkunft IS NOT 'VORGABE'"));
            PruefsummenStimmen(BauteilkatalogSchema.TAB_BAUSTOFF_STAMM);
            PruefsummenStimmen(BauteilkatalogSchema.TAB_AUFBAU_STAMM);

            var zweiter = new List<string>();
            Assert.Equal(0, BauteilkatalogSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.StartsWith("steht bereits", StringComparison.Ordinal));
        }

        [Fact]
        public void Die_Pruefklauseln_weisen_ungueltige_Werte_ab_und_die_Quellkennung_ist_je_Import_eindeutig()
        {
            if (!_db.Vorhanden) return;
            long id = Zahl("SELECT MIN(ID) FROM Tab_Bauteilaufbau_STAMM");
            Assert.ThrowsAny<Exception>(() => Strikt(
                "UPDATE Tab_Bauteilaufbau_STAMM SET Rahmenanteil = 1.5 WHERE ID = ?", new DbParam("p1", id)));
            Assert.ThrowsAny<Exception>(() => Strikt(
                "UPDATE Tab_Bauteilaufbau_STAMM SET Hinterlueftet = 2 WHERE ID = ?", new DbParam("p1", id)));
            Assert.ThrowsAny<Exception>(() => Strikt(
                "UPDATE Tab_Bauteilaufbau_STAMM SET Gruppe_Code = 'UNBEKANNT' WHERE ID = ?", new DbParam("p1", id)));
            Assert.ThrowsAny<Exception>(() => Strikt(
                "UPDATE Tab_Baustoff_STAMM SET Herkunft = 'UNBEKANNT' WHERE ID = (SELECT MIN(ID) FROM Tab_Baustoff_STAMM)"));

            const string guid = "00000000-0000-4000-8000-000000000001";
            Einfuegen("INSERT INTO Tab_Baustoff_STAMM (Bezeichner, Herkunft, Quellkennung, ReadOnly) VALUES ('BK2 Import A', 'IMPORT', '" + guid + "', 0)");
            Assert.ThrowsAny<Exception>(() => Strikt(
                "INSERT INTO Tab_Baustoff_STAMM (Bezeichner, Herkunft, Quellkennung, ReadOnly) VALUES ('BK2 Import B', 'IMPORT', ?, 0)",
                new DbParam("p1", guid)));
            // Dieselbe Kennung an einem eigenen Satz stört nicht.
            Einfuegen("INSERT INTO Tab_Baustoff_STAMM (Bezeichner, Herkunft, Quellkennung, ReadOnly) VALUES ('BK2 Eigen C', 'EIGEN', '" + guid + "', 0)");
        }

        [Fact]
        public void Die_Controller_schreiben_und_lesen_Gliederung_Kennzeichen_und_Felder_und_die_Kopie_nimmt_sie_mit()
        {
            if (!_db.Vorhanden) return;
            var stoffe = new BaustoffCtrl();
            var neuStoff = new BaustoffModel { Bezeichner = "BK2 Probe Daemmung", Gruppe = "Dämmstoffe", Lambda = 0.035, Rho = 20, Cp = 1450, KlasseCode = "POLYSTYROL" };
            neuStoff.Zusatz["Mue_Min"] = 20.0;
            neuStoff.Zusatz["Dampfdicht"] = 0L;
            BaustoffCtrl.Ergebnis e = stoffe.KatalogAnlegen(neuStoff);
            Assert.True(e.Ok, e.Meldung);
            BaustoffModel stoff = stoffe.LesenKatalogsatz(e.Id);
            Assert.Equal("EIGEN", stoff.Herkunft);
            Assert.Equal("DAEMMSTOFFE", stoff.GruppeCode);
            Assert.Equal("POLYSTYROL", stoff.KlasseCode);
            Assert.Equal(20.0, Bauteilkatalog.Zahl(stoff.Zusatz, "Mue_Min"));

            var aufbauten = new BauteilaufbauCtrl();
            var a = new BauteilaufbauModel { Bezeichner = "BK2 Probe Aufbau", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND };
            a.Zusatz[BauteilkatalogSchema.SPALTE_U_QUELLE] = 0.25;
            a.Zusatz["Rsi"] = 0.13;
            var schicht = new BauteilschichtModel { Dicke = 0.2, ID_Baustoff = e.Id };
            schicht.Zusatz["Emissionsgrad"] = 0.9;
            schicht.Zusatz["Quellkennung"] = "00000000-0000-4000-8000-000000000002";
            a.Schichten.Add(schicht);
            a.Schichten.Add(new BauteilschichtModel { Dicke = 0.01 });     // ohne lambda
            BauteilaufbauCtrl.Ergebnis ea = aufbauten.KatalogSpeichern(a);
            Assert.True(ea.Ok, ea.Meldung);

            BauteilaufbauModel gelesen = aufbauten.LesenKatalogsatz(ea.Id);
            Assert.Equal("EIGEN", gelesen.Herkunft);
            Assert.Equal(("WAENDE", "AUSSENWAND"), (gelesen.GruppeCode, gelesen.KlasseCode));
            Assert.True(gelesen.SimulationUngeeignet);
            Assert.Equal(0.25, Bauteilkatalog.Zahl(gelesen.Zusatz, BauteilkatalogSchema.SPALTE_U_QUELLE));
            Assert.Equal(0.9, Bauteilkatalog.Zahl(gelesen.Schichten[0].Zusatz, "Emissionsgrad"));

            // Ohne die Schicht ohne lambda ist der Aufbau geeignet.
            gelesen.Schichten.RemoveAt(1);
            Assert.True(aufbauten.KatalogSpeichern(gelesen).Ok);
            Assert.False(aufbauten.LesenKatalogsatz(ea.Id).SimulationUngeeignet);

            long projekt = Zahl("SELECT MIN(ID) FROM Tab_Projekt");
            int kopie = aufbauten.CopyFromStamm(ea.Id, (int)projekt);
            Assert.True(kopie > 0);
            BauteilaufbauModel k = aufbauten.LesenProjektsatz(kopie);
            Assert.Equal(0.25, Bauteilkatalog.Zahl(k.Zusatz, BauteilkatalogSchema.SPALTE_U_QUELLE));
            Assert.Equal(0.9, Bauteilkatalog.Zahl(k.Schichten[0].Zusatz, "Emissionsgrad"));
            Assert.Equal("WAENDE", k.GruppeCode);
            Assert.Equal(20.0, Zahl("SELECT CAST(Mue_Min AS INTEGER) FROM Tab_Baustoff WHERE ID = ?",
                                    new DbParam("p1", k.Schichten[0].ID_Baustoff ?? 0)));
        }

        // ---- Hilfen ------------------------------------------------------------------------

        private static long Einfuegen(string sql)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                int id = v.EinfuegenUndId(sql, Array.Empty<DbParam>());
                v.Commit();
                return id;
            }
        }

        /// <summary>Eine Anweisung, die bei einem Fehler wirft (der Vorgang meldet ihn, anders als die stille Zugriffsschicht).</summary>
        private static void Strikt(string sql, params DbParam[] p)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                v.Ausfuehren(sql, p);
                v.Commit();
            }
        }

        private static string Text(string sql, long id)
            => DataRepository.ExecuteScalar(sql, new DbParam("p1", id)) as string;

        /// <summary>Die IDs aller beteiligten Zeilen samt ihrer Verweise — ein Fingerabdruck vor und nach dem Schritt.</summary>
        private static string Kennungen()
        {
            var teile = new List<string>();
            foreach (string sql in new[]
                     {
                         "SELECT group_concat(ID) FROM (SELECT ID FROM Tab_Baustoff_STAMM ORDER BY ID)",
                         "SELECT group_concat(ID) FROM (SELECT ID FROM Tab_Baustoff ORDER BY ID)",
                         "SELECT group_concat(ID) FROM (SELECT ID FROM Tab_Bauteilaufbau_STAMM ORDER BY ID)",
                         "SELECT group_concat(ID) FROM (SELECT ID FROM Tab_Bauteilaufbau ORDER BY ID)",
                         "SELECT group_concat(x) FROM (SELECT ID || ':' || ID_Aufbau || ':' || IFNULL(ID_Baustoff, '-') AS x FROM Tab_Bauteilschicht_STAMM ORDER BY ID)",
                         "SELECT group_concat(x) FROM (SELECT ID || ':' || IFNULL(ID_Aufbau, '-') AS x FROM Tab_Bauteil ORDER BY ID)",
                         "SELECT group_concat(x) FROM (SELECT ID || ':' || ID_Baustoff AS x FROM Tab_Baustoffsynonym_STAMM ORDER BY ID)",
                         "SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Baustoff_STAMM'",
                         "SELECT seq FROM sqlite_sequence WHERE name = 'Tab_Bauteilaufbau_STAMM'"
                     })
                teile.Add(Convert.ToString(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture));
            return string.Join("|", teile);
        }

        private static void PruefsummenStimmen(string tabelle)
        {
            Katalogtabelle t = Katalogfassung.Tabelle(tabelle);
            List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
            DataTable dt = DataRepository.GetDataTable("SELECT ID, Katalog_Pruefsumme, " + Katalogfassung.Spaltentext(spalten) +
                                                       " FROM \"" + tabelle + "\" WHERE Katalog_Pruefsumme IS NOT NULL");
            Assert.True(dt.Rows.Count > 0);
            foreach (DataRow r in dt.Rows)
                Assert.Equal(r["Katalog_Pruefsumme"] as string,
                             Katalogfassung.PruefsummeDerZeile(t, spalten, r, Katalogfassung.LeseOhneVorgang));
        }

        /// <summary>
        /// Baut die Kopftabellen auf den Stand vor dem Schritt zurück: neue Spalten und Teilindizes weg, Herkunft der
        /// Auslieferung bleibt VORGABE, eigene Sätze werden MANUELL, die alte Herkunfts- und Quellklausel kehrt zurück.
        /// Die Schichtspalten ohne Verweis fallen ebenfalls; Abschnittstabellen und Verweis bleiben (der Verweis lässt sich
        /// nicht abwerfen) - der Schritt muss auch den halben Stand nachziehen.
        /// </summary>
        private static void Rueckbau()
        {
            foreach (string t in BauteilkatalogSchema.KOPFTABELLEN)
                Strikt("DROP INDEX IF EXISTS \"" + BauteilkatalogSchema.Importindex(t) + "\"");
            foreach (string t in BauteilkatalogSchema.SPALTENTABELLEN)
                foreach (BauteilkatalogSchema.Spalte s in BauteilkatalogSchema.SpaltenVon(t).Reverse())
                    if (s.Art != BauteilkatalogSchema.Spaltenart.Verweis)
                        Strikt("ALTER TABLE \"" + t + "\" DROP COLUMN \"" + s.Name + "\"");
            foreach (string t in new[] { BauteilkatalogSchema.TAB_BAUSTOFF_STAMM, BauteilkatalogSchema.TAB_AUFBAU_STAMM })
                Strikt("UPDATE \"" + t + "\" SET Herkunft = 'MANUELL' WHERE Herkunft IN ('EIGEN','IMPORT')");
            foreach (string t in new[] { BauteilkatalogSchema.TAB_BAUSTOFF, BauteilkatalogSchema.TAB_AUFBAU })
                Strikt("UPDATE \"" + t + "\" SET Herkunft = 'MANUELL' WHERE Herkunft IN ('EIGEN','IMPORT')");
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                foreach (string t in BauteilkatalogSchema.KOPFTABELLEN)
                {
                    string text = Convert.ToString(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                            new DbParam("p1", t)), CultureInfo.InvariantCulture);
                    string alt = text.Replace(BauteilkatalogSchema.CHECK_HERKUNFT_NEU, BauteilkatalogSchema.CHECK_HERKUNFT_ALT)
                                     .Replace(BauteilkatalogSchema.CHECK_QUELLE_NEU, BauteilkatalogSchema.CHECK_QUELLE_ALT);
                    Assert.NotEqual(text, alt);
                    TwwBezugsartSchema.Neubau(v, t, alt, t + "_rueckbau");
                }
                v.Commit();
            }
        }
    }
}
