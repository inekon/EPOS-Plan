using System;
using System.Globalization;
using System.IO;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Namen der Ferienzeiträume 1 bis 4 überstehen jeden Schreibweg der Ferienspalten</b>: Die Namen stehen im
    /// Bezeichner der Spiegelperioden (Rang 200 … 203 des gemeinsamen Kalenders); der Spiegel-Trigger legt sie bei jeder
    /// Datumsänderung einer Ferienspalte neu als „Ferien k" an. Auf einer Arbeitskopie der Testdatenbank bekommt das
    /// Gebäude des Referenzprojekts 1051 den Namen „Winterferien" an Zeitraum 1; danach schreibt jeder Weg die Spalten —
    /// Katalogbau anlegen, aktualisieren und duplizieren, Projektkopie (Gebäudedialog mit und ohne Vorgang), Projekt
    /// duplizieren, Projektpaket hin und zurück, Zapfprofil speichern — und der Name steht. Die Gegenprobe hält das
    /// Trigger-Verhalten fest: ein Schreiben der Spalten ohne Hülle setzt „Ferien 1".
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class FeriennamenBewahrungTests : IDisposable
    {
        private const string NAME = "Winterferien";

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private bool Bereit => _db.Vorhanden && KonditionierungSchema.Lesbar() && Kalendergemeinschaft.SchrittSteht();

        [Fact]
        public void Gegenprobe_ein_Schreiben_ohne_Huelle_setzt_den_Namen_auf_Ferien_1()
        {
            if (!Bereit) return;
            long gebaeude = MitNamen();
            DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Ferienende_1 = 13 WHERE ID = ?", new DbParam("@id", gebaeude));
            Assert.Equal("Ferien 1", Name("ID_Gebaeude", gebaeude));
        }

        [Fact]
        public void Die_Huelle_bewahrt_den_Namen_bei_demselben_Schreiben()
        {
            if (!Bereit) return;
            long gebaeude = MitNamen();
            Assert.True(Kalendergemeinschaft.MitFeriennamen(
                Kalendergemeinschaft.Schluessel.Von(KonditionierungCtrl.Eigner.Gebaeude(gebaeude)),
                () => DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Ferienende_1 = 13 WHERE ID = ?", new DbParam("@id", gebaeude))));
            Assert.Equal(NAME, Name("ID_Gebaeude", gebaeude));
            Assert.Equal(13.0, Zahl("SELECT Ferienende_1 FROM Tab_Gebaeude WHERE ID = ?", gebaeude));
        }

        [Fact]
        public void Projektkopie_ueber_den_Gebaeudedialog_und_ohne_Vorgang_bewahrt_den_Namen()
        {
            if (!Bereit) return;
            long gebaeude = MitNamen();
            GebaeudeModel m = GebaeudeStammCtrl.LiesProjektkopie((int)gebaeude);
            Assert.NotNull(m);
            m.Ferienende_1 = 14;
            KonditionierungCtrl.Ergebnis e = GebaeudeStammCtrl.ProjektkopieSchreiben((int)gebaeude, Konditionierungsprojekt1051.NEU, m, null);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(NAME, Name("ID_Gebaeude", gebaeude));
            Assert.Equal(14.0, Zahl("SELECT Ferienende_1 FROM Tab_Gebaeude WHERE ID = ?", gebaeude));

            m.Ferienende_1 = 15;
            Assert.True(GebaeudeStammCtrl.ProjektkopieUeberschreiben((int)gebaeude, Konditionierungsprojekt1051.NEU, m));
            Assert.Equal(NAME, Name("ID_Gebaeude", gebaeude));
            Assert.Equal(15.0, Zahl("SELECT Ferienende_1 FROM Tab_Gebaeude WHERE ID = ?", gebaeude));
        }

        [Fact]
        public void Katalogbau_anlegen_aktualisieren_und_duplizieren_bewahrt_den_Namen()
        {
            if (!Bereit) return;
            long gebaeude = MitNamen();
            GebaeudeModel m = GebaeudeStammCtrl.LiesProjektkopie((int)gebaeude);
            m.Gebaeudename = "FN Prüfbau " + Guid.NewGuid().ToString("N").Substring(0, 6);
            GebaeudeStammCtrl.Katalogschreibergebnis neu = GebaeudeStammCtrl.KatalogSchreiben(
                m, true, m.Gebaeudename, null, KonditionierungCtrl.Eigner.Gebaeude(gebaeude));
            Assert.True(neu.Ok, neu.Meldung);
            Assert.Equal(NAME, Name("ID_Gebaeude_Stamm", neu.Id));

            GebaeudeModel k = new GebaeudeStammCtrl().Lies(m.Gebaeudename);
            k.Ferienende_1 = 16;
            GebaeudeStammCtrl.Katalogschreibergebnis akt = GebaeudeStammCtrl.KatalogSchreiben(k, false, k.Gebaeudename, null);
            Assert.True(akt.Ok, akt.Meldung);
            Assert.Equal(NAME, Name("ID_Gebaeude_Stamm", neu.Id));
            Assert.Equal(16.0, Zahl("SELECT Ferienende_1 FROM Tab_Gebaeude_STAMM WHERE ID = ?", neu.Id));

            k.Ferienende_1 = 17;
            Assert.True(new GebaeudeStammCtrl().Overwrite(k));                               // ohne Vorgang
            Assert.Equal(NAME, Name("ID_Gebaeude_Stamm", neu.Id));
            Assert.Equal(17.0, Zahl("SELECT Ferienende_1 FROM Tab_Gebaeude_STAMM WHERE ID = ?", neu.Id));

            Katalogkopie.Ergebnis kopie = GebaeudeStammCtrl.Duplizieren(neu.Id, m.Gebaeudename + " Kopie");
            Assert.True(kopie.Ok, kopie.Meldung);
            Assert.Equal(NAME, Name("ID_Gebaeude_Stamm", kopie.Id));
        }

        [Fact]
        public void Projekt_duplizieren_und_Projektpaket_tragen_den_Namen()
        {
            if (!Bereit) return;
            long gebaeude = MitNamen();
            string projekt = Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                new DbParam("@p", Konditionierungsprojekt1051.NEU)), CultureInfo.InvariantCulture);

            int dup = new ProjektDuplizierenCtrl().Duplizieren(projekt, "FN Duplikat");
            Assert.True(dup > 0);
            Assert.Equal(NAME, NameImProjekt(dup));

            string ordner = Path.Combine(Path.GetTempPath(), "epos-fn-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(projekt, paket));
                int neu = io.Importieren(paket, "FN Paket", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Assert.Equal(NAME, NameImProjekt(neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch (IOException) { }
            }
            Assert.Equal(NAME, Name("ID_Gebaeude", gebaeude));
        }

        [Fact]
        public void Zapfprofil_speichern_bewahrt_den_Namen()
        {
            if (!Bereit) return;
            long gebaeude = MitNamen();
            ZapfprofilStand stand = ZapfprofilCtrl.Lies(Konditionierungsprojekt1051.NEU);
            Assert.NotNull(stand);
            ZapfprofilCtrl.Speichern(Konditionierungsprojekt1051.NEU, stand);
            Assert.Equal(NAME, Name("ID_Gebaeude", gebaeude));
        }

        // -----------------------------------------------------------------

        /// <summary>Das Gebäude von 1051 mit „Winterferien" an Zeitraum 1 (10. bis 12. Tag) über den Schreibweg der Kalenderbedienung.</summary>
        private static long MitNamen()
        {
            long gebaeude = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT k.ID_Gebaeude FROM Tab_Konditionierungskalender k JOIN Tab_Gebaeude g ON g.ID = k.ID_Gebaeude " +
                "WHERE g.ID_Projekt = ? ORDER BY k.ID_Gebaeude LIMIT 1", new DbParam("@p", Konditionierungsprojekt1051.NEU)),
                CultureInfo.InvariantCulture);
            var ctrl = new KonditionierungCtrl();
            Konditionierungsarbeitsstand a = ctrl.ArbeitsstandLesen(gebaeude, null, out string meldung);
            Assert.True(a != null, meldung);
            Konditionierungsschritt s = Kalenderbedienung.FerienlisteSetzen(a, new[] { new Ferienzeile(NAME, 10, 12) });
            Assert.True(s.Ok, s.Meldung);
            using (DbVorgang v = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
            {
                KonditionierungCtrl.Ergebnis e = ctrl.StandSchreiben(v, KonditionierungCtrl.Eigner.Gebaeude(gebaeude), s.Stand.Gebaeude, true, out _);
                Assert.True(e.Ok, e.Meldung);
                v.Commit();
            }
            Assert.Equal(NAME, Name("ID_Gebaeude", gebaeude));
            return gebaeude;
        }

        /// <summary>Der Bezeichner der Spiegelperiode 1 (Rang 200) im gemeinsamen Kalender eines Gebäudes oder Katalogbaus.</summary>
        private static string Name(string eignerspalte, long id)
        {
            string zone = eignerspalte == "ID_Gebaeude" ? "ID_Zone" : "ID_Zone_Stamm";
            object o = DataRepository.ExecuteScalar(
                "SELECT p.Bezeichner FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k ON k.ID = p.ID_Kalender " +
                "WHERE k." + eignerspalte + " = ? AND k." + zone + " IS NULL AND k.Groesse = 'ALLE' AND p.Rang = 200",
                new DbParam("@id", id));
            return o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Der Bezeichner der Spiegelperiode 1 am ersten Gebäude eines Projekts.</summary>
        private static string NameImProjekt(int idProjekt)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT p.Bezeichner FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k ON k.ID = p.ID_Kalender " +
                "JOIN Tab_Gebaeude g ON g.ID = k.ID_Gebaeude WHERE g.ID_Projekt = ? AND k.ID_Zone IS NULL AND k.Groesse = 'ALLE' " +
                "AND p.Rang = 200 ORDER BY g.ID LIMIT 1", new DbParam("@p", idProjekt));
            return o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        private static double Zahl(string sql, long id)
            => Convert.ToDouble(DataRepository.ExecuteScalar(sql, new DbParam("@id", id)), CultureInfo.InvariantCulture);
    }
}
