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
    /// <b>Rückweg „In die Datenbank übernehmen…" der Stromspeicher</b> (Konzept Projektdialoge mit Katalogauswahl 5.2, 5.6,
    /// KA‑E‑9; Kernweg <see cref="Katalogrueckweg"/>, Gewerk <see cref="StromspeicherStammCtrl.Rueckweg"/>): neu, überschreiben,
    /// die drei Sperrgründe, Namenszusatz, die vier Kostenposten und die Gerätewerte, Kosten als Satzvorlagen samt
    /// Investitionspositionen (KA‑E‑14) und Vorrang, Name der Kopie bleibt (KA‑E‑15), Löschen mit Satzvorlage (KA‑E‑16),
    /// Katalogpaket und die Transaktion. Nach dem Muster der <c>BhkwRueckwegTests</c>; geschrieben wird in der Kopie der Testdatenbank, die gesäten Stromfelder der Referenzkopien bleiben.
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromspeicherRueckwegTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        private const string KOPIE = StromspeicherStammCtrl.TABELLE_PROJEKT;
        private const string KATALOG = StromspeicherStammCtrl.TABLE;

        /// <summary>Die erste Projektkopie mit Anlage: (ID, Projekt, Name, Anlage).</summary>
        private static (int Id, int Projekt, string Name, int Anlage) Kopie()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT h.ID, h.ID_Projekt, h.Bezeichner, (SELECT MIN(e.ID) FROM Tab_Energieanlagen e WHERE e.ID_SP = h.ID " +
                "AND e.ID_Projekt = h.ID_Projekt) AS Anlage FROM Tab_Stromspeicher h WHERE Anlage IS NOT NULL ORDER BY h.ID LIMIT 1");
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
            => StromspeicherStammCtrl.AusProjektUebernehmen(new[] { new Rueckwegauftrag(id, Rueckwegart.Neu, name) });
        private static Rueckwegergebnis Ueberschreiben(int id)
            => StromspeicherStammCtrl.AusProjektUebernehmen(new[] { new Rueckwegauftrag(id, Rueckwegart.Ueberschreiben, "") });

        [Fact]
        public void Neu_legt_einen_ungesperrten_Satz_an_und_die_Kopie_bekommt_ihn_als_Ursprung()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("UPDATE Tab_Stromspeicher SET Energie = 77.5, Typ = 'aus dem Projekt' WHERE ID = ?", k.Id);

            Rueckwegergebnis e = Neu(k.Id, "Rueckweg Neu");

            Assert.True(e.Ok, e.Meldung);
            Rueckwegsatz s = Assert.Single(e.Saetze);
            Assert.True(s.Neu);
            Assert.Equal(0, Anzahl("SELECT ReadOnly FROM Tab_Stromspeicher_STAMM WHERE ID = ?", s.IdKatalog));
            Assert.Equal(77.5, Convert.ToDouble(Wert("SELECT Energie FROM Tab_Stromspeicher_STAMM WHERE ID = ?", s.IdKatalog), CultureInfo.InvariantCulture));
            Assert.Equal("aus dem Projekt", Wert("SELECT Typ FROM Tab_Stromspeicher_STAMM WHERE ID = ?", s.IdKatalog));
            Assert.Equal("Rueckweg Neu", Wert("SELECT Bezeichner FROM Tab_Stromspeicher_STAMM WHERE ID = ?", s.IdKatalog));
            Assert.True(Leer(Wert("SELECT Katalog_Schluessel FROM Tab_Stromspeicher_STAMM WHERE ID = ?", s.IdKatalog)));
            Assert.Equal(s.IdKatalog, Anzahl("SELECT ID_Stamm FROM Tab_Stromspeicher WHERE ID = ?", k.Id));
            Assert.Contains("Rueckweg Neu", e.Meldung);
        }

        [Fact]
        public void Ein_zweiter_Rueckweg_ueberschreibt_den_Ursprung_und_behaelt_seinen_Namen()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            int neu = Neu(k.Id, "Rueckweg Ursprung").Saetze[0].IdKatalog;
            int vorher = Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher_STAMM");
            Sql("UPDATE Tab_Stromspeicher SET Energie = 91, Bezeichner = 'anders benannt' WHERE ID = ?", k.Id);

            Rueckwegzeile z = Assert.Single(StromspeicherStammCtrl.RueckwegVorschau(new[] { k.Id }));
            Assert.Equal(Rueckwegabsage.Keine, z.Ueberschreiben);
            Assert.Equal("Rueckweg Ursprung", z.NameUrsprung);

            Rueckwegergebnis e = Ueberschreiben(k.Id);

            Assert.True(e.Ok, e.Meldung);
            Assert.False(e.Saetze[0].Neu);
            Assert.Equal(neu, e.Saetze[0].IdKatalog);
            Assert.Equal(vorher, Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher_STAMM"));
            Assert.Equal(91.0, Convert.ToDouble(Wert("SELECT Energie FROM Tab_Stromspeicher_STAMM WHERE ID = ?", neu), CultureInfo.InvariantCulture));
            Assert.Equal("Rueckweg Ursprung", Wert("SELECT Bezeichner FROM Tab_Stromspeicher_STAMM WHERE ID = ?", neu));
        }

        [Fact]
        public void Ein_gesperrter_Ursprung_wird_nicht_ueberschrieben()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            int neu = Neu(k.Id, "Rueckweg Gesperrt").Saetze[0].IdKatalog;
            Sql("UPDATE Tab_Stromspeicher_STAMM SET ReadOnly = 1 WHERE ID = ?", neu);
            Sql("UPDATE Tab_Stromspeicher SET Energie = 12 WHERE ID = ?", k.Id);

            Assert.Equal(Rueckwegabsage.UrsprungGesperrt, StromspeicherStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Ueberschreiben);
            Rueckwegergebnis e = Ueberschreiben(k.Id);

            Assert.False(e.Ok);
            Assert.Equal(Rueckwegabsage.UrsprungGesperrt, e.Absage);
            Assert.NotEqual(12.0, Convert.ToDouble(Wert("SELECT Energie FROM Tab_Stromspeicher_STAMM WHERE ID = ?", neu), CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Ein_Verweis_ins_Leere_heisst_Ursprung_nicht_mehr_vorhanden()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                v.Ausfuehren("UPDATE Tab_Stromspeicher SET ID_Stamm = 987654 WHERE ID = ?", new DbParam("@id", k.Id));
                v.Commit();
            }

            Assert.Equal(Rueckwegabsage.UrsprungFehlt, StromspeicherStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Ueberschreiben);
            Rueckwegergebnis e = Ueberschreiben(k.Id);
            Assert.False(e.Ok);
            Assert.Equal(Rueckwegabsage.UrsprungFehlt, e.Absage);
        }

        [Fact]
        public void Eine_Kopie_ohne_Verweis_kennt_ihren_Ursprung_nicht_und_ein_geloeschter_Ursprung_leert_ihn()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Assert.Equal(Rueckwegabsage.UrsprungUnbekannt, StromspeicherStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Ueberschreiben);
            Assert.Equal(Rueckwegabsage.UrsprungUnbekannt, Ueberschreiben(k.Id).Absage);

            // ON DELETE SET NULL: Nach dem Loeschen des Ursprungs ist die Kopie wieder „nicht bekannt".
            int neu = Neu(k.Id, "Rueckweg Geloescht").Saetze[0].IdKatalog;
            Sql("DELETE FROM Tab_Stromspeicher_STAMM WHERE ID = ?", neu);
            Assert.True(Leer(Wert("SELECT ID_Stamm FROM Tab_Stromspeicher WHERE ID = ?", k.Id)));
        }

        [Fact]
        public void Ein_belegter_Name_bekommt_den_Zusatz_Projekt_dann_mit_Zaehler_und_wird_nie_gespeichert()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("UPDATE Tab_Stromspeicher SET Bezeichner = 'Rueckweg Name' WHERE ID = ?", k.Id);
            Assert.Equal("Rueckweg Name", StromspeicherStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Namensvorschlag);

            Assert.True(Neu(k.Id, "Rueckweg Name").Ok);
            string mit = string.Format(Resource.Culture, Resource.KATRUECK_NAME_ZUSATZ, "Rueckweg Name");
            Assert.Equal(mit, StromspeicherStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Namensvorschlag);

            Assert.True(Neu(k.Id, mit).Ok);
            Assert.Equal(string.Format(Resource.Culture, Resource.KATRUECK_NAME_ZUSATZ_N, "Rueckweg Name", "2"),
                         StromspeicherStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Namensvorschlag);

            int vorher = Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher_STAMM");
            Rueckwegergebnis e = Neu(k.Id, "Rueckweg Name");
            Assert.False(e.Ok);
            Assert.Equal(Rueckwegabsage.NameBelegt, e.Absage);
            Assert.Equal(Rueckwegabsage.NameLeer, Neu(k.Id, "  ").Absage);
            Assert.Equal(vorher, Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher_STAMM"));
        }

        [Fact]
        public void Derselbe_Name_zweimal_in_einem_Aufruf_wird_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            var ids = DataRepository.GetDataTable("SELECT ID FROM Tab_Stromspeicher ORDER BY ID LIMIT 2").Rows
                .Cast<DataRow>().Select(r => I(r[0])).ToList();
            Rueckwegergebnis e = StromspeicherStammCtrl.AusProjektUebernehmen(new[]
            {
                new Rueckwegauftrag(ids[0], Rueckwegart.Neu, "Rueckweg Doppelt"),
                new Rueckwegauftrag(ids[1], Rueckwegart.Neu, "Rueckweg Doppelt"),
            });
            Assert.Equal(Rueckwegabsage.NameBelegt, e.Absage);
            Assert.False(StromspeicherStammCtrl.RueckwegNameBelegt("Rueckweg Doppelt"));
        }

        [Fact]
        public void Scheitert_der_zweite_Satz_wird_auch_der_erste_nicht_geschrieben()
        {
            if (!_db.Vorhanden) return;
            var ids = DataRepository.GetDataTable("SELECT ID FROM Tab_Stromspeicher ORDER BY ID LIMIT 2").Rows
                .Cast<DataRow>().Select(r => I(r[0])).ToList();
            int vorher = Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher_STAMM");

            // Der zweite will einen unbekannten Ursprung ueberschreiben - die Absage kommt IM Vorgang, nach dem ersten INSERT.
            Rueckwegergebnis e = StromspeicherStammCtrl.AusProjektUebernehmen(new[]
            {
                new Rueckwegauftrag(ids[0], Rueckwegart.Neu, "Rueckweg Transaktion"),
                new Rueckwegauftrag(ids[1], Rueckwegart.Ueberschreiben, ""),
            });

            Assert.False(e.Ok);
            Assert.Equal(ids[1], e.IdKopie);
            Assert.Equal(vorher, Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher_STAMM"));
            Assert.False(StromspeicherStammCtrl.RueckwegNameBelegt("Rueckweg Transaktion"));
            Assert.True(Leer(Wert("SELECT ID_Stamm FROM Tab_Stromspeicher WHERE ID = ?", ids[0])));
        }

        [Fact]
        public void Ein_Pruefverstoss_der_Kopie_wird_benannt_abgelehnt()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("UPDATE Tab_Stromspeicher SET Ladezustand = 150 WHERE ID = ?", k.Id);
            Rueckwegergebnis e = Neu(k.Id, "Rueckweg Verstoss");
            Assert.Equal(Rueckwegabsage.Pruefverstoss, e.Absage);
            Assert.False(StromspeicherStammCtrl.RueckwegNameBelegt("Rueckweg Verstoss"));
        }

        [Fact]
        public void Die_Betriebskosten_der_Anlage_werden_Kostenvorlage_des_Satzes_und_gehen_der_Standardvorlage_vor()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("DELETE FROM Tab_ProjektWerte WHERE ID_Anlage = ?", k.Anlage);
            int stamm = Anzahl("SELECT MIN(StammID) FROM Tab_Kostenfaktor");
            Sql("INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, Bemessung, " +
                "Kostenart, ID_Anlage, Nutzungsdauer) VALUES (?, ?, 5, ?, 432.1, 'BETRAG', 'BETRIEB', ?, 15)",
                k.Projekt, stamm, DbWerte.KOSTEN_KATEGORIE_BETRIEB, k.Anlage);

            Rueckwegergebnis e = Neu(k.Id, "Rueckweg Kosten");
            Assert.True(e.Ok, e.Meldung);
            int satz = e.Saetze[0].IdKatalog;
            Assert.Equal(1, e.Saetze[0].Kostenpositionen);
            int vorlage = Anzahl("SELECT ID_KostenVorlage FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz);
            Assert.Equal("Rueckweg Kosten", Wert("SELECT Name FROM Tab_KostenVorlage WHERE ID = ?", vorlage));
            Assert.Equal(0, Anzahl("SELECT IstStandard FROM Tab_KostenVorlage WHERE ID = ?", vorlage));
            Assert.Equal(StromspeicherStammCtrl.KOMPONENTE_KOSTEN, Anzahl("SELECT KomponentenID FROM Tab_KostenVorlage WHERE ID = ?", vorlage));
            Assert.Equal(432.1, Convert.ToDouble(Wert("SELECT Satz FROM Tab_KostenVorlagePosition WHERE VorlageID = ?", vorlage), CultureInfo.InvariantCulture));

            // Ein zweiter Rueckweg (ueberschreiben) ersetzt die Positionen derselben Vorlage, statt eine zweite anzulegen.
            Sql("UPDATE Tab_ProjektWerte SET EingegebenerWert = 500 WHERE ID_Anlage = ? AND KategorieID = ?", k.Anlage, DbWerte.KOSTEN_KATEGORIE_BETRIEB);
            Assert.True(Ueberschreiben(k.Id).Ok);
            Assert.Equal(vorlage, Anzahl("SELECT ID_KostenVorlage FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz));
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlagePosition WHERE VorlageID = ?", vorlage));

            // VORRANG: Die Anlage zeigt ueber die Kopie auf den Satz - ihre Satzvorlage geht der Standardvorlage vor.
            KostenVorlageKopf kopf = Katalogrueckweg.SatzvorlageDerAnlage(k.Anlage, WizardItemClass.SP_TYP, StromspeicherStammCtrl.KOMPONENTE_KOSTEN);
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
            Sql("UPDATE Tab_Stromspeicher_STAMM SET Katalog_Schluessel = 'SP:Rueckweg', Katalog_Pruefsumme = ? WHERE ID = ?", summe, satz);
            Sql("UPDATE Tab_Stromspeicher SET Energie = 55 WHERE ID = ?", k.Id);

            Assert.True(Ueberschreiben(k.Id).Ok);

            Assert.Equal("SP:Rueckweg", Wert("SELECT Katalog_Schluessel FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz));
            Assert.Equal(summe, Wert("SELECT Katalog_Pruefsumme FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz));
            Assert.Equal(55.0, Convert.ToDouble(Wert("SELECT Energie FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz), CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Was_mitgeht_ist_die_Schnittmenge_ohne_Verwaltungsspalten_und_der_Stromspeicher_hat_keine_Kindtabellen()
        {
            if (!_db.Vorhanden) return;
            IReadOnlyList<string> spalten = Katalogrueckweg.Spalten(StromspeicherStammCtrl.Rueckweg());
            foreach (string aus in new[] { "ID", "ID_Projekt", "ID_Stamm", "ReadOnly", "ID_KostenVorlage", "ID_KostenVorlageInvestition", "Bezeichner",
                                           "Katalog_Schluessel", "Katalog_Pruefsumme", "Katalog_Ausgelaufen" })
                Assert.DoesNotContain(aus, spalten);
            foreach (string mit in new[] { "Typ", "Firma", "Energie", "Leistung", "Degradation", "Ladezustand", "Modulkosten",
                                           "Wirkungsgrad_RT", "Zyklen_Zugesichert", "Verschleisskosten", "Leistungskosten",
                                           "Investition_Fix", "Standby_Verbrauch", "Selbstentladung_Prozent_Monat" })
                Assert.Contains(mit, spalten);
            Assert.Empty(StromspeicherStammCtrl.Rueckweg().Kinder);
        }

        [Fact]
        public void Die_Uebernahme_aus_dem_Katalog_traegt_den_Ursprung_ein()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            int satz = Neu(k.Id, "Rueckweg Zurueck").Saetze[0].IdKatalog;
            int neueKopie = new StromspeicherCtrl().CopyFromStamm(satz, k.Projekt);
            Assert.True(neueKopie > 0);
            Assert.Equal(satz, Anzahl("SELECT ID_Stamm FROM Tab_Stromspeicher WHERE ID = ?", neueKopie));
        }

        // ------------------------------------------------ KA-E-14: Investitionspositionen ---

        private const int BETRIEB = DbWerte.KOSTEN_KATEGORIE_BETRIEB;
        private const int INVEST = DbWerte.KOSTEN_KATEGORIE_INVESTITION;

        /// <summary>Setzt die Kostenpositionen der Anlage: <paramref name="betrieb"/> Betriebs- und
        /// <paramref name="invest"/> Investitionspositionen (Betrag 100, 200, …).</summary>
        private static void Positionen((int Id, int Projekt, string Name, int Anlage) k, int betrieb, int invest)
        {
            Sql("DELETE FROM Tab_ProjektWerte WHERE ID_Anlage = ?", k.Anlage);
            DataTable stamm = DataRepository.GetDataTable("SELECT StammID FROM Tab_Kostenfaktor ORDER BY StammID LIMIT 6");
            int n = 0;
            for (int i = 0; i < betrieb + invest; i++)
            {
                bool inv = i >= betrieb;
                Sql("INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, Bemessung, " +
                    "Kostenart, ID_Anlage, Nutzungsdauer) VALUES (?, ?, 5, ?, ?, 'BETRAG', ?, ?, 20)",
                    k.Projekt, I(stamm.Rows[n++][0]), inv ? INVEST : BETRIEB, 100.0 * (i + 1),
                    inv ? (i == betrieb ? DbWerte.KOSTENART_KAPITALGEBUNDEN : DbWerte.KOSTENART_ZUSCHUSS) : DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                    k.Anlage);
            }
        }

        private static int Vorlage(int satz) => Anzahl("SELECT ID_KostenVorlage FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz);
        private static int Invest(int satz)
        {
            object o = Wert("SELECT ID_KostenVorlageInvestition FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz);
            return Leer(o) ? 0 : I(o);
        }
        private static int Positionenzahl(int vorlage) => Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlagePosition WHERE VorlageID = ?", vorlage);

        [Fact]
        public void Investitionspositionen_reisen_als_eigene_Vorlage_mit_neu_und_beim_Ueberschreiben()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Positionen(k, 1, 2);

            Rueckwegergebnis e = Neu(k.Id, "Rueckweg Invest");
            Assert.True(e.Ok, e.Meldung);
            int satz = e.Saetze[0].IdKatalog;
            Assert.Equal(3, e.Saetze[0].Kostenpositionen);
            int betrieb = Vorlage(satz);
            int invest = Invest(satz);
            Assert.True(betrieb > 0 && invest > 0);
            Assert.Equal(INVEST, Anzahl("SELECT KategorieID FROM Tab_KostenVorlage WHERE ID = ?", invest));
            Assert.Equal(BETRIEB, Anzahl("SELECT KategorieID FROM Tab_KostenVorlage WHERE ID = ?", betrieb));
            Assert.Equal(1, Positionenzahl(betrieb));
            Assert.Equal(2, Positionenzahl(invest));
            Assert.Equal(0, Anzahl("SELECT IstStandard FROM Tab_KostenVorlage WHERE ID = ?", invest));
            Assert.Equal(2, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlagePosition WHERE VorlageID = ? AND Kostenart IN (?, ?)",
                                   invest, DbWerte.KOSTENART_KAPITALGEBUNDEN, DbWerte.KOSTENART_ZUSCHUSS));
            Assert.Equal(200.0, Convert.ToDouble(Wert("SELECT MIN(Satz) FROM Tab_KostenVorlagePosition WHERE VorlageID = ?", invest),
                                                 CultureInfo.InvariantCulture));

            // Überschreiben ersetzt vollständig: andere Beträge, eine Investitionsposition mehr, dieselben Vorlagen.
            Positionen(k, 1, 3);
            Assert.True(Ueberschreiben(k.Id).Ok);
            Assert.Equal(betrieb, Vorlage(satz));
            Assert.Equal(invest, Invest(satz));
            Assert.Equal(1, Positionenzahl(betrieb));
            Assert.Equal(3, Positionenzahl(invest));
        }

        [Fact]
        public void Ein_zweiter_Rueckweg_ersetzt_Betrieb_und_Investition_und_eine_leere_Kategorie_verliert_ihre_Vorlage()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Positionen(k, 2, 1);
            int satz = Neu(k.Id, "Rueckweg Ersatz").Saetze[0].IdKatalog;
            int betrieb = Vorlage(satz);
            int invest = Invest(satz);

            // Nur noch Investition: die Betriebsvorlage geht, ihr Verweis wird leer, die Investition bleibt.
            Positionen(k, 0, 2);
            Rueckwegergebnis e = Ueberschreiben(k.Id);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(2, e.Saetze[0].Kostenpositionen);
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlage WHERE ID = ?", betrieb));
            Assert.Equal(0, Positionenzahl(betrieb));
            Assert.True(Leer(Wert("SELECT ID_KostenVorlage FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz)));
            Assert.Equal(invest, Invest(satz));
            Assert.Equal(2, Positionenzahl(invest));

            // Wieder mit Betrieb: eine neue Betriebsvorlage, die Investitionsvorlage bleibt dieselbe.
            Positionen(k, 1, 1);
            Assert.True(Ueberschreiben(k.Id).Ok);
            int betriebNeu = Vorlage(satz);
            Assert.True(betriebNeu > 0);
            Assert.Equal(invest, Invest(satz));
            Assert.Equal(1, Positionenzahl(betriebNeu));
            Assert.Equal(1, Positionenzahl(invest));
            Assert.Equal(2, Katalogrueckweg.SatzvorlagenDerAnlage(k.Anlage, WizardItemClass.SP_TYP, StromspeicherStammCtrl.KOMPONENTE_KOSTEN).Count);
        }

        [Fact]
        public void Die_Uebernahme_mit_Vorrang_legt_auch_die_Investitionspositionen_an_und_danach_nur_Pflicht()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Positionen(k, 1, 2);
            int satz = Neu(k.Id, "Rueckweg Vorrang").Saetze[0].IdKatalog;
            int invest = Invest(satz);
            IReadOnlyList<KostenVorlageKopf> koepfe =
                Katalogrueckweg.SatzvorlagenDerAnlage(k.Anlage, WizardItemClass.SP_TYP, StromspeicherStammCtrl.KOMPONENTE_KOSTEN);
            Assert.Equal(new[] { BETRIEB, INVEST }, koepfe.Select(x => x.KategorieId).ToArray());
            Assert.Equal(Vorlage(satz), koepfe[0].Id);

            Sql("DELETE FROM Tab_ProjektWerte WHERE ID_Anlage = ?", k.Anlage);
            KostenVorlagenUebernahmeCtrl.PflichtpositionenSicherstellen(k.Projekt);
            Assert.Equal(2, Anzahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage = ? AND KategorieID = ? AND VorlageID = ?",
                                   k.Anlage, INVEST, invest));
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage = ? AND KategorieID = ?", k.Anlage, BETRIEB));

            // Eine gelöschte (nicht Pflicht-)Investitionsposition kehrt nicht zurück.
            Sql("DELETE FROM Tab_ProjektWerte WHERE ID = (SELECT MIN(ID) FROM Tab_ProjektWerte WHERE ID_Anlage = ? AND KategorieID = ?)",
                k.Anlage, INVEST);
            KostenVorlagenUebernahmeCtrl.PflichtpositionenSicherstellen(k.Projekt);
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage = ? AND KategorieID = ?", k.Anlage, INVEST));
        }

        [Fact]
        public void Die_vier_Kostenposten_und_die_Geraetewerte_gehen_mit_der_Name_der_Kopie_bleibt()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("UPDATE Tab_Stromspeicher SET Modulkosten = 450, Leistungskosten = 120, Investition_Fix = 3000, " +
                "Verschleisskosten = 0.04, Wirkungsgrad_RT = 0.91, Zyklen_Zugesichert = 7000, Standby_Verbrauch = 25, " +
                "Selbstentladung_Prozent_Monat = 2 WHERE ID = ?", k.Id);

            Rueckwegergebnis e = Neu(k.Id, "Rueckweg Kostenposten");
            Assert.True(e.Ok, e.Meldung);
            int satz = e.Saetze[0].IdKatalog;
            double Z(string spalte) => Convert.ToDouble(Wert("SELECT " + spalte + " FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz),
                                                        CultureInfo.InvariantCulture);
            Assert.Equal(450, Z("Modulkosten"));
            Assert.Equal(120, Z("Leistungskosten"));
            Assert.Equal(3000, Z("Investition_Fix"));
            Assert.Equal(0.04, Z("Verschleisskosten"), 9);
            Assert.Equal(0.91, Z("Wirkungsgrad_RT"), 9);
            Assert.Equal(7000, Z("Zyklen_Zugesichert"));
            Assert.Equal(25, Z("Standby_Verbrauch"));
            Assert.Equal(2, Z("Selbstentladung_Prozent_Monat"));
            // KA-E-15: Der Name der Projektkopie bleibt, auch wenn der Katalogsatz anders heisst.
            Assert.Equal(k.Name, Wert("SELECT Bezeichner FROM Tab_Stromspeicher WHERE ID = ?", k.Id));
        }

        [Fact]
        public void Die_Anlagenzeile_bleibt_im_Projekt()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            object standbyVorher = Wert("SELECT Standby_Verbrauch FROM Tab_Stromspeicher WHERE ID = ?", k.Id);
            object selbstVorher = Wert("SELECT Selbstentladung_Prozent_Monat FROM Tab_Stromspeicher WHERE ID = ?", k.Id);
            int variantenVorher = Anzahl("SELECT COUNT(*) FROM Tab_StromspeicherVariante");
            object traegerVorher = Wert("SELECT ID_Carrier FROM Tab_Energieanlagen WHERE ID = ?", k.Anlage);
            int satz = Neu(k.Id, "Rueckweg Anlage").Saetze[0].IdKatalog;
            Assert.True(satz > 0);
            // Die gesaeten Stromfelder der Projektkopie (Einfrierregel) und die Betriebsfuehrung der Anlage bleiben.
            Assert.Equal(standbyVorher, Wert("SELECT Standby_Verbrauch FROM Tab_Stromspeicher WHERE ID = ?", k.Id));
            Assert.Equal(selbstVorher, Wert("SELECT Selbstentladung_Prozent_Monat FROM Tab_Stromspeicher WHERE ID = ?", k.Id));
            Assert.Equal(variantenVorher, Anzahl("SELECT COUNT(*) FROM Tab_StromspeicherVariante"));
            Assert.Equal(traegerVorher, Wert("SELECT ID_Carrier FROM Tab_Energieanlagen WHERE ID = ?", k.Anlage));
            Assert.Equal(k.Id, Anzahl("SELECT ID_SP FROM Tab_Energieanlagen WHERE ID = ?", k.Anlage));
        }

        [Fact]
        public void Delete_ueber_den_Namen_loescht_samt_Satzvorlage()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Positionen(k, 1, 1);
            int satz = Neu(k.Id, "Rueckweg Namensweg").Saetze[0].IdKatalog;
            int betrieb = Vorlage(satz);
            Assert.True(new StromspeicherStammCtrl().Delete("Rueckweg Namensweg"));
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz));
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlage WHERE ID = ?", betrieb));
        }

        // ------------------------------------------------ KA-E-16: Löschen mit Satzvorlage ---

        [Fact]
        public void Loeschen_entfernt_beide_Vorlagen_ohne_weiteren_Verweis()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Positionen(k, 1, 1);
            int satz = Neu(k.Id, "Rueckweg Weg").Saetze[0].IdKatalog;
            int betrieb = Vorlage(satz);
            int invest = Invest(satz);

            StromspeicherStammCtrl.KatalogsatzLoeschung l = StromspeicherStammCtrl.KatalogsatzLoeschen(satz);
            Assert.True(l.Ok);
            Assert.Equal(Satzvorlagenabbau.Geloescht, l.Vorlage);
            Assert.Equal("", l.Meldung);
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz));
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlage WHERE ID IN (?, ?)", betrieb, invest));
            Assert.Equal(0, Positionenzahl(betrieb) + Positionenzahl(invest));
        }

        [Fact]
        public void Loeschen_behaelt_die_Vorlage_bei_Verweis_eines_anderen_Satzes_und_bei_Projektzeilen()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Positionen(k, 1, 1);
            int satz = Neu(k.Id, "Rueckweg Bleibt").Saetze[0].IdKatalog;
            int betrieb = Vorlage(satz);
            int invest = Invest(satz);

            // Ein anderer Satz (anderer Katalog) verweist auf die Investitionsvorlage.
            int fremd = Anzahl("SELECT MIN(ID) FROM Tab_Stromspeicher_STAMM");
            Sql("UPDATE Tab_Stromspeicher_STAMM SET ID_KostenVorlage = ? WHERE ID = ?", invest, fremd);
            int zweiter = Neu(k.Id, "Rueckweg Bleibt 2").Saetze[0].IdKatalog;
            Sql("UPDATE Tab_Stromspeicher_STAMM SET ID_KostenVorlage = ?, ID_KostenVorlageInvestition = ? WHERE ID = ?",
                betrieb, invest, zweiter);
            Sql("UPDATE Tab_Stromspeicher_STAMM SET ID_KostenVorlage = ? WHERE ID = ?", betrieb, satz);
            StromspeicherStammCtrl.KatalogsatzLoeschung l = StromspeicherStammCtrl.KatalogsatzLoeschen(satz);
            Assert.True(l.Ok);
            Assert.Equal(Satzvorlagenabbau.BleibtAndererSatz, l.Vorlage);
            Assert.Equal(2, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlage WHERE ID IN (?, ?)", betrieb, invest));

            // Projektzeilen tragen die Herkunft der Investitionsvorlage: der zweite Satz geht, sie bleibt, die Meldung nennt es;
            // die Betriebsvorlage ohne weiteren Verweis geht mit.
            Sql("UPDATE Tab_Stromspeicher_STAMM SET ID_KostenVorlage = NULL WHERE ID = ?", fremd);
            Sql("UPDATE Tab_ProjektWerte SET VorlageID = ? WHERE ID = (SELECT MIN(ID) FROM Tab_ProjektWerte WHERE ID_Anlage = ?)",
                invest, k.Anlage);
            l = StromspeicherStammCtrl.KatalogsatzLoeschen(zweiter);
            Assert.True(l.Ok);
            Assert.Equal(Satzvorlagenabbau.BleibtProjektzeilen, l.Vorlage);
            Assert.Contains("Rueckweg Bleibt 2", l.Meldung);
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher_STAMM WHERE ID = ?", zweiter));
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlage WHERE ID = ?", invest));
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlage WHERE ID = ?", betrieb));
        }

        [Fact]
        public void Loeschen_laesst_Standardvorlage_und_gesperrten_Satz_stehen()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            int satz = Neu(k.Id, "Rueckweg Standard").Saetze[0].IdKatalog;
            int standard = Anzahl("SELECT ID FROM Tab_KostenVorlage WHERE KomponentenID = 5 AND KategorieID = ? AND IstStandard = 1", BETRIEB);
            Sql("UPDATE Tab_Stromspeicher_STAMM SET ID_KostenVorlage = ? WHERE ID = ?", standard, satz);
            Sql("UPDATE Tab_Stromspeicher_STAMM SET ReadOnly = 1 WHERE ID = ?", satz);
            Assert.False(StromspeicherStammCtrl.KatalogsatzLoeschen(satz).Ok);
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz));

            Sql("UPDATE Tab_Stromspeicher_STAMM SET ReadOnly = 0 WHERE ID = ?", satz);
            StromspeicherStammCtrl.KatalogsatzLoeschung l = StromspeicherStammCtrl.KatalogsatzLoeschen(satz);
            Assert.True(l.Ok);
            Assert.Equal(Satzvorlagenabbau.Standardvorlage, l.Vorlage);
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlage WHERE ID = ?", standard));
        }

        [Fact]
        public void Scheitert_das_Mitloeschen_der_Vorlage_bleibt_auch_der_Satz()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Positionen(k, 1, 1);
            int satz = Neu(k.Id, "Rueckweg Sperre").Saetze[0].IdKatalog;
            int betrieb = Vorlage(satz);
            Sql("CREATE TRIGGER trg_rw_sperre BEFORE DELETE ON Tab_KostenVorlage BEGIN SELECT RAISE(ABORT, 'gesperrt'); END");
            try
            {
                Assert.False(StromspeicherStammCtrl.KatalogsatzLoeschen(satz).Ok);
                Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_Stromspeicher_STAMM WHERE ID = ?", satz));
                Assert.Equal(1, Positionenzahl(betrieb));
            }
            finally { Sql("DROP TRIGGER trg_rw_sperre"); }
        }
    }
}
