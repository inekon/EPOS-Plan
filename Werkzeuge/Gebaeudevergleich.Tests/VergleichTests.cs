using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Xunit;
using Xunit.Abstractions;

namespace Gebaeudevergleich.Tests
{
    /// <summary>
    /// T1–T6 und T10–T12: der Vergleich an der Grundkopie der Testdatenbank, gehalten gegen die
    /// Referenzbasis. Die Zahlen des Werkzeugs sind dieselben wie die des Laufs — sonst verglichen
    /// es zwei Wege, die der Anwender nie rechnet.
    /// </summary>
    [Collection(Vergleichssammlung.NAME)]
    public sealed class VergleichTests
    {
        /// <summary>Relative Toleranz gegen die Basis (T1, T2, T4).</summary>
        private const double REL = 1e-4;

        /// <summary>Das Projekt des Tagesbilanz-Wegs in der Basis (bis Stufe GA).</summary>
        private const int PROJEKT_TAGESBILANZ = 1040;

        private readonly Vorrichtung _v;
        private readonly ITestOutputHelper _aus;

        public VergleichTests(Vorrichtung v, ITestOutputHelper aus)
        {
            _v = v;
            _aus = aus;
        }

        private static string Id(int id) => id.ToString(CultureInfo.InvariantCulture);

        private static void Relativ(double erwartet, double ist, string was)
            => Assert.True(Math.Abs(ist - erwartet) <= REL * Math.Abs(erwartet),
                           was + ": erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) +
                           ", ist " + ist.ToString("R", CultureInfo.InvariantCulture));

        /// <summary>Die Gebäude der Basis: (Projekt, Index, Aggregat) je <c>Geb[i]</c>.</summary>
        private static List<(int Projekt, int I, Dictionary<string, string> Agg)> BasisGebaeude()
        {
            var liste = new List<(int, int, Dictionary<string, string>)>();
            foreach (string ordner in Directory.GetDirectories(Werkzeuglauf.Basis, "Projekt_*").OrderBy(d => d, StringComparer.Ordinal))
            {
                int projekt = int.Parse(Path.GetFileName(ordner).Substring("Projekt_".Length), CultureInfo.InvariantCulture);
                Dictionary<string, string> agg = Werkzeuglauf.Aggregat(ordner);
                for (int i = 0; agg.ContainsKey("Geb[" + Id(i) + "].ID_Gebaeude"); i++) liste.Add((projekt, i, agg));
            }
            return liste;
        }

        private static double Agg(Dictionary<string, string> agg, int i, string groesse)
            => Werkzeuglauf.Zahl(agg["Geb[" + Id(i) + "]." + groesse]);

        // =================================================================================

        [Fact]
        public void T1_Paritaet_VDI_jedes_Gebaeude_der_Basis()
        {
            if (!_v.Vorhanden) return;
            Vorrichtung.Lauf a = _v.LaufA;

            int geprueft = 0;
            foreach ((int projekt, int i, Dictionary<string, string> agg) in BasisGebaeude())
            {
                if (projekt == PROJEKT_TAGESBILANZ) continue;
                int gebaeude = int.Parse(agg["Geb[" + Id(i) + "].ID_Gebaeude"], CultureInfo.InvariantCulture);
                Dictionary<string, string> z = a.Zeile(projekt, gebaeude);
                Assert.True(z != null, "Keine Zeile P" + Id(projekt) + "/G" + Id(gebaeude));
                Assert.Equal("1", z["Neu_ok"]);
                string wer = "P" + Id(projekt) + "/G" + Id(gebaeude);
                Relativ(Agg(agg, i, "JahresheizwaermeMwh"), Werkzeuglauf.Zahl(z["Jahr_MWh_neu"]), wer + " Jahr");
                Relativ(Agg(agg, i, "SpitzeKw"), Werkzeuglauf.Zahl(z["Spitze_Stunde_kW_neu"]), wer + " Spitze");
                Relativ(Agg(agg, i, "SpitzeTagesmittelKw"), Werkzeuglauf.Zahl(z["Spitze_24h_Mittel_kW_neu"]), wer + " 24-h-Mittel");
                Relativ(Agg(agg, i, "Spitze95Kw"), Werkzeuglauf.Zahl(z["Q95_kW_neu"]), wer + " Q95");
                geprueft++;
            }
            _aus.WriteLine("T1: " + Id(geprueft) + " Gebäude der Basis auf dem VDI-Weg geprüft.");
            Assert.True(geprueft >= 15, "Zu wenige Gebäude in der Basis: " + Id(geprueft));

            // Der Anker aus dem Auftrag: 1045/10651.
            Dictionary<string, string> anker = a.Zeile(1045, 10651);
            Relativ(75.9406927, Werkzeuglauf.Zahl(anker["Jahr_MWh_neu"]), "1045 Jahr");
            Relativ(39.6751852, Werkzeuglauf.Zahl(anker["Spitze_Stunde_kW_neu"]), "1045 Spitze");
            Relativ(28.9795791, Werkzeuglauf.Zahl(anker["Spitze_24h_Mittel_kW_neu"]), "1045 24-h-Mittel");
            Relativ(21.6898285, Werkzeuglauf.Zahl(anker["Q95_kW_neu"]), "1045 Q95");
        }

