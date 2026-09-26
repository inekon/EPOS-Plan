using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die <b>Katalogprobe</b> des iOS-Prüfmodus (Anwenderentscheid ZU26, N23): derselbe Weg, den
    /// <c>EPOS.iOS/Pruefung/Katalogprobe.cs</c> im Simulator fährt — das Probepaket
    /// <c>Proben/Zapfprofil/Katalogpaket/</c> als ZIP-Archiv, erst Prüflauf, dann Import. Geprüft
    /// wird die Zeilenform, die <c>ios.yml</c> auswertet: sprachfrei (Kennungen, Zahlen invariant),
    /// also ohne Kulturpinnung.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwKatalogprobeTests : IDisposable
    {
        private readonly string _ordner = Path.Combine(Path.GetTempPath(),
            "epos-katalogprobe-" + Guid.NewGuid().ToString("N").Substring(0, 8));

        public TwwKatalogprobeTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            try { Directory.Delete(_ordner, true); } catch { /* Temp-Rest ist kein Befund */ }
        }

        /// <summary>Das Probepaket als ZIP-Archiv — wie die iOS-Schale es packt.</summary>
        private string Archiv()
        {
            string quelle = Path.Combine(ZapfZufallTests.Probenordner(), "Katalogpaket");
            string stufe = Path.Combine(_ordner, "paket");
            Directory.CreateDirectory(stufe);
            foreach (string d in Directory.GetFiles(quelle, "*.csv"))
                File.Copy(d, Path.Combine(stufe, Path.GetFileName(d)));
            string zip = Path.Combine(_ordner, "Katalogpaket.zip");
            ZipFile.CreateFromDirectory(stufe, zip);
            return zip;
        }

        private static List<string> Fahren(string pfad, out int befunde)
        {
            var zeilen = new List<string>();
            befunde = TwwKatalogprobe.Probelauf(pfad, zeilen.Add);
            return zeilen;
        }

        /// <summary>
        /// Auf einem leeren Katalog: das ZIP-Paket wird gelesen, Prüflauf und Import zählen gleich,
        /// nichts ist abgelehnt, und der Import hat wirklich geschrieben (Zuwachs zwei).
        /// </summary>
        [Fact]
        public void Das_ZIP_Probepaket_laeuft_ohne_Befund_durch_und_der_Import_schreibt()
        {
            using var db = new TwwTestdatenbank();
            List<string> z = Fahren(Archiv(), out int befunde);

            Assert.True(befunde == 0, string.Join("\n", z));
            Assert.Contains(z, s => s.StartsWith("KATALOGPROBE paket ergebnis=GELESEN dateien=7 ", StringComparison.Ordinal));
            Assert.Contains(z, s => s.StartsWith("KATALOGPROBE lauf=pruefung ergebnis=OK nutzungsarten=2 ", StringComparison.Ordinal));
            Assert.Contains(z, s => s.StartsWith("KATALOGPROBE lauf=import ergebnis=OK nutzungsarten=2 ", StringComparison.Ordinal));
            Assert.Contains("KATALOGPROBE vergleich ergebnis=GLEICH", z);
            Assert.Contains(z, s => s.StartsWith("KATALOGPROBE katalog ", StringComparison.Ordinal) && s.EndsWith(" zuwachs=2", StringComparison.Ordinal));
            Assert.DoesNotContain(z, s => s.Contains("abgelehnt bereich=", StringComparison.Ordinal));
            Assert.Equal("KATALOGPROBE ende ergebnis=OK befunde=0", z.Last());
        }

        /// <summary>
        /// Auf der Testdatenbank — der Seed, den die iOS-Schale mitbringt: derselbe Lauf ohne Befund.
        /// Fehlt die Datenbank (LFS-Zeiger), schweigt der Fall wie alle Datenbankfälle.
        /// </summary>
        [Fact]
        public void Auf_der_Testdatenbank_laeuft_die_Probe_ohne_Befund()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<string> z = Fahren(Archiv(), out int befunde);

            Assert.True(befunde == 0, string.Join("\n", z));
            Assert.Contains("KATALOGPROBE vergleich ergebnis=GLEICH", z);
            Assert.Equal("KATALOGPROBE ende ergebnis=OK befunde=0", z.Last());
        }

        // =================================================================================
        //  Der Weg ohne Ordnerwahl (ZU26): Die Hülle reicht die Fähigkeit der Plattform durch
        // =================================================================================

        /// <summary>Ein Dateidienst, der nur die Fähigkeit der Ordnerwahl ausweist.</summary>
        private sealed class Ordnerprobe : IDateiDienst
        {
            internal bool Kann { get; init; }
            public bool OrdnerwahlMoeglich => Kann;
            public string DateiOeffnen(string titel, string filter, string startOrdner) => "";
            public string DateiSpeichern(string titel, string filter, string vorschlag) => "";
            public string OrdnerWaehlen(string titel, string startOrdner) => "";
            public bool MitSystemOeffnen(string pfad) => false;
        }

        /// <summary>
        /// <c>KatalogGaben()</c> trägt <c>OrdnerwahlVerfuegbar</c> aus
        /// <c>IDateiDienst.OrdnerwahlMoeglich</c>: Windows ja, iOS nein. Die Standardumsetzung der
        /// Schnittstelle sagt ja — jede vorhandene Fassung bleibt beim Dialog von heute.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Die_Kataloggabe_traegt_die_Ordnerwahl_der_Plattform(bool kann)
        {
            IDateiDienst vorher = Dienste.Datei;
            try
            {
                Dienste.Datei = new Ordnerprobe { Kann = kann };
                IReadOnlyDictionary<string, object> gaben = ZapfprofilHuelle.KatalogGaben();
                Assert.Equal(kann, Assert.IsType<bool>(gaben["OrdnerwahlVerfuegbar"]));
            }
            finally
            {
                Dienste.Datei = vorher;
            }
        }

        /// <summary>Die Standardumsetzung der Schnittstelle führt die Ordnerwahl.</summary>
        [Fact]
        public void Die_Standardumsetzung_fuehrt_die_Ordnerwahl()
            => Assert.True(((IDateiDienst)new KeineDateiwahl()).OrdnerwahlMoeglich);

        /// <summary>Ein Paket, das es nicht gibt, ist ein benannter Befund und keine Ausnahme.</summary>
        [Fact]
        public void Ein_fehlendes_Paket_ist_ein_benannter_Befund()
        {
            using var db = new TwwTestdatenbank();
            List<string> z = Fahren(Path.Combine(_ordner, "gibt_es_nicht.zip"), out int befunde);

            Assert.Equal(1, befunde);
            Assert.Contains(z, s => s.StartsWith("KATALOGPROBE paket ergebnis=ABGELEHNT grund=", StringComparison.Ordinal)
                                    || s.StartsWith("KATALOGPROBE abgebrochen ", StringComparison.Ordinal));
            Assert.Equal("KATALOGPROBE ende ergebnis=BEFUNDE befunde=1", z.Last());
        }
    }
}
