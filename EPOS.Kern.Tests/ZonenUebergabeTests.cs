using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>AK1z — die Wärmeübergabe je Zone</b> (E63, <see cref="ZonenUebergabeSchema"/>, Teil A: Schema und Datenweg).
    ///
    /// <para><b>Geprüft wird:</b> Nummer und Paketanhebung; die Prüfklauseln an einer STRICT-Tabelle; der Stand der
    /// Testdatenbank und die Rundreise 180 → 181; das Aggregat der Zone (NULL-erhaltend), die Prüfregeln, das
    /// Projektduplikat und der Projekttransfer; die Kaskade <see cref="Zonenuebergabevorgaben"/> je Feld, mit
    /// <c>IDEAL</c> an der Zone und mit dem Flächenanteil der beheizten Zonen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenUebergabeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;

        // =============================================================================
        //  Teil 1 - Nummer, Register, Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Die_Nummer_folgt_lueckenlos_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(ErdreichVorgabeSchema.SCHRITT + 1, ZonenUebergabeSchema.SCHRITT);
            Assert.Equal(181, ZonenUebergabeSchema.SCHRITT);
            Assert.Equal(ZonenUebergabeSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == ZonenUebergabeSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
            Assert.Equal(new[]
                {
                    "Tab_Zone.Auslegung_Vorlauf", "Tab_Zone.Auslegung_Ruecklauf", "Tab_Zone.Auslegung_Raumtemperatur",
                    "Tab_Zone.Regler_Proportionalband", "Tab_ErgebnisZone.Vorlauf_Mittel_C",
                    "Tab_ErgebnisZone.Ruecklauf_Mittel_C", "Tab_ErgebnisZone.Uebergabe_Begrenzt_H",
                },
                ZonenUebergabeSchema.SPALTEN.Select(x => x.Tabelle + "." + x.Spalte).ToArray());
        }

        /// <summary>Die Prüfklauseln an einer STRICT-Tabelle: Proportionalband ≥ 0, Begrenzungsstunden 0 … 8760.</summary>
        [Fact]
        public void Die_Pruefklauseln_halten_Band_und_Stunden()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "CREATE TABLE \"Tab_Zone\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_ErgebnisZone\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "INSERT INTO Tab_Zone (ID) VALUES (1)");
            Ausfuehren(c, "INSERT INTO Tab_ErgebnisZone (ID) VALUES (1)");
            foreach (var s in ZonenUebergabeSchema.SPALTEN) Ausfuehren(c, ZonenUebergabeSchema.Anlegen(s));
            foreach (string gut in new[] { "NULL", "0", "0.5", "5" })
                Assert.False(Wirft(c, "UPDATE Tab_Zone SET Regler_Proportionalband = " + gut), gut);
            foreach (string schlecht in new[] { "-0.1", "'abc'" })
                Assert.True(Wirft(c, "UPDATE Tab_Zone SET Regler_Proportionalband = " + schlecht), schlecht);
            foreach (string gut in new[] { "NULL", "0", "8760" })
                Assert.False(Wirft(c, "UPDATE Tab_ErgebnisZone SET Uebergabe_Begrenzt_H = " + gut), gut);
            foreach (string schlecht in new[] { "-1", "8761" })
                Assert.True(Wirft(c, "UPDATE Tab_ErgebnisZone SET Uebergabe_Begrenzt_H = " + schlecht), schlecht);
            Assert.True(Wirft(c, "UPDATE Tab_Zone SET Auslegung_Vorlauf = 'warm'"));
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank und die Rundreise
        // =============================================================================

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= ZonenUebergabeSchema.SCHRITT);
            Assert.True(ZonenUebergabeSchema.Vollstaendig());
            Assert.True(ZonenUebergabeSchema.ZoneVorhanden());
            // Gesät ist allein die Zone „Gastronomie und Verwaltung" des Referenzprojekts 1054
            // (ZonenHeizkreisReferenzprojektWacheTests); jede andere Zone steht leer.
            foreach (string s in ZonenUebergabeSchema.SPALTEN_ZONE)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Zone WHERE \"" + s + "\" IS NOT NULL AND " +
                                      "NOT (Bezeichner = 'Gastronomie und Verwaltung' AND ID_Gebaeude IN (SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = 1054))"));
        }

        /// <summary>Rundreise 180 → 181: aus dem Stand davor legt der Schritt sieben Spalten an; zweimal = nichts.</summary>
        [Fact]
        public void Der_Schritt_hebt_den_Stand_davor_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            foreach (var s in ZonenUebergabeSchema.SPALTEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Spalte + "\"");
            GebaeudeZonenanschluss.ProbeVerwerfen();
            long zeilen = Zahl("SELECT COUNT(*) FROM Tab_Zone");
            Assert.False(ZonenUebergabeSchema.Vollstaendig());
            Assert.False(GebaeudeZonenanschluss.UebergabespaltenVorhanden());
            // Ohne die Spalten liest und schreibt das Aggregat weiter (älterer Stand, etwa iOS).
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, DreiZonen()).Ok);

            var bericht = new List<string>();
            Assert.Equal(7, ZonenUebergabeSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("KEIN DML", StringComparison.Ordinal));
            Assert.True(ZonenUebergabeSchema.Vollstaendig());
            Assert.True(GebaeudeZonenanschluss.UebergabespaltenVorhanden());
            Assert.Equal(zeilen + 3, Zahl("SELECT COUNT(*) FROM Tab_Zone"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            Assert.Equal(0, ZonenUebergabeSchema.Ausfuehren(null));
        }

        // =============================================================================
        //  Teil 3 - Aggregat, Prüfregeln, Kopierwege
        // =============================================================================

        /// <summary>Drei Zonen: ausdrücklich gesetzt, ausdrücklich ideal, ganz leer (wie Gebäude).</summary>
        private static List<ZoneModel> DreiZonen()
        {
            ZoneModel gesetzt = GebaeudeG3PruefregelTests.GueltigeZone();
            gesetzt.Bezeichner = "Büro";
            gesetzt.Nutzflaeche = 40;
            gesetzt.Uebergabe_Art = DbWerte.UEBERGABE_FLAECHE;
            gesetzt.Auslegung_Vorlauf = 38.0;
            gesetzt.Auslegung_Ruecklauf = 30.0;
            gesetzt.Auslegung_Raumtemperatur = 21.0;
            gesetzt.Regler_Proportionalband = 0.5;

            ZoneModel ideal = GebaeudeG3PruefregelTests.GueltigeZone();
            ideal.Bezeichner = "Lager";
            ideal.Nutzflaeche = 30;
            ideal.Uebergabe_Art = DbWerte.UEBERGABE_IDEAL;
            ideal.Regler_Proportionalband = 0.0;

            ZoneModel leer = GebaeudeG3PruefregelTests.GueltigeZone();
            leer.Bezeichner = "Flur";
            leer.Nutzflaeche = 20;
            return new List<ZoneModel> { gesetzt, ideal, leer };
        }

        private static void PruefeDreiZonen(IList<ZoneModel> g)
        {
            Assert.Equal(new[] { "Büro", "Lager", "Flur" }, g.Select(z => z.Bezeichner));
            Assert.Equal(38.0, g[0].Auslegung_Vorlauf);
            Assert.Equal(30.0, g[0].Auslegung_Ruecklauf);
            Assert.Equal(21.0, g[0].Auslegung_Raumtemperatur);
            Assert.Equal(0.5, g[0].Regler_Proportionalband);
            Assert.Null(g[1].Auslegung_Vorlauf);
            Assert.Equal(0.0, g[1].Regler_Proportionalband);
            Assert.Null(g[2].Auslegung_Vorlauf);
            Assert.Null(g[2].Auslegung_Ruecklauf);
            Assert.Null(g[2].Auslegung_Raumtemperatur);
            Assert.Null(g[2].Regler_Proportionalband);
        }

        [Fact]
        public void Das_Aggregat_traegt_die_Uebergabe_der_Zone_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            GebaeudeZonenCtrl.Ergebnis e = ctrl.SpeichernJeGebaeude(GEBAEUDE, DreiZonen());
            Assert.True(e.Ok, e.Meldung);
            List<ZoneModel> g = ctrl.LesenJeGebaeude(GEBAEUDE);
            PruefeDreiZonen(g);

            g[0].Auslegung_Raumtemperatur = null;
            g[2].Regler_Proportionalband = 2.0;
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, g).Ok);
            List<ZoneModel> h = ctrl.LesenJeGebaeude(GEBAEUDE);
            Assert.Null(h[0].Auslegung_Raumtemperatur);
            Assert.Equal(38.0, h[0].Auslegung_Vorlauf);
            Assert.Equal(2.0, h[2].Regler_Proportionalband);

            List<ZoneModel> p = ctrl.LesenJeProjekt(PROJEKT)[GEBAEUDE];
            Assert.Equal(h.Select(z => z.Regler_Proportionalband), p.Select(z => z.Regler_Proportionalband));
            Assert.Equal(h.Select(z => z.Auslegung_Vorlauf), p.Select(z => z.Auslegung_Vorlauf));
        }

        [Fact]
        public void Die_Pruefregeln_halten_die_Baender_des_Gebaeudes()
        {
            ZoneModel z = GebaeudeG3PruefregelTests.GueltigeZone();
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { z }));

            z.Auslegung_Vorlauf = 95.0;
            Assert.StartsWith("Zone „Wohnen“: Der Auslegungsvorlauf", GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { z }));
            z.Auslegung_Vorlauf = 45.0;
            z.Auslegung_Ruecklauf = 45.0;
            Assert.StartsWith("Zone „Wohnen“: Der Auslegungsrücklauf", GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { z }));
            z.Auslegung_Ruecklauf = 35.0;
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { z }));
            z.Auslegung_Vorlauf = null;          // Rücklauf allein: gegen den Vorlauf prüft der Eingang
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { z }));
            z.Auslegung_Raumtemperatur = 30.0;
            Assert.StartsWith("Zone „Wohnen“: Die Auslegungsraumtemperatur", GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { z }));
            z.Auslegung_Raumtemperatur = 20.0;
            z.Regler_Proportionalband = -0.5;
            Assert.StartsWith("Zone „Wohnen“: Das Proportionalband", GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { z }));
            z.Regler_Proportionalband = 0.0;
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { z }));
        }

        private static List<ZoneModel> Zonen(int idProjekt)
            => new GebaeudeZonenCtrl().LesenJeProjekt(idProjekt).Values.Single();

        [Fact]
        public void Das_Projektduplikat_traegt_die_Uebergabe_der_Zone()
        {
            if (!_db.Vorhanden) return;
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, DreiZonen()).Ok);
            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Zone AK1z");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            PruefeDreiZonen(Zonen(neu));
        }

        [Fact]
        public void Der_Projekttransfer_traegt_die_Uebergabe_der_Zone()
        {
            if (!_db.Vorhanden) return;
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, DreiZonen()).Ok);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-ak1z-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "z.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer Zone AK1z", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeDreiZonen(Zonen(neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        [Fact]
        public void Zoneneingaben_tragen_die_sieben_Uebergabefelder()
        {
            ZoneModel z = DreiZonen()[0];
            z.Uebergabe_Exponent = 1.2;
            z.Uebergabe_Leistung_Nenn = 4.0;
            Zoneneingaben e = Zoneneingaben.Aus(z);
            Assert.Equal(DbWerte.UEBERGABE_FLAECHE, e.UebergabeArt);
            Assert.Equal(1.2, e.UebergabeExponent);
            Assert.Equal(4.0, e.UebergabeLeistungNennKw);
            Assert.Equal(38.0, e.AuslegungVorlaufC);
            Assert.Equal(30.0, e.AuslegungRuecklaufC);
            Assert.Equal(21.0, e.AuslegungRaumtemperaturC);
            Assert.Equal(0.5, e.ReglerProportionalbandK);
            Zoneneingaben leer = Zoneneingaben.Aus(DreiZonen()[2]);
            Assert.Null(leer.UebergabeArt);
            Assert.Null(leer.AuslegungVorlaufC);
            Assert.Null(leer.ReglerProportionalbandK);
        }

        // =============================================================================
        //  Teil 4 - die Kaskade je Zone (rein)
        // =============================================================================

        private static readonly Gebaeudeuebergabe RadiatorLeer = new(DbWerte.UEBERGABE_RADIATOR, null, null, null, null, null);
        private static readonly Gebaeudeuebergabe RadiatorVoll = new(DbWerte.UEBERGABE_RADIATOR, 1.25, 60.0, 40.0, 22.0, 2.0);

        [Fact]
        public void Leere_Zone_erbt_die_Vorgaben_der_Gebaeudeart()
        {
            Zonenuebergabe u = Zonenuebergabevorgaben.Aufloesen(new Zoneneingaben(), RadiatorLeer, 20.5, 10000.0, 1.0);
            Assert.False(u.Ideal);
            Assert.Equal(DbWerte.UEBERGABE_RADIATOR, u.Art);
            Assert.Equal(GebaeudeFestwerte.UEBERGABE_EXPONENT_RADIATOR, u.Exponent);
            Assert.Equal(GebaeudeFestwerte.AUSLEGUNG_VORLAUF_RADIATOR, u.AuslegungVorlaufC);
            Assert.Equal(GebaeudeFestwerte.AUSLEGUNG_RUECKLAUF_RADIATOR, u.AuslegungRuecklaufC);
            Assert.Equal(20.5, u.AuslegungRaumtemperaturC);       // Tag-Sollwert der Zone
            Assert.Equal(GebaeudeFestwerte.VORGABE_REGLER_PROPORTIONALBAND_K, u.ReglerProportionalbandK);
            Assert.Equal(10000.0, u.NennleistungW);
            Assert.Equal(Vorgabeherkunft.GebaeudeAnteilig, u.NennleistungHerkunft);
        }

        [Fact]
        public void Leere_Zone_erbt_die_ausdruecklichen_Gebaeudewerte()
        {
            Zonenuebergabe u = Zonenuebergabevorgaben.Aufloesen(new Zoneneingaben(), RadiatorVoll, 20.5, 10000.0, 0.25);
            Assert.Equal(1.25, u.Exponent);
            Assert.Equal(60.0, u.AuslegungVorlaufC);
            Assert.Equal(40.0, u.AuslegungRuecklaufC);
            Assert.Equal(22.0, u.AuslegungRaumtemperaturC);
            Assert.Equal(2.0, u.ReglerProportionalbandK);
            Assert.Equal(2500.0, u.NennleistungW);
        }

        [Fact]
        public void Zonenwerte_gehen_vor_je_Feld()
        {
            var zone = new Zoneneingaben(UebergabeArt: DbWerte.UEBERGABE_FLAECHE, UebergabeExponent: 1.05,
                                         UebergabeLeistungNennKw: 3.0, AuslegungVorlaufC: 33.0, AuslegungRuecklaufC: 27.0,
                                         AuslegungRaumtemperaturC: 19.0, ReglerProportionalbandK: 0.5);
            Zonenuebergabe u = Zonenuebergabevorgaben.Aufloesen(zone, RadiatorVoll, 20.5, 10000.0, 0.25, 2.0);
            Assert.Equal(DbWerte.UEBERGABE_FLAECHE, u.Art);
            Assert.Equal(1.05, u.Exponent);
            Assert.Equal(33.0, u.AuslegungVorlaufC);
            Assert.Equal(27.0, u.AuslegungRuecklaufC);
            Assert.Equal(19.0, u.AuslegungRaumtemperaturC);
            Assert.Equal(0.5, u.ReglerProportionalbandK);
            Assert.Equal(1500.0, u.NennleistungW);                 // 1000 · 3 kW / Skalierung 2
            Assert.Equal(Vorgabeherkunft.Zone, u.NennleistungHerkunft);
        }

        /// <summary>Eine Zonenart ohne eigene Zahlen nimmt die Vorgaben ihrer EIGENEN Art, nicht der Gebäudeart.</summary>
        [Fact]
        public void Abweichende_Zonenart_nimmt_die_Vorgaben_ihrer_Art()
        {
            var zone = new Zoneneingaben(UebergabeArt: DbWerte.UEBERGABE_FLAECHE);
            Zonenuebergabe u = Zonenuebergabevorgaben.Aufloesen(zone, RadiatorLeer, 20.0, 8000.0, 0.5);
            Assert.Equal(GebaeudeFestwerte.UEBERGABE_EXPONENT_FLAECHE, u.Exponent);
            Assert.Equal(GebaeudeFestwerte.AUSLEGUNG_VORLAUF_FLAECHE, u.AuslegungVorlaufC);
            Assert.Equal(GebaeudeFestwerte.AUSLEGUNG_RUECKLAUF_FLAECHE, u.AuslegungRuecklaufC);
            Assert.Equal(4000.0, u.NennleistungW);
        }

        [Fact]
        public void IDEAL_an_der_Zone_rechnet_ideal_im_gekoppelten_Gebaeude()
        {
            Zonenuebergabe u = Zonenuebergabevorgaben.Aufloesen(new Zoneneingaben(UebergabeArt: DbWerte.UEBERGABE_IDEAL),
                                                                RadiatorVoll, 20.0, 8000.0, 0.5);
            Assert.True(u.Ideal);
            Assert.Equal(DbWerte.UEBERGABE_IDEAL, u.Art);
            Assert.True(double.IsNaN(u.NennleistungW));
            Assert.Equal(Vorgabeherkunft.Leer, u.NennleistungHerkunft);

            // Ein ungekoppeltes Gebäude (Art leer) und eine leere Zone: ideal.
            Zonenuebergabe v = Zonenuebergabevorgaben.Aufloesen(new Zoneneingaben(),
                                                                new Gebaeudeuebergabe(null, null, null, null, null, null),
                                                                20.0, 8000.0, 1.0);
            Assert.True(v.Ideal);
            Assert.Null(v.Art);
        }

        [Fact]
        public void Der_Flaechenanteil_zaehlt_nur_beheizte_Zonen()
        {
            var a = new Zoneneingaben(Nutzflaeche: 60.0);
            var b = new Zoneneingaben(Nutzflaeche: 20.0);
            var kalt = new Zoneneingaben(Nutzflaeche: 100.0, IstBeheizt: false);
            var alle = new[] { a, b, kalt };
            Assert.Equal(0.75, Zonenuebergabevorgaben.FlaechenanteilBeheizt(a, alle));
            Assert.Equal(0.25, Zonenuebergabevorgaben.FlaechenanteilBeheizt(b, alle));
            Assert.Equal(0.0, Zonenuebergabevorgaben.FlaechenanteilBeheizt(kalt, alle));

            // Eine einzige beheizte Zone trägt alles, auch ohne eigene Nutzfläche.
            var einzig = new Zoneneingaben();
            Assert.Equal(1.0, Zonenuebergabevorgaben.FlaechenanteilBeheizt(einzig, new[] { einzig, kalt }));
            // Ab zwei beheizten Zonen ohne Fläche: nicht bestimmbar.
            Assert.True(double.IsNaN(Zonenuebergabevorgaben.FlaechenanteilBeheizt(einzig, new[] { einzig, a })));

            // Die Summe der Anteile × Gebäudenennleistung ist die Gebäudenennleistung.
            double summe = alle.Sum(z => Zonenuebergabevorgaben.Aufloesen(z, RadiatorLeer, 20.0, 12000.0,
                                             Zonenuebergabevorgaben.FlaechenanteilBeheizt(z, alle)).NennleistungW);
            Assert.Equal(12000.0, summe, 9);
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

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
    }
}