        [Fact]
        public void T2_Paritaet_Tagesbilanz_1040()
        {
            if (!_v.Vorhanden) return;
            Dictionary<string, string> agg = Werkzeuglauf.Aggregat(Path.Combine(Werkzeuglauf.Basis, "Projekt_1040"));
            double erwartet = Werkzeuglauf.Zahl(agg["Vektor.waermebedarf_gebaeude.Summe"]) / 1e6;

            Dictionary<string, string> z = _v.LaufA.Zeile(PROJEKT_TAGESBILANZ, 10645);
            Assert.NotNull(z);
            Assert.Equal("1", z["Alt_ok"]);
            Assert.Equal("1", z["Neu_ok"]);
            Relativ(erwartet, Werkzeuglauf.Zahl(z["Jahr_MWh_alt"]), "1040 alt");
            Assert.Contains("U-TB", z["Regeln"].Split(' '));
            Assert.Equal("TAGESBILANZ", z["Spalte_Modell"]);
            _aus.WriteLine("T2: 1040 alt " + z["Jahr_MWh_alt"] + " MWh (Basis " + erwartet.ToString("R", CultureInfo.InvariantCulture) + ")");
        }

        [Fact]
        public void T3_R12_1045_Modellwechsel_erklaert()
        {
            if (!_v.Vorhanden) return;
            Dictionary<string, string> z = _v.LaufA.Zeile(1045, 10651);
            Assert.NotNull(z);
            Relativ(59.354116, Werkzeuglauf.Zahl(z["Jahr_MWh_alt"]), "1045 alt");
            double delta = Werkzeuglauf.Zahl(z["Jahr_MWh_delta_proz"]);
            Assert.InRange(delta, 27.5, 28.5);
            Assert.Equal("erklärt", z["Ampel"]);
            Assert.Contains("U-MW", z["Regeln"].Split(' '));
            Assert.Equal("kein Katalogwert", z["Katalogtreffer"]);
            Assert.Equal("1", z["Flaechenangabe"]);
            _aus.WriteLine("T3: 1045 alt " + z["Jahr_MWh_alt"] + ", neu " + z["Jahr_MWh_neu"] + ", Δ " + z["Jahr_MWh_delta_proz"] + " %");
        }

        [Fact]
        public void T3b_Band_E8_an_1009()
        {
            if (!_v.Vorhanden) return;
            Dictionary<string, string> z = _v.LaufA.Zeile(1009, 10612);
            Assert.NotNull(z);
            Assert.Equal("1", z["Alt_ok"]);
            Assert.Equal("1", z["Neu_ok"]);
            Assert.Equal("0", z["Flaechenangabe"]);
            Assert.Equal("nicht bestimmbar (Verbrauchsangabe)", z["Katalogtreffer"]);
            Assert.Equal(15200.0, Werkzeuglauf.Zahl(z["Bauweise_Wh_K"]));

            double delta = Math.Abs(Werkzeuglauf.Zahl(z["Jahr_MWh_delta_proz"]));
            _aus.WriteLine("T3b: 1009 alt " + z["Jahr_MWh_alt"] + " MWh, neu " + z["Jahr_MWh_neu"] + " MWh, |Δ Jahr| " +
                           delta.ToString("0.000000", CultureInfo.InvariantCulture) + " %, Verbrauchstreffer alt " +
                           z["Verbrauchstreffer_alt_proz"] + " %, neu " + z["Verbrauchstreffer_neu_proz"] + " %; Band E8 " +
                           Ursachenregeln.BAND_E8_PROZENT.ToString("0.0", CultureInfo.InvariantCulture) + " %");

            Assert.True(Ursachenregeln.BAND_E8_PROZENT <= 2.0, "Das Band E8 darf höchstens 2 % betragen.");
            Assert.True(delta <= Ursachenregeln.BAND_E8_PROZENT,
                        "Der Messwert |Δ Jahr| an 1009 liegt über dem Band E8 - an den Orchestrator melden, die Regel nicht biegen.");
            Assert.Contains("U-E8", z["Regeln"].Split(' '));
            Assert.Equal("erklärt", z["Ampel"]);
        }

