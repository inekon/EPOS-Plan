using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Das Supportpaket (Konzept Administration und Support, Abschnitt 5, AS-Q6): der
    /// Diagnoseblock steht drin, vorhandene Protokolle landen im Paket, fehlende in
    /// <see cref="Supportpaketergebnis.Fehlend"/>, das KI-Protokoll nur mit Haken, nie eine
    /// Datenbank, und ein Schreibfehler am Ziel kommt benannt zurueck.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SupportpaketTests : IDisposable
    {
        private const string DIAGNOSE = "Programmversion: 9.8.7-probe\r\nSprache: de-DE";

        private readonly string _wurzel;
        private readonly string _logs;
        private readonly string _dbOrdner;

        public SupportpaketTests()
        {
            _wurzel = Path.Combine(Path.GetTempPath(), "epos-supportpaket-" + Guid.NewGuid().ToString("N"));
            _dbOrdner = Path.Combine(_wurzel, "db");
            _logs = Path.Combine(_dbOrdner, "Logs");
            Directory.CreateDirectory(_logs);
        }

        public void Dispose()
        {
            try { Directory.Delete(_wurzel, true); } catch { /* bleibt liegen */ }
        }

        private string Ziel => Path.Combine(_wurzel, "paket.zip");

        private static string[] Eintraege(string zip)
        {
            using var archiv = ZipFile.OpenRead(zip);
            return archiv.Entries.Select(e => e.FullName).ToArray();
        }

        private static string Inhalt(string zip, string eintrag)
        {
            using var archiv = ZipFile.OpenRead(zip);
            using var leser = new StreamReader(archiv.GetEntry(eintrag)!.Open(), Encoding.UTF8);
            return leser.ReadToEnd();
        }

        [Fact]
        public void Diagnosedaten_stehen_im_Paket()
        {
            var erg = Supportpaket.Erstellen(Ziel, DIAGNOSE, false, _logs, _dbOrdner);

            Assert.True(erg.Geschrieben, erg.Fehler);
            Assert.Equal(Path.GetFullPath(Ziel), erg.Pfad);
            Assert.Contains(Supportpaket.EINTRAG_DIAGNOSE, Eintraege(Ziel));
            Assert.Equal(DIAGNOSE, Inhalt(Ziel, Supportpaket.EINTRAG_DIAGNOSE));
            Assert.Equal(Eintraege(Ziel), erg.Enthalten.ToArray());
        }

        [Fact]
        public void Vorhandene_Protokolle_landen_im_Paket_fehlende_stehen_in_Fehlend()
        {
            File.WriteAllText(Path.Combine(_logs, Ausnahmeprotokoll.DATEI), "ausnahme");
            File.WriteAllText(Path.Combine(_dbOrdner, Supportpaket.MIGRATIONSPROTOKOLL), "migration");

            var erg = Supportpaket.Erstellen(Ziel, DIAGNOSE, false, _logs, _dbOrdner);

            Assert.True(erg.Geschrieben, erg.Fehler);
            string[] eintraege = Eintraege(Ziel);
            Assert.Contains(Ausnahmeprotokoll.DATEI, eintraege);
            Assert.Contains(Supportpaket.MIGRATIONSPROTOKOLL, eintraege);
            Assert.Equal("ausnahme", Inhalt(Ziel, Ausnahmeprotokoll.DATEI));
            Assert.Equal("migration", Inhalt(Ziel, Supportpaket.MIGRATIONSPROTOKOLL));
            Assert.DoesNotContain(Ausnahmeprotokoll.DATEI_ALT, eintraege);
            Assert.Contains(Ausnahmeprotokoll.DATEI_ALT, erg.Fehlend);
            Assert.DoesNotContain(Ausnahmeprotokoll.DATEI, erg.Fehlend);
        }

        [Fact]
        public void Ohne_Haken_kein_KI_Protokoll_mit_Haken_dabei()
        {
            File.WriteAllText(Path.Combine(_dbOrdner, KiProtokoll.Dateiname), "ki");

            var ohne = Supportpaket.Erstellen(Ziel, DIAGNOSE, false, _logs, _dbOrdner);
            Assert.DoesNotContain(KiProtokoll.Dateiname, Eintraege(Ziel));
            Assert.DoesNotContain(KiProtokoll.Dateiname, ohne.Fehlend);

            var mit = Supportpaket.Erstellen(Ziel, DIAGNOSE, true, _logs, _dbOrdner);
            Assert.Contains(KiProtokoll.Dateiname, Eintraege(Ziel));
            Assert.Contains(KiProtokoll.Dateiname, mit.Enthalten);
            Assert.Equal("ki", Inhalt(Ziel, KiProtokoll.Dateiname));
        }

        [Fact]
        public void Mit_Haken_ohne_Datei_steht_das_KI_Protokoll_in_Fehlend()
        {
            var erg = Supportpaket.Erstellen(Ziel, DIAGNOSE, true, _logs, _dbOrdner);
            Assert.True(erg.Geschrieben, erg.Fehler);
            Assert.Contains(KiProtokoll.Dateiname, erg.Fehlend);
        }

        [Fact]
        public void Nie_eine_Datenbank_im_Paket()
        {
            File.WriteAllText(Path.Combine(_dbOrdner, "Kenndaten.sqlite"), "keine");
            File.WriteAllText(Path.Combine(_logs, "Kopie.sqlite"), "keine");
            foreach (string d in new[] { Ausnahmeprotokoll.DATEI, Ausnahmeprotokoll.DATEI_ALT })
                File.WriteAllText(Path.Combine(_logs, d), "x");

            Supportpaket.Erstellen(Ziel, DIAGNOSE, true, _logs, _dbOrdner);

            Assert.DoesNotContain(Eintraege(Ziel), e => e.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Schreibfehler_am_Ziel_kommt_als_Fehler()
        {
            string ungueltig = Path.Combine(_wurzel, "gibt-es-nicht", "unter", "paket.zip");

            var erg = Supportpaket.Erstellen(ungueltig, DIAGNOSE, false, _logs, _dbOrdner);

            Assert.False(erg.Geschrieben);
            Assert.Equal("", erg.Pfad);
            Assert.False(string.IsNullOrWhiteSpace(erg.Fehler));
            Assert.False(File.Exists(ungueltig));
        }

        [Fact]
        public void Leeres_Ziel_kommt_als_Fehler()
        {
            var erg = Supportpaket.Erstellen("  ", DIAGNOSE, false, _logs, _dbOrdner);
            Assert.False(erg.Geschrieben);
            Assert.False(string.IsNullOrWhiteSpace(erg.Fehler));
        }

        [Fact]
        public void Vorgabename_traegt_Datum_und_Uhrzeit()
        {
            Assert.Equal("EPOS-Plan_Supportpaket_2026-10-11_1405.zip",
                         Supportpaket.Vorgabename(new DateTime(2026, 10, 11, 14, 5, 59)));
        }

        [Fact]
        public void Mit_Testdatenbank_steht_der_Katalogabgleich_mit_Spaltenkoepfen_drin()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string logs = Diagnosedaten.ProtokollordnerPfad();
            Directory.CreateDirectory(logs);
            File.WriteAllText(Path.Combine(logs, Ausnahmeprotokoll.DATEI), "ausnahme");

            var erg = Supportpaket.Erstellen(Ziel, DIAGNOSE, false);

            Assert.True(erg.Geschrieben, erg.Fehler);
            string[] eintraege = Eintraege(Ziel);
            Assert.Contains(Supportpaket.EINTRAG_KATALOGABGLEICH, eintraege);
            Assert.Contains(Ausnahmeprotokoll.DATEI, eintraege);
            string auszug = Inhalt(Ziel, Supportpaket.EINTRAG_KATALOGABGLEICH);
            Assert.StartsWith("Zeitpunkt\tFassung\tTabelle\tSchluessel\tAktion\tHinweis", auszug.TrimStart('\uFEFF'));
            Assert.DoesNotContain(eintraege, e => e.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase));
        }
    }
}
