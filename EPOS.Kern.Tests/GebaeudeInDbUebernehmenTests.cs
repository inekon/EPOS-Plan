using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„In DB übernehmen"</b> im Gebäudedialog: die Projektkopie eines Projektgebäudes als neuer
    /// Anwendersatz im Katalog (<see cref="GebaeudeStammCtrl.AusProjektUebernehmen"/>) — der Spiegel von
    /// <c>CopyFromStamm</c>.
    ///
    /// <para><b>Geprüft wird:</b> alle 103 Fachspalten wörtlich wie in der Projektkopie, neue Id,
    /// <c>ReadOnly = 0</c>, die Herkunft in der Beschreibung, das Projekt unberührt; die benannten
    /// Absagen (kein Gebäude, Name leer, Name vergeben — ohne Schreiben); der Namensvorschlag mit
    /// Zähler; die Konditionierung der Gebäudeebene reist mit, Zonen und Bauteile bleiben im Projekt
    /// und stehen im Ergebnis und in der Statuszeile der Hülle.</para>
    ///
    /// <para>Jeder Fall arbeitet auf einer Arbeitskopie der Testdatenbank; ohne sie schweigt er. Die
    /// Kultur ist de-DE gepinnt — die Herkunft ist ein Ressourcentext mit Datum.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeInDbUebernehmenTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static readonly DateTime STICHTAG = new DateTime(2026, 10, 2);

        [Fact]
        public void Das_Projektgebaeude_von_1040_steht_spaltengleich_als_Anwendersatz_im_Katalog()
        {
            if (!_db.Vorhanden) return;
            int idZ = Zuordnung(1040);
            Assert.True(idZ > 0);
            DataRow vorher = Projektkopie(idZ);
            long saetzeVorher = Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\"");

            GebaeudeStammCtrl.ProjektuebernahmeErgebnis e =
                GebaeudeStammCtrl.AusProjektUebernehmen(idZ, "  Übernahmeprobe 1040  ", STICHTAG);

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(GebaeudeStammCtrl.Projektuebernahmeabsage.Keine, e.Absage);
            Assert.Equal("Übernahmeprobe 1040", e.Name);
            Assert.True(e.Id > 0);
            Assert.Equal(saetzeVorher + 1, Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\""));

            DataRow neu = Katalogsatz(e.Id);
            Assert.Equal("Übernahmeprobe 1040", Convert.ToString(neu["Bezeichner"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Convert.ToInt64(neu["ReadOnly"], CultureInfo.InvariantCulture));

            // Alle Fachspalten wörtlich - NULL bleibt NULL, Schalter bleiben 0/1.
            string[] spalten = Kopfspalten();
            Assert.Equal(103, spalten.Length);     // Schritt K2: Wochenendtage und Feiertagsland
            foreach (string s in spalten)
                Assert.True(Equals(vorher[s], neu[s]) || (vorher[s] is DBNull && neu[s] is DBNull),
                            "Spalte " + s + ": Projekt " + vorher[s] + ", Katalog " + neu[s]);

            // Die Beschreibung: die der Kopie, darunter die Herkunft.
            string beschreibung = Convert.ToString(neu["Beschreibung"], CultureInfo.InvariantCulture);
            Assert.Equal(Convert.ToString(vorher["Beschreibung"], CultureInfo.InvariantCulture)
                         + "\naus Projekt zwei Puffer je Kanal, 02.10.2026", beschreibung);

            // Das Projekt bleibt unberührt.
            DataRow nachher = Projektkopie(idZ);
            foreach (DataColumn c in vorher.Table.Columns)
                Assert.True(Equals(vorher[c.ColumnName], nachher[c.ColumnName]), "Projektspalte " + c.ColumnName);
        }

        [Fact]
        public void Kein_Gebaeude_leerer_Name_und_Doppelname_werden_benannt_abgelehnt_ohne_zu_schreiben()
        {
            if (!_db.Vorhanden) return;
            int idZ = Zuordnung(1017);
            Assert.True(idZ > 0);
            string vergeben = Convert.ToString(Projektkopie(idZ)["Gebaeudename"], CultureInfo.InvariantCulture);
            Assert.True(GebaeudeStammCtrl.NameVergeben(vergeben));
            long vorher = Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\"");

            GebaeudeStammCtrl.ProjektuebernahmeErgebnis keins = GebaeudeStammCtrl.AusProjektUebernehmen(0, "Probe");
            Assert.False(keins.Ok);
            Assert.Equal(GebaeudeStammCtrl.Projektuebernahmeabsage.KeinGebaeude, keins.Absage);
            Assert.False(keins.AmNamen);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEB_MSG_DB_UEBERNAHME_KEIN_GEBAEUDE, keins.Meldung);
            Assert.Equal(GebaeudeStammCtrl.Projektuebernahmeabsage.KeinGebaeude,
                         GebaeudeStammCtrl.AusProjektUebernehmen(987654, "Probe").Absage);

            GebaeudeStammCtrl.ProjektuebernahmeErgebnis leer = GebaeudeStammCtrl.AusProjektUebernehmen(idZ, "   ");
            Assert.Equal(GebaeudeStammCtrl.Projektuebernahmeabsage.NameLeer, leer.Absage);
            Assert.True(leer.AmNamen);

            GebaeudeStammCtrl.ProjektuebernahmeErgebnis doppelt = GebaeudeStammCtrl.AusProjektUebernehmen(idZ, vergeben);
            Assert.False(doppelt.Ok);
            Assert.Equal(GebaeudeStammCtrl.Projektuebernahmeabsage.NameVergeben, doppelt.Absage);
            Assert.True(doppelt.AmNamen);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.GEBK_MSG_NAME_VERGEBEN, doppelt.Meldung);

            Assert.Equal(vorher, Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\""));

            // Ein zweites Mal unter demselben neuen Namen: beim zweiten Mal vergeben.
            Assert.True(GebaeudeStammCtrl.AusProjektUebernehmen(idZ, "Übernahmeprobe 1017").Ok);
            Assert.Equal(GebaeudeStammCtrl.Projektuebernahmeabsage.NameVergeben,
                         GebaeudeStammCtrl.AusProjektUebernehmen(idZ, "Übernahmeprobe 1017").Absage);
        }

        [Fact]
        public void Der_Namensvorschlag_ist_der_Projektname_und_bei_Doppelnamen_mit_Zaehler_eindeutig()
        {
            if (!_db.Vorhanden) return;
            int idZ = Zuordnung(1017);
            string basis = Convert.ToString(Projektkopie(idZ)["Gebaeudename"], CultureInfo.InvariantCulture);

            // Der Name der Kopie steht im Katalog - der Vorschlag zählt.
            string vorschlag = GebaeudeStammCtrl.NamensvorschlagAusProjekt(idZ);
            Assert.Equal(basis + " (2)", vorschlag);
            Assert.False(GebaeudeStammCtrl.NameVergeben(vorschlag));

            Assert.True(GebaeudeStammCtrl.AusProjektUebernehmen(idZ, vorschlag).Ok);
            Assert.Equal(basis + " (3)", GebaeudeStammCtrl.NamensvorschlagAusProjekt(idZ));

            // Ein freier Name bleibt, wie er ist.
            DataRepository.ExecuteSQL("UPDATE \"Tab_Gebaeude\" SET \"Gebaeudename\" = ? WHERE \"ID_ProjektGebaeude\" = ?",
                                      new DbParam("@n", "Freier Probename"), new DbParam("@z", idZ));
            Assert.Equal("Freier Probename", GebaeudeStammCtrl.NamensvorschlagAusProjekt(idZ));

            Assert.Equal("", GebaeudeStammCtrl.NamensvorschlagAusProjekt(987654));
        }

        [Fact]
        public void Konditionierung_und_Zonen_reisen_mit_die_Projektkopie_behaelt_ihre_Zonen()
        {
            if (!_db.Vorhanden || !KonditionierungSchema.Lesbar()) return;
            int idZ = Zuordnung(1040);
            int idGebaeude = Convert.ToInt32(Projektkopie(idZ)["ID"], CultureInfo.InvariantCulture);

            // Eine Vorgabe der Gebäudeebene und eine Zone mit einem Bauteil an der Projektkopie.
            var vorgabe = new List<DbParam>(KonditionierungCtrl.Eigner.Gebaeude(idGebaeude).Spaltenwerte("@e"))
            {
                new DbParam("@gr", Konditionierungsgroessen.Kennwort(Konditionierungsgroesse.Lueftung)),
                new DbParam("@ze", DbWerte.KOND_ZEILE_NACHT),
                new DbParam("@we", 0.7),
            };
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE +
                "\" (\"ID_Gebaeude\", \"ID_Zone\", \"ID_Gebaeude_Stamm\", \"ID_Vorlage\", " +
                "\"Groesse\", \"Zeile\", \"Wert\", \"Aus\") VALUES (?, ?, ?, ?, ?, ?, ?, 0)", vorgabe.ToArray()));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Zone\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\", \"IstBeheizt\") VALUES (?, 1, ?, 1)",
                new DbParam("@g", idGebaeude), new DbParam("@b", "Übernahmeprobe Zone")));
            long zone = Zahl("SELECT MAX(ID) FROM \"Tab_Zone\"");
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Bauteil\" (\"ID_Zone\", \"Rang\", \"Bezeichner\", \"Bauteilart\", \"Flaeche\") " +
                "VALUES (?, 1, ?, 'AUSSENWAND', 10.0)",
                new DbParam("@z", zone), new DbParam("@b", "Übernahmeprobe Wand")));

            GebaeudeStammCtrl.ProjektuebernahmeErgebnis e =
                GebaeudeStammCtrl.AusProjektUebernehmen(idZ, "Übernahmeprobe Zonen", STICHTAG);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(1, e.Zonen);
            Assert.Equal(1, e.Bauteile);
            Assert.Equal(1, e.Befund.Vorgaben);

            List<Vorgabezeile> imKatalog = new KonditionierungCtrl().Vorgaben(KonditionierungCtrl.Eigner.Katalogbau(e.Id));
            Assert.Single(imKatalog);
            Assert.Equal(0.7, imKatalog[0].Wert.Value, 9);

            // Die Zone bleibt an der Projektkopie, und der Katalogsatz traegt ihre Kopie (Schritt ZK).
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = " + idGebaeude));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteil\" WHERE \"ID_Zone\" = " + zone));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"Tab_Zone_STAMM\" WHERE \"ID_Gebaeude\" = " + e.Id));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteil_STAMM\" b INNER JOIN \"Tab_Zone_STAMM\" z " +
                                  "ON z.\"ID\" = b.\"ID_Zone\" WHERE z.\"ID_Gebaeude\" = " + e.Id));
        }

        [Fact]
        public void Die_Huelle_meldet_die_Uebernahme_und_lehnt_eine_ungespeicherte_Zeile_ab()
        {
            if (!_db.Vorhanden) return;
            int idZ = Zuordnung(1040);

            GebaeudeDbUebernahme ohneKopie = GebaeudeHuelle.InDbUebernehmen(
                new GebaeudeProjektZeile { IdZ = GebaeudeHuelle.STARTINDEX, HatProjektkopie = false }, "Probe");
            Assert.False(ohneKopie.Ok);
            Assert.False(ohneKopie.AmNamen);

            GebaeudeDbUebernahme leer = GebaeudeHuelle.InDbUebernehmen(
                new GebaeudeProjektZeile { IdZ = idZ, HatProjektkopie = true }, "");
            Assert.False(leer.Ok);
            Assert.True(leer.AmNamen);

            GebaeudeDbUebernahme gut = GebaeudeHuelle.InDbUebernehmen(
                new GebaeudeProjektZeile { IdZ = idZ, HatProjektkopie = true }, "Übernahmeprobe Hülle");
            Assert.True(gut.Ok, gut.Meldung);
            Assert.Equal("Übernahmeprobe Hülle", gut.Name);
            Assert.Equal("Gebäude „Übernahmeprobe Hülle“ in die Datenbank übernommen.", gut.Meldung);
            Assert.True(GebaeudeStammCtrl.NameVergeben("Übernahmeprobe Hülle"));
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        /// <summary>Die erste Gebäudezuordnung (<c>Z_ProjektGebaeude.ID</c>) eines Projekts mit Projektkopie.</summary>
        private static int Zuordnung(int projekt)
            => (int)Zahl("SELECT MIN(\"ID_ProjektGebaeude\") FROM \"Tab_Gebaeude\" WHERE \"ID_Projekt\" = " + projekt);

        private static DataRow Projektkopie(int idZ)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM \"Tab_Gebaeude\" WHERE \"ID_ProjektGebaeude\" = ?", new DbParam("@z", idZ));
            Assert.NotNull(dt);
            Assert.Single(dt.Rows);
            return dt.Rows[0];
        }

        private static DataRow Katalogsatz(int id)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM \"Tab_Gebaeude_STAMM\" WHERE \"ID\" = ?", new DbParam("@i", id));
            Assert.NotNull(dt);
            Assert.Single(dt.Rows);
            return dt.Rows[0];
        }

        /// <summary>Die Fachspalten aus der Konstante des Kerns — und gegen das Schema gehalten.</summary>
        private static string[] Kopfspalten()
        {
            var spalten = new List<string>();
            foreach (string s in GebaeudeStammCtrl.KOPFSPALTEN.Split(','))
                spalten.Add(s.Trim().Trim('[', ']'));

            // Jede Katalogspalte außer ID, Bezeichner, Beschreibung und ReadOnly steht in der Liste.
            DataTable schema = DataRepository.GetDataTable("SELECT name FROM pragma_table_info('Tab_Gebaeude_STAMM')");
            foreach (DataRow r in schema.Rows)
            {
                string name = Convert.ToString(r["name"], CultureInfo.InvariantCulture);
                if (name is "ID" or "Bezeichner" or "Beschreibung" or "ReadOnly") continue;
                if (Katalogfassung.IstKatalogspalte(name)) continue;   // KU1: kein Fachwert
                Assert.Contains(name, spalten);
            }
            return spalten.ToArray();
        }

        private static long Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
