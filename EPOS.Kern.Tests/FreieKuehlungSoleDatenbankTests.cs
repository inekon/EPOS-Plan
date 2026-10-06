using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Freie Kühlung über die Wärmequelle — die Datenbankprobe</b> (KU3-6b, F5) auf einer Arbeitskopie der
    /// Testdatenbank: Die Sole-Wasser-Wärmepumpe des Referenzprojekts 1047 (Kühlbetrieb, Vorlauf 18 °C) bekommt die
    /// Erdreichquelle von Projekt 1029 (Sonde, 90 m) und den Schalter <c>Kuehl_Frei</c>. Der Lauf zählt Stunden und
    /// Kälte der freien Kühlung an Modul und Summe, die Kennzahlen stehen im Bericht, die Heizreihen bleiben
    /// bitgleich zum Lauf ohne Schalter; mit Schalter 0 bleiben die Zähler NULL; ohne tragende Quelle wird der
    /// Schalter benannt abgelehnt.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class FreieKuehlungSoleDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public FreieKuehlungSoleDatenbankTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1047;
        private const int WP_1047 = 14946;
        private const int WP_1029 = 11323;

        /// <summary>Die Erdreichquelle der Anlage 11323 (Projekt 1029, Sonde 90 m) auf die Wärmepumpe von 1047.</summary>
        private static void ErdreichquelleSetzen()
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET " +
                "WQ_Typ = (SELECT q.WQ_Typ FROM Tab_Energieanlagen q WHERE q.ID = ?), " +
                "WQ_Spreizung = (SELECT q.WQ_Spreizung FROM Tab_Energieanlagen q WHERE q.ID = ?), " +
                "WQ_Tiefe = (SELECT q.WQ_Tiefe FROM Tab_Energieanlagen q WHERE q.ID = ?), " +
                "WQ_Flaeche = (SELECT q.WQ_Flaeche FROM Tab_Energieanlagen q WHERE q.ID = ?), " +
                "WQ_Anzahl = (SELECT q.WQ_Anzahl FROM Tab_Energieanlagen q WHERE q.ID = ?), " +
                "WQ_Bodentyp = (SELECT q.WQ_Bodentyp FROM Tab_Energieanlagen q WHERE q.ID = ?), " +
                "WQ_Quellsystem = (SELECT q.WQ_Quellsystem FROM Tab_Energieanlagen q WHERE q.ID = ?) " +
                "WHERE ID = ?",
                new DbParam("@a", WP_1029), new DbParam("@b", WP_1029), new DbParam("@c", WP_1029),
                new DbParam("@d", WP_1029), new DbParam("@e", WP_1029), new DbParam("@f", WP_1029),
                new DbParam("@g", WP_1029), new DbParam("@id", WP_1047)));
            Assert.Equal(DbWerte.WQ_TYP_ERDREICH, WaermequelleClass.WertLesenStill(WP_1047, "WQ_Typ") as string);
            Assert.Equal("Sonde", WaermequelleClass.WertLesenStill(WP_1047, "WQ_Quellsystem") as string);
        }

        private static void SchalterSetzen(int wert)
        {
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Energieanlagen SET Kuehl_Frei = ? WHERE ID = ?",
                new DbParam("@f", wert), new DbParam("@id", WP_1047)));
        }

        private static SimulationRunner Rechnen(bool regeneration = false)
        {
            SimulationProtokoll.NeuStarten();
            var lauf = new SimulationRunner();
            // Die Regeneration der Sonde (Konzept Simulationsablauf 23.5) koppelt die Kälteseite über die
            // Rückspeisung an die Heizseite; die Proben hier halten die Kälteseite allein und rechnen ohne sie.
            lauf.sim.RegenerationRechnen = regeneration;
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            Assert.DoesNotContain(lauf.Protokoll.Fehler, f => f.StartsWith("Deckungsprobe Kälte", StringComparison.Ordinal));
            return lauf;
        }

        private static double? Kennzahl(string schluessel, ErgebnisModel erg)
            => KennzahlenKatalog.Alle().Single(k => k.Schluessel == schluessel).Wert(new VariantenDaten { Ergebnis = erg });

        [Fact]
        public void Erdreichquelle_mit_Schalter_kuehlt_frei_und_die_Heizreihen_bleiben()
        {
            if (!_db.Vorhanden) return;
            ErdreichquelleSetzen();

            // Gegenprobe: Schalter 0 - die Zähler bleiben NULL, die Kennzahlen leer.
            SchalterSetzen(0);
            SimulationRunner ohneLauf = Rechnen();
            Kaelteerzeuger ohneWp = ohneLauf.simulation_Kaeltebedarf.Kaskade.Erzeuger.Single(e => e.Maschine == null);
            Assert.False(ohneWp.FreieKuehlungSole);
            Assert.Equal(0, ohneWp.StundenFreieKuehlung);
            double[] heizOhne = ohneLauf.sim.simulation_wp.WP_Waermeproduktion_stuendlich.ToArray();
            ErgebnisModel ohne = new ErgebnisCtrl().Load(PROJEKT);
            Assert.Null(ohne.Waermepumpe.FreieKuehlung_MWh);
            Assert.Null(ohne.Waermepumpe.FreieKuehlung_Stunden);
            Assert.All(ohne.Waermepumpe.Module, m => Assert.Null(m.FreieKuehlung_MWh));
            Assert.Null(Kennzahl(KennzahlenKatalog.SCHLUESSEL_WP_FREI, ohne));
            Assert.Null(Kennzahl(KennzahlenKatalog.SCHLUESSEL_WP_FREI_STUNDEN, ohne));
            double kaelteOhne = ohneWp.KaelteGesamtKwh;
            double stromOhne = ohneWp.StromGesamtKwh;

            SchalterSetzen(1);
            SimulationRunner lauf = Rechnen();
            Kaelteerzeuger wp = lauf.simulation_Kaeltebedarf.Kaskade.Erzeuger.Single(e => e.Maschine == null);
            Assert.True(wp.FreieKuehlungSole);
            Assert.Equal(KaelteFestwerte.FREIE_KUEHLUNG_SOLE_GRAEDIGKEIT_K, wp.FreieKuehlungGraedigkeitK);
            Assert.True(wp.StundenFreieKuehlung > 0);
            Assert.True(wp.KaelteFreiKwh > 0);
            Assert.DoesNotContain(string.Format(CultureInfo.CurrentCulture, R.SIMENG_KAELTE_WP_FREI_OHNE_QUELLE, wp.Bezeichner),
                                  lauf.Protokoll.Warnungen);

            // Heizreihen unberührt (F3): die Wärmeproduktion der Wärmepumpe bitgleich.
            double[] heizMit = lauf.sim.simulation_wp.WP_Waermeproduktion_stuendlich.ToArray();
            Assert.Equal(heizOhne.Length, heizMit.Length);
            for (int h = 0; h < heizMit.Length; h++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(heizOhne[h]), BitConverter.DoubleToInt64Bits(heizMit[h]));

            // Dieselbe Kälte, weniger Strom: Die freie Kühlung ersetzt Verdichterarbeit.
            Assert.Equal(kaelteOhne, wp.KaelteGesamtKwh, 3);
            Assert.True(wp.StromGesamtKwh < stromOhne);

            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
            Assert.NotNull(erg.Waermepumpe.FreieKuehlung_MWh);
            Assert.True(erg.Waermepumpe.FreieKuehlung_MWh > 0);
            Assert.True(erg.Waermepumpe.FreieKuehlung_Stunden > 0);
            Assert.Equal(wp.StundenFreieKuehlung, erg.Waermepumpe.FreieKuehlung_Stunden);
            ErgebnisWaermepumpeModulModel modul = erg.Waermepumpe.Module.Single(m => m.FreieKuehlung_MWh.HasValue);
            Assert.Equal(wp.StundenFreieKuehlung, modul.FreieKuehlung_Stunden);
            Assert.InRange(modul.FreieKuehlung_MWh.Value, wp.KaelteFreiKwh / 1000.0 - 0.006, wp.KaelteFreiKwh / 1000.0 + 0.006);

            Assert.Equal(erg.Waermepumpe.FreieKuehlung_MWh, Kennzahl(KennzahlenKatalog.SCHLUESSEL_WP_FREI, erg));
            Assert.Equal((double)erg.Waermepumpe.FreieKuehlung_Stunden, Kennzahl(KennzahlenKatalog.SCHLUESSEL_WP_FREI_STUNDEN, erg));

            // Die Skalare der Kennzahlendatei tragen die eigenen Schlüssel der Wärmepumpe.
            IReadOnlyList<KeyValuePair<string, double>> skalare = KaelteErgebnisexport.Skalare(lauf.simulation_Kaeltebedarf);
            Assert.Contains(skalare, s => s.Key == "Kaelte[0].WpFreieKuehlungStunden" && s.Value == wp.StundenFreieKuehlung);
            Assert.DoesNotContain(KaelteErgebnisexport.Skalare(ohneLauf.simulation_Kaeltebedarf), s => s.Key.Contains("WpFreieKuehlung"));

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1047 Sonde: frei {0} h, {1:F3} MWh von {2:F3} MWh Kälte; Kältestrom {3:F3} -> {4:F3} MWh",
                wp.StundenFreieKuehlung, wp.KaelteFreiKwh / 1000.0, wp.KaelteGesamtKwh / 1000.0,
                stromOhne / 1000.0, wp.StromGesamtKwh / 1000.0));
        }

        /// <summary>
        /// F2: Die Wärmepumpe von 1047 ohne gepflegte Quelle (Außenluft-Rückfall) - der Schalter bleibt ohne Wirkung
        /// und wird im Lauf benannt; die Zähler bleiben NULL.
        /// </summary>
        [Fact]
        public void Ohne_tragende_Quelle_bleibt_der_Schalter_wirkungslos_mit_Warnung()
        {
            if (!_db.Vorhanden) return;
            SchalterSetzen(1);

            SimulationRunner lauf = Rechnen();
            Kaelteerzeuger wp = lauf.simulation_Kaeltebedarf.Kaskade.Erzeuger.Single(e => e.Maschine == null);
            Assert.False(wp.FreieKuehlungSole);
            Assert.Equal(0, wp.StundenFreieKuehlung);
            string erwartet = string.Format(CultureInfo.CurrentCulture, R.SIMENG_KAELTE_WP_FREI_OHNE_QUELLE, wp.Bezeichner);
            Assert.Contains(erwartet, lauf.Protokoll.Warnungen);

            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
            Assert.Null(erg.Waermepumpe.FreieKuehlung_MWh);
            Assert.Null(erg.Waermepumpe.FreieKuehlung_Stunden);
        }
    }
}
