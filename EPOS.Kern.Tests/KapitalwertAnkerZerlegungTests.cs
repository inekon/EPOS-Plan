using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E30/4 (#545, Befund B8 der Sichtprüfung 1030, Entscheid E30‑Q11 b) — <b>warum
    /// das BHKW-Referenzprojekt 1030 zwei Kapitalwert-Anker trägt</b>, als Zerlegung
    /// festgehalten.
    ///
    /// <list type="bullet">
    ///   <item><description><b>−21.895.377,28 €</b>: der Kernweg über den GESPEICHERTEN Lauf 212
    ///   vom 30.08.2026 (<see cref="WirtschaftlichkeitAnkerTests"/>, ohne Zeitreihen).</description></item>
    ///   <item><description><b>−31.142.971,06 €</b>: der Berichtsweg, frisch simuliert mit
    ///   Zeitreihen (<c>PvAusweisStromMatrixTests</c>) — der fachliche Anker des Projekts.</description></item>
    /// </list>
    ///
    /// <para><b>Die Ursache ist der veraltete Lauf, nicht die Rechnung.</b> Lauf 212 stammt
    /// von vor Befund B‑1: Seine Kesselzeile führt keinen Brennstoff (5.403,1 MWh Wärme bei
    /// Verbrauch 0, <c>KesselVerbrauchFehlt</c>), und sein BHKW rechnete noch mit dem
    /// Wirkungsgrad vor R10 (1.048,27 statt rund 1.241,5 MWh). Beides fehlt dem Kernweg in
    /// den Energiekosten (Gas 0,08 €/kWh) und in der CO₂-Abgabe (55 €/t); dazu der
    /// geklemmte Netzbezug (E27, +0,39 MWh × 0,25 €/kWh). Die Energiekosten liegen damit
    /// um 447.807,10 €/a, die CO₂-Abgabe um 73.872,08 €/a höher; über 20 Jahre mit p_E
    /// (Barwertfaktor 17,7267) erklärt das die Differenz bis auf den KWKG-Split des
    /// Berichtswegs (Zeitreihen, rund 54 € Barwert).</para>
    ///
    /// <para><b>Keine Neubuchung</b> (E30‑Q11 b): Lauf 212 bleibt Vorrichtung weiterer Tests;
    /// der Kernanker heißt deshalb „Kapitalwert des gespeicherten Altlaufs 212".</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KapitalwertAnkerZerlegungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1030;
        private const int ERDGAS_E = 63;

        /// <summary>Barwertfaktor des Endenergie-Topfs von 1030: i = 3 %, p_E = 2 %, 20 a.</summary>
        private static readonly double BWF_PE =
            Enumerable.Range(1, 20).Sum(t => Math.Pow(1.02, t - 1) / Math.Pow(1.03, t));

        private sealed class Weg
        {
            public VariantenDaten Variante;
            public WirtschaftlichkeitErgebnis Ergebnis;
        }

        /// <summary>Der Kernweg: gespeicherter Lauf, ohne Zeitreihen.</summary>
        private static Weg Kernweg()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT, IstStamm = true, Projektname = "Anker " + PROJEKT,
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return new Weg
            {
                Variante = v,
                Ergebnis = new WirtschaftlichkeitCtrl().Berechne(daten, p).First(
                    x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT)
            };
        }

        /// <summary>Der Berichtsweg: frisch simuliert, mit Zeitreihen.</summary>
        private static Weg Berichtsweg()
        {
            BerichtsDatenSammler.VariantenStatus stamm =
                BerichtsDatenSammler.ErmittleStatus(PROJEKT, "").First(s => s.IstStamm);
            BerichtsDaten daten = new BerichtsDatenSammler().Sammle(PROJEKT, stamm.Projektname,
                new List<int>(), true, true, null, CancellationToken.None);
            var ctrl = new WirtschaftlichkeitCtrl();
            return new Weg
            {
                Variante = daten.Varianten.First(x => x.IdProjekt == PROJEKT),
                Ergebnis = ctrl.Berechne(daten, ctrl.LadeParameter(PROJEKT)).Single(
                    x => x.IdProjekt == PROJEKT && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET)
            };
        }

        private static double Bhkw(ErgebnisModel e) => e.BHKW.Module.Sum(m => m.Verbrauch);
        private static double Kessel(ErgebnisModel e) => e.Heizkessel.Module.Sum(m => m.Verbrauch);

        [Fact]
        public void Der_Kernanker_rechnet_mit_dem_Altlauf_212_ohne_Kesselbrennstoff()
        {
            if (!_db.Vorhanden) return;

            Weg alt = Kernweg();
            Assert.Equal(212, alt.Variante.Ergebnis.ID);
            Assert.True(alt.Variante.KesselVerbrauchFehlt, "Lauf 212 führt Kesselbrennstoff.");
            Assert.Equal(0.0, Kessel(alt.Variante.Ergebnis), 9);
            Assert.Equal(5403.1, alt.Variante.Ergebnis.Heizkessel.Module.Sum(m => m.Waerme_Gas), 6);
            Assert.Equal(1048.27, Bhkw(alt.Variante.Ergebnis), 6);
            Assert.Equal(-21895377.28, alt.Ergebnis.Kapitalwert.Value, 2);
            Assert.Equal(1176906.60, alt.Ergebnis.EnergiekostenJahr.Value, 2);
        }

        [Fact]
        public void Die_Differenz_der_Anker_zerlegt_sich_in_Kesselbrennstoff_BHKW_Netzbezug_und_CO2()
        {
            if (!_db.Vorhanden) return;

            Weg alt = Kernweg();
            Weg neu = Berichtsweg();
            Assert.False(neu.Variante.KesselVerbrauchFehlt);
            Assert.Equal(-31142971.06, neu.Ergebnis.Kapitalwert.Value, 2);

            double gas = KostenEmissionRechner.ArbeitspreisJeKwh(PROJEKT, ERDGAS_E).Value;
            Assert.Equal(0.08, gas, 9);

            // Brennstoffe: der Kessel ganz, das BHKW mit seinem Mehrverbrauch.
            double dKessel = Kessel(neu.Variante.Ergebnis) - Kessel(alt.Variante.Ergebnis);
            double dBhkw = Bhkw(neu.Variante.Ergebnis) - Bhkw(alt.Variante.Ergebnis);
            Assert.Equal(5403.1, dKessel, 6);
            Assert.True(dBhkw > 190 && dBhkw < 196, "BHKW-Mehrverbrauch " + dBhkw);
            double dBrennstoff = (neu.Ergebnis.EnergiekostenJahr.Value - neu.Variante.StromkostenNetz.Value)
                               - (alt.Ergebnis.EnergiekostenJahr.Value - alt.Variante.StromkostenNetz.Value);
            Assert.Equal((dKessel + dBhkw) * 1000.0 * gas, dBrennstoff, 2);

            // Netzbezug: die zwölf Überschussstunden der E27-Klemme.
            double dNetz = neu.Variante.StromkostenNetz.Value - alt.Variante.StromkostenNetz.Value;
            Assert.Equal(97.50, dNetz, 2);

            double dEnergie = neu.Ergebnis.EnergiekostenJahr.Value - alt.Ergebnis.EnergiekostenJahr.Value;
            Assert.Equal(447807.10, dEnergie, 2);
            double dCo2 = neu.Ergebnis.CO2AbgabeJahr - alt.Ergebnis.CO2AbgabeJahr;
            Assert.Equal(73872.08, dCo2, 2);

            // Betriebskosten und Investition sind in beiden Wegen gleich.
            Assert.Equal(alt.Ergebnis.BetriebskostenJahr.Value, neu.Ergebnis.BetriebskostenJahr.Value, 9);
            Assert.Equal(alt.Ergebnis.Investition, neu.Ergebnis.Investition, 9);

            // Der Kapitalwert: Energie und CO₂ über 20 Jahre mit p_E, dazu der KWKG-Split des
            // Berichtswegs (Barwert der Einnahmen).
            double dEinnahmen = neu.Ergebnis.BarwertEinnahmen.Value - alt.Ergebnis.BarwertEinnahmen.Value;
            Assert.True(Math.Abs(dEinnahmen) < 100, "KWKG-Split " + dEinnahmen);
            double dKw = neu.Ergebnis.Kapitalwert.Value - alt.Ergebnis.Kapitalwert.Value;
            Assert.Equal(-9247593.78, dKw, 1);
            // Rest unter 5 €: Die Rechnung trägt die ungerundeten Jahresbeträge, die
            // Zerlegung die auf Cent gerundeten.
            double rest = dKw - (-(dEnergie + dCo2) * BWF_PE + dEinnahmen);
            Assert.True(Math.Abs(rest) < 5.0, "Unerklärter Rest " + rest);
        }
    }
}
