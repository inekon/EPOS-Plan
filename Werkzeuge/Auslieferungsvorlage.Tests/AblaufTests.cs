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
        //  A5 — Der Katalogwaechter (nur bei ausdruecklichem --kataloge readonly)
        // =============================================================================
        /// <summary>
        /// Die Regel aus Setup-Konzept 6.1 Schritt 3 („in <c>*_STAMM</c> bleibt, was
        /// <c>ReadOnly = TRUE</c> traegt") leert am Bestand der Testdatenbank 22 der 28
        /// Katalogtabellen. Das Werkzeug bricht deshalb ab, statt eine Vorlage mit leerem
        /// Katalog abzulegen — die faellt sonst erst beim Kunden auf.
        ///
        /// <para>Seit Anwenderentscheid <b>#160‑E‑1a</b> (11.09.2026) ist die Vorgabe
        /// <c>--kataloge alle</c>; die Regel und damit dieser Waechter greifen nur noch,
        /// wenn <c>--kataloge readonly</c> ausdruecklich gewaehlt wird — die Gegenprobe zum
        /// Aufruf OHNE den Schalter steht in A9.</para>
        /// </summary>
        [Fact]
        public void A5_Die_ReadOnly_Regel_bricht_mit_Kataloge_readonly_ab_wenn_sie_einen_Katalog_leert()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel, "--kataloge", "readonly");

            Assert.Equal(4, e.Code);
            Assert.Contains("Katalogtabelle", e.Fehlerausgabe);
            Assert.False(File.Exists(ziel), "Trotz Abbruch liegt eine Zieldatei da.");

            // Stufe G3 (L1): Der Waechter schlaegt nur an einem Katalog an, der auf NULL FAELLT.
            // Aufbaukatalog und Schichten sind von Anfang an leer, der Baustoffkatalog traegt
            // seine Saat mit ReadOnly = 1 - keiner der drei steht in der Abbruchliste.
            Assert.DoesNotContain("Tab_Bauteilschicht_STAMM", e.Fehlerausgabe);
            Assert.DoesNotContain("Tab_Bauteilaufbau_STAMM", e.Fehlerausgabe);
            Assert.DoesNotContain("Tab_Baustoff_STAMM", e.Fehlerausgabe);
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

        // =============================================================================
        //  A9 — Ohne den Schalter ist die Vorgabe seit #160-E-1a: alle Katalogzeilen
        //       bleiben, und der Katalogwaechter (A5) greift NICHT
        // =============================================================================
        /// <summary>
        /// Anwenderentscheid <b>#160‑E‑1a</b> (11.09.2026, „a"): Die Vorgabe von
        /// <c>--kataloge</c> ist <c>alle</c>. Ein Aufruf OHNE den Schalter darf deshalb
        /// nicht mehr am Katalogwaechter scheitern (Code 4, siehe A5) — jede Katalogzeile
        /// der Quelle muss Tabelle fuer Tabelle in der Vorlage wiederzufinden sein, genau
        /// die Vorher/Nachher-Zaehlung, die auch im Prueflauf (Schritt 3) steht.
        /// </summary>
        [Fact]
        public void A9_Ohne_Kataloge_Schalter_ist_die_Vorgabe_alle_und_jede_Zeile_bleibt()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string ziel = o.Datei("Kenndaten.sqlite");

            Dictionary<string, long> vorher = StammZeilenzahlen(quelle);

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel);   // KEIN --kataloge

            Assert.True(e.Code == 0, e.Alles);
            Assert.Contains("Modus: alle", e.Ausgabe);

            Dictionary<string, long> nachher = StammZeilenzahlen(ziel);
            Assert.Equal(vorher.Keys.OrderBy(k => k, StringComparer.Ordinal),
                         nachher.Keys.OrderBy(k => k, StringComparer.Ordinal));
            foreach (string t in vorher.Keys)
            {
                // Die Tww-Kataloge des Zapfprofilgenerators folgen ihrer EIGENEN Regel,
                // unabhaengig von --kataloge (Umsetzungskonzept Zapfprofilgenerator 3.2,
                // 6 (b)): Der fiktive Testkatalog (EIGEN, FIKTIV) faellt; es bleibt allein der freie
                // Paketteil (Herkunftsart FREI, VERFAHREN, EIGENKONSTRUKTION). Siehe TwwVorlageTests.
                if (t.StartsWith("Tab_Tww", StringComparison.Ordinal))
                {
                    long frei = FreieZeilen(ziel, t);
                    Assert.True(nachher[t] == frei, t + ": " + nachher[t] + " Zeile(n), davon " + frei +
                                                    " aus dem freien Paketteil — der fiktive Testkatalog gehoert nicht in die Vorlage.");
                    continue;
                }
                Assert.True(vorher[t] == nachher[t],
                            t + ": Quelle " + vorher[t] + " Zeile(n) gegen Vorlage " + nachher[t] + ".");
            }
            Assert.True(vorher.Values.Sum() > 0, "Die Quelle fuehrt keine Katalogzeile — die Probe waere leer.");
        }

        // =============================================================================
        //  A10 — Ein Beispiel mit Importherkunft laesst die Abnahme fallen (Schritt S-F)
        // =============================================================================
        /// <summary>
        /// <b>Die Gegenprobe zu <c>VorlageTests.P6d</c></b> (Datenaustauschkonzept 7.4): Ein
        /// Beispielpaket, dessen Gebäude eine Importquelle samt Paarung trägt, reist mit seiner
        /// Herkunft (der Projekttransfer führt beide Tabellen über ihre <c>KINDER</c>-Einträge) —
        /// und die Prüfregel „Importablage leer" lässt die Abnahme mit Code 5 fallen, statt
        /// Dateiname und SHA-256 eines fremden Imports in die Auslieferung zu tragen.
        /// </summary>
        [Fact]
        public void A10_Ein_Beispiel_mit_Importherkunft_faellt_in_der_Pruefung()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            const string BEISPIEL = "Laurentiuskirche";      // Projekt 1007 mit einem Gebaeude

            string werkbank = o.Datei("werkbank.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, werkbank);
            string paket = o.Datei("beispiel-mit-import.wpx");
            string vorher = DataRepository.PfadUeberschreibung;
            Func<bool> schreibrecht = Schreibnaht.Schreibrecht;
            try
            {
                DataRepository.PfadUeberschreibung = werkbank;
                Schreibnaht.WerkzeugFreigabe("Auslieferungsvorlage.Tests (Beispiel mit Importherkunft)");
                int gebaeude = Convert.ToInt32(DataRepository.ExecuteScalar(
                    "SELECT MIN(g.ID) FROM Tab_Gebaeude g INNER JOIN Tab_Projekt p ON p.ID = g.ID_Projekt WHERE p.Projektname = ?",
                    new DbParam("?", BEISPIEL)));
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    int quelle = v.EinfuegenUndId(
                        "INSERT INTO Tab_Importquelle (ID_Gebaeude, Format, Dateiname, Hash, Groesse, Zeitpunkt) " +
                        "VALUES (?, 'GBXML', 'haus.xml', ?, 1234, '2026-09-25T10:00:00+02:00')",
                        new[] { new DbParam("@g", gebaeude), new DbParam("@h", new string('a', 64)) });
                    v.Ausfuehren("INSERT INTO Tab_Importzuordnung (ID_Importquelle, ID_Gebaeude, Quellkennung, Quelltyp) " +
                                 "VALUES (?, ?, 'bldg-1', 'Building')", new DbParam("@q", quelle), new DbParam("@g", gebaeude));
                    v.Commit();
                }
                Assert.True(new ProjektExportImportCtrl().Exportieren(BEISPIEL, paket), "Der Export des Beispiels ist fehlgeschlagen.");
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                Schreibnaht.Schreibrecht = schreibrecht;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }

            string quelldatei = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelldatei);
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelldatei, o.Datei("Kenndaten.sqlite"),
                                                           "--beispiele", paket, "--trocken");
            Assert.True(e.Code == 5, e.Alles);
            Assert.Contains("FEHLER  Importablage leer (Tab_Importquelle 1, Tab_Importzuordnung 1)", e.Ausgabe);
        }

        // =============================================================================
        //  A11 — Die gemerkten Zuordnungen des Namensabgleichs werden bereinigt
        // =============================================================================
        /// <summary>
        /// <b>Die Gegenprobe zu <c>VorlageTests.P6e</c></b> (Namensabgleich der Baustoffe, N7): Eine Quelle, in
        /// der zwei Projekte gemerkte Zuordnungen tragen, liefert eine Vorlage ohne jede Zuordnung — die
        /// Tabelle hängt über <c>ID_Projekt</c> mit Löschweitergabe am Projekt und fällt mit der
        /// Projektbereinigung. Die Synonyme der Auslieferung bleiben vollständig.
        /// </summary>
        [Fact]
        public void A11_Gemerkte_Baustoffzuordnungen_fallen_die_Synonyme_bleiben()
        {
            if (Werkzeuglauf.Testdatenbank == null) return;
            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            string vorher = DataRepository.PfadUeberschreibung;
            Func<bool> schreibrecht = Schreibnaht.Schreibrecht;
            try
            {
                DataRepository.PfadUeberschreibung = quelle;
                Schreibnaht.WerkzeugFreigabe("Auslieferungsvorlage.Tests (gemerkte Baustoffzuordnungen)");
                foreach (int projekt in new[] { 1007, 1030 })
                    Assert.Equal(1, DataRepository.ExecuteNonQuery(
                        "INSERT INTO \"Tab_Baustoffzuordnung\" (\"ID_Projekt\", \"Materialname\", \"ID_Baustoff\", \"Zeitpunkt\") " +
                        "VALUES (?, 'fussbodenaufbau', 5, '2026-09-25T10:00:00Z')", new DbParam("@p", projekt)));
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                Schreibnaht.Schreibrecht = schreibrecht;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }

            string ziel = o.Datei("Kenndaten.sqlite");
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, ziel);
            Assert.True(e.Code == 0, e.Alles);

            try
            {
                DataRepository.PfadUeberschreibung = ziel;
                Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"Tab_Baustoffzuordnung\"")));
                Assert.Equal((long)BaustoffabgleichSchema.Saat.Count, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM \"Tab_Baustoffsynonym_STAMM\" WHERE \"ReadOnly\" = 1")));
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
        }

        // -----------------------------------------------------------------------------

        /// <summary>
        /// Die Zeilen einer Tww-Tabelle, die aus dem freien Paketteil stammen: Herkunftsart FREI,
        /// bei den Ereignissen die eines freien Bedarfstags; eine Tabelle ohne Herkunft sonst 0.
        /// </summary>
        private static long FreieZeilen(string datei, string tabelle)
        {
            string vorher = DataRepository.PfadUeberschreibung;
            try
            {
                DataRepository.PfadUeberschreibung = datei;
                // Der Paketteil fuehrt FREI (freie Quelle), VERFAHREN (gerechnet, die aus VDI 6002
                // abgeleiteten Nutzungsarten samt Tagesgaengen, ZU20) und EIGENKONSTRUKTION (die
                // Setzungen der Speicherauslegung aus der INEKON-Vorlage V4, N28).
                var herkunft = new[] { new DbParam("?", "FREI"), new DbParam("?", "VERFAHREN"), new DbParam("?", "EIGENKONSTRUKTION") };
                if (tabelle == "Tab_TwwBedarfstagEreignis_STAMM")
                    return Convert.ToInt64(DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM Tab_TwwBedarfstagEreignis_STAMM WHERE ID_Bedarfstag IN " +
                        "(SELECT ID FROM Tab_TwwBedarfstag_STAMM WHERE Herkunftsart IN (?, ?, ?))", herkunft));
                // Der Tagesgangsatz traegt keine eigene Herkunft — sie steht an seinen Tagesgaengen.
                if (tabelle == "Tab_TwwTagesgangsatz_STAMM")
                    return Convert.ToInt64(DataRepository.ExecuteScalar(
                        "SELECT COUNT(DISTINCT ID_Tagesgangsatz) FROM Tab_TwwTagesgang_STAMM WHERE Herkunftsart IN (?, ?, ?)", herkunft));
                string spalte = DataRepository.SpaltenVonTabelle(tabelle)
                    .FirstOrDefault(s => s == "Herkunftsart" || s.EndsWith("_Herkunftsart", StringComparison.Ordinal));
                if (spalte == null) return 0;
                return Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" IN (?, ?, ?)", herkunft));
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
        }

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

        /// <summary>Nur die Auslieferungskataloge (<c>*_STAMM</c>) — dieselbe Abgrenzung wie
        /// <see cref="Vorlagenbau.KatalogeBereinigen"/> ueber <c>Projektsicht.Stammtabellen</c>.</summary>
        private static Dictionary<string, long> StammZeilenzahlen(string datei)
        {
            string vorher = DataRepository.PfadUeberschreibung;
            try
            {
                DataRepository.PfadUeberschreibung = datei;
                var d = new Dictionary<string, long>(StringComparer.Ordinal);
                DataTable t = DataRepository.GetDataTable(
                    "SELECT name FROM sqlite_master WHERE type = 'table' AND name LIKE '%\\_STAMM' ESCAPE '\\' " +
                    "ORDER BY name");
                foreach (DataRow r in t.Rows)
                {
                    string name = Convert.ToString(r["name"]);
                    if (!name.EndsWith("_STAMM", StringComparison.Ordinal)) continue;
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
                    // Tww-Kataloge: eigene Regel ueber Status (TwwVorlageTests.T1).
                    if (name.StartsWith("Tab_Tww", StringComparison.Ordinal)) continue;
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
