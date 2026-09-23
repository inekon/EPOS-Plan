using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Aufräumproben der Vorrichtung <see cref="TestDatenbank"/> (Befund 23.09.2026:
    /// 1 170 liegen gebliebene Arbeitskopien, 77 GB, Platte voll).
    ///
    /// <para>Geprüft wird viererlei: Eine entsorgte Kopie ist fort und der Prozesszustand
    /// zurückgestellt — auch hinter einer Verbindung, die niemand geschlossen hat. Ein
    /// abgebrochener Aufbau hinterlässt weder Ordner noch Zustand. Die Besitzmarke ist
    /// belegt, solange die Vorrichtung lebt. Und der Aufräumlauf nimmt genau die verwaisten
    /// Kopien mit — nie eine, deren Besitzer noch lebt, nie eine mit gesperrter Datei, nie
    /// einen fremden Ordner.</para>
    ///
    /// <para>Der Aufräumlauf wird hier nur in einem eigenen Probenordner gerufen, nie mit
    /// verschobener Uhr über <c>%TEMP%</c>: Dort liegen womöglich die Kopien eines Laufs in
    /// einer anderen Sitzung.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class TestDatenbankAufraeumenTests
    {
        private const string DATENBANK = "Kenndaten.sqlite";

        // =====================================================================
        //  Entsorgen
        // =====================================================================

        [Fact]
        public void Dispose_loescht_die_Kopie_und_stellt_den_Pfad_zurueck()
        {
            string vorher = DataRepository.PfadUeberschreibung;
            string ordner;
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;
                ordner = db.Ordner;

                Assert.Equal(Path.Combine(ordner, DATENBANK), DataRepository.PfadUeberschreibung);
                Assert.True(File.Exists(Path.Combine(ordner, TestDatenbank.BESITZMARKE)));
                // Ein Zugriff über den Kern, damit der Pool die Kopie wirklich hält.
                Assert.True(DataRepository.TabelleVorhanden("Tab_Projekt"));
            }

            Assert.False(Directory.Exists(ordner), "Die Arbeitskopie blieb liegen: " + ordner);
            Assert.Equal(vorher, DataRepository.PfadUeberschreibung);
        }

        /// <summary>
        /// Eine ungepoolte Verbindung, die niemand schließt, hält die Kopie unter Windows fest,
        /// bis der Finalisierer ihren Griff freigibt — erst ein späterer Löschversuch kommt
        /// durch (<see cref="TestDatenbank.OrdnerLoeschen"/>). Unter Linux und macOS darf eine
        /// offene Datei gelöscht werden; dort geht schon der erste Versuch durch.
        /// </summary>
        [Fact]
        public void Dispose_loescht_die_Kopie_auch_hinter_einer_nie_geschlossenen_Verbindung()
        {
            string ordner;
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;
                ordner = db.Ordner;
                VerbindungOeffnenUndLiegenLassen(Path.Combine(ordner, DATENBANK));
            }

            Assert.False(Directory.Exists(ordner), "Die Arbeitskopie blieb hinter der offenen Verbindung liegen: " + ordner);
        }

        [Fact]
        public void Ein_abgebrochener_Aufbau_hinterlaesst_weder_Ordner_noch_Zustand()
        {
            string wurzel = Probenordner();
            string pfadVorher = DataRepository.PfadUeberschreibung;
            Func<bool> schreibrechtVorher = Schreibnaht.Schreibrecht;
            try
            {
                // Die Quelle fehlt: Der Ordner und die Besitzmarke stehen schon, File.Copy wirft.
                string fehlt = Path.Combine(wurzel, "fehlt.sqlite");
                Assert.Throws<FileNotFoundException>(() => { using var db = new TestDatenbank(fehlt, wurzel); });

                Assert.Empty(Directory.GetFileSystemEntries(wurzel));
                Assert.Equal(pfadVorher, DataRepository.PfadUeberschreibung);
                Assert.Same(schreibrechtVorher, Schreibnaht.Schreibrecht);
            }
            finally
            {
                Directory.Delete(wurzel, true);
            }
        }

        [Fact]
        public void Die_Besitzmarke_ist_belegt_solange_die_Vorrichtung_lebt()
        {
            string marke;
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;
                marke = Path.Combine(db.Ordner, TestDatenbank.BESITZMARKE);
                Assert.Throws<IOException>(() => { using (Belegen(marke)) { } });
            }
            Assert.False(File.Exists(marke));
        }

        // =====================================================================
        //  Der Aufräumlauf
        // =====================================================================

        [Fact]
        public void Der_Aufraeumlauf_nimmt_genau_die_verwaisten_Kopien_mit()
        {
            string wurzel = Probenordner();
            try
            {
                string markeFrei = Kopie(wurzel, "0000000a", mitMarke: true);
                string markeBelegt = Kopie(wurzel, "0000000b", mitMarke: true);
                string ohneMarke = Kopie(wurzel, "0000000c", mitMarke: false);
                string ohneMarkeGesperrt = Kopie(wurzel, "0000000d", mitMarke: false);
                string fremd = Kopie(wurzel, "probe", mitMarke: false);
                TimeSpan frist = TestDatenbank.SCHONFRIST_OHNE_MARKE;
                DateTime jetzt = DateTime.UtcNow;

                using (Belegen(Path.Combine(markeBelegt, TestDatenbank.BESITZMARKE)))
                using (Belegen(Path.Combine(ohneMarkeGesperrt, DATENBANK)))
                {
                    // Jetzt: Allein die Kopie mit FREIER Marke ist verwaist - ihr Besitzer ist fort.
                    // Die Kopien ohne Marke sind zu jung, um sie anzufassen.
                    Assert.Equal(1, TestDatenbank.VerwaisteKopienAufraeumen(wurzel, jetzt, frist));
                    Assert.False(Directory.Exists(markeFrei));
                    Assert.True(Directory.Exists(markeBelegt));
                    Assert.True(Directory.Exists(ohneMarke));
                    Assert.True(Directory.Exists(ohneMarkeGesperrt));

                    // Nach Ablauf der Schonfrist geht auch die Kopie ohne Marke - nicht aber die
                    // mit gesperrter Datei, die mit belegter Marke und der fremde Ordner.
                    Assert.Equal(1, TestDatenbank.VerwaisteKopienAufraeumen(wurzel, jetzt + frist + TimeSpan.FromMinutes(1), frist));
                    Assert.False(Directory.Exists(ohneMarke));
                    Assert.True(Directory.Exists(markeBelegt));
                    Assert.True(Directory.Exists(ohneMarkeGesperrt));
                    Assert.True(Directory.Exists(fremd));
                }

                // Sind die Sperren fort, bleibt allein der fremde Ordner.
                Assert.Equal(2, TestDatenbank.VerwaisteKopienAufraeumen(wurzel, jetzt + frist + TimeSpan.FromMinutes(1), frist));
                Assert.Equal(new[] { fremd }, Directory.GetDirectories(wurzel));
            }
            finally
            {
                Directory.Delete(wurzel, true);
            }
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>
        /// Öffnet eine UNGEPOOLTE Verbindung und lässt sie offen liegen — bewusst ohne Dispose.
        /// Eigene Methode, damit nach der Rückkehr nichts mehr auf die Verbindung zeigt.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void VerbindungOeffnenUndLiegenLassen(string pfad)
        {
            var verbindung = new SqliteConnection("Data Source=" + pfad + ";Pooling=False");
            verbindung.Open();
            using SqliteCommand befehl = verbindung.CreateCommand();
            befehl.CommandText = "SELECT COUNT(*) FROM sqlite_master";
            Assert.True(Convert.ToInt64(befehl.ExecuteScalar()) > 0);
        }

        /// <summary>Hält eine Datei exklusiv offen — so, wie die Vorrichtung ihre Besitzmarke hält.</summary>
        private static FileStream Belegen(string datei)
        {
            return new FileStream(datei, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }

        /// <summary>Ein Kopieordner zum Anfassen: eine kleine Datenbankdatei, auf Wunsch mit (freier) Marke.</summary>
        private static string Kopie(string wurzel, string kennung, bool mitMarke)
        {
            string ordner = Path.Combine(wurzel, TestDatenbank.ORDNER_PRAEFIX + kennung);
            Directory.CreateDirectory(ordner);
            File.WriteAllBytes(Path.Combine(ordner, DATENBANK), new byte[16]);
            if (mitMarke) File.WriteAllBytes(Path.Combine(ordner, TestDatenbank.BESITZMARKE), Array.Empty<byte>());
            return ordner;
        }

        /// <summary>Ein eigener, leerer Probenordner unter <c>%TEMP%</c> — bewusst OHNE das Präfix der Kopien.</summary>
        private static string Probenordner()
        {
            string ordner = Path.Combine(Path.GetTempPath(), "epos-aufraeumprobe-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            return ordner;
        }
    }
}