        [Fact]
        public void T4_Kalibrierprobe_alle_Gebaeude_der_Referenzprojekte()
        {
            if (!_v.Vorhanden) return;
            Vorrichtung.Lauf a = _v.LaufA;

            var projekte = Directory.GetDirectories(Werkzeuglauf.Basis, "Projekt_*")
                                    .Select(d => int.Parse(Path.GetFileName(d).Substring("Projekt_".Length), CultureInfo.InvariantCulture))
                                    .OrderBy(p => p).ToList();
            int gebaeude = 0;
            var nichtErklaert = new List<string>();
            foreach (int p in projekte)
            {
                List<Dictionary<string, string>> zeilen = a.Gebaeude.Where(z => z["Projekt"] == Id(p)).ToList();
                if (zeilen.Count == 0)
                {
                    Dictionary<string, string> pz = a.Projekt(p);
                    Assert.Equal("übersprungen", pz["Status"]);
                    _aus.WriteLine("T4: P" + Id(p) + " übersprungen (" + pz["Grund"] + ")");
                    continue;
                }
                foreach (Dictionary<string, string> z in zeilen)
                {
                    gebaeude++;
                    string wer = "P" + z["Projekt"] + "/G" + z["Gebaeude"];
                    Assert.True(z["Alt_ok"] == "1" && z["Neu_ok"] == "1" && z["Ampel"] != "Fehler",
                                wer + " ist rot: " + z["Alt_Fehlercode"] + " " + z["Neu_Fehlercode"] + " " + z["Vermerk"]);
                    if (z["Ampel"] != "erklärt")
                        nichtErklaert.Add(wer + ": " + z["Ampel"] + " [" + z["Regeln"] + "] Δ Jahr " + z["Jahr_MWh_delta_proz"] +
                                          " %, Katalog neu " + z["Katalogtreffer_neu_proz"]);
                }
            }
            _aus.WriteLine("T4: " + Id(gebaeude) + " Gebäude in " + Id(projekte.Count) + " Basisprojekten, ohne Fehler.");
            foreach (string s in nichtErklaert) _aus.WriteLine("T4 nicht erklärt: " + s);
            Assert.True(gebaeude >= 15, "Zu wenige Gebäude: " + Id(gebaeude));
            Assert.Equal("übersprungen", a.Projekt(1030)["Status"]);

            // 1017 (und jedes gekühlte Basisgebäude): U-KU und die Kälte des Laufs.
            Dictionary<string, string> k = a.Zeile(1017, 10599);
            Assert.Contains("U-KU", k["Regeln"].Split(' '));
            Relativ(2.52168642, Werkzeuglauf.Zahl(k["Kaelte_neu_MWh"]), "1017 Kälte");
            foreach ((int projekt, int i, Dictionary<string, string> agg) in BasisGebaeude())
            {
                if (!agg.ContainsKey("Geb[" + Id(i) + "].KuehlenergieMwh")) continue;
                Dictionary<string, string> z = a.Zeile(projekt, int.Parse(agg["Geb[" + Id(i) + "].ID_Gebaeude"], CultureInfo.InvariantCulture));
                Assert.Contains("U-KU", z["Regeln"].Split(' '));
                Relativ(Agg(agg, i, "KuehlenergieMwh"), Werkzeuglauf.Zahl(z["Kaelte_neu_MWh"]), "P" + Id(projekt) + " Kälte");
            }
        }

