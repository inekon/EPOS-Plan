using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Jahresarbeitszahl der Wärmepumpe</b> (Übergabe „Dialoge und Korrekturen“ 05.10.2026,
    /// Anwenderentscheide 06.10.2026): EINE Formel im Kern (<see cref="Jahresarbeitszahl"/>), die
    /// Kennzahl <c>eff.jaz</c> des Berichts, der Block „Strom“ des Wärmepumpen-Reiters und die Spalte
    /// JAZ seiner Modultabelle rufen sie. <c>eff.jaz</c> ist die JAZ des SYSTEMS mit Heizstab
    /// (Bilanzgrenze nach VDI 4650); die JAZ der Wärmepumpe allein ist derselbe Quotient ohne
    /// Heizstab. Ohne Strom gibt es keine Zahl.
    /// </summary>
    public sealed class WaermepumpeJahresarbeitszahlTests
    {
        private static double? Kennzahl(VariantenDaten v)
            => KennzahlenKatalog.Alle().Single(k => k.Schluessel == "eff.jaz").Wert(v);

        private static VariantenDaten Mit(double waerme, double strom, double heizstab)
            => new VariantenDaten
            {
                Ergebnis = new ErgebnisModel
                {
                    Waermepumpe = new ErgebnisWaermepumpeModel
                    {
                        Waermeproduktion_WP = waerme,
                        Stromverbrauch_WP = strom,
                        Stromverbrauch_Heizstab = heizstab,
                    }
                }
            };

        [Fact]
        public void Die_JAZ_der_Waermepumpe_ist_Waerme_durch_Strom()
        {
            Assert.Equal(4.0, Jahresarbeitszahl.Waermepumpe(300.0, 75.0));
            Assert.Equal(25.09 / 5.8, Jahresarbeitszahl.Waermepumpe(25.09, 5.8));
        }

        [Fact]
        public void Die_JAZ_des_Systems_zaehlt_die_Heizstabwaerme_in_den_Zaehler_und_seinen_Strom_in_den_Nenner()
        {
            Assert.Equal((300.0 + 2.5) / (75.0 + 2.5), Jahresarbeitszahl.MitHeizstab(300.0, 75.0, 2.5));
            // Ein reiner Heizstab hat die Leistungszahl 1.
            Assert.Equal(1.0, Jahresarbeitszahl.MitHeizstab(0.0, 0.0, 5.0));
            // Mit Heizstab liegt die System-JAZ zwischen 1 und der JAZ der Wärmepumpe.
            double system = Jahresarbeitszahl.MitHeizstab(102.26, 43.01, 89.39)!.Value;
            Assert.InRange(system, 1.0, Jahresarbeitszahl.Waermepumpe(102.26, 43.01)!.Value);
        }

        [Fact]
        public void Ohne_Heizstab_sind_beide_JAZ_bitgleich()
        {
            foreach ((double q, double p) in new[] { (300.0, 75.0), (78.27, 18.86), (0.01, 0.01), (1.0 / 3.0, 0.1) })
                Assert.Equal(Jahresarbeitszahl.Waermepumpe(q, p), Jahresarbeitszahl.MitHeizstab(q, p, 0.0));
        }

        [Fact]
        public void Ohne_Strom_gibt_es_keine_JAZ()
        {
            Assert.Null(Jahresarbeitszahl.Waermepumpe(10.0, 0.0));
            Assert.Null(Jahresarbeitszahl.Waermepumpe(0.0, 0.0));
            Assert.Null(Jahresarbeitszahl.MitHeizstab(0.0, 0.0, 0.0));
            Assert.Null(Jahresarbeitszahl.MitHeizstab(10.0, double.NaN, 0.0));
            Assert.Null(Jahresarbeitszahl.Waermepumpe(10.0, -1.0));
            // Nur Heizstab: keine JAZ der Wärmepumpe, wohl aber eine des Systems.
            Assert.Null(Jahresarbeitszahl.Waermepumpe(0.0, 0.0));
            Assert.NotNull(Jahresarbeitszahl.MitHeizstab(0.0, 0.0, 2.5));
        }

        /// <summary>
        /// Die Kennzahl <c>eff.jaz</c> IST die System-JAZ — derselbe Aufruf, nicht eine zweite Formel.
        /// Die Zahlen von Projekt 1007 der Basis R38 (Wärme 25,09, Strom 5,8, Heizstab 37,81 MWh/a):
        /// 1,44 statt der früheren Mischgröße 25,09 ÷ (5,8 + 37,81) = 0,58.
        /// </summary>
        [Fact]
        public void Die_Kennzahl_eff_jaz_ist_die_JAZ_des_Systems_mit_Heizstab()
        {
            Assert.Equal(Jahresarbeitszahl.MitHeizstab(25.09, 5.8, 37.81), Kennzahl(Mit(25.09, 5.8, 37.81)));
            Assert.Equal(1.44, System.Math.Round(Kennzahl(Mit(25.09, 5.8, 37.81))!.Value, 2));
            // Ohne Heizstab ändert sich nichts: Wärme ÷ Strom.
            Assert.Equal(78.27 / 18.86, Kennzahl(Mit(78.27, 18.86, 0.0)));
            // Ohne Strom und ohne Wärmepumpe: keine Zahl.
            Assert.Null(Kennzahl(Mit(0.0, 0.0, 0.0)));
            Assert.Null(Kennzahl(new VariantenDaten { Ergebnis = new ErgebnisModel() }));
            Assert.Null(Kennzahl(new VariantenDaten()));
        }

        [Fact]
        public void Die_Beschriftung_der_Kennzahl_nennt_den_Heizstab()
        {
            Kennzahl k = KennzahlenKatalog.Alle().Single(x => x.Schluessel == "eff.jaz");
            Assert.Equal("Jahresarbeitszahl (JAZ) Wärmepumpe mit Heizstab", k.LabelDe);
            Assert.Equal("Heat pump system SPF incl. backup heater", k.LabelEn);
            Assert.Equal("N2", k.Format);
        }

        /// <summary>
        /// Der Reiter liest beide JAZ aus dem Ergebnis des Laufs und je Modul aus der Modulzeile — über
        /// dieselbe Rechnung, aus den Mengen, die er ohnehin zeigt.
        /// </summary>
        [Fact]
        public void Reiter_und_Modulzeile_rufen_dieselbe_Rechnung()
        {
            var e = new SimulationErgebnisCtrl.WaermepumpeErgebnis
            {
                WaermeproduktionMwh = 300.0,
                StromverbrauchMwh = 75.0,
                HeizstabStromverbrauchMwh = 2.5,
            };
            Assert.Equal(Jahresarbeitszahl.Waermepumpe(300.0, 75.0), e.JazWaermepumpe);
            Assert.Equal(Jahresarbeitszahl.MitHeizstab(300.0, 75.0, 2.5), e.JazMitHeizstab);

            var m = new SimulationErgebnisCtrl.WpModulZeile("WP 1", 30.0, 120.0, 40.0, 4.0, 1500.0);
            Assert.Equal(3.0, m.Jaz);
            Assert.Equal(124.0 / 44.0, m.JazMitHeizstab);

            var ohneStrom = new SimulationErgebnisCtrl.WpModulZeile("WP 2", 30.0, 0.0, 0.0, 0.0, 0.0);
            Assert.Null(ohneStrom.Jaz);
            Assert.Null(ohneStrom.JazMitHeizstab);
            Assert.Null(new SimulationErgebnisCtrl.WaermepumpeErgebnis().JazMitHeizstab);
        }
    }
}
