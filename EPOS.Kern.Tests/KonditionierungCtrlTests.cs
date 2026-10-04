using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Datenbankfälle des Konditionierungscontrollers</b> (Stufe KP1, Konzept
    /// Konditionierungsprofile 5.1, 5.6 und 6): Schemaschritt zweimal, Eigentümerregel,
    /// Löschkaskade, Anlegen, Verwerfen, „erneut anwenden" nach P12 und die Vorgabezeilen.
    ///
    /// <para>Die Fälle arbeiten auf einer Arbeitskopie der Testdatenbank
    /// (<see cref="TestDatenbank"/>); ohne sie schweigen sie. Sie pinnen keine Kultur — die
    /// Prüfungen vergleichen Zahlen und Kennwörter, keine Ressourcentexte.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KonditionierungCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly KonditionierungCtrl _ctrl = new KonditionierungCtrl();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        private static bool Bereit() => KonditionierungSchema.Lesbar();

        /// <summary>Ein Projektgebäude der Testdatenbank — das erste, das es gibt.</summary>
        private static long EinGebaeude()
        {
            object o = DataRepository.ExecuteScalar("SELECT MIN(ID) FROM \"Tab_Gebaeude\"");
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Ein Katalogbau der Testdatenbank.</summary>
        private static long EinKatalogbau()
        {
            object o = DataRepository.ExecuteScalar("SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\"");
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static long Zaehlen(string tabelle)
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + tabelle + "\""),
                               CultureInfo.InvariantCulture);

        /// <summary>
        /// Die Perioden außerhalb der Vorlagenkalender — die Saat der ausgelieferten Vorlagen (Schritt 156)
        /// trägt Feiertagsregeln und zählt hier nicht mit; eine Waise zählt.
        /// </summary>
        private static long PeriodenOhneVorlagen()
            => Convert.ToInt64(DataRepository.ExecuteScalar(
                   "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID_Kalender\" NOT IN " +
                   "(SELECT \"ID\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Vorlage\" IS NOT NULL) " +
                   "AND \"ID_Kalender\" NOT IN (" + Zonenbestand.KALENDER_ZONENPROJEKTE + ") AND \"ID_Kalender\" NOT IN (" + Konditionierungsbestand.KALENDER_1051 + ")"),    // ohne die Zonenkalender von 1052 (G6d) und die Kalender von 1051 (KP3, RP1)
                               CultureInfo.InvariantCulture);

        /// <summary>Die Matrix eines Probegebäudes mit Wochenendwert und Heizperiode.</summary>
        private static Vorgabematrix Matrix(int? saisonVon = null, int? saisonBis = null)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Wochenende = 16.0;
            Matrixeingang b = Konditionierungseingang.Bestand(g, false, false);
            var vorgaben = new List<Vorgabezeile>();
            if (saisonVon.HasValue)
                vorgaben.Add(new Vorgabezeile
                {
                    IdGebaeude = 1, Groesse = DbWerte.KOND_GROESSE_HEIZSOLL,
                    Zeile = DbWerte.KOND_ZEILE_SAISON, Von = saisonVon, Bis = saisonBis,
                });
            return Vorgabematrix.Bilden(b, vorgaben);
        }

        // =============================================================================
        //  Der Schemaschritt
        // =============================================================================

        [Fact]
        public void Der_Schemaschritt_steht_und_laeuft_ein_zweites_Mal_ohne_Wirkung()
        {
            if (!Bereit()) return;
            Assert.True(KonditionierungSchema.Vollstaendig());
            // Die Kette bis zum Ziel haelt KonditionierungVorlagenSchemaTests (KP-S1v folgt auf 151).
            Assert.True(SchemaStand.Zielversion >= KonditionierungSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + KonditionierungSchema.SCHRITT + ".");

            long kalender = Zaehlen(KonditionierungSchema.TAB_KALENDER);
            long perioden = Zaehlen(KonditionierungSchema.TAB_PERIODE);
            long vorgaben = Zaehlen(KonditionierungSchema.TAB_VORGABE);

            var bericht = new List<string>();
            int angelegt = KonditionierungSchema.Ausfuehren(bericht);
            Assert.Equal(0, angelegt);                        // alles stand bereits
            Assert.True(KonditionierungSchema.Vollstaendig());
            Assert.Equal(kalender, Zaehlen(KonditionierungSchema.TAB_KALENDER));
            Assert.Equal(perioden, Zaehlen(KonditionierungSchema.TAB_PERIODE));
            Assert.Equal(vorgaben, Zaehlen(KonditionierungSchema.TAB_VORGABE));
        }

        [Fact]
        public void Die_drei_Tabellen_sind_STRICT_und_tragen_die_erwarteten_Spalten()
        {
            if (!Bereit()) return;
            foreach (var (tabelle, spalten) in new[]
                     {
                         (KonditionierungSchema.TAB_KALENDER, KonditionierungNutzungSchema.SPALTENZAHL_KALENDER),
                         (KonditionierungSchema.TAB_PERIODE, KonditionierungSchema.SPALTENZAHL_PERIODE),
                         (KonditionierungSchema.TAB_VORGABE, KonditionierungSchema.SPALTENZAHL_VORGABE),
                     })
            {
                long n = Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM pragma_table_info(?)", new DbParam("@t", tabelle)),
                    CultureInfo.InvariantCulture);
                Assert.Equal(spalten, (int)n);

                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@t", tabelle)),
                    CultureInfo.InvariantCulture);
                Assert.Contains("STRICT", ddl, StringComparison.Ordinal);
            }
        }

        // =============================================================================
        //  Anlegen, Verwerfen, Kaskade
        // =============================================================================

        [Fact]
        public void Anlegen_schreibt_Kalender_und_Perioden_und_Verwerfen_nimmt_beides_zurueck()
        {
            if (!Bereit()) return;
            long id = EinGebaeude();
            if (id == 0) return;
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(id);

            // Heizperiode 1.10. bis 30.4.: eine Betriebspause, dazu die Ferien des Probegebaeudes.
            KonditionierungCtrl.Ergebnis r = _ctrl.Anlegen(eigner, Matrix(274, 120),
                                                           Konditionierungsgroesse.Heizsoll);
            Assert.True(r.Ok, r.Meldung);

            Dictionary<Konditionierungsgroesse, Konditionierungskalender> k = _ctrl.Kalender(eigner, out string m);
            Assert.Null(m);
            Assert.True(k.ContainsKey(Konditionierungsgroesse.Heizsoll));
            Konditionierungskalender h = k[Konditionierungsgroesse.Heizsoll];
            Assert.Equal(Angabeart.Woche, h.Grundangabe.Art);     // Wochenendwert 16 Grad -> Woche
            Assert.Single(h.Perioden);                            // die Betriebspause
            Assert.Equal(DbWerte.KOND_ART_BETRIEBSPAUSE, h.Perioden[0].Art);
            Assert.Equal(Standardfahrplan.RANG_SAISON, h.Perioden[0].Rang);
            Assert.True(h.TraegtAus());

            // Zweimal anlegen ersetzt, es entsteht keine zweite Zeile (Eindeutigkeit).
            Assert.True(_ctrl.Anlegen(eigner, Matrix(274, 120), Konditionierungsgroesse.Heizsoll).Ok);
            Assert.Single(_ctrl.Kalender(eigner, out _));

            // Verwerfen nimmt Kalender UND Perioden zurueck (Kaskade).
            Assert.True(_ctrl.Verwerfen(eigner, Konditionierungsgroesse.Heizsoll).Ok);
            Assert.Empty(_ctrl.Kalender(eigner, out _));
            Assert.Equal(0, PeriodenOhneVorlagen());
        }

        [Fact]
        public void Ein_Katalogbau_traegt_eigene_Kalender_und_der_Lauf_liest_sie_nicht()
        {
            if (!Bereit()) return;
            long stamm = EinKatalogbau();
            if (stamm == 0) return;
            var eigner = KonditionierungCtrl.Eigner.Katalogbau(stamm);

            Assert.True(_ctrl.Anlegen(eigner, Matrix(), Konditionierungsgroesse.Heizsoll).Ok);
            Assert.True(_ctrl.Kalender(eigner, out _).ContainsKey(Konditionierungsgroesse.Heizsoll));

            // Der Lauf liest ausschliesslich Projektkalender (Konzept 6): ein Katalogkalender
            // erreicht ihn nicht.
            long geb = EinGebaeude();
            if (geb == 0) return;
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.ID_Gebaeude = (int)geb;
            Konditionierungssatz satz = Konditionierungdatenweg.Satz(
                g, Vdi6007Probe.Wochenende(), 2025, false, false);
            Assert.Null(satz);

            Assert.True(_ctrl.Verwerfen(eigner, Konditionierungsgroesse.Heizsoll).Ok);
        }

        [Fact]
        public void Die_Kaskade_nimmt_Kalender_und_Vorgaben_mit_dem_Gebaeude()
        {
            if (!Bereit()) return;
            // Ein eigenes Gebaeude anlegen, damit die Kaskade ohne Bestandsschaden gemessen wird.
            object p = DataRepository.ExecuteScalar("SELECT MIN(ID_Projekt) FROM \"Tab_Gebaeude\"");
            if (p == null || p == DBNull.Value) return;
            DataRepository.ExecuteNonQuery(
                "INSERT INTO \"Tab_Gebaeude\" (\"ID_Projekt\", \"Raumsolltemperatur_Tag\") VALUES (?, 20)",
                new DbParam("@p", Convert.ToInt32(p, CultureInfo.InvariantCulture)));
            // last_insert_rowid() taugt hier nicht: DataRepository arbeitet je Aufruf auf einer
            // eigenen Verbindung, und die Funktion gilt je Verbindung.
            long id = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT MAX(ID) FROM \"Tab_Gebaeude\""),
                                      CultureInfo.InvariantCulture);
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(id);

            KonditionierungCtrl.Ergebnis a = _ctrl.Anlegen(eigner, Matrix(274, 120),
                                                          Konditionierungsgroesse.Heizsoll);
            Assert.True(a.Ok, "Anlegen für Gebäude " + id + ": " + a.Meldung);
            KonditionierungCtrl.Ergebnis vz = _ctrl.Vorgabe(eigner, Konditionierungsgroesse.Personen,
                                                           DbWerte.KOND_ZEILE_NENNWERT,
                                                           Matrixzelle.AusWert(350.0));
            Assert.True(vz.Ok, vz.Meldung);
            Assert.Single(_ctrl.Kalender(eigner, out _));
            Assert.Single(_ctrl.Vorgaben(eigner));

            DataRepository.ExecuteNonQuery("DELETE FROM \"Tab_Gebaeude\" WHERE \"ID\" = ?", new DbParam("@g", id));
            Assert.Empty(_ctrl.Kalender(eigner, out _));
            Assert.Empty(_ctrl.Vorgaben(eigner));
            Assert.Equal(0, PeriodenOhneVorlagen());
        }

        // =============================================================================
        //  „Erneut anwenden" ersetzt genau den Matrixbereich (P12)
        // =============================================================================

        [Fact]
        public void Erneut_anwenden_ersetzt_den_Matrixbereich_und_laesst_eigene_Perioden_stehen()
        {
            if (!Bereit()) return;
            long id = EinGebaeude();
            if (id == 0) return;
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(id);

            // Angelegter Kalender mit einer EIGENEN Periode (ZEITRAUM) und einem Feiertag.
            Vorgabematrix m1 = Matrix(274, 120);
            Fahrplanlesung l = Standardfahrplan.Erzeugen(m1, Konditionierungsgroesse.Heizsoll, true);
            Assert.Equal(Fahrplanbefund.Erzeugt, l.Befund);
            var perioden = new List<Kalenderregel>(l.Kalender.Perioden)
            {
                Kalenderregel.Zeitraum(500, DbWerte.KOND_ART_ZEITRAUM, "Betriebsurlaub", 200, 210,
                                       Kalenderangabe.AusWert(14.0)),
                Kalenderregel.Feiertag(600, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR,
                                       Kalenderangabe.AusWert(15.0)),
            };
            var eigen = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                                     l.Kalender.Grundangabe, null, perioden);
            Assert.True(_ctrl.Schreiben(eigner, eigen).Ok);
            Assert.Equal(3, _ctrl.Kalender(eigner, out _)[Konditionierungsgroesse.Heizsoll].Perioden.Count);

            // Die Matrix aendern (andere Heizperiode) und erneut anwenden.
            Assert.True(_ctrl.ErneutAnwenden(eigner, Matrix(250, 100), Konditionierungsgroesse.Heizsoll).Ok);
            Konditionierungskalender neu = _ctrl.Kalender(eigner, out _)[Konditionierungsgroesse.Heizsoll];

            // Die eigenen zwei Perioden stehen noch, die Betriebspause ist die NEUE.
            var arten = new List<string>();
            foreach (Kalenderregel r in neu.Perioden) arten.Add(r.Art);
            Assert.Contains(DbWerte.KOND_ART_ZEITRAUM, arten);
            Assert.Contains(DbWerte.KOND_ART_FEIERTAG, arten);
            Assert.Contains(DbWerte.KOND_ART_BETRIEBSPAUSE, arten);
            Assert.Equal(3, neu.Perioden.Count);
            // Die neue Betriebspause laeuft von Tag 101 bis 249 (ausserhalb der Heizperiode 250 … 100).
            Kalenderregel pause = neu.Perioden[0];
            foreach (Kalenderregel r in neu.Perioden)
                if (string.Equals(r.Art, DbWerte.KOND_ART_BETRIEBSPAUSE, StringComparison.Ordinal)) pause = r;
            Assert.Equal(101, pause.Beginn);
            Assert.Equal(249, pause.Ende);

            Assert.True(_ctrl.Verwerfen(eigner, Konditionierungsgroesse.Heizsoll).Ok);
        }

        [Fact]
        public void Erneut_anwenden_ohne_angelegten_Kalender_legt_ihn_an()
        {
            if (!Bereit()) return;
            long id = EinGebaeude();
            if (id == 0) return;
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(id);
            Assert.Empty(_ctrl.Kalender(eigner, out _));
            Assert.True(_ctrl.ErneutAnwenden(eigner, Matrix(), Konditionierungsgroesse.Heizsoll).Ok);
            Assert.Single(_ctrl.Kalender(eigner, out _));
            Assert.True(_ctrl.Verwerfen(eigner, Konditionierungsgroesse.Heizsoll).Ok);
        }

        // =============================================================================
        //  Vorgabezeilen
        // =============================================================================

        [Fact]
        public void Eine_Vorgabezeile_wird_ersetzt_und_eine_leere_Zelle_faellt()
        {
            if (!Bereit()) return;
            long id = EinGebaeude();
            if (id == 0) return;
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(id);

            Assert.True(_ctrl.Vorgabe(eigner, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT,
                                      Matrixzelle.AusWert(2.0, 22, 6, 3.0)).Ok);
            List<Vorgabezeile> v = _ctrl.Vorgaben(eigner);
            Assert.Single(v);
            Assert.Equal(2.0, v[0].Wert);
            Assert.Equal(22, v[0].Von);
            Assert.Equal(6, v[0].Bis);
            Assert.Equal(3.0, v[0].BedingtK);

            // Dieselbe Zeile ersetzt, es entsteht keine zweite.
            Assert.True(_ctrl.Vorgabe(eigner, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT,
                                      Matrixzelle.Abgeschaltet(20, 7)).Ok);
            v = _ctrl.Vorgaben(eigner);
            Assert.Single(v);
            Assert.True(v[0].Aus);
            Assert.Null(v[0].Wert);
            Assert.Equal(20, v[0].Von);

            // Eine in jedem Feld leere Zelle nimmt die Zeile weg.
            Assert.True(_ctrl.Vorgabe(eigner, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT,
                                      Matrixzelle.Leer).Ok);
            Assert.Empty(_ctrl.Vorgaben(eigner));
        }

        [Fact]
        public void Ein_Wert_ausserhalb_der_Grenzen_wird_benannt_abgelehnt()
        {
            if (!Bereit()) return;
            long id = EinGebaeude();
            if (id == 0) return;
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(id);
            Assert.False(_ctrl.Vorgabe(eigner, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_TAG,
                                       Matrixzelle.AusWert(25.0)).Ok);
            Assert.False(_ctrl.Vorgabe(eigner, Konditionierungsgroesse.Geraete, DbWerte.KOND_ZEILE_TAG,
                                       Matrixzelle.AusWert(1.5)).Ok);
            Assert.False(_ctrl.Vorgabe(eigner, Konditionierungsgroesse.Heizsoll, "MITTAG",
                                       Matrixzelle.AusWert(20.0)).Ok);
            Assert.Empty(_ctrl.Vorgaben(eigner));
        }

        // =============================================================================
        //  Die Vorgabezeilen tragen das „aus" einer Bestandszelle (Konzept 5.6)
        // =============================================================================

        [Fact]
        public void Ein_aus_in_der_Vorgabezeile_schlaegt_den_Zahlenwert_der_Bestandsspalte()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            Matrixeingang b = Konditionierungseingang.Bestand(g, false, false);

            Vorgabematrix ohne = Vorgabematrix.Bilden(b, null);
            Assert.True(ohne.Heizsoll.Tag.Belegt);
            Assert.False(ohne.Heizsoll.Tag.Aus);
            Assert.Equal(g.Raumsolltemperatur_Tag, ohne.Heizsoll.Tag.Wert);

            Vorgabematrix mit = Vorgabematrix.Bilden(b, new[]
            {
                new Vorgabezeile
                {
                    IdGebaeude = 1, Groesse = DbWerte.KOND_GROESSE_HEIZSOLL,
                    Zeile = DbWerte.KOND_ZEILE_TAG, Aus = true,
                },
            });
            Assert.True(mit.Heizsoll.Tag.Belegt);
            Assert.True(mit.Heizsoll.Tag.Aus);
        }

        // =============================================================================
        //  Energie bleibt (P1)
        // =============================================================================

        [Fact]
        public void Das_Jahresmittel_der_Gewinne_bleibt_beim_Anlegen_eines_Personenkalenders_gleich()
        {
            const double gesamt = 462.0;
            var personen = new Konditionierungskalender(Konditionierungsgroesse.Personen,
                                                        Kalenderangabe.AusWert(0.5), 350.0, null);
            double mittel = KonditionierungCtrl.PersonenJahresmittelW(personen, 2, 2025);
            Assert.Equal(175.0, mittel, 9);

            double geraete = KonditionierungCtrl.GeraeteNennwertNachPersonen(gesamt, personen, 2, 2025);
            Assert.Equal(gesamt - mittel, geraete, 9);
            // Energieerhaltend: Geraete-Nennwert plus Personenmittel ist der alte Dauerwert.
            Assert.Equal(gesamt, geraete + mittel, 9);
        }

        [Fact]
        public void Der_Personennennwert_kommt_aus_Bewohner_sonst_aus_der_Nutzflaeche()
        {
            Assert.Equal(4.0 * Matrixeingang.PERSON_W,
                         KonditionierungCtrl.PersonenNennwertVorschlag(4.0, 201.0, 40.0), 9);
            Assert.Equal(201.0 / 40.0 * Matrixeingang.PERSON_W,
                         KonditionierungCtrl.PersonenNennwertVorschlag(null, 201.0, 40.0), 9);
            Assert.Equal(0.0, KonditionierungCtrl.PersonenNennwertVorschlag(null, 201.0, null));
        }

        // =============================================================================
        //  Der Datenweg liest, was der Controller schrieb
        // =============================================================================

        [Fact]
        public void Der_Datenweg_liest_den_angelegten_Kalender_des_Projektgebaeudes()
        {
            if (!Bereit()) return;
            long id = EinGebaeude();
            if (id == 0) return;
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(id);
            Assert.True(_ctrl.Anlegen(eigner, Matrix(274, 120), Konditionierungsgroesse.Heizsoll).Ok);

            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.ID_Gebaeude = (int)id;
            Konditionierungssatz satz = Konditionierungdatenweg.Satz(
                g, Vdi6007Probe.Wochenende(), 2025, false, false);
            Assert.NotNull(satz);
            Assert.True(satz.Wirksam);
            Assert.True(satz.Hat(Konditionierungsgroesse.Heizsoll));
            Assert.Equal(153 * 24, satz.StundenOhneHeizung());

            Assert.True(_ctrl.Verwerfen(eigner, Konditionierungsgroesse.Heizsoll).Ok);
            Assert.Null(Konditionierungdatenweg.Satz(g, Vdi6007Probe.Wochenende(), 2025, false, false));
        }
    }
}
