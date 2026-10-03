using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Katalogabgleich mit Katalogfassung</b> (Entscheidungsvorlage Modellgrenzen KU1 Stufe 1, Welle M6):
    /// Prüfsumme und Schlüssel, Saat des Schemaschritts, das Paket und der Abgleich mit der Probe
    /// <c>Referenzlaeufe/Importproben/Katalogpaket_Probe.json</c> (Prozesswärme: acht Kopfsätze, acht
    /// Wochenprofile, Fassung 1) — einfügen, aktualisieren, behalten (geändert / entsperrt), ausgelaufen,
    /// zweiter Lauf, Projektkopien, kein Paket, Wiederherstellen.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogabgleichTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static string Repowurzel
        {
            get
            {
                var d = new DirectoryInfo(AppContext.BaseDirectory);
                while (d != null && !File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) d = d.Parent;
                return d?.FullName ?? throw new InvalidOperationException("Repowurzel nicht gefunden");
            }
        }

        private static string Probe =>
            Path.Combine(Repowurzel, "Referenzlaeufe", "Importproben", "Katalogpaket_Probe.json");

        // =============================================================================
        //  Teil 1 - Prüfsumme, Schlüssel, Paket (ohne Datenbank)
        // =============================================================================

        private static Katalogtabelle Pw => Katalogfassung.Tabelle("Tab_Prozesswaerme_STAMM");

        /// <summary>Deterministisch, unabhängig von der Lieferreihenfolge, empfindlich für Spalte und Wert.</summary>
        [Fact]
        public void Pruefsumme_ist_deterministisch_und_spaltenfolgefest()
        {
            var a = new Dictionary<string, object> { ["Bezeichner"] = "A", ["Monat_1"] = 1.5, ["Monat_2"] = 2L, ["Vorlauf"] = null };
            var b = new Dictionary<string, object> { ["Vorlauf"] = null, ["Monat_2"] = 2.0, ["Monat_1"] = 1.5, ["Bezeichner"] = "A" };
            string s = Katalogfassung.Pruefsumme(Pw, a);
            Assert.Equal(64, s.Length);
            Assert.Equal(s, Katalogfassung.Pruefsumme(Pw, a));
            Assert.Equal(s, Katalogfassung.Pruefsumme(Pw, b));                  // Reihenfolge und 2 = 2,0

            var vertauscht = new Dictionary<string, object> { ["Bezeichner"] = "A", ["Monat_1"] = 2L, ["Monat_2"] = 1.5 };
            Assert.NotEqual(s, Katalogfassung.Pruefsumme(Pw, vertauscht));      // Werte in anderer Spalte
            var anders = new Dictionary<string, object>(a) { ["Monat_1"] = 1.5000000001 };
            Assert.NotEqual(s, Katalogfassung.Pruefsumme(Pw, anders));
            var text = new Dictionary<string, object>(a) { ["Monat_2"] = "2" };
            Assert.NotEqual(s, Katalogfassung.Pruefsumme(Pw, text));            // Text ist nicht Zahl
            var fremd = new Dictionary<string, object>(a) { ["Fremdspalte"] = 7 };
            Assert.Equal(s, Katalogfassung.Pruefsumme(Pw, fremd));              // nur die Fachspalten zählen
        }

        /// <summary>Wahrheitswert und Ganzzahl, leer und fehlend sind gleich; die Kultur spielt keine Rolle.</summary>
        [Fact]
        public void Normierung_ist_invariant()
        {
            Assert.Equal(Katalogfassung.Normiert(1L), Katalogfassung.Normiert(true));
            Assert.Equal(Katalogfassung.Normiert(3L), Katalogfassung.Normiert(3.0));
            Assert.Null(Katalogfassung.Normiert(DBNull.Value));
            CultureInfo vorher = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.Equal("n:0.1", Katalogfassung.Normiert(0.1));
            }
            finally { CultureInfo.CurrentCulture = vorher; }
        }

        /// <summary>Kindzeilen gehen sortiert ein — ihre Reihenfolge ändert die Prüfsumme nicht, ihr Wert schon.</summary>
        [Fact]
        public void Kindzeilen_gehen_sortiert_ein()
        {
            Katalogtabelle wp = Katalogfassung.Tabelle("Tab_WP_STAMM");
            var kopf = new Dictionary<string, object> { ["Bezeichner"] = "W" };
            IReadOnlyDictionary<string, object> z1 = new Dictionary<string, object> { ["Vorlauf"] = 35L, ["Temperatur"] = 0L, ["COP"] = 3.0 };
            IReadOnlyDictionary<string, object> z2 = new Dictionary<string, object> { ["Vorlauf"] = 35L, ["Temperatur"] = 10L, ["COP"] = 3.5 };
            string s1 = Katalogfassung.Pruefsumme(wp, kopf, new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>>
                { ["Tab_Kenndaten_STAMM"] = new[] { z1, z2 } });
            string s2 = Katalogfassung.Pruefsumme(wp, kopf, new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>>
                { ["Tab_Kenndaten_STAMM"] = new[] { z2, z1 } });
            string s3 = Katalogfassung.Pruefsumme(wp, kopf, new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>>
                { ["Tab_Kenndaten_STAMM"] = new[] { z1 } });
            Assert.Equal(s1, s2);
            Assert.NotEqual(s1, s3);
        }

        /// <summary>Schlüssel aus Kürzel und bereinigtem Bezeichner, mit Zähler bei Dopplung.</summary>
        [Fact]
        public void Schluessel_aus_Kuerzel_und_Bezeichner_mit_Zaehler()
        {
            Assert.Equal("PW:WASCHEN_BAEDER", Katalogfassung.Schluesselstamm(Pw, "Waschen/Bäder"));
            Assert.Equal("PW:REINIGUNG_SPUELEN_CIP", Katalogfassung.Schluesselstamm(Pw, " Reinigung/Spülen (CIP) "));
            Assert.Equal("PW:SATZ", Katalogfassung.Schluesselstamm(Pw, "§§"));
            var belegt = new HashSet<string> { "PW:A_1", "PW:A_1_2" };
            Assert.Equal("PW:A_1_3", Katalogfassung.Schluessel(Pw, "a-1", belegt.Contains));
            Assert.True(Katalogfassung.Schluesselstamm(Pw, new string('x', 500)).Length <= Katalogfassung.SCHLUESSEL_MAX);
        }

        /// <summary>Die Probe ist ein gültiges Paket in kanonischer Form; eine verfälschte Prüfsumme liest sich nicht.</summary>
        [Fact]
        public void Die_Probe_ist_kanonisch_und_ein_verfaelschter_Wert_wird_abgelehnt()
        {
            byte[] bytes = File.ReadAllBytes(Probe);
            Katalogpaket p = Katalogpaket.AusBytes(bytes);
            Assert.Equal(1, p.Fassung);
            Assert.Equal(new[] { "Tab_Prozesswaerme_STAMM", "Tab_Prozesstyp_STAMM" }, p.Tabellen.Select(t => t.Tabelle).ToArray());
            Assert.Equal(16, p.Satzzahl);
            Assert.Equal(bytes, p.Bytes());

            string text = Encoding.ASCII.GetString(bytes).Replace("\"Monat_1\": ", "\"Monat_1\": 1");
            Assert.Throws<InvalidDataException>(() => Katalogpaket.AusBytes(Encoding.ASCII.GetBytes(text)));
        }

        /// <summary>Das Paket liegt neben der Vorlagendatenbank.</summary>
        [Fact]
        public void Das_Paket_liegt_neben_der_Vorlage()
        {
            string vorlage = Path.Combine("app", "Vorlage", "Kenndaten.sqlite");
            Assert.Equal(Path.Combine("app", "Vorlage", Katalogpaket.DATEINAME), Katalogpaket.Pfad(vorlage));
            Assert.Equal("", Katalogpaket.Pfad(""));
        }

        /// <summary>Nummer, Ziel und Paketstufe des Schemaschritts.</summary>
        [Fact]
        public void Nummer_Ziel_und_Paketstufe()
        {
            Assert.Equal(ErzeugerTeillastSchema.SCHRITT + 1, KatalogfassungSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KatalogfassungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Katalog, Paketanhebung.Stufen.Single(x => x.Nr == KatalogfassungSchema.SCHRITT).Wirkung);
            Assert.Equal(8, Katalogfassung.Stufe1.Count);
            Assert.Equal(Katalogfassung.Stufe1.Count, Katalogfassung.Stufe1.Select(t => t.Kuerzel).Distinct().Count());
        }

        // =============================================================================
        //  Teil 2 - Wache und Saat auf der Testkopie
        // =============================================================================

        /// <summary>
        /// <b>Die Wache der Stufe 1:</b> Jede Tabelle und Kindtabelle steht im Schema, jede Fachspalte
        /// der Liste steht in der Tabelle, und jede Spalte der Tabelle ist entweder Fachspalte oder
        /// Metaspalte. Ein neuer Schemaschritt mit einer neuen Katalogspalte macht sie rot — dann gehört
        /// die Spalte in die Liste (und die Prüfsummen festgeschrieben, wenn sie gefüllt entsteht).
        /// </summary>
        [Fact]
        public void Wache_die_Tabellenliste_der_Stufe1_haelt_das_Schema()
        {
            if (!_db.Vorhanden) return;

            foreach (Katalogtabelle t in Katalogfassung.Stufe1)
            {
                Assert.True(DataRepository.TabelleVorhanden(t.Tabelle), t.Tabelle + " fehlt.");
                List<string> schema = DataRepository.SpaltenVonTabelle(t.Tabelle);
                foreach (string s in t.Fachspalten)
                    Assert.True(schema.Contains(s), t.Tabelle + "." + s + " steht nicht im Schema.");
                foreach (string s in schema)
                    Assert.True(t.Fachspalten.Contains(s) || Katalogfassung.Metaspalten.Contains(s),
                                t.Tabelle + "." + s + " ist weder Fach- noch Metaspalte der Stufe 1.");
                Assert.True(schema.Contains("Bezeichner") && schema.Contains("ReadOnly"));

                foreach (Katalogkind k in t.Kinder)
                {
                    Assert.True(DataRepository.TabelleVorhanden(k.Tabelle), k.Tabelle + " fehlt.");
                    List<string> kind = DataRepository.SpaltenVonTabelle(k.Tabelle);
                    Assert.Contains(k.Fremdschluessel, kind);
                    if (k.Projektspalte != null) Assert.Contains(k.Projektspalte, kind);
                    foreach (string s in kind)
                        Assert.True(k.Fachspalten.Contains(s) || s == "ID" || s == "ReadOnly" || s == k.Fremdschluessel ||
                                    s == k.Projektspalte, k.Tabelle + "." + s + " ist keine Fachspalte der Kindtabelle.");
                }
            }
        }

        /// <summary>
        /// Die Saat: jeder ausgelieferte Satz mit Schlüssel und der Prüfsumme seiner Werte, kein
        /// Anwendersatz mit Schlüssel; ein zweiter Lauf tut nichts, und das Schema steht vollständig.
        /// </summary>
        [Fact]
        public void Saat_ist_wiederholbar_und_laesst_Anwendersaetze_ohne_Schluessel()
        {
            if (!_db.Vorhanden) return;

            Assert.True(KatalogfassungSchema.Vollstaendig());
            Assert.Empty(KatalogfassungSchema.Anweisungen);
            Assert.Equal(0, KatalogSchluesselSaat.OffeneSaetze());
            Dictionary<string, string> vorher = Schluesselbild();
            Assert.Equal(0, KatalogSchluesselSaat.Ausfuehren(null));
            Assert.Equal(vorher, Schluesselbild());

            foreach (Katalogtabelle t in Katalogfassung.Stufe1)
            {
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + t.Tabelle + "\" WHERE \"ReadOnly\" = 0 AND Katalog_Schluessel IS NOT NULL"));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + t.Tabelle + "\" WHERE \"ReadOnly\" = 1 AND " +
                                      "(Katalog_Schluessel IS NULL OR Katalog_Pruefsumme IS NULL)"));
            }
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Prozesswaerme_STAMM WHERE \"ReadOnly\" = 0") > 0);
            Assert.Equal("PW:WASCHEN_BAEDER", Text("SELECT Katalog_Schluessel FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = 'Waschen/Bäder'"));

            // Die gespeicherte Prüfsumme ist die der Werte: Ein Paket aus der Kopie plant nichts.
            KatalogabgleichErgebnis plan = Katalogabgleich.Ausfuehren(Katalogpaket.AusDatenbank(1), nurPruefen: true);
            Assert.True(plan.Ausgefuehrt);
            Assert.False(plan.EtwasZuTun);
            Assert.Equal(0, plan.Behalten);
        }

        /// <summary>Eine Kopie eines ausgelieferten Satzes ist ein Anwendersatz — ohne Schlüssel und Prüfsumme.</summary>
        [Fact]
        public void Eine_Katalogkopie_traegt_keinen_Schluessel()
        {
            if (!_db.Vorhanden) return;

            int id = (int)Zahl("SELECT ID FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = 'Einschicht 5 Tage'");
            Katalogkopie.Ergebnis e = Katalogkopie.Duplizieren("Tab_Prozesswaerme_STAMM", id, "Einschicht Kopie", (IReadOnlyDictionary<string, object>)null);
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Prozesswaerme_STAMM WHERE ID = " + e.Id +
                                  " AND Katalog_Schluessel IS NULL AND Katalog_Pruefsumme IS NULL AND Katalog_Ausgelaufen = 0 AND \"ReadOnly\" = 0"));
        }

        // =============================================================================
        //  Teil 3 - der Abgleich mit der Probe
        // =============================================================================

        /// <summary>
        /// <b>Alle vier Aktionen in einem Lauf</b>, dazu Protokoll, Fassung, Projektkopien und der zweite
        /// Lauf. Vorbereitung an der Kopie: ein Satz fehlt (einfügen), einer trägt einen „alten
        /// Auslieferungsstand" (aktualisieren), einer ist geändert, einer entsperrt (behalten), und der
        /// Probe fehlt ein Satz (ausgelaufen).
        /// </summary>
        [Fact]
        public void Abgleich_mit_der_Probe_einfuegen_aktualisieren_behalten_ausgelaufen()
        {
            if (!_db.Vorhanden) return;

            Katalogpaket probe = Katalogpaket.Lesen(Probe);
            Katalogpaket paket = Katalogpaket.AusBytes(probe.Bytes());
            paket.Tabellen[0].Saetze.RemoveAll(s => s.Schluessel == "PW:RAUMLUFTTECHNIK_HALLE");   // ausgelaufen

            string projektVorher = Projektbild();
            string probenwert = Text("SELECT Monat_1 FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = 'Zweischicht 5 Tage'");

            // einfügen: der Kopfsatz fehlt
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = 'Einschicht 5 Tage'");
            // aktualisieren: ein alter Auslieferungsstand mit seiner Prüfsumme
            DataRepository.ExecuteNonQuery("UPDATE Tab_Prozesswaerme_STAMM SET Monat_1 = 99 WHERE Bezeichner = 'Zweischicht 5 Tage'");
            PruefsummeFestschreiben("Tab_Prozesswaerme_STAMM", "Zweischicht 5 Tage");
            // behalten (geändert): gesperrt, aber der Wert weicht ab
            DataRepository.ExecuteNonQuery("UPDATE Tab_Prozesswaerme_STAMM SET Beschreibung = 'eigene Fassung' WHERE Bezeichner = 'Dreischicht 5 Tage'");
            // behalten (entsperrt): entsperrt und geändert
            DataRepository.ExecuteNonQuery("UPDATE Tab_Prozesswaerme_STAMM SET \"ReadOnly\" = 0, Monat_2 = 1 WHERE Bezeichner = 'Durchlaufbetrieb 7 Tage'");

            // Nur prüfen: plant, schreibt nichts.
            string katalogVorher = Katalogbild();
            KatalogabgleichErgebnis plan = Katalogabgleich.Ausfuehren(paket, nurPruefen: true);
            Assert.True(plan.NurGeprueft);
            Assert.Equal((1, 1, 2, 1), (plan.Neu, plan.Aktualisiert, plan.Behalten, plan.Ausgelaufen));
            Assert.Equal(katalogVorher, Katalogbild());
            Assert.Null(Katalogabgleich.FassungDerDatenbank());

            KatalogabgleichErgebnis e = Katalogabgleich.Ausfuehren(paket, nurPruefen: false);
            Assert.True(e.Ausgefuehrt, e.Meldung);
            Assert.Equal((1, 1, 2, 1), (e.Neu, e.Aktualisiert, e.Behalten, e.Ausgelaufen));
            Assert.Equal(11, e.Unveraendert);
            Assert.Equal(1, Katalogabgleich.FassungDerDatenbank());

            // eingefügt: gesperrt, mit Schlüssel und Prüfsumme der Probe
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = 'Einschicht 5 Tage' AND " +
                                  "\"ReadOnly\" = 1 AND Katalog_Schluessel = 'PW:EINSCHICHT_5_TAGE' AND Katalog_Ausgelaufen = 0"));
            // aktualisiert: wieder der Wert der Probe
            Assert.Equal(probenwert, Text("SELECT Monat_1 FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = 'Zweischicht 5 Tage'"));
            // behalten: die Anpassungen stehen
            Assert.Equal("eigene Fassung", Text("SELECT Beschreibung FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = 'Dreischicht 5 Tage'"));
            Assert.Equal("1", Text("SELECT Monat_2 FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = 'Durchlaufbetrieb 7 Tage'"));
            Assert.Equal("0", Text("SELECT \"ReadOnly\" FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = 'Durchlaufbetrieb 7 Tage'"));
            KatalogabgleichEintrag geaendert = e.Eintraege.Single(x => x.Schluessel == "PW:DREISCHICHT_5_TAGE");
            Assert.Equal(R.KABG_HINWEIS_BEHALTEN_GEAENDERT, geaendert.Hinweis);
            Assert.True(geaendert.Wiederherstellbar);
            Assert.Equal(R.KABG_HINWEIS_BEHALTEN_ENTSPERRT,
                         e.Eintraege.Single(x => x.Schluessel == "PW:DURCHLAUFBETRIEB_7_TAGE").Hinweis);
            // ausgelaufen: gekennzeichnet, nicht gelöscht
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = 'Raumlufttechnik Halle' AND Katalog_Ausgelaufen = 1"));

            // Protokoll: je Aktion eine Zeile und der Bericht
            IReadOnlyList<KatalogabgleichProtokollzeile> protokoll = Katalogabgleich.Protokoll();
            Assert.Equal(6, protokoll.Count);
            Assert.Equal(Katalogabgleich.AKTION_BERICHT, protokoll[0].Aktion);
            Assert.Equal(e.Zusammenfassung(), protokoll[0].Hinweis);
            Assert.All(protokoll, z => Assert.Equal(1, z.Fassung));

            // Projektkopien unberührt
            Assert.Equal(projektVorher, Projektbild());

            // Zweiter Lauf: tut nichts - weder ohne noch mit Zwang.
            string katalogNachher = Katalogbild();
            KatalogabgleichErgebnis zweiter = Katalogabgleich.Ausfuehren(paket, nurPruefen: false);
            Assert.False(zweiter.Ausgefuehrt);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.KABG_BEREITS, 1), zweiter.Meldung);
            KatalogabgleichErgebnis erzwungen = Katalogabgleich.Ausfuehren(paket, nurPruefen: false, erzwingen: true);
            Assert.Equal((0, 0, 2, 0), (erzwungen.Neu, erzwungen.Aktualisiert, erzwungen.Behalten, erzwungen.Ausgelaufen));
            Assert.Equal(katalogNachher, Katalogbild());
            Assert.Equal(6, Katalogabgleich.Protokoll().Count);
            Assert.Equal(projektVorher, Projektbild());
        }

        /// <summary>Wiederherstellen setzt Werte, Schloss und Prüfsumme des Pakets — eine Zeile im Protokoll.</summary>
        [Fact]
        public void Wiederherstellen_bringt_den_Auslieferungsstand_zurueck()
        {
            if (!_db.Vorhanden) return;

            Katalogpaket probe = Katalogpaket.Lesen(Probe);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Prozesswaerme_STAMM SET \"ReadOnly\" = 0, Beschreibung = 'eigen' WHERE Bezeichner = 'Dreischicht 5 Tage'");

            (bool ok, string meldung) = Katalogabgleich.Wiederherstellen(probe, "Tab_Prozesswaerme_STAMM", "PW:DREISCHICHT_5_TAGE");
            Assert.True(ok, meldung);
            string summe = probe.Tabellen[0].Saetze.Single(s => s.Schluessel == "PW:DREISCHICHT_5_TAGE").Pruefsumme;
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = 'Dreischicht 5 Tage' AND " +
                                  "\"ReadOnly\" = 1 AND Katalog_Pruefsumme = '" + summe + "' AND Beschreibung <> 'eigen'"));
            Assert.Equal(Katalogabgleich.AKTION_WIEDERHERGESTELLT, Katalogabgleich.Protokoll().Single().Aktion);

            (bool fremd, _) = Katalogabgleich.Wiederherstellen(probe, "Tab_Prozesswaerme_STAMM", "PW:GIBT_ES_NICHT");
            Assert.False(fremd);
        }

        /// <summary>
        /// Die Kennlinie gehört zum Satz der Wärmepumpe: Ein neuer Auslieferungsstand mit geänderter
        /// Kennlinie ersetzt die Kindzeilen des Katalogs — die Kennlinien der Projektkopien bleiben.
        /// </summary>
        [Fact]
        public void Abgleich_ersetzt_die_Kennlinie_des_Katalogs_und_laesst_die_Projektkennlinien()
        {
            if (!_db.Vorhanden) return;

            Katalogpaket paket = Katalogpaket.AusDatenbank(2);
            Katalogpakettabelle wp = paket.Tabellen.Single(t => t.Tabelle == "Tab_WP_STAMM");
            Katalogpaketsatz satz = wp.Saetze.First(s => s.Kinder.TryGetValue("Tab_Kenndaten_STAMM", out var k) && k.Count > 1);
            List<Dictionary<string, object>> kennlinie = satz.Kinder["Tab_Kenndaten_STAMM"];
            kennlinie[0]["COP"] = 9.25;
            kennlinie.RemoveAt(kennlinie.Count - 1);
            satz.Pruefsumme = Katalogfassung.Pruefsumme(Katalogfassung.Tabelle("Tab_WP_STAMM"), satz.Werte,
                satz.Kinder.ToDictionary(k => k.Key,
                    k => (IReadOnlyList<IReadOnlyDictionary<string, object>>)k.Value.Cast<IReadOnlyDictionary<string, object>>().ToList()));

            long id = Zahl("SELECT ID FROM Tab_WP_STAMM WHERE Katalog_Schluessel = '" + satz.Schluessel + "'");
            long zeilenVorher = Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM WHERE ID_WP = " + id);
            string projektVorher = Projektbild();

            KatalogabgleichErgebnis e = Katalogabgleich.Ausfuehren(paket, nurPruefen: false);
            Assert.Equal((0, 1, 0, 0), (e.Neu, e.Aktualisiert, e.Behalten, e.Ausgelaufen));
            Assert.Equal(zeilenVorher - 1, Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM WHERE ID_WP = " + id));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_STAMM WHERE ID_WP = " + id + " AND COP = 9.25"));
            Assert.Equal(satz.Pruefsumme, Text("SELECT Katalog_Pruefsumme FROM Tab_WP_STAMM WHERE ID = " + id));
            Assert.Equal(projektVorher, Projektbild());
        }

        /// <summary>Ohne Paket: kein Abgleich, benannt im Protokoll; beim Start ohne Paket geschieht nichts.</summary>
        [Fact]
        public void Kein_Paket_ist_benannt_und_der_Start_ohne_Paket_tut_nichts()
        {
            if (!_db.Vorhanden) return;

            string fehlt = Path.Combine(_db.Ordner, "gibt-es-nicht", Katalogpaket.DATEINAME);
            KatalogabgleichErgebnis start = Katalogabgleich.BeimStart(fehlt, () => throw new InvalidOperationException("keine Sicherung"));
            Assert.False(start.Ausgefuehrt);
            Assert.Empty(Katalogabgleich.Protokoll());

            KatalogabgleichErgebnis e = Katalogabgleich.AusDatei(fehlt, nurPruefen: false);
            Assert.False(e.Ausgefuehrt);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.KABG_KEIN_PAKET, fehlt), e.Meldung);
            Assert.Equal(Katalogabgleich.AKTION_KEIN_PAKET, Katalogabgleich.Protokoll().Single().Aktion);
            Assert.Null(Katalogabgleich.FassungDerDatenbank());

            string kaputt = Path.Combine(_db.Ordner, "kaputt.json");
            File.WriteAllText(kaputt, "{ \"Format\": \"etwas anderes\" }");
            Assert.False(Katalogabgleich.AusDatei(kaputt, nurPruefen: false).Ausgefuehrt);
            Assert.Equal(2, Katalogabgleich.Protokoll().Count);
        }

        /// <summary>
        /// Beim Start: nur eine NEUERE Fassung gleicht ab, erst nach der Sicherung; der Bericht liegt
        /// einmal für die Überlagerung bereit.
        /// </summary>
        [Fact]
        public void Beim_Start_gleicht_nur_eine_neuere_Fassung_nach_der_Sicherung_ab()
        {
            if (!_db.Vorhanden) return;

            Katalogabgleich.StartberichtAbholen();
            string datei = Path.Combine(_db.Ordner, Katalogpaket.DATEINAME);
            Katalogpaket paket = Katalogpaket.Lesen(Probe);
            paket.Tabellen[0].Saetze.RemoveAll(s => s.Schluessel == "PW:WASCHEN_BAEDER");
            paket.Speichern(datei);

            int sicherungen = 0;
            KatalogabgleichErgebnis e = Katalogabgleich.BeimStart(datei, () => { sicherungen++; return "sicherung.sqlite"; });
            Assert.True(e.Ausgefuehrt);
            Assert.Equal(1, sicherungen);
            Assert.Equal(1, e.Ausgelaufen);
            KatalogabgleichErgebnis bericht = Katalogabgleich.StartberichtAbholen();
            Assert.Same(e, bericht);
            Assert.Null(Katalogabgleich.StartberichtAbholen());
            Assert.Contains("sicherung.sqlite", bericht.Starttext());

            KatalogabgleichErgebnis zweiter = Katalogabgleich.BeimStart(datei, () => { sicherungen++; return ""; });
            Assert.False(zweiter.Ausgefuehrt);
            Assert.Equal(1, sicherungen);
            Assert.Null(Katalogabgleich.StartberichtAbholen());
        }

        /// <summary>Festschreiben (Werkzeug Auslieferungsvorlage): Prüfsummen auf den heutigen Stand, Fassung gesetzt.</summary>
        [Fact]
        public void Festschreiben_setzt_Pruefsummen_und_Fassung()
        {
            if (!_db.Vorhanden) return;

            DataRepository.ExecuteNonQuery("UPDATE Tab_Prozesswaerme_STAMM SET Beschreibung = 'gepflegt' WHERE Bezeichner = 'Dreischicht 5 Tage'");
            Katalogpaket p = Katalogpaket.Festschreiben(7);
            Assert.Equal(7, p.Fassung);
            Assert.Equal(7, Katalogabgleich.FassungDerDatenbank());
            KatalogabgleichErgebnis plan = Katalogabgleich.Ausfuehren(p, nurPruefen: true, erzwingen: true);
            Assert.Equal(0, plan.Neu + plan.Aktualisiert + plan.Behalten + plan.Ausgelaufen);
            Assert.Equal(p.Bytes(), Katalogpaket.AusDatenbank(7).Bytes());
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static string Text(string sql) => Convert.ToString(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static Dictionary<string, string> Schluesselbild()
        {
            var d = new Dictionary<string, string>();
            foreach (Katalogtabelle t in Katalogfassung.Stufe1)
            {
                DataTable dt = DataRepository.GetDataTable("SELECT ID, Katalog_Schluessel, Katalog_Pruefsumme FROM \"" + t.Tabelle + "\" ORDER BY ID");
                foreach (DataRow r in dt.Rows)
                    d[t.Tabelle + "/" + r[0]] = Convert.ToString(r[1]) + "|" + Convert.ToString(r[2]);
            }
            return d;
        }

        /// <summary>Schreibt die gespeicherte Prüfsumme eines Satzes auf seine heutigen Werte („so ausgeliefert").</summary>
        private static void PruefsummeFestschreiben(string tabelle, string bezeichner)
        {
            Katalogtabelle t = Katalogfassung.Tabelle(tabelle);
            List<string> spalten = t.Fachspalten.ToList();
            DataTable dt = DataRepository.GetDataTable("SELECT ID, " + string.Join(", ", spalten.Select(s => "\"" + s + "\"")) +
                                                       " FROM \"" + tabelle + "\" WHERE Bezeichner = ?", new DbParam("@b", bezeichner));
            string summe = Katalogfassung.PruefsummeDerZeile(t, spalten, dt.Rows[0], Katalogfassung.LeseOhneVorgang);
            DataRepository.ExecuteNonQuery("UPDATE \"" + tabelle + "\" SET Katalog_Pruefsumme = ? WHERE Bezeichner = ?",
                                           new DbParam("@s", summe), new DbParam("@b", bezeichner));
        }

        /// <summary>Ein Bild aller Zeilen der Prozesswärmekataloge (Werte samt Katalogspalten).</summary>
        private static string Katalogbild() => Tabellenbild("Tab_Prozesswaerme_STAMM", "Tab_Prozesstyp_STAMM", "Tab_Katalogabgleich");

        /// <summary>Ein Bild der Projektkopien und Zuordnungen, die ein Abgleich nie anfassen darf.</summary>
        private static string Projektbild() =>
            Tabellenbild("Tab_Prozesswaerme", "Tab_Prozesstyp", "Z_Projekt_Prozesswaerme", "Tab_WP", "Tab_Kenndaten",
                         "Tab_Kenndaten_Kuehlung", "Tab_BHKW", "Tab_Heizkessel", "Tab_Brauchwasser", "Tab_Brauchwassertyp");

        private static string Tabellenbild(params string[] tabellen)
        {
            var sb = new StringBuilder();
            foreach (string t in tabellen)
            {
                if (!DataRepository.TabelleVorhanden(t)) continue;
                DataTable dt = DataRepository.GetDataTable("SELECT * FROM \"" + t + "\" ORDER BY 1");
                sb.Append('#').Append(t).Append('\n');
                foreach (DataRow r in dt.Rows)
                    sb.Append(string.Join("|", r.ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture)))).Append('\n');
            }
            using (SHA256 sha = SHA256.Create())
                return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
        }
    }
}
