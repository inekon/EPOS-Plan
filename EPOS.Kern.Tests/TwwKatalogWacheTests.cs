using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Wächter über den Tww-Katalog der Repo-Testdatenbank</b> (Umsetzungskonzept
    /// Zapfprofilgenerator, Kapitel 6 (b), (c); Stufe Z0, Posten P11).
    ///
    /// <para><b>Warum es ihn braucht.</b> Die Testdatenbank liegt im Repository und ist
    /// Messlatte für Tests, Referenzlauf und CI. Normzahlen, Kennwerte und Referenzprofile
    /// dürfen dort nie stehen; die Auslieferungswerte kommen aus einem Katalogpaket außerhalb
    /// des Repositoriums (<c>Werkzeuge/Auslieferungsvorlage --katalogpaket</c>). Eine Zeile
    /// mit <c>Status = 'AUSLIEFERUNG'</c> in der Testdatenbank wäre genau so ein Wert — er
    /// fiele sonst erst in einer Vorlage oder einem Wiki-Beispiel auf.</para>
    ///
    /// <para><b>Die Fälle:</b> keine Zeile mit <c>Status = 'AUSLIEFERUNG'</c>; jede
    /// Katalogzeile ist <c>EIGEN</c> UND trägt in jeder Provenienzgruppe ein zugelassenes Paar
    /// aus Herkunftsart und Quelle — <c>FIKTIV</c> mit „Testkatalog (fiktiv)“ (Kapitel 6 (b)),
    /// in Nutzungsarten und Tagesgängen dazu <c>FIKTIV</c> mit „VDI 6002 Blatt n (abgeleitet)“
    /// (Anwenderentscheid ZU19: geringfügig abweichende VDI-Werte, Regel in
    /// <c>Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py</c> — Testdaten nach Regel, weder
    /// Eigenkonstruktion noch Normwert), in Bedarfstagen <c>FREI</c> mit der Ecodesign-Verordnung
    /// (EU-Recht), und überall, wo der freie Paketteil (<c>Referenzlaeufe/Katalogpaket_frei/</c>) eine Datei
    /// führt, <c>FREI</c> mit einer Quelle dieser Datei. Eine Tabelle ohne Status — die
    /// Tagesgänge — prüft nur Herkunft und Quelle, eine ohne Herkunftsspalte — der
    /// Tagesgangsatz — nur den Status. Die Katalogversion ist nie leer; das Einspielskript
    /// <c>Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py</c> ist wiederholbar — ein weiterer
    /// Lauf auf einer Arbeitskopie ändert keine Tww-Zeile. Liegen die VDI-Originale lokal, gleicht
    /// kein abgeleiteter Wert seinem Original, und jeder liegt innerhalb ±6 % (lokaler Nachweis;
    /// ohne Ordner schweigt der Fall).</para>
    ///
    /// <para><b>Nur LESEND</b>, über <c>mode=ro&amp;immutable=1</c> wie
    /// <see cref="TestdatenbankSchemastandWacheTests"/> — ohne Beidateien. Fehlt die Datei,
    /// wird nicht geprüft; fehlt Python (<c>py</c> bzw. <c>python3</c>), schweigt der
    /// Skriptfall.</para>
    /// </summary>
    public class TwwKatalogWacheTests
    {
        /// <summary>Die sieben Katalogtabellen; die ersten fünf tragen Status und Katalogversion.</summary>
        private static readonly string[] KOEPFE =
        {
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, TwwSchema.TAB_TWW_PARAMETER_STAMM,
            TwwSchema.TAB_TWW_DIN4708_WERT_STAMM
        };

        private static readonly string[] KINDER =
        {
            TwwSchema.TAB_TWW_TAGESGANG_STAMM, TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM
        };

        /// <summary>Katalogtabellen mit Status, aber ohne eigene Katalogversion: die Zapfkategorien (T2).</summary>
        private static readonly string[] OHNE_VERSION = { TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM };

        /// <summary>Die Quelle der aus VDI 6002 abgeleiteten Zeilen (ZU19); „{0}“ ist das Blatt.</summary>
        private const string QUELLE_VDI_ABGELEITET = "VDI 6002 Blatt {0} (abgeleitet)";

        /// <summary>Die Quelle des Ecodesign-Zapfprofils (EU-Recht, frei).</summary>
        private const string QUELLE_ECODESIGN = "Verordnung (EU) Nr. 814/2013 Anhang III";

        /// <summary>
        /// Die zugelassenen Paare aus Herkunftsart und Quelle je Tabelle: überall der fiktive
        /// Testkatalog; in Nutzungsarten und Tagesgängen die abgeleiteten VDI-Werte (ZU19, Herkunftsart
        /// <c>FIKTIV</c>); wo der freie Paketteil eine Datei führt, deren Paare (Herkunftsart <c>FREI</c>
        /// — Ecodesign-Zapfprofil, Parameter der Stochastik, Zapfkategorien nach Jordan/Vajen).
        /// </summary>
        private static IEnumerable<(string Herkunft, string Quelle)> Zugelassen(string tabelle)
        {
            yield return (TwwSchema.HERKUNFT_FIKTIV, QUELLE_FIKTIV);
            if (tabelle == TwwSchema.TAB_TWW_NUTZUNGSART_STAMM || tabelle == TwwSchema.TAB_TWW_TAGESGANG_STAMM)
                foreach (string blatt in new[] { "1", "2" })
                    yield return (TwwSchema.HERKUNFT_FIKTIV, string.Format(CultureInfo.InvariantCulture, QUELLE_VDI_ABGELEITET, blatt));
            string ordner = PaketteilOrdner();
            if (ordner != null && File.Exists(Path.Combine(ordner, tabelle + ".csv")))
                foreach (var paar in Paketteil(ordner, tabelle).Where(z => z.ContainsKey("Herkunftsart"))
                                                               .Select(z => (z["Herkunftsart"], z["Quelle"])).Distinct())
                    yield return paar;
        }

        private const string SKRIPT = "Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py";

        /// <summary>Die Quelle jeder Zeile des fiktiven Testkatalogs (Kapitel 6 (b)).</summary>
        private const string QUELLE_FIKTIV = "Testkatalog (fiktiv)";

        /// <summary>Frist eines Skriptlaufs; danach wird abgebrochen statt die CI zu blockieren.</summary>
        private const int SKRIPT_FRIST_MS = 120000;

        [Fact]
        public void Keine_Zeile_mit_Status_AUSLIEFERUNG_in_der_Testdatenbank()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            using SqliteConnection c = Oeffnen(pfad);
            var funde = new List<string>();
            foreach (string t in KOEPFE.Concat(OHNE_VERSION))
            {
                Assert.True(TabelleDa(c, t), t + " fehlt in der Testdatenbank (Schemaschritte 103, 115).");
                long n = Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Status\" = $w", TwwSchema.STATUS_AUSLIEFERUNG);
                if (n > 0) funde.Add(t + ": " + n);
            }
            Assert.True(funde.Count == 0,
                "Die Testdatenbank fuehrt Tww-Zeilen mit Status AUSLIEFERUNG (Kapitel 6 (b), (c)) — " +
                "Auslieferungswerte kommen nur aus dem Katalogpaket ausserhalb des Repositoriums:\n" +
                string.Join("\n", funde));
        }

        /// <summary>
        /// Kapitel 6 (b) mit ZU19 verlangt alles ZUGLEICH: <c>Status = 'EIGEN'</c> und in JEDER
        /// Provenienzgruppe ein zugelassenes Paar aus Herkunftsart und Quelle
        /// (<see cref="Zugelassen"/>). Eine Zeile <c>EIGEN</c> mit Herkunftsart <c>VERFAHREN</c>
        /// wäre genau der Weg, auf dem eine echte Normzahl als Anwenderkopie in die Testdatenbank
        /// käme; eine Zeile <c>IMPORT</c> mit <c>FIKTIV</c> ein mitgenommener Fremdkatalog.
        /// </summary>
        [Fact]
        public void Jede_Tww_Katalogzeile_ist_EIGEN_mit_zugelassener_Herkunft()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            using SqliteConnection c = Oeffnen(pfad);
            List<string> funde = Verstoesse(c, out long geprueft);
            Assert.True(funde.Count == 0,
                "Tww-Katalogzeilen der Testdatenbank, die nicht zugleich EIGEN und mit einem zugelassenen Paar aus " +
                "Herkunftsart und Quelle gefuehrt sind (Kapitel 6 (b), ZU19):\n" + string.Join("\n", funde));
            Assert.True(geprueft > 0, "Der Testkatalog fehlt — die Probe waere leer.");
            // Die abgeleiteten VDI-Zeilen und das Ecodesign-Zapfprofil stehen da (ZU19, Stufe Z3).
            Assert.True(Zahl(c, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" WHERE \"Bedarf_Herkunftsart\" = $w " +
                             "AND \"Bedarf_Quelle\" LIKE 'VDI 6002 Blatt _ (abgeleitet)'",
                             TwwSchema.HERKUNFT_FIKTIV) > 0, "Keine abgeleitete VDI-Nutzungsart in der Testdatenbank.");
            Assert.True(Zahl(c, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + "\" WHERE \"Quelle_Art\" = 5 AND \"Quelle\" = $w",
                             QUELLE_ECODESIGN) == 1, "Das Ecodesign-Zapfprofil fehlt in der Testdatenbank.");
        }

        /// <summary>
        /// Gegenprobe auf einer Arbeitskopie: Genau die zwei Faelle, die eine Regel „EIGEN ODER
        /// FIKTIV“ durchliesse, schlagen an — eine Nutzungsart EIGEN mit Herkunftsart
        /// VERFAHREN, ein Parameter IMPORT mit Herkunftsart FIKTIV — und dazu ein Tagesgang
        /// mit fremder Quelle.
        /// </summary>
        [Fact]
        public void Gegenprobe_EIGEN_mit_VERFAHREN_und_IMPORT_mit_FIKTIV_schlagen_an()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string ordner = Path.Combine(Path.GetTempPath(), "epos-twwwache-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string kopie = Path.Combine(ordner, "Kenndaten_Test.sqlite");
                File.Copy(pfad, kopie);
                using (var s = new SqliteConnection(new SqliteConnectionStringBuilder
                       { DataSource = kopie, Pooling = false }.ToString()))
                {
                    s.Open();
                    using SqliteCommand b = s.CreateCommand();
                    b.CommandText =
                        "UPDATE \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" SET \"Bedarf_Herkunftsart\" = $v " +
                        "WHERE \"ID\" = (SELECT MIN(\"ID\") FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\");" +
                        "UPDATE \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\" SET \"Status\" = $i " +
                        "WHERE \"ID\" = (SELECT MIN(\"ID\") FROM \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\");" +
                        "UPDATE \"" + TwwSchema.TAB_TWW_TAGESGANG_STAMM + "\" SET \"Quelle\" = 'Probe' " +
                        "WHERE \"ID\" = (SELECT MIN(\"ID\") FROM \"" + TwwSchema.TAB_TWW_TAGESGANG_STAMM + "\");";
                    b.Parameters.AddWithValue("$v", TwwSchema.HERKUNFT_VERFAHREN);
                    b.Parameters.AddWithValue("$i", TwwSchema.STATUS_IMPORT);
                    Assert.Equal(3, b.ExecuteNonQuery());
                }

                using SqliteConnection c = Oeffnen(kopie);
                List<string> funde = Verstoesse(c, out _);
                Assert.Equal(new[]
                {
                    TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ": 1", TwwSchema.TAB_TWW_PARAMETER_STAMM + ": 1",
                    TwwSchema.TAB_TWW_TAGESGANG_STAMM + ": 1"
                }, funde.ToArray());
            }
            finally
            {
                try { SqliteConnection.ClearAllPools(); } catch { }
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        /// <summary>
        /// Die Tabellen mit Zeilen, die nicht zugleich <c>EIGEN</c> (wo es Status gibt) und in
        /// jeder Provenienzgruppe (Herkunftsart und Quelle mit demselben Präfix) ein zugelassenes
        /// Paar tragen; <paramref name="geprueft"/> zaehlt die gepruefte Zeilen.
        /// </summary>
        private static List<string> Verstoesse(SqliteConnection c, out long geprueft)
        {
            var funde = new List<string>();
            geprueft = 0;
            foreach (string t in KOEPFE.Concat(OHNE_VERSION).Concat(KINDER))
            {
                Assert.True(TabelleDa(c, t), t + " fehlt in der Testdatenbank (Schemaschritte 103, 115).");
                List<string> spalten = Spalten(c, t);
                List<string> praefixe = spalten.Where(s => s == "Herkunftsart" ||
                                                           s.EndsWith("_Herkunftsart", StringComparison.Ordinal))
                                               .Select(s => s.Substring(0, s.Length - "Herkunftsart".Length)).ToList();
                bool mitStatus = spalten.Contains("Status");
                if (!mitStatus && praefixe.Count == 0) continue;   // Ereignisse: am Kopf geprueft
                Assert.All(praefixe, p => Assert.Contains(p + "Quelle", spalten));

                // Verlangt: EIGEN (wo es Status gibt) UND in JEDER Gruppe eines der zugelassenen
                // Paare. Verletzt ist jede Zeile, der eines fehlt (IS statt =, damit NULL ebenfalls
                // verletzt). Die Werte gehen als Parameter.
                var paare = Zugelassen(t).ToList();
                var werte = new List<(string Name, string Wert)> { ("$e", TwwSchema.STATUS_EIGEN) };
                for (int i = 0; i < paare.Count; i++)
                {
                    werte.Add(("$h" + i, paare[i].Herkunft));
                    werte.Add(("$q" + i, paare[i].Quelle));
                }
                var bedingung = new List<string>();
                if (mitStatus) bedingung.Add("\"Status\" IS $e");
                foreach (string p in praefixe)
                    bedingung.Add("(" + string.Join(" OR ", paare.Select((_, i) =>
                        "(\"" + p + "Herkunftsart\" IS $h" + i + " AND \"" + p + "Quelle\" IS $q" + i + ")")) + ")");
                long n = Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\" WHERE NOT (" + string.Join(" AND ", bedingung) + ")",
                              null, werte.ToArray());
                if (n > 0) funde.Add(t + ": " + n);
                geprueft += Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\"", null);
            }
            return funde;
        }

        // =============================================================================
        //  ZU19 — die abgeleiteten VDI-Werte gegen die lokalen Originale
        // =============================================================================

        /// <summary>Hoechste relative Abweichung eines abgeleiteten Werts von seinem Original (ZU19).</summary>
        private const double BAND = 0.06;

        /// <summary>Darunter gilt ein Wert als unveraendert (relativ).</summary>
        private const double GLEICH = 1e-9;

        /// <summary>Die Wärmekapazität, mit der das Einspielskript Liter in kWh umrechnet [Wh/(l·K)].</summary>
        private const double CW = 1.163;

        /// <summary>
        /// <b>Lokaler Nachweis zu ZU19:</b> Kein Katalogwert der Testdatenbank und kein Wert der
        /// abgeleiteten JSON-Datei gleicht seinem VDI-6002-Original (relativ &lt; 1e-9), und jeder
        /// liegt innerhalb ±6 % — Bedarfswerte (über die Bezugstemperaturen der Zeile zurück in
        /// Liter), Monatsfaktoren, Wochenanteile und Stundenanteile der Tagesgänge. Nur, wenn
        /// <c>Referenzlaeufe/Normzahlen/vdi6002/</c> lokal liegt; sonst schweigt der Fall. Die
        /// Meldung nennt Abweichungen, nie einen Absolutwert.
        /// </summary>
        [Fact]
        public void Kein_abgeleiteter_Katalogwert_gleicht_dem_VDI_Original()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            string originale = VdiOriginale();
            if (originale == null) return;                     // lokal nicht beigestellt - schweigen
            LfsZeigerProbe.Sicherstellen(pfad);

            var bedarf = Csv(Path.Combine(originale, "bedarfskennwerte.csv")).ToDictionary(r => r["nutzungsart"]);
            var tage = Csv(Path.Combine(originale, "tagesprofile.csv"));
            var woche = Csv(Path.Combine(originale, "wochenanteile.csv"));
            var saison = Csv(Path.Combine(originale, "saisonfaktoren.csv"));
            var funde = new List<string>();
            int verglichen = 0;

            void Pruefen(string was, double katalog, string original)
            {
                double o = double.Parse(original, CultureInfo.InvariantCulture);
                double a = Math.Abs(katalog / o - 1.0);
                verglichen++;
                if (!(a > GLEICH)) funde.Add(was + ": gleich dem Original");
                else if (a > BAND + GLEICH) funde.Add(was + ": Abweichung " + (a * 100).ToString("0.00", CultureInfo.InvariantCulture) + " %");
            }

            // --- 1. Die Katalogzeilen der Testdatenbank ------------------------------------
            using (SqliteConnection c = Oeffnen(pfad))
            using (SqliteCommand b = c.CreateCommand())
            {
                b.CommandText = "SELECT * FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" WHERE \"Bedarf_Herkunftsart\" = $h " +
                                "AND \"Bedarf_Quelle\" LIKE 'VDI 6002 Blatt _ (abgeleitet)'";
                b.Parameters.AddWithValue("$h", TwwSchema.HERKUNFT_FIKTIV);
                var zeilen = new List<Dictionary<string, object>>();
                using (SqliteDataReader r = b.ExecuteReader())
                    while (r.Read())
                    {
                        var z = new Dictionary<string, object>();
                        for (int i = 0; i < r.FieldCount; i++) z[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                        zeilen.Add(z);
                    }
                Assert.NotEmpty(zeilen);
                foreach (Dictionary<string, object> z in zeilen)
                {
                    string name = Convert.ToString(z["Bezeichner"]);
                    Assert.EndsWith(" (abgeleitet)", name);
                    string art = name.Substring(0, name.Length - " (abgeleitet)".Length);
                    Assert.True(bedarf.ContainsKey(art), "Keine Nutzungsart der Quelle zu " + name);
                    double proLiter = CW * (Convert.ToDouble(z["Bezug_Zapftemperatur"]) - Convert.ToDouble(z["Bezug_Kaltwasser"])) / 1000.0;
                    Pruefen(name + " Bedarf_Niedrig", Convert.ToDouble(z["Bedarf_Niedrig"]) / proLiter, bedarf[art]["minimum"]);
                    Pruefen(name + " Bedarf_Mittel", Convert.ToDouble(z["Bedarf_Mittel"]) / proLiter, bedarf[art]["mittel"]);
                    Pruefen(name + " Bedarf_Hoch", Convert.ToDouble(z["Bedarf_Hoch"]) / proLiter, bedarf[art]["maximum"]);
                    string[] monate = { "jan", "feb", "mar", "apr", "mai", "jun", "jul", "aug", "sep", "okt", "nov", "dez" };
                    for (int m = 0; m < 12; m++)
                        Pruefen(name + " Monat_" + (m + 1), Convert.ToDouble(z["Monat_" + (m + 1)]),
                                saison.Single(s => s["nutzungsart"] == art && s["monat_oder_periode"] == monate[m])["faktor"]);
                    string[] tage7 = { "mo", "di", "mi", "do", "fr", "sa", "so" };
                    for (int w = 0; w < 7; w++)
                        Pruefen(name + " Woche_" + (w + 1), Convert.ToDouble(z["Woche_" + (w + 1)]),
                                woche.Single(s => s["nutzungsart"] == art && s["wochentag"] == tage7[w])["anteil"]);

                    // Die Tagtypen wie im Einspielskript: 1 Werktag, 2 Samstag, 3 und 4 Sonntag; „alle“ für jeden.
                    using SqliteCommand g = c.CreateCommand();
                    g.CommandText = "SELECT * FROM \"" + TwwSchema.TAB_TWW_TAGESGANG_STAMM + "\" WHERE \"ID_Tagesgangsatz\" = $s";
                    g.Parameters.AddWithValue("$s", z["ID_Tagesgangsatz"]);
                    using SqliteDataReader gr = g.ExecuteReader();
                    int gaenge = 0;
                    while (gr.Read())
                    {
                        gaenge++;
                        int tagtyp = Convert.ToInt32(gr["Tagtyp"]);
                        bool alle = tage.Any(s => s["nutzungsart"] == art && s["tagtyp"] == "alle");
                        string quelltyp = alle ? "alle" : tagtyp == 1 ? "werktag" : tagtyp == 2 ? "samstag" : "sonntag";
                        for (int h = 1; h <= 24; h++)
                            Pruefen(name + " Tagtyp " + tagtyp + " Anteil_" + h.ToString("00", CultureInfo.InvariantCulture),
                                    Convert.ToDouble(gr["Anteil_" + h.ToString("00", CultureInfo.InvariantCulture)]),
                                    tage.Single(s => s["nutzungsart"] == art && s["tagtyp"] == quelltyp &&
                                                     s["stunde"] == (h - 1).ToString(CultureInfo.InvariantCulture))["anteil"]);
                    }
                    Assert.Equal(4, gaenge);
                }
            }

            // --- 2. Die committete JSON-Datei, jeder Wert --------------------------------------
            string json = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(pfad)), "Referenzlaeufe", "Skripte",
                                       "tww_katalogwerte_abgeleitet.json");
            using (JsonDocument d = JsonDocument.Parse(File.ReadAllText(json, Encoding.UTF8)))
            {
                JsonElement w = d.RootElement;
                foreach (JsonElement b in w.GetProperty("bedarf").EnumerateArray())
                {
                    string art = b.GetProperty("nutzungsart").GetString();
                    foreach (string s in new[] { "mittel", "minimum", "maximum", "winterspitze", "sommerschwachlast" })
                    {
                        string original = bedarf[art][s];
                        JsonElement v = b.GetProperty(s);
                        if (original.Length == 0) { Assert.Equal(JsonValueKind.Null, v.ValueKind); continue; }
                        Pruefen("JSON " + art + " " + s, v.GetDouble(), original);
                    }
                }
                foreach (Dictionary<string, string> s in tage)
                    Pruefen("JSON Tagesprofil " + s["nutzungsart"] + "/" + s["tagtyp"],
                            w.GetProperty("tagesprofile").GetProperty(s["nutzungsart"]).GetProperty(s["tagtyp"])[int.Parse(s["stunde"], CultureInfo.InvariantCulture)].GetDouble(),
                            s["anteil"]);
                foreach (Dictionary<string, string> s in woche)
                    Pruefen("JSON Woche " + s["nutzungsart"],
                            w.GetProperty("wochenanteile").GetProperty(s["nutzungsart"]).GetProperty(s["wochentag"]).GetDouble(), s["anteil"]);
                foreach (Dictionary<string, string> s in saison)
                    Pruefen("JSON Monat " + s["nutzungsart"],
                            w.GetProperty("saisonfaktoren").GetProperty(s["nutzungsart"]).GetProperty(s["monat_oder_periode"]).GetDouble(), s["faktor"]);
            }

            Assert.True(verglichen > 0, "Nichts verglichen.");
            Assert.True(funde.Count == 0, "Abgeleitete VDI-Werte ausserhalb der Regel (ZU19), " + funde.Count + " von " +
                                          verglichen + ":\n" + string.Join("\n", funde.Take(40)));
        }

        /// <summary>
        /// Der lokale Ordner der VDI-6002-Originale: aufwärts gesucht nach
        /// <c>Referenzlaeufe/Normzahlen/vdi6002/bedarfskennwerte.csv</c> (Muster <see cref="Normzahlen"/>),
        /// ab dem Ordner dieser Datei und ab dem Laufordner, höchstens acht Ebenen; sonst <c>null</c>.
        /// </summary>
        private static string VdiOriginale([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            foreach (string start in new[] { Path.GetDirectoryName(eigeneDatei ?? ""), AppContext.BaseDirectory })
            {
                DirectoryInfo d = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
                for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
                {
                    string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Normzahlen", "vdi6002");
                    if (File.Exists(Path.Combine(kandidat, "bedarfskennwerte.csv"))) return kandidat;
                }
            }
            return null;
        }

        /// <summary>Eine CSV der Originale (Semikolon, Kopfzeile, UTF-8) als Zeilen nach Spaltennamen.</summary>
        private static List<Dictionary<string, string>> Csv(string datei)
        {
            string[] zeilen = File.ReadAllLines(datei, Encoding.UTF8).Where(z => z.Length > 0).ToArray();
            string[] kopf = zeilen[0].TrimStart('\uFEFF').Split(';');
            return zeilen.Skip(1).Select(z =>
            {
                string[] f = z.Split(';');
                var d = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int i = 0; i < kopf.Length; i++) d[kopf[i]] = i < f.Length ? f[i] : "";
                return d;
            }).ToList();
        }

        [Fact]
        public void Die_Katalogversion_ist_nie_leer()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            using SqliteConnection c = Oeffnen(pfad);
            var funde = new List<string>();
            foreach (string t in KOEPFE)
            {
                Assert.True(TabelleDa(c, t), t + " fehlt in der Testdatenbank (Schemaschritt 103).");
                long n = Zahl(c, "SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Katalogversion\" IS NULL OR " +
                                 "trim(\"Katalogversion\") = ''", null);
                if (n > 0) funde.Add(t + ": " + n);
            }
            Assert.True(funde.Count == 0,
                "Tww-Katalogzeilen ohne Katalogversion (natuerlicher Schluessel, Konzept 3.2):\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// Das Einspielskript ist wiederholbar, und die Repo-Testdatenbank steht auf seinem Stand:
        /// Sie trägt schon jeden Parameterschlüssel von Bilanz und Auslegung, und jeder Lauf — auch
        /// der erste auf der Arbeitskopie — legt nichts an, führt nichts nach und lässt jede
        /// Tww-Zeile gleich. Wer den Testkatalog im Skript erweitert, zieht die Repo-Datei im
        /// selben Schritt nach. Ohne Python schweigt der Fall — der Handlauf steht im Kopf des
        /// Skripts.
        /// </summary>
        [Fact]
        public void Das_Einspielskript_ist_wiederholbar()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string skript = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(pfad)), SKRIPT);
            Assert.True(File.Exists(skript), "Das Einspielskript fehlt: " + skript);

            string ordner = Path.Combine(Path.GetTempPath(), "epos-twwwache-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string kopie = Path.Combine(ordner, "Kenndaten_Test.sqlite");
                File.Copy(pfad, kopie);

                // Die Repo-Datei trägt jeden Schlüssel, den Bilanz und Auslegung lesen — so rechnet
                // der Generator auf einer Projektkopie ohne fehlenden Parameter.
                List<string> fehlend = FehlendeParameter(kopie);
                Assert.True(fehlend.Count == 0, "Dem Testkatalog der Repo-Datei fehlen Parameter: " + string.Join(", ", fehlend));
                SqliteConnection.ClearAllPools();
                string vorher = Abbild(kopie);

                // Lauf 0 bis 2: streng 0/0, keine Tww-Zeile verändert.
                for (int lauf = 0; lauf <= 2; lauf++)
                {
                    (int code, string ausgabe)? r = PythonStarten(skript, kopie);
                    if (r == null) return;                                  // kein Python - schweigen
                    Assert.True(r.Value.code == 0, "Lauf " + lauf + " endete mit " + r.Value.code + ":\n" + r.Value.ausgabe);
                    Assert.True(r.Value.ausgabe.Contains("0 Zeile(n) angelegt, 0 nachgefuehrt"),
                        "Lauf " + lauf + " legt an oder führt nach — die Repo-Testdatenbank steht nicht auf dem Stand "
                        + "des Einspielskripts oder das Skript ist nicht wiederholbar:\n" + r.Value.ausgabe);
                    Assert.True(vorher == Abbild(kopie), "Lauf " + lauf + " hat Tww-Zeilen veraendert.");
                }
            }
            finally
            {
                try { SqliteConnection.ClearAllPools(); } catch { }
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        // =============================================================================
        //  Testdatenbank = freier Paketteil (läuft überall, ohne Originale)
        // =============================================================================

        /// <summary>Die Spalten einer Paketzeile, die die Testdatenbank anders führt (Kapitel 6 (c)) oder abbildet.</summary>
        private static readonly string[] NICHT_VERGLICHEN = { "ID", "ID_Bedarfstag", "Status", "ReadOnly" };

        /// <summary>
        /// <b>Die freien Zeilen der Testdatenbank gleichen dem Paketteil</b>
        /// (<c>Referenzlaeufe/Katalogpaket_frei/</c>, dieselben Dateien, die die Auslieferungsvorlage
        /// einspielt), Wert für Wert und in der Anzahl: jeder Parameter und jeder Bedarfstag (samt
        /// Ereignissen) der Dateien steht mit Herkunftsart <c>FREI</c> und der Katalogversion des
        /// Testkatalogs da, jede Nutzungsart trägt genau den Vorgabesatz der Zapfkategorien, und keine
        /// weitere Zeile trägt <c>FREI</c>. Status und ReadOnly folgen der Regel der Testdatenbank
        /// (<c>EIGEN</c>, 0). Ohne Python und ohne die VDI-Originale — die Wache läuft in jeder CI.
        /// </summary>
        [Fact]
        public void Die_freien_Zeilen_der_Testdatenbank_gleichen_dem_Paketteil()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string ordner = PaketteilOrdner();
            Assert.True(ordner != null, "Der freie Paketteil Referenzlaeufe/Katalogpaket_frei fehlt.");

            List<string> funde = PaketteilAbweichungen(pfad, ordner);
            Assert.True(funde.Count == 0, "Die freien Zeilen der Testdatenbank weichen vom Paketteil ab — " +
                                          "Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py nachlaufen lassen:\n" +
                                          string.Join("\n", funde.Take(40)));
        }

        /// <summary>
        /// Gegenprobe auf einer Arbeitskopie: ein geänderter freier Parameter, eine fehlende
        /// Zapfkategorie und ein zusätzliches Ereignis des Ecodesign-Zapfprofils schlagen an.
        /// </summary>
        [Fact]
        public void Gegenprobe_Abweichungen_vom_Paketteil_schlagen_an()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string ordner = PaketteilOrdner();
            Assert.True(ordner != null, "Der freie Paketteil Referenzlaeufe/Katalogpaket_frei fehlt.");

            string arbeit = Path.Combine(Path.GetTempPath(), "epos-twwwache-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(arbeit);
            try
            {
                string kopie = Path.Combine(arbeit, "Kenndaten_Test.sqlite");
                File.Copy(pfad, kopie);
                using (var s = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = kopie, Pooling = false }.ToString()))
                {
                    s.Open();
                    using SqliteCommand b = s.CreateCommand();
                    b.CommandText =
                        "UPDATE \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\" SET \"Wert\" = \"Wert\" + 1 WHERE \"ID\" = " +
                        "(SELECT MIN(\"ID\") FROM \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\" WHERE \"Herkunftsart\" = 'FREI');" +
                        "DELETE FROM \"" + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + "\" WHERE \"ID\" = " +
                        "(SELECT MIN(\"ID\") FROM \"" + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + "\");" +
                        "INSERT INTO \"" + TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM + "\" (\"ID_Bedarfstag\", \"Minute_Beginn\", " +
                        "\"Dauer_min\", \"Energie_Kwh\", \"Reihenfolge\") SELECT \"ID\", 1300, 1, 0.1, 99 FROM \"" +
                        TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + "\" WHERE \"Herkunftsart\" = 'FREI';";
                    Assert.Equal(3, b.ExecuteNonQuery());
                }
                List<string> funde = PaketteilAbweichungen(kopie, ordner);
                Assert.Contains(funde, f => f.StartsWith(TwwSchema.TAB_TWW_PARAMETER_STAMM + " \"", StringComparison.Ordinal)
                                            && f.EndsWith(": Wert weicht ab", StringComparison.Ordinal));
                Assert.Contains(funde, f => f.Contains(" Zapfkategorien statt "));
                Assert.Contains(funde, f => f.Contains(" Ereignisse statt "));
            }
            finally
            {
                try { SqliteConnection.ClearAllPools(); } catch { }
                try { Directory.Delete(arbeit, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        /// <summary>Die Abweichungen der freien Zeilen einer Datenbank vom Paketteil (leer = gleich).</summary>
        private static List<string> PaketteilAbweichungen(string pfad, string ordner)
        {
            using SqliteConnection c = Oeffnen(pfad);
            var funde = new List<string>();

            // --- Parameter und Bedarfstage: je Zeile der Datei die Zeile der Testdatenbank ------
            foreach ((string tabelle, string schluessel) in new[]
                     {
                         (TwwSchema.TAB_TWW_PARAMETER_STAMM, "Schluessel"), (TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, "Bezeichner")
                     })
            {
                List<Dictionary<string, string>> soll = Paketteil(ordner, tabelle);
                Assert.NotEmpty(soll);
                foreach (Dictionary<string, string> z in soll)
                {
                    List<Dictionary<string, object>> ist = Zeilen(c, "SELECT * FROM \"" + tabelle + "\" WHERE \"" + schluessel +
                                                                     "\" = $w AND \"Katalogversion\" = 'TEST-1'", z[schluessel]);
                    if (ist.Count != 1) { funde.Add(tabelle + " \"" + z[schluessel] + "\": " + ist.Count + " Zeile(n)"); continue; }
                    Vergleichen(tabelle + " \"" + z[schluessel] + "\"", z, ist[0], funde);
                    if (Convert.ToString(ist[0]["Status"]) != TwwSchema.STATUS_EIGEN || Convert.ToInt64(ist[0]["ReadOnly"]) != 0)
                        funde.Add(tabelle + " \"" + z[schluessel] + "\": nicht EIGEN/ReadOnly 0");

                    if (tabelle != TwwSchema.TAB_TWW_BEDARFSTAG_STAMM) continue;
                    List<Dictionary<string, string>> e = Paketteil(ordner, TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM)
                        .Where(x => x["ID_Bedarfstag"] == z["ID"]).ToList();
                    List<Dictionary<string, object>> ei = Zeilen(c, "SELECT * FROM \"" + TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM +
                                                                    "\" WHERE \"ID_Bedarfstag\" = $w ORDER BY \"Reihenfolge\", \"ID\"",
                                                                    Convert.ToString(ist[0]["ID"], CultureInfo.InvariantCulture));
                    if (e.Count != ei.Count) funde.Add(z[schluessel] + ": " + ei.Count + " Ereignisse statt " + e.Count);
                    else for (int i = 0; i < e.Count; i++) Vergleichen(z[schluessel] + " Ereignis " + (i + 1), e[i], ei[i], funde);
                }
                long frei = Zahl(c, "SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"Herkunftsart\" = $w", TwwSchema.HERKUNFT_FREI);
                if (frei != soll.Count) funde.Add(tabelle + ": " + frei + " Zeile(n) FREI statt " + soll.Count);
            }

            // --- Zapfkategorien: der Vorgabesatz an jeder Nutzungsart, sonst nichts ----------------
            List<Dictionary<string, string>> satz = Paketteil(ordner, TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM);
            Assert.NotEmpty(satz);
            List<Dictionary<string, object>> arten = Zeilen(c, "SELECT \"ID\", \"Bezeichner\" FROM \"" +
                                                                TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" ORDER BY \"ID\"", null);
            Assert.NotEmpty(arten);
            foreach (Dictionary<string, object> a in arten)
            {
                List<Dictionary<string, object>> k = Zeilen(c, "SELECT * FROM \"" + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM +
                                                               "\" WHERE \"ID_Nutzungsart\" = $w ORDER BY \"Reihenfolge\", \"ID\"",
                                                               Convert.ToString(a["ID"], CultureInfo.InvariantCulture));
                string art = Convert.ToString(a["Bezeichner"]);
                if (k.Count != satz.Count) { funde.Add(art + ": " + k.Count + " Zapfkategorien statt " + satz.Count); continue; }
                for (int i = 0; i < satz.Count; i++) Vergleichen(art + " Kategorie " + (i + 1), satz[i], k[i], funde);
            }
            long alle = Zahl(c, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + "\"", null);
            if (alle != (long)arten.Count * satz.Count)
                funde.Add(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + ": " + alle + " Zeile(n) statt " + arten.Count * satz.Count);
            return funde;
        }

        /// <summary>
        /// Vergleicht jede Spalte einer Paketzeile (ausser <see cref="NICHT_VERGLICHEN"/>) mit der
        /// Datenbankzeile: Zahlen als Zahl (relativ 1e-12), leer gegen NULL, sonst Text.
        /// </summary>
        private static void Vergleichen(string was, Dictionary<string, string> soll, Dictionary<string, object> ist, List<string> funde)
        {
            foreach (KeyValuePair<string, string> s in soll)
            {
                if (NICHT_VERGLICHEN.Contains(s.Key)) continue;
                if (!ist.TryGetValue(s.Key, out object w)) { funde.Add(was + ": Spalte " + s.Key + " fehlt"); continue; }
                bool gleich;
                if (s.Value.Length == 0) gleich = w == null;
                else if (w is double || w is long)
                {
                    double d = Convert.ToDouble(w, CultureInfo.InvariantCulture);
                    gleich = double.TryParse(s.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double e)
                             && Math.Abs(d - e) <= 1e-12 * Math.Max(1.0, Math.Abs(e));
                }
                else gleich = string.Equals(Convert.ToString(w, CultureInfo.InvariantCulture), s.Value, StringComparison.Ordinal);
                if (!gleich) funde.Add(was + ": " + s.Key + " weicht ab");
            }
        }

        /// <summary>Die Zeilen einer Abfrage als dict Spaltenname → Wert (NULL = null); <c>$w</c> optional.</summary>
        private static List<Dictionary<string, object>> Zeilen(SqliteConnection c, string sql, string wert)
        {
            using SqliteCommand b = c.CreateCommand();
            b.CommandText = sql;
            if (wert != null) b.Parameters.AddWithValue("$w", wert);
            var liste = new List<Dictionary<string, object>>();
            using SqliteDataReader r = b.ExecuteReader();
            while (r.Read())
            {
                var z = new Dictionary<string, object>(StringComparer.Ordinal);
                for (int i = 0; i < r.FieldCount; i++) z[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
                liste.Add(z);
            }
            return liste;
        }

        /// <summary>Der Ordner des freien Paketteils neben der Testdatenbank; <c>null</c> ohne Testdatenbank oder Ordner.</summary>
        private static string PaketteilOrdner()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return null;
            string ordner = Path.Combine(Path.GetDirectoryName(pfad), "Katalogpaket_frei");
            return Directory.Exists(ordner) ? ordner : null;
        }

        /// <summary>
        /// Eine Datei des Paketteils (Semikolon, Kopfzeile, UTF-8) als Zeilen nach Spaltennamen; die
        /// Dateien führen weder Trenner noch Anführungszeichen in Feldern.
        /// </summary>
        private static List<Dictionary<string, string>> Paketteil(string ordner, string tabelle)
        {
            string[] zeilen = File.ReadAllLines(Path.Combine(ordner, tabelle + ".csv"), Encoding.UTF8)
                                  .Where(z => z.Trim().Length > 0).ToArray();
            string[] kopf = zeilen[0].TrimStart('\uFEFF').Split(';');
            return zeilen.Skip(1).Select(z =>
            {
                string[] f = z.Split(';');
                Assert.Equal(kopf.Length, f.Length);
                var d = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int i = 0; i < kopf.Length; i++) d[kopf[i]] = f[i];
                return d;
            }).ToList();
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        /// <summary>Alle Tww-Katalogzeilen als Text, nach Tabelle und Id geordnet.</summary>
        private static string Abbild(string datei)
        {
            var sb = new StringBuilder();
            using var c = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = datei, Mode = SqliteOpenMode.ReadOnly, Pooling = false
            }.ToString());
            c.Open();
            foreach (string t in KOEPFE.Concat(OHNE_VERSION).Concat(KINDER))
            {
                using SqliteCommand b = c.CreateCommand();
                b.CommandText = "SELECT * FROM \"" + t + "\" ORDER BY \"ID\"";
                using SqliteDataReader r = b.ExecuteReader();
                while (r.Read())
                {
                    sb.Append(t);
                    for (int i = 0; i < r.FieldCount; i++)
                        sb.Append('|').Append(r.IsDBNull(i) ? "NULL"
                            : Convert.ToString(r.GetValue(i), System.Globalization.CultureInfo.InvariantCulture));
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Die Parameterschlüssel von Bilanz, Auslegung und Stochastik, die der Datei fehlen
        /// (Präfixe ausgenommen; das Quantil der Stochastik je Perzentil der Wertemenge).
        /// </summary>
        private static List<string> FehlendeParameter(string datei)
        {
            var fehlend = new List<string>();
            using SqliteConnection c = Oeffnen(datei);
            var schluessel = new List<string>();
            foreach (Type t in new[] { typeof(ZapfParameter), typeof(ZapfAuslegungParameter), typeof(ZapfStochastikParameter) })
                foreach (System.Reflection.FieldInfo f in t.GetFields(System.Reflection.BindingFlags.Static
                             | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
                {
                    if (!f.IsLiteral || f.FieldType != typeof(string)) continue;
                    string s = (string)f.GetRawConstantValue();
                    if (s.EndsWith(".", StringComparison.Ordinal)) continue;   // Präfix
                    if (s == ZapfStochastikParameter.QUANTIL)
                        schluessel.AddRange(TwwSchema.Perzentile.Select(p => s + p.ToString(CultureInfo.InvariantCulture)));
                    else schluessel.Add(s);
                }
            foreach (string s in schluessel)
                if (Zahl(c, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\" WHERE \"Schluessel\" = $w", s) == 0)
                    fehlend.Add(s);
            return fehlend;
        }

        /// <summary>
        /// Startet das Skript über <c>py</c> (Windows-Starter) oder <c>python3</c>;
        /// <c>null</c>, wenn keines von beiden startet.
        /// </summary>
        private static (int, string)? PythonStarten(string skript, string datenbank)
        {
            foreach (string programm in new[] { "py", "python3" })
            {
                var start = new ProcessStartInfo
                {
                    FileName = programm,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                start.ArgumentList.Add(skript);
                start.ArgumentList.Add(datenbank);
                start.Environment["PYTHONIOENCODING"] = "utf-8";
                try
                {
                    using Process p = Process.Start(start);
                    if (p == null) continue;
                    // Beide Kanaele NEBENEINANDER lesen: Nacheinander blockierten Prozess und
                    // Test sich gegenseitig, sobald stderr den Pipe-Puffer fuellt.
                    var aus = p.StandardOutput.ReadToEndAsync();
                    var fehler = p.StandardError.ReadToEndAsync();
                    if (!p.WaitForExit(SKRIPT_FRIST_MS))
                    {
                        try { p.Kill(entireProcessTree: true); } catch { /* schon beendet */ }
                        Assert.Fail("Das Einspielskript lief laenger als " + (SKRIPT_FRIST_MS / 1000) +
                                    " s und wurde abgebrochen: " + skript);
                    }
                    p.WaitForExit();   // leert die asynchronen Leser
                    return (p.ExitCode, aus.GetAwaiter().GetResult() + fehler.GetAwaiter().GetResult());
                }
                catch (Win32Exception) { /* Programm nicht vorhanden - naechstes */ }
            }
            return null;
        }

        private static SqliteConnection Oeffnen(string pfad)
        {
            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            c.Open();
            return c;
        }

        private static bool TabelleDa(SqliteConnection c, string tabelle) =>
            Zahl(c, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $w", tabelle) > 0;

        private static List<string> Spalten(SqliteConnection c, string tabelle)
        {
            var l = new List<string>();
            using SqliteCommand b = c.CreateCommand();
            b.CommandText = "SELECT name FROM pragma_table_info($w)";
            b.Parameters.AddWithValue("$w", tabelle);
            using SqliteDataReader r = b.ExecuteReader();
            while (r.Read()) l.Add(r.GetString(0));
            return l;
        }

        private static long Zahl(SqliteConnection c, string sql, string wert, params (string Name, string Wert)[] weitere)
        {
            using SqliteCommand b = c.CreateCommand();
            b.CommandText = sql;
            if (wert != null) b.Parameters.AddWithValue("$w", wert);
            foreach (var (name, w) in weitere)
                if (sql.Contains(name)) b.Parameters.AddWithValue(name, w);
            object o = b.ExecuteScalar();
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o);
        }

        /// <summary>Die Repo-Testdatenbank wie in <see cref="TestdatenbankSchemastandWacheTests"/>; sonst <c>null</c>.</summary>
        private static string Testdatenbank([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            string wurzel = null;
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string kandidat = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (kandidat != null && File.Exists(Path.Combine(kandidat, "WP-Plan.sln"))) wurzel = kandidat;
            }
            if (wurzel == null)
            {
                var d = new DirectoryInfo(AppContext.BaseDirectory);
                while (d != null && wurzel == null)
                {
                    if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) wurzel = d.FullName;
                    d = d.Parent;
                }
            }
            if (wurzel == null) return null;
            string datei = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            return File.Exists(datei) ? datei : null;
        }
    }
}
