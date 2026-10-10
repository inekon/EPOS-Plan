using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Mehrfach-Bearbeiten der BHKW-Module in EINER Transaktion</b> (Konzept Projektdialoge mit
    /// Katalogauswahl 4.6, Entscheid KA‑E‑8, Stufe 3): <see cref="BHKWStammCtrl.AnzeigefelderSchreibenAlle"/>
    /// schreibt alle geänderten Sätze — Projektkopien oder Katalogsätze — oder keinen; die fünf Kostenposten gehen
    /// mit, die Investition je kWel wird nachgerechnet.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BhkwMehrfachbearbeitungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        private static BHKWStammCtrl.AnzeigefelderBhkw Satz(string beschreibung, double ptherm = 80, double pel = 40,
                                                            double? modul = null, double? montage = null,
                                                            double? lieferung = null, double? schall = null,
                                                            double? abgas = null, double? wartung = null,
                                                            string anfahr = null)
            => new BHKWStammCtrl.AnzeigefelderBhkw("Musterwerk", ptherm, pel, 30, 80, 60,
                                                   Beschreibung: beschreibung,
                                                   KostenModul: modul, KostenMontage: montage, KostenLieferung: lieferung,
                                                   KostenSchallschutzhaube: schall, KostenAbgasreinigung: abgas,
                                                   WartungskostenJeKWhel: wartung, AnfahrverlustKwh: anfahr);

        private static List<int> Ids(string tabelle, int anzahl)
        {
            var ids = new List<int>();
            var dt = DataRepository.GetDataTable("SELECT ID FROM [" + tabelle + "] ORDER BY ID LIMIT " +
                                                 anzahl.ToString(CultureInfo.InvariantCulture));
            foreach (System.Data.DataRow r in dt.Rows) ids.Add(Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            return ids;
        }

        private static string Feld(bool projektkopie, int id, string feld)
            => BHKWStammCtrl.SatzAnzeige(projektkopie, id)[feld];

        private static double Spalte(string tabelle, int id, string spalte)
            => Convert.ToDouble(DataRepository.ExecuteScalar("SELECT [" + spalte + "] FROM [" + tabelle + "] WHERE ID = ?",
                                                             new DbParam("?", id)), CultureInfo.InvariantCulture);

        private static void Sperren(int id, bool gesperrt)
            => DataRepository.ExecuteSQL("UPDATE [Tab_BHKW_STAMM] SET ReadOnly = ? WHERE ID = ?",
                                         new DbParam("?", gesperrt ? 1 : 0), new DbParam("?", id));

        [Fact]
        public void SatzAnzeige_liest_Projektkopie_und_Katalogsatz_nach_ID_samt_Nebenposten()
        {
            if (!_db.Vorhanden) return;
            int p = Ids(BHKWStammCtrl.TABELLE_PROJEKT, 1)[0];
            int k = Ids(BHKWStammCtrl.TABLE, 1)[0];
            string np = Convert.ToString(DataRepository.ExecuteScalar("SELECT Bezeichner FROM Tab_BHKW WHERE ID = ?", new DbParam("?", p)), CultureInfo.InvariantCulture);
            string nk = Convert.ToString(DataRepository.ExecuteScalar("SELECT Bezeichner FROM Tab_BHKW_STAMM WHERE ID = ?", new DbParam("?", k)), CultureInfo.InvariantCulture);
            Assert.Equal(np, Feld(true, p, KatalogBrowserProfil.FeldBezeichner));
            Assert.Equal(nk, Feld(false, k, KatalogBrowserProfil.FeldBezeichner));
            var anzeige = BHKWStammCtrl.SatzAnzeige(true, p);
            foreach (string f in new[] { KatalogBrowserProfil.FeldKostenModul, KatalogBrowserProfil.FeldKostenMontage,
                                         KatalogBrowserProfil.FeldKostenLieferung, KatalogBrowserProfil.FeldKostenSchallschutz,
                                         KatalogBrowserProfil.FeldKostenAbgasreinigung, KatalogBrowserProfil.FeldInvestitionJeKwel,
                                         KatalogBrowserProfil.FeldWartungJeKwhel, KatalogBrowserProfil.FeldGrenzleistung })
                Assert.True(anzeige.ContainsKey(f), f);
            Assert.Null(BHKWStammCtrl.SatzAnzeige(true, 987654));
        }

        [Fact]
        public void Alle_Projektkopien_werden_samt_Nebenposten_geschrieben_der_Katalog_bleibt()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(BHKWStammCtrl.TABELLE_PROJEKT, 2);
            Assert.Equal(2, ids.Count);
            int katalog = Ids(BHKWStammCtrl.TABLE, 1)[0];
            string katalogVorher = Feld(false, katalog, KatalogBrowserProfil.FeldBeschreibung);

            var e = BHKWStammCtrl.AnzeigefelderSchreibenAlle(true, new[]
            {
                new BHKWStammCtrl.Satzaenderung(ids[0], Satz("Sammel 1", modul: 30000, montage: 4000, lieferung: 1000,
                                                             schall: 3000, abgas: 2000, wartung: 0.025, anfahr: "1,5")),
                new BHKWStammCtrl.Satzaenderung(ids[1], Satz("Sammel 2"))
            });

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Sammel 1", Feld(true, ids[0], KatalogBrowserProfil.FeldBeschreibung));
            Assert.Equal("Sammel 2", Feld(true, ids[1], KatalogBrowserProfil.FeldBeschreibung));
            Assert.Equal(3000, Spalte("Tab_BHKW", ids[0], "Kosten_Schallschutzhaube"));
            Assert.Equal(2000, Spalte("Tab_BHKW", ids[0], "Kosten_Abgasreinigung"));
            Assert.Equal(0.025, Spalte("Tab_BHKW", ids[0], "Wartungskosten_kwhel"), 9);
            // Die Investition je kWel ist die Ableitung der fünf Posten: 40 000 € / 40 kWel.
            Assert.Equal(1000, Spalte("Tab_BHKW", ids[0], "Investition_kwel"), 6);
            Assert.Equal(1.5, Spalte("Tab_BHKW", ids[0], "Anfahrverlust_kWh"), 9);
            Assert.Equal(katalogVorher, Feld(false, katalog, KatalogBrowserProfil.FeldBeschreibung));
        }

        [Fact]
        public void Ein_Verstoss_im_zweiten_Satz_schreibt_keinen()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(BHKWStammCtrl.TABLE, 2);
            ids.ForEach(id => Sperren(id, false));
            string vorher = Feld(false, ids[0], KatalogBrowserProfil.FeldBeschreibung);

            var e = BHKWStammCtrl.AnzeigefelderSchreibenAlle(false, new[]
            {
                new BHKWStammCtrl.Satzaenderung(ids[0], Satz("Sammel 1")),
                new BHKWStammCtrl.Satzaenderung(ids[1], Satz("Sammel 2", ptherm: -5))
            });

            Assert.False(e.Ok);
            Assert.False(string.IsNullOrEmpty(e.Meldung));
            Assert.Equal(vorher, Feld(false, ids[0], KatalogBrowserProfil.FeldBeschreibung));
        }

        [Fact]
        public void Ein_gesperrter_Katalogsatz_rollt_alles_zurueck_und_wird_genannt()
        {
            if (!_db.Vorhanden) return;
            var ids = Ids(BHKWStammCtrl.TABLE, 2);
            Sperren(ids[0], false);
            Sperren(ids[1], true);
            string vorher = Feld(false, ids[0], KatalogBrowserProfil.FeldBeschreibung);
            string name = Feld(false, ids[1], KatalogBrowserProfil.FeldBezeichner);

            var e = BHKWStammCtrl.AnzeigefelderSchreibenAlle(false, new[]
            {
                new BHKWStammCtrl.Satzaenderung(ids[0], Satz("Sammel 1")),
                new BHKWStammCtrl.Satzaenderung(ids[1], Satz("Sammel 2"))
            });

            Assert.False(e.Ok);
            Assert.Contains(name, e.Meldung);
            Assert.Equal(vorher, Feld(false, ids[0], KatalogBrowserProfil.FeldBeschreibung));
        }

        [Fact]
        public void Eine_fehlende_ID_schreibt_keinen()
        {
            if (!_db.Vorhanden) return;
            int id = Ids(BHKWStammCtrl.TABELLE_PROJEKT, 1)[0];
            string vorher = Feld(true, id, KatalogBrowserProfil.FeldBeschreibung);

            var e = BHKWStammCtrl.AnzeigefelderSchreibenAlle(true, new[]
            {
                new BHKWStammCtrl.Satzaenderung(id, Satz("Sammel 1")),
                new BHKWStammCtrl.Satzaenderung(987654, Satz("Sammel 2"))
            });

            Assert.False(e.Ok);
            Assert.Contains("987654", e.Meldung);
            Assert.Equal(vorher, Feld(true, id, KatalogBrowserProfil.FeldBeschreibung));
        }

        [Fact]
        public void Ohne_Saetze_ist_nichts_zu_tun()
        {
            Assert.True(BHKWStammCtrl.AnzeigefelderSchreibenAlle(false, Array.Empty<BHKWStammCtrl.Satzaenderung>()).Ok);
        }
    }
}
