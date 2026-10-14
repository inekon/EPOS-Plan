using System;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Diagnoseblock (Konzept Administration und Support 3.6, Handbuch 3.3): jede
    /// Bezeichnung im Block, nie eine Ausnahme, mit Testdatenbank der Schemastand.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class DiagnosedatenTests : IDisposable
    {
        private const string VERSION = "9.8.7-probe";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly string[] BEZEICHNUNGEN =
        {
            R.DIAG_PROGRAMMVERSION, R.DIAG_PLATTFORM, R.DIAG_SPRACHE, R.DIAG_DATENBANKPFAD,
            R.DIAG_SCHEMASTAND, R.DIAG_KATALOGFASSUNG, R.DIAG_LIZENZTYP, R.DIAG_LIZENZNUMMER,
            R.DIAG_FIRMA, R.DIAG_LIZENZSTATUS, R.DIAG_GUELTIG_BIS, R.DIAG_GERAETEKENNUNG,
            R.DIAG_PROTOKOLLORDNER, R.DIAG_ENTWICKLERMODUS,
        };

        [Fact]
        public void Ohne_Datenbank_wirft_nie_und_nennt_jede_Bezeichnung()
        {
            string vorher = DataRepository.PfadUeberschreibung;
            try
            {
                DataRepository.PfadUeberschreibung = Path.Combine(Path.GetTempPath(),
                    "epos-diagnose-" + Guid.NewGuid().ToString("N"), "Kenndaten.sqlite");

                string block = Diagnosedaten.Textblock(VERSION);
                foreach (string b in BEZEICHNUNGEN)
                    Assert.Contains(b + ": ", block);
                Assert.Contains(R.DIAG_PROGRAMMVERSION + ": " + VERSION, block);
                Assert.Contains(R.DIAG_SCHEMASTAND + ": " + R.DIAG_KEINE_DATENBANK, block);
                Assert.False(File.Exists(DataRepository.PfadUeberschreibung));

                var zeilen = Diagnosedaten.Zeilen(VERSION);
                Assert.Equal(BEZEICHNUNGEN, zeilen.Select(z => z.Bezeichnung).ToArray());
                Assert.All(zeilen, z => Assert.False(string.IsNullOrWhiteSpace(z.Wert)));
                Assert.Equal(zeilen.Count, block.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
            }
        }

        [Fact]
        public void Ohne_Version_heisst_der_Wert_nicht_ermittelbar()
        {
            var zeilen = Diagnosedaten.Zeilen(null);
            Assert.Equal(R.DIAG_NICHT_ERMITTELBAR, zeilen[0].Wert);
        }

        [Fact]
        public void Geraetekennung_ist_die_Kurzform()
        {
            var zeile = Diagnosedaten.Zeilen(VERSION).Single(z => z.Bezeichnung == R.DIAG_GERAETEKENNUNG);
            string abdruck = GeraeteId.Ermitteln();
            Assert.Equal(abdruck.Substring("SHA256:".Length, 8), zeile.Wert);
        }

        [Fact]
        public void Mit_Testdatenbank_zeigt_der_Block_den_Schemastand()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string erwartet = string.Format(R.DIAG_SCHEMA_FORMAT, SchemaStand.Zielversion, SchemaStand.Zielversion);
            string block = Diagnosedaten.Textblock(VERSION);
            Assert.Contains(R.DIAG_SCHEMASTAND + ": " + erwartet, block);
            Assert.Contains(R.DIAG_DATENBANKPFAD + ": " + DataRepository.GetDBPath(), block);
            Assert.Contains(R.DIAG_PROTOKOLLORDNER + ": "
                            + Path.Combine(Path.GetDirectoryName(DataRepository.GetDBPath()), "Logs"), block);
        }
    }
}
