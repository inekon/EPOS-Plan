using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Schemaschritt des Vorheizens</b> (<see cref="VorheizenSchema"/>; Welle AH-V4, Entwurf Vorheizrampe Fassung 2,
    /// Abschnitt 4): Nummer und Kette, Spalten und Prüfklauseln, die Testkopie auf dem Schritt (alles NULL), Wiederholbarkeit;
    /// der Datenweg — Einstellung lesen und schreiben, Vorheizzeit der Zone, Vorbelegung der Toleranz (F14) — und das
    /// Ergebnisschreiben (Sollwertrampe NULL, Vorgabe mit Werten an Gebäude und Zone).
    /// </summary>
    [Collection("Testdatenbank")]
    public class VorheizenSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>Das Referenzprojekt mit berechneter Vorheizzeit (<see cref="VorheizReferenzprojektWacheTests"/>).</summary>
        private const int REFERENZPROJEKT_VORHEIZEN = 1065;

        public void Dispose() => _db.Dispose();

        // =================================================================
        //  Nummer, Spalten, Klauseln (ohne Datenbank)
        // =================================================================

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_ist_das_Ziel_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(215, VorheizenSchema.SCHRITT);
            Assert.Equal(RueckkuehlwerkSchema.SCHRITT + 1, VorheizenSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= VorheizenSchema.SCHRITT);   // KM4-E1 haengt 216 an
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == VorheizenSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Die_Spalten_folgen_dem_Entwurf_und_sind_nullbar()
        {
            Assert.Equal(new[]
            {
                "Aufheiz_Verfahren", "Aufheiz_Vorheizzeit_H", "Aufheiz_Toleranz_Art", "Aufheiz_Toleranz",
                "Aufheiz_Regelgenauigkeit_K", "Aufheiz_Geltung", "Aufheiz_Heizkreis_Einbeziehen"
            }, VorheizenSchema.SPALTEN_EINSTELLUNG.Select(s => s.Spalte));
            Assert.All(VorheizenSchema.SPALTEN_EINSTELLUNG, s => Assert.Equal("Tab_Einstellungen", s.Tabelle));
            Assert.Equal(new[] { "Tab_Zone", "Tab_Zone_STAMM" }, VorheizenSchema.SPALTEN_ZONE.Select(s => s.Tabelle));
            Assert.All(VorheizenSchema.SPALTEN_ZONE, s => Assert.Equal("Vorheizzeit_H", s.Spalte));
            Assert.Equal(new[]
            {
                "Aufheiz_Verfahren", "Vorheizzeit_H", "Vorheizzeit_Bedarf_Max_H", "Vorheiz_Bezug_Kw", "Vorheiz_Deckel_Kw",
                "Vorheiz_Leistung_Kw", "Vorheiz_Sprung_Vorlauf_Max_Kw", "Vorheiz_Sprung_Lauf_Max_Kw", "Vorheiz_Tage_Verfehlt",
                "Vorheiz_Tage_Ueber_Vorheizzeit", "Vorheiz_Unterschreitung_Max_K", "Vorheiz_Unterschreitung_H",
                "Vorheiz_Deckelstunden_H", "Vorheiz_Floorstunden_H", "Vorheiz_Naechte_Ohne_Absenkung", "Vorheiz_Unerreichbar_Tage",
                "Vorheiz_Sperrzeit_Tage", "Vorheiz_Mehrwaerme_Mwh"
            }, VorheizenSchema.ERGEBNISSPALTEN.Select(s => s.Spalte));
            Assert.Equal(7 + 2 + 2 * 18, VorheizenSchema.SPALTEN.Count);
            Assert.All(VorheizenSchema.SPALTEN, s => Assert.DoesNotContain("NOT NULL", s.Typ, StringComparison.Ordinal));
            Assert.All(VorheizenSchema.SPALTEN, s => Assert.DoesNotContain("DEFAULT", s.Typ, StringComparison.Ordinal));
        }

        [Fact]
        public void Die_Pruefklauseln_halten_Wertemengen_und_Bereiche()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"Tab_Einstellungen\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_Zone\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_Zone_STAMM\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_ErgebnisGebaeude\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_ErgebnisZone\" (ID INTEGER PRIMARY KEY) STRICT");
            foreach (string t in new[] { "Tab_Einstellungen", "Tab_Zone", "Tab_Zone_STAMM", "Tab_ErgebnisGebaeude", "Tab_ErgebnisZone" })
                Ausfuehren(c, "INSERT INTO \"" + t + "\" (ID) VALUES (1)");
            foreach (var s in VorheizenSchema.SPALTEN) Ausfuehren(c, VorheizenSchema.Anlegen(s));

            foreach (string gut in new[]
                     {
                         "Aufheiz_Verfahren = 'RAMPE'", "Aufheiz_Verfahren = 'VORGABE'", "Aufheiz_Verfahren = 'BERECHNET'",
                         "Aufheiz_Verfahren = NULL", "Aufheiz_Vorheizzeit_H = 1", "Aufheiz_Vorheizzeit_H = 47",
                         "Aufheiz_Toleranz_Art = 'KW'", "Aufheiz_Toleranz_Art = 'PROZENT'", "Aufheiz_Toleranz = 0",
                         "Aufheiz_Toleranz = 20", "Aufheiz_Regelgenauigkeit_K = 5", "Aufheiz_Regelgenauigkeit_K = 0.1",
                         "Aufheiz_Geltung = 'ZONE'", "Aufheiz_Heizkreis_Einbeziehen = 0", "Aufheiz_Heizkreis_Einbeziehen = 1"
                     })
                Ausfuehren(c, "UPDATE Tab_Einstellungen SET " + gut);
            foreach (string schlecht in new[]
                     {
                         "Aufheiz_Verfahren = 'FEST'", "Aufheiz_Vorheizzeit_H = 0", "Aufheiz_Vorheizzeit_H = 48",
                         "Aufheiz_Vorheizzeit_H = 2.5", "Aufheiz_Toleranz_Art = 'kW'", "Aufheiz_Toleranz = -0.1",
                         "Aufheiz_Regelgenauigkeit_K = 0", "Aufheiz_Regelgenauigkeit_K = 5.01", "Aufheiz_Geltung = 'PROJEKT'",
                         "Aufheiz_Heizkreis_Einbeziehen = 2"
                     })
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE Tab_Einstellungen SET " + schlecht));

            Ausfuehren(c, "UPDATE Tab_Zone SET Vorheizzeit_H = 12");
            Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE Tab_Zone SET Vorheizzeit_H = 0"));
            Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE Tab_Zone SET Vorheizzeit_H = 48"));

            foreach (string t in new[] { "Tab_ErgebnisGebaeude", "Tab_ErgebnisZone" })
            {
                Ausfuehren(c, "UPDATE " + t + " SET Aufheiz_Verfahren = 'BERECHNET', Vorheizzeit_H = 0, Vorheiz_Tage_Verfehlt = 365, " +
                              "Vorheiz_Sprung_Lauf_Max_Kw = -3.5, Vorheiz_Mehrwaerme_Mwh = -0.2, Vorheiz_Unterschreitung_H = 20000");
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE " + t + " SET Aufheiz_Verfahren = 'RAMPE'"));
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE " + t + " SET Vorheiz_Tage_Verfehlt = 366"));
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE " + t + " SET Vorheiz_Deckel_Kw = -1"));
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE " + t + " SET Vorheiz_Deckelstunden_H = -1"));
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE " + t + " SET Vorheizzeit_H = 48"));
            }
        }

        [Fact]
        public void Die_Persistenzwerte_uebersetzen_in_beide_Richtungen_und_leer_ist_die_Vorgabe()
        {
            foreach (Aufheizverfahren v in Enum.GetValues<Aufheizverfahren>())
                Assert.Equal(v, VorheizenSchema.VerfahrenAus(VorheizenSchema.VerfahrenWert(v)));
            Assert.Null(VorheizenSchema.VerfahrenWert(Aufheizverfahren.Sollwertrampe));
            Assert.Equal(Aufheizverfahren.Sollwertrampe, VorheizenSchema.VerfahrenAus("RAMPE"));
            Assert.Equal(Vorheiztoleranzart.Kilowatt, VorheizenSchema.ToleranzartAus("KW"));
            Assert.Equal(Vorheiztoleranzart.Prozent, VorheizenSchema.ToleranzartAus(null));
            Assert.Null(VorheizenSchema.ToleranzartWert(Vorheiztoleranzart.Prozent));
            Assert.Equal(Vorheizgeltung.Zone, VorheizenSchema.GeltungAus("ZONE"));
            Assert.Null(VorheizenSchema.GeltungWert(Vorheizgeltung.Gebaeude));
        }

        // =================================================================
        //  Testkopie und Wiederholbarkeit
        // =================================================================

        [Fact]
        public void Die_Testkopie_steht_auf_dem_Schritt_und_jede_Zelle_ist_leer()
        {
            if (!_db.Vorhanden) return;
            Assert.True(VorheizenSchema.Vollstaendig());
            // Allein das Referenzprojekt 1065 (Vorheizen, VorheizReferenzprojektWacheTests) trägt ein Verfahren.
            Assert.Equal(1, VorheizenSchema.ZeilenMitVerfahren());
            foreach (var s in VorheizenSchema.SPALTEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + s.Tabelle + "\" WHERE \"" + s.Spalte + "\" IS NOT NULL" +
                                      (s.Tabelle == VorheizenSchema.TAB_EINSTELLUNGEN ? " AND ID_Projekt <> " + REFERENZPROJEKT_VORHEIZEN : "")));
            Assert.Equal(VorheizenSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE, DataRepository.SpaltenVonTabelle(VorheizenSchema.TAB_ERGEBNIS_GEBAEUDE).Count);
            Assert.Equal(VorheizenSchema.SPALTENZAHL_ERGEBNIS_ZONE, DataRepository.SpaltenVonTabelle(VorheizenSchema.TAB_ERGEBNIS_ZONE).Count);
            Assert.Equal((long)SchemaStand.Zielversion, Zahl("SELECT SchemaVersion FROM Tab_Applikation"));
        }

        [Fact]
        public void Der_Schritt_ist_wiederholbar_und_legt_fehlende_Spalten_leer_nach()
        {
            if (!_db.Vorhanden) return;
            var zweiter = new List<string>();
            Assert.Equal(0, VorheizenSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("nichts zu tun", StringComparison.Ordinal));

            foreach (var s in VorheizenSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
            GebaeudeZonenanschluss.ProbeVerwerfen();
            Assert.False(VorheizenSchema.Vollstaendig());
            // Ohne die Spalten: Sollwertrampe, und Lesen der Zonen geht wie vorher.
            Assert.Equal(Vorheizvorgabe.Sollwertrampe, KonfigurationCtrl.VorheizvorgabeLesen(1051));
            Assert.Equal(VorheizenSchema.SPALTEN.Count, VorheizenSchema.Ausfuehren(null));
            Assert.True(VorheizenSchema.Vollstaendig());
            Assert.Equal(0, VorheizenSchema.ZeilenMitVerfahren());
            GebaeudeZonenanschluss.ProbeVerwerfen();
        }

        // =================================================================
        //  Datenweg: Einstellung, Zone, Vorbelegung
        // =================================================================

        [Fact]
        public void Die_Einstellung_geht_in_die_Spalten_und_kommt_gleich_zurueck()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1051;
            Aufheizvorgabe bestand = KonfigurationCtrl.AufheizvorgabeLesen(projekt);
            Assert.Equal(Vorheizvorgabe.Sollwertrampe, bestand.Vorheizen);

            var vh = new Vorheizvorgabe(Aufheizverfahren.Vorgabe, 6, Vorheiztoleranzart.Kilowatt, 2.5, 0.5,
                                        Vorheizgeltung.Zone, heizkreisEinbeziehen: false);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, bestand with { Vorheizen = vh }));
            Assert.Equal(vh, KonfigurationCtrl.AufheizvorgabeLesen(projekt).Vorheizen);
            Assert.Equal("VORGABE", Text("SELECT Aufheiz_Verfahren FROM Tab_Einstellungen WHERE ID_Projekt = 1051"));
            Assert.Equal("KW", Text("SELECT Aufheiz_Toleranz_Art FROM Tab_Einstellungen WHERE ID_Projekt = 1051"));
            Assert.Equal(0L, Zahl("SELECT Aufheiz_Heizkreis_Einbeziehen FROM Tab_Einstellungen WHERE ID_Projekt = 1051"));

            // Berechnet mit leeren Feldern: die Vorgaben schreiben NULL.
            var berechnet = new Vorheizvorgabe(Aufheizverfahren.Berechnet);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, bestand with { Vorheizen = berechnet }));
            Assert.Equal(berechnet, KonfigurationCtrl.AufheizvorgabeLesen(projekt).Vorheizen);
            Assert.Equal(2, VorheizenSchema.ZeilenMitVerfahren());   // 1051 und das Referenzprojekt 1065

            // Zurück auf die Sollwertrampe: jede Spalte wieder NULL, der Bestand ist wieder gleich.
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, bestand));
            foreach (var s in VorheizenSchema.SPALTEN_EINSTELLUNG)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE \"" + s.Spalte + "\" IS NOT NULL AND ID_Projekt <> " +
                                      REFERENZPROJEKT_VORHEIZEN));
            Assert.Equal(bestand, KonfigurationCtrl.AufheizvorgabeLesen(projekt));
        }

        [Fact]
        public void Die_ausdrueckliche_Rampe_liest_sich_als_Sollwertrampe()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("UPDATE Tab_Einstellungen SET Aufheiz_Verfahren = 'RAMPE', Aufheiz_Heizkreis_Einbeziehen = 1 " +
                                           "WHERE ID_Projekt = 1051");
            Assert.Same(Vorheizvorgabe.Sollwertrampe, KonfigurationCtrl.VorheizvorgabeLesen(1051));
        }

        [Fact]
        public void Die_Vorheizzeit_der_Zone_reist_ueber_Lesen_und_Speichern()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            KeyValuePair<int, List<ZoneModel>> geb = ctrl.LesenJeProjekt(1052).First(g => g.Value.Count >= 2);
            List<ZoneModel> zonen = geb.Value;
            Assert.All(zonen, z => Assert.Null(z.Vorheizzeit_H));
            zonen[0].Vorheizzeit_H = 9;
            Assert.True(ctrl.SpeichernJeGebaeude(geb.Key, zonen).Ok);
            List<ZoneModel> wieder = ctrl.LesenJeGebaeude(geb.Key);
            Assert.Equal(9, wieder.Single(z => z.Bezeichner == zonen[0].Bezeichner).Vorheizzeit_H);
            Assert.All(wieder.Where(z => z.Bezeichner != zonen[0].Bezeichner), z => Assert.Null(z.Vorheizzeit_H));
            Assert.Equal(9, Zoneneingaben.Aus(wieder.Single(z => z.Bezeichner == zonen[0].Bezeichner)).VorheizzeitH);
        }

        [Fact]
        public void Die_Vorbelegung_der_Toleranz_schreibt_nur_mit_Programmeinstellung()
        {
            if (!_db.Vorhanden) return;
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                Dienste.Einstellungen = new FluechtigeEinstellungen();
                Assert.Equal((null, Vorheiztoleranzart.Prozent), EinstellungenCtrl.VorheiztoleranzLesen());
                Assert.False(KonfigurationCtrl.VorheiztoleranzAnfangswertSetzen(1051));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Einstellungen WHERE Aufheiz_Toleranz IS NOT NULL"));

                EinstellungenCtrl.VorheiztoleranzSchreiben(3.5, Vorheiztoleranzart.Kilowatt);
                Assert.Equal((3.5, Vorheiztoleranzart.Kilowatt), EinstellungenCtrl.VorheiztoleranzLesen());
                Assert.True(KonfigurationCtrl.VorheiztoleranzAnfangswertSetzen(1051));
                Vorheizvorgabe v = KonfigurationCtrl.VorheizvorgabeLesen(1051);
                Assert.Equal(Aufheizverfahren.Sollwertrampe, v.Verfahren);
                Assert.Equal(3.5, v.ToleranzWert);
                Assert.Equal(Vorheiztoleranzart.Kilowatt, v.Toleranzart);

                Dienste.Einstellungen.Schreib(EinstellungenCtrl.SCHLUESSEL_NEUE_PROJEKTE_VORHEIZTOLERANZ, "-1");
                Assert.Null(EinstellungenCtrl.VorheiztoleranzLesen().Wert);
                EinstellungenCtrl.VorheiztoleranzSchreiben(null, Vorheiztoleranzart.Prozent);
                Assert.Null(EinstellungenCtrl.VorheiztoleranzLesen().Wert);
            }
            finally
            {
                Dienste.Einstellungen = vorher;
            }
        }

        // =================================================================
        //  Ergebnis: Sollwertrampe NULL, Vorgabe mit Werten
        // =================================================================

        [Fact]
        public void Ein_Lauf_mit_Sollwertrampe_laesst_die_Vorheizspalten_leer()
        {
            if (!_db.Vorhanden) return;
            int kopf = Lauf(1051);
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = " + kopf) >= 1);
            foreach (var s in VorheizenSchema.SPALTEN_ERGEBNIS)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + s.Tabelle + "\" WHERE \"" + s.Spalte + "\" IS NOT NULL"));
            Assert.All(new ErgebnisCtrl().Load(1051).Gebaeude, g => Assert.Null(g.Vorheizen));
        }

        [Fact]
        public void Ein_Lauf_mit_Vorgabe_schreibt_die_Vorheizgroessen_an_Gebaeude_und_Zone()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1052;
            Aufheizvorgabe an = VorheizAk3Tests.ProjektvorgabeAn(projekt);
            Assert.True(KonfigurationCtrl.AufheizvorgabeSetzen(projekt, an with
            {
                Vorheizen = new Vorheizvorgabe(Aufheizverfahren.Vorgabe, 4, geltung: Vorheizgeltung.Zone),
            }));
            var ctrl = new GebaeudeZonenCtrl();
            KeyValuePair<int, List<ZoneModel>> geb = ctrl.LesenJeProjekt(projekt).First(g => g.Value.Count >= 2);
            ZoneModel eigene = geb.Value.First(z => z.IstBeheizt);
            eigene.Vorheizzeit_H = 7;
            Assert.True(ctrl.SpeichernJeGebaeude(geb.Key, geb.Value).Ok);

            int kopf = Lauf(projekt);
            ErgebnisModel m = new ErgebnisCtrl().Load(projekt);
            ErgebnisGebaeudeModel g = m.Gebaeude.Single(x => x.Zonen.Count >= 2);
            Assert.NotNull(g.Vorheizen);
            Assert.Equal(VorheizenSchema.VERFAHREN_VORGABE, g.Vorheizen.Verfahren);
            Assert.Equal(7, g.Vorheizen.VorheizzeitH);
            Assert.True(g.Vorheizen.BezugKw > 0.0);
            Assert.True(g.Vorheizen.DeckelKw >= g.Vorheizen.BezugKw);
            Assert.NotNull(g.Vorheizen.MehrwaermeMwh);
            ErgebnisZoneModel ze = g.Zonen.Single(z => z.ID_Zone == eigene.ID);
            Assert.Equal(7, ze.Vorheizen.VorheizzeitH);
            Assert.All(g.Zonen.Where(z => z.IstBeheizt && z.ID_Zone != eigene.ID), z => Assert.Equal(4, z.Vorheizen.VorheizzeitH));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisGebaeude WHERE ID_Ergebnis = " + kopf + " AND Aufheiz_Verfahren = 'VORGABE'"));
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

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql));

        private static string Text(string sql) => Convert.ToString(DataRepository.ExecuteScalar(sql));

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand k = c.CreateCommand();
            k.CommandText = sql;
            k.ExecuteNonQuery();
        }
    }
}
