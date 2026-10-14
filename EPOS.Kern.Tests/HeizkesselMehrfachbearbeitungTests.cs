using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Mehrfach-Bearbeiten der Heizkessel in EINER Transaktion</b> (Konzept Projektdialoge mit
    /// Katalogauswahl 4.6, Entscheid KA‑E‑8): <see cref="HeizkesselStammCtrl.AnzeigefelderSchreibenAlle"/>
    /// schreibt alle geänderten Sätze — Projektkopien oder Katalogsätze — oder keinen.
    /// </summary>
    [Collection("Testdatenbank")]
    public class HeizkesselMehrfachbearbeitungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        private static HeizkesselStammCtrl.AnzeigefelderHeizkessel Satz(string beschreibung, double ptherm = 30)
            => new HeizkesselStammCtrl.AnzeigefelderHeizkessel(beschreibung, ptherm, 100, false, 70, 50);

        private static List<int> Ids(string tabelle, int anzahl)
        {
            var ids = new List<int>();
            var dt = DataRepository.GetDataTable("SELECT ID FROM [" + tabelle + "] ORDER BY ID LIMIT " +
                                                 anzahl.ToString(CultureInfo.InvariantCulture));
            foreach (System.Data.DataRow r in dt.Rows) ids.Add(Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            return ids;
        }

        private static string Beschreibung(bool projektkopie, int id)
            => new HeizkesselStammCtrl().SatzAnzeige(projektkopie, id)[KatalogBrowserProfil.FeldBeschreibung];

        private static void Entsperren(int id)
            => DataRepository.ExecuteSQL("UPDATE [Tab_Heizkessel_STAMM] SET ReadOnly = 0 WHERE ID = ?", new DbParam("?", id));

        [Fact]
        public void SatzAnzeige_liest_Projektkopie_und_Katalogsatz_nach_ID()
        {
            if (!_db.Vorhanden) return;
            int p = Ids(HeizkesselStammCtrl.TABELLE_PROJEKT, 1)[0];
            int k = Ids(HeizkesselStammCtrl.TABLE, 1)[0];
            string np = Convert.ToString(DataRepository.ExecuteScalar("SELECT Bezeichner FROM Tab_Heizkessel WHERE ID = ?", new DbParam("?", p)), CultureInfo.InvariantCulture);
            string nk = Convert.ToString(DataRepository.ExecuteScalar("SELECT Bezeichner FROM Tab_Heizkessel_STAMM WHERE ID = ?", new DbParam("?", k)), CultureInfo.InvariantCulture);
            Assert.Equal(np, new HeizkesselStammCtrl().SatzAnzeige(true, p)[KatalogBrowserProfil.FeldBezeichner]);
            Assert.Equal(nk, new HeizkesselStammCtrl().SatzAnzeige(false, k)[KatalogBrowserProfil.FeldBezeichner]);
            Assert.Null(new HeizkesselStammCtrl().SatzAnzeige(true, 987654));
        }

        [Fact]
        public void Alle_Projektkopien_werden_geschrieben_der_Katalog_bleibt()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(HeizkesselStammCtrl.TABELLE_PROJEKT, 2);
            Assert.Equal(2, ids.Count);
            int katalog = Ids(HeizkesselStammCtrl.TABLE, 1)[0];
            string katalogVorher = Beschreibung(false, katalog);

            var e = HeizkesselStammCtrl.AnzeigefelderSchreibenAlle(true, new[]
            {
                new HeizkesselStammCtrl.Satzaenderung(ids[0], Satz("Sammel 1")),
                new HeizkesselStammCtrl.Satzaenderung(ids[1], Satz("Sammel 2"))
            });

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Sammel 1", Beschreibung(true, ids[0]));
            Assert.Equal("Sammel 2", Beschreibung(true, ids[1]));
            Assert.Equal(katalogVorher, Beschreibung(false, katalog));
        }

        [Fact]
        public void Ein_Verstoss_im_zweiten_Satz_schreibt_keinen()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(HeizkesselStammCtrl.TABLE, 2);
            ids.ForEach(Entsperren);
            string vorher = Beschreibung(false, ids[0]);

            var e = HeizkesselStammCtrl.AnzeigefelderSchreibenAlle(false, new[]
            {
                new HeizkesselStammCtrl.Satzaenderung(ids[0], Satz("Sammel 1")),
                new HeizkesselStammCtrl.Satzaenderung(ids[1], Satz("Sammel 2", ptherm: -5))
            });

            Assert.False(e.Ok);
            Assert.False(string.IsNullOrEmpty(e.Meldung));
            Assert.Equal(vorher, Beschreibung(false, ids[0]));
        }

        [Fact]
        public void Ein_gesperrter_Katalogsatz_rollt_alles_zurueck_und_wird_genannt()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(HeizkesselStammCtrl.TABLE, 2);
            Entsperren(ids[0]);
            DataRepository.ExecuteSQL("UPDATE [Tab_Heizkessel_STAMM] SET ReadOnly = 1 WHERE ID = ?", new DbParam("?", ids[1]));
            string vorher = Beschreibung(false, ids[0]);
            string name = new HeizkesselStammCtrl().SatzAnzeige(false, ids[1])[KatalogBrowserProfil.FeldBezeichner];

            var e = HeizkesselStammCtrl.AnzeigefelderSchreibenAlle(false, new[]
            {
                new HeizkesselStammCtrl.Satzaenderung(ids[0], Satz("Sammel 1")),
                new HeizkesselStammCtrl.Satzaenderung(ids[1], Satz("Sammel 2"))
            });

            Assert.False(e.Ok);
            Assert.Contains(name, e.Meldung);
            Assert.Equal(vorher, Beschreibung(false, ids[0]));
        }

        [Fact]
        public void Eine_fehlende_ID_schreibt_keinen()
        {
            if (!_db.Vorhanden) return;
            int id = Ids(HeizkesselStammCtrl.TABELLE_PROJEKT, 1)[0];
            string vorher = Beschreibung(true, id);

            var e = HeizkesselStammCtrl.AnzeigefelderSchreibenAlle(true, new[]
            {
                new HeizkesselStammCtrl.Satzaenderung(id, Satz("Sammel 1")),
                new HeizkesselStammCtrl.Satzaenderung(987654, Satz("Sammel 2"))
            });

            Assert.False(e.Ok);
            Assert.Contains("987654", e.Meldung);
            Assert.Equal(vorher, Beschreibung(true, id));
        }

        [Fact]
        public void Ohne_Saetze_ist_nichts_zu_tun()
        {
            Assert.True(HeizkesselStammCtrl.AnzeigefelderSchreibenAlle(false, Array.Empty<HeizkesselStammCtrl.Satzaenderung>()).Ok);
        }
    }
}
