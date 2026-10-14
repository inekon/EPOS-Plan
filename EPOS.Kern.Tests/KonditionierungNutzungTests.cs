using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Konditionierungsnutzung an der Kalenderkopie</b> (Schemaschritt
    /// <see cref="KonditionierungNutzungSchema"/>, Konzept Simulationsablauf Abschnitt 22) — auf einer
    /// Arbeitskopie der Testdatenbank: Nummer, Ziel und Paketstufe; „Vorlage übernehmen" setzt die Nutzung
    /// an der Kopie, Bearbeiten behält sie; die Saat füllt bestehende Kalender aus der Herkunft und ist
    /// wiederholbar; die Vorbelegung der Pufferauslegung bleibt nach Umbenennen und Löschen der Vorlage gleich.
    /// Ohne Testdatenbank schweigen die Fälle.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KonditionierungNutzungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly KonditionierungsvorlageCtrl _ctrl = new KonditionierungsvorlageCtrl();
        private readonly KonditionierungCtrl _kond = new KonditionierungCtrl();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        /// <summary>Das Referenzprojekt ohne Zapfzone und ohne Prozesswärme — die Nutzung entscheidet.</summary>
        private const int PROJEKT = 1007;

        private static bool Bereit()
            => KonditionierungSchema.Lesbar() && KonditionierungVorlagenSchema.Lesbar() &&
               KonditionierungNutzungSchema.SchemaVollstaendig();

        // =====================================================================
        //  Vorrichtung
        // =====================================================================

        private static long Zahl(string sql, params DbParam[] p)
        {
            object o = DataRepository.ExecuteScalar(sql, p);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static string Text(string sql, params DbParam[] p)
        {
            object o = DataRepository.ExecuteScalar(sql, p);
            return o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Das erste Gebäude des Projekts.</summary>
        private static long GebaeudeDesProjekts(int idProjekt)
            => Zahl("SELECT MIN(ID) FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("@p", idProjekt));

        /// <summary>Legt eine eigene Vorlage mit einer Tagzelle und der Nutzung an.</summary>
        private long Vorlage(Konditionierungsgroesse groesse, string name, string nutzung, double tagwert)
        {
            long id = Zahl("SELECT COALESCE(MAX(ID), 0) + 1 FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE + "\"");
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                "\" (\"ID\", \"Groesse\", \"Bezeichner\", \"Nutzung\", \"ReadOnly\") VALUES (?, ?, ?, ?, 0)",
                new DbParam("@id", id), new DbParam("@gr", Konditionierungsgroessen.Kennwort(groesse)),
                new DbParam("@bz", name), new DbParam("@nu", (object)nutzung)));
            KonditionierungCtrl.Ergebnis e = _kond.Vorgabe(KonditionierungCtrl.Eigner.Vorlage(id), groesse,
                                                           DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(tagwert));
            Assert.True(e.Ok, e.Meldung);
            return id;
        }

        private static Vorgabematrix Zielmatrix() => Vorgabematrix.Bilden(new Matrixeingang
        {
            SollTag = 20.0, SollNacht = 18.0, SollWochenende = 16.0, SollFerien = 12.0, NachtBeginn = 22, NachtEnde = 6,
        }, Array.Empty<Vorgabezeile>());

        private static string NutzungAmKalender(long idGebaeude, Konditionierungsgroesse groesse)
            => Text("SELECT \"Nutzung\" FROM \"" + KonditionierungSchema.TAB_KALENDER +
                    "\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL AND \"Groesse\" = ?",
                    new DbParam("@g", idGebaeude), new DbParam("@gr", Konditionierungsgroessen.Kennwort(groesse)));

        // =====================================================================
        //  Schritt, Register, Schema
        // =====================================================================

        [Fact]
        public void Nummer_Ziel_und_Paketstufe()
        {
            Assert.Equal(ProjektkopienKatalogeSchema.SCHRITT + 1, KonditionierungNutzungSchema.SCHRITT);
            Assert.Equal(176, KonditionierungNutzungSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KonditionierungNutzungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl,
                         Paketanhebung.Stufen.Single(x => x.Nr == KonditionierungNutzungSchema.SCHRITT).Wirkung);
            Assert.Equal(12, KonditionierungNutzungSchema.SPALTENZAHL_KALENDER);
        }

        [Fact]
        public void Die_Spalte_steht_mit_Pruefklausel_und_der_Schritt_ist_wiederholbar()
        {
            if (!Bereit()) return;
            Assert.True(KonditionierungNutzungSchema.Vollstaendig());
            Assert.Empty(KonditionierungNutzungSchema.Anweisungen);
            var bericht = new List<string>();
            Assert.Equal(0, KonditionierungNutzungSchema.Ausfuehren(bericht));
            Assert.Equal(0, KonditionierungNutzungSchema.Saat(null));

            string sql = Text("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                              new DbParam("@t", KonditionierungSchema.TAB_KALENDER));
            Assert.Contains("\"Nutzung\" TEXT CHECK", sql);
            Assert.EndsWith("STRICT", sql.TrimEnd());

            // Die Prüfklausel hält einen leeren Wert fern (seit Schritt 189 freier Text, NP-F15).
            long g = GebaeudeDesProjekts(PROJEKT);
            long id = Vorlage(Konditionierungsgroesse.Heizsoll, "Nutzungsprobe", "WOHNEN", 21.0);
            Assert.True(_ctrl.Uebernehmen(id, KonditionierungCtrl.Eigner.Gebaeude(g), Zielmatrix()).Ok);
            Assert.False(DataRepository.ExecuteSQL(
                "UPDATE \"" + KonditionierungSchema.TAB_KALENDER + "\" SET \"Nutzung\" = '' WHERE \"ID_Gebaeude\" = ?",
                new DbParam("@g", g)));
            Assert.Equal("WOHNEN", NutzungAmKalender(g, Konditionierungsgroesse.Heizsoll));
        }

        // =====================================================================
        //  Übernehmen und Bearbeiten
        // =====================================================================

        [Fact]
        public void Uebernehmen_setzt_die_Nutzung_an_der_Kopie_und_Bearbeiten_behaelt_sie()
        {
            if (!Bereit()) return;
            long g = GebaeudeDesProjekts(PROJEKT);
            var ziel = KonditionierungCtrl.Eigner.Gebaeude(g);
            long buero = Vorlage(Konditionierungsgroesse.Heizsoll, "Büro Nutzungsprobe", "BUERO", 21.0);

            KonditionierungCtrl.Ergebnis e = _ctrl.Uebernehmen(buero, ziel, Zielmatrix());
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("BUERO", NutzungAmKalender(g, Konditionierungsgroesse.Heizsoll));

            // Ein zweites Übernehmen derselben Vorlage schreibt ihre (geänderte) Nutzung.
            Assert.True(DataRepository.ExecuteSQL("UPDATE \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                                                  "\" SET \"Nutzung\" = 'SCHULE' WHERE \"ID\" = ?", new DbParam("@id", buero)));
            Assert.True(_ctrl.Uebernehmen(buero, ziel, Zielmatrix()).Ok);
            Assert.Equal("SCHULE", NutzungAmKalender(g, Konditionierungsgroesse.Heizsoll));

            // Eine andere Vorlage ohne Nutzung: die Kopie trägt keine mehr.
            long ohne = Vorlage(Konditionierungsgroesse.Heizsoll, "Ohne Nutzung", null, 19.0);
            Assert.True(_ctrl.Uebernehmen(ohne, ziel, Zielmatrix()).Ok);
            Assert.Null(NutzungAmKalender(g, Konditionierungsgroesse.Heizsoll));

            // Zurück auf die Schulvorlage, dann den Kalender neu schreiben (gleiche Herkunft): die Nutzung bleibt.
            Assert.True(_ctrl.Uebernehmen(buero, ziel, Zielmatrix()).Ok);
            Dictionary<Konditionierungsgroesse, Konditionierungskalender> kalender = _kond.Kalender(ziel, out string m);
            Assert.Null(m);
            string bemerkung = Text("SELECT \"Bemerkung\" FROM \"" + KonditionierungSchema.TAB_KALENDER +
                                    "\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL AND \"Groesse\" = ?",
                                    new DbParam("@g", g), new DbParam("@gr", Konditionierungsgroessen.Kennwort(Konditionierungsgroesse.Heizsoll)));
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.True(KonditionierungCtrl.KalenderSchreiben(v, ziel, kalender[Konditionierungsgroesse.Heizsoll], bemerkung).Ok);
                v.Commit();
            }
            Assert.Equal("SCHULE", NutzungAmKalender(g, Konditionierungsgroesse.Heizsoll));

            // Ohne Herkunft (eigener Kalender) fällt die Nutzung weg.
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.True(KonditionierungCtrl.KalenderSchreiben(v, ziel, kalender[Konditionierungsgroesse.Heizsoll], "eigener Vermerk").Ok);
                v.Commit();
            }
            Assert.Null(NutzungAmKalender(g, Konditionierungsgroesse.Heizsoll));
        }

        // =====================================================================
        //  Saat
        // =====================================================================

        [Fact]
        public void Die_Saat_fuellt_bestehende_Kalender_aus_der_Herkunft_und_ist_wiederholbar()
        {
            if (!Bereit()) return;
            long g = GebaeudeDesProjekts(PROJEKT);
            var ziel = KonditionierungCtrl.Eigner.Gebaeude(g);
            long buero = Vorlage(Konditionierungsgroesse.Heizsoll, "Büro Saatprobe", "BUERO", 21.0);
            Assert.True(_ctrl.Uebernehmen(buero, ziel, Zielmatrix()).Ok);
            long ohneTreffer = Vorlage(Konditionierungsgroesse.Kuehlsoll, "Kühlung Saatprobe", "SCHULE", 26.0);
            Assert.True(_ctrl.Uebernehmen(ohneTreffer, ziel, Zielmatrix()).Ok);

            // Der Stand vor dem Schritt: keine Nutzung an den Kalendern; die zweite Vorlage gibt es nicht mehr.
            DataRepository.ExecuteNonQuery("UPDATE \"" + KonditionierungSchema.TAB_KALENDER + "\" SET \"Nutzung\" = NULL");
            Assert.True(_ctrl.Loeschen(ohneTreffer).Ok);
            Assert.True(KonditionierungNutzungSchema.OffeneSaat() >= 1);
            Assert.False(KonditionierungNutzungSchema.Vollstaendig());

            int n = KonditionierungNutzungSchema.Saat(null);
            Assert.True(n >= 1);
            Assert.Equal("BUERO", NutzungAmKalender(g, Konditionierungsgroesse.Heizsoll));
            Assert.Null(NutzungAmKalender(g, Konditionierungsgroesse.Kuehlsoll));
            Assert.True(KonditionierungNutzungSchema.Vollstaendig());

            // Wiederholbar: nichts mehr offen, eine gesetzte Nutzung bleibt, auch wenn die Vorlage sich ändert.
            Assert.Equal(0, KonditionierungNutzungSchema.Saat(null));
            Assert.True(DataRepository.ExecuteSQL("UPDATE \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                                                  "\" SET \"Nutzung\" = 'SCHULE' WHERE \"ID\" = ?", new DbParam("@id", buero)));
            Assert.Equal(0, KonditionierungNutzungSchema.Ausfuehren(null));
            Assert.Equal("BUERO", NutzungAmKalender(g, Konditionierungsgroesse.Heizsoll));
        }

        /// <summary>Ein Paket ohne Nutzung (älterer Stand) bekommt sie beim Import aus der Herkunft in Bemerkung.</summary>
        [Fact]
        public void Ein_Paketimport_ohne_Nutzung_saet_sie_aus_der_Herkunft()
        {
            if (!Bereit()) return;
            long g = GebaeudeDesProjekts(PROJEKT);
            long buero = Vorlage(Konditionierungsgroesse.Heizsoll, "Büro Paketprobe", "BUERO", 21.0);
            Assert.True(_ctrl.Uebernehmen(buero, KonditionierungCtrl.Eigner.Gebaeude(g), Zielmatrix()).Ok);
            // Das Paket entsteht ohne Nutzung - wie aus einer Datenbank vor dem Schemaschritt.
            DataRepository.ExecuteNonQuery("UPDATE \"" + KonditionierungSchema.TAB_KALENDER + "\" SET \"Nutzung\" = NULL");

            string name = Text("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT));
            string ordner = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                                                   "epos-knutz-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            System.IO.Directory.CreateDirectory(ordner);
            try
            {
                string paket = System.IO.Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(name, paket));
                int neu = io.Importieren(paket, "Transfer ohne Nutzung", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

                long gNeu = GebaeudeDesProjekts(neu);
                Assert.True(gNeu > 0);
                Assert.Equal("BUERO", NutzungAmKalender(gNeu, Konditionierungsgroesse.Heizsoll));
                // Nur die eingespielten Projekte: die Quelle bleibt, wie sie ist.
                Assert.Null(NutzungAmKalender(g, Konditionierungsgroesse.Heizsoll));
                Assert.Equal(PufferNutzungsprofil.BUERO_SCHULE, PufferAuslegungCtrl.Vorbelegen(neu, null).Nutzungsprofil.Profil);
            }
            finally
            {
                try { System.IO.Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        // =====================================================================
        //  Vorbelegung der Pufferauslegung
        // =====================================================================

        [Fact]
        public void Vorbelegung_bleibt_nach_Umbenennen_und_Loeschen_der_Vorlage_gleich()
        {
            if (!Bereit()) return;
            Assert.Equal(PufferNutzungsprofil.WOHNEN, PufferAuslegungCtrl.Vorbelegen(PROJEKT, null).Nutzungsprofil.Profil);

            long g = GebaeudeDesProjekts(PROJEKT);
            long buero = Vorlage(Konditionierungsgroesse.Heizsoll, "Büro Vorbelegung", "BUERO", 21.0);
            Assert.True(_ctrl.Uebernehmen(buero, KonditionierungCtrl.Eigner.Gebaeude(g), Zielmatrix()).Ok);
            Assert.Equal(PufferNutzungsprofil.BUERO_SCHULE, PufferAuslegungCtrl.Vorbelegen(PROJEKT, null).Nutzungsprofil.Profil);

            Assert.True(_ctrl.Umbenennen(buero, "Anderer Name").Ok);
            Assert.Equal(PufferNutzungsprofil.BUERO_SCHULE, PufferAuslegungCtrl.Vorbelegen(PROJEKT, null).Nutzungsprofil.Profil);

            Assert.True(DataRepository.ExecuteSQL("UPDATE \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                                                  "\" SET \"Nutzung\" = 'WOHNEN' WHERE \"ID\" = ?", new DbParam("@id", buero)));
            Assert.Equal(PufferNutzungsprofil.BUERO_SCHULE, PufferAuslegungCtrl.Vorbelegen(PROJEKT, null).Nutzungsprofil.Profil);

            Assert.True(_ctrl.Loeschen(buero).Ok);
            Assert.Equal(PufferNutzungsprofil.BUERO_SCHULE, PufferAuslegungCtrl.Vorbelegen(PROJEKT, null).Nutzungsprofil.Profil);
        }
    }
}
