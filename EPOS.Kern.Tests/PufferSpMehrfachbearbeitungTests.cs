using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Mehrfach-Bearbeiten der Pufferspeicher in EINER Transaktion</b> (Konzept Projektdialoge mit
    /// Katalogauswahl 4.6, Entscheid KA‑E‑8, Stufe 3): <see cref="PufferSpStammCtrl.AnzeigefelderSchreibenAlle"/>
    /// schreibt alle geänderten Sätze — Projektkopien oder Katalogsätze — oder keinen; die Investitionskosten gehen
    /// mit, der Speichertyp läuft durch die Bestandsabbildung.
    /// </summary>
    [Collection("Testdatenbank")]
    public class PufferSpMehrfachbearbeitungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        private static PufferSpStammCtrl.AnzeigefelderPufferspeicher Satz(string firma, int volumen = 800,
                                                                           double verluste = 2.5, double invest = 1900,
                                                                           string typ = DbWerte.PSP_SPEICHERTYP_PUFFER)
            => new PufferSpStammCtrl.AnzeigefelderPufferspeicher(firma, typ, verluste, volumen, invest);

        private static List<int> Ids(string tabelle, int anzahl)
        {
            var ids = new List<int>();
            var dt = DataRepository.GetDataTable("SELECT ID FROM [" + tabelle + "] ORDER BY ID LIMIT " +
                                                 anzahl.ToString(CultureInfo.InvariantCulture));
            foreach (System.Data.DataRow r in dt.Rows) ids.Add(Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            return ids;
        }

        private static string Feld(bool projektkopie, int id, string feld)
            => PufferSpStammCtrl.SatzAnzeige(projektkopie, id)[feld];

        private static void Sperren(int id, bool gesperrt)
            => DataRepository.ExecuteSQL("UPDATE [Tab_Pufferspeicher_STAMM] SET ReadOnly = ? WHERE ID = ?",
                                         new DbParam("?", gesperrt ? 1 : 0), new DbParam("?", id));

        [Fact]
        public void SatzAnzeige_liest_Projektkopie_und_Katalogsatz_nach_ID()
        {
            if (!_db.Vorhanden) return;
            int p = Ids(PufferSpStammCtrl.TABELLE_PROJEKT, 1)[0];
            int k = Ids(PufferSpStammCtrl.TABLE, 1)[0];
            string np = Convert.ToString(DataRepository.ExecuteScalar("SELECT Bezeichner FROM Tab_Pufferspeicher WHERE ID = ?", new DbParam("?", p)), CultureInfo.InvariantCulture);
            string nk = Convert.ToString(DataRepository.ExecuteScalar("SELECT Bezeichner FROM Tab_Pufferspeicher_STAMM WHERE ID = ?", new DbParam("?", k)), CultureInfo.InvariantCulture);
            Assert.Equal(np, Feld(true, p, KatalogBrowserProfil.FeldBezeichner));
            Assert.Equal(nk, Feld(false, k, KatalogBrowserProfil.FeldBezeichner));
            var anzeige = PufferSpStammCtrl.SatzAnzeige(true, p);
            foreach (string f in new[] { KatalogBrowserProfil.FeldFirma, KatalogBrowserProfil.FeldSpeichertyp,
                                         KatalogBrowserProfil.FeldVerluste, KatalogBrowserProfil.FeldVolumen,
                                         KatalogBrowserProfil.FeldInvestitionskosten })
                Assert.True(anzeige.ContainsKey(f), f);
            Assert.Null(PufferSpStammCtrl.SatzAnzeige(true, 987654));
        }

        [Fact]
        public void Alle_Projektkopien_werden_geschrieben_der_Katalog_bleibt()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(PufferSpStammCtrl.TABELLE_PROJEKT, 2);
            Assert.Equal(2, ids.Count);
            int katalog = Ids(PufferSpStammCtrl.TABLE, 1)[0];
            string katalogVorher = Feld(false, katalog, KatalogBrowserProfil.FeldFirma);

            var e = PufferSpStammCtrl.AnzeigefelderSchreibenAlle(true, new[]
            {
                new PufferSpStammCtrl.Satzaenderung(ids[0], Satz("Neuwerk A", volumen: 1000, invest: 2100)),
                new PufferSpStammCtrl.Satzaenderung(ids[1], Satz("Neuwerk B", volumen: 1500, invest: 2600)),
            });

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Neuwerk A", Feld(true, ids[0], KatalogBrowserProfil.FeldFirma));
            Assert.Equal("1000", Feld(true, ids[0], KatalogBrowserProfil.FeldVolumen));
            Assert.Equal("2600", Feld(true, ids[1], KatalogBrowserProfil.FeldInvestitionskosten));
            Assert.Equal(katalogVorher, Feld(false, katalog, KatalogBrowserProfil.FeldFirma));
        }

        [Fact]
        public void Katalogsaetze_werden_geschrieben_der_englische_Altwert_des_Typs_wird_umgesetzt()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(PufferSpStammCtrl.TABLE, 2);
            foreach (int id in ids) Sperren(id, false);

            var e = PufferSpStammCtrl.AnzeigefelderSchreibenAlle(false, new[]
            {
                new PufferSpStammCtrl.Satzaenderung(ids[0], Satz("Neuwerk", typ: "Buffer storage")),
                new PufferSpStammCtrl.Satzaenderung(ids[1], Satz("Neuwerk")),
            });

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(DbWerte.PSP_SPEICHERTYP_PUFFER, Feld(false, ids[0], KatalogBrowserProfil.FeldSpeichertyp));
            Assert.Equal("Neuwerk", Feld(false, ids[1], KatalogBrowserProfil.FeldFirma));
        }

        [Fact]
        public void Ein_gesperrter_Katalogsatz_rollt_alles_zurueck_und_nennt_den_Satz()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(PufferSpStammCtrl.TABLE, 2);
            Sperren(ids[0], false);
            Sperren(ids[1], true);
            string vorher = Feld(false, ids[0], KatalogBrowserProfil.FeldFirma);
            string name = Feld(false, ids[1], KatalogBrowserProfil.FeldBezeichner);

            var e = PufferSpStammCtrl.AnzeigefelderSchreibenAlle(false, new[]
            {
                new PufferSpStammCtrl.Satzaenderung(ids[0], Satz("Neuwerk")),
                new PufferSpStammCtrl.Satzaenderung(ids[1], Satz("Neuwerk")),
            });

            Assert.False(e.Ok);
            Assert.Contains(name, e.Meldung);
            Assert.Equal(vorher, Feld(false, ids[0], KatalogBrowserProfil.FeldFirma));
        }

        [Fact]
        public void Ein_Pruefverstoss_oder_eine_fehlende_ID_schreibt_nichts()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(PufferSpStammCtrl.TABELLE_PROJEKT, 2);
            string vorher = Feld(true, ids[0], KatalogBrowserProfil.FeldFirma);

            var verstoss = PufferSpStammCtrl.AnzeigefelderSchreibenAlle(true, new[]
            {
                new PufferSpStammCtrl.Satzaenderung(ids[0], Satz("Neuwerk")),
                new PufferSpStammCtrl.Satzaenderung(ids[1], Satz("Neuwerk", volumen: -5)),
            });
            Assert.False(verstoss.Ok);
            Assert.Equal(vorher, Feld(true, ids[0], KatalogBrowserProfil.FeldFirma));

            var fehlt = PufferSpStammCtrl.AnzeigefelderSchreibenAlle(true, new[]
            {
                new PufferSpStammCtrl.Satzaenderung(ids[0], Satz("Neuwerk")),
                new PufferSpStammCtrl.Satzaenderung(987654, Satz("Neuwerk")),
            });
            Assert.False(fehlt.Ok);
            Assert.Contains("987654", fehlt.Meldung);
            Assert.Equal(vorher, Feld(true, ids[0], KatalogBrowserProfil.FeldFirma));
        }

        [Fact]
        public void Ohne_Saetze_ist_nichts_zu_tun()
        {
            var e = PufferSpStammCtrl.AnzeigefelderSchreibenAlle(true, Array.Empty<PufferSpStammCtrl.Satzaenderung>());
            Assert.True(e.Ok);
        }
    }
}
