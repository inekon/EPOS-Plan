using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Rechenregeln der Pufferoptionen</b> (Welle M7: PS1 (c), PS1 (a), PS5 (a); Konzept
    /// Simulationsablauf 21) ohne Datenbank — Formeln von <see cref="PufferOptionen"/> und ihre Wirkung im
    /// Speichermodell <see cref="SimulationPufferspeicher"/>.
    /// </summary>
    [Collection("Testdatenbank")]
    public class PufferOptionenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =============================================================================
        //  PS1 (c) — Bereitschaftsverlust temperaturabhängig
        // =============================================================================

        /// <summary>H = Q_B · 1000 / (24 · 45 K): 3,24 kWh/24 h ergeben 3 W/K.</summary>
        [Fact]
        public void Der_Verlustkoeffizient_folgt_aus_dem_Bereitschaftswert()
        {
            Assert.Equal(3.0, PufferOptionen.VerlustkoeffizientWK(3.24), 12);
            Assert.Equal(0.0, PufferOptionen.VerlustkoeffizientWK(0));
            Assert.Equal(0.0, PufferOptionen.VerlustkoeffizientWK(-1));
            // Bei 45 K Übertemperatur ergibt H über 24 h wieder genau den Bereitschaftswert.
            Assert.Equal(3.24, PufferOptionen.VerlustkoeffizientWK(3.24) * 45 * 24 / 1000.0, 12);
        }

        /// <summary>Zonenverlust H · a · (ϑ − ϑ_Raum), nie negativ, höchstens der Zoneninhalt.</summary>
        [Fact]
        public void Der_Zonenverlust_folgt_der_Uebertemperatur_und_bleibt_im_Inhalt()
        {
            Assert.Equal(3.0 * 0.25 * 40 / 1000.0, PufferOptionen.ZonenverlustKwh(3.0, 0.25, 60, 20, 10), 12);
            Assert.Equal(0.0, PufferOptionen.ZonenverlustKwh(3.0, 0.25, 15, 20, 10));
            Assert.Equal(0.01, PufferOptionen.ZonenverlustKwh(3.0, 1.0, 80, 20, 0.01), 12);
            Assert.Equal(0.0, PufferOptionen.ZonenverlustKwh(3.0, 1.0, 80, 20, 0));
            Assert.Equal(20.0, PufferOptionen.Aufstellraum(null));
            Assert.Equal(12.0, PufferOptionen.Aufstellraum(12));
            Assert.Equal(20.0, PufferOptionen.Aufstellraum(40));
            Assert.True(PufferOptionen.IstTemperaturweg("temperatur"));
            Assert.False(PufferOptionen.IstTemperaturweg("tag"));
            Assert.False(PufferOptionen.IstTemperaturweg(null));
        }

        /// <summary>Ein Ein-Zonen-Speicher 3 000 l, 75/35 °C, Q_B = 3,24 kWh/24 h.</summary>
        private static SimulationPufferspeicher Speicher(bool temperatur, int schichten = 1, double[] anteile = null)
        {
            var sp = new SimulationPufferspeicher();
            sp.Init(3000, 75, 35, 3.24);
            sp.SchichtenAnzahl = schichten;
            sp.SchichtAnteile = anteile;
            sp.BereitschaftTemperatur = temperatur;
            sp.SchichtenAufbauen();
            return sp;
        }

        /// <summary>
        /// Weg „temperatur": Ein leerer Speicher (Rücklauftemperatur) verliert nichts; ein voller bei 75 °C
        /// verliert H · 55 K und damit mehr als der Tageswert; ein halbvoller (55 °C) H · 35 K.
        /// </summary>
        [Fact]
        public void Der_Temperaturweg_verliert_leer_nichts_und_voll_mehr_als_der_Tageswert()
        {
            SimulationPufferspeicher leer = Speicher(true);
            leer.StundeAbschliessen(0);
            Assert.Equal(0.0, leer.Verluste_gesamt);

            SimulationPufferspeicher voll = Speicher(true);
            voll.Laden(voll.Q_max, 0);
            voll.StundeAbschliessen(0);
            Assert.Equal(3.0 * 55 / 1000.0, voll.Verluste_gesamt, 12);
            Assert.Equal(voll.Verluste_gesamt, voll.BereitschaftTemperaturKwh, 15);

            SimulationPufferspeicher tag = Speicher(false);
            tag.Laden(tag.Q_max, 0);
            tag.StundeAbschliessen(0);
            Assert.Equal(3.24 / 24.0, tag.Verluste_gesamt, 12);
            Assert.True(voll.Verluste_gesamt > tag.Verluste_gesamt);

            SimulationPufferspeicher halb = Speicher(true);
            halb.Laden(halb.Q_max / 2, 0);
            halb.StundeAbschliessen(0);
            Assert.Equal(3.0 * 35 / 1000.0, halb.Verluste_gesamt, 12);

            // Wärmerer Aufstellraum, weniger Verlust.
            SimulationPufferspeicher warm = Speicher(true);
            warm.AufstellraumC = 30;
            warm.Laden(warm.Q_max, 0);
            warm.StundeAbschliessen(0);
            Assert.Equal(3.0 * 45 / 1000.0, warm.Verluste_gesamt, 12);
        }

        /// <summary>Geschichtet trägt jede Zone ihren Anteil; eine Zone auf Rücklauf verliert nichts.</summary>
        [Fact]
        public void Der_Temperaturweg_rechnet_je_Zone()
        {
            SimulationPufferspeicher sp = Speicher(true, 4);
            sp.Laden(sp.Q_max / 4, 0);           // oberste Zone voll (75 °C), übrige auf 35 °C
            sp.StundeAbschliessen(0);
            // Die Ausgleichsleitung vor den Verlusten verschiebt etwas Wärme; die Größenordnung bleibt
            // H/4 · 55 K bis H/4 · Σ(ϑ_i − 20 K) (die Nachbarzonen nehmen etwas auf und verlieren es mit).
            Assert.InRange(sp.Verluste_gesamt, 0.5 * 3.0 * 0.25 * 55 / 1000.0, 3.0 * 0.25 * 100 / 1000.0 + 1e-12);
            double summe = Enumerable.Range(0, 4).Sum(i => sp.SchichtEnergie(i));
            Assert.Equal(Math.Min(sp.SOC, sp.Q_max), summe, 9);
        }

        // =============================================================================
        //  PS1 (a) — Zonenanteile
        // =============================================================================

        /// <summary>Die Prüfung nimmt Komma und Punkt, normiert und lehnt Zahl, Bereich, Anzahl und Summe benannt ab.</summary>
        [Fact]
        public void Die_Anteile_werden_geprueft_und_benannt_abgelehnt()
        {
            Assert.Null(PufferOptionen.AnteilePruefen("0,10;0,16;0,37;0,37", 4, out double[] a));
            Assert.Equal(new[] { 0.10, 0.16, 0.37, 0.37 }, a.Select(x => Math.Round(x, 12)).ToArray());
            Assert.Null(PufferOptionen.AnteilePruefen("0.5; 0.5", 2, out a));
            Assert.Null(PufferOptionen.AnteilePruefen("", 4, out a));
            Assert.Null(a);
            Assert.Null(PufferOptionen.AnteilePruefen("0,3334;0,3333;0,3338", 3, out a));
            Assert.Equal(1.0, a.Sum(), 12);

            Assert.NotNull(PufferOptionen.AnteilePruefen("0,1;x;0,5;0,3", 4, out _));
            Assert.NotNull(PufferOptionen.AnteilePruefen("0,1;0;0,5;0,4", 4, out _));
            Assert.NotNull(PufferOptionen.AnteilePruefen("0,5;0,5", 4, out _));
            Assert.NotNull(PufferOptionen.AnteilePruefen("0,2;0,2;0,2;0,2", 4, out _));
            Assert.Contains("0,8", PufferOptionen.AnteilePruefen("0,2;0,2;0,2;0,2", 4, out _));

            Assert.Equal("0,10;0,16;0,37;0,37", PufferOptionen.VorschlagKombispeicherText());
            Assert.Null(PufferOptionen.AnteilePruefen(PufferOptionen.VorschlagKombispeicherText(), 4, out _));
        }

        /// <summary>
        /// Zonengröße Q_max · a_i: Eine Ladung von 10 % füllt die oberste Zone des Vorschlags gerade voll;
        /// die Temperaturen bleiben monoton, die Summe gleich dem Inhalt.
        /// </summary>
        [Fact]
        public void Die_Zonengroesse_folgt_dem_Anteil()
        {
            double[] anteile = { 0.10, 0.16, 0.37, 0.37 };
            SimulationPufferspeicher sp = Speicher(false, 4, anteile);
            sp.Laden(sp.Q_max * 0.10, 0);
            Assert.Equal(sp.Q_max * 0.10, sp.SchichtEnergie(0), 9);
            Assert.Equal(75.0, sp.SchichtTemperatur(0), 9);
            Assert.Equal(35.0, sp.SchichtTemperatur(1), 9);

            var zufall = new Random(7);
            for (int h = 0; h < 500; h++)
            {
                if (zufall.NextDouble() < 0.5) sp.Laden(zufall.NextDouble() * sp.Q_max * 0.3, h);
                else sp.Entladen(zufall.NextDouble() * sp.Q_max * 0.3, h);
                sp.StundeAbschliessen(h);

                double summe = Enumerable.Range(0, 4).Sum(i => sp.SchichtEnergie(i));
                Assert.Equal(Math.Min(sp.SOC, sp.Q_max), summe, 6);
                for (int i = 0; i < 3; i++)
                    Assert.True(sp.SchichtTemperatur(i) >= sp.SchichtTemperatur(i + 1) - 1e-9, "Schichtung kippt in Stunde " + h);
                for (int i = 0; i < 4; i++)
                    Assert.InRange(sp.SchichtEnergie(i), 0, sp.Q_max * anteile[i] + 1e-9);
            }
            Assert.Equal(0, sp.SchichtInvarianteVerletzungen);
        }

        /// <summary>Anteile falscher Länge wirken nicht: Der Speicher rechnet gleich große Zonen.</summary>
        [Fact]
        public void Anteile_falscher_Laenge_rechnen_gleich_grosse_Zonen()
        {
            SimulationPufferspeicher sp = Speicher(false, 4, new[] { 0.5, 0.5 });
            sp.Laden(sp.Q_max * 0.25, 0);
            Assert.Equal(sp.Q_max * 0.25, sp.SchichtEnergie(0), 9);
        }

        // =============================================================================
        //  PS5 (a) — Frischwassermodul
        // =============================================================================

        /// <summary>ϑ_oben ≥ ϑ_Zapf + ΔT_FWM, mit Zahlenrand; Grädigkeit leer = 5 K.</summary>
        [Fact]
        public void Das_Frischwassermodul_braucht_Zapftemperatur_plus_Graedigkeit()
        {
            Assert.True(PufferOptionen.FwmFreigabe(65, 60, 5));
            Assert.True(PufferOptionen.FwmFreigabe(65 - 1e-12, 60, 5));
            Assert.False(PufferOptionen.FwmFreigabe(64.9, 60, 5));
            Assert.Equal(5.0, PufferOptionen.FwmGraedigkeit(null));
            Assert.Equal(8.0, PufferOptionen.FwmGraedigkeit(8));
            Assert.Equal(5.0, PufferOptionen.FwmGraedigkeit(25));
            Assert.Equal(65.0, PufferOptionen.FwmMindesttemperatur(60, 5));
        }

        /// <summary>
        /// Im Speicher: Ein Ein-Zonen-Speicher 75/35 °C mit Mindesttemperatur 55 °C gibt voll die Hälfte
        /// ab (den Inhalt über 55 °C), leer nichts; ohne Modul ist die Fähigkeit unbegrenzt.
        /// </summary>
        [Fact]
        public void Das_Frischwassermodul_begrenzt_die_Entnahme_auf_den_Inhalt_ueber_der_Mindesttemperatur()
        {
            SimulationPufferspeicher sp = Speicher(false);
            Assert.Equal(double.MaxValue, sp.FrischwasserEntnahmefaehigkeit());

            sp.Frischwassermodul = true;
            sp.FwmMindestC = 55;
            Assert.Equal(0.0, sp.FrischwasserEntnahmefaehigkeit());
            sp.Laden(sp.Q_max, 0);
            Assert.Equal(sp.Q_max / 2, sp.FrischwasserEntnahmefaehigkeit(), 9);

            sp.FwmMindestC = 80;               // über dem Vorlauf: nie erreicht
            Assert.Equal(0.0, sp.FrischwasserEntnahmefaehigkeit());

            SimulationPufferspeicher g = Speicher(false, 4);
            g.Frischwassermodul = true;
            g.FwmMindestC = 70;
            g.Laden(g.Q_max / 4, 0);           // oberste Zone 75 °C
            Assert.Equal(g.Q_max / 4, g.FrischwasserEntnahmefaehigkeit(), 9);
        }
    }
}
