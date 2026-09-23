using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c — <b>B‑4 Rest</b> (Konzept § 4): Die Bemessungsarten
    /// <c>PROZENT_BRENNSTOFFKOSTEN</c> und <c>PROZENT_STROMKOSTEN</c> beziehen ihre
    /// Bezugsgröße frisch aus dem jüngsten Lauf wie <c>EUR_PRO_H</c> und
    /// <c>EUR_PRO_KWH_*</c> — die projektweiten Brennstoff- bzw. Stromkosten; die
    /// Menge-Spalte ist Konserve nur, wo frisch nichts ermittelbar ist.
    ///
    /// <para><b>A/B an 1030</b> (die Position „Hilfsenergiekosten (Speicherladepumpe)"
    /// umgestellt auf 3 % mit Konserve 10.000 €; gemessen 23.09.2026): vorher
    /// 300,00 €/a aus der Konserve, Betriebskosten 20.300,00 €/a, Kapitalwert
    /// −21.900.695,28 €; nachher mit den Brennstoffkosten des Laufs 516.109,60 €/a →
    /// 15.483,29 €/a, Betriebskosten 35.483,29 €/a, Kapitalwert −22.169.844,81 €; mit den
    /// Stromkosten des Laufs (4.357,78 MWh × 0,25 €/kWh = 1.089.445,00 €/a) →
    /// Betriebskosten 52.683,35 €/a, Kapitalwert −22.474.745,07 €. Die dreizehn
    /// Basisprojekte ändern sich nicht (die eine Zeile dieser Art, 1018, trägt keinen
    /// Satz).</para>
    ///
    /// <para><b>Anmerkung zu 1030:</b> Der gespeicherte Lauf stammt von vor Befund B‑1
    /// und führt am Kessel keinen Verbrauch; die Bezugsgröße nimmt — wie Weg A („% der
    /// Endenergiekosten") — den aus der Wärme abgeleiteten Brennstoff
    /// (<c>HilfsstromRechner.KesselBrennstoffMWh</c>, #363) und liegt deshalb über dem
    /// Brennstoffanteil der gebuchten Energiekosten.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektkostenArtenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;
        private const int ZEILE = 101600584;   // Hilfsenergiekosten (Speicherladepumpe)

        private static void Umstellen(string bemessung)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWerte SET Bemessung = ?, Einheitpreis = 3, Menge = 10000 WHERE ID = ?",
                new DbParam("@b", bemessung), new DbParam("@id", ZEILE));
        }

        private static WirtschaftlichkeitErgebnis Rechne()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT, IstStamm = true, Projektname = "Probe " + PROJEKT,
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return new WirtschaftlichkeitCtrl().Berechne(daten, p).First(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
        }

        [Fact]
        public void Prozent_der_Brennstoffkosten_bezieht_die_Brennstoffkosten_des_Laufs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Umstellen(DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN);

            Dictionary<int, KostenPositionNachweis> karte =
                WirtschaftlichkeitCtrl.BetriebNachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(516109.60, karte[ZEILE].Menge.Value, 2);     // alt: 10.000 (Konserve)
            Assert.Equal(15483.29, karte[ZEILE].BetragJahr, 2);       // alt: 300,00

            WirtschaftlichkeitErgebnis e = Rechne();
            Assert.Equal(35483.29, e.BetriebskostenJahr.Value, 2);    // alt: 20.300,00
            Assert.Equal(-22169844.81, e.Kapitalwert.Value, 2);       // alt: −21.900.695,28
        }

        [Fact]
        public void Prozent_der_Stromkosten_bezieht_die_Stromkosten_des_Laufs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Umstellen(DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN);

            Dictionary<int, KostenPositionNachweis> karte =
                WirtschaftlichkeitCtrl.BetriebNachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(4357.78 * 1000.0 * 0.25, karte[ZEILE].Menge.Value, 2);   // 1.089.445,00
            Assert.Equal(32683.35, karte[ZEILE].BetragJahr, 2);

            WirtschaftlichkeitErgebnis e = Rechne();
            Assert.Equal(52683.35, e.BetriebskostenJahr.Value, 2);    // alt: 20.300,00
            Assert.Equal(-22474745.07, e.Kapitalwert.Value, 2);       // alt: −21.900.695,28
        }

        /// <summary>Ohne ermittelbare Bezugsgröße — 1018 führt für Erdgas E keinen
        /// Arbeitspreis — bleibt die gepflegte Menge (Konserve), und der Grund nennt den
        /// fehlenden Preis statt „Konserve".</summary>
        [Fact]
        public void Ohne_Preis_bleibt_die_Konserve_und_der_Grund_nennt_den_Preis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWerte SET Einheitpreis = 3, Menge = 10000 WHERE ID = 101600562");

            Dictionary<int, KostenPositionNachweis> karte =
                WirtschaftlichkeitCtrl.BetriebNachId(1018, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(10000.0, karte[101600562].Menge.Value, 6);
            Assert.Equal(300.0, karte[101600562].BetragJahr, 6);

            string grund;
            Assert.Null(WirtschaftlichkeitCtrl.FrischeBasis(101600562, null, out grund));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_PREIS, grund);
        }
    }
}
