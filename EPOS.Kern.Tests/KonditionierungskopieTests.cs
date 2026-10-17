using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der EINE Kopierer der Konditionierung</b> (Stufe KP1b, Konzept
    /// Konditionierungsprofile 5.5): Vorgabezeilen, Kalender und Perioden reisen von einem
    /// Eigentümer zum anderen — in EINEM Vorgang, mit der Alt→Neu-Zuordnung über
    /// <c>last_insert_rowid()</c> auf derselben Verbindung.
    ///
    /// <para>Geprüft werden Inhalt, Ersetzen, Größenfilter, der Vorlagenfilter nach E54 und
    /// Konzept 5.7, die zurückbleibenden Zonenzeilen und die Klammer: Ein Rollback lässt
    /// nichts stehen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KonditionierungskopieTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly KonditionierungCtrl _ctrl = new KonditionierungCtrl();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        private bool Bereit() => _db.Vorhanden && KonditionierungSchema.Lesbar();

        // =============================================================================
        //  Alles reist mit
        // =============================================================================

        [Fact]
        public void Vorgaben_Kalender_und_Perioden_reisen_mit()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            long gebaeude = EinGebaeude();
            if (stamm == 0 || gebaeude == 0) return;

            KonditionierungCtrl.Eigner von = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            KonditionierungCtrl.Eigner nach = KonditionierungCtrl.Eigner.Gebaeude(gebaeude);

            VorgabeSchreiben(von, Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_SAISON,
                             null, von: 274, bis: 120);
            VorgabeSchreiben(von, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);
            Assert.True(_ctrl.Schreiben(von, Heizkalender()).Ok);

            Konditionierungskopie.Befund befund = Kopieren(von, nach, Konditionierungskopie.Auswahl.Alles);
            Assert.True(befund.Ok, befund.Meldung);
            Assert.Equal(2, befund.Vorgaben);
            Assert.Equal(1, befund.Kalender);
            Assert.Equal(3, befund.Perioden);

            // Der Inhalt steht am ZIEL und trägt dessen Eigentümer.
            List<Vorgabezeile> zeilen = _ctrl.Vorgaben(nach);
            Assert.Equal(2, zeilen.Count);
            foreach (Vorgabezeile z in zeilen)
            {
                Assert.Equal(gebaeude, z.IdGebaeude);
                Assert.Null(z.IdZone);
                Assert.Null(z.IdGebaeudeStamm);
                Assert.Null(z.IdVorlage);
            }

            Dictionary<Konditionierungsgroesse, Konditionierungskalender> kalender =
                _ctrl.Kalender(nach, out string meldung);
            Assert.Null(meldung);
            Assert.True(kalender.ContainsKey(Konditionierungsgroesse.Heizsoll));
            Konditionierungskalender k = kalender[Konditionierungsgroesse.Heizsoll];
            Assert.Equal(3, k.Perioden.Count);
            Assert.Equal(168, k.Standardwoche.Count);
            Assert.Equal(20.0, k.Standardwoche[0], 9);

            // Die Quelle bleibt, wie sie war.
            Assert.Equal(2, _ctrl.Vorgaben(von).Count);
            Assert.Single(_ctrl.Kalender(von, out _));
        }

        // =============================================================================
        //  Ersetzen und Größenfilter
        // =============================================================================

        [Fact]
        public void Ersetzen_raeumt_die_Zielebene_vorher()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            long gebaeude = EinGebaeude();
            if (stamm == 0 || gebaeude == 0) return;

            KonditionierungCtrl.Eigner von = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            KonditionierungCtrl.Eigner nach = KonditionierungCtrl.Eigner.Gebaeude(gebaeude);

            // Das Ziel trägt schon etwas anderes — dieselbe Größe und Zeile.
            VorgabeSchreiben(nach, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.2);
            VorgabeSchreiben(von, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);

            Konditionierungskopie.Befund befund = Kopieren(von, nach, Konditionierungskopie.Auswahl.Ersetzend);
            Assert.True(befund.Ok, befund.Meldung);

            List<Vorgabezeile> zeilen = _ctrl.Vorgaben(nach);
            Assert.Single(zeilen);
            Assert.Equal(0.8, zeilen[0].Wert.Value, 9);
        }

        [Fact]
        public void Der_Groessenfilter_nimmt_nur_seine_Spalte_mit()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            long gebaeude = EinGebaeude();
            if (stamm == 0 || gebaeude == 0) return;

            KonditionierungCtrl.Eigner von = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            KonditionierungCtrl.Eigner nach = KonditionierungCtrl.Eigner.Gebaeude(gebaeude);

            VorgabeSchreiben(von, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);
            VorgabeSchreiben(von, Konditionierungsgroesse.Personen, DbWerte.KOND_ZEILE_TAG, 0.5);
            Assert.True(_ctrl.Schreiben(von, Heizkalender()).Ok);

            var auswahl = new Konditionierungskopie.Auswahl(
                new[] { Konditionierungsgroesse.Lueftung }, false, false);
            Konditionierungskopie.Befund befund = Kopieren(von, nach, auswahl);
            Assert.True(befund.Ok, befund.Meldung);
            Assert.Equal(1, befund.Vorgaben);
            Assert.Equal(0, befund.Kalender);

            List<Vorgabezeile> zeilen = _ctrl.Vorgaben(nach);
            Assert.Single(zeilen);
            Assert.Equal(DbWerte.KOND_GROESSE_LUEFTUNG, zeilen[0].Groesse);
        }

        // =============================================================================
        //  Der Vorlagenfilter (E54, Konzept 5.7)
        // =============================================================================

        [Fact]
        public void Der_Vorlagenfilter_laesst_Nennwert_Saison_und_Ferien_zurueck()
        {
            if (!Bereit() || !KonditionierungVorlagenSchema.Lesbar()) return;
            long stamm = FreierKatalogbau();
            if (stamm == 0) return;
            long vorlage = VorlageAnlegen("Kopierprobe Heizen", DbWerte.KOND_GROESSE_HEIZSOLL);

            KonditionierungCtrl.Eigner von = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            KonditionierungCtrl.Eigner nach = KonditionierungCtrl.Eigner.Vorlage(vorlage);

            VorgabeSchreiben(von, Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_NACHT, 17.0);
            VorgabeSchreiben(von, Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_SAISON,
                             null, von: 274, bis: 120);
            VorgabeSchreiben(von, Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_NENNWERT, 21.0);
            Assert.True(_ctrl.Schreiben(von, Heizkalender()).Ok);

            Konditionierungskopie.Befund befund = Kopieren(
                von, nach, Konditionierungskopie.Auswahl.FuerVorlage(Konditionierungsgroesse.Heizsoll));
            Assert.True(befund.Ok, befund.Meldung);

            // Nur die Nutzungszeile reist — Nennwert und Saison bleiben beim Ziel (E54).
            List<Vorgabezeile> zeilen = _ctrl.Vorgaben(nach);
            Assert.Single(zeilen);
            Assert.Equal(DbWerte.KOND_ZEILE_NACHT, zeilen[0].Zeile);
            Assert.Equal(vorlage, zeilen[0].IdVorlage);

            // Von den drei Perioden bleibt die eigene; Ferien und Saison reisen nicht (Konzept 3.5).
            Assert.Equal(1, befund.Kalender);
            Assert.Equal(1, befund.Perioden);
            Konditionierungskalender k = _ctrl.Kalender(nach, out _)[Konditionierungsgroesse.Heizsoll];
            Assert.Single(k.Perioden);
            Assert.Equal(DbWerte.KOND_ART_ZEITRAUM, k.Perioden[0].Art);
        }

        [Fact]
        public void Der_Vorlagenfilter_loescht_den_Nennwert_am_Kalender()
        {
            if (!Bereit() || !KonditionierungVorlagenSchema.Lesbar()) return;
            long stamm = FreierKatalogbau();
            if (stamm == 0) return;
            long vorlage = VorlageAnlegen("Kopierprobe Geraete", DbWerte.KOND_GROESSE_GERAETE);

            KonditionierungCtrl.Eigner von = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            KonditionierungCtrl.Eigner nach = KonditionierungCtrl.Eigner.Vorlage(vorlage);

            var kalender = new Konditionierungskalender(Konditionierungsgroesse.Geraete,
                                                        Kalenderangabe.AusWert(0.5), 500.0, null);
            Assert.True(_ctrl.Schreiben(von, kalender).Ok);
            Assert.Equal(500.0, _ctrl.Kalender(von, out _)[Konditionierungsgroesse.Geraete].Nennwert.Value, 9);

            Konditionierungskopie.Befund befund = Kopieren(
                von, nach, Konditionierungskopie.Auswahl.FuerVorlage(Konditionierungsgroesse.Geraete));
            Assert.True(befund.Ok, befund.Meldung);
            Assert.Null(_ctrl.Kalender(nach, out _)[Konditionierungsgroesse.Geraete].Nennwert);
        }

        // =============================================================================
        //  Zonen, gleicher Eigner, Klammer
        // =============================================================================

        [Fact]
        public void Die_Zonenzeilen_bleiben_zurueck_und_werden_gezaehlt()
        {
            if (!Bereit()) return;
            long gebaeude = EinGebaeude();
            long stamm = FreierKatalogbau();
            if (gebaeude == 0 || stamm == 0) return;
            long zone = ZoneAnlegen(gebaeude);

            KonditionierungCtrl.Eigner von = KonditionierungCtrl.Eigner.Gebaeude(gebaeude);
            KonditionierungCtrl.Eigner nach = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            KonditionierungCtrl.Eigner zonenEigner = KonditionierungCtrl.Eigner.Zone(gebaeude, zone);

            VorgabeSchreiben(von, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);
            VorgabeSchreiben(zonenEigner, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.3);

            Konditionierungskopie.Befund befund = Kopieren(von, nach, Konditionierungskopie.Auswahl.Alles);
            Assert.True(befund.Ok, befund.Meldung);
            Assert.Equal(1, befund.Vorgaben);
            Assert.Equal(1, befund.ZonenZurueck);

            // Am Ziel steht nur die Gebäudeebene; die Zonenzeile bleibt, wo sie war.
            Assert.Single(_ctrl.Vorgaben(nach));
            Assert.Single(_ctrl.Vorgaben(zonenEigner));
        }

        [Fact]
        public void Derselbe_Eigner_wird_benannt_abgelehnt()
        {
            if (!Bereit()) return;
            long gebaeude = EinGebaeude();
            if (gebaeude == 0) return;

            KonditionierungCtrl.Eigner e = KonditionierungCtrl.Eigner.Gebaeude(gebaeude);
            Konditionierungskopie.Befund befund = Kopieren(e, KonditionierungCtrl.Eigner.Gebaeude(gebaeude),
                                                           Konditionierungskopie.Auswahl.Alles);
            Assert.False(befund.Ok);
            Assert.False(string.IsNullOrEmpty(befund.Meldung));
        }

        [Fact]
        public void Ein_Rollback_laesst_nichts_zurueck()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            long gebaeude = EinGebaeude();
            if (stamm == 0 || gebaeude == 0) return;

            KonditionierungCtrl.Eigner von = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            KonditionierungCtrl.Eigner nach = KonditionierungCtrl.Eigner.Gebaeude(gebaeude);

            VorgabeSchreiben(von, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);
            Assert.True(_ctrl.Schreiben(von, Heizkalender()).Ok);

            using (DbVorgang v = DataRepository.Vorgang())
            {
                Konditionierungskopie.Befund befund = Konditionierungskopie.Kopieren(
                    v, von, nach, Konditionierungskopie.Auswahl.Alles);
                Assert.True(befund.Ok, befund.Meldung);
                v.Rollback();
            }

            Assert.Empty(_ctrl.Vorgaben(nach));
            Assert.Empty(_ctrl.Kalender(nach, out _));
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        private static Konditionierungskopie.Befund Kopieren(KonditionierungCtrl.Eigner von,
                                                             KonditionierungCtrl.Eigner nach,
                                                             Konditionierungskopie.Auswahl auswahl)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Konditionierungskopie.Befund befund = Konditionierungskopie.Kopieren(v, von, nach, auswahl);
                if (befund.Ok) v.Commit(); else v.Rollback();
                return befund;
            }
        }

        /// <summary>Ein Heizkalender mit Standardwoche, Ferien-, Saison- und eigener Periode.</summary>
        private static Konditionierungskalender Heizkalender()
        {
            var woche = new double[168];
            for (int i = 0; i < woche.Length; i++) woche[i] = 20.0;
            return new Konditionierungskalender(
                Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(woche), null,
                new[]
                {
                    Kalenderregel.Zeitraum(Standardfahrplan.RANG_FERIEN, DbWerte.KOND_ART_FERIEN,
                                           "Ferien 1", 100, 110, Kalenderangabe.AusWert(16.0)),
                    Kalenderregel.Zeitraum(Standardfahrplan.RANG_SAISON, DbWerte.KOND_ART_BETRIEBSPAUSE,
                                           "Heizperiode", 120, 250, Kalenderangabe.Abgeschaltet),
                    Kalenderregel.Zeitraum(400, DbWerte.KOND_ART_ZEITRAUM,
                                           "Eigene Zeile", 200, 210, Kalenderangabe.AusWert(18.0)),
                });
        }

        private static void VorgabeSchreiben(KonditionierungCtrl.Eigner e, Konditionierungsgroesse g,
                                             string zeile, double? wert, int? von = null, int? bis = null)
        {
            var parameter = new List<DbParam>(e.Spaltenwerte("@e"))
            {
                new DbParam("@gr", Konditionierungsgroessen.Kennwort(g)),
                new DbParam("@ze", zeile),
                new DbParam("@we", (object)wert),
                new DbParam("@vo", (object)von),
                new DbParam("@bi", (object)bis),
            };
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE +
                "\" (\"ID_Gebaeude\", \"ID_Zone\", \"ID_Gebaeude_Stamm\", \"ID_Vorlage\", " +
                "\"Groesse\", \"Zeile\", \"Wert\", \"Aus\", \"Von\", \"Bis\") " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, 0, ?, ?)", parameter.ToArray()));
        }

        private static long EinGebaeude() => Id("SELECT MIN(ID) FROM \"Tab_Gebaeude\"");

        private static long FreierKatalogbau()
            => Id("SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\" WHERE \"ReadOnly\" = 0");

        private static long ZoneAnlegen(long idGebaeude)
        {
            DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Zone\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\", \"IstBeheizt\") " +
                "VALUES (?, 1, ?, 1)",
                new DbParam("@g", idGebaeude), new DbParam("@b", "Kopierprobe"));
            return Id("SELECT MAX(ID) FROM \"Tab_Zone\"");
        }

        private static long VorlageAnlegen(string bezeichner, string groesse)
        {
            DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                "\" (\"Groesse\", \"Bezeichner\", \"ReadOnly\") VALUES (?, ?, 0)",
                new DbParam("@g", groesse), new DbParam("@b", bezeichner));
            return Id("SELECT MAX(ID) FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE + "\"");
        }

        private static long Id(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
