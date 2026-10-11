using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Referenzlauf;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Gleichwertigkeitsprobe des Rückkühlwerks</b> (K-F1; Entwurf Split/VRF/Rückkühlwerk 5.6): Ein Referenzprojekt
    /// mit Kältemaschine rechnet auf einer Kopie der Testdatenbank einmal ohne und einmal mit einem Rückkühlwerk
    /// <c>TROCKEN</c>/<c>FEST</c>/leere Felder/<c>PARALLEL</c> an seiner Kältemaschinen-Anlage — die Ordner des Referenzlaufs
    /// (<c>Ergebnisexport.ProjektAusfuehren</c>) und die Ergebniszeilen der Kältemaschine (<c>Tab_ErgebnisKaeltemaschine</c>)
    /// sind byte-gleich. Gegenprobe: dieselbe Kopie mit +2 K Annäherung rechnet mehr Kältestrom. Ein Datenbanktest, nicht in
    /// der Basis.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RueckkuehlwerkGleichwertigkeitTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public RueckkuehlwerkGleichwertigkeitTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        /// <summary>Die Kältemaschinen-Anlage (Typ 13) des Projekts — genau eine.</summary>
        private static int Anlage(int projekt) =>
            Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                new DbParam("?", projekt), new DbParam("?", KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE)), CultureInfo.InvariantCulture);

        private static string Ordner(string name)
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-kf1b-" + name + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static int Export(int projekt, string ziel)
        {
            var log = new Protokoll();
            int n = Ergebnisexport.ProjektAusfuehren(projekt, ziel, log);
            Assert.True(n > 0, "Der Lauf von " + projekt + " scheiterte (" + log.Fehler + " Fehler).");
            return n;
        }

        /// <summary>Die Ergebniszeilen der Kältemaschine des jüngsten Laufs, jede Zahl rundungsfrei als Text.</summary>
        private static List<string> KaelteZeilen(int projekt)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT k.* FROM Tab_ErgebnisKaeltemaschine k WHERE k.ID_Ergebnis = " +
                "(SELECT MAX(ID) FROM Tab_Ergebnis WHERE ID_Projekt = ?) ORDER BY k.ID_Kaeltemaschine, k.ID",
                new DbParam("?", projekt));
            var zeilen = new List<string>();
            foreach (DataRow r in t.Rows)
                zeilen.Add(string.Join("|", t.Columns.Cast<DataColumn>()
                    .Where(c => c.ColumnName != "ID" && c.ColumnName != "ID_Ergebnis")
                    .Select(c => c.ColumnName + "=" + (r[c] is double d ? d.ToString("R", CultureInfo.InvariantCulture)
                                                       : Convert.ToString(r[c], CultureInfo.InvariantCulture)))));
            return zeilen;
        }

        private static double KaelteSumme(int projekt, string spalte) =>
            Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT SUM(" + spalte + ") FROM Tab_ErgebnisKaeltemaschine WHERE ID_Ergebnis = " +
                "(SELECT MAX(ID) FROM Tab_Ergebnis WHERE ID_Projekt = ?)", new DbParam("?", projekt)), CultureInfo.InvariantCulture);

        /// <summary>Vergleicht zwei Ordner des Referenzlaufs Byte für Byte (ohne <c>protokoll.txt</c>); die Zahl der Bytes.</summary>
        private static long Bytegleich(string a, string b, out int dateien)
        {
            string[] da = Directory.GetFiles(a).Select(Path.GetFileName).Where(f => f != "protokoll.txt").OrderBy(f => f, StringComparer.Ordinal).ToArray();
            string[] dbb = Directory.GetFiles(b).Select(Path.GetFileName).Where(f => f != "protokoll.txt").OrderBy(f => f, StringComparer.Ordinal).ToArray();
            Assert.Equal(da, dbb);
            long bytes = 0;
            foreach (string f in da)
            {
                byte[] x = File.ReadAllBytes(Path.Combine(a, f)), y = File.ReadAllBytes(Path.Combine(b, f));
                Assert.True(x.AsSpan().SequenceEqual(y), f + " weicht mit Rückkühlwerk ab.");
                bytes += x.Length;
            }
            dateien = da.Length;
            return bytes;
        }

        [Theory]
        [InlineData(1055)]   // Kältemaschine mit Trocken-Rückkühler und Kältespeicher
        [InlineData(1063)]   // dieselbe mit Teillastkurve, Takten und Gütegrad-Extrapolation
        public void Ein_Rueckkuehlwerk_mit_leeren_Feldern_rechnet_bytegleich(int projekt)
        {
            if (!_db.Vorhanden) return;
            int anlage = Anlage(projekt);
            Assert.Null(KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);

            string ohne = Ordner("ohne"), mit = Ordner("mit");
            try
            {
                Export(projekt, ohne);
                List<string> zeilenOhne = KaelteZeilen(projekt);
                double stromOhne = KaelteSumme(projekt, "Stromverbrauch_MWh");
                Assert.NotEmpty(zeilenOhne);

                RueckkuehlwerkStammCtrl.SpeicherErgebnis s = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel
                {
                    Bezeichner = "RK Gleichwertigkeit", Bauart = RueckkuehlwerkSchema.BAUART_TROCKEN,
                    Annaeherung_Weg = RueckkuehlwerkSchema.WEG_FEST, Freikuehlung_Schaltung = RueckkuehlwerkSchema.SCHALTUNG_PARALLEL
                });
                Assert.True(s.Ok, s.Meldung);
                Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, s.Id));
                int kopie = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;

                Export(projekt, mit);
                long bytes = Bytegleich(ohne, mit, out int dateien);
                Assert.Equal(zeilenOhne, KaelteZeilen(projekt));
                _aus.WriteLine("{0}: mit Rückkühlwerk TROCKEN/FEST/leer/PARALLEL {1} Dateien, {2} Bytes byte-gleich, {3} Ergebniszeilen gleich",
                               projekt, dateien, bytes, zeilenOhne.Count);

                // Gegenprobe: +2 K Annäherung über dem Festwert - wärmerer Rückkühler, mehr Kältestrom.
                RueckkuehlwerkModel m = RueckkuehlwerkCtrl.Laden(kopie);
                m.Annaeherung_Nenn_K = KaelteFestwerte.GRAEDIGKEIT_TROCKENKUEHLER_K + 2.0;
                Assert.Null(RueckkuehlwerkCtrl.Speichern(m));
                var lauf = new SimulationRunner();
                Assert.True(lauf.SimuliereUndSpeichere(projekt, out string fehler) > 0, "Lauf gescheitert: " + fehler);
                double stromWaermer = KaelteSumme(projekt, "Stromverbrauch_MWh");
                Assert.NotEqual(zeilenOhne, KaelteZeilen(projekt));
                Assert.True(stromWaermer > stromOhne,
                            "Kältestrom mit +2 K " + stromWaermer.ToString("R", CultureInfo.InvariantCulture) +
                            " MWh nicht über " + stromOhne.ToString("R", CultureInfo.InvariantCulture) + " MWh");
                Assert.DoesNotContain(lauf.Protokoll.Hinweise, h => h.Contains("RK Gleichwertigkeit", StringComparison.Ordinal));
                _aus.WriteLine("{0}: Kältestrom {1:F4} MWh, mit +2 K {2:F4} MWh", projekt, stromOhne, stromWaermer);
            }
            finally
            {
                Directory.Delete(ohne, true);
                Directory.Delete(mit, true);
            }
        }

        [Fact]
        public void Was_K_F1_nicht_rechnet_rechnet_fest_und_wird_einmal_je_Anlage_benannt()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1055;
            int anlage = Anlage(projekt);
            var lauf = new SimulationRunner();
            Assert.True(lauf.SimuliereUndSpeichere(projekt, out string fehler) > 0, "Lauf gescheitert: " + fehler);
            List<string> zeilenOhne = KaelteZeilen(projekt);

            RueckkuehlwerkStammCtrl.SpeicherErgebnis s = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel
            {
                Bezeichner = "RK Vorgriff", Bauart = RueckkuehlwerkSchema.BAUART_TROCKEN, Nennleistung_kW = 80,
                Annaeherung_Weg = RueckkuehlwerkSchema.WEG_LASTABHAENGIG, Ventilator_Nenn_kW = 1.5,
                Ventilator_Regelung = RueckkuehlwerkSchema.REGELUNG_DREHZAHL, Ventilator_Drehzahl_Min = 0.2,
                Freikuehlung_Schaltung = RueckkuehlwerkSchema.SCHALTUNG_REIHE
            });
            Assert.True(s.Ok, s.Meldung);
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, s.Id));

            lauf = new SimulationRunner();
            Assert.True(lauf.SimuliereUndSpeichere(projekt, out fehler) > 0, "Lauf gescheitert: " + fehler);
            // Der feste Weg: dieselben Ergebnisse wie ohne Rückkühlwerk.
            Assert.Equal(zeilenOhne, KaelteZeilen(projekt));
            string hinweis = Assert.Single(lauf.Protokoll.Hinweise, h => h.Contains("RK Vorgriff", StringComparison.Ordinal));
            // Drei Merkmale in EINER Aufzählung.
            Assert.Contains(SimulationControl.RueckkuehlwerkMerkmale(new[]
            {
                Rueckkuehlwerk.MERKMAL_LASTABHAENGIG, Rueckkuehlwerk.MERKMAL_VENTILATOR, Rueckkuehlwerk.MERKMAL_REIHE
            }), hinweis, StringComparison.Ordinal);
            _aus.WriteLine(hinweis);
        }
    }
}
