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
    /// <b>Rückweg der Wärmepumpe „In die Datenbank übernehmen…"</b> (Konzept Projektdialoge mit Katalogauswahl 5.2,
    /// KA‑E‑9; Kernweg <see cref="Katalogrueckweg"/>, Gewerk <see cref="WPStammCtrl.Rueckweg"/>): neu, überschreiben, die
    /// Sperrgründe, Namenszusatz, die Gerätespalten, <b>Heiz- und Kühlkennlinie als technische Kindzeilen mit allen
    /// Stützstellen</b> (Konzept 7 Nr. 5), Kosten als Satzvorlagen samt Investitionspositionen (KA‑E‑14) und Vorrang, Name
    /// der Kopie bleibt (KA‑E‑15), Löschen samt Kennlinien und Satzvorlage (KA‑E‑16). Nach dem Muster der
    /// <c>StromspeicherRueckwegTests</c>; geschrieben wird in der Kopie der Testdatenbank an einer Wärmepumpe eines
    /// Projekts AUSSERHALB der Referenzprojekte — ein Test hält fest, dass die Geräte von 1047, 1056, 1058 und 1060
    /// unverändert bleiben (Einfrierregeln).
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermepumpeRueckwegTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        private const string KOPIE = WPStammCtrl.TABELLE_PROJEKT;
        private const string KATALOG = WPStammCtrl.TABLE;

        /// <summary>Die Projekte der Referenzbasis — ihre Wärmepumpen fasst kein Test an.</summary>
        private const string REFERENZ = "1007,1017,1018,1023,1029,1030,1040,1045,1046,1047,1049,1050,1051,1052,1054," +
                                        "1055,1056,1057,1058,1059,1060,1061,1062,1063";

        private static readonly int[] REFERENZGERAETE_PROJEKTE = { 1047, 1056, 1058, 1060 };

        /// <summary>Die erste Projektkopie mit Anlage und Heizkennlinie außerhalb der Referenzprojekte.</summary>
        private static (int Id, int Projekt, string Name, int Anlage) Kopie()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT h.ID, h.ID_Projekt, h.Bezeichner, (SELECT MIN(e.ID) FROM Tab_Energieanlagen e WHERE e.ID_WP = h.ID " +
                "AND e.ID_Projekt = h.ID_Projekt) AS Anlage FROM Tab_WP h WHERE Anlage IS NOT NULL AND h.ID_Projekt NOT IN (" +
                REFERENZ + ") AND EXISTS (SELECT 1 FROM Tab_Kenndaten k WHERE k.ID_WP = h.ID) ORDER BY h.ID LIMIT 1");
            DataRow r = dt.Rows[0];
            return (I(r[0]), I(r[1]), Convert.ToString(r[2], CultureInfo.InvariantCulture), I(r[3]));
        }

        private static bool Leer(object o) => o == null || o == DBNull.Value;
        private static int I(object o) => Convert.ToInt32(o, CultureInfo.InvariantCulture);
        private static double D(object o) => Convert.ToDouble(o, CultureInfo.InvariantCulture);
        private static object Wert(string sql, params object[] p)
            => DataRepository.ExecuteScalar(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray());
        private static void Sql(string sql, params object[] p)
            => DataRepository.ExecuteSQL(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray());
        private static int Anzahl(string sql, params object[] p) => I(Wert(sql, p));

        private static Rueckwegergebnis Neu(int id, string name)
            => WPStammCtrl.AusProjektUebernehmen(new[] { new Rueckwegauftrag(id, Rueckwegart.Neu, name) });
        private static Rueckwegergebnis Ueberschreiben(int id)
            => WPStammCtrl.AusProjektUebernehmen(new[] { new Rueckwegauftrag(id, Rueckwegart.Ueberschreiben, "") });

        /// <summary>Die Stützstellen einer Kennlinie als sortierte Liste „Vorlauf|Temperatur|COP|Leistung".</summary>
        private static List<string> Kennlinie(string tabelle, string leistung, int idWp)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT Vorlauf, Temperatur, COP, [" + leistung + "] FROM [" + tabelle +
                                                       "] WHERE ID_WP = ? ORDER BY Vorlauf, Temperatur", new DbParam("@id", idWp));
            return dt.Rows.Cast<DataRow>()
                .Select(r => string.Join("|", r.ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture))))
                .ToList();
        }

        /// <summary>Legt der Kopie eine Kühlkennlinie mit drei Stützstellen an (die Projekte außerhalb der Basis haben keine).</summary>
        private static void Kuehlkennlinie(int idKopie, double cop)
        {
            Sql("DELETE FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = ?", idKopie);
            foreach ((int vl, int t) in new[] { (7, 25), (7, 35), (18, 35) })
                Sql("INSERT INTO Tab_Kenndaten_Kuehlung (ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) VALUES (?, ?, ?, ?, ?, 100)",
                    idKopie, vl, t, cop, 10.5);
        }

        private static void Positionen((int Id, int Projekt, string Name, int Anlage) k, int betrieb, int invest)
        {
            Sql("DELETE FROM Tab_ProjektWerte WHERE ID_Anlage = ?", k.Anlage);
            DataTable stamm = DataRepository.GetDataTable("SELECT StammID FROM Tab_Kostenfaktor ORDER BY StammID LIMIT 6");
            int n = 0;
            for (int i = 0; i < betrieb + invest; i++)
            {
                bool inv = i >= betrieb;
                Sql("INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, Bemessung, " +
                    "Kostenart, ID_Anlage, Nutzungsdauer) VALUES (?, ?, 1, ?, ?, 'BETRAG', ?, ?, 20)",
                    k.Projekt, I(stamm.Rows[n++][0]), inv ? DbWerte.KOSTEN_KATEGORIE_INVESTITION : DbWerte.KOSTEN_KATEGORIE_BETRIEB,
                    100.0 * (i + 1),
                    inv ? DbWerte.KOSTENART_KAPITALGEBUNDEN : DbWerte.KOSTENART_BETRIEBSGEBUNDEN, k.Anlage);
            }
        }

        private static int Vorlage(int satz) => Anzahl("SELECT ID_KostenVorlage FROM Tab_WP_STAMM WHERE ID = ?", satz);
        private static int Invest(int satz)
        {
            object o = Wert("SELECT ID_KostenVorlageInvestition FROM Tab_WP_STAMM WHERE ID = ?", satz);
            return Leer(o) ? 0 : I(o);
        }

        /// <summary>Ein Fingerabdruck der Geräte eines Projekts: Gerätezeilen, Heiz- und Kühlkennlinie, Anlagenzeilen.</summary>
        private static string Fingerabdruck(int projekt)
        {
            var teile = new List<string>();
            foreach (string sql in new[]
            {
                "SELECT * FROM Tab_WP WHERE ID_Projekt = ? ORDER BY ID",
                "SELECT k.* FROM Tab_Kenndaten k JOIN Tab_WP w ON w.ID = k.ID_WP WHERE w.ID_Projekt = ? ORDER BY k.ID",
                "SELECT k.* FROM Tab_Kenndaten_Kuehlung k JOIN Tab_WP w ON w.ID = k.ID_WP WHERE w.ID_Projekt = ? ORDER BY k.ID",
                "SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_WP > 0 ORDER BY ID",
            })
            {
                DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@p", projekt));
                foreach (DataRow r in dt.Rows)
                    teile.Add(string.Join("|", r.ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture))));
            }
            return string.Join("\n", teile);
        }

        // ------------------------------------------------------------------ neu ---

        [Fact]
        public void Neu_legt_einen_ungesperrten_Satz_an_samt_Geraetespalten_und_die_Kopie_bekommt_ihn_als_Ursprung()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("UPDATE Tab_WP SET Nennleistung = 17, Modulkosten = 12345, Kaeltemittel = 'R290', Spreizung_Auslegung_K = 6.5, " +
                "Kuehl_Vorlauf = 16 WHERE ID = ?", k.Id);

            Rueckwegergebnis e = Neu(k.Id, "Rueckweg WP Neu");

            Assert.True(e.Ok, e.Meldung);
            Rueckwegsatz s = Assert.Single(e.Saetze);
            Assert.True(s.Neu);
            Assert.Equal(0, Anzahl("SELECT ReadOnly FROM Tab_WP_STAMM WHERE ID = ?", s.IdKatalog));
            Assert.Equal(17, Anzahl("SELECT Nennleistung FROM Tab_WP_STAMM WHERE ID = ?", s.IdKatalog));
            Assert.Equal(12345, Anzahl("SELECT Modulkosten FROM Tab_WP_STAMM WHERE ID = ?", s.IdKatalog));
            Assert.Equal("R290", Wert("SELECT Kaeltemittel FROM Tab_WP_STAMM WHERE ID = ?", s.IdKatalog));
            Assert.Equal(6.5, D(Wert("SELECT Spreizung_Auslegung_K FROM Tab_WP_STAMM WHERE ID = ?", s.IdKatalog)));
            Assert.Equal(16, Anzahl("SELECT Kuehl_Vorlauf FROM Tab_WP_STAMM WHERE ID = ?", s.IdKatalog));
            Assert.True(Leer(Wert("SELECT Katalog_Schluessel FROM Tab_WP_STAMM WHERE ID = ?", s.IdKatalog)));
            Assert.Equal(s.IdKatalog, Anzahl("SELECT ID_Stamm FROM Tab_WP WHERE ID = ?", k.Id));
            Assert.Contains("Rueckweg WP Neu", e.Meldung);
        }

        [Fact]
        public void Heiz_und_Kuehlkennlinie_gehen_mit_allen_Stuetzstellen_mit()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Kuehlkennlinie(k.Id, 4.2);
            List<string> heiz = Kennlinie("Tab_Kenndaten", "Ptherm", k.Id);
            List<string> kuehl = Kennlinie("Tab_Kenndaten_Kuehlung", "Pkuehl", k.Id);
            Assert.NotEmpty(heiz);

            Rueckwegergebnis e = Neu(k.Id, "Rueckweg WP Kennlinien");

            Assert.True(e.Ok, e.Meldung);
            int satz = e.Saetze[0].IdKatalog;
            Assert.Equal(heiz.Count + kuehl.Count, e.Saetze[0].Kindzeilen);
            Assert.Equal(heiz, Kennlinie("Tab_Kenndaten_STAMM", "Ptherm", satz));
            Assert.Equal(kuehl, Kennlinie("Tab_Kenndaten_Kuehlung_STAMM", "Pkuehl", satz));
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM WHERE ID_WP = ? AND ReadOnly <> 0", satz));
            // Die Kennlinie der Kopie bleibt stehen.
            Assert.Equal(heiz, Kennlinie("Tab_Kenndaten", "Ptherm", k.Id));
        }

        // ---------------------------------------------------------- überschreiben ---

        [Fact]
        public void Ueberschreiben_ersetzt_Geraetewerte_und_Kennlinien_des_Ursprungs_und_behaelt_seinen_Namen()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Kuehlkennlinie(k.Id, 4.2);
            int neu = Neu(k.Id, "Rueckweg WP Ursprung").Saetze[0].IdKatalog;
            int vorher = Anzahl("SELECT COUNT(*) FROM Tab_WP_STAMM");

            Sql("UPDATE Tab_WP SET Nennleistung = 23, Bezeichner = 'anders benannt' WHERE ID = ?", k.Id);
            Sql("DELETE FROM Tab_Kenndaten WHERE ID_WP = ? AND ID <> (SELECT MIN(ID) FROM Tab_Kenndaten WHERE ID_WP = ?)", k.Id, k.Id);
            Sql("UPDATE Tab_Kenndaten SET COP = 9.75 WHERE ID_WP = ?", k.Id);
            Kuehlkennlinie(k.Id, 3.3);

            Rueckwegzeile z = Assert.Single(WPStammCtrl.RueckwegVorschau(new[] { k.Id }));
            Assert.Equal(Rueckwegabsage.Keine, z.Ueberschreiben);
            Assert.Equal("Rueckweg WP Ursprung", z.NameUrsprung);

            Rueckwegergebnis e = Ueberschreiben(k.Id);

            Assert.True(e.Ok, e.Meldung);
            Assert.False(e.Saetze[0].Neu);
            Assert.Equal(neu, e.Saetze[0].IdKatalog);
            Assert.Equal(vorher, Anzahl("SELECT COUNT(*) FROM Tab_WP_STAMM"));
            Assert.Equal(23, Anzahl("SELECT Nennleistung FROM Tab_WP_STAMM WHERE ID = ?", neu));
            Assert.Equal("Rueckweg WP Ursprung", Wert("SELECT Bezeichner FROM Tab_WP_STAMM WHERE ID = ?", neu));
            Assert.Equal(Kennlinie("Tab_Kenndaten", "Ptherm", k.Id), Kennlinie("Tab_Kenndaten_STAMM", "Ptherm", neu));
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM WHERE ID_WP = ?", neu));
            Assert.Equal(9.75, D(Wert("SELECT COP FROM Tab_Kenndaten_STAMM WHERE ID_WP = ?", neu)));
            Assert.Equal(Kennlinie("Tab_Kenndaten_Kuehlung", "Pkuehl", k.Id), Kennlinie("Tab_Kenndaten_Kuehlung_STAMM", "Pkuehl", neu));
        }

        [Fact]
        public void Ein_gesperrter_Ursprung_wird_nicht_ueberschrieben_und_seine_Kennlinie_bleibt()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            int neu = Neu(k.Id, "Rueckweg WP Gesperrt").Saetze[0].IdKatalog;
            List<string> heiz = Kennlinie("Tab_Kenndaten_STAMM", "Ptherm", neu);
            Sql("UPDATE Tab_WP_STAMM SET ReadOnly = 1 WHERE ID = ?", neu);
            Sql("UPDATE Tab_Kenndaten SET COP = 1.11 WHERE ID_WP = ?", k.Id);

            Assert.Equal(Rueckwegabsage.UrsprungGesperrt, WPStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Ueberschreiben);
            Rueckwegergebnis e = Ueberschreiben(k.Id);

            Assert.False(e.Ok);
            Assert.Equal(Rueckwegabsage.UrsprungGesperrt, e.Absage);
            Assert.Equal(heiz, Kennlinie("Tab_Kenndaten_STAMM", "Ptherm", neu));
        }

        [Fact]
        public void Eine_Kopie_ohne_Verweis_kennt_ihren_Ursprung_nicht()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("UPDATE Tab_WP SET ID_Stamm = NULL WHERE ID = ?", k.Id);

            Assert.Equal(Rueckwegabsage.UrsprungUnbekannt, WPStammCtrl.RueckwegVorschau(new[] { k.Id })[0].Ueberschreiben);
            Assert.Equal(Rueckwegabsage.UrsprungUnbekannt, Ueberschreiben(k.Id).Absage);
        }

        // ------------------------------------------------------------ Namen, Prüfung ---

        [Fact]
        public void Ein_belegter_Name_bekommt_den_Zusatz_Projekt_und_wird_nie_gespeichert()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            string belegt = Convert.ToString(Wert("SELECT Bezeichner FROM Tab_WP_STAMM ORDER BY ID LIMIT 1"), CultureInfo.InvariantCulture);
            Sql("UPDATE Tab_WP SET Bezeichner = ? WHERE ID = ?", belegt, k.Id);

            Rueckwegzeile z = Assert.Single(WPStammCtrl.RueckwegVorschau(new[] { k.Id }));
            Assert.StartsWith(belegt + " (", z.Namensvorschlag);
            Assert.True(WPStammCtrl.RueckwegNameBelegt(belegt));
            int vorher = Anzahl("SELECT COUNT(*) FROM Tab_WP_STAMM");

            Rueckwegergebnis e = Neu(k.Id, belegt);

            Assert.False(e.Ok);
            Assert.Equal(Rueckwegabsage.NameBelegt, e.Absage);
            Assert.Equal(vorher, Anzahl("SELECT COUNT(*) FROM Tab_WP_STAMM"));
        }

        [Fact]
        public void Ein_Pruefverstoss_der_Kopie_wird_benannt_abgelehnt_ohne_Kennlinien_zu_schreiben()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Sql("UPDATE Tab_WP SET Modulkosten = -1 WHERE ID = ?", k.Id);
            int vorher = Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM");

            Rueckwegergebnis e = Neu(k.Id, "Rueckweg WP Verstoss");

            Assert.False(e.Ok);
            Assert.Equal(Rueckwegabsage.Pruefverstoss, e.Absage);
            Assert.Equal(vorher, Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM"));
        }

        [Fact]
        public void Der_Name_der_Projektkopie_bleibt_und_die_Anlagenzeile_bleibt_im_Projekt()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            DataTable vorher = DataRepository.GetDataTable("SELECT * FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@a", k.Anlage));

            Rueckwegergebnis e = Neu(k.Id, "Rueckweg WP anderer Name");

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(k.Name, Wert("SELECT Bezeichner FROM Tab_WP WHERE ID = ?", k.Id));
            DataTable nachher = DataRepository.GetDataTable("SELECT * FROM Tab_Energieanlagen WHERE ID = ?", new DbParam("@a", k.Anlage));
            Assert.Equal(vorher.Rows[0].ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture)),
                         nachher.Rows[0].ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture)));
        }

        [Fact]
        public void Was_mitgeht_ist_die_Schnittmenge_ohne_Verwaltungsspalten_und_die_zwei_Kennlinien_sind_Kindtabellen()
        {
            if (!_db.Vorhanden) return;
            Rueckweggewerk g = WPStammCtrl.Rueckweg();
            IReadOnlyList<string> sp = Katalogrueckweg.Spalten(g);
            foreach (string nie in new[] { "ID", "ID_Projekt", "ID_Stamm", "ReadOnly", "Bezeichner", "Katalog_Schluessel", "ID_KostenVorlage" })
                Assert.DoesNotContain(nie, sp);
            foreach (string mit in new[] { "Nennleistung", "Modulkosten", "Kuehlbetrieb", "Kuehl_Vorlauf", "Kaeltemittel", "Ruecklauf_Max" })
                Assert.Contains(mit, sp);
            Assert.Equal(new[] { ("Tab_Kenndaten", "Tab_Kenndaten_STAMM", "ID_WP"), ("Tab_Kenndaten_Kuehlung", "Tab_Kenndaten_Kuehlung_STAMM", "ID_WP") },
                         g.Kinder.ToArray());
            Assert.Equal(1, g.KomponentenId);
        }

        // ------------------------------------------------------------------ Kosten ---

        [Fact]
        public void Betriebs_und_Investitionspositionen_werden_Satzvorlagen_und_gehen_der_Standardvorlage_vor()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Positionen(k, 1, 1);

            Rueckwegergebnis e = Neu(k.Id, "Rueckweg WP Kosten");

            Assert.True(e.Ok, e.Meldung);
            int satz = e.Saetze[0].IdKatalog;
            Assert.Equal(2, e.Saetze[0].Kostenpositionen);
            int betrieb = Vorlage(satz);
            int invest = Invest(satz);
            Assert.True(betrieb > 0 && invest > 0);
            Assert.Equal("Rueckweg WP Kosten", Wert("SELECT Name FROM Tab_KostenVorlage WHERE ID = ?", betrieb));
            Assert.Equal(WPStammCtrl.KOMPONENTE_KOSTEN, Anzahl("SELECT KomponentenID FROM Tab_KostenVorlage WHERE ID = ?", invest));

            KostenVorlageKopf kopf = Katalogrueckweg.SatzvorlageDerAnlage(k.Anlage, WizardItemClass.WP_TYP, WPStammCtrl.KOMPONENTE_KOSTEN);
            Assert.NotNull(kopf);
            Assert.Equal(betrieb, kopf.Id);
        }

        // ------------------------------------------------------- KA-E-16: Löschen ---

        [Fact]
        public void Loeschen_raeumt_Kennlinien_und_beide_Satzvorlagen_ab()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Kuehlkennlinie(k.Id, 4.2);
            Positionen(k, 1, 1);
            int satz = Neu(k.Id, "Rueckweg WP Weg").Saetze[0].IdKatalog;
            int betrieb = Vorlage(satz);
            int invest = Invest(satz);
            Assert.True(Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM WHERE ID_WP = ?", satz) > 0);

            WPStammCtrl.KatalogsatzLoeschung l = WPStammCtrl.KatalogsatzLoeschen(satz);

            Assert.True(l.Ok);
            Assert.Equal(Satzvorlagenabbau.Geloescht, l.Vorlage);
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_WP_STAMM WHERE ID = ?", satz));
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM WHERE ID_WP = ?", satz));
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kuehlung_STAMM WHERE ID_WP = ?", satz));
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlage WHERE ID IN (?, ?)", betrieb, invest));
            // Die Projektkopie und ihre Kennlinien bleiben; ihr Ursprung ist leer.
            Assert.True(Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten WHERE ID_WP = ?", k.Id) > 0);
            Assert.True(Leer(Wert("SELECT ID_Stamm FROM Tab_WP WHERE ID = ?", k.Id)));
        }

        [Fact]
        public void Delete_ueber_den_Namen_laeuft_ueber_denselben_Loeschweg()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            Positionen(k, 1, 0);
            int satz = Neu(k.Id, "Rueckweg WP Namensweg").Saetze[0].IdKatalog;
            int betrieb = Vorlage(satz);

            Assert.True(new WPStammCtrl { WPName = "Rueckweg WP Namensweg" }.Delete());

            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_WP_STAMM WHERE ID = ?", satz));
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM WHERE ID_WP = ?", satz));
            Assert.Equal(0, Anzahl("SELECT COUNT(*) FROM Tab_KostenVorlage WHERE ID = ?", betrieb));
        }

        [Fact]
        public void Ein_gesperrter_Satz_wird_nicht_geloescht()
        {
            if (!_db.Vorhanden) return;
            int gesperrt = Anzahl("SELECT ID FROM Tab_WP_STAMM WHERE ReadOnly = 1 ORDER BY ID LIMIT 1");
            int kennlinien = Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM WHERE ID_WP = ?", gesperrt);

            Assert.False(WPStammCtrl.KatalogsatzLoeschen(gesperrt).Ok);
            Assert.Equal(1, Anzahl("SELECT COUNT(*) FROM Tab_WP_STAMM WHERE ID = ?", gesperrt));
            Assert.Equal(kennlinien, Anzahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM WHERE ID_WP = ?", gesperrt));
        }

        // ------------------------------------------------- Ursprung beim Kopieren ---

        [Fact]
        public void Die_Uebernahme_aus_dem_Katalog_traegt_den_Ursprung_ein_und_kopiert_die_Kennlinien()
        {
            if (!_db.Vorhanden) return;
            var k = Kopie();
            int satz = Neu(k.Id, "Rueckweg WP Rundweg").Saetze[0].IdKatalog;
            int projekt = Anzahl("SELECT MIN(ID) FROM Tab_Projekt WHERE ID NOT IN (" + REFERENZ + ") AND ID <> ?", k.Projekt);

            int kopie = new WPCtrl().CopyFromStamm(satz, projekt);

            Assert.True(kopie > 0);
            Assert.Equal(satz, Anzahl("SELECT ID_Stamm FROM Tab_WP WHERE ID = ?", kopie));
            Assert.Equal(Kennlinie("Tab_Kenndaten_STAMM", "Ptherm", satz), Kennlinie("Tab_Kenndaten", "Ptherm", kopie));
        }

        // ------------------------------------------------------ Einfrierregeln ---

        [Fact]
        public void Die_Waermepumpen_der_Referenzprojekte_1047_1056_1058_1060_bleiben_unveraendert()
        {
            if (!_db.Vorhanden) return;
            var vorher = REFERENZGERAETE_PROJEKTE.ToDictionary(p => p, Fingerabdruck);
            int stammVorher = Anzahl("SELECT COUNT(*) FROM Tab_WP_STAMM");
            var k = Kopie();
            Kuehlkennlinie(k.Id, 4.2);
            Positionen(k, 1, 1);

            int satz = Neu(k.Id, "Rueckweg WP Referenz").Saetze[0].IdKatalog;
            Assert.True(Ueberschreiben(k.Id).Ok);
            Assert.True(WPStammCtrl.SammelfelderSchreibenAlle(false, new[]
            {
                new WPStammCtrl.Sammelaenderung(satz, new WPStammCtrl.Sammelfelder("Neuwerk", "", 1)),
            }).Ok);
            Assert.True(WPStammCtrl.KatalogsatzLoeschen(satz).Ok);

            foreach (int p in REFERENZGERAETE_PROJEKTE)
                Assert.True(vorher[p] == Fingerabdruck(p), "Gerät des Referenzprojekts " + p + " verändert");
            Assert.Equal(stammVorher, Anzahl("SELECT COUNT(*) FROM Tab_WP_STAMM"));
            Assert.All(REFERENZGERAETE_PROJEKTE, p => Assert.NotEmpty(vorher[p]));
        }
    }
}
