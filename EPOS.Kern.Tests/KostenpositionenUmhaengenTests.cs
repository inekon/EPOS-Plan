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
    /// Kostenpositionen (<c>Tab_ProjektWerte</c>), deren Anlage verschwindet — Anwenderentscheide
    /// 07.10.2026 zum Befund „Verwaiste Kostenpositionen“:
    /// <list type="number">
    /// <item>Die Komponentenübernahme hängt die Positionen der ersetzten Anlagen der Reihe nach
    /// auf die neuen Anlagen um (Anlage und Geräteanker), Beträge unverändert.</item>
    /// <item>Bei anderer Anzahl werden übrige Positionen lose (Anlage und Anker leer) und es
    /// kommt der Hinweis <c>BK_KOMP_HINW_KOSTEN_LOSE</c>.</item>
    /// <item>Kältemaschine löschen und Projektpuffer entfernen nehmen die Positionen ihrer
    /// Anlage mit; die Wirtschaftlichkeit heilt die Zuordnung vor dem Lesen und rechnet
    /// deshalb gleich, ob die Kostenseite vorher geöffnet war oder nicht.</item>
    /// </list>
    /// <para><b>Eigene Arbeitskopie je Fall</b> — jeder Fall schreibt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KostenpositionenUmhaengenTests
    {
        private const string GEWERK = "Wärmepumpe";

        // =============================================================================
        //  Komponentenübernahme
        // =============================================================================

        /// <summary>1:1 — Quelle 1040 (eine Wärmepumpe) auf Ziel 1024 (eine Wärmepumpe mit zwei Positionen).</summary>
        [Fact]
        public void Gleiche_Anzahl_haengt_die_Positionen_auf_die_neue_Anlage_um()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int quelle = 1040, ziel = 1024, altAnlage = 11262;
            Dictionary<long, double> betraege = Betraege(ziel, altAnlage);
            Assert.Equal(2, betraege.Count);

            string hinweise = Uebernehmen(quelle, ziel);

            List<int> neu = WpAnlagen(ziel);
            Assert.Single(neu);
            Assert.Equal(betraege, Betraege(ziel, neu[0]));
            Assert.All(Anker(ziel, neu[0]), a => Assert.Equal(WpGeraet(neu[0]), a));
            Assert.DoesNotContain(LoseText(2), hinweise);
            Assert.Equal(0, Lose(ziel));

            // Die Selbstheilung findet danach nichts mehr zu tun.
            KostenProjektPositionenCtrl.ZuordnungReparieren(ziel);
            Assert.Equal(betraege, Betraege(ziel, neu[0]));
        }

        /// <summary>Weniger — Quelle 1040 (eine) auf Ziel 1019 (zwei Wärmepumpen, 6 + 3 Positionen).</summary>
        [Fact]
        public void Weniger_neue_Anlagen_lassen_den_Rest_lose_und_melden_ihn()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int quelle = 1040, ziel = 1019, ersteAlt = 14922, zweiteAlt = 14923;
            Dictionary<long, double> erste = Betraege(ziel, ersteAlt);
            Dictionary<long, double> zweite = Betraege(ziel, zweiteAlt);
            Assert.Equal(6, erste.Count);
            Assert.Equal(3, zweite.Count);
            int loseVorher = Lose(ziel);

            string hinweise = Uebernehmen(quelle, ziel);

            List<int> neu = WpAnlagen(ziel);
            Assert.Single(neu);
            Assert.Equal(erste, Betraege(ziel, neu[0]));
            Assert.Contains(LoseText(3), hinweise);

            // Die übrigen drei: lose, OHNE Geräteanker - der zufällige Treffer einer
            // wiederverwendeten Geräte-ID (MAX(ID)+1) kann sie nicht mehr anhängen.
            foreach (long id in zweite.Keys)
            {
                DataTable z = DataRepository.GetDataTable(
                    "SELECT ID_Anlage, ID_AnlageGeraet, EingegebenerWert FROM Tab_ProjektWerte WHERE ID = ?",
                    new DbParam("@id", id));
                Assert.Equal(DBNull.Value, z.Rows[0]["ID_Anlage"]);
                Assert.Equal(DBNull.Value, z.Rows[0]["ID_AnlageGeraet"]);
                Assert.Equal(zweite[id], Convert.ToDouble(z.Rows[0]["EingegebenerWert"], CultureInfo.InvariantCulture));
            }
            Assert.Equal(loseVorher + 3, Lose(ziel));

            KostenProjektPositionenCtrl.ZuordnungReparieren(ziel);
            Assert.Equal(loseVorher + 3, Lose(ziel));
        }

        /// <summary>Mehr — Quelle 1043 (zwei) auf Ziel 1024 (eine): alles auf die erste neue Anlage.</summary>
        [Fact]
        public void Mehr_neue_Anlagen_haengen_der_Reihe_nach_um()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int quelle = 1043, ziel = 1024, altAnlage = 11262;
            Dictionary<long, double> betraege = Betraege(ziel, altAnlage);

            string hinweise = Uebernehmen(quelle, ziel);

            List<int> neu = WpAnlagen(ziel);
            Assert.Equal(2, neu.Count);
            Assert.Equal(betraege, Betraege(ziel, neu[0]));
            Assert.Empty(Betraege(ziel, neu[1]));
            Assert.Equal(0, Lose(ziel));
            Assert.DoesNotContain(LoseText(1), hinweise);
        }

        // =============================================================================
        //  Löschwege
        // =============================================================================

        [Fact]
        public void Kaeltemaschine_loeschen_nimmt_ihre_Positionen_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int projekt = 1055, anlage = 23776;
            Assert.NotNull(KaeltemaschineAnlageCtrl.Laden(anlage));
            PositionAnlegen(projekt, DbWerte.KOSTEN_KOMPONENTE_KAELTEMASCHINE, anlage, 4321);
            int andere = Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ? AND (ID_Anlage IS NULL OR ID_Anlage <> ?)",
                              projekt, anlage);

            KaeltemaschineAnlageCtrl.Loeschen(anlage);

            Assert.Null(KaeltemaschineAnlageCtrl.Laden(anlage));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage = ?", anlage));
            Assert.Equal(andere, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", projekt));
        }

        [Fact]
        public void Projektpuffer_entfernen_nimmt_die_Positionen_seiner_Anlage_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int projekt = 1040, anlage = 14746, puffer = 1054188, nachbar = 14743;
            Assert.Equal(2, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage = ?", anlage));
            int gesamt = Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", projekt);
            int beimNachbarn = Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage = ?", nachbar);

            Assert.True(PufferSpCtrl.ProjektPufferEntfernen(puffer, projekt));

            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID = ?", anlage));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage = ?", anlage));
            Assert.Equal(gesamt - 2, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", projekt));
            Assert.Equal(beimNachbarn, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage = ?", nachbar));
        }

        /// <summary>
        /// Die Ankerkarte der Selbstheilung kennt die Kältemaschine: Eine Position auf einer
        /// gelöschten Anlage findet über ihren Anker die Anlage desselben Geräts wieder.
        /// </summary>
        [Fact]
        public void Die_Selbstheilung_kennt_die_Kaeltemaschine()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int projekt = 1055, anlage = 23776;
            int geraet = Zahl("SELECT ID_Kaeltemaschine FROM Tab_Energieanlagen WHERE ID = ?", anlage);
            long id = PositionAnlegen(projekt, DbWerte.KOSTEN_KOMPONENTE_KAELTEMASCHINE, 999999, 100, geraet);

            KostenProjektPositionenCtrl.ZuordnungReparieren(projekt);

            Assert.Equal(anlage, Zahl("SELECT ID_Anlage FROM Tab_ProjektWerte WHERE ID = ?", id));
        }

        // =============================================================================
        //  Wirtschaftlichkeit
        // =============================================================================

        /// <summary>
        /// Eine Position mit Verweis auf eine gelöschte Anlage (Projekt 1030, BHKW-Zeile
        /// „€/kWh elektrisch" mit Konserve) rechnet in der Wirtschaftlichkeit gleich — ob die
        /// Kostenseite (<c>ZuordnungReparieren</c>) vorher lief oder nicht.
        /// </summary>
        [Fact]
        public void Die_Wirtschaftlichkeit_haengt_nicht_am_Oeffnen_der_Kostenseite()
        {
            const int projekt = 1030;
            WirtschaftlichkeitErgebnis ohne, mit;
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;
                Verwaisen();
                ohne = Rechne(projekt);
            }
            using (var db = new TestDatenbank())
            {
                Verwaisen();
                KostenProjektPositionenCtrl.ZuordnungReparieren(projekt);
                mit = Rechne(projekt);
            }

            Assert.Equal(mit.BetriebskostenJahr.Value, ohne.BetriebskostenJahr.Value, 6);
            Assert.Equal(mit.Kapitalwert.Value, ohne.Kapitalwert.Value, 6);

            static void Verwaisen()
            {
                Assert.True(DataRepository.ExecuteSQL(
                    "UPDATE Tab_ProjektWerte SET ID_Anlage = 999999, Einheitpreis = 0.05, Menge = 1 WHERE ID = ?",
                    new DbParam("@id", 101600591L)));
            }
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        private static string Uebernehmen(int quelle, int ziel)
        {
            bool ok = new KomponentenUebernahmeCtrl().Uebernehmen(quelle, ziel, GEWERK, out string fehler, out string hinweise);
            Assert.True(ok, fehler);
            return hinweise ?? "";
        }

        private static string LoseText(int n) =>
            string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.BK_KOMP_HINW_KOSTEN_LOSE, n);

        private static List<int> WpAnlagen(int projekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_WP IS NOT NULL AND ID_WP <> 0 ORDER BY ID",
                new DbParam("@p", projekt));
            return dt.Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture)).ToList();
        }

        private static int WpGeraet(int anlage) =>
            Zahl("SELECT ID_WP FROM Tab_Energieanlagen WHERE ID = ?", anlage);

        private static Dictionary<long, double> Betraege(int projekt, int anlage)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, EingegebenerWert FROM Tab_ProjektWerte WHERE ProjektID = ? AND ID_Anlage = ?",
                new DbParam("@p", projekt), new DbParam("@a", anlage));
            return dt.Rows.Cast<DataRow>().ToDictionary(
                r => Convert.ToInt64(r[0], CultureInfo.InvariantCulture),
                r => Convert.ToDouble(r[1], CultureInfo.InvariantCulture));
        }

        private static List<int> Anker(int projekt, int anlage)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID_AnlageGeraet FROM Tab_ProjektWerte WHERE ProjektID = ? AND ID_Anlage = ?",
                new DbParam("@p", projekt), new DbParam("@a", anlage));
            return dt.Rows.Cast<DataRow>().Select(r => r[0] == DBNull.Value ? 0 : Convert.ToInt32(r[0], CultureInfo.InvariantCulture)).ToList();
        }

        private static int Lose(int projekt) =>
            Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ? AND ID_Anlage IS NULL", projekt);

        private static int Zahl(string sql, params object[] werte)
        {
            DbParam[] p = werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray();
            object o = DataRepository.ExecuteScalar(sql, p);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static long PositionAnlegen(int projekt, string komponente, int anlage, double betrag, int geraet = 0)
        {
            object k = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_KostenKomponente WHERE Komponente = ?", new DbParam("@k", komponente));
            Assert.NotNull(k);
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_ProjektWerte (ProjektID, KomponentenID, KategorieID, EingegebenerWert, Kostenart, " +
                "Bemessung, ID_Anlage, ID_AnlageGeraet) VALUES (?, ?, 1, ?, 'KAPITALGEBUNDEN', 'BETRAG', ?, ?)",
                new DbParam("@p", projekt), new DbParam("@k", k), new DbParam("@w", betrag),
                new DbParam("@a", anlage), new DbParam("@g", geraet > 0 ? (object)geraet : DBNull.Value)));
            return Convert.ToInt64(DataRepository.ExecuteScalar("SELECT MAX(ID) FROM Tab_ProjektWerte"), CultureInfo.InvariantCulture);
        }

        private static WirtschaftlichkeitErgebnis Rechne(int projekt)
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(projekt);
            var v = new VariantenDaten
            {
                IdProjekt = projekt, IstStamm = true, Projektname = "Probe " + projekt,
                Ergebnis = new ErgebnisCtrl().Load(projekt)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = projekt, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return ctrl.Berechne(daten, p).First(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == projekt);
        }
    }
}
