using System;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die WÄRME-AUTARKIE der Solarthermie je Monat (<see cref="SolarWaermeMonate"/>) — die
    /// Aggregation hinter dem Monatsstapel „Wärmebedarf &amp; Deckung" der Autarkie-Analyse.
    ///
    /// <para>Synthetische Reihen prüfen Kalendermonate, Lücke und Kennzahlen; ein echter
    /// Lauf eines Solarthermie-Projekts der Testdatenbank prüft, dass die Jahressumme
    /// DIESELBE Zahl ist wie die Wärmedeckung der Solarthermie in der Übersicht. Keines der
    /// CI-Referenzprojekte führt Solarthermie; 1026 ist das Beispielprojekt der
    /// Testdatenbank mit Kollektorfeld in der Kaskade (1028 und 1029 führen ein Feld,
    /// aber nicht in der Kaskade, und decken deshalb nichts).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SolarWaermeMonateTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public SolarWaermeMonateTests(TestDatenbank db) { _db = db; }

        private const int PROJEKT_SOLAR = 1026;

        private static double[] Konstant(double wert)
            => Enumerable.Repeat(wert, Kanalsatz.STUNDEN_JAHR).ToArray();

        [Fact]
        public void Kalendermonate_summieren_die_Stunden_und_die_Luecke_ist_der_Rest()
        {
            SolarWaermeMonate w = SolarWaermeMonate.Aggregieren(Konstant(2.0), Konstant(0.5), Konstant(0.25));

            Assert.Equal(31 * 24 * 2.0, w.BedarfKwh[0], 9);
            Assert.Equal(28 * 24 * 2.0, w.BedarfKwh[1], 9);
            Assert.Equal(30 * 24 * 0.5, w.DirektKwh[3], 9);
            Assert.Equal(31 * 24 * 0.25, w.SpeicherKwh[11], 9);
            Assert.Equal(31 * 24 * 1.25, w.LueckeKwh[0], 9);

            Assert.Equal(8760 * 2.0, w.BedarfJahrKwh, 6);
            Assert.Equal(8760 * 0.75, w.SolarJahrKwh, 6);
            Assert.Equal(37.5, w.DeckungsanteilProzent.Value, 9);
            Assert.All(w.DeckungMonatProzent, p => Assert.Equal(37.5, p, 9));
            Assert.True(w.HatSpeicheranteil);
        }

        [Fact]
        public void Die_Luecke_wird_nie_negativ_und_ohne_Bedarf_gibt_es_keinen_Anteil()
        {
            SolarWaermeMonate w = SolarWaermeMonate.Aggregieren(Konstant(0.0), Konstant(1.0), null);

            Assert.All(w.LueckeKwh, l => Assert.Equal(0.0, l));
            Assert.Null(w.DeckungsanteilProzent);
            Assert.All(w.DeckungMonatProzent, p => Assert.Equal(0.0, p));
            Assert.False(w.HatSpeicheranteil);
        }

        [Fact]
        public void Die_Monatszeile_nennt_zwoelf_Monate_und_ohne_Bedarf_einen_Strich()
        {
            using (new Kulturvorrichtung())
            {
                SolarWaermeMonate w = SolarWaermeMonate.Aggregieren(Konstant(2.0), Konstant(0.5), Konstant(0.25));
                Assert.Equal("Solare Deckung je Monat: Jan 38 % · Feb 38 % · Mrz 38 % · Apr 38 % · "
                             + "Mai 38 % · Jun 38 % · Jul 38 % · Aug 38 % · Sep 38 % · Okt 38 % · "
                             + "Nov 38 % · Dez 38 %", w.Deckungszeile());

                SolarWaermeMonate leer = SolarWaermeMonate.Aggregieren(Konstant(0.0), Konstant(1.0), null);
                Assert.StartsWith("Solare Deckung je Monat: Jan – · Feb –", leer.Deckungszeile());
            }

            using (new Kulturvorrichtung("en-US"))
            {
                SolarWaermeMonate w = SolarWaermeMonate.Aggregieren(Konstant(2.0), Konstant(0.5), null);
                Assert.StartsWith("Solar coverage per month: Jan 25 % · Feb 25 % · Mar 25 %", w.Deckungszeile());
            }
        }

        [Fact]
        public void Das_Bild_fuehrt_die_Speicherreihe_nur_mit_Speicheranteil()
        {
            SolarWaermeMonate ohne = SolarWaermeMonate.Aggregieren(Konstant(2.0), Konstant(0.5), null);
            SolarWaermeMonate mit = SolarWaermeMonate.Aggregieren(Konstant(2.0), Konstant(0.5), Konstant(0.25));

            var texte = new WaermeAutarkieBild.Texte();
            Assert.Equal(2, Legenden(WaermeAutarkieBild.Modell(ohne, texte)));
            Assert.Equal(3, Legenden(WaermeAutarkieBild.Modell(mit, texte)));
            Assert.Null(WaermeAutarkieBild.Modell(null));
        }

        private static int Legenden(Zeichenmodell z)
        {
            var marken = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            Sammle(z.Befehle, marken);
            return marken.Count(m => m.StartsWith("legende:", StringComparison.Ordinal));
        }

        private static void Sammle(System.Collections.Generic.IReadOnlyList<Zeichenbefehl> befehle,
                                   System.Collections.Generic.HashSet<string> ziel)
        {
            foreach (Zeichenbefehl b in befehle)
            {
                if (b.Marke != null) ziel.Add(b.Marke);
                if (b is Gruppe g) Sammle(g.Befehle, ziel);
            }
        }

        [Fact]
        public void Ein_Lauf_mit_Solarthermie_deckt_sich_mit_der_Uebersicht()
        {
            if (!_db.Vorhanden) return;

            SimulationRunner l = new SimulationRunner();
            string fehler;
            Assert.True(l.Simuliere(PROJEKT_SOLAR, out fehler), "Lauf gescheitert: " + fehler);

            SolarWaermeMonate w = SolarWaermeMonate.AusLauf(l.sim, l.simulation_Waermebedarf);
            Assert.NotNull(w);
            Assert.True(w.SolarJahrKwh > 0, "Projekt " + PROJEKT_SOLAR + " deckt keine Wärme solar.");

            var u = SimulationErgebnisCtrl.Uebersicht(l.sim, l.simulation_Waermebedarf, l.simulation_Strombedarf);
            Assert.Equal(u.WaermeSolarMwh * 1000.0, w.SolarJahrKwh, 1e-6 * Math.Max(1.0, w.SolarJahrKwh));

            for (int m = 0; m < 12; m++)
            {
                Assert.True(w.LueckeKwh[m] >= 0.0);
                Assert.True(w.DirektKwh[m] + w.SpeicherKwh[m] <= w.BedarfKwh[m] + 1e-6,
                            "Monat " + (m + 1) + ": Solar deckt mehr als den Bedarf.");
                Assert.Equal(w.BedarfKwh[m], w.DirektKwh[m] + w.SpeicherKwh[m] + w.LueckeKwh[m],
                             1e-6 * Math.Max(1.0, w.BedarfKwh[m]));
            }
        }

        /// <summary>
        /// Die Monatszeile des echten Laufs: zwölf Monate, je Monat der gerundete Anteil aus
        /// <see cref="SolarWaermeMonate.DeckungMonatProzent"/>, der Anteil in 0…100 %.
        /// <para>Hinweis: In 1026 steht die Solarthermie an dritter Stelle hinter Wärmepumpe
        /// und Kessel und deckt praktisch nichts (Größenordnung 1e-14 kWh) — die Zeile lautet
        /// dort zwölfmal „0 %". Die Zahlen selbst prüft der synthetische Fall oben.</para>
        /// </summary>
        [Fact]
        public void Die_Monatszeile_eines_Laufs_mit_Solarthermie_folgt_den_Monatsanteilen()
        {
            if (!_db.Vorhanden) return;

            using (new Kulturvorrichtung())
            {
                SimulationRunner l = new SimulationRunner();
                string fehler;
                Assert.True(l.Simuliere(PROJEKT_SOLAR, out fehler), "Lauf gescheitert: " + fehler);

                SolarWaermeMonate w = SolarWaermeMonate.AusLauf(l.sim, l.simulation_Waermebedarf);
                Assert.NotNull(w);

                double[] p = w.DeckungMonatProzent;
                string zeile = w.Deckungszeile();
                Assert.StartsWith("Solare Deckung je Monat: ", zeile);

                string[] teile = zeile.Substring("Solare Deckung je Monat: ".Length).Split(" · ");
                Assert.Equal(12, teile.Length);
                for (int m = 0; m < 12; m++)
                {
                    Assert.InRange(p[m], 0.0, 100.0 + 1e-9);
                    string erwartet = w.BedarfKwh[m] > 0.0
                        ? p[m].ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + " %"
                        : "–";
                    Assert.EndsWith(" " + erwartet, teile[m]);
                }
            }
        }
    }
}
