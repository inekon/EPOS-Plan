using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die sechs KWKG-Projektspalten fallen weg</b> — Schemaschritt 90 (DDL-Teil), der
    /// Nachweis zu <see cref="KwkgProjektaltspalten"/>.
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Schritt 89 und Etappe BK1a haben sechs
    /// Spalten ohne Leser zurueckgelassen: Der Regelweg je Anlage und der Ersatzweg
    /// (leistungsgewichtete virtuelle Gesamtanlage) holen Satz, Kontingent, Deckel,
    /// Tatbestand und Anlagenart beide aus <c>Tab_Energieanlagen</c>. Der Schritt entfernt
    /// sie; dass dabei kein Wert und keine Zeile verloren geht, ist am Referenzlauf nicht
    /// abzulesen — er rechnet mit keiner von ihnen. Geprueft wird deshalb hier.</para>
    ///
    /// <para><b>Diese Klasse SCHREIBT</b> und braucht ihre eigene Arbeitskopie;
    /// <see cref="TestDatenbank"/> als <c>IClassFixture</c> legt je Testklasse eine an.
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> bleibt unberuehrt. Fehlt die Datei,
    /// schweigen die Faelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwkgProjektaltspaltenTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public KwkgProjektaltspaltenTests(TestDatenbank db) { _db = db; }

        /// <summary>
        /// Der ganze Schritt in EINEM Fall: Die Arbeitskopie steht bereits auf dem
        /// Zielstand, der Fall stellt den Ausgangszustand deshalb selbst her (die sechs
        /// Spalten zurueck) und faehrt dann DIESELBEN Anweisungen, die Migration und
        /// Werkzeug fahren.
        /// </summary>
        [Fact]
        public void Der_Schritt_entfernt_genau_die_sechs_Spalten()
        {
            if (!_db.Vorhanden) return;

            TestDatenbank.AltspaltenKwkgProjektWiederherstellen();

            Assert.Equal(6, KwkgProjektaltspalten.Offen());
            foreach (KeyValuePair<string, string> s in KwkgProjektaltspalten.Spalten)
                Assert.True(KwkgProjektaltspalten.Vorhanden(s.Key, s.Value), s.Key + "." + s.Value);

            long zeilen = Zahl("SELECT COUNT(*) FROM \"" + KwkgProjektaltspalten.TABELLE + "\"");

            foreach (KeyValuePair<string, string> a in KwkgProjektaltspalten.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);

            // --- Die sechs sind weg ...
            Assert.Equal(0, KwkgProjektaltspalten.Offen());
            foreach (KeyValuePair<string, string> s in KwkgProjektaltspalten.Spalten)
                Assert.False(KwkgProjektaltspalten.Vorhanden(s.Key, s.Value), s.Key + "." + s.Value);

            // --- ... und sonst nichts: keine Zeile verloren, die Nachbarn stehen.
            Assert.Equal(zeilen, Zahl("SELECT COUNT(*) FROM \"" +
                                      KwkgProjektaltspalten.TABELLE + "\""));

            foreach (string nachbar in new[]
                     {
                         SchemaKatalog.SPALTE_PW_KWKG_KOSTENANTEIL,
                         SchemaKatalog.SPALTE_PW_KWKG_PAUSCHALMODUS,
                         "KWKG_Abschlag_Negativ",
                         SchemaKatalog.SPALTE_PW_KWKG_STICHTAG,
                         SchemaKatalog.SPALTE_PW_KWKG_INBETRIEBNAHME
                     })
                Assert.True(DataRepository.SpalteVorhanden(KwkgProjektaltspalten.TABELLE, nachbar),
                            nachbar + " ist mitgefallen.");

            // --- Wiederholbar: Ein zweiter Lauf gibt keine Anweisung mehr heraus.
            Assert.Empty(KwkgProjektaltspalten.Anweisungen);
        }

        /// <summary>
        /// Auf dem Zielstand ist der Schritt gelaufen: keine der sechs Spalten steht mehr,
        /// und die Liste gibt nichts mehr her. So findet ihn auch die Migration vor, wenn
        /// sie ein zweites Mal ueber dieselbe Datei geht.
        /// </summary>
        [Fact]
        public void Auf_dem_Zielstand_ist_nichts_mehr_zu_tun()
        {
            if (!_db.Vorhanden) return;

            foreach (KeyValuePair<string, string> a in KwkgProjektaltspalten.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);

            Assert.Equal(0, KwkgProjektaltspalten.Offen());
            Assert.Empty(KwkgProjektaltspalten.Anweisungen);
        }

        /// <summary>
        /// Die Anweisung ist ein <c>DROP COLUMN</c> auf genau diese Tabelle und Spalte —
        /// kein Tabellenneubau, kein DML. Der Spaltenname steht nur als Argument darin
        /// (Begruendung im Kopfblock der Quelle).
        /// </summary>
        [Fact]
        public void Die_Anweisungen_sind_sechs_DROP_COLUMN_in_fester_Reihenfolge()
        {
            var spalten = new List<KeyValuePair<string, string>>(KwkgProjektaltspalten.Spalten);

            Assert.Equal(6, spalten.Count);
            Assert.Equal(KwkgProjektaltspalten.SPALTE_BONUS, spalten[0].Value);
            Assert.Equal(KwkgProjektaltspalten.SPALTE_BONUS_EINSPEISUNG, spalten[1].Value);
            Assert.Equal(KwkgProjektaltspalten.SPALTE_KONTINGENT, spalten[2].Value);
            Assert.Equal(KwkgProjektaltspalten.SPALTE_JAHRESDECKEL, spalten[3].Value);
            Assert.Equal(KwkgProjektaltspalten.SPALTE_TATBESTAND, spalten[4].Value);
            Assert.Equal(KwkgProjektaltspalten.SPALTE_ANLAGENART, spalten[5].Value);

            foreach (KeyValuePair<string, string> s in spalten)
                Assert.Equal(SchemaKatalog.TAB_PROJEKTWIRTSCHAFT, s.Key);

            if (!_db.Vorhanden) return;

            TestDatenbank.AltspaltenKwkgProjektWiederherstellen();
            foreach (KeyValuePair<string, string> a in KwkgProjektaltspalten.Anweisungen)
            {
                Assert.StartsWith("ALTER TABLE \"", a.Value);
                Assert.Contains("\" DROP COLUMN \"", a.Value);
                Assert.DoesNotContain("UPDATE ", a.Value);
                Assert.DoesNotContain("CREATE TABLE", a.Value);
            }
            foreach (KeyValuePair<string, string> a in KwkgProjektaltspalten.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);
        }

        /// <summary>
        /// <b>Der Migrationslauf auf einer Altdatei — 89 überträgt, 90 entfernt.</b>
        ///
        /// <para>Der Fall stellt den Stand VOR Schritt 89 her: die sechs Projektspalten
        /// mit ihren Werten, die Anlagenzellen leer. Dann fährt er DIESELBEN Quellen in
        /// DERSELBEN Reihenfolge, die auch <c>SchemaMigration</c> und
        /// <c>Werkzeuge/Testdatenbankschema</c> fahren — <see cref="KwkAnlagenwahrheit"/>,
        /// dann <see cref="KwkgProjektaltspalten"/>. Danach tragen die Anlagen die Werte,
        /// die Spalten sind weg, und ein zweiter Lauf fasst nichts an.</para>
        ///
        /// <para><b>Warum das die Reihenfolge beweist:</b> Schritt 89 liest die sechs
        /// Spalten. Stünde 90 davor, fände 89 sie nicht mehr, und keine Anlage bekäme
        /// ihre Werte — der Fall prüft deshalb die Übertragung und nicht nur den
        /// Drop.</para>
        /// </summary>
        [Fact]
        public void Auf_einer_Altdatei_uebertraegt_89_und_90_entfernt()
        {
            if (!_db.Vorhanden) return;

            const int projekt = 1030;

            // ---- Der Stand VOR Schritt 89 ----
            TestDatenbank.AltspaltenKwkgProjektWiederherstellen();
            DataRepository.ExecuteNonQuery(
                "UPDATE " + KwkgProjektaltspalten.TABELLE + " SET " +
                KwkgProjektaltspalten.SPALTE_BONUS + " = 4.0, " +
                KwkgProjektaltspalten.SPALTE_BONUS_EINSPEISUNG + " = 8.0, " +
                KwkgProjektaltspalten.SPALTE_KONTINGENT + " = 30000.0 " +
                "WHERE ID_Projekt = ?", new DbParam("@p", projekt));
            DataRepository.ExecuteNonQuery(
                "UPDATE " + SchemaKatalog.TAB_ENERGIEANLAGEN + " SET " +
                SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN + " = NULL, " +
                SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP + " = NULL, " +
                SchemaKatalog.SPALTE_EA_KWKG_KONTINGENT + " = NULL " +
                "WHERE ID_Projekt = ?", new DbParam("@p", projekt));

            Assert.Equal(6, KwkgProjektaltspalten.Offen());

            // ---- Schritt 89: die Uebertragung ----
            int getroffen = 0;
            foreach (KwkAnlagenwahrheit.Paar paar in KwkAnlagenwahrheit.Paare)
            {
                object o = DataRepository.ExecuteScalar(KwkAnlagenwahrheit.Zaehlung(paar));
                long offen = o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o);
                if (offen <= 0) continue;
                getroffen += (int)offen;
                DataRepository.ExecuteNonQuery(KwkAnlagenwahrheit.Uebertragung(paar));
            }
            Assert.True(getroffen > 0, "Schritt 89 hat keine Zelle nachgetragen.");

            Assert.Equal(4.0, Wert(projekt, SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN), 9);
            Assert.Equal(8.0, Wert(projekt, SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP), 9);
            Assert.Equal(30000.0, Wert(projekt, SchemaKatalog.SPALTE_EA_KWKG_KONTINGENT), 9);

            // ---- Schritt 90: der Drop ----
            foreach (KeyValuePair<string, string> a in KwkgProjektaltspalten.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value);
            Assert.Equal(0, KwkgProjektaltspalten.Offen());

            // ---- Der zweite Lauf fasst nichts an ----
            Assert.Empty(KwkgProjektaltspalten.Anweisungen);
        }

        /// <summary>
        /// Der Zielstand des Schemas ist 90 — die Nummer des Schritts. Laeuft er nicht
        /// mit, staende die Messlatte auf einer Version, die es nicht gibt.
        /// </summary>
        [Fact]
        public void Der_Zielstand_traegt_den_Schritt_90()
        {
            Assert.True(SchemaStand.Zielversion >= 90,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 90.");
        }

        /// <summary>
        /// <b>Schritt 90 steht HINTER Schritt 89.</b> Schritt 89 liest die sechs Spalten
        /// als Quelle seiner Uebertragung; liefe 90 zuerst, meldete 89 auf einer Altdatei
        /// „no such column" und jede Anlage bliebe ohne ihre Werte.
        ///
        /// <para>Geprueft wird an der QUELLE der Migration, weil ihre Schrittliste in der
        /// Windows-Schale liegt und privat ist — derselbe Weg, den
        /// <see cref="DiensteSammlungTests"/> fuer die Sammlungszugehoerigkeit geht.</para>
        /// </summary>
        [Fact]
        public void Schritt_90_steht_hinter_Schritt_89()
        {
            string datei = Migrationsquelle();
            if (datei == null) return;            // Quelle nicht im Baum: nichts zu pruefen

            string text = File.ReadAllText(datei);

            int ort89 = text.IndexOf("new Schritt(SCHRITT_89_KWK_ANLAGENWAHRHEIT", StringComparison.Ordinal);
            int ort90 = text.IndexOf("new Schritt(SCHRITT_90_KWKG_PROJEKTALTSPALTEN", StringComparison.Ordinal);

            Assert.True(ort89 > 0, "Schritt 89 steht nicht in der Schrittliste.");
            Assert.True(ort90 > 0, "Schritt 90 steht nicht in der Schrittliste.");
            Assert.True(ort90 > ort89,
                        "Schritt 90 steht VOR Schritt 89 - Schritt 89 liest die sechs Spalten.");
        }

        // =================================================================
        //  Werkzeug
        // =================================================================

        /// <summary><c>WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs</c>,
        /// aufwaerts gesucht; <c>null</c>, wenn der Arbeitsbaum nicht danebensteht.</summary>
        private static string Migrationsquelle([CallerFilePath] string eigeneDatei = null)
        {
            var kandidaten = new List<string>();
            if (!string.IsNullOrEmpty(eigeneDatei))
                kandidaten.Add(Path.GetDirectoryName(eigeneDatei));
            kandidaten.Add(AppContext.BaseDirectory);

            foreach (string start in kandidaten)
            {
                DirectoryInfo d = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
                while (d != null)
                {
                    string p = Path.Combine(d.FullName, "WindowsFormsApplication1",
                                            "Allgemein", "Update", "SchemaMigration.cs");
                    if (File.Exists(p)) return p;
                    d = d.Parent;
                }
            }
            return null;
        }

        /// <summary>Der Wert einer KWKG-Anlagenspalte der ERSTEN BHKW-Anlage des
        /// Projekts; 0, wenn keine da ist.</summary>
        private static double Wert(int projekt, string spalte)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(IFNULL(\"" + spalte + "\", 0)) FROM " +
                SchemaKatalog.TAB_ENERGIEANLAGEN +
                " WHERE ID_Projekt = ? AND \"" + spalte + "\" IS NOT NULL",
                new DbParam("@p", projekt));
            return o == null || o == DBNull.Value ? 0 : Convert.ToDouble(o);
        }

        private static long Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o);
        }
    }
}
