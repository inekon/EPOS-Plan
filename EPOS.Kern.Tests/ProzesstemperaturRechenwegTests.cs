using System;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Temperaturniveau des Prozesskanals im Rechenweg</b> (Entscheidungsvorlage
    /// Modellgrenzen, PW1 Stufe 1; Regeln bei <see cref="Prozesstemperatur"/>).
    ///
    /// <para><b>Geprüft wird:</b> das Niveau je Stunde (höchster Vorlauf, mengengewichteter
    /// Rücklauf), die Prüfung des Paars, die Kennlinienwahl der Wärmepumpe für den Prozessanteil,
    /// die Entnahmegrenze des Puffers, und im Lauf auf Kopien der Referenzprojekte 1041
    /// (Wärmepumpe mit Direktsenke Prozesswärme) und 1050 (Brennwertkessel mit Kennlinie): (a)
    /// anderer COP bzw. (b) keine Prozessdeckung, (c) Prozessrücklauf im Brennwertkessel — und
    /// dass ohne Temperaturpaar alles wie zuvor rechnet.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProzesstemperaturRechenwegTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =============================================================================
        //  Teil 1 - Regeln ohne Datenbank
        // =============================================================================

        /// <summary>Je Stunde der höchste Vorlauf und der mit der Menge gewichtete Rücklauf; ohne Bedarf NaN.</summary>
        [Fact]
        public void Das_Niveau_ist_der_hoechste_Vorlauf_mit_gewichtetem_Ruecklauf()
        {
            var a = new double[8760];
            var b = new double[8760];
            a[0] = 30; b[0] = 10;          // beide aktiv
            a[1] = 20;                      // nur a
            b[2] = 5;                       // nur b

            var n = new Prozesstemperatur();
            n.Aufnehmen(60, 40, a);
            n.Aufnehmen(90, 70, b);
            n.Abschliessen();

            Assert.Equal(2, n.Profile);
            Assert.Equal(90, n.VorlaufMax);
            Assert.Equal(3, n.Stunden);
            Assert.Equal(90, n.Vorlauf(0));
            Assert.Equal((30 * 40 + 10 * 70) / 40.0, n.Ruecklauf(0), 12);
            Assert.Equal(60, n.Vorlauf(1));
            Assert.Equal(40, n.Ruecklauf(1));
            Assert.Equal(90, n.Vorlauf(2));
            Assert.True(double.IsNaN(n.Vorlauf(3)));
            Assert.True(double.IsNaN(n.Ruecklauf(3)));
            Assert.True(double.IsNaN(n.Vorlauf(-1)));
            Assert.True(double.IsNaN(n.Vorlauf(8760)));
            Assert.Throws<InvalidOperationException>(() => n.Aufnehmen(50, 30, a));
        }

        /// <summary>Nur ein gepflegter Erzeugervorlauf schließt aus; der Mischrücklauf gewichtet und klemmt.</summary>
        [Fact]
        public void Erreicht_und_Mischruecklauf()
        {
            Assert.True(Prozesstemperatur.Erreicht(0, 90));          // nicht gepflegt
            Assert.True(Prozesstemperatur.Erreicht(70, double.NaN)); // keine Forderung
            Assert.True(Prozesstemperatur.Erreicht(90, 90));
            Assert.False(Prozesstemperatur.Erreicht(70, 90));

            Assert.Equal(50, Prozesstemperatur.MischRuecklauf(0, 30, 50));
            Assert.Equal(40, Prozesstemperatur.MischRuecklauf(0.5, 30, 50), 12);
            Assert.Equal(30, Prozesstemperatur.MischRuecklauf(1.7, 30, 50), 12);
            Assert.Equal(50, Prozesstemperatur.MischRuecklauf(0.5, double.NaN, 50));
        }

        /// <summary>Die Paarprüfung hält dieselben Grenzen wie die Prüfklauseln des Schemas.</summary>
        [Fact]
        public void Die_Paarpruefung()
        {
            Assert.Null(Prozesstemperatur.Paarpruefung(null, null));
            Assert.Null(Prozesstemperatur.Paarpruefung(90, 60));
            Assert.Null(Prozesstemperatur.Paarpruefung(60, 60));
            Assert.Null(Prozesstemperatur.Paarpruefung(250, 0));
            Assert.Equal(Resource().PW_MSG_TEMPERATUR_PAAR, Prozesstemperatur.Paarpruefung(90, null));
            Assert.Equal(Resource().PW_MSG_TEMPERATUR_PAAR, Prozesstemperatur.Paarpruefung(null, 60));
            Assert.Equal(Resource().PW_MSG_TEMPERATUR_BEREICH, Prozesstemperatur.Paarpruefung(251, 60));
            Assert.Equal(Resource().PW_MSG_TEMPERATUR_BEREICH, Prozesstemperatur.Paarpruefung(60, -1));
            Assert.Equal(Resource().PW_MSG_TEMPERATUR_REIHENFOLGE, Prozesstemperatur.Paarpruefung(50, 60));
            Assert.Equal("90 / 60 °C", Prozesstemperatur.Anzeige(90, 60));
            Assert.Equal("", Prozesstemperatur.Anzeige(90, null));
        }

        /// <summary>Spaltentolerant: ohne Spalten oder mit halbem Paar kein Paar.</summary>
        [Fact]
        public void Das_Paar_aus_einer_Kopfzeile()
        {
            var ohne = new DataTable();
            ohne.Columns.Add("Bezeichner", typeof(string));
            Assert.False(Prozesstemperatur.PaarAusZeile(ohne.Rows.Add("x"), out _, out _));

            var mit = new DataTable();
            mit.Columns.Add("Vorlauf", typeof(double));
            mit.Columns.Add("Ruecklauf", typeof(double));
            Assert.True(Prozesstemperatur.PaarAusZeile(mit.Rows.Add(80.0, 55.0), out double v, out double r));
            Assert.Equal(80, v);
            Assert.Equal(55, r);
            Assert.False(Prozesstemperatur.PaarAusZeile(mit.Rows.Add(80.0, DBNull.Value), out _, out _));
        }

        /// <summary>
        /// (a) Die Kennlinie für den Prozessanteil ist die UNTERSTE, die den Vorlauf erreicht; darüber
        /// die oberste bei erlaubter Extrapolation, sonst keine.
        /// </summary>
        [Fact]
        public void Die_Kennlinie_fuer_den_Prozessanteil()
        {
            SimulationWaermepumpe._Kenndaten[] k = new[] { 35, 45, 55, 65, 75 }
                .Select(v => new SimulationWaermepumpe._Kenndaten { Vorlauf = v }).ToArray();

            Assert.Equal(65, SimulationWaermepumpe.ProzessKennlinieWaehlen(k, k[1], 58, false, out bool o1).Vorlauf);
            Assert.False(o1);
            Assert.Equal(55, SimulationWaermepumpe.ProzessKennlinieWaehlen(k, k[1], 55, false, out _).Vorlauf);
            Assert.Equal(75, SimulationWaermepumpe.ProzessKennlinieWaehlen(k, k[1], 90, true, out bool o2).Vorlauf);
            Assert.True(o2);
            Assert.Null(SimulationWaermepumpe.ProzessKennlinieWaehlen(k, k[1], 90, false, out _));
            // Ohne geladene Kennlinien zählt die feste.
            Assert.Null(SimulationWaermepumpe.ProzessKennlinieWaehlen(null, k[1], 50, false, out _));
            Assert.Equal(45, SimulationWaermepumpe.ProzessKennlinieWaehlen(null, k[1], 40, false, out _).Vorlauf);
        }

        /// <summary>
        /// (d) Die Entnahmegrenze des Puffers: geschichtet der Prozessvorlauf als Mindesttemperatur,
        /// ungeschichtet mit gepflegtem Paar unter dem Prozessvorlauf gesperrt, sonst wie zuvor.
        /// </summary>
        [Fact]
        public void Die_Entnahmegrenze_des_Puffers()
        {
            var a = new double[8760];
            a[5] = 10;
            var n = new Prozesstemperatur();
            n.Aufnehmen(70, 50, a);
            n.Abschliessen();
            var schleife = new Kaskadenschleife { Prozesstemperatur = n };

            var einfach = new SimulationPufferspeicher();
            einfach.Init(1000, 60, 40, 0);
            Assert.True(double.IsPositiveInfinity(schleife.ProzessEntnahmeGrenze(einfach, Kanal.PROZESS, 5)));
            Assert.True(double.IsNaN(schleife.ProzessEntnahmeGrenze(einfach, Kanal.PROZESS, 6)));   // keine Forderung
            Assert.True(double.IsNaN(schleife.ProzessEntnahmeGrenze(einfach, Kanal.HEIZUNG, 5)));   // anderer Kanal

            var warm = new SimulationPufferspeicher();
            warm.Init(1000, 80, 40, 0);
            Assert.True(double.IsNaN(schleife.ProzessEntnahmeGrenze(warm, Kanal.PROZESS, 5)));

            var ohnePaar = new SimulationPufferspeicher();
            ohnePaar.Init(1000, 0, 0, 0);
            Assert.True(double.IsNaN(schleife.ProzessEntnahmeGrenze(ohnePaar, Kanal.PROZESS, 5)));

            var geschichtet = new SimulationPufferspeicher();
            geschichtet.Init(1000, 80, 40, 0);
            geschichtet.SchichtenAnzahl = 4;
            geschichtet.SchichtenAufbauen();
            Assert.True(geschichtet.Geschichtet);
            Assert.Equal(70, schleife.ProzessEntnahmeGrenze(geschichtet, Kanal.PROZESS, 5));

            // Die Grenze wirkt über die Entladefähigkeit: Die halb volle Schichtung hält oben 80 °C,
            // die übrigen Schichten liegen auf dem Rücklauf - mehr als die volle Hälfte kommt bei
            // 70 °C Mindesttemperatur nicht heraus.
            geschichtet.Laden(geschichtet.Q_max / 2, 5, 0);
            double alle = geschichtet.EntladefaehigkeitKanal(Kanal.PROZESS);
            geschichtet.TNutz[Kanal.PROZESS] = 70;
            double heiss = geschichtet.EntladefaehigkeitKanal(Kanal.PROZESS);
            Assert.True(heiss <= alle + 1e-9);
            Assert.True(heiss > 0);

            // Ohne Temperaturniveau nie eine Grenze.
            Assert.True(double.IsNaN(new Kaskadenschleife().ProzessEntnahmeGrenze(einfach, Kanal.PROZESS, 5)));
        }

        // =============================================================================
        //  Teil 2 - der Lauf (Rechentest auf Kopien der Referenzprojekte)
        // =============================================================================

        private const int PROJEKT_WP = 1041;          // Wärmepumpe, Direktsenke Prozesswärme Rang 2
        private const int PROZESSKOPIE_1041 = 13;     // Tab_Prozesswaerme.ID der Projektkopie „Hotel_1"

        private sealed class WpErgebnis
        {
            public double Waerme, Strom, Prozess;
            public int Kennlinie, Gesperrt;
            public bool Niveau;
        }

        private static WpErgebnis WpLauf()
        {
            var laeufer = new SimulationRunner();
            Assert.True(laeufer.Simuliere(PROJEKT_WP, out string fehler), "Lauf gescheitert: " + fehler);
            SimulationWaermepumpe wp = laeufer.sim.simulation_wp;
            return new WpErgebnis
            {
                Waerme = wp.WpWaermeproduktionGesamtKwh,
                Strom = wp.WpStrombedarfGesamtKwh,
                Prozess = wp.Direktdeckung_Kanal[Kanal.PROZESS],
                Kennlinie = wp.ProzessKennlinieStunden(0),
                Gesperrt = wp.ProzessGesperrtStunden(0),
                Niveau = laeufer.sim.simulation_Waermebedarf?.ProzessTemperatur != null
            };
        }

        private static void Paar(int idKopie, double? vorlauf, double? ruecklauf)
        {
            DataRepository.ExecuteSQL("UPDATE Tab_Prozesswaerme SET Vorlauf = ?, Ruecklauf = ? WHERE ID = ?",
                new DbParam("@v", (object)vorlauf ?? DBNull.Value),
                new DbParam("@r", (object)ruecklauf ?? DBNull.Value),
                new DbParam("@id", idKopie));
        }

        /// <summary>
        /// (a) und (b) an der Wärmepumpe von 1041: Ohne Paar kein Niveau und dieselben Zahlen; mit
        /// 60/40 °C deckt sie den Prozess an der 65-°C-Kennlinie mit mehr Strom; mit 90/60 °C und
        /// verbotener Extrapolation deckt sie den Prozesskanal nicht mehr.
        /// </summary>
        [Fact]
        public void Waermepumpe_rechnet_den_Prozessanteil_am_Prozessvorlauf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WpErgebnis basis = WpLauf();
            Assert.False(basis.Niveau);
            Assert.Equal(0, basis.Kennlinie);
            Assert.Equal(0, basis.Gesperrt);
            Assert.True(basis.Prozess > 0);

            // 60/40 °C: über der Kennlinie der Stunde (45 °C) - die 65-°C-Kennlinie rechnet.
            Paar(PROZESSKOPIE_1041, 60, 40);
            WpErgebnis warm = WpLauf();
            Assert.True(warm.Niveau);
            Assert.True(warm.Kennlinie > 0);
            Assert.True(warm.Strom > basis.Strom * 1.2, "Die Arbeitszahl am Prozessvorlauf ist kleiner.");
            Assert.True(warm.Waerme / warm.Strom < basis.Waerme / basis.Strom);

            // 90/60 °C, Extrapolation verboten: keine Kennlinie erreicht 90 °C.
            Paar(PROZESSKOPIE_1041, 90, 60);
            DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Extrapolation_erlaubt = 0 WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT_WP));
            WpErgebnis heiss = WpLauf();
            Assert.True(heiss.Gesperrt > 0);
            Assert.Equal(0, heiss.Kennlinie);
            Assert.True(heiss.Prozess < basis.Prozess * 0.01, "Die Wärmepumpe deckt den Prozesskanal nicht mehr.");

            // Zurück auf leer: wieder die Basis, Zeichen für Zeichen.
            Paar(PROZESSKOPIE_1041, null, null);
            DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Extrapolation_erlaubt = 1 WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT_WP));
            WpErgebnis zurueck = WpLauf();
            Assert.Equal(basis.Waerme, zurueck.Waerme);
            Assert.Equal(basis.Strom, zurueck.Strom);
            Assert.Equal(basis.Prozess, zurueck.Prozess);
        }

        private const int PROJEKT_KESSEL = 1050;      // Brennwertkessel mit Kennlinie, Heizkreis-Direktsenke
        private const int KESSELANLAGE_1050 = 16961;

        /// <summary>
        /// Legt in 1050 eine Prozesswärme (Katalogsatz „CONT", 50 MWh/a) an und gibt dem Kessel die
        /// Direktsenke Prozesswärme auf Rang 2. Rückgabe: die Id der Projektkopie.
        /// </summary>
        private static int ProzessIn1050()
        {
            int kopie = ProzesswaermeStammCtrl.CopyFromStamm("CONT", PROJEKT_KESSEL);
            Assert.True(kopie > 0);
            DataRepository.ExecuteSQL(
                "INSERT INTO Z_Projekt_Prozesswaerme (ID_Projekt, ID_Prozesswaerme, Bezeichner, Summe) VALUES (?, ?, ?, ?)",
                new DbParam("@p", PROJEKT_KESSEL), new DbParam("@k", kopie), new DbParam("@b", "CONT"),
                new DbParam("@s", 50.0));
            DataRepository.ExecuteSQL(
                "INSERT INTO Z_AnlageSenke (ID_Anlage, Rang, Ziel, Bedarfsart) VALUES (?, ?, ?, ?)",
                new DbParam("@a", KESSELANLAGE_1050), new DbParam("@r", 2),
                new DbParam("@z", WaermesenkeClass.ZIEL_PROZESSWAERME), new DbParam("@t", DbWerte.WS_TYP_BEIDES));
            return kopie;
        }

        /// <summary>
        /// (c) Der Brennwertkessel von 1050 sieht den Prozessrücklauf: mit 40/25 °C sinkt sein mittlerer
        /// Rücklauf unter den Rückfall 50 °C; (b) mit gepflegtem Kesselvorlauf 70 °C und 80/60 °C
        /// Prozess deckt er den Prozesskanal nicht.
        /// </summary>
        [Fact]
        public void Brennwertkessel_sieht_den_Prozessruecklauf_und_erreicht_den_Vorlauf_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int kopie = ProzessIn1050();

            // Ohne Paar: Rücklauf aus der Kette (Rückfall 50 °C), keine Prozessstunde.
            SimulationSPK ohne = KesselLauf();
            Assert.True(ohne.RechnetMitBrennwertkennlinie(0));
            Assert.Equal(0, ohne.ProzessRuecklaufStunden(0));
            Assert.Equal(50, ohne.RuecklaufMittel(0), 9);
            double prozessOhne = ohne.Direktdeckung_Kanal[Kanal.PROZESS];
            Assert.True(prozessOhne > 0);

            // (c) 40/25 °C: der Prozessrücklauf zieht den Mittelwert herunter, der Kessel deckt weiter.
            Paar(kopie, 40, 25);
            SimulationSPK mit = KesselLauf();
            Assert.True(mit.ProzessRuecklaufStunden(0) > 0);
            Assert.True(mit.RuecklaufMittel(0) < 50);
            Assert.True(mit.RuecklaufMittel(0) > 25);
            Assert.Equal(prozessOhne, mit.Direktdeckung_Kanal[Kanal.PROZESS], 6);

            // (b) Kesselvorlauf 70 °C gepflegt, Prozess 80/60 °C: keine Prozessdeckung durch den Kessel.
            Paar(kopie, 80, 60);
            DataRepository.ExecuteSQL("UPDATE Tab_Energieanlagen SET Vorlauf = 70, [Rücklauf] = 50 WHERE ID = ?",
                new DbParam("@id", KESSELANLAGE_1050));
            SimulationSPK gesperrt = KesselLauf();
            Assert.True(gesperrt.ProzessGesperrtStunden(0) > 0);
            Assert.Equal(0, gesperrt.Direktdeckung_Kanal[Kanal.PROZESS], 9);
        }

        private static SimulationSPK KesselLauf()
        {
            var laeufer = new SimulationRunner();
            Assert.True(laeufer.Simuliere(PROJEKT_KESSEL, out string fehler), "Lauf gescheitert: " + fehler);
            return laeufer.sim.simulation_spk;
        }

        private static ResourceZugriff Resource() => new ResourceZugriff();

        /// <summary>Kurzer Zugriff auf die Texte, gegen die die Prüfung meldet.</summary>
        private sealed class ResourceZugriff
        {
            public string PW_MSG_TEMPERATUR_PAAR => WindowsFormsApplication1.MyResource.Resource.PW_MSG_TEMPERATUR_PAAR;
            public string PW_MSG_TEMPERATUR_BEREICH => WindowsFormsApplication1.MyResource.Resource.PW_MSG_TEMPERATUR_BEREICH;
            public string PW_MSG_TEMPERATUR_REIHENFOLGE => WindowsFormsApplication1.MyResource.Resource.PW_MSG_TEMPERATUR_REIHENFOLGE;
        }
    }
}
