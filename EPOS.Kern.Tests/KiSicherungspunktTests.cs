using System;
using System.IO;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="KiSicherungspunkt"/> auf dem neuen Weg über
    /// <see cref="Datenbanksicherung.KopieAnlegen"/> (Auftrag #158).
    /// </summary>
    /// <remarks>
    /// <b>Sammlung „Testdatenbank" trotz eigener Quelle.</b> Jeder Fall biegt
    /// <see cref="DataRepository.PfadUeberschreibung"/> um - ein statisches, prozessweites
    /// Feld (Muster <see cref="TestDatenbank"/>) - deshalb dieselbe serielle Sammlung wie
    /// jeder andere Fall, der diesen Haken anfasst (Befund iU5-O-1).
    /// </remarks>
    [Collection("Testdatenbank")]
    public sealed class KiSicherungspunktTests : IDisposable
    {
        private readonly string _vorherPfad;
        private readonly string _ordner;

        public KiSicherungspunktTests()
        {
            _vorherPfad = DataRepository.PfadUeberschreibung;
            _ordner = Path.Combine(Path.GetTempPath(),
                                   "epos-kisicherungspunkt-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_ordner);
            KiSicherungspunkt.Zuruecksetzen();
        }

        public void Dispose()
        {
            KiSicherungspunkt.Zuruecksetzen();
            DataRepository.PfadUeberschreibung = _vorherPfad;
            try { SqliteConnection.ClearAllPools(); } catch { /* Aufraeumen darf nicht scheitern */ }
            try { Directory.Delete(_ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
        }

        private string NeueProjektdatenbank()
        {
            string pfad = Path.Combine(_ordner, "Kenndaten.sqlite");
            using (SqliteConnection verbindung = new SqliteConnection($"Data Source={pfad}"))
            {
                verbindung.Open();
                using (SqliteCommand cmd = verbindung.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE Probe (Wert TEXT);";
                    cmd.ExecuteNonQuery();
                }
            }
            return pfad;
        }

        /// <summary>
        /// Der Regelfall: Sicherstellen() legt über den neuen Helfer eine echte,
        /// eigenständig lesbare Kopie an, meldet keinen Fehler und lässt
        /// <see cref="KiSicherungspunkt.Hinweis"/> leer - die frühere ".laccdb"-Meldung
        /// (jetzt <c>KI_SICH_GEOEFFNET</c>, entfallen) kann nicht mehr auftreten, weil
        /// <c>VACUUM INTO</c> über eine geöffnete Verbindung immer den vollständigen,
        /// committeten Stand liefert.
        /// </summary>
        [Fact]
        public void Sicherstellen_legt_eine_lesbare_Kopie_an_und_meldet_keinen_Hinweis()
        {
            DataRepository.PfadUeberschreibung = NeueProjektdatenbank();

            string grund = KiSicherungspunkt.Sicherstellen(out string pfad);

            Assert.Null(grund);
            Assert.True(File.Exists(pfad), "Der gemeldete Sicherungspfad existiert nicht: " + pfad);
            Assert.Equal("", KiSicherungspunkt.Hinweis);

            using (SqliteConnection kopie = new SqliteConnection($"Data Source={pfad};Mode=ReadOnly"))
            {
                kopie.Open();
                using (SqliteCommand lese = kopie.CreateCommand())
                {
                    lese.CommandText = "SELECT COUNT(*) FROM Probe";
                    Assert.Equal(0L, Convert.ToInt64(lese.ExecuteScalar()));
                }
            }
        }

        /// <summary>
        /// Zweiter Aufruf derselben Sitzung auf dieselbe Quelle liefert DIESELBE Kopie
        /// zurück, statt eine zweite anzulegen (Fachkonzept 4.4, Punkt 1: „einmal je
        /// Sitzung, nicht je Aktion") - unverändert durch die Umstellung auf
        /// <see cref="Datenbanksicherung"/>.
        /// </summary>
        [Fact]
        public void Zweiter_Aufruf_derselben_Sitzung_legt_keine_zweite_Kopie_an()
        {
            DataRepository.PfadUeberschreibung = NeueProjektdatenbank();

            Assert.Null(KiSicherungspunkt.Sicherstellen(out string ersterPfad));
            Assert.Null(KiSicherungspunkt.Sicherstellen(out string zweiterPfad));

            Assert.Equal(ersterPfad, zweiterPfad);
        }

        /// <summary>
        /// <b>Fehlschlag sperrt die Sitzung.</b> Lässt sich die Kopie nicht anlegen, liefert
        /// <see cref="KiSicherungspunkt.Sicherstellen"/> einen Grund ungleich <c>null</c> -
        /// das Fachkonzept 4.4, Punkt 1, verlangt genau das: Schreibaktionen bleiben
        /// gesperrt, statt dass der Assistent ohne Rückweg ändert. Erzwungen wird der
        /// Fehlschlag plattformunabhängig: "DB-Backup" existiert schon als DATEI, der
        /// Helfer kann den Ordner deshalb nicht anlegen (siehe
        /// <see cref="DatenbanksicherungTests"/>).
        /// </summary>
        [Fact]
        public void Fehlschlag_bei_unbeschreibbarem_Zielordner_sperrt_die_Sitzung()
        {
            string quelle = NeueProjektdatenbank();
            File.WriteAllText(Path.Combine(_ordner, KiSicherungspunkt.ORDNER), "kein Ordner");
            DataRepository.PfadUeberschreibung = quelle;

            string grund = KiSicherungspunkt.Sicherstellen(out string pfad);

            Assert.NotNull(grund);
            Assert.NotEqual("", grund);
            Assert.Equal("", pfad);
            Assert.Equal("", KiSicherungspunkt.Pfad);
        }
    }
}
