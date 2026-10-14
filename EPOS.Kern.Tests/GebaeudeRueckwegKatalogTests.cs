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
    /// <b>Gebäude-Rückweg Projekt → Katalog nach Regel 5.2</b> (<see cref="GebaeudeRueckweg"/>; Konzept Katalogauswahl 5.14) —
    /// nicht zu verwechseln mit dem Tagesbilanz-Rückweg der Gebäuderechnung (<c>GebaeudeRueckwegTests</c>).
    ///
    /// <para><b>Geprüft wird:</b> „neu" unter neuem Namen setzt den Ursprungsverweis; „überschreiben" ersetzt Kopf und
    /// Kindzeilen (Zonen, Bauteile samt Nachbarzonen, Luftströme, Kalender) — zweimal hintereinander ohne Verdopplung — und
    /// lässt Name, Schloss und <c>Katalog_*</c> stehen; ein Fehler mitten im Vorgang rollt alles zurück; gesperrter und
    /// unbekannter Ursprung, leerer und vergebener Name werden benannt abgelehnt, ohne zu schreiben; die Vorschau nennt die
    /// Abweichungen und Kindzeilen; andere Projektgebäude mit demselben Ursprung behalten ihren Verweis.</para>
    ///
    /// <para>Jeder Fall arbeitet auf einer Arbeitskopie der Testdatenbank; ohne sie schweigt er.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeRueckwegKatalogTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static readonly DateTime STICHTAG = new DateTime(2026, 10, 11);

        [Fact]
        public void Neu_unter_neuem_Namen_setzt_den_Ursprung_und_laesst_die_anderen_Verweise_stehen()
        {
            if (!_db.Vorhanden) return;
            int idZ = Zuordnung(1040);
            long alterUrsprung = Ursprung(idZ);
            List<int> andere = AndereMitUrsprung(idZ, alterUrsprung);
            Assert.NotEmpty(andere);
            long saetze = Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\"");

            Rueckwegergebnis e = GebaeudeStammCtrl.Rueckweg(idZ, Rueckwegart.Neu, "  Rückwegprobe neu  ", STICHTAG);

            Assert.True(e.Ok, e.Meldung);
            Rueckwegsatz s = Assert.Single(e.Saetze);
            Assert.True(s.Neu);
            Assert.Equal("Rückwegprobe neu", s.Name);
            Assert.Equal(saetze + 1, Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\""));
            Assert.Equal(s.IdKatalog, Ursprung(idZ));
            Assert.Equal(0L, Zahl("SELECT \"ReadOnly\" FROM \"Tab_Gebaeude_STAMM\" WHERE \"ID\" = " + s.IdKatalog));
            foreach (int a in andere) Assert.Equal(alterUrsprung, Ursprung(a));

            // Ein zweiter Rückweg kann jetzt überschreiben.
            GebaeudeRueckwegVorschau v = GebaeudeStammCtrl.RueckwegVorschau(idZ);
            Assert.Equal(s.IdKatalog, v.IdUrsprung);
            Assert.True(v.KannUeberschreiben);
            Assert.Equal("Rückwegprobe neu", v.NameUrsprung);
        }

        [Fact]
        public void Ueberschreiben_ersetzt_Kopf_und_Kindzeilen_und_laesst_Name_Schloss_und_Katalogspalten_stehen()
        {
            if (!_db.Vorhanden) return;
            int idZ = Zuordnung(1052);
            int idKopie = Kopie(idZ);
            long idU = Ursprung(idZ);
            List<int> andere = AndereMitUrsprung(idZ, idU);
            Assert.NotEmpty(andere);
            Dictionary<string, int> projekt = GebaeudeRueckweg.Kindzeilen(null, Zonenebene.Projekt, idKopie);
            Assert.Equal(3, projekt[GebaeudeRueckweg.ART_ZONEN]);
            Assert.True(projekt[GebaeudeRueckweg.ART_BAUTEILE] > 0);
            Assert.True(projekt[GebaeudeRueckweg.ART_KALENDER] > 0);

            Ausfuehren("UPDATE \"Tab_Gebaeude\" SET \"Bewohner\" = 777 WHERE \"ID\" = " + idKopie);
            Ausfuehren("UPDATE \"Tab_Gebaeude_STAMM\" SET \"Katalog_Schluessel\" = 'PROBE-SCHLUESSEL', " +
                       "\"Katalog_Pruefsumme\" = '" + new string('a', 64) + "' WHERE \"ID\" = " + idU);
            DataRow vorher = Katalogsatz(idU);
            long saetze = Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\"");

            for (int runde = 1; runde <= 2; runde++)
            {
                Rueckwegergebnis e = GebaeudeStammCtrl.Rueckweg(idZ, Rueckwegart.Ueberschreiben, "egal", STICHTAG);
                Assert.True(e.Ok, e.Meldung);
                Rueckwegsatz s = Assert.Single(e.Saetze);
                Assert.False(s.Neu);
                Assert.Equal(idU, s.IdKatalog);
                Assert.True(s.Kindzeilen > 0);

                // Ersatz, keine Verdopplung — in jeder Runde dieselben Zahlen wie am Projektgebäude.
                Dictionary<string, int> katalog = GebaeudeRueckweg.Kindzeilen(null, Zonenebene.Katalog, idU);
                foreach (KeyValuePair<string, int> p in projekt)
                    Assert.True(p.Value == katalog[p.Key], "Runde " + runde + ", " + p.Key + ": Projekt " + p.Value + ", Katalog " + katalog[p.Key]);
            }

            Assert.Equal(saetze, Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\""));
            DataRow nachher = Katalogsatz(idU);
            DataRow kopie = Projektkopie(idZ);
            foreach (string sp in GebaeudeRueckweg.Kopfspalten())
                Assert.True(Equals(kopie[sp], nachher[sp]) || (kopie[sp] is DBNull && nachher[sp] is DBNull), "Spalte " + sp);
            Assert.Equal(777L, Convert.ToInt64(nachher["Bewohner"], CultureInfo.InvariantCulture));
            foreach (string sp in new[] { "Bezeichner", "ReadOnly", "Katalog_Schluessel", "Katalog_Pruefsumme", "Katalog_Ausgelaufen" })
                Assert.True(Equals(vorher[sp], nachher[sp]), "Spalte " + sp);

            // Die Nachbarzonen zeigen auf die neuen Zonen des Ursprungs.
            long nachbarProjekt = Zahl("SELECT COUNT(*) FROM \"Tab_Bauteil\" WHERE \"ID_Nachbarzone\" IS NOT NULL AND \"ID_Zone\" IN " +
                                       "(SELECT \"ID\" FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = " + idKopie + ")");
            string zonenU = "(SELECT \"ID\" FROM \"Tab_Zone_STAMM\" WHERE \"ID_Gebaeude\" = " + idU + ")";
            Assert.Equal(nachbarProjekt, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteil_STAMM\" WHERE \"ID_Nachbarzone\" IN " + zonenU +
                                              " AND \"ID_Zone\" IN " + zonenU));

            // Andere Projektgebäude behalten den Verweis; das Projektgebäude selbst auch.
            Assert.Equal(idU, Ursprung(idZ));
            foreach (int a in andere) Assert.Equal(idU, Ursprung(a));
        }

        [Fact]
        public void Ueberschreiben_ersetzt_die_Konditionierungskalender_ohne_Verdopplung()
        {
            if (!_db.Vorhanden) return;
            int idZ = Zuordnung(1051);
            int idKopie = Kopie(idZ);
            long idU = Ursprung(idZ);
            Dictionary<string, int> projekt = GebaeudeRueckweg.Kindzeilen(null, Zonenebene.Projekt, idKopie);
            Assert.True(projekt[GebaeudeRueckweg.ART_KALENDER] > 0);
            Assert.True(GebaeudeRueckweg.Kindzeilen(null, Zonenebene.Katalog, idU)[GebaeudeRueckweg.ART_KALENDER] > 0);

            Rueckwegergebnis e = GebaeudeRueckweg.Rueckweg(idZ, Rueckwegart.Ueberschreiben, "", STICHTAG);

            Assert.True(e.Ok, e.Meldung);
            Dictionary<string, int> katalog = GebaeudeRueckweg.Kindzeilen(null, Zonenebene.Katalog, idU);
            Assert.Equal(projekt[GebaeudeRueckweg.ART_KALENDER], katalog[GebaeudeRueckweg.ART_KALENDER]);
            Assert.Equal(projekt[GebaeudeRueckweg.ART_VORGABEN], katalog[GebaeudeRueckweg.ART_VORGABEN]);
            Assert.Equal(projekt[GebaeudeRueckweg.ART_PERIODEN], katalog[GebaeudeRueckweg.ART_PERIODEN]);
        }

        [Fact]
        public void Ein_Fehler_mitten_im_Ueberschreiben_rollt_Kopf_und_Kindzeilen_zurueck()
        {
            if (!_db.Vorhanden) return;
            int idZ = Zuordnung(1052);
            int idKopie = Kopie(idZ);
            long idU = Ursprung(idZ);
            Assert.True(GebaeudeRueckweg.Rueckweg(idZ, Rueckwegart.Ueberschreiben, "", STICHTAG).Ok);
            DataRow vorher = Katalogsatz(idU);
            Dictionary<string, int> kinderVorher = GebaeudeRueckweg.Kindzeilen(null, Zonenebene.Katalog, idU);
            Assert.Equal(3, kinderVorher[GebaeudeRueckweg.ART_ZONEN]);

            Ausfuehren("UPDATE \"Tab_Gebaeude\" SET \"Bewohner\" = 778 WHERE \"ID\" = " + idKopie);
            Ausfuehren("CREATE TRIGGER \"trg_Probe_Rueckweg\" BEFORE INSERT ON \"Tab_Zone_STAMM\" WHEN NEW.\"ID_Gebaeude\" = " + idU +
                       " BEGIN SELECT RAISE(ABORT, 'Probe'); END");
            try
            {
                Rueckwegergebnis e = GebaeudeRueckweg.Rueckweg(idZ, Rueckwegart.Ueberschreiben, "", STICHTAG);
                Assert.False(e.Ok);
                Assert.Equal(Rueckwegabsage.Fehler, e.Absage);
                Assert.Empty(e.Saetze);
            }
            finally
            {
                Ausfuehren("DROP TRIGGER \"trg_Probe_Rueckweg\"");
            }

            DataRow nachher = Katalogsatz(idU);
            foreach (DataColumn c in vorher.Table.Columns)
                Assert.True(Equals(vorher[c.ColumnName], nachher[c.ColumnName]), "Spalte " + c.ColumnName);
            Dictionary<string, int> kinderNachher = GebaeudeRueckweg.Kindzeilen(null, Zonenebene.Katalog, idU);
            foreach (KeyValuePair<string, int> k in kinderVorher) Assert.Equal(k.Value, kinderNachher[k.Key]);
        }

        [Fact]
        public void Gesperrter_und_unbekannter_Ursprung_werden_benannt_abgelehnt_ohne_zu_schreiben()
        {
            if (!_db.Vorhanden) return;
            int idZ = Zuordnung(1040);
            int idKopie = Kopie(idZ);
            long idU = Ursprung(idZ);
            Ausfuehren("UPDATE \"Tab_Gebaeude\" SET \"Bewohner\" = 779 WHERE \"ID\" = " + idKopie);
            Ausfuehren("UPDATE \"Tab_Gebaeude_STAMM\" SET \"ReadOnly\" = 1 WHERE \"ID\" = " + idU);
            DataRow vorher = Katalogsatz(idU);

            Assert.Equal(Rueckwegabsage.UrsprungGesperrt, GebaeudeRueckweg.Vorschau(idZ).Ueberschreiben);
            Rueckwegergebnis gesperrt = GebaeudeRueckweg.Rueckweg(idZ, Rueckwegart.Ueberschreiben, "", STICHTAG);
            Assert.False(gesperrt.Ok);
            Assert.Equal(Rueckwegabsage.UrsprungGesperrt, gesperrt.Absage);
            Assert.Contains("Schloss", gesperrt.Meldung);
            DataRow nachher = Katalogsatz(idU);
            foreach (DataColumn c in vorher.Table.Columns)
                Assert.True(Equals(vorher[c.ColumnName], nachher[c.ColumnName]), "Spalte " + c.ColumnName);

            Ausfuehren("UPDATE \"Tab_Gebaeude\" SET \"ID_Gebaeude_Stamm\" = NULL WHERE \"ID\" = " + idKopie);
            GebaeudeRueckwegVorschau v = GebaeudeRueckweg.Vorschau(idZ);
            Assert.Equal(Rueckwegabsage.UrsprungUnbekannt, v.Ueberschreiben);
            Assert.Null(v.IdUrsprung);
            Assert.Equal(Rueckwegabsage.UrsprungUnbekannt,
                         GebaeudeRueckweg.Rueckweg(idZ, Rueckwegart.Ueberschreiben, "", STICHTAG).Absage);
        }

        [Fact]
        public void Kein_Gebaeude_leerer_und_vergebener_Name_werden_benannt_abgelehnt_ohne_zu_schreiben()
        {
            if (!_db.Vorhanden) return;
            int idZ = Zuordnung(1052);
            long saetze = Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\"");
            string vergeben = Convert.ToString(Projektkopie(idZ)["Gebaeudename"], CultureInfo.InvariantCulture);
            Assert.True(GebaeudeStammCtrl.RueckwegNameBelegt(vergeben));

            Assert.Equal(Rueckwegabsage.KeineKopie, GebaeudeRueckweg.Rueckweg(0, Rueckwegart.Neu, "Probe").Absage);
            Assert.Equal(Rueckwegabsage.NameLeer, GebaeudeRueckweg.Rueckweg(idZ, Rueckwegart.Neu, "   ").Absage);
            Rueckwegergebnis belegt = GebaeudeRueckweg.Rueckweg(idZ, Rueckwegart.Neu, " " + vergeben + " ");
            Assert.False(belegt.Ok);
            Assert.Equal(Rueckwegabsage.NameBelegt, belegt.Absage);
            Assert.Equal(saetze, Zahl("SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\""));
        }

        [Fact]
        public void Die_Vorschau_nennt_Ursprung_Namensvorschlag_Abweichungen_und_Kindzeilen_und_schreibt_nichts()
        {
            if (!_db.Vorhanden) return;
            int idZ = Zuordnung(1052);
            int idKopie = Kopie(idZ);
            long idU = Ursprung(idZ);
            Ausfuehren("UPDATE \"Tab_Gebaeude\" SET \"Bewohner\" = 781 WHERE \"ID\" = " + idKopie);
            DataRow vorher = Katalogsatz(idU);
            string nameU = Convert.ToString(vorher["Bezeichner"], CultureInfo.InvariantCulture);

            GebaeudeRueckwegVorschau v = GebaeudeRueckweg.Vorschau(idZ);

            Assert.NotNull(v);
            Assert.Equal(idKopie, v.IdKopie);
            Assert.Equal((int?)idU, v.IdUrsprung);
            Assert.Equal(nameU, v.NameUrsprung);
            Assert.True(v.KannUeberschreiben);
            Assert.Equal(nameU + " (Projekt)", v.Namensvorschlag);
            GebaeudeAbweichung bewohner = Assert.Single(v.Abweichungen, a => a.Spalte == "Bewohner");
            Assert.Equal("781", bewohner.Projekt);
            Assert.DoesNotContain(v.Abweichungen, a => a.Spalte.StartsWith("Katalog_", StringComparison.Ordinal) ||
                                                       a.Spalte is "ReadOnly" or "Bezeichner" or "ID");
            GebaeudeKindzahl zonen = Assert.Single(v.Kindzeilen, k => k.Art == GebaeudeRueckweg.ART_ZONEN);
            Assert.Equal(3, zonen.Projekt);
            Assert.Equal(0, zonen.Ursprung);
            Assert.Equal(idZ, v.AlsZeile().IdKopie);
            Assert.Null(GebaeudeRueckweg.Vorschau(0));

            DataRow nachher = Katalogsatz(idU);
            foreach (DataColumn c in vorher.Table.Columns)
                Assert.True(Equals(vorher[c.ColumnName], nachher[c.ColumnName]), "Spalte " + c.ColumnName);
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        private static int Zuordnung(int projekt)
        {
            int idZ = (int)Zahl("SELECT MIN(\"ID_ProjektGebaeude\") FROM \"Tab_Gebaeude\" WHERE \"ID_Projekt\" = " + projekt);
            Assert.True(idZ > 0, "Projekt " + projekt + " ohne Gebäude");
            return idZ;
        }

        private static int Kopie(int idZ)
            => (int)Zahl("SELECT \"ID\" FROM \"Tab_Gebaeude\" WHERE \"ID_ProjektGebaeude\" = " + idZ);

        private static long Ursprung(int idZ)
            => Zahl("SELECT \"ID_Gebaeude_Stamm\" FROM \"Tab_Gebaeude\" WHERE \"ID_ProjektGebaeude\" = " + idZ);

        private static List<int> AndereMitUrsprung(int idZ, long idU)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"ID_ProjektGebaeude\" FROM \"Tab_Gebaeude\" WHERE \"ID_Gebaeude_Stamm\" = ? AND \"ID_ProjektGebaeude\" <> ?",
                new DbParam("@u", idU), new DbParam("@z", idZ));
            return dt.Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture)).ToList();
        }

        private static DataRow Projektkopie(int idZ)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM \"Tab_Gebaeude\" WHERE \"ID_ProjektGebaeude\" = ?", new DbParam("@z", idZ));
            return Assert.Single(dt.Rows.Cast<DataRow>());
        }

        private static DataRow Katalogsatz(long id)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM \"Tab_Gebaeude_STAMM\" WHERE \"ID\" = ?", new DbParam("@i", id));
            return Assert.Single(dt.Rows.Cast<DataRow>());
        }

        private static void Ausfuehren(string sql) => Assert.True(DataRepository.ExecuteSQL(sql), sql);

        private static long Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
