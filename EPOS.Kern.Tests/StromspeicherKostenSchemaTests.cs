using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using SpeicherEngine;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schemaschritt „Die Kostenspalten des Stromspeichers entfallen"</b> (<see cref="StromspeicherKostenSchema"/>,
    /// KB-5): Nummer und Register, Entfall der acht Spalten, Saat und Sicherheitslauf, der leere zweite Lauf, der
    /// Satzleser mit seiner Rangfolge und Datenlücke, die eine Buchung des Verschleißes und die Flotte.
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromspeicherKostenSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        // Die Vaillant-Projektkopie des Projekts 1007 (gesät aus Modulkosten 3 999,5 €/kWh) und ihre Anlage.
        private const int KOPIE_1007 = 1007001;
        private const int ANLAGE_1007 = 10357;
        private const double SATZ_VAILLANT = 3999.5;

        [Fact]
        public void Die_Nummer_ist_229_und_haengt_an_228()
        {
            Assert.Equal(229, StromspeicherKostenSchema.SCHRITT);
            Assert.Equal(GeraetekostenEntfallSchema.SCHRITT + 1, StromspeicherKostenSchema.SCHRITT);
            // Das Ziel steht seit KBP-2 eine Nummer weiter (KaeltebedarfsprofilVorlagenSchema); dieser Schritt bleibt in der Kette.
            Assert.True(SchemaStand.Zielversion >= StromspeicherKostenSchema.SCHRITT);
            Assert.Contains(Paketanhebung.Stufen,
                            s => s.Nr == StromspeicherKostenSchema.SCHRITT && s.Wirkung == Paketanhebung.Art.Ddl);
        }

        [Fact]
        public void Die_acht_Kostenspalten_sind_fort_und_stehen_in_keiner_Liste_mehr()
        {
            Assert.Equal(8, StromspeicherKostenSchema.Entfall.Count());
            if (!_db.Vorhanden) return;
            Assert.True(StromspeicherKostenSchema.Vollstaendig());
            Assert.Empty(StromspeicherKostenSchema.OhneVerweis());
            foreach ((string tabelle, string spalte) in StromspeicherKostenSchema.Entfall)
            {
                Assert.False(DataRepository.SpalteVorhanden(tabelle, spalte), tabelle + "." + spalte + " steht noch.");
                Katalogtabelle t = Katalogfassung.Tabelle(tabelle);
                if (t != null) Assert.DoesNotContain(spalte, t.Fachspalten);
                // Die Rückfallebene darf sie nicht wieder anlegen.
                Assert.DoesNotContain(SchemaKatalog.Schritt11_Stromspeicher,
                                      s => s.Tabelle == tabelle && s.Name == spalte);
            }
        }

        [Fact]
        public void Ein_zweiter_Lauf_aendert_nichts()
        {
            if (!_db.Vorhanden) return;
            StromspeicherKostenSchema.Laufergebnis e = StromspeicherKostenSchema.Lauf(null);
            Assert.Equal(0, e.Handgriffe);
        }

        /// <summary>
        /// Nachzug Nutzungsdauer (KB-5): Gesäte Investitionspositionen ohne <c>NutzungsdauerID</c> bekommen sie nach der Regel
        /// von Schritt 75; ein zweiter Lauf ändert nichts, eine gepflegte Zuordnung bleibt.
        /// </summary>
        [Fact]
        public void Der_Nachzug_setzt_die_Nutzungsdauer_gesaeter_Positionen_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            const string OHNE = "SELECT COUNT(*) FROM Tab_KostenVorlagePosition p JOIN Tab_KostenVorlage v ON v.ID = p.VorlageID " +
                                "WHERE v.KomponentenID = ? AND v.KategorieID = 1 AND p.Bezeichnung = ? AND p.NutzungsdauerID IS NULL";
            int Ohne(int komponente, string name) => Convert.ToInt32(DataRepository.ExecuteScalar(OHNE,
                new DbParam("@k", komponente), new DbParam("@b", name)), CultureInfo.InvariantCulture);
            // Nach der Hebung trägt keine gesäte Wechselrichter- und Speicherposition mehr einen leeren Verweis.
            Assert.Equal(0, Ohne(3, "Wechselrichter"));
            Assert.Equal(0, Ohne(5, "Speicher"));

            // Ein Stand wie nach 226/228 ohne Schritt 75 im selben Lauf: die Verweise leer, eine Position vom Anwender gepflegt.
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_KostenVorlagePosition SET NutzungsdauerID = NULL WHERE Bezeichnung IN ('Wechselrichter', 'Speicher') " +
                "AND VorlageID IN (SELECT ID FROM Tab_KostenVorlage WHERE KomponentenID IN (3, 5) AND KategorieID = 1)");
            int gepflegt = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(p.ID) FROM Tab_KostenVorlagePosition p JOIN Tab_KostenVorlage v ON v.ID = p.VorlageID " +
                "WHERE v.KomponentenID = 5 AND v.KategorieID = 1 AND p.Bezeichnung = 'Speicher'"), CultureInfo.InvariantCulture);
            int fremd = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Nutzungsdauer"), CultureInfo.InvariantCulture);
            DataRepository.ExecuteSQL("UPDATE Tab_KostenVorlagePosition SET NutzungsdauerID = ? WHERE ID = ?",
                                      new DbParam("@n", fremd), new DbParam("@id", gepflegt));
            int leer = Ohne(3, "Wechselrichter") + Ohne(5, "Speicher");
            Assert.True(leer > 0);

            StromspeicherKostenSchema.Laufergebnis e = StromspeicherKostenSchema.Lauf(null);
            Assert.Equal(leer, e.NutzungsdauerNachgetragen);
            Assert.Equal(0, Ohne(3, "Wechselrichter") + Ohne(5, "Speicher"));
            Assert.Equal(NutzungsdauerSchema.ZeileZu(5, "Batterie"), Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT NutzungsdauerID FROM Tab_KostenVorlagePosition WHERE ID = ?",
                new DbParam("@a", AndereSpeicherposition(gepflegt))), CultureInfo.InvariantCulture));
            Assert.Equal(fremd, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT NutzungsdauerID FROM Tab_KostenVorlagePosition WHERE ID = ?", new DbParam("@id", gepflegt)),
                CultureInfo.InvariantCulture));
            Assert.Equal(0, StromspeicherKostenSchema.Lauf(null).Handgriffe);
        }

        private static int AndereSpeicherposition(int ausser) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT MAX(p.ID) FROM Tab_KostenVorlagePosition p JOIN Tab_KostenVorlage v ON v.ID = p.VorlageID " +
            "WHERE v.KomponentenID = 5 AND v.KategorieID = 1 AND p.Bezeichnung = 'Speicher' AND p.ID <> ?",
            new DbParam("@a", ausser)), CultureInfo.InvariantCulture);

        [Fact]
        public void Die_Pruefsummen_des_Katalogs_gelten_nach_dem_Schritt_als_unveraendert()
        {
            if (!_db.Vorhanden) return;
            Katalogtabelle t = Katalogfassung.Tabelle(StromspeicherKostenSchema.TAB_STAMM);
            List<string> spalten = Katalogfassung.VorhandeneFachspalten(t);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"ID\", \"" + Katalogfassung.SPALTE_PRUEFSUMME + "\" AS S, " + Katalogfassung.Spaltentext(spalten) +
                " FROM \"" + StromspeicherKostenSchema.TAB_STAMM + "\" WHERE \"" + Katalogfassung.SPALTE_PRUEFSUMME +
                "\" IS NOT NULL");
            Assert.NotNull(dt);
            foreach (DataRow r in dt.Rows)
                Assert.Equal(Convert.ToString(r["S"], CultureInfo.InvariantCulture),
                             Katalogfassung.PruefsummeDerZeile(t, spalten, r, Katalogfassung.LeseOhneVorgang));
        }

        [Fact]
        public void Der_Sicherheitslauf_saet_einen_Kostenwert_ohne_Verweis_und_entfernt_die_Spalte()
        {
            if (!_db.Vorhanden) return;
            // Ein älterer Stand: Die Projektkopie 1007013 (BYD, ohne Vorlage) trägt noch einen Leistungssatz und Verschleiß.
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Stromspeicher\" ADD COLUMN \"Leistungskosten\" REAL");
            DataRepository.ExecuteNonQuery("ALTER TABLE \"Tab_Stromspeicher\" ADD COLUMN \"Verschleisskosten\" REAL");
            DataRepository.ExecuteNonQuery(
                "UPDATE \"Tab_Stromspeicher\" SET \"Leistungskosten\" = 200, \"Verschleisskosten\" = 0.04 WHERE \"ID\" = 1007013");
            Assert.Equal(2, StromspeicherKostenSchema.Offen());
            Assert.NotEmpty(StromspeicherKostenSchema.OhneVerweis());

            StromspeicherKostenSchema.Laufergebnis e = StromspeicherKostenSchema.Lauf(null);
            Assert.Equal(2, e.Gesaet);       // Investitions- und Betriebsvorlage
            Assert.Equal(2, e.Entfernt);
            Assert.True(StromspeicherKostenSchema.Vollstaendig());

            KatalogkostensatzCtrl.Stromspeichersaetze s =
                KatalogkostensatzCtrl.Stromspeicher(StromspeicherKostenSchema.TAB_KOPIE, 1007013);
            Assert.Equal(200.0, s.CPowEurProKw.Value, 6);
            Assert.Equal(0.04, s.CVerEurProKwh.Value, 9);
            Assert.Null(s.CCapEurProKwh);
            Assert.Equal(0, StromspeicherKostenSchema.Lauf(null).Handgriffe);
        }

        [Fact]
        public void Der_Satzleser_liest_die_Saat_der_Projektkopie_und_benennt_die_Luecken()
        {
            if (!_db.Vorhanden) return;
            KatalogkostensatzCtrl.Stromspeichersaetze s =
                KatalogkostensatzCtrl.Stromspeicher(StromspeicherKostenSchema.TAB_KOPIE, KOPIE_1007, ANLAGE_1007);
            Assert.Equal(SATZ_VAILLANT, s.CCapEurProKwh.Value, 6);
            Assert.Equal(KatalogkostensatzCtrl.QUELLE_PROJEKTKOPIE, s.Quelle[KatalogkostensatzCtrl.SATZ_CCAP]);
            // Leistung, Festanteil und Verschleiß sind nirgends gepflegt: null, nie still 0, mit benanntem Grund.
            Assert.Null(s.CPowEurProKw);
            Assert.Null(s.IFixEur);
            Assert.Null(s.CVerEurProKwh);
            Assert.Equal(3, s.Datenluecken.Count);
            Assert.Contains(s.Datenluecken, z => z.StartsWith(KatalogkostensatzCtrl.SATZ_CVER, StringComparison.Ordinal));

            // Der Katalogsatz trägt dieselbe Zahl.
            Assert.Equal(SATZ_VAILLANT,
                KatalogkostensatzCtrl.Stromspeicher(StromspeicherKostenSchema.TAB_STAMM, 1).CCapEurProKwh.Value, 6);
        }

        [Fact]
        public void Die_Rangfolge_ist_Position_vor_Projektkopie_vor_Katalogsatz()
        {
            if (!_db.Vorhanden) return;
            // Stufe 3: Ohne eigenen Verweis fällt die Projektkopie auf ihren Katalogsatz.
            DataRepository.ExecuteNonQuery(
                "UPDATE \"Tab_Stromspeicher\" SET \"ID_Stamm\" = 1, \"ID_KostenVorlageInvestition\" = NULL WHERE \"ID\" = ?",
                new DbParam("@id", KOPIE_1007));
            KatalogkostensatzCtrl.Stromspeichersaetze s =
                KatalogkostensatzCtrl.Stromspeicher(StromspeicherKostenSchema.TAB_KOPIE, KOPIE_1007, ANLAGE_1007);
            Assert.Equal(SATZ_VAILLANT, s.CCapEurProKwh.Value, 6);
            Assert.Equal(KatalogkostensatzCtrl.QUELLE_KATALOG, s.Quelle[KatalogkostensatzCtrl.SATZ_CCAP]);

            // Stufe 1: Eine Kostenposition der Anlage schlägt beide Vorlagen — je Satz für sich.
            PositionAnlegen(1007, ANLAGE_1007, DbWerte.KOSTEN_KATEGORIE_INVESTITION,
                            DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, 500.0);
            PositionAnlegen(1007, ANLAGE_1007, DbWerte.KOSTEN_KATEGORIE_BETRIEB,
                            DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, 0.05);
            s = KatalogkostensatzCtrl.Stromspeicher(StromspeicherKostenSchema.TAB_KOPIE, KOPIE_1007, ANLAGE_1007);
            Assert.Equal(500.0, s.CCapEurProKwh.Value, 6);
            Assert.Equal(0.05, s.CVerEurProKwh.Value, 9);
            Assert.Equal(KatalogkostensatzCtrl.QUELLE_POSITION, s.Quelle[KatalogkostensatzCtrl.SATZ_CCAP]);
            // Ohne Anlage (Planwert, Peak-Shaving) zählt die Position nicht.
            Assert.Equal(SATZ_VAILLANT,
                KatalogkostensatzCtrl.Stromspeicher(StromspeicherKostenSchema.TAB_KOPIE, KOPIE_1007).CCapEurProKwh.Value, 6);
        }

        [Fact]
        public void Der_Lauf_liest_den_Speichersatz_wie_vor_dem_Entfall()
        {
            if (!_db.Vorhanden) return;
            SpeicherParameter p = new StromspeicherSimCtrl().LeseParameter(1007, ANLAGE_1007);
            Assert.NotNull(p);
            Assert.Equal(SATZ_VAILLANT, p.CCapEurProKwh, 6);
            Assert.Equal(0.0, p.CPowEurProKw, 9);
            Assert.Equal(0.0, p.IFixEur, 9);
            // Kein Verschleißsatz gepflegt: der Rückfall wie vorher.
            Assert.Equal(StromspeicherSimCtrl.C_VER_STANDARD, p.CVerEurProKwhZyklus, 9);
        }

        [Fact]
        public void Die_Verschleissposition_bucht_genau_einmal_ueber_den_Lauf()
        {
            if (!_db.Vorhanden) return;
            double vorher = WirtschaftlichkeitCtrl.LiesBetriebskosten(1007, WirtschaftlichkeitSzenario.ERWARTET);
            PositionAnlegen(1007, ANLAGE_1007, DbWerte.KOSTEN_KATEGORIE_BETRIEB,
                            DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, 0.05);

            // Die Kostenrechnung bucht sie nicht …
            Assert.Equal(vorher, WirtschaftlichkeitCtrl.LiesBetriebskosten(1007, WirtschaftlichkeitSzenario.ERWARTET), 6);
            Assert.True(WirtschaftlichkeitCtrl.VomLaufGebucht(5, DbWerte.KOSTEN_KATEGORIE_BETRIEB,
                                                               DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, false));
            Assert.False(WirtschaftlichkeitCtrl.VomLaufGebucht(5, DbWerte.KOSTEN_KATEGORIE_BETRIEB,
                                                                DbWerte.BEMESSUNG_JAHRESBETRAG, false));
            Assert.False(WirtschaftlichkeitCtrl.VomLaufGebucht(1, DbWerte.KOSTEN_KATEGORIE_BETRIEB,
                                                                DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, false));
            // … der Lauf nimmt ihren Satz als c_ver und bucht ihn als eigene Zeile.
            SpeicherParameter p = new StromspeicherSimCtrl().LeseParameter(1007, ANLAGE_1007);
            Assert.Equal(0.05, p.CVerEurProKwhZyklus, 9);
        }

        [Fact]
        public void Die_Flotte_fuellt_die_Einheit_aus_der_Satzvorlage_und_uebernimmt_als_Positionen()
        {
            if (!_db.Vorhanden) return;
            FlottenEinheit e = SpeicherFlottenStudieCtrl.EinheitAusKatalog(1);
            Assert.NotNull(e);
            Assert.True(e.EigeneKosten);
            Assert.Equal(SATZ_VAILLANT, e.InvestitionEuroProKWh, 6);
            Assert.Equal(0.0, e.InvestitionEuroProKw, 9);

            e.InvestitionEuroProKw = 150.0;
            e.InvestitionEuro = 1200.0;
            e.DurchsatzkostenEuroProKWhEntladung = 0.03;
            FlottenUebernahmeErgebnis erg = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(1007, new[] { e });
            Assert.True(erg.Erfolg, erg.Meldung);
            int anlage = erg.Anlagen.Single().AnlageId;
            int geraet = erg.Anlagen.Single().GeraeteId;

            KatalogkostensatzCtrl.Stromspeichersaetze s =
                KatalogkostensatzCtrl.Stromspeicher(StromspeicherKostenSchema.TAB_KOPIE, geraet, anlage);
            Assert.Equal(SATZ_VAILLANT, s.CCapEurProKwh.Value, 6);
            Assert.Equal(150.0, s.CPowEurProKw.Value, 6);
            Assert.Equal(1200.0, s.IFixEur.Value, 6);
            Assert.Equal(0.03, s.CVerEurProKwh.Value, 9);
            Assert.Equal(KatalogkostensatzCtrl.QUELLE_POSITION, s.Quelle[KatalogkostensatzCtrl.SATZ_CPOW]);
            Assert.Equal(4, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID_Anlage = ? AND KomponentenID = 5",
                new DbParam("@a", anlage)), CultureInfo.InvariantCulture));
        }

        private static void PositionAnlegen(int projekt, int anlage, int kategorie, string bemessung, double satz)
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                "Nutzungsdauer, Einheit, Gruppe, Kostenart, Bemessung, Einheitpreis, IstErloes, ID_Anlage) " +
                "VALUES (?, ?, 5, ?, 0, 0, '€', 'Allgemein', ?, ?, ?, 0, ?)",
                new DbParam("@p", projekt),
                new DbParam("@s", SpeicherFlottenStudieCtrl.KostenstammStromspeicher()),
                new DbParam("@k", kategorie),
                new DbParam("@ka", kategorie == DbWerte.KOSTEN_KATEGORIE_INVESTITION
                    ? DbWerte.KOSTENART_KAPITALGEBUNDEN : DbWerte.KOSTENART_BETRIEBSGEBUNDEN),
                new DbParam("@b", bemessung), new DbParam("@sz", satz), new DbParam("@a", anlage));
        }
    }
}
