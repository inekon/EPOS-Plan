using System;
using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Volumen ist die einzige Bezugsgröße des Pufferspeichers</b> — Auftrag #284,
    /// Anwenderentscheid vom 15.09.2026, wortgleich: „Prüfe Pufferspeicher Daten mit
    /// Volumen/Größe. EUR_PRO_KWH_KAPAZITAET spielt keine Rolle, nur das Volumen als
    /// Bezugsgröße."
    ///
    /// <para><b>Was der Entscheid schließt.</b> Der Pufferspeicher führt keine
    /// kWh-Kapazität: Ohne Temperaturpaar gibt es keine belastbare Umrechnung seines
    /// Volumens. „je kWh Kapazität" blieb dort deshalb ohne Bezugsgröße, wurde aber
    /// weiter angeboten — die Bemessungsliste war flach, ohne Zuordnung je Gewerk — und
    /// die ausgelieferte Investitionsvorlage trug die Art sogar. Ab hier gilt am
    /// Pufferspeicher nur noch die Bemessung je LITER Gesamtvolumen
    /// (<c>EUR_PRO_KW_LEISTUNG</c>, Anzeige „je Liter", €/Ltr.).</para>
    ///
    /// <para><b>Zwei Seiten, ein Nachweis.</b> Die AUSWAHL (Kern, ohne Datenbank) und der
    /// SCHEMASCHRITT 77 an der Messlatte. Die Fälle der zweiten Gruppe brauchen eine
    /// Arbeitskopie; fehlt die Datei, schweigen sie.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PufferspeicherVolumenbemessungTests
    {
        /// <summary><c>Tab_KostenKomponente.ID</c> der Gewerke, die hier vorkommen.</summary>
        private const int K_WAERMEPUMPE = 1;
        private const int K_STROMSPEICHER = 5;
        private const int K_PUFFER = 6;

        /// <summary>Alle zehn Kostenkomponenten der Datenbank.</summary>
        private static readonly int[] ALLE_GEWERKE = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // =====================================================================
        //  Die Auswahl
        // =====================================================================

        /// <summary>DIE EIGENTLICHE ZUSAGE: „je kWh Kapazität" steht am Pufferspeicher
        /// nicht mehr zur Wahl — weder im Investitions- noch im Betriebsraster.</summary>
        [Fact]
        public void Am_Pufferspeicher_steht_je_kWh_Kapazitaet_nicht_mehr_zur_Wahl()
        {
            Assert.DoesNotContain(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                                  Persistenzwerte(K_PUFFER, true));
            Assert.DoesNotContain(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                                  Persistenzwerte(K_PUFFER, false));
        }

        /// <summary>Am Stromspeicher bleibt sie — dort IST die Kapazität die Baugröße
        /// (<c>Tab_Stromspeicher.Energie</c>).</summary>
        [Fact]
        public void Am_Stromspeicher_bleibt_je_kWh_Kapazitaet_waehlbar()
        {
            Assert.Contains(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                            Persistenzwerte(K_STROMSPEICHER, true));
        }

        /// <summary>Was am Pufferspeicher bleibt: die Volumenbemessung und die Arten, die
        /// gar keine Baugröße brauchen. Die Liste ist damit vollständig benannt — in
        /// BEIDEN Rastern, denn ein Volumen trägt eine Wartung genauso wie einen
        /// Kaufpreis.</summary>
        [Fact]
        public void Am_Pufferspeicher_bleibt_die_Volumenbemessung_und_das_Absolute()
        {
            Assert.Equal(new[]
                {
                    DbWerte.BEMESSUNG_BETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                },
                Persistenzwerte(K_PUFFER, true));

            Assert.Equal(new[]
                {
                    DbWerte.BEMESSUNG_JAHRESBETRAG,
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                    DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                },
                Persistenzwerte(K_PUFFER, false));
        }

        /// <summary>
        /// Der Geltungsbereich umfasst alle zehn Gewerke (zweiter Entscheid vom
        /// 15.09.2026, Auftrag #287): Der Pufferspeicher ist darin kein Sonderfall mehr,
        /// sondern das Gewerk, an dem die Regel zuerst scharf geschaltet wurde. Was JEDES
        /// Gewerk anbietet und was nicht, steht Wort für Wort in
        /// <c>BemessungsauswahlJeGewerkTests</c>.
        /// </summary>
        [Fact]
        public void Auch_die_uebrigen_Gewerke_stehen_im_Geltungsbereich()
        {
            foreach (int k in ALLE_GEWERKE)
                Assert.True(BemessungKatalog.AuswahlWirdGefiltert(k),
                            "Gewerk " + k + " fehlt im Geltungsbereich.");
        }

        /// <summary>Ohne bekanntes Gewerk (0) wird nicht gefiltert — sonst verschwänden
        /// Arten aus einer Liste, die gar kein Gewerk meint.</summary>
        [Fact]
        public void Ohne_Gewerk_wird_nicht_gefiltert()
        {
            Assert.Equal(Rasterliste(true), Persistenzwerte(0, true));
            Assert.True(BemessungKatalog.PasstZuGewerk(
                DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, 0));
        }

        /// <summary>
        /// Eine BESTANDSZEILE verliert ihre Art nicht: Trägt eine vorhandene Position am
        /// Pufferspeicher noch „je kWh Kapazität", steht die Art weiter in der Liste —
        /// sonst verlöre die Zeile beim Anzeigen ihren Wert.
        /// </summary>
        [Fact]
        public void Eine_Bestandszeile_behaelt_ihre_Art_in_der_Liste()
        {
            var benutzt = new HashSet<string>(StringComparer.Ordinal)
                { DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET };

            Assert.Contains(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                            Persistenzwerte(K_PUFFER, true, benutzt));
        }

        /// <summary>
        /// KEINE ZWEITE LISTE: Die Auswahl fragt dieselbe Landkarte, aus der auch der
        /// Dialog seinen Grundtext holt. Für JEDE Art und JEDES Gewerk stimmt
        /// <see cref="BemessungKatalog.PasstZuGewerk"/> mit
        /// <see cref="WirtschaftlichkeitCtrl.BasisGrund"/> überein — und für die Arten der
        /// Gerätewelt zusätzlich mit <see cref="TechnikPlanwertCtrl.KenntBaugroesse"/>.
        /// </summary>
        [Fact]
        public void Die_Zuordnung_kommt_aus_EINER_Landkarte()
        {
            foreach (BemessungKatalog.Info i in BemessungKatalog.Alle)
                foreach (int k in ALLE_GEWERKE)
                {
                    bool passt = BemessungKatalog.PasstZuGewerk(i.Persistenz, k);
                    Assert.Equal(
                        !string.Equals(WirtschaftlichkeitCtrl.BasisGrund(i.Persistenz, k),
                                       WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                                       StringComparison.Ordinal),
                        passt);
                }

            // Die vier Arten der Gerätewelt: Da ist die Landkarte die Gerätespalte selbst.
            string[] geraetearten =
            {
                DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
                DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
                DbWerte.BEMESSUNG_EUR_PRO_KWP,
                DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR,
            };
            foreach (string bem in geraetearten)
                foreach (int k in ALLE_GEWERKE)
                    Assert.Equal(TechnikPlanwertCtrl.KenntBaugroesse(k, bem),
                                 BemessungKatalog.PasstZuGewerk(bem, k));

            // E20 (Anwenderentscheid 25.09.2026): Die Landkarte kennt das RASTER — und
            // bleibt je Raster EINE. Für beide Raster stimmen Auswahl, Grund und
            // Gerätewelt überein; die Fassung ohne Raster ist die des Betriebsrasters.
            foreach (bool invest in new[] { false, true })
            {
                foreach (BemessungKatalog.Info i in BemessungKatalog.Alle)
                    foreach (int k in ALLE_GEWERKE)
                        Assert.Equal(
                            !string.Equals(WirtschaftlichkeitCtrl.BasisGrund(i.Persistenz, k, false, invest),
                                           WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                                           StringComparison.Ordinal),
                            BemessungKatalog.PasstZuGewerk(i.Persistenz, k, invest));

                foreach (string bem in geraetearten)
                    foreach (int k in ALLE_GEWERKE)
                        Assert.Equal(TechnikPlanwertCtrl.KenntBaugroesse(k, bem, invest),
                                     BemessungKatalog.PasstZuGewerk(bem, k, invest));
            }
        }

        /// <summary>
        /// E20: Die Raster unterscheiden sich in GENAU EINER Zelle der Kreuztafel —
        /// „je kW elektrisch" an der Wärmepumpe (Anwenderentscheid 25.09.2026:
        /// „Wärmepumpe beides" nur bei den Investitionskosten). Jede andere Kombination
        /// aus Art und Gewerk antwortet in beiden Rastern gleich.
        /// </summary>
        [Fact]
        public void Die_Raster_unterscheiden_sich_nur_in_je_kW_elektrisch_an_der_Waermepumpe()
        {
            var abweichend = new List<string>();
            foreach (BemessungKatalog.Info i in BemessungKatalog.Alle)
                foreach (int k in ALLE_GEWERKE)
                    if (BemessungKatalog.PasstZuGewerk(i.Persistenz, k, true) !=
                        BemessungKatalog.PasstZuGewerk(i.Persistenz, k, false))
                        abweichend.Add(k + " " + i.Persistenz);

            Assert.Equal(new[] { "1 " + DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH }, abweichend);
            Assert.True(BemessungKatalog.PasstZuGewerk(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, 1, true));
            Assert.False(BemessungKatalog.PasstZuGewerk(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, 1, false));
        }

        /// <summary>Am Pufferspeicher führt die verbleibende Art auch wirklich eine
        /// Bezugsgröße — das Volumen; die Beschriftung folgt ihr.</summary>
        [Fact]
        public void Die_verbleibende_Art_fuehrt_am_Pufferspeicher_das_Volumen()
        {
            Assert.True(TechnikPlanwertCtrl.KenntBaugroesse(
                K_PUFFER, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG));
            Assert.False(TechnikPlanwertCtrl.KenntBaugroesse(
                K_PUFFER, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET));
        }

        // =====================================================================
        //  Der Schemaschritt 77
        // =====================================================================

        /// <summary>Saat und Nachzug tragen DIESELBE Art — eine frisch gesäte Datenbank
        /// und eine nachgezogene dürfen nicht auseinanderlaufen.</summary>
        [Fact]
        public void Saat_und_Nachzug_tragen_dieselbe_Art()
        {
            string ausDerSaat = null;
            foreach (SchemaKatalog.KostenVorlagenSeed s in SchemaKatalog.Schritt39_Vorlagen)
            {
                if (!string.Equals(s.Komponente, DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER,
                                   StringComparison.Ordinal)) continue;
                if (s.KategorieId != DbWerte.KOSTEN_KATEGORIE_INVESTITION) continue;
                foreach (SchemaKatalog.VorlagenPositionSeed p in s.Positionen)
                    if (string.Equals(p.Bezeichnung, "Speicher", StringComparison.Ordinal))
                        ausDerSaat = p.Bemessung;
            }

            Assert.Equal(PufferspeicherBemessungVolumen.BEMESSUNG_NEU, ausDerSaat);
            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, ausDerSaat);
        }

        /// <summary>Die Messlatte ist nachgezogen: keine Vorlagenzeile des
        /// Pufferspeichers trägt noch die alte Art, und die Position „Speicher" führt die
        /// Volumenbemessung.</summary>
        [Fact]
        public void Die_ausgelieferte_Vorlage_traegt_die_Volumenbemessung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, Zahl(PufferspeicherBemessungVolumen.Zaehlung()));
            Assert.False(PufferspeicherBemessungVolumen.UmstellungNoetig());

            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, BemessungDerSpeicherzeile(K_PUFFER));
        }

        /// <summary>Die Vorlage des STROMSPEICHERS bleibt, wie sie war — der Entscheid
        /// gilt dem Pufferspeicher.</summary>
        [Fact]
        public void Die_Vorlage_des_Stromspeichers_bleibt_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
                         BemessungDerSpeicherzeile(K_STROMSPEICHER));
        }

        /// <summary>
        /// DIE GRENZE DES SCHRITTS: Eine Zeile mit GEPFLEGTEM Satz wird nicht umgedeutet.
        /// Ihre Zahl ist ein €/kWh-Satz; als €/Ltr.-Satz weitergeführt wäre sie eine
        /// andere Zahl mit derselben Ziffer.
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

            int id = SpeicherzeileId(K_PUFFER);
            Assert.True(id > 0);

            // Altstand nachbauen: alte Art, gepflegter Satz.
            DataRepository.ExecuteSQL(
                "UPDATE \"Tab_KostenVorlagePosition\" SET \"Bemessung\" = ?, \"Satz\" = ? " +
                "WHERE \"ID\" = ?",
                new DbParam("@b", DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET),
                new DbParam("@s", 250.0),
                new DbParam("@id", id));

            Assert.Equal(0, Zahl(PufferspeicherBemessungVolumen.Zaehlung()));
            Assert.Equal(1, Zahl(PufferspeicherBemessungVolumen.ZaehlungGepflegt()));

            DataRepository.ExecuteNonQuery(PufferspeicherBemessungVolumen.SQL_UMSTELLEN);

            // Unverändert: Art UND Satz stehen noch da.
            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, BemessungDerSpeicherzeile(K_PUFFER));
            Assert.Equal(1, Zahl(PufferspeicherBemessungVolumen.ZaehlungGepflegt()));
        }

        /// <summary>Der Schritt selbst — auf einem nachgebauten Altstand OHNE Satz: Er
        /// stellt genau eine Zeile um und ist wiederholbar.</summary>
        [Fact]
        public void Der_Schritt_stellt_eine_Zeile_ohne_Satz_um_und_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = SpeicherzeileId(K_PUFFER);
            Assert.True(id > 0);

            DataRepository.ExecuteSQL(
                "UPDATE \"Tab_KostenVorlagePosition\" SET \"Bemessung\" = ?, \"Satz\" = NULL " +
                "WHERE \"ID\" = ?",
                new DbParam("@b", DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET),
                new DbParam("@id", id));

            Assert.Equal(1, Zahl(PufferspeicherBemessungVolumen.Zaehlung()));
            Assert.True(PufferspeicherBemessungVolumen.UmstellungNoetig());

            DataRepository.ExecuteNonQuery(PufferspeicherBemessungVolumen.SQL_UMSTELLEN);
            Assert.Equal(0, Zahl(PufferspeicherBemessungVolumen.Zaehlung()));
            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, BemessungDerSpeicherzeile(K_PUFFER));

            // Zweiter Lauf: nichts mehr zu tun, und nichts ändert sich.
            DataRepository.ExecuteNonQuery(PufferspeicherBemessungVolumen.SQL_UMSTELLEN);
            Assert.Equal(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, BemessungDerSpeicherzeile(K_PUFFER));
        }

        /// <summary>Der Schritt fasst NUR die Vorlagen an: <c>Tab_ProjektWerte</c> führt
        /// die Art am Pufferspeicher nicht, und daran ändert er nichts.</summary>
        [Fact]
        public void Die_Projektwerte_bleiben_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int vorher = Zahl(
                "SELECT COUNT(*) FROM \"Tab_ProjektWerte\" WHERE \"KomponentenID\" = 6 " +
                "AND \"Bemessung\" = '" + DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET + "'");
            Assert.Equal(0, vorher);

            int alle = Zahl("SELECT COUNT(*) FROM \"Tab_ProjektWerte\"");
            DataRepository.ExecuteNonQuery(PufferspeicherBemessungVolumen.SQL_UMSTELLEN);
            Assert.Equal(alle, Zahl("SELECT COUNT(*) FROM \"Tab_ProjektWerte\""));
            Assert.Equal(0, Zahl(
                "SELECT COUNT(*) FROM \"Tab_ProjektWerte\" WHERE \"KomponentenID\" = 6 " +
                "AND \"Bemessung\" = '" + DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG + "'"));
        }

        /// <summary>Die Messlatte steht auf dem Zielstand des Schritts.</summary>
        [Fact]
        public void Die_Messlatte_steht_auf_dem_Zielstand()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(SchemaStand.Zielversion >= 77,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 77.");
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= SchemaStand.Zielversion);
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        private static string[] Persistenzwerte(int komponentenId, bool invest,
                                                ICollection<string> benutzt = null)
        {
            List<BemessungKatalog.Info> liste =
                BemessungKatalog.Auswahl(komponentenId, invest, benutzt);
            var werte = new string[liste.Count];
            for (int n = 0; n < liste.Count; n++) werte[n] = liste[n].Persistenz;
            return werte;
        }

        /// <summary>Die ungefilterte Rasterliste des Katalogs — der Stand vor dem
        /// Entscheid, gegen den die übrigen Gewerke gehalten werden.</summary>
        private static string[] Rasterliste(bool invest)
        {
            var werte = new List<string>();
            foreach (BemessungKatalog.Info i in BemessungKatalog.Alle)
                if (invest ? i.FuerInvest : i.FuerBetrieb) werte.Add(i.Persistenz);
            return werte.ToArray();
        }

        /// <summary>Die Bemessung der Position „Speicher" in der Investitionsvorlage
        /// eines Gewerks.</summary>
        private static string BemessungDerSpeicherzeile(int komponentenId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT p.\"Bemessung\" FROM \"Tab_KostenVorlagePosition\" AS p " +
                "INNER JOIN \"Tab_KostenVorlage\" AS v ON v.\"ID\" = p.\"VorlageID\" " +
                "WHERE v.\"KomponentenID\" = ? AND v.\"KategorieID\" = 1 " +
                "AND p.\"Bezeichnung\" = 'Speicher'",
                new DbParam("@k", komponentenId));
            return o == null || o == DBNull.Value ? null : Convert.ToString(o);
        }

        private static int SpeicherzeileId(int komponentenId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT p.\"ID\" FROM \"Tab_KostenVorlagePosition\" AS p " +
                "INNER JOIN \"Tab_KostenVorlage\" AS v ON v.\"ID\" = p.\"VorlageID\" " +
                "WHERE v.\"KomponentenID\" = ? AND v.\"KategorieID\" = 1 " +
                "AND p.\"Bezeichnung\" = 'Speicher'",
                new DbParam("@k", komponentenId));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }
    }
}
