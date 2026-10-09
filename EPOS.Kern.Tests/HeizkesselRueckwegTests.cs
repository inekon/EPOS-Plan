using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Rückweg „In die Datenbank übernehmen…" des Heizkessels</b> (Konzept Projektdialoge mit Katalogauswahl 5.2,
    /// KA‑E‑9; Kernweg <see cref="Katalogrueckweg"/>, Gewerk <see cref="HeizkesselStammCtrl.Rueckweg"/>): neu,
    /// überschreiben, die drei Sperrgründe, Namenszusatz, Kosten als Satzvorlage samt Vorrang, Kindzeilen, Katalogpaket
    /// und die Transaktion.
    /// </summary>
    [Collection("Testdatenbank")]
    public class HeizkesselRueckwegTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        private const string KOPIE = HeizkesselStammCtrl.TABELLE_PROJEKT;
        private const string KATALOG = HeizkesselStammCtrl.TABLE;

        /// <summary>Die erste Projektkopie mit Anlage: (ID, Projekt, Name, Anlage).</summary>
        private static (int Id, int Projekt, string Name, int Anlage) Kopie()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT h.ID, h.ID_Projekt, h.Bezeichner, (SELECT MIN(e.ID) FROM Tab_Energieanlagen e WHERE e.ID_Kessel = h.ID " +
                "AND e.ID_Projekt = h.ID_Projekt) AS Anlage FROM Tab_Heizkessel h WHERE Anlage IS NOT NULL ORDER BY h.ID LIMIT 1");
            DataRow r = dt.Rows[0];
            return (I(r[0]), I(r[1]), Convert.ToString(r[2], CultureInfo.InvariantCulture), I(r[3]));
        }

        private static bool Leer(object o) => o == null || o == DBNull.Value;
        private static int I(object o) => Convert.ToInt32(o, CultureInfo.InvariantCulture);
        private static object Wert(string sql, params object[] p)
            => DataRepository.ExecuteScalar(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray());
        private static void Sql(string sql, params object[] p)
            => DataRepository.ExecuteSQL(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray());
        private static int Anzahl(string sql, params object[] p) => I(Wert(sql, p));

        private static Rueckwegergebnis Neu(int id, string name)
            => HeizkesselStammCtrl.AusProjektUebernehmen(new[] { new Rueckwegauftrag(id, Rueckwegart.Neu, name) });
        private static Rueckwegergebnis Ueberschreiben(int id)
            => HeizkesselStammCtrl.AusProjektUebernehmen(new[] { new Rueckwegauftrag(id, Rueckwegart.Ueberschreiben, "") });

        [Fact]
        public void Neu_legt_einen_ungesperrten_Satz_an_und_die_Kopie_bekommt_ihn_als_Ursprung()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("UPDATE Tab_Heizkessel SET Ptherm = 77.5, Beschreibung = 'aus dem Projekt' WHERE ID = ?", k.Id);

            Rueckwegergebnis e = Neu(k.Id, "Rueckweg Neu");

            Assert.True(e.Ok, e.Meldung);
            Rueckwegsatz s = Assert.Single(e.Saetze);
            Assert.True(s.Neu);
            Assert.Equal(0, Anzahl("SELECT ReadOnly FROM Tab_Heizkessel_STAMM WHERE ID = ?", s.IdKatalog));
            Assert.Equal(77.5, Convert.ToDouble(Wert("SELECT Ptherm FROM Tab_Heizkessel_STAMM WHERE ID = ?", s.IdKatalog), CultureInfo.InvariantCulture));
            Assert.Equal("aus dem Projekt", Wert("SELECT Beschreibung FROM Tab_Heizkessel_STAMM WHERE ID = ?", s.IdKatalog));
            Assert.Equal("Rueckweg Neu", Wert("SELECT Bezeichner FROM Tab_Heizkessel_STAMM WHERE ID = ?", s.IdKatalog));
            Assert.True(Leer(Wert("SELECT Katalog_Schluessel FROM Tab_Heizkessel_STAMM WHERE ID = ?", s.IdKatalog)));
            Assert.Equal(s.IdKatalog, Anzahl("SELECT ID_Stamm FROM Tab_Heizkessel WHERE ID = ?", k.Id));
            Assert.Contains("Rueckweg Neu", e.Meldung);
        }

        [Fact]
        public void Ein_zweiter_Rueckweg_ueberschreibt_den_Ursprung_und_behaelt_seinen_Namen()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            int neu = Neu(k.Id, "Rueckweg Ursprung").Saetze[0].IdKatalog;
            int vorher = Anzahl("SELECT COUNT(*) FROM Tab_Heizkessel_STAMM");
            Sql("UPDATE Tab_Heizkessel SET Ptherm = 91, Bezeichner = 'anders benannt' WHERE ID = ?", k.Id);

            Rueckwegzeile z = Assert.Single(HeizkesselStammCtrl.RueckwegVorschau(new[] { k.Id }));
            Assert.Equal(Rueckwegabsage.Keine, z.Ueberschreiben);
            Assert.Equal("Rueckweg Ursprung", z.NameUrsprung);

            Rueckwegergebnis e = Ueberschreiben(k.Id);

            Assert.True(e.Ok, e.Meldung);
            Assert.False(e.Saetze[0].Neu);
            Assert.Equal(neu, e.Saetze[0].IdKatalog);
            Assert.Equal(vorher, Anzahl("SELECT COUNT(*) FROM Tab_Heizkessel_STAMM"));
            Assert.Equal(91.0, Convert.ToDouble(Wert("SELECT Ptherm FROM Tab_Heizkessel_STAMM WHERE ID = ?", neu), CultureInfo.InvariantCulture));
            Assert.Equal("Rueckweg Ursprung", Wert("SELECT Bezeichner FROM Tab_Heizkessel_STAMM WHERE ID = ?", neu));
        }

        [Fact]
        public void Ein_gesperrter_Ursprung_wird_nicht_ueberschrieben()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            int neu = Neu(k.Id, "Rueckweg Gesperrt").Saetze[0].IdKatalog;
            Sql("UPDATE Tab_Heizkessel_STAMM SET ReadOnly = 1 WHERE ID = ?", neu);
            Sql("UPDATE Tab_Heizkessel SET Ptherm = 12 WHERE ID = ?", k.Id);

            Assert.Equal(Rueckwegabsage.UrsprungGesperrt, HeizkesselStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Ueberschreiben);
            Rueckwegergebnis e = Ueberschreiben(k.Id);

            Assert.False(e.Ok);
            Assert.Equal(Rueckwegabsage.UrsprungGesperrt, e.Absage);
            Assert.NotEqual(12.0, Convert.ToDouble(Wert("SELECT Ptherm FROM Tab_Heizkessel_STAMM WHERE ID = ?", neu), CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Ein_Verweis_ins_Leere_heisst_Ursprung_nicht_mehr_vorhanden()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                v.Ausfuehren("UPDATE Tab_Heizkessel SET ID_Stamm = 987654 WHERE ID = ?", new DbParam("@id", k.Id));
                v.Commit();
            }

            Assert.Equal(Rueckwegabsage.UrsprungFehlt, HeizkesselStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Ueberschreiben);
            Rueckwegergebnis e = Ueberschreiben(k.Id);
            Assert.False(e.Ok);
            Assert.Equal(Rueckwegabsage.UrsprungFehlt, e.Absage);
        }

        [Fact]
        public void Eine_Kopie_ohne_Verweis_kennt_ihren_Ursprung_nicht_und_ein_geloeschter_Ursprung_leert_ihn()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Assert.Equal(Rueckwegabsage.UrsprungUnbekannt, HeizkesselStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Ueberschreiben);
            Assert.Equal(Rueckwegabsage.UrsprungUnbekannt, Ueberschreiben(k.Id).Absage);

            // ON DELETE SET NULL: Nach dem Loeschen des Ursprungs ist die Kopie wieder „nicht bekannt".
            int neu = Neu(k.Id, "Rueckweg Geloescht").Saetze[0].IdKatalog;
            Sql("DELETE FROM Tab_Heizkessel_STAMM WHERE ID = ?", neu);
            Assert.True(Leer(Wert("SELECT ID_Stamm FROM Tab_Heizkessel WHERE ID = ?", k.Id)));
        }

        [Fact]
        public void Ein_belegter_Name_bekommt_den_Zusatz_Projekt_dann_mit_Zaehler_und_wird_nie_gespeichert()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("UPDATE Tab_Heizkessel SET Bezeichner = 'Rueckweg Name' WHERE ID = ?", k.Id);
            Assert.Equal("Rueckweg Name", HeizkesselStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Namensvorschlag);

            Assert.True(Neu(k.Id, "Rueckweg Name").Ok);
            string mit = string.Format(Resource.Culture, Resource.KATRUECK_NAME_ZUSATZ, "Rueckweg Name");
            Assert.Equal(mit, HeizkesselStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Namensvorschlag);

            Assert.True(Neu(k.Id, mit).Ok);
            Assert.Equal(string.Format(Resource.Culture, Resource.KATRUECK_NAME_ZUSATZ_N, "Rueckweg Name", "2"),
                         HeizkesselStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Namensvorschlag);

            int vorher = Anzahl("SELECT COUNT(*) FROM Tab_Heizkessel_STAMM");
            Rueckwegergebnis e = Neu(k.Id, "Rueckweg Name");
            Assert.False(e.Ok);
            Assert.Equal(Rueckwegabsage.NameBelegt, e.Absage);
            Assert.Equal(Rueckwegabsage.NameLeer, Neu(k.Id, "  ").Absage);
            Assert.Equal(vorher, Anzahl("SELECT COUNT(*) FROM Tab_Heizkessel_STAMM"));
        }

        [Fact]
        public void Derselbe_Name_zweimal_in_einem_Aufruf_wird_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            var ids = DataRepository.GetDataTable("SELECT ID FROM Tab_Heizkessel ORDER BY ID LIMIT 2").Rows
                .Cast<DataRow>().Select(r => I(r[0])).ToList();
            Rueckwegergebnis e = HeizkesselStammCtrl.AusProjektUebernehmen(new[]
            {
                new Rueckwegauftrag(ids[0], Rueckwegart.Neu, "Rueckweg Doppelt"),
                new Rueckwegauftrag(ids[1], Rueckwegart.Neu, "Rueckweg Doppelt"),
            });
            Assert.Equal(Rueckwegabsage.NameBelegt, e.Absage);
            Assert.False(HeizkesselStammCtrl.RueckwegNameBelegt("Rueckweg Doppelt"));
        }

        [Fact]
        public void Scheitert_der_zweite_Satz_wird_auch_der_erste_nicht_geschrieben()
        {
            if (!_db.Vorhanden) return;
            var ids = DataRepository.GetDataTable("SELECT ID FROM Tab_Heizkessel ORDER BY ID LIMIT 2").Rows
                .Cast<DataRow>().Select(r => I(r[0])).ToList();
            int vorher = Anzahl("SELECT COUNT(*) FROM Tab_Heizkessel_STAMM");

            // Der zweite will einen unbekannten Ursprung ueberschreiben - die Absage kommt IM Vorgang, nach dem ersten INSERT.
            Rueckwegergebnis e = HeizkesselStammCtrl.AusProjektUebernehmen(new[]
            {
                new Rueckwegauftrag(ids[0], Rueckwegart.Neu, "Rueckweg Transaktion"),
                new Rueckwegauftrag(ids[1], Rueckwegart.Ueberschreiben, ""),
            });

            Assert.False(e.Ok);
            Assert.Equal(ids[1], e.IdKopie);
            Assert.Equal(vorher, Anzahl("SELECT COUNT(*) FROM Tab_Heizkessel_STAMM"));
            Assert.False(HeizkesselStammCtrl.RueckwegNameBelegt("Rueckweg Transaktion"));
            Assert.True(Leer(Wert("SELECT ID_Stamm FROM Tab_Heizkessel WHERE ID = ?", ids[0])));
        }

        [Fact]
        public void Ein_Pruefverstoss_der_Kopie_wird_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("UPDATE Tab_Heizkessel SET Ptherm = 20, Mindestleistung = 500 WHERE ID = ?", k.Id);
            Rueckwegergebnis e = Neu(k.Id, "Rueckweg Verstoss");
            Assert.Equal(Rueckwegabsage.Pruefverstoss, e.Absage);
            Assert.False(HeizkesselStammCtrl.RueckwegNameBelegt("Rueckweg Verstoss"));
        }

        [Fact]
        public void Die_Betriebskosten_der_Anlage_werden_Kostenvorlage_des_Satzes_und_gehen_der_Standardvorlage_vor()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("DELETE FROM Tab_ProjektWerte WHERE ID_Anlage = ? AND KategorieID = ?", k.Anlage, DbWerte.KOSTEN_KATEGORIE_BETRIEB);
            int stamm = Anzahl("SELECT MIN(StammID) FROM Tab_Kostenfaktor");
            Sql("INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, Bemessung, " +
                "Kostenart, ID_Anlage, Nutzungsdauer) VALUES (?, ?, 2, ?, 432.1, 'BETRAG', 'BETRIEB', ?, 15)",
                k.Projekt, stamm, DbWerte.KOSTEN_KATEGORIE_BETRIEB, k.Anlage);

            Rueckwegergebnis e = Neu(k.Id, "Rueckweg Kosten");
            Assert.True(e.Ok, e.Meldung);
            int satz = e.Saetze[0].IdKatalog;
            Assert.Equal(1, e.Saetze[0].Kostenpositionen);
            int vorlage = Anzahl("SELECT ID_KostenVorlage FROM Tab_Heizkessel_STAMM WHERE ID = ?", satz);
            Assert.Equal("Rueckweg Kosten", Wert("SELECT Name FROM Tab_KostenVorlage WHERE ID = ?", vorlage));
            Assert.Equal(0, Anzahl("SELECT IstStandard FROM Tab_KostenVorlage WHERE ID = ?", vorlage));
            Assert.Equal(HeizkesselStammCtrl.KOMPONENTE_KOSTEN, Anzahl("SELECT KomponentenID FROM Tab_KostenVorlage WHERE ID = ?", vorlage));
            Assert.Equal(432.1, Convert.ToDouble(Wert("SELECT Satz FROM Tab_KostenVorlagePosition WHERE VorlageID = ?", vorlage), CultureInfo.InvariantCulture));

            // Ein zweiter Rueckweg (ueberschreiben) ersetzt die Positionen derselben Vorlage, statt eine zweite anzulegen.
            Sql("UPDATE Tab_ProjektWerte SET EingegebenerWert = 500 WHERE ID_Anlage = ? AND KategorieID = ?", k.Anlage, DbWerte.KOSTEN_KATEGORIE_BETRIEB);
            Assert.True(Ueberschreiben(k.Id).Ok);
            Assert.Equal(vorlage, Anzahl("SELECT ID_KostenVorlage FROM Tab_Heizkessel_STAMM WHERE ID = ?", satz));
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlagePosition WHERE VorlageID = ?", vorlage));

            // VORRANG: Die Anlage zeigt ueber die Kopie auf den Satz - ihre Satzvorlage geht der Standardvorlage vor.
            KostenVorlageKopf kopf = Katalogrueckweg.SatzvorlageDerAnlage(k.Anlage, WizardItemClass.KESSEL_TYP, HeizkesselStammCtrl.KOMPONENTE_KOSTEN);
            Assert.NotNull(kopf);
            Assert.Equal(vorlage, kopf.Id);
            Sql("DELETE FROM Tab_ProjektWerte WHERE ID_Anlage = ? AND KategorieID = ?", k.Anlage, DbWerte.KOSTEN_KATEGORIE_BETRIEB);
            KostenVorlagenUebernahmeCtrl.PflichtpositionenSicherstellen(k.Projekt);
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage = ? AND KategorieID = ? AND VorlageID = ?",
                                   k.Anlage, DbWerte.KOSTEN_KATEGORIE_BETRIEB, vorlage));
        }

        [Fact]
        public void Ueberschreiben_eines_Auslieferungssatzes_laesst_Schluessel_und_Pruefsumme_stehen()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            int satz = Neu(k.Id, "Rueckweg Paket").Saetze[0].IdKatalog;
            string summe = new string('a', 64);
            Sql("UPDATE Tab_Heizkessel_STAMM SET Katalog_Schluessel = 'KES:Rueckweg', Katalog_Pruefsumme = ? WHERE ID = ?", summe, satz);
            Sql("UPDATE Tab_Heizkessel SET Ptherm = 55 WHERE ID = ?", k.Id);

            Assert.True(Ueberschreiben(k.Id).Ok);

            Assert.Equal("KES:Rueckweg", Wert("SELECT Katalog_Schluessel FROM Tab_Heizkessel_STAMM WHERE ID = ?", satz));
            Assert.Equal(summe, Wert("SELECT Katalog_Pruefsumme FROM Tab_Heizkessel_STAMM WHERE ID = ?", satz));
            Assert.Equal(55.0, Convert.ToDouble(Wert("SELECT Ptherm FROM Tab_Heizkessel_STAMM WHERE ID = ?", satz), CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Was_mitgeht_ist_die_Schnittmenge_ohne_Verwaltungsspalten_und_der_Kessel_hat_keine_Kindtabellen()
        {
            if (!_db.Vorhanden) return;
            IReadOnlyList<string> spalten = Katalogrueckweg.Spalten(HeizkesselStammCtrl.Rueckweg());
            foreach (string aus in new[] { "ID", "ID_Projekt", "ID_Stamm", "ReadOnly", "ID_KostenVorlage", "Bezeichner",
                                           "Katalog_Schluessel", "Katalog_Pruefsumme", "Katalog_Ausgelaufen" })
                Assert.DoesNotContain(aus, spalten);
            foreach (string mit in new[] { "Ptherm", "Investitionskosten", "Wartungskosten", "Wartungskosten_Einheit",
                                           "Nutzungsdauer", "Wirkungsgrad_Teillast30", "Mindestlaufzeit_min", "Bereitschaft_Einheit" })
                Assert.Contains(mit, spalten);
            Assert.Empty(HeizkesselStammCtrl.Rueckweg().Kinder);
        }

        [Fact]
        public void Kindzeilen_gehen_bei_neu_als_Kopie_und_ersetzen_beim_Ueberschreiben_die_des_Ursprungs()
        {
            if (!_db.Vorhanden) return;
            // Ein Gewerk mit Kindtabelle (wie Tab_Kenndaten an der Waermepumpe), eigens fuer den Mechanismus angelegt.
            Sql("CREATE TABLE Tab_RwKind (ID INTEGER PRIMARY KEY, ID_Kessel INTEGER, ID_Projekt INTEGER, Wert REAL)");
            Sql("CREATE TABLE Tab_RwKind_STAMM (ID INTEGER PRIMARY KEY, ID_Kessel INTEGER, Wert REAL)");
            var k = Kopie();
            Sql("INSERT INTO Tab_RwKind (ID_Kessel, ID_Projekt, Wert) VALUES (?, ?, 1), (?, ?, 2)", k.Id, k.Projekt, k.Id, k.Projekt);
            var g = new Rueckweggewerk
            {
                Kopietabelle = KOPIE, Katalogtabelle = KATALOG, Anlagenverweis = "ID_Kessel", KomponentenId = 2,
                Kinder = new[] { ("Tab_RwKind", "Tab_RwKind_STAMM", "ID_Kessel") },
            };

            Rueckwegergebnis e = Katalogrueckweg.Uebernehmen(g, new[] { new Rueckwegauftrag(k.Id, Rueckwegart.Neu, "Rueckweg Kind") });
            Assert.True(e.Ok, e.Meldung);
            int satz = e.Saetze[0].IdKatalog;
            Assert.Equal(2, e.Saetze[0].Kindzeilen);
            Assert.Equal(3.0, Convert.ToDouble(Wert("SELECT SUM(Wert) FROM Tab_RwKind_STAMM WHERE ID_Kessel = ?", satz), CultureInfo.InvariantCulture));

            Sql("DELETE FROM Tab_RwKind WHERE Wert = 1");
            Assert.True(Katalogrueckweg.Uebernehmen(g, new[] { new Rueckwegauftrag(k.Id, Rueckwegart.Ueberschreiben, "") }).Ok);
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_RwKind_STAMM WHERE ID_Kessel = ?", satz));
            Assert.Equal(2.0, Convert.ToDouble(Wert("SELECT Wert FROM Tab_RwKind_STAMM WHERE ID_Kessel = ?", satz), CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Die_Uebernahme_aus_dem_Katalog_traegt_den_Ursprung_ein()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            int satz = Neu(k.Id, "Rueckweg Zurueck").Saetze[0].IdKatalog;
            int neueKopie = new HeizkesselCtrl().CopyFromStamm(satz, k.Projekt);
            Assert.True(neueKopie > 0);
            Assert.Equal(satz, Anzahl("SELECT ID_Stamm FROM Tab_Heizkessel WHERE ID = ?", neueKopie));
        }
    }
}
