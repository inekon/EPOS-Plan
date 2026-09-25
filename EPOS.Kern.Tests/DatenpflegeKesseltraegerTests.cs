using System;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E24 (Konzept Wirtschaftlichkeit § 6.3 Nr. 24, Anwender 25.09.2026) — die
    /// Datenpflege der Testdatenbank an zwei Referenzprojekten, festgehalten als Fakt:
    /// <list type="bullet">
    ///   <item><description><b>1018</b> „BHKW Test München": der Kessel (Anlage 10369,
    ///   Gerät 1018251, Brennstoff 3) trägt den Energieträger 63 „Erdgas E" wie das BHKW
    ///   11327 desselben Projekts. Einen Gaspreis bekommt 1018 bewusst NICHT (E24‑Q4):
    ///   „1018 ohne Arbeitspreis für Erdgas E" bleibt Prüffall
    ///   (<see cref="ProjektkostenArtenTests"/>).</description></item>
    ///   <item><description><b>1023</b> „Wöhler - Test1": der Kessel (Anlage 11205, Gerät
    ///   1018254) trägt Träger 63, dazu die Projektzeile 10130 und der Preisstand 10185
    ///   als Kopie der Zeilen von 1030 (0,84 €/Nm³, 1 200 €/a, CO₂ 240 g/kWh als
    ///   Projektwert, E24‑Q1 a und Q2).</description></item>
    /// </list>
    /// <para><b>Rechenwirkung</b> (Referenzbasis <c>2026-09-25_R17_Datenpflege</c>): allein
    /// <c>HeizkesselModul[0].carrier_id</c> in 1018 und 1023; die Emissionen bleiben, weil
    /// der Projektwert 240 vor dem Katalogwert 201 steht. Die gebuchten Ergebnisse der
    /// Gruppe „Wöhler" bleiben „ohne Nachweis" — die Pflege berührt keine Ergebniszeile.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class DatenpflegeKesseltraegerTests
    {
        private const int ERDGAS_E = 63;

        private static int? Traeger(int anlage)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID_Carrier FROM Tab_Energieanlagen WHERE ID = ?",
                new DbParam("@a", DbParamTyp.Integer) { Wert = anlage });
            Assert.Equal(1, dt.Rows.Count);
            object o = dt.Rows[0][0];
            return o == DBNull.Value ? (int?)null : Convert.ToInt32(o);
        }

        [Theory]
        [InlineData(1018, 10369)]
        [InlineData(1023, 11205)]
        public void Der_Kessel_traegt_Erdgas_E_und_rechnet_mit_dem_Projektwert(int projekt, int anlage)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(ERDGAS_E, Traeger(anlage));

            Emissionsfaktoren f = Emissionsquelle.Fuer(projekt, ERDGAS_E, 3, DbWerte.EMISSION_MODUS_CO2);
            Assert.True(f.Co2Gepflegt);
            Assert.Equal(240.0, f.Co2GKwh, 6);
            Assert.Equal(EmissionsFaktorLader.EBENE_PROJEKT, f.Ebene);
        }

        /// <summary>1023 trägt jetzt einen Gaspreis — 0,84 €/Nm³ über den Heizwert
        /// 10,5 kWh/Nm³ = 0,08 €/kWh —, 1018 weiterhin keinen (Prüffall, E24‑Q4).</summary>
        [Fact]
        public void Erdgas_hat_in_1023_einen_Preis_und_in_1018_keinen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double? preis1023 = KostenEmissionRechner.ArbeitspreisJeKwh(1023, ERDGAS_E);
            Assert.True(preis1023.HasValue);
            Assert.Equal(0.84 / 10.5, preis1023.Value, 9);
            Assert.Null(KostenEmissionRechner.ArbeitspreisJeKwh(1018, ERDGAS_E));
        }

        /// <summary>Die Gas-Anschlussleistung zählt den Kessel mit, seit er den Träger
        /// trägt: in 1023 allein der Kessel (19,3 kW / 0,874).</summary>
        [Fact]
        public void Die_Anschlussleistung_zaehlt_den_Kessel_von_1023()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(19.3 / 0.874, KostenEmissionRechner.AnschlussleistungKW(1023, ERDGAS_E), 6);
        }
    }
}
