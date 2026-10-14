using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Pufferoptionen und die Desinfektion im Lauf</b> (Welle M7; Konzept Simulationsablauf 21) auf
    /// Kopien der Referenzprojekte 1049 (Kollektorfeld vor BHKW und Kessel am Puffer) und 1045
    /// (Brauchwasser über den Zapfprofilgenerator, Kombispeicher an der Wärmepumpe).
    ///
    /// <para><b>Geprüft wird:</b> Leere Felder und ausdrücklich gesetzte Vorgaben rechnen bitgleich; der
    /// Weg „temperatur" verliert anders als der Tageswert und mit wärmerem Aufstellraum weniger; die
    /// Zonenanteile halten die Schicht-Invariante; das Frischwassermodul an einem zu kalten Speicher
    /// zählt Nachheizstunden; die Desinfektion bringt 52 Ereignisse mit V · 1,163 · ΔT.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PufferOptionenLaufTests : IDisposable
    {
        private const int PROJEKT_PUFFER = 1049;
        private const int PUFFER_1049 = 1054225;
        private const int PROJEKT_BW = 1045;
        private const int KOMBI_1045 = 1054210;
        private const int BW_PUFFER_1045 = 1054214;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly Xunit.Abstractions.ITestOutputHelper _aus;

        public PufferOptionenLaufTests(Xunit.Abstractions.ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _kultur.Dispose();

        private sealed class Lauf
        {
            public double Restwaerme, Reststrom, Verluste, VerlusteTemperatur;
            public int Verletzungen;
            public SimulationControl Sim;
        }

        private static Lauf Rechne(int projekt, int puffer)
        {
            var laeufer = new SimulationRunner();
            Assert.True(laeufer.Simuliere(projekt, out string fehler), "Lauf gescheitert: " + fehler);
            SimulationControl sim = laeufer.sim;
            sim.speicherRegistry.TryGetValue(puffer, out SimulationPufferspeicher sp);
            return new Lauf
            {
                Restwaerme = sim.RestwaermeMwh,
                Reststrom = sim.ReststromMwh,
                Verluste = sp?.Verluste_gesamt ?? double.NaN,
                VerlusteTemperatur = sp?.BereitschaftTemperaturKwh ?? double.NaN,
                Verletzungen = sp?.SchichtInvarianteVerletzungen ?? -1,
                Sim = sim
            };
        }

        private static void Puffer(int id, string spalte, object wert)
            => DataRepository.ExecuteSQL("UPDATE Tab_Pufferspeicher SET \"" + spalte + "\" = ? WHERE ID = ?",
                                         new DbParam("@w", wert ?? DBNull.Value), new DbParam("@id", id));

        private static void Einstellung(int projekt, string spalte, object wert)
            => DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET \"" + spalte + "\" = ? WHERE ID_Projekt = ?",
                                         new DbParam("@w", wert ?? DBNull.Value), new DbParam("@p", projekt));

        /// <summary>
        /// Schreib- und Lesewege: Die Optionen gehen über <c>PufferSpCtrl.SchichtdatenSchreiben</c> hin und
        /// kommen über <c>SchichtdatenLesen</c> zurück; leer schreibt NULL. Die Desinfektion geht über
        /// <c>KonfigurationCtrl.DesinfektionSetzen</c> hin und über <c>DesinfektionLesen</c> zurück.
        /// </summary>
        [Fact]
        public void Optionen_und_Desinfektion_gehen_hin_und_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            PufferSpCtrl.Schichtdaten d = PufferSpCtrl.SchichtdatenLesen(PUFFER_1049);
            Assert.True(d.OptionenLeer);
            d.Schichten = 4;
            d.BereitschaftWeg = DbWerte.PSP_BEREITSCHAFT_TEMPERATUR;
            d.AufstellraumC = 15;
            d.SchichtAnteile = "0,10;0,16;0,37;0,37";
            d.Frischwassermodul = true;
            d.FwmGraedigkeitK = 7;
            Assert.True(PufferSpCtrl.SchichtdatenSchreiben(PUFFER_1049, d));

            PufferSpCtrl.Schichtdaten z = PufferSpCtrl.SchichtdatenLesen(PUFFER_1049);
            Assert.Equal(DbWerte.PSP_BEREITSCHAFT_TEMPERATUR, z.BereitschaftWeg);
            Assert.Equal(15.0, z.AufstellraumC);
            Assert.Equal("0,10;0,16;0,37;0,37", z.SchichtAnteile);
            Assert.True(z.Frischwassermodul);
            Assert.Equal(7.0, z.FwmGraedigkeitK);

            Assert.True(PufferSpCtrl.SchichtdatenSchreiben(PUFFER_1049, new PufferSpCtrl.Schichtdaten()));
            Assert.True(PufferSpCtrl.SchichtdatenLesen(PUFFER_1049).OptionenLeer);

            Assert.False(KonfigurationCtrl.DesinfektionLesen(PROJEKT_BW).Aktiv);
            var v = new Desinfektionsvorgabe(true, 14, 3, 65, 800);
            Assert.True(KonfigurationCtrl.DesinfektionSetzen(PROJEKT_BW, v));
            Assert.Equal(v, KonfigurationCtrl.DesinfektionLesen(PROJEKT_BW));
            Assert.True(KonfigurationCtrl.DesinfektionSetzen(PROJEKT_BW, Desinfektionsvorgabe.Aus));
            Assert.Equal(Desinfektionsvorgabe.Aus, KonfigurationCtrl.DesinfektionLesen(PROJEKT_BW));
        }

        /// <summary>
        /// 1049: Ausdrücklich „tag", kein Modul und Desinfektion aus rechnen bitgleich wie leer; der Weg
        /// „temperatur" verliert anders, und ein Aufstellraum von 30 °C verliert weniger als 20 °C.
        /// </summary>
        [Fact]
        public void Bereitschaft_nach_Temperatur_am_Puffer_von_1049()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Lauf leer = Rechne(PROJEKT_PUFFER, PUFFER_1049);
            Assert.True(leer.Verluste > 0);
            Assert.Equal(0.0, leer.VerlusteTemperatur);

            Puffer(PUFFER_1049, PufferOptionenSchema.SPALTE_BEREITSCHAFT_WEG, DbWerte.PSP_BEREITSCHAFT_TAG);
            Puffer(PUFFER_1049, PufferOptionenSchema.SPALTE_FRISCHWASSERMODUL, 0);
            Einstellung(PROJEKT_PUFFER, PufferOptionenSchema.SPALTE_DESINFEKTION_AKTIV, 0);
            Lauf tag = Rechne(PROJEKT_PUFFER, PUFFER_1049);
            Assert.Equal(leer.Verluste, tag.Verluste);
            Assert.Equal(leer.Restwaerme, tag.Restwaerme);
            Assert.Equal(leer.Reststrom, tag.Reststrom);

            Puffer(PUFFER_1049, PufferOptionenSchema.SPALTE_BEREITSCHAFT_WEG, DbWerte.PSP_BEREITSCHAFT_TEMPERATUR);
            Lauf temp = Rechne(PROJEKT_PUFFER, PUFFER_1049);
            Assert.True(temp.Verluste > 0);
            Assert.Equal(temp.Verluste, temp.VerlusteTemperatur, 9);
            Assert.NotEqual(leer.Verluste, temp.Verluste);

            Puffer(PUFFER_1049, PufferOptionenSchema.SPALTE_AUFSTELLRAUM, 30.0);
            Lauf warm = Rechne(PROJEKT_PUFFER, PUFFER_1049);
            Assert.True(warm.Verluste < temp.Verluste,
                        "30 °C Aufstellraum: " + warm.Verluste + " kWh, 20 °C: " + temp.Verluste + " kWh");
            _aus.WriteLine("Bereitschaft 1049: tag " + leer.Verluste.ToString("0.0") + " kWh, temperatur 20 °C " +
                           temp.Verluste.ToString("0.0") + " kWh, 30 °C " + warm.Verluste.ToString("0.0") + " kWh");
        }

        /// <summary>1049 mit vier Zonen und Kombi-Anteilen: der Lauf geht durch, die Invariante hält.</summary>
        [Fact]
        public void Zonenanteile_am_Puffer_von_1049()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Puffer(PUFFER_1049, SchemaKatalog.SPALTE_PSP_SCHICHTEN_ANZAHL, 4);
            Lauf gleich = Rechne(PROJEKT_PUFFER, PUFFER_1049);
            Assert.Equal(0, gleich.Verletzungen);

            Puffer(PUFFER_1049, PufferOptionenSchema.SPALTE_SCHICHT_ANTEILE, "0,10;0,16;0,37;0,37");
            Lauf anteile = Rechne(PROJEKT_PUFFER, PUFFER_1049);
            Assert.Equal(0, anteile.Verletzungen);
            Assert.True(anteile.Verluste > 0);

            // Eine abgelehnte Angabe (Summe 0,9) rechnet wie gleich große Zonen.
            Puffer(PUFFER_1049, PufferOptionenSchema.SPALTE_SCHICHT_ANTEILE, "0,10;0,16;0,37;0,27");
            Lauf abgelehnt = Rechne(PROJEKT_PUFFER, PUFFER_1049);
            Assert.Equal(gleich.Verluste, abgelehnt.Verluste);
            Assert.Equal(gleich.Restwaerme, abgelehnt.Restwaerme);
        }

        /// <summary>
        /// 1045: Der Brauchwasserpuffer, verkleinert auf 300 l bei 70/40 °C, mit Frischwassermodul
        /// (60 + 5 °C): In einem Teil der Stunden hält die oberste Zone 65 °C nicht — der Kanal zapft dann
        /// nur, was darüber liegt, und die Nachheizstunden sind gezählt; der Puffer gibt weniger
        /// Brauchwasser ab als ohne Modul.
        /// </summary>
        [Fact]
        public void Frischwassermodul_am_kleinen_Brauchwasserpuffer_von_1045()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Puffer(BW_PUFFER_1045, "Gesamtvolumen", 300);
            Puffer(BW_PUFFER_1045, "Vorlauf", 70);
            Puffer(BW_PUFFER_1045, "Ruecklauf", 40);
            Lauf ohne = Rechne(PROJEKT_BW, BW_PUFFER_1045);
            Assert.Empty(ohne.Sim.FrischwasserBegrenzteStunden);
            double bwOhne = ohne.Sim.speicherRegistry[BW_PUFFER_1045].Entladung_Kanal[Kanal.BRAUCHWASSER];

            Puffer(BW_PUFFER_1045, PufferOptionenSchema.SPALTE_FRISCHWASSERMODUL, 1);
            Lauf mit = Rechne(PROJEKT_BW, BW_PUFFER_1045);
            Assert.True(mit.Sim.FrischwasserBegrenzteStunden.TryGetValue(BW_PUFFER_1045, out int stunden));
            Assert.True(stunden > 0, "Nachheizstunden: " + stunden);
            Assert.True(stunden < 8760, "Nachheizstunden: " + stunden);
            double bwMit = mit.Sim.speicherRegistry[BW_PUFFER_1045].Entladung_Kanal[Kanal.BRAUCHWASSER];
            Assert.True(bwMit < bwOhne, "Brauchwasser aus dem Puffer mit Modul " + bwMit + " kWh, ohne " + bwOhne + " kWh");

            // Ein Speicher, der kein Brauchwasser führt, bekommt kein Modul.
            Puffer(1054212, PufferOptionenSchema.SPALTE_FRISCHWASSERMODUL, 1);
            Assert.False(Rechne(PROJEKT_BW, BW_PUFFER_1045).Sim.FrischwasserBegrenzteStunden.ContainsKey(1054212));
            _aus.WriteLine("FWM 1045: Nachheizstunden " + stunden + ", Brauchwasser aus dem Puffer " +
                           bwOhne.ToString("0.0") + " -> " + bwMit.ToString("0.0") + " kWh");
        }

        /// <summary>
        /// 1045 mit Desinfektion alle 7 Tage, 1 000 l, 70 °C gegen 60 °C (der Generator führt keine
        /// Speichersolltemperatur): 52 Ereignisse, je 1 000 · 1,163 · 10 / 1000 kWh, gedeckt von einer
        /// Stufe oder vom Zusatzstrom.
        /// </summary>
        [Fact]
        public void Desinfektion_im_Brauchwasser_von_1045()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Lauf ohne = Rechne(PROJEKT_BW, KOMBI_1045);
            Assert.Equal(0.0, ohne.Sim.simulation_Waermebedarf.Brauchwasser_Desinfektion_Mwh);
            Assert.Equal(0.0, ohne.Sim.DesinfektionZusatzstromKwh);

            Einstellung(PROJEKT_BW, PufferOptionenSchema.SPALTE_DESINFEKTION_AKTIV, 1);
            Einstellung(PROJEKT_BW, PufferOptionenSchema.SPALTE_DESINFEKTION_VOLUMEN, 1000.0);
            Lauf mit = Rechne(PROJEKT_BW, KOMBI_1045);
            SimulationWaermebedarf b = mit.Sim.simulation_Waermebedarf;

            double jeEreignis = 1000 * 1.163 * (70 - 60) / 1000.0;
            Assert.Equal(52, b.DesinfektionEreignisse);
            Assert.Equal(52, b.Brauchwasser_Desinfektion_stuendlich.Count(v => v > 0));
            Assert.All(b.Brauchwasser_Desinfektion_stuendlich.Where(v => v > 0), v => Assert.Equal(jeEreignis, v, 12));
            Assert.Equal(52 * jeEreignis / 1000.0, b.Brauchwasser_Desinfektion_Mwh, 9);
            // Ereignisse um 2 Uhr am 7., 14., … Tag.
            Assert.True(b.Brauchwasser_Desinfektion_stuendlich[6 * 24 + 2] > 0);
            Assert.Equal(0.0, b.Brauchwasser_Desinfektion_stuendlich[6 * 24 + 3]);

            // Die Bilanz geht auf: gedeckt je Stufe plus Zusatzstrom gleich Bedarf.
            double gedeckt = mit.Sim.DesinfektionGedecktJeStufe.Values.Sum();
            Assert.Equal(52 * jeEreignis, gedeckt + mit.Sim.DesinfektionZusatzstromKwh, 6);
            _aus.WriteLine("Desinfektion 1045: " + b.DesinfektionEreignisse + " Ereignisse, " +
                           b.Brauchwasser_Desinfektion_Mwh.ToString("0.00000") + " MWh, gedeckt " +
                           string.Join(", ", mit.Sim.DesinfektionGedecktJeStufe.Select(e => e.Key + " " + e.Value.ToString("0.00"))) +
                           ", Zusatzstrom " + mit.Sim.DesinfektionZusatzstromKwh.ToString("0.00") + " kWh");
            // Kein Restwärmebedarf entsteht aus der Desinfektion.
            Assert.Equal(ohne.Restwaerme, mit.Restwaerme, 6);

            // Hält der Brauchwasserpuffer 75/45 °C, gibt er den Zusatzbedarf ab - geladen vom Kessel, der
            // ohne gepflegten Vorlauf als fähig gilt; der Zusatzstrom sinkt.
            Puffer(BW_PUFFER_1045, "Vorlauf", 75);
            Puffer(BW_PUFFER_1045, "Ruecklauf", 45);
            Lauf warm = Rechne(PROJEKT_BW, BW_PUFFER_1045);
            double ausPuffer = warm.Sim.DesinfektionGedecktJeStufe.Values.Sum();
            Assert.True(ausPuffer > 0, "aus dem Puffer gedeckt: " + ausPuffer);
            Assert.True(warm.Sim.DesinfektionZusatzstromKwh < mit.Sim.DesinfektionZusatzstromKwh);
            Assert.Equal(52 * jeEreignis, ausPuffer + warm.Sim.DesinfektionZusatzstromKwh, 6);
            _aus.WriteLine("Desinfektion 1045 mit Puffer 75/45: gedeckt " +
                           string.Join(", ", warm.Sim.DesinfektionGedecktJeStufe.Select(e => e.Key + " " + e.Value.ToString("0.00"))) +
                           ", Zusatzstrom " + warm.Sim.DesinfektionZusatzstromKwh.ToString("0.00") + " kWh");
        }
    }
}
