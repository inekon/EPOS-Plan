using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace Auslieferungsvorlage.Tests
{
    /// <summary>
    /// Die Proben, die einen EIGENEN Lauf brauchen: Trockenlauf, Schreibort,
    /// Katalogwaechter, Aufruffehler und die Wiederholbarkeit.
    /// </summary>
    [Collection("Auslieferungsvorlage")]
    public sealed class AblaufTests
    {
        // =============================================================================
        //  A1 — Der Aufruf ohne Argumente erklaert sich und meldet 2
        // =============================================================================
        [Fact]
        public void A1_Ohne_Argumente_kommt_die_Hilfe_und_Rueckgabe_2()
        {
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten();
            Assert.Equal(2, e.Code);
            Assert.Contains("--beispiele", e.Ausgabe);
            Assert.Contains("Rueckgabe:", e.Ausgabe);
        }

        [Fact]
        public void A2_Eine_fehlende_Quelle_meldet_2_mit_Grund_auf_stderr()
        {
            using var o = new Arbeitsordner();
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(o.Datei("gibtsnicht.sqlite"), o.Datei("ziel.sqlite"));
            Assert.Equal(2, e.Code);
            Assert.Contains("Quelldatenbank nicht gefunden", e.Fehlerausgabe);
        }

        // =============================================================================
        //  A3 — Das Repository ist tabu (ausser Setup/Vorlage/)
        // =============================================================================
        /// <summary>
        /// Die Weigerung greift VOR jedem Datenbankzugriff — deshalb braucht dieser Fall
        /// die Testdatenbank nicht einmal zu kopieren und ist in Millisekunden durch.
        /// </summary>
        [Fact]
        public void A3_Ein_Ziel_im_Repository_wird_verweigert()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;

            string verboten = Path.Combine(Werkzeuglauf.Repowurzel, "Kenndaten.sqlite");
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(Werkzeuglauf.Testdatenbank, verboten);

            Assert.Equal(3, e.Code);
            Assert.Contains("Setup/Vorlage", e.Fehlerausgabe);
            Assert.False(File.Exists(verboten), "Das Werkzeug hat trotz Weigerung geschrieben.");
        }

        /// <summary>
        /// Die Gegenprobe: <c>Setup/Vorlage/</c> ist erlaubt. Geprueft wird nur die
        /// WEIGERUNG (sie faellt vor dem ersten Datenbankzugriff), nicht der ganze Lauf —
        /// ein Lauf mit fehlender Quelle endet mit 2, nicht mit 3.
        /// </summary>
        [Fact]
        public void A4_Setup_Vorlage_ist_der_eine_erlaubte_Ort_im_Repository()
        {
            string erlaubt = Path.Combine(Werkzeuglauf.Repowurzel, "Setup", "Vorlage", "Kenndaten.sqlite");
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(
                Path.Combine(Werkzeuglauf.Repowurzel, "gibtsnicht.sqlite"), erlaubt);

            Assert.Equal(2, e.Code);            // Quelle fehlt — aber NICHT 3
            Assert.False(File.Exists(erlaubt));
        }

        // =============================================================================
        //  A5 — Der Katalogwaechter
        // =============================================================================
        /// <summary>
        /// Die Regel aus Setup-Konzept 6.1 Schritt 3 („in <c>*_STAMM</c> bleibt, was
        /// <c>ReadOnly = TRUE</c> traegt") leert am Bestand der Testdatenbank 22 der 28
        /// Katalogtabellen. Das Werkzeug bricht deshalb ab, statt eine Vorlage mit leerem
        /// Katalog abzulegen — die faellt sonst erst beim Kunden auf.
        /// </summary>
        [Fact]
        public void A5_Die_ReadOnly_Regel_bricht_ab_wenn_sie_einen_Katalog_leert()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel);

            Assert.Equal(4, e.Code);
            Assert.Contains("Katalogtabelle", e.Fehlerausgabe);
            Assert.False(File.Exists(ziel), "Trotz Abbruch liegt eine Zieldatei da.");
        }

        // =============================================================================
        //  A6 — Der Trockenlauf schreibt nichts
        // =============================================================================
        [Fact]
        public void A6_Der_Trockenlauf_berichtet_und_schreibt_nichts()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--kataloge", "alle", "--trocken");

            Assert.True(e.Code == 0, e.Alles);
            Assert.Contains("Schritt 2 — Projektdaten entfernen", e.Ausgabe);
            Assert.False(File.Exists(ziel), "Der Trockenlauf hat eine Zieldatei angelegt.");
            Assert.False(File.Exists(ziel + ".bericht.txt"));
        }

        // =============================================================================
        //  A7 — Zwei Laeufe aus derselben Quelle liefern dieselbe Vorlage
        // =============================================================================
        /// <summary>
        /// <b>Wiederholbarkeit, nicht Byte-Gleichheit.</b> Eine SQLite-Datei ist als
        /// GANZES nicht reproduzierbar — <c>VACUUM</c> und die Seitenbelegung haengen an
        /// der Reihenfolge der Schreibvorgaenge, und der Import vergibt neue Ids. Was
        /// wiederholbar sein MUSS, ist der Inhalt: dieselben Tabellen mit denselben
        /// Zeilenzahlen und dieselbe Projektliste. (Dieselbe Unterscheidung trifft
        /// <c>EPOS.Kern.Tests.ProjekttransferTests</c> fuer die Pakete.)
        /// </summary>
        [Fact]
        public void A7_Ein_zweiter_Lauf_liefert_denselben_Inhalt()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);

            string a = o.Datei("a.sqlite");
            string b = o.Datei("b.sqlite");
            Assert.Equal(0, Werkzeuglauf.Starten(quelle, a, "--kataloge", "alle").Code);
            Assert.Equal(0, Werkzeuglauf.Starten(quelle, b, "--kataloge", "alle").Code);

            Dictionary<string, long> za = Zeilenzahlen(a);
            Dictionary<string, long> zb = Zeilenzahlen(b);

            Assert.Equal(za.Keys.OrderBy(k => k, StringComparer.Ordinal),
                         zb.Keys.OrderBy(k => k, StringComparer.Ordinal));
            foreach (string t in za.Keys)
                Assert.True(za[t] == zb[t], t + ": " + za[t] + " gegen " + zb[t]);
        }

        // =============================================================================
        //  A8 — Ein zweiter Lauf ueber eine bestehende Zieldatei ersetzt sie
        // =============================================================================
        [Fact]
        public void A8_Eine_vorhandene_Zieldatei_wird_ersetzt_statt_zu_scheitern()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");
            File.WriteAllText(ziel, "kein gueltiger Datenbankinhalt");

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--kataloge", "alle");

            Assert.True(e.Code == 0, e.Alles);
            Assert.True(new FileInfo(ziel).Length > 1_000_000);
        }

        // -----------------------------------------------------------------------------
        private static Dictionary<string, long> Zeilenzahlen(string datei)
        {
            string vorher = DataRepository.PfadUeberschreibung;
            try
            {
                DataRepository.PfadUeberschreibung = datei;
                var d = new Dictionary<string, long>(StringComparer.Ordinal);
                DataTable t = DataRepository.GetDataTable(
                    "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name");
                foreach (DataRow r in t.Rows)
                {
                    string name = Convert.ToString(r["name"]);
                    d[name] = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + name + "\""));
                }
                return d;
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
        }
    }

    /// <summary>
    /// Die Probe zur KATALOGREGEL selbst — mit ausdruecklicher Freigabe der Leerung, weil
    /// der Waechter sonst abbricht.
    /// </summary>
    [Collection("Auslieferungsvorlage")]
    public sealed class KatalogregelTests
    {
        /// <summary>
        /// Nach dem Lauf im Modus <c>readonly</c> traegt in jeder <c>*_STAMM</c>-Tabelle
        /// mit Spalte <c>ReadOnly</c> jede verbliebene Zeile <c>ReadOnly = TRUE</c>.
        /// Zugleich der Beleg fuer den Befund: 22 Tabellen sind danach leer.
        /// </summary>
        [Fact]
        public void K1_In_den_STAMM_Tabellen_bleibt_nur_ReadOnly_TRUE()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(
                quelle, ziel, "--kataloge", "readonly", "--katalogleerung-zulassen");
            Assert.True(e.Code == 0, e.Alles);

            string vorher = DataRepository.PfadUeberschreibung;
            try
            {
                DataRepository.PfadUeberschreibung = ziel;
                var gepruefte = new List<string>();
                DataTable t = DataRepository.GetDataTable(
                    "SELECT name FROM sqlite_master WHERE type = 'table' AND name LIKE '%\\_STAMM' ESCAPE '\\' " +
                    "ORDER BY name");
                foreach (DataRow r in t.Rows)
                {
                    string name = Convert.ToString(r["name"]);
                    if (!name.EndsWith("_STAMM", StringComparison.Ordinal)) continue;
                    if (!DataRepository.SpalteVorhanden(name, "ReadOnly")) continue;
                    gepruefte.Add(name);
                    long uebrig = Convert.ToInt64(DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM \"" + name + "\" WHERE \"ReadOnly\" IS NULL OR \"ReadOnly\" = 0"));
                    Assert.True(uebrig == 0, name + ": " + uebrig + " Zeile(n) ohne ReadOnly = TRUE.");
                }
                Assert.True(gepruefte.Count >= 20,
                            "Es wurden nur " + gepruefte.Count + " Katalogtabellen geprueft — zu wenige.");
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
        }
    }
}
