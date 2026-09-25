using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Gebaeudevergleich.Tests
{
    /// <summary>
    /// T8: die Aufnahme — Quelle byte-gleich, keine Nebendatei, <c>integrity_check</c> der Kopie;
    /// Abbruch bei nichtleerer <c>-wal</c> und bei <c>-journal</c>; ein Pfad mit Leerzeichen und
    /// <c>#</c>; die Prozesssperre verweigert nur eine Produktivquelle.
    /// </summary>
    [Collection(Vergleichssammlung.NAME)]
    public sealed class AufnahmeTests
    {
        private readonly Vorrichtung _v;
        private readonly ITestOutputHelper _aus;

        public AufnahmeTests(Vorrichtung v, ITestOutputHelper aus)
        {
            _v = v;
            _aus = aus;
        }

        private static string[] Nebendateien(string quelle)
            => new[] { "-wal", "-shm", "-journal" }.Where(e => File.Exists(quelle + e))
                                                    .Select(e => e + ":" + new FileInfo(quelle + e).Length).ToArray();

        [Fact]
        public void T8_Aufnahme_laesst_die_Quelle_unberuehrt_und_prueft_die_Kopie()
        {
            if (!_v.Vorhanden) return;
            string quelle = _v.Walkopie;
            Assert.Contains(" ", quelle);
            Assert.Contains("#", quelle);

            string hashVorher = Werkzeuglauf.Pruefsumme(quelle);
            string[] nebenVorher = Nebendateien(quelle);
            DateTime zeitVorher = File.GetLastWriteTimeUtc(quelle);

            string ziel = _v.Ausgabe("t8_aufnahme");
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten("aufnahme", "--quelle", quelle, "--ziel", ziel);
            Assert.True(e.Code == 0, e.Alles);

            Assert.Equal(hashVorher, Werkzeuglauf.Pruefsumme(quelle));
            Assert.Equal(nebenVorher, Nebendateien(quelle));
            Assert.Equal(zeitVorher, File.GetLastWriteTimeUtc(quelle));

            string kopie = Path.Combine(ziel, "aufnahme.sqlite");
            Assert.True(File.Exists(kopie));
            string protokoll = File.ReadAllText(Path.Combine(ziel, "protokoll.txt"), Encoding.UTF8);
            Assert.Contains("integrity_check der Kopie: ok", protokoll);
            Assert.Contains("SHA-256 Quelle vorher:  " + hashVorher.ToLowerInvariant(), protokoll);
            Assert.Contains("SHA-256 Quelle nachher: " + hashVorher.ToLowerInvariant(), protokoll);
            Assert.Contains("Schemastand der Kopie: ", protokoll);
            _aus.WriteLine("T8: " + protokoll.Split('\n').FirstOrDefault(z => z.StartsWith("SHA-256 Quelle nachher", StringComparison.Ordinal)));
            // Ein zweiter Aufruf auf dasselbe Ziel überschreibt die Momentaufnahme nicht.
            Werkzeuglauf.Ergebnis zweiter = Werkzeuglauf.Starten("aufnahme", "--quelle", quelle, "--ziel", ziel);
            Assert.Equal(2, zweiter.Code);
            File.Delete(kopie);
        }

        [Fact]
        public void T8_Nichtleere_wal_und_journal_brechen_ab_leere_wal_und_shm_nicht()
        {
            if (!_v.Vorhanden) return;
            string quelle = _v.Walkopie;
            string hash = Werkzeuglauf.Pruefsumme(quelle);
            try
            {
                // Nichtleere -wal: immutable sähe ihren Inhalt nicht.
                File.WriteAllBytes(quelle + "-wal", new byte[] { 1, 2, 3, 4 });
                Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten("aufnahme", "--quelle", quelle, "--ziel", _v.Ausgabe("t8_wal"));
                Assert.Equal(2, e.Code);
                Assert.Contains("-wal", e.Fehlerausgabe);
                Assert.False(File.Exists(Path.Combine(_v.Ausgabe("t8_wal"), "aufnahme.sqlite")));
                File.Delete(quelle + "-wal");

                // -journal: eine unterbrochene Transaktion.
                File.WriteAllBytes(quelle + "-journal", Array.Empty<byte>());
                e = Werkzeuglauf.Starten("aufnahme", "--quelle", quelle, "--ziel", _v.Ausgabe("t8_journal"));
                Assert.Equal(2, e.Code);
                Assert.Contains("-journal", e.Fehlerausgabe);
                File.Delete(quelle + "-journal");

                // Leere -wal und -shm werden nur protokolliert.
                File.WriteAllBytes(quelle + "-wal", Array.Empty<byte>());
                File.WriteAllBytes(quelle + "-shm", new byte[32768]);
                string ziel = _v.Ausgabe("t8_leer");
                e = Werkzeuglauf.Starten("aufnahme", "--quelle", quelle, "--ziel", ziel);
                Assert.True(e.Code == 0, e.Alles);
                string protokoll = File.ReadAllText(Path.Combine(ziel, "protokoll.txt"), Encoding.UTF8);
                Assert.Contains("quelle.sqlite-wal 0 B", protokoll);
                Assert.Contains("quelle.sqlite-shm 32768 B", protokoll);
                Assert.Equal(0, new FileInfo(quelle + "-wal").Length);
                File.Delete(Path.Combine(ziel, "aufnahme.sqlite"));
            }
            finally
            {
                foreach (string d in new[] { quelle + "-wal", quelle + "-shm", quelle + "-journal" })
                    if (File.Exists(d)) File.Delete(d);
            }
            Assert.Equal(hash, Werkzeuglauf.Pruefsumme(quelle));
        }

        [Fact]
        public void T8_Prozesssperre_verweigert_nur_eine_Produktivquelle()
        {
            // Keine Datei wird angefasst: Die Sperre entscheidet am Pfad, bevor gelesen wird.
            string produktiv = Path.Combine(Schreibort.Produktivordner, "Kenndaten.sqlite");
            string temp = Path.Combine(Path.GetTempPath(), "irgendwo", "Kenndaten.sqlite");

            Assert.NotNull(Aufnahme.Prozesssperre(produktiv, () => true));
            Assert.Null(Aufnahme.Prozesssperre(produktiv, () => false));
            Assert.Null(Aufnahme.Prozesssperre(temp, () => true));
            Assert.Null(Aufnahme.Prozesssperre(temp, () => false));
        }

        [Fact]
        public void T8_Die_URI_maskiert_Leerzeichen_Raute_Prozent_und_Fragezeichen()
        {
            string uri = Sqlitehilfe.UnveraenderlichUri(Path.Combine(Path.GetTempPath(), "a b#c%d?e", "x.sqlite"));
            Assert.StartsWith("file:///", uri);
            Assert.EndsWith("?immutable=1", uri);
            Assert.Contains("a%20b%23c%25d%3Fe", uri);
            Assert.DoesNotContain("\\", uri);
        }
    }
}
