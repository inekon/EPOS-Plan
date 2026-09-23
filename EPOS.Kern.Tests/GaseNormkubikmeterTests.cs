using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c — <b>Schritt G (Schemaschritt 109): der Stammtext der fünf Gase auf
    /// Nm³</b> (Entscheid U‑1 Weg (a), Freigabe A9).
    ///
    /// <para>Gemessen an der Testdatenbank (Schemastand 105 → 109): 5 Einheiten, 5
    /// Preiseinheiten und 1 Preiszeile (Projekt 1039, Erdgas E) — danach führt keine der
    /// fünf Stammzeilen und keine Preiszeile eines Gasträgers mehr „m³"; der Brennstoff 24
    /// („Sonstige", kein Gas) bleibt. Die Wirkung: Die Identitätsregel eines Gases lässt
    /// sich aus dem Stammtext ableiten (vorher −1). Die dreizehn Basisprojekte rechnen
    /// Wert für Wert gleich (9.195 von 9.195 Werten samt Emissionen und frischem Lauf).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GaseNormkubikmeterTests
    {
        [Fact]
        public void Der_Zielstand_ist_109_und_der_Schritt_nennt_die_fuenf_Gase()
        {
            Assert.True(SchemaStand.Zielversion >= 109,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 109.");
            Assert.Equal(new[] { 1, 2, 3, 14, 25 }, GaseNormkubikmeter.BRENNSTOFFE);
            Assert.Equal("m³", GaseNormkubikmeter.ALT);
            Assert.Equal("Nm³", GaseNormkubikmeter.NEU);
            Assert.Equal("€/Nm³", GaseNormkubikmeter.PREIS_NEU);
        }

        [Fact]
        public void Die_fuenf_Gase_fuehren_Nm3_und_der_Brennstoff_24_bleibt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (int id in GaseNormkubikmeter.BRENNSTOFFE)
            {
                Assert.Equal("Nm³", Text("SELECT Einheit FROM Tab_Brennstoff_Stamm WHERE ID = " + id));
                Assert.Equal("€/Nm³", Text("SELECT PreisEinheit FROM Tab_Brennstoff_Stamm WHERE ID = " + id));
            }
            Assert.Equal("m³", Text("SELECT Einheit FROM Tab_Brennstoff_Stamm WHERE ID = 24"));

            Assert.Equal(0, GaseNormkubikmeter.Offen());
            Assert.Equal("Nm³", Text("SELECT arbeitspreis_unit FROM energy_price WHERE id = 10141"));

            // Die Emissionsfaktoren des Stamms (Einfrierliste) sind unberührt.
            Assert.Equal(240.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT CO2 FROM Tab_Brennstoff_Stamm WHERE ID = 3")), 9);

            // Wiederholbar: Der zweite Lauf trifft nichts.
            GaseNormkubikmeter.Bericht zweiter = GaseNormkubikmeter.Ausfuehren();
            Assert.Equal(0, zweiter.Einheiten + zweiter.Preiseinheiten + zweiter.Preiszeilen);
        }

        /// <summary>
        /// Die Wirkung des Schrittes: Die Zuordnung eines Gasträgers leitet ihre
        /// Identitätsregel aus dem Stammtext ab — mit „Nm³" findet sie die Regel
        /// „Nm³ → Nm³" (Erdgas E: 40), mit dem alten „m³" fand sie keine (−1).
        /// </summary>
        [Fact]
        public void Die_Identitaetsregel_eines_Gases_ist_wieder_ableitbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string einheit = Text("SELECT Einheit FROM Tab_Brennstoff_Stamm WHERE ID = 3");
            Assert.Equal(40, WizardCtrl.ConvIdErmitteln(3, einheit));
            Assert.Equal(-1, WizardCtrl.ConvIdErmitteln(3, "m³"));
        }

        private static string Text(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? null : Convert.ToString(o);
        }
    }
}
