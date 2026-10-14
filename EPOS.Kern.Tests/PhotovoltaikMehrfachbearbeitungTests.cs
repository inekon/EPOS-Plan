using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Mehrfach-Bearbeiten der PV-Module in EINER Transaktion</b> (Konzept Projektdialoge mit Katalogauswahl 4.6,
    /// Entscheid KA‑E‑8, Stufe 3): <see cref="PhotovoltaikStammCtrl.SchreibenAlle"/> schreibt alle geänderten Sätze —
    /// Projektkopien oder Katalogsätze — oder keinen; die Modulkosten gehen mit, der Name bleibt, die Koeffizienten
    /// <c>alpha_SC</c> und <c>beta_OC</c> bleiben unberührt, eine leere Spalte bleibt leer. Geschrieben wird in der
    /// Kopie der Testdatenbank (<see cref="TestDatenbank"/>); die Module der Referenzprojekte bleiben, wie sie sind.
    /// </summary>
    [Collection("Testdatenbank")]
    public class PhotovoltaikMehrfachbearbeitungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        /// <summary>Die Projektkopie des Referenzprojekts 1046 (Einfrierregel: gesäte Modulkoeffizienten).</summary>
        private const int KOPIE_1046 = 1015250;

        private static List<int> Ids(string tabelle, int anzahl)
        {
            var ids = new List<int>();
            var dt = DataRepository.GetDataTable("SELECT ID FROM [" + tabelle + "] ORDER BY ID LIMIT " +
                                                 anzahl.ToString(CultureInfo.InvariantCulture));
            foreach (System.Data.DataRow r in dt.Rows) ids.Add(Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            return ids;
        }

        private static PhotovoltaikModel Daten(bool projektkopie, int id, string firma, double modulkosten = 120,
                                               double leistung = 400)
        {
            PhotovoltaikModel m = PhotovoltaikStammCtrl.Satz(projektkopie, id);
            m.m_szFirma = firma;
            m.m_Modulkosten = modulkosten;
            m.m_Leistung = leistung;
            m.m_alpha_SC = 9.9;                 // fuehrt die Feldliste nicht - wird nie geschrieben
            m.m_szName = "Umbenannt";           // wird nie geschrieben
            return m;
        }

        private static object Roh(string tabelle, int id, string spalte)
            => DataRepository.ExecuteScalar("SELECT [" + spalte + "] FROM [" + tabelle + "] WHERE ID = ?", new DbParam("?", id));

        private static double Zahl(string tabelle, int id, string spalte)
            => Convert.ToDouble(Roh(tabelle, id, spalte), CultureInfo.InvariantCulture);

        private static bool Leer(object v) => v == null || v == DBNull.Value;

        private static string Feld(bool projektkopie, int id, string feld)
            => PhotovoltaikStammCtrl.SatzAnzeige(projektkopie, id)[feld];

        private static void Sperren(int id, bool gesperrt)
            => DataRepository.ExecuteSQL("UPDATE [Tab_PV_STAMM] SET ReadOnly = ? WHERE ID = ?",
                                         new DbParam("?", gesperrt ? 1 : 0), new DbParam("?", id));

        [Fact]
        public void SatzAnzeige_und_SatzDetail_lesen_Projektkopie_und_Katalogsatz_nach_ID()
        {
            if (!_db.Vorhanden) return;
            int k = Ids(PhotovoltaikStammCtrl.TABLE, 1)[0];
            Assert.Equal(Convert.ToString(Roh("Tab_PV", KOPIE_1046, "Bezeichner"), CultureInfo.InvariantCulture),
                         Feld(true, KOPIE_1046, ModulKatalogProfil.FeldBezeichner));
            Assert.Equal(Convert.ToString(Roh("Tab_PV_STAMM", k, "Bezeichner"), CultureInfo.InvariantCulture),
                         Feld(false, k, ModulKatalogProfil.FeldBezeichner));
            Assert.True(PhotovoltaikStammCtrl.SatzAnzeige(true, KOPIE_1046).ContainsKey(ModulKatalogProfil.FeldModulkosten));
            PhotovoltaikStammCtrl.ModulDetail d = PhotovoltaikStammCtrl.SatzDetail(true, KOPIE_1046);
            Assert.Equal(Zahl("Tab_PV", KOPIE_1046, "alpha_SC"), d.AlphaSc!.Value, 12);
            Assert.Null(PhotovoltaikStammCtrl.SatzAnzeige(true, 987654));
            Assert.Null(PhotovoltaikStammCtrl.Satz(false, 987654));
            Assert.Null(PhotovoltaikStammCtrl.SatzDetail(false, 0));
        }

        [Fact]
        public void Alle_Projektkopien_werden_geschrieben_Name_Koeffizienten_und_Katalog_bleiben()
        {
            if (!_db.Vorhanden) return;
            var ids = new List<int> { KOPIE_1046, KOPIE_1046 + 1 };
            int katalog = Ids(PhotovoltaikStammCtrl.TABLE, 1)[0];
            string katalogVorher = Feld(false, katalog, ModulKatalogProfil.FeldFirma);
            string name0 = Feld(true, ids[0], ModulKatalogProfil.FeldBezeichner);
            double alpha = Zahl("Tab_PV", ids[0], "alpha_SC");
            double beta = Zahl("Tab_PV", ids[0], "beta_OC");

            var e = PhotovoltaikStammCtrl.SchreibenAlle(true, new[]
            {
                new PhotovoltaikStammCtrl.Satzaenderung(ids[0], Daten(true, ids[0], "Sammel 1", modulkosten: 333)),
                new PhotovoltaikStammCtrl.Satzaenderung(ids[1], Daten(true, ids[1], "Sammel 2"))
            });

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Sammel 1", Feld(true, ids[0], ModulKatalogProfil.FeldFirma));
            Assert.Equal("Sammel 2", Feld(true, ids[1], ModulKatalogProfil.FeldFirma));
            Assert.Equal(333, Zahl("Tab_PV", ids[0], "Modulkosten"));
            Assert.Equal(400, Zahl("Tab_PV", ids[0], "Leistung"));
            Assert.Equal(name0, Feld(true, ids[0], ModulKatalogProfil.FeldBezeichner));
            Assert.Equal(alpha, Zahl("Tab_PV", ids[0], "alpha_SC"), 12);
            Assert.Equal(beta, Zahl("Tab_PV", ids[0], "beta_OC"), 12);
            Assert.Equal(katalogVorher, Feld(false, katalog, ModulKatalogProfil.FeldFirma));
        }

        [Fact]
        public void Ein_Katalogsatz_wird_geschrieben_die_Projektkopien_der_Referenzprojekte_bleiben()
        {
            if (!_db.Vorhanden) return;
            int katalog = PhotovoltaikStammCtrl.IdsMitBezeichner("Ablytek 6MN6A270")[0];
            Sperren(katalog, false);
            var vorher = DataRepository.GetDataTable("SELECT * FROM Tab_PV WHERE Bezeichner = ? ORDER BY ID",
                                                     new DbParam("?", "Ablytek 6MN6A270"));

            var e = PhotovoltaikStammCtrl.SchreibenAlle(false, new[]
                { new PhotovoltaikStammCtrl.Satzaenderung(katalog, Daten(false, katalog, "Katalogprobe")) });

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Katalogprobe", Feld(false, katalog, ModulKatalogProfil.FeldFirma));
            var nachher = DataRepository.GetDataTable("SELECT * FROM Tab_PV WHERE Bezeichner = ? ORDER BY ID",
                                                      new DbParam("?", "Ablytek 6MN6A270"));
            Assert.Equal(vorher.Rows.Count, nachher.Rows.Count);
            for (int i = 0; i < vorher.Rows.Count; i++)
                Assert.Equal(vorher.Rows[i].ItemArray, nachher.Rows[i].ItemArray);
        }

        [Fact]
        public void Eine_leere_Spalte_bleibt_leer_wenn_sie_als_0_zurueckkommt()
        {
            if (!_db.Vorhanden) return;
            int id = KOPIE_1046;
            DataRepository.ExecuteSQL("UPDATE [Tab_PV] SET T_NOCT = NULL, Laenge = NULL, Technologie = NULL WHERE ID = ?",
                                      new DbParam("?", id));

            var e = PhotovoltaikStammCtrl.SchreibenAlle(true, new[]
                { new PhotovoltaikStammCtrl.Satzaenderung(id, Daten(true, id, "Leerprobe")) });

            Assert.True(e.Ok, e.Meldung);
            Assert.True(Leer(Roh("Tab_PV", id, "T_NOCT")), "T_NOCT");
            Assert.True(Leer(Roh("Tab_PV", id, "Laenge")), "Laenge");
            Assert.True(Leer(Roh("Tab_PV", id, "Technologie")), "Technologie");
        }

        [Fact]
        public void Ein_Verstoss_im_zweiten_Satz_schreibt_keinen()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(PhotovoltaikStammCtrl.TABLE, 2);
            ids.ForEach(id => Sperren(id, false));
            string vorher = Feld(false, ids[0], ModulKatalogProfil.FeldFirma);

            PhotovoltaikModel b = Daten(false, ids[1], "Sammel 2");
            b.m_Wirkungsgrad = 150;
            var e = PhotovoltaikStammCtrl.SchreibenAlle(false, new[]
            {
                new PhotovoltaikStammCtrl.Satzaenderung(ids[0], Daten(false, ids[0], "Sammel 1")),
                new PhotovoltaikStammCtrl.Satzaenderung(ids[1], b)
            });

            Assert.False(e.Ok);
            Assert.False(string.IsNullOrEmpty(e.Meldung));
            Assert.Equal(vorher, Feld(false, ids[0], ModulKatalogProfil.FeldFirma));
            Assert.Null(PhotovoltaikStammCtrl.Pruefen(Daten(false, ids[0], "x")));
            Assert.NotNull(PhotovoltaikStammCtrl.Pruefen(Daten(false, ids[0], "x", leistung: -1)));
            // Der Temperaturkoeffizient der Leistung ist negativ und kein Verstoss.
            PhotovoltaikModel g = Daten(false, ids[0], "x");
            g.m_Temp_Coeff_Pmax = -0.45;
            Assert.Null(PhotovoltaikStammCtrl.Pruefen(g));
        }

        [Fact]
        public void Ein_gesperrter_Katalogsatz_rollt_alles_zurueck_und_wird_genannt()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(PhotovoltaikStammCtrl.TABLE, 2);
            Sperren(ids[0], false);
            Sperren(ids[1], true);
            string vorher = Feld(false, ids[0], ModulKatalogProfil.FeldFirma);
            string name = Feld(false, ids[1], ModulKatalogProfil.FeldBezeichner);

            var e = PhotovoltaikStammCtrl.SchreibenAlle(false, new[]
            {
                new PhotovoltaikStammCtrl.Satzaenderung(ids[0], Daten(false, ids[0], "Sammel 1")),
                new PhotovoltaikStammCtrl.Satzaenderung(ids[1], Daten(false, ids[1], "Sammel 2"))
            });

            Assert.False(e.Ok);
            Assert.Contains(name, e.Meldung);
            Assert.Equal(vorher, Feld(false, ids[0], ModulKatalogProfil.FeldFirma));
        }

        [Fact]
        public void Eine_fehlende_ID_schreibt_keinen()
        {
            if (!_db.Vorhanden) return;
            int id = KOPIE_1046;
            string vorher = Feld(true, id, ModulKatalogProfil.FeldFirma);

            var e = PhotovoltaikStammCtrl.SchreibenAlle(true, new[]
            {
                new PhotovoltaikStammCtrl.Satzaenderung(id, Daten(true, id, "Sammel 1")),
                new PhotovoltaikStammCtrl.Satzaenderung(987654, new PhotovoltaikModel())
            });

            Assert.False(e.Ok);
            Assert.Contains("987654", e.Meldung);
            Assert.Equal(vorher, Feld(true, id, ModulKatalogProfil.FeldFirma));
        }

        [Fact]
        public void Ohne_Saetze_ist_nichts_zu_tun()
        {
            Assert.True(PhotovoltaikStammCtrl.SchreibenAlle(false, Array.Empty<PhotovoltaikStammCtrl.Satzaenderung>()).Ok);
        }
    }
}
