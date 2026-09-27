using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Aktualitätswache der mitgelieferten Vorlagen</b> (Etappe BV-E9, Anwenderauftrag „Die Berichtsvorlage soll
    /// zukünftig mit ausgeliefert werden und an den aktuellen Stand angepasst werden — sowohl Word als auch Excel“): Jede Datei
    /// des Sammelbefehls <c>alle</c> von <c>Werkzeuge/Berichtsvorlage</c> (<c>Sammellauf.Schritte</c>) wird frisch aus Katalog und
    /// Stilvorlage erzeugt und mit der eingecheckten verglichen — über <see cref="BerichtsvorlagenCtrl.Inhaltsschluessel"/>, also
    /// ohne Zeitstempel, Kerneigenschaften und Zufallskennungen. Weicht eine ab, ist sie veraltet: Der Fall nennt die Dateien und
    /// den Werkzeugbefehl, der sie erneuert. Dazu hält er, dass der Sammelbefehl jede ausgelieferte Vorlage erzeugt.
    /// </summary>
    public class AuslieferungsvorlagenAktualitaetWacheTests : IDisposable
    {
        /// <summary>Der Befehl, der alle mitgelieferten Vorlagen erneuert.</summary>
        internal const string BEFEHL = "dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- alle WindowsFormsApplication1/Allgemein/Bericht/Vorlagen";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;
        private readonly string _ordner = Probevorlagen.TempOrdner("epos-bv-e9-aktuell-");

        public AuslieferungsvorlagenAktualitaetWacheTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_ordner);
        }

        private static string Vorlagenordner()
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            return wurzel == null ? null
                : Path.Combine(wurzel, BerichtsvorlageDateiWacheTests.ORDNER_REPO.Replace('/', Path.DirectorySeparatorChar));
        }

        [Fact]
        public void Jede_mitgelieferte_Vorlage_ist_aus_dem_aktuellen_Katalog_erzeugt()
        {
            string ordner = Vorlagenordner();
            if (ordner == null) return;
            string stil = Path.Combine(ordner, global::Berichtsvorlage.Sammellauf.STILVORLAGE);
            Assert.True(File.Exists(stil), "Stilvorlage fehlt: " + stil);

            var veraltet = new List<string>();
            foreach (global::Berichtsvorlage.Sammelschritt s in global::Berichtsvorlage.Sammellauf.Schritte())
            {
                string frisch = Path.Combine(_ordner, s.Datei);
                var protokoll = new StringWriter();
                int rc = s.Lauf(stil, frisch, protokoll);
                Assert.True(rc == 0, s.Datei + ": Werkzeug rot (" + rc + ")\n" + protokoll);
                string eingecheckt = Path.Combine(ordner, s.Datei);
                if (!File.Exists(eingecheckt))
                {
                    veraltet.Add(s.Datei + " (fehlt)");
                    continue;
                }
                string a = BerichtsvorlagenCtrl.Inhaltsschluessel(File.ReadAllBytes(frisch));
                string b = BerichtsvorlagenCtrl.Inhaltsschluessel(File.ReadAllBytes(eingecheckt));
                _ausgabe.WriteLine(s.Datei + ": " + (a == b ? "aktuell" : "VERALTET"));
                if (a != b) veraltet.Add(s.Datei);
            }
            Assert.True(veraltet.Count == 0,
                "Veraltete mitgelieferte Vorlagen: " + string.Join(", ", veraltet)
                + ". Katalog oder Werkzeug sind weiter als die eingecheckten Dateien — neu erzeugen mit\n  " + BEFEHL
                + "\nund die Dateien committen.");
        }

        [Fact]
        public void Der_Sammelbefehl_erzeugt_jede_ausgelieferte_Vorlage()
        {
            var schritte = new HashSet<string>(global::Berichtsvorlage.Sammellauf.Schritte().Select(s => s.Datei), StringComparer.Ordinal);
            List<string> fehlt = AuslieferungsvorlagenWacheTests.Ausgeliefert
                .Concat(AuslieferungsvorlagenWacheTests.NichtAusgeliefert)
                .Where(d => d != global::Berichtsvorlage.Sammellauf.STILVORLAGE && !schritte.Contains(d))
                .ToList();
            Assert.True(fehlt.Count == 0, "Diese Vorlagen erzeugt der Sammelbefehl nicht: " + string.Join(", ", fehlt));
        }
    }
}
