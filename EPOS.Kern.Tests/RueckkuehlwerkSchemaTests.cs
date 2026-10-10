using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Schemaschritt des Rückkühlwerks</b> (<see cref="RueckkuehlwerkSchema"/>; Welle K-F1, Entscheid E120):
    /// Katalog und Projektkopie (STRICT, leer), Verweis und Wasserpreis an der Anlagenzeile, sechs Kennzahlen am Ergebnis
    /// der Kältemaschine; dazu die Datenwege (Katalog, Projektkopie, Anlagenzeile samt Verträglichkeit, Löschen, Duplizieren).
    /// Reines DDL, wiederholbar, ergebnisneutral.
    /// </summary>
    [Collection("Testdatenbank")]
    public class RueckkuehlwerkSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1017;
        private const string TROCKENKUEHLER = "Kältemaschine 200 kW wassergekühlt mit Trockenkühler";
        private const string LUFTGEKUEHLT = "Kältemaschine 50 kW luftgekühlt";

        // =================================================================
        //  Nummer, Spalten, Klauseln (ohne Datenbank)
        // =================================================================

        [Fact]
        public void Die_Nummer_haengt_an_der_Kette_ist_das_Ziel_und_die_Paketanhebung_fuehrt_Katalog()
        {
            // Arbeitskette: Beim Merge haengt der Schritt an die Klasse von 213 (K1) und traegt 214.
            Assert.Equal(KaelteRangSchema.SCHRITT + 1, RueckkuehlwerkSchema.SCHRITT);
            Assert.Equal(RueckkuehlwerkSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == RueckkuehlwerkSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Katalog, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Die_Fachspalten_folgen_dem_Entwurf_und_sind_ausser_dem_Bezeichner_nullbar()
        {
            Assert.Equal(new[]
            {
                "Bezeichner", "Beschreibung", "Bauart", "Nennleistung_kW", "Annaeherung_Nenn_K", "Annaeherung_Weg",
                "Ventilator_Nenn_kW", "Ventilator_Regelung", "Ventilator_Stufen", "Ventilator_Drehzahl_Min",
                "Befeuchtung_Wirkungsgrad", "Befeuchtung_Ab_C", "Verdunstung_Faktor", "Eindickung", "Drift_Anteil",
                "Freikuehlung_Schaltung", "Modulkosten"
            }, RueckkuehlwerkSchema.Fachspalten);
            foreach ((string spalte, string typ) in RueckkuehlwerkSchema.FACH_SPALTEN.Skip(1))
            {
                Assert.DoesNotContain("NOT NULL", typ, StringComparison.Ordinal);
                Assert.DoesNotContain("DEFAULT", typ, StringComparison.Ordinal);
            }
            Assert.Equal(new[] { "ID_Rueckkuehlwerk", "Wasserpreis_EUR_m3" },
                         RueckkuehlwerkSchema.SPALTEN_ANLAGE.Select(s => s.Spalte));
            Assert.All(RueckkuehlwerkSchema.SPALTEN_ANLAGE, s => Assert.Equal("Tab_Energieanlagen", s.Tabelle));
            Assert.Contains("REFERENCES \"Tab_Rueckkuehlwerk\" (\"ID\") ON DELETE SET NULL", RueckkuehlwerkSchema.SPALTEN_ANLAGE[0].Typ,
                            StringComparison.Ordinal);
            Assert.Equal(new[]
            {
                "Ventilatorstrom_MWh", "Wasser_m3", "Stunden_Nass", "TeilFreikuehlung_MWh", "TeilFreikuehlung_Stunden",
                "Rueckkuehltemperatur_Mittel"
            }, RueckkuehlwerkSchema.SPALTEN_ERGEBNIS.Select(s => s.Spalte));
            Assert.All(RueckkuehlwerkSchema.SPALTEN_ERGEBNIS, s => Assert.Equal("Tab_ErgebnisKaeltemaschine", s.Tabelle));
            Assert.EndsWith(") STRICT", RueckkuehlwerkSchema.SqlCreateStamm(), StringComparison.Ordinal);
            Assert.EndsWith(") STRICT", RueckkuehlwerkSchema.SqlCreateProjekt(), StringComparison.Ordinal);
        }

        [Fact]
        public void Die_Pruefklauseln_halten_Wertemengen_und_Bereiche()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"Tab_Projekt\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_KostenVorlage\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, RueckkuehlwerkSchema.SqlCreateStamm());
            Ausfuehren(c, RueckkuehlwerkSchema.SqlCreateProjekt());
            Ausfuehren(c, "INSERT INTO Tab_Rueckkuehlwerk_STAMM (ID, Bezeichner) VALUES (1, 'RK')");

            foreach (string gut in new[]
                     {
                         "Bauart = 'TROCKEN'", "Bauart = 'KUEHLTURM_GESCHLOSSEN'", "Bauart = NULL", "Annaeherung_Weg = 'LASTABHAENGIG'",
                         "Ventilator_Regelung = 'DREHZAHL'", "Ventilator_Stufen = 2", "Ventilator_Drehzahl_Min = 0.2",
                         "Annaeherung_Nenn_K = 0", "Annaeherung_Nenn_K = 30", "Eindickung = 3", "Drift_Anteil = 0.0002",
                         "Freikuehlung_Schaltung = 'REIHE'", "Befeuchtung_Ab_C = 22", "Verdunstung_Faktor = 1", "Modulkosten = 0"
                     })
                Ausfuehren(c, "UPDATE Tab_Rueckkuehlwerk_STAMM SET " + gut);
            foreach (string schlecht in new[]
                     {
                         "Bauart = 'NASSKUEHLER'", "Annaeherung_Weg = 'fest'", "Ventilator_Regelung = 'AUS'", "Ventilator_Stufen = 1",
                         "Ventilator_Stufen = 2.5", "Ventilator_Drehzahl_Min = 1.5", "Nennleistung_kW = 0", "Annaeherung_Nenn_K = -1",
                         "Eindickung = 1", "Drift_Anteil = 2", "Freikuehlung_Schaltung = 'SERIE'", "Verdunstung_Faktor = 0",
                         "Befeuchtung_Wirkungsgrad = 1.1", "Modulkosten = -1", "Bezeichner = ''", "ReadOnly = 2"
                     })
                Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE Tab_Rueckkuehlwerk_STAMM SET " + schlecht));

            Ausfuehren(c, "CREATE TABLE \"Tab_Energieanlagen\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO Tab_Energieanlagen (ID) VALUES (1)");
            foreach (var s in RueckkuehlwerkSchema.SPALTEN_ANLAGE) Ausfuehren(c, RueckkuehlwerkSchema.Anlegen(s));
            Ausfuehren(c, "UPDATE Tab_Energieanlagen SET Wasserpreis_EUR_m3 = 4.5");
            Assert.Throws<SqliteException>(() => Ausfuehren(c, "UPDATE Tab_Energieanlagen SET Wasserpreis_EUR_m3 = -0.1"));
        }

        [Fact]
        public void Pruefen_ohne_Datenbank_haelt_dieselben_Grenzen()
        {
            Assert.Null(RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel { Bezeichner = "RK" }));
            Assert.Null(RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel
            {
                Bezeichner = "RK", Bauart = "HYBRID", Annaeherung_Weg = "FEST", Ventilator_Regelung = "STUFEN", Ventilator_Stufen = 3,
                Eindickung = 4, Drift_Anteil = 0.001, Freikuehlung_Schaltung = "PARALLEL", Annaeherung_Nenn_K = 6
            }));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG, RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel { Bezeichner = " " }));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_MSG_BAUART_UNGUELTIG,
                         RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel { Bezeichner = "RK", Bauart = "LUFT" }));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_MSG_AUSWAHL_UNGUELTIG,
                         RueckkuehlwerkStammCtrl.Pruefen(new RueckkuehlwerkModel { Bezeichner = "RK", Freikuehlung_Schaltung = "SERIE" }));
            foreach (RueckkuehlwerkModel m in new[]
                     {
                         new RueckkuehlwerkModel { Bezeichner = "RK", Ventilator_Stufen = 1 },
                         new RueckkuehlwerkModel { Bezeichner = "RK", Eindickung = 1 },
                         new RueckkuehlwerkModel { Bezeichner = "RK", Nennleistung_kW = double.NaN },
                         new RueckkuehlwerkModel { Bezeichner = "RK", Annaeherung_Nenn_K = 31 },
                     })
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_MSG_WERT_UNGUELTIG, RueckkuehlwerkStammCtrl.Pruefen(m));
        }

        [Fact]
        public void Eine_luftgekuehlte_Maschine_bekommt_kein_Rueckkuehlwerk()
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_MSG_LUFT_UNVERTRAEGLICH, KaeltemaschineAnlageCtrl.RueckkuehlwerkPruefen("LUFT", true));
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkPruefen("LUFT", false));
            foreach (string art in new[] { "WASSER", "TROCKENKUEHLER", "NASSKUEHLER", null })
                Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkPruefen(art, true));
            Assert.Contains("Rückkühlwerk", WindowsFormsApplication1.MyResource.Resource.RKW_MSG_LUFT_UNVERTRAEGLICH, StringComparison.Ordinal);
        }

        // =================================================================
        //  Testkopie
        // =================================================================

        /// <summary>Die Testkopie steht auf dem Schritt: Katalog leer, kein Verweis, kein Preis, keine Kennzahl (Basis byte-gleich).</summary>
        [Fact]
        public void Die_Testkopie_steht_auf_dem_Schritt_ohne_Saat_und_ohne_Wert()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= RueckkuehlwerkSchema.SCHRITT);
            Assert.True(RueckkuehlwerkSchema.Vollstaendig());
            Assert.Equal(0, RueckkuehlwerkSchema.ZeilenMitWert());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Rueckkuehlwerk_STAMM"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Rueckkuehlwerk"));
            foreach (var s in RueckkuehlwerkSchema.SPALTEN_ERGEBNIS)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisKaeltemaschine WHERE \"" + s.Spalte + "\" IS NOT NULL"));
            foreach (string t in new[] { RueckkuehlwerkSchema.TAB_STAMM, RueckkuehlwerkSchema.TAB_PROJEKT })
                Assert.EndsWith("STRICT", Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE name = ?", new DbParam("@n", t)), CultureInfo.InvariantCulture).TrimEnd(),
                    StringComparison.Ordinal);
            // Die Spalten haengen hinten an: an der Anlagenzeile die zwei des Schritts, am Ergebnis die sechs.
            List<string> anlagen = DataRepository.SpaltenVonTabelle(RueckkuehlwerkSchema.TAB_ANLAGEN);
            Assert.Equal(RueckkuehlwerkSchema.SPALTEN_ANLAGE.Select(s => s.Spalte), anlagen.Skip(anlagen.Count - 2));
            List<string> ergebnis = DataRepository.SpaltenVonTabelle(RueckkuehlwerkSchema.TAB_ERGEBNIS);
            Assert.Equal(RueckkuehlwerkSchema.SPALTEN_ERGEBNIS.Select(s => s.Spalte), ergebnis.Skip(ergebnis.Count - 6));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
        }

        [Fact]
        public void Der_Schritt_ist_wiederholbar_und_legt_fehlende_Spalten_leer_nach()
        {
            if (!_db.Vorhanden) return;
            var zweiter = new List<string>();
            Assert.Equal(0, RueckkuehlwerkSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.Contains("nichts zu tun", StringComparison.Ordinal));

            foreach (var s in RueckkuehlwerkSchema.SPALTEN_ERGEBNIS)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Energieanlagen\" DROP COLUMN \"Wasserpreis_EUR_m3\"");
            Assert.False(RueckkuehlwerkSchema.Vollstaendig());
            Assert.Equal(7, RueckkuehlwerkSchema.Ausfuehren(null));
            Assert.True(RueckkuehlwerkSchema.Vollstaendig());
            Assert.Equal(0, RueckkuehlwerkSchema.ZeilenMitWert());
        }

        // =================================================================
        //  Datenwege
        // =================================================================

        [Fact]
        public void Katalog_speichern_duplizieren_sperren_und_loeschen()
        {
            if (!_db.Vorhanden) return;
            RueckkuehlwerkStammCtrl.SpeicherErgebnis e = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel
            {
                Bezeichner = "Rückkühlwerk 200 kW trocken", Bauart = "TROCKEN", Nennleistung_kW = 250, Annaeherung_Nenn_K = 8,
                Ventilator_Regelung = "STUFEN", Ventilator_Stufen = 3, Freikuehlung_Schaltung = "REIHE"
            });
            Assert.True(e.Ok, e.Meldung);
            RueckkuehlwerkModel m = RueckkuehlwerkStammCtrl.Laden(e.Id);
            Assert.Equal(("TROCKEN", 250.0, 8.0, 3, "REIHE", false),
                         (m.Bauart, m.Nennleistung_kW.Value, m.Annaeherung_Nenn_K.Value, m.Ventilator_Stufen.Value, m.Freikuehlung_Schaltung, m.ReadOnly));
            Assert.Null(m.Annaeherung_Weg);
            Assert.Single(RueckkuehlwerkStammCtrl.Liste());
            Assert.False(RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel { Bezeichner = m.Bezeichner }).Ok);

            Katalogkopie.Ergebnis kopie = RueckkuehlwerkStammCtrl.Duplizieren(e.Id, "Rückkühlwerk Kopie");
            Assert.True(kopie.Ok);
            Assert.Equal(2, RueckkuehlwerkStammCtrl.Liste().Count);

            RueckkuehlwerkStammCtrl.SchlossSetzen(new[] { e.Id }, true);
            Assert.True(RueckkuehlwerkStammCtrl.Gesperrt(e.Id));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_MSG_AUSGELIEFERT, RueckkuehlwerkStammCtrl.Loeschen(e.Id).Meldung);
            m.Id = e.Id;
            Assert.False(RueckkuehlwerkStammCtrl.Speichern(m).Ok);
            Assert.True(RueckkuehlwerkStammCtrl.Loeschen(kopie.Id).Ok);
        }

        [Fact]
        public void Anlagenzeile_waehlt_eine_eigene_Projektkopie_und_raeumt_sie_auf()
        {
            if (!_db.Vorhanden) return;
            int stamm = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel
            {
                Bezeichner = "RK Test", Bauart = "ADIABAT", Befeuchtung_Ab_C = 24
            }).Id;
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(TROCKENKUEHLER), "KM wassergekühlt");
            KaeltemaschineAnlageModel a = KaeltemaschineAnlageCtrl.Laden(anlage);
            Assert.Null(a.IdRueckkuehlwerk);
            Assert.Null(a.WasserpreisEurM3);
            Assert.Equal("TROCKENKUEHLER", a.Rueckkuehlart);

            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, stamm));
            Assert.Null(KaeltemaschineAnlageCtrl.WasserpreisSetzen(anlage, 4.2));
            a = KaeltemaschineAnlageCtrl.Laden(anlage);
            Assert.True(a.IdRueckkuehlwerk.HasValue);
            Assert.Equal(4.2, a.WasserpreisEurM3);
            RueckkuehlwerkModel kopie = RueckkuehlwerkCtrl.Laden(a.IdRueckkuehlwerk.Value);
            Assert.Equal((PROJEKT, stamm, "ADIABAT", 24.0), (kopie.IdProjekt.Value, kopie.IdStamm.Value, kopie.Bauart, kopie.Befeuchtung_Ab_C.Value));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_MSG_WERT_UNGUELTIG, KaeltemaschineAnlageCtrl.WasserpreisSetzen(anlage, -1));

            // Speichern der Anlagenzeile (Weg der Huelle) laesst Rueckkuehlwerk und Preis stehen.
            a.Anzahl = 2;
            Assert.Null(KaeltemaschineAnlageCtrl.Speichern(a));
            Assert.Equal(kopie.Id, KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);

            // Ein neues Rueckkuehlwerk ersetzt die alte Kopie; keins = heutiger Weg, die Kopie geht.
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, stamm));
            int zweite = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;
            Assert.NotEqual(kopie.Id, zweite);
            Assert.Null(RueckkuehlwerkCtrl.Laden(kopie.Id));
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, null));
            Assert.Null(KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);
            Assert.Null(RueckkuehlwerkCtrl.Laden(zweite));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_MSG_SATZ_FEHLT, KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, 999999));

            // Loeschen der Projektkopie setzt den Verweis leer; Loeschen der Anlage nimmt die Kopie mit.
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, stamm));
            int dritte = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;
            RueckkuehlwerkCtrl.Loeschen(dritte);
            Assert.Null(KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, stamm));
            int vierte = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;
            KaeltemaschineAnlageCtrl.Loeschen(anlage);
            Assert.Null(RueckkuehlwerkCtrl.Laden(vierte));

            // Loeschen des Katalogsatzes laesst eine Projektkopie stehen, ohne Katalogverweis.
            int anlage2 = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(TROCKENKUEHLER), "KM 2");
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage2, stamm));
            int fuenfte = KaeltemaschineAnlageCtrl.Laden(anlage2).IdRueckkuehlwerk.Value;
            Assert.True(RueckkuehlwerkStammCtrl.Loeschen(stamm).Ok);
            Assert.Null(RueckkuehlwerkCtrl.Laden(fuenfte).IdStamm);
        }

        [Fact]
        public void Eine_luftgekuehlte_Anlage_lehnt_das_Rueckkuehlwerk_benannt_ab()
        {
            if (!_db.Vorhanden) return;
            int stamm = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel { Bezeichner = "RK Luft" }).Id;
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(LUFTGEKUEHLT), "KM luftgekühlt");
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.RKW_MSG_LUFT_UNVERTRAEGLICH, KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, stamm));
            Assert.Null(KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Rueckkuehlwerk"));
        }

        [Fact]
        public void Duplizieren_des_Projekts_nimmt_die_Projektkopie_mit()
        {
            if (!_db.Vorhanden) return;
            int stamm = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel { Bezeichner = "RK Duplikat", Bauart = "HYBRID" }).Id;
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(TROCKENKUEHLER), "KM Duplikat");
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, stamm));
            Assert.Null(KaeltemaschineAnlageCtrl.WasserpreisSetzen(anlage, 3.0));
            string name = Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                                        new DbParam("?", PROJEKT)), CultureInfo.InvariantCulture);

            int neu = new ProjektDuplizierenCtrl().Duplizieren(name, name + " RKW");
            Assert.True(neu > 0);
            KaeltemaschineAnlageModel kopie = KaeltemaschineAnlageCtrl.Liste(neu).Single(x => x.Bezeichner == "KM Duplikat");
            Assert.True(kopie.IdRueckkuehlwerk.HasValue);
            Assert.NotEqual(KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk, kopie.IdRueckkuehlwerk);
            RueckkuehlwerkModel rk = RueckkuehlwerkCtrl.Laden(kopie.IdRueckkuehlwerk.Value);
            Assert.Equal((neu, "HYBRID", "RK Duplikat"), (rk.IdProjekt.Value, rk.Bauart, rk.Bezeichner));
            Assert.Equal(3.0, kopie.WasserpreisEurM3);
        }

        // =================================================================

        private static int StammKm(string bezeichner) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM " + KaeltemaschineSchema.TAB_STAMM + " WHERE Bezeichner = ?", new DbParam("?", bezeichner)),
            CultureInfo.InvariantCulture);

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using SqliteCommand cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
