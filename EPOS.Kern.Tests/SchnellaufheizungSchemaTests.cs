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
    /// <b>Der Schemaschritt der Schnellaufheizung</b> (<see cref="SchnellaufheizungSchema"/>; Welle SA2, E128): Nummer und
    /// Kette, Spalten und Prüfklauseln, die Testkopie auf dem Schritt (alles NULL), Wiederholbarkeit; der Datenweg — Schalter und
    /// Überhöhung lesen und schreiben, fehlende Spalte = aus —, das Ergebnisschreiben (NULL ohne Wirkung, Werte an Gebäude und
    /// Zone mit Wirkung) und die Datenwege Duplizieren, Variante und Projektpaket.
    /// </summary>
    [Collection("Testdatenbank")]
    public class SchnellaufheizungSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // =================================================================
        //  Nummer, Spalten, Klauseln (ohne Datenbank)
        // =================================================================

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_ist_das_Ziel_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(217, SchnellaufheizungSchema.SCHRITT);
            Assert.Equal(KaeltemittelSchema.SCHRITT + 1, SchnellaufheizungSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= SchnellaufheizungSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == SchnellaufheizungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Die_Spalten_sind_benannt_nullbar_und_an_beiden_Ergebnistabellen_gleich()
        {
            Assert.Equal(new[] { "Aufheiz_Schnellaufheizung", "Aufheiz_Ueberhoehung_K" },
                         SchnellaufheizungSchema.SPALTEN_EINSTELLUNG.Select(s => s.Spalte));
            Assert.All(SchnellaufheizungSchema.SPALTEN_EINSTELLUNG, s => Assert.Equal("Tab_Einstellungen", s.Tabelle));
            Assert.Equal(new[]
            {
                "Vorheiz_Ueberhoehung_K", "Vorheiz_Ueberhoehung_H", "Vorheiz_Ueberhoehung_Vorlauf_Max_C", "Vorheiz_Vorlaufgrenze_H",
                "Vorheiz_Ueberhoehung_Wirkungslos_H", "Vorheiz_Ueberhoehung_Sperrzeit_H"
            }, SchnellaufheizungSchema.ERGEBNISSPALTEN.Select(s => s.Spalte));
            Assert.Equal(2 + 2 * 6, SchnellaufheizungSchema.SPALTEN.Count);
            Assert.All(SchnellaufheizungSchema.SPALTEN, s => Assert.DoesNotContain("NOT NULL", s.Typ, StringComparison.Ordinal));
            Assert.All(SchnellaufheizungSchema.SPALTEN, s => Assert.DoesNotContain("DEFAULT", s.Typ, StringComparison.Ordinal));
            Assert.Equal(VorheizenSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE + 6, SchnellaufheizungSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE);
            Assert.Equal(VorheizenSchema.SPALTENZAHL_ERGEBNIS_ZONE + 6, SchnellaufheizungSchema.SPALTENZAHL_ERGEBNIS_ZONE);
            // Keine Spalte überschneidet sich mit dem Vorgängerschritt.
            Assert.Empty(SchnellaufheizungSchema.SPALTEN.Select(s => s.Tabelle + "." + s.Spalte)
                         .Intersect(VorheizenSchema.SPALTEN.Select(s => s.Tabelle + "." + s.Spalte)));
        }

        [Fact]
        public void Die_Pruefklauseln_halten_Schalter_und_Bereiche()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            foreach (string t in new[] { "Tab_Einstellungen", "Tab_ErgebnisGebaeude", "Tab_ErgebnisZone" })
            {
                Ausfuehren(c, "CREATE TABLE \"" + t + "\" (ID INTEGER PRIMARY KEY) STRICT");
                Ausfuehren(c, "INSERT INTO \"" + t + "\" (ID) VALUES (1)");
            }
            foreach (var s in SchnellaufheizungSchema.SPALTEN) Ausfuehren(c, SchnellaufheizungSchema.Anlegen(s));

            foreach (string gut in new[]
                     {
                         "Aufheiz_Schnellaufheizung = 0", "Aufheiz_Schnellaufheizung = 1", "Aufheiz_Schnellaufheizung = NULL",
                         "Aufheiz_Ueberhoehung_K = 0.1", "Aufheiz_Ueberhoehung_K = 10", "Aufheiz_Ueberhoehung_K = NULL"
                     })
                Ausfuehren(c, "UPDATE Tab_Einstellungen SET " + gut);
            foreach (string schlecht in new[]
                     {
                         "Aufheiz_Schnellaufheizung = 2", "Aufheiz_Schnellaufheizung = -1", "Aufheiz_Ueberhoehung_K = 0",
                         "Aufheiz_Ueberhoehung_K = -1", "Aufheiz_Ueberhoehung_K = 10.01"
                     })
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE Tab_Einstellungen SET " + schlecht));

            foreach (string t in new[] { "Tab_ErgebnisGebaeude", "Tab_ErgebnisZone" })
            {
                Ausfuehren(c, "UPDATE " + t + " SET Vorheiz_Ueberhoehung_K = 2, Vorheiz_Ueberhoehung_H = 20000, " +
                              "Vorheiz_Ueberhoehung_Vorlauf_Max_C = -5.5, Vorheiz_Vorlaufgrenze_H = 0, " +
                              "Vorheiz_Ueberhoehung_Wirkungslos_H = 3, Vorheiz_Ueberhoehung_Sperrzeit_H = 4");
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE " + t + " SET Vorheiz_Ueberhoehung_K = 0"));
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE " + t + " SET Vorheiz_Ueberhoehung_K = 11"));
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE " + t + " SET Vorheiz_Ueberhoehung_H = -1"));
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE " + t + " SET Vorheiz_Vorlaufgrenze_H = 1.5"));
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE " + t + " SET Vorheiz_Ueberhoehung_Sperrzeit_H = -2"));
            }
        }

        // =================================================================
        //  Testkopie und Wiederholbarkeit
        // =================================================================

        [Fact]
        public void Die_Testkopie_steht_auf_dem_Schritt_und_jede_Zelle_ist_leer()
        {
            if (!_db.Vorhanden) return;
            Assert.True(SchnellaufheizungSchema.Vollstaendig());
            Assert.Equal(0, SchnellaufheizungSchema.ZeilenMitSchalter());
            foreach (var s in SchnellaufheizungSchema.SPALTEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + s.Tabelle + "\" WHERE \"" + s.Spalte + "\" IS NOT NULL"));
            Assert.Equal(SchnellaufheizungSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE,
                         DataRepository.SpaltenVonTabelle(SchnellaufheizungSchema.TAB_ERGEBNIS_GEBAEUDE).Count);
            Assert.Equal(SchnellaufheizungSchema.SPALTENZAHL_ERGEBNIS_ZONE,
                         DataRepository.SpaltenVonTabelle(SchnellaufheizungSchema.TAB_ERGEBNIS_ZONE).Count);
            Assert.Equal((long)SchemaStand.Zielversion, Zahl("SELECT SchemaVersion FROM Tab_Applikation"));
        }

        [Fact]
        public void Der_Schritt_ist_wiederholbar_und_ohne_Spalten_ist_die_Schnellaufheizung_aus()
        {
            if (!_db.Vorhanden) return;
            var zweiter = new List<string>();
            Assert.Equal(0, SchnellaufheizungSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("nichts zu tun", StringComparison.Ordinal));

            const int projekt = 1065;   // trägt „Vorheizzeit berechnet“
            Vorheizvorgabe bestand = KonfigurationCtrl.VorheizvorgabeLesen(projekt);
            Assert.False(bestand.Schnellaufheizung);
            Assert.Null(bestand.UeberhoehungK);

            foreach (var s in SchnellaufheizungSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
            Assert.False(SchnellaufheizungSchema.Vollstaendig());
            // Ohne die Spalten: dieselbe Vorgabe wie vorher (aus); schreiben ohne Schalter geht, mit Schalter nicht.
            Assert.Equal(bestand, KonfigurationCtrl.VorheizvorgabeLesen(projekt));
            Aufheizvorgabe av = KonfigurationCtrl.AufheizvorgabeLesen(projekt);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSchreiben(projekt, av));
            Assert.False(KonfigurationCtrl.AufheizvorgabeSchreiben(projekt, av with { Vorheizen = av.Vorheizen.MitSchnellaufheizung(true) }));

            Assert.Equal(SchnellaufheizungSchema.SPALTEN.Count, SchnellaufheizungSchema.Ausfuehren(null));
            Assert.True(SchnellaufheizungSchema.Vollstaendig());
            Assert.Equal(0, SchnellaufheizungSchema.ZeilenMitSchalter());
        }

        // =================================================================
        //  Datenweg: Einstellung
        // =================================================================

        [Fact]
        public void Schalter_und_Ueberhoehung_gehen_in_die_Spalten_und_kommen_gleich_zurueck()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1065;
            Aufheizvorgabe bestand = KonfigurationCtrl.AufheizvorgabeLesen(projekt);
            Assert.Equal(Aufheizverfahren.Berechnet, bestand.Vorheizen.Verfahren);

            Vorheizvorgabe mit = bestand.Vorheizen.MitSchnellaufheizung(true, 3.5);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, bestand with { Vorheizen = mit }));
            Vorheizvorgabe gelesen = KonfigurationCtrl.AufheizvorgabeLesen(projekt).Vorheizen;
            Assert.Equal(mit, gelesen);
            Assert.True(gelesen.SchnellaufheizungWirksam);
            Assert.Equal(1L, Zahl("SELECT Aufheiz_Schnellaufheizung FROM Tab_Einstellungen WHERE ID_Projekt = 1065"));
            Assert.Equal(3.5, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Aufheiz_Ueberhoehung_K FROM Tab_Einstellungen WHERE ID_Projekt = 1065"), CultureInfo.InvariantCulture));
            Assert.Equal(1, SchnellaufheizungSchema.ZeilenMitSchalter());

            // Schalter an mit Kern-Vorgabe: die Überhöhung bleibt NULL.
            Vorheizvorgabe vorgabe = bestand.Vorheizen.MitSchnellaufheizung(true);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, bestand with { Vorheizen = vorgabe }));
            Assert.Equal(vorgabe, KonfigurationCtrl.VorheizvorgabeLesen(projekt));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Aufheiz_Ueberhoehung_K IS NOT NULL"));
            Assert.Equal(Vorheizvorgabe.UEBERHOEHUNG_VORGABE_K, KonfigurationCtrl.VorheizvorgabeLesen(projekt).UeberhoehungWirksamK);

            // Zurück auf aus: beide Spalten wieder NULL, der Bestand ist wieder gleich.
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, bestand));
            foreach (var s in SchnellaufheizungSchema.SPALTEN_EINSTELLUNG)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE \"" + s.Spalte + "\" IS NOT NULL"));
            Assert.Equal(bestand, KonfigurationCtrl.AufheizvorgabeLesen(projekt));
        }

        /// <summary>
        /// <b>Die Hüllen (SA3a)</b>: Die Naht der Ergebnishülle (<c>AufheizvorgabeSchreiben</c>, der Weg der Gruppe „Aufheizen vor
        /// Nutzungsbeginn“) schreibt Schalter und Überhöhung, und das Speichern der Kaskade (Delete + Insert in
        /// <c>SimulationKonfigHuelle</c>) trägt beide mit — auch „aus“ mit gepflegter Überhöhung.
        /// </summary>
        [Fact]
        public void Die_Huellen_schreiben_die_Schnellaufheizung_und_das_Speichern_der_Kaskade_traegt_sie_mit()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1065;
            IEinstellungen vorher = Dienste.Einstellungen;
            Dienste.Einstellungen = new FluechtigeEinstellungen();
            try
            {
                Aufheizvorgabe bestand = KonfigurationCtrl.AufheizvorgabeLesen(projekt);
                EPOS.UI.Seiten.Simulation.SimulationParameterDienste wege =
                    SimulationErgebnisHuelle.Erzeugen(null, projekt, new BedarfsZustand()).ParameterGaben();
                foreach (Vorheizvorgabe vh in new[]
                         {
                             bestand.Vorheizen.MitSchnellaufheizung(true, 4.5),
                             bestand.Vorheizen.MitSchnellaufheizung(true),
                             bestand.Vorheizen.MitSchnellaufheizung(false, 2.5),
                         })
                {
                    Aufheizvorgabe v = bestand.MitVorheizen(vh);
                    Assert.True(wege.AufheizvorgabeSchreiben(v));
                    Assert.Equal(v, KonfigurationCtrl.AufheizvorgabeLesen(projekt));
                    var dienste = (EPOS.UI.Seiten.Simulation.SimulationKonfigDienste)
                        SimulationKonfigHuelle.Erzeugen(projekt).Gaben()["Dienste"];
                    Assert.True(dienste.Speichern());
                    Assert.Equal(v, KonfigurationCtrl.AufheizvorgabeLesen(projekt));
                }
            }
            finally
            {
                Dienste.Einstellungen = vorher;
            }
        }

        [Fact]
        public void Schalter_null_oder_null_ist_aus_und_ein_ungueltiger_Satz_die_Sollwertrampe()
        {
            if (!_db.Vorhanden) return;
            Vorheizvorgabe bestand = KonfigurationCtrl.VorheizvorgabeLesen(1065);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET Aufheiz_Schnellaufheizung = 0 WHERE ID_Projekt = 1065");
            Assert.Equal(bestand, KonfigurationCtrl.VorheizvorgabeLesen(1065));
            // Eine Überhöhung ohne Schalter reist mit, wirkt aber nicht.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET Aufheiz_Ueberhoehung_K = 4 WHERE ID_Projekt = 1065");
            Vorheizvorgabe v = KonfigurationCtrl.VorheizvorgabeLesen(1065);
            Assert.False(v.SchnellaufheizungWirksam);
            Assert.Equal(4.0, v.UeberhoehungK);
            // Projekt mit Sollwertrampe: Schalter an wirkt nicht.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET Aufheiz_Schnellaufheizung = 1 WHERE ID_Projekt = 1051");
            Vorheizvorgabe r = KonfigurationCtrl.VorheizvorgabeLesen(1051);
            Assert.Equal(Aufheizverfahren.Sollwertrampe, r.Verfahren);
            Assert.True(r.Schnellaufheizung);
            Assert.False(r.SchnellaufheizungWirksam);
        }

        // =================================================================
        //  Ergebnis: ohne Wirkung NULL, mit Wirkung Werte
        // =================================================================

        [Fact]
        public void Ein_Lauf_ohne_Schnellaufheizung_laesst_die_Spalten_leer()
        {
            if (!_db.Vorhanden) return;
            Lauf(1065);
            foreach (var s in SchnellaufheizungSchema.SPALTEN_ERGEBNIS)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + s.Tabelle + "\" WHERE \"" + s.Spalte + "\" IS NOT NULL"));
            ErgebnisGebaeudeModel g = new ErgebnisCtrl().Load(1065).Gebaeude.First(x => x.Vorheizen != null);
            Assert.Null(g.Vorheizen.UeberhoehungK);
            Assert.Null(g.Vorheizen.UeberhoehungH);
        }

        [Fact]
        public void Ein_Lauf_mit_Schnellaufheizung_schreibt_die_Groessen_an_Gebaeude_und_Zone()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1054;   // AK1, drei Zonen, Heizkreis mit Heizkurve
            Aufheizvorgabe an = VorheizAk3Tests.ProjektvorgabeAn(projekt);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, an with
            {
                Vorheizen = new Vorheizvorgabe(Aufheizverfahren.Berechnet).MitSchnellaufheizung(true),
            }));

            int kopf = Lauf(projekt);
            ErgebnisGebaeudeModel g = new ErgebnisCtrl().Load(projekt).Gebaeude.Single(x => x.Zonen.Count >= 2);
            Assert.NotNull(g.Vorheizen);
            Assert.Equal(Vorheizvorgabe.UEBERHOEHUNG_VORGABE_K, g.Vorheizen.UeberhoehungK);
            Assert.True(g.Vorheizen.UeberhoehungH > 0);
            Assert.NotNull(g.Vorheizen.UeberhoehungVorlaufMaxC);
            Assert.NotNull(g.Vorheizen.VorlaufgrenzeH);
            Assert.NotNull(g.Vorheizen.UeberhoehungWirkungslosH);
            Assert.NotNull(g.Vorheizen.UeberhoehungSperrzeitH);
            List<ErgebnisZoneModel> wirksam = g.Zonen.Where(z => z.Vorheizen?.UeberhoehungK != null).ToList();
            Assert.NotEmpty(wirksam);
            Assert.Equal(g.Vorheizen.UeberhoehungH, g.Zonen.Sum(z => z.Vorheizen?.UeberhoehungH ?? 0));
            Assert.Equal(g.Vorheizen.UeberhoehungVorlaufMaxC, wirksam.Max(z => z.Vorheizen.UeberhoehungVorlaufMaxC));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = " + kopf +
                                  " AND Vorheiz_Ueberhoehung_K = 2 AND Vorheiz_Ueberhoehung_H > 0"));
        }

        // =================================================================
        //  Datenwege: Duplizieren, Variante, Projektpaket
        // =================================================================

        private const int QUELLE = 1065;

        private static void SchnellSetzen()
            => Assert.True(DataRepository.ExecuteSQL(
                "UPDATE \"Tab_Einstellungen\" SET \"Aufheiz_Schnellaufheizung\" = 1, \"Aufheiz_Ueberhoehung_K\" = 3.5 WHERE \"ID_Projekt\" = ?",
                new DbParam("@p", QUELLE)));

        private static void PruefeKopie(int kopie)
        {
            Assert.True(kopie > 0);
            Assert.NotEqual(QUELLE, kopie);
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Aufheiz_Schnellaufheizung\", \"Aufheiz_Ueberhoehung_K\" FROM \"Tab_Einstellungen\" WHERE \"ID_Projekt\" = ?",
                new DbParam("@p", kopie));
            Assert.Single(t.Rows.Cast<DataRow>());
            Assert.Equal("1|3.5", string.Join("|", t.Rows[0].ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture))));
            Vorheizvorgabe v = KonfigurationCtrl.VorheizvorgabeLesen(kopie);
            Assert.True(v.SchnellaufheizungWirksam);
            Assert.Equal(3.5, v.UeberhoehungK);
        }

        [Fact]
        public void Duplizieren_nimmt_die_Schnellaufheizung_mit()
        {
            if (!_db.Vorhanden) return;
            SchnellSetzen();
            PruefeKopie(new ProjektDuplizierenCtrl().Duplizieren(Name(QUELLE), "Schnell Duplikat"));
        }

        [Fact]
        public void Die_Variante_nimmt_die_Schnellaufheizung_mit()
        {
            if (!_db.Vorhanden) return;
            SchnellSetzen();
            int v = new VariantenCtrl().AnlegenAusStamm(QUELLE, Name(QUELLE), "Schnellprobe", out string fehler);
            Assert.True(v > 0, fehler);
            PruefeKopie(v);
        }

        [Fact]
        public void Das_Projektpaket_nimmt_die_Schnellaufheizung_mit()
        {
            if (!_db.Vorhanden) return;
            SchnellSetzen();
            string ordner = Path.Combine(Path.GetTempPath(), "epos-schnellpaket-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "schnell.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(Name(QUELLE), paket));
                int neu = io.Importieren(paket, "Schnell Paket", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeKopie(neu);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch (IOException) { }
            }
        }

        // =================================================================
        //  Hilfen
        // =================================================================

        private static int Lauf(int projekt)
        {
            var laeufer = new SimulationRunner();
            int kopf = laeufer.SimuliereUndSpeichere(projekt, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            return kopf;
        }

        private static string Name(int id)
            => Convert.ToString(DataRepository.ExecuteScalar("SELECT \"Projektname\" FROM \"Tab_Projekt\" WHERE \"ID\" = ?",
                                                             new DbParam("@p", id)), CultureInfo.InvariantCulture);

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            k.ExecuteNonQuery();
        }
    }
}