        [Fact]
        public void T5_Fehlerfang_Bauweise_50()
        {
            if (!_v.Vorhanden) return;
            Vorrichtung.Lauf l = Vorrichtung.Vergleich(_v.Bauweise50, _v.Ausgabe("lauf_bauweise50"), "--projekte", "1008,1045");
            Assert.True(l.Ergebnis.Code == 1, "Exitcode " + Id(l.Ergebnis.Code) + ": " + l.Ergebnis.Alles);

            Dictionary<string, string> rot = l.Zeile(1008, 10576);
            Assert.Equal("Fehler", rot["Ampel"]);
            Assert.Equal("Datenfehler", rot["Vermerk"]);
            Assert.Equal("BauweiseUnplausibel", rot["Neu_Fehlercode"]);
            Assert.Contains("U-BW", rot["Regeln"].Split(' '));
            Assert.Contains("P1008/G10576: alt ok, neu FEHLER BauweiseUnplausibel", l.Ergebnis.Ausgabe);

            foreach ((int p, int g) in new[] { (1008, 10577), (1045, 10651) })
            {
                Dictionary<string, string> z = l.Zeile(p, g);
                Assert.True(z["Alt_ok"] == "1" && z["Neu_ok"] == "1", "P" + Id(p) + "/G" + Id(g) + " nicht gerechnet");
            }
        }

        [Fact]
        public void T6_Uebersprungen_mit_Grund_ohne_Ausnahme()
        {
            if (!_v.Vorhanden) return;
            Vorrichtung.Lauf a = _v.LaufA;
            Assert.True(a.Ergebnis.Code == 0, "Exitcode " + Id(a.Ergebnis.Code) + ": " + a.Ergebnis.Alles);

            Dictionary<string, string> p19 = a.Projekt(19);
            Assert.Equal("übersprungen", p19["Status"]);
            Assert.Contains("Klimaregion", p19["Grund"]);
            Dictionary<string, string> p1030 = a.Projekt(1030);
            Assert.Equal("übersprungen", p1030["Status"]);
            Assert.Contains("kein Gebäude", p1030["Grund"]);

            string protokoll = File.ReadAllText(Path.Combine(a.Ziel, "protokoll.txt"), Encoding.UTF8);
            Assert.DoesNotContain("Ausnahme", protokoll);
            Assert.DoesNotContain("Abbruch", protokoll);
        }

        [Fact]
        public void T10_Kein_Schreiben_in_die_Datenbank()
        {
            if (!_v.Vorhanden) return;
            _ = _v.LaufA;
            SqliteConnection.ClearAllPools();
            Assert.Equal(_v.GrundHashVorLaufA, _v.GrundHashNachLaufA);
            Assert.Equal(_v.GrundHashVorLaufA, Werkzeuglauf.Pruefsumme(_v.Grund));
            Assert.False(File.Exists(_v.Grund + "-wal") && new FileInfo(_v.Grund + "-wal").Length > 0);
        }

        [Fact]
        public void T11_Zwei_Laeufe_ergeben_byte_gleiche_Dateien()
        {
            if (!_v.Vorhanden) return;
            Vorrichtung.Lauf a = _v.LaufA, b = _v.LaufB;
            Assert.Equal(0, b.Ergebnis.Code);
            foreach (string datei in new[] { "gebaeude.csv", "gebaeude_monate.csv", "projekte.csv", "bericht.html", "zusammenfassung.md" })
                Assert.True(File.ReadAllBytes(Path.Combine(a.Ziel, datei)).SequenceEqual(File.ReadAllBytes(Path.Combine(b.Ziel, datei))),
                            datei + " ist zwischen zwei Läufen nicht byte-gleich.");

            // Zeitstempel stehen nur im Protokoll.
            Assert.Matches(new Regex(@"^Beginn \d{4}-\d{2}-\d{2} ", RegexOptions.Multiline),
                           File.ReadAllText(Path.Combine(a.Ziel, "protokoll.txt"), Encoding.UTF8));
        }

