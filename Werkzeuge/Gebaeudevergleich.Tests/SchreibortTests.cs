using System;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace Gebaeudevergleich.Tests
{
    /// <summary>
    /// T7 (Schreibort), T9 (Schemastand), T14 (Schreibsperre und Pfad) und T15 (Variante).
    /// </summary>
    [Collection(Vergleichssammlung.NAME)]
    public sealed class SchreibortTests
    {
        private readonly Vorrichtung _v;

        public SchreibortTests(Vorrichtung v) { _v = v; }

        private static string Probe(string ordner) => Path.Combine(ordner, "gv-probe-" + Guid.NewGuid().ToString("N").Substring(0, 8));

        /// <summary>Ein Pfad unter %ProgramData%\EPOS_PLAN, den es nicht gibt — die Produktivdatei wird nie genannt.</summary>
        private static string Produktiv(string endung = "") => Probe(Schreibort.Produktivordner) + endung;

        private static void Verweigert(string[] argumente, string nichtAngelegt)
        {
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(argumente);
            Assert.True(e.Code == 2, "Exitcode " + e.Code + ": " + e.Alles);
            Assert.Contains("Abbruch", e.Fehlerausgabe);
            if (nichtAngelegt != null)
                Assert.False(Directory.Exists(nichtAngelegt) || File.Exists(nichtAngelegt), "angelegt: " + nichtAngelegt);
        }

        [Fact]
        public void T7_Ziel_im_Repository_oder_unter_ProgramData_und_db_unter_ProgramData_werden_verweigert()
        {
            if (!_v.Vorhanden) return;
            string repo = Probe(Path.Combine(Werkzeuglauf.Repowurzel, "Werkzeuge", "Gebaeudevergleich"));
            string programData = Produktiv();

            // vergleich
            Verweigert(new[] { "vergleich", "--db", _v.Grund, "--ziel", repo }, repo);
            Verweigert(new[] { "vergleich", "--db", _v.Grund, "--ziel", programData }, programData);
            string zielFrei = _v.Ausgabe("t7_frei");
            Verweigert(new[] { "vergleich", "--db", Produktiv(".sqlite"), "--ziel", zielFrei }, zielFrei);

            // aufnahme
            Verweigert(new[] { "aufnahme", "--quelle", _v.Grund, "--ziel", repo }, repo);
            Verweigert(new[] { "aufnahme", "--quelle", _v.Grund, "--ziel", programData }, programData);

            // variante
            Verweigert(new[] { "variante", "--db", _v.Grund, "--modell", "VDI6007", "--ziel", repo + ".sqlite" }, repo + ".sqlite");
            Verweigert(new[] { "variante", "--db", _v.Grund, "--modell", "VDI6007", "--ziel", programData + ".sqlite" }, programData + ".sqlite");
            string varianteFrei = _v.Ausgabe("t7_variante.sqlite");
            Verweigert(new[] { "variante", "--db", Produktiv(".sqlite"), "--modell", "VDI6007", "--ziel", varianteFrei }, varianteFrei);

            Assert.False(Directory.Exists(Schreibort.Produktivordner) &&
                         Directory.GetFileSystemEntries(Schreibort.Produktivordner, "gv-probe-*").Length > 0);
        }

        [Fact]
        public void T9_Schemastand_129_bricht_mit_beiden_Staenden_ab()
        {
            if (!_v.Vorhanden) return;
            string ziel = _v.Ausgabe("t9");
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten("vergleich", "--db", _v.Schema129, "--ziel", ziel);
            Assert.True(e.Code == 2, e.Alles);
            string ziel142 = SchemaStand.Zielversion.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.Contains("Schemastand der Datenbank 129, Zielstand dieses Werkzeugs " + ziel142, e.Fehlerausgabe);
            Assert.Contains("Migrationsweg", e.Fehlerausgabe);
            Assert.False(File.Exists(Path.Combine(ziel, "gebaeude.csv")));

            string variante = _v.Ausgabe("t9_variante.sqlite");
            e = Werkzeuglauf.Starten("variante", "--db", _v.Schema129, "--modell", "VDI6007", "--ziel", variante);
            Assert.True(e.Code == 2, e.Alles);
            Assert.Contains("129", e.Fehlerausgabe);
            Assert.False(File.Exists(variante));
        }

        [Fact]
        public void T14_Schreibsperre_und_Pfad()
        {
            if (!_v.Vorhanden) return;
            Func<bool> vorher = Schreibnaht.Schreibrecht;
            string pfadVorher = DataRepository.PfadUeberschreibung;
            string hash = Werkzeuglauf.Pruefsumme(_v.Grund);
            try
            {
                Einstieg.SchreibsperreSetzen();
                Assert.False(Schreibnaht.Schreibrecht());
                Assert.False(Schreibnaht.DarfSchreiben());

                Assert.Null(Einstieg.DatenbankBinden(_v.Grund));
                Assert.Equal(Path.GetFullPath(_v.Grund), DataRepository.GetDBPath());

                // Ein Schreibversuch über die Zugriffsschicht scheitert an der Sperre.
                int n = DataRepository.ExecuteNonQuery("UPDATE Tab_Applikation SET SchemaVersion = SchemaVersion");
                Assert.Equal(-1, n);

                // Eine Überschreibung auf %ProgramData%\EPOS_PLAN wird verweigert und zurückgenommen.
                string grund = Einstieg.DatenbankBinden(Produktiv(".sqlite"));
                Assert.NotNull(grund);
                Assert.Null(DataRepository.PfadUeberschreibung);
            }
            finally
            {
                Schreibnaht.Schreibrecht = vorher;
                DataRepository.PfadUeberschreibung = pfadVorher;
                SqliteConnection.ClearAllPools();
            }
            Assert.Equal(hash, Werkzeuglauf.Pruefsumme(_v.Grund));
        }

        [Fact]
        public void T15_Variante_setzt_jede_Gebaeudezeile_und_laesst_die_Quelle()
        {
            if (!_v.Vorhanden) return;
            string hash = Werkzeuglauf.Pruefsumme(_v.Grund);
            string ziel = _v.Ausgabe("t15_tagesbilanz.sqlite");
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten("variante", "--db", _v.Grund, "--modell", "TAGESBILANZ", "--ziel", ziel);
            Assert.True(e.Code == 0, e.Alles);
            Assert.Equal(hash, Werkzeuglauf.Pruefsumme(_v.Grund));

            using (var v = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = ziel, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
            {
                v.Open();
                using SqliteCommand k = v.CreateCommand();
                k.CommandText = "SELECT COUNT(*), SUM(CASE WHEN Gebaeude_Modell = 'TAGESBILANZ' THEN 1 ELSE 0 END) FROM Tab_Gebaeude";
                using SqliteDataReader r = k.ExecuteReader();
                Assert.True(r.Read());
                long alle = r.GetInt64(0), gesetzt = r.GetInt64(1);
                Assert.True(alle > 0);
                Assert.Equal(alle, gesetzt);
            }
            string protokoll = File.ReadAllText(ziel + ".protokoll.txt", Encoding.UTF8);
            Assert.Contains("integrity_check der Variante: ok", protokoll);

            // Schreibort: ein vorhandenes Ziel wird nicht überschrieben, eines im Repository verweigert.
            Werkzeuglauf.Ergebnis nochmal = Werkzeuglauf.Starten("variante", "--db", _v.Grund, "--modell", "VDI6007", "--ziel", ziel);
            Assert.Equal(2, nochmal.Code);
            string repo = Probe(Path.Combine(Werkzeuglauf.Repowurzel, "Werkzeuge", "Gebaeudevergleich")) + ".sqlite";
            Verweigert(new[] { "variante", "--db", _v.Grund, "--modell", "TAGESBILANZ", "--ziel", repo }, repo);

            SqliteConnection.ClearAllPools();
            File.Delete(ziel);
        }
    }
}
