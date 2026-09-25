using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Gebaeudevergleich.Tests
{
    /// <summary>
    /// Die EINE Sammlung: <c>DataRepository.PfadUeberschreibung</c> und <c>Schreibnaht.Schreibrecht</c>
    /// sind prozessweit, und die Läufe teilen sich die Kopien — alles nacheinander.
    /// </summary>
    [CollectionDefinition(NAME)]
    public sealed class Vergleichssammlung : ICollectionFixture<Vorrichtung>
    {
        internal const string NAME = "Gebaeudevergleich";
    }

    /// <summary>
    /// Ein Wegwerfordner unter <c>%TEMP%</c> mit der Besitzmarke <c>besitz.sperre</c>, die er vom
    /// Anlegen bis zum Löschen exklusiv offen hält (Muster <c>EPOS.Kern.Tests/TestDatenbank.cs</c>):
    /// Ein abgebrochener Lauf hinterlässt einen Ordner mit freier Marke, den der nächste Lauf wegräumt.
    /// </summary>
    internal sealed class Kopieordner : IDisposable
    {
        internal const string PRAEFIX = "gebaeudevergleich-probe-";
        internal const string BESITZMARKE = "besitz.sperre";
        private static readonly Regex ORDNERNAME = new Regex("^" + PRAEFIX + "[0-9a-f]{8}$", RegexOptions.CultureInvariant);
        private static readonly TimeSpan SCHONFRIST_OHNE_MARKE = TimeSpan.FromHours(2);
        private static bool _aufgeraeumt;

        private FileStream _marke;

        internal Kopieordner()
        {
            if (!_aufgeraeumt) { _aufgeraeumt = true; WaisenAufraeumen(); }
            Pfad = Path.Combine(Path.GetTempPath(), PRAEFIX + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(Pfad);
            _marke = new FileStream(Path.Combine(Pfad, BESITZMARKE), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }

        internal string Pfad { get; }

        internal string Datei(params string[] teile)
        {
            var alle = new List<string> { Pfad };
            alle.AddRange(teile);
            return Path.Combine(alle.ToArray());
        }

        /// <summary>Legt eine Kopie der Testdatenbank unter <paramref name="relativ"/> an.</summary>
        internal string Kopie(string relativ)
        {
            string ziel = Datei(relativ);
            Directory.CreateDirectory(Path.GetDirectoryName(ziel));
            File.Copy(Werkzeuglauf.Testdatenbank, ziel);
            return ziel;
        }

        public void Dispose()
        {
            try { SqliteConnection.ClearAllPools(); } catch { }
            try { _marke?.Dispose(); } catch { }
            _marke = null;
            try { Directory.Delete(Pfad, true); } catch { }
        }

        /// <summary>Räumt Ordner früherer Läufe weg, deren Marke frei ist (ihr Besitzer lebt nicht mehr).</summary>
        private static void WaisenAufraeumen()
        {
            foreach (string d in Directory.GetDirectories(Path.GetTempPath(), PRAEFIX + "*"))
            {
                if (!ORDNERNAME.IsMatch(Path.GetFileName(d))) continue;
                string marke = Path.Combine(d, BESITZMARKE);
                try
                {
                    if (File.Exists(marke))
                    {
                        using (new FileStream(marke, FileMode.Open, FileAccess.Write, FileShare.None)) { }
                    }
                    else if (DateTime.UtcNow - Directory.GetCreationTimeUtc(d) < SCHONFRIST_OHNE_MARKE) continue;
                    Directory.Delete(d, true);
                }
                catch (IOException) { /* Marke belegt: ein laufender Prozess besitzt den Ordner */ }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    /// <summary>
    /// Die Vorrichtung der Sammlung: höchstens vier Kopien der Testdatenbank je Lauf — die
    /// gemeinsame Grundkopie (nur gelesen) und die Varianten Bauweise 50, Schema 129 und WAL,
    /// jede in einem eigenen Ordner. Die Läufe des Werkzeugs entstehen bei Bedarf und einmal.
    /// </summary>
    public sealed class Vorrichtung : IDisposable
    {
        /// <summary>Ein Lauf des Werkzeugs samt seiner Ausgabe.</summary>
        internal sealed class Lauf
        {
            internal Werkzeuglauf.Ergebnis Ergebnis;
            internal string Ziel;
            internal List<Dictionary<string, string>> Gebaeude;
            internal List<Dictionary<string, string>> Projekte;

            internal Dictionary<string, string> Zeile(int projekt, int gebaeude)
                => Gebaeude.Find(z => z["Projekt"] == Id(projekt) && z["Gebaeude"] == Id(gebaeude));

            internal Dictionary<string, string> Projekt(int projekt)
                => Projekte.Find(z => z["Projekt"] == Id(projekt));

            private static string Id(int id) => id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private readonly List<Kopieordner> _ordner = new List<Kopieordner>();
        private Kopieordner _grundordner;
        private Kopieordner _ausgaben;
        private string _bauweise50, _schema129, _wal;
        private Lauf _laufA, _laufB;

        public Vorrichtung()
        {
            Vorhanden = Werkzeuglauf.Testdatenbank != null;
            if (!Vorhanden) return;
            _grundordner = Neu();
            Grund = _grundordner.Kopie("grund.sqlite");
            _ausgaben = Neu();
        }

        /// <summary>Steht die Testdatenbank? Sonst schweigen die Fälle.</summary>
        internal bool Vorhanden { get; }

        /// <summary>Die Grundkopie — nur gelesen.</summary>
        internal string Grund { get; }

        internal string GrundHashVorLaufA { get; private set; }
        internal string GrundHashNachLaufA { get; private set; }

        /// <summary>Ein neuer Ausgabeordner (noch nicht angelegt) unter dem Ausgabeordner der Vorrichtung.</summary>
        internal string Ausgabe(string name) => _ausgaben.Datei(name);

        /// <summary>Lauf A: <c>vergleich</c> über alle Projekte der Grundkopie, ohne Namen.</summary>
        internal Lauf LaufA
        {
            get
            {
                if (_laufA != null) return _laufA;
                GrundHashVorLaufA = Werkzeuglauf.Pruefsumme(Grund);
                _laufA = Vergleich(Grund, Ausgabe("lauf_a"));
                GrundHashNachLaufA = Werkzeuglauf.Pruefsumme(Grund);
                return _laufA;
            }
        }

        /// <summary>Lauf B: derselbe Aufruf ein zweites Mal (T11).</summary>
        internal Lauf LaufB => _laufB ??= Vergleich(Grund, Ausgabe("lauf_b"));

        /// <summary>Die Kopie mit Bauweise 50 Wh/K an Gebäude 10576 (T5).</summary>
        internal string Bauweise50 => _bauweise50 ??= Variante("bauweise50.sqlite", "UPDATE Tab_Gebaeude SET Bauweise = 50 WHERE ID = 10576");

        /// <summary>Die Kopie mit Schemastand 129 (T9).</summary>
        internal string Schema129 => _schema129 ??= Variante("schema129.sqlite", "UPDATE Tab_Applikation SET SchemaVersion = 129");

        /// <summary>Die Kopie für die Aufnahme — in einem Pfad mit Leerzeichen und # (T8).</summary>
        internal string Walkopie => _wal ??= Neu().Kopie(Path.Combine("q a#b", "quelle.sqlite"));

        internal static Lauf Vergleich(string db, string ziel, params string[] weitere)
        {
            var args = new List<string> { "vergleich", "--db", db, "--ziel", ziel };
            args.AddRange(weitere);
            var lauf = new Lauf { Ergebnis = Werkzeuglauf.Starten(args.ToArray()), Ziel = ziel };
            string g = Path.Combine(ziel, "gebaeude.csv");
            string p = Path.Combine(ziel, "projekte.csv");
            lauf.Gebaeude = File.Exists(g) ? Werkzeuglauf.Csv(g) : new List<Dictionary<string, string>>();
            lauf.Projekte = File.Exists(p) ? Werkzeuglauf.Csv(p) : new List<Dictionary<string, string>>();
            return lauf;
        }

        private string Variante(string name, string sql)
        {
            string pfad = Neu().Kopie(name);
            using (var v = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = pfad, Pooling = false }.ToString()))
            {
                v.Open();
                using SqliteCommand k = v.CreateCommand();
                k.CommandText = sql;
                Assert.True(k.ExecuteNonQuery() > 0, "Die Variante hat keine Zeile geändert: " + sql);
            }
            return pfad;
        }

        private Kopieordner Neu()
        {
            var o = new Kopieordner();
            _ordner.Add(o);
            return o;
        }

        public void Dispose()
        {
            foreach (Kopieordner o in _ordner) o.Dispose();
            _ordner.Clear();
        }
    }
}
