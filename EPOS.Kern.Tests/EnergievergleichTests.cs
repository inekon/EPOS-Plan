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

        private static EnergieTraegerNachweis Brennstoff(int id, string name, double mwh)
            => new EnergieTraegerNachweis { CarrierId = id, Traeger = name, MengeMWh = mwh, WaermeMengeMWh = mwh, Einheit = "m³" };

        private static EnergieTraegerNachweis Netzstrom(double mwh)
            => new EnergieTraegerNachweis { CarrierId = 2, Traeger = "Strom", Netzstrom = true, MengeMWh = mwh, Einheit = "kWh" };

        [Fact]
        public void Endenergie_je_Traeger_mit_Menge_Abweichung_und_ohne_zweite_Stromzeile()
        {
            VariantenDaten stamm = Version(100, 50, true), variante = Version(100, 50);
            stamm.EnergiekostenJeTraeger = new List<EnergieTraegerNachweis> { Brennstoff(1, "Erdgas", 30), Netzstrom(50) };
            variante.EnergiekostenJeTraeger = new List<EnergieTraegerNachweis>
            {
                Brennstoff(1, "Erdgas", 33), Brennstoff(3, "Heizöl", 5.4), Netzstrom(50)
            };
            var e = Energievergleich.Vergleiche(new[] { stamm, variante }, false, DE);

            EnergievergleichZeile gas = Zeile(e, Energievergleich.SchluesselTraeger(1, false));
            Assert.Equal("endenergie:1", gas.Schluessel);
            Assert.Equal(Energievergleich.GRUPPE_ENDENERGIE, gas.Gruppe);
            Assert.Equal("Endenergie Erdgas", gas.Bezeichnung);
            Assert.Equal("MWh/a", gas.Einheit);
            Assert.Equal(new[] { "30", "33" }, gas.Texte);
            Assert.Equal(new[] { false, true }, gas.WeichtAb);
            Assert.Equal(new[] { "", "+10,0 %" }, gas.Abweichung);

            // Ein Träger, den nur die Variante führt: beim Stamm „—“, ganzzahlig wie die übrigen MWh-Zeilen.
            EnergievergleichZeile oel = Zeile(e, "endenergie:3");
            Assert.Equal(new[] { "—", "5" }, oel.Texte);

            // Der Netzstrom des Kostennachweises ist der Netzbezug: keine zweite Stromzeile.
            Assert.DoesNotContain(e.Zeilen, z => z.Schluessel == "endenergie:2");
            Assert.Single(e.Zeilen, z => z.Gruppe == Energievergleich.GRUPPE_ENDENERGIE && z.Bezeichnung.Contains("Strom"));

            // Reihenfolge: Netzbezug Strom, darunter die Träger.
            List<string> endenergie = e.Zeilen.Where(z => z.Gruppe == Energievergleich.GRUPPE_ENDENERGIE)
                                              .Select(z => z.Schluessel).ToList();
            Assert.Equal(new[] { "energie.netzbezug", "endenergie:1", "endenergie:3" }, endenergie);
        }

        [Fact]
        public void Kaeltestrom_eines_eigenen_Kuehltraegers_ist_ausdruecklich_benannt()
        {
            VariantenDaten stamm = Version(100, 50, true);
            stamm.EnergiekostenJeTraeger = new List<EnergieTraegerNachweis>
            {
                Netzstrom(45), new EnergieTraegerNachweis { CarrierId = 4, Traeger = "Ökostrom", MengeMWh = 5, Einheit = "kWh" }
            };
            var e = Energievergleich.Vergleiche(new[] { stamm }, false, DE);

            EnergievergleichZeile k = Zeile(e, "endenergie:kaeltestrom:4");
            Assert.Equal("Endenergie Ökostrom (Kältestrom)", k.Bezeichnung);
            Assert.Equal(new[] { "5" }, k.Texte);
            Assert.Equal("Final energy Ökostrom (cooling electricity)",
                         Zeile(Energievergleich.Vergleiche(new[] { stamm }, true, DE), "endenergie:kaeltestrom:4").Bezeichnung);
        }

        [Fact]
        public void Gruppen_als_Daten_offen_und_zugeklappt()
        {
            Assert.Equal(Energievergleich.GRUPPEN, Energievergleich.GRUPPEN_DATEN.Select(g => g.Schluessel));
            string[] offen = Energievergleich.GRUPPEN_DATEN.Where(g => g.VorgabeOffen).Select(g => g.Schluessel).ToArray();
            Assert.Equal(new[]
            {
                Energievergleich.GRUPPE_BEDARF, Energievergleich.GRUPPE_ERZEUGUNG, Energievergleich.GRUPPE_STROM,
                Energievergleich.GRUPPE_ENDENERGIE, Energievergleich.GRUPPE_DECKUNG, Energievergleich.GRUPPE_EFFIZIENZ
            }, offen);
            Assert.Equal(new[]
            {
                Energievergleich.GRUPPE_KAELTE, Energievergleich.GRUPPE_EFFIZIENZ_WEITERE,
                Energievergleich.GRUPPE_GEBAEUDE, Energievergleich.GRUPPE_EMISSIONEN
            }, Energievergleich.GRUPPEN_DATEN.Where(g => !g.VorgabeOffen).Select(g => g.Schluessel));
            Assert.False(Energievergleich.VorgabeOffen(Energievergleich.GRUPPE_EMISSIONEN));

            // Schlanke Effizienz: JAZ (auch Kälte) und Vollbenutzungsstunden; der Rest unter „Weitere Effizienzwerte“.
            Dictionary<string, Kennzahl> k = Energievergleich.Kennzahlen().ToDictionary(x => x.Schluessel);
            foreach (string s in new[] { "eff.jaz", KennzahlenKatalog.SCHLUESSEL_KAELTE_JAZ, "eff.wp_vbh", "eff.bhkw_bh", "eff.bhkw_vbh_el" })
                Assert.Equal(Energievergleich.GRUPPE_EFFIZIENZ, Energievergleich.Gruppe(k[s]));
            foreach (string s in new[] { "eff.t_oben_mittel", "eff.t_oben_min", KennzahlenKatalog.SCHLUESSEL_WP_VORLAUF_DARUNTER })
                Assert.Equal(Energievergleich.GRUPPE_EFFIZIENZ_WEITERE, Energievergleich.Gruppe(k[s]));
            Assert.Equal(Energievergleich.GRUPPE_KAELTE, Energievergleich.Gruppe(k[KennzahlenKatalog.SCHLUESSEL_KAELTE_STROM]));
            Assert.Equal(Energievergleich.GRUPPE_GEBAEUDE, Energievergleich.Gruppe(k[KennzahlenKatalog.SCHLUESSEL_GEB_AUFHEIZZEIT]));
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

            // Die Trägerzeilen sind die Mengen des Kostennachweises; der Netzstrom steht nur als „Netzbezug Strom“.
            VariantenDaten v = EnergievergleichCtrl.Lade(1024, true);
            foreach (EnergieTraegerNachweis t in v.EnergiekostenJeTraeger.Where(t => !t.Netzstrom && t.WaermeMengeMWh > 0))
                Assert.Equal(t.MengeMWh.ToString("N0", DE), Zeile(e, Energievergleich.SchluesselTraeger(t.CarrierId, false)).Texte[0]);
            EnergieTraegerNachweis netz = v.EnergiekostenJeTraeger.FirstOrDefault(t => t.Netzstrom);
            if (netz != null)
            {
                Assert.DoesNotContain(e.Zeilen, z => z.Schluessel == Energievergleich.SchluesselTraeger(netz.CarrierId, false));
                Assert.Equal(netz.MengeMWh.ToString("N0", DE), Zeile(e, "energie.netzbezug").Texte[0]);
            }
        }
    }
}
