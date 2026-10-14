using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Nachladen des freien Paketteils</b> (Anwenderentscheid 29.09.2026): Fehlt einer
    /// Datenbank mit Tww-Tabellen die Katalogversion der Brauchwasserparameter, lädt der Kern den
    /// freien Paketteil (<c>Referenzlaeufe/Katalogpaket_frei/</c>, eingebettet in <c>EPOS.Kern</c>)
    /// beim ersten <see cref="ZapfprofilCtrl.Verfuegbar"/> nach — über denselben Einspielweg wie die
    /// Auslieferungsvorlage (<see cref="TwwPaketteilCtrl.Einspielen"/>). Wiederholbar, nie über eine
    /// vorhandene Katalogversion, ohne Tww-Tabellen nichts, ein Fehler benannt und zurückgerollt; die
    /// eingebetteten Ressourcen sind die Dateien des Ordners (Wache).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwPaketteilNachladenTests
    {
        // =================================================================================
        // Die Wache: eine Quelle
        // =================================================================================

        /// <summary>
        /// <b>Die eingebetteten Ressourcen SIND die Dateien des Ordners</b> — gleiche Namen, gleiche
        /// Bytes, keine mehr und keine weniger. Die <c>LIESMICH.md</c> (Entwicklerunterlage) ist nicht
        /// eingebettet, und keine Ressource des Kerns stammt aus den lokalen Normdaten
        /// (<c>Referenzlaeufe/Normzahlen/</c>) oder aus einem A100-Paket.
        /// </summary>
        [Fact]
        public void Die_eingebetteten_Ressourcen_gleichen_den_Dateien_des_Ordners()
        {
            string ordner = Path.Combine(Wurzel(), "Referenzlaeufe", "Katalogpaket_frei");
            Assembly kern = typeof(TwwPaketteilCtrl).Assembly;
            string[] ressourcen = kern.GetManifestResourceNames()
                                      .Where(n => n.StartsWith(TwwPaketteilCtrl.RESSOURCE_PRAEFIX, StringComparison.Ordinal))
                                      .Select(n => n.Substring(TwwPaketteilCtrl.RESSOURCE_PRAEFIX.Length))
                                      .OrderBy(n => n, StringComparer.Ordinal).ToArray();
            string[] dateien = Directory.GetFiles(ordner, "*.csv").Select(Path.GetFileName)
                                        .OrderBy(n => n, StringComparer.Ordinal).ToArray();

            Assert.Equal(TwwPaketteilCtrl.TABELLEN.Length, dateien.Length);
            Assert.Equal(dateien, ressourcen);
            foreach (string d in dateien)
            {
                using Stream s = kern.GetManifestResourceStream(TwwPaketteilCtrl.RESSOURCE_PRAEFIX + d);
                using var m = new MemoryStream();
                s.CopyTo(m);
                Assert.True(m.ToArray().AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(ordner, d))),
                            d + ": die eingebettete Ressource weicht von der Datei im Ordner ab.");
            }
            Assert.DoesNotContain(kern.GetManifestResourceNames(), n => n.EndsWith("LIESMICH.md", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(kern.GetManifestResourceNames(), n => n.Contains("Normzahlen", StringComparison.OrdinalIgnoreCase)
                                                                       || n.Contains("A100", StringComparison.OrdinalIgnoreCase));

            // Der Leser gibt dieselben Texte wie der Leser des Katalogimports aus dem Ordner.
            IReadOnlyList<TwwPaketdatei> eingebettet = TwwPaketteilCtrl.Eingebettet();
            IReadOnlyList<TwwPaketdatei> gelesen = TwwNutzungsartCtrl.PaketLesen(ordner, out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(gelesen.OrderBy(d => d.Name, StringComparer.Ordinal).Select(d => d.Name + "\n" + d.Inhalt),
                         eingebettet.Select(d => d.Name + "\n" + d.Inhalt));
        }

        // =================================================================================
        // Das Nachladen
        // =================================================================================

        /// <summary>
        /// Eine Datenbank mit Tww-Tabellen und leerem Parameterkatalog: Das erste
        /// <see cref="ZapfprofilCtrl.Verfuegbar"/> lädt den freien Paketteil nach — der Generator ist
        /// verfügbar, die Katalogversion ist der Rückfall der Zielkatalogversion
        /// (<see cref="ZapfprofilCtrl.KATALOGVERSION_RUECKFALL"/>: weder der Katalog noch der Paketteil
        /// führt eine), jede Datei steht mit ihrer Zeilenzahl da, jede Kopfzeile
        /// als Auslieferung (<c>AUSLIEFERUNG</c>, <c>ReadOnly</c> 1), jede Nutzungsart mit dem
        /// Vorgabesatz ihrer Gruppe. Der Hinweis steht im Laufprotokoll.
        /// </summary>
        [Fact]
        public void Ohne_Katalogversion_laedt_Verfuegbar_den_freien_Paketteil_nach()
        {
            using var db = new TwwTestdatenbank();
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            Assert.Null(ZapfprofilCtrl.AktuelleKatalogversion());

            ZapfVerfuegbarkeit v = ZapfprofilCtrl.Verfuegbar();

            Assert.True(v.Ja, v.Klartext);
            Assert.Equal(ZapfVerfuegbarkeitsgrund.Verfuegbar, v.Grund);
            Assert.Equal(ZapfprofilCtrl.KATALOGVERSION_RUECKFALL, ZapfprofilCtrl.AktuelleKatalogversion());
            Assert.Equal("PAKETTEIL_NACHGELADEN", v.Nachladen?.Kennung);
            Assert.Equal(ZapfprofilCtrl.KATALOGVERSION_RUECKFALL, v.Nachladen.Werte[0]);
            Assert.Equal(Datenzeilen(TwwSchema.TAB_TWW_PARAMETER_STAMM).Count, v.Nachladen.Werte[1]);
            Assert.Equal(Datenzeilen(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM).Count, v.Nachladen.Werte[2]);
            Assert.Equal(Datenzeilen(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM).Count, v.Nachladen.Werte[3]);
            Assert.Contains(v.Nachladen.Klartext, protokoll.Hinweise);
            Assert.Empty(protokoll.Warnungen);

            foreach (string t in TwwPaketteilCtrl.TABELLEN.Where(t => t != TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM))
                Assert.Equal((long)Datenzeilen(t).Count, Zahl("SELECT COUNT(*) FROM \"" + t + "\""));
            Assert.Equal((long)KategorienSoll(), Zahl("SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + "\""));
            foreach (string t in new[] { TwwSchema.TAB_TWW_PARAMETER_STAMM, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
                                         TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM })
            {
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Status\" <> ? OR \"ReadOnly\" <> 1",
                                      TwwSchema.STATUS_AUSLIEFERUNG));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Katalogversion\" <> ?",
                                      ZapfprofilCtrl.KATALOGVERSION_RUECKFALL));
            }

            // Der Parametersatz der Katalogversion trägt jeden Schlüssel des Pakets.
            Parametersatz ps = ZapfprofilCtrl.Parameter();
            Assert.Equal(ZapfprofilCtrl.KATALOGVERSION_RUECKFALL, ps.Katalogversion);
        }

        /// <summary>
        /// <b>Wiederholbar:</b> Ein zweites <see cref="ZapfprofilCtrl.Verfuegbar"/> und ein zweites
        /// <see cref="TwwPaketteilCtrl.Nachladen"/> ändern nichts — kein Hinweis, keine Zeile mehr,
        /// jede Tww-Tabelle Wert für Wert wie nach dem ersten.
        /// </summary>
        [Fact]
        public void Ein_zweiter_Aufruf_aendert_nichts()
        {
            using var db = new TwwTestdatenbank();
            Assert.True(ZapfprofilCtrl.Verfuegbar().Ja);
            string nachErstem = Fingerabdruck();

            ZapfVerfuegbarkeit zweit = ZapfprofilCtrl.Verfuegbar();
            Assert.True(zweit.Ja);
            Assert.Null(zweit.Nachladen);
            TwwPaketteilNachladen direkt = TwwPaketteilCtrl.Nachladen();
            Assert.False(direkt.Versucht);
            Assert.Null(direkt.Satz);
            Assert.Equal(nachErstem, Fingerabdruck());

            // Auch der Einspielweg selbst fasst eine Datenbank mit Katalogversion nicht an.
            Assert.True(TwwPaketteilCtrl.Einspielen(TwwPaketteilCtrl.Eingebettet(), "Probe", TwwPaketteilCtrl.VORRANG_DATENBANK,
                                                    true, null, out string fehler, out TwwPaketteilZahlen z));
            Assert.Null(fehler);
            Assert.True(z.Uebergangen);
            Assert.Equal(nachErstem, Fingerabdruck());
        }

        /// <summary>
        /// <b>Eine vorhandene Katalogversion bleibt unberührt</b> — auch eine eigene, abweichende: kein
        /// Nachladen, keine Zeile mehr, die Version bleibt die der Datenbank.
        /// </summary>
        [Fact]
        public void Eine_vorhandene_Katalogversion_bleibt_unberuehrt()
        {
            using var db = new TwwTestdatenbank();
            TwwTestdatenbank.ParameterAnlegen("Probe.Eins", 1.0, "EIGEN-7");
            string vorher = Fingerabdruck();

            ZapfVerfuegbarkeit v = ZapfprofilCtrl.Verfuegbar();
            Assert.True(v.Ja);
            Assert.Null(v.Nachladen);
            Assert.Equal("EIGEN-7", ZapfprofilCtrl.AktuelleKatalogversion());
            Assert.False(TwwPaketteilCtrl.Nachladen().Versucht);
            Assert.Equal(vorher, Fingerabdruck());
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\""));
        }

        /// <summary>Ohne Tww-Tabellen geschieht nichts: kein Nachladen, keine Tabelle entsteht.</summary>
        [Fact]
        public void Ohne_Tww_Tabellen_geschieht_nichts()
        {
            using var db = new TwwTestdatenbank(mitTwwSchema: false);

            Assert.False(TwwPaketteilCtrl.Nachladen().Versucht);
            ZapfVerfuegbarkeit v = ZapfprofilCtrl.Verfuegbar();
            Assert.False(v.Ja);
            Assert.Equal(ZapfVerfuegbarkeitsgrund.TabellenFehlen, v.Grund);
            Assert.Null(v.Nachladen);
            foreach (string t in TwwPaketteilCtrl.TABELLEN) Assert.False(DataRepository.TabelleVorhanden(t), t);
        }

        /// <summary>
        /// <b>Ein misslungenes Nachladen ist benannt und ändert nichts:</b> Nimmt die Datenbank eine
        /// Zeile nicht an (hier ein Auslöser, der jedes Einfügen in den Parameterkatalog abweist), rollt
        /// der ganze Paketteil zurück, die Ablehnung der Verfügbarkeit nennt den Grund
        /// (<c>VERFUEGBAR_KEINE_KATALOGVERSION_NACHLADEN</c>), und das Laufprotokoll trägt die Warnung.
        /// </summary>
        [Fact]
        public void Ein_misslungenes_Nachladen_ist_benannt_und_aendert_nichts()
        {
            using var db = new TwwTestdatenbank();
            DataRepository.ExecuteNonQuery("CREATE TRIGGER \"Probe_Abweisen\" BEFORE INSERT ON \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM +
                                           "\" BEGIN SELECT RAISE(ABORT, 'Probe weist ab'); END");
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();

            ZapfVerfuegbarkeit v = ZapfprofilCtrl.Verfuegbar();

            Assert.False(v.Ja);
            Assert.Equal(ZapfVerfuegbarkeitsgrund.KeineKatalogversion, v.Grund);
            Assert.Equal("VERFUEGBAR_KEINE_KATALOGVERSION_NACHLADEN", v.Satz.Kennung);
            Assert.Equal("PAKETTEIL_NACHLADEN_FEHLGESCHLAGEN", v.Nachladen?.Kennung);
            Assert.Contains("Parameterkatalog des Zapfprofils", v.Klartext);
            Assert.Contains("Probe weist ab", v.Klartext);
            Assert.Contains(v.Nachladen.Klartext, protokoll.Warnungen);
            foreach (string t in TwwPaketteilCtrl.TABELLEN)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + t + "\""));
        }

        /// <summary>
        /// <b>Die Testdatenbank mit geleertem Parameterkatalog:</b> Der Paketteil kommt unter
        /// <see cref="ZapfprofilCtrl.KATALOGVERSION_RUECKFALL"/> dazu; jede Zeile, die die Datenbank schon
        /// führt (der Testkatalog unter seiner eigenen Katalogversion, samt seinen Kopien der freien
        /// Zeilen), bleibt Wert für Wert, wie sie war.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_mit_geleertem_Parameterkatalog_laedt_nach_und_behaelt_ihre_Zeilen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_TwwParameter_STAMM"));
            Dictionary<string, long> bis = HoechsteZeilen();
            string vorher = Fingerabdruck(bis);

            ZapfVerfuegbarkeit v = ZapfprofilCtrl.Verfuegbar();

            Assert.True(v.Ja, v.Klartext);
            Assert.Equal("PAKETTEIL_NACHGELADEN", v.Nachladen?.Kennung);
            Assert.Equal(ZapfprofilCtrl.KATALOGVERSION_RUECKFALL, ZapfprofilCtrl.AktuelleKatalogversion());
            Assert.Equal((long)Datenzeilen(TwwSchema.TAB_TWW_PARAMETER_STAMM).Count,
                         Zahl("SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\""));
            Assert.Equal(vorher, Fingerabdruck(bis));
        }

        // =================================================================================
        // Eine Katalogversion für Vorlage, Nachladen und Katalogimport (N38)
        // =================================================================================

        /// <summary>
        /// <b>Der Einspielweg nimmt die Regel des Kerns</b> (<see cref="ZapfprofilCtrl.Zielkatalogversion"/>,
        /// N38) und landet bei derselben Katalogversion wie der Katalogimport desselben Pakets: bei der
        /// des Katalogs, bei leerer Version der jüngsten Parameterzeile beim Rückfall. Der Import läuft
        /// als Prüflauf vorweg und schreibt nichts.
        /// </summary>
        [Theory]
        [InlineData("KAT-7", "KAT-7")]
        [InlineData("", ZapfprofilCtrl.KATALOGVERSION_RUECKFALL)]
        [InlineData("  ", ZapfprofilCtrl.KATALOGVERSION_RUECKFALL)]
        public void Einspielweg_und_Katalogimport_nehmen_dieselbe_Zielkatalogversion(string katalog, string erwartet)
        {
            using var db = new TwwTestdatenbank();
            TwwTestdatenbank.ParameterAnlegen("Probe.Eins", 1.0, katalog);
            Assert.Equal(erwartet, ZapfprofilCtrl.Zielkatalogversion());

            TwwKatalogimportBericht import = TwwNutzungsartCtrl.Importieren(TwwPaketteilCtrl.Eingebettet(), pruefen: true);
            Assert.Null(import.Abbruch);
            Assert.Contains(import.Hinweise, h => h.Kennung == "KATALOGIMPORT_OHNE_KATALOGVERSION" && h.Nennt(erwartet));

            var bericht = new List<string>();
            Assert.True(TwwPaketteilCtrl.Einspielen(TwwPaketteilCtrl.Eingebettet(), "Probe", null, false, bericht.Add,
                                                    out string fehler, out TwwPaketteilZahlen zahlen), fehler);
            Assert.Equal(erwartet, zahlen.Katalogversion);
            Assert.Contains(bericht, z => z.StartsWith("Katalogversion der Paketteil-Zeilen: " + erwartet + " (", StringComparison.Ordinal));
            Assert.Equal(erwartet, ZapfprofilCtrl.AktuelleKatalogversion());
            Assert.Equal((long)Datenzeilen(TwwSchema.TAB_TWW_PARAMETER_STAMM).Count,
                         Zahl("SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\" WHERE \"Katalogversion\" = ? " +
                              "AND \"Schluessel\" <> 'Probe.Eins'", erwartet));
        }

        /// <summary>
        /// <b>Warum das Nachladen nicht den Katalogimport nimmt</b> (Klassenkommentar von
        /// <see cref="TwwPaketteilCtrl"/>): Beide Wege bringen den Paketteil in einen leeren Katalog und
        /// landen bei derselben Katalogversion — aber nur das Nachladen liefert aus (<c>AUSLIEFERUNG</c>,
        /// <c>ReadOnly</c> 1, die Herkunftsarten des Pakets). Der Import legt dieselben Zeilen als
        /// Anwenderzeilen an: <c>IMPORT</c>, <c>ReadOnly</c> 0, aus <c>EIGENKONSTRUKTION</c> und
        /// <c>VERFAHREN</c> wird <c>IMPORT</c>.
        /// </summary>
        [Fact]
        public void Nachladen_und_Katalogimport_treten_derselben_Version_bei_nur_das_Nachladen_liefert_aus()
        {
            string p = "\"" + TwwSchema.TAB_TWW_PARAMETER_STAMM + "\"";
            string n = "\"" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + "\"";
            string nachgeladen;
            using (var db = new TwwTestdatenbank())
            {
                Assert.True(TwwPaketteilCtrl.Nachladen().Erfolg);
                nachgeladen = ZapfprofilCtrl.AktuelleKatalogversion();
                Assert.Equal(ZapfprofilCtrl.KATALOGVERSION_RUECKFALL, nachgeladen);
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + p + " WHERE \"Status\" <> ? OR \"ReadOnly\" <> 1", TwwSchema.STATUS_AUSLIEFERUNG));
                Assert.True(Zahl("SELECT COUNT(*) FROM " + p + " WHERE \"Herkunftsart\" = ?", TwwSchema.HERKUNFT_EIGENKONSTRUKTION) > 0);
                Assert.True(Zahl("SELECT COUNT(*) FROM " + n + " WHERE \"Bedarf_Herkunftsart\" = ?", TwwSchema.HERKUNFT_VERFAHREN) > 0);
            }
            using (var db = new TwwTestdatenbank())
            {
                TwwKatalogimportBericht b = TwwNutzungsartCtrl.Importieren(TwwPaketteilCtrl.Eingebettet());
                Assert.Null(b.Abbruch);
                Assert.Contains(b.Hinweise, h => h.Kennung == "KATALOGIMPORT_OHNE_KATALOGVERSION" && h.Nennt(nachgeladen));
                Assert.Equal(nachgeladen, ZapfprofilCtrl.AktuelleKatalogversion());
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + p + " WHERE \"Status\" <> ? OR \"ReadOnly\" <> 0", TwwSchema.STATUS_IMPORT));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + p + " WHERE \"Herkunftsart\" = ?", TwwSchema.HERKUNFT_EIGENKONSTRUKTION));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + n + " WHERE \"Bedarf_Herkunftsart\" = ?", TwwSchema.HERKUNFT_VERFAHREN));
            }
        }

        // =================================================================================
        // Helfer
        // =================================================================================

        /// <summary>Die Datenzeilen einer Datei des freien Paketteils (ohne Kopf und Leerzeilen).</summary>
        private static List<List<string>> Datenzeilen(string tabelle)
        {
            string text = File.ReadAllText(Path.Combine(Wurzel(), "Referenzlaeufe", "Katalogpaket_frei", tabelle + ".csv"), Encoding.UTF8);
            return TwwPaketteilCtrl.CsvLesen(text).Skip(1).Where(z => !(z.Count == 1 && string.IsNullOrWhiteSpace(z[0]))).ToList();
        }

        /// <summary>Die Kategorien, die der Paketteil an seine eigenen Nutzungsarten bindet: je Nutzungsart der Satz ihrer Gruppe.</summary>
        private static int KategorienSoll()
        {
            List<string> kopfK = TwwPaketteilCtrl.CsvLesen(File.ReadAllText(Path.Combine(Wurzel(), "Referenzlaeufe", "Katalogpaket_frei",
                                                                                          TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM + ".csv"), Encoding.UTF8))[0];
            int spalteGruppe = kopfK.IndexOf(TwwSchema.STEUERSPALTE_GRUPPE);
            List<List<string>> kategorien = Datenzeilen(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM);
            List<string> kopfN = TwwPaketteilCtrl.CsvLesen(File.ReadAllText(Path.Combine(Wurzel(), "Referenzlaeufe", "Katalogpaket_frei",
                                                                                          TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv"), Encoding.UTF8))[0];
            int spalteKalender = kopfN.IndexOf("Kalenderart");
            int summe = 0;
            foreach (List<string> n in Datenzeilen(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM))
            {
                string gruppe = TwwSchema.Kategoriengruppe(long.Parse(n[spalteKalender], CultureInfo.InvariantCulture));
                int mit = kategorien.Count(k => k[spalteGruppe] == gruppe);
                summe += mit > 0 ? mit : kategorien.Count(k => k[spalteGruppe].Length == 0);
            }
            return summe;
        }

        /// <summary>
        /// Jede Tww-Tabelle Zeile für Zeile als Text (in <c>rowid</c>-Folge) — der Vergleich „nichts
        /// geändert". Mit <paramref name="bis"/> nur die Zeilen bis zur genannten <c>rowid</c> je
        /// Tabelle (was die Datenbank vorher führte: Das Nachladen hängt nur an).
        /// </summary>
        private static string Fingerabdruck(IReadOnlyDictionary<string, long> bis = null)
        {
            var sb = new StringBuilder();
            foreach (string t in TwwTabellen())
            {
                DataTable dt = bis == null
                    ? DataRepository.GetDataTable("SELECT * FROM \"" + t + "\" ORDER BY rowid")
                    : DataRepository.GetDataTable("SELECT * FROM \"" + t + "\" WHERE rowid <= ? ORDER BY rowid",
                                                  new DbParam("?", bis[t]));
                sb.Append(t).Append('\n');
                foreach (DataRow r in dt.Rows)
                    sb.Append(string.Join("|", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)))).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Je Tww-Tabelle die höchste <c>rowid</c> (0 = leer).</summary>
        private static Dictionary<string, long> HoechsteZeilen()
            => TwwTabellen().ToDictionary(t => t, t => Zahl("SELECT IFNULL(MAX(rowid), 0) FROM \"" + t + "\""), StringComparer.Ordinal);

        /// <summary>Die vorhandenen Tww-Tabellen, nach Namen geordnet.</summary>
        private static IEnumerable<string> TwwTabellen()
            => TwwSchema.AlleAnweisungen.Select(a => a.Key).Distinct(StringComparer.Ordinal)
                        .Where(DataRepository.TabelleVorhanden).OrderBy(n => n, StringComparer.Ordinal);

        private static long Zahl(string sql, params string[] werte)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, werte.Select(w => new DbParam("?", w)).ToArray()),
                               CultureInfo.InvariantCulture);

        private static string Wurzel([CallerFilePath] string eigeneDatei = "")
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
