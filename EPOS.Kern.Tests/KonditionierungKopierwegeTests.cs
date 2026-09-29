using System;
using System.Collections.Generic;
using System.Globalization;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kopierwege der Konditionierung</b> (Stufe KP1b, Konzept Konditionierungsprofile 5.5):
    /// Katalog → Projekt (<c>CopyFromStamm</c>), die erneute Übernahme, „Speichern unter"
    /// (Projekt → Katalog und Katalog → Katalog), das Duplizieren eines Katalogbaus — dazu das
    /// <b>Schloss</b>: Ein Katalogbau oder eine Vorlage der Auslieferung lehnt jeden Schreibweg
    /// benannt ab (Befund NB5).
    ///
    /// <para>Jeder Fall arbeitet auf einer Arbeitskopie der Testdatenbank; ohne sie schweigt er.
    /// Er pinnt keine Kultur — geprüft werden Zahlen und Kennwörter, keine Ressourcentexte.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KonditionierungKopierwegeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly KonditionierungCtrl _ctrl = new KonditionierungCtrl();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        private bool Bereit() => _db.Vorhanden && KonditionierungSchema.Lesbar();

        // =============================================================================
        //  Katalog → Projekt und die erneute Übernahme
        // =============================================================================

        [Fact]
        public void CopyFromStamm_nimmt_Matrix_Kalender_und_Perioden_mit()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            long projekt = EinProjekt();
            if (stamm == 0 || projekt == 0) return;

            KonditionierungCtrl.Eigner katalog = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            VorgabeSchreiben(katalog, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);
            VorgabeSchreiben(katalog, Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_SAISON,
                             null, 274, 120);
            Assert.True(_ctrl.Schreiben(katalog, Heizkalender()).Ok);

            int neu = Uebernehmen(stamm, projekt);
            Assert.True(neu > 0);

            KonditionierungCtrl.Eigner gebaeude = KonditionierungCtrl.Eigner.Gebaeude(neu);
            Assert.Equal(2, _ctrl.Vorgaben(gebaeude).Count);
            Dictionary<Konditionierungsgroesse, Konditionierungskalender> kalender =
                _ctrl.Kalender(gebaeude, out string meldung);
            Assert.Null(meldung);
            Assert.Equal(3, kalender[Konditionierungsgroesse.Heizsoll].Perioden.Count);
        }

        [Fact]
        public void Die_erneute_Uebernahme_ersetzt_die_Gebaeudeebene_und_laesst_die_Zonen()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            long projekt = EinProjekt();
            if (stamm == 0 || projekt == 0) return;

            KonditionierungCtrl.Eigner katalog = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            VorgabeSchreiben(katalog, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);

            int neu = Uebernehmen(stamm, projekt);
            Assert.True(neu > 0);

            KonditionierungCtrl.Eigner gebaeude = KonditionierungCtrl.Eigner.Gebaeude(neu);
            long zone = ZoneAnlegen(neu);
            KonditionierungCtrl.Eigner zonenEigner = KonditionierungCtrl.Eigner.Zone(neu, zone);
            VorgabeSchreiben(zonenEigner, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.3);

            // Der Katalogbau ändert sich, das Projektgebäude bekommt eine eigene Zeile dazu.
            DataRepository.ExecuteSQL(
                "UPDATE \"" + KonditionierungSchema.TAB_VORGABE + "\" SET \"Wert\" = 0.9 WHERE " +
                "\"ID_Gebaeude_Stamm\" = ?", new DbParam("@s", stamm));
            VorgabeSchreiben(gebaeude, Konditionierungsgroesse.Personen, DbWerte.KOND_ZEILE_TAG, 0.5);

            Konditionierungskopie.Befund befund = GebaeudeStammCtrl.KonditionierungErneutUebernehmen(neu);
            Assert.True(befund.Ok, befund.Meldung);
            Assert.Equal(1, befund.ZonenZurueck);

            // Die Gebäudeebene ist ersetzt — die eigene Personenzeile ist weg, die Lüftung neu.
            List<Vorgabezeile> zeilen = _ctrl.Vorgaben(gebaeude);
            Assert.Single(zeilen);
            Assert.Equal(DbWerte.KOND_GROESSE_LUEFTUNG, zeilen[0].Groesse);
            Assert.Equal(0.9, zeilen[0].Wert.Value, 9);

            // Die Zone bleibt, wie sie war.
            List<Vorgabezeile> zonenzeilen = _ctrl.Vorgaben(zonenEigner);
            Assert.Single(zonenzeilen);
            Assert.Equal(0.3, zonenzeilen[0].Wert.Value, 9);
        }

        // =============================================================================
        //  „Speichern unter" — Projekt → Katalog und Katalog → Katalog
        // =============================================================================

        [Fact]
        public void Speichern_unter_nimmt_vom_Projektgebaeude_nur_die_Gebaeudeebene_mit()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            long projekt = EinProjekt();
            if (stamm == 0 || projekt == 0) return;

            int neu = Uebernehmen(stamm, projekt);
            Assert.True(neu > 0);

            KonditionierungCtrl.Eigner gebaeude = KonditionierungCtrl.Eigner.Gebaeude(neu);
            VorgabeSchreiben(gebaeude, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);
            Assert.True(_ctrl.Schreiben(gebaeude, Heizkalender()).Ok);

            long zone = ZoneAnlegen(neu);
            VorgabeSchreiben(KonditionierungCtrl.Eigner.Zone(neu, zone),
                             Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_TAG, 0.3);

            GebaeudeStammCtrl.SpeichernUnterErgebnis erg =
                GebaeudeStammCtrl.SpeichernUnter(Kopfsatz(stamm, "D1 Projekt zu Katalog"), gebaeude);
            Assert.True(erg.Ok, erg.Meldung);
            Assert.True(erg.Id > 0);
            Assert.Equal(1, erg.Befund.Vorgaben);
            Assert.Equal(1, erg.Befund.Kalender);
            Assert.Equal(3, erg.Befund.Perioden);
            Assert.Equal(1, erg.Befund.ZonenZurueck);

            KonditionierungCtrl.Eigner ziel = KonditionierungCtrl.Eigner.Katalogbau(erg.Id);
            Assert.Single(_ctrl.Vorgaben(ziel));
            Assert.Equal(3, _ctrl.Kalender(ziel, out _)[Konditionierungsgroesse.Heizsoll].Perioden.Count);
        }

        [Fact]
        public void Speichern_unter_wirkt_im_Katalogmodus_wie_Duplizieren()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            if (stamm == 0) return;

            KonditionierungCtrl.Eigner katalog = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            VorgabeSchreiben(katalog, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);
            Assert.True(_ctrl.Schreiben(katalog, Heizkalender()).Ok);

            GebaeudeStammCtrl.SpeichernUnterErgebnis erg =
                GebaeudeStammCtrl.SpeichernUnter(Kopfsatz(stamm, "D1 Katalog zu Katalog"), katalog);
            Assert.True(erg.Ok, erg.Meldung);

            KonditionierungCtrl.Eigner ziel = KonditionierungCtrl.Eigner.Katalogbau(erg.Id);
            Assert.Single(_ctrl.Vorgaben(ziel));
            Assert.Equal(3, _ctrl.Kalender(ziel, out _)[Konditionierungsgroesse.Heizsoll].Perioden.Count);
            Assert.Equal(0, erg.Befund.ZonenZurueck);
        }

        [Fact]
        public void Ein_gescheiterter_Kopf_laesst_nichts_zurueck()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            if (stamm == 0) return;

            KonditionierungCtrl.Eigner katalog = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            VorgabeSchreiben(katalog, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);

            long vorher = Anzahl("Tab_Gebaeude_STAMM");
            long vorgabenVorher = Anzahl(KonditionierungSchema.TAB_VORGABE);

            // Gebaeude_Modell trägt höchstens 20 Zeichen (CHECK) — der Kopf wird abgewiesen.
            GebaeudeModel kopf = Kopfsatz(stamm, "D1 Kopf faellt");
            kopf.Gebaeude_Modell = new string('X', 30);

            GebaeudeStammCtrl.SpeichernUnterErgebnis erg = GebaeudeStammCtrl.SpeichernUnter(kopf, katalog);
            Assert.False(erg.Ok);
            Assert.Equal(vorher, Anzahl("Tab_Gebaeude_STAMM"));
            Assert.Equal(vorgabenVorher, Anzahl(KonditionierungSchema.TAB_VORGABE));
        }

        // =============================================================================
        //  Katalogbau duplizieren
        // =============================================================================

        [Fact]
        public void Ein_Katalogbau_duplizieren_nimmt_Matrix_Kalender_und_Perioden_mit()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            if (stamm == 0) return;

            KonditionierungCtrl.Eigner katalog = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            VorgabeSchreiben(katalog, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);
            Assert.True(_ctrl.Schreiben(katalog, Heizkalender()).Ok);

            Katalogkopie.Ergebnis erg = GebaeudeStammCtrl.Duplizieren((int)stamm, "D1 Duplikat");
            Assert.True(erg.Ok, erg.Meldung);

            KonditionierungCtrl.Eigner ziel = KonditionierungCtrl.Eigner.Katalogbau(erg.Id);
            Assert.Single(_ctrl.Vorgaben(ziel));
            Assert.Equal(3, _ctrl.Kalender(ziel, out _)[Konditionierungsgroesse.Heizsoll].Perioden.Count);

            // Die Quelle bleibt unberührt.
            Assert.Single(_ctrl.Vorgaben(katalog));
        }

        [Fact]
        public void Ein_Namensfehler_beim_Duplizieren_laesst_keine_halbe_Kopie_zurueck()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            if (stamm == 0) return;

            KonditionierungCtrl.Eigner katalog = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            VorgabeSchreiben(katalog, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);

            long vorherKoepfe = Anzahl("Tab_Gebaeude_STAMM");
            long vorherZeilen = Anzahl(KonditionierungSchema.TAB_VORGABE);

            Katalogkopie.Ergebnis erg = GebaeudeStammCtrl.Duplizieren((int)stamm, "   ");
            Assert.False(erg.Ok);
            Assert.Equal(vorherKoepfe, Anzahl("Tab_Gebaeude_STAMM"));
            Assert.Equal(vorherZeilen, Anzahl(KonditionierungSchema.TAB_VORGABE));
        }

        // =============================================================================
        //  Das Schloss (Befund NB5)
        // =============================================================================

        [Fact]
        public void Ein_gesperrter_Katalogbau_lehnt_jeden_Schreibweg_ab()
        {
            if (!Bereit()) return;
            long gesperrt = Id("SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\" WHERE \"ReadOnly\" = 1");
            if (gesperrt == 0) return;

            KonditionierungCtrl.Eigner e = KonditionierungCtrl.Eigner.Katalogbau(gesperrt);
            Assert.NotNull(KonditionierungCtrl.Schloss(e));
            AlleSchreibwegeAbgelehnt(e);
        }

        [Fact]
        public void Eine_gesperrte_Vorlage_lehnt_jeden_Schreibweg_ab()
        {
            if (!Bereit() || !KonditionierungVorlagenSchema.Lesbar()) return;

            DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                "\" (\"Groesse\", \"Bezeichner\", \"ReadOnly\") VALUES (?, ?, 1)",
                new DbParam("@g", DbWerte.KOND_GROESSE_HEIZSOLL),
                new DbParam("@b", "D1 gesperrte Vorlage"));
            long vorlage = Id("SELECT MAX(ID) FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE + "\"");
            Assert.True(vorlage > 0);

            KonditionierungCtrl.Eigner e = KonditionierungCtrl.Eigner.Vorlage(vorlage);
            Assert.NotNull(KonditionierungCtrl.Schloss(e));
            AlleSchreibwegeAbgelehnt(e);
        }

        [Fact]
        public void Ein_freier_Katalogbau_und_ein_Projektgebaeude_tragen_kein_Schloss()
        {
            if (!Bereit()) return;
            Assert.Null(KonditionierungCtrl.Schloss(KonditionierungCtrl.Eigner.Katalogbau(FreierKatalogbau())));
            Assert.Null(KonditionierungCtrl.Schloss(KonditionierungCtrl.Eigner.Gebaeude(EinGebaeude())));
            Assert.Null(KonditionierungCtrl.Schloss(KonditionierungCtrl.Eigner.Zone(EinGebaeude(), 1)));
        }

        private void AlleSchreibwegeAbgelehnt(KonditionierungCtrl.Eigner e)
        {
            Vorgabematrix matrix = Vorgabematrix.Bilden(new Matrixeingang { SollTag = 20.0 }, null, e.Art);

            Assert.False(_ctrl.Anlegen(e, matrix, Konditionierungsgroesse.Heizsoll).Ok);
            Assert.False(_ctrl.Schreiben(e, Heizkalender()).Ok);
            Assert.False(_ctrl.Verwerfen(e, Konditionierungsgroesse.Heizsoll).Ok);
            Assert.False(_ctrl.ErneutAnwenden(e, matrix, Konditionierungsgroesse.Heizsoll).Ok);
            Assert.False(_ctrl.Vorgabe(e, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT,
                                       Matrixzelle.AusWert(0.8)).Ok);

            Assert.Empty(_ctrl.Vorgaben(e));
            Assert.Empty(_ctrl.Kalender(e, out _));
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        // =============================================================================
        //  Die Hülle bindet den Eigentümer (die Razor-Karte bleibt unberührt)
        // =============================================================================

        [Fact]
        public void Die_Huelle_bindet_im_Katalogmodus_den_Ursprungssatz()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            if (stamm == 0) return;
            string name = Katalogname(stamm);

            KonditionierungCtrl.Eigner katalog = KonditionierungCtrl.Eigner.Katalogbau(stamm);
            VorgabeSchreiben(katalog, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);
            Assert.True(_ctrl.Schreiben(katalog, Heizkalender()).Ok);

            GebaeudeKatalogDaten daten = GebaeudeKatalogHuelle.AusModell(GebaeudeKatalogHuelle.Laden(name));
            daten.Name = "D1 Huelle Katalogmodus";
            GebaeudeKatalogErgebnis erg = GebaeudeKatalogHuelle.Schreiben(daten, true, name);
            Assert.True(erg.Erfolg, erg.Meldung);

            long neu = new GebaeudeStammCtrl().Lies(daten.Name).ID;
            KonditionierungCtrl.Eigner ziel = KonditionierungCtrl.Eigner.Katalogbau(neu);
            Assert.Single(_ctrl.Vorgaben(ziel));
            Assert.Equal(3, _ctrl.Kalender(ziel, out _)[Konditionierungsgroesse.Heizsoll].Perioden.Count);
        }

        [Fact]
        public void Die_Huelle_bindet_im_Projektmodus_das_Projektgebaeude()
        {
            if (!Bereit()) return;
            long stamm = FreierKatalogbau();
            long projekt = EinProjekt();
            if (stamm == 0 || projekt == 0) return;

            int gebaeude = Uebernehmen(stamm, projekt, out long z);
            Assert.True(gebaeude > 0);

            KonditionierungCtrl.Eigner quelle = KonditionierungCtrl.Eigner.Gebaeude(gebaeude);
            VorgabeSchreiben(quelle, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);

            IReadOnlyDictionary<string, object> gaben =
                GebaeudeKatalogHuelle.ProjektGaben((int)projekt, (int)z);
            Assert.NotNull(gaben);

            var speichern = (Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>)gaben["Speichern"];
            var daten = (GebaeudeKatalogDaten)gaben["Daten"];
            string bisher = daten.Name;
            daten.Name = "D1 Huelle Projektmodus";

            GebaeudeKatalogErgebnis erg = speichern(daten, true, bisher);
            Assert.True(erg.Erfolg, erg.Meldung);

            long neu = new GebaeudeStammCtrl().Lies(daten.Name).ID;
            Assert.Single(_ctrl.Vorgaben(KonditionierungCtrl.Eigner.Katalogbau(neu)));
        }

        /// <summary>Katalogbau → Projekt über den einzigen Weg, mit frischer Zuordnungszeile.</summary>
        private static int Uebernehmen(long stamm, long projekt) => Uebernehmen(stamm, projekt, out _);

        /// <summary>Dieselbe Übernahme; <paramref name="z"/> ist die neue Zuordnungszeile.</summary>
        private static int Uebernehmen(long stamm, long projekt, out long z)
        {
            DataRepository.ExecuteSQL(
                "INSERT INTO \"Z_ProjektGebaeude\" (\"ID_Projekt\", \"Wohnflaeche_Waermebedarf\", " +
                "\"Einheit_Waermebedarf_Wohnflaeche\", \"Jahresnutzungsgrad\", \"dezWarmwasserbereitung\") " +
                "VALUES (?, 100.0, ?, 0.9, 0)",
                new DbParam("@p", projekt), new DbParam("@e", "m2"));
            z = Id("SELECT MAX(ID) FROM \"Z_ProjektGebaeude\"");
            return new GebaeudeStammCtrl().CopyFromStamm((int)stamm, Katalogname(stamm),
                                                         (int)projekt, (int)z);
        }

        /// <summary>Der Bezeichner eines Katalogbaus.</summary>
        private static string Katalogname(long stamm)
            => Convert.ToString(DataRepository.ExecuteScalar(
                   "SELECT \"Bezeichner\" FROM \"Tab_Gebaeude_STAMM\" WHERE \"ID\" = ?",
                   new DbParam("@s", stamm)), CultureInfo.InvariantCulture);

        /// <summary>Der Katalogsatz als Kopf für „Speichern unter", unter neuem Namen.</summary>
        private static GebaeudeModel Kopfsatz(long stamm, string name)
        {
            GebaeudeModel m = new GebaeudeStammCtrl().Lies(Katalogname(stamm));
            Assert.NotNull(m);
            m.Gebaeudename = name;
            return m;
        }

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

        private static long ZoneAnlegen(long idGebaeude)
        {
            DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Zone\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\", \"IstBeheizt\") " +
                "VALUES (?, 1, ?, 1)",
                new DbParam("@g", idGebaeude), new DbParam("@b", "Kopierwegprobe"));
            return Id("SELECT MAX(ID) FROM \"Tab_Zone\"");
        }

        private static long EinGebaeude() => Id("SELECT MIN(ID) FROM \"Tab_Gebaeude\"");

        private static long EinProjekt() => Id("SELECT MIN(ID) FROM \"Tab_Projekt\"");

        private static long FreierKatalogbau()
            => Id("SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\" WHERE \"ReadOnly\" = 0");

        private static long Anzahl(string tabelle) => Id("SELECT COUNT(*) FROM \"" + tabelle + "\"");

        private static long Id(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
