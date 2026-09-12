using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using WindowsFormsApplication1;
using Xunit;

namespace Auslieferungsvorlage.Tests
{
    /// <summary>
    /// Die Testsammlung. <see cref="DataRepository.PfadUeberschreibung"/> ist ein
    /// STATISCHES Feld — es gibt genau eines fuer den ganzen Testlauf. Zwei Klassen, die
    /// nebeneinander ihre eigene Datenbank einlegen, ueberschrieben sich gegenseitig den
    /// Pfad. Dieselbe Sammlung heisst: eine nach der anderen. (Wortgleich zur Begruendung
    /// in <c>EPOS.Kern.Tests.TestdatenbankSammlung</c>.)
    /// </summary>
    [CollectionDefinition("Auslieferungsvorlage")]
    public sealed class Vorlagensammlung { }

    /// <summary>
    /// EIN vollstaendiger Lauf des Werkzeugs, an dem sich alle Inhaltsproben bedienen.
    ///
    /// <para><b>Warum eine Klassenvorrichtung.</b> Ein Lauf zieht eine 67-MB-Arbeitskopie,
    /// loescht 1,17 Millionen Zeilen, spielt ein Projektpaket ein und verdichtet. Das je
    /// Fall zu wiederholen kostet Minuten ohne Erkenntnisgewinn — die Proben stellen
    /// verschiedene Fragen an DASSELBE Ergebnis. Die Faelle, die einen ANDEREN Lauf
    /// brauchen (Trockenlauf, Katalogwaechter, Schreibort, Idempotenz), starten ihren
    /// eigenen.</para>
    ///
    /// <para><b>Modus <c>--kataloge alle</c>.</b> Die Vorrichtung baut die Vorlage so, wie
    /// sie heute auslieferbar waere. Die Regel „nur <c>ReadOnly = TRUE</c>" aus
    /// Setup-Konzept 6.1 leert am Bestand der Testdatenbank 22 der 28 Katalogtabellen
    /// (419 722 Zeilen auf 101) — sie wird deshalb in einer EIGENEN Probe geprueft
    /// (<see cref="KatalogregelTests"/>), nicht hier.</para>
    /// </summary>
    public sealed class Vorlage : IDisposable
    {
        /// <summary>Das Projekt der Referenzlaeufe (Id 1030) — der Beispielinhalt dieser Proben.</summary>
        internal const string BEISPIELPROJEKT = "Referenz BHKW-Kaskade (Regressionstest)";

        private readonly Arbeitsordner _ordner = new Arbeitsordner();

        public Vorlage()
        {
            Vorhanden = Werkzeuglauf.Testdatenbank != null;
            if (!Vorhanden) return;

            // Die Quelle ist eine KOPIE der Testdatenbank. Die Datei im Repository wird nie
            // geoeffnet: Sie ist die Quelle jedes Referenzlaufs, und schon das Oeffnen im
            // WAL-Modus legt Beidateien daneben.
            Quelle = _ordner.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, Quelle);

            Beispielpaket = BeispielExportieren();

            QuelleVorher = Pruefsumme(Quelle);
            Ziel = _ordner.Datei("Kenndaten.sqlite");
            Lauf = Werkzeuglauf.Starten(Quelle, Ziel, "--beispiele", Beispielpaket, "--kataloge", "alle");
            QuelleNachher = Pruefsumme(Quelle);
        }

        internal bool Vorhanden { get; }
        internal string Quelle { get; }
        internal string Ziel { get; }
        internal string Beispielpaket { get; }
        internal string QuelleVorher { get; }
        internal string QuelleNachher { get; }
        internal Werkzeuglauf.Ergebnis Lauf { get; }

        /// <summary>
        /// Erzeugt das Projektpaket, das eingespielt wird. Beispielpakete gibt es im
        /// Repository heute noch nicht (das Beispielgeruest fuehrt <c>projekt.epx</c> als
        /// vorgesehene, aber noch nicht gebaute Datei); der Einspielweg wird deshalb mit
        /// einem Paket belegt, das aus der Testdatenbank exportiert wird — genau so, wie
        /// das erste echte Beispiel spaeter entsteht.
        /// </summary>
        private string BeispielExportieren()
        {
            string werkbank = _ordner.Datei("werkbank.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, werkbank);

            string paket = _ordner.Datei("beispiel.wpx");
            string vorher = DataRepository.PfadUeberschreibung;
            Func<bool> schreibrecht = Schreibnaht.Schreibrecht;
            try
            {
                DataRepository.PfadUeberschreibung = werkbank;
                Schreibnaht.WerkzeugFreigabe("Auslieferungsvorlage.Tests (Beispielpaket erzeugen)");
                Assert.True(new ProjektExportImportCtrl().Exportieren(BEISPIELPROJEKT, paket),
                            "Der Export des Beispielprojekts ist fehlgeschlagen.");
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                Schreibnaht.Schreibrecht = schreibrecht;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
            return paket;
        }

        /// <summary>Fuehrt eine Abfrage gegen die ERZEUGTE Vorlage aus.</summary>
        internal T Lesen<T>(Func<T> abfrage)
        {
            string vorher = DataRepository.PfadUeberschreibung;
            try
            {
                DataRepository.PfadUeberschreibung = Ziel;
                return abfrage();
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
        }

        internal static string Pruefsumme(string datei)
        {
            using var s = File.OpenRead(datei);
            return Convert.ToHexString(SHA256.HashData(s));
        }

        public void Dispose() => _ordner.Dispose();
    }
}