        [Fact]
        public void T12_Ohne_mit_namen_steht_kein_Name_in_einer_Ausgabe()
        {
            if (!_v.Vorhanden) return;
            Vorrichtung.Lauf a = _v.LaufA;
            HashSet<string> namen = Namenswerte(_v.Grund);
            Assert.True(namen.Count > 10, "Zu wenige Namenswerte gelesen: " + Id(namen.Count));

            var texte = new List<(string Wo, string Text)>();
            foreach (string datei in Directory.GetFiles(a.Ziel, "*", SearchOption.AllDirectories))
                texte.Add((Path.GetFileName(datei), File.ReadAllText(datei, Encoding.UTF8)));
            texte.Add(("Konsole", a.Ergebnis.Alles));

            // Auch Aufnahme und Variante schreiben ein Protokoll.
            string auf = _v.Ausgabe("t12_aufnahme");
            Werkzeuglauf.Ergebnis aufnahme = Werkzeuglauf.Starten("aufnahme", "--quelle", _v.Grund, "--ziel", auf);
            Assert.Equal(0, aufnahme.Code);
            texte.Add(("Aufnahme-Konsole", aufnahme.Alles));
            texte.Add(("Aufnahme-Protokoll", File.ReadAllText(Path.Combine(auf, "protokoll.txt"), Encoding.UTF8)));
            string variante = Path.Combine(auf, "variante.sqlite");
            Werkzeuglauf.Ergebnis v = Werkzeuglauf.Starten("variante", "--db", _v.Grund, "--modell", "VDI6007", "--ziel", variante);
            Assert.Equal(0, v.Code);
            texte.Add(("Varianten-Konsole", v.Alles));
            texte.Add(("Varianten-Protokoll", File.ReadAllText(variante + ".protokoll.txt", Encoding.UTF8)));
            File.Delete(Path.Combine(auf, "aufnahme.sqlite"));
            File.Delete(variante);

            var funde = new List<string>();
            foreach ((string wo, string text) in texte)
            {
                int n = namen.Count(w => text.Contains(w, StringComparison.Ordinal));
                if (n > 0) funde.Add(wo + ": " + Id(n) + " Namenswert(e)");
            }
            Assert.True(funde.Count == 0, "Namen in der Ausgabe: " + string.Join("; ", funde));

            // zusammenfassung.md trägt auch keine ID - außer in der Zeile der Werkzeugversion
            // (Commit-Kennung) stehen dort nur Zählungen und Prozente.
            HashSet<string> ids = Ids(_v.Grund);
            string[] zeilen = File.ReadAllLines(Path.Combine(a.Ziel, "zusammenfassung.md"), Encoding.UTF8)
                                  .Where(z => !z.StartsWith("- Werkzeug:", StringComparison.Ordinal)).ToArray();
            foreach (string z in zeilen)
            {
                Assert.DoesNotMatch(new Regex(@"(Projekt|Gebäude) \d|P\d+/G\d+"), z);
                foreach (Match m in Regex.Matches(z, @"\d+"))
                    Assert.False(ids.Contains(m.Value), "zusammenfassung.md nennt eine ID: " + z);
            }
        }

        /// <summary>Die Namenswerte der Kopie (ab zwei Zeichen) - eigene Lese-SQL der Probe.</summary>
        internal static HashSet<string> Namenswerte(string db)
        {
            var werte = new HashSet<string>(StringComparer.Ordinal);
            using var v = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = db, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            v.Open();
            foreach (string sql in new[] { "SELECT Projektname, Kunde, Bearbeiter, Beschreibung FROM Tab_Projekt",
                                           "SELECT Gebaeudename, Beschreibung FROM Tab_Gebaeude" })
            {
                using SqliteCommand k = v.CreateCommand();
                k.CommandText = sql;
                using SqliteDataReader r = k.ExecuteReader();
                while (r.Read())
                    for (int i = 0; i < r.FieldCount; i++)
                    {
                        if (r.IsDBNull(i)) continue;
                        string w = Convert.ToString(r.GetValue(i), CultureInfo.InvariantCulture);
                        foreach (string f in new[] { w, w.Trim() })
                            if (f.Length >= 2) werte.Add(f);
                    }
            }
            return werte;
        }

        /// <summary>Alle Projekt- und Gebäude-IDs ab vier Stellen.</summary>
        private static HashSet<string> Ids(string db)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            using var v = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = db, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            v.Open();
            foreach (string sql in new[] { "SELECT ID FROM Tab_Projekt", "SELECT ID FROM Tab_Gebaeude" })
            {
                using SqliteCommand k = v.CreateCommand();
                k.CommandText = sql;
                using SqliteDataReader r = k.ExecuteReader();
                while (r.Read())
                {
                    string id = Convert.ToString(r.GetValue(0), CultureInfo.InvariantCulture);
                    if (id.Length >= 4) ids.Add(id);
                }
            }
            return ids;
        }
    }
}
