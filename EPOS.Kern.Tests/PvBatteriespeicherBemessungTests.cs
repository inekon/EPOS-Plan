using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die PV-Vorlagenzeile „Batteriespeicher" bekommt eine Bezugsgröße, die es
    /// gibt</b> — Auftrag #287, Anwenderentscheid vom 15.09.2026, Schemaschritt 78.
    ///
    /// <para><b>Was der Entscheid schließt.</b> Die ausgelieferte Investitionsvorlage der
    /// Photovoltaik (Vorlage 5, <c>Tab_KostenKomponente.ID</c> 3) führte die Position
    /// „Batteriespeicher" mit „je kWh Kapazität". Eine Kapazität führt die PV-Anlage
    /// nicht: Ihre einzige Baugröße ist die installierte Leistung in kWp. Ein Satz an so
    /// einer Zeile fiel über den Anwenderentscheid I-2 auf den erfassten Betrag zurück —
    /// bei einer reinen Satzzeile also auf 0, und zwar ohne Warnung.</para>
    ///
    /// <para><b>Warum der feste Betrag und nicht „je kWp".</b> Die beiden Arten mit echter
    /// Baugröße meinen an der Photovoltaik dieselbe Zahl, die Modulleistung; ein
    /// Batteriespeicher-Satz je kWp bemäße den Preis eines Geräts an der Größe eines
    /// anderen. Die übrigen Gerätepositionen derselben Vorlage stehen bereits auf dem
    /// festen Betrag. Wer nach der Kapazität bemessen will, nimmt das Gewerk
    /// Stromspeicher — dort IST sie die Baugröße.</para>
    ///
    /// <para><b>Zwei Seiten, ein Nachweis.</b> Die LANDKARTE (Kern, ohne Datenbank) und
    /// der SCHEMASCHRITT 78 an der Messlatte. Die Fälle der zweiten Gruppe brauchen eine
    /// Arbeitskopie; fehlt die Datei, schweigen sie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PvBatteriespeicherBemessungTests
    {
        private const int K_PHOTOVOLTAIK = 3;
        private const int K_STROMSPEICHER = 5;

        // =====================================================================
        //  Die Landkarte
        // =====================================================================

        /// <summary>Die Photovoltaik führt keine kWh-Kapazität — und der feste Betrag
        /// braucht gar keine Bezugsgröße.</summary>
        [Fact]
        public void Die_Photovoltaik_fuehrt_keine_Kapazitaet()
        {
            Assert.False(TechnikPlanwertCtrl.KenntBaugroesse(
                K_PHOTOVOLTAIK, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET));
            Assert.False(BemessungKatalog.PasstZuGewerk(
                PvVorlageBatteriespeicher.BEMESSUNG_ALT, K_PHOTOVOLTAIK));

            Assert.True(BemessungKatalog.PasstZuGewerk(
                PvVorlageBatteriespeicher.BEMESSUNG_NEU, K_PHOTOVOLTAIK));
            Assert.Equal("", WirtschaftlichkeitCtrl.BasisGrund(
                PvVorlageBatteriespeicher.BEMESSUNG_NEU, K_PHOTOVOLTAIK));
        }

        /// <summary>Die neue Art ist ABSOLUT: Der erfasste Wert IST der Betrag, es gibt
        /// keine Menge, die fehlen könnte — genau deshalb ist sie hier die richtige.</summary>
        [Fact]
        public void Die_neue_Art_ist_absolut_und_kann_nicht_ins_Leere_laufen()
        {
            BemessungKatalog.Info i =
                BemessungKatalog.Finde(PvVorlageBatteriespeicher.BEMESSUNG_NEU);
            Assert.NotNull(i);
            Assert.True(i.Absolut);
            Assert.True(i.FuerInvest);
        }

        /// <summary>Am Stromspeicher bleibt die Kapazität, was sie ist — die
        /// kapazitätsbemessene Zeile geht nicht verloren, sie steht am richtigen
        /// Gewerk.</summary>
        [Fact]
        public void Am_Stromspeicher_bleibt_die_Kapazitaet_die_Baugroesse()
        {
            Assert.True(TechnikPlanwertCtrl.KenntBaugroesse(
                K_STROMSPEICHER, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET));
            Assert.Contains(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                            Persistenzwerte(K_STROMSPEICHER, true));
        }

        /// <summary>Und an der Photovoltaik steht „je kWh Kapazität" nicht mehr zur
        /// Wahl — weder im Investitions- noch im Betriebsraster.</summary>
        [Fact]
        public void An_der_Photovoltaik_steht_je_kWh_Kapazitaet_nicht_mehr_zur_Wahl()
        {
            Assert.DoesNotContain(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                                  Persistenzwerte(K_PHOTOVOLTAIK, true));
            Assert.DoesNotContain(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                                  Persistenzwerte(K_PHOTOVOLTAIK, false));
        }

        // =====================================================================
        //  Der Schemaschritt 78
        // =====================================================================

        /// <summary>Saat und Nachzug tragen DIESELBE Art — eine frisch gesäte Datenbank
        /// und eine nachgezogene dürfen nicht auseinanderlaufen.</summary>
        [Fact]
        public void Saat_und_Nachzug_tragen_dieselbe_Art()
        {
            string ausDerSaat = null;
            foreach (SchemaKatalog.KostenVorlagenSeed s in SchemaKatalog.Schritt39_Vorlagen)
            {
                if (!string.Equals(s.Komponente, DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK,
                                   StringComparison.Ordinal)) continue;
                if (s.KategorieId != DbWerte.KOSTEN_KATEGORIE_INVESTITION) continue;
                foreach (SchemaKatalog.VorlagenPositionSeed p in s.Positionen)
                    if (string.Equals(p.Bezeichnung, PvVorlageBatteriespeicher.POSITION,
                                      StringComparison.Ordinal))
                        ausDerSaat = p.Bemessung;
            }

            Assert.Equal(PvVorlageBatteriespeicher.BEMESSUNG_NEU, ausDerSaat);
            Assert.Equal(DbWerte.BEMESSUNG_BETRAG, ausDerSaat);
        }

        /// <summary>Die Messlatte ist nachgezogen: keine Vorlagenzeile der Photovoltaik
        /// trägt noch die alte Art, und die Position „Batteriespeicher" führt den festen
        /// Betrag.</summary>
        [Fact]
        public void Die_ausgelieferte_Vorlage_traegt_den_festen_Betrag()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, Zahl(PvVorlageBatteriespeicher.Zaehlung()));
            Assert.False(PvVorlageBatteriespeicher.UmstellungNoetig());

            Assert.Equal(DbWerte.BEMESSUNG_BETRAG,
                         BemessungDerPosition(K_PHOTOVOLTAIK, PvVorlageBatteriespeicher.POSITION));
        }

        /// <summary>Die Vorlage des STROMSPEICHERS bleibt, wie sie war — der Entscheid
        /// gilt der Photovoltaik.</summary>
        [Fact]
        public void Die_Vorlage_des_Stromspeichers_bleibt_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                         BemessungDerPosition(K_STROMSPEICHER, "Speicher"));
        }

        /// <summary>
        /// DIE GRENZE DES SCHRITTS: Eine Zeile mit GEPFLEGTEM Satz wird nicht umgedeutet.
        /// Ihre Zahl ist ein €/kWh-Satz; als fester Betrag weitergeführt wäre aus einem
        /// Satz je Einheit ein Gesamtbetrag geworden.
        ///
        /// <para>Der Fall ist möglich: Der Schreibschutz der Auslieferungsvorlagen ist
        /// aufgehoben (Ä8), jede Vorlagenzeile lässt sich pflegen. Er wird hier eigens
        /// hergestellt.</para>
        /// </summary>
        [Fact]
        public void Eine_Zeile_mit_gepflegtem_Satz_wird_nicht_umgedeutet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = PositionsId(K_PHOTOVOLTAIK, PvVorlageBatteriespeicher.POSITION);
            Assert.True(id > 0);

            DataRepository.ExecuteSQL(
                "UPDATE \"Tab_KostenVorlagePosition\" SET \"Bemessung\" = ?, \"Satz\" = ? " +
                "WHERE \"ID\" = ?",
                new DbParam("@b", DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET),
                new DbParam("@s", 450.0),
                new DbParam("@id", id));

            Assert.Equal(0, Zahl(PvVorlageBatteriespeicher.Zaehlung()));
            Assert.Equal(1, Zahl(PvVorlageBatteriespeicher.ZaehlungGepflegt()));

            DataRepository.ExecuteNonQuery(PvVorlageBatteriespeicher.SQL_UMSTELLEN);

            // Unverändert: Art UND Satz stehen noch da.
            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                         BemessungDerPosition(K_PHOTOVOLTAIK, PvVorlageBatteriespeicher.POSITION));
            Assert.Equal(1, Zahl(PvVorlageBatteriespeicher.ZaehlungGepflegt()));
        }

        /// <summary>Der Schritt selbst — auf einem nachgebauten Altstand OHNE Satz: Er
        /// stellt genau eine Zeile um und ist wiederholbar.</summary>
        [Fact]
        public void Der_Schritt_stellt_eine_Zeile_ohne_Satz_um_und_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = PositionsId(K_PHOTOVOLTAIK, PvVorlageBatteriespeicher.POSITION);
            Assert.True(id > 0);

            DataRepository.ExecuteSQL(
                "UPDATE \"Tab_KostenVorlagePosition\" SET \"Bemessung\" = ?, \"Satz\" = NULL " +
                "WHERE \"ID\" = ?",
                new DbParam("@b", DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET),
                new DbParam("@id", id));

            Assert.Equal(1, Zahl(PvVorlageBatteriespeicher.Zaehlung()));
            Assert.True(PvVorlageBatteriespeicher.UmstellungNoetig());

            DataRepository.ExecuteNonQuery(PvVorlageBatteriespeicher.SQL_UMSTELLEN);
            Assert.Equal(0, Zahl(PvVorlageBatteriespeicher.Zaehlung()));
            Assert.Equal(DbWerte.BEMESSUNG_BETRAG,
                         BemessungDerPosition(K_PHOTOVOLTAIK, PvVorlageBatteriespeicher.POSITION));

            // Zweiter Lauf: nichts mehr zu tun, und nichts ändert sich.
            DataRepository.ExecuteNonQuery(PvVorlageBatteriespeicher.SQL_UMSTELLEN);
            Assert.Equal(DbWerte.BEMESSUNG_BETRAG,
                         BemessungDerPosition(K_PHOTOVOLTAIK, PvVorlageBatteriespeicher.POSITION));
        }

        /// <summary>Der Schritt fasst NUR die Vorlagen der Photovoltaik an: Weder
        /// <c>Tab_ProjektWerte</c> noch die Vorlage des Stromspeichers ändern sich.</summary>
        [Fact]
        public void Projektwerte_und_Stromspeichervorlage_bleiben_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int alle = Zahl("SELECT COUNT(*) FROM \"Tab_ProjektWerte\"");
            int kapazitaetVorher = Zahl(
                "SELECT COUNT(*) FROM \"Tab_KostenVorlagePosition\" " +
                "WHERE \"Bemessung\" = '" + DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET + "'");

            DataRepository.ExecuteNonQuery(PvVorlageBatteriespeicher.SQL_UMSTELLEN);

            Assert.Equal(alle, Zahl("SELECT COUNT(*) FROM \"Tab_ProjektWerte\""));
            Assert.Equal(kapazitaetVorher, Zahl(
                "SELECT COUNT(*) FROM \"Tab_KostenVorlagePosition\" " +
                "WHERE \"Bemessung\" = '" + DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET + "'"));
            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                         BemessungDerPosition(K_STROMSPEICHER, "Speicher"));
        }

        /// <summary>
        /// DER BESTAND DER MESSLATTE, ausgezählt: Nach dem Schritt trägt KEINE
        /// Vorlagenzeile und KEINE Projektzeile mehr eine Art, die an ihrem eigenen Gewerk
        /// keine Bezugsgröße führt. Genau das ist die Zusage, unter der die Filterung auf
        /// alle zehn Gewerke ausgeweitet werden durfte.
        /// </summary>
        [Fact]
        public void Kein_Bestand_traegt_noch_eine_gewerksfremde_Art()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (BemessungKatalog.Info i in BemessungKatalog.Alle)
                for (int k = 1; k <= 10; k++)
                {
                    if (BemessungKatalog.PasstZuGewerk(i.Persistenz, k)) continue;

                    Assert.Equal(0, Zahl(
                        "SELECT COUNT(*) FROM \"Tab_KostenVorlagePosition\" AS p " +
                        "INNER JOIN \"Tab_KostenVorlage\" AS v ON v.\"ID\" = p.\"VorlageID\" " +
                        "WHERE v.\"KomponentenID\" = " + k +
                        " AND p.\"Bemessung\" = '" + i.Persistenz + "'"));

                    Assert.Equal(0, Zahl(
                        "SELECT COUNT(*) FROM \"Tab_ProjektWerte\" " +
                        "WHERE \"KomponentenID\" = " + k +
                        " AND \"Bemessung\" = '" + i.Persistenz + "'"));
                }
        }

        /// <summary>Die Messlatte steht auf dem Zielstand des Schritts.</summary>
        [Fact]
        public void Die_Messlatte_steht_auf_dem_Zielstand()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(SchemaStand.Zielversion >= 78,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 78.");
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= SchemaStand.Zielversion);
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        private static string[] Persistenzwerte(int komponentenId, bool invest)
        {
            System.Collections.Generic.List<BemessungKatalog.Info> liste =
                BemessungKatalog.Auswahl(komponentenId, invest, null);
            var werte = new string[liste.Count];
            for (int n = 0; n < liste.Count; n++) werte[n] = liste[n].Persistenz;
            return werte;
        }

        /// <summary>Die Bemessung einer Position der Investitionsvorlage eines Gewerks.</summary>
        private static string BemessungDerPosition(int komponentenId, string bezeichnung)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT p.\"Bemessung\" FROM \"Tab_KostenVorlagePosition\" AS p " +
                "INNER JOIN \"Tab_KostenVorlage\" AS v ON v.\"ID\" = p.\"VorlageID\" " +
                "WHERE v.\"KomponentenID\" = ? AND v.\"KategorieID\" = 1 " +
                "AND p.\"Bezeichnung\" = ?",
                new DbParam("@k", komponentenId), new DbParam("@b", bezeichnung));
            return o == null || o == DBNull.Value ? null : Convert.ToString(o);
        }

        private static int PositionsId(int komponentenId, string bezeichnung)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT p.\"ID\" FROM \"Tab_KostenVorlagePosition\" AS p " +
                "INNER JOIN \"Tab_KostenVorlage\" AS v ON v.\"ID\" = p.\"VorlageID\" " +
                "WHERE v.\"KomponentenID\" = ? AND v.\"KategorieID\" = 1 " +
                "AND p.\"Bezeichnung\" = ?",
                new DbParam("@k", komponentenId), new DbParam("@b", bezeichnung));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }
    }
}
