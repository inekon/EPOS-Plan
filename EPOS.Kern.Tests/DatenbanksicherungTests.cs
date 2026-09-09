using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="Datenbanksicherung"/> — der Nachweis von Auftrag #158 ("eine
    /// Sicherungswahrheit im Kern").
    /// </summary>
    /// <remarks>
    /// <para><b>Ohne die Testdatenbank.</b> <see cref="Datenbanksicherung.KopieAnlegen"/>
    /// ist ein reiner Pfad-zu-Pfad-Vorgang und kennt <c>DataRepository.GetDBPath()</c>
    /// nicht - jeder Fall hier baut sich seine eigene, winzige SQLite-Quelle in einem
    /// eigenen Temp-Ordner. Keine <c>[Collection("Testdatenbank")]</c> noetig:
    /// <c>Schreibnaht.Freigabe</c> haengt an einem <c>AsyncLocal</c> (je Aufruf, nicht
    /// prozessweit) und <c>Datenbanksicherung</c> ruehrt <c>DataRepository.PfadUeberschreibung</c>
    /// nie an - anders als <see cref="KiSicherungspunktTests"/>, die genau das braucht.</para>
    /// </remarks>
    public sealed class DatenbanksicherungTests : IDisposable
    {
        private readonly string _ordner;

        public DatenbanksicherungTests()
        {
            _ordner = Path.Combine(Path.GetTempPath(),
                                   "epos-datenbanksicherung-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_ordner);
        }

        public void Dispose()
        {
            // Derselbe Grund wie in TestDatenbank.Dispose: der Verbindungspool haelt Dateien
            // offen, bis er geleert wird.
            try { SqliteConnection.ClearAllPools(); } catch { /* Aufraeumen darf nicht scheitern */ }
            try { Directory.Delete(_ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
        }

        /// <summary>Baut eine winzige WAL-Datenbank mit einer leeren Tabelle "Probe".</summary>
        private string NeueQuelle(string dateiname = "Quelle.sqlite")
        {
            string pfad = Path.Combine(_ordner, dateiname);
            using (SqliteConnection verbindung = new SqliteConnection($"Data Source={pfad}"))
            {
                verbindung.Open();
                using (SqliteCommand cmd = verbindung.CreateCommand())
                {
                    // journal_mode=WAL ist dateipersistent (wie im Betrieb, BETRIEB_SQLITE.md
                    // § 2) - jede spaetere Verbindung auf dieselbe Datei erbt ihn.
                    cmd.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE Probe (Wert TEXT);";
                    cmd.ExecuteNonQuery();
                }
            }
            return pfad;
        }

        // =====================================================================
        //  1) Kopie enthaelt Aenderungen, die nur in der -wal standen
        // =====================================================================

        /// <summary>
        /// Schreiben OHNE Checkpoint (die schreibende Verbindung bleibt waehrend der
        /// gesamten Probe offen - kein "letzte Verbindung schliesst" -Checkpoint kann
        /// dazwischenfunken), sichern, Kopie GETRENNT oeffnen, Satz vorhanden. Eine reine
        /// <c>File.Copy</c> der Hauptdatei haette genau diesen Satz verloren (Befund #155).
        /// </summary>
        [Fact]
        public void Kopie_enthaelt_Aenderungen_die_zum_Sicherungszeitpunkt_nur_in_der_Wal_standen()
        {
            string quelle = NeueQuelle();

            using (SqliteConnection schreiber = new SqliteConnection($"Data Source={quelle}"))
            {
                schreiber.Open();
                using (SqliteCommand cmd = schreiber.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Probe (Wert) VALUES ('nur-in-wal');";
                    cmd.ExecuteNonQuery();
                }

                Assert.True(File.Exists(quelle + "-wal"),
                    "Erwartet eine bestehende '-wal' als Beleg fuer den ungecheckpointeten Schreibvorgang.");

                string ziel = Datenbanksicherung.KopieAnlegen(quelle, Path.Combine(_ordner, "Sicherung1"), "Probe");

                using (SqliteConnection kopie = new SqliteConnection($"Data Source={ziel};Mode=ReadOnly"))
                {
                    kopie.Open();
                    using (SqliteCommand lese = kopie.CreateCommand())
                    {
                        lese.CommandText = "SELECT Wert FROM Probe";
                        Assert.Equal("nur-in-wal", Convert.ToString(lese.ExecuteScalar()));
                    }
                }
            }
        }

        // =====================================================================
        //  2) Kopie hat keine Begleitdateien
        // =====================================================================

        [Fact]
        public void Kopie_hat_keine_Wal_und_keine_Shm_Begleitdateien()
        {
            string quelle = NeueQuelle();
            using (SqliteConnection schreiber = new SqliteConnection($"Data Source={quelle}"))
            {
                schreiber.Open();
                using (SqliteCommand cmd = schreiber.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Probe (Wert) VALUES ('x');";
                    cmd.ExecuteNonQuery();
                }

                string ziel = Datenbanksicherung.KopieAnlegen(quelle, Path.Combine(_ordner, "Sicherung2"), "Probe");

                Assert.True(File.Exists(ziel));
                Assert.False(File.Exists(ziel + "-wal"), "Die Kopie darf keine eigene -wal fuehren.");
                Assert.False(File.Exists(ziel + "-shm"), "Die Kopie darf keine eigene -shm fuehren.");
            }
        }

        // =====================================================================
        //  3) Fehlschlaege werfen - klar und nicht still
        // =====================================================================

        [Fact]
        public void Fehlende_Quelle_wirft_FileNotFoundException()
        {
            string fehlt = Path.Combine(_ordner, "gibtsnicht.sqlite");

            var ex = Assert.Throws<FileNotFoundException>(() =>
                Datenbanksicherung.KopieAnlegen(fehlt, Path.Combine(_ordner, "Ziel"), "Probe"));
            Assert.Contains(fehlt, ex.FileName);
        }

        /// <summary>
        /// Ein unbeschreibbares Ziel wirft (Regel: „als Rueckgabe/Exception mit klarer
        /// Meldung, nicht still"). Erzwungen wird das PLATTFORMUNABHAENGIG und ohne
        /// Zugriffsrechte-Tricks: Der Zielordner-Pfad existiert schon als DATEI, und ein
        /// Ordner kann dort nie entstehen - weder als root noch als Normalnutzer.
        /// </summary>
        [Fact]
        public void Fehlschlag_bei_unbeschreibbarem_Ziel_wirft_und_hinterlaesst_keine_Datei()
        {
            string quelle = NeueQuelle();
            string blockiertesZiel = Path.Combine(_ordner, "Ziel-ist-eine-Datei");
            File.WriteAllText(blockiertesZiel, "hier kann kein Ordner entstehen");

            Assert.ThrowsAny<IOException>(() => Datenbanksicherung.KopieAnlegen(quelle, blockiertesZiel, "Probe"));

            // Die blockierende Datei selbst bleibt unangetastet - kein Nebenschaden.
            Assert.Equal("hier kann kein Ordner entstehen", File.ReadAllText(blockiertesZiel));
        }

        [Fact]
        public void Leerer_Quellpfad_wirft_ArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                Datenbanksicherung.KopieAnlegen("", Path.Combine(_ordner, "Ziel"), "Probe"));
        }

        // =====================================================================
        //  4) Waechter: beide Aufrufer nutzen den Helfer - kein File.Copy mehr
        // =====================================================================

        /// <summary>
        /// Entspricht <c>grep -c "File.Copy" KiSicherungspunkt.cs MenueCtrl.cs</c> = 0 aus
        /// dem Auftrag - als xunit-Fall, damit der CI-Lauf ihn automatisch mitprueft statt
        /// dass ihn jemand von Hand aufruft.
        /// </summary>
        [Fact]
        public void KiSicherungspunkt_und_MenueCtrl_kopieren_die_Datenbank_nicht_mehr_selbst()
        {
            string kiDatei = QuellDatei("EPOS.Kern", "Allgemein", "KI", "KiSicherungspunkt.cs");
            string menueDatei = QuellDatei("WindowsFormsApplication1", "Controller", "MenueCtrl.cs");

            Assert.DoesNotContain("File.Copy", File.ReadAllText(kiDatei));
            Assert.DoesNotContain("File.Copy", File.ReadAllText(menueDatei));

            // Gegenprobe: beide rufen tatsaechlich den gemeinsamen Helfer.
            Assert.Contains("Datenbanksicherung.KopieAnlegen", File.ReadAllText(kiDatei));
            Assert.Contains("Datenbanksicherung.KopieAnlegen", File.ReadAllText(menueDatei));
        }

        private static string QuellDatei(params string[] teile)
        {
            string pfad = Arbeitsbaum();
            foreach (string teil in teile) pfad = Path.Combine(pfad, teil);
            Assert.True(File.Exists(pfad), "Datei nicht gefunden: " + pfad);
            return pfad;
        }

        /// <summary>Muster wie in <c>EinheitenWacheTests.Arbeitsbaum</c>.</summary>
        private static string Arbeitsbaum([CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string wurzel = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (wurzel != null && File.Exists(Path.Combine(wurzel, "WP-Plan.sln"))) return wurzel;
            }

            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }

            Assert.Fail("Die Wurzel des Arbeitsbaums (WP-Plan.sln) ist nicht zu finden.");
            return null;
        }
    }
}
