using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des Anwenderentscheids W6‑E‑7</b> vom 07.09.2026 („Es soll kein
    /// Fallback geben, wenn 0 dann bleibt es so oder es soll in der Einstellung sichtbar
    /// sein") — er revidiert PAKET BHKW-REGULÄR vom 17.08.2026, Punkt 2.
    ///
    /// <para><b>Was hier geprüft wird.</b> Die zwei Ebenen der Grenzleistung in
    /// <c>SimulationBHKW.Moduldaten_Einlesen</c> (Modulwert schlägt Projektwert,
    /// Projektwert 0 bleibt 0 statt still 30 % zu werden), dass ein Lauf mit Grenze 0
    /// durchläuft und keine unendlichen Werte liefert, die Anweisung des
    /// Migrationsschritts 67 (<see cref="BhkwLeistungsgrenzeVorgabe"/>: nur
    /// <c>NULL</c>, nicht die gepflegte 0) und die Vorbelegung des
    /// <see cref="KonfigurationModel"/> für ein NEUES Projekt.</para>
    ///
    /// <para><b>Warum die Fälle die Datenbank brauchen.</b> <c>Moduldaten_Einlesen</c>
    /// liest die Modulkennwerte über <c>BHKWCtrl.ReadSingle</c> aus
    /// <c>Tab_BHKW</c>; ohne eine echte Katalogzeile gäbe es keine Leistung, gegen die
    /// der Faktor überhaupt wirken könnte. Zwei Zeilen der Testdatenbank tragen die
    /// beiden Fälle: <see cref="MODUL_OHNE_EIGENEN_WERT"/> führt keine eigene
    /// Grenzleistung (NULL), <see cref="MODUL_MIT_EIGENEM_WERT"/> führt 15 %.</para>
    ///
    /// <para>Eine Arbeitskopie je Klasse (Regel seit iU9‑W11a); fehlt die Datei,
    /// schweigen die Fälle. <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BhkwLeistungsgrenzeTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public BhkwLeistungsgrenzeTests(TestDatenbank db) { _db = db; }

        /// <summary>
        /// <c>Tab_BHKW</c>-Zeile des Projekts 1017 („BHKW EW K 10 S [K] Heizöl") —
        /// <c>Grenzleistung</c> ist NULL, das Modul greift also auf den Projektwert
        /// durch. Genau dieses Projekt ist der Referenzlauf-Nachweis des Schrittes 67.
        /// </summary>
        private const int MODUL_OHNE_EIGENEN_WERT = 1017080;

        /// <summary>
        /// <c>Tab_BHKW</c>-Zeile des Projekts 1030 („BHKW EW M 50 S [K] Erdgas") —
        /// <c>Grenzleistung</c> = 15 %. Sie überstimmt den Projektwert.
        /// </summary>
        private const int MODUL_MIT_EIGENEM_WERT = 1018148;

        /// <summary>Der Katalogwert der Zeile <see cref="MODUL_MIT_EIGENEM_WERT"/> in %.</summary>
        private const double EIGENER_WERT_PROZENT = 15.0;

        /// <summary>Elektrische Leistung der Zeile <see cref="MODUL_OHNE_EIGENEN_WERT"/> [kW].</summary>
        private const float PEL_MODUL = 10f;

        /// <summary>Thermische Leistung derselben Zeile [kW].</summary>
        private const float PTHERM_MODUL = 19f;

        /// <summary>
        /// Strombedarf der Probestunde [kWh] — <b>kleiner als 30 % von
        /// <see cref="PEL_MODUL"/></b> (2 &lt; 3) und damit genau der Bereich, in dem
        /// sich Grenze 0 und Grenze 30 % unterscheiden.
        /// </summary>
        private const float KLEINER_STROMBEDARF = 2f;

        // =================================================================================
        // 1 — Die zwei Ebenen im Rechenweg (kein stiller Fallback mehr)
        // =================================================================================

        /// <summary>
        /// <b>Der Kern des Entscheids.</b> Projektwert 0 heißt seit W6‑E‑7 „keine
        /// Untergrenze" und wird auch so gerechnet. Vor W6‑E‑7 stand hier 0,3.
        /// </summary>
        [Fact]
        public void Projektwert_0_rechnet_als_0_und_nicht_mehr_still_als_30_Prozent()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0f, Grenzfaktor(MODUL_OHNE_EIGENEN_WERT, projektwertProzent: 0));
        }

        /// <summary>
        /// Der gepflegte Projektwert wird zum Faktor — 30 % → 0,3. Das ist der Weg, den
        /// Migrationsschritt 67 für jeden Bestandssatz ohne gepflegten Wert herstellt.
        /// </summary>
        [Fact]
        public void Projektwert_30_wird_zum_Faktor_0_3()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0.3f, Grenzfaktor(MODUL_OHNE_EIGENEN_WERT, projektwertProzent: 30), 5);
        }

        /// <summary>
        /// Ein anderer gepflegter Projektwert wirkt genauso — die 30 sind eine Vorgabe,
        /// keine zweite fest verdrahtete Zahl. (Projekt 1024 der Testdatenbank führt
        /// tatsächlich 10.)
        /// </summary>
        [Fact]
        public void Ein_beliebiger_Projektwert_wirkt_genauso()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(0.1f, Grenzfaktor(MODUL_OHNE_EIGENEN_WERT, projektwertProzent: 10), 5);
        }

        /// <summary>
        /// <b>Die Rangfolge bleibt:</b> Führt die Katalogzeile eine eigene Grenzleistung,
        /// überstimmt sie den Projektwert — auch dann, wenn der Projektwert größer ist.
        /// </summary>
        [Fact]
        public void Der_Modulwert_ueberstimmt_den_Projektwert()
        {
            if (!_db.Vorhanden) return;

            float erwartet = (float)(EIGENER_WERT_PROZENT / 100.0);

            Assert.Equal(erwartet, Grenzfaktor(MODUL_MIT_EIGENEM_WERT, projektwertProzent: 30), 5);
            Assert.Equal(erwartet, Grenzfaktor(MODUL_MIT_EIGENEM_WERT, projektwertProzent: 0), 5);
        }

        /// <summary>
        /// <b>Der Lauf hält die Grenze 0 aus — und moduliert wirklich bis 0.</b>
        ///
        /// <para>Der Fall ist so gebaut, dass er den Unterschied SIEHT. Das Modul führt
        /// 10 kW elektrisch; der Strombedarf der Stunde ist mit
        /// <see cref="KLEINER_STROMBEDARF"/> = 2 kW bewusst KLEINER als 30 % davon.
        /// Stromgeführt heißt das: Mit der Grenze 30 % darf der Motor gar nicht anlaufen
        /// (<c>Pel · 0,3 = 3 &gt; 2</c>), mit der Grenze 0 moduliert er auf genau die
        /// zwei Kilowatt herunter. Vor W6‑E‑7 waren beide Läufe gleich — der stille
        /// Fallback machte aus der 0 dieselben 30 %.</para>
        /// </summary>
        [Fact]
        public void Mit_Grenze_0_moduliert_das_Modul_unter_30_Prozent_herunter()
        {
            if (!_db.Vorhanden) return;

            SimulationBHKW ohneGrenze = Stromgefuehrt(projektwertProzent: 0);
            SimulationBHKW mitGrenze30 = Stromgefuehrt(projektwertProzent: 30);

            // Mit 30 % bleibt der Motor stehen: Er kann 2 kW nicht liefern, ohne unter
            // seine Untergrenze von 3 kW zu gehen.
            Assert.Equal(0f, mitGrenze30.stromproduktion[0]);

            // Ohne Untergrenze deckt er den Bedarf genau.
            Assert.Equal(KLEINER_STROMBEDARF, ohneGrenze.stromproduktion[0], 3);

            // Und die Koppelwaerme folgt dem Dreisatz ueber den Waermeleistungsanteil.
            Assert.Equal(KLEINER_STROMBEDARF / PEL_MODUL * PTHERM_MODUL,
                         ohneGrenze.waermeproduktion[0], 3);
        }

        /// <summary>
        /// Und er läuft in <b>allen drei Fahrweisen</b> durch: keine Division durch die
        /// Grenze, keine Endlosschleife, keine unendlichen Zahlen. <c>bhkwGrenzL</c> ist
        /// ausschließlich MULTIPLIKATOR der Motorläufe — es wird nirgends durch die
        /// Grenze geteilt und nirgends als Schleifenbedingung gelesen.
        /// </summary>
        [Theory]
        [InlineData(0)]   // waermegefuehrt
        [InlineData(1)]   // stromgefuehrt
        [InlineData(2)]   // ohne Einspeisung
        public void Ein_Lauf_mit_Grenze_0_liefert_in_jeder_Fahrweise_endliche_Werte(int betriebsart)
        {
            if (!_db.Vorhanden) return;

            SimulationBHKW sim = Vorbereitet(MODUL_OHNE_EIGENEN_WERT, projektwertProzent: 0);
            sim.modeBHKW = betriebsart;
            sim.strombedarf[0] = KLEINER_STROMBEDARF;

            Assert.Equal(0f, sim.bhkwGrenzL[0]);

            double[] rest = new double[Kanal.ANZAHL];
            for (int k = 0; k < rest.Length; k++) rest[k] = 50.0;

            sim.Stunde_Start(0, rest);
            sim.Stunde_Bedarf(0, false, rest);
            sim.Stunde_Ende(0, 0.0);

            Assert.True(float.IsFinite(sim.waermeproduktion[0]),
                        "Waermeproduktion ist keine endliche Zahl.");
            Assert.True(float.IsFinite(sim.stromproduktion[0]),
                        "Stromproduktion ist keine endliche Zahl.");
            Assert.True(sim.waermeproduktion[0] >= 0f);
            Assert.True(sim.stromproduktion[0] >= 0f);

            foreach (double wert in rest)
                Assert.True(double.IsFinite(wert), "Ein Restbedarf ist keine endliche Zahl.");
        }

        // =================================================================================
        // 2 — Migrationsschritt 67 (die eine Anweisung aus dem Kern)
        // =================================================================================

        /// <summary>
        /// Der Schritt hebt <b>nur</b> die Sätze OHNE gepflegten Wert: <c>NULL</c> wird
        /// zu 30, eine gepflegte 0 bleibt 0 („wenn 0 dann bleibt es so"), und ein
        /// bereits gepflegter Wert bleibt unangetastet.
        /// </summary>
        [Fact]
        public void Schritt_67_hebt_nur_NULL_und_laesst_die_gepflegte_0_stehen()
        {
            if (!_db.Vorhanden) return;

            // Drei Projekte der Testdatenbank in die drei Ausgangslagen bringen. Sie
            // stehen nach dem Nachziehen der Datei alle auf einem gepflegten Wert; der
            // Fall stellt den Zustand VOR dem Schritt wieder her und laesst ihn erneut
            // laufen - so prueft er die Anweisung, nicht den Dateizustand.
            Setzen(1007, null);
            Setzen(1039, 0);
            Setzen(1024, 10);

            Assert.Equal(1, Zaehlung());

            DataRepository.ExecuteNonQuery(BhkwLeistungsgrenzeVorgabe.Anhebung());

            Assert.Equal(BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT, Lesen(1007));
            Assert.Equal(0, Lesen(1039));
            Assert.Equal(10, Lesen(1024));
        }

        /// <summary>
        /// <b>Idempotent.</b> Das <c>UPDATE</c> trägt sein <c>WHERE … IS NULL</c> selbst;
        /// ein zweiter Lauf findet nichts mehr und ändert nichts.
        /// </summary>
        [Fact]
        public void Schritt_67_ist_idempotent()
        {
            if (!_db.Vorhanden) return;

            Setzen(1008, null);
            DataRepository.ExecuteNonQuery(BhkwLeistungsgrenzeVorgabe.Anhebung());

            int nachErstemLauf = Lesen(1008);
            Assert.Equal(0, Zaehlung());

            DataRepository.ExecuteNonQuery(BhkwLeistungsgrenzeVorgabe.Anhebung());

            Assert.Equal(nachErstemLauf, Lesen(1008));
            Assert.Equal(0, Zaehlung());
        }

        /// <summary>
        /// Die Anweisung fasst <b>nur</b> die eine Spalte an: Sie nennt weder
        /// <c>BHKW_Grenzleistung</c> noch sonst ein Feld, und sie schränkt auf
        /// <c>IS NULL</c> ein — nicht auf <c>= 0</c>, wie es der Access-Teilschritt 13b
        /// des Pakets BHKW-REGULÄR noch tat.
        /// </summary>
        [Fact]
        public void Die_Anweisung_nennt_nur_die_eine_Spalte_und_nur_IS_NULL()
        {
            string sql = BhkwLeistungsgrenzeVorgabe.Anhebung();

            Assert.Contains("Tab_Einstellungen", sql, StringComparison.Ordinal);
            Assert.Contains("Leistungsgrenze IS NULL", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("BHKW_Grenzleistung", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("= 0", sql, StringComparison.Ordinal);
        }

        // =================================================================================
        // 3 — Die sichtbare Vorgabe eines NEUEN Projekts
        // =================================================================================

        /// <summary>
        /// Ein frisches <see cref="KonfigurationModel"/> — und damit jedes neu angelegte
        /// Projekt, denn <c>KonfigurationCtrl.Insert</c> schreibt genau dieses Feld —
        /// startet mit der sichtbaren Vorgabe statt mit 0. Ohne sie liefe ein neues
        /// Projekt seit dem Wegfall des Fallbacks OHNE Untergrenze, ohne dass der
        /// Anwender das je gewählt hätte.
        /// </summary>
        [Fact]
        public void Ein_neues_Projekt_startet_mit_der_sichtbaren_Vorgabe()
        {
            Assert.Equal(30, BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT);
            Assert.Equal(BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT,
                         new KonfigurationModel().Leistungsgrenze);
        }

        // =================================================================================
        // Hilfsmittel
        // =================================================================================

        /// <summary>
        /// Fährt <c>Moduldaten_Einlesen</c> über den einzigen öffentlichen Weg dorthin
        /// (<c>Vorbereiten_Zweikanalig</c>) und gibt den aufgelösten Faktor des ersten
        /// Moduls zurück.
        /// </summary>
        private static float Grenzfaktor(int idModul, int projektwertProzent)
        {
            return Vorbereitet(idModul, projektwertProzent).bhkwGrenzL[0];
        }

        /// <summary>
        /// Ein stromgeführter Lauf über EINE Stunde mit <see cref="KLEINER_STROMBEDARF"/>
        /// als Strombedarf — der Aufbau, an dem sich die zwei Grenzen unterscheiden.
        /// </summary>
        private static SimulationBHKW Stromgefuehrt(int projektwertProzent)
        {
            SimulationBHKW sim = Vorbereitet(MODUL_OHNE_EIGENEN_WERT, projektwertProzent);
            sim.modeBHKW = 1;
            sim.strombedarf[0] = KLEINER_STROMBEDARF;

            double[] rest = new double[Kanal.ANZAHL];
            for (int k = 0; k < rest.Length; k++) rest[k] = 50.0;

            sim.Stunde_Start(0, rest);
            sim.Stunde_Bedarf(0, false, rest);
            sim.Stunde_Ende(0, 0.0);
            return sim;
        }

        private static SimulationBHKW Vorbereitet(int idModul, int projektwertProzent)
        {
            var sim = new SimulationBHKW();
            sim.bhkw_list.Add(idModul);
            sim.bhkw_list_Namen.Add("Probe");
            sim.bhkwGrenzleistungAllgemein = projektwertProzent;

            Assert.True(sim.Vorbereiten_Zweikanalig(0, new List<Senkenzuordnung>()),
                        "Vorbereiten_Zweikanalig ist gescheitert: " + sim.Fehlertext);
            return sim;
        }

        private static void Setzen(int idProjekt, int? wert)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Einstellungen SET Leistungsgrenze = ? WHERE ID_Projekt = ?",
                new DbParam("?", wert.HasValue ? (object)wert.Value : DBNull.Value),
                new DbParam("?", idProjekt));
        }

        private static int Lesen(int idProjekt)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT Leistungsgrenze FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                new DbParam("?", idProjekt));
            Assert.NotNull(o);
            Assert.NotEqual(DBNull.Value, o);
            return Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static int Zaehlung()
        {
            object o = DataRepository.ExecuteScalar(BhkwLeistungsgrenzeVorgabe.Zaehlung());
            return Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }
    }
}
