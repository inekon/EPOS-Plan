using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Nachrang-Abschaltschwelle bei Solarthermie am Puffer.</b> Lädt eine
    /// Solarthermie einen Puffer vorrangig und ist <c>Schwelle_Aus_Nachrang</c> nicht
    /// gepflegt, gilt für den nachrangigen Erzeuger 30 % statt <c>Schwelle_Aus</c>
    /// (<see cref="Ladeordnung.SCHWELLE_AUS_NACHRANG_SOLAR_DEFAULT"/>). Ein gepflegter
    /// hoher Wert bleibt und wird als <see cref="Warnkriterien.SOLAR_NACHRANG_HOCH"/>
    /// gemeldet; der Puffer ohne Temperaturpaar als
    /// <see cref="Warnkriterien.PUFFER_OHNE_TEMPERATURPAAR"/>.
    ///
    /// <para><b>Aufbau auf der Arbeitskopie</b> (Projekt 1026, kein Referenzprojekt): die
    /// Lage des Anwenders nachgestellt — Kaskade Solarthermie → Heizkessel, beide laden
    /// denselben Puffer (3000 l, ohne Temperaturpaar, Rückfall ΔT 10 K), die Wärmepumpe
    /// ist aus der Kaskade genommen. Jeder Fall legt seine eigene Arbeitskopie an.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SolarNachrangschwelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly Xunit.Abstractions.ITestOutputHelper _ausgabe;

        public SolarNachrangschwelleTests(Xunit.Abstractions.ITestOutputHelper ausgabe)
        {
            _ausgabe = ausgabe;
        }

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1026;
        private const int PUFFER = 1054165;
        private const int SOLAR = 11274;
        private const int KESSEL = 11275;
        private const int WP = 14917;

        // =================================================================
        // Die Regel ohne Datenbank
        // =================================================================

        [Fact]
        public void Ungepflegt_mit_Solar_im_Vorrang_gilt_die_Solar_Vorgabe()
        {
            double w = Ladeordnung.NachrangschwelleWirksam(95, 95, false, true, out bool vorgabe);
            Assert.Equal(Ladeordnung.SCHWELLE_AUS_NACHRANG_SOLAR_DEFAULT, w);
            Assert.Equal(30.0, w);
            Assert.True(vorgabe);
        }

        [Fact]
        public void Ungepflegt_ohne_Solar_gilt_Schwelle_Aus()
        {
            double w = Ladeordnung.NachrangschwelleWirksam(90, 90, false, false, out bool vorgabe);
            Assert.Equal(90.0, w);
            Assert.False(vorgabe);
        }

        [Fact]
        public void Ein_gepflegter_Wert_bleibt_massgeblich()
        {
            double w = Ladeordnung.NachrangschwelleWirksam(95, 95, true, true, out bool vorgabe);
            Assert.Equal(95.0, w);
            Assert.False(vorgabe);

            w = Ladeordnung.NachrangschwelleWirksam(95, 50, true, true, out vorgabe);
            Assert.Equal(50.0, w);
            Assert.False(vorgabe);
        }

        [Fact]
        public void Die_Vorgabe_liegt_nie_ueber_Schwelle_Aus()
        {
            double w = Ladeordnung.NachrangschwelleWirksam(25, 25, false, true, out bool vorgabe);
            Assert.Equal(25.0, w);
            Assert.False(vorgabe);   // keine Reservezone - also auch keine Vorgabe zu melden
        }

        // =================================================================
        // Ladeordnung auf der Arbeitskopie
        // =================================================================

        [Fact]
        public void Ladereihenfolge_gibt_dem_Kessel_30_Prozent_und_der_Solarthermie_Schwelle_Aus()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Aufbauen(nachrang: null, mitSolar: true);

            List<Ladeordnung.LadeEintrag> liste = Ladeordnung.Ladereihenfolge(PROJEKT, PUFFER);

            Ladeordnung.LadeEintrag solar = Assert.Single(liste, e => e.ID_Anlage == SOLAR);
            Ladeordnung.LadeEintrag kessel = Assert.Single(liste, e => e.ID_Anlage == KESSEL);
            Assert.True(solar.Vorrangig);
            Assert.Equal(95.0, solar.Obergrenze);
            Assert.False(solar.ObergrenzeSolarVorgabe);
            Assert.False(kessel.Vorrangig);
            Assert.Equal(30.0, kessel.Obergrenze);
            Assert.True(kessel.ObergrenzeSolarVorgabe);
            Assert.Equal(30.0, Ladeordnung.SolarVorgabe(liste));
        }

        [Fact]
        public void Ladereihenfolge_mit_gepflegten_95_Prozent_bleibt_bei_95()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Aufbauen(nachrang: 95, mitSolar: true);

            List<Ladeordnung.LadeEintrag> liste = Ladeordnung.Ladereihenfolge(PROJEKT, PUFFER);

            Assert.Equal(95.0, Assert.Single(liste, e => e.ID_Anlage == KESSEL).Obergrenze);
            Assert.Null(Ladeordnung.SolarVorgabe(liste));
        }

        [Fact]
        public void Ohne_Solar_am_Puffer_bleibt_der_Rueckfall_Schwelle_Aus()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Aufbauen(nachrang: null, mitSolar: false);

            List<Ladeordnung.LadeEintrag> liste = Ladeordnung.Ladereihenfolge(PROJEKT, PUFFER);

            // Wärmepumpe vorrangig (Ladeprio 20), Kessel nachrangig (40) - ohne Solar
            // bekommt der Kessel die ungepflegte Nachrangschwelle = Schwelle_Aus.
            Ladeordnung.LadeEintrag kessel = Assert.Single(liste, e => e.ID_Anlage == KESSEL);
            Assert.False(kessel.Vorrangig);
            Assert.Equal(95.0, kessel.Obergrenze);
            Assert.False(kessel.ObergrenzeSolarVorgabe);
            Assert.Null(Ladeordnung.SolarVorgabe(liste));
        }

        // =================================================================
        // Warnkriterien
        // =================================================================

        [Fact]
        public void Warnkriterium_meldet_gepflegte_95_Prozent_an_der_Solaranlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Aufbauen(nachrang: 95, mitSolar: true);

            Warnbefund b = Assert.Single(Warnkriterien.PruefeProjekt(PROJEKT),
                                         x => x.Kriterium == Warnkriterien.SOLAR_NACHRANG_HOCH);
            Assert.False(b.Hart);
            Assert.Equal(SOLAR, b.ID_Anlage);
            Assert.Equal(PUFFER, b.ID_Puffer);
            Assert.Contains("Nachrang-Abschaltschwelle 95 %", b.Text);
            Assert.Contains("Feld leeren = Automatik 30 %", b.Text);
        }

        [Fact]
        public void Warnkriterium_schweigt_bei_30_Prozent()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Aufbauen(nachrang: 30, mitSolar: true);
            Assert.DoesNotContain(Warnkriterien.PruefeProjekt(PROJEKT),
                                  x => x.Kriterium == Warnkriterien.SOLAR_NACHRANG_HOCH);

            // Ungepflegt greift die Vorgabe (30 %) - ebenfalls still.
            Aufbauen(nachrang: null, mitSolar: true);
            Assert.DoesNotContain(Warnkriterien.PruefeProjekt(PROJEKT),
                                  x => x.Kriterium == Warnkriterien.SOLAR_NACHRANG_HOCH);
        }

        [Fact]
        public void Warnkriterium_schweigt_ohne_Solar_am_Puffer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Aufbauen(nachrang: 95, mitSolar: false);

            Assert.DoesNotContain(Warnkriterien.PruefeProjekt(PROJEKT),
                                  x => x.Kriterium == Warnkriterien.SOLAR_NACHRANG_HOCH);
        }

        [Fact]
        public void Puffer_ohne_Temperaturpaar_meldet_den_Rueckfall_mit_Kapazitaet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Aufbauen(nachrang: null, mitSolar: true);

            Warnbefund b = Assert.Single(Warnkriterien.PruefeProjekt(PROJEKT),
                                         x => x.Kriterium == Warnkriterien.PUFFER_OHNE_TEMPERATURPAAR);
            Assert.False(b.Hart);
            Assert.Equal(0, b.ID_Anlage);
            Assert.Equal(PUFFER, b.ID_Puffer);
            // 3000 l · 1,16 Wh/(l·K) · 10 K = 34,8 kWh - dieselbe Zahl wie der Lauf.
            Assert.Contains("ΔT = 10 K", b.Text);
            Assert.Contains("34,8 kWh", b.Text);
        }

        [Fact]
        public void Puffer_mit_Temperaturpaar_meldet_keinen_Rueckfall()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Aufbauen(nachrang: null, mitSolar: true, vorlauf: 55, ruecklauf: 30);

            Assert.DoesNotContain(Warnkriterien.PruefeProjekt(PROJEKT),
                                  x => x.Kriterium == Warnkriterien.PUFFER_OHNE_TEMPERATURPAAR);
        }

        // =================================================================
        // Die Speicherkachel der Simulationskonfiguration
        // =================================================================

        /// <summary>
        /// Die Kachel zeigt die WIRKSAME Nachrangschwelle (30 %, als Vorgabe benannt) und
        /// den Temperaturpaar-Rückfall als Warn-Chip — vor dem Lauf, nicht erst im
        /// Protokoll.
        /// </summary>
        [Fact]
        public void Speicherkachel_nennt_Vorgabe_und_Temperaturpaar_Rueckfall()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Aufbauen(nachrang: null, mitSolar: true);

            var konfig = (EPOS.UI.Seiten.Simulation.SimulationKonfigDienste)
                SimulationKonfigHuelle.Erzeugen(PROJEKT).Gaben()["Dienste"];
            EPOS.UI.Bausteine.SpeicherKachelDaten k =
                konfig.Laden(PROJEKT).Speicher.Single(s => s.IdPuffer == PUFFER);

            Assert.Equal(30.0, k.SchwelleAusNachrang);
            Assert.Contains(k.Detailzeilen,
                            z => z == "Nachrang-Abschaltschwelle 30 % (Vorgabe wegen Solarthermie am Puffer)");
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMWARN_KARTE_CHIP, k.Warnchip);
            Assert.Contains("ΔT = 10 K", k.Warnhinweis);

            // Gepflegtes Paar und gepflegte 30 %: kein Chip, keine Vorgabezeile.
            Aufbauen(nachrang: 30, mitSolar: true, vorlauf: 55, ruecklauf: 30);
            k = konfig.Laden(PROJEKT).Speicher.Single(s => s.IdPuffer == PUFFER);
            Assert.Equal("", k.Warnchip);
            Assert.DoesNotContain(k.Detailzeilen, z => z.Contains("Vorgabe wegen Solarthermie"));
        }

        // =================================================================
        // Der Lauf
        // =================================================================

        /// <summary>
        /// DER BEFUND SELBST: Mit der Vorgabe (ungepflegt → 30 %) nutzt der Lauf mehr
        /// Solarwärme als mit einem Nachrang, der den Puffer bis 95 % geladen hält; der
        /// Lauf nennt die Vorgabe als Hinweis.
        /// </summary>
        [Fact]
        public void Mit_der_Vorgabe_steigt_die_genutzte_Solarwaerme()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Aufbauen(nachrang: 95, mitSolar: true);
            SimulationRunner hoch = Rechne();
            double genutztHoch = hoch.sim.simulation_solarthermie.WaermeproduktionGesamtKwh;
            Assert.DoesNotContain(hoch.Protokoll.Hinweise,
                                  t => t.Contains("Vorgabe wegen Solarthermie am Puffer"));

            Aufbauen(nachrang: null, mitSolar: true);
            SimulationRunner vorgabe = Rechne();
            double genutztVorgabe = vorgabe.sim.simulation_solarthermie.WaermeproduktionGesamtKwh;
            Assert.Contains(vorgabe.Protokoll.Hinweise,
                            t => t.Contains("Nachrang-Abschaltschwelle 30 % (Vorgabe wegen Solarthermie am Puffer)"));

            _ausgabe.WriteLine("Solarthermie genutzt: Nachrang 95 % {0:0} kWh, Vorgabe 30 % {1:0} kWh; " +
                               "Überschuss {2:0} bzw. {3:0} kWh",
                               genutztHoch, genutztVorgabe,
                               hoch.sim.simulation_solarthermie.UeberschussSummeKwh,
                               vorgabe.sim.simulation_solarthermie.UeberschussSummeKwh);

            Assert.True(genutztHoch > 0, "Die Solarthermie rechnet nicht mit.");
            Assert.True(genutztVorgabe > genutztHoch * 1.01,
                        $"genutzt mit Vorgabe {genutztVorgabe:0} kWh, mit 95 % {genutztHoch:0} kWh");
        }

        // =================================================================
        // Leer = Automatik: Anlegen, Hülle, Anzeige
        // =================================================================

        /// <summary>
        /// Was ein LEERES Feld bedeutet, rechnet der Kern mit derselben Regel wie der Lauf:
        /// 30 % bei Solarthermie im Vorrang, sonst die Abschaltschwelle im Feld; ein noch
        /// nicht angelegter Puffer (Id 0) hat keine Lader.
        /// </summary>
        [Fact]
        public void Die_Automatik_nennt_30_Prozent_bei_Solar_sonst_die_Abschaltschwelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Aufbauen(nachrang: 95, mitSolar: true);      // gepflegt oder nicht: die Automatik fragt nur die Lader
            Assert.Equal(30.0, PufferSpCtrl.NachrangAutomatik(PROJEKT, PUFFER, 95, out bool solar));
            Assert.True(solar);

            Aufbauen(nachrang: null, mitSolar: false);
            Assert.Equal(90.0, PufferSpCtrl.NachrangAutomatik(PROJEKT, PUFFER, 90, out solar));
            Assert.False(solar);

            Assert.Equal(95.0, PufferSpCtrl.NachrangAutomatik(PROJEKT, 0, 95, out solar));
            Assert.False(solar);
        }

        /// <summary>
        /// Anlegen schreibt KEINE 95 % mehr in <c>Schwelle_Aus_Nachrang</c>: Der Dialogweg
        /// mit leerem Feld und der Katalogweg legen NULL ab, ein eingetragener Wert bleibt.
        /// </summary>
        [Fact]
        public void Anlegen_ohne_Nachrangwert_schreibt_NULL()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int leer = PufferSpCtrl.ProjektPufferAnlegen(PROJEKT, "Automatikpuffer", "", "", 1000, 1.0, 0,
                                                         "", 60, 40, 10, 95, null, 0);
            Assert.True(leer > 0);
            Assert.Equal(DBNull.Value, NachrangSpalte(leer));

            int gepflegt = PufferSpCtrl.ProjektPufferAnlegen(PROJEKT, "Gepflegter Puffer", "", "", 1000, 1.0,
                                                             0, "", 60, 40, 10, 95, 40, 0);
            Assert.Equal(40.0, Convert.ToDouble(NachrangSpalte(gepflegt)));

            // Ändern mit leerem Feld macht aus dem gepflegten Wert wieder die Automatik.
            Assert.True(PufferSpCtrl.ProjektPufferAendern(gepflegt, PROJEKT, "Gepflegter Puffer", "", "",
                                                          1000, 1.0, 0, "", 60, 40, 10, 95, null, 0));
            Assert.Equal(DBNull.Value, NachrangSpalte(gepflegt));

            object stamm = DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Pufferspeicher_STAMM");
            if (stamm == null || stamm == DBNull.Value) return;
            int kopie = new PufferSpCtrl().CopyFromStammNeu(Convert.ToInt32(stamm), PROJEKT, "");
            Assert.True(kopie > 0);
            Assert.Equal(DBNull.Value, NachrangSpalte(kopie));
        }

        /// <summary>
        /// Die Hülle des Pufferdialogs lädt eine ungepflegte Schwelle als LEER (nicht als
        /// Rückfallwert Schwelle_Aus) und nennt neben dem Feld die Automatik mit Grund.
        /// </summary>
        [Fact]
        public void Die_Huelle_laedt_leer_und_nennt_die_Automatik()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Aufbauen(nachrang: null, mitSolar: true);
            EPOS.UI.Dialoge.Simulation.PufferSpProjektDienste d = PufferSpProjektHuelle.Dienste(PROJEKT);
            Assert.Null(d.PufferLesen(PUFFER)!.SchwelleAusNachrang);
            Assert.Equal("→  leer = Automatik: 30 % (Solarthermie am Puffer)",
                         d.NachrangAutomatik!(PUFFER, 95));
            Assert.Equal("→  leer = Automatik: 95 % (= Abschaltschwelle)",
                         d.NachrangAutomatik!(0, 95));

            Aufbauen(nachrang: 95, mitSolar: true);
            Assert.Equal(95.0, d.PufferLesen(PUFFER)!.SchwelleAusNachrang);
        }

        private static object NachrangSpalte(int idPuffer)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Schwelle_Aus_Nachrang FROM Tab_Pufferspeicher WHERE ID = ?",
                new DbParam("?", idPuffer));
            return v ?? DBNull.Value;
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        private static SimulationRunner Rechne()
        {
            var r = new SimulationRunner();
            Assert.True(r.Simuliere(PROJEKT, out string fehler), "Lauf gescheitert: " + fehler);
            return r;
        }

        /// <summary>
        /// Stellt die Lage des Anwenders auf der Arbeitskopie her. <paramref name="mitSolar"/>
        /// false: statt der Solarthermie lädt die Wärmepumpe den Puffer (vorrangig), der
        /// Kessel bleibt nachrangig.
        /// </summary>
        private static void Aufbauen(double? nachrang, bool mitSolar,
                                     int? vorlauf = null, int? ruecklauf = null)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Pufferspeicher SET Gesamtvolumen = ?, Vorlauf = ?, Ruecklauf = ?, " +
                "Schwelle_Aus = ?, Schwelle_Aus_Nachrang = ? WHERE ID = ?",
                new DbParam("?", 3000),
                new DbParam("?", vorlauf.HasValue ? (object)vorlauf.Value : DBNull.Value),
                new DbParam("?", ruecklauf.HasValue ? (object)ruecklauf.Value : DBNull.Value),
                new DbParam("?", 95.0),
                new DbParam("?", nachrang.HasValue ? (object)nachrang.Value : DBNull.Value),
                new DbParam("?", PUFFER));

            DataRepository.ExecuteNonQuery(
                "DELETE FROM Z_AnlageSenke WHERE ID_Anlage IN (?, ?, ?)",
                new DbParam("?", SOLAR), new DbParam("?", KESSEL), new DbParam("?", WP));

            int erster = mitSolar ? SOLAR : WP;
            foreach (int anlage in new[] { erster, KESSEL })
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO Z_AnlageSenke (ID_Anlage, Rang, Ziel, Bedarfsart, ID_Puffer, " +
                    "Ladeprio, Ladeprio_PV, Ladegrenze) VALUES (?, 1, ?, 'Beides', ?, 0, 0, 0)",
                    new DbParam("?", anlage), new DbParam("?", DbWerte.WS_ZIEL_PUFFER_KOMBI),
                    new DbParam("?", PUFFER));

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Einstellungen SET Tool_1 = ?, Tool_2 = ?, Tool_3 = '', Tool_4 = '' " +
                "WHERE ID_Projekt = ?",
                new DbParam("?", mitSolar ? DbWerte.ERZEUGER_SOLARTHERMIE : DbWerte.ERZEUGER_WAERMEPUMPE),
                new DbParam("?", DbWerte.ERZEUGER_HEIZKESSEL),
                new DbParam("?", PROJEKT));
        }
    }
}
