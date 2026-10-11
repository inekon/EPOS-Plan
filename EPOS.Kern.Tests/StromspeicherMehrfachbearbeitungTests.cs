using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Mehrfach-Bearbeiten der Stromspeicher in EINER Transaktion</b> (Konzept Projektdialoge mit Katalogauswahl
    /// 4.6, Entscheid KA‑E‑8, Stufe 3): <see cref="StromspeicherStammCtrl.SchreibenAlle"/> schreibt alle geänderten
    /// Sätze — Projektkopien oder Katalogsätze — oder keinen; die vier Kostenposten gehen mit, der Name bleibt, eine
    /// leere Gerätespalte bleibt leer. Geschrieben wird in der Kopie der Testdatenbank (<see cref="TestDatenbank"/>).
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromspeicherMehrfachbearbeitungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        private static List<int> Ids(string tabelle, int anzahl)
        {
            var ids = new List<int>();
            var dt = DataRepository.GetDataTable("SELECT ID FROM [" + tabelle + "] ORDER BY ID LIMIT " +
                                                 anzahl.ToString(CultureInfo.InvariantCulture));
            foreach (System.Data.DataRow r in dt.Rows) ids.Add(Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            return ids;
        }

        private static StromspeicherModel Daten(bool projektkopie, int id, string firma, double modulkosten = 300,
                                                double leistung = 10)
        {
            StromspeicherModel m = StromspeicherStammCtrl.Satz(projektkopie, id);
            m.m_szFirma = firma;
            m.m_Modulkosten = modulkosten;
            m.m_Leistung = leistung;
            m.m_szBezeichner = "Umbenannt";      // wird nie geschrieben
            return m;
        }

        private static object Roh(string tabelle, int id, string spalte)
            => DataRepository.ExecuteScalar("SELECT [" + spalte + "] FROM [" + tabelle + "] WHERE ID = ?", new DbParam("?", id));

        private static bool Leer(object v) => v == null || v == DBNull.Value;

        private static string Feld(bool projektkopie, int id, string feld)
            => StromspeicherStammCtrl.SatzAnzeige(projektkopie, id)[feld];

        private static void Sperren(int id, bool gesperrt)
            => DataRepository.ExecuteSQL("UPDATE [Tab_Stromspeicher_STAMM] SET ReadOnly = ? WHERE ID = ?",
                                         new DbParam("?", gesperrt ? 1 : 0), new DbParam("?", id));

        [Fact]
        public void SatzAnzeige_liest_Projektkopie_und_Katalogsatz_nach_ID_samt_Kostenposten()
        {
            if (!_db.Vorhanden) return;
            int p = Ids(StromspeicherStammCtrl.TABELLE_PROJEKT, 1)[0];
            int k = Ids(StromspeicherStammCtrl.TABLE, 1)[0];
            Assert.Equal(Convert.ToString(Roh("Tab_Stromspeicher", p, "Bezeichner"), CultureInfo.InvariantCulture),
                         Feld(true, p, ModulKatalogProfil.FeldBezeichner));
            Assert.Equal(Convert.ToString(Roh("Tab_Stromspeicher_STAMM", k, "Bezeichner"), CultureInfo.InvariantCulture),
                         Feld(false, k, ModulKatalogProfil.FeldBezeichner));
            var anzeige = StromspeicherStammCtrl.SatzAnzeige(true, p);
            foreach (string f in new[] { ModulKatalogProfil.FeldModulkosten, ModulKatalogProfil.FeldLeistungskosten,
                                         ModulKatalogProfil.FeldInvestitionFix, ModulKatalogProfil.FeldVerschleisskosten })
                Assert.True(anzeige.ContainsKey(f), f);
            Assert.Null(StromspeicherStammCtrl.SatzAnzeige(true, 987654));
            Assert.Null(StromspeicherStammCtrl.Satz(false, 987654));
        }

        [Fact]
        public void Alle_Projektkopien_werden_samt_Kostenposten_geschrieben_Name_und_Katalog_bleiben()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(StromspeicherStammCtrl.TABELLE_PROJEKT, 2);
            Assert.Equal(2, ids.Count);
            int katalog = Ids(StromspeicherStammCtrl.TABLE, 1)[0];
            string katalogVorher = Feld(false, katalog, ModulKatalogProfil.FeldFirma);
            string name0 = Feld(true, ids[0], ModulKatalogProfil.FeldBezeichner);

            StromspeicherModel a = Daten(true, ids[0], "Sammel 1", modulkosten: 333);
            a.m_Leistungskosten = 111; a.m_InvestitionFix = 2222; a.m_Verschleisskosten = 0.05;
            var e = StromspeicherStammCtrl.SchreibenAlle(true, new[]
            {
                new StromspeicherStammCtrl.Satzaenderung(ids[0], a),
                new StromspeicherStammCtrl.Satzaenderung(ids[1], Daten(true, ids[1], "Sammel 2"))
            });

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Sammel 1", Feld(true, ids[0], ModulKatalogProfil.FeldFirma));
            Assert.Equal("Sammel 2", Feld(true, ids[1], ModulKatalogProfil.FeldFirma));
            Assert.Equal(333, Convert.ToDouble(Roh("Tab_Stromspeicher", ids[0], "Modulkosten"), CultureInfo.InvariantCulture));
            Assert.Equal(111, Convert.ToDouble(Roh("Tab_Stromspeicher", ids[0], "Leistungskosten"), CultureInfo.InvariantCulture));
            Assert.Equal(2222, Convert.ToDouble(Roh("Tab_Stromspeicher", ids[0], "Investition_Fix"), CultureInfo.InvariantCulture));
            Assert.Equal(0.05, Convert.ToDouble(Roh("Tab_Stromspeicher", ids[0], "Verschleisskosten"), CultureInfo.InvariantCulture), 9);
            Assert.Equal(name0, Feld(true, ids[0], ModulKatalogProfil.FeldBezeichner));
            Assert.Equal(katalogVorher, Feld(false, katalog, ModulKatalogProfil.FeldFirma));
        }

        [Fact]
        public void Eine_leere_Geraetespalte_bleibt_leer_wenn_sie_als_0_zurueckkommt()
        {
            if (!_db.Vorhanden) return;
            int id = Ids(StromspeicherStammCtrl.TABELLE_PROJEKT, 1)[0];
            DataRepository.ExecuteSQL("UPDATE [Tab_Stromspeicher] SET Selbstentladung_Prozent_Monat = NULL, Standby_Verbrauch = NULL WHERE ID = ?",
                                      new DbParam("?", id));

            var e = StromspeicherStammCtrl.SchreibenAlle(true, new[]
                { new StromspeicherStammCtrl.Satzaenderung(id, Daten(true, id, "Leerprobe")) });

            Assert.True(e.Ok, e.Meldung);
            Assert.True(Leer(Roh("Tab_Stromspeicher", id, "Selbstentladung_Prozent_Monat")), "Selbstentladung_Prozent_Monat");
            Assert.True(Leer(Roh("Tab_Stromspeicher", id, "Standby_Verbrauch")), "Standby_Verbrauch");
        }

        [Fact]
        public void Ein_Verstoss_im_zweiten_Satz_schreibt_keinen()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(StromspeicherStammCtrl.TABLE, 2);
            ids.ForEach(id => Sperren(id, false));
            string vorher = Feld(false, ids[0], ModulKatalogProfil.FeldFirma);

            StromspeicherModel b = Daten(false, ids[1], "Sammel 2");
            b.m_Ladezustand = 150;
            var e = StromspeicherStammCtrl.SchreibenAlle(false, new[]
            {
                new StromspeicherStammCtrl.Satzaenderung(ids[0], Daten(false, ids[0], "Sammel 1")),
                new StromspeicherStammCtrl.Satzaenderung(ids[1], b)
            });

            Assert.False(e.Ok);
            Assert.False(string.IsNullOrEmpty(e.Meldung));
            Assert.Equal(vorher, Feld(false, ids[0], ModulKatalogProfil.FeldFirma));
            Assert.Null(StromspeicherStammCtrl.Pruefen(Daten(false, ids[0], "x")));
            Assert.NotNull(StromspeicherStammCtrl.Pruefen(Daten(false, ids[0], "x", leistung: -1)));
        }

        [Fact]
        public void Ein_gesperrter_Katalogsatz_rollt_alles_zurueck_und_wird_genannt()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(StromspeicherStammCtrl.TABLE, 2);
            Sperren(ids[0], false);
            Sperren(ids[1], true);
            string vorher = Feld(false, ids[0], ModulKatalogProfil.FeldFirma);
            string name = Feld(false, ids[1], ModulKatalogProfil.FeldBezeichner);

            var e = StromspeicherStammCtrl.SchreibenAlle(false, new[]
            {
                new StromspeicherStammCtrl.Satzaenderung(ids[0], Daten(false, ids[0], "Sammel 1")),
                new StromspeicherStammCtrl.Satzaenderung(ids[1], Daten(false, ids[1], "Sammel 2"))
            });

            Assert.False(e.Ok);
            Assert.Contains(name, e.Meldung);
            Assert.Equal(vorher, Feld(false, ids[0], ModulKatalogProfil.FeldFirma));
        }

        [Fact]
        public void Eine_fehlende_ID_schreibt_keinen()
        {
            if (!_db.Vorhanden) return;
            int id = Ids(StromspeicherStammCtrl.TABELLE_PROJEKT, 1)[0];
            string vorher = Feld(true, id, ModulKatalogProfil.FeldFirma);

            var e = StromspeicherStammCtrl.SchreibenAlle(true, new[]
            {
                new StromspeicherStammCtrl.Satzaenderung(id, Daten(true, id, "Sammel 1")),
                new StromspeicherStammCtrl.Satzaenderung(987654, new StromspeicherModel())
            });

            Assert.False(e.Ok);
            Assert.Contains("987654", e.Meldung);
            Assert.Equal(vorher, Feld(true, id, ModulKatalogProfil.FeldFirma));
        }

        [Fact]
        public void Ohne_Saetze_ist_nichts_zu_tun()
        {
            Assert.True(StromspeicherStammCtrl.SchreibenAlle(false, Array.Empty<StromspeicherStammCtrl.Satzaenderung>()).Ok);
        }
    }
}
