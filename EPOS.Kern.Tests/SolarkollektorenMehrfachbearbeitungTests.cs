using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Mehrfach-Bearbeiten der Solarkollektoren in EINER Transaktion</b> (Konzept Projektdialoge mit
    /// Katalogauswahl 4.6, Entscheid KA‑E‑8, Stufe 3): <see cref="SolarkollektorenStammCtrl.AnzeigefelderSchreibenAlle"/>
    /// schreibt alle geänderten Sätze — Projektkopien oder Katalogsätze — oder keinen; die Investitionskosten gehen mit,
    /// der Name bleibt. Gearbeitet wird nur an eigens angelegten Kopien auf einer Kopie der Testdatenbank — der
    /// Kollektorsatz des Referenzprojekts 1049 bleibt unberührt.
    /// </summary>
    [Collection("Testdatenbank")]
    public class SolarkollektorenMehrfachbearbeitungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        /// <summary>Ein Projekt der Testdatenbank, das nicht 1049 ist.</summary>
        private const int PROJEKT = 1030;

        private static SolarkollektorenStammCtrl.AnzeigefelderSolarkollektor Satz(string beschreibung, double h0 = 0.8,
                                                                               double invest = 900)
            => new SolarkollektorenStammCtrl.AnzeigefelderSolarkollektor("Flach", "Musterwerk", beschreibung,
                                                                       2.5, 2.3, h0, 3.5, 0.015, 0.95, 0.9, invest);

        private static List<int> StammIds(int anzahl)
        {
            var ids = new List<int>();
            var dt = DataRepository.GetDataTable("SELECT ID FROM [" + SolarkollektorenStammCtrl.TABLE + "] ORDER BY ID LIMIT " +
                                                 anzahl.ToString(CultureInfo.InvariantCulture));
            foreach (System.Data.DataRow r in dt.Rows) ids.Add(Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            return ids;
        }

        /// <summary>Zwei eigene Projektkopien im Projekt <see cref="PROJEKT"/>.</summary>
        private static List<int> EigeneKopien()
        {
            var ids = new List<int>();
            foreach (int stamm in StammIds(2))
            {
                // Eigener Name: ein vorhandener Satz gleichen Namens im Projekt wuerde wiederverwendet.
                int kopie = new SolarkollektorenCtrl().CopyFromStamm(stamm, PROJEKT);
                Assert.True(kopie > 0);
                DataRepository.ExecuteSQL("UPDATE [Tab_Solarkollektoren] SET Bezeichner = ? WHERE ID = ?",
                                          new DbParam("?", "Mehrfachprobe " + kopie.ToString(CultureInfo.InvariantCulture)),
                                          new DbParam("?", kopie));
                ids.Add(kopie);
            }
            return ids;
        }

        private static string Feld(bool projektkopie, int id, string feld)
            => SolarkollektorenStammCtrl.SatzAnzeige(projektkopie, id)[feld];

        private static void Sperren(int id, bool gesperrt)
            => DataRepository.ExecuteSQL("UPDATE [Tab_Solarkollektoren_STAMM] SET ReadOnly = ? WHERE ID = ?",
                                         new DbParam("?", gesperrt ? 1 : 0), new DbParam("?", id));

        private static string Satz1049()
            => Convert.ToString(DataRepository.ExecuteScalar(
                   "SELECT Bezeichner || '|' || Kdfu || '|' || Bezugsflaeche || '|' || Investitionskosten FROM Tab_Solarkollektoren WHERE ID_Projekt = 1049"),
                   CultureInfo.InvariantCulture);

        [Fact]
        public void SatzAnzeige_liest_Projektkopie_und_Katalogsatz_nach_ID_samt_Investition()
        {
            if (!_db.Vorhanden) return;
            int k = StammIds(1)[0];
            int p = EigeneKopien()[0];
            Assert.StartsWith("Mehrfachprobe", Feld(true, p, KatalogBrowserProfil.FeldBezeichner));
            Assert.Equal(SolarkollektorenStammCtrl.KatalogsatzAnzeige(Feld(false, k, KatalogBrowserProfil.FeldBezeichner))
                             [KatalogBrowserProfil.FeldInvestitionskosten],
                         Feld(false, k, KatalogBrowserProfil.FeldInvestitionskosten));
            Assert.True(SolarkollektorenStammCtrl.SatzAnzeige(true, p).ContainsKey(KatalogBrowserProfil.FeldBezugsflaeche));
            Assert.Null(SolarkollektorenStammCtrl.SatzAnzeige(true, 987654321));
        }

        [Fact]
        public void Alle_Projektkopien_werden_geschrieben_der_Katalog_und_1049_bleiben()
        {
            if (!_db.Vorhanden) return;
            string vorher1049 = Satz1049();
            var ids = EigeneKopien();
            int katalog = StammIds(1)[0];
            string katalogVorher = Feld(false, katalog, KatalogBrowserProfil.FeldBeschreibung);
            string name0 = Feld(true, ids[0], KatalogBrowserProfil.FeldBezeichner);

            var e = SolarkollektorenStammCtrl.AnzeigefelderSchreibenAlle(true, new[]
            {
                new SolarkollektorenStammCtrl.Satzaenderung(ids[0], Satz("Kopie A", invest: 1111)),
                new SolarkollektorenStammCtrl.Satzaenderung(ids[1], Satz("Kopie B", invest: 2222)),
            });

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Kopie A", Feld(true, ids[0], KatalogBrowserProfil.FeldBeschreibung));
            Assert.Equal("Kopie B", Feld(true, ids[1], KatalogBrowserProfil.FeldBeschreibung));
            Assert.Equal(2222, double.Parse(Feld(true, ids[1], KatalogBrowserProfil.FeldInvestitionskosten), CultureInfo.CurrentCulture));
            Assert.Equal(name0, Feld(true, ids[0], KatalogBrowserProfil.FeldBezeichner));   // der Name bleibt
            Assert.Equal(katalogVorher, Feld(false, katalog, KatalogBrowserProfil.FeldBeschreibung));
            Assert.Equal(vorher1049, Satz1049());
        }

        [Fact]
        public void Ein_Verstoss_schreibt_keinen_Satz_und_nennt_den_Namen()
        {
            if (!_db.Vorhanden) return;
            var ids = EigeneKopien();
            string vorher = Feld(true, ids[0], KatalogBrowserProfil.FeldBeschreibung);
            string name1 = Feld(true, ids[1], KatalogBrowserProfil.FeldBezeichner);

            var e = SolarkollektorenStammCtrl.AnzeigefelderSchreibenAlle(true, new[]
            {
                new SolarkollektorenStammCtrl.Satzaenderung(ids[0], Satz("geht")),
                new SolarkollektorenStammCtrl.Satzaenderung(ids[1], Satz("geht nicht", h0: 76.1)),   // Prozent statt Faktor
            });

            Assert.False(e.Ok);
            Assert.Contains(name1, e.Meldung);
            Assert.Equal(vorher, Feld(true, ids[0], KatalogBrowserProfil.FeldBeschreibung));
        }

        [Fact]
        public void Ein_gesperrter_Katalogsatz_rollt_alles_zurueck()
        {
            if (!_db.Vorhanden) return;
            var ids = StammIds(2);
            Sperren(ids[0], false);
            Sperren(ids[1], true);
            string vorher = Feld(false, ids[0], KatalogBrowserProfil.FeldBeschreibung);

            var e = SolarkollektorenStammCtrl.AnzeigefelderSchreibenAlle(false, new[]
            {
                new SolarkollektorenStammCtrl.Satzaenderung(ids[0], Satz("neu")),
                new SolarkollektorenStammCtrl.Satzaenderung(ids[1], Satz("neu")),
            });

            Assert.False(e.Ok);
            Assert.Equal(vorher, Feld(false, ids[0], KatalogBrowserProfil.FeldBeschreibung));
        }

        [Fact]
        public void Ungesperrte_Katalogsaetze_werden_gemeinsam_geschrieben()
        {
            if (!_db.Vorhanden) return;
            var ids = StammIds(2);
            Sperren(ids[0], false);
            Sperren(ids[1], false);

            var e = SolarkollektorenStammCtrl.AnzeigefelderSchreibenAlle(false, new[]
            {
                new SolarkollektorenStammCtrl.Satzaenderung(ids[0], Satz("Katalog A")),
                new SolarkollektorenStammCtrl.Satzaenderung(ids[1], Satz("Katalog B")),
            });

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Katalog A", Feld(false, ids[0], KatalogBrowserProfil.FeldBeschreibung));
            Assert.Equal("Katalog B", Feld(false, ids[1], KatalogBrowserProfil.FeldBeschreibung));
        }

        [Fact]
        public void Eine_fehlende_ID_schreibt_nichts()
        {
            if (!_db.Vorhanden) return;
            var ids = EigeneKopien();
            string vorher = Feld(true, ids[0], KatalogBrowserProfil.FeldBeschreibung);

            var e = SolarkollektorenStammCtrl.AnzeigefelderSchreibenAlle(true, new[]
            {
                new SolarkollektorenStammCtrl.Satzaenderung(ids[0], Satz("neu")),
                new SolarkollektorenStammCtrl.Satzaenderung(987654321, Satz("neu")),
            });

            Assert.False(e.Ok);
            Assert.Contains("987654321", e.Meldung);
            Assert.Equal(vorher, Feld(true, ids[0], KatalogBrowserProfil.FeldBeschreibung));
        }
    }
}
