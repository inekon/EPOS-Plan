using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ferienliste des Betriebskalenders</b> (Anwenderentscheid 17.10.2026; <see cref="Betriebskalender.Ferienliste"/>,
    /// <see cref="BetriebskalenderCtrl"/>): Die Liste ergänzt die vier Spaltenpaare — Prüfung über alle Zeiträume
    /// (gültige Tage, keine Überschneidung, höchstens vier in den Spalten), Tagesarten mit Spalten und Liste, Lesen,
    /// Speichern und Löschen auf einer Kopie der Testdatenbank und „Aus Gebäude übernehmen…“ mit sechs Zeiträumen.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class BetriebskalenderFerienlisteTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Sechs Zeiträume ohne Überschneidung, einer über den Jahreswechsel.</summary>
        private static readonly Ferienzeitraum[] SECHS =
        {
            new Ferienzeitraum(357, 6), new Ferienzeitraum(95, 100), new Ferienzeitraum(145, 150),
            new Ferienzeitraum(213, 226), new Ferienzeitraum(290, 295), new Ferienzeitraum(340, 345),
        };

        private static Betriebskalender MitSechs()
        {
            var k = new Betriebskalender { Bezeichner = "Sechs Zeiträume" };
            k.FerienUebernehmen(SECHS);
            return k;
        }

        // =====================================================================
        //  Modell
        // =====================================================================

        [Fact]
        public void Aufteilen_vier_in_die_Spalten_der_Rest_in_die_Liste()
        {
            Betriebskalender k = MitSechs();
            Assert.Equal(SECHS.Take(4), k.Ferien);
            Assert.Equal(new[] { new Betriebsferien(null, 290, 295), new Betriebsferien(null, 340, 345) }, k.Ferienliste);
            Assert.Equal(SECHS, k.AlleFerien());

            var (spalten, liste) = Betriebskalender.FerienAufteilen(SECHS.Take(3));
            Assert.Equal(3, spalten.Count);
            Assert.Empty(liste);
        }

        [Fact]
        public void Pruefen_sechs_Zeitraeume_gruen_Ueberschneidung_rot()
        {
            Betriebskalender k = MitSechs();
            Assert.Null(k.Pruefen());

            // Ein Listenzeitraum, der in die Weihnachtsferien der ersten Spalte greift (Tag 3 liegt in 357 … 6).
            Betriebskalender ueber = k.Kopie();
            ueber.Ferienliste.Add(new Betriebsferien("Neujahr", 2, 4));
            string grund = ueber.Pruefen();
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, Resource.BKAL_MSG_FERIEN_UEBERSCHNEIDUNG, 1, 7), grund);

            // Zwei Listenzeiträume überschneiden sich untereinander.
            Betriebskalender liste = k.Kopie();
            liste.Ferienliste.Add(new Betriebsferien("A", 300, 310));
            liste.Ferienliste.Add(new Betriebsferien("B", 310, 312));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, Resource.BKAL_MSG_FERIEN_UEBERSCHNEIDUNG, 7, 8),
                         liste.Pruefen());
        }

        [Fact]
        public void Pruefen_Grenzen_von_Spalten_und_Liste()
        {
            Betriebskalender fuenf = MitSechs();
            fuenf.Ferien.Add(new Ferienzeitraum(20, 21));                   // fünf in den Spalten
            Assert.Equal(Resource.BKAL_MSG_FERIEN_ANZAHL, fuenf.Pruefen());

            Betriebskalender tag = MitSechs();
            tag.Ferienliste.Add(new Betriebsferien("X", 366, 2));
            Assert.Equal(Resource.BKAL_MSG_FERIEN_DATUM, tag.Pruefen());

            Betriebskalender name = MitSechs();
            name.Ferienliste.Add(new Betriebsferien(new string('n', 101), 20, 21));
            Assert.Equal(Resource.BKAL_MSG_FERIEN_NAME_LANG, name.Pruefen());
        }

        [Fact]
        public void Tagesarten_mit_sechs_Zeitraeumen()
        {
            Betriebskalender k = MitSechs();
            k.FeiertagWieSonntag = false;
            byte[] arten = k.Tagesarten(new Gemeinjahrkalender(0));
            int erwartet = (365 - 357 + 1 + 6) + 6 + 6 + 14 + 6 + 6;
            Assert.Equal(erwartet, arten.Count(a => a == Betriebskalender.TAG_FERIEN));
            Assert.Equal(Betriebskalender.TAG_FERIEN, arten[291 - 1]);   // Liste, Zeitraum 5
            Assert.Equal(Betriebskalender.TAG_FERIEN, arten[345 - 1]);   // Liste, Zeitraum 6
            Assert.Equal(Betriebskalender.TAG_NORMAL, arten[346 - 1]);

            // Ohne die Liste fehlen genau ihre zwölf Tage.
            Betriebskalender ohne = k.Kopie();
            ohne.Ferienliste.Clear();
            Assert.Equal(erwartet - 12, ohne.Tagesarten(new Gemeinjahrkalender(0)).Count(a => a == Betriebskalender.TAG_FERIEN));
        }

        // =====================================================================
        //  Datenbank
        // =====================================================================

        [Fact]
        public void Speichern_Lesen_Loeschen_der_Liste()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Betriebskalender k = MitSechs();
            k.Ferienliste[0] = new Betriebsferien("  Herbst ", 290, 295);
            int id = BetriebskalenderCtrl.Speichern(k);
            Assert.True(id > 0);

            Betriebskalender gelesen = BetriebskalenderCtrl.Lies(id);
            Assert.Equal(SECHS.Take(4), gelesen.Ferien);
            Assert.Equal(new[] { new Betriebsferien("Herbst", 290, 295), new Betriebsferien(null, 340, 345) }, gelesen.Ferienliste);
            Assert.Equal(gelesen.Ferienliste, BetriebskalenderCtrl.Liste().Single(x => x.ID == id).Ferienliste);
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_Betriebskalenderferien WHERE ID_Betriebskalender = ?", id));
            Assert.Equal("1,2", Text("SELECT group_concat(Reihenfolge) FROM (SELECT Reihenfolge FROM Tab_Betriebskalenderferien " +
                                     "WHERE ID_Betriebskalender = ? ORDER BY Reihenfolge)", id));

            // Ändern ersetzt die Liste.
            gelesen.Ferienliste = new List<Betriebsferien> { new Betriebsferien("Advent", 330, 335) };
            Assert.Equal(id, BetriebskalenderCtrl.Speichern(gelesen));
            Assert.Equal(new[] { new Betriebsferien("Advent", 330, 335) }, BetriebskalenderCtrl.Lies(id).Ferienliste);

            // Ein anderer Kalender behält seine Liste.
            Betriebskalender zweiter = MitSechs();
            zweiter.Bezeichner = "Zweiter";
            int id2 = BetriebskalenderCtrl.Speichern(zweiter);
            Assert.Equal(2, BetriebskalenderCtrl.Lies(id2).Ferienliste.Count);

            // Eine ungültige Liste schreibt nichts.
            Betriebskalender falsch = BetriebskalenderCtrl.Lies(id);
            falsch.Ferienliste.Add(new Betriebsferien("Überlapp", 332, 340));
            Assert.Equal(0, BetriebskalenderCtrl.Speichern(falsch));
            Assert.Single(BetriebskalenderCtrl.Lies(id).Ferienliste);

            // Löschen nimmt die Liste mit.
            Assert.True(BetriebskalenderCtrl.Loeschen(id));
            Assert.Null(BetriebskalenderCtrl.Lies(id));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Betriebskalenderferien WHERE ID_Betriebskalender = ?", id));
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_Betriebskalenderferien WHERE ID_Betriebskalender = ?", id2));
        }

        [Fact]
        public void Aus_Gebaeude_mit_sechs_Zeitraeumen_uebernehmen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden || !KonditionierungSchema.Lesbar() || !Kalendergemeinschaft.SchrittSteht()) return;

            // Das Gebäude von 1051 bekommt sechs Ferienzeiträume: vier Spalten, zwei in der Ferienliste.
            long gebaeude = Gebaeude1051();
            var ctrl = new KonditionierungCtrl();
            Konditionierungsarbeitsstand a = ctrl.ArbeitsstandLesen(gebaeude, null, out string meldung);
            Assert.True(a != null, meldung);
            Konditionierungsschritt s = Kalenderbedienung.FerienlisteSetzen(a, new[]
            {
                new Ferienzeile("Winter", 357, 6), new Ferienzeile("Ostern", 95, 100), new Ferienzeile("Pfingsten", 145, 150),
                new Ferienzeile("Sommer", 213, 226), new Ferienzeile("Herbst", 290, 295), new Ferienzeile("Advent", 340, 345),
            });
            Assert.True(s.Ok, s.Meldung);
            using (DbVorgang v = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
            {
                KonditionierungCtrl.Ergebnis e = ctrl.StandSchreiben(v, KonditionierungCtrl.Eigner.Gebaeude(gebaeude), s.Stand.Gebaeude,
                                                                     true, out _);
                Assert.True(e.Ok, e.Meldung);
                v.Commit();
            }

            BetriebskalenderGebaeudequelle quelle = Assert.Single(BetriebskalenderGebaeudeCtrl.Quellen(Konditionierungsprojekt1051.NEU));
            Assert.Equal(SECHS, quelle.Ferien);

            var k = new Betriebskalender { Bezeichner = "Aus Gebäude", Bundesland = quelle.Feiertagsland };
            k.FerienUebernehmen(quelle.Ferien);
            Assert.Null(k.Pruefen());                                       // kein Fehler bei mehr als vier
            Assert.Equal(4, k.Ferien.Count);
            Assert.Equal(2, k.Ferienliste.Count);

            int id = BetriebskalenderCtrl.Speichern(k);
            Assert.True(id > 0);
            Betriebskalender gelesen = BetriebskalenderCtrl.Lies(id);
            Assert.Equal(SECHS, gelesen.AlleFerien());
            Assert.Equal(k.Tagesarten(new Gemeinjahrkalender(0)), gelesen.Tagesarten(new Gemeinjahrkalender(0)));
        }

        private static long Gebaeude1051()
            => Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT k.ID_Gebaeude FROM Tab_Konditionierungskalender k JOIN Tab_Gebaeude g ON g.ID = k.ID_Gebaeude " +
                "WHERE g.ID_Projekt = ? ORDER BY k.ID_Gebaeude LIMIT 1", new DbParam("@p", Konditionierungsprojekt1051.NEU)),
                CultureInfo.InvariantCulture);

        private static long Zahl(string sql, long id)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, new DbParam("?", id)), CultureInfo.InvariantCulture);

        private static string Text(string sql, long id)
            => Convert.ToString(DataRepository.ExecuteScalar(sql, new DbParam("?", id)), CultureInfo.InvariantCulture);
    }
}
