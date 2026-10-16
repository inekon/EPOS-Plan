using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Welle RZ, Teil a — zuschaltbare Reihen in Stromgang und Wärmepumpen-Strombild</b> (Anwenderentscheid
    /// 16.10.2026, Kandidaten 1 und 2). Gemessen am Weg der Seite: Hülle bauen, <c>Dienste.Laufen</c> fahren, dann
    /// Daten, Modell und CSV über die Delegaten der Seite.
    ///
    /// <para>Die drei Zusagen je Kandidat: Ab Werk ist die neue Reihe AUS (Vorgabe und Bild wie vorher); an, steht
    /// sie im Bild; die CSV-Spalten folgen der Wahl. Die Netzreihen selbst sind keine neue Rechnung — ihre Summen
    /// treffen die Kennzahlen des Laufs.</para>
    /// </summary>
    [Collection("Testdatenbank")]   // Testdatenbank und Dienste.Datei sind prozessweiter Zustand
    public sealed class ZuschaltbareReihenStromTests : IDisposable
    {
        /// <summary>Photovoltaik und BHKW — Netzbezug UND Einspeisung.</summary>
        private const int PV_BHKW = 1018;

        /// <summary>Wärmepumpe mit Strombild.</summary>
        private const int WAERMEPUMPE = 1017;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly IDateiDienst _dateiVorher = Dienste.Datei;
        private readonly string _ordner =
            Path.Combine(Path.GetTempPath(), "epos-rza-" + Guid.NewGuid().ToString("N"));

        public ZuschaltbareReihenStromTests() => Directory.CreateDirectory(_ordner);

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

        private static async Task<SimulationErgebnisDienste> Gelaufen(int projekt)
        {
            SimulationErgebnisHuelle huelle =
                SimulationErgebnisHuelle.Erzeugen(null, projekt, new BedarfsZustand());
            var dienste = (SimulationErgebnisDienste)huelle.Gaben()["Dienste"];
            Rueckmeldung lauf = await dienste.Laufen!((anteil, text) => { });
            Assert.True(lauf.Erfolg, lauf.Text);
            return dienste;
        }

        private static List<string> Namen(Zeichenmodell m) => m.Reihen.Select(r => r.Name).ToList();

        // =====================================================================
        //  Kandidat 1: Stromgang — Netzbezug und Einspeisung
        // =====================================================================

        [Fact]
        public async Task Stromgang_fuehrt_Netzbezug_und_Einspeisung_ab_Werk_aus()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationErgebnisDienste dienste = await Gelaufen(PV_BHKW);
            SimulationErgebnisDaten d = dienste.Laden!(PV_BHKW);

            // Die zwei Reihen stehen HINTER allen anderen und sind vorhanden.
            IReadOnlyList<Ganglinienreihe> reihen = d.Stromgang.Reihen;
            Assert.Equal("NETZBEZUG", reihen[reihen.Count - 2].Schluessel);
            Assert.Equal("NETZEINSPEISUNG", reihen[reihen.Count - 1].Schluessel);
            Assert.True(reihen[reihen.Count - 2].Vorhanden);
            Assert.True(reihen[reihen.Count - 1].Vorhanden);
            Assert.Equal(R.CHART_LEGENDE_NETZBEZUG, reihen[reihen.Count - 2].Text);

            // Vorgabe des Reiters ist „nur Summe": kein Netz im Bild.
            Zeichenmodell vorgabe = dienste.Modell!(new Bildauftrag(Bilder.Stromgang, false, -1, new[] { "GESAMT" }))!;
            Assert.DoesNotContain(R.CHART_LEGENDE_NETZBEZUG, Namen(vorgabe));
            Assert.DoesNotContain(R.CHART_LEGENDE_NETZEINSPEISUNG, Namen(vorgabe));

            // Mit Wahl ja — und ohne Wahl gleicht das Bild dem mit Wahl in allen übrigen Reihen.
            Zeichenmodell an = dienste.Modell!(new Bildauftrag(Bilder.Stromgang, false, -1,
                new[] { "GESAMT", "NETZBEZUG", "NETZEINSPEISUNG" }))!;
            Assert.Contains(R.CHART_LEGENDE_NETZBEZUG, Namen(an));
            Assert.Contains(R.CHART_LEGENDE_NETZEINSPEISUNG, Namen(an));
            Assert.Equal(Namen(vorgabe), Namen(an).Where(n => n != R.CHART_LEGENDE_NETZBEZUG
                                                            && n != R.CHART_LEGENDE_NETZEINSPEISUNG).ToList());

            Zeichenmodell nurBezug = dienste.Modell!(new Bildauftrag(Bilder.Stromgang, false, -1,
                new[] { "NETZBEZUG" }))!;
            Assert.Contains(R.CHART_LEGENDE_NETZBEZUG, Namen(nurBezug));
            Assert.DoesNotContain(R.CHART_LEGENDE_NETZEINSPEISUNG, Namen(nurBezug));
        }

        [Fact]
        public async Task Die_Netzreihen_treffen_die_Kennzahlen_des_Laufs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var r = new SimulationRunner();
            Assert.True(r.Simuliere(PV_BHKW, out string fehler), "Lauf gescheitert: " + fehler);
            SimulationErgebnisCtrl.Netzreihen netz = SimulationErgebnisCtrl.NetzreihenViertelstunde(r.sim);
            Assert.NotNull(netz.NetzbezugKw);
            Assert.NotNull(netz.EinspeisungKw);
            Assert.Equal(35040, netz.NetzbezugKw.Length);
            Assert.Equal(35040, netz.EinspeisungKw.Length);

            // Netzbezug = Reststrom des Laufs [MWh].
            Assert.Equal(r.sim.ReststromMwh, netz.NetzbezugKw.Sum() / 4000.0, 6);

            // Einspeisung = PV-Einspeisung + BHKW-Einspeisung des Zeitreihensatzes (Stundenmittel).
            ZeitreihenSatz z = ZeitreihenExtraktor.AusLauf(r);
            double erwartetKwh = (z.Hole(ZeitreihenSatz.PV_UEBERSCHUSS)?.Sum() ?? 0.0)
                               + (z.Hole(ZeitreihenSatz.BHKW_UEBERSCHUSS)?.Sum() ?? 0.0);
            Assert.True(erwartetKwh > 0.0, "Projekt 1018 speist ein.");
            Assert.Equal(erwartetKwh, netz.EinspeisungKw.Sum() / 4.0, Math.Max(1e-6 * erwartetKwh, 0.6));
        }

        [Fact]
        public void Ohne_Lauf_gibt_es_keine_Netzreihen()
            => Assert.Null(SimulationErgebnisCtrl.NetzreihenViertelstunde(null));

        [Fact]
        public async Task Die_CSV_des_Stromgangs_folgt_der_Wahl()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationErgebnisDienste dienste = await Gelaufen(PV_BHKW);
            Assert.NotNull(dienste.CsvStromgang);

            string ohne = Path.Combine(_ordner, "ohne.csv");
            Dienste.Datei = new Dateiprobe { Antwort = ohne };
            dienste.CsvStromgang!(new[] { "PROFIL_LASTGANG" });
            string kopfOhne = File.ReadLines(ohne, Encoding.UTF8).First();
            Assert.DoesNotContain(R.CHART_LEGENDE_NETZBEZUG, kopfOhne);
            Assert.DoesNotContain(R.CHART_LEGENDE_NETZEINSPEISUNG, kopfOhne);

            string mit = Path.Combine(_ordner, "mit.csv");
            Dienste.Datei = new Dateiprobe { Antwort = mit };
            dienste.CsvStromgang!(new[] { "PROFIL_LASTGANG", "NETZBEZUG", "NETZEINSPEISUNG" });
            string kopfMit = File.ReadLines(mit, Encoding.UTF8).First();
            Assert.Contains(R.CHART_LEGENDE_NETZBEZUG, kopfMit);
            Assert.Contains(R.CHART_LEGENDE_NETZEINSPEISUNG, kopfMit);
            Assert.Equal(kopfOhne.Split(';').Length + 2, kopfMit.Split(';').Length);
        }

        // =====================================================================
        //  Kandidat 2: Wärmepumpe — Strom von Wärmepumpe und Heizstab getrennt
        // =====================================================================

        [Fact]
        public async Task Das_WP_Strombild_trennt_nur_auf_Wahl()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SimulationErgebnisDienste dienste = await Gelaufen(WAERMEPUMPE);

            // Vorgabe: der Auftrag ohne Reihen - die eine Linie, dasselbe PNG wie mit ausdrücklichem null.
            Zeichenmodell vorgabe = dienste.Modell!(new Bildauftrag(Bilder.WpStromverbrauch))!;
            Zeichenmodell auchVorgabe = dienste.Modell!(new Bildauftrag(Bilder.WpStromverbrauch, false, -1, null))!;
            Assert.Single(vorgabe.Reihen);
            Assert.Equal(SkiaMaler.Png(vorgabe), SkiaMaler.Png(auchVorgabe));
            Assert.DoesNotContain(R.CHART_SEGMENT_HEIZSTAB, Namen(vorgabe));

            // Getrennt: Wärmepumpe, Heizstab und die Summe darüber.
            Zeichenmodell getrennt = dienste.Modell!(new Bildauftrag(Bilder.WpStromverbrauch, false, -1,
                new[] { "WP_STROM", "HEIZSTAB_STROM", "WP_STROM_SUMME" }))!;
            List<string> namen = Namen(getrennt);
            Assert.Contains(R.SIM_ERZEUGERNAME_WAERMEPUMPE, namen);
            Assert.Contains(R.CHART_SEGMENT_HEIZSTAB, namen);
            Assert.Contains(R.CHART_LEGENDE_SUMME_STROM_WP, namen);

            // Die Summe ist dieselbe Reihe wie die eine Linie der Vorgabe.
            double[] linie = vorgabe.Reihen[0].Werte;
            double[] summe = getrennt.Reihen.First(x => x.Name == R.CHART_LEGENDE_SUMME_STROM_WP).Werte;
            Assert.Equal(linie.Sum(), summe.Sum(), 6);
        }
    }
}
