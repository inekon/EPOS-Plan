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
    /// <b>Die Datenbankfälle des Vorlagen-Controllers</b> (Stufe KP1b, Konzept
    /// Konditionierungsprofile 3.5 und 5.7): listen je Größe, als Vorlage speichern nach E54,
    /// übernehmen nach P12, die Namensregel, das Schloss, die Löschkaskade und die Werkzeuge der
    /// Karte.
    ///
    /// <para>Die Fälle arbeiten auf einer Arbeitskopie der Testdatenbank
    /// (<see cref="TestDatenbank"/>); ohne sie schweigen sie. Sie pinnen keine Kultur — geprüft
    /// werden Zahlen, Ränge und Kennwörter, keine Ressourcentexte.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KonditionierungsvorlageCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly KonditionierungsvorlageCtrl _ctrl = new KonditionierungsvorlageCtrl();
        private readonly KonditionierungCtrl _kond = new KonditionierungCtrl();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        private static bool Bereit()
            => KonditionierungSchema.Lesbar() && KonditionierungVorlagenSchema.Lesbar();

        // =============================================================================
        //  Vorrichtung
        // =============================================================================

        /// <summary>Das erste Projektgebäude der Testdatenbank.</summary>
        private static long EinGebaeude()
            => Id("SELECT MIN(ID) FROM \"" + Matrixzellenort.TAB_GEBAEUDE + "\"");

        /// <summary>
        /// Setzt am Gebäude die Bestandsfelder, mit denen die Proben rechnen — vier Sollwerte, das
        /// Nachtfenster 22–6 Uhr, den Ferienmerker und einen Ferienzeitraum (Tag 200 … 210).
        /// </summary>
        private static void GebaeudeSetzen(long id)
            => Assert.True(DataRepository.ExecuteSQL(
                "UPDATE \"" + Matrixzellenort.TAB_GEBAEUDE + "\" SET \"Raumsolltemperatur_Tag\" = 20, " +
                "\"Raumsolltemperatur_Nachtabsenkung\" = 18, \"Raumsolltemperatur_Wochenende\" = 16, " +
                "\"Raumsolltemperatur_Ferien\" = 12, \"Ferien\" = 1, \"" +
                Matrixzellenort.SPALTE_NACHT_BEGINN + "\" = 22, \"" +
                Matrixzellenort.SPALTE_NACHT_ENDE + "\" = 6, \"Ferienbeginn_1\" = 200, " +
                "\"Ferienende_1\" = 210, \"Interne_Waermegewinne\" = 500 WHERE \"ID\" = ?",
                new DbParam("@id", id)));

        /// <summary>Der Bestandseingang, der zu <see cref="GebaeudeSetzen"/> gehört.</summary>
        private static Matrixeingang Bestand()
        {
            var b = new Matrixeingang
            {
                SollTag = 20.0,
                SollNacht = 18.0,
                SollWochenende = 16.0,
                SollFerien = 12.0,
                Ferienmerker = 1.0,
                NachtBeginn = 22,
                NachtEnde = 6,
                InterneWaermegewinne = 500.0,
            };
            b.Ferienbeginn[0] = 200;
            b.Ferienende[0] = 210;
            return b;
        }

        /// <summary>Die wirksame Matrix des Ziels — der Eingang samt den übergebenen Vorgabezeilen.</summary>
        private static Vorgabematrix Zielmatrix(params Vorgabezeile[] vorgaben)
            => Vorgabematrix.Bilden(Bestand(), vorgaben);

        private static Vorgabezeile Saisonzeile(Konditionierungsgroesse groesse, int von, int bis)
            => new Vorgabezeile
            {
                Groesse = Konditionierungsgroessen.Kennwort(groesse),
                Zeile = DbWerte.KOND_ZEILE_SAISON,
                Von = von,
                Bis = bis,
            };

        private static long Id(string sql, params DbParam[] p)
        {
            object o = DataRepository.ExecuteScalar(sql, p);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static long Zaehlen(string sql, params DbParam[] p) => Id(sql, p);

        /// <summary>Die Vorgabezeilen einer Vorlage, nach Zeile abrufbar.</summary>
        private Dictionary<string, Vorgabezeile> Vorlagenzeilen(long idVorlage)
        {
            var ziel = new Dictionary<string, Vorgabezeile>(StringComparer.Ordinal);
            foreach (Vorgabezeile z in _kond.Vorgaben(KonditionierungCtrl.Eigner.Vorlage(idVorlage)))
                ziel[z.Zeile] = z;
            return ziel;
        }

        /// <summary>Legt eine eigene Vorlage mit einer Tagzelle an — ohne Umweg über ein Gebäude.</summary>
        private long VorlageMitTagwert(Konditionierungsgroesse groesse, string name, double tagwert)
        {
            long id = Id("SELECT COALESCE(MAX(ID), 0) + 1 FROM \"" +
                         KonditionierungVorlagenSchema.TAB_VORLAGE + "\"");
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                "\" (\"ID\", \"Groesse\", \"Bezeichner\", \"ReadOnly\") VALUES (?, ?, ?, 0)",
                new DbParam("@id", id),
                new DbParam("@gr", Konditionierungsgroessen.Kennwort(groesse)),
                new DbParam("@bz", name)));

            KonditionierungCtrl.Ergebnis e = _kond.Vorgabe(
                KonditionierungCtrl.Eigner.Vorlage(id), groesse, DbWerte.KOND_ZEILE_TAG,
                Matrixzelle.AusWert(tagwert));
            Assert.True(e.Ok, e.Meldung);
            return id;
        }

        private static void SchlossSetzen(long idVorlage)
            => Assert.True(DataRepository.ExecuteSQL(
                "UPDATE \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                "\" SET \"ReadOnly\" = 1 WHERE \"ID\" = ?", new DbParam("@id", idVorlage)));

        // =============================================================================
        //  Listen je Größe
        // =============================================================================

        [Fact]
        public void Die_Liste_zeigt_je_Groesse_die_ausgelieferten_zuerst_dann_nach_Name()
        {
            if (!Bereit()) return;
            long eigen = VorlageMitTagwert(Konditionierungsgroesse.Heizsoll, "Aaa eigen", 20.0);
            long ausgeliefert = VorlageMitTagwert(Konditionierungsgroesse.Heizsoll, "Zzz geliefert", 21.0);
            long andere = VorlageMitTagwert(Konditionierungsgroesse.Kuehlsoll, "Aaa eigen", 26.0);
            SchlossSetzen(ausgeliefert);

            List<KonditionierungsvorlageCtrl.Vorlage> heizen =
                _ctrl.Liste(Konditionierungsgroesse.Heizsoll);
            int iAusgeliefert = heizen.FindIndex(v => v.Id == ausgeliefert);
            int iEigen = heizen.FindIndex(v => v.Id == eigen);
            Assert.True(iAusgeliefert >= 0 && iEigen >= 0);
            Assert.True(iAusgeliefert < iEigen, "Die ausgelieferte Vorlage steht nicht zuerst.");
            Assert.True(heizen[iAusgeliefert].Ausgeliefert);
            Assert.False(heizen[iEigen].Ausgeliefert);
            Assert.DoesNotContain(heizen, v => v.Id == andere);

            List<KonditionierungsvorlageCtrl.Vorlage> kuehlen =
                _ctrl.Liste(Konditionierungsgroesse.Kuehlsoll);
            Assert.Contains(kuehlen, v => v.Id == andere);
            Assert.DoesNotContain(kuehlen, v => v.Id == eigen);
            foreach (KonditionierungsvorlageCtrl.Vorlage v in kuehlen)
                Assert.Equal(Konditionierungsgroesse.Kuehlsoll, v.Groesse);
        }

        // =============================================================================
        //  Als Vorlage speichern (E54)
        // =============================================================================

        [Fact]
        public void Speichern_nimmt_die_Nutzungszeilen_mit_und_laesst_Nennwert_Saison_und_Ferien_zurueck()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(g);

            // Die Quelle traegt eine Saisonzeile und einen angelegten Kalender samt Ferien- und
            // Saisonperiode - beides gehoert dem Objekt und darf nicht mitreisen (E54).
            KonditionierungCtrl.Ergebnis s = _kond.Vorgabe(eigner, Konditionierungsgroesse.Heizsoll,
                DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(250, 120));
            Assert.True(s.Ok, s.Meldung);
            KonditionierungCtrl.Ergebnis a = _kond.Anlegen(
                eigner, Zielmatrix(Saisonzeile(Konditionierungsgroesse.Heizsoll, 250, 120)),
                Konditionierungsgroesse.Heizsoll);
            Assert.True(a.Ok, a.Meldung);

            KonditionierungCtrl.Ergebnis e = _ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll,
                "Probe Heizen", "Beschreibung", DbWerte.KOND_NUTZUNG_BUERO, out long id);
            Assert.True(e.Ok, e.Meldung);
            Assert.True(id > 0);

            KonditionierungsvorlageCtrl.Vorlage v = _ctrl.Lesen(id);
            Assert.NotNull(v);
            Assert.Equal(Konditionierungsgroesse.Heizsoll, v.Groesse);
            Assert.Equal("Probe Heizen", v.Bezeichner);
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, v.Nutzung);
            Assert.False(v.Ausgeliefert);

            // Die vier Nutzungszeilen - ihre Werte kommen aus den BESTANDSSPALTEN der Quelle
            // (eine Vorlage fuehrt keine, Konzept 5.7).
            Dictionary<string, Vorgabezeile> zeilen = Vorlagenzeilen(id);
            Assert.Equal(20.0, zeilen[DbWerte.KOND_ZEILE_TAG].Wert);
            Assert.Equal(18.0, zeilen[DbWerte.KOND_ZEILE_NACHT].Wert);
            Assert.Equal(16.0, zeilen[DbWerte.KOND_ZEILE_WOCHENENDE].Wert);
            Assert.Equal(12.0, zeilen[DbWerte.KOND_ZEILE_FERIEN].Wert);

            // Nacht MIT Zeiten (E54) - sie stehen an der Quelle in Nachtabsenkung_Beginn/_Ende.
            Assert.Equal(22, zeilen[DbWerte.KOND_ZEILE_NACHT].Von);
            Assert.Equal(6, zeilen[DbWerte.KOND_ZEILE_NACHT].Bis);

            // Weder Nennwert noch Saison (E54).
            Assert.False(zeilen.ContainsKey(DbWerte.KOND_ZEILE_NENNWERT));
            Assert.False(zeilen.ContainsKey(DbWerte.KOND_ZEILE_SAISON));

            // Der Kalender der Vorlage: ohne Nennwert, ohne Ferien- und Saisonperiode.
            Assert.Equal(0, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER +
                                    "\" WHERE \"ID_Vorlage\" = ? AND \"Nennwert\" IS NOT NULL",
                                    new DbParam("@v", id)));
            Assert.Equal(1, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER +
                                    "\" WHERE \"ID_Vorlage\" = ?", new DbParam("@v", id)));
            Assert.Equal(0, Zaehlen(
                "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p JOIN \"" +
                KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" " +
                "WHERE k.\"ID_Vorlage\" = ? AND p.\"Art\" IN (?, ?)",
                new DbParam("@v", id),
                new DbParam("@a1", DbWerte.KOND_ART_FERIEN),
                new DbParam("@a2", DbWerte.KOND_ART_BETRIEBSPAUSE)));
        }

        [Fact]
        public void Speichern_nimmt_nur_die_eine_Groesse_mit()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(g);

            KonditionierungCtrl.Ergebnis k = _kond.Vorgabe(eigner, Konditionierungsgroesse.Kuehlsoll,
                DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(26.0));
            Assert.True(k.Ok, k.Meldung);

            KonditionierungCtrl.Ergebnis e = _ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll,
                "Nur Heizen", null, null, out long id);
            Assert.True(e.Ok, e.Meldung);

            Assert.Equal(0, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                    "\" WHERE \"ID_Vorlage\" = ? AND \"Groesse\" <> ?",
                                    new DbParam("@v", id),
                                    new DbParam("@gr", DbWerte.KOND_GROESSE_HEIZSOLL)));
        }

        [Fact]
        public void Eine_Vorlage_ist_keine_Quelle_und_kein_Ziel()
        {
            if (!Bereit()) return;
            long id = VorlageMitTagwert(Konditionierungsgroesse.Heizsoll, "Quelle", 20.0);

            KonditionierungCtrl.Ergebnis e = _ctrl.Speichern(
                KonditionierungCtrl.Eigner.Vorlage(id), Konditionierungsgroesse.Heizsoll, "Zweite",
                null, null, out long neu);
            Assert.False(e.Ok);
            Assert.Equal(0, neu);

            e = _ctrl.Uebernehmen(id, KonditionierungCtrl.Eigner.Vorlage(id), Zielmatrix());
            Assert.False(e.Ok);
        }

        // =============================================================================
        //  Die Namensregel (Konzept 5.7)
        // =============================================================================

        [Fact]
        public void Der_Name_ist_je_Groesse_eindeutig_ohne_Unterschied_von_Gross_und_Kleinschreibung()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(g);

            Assert.True(_ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll, "Buero", null, null,
                                        out long erste).Ok);

            // ASCII: das faltet auch der NOCASE-Index - der Controller lehnt VORHER benannt ab.
            KonditionierungCtrl.Ergebnis e = _ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll,
                "BUERO", null, null, out long zweite);
            Assert.False(e.Ok);
            Assert.False(string.IsNullOrEmpty(e.Meldung));
            Assert.Equal(0, zweite);

            // Umlaut: NOCASE faltet ihn NICHT - hier traegt allein OrdinalIgnoreCase. (Nicht "Büro":
            // den Namen belegt in jeder Liste schon die ausgelieferte Vorlage, Schritt 156.)
            Assert.True(_ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll, "Bürotrakt", null, null,
                                        out long dritte).Ok);
            e = _ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll, "BÜROTRAKT", null, null, out long vierte);
            Assert.False(e.Ok);
            Assert.Equal(0, vierte);

            // Dieselbe Regel gegen eine AUSGELIEFERTE Vorlage: "BÜRO" trifft die gesaete "Büro".
            Assert.Contains(_ctrl.Liste(Konditionierungsgroesse.Heizsoll), v => v.Ausgeliefert && v.Bezeichner == "Büro");
            e = _ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll, "BÜRO", null, null, out long gegenGesperrt);
            Assert.False(e.Ok);
            Assert.Equal(0, gegenGesperrt);

            // Derselbe Name in einer anderen Liste ist erlaubt (P11).
            Assert.True(_ctrl.Speichern(eigner, Konditionierungsgroesse.Kuehlsoll, "Buero", null, null,
                                        out long fuenfte).Ok);
            Assert.True(fuenfte > 0 && erste > 0 && dritte > 0);
        }

        [Fact]
        public void Rand_Leerzeichen_werden_getrimmt_und_die_Laenge_wird_geprueft()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(g);

            Assert.True(_ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll, "  Rand  ", null, null,
                                        out long id).Ok);
            Assert.Equal("Rand", _ctrl.Lesen(id).Bezeichner);

            // Der getrimmte Name kollidiert mit dem schon vorhandenen.
            Assert.False(_ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll, "rand ", null, null,
                                         out long _).Ok);

            Assert.False(_ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll, "   ", null, null,
                                         out long _).Ok);
            Assert.False(_ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll,
                new string('x', KonditionierungVorlagenSchema.BEZEICHNER_MAX_ZEICHEN + 1), null, null,
                out long _).Ok);

            // Eine Nutzung, die es nicht gibt, wird benannt abgelehnt.
            Assert.False(_ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll, "Nutzung", null,
                                         "FREMD", out long _).Ok);
        }

        // =============================================================================
        //  Das Schloss und die Löschkaskade
        // =============================================================================

        [Fact]
        public void Eine_ausgelieferte_Vorlage_laesst_sich_nur_duplizieren()
        {
            if (!Bereit()) return;
            long id = VorlageMitTagwert(Konditionierungsgroesse.Heizsoll, "Geliefert", 20.0);
            SchlossSetzen(id);

            KonditionierungCtrl.Ergebnis e = _ctrl.Umbenennen(id, "Neuer Name");
            Assert.False(e.Ok);
            Assert.False(string.IsNullOrEmpty(e.Meldung));

            e = _ctrl.Loeschen(id);
            Assert.False(e.Ok);
            Assert.Equal(1, Zaehlen("SELECT COUNT(*) FROM \"" +
                                    KonditionierungVorlagenSchema.TAB_VORLAGE + "\" WHERE \"ID\" = ?",
                                    new DbParam("@id", id)));

            e = _ctrl.Duplizieren(id, null, out long kopie);
            Assert.True(e.Ok, e.Meldung);
            KonditionierungsvorlageCtrl.Vorlage v = _ctrl.Lesen(kopie);
            Assert.False(v.Ausgeliefert);
            Assert.NotEqual("Geliefert", v.Bezeichner);
            Assert.Equal(Konditionierungsgroesse.Heizsoll, v.Groesse);

            // Der Inhalt reist mit.
            Assert.Equal(1, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                    "\" WHERE \"ID_Vorlage\" = ? AND \"Zeile\" = ?",
                                    new DbParam("@v", kopie),
                                    new DbParam("@z", DbWerte.KOND_ZEILE_TAG)));

            // Zweimal duplizieren gibt zwei verschiedene, eindeutige Namen.
            Assert.True(_ctrl.Duplizieren(id, null, out long kopie2).Ok);
            Assert.NotEqual(v.Bezeichner, _ctrl.Lesen(kopie2).Bezeichner);
        }

        [Fact]
        public void Loeschen_nimmt_Kalender_Perioden_und_Vorgaben_mit_und_beruehrt_kein_Gebaeude()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(g);
            KonditionierungCtrl.Ergebnis a = _kond.Anlegen(eigner, Zielmatrix(),
                                                           Konditionierungsgroesse.Heizsoll);
            Assert.True(a.Ok, a.Meldung);

            Assert.True(_ctrl.Speichern(eigner, Konditionierungsgroesse.Heizsoll, "Wegwerf", null, null,
                                        out long id).Ok);
            Assert.True(Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                "\" WHERE \"ID_Vorlage\" = ?", new DbParam("@v", id)) > 0);
            Assert.Equal(1, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER +
                                    "\" WHERE \"ID_Vorlage\" = ?", new DbParam("@v", id)));

            long gebaeudeKalender = Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER +
                                            "\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", g));
            long gebaeudeVorgaben = Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                            "\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", g));

            KonditionierungCtrl.Ergebnis e = _ctrl.Loeschen(id);
            Assert.True(e.Ok, e.Meldung);

            Assert.Equal(0, Zaehlen("SELECT COUNT(*) FROM \"" +
                                    KonditionierungVorlagenSchema.TAB_VORLAGE + "\" WHERE \"ID\" = ?",
                                    new DbParam("@id", id)));
            Assert.Equal(0, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                    "\" WHERE \"ID_Vorlage\" = ?", new DbParam("@v", id)));
            Assert.Equal(0, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER +
                                    "\" WHERE \"ID_Vorlage\" = ?", new DbParam("@v", id)));
            Assert.Equal(0, Zaehlen(
                "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p LEFT JOIN \"" +
                KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" " +
                "WHERE k.\"ID\" IS NULL"));

            // Das Gebaeude bleibt unberuehrt - uebernommen ist kopiert (Konzept 5.5).
            Assert.Equal(gebaeudeKalender, Zaehlen("SELECT COUNT(*) FROM \"" +
                KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", g)));
            Assert.Equal(gebaeudeVorgaben, Zaehlen("SELECT COUNT(*) FROM \"" +
                KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", g)));
        }

        [Fact]
        public void Umbenennen_haelt_die_Namensregel_und_eine_fehlende_Vorlage_wird_benannt()
        {
            if (!Bereit()) return;
            long erste = VorlageMitTagwert(Konditionierungsgroesse.Heizsoll, "Erste", 20.0);
            long zweite = VorlageMitTagwert(Konditionierungsgroesse.Heizsoll, "Zweite", 21.0);

            Assert.False(_ctrl.Umbenennen(zweite, "erste").Ok);
            Assert.True(_ctrl.Umbenennen(zweite, " Dritte ").Ok);
            Assert.Equal("Dritte", _ctrl.Lesen(zweite).Bezeichner);
            Assert.Equal("Erste", _ctrl.Lesen(erste).Bezeichner);

            KonditionierungCtrl.Ergebnis e = _ctrl.Umbenennen(999999, "Nichts");
            Assert.False(e.Ok);
            Assert.False(string.IsNullOrEmpty(e.Meldung));
            Assert.False(_ctrl.Loeschen(999999).Ok);
        }

        // =============================================================================
        //  Übernehmen (Konzept 3.5, P12)
        // =============================================================================

        [Fact]
        public void Uebernehmen_auf_ein_Ziel_ohne_Kalender_legt_ihn_mit_den_Ferien_des_Ziels_an()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);
            var ziel = KonditionierungCtrl.Eigner.Gebaeude(g);
            long id = VorlageMitTagwert(Konditionierungsgroesse.Heizsoll, "Übernahme", 21.0);

            KonditionierungCtrl.Ergebnis e = _ctrl.Uebernehmen(id, ziel, Zielmatrix());
            Assert.True(e.Ok, e.Meldung);

            // Das Gebäude merkt die übernommene Vorlage (Welle P4c).
            Assert.Equal(id, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT \"" + PufferAuslegungErgaenzungSchema.SPALTE_KONDITIONIERUNGSVORLAGE + "\" FROM \"" +
                Matrixzellenort.TAB_GEBAEUDE + "\" WHERE \"ID\" = ?", new DbParam("@id", g)), CultureInfo.InvariantCulture));

            // Die Tagzelle der Vorlage ist in der BESTANDSSPALTE des Ziels gelandet (Weiche 5.6).
            Assert.Equal(21.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT \"Raumsolltemperatur_Tag\" FROM \"" + Matrixzellenort.TAB_GEBAEUDE +
                "\" WHERE \"ID\" = ?", new DbParam("@id", g)), CultureInfo.InvariantCulture));

            Dictionary<Konditionierungsgroesse, Konditionierungskalender> kalender =
                _kond.Kalender(ziel, out string m);
            Assert.Null(m);
            Assert.True(kalender.ContainsKey(Konditionierungsgroesse.Heizsoll));

            // Die Ferienperiode kommt aus dem ZIEL (Tag 200 … 210), nicht aus der Vorlage.
            bool ferien = false;
            foreach (Kalenderregel r in kalender[Konditionierungsgroesse.Heizsoll].Perioden)
                if (string.Equals(r.Art, DbWerte.KOND_ART_FERIEN, StringComparison.Ordinal))
                {
                    ferien = true;
                    Assert.Equal(200, r.Beginn);
                    Assert.Equal(210, r.Ende);
                }
            Assert.True(ferien, "Der Kalender trägt keine Ferienperiode des Ziels.");

            // Die Herkunft steht als TEXT in Bemerkung, nicht als Id (Festlegung 6).
            string bemerkung = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT \"Bemerkung\" FROM \"" + KonditionierungSchema.TAB_KALENDER +
                "\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL AND \"Groesse\" = ?",
                new DbParam("@g", g), new DbParam("@gr", DbWerte.KOND_GROESSE_HEIZSOLL)),
                CultureInfo.InvariantCulture);
            Assert.False(string.IsNullOrEmpty(bemerkung));
            Assert.Contains("Übernahme", bemerkung, StringComparison.Ordinal);
        }

        [Fact]
        public void Uebernehmen_ersetzt_nur_den_Matrixbereich_und_laesst_eigene_Perioden_stehen()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);
            var ziel = KonditionierungCtrl.Eigner.Gebaeude(g);

            // Das Ziel traegt einen angelegten Kalender mit EIGENER Periode und einer Feiertagsregel.
            var eigene = new[]
            {
                Kalenderregel.Zeitraum(400, DbWerte.KOND_ART_ZEITRAUM, "Brückentag", 100, 101,
                                       Kalenderangabe.AusWert(17.0)),
                Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG, "Neujahr",
                                       DbWerte.KOND_FEIERTAG_NEUJAHR, Kalenderangabe.Abgeschaltet),
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_FERIEN, DbWerte.KOND_ART_FERIEN, "Alt",
                                       10, 20, Kalenderangabe.AusWert(12.0)),
            };
            Assert.True(_kond.Schreiben(ziel, new Konditionierungskalender(
                Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(19.0), null, eigene)).Ok);

            // Die Vorlage bringt eine eigene Periode und DIESELBE Feiertagsregel mit.
            long id = VorlageMitTagwert(Konditionierungsgroesse.Heizsoll, "Mit Perioden", 21.0);
            var ausVorlage = new[]
            {
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN, DbWerte.KOND_ART_ZEITRAUM, "Aktion",
                                       150, 160, Kalenderangabe.AusWert(18.0)),
                Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG, "Neujahr",
                                       DbWerte.KOND_FEIERTAG_NEUJAHR, Kalenderangabe.AlsWochentag(7)),
            };
            Assert.True(_kond.Schreiben(KonditionierungCtrl.Eigner.Vorlage(id),
                new Konditionierungskalender(Konditionierungsgroesse.Heizsoll,
                                             Kalenderangabe.AusWert(21.0), null, ausVorlage)).Ok);

            KonditionierungCtrl.Ergebnis e = _ctrl.Uebernehmen(id, ziel, Zielmatrix());
            Assert.True(e.Ok, e.Meldung);

            Konditionierungskalender neu = _kond.Kalender(ziel, out string m)[Konditionierungsgroesse.Heizsoll];
            Assert.Null(m);

            // Die eigene Periode bleibt SAMT RANG.
            Kalenderregel brueckentag = null;
            int neujahr = 0, ferienAlt = 0, ferienNeu = 0, aktion = 0;
            foreach (Kalenderregel r in neu.Perioden)
            {
                if (string.Equals(r.Bezeichner, "Brückentag", StringComparison.Ordinal)) brueckentag = r;
                if (r.IstFeiertag &&
                    string.Equals(r.Feiertagsregel, DbWerte.KOND_FEIERTAG_NEUJAHR, StringComparison.Ordinal))
                    neujahr++;
                if (string.Equals(r.Art, DbWerte.KOND_ART_FERIEN, StringComparison.Ordinal))
                {
                    if (r.Beginn == 10) ferienAlt++;
                    if (r.Beginn == 200) ferienNeu++;
                }
                if (string.Equals(r.Bezeichner, "Aktion", StringComparison.Ordinal))
                {
                    aktion++;
                    Assert.InRange(r.Rang, Standardfahrplan.RANG_EIGEN, Standardfahrplan.RANG_EIGEN_LETZTER);
                }
            }
            Assert.NotNull(brueckentag);
            Assert.Equal(400, brueckentag.Rang);
            Assert.Equal(1, neujahr);                 // die Feiertagsregel nur EINMAL
            Assert.Equal(0, ferienAlt);               // der Matrixbereich ist ersetzt
            Assert.Equal(1, ferienNeu);               // durch die Ferien des Ziels
            Assert.Equal(1, aktion);                  // die Periode der Vorlage kam dazu
        }

        [Fact]
        public void Eine_Rangkollision_beim_Uebernehmen_wird_benannt_abgelehnt()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);
            var ziel = KonditionierungCtrl.Eigner.Gebaeude(g);

            // Eine EIGENE Periode auf dem Rang, den der Generator den Ferien gibt.
            var eigene = new[]
            {
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_FERIEN, DbWerte.KOND_ART_ZEITRAUM, "Kollision",
                                       100, 101, Kalenderangabe.AusWert(17.0)),
            };
            Assert.True(_kond.Schreiben(ziel, new Konditionierungskalender(
                Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(19.0), null, eigene)).Ok);

            long id = VorlageMitTagwert(Konditionierungsgroesse.Heizsoll, "Kollisionsprobe", 21.0);
            KonditionierungCtrl.Ergebnis e = _ctrl.Uebernehmen(id, ziel, Zielmatrix());
            Assert.False(e.Ok);
            Assert.False(string.IsNullOrEmpty(e.Meldung));

            // Nichts geschrieben: der alte Kalender steht unveraendert.
            Konditionierungskalender alt = _kond.Kalender(ziel, out string m)[Konditionierungsgroesse.Heizsoll];
            Assert.Null(m);
            Assert.Single(alt.Perioden);
            Assert.Equal("Kollision", alt.Perioden[0].Bezeichner);
        }

        [Fact]
        public void Uebernehmen_laesst_Nennwert_und_Saison_des_Ziels_stehen()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);
            var ziel = KonditionierungCtrl.Eigner.Gebaeude(g);

            // Die Saisonzeile der Geraetespalte gehoert dem Ziel und steht in der Datenbank.
            KonditionierungCtrl.Ergebnis s = _kond.Vorgabe(ziel, Konditionierungsgroesse.Geraete,
                DbWerte.KOND_ZEILE_SAISON, Matrixzelle.NurZeiten(100, 200));
            Assert.True(s.Ok, s.Meldung);

            long id = VorlageMitTagwert(Konditionierungsgroesse.Geraete, "Geräte Vorlage", 0.8);
            KonditionierungCtrl.Ergebnis e = _ctrl.Uebernehmen(
                id, ziel, Zielmatrix(Saisonzeile(Konditionierungsgroesse.Geraete, 100, 200)));
            Assert.True(e.Ok, e.Meldung);

            // Der Nennwert des Ziels (Interne_Waermegewinne) ist unberuehrt …
            Assert.Equal(500.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT \"Interne_Waermegewinne\" FROM \"" + Matrixzellenort.TAB_GEBAEUDE +
                "\" WHERE \"ID\" = ?", new DbParam("@id", g)), CultureInfo.InvariantCulture));

            // … und die Saisonzeile steht, wie sie stand.
            Assert.Equal(1, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                    "\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL AND " +
                                    "\"Groesse\" = ? AND \"Zeile\" = ? AND \"Von\" = 100 AND \"Bis\" = 200",
                                    new DbParam("@g", g),
                                    new DbParam("@gr", DbWerte.KOND_GROESSE_GERAETE),
                                    new DbParam("@z", DbWerte.KOND_ZEILE_SAISON)));

            // Der angelegte Kalender traegt den Nennwert des ZIELS.
            Konditionierungskalender k = _kond.Kalender(ziel, out string m)[Konditionierungsgroesse.Geraete];
            Assert.Null(m);
            Assert.Equal(500.0, k.Nennwert);

            // Die Kuehlspalte ist unberuehrt geblieben - uebernommen wird nur EINE Groesse.
            Assert.Equal(0, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                    "\" WHERE \"ID_Gebaeude\" = ? AND \"Groesse\" = ?",
                                    new DbParam("@g", g),
                                    new DbParam("@gr", DbWerte.KOND_GROESSE_KUEHLSOLL)));
        }

        [Fact]
        public void Eine_fehlende_Vorlage_und_ein_gesperrtes_Ziel_werden_benannt_abgelehnt()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);

            KonditionierungCtrl.Ergebnis e = _ctrl.Uebernehmen(
                999999, KonditionierungCtrl.Eigner.Gebaeude(g), Zielmatrix());
            Assert.False(e.Ok);
            Assert.False(string.IsNullOrEmpty(e.Meldung));

            long stamm = Id("SELECT MIN(\"ID\") FROM \"" + Matrixzellenort.TAB_KATALOGBAU +
                            "\" WHERE \"ReadOnly\" = 1");
            if (stamm == 0) return;                   // kein gesperrter Katalogbau in der Testdatenbank
            long id = VorlageMitTagwert(Konditionierungsgroesse.Heizsoll, "Gegen das Schloss", 21.0);
            e = _ctrl.Uebernehmen(id, KonditionierungCtrl.Eigner.Katalogbau(stamm), Zielmatrix());
            Assert.False(e.Ok);
        }

        // =============================================================================
        //  Kopieren nach … (Konzept 3.5, 7.4)
        // =============================================================================

        /// <summary>Die Id einer Vorlage über Größe und Namen; 0, wenn es sie nicht gibt.</summary>
        private static long Vorlagenid(Konditionierungsgroesse groesse, string name)
            => Id("SELECT \"ID\" FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                  "\" WHERE \"Groesse\" = ? AND \"Bezeichner\" = ?",
                  new DbParam("@gr", Konditionierungsgroessen.Kennwort(groesse)), new DbParam("@bz", name));

        /// <summary>
        /// Der Inhalt einer Vorlage als Zeilen, wie die Datenbank ihn trägt — Vorgabezeilen, Kalender samt
        /// Bemerkung und Perioden, ohne Ids und ohne Größe: Zwei Vorlagen mit gleichen Zeilen tragen
        /// bitgleich denselben Inhalt.
        /// </summary>
        private static List<string> Inhaltszeilen(long idVorlage)
        {
            var zeilen = new List<string>();
            Hinzu(zeilen, "SELECT \"Zeile\", \"Wert\", \"Aus\", \"Von\", \"Bis\", \"Bedingt_K\" FROM \"" +
                          KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID_Vorlage\" = ? ORDER BY \"Zeile\"", idVorlage);
            Hinzu(zeilen, "SELECT \"Wert\", \"Aus\", \"Woche\", \"Nennwert\", \"Bemerkung\" FROM \"" +
                          KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Vorlage\" = ?", idVorlage);
            Hinzu(zeilen, "SELECT p.\"Rang\", p.\"Art\", p.\"Bezeichner\", p.\"Beginn\", p.\"Ende\", p.\"Feiertagsregel\", " +
                          "p.\"Wert\", p.\"Aus\", p.\"Woche\", p.\"WieWochentag\" FROM \"" + KonditionierungSchema.TAB_PERIODE +
                          "\" p JOIN \"" + KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" " +
                          "WHERE k.\"ID_Vorlage\" = ? ORDER BY p.\"Rang\"", idVorlage);
            return zeilen;
        }

        private static void Hinzu(List<string> zeilen, string sql, long idVorlage)
        {
            DataTable t = DataRepository.GetDataTable(sql, new DbParam("@v", idVorlage));
            Assert.NotNull(t);
            foreach (DataRow r in t.Rows)
            {
                var teile = new List<string>();
                foreach (object o in r.ItemArray)
                    teile.Add(o == null || o == DBNull.Value ? "NULL" : Convert.ToString(o, CultureInfo.InvariantCulture));
                zeilen.Add(string.Join(";", teile));
            }
        }

        /// <summary>Wie viele Vorgabe- und Kalenderzeilen der Vorlage NICHT in <paramref name="groesse"/> stehen.</summary>
        private static long AusserhalbDerGroesse(long idVorlage, Konditionierungsgroesse groesse)
        {
            var p = new[] { new DbParam("@v", idVorlage), new DbParam("@gr", Konditionierungsgroessen.Kennwort(groesse)) };
            return Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                           "\" WHERE \"ID_Vorlage\" = ? AND \"Groesse\" <> ?", p)
                   + Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER +
                             "\" WHERE \"ID_Vorlage\" = ? AND \"Groesse\" <> ?", p);
        }

        [Fact]
        public void Kopieren_nach_Personen_und_zurueck_ist_bitgleich_und_steht_nur_in_der_Zielgroesse()
        {
            if (!Bereit()) return;
            KonditionierungCtrl.Ergebnis s = _ctrl.SpeichernAus(VorlagenkopierregelTests.Geraetevorlage(),
                                                                Konditionierungsgroesse.Geraete, "Werkstatt", "Probe der Kopie",
                                                                DbWerte.KOND_NUTZUNG_SONSTIGE, out long geraete);
            Assert.True(s.Ok, s.Meldung);
            List<string> original = Inhaltszeilen(geraete);
            Assert.Equal(4 + 1 + 11, original.Count);                 // vier Zeilen, der Kalender, elf Perioden

            KonditionierungCtrl.Ergebnis e = _ctrl.KopierenNach(geraete, Konditionierungsgroesse.Personen, " Werkstatt ", null,
                                                                null, out long personen);
            Assert.True(e.Ok, e.Meldung);
            Assert.True(personen > 0);
            Assert.Equal(original, Inhaltszeilen(personen));          // Vorgaben, Woche, Perioden, Feiertagsregeln, Bemerkung
            Assert.Equal(0, AusserhalbDerGroesse(personen, Konditionierungsgroesse.Personen));

            KonditionierungsvorlageCtrl.Vorlage kopf = _ctrl.Lesen(personen);
            Assert.Equal((Konditionierungsgroesse.Personen, "Werkstatt", DbWerte.KOND_NUTZUNG_SONSTIGE, false),
                         (kopf.Groesse, kopf.Bezeichner, kopf.Nutzung, kopf.Ausgeliefert));
            Assert.Equal(Vorlagenkopierregel.Beschreibung("Probe der Kopie", "Werkstatt", Konditionierungsgroesse.Geraete),
                         kopf.Beschreibung);

            // Und zurück - unter einem Namen, den die Geräteliste noch nicht führt.
            Assert.False(_ctrl.KopierenNach(personen, Konditionierungsgroesse.Geraete, "werkstatt", null, null, out _).Ok);
            e = _ctrl.KopierenNach(personen, Konditionierungsgroesse.Geraete, "Werkstatt zurück", null, null, out long zurueck);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(original, Inhaltszeilen(zurueck));
            Assert.Equal(0, AusserhalbDerGroesse(zurueck, Konditionierungsgroesse.Geraete));
            Assert.Equal(original, Inhaltszeilen(geraete));           // die Quelle bleibt, wie sie war
        }

        [Fact]
        public void Heizen_nach_Kuehlen_nimmt_Zeitstruktur_und_Aus_Zeiten_und_setzt_Komfort_und_Absenksollwert()
        {
            if (!Bereit()) return;
            Assert.True(_ctrl.SpeichernAus(VorlagenkopierregelTests.Heizvorlage(rein: true), Konditionierungsgroesse.Heizsoll,
                                           "Werkhalle", null, null, out long heizen).Ok);
            List<string> vorher = Inhaltszeilen(heizen);

            KonditionierungCtrl.Ergebnis e = _ctrl.KopierenNach(heizen, Konditionierungsgroesse.Kuehlsoll, "Werkhalle", 25.5,
                                                                28.0, out long kuehlen);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(vorher, Inhaltszeilen(heizen));
            Assert.Equal(0, AusserhalbDerGroesse(kuehlen, Konditionierungsgroesse.Kuehlsoll));

            // Die Zeilen: der Tagwert (21 °C) wird der Komfortsollwert, jeder Sollwert darunter (Nacht 17, Ferien 16)
            // der Absenksollwert, die Nachtzeiten bleiben, „aus" bleibt „aus".
            Dictionary<string, Vorgabezeile> z = Vorlagenzeilen(kuehlen);
            Assert.Equal(new[] { DbWerte.KOND_ZEILE_FERIEN, DbWerte.KOND_ZEILE_NACHT, DbWerte.KOND_ZEILE_TAG,
                                 DbWerte.KOND_ZEILE_WOCHENENDE },
                         z.Keys.OrderBy(k => k, StringComparer.Ordinal));
            Assert.Equal(((double?)25.5, false, (int?)null, (int?)null), (z[DbWerte.KOND_ZEILE_TAG].Wert, z[DbWerte.KOND_ZEILE_TAG].Aus,
                                                                 z[DbWerte.KOND_ZEILE_TAG].Von, z[DbWerte.KOND_ZEILE_TAG].Bis));
            Assert.Equal(((double?)28.0, false, (int?)18, (int?)7), (z[DbWerte.KOND_ZEILE_NACHT].Wert, z[DbWerte.KOND_ZEILE_NACHT].Aus,
                                                            z[DbWerte.KOND_ZEILE_NACHT].Von, z[DbWerte.KOND_ZEILE_NACHT].Bis));
            Assert.Equal(((double?)null, true), (z[DbWerte.KOND_ZEILE_WOCHENENDE].Wert, z[DbWerte.KOND_ZEILE_WOCHENENDE].Aus));
            Assert.Equal((double?)28.0, z[DbWerte.KOND_ZEILE_FERIEN].Wert);

            // Der Kalender: dieselbe Woche mit 25,5 statt 21 °C und 28 statt 17 °C, „aus" bleibt; die Perioden mit Rang,
            // Art, Tagen und Feiertagsregel, „wie Sonntag" bleibt; kein Nennwert, keine Herkunft der Heizung.
            Dictionary<Konditionierungsgroesse, Konditionierungskalender> kalender =
                _kond.Kalender(KonditionierungCtrl.Eigner.Vorlage(kuehlen), out string m);
            Assert.Null(m);
            Konditionierungskalender k = Assert.Single(kalender).Value;
            Assert.Equal(Konditionierungsgroesse.Kuehlsoll, k.Groesse);
            Assert.Null(k.Nennwert);
            Konditionierungskalender h = VorlagenkopierregelTests.Heizvorlage(rein: true).Kalender(Konditionierungsgroesse.Heizsoll);
            for (int i = 0; i < Kalenderwoche.WOCHENWERTE; i++)
                Assert.Equal(double.IsNaN(h.Standardwoche[i]) ? double.NaN : h.Standardwoche[i] < 21.0 ? 28.0 : 25.5,
                             k.Standardwoche[i]);
            Assert.Equal(h.Perioden.Select(r => (r.Rang, r.Art, r.Bezeichner, r.Beginn, r.Ende, r.Feiertagsregel, r.Angabe.Art,
                                                 r.Angabe.WieWochentag)),
                         k.Perioden.Select(r => (r.Rang, r.Art, r.Bezeichner, r.Beginn, r.Ende, r.Feiertagsregel, r.Angabe.Art,
                                                 r.Angabe.WieWochentag)));
            Assert.Equal(28.0, Assert.Single(k.Perioden, r => r.Angabe.Art == Angabeart.Wert).Angabe.Wert);   // Inventur 15 °C
            Assert.All(Assert.Single(k.Perioden, r => r.Angabe.Art == Angabeart.Woche).Angabe.Woche,
                       w => Assert.True(double.IsNaN(w) || w == 25.5));
            Assert.Equal(0, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_KALENDER +
                                    "\" WHERE \"ID_Vorlage\" = ? AND \"Bemerkung\" IS NOT NULL", new DbParam("@v", kuehlen)));

            // E54: keine Zeilen NENNWERT und SAISON, keine Ferien- oder Saisonperiode.
            Assert.Equal(0, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_VORGABE +
                                    "\" WHERE \"ID_Vorlage\" = ? AND \"Zeile\" IN (?, ?)", new DbParam("@v", kuehlen),
                                    new DbParam("@z1", DbWerte.KOND_ZEILE_NENNWERT), new DbParam("@z2", DbWerte.KOND_ZEILE_SAISON)));
            Assert.Equal(0, Zaehlen("SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p JOIN \"" +
                                    KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" " +
                                    "WHERE k.\"ID_Vorlage\" = ? AND p.\"Art\" IN (?, ?)", new DbParam("@v", kuehlen),
                                    new DbParam("@a1", DbWerte.KOND_ART_FERIEN), new DbParam("@a2", DbWerte.KOND_ART_BETRIEBSPAUSE)));
        }

        [Fact]
        public void Eine_ausgelieferte_Quelle_bleibt_unveraendert_und_die_Kopie_ist_eine_eigene_Vorlage()
        {
            if (!Bereit()) return;
            long buero = Vorlagenid(Konditionierungsgroesse.Heizsoll, "Büro");
            Assert.True(buero > 0);
            List<string> vorher = Inhaltszeilen(buero);

            KonditionierungCtrl.Ergebnis e = _ctrl.KopierenNach(buero, Konditionierungsgroesse.Kuehlsoll, "Büro aus Heizen",
                                                                Vorlagenkopierregel.KOMFORTSOLLWERT_VORGABE,
                                                                Vorlagenkopierregel.ABSENKSOLLWERT_VORGABE, out long kopie);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(vorher, Inhaltszeilen(buero));
            Assert.True(_ctrl.Lesen(buero).Ausgeliefert);

            KonditionierungsvorlageCtrl.Vorlage kopf = _ctrl.Lesen(kopie);
            Assert.False(kopf.Ausgeliefert);
            Assert.Equal(0, Zaehlen("SELECT \"ReadOnly\" FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                                    "\" WHERE \"ID\" = ?", new DbParam("@id", kopie)));
            Assert.Equal((Konditionierungsgroesse.Kuehlsoll, DbWerte.KOND_NUTZUNG_BUERO), (kopf.Groesse, kopf.Nutzung));
            Assert.Equal(Vorlagenkopierregel.Beschreibung(_ctrl.Lesen(buero).Beschreibung, "Büro", Konditionierungsgroesse.Heizsoll),
                         kopf.Beschreibung);

            // Büro heizt Tag 20, Nacht 16 (18–7), Wochenende 16, Ferien 16 und in der Grundangabe 16 - die Kopie
            // kühlt am Tag auf 26 °C, in den Absenkzeiten auf 28 °C; die neun Feiertage bleiben „wie Sonntag".
            Dictionary<string, Vorgabezeile> z = Vorlagenzeilen(kopie);
            Assert.Equal(4, z.Count);
            Assert.Equal(((double?)26.0, false), (z[DbWerte.KOND_ZEILE_TAG].Wert, z[DbWerte.KOND_ZEILE_TAG].Aus));
            foreach (string absenk in new[] { DbWerte.KOND_ZEILE_NACHT, DbWerte.KOND_ZEILE_WOCHENENDE, DbWerte.KOND_ZEILE_FERIEN })
                Assert.Equal(((double?)28.0, false), (z[absenk].Wert, z[absenk].Aus));
            Assert.Equal(((int?)18, (int?)7), (z[DbWerte.KOND_ZEILE_NACHT].Von, z[DbWerte.KOND_ZEILE_NACHT].Bis));
            Konditionierungskalender k = Assert.Single(_kond.Kalender(KonditionierungCtrl.Eigner.Vorlage(kopie), out _)).Value;
            Assert.Equal(28.0, k.Grundangabe.Wert);
            Assert.Equal(9, k.Perioden.Count);
            Assert.All(k.Perioden, r => Assert.Equal((DbWerte.KOND_ART_FEIERTAG, 7), (r.Art, r.Angabe.WieWochentag)));

            // Die Kopie ist eigen: umbenennen und löschen gehen, die Quelle bleibt.
            Assert.True(_ctrl.Umbenennen(kopie, "Kontor Kühlung").Ok);
            Assert.True(_ctrl.Loeschen(kopie).Ok);
            Assert.Equal(vorher, Inhaltszeilen(buero));
        }

        [Fact]
        public void Doppelname_fehlende_Sollwerte_und_fremde_Richtungen_werden_benannt_abgelehnt()
        {
            if (!Bereit()) return;
            long heizBuero = Vorlagenid(Konditionierungsgroesse.Heizsoll, "Büro");
            long kuehlBuero = Vorlagenid(Konditionierungsgroesse.Kuehlsoll, "Büro");
            long lueftBuero = Vorlagenid(Konditionierungsgroesse.Lueftung, "Büro");
            Assert.True(heizBuero > 0 && kuehlBuero > 0 && lueftBuero > 0);
            string anzahl = "SELECT COUNT(*) FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE + "\"";
            long vorher = Zaehlen(anzahl);

            foreach ((long id, Konditionierungsgroesse ziel, string name, double? komfort, double? absenk) in
                     new (long, Konditionierungsgroesse, string, double?, double?)[]
                     {
                         (heizBuero, Konditionierungsgroesse.Kuehlsoll, "BÜRO", 26.0, 28.0),            // Doppelname in der Zielliste
                         (heizBuero, Konditionierungsgroesse.Kuehlsoll, "", 26.0, 28.0),                // kein Name
                         (heizBuero, Konditionierungsgroesse.Kuehlsoll, "Büro Heizung", null, 28.0),    // Komfortsollwert fehlt
                         (heizBuero, Konditionierungsgroesse.Kuehlsoll, "Büro Heizung", 36.0, 28.0),    // außerhalb der Kühlspalte
                         (heizBuero, Konditionierungsgroesse.Kuehlsoll, "Büro Heizung", 26.0, null),    // Absenksollwert fehlt
                         (heizBuero, Konditionierungsgroesse.Kuehlsoll, "Büro Heizung", 26.0, 36.0),    // außerhalb der Kühlspalte
                         (heizBuero, Konditionierungsgroesse.Kuehlsoll, "Büro Heizung", 26.0, 25.0),    // unter dem Komfortsollwert
                         (heizBuero, Konditionierungsgroesse.Heizsoll, "Büro Heizung", 26.0, 28.0),     // dieselbe Größe
                         (heizBuero, Konditionierungsgroesse.Lueftung, "Büro Heizung", 26.0, 28.0),     // keine Richtung
                         (kuehlBuero, Konditionierungsgroesse.Heizsoll, "Büro Kühlung", 20.0, 28.0),    // Kühlen -> Heizen
                         (lueftBuero, Konditionierungsgroesse.Personen, "Büro Lüftung", null, null),    // alles mit Lüftung
                         (999999, Konditionierungsgroesse.Kuehlsoll, "Nichts", 26.0, 28.0),             // die Quelle fehlt
                     })
            {
                KonditionierungCtrl.Ergebnis e = _ctrl.KopierenNach(id, ziel, name, komfort, absenk, out long neu);
                Assert.False(e.Ok, id + " -> " + ziel + " „" + name + "“");
                Assert.False(string.IsNullOrEmpty(e.Meldung));
                Assert.Equal(0, neu);
            }
            Assert.Equal(vorher, Zaehlen(anzahl));
        }

        // =============================================================================
        //  Die Werkzeuge der Karte am Controller
        // =============================================================================

        [Fact]
        public void Das_Zeitfenster_wirkt_auf_den_angelegten_Kalender_und_vermerkt_sich()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(g);

            // Ohne angelegten Kalender gibt es nichts zu aendern.
            KonditionierungCtrl.Ergebnis ohne = _ctrl.Zeitfenster(
                eigner, Konditionierungsgroesse.Heizsoll, new[] { 0 }, 7, 18, 21.0);
            Assert.False(ohne.Ok);

            Assert.True(_kond.Anlegen(eigner, Zielmatrix(), Konditionierungsgroesse.Heizsoll).Ok);
            KonditionierungCtrl.Ergebnis e = _ctrl.Zeitfenster(
                eigner, Konditionierungsgroesse.Heizsoll, new[] { 0 }, 7, 18, 21.0);
            Assert.True(e.Ok, e.Meldung);

            Konditionierungskalender k = _kond.Kalender(eigner, out string m)[Konditionierungsgroesse.Heizsoll];
            Assert.Null(m);
            Assert.NotNull(k.Standardwoche);
            for (int stunde = 7; stunde < 18; stunde++)
                Assert.Equal(21.0, k.Standardwoche[Kalenderwoche.Stelle(0, stunde)]);
            Assert.NotEqual(21.0, k.Standardwoche[Kalenderwoche.Stelle(1, 7)]);

            string bemerkung = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT \"Bemerkung\" FROM \"" + KonditionierungSchema.TAB_KALENDER +
                "\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL AND \"Groesse\" = ?",
                new DbParam("@g", g), new DbParam("@gr", DbWerte.KOND_GROESSE_HEIZSOLL)),
                CultureInfo.InvariantCulture);
            Assert.False(string.IsNullOrEmpty(bemerkung));
        }

        [Fact]
        public void Die_Feiertage_landen_als_neun_Perioden_in_der_Datenbank()
        {
            if (!Bereit()) return;
            long g = EinGebaeude();
            GebaeudeSetzen(g);
            var eigner = KonditionierungCtrl.Eigner.Gebaeude(g);
            Assert.True(_kond.Anlegen(eigner, Zielmatrix(), Konditionierungsgroesse.Heizsoll).Ok);

            Assert.True(_ctrl.Feiertagsregeln(eigner, Konditionierungsgroesse.Heizsoll).Ok);
            long erste = Zaehlen(
                "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p JOIN \"" +
                KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" " +
                "WHERE k.\"ID_Gebaeude\" = ? AND k.\"Groesse\" = ? AND p.\"Art\" = ?",
                new DbParam("@g", g), new DbParam("@gr", DbWerte.KOND_GROESSE_HEIZSOLL),
                new DbParam("@a", DbWerte.KOND_ART_FEIERTAG));
            Assert.Equal(9, erste);

            // Ein zweiter Lauf legt nichts doppelt an.
            Assert.True(_ctrl.Feiertagsregeln(eigner, Konditionierungsgroesse.Heizsoll).Ok);
            Assert.Equal(9, Zaehlen(
                "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p JOIN \"" +
                KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" " +
                "WHERE k.\"ID_Gebaeude\" = ? AND k.\"Groesse\" = ? AND p.\"Art\" = ?",
                new DbParam("@g", g), new DbParam("@gr", DbWerte.KOND_GROESSE_HEIZSOLL),
                new DbParam("@a", DbWerte.KOND_ART_FEIERTAG)));

            // Die Feiertage liegen UNTER den Ferien des Ziels.
            Assert.Equal(0, Zaehlen(
                "SELECT COUNT(*) FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p JOIN \"" +
                KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" " +
                "WHERE k.\"ID_Gebaeude\" = ? AND p.\"Art\" = ? AND p.\"Rang\" >= ?",
                new DbParam("@g", g), new DbParam("@a", DbWerte.KOND_ART_FEIERTAG),
                new DbParam("@r", Standardfahrplan.RANG_FERIEN)));
        }
    }
}
