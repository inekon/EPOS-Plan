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
    /// in Nutzungsarten und Tagesgängen dazu <c>VERFAHREN</c> mit „abgeleitet aus VDI 6002 Blatt n“
    /// (Anwenderentscheide ZU19 und ZU20: geringfügig abweichende VDI-Werte, Regel in
    /// <c>Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py</c> — aus einem Verfahren gerechnet,
    /// weder Normwert noch Eigenkonstruktion noch eine frei verfügbare Quelle), in Bedarfstagen
    /// <c>FREI</c> mit der Ecodesign-Verordnung
    /// (EU-Recht), und überall, wo der freie Paketteil (<c>Referenzlaeufe/Katalogpaket_frei/</c>) eine Datei
    /// führt, <c>FREI</c> mit einer Quelle dieser Datei. Eine Tabelle ohne Status — die
    /// Tagesgänge — prüft nur Herkunft und Quelle, eine ohne Herkunftsspalte — der
    /// Tagesgangsatz — nur den Status. Die Katalogversion ist nie leer; das Einspielskript
    /// <c>Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py</c> ist wiederholbar — ein weiterer
    /// Lauf auf einer Arbeitskopie ändert keine Tww-Zeile. Liegen die VDI-Originale lokal, gleicht
    /// kein abgeleiteter Wert seinem Original — außer einer Null, die multiplikativ nicht abzuleiten
    /// ist und unverändert bleibt —, und jeder liegt innerhalb ±6 % (lokaler Nachweis;
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
        private const string QUELLE_VDI_ABGELEITET = "abgeleitet aus VDI 6002 Blatt {0}";

        /// <summary>Die Quelle des Ecodesign-Zapfprofils (EU-Recht, frei).</summary>
        private const string QUELLE_ECODESIGN = "Verordnung (EU) Nr. 814/2013 Anhang III";

        /// <summary>
        /// <b>Die Formsetzung des Katalogausbaus (Stufe Z5, ZU21):</b> Nutzungsarten, deren
        /// Tagesgänge, Wochenanteile und Monatsfaktoren das Einspielskript von einer ANDEREN
        /// Nutzungsart der Quelle nimmt, weil die Richtlinie ihnen keine gibt — das Ein- und
        /// Zweifamilienhaus nimmt die des großen Wohngebäudes (derselbe Kalender „Wohnen"). Kein
        /// neuer Zahlenwert: Die Werte sind dieselben abgeleiteten.
        /// </summary>
        private static readonly Dictionary<string, string> FORMQUELLE = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Ein- und Zweifamilienhaus", "Wohnen groß" }
        };

        /// <summary>Die Nutzungsart, deren Formen für <paramref name="art"/> gelten (<see cref="FORMQUELLE"/>).</summary>
        private static string Formquelle(string art) => FORMQUELLE.TryGetValue(art, out string f) ? f : art;

        /// <summary>
        /// Die zugelassenen Paare aus Herkunftsart und Quelle je Tabelle: überall der fiktive
        /// Testkatalog; in Nutzungsarten und Tagesgängen die abgeleiteten VDI-Werte (ZU19/ZU20, Herkunftsart
        /// <c>VERFAHREN</c>); wo der freie Paketteil eine Datei führt, deren Paare (Herkunftsart <c>FREI</c>
        /// — Ecodesign-Zapfprofil, Parameter der Stochastik, Zapfkategorien nach Jordan/Vajen —, dazu
        /// <c>EIGENKONSTRUKTION</c> für die INEKON-Setzungen und die Nutzungsart „Hotel (aus Messung)"),
        /// je Provenienzgruppe der Datei (<c>Herkunftsart</c> bzw. <c>Bedarf_</c>, <c>Jahresgang_</c>,
        /// <c>Wochengang_Herkunftsart</c> mit der Quelle derselben Gruppe).
        /// </summary>
        private static IEnumerable<(string Herkunft, string Quelle)> Zugelassen(string tabelle)
        {
            yield return (TwwSchema.HERKUNFT_FIKTIV, QUELLE_FIKTIV);
            if (tabelle == TwwSchema.TAB_TWW_NUTZUNGSART_STAMM || tabelle == TwwSchema.TAB_TWW_TAGESGANG_STAMM)
                foreach (string blatt in new[] { "1", "2" })
                    yield return (TwwSchema.HERKUNFT_VERFAHREN, string.Format(CultureInfo.InvariantCulture, QUELLE_VDI_ABGELEITET, blatt));
            string ordner = PaketteilOrdner();
            if (ordner != null && File.Exists(Path.Combine(ordner, tabelle + ".csv")))
                foreach (var paar in Paketteil(ordner, tabelle)
                             .SelectMany(z => z.Keys.Where(k => k.EndsWith("Herkunftsart", StringComparison.Ordinal))
                                               .Select(k => k.Substring(0, k.Length - "Herkunftsart".Length))
                                               .Where(p => z.ContainsKey(p + "Quelle"))
                                               .Select(p => (z[p + "Herkunftsart"], z[p + "Quelle"])))
                             .Distinct())
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
        /// (<see cref="Zugelassen"/>). Entscheidend ist das PAAR: Eine Zeile <c>EIGEN</c> mit
        /// Herkunftsart <c>VERFAHREN</c> zu einer anderen Quelle als den abgeleiteten VDI-Werten wäre
        /// genau der Weg, auf dem eine echte Normzahl als Anwenderkopie in die Testdatenbank käme;
        /// eine Zeile <c>IMPORT</c> mit <c>FIKTIV</c> ein mitgenommener Fremdkatalog.
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
            // Die abgeleiteten VDI-Zeilen und die neun Ecodesign-Zapfprofile stehen da (ZU19, Stufe Z3, N26).
            Assert.True(Zahl(c, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" WHERE \"Bedarf_Herkunftsart\" = $w " +
                             "AND \"Bedarf_Quelle\" LIKE 'abgeleitet aus VDI 6002 Blatt %'",
                             TwwSchema.HERKUNFT_VERFAHREN) > 0, "Keine abgeleitete VDI-Nutzungsart in der Testdatenbank.");
            Assert.True(Zahl(c, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + "\" WHERE \"Quelle_Art\" = 5 AND \"Quelle\" = $w",
                             QUELLE_ECODESIGN) == 9, "Die neun Ecodesign-Zapfprofile (XXS bis 4XL) fehlen (teils) in der Testdatenbank.");
        }

        /// <summary>
        /// Gegenprobe auf einer Arbeitskopie: Genau die zwei Faelle, die eine Regel „EIGEN ODER
        /// FIKTIV“ durchliesse, schlagen an — eine Nutzungsart EIGEN mit Herkunftsart VERFAHREN zur
        /// Quelle „Testkatalog (fiktiv)“ (das Paar ist keines der zugelassenen), ein Parameter IMPORT
        /// mit Herkunftsart FIKTIV — und dazu ein Tagesgang mit fremder Quelle.
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
        /// abgeleiteten JSON-Datei gleicht seinem VDI-6002-Original (relativ &lt; 1e-9) — eine Null der
        /// Quelle bleibt Null und wird allein darauf geprüft (<see cref="ReellPruefen"/>) —, und jeder
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
                                "AND \"Bedarf_Quelle\" LIKE 'abgeleitet aus VDI 6002 Blatt %'";
                b.Parameters.AddWithValue("$h", TwwSchema.HERKUNFT_VERFAHREN);
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
                    string form = Formquelle(art);
                    Pruefen(name + " Bedarf_Niedrig", Convert.ToDouble(z["Bedarf_Niedrig"]) / proLiter, bedarf[art]["minimum"]);
                    // Ohne Mittelwert in der Quelle ist der mittlere Bedarf die Mitte der abgeleiteten
                    // Spanne (Setzung des Katalogausbaus) — die Wache der JSON-Datei prüft sie.
                    if (bedarf[art]["mittel"].Length > 0)
                        Pruefen(name + " Bedarf_Mittel", Convert.ToDouble(z["Bedarf_Mittel"]) / proLiter, bedarf[art]["mittel"]);
                    Pruefen(name + " Bedarf_Hoch", Convert.ToDouble(z["Bedarf_Hoch"]) / proLiter, bedarf[art]["maximum"]);
                    string[] monate = { "jan", "feb", "mar", "apr", "mai", "jun", "jul", "aug", "sep", "okt", "nov", "dez" };
                    for (int m = 0; m < 12; m++)
                        Pruefen(name + " Monat_" + (m + 1), Convert.ToDouble(z["Monat_" + (m + 1)]),
                                saison.Single(s => s["nutzungsart"] == form && s["monat_oder_periode"] == monate[m])["faktor"]);
                    string[] tage7 = { "mo", "di", "mi", "do", "fr", "sa", "so" };
                    for (int w = 0; w < 7; w++)
                        Pruefen(name + " Woche_" + (w + 1), Convert.ToDouble(z["Woche_" + (w + 1)]),
                                woche.Single(s => s["nutzungsart"] == form && s["wochentag"] == tage7[w])["anteil"]);

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
                        bool alle = tage.Any(s => s["nutzungsart"] == form && s["tagtyp"] == "alle");
                        string quelltyp = alle ? "alle" : tagtyp == 1 ? "werktag" : tagtyp == 2 ? "samstag" : "sonntag";
                        for (int h = 1; h <= 24; h++)
                            Pruefen(name + " Tagtyp " + tagtyp + " Anteil_" + h.ToString("00", CultureInfo.InvariantCulture),
                                    Convert.ToDouble(gr["Anteil_" + h.ToString("00", CultureInfo.InvariantCulture)]),
                                    tage.Single(s => s["nutzungsart"] == form && s["tagtyp"] == quelltyp &&
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

        /// <summary>Höchste Abweichung einer abgeleiteten GANZEN ZAHL in Tagen — mindestens zwei (ZU19, VDI 4655).</summary>
        private const int GANZ_SPANNE = 2;

        /// <summary>
        /// Die Regel ZU19 für einen REELLEN Wert: mehr als <see cref="GLEICH"/> und höchstens
        /// <see cref="BAND"/> vom Original entfernt. Jeder Verstoß hängt an <paramref name="funde"/>
        /// — die Meldung nennt die Abweichung, nie einen Absolutwert.
        /// </summary>
        private static void ReellPruefen(ICollection<string> funde, string was, double abgeleitet, double original)
        {
            if (original == 0.0) { if (abgeleitet != 0.0) funde.Add(was + ": Null nicht Null"); return; }
            double a = Math.Abs(abgeleitet / original - 1.0);
            if (!(a > GLEICH)) funde.Add(was + ": gleich dem Original");
            else if (a > BAND + GLEICH)
                funde.Add(was + ": Abweichung " + (a * 100).ToString("0.00", CultureInfo.InvariantCulture) + " %");
        }

        /// <summary>
        /// Die Regel ZU19 für eine GANZE ZAHL: mindestens einen und höchstens
        /// <c>max(<see cref="GANZ_SPANNE"/>; <see cref="GANZ_BAND"/> · w)</c> Tag(e) entfernt und
        /// nicht negativ.
        /// </summary>
        private static void GanzPruefen(ICollection<string> funde, string was, int abgeleitet, int original)
        {
            int d = Math.Abs(abgeleitet - original);
            int spanne = Math.Max(GANZ_SPANNE, (int)Math.Round(GANZ_BAND * original, MidpointRounding.AwayFromZero));
            if (d == 0) funde.Add(was + ": gleich dem Original");
            else if (d > spanne) funde.Add(was + ": " + d + " Tag(e) Abweichung, erlaubt " + spanne);
            else if (abgeleitet < 0) funde.Add(was + ": negativ");
        }

        /// <summary>
        /// <b>Gegenprobe der Regel ZU19</b> (Nachbesserung Gruppe 1): Die beiden Proben schlagen
        /// an, wenn ein Wert gleich dem Original, zu weit weg oder negativ ist — ohne sie wäre eine
        /// Wache denkbar, die nichts mehr prüft und trotzdem grün ist. Sie läuft <b>immer</b>, auch
        /// ohne die lokalen Originale, und hält damit auch in der CI.
        /// </summary>
        [Fact]
        public void Die_Proben_der_ZU19_Regel_schlagen_an()
        {
            var funde = new List<string>();
            ReellPruefen(funde, "gleich", 1.0, 1.0);
            ReellPruefen(funde, "zu weit", 2.0, 1.0);
            ReellPruefen(funde, "Null", 1.0, 0.0);
            GanzPruefen(funde, "gleich", 40, 40);
            GanzPruefen(funde, "zu weit", 40 + GANZ_SPANNE + 1, 40);        // 40 · 0,06 = 2,4 → Spanne 2
            GanzPruefen(funde, "negativ", -1, 1);                           // in der Spanne, aber negativ
            Assert.Equal(6, funde.Count);
            Assert.Equal(2, funde.Count(f => f.EndsWith("gleich dem Original", StringComparison.Ordinal)));
            Assert.Contains(funde, f => f.Contains("Abweichung 100.00 %", StringComparison.Ordinal));
            Assert.Contains(funde, f => f.EndsWith("Null nicht Null", StringComparison.Ordinal));
            Assert.Contains(funde, f => f.Contains("Tag(e) Abweichung, erlaubt " + GANZ_SPANNE, StringComparison.Ordinal));
            Assert.Contains(funde, f => f.EndsWith("negativ", StringComparison.Ordinal));

            // Was die Regel erfüllt, meldet nichts: am Rand des Bandes und am Rand der Spanne.
            var still = new List<string>();
            ReellPruefen(still, "am Rand", 1.0 + BAND, 1.0);
            ReellPruefen(still, "knapp daneben", 1.0 + 10 * GLEICH, 1.0);
            GanzPruefen(still, "Spanne klein", 40 + GANZ_SPANNE, 40);
            GanzPruefen(still, "Spanne gross", 100 + (int)Math.Round(GANZ_BAND * 100), 100);
            Assert.Empty(still);
        }

        /// <summary>
        /// Relative Spanne der abgeleiteten GANZEN ZAHLEN — <b>dieselbe Zahl wie im Skript</b>
        /// (<c>Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py</c>: <c>max(2; 0,06 · w)</c>).
        /// Sie ist bewusst nicht <see cref="BAND"/>: Das Skript stört REELLE Werte mit 0,059 und
        /// lässt GANZE ZAHLEN bis 0,06 wandern; wer das eine ändert, ändert nicht still das andere.
        /// </summary>
        private const double GANZ_BAND = 0.06;

        /// <summary>
        /// <b>Lokaler Nachweis zu ZU19 (VDI 4655, Stufe Z4b):</b> Kein Wert von
        /// <c>Referenzlaeufe/Skripte/vdi4655_abgeleitet.json</c> gleicht seinem Original.
        /// <list type="bullet">
        /// <item>REELLE Werte (Faktoren der Tagesenergie, Kennwerte): relativ mehr als 1e-9 und
        /// höchstens 6 % entfernt.</item>
        /// <item>GANZE ZAHLEN (Kalendertage je Klimazone): mindestens ein und höchstens
        /// max(2; 6 %) Tag(e) entfernt, jeder ≥ 0, Zeilensumme wieder 365 — innerhalb von 6 %
        /// ließe sich eine Zahl von drei Tagen nicht verändern (Regel des Skripts).</item>
        /// <item>Codes, Zonennamen und Namen der Gebäudevarianten stehen nicht in der
        /// Ausgabe: Die Typtage heißen TT01…, die Varianten variante_1…, von den Zonen bleibt
        /// die Nummer.</item>
        /// </list>
        /// Nur, wenn <c>Referenzlaeufe/Normzahlen/vdi4655/</c> lokal liegt; sonst schweigt der
        /// Fall. Die Meldung nennt Abweichungen, nie einen Absolutwert.
        /// </summary>
        [Fact]
        public void Kein_abgeleiteter_Typtagwert_gleicht_dem_VDI_Original()
        {
            string originale = Vdi4655Originale();
            if (originale == null) return;                      // lokal nicht beigestellt — schweigen
            string json = AbgeleiteteTyptage();
            Assert.NotNull(json);

            var quellTyptage = Csv(Path.Combine(originale, "typtage.csv"));
            var quellAnzahl = Csv(Path.Combine(originale, "typtage_je_zone.csv"));
            var quellFaktoren = Csv(Path.Combine(originale, "f_twe_tt.csv"));
            var quellKennwerte = Csv(Path.Combine(originale, "kennwerte.csv")).ToDictionary(r => r["schluessel"]);

            var funde = new List<string>();
            List<string> ziel = funde;                 // die Gegenprobe am Ende schreibt in eine eigene Liste
            int verglichen = 0;

            void Reell(string was, double abgeleitet, double original)
            {
                verglichen++;
                ReellPruefen(ziel, was, abgeleitet, original);
            }

            void Ganz(string was, int abgeleitet, int original)
            {
                verglichen++;
                GanzPruefen(ziel, was, abgeleitet, original);
            }

            using JsonDocument d = JsonDocument.Parse(File.ReadAllText(json, Encoding.UTF8));
            JsonElement w = d.RootElement;

            // --- 1. Die Codes sind neutral, die Merkmale stimmen ------------------------------
            var codeAlt = new Dictionary<string, string>(StringComparer.Ordinal);
            JsonElement typtage = w.GetProperty("typtage");
            Assert.Equal(quellTyptage.Count, typtage.GetArrayLength());
            int i = 0;
            foreach (JsonElement t in typtage.EnumerateArray())
            {
                string code = t.GetProperty("code").GetString();
                Assert.Matches("^TT[0-9]{2}$", code);
                Assert.DoesNotContain(quellTyptage.Select(r => r["code"]), c => c == code);
                codeAlt[code] = quellTyptage[i]["code"];
                i++;
            }

            // --- 2. Die Varianten sind neutral ----------------------------------------------
            var artAlt = new Dictionary<string, string>(StringComparer.Ordinal);
            var reihenfolge = new List<string>();
            foreach (Dictionary<string, string> r in quellFaktoren)
                if (!reihenfolge.Contains(r["gebaeude"])) reihenfolge.Add(r["gebaeude"]);
            JsonElement arten = w.GetProperty("gebaeudearten");
            Assert.Equal(reihenfolge.Count, arten.GetArrayLength());
            i = 0;
            foreach (JsonElement a in arten.EnumerateArray())
            {
                string neu = a.GetString();
                Assert.Matches("^variante_[0-9]+$", neu);
                artAlt[neu] = reihenfolge[i];
                i++;
            }

            // Die Vollständigkeit je Abschnitt: Was die Quelle führt, steht auch in der Ausgabe.
            int anzahlTage = 0, anzahlFaktoren = 0, anzahlKennwerte = 0;
            var benutzteQuellzeilen = new HashSet<string>(StringComparer.Ordinal);

            // --- 3. Die Kalendertage je Zone: ganze Zahlen, Summe 365 -----------------------
            foreach (JsonProperty art in w.GetProperty("typtage_je_zone").EnumerateObject())
            {
                string alt = artAlt[art.Name];
                string variante = quellAnzahl.Select(r => r["variante"]).Distinct()
                                             .First(v => alt.EndsWith(v, StringComparison.Ordinal));
                foreach (JsonProperty zone in art.Value.EnumerateObject())
                {
                    Dictionary<string, string> quelle = quellAnzahl.Single(
                        r => r["variante"] == variante && r["zone"] == zone.Name);
                    int summe = 0;
                    foreach (JsonProperty tt in zone.Value.EnumerateObject())
                    {
                        int neu = tt.Value.GetInt32();
                        summe += neu;
                        anzahlTage++;
                        Ganz(art.Name + "/" + zone.Name + "/" + tt.Name, neu,
                             int.Parse(quelle[codeAlt[tt.Name]], CultureInfo.InvariantCulture));
                    }
                    if (summe != 365) funde.Add(art.Name + "/" + zone.Name + ": Summe " + summe + " statt 365");
                    benutzteQuellzeilen.Add(variante + "|" + zone.Name);
                }
            }

            // --- 4. Die Faktoren: reell, dürfen negativ sein ------------------------------
            var quellFaktor = quellFaktoren.ToDictionary(r => r["gebaeude"] + "|" + r["zone"] + "|" + r["typtag"],
                                                        r => double.Parse(r["f_twe_tt"], CultureInfo.InvariantCulture),
                                                        StringComparer.Ordinal);
            foreach (JsonProperty art in w.GetProperty("f_twe_tt").EnumerateObject())
                foreach (JsonProperty zone in art.Value.EnumerateObject())
                    foreach (JsonProperty tt in zone.Value.EnumerateObject())
                    {
                        anzahlFaktoren++;
                        Reell(art.Name + "/" + zone.Name + "/" + tt.Name, tt.Value.GetDouble(),
                              quellFaktor[artAlt[art.Name] + "|" + zone.Name + "|" + codeAlt[tt.Name]]);
                    }

            // --- 5. Die Kennwerte -----------------------------------------------------------
            foreach (JsonProperty kw in w.GetProperty("kennwerte").EnumerateObject())
            {
                string alt = kw.Name == "wintergrenze" ? "grenze_uebergang_winter"
                           : kw.Name == "bewoelkung.schwelle" ? "grenze_bewoelkt_bedeckungsgrad"
                           : kw.Name.StartsWith("heizgrenze.", StringComparison.Ordinal)
                               ? "heizgrenztemperatur_" + quellAnzahl.Select(r => r["variante"]).Distinct()
                                     .First(v => artAlt[kw.Name.Substring("heizgrenze.".Length)].EndsWith(v, StringComparison.Ordinal))
                               : kw.Name;
                Assert.True(quellKennwerte.ContainsKey(alt), "Kein Kennwert der Quelle zu " + kw.Name);
                anzahlKennwerte++;
                Reell("Kennwert " + kw.Name, kw.Value.GetDouble(), Kennwertzahl(quellKennwerte[alt]));
            }

            // --- 6. Vollständigkeit je Abschnitt gegen die Quelle ---------------------------
            // Jeder Faktor der Quelle hat seine abgeleitete Zelle; jede (Variante, Zone) der
            // Tabelle der Kalendertage ist benutzt; je Gebäudeart und Zone stehen alle Typtage.
            // So fällt eine Zelle auf, die die Ableitung stillschweigend weggelassen hätte.
            int artenzahl = w.GetProperty("gebaeudearten").GetArrayLength();
            int zonenzahl = w.GetProperty("klimazonen").GetArrayLength();
            int typtagzahl = w.GetProperty("typtage").GetArrayLength();
            Assert.Equal(quellFaktor.Count, anzahlFaktoren);
            Assert.Equal(artenzahl * zonenzahl * typtagzahl, anzahlFaktoren);
            Assert.Equal(artenzahl * zonenzahl * typtagzahl, anzahlTage);
            Assert.Equal(quellAnzahl.Count, benutzteQuellzeilen.Count);
            Assert.Equal(quellAnzahl.Select(r => r["zone"]).Distinct().Count(), zonenzahl);
            Assert.Equal(anzahlTage + anzahlFaktoren + anzahlKennwerte, verglichen);

            Assert.True(verglichen > 0, "Nichts verglichen.");
            Assert.True(funde.Count == 0, "Abgeleitete VDI-4655-Werte ausserhalb der Regel (ZU19), " + funde.Count +
                                          " von " + verglichen + ":\n" + string.Join("\n", funde.Take(40)));

            // Die Gegenprobe der beiden Regeln steht in `Die_Proben_der_ZU19_Regel_schlagen_an` —
            // sie läuft auch ohne die lokalen Originale.
        }

        /// <summary>
        /// Ein Kennwert der Quelle als Zahl. Ein Bruch <c>a/b</c> wird ausgerechnet; trägt er die
        /// Einheit „Achtel", zählt er in Achteln (mal 8) — dieselbe Regel wie im Ableitungsskript.
        /// </summary>
        private static double Kennwertzahl(Dictionary<string, string> zeile)
        {
            string s = (zeile["wert"] ?? "").Trim();
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double wert)) return wert;
            string[] teile = s.Split('/');
            Assert.True(teile.Length == 2, "Kennwert ist weder Zahl noch Bruch.");
            double bruch = double.Parse(teile[0], CultureInfo.InvariantCulture)
                           / double.Parse(teile[1], CultureInfo.InvariantCulture);
            string einheit = zeile.TryGetValue("einheit", out string e) ? (e ?? "") : "";
            return einheit.Trim().StartsWith("Achtel", StringComparison.OrdinalIgnoreCase) ? bruch * 8.0 : bruch;
        }

        /// <summary>Der lokale Ordner der VDI-4655-Originale (Muster <see cref="VdiOriginale"/>); sonst <c>null</c>.</summary>
        private static string Vdi4655Originale([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            foreach (string start in new[] { Path.GetDirectoryName(eigeneDatei ?? ""), AppContext.BaseDirectory })
            {
                DirectoryInfo o = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
                for (int i = 0; i < 8 && o != null; i++, o = o.Parent)
                {
                    string kandidat = Path.Combine(o.FullName, "Referenzlaeufe", "Normzahlen", "vdi4655");
                    if (File.Exists(Path.Combine(kandidat, "typtage.csv"))) return kandidat;
                }
            }
            return null;
        }

        /// <summary>Die committete Datei <c>Referenzlaeufe/Skripte/vdi4655_abgeleitet.json</c>; sonst <c>null</c>.</summary>
        private static string AbgeleiteteTyptage([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            foreach (string start in new[] { Path.GetDirectoryName(eigeneDatei ?? ""), AppContext.BaseDirectory })
            {
                DirectoryInfo o = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
                for (int i = 0; i < 8 && o != null; i++, o = o.Parent)
                {
                    string kandidat = Path.Combine(o.FullName, "Referenzlaeufe", "Skripte", "vdi4655_abgeleitet.json");
                    if (File.Exists(kandidat)) return kandidat;
                }
            }
            return null;
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
        //  Testdatenbank = abgeleitete JSON (läuft überall, ohne Originale)
        // =============================================================================

        /// <summary>
        /// <b>Die abgeleiteten VDI-Zeilen der Testdatenbank gleichen der JSON-Datei</b>
        /// (<c>Referenzlaeufe/Skripte/tww_katalogwerte_abgeleitet.json</c>, ZU19), so wie das
        /// Einspielskript sie umrechnet: Bedarf niedrig/mittel/hoch = Minimum/Mittel/Maximum in Litern
        /// bei den Bezugstemperaturen der Zeile, Monatsfaktoren auf Mittel 1, Wochenanteile und
        /// Tagesgänge auf Summe 1 (Tagtyp 1 Werktag, 2 Samstag, 3 und 4 Sonntag, „alle“ für jeden);
        /// dazu Blatt und Anzahl (je abgeleitete Nutzungsart ein eigener Satz mit vier Tagesgängen).
        /// Relativ 1e-12. Ohne Python und ohne die VDI-Originale — die Wache läuft in jeder CI.
        /// </summary>
        [Fact]
        public void Die_abgeleiteten_Zeilen_der_Testdatenbank_gleichen_der_JSON()
        {
            string pfad = Testdatenbank();
            if (pfad == null) return;
            LfsZeigerProbe.Sicherstellen(pfad);
            string json = Path.Combine(Path.GetDirectoryName(pfad), "Skripte", "tww_katalogwerte_abgeleitet.json");
            Assert.True(File.Exists(json), "Die abgeleitete JSON-Datei fehlt: " + json);

            var funde = new List<string>();
            void Gleich(string was, double ist, double soll)
            {
                if (!(Math.Abs(ist - soll) <= 1e-12 * Math.Max(1.0, Math.Abs(soll)))) funde.Add(was + " weicht ab");
            }
            static double[] Normiert(IEnumerable<double> werte, double ziel)
            {
                double[] w = werte.ToArray();
                double s = w.Sum();
                return w.Select(x => x * ziel / s).ToArray();
            }

            using JsonDocument d = JsonDocument.Parse(File.ReadAllText(json, Encoding.UTF8));
            JsonElement wurzel = d.RootElement;
            var bedarf = wurzel.GetProperty("bedarf").EnumerateArray().ToDictionary(b => b.GetProperty("nutzungsart").GetString());
            string[] monate = { "jan", "feb", "mar", "apr", "mai", "jun", "jul", "aug", "sep", "okt", "nov", "dez" };
            string[] tage = { "mo", "di", "mi", "do", "fr", "sa", "so" };

            using SqliteConnection c = Oeffnen(pfad);
            List<Dictionary<string, object>> arten = Zeilen(c, "SELECT * FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM +
                                                                "\" WHERE \"Bedarf_Quelle\" LIKE 'abgeleitet aus VDI 6002 Blatt %' ORDER BY \"ID\"", null);
            Assert.NotEmpty(arten);
            var saetze = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (Dictionary<string, object> z in arten)
            {
                string name = Convert.ToString(z["Bezeichner"]);
                if (!name.EndsWith(" (abgeleitet)", StringComparison.Ordinal)) { funde.Add(name + ": ohne Zusatz (abgeleitet)"); continue; }
                string art = name.Substring(0, name.Length - " (abgeleitet)".Length);
                if (!bedarf.TryGetValue(art, out JsonElement b)) { funde.Add(name + ": keine Nutzungsart der JSON-Datei"); continue; }
                string quelle = string.Format(CultureInfo.InvariantCulture, QUELLE_VDI_ABGELEITET, b.GetProperty("blatt").GetString());
                foreach (string g in new[] { "Bedarf", "Jahresgang", "Wochengang" })
                    if (Convert.ToString(z[g + "_Quelle"]) != quelle) funde.Add(name + ": " + g + "_Quelle weicht ab");

                double proLiter = CW * (Convert.ToDouble(z["Bezug_Zapftemperatur"]) - Convert.ToDouble(z["Bezug_Kaltwasser"])) / 1000.0;
                string form = Formquelle(art);
                // Ohne Mittelwert in der Quelle die Mitte der abgeleiteten Spanne (Setzung, Stufe Z5).
                double mittel = b.GetProperty("mittel").ValueKind == JsonValueKind.Null
                    ? (b.GetProperty("minimum").GetDouble() + b.GetProperty("maximum").GetDouble()) / 2.0
                    : b.GetProperty("mittel").GetDouble();
                Gleich(name + " Bedarf_Niedrig", Convert.ToDouble(z["Bedarf_Niedrig"]), b.GetProperty("minimum").GetDouble() * proLiter);
                Gleich(name + " Bedarf_Mittel", Convert.ToDouble(z["Bedarf_Mittel"]), mittel * proLiter);
                Gleich(name + " Bedarf_Hoch", Convert.ToDouble(z["Bedarf_Hoch"]), b.GetProperty("maximum").GetDouble() * proLiter);

                double[] m = Normiert(monate.Select(x => wurzel.GetProperty("saisonfaktoren").GetProperty(form).GetProperty(x).GetDouble()), 12.0);
                for (int i = 0; i < 12; i++) Gleich(name + " Monat_" + (i + 1), Convert.ToDouble(z["Monat_" + (i + 1)]), m[i]);
                double[] w = Normiert(tage.Select(x => wurzel.GetProperty("wochenanteile").GetProperty(form).GetProperty(x).GetDouble()), 1.0);
                for (int i = 0; i < 7; i++) Gleich(name + " Woche_" + (i + 1), Convert.ToDouble(z["Woche_" + (i + 1)]), w[i]);

                long satz = Convert.ToInt64(z["ID_Tagesgangsatz"]);
                saetze[art] = satz;                       // die Zuordnung Satz ↔ Nutzungsart prüft der Block unten
                JsonElement profile = wurzel.GetProperty("tagesprofile").GetProperty(form);
                bool alle = profile.TryGetProperty("alle", out _);
                List<Dictionary<string, object>> gaenge = Zeilen(c, "SELECT * FROM \"" + TwwSchema.TAB_TWW_TAGESGANG_STAMM +
                                                                    "\" WHERE \"ID_Tagesgangsatz\" = $w ORDER BY \"Tagtyp\"",
                                                                    satz.ToString(CultureInfo.InvariantCulture));
                if (gaenge.Count != 4) { funde.Add(name + ": " + gaenge.Count + " Tagesgänge statt 4"); continue; }
                foreach (Dictionary<string, object> g in gaenge)
                {
                    int tagtyp = Convert.ToInt32(g["Tagtyp"]);
                    string quelltyp = alle ? "alle" : tagtyp == 1 ? "werktag" : tagtyp == 2 ? "samstag" : "sonntag";
                    if (Convert.ToString(g["Quelle"]) != quelle) funde.Add(name + " Tagtyp " + tagtyp + ": Quelle weicht ab");
                    double[] soll = Normiert(profile.GetProperty(quelltyp).EnumerateArray().Select(x => x.GetDouble()), 1.0);
                    for (int h = 1; h <= 24; h++)
                        Gleich(name + " Tagtyp " + tagtyp + " Anteil_" + h.ToString("00", CultureInfo.InvariantCulture),
                               Convert.ToDouble(g["Anteil_" + h.ToString("00", CultureInfo.InvariantCulture)]), soll[h - 1]);
                }
            }
            // Je abgeleitete Nutzungsart EIN eigener Tagesgangsatz — außer den Nutzungsarten mit
            // geliehenen Formen (FORMQUELLE, Setzung Z5): Sie teilen den Satz ihrer Formquelle.
            foreach (KeyValuePair<string, long> p in saetze)
            {
                string quelle = Formquelle(p.Key);
                if (quelle != p.Key)
                {
                    if (!saetze.TryGetValue(quelle, out long geteilt) || geteilt != p.Value)
                        funde.Add(p.Key + ": nicht der Tagesgangsatz von " + quelle);
                }
                else if (saetze.Any(q => q.Key != p.Key && Formquelle(q.Key) == q.Key && q.Value == p.Value))
                    funde.Add(p.Key + ": Tagesgangsatz mit einer anderen abgeleiteten Nutzungsart geteilt");
            }
            int eigene = saetze.Keys.Count(a => Formquelle(a) == a);
            long abgeleiteteSaetze = Zahl(c, "SELECT COUNT(DISTINCT \"ID_Tagesgangsatz\") FROM \"" + TwwSchema.TAB_TWW_TAGESGANG_STAMM +
                                             "\" WHERE \"Quelle\" LIKE 'abgeleitet aus VDI 6002 Blatt %'", null);
            if (abgeleiteteSaetze != eigene)
                funde.Add("abgeleitete Tagesgangsätze: " + abgeleiteteSaetze + " statt " + eigene);

            Assert.True(funde.Count == 0, "Die abgeleiteten Zeilen der Testdatenbank weichen von der JSON-Datei ab — " +
                                          "Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py nachlaufen lassen:\n" +
                                          string.Join("\n", funde.Take(40)));
        }

        // =============================================================================
        //  Testdatenbank = freier Paketteil (läuft überall, ohne Originale)
        // =============================================================================

        /// <summary>
        /// Die Spalten einer Paketzeile, die die Testdatenbank anders führt (Kapitel 6 (c)) oder
        /// abbildet; <c>Gruppe</c> ist die Steuerspalte des Paketteils und keine Spalte der Tabelle.
        /// </summary>
        private static readonly string[] NICHT_VERGLICHEN =
        {
            "ID", "ID_Bedarfstag", "ID_Tagesgangsatz", "Status", "ReadOnly", TwwSchema.STEUERSPALTE_GRUPPE
        };

        /// <summary>
        /// <b>Der Vorgabesatz einer Gruppe</b> aus den Kategoriezeilen des Paketteils (Stufe Z5):
        /// die Zeilen mit dieser Gruppe in ihrer Reihenfolge, sonst die ohne Gruppe (Rückfall eines
        /// Paketteils ohne Steuerspalte) — dieselbe Regel wie <c>TwwKataloge.Vorgabesatz</c> der
        /// Auslieferungsvorlage und wie das Einspielskript.
        /// </summary>
        private static List<Dictionary<string, string>> Vorgabesatz(List<Dictionary<string, string>> zeilen, string gruppe)
        {
            static string G(Dictionary<string, string> z)
                => z.TryGetValue(TwwSchema.STEUERSPALTE_GRUPPE, out string g) && g.Length > 0 ? g : null;
            var satz = zeilen.Where(z => string.Equals(G(z), gruppe, StringComparison.Ordinal)).ToList();
            return satz.Count > 0 ? satz : zeilen.Where(z => G(z) == null).ToList();
        }

        /// <summary>
        /// <b>Die freien Zeilen der Testdatenbank gleichen dem Paketteil</b>
        /// (<c>Referenzlaeufe/Katalogpaket_frei/</c>, dieselben Dateien, die die Auslieferungsvorlage
        /// einspielt), Wert für Wert und in der Anzahl: jeder Parameter und jeder Bedarfstag (samt
        /// Ereignissen) der Dateien steht mit der Herkunftsart der Datei (<c>FREI</c>, bei den
        /// Setzungen der Speicherauslegung aus der Vorlage V4 <c>EIGENKONSTRUKTION</c>, N28) und der Katalogversion des
        /// Testkatalogs da, jeder Tagesgangsatz mit seinen vier Tagesgängen und jede abgeleitete
        /// Nutzungsart mit Herkunftsart <c>VERFAHREN</c> und dem Satz ihrer Datei (ZU20),
        /// jede Nutzungsart trägt genau den Vorgabesatz der Zapfkategorien
        /// <b>ihrer Gruppe</b> (Wohnen oder Nichtwohnen, Stufe Z5), und keine
        /// weitere Zeile trägt <c>FREI</c> oder <c>EIGENKONSTRUKTION</c>. Status und ReadOnly folgen der Regel der Testdatenbank
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
                        TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + "\" WHERE \"ID\" = (SELECT MIN(\"ID\") FROM \"" +
                        TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + "\" WHERE \"Herkunftsart\" = 'FREI');";
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
                // Die Herkunftsarten des Paketteils: FREI und die INEKON-Setzungen aus V4 (EIGENKONSTRUKTION, N28).
                long frei = Zahl(c, "SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"Herkunftsart\" IN ($w, '" +
                                    TwwSchema.HERKUNFT_EIGENKONSTRUKTION + "')", TwwSchema.HERKUNFT_FREI);
                if (frei != soll.Count) funde.Add(tabelle + ": " + frei + " Zeile(n) FREI/EIGENKONSTRUKTION statt " + soll.Count);
            }

            // --- Tagesgangsätze, Tagesgänge und Nutzungsarten (ZU20) ---------------------------
            // Die ID der Datei ist nur Schlüssel des Pakets; verglichen wird über den Bezeichner,
            // und der Verweis der Nutzungsart wird auf die echte Id des Satzes zurückgerechnet.
            var satzIds = new Dictionary<string, long>(StringComparer.Ordinal);
            List<Dictionary<string, string>> sollSaetze = Paketteil(ordner, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM);
            Assert.NotEmpty(sollSaetze);
            List<Dictionary<string, string>> sollGaenge = Paketteil(ordner, TwwSchema.TAB_TWW_TAGESGANG_STAMM);
            foreach (Dictionary<string, string> z in sollSaetze)
            {
                string was = TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + " \"" + z["Bezeichner"] + "\"";
                List<Dictionary<string, object>> ist = Zeilen(c, "SELECT * FROM \"" + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM +
                                                                 "\" WHERE \"Bezeichner\" = $w AND \"Katalogversion\" = 'TEST-1'", z["Bezeichner"]);
                if (ist.Count != 1) { funde.Add(was + ": " + ist.Count + " Zeile(n)"); continue; }
                Vergleichen(was, z, ist[0], funde);
                if (Convert.ToString(ist[0]["Status"]) != TwwSchema.STATUS_EIGEN || Convert.ToInt64(ist[0]["ReadOnly"]) != 0)
                    funde.Add(was + ": nicht EIGEN/ReadOnly 0");
                long id = Convert.ToInt64(ist[0]["ID"], CultureInfo.InvariantCulture);
                satzIds[z["ID"]] = id;

                List<Dictionary<string, string>> g = sollGaenge.Where(x => x["ID_Tagesgangsatz"] == z["ID"])
                                                               .OrderBy(x => int.Parse(x["Tagtyp"], CultureInfo.InvariantCulture)).ToList();
                List<Dictionary<string, object>> gi = Zeilen(c, "SELECT * FROM \"" + TwwSchema.TAB_TWW_TAGESGANG_STAMM +
                                                                "\" WHERE \"ID_Tagesgangsatz\" = $w ORDER BY \"Tagtyp\"",
                                                                id.ToString(CultureInfo.InvariantCulture));
                if (g.Count != gi.Count) { funde.Add(was + ": " + gi.Count + " Tagesgänge statt " + g.Count); continue; }
                for (int i = 0; i < g.Count; i++) Vergleichen(was + " Tagtyp " + g[i]["Tagtyp"], g[i], gi[i], funde);
            }
            // Die Tagesgänge des Paketteils: VERFAHREN (VDI-6002-Ableitung) und EIGENKONSTRUKTION (Hotel aus Messung).
            long freieGaenge = Zahl(c, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_TAGESGANG_STAMM +
                                       "\" WHERE \"Herkunftsart\" IN ($w, '" + TwwSchema.HERKUNFT_EIGENKONSTRUKTION + "')",
                                       TwwSchema.HERKUNFT_VERFAHREN);
            if (freieGaenge != sollGaenge.Count)
                funde.Add(TwwSchema.TAB_TWW_TAGESGANG_STAMM + ": " + freieGaenge + " Zeile(n) VERFAHREN/EIGENKONSTRUKTION statt " +
                          sollGaenge.Count);

            List<Dictionary<string, string>> sollArten = Paketteil(ordner, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM);
            Assert.NotEmpty(sollArten);
            foreach (Dictionary<string, string> z in sollArten)
            {
                string was = TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " \"" + z["Bezeichner"] + "\"";
                List<Dictionary<string, object>> ist = Zeilen(c, "SELECT * FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM +
                                                                 "\" WHERE \"Bezeichner\" = $w AND \"Katalogversion\" = 'TEST-1'", z["Bezeichner"]);
                if (ist.Count != 1) { funde.Add(was + ": " + ist.Count + " Zeile(n)"); continue; }
                Vergleichen(was, z, ist[0], funde);
                if (Convert.ToString(ist[0]["Status"]) != TwwSchema.STATUS_EIGEN || Convert.ToInt64(ist[0]["ReadOnly"]) != 0)
                    funde.Add(was + ": nicht EIGEN/ReadOnly 0");
                if (!satzIds.TryGetValue(z["ID_Tagesgangsatz"], out long satz) ||
                    satz != Convert.ToInt64(ist[0]["ID_Tagesgangsatz"], CultureInfo.InvariantCulture))
                    funde.Add(was + ": nicht der Tagesgangsatz des Paketteils");
            }
            long freieArten = Zahl(c, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM +
                                      "\" WHERE \"Bedarf_Herkunftsart\" IN ($w, '" + TwwSchema.HERKUNFT_EIGENKONSTRUKTION + "')",
                                      TwwSchema.HERKUNFT_VERFAHREN);
            if (freieArten != sollArten.Count)
                funde.Add(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ": " + freieArten + " Zeile(n) VERFAHREN/EIGENKONSTRUKTION statt " +
                          sollArten.Count);

            // --- Zapfkategorien: der Vorgabesatz IHRER GRUPPE an jeder Nutzungsart, sonst nichts ---
            List<Dictionary<string, string>> zeilen = Paketteil(ordner, TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM);
            Assert.NotEmpty(zeilen);
            List<Dictionary<string, object>> arten = Zeilen(c, "SELECT \"ID\", \"Bezeichner\", \"Kalenderart\" FROM \"" +
                                                                TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\" ORDER BY \"ID\"", null);
            Assert.NotEmpty(arten);
            long erwartet = 0;
            foreach (Dictionary<string, object> a in arten)
            {
                List<Dictionary<string, string>> satz = Vorgabesatz(zeilen,
                    TwwSchema.Kategoriengruppe(Convert.ToInt64(a["Kalenderart"], CultureInfo.InvariantCulture)));
                erwartet += satz.Count;
                List<Dictionary<string, object>> k = Zeilen(c, "SELECT * FROM \"" + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM +
                                                               "\" WHERE \"ID_Nutzungsart\" = $w ORDER BY \"Reihenfolge\", \"ID\"",
                                                               Convert.ToString(a["ID"], CultureInfo.InvariantCulture));
                string art = Convert.ToString(a["Bezeichner"]);
                if (k.Count != satz.Count) { funde.Add(art + ": " + k.Count + " Zapfkategorien statt " + satz.Count); continue; }
                for (int i = 0; i < satz.Count; i++) Vergleichen(art + " Kategorie " + (i + 1), satz[i], k[i], funde);
            }
            long alle = Zahl(c, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + "\"", null);
            if (alle != erwartet)
                funde.Add(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + ": " + alle + " Zeile(n) statt " + erwartet);
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
