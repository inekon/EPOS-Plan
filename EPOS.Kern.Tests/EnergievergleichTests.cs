using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Energievergleich der Übersicht (<see cref="Energievergleich"/>, <see cref="EnergievergleichCtrl"/>): die
    /// Energie-Kennzahlen des Katalogs je Version, gruppiert, die Abweichung zum Stamm samt Prozentangabe, nicht
    /// simulierte Versionen mit „—“, keine Kosten und kein CO₂.
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergievergleichTests : System.IDisposable
    {
        /// <summary>Zahlen mit deutschem Dezimalkomma — de-DE gepinnt.</summary>
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        private static VariantenDaten Version(double waerme, double strom, bool stamm = false)
        {
            return new VariantenDaten
            {
                IstStamm = stamm,
                Ergebnis = new ErgebnisModel
                {
                    Energiebedarf = new ErgebnisEnergiebedarfModel
                    {
                        Waermebedarf_Gesamt = waerme,
                        Strombedarf_Gesamt = strom,
                        Stromrestbedarf = strom
                    }
                }
            };
        }

        private static EnergievergleichZeile Zeile(EnergievergleichErgebnis e, string schluessel)
            => e.Zeilen.Single(z => z.Schluessel == schluessel);

        [Fact]
        public void Werte_je_Version_mit_Abweichung_und_Prozent_zum_Stamm()
        {
            var e = Energievergleich.Vergleiche(new[] { Version(100, 50, true), Version(110, 50) }, false, DE);

            EnergievergleichZeile w = Zeile(e, "energie.waermebedarf");
            Assert.Equal(Energievergleich.GRUPPE_BEDARF, w.Gruppe);
            Assert.Equal("Wärmebedarf gesamt", w.Bezeichnung);
            Assert.Equal("MWh/a", w.Einheit);
            Assert.Equal(new[] { "100", "110" }, w.Texte);
            Assert.Equal(new[] { false, true }, w.WeichtAb);
            Assert.Equal(new[] { "", "+10,0 %" }, w.Abweichung);

            EnergievergleichZeile s = Zeile(e, "energie.strombedarf");
            Assert.Equal(new[] { false, false }, s.WeichtAb);
            Assert.Equal(new[] { "", "" }, s.Abweichung);

            Assert.Equal(Energievergleich.GRUPPE_ENDENERGIE, Zeile(e, "energie.netzbezug").Gruppe);
        }

        [Fact]
        public void Zeilen_ohne_Wert_entfallen_und_die_Gruppen_folgen_der_Reihenfolge()
        {
            var e = Energievergleich.Vergleiche(new[] { Version(100, 50, true) }, false, DE);

            // Ohne Wärmepumpe im Ergebnis führt keine Version ihre Kennzahlen — die Zeilen entfallen.
            Assert.DoesNotContain(e.Zeilen, z => z.Schluessel == "energie.wp_waerme");
            int[] rang = e.Zeilen.Select(z => Energievergleich.GRUPPEN.ToList().IndexOf(z.Gruppe)).ToArray();
            Assert.Equal(rang.OrderBy(r => r), rang);
        }

        [Fact]
        public void Nicht_simulierte_Version_zeigt_Strich_und_weicht_nicht_ab()
        {
            var ohne = new VariantenDaten();
            var e = Energievergleich.Vergleiche(new[] { Version(100, 50, true), ohne, null! }, false, DE);

            Assert.Equal(new[] { true, false, false }, e.Simuliert);
            EnergievergleichZeile w = Zeile(e, "energie.waermebedarf");
            Assert.Equal(new[] { "100", "—", "—" }, w.Texte);
            Assert.Equal(new[] { false, false, false }, w.WeichtAb);
            Assert.False(e.KeineSimuliert);
        }

        [Fact]
        public void Ohne_jede_Simulation_keine_Zeilen()
        {
            var e = Energievergleich.Vergleiche(new[] { new VariantenDaten(), new VariantenDaten() }, false, DE);
            Assert.True(e.KeineSimuliert);
            Assert.Empty(e.Zeilen);
        }

        [Fact]
        public void Ohne_simulierten_Stamm_wird_nichts_markiert()
        {
            var e = Energievergleich.Vergleiche(new[] { new VariantenDaten(), Version(110, 50) }, false, DE);
            Assert.Equal(new[] { "—", "110" }, Zeile(e, "energie.waermebedarf").Texte);
            Assert.All(e.Zeilen, z => Assert.False(z.IrgendeineAbweichung));
        }

        [Fact]
        public void Keine_Kosten_aber_CO2_als_letzte_Gruppe()
        {
            List<Kennzahl> k = Energievergleich.Kennzahlen();
            Assert.NotEmpty(k);
            Assert.DoesNotContain(k, x => x.Gruppe == KennzahlenKatalog.GR_KOSTEN || x.Einheit.Contains('€'));
            Assert.DoesNotContain(k, x => x.Schluessel == KennzahlenKatalog.SCHLUESSEL_KAELTE_KOSTEN);
            // Die Energie- und Effizienzkennzahlen sind vollständig dabei, CO₂ steht in der Gruppe Emissionen.
            Assert.Contains(k, x => x.Schluessel == "eff.jaz");
            Assert.Contains(k, x => x.Schluessel == "energie.pv_strom");
            Assert.Equal(Energievergleich.GRUPPE_STROM, Energievergleich.Gruppe(k.Single(x => x.Schluessel == "eff.autarkie")));
            Assert.Equal(Energievergleich.GRUPPE_EFFIZIENZ, Energievergleich.Gruppe(k.Single(x => x.Schluessel == "eff.jaz")));
            Assert.Equal(Energievergleich.GRUPPE_EMISSIONEN, Energievergleich.Gruppe(k.Single(x => x.Schluessel == "em.co2")));
            Assert.Equal(Energievergleich.GRUPPE_EMISSIONEN,
                         Energievergleich.Gruppe(k.Single(x => x.Schluessel == KennzahlenKatalog.SCHLUESSEL_KAELTE_CO2)));
            Assert.Equal(Energievergleich.GRUPPE_EMISSIONEN, Energievergleich.GRUPPEN.Last());

            VariantenDaten stamm = Version(100, 50, true), variante = Version(100, 50);
            stamm.CO2Gesamt = 20; variante.CO2Gesamt = 15;
            var e = Energievergleich.Vergleiche(new[] { stamm, variante }, false, DE);
            EnergievergleichZeile co2 = Zeile(e, "em.co2");
            Assert.Equal("t/a", co2.Einheit);
            Assert.Equal(new[] { "20,0", "15,0" }, co2.Texte);
            Assert.Equal(new[] { "", "−25,0 %" }, co2.Abweichung);
            Assert.Same(co2, e.Zeilen.Last());
        }

        [Fact]
        public void Englische_Beschriftung_aus_dem_Katalog()
        {
            var e = Energievergleich.Vergleiche(new[] { Version(100, 50, true) }, true, CultureInfo.InvariantCulture);
            Assert.Equal("Total heat demand", Zeile(e, "energie.waermebedarf").Bezeichnung);
        }

        /// <summary>
        /// Probe gegen die Testdatenbank: 1024 und 1030 tragen ein gespeichertes Ergebnis, 1007 keins. Der Wärmebedarf der
        /// Übersicht ist der des gespeicherten Laufs — dieselbe Zahl wie in der Kennzahlentafel des Berichts.
        /// </summary>
        [Fact]
        public void Probe_gegen_die_Testdatenbank()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            EnergievergleichErgebnis e = EnergievergleichCtrl.Vergleiche(new[] { 1024, 1030, 1007 }, false, DE);

            Assert.Equal(new[] { true, true, false }, e.Simuliert);
            ErgebnisModel stamm = new ErgebnisCtrl().Load(1024);
            EnergievergleichZeile w = Zeile(e, "energie.waermebedarf");
            Assert.Equal(stamm.Energiebedarf.Waermebedarf_Gesamt.ToString("N0", DE), w.Texte[0]);
            Assert.Equal("—", w.Texte[2]);
            Assert.False(w.WeichtAb[2]);
            Assert.Contains(e.Zeilen, z => z.Gruppe == Energievergleich.GRUPPE_ERZEUGUNG);
            Assert.DoesNotContain(e.Zeilen, z => z.Schluessel.StartsWith("ko."));
        }
    }
}
