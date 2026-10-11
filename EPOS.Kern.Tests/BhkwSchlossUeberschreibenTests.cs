using System;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ein gesperrter BHKW-Katalogsatz wird nie überschrieben</b> (Anwenderentscheid
    /// 08.10.2026): Der Katalogeditor fragt nicht mehr nach und hebt das Schloss nicht für
    /// einen Vorgang auf. <see cref="BHKWStammCtrl.Ueberschreiben"/> lehnt benannt ab und
    /// nennt den Weg „Schloss aufheben…“; erst danach schreibt er.
    /// Geprüft gegen eine Arbeitskopie der Testdatenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BhkwSchlossUeberschreibenTests
    {
        private static DataRow GesperrterSatz()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner FROM " + BHKWStammCtrl.TABLE + " WHERE ReadOnly = 1 ORDER BY ID LIMIT 1");
            Assert.NotNull(dt);
            Assert.Equal(1, dt.Rows.Count);
            return dt.Rows[0];
        }

        private static string Beschreibung(string name) => Convert.ToString(DataRepository.ExecuteScalar(
            "SELECT Beschreibung FROM " + BHKWStammCtrl.TABLE + " WHERE Bezeichner = ?",
            new DbParam("@name", name)), CultureInfo.InvariantCulture);

        [Fact]
        public void Neuanlage_und_Ueberschreiben_eines_gesperrten_Satzes_werden_abgelehnt()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                DataRow satz = GesperrterSatz();
                string name = Convert.ToString(satz["Bezeichner"], CultureInfo.InvariantCulture);
                string vorher = Beschreibung(name);
                var ctrl = new BHKWStammCtrl();

                // „Neu…“ unter dem Namen des gesperrten Satzes: abgelehnt, nichts geschrieben.
                BHKWStammModel neu = ctrl.ReadModel(name);
                neu.m_szBeschreibung = "Neuanlage-Probe";
                BHKWStammCtrl.SpeicherErgebnis anlage = BHKWStammCtrl.Anlegen(neu, name);
                Assert.False(anlage.Ok);
                Assert.Equal(vorher, Beschreibung(name));

                // „Überschreiben“: benannt abgelehnt mit dem Weg über das Schloss.
                BHKWStammModel geaendert = ctrl.ReadModel(name);
                geaendert.m_szBeschreibung = "Ueberschreib-Probe";
                BHKWStammCtrl.SpeicherErgebnis e = BHKWStammCtrl.Ueberschreiben(geaendert);

                Assert.False(e.Ok);
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ADM_SCHLOSS_ERST_AUFHEBEN, e.Meldung);
                Assert.Equal(vorher, Beschreibung(name));
                Assert.True(ctrl.IsReadOnly(name));
            }
        }

        [Fact]
        public void Nach_Schloss_aufheben_wird_der_Satz_ueberschrieben()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                DataRow satz = GesperrterSatz();
                int id = Convert.ToInt32(satz["ID"], CultureInfo.InvariantCulture);
                string name = Convert.ToString(satz["Bezeichner"], CultureInfo.InvariantCulture);

                Assert.True(BHKWStammCtrl.SchlossSetzen(new[] { id }, false).Ok);

                BHKWStammModel geaendert = new BHKWStammCtrl().ReadModel(name);
                geaendert.m_szBeschreibung = "Ueberschreib-Probe";
                BHKWStammCtrl.SpeicherErgebnis e = BHKWStammCtrl.Ueberschreiben(geaendert);

                Assert.True(e.Ok, e.Meldung);
                Assert.Equal("Ueberschreib-Probe", Beschreibung(name));
            }
        }

        [Fact]
        public void Ein_ungesperrter_Satz_wird_ohne_Freigabe_ueberschrieben()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                string quelle = Convert.ToString(GesperrterSatz()["Bezeichner"], CultureInfo.InvariantCulture);
                BHKWStammModel kopie = new BHKWStammCtrl().ReadModel(quelle);
                const string name = "Schlossprobe eigener Satz";

                BHKWStammCtrl.SpeicherErgebnis anlage = BHKWStammCtrl.Anlegen(kopie, name);
                Assert.True(anlage.Ok, anlage.Meldung);
                Assert.False(new BHKWStammCtrl().IsReadOnly(name));

                BHKWStammModel geaendert = new BHKWStammCtrl().ReadModel(name);
                geaendert.m_szBeschreibung = "Ueberschreib-Probe";
                BHKWStammCtrl.SpeicherErgebnis e = BHKWStammCtrl.Ueberschreiben(geaendert);

                Assert.True(e.Ok, e.Meldung);
                Assert.Equal("Ueberschreib-Probe", Beschreibung(name));
            }
        }
    }
}
