using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Befund B-1 — der Brennstoffverbrauch je Kessel steht in der Modulzeile</b>
    /// (Anwenderentscheid vom 18.09.2026: „Verbrauch aus dem Lauf nachziehen").
    ///
    /// <para><b>Die Lage davor.</b> Der Rechenkern ermittelt den Brennstoffeinsatz jedes
    /// Kessels stuendlich und bucht ihn je Brennstoffart auf die ANLAGENzeile
    /// (<c>Tab_ErgebnisHeizkessel.Gasverbrauch</c> und Geschwister). Die MODULzeile
    /// (<c>Tab_ErgebnisHeizkesselModul.Verbrauch</c>) blieb leer — und genau sie liest die
    /// Kostenkette. Der Kesselbrennstoff fehlte damit in Energiekosten, CO2-Bilanz und
    /// BEHG-Abgabe, ohne dass eine Zahl es angezeigt haette.</para>
    ///
    /// <para><b>Was hier gepinnt wird — vier Dinge.</b>
    /// <list type="number">
    ///   <item><description>DIE SPALTE TRAEGT DEN LAUF: Modulverbrauch und
    ///     Anlagensumme sind dieselbe Groesse, nur anders geschnitten. Ein Kessel, ein
    ///     Traeger — die beiden Zahlen muessen gleich sein.</description></item>
    ///   <item><description>DER ELEKTROKESSEL ist die Ausnahme: Er bucht auf den
    ///     Stromzaehler und steht ueber den Reststrombedarf im Netzbezug. Seine
    ///     Modulzeile bleibt bei 0, sonst stuende derselbe Strom zweimal in den
    ///     Kosten.</description></item>
    ///   <item><description>DIE WARNUNG <c>WIRT_KESSELBRENNSTOFF_FEHLT</c> bleibt als
    ///     Waechter stehen und feuert weiterhin, wenn eine Zeile Waerme ohne Verbrauch
    ///     fuehrt — der Fall gespeicherter Laeufe von vor B-1.</description></item>
    ///   <item><description>DIESELBE WARNUNG SCHWEIGT beim Elektrokessel: Dort ist die
    ///     0 die richtige Zahl und kein Loch.</description></item>
    /// </list></para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> (<see cref="TestDatenbank"/>): Die
    /// Simulationsfaelle SCHREIBEN (Kaskadenplatz, Ergebniszeilen).
    /// <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselBrennstoffModulTests
    {
        /// <summary>Fuehrt einen Gaskessel in der Kaskade — der Regelfall.</summary>
        private const int PROJEKT_GASKESSEL = 1030;

        /// <summary>Fuehrt einen ELEKTROKESSEL („eloBLOCK VE 10", Brennstoff 13).</summary>
        private const int PROJEKT_ELEKTROKESSEL = 1024;

        // =================================================================
        // 1 — Die Spalte traegt den Brennstoffeinsatz des Laufs
        // =================================================================

        /// <summary>
        /// DER BEFUND SELBST. Nach dem Lauf traegt die Modulzeile den Brennstoffeinsatz
        /// dieses Kessels, seine Nutzwaerme und sein Brennstoffwort — und der
        /// Modulverbrauch ist DIESELBE Zahl wie die Anlagensumme seines Traegers. Ein
        /// Unterschied waere eine zweite Wahrheit ueber denselben Brennstoff.
        /// </summary>
        [Fact]
        public void Die_Modulzeile_traegt_den_Brennstoffeinsatz_des_Laufs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ErgebnisHeizkesselModel h = Kesselergebnis(PROJEKT_GASKESSEL);
            Assert.NotNull(h);
            Assert.Single(h.Module);

            ErgebnisHeizkesselModulModel mo = h.Module[0];
            Assert.True(mo.Verbrauch > 0, "Der Modulverbrauch ist leer geblieben.");
            Assert.Equal(h.Gasverbrauch, mo.Verbrauch, 6);
            Assert.Equal(mo.Waerme_Gas + mo.Waerme_Oel, mo.Waermeproduktion, 6);
            Assert.Equal("Gas", mo.Brennstoff);
        }

        /// <summary>
        /// Die Umkehrprobe des Rechenwegs: Der Verbrauch ist die Nutzwaerme ueber den
        /// Jahresnutzungsgrad. Beide Groessen stammen aus demselben Lauf, also muss der
        /// Zusammenhang gelten, den <c>HilfsstromRechner.KesselBrennstoffMWh</c> als
        /// Rueckfall benutzt — sonst rechnete die Steuerseite mit einer anderen Menge als
        /// die Kostenseite.
        /// </summary>
        [Fact]
        public void Verbrauch_und_Jahresnutzungsgrad_passen_zueinander()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ErgebnisHeizkesselModulModel mo = Kesselergebnis(PROJEKT_GASKESSEL).Module[0];
            Assert.True(mo.Jahresnutzungsgrad > 0);

            double erwartet = (mo.Waerme_Gas + mo.Waerme_Oel) / (mo.Jahresnutzungsgrad / 100.0);
            Assert.Equal(erwartet, mo.Verbrauch, 2);
            Assert.Equal(mo.Verbrauch, HilfsstromRechner.KesselBrennstoffMWh(mo), 6);
        }

        // =================================================================
        // 2 — Der Elektrokessel
        // =================================================================

        /// <summary>
        /// DER SONDERFALL. Ein Elektrokessel bucht seinen Einsatz auf den Stromzaehler
        /// der Anlagenzeile und steht ueber den Reststrombedarf im Netzbezug. Seine
        /// Modulzeile fuehrt deshalb KEINEN Brennstoffverbrauch — sonst stuende derselbe
        /// Strom zweimal in Energiekosten und CO2-Bilanz: einmal als Netzbezug und einmal
        /// als „Brennstoff" seines Traegers. Waerme, Nutzungsgrad und Brennstoffwort
        /// stehen trotzdem.
        /// </summary>
        [Fact]
        public void Der_Elektrokessel_fuehrt_keinen_Brennstoffverbrauch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ErgebnisHeizkesselModel h = Kesselergebnis(PROJEKT_ELEKTROKESSEL);
            Assert.NotNull(h);
            ErgebnisHeizkesselModulModel mo = h.Module[0];

            Assert.Equal(0.0, mo.Verbrauch);
            Assert.Equal("Strom", mo.Brennstoff);
            Assert.True(mo.Waermeproduktion > 0, "Die Nutzwaerme fehlt.");

            // Die Anlagenzeile fuehrt die Energie auf dem STROMzaehler, nicht auf einem
            // Brennstoffzaehler — das ist der Grund fuer die 0 in der Modulzeile.
            Assert.True(h.Stromverbrauch > 0);
            Assert.Equal(0.0, h.Gasverbrauch);
            Assert.Equal(0.0, h.Oelverbrauch);
        }

        // =================================================================
        // 3 — Die Warnung WIRT_KESSELBRENNSTOFF_FEHLT
        // =================================================================

        /// <summary>
        /// DER WAECHTER FEUERT. Eine Modulzeile mit erzeugter Waerme und ohne Verbrauch
        /// ist genau die Lage, in der der Kesselbrennstoff still aus Energiekosten,
        /// CO2-Bilanz und BEHG-Abgabe faellt. Sie entsteht im frischen Lauf nicht mehr,
        /// wohl aber beim Lesen eines Laufs von vor B-1 — und dann muss die Meldung
        /// stehen, samt Namen der Anlage.
        ///
        /// <para>Bis zu diesem Auftrag war die Fahne durch KEINEN Test gedeckt.</para>
        /// </summary>
        [Fact]
        public void Die_Warnung_feuert_bei_Waerme_ohne_Verbrauch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten v = Rechne(PROJEKT_GASKESSEL, Kesselzeile("Altkessel", waerme: 120, verbrauch: 0));

            Assert.True(v.KesselVerbrauchFehlt);
            Assert.Equal(new List<string> { "Altkessel" }, v.KesselOhneVerbrauch);
        }

        /// <summary>
        /// DERSELBE WAECHTER SCHWEIGT, sobald die Spalte gefuellt ist — die Lage nach
        /// B-1. Ohne diesen Fall waere nicht belegt, dass die Reparatur die Meldung
        /// tatsaechlich zum Verstummen bringt.
        /// </summary>
        [Fact]
        public void Die_Warnung_schweigt_mit_gefuelltem_Verbrauch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten v = Rechne(PROJEKT_GASKESSEL, Kesselzeile("Altkessel", waerme: 120, verbrauch: 130));

            Assert.False(v.KesselVerbrauchFehlt);
            Assert.Empty(v.KesselOhneVerbrauch);
        }

        /// <summary>
        /// DIE AUSNAHME. Beim Elektrokessel IST die 0 die richtige Zahl; eine Meldung
        /// ueber einen fehlenden Brennstoff stuende dort dauerhaft und waere falsch.
        /// Erkannt wird er an <c>Tab_Heizkessel.Brennstoff</c> = 13 — der Angabe, die
        /// auch fuer eine gespeicherte Zeile ohne zugeordneten Energietraeger gilt.
        /// </summary>
        [Fact]
        public void Die_Warnung_schweigt_beim_Elektrokessel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten v = Rechne(PROJEKT_ELEKTROKESSEL,
                                      Kesselzeile("eloBLOCK VE 10", waerme: 47.44, verbrauch: 0));

            Assert.False(v.KesselVerbrauchFehlt);
            Assert.Empty(v.KesselOhneVerbrauch);
        }

        /// <summary>
        /// Die Gegenprobe zur Ausnahme: Ein Kessel MIT Brennstoff im selben Projekt wird
        /// weiterhin gemeldet. Die Ausnahme gilt der einen Anlage, nicht dem Projekt.
        /// </summary>
        [Fact]
        public void Die_Ausnahme_gilt_der_Anlage_und_nicht_dem_Projekt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten v = Rechne(PROJEKT_ELEKTROKESSEL,
                                      Kesselzeile("eloBLOCK VE 10", waerme: 47.44, verbrauch: 0),
                                      Kesselzeile("Gaskessel", waerme: 10, verbrauch: 0));

            Assert.True(v.KesselVerbrauchFehlt);
            Assert.Equal(new List<string> { "Gaskessel" }, v.KesselOhneVerbrauch);
        }

        // =================================================================
        // Helfer
        // =================================================================

        /// <summary>Rechnet das Projekt und gibt seine Kessel-Ergebniszeile zurueck.</summary>
        private static ErgebnisHeizkesselModel Kesselergebnis(int idProjekt)
        {
            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(idProjekt, out fehler), "Lauf gescheitert: " + fehler);

            ErgebnisModel m = SimulationRunner.BaueErgebnis(
                idProjekt, laeufer.simulation_Waermebedarf, laeufer.simulation_Strombedarf, laeufer.sim);
            Assert.NotNull(m);
            return m.Heizkessel;
        }

        /// <summary>Eine Kessel-Modulzeile von Hand — Waerme auf dem Gaskanal.</summary>
        private static ErgebnisHeizkesselModulModel Kesselzeile(string name, double waerme, double verbrauch)
        {
            return new ErgebnisHeizkesselModulModel
            {
                Modul = name,
                Waerme_Gas = waerme,
                Waermeproduktion = waerme,
                Verbrauch = verbrauch,
                Jahresnutzungsgrad = 95
            };
        }

        /// <summary>
        /// Laesst die Kostenrechnung ueber ein Ergebnis mit genau diesen Modulzeilen
        /// laufen. Der Bau von Hand ist Absicht: Die Lage „Waerme ohne Verbrauch"
        /// entsteht im frischen Lauf nicht mehr, sie kommt aus gespeicherten Zeilen.
        /// </summary>
        private static VariantenDaten Rechne(int idProjekt, params ErgebnisHeizkesselModulModel[] module)
        {
            var h = new ErgebnisHeizkesselModel();
            foreach (ErgebnisHeizkesselModulModel mo in module)
            {
                h.Module.Add(mo);
                h.Waermeproduktion += mo.Waermeproduktion;
            }

            var erg = new ErgebnisModel { ID_Projekt = idProjekt, Heizkessel = h };
            var v = new VariantenDaten { IdProjekt = idProjekt, Ergebnis = erg };
            KostenEmissionRechner.Berechne(v);
            return v;
        }
    }
}
