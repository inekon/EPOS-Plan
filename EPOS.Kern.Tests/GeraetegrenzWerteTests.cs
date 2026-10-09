using System;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Gerätegrenzen an der Datenbankgrenze</b> (UB‑E3‑b, <see cref="GeraetegrenzWerte"/>): Stammblatt-Speicherweg,
    /// Projektkopie (<c>WPCtrl.CopyFromStamm</c>, <c>BHKWCtrl.CopyFromStamm</c>), „Duplizieren…" und die Bereiche des
    /// Schemas. Leer bleibt leer.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GeraetegrenzWerteTests : IDisposable
    {
        private const int PROJEKT = 1017;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly Geraetespalten GEPFLEGT =
            new Geraetespalten(6.0, 12.0, 3.5, 40.0, 55.0, 30.0, 2.5, "R744");

        [Fact]
        public void Die_Bereiche_sind_die_des_Schemas()
        {
            Assert.Equal(UebergabegrenzeSchema.WP_SPALTEN.Count - 1, GeraetegrenzWerte.WP_BEREICHE.Count);
            for (int i = 0; i < GeraetegrenzWerte.WP_BEREICHE.Count; i++)
            {
                GeraetegrenzWerte.Bereich b = GeraetegrenzWerte.WP_BEREICHE[i];
                (string spalte, string typ) = UebergabegrenzeSchema.WP_SPALTEN[i];
                Assert.Equal(spalte, b.Spalte);
                Assert.Contains("BETWEEN " + b.Von.ToString(CultureInfo.InvariantCulture) + " AND " +
                                b.Bis.ToString(CultureInfo.InvariantCulture), typ);
            }
            Assert.Contains("BETWEEN 40 AND 90", UebergabegrenzeSchema.BHKW_SPALTE.Typ);
            Assert.Equal(70.0, GeraetegrenzWerte.BHKW_RUECKLAUF_MAX_VORGABE_C);
        }

        [Fact]
        public void Verstoesse_sind_benannt_leer_ist_keiner()
        {
            Assert.Null(GeraetegrenzWerte.WpVerstoss(Geraetespalten.Leer));
            Assert.Null(GeraetegrenzWerte.WpVerstoss(GEPFLEGT));
            string g = GeraetegrenzWerte.WpVerstoss(GEPFLEGT with { RuecklaufMaxC = 75.0 });
            Assert.NotNull(g);
            Assert.Contains("70", g);
            Assert.Null(GeraetegrenzWerte.BhkwVerstoss(null));
            Assert.Null(GeraetegrenzWerte.BhkwVerstoss(70.0));
            Assert.NotNull(GeraetegrenzWerte.BhkwVerstoss(95.0));
            Assert.True(GeraetegrenzWerte.BhkwAuslegungHinweis(70, 70.0));
            Assert.False(GeraetegrenzWerte.BhkwAuslegungHinweis(60, 70.0));
            Assert.False(GeraetegrenzWerte.BhkwAuslegungHinweis(null, 70.0));
        }

        [Fact]
        public void Stammblatt_speichert_die_acht_Spalten_und_weist_einen_Bereich_benannt_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ErsterStammsatz("Tab_WP_STAMM");
            var ctrl = new WPStammCtrl();
            ctrl.ReadAll("ID=" + id);
            WPModel m = ctrl.items[0];
            DataRepository.ExecuteNonQuery("UPDATE Tab_WP_STAMM SET ReadOnly = 0 WHERE ID = ?", new DbParam("@id", id));

            m.Grenzspalten = GEPFLEGT with { RuecklaufMaxC = 75.0 };
            WPStammCtrl.SpeicherErgebnis abgewiesen = new WPStammCtrl().Speichern(m, false);
            Assert.False(abgewiesen.Ok);
            Assert.Equal(Geraetespalten.Leer, GeraetegrenzWerte.WpLesen("Tab_WP_STAMM", id));

            m.Grenzspalten = GEPFLEGT;
            Assert.True(new WPStammCtrl().Speichern(m, false).Ok);
            Assert.Equal(GEPFLEGT, GeraetegrenzWerte.WpLesen("Tab_WP_STAMM", id));

            // Ein Speicherweg ohne die Spalten (null) laesst sie stehen.
            m.Grenzspalten = null;
            Assert.True(new WPStammCtrl().Speichern(m, false).Ok);
            Assert.Equal(GEPFLEGT, GeraetegrenzWerte.WpLesen("Tab_WP_STAMM", id));
        }

        [Fact]
        public void Projektkopie_und_Duplizieren_nehmen_alle_acht_Spalten_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ErsterStammsatz("Tab_WP_STAMM");
            GeraetegrenzWerte.WpSchreiben("Tab_WP_STAMM", id, GEPFLEGT);

            int kopie = new WPCtrl().CopyFromStamm(id, PROJEKT);
            Assert.True(kopie > 0);
            Assert.Equal(GEPFLEGT, GeraetegrenzWerte.WpLesen("Tab_WP", kopie));

            Katalogkopie.Ergebnis dup = WPStammCtrl.Duplizieren(id, "Grenzprobe Duplikat");
            Assert.True(dup.Ok, dup.Meldung);
            Assert.Equal(GEPFLEGT, GeraetegrenzWerte.WpLesen("Tab_WP_STAMM", dup.Id));

            // Leer bleibt leer: ein Katalogsatz ohne Werte gibt eine Kopie ohne Werte.
            int leer = ErsterStammsatz("Tab_WP_STAMM", id);
            GeraetegrenzWerte.WpSchreiben("Tab_WP_STAMM", leer, null);
            int kopieLeer = new WPCtrl().CopyFromStamm(leer, PROJEKT);
            Assert.Equal(Geraetespalten.Leer, GeraetegrenzWerte.WpLesen("Tab_WP", kopieLeer));
        }

        [Fact]
        public void Bhkw_Abschaltgrenze_wird_gespeichert_gelesen_und_ins_Projekt_kopiert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = ErsterStammsatz("Tab_BHKW_STAMM");
            string name = Convert.ToString(DataRepository.ExecuteScalar("SELECT Bezeichner FROM Tab_BHKW_STAMM WHERE ID = ?",
                                                                         new DbParam("@id", id)), CultureInfo.InvariantCulture);
            DataRepository.ExecuteNonQuery("UPDATE Tab_BHKW_STAMM SET ReadOnly = 0 WHERE ID = ?", new DbParam("@id", id));

            BHKWStammModel m = new BHKWStammCtrl().ReadModel(name);
            Assert.Null(m.m_Ruecklauf_Max);
            m.m_Ruecklauf_Max = 95.0;
            Assert.False(BHKWStammCtrl.Ueberschreiben(m).Ok);
            m.m_Ruecklauf_Max = 72.0;
            Assert.True(BHKWStammCtrl.Ueberschreiben(m).Ok);
            Assert.Equal(72.0, new BHKWStammCtrl().ReadModel(name).m_Ruecklauf_Max);

            int kopie = new BHKWCtrl().CopyFromStamm(id, PROJEKT);
            Assert.True(kopie > 0);
            Assert.Equal(72.0, BHKWCtrl.RuecklaufMaxLesen(kopie));
        }

        private static int ErsterStammsatz(string tabelle, int ausser = 0)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM " + tabelle + " WHERE ID <> ?", new DbParam("@id", ausser)), CultureInfo.InvariantCulture);
    }
}
