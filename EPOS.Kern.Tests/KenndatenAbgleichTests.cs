using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="KenndatenCtrl.Abgleichen"/> (iU9-W7.0d) auf einer Arbeitskopie der
    /// Testdatenbank — loeschen, anlegen und aendern in EINER Transaktion.
    ///
    /// <para>Bis Welle 7 stand dieser Weg als <c>RowState</c>-Schleife in
    /// <c>Form_WP.btn_Kenndaten_Click</c> und war nur am Windows-Geraet pruefbar. Er
    /// entscheidet ueber den SIMULATIONSEINGANG: <c>Tab_Kenndaten_STAMM</c> traegt die
    /// Kennlinien, aus denen die Waermepumpe ihren COP und ihre Leistung nimmt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KenndatenAbgleichTests
    {
        /// <summary>Ein Stammgeraet, das Kennlinien hat — sonst ist nichts zu vergleichen.</summary>
        private static int GeraetMitKennlinien()
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID_WP FROM " + WPStammCtrl.CURVE + " GROUP BY ID_WP ORDER BY COUNT(*) DESC LIMIT 1");
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static List<KenndatenModel> IstStand(int idWp)
        {
            var l = new List<KenndatenModel>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm FROM " + WPStammCtrl.CURVE +
                " WHERE ID_WP = ? ORDER BY ID",
                new DbParam("@id", idWp));
            if (dt == null) return l;
            foreach (DataRow r in dt.Rows)
                l.Add(new KenndatenModel
                {
                    m_ID = Convert.ToInt32(r["ID"]),
                    m_ID_WP = Convert.ToInt32(r["ID_WP"]),
                    m_nVorlauf = Convert.ToInt32(r["Vorlauf"]),
                    m_nTemperatur = Convert.ToInt32(r["Temperatur"]),
                    m_nCOP = Convert.ToDouble(r["COP"]),
                    m_nPTherm = Convert.ToDouble(r["Ptherm"])
                });
            return l;
        }

        [Fact]
        public void Ein_unveraenderter_Sollstand_laesst_alles_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int idWp = GeraetMitKennlinien();
            Assert.True(idWp > 0, "Kein Stammgeraet mit Kennlinien in der Testdatenbank.");

            var vorher = IstStand(idWp);
            Assert.True(KenndatenCtrl.Abgleichen(idWp, vorher));

            var nachher = IstStand(idWp);
            Assert.Equal(vorher.Select(m => m.m_ID), nachher.Select(m => m.m_ID));
            Assert.Equal(vorher.Select(m => m.m_nCOP), nachher.Select(m => m.m_nCOP));
        }

        [Fact]
        public void Eine_fehlende_Zeile_wird_geloescht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int idWp = GeraetMitKennlinien();
            var vorher = IstStand(idWp);
            Assert.True(vorher.Count >= 2);

            int weg = vorher[0].m_ID;
            var soll = vorher.Skip(1).ToList();
            Assert.True(KenndatenCtrl.Abgleichen(idWp, soll));

            var nachher = IstStand(idWp);
            Assert.Equal(vorher.Count - 1, nachher.Count);
            Assert.DoesNotContain(nachher, m => m.m_ID == weg);
        }

        [Fact]
        public void Eine_Zeile_ohne_Id_wird_angelegt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int idWp = GeraetMitKennlinien();
            var vorher = IstStand(idWp);

            object maxVorher = DataRepository.ExecuteScalar("SELECT Max(ID) FROM " + WPStammCtrl.CURVE);
            int erwarteteId = Convert.ToInt32(maxVorher) + 1;

            var soll = new List<KenndatenModel>(vorher)
            {
                new KenndatenModel { m_ID = 0, m_nVorlauf = 65, m_nTemperatur = -12, m_nCOP = 1.75, m_nPTherm = 4.25 }
            };
            Assert.True(KenndatenCtrl.Abgleichen(idWp, soll));

            var nachher = IstStand(idWp);
            Assert.Equal(vorher.Count + 1, nachher.Count);

            KenndatenModel neu = nachher.Single(m => m.m_ID == erwarteteId);
            Assert.Equal(idWp, neu.m_ID_WP);
            Assert.Equal(65, neu.m_nVorlauf);
            Assert.Equal(-12, neu.m_nTemperatur);
            Assert.Equal(1.75, neu.m_nCOP, 6);
            Assert.Equal(4.25, neu.m_nPTherm, 6);
        }

        [Fact]
        public void Zwei_neue_Zeilen_bekommen_fortlaufende_Ids()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int idWp = GeraetMitKennlinien();
            object maxVorher = DataRepository.ExecuteScalar("SELECT Max(ID) FROM " + WPStammCtrl.CURVE);
            int erste = Convert.ToInt32(maxVorher) + 1;

            var soll = new List<KenndatenModel>(IstStand(idWp))
            {
                new KenndatenModel { m_nVorlauf = 65, m_nTemperatur = -12, m_nCOP = 1.7, m_nPTherm = 4.2 },
                new KenndatenModel { m_nVorlauf = 65, m_nTemperatur = -7,  m_nCOP = 2.1, m_nPTherm = 5.0 }
            };
            Assert.True(KenndatenCtrl.Abgleichen(idWp, soll));

            var nachher = IstStand(idWp);
            Assert.Contains(nachher, m => m.m_ID == erste);
            Assert.Contains(nachher, m => m.m_ID == erste + 1);
        }

        [Fact]
        public void Ein_geaenderter_Wert_wird_geschrieben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int idWp = GeraetMitKennlinien();
            var soll = IstStand(idWp);
            int id = soll[0].m_ID;
            soll[0].m_nCOP = 9.875;
            soll[0].m_nTemperatur = 42;

            Assert.True(KenndatenCtrl.Abgleichen(idWp, soll));

            KenndatenModel nachher = IstStand(idWp).Single(m => m.m_ID == id);
            Assert.Equal(9.875, nachher.m_nCOP, 6);
            Assert.Equal(42, nachher.m_nTemperatur);
        }

        [Fact]
        public void Loeschen_Anlegen_und_Aendern_gehen_in_EINEM_Durchgang()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int idWp = GeraetMitKennlinien();
            var vorher = IstStand(idWp);
            Assert.True(vorher.Count >= 2);

            int weg = vorher[0].m_ID;
            int geaendert = vorher[1].m_ID;

            var soll = vorher.Skip(1).ToList();
            soll[0].m_nPTherm = 12.5;
            soll.Add(new KenndatenModel { m_nVorlauf = 65, m_nTemperatur = 3, m_nCOP = 3.3, m_nPTherm = 7.7 });

            Assert.True(KenndatenCtrl.Abgleichen(idWp, soll));

            var nachher = IstStand(idWp);
            Assert.Equal(vorher.Count, nachher.Count);                       // eine weg, eine dazu
            Assert.DoesNotContain(nachher, m => m.m_ID == weg);
            Assert.Equal(12.5, nachher.Single(m => m.m_ID == geaendert).m_nPTherm, 6);
            Assert.Contains(nachher, m => m.m_nVorlauf == 65 && m.m_nTemperatur == 3);
        }

        [Fact]
        public void Ein_leerer_Sollstand_raeumt_das_Geraet_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int idWp = GeraetMitKennlinien();
            Assert.NotEmpty(IstStand(idWp));

            Assert.True(KenndatenCtrl.Abgleichen(idWp, new List<KenndatenModel>()));
            Assert.Empty(IstStand(idWp));
        }

        [Fact]
        public void Ohne_Geraet_wird_nichts_geschrieben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            object vorher = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + WPStammCtrl.CURVE);
            Assert.False(KenndatenCtrl.Abgleichen(0, new List<KenndatenModel>
            {
                new KenndatenModel { m_nVorlauf = 35, m_nTemperatur = 0, m_nCOP = 3, m_nPTherm = 5 }
            }));
            object nachher = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + WPStammCtrl.CURVE);
            Assert.Equal(Convert.ToInt32(vorher), Convert.ToInt32(nachher));
        }

        [Fact]
        public void Fremde_Geraete_bleiben_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int idWp = GeraetMitKennlinien();
            object fremdVorher = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + WPStammCtrl.CURVE + " WHERE ID_WP <> ?", new DbParam("@id", idWp));

            Assert.True(KenndatenCtrl.Abgleichen(idWp, new List<KenndatenModel>()));

            object fremdNachher = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + WPStammCtrl.CURVE + " WHERE ID_WP <> ?", new DbParam("@id", idWp));
            Assert.Equal(Convert.ToInt32(fremdVorher), Convert.ToInt32(fremdNachher));
        }
    }
}
