using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c — <b>Schritt G (Schemaschritt 113): der Stammtext der fünf Gase auf
    /// Nm³</b> (Entscheid U‑1 Weg (a), Freigabe A9), dazu <b>der Brennstoff 24 „Sonstige"
    /// auf kWh</b> (Entscheid E7c2‑Q4 vom 23.09.2026, E7c2/10).
    ///
    /// <para>Gemessen an der Testdatenbank (Schemastand 105 → 113): 5 Einheiten, 5
    /// Preiseinheiten und 1 Preiszeile (Projekt 1039, Erdgas E) — danach führt keine der
    /// fünf Stammzeilen und keine Preiszeile eines Gasträgers mehr „m³". Der Brennstoff 24
    /// führt „kWh"/„€/kWh"; ihn nutzt kein Träger, keine Preiszeile, keine Projektzuordnung
    /// und keine Umrechnungsregel (Testdatenbank 105 und 113, Anwenderdatenbank des
    /// Rechners), sein Stamm trägt Hi = Hs = 0 — also reiner Stammtext, keine
    /// Preisumrechnung. Die Wirkung: Die Identitätsregel eines Gases lässt sich aus dem
    /// Stammtext ableiten (vorher −1). Die dreizehn Basisprojekte rechnen Wert für Wert
    /// gleich (9.195 von 9.195 Werten samt Emissionen und frischem Lauf).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GaseNormkubikmeterTests
    {
        [Fact]
        public void Der_Zielstand_ist_113_und_der_Schritt_nennt_die_fuenf_Gase()
        {
            Assert.True(SchemaStand.Zielversion >= 113,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 113.");
            Assert.Equal(new[] { 1, 2, 3, 14, 25 }, GaseNormkubikmeter.BRENNSTOFFE);
            Assert.Equal("m³", GaseNormkubikmeter.ALT);
            Assert.Equal("Nm³", GaseNormkubikmeter.NEU);
            Assert.Equal("€/Nm³", GaseNormkubikmeter.PREIS_NEU);

            // E7c2-Q4: der Brennstoff 24 auf kWh, wie Strom und Fernwärme.
            Assert.Equal(24, GaseNormkubikmeter.SONSTIGE);
            Assert.Equal("kWh", GaseNormkubikmeter.SONSTIGE_NEU);
            Assert.Equal("€/kWh", GaseNormkubikmeter.SONSTIGE_PREIS_NEU);
        }

        [Fact]
        public void Die_fuenf_Gase_fuehren_Nm3_und_der_Brennstoff_24_kWh()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (int id in GaseNormkubikmeter.BRENNSTOFFE)
            {
                Assert.Equal("Nm³", Text("SELECT Einheit FROM Tab_Brennstoff_Stamm WHERE ID = " + id));
                Assert.Equal("€/Nm³", Text("SELECT PreisEinheit FROM Tab_Brennstoff_Stamm WHERE ID = " + id));
            }
            Assert.Equal("kWh", Text("SELECT Einheit FROM Tab_Brennstoff_Stamm WHERE ID = 24"));
            Assert.Equal("€/kWh", Text("SELECT PreisEinheit FROM Tab_Brennstoff_Stamm WHERE ID = 24"));
            // Die Heizwerte des Stamms bleiben (Hi = Hs = 0; nur der Text wandert).
            Assert.Equal(0.0, Zahl("SELECT Hi FROM Tab_Brennstoff_Stamm WHERE ID = 24"), 9);
            Assert.Equal(0.0, Zahl("SELECT Hs FROM Tab_Brennstoff_Stamm WHERE ID = 24"), 9);

            Assert.Equal(0, GaseNormkubikmeter.Offen());
            Assert.Equal("Nm³", Text("SELECT arbeitspreis_unit FROM energy_price WHERE id = 10141"));

            // Die Emissionsfaktoren des Stamms (Einfrierliste) sind unberührt.
            Assert.Equal(240.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT CO2 FROM Tab_Brennstoff_Stamm WHERE ID = 3")), 9);

            // Wiederholbar: Der zweite Lauf trifft nichts — und die Messung des Brennstoffs
            // 24 fürs Protokoll findet in der Testdatenbank keinen Nutzer.
            GaseNormkubikmeter.Bericht zweiter = GaseNormkubikmeter.Ausfuehren();
            Assert.Equal(0, zweiter.Einheiten + zweiter.Preiseinheiten + zweiter.Preiszeilen);
            Assert.Equal(0, zweiter.SonstigeEinheit + zweiter.SonstigePreiseinheit);
            Assert.Equal(0, zweiter.SonstigeTraeger + zweiter.SonstigePreiszeilen + zweiter.SonstigeZuordnungen);
        }

        /// <summary>
        /// E7c2‑Q4 auf einer Datenbank VOR dem Schritt, die den Brennstoff 24 nutzt: Der
        /// Schritt zieht nur den Stammtext (m³ → kWh, €/m3 → €/kWh). Ein Träger des
        /// Brennstoffs und seine Preiszeile behalten Einheit und Preis — „m³" → „kWh" ist
        /// keine Umbenennung, sondern hieße umrechnen, und dafür fehlt dem Stamm der
        /// Heizwert. Das Protokoll zählt sie; die Nachprobe bleibt 0.
        /// </summary>
        [Fact]
        public void Der_Brennstoff_24_wandert_nur_im_Stammtext()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Der Stand vor dem Schritt, mit einem Nutzer des Brennstoffs 24.
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Brennstoff_Stamm SET Einheit = ?, PreisEinheit = ? WHERE ID = 24",
                new DbParam("@e", "m³"), new DbParam("@p", "€/m3"));
            DataRepository.ExecuteNonQuery(
                "INSERT INTO energy_carrier (ID_Brennstoff, name, billing_unit, hi_kwh_per_unit, is_active) " +
                "VALUES (?, ?, ?, ?, ?)",
                new DbParam("@b", 24), new DbParam("@n", "Probe Sonstige E7c2"), new DbParam("@u", "m³"),
                // (object)0: Eine KONSTANTE 0 waehlte sonst den Typ-Konstruktor
                // DbParam(string, DbParamTyp) - der Wert bliebe NULL.
                new DbParam("@hi", 2.5), new DbParam("@a", (object)0));
            int traeger = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MAX(id) FROM energy_carrier WHERE name = ?", new DbParam("@n", "Probe Sonstige E7c2")));
            DataRepository.ExecuteNonQuery(
                "INSERT INTO energy_price (ID_Projekt, carrier_id, valid_from, arbeitspreis, arbeitspreis_unit) " +
                "VALUES (?, ?, ?, ?, ?)",
                new DbParam("@p", 1039), new DbParam("@c", traeger), new DbParam("@v", "2026-01-01"),
                new DbParam("@ap", 0.5), new DbParam("@u", "m³"));
            Assert.True(GaseNormkubikmeter.Offen() > 0);

            GaseNormkubikmeter.Bericht b = GaseNormkubikmeter.Ausfuehren();

            Assert.Equal(1, b.SonstigeEinheit);
            Assert.Equal(1, b.SonstigePreiseinheit);
            Assert.Equal(1, b.SonstigeTraeger);
            Assert.Equal(1, b.SonstigePreiszeilen);
            Assert.Equal(0, b.SonstigeZuordnungen);
            Assert.Contains("Brennstoff 24: 1 Einheit m³ -> kWh", b.Text());
            Assert.Equal("kWh", Text("SELECT Einheit FROM Tab_Brennstoff_Stamm WHERE ID = 24"));
            Assert.Equal("€/kWh", Text("SELECT PreisEinheit FROM Tab_Brennstoff_Stamm WHERE ID = 24"));
            Assert.Equal(0, GaseNormkubikmeter.Offen());

            // Träger und Preis bleiben, wie sie sind — kein Einheitenwechsel ohne Umrechnung.
            Assert.Equal("m³", Text("SELECT billing_unit FROM energy_carrier WHERE id = " + traeger));
            Assert.Equal("m³", Text("SELECT arbeitspreis_unit FROM energy_price WHERE carrier_id = " + traeger));
            Assert.Equal(0.5, Zahl("SELECT arbeitspreis FROM energy_price WHERE carrier_id = " + traeger), 12);
            // Die übrigen kWh-Brennstoffe sind unberührt.
            Assert.Equal("kWh", Text("SELECT Einheit FROM Tab_Brennstoff_Stamm WHERE ID = 13"));
            Assert.Equal("kWh", Text("SELECT Einheit FROM Tab_Brennstoff_Stamm WHERE ID = 23"));

            // Wiederholbar.
            GaseNormkubikmeter.Bericht zweiter = GaseNormkubikmeter.Ausfuehren();
            Assert.Equal(0, zweiter.SonstigeEinheit + zweiter.SonstigePreiseinheit);
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

        private static double Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? double.NaN : Convert.ToDouble(o);
        }
    }
}
