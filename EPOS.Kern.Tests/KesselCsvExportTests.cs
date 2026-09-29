using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der CSV-Export des Heizkessel-Reiters trägt dieselben Zahlen wie das
    /// Kesselbild (#568, offener Punkt 3 des Protokolls
    /// <c>SK2_Kessel_Bereitschaft_Stunden_R23</c>).</b>
    ///
    /// <para>Bis dahin führte der Export <c>Kesselleistung_stuendlich</c> (Abgabe SAMT
    /// Speicherladung) und <c>Restwaerme</c> (Rest NACH der Direktdeckung, VOR
    /// Lade-/Entladephase) — in der alten Bedeutung, während Tafel und Kesselbild seit
    /// #568 den Stufeneingang zeigen (<see cref="SimulationErgebnisCtrl.KesselbildReihen"/>).
    /// Gemessen wird hier zweimal derselbe Lauf: einmal DIREKT über
    /// <see cref="SimulationRunner"/> (die Tafelwerte, wie in
    /// <c>KesselBereitschaftTests.Das_Kesselbild_teilt_den_Stufeneingang_wie_die_Tafel</c>),
    /// einmal über den WEG der Seite (Hülle bauen, <c>Dienste.Laufen</c> fahren,
    /// <c>Dienste.CsvHeizkessel</c> exportieren) — beide Läufe sind deterministisch
    /// (auch 1045 über den Zapfprofilgenerator mit festem Seed), ihre Jahressummen
    /// müssen deshalb übereinstimmen.</para>
    /// </summary>
    [Collection("Testdatenbank")]   // die Fälle tauschen Dienste.Datei — prozessweiter Zustand
    public sealed class KesselCsvExportTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly IDateiDienst _dateiVorher = Dienste.Datei;
        private readonly string _ordner =
            Path.Combine(Path.GetTempPath(), "epos-kcsv-" + Guid.NewGuid().ToString("N"));

        public KesselCsvExportTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            Dienste.Datei = _dateiVorher;
            _kultur.Dispose();
            try { Directory.Delete(_ordner, recursive: true); } catch (IOException) { }
        }

        private sealed class Dateiprobe : IDateiDienst
        {
            internal string Antwort = "";
            public string DateiOeffnen(string titel, string filter, string startOrdner) => "";
            public string DateiSpeichern(string titel, string filter, string vorschlag) => Antwort;
            public string OrdnerWaehlen(string titel, string startOrdner) => "";
            public bool MitSystemOeffnen(string pfad) => true;
        }

        [Theory]
        [InlineData(1030)]
        [InlineData(1045)]
        public async Task Der_CSV_Export_traegt_dieselben_Zahlen_wie_das_Kesselbild(int projekt)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Die Tafelwerte unabhaengig gerechnet - derselbe Rechenweg wie
            // KesselBereitschaftTests.Das_Kesselbild_teilt_den_Stufeneingang_wie_die_Tafel.
            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(projekt, out fehler), "Lauf gescheitert: " + fehler);
            SimulationErgebnisCtrl.Kesselbildreihen erwartet =
                SimulationErgebnisCtrl.KesselbildReihen(laeufer.sim.simulation_spk);

            // Der Weg der Seite: Huelle bauen, den Dienst Laufen fahren, CSV exportieren.
            SimulationErgebnisHuelle huelle =
                SimulationErgebnisHuelle.Erzeugen(null, projekt, new BedarfsZustand());
            var dienste = (SimulationErgebnisDienste)huelle.Gaben()["Dienste"];
            Assert.NotNull(dienste.Laufen);
            Rueckmeldung lauf = await dienste.Laufen((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);

            string ziel = Path.Combine(_ordner, "kessel_" + projekt + ".csv");
            Dienste.Datei = new Dateiprobe { Antwort = ziel };
            Assert.NotNull(dienste.CsvHeizkessel);
            dienste.CsvHeizkessel!();
            Assert.True(File.Exists(ziel), "CsvHeizkessel hat keine Datei geschrieben.");

            (double kessel, double puffer, double rest) = SpaltensummenLesen(ziel);

            double kesselErwartet = erwartet.Kesselwaerme.Sum();
            double pufferErwartet = erwartet.AusPufferAndere.Sum();
            double restErwartet = erwartet.RestNachKessel.Sum();

            Assert.Equal(kesselErwartet, kessel, Toleranz(kesselErwartet));
            Assert.Equal(pufferErwartet, puffer, Toleranz(pufferErwartet));
            Assert.Equal(restErwartet, rest, Toleranz(restErwartet));
        }

        /// <summary>
        /// Die Toleranz einer Jahressumme aus auf drei Nachkommastellen gerundeten
        /// Stundenwerten (Formatstring der CSV-Zeile <c>"0.0##"</c>): 8 760 Stunden
        /// können bis zu 8 760 · 0,0005 kWh Rundung tragen — reichlich bemessen mit dem
        /// Betrag der Regressionsnetz-Toleranz (relativ 1e-4, sonst absolut).
        /// </summary>
        private static double Toleranz(double wert) => Math.Abs(wert) * 1e-4 + 10.0;

        /// <summary>Liest die drei Kesselbild-Spalten der CSV-Datei und summiert sie.</summary>
        private static (double kessel, double puffer, double rest) SpaltensummenLesen(string pfad)
        {
            string[] zeilen = File.ReadAllLines(pfad, Encoding.UTF8);
            Assert.True(zeilen.Length > 1, "CSV-Datei ohne Datenzeilen.");

            string[] kopf = zeilen[0].Split(';');
            int iKessel = Array.IndexOf(kopf, WindowsFormsApplication1.MyResource.Resource.CHART_LEGENDE_KESSELWAERME);
            int iPuffer = Array.IndexOf(kopf, WindowsFormsApplication1.MyResource.Resource.CHART_LEGENDE_PUFFER_ANDERE);
            int iRest = Array.IndexOf(kopf, WindowsFormsApplication1.MyResource.Resource.CHART_LEGENDE_REST_NACH_KESSEL);
            Assert.True(iKessel >= 0 && iPuffer >= 0 && iRest >= 0,
                        "Spaltenkopf des Kesselbildes fehlt: " + zeilen[0]);

            CultureInfo kultur = new CultureInfo("de-DE");
            double sKessel = 0.0, sPuffer = 0.0, sRest = 0.0;
            for (int i = 1; i < zeilen.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(zeilen[i])) continue;
                string[] werte = zeilen[i].Split(';');
                sKessel += double.Parse(werte[iKessel], NumberStyles.Float, kultur);
                sPuffer += double.Parse(werte[iPuffer], NumberStyles.Float, kultur);
                sRest += double.Parse(werte[iRest], NumberStyles.Float, kultur);
            }
            return (sKessel, sPuffer, sRest);
        }
    }
}
